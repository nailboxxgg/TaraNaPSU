using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Fetches latest room names and configuration from Firebase Firestore REST API.
/// This avoids the need for the heavy Firebase SDK.
/// </summary>
public class CloudDataSync : MonoBehaviour
{
    private const string PROJECT_ID = "taranapsu";
    private const string FIRESTORE_URL = "https://firestore.googleapis.com/v1/projects/" + PROJECT_ID + "/databases/(default)/documents/rooms?pageSize=1000";

    [Header("Settings")]
    public bool syncOnStart = true;
    public float timeout = 10f;

    void Start()
    {
        if (syncOnStart)
        {
            SyncNow();
        }
    }

    public void SyncNow()
    {
        StartCoroutine(FetchRoomsRoutine());
    }

    private IEnumerator FetchRoomsRoutine()
    {
        Debug.Log("[CloudDataSync] Fetching latest room data...");

        using (UnityWebRequest request = UnityWebRequest.Get(FIRESTORE_URL))
        {
            request.timeout = (int)timeout;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[CloudDataSync] Failed to fetch data: {request.error}");
                // If cloud fails, we just keep the local defaults loaded by TargetManager
                yield break;
            }

            try
            {
                string json = request.downloadHandler.text;
                List<TargetData> cloudTargets = ParseFirestoreResponse(json);
                
                if (cloudTargets != null && cloudTargets.Count > 0)
                {
                    TargetManager.Instance.UpdateTargets(cloudTargets);
                    Debug.Log($"[CloudDataSync] Successfully synced {cloudTargets.Count} rooms.");
                }
                else
                {
                    Debug.LogWarning("[CloudDataSync] Downloaded data was empty or invalid.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudDataSync] Error parsing response: {e.Message}");
            }
        }
    }

    // --- Firestore JSON Parsing (Manual Helpers) ---

    // Root object
    [Serializable]
    private class FirestoreRoot
    {
        public List<FirestoreDoc> documents;
    }

    [Serializable]
    private class FirestoreDoc
    {
        public FirestoreFields fields;
    }

    [Serializable]
    private class FirestoreFields
    {
        public StringValue Name;
        public IntegerValue FloorNumber; // Sometimes returned as integerValue (string)
        public MapValue Position;
        public MapValue Rotation;
    }

    [Serializable]
    private class StringValue { public string stringValue; }
    
    [Serializable]
    private class IntegerValue { public string integerValue; } // Firestore returns integers as strings!
    
    [Serializable]
    private class DoubleValue { public double doubleValue; }

    [Serializable]
    private class MapValue
    {
        public MapValueFields mapValue;
    }

    [Serializable]
    private class MapValueFields
    {
        public VectorFields fields;
    }

    [Serializable]
    private class VectorFields
    {
        public DoubleValue x;
        public DoubleValue y;
        public DoubleValue z;
    }

    private List<TargetData> ParseFirestoreResponse(string json)
    {
        // Wrap for JsonUtility
        FirestoreRoot root = JsonUtility.FromJson<FirestoreRoot>(json);
        
        if (root == null || root.documents == null) return null;

        List<TargetData> results = new List<TargetData>();

        foreach (var doc in root.documents)
        {
            if (doc.fields == null) continue;

            // Manual mapping from Firestore structure to TargetData
            TargetData item = new TargetData();
            
            // Name
            item.Name = doc.fields.Name != null ? doc.fields.Name.stringValue : "Unknown";

            // Floor Number
            if (doc.fields.FloorNumber != null && !string.IsNullOrEmpty(doc.fields.FloorNumber.integerValue))
            {
                int.TryParse(doc.fields.FloorNumber.integerValue, out item.FloorNumber);
            }

            // Position
            item.Position = new Vector3Serializable();
            if (doc.fields.Position != null && doc.fields.Position.mapValue != null && doc.fields.Position.mapValue.fields != null)
            {
                var f = doc.fields.Position.mapValue.fields;
                item.Position.x = (float)(f.x != null ? f.x.doubleValue : 0);
                item.Position.y = (float)(f.y != null ? f.y.doubleValue : 0);
                item.Position.z = (float)(f.z != null ? f.z.doubleValue : 0);
            }

            // Rotation
            item.Rotation = new Vector3Serializable();
            if (doc.fields.Rotation != null && doc.fields.Rotation.mapValue != null && doc.fields.Rotation.mapValue.fields != null)
            {
                var f = doc.fields.Rotation.mapValue.fields;
                item.Rotation.x = (float)(f.x != null ? f.x.doubleValue : 0);
                item.Rotation.y = (float)(f.y != null ? f.y.doubleValue : 0);
                item.Rotation.z = (float)(f.z != null ? f.z.doubleValue : 0);
            }

            results.Add(item);
        }

        return results;
    }
}

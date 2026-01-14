using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using Core.Config;

[Serializable]
public class TargetDataList
{
    public List<TargetData> TargetList;
}

[Serializable]
public class TargetData
{
    public string Name;
    public int FloorNumber;
    public Vector3Serializable Position;
    public Vector3Serializable Rotation;
}

[Serializable]
public class Vector3Serializable
{
    public float x, y, z;

    public Vector3 ToVector3() => new Vector3(x, y, z);
}

public class TargetManager : MonoBehaviour
{
    private static TargetManager _instance;
    public static TargetManager Instance
    {
        get
        {
            if (_instance == null) _instance = FindObjectOfType<TargetManager>();
            return _instance;
        }
    }

    [Header("Sync Settings")]
    public FirebaseConfig firebaseConfig;
    private string cachePath;

    private Dictionary<string, TargetData> targets = new();
    public bool IsLoading { get; private set; }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        cachePath = Path.Combine(Application.persistentDataPath, "TargetData_Cache.json");
        InitializeData();
    }

    private void InitializeData()
    {
        // 1. Try to load from Cache first (Persistent storage)
        if (LoadFromCache())
        {
            Debug.Log("📂 Loaded targets from local cache.");
        }
        else
        {
            // 2. Fallback to Resources (Default shipped data)
            LoadFromResources();
        }

        // 3. Start background sync if config is available
        if (firebaseConfig != null)
        {
            StartCoroutine(SyncWithFirestore());
        }
    }

    private bool LoadFromCache()
    {
        if (!File.Exists(cachePath)) return false;

        try
        {
            string json = File.ReadAllText(cachePath);
            UpdateTargetsFromJson(json);
            return targets.Count > 0;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ Failed to load cache: {e.Message}");
            return false;
        }
    }

    private void LoadFromResources()
    {
        TextAsset json = Resources.Load<TextAsset>("TargetData");
        if (json == null)
        {
            Debug.LogError("❌ TargetData.json not found in Resources!");
            return;
        }

        UpdateTargetsFromJson(json.text);
        Debug.Log($"🏡 Loaded {targets.Count} default targets from Resources.");
    }

    private void UpdateTargetsFromJson(string jsonText)
    {
        var dataList = JsonUtility.FromJson<TargetDataList>(jsonText);
        if (dataList?.TargetList == null) return;

        foreach (var target in dataList.TargetList)
        {
            if (!targets.ContainsKey(target.Name))
                targets.Add(target.Name, target);
            else
                targets[target.Name] = target; // Update existing
        }
    }

    private IEnumerator SyncWithFirestore()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.Log("📶 No internet, skipping sync.");
            yield break;
        }

        IsLoading = true;
        string url = firebaseConfig.GetFirestoreUrl();
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("⭐ Successfully fetched latest room data from Firebase.");
                ProcessFirestoreResponse(request.downloadHandler.text);
            }
            else
            {
                Debug.LogWarning($"☁️ Sync failed: {request.error}");
            }
        }
        IsLoading = false;
    }

    private void ProcessFirestoreResponse(string json)
    {
        try {
            // Firestore REST API wraps everything in 'fields'.
            // For 'sync/latest', the structure is { "fields": { "TargetList": { "arrayValue": { "values": [...] } } } }
            
            // If the JSON is already in our flat format, update directly
            if (json.Contains("\"TargetList\":["))
            {
                UpdateData(json);
                return;
            }

            // Simple flattening for Firestore REST structure
            // This is a minimal approach to avoid adding large JSON libraries
            string flattenedJson = SanitizeFirestoreJson(json);
            UpdateData(flattenedJson);

        } catch (Exception e) {
            Debug.LogWarning($"⚠️ Sync processing failed: {e.Message}");
        }
    }

    private void UpdateData(string json)
    {
        var dataList = JsonUtility.FromJson<TargetDataList>(json);
        if (dataList != null && dataList.TargetList != null && dataList.TargetList.Count > 0)
        {
            File.WriteAllText(cachePath, json);
            UpdateTargetsFromJson(json);
            Debug.Log("💾 Cache updated with latest application data.");
        }
    }

    private string SanitizeFirestoreJson(string json)
    {
        // 1. Convert Firestore value wrappers to flat values
        // "stringValue": "abc" -> "abc"
        string result = Regex.Replace(json, "\"stringValue\":\\s*\"([^\"]*)\"", "\"$1\"");
        
        // "doubleValue": 1.23 -> 1.23 (no quotes for float/double)
        result = Regex.Replace(result, "\"doubleValue\":\\s*([0-9.-]+)", "$1");
        
        // "integerValue": "123" -> 123
        result = Regex.Replace(result, "\"integerValue\":\\s*\"([0-9.-]+)\"", "$1");

        // 2. Unwrap the { "value" } or { "anyKey": value } patterns
        // Matches: { "something": "value" } -> "value"
        result = Regex.Replace(result, "\\{[^\\{\\}]*\"[^\"]+\":\\s*(\"[^\"]*\"|[0-9.-]+)\\s*\\}", "$1");

        // 3. Handle 'fields' wrapper which appears in Firestore responses
        if (result.Contains("\"fields\":"))
        {
            // Simple unwrap of first 'fields' level
            int fieldsIndex = result.IndexOf("\"fields\":");
            int firstBrace = result.IndexOf('{', fieldsIndex);
            int lastBrace = result.LastIndexOf('}');
            if (firstBrace != -1 && lastBrace != -1)
            {
                result = result.Substring(firstBrace, lastBrace - firstBrace);
            }
        }
        
        return result;
    }

    public List<string> GetAllTargetNames() => new List<string>(targets.Keys);
    public bool TryGetTarget(string name, out TargetData data) => targets.TryGetValue(name, out data);
}

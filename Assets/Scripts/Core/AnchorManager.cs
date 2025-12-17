using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages anchor and stair metadata loaded from AnchorData.json.
/// Provides lookup and helper methods for navigation and QR logic.
/// </summary>
public class AnchorManager : MonoBehaviour
{
    public static AnchorManager Instance { get; private set; }

    [Header("Anchor Data")]
    public TextAsset anchorDataFile;  // Optional manual override (else loads from Resources)
    public List<AnchorData> Anchors= new List<AnchorData>();
    public List<StairPair> stairPairs = new List<StairPair>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadAnchors();
    }

    // --------------------------------------------------------------------
    // 📦 Data Loading
    // --------------------------------------------------------------------

    private void LoadAnchors()
    {
        try
        {
            string json;
            string source = "Resources";

            if (anchorDataFile != null)
            {
                json = anchorDataFile.text;
                source = "Manual Override (Inspector)";
            }
            else
            {
                TextAsset file = Resources.Load<TextAsset>("AnchorData");
                if (file == null)
                {
                    Debug.LogError("❌ AnchorManager: Couldn't find Resources/AnchorData.json");
                    return;
                }
                json = file.text;
            }

            Debug.Log($"[AnchorManager] Loading anchors from {source}. JSON Length: {json.Length}");

            var wrapper = JsonUtility.FromJson<AnchorListWrapper>(json);
            if (wrapper != null && wrapper.anchors != null)
            {
                Anchors = wrapper.anchors;
                Debug.Log($"[AnchorManager] Successfully loaded {Anchors.Count} anchors.");
                
                // Log all IDs for debugging
                string ids = string.Join(", ", Anchors.Select(a => a.AnchorId));
                Debug.Log($"[AnchorManager] Loaded IDs: {ids}");
            }
            else
            {
                Debug.LogWarning("⚠️ AnchorManager: wrapper is null or anchors list is null.");
            }

            BuildStairPairs();
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ AnchorManager: Error loading anchors - {ex.Message}\n{ex.StackTrace}");
        }
    }

    // --------------------------------------------------------------------
    // 🧩 Building & Linking Anchors
    // --------------------------------------------------------------------

    private void BuildStairPairs()
    {
        stairPairs.Clear();

        // Get all stairs
        var stairs = Anchors.Where(a => a.Type == "stair" || a.AnchorId.Contains("Stair")).ToList();

        // Helper to extract marker number (e.g. "Marker 1" -> "1")
        string GetMarkerNumber(string id)
        {
            var parts = id.Split(' ');
            if (parts.Length > 0 && int.TryParse(parts.Last(), out _))
                return parts.Last(); // Returns "1", "2"
            return "";
        }

        // Separate by floor
        var groundStairs = stairs.Where(a => a.Floor == 0).ToList();
        var firstStairs = stairs.Where(a => a.Floor == 1).ToList();

        foreach (var stairDown in groundStairs)
        {
            string markerNum = GetMarkerNumber(stairDown.AnchorId);
            
            // Find matching stair on Floor 1 with same Marker Number
            // We ignore BuildingId strict mapping to allow B1->B2 user scenario if intended
            var stairUp = firstStairs.FirstOrDefault(s => GetMarkerNumber(s.AnchorId) == markerNum);

            if (stairUp != null)
            {
                stairPairs.Add(new StairPair
                {
                    BuildingId = stairDown.BuildingId, // Associate pair with the ground building
                    Bottom = stairDown,
                    Top = stairUp
                });
                Debug.Log($"[AnchorManager] Linked Stair Pair: {stairDown.AnchorId} <-> {stairUp.AnchorId}");
            }
        }

        Debug.Log($"[AnchorManager] Linked {stairPairs.Count} stair pairs.");
    }

    // --------------------------------------------------------------------
    // 🔍 Public API
    // --------------------------------------------------------------------

    /// <summary>
    /// Returns the nearest stair pair connecting two floors in a specific building.
    /// </summary>
    public StairPair FindNearestStair(string buildingId, int fromFloor, int toFloor, Vector3 currentPos)
    {
        var candidates = stairPairs.Where(s =>
            s.BuildingId == buildingId &&
            ((s.Bottom.Floor == fromFloor && s.Top.Floor == toFloor) ||
             (s.Top.Floor == fromFloor && s.Bottom.Floor == toFloor))
        );

        StairPair nearest = null;
        float bestDist = float.MaxValue;

        foreach (var stair in candidates)
        {
            float dist = Vector3.Distance(currentPos, stair.Bottom.PositionVector);
            if (dist < bestDist)
            {
                bestDist = dist;
                nearest = stair;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Returns all anchors for a building and floor.
    /// </summary>
    public List<AnchorData> GetAnchors(string buildingId, int floor)
    {
        return Anchors.Where(a => a.BuildingId == buildingId && a.Floor == floor).ToList();
    }

    /// <summary>
    /// Find anchor by its unique ID.
    /// </summary>
    public AnchorData FindAnchor(string anchorId)
    {
        if (string.IsNullOrEmpty(anchorId)) return null;

        var anchor = Anchors.FirstOrDefault(a => 
            string.Equals(a.AnchorId.Trim(), anchorId.Trim(), StringComparison.OrdinalIgnoreCase));

        if (anchor == null)
        {
            Debug.LogWarning($"[AnchorManager] FindAnchor: Could not find '{anchorId}'. Available Anchors ({Anchors.Count}): {string.Join(", ", Anchors.Select(a => a.AnchorId))}");
        }
        return anchor;
    }

    // --------------------------------------------------------------------
    // 🧱 Data Structures
    // --------------------------------------------------------------------

    [Serializable]
    public class AnchorListWrapper
    {
        public List<AnchorData> anchors;
    }

    [Serializable]
    public class AnchorData
    {
        public string Type;        // "anchor", "stair", "entrance", etc.
        public string BuildingId;  // e.g., "B1"
        public string AnchorId;    // e.g., "B1-Stair-North-Up"
        public int Floor;          // e.g., 0 or 1
        public Vector3Serializable Position;
        public Vector3Serializable Rotation;
        public string Meta;        // optional description

        public Vector3 PositionVector => Position.ToVector3();
        public Quaternion RotationQuaternion => Quaternion.Euler(Rotation.ToVector3());
    }

    [Serializable]
    public class StairPair
    {
        public string BuildingId;
        public AnchorData Bottom;
        public AnchorData Top;

        public bool IsValid => Bottom != null && Top != null;
    }

    [System.Serializable]
    public class Vector3Serializable
    {
        public float x, y, z;

        public Vector3Serializable() { }

        public Vector3Serializable(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3Serializable(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

}

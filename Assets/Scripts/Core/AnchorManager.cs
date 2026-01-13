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

            if (anchorDataFile != null)
                json = anchorDataFile.text;
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

            var wrapper = JsonUtility.FromJson<AnchorListWrapper>(json);
            if (wrapper != null && wrapper.anchors != null)
            {
                Anchors = wrapper.anchors;
                Debug.Log($"[AnchorManager] Loaded {Anchors.Count} anchors from JSON");
            }
            else
            {
                Debug.LogWarning("⚠️ AnchorManager: AnchorData.json is empty or invalid.");
            }

            BuildStairPairs();
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ AnchorManager: Error loading anchors - {ex.Message}");
        }
    }

    // --------------------------------------------------------------------
    // 🧩 Building & Linking Anchors
    // --------------------------------------------------------------------

    private void BuildStairPairs()
    {
        stairPairs.Clear();

        // Group anchors by building and floor
        var stairs = Anchors.Where(a => a.Type == "stair").ToList();

        foreach (var stairGroup in stairs.GroupBy(a => a.BuildingId))
        {
            var buildingStairs = stairGroup.ToList();
            for (int i = 0; i < buildingStairs.Count; i++)
            {
                var stairA = buildingStairs[i];
                var stairB = buildingStairs.FirstOrDefault(s =>
                    s.Floor == stairA.Floor + 1 &&
                    s.AnchorId.StartsWith(stairA.AnchorId.Replace("-Down", "").Replace("-Up", ""))
                );

                if (stairB != null)
                {
                    stairPairs.Add(new StairPair
                    {
                        BuildingId = stairA.BuildingId,
                        Bottom = stairA,
                        Top = stairB
                    });
                }
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
        return Anchors.FirstOrDefault(a => a.AnchorId == anchorId);
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

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages anchor and stair metadata loaded from AnchorData.json.
/// Provides lookup and helper methods for navigation and QR logic.
/// Updated to support Building Connections (Entrances/Exits).
/// </summary>
public class AnchorManager : MonoBehaviour
{
    public static AnchorManager Instance { get; private set; }

    [Header("Anchor Data")]
    public TextAsset anchorDataFile;  // Optional manual override (else loads from Resources)
    public List<AnchorData> Anchors = new List<AnchorData>();
    public List<StairPair> stairPairs = new List<StairPair>();
    public List<BuildingConnection> connections = new List<BuildingConnection>();

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
            BuildConnections();
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

    private void BuildConnections()
    {
        connections.Clear();

        // Find all anchors tagged as "entrance" or "connector"
        var looseConnectors = Anchors.Where(a => 
            a.Type.ToLower() == "entrance" || 
            a.Type.ToLower() == "connector").ToList();

        // Group them by AnchorId. We assume a pair of connectors (one on each side) share the EXACT same ID.
        // Example: "MainGate-To-B1" exists in Building "Campus" AND Building "B1".
        foreach (var group in looseConnectors.GroupBy(a => a.AnchorId))
        {
            var pair = group.ToList();
            if (pair.Count >= 2)
            {
                // We found a link!
                connections.Add(new BuildingConnection
                {
                    NodeA = pair[0],
                    NodeB = pair[1]
                });
                Debug.Log($"[AnchorManager] 🔗 Linked Buildings: {pair[0].BuildingId} <-> {pair[1].BuildingId} via {pair[0].AnchorId}");
            }
        }

        Debug.Log($"[AnchorManager] Built {connections.Count} building connections.");
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
    /// Returns the best connector to go from currentBuilding -> targetBuilding.
    /// </summary>
    public BuildingConnection FindBestConnector(string currentBuilding, string targetBuilding, Vector3 currentPos)
    {
        // Find connections that have one node in Start and one node in Target
        var candidates = connections.Where(c => 
            (c.NodeA.BuildingId == currentBuilding && c.NodeB.BuildingId == targetBuilding) ||
            (c.NodeB.BuildingId == currentBuilding && c.NodeA.BuildingId == targetBuilding)
        ).ToList();

        if (candidates.Count == 0) return null;

        // Find nearest based on distance to the "Entry" node
        BuildingConnection best = null;
        float bestDist = float.MaxValue;

        foreach (var conn in candidates)
        {
            // Determine which node is the "Entry" (on our side)
            var entryNode = (conn.NodeA.BuildingId == currentBuilding) ? conn.NodeA : conn.NodeB;
            
            float d = Vector3.Distance(currentPos, entryNode.PositionVector);
            if (d < bestDist)
            {
                bestDist = d;
                best = conn;
            }
        }

        return best;
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
        public string Type;        // "anchor", "stair", "entrance", "connector"
        public string BuildingId;  // e.g., "B1", "Campus"
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

    [Serializable]
    public class BuildingConnection
    {
        public AnchorData NodeA;
        public AnchorData NodeB;

        public string Id => NodeA != null ? NodeA.AnchorId : "?";
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

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AnchorManager : MonoBehaviour
{
    public static AnchorManager Instance { get; private set; }

    [Header("Anchor Data")]
    public TextAsset anchorDataFile;  
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

    private void BuildStairPairs()
    {
        stairPairs.Clear();

        var stairs = Anchors.Where(a => a.Type == "stair" || a.AnchorId.Contains("Stair")).ToList();

        string GetMarkerNumber(string id)
        {
            var parts = id.Split(' ');
            if (parts.Length > 0 && int.TryParse(parts.Last(), out _))
                return parts.Last(); 
            return "";
        }

        var groundStairs = stairs.Where(a => a.Floor == 0).ToList();
        var firstStairs = stairs.Where(a => a.Floor == 1).ToList();

        foreach (var stairDown in groundStairs)
        {
            string markerNum = GetMarkerNumber(stairDown.AnchorId);
            
            var stairUp = firstStairs.FirstOrDefault(s => GetMarkerNumber(s.AnchorId) == markerNum);

            if (stairUp != null)
            {
                stairPairs.Add(new StairPair
                {
                    BuildingId = stairDown.BuildingId, 
                    Bottom = stairDown,
                    Top = stairUp
                });
                Debug.Log($"[AnchorManager] Linked Stair Pair: {stairDown.AnchorId} <-> {stairUp.AnchorId}");
            }
        }

        Debug.Log($"[AnchorManager] Linked {stairPairs.Count} stair pairs.");
    }

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

    public List<AnchorData> GetAnchors(string buildingId, int floor)
    {
        return Anchors.Where(a => a.BuildingId == buildingId && a.Floor == floor).ToList();
    }

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
}

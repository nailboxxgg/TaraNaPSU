using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages visibility of all target markers in the scene.
/// Hides all markers except the selected destination during navigation.
/// </summary>
public class TargetMarkerManager : MonoBehaviour
{
    public static TargetMarkerManager Instance { get; private set; }

    [Header("Target Marker Settings")]
    public GameObject targetMarkerPrefab;  // Optional: prefab to create markers if they don't exist
    public bool createMarkersDynamically = false;  // Set to true if markers should be created at runtime
    public Transform markersParent;  // Parent transform for all marker objects

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Dictionary<string, GameObject> allTargetMarkers = new Dictionary<string, GameObject>();
    private GameObject currentActiveMarker;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeTargetMarkers();
    }

    /// <summary>
    /// Initialize target markers - either find existing ones in scene or create them dynamically
    /// </summary>
    private void InitializeTargetMarkers()
    {
        if (TargetManager.Instance == null)
        {
            Debug.LogError("[TargetMarkerManager] TargetManager.Instance is null!");
            return;
        }

        var allTargetNames = TargetManager.Instance.GetAllTargetNames();

        if (createMarkersDynamically)
        {
            CreateMarkersDynamically(allTargetNames);
        }
        else
        {
            FindExistingMarkers(allTargetNames);
        }

        // Initially hide all markers
        HideAllMarkers();

        if (showDebugLogs)
        {
            Debug.Log($"[TargetMarkerManager] Initialized with {allTargetMarkers.Count} target markers");
        }
    }

    /// <summary>
    /// Find existing target markers in the scene
    /// </summary>
    private void FindExistingMarkers(List<string> targetNames)
    {
        foreach (string targetName in targetNames)
        {
            // Look for GameObjects with names that match the target pattern
            GameObject marker = GameObject.Find($"TargetPin-{targetName}") ??
                              GameObject.Find($"Target-{targetName}") ??
                              GameObject.Find($"{targetName}-Marker") ??
                              GameObject.Find(targetName);

            if (marker != null)
            {
                allTargetMarkers[targetName] = marker;
                if (showDebugLogs)
                    Debug.Log($"[TargetMarkerManager] Found existing marker for: {targetName}");
            }
            else
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[TargetMarkerManager] No marker found for: {targetName}");
            }
        }
    }

    /// <summary>
    /// Create target markers dynamically if they don't exist in the scene
    /// </summary>
    private void CreateMarkersDynamically(List<string> targetNames)
    {
        if (targetMarkerPrefab == null)
        {
            Debug.LogError("[TargetMarkerManager] targetMarkerPrefab is not assigned!");
            return;
        }

        if (markersParent == null)
        {
            GameObject parent = new GameObject("TargetMarkers");
            parent.transform.position = Vector3.zero;
            markersParent = parent.transform;
        }

        foreach (string targetName in targetNames)
        {
            if (TargetManager.Instance.TryGetTarget(targetName, out var targetData))
            {
                Vector3 pos = targetData.Position.ToVector3();
                Quaternion rot = Quaternion.Euler(targetData.Rotation.ToVector3());

                GameObject marker = Instantiate(targetMarkerPrefab, pos, rot, markersParent);
                marker.name = $"TargetPin-{targetName}";
                allTargetMarkers[targetName] = marker;

                if (showDebugLogs)
                    Debug.Log($"[TargetMarkerManager] Created marker for: {targetName}");
            }
        }
    }

    /// <summary>
    /// Show only the selected target marker, hide all others
    /// </summary>
    public void ShowOnlyTarget(string targetName)
    {
        HideAllMarkers();

        if (allTargetMarkers.TryGetValue(targetName, out GameObject marker))
        {
            marker.SetActive(true);
            currentActiveMarker = marker;

            if (showDebugLogs)
                Debug.Log($"[TargetMarkerManager] Showing marker for: {targetName}");
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning($"[TargetMarkerManager] No marker found to show for: {targetName}");
        }
    }

    /// <summary>
    /// Hide all target markers
    /// </summary>
    public void HideAllMarkers()
    {
        foreach (var kvp in allTargetMarkers)
        {
            if (kvp.Value != null)
            {
                kvp.Value.SetActive(false);
            }
        }

        currentActiveMarker = null;

        if (showDebugLogs)
            Debug.Log("[TargetMarkerManager] Hidden all target markers");
    }

    /// <summary>
    /// Get the currently active target marker
    /// </summary>
    public GameObject GetCurrentActiveMarker()
    {
        return currentActiveMarker;
    }

    /// <summary>
    /// Check if a target has a marker
    /// </summary>
    public bool HasMarkerForTarget(string targetName)
    {
        return allTargetMarkers.ContainsKey(targetName) && allTargetMarkers[targetName] != null;
    }

    /// <summary>
    /// Add a new target marker at runtime
    /// </summary>
    public void AddTargetMarker(string targetName, GameObject marker)
    {
        if (!string.IsNullOrEmpty(targetName) && marker != null)
        {
            allTargetMarkers[targetName] = marker;
            marker.SetActive(false); // Initially hidden

            if (showDebugLogs)
                Debug.Log($"[TargetMarkerManager] Added runtime marker for: {targetName}");
        }
    }

    /// <summary>
    /// Remove a target marker at runtime
    /// </summary>
    public void RemoveTargetMarker(string targetName)
    {
        if (allTargetMarkers.TryGetValue(targetName, out GameObject marker))
        {
            if (marker == currentActiveMarker)
                currentActiveMarker = null;

            if (Application.isPlaying)
                Destroy(marker);
            else
                DestroyImmediate(marker);

            allTargetMarkers.Remove(targetName);

            if (showDebugLogs)
                Debug.Log($"[TargetMarkerManager] Removed marker for: {targetName}");
        }
    }
}
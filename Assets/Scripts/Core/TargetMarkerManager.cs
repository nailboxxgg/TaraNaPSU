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

                // Create missing marker as fallback if prefab is available
                if (targetMarkerPrefab != null)
                {
                    CreateSingleMarker(targetName);
                }
                else
                {
                    Debug.LogError($"[TargetMarkerManager] Cannot create marker for {targetName} - targetMarkerPrefab not assigned");
                }
            }
        }
    }

    /// <summary>
    /// Create a single target marker dynamically
    /// </summary>
    private void CreateSingleMarker(string targetName)
    {
        if (TargetManager.Instance.TryGetTarget(targetName, out var targetData))
        {
            // Create parent if it doesn't exist
            if (markersParent == null)
            {
                GameObject parent = new GameObject("TargetMarkers");
                parent.transform.position = Vector3.zero;
                markersParent = parent.transform;
            }

            Vector3 pos = targetData.Position.ToVector3();
            Quaternion rot = Quaternion.Euler(targetData.Rotation.ToVector3());

            GameObject marker = Instantiate(targetMarkerPrefab, pos, rot, markersParent);
            marker.name = $"TargetPin-{targetName}";
            allTargetMarkers[targetName] = marker;

            if (showDebugLogs)
                Debug.Log($"[TargetMarkerManager] Created missing marker for: {targetName} at {pos}");
        }
        else
        {
            Debug.LogError($"[TargetMarkerManager] Could not get target data for: {targetName}");
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
            if (marker != null)
            {
                marker.SetActive(true);
                currentActiveMarker = marker;

                if (showDebugLogs)
                    Debug.Log($"[TargetMarkerManager] Showing marker for: {targetName}");
            }
            else
            {
                // Remove the null marker from the dictionary and try to recreate it
                allTargetMarkers.Remove(targetName);
                if (showDebugLogs)
                    Debug.LogWarning($"[TargetMarkerManager] Marker for {targetName} was destroyed, attempting to recreate");

                // Try to recreate the missing marker
                if (targetMarkerPrefab != null)
                {
                    CreateSingleMarker(targetName);
                    if (allTargetMarkers.TryGetValue(targetName, out marker) && marker != null)
                    {
                        marker.SetActive(true);
                        currentActiveMarker = marker;
                        if (showDebugLogs)
                            Debug.Log($"[TargetMarkerManager] Successfully recreated marker for: {targetName}");
                    }
                }
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning($"[TargetMarkerManager] No marker found to show for: {targetName}, attempting to create it");

            // Try to create the missing marker
            if (targetMarkerPrefab != null)
            {
                CreateSingleMarker(targetName);
                if (allTargetMarkers.TryGetValue(targetName, out marker) && marker != null)
                {
                    marker.SetActive(true);
                    currentActiveMarker = marker;
                    if (showDebugLogs)
                        Debug.Log($"[TargetMarkerManager] Successfully created and showing marker for: {targetName}");
                }
            }
            else
            {
                Debug.LogError($"[TargetMarkerManager] Cannot create marker for {targetName} - targetMarkerPrefab not assigned");
            }
        }
    }

    /// <summary>
    /// Hide all target markers
    /// </summary>
    public void HideAllMarkers()
    {
        // Collect keys of null markers to remove them
        var keysToRemove = new List<string>();

        foreach (var kvp in allTargetMarkers)
        {
            if (kvp.Value != null)
            {
                kvp.Value.SetActive(false);
            }
            else
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        // Remove null markers from dictionary
        foreach (var key in keysToRemove)
        {
            allTargetMarkers.Remove(key);
            if (showDebugLogs)
                Debug.Log($"[TargetMarkerManager] Removed null marker for: {key}");
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
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Updated TargetHandler that integrates with the enhanced NavigationController
/// This replaces or updates your existing TargetHandler.cs
/// </summary>
public class TargetHandler : MonoBehaviour {

    [Header("References")]
    public NavigationController navigationController;
    public TextAsset targetDataFile; // Assign TargetData.json here

    [Header("Runtime Data")]
    public List<Target> allTargets = new List<Target>();
    private Target currentSelectedTarget;

    private void Start() {
        // Find NavigationController if not assigned
        if (navigationController == null) {
            navigationController = FindObjectOfType<NavigationController>();
        }

        // Load targets from JSON
        LoadTargetsFromJSON();

        // Instantiate target GameObjects in the scene (optional)
        // InstantiateTargets();
    }

    /// <summary>
    /// Load targets from TargetData.json
    /// </summary>
    private void LoadTargetsFromJSON() {
        if (targetDataFile == null) {
            Debug.LogError("TargetData.json file not assigned!");
            return;
        }

        try {
            TargetWrapper wrapper = JsonUtility.FromJson<TargetWrapper>(targetDataFile.text);
            allTargets = new List<Target>(wrapper.TargetList);
            Debug.Log($"Loaded {allTargets.Count} targets from JSON");
        } catch (System.Exception e) {
            Debug.LogError($"Failed to load targets: {e.Message}");
        }
    }

    /// <summary>
    /// Navigate to a target by name
    /// </summary>
    public void NavigateToTarget(string targetName) {
        Target target = GetTargetByName(targetName);
        
        if (target == null) {
            Debug.LogError($"Target '{targetName}' not found!");
            if (navigationController != null) {
                navigationController.OnNavigationError?.Invoke($"Target '{targetName}' not found in database.");
            }
            return;
        }

        NavigateToTarget(target);
    }

    /// <summary>
    /// Navigate to a target object
    /// </summary>
    public void NavigateToTarget(Target target) {
        if (target == null) {
            Debug.LogError("Cannot navigate to null target!");
            return;
        }

        if (navigationController == null) {
            Debug.LogError("NavigationController not found!");
            return;
        }

        currentSelectedTarget = target;

        // Start navigation with enhanced features
        navigationController.StartNavigation(
            target.Position,
            target.FloorNumber,
            target.Name
        );

        Debug.Log($"Started navigation to {target.Name} on floor {target.FloorNumber}");
    }

    /// <summary>
    /// Stop current navigation
    /// </summary>
    public void StopNavigation() {
        if (navigationController != null) {
            navigationController.StopNavigation();
            currentSelectedTarget = null;
        }
    }

    /// <summary>
    /// Get target by name
    /// </summary>
    public Target GetTargetByName(string name) {
        return allTargets.Find(t => t.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get all targets on a specific floor
    /// </summary>
    public List<Target> GetTargetsOnFloor(int floorNumber) {
        return allTargets.FindAll(t => t.FloorNumber == floorNumber);
    }

    /// <summary>
    /// Get nearest target to a position
    /// </summary>
    public Target GetNearestTarget(Vector3 position, int? floorFilter = null) {
        Target nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (Target target in allTargets) {
            // Filter by floor if specified
            if (floorFilter.HasValue && target.FloorNumber != floorFilter.Value) {
                continue;
            }

            float distance = Vector3.Distance(position, target.Position);
            if (distance < minDistance) {
                minDistance = distance;
                nearest = target;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Search targets by partial name match
    /// </summary>
    public List<Target> SearchTargets(string searchQuery) {
        if (string.IsNullOrEmpty(searchQuery)) {
            return new List<Target>(allTargets);
        }

        return allTargets.FindAll(t => 
            t.Name.IndexOf(searchQuery, System.StringComparison.OrdinalIgnoreCase) >= 0
        );
    }

    /// <summary>
    /// Optional: Instantiate target prefabs in the scene for visualization
    /// </summary>
    private void InstantiateTargets() {
        // Only if you want physical GameObjects for each target
        GameObject targetPrefab = Resources.Load<GameObject>("TargetPrefab");
        
        if (targetPrefab == null) {
            Debug.LogWarning("TargetPrefab not found in Resources folder. Skipping instantiation.");
            return;
        }

        foreach (Target target in allTargets) {
            GameObject targetObj = Instantiate(targetPrefab);
            targetObj.name = $"Target - {target.Name}";
            targetObj.transform.position = target.Position;
            targetObj.transform.rotation = Quaternion.Euler(target.Rotation);

            // Parent to correct floor
            Transform parentFloor = GetFloorParent(target.FloorNumber);
            if (parentFloor != null) {
                targetObj.transform.SetParent(parentFloor, true);
            }
        }

        Debug.Log($"Instantiated {allTargets.Count} target GameObjects");
    }

    /// <summary>
    /// Get the parent transform for a specific floor
    /// </summary>
    private Transform GetFloorParent(int floorNumber) {
        // Adjust these names to match your hierarchy
        switch (floorNumber) {
            case 0:
                GameObject firstFloor = GameObject.Find("FirstFloor-NavigationTargets");
                return firstFloor != null ? firstFloor.transform : null;
            case 1:
                GameObject secondFloor = GameObject.Find("SecondFloor-NavigationTargets");
                return secondFloor != null ? secondFloor.transform : null;
            default:
                return null;
        }
    }

    /// <summary>
    /// Get current selected target
    /// </summary>
    public Target GetCurrentTarget() {
        return currentSelectedTarget;
    }

    /// <summary>
    /// Check if currently navigating
    /// </summary>
    public bool IsNavigating() {
        return navigationController != null && navigationController.IsNavigating;
    }

    // Debug: Show all targets in Scene view
    private void OnDrawGizmos() {
        if (allTargets == null || allTargets.Count == 0) return;

        foreach (Target target in allTargets) {
            // Different colors for different floors
            Gizmos.color = target.FloorNumber == 0 ? Color.cyan : Color.magenta;
            Gizmos.DrawWireSphere(target.Position, 0.3f);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(
                target.Position + Vector3.up * 0.5f,
                $"{target.Name}\nFloor {target.FloorNumber}"
            );
            #endif
        }
    }
}

// Extension method for easier UI integration
public static class TargetHandlerExtensions {
    /// <summary>
    /// Quick method to navigate by button click
    /// </summary>
    public static void NavigateToTargetByButton(this TargetHandler handler, string targetName) {
        handler.NavigateToTarget(targetName);
    }
}
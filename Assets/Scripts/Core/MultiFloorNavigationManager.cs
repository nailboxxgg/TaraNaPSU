using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Handles multi-floor navigation with stairway routing.
/// This extends the basic NavigationController to provide intelligent floor-to-floor pathfinding.
/// </summary>
public class MultiFloorNavigationManager : MonoBehaviour
{
    public static MultiFloorNavigationManager Instance { get; private set; }

    [Header("References")]
    public NavigationController navigationController;
    public AnchorManager anchorManager;
    public TargetManager targetManager;

    [Header("Navigation Settings")]
    public float stairwayArrivalDistance = 2.0f;  // Distance to consider "arrived" at stairway
    public bool showStairwayPrompts = true;

    // Multi-floor navigation state
    private bool isMultiFloorNavigation = false;
    private List<NavigationSegment> navigationPath = new List<NavigationSegment>();
    private int currentSegmentIndex = 0;
    private NavigationSegment currentSegment;

    // Data structures for multi-floor routing
    [System.Serializable]
    public class NavigationSegment
    {
        public string TargetName;           // Target or stairway name
        public Vector3 TargetPosition;      // Target position
        public int Floor;                   // Floor number
        public string BuildingId;           // Building ID
        public bool IsStairwayTransition;   // Is this a stairway segment
        public string StairwayId;           // ID of the stairway (if applicable)
        public string NextFloorBuildingId;  // Next floor's building ID after stairway
    }

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        // Get references if not assigned
        if (navigationController == null)
            navigationController = FindObjectOfType<NavigationController>();
        if (anchorManager == null)
            anchorManager = AnchorManager.Instance;
        if (targetManager == null)
            targetManager = TargetManager.Instance;
    }

    /// <summary>
    /// Begin multi-floor navigation from current position to target
    /// </summary>
    public void BeginMultiFloorNavigation(string targetName, AnchorManager.AnchorData userAnchor)
    {
        if (anchorManager == null || targetManager == null)
        {
            Debug.LogError("[MultiFloorNavigationManager] Missing required managers");
            return;
        }

        // Get target data
        if (!targetManager.TryGetTarget(targetName, out var targetData))
        {
            Debug.LogError($"[MultiFloorNavigationManager] Target not found: {targetName}");
            return;
        }

        // Calculate multi-floor route
        var route = CalculateMultiFloorRoute(userAnchor, targetData);
        if (route == null || route.Count == 0)
        {
            Debug.LogError("[MultiFloorNavigationManager] Could not calculate route to target");
            return;
        }

        // Start multi-floor navigation
        StartMultiFloorNavigation(route, userAnchor);
    }

    /// <summary>
    /// Calculate the complete multi-floor route including stairway transitions
    /// </summary>
    private List<NavigationSegment> CalculateMultiFloorRoute(AnchorManager.AnchorData start, TargetData target)
    {
        var route = new List<NavigationSegment>();

        // Check if we need floor transition
        if (start.Floor == target.FloorNumber && start.BuildingId == GetTargetBuildingId(target))
        {
            // Same floor - direct navigation
            route.Add(new NavigationSegment
            {
                TargetName = target.Name,
                TargetPosition = target.Position.ToVector3(),
                Floor = target.FloorNumber,
                BuildingId = GetTargetBuildingId(target),
                IsStairwayTransition = false
            });
            return route;
        }

        // Multi-floor navigation needed
        var currentFloor = start.Floor;
        var currentBuilding = start.BuildingId;
        var targetBuilding = GetTargetBuildingId(target);
        var targetFloor = target.FloorNumber;

        // Add path to nearest stairway on current floor
        var nearestStair = anchorManager.FindNearestStair(currentBuilding, currentFloor, targetFloor, start.PositionVector);
        if (nearestStair == null)
        {
            Debug.LogError($"[MultiFloorNavigationManager] No stairway found connecting {currentBuilding} floor {currentFloor} to floor {targetFloor}");
            return null;
        }

        // First segment: navigate to stairway
        route.Add(new NavigationSegment
        {
            TargetName = nearestStair.Bottom.AnchorId,
            TargetPosition = nearestStair.Bottom.PositionVector,
            Floor = currentFloor,
            BuildingId = currentBuilding,
            IsStairwayTransition = true,
            StairwayId = nearestStair.Bottom.AnchorId,
            NextFloorBuildingId = targetBuilding
        });

        // If target is on different floor, add segment on target floor
        if (targetFloor != currentFloor)
        {
            route.Add(new NavigationSegment
            {
                TargetName = target.Name,
                TargetPosition = target.Position.ToVector3(),
                Floor = targetFloor,
                BuildingId = targetBuilding,
                IsStairwayTransition = false
            });
        }

        return route;
    }

    /// <summary>
    /// Start executing the multi-floor navigation plan
    /// </summary>
    private void StartMultiFloorNavigation(List<NavigationSegment> route, AnchorManager.AnchorData userAnchor)
    {
        isMultiFloorNavigation = true;
        navigationPath = route;
        currentSegmentIndex = 0;
        currentSegment = navigationPath[currentSegmentIndex];

        Debug.Log($"[MultiFloorNavigationManager] Starting multi-floor navigation with {navigationPath.Count} segments");

        // Start first segment
        ExecuteCurrentSegment(userAnchor);
    }

    /// <summary>
    /// Execute the current navigation segment
    /// </summary>
    private void ExecuteCurrentSegment(AnchorManager.AnchorData userAnchor)
    {
        if (currentSegment == null) return;

        Debug.Log($"[MultiFloorNavigationManager] Executing segment {currentSegmentIndex + 1}/{navigationPath.Count}: {currentSegment.TargetName}");

        if (currentSegment.IsStairwayTransition)
        {
            // Show stairway prompt
            if (showStairwayPrompts)
            {
                ShowStairwayPrompt(currentSegment);
            }
        }

        // Start navigation to current segment target
        // For stairway segments, navigate to the stair anchor directly
        // For final segments, navigate to the actual target
        if (currentSegment.IsStairwayTransition)
        {
            // Navigate to stairway anchor (not target name)
            NavigateToStairway(userAnchor, currentSegment);
        }
        else
        {
            // Navigate to final destination
            navigationController.BeginNavigationToSegment(userAnchor, currentSegment.TargetName);
        }
    }

    /// <summary>
    /// Call this when user arrives at current segment target
    /// </summary>
    public void OnSegmentArrived()
    {
        if (!isMultiFloorNavigation) return;

        Debug.Log($"[MultiFloorNavigationManager] Arrived at segment: {currentSegment.TargetName}");

        // Check if this was a stairway transition
        if (currentSegment.IsStairwayTransition)
        {
            HandleStairwayTransition();
            return;
        }

        // Move to next segment
        currentSegmentIndex++;
        if (currentSegmentIndex < navigationPath.Count)
        {
            currentSegment = navigationPath[currentSegmentIndex];

            // For stairway transitions, we need to update the user's anchor
            var userAnchor = GetUpdatedUserAnchorAfterStairway();
            ExecuteCurrentSegment(userAnchor);
        }
        else
        {
            // Navigation complete
            CompleteMultiFloorNavigation();
        }
    }

    /// <summary>
    /// Handle stairway floor transition with proper floor switching
    /// </summary>
    private void HandleStairwayTransition()
    {
        if (currentSegment == null || !currentSegment.IsStairwayTransition) return;

        Debug.Log($"[MultiFloorNavigationManager] 🚶 Handling stairway transition: {currentSegment.StairwayId}");
        Debug.Log($"[MultiFloorNavigationManager] 📍 User is transitioning from floor {currentSegment.Floor} to next floor");

        // Clean up temporary stairway navigation objects
        CleanupTemporaryObjects();

        // Find the corresponding stair anchor on the next floor
        var stairPair = anchorManager.stairPairs.FirstOrDefault(s =>
            s.Bottom.AnchorId == currentSegment.StairwayId || s.Top.AnchorId == currentSegment.StairwayId);

        if (stairPair == null)
        {
            Debug.LogError($"[MultiFloorNavigationManager] Could not find stair pair for {currentSegment.StairwayId}");
            return;
        }

        // Determine which stair anchor we should use for the next segment
        AnchorManager.AnchorData nextFloorStair = null;
        int targetFloor = -1;

        if (stairPair.Bottom.AnchorId == currentSegment.StairwayId)
        {
            nextFloorStair = stairPair.Top;
            targetFloor = stairPair.Top.Floor;
        }
        else if (stairPair.Top.AnchorId == currentSegment.StairwayId)
        {
            nextFloorStair = stairPair.Bottom;
            targetFloor = stairPair.Bottom.Floor;
        }

        if (nextFloorStair == null)
        {
            Debug.LogError("[MultiFloorNavigationManager] Could not determine next floor stair anchor");
            return;
        }

        Debug.Log($"[MultiFloorNavigationManager] 🏗 Switching to floor {targetFloor} - {nextFloorStair.AnchorId}");

        // Force NavMesh switch to the new floor BEFORE starting navigation
        navigationController.SwitchToNavMeshFor(nextFloorStair.BuildingId, targetFloor);

        // Small delay to ensure NavMesh switch is complete
        StartCoroutine(DelayedFloorTransition(nextFloorStair));
    }

    /// <summary>
    /// Delayed floor transition to ensure NavMesh switching is complete
    /// </summary>
    private System.Collections.IEnumerator DelayedFloorTransition(AnchorManager.AnchorData nextFloorStair)
    {
        yield return new WaitForSeconds(0.2f);

        Debug.Log($"[MultiFloorNavigationManager] ✅ Floor switch complete - now on floor {nextFloorStair.Floor}");

        // Update user's position to the next floor stair anchor
        var updatedAnchor = new AnchorManager.AnchorData
        {
            AnchorId = nextFloorStair.AnchorId,
            BuildingId = nextFloorStair.BuildingId,
            Floor = nextFloorStair.Floor,
            Position = nextFloorStair.Position,
            Rotation = nextFloorStair.Rotation,
            Type = "anchor",
            Meta = "User position after stairway transition"
        };

        // Move to next segment
        currentSegmentIndex++;
        if (currentSegmentIndex < navigationPath.Count)
        {
            currentSegment = navigationPath[currentSegmentIndex];
            Debug.Log($"[MultiFloorNavigationManager] 🎯 Starting segment {currentSegmentIndex + 1}/{navigationPath.Count}: {currentSegment.TargetName}");
            ExecuteCurrentSegment(updatedAnchor);
        }
        else
        {
            CompleteMultiFloorNavigation();
        }
    }

    /// <summary>
    /// Clean up temporary navigation objects
    /// </summary>
    private void CleanupTemporaryObjects()
    {
        // Clean up temporary stairway navigation objects
        var stairwayPins = GameObject.FindGameObjectsWithTag("Untagged").Where(g => g.name.StartsWith("StairwayPin-") || g.name.StartsWith("StairwayTarget-")).ToArray();
        foreach (var pin in stairwayPins)
        {
            if (pin != null && pin != navigationController.activeTargetPin)
            {
                Destroy(pin);
            }
        }
    }

    /// <summary>
    /// Get updated user anchor after stairway transition (simulation)
    /// </summary>
    private AnchorManager.AnchorData GetUpdatedUserAnchorAfterStairway()
    {
        // In a real implementation, this would get the user's actual position
        // after they've climbed the stairs. For now, we'll use the stair anchor.
        if (currentSegment.IsStairwayTransition && !string.IsNullOrEmpty(currentSegment.StairwayId))
        {
            var stairPair = anchorManager.stairPairs.FirstOrDefault(s =>
                s.Bottom.AnchorId == currentSegment.StairwayId || s.Top.AnchorId == currentSegment.StairwayId);

            if (stairPair != null)
            {
                // Return the appropriate stair anchor based on direction
                if (stairPair.Bottom.AnchorId == currentSegment.StairwayId)
                    return stairPair.Top;
                else
                    return stairPair.Bottom;
            }
        }

        // Fallback - return current anchor
        return navigationController.startAnchor;
    }

    /// <summary>
    /// Navigate directly to stairway location
    /// </summary>
    private void NavigateToStairway(AnchorManager.AnchorData userAnchor, NavigationSegment segment)
    {
        Debug.Log($"[MultiFloorNavigationManager] Navigating to stairway: {segment.StairwayId}");

        // Find the stairway anchor data
        var stairAnchor = anchorManager.FindAnchor(segment.StairwayId);
        if (stairAnchor == null)
        {
            Debug.LogError($"[MultiFloorNavigationManager] Stairway anchor not found: {segment.StairwayId}");
            return;
        }

        // Create a temporary target at stairway location for navigation
        // We'll use the stairway's position directly
        var stairwayPosition = stairAnchor.PositionVector;

        // Create a temporary GameObject at stairway position for navigation
        GameObject stairwayTarget = new GameObject($"StairwayTarget-{segment.StairwayId}");
        stairwayTarget.transform.position = stairwayPosition;
        stairwayTarget.transform.SetParent(transform);

        // Use NavigationController to navigate to stairway position
        // We'll create a custom navigation call that bypasses target marker system
        NavigateToPosition(userAnchor, stairwayPosition, segment.StairwayId);
    }

    /// <summary>
    /// Navigate to a specific position (used for stairways)
    /// </summary>
    private void NavigateToPosition(AnchorManager.AnchorData userAnchor, Vector3 targetPosition, string targetName)
    {
        // Switch to appropriate NavMesh for this floor
        navigationController.SwitchToNavMeshFor(userAnchor.BuildingId, userAnchor.Floor);

        // Create a temporary target pin at stairway position
        if (navigationController.activeTargetPin != null)
        {
            Destroy(navigationController.activeTargetPin);
        }

        GameObject stairwayPin = new GameObject($"StairwayPin-{targetName}");
        stairwayPin.transform.position = targetPosition;

        // Add visual marker for stairway (optional)
        if (navigationController.targetPinPrefab != null)
        {
            var pin = Instantiate(navigationController.targetPinPrefab, targetPosition, Quaternion.identity);
            pin.name = $"StairwayPin-{targetName}";
            navigationController.activeTargetPin = pin;
        }
        else
        {
            navigationController.activeTargetPin = stairwayPin;
        }

        // Calculate and draw path to stairway position
        Vector3 snappedStart = navigationController.SampleNavMeshPosition(userAnchor.PositionVector, 2f, fallback: userAnchor.PositionVector);
        Vector3 snappedTarget = navigationController.SampleNavMeshPosition(targetPosition, 2f, fallback: targetPosition);

        // Calculate path using NavigationController's internal method
        if (navigationController.agent != null && navigationController.agent.isOnNavMesh)
        {
            // Use the same path calculation logic as NavigationController
            int currentFloorArea = navigationController.GetCurrentNavMeshArea();
            var navPath = new UnityEngine.AI.NavMeshPath();
            UnityEngine.AI.NavMesh.CalculatePath(snappedStart, snappedTarget, currentFloorArea, navPath);

            // Update NavigationController's path and draw it
            navigationController.UpdateCornersFromPath(navPath);
            navigationController.DrawPath();
        }
        else
        {
            Debug.LogWarning("[MultiFloorNavigationManager] Cannot calculate path - NavigationController agent is not available");
        }

        // Start navigation to stairway position
        navigationController.navigating = true;
        navigationController.hasArrived = false;
        navigationController.startAnchor = userAnchor;

        // Show navigation elements
        if (navigationController.activeArrow != null)
            navigationController.activeArrow.SetActive(navigationController.showArrowByDefault);
        if (navigationController.lineRenderer != null)
            navigationController.lineRenderer.enabled = navigationController.showLineByDefault;

        Debug.Log($"[MultiFloorNavigationManager] Navigation started to {targetName} at {targetPosition}");
    }

    /// <summary>
    /// Show prompt for stairway navigation
    /// </summary>
    private void ShowStairwayPrompt(NavigationSegment segment)
    {
        // You can integrate this with existing StairPromptUI or create custom UI
        Debug.Log($"📍 Please take the stairs to {(segment.NextFloorBuildingId == segment.BuildingId ? "next floor" : segment.NextFloorBuildingId)} - {segment.StairwayId}");

        // You could show a UI panel here with instructions
    }

    /// <summary>
    /// Complete multi-floor navigation
    /// </summary>
    private void CompleteMultiFloorNavigation()
    {
        isMultiFloorNavigation = false;
        navigationPath.Clear();
        currentSegmentIndex = 0;
        currentSegment = null;

        Debug.Log("[MultiFloorNavigationManager] Multi-floor navigation completed!");
    }

    /// <summary>
    /// Stop multi-floor navigation
    /// </summary>
    public void StopMultiFloorNavigation()
    {
        if (isMultiFloorNavigation)
        {
            CompleteMultiFloorNavigation();
            navigationController.StopNavigation();
        }
    }

    /// <summary>
    /// Helper method to get building ID from target
    /// </summary>
    private string GetTargetBuildingId(TargetData target)
    {
        // Use naming convention to determine building
        if (target.Name.ToLower().Contains("b1")) return "B1";
        if (target.Name.ToLower().Contains("b2")) return "B2";
        if (target.Name.ToLower().Contains("b3")) return "B3";

        // Alternative: You could add a BuildingId field to TargetData
        // For now, return a default
        return "B1"; // Default - you should enhance this logic based on your data
    }

    /// <summary>
    /// Check if multi-floor navigation is active
    /// </summary>
    public bool IsMultiFloorNavigationActive => isMultiFloorNavigation;

    /// <summary>
    /// Get current navigation segment info
    /// </summary>
    public NavigationSegment GetCurrentSegment => currentSegment;

    /// <summary>
    /// Get total number of segments in current navigation
    /// </summary>
    public int GetTotalSegments => navigationPath.Count;

    /// <summary>
    /// Get current segment index (1-based)
    /// </summary>
    public int GetCurrentSegmentNumber => currentSegmentIndex + 1;
}
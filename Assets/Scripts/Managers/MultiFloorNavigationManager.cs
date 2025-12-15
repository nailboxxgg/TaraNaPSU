using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Handles multi-floor and multi-building navigation.
/// Extends basic NavigationController to provide intelligent routing across:
/// 1. Floors (via Stairs)
/// 2. Buildings (via Entrances/Connectors)
/// </summary>
public class MultiFloorNavigationManager : MonoBehaviour
{
    public static MultiFloorNavigationManager Instance { get; private set; }

    [Header("References")]
    public NavigationController navigationController;
    public AnchorManager anchorManager;
    public TargetManager targetManager;
    public StairPromptUI stairPromptUI;

    [Header("Navigation Settings")]
    public float stairwayArrivalDistance = 2.0f;  // Distance to consider "arrived" at portal
    public bool showStairwayPrompts = true;

    // Multi-floor navigation state
    private bool isMultiFloorNavigation = false;
    private List<NavigationSegment> navigationPath = new List<NavigationSegment>();
    private int currentSegmentIndex = 0;
    private NavigationSegment currentSegment;

    // Data structures for routing
    [System.Serializable]
    public class NavigationSegment
    {
        public string TargetName;           // Target or portal name
        public Vector3 TargetPosition;      // Target position
        public int Floor;                   // Floor number
        public string BuildingId;           // Building ID
        public bool IsPortalTransition;     // Is this a portal (stair/entrance) segment
        public string PortalId;             // ID of the portal
        public string NextContextId;        // ID of the context we are switching TO (Building or Floor)
    }

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        if (navigationController == null) navigationController = FindObjectOfType<NavigationController>();
        if (anchorManager == null) anchorManager = AnchorManager.Instance;
        if (targetManager == null) targetManager = TargetManager.Instance;
        if (stairPromptUI == null) stairPromptUI = FindObjectOfType<StairPromptUI>();
    }

    /// <summary>
    /// Begin multi-step navigation from current position to target
    /// </summary>
    public void BeginMultiFloorNavigation(string targetName, AnchorManager.AnchorData userAnchor)
    {
        if (anchorManager == null || targetManager == null) return;

        // Get target data
        if (!targetManager.TryGetTarget(targetName, out var targetData))
        {
            Debug.LogError($"[MultiFloorNavigationManager] Target not found: {targetName}");
            return;
        }

        // Calculate route
        var route = CalculateRouteRecursive(userAnchor.BuildingId, userAnchor.Floor, userAnchor.PositionVector, targetData);

        if (route == null || route.Count == 0)
        {
            Debug.LogError("[MultiFloorNavigationManager] Could not calculate route to target");
            return;
        }

        // Start navigation
        StartNavigationSequence(route, userAnchor);
    }

    /// <summary>
    /// Calculates a route that can handle Building Jump -> Floor Jump -> Final Target
    /// </summary>
    private List<NavigationSegment> CalculateRouteRecursive(string currentBuilding, int currentFloor, Vector3 currentPos, TargetData target)
    {
        var route = new List<NavigationSegment>();
        string targetBuilding = GetTargetBuildingId(target);
        int targetFloor = target.FloorNumber;

        // 1. Check if we need to switch BUILDINGS first
        if (currentBuilding != targetBuilding)
        {
            // Find a connector from Current -> Target
            var connector = anchorManager.FindBestConnector(currentBuilding, targetBuilding, currentPos);
            
            if (connector != null)
            {
                // Determine which node is the "Entry" (on our side)
                var entryNode = (connector.NodeA.BuildingId == currentBuilding) ? connector.NodeA : connector.NodeB;
                
                // Add segment to walk to the Entrance
                route.Add(new NavigationSegment
                {
                    TargetName = entryNode.AnchorId,
                    TargetPosition = entryNode.PositionVector,
                    Floor = currentFloor,
                    BuildingId = currentBuilding,
                    IsPortalTransition = true,
                    PortalId = entryNode.AnchorId, 
                    NextContextId = targetBuilding // Just for debug/info
                });

                // Determine "Exit" node (where we land after transition)
                var exitNode = (entryNode == connector.NodeA) ? connector.NodeB : connector.NodeA;

                // Recursively calculate remaining path from the new building context
                var remainingRoute = CalculateRouteRecursive(exitNode.BuildingId, exitNode.Floor, exitNode.PositionVector, target);
                if (remainingRoute != null) route.AddRange(remainingRoute);
                
                return route;
            }
            else
            {
                Debug.LogError($"[MultiFloorNavigationManager] No connector found from {currentBuilding} to {targetBuilding}");
                return null;
            }
        }

        // 2. We are in the correct Building. Check if we need to switch FLOORS.
        if (currentFloor != targetFloor)
        {
            var stair = anchorManager.FindNearestStair(currentBuilding, currentFloor, targetFloor, currentPos);
            if (stair != null)
            {
                // Add segment to walk to Stair
                route.Add(new NavigationSegment
                {
                    TargetName = stair.Bottom.AnchorId, // Assuming we walk to the one on our floor. We need simple logic here.
                    TargetPosition = (stair.Bottom.Floor == currentFloor) ? stair.Bottom.PositionVector : stair.Top.PositionVector,
                    Floor = currentFloor,
                    BuildingId = currentBuilding,
                    IsPortalTransition = true,
                    PortalId = (stair.Bottom.Floor == currentFloor) ? stair.Bottom.AnchorId : stair.Top.AnchorId,
                    NextContextId = targetFloor.ToString()
                });

                // Recursively calculate from next floor
                // Assuming we land on the other side of stiar
                int nextFloor = targetFloor; // Stairs usually connect directly or we assume simple steps.
                var exitStair = (stair.Bottom.Floor == currentFloor) ? stair.Top : stair.Bottom;
                
                var remainingRoute = CalculateRouteRecursive(currentBuilding, exitStair.Floor, exitStair.PositionVector, target);
                if (remainingRoute != null) route.AddRange(remainingRoute);

                return route;
            }
            else
            {
                Debug.LogError($"[MultiFloorNavigationManager] No stair found in {currentBuilding} from {currentFloor} to {targetFloor}");
                return null;
            }
        }

        // 3. Same Building, Same Floor -> Direct Path
        route.Add(new NavigationSegment
        {
            TargetName = target.Name,
            TargetPosition = target.Position.ToVector3(),
            Floor = targetFloor,
            BuildingId = targetBuilding,
            IsPortalTransition = false
        });

        return route;
    }

    // --- Execution Logic ---

    private void StartNavigationSequence(List<NavigationSegment> route, AnchorManager.AnchorData userAnchor)
    {
        isMultiFloorNavigation = true;
        navigationPath = route;
        currentSegmentIndex = 0;
        currentSegment = navigationPath[currentSegmentIndex];

        Debug.Log($"[MultiFloorNavigationManager] Starting sequence with {navigationPath.Count} segments");
        ExecuteCurrentSegment(userAnchor);
    }

    private void ExecuteCurrentSegment(AnchorManager.AnchorData userAnchor)
    {
        if (currentSegment == null) return;

        Debug.Log($"[MultiFloorNavigationManager] Executing segment {currentSegmentIndex + 1}: Go to {currentSegment.TargetName}");

        if (currentSegment.IsPortalTransition && showStairwayPrompts)
        {
             stairPromptUI.ShowMessage($"Head to {currentSegment.TargetName}", 5.0f);
        }

        if (currentSegment.IsPortalTransition)
        {
            // Navigate to Portal (Coordinate based)
            NavigateToPortal(userAnchor, currentSegment);
        }
        else
        {
            // Navigate to Final Target (Name based)
            navigationController.BeginNavigationToSegment(userAnchor, currentSegment.TargetName);
        }
    }

    public void OnSegmentArrived()
    {
        if (!isMultiFloorNavigation) return;
        Debug.Log($"[MultiFloorNavigationManager] Arrived at: {currentSegment.TargetName}");

        if (currentSegment.IsPortalTransition)
        {
            HandlePortalTransition();
            return;
        }

        // Next segment
        currentSegmentIndex++;
        if (currentSegmentIndex < navigationPath.Count)
        {
            currentSegment = navigationPath[currentSegmentIndex];
            // Since we just arrived at a non-portal (unlikely in this logic flow unless paths are chained),
            // usually "Arrived" at final target stops everything.
            // But if we had waypoints, we'd continue.
            // For now, if we arrive at a non-portal segment, it means we are DONE.
            CompleteMultiFloorNavigation();
        }
        else
        {
            CompleteMultiFloorNavigation();
        }
    }

    private void HandlePortalTransition()
    {
        // We arrived at a door/stair. We need to "Teleport" context to the other side.
        string portalId = currentSegment.PortalId;
        
        Debug.Log($"[MultiFloorNavigationManager] Handling transition through {portalId}");

        // 1. Is it a Building Connector?
        var connector = anchorManager.connections.FirstOrDefault(c => c.NodeA.AnchorId == portalId || c.NodeB.AnchorId == portalId);
        if (connector != null)
        {
            // Transition to other building
            var exitNode = (connector.NodeA.AnchorId == portalId) ? connector.NodeB : connector.NodeA;
            TransitionToNewContext(exitNode);
            return;
        }

        // 2. Is it a Stair?
        var stairPair = anchorManager.stairPairs.FirstOrDefault(s => s.Bottom.AnchorId == portalId || s.Top.AnchorId == portalId);
        if (stairPair != null)
        {
            var exitNode = (stairPair.Bottom.AnchorId == portalId) ? stairPair.Top : stairPair.Bottom;
            TransitionToNewContext(exitNode);
            return;
        }

        Debug.LogError($"[MultiFloorNavigationManager] Unknown portal ID: {portalId}");
    }

    private void TransitionToNewContext(AnchorManager.AnchorData exitNode)
    {
        Debug.Log($"[MultiFloorNavigationManager] 🌀 Teleporting context to {exitNode.BuildingId} Floor {exitNode.Floor}");
        
        // Show UI
        if (stairPromptUI != null) stairPromptUI.ShowMessage($"Entering {exitNode.BuildingId} Floor {exitNode.Floor}...", 2.0f);

        // Switch NavMesh
        navigationController.SwitchToNavMeshFor(exitNode.BuildingId, exitNode.Floor);

        // Wait a frame then start next segment
        StartCoroutine(DelayedTransitionRoutine(exitNode));
    }

    private System.Collections.IEnumerator DelayedTransitionRoutine(AnchorManager.AnchorData exitNode)
    {
        yield return new WaitForSeconds(0.2f); // stabilization

        currentSegmentIndex++;
        if (currentSegmentIndex < navigationPath.Count)
        {
            currentSegment = navigationPath[currentSegmentIndex];
            
            // We simulate that the user is now AT the exit node
            ExecuteCurrentSegment(exitNode);
        }
        else
        {
            CompleteMultiFloorNavigation();
        }
    }

    // --- Helpers ---

    private void NavigateToPortal(AnchorManager.AnchorData userAnchor, NavigationSegment segment)
    {
        // Similar to NavigateToStairway but generic
        navigationController.SwitchToNavMeshFor(userAnchor.BuildingId, userAnchor.Floor);
        
        // Setup temporary target pin
        if (navigationController.activeTargetPin != null) Destroy(navigationController.activeTargetPin);
        
        GameObject pin = new GameObject($"PortalPin-{segment.TargetName}");
        pin.transform.position = segment.TargetPosition;
        if (navigationController.targetPinPrefab != null)
        {
            var vis = Instantiate(navigationController.targetPinPrefab, segment.TargetPosition, Quaternion.identity);
            navigationController.activeTargetPin = vis;
        }
        else
        {
            navigationController.activeTargetPin = pin;
        }

        // Calc path
        Vector3 startPos = navigationController.SampleNavMeshPosition(userAnchor.PositionVector);
        Vector3 targetPos = navigationController.SampleNavMeshPosition(segment.TargetPosition);
        
        UnityEngine.AI.NavMeshPath p = new UnityEngine.AI.NavMeshPath();
        UnityEngine.AI.NavMesh.CalculatePath(startPos, targetPos, navigationController.GetCurrentNavMeshArea(), p);
        
        navigationController.UpdateCornersFromPath(p);
        navigationController.DrawPath();

        navigationController.navigating = true;
        navigationController.hasArrived = false;
        navigationController.startAnchor = userAnchor;
        
        // Show arrow
        if (navigationController.activeArrow) navigationController.activeArrow.SetActive(navigationController.showArrowByDefault);
    }

    private void CompleteMultiFloorNavigation()
    {
        isMultiFloorNavigation = false;
        navigationPath.Clear();
        Debug.Log("[MultiFloorNavigationManager] Navigation Complete");
    }

    public void StopMultiFloorNavigation()
    {
        CompleteMultiFloorNavigation();
        navigationController.StopNavigation();
    }

    private string GetTargetBuildingId(TargetData target)
    {
        string name = target.Name.ToLower();
        if (name.Contains("b1")) return "B1";
        if (name.Contains("b2")) return "B2";
        if (name.Contains("b3")) return "B3";
        if (name.Contains("campus") || name.Contains("gate") || name.Contains("guard") || name.Contains("canteen")) return "Campus";
        
        return "B1"; // fallback
    }

    public bool IsMultiFloorNavigationActive => isMultiFloorNavigation;
    public NavigationSegment GetCurrentSegment => currentSegment;
}

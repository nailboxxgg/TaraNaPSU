using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// AR-friendly NavigationController:
/// - Agent used for path calculations only (agent.updatePosition = false)
/// - Uses AR camera position as source (user position)
/// - Draws path with LineRenderer and rotates an in-front arrow to point to next waypoint
/// - Snaps anchors/targets to nearest NavMesh to avoid cross-floor snapping
/// - BeginNavigation(AnchorManager.AnchorData start, string targetName) API
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class NavigationController : MonoBehaviour
{
    public static NavigationController Instance { get; private set; }

    [Header("Core references")]
    public NavMeshAgent agent;                // NavMeshAgent used for path calculation only
    public Transform arCamera;                // AR camera / XR origin camera transform
    public LineRenderer lineRenderer;         // draws path
    public GameObject arrowPrefab;            // optional 3D arrow prefab (Option A behaviour)
    public GameObject targetPinPrefab;        // optional target pin prefab
    public NavigationStatusController statusController; // shows Status/Building/Target
    public ARSession arSession;              // AR Session for reset functionality
    public GameObject arSessionOrigin;          // AR Session Origin for position offset
    public MultiFloorNavigationManager multiFloorManager; // Multi-floor navigation manager
    public ARCompatibilityManager compatibilityManager; // AR compatibility manager

    [Header("Behavior")]
    public float arriveDistance = 1.0f;       // when user is within this distance — arrived
    public float arrowDistanceFromCamera = 1.5f; // position arrow this far in front of camera
    public float arrowSlerpSpeed = 10f;       // arrow rotation smoothing

    [Header("Navigation Visualization Settings")]
    public bool showArrowByDefault = false;   // 3D Arrow visibility (default: false)
    public bool showLineByDefault = true;     // Navigation Line visibility (default: true)
    

    [Header("Optional: Multi-floor NavMeshSurfaces")]
    public List<NavMeshSurface> navMeshSurfaces = new List<NavMeshSurface>();
    // Use SwitchToNavMeshFor(buildingId, floor) to enable appropriate surface(s)

    // Public state
    public bool IsNavigating => navigating;

    // Public properties for navigation visualization
    public bool ShowArrowByDefault => showArrowByDefault;
    public bool ShowLineByDefault => showLineByDefault;

    // Internal
    private NavMeshPath navPath;
    public bool navigating = false;   // Made public for MultiFloorNavigationManager access
    public bool hasArrived = false;  // Made public for MultiFloorNavigationManager access

    public AnchorManager.AnchorData startAnchor;
    public TargetData targetData;

    public GameObject activeTargetPin;  // Made public for MultiFloorNavigationManager access
    public GameObject activeArrow;     // Made public for MultiFloorNavigationManager access
    public GameObject ActiveArrow => activeArrow;
    public Transform target;


    private List<Vector3> currentCorners = new List<Vector3>();

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        // Get compatibility manager
        if (compatibilityManager == null)
            compatibilityManager = FindObjectOfType<ARCompatibilityManager>();

        if (agent == null)
        {
            Debug.LogError("[NavigationController] Assign a NavMeshAgent in Inspector.");
            return;
        }
        if (arCamera == null && Camera.main != null) arCamera = Camera.main.transform;

        // Agent must not drive the camera - use it as a path calculator only
        if (agent != null)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;

            // Delay agent placement to ensure NavMesh surfaces are ready
            StartCoroutine(DelayedAgentPlacement());
        }

        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        navPath = new NavMeshPath();

        // Instantiate arrow now (if provided)
        if (arrowPrefab != null)
        {
            activeArrow = Instantiate(arrowPrefab);
            activeArrow.SetActive(showArrowByDefault);
        }

        // Initialize line renderer state
        if (lineRenderer != null)
        {
            lineRenderer.enabled = showLineByDefault;
        }
    }

    /// <summary>
    /// Delay agent placement to ensure NavMesh surfaces are fully initialized
    /// </summary>
    private System.Collections.IEnumerator DelayedAgentPlacement()
    {
        // Wait a few frames for NavMesh surfaces to be ready
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        if (agent == null) yield break;

        // Try multiple attempts with increasing search radius
        float[] searchRadii = { 2.0f, 5.0f, 10.0f, 20.0f };
        bool placed = false;

        for (int i = 0; i < searchRadii.Length && !placed; i++)
        {
            Vector3 agentPos = arCamera != null ? arCamera.position : Vector3.zero;

            if (NavMesh.SamplePosition(agentPos, out NavMeshHit hit, searchRadii[i], NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                Debug.Log($"[NavigationController] Agent placed on NavMesh at: {hit.position} (search radius: {searchRadii[i]})");
                placed = true;
            }
            else
            {
                Debug.LogWarning($"[NavigationController] Could not place agent on NavMesh with radius {searchRadii[i]}");
            }
        }

        if (!placed)
        {
            Debug.LogError("[NavigationController] Failed to place agent on NavMesh after multiple attempts. Navigation may not work properly.");
        }

        // Set flag to indicate agent is ready
        agentReady = placed;
    }

    /// <summary>
    /// Check if agent is ready for navigation operations
    /// </summary>
    public bool IsAgentReady()
    {
        return agent != null && agent.isOnNavMesh && agentReady;
    }

    // Internal flag to track agent readiness
    private bool agentReady = false;

    [Header("Performance Settings")]
    public float pathCalculationInterval = 0.5f; // Recalculate path every 0.5s
    private float lastPathCalcTime = 0f;

    void Update()
    {
        if (!navigating || targetData == null || arCamera == null) return;

        Vector3 userPos = arCamera.position;

        // Check if activeTargetPin exists before using it
        if (activeTargetPin == null)
        {
            // For multi-floor navigation, it's normal for target pin to be null during stairway segments
            // Only warn if we're not in a multi-floor scenario
            if (multiFloorManager == null || !multiFloorManager.IsMultiFloorNavigationActive)
            {
                Debug.LogWarning("[NavigationController] activeTargetPin is null, skipping path calculation");
                return;
            }
            else
            {
                // Multi-floor navigation active - target pin might be temporarily null during floor transitions
                // Try to get marker from TargetMarkerManager or create temporary one
                if (TargetMarkerManager.Instance != null && targetData != null)
                {
                    // First try to get the marker without calling ShowOnlyTarget to avoid infinite calls
                    activeTargetPin = TargetMarkerManager.Instance.GetCurrentActiveMarker();

                    if (activeTargetPin == null)
                    {
                        // Try to show the target marker
                        TargetMarkerManager.Instance.ShowOnlyTarget(targetData.Name);
                        activeTargetPin = TargetMarkerManager.Instance.GetCurrentActiveMarker();
                    }

                    if (activeTargetPin == null)
                    {
                        Debug.LogWarning("[NavigationController] No marker found for multi-floor target, creating temporary pin");
                        SpawnOrPlaceTargetPin(targetData);
                    }
                }
                else
                {
                    Debug.LogWarning("[NavigationController] Cannot proceed without target pin for multi-floor navigation");
                    return;
                }
            }
        }

        // Double-check that activeTargetPin is not null before using it
        if (activeTargetPin == null)
        {
            Debug.LogWarning("[NavigationController] activeTargetPin is still null after recovery attempts, skipping frame");
            return;
        }

        // --- Performance Optimization: Throttle Path Calculation ---
        if (Time.time - lastPathCalcTime > pathCalculationInterval)
        {
            lastPathCalcTime = Time.time;

            // Recalculate path from user to target with current floor constraint
            int currentFloorArea = GetCurrentNavMeshArea();
            NavMesh.CalculatePath(userPos, activeTargetPin.transform.position, currentFloorArea, navPath);
            UpdateCornersFromPath(navPath);
        }

        // Check if we should hide the navigation line during stair transitions
        bool shouldHideNavLine = ShouldHideNavLineDuringStairTransition();

        if (!shouldHideNavLine)
        {
            DrawPath();
        }
        else
        {
            // Hide the line renderer during stair transition
            if (lineRenderer != null)
                lineRenderer.positionCount = 0;
        }

        UpdateArrow(userPos);

        // Use user distance for arrival detection (not agent.remainingDistance)
        float dist = Vector3.Distance(userPos, activeTargetPin.transform.position);

        if (!hasArrived && dist <= arriveDistance)
        {
            hasArrived = true;
            OnArrived();
        }
    }

    #region Public API

    /// <summary>
    /// Begin navigation to a specific target for multi-floor segments.
    /// This is called by MultiFloorNavigationManager for individual segments.
    /// </summary>
    public void BeginNavigationToSegment(AnchorManager.AnchorData start, string targetName)
    {
        // Force NavMesh switch before starting navigation
        SwitchToNavMeshFor(start.BuildingId, start.Floor);

        // Small delay to ensure NavMesh switch is complete
        StartCoroutine(DelayedNavigationStart(start, targetName));
    }

    private System.Collections.IEnumerator DelayedNavigationStart(AnchorManager.AnchorData start, string targetName)
    {
        yield return new WaitForSeconds(0.1f); // Small delay for NavMesh switch

        // Skip multi-floor check since this is called by the manager
        BeginNavigationInternal(start, targetName, false);
    }

    /// <summary>
    /// Start navigation given a scanned start anchor and a target name from TargetManager.
    /// This will:
    /// - check AR compatibility and bypass if needed
    /// - attempt to snap anchor & target to the nearest NavMesh
    /// - optionally switch NavMesh surfaces (if you have them assigned)
    /// - show only the selected target marker, hide all others
    /// - enable arrow/line drawing
    /// - check if multi-floor navigation is needed and delegate to MultiFloorNavigationManager
    /// </summary>
    public void BeginNavigation(AnchorManager.AnchorData start, string targetName, bool warpAgentToStart = false)
    {
        if (start == null)
        {
            Debug.LogError("[NavigationController] BeginNavigation called with null start.");
            return;
        }

        // Check AR compatibility first
        if (compatibilityManager != null && compatibilityManager.IsARBypassed)
        {
            Debug.LogWarning("[NavigationController] 🚫 AR Bypassed - using compatibility mode");
            BeginCompatibilityNavigation(start, targetName, warpAgentToStart);
            return;
        }

        // Find the target from TargetManager (assumes TargetManager.Instance exists and TryGetTarget)
        if (!TargetManager.Instance.TryGetTarget(targetName, out var td))
        {
            Debug.LogError($"[NavigationController] Target not found: {targetName}");
            return;
        }

        startAnchor = start;
        targetData = td;

        // Check if we need multi-floor navigation
        if (multiFloorManager != null && NeedsMultiFloorNavigation(start, td))
        {
            Debug.Log($"[NavigationController] Multi-floor navigation needed from {start.BuildingId} floor {start.Floor} to {targetName}");
            multiFloorManager.BeginMultiFloorNavigation(targetName, start);
            return;
        }

        // Single floor navigation
        BeginNavigationInternal(start, targetName, warpAgentToStart);
    }

    /// <summary>
    /// Start navigation in compatibility mode (reduced features)
    /// </summary>
    private void BeginCompatibilityNavigation(AnchorManager.AnchorData start, string targetName, bool warpAgentToStart)
    {
        Debug.Log($"[NavigationController] 🔄 Starting COMPATIBILITY navigation to {targetName}");

        // Simplified navigation for problematic devices
        // Skip AR-specific features, use basic navigation
        startAnchor = start;

        if (!TargetManager.Instance.TryGetTarget(targetName, out var td))
        {
            Debug.LogError($"[NavigationController] Target not found: {targetName}");
            return;
        }

        targetData = td;

        // Bypass multi-floor navigation for compatibility mode
        if (multiFloorManager != null && NeedsMultiFloorNavigation(start, td))
        {
            Debug.LogWarning("[NavigationController] ⚠️ Multi-floor navigation disabled in compatibility mode - using direct route");
            // Still try basic navigation but warn user
        }

        // Use simplified navigation without AR features
        BeginNavigationInternal(start, targetName, warpAgentToStart);
    }

    /// <summary>
    /// Internal navigation logic without multi-floor check
    /// </summary>
    private void BeginNavigationInternal(AnchorManager.AnchorData start, string targetName, bool warpAgentToStart)
    {
        // Target data should already be set by caller, but get it to be safe
        if (!TargetManager.Instance.TryGetTarget(targetName, out var td))
        {
            Debug.LogError($"[NavigationController] Target not found: {targetName}");
            return;
        }

        targetData = td;

        // If you use navMeshSurfaces per building/floor, switch to relevant one
        // This is optional; implement matching logic by buildingId/floor stored in AnchorData & surfaces
        SwitchToNavMeshFor(start.BuildingId, start.Floor);

        // Use TargetMarkerManager to show only the selected target marker
        if (TargetMarkerManager.Instance != null)
        {
            TargetMarkerManager.Instance.ShowOnlyTarget(targetName);

            // Try to get the active marker from TargetMarkerManager
            activeTargetPin = TargetMarkerManager.Instance.GetCurrentActiveMarker();

            if (activeTargetPin != null)
            {
                Debug.Log($"[NavigationController] Using existing marker for: {targetName}");
            }
            else
            {
                // Fallback: Create target pin if no marker exists
                Debug.LogWarning($"[NavigationController] No existing marker found for {targetName}, creating fallback pin");
                SpawnOrPlaceTargetPin(targetData);
            }
        }
        else
        {
            // Fallback: Create target pin if TargetMarkerManager is not available
            Debug.LogWarning("[NavigationController] TargetMarkerManager.Instance is null, creating fallback pin");
            SpawnOrPlaceTargetPin(targetData);
        }

        // Final check - ensure activeTargetPin is not null
        if (activeTargetPin == null)
        {
            Debug.LogError($"[NavigationController] Failed to create or find target pin for {targetName}. Navigation cannot proceed.");
            navigating = false;
            return;
        }

        // Snap start and target to NavMesh (prevents snapping to wrong floor)
        Vector3 snappedStart = SampleNavMeshPosition(startAnchor.PositionVector, 2f, fallback: startAnchor.PositionVector);
        Vector3 snappedTarget = SampleNavMeshPosition(activeTargetPin.transform.position, 2f, fallback: activeTargetPin.transform.position);

        // Optionally warp the agent (invisible helper) onto the navmesh near the user/start.
        // We avoid warping the AR camera. Warping the agent is safe (agent is not the camera).
        if (agent != null)
        {
            // If warpAgentToStart is true, attempt to place agent at snappedStart;
            // otherwise ensure agent resides on navmesh near the camera
            Vector3 agentPlace = arCamera != null ? arCamera.position : snappedStart;

            if (warpAgentToStart)
                agentPlace = snappedStart;

            if (NavMesh.SamplePosition(agentPlace, out NavMeshHit agentHit, 2.0f, NavMesh.AllAreas))
                agent.Warp(agentHit.position);
            else
                Debug.LogWarning("[NavigationController] Could not place agent exactly — continuing.");
        }

        // Mark state
        navigating = true;
        hasArrived = false;

        // Show navigation elements based on default settings
        if (activeArrow != null) activeArrow.SetActive(showArrowByDefault);
        if (lineRenderer != null) lineRenderer.enabled = showLineByDefault;

        if (statusController != null)
            statusController.SetNavigationInfo(startAnchor.BuildingId ?? "-", targetData.Name);

        Debug.Log($"[NavigationController] Navigation started from {startAnchor.AnchorId} to {targetData.Name}");
    }

    /// <summary>
    /// Recenter navigation to a new anchor position while keeping the same target.
    /// This handles QR code recentering during active navigation.
    /// </summary>
    public void RecenterNavigation(AnchorManager.AnchorData newAnchor, string targetName)
    {
        if (newAnchor == null)
        {
            Debug.LogError("[NavigationController] RecenterNavigation called with null newAnchor.");
            return;
        }
        if (string.IsNullOrEmpty(targetName))
        {
            Debug.LogError("[NavigationController] RecenterNavigation called with empty targetName.");
            return;
        }

        Debug.Log($"[NavigationController] Recentering navigation from {startAnchor?.AnchorId} to {newAnchor.AnchorId}");

        // Reset AR session to clear all tracking and anchors
        if (arSession != null)
        {
            arSession.Reset();
            Debug.Log("[NavigationController] AR Session reset - all trackables cleared");
        }

        // Apply position offset based on the new anchor location (as shown in transcript)
        if (arSessionOrigin != null)
        {
            Vector3 newOffset = newAnchor.PositionVector;
            arSessionOrigin.transform.position = newOffset;
            Debug.Log($"[NavigationController] AR Session Origin offset updated to: {newOffset}");
        }

        // Update the current anchor
        startAnchor = newAnchor;

        // Switch NavMesh if needed
        SwitchToNavMeshFor(startAnchor.BuildingId, startAnchor.Floor);

        // Get target data for the current destination
        if (!TargetManager.Instance.TryGetTarget(targetName, out var currentTargetData))
        {
            Debug.LogError($"[NavigationController] Target not found for recenter: {targetName}");
            return;
        }
        targetData = currentTargetData;

        // Show only the selected target marker (re-use existing logic)
        if (TargetMarkerManager.Instance != null)
        {
            TargetMarkerManager.Instance.ShowOnlyTarget(targetName);
            activeTargetPin = TargetMarkerManager.Instance.GetCurrentActiveMarker();
        }
        else
        {
            // Fallback: Create target pin if no marker exists
            SpawnOrPlaceTargetPin(targetData);
        }

        // Recalculate path from new anchor position
        Vector3 snappedStart = SampleNavMeshPosition(startAnchor.PositionVector, 2f, fallback: startAnchor.PositionVector);
        Vector3 snappedTarget = SampleNavMeshPosition(activeTargetPin.transform.position, 2f, fallback: activeTargetPin.transform.position);

        // Warp the agent to new start position for path calculation
        if (agent != null)
        {
            Vector3 agentPlace = arCamera != null ? arCamera.position : snappedStart;

            // Try to place agent on NavMesh with increasing search radius
            float[] searchRadii = { 2.0f, 5.0f, 10.0f };
            bool placed = false;

            for (int i = 0; i < searchRadii.Length && !placed; i++)
            {
                if (NavMesh.SamplePosition(agentPlace, out NavMeshHit agentHit, searchRadii[i], NavMesh.AllAreas))
                {
                    agent.Warp(agentHit.position);
                    Debug.Log($"[NavigationController] Agent placed on NavMesh for recentering at: {agentHit.position} (radius: {searchRadii[i]})");
                    placed = true;
                }
            }

            if (!placed)
            {
                Debug.LogWarning("[NavigationController] Could not place agent on NavMesh for recentering. Path calculation may be affected.");
            }
        }

        // Update navigation status
        if (statusController != null)
            statusController.SetNavigationInfo(startAnchor.BuildingId ?? "-", targetData.Name);

        Debug.Log($"[NavigationController] Navigation recentered to {newAnchor.AnchorId} for {targetName}");
    }

    /// <summary>
    /// Stops current navigation and clears visuals.
    /// </summary>
    public void StopNavigation()
    {
        // Stop multi-floor navigation if active
        if (multiFloorManager != null && multiFloorManager.IsMultiFloorNavigationActive)
        {
            multiFloorManager.StopMultiFloorNavigation();
        }

        navigating = false;
        hasArrived = false;

        // Hide all target markers using TargetMarkerManager
        if (TargetMarkerManager.Instance != null)
        {
            TargetMarkerManager.Instance.HideAllMarkers();
        }

        // Only destroy the active target pin if it was created by this controller (fallback case)
        if (activeTargetPin != null && activeTargetPin.name.StartsWith("TargetPin-") &&
            !TargetMarkerManager.Instance.HasMarkerForTarget(activeTargetPin.name.Replace("TargetPin-", "")))
        {
            Destroy(activeTargetPin);
        }
        activeTargetPin = null;

        if (activeArrow != null) activeArrow.SetActive(false);

        lineRenderer.positionCount = 0;

        if (statusController != null)
            statusController.UpdateStatus("Navigation stopped");
    }

    #endregion

    #region Helpers - Path & Visuals

    public void UpdateCornersFromPath(NavMeshPath path)
    {
        currentCorners.Clear();
        if (path == null || path.corners == null || path.corners.Length == 0) return;
        currentCorners.AddRange(path.corners);
    }

    public void DrawPath()
    {
        if (navPath == null || navPath.corners == null || navPath.corners.Length == 0)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        // Raise the path a little so it is visible above the floor (tweak as needed)
        int count = navPath.corners.Length;
        lineRenderer.positionCount = count;
        for (int i = 0; i < count; i++)
        {
            Vector3 p = navPath.corners[i];
            p.y += 0.05f; // small offset
            lineRenderer.SetPosition(i, p);
        }
    }

    private void SpawnOrPlaceTargetPin(TargetData td)
    {
        // Clear old
        if (activeTargetPin != null) Destroy(activeTargetPin);

        Vector3 pos = td.Position.ToVector3();
        Quaternion rot = Quaternion.Euler(td.Rotation.ToVector3());

        if (targetPinPrefab != null)
        {
            activeTargetPin = Instantiate(targetPinPrefab, pos, rot);
            activeTargetPin.name = "TargetPin-" + td.Name;
        }
        else
        {
            activeTargetPin = new GameObject("TargetPin-" + td.Name);
            activeTargetPin.transform.position = pos;
            activeTargetPin.transform.rotation = rot;
        }
    }

    private void UpdateArrow(Vector3 userPos)
    {
        if (activeArrow == null) return;

        // place arrow a fixed distance in front of camera (Option A)
        Vector3 forward = arCamera.forward;
        Vector3 arrowPos = arCamera.position + forward.normalized * arrowDistanceFromCamera;
        activeArrow.transform.position = arrowPos;

        // compute the look target: the next corner of the path, otherwise final target
        Vector3 lookTarget = activeTargetPin != null ? activeTargetPin.transform.position : (currentCorners.Count > 0 ? currentCorners[currentCorners.Count - 1] : arrowPos + forward);
        if (currentCorners.Count > 1)
            lookTarget = currentCorners[Mathf.Min(1, currentCorners.Count - 1)]; // prefer the next corner after current
        else if (currentCorners.Count == 1)
            lookTarget = currentCorners[0];

        // direction to face (ignore vertical tilt so arrow is horizontal)
        Vector3 dir = lookTarget - activeArrow.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = arCamera.forward;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        activeArrow.transform.rotation = Quaternion.Slerp(activeArrow.transform.rotation, targetRot, Time.deltaTime * arrowSlerpSpeed);
    }

    #endregion

    #region NavMesh utilities / floor switching

    /// <summary>
    /// Samples the nearest NavMesh position within maxDistance. Returns fallback if none found.
    /// </summary>
    public Vector3 SampleNavMeshPosition(Vector3 pos, float maxDistance = 2f, Vector3 fallback = default)
    {
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
            return hit.position;
        return fallback == default ? pos : fallback;
    }

    /// <summary>
    /// Optional: enable/disable NavMeshSurface components to switch active NavMesh.
    /// Ensures ONLY ONE floor is visible at any time for clean floor-by-floor navigation.
    /// </summary>
    public void SwitchToNavMeshFor(string buildingId, int floor)
    {
        if (navMeshSurfaces == null || navMeshSurfaces.Count == 0) return;

        Debug.Log($"[NavigationController] Switching to {buildingId} floor {floor}");
        Debug.Log($"[NavigationController] Total NavMesh surfaces: {navMeshSurfaces.Count}");

        // Disable ALL surfaces first (ensure only one active)
        foreach (var s in navMeshSurfaces)
        {
            if (s == null) continue;
            s.enabled = false;
        }

        // Enable only the matching surface
        bool foundMatch = false;
        string matchedSurface = "";

        // Support both naming conventions for flexibility
        string needle1 = (buildingId ?? "") + $"_Floor{floor}";        // B1_Floor0
        string needle2 = $"Floor{floor}_{buildingId}";                     // Floor0_B1
        string needle3 = $"GroundFloor{floor}_{buildingId}";               // GroundFloor0_B1
        string needle4 = $"FirstFloor{floor}_{buildingId}";                 // FirstFloor1_B1

        foreach (var s in navMeshSurfaces)
        {
            if (s == null) continue;

            bool match = s.name.Contains(needle1) ||
                       s.name.Contains(needle2) ||
                       s.name.Contains(needle3) ||
                       s.name.Contains(needle4);

            if (match)
            {
                s.enabled = true;
                foundMatch = true;
                matchedSurface = s.name;
                Debug.Log($"[NavigationController] ✅ ENABLED: {s.name} for {buildingId} floor {floor}");
            }
            else
            {
                Debug.Log($"[NavigationController] ❌ DISABLED: {s.name} (not matching {buildingId} floor {floor})");
            }
        }

        if (!foundMatch)
        {
            Debug.LogError($"[NavigationController] ❌ No navmesh surface matched for {buildingId} floor {floor}");
            Debug.LogError($"[NavigationController] Expected patterns: {needle1}, {needle2}, {needle3}, {needle4}");

            // Fallback: enable first surface as emergency
            if (navMeshSurfaces.Count > 0 && navMeshSurfaces[0] != null)
            {
                navMeshSurfaces[0].enabled = true;
                Debug.LogWarning($"[NavigationController] ⚠️ FALLBACK: Enabled {navMeshSurfaces[0].name}");
            }
        }
        else
        {
            Debug.Log($"[NavigationController] ✅ Successfully switched to {matchedSurface} - only one floor active");
        }
    }

    #endregion

    #region Multi-Floor Navigation Helpers

    /// <summary>
    /// Get the currently active NavMesh area index
    /// </summary>
    public int GetCurrentNavMeshArea()
    {
        // Find which NavMeshSurface is currently active
        if (navMeshSurfaces != null)
        {
            foreach (var surface in navMeshSurfaces)
            {
                if (surface != null && surface.enabled)
                {
                    // Return the area type for this surface
                    // This assumes each floor has a unique area type
                    return GetNavMeshAreaForSurface(surface);
                }
            }
        }

        // Fallback to all areas
        return NavMesh.AllAreas;
    }

    /// <summary>
    /// Get NavMesh area index for a specific surface
    /// </summary>
    private int GetNavMeshAreaForSurface(NavMeshSurface surface)
    {
        // This requires that you set up different areas for each floor
        // You can configure this in Project Settings > Navigation > Areas

        // Fallback: Check surface name to determine area
        if (surface.name.Contains("Floor0") || surface.name.Contains("GroundFloor"))
            return 1; // Ground floor area
        else if (surface.name.Contains("Floor1") || surface.name.Contains("FirstFloor"))
            return 2; // First floor area
        else if (surface.name.Contains("Floor2"))
            return 3; // Second floor area
        else if (surface.name.Contains("Floor3"))
            return 4; // Third floor area
        else if (surface.name.Contains("Floor4"))
            return 5; // Fourth floor area
        else if (surface.name.Contains("Floor5"))
            return 6; // Fifth floor area

        return NavMesh.AllAreas; // Default fallback
    }

    /// <summary>
    /// Check if navigation requires multi-floor routing
    /// </summary>
    private bool NeedsMultiFloorNavigation(AnchorManager.AnchorData start, TargetData target)
    {
        // Get target building - for now, we'll assume targets are in buildings
        // You may need to enhance this based on your target data structure
        string targetBuilding = GetTargetBuildingId(target);

        // Check if different floor OR different building
        return start.Floor != target.FloorNumber || start.BuildingId != targetBuilding;
    }

    /// <summary>
    /// Get building ID for a target - you may need to enhance this based on your data
    /// </summary>
    private string GetTargetBuildingId(TargetData target)
    {
        // This is a simplified approach - you could:
        // 1. Add BuildingId field to TargetData
        // 2. Determine building from position coordinates
        // 3. Use a naming convention in target names

        // For now, let's assume a basic mapping based on position or naming
        if (target.Name.ToLower().Contains("b1")) return "B1";
        if (target.Name.ToLower().Contains("b2")) return "B2";
        if (target.Name.ToLower().Contains("b3")) return "B3";

        // Default fallback - you should customize this logic
        return "B1";
    }

    #endregion

    #region Navigation Line Control

    /// <summary>
    /// Determines if navigation line should be hidden during stair transitions
    /// </summary>
    private bool ShouldHideNavLineDuringStairTransition()
    {
        // If not in multi-floor navigation, always show the line
        if (multiFloorManager == null || !multiFloorManager.IsMultiFloorNavigationActive)
            return false;

        // Get current navigation segment
        var currentSegment = multiFloorManager.GetCurrentSegment;
        if (currentSegment == null)
            return false;

        // Hide navigation line only for stairway transition segments
        // This creates the effect: show line to stairway -> hide during stairs -> show on new floor
        return currentSegment.IsStairwayTransition;
    }

    #endregion

    #region Arrival callback

    private void OnArrived()
    {
        Debug.Log("✅ Arrived at destination!");

        // Check if this is part of multi-floor navigation
        if (multiFloorManager != null && multiFloorManager.IsMultiFloorNavigationActive)
        {
            // Notify multi-floor manager about segment arrival
            multiFloorManager.OnSegmentArrived();
            return;
        }

        // Single-floor navigation complete
        CompleteNavigation();
    }

    /// <summary>
    /// Complete the navigation process
    /// </summary>
    private void CompleteNavigation()
    {
        // freeze visuals
        lineRenderer.positionCount = 0;
        if (activeArrow != null) activeArrow.SetActive(false);

        if (statusController != null)
            statusController.OnArrived();

        // keep navigating = false to stop updating
        navigating = false;
    }

    public void EndNavigation()
    {
        // Stop the NavMeshAgent
        if (agent != null)
        {
            // Only stop agent if it's active and on NavMesh
            if (agent.isOnNavMesh && agent.isActiveAndEnabled)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            else
            {
                Debug.LogWarning("[NavigationController] Agent not properly placed on NavMesh, skipping agent operations");
            }
        }

        // Hide all target markers using TargetMarkerManager
        if (TargetMarkerManager.Instance != null)
        {
            TargetMarkerManager.Instance.HideAllMarkers();
        }

        // Clear target
        target = null;

        // Clear visual path
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }

        // Reset navPath
        navPath = new NavMeshPath();

        // Reset arrival state
        hasArrived = false;

        Debug.Log("🛑 Navigation terminated");
    }
    #endregion
}
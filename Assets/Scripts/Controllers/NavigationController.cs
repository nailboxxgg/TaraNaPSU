using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

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

    [Header("Behavior")]
    public float arriveDistance = 1.0f;       // when user is within this distance — arrived
    public float arrowDistanceFromCamera = 1.5f; // position arrow this far in front of camera
    public float arrowSlerpSpeed = 10f;       // arrow rotation smoothing
    

    [Header("Optional: Multi-floor NavMeshSurfaces")]
    public List<NavMeshSurface> navMeshSurfaces = new List<NavMeshSurface>();
    // Use SwitchToNavMeshFor(buildingId, floor) to enable appropriate surface(s)

    // Public state
    public bool IsNavigating => navigating;

    // Internal
    private NavMeshPath navPath;
    private bool navigating = false;
    private bool hasArrived = false;

    private AnchorManager.AnchorData startAnchor;
    private TargetData targetData;

    private GameObject activeTargetPin;
    private GameObject activeArrow;
    public Transform target;


    private List<Vector3> currentCorners = new List<Vector3>();

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        if (agent == null) Debug.LogError("[NavigationController] Assign a NavMeshAgent in Inspector.");
        if (arCamera == null && Camera.main != null) arCamera = Camera.main.transform;

        // Agent must not drive the camera - use it as a path calculator only
        if (agent != null)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
        }

        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        navPath = new NavMeshPath();

        // Instantiate arrow now (if provided)
        if (arrowPrefab != null)
        {
            activeArrow = Instantiate(arrowPrefab);
            activeArrow.SetActive(false);
        }
    }

    void Update()
    {
        if (!navigating || targetData == null || arCamera == null) return;

        Vector3 userPos = arCamera.position;

        // Recalculate path from user to target
        NavMesh.CalculatePath(userPos, activeTargetPin.transform.position, NavMesh.AllAreas, navPath);
        UpdateCornersFromPath(navPath);

        DrawPath();

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
    /// Start navigation given a scanned start anchor and a target name from TargetManager.
    /// This will:
    /// - attempt to snap anchor & target to the nearest NavMesh
    /// - optionally switch NavMesh surfaces (if you have them assigned)
    /// - spawn a target pin and enable arrow/line drawing
    /// </summary>
    public void BeginNavigation(AnchorManager.AnchorData start, string targetName, bool warpAgentToStart = false)
    {
        if (start == null)
        {
            Debug.LogError("[NavigationController] BeginNavigation called with null start.");
            return;
        }
        if (string.IsNullOrEmpty(targetName))
        {
            Debug.LogError("[NavigationController] BeginNavigation called with empty targetName.");
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

        // If you use navMeshSurfaces per building/floor, switch to relevant one
        // This is optional; implement matching logic by buildingId/floor stored in AnchorData & surfaces
        SwitchToNavMeshFor(startAnchor.BuildingId, startAnchor.Floor);

        // Spawn/position target pin
        SpawnOrPlaceTargetPin(targetData);

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

        // Show arrow and status
        if (activeArrow != null) activeArrow.SetActive(true);
        if (statusController != null)
            statusController.SetNavigationInfo(startAnchor.BuildingId ?? "-", targetData.Name);

        Debug.Log($"[NavigationController] Navigation started from {startAnchor.AnchorId} to {targetData.Name}");
    }

    /// <summary>
    /// Stops current navigation and clears visuals.
    /// </summary>
    public void StopNavigation()
    {
        navigating = false;
        hasArrived = false;

        if (activeTargetPin != null) Destroy(activeTargetPin);
        activeTargetPin = null;

        if (activeArrow != null) activeArrow.SetActive(false);

        lineRenderer.positionCount = 0;

        if (statusController != null)
            statusController.UpdateStatus("Navigation stopped");
    }

    #endregion

    #region Helpers - Path & Visuals

    private void UpdateCornersFromPath(NavMeshPath path)
    {
        currentCorners.Clear();
        if (path == null || path.corners == null || path.corners.Length == 0) return;
        currentCorners.AddRange(path.corners);
    }

    private void DrawPath()
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
    private Vector3 SampleNavMeshPosition(Vector3 pos, float maxDistance = 2f, Vector3 fallback = default)
    {
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
            return hit.position;
        return fallback == default ? pos : fallback;
    }

    /// <summary>
    /// Optional: enable/disable NavMeshSurface components to switch active NavMesh.
    /// You must assign navMeshSurfaces in Inspector and name them or provide custom metadata.
    /// This method does a best-effort toggle by matching substrings in surface.name.
    /// </summary>
    public void SwitchToNavMeshFor(string buildingId, int floor)
    {
        if (navMeshSurfaces == null || navMeshSurfaces.Count == 0) return;

        string needle = (buildingId ?? "") + $"_F{floor}";
        bool anyMatched = false;

        foreach (var s in navMeshSurfaces)
        {
            if (s == null) continue;
            bool match = s.name.Contains(needle) || s.name.Contains(buildingId) || s.name.Contains($"F{floor}");
            s.enabled = match;
            anyMatched |= match;
        }

        if (!anyMatched)
            Debug.LogWarning($"[NavigationController] No navmesh surface matched for {buildingId} floor {floor}. Ensure navMeshSurfaces are assigned and named consistently.");
        else
            Debug.Log($"[NavigationController] Switched NavMesh to {buildingId} floor {floor} (best-effort).");
    }

    #endregion

    #region Arrival callback

    private void OnArrived()
    {
        Debug.Log("✅ Arrived at destination!");
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
            agent.isStopped = true;
            agent.ResetPath();
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
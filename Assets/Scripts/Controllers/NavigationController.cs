using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(LineRenderer))]
public class NavigationController : MonoBehaviour
{
    public static NavigationController Instance { get; private set; }

    [Header("References")]
    public NavMeshAgent agent;          // assign Player's NavMeshAgent
    public Transform target;            // destination Transform
    public LineRenderer lineRenderer;   // path visualization
    public NavigationStatusController statusController; // Navigation status UI

    [Header("Visuals")]
    public float lineHeightOffset = 0.1f;
    public Color lineColor = Color.cyan;
    public float arriveDistance = 1.0f;

    private NavMeshPath navPath;
    private bool hasArrived = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        navPath = new NavMeshPath();
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
    }

    void Update()
    {
        if (target == null || agent == null) return;

        // Don't update if already arrived
        if (hasArrived) return;

        // Update NavMesh path
        NavMesh.CalculatePath(agent.transform.position, target.position, NavMesh.AllAreas, navPath);
        DrawPath();

        // Move agent toward destination
        agent.SetDestination(target.position);

        // Check arrival - only call once
        if (!agent.pathPending && agent.remainingDistance <= arriveDistance && !hasArrived)
        {
            OnArrived();
            hasArrived = true;
        }
    }

    private void DrawPath()
    {
        if (navPath.corners.Length == 0)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        lineRenderer.positionCount = navPath.corners.Length;
        for (int i = 0; i < navPath.corners.Length; i++)
        {
            Vector3 pos = navPath.corners[i];
            pos.y += lineHeightOffset;
            lineRenderer.SetPosition(i, pos);
        }
    }

    // ✅ This method must exist to clear the error CS0103
    private void OnArrived()
    {
        Debug.Log("✅ Arrived at destination!");
        lineRenderer.positionCount = 0;

        // Update navigation status UI
        if (statusController != null)
        {
            statusController.OnArrived();
        }
    }

    // ✅ This method must exist before BeginNavigation()
    public void SetDestination(Transform dest)
    {
        target = dest;
    }

    // ✅ This method fixes BeginNavigation() call in AppFlowController
// Start navigation using a startAnchor (AnchorData) and a target name.
public void BeginNavigation(AnchorManager.AnchorData startAnchor, string targetName)
{
    if (string.IsNullOrEmpty(targetName))
    {
        Debug.LogWarning("BeginNavigation called with empty target name.");
        return;
    }

    // Try to find target in TargetManager
    if (!TargetManager.Instance.TryGetTarget(targetName, out var targetData))
    {
        Debug.LogWarning($"No target data found for: {targetName}");
        return;
    }

    // Find or create a target marker in the scene
    GameObject targetObj = GameObject.Find(targetData.Name);
    if (targetObj == null)
    {
        targetObj = new GameObject(targetData.Name);
        targetObj.transform.position = targetData.Position.ToVector3();
        targetObj.transform.rotation = Quaternion.Euler(targetData.Rotation.ToVector3());
    }

    // If a startAnchor was provided, warp the agent there
    if (startAnchor != null)
    {
        if (agent != null)
        {
            Vector3 anchorPos = startAnchor.Position.ToVector3();

            // Adjust anchor Y position to match NavMesh level (Y=0)
            Vector3 adjustedAnchorPos = new Vector3(anchorPos.x, 0f, anchorPos.z);

            // Try to find a valid NavMesh position near the adjusted anchor position
            if (NavMesh.SamplePosition(adjustedAnchorPos, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                // Successfully found a valid NavMesh position, warp there
                agent.Warp(hit.position);
                Debug.Log($"📍 Agent warped to valid NavMesh position near anchor {startAnchor.AnchorId}: {hit.position}");
            }
            else
            {
                // No valid NavMesh found near anchor, try agent's current position
                Debug.LogWarning($"⚠️ No valid NavMesh found within 5m of adjusted anchor {adjustedAnchorPos}. Starting from current position.");
            }
        }
        else
        {
            Debug.LogWarning("BeginNavigation: NavMeshAgent is not assigned; cannot warp to anchor.");
        }
    }
    else
    {
        Debug.Log("BeginNavigation: startAnchor is null — starting navigation from current agent position.");
    }

    // Extract building information from target name (e.g., "B1-Quality Assurance Office")
    string buildingName = targetName;
    string destinationName = targetName;

    if (targetName.Contains("-"))
    {
        string[] parts = targetName.Split('-');
        if (parts.Length >= 2)
        {
            buildingName = parts[0];
            destinationName = parts[1];
        }
    }

    // Update navigation status UI
    if (statusController != null)
    {
        statusController.SetNavigationInfo(buildingName, destinationName);
    }

    // Set destination and begin navigating
    SetDestination(targetObj.transform);

    Debug.Log($"🚀 Navigation started to {targetName}");
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
}
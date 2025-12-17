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

    // Event for other scripts to listen for arrival
    public event System.Action OnArrival;

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

        OnArrival?.Invoke();
    }

    // ✅ This method must exist before BeginNavigation()
    public void SetDestination(Transform dest)
    {
        target = dest;
    }

    // ✅ Overload for navigating to an Anchor (Intermediate Stair)
    public void BeginNavigation(AnchorManager.AnchorData startAnchor, AnchorManager.AnchorData destAnchor)
    {
        if (destAnchor == null) return;

        // Create or find a temp object for the destination
        GameObject targetObj = GameObject.Find(destAnchor.AnchorId);
        if (targetObj == null)
        {
            targetObj = new GameObject(destAnchor.AnchorId);
            targetObj.transform.position = destAnchor.Position.ToVector3();
            targetObj.transform.rotation = Quaternion.Euler(destAnchor.Rotation.ToVector3());
        }

        // Warp agent to start anchor if provided
        WarpToAnchor(startAnchor);

        // Update UI
        if (statusController != null)
        {
            statusController.SetNavigationInfo("Navigation", "Go to " + destAnchor.Meta);
        }

        // HIDE ALL OTHER TARGETS & SHOW CURRENT ONE
        HideAllTargets();
        if (targetObj != null) targetObj.SetActive(true);

        // Start Moving
        SetDestination(targetObj.transform);
        // Reset arrival state
        hasArrived = false;
        if (agent != null) agent.isStopped = false;

        Debug.Log($"🚀 Navigation started to Anchor: {destAnchor.AnchorId}");
    }

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

        // Warp if needed
        WarpToAnchor(startAnchor);

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

        // HIDE ALL OTHER TARGETS & SHOW CURRENT ONE
        HideAllTargets();
        if (targetObj != null) 
        {
            targetObj.SetActive(true);
        }

        // Set destination and begin navigating
        SetDestination(targetObj.transform);
        
        // Reset arrival state
        hasArrived = false;
        if (agent != null) agent.isStopped = false;

        Debug.Log($"🚀 Navigation started to {targetName}");
    }

    private void HideAllTargets()
    {
        if (TargetManager.Instance != null)
        {
            foreach (string name in TargetManager.Instance.GetAllTargetNames())
            {
                GameObject obj = GameObject.Find(name);
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
            // Also hide Anchors/Stairs that might be active markers
            if (AnchorManager.Instance != null)
            {
                foreach (var anchor in AnchorManager.Instance.Anchors)
                {
                     GameObject obj = GameObject.Find(anchor.AnchorId);
                     if (obj != null) obj.SetActive(false);
                }
            }
        }
    }

    private void WarpToAnchor(AnchorManager.AnchorData startAnchor)
    {
        // If a startAnchor was provided, warp the agent there
        if (startAnchor != null)
        {
            // Auto-assign if missing
            if (agent == null) agent = GetComponent<NavMeshAgent>();

            if (agent != null)
            {
                Vector3 anchorPos = startAnchor.Position.ToVector3();

                if (NavMesh.SamplePosition(anchorPos, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                    Debug.Log($"📍 Agent warped to valid NavMesh position near anchor {startAnchor.AnchorId}: {hit.position}");
                }
                else
                {
                   
                    Debug.LogWarning($"⚠️ No valid NavMesh found within 5m of anchor {anchorPos}. Check if NavMesh is baked at this floor.");
                }
            }
            else
            {
                Debug.LogWarning("BeginNavigation: NavMeshAgent is not assigned; cannot warp to anchor.");
            }
        }
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
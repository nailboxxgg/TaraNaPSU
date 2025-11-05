using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(LineRenderer))]
public class NavigationController : MonoBehaviour
{
    public static NavigationController Instance { get; private set; }

    [Header("References")]
    public NavMeshAgent agent;          // assign Player’s NavMeshAgent
    public Transform target;            // destination Transform
    public LineRenderer lineRenderer;   // path visualization

    [Header("Visuals")]
    public float lineHeightOffset = 0.1f;
    public Color lineColor = Color.cyan;
    public float arriveDistance = 1.0f;

    private NavMeshPath navPath;

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

        // Update NavMesh path
        NavMesh.CalculatePath(agent.transform.position, target.position, NavMesh.AllAreas, navPath);
        DrawPath();

        // Move agent toward destination
        agent.SetDestination(target.position);

        // Check arrival
        if (!agent.pathPending && agent.remainingDistance <= arriveDistance)
            OnArrived();
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
    }

    // ✅ This method must exist before BeginNavigation()
    public void SetDestination(Transform dest)
    {
        target = dest;
    }

    // ✅ This method fixes BeginNavigation() call in AppFlowController
    public void BeginNavigation(string targetName)
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
            // Create a simple marker object if it doesn’t exist
            targetObj = new GameObject(targetData.Name);
            targetObj.transform.position = targetData.Position.ToVector3();
            targetObj.transform.rotation = Quaternion.Euler(targetData.Rotation.ToVector3());
        }

        // ✅ This line works now because SetDestination exists above
        SetDestination(targetObj.transform);

        Debug.Log($"🚀 Navigation started to {targetName}");
    }
}

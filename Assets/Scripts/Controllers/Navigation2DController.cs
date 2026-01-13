using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(LineRenderer))]
public class Navigation2DController : MonoBehaviour
{
    public static Navigation2DController Instance { get; private set; }

    [Header("Path Visualization")]
    public LineRenderer lineRenderer;
    public float pathHeight = 0.5f;
    public Color pathColor = Color.blue;
    public float pathWidth = 0.3f;

    [Header("Navigation Settings")]
    public float arriveDistance = 1.5f;

    private NavMeshPath navPath;
    private bool isNavigating = false;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private string targetName;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        navPath = new NavMeshPath();

        SetupLineRenderer();
    }

    void SetupLineRenderer()
    {
        if (lineRenderer != null)
        {
            lineRenderer.startWidth = pathWidth;
            lineRenderer.endWidth = pathWidth;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = pathColor;
            lineRenderer.endColor = pathColor;
            lineRenderer.positionCount = 0;
        }
    }

    public void BeginNavigation(Vector3 start, Vector3 target, string destinationName)
    {
        startPosition = start;
        targetPosition = target;
        targetName = destinationName;

        Vector3 snappedStart = SampleNavMeshPosition(start, 5f, start);
        Vector3 snappedTarget = SampleNavMeshPosition(target, 5f, target);

        bool pathFound = NavMesh.CalculatePath(snappedStart, snappedTarget, NavMesh.AllAreas, navPath);

        if (pathFound && navPath.status == NavMeshPathStatus.PathComplete)
        {
            DrawPath();
            isNavigating = true;
            Debug.Log($"[Nav2D] Path found from start to {destinationName}");
        }
        else
        {
            Debug.LogWarning($"[Nav2D] Could not find path to {destinationName}");
            lineRenderer.positionCount = 0;
        }
    }

    public void RecalculatePath(Vector3 newStart)
    {
        if (!isNavigating) return;

        startPosition = newStart;
        Vector3 snappedStart = SampleNavMeshPosition(newStart, 5f, newStart);
        Vector3 snappedTarget = SampleNavMeshPosition(targetPosition, 5f, targetPosition);

        NavMesh.CalculatePath(snappedStart, snappedTarget, NavMesh.AllAreas, navPath);
        DrawPath();
    }

    void DrawPath()
    {
        if (navPath == null || navPath.corners == null || navPath.corners.Length == 0)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        int count = navPath.corners.Length;
        lineRenderer.positionCount = count;

        for (int i = 0; i < count; i++)
        {
            Vector3 point = navPath.corners[i];
            point.y = pathHeight;
            lineRenderer.SetPosition(i, point);
        }
    }

    public void StopNavigation()
    {
        isNavigating = false;
        lineRenderer.positionCount = 0;
        targetName = null;
        Debug.Log("[Nav2D] Navigation stopped");
    }

    public bool CheckArrival(Vector3 currentPosition)
    {
        if (!isNavigating) return false;

        float distance = Vector3.Distance(
            new Vector3(currentPosition.x, 0, currentPosition.z),
            new Vector3(targetPosition.x, 0, targetPosition.z)
        );

        return distance <= arriveDistance;
    }

    private Vector3 SampleNavMeshPosition(Vector3 pos, float maxDistance, Vector3 fallback)
    {
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
            return hit.position;
        return fallback;
    }

    public float GetPathLength()
    {
        if (navPath == null || navPath.corners.Length < 2) return 0f;

        float length = 0f;
        for (int i = 1; i < navPath.corners.Length; i++)
        {
            length += Vector3.Distance(navPath.corners[i - 1], navPath.corners[i]);
        }
        return length;
    }

    public bool IsNavigating => isNavigating;
    public string TargetName => targetName;
    public Vector3[] GetPathCorners() => navPath?.corners;
}

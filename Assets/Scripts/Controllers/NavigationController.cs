using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(LineRenderer))]
public class NavigationController : MonoBehaviour
{
    public static NavigationController Instance { get; private set; }

    [Header("References")]
    public NavMeshAgent agent;          
    public Transform target;            
    public LineRenderer lineRenderer;   
    public NavigationStatusController statusController; 

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

        
        if (hasArrived) return;

        
        NavMesh.CalculatePath(agent.transform.position, target.position, NavMesh.AllAreas, navPath);
        DrawPath();

        
        agent.SetDestination(target.position);

        
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

    
    public event System.Action OnArrival;

    
    private void OnArrived()
    {
        Debug.Log("✅ Arrived at destination!");
        lineRenderer.positionCount = 0;

        
        if (statusController != null)
        {
            statusController.OnArrived();
        }

        OnArrival?.Invoke();
    }

    
    public void SetDestination(Transform dest)
    {
        target = dest;
    }

    
    public void BeginNavigation(AnchorData startAnchor, AnchorData destAnchor)
    {
        if (destAnchor == null) return;

        
        GameObject targetObj = GameObject.Find(destAnchor.AnchorId);
        if (targetObj == null)
        {
            targetObj = new GameObject(destAnchor.AnchorId);
            targetObj.transform.position = destAnchor.Position.ToVector3();
            targetObj.transform.rotation = Quaternion.Euler(destAnchor.Rotation.ToVector3());
        }

        
        WarpToAnchor(startAnchor);

        
        if (statusController != null)
        {
            statusController.SetNavigationInfo("Navigation", "Go to " + destAnchor.Meta);
        }

        
        HideAllTargets();
        if (targetObj != null) targetObj.SetActive(true);

        
        SetDestination(targetObj.transform);
        
        hasArrived = false;
        if (agent != null) agent.isStopped = false;

        Debug.Log($"🚀 Navigation started to Anchor: {destAnchor.AnchorId}");
    }

    public void BeginNavigation(AnchorData startAnchor, string targetName)
    {
        if (string.IsNullOrEmpty(targetName))
        {
            Debug.LogWarning("BeginNavigation called with empty target name.");
            return;
        }

        
        if (!TargetManager.Instance.TryGetTarget(targetName, out var targetData))
        {
            Debug.LogWarning($"No target data found for: {targetName}");
            return;
        }

        
        GameObject targetObj = GameObject.Find(targetData.Name);
        if (targetObj == null)
        {
            targetObj = new GameObject(targetData.Name);
            targetObj.transform.position = targetData.Position.ToVector3();
            targetObj.transform.rotation = Quaternion.Euler(targetData.Rotation.ToVector3());
        }

        
        WarpToAnchor(startAnchor);

        
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

        
        if (statusController != null)
        {
            statusController.SetNavigationInfo(buildingName, destinationName);
        }

        
        HideAllTargets();
        if (targetObj != null) 
        {
            targetObj.SetActive(true);
        }

        
        SetDestination(targetObj.transform);
        
        
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

    private void WarpToAnchor(AnchorData startAnchor)
    {
        
        if (startAnchor != null)
        {
            
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
        
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        
        target = null;

        
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }

        
        navPath = new NavMeshPath();

        
        hasArrived = false;

        Debug.Log("🛑 Navigation terminated");
    }
}

using UnityEngine;

/// <summary>
/// Enhanced StairwayMarker that integrates with the navigation system.
/// Acts as both a visual marker and a functional stairway for multi-floor navigation.
/// </summary>
public class StairwayMarker : MonoBehaviour
{
    [Header("Floor Connection")]
    [Tooltip("Which floor does this stair start from?")]
    public int fromFloor = 0;

    [Tooltip("Which floor does this stair lead to?")]
    public int toFloor = 1;

    [Header("Bi-directional")]
    [Tooltip("Can users go both ways? (up and down)")]
    public bool isBidirectional = true;

    [Header("Building & Navigation")]
    [Tooltip("Building ID this stairway belongs to (e.g., B1, B2, B3)")]
    public string buildingId = "B1";

    [Tooltip("Stairway identifier for navigation system")]
    public string stairwayId = "";

    [Header("Visual Settings")]
    public bool showGizmo = true;
    public Color gizmoColor = Color.yellow;

    // Navigation integration
    [Header("Navigation Integration")]
    [Tooltip("Automatically register with AnchorManager on Start")]
    public bool autoRegister = true;

    [Header("Stairway Detection")]
    [Tooltip("Distance to automatically trigger stairway arrival")]
    public float arrivalTriggerDistance = 1.5f;

    [Header("Visual Settings")]
    [Tooltip("Show trigger zone gizmo")]
    public bool showTriggerZone = true;

    void Start()
    {
        // Auto-generate stairway ID if not set
        if (string.IsNullOrEmpty(stairwayId))
        {
            stairwayId = $"{buildingId}-Stair-{fromFloor}-{toFloor}";
        }

        // Auto-register with AnchorManager if enabled
        if (autoRegister && AnchorManager.Instance != null)
        {
            RegisterWithAnchorManager();
        }
    }

    /// <summary>
    /// Register this stairway with AnchorManager for navigation
    /// </summary>
    private void RegisterWithAnchorManager()
    {
        // Create bottom anchor entry
        var bottomAnchor = new AnchorManager.AnchorData
        {
            Type = "stair",
            BuildingId = buildingId,
            AnchorId = $"{stairwayId}-Bottom",
            Floor = fromFloor,
            Position = new AnchorManager.Vector3Serializable(transform.position),
            Rotation = new AnchorManager.Vector3Serializable(transform.rotation.eulerAngles),
            Meta = $"Stairway bottom from F{fromFloor} to F{toFloor}"
        };

        // Create top anchor entry
        var topAnchor = new AnchorManager.AnchorData
        {
            Type = "stair",
            BuildingId = buildingId,
            AnchorId = $"{stairwayId}-Top",
            Floor = toFloor,
            Position = new AnchorManager.Vector3Serializable(transform.position),
            Rotation = new AnchorManager.Vector3Serializable(transform.rotation.eulerAngles),
            Meta = $"Stairway top from F{fromFloor} to F{toFloor}"
        };

        // Add to AnchorManager
        AnchorManager.Instance.Anchors.Add(bottomAnchor);
        AnchorManager.Instance.Anchors.Add(topAnchor);

        Debug.Log($"[StairwayMarker] Registered stairway: {stairwayId} ({buildingId} F{fromFloor}→F{toFloor})");
    }

    /// <summary>
    /// Check if this stairway connects the specified floors
    /// </summary>
    public bool ConnectsFloors(int floor1, int floor2)
    {
        if (isBidirectional)
        {
            return (fromFloor == floor1 && toFloor == floor2) ||
                   (fromFloor == floor2 && toFloor == floor1);
        }
        else
        {
            return fromFloor == floor1 && toFloor == floor2;
        }
    }

    /// <summary>
    /// Get the destination floor from current floor
    /// </summary>
    public int GetDestinationFloor(int currentFloor)
    {
        if (currentFloor == fromFloor)
        {
            return toFloor;
        }
        else if (isBidirectional && currentFloor == toFloor)
        {
            return fromFloor;
        }
        return -1;
    }

    /// <summary>
    /// Check if this stairway is on the specified floor
    /// </summary>
    public bool IsOnFloor(int floor)
    {
        return fromFloor == floor || toFloor == floor;
    }

    /// <summary>
    /// Get the other floor this stairway connects to
    /// </summary>
    public int GetOtherFloor(int currentFloor)
    {
        if (currentFloor == fromFloor)
            return toFloor;
        else if (currentFloor == toFloor)
            return fromFloor;
        return -1;
    }

    /// <summary>
    /// Get stairway position for navigation (can be overridden for custom positioning)
    /// </summary>
    public Vector3 GetNavigationPosition()
    {
        return transform.position;
    }

    void Update()
    {
        // Auto-trigger arrival if user is close enough (for AR scenarios)
        if (arrivalTriggerDistance > 0 && Camera.main != null)
        {
            float distanceToUser = Vector3.Distance(transform.position, Camera.main.transform.position);
            if (distanceToUser <= arrivalTriggerDistance)
            {
                TriggerArrival();
            }
        }
    }

    /// <summary>
    /// Trigger arrival at this stairway (for manual triggering or testing)
    /// </summary>
    public void TriggerArrival()
    {
        Debug.Log($"[StairwayMarker] Arrival triggered at {stairwayId}");

        // Notify MultiFloorNavigationManager if active
        if (MultiFloorNavigationManager.Instance != null && MultiFloorNavigationManager.Instance.IsMultiFloorNavigationActive)
        {
            MultiFloorNavigationManager.Instance.OnSegmentArrived();
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo) return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        Vector3 direction = (toFloor > fromFloor) ? Vector3.up : Vector3.down;
        Gizmos.DrawRay(transform.position, direction * 1f);

        #if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.8f,
            $"Stairs: F{fromFloor} → F{toFloor}\n{buildingId}\n{stairwayId}"
        );
        #endif
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 1f);

        // Draw connection to other floor
        Vector3 otherFloorPos = transform.position + Vector3.up * (toFloor - fromFloor) * 2f;
        Gizmos.DrawLine(transform.position, otherFloorPos);
        Gizmos.DrawWireSphere(otherFloorPos, 0.3f);

        // Draw trigger zone if enabled
        if (showTriggerZone)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Semi-transparent green
            Gizmos.DrawWireSphere(transform.position, arrivalTriggerDistance);
        }
    }
}
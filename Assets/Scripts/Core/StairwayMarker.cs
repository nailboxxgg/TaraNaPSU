using UnityEngine;

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

    [Header("Visual Settings")]
    public bool showGizmo = true;
    public Color gizmoColor = Color.yellow;

    public bool ConnectsFloors(int floor1, int floor2) 
    {
        if (isBidirectional) 
        {
            return (fromFloor == floor1 && toFloor == floor2) || 
                   (fromFloor == floor2 && toFloor == floor1);
        } else {
            return fromFloor == floor1 && toFloor == floor2;
        }
    }

    public int GetDestinationFloor(int currentFloor) 
    {
        if (currentFloor == fromFloor) 
        {
            return toFloor;
        } else if (isBidirectional && currentFloor == toFloor) {
            return fromFloor;
        }
        return -1; 
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
            $"Stairs: F{fromFloor} → F{toFloor}"
        );
        #endif
    }

    private void OnDrawGizmosSelected() 
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}

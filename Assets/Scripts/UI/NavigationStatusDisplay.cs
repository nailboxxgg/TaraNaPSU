using UnityEngine;
using TMPro;  // For TextMeshPro text components
using System;  // For Action events

public class NavigationStatusDisplay : MonoBehaviour
{
    // Singleton instance for global access (optional but safe)
    public static NavigationStatusDisplay Instance { get; private set; }

    // UI Text components (assign in Inspector)
    [Header("UI Text Components")]
    [SerializeField] private TextMeshProUGUI StatusText;        // Displays "Navigating" or "Idle"
    [SerializeField] private TextMeshProUGUI BuildingNumberText; // Displays current floor/building (e.g., "Floor 1")
    [SerializeField] private TextMeshProUGUI NavigationTargetText; // Displays destination name (e.g., "Room 101")

    // Data structure for navigation status (read-only)
    [Serializable]
    public class NavigationStatus
    {
        public string Status;        // e.g., "Idle" or "Navigating"
        public string Building;      // Current building/floor (placeholder: uses target floor)
        public string Destination;   // Target name
        public float Progress;       // Optional: 0-1 for path progress (0 if idle)
    }

    private void Awake()
    {
        // Setup singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
                        return;
        }
        Instance = this;

        // Optional: Subscribe to planned events (implement in NavigationController later)
        // NavigationController.Instance.OnFloorChanged += UpdateStatusFromNavigation;

        // Initialize with default status
        UpdateUI("Idle", "Floor 1", "None");
    }

    // Public method to get current navigation status (safe read)
    public NavigationStatus GetNavigationStatus()
    {
        NavigationStatus status = new NavigationStatus();
        string currentFloor = "Floor 1";  // Placeholder: replace with real logic later
        string statusText = "Idle";

        // Safely check NavigationController instance
        if (NavigationController.Instance != null && NavigationController.Instance.target != null)
        {
            statusText = "Navigating";
            // Get destination name from current target (assumes GameObject name matches)
            status.Destination = NavigationController.Instance.target.name;
            // TODO: Implement building/floor from target data or transform
            currentFloor = "Floor 1";  // Placeholder: will be improved
        }
        else
        {
            status.Destination = "--";
        }

        status.Status = statusText;
        status.Building = currentFloor;
        status.Progress = 0f;  // Placeholder: implement path progress later

        return status;
    }

    // Public method to update the UI manually (safe to call)
    public void SetNavigationDetails(string status, string building, string destination)
    {
        UpdateUI(status, building, destination);
    }

    // Public method to trigger UI refresh from event (safe, no app disruption)
    public void UpdateStatusFromEvent()
    {
        var currentStatus = GetNavigationStatus();
        UpdateUI(currentStatus.Status, currentStatus.Building, currentStatus.Destination);
    }

    // Internal UI update (avoids direct inspector changes)
    private void UpdateUI(string status, string building, string destination)
    {
        if (StatusText != null) StatusText.text = status;
        if (BuildingNumberText != null) BuildingNumberText.text = building;
        if (NavigationTargetText != null) NavigationTargetText.text = destination;
    }

    // Cleanup on destroy
    private void OnDestroy()
    {
        // Optional: Unsubscribe events if added
        // if (NavigationController.Instance != null)
        //     NavigationController.Instance.OnFloorChanged -= UpdateStatusFromNavigation;
    }
}
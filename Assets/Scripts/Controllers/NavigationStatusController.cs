using UnityEngine;
using TMPro;

public class NavigationStatusController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text StatusText;       
    public TMP_Text BuildingText;     
    public TMP_Text DestinationText;  

    private string currentBuilding = "";
    private string currentDestination = "";

    void Start()
    {
        // Initialize default display
        StatusText.text = "Waiting for navigation...";
        BuildingText.text = "-";
        DestinationText.text = "-";
    }

    // ---------------------------------------------------------
    // 📌 Called when the app starts navigation
    // ---------------------------------------------------------
    public void SetNavigationInfo(string buildingId, string destinationName)
    {
        currentBuilding = buildingId;
        currentDestination = destinationName;

        BuildingText.text = $"Building: {buildingId}";
        DestinationText.text = $"Destination: {destinationName}";

        StatusText.text = "Status: Navigation Started";
    }

    // ---------------------------------------------------------
    // 📌 Called continuously during navigation (optional)
    // ---------------------------------------------------------
    public void UpdateStatus(string newStatus)
    {
        StatusText.text = $"Status: {newStatus}";
    }

    // ---------------------------------------------------------
    // 📌 Called when the user arrives
    // ---------------------------------------------------------
    public void OnArrived()
    {
        StatusText.text = "Arrived!";
    }
}

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
        StatusText.text = "Waiting for navigation...";
        BuildingText.text = "-";
        DestinationText.text = "-";
    }

    public void SetNavigationInfo(string buildingId, string destinationName)
    {
        currentBuilding = buildingId;
        currentDestination = destinationName;

        BuildingText.text = $"Building: {buildingId}";
        DestinationText.text = $"Destination: {destinationName}";

        StatusText.text = "Status: Navigation Started";
    }

    public void UpdateStatus(string newStatus)
    {
        StatusText.text = $"Status: {newStatus}";
    }

    public void OnArrived()
    {
        if (StatusText != null)
        {
            StatusText.text = "Status: Arrived!";
        }
        else
        {
            Debug.LogError("❌ StatusText is not assigned in NavigationStatusController!");
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NavigationStatusDisplay2D : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text statusText;
    public TMP_Text destinationText;
    public TMP_Text distanceText;
    public TMP_Text floorText;
    public TMP_Text promptText;
    public GameObject promptPanel;
    public GameObject arrivedPanel;

    [Header("References")]
    public Navigation2DController navigationController;
    public Map2DController mapController;

    void Update()
    {
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (navigationController == null) return;

        if (navigationController.IsNavigating)
        {
            if (statusText != null)
                statusText.text = "Navigating...";

            if (destinationText != null)
                destinationText.text = navigationController.TargetName ?? "-";

            if (distanceText != null)
            {
                float distance = navigationController.GetPathLength();
                distanceText.text = $"{distance:F1}m";
            }
        }
        else
        {
            if (statusText != null)
                statusText.text = "Select a destination";

            if (destinationText != null)
                destinationText.text = "-";

            if (distanceText != null)
                distanceText.text = "-";
        }

        if (mapController != null && floorText != null)
        {
            string[] floorNames = { "Campus Grounds", "Ground Floor", "1st Floor" };
            int floor = mapController.currentFloor;
            floorText.text = floor < floorNames.Length ? floorNames[floor] : $"Floor {floor}";

            UpdatePrompt(floor);
        }

        // Show arrival when player guide finishes walking
        if (PlayerGuideController.Instance != null && !PlayerGuideController.Instance.IsWalking && navigationController.IsNavigating)
        {
            if (navigationController.CheckArrival(PlayerGuideController.Instance.transform.position))
            {
                ShowArrived();
            }
        }
        else
        {
            HideArrived();
        }
    }

    void UpdatePrompt(int currentMapFloor)
    {
        if (AppFlowController2D.Instance == null || promptPanel == null || promptText == null) return;

        if (!navigationController.IsNavigating)
        {
            promptPanel.SetActive(false);
            return;
        }

        int targetFloor = AppFlowController2D.Instance.TargetFloor;

        if (currentMapFloor != targetFloor)
        {
            promptPanel.SetActive(true);
            string floorName = targetFloor == 0 ? "Campus Grounds" : (targetFloor == 1 ? "Ground Floor" : "1st Floor");
            promptText.text = $"Proceed to the stairs. Destination is on the <color=yellow>{floorName}</color>.";
        }
        else
        {
            // If on the correct floor but far from destination
            if (navigationController.GetPathLength() > 5f)
            {
                promptPanel.SetActive(true);
                promptText.text = "Follow the path to your destination.";
            }
            else
            {
                promptPanel.SetActive(false);
            }
        }
    }

    public void ShowArrived()
    {
        if (arrivedPanel != null)
            arrivedPanel.SetActive(true);

        if (statusText != null)
            statusText.text = "You have arrived!";
    }

    public void HideArrived()
    {
        if (arrivedPanel != null)
            arrivedPanel.SetActive(false);
    }
}

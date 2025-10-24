using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages navigation UI elements (distance, ETA, status messages)
/// </summary>
public class NavigationUIController : MonoBehaviour {

    [Header("References")]
    public NavigationController navigationController;

    [Header("UI Elements")]
    public GameObject navigationInfoPanel;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI etaText;
    public TextMeshProUGUI targetNameText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI currentFloorText;
    
    [Header("Error/Status Display")]
    public GameObject errorPanel;
    public TextMeshProUGUI errorMessageText;
    public float errorDisplayDuration = 3f;

    [Header("Arrival Notification")]
    public GameObject arrivalPanel;
    public TextMeshProUGUI arrivalMessageText;

    private float errorTimer;
    private bool isShowingError;

    private void Start() {
        if (navigationController == null) {
            navigationController = FindObjectOfType<NavigationController>();
        }

        // Subscribe to navigation events
        if (navigationController != null) {
            navigationController.OnNavigationUpdate.AddListener(UpdateNavigationUI);
            navigationController.OnNavigationError.AddListener(ShowError);
            navigationController.OnTargetReached.AddListener(ShowArrivalNotification);
            navigationController.OnFloorChanged.AddListener(UpdateFloorDisplay);
        }

        // Hide panels initially
        if (navigationInfoPanel != null) navigationInfoPanel.SetActive(false);
        if (errorPanel != null) errorPanel.SetActive(false);
        if (arrivalPanel != null) arrivalPanel.SetActive(false);

        UpdateFloorDisplay(navigationController?.currentFloor ?? 0);
    }

    private void Update() {
        // Auto-hide error message after duration
        if (isShowingError) {
            errorTimer -= Time.deltaTime;
            if (errorTimer <= 0) {
                HideError();
            }
        }

        // Update status text
        UpdateStatusText();
    }

    /// <summary>
    /// Called when navigation updates (distance/ETA changes)
    /// </summary>
    private void UpdateNavigationUI(float distance, float eta) {
        if (navigationInfoPanel != null && !navigationInfoPanel.activeSelf) {
            navigationInfoPanel.SetActive(true);
        }

        if (distanceText != null) {
            if (distance < 1f) {
                distanceText.text = $"{(distance * 100f):F0} cm";
            } else {
                distanceText.text = $"{distance:F1} m";
            }
        }

        if (etaText != null) {
            if (eta < 60f) {
                etaText.text = $"~{eta:F0} sec";
            } else {
                int minutes = Mathf.FloorToInt(eta / 60f);
                int seconds = Mathf.FloorToInt(eta % 60f);
                etaText.text = $"~{minutes}:{seconds:D2} min";
            }
        }

        if (targetNameText != null && !string.IsNullOrEmpty(navigationController.CurrentTargetName)) {
            targetNameText.text = navigationController.CurrentTargetName;
        }
    }

    /// <summary>
    /// Update the status text based on navigation state
    /// </summary>
    private void UpdateStatusText() {
        if (statusText == null || navigationController == null) return;

        if (!navigationController.IsNavigating) {
            statusText.text = "Select a destination";
            statusText.color = Color.gray;
        } else if (navigationController.currentFloor != navigationController.TargetFloor) {
            statusText.text = $"Go to stairs → Floor {navigationController.TargetFloor}";
            statusText.color = Color.yellow;
        } else if (navigationController.HasValidPath) {
            if (navigationController.DistanceToTarget < 2f) {
                statusText.text = "You're almost there!";
                statusText.color = Color.green;
            } else {
                statusText.text = "Follow the path";
                statusText.color = Color.cyan;
            }
        } else {
            statusText.text = "Calculating path...";
            statusText.color = Color.yellow;
        }
    }

    /// <summary>
    /// Show error message
    /// </summary>
    public void ShowError(string message) {
        if (errorPanel == null || errorMessageText == null) {
            Debug.LogWarning($"Navigation Error: {message}");
            return;
        }

        errorMessageText.text = message;
        errorPanel.SetActive(true);
        errorTimer = errorDisplayDuration;
        isShowingError = true;

        Debug.LogWarning($"Navigation Error: {message}");
    }

    /// <summary>
    /// Hide error message
    /// </summary>
    public void HideError() {
        if (errorPanel != null) {
            errorPanel.SetActive(false);
        }
        isShowingError = false;
    }

    /// <summary>
    /// Show arrival notification
    /// </summary>
    private void ShowArrivalNotification() {
        if (arrivalPanel == null || arrivalMessageText == null) return;

        arrivalMessageText.text = $"You've arrived at {navigationController.CurrentTargetName}!";
        arrivalPanel.SetActive(true);

        // Auto-hide after 3 seconds
        Invoke(nameof(HideArrivalNotification), 3f);
    }

    /// <summary>
    /// Hide arrival notification
    /// </summary>
    public void HideArrivalNotification() {
        if (arrivalPanel != null) {
            arrivalPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Update floor display
    /// </summary>
    private void UpdateFloorDisplay(int floor) {
        if (currentFloorText != null) {
            currentFloorText.text = $"Floor {floor}";
        }
    }

    /// <summary>
    /// Called when navigation stops
    /// </summary>
    public void OnNavigationStopped() {
        if (navigationInfoPanel != null) {
            navigationInfoPanel.SetActive(false);
        }
    }

    private void OnDestroy() {
        // Unsubscribe from events
        if (navigationController != null) {
            navigationController.OnNavigationUpdate.RemoveListener(UpdateNavigationUI);
            navigationController.OnNavigationError.RemoveListener(ShowError);
            navigationController.OnTargetReached.RemoveListener(ShowArrivalNotification);
            navigationController.OnFloorChanged.RemoveListener(UpdateFloorDisplay);
        }
    }
}
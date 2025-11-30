using UnityEngine;
public class AppFlowController : MonoBehaviour
{
    public static AppFlowController Instance;

    [Header("Panels")]
    public GameObject WelcomePanel;
    public GameObject QRCodePanel;
    public GameObject NavigationPanel;

    [Header("References")]
    public SearchBarQR searchBar;
    public QRUIController qrUI;
    public NavigationController navigationController;

    private string selectedTargetName;
    private AnchorManager.AnchorData currentAnchor;

    void Awake()
    {
        Instance = this;
        ShowWelcome();
    }

    // ---------- PANEL FLOW ----------

    public void ShowWelcome()
    {
        if (UITransitionManager.Instance != null)
            UITransitionManager.Instance.FadeSwitch(QRCodePanel, WelcomePanel);
        else
        {
        WelcomePanel.SetActive(true);
        QRCodePanel.SetActive(false);
        NavigationPanel.SetActive(false);
        }
    }

    public void ShowQRCodePanel()
    {
        if (UITransitionManager.Instance != null)
            UITransitionManager.Instance.FadeSwitch(WelcomePanel, QRCodePanel);
        else
        {
        WelcomePanel.SetActive(false);
        QRCodePanel.SetActive(true);
        NavigationPanel.SetActive(false);
        }
    }

    public void ShowNavigationPanel(string targetName)
    {
        if (UITransitionManager.Instance != null)
            UITransitionManager.Instance.FadeSwitch(QRCodePanel, NavigationPanel);
        else
        {
            WelcomePanel.SetActive(false);
            QRCodePanel.SetActive(false);
            NavigationPanel.SetActive(true);
        }

        selectedTargetName = targetName;
        isNavigating = true; // Mark navigation as active

        if (navigationController != null)
            navigationController.BeginNavigation(currentAnchor, targetName);

    }

    // ---------- CALLED FROM OTHER SCRIPTS ----------
 // stores last scanned anchor
    private bool isNavigating = false; // Track if navigation is active

// ---------- QR Payload Class ----------
    [System.Serializable]
    public class QRPayload
    {
        public string type;
        public string buildingId;
        public string anchorId;
        public int floor;
    }



    public void OnDestinationSelected(string targetName)
    {
        selectedTargetName = targetName;
        Debug.Log($"Destination selected: {targetName}");
    }

    public void OnQRCodeScanned(string qrResult)
    {
        Debug.Log($"📷 QR scanned: {qrResult}");

        // Try to parse JSON
        QRPayload payload;

        try
        {
            payload = JsonUtility.FromJson<QRPayload>(qrResult);
        }
        catch
        {
            Debug.LogError("❌ QR is not valid JSON.");
            return;
        }

        if (payload == null || string.IsNullOrEmpty(payload.anchorId))
        {
            Debug.LogError("❌ QR does not contain anchorId.");
            return;
        }

        // Find anchor in AnchorManager
        currentAnchor = AnchorManager.Instance.FindAnchor(payload.anchorId);

        if (currentAnchor == null)
        {
            Debug.LogError($"❌ No Anchor found for ID: {payload.anchorId}");
            return;
        }

        Debug.Log($"✅ QR Recenter: User position updated to {currentAnchor.AnchorId}");

        // Check if user is currently navigating
        if (isNavigating && !string.IsNullOrEmpty(selectedTargetName))
        {
            Debug.Log($"🔄 Recentering active navigation to new anchor position");

            // Update navigation with new anchor while keeping same target
            if (NavigationController.Instance != null)
                NavigationController.Instance.RecenterNavigation(currentAnchor, selectedTargetName);

            return;
        }

        // Must have selected a target BEFORE scanning for initial navigation
        if (string.IsNullOrEmpty(selectedTargetName))
        {
            Debug.Log("ℹ️ QR scanned but no destination selected - anchor saved for future navigation");
            return;
        }

        // Switch to navigation panel
        ShowNavigationPanel(selectedTargetName);

        // Begin navigation from new anchor position
        if (NavigationController.Instance != null)
            NavigationController.Instance.BeginNavigation(currentAnchor, selectedTargetName);
    }


    public void StopNavigation()
    {
        if (navigationController !=null)
        {
            NavigationPanel.SetActive(false);
            navigationController.StopNavigation();
        }

        // Reset all navigation state to fresh start
        ResetNavigationState();
        isNavigating = false; // Mark navigation as inactive

        ShowWelcome();
    }

    public void ChangeLocation()
    {
        if (NavigationPanel != null)
        {
           NavigationPanel.SetActive(false);
        }

        // Reset all navigation state to fresh start
        ResetNavigationState();
        isNavigating = false; // Mark navigation as inactive

        ShowQRCodePanel();
    }

    private void ResetNavigationState()
    {
        // Reset QR scanner UI using dedicated reset method
        if (qrUI != null)
        {
            qrUI.ResetScannerUI();
        }

        // Clear search bar selection
        if (searchBar != null)
        {
            searchBar.ClearSelection();
        }

        // Reset stored navigation data
        selectedTargetName = null;
        currentAnchor = null;
    }

}

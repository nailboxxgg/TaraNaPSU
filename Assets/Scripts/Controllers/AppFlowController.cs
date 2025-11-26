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
        if (navigationController != null)
            navigationController.BeginNavigation(currentAnchor, targetName);

    }

    // ---------- CALLED FROM OTHER SCRIPTS ----------
 // stores last scanned anchor

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
        QRPayload payload = null;

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

        Debug.Log($"✅ Anchor Scanned: {currentAnchor.AnchorId}");

        // Must have selected a target BEFORE scanning
        if (string.IsNullOrEmpty(selectedTargetName))
        {
            Debug.LogWarning("⚠️ User has not selected a destination yet.");
            return;
        }

    // Switch to navigation panel
    ShowNavigationPanel(selectedTargetName);

    // Begin navigation
    if (NavigationController.Instance != null)
        NavigationController.Instance.BeginNavigation(currentAnchor, selectedTargetName);
}


    public void StopNavigation()
    {
        if (navigationController !=null)
        {
            NavigationPanel.SetActive(false);
            navigationController.EndNavigation();
        }

        // Reset all navigation state to fresh start
        ResetNavigationState();

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

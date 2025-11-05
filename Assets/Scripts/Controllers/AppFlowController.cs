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
            navigationController.BeginNavigation(targetName);
    }

    // ---------- CALLED FROM OTHER SCRIPTS ----------

    public void OnDestinationSelected(string targetName)
    {
        selectedTargetName = targetName;
        Debug.Log($"Destination selected: {targetName}");
    }

    public void OnQRCodeScanned(string qrResult)
    {
        Debug.Log($"QR scanned: {qrResult}");
        if (selectedTargetName == qrResult)
        {
            ShowNavigationPanel(selectedTargetName);
        }
        else
        {
            Debug.LogWarning("QR doesn’t match selected destination!");
        }
    }

    public void StopNavigation()
    {
        ShowWelcome();
    }
}

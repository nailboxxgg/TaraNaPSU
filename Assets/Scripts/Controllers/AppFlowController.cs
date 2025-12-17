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

    // State for multi-floor navigation
    private bool isWaitingForFloorChange = false;
    private string finalDestinationName;
    private AnchorManager.StairPair pendingStairPair;

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
        
        // Check for multi-floor requirement
        if (!CheckMultiFloorNavigation(targetName))
        {
            // Standard Navigation
            if (navigationController != null)
                navigationController.BeginNavigation(currentAnchor, targetName);
        }
    }

    private bool CheckMultiFloorNavigation(string targetName)
    {
        // Safety check
        if (currentAnchor == null) return false;
        
        // Get target data
        if (!TargetManager.Instance.TryGetTarget(targetName, out var targetData))
            return false;

        // If on same floor, standard navigation
        if (currentAnchor.Floor == targetData.FloorNumber)
            return false;

        Debug.Log($"[AppFlow] Multi-floor detected: Anchor F{currentAnchor.Floor} -> Target F{targetData.FloorNumber}");

        // Find nearest stair
        var stairPair = AnchorManager.Instance.FindNearestStair(
            currentAnchor.BuildingId, 
            currentAnchor.Floor, 
            targetData.FloorNumber, 
            currentAnchor.PositionVector
        );

        if (stairPair == null)
        {
            Debug.LogError("❌ No stair pair found for this floor transition!");
            return false; // Fallback to standard (will likely fail pathing but keeps app running)
        }

        // Start Navigation to the specific Stair Anchor (Bottom/Start of stair)
        // If we are going UP (0->1), dest is Bottom. If DOWN (1->0), dest is Top.
        // Based on AnchorManager, 'Bottom' is Floor 0, 'Top' is Floor 1.
        var intermediateStair = (currentAnchor.Floor < targetData.FloorNumber) ? stairPair.Bottom : stairPair.Top;
        
        // AUTOMATED FLOW: Route user to the nearest stairway marker.
        // They do NOT need to scan this stair marker; it is just a navigation waypoint.
        // Once they arrive here, OnStairArrived triggers the floor change prompt.
        Debug.Log($"[AppFlow] Routing to intermediate stair: {intermediateStair.AnchorId}");

        pendingStairPair = stairPair;
        finalDestinationName = targetName;

        if (navigationController != null)
        {
            // Subscribe to arrival
            navigationController.OnArrival -= OnStairArrived; // Ensure no double sub
            navigationController.OnArrival += OnStairArrived;
            
            navigationController.BeginNavigation(currentAnchor, intermediateStair);
        }

        return true;
    }

    private void OnStairArrived()
    {
        // Unsubscribe
        if (navigationController != null)
            navigationController.OnArrival -= OnStairArrived;

        Debug.Log("✅ Arrived at stair. Prompting user to change floors.");

        // Visual Feedback: Show prompt (using navigation monitor if available, or just console/overlay)
        // For now, we utilize the status controller text via NavigationController
        if (NavigationController.Instance.statusController != null)
        {
            NavigationController.Instance.statusController.SetNavigationInfo("FLOOR TRANSFER", "Go Upstairs -> Scan 1st Floor QR");
        }

        // Logic: Enable scanner or prompt user to open it?
        // User request: "system prompts the user to go upstairs first and then they will see a new QR code"
        // We should switch to Scan Mode but keep context
        isWaitingForFloorChange = true;
        
        // Optionally switch back to QR panel UI after a short delay or immediately
        // ShowQRCodePanel(); // This might be too abrupt. 
        // Let's explicitly overlay a "Scan Next Floor" message or just transition:
        // Ideally we show a UI Modal. Lacking that, we go to QR Panel with a specific "Scanning for Floor 1..." message.
        
        // Slight delay to read "Arrived" then switch
        StartCoroutine(SwitchToScanForFloorChange());
    }

    private System.Collections.IEnumerator SwitchToScanForFloorChange()
    {
        yield return new WaitForSeconds(2.0f);
        ShowQRCodePanel();
        if (qrUI != null)
        {
            qrUI.titleText.text = "Floor Transition";
            qrUI.subtitleText.text = "Go upstairs & scan marker";
        }
    }

    // ---------- CALLED FROM OTHER SCRIPTS ----------
 
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
        string lookupId = payload.anchorId != null ? payload.anchorId.Trim() : "";
        Debug.Log($"[AppFlowController] Looking up anchor: '{lookupId}' (Original: '{payload.anchorId}')");
        currentAnchor = AnchorManager.Instance.FindAnchor(lookupId);

        if (currentAnchor == null)
        {
            Debug.LogError($"❌ No Anchor found for ID: {payload.anchorId}");
            return;
        }

        Debug.Log($"✅ Anchor Scanned: {currentAnchor.AnchorId}");

        // HANDLE MULTI-FLOOR TRANSITION SCAN
        if (isWaitingForFloorChange)
        {
            // Verify this is the correct next-floor anchor
            // We expect the anchor to be on the target floor of the original request
            if (TargetManager.Instance.TryGetTarget(finalDestinationName, out var targetData))
            {
                if (currentAnchor.Floor == targetData.FloorNumber)
                {
                    Debug.Log("✅ Floor transition confirmed! Resuming navigation...");
                    isWaitingForFloorChange = false;
                    
                    // Resume to final target
                    ShowNavigationPanel(finalDestinationName);
                    // BeginNavigation will warp to the new currentAnchor automatically
                    // Note: BeginNavigation(currentAnchor, finalDestinationName) handles the request.
                    return; 
                }
                else
                {
                    Debug.LogWarning($"⚠️ Wrong floor scanned. Expected Floor {targetData.FloorNumber}, got {currentAnchor.Floor}");
                    if (qrUI != null) qrUI.subtitleText.text = "Wrong floor! Go to Floor " + targetData.FloorNumber;
                    return;
                }
            }
        }

        // Must have selected a target BEFORE scanning (Standard Flow)
        if (string.IsNullOrEmpty(selectedTargetName))
        {
            Debug.LogWarning("⚠️ User has not selected a destination yet.");
            return;
        }

        // Switch to navigation panel
        ShowNavigationPanel(selectedTargetName);
    } // End of OnQRCodeScanned


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

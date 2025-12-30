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
    private AnchorData currentAnchor;

    void Awake()
    {
        Instance = this;
        ShowWelcome();
    }

    

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

    
    private bool isWaitingForFloorChange = false;
    private string finalDestinationName;
    private StairPair pendingStairPair;

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
        
        
        if (!CheckMultiFloorNavigation(targetName))
        {
            
            if (navigationController != null)
                navigationController.BeginNavigation(currentAnchor, targetName);
        }
    }

    private bool CheckMultiFloorNavigation(string targetName)
    {
        
        if (currentAnchor == null) return false;
        
        
        if (!TargetManager.Instance.TryGetTarget(targetName, out var targetData))
            return false;

        
        if (currentAnchor.Floor == targetData.FloorNumber)
            return false;

        Debug.Log($"[AppFlow] Multi-floor detected: Anchor F{currentAnchor.Floor} -> Target F{targetData.FloorNumber}");

        
        var stairPair = AnchorManager.Instance.FindNearestStair(
            currentAnchor.BuildingId, 
            currentAnchor.Floor, 
            targetData.FloorNumber, 
            currentAnchor.PositionVector
        );

        if (stairPair == null)
        {
            Debug.LogError("❌ No stair pair found for this floor transition!");
            return false; 
        }

        var intermediateStair = (currentAnchor.Floor < targetData.FloorNumber) ? stairPair.Bottom : stairPair.Top;

        Debug.Log($"[AppFlow] Routing to intermediate stair: {intermediateStair.AnchorId}");

        pendingStairPair = stairPair;
        finalDestinationName = targetName;

        if (navigationController != null)
        {
            
            navigationController.OnArrival -= OnStairArrived; 
            navigationController.OnArrival += OnStairArrived;
            
            navigationController.BeginNavigation(currentAnchor, intermediateStair);
        }

        return true;
    }

    private void OnStairArrived()
    {
        
        if (navigationController != null)
            navigationController.OnArrival -= OnStairArrived;

        Debug.Log("✅ Arrived at stair. Prompting user to change floors.");


        if (NavigationController.Instance.statusController != null)
        {
            NavigationController.Instance.statusController.SetNavigationInfo("FLOOR TRANSFER", "Go Upstairs -> Scan 1st Floor QR");
        }


        isWaitingForFloorChange = true;
        

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

    
 
    
    

    public void OnDestinationSelected(string targetName)
    {
        selectedTargetName = targetName;
        Debug.Log($"Destination selected: {targetName}");
    }

    public void OnQRCodeScanned(string qrResult)
    {
        Debug.Log($"📷 QR scanned: {qrResult}");

        
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

        
        string lookupId = payload.anchorId != null ? payload.anchorId.Trim() : "";
        Debug.Log($"[AppFlowController] Looking up anchor: '{lookupId}' (Original: '{payload.anchorId}')");
        currentAnchor = AnchorManager.Instance.FindAnchor(lookupId);

        if (currentAnchor == null)
        {
            Debug.LogError($"❌ No Anchor found for ID: {payload.anchorId}");
            return;
        }

        Debug.Log($"✅ Anchor Scanned: {currentAnchor.AnchorId}");

        
        if (isWaitingForFloorChange)
        {
            
            
            if (TargetManager.Instance.TryGetTarget(finalDestinationName, out var targetData))
            {
                if (currentAnchor.Floor == targetData.FloorNumber)
                {
                    Debug.Log("✅ Floor transition confirmed! Resuming navigation...");
                    isWaitingForFloorChange = false;
                    
                    
                    ShowNavigationPanel(finalDestinationName);
                    
                    
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

        
        if (string.IsNullOrEmpty(selectedTargetName))
        {
            Debug.LogWarning("⚠️ User has not selected a destination yet.");
            return;
        }

        
        ShowNavigationPanel(selectedTargetName);
    } 


    public void StopNavigation()
    {
        if (navigationController !=null)
        {
            NavigationPanel.SetActive(false);
            navigationController.EndNavigation();
        }

        
        ResetNavigationState();

        ShowWelcome();
    }

    public void ChangeLocation()
    {
        if (NavigationPanel != null)
        {
           NavigationPanel.SetActive(false);
        }

        
        ResetNavigationState();

        ShowQRCodePanel();
    }

    private void ResetNavigationState()
    {
        
        if (qrUI != null)
        {
            qrUI.ResetScannerUI();
        }

        
        if (searchBar != null)
        {
            searchBar.ClearSelection();
        }

        
        selectedTargetName = null;
        currentAnchor = null;
    }

}


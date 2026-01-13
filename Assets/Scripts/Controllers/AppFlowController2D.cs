using UnityEngine;

public class AppFlowController2D : MonoBehaviour
{
    public static AppFlowController2D Instance;

    [Header("Panels")]
    public GameObject WelcomePanel;
    public GameObject MapPanel;

    [Header("References")]
    public LocationSearchBar searchBar;
    public StartPointSelector startPointSelector;
    public Navigation2DController navigationController;
    public Map2DController mapController;
    public PlayerGuideController playerGuide;

    private string selectedStartPoint;
    private string selectedDestination;
    private Vector3 startPosition;
    private Vector3 destinationPosition;
    private int targetFloor;
    private int startFloor;

    void Awake()
    {
        Instance = this;
        ShowWelcome();
    }

    public void ShowWelcome()
    {
        WelcomePanel.SetActive(true);
        MapPanel.SetActive(false);
    }

    public void ShowMap()
    {
        WelcomePanel.SetActive(false);
        MapPanel.SetActive(true);
    }

    public void OnStartPointSelected(string anchorId, Vector3 position, int floor)
    {
        selectedStartPoint = anchorId;
        startPosition = position;
        startFloor = floor;
        Debug.Log($"[AppFlow2D] Start point selected: {anchorId} on floor {floor}");

        if (mapController != null)
            mapController.SetUserPosition(position);

        if (playerGuide != null)
            playerGuide.TeleportTo(position);

        Debug.Log($"[AppFlow2D] Start point ready: {anchorId}");
    }

    public void OnDestinationSelected(string targetName, Vector3 position, int floor)
    {
        selectedDestination = targetName;
        destinationPosition = position;
        targetFloor = floor;
        Debug.Log($"[AppFlow2D] Destination selected: {targetName} on floor {floor}");

        if (mapController != null)
            mapController.SetDestination(position, targetName);

        Debug.Log($"[AppFlow2D] Destination ready: {targetName}");
    }

    public void StartNavigation()
    {
        ShowMap();

        if (navigationController != null)
        {
            navigationController.BeginNavigation(startPosition, destinationPosition, selectedDestination);
            
            // Start the player guide "simulation" walk
            if (playerGuide != null)
            {
                playerGuide.StartAutoWalk(navigationController.GetPathCorners());
            }
        }

        Debug.Log($"[AppFlow2D] Navigation started: {selectedStartPoint} → {selectedDestination}");
    }

    public void SmartStart()
    {
        if (string.IsNullOrEmpty(selectedDestination))
        {
            Debug.LogWarning("[AppFlow2D] Cannot start: No destination selected.");
            return;
        }

        if (string.IsNullOrEmpty(selectedStartPoint))
        {
            Debug.Log("[AppFlow2D] No start point selected. Opening scanner for localization.");
            if (QRUIController.Instance != null)
                QRUIController.Instance.OpenScanner();
        }
        else
        {
            StartNavigation();
        }
    }

    // ---------- QR CODE LOCALIZATION ----------

    [System.Serializable]
    public class QRPayload
    {
        public string anchorId;
    }

    public void OnQRCodeScanned(string qrResult)
    {
        Debug.Log($"📷 QR scanned: {qrResult}");

        QRPayload payload;
        try {
            payload = JsonUtility.FromJson<QRPayload>(qrResult);
        } catch {
            Debug.LogError("❌ QR is not valid JSON.");
            return;
        }

        if (payload == null || string.IsNullOrEmpty(payload.anchorId)) {
            Debug.LogError("❌ QR does not contain anchorId.");
            return;
        }

        AnchorManager.AnchorData anchor = AnchorManager.Instance.FindAnchor(payload.anchorId);
        if (anchor == null) {
            Debug.LogError($"❌ No Anchor found for ID: {payload.anchorId}");
            return;
        }

        Debug.Log($"✅ QR Localized: User moved to {anchor.AnchorId}");

        // Update Position
        startPosition = anchor.PositionVector;
        startFloor = anchor.Floor;

        if (mapController != null) {
            mapController.SetUserPosition(startPosition);
            mapController.ShowFloor(startFloor);
        }

        if (playerGuide != null) {
            playerGuide.TeleportTo(startPosition);
        }

        // If already navigating, recalculate path from new position
        if (navigationController != null && navigationController.IsNavigating) {
            navigationController.BeginNavigation(startPosition, destinationPosition, selectedDestination);
            if (playerGuide != null) {
                playerGuide.StartAutoWalk(navigationController.GetPathCorners());
            }
        }
    }

    public void StopNavigation()
    {
        if (navigationController != null)
            navigationController.StopNavigation();

        if (mapController != null)
            mapController.ClearMarkers();

        if (playerGuide != null)
            playerGuide.StopAutoWalk();

        ResetState();
        ShowWelcome();
    }

    public void ChangeDestination()
    {
        selectedDestination = null;
        destinationPosition = Vector3.zero;

        if (navigationController != null)
            navigationController.StopNavigation();

        if (searchBar != null)
            searchBar.ClearSelection();
    }

    private void ResetState()
    {
        selectedStartPoint = null;
        selectedDestination = null;
        startPosition = Vector3.zero;
        destinationPosition = Vector3.zero;

        if (searchBar != null)
            searchBar.ClearSelection();

        if (startPointSelector != null)
            startPointSelector.ClearSelection();
    }

    public bool HasStartPoint => !string.IsNullOrEmpty(selectedStartPoint);
    public bool HasDestination => !string.IsNullOrEmpty(selectedDestination);
    public int TargetFloor => targetFloor;
    public int StartFloor => startFloor;
}

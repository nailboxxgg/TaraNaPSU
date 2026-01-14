using UnityEngine;

public class AppFlowController2D : MonoBehaviour
{
    private static AppFlowController2D _instance;
    public static AppFlowController2D Instance
    {
        get
        {
            if (_instance == null) _instance = FindObjectOfType<AppFlowController2D>();
            return _instance;
        }
    }

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

    // --- Chained Navigation Support ---
    private Vector3 checkpointPosition;
    private bool isNavigatingToCheckpoint = false;
    private AnchorManager.AnchorData currentStairTarget;

    void Awake()
    {
        _instance = this;
        ShowWelcome();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBackAction();
        }

        // Check for checkpoint arrival
        if (isNavigatingToCheckpoint && playerGuide != null)
        {
            float dist = Vector3.Distance(playerGuide.transform.position, checkpointPosition);
            if (dist < 1.0f) // Within 1 meter of stairs
            {
                OnCheckpointReached();
            }
        }
    }

    private void OnCheckpointReached()
    {
        isNavigatingToCheckpoint = false;
        
        if (playerGuide != null)
            playerGuide.StopAutoWalk();

        if (mapController != null)
            mapController.SetFollowTarget(null);

        Debug.Log($"[AppFlow2D] Checkpoint reached: {currentStairTarget?.AnchorId}. Ready for floor change.");
        // UI prompts are handled by NavigationStatusDisplay2D monitoring the state
    }

    private void HandleBackAction()
    {
        // If MapPanel is active, it means we are either searching or navigating.
        // Return to WelcomePanel and stop any active navigation.
        if (MapPanel.activeSelf)
        {
            Debug.Log("[AppFlow2D] Back pressed: Returning to Welcome Panel.");
            StopNavigation();
        }
        else if (WelcomePanel.activeSelf)
        {
            // If we are already on the WelcomePanel, exit the app.
            Debug.Log("[AppFlow2D] Back pressed: Exiting application.");
            Application.Quit();
        }
    }

    /// <summary>
    /// Standard floor mapping: Campus is 0, Buildings map JSON 0->System 1, JSON 1->System 2.
    /// </summary>
    public int MapFloor(string buildingId, int jsonFloor)
    {
        if (buildingId?.ToLower() == "campus") return 0;
        return jsonFloor + 1;
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

        // --- TESTING PHASE: Dynamic Discovery ---
        // Prioritize existing static instances (singletons) to avoid duplicate/destroyed object issues.
        if (mapController == null)
            mapController = Map2DController.Instance;
        
        if (navigationController == null)
            navigationController = Navigation2DController.Instance;

        // Fallback: If still null, try finding specifically in the MapPanel hierarchy
        if (mapController == null) mapController = MapPanel.GetComponentInChildren<Map2DController>(true);
        if (navigationController == null) navigationController = MapPanel.GetComponentInChildren<Navigation2DController>(true);
    }

    public void OnStartPointSelected(string anchorId, Vector3 position, int floor)
    {
        Debug.Log($"[AppFlow2D] >>> DATA RECEIVED: Start Point = {anchorId} (Floor {floor})");
        selectedStartPoint = anchorId;
        startPosition = position;
        startFloor = floor;

        if (mapController != null)
            mapController.SetUserPosition(position);

        if (playerGuide != null)
            playerGuide.TeleportTo(position);

        Debug.Log($"[AppFlow2D] Start point ready: {anchorId}");
    }

    public void OnDestinationSelected(string targetName, Vector3 position, int floor)
    {
        Debug.Log($"[AppFlow2D] >>> DATA RECEIVED: Destination = {targetName} (Floor {floor})");
        selectedDestination = targetName;
        destinationPosition = position;
        targetFloor = floor;

        if (mapController != null)
            mapController.SetDestination(position, targetName);

        Debug.Log($"[AppFlow2D] Destination ready: {targetName}");
    }

    public void StartNavigation()
    {
        ShowMap();

        if (navigationController != null)
        {
            Vector3 navTarget = destinationPosition;
            string navName = selectedDestination;

            // --- Multi-Floor Reroute Logic ---
            if (startFloor != targetFloor && AnchorManager.Instance != null)
            {
                Debug.Log($"[AppFlow2D] Multi-floor detected: {startFloor} -> {targetFloor}. Finding nearest stairway.");
                
                // Convert System Floor back to raw JSON Floor for AnchorManager lookup
                // System 0 -> JSON 0 (Campus)
                // System 1 -> JSON 0 (Ground)
                // System 2 -> JSON 1 (1st Floor)
                int jsonFloor = (startFloor == 0) ? 0 : startFloor - 1;
                string searchBuilding = (startFloor == 0) ? "Campus" : "B1"; 
                
                var stairPair = AnchorManager.Instance.FindNearestStairAtFloor(searchBuilding, jsonFloor, startPosition);
                if (stairPair != null && stairPair.IsValid)
                {
                    // Select the end of the stair that matches our current JSON floor
                    currentStairTarget = (stairPair.Bottom.Floor == jsonFloor) ? stairPair.Bottom : stairPair.Top;
                    checkpointPosition = currentStairTarget.PositionVector;
                    isNavigatingToCheckpoint = true;
                    
                    navTarget = checkpointPosition;
                    navName = $"Stairway to Floor {targetFloor}";
                    Debug.Log($"[AppFlow2D] Rerouting to checkpoint: {navName} ({currentStairTarget.AnchorId}) at {navTarget}");
                }
                else
                {
                    Debug.LogWarning($"[AppFlow2D] No stair path found for JSON floor {jsonFloor} in building {searchBuilding}!");
                }
            }
            else
            {
                isNavigatingToCheckpoint = false;
            }

            Debug.Log($"[AppFlow2D] Beginning navigation flow from {startPosition} to {navTarget}");
            navigationController.BeginNavigation(startPosition, navTarget, navName);
            
            // Start the player guide "simulation" walk
            if (playerGuide != null)
            {
                playerGuide.StartAutoWalk(navigationController.GetPathCorners());

                // Enable Camera Follow during simulation
                if (mapController != null)
                    mapController.SetFollowTarget(playerGuide.transform);
            }
        }
        else
        {
            Debug.LogError("[AppFlow2D] NavigationController is MISSING! Navigation cannot start.");
        }

        Debug.Log($"[AppFlow2D] Navigation started log: {selectedStartPoint} → {selectedDestination}");
    }

    public void SmartStart()
    {
        Debug.Log($"[AppFlow2D] SmartStart clicked. Start: {selectedStartPoint}, Dest: {selectedDestination}");

        if (string.IsNullOrEmpty(selectedDestination))
        {
            Debug.LogWarning("[AppFlow2D] Cannot start: No destination selected.");
            // You might want to trigger a UI popup here for the user
            return;
        }

        if (string.IsNullOrEmpty(selectedStartPoint))
        {
            Debug.Log("[AppFlow2D] No start point selected. Opening scanner for localization.");
            if (QRUIController.Instance != null)
                QRUIController.Instance.OpenScanner();
            else
                Debug.LogError("[AppFlow2D] Localization failed: Start point is empty and QR scanner is unavailable.");
        }
        else
        {
            Debug.Log($"[AppFlow2D] Flow confirmed: {selectedStartPoint} -> {selectedDestination}. Starting...");
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
        startFloor = MapFloor(anchor.BuildingId, anchor.Floor);

        if (mapController != null) {
            mapController.SetUserPosition(startPosition);
            mapController.ShowFloor(startFloor);
        }

        if (playerGuide != null) {
            playerGuide.TeleportTo(startPosition);
        }

        // If already navigating, recalculate path from new position
        if (navigationController != null && navigationController.IsNavigating)
        {
            Vector3 finalTarget = destinationPosition;
            string finalName = selectedDestination;

            // If we were headed to a checkpoint and we've reached the target floor,
            // we can now resume to the actual destination.
            if (isNavigatingToCheckpoint && startFloor == targetFloor)
            {
                Debug.Log("[AppFlow2D] Checkpoint reached & floor matched. Resuming to final destination.");
                isNavigatingToCheckpoint = false;
            }
            else if (isNavigatingToCheckpoint)
            {
                // Still need to go to checkpoint or we are on an intermediate floor?
                // For now, assume first scannable on target floor = resume.
                finalTarget = checkpointPosition;
                finalName = $"Stairway to Floor {targetFloor}";
            }

            navigationController.BeginNavigation(startPosition, finalTarget, finalName);
            if (playerGuide != null)
            {
                playerGuide.StartAutoWalk(navigationController.GetPathCorners());
                if (mapController != null)
                    mapController.SetFollowTarget(playerGuide.transform);
            }
        }
    }

    public void StopNavigation()
    {
        if (navigationController != null)
            navigationController.StopNavigation();

        if (mapController != null)
        {
            mapController.ClearMarkers();
            mapController.SetFollowTarget(null); // Stop following
        }

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

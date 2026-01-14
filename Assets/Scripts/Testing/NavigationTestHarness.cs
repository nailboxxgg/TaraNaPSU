using UnityEngine;

public class NavigationTestHarness : MonoBehaviour
{
    [Header("Gate Main Entrance (Start)")]
    public string startId = "Gate-Main-Entrance";
    public Vector3 startPos = Vector3.zero;
    public int startFloor = 0;

    [Header("Registrar's Office (Destination)")]
    public string destName = "Registrar's Office";
    public Vector3 destPos = new Vector3(47.08f, 1f, 48.07f);
    public int destFloor = 0;

    [ContextMenu("Run Gate to Registrar Test")]
    public void RunSimulationTest()
    {
        if (AppFlowController2D.Instance == null)
        {
            Debug.LogError("[TestHarness] AppFlowController2D Instance not found!");
            return;
        }

        Debug.Log("[TestHarness] Starting Simulated Navigation: Gate -> Registrar");

        // 1. Simulate Start Point Selection
        AppFlowController2D.Instance.OnStartPointSelected(startId, startPos, startFloor);

        // 2. Simulate Destination Selection
        AppFlowController2D.Instance.OnDestinationSelected(destName, destPos, destFloor);

        // 3. Trigger Navigation Flow
        AppFlowController2D.Instance.StartNavigation();

        Debug.Log("[TestHarness] Simulation triggered successfully.");
    }

    [ContextMenu("Run Multi-Floor Chained Test")]
    public void RunMultiFloorChainedTest()
    {
        if (AppFlowController2D.Instance == null)
        {
            Debug.LogError("[TestHarness] AppFlowController2D Instance not found!");
            return;
        }

        Debug.Log("[TestHarness] Starting Multi-Floor Test: Gate (F0) -> ICTMO (F2)");

        // 1. Start at Gate (Floor 0)
        AppFlowController2D.Instance.OnStartPointSelected("Gate-Main-Entrance", Vector3.zero, 0);

        // 2. Target ICTMO (First Floor in JSON is Floor 2 in App)
        // ICTMO/MIS: { "x": 5, "y": 1, "z": -5.5 }
        AppFlowController2D.Instance.OnDestinationSelected("ICTMO/MIS Office", new Vector3(5, 1, -5.5f), 2);

        // 3. Start
        AppFlowController2D.Instance.StartNavigation();

        Debug.Log("[TestHarness] Step 1 Triggered: Navigating to Stairway Checkpoint.");
    }

    [ContextMenu("Simulate Floor Change QR")]
    public void SimulateFloorChangeQR()
    {
        if (AppFlowController2D.Instance == null) return;

        // Simulate scanning a QR code on Floor 2 (1st Floor)
        // This should trigger the resumption logic in AppFlowController2D
        string qrJson = "{\"anchorId\": \"B1-First-Floor-Stairway Marker 1\"}";
        AppFlowController2D.Instance.OnQRCodeScanned(qrJson);

        Debug.Log("[TestHarness] Step 2 Triggered: QR Scanned on new floor. Resuming navigation.");
    }

    [ContextMenu("Verify Map Panel Setup")]
    public void VerifyMapPanelSetup()
    {
        if (AppFlowController2D.Instance == null)
        {
            Debug.LogError("[TestHarness] AppFlowController2D Instance not found!");
            return;
        }

        GameObject mapPanel = AppFlowController2D.Instance.MapPanel;
        if (mapPanel == null)
        {
            Debug.LogError("[TestHarness] MapPanel reference is missing in AppFlowController2D!");
            return;
        }

        var mapC = mapPanel.GetComponentInChildren<Map2DController>(true);
        var navC = mapPanel.GetComponentInChildren<Navigation2DController>(true);

        Debug.Log($"[TestHarness] Verification Results for {mapPanel.name}:");
        Debug.Log($"- Map2DController found on panel: {mapC != null}");
        Debug.Log($"- Navigation2DController found on panel: {navC != null}");

        // Duplicate Check
        var allMaps = FindObjectsOfType<Map2DController>(true);
        var allNavs = FindObjectsOfType<Navigation2DController>(true);

        if (allMaps.Length > 1) Debug.LogWarning($"[TestHarness] ⚠️ DUPLICATE DETECTED: Found {allMaps.Length} Map2DControllers in scene. This can cause the MissingReferenceException if the wrong one is destroyed.");
        if (allNavs.Length > 1) Debug.LogWarning($"[TestHarness] ⚠️ DUPLICATE DETECTED: Found {allNavs.Length} Navigation2DControllers in scene. This can cause the MissingReferenceException if the wrong one is destroyed.");

        if (mapC != null && navC != null && allMaps.Length == 1 && allNavs.Length == 1)
        {
            Debug.Log("[TestHarness] ✅ SUCCESS: Controllers are correctly placed and unique.");
        }
        else if (mapC != null && navC != null)
        {
            Debug.LogWarning("[TestHarness] ⚠️ ATTENTION: Controllers found on panel, but duplicates exist. Please remove the old ones from the parent object.");
        }
        else
        {
            Debug.LogWarning("[TestHarness] ⚠️ ATTENTION: Some controllers are missing from the MapPanel.");
        }
    }
}

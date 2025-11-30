using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// QR Recenter Targets - Empty GameObjects that represent specific locations
/// for QR code recentering functionality as shown in transcript.
/// These act as virtual reset points when QR codes are scanned.
/// </summary>
public class QRRecenterTargets : MonoBehaviour
{
    [Header("Recenter Target References")]
    public Transform livingRoomTarget;
    public Transform mainEntranceTarget;
    public Transform kitchenTarget;

    [Header("Debug")]
    public bool enableDebugMode = true;
    public KeyCode testKey = KeyCode.Space;

    /// <summary>
    /// Find recenter target by name
    /// </summary>
    public Transform FindTargetByName(string targetName)
    {
        switch (targetName.ToLower())
        {
            case "living room":
            case "start point":
                return livingRoomTarget;
            case "main entrance":
                return mainEntranceTarget;
            case "kitchen":
                return kitchenTarget;
            default:
                Debug.LogWarning($"[QRRecenterTargets] No target found for: {targetName}");
                return null;
        }
    }

    /// <summary>
    /// Get all available recenter targets
    /// </summary>
    public List<string> GetAvailableTargets()
    {
        return new List<string> { "living room", "main entrance", "kitchen" };
    }

    void Update()
    {
        // Debug testing with spacebar (as shown in transcript)
        if (enableDebugMode && Input.GetKeyDown(testKey))
        {
            TestRecenterSequence();
        }
    }

    /// <summary>
    /// Test recenter sequence through all targets (debug)
    /// </summary>
    private void TestRecenterSequence()
    {
        var targets = GetAvailableTargets();

        for (int i = 0; i < targets.Count; i++)
        {
            string targetName = targets[i];
            Transform target = FindTargetByName(targetName);

            if (target != null)
            {
                Debug.Log($"[QRRecenterTargets] Testing recenter to: {targetName}");

                // Trigger recenter event (you can connect this to your NavigationController)
                if (NavigationController.Instance != null)
                {
                    // Create mock anchor data for testing
                    var mockAnchor = new AnchorManager.AnchorData
                    {
                        AnchorId = $"QR_{targetName}",
                        BuildingId = "B1",
                        Floor = 1,
                        PositionVector = target.position,
                        Rotation = target.rotation.eulerAngles
                    };

                    NavigationController.Instance.RecenterNavigation(mockAnchor, "test_target");
                }
            }

            // Wait a bit between tests
            System.Threading.Thread.Sleep(1000);
        }
    }

    void OnDrawGizmos()
    {
        if (!enableDebugMode) return;

        // Draw gizmos to visualize recenter targets
        Gizmos.color = Color.green;
        if (livingRoomTarget != null)
        {
            Gizmos.DrawWireSphere(livingRoomTarget.position, 0.5f);
            Gizmos.DrawIcon(livingRoomTarget.position, "Living Room");
        }

        Gizmos.color = Color.blue;
        if (mainEntranceTarget != null)
        {
            Gizmos.DrawWireSphere(mainEntranceTarget.position, 0.5f);
            Gizmos.DrawIcon(mainEntranceTarget.position, "Main Entrance");
        }

        Gizmos.color = Color.red;
        if (kitchenTarget != null)
        {
            Gizmos.DrawWireSphere(kitchenTarget.position, 0.5f);
            Gizmos.DrawIcon(kitchenTarget.position, "Kitchen");
        }
    }
}
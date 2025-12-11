using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// QR Recenter Targets - Integrates with TargetData.json for QR code recentering.
/// Uses existing target data instead of hardcoded Transform references.
/// </summary>
public class QRRecenterTargets : MonoBehaviour
{
    [Header("QR Recenter Settings")]
    public bool createDebugMarkers = false;  // Create visual markers at target positions

    private TargetManager targetManager;

    void Start()
    {
        targetManager = TargetManager.Instance;
        if (targetManager == null)
        {
            Debug.LogError("[QRRecenterTargets] TargetManager instance not found!");
            return;
        }

        // Optional: Create visual markers at all target positions
        if (createDebugMarkers)
        {
            CreateDebugMarkers();
        }
    }

    /// <summary>
    /// Find recenter target by name using TargetData.json
    /// </summary>
    public Transform FindTargetByName(string targetName)
    {
        // Try to get target from TargetManager
        if (targetManager.TryGetTarget(targetName, out var targetData))
        {
            // Create temporary GameObject at target position
            GameObject tempTarget = new GameObject($"QRTarget-{targetName}");
            var targetPosition = targetData.Position.ToVector3();
            var targetRotation = Quaternion.Euler(targetData.Rotation.ToVector3());

            tempTarget.transform.position = targetPosition;
            tempTarget.transform.rotation = targetRotation;
            tempTarget.transform.SetParent(transform);

            Debug.Log($"[QRRecenterTargets] Found target in data: {targetName} at {targetData.Position.ToVector3()}");
            return tempTarget.transform;
        }

        Debug.LogWarning($"[QRRecenterTargets] No target found in data: {targetName}");
        return null;
    }

    /// <summary>
    /// Get all available targets from TargetData.json
    /// </summary>
    public List<string> GetAvailableTargets()
    {
        if (targetManager != null)
        {
            return targetManager.GetAllTargetNames();
        }

        Debug.LogWarning("[QRRecenterTargets] TargetManager not available");
        return new List<string>();
    }

    /// <summary>
    /// Trigger recenter to specific target using TargetData.json
    /// </summary>
    public void RecenterToTarget(string targetName, string destinationTarget = null)
    {
        if (targetManager == null)
        {
            Debug.LogError("[QRRecenterTargets] TargetManager not available");
            return;
        }

        // Get target data from TargetManager
        if (!targetManager.TryGetTarget(targetName, out var targetData))
        {
            Debug.LogError($"[QRRecenterTargets] Target not found in data: {targetName}");
            return;
        }

        if (NavigationController.Instance != null)
        {
            // Create anchor data using TargetData.json values
            var anchorData = new AnchorManager.AnchorData
            {
                AnchorId = targetName,
                BuildingId = ExtractBuildingId(targetName),
                Floor = targetData.FloorNumber,
                Position = new AnchorManager.Vector3Serializable(targetData.Position.x, targetData.Position.y, targetData.Position.z),
                Rotation = new AnchorManager.Vector3Serializable(targetData.Rotation.x, targetData.Rotation.y, targetData.Rotation.z),
                Type = "qr_recenter"
            };

            // Use destination if provided, otherwise use current navigation target
            string finalDestination = destinationTarget ?? (NavigationController.Instance.targetData?.Name);

            if (!string.IsNullOrEmpty(finalDestination))
            {
                NavigationController.Instance.RecenterNavigation(anchorData, finalDestination);
                Debug.Log($"[QRRecenterTargets] Recentered to {targetName} (Floor {targetData.FloorNumber}) for destination: {finalDestination}");
            }
            else
            {
                Debug.LogWarning("[QRRecenterTargets] No destination available for recentering");
            }
        }
        else
        {
            Debug.LogError("[QRRecenterTargets] NavigationController instance not available");
        }
    }

    /// <summary>
    /// Extract building ID from target name
    /// </summary>
    private string ExtractBuildingId(string targetName)
    {
        if (targetName.Contains("B1")) return "B1";
        if (targetName.Contains("B2")) return "B2";
        if (targetName.Contains("B3")) return "B3";
        // Add more building IDs as needed
        return "B1"; // Default fallback
    }


    /// <summary>
    /// Create debug visual markers for all targets in TargetData.json
    /// </summary>
    private void CreateDebugMarkers()
    {
        var allTargets = targetManager?.GetAllTargetNames();
        if (allTargets == null) return;

        foreach (var targetName in allTargets)
        {
            if (targetManager.TryGetTarget(targetName, out var target))
            {
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = $"Marker-{target.Name}";
                marker.transform.position = target.Position.ToVector3();
                marker.transform.localScale = Vector3.one * 0.3f;

                // Add color based on floor
                Renderer renderer = marker.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color color = target.FloorNumber switch
                    {
                        0 => Color.green,    // Ground floor
                        1 => Color.blue,     // First floor
                        2 => Color.yellow,    // Second floor
                        3 => Color.cyan,     // Third floor
                        4 => Color.magenta,   // Fourth floor
                        5 => Color.red,      // Fifth floor
                        _ => Color.gray       // Other floors
                    };
                    renderer.material.color = color;
                }
            }
        }
    }
}
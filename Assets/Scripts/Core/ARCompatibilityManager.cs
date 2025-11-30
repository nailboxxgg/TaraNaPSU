using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Manages AR compatibility and feature bypassing for different devices.
/// Provides fallback modes when AR features don't work properly.
/// </summary>
public class ARCompatibilityManager : MonoBehaviour
{
    public static ARCompatibilityManager Instance { get; private set; }

    [Header("Compatibility Settings")]
    public bool forceCompatibilityMode = false;
    public CompatibilityMode currentMode = CompatibilityMode.Auto;

    [Header("Bypass Options")]
    public bool bypassARSession = false;
    public bool bypassARRendering = false;
    public bool useLegacyNavigation = false;

    [Header("Device Detection")]
    public bool autoDetectDevice = true;
    public List<string> compatibleDevices = new List<string> { "Pixel", "iPhone", "iPad", "Android" };
    public List<string> problematicDevices = new List<string> { "Tecno Spark", "Alcatel", "低端设备" };

    // Events
    public event Action<CompatibilityMode> OnCompatibilityModeChanged;
    public event Action<string> OnDeviceDetected;
    public event Action<bool> OnARBypassChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        DontDestroyOnLoad(gameObject);
        DetectDevice();
        ApplyCompatibilitySettings();
    }

    void Start()
    {
        Debug.Log($"[ARCompatibilityManager] Device: {SystemInfo.deviceModel}, Mode: {currentMode}");
        Debug.Log($"[ARCompatibilityManager] AR Support: {IsARSupported()}");
        Debug.Log($"[ARCompatibilityManager] Compatibility Mode: {GetCompatibilityDescription()}");
    }

    #region Device Detection

    /// <summary>
    /// Detect device and set appropriate compatibility mode
    /// </summary>
    public void DetectDevice()
    {
        if (!autoDetectDevice) return;

        string deviceModel = SystemInfo.deviceModel?.ToLower() ?? "unknown";
        string deviceName = SystemInfo.deviceName?.ToLower() ?? "unknown";

        Debug.Log($"[ARCompatibilityManager] Detected device: {deviceModel} ({deviceName})");

        // Check for known problematic devices
        foreach (var problematic in problematicDevices)
        {
            if (deviceModel.Contains(problematic.ToLower()) || deviceName.Contains(problematic.ToLower()))
            {
                currentMode = CompatibilityMode.Legacy;
                Debug.LogWarning($"[ARCompatibilityManager] ⚠️ Problematic device detected: {deviceModel}");
                OnDeviceDetected?.Invoke(deviceModel);
                return;
            }
        }

        // Check for known compatible devices
        foreach (var compatible in compatibleDevices)
        {
            if (deviceModel.Contains(compatible.ToLower()) || deviceName.Contains(compatible.ToLower()))
            {
                currentMode = CompatibilityMode.Standard;
                Debug.Log($"[ARCompatibilityManager] ✅ Compatible device: {deviceModel}");
                OnDeviceDetected?.Invoke(deviceModel);
                return;
            }
        }

        // Unknown device - use conservative settings
        currentMode = CompatibilityMode.Safe;
        Debug.LogWarning($"[ARCompatibilityManager] ❓ Unknown device: {deviceModel} - using Safe mode");
        OnDeviceDetected?.Invoke(deviceModel);
    }

    /// <summary>
    /// Check if current device supports AR features
    /// </summary>
    public bool IsARSupported()
    {
        if (forceCompatibilityMode) return false;

        switch (currentMode)
        {
            case CompatibilityMode.Standard:
                return true;
            case CompatibilityMode.Safe:
                return CheckARCapability();
            case CompatibilityMode.Legacy:
                return false;
            default:
                return CheckARCapability();
        }
    }

    /// <summary>
    /// Basic AR capability check
    /// </summary>
    private bool CheckARCapability()
    {
        // Check for basic AR support indicators
        bool hasGyroscope = SystemInfo.supportsGyroscope;
        bool hasAccelerometer = SystemInfo.supportsAccelerometer;
        bool hasCamera = SystemInfo.deviceType != DeviceType.Unknown;

        int supportedLevel = 0;
        if (hasGyroscope) supportedLevel++;
        if (hasAccelerometer) supportedLevel++;
        if (hasCamera) supportedLevel++;

        Debug.Log($"[ARCompatibilityManager] AR Capability Score: {supportedLevel}/3");

        return supportedLevel >= 2; // At least 2/3 features supported
    }

    #endregion

    #region Compatibility Settings

    /// <summary>
    /// Apply compatibility settings based on detected mode
    /// </summary>
    private void ApplyCompatibilitySettings()
    {
        switch (currentMode)
        {
            case CompatibilityMode.Legacy:
                SetLegacyMode();
                break;
            case CompatibilityMode.Safe:
                SetSafeMode();
                break;
            case CompatibilityMode.Standard:
                SetStandardMode();
                break;
        }

        OnCompatibilityModeChanged?.Invoke(currentMode);
    }

    /// <summary>
    /// Set legacy mode for problematic devices
    /// </summary>
    private void SetLegacyMode()
    {
        Debug.Log("[ARCompatibilityManager] 🔄 Setting LEGACY mode");

        bypassARSession = true;
        bypassARRendering = true;
        useLegacyNavigation = true;

        // Disable AR components
        DisableARComponents();

        OnARBypassChanged?.Invoke(true);
    }

    /// <summary>
    /// Set safe mode for unknown devices
    /// </summary>
    private void SetSafeMode()
    {
        Debug.Log("[ARCompatibilityManager] 🛡️ Setting SAFE mode");

        bypassARSession = false;
        bypassARRendering = false;
        useLegacyNavigation = false;

        // Enable basic AR but with reduced features
        ReduceARFeatures();
    }

    /// <summary>
    /// Set standard mode for compatible devices
    /// </summary>
    private void SetStandardMode()
    {
        Debug.Log("[ARCompatibilityManager] ✅ Setting STANDARD mode");

        bypassARSession = false;
        bypassARRendering = false;
        useLegacyNavigation = false;

        // Enable all AR features
        EnableARComponents();

        OnARBypassChanged?.Invoke(false);
    }

    #endregion

    #region Component Control

    /// <summary>
    /// Disable AR components for legacy mode
    /// </summary>
    private void DisableARComponents()
    {
        // Find and disable AR-related components
        var arSession = FindObjectOfType<ARSession>();
        var arSessionOrigin = FindObjectOfType<XROrigin>();
        var arCameraManager = FindObjectOfType<ARCameraManager>();
        var arRaycastManager = FindObjectOfType<ARRaycastManager>();

        if (arSession != null)
        {
            arSession.enabled = false;
            Debug.Log("[ARCompatibilityManager] ❌ Disabled ARSession");
        }

        if (arSessionOrigin != null)
        {
            arSessionOrigin.enabled = false;
            Debug.Log("[ARCompatibilityManager] ❌ Disabled ARSessionOrigin");
        }

        if (arCameraManager != null)
        {
            arCameraManager.enabled = false;
            Debug.Log("[ARCompatibilityManager] ❌ Disabled ARCameraManager");
        }

        if (arRaycastManager != null)
        {
            arRaycastManager.enabled = false;
            Debug.Log("[ARCompatibilityManager] ❌ Disabled ARRaycastManager");
        }
    }

    /// <summary>
    /// Reduce AR features for safe mode
    /// </summary>
    private void ReduceARFeatures()
    {
        // Reduce update rates, disable non-essential features
        try
        {
            var arSession = FindObjectOfType<ARSession>();
            if (arSession != null)
            {
                // Set lower frame rate for safe mode
                QualitySettings.SetQualityLevel(0); // 0 = Fast
                Debug.Log("[ARCompatibilityManager] ⚙️ Reduced quality to Fast");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ARCompatibilityManager] ⚠️ Could not reduce AR features: {e.Message}");
        }
    }

    /// <summary>
    /// Enable all AR components for standard mode
    /// </summary>
    private void EnableARComponents()
    {
        var arSession = FindObjectOfType<ARSession>();
        var arSessionOrigin = FindObjectOfType<XROrigin>();
        var arCameraManager = FindObjectOfType<ARCameraManager>();
        var arRaycastManager = FindObjectOfType<ARRaycastManager>();

        if (arSession != null)
        {
            arSession.enabled = true;
            Debug.Log("[ARCompatibilityManager] ✅ Enabled ARSession");
        }

        if (arSessionOrigin != null)
        {
            arSessionOrigin.enabled = true;
            Debug.Log("[ARCompatibilityManager] ✅ Enabled ARSessionOrigin");
        }

        if (arCameraManager != null)
        {
            arCameraManager.enabled = true;
            Debug.Log("[ARCompatibilityManager] ✅ Enabled ARCameraManager");
        }

        if (arRaycastManager != null)
        {
            arRaycastManager.enabled = true;
            Debug.Log("[ARCompatibilityManager] ✅ Enabled ARRaycastManager");
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Force specific compatibility mode
    /// </summary>
    public void SetCompatibilityMode(CompatibilityMode mode)
    {
        if (currentMode == mode) return;

        currentMode = mode;
        ApplyCompatibilitySettings();
    }

    /// <summary>
    /// Toggle AR bypass on/off
    /// </summary>
    public void ToggleARBypass(bool bypass)
    {
        if (bypassARSession == bypass) return;

        bypassARSession = bypass;
        OnARBypassChanged?.Invoke(bypass);

        if (bypass)
        {
            DisableARComponents();
        }
        else
        {
            ApplyCompatibilitySettings();
        }
    }

    /// <summary>
    /// Get description of current compatibility mode
    /// </summary>
    public string GetCompatibilityDescription()
    {
        switch (currentMode)
        {
            case CompatibilityMode.Legacy:
                return "Legacy (AR Disabled)";
            case CompatibilityMode.Safe:
                return "Safe (Reduced AR)";
            case CompatibilityMode.Standard:
                return "Standard (Full AR)";
            default:
                return "Unknown";
        }
    }

    /// <summary>
    /// Check if AR is currently bypassed
    /// </summary>
    public bool IsARBypassed => bypassARSession || forceCompatibilityMode;

    #endregion

    #region Enums

    public enum CompatibilityMode
    {
        Auto,       // Auto-detect based on device
        Standard,    // Full AR features
        Safe,        // Reduced AR features
        Legacy       // No AR features
    }

    #endregion
}
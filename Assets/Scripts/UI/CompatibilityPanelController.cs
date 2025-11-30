using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI controller for AR compatibility settings panel.
/// Allows users to manually adjust compatibility settings if auto-detection fails.
/// </summary>
public class CompatibilityPanelController : MonoBehaviour
{
    public static CompatibilityPanelController Instance { get; private set; }

    [Header("UI References")]
    public GameObject compatibilityPanel;
    public Button legacyModeButton;
    public Button safeModeButton;
    public Button standardModeButton;
    public Button autoDetectButton;
    public Button toggleARButton;
    public Text statusText;
    public Text deviceText;

    [Header("Settings")]
    public float autoHideDelay = 3.0f;

    private bool isVisible = false;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        // Hide panel initially
        if (compatibilityPanel != null)
            compatibilityPanel.SetActive(false);
    }

    void Start()
    {
        // Get compatibility manager
        var compatManager = ARCompatibilityManager.Instance;
        if (compatManager != null)
        {
            // Subscribe to events
            compatManager.OnCompatibilityModeChanged += OnCompatibilityModeChanged;
            compatManager.OnDeviceDetected += OnDeviceDetected;
            compatManager.OnARBypassChanged += OnARBypassChanged;

            // Update UI
            UpdateUI();
        }
    }

    void Update()
    {
        // Auto-hide panel after delay
        if (isVisible && Input.GetKeyDown(KeyCode.Escape))
        {
            HidePanel();
        }
    }

    #region UI Updates

    /// <summary>
    /// Update all UI elements based on current compatibility state
    /// </summary>
    private void UpdateUI()
    {
        var compatManager = ARCompatibilityManager.Instance;
        if (compatManager == null) return;

        // Update device info
        if (deviceText != null)
        {
            deviceText.text = $"Device: {SystemInfo.deviceModel ?? "Unknown"}\nMode: {compatManager.GetCompatibilityDescription()}";
        }

        // Update status text
        if (statusText != null)
        {
            if (compatManager.IsARBypassed)
            {
                statusText.text = "⚠️ AR Features Disabled\nCompatibility Mode Active";
                statusText.color = Color.yellow;
            }
            else
            {
                statusText.text = "✅ AR Features Enabled\nStandard Mode";
                statusText.color = Color.green;
            }
        }

        // Update button states
        UpdateButtonStates(compatManager.currentMode);
    }

    /// <summary>
    /// Update button visual states based on current mode
    /// </summary>
    private void UpdateButtonStates(ARCompatibilityManager.CompatibilityMode mode)
    {
        // Reset all buttons
        if (legacyModeButton != null)
        {
            var legacyColors = legacyModeButton.colors;
            legacyColors.normalColor = mode == ARCompatibilityManager.CompatibilityMode.Legacy ? Color.green : Color.white;
            legacyModeButton.interactable = mode != ARCompatibilityManager.CompatibilityMode.Legacy;
        }

        if (safeModeButton != null)
        {
            var safeColors = safeModeButton.colors;
            safeColors.normalColor = mode == ARCompatibilityManager.CompatibilityMode.Safe ? Color.green : Color.white;
            safeModeButton.interactable = mode != ARCompatibilityManager.CompatibilityMode.Safe;
        }

        if (standardModeButton != null)
        {
            var standardColors = standardModeButton.colors;
            standardColors.normalColor = mode == ARCompatibilityManager.CompatibilityMode.Standard ? Color.green : Color.white;
            standardModeButton.interactable = mode != ARCompatibilityManager.CompatibilityMode.Standard;
        }

        if (toggleARButton != null)
        {
            var toggleColors = toggleARButton.colors;
            var compatManager = ARCompatibilityManager.Instance;
            bool arBypassed = compatManager != null && compatManager.IsARBypassed;

            toggleColors.normalColor = arBypassed ? Color.red : Color.green;
            toggleARButton.GetComponentInChildren<Text>().text = arBypassed ? "Enable AR" : "Disable AR";
        }
    }

    #endregion

    #region Button Handlers

    /// <summary>
    /// Handle legacy mode button click
    /// </summary>
    public void OnLegacyModeClicked()
    {
        var compatManager = ARCompatibilityManager.Instance;
        if (compatManager != null)
        {
            compatManager.SetCompatibilityMode(ARCompatibilityManager.CompatibilityMode.Legacy);
            Debug.Log("[CompatibilityPanel] User selected Legacy mode");
        }
    }

    /// <summary>
    /// Handle safe mode button click
    /// </summary>
    public void OnSafeModeClicked()
    {
        var compatManager = ARCompatibilityManager.Instance;
        if (compatManager != null)
        {
            compatManager.SetCompatibilityMode(ARCompatibilityManager.CompatibilityMode.Safe);
            Debug.Log("[CompatibilityPanel] User selected Safe mode");
        }
    }

    /// <summary>
    /// Handle standard mode button click
    /// </summary>
    public void OnStandardModeClicked()
    {
        var compatManager = ARCompatibilityManager.Instance;
        if (compatManager != null)
        {
            compatManager.SetCompatibilityMode(ARCompatibilityManager.CompatibilityMode.Standard);
            Debug.Log("[CompatibilityPanel] User selected Standard mode");
        }
    }

    /// <summary>
    /// Handle auto-detect button click
    /// </summary>
    public void OnAutoDetectClicked()
    {
        var compatManager = ARCompatibilityManager.Instance;
        if (compatManager != null)
        {
            compatManager.autoDetectDevice = true;
            // Force re-detection
            compatManager.DetectDevice();
            UpdateUI();
            Debug.Log("[CompatibilityPanel] User triggered auto-detection");
        }
    }

    /// <summary>
    /// Handle AR toggle button click
    /// </summary>
    public void OnARToggleClicked()
    {
        var compatManager = ARCompatibilityManager.Instance;
        if (compatManager != null)
        {
            bool currentBypass = compatManager.IsARBypassed;
            compatManager.ToggleARBypass(!currentBypass);
            Debug.Log($"[CompatibilityPanel] User toggled AR bypass: {!currentBypass}");
        }
    }

    #endregion

    #region Panel Control

    /// <summary>
    /// Show compatibility panel
    /// </summary>
    public void ShowPanel()
    {
        if (compatibilityPanel != null)
        {
            compatibilityPanel.SetActive(true);
            isVisible = true;
            UpdateUI();

            // Auto-hide after delay
            Invoke(nameof(HidePanel), autoHideDelay);
        }
    }

    /// <summary>
    /// Hide compatibility panel
    /// </summary>
    public void HidePanel()
    {
        if (compatibilityPanel != null)
        {
            compatibilityPanel.SetActive(false);
            isVisible = false;
            CancelInvoke(nameof(HidePanel));
        }
    }

    /// <summary>
    /// Toggle panel visibility
    /// </summary>
    public void TogglePanel()
    {
        if (isVisible)
            HidePanel();
        else
            ShowPanel();
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handle compatibility mode change event
    /// </summary>
    private void OnCompatibilityModeChanged(ARCompatibilityManager.CompatibilityMode newMode)
    {
        UpdateUI();
        Debug.Log($"[CompatibilityPanel] Compatibility mode changed to: {newMode}");
    }

    /// <summary>
    /// Handle device detection event
    /// </summary>
    private void OnDeviceDetected(string deviceName)
    {
        UpdateUI();
        Debug.Log($"[CompatibilityPanel] Device detected: {deviceName}");
    }

    /// <summary>
    /// Handle AR bypass change event
    /// </summary>
    private void OnARBypassChanged(bool bypassed)
    {
        UpdateUI();
        Debug.Log($"[CompatibilityPanel] AR bypass changed to: {bypassed}");
    }

    #endregion
}
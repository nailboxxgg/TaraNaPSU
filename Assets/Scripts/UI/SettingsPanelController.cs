using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Settings Panel Controller - Allows users to customize navigation visualization
/// including 3D arrow visibility, navigation line visibility, colors, and sizes
/// </summary>
public class SettingsPanelController : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject SettingsPanel;
    public Button closeButton;

    [Header("Navigation Visualization")]
    public Toggle arrowToggle;
    public Toggle lineToggle;
    public Slider arrowSizeSlider;
    public Slider lineWidthSlider;

    [Header("Color Settings")]
    public Button arrowColorButton;
    public Button lineColorButton;
    public Image arrowColorPreview;
    public Image lineColorPreview;

    [Header("Text References")]
    public TextMeshProUGUI arrowSizeText;
    public TextMeshProUGUI lineWidthText;

    // Default settings
    private readonly Color defaultArrowColor = Color.green;
    private readonly Color defaultLineColor = Color.blue;
    private readonly float defaultArrowSize = 1.0f;
    private readonly float defaultLineWidth = 0.1f;

    // Current settings
    private Color currentArrowColor;
    private Color currentLineColor;
    private float currentArrowSize;
    private float currentLineWidth;

    private bool isInitialized = false;

    void Awake()
    {
        // Initialize with default values
        currentArrowColor = defaultArrowColor;
        currentLineColor = defaultLineColor;
        currentArrowSize = defaultArrowSize;
        currentLineWidth = defaultLineWidth;

        // Hide panel initially
        if (SettingsPanel != null)
            SettingsPanel.SetActive(false);
    }

    void Start()
    {
        InitializeSettings();
        SetupUIListeners();
        isInitialized = true;
    }

    /// <summary>
    /// Initialize settings UI with current values
    /// </summary>
    private void InitializeSettings()
    {
        // Set toggle states based on NavigationController defaults
        if (arrowToggle != null)
        {
            bool arrowState = true;
            if (NavigationController.Instance != null)
                arrowState = NavigationController.Instance.ShowArrowByDefault;
            arrowToggle.isOn = arrowState;
        }

        if (lineToggle != null)
        {
            bool lineState = true;
            if (NavigationController.Instance != null)
                lineState = NavigationController.Instance.ShowLineByDefault;
            lineToggle.isOn = lineState;
        }

        // Set slider values
        if (arrowSizeSlider != null)
        {
            arrowSizeSlider.minValue = 0.5f;
            arrowSizeSlider.maxValue = 3.0f;
            arrowSizeSlider.value = currentArrowSize;
        }

        if (lineWidthSlider != null)
        {
            lineWidthSlider.minValue = 0.02f;
            lineWidthSlider.maxValue = 0.5f;
            lineWidthSlider.value = currentLineWidth;
        }

        // Set color previews
        UpdateColorPreviews();
        UpdateTextDisplays();
    }

    /// <summary>
    /// Setup UI event listeners
    /// </summary>
    private void SetupUIListeners()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseSettings);

        if (arrowToggle != null)
            arrowToggle.onValueChanged.AddListener(OnArrowToggleChanged);

        if (lineToggle != null)
            lineToggle.onValueChanged.AddListener(OnLineToggleChanged);

        if (arrowSizeSlider != null)
            arrowSizeSlider.onValueChanged.AddListener(OnArrowSizeChanged);

        if (lineWidthSlider != null)
            lineWidthSlider.onValueChanged.AddListener(OnLineWidthChanged);

        if (arrowColorButton != null)
            arrowColorButton.onClick.AddListener(ShowArrowColorPicker);

        if (lineColorButton != null)
            lineColorButton.onClick.AddListener(ShowLineColorPicker);
    }

    /// <summary>
    /// Open Settings panel
    /// </summary>
    public void OpenSettings()
    {
        if (SettingsPanel != null)
        {
            SettingsPanel.SetActive(true);
            Debug.Log("[SettingsPanel] Settings opened");

            // Refresh current values from NavigationController
            RefreshCurrentSettings();
        }
    }

    /// <summary>
    /// Close Settings panel
    /// </summary>
    public void CloseSettings()
    {
        Debug.Log($"[SettingsPanel] CloseSettings called. CloseButton is {(closeButton != null ? "assigned" : "NULL")}");

        if (SettingsPanel != null)
        {
            SettingsPanel.SetActive(false);
            Debug.Log("[SettingsPanel] Settings closed");
        }
        else
        {
            Debug.LogError("[SettingsPanel] SettingsPanel is NULL!");
        }
    }

    /// <summary>
    /// Refresh current settings from NavigationController
    /// </summary>
    private void RefreshCurrentSettings()
    {
        if (!isInitialized || NavigationController.Instance == null) return;

        // Get current arrow state (use default settings if not navigating)
        if (arrowToggle != null)
        {
            bool arrowState = NavigationController.Instance.ShowArrowByDefault;
            if (NavigationController.Instance.ActiveArrow != null)
                arrowState = NavigationController.Instance.ActiveArrow.activeInHierarchy;
            arrowToggle.isOn = arrowState;
        }

        // Get current line state (use default settings if not navigating)
        if (lineToggle != null)
        {
            bool lineState = NavigationController.Instance.ShowLineByDefault;
            if (NavigationController.Instance.lineRenderer != null)
                lineState = NavigationController.Instance.lineRenderer.enabled;
            lineToggle.isOn = lineState;
        }

        // Get current arrow color (if possible to get from renderer)
        var arrow = NavigationController.Instance.ActiveArrow;
        if (arrow != null && arrow.GetComponent<Renderer>() != null)
        {
            currentArrowColor = arrow.GetComponent<Renderer>().material.color;
        }

        // Get current line color
        var lineRenderer = NavigationController.Instance.lineRenderer;
        if (lineRenderer != null && lineRenderer.material != null)
        {
            currentLineColor = lineRenderer.material.color;
        }

        UpdateColorPreviews();
    }

    #region UI Event Handlers

    private void OnArrowToggleChanged(bool isOn)
    {
        if (NavigationController.Instance != null && NavigationController.Instance.ActiveArrow != null)
            NavigationController.Instance.ActiveArrow.SetActive(isOn);

        Debug.Log($"[SettingsPanel] Arrow visibility: {(isOn ? "ON" : "OFF")}");
    }

    private void OnLineToggleChanged(bool isOn)
    {
        if (NavigationController.Instance != null && NavigationController.Instance.lineRenderer != null)
            NavigationController.Instance.lineRenderer.enabled = isOn;

        Debug.Log($"[SettingsPanel] Navigation line visibility: {(isOn ? "ON" : "OFF")}");
    }

    private void OnArrowSizeChanged(float size)
    {
        currentArrowSize = size;
        UpdateTextDisplays();

        // Apply size to arrow if it exists
        if (NavigationController.Instance != null && NavigationController.Instance.ActiveArrow != null)
        {
            NavigationController.Instance.ActiveArrow.transform.localScale = Vector3.one * size;
        }

        Debug.Log($"[SettingsPanel] Arrow size: {size:F2}");
    }

    private void OnLineWidthChanged(float width)
    {
        currentLineWidth = width;
        UpdateTextDisplays();

        // Apply width to line renderer if it exists
        if (NavigationController.Instance != null && NavigationController.Instance.lineRenderer != null)
        {
            NavigationController.Instance.lineRenderer.startWidth = width;
            NavigationController.Instance.lineRenderer.endWidth = width;
        }

        Debug.Log($"[SettingsPanel] Line width: {width:F3}");
    }

    private void ShowArrowColorPicker()
    {
        // Simple color picker (you can enhance this with a proper color picker UI)
        Color newColor = GetColorFromUser("Arrow Color", currentArrowColor);
        if (newColor != currentArrowColor)
        {
            currentArrowColor = newColor;
            ApplyArrowColor(newColor);
        }
    }

    private void ShowLineColorPicker()
    {
        // Simple color picker (you can enhance this with a proper color picker UI)
        Color newColor = GetColorFromUser("Line Color", currentLineColor);
        if (newColor != currentLineColor)
        {
            currentLineColor = newColor;
            ApplyLineColor(newColor);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Update color preview images
    /// </summary>
    private void UpdateColorPreviews()
    {
        if (arrowColorPreview != null)
            arrowColorPreview.color = currentArrowColor;

        if (lineColorPreview != null)
            lineColorPreview.color = currentLineColor;
    }

    /// <summary>
    /// Update text displays
    /// </summary>
    private void UpdateTextDisplays()
    {
        if (arrowSizeText != null)
            arrowSizeText.text = $"Arrow Size: {currentArrowSize:F1}";

        if (lineWidthText != null)
            lineWidthText.text = $"Line Width: {currentLineWidth:F2}";
    }

    /// <summary>
    /// Apply color to arrow
    /// </summary>
    private void ApplyArrowColor(Color color)
    {
        if (NavigationController.Instance != null && NavigationController.Instance.ActiveArrow != null)
        {
            var renderer = NavigationController.Instance.ActiveArrow.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Create new material if needed
                if (renderer.material == null || !renderer.material.name.Contains("Custom"))
                {
                    Material customMaterial = new Material(Shader.Find("Standard"))
                    {
                        name = "Custom Arrow Material",
                        color = color
                    };
                    renderer.material = customMaterial;
                }
                else
                {
                    renderer.material.color = color;
                }
            }
        }

        UpdateColorPreviews();
        Debug.Log($"[SettingsPanel] Arrow color changed to: {color}");
    }

    /// <summary>
    /// Apply color to navigation line
    /// </summary>
    private void ApplyLineColor(Color color)
    {
        if (NavigationController.Instance != null && NavigationController.Instance.lineRenderer != null)
        {
            // Create new material if needed
            if (NavigationController.Instance.lineRenderer.material == null ||
                !NavigationController.Instance.lineRenderer.material.name.Contains("Custom"))
            {
                Material customMaterial = new Material(Shader.Find("Sprites/Default"))
                {
                    name = "Custom Line Material",
                    color = color
                };
                NavigationController.Instance.lineRenderer.material = customMaterial;
            }
            else
            {
                NavigationController.Instance.lineRenderer.material.color = color;
            }
        }

        UpdateColorPreviews();
        Debug.Log($"[SettingsPanel] Line color changed to: {color}");
    }

    /// <summary>
    /// Simple color picker dialog (you can replace this with a proper UI color picker)
    /// </summary>
    private Color GetColorFromUser(string title, Color currentColor)
    {
        // For now, cycle through some preset colors
        Color[] presetColors = { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan, Color.white };

        int currentIndex = -1;
        for (int i = 0; i < presetColors.Length; i++)
        {
            if (presetColors[i] == currentColor)
            {
                currentIndex = i;
                break;
            }
        }

        int nextIndex = (currentIndex + 1) % presetColors.Length;
        Debug.Log($"[SettingsPanel] {title}: Selected color {presetColors[nextIndex]}");
        return presetColors[nextIndex];
    }

    #endregion

    /// <summary>
    /// Reset settings to defaults
    /// </summary>
    public void ResetToDefaults()
    {
        currentArrowColor = defaultArrowColor;
        currentLineColor = defaultLineColor;
        currentArrowSize = defaultArrowSize;
        currentLineWidth = defaultLineWidth;

        ApplyArrowColor(defaultArrowColor);
        ApplyLineColor(defaultLineColor);
        OnArrowSizeChanged(defaultArrowSize);
        OnLineWidthChanged(defaultLineWidth);

        if (arrowToggle != null)
            arrowToggle.isOn = true;

        if (lineToggle != null)
            lineToggle.isOn = true;

        Debug.Log("[SettingsPanel] Settings reset to defaults");
    }

    /// <summary>
    /// Test method - call this from button OnClick for testing
    /// </summary>
    public void TestCloseButton()
    {
        Debug.Log("[SettingsPanel] TestCloseButton called!");
        CloseSettings();
    }
}
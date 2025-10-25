using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Enhanced minimap with auto-follow, zoom, and floor awareness
/// </summary>
public class EnhancedMinimapController : MonoBehaviour {

    [Header("References")]
    public NavigationController navigationController;
    public Camera minimapCamera;
    public RawImage minimapDisplay;
    public Transform playerMarker;

    [Header("Camera Settings")]
    [Tooltip("Target to follow (usually Main Camera or XR Origin)")]
    public Transform followTarget;
    
    [Tooltip("Height of minimap camera above player")]
    public float cameraHeight = 15f;
    
    [Tooltip("Camera rotation (0 = north-up, follow player rotation)")]
    public bool rotateWithPlayer = false;
    
    [Header("Zoom Settings")]
    public float defaultZoom = 10f;
    public float minZoom = 5f;
    public float maxZoom = 30f;
    public float zoomSpeed = 5f;
    
    [Tooltip("Auto-zoom based on distance to target")]
    public bool autoZoom = true;
    
    [Header("Floor Display")]
    public GameObject[] floorMaps = new GameObject[6]; // One per floor
    public bool hideOtherFloors = true;
    
    [Header("Visual Settings")]
    public Color playerMarkerColor = Color.cyan;
    public Color targetMarkerColor = Color.red;
    public float markerPulseSpeed = 2f;
    
    [Header("UI Elements")]
    public GameObject zoomInButton;
    public GameObject zoomOutButton;
    public Text floorLabel;

    // Private variables
    private float currentZoom;
    private GameObject targetMarker;
    private float pulseTime = 0f;
    private int lastFloor = -1;

    private void Start() {
        if (navigationController == null) {
            navigationController = FindObjectOfType<NavigationController>();
        }

        if (followTarget == null) {
            followTarget = Camera.main.transform;
        }

        if (minimapCamera == null) {
            minimapCamera = GetComponent<Camera>();
        }

        // Setup minimap camera
        if (minimapCamera != null) {
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = defaultZoom;
            currentZoom = defaultZoom;
        }

        // Create target marker
        CreateTargetMarker();

        // Setup player marker
        if (playerMarker != null) {
            Renderer markerRenderer = playerMarker.GetComponent<Renderer>();
            if (markerRenderer != null) {
                markerRenderer.material.color = playerMarkerColor;
            }
        }

        // Subscribe to events
        if (navigationController != null) {
            navigationController.OnFloorChanged.AddListener(OnFloorChanged);
            navigationController.OnNavigationUpdate.AddListener(OnNavigationUpdate);
        }

        // Setup zoom buttons
        if (zoomInButton != null) {
            Button btn = zoomInButton.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(ZoomIn);
        }

        if (zoomOutButton != null) {
            Button btn = zoomOutButton.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(ZoomOut);
        }

        // Initial floor setup
        UpdateFloorDisplay(navigationController?.currentFloor ?? 0);
    }

    private void Update() {
        // Follow player
        if (followTarget != null && minimapCamera != null) {
            Vector3 targetPos = followTarget.position;
            targetPos.y = cameraHeight;
            minimapCamera.transform.position = targetPos;

            // Rotate with player if enabled
            if (rotateWithPlayer) {
                Vector3 rotation = minimapCamera.transform.eulerAngles;
                rotation.y = followTarget.eulerAngles.y;
                minimapCamera.transform.eulerAngles = rotation;
            } else {
                // Always face down (top-down view)
                minimapCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
            }
        }

        // Update player marker position
        if (playerMarker != null && followTarget != null) {
            Vector3 markerPos = followTarget.position;
            markerPos.y = 0.5f; // Slightly above floor for visibility
            playerMarker.position = markerPos;

            if (rotateWithPlayer) {
                playerMarker.rotation = Quaternion.Euler(90, followTarget.eulerAngles.y, 0);
            }
        }

        // Update target marker
        UpdateTargetMarker();

        // Animate markers (pulsing)
        AnimateMarkers();

        // Auto-zoom
        if (autoZoom && navigationController != null && navigationController.IsNavigating) {
            AutoAdjustZoom();
        }

        // Smooth zoom transition
        if (minimapCamera != null) {
            minimapCamera.orthographicSize = Mathf.Lerp(
                minimapCamera.orthographicSize,
                currentZoom,
                Time.deltaTime * zoomSpeed
            );
        }
    }

    /// <summary>
    /// Create marker for navigation target
    /// </summary>
    private void CreateTargetMarker() {
        targetMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        targetMarker.name = "MinimapTargetMarker";
        targetMarker.transform.localScale = Vector3.one * 0.8f;
        
        Renderer renderer = targetMarker.GetComponent<Renderer>();
        if (renderer != null) {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = targetMarkerColor;
            mat.SetFloat("_Metallic", 0.5f);
            mat.SetFloat("_Glossiness", 0.8f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", targetMarkerColor * 0.8f);
            renderer.material = mat;
        }

        Destroy(targetMarker.GetComponent<Collider>());
        targetMarker.SetActive(false);
    }

    /// <summary>
    /// Update target marker position and visibility
    /// </summary>
    private void UpdateTargetMarker() {
        if (targetMarker == null || navigationController == null) return;

        if (navigationController.IsNavigating && navigationController.TargetPosition != Vector3.zero) {
            targetMarker.SetActive(true);
            Vector3 targetPos = navigationController.TargetPosition;
            targetPos.y = 0.6f; // Slightly higher than player marker
            targetMarker.transform.position = targetPos;
        } else {
            targetMarker.SetActive(false);
        }
    }

    /// <summary>
    /// Animate markers with pulsing effect
    /// </summary>
    private void AnimateMarkers() {
        pulseTime += Time.deltaTime * markerPulseSpeed;
        float pulse = (Mathf.Sin(pulseTime) + 1f) * 0.5f; // 0 to 1

        // Pulse player marker
        if (playerMarker != null) {
            float scale = 0.8f + pulse * 0.2f;
            playerMarker.localScale = Vector3.one * scale;
        }

        // Pulse target marker
        if (targetMarker != null && targetMarker.activeSelf) {
            float scale = 0.9f + pulse * 0.3f;
            targetMarker.transform.localScale = Vector3.one * scale;
        }
    }

    /// <summary>
    /// Auto-adjust zoom based on distance to target
    /// </summary>
    private void AutoAdjustZoom() {
        float distance = navigationController.DistanceToTarget;
        
        // Calculate appropriate zoom level
        float targetZoom = Mathf.Clamp(distance * 0.5f + 5f, minZoom, maxZoom);
        currentZoom = targetZoom;
    }

    /// <summary>
    /// Zoom in button handler
    /// </summary>
    public void ZoomIn() {
        currentZoom = Mathf.Max(currentZoom - 2f, minZoom);
        autoZoom = false; // Disable auto-zoom when user manually zooms
    }

    /// <summary>
    /// Zoom out button handler
    /// </summary>
    public void ZoomOut() {
        currentZoom = Mathf.Min(currentZoom + 2f, maxZoom);
        autoZoom = false;
    }

    /// <summary>
    /// Reset zoom to default
    /// </summary>
    public void ResetZoom() {
        currentZoom = defaultZoom;
        autoZoom = true;
    }

    /// <summary>
    /// Called when navigation updates
    /// </summary>
    private void OnNavigationUpdate(float distance, float eta) {
        // Can add distance-based zoom adjustments here
    }

    /// <summary>
    /// Called when floor changes
    /// </summary>
    private void OnFloorChanged(int newFloor) {
        UpdateFloorDisplay(newFloor);
    }

    /// <summary>
    /// Update which floor map is visible
    /// </summary>
    private void UpdateFloorDisplay(int floorNumber) {
        if (floorNumber == lastFloor) return;
        lastFloor = floorNumber;

        // Show/hide floor maps
        if (hideOtherFloors) {
            for (int i = 0; i < floorMaps.Length; i++) {
                if (floorMaps[i] != null) {
                    floorMaps[i].SetActive(i == floorNumber);
                }
            }
        }

        // Update floor label
        if (floorLabel != null && navigationController != null) {
            int building = navigationController.GetBuildingNumber(floorNumber);
            int floor = navigationController.GetFloorInBuilding(floorNumber);
            string floorName = floor == 0 ? "Ground" : "First";
            floorLabel.text = $"B{building} - {floorName}";
        }

        // Adjust camera position for floor
        if (minimapCamera != null) {
            Vector3 pos = minimapCamera.transform.position;
            pos.y = cameraHeight + (floorNumber * 0.1f); // Slight offset per floor
            minimapCamera.transform.position = pos;
        }
    }

    /// <summary>
    /// Toggle minimap visibility
    /// </summary>
    public void ToggleMinimapVisibility() {
        if (minimapDisplay != null) {
            minimapDisplay.enabled = !minimapDisplay.enabled;
        }
    }

    /// <summary>
    /// Set minimap size
    /// </summary>
    public void SetMinimapSize(float size) {
        if (minimapDisplay != null) {
            RectTransform rect = minimapDisplay.GetComponent<RectTransform>();
            if (rect != null) {
                rect.sizeDelta = new Vector2(size, size);
            }
        }
    }

    /// <summary>
    /// Toggle rotation mode
    /// </summary>
    public void ToggleRotationMode() {
        rotateWithPlayer = !rotateWithPlayer;
    }

    private void OnDestroy() {
        if (navigationController != null) {
            navigationController.OnFloorChanged.RemoveListener(OnFloorChanged);
            navigationController.OnNavigationUpdate.RemoveListener(OnNavigationUpdate);
        }

        if (targetMarker != null) {
            Destroy(targetMarker);
        }
    }

    // Debug
    private void OnDrawGizmos() {
        if (minimapCamera != null) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(minimapCamera.transform.position, 
                new Vector3(currentZoom * 2, 0.1f, currentZoom * 2));
        }
    }
}
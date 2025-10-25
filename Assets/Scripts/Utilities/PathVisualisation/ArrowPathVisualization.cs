using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Drawing.Drawing2D;

/// <summary>
/// Enhanced 3D arrow visualization for navigation paths
/// Replaces simple line renderer with animated, directional 3D arrows
/// </summary>
public class ArrowPathVisualizer : MonoBehaviour {

    [Header("References")]
    public NavigationController navigationController;
    
    [Header("Arrow Settings")]
    [Tooltip("Prefab for the 3D arrow (will create default if null)")]
    public GameObject arrowPrefab;
    
    [Tooltip("Distance between arrows along the path")]
    public float arrowSpacing = 2.0f;
    
    [Tooltip("Height offset above ground")]
    public float arrowHeightOffset = 0.3f;
    
    [Tooltip("Arrow scale")]
    public float arrowScale = 0.5f;
    
    [Header("Animation Settings")]
    public bool animateArrows = true;
    public float bobSpeed = 2.0f;
    public float bobHeight = 0.15f;
    public float rotationSpeed = 90f;
    
    [Header("Visual Settings")]
    public Color pathColor = new Color(0f, 0.8f, 1f, 1f); // Cyan
    public Color nearDestinationColor = new Color(0f, 1f, 0f, 1f); // Green
    public float colorTransitionDistance = 5f;
    
    [Header("Advanced")]
    public int maxArrows = 20;
    public bool fadeDistantArrows = true;
    public float fadeStartDistance = 10f;
    public float fadeEndDistance = 20f;

    // Private variables
    private List<GameObject> activeArrows = new List<GameObject>();
    private List<Vector3> arrowPositions = new List<Vector3>();
    private float animationTime = 0f;
    private bool isVisualizingPath = false;

    private void Start() {
        if (navigationController == null) {
            navigationController = FindObjectOfType<NavigationController>();
        }

        // Create default arrow prefab if none provided
        if (arrowPrefab == null) {
            arrowPrefab = CreateDefaultArrowPrefab();
        }

        // Subscribe to navigation events
        if (navigationController != null) {
            navigationController.OnNavigationUpdate.AddListener(OnNavigationUpdated);
        }
    }

    private void Update() {
        if (!isVisualizingPath || navigationController == null) {
            return;
        }

        // Update arrow animations
        if (animateArrows) {
            animationTime += Time.deltaTime;
            AnimateArrows();
        }

        // Update arrow colors based on distance to target
        UpdateArrowColors();

        // Update arrow visibility based on distance
        if (fadeDistantArrows) {
            UpdateArrowFading();
        }
    }

    /// <summary>
    /// Called when navigation updates
    /// </summary>
    private void OnNavigationUpdated(float distance, float eta) {
        if (navigationController.HasValidPath && navigationController.IsNavigating) {
            UpdateArrowVisualization();
            isVisualizingPath = true;
        } else {
            ClearArrows();
            isVisualizingPath = false;
        }
    }

    /// <summary>
    /// Update arrow visualization along the path
    /// </summary>
    private void UpdateArrowVisualization() {
        if (navigationController.CalculatedPath == null || 
            navigationController.CalculatedPath.corners.Length < 2) {
            ClearArrows();
            return;
        }

        // Calculate positions for arrows along the path
        arrowPositions.Clear();
        Vector3[] corners = navigationController.CalculatedPath.corners;

        for (int i = 0; i < corners.Length - 1; i++) {
            Vector3 start = corners[i];
            Vector3 end = corners[i + 1];
            float segmentLength = Vector3.Distance(start, end);
            Vector3 direction = (end - start).normalized;

            // Place arrows along this segment
            float currentDistance = arrowSpacing;
            while (currentDistance < segmentLength && arrowPositions.Count < maxArrows) {
                Vector3 position = start + direction * currentDistance;
                position.y += arrowHeightOffset; // Lift above ground
                arrowPositions.Add(position);
                currentDistance += arrowSpacing;
            }
        }

        // Update or create arrow GameObjects
        UpdateArrowObjects();
    }

    /// <summary>
    /// Update or create arrow GameObjects to match calculated positions
    /// </summary>
    private void UpdateArrowObjects() {
        // Remove excess arrows
        while (activeArrows.Count > arrowPositions.Count) {
            GameObject arrow = activeArrows[activeArrows.Count - 1];
            activeArrows.RemoveAt(activeArrows.Count - 1);
            Destroy(arrow);
        }

        // Create or update arrows
        for (int i = 0; i < arrowPositions.Count; i++) {
            GameObject arrow;
            
            if (i < activeArrows.Count) {
                // Update existing arrow
                arrow = activeArrows[i];
            } else {
                // Create new arrow
                arrow = Instantiate(arrowPrefab, transform);
                arrow.transform.localScale = Vector3.one * arrowScale;
                activeArrows.Add(arrow);
            }

            // Update position
            arrow.transform.position = arrowPositions[i];

            // Update rotation to point along path
            if (i < arrowPositions.Count - 1) {
                Vector3 direction = (arrowPositions[i + 1] - arrowPositions[i]).normalized;
                arrow.transform.rotation = Quaternion.LookRotation(direction);
            } else if (navigationController.TargetPosition != Vector3.zero) {
                Vector3 direction = (navigationController.TargetPosition - arrowPositions[i]).normalized;
                arrow.transform.rotation = Quaternion.LookRotation(direction);
            }

            arrow.SetActive(true);
        }
    }

    /// <summary>
    /// Animate arrows (bobbing, rotating)
    /// </summary>
    private void AnimateArrows() {
        float bobOffset = Mathf.Sin(animationTime * bobSpeed) * bobHeight;

        for (int i = 0; i < activeArrows.Count; i++) {
            if (activeArrows[i] == null) continue;

            // Bobbing animation
            Vector3 basePosition = arrowPositions[i];
            activeArrows[i].transform.position = basePosition + Vector3.up * bobOffset;

            // Optional: Pulsing scale
            float pulseScale = 1f + Mathf.Sin(animationTime * bobSpeed * 2f + i * 0.5f) * 0.1f;
            activeArrows[i].transform.localScale = Vector3.one * arrowScale * pulseScale;
        }
    }

    /// <summary>
    /// Update arrow colors based on distance to destination
    /// </summary>
    private void UpdateArrowColors() {
        if (navigationController == null) return;

        float distanceToTarget = navigationController.DistanceToTarget;

        for (int i = 0; i < activeArrows.Count; i++) {
            if (activeArrows[i] == null) continue;

            Renderer renderer = activeArrows[i].GetComponent<Renderer>();
            if (renderer == null) continue;

            // Interpolate color based on distance
            Color arrowColor = pathColor;
            if (distanceToTarget < colorTransitionDistance) {
                float t = 1f - (distanceToTarget / colorTransitionDistance);
                arrowColor = Color.Lerp(pathColor, nearDestinationColor, t);
            }

            // Apply color to material
            if (renderer.material.HasProperty("_Color")) {
                renderer.material.color = arrowColor;
            } else if (renderer.material.HasProperty("_BaseColor")) {
                renderer.material.SetColor("_BaseColor", arrowColor);
            }
        }
    }

    /// <summary>
    /// Fade distant arrows for better visual clarity
    /// </summary>
    private void UpdateArrowFading() {
        Vector3 userPosition = navigationController.transform.position;

        for (int i = 0; i < activeArrows.Count; i++) {
            if (activeArrows[i] == null) continue;

            float distance = Vector3.Distance(userPosition, arrowPositions[i]);
            Renderer renderer = activeArrows[i].GetComponent<Renderer>();
            
            if (renderer == null) continue;

            // Calculate alpha based on distance
            float alpha = 1f;
            if (distance > fadeStartDistance) {
                float fadeRange = fadeEndDistance - fadeStartDistance;
                float fadeProgress = (distance - fadeStartDistance) / fadeRange;
                alpha = Mathf.Clamp01(1f - fadeProgress);
            }

            // Apply alpha
            Color currentColor = renderer.material.color;
            currentColor.a = alpha;
            renderer.material.color = currentColor;
        }
    }

    /// <summary>
    /// Clear all arrows
    /// </summary>
    public void ClearArrows() {
        foreach (GameObject arrow in activeArrows) {
            if (arrow != null) {
                Destroy(arrow);
            }
        }
        activeArrows.Clear();
        arrowPositions.Clear();
        isVisualizingPath = false;
    }

    /// <summary>
    /// Create a default arrow prefab using Unity primitives
    /// </summary>
    private GameObject CreateDefaultArrowPrefab() {
        GameObject arrow = new GameObject("DefaultArrow");

        // Create arrow body (cylinder)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.transform.SetParent(arrow.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localRotation = Quaternion.Euler(0, 0, 90);
        body.transform.localScale = new Vector3(0.1f, 0.4f, 0.1f);

        // Create arrow head (cone)
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.transform.SetParent(arrow.transform);
        head.transform.localPosition = new Vector3(0.5f, 0, 0);
        head.transform.localRotation = Quaternion.Euler(0, 0, 45);
        head.transform.localScale = new Vector3(0.25f, 0.25f, 0.1f);

        // Apply glowing material
        Material arrowMaterial = new Material(Shader.Find("Standard"));
        arrowMaterial.color = pathColor;
        arrowMaterial.SetFloat("_Metallic", 0.2f);
        arrowMaterial.SetFloat("_Glossiness", 0.8f);
        arrowMaterial.EnableKeyword("_EMISSION");
        arrowMaterial.SetColor("_EmissionColor", pathColor * 0.5f);

        body.GetComponent<Renderer>().material = arrowMaterial;
        head.GetComponent<Renderer>().material = arrowMaterial;

        // Remove colliders (not needed for visual indicators)
        Destroy(body.GetComponent<Collider>());
        Destroy(head.GetComponent<Collider>());

        // Don't save this prefab, it's runtime only
        arrow.hideFlags = HideFlags.DontSave;

        return arrow;
    }

    /// <summary>
    /// Enable/disable arrow visualization
    /// </summary>
    public void SetVisualizationEnabled(bool enabled) {
        if (!enabled) {
            ClearArrows();
        }
        this.enabled = enabled;
    }

    private void OnDestroy() {
        ClearArrows();
        
        if (navigationController != null) {
            navigationController.OnNavigationUpdate.RemoveListener(OnNavigationUpdated);
        }
    }

    // Debug visualization
    private void OnDrawGizmos()
    {
        if (!isVisualizingPath) return;

        Gizmos.color = Color.yellow;
        foreach (Vector3 pos in arrowPositions)
        {
            Gizmos.DrawWireSphere(pos, 0.1f);
        }
    }
}
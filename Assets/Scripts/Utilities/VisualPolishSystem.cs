using UnityEngine;
using System.Collections;

/// <summary>
/// Visual polish system for enhanced graphics and effects
/// Manages lighting, materials, particles, and visual feedback
/// </summary>
public class VisualPolishSystem : MonoBehaviour
{

    [Header("References")]
    public NavigationController navigationController;

    [Header("Lighting Settings")]
    public Light directionalLight;
    public bool dynamicLighting = true;
    public Color dayLightColor = new Color(1f, 0.96f, 0.84f);
    public Color nightLightColor = new Color(0.5f, 0.6f, 0.8f);
    public float lightIntensity = 1.0f;

    [Header("Path Materials")]
    public Material pathLineMaterial;
    public Material arrowMaterial;
    public bool useEmissivePath = true;
    [Range(0, 2)]
    public float pathEmissionIntensity = 0.8f;
    public Color pathMainColor = new Color(0f, 0.8f, 1f);
    public Color pathEmissionColor = new Color(0f, 1f, 1f);

    [Header("Particle Effects")]
    public GameObject arrivalParticlesPrefab;
    public GameObject pathParticlesPrefab;
    public bool enableParticles = true;
    public int particlesPerMeter = 2;

    [Header("Target Visual Effects")]
    public GameObject targetGlowPrefab;
    public bool pulseTargetMarker = true;
    public float targetPulseSpeed = 1.5f;
    public Color targetGlowColor = Color.yellow;

    [Header("Ambient Effects")]
    public bool enableAmbientOcclusion = true;
    public bool enableBloom = true;
    public float bloomIntensity = 0.5f;

    [Header("Floor Materials")]
    public Material[] floorMaterials = new Material[6];
    public bool enhanceFloorMaterials = true;

    // Private variables
    private GameObject targetGlowInstance;
    private GameObject pathParticlesInstance;
    private float timeOfDay = 0.5f; // 0 = midnight, 0.5 = noon, 1 = midnight
    private Coroutine arrivalEffectCoroutine;

    private void Start()
    {
        if (navigationController == null)
        {
            navigationController = FindObjectOfType<NavigationController>();
        }

        // Setup lighting
        SetupLighting();

        // Setup materials
        SetupMaterials();

        // Setup post-processing
        SetupPostProcessing();

        // Subscribe to events
        if (navigationController != null)
        {
            navigationController.OnNavigationUpdate.AddListener(OnNavigationUpdate);
            navigationController.OnTargetReached.AddListener(OnTargetReached);
        }

        // Create target glow
        CreateTargetGlow();
    }

    private void Update()
    {
        // Update dynamic lighting
        if (dynamicLighting && directionalLight != null)
        {
            UpdateDynamicLighting();
        }

        // Update target glow
        if (pulseTargetMarker && targetGlowInstance != null)
        {
            UpdateTargetGlow();
        }
    }

    #region Lighting

    /// <summary>
    /// Setup scene lighting
    /// </summary>
    private void SetupLighting()
    {
        if (directionalLight == null)
        {
            directionalLight = FindObjectOfType<Light>();
        }

        if (directionalLight != null)
        {
            directionalLight.type = LightType.Directional;
            directionalLight.intensity = lightIntensity;
            directionalLight.shadows = LightShadows.Soft;
            directionalLight.shadowStrength = 0.7f;
        }

        // Setup ambient lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.7f, 0.8f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.5f, 0.5f, 0.5f);
        RenderSettings.ambientGroundColor = new Color(0.3f, 0.3f, 0.3f);
        RenderSettings.ambientIntensity = 1.0f;
    }

    /// <summary>
    /// Update lighting based on time of day
    /// </summary>
    private void UpdateDynamicLighting()
    {
        // Slowly cycle through day/night (for demo purposes)
        timeOfDay += Time.deltaTime * 0.01f; // Very slow cycle
        if (timeOfDay > 1f) timeOfDay = 0f;

        // Interpolate light color
        Color currentColor = Color.Lerp(nightLightColor, dayLightColor,
            Mathf.Sin(timeOfDay * Mathf.PI));

        if (directionalLight != null)
        {
            directionalLight.color = currentColor;
        }
    }

    /// <summary>
    /// Set specific lighting preset
    /// </summary>
    public void SetLightingPreset(string preset)
    {
        if (directionalLight == null) return;

        switch (preset.ToLower())
        {
            case "day":
                directionalLight.color = dayLightColor;
                directionalLight.intensity = 1.0f;
                break;
            case "night":
                directionalLight.color = nightLightColor;
                directionalLight.intensity = 0.5f;
                break;
            case "indoor":
                directionalLight.color = new Color(1f, 0.95f, 0.8f);
                directionalLight.intensity = 0.8f;
                break;
        }
    }

    #endregion

    #region Materials

    /// <summary>
    /// Setup and enhance materials
    /// </summary>
    private void SetupMaterials()
    {
        // Create path line material if not assigned
        if (pathLineMaterial == null)
        {
            pathLineMaterial = CreatePathMaterial();
        }

        // Create arrow material if not assigned
        if (arrowMaterial == null)
        {
            arrowMaterial = CreateArrowMaterial();
        }

        // Enhance floor materials
        if (enhanceFloorMaterials)
        {
            EnhanceFloorMaterials();
        }
    }

    /// <summary>
    /// Create enhanced path material
    /// </summary>
    private Material CreatePathMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = pathMainColor;
        mat.SetFloat("_Metallic", 0.2f);
        mat.SetFloat("_Glossiness", 0.8f);

        if (useEmissivePath)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", pathEmissionColor * pathEmissionIntensity);
        }

        return mat;
    }

    /// <summary>
    /// Create enhanced arrow material
    /// </summary>
    private Material CreateArrowMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = pathMainColor;
        mat.SetFloat("_Metallic", 0.3f);
        mat.SetFloat("_Glossiness", 0.9f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", pathEmissionColor * pathEmissionIntensity);

        return mat;
    }

    /// <summary>
    /// Enhance floor materials with better properties
    /// </summary>
    private void EnhanceFloorMaterials()
    {
        for (int i = 0; i < floorMaterials.Length; i++)
        {
            if (floorMaterials[i] == null) continue;

            // Add subtle properties
            floorMaterials[i].SetFloat("_Metallic", 0.1f);
            floorMaterials[i].SetFloat("_Glossiness", 0.3f);

            // Enable normal mapping if available
            if (floorMaterials[i].HasProperty("_BumpMap"))
            {
                floorMaterials[i].EnableKeyword("_NORMALMAP");
            }
        }
    }

    /// <summary>
    /// Apply material to path renderer
    /// </summary>
    public void ApplyPathMaterial(LineRenderer lineRenderer)
    {
        if (lineRenderer != null && pathLineMaterial != null)
        {
            lineRenderer.material = pathLineMaterial;
            lineRenderer.startWidth = 0.2f;
            lineRenderer.endWidth = 0.2f;
            lineRenderer.startColor = pathMainColor;
            lineRenderer.endColor = pathMainColor;
        }
    }

    #endregion

    #region Particle Effects

    /// <summary>
    /// Called when navigation updates
    /// </summary>
    private void OnNavigationUpdate(float distance, float eta)
    {
        if (enableParticles && navigationController.HasValidPath)
        {
            UpdatePathParticles();
        }
    }

    /// <summary>
    /// Update path particles
    /// </summary>
    private void UpdatePathParticles()
    {
        if (pathParticlesPrefab == null || navigationController == null) return;

        // Create particles along path if not exists
        if (pathParticlesInstance == null)
        {
            pathParticlesInstance = Instantiate(pathParticlesPrefab, transform);
        }

        // Position at next waypoint
        if (navigationController.CalculatedPath.corners.Length > 1)
        {
            Vector3 nextCorner = navigationController.CalculatedPath.corners[1];
            pathParticlesInstance.transform.position = nextCorner;
        }
    }

    /// <summary>
    /// Called when target is reached
    /// </summary>
    private void OnTargetReached()
    {
        if (enableParticles && arrivalParticlesPrefab != null)
        {
            PlayArrivalEffect();
        }
    }

    /// <summary>
    /// Play arrival particle effect
    /// </summary>
    private void PlayArrivalEffect()
    {
        if (arrivalEffectCoroutine != null)
        {
            StopCoroutine(arrivalEffectCoroutine);
        }
        arrivalEffectCoroutine = StartCoroutine(ArrivalEffectSequence());
    }

    /// <summary>
    /// Arrival effect animation sequence
    /// </summary>
    private IEnumerator ArrivalEffectSequence()
    {
        // Spawn particles at target location
        if (arrivalParticlesPrefab != null && navigationController != null)
        {
            GameObject particles = Instantiate(
                arrivalParticlesPrefab,
                navigationController.TargetPosition,
                Quaternion.identity
            );

            // Auto-destroy after 3 seconds
            Destroy(particles, 3f);
        }

        // Flash target glow
        if (targetGlowInstance != null)
        {
            for (int i = 0; i < 3; i++)
            {
                targetGlowInstance.SetActive(false);
                yield return new WaitForSeconds(0.1f);
                targetGlowInstance.SetActive(true);
                yield return new WaitForSeconds(0.1f);
            }
        }

        arrivalEffectCoroutine = null;
    }

    /// <summary>
    /// Create default arrival particles if prefab not assigned
    /// </summary>
    private GameObject CreateDefaultArrivalParticles()
    {
        GameObject particles = new GameObject("ArrivalParticles");
        ParticleSystem ps = particles.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = 2f;
        main.startSpeed = 2f;
        main.startSize = 0.3f;
        main.startColor = new Color(1f, 0.8f, 0f, 1f);
        main.maxParticles = 50;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 50)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;

        return particles;
    }

    #endregion

    #region Target Visual Effects

    /// <summary>
    /// Create glowing effect for target
    /// </summary>
    private void CreateTargetGlow()
    {
        if (targetGlowPrefab != null)
        {
            targetGlowInstance = Instantiate(targetGlowPrefab, transform);
        }
        else
        {
            targetGlowInstance = CreateDefaultTargetGlow();
        }

        targetGlowInstance.SetActive(false);
    }

    /// <summary>
    /// Update target glow position and animation
    /// </summary>
    private void UpdateTargetGlow()
    {
        if (navigationController == null || !navigationController.IsNavigating)
        {
            if (targetGlowInstance != null)
            {
                targetGlowInstance.SetActive(false);
            }
            return;
        }

        if (targetGlowInstance != null)
        {
            targetGlowInstance.SetActive(true);
            targetGlowInstance.transform.position = navigationController.TargetPosition;

            // Pulse animation
            float pulse = (Mathf.Sin(Time.time * targetPulseSpeed) + 1f) * 0.5f;
            float scale = 0.8f + pulse * 0.4f;
            targetGlowInstance.transform.localScale = Vector3.one * scale;

            // Rotate glow
            targetGlowInstance.transform.Rotate(Vector3.up, 50f * Time.deltaTime);
        }
    }

    /// <summary>
    /// Create default target glow
    /// </summary>
    private GameObject CreateDefaultTargetGlow()
    {
        GameObject glow = new GameObject("TargetGlow");

        // Create rings
        for (int i = 0; i < 3; i++)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.transform.SetParent(glow.transform);
            ring.transform.localPosition = Vector3.up * (i * 0.3f);
            ring.transform.localScale = new Vector3(1f + i * 0.3f, 0.05f, 1f + i * 0.3f);

            Material mat = new Material(Shader.Find("Standard"));
            mat.color = targetGlowColor;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", targetGlowColor * 2f);
            mat.SetFloat("_Metallic", 0.5f);
            mat.SetFloat("_Glossiness", 0.9f);

            Renderer renderer = ring.GetComponent<Renderer>();
            renderer.material = mat;

            Destroy(ring.GetComponent<Collider>());
        }

        return glow;
    }

    #endregion

    #region Post-Processing

    /// <summary>
    /// Setup post-processing effects
    /// </summary>
    private void SetupPostProcessing()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        // Note: This requires Post Processing Stack v2 package
        // If not installed, these will be ignored

#if UNITY_POST_PROCESSING_STACK_V2
        if (enableAmbientOcclusion) {
            // Add AO
        }

        if (enableBloom) {
            // Add Bloom
        }
#endif

        // Enable HDR
        mainCam.allowHDR = true;
        mainCam.allowMSAA = true;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Toggle particle effects
    /// </summary>
    public void ToggleParticles(bool enabled)
    {
        enableParticles = enabled;

        if (!enabled && pathParticlesInstance != null)
        {
            pathParticlesInstance.SetActive(false);
        }
    }

    /// <summary>
    /// Set path color
    /// </summary>
    public void SetPathColor(Color color)
    {
        pathMainColor = color;
        if (pathLineMaterial != null)
        {
            pathLineMaterial.color = color;
        }
        if (arrowMaterial != null)
        {
            arrowMaterial.color = color;
        }
    }
}
    /// <summary>
    /// Set emission intensity
    #endregion
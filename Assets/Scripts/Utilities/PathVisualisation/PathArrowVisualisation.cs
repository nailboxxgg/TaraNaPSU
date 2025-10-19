using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class PathArrowVisualisation : MonoBehaviour {

    [SerializeField]
    private NavigationController navigationController;
    [SerializeField]
    private GameObject arrow;
    [SerializeField]
    private Slider navigationYOffset;
    [SerializeField]
    private float moveOnDistance;
    [SerializeField] private Renderer arrowRenderer;
    [SerializeField] private Color baseColor = Color.cyan;
    [SerializeField] private float emissionIntensity = 2f;
    [SerializeField] private float pulseSpeed = 2f;

    private Material arrowMaterial;
    private NavMeshPath path;
    private float currentDistance;
    private Vector3[] pathOffset;
    private Vector3 nextNavigationPoint = Vector3.zero;

    private void Start()
    {
        if (arrow != null)
        {
            arrowRenderer = arrow.GetComponentInChildren<Renderer>();
            arrowMaterial = arrowRenderer.material;
            arrowMaterial.EnableKeyword("_EMISSION");
        }
    }
    private void Update()
    {
        path = navigationController.CalculatedPath;

        AddOffsetToPath();
        SelectNextNavigationPoint();
        AddArrowOffset();

        float hover = Mathf.Sin(Time.time * 2f) * 0.05f;
        arrow.transform.position = new Vector3(arrow.transform.position.x, navigationYOffset.value + hover, arrow.transform.position.z);


        arrow.transform.LookAt(nextNavigationPoint);

        if (arrowMaterial != null)
            {
                float emission = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
                Color finalColor = baseColor * Mathf.LinearToGammaSpace(emission * emissionIntensity);
                arrowMaterial.SetColor("_EmissionColor", finalColor);
            }
    }

    private void AddOffsetToPath() {
        pathOffset = new Vector3[path.corners.Length];
        for (int i = 0; i < path.corners.Length; i++) {
            pathOffset[i] = new Vector3(path.corners[i].x, transform.position.y, path.corners[i].z);
        }
    }

    private void SelectNextNavigationPoint() {
        nextNavigationPoint = SelectNextNavigationPointWithinDistance();
    }

    private Vector3 SelectNextNavigationPointWithinDistance() {
        for (int i = 0; i < pathOffset.Length; i++) {
            currentDistance = Vector3.Distance(transform.position, pathOffset[i]);
            if (currentDistance > moveOnDistance) {
                return pathOffset[i];
            }
        }
        return navigationController.TargetPosition;
    }

    private void AddArrowOffset() {
        if (navigationYOffset.value != 0) {
            arrow.transform.position = new Vector3(arrow.transform.position.x, navigationYOffset.value, arrow.transform.position.z);
        }
    }
}

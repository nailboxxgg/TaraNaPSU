using UnityEngine;
using UnityEngine.UI;

public class ZoomControlsUI : MonoBehaviour
{
    [Header("Buttons")]
    public Button zoomInButton;
    public Button zoomOutButton;

    [Header("References")]
    public Map2DController mapController;

    void Start()
    {
        if (zoomInButton != null)
            zoomInButton.onClick.AddListener(OnZoomIn);

        if (zoomOutButton != null)
            zoomOutButton.onClick.AddListener(OnZoomOut);
    }

    void OnZoomIn()
    {
        if (mapController != null)
            mapController.ZoomIn();
    }

    void OnZoomOut()
    {
        if (mapController != null)
            mapController.ZoomOut();
    }
}

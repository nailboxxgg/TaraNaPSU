using UnityEngine;
using UnityEngine.UI;

public class NavigationPanelController : MonoBehaviour
{
    [Header("Navigation Panel Buttons")]
    public Button StopButton;
    public Button ChangeLocationButton;

    void Start()
    {
        
        if (StopButton != null)
        {
            StopButton.onClick.AddListener(OnStopClicked);
        }

        if (ChangeLocationButton != null)
        {
            ChangeLocationButton.onClick.AddListener(OnChangeLocationClicked);
        }
    }

    private void OnStopClicked()
    {
        if (AppFlowController.Instance != null)
        {
            AppFlowController.Instance.StopNavigation();
        }
    }

    private void OnChangeLocationClicked()
    {
        if (AppFlowController.Instance != null)
        {
            AppFlowController.Instance.ChangeLocation();
        }
    }
}

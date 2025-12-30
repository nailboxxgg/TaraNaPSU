using UnityEngine;

public class SettingsController : MonoBehaviour
{
    [Header("Menu References")]
    public GameObject MenuPanel;

    private bool isMenuOpen = false;

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        MenuPanel.SetActive(isMenuOpen);
    }

    public void CloseMenu()
    {
        isMenuOpen = false;
        MenuPanel.SetActive(false);
    }

    public void OpenAbout()
    {
        Debug.Log("About panel coming soon...");
    }

    public void OpenSettings()
    {
        Debug.Log("Settings panel coming soon...");
    }
}


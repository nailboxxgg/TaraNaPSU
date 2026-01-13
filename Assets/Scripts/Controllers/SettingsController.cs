using UnityEngine;

public class BurgerMenuController : MonoBehaviour
{
    [Header("Settings References")]
    public GameObject Settings;
    private bool isSettingsOpen = false;

    public void ToggleMenu()
    {
        isSettingsOpen = !isSettingsOpen;
        Settings.SetActive(isSettingsOpen);
    }

    public void CloseSettings()
    {
        isSettingsOpen = false;
        Settings.SetActive(false);
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

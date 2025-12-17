using UnityEngine;

public class BurgerMenuController : MonoBehaviour
{
    [Header("Menu References")]
    public GameObject MenuPanel;
    public GameObject AdminLogInPanel;

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

    public void OpenAdminLogInPanel()
    {
        Debug.Log("Opening Admin Mode...");
        CloseMenu();

        if (AdminLogInPanel != null)
            AdminLogInPanel.SetActive(true);
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

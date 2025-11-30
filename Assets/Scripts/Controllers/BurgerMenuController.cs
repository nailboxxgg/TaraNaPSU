using UnityEngine;

public class BurgerMenuController : MonoBehaviour
{
    [Header("Menu References")]
    public GameObject MenuPanel;
    public GameObject AdminLogInPanel;
    public GameObject SettingsPanel;

    private bool isMenuOpen = false;
    private SettingsPanelController settingsPanelController;

    void Awake()
    {
        // Auto-discover SettingsPanel if not assigned
        if (SettingsPanel == null)
        {
            // Try to find SettingsPanelController first
            settingsPanelController = FindObjectOfType<SettingsPanelController>();

            if (settingsPanelController != null)
            {
                SettingsPanel = settingsPanelController.SettingsPanel;
                Debug.Log("[BurgerMenuController] Auto-discovered SettingsPanel from SettingsPanelController");
            }
            else
            {
                // Try to find by name or tag as fallback
                GameObject foundPanel = GameObject.Find("SettingsPanel");
                if (foundPanel == null)
                {
                    foundPanel = GameObject.FindGameObjectWithTag("SettingsPanel");
                }

                if (foundPanel != null)
                {
                    SettingsPanel = foundPanel;
                    settingsPanelController = foundPanel.GetComponent<SettingsPanelController>();
                    Debug.Log("[BurgerMenuController] Auto-discovered SettingsPanel by name/tag");
                }
            }
        }
        else
        {
            // Get the SettingsPanelController if SettingsPanel was manually assigned
            settingsPanelController = SettingsPanel.GetComponent<SettingsPanelController>();
        }

        // Log warning if still not found
        if (SettingsPanel == null)
        {
            Debug.LogWarning("[BurgerMenuController] Could not find SettingsPanel! Please ensure:\n" +
                           "1. SettingsPanel GameObject exists in the scene\n" +
                           "2. SettingsPanelController component is attached to it\n" +
                           "3. Named 'SettingsPanel' or tagged 'SettingsPanel'");
        }
    }

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
        CloseMenu();

        if (SettingsPanel != null)
        {
            SettingsPanel.SetActive(true);

            // Use SettingsPanelController to properly open settings
            if (settingsPanelController != null)
            {
                settingsPanelController.OpenSettings();
                Debug.Log("[BurgerMenuController] Settings panel opened via SettingsPanelController");
            }
            else
            {
                Debug.Log("[BurgerMenuController] Settings panel opened (no SettingsPanelController found)");
            }
        }
        else
        {
            Debug.LogError("[BurgerMenuController] SettingsPanel reference is still null! " +
                          "Please create a SettingsPanel GameObject and attach SettingsPanelController to it.");
        }
    }
}

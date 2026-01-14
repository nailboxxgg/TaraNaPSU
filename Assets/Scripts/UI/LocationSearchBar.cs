using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LocationSearchBar : MonoBehaviour
{
    [Header("UI Components")]
    public TMP_InputField inputField;
    public GameObject suggestionRoot; // The ScrollView or container to toggle
    public Transform suggestionContainer; // The Content object to parent items to
    public GameObject suggestionItemPrefab;
    public Button clearButton;
    public Button toggleButton; // New: Arrow button to show all items

    [Header("References")]
    public AppFlowController2D appFlow;

    private List<string> allTargetNames = new List<string>();
    private List<GameObject> activeSuggestions = new List<GameObject>();
    private string selectedTarget;

    void Start()
    {
        // Try to find AppFlow if not assigned
        if (appFlow == null) appFlow = AppFlowController2D.Instance;
        if (appFlow == null) appFlow = FindObjectOfType<AppFlowController2D>();

        LoadTargets();

        if (inputField != null)
        {
            inputField.onValueChanged.AddListener(OnInputChanged);
            inputField.onSelect.AddListener(OnInputSelected);
        }

        if (clearButton != null)
            clearButton.onClick.AddListener(ClearSelection);

        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(() => {
                bool isVisible = suggestionRoot != null && suggestionRoot.activeSelf;
                if (isVisible) HideSuggestions();
                else ShowAllSuggestions();
            });
        }

        HideSuggestions();
    }

    void LoadTargets()
    {
        if (TargetManager.Instance != null)
        {
            allTargetNames = TargetManager.Instance.GetAllTargetNames();
            allTargetNames.Sort(); // Alphabetical sorting for easier discovery
            Debug.Log($"[SearchBar] Loaded {allTargetNames.Count} targets");
        }
    }

    void OnInputSelected(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            ShowAllSuggestions();
        }
        else
        {
            FilterSuggestions(text);
        }
    }

    void OnInputChanged(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            ShowAllSuggestions();
        }
        else
        {
            FilterSuggestions(text);
        }
    }

    void ShowAllSuggestions()
    {
        ClearSuggestions();

        foreach (string name in allTargetNames)
        {
            CreateSuggestionItem(name);
        }

        if (suggestionRoot != null)
            suggestionRoot.SetActive(true);
    }

    void FilterSuggestions(string query)
    {
        ClearSuggestions();

        string lowerQuery = query.ToLower();

        foreach (string name in allTargetNames)
        {
            if (name.ToLower().Contains(lowerQuery))
            {
                CreateSuggestionItem(name);
            }
        }

        if (suggestionRoot != null)
            suggestionRoot.SetActive(activeSuggestions.Count > 0);
    }

    void CreateSuggestionItem(string targetName)
    {
        if (suggestionItemPrefab == null || suggestionContainer == null) return;

        GameObject item = Instantiate(suggestionItemPrefab, suggestionContainer);
        activeSuggestions.Add(item);

        TMP_Text textComponent = item.GetComponentInChildren<TMP_Text>();
        if (textComponent != null)
            textComponent.text = targetName;

        Button button = item.GetComponent<Button>();
        if (button != null)
        {
            string name = targetName;
            button.onClick.AddListener(() => SelectTarget(name));
        }
    }

    void SelectTarget(string targetName)
    {
        selectedTarget = targetName;

        if (inputField != null)
            inputField.text = targetName;

        HideSuggestions();

        HideSuggestions();

        // Strictly use the Instance to ensure we talk to the correct manager
        var flow = AppFlowController2D.Instance;
        var tm = TargetManager.Instance;

        if (tm == null)
        {
            Debug.LogError("[SearchBar] FATAL: TargetManager.Instance is null! Is there a TargetManager in the scene?");
            return;
        }

        if (tm.TryGetTarget(targetName, out TargetData data))
        {
            if (flow != null)
            {
                flow.OnDestinationSelected(targetName, data.Position.ToVector3(), data.FloorNumber + 1);
                Debug.Log($"[SearchBar] Successfully reported to AppFlow: {targetName}");
            }
            else
            {
                Debug.LogError("[SearchBar] FATAL: Could not find any AppFlowController2D in the scene!");
            }
        }
        else
        {
            // FALLBACK: If TryGetTarget fails, it's a name mismatch. Let's try to find it manually.
            Debug.LogWarning($"[SearchBar] TryGetTarget failed for '{targetName}'. Attempting manual fallback search...");
            
            bool foundFallback = false;
            foreach (var name in tm.GetAllTargetNames())
            {
                if (name.Trim().ToLower() == targetName.Trim().ToLower())
                {
                    if (tm.TryGetTarget(name, out data))
                    {
                        if (flow != null)
                        {
                            flow.OnDestinationSelected(name, data.Position.ToVector3(), data.FloorNumber + 1);
                            Debug.Log($"[SearchBar] Fallback Match Success! Reported: {name}");
                            foundFallback = true;
                            break;
                        }
                    }
                }
            }

            if (!foundFallback)
            {
                Debug.LogError($"[SearchBar] ERROR: Target '{targetName}' not found in TargetManager! (Available: {tm.GetAllTargetNames().Count})");
            }
        }

        Debug.Log($"[SearchBar] Selected: {targetName}");
    }

    void ClearSuggestions()
    {
        foreach (var item in activeSuggestions)
        {
            if (item != null)
                Destroy(item);
        }
        activeSuggestions.Clear();
    }

    void HideSuggestions()
    {
        ClearSuggestions();
        if (suggestionRoot != null)
            suggestionRoot.SetActive(false);
    }

    public void ClearSelection()
    {
        selectedTarget = null;
        if (inputField != null)
            inputField.text = "";
        HideSuggestions();
    }

    public string GetSelectedTarget() => selectedTarget;
}

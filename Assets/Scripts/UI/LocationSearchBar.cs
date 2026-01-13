using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LocationSearchBar : MonoBehaviour
{
    [Header("UI Components")]
    public TMP_InputField inputField;
    public Transform suggestionContainer;
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
        LoadTargets();

        if (inputField != null)
        {
            inputField.onValueChanged.AddListener(OnInputChanged);
            inputField.onSelect.AddListener(OnInputSelected);
        }

        if (clearButton != null)
            clearButton.onClick.AddListener(ClearSelection);

        if (toggleButton != null)
            toggleButton.onClick.AddListener(() => {
                if (suggestionContainer.gameObject.activeSelf) HideSuggestions();
                else ShowAllSuggestions();
            });

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

        if (suggestionContainer != null)
            suggestionContainer.gameObject.SetActive(true);
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

        if (suggestionContainer != null)
            suggestionContainer.gameObject.SetActive(activeSuggestions.Count > 0);
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

        if (TargetManager.Instance != null && 
            TargetManager.Instance.TryGetTarget(targetName, out TargetData data))
        {
            if (appFlow != null)
            {
                appFlow.OnDestinationSelected(targetName, data.Position.ToVector3(), data.FloorNumber + 1); // +1 because JSON 0 is Ground Floor, and our System 0 is Campus
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
        if (suggestionContainer != null)
            suggestionContainer.gameObject.SetActive(false);
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

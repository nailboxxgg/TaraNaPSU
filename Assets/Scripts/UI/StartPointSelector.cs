using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StartPointSelector : MonoBehaviour
{
    [Header("UI Components")]
    public TMP_InputField inputField;
    public GameObject suggestionRoot; // The ScrollView or container to toggle
    public Transform suggestionContainer; // The Content object to parent items to
    public GameObject suggestionItemPrefab;
    public Button clearButton;
    public Button toggleButton;

    [Header("References")]
    public AppFlowController2D appFlow;

    private List<AnchorManager.AnchorData> entranceAnchors = new List<AnchorManager.AnchorData>();
    private List<GameObject> activeSuggestions = new List<GameObject>();
    private AnchorManager.AnchorData selectedAnchor;

    void Start()
    {
        // Try to find AppFlow if not assigned
        if (appFlow == null) appFlow = AppFlowController2D.Instance;
        if (appFlow == null) appFlow = FindObjectOfType<AppFlowController2D>();

        LoadEntrancePoints();

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
                if (suggestionContainer.gameObject.activeSelf) HideSuggestions();
                else ShowAllSuggestions();
            });
        }

        HideSuggestions();
    }

    void LoadEntrancePoints()
    {
        entranceAnchors.Clear();

        if (AnchorManager.Instance != null)
        {
            foreach (var anchor in AnchorManager.Instance.Anchors)
            {
                if (anchor.Type?.ToLower() == "entrance")
                {
                    entranceAnchors.Add(anchor);
                }
            }
        }

        Debug.Log($"[StartPoint] Loaded {entranceAnchors.Count} entrance points");
    }

    void OnInputSelected(string text)
    {
        // When the user clicks/focuses the field, show all possibilities so they can change it
        ShowAllSuggestions();
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

        foreach (var anchor in entranceAnchors)
        {
            CreateSuggestionItem(anchor);
        }

        if (suggestionRoot != null)
            suggestionRoot.SetActive(true);
    }

    void FilterSuggestions(string query)
    {
        ClearSuggestions();

        string lowerQuery = query.ToLower();

        foreach (var anchor in entranceAnchors)
        {
            string displayName = !string.IsNullOrEmpty(anchor.Meta) ? anchor.Meta : anchor.AnchorId;
            if (displayName.ToLower().Contains(lowerQuery))
            {
                CreateSuggestionItem(anchor);
            }
        }

        if (suggestionRoot != null)
            suggestionRoot.SetActive(activeSuggestions.Count > 0);
    }

    void CreateSuggestionItem(AnchorManager.AnchorData anchor)
    {
        if (suggestionItemPrefab == null || suggestionContainer == null) return;

        GameObject item = Instantiate(suggestionItemPrefab, suggestionContainer);
        activeSuggestions.Add(item);

        TMP_Text textComponent = item.GetComponentInChildren<TMP_Text>();
        if (textComponent != null)
        {
            string displayName = !string.IsNullOrEmpty(anchor.Meta) ? anchor.Meta : anchor.AnchorId;
            textComponent.text = displayName;
        }

        Button button = item.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => SelectAnchor(anchor));
        }
    }

    void SelectAnchor(AnchorManager.AnchorData anchor)
    {
        selectedAnchor = anchor;

        if (inputField != null)
        {
            string displayName = !string.IsNullOrEmpty(anchor.Meta) ? anchor.Meta : anchor.AnchorId;
            inputField.text = displayName;
        }

        HideSuggestions();

        // Use the robust Instance property
        var flow = AppFlowController2D.Instance;
        if (flow != null)
        {
            int mappedFloor = flow.MapFloor(anchor.BuildingId, anchor.Floor);
            flow.OnStartPointSelected(anchor.AnchorId, anchor.PositionVector, mappedFloor);
            Debug.Log($"[StartPoint] Successfully reported to AppFlow: {anchor.AnchorId} on System Floor {mappedFloor}");
        }
        else
        {
            Debug.LogError("[StartPoint] FATAL: Could not find any AppFlowController2D in the scene!");
        }

        Debug.Log($"[StartPoint] Selected: {anchor.AnchorId}");
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
        selectedAnchor = null;
        if (inputField != null)
            inputField.text = "";
        HideSuggestions();
    }

    public AnchorManager.AnchorData GetSelectedAnchor() => selectedAnchor;
}

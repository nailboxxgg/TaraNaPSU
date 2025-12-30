using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SearchBarQR : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField searchInputField;
    public Button backButton;
    public Button menuButton;

    [Header("Suggestions")]
    public RectTransform suggestionPanel;
    public RectTransform suggestionsContainer;
    public GameObject suggestionItemPrefab;

    private List<string> allTargets = new();
    private List<GameObject> activeSuggestions = new();

    private void Start()
    {
        if (TargetManager.Instance != null)
            allTargets = TargetManager.Instance.GetAllTargetNames();

        searchInputField.onValueChanged.AddListener(OnInputChanged);
        searchInputField.onSubmit.AddListener(OnSearchSubmitted);

        backButton.onClick.AddListener(() => Debug.Log("Back Button Pressed"));
        menuButton.onClick.AddListener(() => Debug.Log("Menu Button Pressed"));

        suggestionPanel.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        searchInputField.onValueChanged.RemoveListener(OnInputChanged);
        searchInputField.onSubmit.RemoveListener(OnSearchSubmitted);
    }

    private void OnInputChanged(string text)
    {
        ClearSuggestions();

        if (string.IsNullOrWhiteSpace(text))
        {
            suggestionPanel.gameObject.SetActive(false);
            return;
        }

        var matches = allTargets.FindAll(t =>
            t.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0);

        if (matches.Count == 0)
        {
            suggestionPanel.gameObject.SetActive(false);
            return;
        }

        suggestionPanel.gameObject.SetActive(true);

        foreach (var match in matches)
        {
            var item = Instantiate(suggestionItemPrefab, suggestionsContainer);
            item.GetComponentInChildren<TMP_Text>().text = match;
            item.GetComponent<Button>().onClick.AddListener(() => OnSuggestionSelected(match));
            activeSuggestions.Add(item);
        }
    }

    private void ClearSuggestions()
    {
        foreach (var item in activeSuggestions)
            Destroy(item);
        activeSuggestions.Clear();
    }

    private void OnSuggestionSelected(string name)
    {
        searchInputField.text = name;
        suggestionPanel.gameObject.SetActive(false);
        AppFlowController.Instance.OnDestinationSelected(name);
    }

    private void OnSearchSubmitted(string text)
    {
        suggestionPanel.gameObject.SetActive(false);
        
        if (AppFlowController.Instance != null && !string.IsNullOrEmpty(text))
        {
            AppFlowController.Instance.OnDestinationSelected(text);
            AppFlowController.Instance.ShowNavigationPanel(text);
        }
        else
        {
            Debug.LogWarning("AppFlowController instance not found or invalid text.");
        }
    }

    public void ClearSelection()
    {
        searchInputField.text = "";
        ClearSuggestions();
        suggestionPanel.gameObject.SetActive(false);
        searchInputField.DeactivateInputField();
    }
}

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

    [Header("Movement Target")]
    public Transform player; // XR Origin or Main Camera
    public float moveSpeed = 3f;

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
        MoveToTarget(text);
    }

    private void MoveToTarget(string name)
    {
        if (!TargetManager.Instance.TryGetTarget(name, out var target))
        {
            Debug.Log($"❌ Target not found: {name}");
            return;
        }

        StartCoroutine(SmoothMove(player, target.Position.ToVector3(),
                                  Quaternion.Euler(target.Rotation.ToVector3())));
    }

    private IEnumerator SmoothMove(Transform obj, Vector3 endPos, Quaternion endRot)
    {
        Vector3 startPos = obj.position;
        Quaternion startRot = obj.rotation;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            obj.position = Vector3.Lerp(startPos, endPos, t);
            obj.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
    }
}

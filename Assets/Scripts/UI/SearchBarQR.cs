using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SearchBarQR : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField searchInputField;
    public Button backButton;
    public Button menuButton;
    public Button dropdownToggleButton; // New button to toggle dropdown

    [Header("Dropdown")]
    public RectTransform dropdownPanel;
    public RectTransform dropdownContainer;
    public GameObject dropdownItemPrefab;
    public TMP_Text placeholderText; // Text showing "Select Destination..."

    [Header("Settings")]
    public bool showAllTargetsOnStart = true;
    public bool enableSearchFiltering = true; // Allow typing to filter dropdown

    [Header("Animation Settings")]
    public float animationDuration = 0.2f;
    public AnimationCurve animationCurve;

    [Header("Movement Target")]
    public Transform player; // XR Origin or Main Camera
    public float moveSpeed = 3f;

    private List<string> allTargets = new();
    private List<GameObject> activeDropdownItems = new();
    private bool isDropdownOpen = false;
    private Coroutine currentAnimation;

    private void Start()
    {
        HideDropdown();

        // Create default animation curve if not set
        if (animationCurve.keys.Length == 0)
        {
            // Create a simple ease-in-out curve (0 to 1)
            Keyframe[] keys = new Keyframe[]
            {
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(0.5f, 0.8f, 1f, 1f),
                new Keyframe(1f, 1f, 0f, 0f)
            };
            animationCurve = new AnimationCurve(keys);
        }

        if (TargetManager.Instance != null)
            allTargets = TargetManager.Instance.GetAllTargetNames();

        // Sort targets alphabetically for better user experience
        allTargets.Sort();

        // Setup input field
        searchInputField.onValueChanged.AddListener(OnInputChanged);
        searchInputField.onSelect.AddListener(OnInputFieldSelected);
        searchInputField.onDeselect.AddListener(OnInputFieldDeselected);
        searchInputField.onSubmit.AddListener(OnSearchSubmitted);

        // Setup buttons
        backButton.onClick.AddListener(() => Debug.Log("Back Button Pressed"));
        menuButton.onClick.AddListener(() => Debug.Log("Menu Button Pressed"));

        if (dropdownToggleButton != null)
            dropdownToggleButton.onClick.AddListener(ToggleDropdown);

        // Setup placeholder text
        if (placeholderText != null)
            placeholderText.text = "Select Destination...";

        // Initially hide dropdown
        if (dropdownPanel != null)
            dropdownPanel.gameObject.SetActive(false);

        // Show all targets on start if enabled
        if (showAllTargetsOnStart == false)
        {
            ShowAllTargets();
        }
    }

    private void OnDestroy()
    {
        searchInputField.onValueChanged.RemoveListener(OnInputChanged);
        searchInputField.onSelect.RemoveListener(OnInputFieldSelected);
        searchInputField.onDeselect.RemoveListener(OnInputFieldDeselected);
        searchInputField.onSubmit.RemoveListener(OnSearchSubmitted);

        if (dropdownToggleButton != null)
            dropdownToggleButton.onClick.RemoveListener(ToggleDropdown);
    }

    private void ShowDropdownAnimated()
    {
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        currentAnimation = StartCoroutine(AnimateDropdown(true));
    }

    private void HideDropdownAnimated()
    {
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        currentAnimation = StartCoroutine(AnimateDropdown(false));
    }

    private IEnumerator AnimateDropdown(bool show)
    {
        if (dropdownPanel == null) yield break;

        dropdownPanel.gameObject.SetActive(true);

        // Get current scale
        Vector3 targetScale = show ? Vector3.one : Vector3.one;
        Vector3 startScale = show ? Vector3.one * 0.8f : Vector3.one;

        if (show)
        {
            dropdownPanel.transform.localScale = startScale;
        }

        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            float curveValue = animationCurve.Evaluate(progress);

            dropdownPanel.transform.localScale = Vector3.Lerp(startScale, targetScale, curveValue);

            yield return null;
        }

        dropdownPanel.transform.localScale = targetScale;

        if (!show)
        {
            dropdownPanel.gameObject.SetActive(false);
        }

        currentAnimation = null;
    }

    private void OnInputFieldSelected(string text)
    {
        // Always show dropdown when input field is clicked/selected
        if (!isDropdownOpen)
        {
            ShowAllTargets();
        }
    }

    private void OnInputFieldDeselected(string text)
    {
        // Optional: Close dropdown when input field loses focus
        // Comment this out if you want dropdown to stay open
        // HideDropdown();
    }

    private void OnInputChanged(string text)
    {
        if (!enableSearchFiltering) return;

        ClearDropdownItems();

        if (string.IsNullOrWhiteSpace(text))
        {
            if (showAllTargetsOnStart)
                ShowAllTargets();
            else
                HideDropdown();
            return;
        }

        // Filter targets based on input
        var matches = allTargets.Where(t =>
            t.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

        if (matches.Count == 0)
        {
            HideDropdown();
            return;
        }

        ShowDropdownItems(matches);
    }

    private void ToggleDropdown()
    {
        if (isDropdownOpen)
        {
            HideDropdown();
        }
        else
        {
            ShowAllTargets();
        }
    }

    private void ShowAllTargets()
    {
        ShowDropdownItems(allTargets);
    }

  private void ShowDropdownItems(List<string> items)
  {
      ClearDropdownItems();

      if (dropdownPanel == null || dropdownContainer == null) return;

      // Show dropdown with animation
      ShowDropdownAnimated();
      isDropdownOpen = true;

      foreach (var item in items)
      {
          var dropdownItem = Instantiate(dropdownItemPrefab, dropdownContainer);
          dropdownItem.GetComponentInChildren<TMP_Text>().text = item;
          dropdownItem.GetComponent<Button>().onClick.AddListener(() => OnDestinationSelected(item));
          activeDropdownItems.Add(dropdownItem);
      }

      // Force layout rebuild and scroll to top
      UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(dropdownContainer);

      // If you have ScrollRect, also add this:
      var scrollRect = dropdownPanel.GetComponent<ScrollRect>();
      if (scrollRect != null)
      {
          scrollRect.verticalNormalizedPosition = 1; // 1 = top, 0 = bottom
      }
  }

    private void HideDropdown()
    {
        if (dropdownPanel != null && isDropdownOpen)
        {
            HideDropdownAnimated();
        }
        isDropdownOpen = false;
    }

    private void ClearDropdownItems()
    {
        foreach (var item in activeDropdownItems)
            Destroy(item);
        activeDropdownItems.Clear();
    }

    private void OnDestinationSelected(string name)
    {
        searchInputField.text = name;
        HideDropdownAnimated(); // Use animated hide
        AppFlowController.Instance.OnDestinationSelected(name);

        Debug.Log($"🎯 Destination selected: {name}");
    }


    private void OnSearchSubmitted(string text)
    {
        // If dropdown is open and there are items, select the first one
        if (isDropdownOpen && activeDropdownItems.Count > 0)
        {
            var firstItem = activeDropdownItems[0];
            var targetName = firstItem.GetComponentInChildren<TMP_Text>().text;
            OnDestinationSelected(targetName);
            return;
        }

        // Otherwise, try to use the typed text directly
        if (!string.IsNullOrWhiteSpace(text))
        {
            MoveToTarget(text);
        }
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

    public void ClearSelection()
    {
        // Clear the input field text
        searchInputField.text = "";

        // Hide dropdown and clear items
        HideDropdown();
        ClearDropdownItems();

        // Remove focus from input field
        searchInputField.DeactivateInputField();
    }

    // Public method to close dropdown when clicking outside
    public void CloseDropdown()
    {
        if (isDropdownOpen)
        {
            HideDropdown();
        }
    }
}

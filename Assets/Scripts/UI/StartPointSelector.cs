using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StartPointSelector : MonoBehaviour
{
    [Header("UI Components")]
    public TMP_Dropdown dropdown;

    [Header("References")]
    public AppFlowController2D appFlow;

    private List<AnchorManager.AnchorData> entranceAnchors = new List<AnchorManager.AnchorData>();
    private AnchorManager.AnchorData selectedAnchor;

    void Start()
    {
        LoadEntrancePoints();
        SetupDropdown();
    }

    void LoadEntrancePoints()
    {
        entranceAnchors.Clear();

        if (AnchorManager.Instance != null)
        {
            foreach (var anchor in AnchorManager.Instance.Anchors)
            {
                if (anchor.Type == "Entrance" || anchor.Type == "entrance")
                {
                    entranceAnchors.Add(anchor);
                }
            }
        }

        Debug.Log($"[StartPoint] Loaded {entranceAnchors.Count} entrance points");
    }

    void SetupDropdown()
    {
        if (dropdown == null) return;

        dropdown.ClearOptions();

        List<string> options = new List<string> { "Select your location..." };

        foreach (var anchor in entranceAnchors)
        {
            string displayName = !string.IsNullOrEmpty(anchor.Meta) ? anchor.Meta : anchor.AnchorId;
            options.Add(displayName);
        }

        dropdown.AddOptions(options);
        dropdown.onValueChanged.AddListener(OnDropdownChanged);
    }

    void OnDropdownChanged(int index)
    {
        if (index == 0)
        {
            selectedAnchor = null;
            return;
        }

        int anchorIndex = index - 1;
        if (anchorIndex >= 0 && anchorIndex < entranceAnchors.Count)
        {
            selectedAnchor = entranceAnchors[anchorIndex];
            Debug.Log($"[StartPoint] Selected: {selectedAnchor.AnchorId}");
            
            // Automatically notify app flow as soon as selection is made
            if (appFlow != null)
            {
                appFlow.OnStartPointSelected(selectedAnchor.AnchorId, selectedAnchor.PositionVector, selectedAnchor.Floor);
            }
        }
    }

    public void ClearSelection()
    {
        selectedAnchor = null;
        if (dropdown != null)
            dropdown.value = 0;
    }

    public AnchorManager.AnchorData GetSelectedAnchor() => selectedAnchor;
}

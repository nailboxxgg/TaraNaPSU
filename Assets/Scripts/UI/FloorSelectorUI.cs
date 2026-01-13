using UnityEngine;
using UnityEngine.UI;

public class FloorSelectorUI : MonoBehaviour
{
    [Header("Floor Buttons")]
    public Button[] floorButtons;
    public Color activeColor = new Color(0.2f, 0.6f, 1f);
    public Color inactiveColor = Color.white;

    [Header("References")]
    public Map2DController mapController;

    private int currentFloor = 0;

    void Start()
    {
        SetupButtons();
        UpdateButtonStates();
    }

    void SetupButtons()
    {
        for (int i = 0; i < floorButtons.Length; i++)
        {
            int floor = i;
            if (floorButtons[i] != null)
            {
                floorButtons[i].onClick.AddListener(() => SelectFloor(floor));
            }
        }
    }

    public void SelectFloor(int floor)
    {
        currentFloor = floor;

        if (mapController != null)
            mapController.ShowFloor(floor);

        UpdateButtonStates();
        Debug.Log($"[FloorUI] Selected floor {floor}");
    }

    void UpdateButtonStates()
    {
        for (int i = 0; i < floorButtons.Length; i++)
        {
            if (floorButtons[i] != null)
            {
                ColorBlock colors = floorButtons[i].colors;
                colors.normalColor = (i == currentFloor) ? activeColor : inactiveColor;
                floorButtons[i].colors = colors;

                Image img = floorButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = (i == currentFloor) ? activeColor : inactiveColor;
            }
        }
    }

    public int CurrentFloor => currentFloor;
}

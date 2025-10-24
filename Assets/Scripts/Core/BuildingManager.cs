using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages building information and provides helper methods for multi-building campus
/// Optional helper script for better organization
/// </summary>
public class BuildingManager : MonoBehaviour {

    [System.Serializable]
    public class BuildingInfo {
        public string buildingName = "Building 1";
        public int groundFloorNumber = 0;
        public int firstFloorNumber = 1;
        public Transform buildingRootTransform;
        public Vector3 buildingEntrancePosition;
        
        [TextArea(2, 4)]
        public string buildingDescription = "Main Academic Building";
    }

    [Header("Building Configuration")]
    public List<BuildingInfo> buildings = new List<BuildingInfo>();

    [Header("References")]
    public NavigationController navigationController;
    public TargetHandler targetHandler;

    [Header("UI (Optional)")]
    public TMP_Dropdown buildingSelector;
    public TMP_Dropdown floorSelector;
    public TextMeshProUGUI currentLocationText;

    private void Start() {
        // Initialize with default 3 buildings
        if (buildings.Count == 0) {
            InitializeDefaultBuildings();
        }

        // Setup UI dropdowns if assigned
        if (buildingSelector != null) {
            PopulateBuildingSelector();
        }

        // Update current location display
        UpdateLocationDisplay();
    }

    /// <summary>
    /// Initialize default 3 buildings with 2 floors each
    /// </summary>
    private void InitializeDefaultBuildings() {
        buildings.Add(new BuildingInfo {
            buildingName = "Building 1",
            groundFloorNumber = 0,
            firstFloorNumber = 1,
            buildingDescription = "Main Academic Building"
        });

        buildings.Add(new BuildingInfo {
            buildingName = "Building 2",
            groundFloorNumber = 2,
            firstFloorNumber = 3,
            buildingDescription = "Science and Technology Building"
        });

        buildings.Add(new BuildingInfo {
            buildingName = "Building 3",
            groundFloorNumber = 4,
            firstFloorNumber = 5,
            buildingDescription = "Administrative Building"
        });
    }

    /// <summary>
    /// Get building info by building number (1-3)
    /// </summary>
    public BuildingInfo GetBuildingByNumber(int buildingNumber) {
        if (buildingNumber >= 1 && buildingNumber <= buildings.Count) {
            return buildings[buildingNumber - 1];
        }
        return null;
    }

    /// <summary>
    /// Get building info by floor number (0-5)
    /// </summary>
    public BuildingInfo GetBuildingByFloor(int floorNumber) {
        foreach (BuildingInfo building in buildings) {
            if (floorNumber == building.groundFloorNumber || 
                floorNumber == building.firstFloorNumber) {
                return building;
            }
        }
        return null;
    }

    /// <summary>
    /// Get all targets in a specific building
    /// </summary>
    public List<Target> GetTargetsInBuilding(int buildingNumber) {
        BuildingInfo building = GetBuildingByNumber(buildingNumber);
        if (building == null || targetHandler == null) {
            return new List<Target>();
        }

        List<Target> buildingTargets = new List<Target>();
        buildingTargets.AddRange(targetHandler.GetTargetsOnFloor(building.groundFloorNumber));
        buildingTargets.AddRange(targetHandler.GetTargetsOnFloor(building.firstFloorNumber));

        return buildingTargets;
    }

    /// <summary>
    /// Check if two floors are in the same building
    /// </summary>
    public bool AreSameBuilding(int floor1, int floor2) {
        BuildingInfo building1 = GetBuildingByFloor(floor1);
        BuildingInfo building2 = GetBuildingByFloor(floor2);
        
        return building1 != null && building2 != null && building1 == building2;
    }

    /// <summary>
    /// Get distance between two buildings (approximate, based on entrance positions)
    /// </summary>
    public float GetDistanceBetweenBuildings(int building1, int building2) {
        BuildingInfo b1 = GetBuildingByNumber(building1);
        BuildingInfo b2 = GetBuildingByNumber(building2);

        if (b1 == null || b2 == null) return 0f;

        return Vector3.Distance(b1.buildingEntrancePosition, b2.buildingEntrancePosition);
    }

    /// <summary>
    /// Populate building selector dropdown
    /// </summary>
    private void PopulateBuildingSelector() {
        if (buildingSelector == null) return;

        buildingSelector.ClearOptions();
        List<string> options = new List<string>();

        foreach (BuildingInfo building in buildings) {
            options.Add(building.buildingName);
        }

        buildingSelector.AddOptions(options);
        buildingSelector.onValueChanged.AddListener(OnBuildingSelected);
    }

    /// <summary>
    /// Populate floor selector dropdown based on selected building
    /// </summary>
    private void PopulateFloorSelector(int buildingNumber) {
        if (floorSelector == null) return;

        BuildingInfo building = GetBuildingByNumber(buildingNumber);
        if (building == null) return;

        floorSelector.ClearOptions();
        List<string> options = new List<string> {
            "Ground Floor",
            "First Floor"
        };

        floorSelector.AddOptions(options);
        floorSelector.onValueChanged.AddListener(OnFloorSelected);
    }

    /// <summary>
    /// Called when user selects a building from dropdown
    /// </summary>
    private void OnBuildingSelected(int index) {
        int buildingNumber = index + 1;
        PopulateFloorSelector(buildingNumber);
        
        Debug.Log($"Building {buildingNumber} selected");
    }

    /// <summary>
    /// Called when user selects a floor from dropdown
    /// </summary>
    private void OnFloorSelected(int index) {
        if (buildingSelector == null || navigationController == null) return;

        int buildingNumber = buildingSelector.value + 1;
        BuildingInfo building = GetBuildingByNumber(buildingNumber);
        
        if (building == null) return;

        int selectedFloor = (index == 0) ? building.groundFloorNumber : building.firstFloorNumber;
        
        // Only change floor if it makes sense (same building check)
        int currentBuilding = navigationController.GetBuildingNumber(navigationController.currentFloor);
        
        if (currentBuilding == buildingNumber) {
            navigationController.ChangeFloor(selectedFloor);
        } else {
            Debug.LogWarning($"Cannot switch to Building {buildingNumber} floor. You are in Building {currentBuilding}.");
        }
    }

    /// <summary>
    /// Update current location text display
    /// </summary>
    public void UpdateLocationDisplay() {
        if (currentLocationText == null || navigationController == null) return;

        int currentBuilding = navigationController.GetBuildingNumber(navigationController.currentFloor);
        BuildingInfo building = GetBuildingByNumber(currentBuilding);

        if (building != null) {
            int floorInBuilding = navigationController.GetFloorInBuilding(navigationController.currentFloor);
            string floorName = floorInBuilding == 0 ? "Ground Floor" : "First Floor";
            currentLocationText.text = $"{building.buildingName}\n{floorName}";
        }
    }

    /// <summary>
    /// Get formatted building and floor info
    /// </summary>
    public string GetLocationString(int floorNumber) {
        int buildingNum = navigationController != null ? 
            navigationController.GetBuildingNumber(floorNumber) : (floorNumber / 2) + 1;
        
        BuildingInfo building = GetBuildingByNumber(buildingNum);
        if (building == null) return "Unknown Location";

        int floorInBuilding = floorNumber % 2;
        string floorName = floorInBuilding == 0 ? "Ground Floor" : "First Floor";
        
        return $"{building.buildingName} - {floorName}";
    }

    /// <summary>
    /// Show info about cross-building navigation
    /// </summary>
    public void ShowCrossBuildingInfo(int currentFloor, int targetFloor) {
        int currentBuilding = navigationController.GetBuildingNumber(currentFloor);
        int targetBuilding = navigationController.GetBuildingNumber(targetFloor);

        if (currentBuilding == targetBuilding) return;

        BuildingInfo currentBldg = GetBuildingByNumber(currentBuilding);
        BuildingInfo targetBldg = GetBuildingByNumber(targetBuilding);

        if (currentBldg != null && targetBldg != null) {
            float distance = GetDistanceBetweenBuildings(currentBuilding, targetBuilding);
            
            string message = $"Target is in {targetBldg.buildingName}.\n" +
                           $"You are currently in {currentBldg.buildingName}.\n" +
                           $"Approximate distance: {distance:F0}m\n\n" +
                           $"Please navigate to {targetBldg.buildingName} and scan QR code at entrance.";
            
            if (navigationController != null) {
                navigationController.OnNavigationError?.Invoke(message);
            }
        }
    }

    /// <summary>
    /// Get floor number from building and floor in building
    /// </summary>
    public int GetGlobalFloor(int buildingNumber, int floorInBuilding) {
        BuildingInfo building = GetBuildingByNumber(buildingNumber);
        if (building == null) return -1;

        return floorInBuilding == 0 ? building.groundFloorNumber : building.firstFloorNumber;
    }

    /// <summary>
    /// Debug: Print all building info
    /// </summary>
    [ContextMenu("Print Building Info")]
    public void PrintBuildingInfo() {
        Debug.Log("=== Campus Building Information ===");
        
        for (int i = 0; i < buildings.Count; i++) {
            BuildingInfo b = buildings[i];
            Debug.Log($"\n{b.buildingName}:");
            Debug.Log($"  Ground Floor: {b.groundFloorNumber}");
            Debug.Log($"  First Floor: {b.firstFloorNumber}");
            Debug.Log($"  Description: {b.buildingDescription}");
            
            if (targetHandler != null) {
                List<Target> targets = GetTargetsInBuilding(i + 1);
                Debug.Log($"  Total Targets: {targets.Count}");
            }
        }
    }

    private void Update() {
        // Continuously update location display
        UpdateLocationDisplay();
    }
}
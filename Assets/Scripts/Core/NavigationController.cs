using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using System.Collections.Generic;

public class NavigationController : MonoBehaviour {

    [Header("Navigation Settings")]
    [Tooltip("Distance threshold to consider target reached (in meters)")]
    public float arrivalThreshold = 1.5f;
    
    [Tooltip("Average walking speed in meters per second")]
    public float averageWalkingSpeed = 1.4f;

    [Header("Multi-Building Floor Management")]
    [Tooltip("Current floor user is on (0-5 for 3 buildings x 2 floors)")]
    public int currentFloor = 0;
    
    [Tooltip("Assign all floor navigation areas in order: B1-Ground, B1-First, B2-Ground, B2-First, B3-Ground, B3-First")]
    public Transform[] floorNavigationAreas = new Transform[6];

    [Header("Events")]
    public UnityEvent<string> OnNavigationError;
    public UnityEvent<float, float> OnNavigationUpdate; // distance, eta
    public UnityEvent OnTargetReached;
    public UnityEvent<int> OnFloorChanged;
    public UnityEvent<int, string> OnBuildingChanged; // building number, building name

    // Public Properties
    public Vector3 TargetPosition { get; set; } = Vector3.zero;
    public int TargetFloor { get; set; } = 0;
    public NavMeshPath CalculatedPath { get; private set; }
    public float DistanceToTarget { get; private set; }
    public float EstimatedTimeToArrival { get; private set; }
    public bool IsNavigating { get; private set; }
    public bool HasValidPath { get; private set; }
    public string CurrentTargetName { get; set; }

    // Private variables
    private bool targetReached = false;
    private float lastPathUpdateTime;
    private const float PATH_UPDATE_INTERVAL = 0.5f;

    // Building and floor helpers
    private int currentBuildingNumber = 1;
    private int currentFloorInBuilding = 0; // 0 = ground, 1 = first

    private void Start() {
        CalculatedPath = new NavMeshPath();
        IsNavigating = false;
        HasValidPath = false;
        
        // Initialize events if null
        if (OnNavigationError == null) OnNavigationError = new UnityEvent<string>();
        if (OnNavigationUpdate == null) OnNavigationUpdate = new UnityEvent<float, float>();
        if (OnTargetReached == null) OnTargetReached = new UnityEvent();
        if (OnFloorChanged == null) OnFloorChanged = new UnityEvent<int>();
        if (OnBuildingChanged == null) OnBuildingChanged = new UnityEvent<int, string>();

        // Calculate initial building and floor
        UpdateBuildingAndFloorInfo(currentFloor);
    }

    private void Update() {
        if (!IsNavigating || TargetPosition == Vector3.zero) {
            return;
        }

        // Check if we need to handle floor/building transition
        if (currentFloor != TargetFloor) {
            HandleFloorTransition();
            return;
        }

        // Update path periodically
        if (Time.time - lastPathUpdateTime > PATH_UPDATE_INTERVAL) {
            UpdateNavigation();
            lastPathUpdateTime = Time.time;
        }

        // Check if target is reached
        CheckArrival();
    }

    /// <summary>
    /// Start navigation to a target position
    /// </summary>
    public void StartNavigation(Vector3 targetPos, int targetFloor, string targetName = "") {
        TargetPosition = targetPos;
        TargetFloor = targetFloor;
        CurrentTargetName = targetName;
        IsNavigating = true;
        targetReached = false;
        HasValidPath = false;

        // Calculate target building info
        int targetBuilding = GetBuildingNumber(targetFloor);
        int targetFloorInBuilding = GetFloorInBuilding(targetFloor);
        
        Debug.Log($"Navigation started to {targetName} on floor {targetFloor} (Building {targetBuilding}, Floor {targetFloorInBuilding})");

        // Check if target is in different building or floor
        if (currentFloor != TargetFloor) {
            if (GetBuildingNumber(currentFloor) != targetBuilding) {
                OnNavigationError?.Invoke($"Target is in Building {targetBuilding}. Please navigate there first.");
                Debug.Log($"Cross-building navigation: Current Building {currentBuildingNumber} → Target Building {targetBuilding}");
            }
            HandleFloorTransition();
        } else {
            UpdateNavigation();
        }
    }

    /// <summary>
    /// Stop current navigation
    /// </summary>
    public void StopNavigation() {
        IsNavigating = false;
        TargetPosition = Vector3.zero;
        HasValidPath = false;
        DistanceToTarget = 0;
        EstimatedTimeToArrival = 0;
        targetReached = false;
        
        if (CalculatedPath != null) {
            CalculatedPath.ClearCorners();
        }
        
        Debug.Log("Navigation stopped");
    }

    /// <summary>
    /// Update navigation path and metrics
    /// </summary>
    private void UpdateNavigation() {
        bool pathFound = NavMesh.CalculatePath(transform.position, TargetPosition, NavMesh.AllAreas, CalculatedPath);

        if (!pathFound || CalculatedPath.status != NavMeshPathStatus.PathComplete) {
            HasValidPath = false;
            OnNavigationError?.Invoke($"Cannot find path to {CurrentTargetName}. Target may be unreachable.");
            Debug.LogWarning($"Path calculation failed. Status: {CalculatedPath.status}");
            return;
        }

        HasValidPath = true;
        DistanceToTarget = CalculatePathDistance();

        if (averageWalkingSpeed > 0) {
            EstimatedTimeToArrival = DistanceToTarget / averageWalkingSpeed;
        }

        OnNavigationUpdate?.Invoke(DistanceToTarget, EstimatedTimeToArrival);
    }

    /// <summary>
    /// Calculate total distance along the path
    /// </summary>
    private float CalculatePathDistance() {
        if (CalculatedPath == null || CalculatedPath.corners.Length < 2) {
            return 0f;
        }

        float totalDistance = 0f;
        for (int i = 0; i < CalculatedPath.corners.Length - 1; i++) {
            totalDistance += Vector3.Distance(CalculatedPath.corners[i], CalculatedPath.corners[i + 1]);
        }

        return totalDistance;
    }

    /// <summary>
    /// Check if user has arrived at target
    /// </summary>
    private void CheckArrival() {
        float distanceToFinalTarget = Vector3.Distance(transform.position, TargetPosition);

        if (distanceToFinalTarget <= arrivalThreshold && !targetReached) {
            targetReached = true;
            Debug.Log($"Target reached: {CurrentTargetName}");
            OnTargetReached?.Invoke();
        }
    }

    /// <summary>
    /// Handle navigation when target is on different floor or building
    /// </summary>
    private void HandleFloorTransition() {
        int targetBuilding = GetBuildingNumber(TargetFloor);
        int currentBuilding = GetBuildingNumber(currentFloor);

        // Check if cross-building navigation
        if (currentBuilding != targetBuilding) {
            OnNavigationError?.Invoke(
                $"Target is in Building {targetBuilding}. You are in Building {currentBuilding}. " +
                $"Navigate to Building {targetBuilding} first, then scan QR code to recenter."
            );
            return;
        }

        // Same building, different floor - find stairs
        Vector3 transitionPoint = FindNearestTransitionPoint();

        if (transitionPoint == Vector3.zero) {
            OnNavigationError?.Invoke("No stairs found to reach target floor. Please use QR code to change floors.");
            Debug.LogError("No transition point found between floors!");
            return;
        }

        Debug.Log($"Navigating to stairs at {transitionPoint}");
        
        bool pathFound = NavMesh.CalculatePath(transform.position, transitionPoint, NavMesh.AllAreas, CalculatedPath);

        if (pathFound && CalculatedPath.status == NavMeshPathStatus.PathComplete) {
            HasValidPath = true;
            DistanceToTarget = CalculatePathDistance();
            EstimatedTimeToArrival = DistanceToTarget / averageWalkingSpeed;
            OnNavigationUpdate?.Invoke(DistanceToTarget, EstimatedTimeToArrival);
        } else {
            OnNavigationError?.Invoke("Cannot find path to stairs.");
        }
    }

    /// <summary>
    /// Find the nearest transition point (stairs) between floors
    /// </summary>
    private Vector3 FindNearestTransitionPoint() {
        GameObject[] stairs = GameObject.FindGameObjectsWithTag("Stairs");
        
        if (stairs.Length == 0) {
            Debug.LogWarning("No stairs found! Make sure stair objects are tagged with 'Stairs'");
            return Vector3.zero;
        }

        Vector3 nearestPoint = Vector3.zero;
        float nearestDistance = Mathf.Infinity;

        foreach (GameObject stair in stairs) {
            StairwayMarker marker = stair.GetComponent<StairwayMarker>();
            if (marker != null && marker.ConnectsFloors(currentFloor, TargetFloor)) {
                float distance = Vector3.Distance(transform.position, stair.transform.position);
                if (distance < nearestDistance) {
                    nearestDistance = distance;
                    nearestPoint = stair.transform.position;
                }
            }
        }

        return nearestPoint;
    }

    /// <summary>
    /// Change current floor (called when user scans QR or manually selects floor)
    /// </summary>
    public void ChangeFloor(int newFloor) {
        if (newFloor == currentFloor || newFloor < 0 || newFloor > 5) {
            return;
        }

        Debug.Log($"Floor changed from {currentFloor} to {newFloor}");
        
        int oldBuilding = GetBuildingNumber(currentFloor);
        int newBuilding = GetBuildingNumber(newFloor);
        
        currentFloor = newFloor;
        UpdateBuildingAndFloorInfo(newFloor);
        
        OnFloorChanged?.Invoke(newFloor);
        
        // Check if building changed
        if (oldBuilding != newBuilding) {
            string buildingName = GetBuildingName(newBuilding);
            OnBuildingChanged?.Invoke(newBuilding, buildingName);
        }

        // If navigating and now on target floor, recalculate path
        if (IsNavigating && currentFloor == TargetFloor) {
            UpdateNavigation();
        }
    }

    /// <summary>
    /// Update building and floor information based on current floor number
    /// </summary>
    private void UpdateBuildingAndFloorInfo(int floorNumber) {
        currentBuildingNumber = GetBuildingNumber(floorNumber);
        currentFloorInBuilding = GetFloorInBuilding(floorNumber);
    }

    /// <summary>
    /// Get building number (1-3) from floor number (0-5)
    /// </summary>
    public int GetBuildingNumber(int floorNumber) {
        return (floorNumber / 2) + 1;
    }

    /// <summary>
    /// Get floor within building (0=ground, 1=first) from global floor number
    /// </summary>
    public int GetFloorInBuilding(int floorNumber) {
        return floorNumber % 2;
    }

    /// <summary>
    /// Get building name from building number
    /// </summary>
    public string GetBuildingName(int buildingNumber) {
        switch (buildingNumber) {
            case 1: return "Building 1";
            case 2: return "Building 2";
            case 3: return "Building 3";
            default: return "Unknown Building";
        }
    }

    /// <summary>
    /// Get floor name (Ground/First) from floor in building
    /// </summary>
    public string GetFloorName(int floorInBuilding) {
        return floorInBuilding == 0 ? "Ground Floor" : "First Floor";
    }

    /// <summary>
    /// Get full location string (e.g., "Building 2 - First Floor")
    /// </summary>
    public string GetFullLocationString() {
        return $"{GetBuildingName(currentBuildingNumber)} - {GetFloorName(currentFloorInBuilding)}";
    }

    /// <summary>
    /// Get formatted distance string
    /// </summary>
    public string GetDistanceString() {
        if (DistanceToTarget < 1f) {
            return $"{(DistanceToTarget * 100f):F0} cm";
        } else {
            return $"{DistanceToTarget:F1} m";
        }
    }

    /// <summary>
    /// Get formatted ETA string
    /// </summary>
    public string GetETAString() {
        if (EstimatedTimeToArrival < 60f) {
            return $"{EstimatedTimeToArrival:F0} sec";
        } else {
            int minutes = Mathf.FloorToInt(EstimatedTimeToArrival / 60f);
            int seconds = Mathf.FloorToInt(EstimatedTimeToArrival % 60f);
            return $"{minutes} min {seconds} sec";
        }
    }

    private void OnDrawGizmos() {
        if (CalculatedPath != null && CalculatedPath.corners.Length > 0) {
            Gizmos.color = Color.green;
            for (int i = 0; i < CalculatedPath.corners.Length - 1; i++) {
                Gizmos.DrawLine(CalculatedPath.corners[i], CalculatedPath.corners[i + 1]);
                Gizmos.DrawSphere(CalculatedPath.corners[i], 0.1f);
            }
        }
    }
}
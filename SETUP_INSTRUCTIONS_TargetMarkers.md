# Target Marker Visibility System Setup

## Overview
This system ensures that when a user selects a destination, only that specific target marker is visible while all other target markers are hidden.

## How It Works
1. **TargetMarkerManager** manages visibility of all target markers in your scene
2. **NavigationController** integrates with this manager to show only the selected destination
3. All other markers are automatically hidden during navigation

## Setup Instructions

### Option 1: Using Pre-placed Target Markers (Recommended)
If you already have target markers in your Unity scene:

1. **Ensure your target markers are named correctly**:
   - `TargetPin-<TargetName>` (e.g., `TargetPin-Registrar Office`)
   - `Target-<TargetName>`
   - `<TargetName>-Marker`
   - Or just `<TargetName>`

2. **Add TargetMarkerManager to your scene**:
   - Create an empty GameObject named "TargetMarkerManager"
   - Attach the `TargetMarkerManager.cs` script
   - Set `createMarkersDynamically` to **false**
   - Assign a parent transform for organization (optional)

3. **Configure NavigationController**:
   - The NavigationController will automatically use TargetMarkerManager
   - No additional configuration needed

### Option 2: Dynamic Marker Creation
If you want markers created automatically:

1. **Create a target marker prefab**:
   - Design a visual marker (pin, sphere, etc.)
   - Add it to your project as a prefab

2. **Add TargetMarkerManager to your scene**:
   - Create an empty GameObject named "TargetMarkerManager"
   - Attach the `TargetMarkerManager.cs` script
   - Set `createMarkersDynamically` to **true**
   - Assign your marker prefab to `targetMarkerPrefab`
   - Create an empty GameObject "TargetMarkers" and assign it to `markersParent`

3. **Configure NavigationController**:
   - The system will automatically create markers for all targets in TargetData.json

## Target Naming Conventions
Your target markers should match the names in `TargetData.json`. Examples:
- Target name: "Registrar Office" → Marker name: "TargetPin-Registrar Office"
- Target name: "Library" → Marker name: "TargetPin-Library"
- Target name: "Room 201" → Marker name: "TargetPin-Room 201"

## Verification
To test the system:

1. Run your scene
2. Select a destination from the search bar
3. Scan a QR code to start navigation
4. **Only the selected destination marker should be visible**
5. All other target markers should be hidden
6. When navigation stops, all markers should be hidden

## Troubleshooting

### Markers not hiding/showing correctly:
- Check that TargetMarkerManager.Instance is not null
- Verify marker names match the target names in TargetData.json
- Ensure `showDebugLogs` is enabled to see debug messages

### No markers found:
- If using pre-placed markers, check naming conventions
- If using dynamic creation, ensure targetMarkerPrefab is assigned
- Verify TargetManager.Instance has loaded the target data

### Performance issues:
- The system only manages active/inactive states, no performance impact
- Markers are simply disabled/enabled, not destroyed/created during navigation

## Integration Notes
- The system is backward compatible with your existing NavigationController
- Fallback behavior: If TargetMarkerManager is not available, the original marker creation logic is used
- Works with both AR and non-AR navigation modes
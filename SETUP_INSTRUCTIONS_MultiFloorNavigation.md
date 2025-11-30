# Multi-Floor Navigation Setup Instructions

## Overview
The enhanced navigation system now supports intelligent floor-to-floor routing using stairways. This system automatically detects when navigation requires multiple floors and guides users through the appropriate stairways.

## Key Components Added

### 1. MultiFloorNavigationManager.cs
- **Purpose**: Handles multi-floor routing and stairway navigation
- **Features**:
  - Automatic detection of multi-floor navigation needs
  - Intelligent stairway selection (nearest stairway)
  - Segment-based navigation (user → stairway → destination)
  - Stairway transition handling

### 2. Enhanced NavigationController.cs
- **Purpose**: Extended to integrate with multi-floor navigation
- **Features**:
  - Automatic multi-floor detection
  - Integration with MultiFloorNavigationManager
  - Segment-based navigation support

## Setup Instructions

### Step 1: Add MultiFloorNavigationManager to Scene
1. Create an empty GameObject named "MultiFloorNavigationManager"
2. Add the `MultiFloorNavigationManager` component
3. Configure the references:
   - `Navigation Controller`: Drag your NavigationController instance
   - `Anchor Manager`: Should auto-find AnchorManager.Instance
   - `Target Manager`: Should auto-find TargetManager.Instance

### Step 2: Update NavigationController
1. Select your NavigationController GameObject
2. In the Inspector, find the new `Multi Floor Manager` field
3. Drag the MultiFloorNavigationManager GameObject to this field

### Step 3: Configure Stairway Data
Your `AnchorData.json` already contains stairway data. The system uses:
- **Type**: "stair" for stairway anchors
- **StairPair system**: Automatically pairs stairways between floors
- **Naming convention**: `{BuildingId}-Stair{Number}-{Up/Down}`

Example stairway entries:
```json
{
  "Type": "stair",
  "AnchorId": "B1-Stair1-Down",
  "BuildingId": "B1",
  "Floor": 0,
  "Position": { "x": 9, "y": -1, "z": -2.35 },
  "Rotation": { "x": 0, "y": 0, "z": 0 },
  "Meta": "Stairway 1 (Ground)"
},
{
  "Type": "stair",
  "AnchorId": "B1-Stair1-Up",
  "BuildingId": "B1",
  "Floor": 1,
  "Position": { "x": 9, "y": -1, "z": -2.35 },
  "Rotation": { "x": 0, "y": 0, "z": 0 },
  "Meta": "Stairway 1 (First Floor)"
}
```

### Step 4: Enhance Target Data (Optional)
For better building detection, consider adding building information to your target names or adding a BuildingId field to TargetData.

Current naming convention detection:
- Target names containing "b1" → Building B1
- Target names containing "b2" → Building B2
- Target names containing "b3" → Building B3

## How It Works

### Multi-Floor Detection
The system automatically detects multi-floor navigation when:
- User floor ≠ Target floor
- User building ≠ Target building

### Navigation Flow
1. **Single Floor**: Direct navigation (existing behavior)
2. **Multi Floor**:
   - Route to nearest stairway on current floor
   - Guide user to take stairs
   - Continue navigation to destination on target floor

### Stairway Selection
- Finds all stairways connecting the required floors
- Selects the nearest stairway to user's current position
- Uses AnchorManager's `FindNearestStair()` method

## Testing the System

### Test Scenario 1: Same Floor Navigation
1. Scan QR code on Floor 0_B1
2. Select target on Floor 0_B1
3. **Expected**: Direct navigation (existing behavior)

### Test Scenario 2: Different Floor Navigation
1. Scan QR code on Floor 0_B1
2. Select target on Floor 1_B1
3. **Expected**:
   - Navigate to nearest stairway
   - Prompt to take stairs
   - Navigate to destination on Floor 1

### Test Scenario 3: Different Building Navigation
1. Scan QR code on Floor 0_B1
2. Select target in B2
3. **Expected**: Multi-floor navigation with building transition

## Configuration Options

### MultiFloorNavigationManager Settings
- **Stairway Arrival Distance**: Distance to consider "arrived" at stairway (default: 2.0m)
- **Show Stairway Prompts**: Enable/disable stairway UI prompts (default: true)

### NavigationController Integration
- **Multi Floor Manager**: Reference to MultiFloorNavigationManager instance
- **Arrival Distance**: Distance for arrival detection (shared with single-floor)

## Debug Information

The system provides detailed debug logs:
- Multi-floor navigation detection
- Route calculation details
- Stairway selection
- Segment progression
- Arrival handling

Check the Console for:
```
[MultiFloorNavigationManager] Starting multi-floor navigation with X segments
[MultiFloorNavigationManager] Executing segment 1/X: TargetName
[NavigationController] Multi-floor navigation needed from B1 floor 0 to TargetName
```

## Troubleshooting

### Issue: "Multi-floor navigation not detected"
- Check target naming convention (contains B1/B2/B3)
- Verify AnchorManager has stairway data
- Ensure TargetManager has target data

### Issue: "No stairway found"
- Verify stairway entries in AnchorData.json
- Check stairway naming convention
- Ensure stairway pairs exist for required floors

### Issue: "Navigation doesn't switch floors"
- Verify NavMeshSurfaces are properly configured
- Check `SwitchToNavMeshFor()` method
- Ensure NavMesh assets exist for all floors

## Future Enhancements

### Potential Improvements
1. **Visual Stairway Indicators**: Show stairway locations in AR
2. **Elevator Support**: Add elevator routing alongside stairs
3. **Progress Indicators**: Show multi-floor navigation progress
4. **Voice Prompts**: Audio instructions for stairway transitions
5. **Optimized Routing**: Consider multiple stairway options

### Data Structure Enhancements
1. **TargetData BuildingId**: Add explicit building information
2. **Stairway Types**: Differentiate between stairs, elevators, ramps
3. **Navigation Costs**: Weight different connection types

## Files Modified/Created

### New Files
- `Assets/Scripts/Core/MultiFloorNavigationManager.cs`
- `SETUP_INSTRUCTIONS_MultiFloorNavigation.md`

### Modified Files
- `Assets/Scripts/Controllers/NavigationController.cs` (enhanced with multi-floor support)

### Existing Dependencies
- `Assets/Scripts/Core/AnchorManager.cs` (stairway data)
- `Assets/Scripts/Core/TargetManager.cs` (target data)
- `Assets/Resources/AnchorData.json` (stairway positions)
- `Assets/Resources/TargetData.json` (target positions)

## Support

For issues or questions:
1. Check Unity Console for debug messages
2. Verify all required components are assigned
3. Ensure stairway and target data are correctly configured
4. Test with simple single-floor navigation first
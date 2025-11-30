# Multi-Floor Navigation Test Guide

## 🎯 **Expected Behavior Now**

### **Test Scenario: B1-North Entrance → B1-ICT-MO (Different Floor)**

1. **User scans QR at B1-North Entrance** (Floor 0)
2. **User selects B1-ICT-MO** (Floor 1)
3. **System should:**
   - ✅ Detect multi-floor navigation needed
   - ✅ Find nearest stairway on Floor 0
   - ✅ Show navigation line/arrow **directly to stairway** on Floor 0
   - ✅ NOT show line going through walls
   - ✅ When user reaches stairway, prompt to take stairs
   - ✅ After stairs, show navigation line/arrow to B1-ICT-MO on Floor 1

## 📋 **Step-by-Step Navigation Flow**

### **Segment 1: Entrance → Stairway**
```
Start: B1-North Entrance (Floor 0)
Navigation Line: Direct path to nearest stairway (B1-Stair1-Down or B1-Stair2-Down)
Arrow: Points toward stairway
Arrival: When user gets within 2m of stairway
```

### **Stairway Transition**
```
Prompt: "Please take stairs to next floor - B1-Stair1-Down"
User Action: User climbs stairs
System: Detects arrival, switches to Floor 1 NavMesh
```

### **Segment 2: Stairway → Destination**
```
Start: Stairway position on Floor 1 (B1-Stair1-Up)
Navigation Line: Path from stairway to B1-ICT-MO
Arrow: Points toward destination
Arrival: When user reaches B1-ICT-MO
```

## 🔍 **Debug Console Messages**

You should see these messages:
```
[NavigationController] Multi-floor navigation needed from B1 floor 0 to B1-ICT-MO
[MultiFloorNavigationManager] Starting multi-floor navigation with 2 segments
[MultiFloorNavigationManager] Executing segment 1/2: B1-Stair1-Down
[MultiFloorNavigationManager] Navigating to stairway: B1-Stair1-Down
[MultiFloorNavigationManager] Navigation started to B1-Stair1-Down at (9, -1, -2.35)
✅ Arrived at destination!
[MultiFloorNavigationManager] Handling stairway transition: B1-Stair1-Down
[MultiFloorNavigationManager] Executing segment 2/2: B1-ICT-MO
[MultiFloorNavigationManager] Navigation completed!
```

## 🚨 **If Line Still Clips Through Walls**

Check these things:

### **1. NavMesh Surface Not Switching**
- Verify only ONE NavMeshSurface is enabled at a time
- Check `SwitchToNavMeshFor()` is working
- Look for: `[NavigationController] Switched NavMesh to B1 floor 0`

### **2. Wrong Target Position**
- Stairway position might be wrong floor
- Check `AnchorData.json` stairway coordinates
- Verify stairway Y-coordinate matches floor

### **3. Path Calculation Issue**
- The line might be calculated on wrong NavMesh
- Check if `GetCurrentNavMeshArea()` returns correct area
- Verify `NavMesh.CalculatePath()` uses correct area mask

### **4. Temporary Object Issue**
- Temporary stairway pin might be on wrong position
- Check `NavigateToPosition()` sets correct coordinates
- Verify `activeTargetPin.transform.position`

## 🛠️ **Quick Fixes to Try**

### **Fix 1: Force NavMesh Switch**
```csharp
// In NavigateToPosition method, add this:
navigationController.SwitchToNavMeshFor(userAnchor.BuildingId, userAnchor.Floor);
yield return new WaitForSeconds(0.2f); // Wait for switch
```

### **Fix 2: Debug Path Calculation**
```csharp
// In NavigationController.Update(), add this debug:
Debug.Log($"Path from {userPos} to {activeTargetPin.transform.position}");
Debug.Log($"Path valid: {navPath.status}");
Debug.Log($"Path corners: {navPath.corners.Length}");
```

### **Fix 3: Verify Stairway Position**
```csharp
// In NavigateToStairway method, add this:
Debug.Log($"Stairway anchor position: {stairAnchor.PositionVector}");
Debug.Log($"User anchor floor: {userAnchor.Floor}");
```

## 📊 **What Should Be Different Now**

### **Before (Problem):**
- Navigation line tried to connect entrance directly to destination
- Line passed through walls because floors weren't connected
- System used NavMeshLinks or failed pathfinding

### **After (Fixed):**
- Navigation line goes entrance → stairway (clear path on Floor 0)
- User takes stairs (manual action)
- Navigation line goes stairway → destination (clear path on Floor 1)
- No wall clipping, proper floor-by-floor guidance

## 🎯 **Key Changes Made**

1. **Segmented Navigation**: Instead of one continuous path, now uses segments
2. **Direct Stairway Targeting**: Navigation line points to actual stairway coordinates
3. **Floor-Specific Pathfinding**: Each segment uses correct floor's NavMesh
4. **Manual Stairway Transition**: User physically climbs stairs, system detects arrival
5. **Proper NavMesh Switching**: Switches to correct floor's NavMesh for each segment

The navigation should now show clear, wall-free paths for each segment!
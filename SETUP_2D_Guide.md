# TaraNaPSU 2D - Unity Scene Setup Guide

This guide explains how to set up the Unity scene for the new 2D top-down map navigation system.

## 1. Camera Setup

1. **Select Main Camera** in Hierarchy
2. Set these properties in Inspector:
   - **Projection**: Orthographic
   - **Size**: 50 (adjust based on campus size)
   - **Position**: (30, 100, 30) - above map center
   - **Rotation**: (90, 0, 0) - looking straight down
   - **Clear Flags**: Solid Color
   - **Background**: Choose a sky/background color

## 2. Remove AR Objects

Delete these objects from the scene (if present):
- AR Session
- AR Session Origin
- XR Origin
- Any AR Camera Rigs

## 3. Create Map Container (Restructured)

To support your two buildings and stairway checkpoints, organize your hierarchy like this:

- **MapContainer**
   - **Floor_0** (Campus Grounds - **Persistent Base**)
     - `CampusGrounds` model & `NavMeshSurface`
   - **Floor_1** (Combined Ground Floor)
     - `Floor0_B1` (Building 1 logic/walls/markers)
     - `Floor0_B2` (Building 2 logic/walls/markers)
     - `GroundFloorStairways` (All stairway checkpoints for Floor 0)
     - `Floor0_B1 Targets` & `Floor0_B2 Targets`
   - **Floor_2** (Combined 1st Floor)
     - `Floor1_B1` (Building 1 first floor/markers)
     - `Floor1_B2` (Building 2 first floor/markers)
     - `Floor1_B1 Targets` & `Floor1_B2 Targets`

**Why this works:**
- **Coordinate Stability:** Since everything is under one `MapContainer`, the 4.8m gap between B1 and B2 stays exactly the same.
- **Layered View:** Turning on `Floor_1` shows the ground floor interiors for **both** buildings at once.
- **Stairway Markers:** Keep these inside their respective floor containers so the QR "Scan" button only shows the checkpoints on the floor the user is currently looking at.

## 4. Add Core Scripts

Create Empty GameObject: **"GameManager"**

Add these components:
- `Map2DController`
- `Navigation2DController` 
- `AppFlowController2D`
- `TargetManager` (keep existing)
- `QRUIController`
- `ZXingScannerController`

### Map2DController Settings:
- **Map Camera**: Drag Main Camera
- **Floor Containers**: Drag Floor_0, Floor_1, etc.
- **Min Zoom**: 20
- **Max Zoom**: 100
- **Map Min Bounds**: (0, 0) - or your bottom-left corner
- **Map Max Bounds**: (40, 50) - or your top-right corner
- **Tip**: Set these so the camera stays within the campus model.

### Navigation2DController Settings:
- **Path Color**: Blue
- **Path Width**: 0.3
- **Path Height**: 0.5

## 5. Create UI Canvas

Create Canvas with these panels:

### Welcome Panel (Action Steps)
1. **Create Panel**: Right-click Canvas -> `UI -> Panel`. Name it **"WelcomePanel"**.
2. **Apply Background**: Drag `welcome_panel_background_v2` into the `Image` component's **Source Image** slot. (Follow Section 10 for stretching fix).
3. **Add Title**: Right-click WelcomePanel -> `UI -> Text - MeshPro`. Set text to "TaraNaPSU".
4. **Setup Start Point**:
   - Right-click WelcomePanel -> `UI -> Dropdown - TextMeshPro`.
   - Add `StartPointSelector` script to this dropdown.
   - Drag **AppFlowController2D** into its `appFlow` slot.
   - (No confirm button needed; it registers automatically on selection).
5. **Setup Search Bar (Hybrid Dropdown)**:
   - **Create Container**: Add an Empty GameObject called **"SearchBar_Group"**.
   - **Add Input**: Inside the group, add a `UI -> Input Field - TextMeshPro`.
   - **Add Toggle Button**: Inside the group, add a `UI -> Button`. Use a "Down Arrow" icon. Name it **"DropdownToggle"**.
   - **Add Suggestion List**:
     - Create a `UI -> Scroll View`. Name it **"SuggestionList"**.
     - Set its **Content** object to have a `Vertical Layout Group` and `Content Size Fitter` (Vertical Fit: Preferred).
     - **Initially Hide**: Disable the "SuggestionList" object.
   - **Attach Script**: Add `LocationSearchBar` to "SearchBar_Group".
   - **Assign References**:
     - **Input Field**: Drag the Input Field child.
     - **Suggestion Container**: Drag the Scroll View's **Content** object.
     - **Toggle Button**: Drag the DropdownToggle button.
     - **App Flow**: Drag the **GameManager**.
6. **Add Welcome Button**:
   - Create a primary **"StartButton"**.
   - Set its **OnClick** event to call `AppFlowController2D.SmartStart`.
   - **How it works**: If the user has NOT picked a start point manually, clicking this will automatically open the QR scanner. If they HAVE picked a start point, it will go straight to the map.

### QR Panel (Action Steps)
1. **Create Panel**: Right-click Canvas -> `UI -> Panel`. Name it **"QRPanel"**.
2. **Add RawImage**: For the camera feed.
   - **Full Screen**: Set **Anchors** to "Stretch-Stretch" (hold Alt + click bottom-right corner in RectTransform) so it fills the entire panel.
3. **Add Aspect Ratio Fitter**: Attach to RawImage.
   - Set **Aspect Mode** to `Envelope Parent`. This ensures the camera feed fills the whole screen without black bars.
4. **Add Status Text**: Right-click QRPanel -> `UI -> Text - MeshPro`. 
   - Position it near the top or center.
   - Set initial text to: "Align the camera to the QR Code".
5. **Add Close Button**: Set OnClick to call `QRUIController.CloseScanner`.
6. **Add ZXingScannerController**: Attach to this panel.
   - **Assign References**: Drag the RawImage, AspectRatioFitter, and the new Status Text into their slots.

### Map Panel (Action Steps)
1. **Create Panel**: Right-click Canvas -> `UI -> Panel`. Name it **"MapPanel"**.
   - **Styling**: (Optional) Use the "Overflow Hack" (Section 10) to round only the top corners.
2. **Setup Floor Selector**:
   - Create a **Vertical Layout Group** container on the right side.
   - Inside it, create 3 Buttons: "Campus", "Ground", "1st Floor".
   - Add `FloorSelectorUI` script to the container.
   - **Assign**: Drag the 3 Buttons into the `floorButtons` array in the Inspector.
3. **Setup Zoom Controls**: 
   - Create two UI Buttons (+ and -). Position them in the bottom-right.
   - Add `ZoomControlsUI` script to them. 
   - **Link**: Drag the **GameManager** into the `mapController` slot.
4. **Setup Status Display (Bottom Sheet)**:
   - Create a Panel at the bottom named **"StatusDisplay"**.
   - Inside, add 4 `UI -> Text - MeshPro` objects: **Status**, **Destination**, **Distance**, **Floor**.
   - Create two sub-panels: **"PromptPanel"** and **"ArrivedPanel"** (both initially hidden).
     - Inside **PromptPanel**, add a text object: **FloorPromptText**.
     - Inside **ArrivedPanel**, add a text object: **ArrivedPromptText**.
   - Add `NavigationStatusDisplay2D` script to "StatusDisplay".
   - **Assign References**: Drag all Text objects (including the 2 inside the sub-panels) into their respective slots.
5. **Add Navigation Buttons**: 
   - Inside the StatusDisplay panel, add two Buttons:
   - **Stop Button**: Set OnClick to call `AppFlowController2D.StopNavigation`.
   - **Re-center Button**: Set OnClick to call `QRUIController.OpenScanner`. (Use a QR/Camera icon for this).

## 6. Create Prefabs

### Player Guide (The Avatar)
1. Create a 3D Character (Capsule/Humanoid) or a 2D Sprite.
2. Add the `PlayerGuideController` script to it.
3. If using 3D: Drag the character model into the **Avatar Model** slot.
4. If you have animations: Drag the Animator into the **Animator** slot (set a "isWalking" boolean parameter).
5. Drag this object into the **AppFlowController2D**'s `playerGuide` slot.

### User Marker Prefab (Static Marker)
1. Create 3D Cylinder or Sprite
2. Color: Blue/Green
3. Scale: (1, 0.1, 1)
4. Save as Prefab

### Destination Marker Prefab  
1. Create 3D Cylinder or Pin sprite
2. Color: Red
3. Scale: (1, 0.1, 1)
4. Save as Prefab

### Suggestion Item Prefab (for search)
1. Create UI Button with Text
2. Save as Prefab

## 7. Wire References

Connect all script references in Inspector:
- AppFlowController2D → Map2DController, Navigation2DController
- Map2DController → Camera, Floor containers, Marker prefabs
- StartPointSelector → AppFlowController2D
- LocationSearchBar → AppFlowController2D
- FloorSelectorUI → Map2DController
- QRUIController → qrPanel, ZXingScannerController

## 8. NavMesh

Keep your existing NavMesh baked floors. The 2D navigation still uses NavMesh for pathfinding - only the camera perspective changes.

## 9. Test

1. Press Play
2. Select a starting location from dropdown
3. Search for and select a destination
4. Verify path is drawn on map
5. Test pan (drag), zoom (scroll), floor switching

## 10. UI Scaling & Aspect Ratio (Fix Stretching)

To ensure your UI looks perfect on a **1920x1080** (or 1080x1920) screen without stretching:

### Canvas Scaler Settings
1. Select your **Canvas** in the Hierarchy.
2. Under **Canvas Scaler**, set:
   - **UI Scale Mode**: `Scale With Screen Size`
   - **Reference Resolution**: `1080 x 1920` (if Portrait) or `1920 x 1080` (if Landscape)
   - **Screen Match Mode**: `Match Width Or Height`
   - **Match**: `0.5`

### Image Component (Background)
1. Select the **Background Image** object.
2. Set **Anchors** to "Stretch-Stretch" (hold Alt + click bottom-right corner in RectTransform).
3. In the **Image** component:
   - Check **Preserve Aspect**: This prevents geometric patterns from becoming ovals.

### Auto-Filling the Screen (Best for Backgrounds)
1. Add an **Aspect Ratio Fitter** component to the Background.
2. Set **Aspect Mode** to `Envelope Parent`.
3. Set **Aspect Ratio** to `1.0` (as the generated images are square).

## 11. In-Depth: Welcome Panel UI Components

Follow these precise technical steps to build the selection UI from scratch:

### A. Start Point Dropdown (Entrance Selection)
1. **The Object**: Right-click WelcomePanel -> `UI -> Dropdown - TextMeshPro`.
2. **The Logic**: The `StartPointSelector` script automatically scans your `AnchorData.json` for all entries tagged as `type: "Entrance"`.
3. **Setup Steps**:
   - Attach the **StartPointSelector** script.
   - Drag the **AppFlowController2D** (GameManager) into the `appFlow` slot.
   - Drag the dropdown's own **TMP_Dropdown** into the `dropdown` slot.
4. **Immediate Action**: This dropdown registers the user's choice the moment they click an item (no "Confirm" button required).

### B. Destination Search Bar (The Hybrid Component)
This component combines a **TMP Input Field** for typing and a **Scroll View** that acts as a dropdown.

1. **Hierarchy Organization**:
   - Create an empty GameObject named **"SearchBar_Group"**.
   - Add **UI -> Input Field - TextMeshPro** as a child. (Name it: *DestinationInput*).
   - Add **UI -> Button** as a child. Position it on the right edge of the Input Field. (Name it: *ToggleArrow*).
2. **The Suggestion Body**:
   - Create **UI -> Scroll View** inside the group. Position it just below the Input Field.
   - On the **Content** object of the Scroll View, add:
     - **Vertical Layout Group**: Spacing 5, Padding 10.
     - **Content Size Fitter**: Vertical Fit = `Preferred`.
   - **Deactivate** the entire Scroll View by default.
3. **Logic Wiring**:
   - Attach `LocationSearchBar` to **"SearchBar_Group"**.
   - **Assignments**:
     - `InputField`: Drag the *DestinationInput* child.
     - `Suggestion Transform`: Drag the **Content** object of the Scroll View.
     - `Suggestion Prefab`: Drag your **Suggestion Item Prefab** (see below).
     - `Dropdown icon`: Drag the *ToggleArrow* button.
     - `App Flow`: Drag the **GameManager**.

### C. Creating the Suggestion Prefab
1. Create a `UI -> Button - TextMeshPro` in a temporary spot in the Hierarchy.
2. Style it (rounded corners, white background, font color: Deep Blue).
3. Set text alignment to Left.
4. Drag it into your **Prefabs** folder and delete it from the Hierarchy.
5. Link this prefab to the `LocationSearchBar` script's **Suggestion Prefab** slot.

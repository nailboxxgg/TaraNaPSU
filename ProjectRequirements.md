# Project Requirements & Status Checklist

This document outlines the necessary components and structure for the TaraNaPSU AR Navigation project, verified against Unity AR Foundation documentation.

## 1. Unity Packages (Verified in manifest.json)
-   [x] **AR Foundation** (`com.unity.xr.arfoundation`): Core AR features.
-   [x] **AR Core XR Plugin** (`com.unity.xr.arcore`): Android support.
-   [x] **XR Plugin Management** (`com.unity.xr.management`): Loader management.
-   [x] **AI Navigation** (`com.unity.ai.navigation`): For `NavMeshAgent`.

## 2. Essential Scene Components (Must exist in Unity Scene)
> **Note**: These components must be added in the Unity Editor hierarchy.

-   [ ] **AR Session**:
    -   Script: `ARSession.cs`
    -   Script: `ARInputManager.cs`
-   [ ] **XR Origin (Mobile AR)**:
    -   Script: `XROrigin.cs`
    -   **Camera Offset**: Parent of the Main Camera.
    -   **Main Camera**:
        -   Tag: `MainCamera`
        -   Script: `ARCameraManager.cs`
        -   Script: `ARCameraBackground.cs`
        -   Script: `TrackedPoseDriver.cs` (Input System).
-   [ ] **Navigation**:
    -   **NavMeshSurface**: Use specific agent type settings matching your `NavMeshAgent`.
-   [ ] **Managers GameObject**:
    -   `AppFlowController`
    -   `NavigationController`
    -   `AnchorManager`
    -   `TargetManager`
    -   `NotificationController` (UI)

## 3. Script Refactoring Status
-   [x] **Code Cleanliness**: All comments and Admin code removed.
-   [x] **Data Structures**: Centralized in `Core/DataStructures.cs`.
-   [x] **QR Scanning**: Refactored `ZXingScannerController` to use `ARCameraManager` instead of `WebCamTexture` for AR compatibility.
-   [x] **Navigation**: `NavigationController` requires `NavMeshAgent` and `LineRenderer`.
-   [x] **Dependencies**: All scripts found in `Assets/Scripts` are properly linked and organized.

## 4. Next Steps for User
1.  **Open Unity Editor**.
2.  **Verify Scene Hierarchy**: Ensure `AR Session` and `XR Origin` exist.
3.  **Bake NavMesh**: Ensure the navigation mesh is baked.
4.  **Assign References**: Link `NavigationController` and `ZXingScannerController` references in Inspector.

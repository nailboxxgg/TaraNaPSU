# Project Requirements & Status Checklist

This document outlines the necessary components and structure for the TaraNaPSU 2D Navigation project.

## 1. Unity Packages
-   [x] **AI Navigation** (`com.unity.ai.navigation`): For `NavMeshAgent` and pathfinding.
-   [x] **Unity UI** (`com.unity.ugui`): For standard UI components and TextMeshPro.

## 2. Essential Scene Components (Must exist in Unity Scene)
> **Note**: These components must be added in the Unity Editor hierarchy.

-   [x] **Main Camera**:
    -   **Projection**: Orthographic
    -   **Rotation**: (90, 0, 0) - Top-down view
-   [x] **Navigation**:
    -   **NavMeshSurface**: Use specific agent type settings matching your `NavMeshAgent`.
-   [x] **Managers GameObject**:
    -   `AppFlowController2D`
    -   `Map2DController`
    -   `Navigation2DController`
    -   `AnchorManager`
    -   `TargetManager`
    -   `QRUIController`

## 3. Script Refactoring Status
-   [x] **2D Transition**: AR Foundation removed; Orthographic camera logic implemented.
-   [x] **Code Cleanliness**: All comments and Admin code removed.
-   [x] **Data Structures**: Centralized in `Core/DataStructures.cs`.
-   [x] **QR Scanning**: Uses standard `WebCamTexture` for re-localization.
-   [x] **Navigation**: `Navigation2DController` handles path rendering on the 2D plane.
-   [x] **Dependencies**: All scripts found in `Assets/Scripts` are properly linked.

## 4. Next Steps for User
1.  **Open Unity Editor**.
2.  **Verify Scene Hierarchy**: Ensure `GameManager` (with controllers) and `Main Camera` exist.
3.  **Bake NavMesh**: Ensure the navigation mesh is baked for all floors.
4.  **Assign References**: Link `Map2DController` and UI components in the Inspector.

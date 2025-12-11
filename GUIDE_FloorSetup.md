# 🏗️ Unity Floor & Room Setup Guide

This guide will help you create accurate room layouts for your AR Navigation app.

## 📏 The Golden Rule
In Unity, **1 Unit = 1 Meter**.
*   A default Cube (1x1x1) is exactly **1 meter** wide, tall, and deep.
*   An average doorway is **0.9 to 1.0 meters** wide.
*   An average room height is **2.5 to 3.0 meters**.

---

## 🛠️ Step 1: Install ProBuilder (Recommended)
While you *can* use basic Cubes, **ProBuilder** makes creating rooms much faster.
1.  In Unity, go to **Window > Package Manager**.
2.  Change the dropdown from "In Project" to **"Unity Registry"**.
3.  Search for **ProBuilder** and click **Install**.

---

## 🖼️ Step 2: Set Up Your Blueprint (Floor Plan)
To get accurate scale, we will use a floor plan image as a reference.

1.  **Get your Floor Plan**: Take a picture or scan of the building's floor plan.
2.  **Import to Unity**: Drag the image into your `Assets/Textures` folder.
3.  **Create a Plane**:
    *   Right-click in Hierarchy > `3D Object` > `Plane`.
    *   Name it `Reference_Plan_Floor1`.
4.  **Apply Image**: Drag your floor plan image from the Project window onto the Plane in the Scene view.

### 📐 Callibrating the Scale (Crucial Step!)
Now we need to make sure the image is the correct size in the real world.

1.  **Create a Reference Cube**:
    *   Right-click > `3D Object` > `Cube`.
    *   In the Inspector, set its **Scale** to `(1, 1, 1)` (This represents 1 meter).
    *   *Tip: If you know a hallway is exactly 3 meters wide, set the Cube scale to `(3, 1, 1)`.*
2.  **Align & Scale**:
    *   Move the Cube to a known spot on your floor plan (e.g., a doorway or hallway width).
    *   Select your `Reference_Plan_Floor1`.
    *   Use the **Scale Tool (R)** to enlarge/shrink the plane until the doorway on the drawing matches the width of your 1-meter Cube.
    *   *Now your floor plan is real-world scale!*

---

## 🧱 Step 3: Building the Walls
Now that your reference is scaled, trace over it.

### Option A: Using ProBuilder (Best)
1.  Open **Tools > ProBuilder > ProBuilder Window**.
2.  Use the **New Shape** tool to draw walls efficiently.
3.  Select specific faces to extrude or move walls easily.

### Option B: Using Standard Cubes (Simple)
1.  Create a Cube: `3D Object` > `Cube`.
2.  Name it `Wall_Outer`.
3.  **Scale it**:
    *   Width/Length: Match your floor plan lines.
    *   Height (**Y**): Set to **0.1** if creating just the floor, or **2.5** if creating full walls.
4.  **Duplicate (Ctrl+D)**: Move and rotate pieces to form your rooms.

---

## 📂 Step 4: Organization (Important for Navigation!)
Your `NavigationController` expects a specific hierarchy to handle multi-floor switching automatically.

Organize your hierarchy like this:
```text
NavMeshSurfaces
├── Building B1
│   ├── Floor0_B1           <-- Group all Level 1 Ground objects here
│   │   ├── FloorCollider   <-- The floor the user walks on
│   │   ├── Walls
│   │   └── Props
│   └── Floor1_B1           <-- Group all Level 2 objects here
├── Building B2
    ...
```

*   **Note**: Put your **StairwayMarkers** at the actual positions where the stairs meet the floor.

---

## 👟 Step 5: Testing Scale
1.  Create a **Capsule** (`3D Object` > `Capsule`).
2.  Set position to `(0, 1, 0)`.
3.  This represents an average human (2m tall).
4.  Move it around your rooms. Do the doorways look correct? Does the hallway feel cramped? Adjust if necessary.

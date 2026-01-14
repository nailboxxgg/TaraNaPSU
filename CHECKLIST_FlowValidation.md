    # Final Checklist: Ensuring Navigation Flow

If selecting points works but the "Start Navigation" button does nothing, check these 3 things in Unity:

## 1. The "Start" Button Event
This is the most common cause.
1.  Select your **Start Navigating** button in the Hierarchy.
2.  In the **Inspector**, look at the **On Click ()** section.
3.  Ensure your `AppFlowController2D` object is dragged into the slot.
4.  Ensure the function selected is **`AppFlowController2D > SmartStart`**.

## 2. AppFlow Panel Assignments
If the panels aren't assigned, the screen won't change.
1.  Select your **AppFlowController2D** object.
2.  Check the **Panels** header:
    - **Welcome Panel**: Should be the parent group of your starting screen.
    - **Map Panel**: Should be the parent group of your map screen.
3.  Check the **References** header:
    - **Navigation Controller**: Drag your `Navigation2DController` object here.

## 3. NavMesh Baking
If the code runs but no blue line appears:
1.  Go to **Window > AI > Navigation (Obsolete)** or **Navigation**.
2.  Make sure you have **Baked** the NavMesh for all floor surfaces.
3.  In Play mode, check the Console for `[Nav2D] Could not find path`. If you see this, your start or end positions are too far from the walkable area.

---

### How to use the Debug Logs
While playing, keep the **Console Window** open.
- **Goal**: You want to see `[AppFlow2D] Flow confirmed: ... Starting...`
- **If you see `No destination selected`**: Your Search Bar is not successfully telling the AppFlow what was picked. Ensure the Search Bar has the AppFlow reference.
- **If you see `No start point selected`**: Your Start Point Selector is not successfully telling the AppFlow what was picked.

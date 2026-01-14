# Guide: Creating the Searchable Start Point Dropdown

This guide will walk you through the process of creating the UI for the searchable starting point in Unity, using the updated `StartPointSelector.cs` script.

## Step 1: Create the Suggestion Item Prefab
1.  **Create Object**: Right-click in Hierarchy > **UI > Button - TextMeshPro**. Rename it `SuggestionItem_Prefab`.
2.  **Size**: Set **Width** to `350` and **Height** to `60` (or whatever fits your design).
3.  **Navigation**: Set **Navigation** to `None` in the Button component.
4.  **Text Alignment**: Select the child `Text (TMP)` and set:
    - **Alignment**: Left & Middle.
    - **Margin (Left)**: `15`.
5.  **Save Prefab**: Drag the object from Hierarchy into your `Assets/Prefabs` folder.
6.  **Delete**: Remove it from the Hierarchy.

---

## Step 2: Create the Search Bar UI
1.  **Container**: Create a **Panel**, **Image**, or an **Empty GameObject (RectTransform)** as a parent. Name it `StartPoint_SearchBar`. (Use an empty object if you don't need a visual background).
2.  **Input Field**: Inside the container, add **UI > Input Field - TextMeshPro**.
    - Rename to `StartPoint_InputField`.
    - Place it at the top.
3.  **Clear Button**: Add a **UI > Button** inside the Input Field.
    - Rename to `Clear_Button`.
    - Position it on the far right of the input box.
    - Icon: Use an "X" sprite or text.
4.  **Toggle Button**: Add another **UI > Button**.
    - Rename to `Toggle_Button`.
    - Position it next to the Clear button (usually a down arrow).
    - Icon: Use a "Dropdown Arrow" sprite.

---

## Step 3: Create the Suggestion List (Scroll View)
1.  **Scroll View**: Right-click on your `StartPoint_SearchBar` container > **UI > Scroll View**.
    - Rename to `Suggestion_List`.
    - Position it directly below the Input Field.
2.  **Clean Up**:
    - Remove the **Horizontal Scrollbar**.
    - In the **Scroll Rect** component, uncheck **Horizontal**.
3.  **Content Layout**: Select the `Content` object inside `Viewport`:
    - Add a **Vertical Layout Group** component.
    - Check **Child Force Expand (Width)**.
    - Add a **Content Size Fitter** component.
    - Set **Vertical Fit** to `Preferred Size`.

---

## Step 4: Link Everything in the Script
1.  **Attach Script**: Select your main UI controller or the `StartPoint_SearchBar` object and ensure the `StartPointSelector.cs` script is attached.
2.  **Assign References**:
    - **Input Field**: Drag `StartPoint_InputField`.
    - **Suggestion Root**: Drag the **Suggestion_List** (the Scroll View itself). 
        > [!WARNING]
        > **DO NOT** drag the entire `StartPoint_SearchBar` parent here, or the whole search bar will disappear when the game starts!
    - **Suggestion Container**: Drag the **Content** object (child of the Scroll View's Viewport).
    - **Suggestion Item Prefab**: Drag your `SuggestionItem_Prefab` from Assets.
    - **Clear Button**: Drag `Clear_Button`.
    - **Toggle Button**: Drag `Toggle_Button`.
    - **App Flow**: Drag your `AppFlowController2D` object.

---

## Step 5: Final Polish
1.  **Auto-Hide**: Click on the `Suggestion_List` (Scroll View) and disable its GameObject (uncheck the box in the Inspector). The script will show it automatically when you start typing.
2.  **Re-selection**: The script is now configured to show all options whenever you click back into the input field, making it easy to change the starting point at any time.
3.  **Test**: Enter Play mode, type "Gate" or any entrance name, and see the suggestions appear!

---

## 🛠️ Troubleshooting: "My Search Bar disappeared!"
If your search bar group disappears as soon as you press Play:
1.  **Check Suggestion Root**: You likely assigned the **Parent Object** (the one containing the Input Field) to the `Suggestion Root` field.
2.  **The Fix**: Ensure `Suggestion Root` is assigned **ONLY** to the `Suggestion_List` (the Scroll View). The script disables the Root on start; if the Root is the parent, the whole search bar turns off!

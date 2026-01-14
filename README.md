# TaraNaPSU - 2D Indoor Navigation System

![Project Banner](https://placeholder-image-url.com) *<!-- Replace with actual banner if available -->*

**Capstone Project for A.Y. 2025-2026**  
**Institution:** Pangasinan State University - Alaminos City Campus  
**Adviser:** Christian Paul Cruz  
**Developed By:** Marc Nels Luminoque, Sean Ryzen Pasuquin

---

## 📖 Project Overview

**TaraNaPSU** is a 2D Top-Down Indoor Navigation application designed to help students, faculty, and visitors navigate the Pangasinan State University Alaminos City Campus. 

Unlike traditional AR apps, TaraNaPSU provides a clean, 2D orthographic map view that allows users to plan their routes across multiple buildings and floors with ease. Using Unity's NavMesh features, the application provides real-time pathfinding to classrooms, offices, and facilities, addressing the challenge of navigating complex campus layouts through a familiar digital map interface.

## ✨ Key Features

*   **2D Map Pathfinding:** Visualizes the shortest path to a destination using a top-down digital map.
*   **Multi-Floor Interactivity:** Seamlessly switch between floors (Ground Floor, 1st Floor, etc.) using an intuitive UI selector.
*   **Multi-Building Navigation:** Navigate between campus buildings (e.g., Building 1 and Building 2) with accurate spatial representation.
*   **Flexible Localization:** 
    *   **Dropdown Selection:** Quickly set your starting point from a list of major entrances.
    *   **QR Code Re-localization:** Scan QR codes at checkpoints to instantly update your position on the map.
*   **Smart Searchable Directory:** A comprehensive, type-ahead search for offices, faculty rooms, and labs.
*   **Automated Routing:** Automatically calculates paths through stairways and corridors using an optimized Navigation Mesh.

## 🛠️ Tech Stack

*   **Engine:** Unity 2022.3 (or relevant version)
*   **Language:** C#
*   **Camera:** Orthographic 2D Projection
*   **Navigation:** Unity NavMesh Surface (AI Navigation)
*   **UI System:** Unity UI (UGUI) with TextMeshPro
*   **Data Storage:** JSON (in `Assets/Resources`)

## 🚀 Getting Started

### Prerequisites
*   **Unity Hub** and **Unity Editor** installed.
*   **Android Device** (for testing the mobile experience).
*   USB Cable for debugging/deploying.

### Installation
1.  **Clone the Repository:**
    ```bash
    git clone https://github.com/Start-sys-cmd/TaraNaPSU.git
    ```
2.  **Open in Unity:**
    *   Open Unity Hub -> Add -> Select the `TaraNaPSU` folder.
    *   Open the project.
3.  **Build to Android:**
    *   Go to `File > Build Settings`.
    *   Ensure Platform is set to **Android**.
    *   Connect your device and click **Build and Run**.

## 📱 How to Use

1.  **Launch the App:** You will see the Welcome Panel.
2.  **Select/Scan Your Location:**
    *   Choose your current entrance from the **Start Point Dropdown**.
    *   OR tap the **QR/Camera icon** to scan a nearby location anchor.
3.  **Select Destination:**
    *   Tap the **Search Bar**.
    *   Start typing the name of the office or room (e.g., "Quality Assurance Office").
    *   Select the target from the suggestions.
4.  **Follow the Map:** The map will automatically calculate and draw a path from your location to the target.
5.  **Changing Floors:** Use the **Floor Selector** buttons on the side to view different levels of the campus.

## 📂 Configuration & Data

The app navigates based on static data defined in JSON files located in `Assets/Resources`.

### 1. `AnchorData.json`
Defines the physical starting points (QR codes/Entrances) in the real world.
```json
{
  "Type": "Entrance",
  "AnchorId": "Building-1-Entrance",
  "BuildingId": "B1",
  "Floor": 0,
  "Position": { "x": 43.0, "y": 0.0, "z": 35.5 }
}
```
*   **AnchorId**: Unique ID matched with the QR code content.
*   **Position**: The Unity world coordinates where this anchor exists.

### 2. `TargetData.json`
Defines the destination points (Rooms, Offices).
```json
{
  "Name": "Quality Assurance Office",
  "FloorNumber": 0,
  "Position": { "x": 35, "y": 1, "z": 44.5 }
}
```
*   **Name**: Display name in the search bar.
*   **FloorNumber**: `0` for Ground Floor, `1` for 2nd Floor, etc.

## 🏗️ Project Structure

*   `Assets/Scripts/Controllers`:
    *   `Map2DController.cs`: Manages 2D camera movement and floor visibility.
    *   `AppFlowController2D.cs`: Controls the main application state and navigation logic.
*   `Assets/Scripts/UI`:
    *   `LocationSearchBar.cs`: Handles searchable destination dropdown.
    *   `FloorSelectorUI.cs`: Manages floor switching buttons.
*   `Assets/Resources`: Contains the `.json` data files.

## 🤝 Contributing

1.  Fork the project.
2.  Create your Feature Branch (`git checkout -b feature/AmazingFeature`).
3.  Commit your changes (`git commit -m 'Add some AmazingFeature'`).
4.  Push to the branch (`git push origin feature/AmazingFeature`).
5.  Open a Pull Request.

---
*© 2025 Pangasinan State University - Alaminos City Campus*

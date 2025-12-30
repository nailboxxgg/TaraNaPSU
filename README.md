# TaraNaPSU - AR Indoor Navigation System

![Project Banner](https://placeholder-image-url.com) *<!-- Replace with actual banner if available -->*

**Capstone Project for A.Y. 2025-2026**  
**Institution:** Pangasinan State University - Alaminos City Campus  
**Adviser:** Christian Paul Cruz  
**Developed By:** Marc Nels Luminoque, Sean Ryzen Pasuquin

---

## 📖 Project Overview

**TaraNaPSU** is an Augmented Reality (AR) Indoor Navigation application designed to help students, faculty, and visitors navigate the Pangasinan State University Alaminos City Campus. 

Using Unity's AR Foundation and NavMesh features, the application provides real-time, visual pathfinding to classrooms, offices, and facilities across multiple buildings and floors. It addresses the challenge of navigating complex campus layouts by overlaying digital directional arrows onto the real-world camera view.

## ✨ Key Features

*   **AR Pathfinding:** Visualizes the shortest path to a destination using AR arrows and lines.
*   **Multi-Floor Navigation:** Seamlessly guides users between floors, automatically routing to nearest stairways.
*   **Building-to-Building Navigation:** Supports outdoor navigation between campus buildings (e.g., Main Gate -> Building 1).
*   **QR Code Re-localization:** Uses QR codes placed at key locations (entrances, stair landings) to instantly correct the user's position and current floor.
*   **Searchable Directory:** A comprehensive list of offices, faculty rooms, and labs that users can search and select as destinations.
*   **Smart Floor Switching:** Automatically detects floor changes and updates the navigation mesh accordingly.

## 🛠️ Tech Stack

*   **Engine:** Unity 2022.3 (or relevant version)
*   **Language:** C#
*   **AR Framework:** AR Foundation (ARCore/ARKit)
*   **Navigation:** Unity NavMesh Surface
*   **Data Storage:** JSON (in `Assets/Resources`)

## 🚀 Getting Started

### Prerequisites
*   **Unity Hub** and **Unity Editor** installed.
*   **Android Device** with ARCore support (for testing).
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

1.  **Launch the App:** Give camera permissions when prompted.
2.  **Scan an Anchor:** Point the camera at a known QR code/marker (e.g., Main Gate or Building Entrance) to localize your position.
3.  **Select Destination:**
    *   Tap the **Search Bar**.
    *   Type the name of the office or room (e.g., "Quality Assurance Office").
    *   Select the target from the list.
4.  **Follow the Arrows:** Follow the floating AR arrows to your destination.
5.  **Changing Floors:**
    *   If your destination is on another floor, the app will guide you to a stairway.
    *   Once you climb the stairs, scan the QR code on the new floor to update your location.

## 📂 Configuration & Data

The app navigates based on static data defined in JSON files located in `Assets/Resources`.

### 1. `AnchorData.json`
Defines the physical starting points (QR codes) in the real world.
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

*   `Assets/Scripts/Core`:
    *   `AnchorManager.cs`: Manages positioning relative to QR anchors.
    *   `TargetManager.cs`: Loads and filters navigation targets.
*   `Assets/Scripts/UI`:
    *   `NavigationPanelController.cs`: Controls the bottom UI and direction prompts.
    *   `SearchBarQR.cs`: Handles search input and QR scanning toggle.
*   `Assets/Resources`: Contains the `.json` data files.

## 🤝 Contributing

1.  Fork the project.
2.  Create your Feature Branch (`git checkout -b feature/AmazingFeature`).
3.  Commit your changes (`git commit -m 'Add some AmazingFeature'`).
4.  Push to the branch (`git push origin feature/AmazingFeature`).
5.  Open a Pull Request.

---
*© 2025 Pangasinan State University - Alaminos City Campus*

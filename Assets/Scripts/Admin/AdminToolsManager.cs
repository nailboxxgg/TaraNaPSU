using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdminToolsManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject buildingPanel;
    public GameObject targetPanel;
    public GameObject anchorPanel;

    [Header("Input Fields - Target")]
    public TMP_InputField targetNameInput;
    public TMP_InputField targetFloorInput;
    public TMP_InputField targetX;
    public TMP_InputField targetY;
    public TMP_InputField targetZ;

    [Header("Input Fields - Anchor")]
    public TMP_Dropdown anchorTypeDropdown;
    public TMP_InputField anchorBuildingInput;
    public TMP_InputField anchorFloorInput;
    public TMP_InputField anchorX;
    public TMP_InputField anchorY;
    public TMP_InputField anchorZ;
    public TMP_InputField anchorRotX;
    public TMP_InputField anchorRotY;
    public TMP_InputField anchorRotZ;

    [Header("Buttons")]
    public Button saveAllButton;
    public Button exportButton;
    public Button importButton;

    private List<TargetData> targets = new();
    private List<AnchorManager.AnchorData> anchors = new();

    private string targetPath;
    private string anchorPath;

    private void Start()
    {
        targetPath = Path.Combine(Application.persistentDataPath, "TargetData.json");
        anchorPath = Path.Combine(Application.persistentDataPath, "AnchorData.json");

        LoadExistingData();
        ShowTargetPanel();
    }

    // -------------------------------------------------------------------------
    // 🧭 UI Tabs
    // -------------------------------------------------------------------------
    public void ShowBuildingPanel()
    {
        buildingPanel.SetActive(true);
        targetPanel.SetActive(false);
        anchorPanel.SetActive(false);
    }

    public void ShowTargetPanel()
    {
        buildingPanel.SetActive(false);
        targetPanel.SetActive(true);
        anchorPanel.SetActive(false);
    }

    public void ShowAnchorPanel()
    {
        buildingPanel.SetActive(false);
        targetPanel.SetActive(false);
        anchorPanel.SetActive(true);
    }

    // -------------------------------------------------------------------------
    // 📥 Load Existing Data
    // -------------------------------------------------------------------------
    private void LoadExistingData()
    {
        // Load Targets
        if (File.Exists(targetPath))
        {
            string json = File.ReadAllText(targetPath);
            TargetListWrapper wrapper = JsonUtility.FromJson<TargetListWrapper>(json);
            if (wrapper != null && wrapper.targets != null)
            {
                targets = wrapper.targets;
                Debug.Log($"[AdminTools] Loaded {targets.Count} targets.");
            }
        }
        else
        {
            TextAsset resource = Resources.Load<TextAsset>("TargetData");
            if (resource != null)
            {
                TargetListWrapper wrapper = JsonUtility.FromJson<TargetListWrapper>(resource.text);
                targets = wrapper.targets;
            }
        }

        // Load Anchors
        if (File.Exists(anchorPath))
        {
            string json = File.ReadAllText(anchorPath);
            AnchorManager.AnchorListWrapper wrapper = JsonUtility.FromJson<AnchorManager.AnchorListWrapper>(json);
            if (wrapper != null && wrapper.anchors != null)
            {
                anchors = wrapper.anchors;
                Debug.Log($"[AdminTools] Loaded {anchors.Count} anchors.");
            }
        }
        else
        {
            TextAsset resource = Resources.Load<TextAsset>("AnchorData");
            if (resource != null)
            {
                AnchorManager.AnchorListWrapper wrapper = JsonUtility.FromJson<AnchorManager.AnchorListWrapper>(resource.text);
                anchors = wrapper.anchors;
            }
        }
    }

    // -------------------------------------------------------------------------
    // ➕ Add / Edit Target
    // -------------------------------------------------------------------------
    public void OnAddTargetPressed()
    {
        if (string.IsNullOrEmpty(targetNameInput.text))
        {
            Debug.LogWarning("[AdminTools] Target name cannot be empty.");
            return;
        }

        // Use local Vector3Serializable for TargetData
        TargetData newTarget = new TargetData
        {
            Name = targetNameInput.text,
            FloorNumber = int.Parse(targetFloorInput.text),
            Position = new Vector3Serializable(
                float.Parse(targetX.text),
                float.Parse(targetY.text),
                float.Parse(targetZ.text)
            ),
            Rotation = new Vector3Serializable(0, 0, 0)
        };

        targets.Add(newTarget);
        Debug.Log($"[AdminTools] Added Target: {newTarget.Name}");
        NotificationController.Instance?.Show($"✅ Added Target: {newTarget.Name}");
    }

    // -------------------------------------------------------------------------
    // ➕ Add / Edit Anchor
    // -------------------------------------------------------------------------
    public void OnAddAnchorPressed()
    {
        AnchorManager.AnchorData newAnchor = new AnchorManager.AnchorData
        {
            Type = anchorTypeDropdown.options[anchorTypeDropdown.value].text,
            BuildingId = anchorBuildingInput.text,
            Floor = int.Parse(anchorFloorInput.text),
            Position = new AnchorManager.Vector3Serializable(
                float.Parse(anchorX.text),
                float.Parse(anchorY.text),
                float.Parse(anchorZ.text)
            ),
            Rotation = new AnchorManager.Vector3Serializable(
                float.Parse(anchorRotX.text),
                float.Parse(anchorRotY.text),
                float.Parse(anchorRotZ.text)
            ),
            AnchorId = $"{anchorBuildingInput.text}-{anchorTypeDropdown.options[anchorTypeDropdown.value].text}-{anchors.Count + 1}"
        };

        anchors.Add(newAnchor);
        Debug.Log($"[AdminTools] Added Anchor: {newAnchor.AnchorId}");
        NotificationController.Instance?.Show($"✅ Added Anchor: {newAnchor.AnchorId}");
    }

    // -------------------------------------------------------------------------
    // 💾 Save All
    // -------------------------------------------------------------------------
    public void OnSaveAllPressed()
    {
        // Targets
        TargetListWrapper targetWrapper = new TargetListWrapper { targets = targets };
        string targetJson = JsonUtility.ToJson(targetWrapper, true);
        File.WriteAllText(targetPath, targetJson);

        // Anchors
        AnchorManager.AnchorListWrapper anchorWrapper = new AnchorManager.AnchorListWrapper { anchors = anchors };
        string anchorJson = JsonUtility.ToJson(anchorWrapper, true);
        File.WriteAllText(anchorPath, anchorJson);

        Debug.Log($"✅ All data saved to persistent path: {Application.persistentDataPath}");
        NotificationController.Instance?.Show("💾 All changes saved!");
    }

    // -------------------------------------------------------------------------
    // 📤 Export Data (to Downloads folder)
    // -------------------------------------------------------------------------
    public void OnExportPressed()
    {
        string downloads = Path.Combine(Application.persistentDataPath, "../Download");
        Directory.CreateDirectory(downloads);

        string targetExportPath = Path.Combine(downloads, "TargetData_Export.json");
        string anchorExportPath = Path.Combine(downloads, "AnchorData_Export.json");

        File.Copy(targetPath, targetExportPath, true);
        File.Copy(anchorPath, anchorExportPath, true);

        Debug.Log($"📤 Exported TargetData and AnchorData to:\n{downloads}");
        NotificationController.Instance?.Show("📤 Export completed!");
    }

    // -------------------------------------------------------------------------
    // 📥 Import Data (from Downloads folder)
    // -------------------------------------------------------------------------
    public void OnImportPressed()
    {
        string downloads = Path.Combine(Application.persistentDataPath, "../Download");
        string targetImport = Path.Combine(downloads, "TargetData_Import.json");
        string anchorImport = Path.Combine(downloads, "AnchorData_Import.json");

        if (File.Exists(targetImport))
        {
            File.Copy(targetImport, targetPath, true);
            Debug.Log("✅ Imported TargetData from Downloads.");
        }

        if (File.Exists(anchorImport))
        {
            File.Copy(anchorImport, anchorPath, true);
            Debug.Log("✅ Imported AnchorData from Downloads.");
        }

        LoadExistingData();
        NotificationController.Instance?.Show("📥 Import successful! Data reloaded.");
    }

    // -------------------------------------------------------------------------
    // 📦 Wrappers
    // -------------------------------------------------------------------------
    [System.Serializable]
    public class TargetListWrapper
    {
        public List<TargetData> targets;
    }

    // Local serializable structure for TargetData
    [System.Serializable]
    public class TargetData
    {
        public string Name;
        public int FloorNumber;
        public Vector3Serializable Position;
        public Vector3Serializable Rotation;
    }

    [System.Serializable]
    public class Vector3Serializable
    {
        public float x, y, z;

        public Vector3Serializable() { }

        public Vector3Serializable(float x, float y, float z)
        {
            this.x = x; this.y = y; this.z = z;
        }

        public Vector3 ToVector3() => new Vector3(x, y, z);
    }
}

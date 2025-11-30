using UnityEngine;
using Unity.AI.Navigation;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Auto-creates NavMeshSurface structure for multi-building navigation
/// </summary>
public class NavMeshStructureSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    public bool autoSetupOnStart = false;

    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupNavMeshStructure();
        }
    }

    [ContextMenu("Setup NavMesh Structure")]
    public void SetupNavMeshStructure()
    {
        // Clear existing structure
        Transform existing = transform.Find("NavMeshSurfaces");
        if (existing != null)
        {
            DestroyImmediate(existing.gameObject);
        }

        // Create parent
        GameObject parent = new GameObject("NavMeshSurfaces");
        parent.transform.SetParent(transform);

        // Building structure - each building has 2 floors
        var buildingStructure = new Dictionary<string, int[]>
        {
            ["B1"] = new int[] { 0, 1 },  // Ground Floor, First Floor
            ["B2"] = new int[] { 2, 3 },  // Ground Floor (2), First Floor (3)
            ["B3"] = new int[] { 4, 5 }   // Ground Floor (4), First Floor (5) - matching your current naming
        };

        // Create surfaces to match your current naming convention
        foreach (var building in buildingStructure)
        {
            GameObject buildingParent = new GameObject($"Building {building.Key}");
            buildingParent.transform.SetParent(parent.transform);

            foreach (int floor in building.Value)
            {
                // Use your naming convention: Floor{number}_{Building}
                GameObject surface = new GameObject($"Floor{floor}_{building.Key}");
                surface.transform.SetParent(buildingParent.transform);

                // Add NavMeshSurface component
                NavMeshSurface navSurface = surface.AddComponent<NavMeshSurface>();

                // Note: Configure settings manually in the Inspector after creation
                // or use navSurface.defaultSettings for basic configuration

                Debug.Log($"Created NavMeshSurface: {surface.name} (Building {building.Key}, Floor {floor})");
            }
        }

        Debug.Log("NavMesh structure setup complete! Configure each surface and bake individually.");
    }

#if UNITY_EDITOR
    [MenuItem("TaraNaPSU/Setup NavMesh Structure")]
    public static void SetupNavMeshMenuItem()
    {
        GameObject setup = new GameObject("NavMeshSetup");
        setup.AddComponent<NavMeshStructureSetup>();
        Selection.activeGameObject = setup;
    }
#endif
}
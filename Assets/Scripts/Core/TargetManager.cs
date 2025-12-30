using System;
using System.Collections.Generic;
using UnityEngine;

public class TargetManager : MonoBehaviour
{
    public static TargetManager Instance { get; private set; }

    private Dictionary<string, TargetData> targets = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
            DontDestroyOnLoad(gameObject);
        LoadTargets();
    }

    private void LoadTargets()
    {
        TextAsset json = Resources.Load<TextAsset>("TargetData");
        if (json == null)
        {
            Debug.LogError("❌ TargetData.json not found!");
            return;
        }

        
        var wrapper = JsonUtility.FromJson<TargetListWrapper>(json.text);
        if (wrapper != null && wrapper.TargetList != null)
        {
             foreach (var target in wrapper.TargetList)
            {
                if (!targets.ContainsKey(target.Name))
                    targets.Add(target.Name, target);
            }
            Debug.Log($"✅ Loaded {targets.Count} targets from TargetData.json");
        }
        else
        {
            Debug.LogWarning("⚠️ TargetManager: wrapper or TargetList is null.");
        }
    }

    public List<string> GetAllTargetNames() => new List<string>(targets.Keys);
    public bool TryGetTarget(string name, out TargetData data) => targets.TryGetValue(name, out data);
}


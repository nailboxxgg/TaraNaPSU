using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TargetDataList
{
    public List<TargetData> TargetList;
}

[Serializable]
public class TargetData
{
    public string Name;
    public int FloorNumber;
    public Vector3Serializable Position;
    public Vector3Serializable Rotation;
}

[Serializable]
public class Vector3Serializable
{
    public float x, y, z;

    public Vector3 ToVector3() => new Vector3(x, y, z);
}

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

        var dataList = JsonUtility.FromJson<TargetDataList>(json.text);
        foreach (var target in dataList.TargetList)
        {
            if (!targets.ContainsKey(target.Name))
                targets.Add(target.Name, target);
        }

        Debug.Log($"✅ Loaded {targets.Count} targets from TargetData.json");
    }

    public List<string> GetAllTargetNames() => new List<string>(targets.Keys);
    public bool TryGetTarget(string name, out TargetData data) => targets.TryGetValue(name, out data);
}

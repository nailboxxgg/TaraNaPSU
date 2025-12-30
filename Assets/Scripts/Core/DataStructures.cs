using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[Serializable]
public class Vector3Serializable
{
    public float x, y, z;

    public Vector3Serializable() { }

    public Vector3Serializable(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Vector3Serializable(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }

    public Vector3 ToVector3() => new Vector3(x, y, z);
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
public class TargetListWrapper
{
    public List<TargetData> TargetList; 
}

[Serializable]
public class AnchorData
{
    public string Type;        
    public string BuildingId;  
    public string AnchorId;    
    public int Floor;          
    public Vector3Serializable Position;
    public Vector3Serializable Rotation;
    public string Meta;        

    public Vector3 PositionVector => Position.ToVector3();
    public Quaternion RotationQuaternion => Quaternion.Euler(Rotation.ToVector3());
}

[Serializable]
public class AnchorListWrapper
{
    public List<AnchorData> anchors;
}

[Serializable]
public class StairPair
{
    public string BuildingId;
    public AnchorData Bottom;
    public AnchorData Top;

    public bool IsValid => Bottom != null && Top != null;
}

[Serializable]
public class QRPayload
{
    public string type;
    public string buildingId;
    public string anchorId;
    public int floor;
}

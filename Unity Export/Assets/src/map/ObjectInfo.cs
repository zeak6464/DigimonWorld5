using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectInfo : MonoBehaviour {
    public enum ObjectType
    {
        None,
        Model,
        Terrain,
    }
    public ObjectType objectType;
    public int terrainClusterNo;
    public string modelPath;
}

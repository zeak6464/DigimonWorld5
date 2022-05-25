using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fog : MonoBehaviour
{
    [SerializeField]
    private bool isEnableFog = true;

    [SerializeField]
    private float firstParsonFogNear = 20;
    [SerializeField]
    private float firstParsonFogFar = 70;

    [SerializeField]
    private float threadParsonFogNear = 75;
    [SerializeField]
    private float threadParsonFogFar = 135;

    void Update()
    {
        if (isEnableFog && Yukar.Engine.MapData.sInstance != null && Yukar.Engine.MapData.sInstance.mapRom != null)
        {
            switch (Yukar.Engine.MapData.sInstance.mapRom.cameraMode)
            {
                case Yukar.Common.Rom.Map.CameraControlMode.NORMAL:
                    RenderSettings.fogStartDistance = threadParsonFogNear;
                    RenderSettings.fogEndDistance = threadParsonFogFar;
                    break;
                case Yukar.Common.Rom.Map.CameraControlMode.VIEW:
                    RenderSettings.fogStartDistance = firstParsonFogNear;
                    RenderSettings.fogEndDistance = firstParsonFogFar;
                    break;
            }
        }
    }        
}

//#define VR_MONO_MODE

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnityVR : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {
#if ENABLE_VR_UNITY
#if VR_MONO_MODE
        Screen.SetResolution(960, 544, false);
#else
#endif//VR_MONO_MODE
#endif//ENABLE_VR_UNITY

#if VR_MONO_MODE
        UnityEngine.VR.VRSettings.enabled = false;
#endif
    }

    // Update is called once per frame
    void Update()
    {
#if VR_MONO_MODE
        if (!UnityEngine.VR.VRSettings.enabled)
        {
            UpdateHeadTrackingForVRCameras();
        }
#endif
    }

#if VR_MONO_MODE
    private void UpdateHeadTrackingForVRCameras()
    {
        Quaternion pose =
          UnityEngine.VR.InputTracking.GetLocalRotation(
              UnityEngine.VR.VRSettings.enabled ? UnityEngine.VR.VRNode.Head : UnityEngine.VR.VRNode.CenterEye);
        Camera[] cams = Camera.allCameras;
        for (int i = 0; i < cams.Length; i++)
        {
            Camera cam = cams[i];
            if (cam.targetTexture == null && cam.cullingMask != 0)
            {
                cam.GetComponent<Transform>().localRotation = pose;
            }
        }
    }
#endif

}

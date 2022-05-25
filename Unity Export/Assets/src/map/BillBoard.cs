using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillBoard : MonoBehaviour
{
    static Quaternion sRotation = Quaternion.identity;
    private Quaternion origRotation;

    // Use this for initialization
    void Start()
    {
        origRotation = transform.localRotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (sRotation == Quaternion.identity)
        {
            var camera = Camera.main;
            var lookPos = -camera.transform.forward;
            lookPos.y = 0;
            //  0だとUnityでログを吐くのでチェック
            // https://docs.unity3d.com/ja/530/ScriptReference/Quaternion.LookRotation.html
            if (lookPos != Vector3.zero)
            {
                sRotation = Quaternion.LookRotation(lookPos, Vector3.up);
            }
        }

        this.transform.rotation = sRotation * origRotation;
    }

    void LateUpdate()
    {
        sRotation = Quaternion.identity;
    }
}

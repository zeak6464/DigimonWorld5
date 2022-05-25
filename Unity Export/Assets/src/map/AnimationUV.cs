using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationUV : MonoBehaviour
{
    [SerializeField]
    public bool IsStopAnimation = false;

    [SerializeField]
    public int StopAnimationU;
    [SerializeField]
    public int StopAnimationV;
    [SerializeField]
    public int StopAnimationFrames;
    [SerializeField]
    public float StopAnimationInterval;

    [SerializeField]
    public Vector2 SpeedUV = Vector2.zero;

    Renderer mMesh = null;
    float mAnimationTime = 0;

    // Use this for initialization
    void Start()
    {
        this.mMesh = this.GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 textureUV = Vector2.zero;

        if (this.IsStopAnimation)
        {
            int idx = (int)((this.mAnimationTime / this.StopAnimationInterval)) % this.StopAnimationFrames;
            int uidx = idx % (int)this.StopAnimationU;
            int vidx = (idx / (int)this.StopAnimationU) % (int)this.StopAnimationV;

            textureUV.x = (float)uidx / this.StopAnimationU;
            textureUV.y = 1.0f - (float)vidx / this.StopAnimationV;
        }
        else
        {
            textureUV += this.SpeedUV * this.mAnimationTime;
        }

        //テクスチャセット
        this.mMesh.material.SetTextureOffset("_MainTex", textureUV);

        this.mAnimationTime += Time.deltaTime;
    }
}

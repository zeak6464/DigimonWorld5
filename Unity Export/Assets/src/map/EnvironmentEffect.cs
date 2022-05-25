using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentEffect : MonoBehaviour
{
    int mPrewarmWaitCounter = 2;
    ParticleSystem[] mParticleList = null;

    // Use this for initialization
    void Start()
    {
        this.mParticleList = GetComponentsInChildren<ParticleSystem>();
        this.Stop();
    }

    // Update is called once per frame
    void Update()
    {
        if (0 < this.mPrewarmWaitCounter)
        {
            this.mPrewarmWaitCounter--;
            return;
        }

        this.UpdatePos();
        this.Play();
    }

    void UpdatePos()
    {
        var game = UnityEntry.game;
        if (game == null) return;

        {
            var pos = game.mapScene.mapDrawer.GetEnvironmentEffectPos();
            var mat = SharpKmyMath.Matrix4.translate(pos.x, pos.y, pos.z);
            Yukar.Common.UnityUtil.calcTransformFromMatrix(this.transform, mat.m);
        }

        // y軸反転
        {
            var pos = this.transform.localPosition;
            pos.y *= -1;
            this.transform.localPosition = pos;
        }
    }

    void Play()
    {
        foreach (var particle in this.mParticleList)
        {
            if (particle.isPlaying) continue;
            particle.Play();
        }
    }

    void Stop()
    {
        foreach (var particle in this.mParticleList)
        {
            if (particle.isPlaying == false) continue;
            particle.Stop();
            particle.Clear();
        }
    }

}

using System;
using SharpKmyMath;
using UnityEngine;
using System.Linq;

namespace SharpKmyGfx
{
    public class ParticleInstance
    {
        internal ParticleRoot basedef;
        DrawInfo dummy = new DrawInfo();
        GameObject instance;
        ParticleHandler handler;

        public GameObject GetGameObject() { return this.instance; }


        public ParticleInstance(ParticleRoot def)
        {
            basedef = def;
            instance = Yukar.Common.UnityUtil.createObject(Yukar.Common.UnityUtil.ParentType.OBJECT, def.path);
            handler = instance.AddComponent<ParticleHandler>();
            basedef.apply(instance.transform);
        }

        internal void start(Matrix4 matrix4)
        {
            //throw new NotImplementedException();
        }

        internal DrawInfo[] getDrawInfo()
        {
            return new DrawInfo[]{ dummy };
            //throw new NotImplementedException();
        }

        internal void update(float elapsed, Matrix4 m)
        {
            Yukar.Common.UnityUtil.calcTransformFromMatrix(instance.transform, m.m);
            //instance.transform.localScale *= ModelData.SCALE_FOR_UNITY;
            // y軸反転
            UnityEngine.Vector3 pos = instance.transform.localPosition;
            pos.y *= -1;
            instance.transform.localPosition = pos;
        }

        internal void draw(Render scn)
        {
            handler.visible = true;
        }

        internal void Release()
        {
            if (UnityEntry.IsImportMapScene()) return;
            UnityEngine.Object.Destroy(instance);
        }

        internal bool getUseDirection()
        {
            return basedef.useDirection;
        }

        internal void reset()
        {
            // TODO 必要そうだったら実装する
        }
    }

    public class ParticleHandler : MonoBehaviour
    {
        internal bool visible;

        private void Update()
        {
            /*
            var children = Yukar.Common.UnityUtil.getChildren(gameObject, true).ToList();
            if (children.Count == 0)
                return;

            if (visible != children[0].activeSelf)
            {
                children.ForEach(x => {
                    x.SetActive(visible);
                });
            }
            visible = false;
            */
            
            var components = GetComponentsInChildren<ParticleSystem>().ToList();
            if (components.Count == 0)
                return;

            if (visible != components[0].isPlaying)
            {
                components.ForEach(x => {
                    if (visible)
                    {
                        x.Clear();
                        x.Play();
                    }
                    else
                    {
                        x.Stop();
                        x.Clear();
                    }
                });
            }
            visible = false;
        }
    }
}
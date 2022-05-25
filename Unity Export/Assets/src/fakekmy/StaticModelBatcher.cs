using System;
using SharpKmyMath;
using System.Collections.Generic;
using UnityEngine;
using Yukar.Engine;

namespace SharpKmyGfx
{
    public class StaticModelBatcher
    {
        private ModelData template;
        //private int v;

        static internal Dictionary<MapObjectInstance, GameObject> instances = new Dictionary<MapObjectInstance, GameObject>();
        static private MapObjectInstance currentMapObject;

        public StaticModelBatcher(ModelData model, int v)
        {
            template = model;
            //this.v = v;
        }

        internal DrawInfo getDrawInfo(int v)
        {
            return new DrawInfo();

            //throw new NotImplementedException();
        }

        internal void draw(Render scn)
        {
            //throw new NotImplementedException();
        }

        internal void addInstance(Matrix4 m)
        {
            GameObject instance = null;

            if (!instances.ContainsKey(currentMapObject))
            {
                if (template.obj != null && currentMapObject != null)
                {
                    instance = currentMapObject.minst.inst.instance;
                    instances.Add(currentMapObject, instance);
                }
            }
            else
            {
                instance = instances[currentMapObject];
            }

            Yukar.Common.UnityUtil.calcTransformFromMatrix(instance.transform, m.m);
            instance.transform.localScale = template.obj.transform.localScale * ModelData.SCALE_FOR_UNITY;
            // y軸反転とオフセットが原点ではないときの対処
            var modelOffset = instance.transform.localRotation * (template.obj.transform.localPosition * ModelData.SCALE_FOR_UNITY);
            var pos = instance.transform.position;
            pos.y *= -1;
            instance.transform.localPosition = pos + modelOffset;
            // 回転が0,0,0ではない時の対処
            instance.transform.localRotation *= template.obj.transform.localRotation;
        }

        internal void clearInstances()
        {
        }

        internal bool isAvailable()
        {
            return false;
        }

        internal void Release()
        {
            //throw new NotImplementedException();
        }

        internal void setMaxInstanceCount(int count)
        {
            //throw new NotImplementedException();
        }

        static internal void setNextDrawInstance(MapObjectInstance p)
        {
            currentMapObject = p;
        }
    }
}
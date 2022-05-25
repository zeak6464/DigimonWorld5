using System;
using System.IO;
using UnityEngine;
using Yukar.Common;

namespace SharpKmyGfx
{
    public class ParticleRoot
    {
        internal string path;
        internal int refcount;
        float fPI = (float)Math.PI;
        internal bool useDirection;

        public ParticleRoot(string path)
        {
            this.path = UnityUtil.pathConvertToUnityResource(path, false);
        }
        
        public void Release()
        {
        }

        internal static ParticleRoot load(string path)
        {
            var instance = new ParticleRoot(path);
            return instance;
        }

        internal void apply(Transform trns)
        {
            var res = Resources.Load<TextAsset>(path);
            if (res == null)
                return;
            var buf = res.bytes;
            var stream = new MemoryStream(buf);
            var reader = new StreamReader(stream);
            
            var alphaCurve = new GradientAlphaKey[3];
            var brightnessCurve = new GradientColorKey[3];
            var scaleCurve = new float[3];

            float minSize = 0;
            float maxSize = 1;
            float animStart = 0;
            float startLifeTime = 0;

            var emitterType = "";
            var colors = new GradientColorKey[0];

            Vector3 angle = new Vector3();
            Vector3 angleRange = new Vector3();
            Vector3 startScale = new Vector3();
            Vector3 middleScale = new Vector3();
            Vector3 endScale = new Vector3();

            ParticleSystem ptcl = null;

            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                string[] words = line.Split(new char[] { ' ' });
                if (words.Length == 0)
                    continue;

                if (words[0] == "useDirection")
                {
                    useDirection = true;
                    continue;
                }

                if (words[0] == "name")
                {
                    createEntry(ref ptcl, ref trns, words[1]);
                    continue;
                }

                if (ptcl == null)
                {
                    createEntry(ref ptcl, ref trns, "no name");
                }

                var shape = ptcl.shape;
                var emission = ptcl.emission;
                var main = ptcl.main;
                var renderer = ptcl.GetComponent<ParticleSystemRenderer>();
                var velo = ptcl.velocityOverLifetime;
                var color = ptcl.colorOverLifetime;
                var size = ptcl.sizeOverLifetime;
                var rotate = ptcl.rotationOverLifetime;
                var anim = ptcl.textureSheetAnimation;
                var limit = ptcl.limitVelocityOverLifetime;

                Material mtl = null;
                if (UnityEntry.IsImportMapScene())
                {
                    mtl = renderer.material = new Material(renderer.sharedMaterial);
                }
                else
                {
                    mtl = renderer.material;
                }

                Func<int, float> getFloat = (int index) =>
                {
                    float result = 0;
                    if (words.Length > index)
                        float.TryParse(words[index], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out result);
                    return result;
                };
                Func<int, bool> getBool = (int index) =>
                {
                    bool result = false;
                    if (words.Length > index)
                        bool.TryParse(words[index], out result);
                    return result;
                };
                Func<int, int> getInt = (int index) =>
                {
                    int result = 0;
                    if (words.Length > index)
                        int.TryParse(words[index], out result);
                    return result;
                };

                switch (words[0])
                {
                    case "emitterType":
                        emitterType = words[1];

                        switch (emitterType)
                        {
                            case "box":
                                shape.shapeType = ParticleSystemShapeType.Box;
                                break;
                            case "point":
                                shape.shapeType = ParticleSystemShapeType.Box;
                                shape.randomDirectionAmount = 1;
                                break;
                            case "sphere":
                                shape.shapeType = ParticleSystemShapeType.Box;
                                break;
                        }

                        main.simulationSpace = ParticleSystemSimulationSpace.World;
                        break;
                    case "emitterAmount":
                        emission.rateOverTime = new ParticleSystem.MinMaxCurve(getFloat(1));
                        break;
                    case "emitterSize":
#if UNITY_2017_1_OR_NEWER
                        if (emitterType == "point")
                            shape.scale = new Vector3(0.01f, 0.01f, 0.01f);
                        else
                            shape.scale = new Vector3(getFloat(1), getFloat(2), getFloat(3));
                        break;
#else
                        if (emitterType == "point")
                            shape.box = new Vector3(0, 0, 0);
                        else
                            shape.box = new Vector3(getFloat(1), getFloat(2), getFloat(3));
                        break;
#endif
                    case "gravity":
                        main.gravityModifier = new ParticleSystem.MinMaxCurve(-getFloat(2) / 10);
                        break;
                    case "billboard":
                        bool isBillboard = getBool(1);
                        renderer.renderMode = isBillboard ? ParticleSystemRenderMode.Billboard : ParticleSystemRenderMode.Mesh;
                        renderer.alignment = isBillboard ? ParticleSystemRenderSpace.View : ParticleSystemRenderSpace.Local;
                        renderer.sortMode = ParticleSystemSortMode.YoungestInFront;
                        if (!isBillboard)
                        {
                            renderer.mesh = Resources.Load<Mesh>("Quad Mesh");
                            renderer.alignment = ParticleSystemRenderSpace.Local;
                        }
                        break;
                    case "particleLifeTime":
                        startLifeTime = getFloat(1);
                        break;
                    case "particleLifeRandom":
                        var ltr = getFloat(1) * 0.5f;
                        main.startLifetime = new ParticleSystem.MinMaxCurve(startLifeTime * (1 - ltr), startLifeTime * (1 + ltr));
                        break;
                    case "baseSpeed":
                        main.startSpeed = getFloat(1);
                        break;
                    case "linearVelocity":
                        velo.x = -getFloat(1);
                        velo.y = getFloat(2);
                        velo.z = getFloat(3);
                        velo.enabled = true;
                        break;
                    case "linearVelocityRandom":
                        var lvr = Math.Max(0f, Math.Min(1f, getFloat(1))) * 0.5f;
                        velo.x = new ParticleSystem.MinMaxCurve(velo.x.constant * (1 - lvr), velo.x.constant * (lvr + 1));
                        velo.y = new ParticleSystem.MinMaxCurve(velo.y.constant * (1 - lvr), velo.y.constant * (lvr + 1));
                        velo.z = new ParticleSystem.MinMaxCurve(velo.z.constant * (1 - lvr), velo.z.constant * (lvr + 1));
                        break;
                    case "airDrag":
#if UNITY_2017_2_OR_NEWER
                        limit.drag = getFloat(1);
                        limit.enabled = true;
#endif
                        break;
                    case "randomColorCount":
                        var colCnt = getInt(1) * 2;
                        if (colCnt == 0)
                            colCnt = 1;
                        colors = new GradientColorKey[colCnt];
                        break;
                    case "randomColor0":
                        if (colors.Length > 1)
                        {
                            colors[0].color = new UnityEngine.Color(getFloat(1), getFloat(2), getFloat(3), getFloat(4));
                            colors[0].time = 0;
                            colors[1].color = new UnityEngine.Color(getFloat(1), getFloat(2), getFloat(3), getFloat(4));
                            colors[1].time = 0.2499f;
                        }
                        else
                        {
                            colors[0].color = new UnityEngine.Color(1, 1, 1, 1);
                            colors[0].time = 0;
                        }
                        break;
                    case "randomColor1":
                        if (colors.Length > 3)
                        {
                            colors[2].color = new UnityEngine.Color(getFloat(1), getFloat(2), getFloat(3), getFloat(4));
                            colors[2].time = 0.25f;
                            colors[3].color = new UnityEngine.Color(getFloat(1), getFloat(2), getFloat(3), getFloat(4));
                            colors[3].time = 0.4999f;
                        }
                        break;
                    case "randomColor2":
                        if (colors.Length > 5)
                        {
                            colors[4].color = new UnityEngine.Color(getFloat(1), getFloat(2), getFloat(3), getFloat(4));
                            colors[4].time = 0.5f;
                            colors[5].color = new UnityEngine.Color(getFloat(1), getFloat(2), getFloat(3), getFloat(4));
                            colors[5].time = 0.7499f;
                        }
                        break;
                    case "randomColor3":
                        if (colors.Length > 7)
                        {
                            colors[6].color = new UnityEngine.Color(getFloat(1), getFloat(2), getFloat(3), getFloat(4));
                            colors[6].time = 0.75f;
                            colors[7].color = new UnityEngine.Color(getFloat(1), getFloat(2), getFloat(3), getFloat(4));
                            colors[7].time = 1.0f;
                        }

                        {
                            var grad = new Gradient();
                            grad.colorKeys = colors;
                            var minMax = new ParticleSystem.MinMaxGradient(grad);
                            minMax.mode = ParticleSystemGradientMode.RandomColor;
                            main.startColor = minMax;
                        }
                        break;
                    case "minSize":
                        minSize = getFloat(1) * 2;
                        break;
                    case "maxSize":
                        maxSize = getFloat(1) * 2;
                        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
                        break;
                    case "startAlpha":
                        alphaCurve[0].alpha = getFloat(1);
                        break;
                    case "middleAlpha":
                        alphaCurve[1].alpha = getFloat(1);
                        break;
                    case "endAlpha":
                        alphaCurve[2].alpha = getFloat(1);
                        break;
                    case "startAlphaTime":
                        alphaCurve[0].time = getFloat(1);
                        break;
                    case "endAlphaTime":
                        alphaCurve[2].time = getFloat(1);
                        if (alphaCurve[2].time == alphaCurve[0].time)
                        {
                            alphaCurve[0].time = 0;
                            alphaCurve[2].time = 1;
                        }
                        alphaCurve[1].time = (alphaCurve[2].time + alphaCurve[0].time) / 2;
                        break;
                    case "startBrightness":
                        {
                            var value = getFloat(1);
                            brightnessCurve[0].color = new UnityEngine.Color(value, value, value);
                        }
                        break;
                    case "middleBrightness":
                        {
                            var value = getFloat(1);
                            brightnessCurve[1].color = new UnityEngine.Color(value, value, value);
                        }
                        break;
                    case "endBrightness":
                        {
                            var value = getFloat(1);
                            brightnessCurve[2].color = new UnityEngine.Color(value, value, value);
                        }
                        break;
                    case "startBrightnessTime":
                        brightnessCurve[0].time = getFloat(1);
                        break;
                    case "endBrightnessTime":
                        brightnessCurve[2].time = getFloat(1);
                        if (brightnessCurve[2].time == brightnessCurve[0].time)
                        {
                            brightnessCurve[0].time = 0;
                            brightnessCurve[2].time = 1;
                        }
                        brightnessCurve[1].time = (brightnessCurve[2].time + brightnessCurve[0].time) / 2;
                        {
                            var grad = new Gradient();
                            grad.SetKeys(brightnessCurve, alphaCurve);
                            color.color = new ParticleSystem.MinMaxGradient(grad);
                        }
                        color.enabled = true;
                        break;
                    case "startScale":
                        startScale = new Vector3(getFloat(1), getFloat(2), getFloat(3));
                        break;
                    case "middleScale":
                        middleScale = new Vector3(getFloat(1), getFloat(2), getFloat(3));
                        break;
                    case "endScale":
                        endScale = new Vector3(getFloat(1), getFloat(2), getFloat(3));
                        break;
                    case "startScaleTime":
                        scaleCurve[0] = getFloat(1);
                        break;
                    case "endScaleTime":
                        scaleCurve[2] = getFloat(1);
                        if (scaleCurve[2] == scaleCurve[0])
                        {
                            scaleCurve[0] = 0;
                            scaleCurve[2] = 1;
                        }
                        scaleCurve[1] = (scaleCurve[2] + scaleCurve[0]) / 2;

                        size.enabled = true;
                        size.separateAxes = true;
                        var xCurve = new AnimationCurve();
                        xCurve.AddKey(scaleCurve[0], startScale.x);
                        xCurve.AddKey(scaleCurve[1], middleScale.x);
                        xCurve.AddKey(scaleCurve[2], endScale.x);
                        var yCurve = new AnimationCurve();
                        yCurve.AddKey(scaleCurve[0], startScale.y);
                        yCurve.AddKey(scaleCurve[1], middleScale.y);
                        yCurve.AddKey(scaleCurve[2], endScale.y);
                        var zCurve = new AnimationCurve();
                        zCurve.AddKey(scaleCurve[0], startScale.z);
                        zCurve.AddKey(scaleCurve[1], middleScale.z);
                        zCurve.AddKey(scaleCurve[2], endScale.z);
                        size.x = new ParticleSystem.MinMaxCurve(1, xCurve);
                        size.y = new ParticleSystem.MinMaxCurve(1, yCurve);
                        size.z = new ParticleSystem.MinMaxCurve(1, zCurve);
                        break;
                    case "angle":
                        angle = new Vector3(getFloat(1), getFloat(2), getFloat(3));
                        break;
                    case "angleRandRange":
                        angleRange = new Vector3(getFloat(1), getFloat(2), getFloat(3));

                        Vector3 min, max;
                        min = max = calcEulerRotateXYZ(angle);
                        var half = angleRange / 180 * (float)Math.PI;
                        min -= half;
                        max += half;

                        main.startRotation3D = true;
                        main.startRotationX = new ParticleSystem.MinMaxCurve(min.x, max.x);
                        main.startRotationY = new ParticleSystem.MinMaxCurve(min.y, max.y);
                        main.startRotationZ = new ParticleSystem.MinMaxCurve(min.z, max.z);
                        break;
                    case "angluarVelocity":
                        rotate.separateAxes = true;
                        rotate.x = new ParticleSystem.MinMaxCurve(getFloat(1) * (float)Math.PI / 180);
                        rotate.y = new ParticleSystem.MinMaxCurve(getFloat(2) * (float)Math.PI / 180);
                        rotate.z = new ParticleSystem.MinMaxCurve(getFloat(3) * (float)Math.PI / 180);
                        rotate.enabled = true;
                        break;
                    case "texPatternH":
                        anim.numTilesX = getInt(1);
                        anim.enabled = true;
                        break;
                    case "texPatternV":
                        anim.numTilesY = getInt(1);
                        anim.enabled = true;
                        break;
                    case "texPatternStart":
                        animStart = (float)getInt(1) / (anim.numTilesX * anim.numTilesY);
                        break;
                    case "texPatternEnd":
                        var animEnd = (float)getInt(1) / (anim.numTilesX * anim.numTilesY);
                        if (animStart == animEnd)
                            anim.startFrame = new ParticleSystem.MinMaxCurve(animStart);
                        else
                            anim.startFrame = new ParticleSystem.MinMaxCurve(animStart, animEnd);
                        break;
                    case "texPatternCycle":
                        anim.frameOverTime = new ParticleSystem.MinMaxCurve(0);
                        break;
                    case "texture":
                        var resPath = UnityUtil.pathConvertToUnityResource(Util.file.getDirName(path) + "/textures/" + words[1]);
                        mtl.mainTexture = Resources.Load<Texture2D>(resPath);
                        renderer.material = mtl;//念のため設定しなおす
                        break;
                    case "positionOffset":
                        ptcl.transform.localPosition = new Vector3(getFloat(1), getFloat(2), getFloat(3));
                        break;
                }
            }

            reader.Close();
        }

        private void createEntry(ref ParticleSystem ptcl, ref Transform trns, string name)
        {
            var child = UnityUtil.createObject(UnityUtil.ParentType.ROOT, name);
            child.transform.SetParent(trns);
            ptcl = child.AddComponent<ParticleSystem>();
            var m = ptcl.main;
            m.prewarm = true;
            m.startSpeed = new ParticleSystem.MinMaxCurve(0);
            var r = ptcl.GetComponent<ParticleSystemRenderer>();
            r.material = new Material(UnityEngine.Shader.Find("Custom/Premultiplied"));
        }

        private Vector3 calcEulerRotateXYZ(Vector3 euler)
        {
            var quatX = Quaternion.AngleAxis(euler.x, Vector3.left);
            var quatY = Quaternion.AngleAxis(euler.y, Vector3.up);
            var quatZ = Quaternion.AngleAxis(euler.z, Vector3.back);
            return (quatZ * quatY * quatX).eulerAngles / 180 * fPI;
        }
    }
}
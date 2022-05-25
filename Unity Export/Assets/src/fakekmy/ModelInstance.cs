using System;
using System.Linq;
using SharpKmyMath;
using System.Collections.Generic;
using UnityEngine;
using Yukar.Common;

namespace SharpKmyGfx
{
    public class ModelInstance : Drawable
    {
        public static float sBoundsExtentScale = 1.5f;
        public ModelData template;
        public GameObject instance;
        private GameObject animatorObj;
        private DynamicMotion dm;
        private Dictionary<String, Yukar.Engine.ModifiedModelData.MotionInfo> separatedMotions =
            new Dictionary<String, Yukar.Engine.ModifiedModelData.MotionInfo>();
        private Dictionary<int, DrawInfo> drawInfos = new Dictionary<int, DrawInfo>();
        private List<int> materialIndexes = new List<int>();
        public const string ANIMATOR_OBJ_NAME = "__AnimatorObj__";

        public ModelInstance(ModelData model)
        {
            if (model.obj == null)
            {
                instance = Yukar.Common.UnityUtil.createObject(Yukar.Common.UnityUtil.ParentType.TEMPLATE, model.path);
                template = model;
                template.refcount++;
                return;
            }

            if (UnityEntry.IsImportMapScene())
                instance = UnityUtil.findPlacedModelObject(model.path);

            if (instance == null)
                instance = model.instantiate(Yukar.Common.UnityUtil.ParentType.OBJECT);
            instance.SetActive(true);
            if (!UnityEntry.IsImportMapScene())
                instance.isStatic = false;
            template = model;
            template.refcount++;

            int meshIndex = 0;
            Renderer mesh;
            while ((mesh = template.getMesh(meshIndex++)) != null)
            {
                if (UnityEntry.IsImportMapScene())
                {
                    materialIndexes.Add(template.getMaterialIndex(mesh.sharedMaterial.name, 0, false));
                }
                else
                {
                    materialIndexes.Add(template.getMaterialIndex(mesh.material.name, 0, false));
                }
            }

            // 最初から非表示になっているrendererにはタグをつけておく
            var rendererList = instance.GetComponentsInChildren<UnityEngine.Renderer>();
            foreach (var r in rendererList) if (!r.enabled) r.tag = "Finish";

            // Boundsを拡張する
            if (!UnityEntry.IsImportMapScene())
            {
                var meshes = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var smr in meshes)
                {
                    var bounds = smr.localBounds;
                    bounds.extents *= sBoundsExtentScale;
                    smr.localBounds = bounds;
                }
            }
        }

        public void setVisibility(bool v)
        {
            var renderer = instance.GetComponentInChildren<UnityEngine.Renderer>();
            if (renderer == null || (renderer.tag != "Finish" && renderer.enabled == v))
                return;

            var rendererList = instance.GetComponentsInChildren<UnityEngine.Renderer>();
            foreach (var r in rendererList) if (r.tag != "Finish") r.enabled = v;
        }

        internal DrawInfo getDrawInfo(int i)
        {
            if (drawInfos.ContainsKey(i))
                return drawInfos[i];

            if (i == 0 || template.getMesh(i) != null)
            {
                var info = new DrawInfo();
                info.indexCount = i;
                info.drawable = this;

                drawInfos.Add(i, info);

                return info;
            }
            else
            {
                return null;
            }
        }

        internal void setL2W(Matrix4 m)
        {
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

        internal void update(float elapsed)
        {
            var meshes = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var mesh in meshes)
            {
                Material material = null;
                if (UnityEntry.IsImportMapScene())
                {
                    material = mesh.material = new Material(mesh.sharedMaterial);
                }
                else
                {
                    material = mesh.material;
                }
                material.SetColor("_BiasColor", UnityEngine.Color.clear);
            }
        }

        public override void draw(Render scn)
        {
            var info = drawInfos.Values.FirstOrDefault();
            if (info == null || info.blendType != BLENDTYPE.kALPHA)
                return;

            var a = info.color.a;
            var r = info.color.r;
            var g = info.color.g;
            var b = info.color.b;
            var color = new UnityEngine.Color(r * a, g * a, b * a, 0);
            //var color = UnityUtil.convertToUnityColor(info.color);

            //Debug.Log("" + info.color.a + "," + info.color.r + "," + info.color.g + "," + info.color.b + ",");
            //Debug.Log("" + color.a + "," + color.r + "," + color.g + "," + color.b + ",");

            var meshes = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var mesh in meshes)
            {
                Material material = null;
                if (UnityEntry.IsImportMapScene())
                {
                    material = mesh.material = new Material(mesh.sharedMaterial);
                }
                else
                {
                    material = mesh.material;
                }
                material.SetColor("_BiasColor", color);
            }
        }

        internal void playMotion(string name, float blendTime)
        {
            name = name.ToLower();

            if (dm == null)
            {
                // アニメータがまだセットされていない時はモーション出来ない
                if (animatorObj == null)
                    return;

                dm = animatorObj.AddComponent<DynamicMotion>();
            }

            if (separatedMotions.ContainsKey(name))
            {
                // 動作キャラのオブジェクト名とモーション名からモーションファイルのパスを生成
                var motionPath = "";
                motionPath = template.path.Replace(
                    instance.name.Replace(ModelData.MODELNAME_PREFIX_CLONED, "").ToLower() + ".fbx", "")
                    + "motion/" + separatedMotions[name].name;
                motionPath = UnityUtil.pathConvertToUnityResource(motionPath);
                //Debug.Log("play motion " + motionPath);

                if (!dm.playMotion(motionPath, separatedMotions[name]))
                {
                    motionPath = template.path.Replace(
                        instance.name.Replace(ModelData.MODELNAME_PREFIX_CLONED, "") + ".fbx", "")
                        + "motion/" + separatedMotions[name].name;
                    motionPath = UnityUtil.pathConvertToUnityResource(motionPath);

                    dm.playMotion(motionPath, separatedMotions[name]);
                }
            }
            else
            {
                dm.playClipMotion(this, name);
            }
        }

        internal int getMeshMaterialIndex(int didx)
        {
            if (materialIndexes.Count <= didx)
                return 0;
            return materialIndexes[didx];
        }

        internal ModelData getModel()
        {
            return template;
        }

        internal bool containsMotion(string motion)
        {
            return true;
        }

        internal Matrix4 getNodeMatrix(string name)
        {
            var child = UnityUtil.findChild(instance, name);
            if (child == null)
                return Matrix4.identity();
            var result = new Matrix4();
            result.m = child.transform.localToWorldMatrix;
            result.m.m03 *= -1;
            return result;
        }

        internal new void Release()
        {
            template.refcount--;
            if (UnityEntry.IsImportMapScene())
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
            else
            {

                UnityEngine.Object.Destroy(instance);
            }
        }

        internal void addMotion(string name, ModelData model, float start, float end, bool loop)
        {
            setAnimatorComponent(false);

        }

        internal void addMotion(string name, ModelData model, bool isLooped)
        {
            setAnimatorComponent(true);

            // motion用Dictionary生成
            // 動作名->ファイル名
            if (model.obj == null) return;
            var rename = name;
            if (name.EndsWith(".fbx")) rename = name.Replace(".fbx", "");
            if (separatedMotions.ContainsKey(rename)) return; // 重複回避
            separatedMotions[rename] = new Yukar.Engine.ModifiedModelData.MotionInfo()
            {
                name = model.obj.name.Replace(ModelData.MODELNAME_PREFIX_TEMPLATE, ""),
                loop = isLooped,
                rootName = model.obj.transform.GetChild(0).name
            };

            //UnityEngine.Debug.Log(rename + " " + separatedMotions[rename] + " " + isLooped);
        }

        internal int getMotionLoopCount()
        {
            if (dm == null)
                return 0;

            return (int)dm.getMotionLoopCount();
        }

        private void setAnimatorComponent(bool insideRoot)  // true の時はルートにアニメが影響しないよう、一段掘ってAnimatorを付与する
        {
            if (animatorObj == null)
            {
                if (insideRoot)
                {
                    // アニメータは別OBJにする
                    animatorObj = new GameObject(ANIMATOR_OBJ_NAME);
                    animatorObj.transform.SetParent(instance.transform);
                    animatorObj.transform.localScale = UnityEngine.Vector3.one;
                }
                else
                {
                    animatorObj = instance;
                }
            }

            // instance に Animator 追加
            var anim = animatorObj.GetComponent<Animator>();
            if (anim == null)
            {
                anim = animatorObj.AddComponent<Animator>();
            }

            var motionController = Resources.Load("Motion Controller", typeof(RuntimeAnimatorController)) as RuntimeAnimatorController;
            anim.runtimeAnimatorController = motionController;
        }
    }


    public class DynamicMotion : MonoBehaviour
    {
        internal string lastMotionName;
        private bool isLoop;
        private const string DEFAULT_CLIP_NAME = "Take 001";

        private Transform lastParent = null;
        private Transform lastChild;

        public bool playMotion(string motionPath, Yukar.Engine.ModifiedModelData.MotionInfo info)
        {
            // 同じモーションだったら再度再生しない
            if (lastMotionName == motionPath) return true;
            lastMotionName = motionPath;

            // ルートノードが一致しない事があるので、モーションが適用されるよう外に出す
            if (lastParent != null)
            {
                lastChild.SetParent(lastParent, false);
                lastParent = null;
            }
            var rootNode = UnityUtil.findChild(transform.parent.gameObject, info.rootName);
            if (rootNode != null)
            {
                lastParent = rootNode.transform.parent;
                lastChild = rootNode.transform;
                transform.rotation = rootNode.transform.parent.rotation;
                rootNode.transform.SetParent(transform, false);
            }

            var motion = Resources.Load(motionPath, typeof(AnimationClip)) as AnimationClip;

            if (motion != null)
            {
                motion.wrapMode = info.loop ? WrapMode.Loop : WrapMode.ClampForever;
                ChangeClip(motion);
            }
            else
            {
                //  一コマしかないモーションなどで AnimationClip がない場合があるので、その場合はポーズを直接反映してやる
                var model = Resources.Load<GameObject>(motionPath);
                if (model == null)
                {
                    // それでも見つからなかったら失敗
                    // Debug.Log("Motion : " + name + " cannot find.");
                    return false;
                }

                // まずモーションを止める
                ChangeClip(null);

                // Transformの情報をコピー
                copyTransformValue(gameObject.transform, model.transform);
            }

            return true;
        }

        private void copyTransformValue(Transform a, Transform b)
        {
            a.localPosition = b.localPosition;
            a.localRotation = b.localRotation;
            a.localScale = b.localScale;

            for (int i = 0; i < a.transform.childCount; i++)
            {
                var aChild = a.GetChild(i);
                var bChild = b.Find(aChild.name);
                if (bChild != null)
                    copyTransformValue(aChild, bChild);
            }
        }

        internal void playClipMotion(ModelInstance inst, string name)
        {
            // 同じモーションだったら再度再生しない
            if (lastMotionName == name) return;
            lastMotionName = name;

            var resPath = UnityUtil.pathConvertToUnityResource(inst.template.path);
            var clips = Resources.LoadAll<AnimationClip>(resPath) as AnimationClip[];
            var motion = clips.FirstOrDefault(x => x.name.ToLower() == name.ToLower());

            if (motion != null)
            {
                ChangeClip(motion);
            }
            // else
            // {
            //     Debug.Log("Motion : " + name + " cannot find.");
            // }
        }

        public void OnEnable()
        {
            var animator = gameObject.GetComponent<Animator>();
            if (animator == null)
                return;

            animator.SetBool("loop", isLoop);
        }

        public void ChangeClip(AnimationClip clip)
        {
            var animator = gameObject.GetComponent<Animator>();
            if (animator == null)
                return;

            var controller = getController();
            if (controller == null)
                return;

            animator.enabled = clip != null;

            if (clip == null)
                return;

            controller[DEFAULT_CLIP_NAME] = clip;

            isLoop = clip.wrapMode == WrapMode.Loop;

            if (gameObject.activeInHierarchy == false) return;
            animator.SetBool("loop", isLoop);
            animator.SetTrigger("init");

            var counter = animator.GetBehaviour<LoopCounter>();
            if (counter != null)
                counter.loopCount = -1;
        }

        private AnimatorOverrideController getController()
        {
            var animator = gameObject.GetComponent<Animator>();
            if (animator == null)
                return null;

            RuntimeAnimatorController myController = animator.runtimeAnimatorController;
            if (myController == null)
                return null;

            AnimatorOverrideController myOverrideController = myController as AnimatorOverrideController;
            if (myOverrideController == null)
            {
                myOverrideController = new AnimatorOverrideController();
                myOverrideController.runtimeAnimatorController = myController;
                animator.runtimeAnimatorController = myOverrideController;
            }

            return myOverrideController;
        }

        void OnAnimatorMove()
        {
            var animator = gameObject.GetComponent<Animator>();
            if (animator == null)
                return;

            transform.position = GetComponent<Animator>().rootPosition;
        }

        internal float getMotionLoopCount()
        {
            var animator = gameObject.GetComponent<Animator>();
            if (animator == null)
                return 0;

            var counter = animator.GetBehaviour<LoopCounter>();
            if (counter == null)
                return 0;

            return animator.GetCurrentAnimatorStateInfo(0).normalizedTime + counter.loopCount;
        }
    }
}

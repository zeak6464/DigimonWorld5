using System.Linq;
using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using SharpKmyGfx;

namespace Yukar.Common
{
    public static class UnityUtil
    {
        public enum ParentType
        {
            ROOT = 0,
            OBJECT,
            SOUND,
            TEMPLATE,
            CAMERA,

            COUNT,
        }

        public enum SceneType
        {
            MAP = 0,
            BATTLE,

            COUNT,
        }
        private static SceneType currentScene = SceneType.MAP;

        private static Transform[] parents = new Transform[(int)ParentType.COUNT];
        private static int[] sSceneBuildIndexList = new int[(int)SceneType.COUNT] { -1, -1};
        private static AsyncOperation sSceneAsyncOperation = null;


        internal static void Initialize()
        {
            if (GameObject.Find("MapScene") == null)
                return;

            parents[0] = GameObject.Find("MapScene").transform;
            parents[1] = GameObject.Find("BattleScene").transform;
            parents[2] = GameObject.Find("Sound").transform;
            parents[3] = GameObject.Find("Template").transform;
            parents[4] = GameObject.Find("Main Camera").transform;
            
            //分割が有効の場合
            if (UnityEntry.IsDivideMapScene())
            {
                //オブジェクト名の変更
                parents[(int)ParentType.ROOT].name = "sgb_dynamic_map";
                parents[(int)ParentType.OBJECT].name = "sgb_dynamic_battle";

                UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnActiveSceneChanged;
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
                UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
            }
        }

        internal static GameObject findPlacedModelObject(string path)
        {
            var list = GameObject.FindObjectsOfType<ObjectInfo>();
            var info = list.FirstOrDefault(x => x.objectType == ObjectInfo.ObjectType.Model && x.modelPath == path);
            GameObject result = null;
            if (info != null)
            {
                result = info.gameObject;
                UnityEngine.Object.DestroyImmediate(info);
            }
            return result;
        }

        internal static GameObject findTerrainObject(int index)
        {
            var list = GameObject.FindObjectsOfType<ObjectInfo>();
            var info = list.FirstOrDefault(x => x.objectType == ObjectInfo.ObjectType.Terrain && x.terrainClusterNo == index);
            GameObject result = null;
            if (info != null)
            {
                result = info.gameObject;
                UnityEngine.Object.DestroyImmediate(info);
            }
            return result;
        }

        static void OnActiveSceneChanged(UnityEngine.SceneManagement.Scene inPreScene, UnityEngine.SceneManagement.Scene inPostScene)
        {
            bool isFindSceneBuildIndexList = false;
            foreach (var sceneBuildIndex in sSceneBuildIndexList)
            {
                if (sceneBuildIndex < 0) continue;
                if (sceneBuildIndex != inPostScene.buildIndex) continue;
                isFindSceneBuildIndexList = true;
                break;
            }

            if (isFindSceneBuildIndexList)
            {
                //一時的なカメラを無効
                if (parents[(int)ParentType.CAMERA] != null)
                {
                    parents[(int)ParentType.CAMERA].gameObject.SetActive(false);
                }
                setSceneActive(false, inPreScene.buildIndex);
                setSceneActive(true, inPostScene.buildIndex);
            }
            else
            {
                //一時的なカメラを有効
                var entryScene = getEntryScene();
                if (entryScene == inPostScene.buildIndex)
                {
                    if (parents[(int)ParentType.CAMERA] != null)
                    {
                        parents[(int)ParentType.CAMERA].gameObject.SetActive(true);
                    }
                }
            }
            

        }

        static void OnSceneLoaded(UnityEngine.SceneManagement.Scene inScene, UnityEngine.SceneManagement.LoadSceneMode inMode)
        {
            var index = inScene.buildIndex;
            setSceneActive(false, index);
        }

        static void OnSceneUnloaded(UnityEngine.SceneManagement.Scene inScene)
        {
            //すでにカメラが有効になっているか確認
            bool isUseTempCamera = true;
            foreach (var sceneBuildIndex in sSceneBuildIndexList)
            {
                if (sceneBuildIndex < 0) continue;
                if (isSceneActive(sceneBuildIndex) == true)
                {
                    isUseTempCamera = false;
                    break;
                }
            }

            //一時的なカメラを有効
            if(isUseTempCamera == true
            && parents[(int)ParentType.CAMERA] != null)
            {
                parents[(int)ParentType.CAMERA].gameObject.SetActive(true);
            }
        }
        
        internal static int getEntryScene()
        {
            var entryScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("Entry");
            return entryScene.buildIndex;
        }

        internal static int getCurrentScene()
        {
            switch (currentScene)
            {
                case SceneType.MAP:
                case SceneType.BATTLE:
                    return sSceneBuildIndexList[(int)currentScene];
            }
            return -1;
        }

        internal static void moveParent(int inBuildIndex)
        {
            switch (currentScene)
            {
                case SceneType.MAP:
                    moveParent(inBuildIndex, ParentType.ROOT);
                    break;
                case SceneType.BATTLE:
                    moveParent(inBuildIndex, ParentType.OBJECT);
                    break;
            }
        }

        internal static void moveParent(int inBuildIndex, ParentType inType)
        {
            var obj = parents[(int)inType];
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(inBuildIndex);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obj.gameObject, scene);
        }

        internal static void unloadSceneAsync()
        {
            var curSceneBuildIndex = getCurrentScene();
            if (curSceneBuildIndex == -1) return;
            
            //動的オブジェクトのシーン移動
            var entryScene = getEntryScene();
            moveParent(entryScene);

            //他で使用されている場合は解放しない
            int userCnt = 0;
            foreach (var index in sSceneBuildIndexList)
            {
                if (index != curSceneBuildIndex) continue;
                userCnt++;
            }
            if(2 <= userCnt)
            {
                sSceneBuildIndexList[(int)currentScene] = -1;
                return;
            }

            //解放
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(curSceneBuildIndex);
        }

        internal static bool isUnloadScene()
        {
            var sceneBuildIndex = getCurrentScene();
            if (sceneBuildIndex < 0) return true;

            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(sceneBuildIndex);
            if (scene.isLoaded == true) return false;
            sSceneBuildIndexList[(int)currentScene] = -1;

            return true;
        }

        internal static void loadSceneAsync(Common.Rom.Map inMap)
        {
            var sceneName = inMap.mapSceneName;
            if (string.IsNullOrEmpty(sceneName))
                sceneName = inMap.guId.ToString();
            var scenePath = "Assets/map/" + sceneName + ".unity";
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);

            //すでにシーンが読み込まれているのであれば抜ける
            if (scene.isLoaded) {
                sSceneBuildIndexList[(int)currentScene] = scene.buildIndex;
                return;
            }

            //読み込まれていないのであれば読み込み
            var buildIndex = UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath(scenePath);
            sSceneAsyncOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(buildIndex, UnityEngine.SceneManagement.LoadSceneMode.Additive);
            sSceneAsyncOperation.allowSceneActivation = false;
            sSceneBuildIndexList[(int)currentScene] = buildIndex;
        }

        internal static bool isLoadScene()
        {
            if (sSceneAsyncOperation.progress < 0.9f) return false;
            sSceneAsyncOperation.allowSceneActivation = true;

            var sceneBuildIndex = getCurrentScene();
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(sceneBuildIndex);
            if (scene.isLoaded == false)return false;

            //シーン切り替え
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);

            //動的オブジェクトのシーン移動
            moveParent(sceneBuildIndex);

            //シーンのオブジェクトを検索・設定
            SharpKmyGfx.Light.refind();
            SharpKmyGfx.Render.refind();
            return true;
        }

        internal static List<GameObject> getStaticGameObject(int inBuildIndex = -1)
        {
            List<GameObject> objList = null;
            if (inBuildIndex < 0)
            {
                var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                objList = activeScene.GetRootGameObjects().ToList();
            }
            else
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(inBuildIndex);
                objList = scene.GetRootGameObjects().ToList();
            }
            objList = objList.Where(obj => (obj.name == "sgb_static")).ToList();
            return objList;
        }

        internal static bool isSceneActive(int inBuildIndex = -1)
        {
            bool res = false;
            if (0 <= inBuildIndex)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(inBuildIndex);
                if (scene.isLoaded == false)
                {
                    return false;
                }
            }

            var objList = getStaticGameObject(inBuildIndex);
            foreach (var obj in objList)
            {
                res = obj.activeSelf;
                break;
            }
            return res;
        }

        internal static void setSceneActive(bool inActive, int inBuildIndex = -1)
        {
            var objList = getStaticGameObject(inBuildIndex);
            foreach (var obj in objList)
            {
                obj.SetActive(inActive);
            }
        }


        internal static string pathConvertToUnityResource(string path, bool removeExtension = true)
        {
            if(removeExtension)
                path = GetPathWithoutExtension(path);
            path = FileUtil.toLower(path.Replace("\\", "/"));
            var resPattern = FileUtil.toLower(Catalog.sResourceDir);
            var resIndex = path.LastIndexOf(resPattern);
            if (resIndex >= 0)
            {
                resIndex += resPattern.Length;
                path = path.Substring(resIndex, path.Length - resIndex);
            }
            path = path.Replace("./", "");
            return path;
        }

        // getChildren()で使うリストをここでインスタンス化しておく
        private static List<Transform> children = new List<Transform>();
        private static List<GameObject> result = new List<GameObject>();
        //
        public static List<GameObject> getChildren(this GameObject self, bool includeInactive = false)
        {
            /*
            return self
                .GetComponentsInChildren<Transform>(includeInactive)
                .Where(c => c != self.transform)
                .Select(c => c.gameObject)
                .ToArray();
            */
            // GetComponentsInChildren<>()は, 引数に出力結果を入れるリストを与えてやることで無駄に配列を動的生成するのを防ぐことが出来る
            // ToArray()によるメモリ消費も馬鹿にならないため, ここでは配列に変換することなくリストのままreturnする
            children.Clear();
            result.Clear();
            self.GetComponentsInChildren<Transform>(includeInactive, children);
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] != self.transform)
                {
                    result.Add(children[i].gameObject);
                }
            }
            return result;
            //
        }

        public static GameObject findChild(GameObject instance, string name)
        {
            return instance
                .GetComponentsInChildren<Transform>(true)
                .Where(c => c.name == name)
                .Select(c => c.gameObject)
                .FirstOrDefault();
        }

        public static string GetPathWithoutExtension(string path)
        {
            var extension = Path.GetExtension(path);
            if (string.IsNullOrEmpty(extension))
            {
                return path;
            }
            return path.Replace(extension, string.Empty);
        }

        public static void calcTransformFromMatrix(this Transform transform, Matrix4x4 matrix)
        {
            transform.localScale = matrix.ExtractScale();
            transform.rotation = matrix.ExtractRotation();
            transform.position = matrix.ExtractPosition();
        }

        internal static GameObject createObject(ParentType parent, string name)
        {
            var result = new GameObject(name);
            setParent(parent, ref result);
            return result;
        }

        internal static void setParent(ParentType parent, ref GameObject obj)
        {
            switch (parent)
            {
                case ParentType.ROOT:
                    break;
                case ParentType.OBJECT:
                    if(currentScene == SceneType.MAP)
                        obj.transform.SetParent(parents[0]);
                    else
                        obj.transform.SetParent(parents[1]);
                    break;
                case ParentType.SOUND:
                    obj.transform.SetParent(parents[2]);
                    break;
                case ParentType.TEMPLATE:
                    obj.transform.SetParent(parents[3]);
                    break;
            }
        }

        public static Quaternion ExtractRotation(this Matrix4x4 matrix)
        {
            Vector3 forward;
            forward.x = matrix.m02;
            forward.y = matrix.m12;
            forward.z = matrix.m22;

            Vector3 upwards;
            upwards.x = matrix.m01;
            upwards.y = matrix.m11;
            upwards.z = matrix.m21;

            if (upwards == Vector3.zero) upwards = Vector3.up;
            if (forward == Vector3.zero) forward = Vector3.forward;
            return Quaternion.LookRotation(forward, upwards);
        }

        internal static void changeScene(SceneType scene, bool isPrepare = false)
        {
            if (currentScene == scene) return;

#if UNITY_IOS || UNITY_ANDROID
            UnityResolution.Up();
#endif
#if ENABLE_VR_UNITY && !UNITY_EDITOR
            UnityEngine.VR.InputTracking.Recenter();
#endif

            currentScene = scene;
            parents[0].gameObject.SetActive(scene == SceneType.MAP);
            parents[1].gameObject.SetActive(scene == SceneType.BATTLE);

            if (UnityEntry.IsDivideMapScene())
            {
                if (isPrepare == false)
                {
                    setSceneActive(false);

                    switch (scene)
                    {
                        case SceneType.MAP:
                        case SceneType.BATTLE:
                            {
                                var unitySceneBuildIndex = sSceneBuildIndexList[(int)scene];
                                if (unitySceneBuildIndex < 0) break;
                                var unityScene = UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(unitySceneBuildIndex);
                                if (unityScene.isLoaded == false) break;
                                UnityEngine.SceneManagement.SceneManager.SetActiveScene(unityScene);
                                break;
                            }
                    }

                    setSceneActive(true);
                }
                SharpKmyGfx.Light.refind();
                SharpKmyGfx.Render.refind();
            }

        }

        public static Vector3 ExtractPosition(this Matrix4x4 matrix)
        {
            Vector3 position;
            position.x = -matrix.m03;
            position.y = -matrix.m13;
            position.z = matrix.m23;
            return position;
        }

        public static Vector3 ExtractScale(this Matrix4x4 matrix)
        {
            Vector3 scale;
            scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
            scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
            scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
            return scale;
        }

        internal static Vector3 convertToUnityVector3(Microsoft.Xna.Framework.Vector3 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        internal static UnityEngine.Color convertToUnityColor(SharpKmyGfx.Color color)
        {
            return new UnityEngine.Color(color.r, color.g, color.b, color.a);
        }
    }
}

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yukar.Common.Rom;

public class CustomImporter : AssetPostprocessor
{
    static CreateScene sCreateScene = null;
    public static bool reimport;
    static List<string> sImportedAssets = new List<string>();

    public static bool IsImportMapScene {
        get {
            if (sCreateScene == null) return false;
            return sCreateScene.IsImportMapScene;
        }
    }
    public static bool IsReimportMapScene
    {
        get
        {
            if (sCreateScene == null) return false;
            return sCreateScene.mIsReimportMapScene;
        }
    }

    /// <summary>
    /// 全てのアセットのインポートが終了した際に呼ばれる
    /// </summary>
    public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath)
    {
        if (importedAssets.Count() == 0) return;

        // Unity2018では、VertexColor シェーダーがマテリアルより後にインポートされる可能性があるため、
        // あとから更に正しいシェーダーをセットする
        foreach (string asset in importedAssets)
        {
            // 拡張子のみ取得
            string type = Path.GetExtension(asset);

            if (type == ".mat")
            {
                // マテリアルを取得
                var material = AssetDatabase.LoadAssetAtPath<Material>(asset);
                if (material.shader.name == "Hidden/InternalErrorShader")
                {
                    // とりあえず全部VertexColorが使えるシェーダーにしておく
                    material.shader = Shader.Find("Custom/VertexColor");
                    material.SetFloat("_Mode", 1.0f);
                    material.SetFloat("_Cutoff", 0.1f);
                }
            }
        }

        // マップシーン生成を遅延呼び出し
        sImportedAssets.AddRange(importedAssets);
        if(sCreateScene != null
        && sCreateScene.IsImportMapScene)
        {
            return;
        }

        EditorApplication.CallbackFunction delayCall = null;
        delayCall = () =>
        {
            if (sCreateScene != null && sCreateScene.IsRun)
            {
                return;
            }
            sCreateScene = new CreateScene();
            sCreateScene.Run(sImportedAssets);
            sImportedAssets = new List<string>();
            EditorApplication.delayCall -= delayCall;
        };
        EditorApplication.delayCall += delayCall;
    }

    public static void ImportedAssets()
    {
        if (sCreateScene != null && sCreateScene.IsRun)
        {
            return;
        }
        if (sImportedAssets.Count() == 0)
        {
            sCreateScene = null;
            return;
        }
        sCreateScene = new CreateScene();
        sCreateScene.Run(sImportedAssets);
        sImportedAssets = new List<string>();
    }

    public static void CreateScenes()
    {
        string[] files = System.IO.Directory.GetFiles(
            CreateScene.RESOURCES_MAP_PATH, "*.bytes",
            System.IO.SearchOption.TopDirectoryOnly);
        var assets = new List<string>(files);

        sCreateScene = new CreateScene();
        sCreateScene.EnableDebugLog = true;
        sCreateScene.Run(assets, false);
    }


    ////////////////////////////////////////////////////////////////////
    // インポート直前に動くスクリプト

    /// <summary>
    /// この関数をサブクラスに追加してモデル (.fbx、.mb ファイル等) のインポート完了の直前に通知を取得します
    /// </summary>
    public void OnPreprocessAnimation()
    {
#if UNITY_2018_2_OR_NEWER
        var modelImporter = assetImporter as ModelImporter;
        createDefBasedClips(modelImporter);
#endif
    }

    private void createDefBasedClips(ModelImporter modelImporter)
    {
        // モーション定義がある場合はそれを読み込む
        var defPath = string.Format("{0}/{1}.def.txt",
            Path.GetDirectoryName(assetPath),
            Yukar.Common.Util.file.getFileNameWithoutExtension(assetPath));
        var def = new TinyDefLoader(defPath);
        if (def.motions.Count > 0 && modelImporter.importedTakeInfos.Length > 0)
        {
            var clips = new List<ModelImporterClipAnimation>();
            foreach (var motion in def.motions)
            {
                var take = modelImporter.importedTakeInfos.First();
                var clip = new ModelImporterClipAnimation();
                clip.takeName = take.name;
                clip.name = motion.name;
                clip.firstFrame = motion.start * take.sampleRate / 60;
                clip.lastFrame = motion.end * take.sampleRate / 60;
                if (clip.firstFrame == clip.lastFrame)
                    clip.firstFrame += 0.1f;
                //Debug.Log(clip.name + " : " + clip.firstFrame + ", " + clip.lastFrame);
                clip.wrapMode = motion.loop ? WrapMode.Loop : WrapMode.ClampForever;
                clips.Add(clip);
            }
            modelImporter.clipAnimations = clips.ToArray();
        }
    }

    /// <summary>
    /// この関数をサブクラスに追加してオーディオクリップのインポート完了の直前に通知を取得します
    /// </summary>
    public void OnPreprocessAudio()
    {
    }
    /// <summary>
    /// この関数をサブクラスに追加してモデル (.fbx、.mb ファイル等) のインポート完了の直前に通知を取得します
    /// </summary>
    public void OnPreprocessModel()
    {
        var modelImporter = assetImporter as ModelImporter;

        if (IsOutsideResources(modelImporter.assetPath))
            return;

        if (reimport)
        {
            // スケールをKMY準拠にする
#if UNITY_2017_1_OR_NEWER
            modelImporter.globalScale = 0.01f;
            modelImporter.useFileScale = false;
#if UNITY_2017_3_OR_NEWER
            modelImporter.materialLocation = 0;
#endif
#else
            modelImporter.globalScale = 0.01f / modelImporter.fileScale;
#endif

#if UNITY_2018_2_OR_NEWER
#else
            createDefBasedClips(modelImporter);
#endif

            reimport = false;
            return;
        }

        // モデルの子ノードはマテリアル名を含むようにする
        modelImporter.materialName = ModelImporterMaterialName.BasedOnModelNameAndMaterialName;
#if UNITY_2017_1_OR_NEWER
        modelImporter.importCameras = false;
#endif

        reimport = true;
        modelImporter.SaveAndReimport();
    }

    private bool IsOutsideResources(string assetPath)
    {
        assetPath = assetPath.ToLower();

        if (assetPath.StartsWith("assets/resources/samples") ||
            assetPath.StartsWith("assets/resources/res") ||
            (assetPath.StartsWith("assets/map") && assetPath.EndsWith("/tex.png")))
            return false;

        return true;
    }

    /// <summary>
    /// Add this function in a subclass to get a notification just before a SpeedTree asset (.spm file) is imported.
    /// </summary>
    public void OnPreprocessSpeedTree()
    {
        // if (assetPath.Contains("@")) {
        //  var modelImporter : ModelImporter = assetImporter;
        //  modelImporter.importMaterials = false;
        // }
    }
    /// <summary>
    /// この関数をサブクラスに追加してテクスチャのインポート完了の直後に通知を取得します
    /// </summary>
    public void OnPreprocessTexture()
    {
        if (IsOutsideResources(assetPath))
            return;

        // 2D系ファイル用の画像フォルダ以下にインポートした場合はnpotを解除する
        if (!assetPath.Contains("/res/character/3D/") &&
            !assetPath.Contains("/res/mapobject/"))
        {
            var textureImporter = assetImporter as TextureImporter;
            textureImporter.npotScale = TextureImporterNPOTScale.None;
            textureImporter.spriteImportMode = SpriteImportMode.Single;
            textureImporter.mipmapEnabled = false;
        }

        // 地形用テクスチャは補間を切った上でreadbleにする
        if (assetPath.ToLower().StartsWith("assets/map/"))
        {
            var textureImporter = assetImporter as TextureImporter;
            textureImporter.isReadable = true;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.maxTextureSize = 4096;
            textureImporter.filterMode = FilterMode.Point;
        }

        // マップチップ・ウィンドウ用の画像フォルダ以下にインポートした場合は更にreadbleにする
        if (assetPath.Contains("/res/map/") ||
            assetPath.Contains("/res/window/") ||
            assetPath.Contains("/res/battle/"))
        {
            var textureImporter = assetImporter as TextureImporter;
            textureImporter.isReadable = true;
        }

        // 2Dキャラクター・アイコン用の画像フォルダ以下にインポートした場合は圧縮を解除・対応サイズの変更する
        if (assetPath.Contains("/res/character/2D/") ||
            assetPath.Contains("/res/icon/") ||
            assetPath.Contains("/res/image/") ||
            assetPath.Contains("/res/window/") ||
            assetPath.Contains("/res/system/") ||
            assetPath.Contains("/res/battle/") ||
            assetPath.Contains("/res/effect/"))
        {
            var textureImporter = assetImporter as TextureImporter;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.maxTextureSize = 4096;
        }

        // 2Dキャラクター用・イメージ・ウィンドウ・システムはクロップを解除する
        if (assetPath.Contains("/res/character/2D/") ||
            assetPath.Contains("/res/icon/") ||
            assetPath.Contains("/res/image/") ||
            assetPath.Contains("/res/window/") ||
            assetPath.Contains("/res/battle/"))
        {
            var textureImporter = assetImporter as TextureImporter;
            textureImporter.wrapMode = TextureWrapMode.Clamp;
        }
    }

    /// <summary>
    /// ソースマテリアルをフィードします。
    /// </summary>
    public Material OnAssignMaterialModel(Material material, Renderer renderer)
    {
        if (IsOutsideResources(assetPath))
        {
            return null;
        }

        //作成して保存するマテリアルのパス
        var baseDir = Path.GetDirectoryName(assetPath);
        var materialDir = string.Format("{0}/Materials", baseDir);
        if(!Directory.Exists(materialDir))
            Directory.CreateDirectory(materialDir);

        var materialPath = string.Format("{0}/{1}-{2}.mat",
            materialDir, Yukar.Common.Util.file.getFileNameWithoutExtension(assetPath), material.name);

        // Find if there is a material at the material path
        // Turn this off to always regeneration materials
        if (File.Exists(materialPath))
            return AssetDatabase.LoadAssetAtPath<Material>(materialPath);

        // defのパス
        var defPath = string.Format("{0}/{1}.def.txt",
            baseDir,
            Yukar.Common.Util.file.getFileNameWithoutExtension(assetPath));

        //Debug.Log("assetPath:" + assetPath);
        //Debug.Log("materialPath:" + materialPath);
        //Debug.Log("defPath:" + defPath);

        var def = new TinyDefLoader(defPath);
        // とりあえず全部VertexColorが使えるシェーダーにしておく
        material.shader = Shader.Find("Custom/VertexColor");
        material.SetFloat("_Mode", 1.0f);
        material.SetFloat("_Cutoff", 0.1f);

        // defの定義によって、マテリアルを切り替える
        if (def.materials.ContainsKey(material.name))
        {
            var mtl = def.materials[material.name];

            switch (mtl.blend)
            {
                case 2: // 加算合成
                    material.shader = Shader.Find("Mobile/Particles/Additive");
                    break;
                case 4: // 乗算合成
                    material.shader = Shader.Find("Mobile/Particles/Multiply");
                    break;
            }

            // blendが0でもライティングしないシェーダーが指定されていたら、それに従う
            if (mtl.blend == 0 && mtl.shader == "legacy_tex")
            {
                material.shader = Shader.Find("Mobile/Unlit (Supports Lightmap)");
            }
        }
        
        const string AmbientTexSuffix = "_ambient";
        var texturesDir = Path.Combine(baseDir, "textures");

        if (material.mainTexture == null)
        {
            // モデル内で指定されているテクスチャのファイル名と実際のテクスチャのファイル名が異なる場合、texturesフォルダ内のテクスチャを割り当てる
            if (Directory.Exists(texturesDir))
            {
                foreach (var item in Directory.GetFiles(texturesDir))
                {
                    if ((Path.GetExtension(item).ToLower() != ".meta") && !Path.GetFileNameWithoutExtension(item).EndsWith(AmbientTexSuffix))
                    {
                        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture>(item);

                        if (material.mainTexture != null)
                        {
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            // アンビエント用のテクスチャを誤って適用してしまっているときはアンビエントじゃないやつを探し直す
            if (material.mainTexture.name.EndsWith(AmbientTexSuffix))
            {
            	var notAmbTexName = material.mainTexture.name.Substring(0, material.mainTexture.name.Length - AmbientTexSuffix.Length);
                var notAmbTex = AssetDatabase.LoadAssetAtPath<Texture>(texturesDir + "/" + notAmbTexName + ".png");
                if (notAmbTex != null)
                {
                    material.mainTexture = notAmbTex;
                }
            }
        }

        //シェーダーを変更したマテリアルをmaterialPathに保存する
        AssetDatabase.CreateAsset(material, materialPath);
        //Debug.Log(string.Format("CustomModelImporterの処理により、以下の場所に[{1}]シェーダーのマテリアルを作成しました。\n{0}", materialPath, material.shader.name));
        return material;
    }
    /// <summary>
    /// Handler called when asset is assigned to a different asset bundle.
    /// </summary>
    public void OnPostprocessAssetbundleNameChanged(string assetPath, string previousAssetBundleName, string newAssetBundleName)
    {

    }
    /// <summary>
    /// この関数をサブクラスに追加してオーディオクリップがインポート完了した時に通知を取得します
    /// </summary>
    public void OnPostprocessAudio(AudioClip audio)
    {

    }
    /// <summary>
    /// インポートファイルで少なくとも一つのユーザープロパティーがアタッチされた各々のゲームオブジェクトごとに呼び出されます
    /// </summary>
    public void OnPostprocessGameObjectWithUserProperties(GameObject go, string[] propNames, System.Object[] values)
    {

    }
    /// <summary>
    /// この関数をサブクラスに追加してモデルのインポートが完了したときに通知を取得します
    /// </summary>
    public void OnPostprocessModel(GameObject model)
    {
        // インポートのタイミングで各オブジェクトのstaticにチェックを入れる
        model.isStatic = true;
        //
    }
    /// <summary>
    /// Add this function in a subclass to get a notification when a SpeedTree asset has completed importing.
    /// </summary>
    public void OnPostprocessSpeedTree(GameObject treeModel)
    {

    }
    /// <summary>
    /// この関数をサブクラスに追加してテクスチャのインポート完了の直後に通知を取得します
    /// </summary>
    public void OnPostprocessTexture(Texture2D texture)
    {
    }
}

public class CreateScene
{
    public static readonly string RESOURCES_MAP_PATH = "assets/resources/map/";
    public bool mIsReimportMapScene = false;
    bool mIsImportMapScene = false;
    bool mIsRun = false;
    public bool IsImportMapScene { get { return mIsImportMapScene; } }
    public bool IsRun { get { return mIsRun; } }
    public bool EnableDebugLog = false;

    public CreateScene()
    {
    }

    public bool Run(List<string> inImportedAssets, bool inIsCallbackUpdate = true)
    {
        if (EnableDebugLog)
        {
            Debug.Log("ImportMapScene Run Count:" + inImportedAssets.Count);
            string msg = "";
            foreach (var str in inImportedAssets)
            {
                msg += str + "\n";
            }
            Debug.Log(msg);

        }
        if (this.mIsRun) return false;
        this.mIsRun = true;

        var create = createScene(inImportedAssets);
        if (inIsCallbackUpdate)
        {
            EditorApplication.CallbackFunction update = null;
            update = () =>
            {
                if (create.MoveNext()) return;
                //while (create.MoveNext());
                EditorApplication.update -= update;
                this.mIsRun = false;
                CustomImporter.ImportedAssets();
            };
            EditorApplication.update += update;
        }
        else
        {
            while (create.MoveNext()) ;
        }
        return true;
    }

    private System.Collections.IEnumerator Refrash()
    {
        //Reimportした際に内部データと齟齬が出ないようする
        int waitCnt = 0;
        while (++waitCnt < 4) yield return null;
        AssetDatabase.Refresh();
        waitCnt = 0;
        while (++waitCnt < 2) yield return null;
    }

    private System.Collections.IEnumerator createScene(List<string> inImportedAssets)
    {
        System.Collections.IEnumerator coroutine;
        const string BYTES_SUFFIX = ".bytes";

        if (UnityEntry.IsDivideMapScene() == false) yield break;

        List<string> importedAssets = new List<string>();
        inImportedAssets.Where(path => path.ToLower().StartsWith(RESOURCES_MAP_PATH))
            .Select(path => path.ToLower())
            .ToList().ForEach(path =>
            {
                if (path.EndsWith(BYTES_SUFFIX) == false) return;

                importedAssets.Add(path);
            }
            );

        if (importedAssets.Count == 0) yield break;

        coroutine = Refrash();
        while (coroutine.MoveNext()) yield return null;

        if (EnableDebugLog)
            Debug.Log("ImportMapScene Start " + inImportedAssets.Count());
        this.mIsImportMapScene = true;
        EditorUtility.DisplayProgressBar("Create MapScene", "", 0);

        // マップファイルがあったら予めシーンとして構築する
        UnityEntry.InitializeDir();

        SharpKmyGfx.Render.InitializeRender();
        Yukar.Common.FileUtil.initialize();
        Yukar.Engine.Graphics.SetCommonResourceDir(Yukar.Common.Catalog.sCommonResourceDir);

        Yukar.Common.Catalog.createDlcList(false);
        var catalog = new Yukar.Common.Catalog();
        var mapDic = new Dictionary<Guid, string>();

        foreach (var importPath in importedAssets)
        {
            var path = importPath.Substring(0, importPath.Length - BYTES_SUFFIX.Length);

            // マップファイル名を取得
            catalog.load(Yukar.Common.Util.file.getFileStream(path));
            var map = catalog.findFirstItem(typeof(Yukar.Common.Rom.Map)) as Yukar.Common.Rom.Map;
            if (map == null)
                continue;

            //生成リストに追加
            var filename = Yukar.Engine.MapData.convertValidName(map);
            filename = Application.dataPath + "/map/" + filename;
            if (mapDic.ContainsKey(map.guId) == false)
            {
                mapDic.Add(map.guId, path);
            }

            catalog.clear(false);
        }

        if (0 < mapDic.Count())
        {
            //各シーン読み込み・保存
            catalog.load(false);
            Yukar.Engine.Graphics.Initialize(catalog.getGameSettings().gameFont, true);
            Yukar.Engine.Graphics.ClearResourceServer();

            int index = 0;
            foreach (var item in mapDic)
            {
                index++;
                // マップを取得
                var map = catalog.getItemFromGuid(item.Key, false) as Yukar.Common.Rom.Map;

                if (map == null)
                    continue;

                if (EnableDebugLog)
                    Debug.Log("ImportMapScene MapName:" + map.name);
                EditorUtility.DisplayProgressBar("Create MapScene", map.name, (float)(index) / (float)mapDic.Count());

                coroutine = createScene(map.name + "_" + map.guId.ToString(), map);

                int progressCnt = 0;
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                while (coroutine.MoveNext())
                {
                    if (sw.ElapsedMilliseconds < 1000) continue;
                    progressCnt++;
                    sw.Reset(); sw.Start();
                    string msg = map.name + " ";
                    for (int i1 = 0; i1 < progressCnt; ++i1) msg += ".";
                    if (5 <= progressCnt) progressCnt = 0;
                    EditorUtility.DisplayProgressBar("Create MapScene", msg, (float)(index) / (float)mapDic.Count());
                    //yield return null;
                }
            }


            catalog.clear(false);
        }

        EditorUtility.ClearProgressBar();
        EditorSceneManager.OpenScene("assets/Entry.unity");

        if (EnableDebugLog)
            Debug.Log("ImportMapScene End " + inImportedAssets.Count());

        coroutine = Refrash();
        while (coroutine.MoveNext()) yield return null;

        //登録してるシーンの表示
        if (EnableDebugLog)
        {
            string log = "ImportMapScene Scenes \r\n";
            foreach (var scene in EditorBuildSettings.scenes)
            {
                log += scene.guid + "\t\t : " + scene.path + "\r\n";
            }
            Debug.Log(log);
        }

        this.mIsImportMapScene = false;
    }

    private System.Collections.IEnumerator createScene(string inSceneName, Yukar.Common.Rom.Map data)
    {
        var filename = Application.dataPath + "/map/";
        filename = Yukar.Engine.MapData.convertValidName(data);
        var dir = "assets/map/";
        var path = dir + filename;

        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        Scene scene;

        if (File.Exists(path))
        {
            //シーン更新
            scene = EditorSceneManager.OpenScene(path);

            prepareSetupScene(scene);

            mIsReimportMapScene = true;
            var coroutine = applyChangeMapScene(path, data, scene);
            while (coroutine.MoveNext()) yield return null;
            mIsReimportMapScene = false;
        }
        else
        {
            //シーン生成・設定
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

            var coroutine = setupNewMapScene(path, data, scene);
            while (coroutine.MoveNext()) yield return null;
        }

        var scenes = EditorBuildSettings.scenes;
        var newSettings = new EditorBuildSettingsScene(path, true);
        if (!Array.Exists(scenes, x => x.guid == newSettings.guid))
        {
            ArrayUtility.Add(ref scenes, newSettings);
        }

        EditorBuildSettings.scenes = scenes;
    }

    private void prepareSetupScene(Scene scene)
    {
        // 一度 sgb_static のオブジェクトを全て外に出す
        var staticObj = scene.GetRootGameObjects()[0];
        while (staticObj.transform.childCount > 0)
        {
            staticObj.transform.GetChild(0).SetParent(staticObj.transform.parent);
        }
        UnityEngine.Object.DestroyImmediate(staticObj);
    }

    private System.Collections.IEnumerator applyChangeMapScene(string path, Map data, Scene scene)
    {
        //マップオブジェクトの作成
        var mapData = new Yukar.Engine.MapData();

        var coroutine = mapData.setRom(data);
        while (coroutine.MoveNext()) yield return null;

        SharpKmyGfx.Render.InitializeCamera();
        var render = SharpKmyGfx.Render.getDefaultRender();
        mapData.afterViewPositionFixProc(render);
        render.addDrawable(mapData);

        //マップの設定
        {
            // アセット用ディレクトリの作成
            var mapAssetDir = path.Substring(0, path.Length - ".unity".Length) + "/";
            if (!Directory.Exists(mapAssetDir))
            {
                Directory.CreateDirectory(mapAssetDir);
            }

            var drawInfo = mapData.GetDrawInfo();
            Material mat = null;

            if (0 < drawInfo.terrains.Count)
            {
                var chipInfo = mapData.chipSetInfo;
                var gameobject = drawInfo.terrains[0].GetGameObject();
                var terrain = gameobject.GetComponent<AnimationTerrain>();
                if(terrain == null)
                    terrain = gameobject.AddComponent<AnimationTerrain>();

                var count = chipInfo.chipVariations.Count;
                terrain.ChipVariations = new AnimationTerrain_ChipVariations[count];
                for (int i1 = 0; i1 < count; ++i1)
                {
                    var info = chipInfo.chipVariations[i1];
                    var chipVariation = terrain.ChipVariations[i1] = new AnimationTerrain_ChipVariations();
                    var t = chipInfo.t_temp[i1];
                    chipVariation.TextureSize.x = t.getWidth();
                    chipVariation.TextureSize.y = t.getHeight();
                    chipVariation.ColorData = new uint[info.srcdata.Count()];
                    Array.Copy(info.srcdata, chipVariation.ColorData, info.srcdata.Count());
                    chipVariation.UvList = new Vector2[info.uvlist.Count];
                    for (int i2 = 0; i2 < info.uvlist.Count; ++i2)
                    {
                        chipVariation.UvList[i2].x = info.uvlist[i2].x;
                        chipVariation.UvList[i2].y = info.uvlist[i2].y;
                    }
                }

                // テクスチャの保存
                var mesh = gameobject.GetComponent<Renderer>();
                mat = mesh.sharedMaterial;
                var texPath = mapAssetDir + "tex.png";
                var texture2D = convertFromTexture(mat.mainTexture);
                File.WriteAllBytes(texPath, texture2D.EncodeToPNG());
                AssetDatabase.ImportAsset(texPath);
                var texturePng = AssetDatabase.LoadAssetAtPath(texPath, typeof(Texture2D)) as Texture2D;
                mat.mainTexture = texturePng;

                // マテリアルの保存
                var loadedMat = AssetDatabase.LoadAssetAtPath<Material>(mapAssetDir + "mat.mat");
                if (loadedMat == null)
                {
                    // 無かったら作る
                    AssetDatabase.CreateAsset(mat, mapAssetDir + "mat.mat");
                }
                else
                {
                    EditorUtility.SetDirty(loadedMat);
                }
            }

            int idx = 0;
            foreach (var terrain in drawInfo.terrains)
            {
                //var chipInfo = mapData.chipSetInfo;
                var gameobject = terrain.GetGameObject();

                // クラスタ番号の保存
                var objInfo = gameobject.GetComponent<ObjectInfo>();
                if (objInfo == null)
                    objInfo = gameobject.AddComponent<ObjectInfo>();
                objInfo.objectType = ObjectInfo.ObjectType.Terrain;
                objInfo.terrainClusterNo = idx + 1;

                // マテリアルの適用
                var rnd = gameobject.GetComponent<MeshRenderer>();
                rnd.material = mat;

                // メッシュの保存
                var mesh = gameobject.GetComponent<MeshFilter>();
                AssetDatabase.CreateAsset(mesh.sharedMesh, mapAssetDir + "mesh_" + idx + ".asset");

                idx++;
            }
        }

        //ゲームオブジェクトの取得
        Dictionary<GameObject, Yukar.Engine.MapObjectInstance> gameObjectInstances = new Dictionary<GameObject, Yukar.Engine.MapObjectInstance>();
        int index = 0;
        while (true)
        {
            var mapObj = mapData.getMapObjectInstance(index++);
            if (mapObj == null) break;
            if (mapObj.minst == null) continue;
            if (mapObj.minst.inst == null) continue;
            if (mapObj.minst.inst.instance == null) continue;

            var gameObject = mapObj.minst.inst.instance;
            if (gameObjectInstances.ContainsKey(gameObject)) continue;
            gameObjectInstances.Add(gameObject, mapObj);
        }

        //ゲームオブジェクトの各種設定
        foreach (var modelObj in gameObjectInstances)
        {
            var mapObj = modelObj.Value;
            var gameObject = modelObj.Key;

            // パスの保存
            var objInfo = gameObject.GetComponent<ObjectInfo>();
            if (objInfo == null)
                objInfo = gameObject.AddComponent<ObjectInfo>();
            objInfo.objectType = ObjectInfo.ObjectType.Model;
            objInfo.modelPath = Yukar.Common.FileUtil.GetFullPath(mapObj.mo.path);

            //子ゲームオブジェクトも含める
            List<GameObject> gameObjectAllList = new List<GameObject>();
            gameObjectAllList.Add(gameObject);
            for (int i1 = 0; i1 < gameObject.transform.childCount; ++i1)
            {
                gameObjectAllList.Add(gameObject.transform.GetChild(i1).gameObject);
            }

            //ビルボード
            if (mapObj.mo != null && mapObj.mo.isBillboard &&
                gameObject.GetComponent<BillBoard>() == null)
            {
                gameObject.isStatic = false;
                gameObject.AddComponent<BillBoard>();
            }

            //アニメーション
            if (mapObj.minst != null
            && mapObj.minst.modifiedModel != null)
            {
                var modifiedModel = mapObj.minst.modifiedModel;

                //UV
                foreach (var obj in gameObjectAllList)
                {
                    var mesh = obj.GetComponent<Renderer>();
                    if (mesh == null) continue;
                    var material = mesh.sharedMaterial;
                    if (material == null) continue;
                    var idx = mapObj.minst.inst.template.getMaterialIndex(material.name);

                    if (idx >= Yukar.Engine.ModifiedModelData.MAX_MATERIAL)
                        continue;

                    if (modifiedModel.uspeed[idx] == 0
                    && modifiedModel.vspeed[idx] == 0
                    && modifiedModel.stopanimU[idx] == 0
                    && modifiedModel.stopanimV[idx] == 0
                    && modifiedModel.stopanimFrames[idx] == 0
                    && modifiedModel.stopanimInterval[idx] == 0
                    && modifiedModel.stopanimTime[idx] == 0)
                    {
                        continue;
                    }

                    var animationUV = obj.GetComponent<AnimationUV>();
                    if (animationUV == null)
                        animationUV = obj.AddComponent<AnimationUV>();
                    animationUV.IsStopAnimation = (modifiedModel.stopanimFrames[idx] != 0);

                    animationUV.StopAnimationU = modifiedModel.stopanimU[idx];
                    animationUV.StopAnimationV = modifiedModel.stopanimV[idx];
                    animationUV.StopAnimationFrames = modifiedModel.stopanimFrames[idx];
                    animationUV.StopAnimationInterval = modifiedModel.stopanimInterval[idx];

                    animationUV.SpeedUV.x = modifiedModel.uspeed[idx];
                    animationUV.SpeedUV.y = modifiedModel.vspeed[idx];
                }

                //AnimationMotion
                if (0 < modifiedModel.motions.Count)
                {
                    var animator = gameObject.GetComponent<Animator>();
                    var motion = gameObject.GetComponent<AnimationMotion>();
                    if (animator != null)
                    {
                        var isLoop = animator.GetBool("loop");
                        if(motion == null)
                            motion = gameObject.AddComponent<AnimationMotion>();
                        motion.IsLoop = isLoop;
                    }
                }

                //DynamicMotion
                {
                    var motion = gameObject.GetComponent<SharpKmyGfx.DynamicMotion>();
                    if (motion != null)
                    {
                        GameObject.DestroyImmediate(motion);
                    }
                }
            }
        }

        if (!mIsReimportMapScene)
        {
            //ライトの設定
            var light = mapData.getLight();
            if (light != null) light.setPosture(mapData.getLightMatrix());

            //環境エフェクトの設定
            {
                var effect = mapData.GetEnvironmentEffect();
                if (effect != null && effect.env != null)
                {
                    var gameObject = effect.env.GetGameObject();
                    var particleHandler = gameObject.GetComponent<SharpKmyGfx.ParticleHandler>();
                    if (particleHandler != null)
                    {
                        GameObject.DestroyImmediate(particleHandler);
                    }

                    gameObject.AddComponent<EnvironmentEffect>();
                }
            }

            //スカイボックスの設定
            {
                var skyDrawer = mapData.GetSkyDrawer();
                if (skyDrawer != null)
                {
                    foreach (var model in skyDrawer.skyModelList)
                    {
                        var gameObject = model.inst.instance;
                        gameObject.AddComponent<SkyboxPosition>();

                        var meshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();

                        if (meshRenderers != null)
                        {
                            for (int i = 0; i < meshRenderers.Count(); i++)
                            {
                                if ((meshRenderers.Count() == 1) || (meshRenderers[i].gameObject.name == "sky"))
                                {
                                    // 天球
                                    meshRenderers[i].sharedMaterial.shader = Shader.Find("Custom/SkyboxSphare");
                                }
                                else
                                {
                                    // 天球以外のオブジェクト (太陽など)
                                    meshRenderers[i].sharedMaterial.shader = Shader.Find("Custom/SkyboxObject");
                                }
                            }
                        }
                    }
                }
            }

            // フォグの設定
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = new Color(mapData.mapRom.fogColor[0], mapData.mapRom.fogColor[1], mapData.mapRom.fogColor[2], mapData.mapRom.fogColor[3]);
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogStartDistance = 20;
                RenderSettings.fogEndDistance = 70;

                var mainCameraObject = GameObject.Find("Main Camera");

                if (mainCameraObject != null)
                {
                    mainCameraObject.AddComponent<Fog>();

                    // マップ切替時にゴミが出るのでカメラ調整
                    mainCameraObject.transform.localPosition = new Vector3(-mapData.mapRom.Width / 2, 3, -3);

                    // 上記と同じくゴミが出るのでアングルを戻しておく（LoadMapで180などおかしな値になっているため）
                    mainCameraObject.GetComponent<Camera>().fieldOfView = 60;
                }
            }
        }

        //ルートの設定
        var objList = scene.GetRootGameObjects();
        var staticObj = new GameObject();
        staticObj.name = "sgb_static";
        foreach (var obj in objList)
        {
            if (obj.name.StartsWith("(Template)"))  // テンプレートはもう不要なのでここで削除する
                UnityEngine.Object.DestroyImmediate(obj);
            else
                obj.transform.SetParent(staticObj.transform);
        }

        //保存
        AssetDatabase.Refresh();
        EditorSceneManager.SaveScene(scene, path);
        yield return null;

        // マップデータの解放(ノードが全て撤去されるので保存後に呼び出すのが必須)
        mapData.Destroy();
        Yukar.Engine.Graphics.sInstance.mModelDictionary.Clear();
        SharpKmy.Entry.gfxMemoryCleanup();
    }

    private System.Collections.IEnumerator setupNewMapScene(string path, Map data, Scene scene)
    {
        RenderSettings.reflectionIntensity = 0;
        Lightmapping.realtimeGI = false;
        Lightmapping.bakedGI = false;

        //プレハブから複製
        {
            var files = System.IO.Directory.GetFiles("Assets/src/map/AddAtCreateMap", "*.prefab", System.IO.SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var preObj = AssetDatabase.LoadAssetAtPath<GameObject>(file);
                if (preObj == null) continue;
                var gameObj = GameObject.Instantiate(preObj);
                gameObj.name = preObj.name;
                gameObj.transform.SetSiblingIndex(0);
            }
        }

        var coroutine = applyChangeMapScene(path, data, scene);
        while (coroutine.MoveNext()) yield return null;
    }

    private Texture2D convertFromTexture(Texture mainTexture)
    {
        var texture2D = new Texture2D(mainTexture.width, mainTexture.height, TextureFormat.RGBA32, false);

        RenderTexture currentRT = RenderTexture.active;

        RenderTexture renderTexture = new RenderTexture(mainTexture.width, mainTexture.height, 32);
        // mainTexture のピクセル情報を renderTexture にコピー
        Graphics.Blit(mainTexture, renderTexture);

        // renderTexture のピクセル情報を元に texture2D のピクセル情報を作成
        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();

        RenderTexture.active = currentRT;

        return texture2D;
    }
}

#endif

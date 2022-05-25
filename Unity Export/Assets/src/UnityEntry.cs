using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Yukar.Common;
using Yukar.Engine;

public class UnityEntry : MonoBehaviour
{
    public float boundsExtentScale = 1.5f;

    static protected GameMain sGame = null;
    static internal GameMain game { get { return sGame; } }
    static protected UnityEntry sSelf = null;
    static internal UnityEntry self { get { return sSelf; } }

    private static Texture2D sFrameBuffer;
#if UNITY_IOS || UNITY_ANDROID
    int mUpdateSkipCount = 2;//解像度変更のため２フレーム待つ
#endif

    internal static void InitializeDir()
    {
#if UNITY_SWITCH && !UNITY_EDITOR
        Catalog.sCommonResourceDir = Application.dataPath + "/Resources/samples/common/";
        Catalog.sResourceDir = Application.dataPath + "/Resources/";
        Catalog.sDlcDir = Application.dataPath + "/Resources/samples/";
#else
        var assetPath = "assets/resources";	// 実際の所なんでもいい
        Catalog.sCommonResourceDir = assetPath + "/samples/common/";
        Catalog.sResourceDir = assetPath + "/";
        Catalog.sDlcDir = assetPath + "/samples/";
#endif
    }

    // Use this for initialization
    void Start()
    {
        if (game != null) return;
        SharpKmyGfx.ModelInstance.sBoundsExtentScale = boundsExtentScale;
        SharpKmyGfx.Render.InitializeRender();
        UnityUtil.Initialize();
        sSelf = this;
#if !UNITY_EDITOR
        Debug.logger.logEnabled = false;
#endif
#if UNITY_IOS || UNITY_ANDROID
        UnityResolution.Start();
#endif //UNITY_IOS || UNITY_ANDROID

        Yukar.Common.FileUtil.language = Application.systemLanguage.ToString();

        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        InitializeDir();

        FileUtil.initialize();

        Catalog.createDlcList(false);
        Catalog catalog = new Catalog();
        catalog.load(false);
        Yukar.Common.GameData.SystemData.sDefaultBgmVolume = catalog.getGameSettings().defaultBgmVolume;
        Yukar.Common.GameData.SystemData.sDefaultSeVolume = catalog.getGameSettings().defaultSeVolume;

        sGame = new GameMain();
        game.initialize();

        UnityAdsManager.Initialize(game);
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_STANDALONE_WIN
        if (UnityEngine.Input.GetKeyDown(KeyCode.F4))
        {
            Screen.fullScreen = !Screen.fullScreen;
        }
        if (UnityEngine.Input.GetKeyDown(KeyCode.LeftAlt) && UnityEngine.Input.GetKey(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.RightAlt) && UnityEngine.Input.GetKey(KeyCode.Return))
        {
            Screen.fullScreen = !Screen.fullScreen;
        }
#elif UNITY_IOS || UNITY_ANDROID
        if (0 < this.mUpdateSkipCount){
            this.mUpdateSkipCount--;
            return;
        }
        UnityResolution.Update();
#endif // UNITY_STANDALONE_WIN
        game.update(Time.deltaTime);
        GameMain.setElapsedTime(Time.deltaTime);
    }

    void OnGUI()
    {
        if (Event.current.type == EventType.Repaint)
        {
            SharpKmyGfx.SpriteBatch.DrawAll();
        }
    }

    void OnApplicationQuit()
    {
        sSelf = null;
    }

    public static bool IsImportMapScene()
    {
#if UNITY_EDITOR
        if (CustomImporter.IsImportMapScene) return true;
#endif
        return false;
    }

    public static bool IsReimportMapScene()
    {
#if UNITY_EDITOR
        if (CustomImporter.IsReimportMapScene) return true;
#endif
        return false;
    }

    public static bool IsDivideMapScene()
    {
        return Yukar.Common.Rom.GameSettings.IsDivideMapScene;
    }

    public static void capture()
    {
        var mainCamera = GameObject.Find("Main Camera");

        if (mainCamera == null) return;

        var camera = mainCamera.GetComponent<Camera>();
        var rendertexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Default);

        camera.targetTexture = rendertexture;

        RenderTexture.active = rendertexture;
        camera.Render();

        sFrameBuffer = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        sFrameBuffer.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        sFrameBuffer.Apply();

        RenderTexture.active = null;
        camera.targetTexture = null;
    }

    public static void reserveClearFB()
    {
        self.StartCoroutine(clearFB(false));
    }

    public static IEnumerator clearFB(bool immediate)
    {
        if (!immediate)
            yield return new WaitForEndOfFrame();

        if (sFrameBuffer == null)
            yield break;

        Destroy(sFrameBuffer);
        sFrameBuffer = null;
    }

    public static bool isFBCaptured()
    {
        return sFrameBuffer != null;
    }

    internal static void blackout()
    {
        self.StartCoroutine(clearFB(true));

        sFrameBuffer = new Texture2D(1, 1, TextureFormat.RGB24, false);
        sFrameBuffer.SetPixel(0, 0, Color.black);
        sFrameBuffer.Apply();
    }

    public static void drawCapturedFB()
    {
        if (sFrameBuffer == null)
            return;

        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), sFrameBuffer);
    }

    internal static void startCoroutine(IEnumerator routine)
    {
        self.StartCoroutine(routine);
    }
}


#if UNITY_IOS || UNITY_ANDROID
class UnityResolution
{
    const int ScreenSizeW = 960;
    //const int ScreenSizeH = 540;
    const int FrameRate = 60;
    
    protected static List<int> sScreenSizeList = new List<int>();
    protected static float sScreenSizeAspect = 0;
    protected static int sCurrentScreenSizeIndex = 0;
    protected static List<float> sFrameRateList = new List<float>();

    public static void Start()
    {
        //初期化
        ClearFrameRateList();
        Application.targetFrameRate = FrameRate;

        //可能とする解像度を追加
        sScreenSizeAspect = (float)Screen.height / (float)Screen.width;
#if true
        if (Screen.width <= ScreenSizeW
        || Screen.width / 2 <=  ScreenSizeW)
        {
            sScreenSizeList.Add(Screen.width);
        }
        else
        {
            if(Screen.width / 2 < ScreenSizeW * 2) {
                sScreenSizeList.Add(Screen.width / 2);
            }
            else
            {
                sScreenSizeList.Add(ScreenSizeW * 2);
            }
        }
#else
        sScreenSizeList.Add(Screen.width);
        if (ScreenSizeW < Screen.width)
        {
            for (int i1 = 1; true; ++i1)
            {
                int w = ScreenSizeW * i1;
                if (Screen.width <= w) break;
                sScreenSizeList.Add(w);
            }
        }
        for (int i1 = 2; true; ++i1)
        {
            int w = Screen.width / i1;
            if (w <= ScreenSizeW) break;
            sScreenSizeList.Add(w);
        }
        sScreenSizeList.Sort();

        //解像度の大きさに制限
        for(int i1 = sScreenSizeList.Count -1; 0 <= i1; --i1)
        {
            if (sScreenSizeList[i1] <= ScreenSizeW * 2) continue;
            sScreenSizeList.RemoveAt(i1);
        }
#endif

        //解像度の初期設定
        sCurrentScreenSizeIndex = -1;
        SetResolution(sScreenSizeList.Count - 1);
    }


    public static void Update()
    {
        //フレームリストに追加
        float fps = 1f / Time.deltaTime;
        if (fps < 3) return;//明らかに低い場合は無視する
        AddFrameRateList(fps);

        //フレームが出ているか確認
        float ave = GetFrameRateAve();
        if (45 < ave) return;

        //解像度を下げる
        Down();
    }

    public static void Up()
    {
        SetResolution(sCurrentScreenSizeIndex + 1);
        ClearFrameRateList();
    }

    public static void Down()
    {
        SetResolution(sCurrentScreenSizeIndex - 1);
        ClearFrameRateList();
    }

    static void ClearFrameRateList()
    {
        sFrameRateList.Clear();
        for (var i1 = 0; i1 < FrameRate * 0.1; ++i1) sFrameRateList.Add(FrameRate);
    }

    static void AddFrameRateList(float inFps)
    {
        sFrameRateList.RemoveAt(0);
        sFrameRateList.Add(inFps);
    }

    static float GetFrameRateAve()
    {
        float ave = 0;
        foreach (var fps in sFrameRateList) ave += fps;
        ave /= sFrameRateList.Count;
        return ave;
    }

    static void SetResolution(int inIndex)
    {
        if (inIndex < 0) inIndex = 0;
        if (sScreenSizeList.Count <= inIndex) inIndex = sScreenSizeList.Count - 1;
        if (inIndex == sCurrentScreenSizeIndex) return;
        sCurrentScreenSizeIndex = inIndex;

        int width = sScreenSizeList[inIndex];
        int height = (int)((float)width * sScreenSizeAspect);
        if (Screen.width == width) return;

        Screen.SetResolution(width, height, true, FrameRate);
        Debug.Log("Screen.SetResolution (" + width.ToString() + "," + height.ToString() + ")");
    }
}
#endif //UNITY_IOS || UNITY_ANDROID

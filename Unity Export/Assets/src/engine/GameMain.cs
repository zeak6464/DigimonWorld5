//#define ENABLE_DISP_FPS

#region Using Statements
using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Color = Microsoft.Xna.Framework.Color;

#if WINDOWS
using System.Windows.Forms;
#else
using Eppy;
#endif

#endregion

namespace Yukar.Engine
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class GameMain : SharpKmyBase.Task
    {
        // Sceneから owner.*** でアクセスされる可能性のある変数
        internal Common.Catalog catalog;
        internal Common.GameDataManager data;
        internal struct LoadedSe
        {
            internal int decide;
            internal int cancel;
            internal int select;
            internal int buy;
            internal int item;
            internal int levelup;
            internal int escape;
            internal int defeat;
            internal int skill;
            internal int textInput;
            internal int textDelete;
        };
        internal LoadedSe se;

        private TitleScene titleScene;
        internal MapScene mapScene;
        private BattleTestScene battleTestScene;

        internal Common.DebugSettings debugSettings;
        
        private DebugDialog debugDialog;
        
        public static Vector2 halfOffset = new Vector2(0.5f, 0.5f);

        private bool isDrawInitializeLoadingText = false;

        public bool IsTestPlayMode { get; private set; }

        public static GameMain instance { get; private set; }

        public bool IsBattle2D
        {
            get
            {
                return catalog.getGameSettings().battleType == Common.Rom.GameSettings.BattleType.CLASSIC;
            }
        }

        public enum Scenes
//		internal enum Scenes
        {
            NONE,
            LOGO,
            TITLE,
            MAP,
            BATTLE_TEST,
        }

        internal Scenes nowScene;
        public Scenes getScenes() { return nowScene; }

        SharpKmyIO.Controller controller;

        private SharpKmyGfx.Font loadingFont;

        internal List<Tuple<string, Func<bool>>> taskList = new List<Tuple<string, Func<bool>>>();
        internal void pushTask(Func<bool> task)
        {
            taskList.Add(new Tuple<string, Func<bool>>("", task));
        }
        internal void clearTask(string nickname)
        {
            taskList.RemoveAll(x => x.Item1 == nickname);
        }
        internal void pushTask(string nickname, Func<bool> task)
        {
            taskList.Add(new Tuple<string, Func<bool>>(nickname, task));
        }

        public GameMain()
            : base()
        {
            controller = new SharpKmyIO.Controller();
            controller.addInput("F1", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0x70, 1);
            controller.addInput("F2", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0x71, 1);
            controller.addInput("F3", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0x72, 1);
            controller.addInput("F4", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0x73, 1);
            controller.addInput("F5", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0x74, 1);
            controller.addInput("ReturnTitle", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'C', 1);
            controller.addInput("ControlLeft", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0xA2, 1);
            controller.addInput("ControlRight", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0xA3, 1);
            controller.addInput("Enter", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0x0D, 1);
            controller.addInput("AltLeft", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0xA4, 1);
            controller.addInput("AltRight", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0xA5, 1);
            instance = this;
        }


        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        public override void initialize()
        {
            catalog = Common.Catalog.sInstance; //既に読み込んでいる。
            
            Common.GameDataManager.InititalizeAccount();
            Graphics.Initialize(catalog.getGameSettings().gameFont, false, catalog.getGameSettings().gameFontUseOldMethod);
            
#if !(WINDOWS)
            ResourceManager.Image.initialize();
#endif

            Audio.Initialize();
            Audio.setMasterVolume((float)catalog.getGameSettings().defaultBgmVolume / 100,
                (float)catalog.getGameSettings().defaultSeVolume / 100);
            Input.Initialize(this);
            Touch.Initialize(this);

            refreshResourcePath();

            // 効果音読み込み
            var gs = catalog.getGameSettings();
            if (gs != null)
            {
                loadSe(out se.decide, gs.seDecide);
                loadSe(out se.cancel, gs.seCancel);
                loadSe(out se.select, gs.seSelect);
                loadSe(out se.buy, gs.seBuy);
                loadSe(out se.item, gs.seItem);
                loadSe(out se.levelup, gs.seLevelUp);
                loadSe(out se.escape, gs.seEscape);
                loadSe(out se.defeat, gs.seDefeat);
                loadSe(out se.skill, gs.seSkill);
                loadSe(out se.textInput, gs.seTextInput);
                loadSe(out se.textDelete, gs.seTextDelete);
            }

            loadingFont = Graphics.sInstance.createFont(16);

            debugSettings = new Common.DebugSettings();

            data = new Common.GameDataManager();
            data.inititalize(catalog);
            logoScene = new LogoScene(this);

            // マウスカーソルを表示
            //this.IsMouseVisible = true;

            // バーチャルパッド用の画像を読み込む
            //virtualPad = new VirtualPad();

            //SharpKmyMath.Matrix4 ortho = SharpKmyMath.Matrix4.ortho(0, 960, 0, 544, -1000, 1000);
#if ENABLE_VR
            if( SharpKmyVr.Func.IsReady() )
            {
                // ビューポート設定
                SharpKmyGfx.Render.getDefaultRender().setViewport(0, 0, (int)SharpKmy.Entry.getMainWindowWidth(), (int)SharpKmy.Entry.getMainWindowHeight());
                SharpKmyGfx.Render.getRender2D().setViewport(0, 0, (int)SharpKmy.Entry.getMainWindowWidth(), (int)SharpKmy.Entry.getMainWindowHeight());

                // VR描画の初期化
                VrDrawer.Init();
            }
            else
            {
                SharpKmyGfx.Render.getDefaultRender().setViewport(0, 0, (int)SharpKmy.Entry.getMainWindowWidth(), (int)SharpKmy.Entry.getMainWindowHeight());
            }
#else   // #if ENABLE_VR
            SharpKmyGfx.Render.getDefaultRender().setViewport(0, 0, (int)SharpKmy.Entry.getMainWindowWidth(), (int)SharpKmy.Entry.getMainWindowHeight());
#endif  // #if ENABLE_VR
        }

        private void refreshResourcePath()
        {
            Graphics.ClearResourceServer();
            Graphics.AddResourceDir("./");
            Graphics.SetCommonResourceDir(Common.Catalog.sCommonResourceDir);
            foreach (var info in catalog.getUsingDlcInfoList())
            {
                Graphics.AddResourceDir(info.path);
            }
        }

        private void loadSe(out int id, Guid guid)
        {
            id = -1;
            var rom = catalog.getItemFromGuid(guid) as Common.Resource.Se;
            if (rom != null)
            {
                id = Audio.LoadSound(rom);
            }
        }

        public override void finalize()
        {
            if (Graphics.sInstance == null)
                return;

            if (mapScene != null)
            {
                mapScene.Reset();
                mapScene.mapDrawer.Destroy();
            }
            Input.finalize();
            Touch.finalize();
            Audio.Destroy();
            Graphics.Destroy();

            if (loadingFont != null)
            {
                loadingFont.Release();
                loadingFont = null;
            }

            controller.Release();

            if (debugDialog != null)
            {
                debugDialog.Close();
                debugDialog.Dispose();
            }

            SharpKmyBase.Task.shutdown();
        }

        static public void setElapsedTime(float elapsed)
        {
            sLastElapsed = limitElapsed(elapsed);
        }

        private LogoScene logoScene;
        private static float sLastElapsed = 0.016f;
        private const float GAME_SPEED = 1.0f;
        static public float getElapsedTime()
        {
            return sLastElapsed;
        }

        static public float getRelativeParam60FPS()
        {
            return sLastElapsed * 60f;
        }

        class SpriteDrawer : SharpKmyGfx.Drawable
        {
            public override void draw(SharpKmyGfx.Render scn)
            {
                if (Graphics.sInstance == null)
                    return;

                scn.setViewMatrix(
                    SharpKmyMath.Matrix4.ortho(0, Graphics.ScreenWidth, 0, Graphics.ScreenHeight, -100, 100),
                    SharpKmyMath.Matrix4.identity()
                    );
                Graphics.getSpriteBatch().setLayer(10000);
                Graphics.getSpriteBatch().draw(scn);
            }
        }

        SpriteDrawer spdrawer = new SpriteDrawer();
        public int askExitFrame = -1;
        //internal AskWindow mExitConfirmAsk = new AskWindow(); //#24172
        static protected bool sPushedAndroidBackButton = false;
        static public bool IsPushedAndroidBackButton() { return sPushedAndroidBackButton; }

        public override void update(float elapsed)
        {
#if WINDOWS
            // 終了確認処理
            askExitFrame--;
            if (SharpKmy.Entry.getCloseRequested() || askExitFrame == 0)
            {
                if (!IsTestPlayMode && nowScene == Scenes.MAP)
                {
                    if (SharpKmy.Entry.isFullScreenMode())
                    {
                        setFullScreenMode(false);
                        askExitFrame = 2;
                    }
                    else
                    {
                        mapScene.mapEngine.setCursorVisibility(true);
                        var result = MessageBox.Show(Properties.Resources.str_AskExitGame, Properties.Resources.str_Caution,
                            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (result == DialogResult.Yes)
                            Exit();
                        askExitFrame = 0;
                    }
                }
                else
                {
                    Exit();
                }
            }
#endif

            Engine.Graphics.errorCheck("GameMain");

            sLastElapsed = limitElapsed(elapsed);
            if (data != null)
            {
                data.system.update(elapsed);
            }
            Input.Update();
            Touch.Update(/*this.Window*/);

#if UNITY_ANDROID && false //#24172
            this.mExitConfirmAsk.Update();

            // Androidの戻るボタン
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Escape))
            {
                if (nowScene == Scenes.TITLE)
                {
                    UnityEngine.Application.Quit();
                    return;
                }

                if (this.mExitConfirmAsk.windowState == CommonWindow.WindowState.HIDE_WINDOW)
                {
                    var res = getMenuParamSet();
                    this.mExitConfirmAsk.Initialize(res, Graphics.ScreenWidth / 2, Graphics.ScreenHeight / 2, 640, 160);
                    this.mExitConfirmAsk.setInfo(res.gs.glossary.askExitGame, res.gs.glossary.yes, res.gs.glossary.no);
                    this.mExitConfirmAsk.selected = 1;
                    this.mExitConfirmAsk.Show();
                    sPushedAndroidBackButton = true;
                    mapScene.LockControl();
                }
            }

            if (this.mExitConfirmAsk.windowState == CommonWindow.WindowState.SHOW_WINDOW)
            {
                if (this.mExitConfirmAsk.result == 0)
                {
                    //UnityEngine.Debug.Log("quit");
                    UnityEngine.Application.Quit();
                }
                else if (this.mExitConfirmAsk.result == 1)
                {
                    this.mExitConfirmAsk.Hide();
                    sPushedAndroidBackButton = false;
                    mapScene.UnlockControl();
                }
            }
#endif //UNITY_ANDROID

            // フルスクリーン切り替え
            if (controller.isUp("F4") ||
               ((controller.isHold("AltLeft") || controller.isHold("AltRight")) && controller.isUp("Enter")))
            {
                if (askExitFrame < -2)
                {
                    var targetMode = !SharpKmy.Entry.isFullScreenMode();
                    setFullScreenMode(targetMode);
#if WINDOWS
                    // #34764 フルスクリーンの時にfinishしないとフレーム落ちするので対策
                    if (Program.enableGlFinish)
                        SharpKmy.Entry.enableGLFinish(targetMode);
#endif
                }
            }

            // フルスクリーン切り替え
            if (controller.isUp("F3"))
            {
                setFullScreenMode(false);
#if WINDOWS
                // #34764 フルスクリーンの時にfinishしないとフレーム落ちするので対策
                if (Program.enableGlFinish)
                    SharpKmy.Entry.enableGLFinish(false);
#endif
            }

            if (IsTestPlayMode)
            {
                if (nowScene == Scenes.MAP)
                {
                    // クイックセーブ
                    if (controller.isUp("F1"))
                    {
                        DoSave(0);
                    }

                    // クイックロード
                    if (controller.isUp("F2"))
                    {
                        DoLoad(0);
                        return;
                    }
                }

                // デバッグダイアログ
                if (controller.isUp("F5"))
                {
                    setFullScreenMode(false);

                    if (debugDialog == null)
                    {
                        debugDialog = new DebugDialog();
                        debugDialog.LoadGameData(debugSettings, this);
                    }
                    else
                    {
                        debugDialog.UpdatePartyData();
                        debugDialog.UpdateEventSwitch();
                        debugDialog.UpdateVariable();
                    }

                    if (debugDialog.Visible)
                    {
                        debugDialog.Activate();
                    }
                    else
                    { 
                        debugDialog.Show();
                        mapScene.mapEngine.setCursorVisibility(true);
                    }
                }

                if (debugDialog != null && debugDialog.Visible)
                {
                    if (debugDialog.IsUpdatePartyData()) debugDialog.UpdatePartyData();
                    if (debugDialog.IsUpdateEventSwitch()) debugDialog.UpdateEventSwitch();
                    if (debugDialog.IsUpdateVariable()) debugDialog.UpdateVariable();

                    debugDialog.SetMapScene(mapScene);
                }

                // Ctrl + Cキーでタイトル画面に戻る / バトルテストだったらバトルテストをやり直す
                if (controller.isUp("ReturnTitle") && (controller.isHold("ControlLeft") || controller.isHold("ControlRight")))
                {
                    if(nowScene == Scenes.BATTLE_TEST)
                    {
                        battleTestScene.Restart();
                    }
                    else if (nowScene != Scenes.TITLE)
                    {
                        ChangeScene(Scenes.TITLE);
                    }
                }
            }
            
#if ENABLE_VR
            if( SharpKmyVr.Func.IsReady() )
            {
                // カメラリセット
                if( Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CAMERA_POSITION_RESET) )
                {
                    switch (nowScene)
                    {
//					case Scenes.LOGO:			logoScene.							break;
//					case Scenes.TITLE:			titleScene.							break;
                    case Scenes.MAP:			mapScene.SetupVrCameraData( true );	break;
//					case Scenes.BATTLE_TEST:	battleTestScene.					break;
                    }
                }

                // VRキャリブレーション
                if( Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.VR_CALIBRATION) )
                {
                    VrCameraData vrCameraData = GetCurrentVrCameraData();
                    if( vrCameraData != null ) {
                        vrCameraData.Calibration();
                    }
                }

                // VRカメラ同期
                if( Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.VR_SYNC_CAMERA) )
                {
                    if( nowScene == Scenes.MAP ) {
                        mapScene.SyncCamera();
                    }
                }

                // カメラ回転（VR箱庭視点用）
                {
                    int val = 0;
                    
                    if( Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CAMERA_HORIZONTAL_ROT_CLOCKWISE) ) {
                        val--;
                    } else if( Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CAMERA_HORIZONTAL_ROT_COUNTER_CLOCKWISE) ) {
                        val++;
                    }

                    if( val != 0 )
                    {
                        VrCameraData vrCameraData = GetCurrentVrCameraData();
                        if( vrCameraData != null ) {
                            vrCameraData.ChangeRotateY( val );
                        }
                    }
                }

                // カメラ高さ（VR箱庭視点用）
                {
                    int val = 0;

                    if( Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CAMERA_VERTICAL_ROT_DOWN) ) {
                        val--;
                    } else if( Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CAMERA_VERTICAL_ROT_UP) ) {
                        val++;
                    }

                    if( val != 0 )
                    {
                        VrCameraData vrCameraData = GetCurrentVrCameraData();
                        if( vrCameraData != null ) {
                            vrCameraData.ChangeHeight( val );
                        }
                    }
                }

                // カメラ距離（VR箱庭視点用）
                {
                    int val = 0;

                    if( Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CAMERA_ZOOM_IN) ) {
                        val--;
                    } else if( Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CAMERA_ZOOM_OUT) ) {
                        val++;
                    }

                    if( val != 0 )
                    {
                        VrCameraData vrCameraData = GetCurrentVrCameraData();
                        if( vrCameraData != null ) {
                            vrCameraData.ChangeDistance( val );
                        }
                    }
                }
            }
#endif  // #if ENABLE_VR

            switch (nowScene)
            {
                case Scenes.NONE:
                    if (isDrawInitializeLoadingText)
                    {
                        DoReset(true, false);

                        IsTestPlayMode = false;

#if UNITY_EDITOR
                        IsTestPlayMode = true;
#endif

                        nowScene = Scenes.LOGO;
                        var commands = Environment.GetCommandLineArgs();
                        if (commands == null)
                            commands = new string[0];
                        foreach (var cmd in commands)
                        {
                            if (cmd.StartsWith("/SkipLogo"))
                            {
#if TRIAL
#else
                                logoScene.finalize();
                                nowScene = Scenes.TITLE;
#endif
                                IsTestPlayMode = true;
                            }
                            else if (cmd.StartsWith("/SkipTitle"))
                            {
                                DoReset();
                                ChangeScene(GameMain.Scenes.MAP);
                                IsTestPlayMode = true;
                            }
                            else if (cmd.StartsWith("/BattleTest"))
                            {
                                nowScene = Scenes.BATTLE_TEST;
                                IsTestPlayMode = true;

                                if (cmd.Length > "/BattleTest ".Length)
                                {
                                    string[] options = cmd.Replace("\"", "").Remove(0, "/BattleTest ".Length).Split(",".ToCharArray()); // オプション全体の文字列
                                    string mapStr = options[0];
                                    string partyStr = options[1];
                                    string enemies = options.Length > 2 ? options[2] : "";
                                    string layoutStr = null;
                                    string battleBgStr = null;
                                    for (int i = 3; i < options.Length; i++) {
                                        if (options[i].StartsWith("POS:"))
                                            layoutStr = options[i];
                                        else if (options[i].StartsWith("BG:"))
                                            battleBgStr = options[i];
                                    }

                                    // 戦闘マップ
                                    var p = MapScene.ChangeMapParams.defaultParams;
                                    p.guid = new Guid(mapStr);
                                    mapScene.ChangeMapTiny(p);

                                    // 味方パーティ情報
                                    data.party.members.Clear();

                                    foreach (string memberParameterStr in partyStr.Split(";".ToCharArray()))
                                    {
                                        Common.GameData.Hero hero = null;

                                        foreach (string str in memberParameterStr.Split(" ".ToCharArray()))
                                        {
                                            string[] paramsStr = str.Split(":".ToCharArray());

                                            switch (paramsStr[0])
                                            {
                                                case "GUID":
                                                    hero = data.party.AddMember(catalog.getItemFromGuid(new Guid(paramsStr[1])) as Common.Rom.Hero);
                                                    break;

                                                case "Level":
                                                    hero.SetLevel(int.Parse(paramsStr[1]), catalog);
                                                    break;

                                                case "Head":
                                                    hero.equipments[2] = catalog.getItemFromGuid(new Guid(paramsStr[1])) as Common.Rom.Item;
                                                    break;

                                                case "Weapon":
                                                    hero.equipments[0] = catalog.getItemFromGuid(new Guid(paramsStr[1])) as Common.Rom.Item;
                                                    break;

                                                case "Shield":
                                                    hero.equipments[1] = catalog.getItemFromGuid(new Guid(paramsStr[1])) as Common.Rom.Item;
                                                    break;

                                                case "Armor":
                                                    hero.equipments[3] = catalog.getItemFromGuid(new Guid(paramsStr[1])) as Common.Rom.Item;
                                                    break;

                                                case "Acc1":
                                                    hero.equipments[4] = catalog.getItemFromGuid(new Guid(paramsStr[1])) as Common.Rom.Item;
                                                    break;

                                                case "Acc2":
                                                    hero.equipments[5] = catalog.getItemFromGuid(new Guid(paramsStr[1])) as Common.Rom.Item;
                                                    break;
                                            }
                                        }

                                        if (hero != null)
                                        {
                                            hero.refreshEquipmentEffect();
                                        }
                                    }

                                    // 敵モンスター
                                    var monstersGuid = new List<Guid>();

                                    battleTestScene.BattleSetting = mapScene.map.mapBattleSetting;

                                    foreach (string enemyStr in enemies.Split(";".ToCharArray()))
                                    {
                                        foreach (string str in enemyStr.Split(" ".ToCharArray()))
                                        {
                                            string[] paramsStr = str.Split(":".ToCharArray());

                                            switch (paramsStr[0])
                                            {
                                                case "GUID": // モンスター指定
                                                    monstersGuid.Add(new Guid(paramsStr[1]));
                                                    break;
                                                case "AREA-GUID": // エンカウントエリア設定
                                                    {
                                                        var areaId = new Guid(paramsStr[1]);

                                                        foreach (var item in mapScene.map.areaBattleSettings)
                                                        {
                                                            if (item.guId == areaId)
                                                            {
                                                                battleTestScene.BattleSetting = item;

                                                                break;
                                                            }
                                                        }
                                                    }
                                                    break;
                                            }
                                        }
                                    }

                                    // 敵配置
                                    if (layoutStr != null && catalog.getGameSettings().battleType == Common.Rom.GameSettings.BattleType.MODERN)
                                    {
                                        var layout = new Microsoft.Xna.Framework.Point[monstersGuid.Count];

                                        int count = 0;
                                        foreach (string layoutEntry in layoutStr.Split(";".ToCharArray()))
                                        {
                                            if (count >= monstersGuid.Count)
                                                break;
                                            string[] paramsStr = layoutEntry.Split(new char[]{' ', ':'});
                                            layout[count] = new Microsoft.Xna.Framework.Point(int.Parse(paramsStr[1]), int.Parse(paramsStr[2]));
                                            count++;
                                        }

                                        battleTestScene.layout = layout;
                                    }

                                    // 戦闘背景
                                    if (battleBgStr != null)
                                    {
                                        string[] paramsStr = battleBgStr.Split(new char[] { ' ', ':' });
                                        var battleBg = new Common.Rom.BattleBgSettings()
                                        {
                                            bgRom = catalog.getItemFromGuid(new Guid(paramsStr[1])),
                                            centerX = int.Parse(paramsStr[2]),
                                            centerY = int.Parse(paramsStr[3])
                                        };
                                        battleTestScene.battleBg = battleBg;
                                    }

                                    if (monstersGuid.Count > 0) battleTestScene.BattleMonsters = monstersGuid.ToArray();
                                }
                                else
                                {
                                    // 戦闘マップ
                                    var p = MapScene.ChangeMapParams.defaultParams;
                                    p.guid = catalog.getGameSettings().startMap;
                                    mapScene.ChangeMap(p);
                                }
                            }
                        }

                        if (nowScene == Scenes.LOGO && catalog.getGameSettings().title.splashImage == Guid.Empty)
                        {
                            logoScene.finalize();
                            nowScene = Scenes.TITLE;
                        }

                        ChangeScene(nowScene);
#if false//DEBUG
                        // すべてのアイテムを50個ふやす
                        foreach (var item in catalog.getFilteredItemList(typeof(Common.Rom.Item)))
                        {
                            data.party.AddItemNum(item.guId, 50);
                        }
#endif
                    }
                    break;
                case Scenes.LOGO:
                    logoScene.Update();
                    break;
                case Scenes.TITLE:
                    titleScene.Update();
                    break;
                case Scenes.MAP:
                    mapScene.Update();
                    break;
                case Scenes.BATTLE_TEST:
                    battleTestScene.Update();
                    break;
            }

            foreach (var task in new List<Tuple<string, Func<bool>>>(taskList))
            {
                if (!task.Item2())
                    taskList.Remove(task);
            }

            draw();

#if ENABLE_DISP_FPS
            // FPS表示
            DispFps( elapsed );
#endif  // #if ENABLE_DISP_FPS
        }

        private void setFullScreenMode(bool v)
        {
            SharpKmy.Entry.fullScreenMode(v);
            mapScene.mapEngine.setCursorVisibility(true);
        }

        private static float limitElapsed(float elapsed)
        {
            float maxElapsed = 1f / 15f;//最大補正は15FPSまで。それ以上は読み込みとかそういう
            if (elapsed >= maxElapsed)
            {
                elapsed = maxElapsed;
            }
            return elapsed;
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public void draw()
        {
            switch (nowScene)
            {
                case Scenes.LOGO:
                    logoScene.Draw();
                    break;
                case Scenes.TITLE:
                    titleScene.Draw();
                    break;
                case Scenes.MAP:
                    mapScene.Draw();
                    break;
                case Scenes.BATTLE_TEST:
                    battleTestScene.Draw();
                    break;
            }
            // バーチャルパッドの描画
            /*
            var touchState = Touch.GetState();
            if (touchState.TouchFrameCount > 0 && !touchState.IsDecideGesture)
            {
                float scale = Math.Min(1.0f, touchState.TouchFrameCount / 7.5f);

                Graphics.BeginDraw();

                virtualPad.Draw(touchState.TouchBeginPosition, scale);

                Graphics.EndDraw();
            }
            */

#if ENABLE_VR
            if ( SharpKmyVr.Func.IsReady() ) {
                DrawVr( nowScene );
            }
#endif  // #if ENABLE_VR

            if (!isDrawInitializeLoadingText)
            {
                DrawLoadingText();

                isDrawInitializeLoadingText = true;
            }

            SharpKmyGfx.Render.getDefaultRender().addDrawable(spdrawer);

#if ENABLE_VR
            // 2D描画
            if( SharpKmyVr.Func.IsReady() ) {
                SharpKmyGfx.Render.getRender2D().addDrawable(spdrawer);
            }
#endif  // #if ENABLE_VR

            Input.Draw();
#if UNITY_ANDROID && false //#24172
            this.mExitConfirmAsk.Draw();
#endif
        }

        float posY = 0;
        int cnt = 0;
        const double JUMP_HEIGHT = 16;
        const double SPEED = 16;

        public void DrawLoadingText()
        {
            if (loadingFont != null)
            {
                string text = catalog.getGameSettings().glossary.nowLoading;
                var size = Graphics.MeasureString(loadingFont, text);

                size.Y *= 0.75f;
                
                Graphics.DrawStringSoloColor(loadingFont, text,
                    new Vector2(Graphics.ScreenWidth - size.X - 18,
                    Graphics.ScreenHeight - size.Y - 18 - posY), Color.White);
            }

#if WINDOWS
#else
            // アニメーションはとりあえずUnityでだけ有効にしておく
            posY = (float)Math.Abs(JUMP_HEIGHT * Math.Sin(cnt++ / SPEED));
#endif
        }

        internal void DoReset(bool dataReset = true, bool loadMap = true)
        {
            // ゲームデータ初期化
            if (dataReset) data.inititalize(catalog);

            // デバッグ用 全アイテム20個
            //foreach (Common.Rom.Item item in catalog.getFilteredItemList(typeof(Common.Rom.Item)))
            //{
            //    data.party.AddItem(item.guId, 20);
            //}

            // マップ画面初期化
            catalog.getFilteredItemList(typeof(Common.Rom.Map)).ForEach(
                map => ((Common.Rom.Map)map).cameraMode = ((Common.Rom.Map)map).origCameraMode);
            if (data.start != null)
            {
                if (mapScene != null)
                {
                    mapScene.Reset();
                    mapScene.mapDrawer.Destroy();
                }
                mapScene = new MapScene(this);
                if (loadMap)
                {
                    var p = MapScene.ChangeMapParams.defaultParams;
                    p.guid = data.start.map;
                    p.x = data.start.x;
                    p.y = data.start.y;
                    p.createHero = true;
                    p.eventStates = data.start.events;
                    p.height = data.start.heightAvailable ? data.start.height : -1;
                    p.camera = data.start.camera;
                    p.spriteStates = data.start.sprites;
                    p.dir = data.start.dir;
                    p.playerLock = data.start.plLock;
                    p.cameraLockByEvent = data.start.camLockedByEvent;
                    p.cameraLock = data.start.camLock;
                    p.cameraModeLock = data.start.camModeLock;
                    p.bgmStatus = data.start.currentBgm;
                    p.bgsStatus = data.start.currentBgs;
                    mapScene.ChangeMap(p);
                }
                if (titleScene == null) titleScene = new TitleScene(this, catalog);
                if (battleTestScene == null) battleTestScene = new BattleTestScene(this);
            }
            else
            {
                nowScene = Scenes.NONE;
            }
        }

        internal void ChangeScene(Scenes scene)
        {
#if UNITY_IOS || UNITY_ANDROID
            UnityResolution.Up();
#endif

            if (scene == Scenes.TITLE)
            {
                if(mapScene.menuWindow.isVisible())
                    mapScene.menuWindow.goToClose();
                mapScene.mapEngine.setCursorVisibility(true);  // カーソルを表示する
                titleScene.initialize();
            }

            nowScene = scene;
            update(0);
        }

        public float getScreenAspect()
        {
            return SharpKmy.Entry.getMainWindowAspect();
        }

        public void Exit()
        {
            SharpKmyBase.Task.removeTask(this);
        }

        internal CommonWindow.ParamSet getMenuParamSet()
        {
            return mapScene.menuWindow.res;
        }

        internal void DoLoad(int index)
        {
            data = Yukar.Common.GameDataManager.Load(catalog, index);
            Audio.setMasterVolume((float)data.system.bgmVolume / 100, (float)data.system.seVolume / 100);
            DoReset(false); // データがもうあるので初期化しない
        }

        internal void DoSave(int index)
        {
            data.start.map = mapScene.map.guId;
            data.start.x = (int)mapScene.hero.x;
            data.start.y = (int)mapScene.hero.z;
            data.start.dir = mapScene.hero.getDirection();
            data.start.events = mapScene.saveEventState();
            data.start.height = mapScene.hero.y;
            data.start.camera = mapScene.saveCameraSettings();
            data.start.sprites = mapScene.saveSpriteState();
            data.start.plLock = mapScene.isPlayerLockedByEvent();
            data.start.camLockedByEvent =
                mapScene.map.fixCamera != mapScene.mapFixCamera ||
                mapScene.map.fixCameraMode != mapScene.mapFixCameraMode;
            data.start.camLock = mapScene.mapFixCamera;
            data.start.camModeLock = mapScene.mapFixCameraMode;
            var sound = Audio.GetNowBgm(false);
            data.start.currentBgm.currentBgm = Guid.Empty;
            if (sound != null)
            {
                if(sound.rom != null)
                    data.start.currentBgm.currentBgm = sound.rom.guId;
                data.start.currentBgm.pan = sound.sound.getPan();
                if(Audio.getMasterBgmVolume() > 0)
                    data.start.currentBgm.volume = sound.sound.getVolume() / Audio.getMasterBgmVolume();
                data.start.currentBgm.tempo = sound.sound.getTempo();
            }
            sound = Audio.GetNowBgs();
            data.start.currentBgs.currentBgm = Guid.Empty;
            if (sound != null)
            {
                if (sound.rom != null)
                    data.start.currentBgs.currentBgm = sound.rom.guId;
                data.start.currentBgs.pan = sound.sound.getPan();
                if (Audio.getMasterSeVolume() > 0)
                    data.start.currentBgs.volume = sound.sound.getVolume() / Audio.getMasterSeVolume();
                data.start.currentBgs.tempo = sound.sound.getTempo();
            }
            Yukar.Common.GameDataManager.Save(data, index);
        }

        internal bool isDebugWindowVisible()
        {
            return debugDialog != null && debugDialog.Visible;
        }

        internal bool isDebugWindowFocused()
        {
#if WINDOWS
            if (!isDebugWindowVisible())
                return false;

            return Form.ActiveForm == debugDialog;
#else
            return false;
#endif
        }

        public string replaceForFormat(string str)
        {
            var result = str;
            result = result.Replace("\\\\", "\t");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\\([vhs$#H]|Variable)\[(.*?)\]", variableEvaluator);
            result = result.Replace("\t", "\\\\");
            return result;
        }

        private string variableEvaluator(System.Text.RegularExpressions.Match match)
        {
            if (match.Groups.Count < 3)
                return "";

            int index = 0;
            var gs = catalog.getGameSettings();
            switch (match.Groups[1].Value)
            {
                // 数値型変数のインデックス指定
                case "Variable":
                case "v":
                    int.TryParse(match.Groups[2].Value, out index);
                    return data.system.GetVariable(index, Guid.Empty).ToString();
                // 文字列型変数のインデックス指定
                case "s":
                    int.TryParse(match.Groups[2].Value, out index);
                    if (index < 0 || data.system.StrVariables.Length <= index)
                        return "";
                    return data.system.StrVariables[index];
                // 主人公のインデックス指定
                case "h":
                    int.TryParse(match.Groups[2].Value, out index);
                    var list = catalog.getFilteredItemList(typeof(Common.Rom.Hero));
                    if (list.Count <= index)
                        index = list.Count;
                    if (index < 0)
                        return "";
                    return data.party.getHeroName(list[index].guId);
                // 数値型変数の名前指定
                case "#":
                    foreach(var name in gs.variableNames)
                    {
                        if (name == match.Groups[2].Value)
                            break;

                        index++;
                    }
                    return data.system.GetVariable(index, Guid.Empty).ToString();
                // 文字列型変数の名前指定
                case "$":
                    foreach (var name in gs.strVariableNames)
                    {
                        if (name == match.Groups[2].Value)
                            break;

                        index++;
                    }
                    if (data.system.StrVariables.Length <= index)
                        return "";
                    return data.system.StrVariables[index];
                // 主人公の名前指定
                case "H":
                    var rom = catalog.getItemFromName(match.Groups[2].Value, typeof(Common.Rom.Hero));
                    if (rom == null)
                        return "";
                    return data.party.getHeroName(rom.guId);
            }

            return "";
        }

        private string multiEvaluator(System.Text.RegularExpressions.Match match)
        {
            var str = match.Value;
            int start = str.LastIndexOf('[') + 1;
            int end = str.LastIndexOf(']');
            var numStr = str.Substring(start, end - start);
            int num = 0;
            int.TryParse(numStr, out num);
            return data.system.GetVariable(num, Guid.Empty).ToString();
        }

        private string heroEvaluator(System.Text.RegularExpressions.Match match)
        {
            var str = match.Value;
            int start = str.LastIndexOf('[') + 1;
            int end = str.LastIndexOf(']');
            var numStr = str.Substring(start, end - start);
            int num = 0;
            int.TryParse(numStr, out num);
            var list = catalog.getFilteredItemList(typeof(Common.Rom.Hero));
            if (list.Count <= num)
                num = list.Count;
            if (num < 0)
                return "";
            return data.party.getHeroName(list[num].guId);
        }


#if ENABLE_VR

        //------------------------------------------------------------------------------
        /**
         *	VR描画
         */
        public static void DrawVr( Scenes scene )
        {
            if( scene == Scenes.NONE ) {
                return;
            }

            // VR有効時のみの描画処理
            if( SharpKmyVr.Func.IsReady() )
            {
                Graphics.BeginDraw();

#if VR_SIDE_BY_SIDE
                // 通常表示
//				if( scene == Scenes.MAP )
                {
                    int w = Graphics.ViewportWidth / 2;
                    int h = Graphics.ViewportHeight;

                    // 左目用
                    Graphics.DrawImage2( SharpKmyGfx.Render.getDefaultRenderL().getColorTexture(), 0, 0, w, h );

                    // 右目用
                    Graphics.DrawImage2( SharpKmyGfx.Render.getDefaultRenderR().getColorTexture(), w, 0, w, h );
                }
#endif  // #if VR_SIDE_BY_SIDE

                // 2D
                Graphics.DrawImageVr( SharpKmyGfx.Render.getRender2D().getColorTexture(), 0, 0, Graphics.ViewportWidth, Graphics.ViewportHeight );

                Graphics.EndDraw();
            }
        }

        //------------------------------------------------------------------------------
        /**
         *	現在のVRカメラデータを取得
         */
        private VrCameraData GetCurrentVrCameraData()
        {
            VrCameraData vrCameraData = null;

            switch( nowScene )
            {
//			case Scenes.LOGO:			vrCameraData = logoScene.GetVrCameraData();			break;
//			case Scenes.TITLE:			vrCameraData = titleScene.GetVrCameraData();		break;
            case Scenes.MAP:			vrCameraData = mapScene.GetVrCameraData();			break;
//			case Scenes.BATTLE_TEST:	vrCameraData = battleTestScene.GetVrCameraData();	break;
            }

            return vrCameraData;
        }
#endif  // #if ENABLE_VR




#if ENABLE_DISP_FPS
        float timeMinTmp = 999.0f;
        float timeMaxTmp = 0.0f;
        float timeAvgTmp = 60.0f;
        float timeMin = 999.0f;
        float timeMax = 0.0f;
        float timeAvg = 0.0f;
        float timer = 0.0f;
        const float _FrameTime = 1.0f / 60.0f;
        Vector2 _Pos = new Vector2(10, 10);

        //------------------------------------------------------------------------------
        /**
         *	FPS描画
         */
        private void DispFps( float elapsed )
        {
            // 情報更新
            if( elapsed > 0.0f )
            {
                float elapsed60F = 60.0f / (elapsed / _FrameTime);

                if( timeMinTmp > elapsed60F ) {
                    timeMinTmp = elapsed60F;
                }
                if( timeMaxTmp < elapsed60F ) {
                    timeMaxTmp = elapsed60F;
                }

                timeAvgTmp = (timeAvgTmp + elapsed60F) * 0.5f;

                timer += elapsed;
                if( timer >= 1.0f )
                {
                    timer -= 1.0f;
                    timeMin = timeMinTmp;
                    timeMax = timeMaxTmp;
                    timeAvg = timeAvgTmp;
                    timeMinTmp = 999.0f;
                    timeMaxTmp = 0.0f;
                }
            }

            // 描画
            {
                _Pos.X = 460-(20*8);
                _Pos.Y = 10;

                Graphics.DrawString( 1, string.Format("{0,7:F2}({1,7:F2}～{2,7:F2})", timeAvg, timeMin, timeMax), _Pos, Color.White, 0.75f );
                _Pos.Y += 20;
            }
        }
#endif  // ENABLE_DISP_FPS
    }
}

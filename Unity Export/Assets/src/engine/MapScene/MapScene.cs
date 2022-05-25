using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yukar.Common.GameData;
using Yukar.Common;
using Yukar.Engine;
//using Microsoft.Xna.Framework.Input;
using KeyStates = Yukar.Engine.Input.KeyStates;
using StateType = Yukar.Engine.Input.StateType;
using System.Collections;

namespace Yukar.Engine
{
    // 複数シーンのマップスクロール位置とキャラクター位置を管理するクラス
    public class MapScene
    {
        public const int PLAYER_LOCK_BY_EVENT = 0x10000;
        public const float GHOST_HERO_VISIBLE_DISTANCE = 0.5f;		// ゴースト移動時のキャラ表示切替を行う距離
        public const int EFFECT_2D_GROUND_OFFSET = 48;

        // メニューウィンドウ
        internal MenuController menuWindow = new MenuController();
        // メッセージウィンドウ
        internal MessageWindow messageWindow = new MessageWindow();
        internal const int MAX_MESSAGE_LINE = 3;
        // 選択肢ウィンドウ
        internal ChoicesWindow choicesWindow = new ChoicesWindow();
        // お金ウィンドウ
        internal MoneyWindow moneyWindow = new MoneyWindow();
        // お店ウィンドウ
        internal ShopWindow shopWindow = new ShopWindow();
        // 入力ウィンドウ
        internal InputStringWindow inputStringWindow = new InputStringWindow();
        // アイテムを捨てるウィンドウ
        internal ItemTrashController trashWindow = new ItemTrashController();
        private const float INPUT_WINDOW_OFFSET_Y = 50.0f;
        private const float INPUT_WINDOW_DEFAULT_HEIGHT = 50.0f;
        // トースト
        internal ToastWindow toastWindow = new ToastWindow();
        internal const int WINDOW_SHOW_FRAME = 12;
        internal const int WINDOW_CLOSE_FRAME = 12;

        // スプライト管理クラス
        internal SpriteManager spManager = new SpriteManager();

        // マップキャラ
        internal List<MapCharacter> mapCharList = new List<MapCharacter>();
        
        //同期
        internal ScriptRunner dialoguePushedRunner;
        internal ScriptRunner excludedScript;
        internal ScriptRunner cameraControlledScript;

        internal class ScriptRunnerDictionary
        {
            private Dictionary<Guid, ScriptRunner> dic = new Dictionary<Guid, ScriptRunner>();
            private List<ScriptRunner> list = new List<ScriptRunner>();

            internal bool ContainsKey(Guid guid)
            {
                return dic.ContainsKey(guid);
            }

            internal ScriptRunner this[Guid guid]
            {
                get
                {
                    return dic[guid];
                }
            }

            internal List<ScriptRunner> getList()
            {
                return list;
            }

            internal void Clear()
            {
                dic.Clear();
                list.Clear();
            }

            internal void Add(Guid guid, ScriptRunner runner)
            {
                dic.Add(guid, runner);
                list.Add(runner);
            }

            internal void Remove(Guid guid)
            {
                if (!ContainsKey(guid))
                    return;

                var entry = dic[guid];
                dic.Remove(guid);
                list.Remove(entry);
            }

            internal void bringToFront(ScriptRunner runner)
            {
                list.Remove(runner);
                list.Insert(0, runner);
            }
        }
        internal ScriptRunnerDictionary runnerDic = new ScriptRunnerDictionary();

        // スクリプト用
        internal Color screenColor = Color.Transparent;
        private Color gameoverColor = Color.Transparent;
        internal ScriptRunner exclusiveRunner;   // このスクリプトを除外
        internal bool exclusiveInverse;  // 除外スクリプト以外を実行する
        private ScriptRunner dummyRunner;

        public struct EffectDrawEntry
        {
            public EffectDrawer efDrawer;
            public int x;
            public int y;
        }
        internal Queue<EffectDrawEntry> effectDrawEntries = new Queue<EffectDrawEntry>();

        internal Common.Rom.Map map;
        internal Common.Rom.Map.BattleSetting battleSetting;
        internal MapData mapDrawer;
        internal MapCharacter hero;
        internal GameMain owner;
        internal Random rnd;

        internal int playerLocked;

        // 入力処理
        internal MapEngine mapEngine;

        internal bool isBattle;
        internal BattleSequenceManager battleSequenceManager;

        private Guid reservedMap;
        private int reservedX;
        private int reservedY;

        public bool isGameOver;
        private int gameOverImgId = -1;

        // カメラ関係のパラメータ
        Common.Rom.Map.CameraControlMode cameraControlMode;
        public float yangle = 0;
        public float xangle = -60;
        public float fovy = 5;
        public float dist = 70;
        public float eyeHeight = 0.9f;
        private SharpKmyMath.Vector3 lookAtTarget;
        private bool useLookAtTargetPos;
        public bool mapFixCamera;
        public bool mapFixCameraMode;

        // 保存しなくても良いカメラパラメータ
        bool mUsePerspective;
        float mOrthoHeight;
        float startAngleY, currentScrollAngleY, targetScrollAngleY;
        float scrollAngleY = 0;
        float orthozoom = 1;
        bool isCameraScrollByInput = false;
        bool isCameraScrollByEvent = false;
        public int isCameraMoveByCameraFunc = 0;

        SharpKmyMath.Matrix4 pp = SharpKmyMath.Matrix4.identity();
        SharpKmyMath.Matrix4 vv = SharpKmyMath.Matrix4.identity();
        SharpKmyMath.Vector2 mShakeVal = new SharpKmyMath.Vector2();

        // 画面シェイクの際にMapSceneのppを参照する必要があるためgetterが必要
        internal SharpKmyMath.Matrix4 CurrentPP { get { return pp; } }

        // エフェクトを無効化する視野角
        private const float DISABLE_FOV_4_EFFECTS = 90.0f;

#if ENABLE_VR
        private VrCameraData m_VrCameraData = new VrCameraData();   // VRカメラデータ
#endif  // #if ENABLE_VR

        public virtual SharpKmyMath.Vector2 ShakeValue
        {
            set { mShakeVal = value; }
        }

        internal bool IsMapChangedFrame { get; private set; }

        internal Common.Rom.Map.CameraControlMode CurrentCameraMode { get { return cameraControlMode; } }
        internal bool IsCameraScroll { get { return isCameraScrollByInput || isCameraScrollByEvent; } }

        SharpKmyIO.Controller controller;
        private GameMain.Scenes reservedScene = GameMain.Scenes.NONE;
        private bool isLoading;
        internal bool isBattleLoading;

        private enum CameraRotationDirection
        {
            ClockWise,
            CounterClockWise,
        }

        // ゲームオーバー用変数
        private enum FadeStatus
        {
            FadeIn,
            FadeOut,
            None,
        }
        private const int FADE_TIME = 60;
        private float fadeFrame = 0.0f;
        private FadeStatus fadeStatus = FadeStatus.None; // fadeの状態
        private ScriptRunner currentScriptRunner; // 最後に実行したスクリプト
        Action EndFadeIn; // fadein終了後に発火するイベント
        Action EndFadeOut; // fadeout終了後に発火するイベント
        Action EndExecEvent; //指定したイベント終了後に発火するイベント

        public void resetCamera(bool callByChangeCamera = false)
        {
            var tpCamera = getTpCamera();
            if (callByChangeCamera || !owner.data.start.camLockX) xangle = tpCamera.xAngle;
            if (callByChangeCamera || !owner.data.start.camLockY) yangle = tpCamera.yAngle;
            if (callByChangeCamera || !owner.data.start.camLockZoom) fovy = tpCamera.fov;
            dist = tpCamera.distance;
            orthozoom = 1;
            isCameraScrollByInput = false;
            useLookAtTargetPos = false;
        }

        private Common.Rom.ThirdPersonCameraSettings getTpCamera()
        {
            var tpCamera = map != null ? map.tpCamera : (Common.Rom.ThirdPersonCameraSettings)null;
            if (tpCamera == null)
            {
                tpCamera = owner.catalog.getGameSettings().tpCamera;
            }
            if (tpCamera == null)
            {
                tpCamera = new Common.Rom.ThirdPersonCameraSettings();
            }
            return tpCamera;
        }

        public void resetViewCamera()
        {
            var fpCamera = getFpCamera();
            fovy = fpCamera.fov;
            eyeHeight = fpCamera.eyeHeight;
            xangle = 0;
            yangle = CharacterDirectionToDegree(GetHero());
            isCameraScrollByInput = false;
        }

        private Common.Rom.FirstPersonCameraSettings getFpCamera()
        {
            var fpCamera = map != null ? map.fpCamera : (Common.Rom.FirstPersonCameraSettings)null;
            if (fpCamera == null)
            {
                fpCamera = owner.catalog.getGameSettings().fpCamera;
            }
            if (fpCamera == null)
            {
                fpCamera = new Common.Rom.FirstPersonCameraSettings();
            }
            return fpCamera;
        }

        internal static float CharacterDirectionToDegree(MapCharacter character)
        {
            // 上 下 左 右 getDirection => 上 左 下 右
            int[] cameraAngleTable = { 0, 2, 1, 3 };

            return 90 * cameraAngleTable[character.getDirection()];
        }

        internal static int DegreeToCharacterDirection(float yAngle)
        {
            yAngle %= 360;
            yAngle += 360 + 45;
            yAngle %= 360;

            int[] cameraAngleTable = { 0, 2, 1, 3, 0 };

            return cameraAngleTable[(int)(yAngle / 90)];
        }

        internal void ScrollCameraY(int degree)
        {
            if (degree == 0) return;

            float originalAngleX = xangle;
            float originalAnagleY = yangle;

            if (isCameraScrollByInput)
            {
                targetScrollAngleY += degree;
            }
            else
            {
                currentScrollAngleY = 0;

                targetScrollAngleY = CharacterDirectionToDegree(GetHero()) - originalAnagleY;

                // 360度と0度が同じ扱いのため360度を超えて回転する場合は計算式を変える
                if (originalAnagleY + degree > 360) // 0度を超える場合
                {
                    targetScrollAngleY = (360 - CharacterDirectionToDegree(GetHero())) - originalAnagleY + degree;

                    if (originalAnagleY + targetScrollAngleY <= 360)    // 0度で止まる場合
                    {
                        targetScrollAngleY = degree - (originalAnagleY - CharacterDirectionToDegree(GetHero()));
                    }
                }
                else
                {
                    targetScrollAngleY += degree;
                }

                // 1回転以上はさせない
                while (Math.Abs(targetScrollAngleY / 360) >= 1.0f)
                {
                    if (targetScrollAngleY > 0) targetScrollAngleY -= 360;
                    if (targetScrollAngleY < 0) targetScrollAngleY += 360;
                }

                startAngleY = originalAnagleY;
            }

            // 視野角と目の高さをマップの初期値に戻している
            // #25568で変更した値をそのままにするように変更した。
            //resetViewCamera();
            scrollAngleY = CharacterDirectionToDegree(GetHero()) + degree;
            xangle = originalAngleX;
            yangle = originalAnagleY;

            while (scrollAngleY < 0) scrollAngleY += 360;
            while (scrollAngleY >= 360) scrollAngleY -= 360;

            isCameraScrollByInput = true;
        }

        internal void EventScrollCameraY(int degree)
        {
            if (degree == 0) return;

            float originalAnagleY = yangle;

            resetViewCamera();

            startAngleY = yangle;
            targetScrollAngleY = degree;
            scrollAngleY = CharacterDirectionToDegree(GetHero()) + degree;

            yangle = originalAnagleY;

            while (scrollAngleY < 0) scrollAngleY += 360;
            while (scrollAngleY >= 360) scrollAngleY -= 360;

            isCameraScrollByEvent = true;
        }

        // 継承して使う用
        public MapScene()
        {
            owner = GameMain.instance;
            rnd = new Random();
            initializeMenu();
        }

        public MapScene(GameMain owner)
        {
            this.owner = owner;
            rnd = new Random();
            dummyRunner = new ScriptRunner(this, null, null);

            initializeMenu(true);

            mapEngine = new MapEngine();
            mapEngine.owner = this;

            isBattle = false;
            isGameOver = false;
            gameOverImgId = -1;

            // エフェクト読み込み
            // (戦闘開始に読み込む方が良いかも)
            foreach (Common.Rom.Effect effect in owner.catalog.getFilteredItemList(typeof(Yukar.Common.Rom.Effect)))
            {
                battleSequenceManager.RegisterTestEffect(effect.name, effect, owner.catalog);
            }

            cameraControlMode = Common.Rom.Map.CameraControlMode.NORMAL;
            mUsePerspective = true;
            mOrthoHeight = 15f;

            controller = new SharpKmyIO.Controller();
            controller.addInput("D", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'D', 1);
            controller.addInput("E", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'E', 1);
            controller.addInput("I", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'I', 1);
            controller.addInput("P", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'P', 1);
            controller.addInput("O", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'O', 1);
            controller.addInput("U", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'U', 1);
            controller.addInput("K", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'K', 1);
            controller.addInput("L", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'L', 1);
            controller.addInput("F12", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0x7B, 1);

            mapDrawer = new MapData();
            mapDrawer.mapDrawCallBack += new MapDrawCallBack(Draw);

#if ENABLE_VR
            // VRカメラデータ設定
            SetupVrCameraData(false);
#endif  // #if ENABLE_VR

            // コモンイベントの初期化
            mapEngine.LoadCommonEvents();

            resetCamera();
        }

        private void initializeMenu(bool withBattle = false)
        {
            var gs = owner.catalog.getGameSettings();

            var winRes = owner.catalog.getItemFromGuid(gs.messageWindow) as Common.Resource.Window;
            var winImgId = -1;
            if (winRes != null)
                winImgId = Graphics.LoadImage(winRes.path);

            var winRes2 = owner.catalog.getItemFromGuid(gs.menuWindow) as Common.Resource.Window;
            var winImgId2 = -1;
            if (winRes2 != null)
                winImgId2 = Graphics.LoadImage(winRes2.path);

            var winRes3 = owner.catalog.getItemFromGuid(gs.choicesWindow) as Common.Resource.Window;
            var winImgId3 = -1;
            if (winRes3 != null)
                winImgId3 = Graphics.LoadImage(winRes3.path);

            var selRes = owner.catalog.getItemFromGuid(gs.systemGraphics.menuSelected) as Common.Resource.Window;
            var selImgId = -1;
            if (selRes != null)
                selImgId = Graphics.LoadImage(selRes.path);

            var unselRes = owner.catalog.getItemFromGuid(gs.systemGraphics.menuSelectable) as Common.Resource.Window;
            var unselImgId = -1;
            if (unselRes != null)
                unselImgId = Graphics.LoadImage(unselRes.path);

            var pageRes = new Common.Resource.Character();// owner.catalog.getItemFromGuid(gs.pageIcon) as Common.Resource.Character;
            pageRes.path = ".\\res\\system\\message_wait.png";
            var pageImgId = Graphics.LoadImageDiv(pageRes.path, 2, 1);

            var scrollRes = owner.catalog.getItemFromGuid(gs.scrollIcon) as Common.Resource.Character;
            int scrollImgId = -1;
            if (scrollRes != null)
                scrollImgId = Graphics.LoadImageDiv(scrollRes.path, scrollRes.animationFrame, scrollRes.getDivY());

            menuWindow.parent = this;
            menuWindow.Initialize(winRes2, winImgId2, selRes, selImgId, unselRes, unselImgId, scrollRes, scrollImgId);
            messageWindow.parent = this;
            messageWindow.Initialize(winRes, winImgId, winRes2, winImgId2, pageRes, pageImgId);
            choicesWindow.parent = this;
            choicesWindow.Initialize(winRes3, winImgId3, selRes, selImgId);
            moneyWindow.parent = this;
            moneyWindow.Initialize(winRes2, winImgId2);
            shopWindow.parent = this;
            shopWindow.Initialize(winRes2, winImgId2, selRes, selImgId, unselRes, unselImgId, scrollRes, scrollImgId);
            toastWindow.parent = this;
            toastWindow.Initialize(winRes2, winImgId2);

            inputStringWindow.Initialize(winRes2, winImgId2, selRes, selImgId, unselRes, unselImgId, scrollRes, scrollImgId, menuWindow);
            trashWindow.initialize(this);

            spManager.owner = this;

            if(withBattle)
                battleSequenceManager = new BattleSequenceManager(owner, owner.catalog, new WindowDrawer(winRes, winImgId), new WindowDrawer(winRes, winImgId));
        }

        public void Reset()
        {
            if (hero != null) hero.Reset();
            if (mapDrawer != null) mapDrawer.Reset();
            if (menuWindow != null) menuWindow.Reset();
            if (shopWindow != null) shopWindow.Reset();

            foreach (var c in mapCharList)
            {
                c.Reset();
            }
            mapCharList.Clear();

            controller.Release();

            battleSequenceManager.finalize();
        }

        internal struct ChangeMapParams
        {
            internal Guid guid;
            internal int x;
            internal int y;
            internal bool createHero;
            internal List<Common.GameData.Event> eventStates;
            internal float height;
            internal CameraSettings camera;
            internal List<Common.GameData.Sprite> spriteStates;
            internal int dir;
            internal bool playerLock;
            internal bool cameraLockByEvent;
            internal bool cameraLock;
            internal bool cameraModeLock;

            internal static readonly ChangeMapParams defaultParams
                = new ChangeMapParams() { createHero = true, height = -1, dir = -1 };
            internal BgmPlaySettings bgmStatus;
            internal BgmPlaySettings bgsStatus;
        }

        internal void ChangeMap(ChangeMapParams p)
        {
#if WINDOWS
            var coroutine = changeMapImpl(p);
            while (coroutine.MoveNext()) ;
#else
            UnityEntry.startCoroutine(changeMapImpl(p));
#endif
        }

        internal void ChangeMapTiny(ChangeMapParams p)
        {
            UnityUtil.changeScene(UnityUtil.SceneType.MAP, true);
            map = owner.catalog.getItemFromGuid<Common.Rom.Map>(p.guid);
            battleSetting = map.mapBattleSetting;

            // 戦闘背景を準備しておく
            var coroutine = battleSequenceManager.prepare();
            while (coroutine.MoveNext()) ;
        }

        internal IEnumerator changeMapImpl(ChangeMapParams p)
        {
            var ONE_FRAME = 10000 * 16;
            var lastTime = DateTime.Now.Ticks;
            Func<bool> waiter = () =>
            {
                if (lastTime + ONE_FRAME < DateTime.Now.Ticks)
                {
                    return true;
                }
                return false;
            };

            Action reEntry = () =>
            {
                lastTime = DateTime.Now.Ticks;
            };

            isLoading = true;

#if WINDOWS
#else
            // 初回の遷移などでロード中に表示するためのフレームバッファがキャプチャされていない事があるので、黒で初期化する
            if (!UnityEntry.isFBCaptured())
            {
                UnityEntry.blackout();
            }
#endif

            UnityUtil.changeScene(UnityUtil.SceneType.MAP, true);

            reservedMap = Guid.Empty;
            IsMapChangedFrame = true;

            // フォントを念のため作りなおす
            //Graphics.refreshFont();

            var catalog = owner.catalog;

            var oldMode = Common.Rom.Map.CameraControlMode.NORMAL;
            if (map != null)
                oldMode = map.cameraMode;

            // 方向を覚えておく
            p.dir = hero == null ? p.dir : hero.getDirection();

            // マップデータ読み込み
            map = catalog.getItemFromGuid(p.guid) as Common.Rom.Map;
            if (map == null)
            {
                // マップがない場合は 0 番目を無理やり使ってみる
                var list = catalog.getFilteredItemList(typeof(Common.Rom.Map));
                if (list.Count == 0)
                    yield break;

                map = list[0] as Common.Rom.Map;
            }
            battleSetting = map.mapBattleSetting;
            mapFixCamera = map.fixCamera;
            mapFixCameraMode = map.fixCameraMode;

            // セーブデータから復帰した場合は、カメラとプレイヤーのロック状態を再現する
            if (p.playerLock)
                playerLocked |= PLAYER_LOCK_BY_EVENT;
            if (p.cameraLockByEvent)
                setCamLockState(p.cameraLock, p.cameraModeLock);

            // MapDrawer生成
            var coroutine = mapDrawer.setRomImpl(map, true);
            while (coroutine.MoveNext())
                if (waiter()) { yield return null; reEntry(); }

            // マップキャラクターの生成
            foreach (var c in mapCharList.ToArray())
            {
                if (c.isCommonEvent)
                    continue;

                c.Reset();
                mapCharList.Remove(c);
            }
            var callReserveRunner = exclusiveRunner;
            if (callReserveRunner != null && callReserveRunner.mapChr != null && callReserveRunner.mapChr.isCommonEvent)
            {
                // マップ移動の呼び出し元がコモンイベントだった場合、コモンイベントの呼び出し元を保護する
                var mapChr = callReserveRunner.followChr;
                callReserveRunner = null;
                foreach (var runner in runnerDic.getList())
                {
                    if (runner.mapChr == mapChr)
                    {
                        callReserveRunner = runner;
                        break;
                    }
                }
            }
            foreach (var runner in runnerDic.getList().ToArray())
            {
                if (callReserveRunner == runner || (runner.mapChr != null && runner.mapChr.isCommonEvent))
                    continue;

                runner.finalize();
                runnerDic.Remove(runner.script.guId);
            }
            // 前のマップで画面遷移を実行したスクリプトはそのまま動かす
            excludedScript = null;
			cameraControlledScript = null;
            if (callReserveRunner != null && callReserveRunner.script != null)
            {
                callReserveRunner.removeTrigger = ScriptRunner.RemoveTrigger.ON_EXIT;
            }
            ExclusionAllEvents(null);
            spManager.Clear();
            if (p.createHero)
            {
                // 主人公の透明状態をマップが変わっても引き継ぐためのコード
                //var heroOld = hero;
                mapEngine.clearPosLogList();
                mapEngine.CreateHeroChr(p.x, p.y, mapDrawer);
                if (p.dir >= 0)
                    hero.setDirection(p.dir, true);
                hero.mapHeroSymbol = true;
                if (p.height >= 0)
                    hero.y = p.height;
                hero.Update(mapDrawer, yangle, false);
            }

            coroutine = mapEngine.LoadMapCharacter();
            while (coroutine.MoveNext())
                if (waiter()) { yield return null; reEntry(); }

            // マップキャラクターの位置や方向などをセーブデータから再現する
            if (p.eventStates != null)
                loadEventState(p.eventStates);

            if (p.bgmStatus != null && p.bgmStatus.currentBgm != Common.Resource.Bgm.sNotChangeItem.guId)
            {
                // セーブ時に再生されていたBGMを再生
                var bgm = owner.catalog.getItemFromGuid(p.bgmStatus.currentBgm) as Common.Resource.Bgm;
                if (bgm != null)
                    Audio.PlayBgm(bgm, p.bgmStatus.volume, p.bgmStatus.tempo);
                else
                    Audio.StopBgm();

                var bgs = owner.catalog.getItemFromGuid(p.bgsStatus.currentBgm) as Common.Resource.Bgs;
                if (bgs != null)
                    Audio.PlayBgs(bgs, p.bgsStatus.volume, p.bgsStatus.tempo);
                else
                    Audio.StopBgs();
            }
            else
            {
                // 旧データだった場合、マップBGMを再生
                playMapBgm();
            }

            // カメラモード切り替え
            if (oldMode != map.cameraMode || mapFixCamera)
            {
                changeCameraMode(map.cameraMode);
            }
            useLookAtTargetPos = false; // 固定カメラはオフにしておく

            // マップのカメラ設定を反映(hero.visibleの操作は↑でカメラモードが切り替わっている場合は冗長だが、そのままにしておく)
            if (hero != null)
            {
                switch (cameraControlMode)
                {
                    case Common.Rom.Map.CameraControlMode.NORMAL:
                        hero.hide &= ~MapCharacter.HideCauses.BY_CAMERA;

                        // カメラ設定を反映
                        if (map.cameraMode == Common.Rom.Map.CameraControlMode.NORMAL && map.tpCamera != null)
                        {
                            xangle = map.tpCamera.xAngle;
                            yangle = map.tpCamera.yAngle;
                            fovy = map.tpCamera.fov;
                            dist = map.tpCamera.distance;
                        }
                        break;

                    case Common.Rom.Map.CameraControlMode.VIEW:
                        hero.hide |= MapCharacter.HideCauses.BY_CAMERA;

                        // カメラ設定を反映
                        if (map.cameraMode == Common.Rom.Map.CameraControlMode.VIEW && map.fpCamera != null)
                        {
                            fovy = map.fpCamera.fov;
                            eyeHeight = map.fpCamera.eyeHeight;
                        }
                        break;

#if ENABLE_GHOST_MOVE
                    case Common.Rom.Map.CameraControlMode.GHOST:
                        hero.visibilityForHero = true;

                        // カメラ設定を反映
                        if (map.cameraMode == Common.Rom.Map.CameraControlMode.NORMAL && map.tpCamera != null)
                        {
                            xangle = map.tpCamera.xAngle;
                            yangle = map.tpCamera.yAngle;
                            fovy = map.tpCamera.fov;
                            dist = map.tpCamera.distance;
                        }
                        break;
#endif  // #if ENABLE_GHOST_MOVE
                }
            }

            // カメラ設定があれば読み込み
            if (p.camera != null)
                loadCameraSettings(p.camera);

#if ENABLE_VR
            // VRカメラ同期
            SyncCamera();
#endif  // #if ENABLE_VR

            // スプライト設定があれば読み込み
            if (p.spriteStates != null)
                spManager.load(p.spriteStates);

            // 戦闘背景を準備しておく
            coroutine = battleSequenceManager.prepare();
            while (coroutine.MoveNext()) yield return null;

            // 明転前に１フレームぶんスクリプトを処理しておく
            ScriptRunner.sTimeoutTick = ScriptRunner.INITIAL_TIMEOUT;
            ProcScript();
            ScriptRunner.sTimeoutTick = ScriptRunner.DEFAULT_TIMEOUT;

            // ↑を受けて状態が変わっているかもしれないのですぐに反映する
            mapEngine.checkAllEvent();

            isLoading = false;

#if WINDOWS
#else
            UnityEntry.reserveClearFB();
#endif
        }
        
        internal void updateMapGeometry()
        {
            mapDrawer.Update(GameMain.getElapsedTime());
        }

        internal void Update()
        {
            if (isLoading || isBattleLoading)
                return;

            IsMapChangedFrame = false;

            if (fadeStatus != FadeStatus.None)
            {
                RunFade();
            }
            if (currentScriptRunner != null)
            {
                if (currentScriptRunner.state != ScriptRunner.ScriptState.Running)
                {
                    runnerDic.Remove(currentScriptRunner.script.guId);
                    currentScriptRunner = null;
                    if (EndExecEvent != null) EndExecEvent();
                }
            }

            // 予約したマップに移動
            if (reservedMap != Guid.Empty)
            {
                var p = ChangeMapParams.defaultParams;
                p.guid = reservedMap;
                p.x = reservedX;
                p.y = reservedY;
                ChangeMap(p);
            }

            // 予約したシーン遷移を実行
            if (reservedScene != GameMain.Scenes.NONE && !IsMapChangedFrame)
            {
                owner.ChangeScene(reservedScene);
                reservedScene = GameMain.Scenes.NONE;
                return;
            }

            float angleY = (isCameraScrollByInput) ? scrollAngleY : yangle;

            if (!battleSequenceManager.IsDrawingBattleScene)
            {
                UnityUtil.changeScene(UnityUtil.SceneType.MAP);

                updateMapGeometry();

                Input.SetVirtualPadEnable();

                bool isEventProcessed = false;
                if (!isGameOver && !menuWindow.isVisible())
                    isEventProcessed = ProcScript();

                bool isLockCharacterDirection = false;

                switch (cameraControlMode)
                {
                    case Common.Rom.Map.CameraControlMode.NORMAL: isLockCharacterDirection = false; break;
                    case Common.Rom.Map.CameraControlMode.VIEW: isLockCharacterDirection = true; break;
#if ENABLE_GHOST_MOVE
                    case Common.Rom.Map.CameraControlMode.GHOST: isLockCharacterDirection = false; break;
#endif  // #if ENABLE_GHOST_MOVE
                }

                // キャラクタ座標とイベントシートのアップデート
                foreach (var mapChr in mapCharList)
                {
                    if (!isEventProcessed && mapChr.rom != null)
                        mapEngine.checkAllSheet(mapChr);
                    mapChr.Update(mapDrawer, angleY, isLockCharacterDirection);
                }

                // 移動可能座標リストを更新する
                mapEngine.updateEventHeightMap();
            }

            mapEngine.UpdatePlayer(mapDrawer, angleY);

            // ウィンドウ更新
            toastWindow.Update();
            if (!isToastVisible())
            {
                menuWindow.Update();
                shopWindow.Update();
                choicesWindow.Update();
                messageWindow.SetKeepWindowFlag(choicesWindow.IsActive());
                messageWindow.Update();
                moneyWindow.Update();
                inputStringWindow.Update();
                trashWindow.update();
            }

            spManager.Update();

            if (owner.data.system.menuAvailable && playerLocked == 0 && !hero.IsMoving() && !menuWindow.IsClosing() &&
                !isBattle && Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CANCEL))
                menuWindow.Show();

            if (isBattle)
            {
                battleSequenceManager.Update();
            }

            // 視点の変更
            if (!isBattle && !isCameraScrollByEvent && isCameraMoveByCameraFunc == 0 &&
                (!hero.IsRotating() || cameraControlMode != Common.Rom.Map.CameraControlMode.VIEW))
            {
                if (!mapFixCamera)
                {
                    if (!owner.data.start.camLockX && Input.KeyTest(StateType.DIRECT, KeyStates.CAMERA_VERTICAL_ROT_UP))
                    {
                        switch (cameraControlMode)
                        {
                            case Common.Rom.Map.CameraControlMode.NORMAL:
                                xangle -= 1 * GameMain.getRelativeParam60FPS();
#if WINDOWS
                                xangle = Math.Max(xangle, Math.Min(-89.9999f, getTpCamera().xAngle));
#else
                                xangle = Math.Max(xangle, Math.Min(-89.999f, getTpCamera().xAngle));
#endif
                                break;

                            case Common.Rom.Map.CameraControlMode.VIEW:
                                xangle += 2 * GameMain.getRelativeParam60FPS();

                                xangle = Math.Min(xangle, 270);
                                break;
                        }
                    }
                    else if (!owner.data.start.camLockX && Input.KeyTest(StateType.DIRECT, KeyStates.CAMERA_VERTICAL_ROT_DOWN))
                    {
                        switch (cameraControlMode)
                        {
                            case Common.Rom.Map.CameraControlMode.NORMAL:
#if ENABLE_GHOST_MOVE
                            case Common.Rom.Map.CameraControlMode.GHOST:
#endif  // #if ENABLE_GHOST_MOVE
                                xangle += 1 * GameMain.getRelativeParam60FPS();
                                xangle = Math.Min(xangle, Math.Max(-35, getTpCamera().xAngle));
                                break;

                            case Common.Rom.Map.CameraControlMode.VIEW:
                                xangle -= 2 * GameMain.getRelativeParam60FPS();
                                xangle = Math.Max(xangle, -270);
                                break;
                        }
                    }

                    if (!owner.data.start.camLockY && Input.KeyTest(StateType.DIRECT, KeyStates.CAMERA_HORIZONTAL_ROT_CLOCKWISE))
                    {
                        switch (cameraControlMode)
                        {
                            case Common.Rom.Map.CameraControlMode.NORMAL: yangle -= 2 * GameMain.getRelativeParam60FPS(); break;
                            case Common.Rom.Map.CameraControlMode.VIEW: yangle += 2 * GameMain.getRelativeParam60FPS(); break;
#if ENABLE_GHOST_MOVE
                            case Common.Rom.Map.CameraControlMode.GHOST: yangle -= 1 * GameMain.getRelativeParam60FPS(); break;
#endif  // #if ENABLE_GHOST_MOVE
                        }
                    }
                    else if (!owner.data.start.camLockY && Input.KeyTest(StateType.DIRECT, KeyStates.CAMERA_HORIZONTAL_ROT_COUNTER_CLOCKWISE))
                    {
                        switch (cameraControlMode)
                        {
                            case Common.Rom.Map.CameraControlMode.NORMAL: yangle += 2 * GameMain.getRelativeParam60FPS(); break;
                            case Common.Rom.Map.CameraControlMode.VIEW: yangle -= 2 * GameMain.getRelativeParam60FPS(); break;
#if ENABLE_GHOST_MOVE
                            case Common.Rom.Map.CameraControlMode.GHOST: yangle += 1 * GameMain.getRelativeParam60FPS(); break;
#endif  // #if ENABLE_GHOST_MOVE
                        }
                    }

#if ENABLE_VR
                    if (!SharpKmyVr.Func.IsReady())
#endif  // #if ENABLE_VR
                    {
                        if (!owner.data.start.camLockZoom && Input.KeyTest(StateType.DIRECT, KeyStates.CAMERA_ZOOM_IN))
                        {
                            switch (cameraControlMode)
                            {
                                case Common.Rom.Map.CameraControlMode.NORMAL:
#if ENABLE_GHOST_MOVE
                                case Common.Rom.Map.CameraControlMode.GHOST:
#endif  // #if ENABLE_GHOST_MOVE
                                    if (mUsePerspective) fovy -= 0.05f * GameMain.getRelativeParam60FPS();
                                    else orthozoom -= 0.005f * GameMain.getRelativeParam60FPS();

                                    if (fovy < getTpCamera().fov / 10) fovy = getTpCamera().fov / 10;
                                    if (orthozoom < 0.5) orthozoom = 0.5f;
                                    break;

                                case Common.Rom.Map.CameraControlMode.VIEW:
                                    if (mUsePerspective) fovy -= 0.25f * GameMain.getRelativeParam60FPS();
                                    else orthozoom -= 0.01f * GameMain.getRelativeParam60FPS();

                                    if (fovy < Math.Min(5, getFpCamera().fov)) fovy = Math.Min(5, getFpCamera().fov);
                                    if (orthozoom < 0.5) orthozoom = 0.5f;
                                    break;
                            }
                        }
                        else if (!owner.data.start.camLockZoom && Input.KeyTest(StateType.DIRECT, KeyStates.CAMERA_ZOOM_OUT))
                        {
                            switch (cameraControlMode)
                            {
                                case Common.Rom.Map.CameraControlMode.NORMAL:
#if ENABLE_GHOST_MOVE
                                case Common.Rom.Map.CameraControlMode.GHOST:
#endif  // #if ENABLE_GHOST_MOVE
                                    if (mUsePerspective) fovy += 0.05f * GameMain.getRelativeParam60FPS();
                                    else orthozoom += 0.005f * GameMain.getRelativeParam60FPS();

                                    if (fovy >= getTpCamera().fov) fovy = getTpCamera().fov;
                                    if (orthozoom > 1.5) orthozoom = 1.5f;
                                    break;

                                case Common.Rom.Map.CameraControlMode.VIEW:
                                    if (mUsePerspective) fovy += 0.25f * GameMain.getRelativeParam60FPS();
                                    else orthozoom += 0.01f * GameMain.getRelativeParam60FPS();

                                    if (fovy >= Math.Max(45, getFpCamera().fov)) fovy = Math.Max(45, getFpCamera().fov);
                                    if (orthozoom > 1.5) orthozoom = 1.5f;
                                    break;
                            }
                        }
                    }

                    if (Input.KeyTest(StateType.TRIGGER, KeyStates.CAMERA_POSITION_RESET))
                    {
                        switch (cameraControlMode)
                        {
                            case Common.Rom.Map.CameraControlMode.NORMAL:
#if ENABLE_GHOST_MOVE
                            case Common.Rom.Map.CameraControlMode.GHOST:
#endif  // #if ENABLE_GHOST_MOVE
                                resetCamera();
                                break;

                            case Common.Rom.Map.CameraControlMode.VIEW:
                                resetViewCamera();
                                break;
                        }
                    }
                }

                if (!mapFixCameraMode)
                {
                    if (Input.KeyTest(StateType.TRIGGER, KeyStates.CAMERA_CONTROL_MODE_CHANGE))
                    {
#if true
                        Common.Rom.Map.CameraControlMode cameraControlModeNew = cameraControlMode + 1;
                        if (cameraControlModeNew >= Common.Rom.Map.CameraControlMode.NUM)
                        {
                            cameraControlModeNew = (Common.Rom.Map.CameraControlMode)0;
                        }
                        changeCameraMode(cameraControlModeNew);
#else
                        switch (cameraControlMode)
                        {
                            case Common.Rom.Map.CameraControlMode.NORMAL:
                                changeCameraMode(Common.Rom.Map.CameraControlMode.VIEW);
                                break;
                            case Common.Rom.Map.CameraControlMode.VIEW:
                                changeCameraMode(Common.Rom.Map.CameraControlMode.NORMAL);
                                break;
                        }
#endif
                    }
                }
            }

#if ENABLE_GHOST_MOVE
            // キャラ表示設定（ゴースト時）
            if( cameraControlMode == Common.Rom.Map.CameraControlMode.GHOST )
            {
                MapCharacter hero = GetHero();
                SharpKmyMath.Vector3 pos = m_VrCameraData.m_CameraPos;
                SharpKmyMath.Vector3 vec = new SharpKmyMath.Vector3( hero.x-pos.x, hero.y+eyeHeight-pos.y, hero.z-pos.z );
                hero.visibilityForHero = (vec.length() > GHOST_HERO_VISIBLE_DISTANCE);
            }
#endif  // #if ENABLE_GHOST_MOVE

            if (isCameraScrollByEvent)
            {
                float prevAngleY = currentScrollAngleY;

                if (currentScrollAngleY < targetScrollAngleY)
                {
                    currentScrollAngleY += (4 * GameMain.getRelativeParam60FPS());
                }
                else if (currentScrollAngleY > targetScrollAngleY)
                {
                    currentScrollAngleY -= (4 * GameMain.getRelativeParam60FPS());
                }

                yangle = startAngleY + currentScrollAngleY;

                if ((prevAngleY <= targetScrollAngleY && currentScrollAngleY >= targetScrollAngleY) || (prevAngleY >= targetScrollAngleY && currentScrollAngleY <= targetScrollAngleY))
                {
                    yangle = scrollAngleY;

                    currentScrollAngleY = 0;
                    targetScrollAngleY = 0;

                    isCameraScrollByEvent = false;
                }

                while (yangle >= 360) yangle -= 360;
                while (yangle < 0) yangle += 360;
            }
            else if (isCameraScrollByInput)
            {
                float prevAngle = currentScrollAngleY;

                if (currentScrollAngleY < targetScrollAngleY)
                {
                    currentScrollAngleY += (3 * GameMain.getRelativeParam60FPS());
                }
                else if (currentScrollAngleY > targetScrollAngleY)
                {
                    currentScrollAngleY -= (3 * GameMain.getRelativeParam60FPS());
                }

                yangle = startAngleY + currentScrollAngleY;

                if ((prevAngle <= targetScrollAngleY && currentScrollAngleY >= targetScrollAngleY) || (prevAngle >= targetScrollAngleY && currentScrollAngleY <= targetScrollAngleY))
                {
                    yangle = scrollAngleY;

                    currentScrollAngleY = 0;
                    targetScrollAngleY = 0;

                    isCameraScrollByInput = false;
                }
            }

            while (yangle >= 360) yangle -= 360;
            while (yangle < 0) yangle += 360;

            if (isGameOver)
                ProcGameOver();
        }

        internal void changeCameraMode(Common.Rom.Map.CameraControlMode targetMode)
        {
            switch (targetMode)
            {
                case Common.Rom.Map.CameraControlMode.NORMAL:
                    mapEngine.setCursorVisibility(true);  // カーソルを表示する
                    resetCamera(true);
                    hero.hide &= ~MapCharacter.HideCauses.BY_CAMERA;
                    MapData.sInstance.isExpandViewRange = false;
                    cameraControlMode = Common.Rom.Map.CameraControlMode.NORMAL;
                    break;

                case Common.Rom.Map.CameraControlMode.VIEW:
                    resetViewCamera();
                    hero.hide |= MapCharacter.HideCauses.BY_CAMERA;
                    MapData.sInstance.isExpandViewRange = true;
                    cameraControlMode = Common.Rom.Map.CameraControlMode.VIEW;
                    break;

#if ENABLE_GHOST_MOVE
                case Common.Rom.Map.CameraControlMode.GHOST:
                    resetViewCamera();
                    hero.hide &= ~MapCharacter.HideCauses.BY_CAMERA;
                    MapData.sInstance.isExpandViewRange = true;
                    cameraControlMode = Common.Rom.Map.CameraControlMode.GHOST;
                    break;
#endif  // #if ENABLE_GHOST_MOVE
            }

            // カメラモード設定
            map.cameraMode = targetMode;

#if ENABLE_VR
            // VR設定
            {
                // カメラ同期
                SyncCamera();

                // VRカメラ情報更新
                m_VrCameraData.UpdateInfo(map.cameraMode);

                // VRキャリブレーション
                m_VrCameraData.Calibration();
            }
#endif // #if ENABLE_VR
        }

        private void playMapBgm()
        {
            // BGM・BGS再生
            owner.clearTask("Fanfare");
            var bgm = owner.catalog.getItemFromGuid(map.mapBgm) as Common.Resource.Bgm;
            if (map.mapBgm == Common.Resource.Bgm.sNotChangeItem.guId)
            {
                // 何もしない 
            }
            else if (bgm != null)
                Audio.PlayBgm(bgm);
            else
                Audio.StopBgm();
            var bgs = owner.catalog.getItemFromGuid(map.mapBgs) as Common.Resource.Bgs;
            if (map.mapBgs == Common.Resource.Bgm.sNotChangeItem.guId)
            {
                // 何もしない 
            }
            else if (bgs != null)
                Audio.PlayBgs(bgs);
            else
                Audio.StopBgs();
        }

        private bool ProcScript(bool procSingle = true)
        {
            UnityUtil.changeScene(UnityUtil.SceneType.MAP);

            bool result = false;

            // 通常スクリプトのアップデート
            var runners = runnerDic.getList().ToArray();
            foreach (var runner in runners)
            {
                if (exclusiveRunner == null || (runner == exclusiveRunner && !exclusiveInverse) ||
                    (runner != exclusiveRunner && exclusiveInverse))
                {
                    if (!result && !runner.isParallelTriggers())
                    {
                        bool isFinished = runner.Update();
                        if (isFinished)     // 完了したスクリプトがある場合は、ページ遷移をチェックする
                            break;
                        if (procSingle && runner.state == ScriptRunner.ScriptState.Running)
                            result = true;  // 並列動作しないので、自動移動以外は最初に見つかったRunningしか実行しない
                    }
                }
            }

            // 自動移動スクリプトのアップデート
            if (!result && playerLocked == 0)
            {
                foreach (var runner in runners)
                {
                    if (runner.Trigger == Common.Rom.Script.Trigger.PARALLEL_MV)
                    {
                        runner.Update();
                    }
                }

            }

            // その他の並列スクリプトのアップデート
            foreach (var runner in runners)
            {
                if (exclusiveRunner == null || (runner == exclusiveRunner && !exclusiveInverse) ||
                    (runner != exclusiveRunner && exclusiveInverse))
                {
                    if (runner.Trigger == Common.Rom.Script.Trigger.NONE ||
                        runner.Trigger == Common.Rom.Script.Trigger.PARALLEL)
                    {
                        runner.Update();
                    }
                }
            }

            return result;
        }
        
        internal void ProcParallelScript()
        {
            UnityUtil.changeScene(UnityUtil.SceneType.MAP);

            // その他の並列スクリプトのアップデート
            var runners = runnerDic.getList().ToArray();
            foreach (var runner in runners)
            {
                if (exclusiveRunner != dummyRunner)
                    break;

                if (runner.Trigger == Common.Rom.Script.Trigger.NONE ||
                    runner.Trigger == Common.Rom.Script.Trigger.PARALLEL)
                {
                    runner.Update();
                }
            }
        }

        //------------------------------------------------------------------------------
        /**
         *	描画処理
         */
        internal void Draw(SharpKmyGfx.Render scn, bool isShadowScene)
        {
            // キャラクタを描画
            foreach (var mapChr in mapCharList)
            {
                if (isShadowScene && mapChr.isBillboard()) continue;

                mapChr.draw(scn);
            }

#if ENABLE_VR
            // 2D表示（カメラ前方に板ポリを配置）
            if (SharpKmyVr.Func.IsReady())
            {
                if (SharpKmyGfx.Render.isSameScene(scn, SharpKmyGfx.Render.getRenderL()) ||
                    SharpKmyGfx.Render.isSameScene(scn, SharpKmyGfx.Render.getRenderR()))
                {
                    // カメラ座標
                    SharpKmyMath.Vector3 posCam = new SharpKmyMath.Vector3();
                    switch (cameraControlMode)
                    {
                        case Common.Rom.Map.CameraControlMode.NORMAL:
                            posCam = (SharpKmyMath.Matrix4.rotateY(MathHelper.ToRadians(yangle)) * SharpKmyMath.Matrix4.rotateX(MathHelper.ToRadians(xangle)) * (new SharpKmyMath.Vector3(0, 0, dist))).getXYZ();
                            {
                                SharpKmyMath.Vector3 hmdPos = SharpKmyVr.Func.GetHmdPosePos() * m_VrCameraData.GetHakoniwaScale();
                                SharpKmyMath.Matrix4 mtxTmp1 = SharpKmyMath.Matrix4.translate(hmdPos.x, hmdPos.y, hmdPos.z);
                                posCam = (m_VrCameraData.GetCombinedOffsetRotateMatrix() * mtxTmp1).translation();
                                posCam += m_VrCameraData.GetCombinedOffsetPos();
                            }
                            break;

                        case Common.Rom.Map.CameraControlMode.VIEW:
                            {
                                //							MapCharacter h = GetHero();
                                if (hero != null)
                                {
                                    posCam = (SharpKmyMath.Matrix4.translate((hero.x + hero.offsetX), hero.y + hero.offsetY + eyeHeight, (hero.z + hero.offsetZ)) * SharpKmyMath.Matrix4.rotateY(MathHelper.ToRadians(yangle)) * SharpKmyMath.Matrix4.rotateX(MathHelper.ToRadians(xangle))).translation();
                                    posCam += SharpKmyVr.Func.GetHmdPosePos() + m_VrCameraData.GetCombinedOffsetPos();
                                    //								posCam.y -= eyeHeight;
                                }
                            }
                            break;

#if ENABLE_GHOST_MOVE
                    case Common.Rom.Map.CameraControlMode.GHOST:
                        {
                            MapCharacter h = GetHero();
                            SharpKmyMath.Vector3 posOffset = m_VrCameraData.GetOutOffsetPos();
                            posCam = (SharpKmyMath.Matrix4.translate(posOffset.x, posOffset.y+eyeHeight, posOffset.z) * SharpKmyMath.Matrix4.rotateY(MathHelper.ToRadians(yangle)) * SharpKmyMath.Matrix4.rotateX(MathHelper.ToRadians(xangle))).translation();
                            SharpKmyMath.Vector3 posTmp = SharpKmyVr.Func.GetHmdPosePos();
                            posCam	 += (m_VrCameraData.GetCombinedOffsetRotateMatrix() * SharpKmyMath.Matrix4.translate(posTmp.x,posTmp.y,posTmp.z)).translation() + m_VrCameraData.GetCombinedOffsetPos();
//							posCam.y -= eyeHeight;
                        }
                        break;
#endif  // #if ENABLE_GHOST_MOVE
                    }

                    SharpKmyGfx.Color col;

                    // 3Dバトル時、特定のバトルステートの場合のみ例外的にこちらで塗りつぶし板ポリを描画
                    if (!owner.IsBattle2D)
                    {
                        BattleSequenceManager.BattleState state = battleSequenceManager.GetBattleState();
                        if (state == BattleSequenceManager.BattleState.StartFlash ||
                            state == BattleSequenceManager.BattleState.StartFadeOut ||
                            state == BattleSequenceManager.BattleState.FinishFadeIn)
                        {
                            col = battleSequenceManager.GetFadeScreenColor();
                            VrDrawer.DrawVrFillRectPolygon(scn, posCam, col, false, 1, m_VrCameraData);
                        }
                    }

                    // 2D板ポリ描画
                    col = new SharpKmyGfx.Color(
                        (float)screenColor.R / 255.0f,
                        (float)screenColor.G / 255.0f,
                        (float)screenColor.B / 255.0f,
                        (float)screenColor.A / 255.0f
                        );
                    VrDrawer.DrawVr2dPolygon(scn, posCam, col, true, m_VrCameraData);
                }
            }
#endif // #if ENABLE_VR
        }

        //------------------------------------------------------------------------------
        /**
         *	描画処理
         */
        internal void Draw()
        {
            if (isLoading || isBattleLoading)
            {
                // スクリーンカラーを適用
                Graphics.DrawFillRect(0, 0, Graphics.ViewportWidth, Graphics.ViewportHeight,
                    screenColor.R, screenColor.G, screenColor.B, screenColor.A);

                owner.DrawLoadingText();
                return;
            }

            if (mapDrawer != null && (owner.IsBattle2D || !battleSequenceManager.IsDrawingBattleScene))
            {
                UnityUtil.changeScene(UnityUtil.SceneType.MAP);

                Graphics.BeginDraw();

                createCameraMatrix(out pp, out vv);

                mapEngine.draw();

                // スプライト描画
                spManager.Draw(0, SpriteManager.SYSTEM_SPRITE_INDEX);

                // エフェクト描画
                while (effectDrawEntries.Count > 0)
                {
                    var entry = effectDrawEntries.Dequeue();
                    entry.efDrawer.draw(entry.x, entry.y);
                }

                // スクリーンカラーを適用
                Graphics.DrawFillRect(0, 0, Graphics.ViewportWidth, Graphics.ViewportHeight,
                    screenColor.R, screenColor.G, screenColor.B, screenColor.A);

                Graphics.EndDraw();

#if ENABLE_VR
                //シャドウマトリクスなどの設定を描画前に行う。
                SharpKmyGfx.Render.getDefaultRender().setViewMatrix(pp, vv);
                mapDrawer.afterViewPositionFixProc(SharpKmyGfx.Render.getDefaultRender());

                if (SharpKmyVr.Func.IsReady())
                {
                    // VR描画
                    VrDrawer.DrawVr(mapDrawer, m_VrCameraData, pp, vv);
                }
                else
                {
                    // 通常描画
                    SharpKmyGfx.Render.getDefaultRender().addDrawable(mapDrawer);
                }
#else // #if ENABLE_VR
                SharpKmyGfx.Render.getDefaultRender().setViewMatrix( pp, vv );
                SharpKmyGfx.Render.getDefaultRender().addDrawable( mapDrawer );
#endif  // #if ENABLE_VR
            }

            if (battleSequenceManager != null && isBattle)
            {
                battleSequenceManager.Draw();
            }

            Graphics.BeginDraw();

            // スプライト描画
            spManager.Draw(SpriteManager.SYSTEM_SPRITE_INDEX, SpriteManager.MAX_SPRITE);

            // ゲームオーバー時はゲームオーバー用イメージをオーバーレイ
            if (isGameOver)
            {
                DrawGameOver();
            }

            // ウィンドウ描画
            messageWindow.Draw();
            choicesWindow.Draw();
            moneyWindow.Draw();
            shopWindow.Draw();
            menuWindow.Draw();
            toastWindow.Draw();
            inputStringWindow.Draw();
            trashWindow.draw();

#if WINDOWS
            if (reservedMap != Guid.Empty)
            {
                owner.DrawLoadingText();
            }
#endif

            Graphics.EndDraw();

#if WINDOWS
#else
            if (reservedMap != Guid.Empty)
            {
                UnityEntry.capture();
            }
#endif
        }

        //------------------------------------------------------------------------------
        /**
         *	カメラ行列の生成
         */
        private void createCameraMatrix(out SharpKmyMath.Matrix4 p, out SharpKmyMath.Matrix4 v)
        {
            //主人公を見るマトリクスを設定
            MapCharacter h = GetHero();
            float asp = (float)owner.getScreenAspect();

            /////////////
            float camDistance = 0;
            float nearclip = 0;
            float farclip = 0;
            SharpKmyMath.Vector3 vecUp = new SharpKmyMath.Vector3(0, 1, 0);

            SharpKmyMath.Vector3 _campos = new SharpKmyMath.Vector3(), _ppos = new SharpKmyMath.Vector3(), _lookat = new SharpKmyMath.Vector3(), _target = new SharpKmyMath.Vector3();

#if ENABLE_VR
            if (SharpKmyVr.Func.IsReady())
            {
                SharpKmyMath.Matrix4 mtxTmp = m_VrCameraData.GetCombinedOffsetRotateMatrix() * SharpKmyVr.Func.GetHmdPoseRotateMatrix() * SharpKmyMath.Matrix4.translate(0, 1, 0);
                vecUp.x = mtxTmp.m30;
                vecUp.y = mtxTmp.m31;
                vecUp.z = mtxTmp.m32;
                vecUp = SharpKmyMath.Vector3.normalize(vecUp);
            }
#endif // #if ENABLE_VR

            switch (cameraControlMode)
            {
                case Common.Rom.Map.CameraControlMode.NORMAL:
                    camDistance = dist;
#if ENABLE_VR_UNITY
                    camDistance *= 0.1f;
#endif

                    nearclip = camDistance - 50 > 0 ? camDistance - 50 : 1;
                    farclip = camDistance * 3 > 100 ? camDistance * 3 : 100;

                    _campos = (SharpKmyMath.Matrix4.rotateY(MathHelper.ToRadians(yangle)) * SharpKmyMath.Matrix4.rotateX(MathHelper.ToRadians(xangle)) * (new SharpKmyMath.Vector3(0, 0, camDistance))).getXYZ();
                    _ppos = GetLookAtTarget();
#if WINDOWS
#else
                    // イベント等のカメラワークが左右反転しているのを修正
                    _campos.x *= -1;
                    // 地形の高さの変位とカメラの高さの変位が±逆になっていたのを修正
                    _ppos.y *= -1;
#endif
                    _lookat = _ppos + _campos;
                    _target = _ppos;

#if ENABLE_VR
                    if (SharpKmyVr.Func.IsReady())
                    {
                        SharpKmyMath.Vector3 hmdPos = SharpKmyVr.Func.GetHmdPosePos() * m_VrCameraData.GetHakoniwaScale();
                        SharpKmyMath.Matrix4 mtxTmp1 = SharpKmyMath.Matrix4.translate(hmdPos.x, hmdPos.y, hmdPos.z);
                        _campos = (m_VrCameraData.GetCombinedOffsetRotateMatrix() * mtxTmp1).translation();
                        _campos += m_VrCameraData.GetCombinedOffsetPos();

                        SharpKmyMath.Vector3 dirTmp = SharpKmyVr.Func.GetHmdPoseDirection();
                        SharpKmyMath.Matrix4 mtxTmp2 = m_VrCameraData.GetCombinedOffsetRotateMatrix() * SharpKmyMath.Matrix4.translate(dirTmp.x, dirTmp.y, dirTmp.z);

                        _lookat = _campos;
                        _target = _campos + mtxTmp2.translation();

                        nearclip = 0.1f;
                        farclip = 100.0f;
                        fovy = 25.0f;
                        mUsePerspective = true;
                    }
#endif // #if ENABLE_VR
                    break;

                case Common.Rom.Map.CameraControlMode.VIEW:
                    camDistance = 0;
                    nearclip = 0.1f;
                    farclip = 100;

#if WINDOWS
                    var xAngle = xangle;
#else
                    var xAngle = -xangle;
#endif

                    _campos = (SharpKmyMath.Matrix4.translate((h.x + h.offsetX), h.y + h.offsetY + eyeHeight, (h.z + h.offsetZ)) * SharpKmyMath.Matrix4.rotateY(MathHelper.ToRadians(yangle)) * SharpKmyMath.Matrix4.rotateX(MathHelper.ToRadians(xAngle))).translation();
                    _ppos = new SharpKmyMath.Vector3((h.x + h.offsetX), h.y + h.offsetY, (h.z + h.offsetZ));
#if WINDOWS
#else
                    // イベント等のカメラワークが左右反転しているのを修正
                    _campos.x *= -1;
                    // 地形の高さの変位とカメラの高さの変位が±逆になっていたのを修正
                    _ppos.y *= -1;
#endif
                    _lookat = _campos;
                    _target = _campos + new SharpKmyMath.Vector3((float)-Math.Sin(MathHelper.ToRadians(yangle)) * 100, xAngle, (float)-Math.Cos(MathHelper.ToRadians(yangle)) * 100);

#if ENABLE_VR
                    if (SharpKmyVr.Func.IsReady())
                    {
                        _campos += SharpKmyVr.Func.GetHmdPosePos() + m_VrCameraData.GetCombinedOffsetPos();
                        //						_campos.y -= eyeHeight;

                        SharpKmyMath.Vector3 dirTmp = SharpKmyVr.Func.GetHmdPoseDirection();
                        SharpKmyMath.Matrix4 mtxTmp = m_VrCameraData.GetCombinedOffsetRotateMatrix() * SharpKmyMath.Matrix4.translate(dirTmp.x, dirTmp.y, dirTmp.z);

                        _lookat = _campos;
                        _target = _campos + mtxTmp.translation();
                    }
#endif  // #if ENABLE_VR
                    break;

#if ENABLE_GHOST_MOVE
                case Common.Rom.Map.CameraControlMode.GHOST:
                    {
                        camDistance = 0;
                        nearclip = 0.1f;
                        farclip = 100;

                        SharpKmyMath.Vector3 posOffset = m_VrCameraData.GetOutOffsetPos();

                        _campos = (SharpKmyMath.Matrix4.translate(posOffset.x, posOffset.y+eyeHeight, posOffset.z) * SharpKmyMath.Matrix4.rotateY(MathHelper.ToRadians(yangle)) * SharpKmyMath.Matrix4.rotateX(MathHelper.ToRadians(xangle))).translation();
                        _ppos = new SharpKmyMath.Vector3((h.x + h.offsetX), h.y + h.offsetY, (h.z + h.offsetZ));

                        if( SharpKmyVr.Func.IsReady() )
                        {
                            SharpKmyMath.Vector3 posTmp = SharpKmyVr.Func.GetHmdPosePos();
                            _campos	  += (m_VrCameraData.GetCombinedOffsetRotateMatrix() * SharpKmyMath.Matrix4.translate(posTmp.x,posTmp.y,posTmp.z)).translation() + m_VrCameraData.GetCombinedOffsetPos();
//							_campos.y -= eyeHeight;

                            SharpKmyMath.Vector3 dirTmp = SharpKmyVr.Func.GetHmdPoseDirection();
                            SharpKmyMath.Matrix4 mtxTmp = m_VrCameraData.GetCombinedOffsetRotateMatrix() * SharpKmyMath.Matrix4.translate( dirTmp.x, dirTmp.y, dirTmp.z );

                            _lookat = _campos;
                            _target = _campos + mtxTmp.translation();
                        }
                        else
                        {
                            _lookat = _campos;
                            _target = _campos + new SharpKmyMath.Vector3((float)-Math.Sin(MathHelper.ToRadians(yangle)) * 100, xangle, (float)-Math.Cos(MathHelper.ToRadians(yangle)) * 100);
                        }
                    }
                    break;
#endif  // #if ENABLE_GHOST_MOVE
            }

            mapDrawer.setShadowCenter(new SharpKmyMath.Vector3(_ppos.x, _ppos.y, _ppos.z));

            // SkyDrawerにプレイヤーの座標を中心に描画させる
            if (mapDrawer.sky != null)
                mapDrawer.sky.setPlayerOffset(GetLookAtTarget());

            v = SharpKmyMath.Matrix4.lookat(_lookat, _target, vecUp);
            v = SharpKmyMath.Matrix4.inverse(v);

            if (mUsePerspective)
            {
                p = SharpKmyMath.Matrix4.translate(mShakeVal.x, mShakeVal.y, 0) *
                    SharpKmyMath.Matrix4.perspectiveFOV(MathHelper.ToRadians(fovy), asp, nearclip, farclip);
                if (mapDrawer.sky != null)
                {
                    mapDrawer.sky.setProj(SharpKmyMath.Matrix4.translate(mShakeVal.x, mShakeVal.y, 0) *
                        SharpKmyMath.Matrix4.perspectiveFOV(MathHelper.ToRadians(fovy), asp, 1, 1000));
                }
            }
            else
            {
                //p = Matrix.CreateOrthographic(asp * mOrthoHeight * orthozoom, mOrthoHeight * orthozoom, -300, 500);
                float _w = asp * mOrthoHeight * orthozoom;
                float _h = mOrthoHeight * orthozoom;
                p = SharpKmyMath.Matrix4.ortho(-_w * 0.5f, _w * 0.5f, _h * 0.5f, -_h * 0.5f, -300, 500);
            }
            /////////////

#if ENABLE_VR
            m_VrCameraData.m_CameraPos = _campos;
            m_VrCameraData.m_UpVec = vecUp;
#endif  // #if ENABLE_VR
        }

        internal void SetLookAtTarget(bool flag, SharpKmyMath.Vector3 pos)
        {
            useLookAtTargetPos = flag;
            lookAtTarget = pos;
        }

        internal SharpKmyMath.Vector3 GetLookAtTarget()
        {
            if (useLookAtTargetPos)
                return lookAtTarget;
            return GetHeroPos();
        }

        internal SharpKmyMath.Vector3 GetHeroPos()
        {
            var h = GetHero();
            return new SharpKmyMath.Vector3((h.x + h.offsetX), h.y + h.offsetY + 1.0f, (h.z + h.offsetZ));
        }

        internal void DrawGameOver()
        {
            if (gameOverImgId >= 0)
            {
                int width = Graphics.GetImageWidth(gameOverImgId);
                int height = Graphics.GetImageHeight(gameOverImgId);
                Graphics.DrawImage(gameOverImgId, (Graphics.ScreenWidth - width) / 2,
                    (Graphics.ScreenHeight - height) / 2, gameoverColor);
                //Graphics.DrawFillRect(0, 0, Graphics.ViewportWidth, Graphics.ViewportHeight,
                //    screenColor.R, screenColor.G, screenColor.B, screenColor.A);
            }
        }

        public enum EffectPosType
        {
            Head,
            Body,
            Ground,
        }

        public virtual void GetCharacterScreenPos(MapCharacter chr, out int x, out int y, EffectPosType pos = EffectPosType.Ground)
        {
            GetCharacterScreenPos(chr, out x, out y, pp, vv, pos);
        }

        public static void GetCharacterScreenPos(MapCharacter chr, out int x, out int y,
            SharpKmyMath.Matrix4 pp, SharpKmyMath.Matrix4 vv, EffectPosType pos)
        {
            SharpKmyMath.Vector3 v;
            switch (pos)
            {
                case EffectPosType.Head:
                    v = new SharpKmyMath.Vector3(chr.x + chr.offsetX, chr.y + chr.offsetY + 2, chr.z + chr.offsetZ);
                    break;
                case EffectPosType.Ground:
                    v = new SharpKmyMath.Vector3(chr.x + chr.offsetX, chr.y + chr.offsetY, chr.z + chr.offsetZ);
                    break;
                case EffectPosType.Body:
                    v = new SharpKmyMath.Vector3(chr.x + chr.offsetX, chr.y + chr.offsetY + 1, chr.z + chr.offsetZ);
                    break;
                default:
                    v = new SharpKmyMath.Vector3();
                    break;
            }
#if WINDOWS
#else
            v.y *= -1;
#endif
            SharpKmyMath.Vector4 v4 = (pp * vv * v);
            v4 /= v4.w;
#if WINDOWS
            x = (int)((v4.x * 0.5f + 0.5f) * Graphics.ViewportWidth);
            y = (int)((v4.y * -0.5f + 0.5f) * Graphics.ViewportHeight);
#else
            float defaultAspect = (float)Graphics.ViewportHeight / (float)Graphics.ViewportWidth;
            float screenAspect = (float)UnityEngine.Screen.height / (float)UnityEngine.Screen.width;
            float fixRatio = defaultAspect / screenAspect;
            x = (int)((v4.x * fixRatio * 0.25f + 0.5f) * Graphics.ViewportWidth);
            y = (int)((v4.y * - 0.25f + 0.5f) * Graphics.ViewportHeight);
#endif
            var z = Util.getScreenPos(pp, vv, v4, out x, out y, Graphics.ViewportWidth, Graphics.ViewportHeight);

            // カメラより後ろだったら画面上に出ないようにする
#if WINDOWS
            if (z >= 1.00)
#else
            if (z < 1.00)
#endif
            {
                x = 10000;
                y = 10000;
            }

            if (pos == EffectPosType.Ground)
                y += EFFECT_2D_GROUND_OFFSET;  // 2Dエフェクトには2Dモンスターの影の分を意識したオフセットが入ってるので、そのぶんずらす。
        }

        public bool GetSpritePos(int spriteid, out int x, out int y)
        {
            return spManager.GetPosition(spriteid, out x, out y);
        }

        internal int GetRandom(int max, int min = 0)
        {
            return rnd.Next(min, max);
        }

        internal float GetRandom(float max, float min)
        {
            if(min > max)
            {
                var tmp = min;
                min = max;
                max = tmp;
            }

            return (float)rnd.NextDouble() * (max - min) + min;
        }

        public MapCharacter GetHero()
        {
            return hero;
        }

        public virtual MapCharacter GetHeroForBattle(Common.Rom.Hero rom = null)
        {
            return hero;
        }

        internal virtual ScriptRunner GetScriptRunner(Guid guid)
        {
            return runnerDic.getList()
                .Where(x => x.mapChr != null && x.mapChr.rom != null && x.mapChr.rom.guId == guid)
                .FirstOrDefault();
        }

        internal void PauseAllScript(MapCharacter mapChr, ScriptRunner self, bool callByAuto = false)
        {
            foreach (var runner in runnerDic.getList())
            {
                if (runner.mapChr == mapChr && runner != self)
                {
                    runner.Pause(callByAuto);
                }
            }
        }

        internal void ResumeAllScript(MapCharacter mapChr, bool callByAuto = false)
        {
            foreach (var runner in runnerDic.getList())
            {
                if (runner.mapChr == mapChr)
                {
                    runner.Resume(callByAuto);
                }
            }
        }

        internal void ChangeGraphic(MapCharacter mapChr, Guid guid)
        {
            // 同じグラフィックだったら変更しない
            if (mapChr.character != null && mapChr.character.guId == guid)
                return;

            var rom = owner.catalog.getItemFromGuid(guid) as Common.Resource.ResourceItem;
            mapChr.ChangeGraphic(rom, mapDrawer);
        }

        internal void LockControl(int id = -1, string name = "")
        {
            playerLocked++;
            Console.WriteLine("lock   : " + playerLocked + " / " + id + " " + name);
            //string trace = Environment.StackTrace;
            //Console.WriteLine(trace);
        }

        internal void UnlockControl(int id = -1, string name = "")
        {
            playerLocked--;
            Console.WriteLine("unlock : " + playerLocked + " / " + id + " " + name);
            //string trace = Environment.StackTrace;
            //Console.WriteLine(trace);
        }

        internal void PushEffectDrawEntry(EffectDrawer efDrawer, int x, int y)
        {
            var entry = new EffectDrawEntry();
            entry.efDrawer = efDrawer;
            entry.x = x;
            entry.y = y;
            effectDrawEntries.Enqueue(entry);
        }

        internal Color GetNowScreenColor()
        {
            return screenColor;
        }

        internal void SetScreenColor(Color color)
        {
            screenColor = color;
        }

        internal int PushMessage(string str, int winAlign, MessageWindow.WindowType winType)
        {
            return messageWindow.PushMessage(str, winAlign, winType);
        }

        internal bool IsQueuedMessage(int id)
        {
            return messageWindow.IsQueuedMessage(id);
        }

        internal void ShowChoices(string[] strs)
        {
            choicesWindow.Show(strs);
        }

        internal void CloseChoices()
        {
            choicesWindow.Close();
        }

        internal int GetChoicesResult()
        {
            return choicesWindow.GetResult();
        }

        internal void CloseMessageWindow()
        {
            messageWindow.Close();
        }

        internal void ShowShop(Common.Rom.Item[] items, int[] prices)
        {
            shopWindow.Show(items, prices);
        }

        internal void ShowMoneyWindow(bool flag)
        {
            if (flag)
            {
                moneyWindow.show();
            }
            else
            {
                moneyWindow.hide();
            }
        }

        internal void ShowInputWindow(bool show, int height)
        {
            const int OFFSET_HEIGHT = 100;
            const int DEFAULT_HEIGHT = 70;
            if (show)
            {
                inputStringWindow.Move(480, OFFSET_HEIGHT * height + DEFAULT_HEIGHT);
                inputStringWindow.Show();
            }
            else
            {
                inputStringWindow.Hide();
            }
        }
        internal void SetInputData(string[] loadStrings, int stringLength, string defaultInputs)
        {
            inputStringWindow.LoadStrings(loadStrings);
            inputStringWindow.SetStringLength(stringLength);
            inputStringWindow.SetInputString(defaultInputs);
        }

        internal bool haveFinshedInputing()
        {
            return inputStringWindow.haveFinisedInputing();
        }
        internal string GetInputString()
        {
            return inputStringWindow.GetInputString();
        }

        internal void ExclusionAllEvents(ScriptRunner scriptRunner, bool inverse = false)
        {
            exclusiveRunner = scriptRunner;
            exclusiveInverse = inverse;
        }

        internal bool IsShopVisible()
        {
            return shopWindow.IsVisible() || choicesWindow.IsActive();
        }

        internal bool GetShopResult()
        {
            return shopWindow.GetTraded();
        }

        internal void ShowToast(string p)
        {
            toastWindow.show(p);
        }

        internal void ExclusionAllEvents()
        {
            exclusiveRunner = dummyRunner;
        }

        internal void ReservationChangeMap(Guid guid, int x, int y)
        {
            reservedMap = guid;
            reservedX = x;
            reservedY = y;

            IsMapChangedFrame = true;
        }

        internal bool isMapChangeReserved
        {
            get
            {
                return reservedMap != Guid.Empty;
            }
        }

        internal void StartBattleMode(ScriptRunner sender = null)
        {
            LockControl();
            if (sender != null)
                ExclusionAllEvents(sender);
            else
                ExclusionAllEvents();
            isBattle = true;
            mapEngine.setCursorVisibility(true);
        }

        internal void ShowBattleStartMessage()
        {
            var gs = owner.catalog.getGameSettings();
            ShowToast(gs.glossary.battle_start);
        }

        internal void ShowBattleEscapeMessage()
        {
            //var gs = owner.catalog.getGameSettings();
            //ShowToast(gs.glossary.battle_escape);
        }

        internal void EndBattleMode(int loseSwitch = -1)
        {
            isBattle = false;
            //var gs = owner.catalog.getGameSettings();
            // 全員のステータスを正規化する(戦闘不能の反映など)
            foreach (var hero in owner.data.party.members)
            {
                hero.consistency();
            }
            // 戦闘終了判定
            switch (battleSequenceManager.BattleResult)
            {
                case BattleSequenceManager.BattleResultState.Lose_Advanced_GameOver:
                    var gameOverSettings = owner.data.start.gameOverSettings;
                    switch (gameOverSettings.gameOverType)
                    {
                        case GameOverSettings.GameOverType.DEFAULT:
                            break;
                        case GameOverSettings.GameOverType.RIVIVAL:
                            procGameOverRivival(gameOverSettings);
                            break;
                        case GameOverSettings.GameOverType.ADVANCED_RIVIVAL:
                            procGameOverAdvancedRivival(gameOverSettings);
                            break;
                        default:
                            break;
                    }
                    break;
                case BattleSequenceManager.BattleResultState.Escape:
                    UnlockControl();
                    ExclusionAllEvents(null);
                    break;
                case BattleSequenceManager.BattleResultState.Escape_ToTitle:
                    reservedScene = GameMain.Scenes.TITLE;
                    break;
                case BattleSequenceManager.BattleResultState.Lose_Continue:
                    // 全員のHPを1で復活させる
                    foreach (var hero in owner.data.party.members)
                    {
                        hero.hitpoint = 1;
                        hero.statusAilments &= ~Hero.StatusAilments.DOWN;
                    }
                    UnlockControl();
                    ExclusionAllEvents(null);

                    break;
                case BattleSequenceManager.BattleResultState.Lose_GameOver:
                    foreach (var runner in runnerDic.getList())
                    {
                        runner.Pause();
                    }
                    //ShowToast(gs.glossary.battle_lose);
                    DoGameOver();
                    gameoverColor.R = gameoverColor.G = gameoverColor.B = gameoverColor.A = 0;
                    break;
                case BattleSequenceManager.BattleResultState.Win:
                    //ShowToast(gs.glossary.battle_win);
                    UnlockControl();
                    ExclusionAllEvents(null);
                    BattleWin();
                    break;
            }

            battleSequenceManager.ReleaseImageData();
        }

        internal virtual void DoGameOver()
        {
            if (isBattle)
                return;

            LockControl();

            if (hero != null)
                hero.playMotion("keepdown", 0.4f);

            var gs = owner.catalog.getGameSettings();
            var sound = owner.catalog.getItemFromGuid(gs.gameoverBgm) as Common.Resource.Bgm;

            if (sound != null)
            {
                owner.clearTask("Fanfare");
                Audio.PlayBgm(sound);
            }

            isGameOver = true;
            gameOverImgId = Graphics.LoadImage("./res/system/gameover.png");
        }

        private void BattleWin()
        {
            // 経験値とお金とアイテムを加える
            int totalMoney = battleSequenceManager.DropMoney;
            int totalExp = battleSequenceManager.GetExp;
            var gotItems = battleSequenceManager.DropItems;

            // お金を加える
            if (totalMoney > 0)
            {
                owner.data.party.AddMoney(totalMoney);
            }

            // アイテムを加える
            // ResultViewerでやるよう修正しました。
            /*if (gotItems != null)
            {
                foreach (var gotItem in gotItems)
                {
                    owner.data.party.AddItem(gotItem.guId, 1);
                }
            }*/

            // 生存しているキャラに経験値を与える
            if (totalExp > 0)
            {
                foreach (var hero in owner.data.party.members.Where(hero => !hero.statusAilments.HasFlag(Hero.StatusAilments.DOWN)))
                {
                    hero.AddExp(totalExp, owner.catalog);
                }
            }
        }

        private void ProcGameOver()
        {
            int c = gameoverColor.A;
            if (c < 255)
            {
                c += 2;
                if (c >= 255)
                    c = 255;

                byte cc = (byte)c;
                gameoverColor.A = cc;
                gameoverColor.R = cc;
                gameoverColor.G = cc;
                gameoverColor.B = cc;
            }
            else
            {
                if (Input.KeyTest(Input.StateType.TRIGGER, KeyStates.DECIDE))
                {
                    owner.ChangeScene(GameMain.Scenes.TITLE);
                }
            }
        }

        private void procGameOverRivival(GameOverSettings gameOverSettings)
        {
            changePartyHpAndMp(gameOverSettings);
            UnlockControl();
            ExclusionAllEvents(null);
        }

        private void procGameOverAdvancedRivival(GameOverSettings gameoOverSettings)
        {
            changePartyHpAndMp(gameoOverSettings);
            EndFadeOut = MoveMap;
            EndFadeOut += StartFadeIn;
            EndFadeIn = ExecGameoverEvent;
            EndExecEvent += () => {
                EndExecEvent = null;
                UnlockControl();
                ExclusionAllEvents(null);
            };
            StartFadeOut();
        }

        private void changePartyHpAndMp(GameOverSettings gameOverSettings)
        {
            var party = owner.data.party;
            var changeList = new List<Hero>();
            changeList.Add(party.members[0]);
            if (gameOverSettings.rivivalType == GameOverSettings.RivivalType.ALL)
            {
                for (var i = 1; i < party.members.Count; ++i)
                {
                    changeList.Add(party.members[i]);
                }

            }
            foreach (var hero in changeList)
            {
                // 蘇生
                hero.statusAilments &= ~Hero.StatusAilments.DOWN;
                // hp変更
                var rivivalHp = gameOverSettings.rivivalHp;
                if (rivivalHp == -1)
                {
                    hero.hitpoint = 1;
                }
                else
                {
                    var setHp = (hero.maxHitpoint * rivivalHp) / 100;
                    setHp = setHp > hero.maxHitpoint ? hero.maxHitpoint : setHp;
                    setHp = setHp < 1 ? 1 : setHp;
                    hero.hitpoint = setHp;
                }
                // mp変更
                var rivivalMp = gameOverSettings.rivivalMp;
                if (rivivalMp != -1)
                {
                    var setMp = (hero.maxMagicpoint * rivivalMp) / 100;
                    setMp = setMp > hero.maxMagicpoint ? hero.maxMagicpoint : setMp;
                    setMp = setMp < 0 ? 0 : setMp;
                    hero.magicpoint = setMp;
                }

            }
        }

        /// <summary>
        /// マップの移動
        /// </summary>
        private void MoveMap()
        {
            var gameOverSettings = owner.data.start.gameOverSettings;
            var mapRom = owner.catalog.getItemFromGuid(gameOverSettings.mapGuid);
            // マップの指定がなかった場合は、現在のマップを選択する
            if (mapRom == null)
            {
                mapRom = map;
            }
            hero.setDirection(Util.DIR_SER_UP);
            // マップが異なる場合移動する
            if (mapRom != null && mapRom.guId != map.guId)
            {
                ReservationChangeMap(mapRom.guId, gameOverSettings.x, gameOverSettings.y);
                if (CurrentCameraMode == Common.Rom.Map.CameraControlMode.VIEW)
                {
                    resetViewCamera();
                }
            }
            else // マップが同じだったら、位置だけのワープ
            {
                ExclusionAllEvents(currentScriptRunner, true);
                hero.setPosition(gameOverSettings.x, gameOverSettings.y);
                if (CurrentCameraMode == Common.Rom.Map.CameraControlMode.VIEW)
                {
                    resetViewCamera();
                }
            }
        }

        /// <summary>
        /// イベントの実行
        /// </summary>
        private void ExecGameoverEvent()
        {
            var gameOverSettings = owner.data.start.gameOverSettings;
            var eventGuid = gameOverSettings.eventGuid;

            // イベントがない場合は終了
            if (eventGuid == Guid.Empty)
            {
                if (EndExecEvent != null) EndExecEvent();
                return;
            }

            var calledEvent = GetScriptRunner(eventGuid);
            if (calledEvent == null)
            {
                if (EndExecEvent != null) EndExecEvent();
                return;
            }

            var script = new Common.Rom.Script();
            script.name = "ExecEvent";
            script.trigger = Common.Rom.Script.Trigger.AUTO;
            var cmd = new Common.Rom.Script.Command();
            cmd.type = Common.Rom.Script.Command.FuncType.EXEC;
            cmd.attrList.Add(new Common.Rom.Script.GuidAttr(eventGuid));
            cmd.attrList.Add(new Common.Rom.Script.IntAttr(1));
            script.commands.Add(cmd);
            var runner = new ScriptRunner(this, hero, script);
            runnerDic.Add(script.guId, runner);
            currentScriptRunner = runner;
            runner.Run();
        }

        private Common.Rom.Script CreateScript(Guid eventGuid)
        {
            // イベントがない場合は終了
            if (eventGuid == Guid.Empty)
            {
                return null;
            }

            var calledEvent = GetScriptRunner(eventGuid);
            if (calledEvent == null)
            {
                return null;
            }

            var script = new Common.Rom.Script();
            script.name = "ExecEvent";
            script.trigger = Common.Rom.Script.Trigger.AUTO;
            var cmd = new Common.Rom.Script.Command();
            cmd.type = Common.Rom.Script.Command.FuncType.EXEC;
            cmd.attrList.Add(new Common.Rom.Script.GuidAttr(eventGuid));
            cmd.attrList.Add(new Common.Rom.Script.IntAttr(1));
            script.commands.Add(cmd);

            return script;
        }

        internal ScriptRunner ExecCommon(Guid guid)
        {
            var script = CreateScript(guid);
            if (script == null)
            {
                return null;
            }
            var runner = new ScriptRunner(this, hero, script);
            runnerDic.Add(script.guId, runner);
            runnerDic.bringToFront(runner);
            runner.Run();
            runner.Update();

            // 間接的に実行されたコモンを真っ先に処理する
            var commonRunner = GetScriptRunner(guid);
            if (commonRunner != null)
            {
                commonRunner.Update();
            }

            return runner;
        }

        internal void RefreshHeroMapChr()
        {
            if (hero == null)
                return;

            menuWindow.RefreshPartyChr();

            var heroOld = hero;
            var offset = hero.getOffset();

            hero.Reset();
            mapCharList.Remove(hero);
            mapEngine.CreateHeroChr(hero.x - 0.5f, hero.z - 0.5f, mapDrawer, hero.y);

            Console.WriteLine(offset.ToString());

            hero.setDirection(heroOld.getDirection(), true);
            hero.hide = heroOld.hide;
        }

        internal void setEncountFlag(bool p)
        {
            owner.data.system.encountAvailable = p;
        }

        internal void setMenuAvailable(bool p)
        {
            owner.data.system.menuAvailable = p;
        }

        internal void setSaveAvailable(bool p)
        {
            owner.data.system.saveAvailable = p;
        }

        internal Common.Resource.Bgm getMapBattleBgm()
        {
            if (battleSetting.useDefaultBattleBgm)
                return owner.catalog.getItemFromGuid(
                    owner.catalog.getGameSettings().battleBgm) as Common.Resource.Bgm;
            else
                return owner.catalog.getItemFromGuid(battleSetting.battleBgm) as Common.Resource.Bgm;
        }

        internal Common.Resource.Bgs getMapBattleBgs()
        {
            return owner.catalog.getItemFromGuid(battleSetting.battleBgs) as Common.Resource.Bgs;
        }

        // 現在のマップキャラクターの状態を保存
        internal List<Common.GameData.Event> saveEventState()
        {
            var result = new List<Common.GameData.Event>();

            foreach (var chr in mapCharList)
            {
                if (chr.rom == null)
                    continue;

                var ev = new Common.GameData.Event();
                ev.guId = chr.rom.guId;
                ev.x = chr.x;
                ev.y = chr.y;
                ev.z = chr.z;
                ev.dir = chr.getDirection();
                if (chr.getGraphic() != null)
                    ev.graphic = chr.getGraphic().guId;
                else
                    ev.graphic = Guid.Empty;
                ev.motion = chr.currentMotion;

                // 並列動作スクリプトが動いていたら、その情報も保存
                var runner = GetScriptRunner(chr);
                ev.scriptRunning = runner != null && runner.Trigger == Common.Rom.Script.Trigger.PARALLEL;
                if (ev.scriptRunning)
                {
                    runner.save(ev);
                }

                result.Add(ev);
            }

            return result;
        }

        private ScriptRunner GetScriptRunner(MapCharacter chr)
        {
            foreach (var runner in runnerDic.getList())
            {
                if (runner.mapChr == chr && runner.Trigger != Common.Rom.Script.Trigger.PARALLEL_MV)
                    return runner;
            }

            return null;
        }

        // 現在のマップキャラクターの状態を再現
        private void loadEventState(List<Event> eventStates)
        {
            // イベントROMを保持しているマップキャラクターのGUIDを辞書にする
            var dic = mapCharList.Where(x => x.rom != null).ToDictionary(x => x.rom.guId);

            foreach (var stat in eventStates)
            {
                if (!dic.ContainsKey(stat.guId))
                    continue;

                var chr = dic[stat.guId];
                var res = owner.catalog.getItemFromGuid(stat.graphic) as Common.Resource.ResourceItem;
                mapEngine.changeCharacterGraphic(chr, res);
                chr.playMotion(stat.motion, 0);
                chr.setDirection(stat.dir, true);
                chr.x = stat.x;
                chr.y = stat.y;
                chr.z = stat.z;

                // 並列動作スクリプトが動いていたら、その情報も読み込み
                var runner = GetScriptRunner(chr);
                if (stat.scriptRunning && runner != null && runner.Trigger == Common.Rom.Script.Trigger.PARALLEL)
                {
                    runner.load(stat);
                }
            }
        }

        internal CameraSettings saveCameraSettings()
        {
            var camera = new CameraSettings();
            camera.cameraControlMode = cameraControlMode;
            camera.xangle = xangle;
            camera.yangle = yangle;
            camera.fovy = fovy;
            camera.dist = dist;
            camera.eyeHeight = eyeHeight;
            camera.lookAtTarget = lookAtTarget;
            camera.useLookAtTargetPos = useLookAtTargetPos;
            return camera;
        }

        private void loadCameraSettings(CameraSettings camera)
        {
            cameraControlMode = camera.cameraControlMode;
            changeCameraMode(cameraControlMode);
            xangle = camera.xangle;
            yangle = camera.yangle;
            fovy = camera.fovy;
            dist = camera.dist;
            eyeHeight = camera.eyeHeight;
            lookAtTarget = camera.lookAtTarget;
            useLookAtTargetPos = camera.useLookAtTargetPos;
        }

        internal List<Sprite> saveSpriteState()
        {
            return spManager.save();
        }

        internal virtual void ReservationChangeScene(GameMain.Scenes scene)
        {
            reservedScene = scene;
        }

        internal bool isPlayerLockedByEvent()
        {
            return playerLocked >= MapScene.PLAYER_LOCK_BY_EVENT;
        }

        //------------------------------------------------------------------------------
        /**
         *	カメラ制御モードを取得
         */
        public Common.Rom.Map.CameraControlMode GetCameraControlMode()
        {
            if (battleSequenceManager != null && isBattle && !owner.IsBattle2D)
            {
                // ◆バトル中
                return Common.Rom.Map.CameraControlMode.NORMAL;
            }
            else
            {
                // ◆バトル中以外
                return cameraControlMode;
            }
        }

#if ENABLE_VR

        //------------------------------------------------------------------------------
        /**
         *	VRカメラデータをセットアップ
         */
        public void SetupVrCameraData(bool bUpdateInfo)
        {
            if (battleSequenceManager != null && isBattle && !owner.IsBattle2D)
            {
                // ◆バトル中
                battleSequenceManager.SetupVrCameraData(bUpdateInfo);
            }
            else
            {
                // ◆バトル中以外

                // パラメータセットアップ
                m_VrCameraData.SetupParam(VrCameraData.ParamType.RotateY, 0, 8, true);
                m_VrCameraData.SetupParam(VrCameraData.ParamType.Distance, 2, 8, false, 6.0f, 1.5f);
                m_VrCameraData.SetupParam(VrCameraData.ParamType.Height, 2, 8, false, 0.0f, 1.5f);

                // 箱庭視点時のスケール
                m_VrCameraData.SetHakoniwaScale(5.0f);

                // 情報更新
                if (bUpdateInfo)
                {
                    m_VrCameraData.UpdateInfo(Common.Rom.Map.CameraControlMode.UNKNOWN);
                }
            }
        }

        //------------------------------------------------------------------------------
        /**
         *	VRカメラデータを取得
         */
        public VrCameraData GetVrCameraData()
        {
            if (battleSequenceManager != null && isBattle && !owner.IsBattle2D)
            {
                // ◆バトル中
                return battleSequenceManager.GetVrCameraData();
            }
            else
            {
                // ◆バトル中以外
                return m_VrCameraData;
            }
        }

        //------------------------------------------------------------------------------
        /**
         *	カメラ同期
         */
        public void SyncCamera()
        {
            if (battleSequenceManager != null && isBattle && !owner.IsBattle2D)
            {
                // ◆バトル中

                // 何もしない
            }
            else
            {
                // ◆バトル中以外
                switch (cameraControlMode)
                {
                    case Common.Rom.Map.CameraControlMode.NORMAL:
                        {
#if ENABLE_VR
                            MapCharacter hero = GetHero();
                            m_VrCameraData.SetOutOffsetPos(hero.x, hero.y, hero.z);
                            m_VrCameraData.UpdateInfo(cameraControlMode);
#endif  // #if ENABLE_VR
                        }
                        break;

#if ENABLE_GHOST_MOVE
                case Common.Rom.Map.CameraControlMode.GHOST:
                    {
                        MapCharacter hero = GetHero();
                        m_VrCameraData.SetOutOffsetPos( hero.x, hero.y, hero.z );
                        m_VrCameraData.SetOutOffsetRotateY( hero.getDirectionRadian() );
                        m_VrCameraData.UpdateInfo( cameraControlMode );
                    }
                    break;
#endif  // #if ENABLE_GHOST_MOVE
                }
            }
        }

#endif  // #if ENABLE_VR

        internal void setCamControlLock(bool useYRotate, bool useXRotate, bool useZoom)
        {
            owner.data.start.camLockX = !useXRotate;
            owner.data.start.camLockY = !useYRotate;
            owner.data.start.camLockZoom = !useZoom;
        }

        internal void setCamLockState(bool doLock, bool doModeLock)
        {
            mapFixCamera = doLock;
            mapFixCameraMode = doModeLock;
        }

        internal bool isToastVisible()
        {
            return toastWindow.isVisible();
        }

        internal void ShowTrashWindow(Guid guid, int num)
        {
            if (guid == Guid.Empty)
                trashWindow.show(ItemTrashController.TrashMode.JUST_THROW_AWAY, guid, num);
            else
                trashWindow.show(ItemTrashController.TrashMode.ADD_NEW_ITEM, guid, num);
        }

        internal bool IsTrashVisible()
        {
            return trashWindow.isVisible();
        }

        private void StartFadeIn()
        {

            fadeFrame = 0.0f;
            screenColor.R = 0;
            screenColor.G = 0;
            screenColor.B = 0;
            fadeStatus = FadeStatus.FadeIn;
        }

        private void StartFadeOut()
        {
            fadeFrame = 0.0f;
            screenColor.R = 0;
            screenColor.G = 0;
            screenColor.B = 0;
            fadeStatus = FadeStatus.FadeOut;
        }

        /// <summary>
        /// Fadeの実行
        /// <para>deledateは実行後空にする</para>
        /// </summary>
        private void RunFade()
        {
            var delta = 0.0f;
            byte tmp = 0;
            bool isFinish = false;
            switch (fadeStatus)
            {
                case FadeStatus.FadeIn:
                    delta = fadeFrame / FADE_TIME;
                    delta = delta >= 1 ? 1 : delta;
                    if (delta >= 1.0f)
                    {
                        delta = 1.0f;
                        isFinish = true;
                    }
                    tmp = (byte)(255 * (1 - delta));
                    screenColor.A = tmp;
                    if (isFinish)
                    {
                        fadeStatus = FadeStatus.None;
                        if (EndFadeIn != null)
                        {
                            EndFadeIn();
                            EndFadeIn = null;
                        }
                    }
                    break;
                case FadeStatus.FadeOut:
                    delta = fadeFrame / FADE_TIME;
                    if (delta >= 1.0f)
                    {
                        delta = 1.0f;
                        isFinish = true;
                    }
                    tmp = (byte)(255 * delta);
                    screenColor.A = tmp;
                    if (isFinish)
                    {
                        fadeStatus = FadeStatus.None;
                        if (EndFadeOut != null)
                        {
                            EndFadeOut();
                            EndFadeOut = null;
                        }
                    }
                    break;
                case FadeStatus.None:
                    break;
                default:
                    break;
            }
            fadeFrame += GameMain.getRelativeParam60FPS();
        }

        internal virtual void applyCameraToBattle()
        {
        }

        internal virtual void SetEffectColor(MapCharacter selfChr, Color color)
        {
        }

        internal virtual void setHeroName(Guid hero, string nextHeroName)
        {
            owner.data.party.setHeroName(hero, nextHeroName);
        }

        internal void SetEffectTargetColor(int index, Color color)
        {
            spManager.SetEffectTargetColor(index, color);
        }
    }
}

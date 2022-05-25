using System;
using UnityEngine;
using System.Collections.Generic;
using ResourceManager;

namespace SharpKmyIO
{
    public class Controller
    {
        static public InputState inputState = InputState.BASE;

        Yukar.Engine.GameMain mGameMain = null;
        ControllerVirtual mControllerVirtual = new ControllerVirtual();
        static List<KeyCode> keyCodeValues;

        static public bool repeatTouchUp = false;
        static public bool repeatTouchDown = false;
        static public bool repeatTouchLeft = false;
        static public bool repeatTouchRight = false;

        // イベントによる操作禁止検出に使う変数
        static public int elapsedFramesDuringEvent = 0;

#if UNITY_SWITCH && !UNITY_EDITOR
        ControllerSwitch mSwitch = new ControllerSwitch();
#endif

        public enum InputState
        {
            BASE,
            KEYBOARD,
            GAMEPAD,
            TOUCHPANEL,
        }

        internal void setGameMain(Yukar.Engine.GameMain inGameMain)
        {
            this.mGameMain = inGameMain;
            this.mControllerVirtual.GameMain = inGameMain;
        }

        internal void initialize()
        {
            this.mControllerVirtual.initialize();

#if UNITY_SWITCH && !UNITY_EDITOR
            this.mSwitch.initialize();
#endif

            keyCodeValues = new List<KeyCode>();
            foreach (KeyCode code in Enum.GetValues(typeof(KeyCode)))
                keyCodeValues.Add(code);
        }

        internal void update()
        {
#if UNITY_EDITOR
#if UNITY_2017_1_OR_NEWER
#else
            //キャプチャ
            if (Input.GetKeyDown(KeyCode.P))
                Application.CaptureScreenshot("capture.png");
#endif  // UNITY_2017_1_OR_NEWER
#endif  // UNITY_EDITOR

            //バーチャルコントローラの更新
            {
                if (this.mGameMain != null
                && this.mGameMain.mapScene != null)
                {
                    //mapFixCamera がカメラアングルの禁止状態
                    this.mControllerVirtual.mapFixCamera = this.mGameMain.mapScene.mapFixCamera;
                    //mapFixCameraMode がカメラモード切り替え（TP / FP）の禁止状態です
                    this.mControllerVirtual.mapFixCameraMode = this.mGameMain.mapScene.mapFixCameraMode;
                }

                if (this.mGameMain.catalog.getGameSettings().exportSettings.unity.visibleVirtualPad)
                    this.mControllerVirtual.update();
                
                if (this.mControllerVirtual.Touch.isTouchPressed()) inputState = InputState.TOUCHPANEL;
            }

#if UNITY_SWITCH && !UNITY_EDITOR
            if (this.mSwitch.isConnectController())
            {
                inputState = InputState.KEYBOARD;
            }
#else
            // keyboard入力できるときはボタンを消す
            foreach (var code in keyCodeValues)
            {
                if (KeyCode.Mouse0 <= code && code <= KeyCode.Mouse6) continue;
                if (Input.GetKey(code) == false) continue;
                inputState = InputState.KEYBOARD;
                break;
            }
#endif

            //バーチャルコントローラのボタン表示関連
            if (this.mGameMain == null
            || this.mGameMain.mapScene == null
            || this.mGameMain.mapScene.mapEngine.IsCursorVisible() == false
            || Controller.inputState == Controller.InputState.KEYBOARD)
            {
                this.mControllerVirtual.hide();
            }
            else
            {
                var stickMove = this.mControllerVirtual.getStick(ControllerVirtual.Stick.MOVE);
                stickMove.IsFourWay = true;//スティックの斜め設定(特になければ4方向
                stickMove.IsForceTouchDown = true;//隠れていてもタッチできるように

                var menuState = Yukar.Engine.MenuController.state;
                var windowState = this.mGameMain.mapScene.messageWindow.getWndowState();
                //var windowType = this.mGameMain.mapScene.messageWindow.getWndowType();
                var choicesState = this.mGameMain.mapScene.choicesWindow.getWindowState();
                var isBattle = this.mGameMain.mapScene.isBattle;
                var isPlayerLocked = 0 < this.mGameMain.mapScene.playerLocked;

                // イベント発生中、プレイヤーが操作禁止となってからの経過フレーム数を数える
                // イベント終了後、経過フレーム数をリセット
                if (isPlayerLocked) elapsedFramesDuringEvent++;
                else elapsedFramesDuringEvent = 0;

                //var isMapChangeReserved = this.mGameMain.mapScene.isMapChangeReserved;

                if (this.mGameMain.nowScene == Yukar.Engine.GameMain.Scenes.LOGO)
                {
                    this.mControllerVirtual.show(ControllerVirtual.ShowGadget.Logo);
                }
                else if (this.mGameMain.nowScene == Yukar.Engine.GameMain.Scenes.TITLE)
                {
                    this.mControllerVirtual.show(ControllerVirtual.ShowGadget.Title);
                }
                //else if (this.mGameMain.nowScene == Yukar.Engine.GameMain.Scenes.MOVIE)
                //{
                //    this.mControllerVirtual.show(ControllerVirtual.ShowGadget.Movie); TODO
                //}
                else if (menuState != Yukar.Engine.MenuController.State.HIDE)
                {
                    this.mControllerVirtual.show(ControllerVirtual.ShowGadget.Menu);
                    stickMove.IsForceTouchDown = false;
                }
                else if (choicesState != Yukar.Engine.ChoicesWindow.WindowState.HIDE_WINDOW)
                {
                    this.mControllerVirtual.show(ControllerVirtual.ShowGadget.MessageChoices);
                }
                else if (windowState != Yukar.Engine.MessageWindow.WindowState.HIDE_WINDOW)
                {
                    this.mControllerVirtual.show(ControllerVirtual.ShowGadget.Message);
                }
                else if (isBattle)
                {
                    this.mControllerVirtual.show(ControllerVirtual.ShowGadget.Battle);
                }
                else if (isPlayerLocked)
                {
                    //playerLocked が移動の禁止状態
                    //ただイベントバトル中も playerLocked になっちゃうので、バトル中は別途考慮が必要です。
                    this.mControllerVirtual.show(ControllerVirtual.ShowGadget.Event);
                }
                else
                {
                    this.mControllerVirtual.show();
                    //斜め移動の設定(マップのみ設定次第で８方向
                    stickMove.IsFourWay = (this.mGameMain.catalog.getGameSettings().gridMode == Yukar.Common.Rom.GameSettings.GridMode.SNAP);
                }
            }
        }

        internal bool isTouchPressed(int id = 0)
        {
            return this.mControllerVirtual.Touch.isTouchPressed();
        }

        internal bool isTouchDown(int id = 0)
        {
            return this.mControllerVirtual.Touch.isTouchDown();
        }

        internal void draw()
        {
#if !ENABLE_VR_UNITY
            if (!this.mGameMain.catalog.getGameSettings().exportSettings.unity.visibleVirtualPad)
                return;

            this.mControllerVirtual.draw();
#endif
        }

        internal bool isUp(string keyType)
        {
            switch (keyType)
            {
                // キーボード
                case "F1":
                    return Input.GetKeyUp(KeyCode.F1);
                case "F2":
                    return Input.GetKeyUp(KeyCode.F2);
            }
            return false;
        }

        internal void addInput(string v1, int v2, object p, int v3)
        {
            // Dummy
        }

        internal bool isHold(string v)
        {
            return false;
        }

        internal void Release()
        {
            // Dummy
        }

        internal float getAxis(string keyName)
        {
            float axis = 0.0f;
            var virtualPadAxis = mControllerVirtual.getAxis();
            switch (keyName)
            {
                // ゲームパッド
                case "PAD_X":
                    axis = Input.GetAxis("Horizontal");
                    if (Math.Abs(axis) > Math.Abs(virtualPadAxis.x))
                    {
                        return axis;
                    }
                    return virtualPadAxis.x;
                case "PAD_Y":
                    axis = Input.GetAxis("Vertical");
                    if (Math.Abs(axis) > Math.Abs(virtualPadAxis.y))
                    {
                        return axis;
                    }
                    return virtualPadAxis.y;
                default:
                    return -2.0f;
            }
        }

        internal int getValue(string keyType)
        {
            //バーチャルコントローラ
            {
                int res = 0;
                if (this.mControllerVirtual.getValue(keyType, ref res)) return res;
            }

            //スイッチコントローラ
#if UNITY_SWITCH && !UNITY_EDITOR
            {
                bool isReturnValue = false;
                int res = this.mSwitch.getValue(keyType, ref isReturnValue);
                if(isReturnValue)return res;
            }
#endif

            const float axisThreshold = 0.5f;
            switch (keyType)
            {
                // キーボード
                case "UP_0":
                    return Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W) || repeatTouchUp ? 1 : 0;
                case "DOWN_0":
                    return Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S) || repeatTouchDown ? 1 : 0;
                case "LEFT_0":
                    return Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) || repeatTouchLeft ? 1 : 0;
                case "RIGHT_0":
                    return Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) || repeatTouchRight ? 1 : 0;
                case "DECIDE_0":
                    return Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.Space) ? 1 : 0;
                case "CANCEL_0":
                    return Input.GetKey(KeyCode.X) || Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.RightControl) ? 1 : 0;
                case "MENU_0":
                    return Input.GetKey(KeyCode.C) ? 1 : 0;
                case "DASH_0":
                    return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 1 : 0;

                case "TOUCH":
                    if (this.mGameMain == null) return 0;
                    if (this.mGameMain.mapScene == null) return 0;
                    if (this.mGameMain.mapScene.mapEngine == null) return 0;
                    if (this.mGameMain.mapScene.mapEngine.IsCursorVisible()) return 0;
                    return Input.GetMouseButton(0) ? 1 : 0;

                case "CAMERA_VERTICAL_ROT_UP":
                    return Input.GetKey(KeyCode.R) ? 1 : 0;
                case "CAMERA_VERTICAL_ROT_DOWN":
                    return Input.GetKey(KeyCode.F) ? 1 : 0;
                case "CAMERA_HORIZONTAL_ROT_CLOCKWISE":
                    return Input.GetKey(KeyCode.Q) ? 1 : 0;
                case "CAMERA_HORIZONTAL_ROT_COUNTER_CLOCKWISE":
                    return Input.GetKey(KeyCode.E) ? 1 : 0;
                case "CAMERA_ZOOM_IN":
                    return Input.GetKey(KeyCode.C) ? 1 : 0;
                case "CAMERA_ZOOM_OUT":
                    return Input.GetKey(KeyCode.V) ? 1 : 0;
                case "CAMERA_POSITION_RESET":
                    return Input.GetKey(KeyCode.K) ? 1 : 0;
                case "CAMERA_CONTROL_MODE_CHANGE":
                    return Input.GetKey(KeyCode.B) ? 1 : 0;

                // ゲームパッド
                case "PAD_X":
                    if (axisThreshold < Input.GetAxis("Horizontal")) return 1;
                    if (Input.GetAxis("Horizontal") < -axisThreshold) return -1;
                    return 0;
                case "PAD_Y":
                    if (axisThreshold < Input.GetAxis("Vertical")) return 1;
                    if (Input.GetAxis("Vertical") < -axisThreshold) return -1;
                    return 0;
                case "PAD_DECIDE":
                    return Input.GetButton("B") ? 1 : 0;
                case "PAD_CANCEL":
                    return Input.GetButton("A") ? 1 : 0;
                case "PAD_CAMERA_CONTROL_MODE_CHANGE":
                    return Input.GetButton("Y") ? 1 : 0;
                case "PAD_DASH":
                    return Input.GetButton("X") ? 1 : 0;

                case "PAD_CAMERA_POSITION_RESET":
                    return Input.GetButton("CameraPositonReset") ? 1 : 0;
                case "PAD_CAMERA_ZOOM_IN":
                    return Input.GetButton("CameraZoomIn") ? 1 : 0;
                case "PAD_CAMERA_ZOOM_OUT":
                    return Input.GetButton("CameraZoomOut") ? 1 : 0;
                case "PAD_CAMERA_HORIZONTAL_ROT_CLOCKWISE":
                case "PAD_CAMERA_HORIZONTAL_ROT_COUNTER_CLOCKWISE":
                    if (axisThreshold < Input.GetAxis("Horizontal2")) return 1;
                    if (Input.GetAxis("Horizontal2") < -axisThreshold) return -1;
                    return 0;
                case "PAD_CAMERA_VERTICAL_ROT_UP":
                case "PAD_CAMERA_VERTICAL_ROT_DOWN":
                    if (axisThreshold < Input.GetAxis("Vertical2")) return -1;
                    if (Input.GetAxis("Vertical2") < -axisThreshold) return 1;
                    return 0;

                default:
                    break;
            }

            return 0;
        }

        internal SharpKmyMath.Vector2 getTouchPos(int inIndex = 0)
        {
            var point = this.mControllerVirtual.Touch.getPointFromIndex(inIndex);
            if (point == null) return new SharpKmyMath.Vector2(0, 0);
            return point.Pos;
        }

    }

    public enum INPUTID
    {
        kKEYBOARD_BEGIN,
    }


    ///////////////////////////////////////////////////
    //ControllerVirtual
    ///////////////////////////////////////////////////
    public class ControllerVirtual
    {
        List<ButtonGadget> mButtonList = new List<ButtonGadget>();
        List<StickGadget> mStickList = new List<StickGadget>();
        List<SliderGadget> mSliderList = new List<SliderGadget>();

        public SharpKmyIO.Touch mTouch = new SharpKmyIO.Touch();
        public SharpKmyIO.Touch Touch { get { return this.mTouch; } }

        public bool mapFixCamera = true;//mapFixCamera がカメラアングルの禁止状態
        public bool mapFixCameraMode = true;//mapFixCameraMode がカメラモード切り替え（TP / FP）の禁止状態です

        public bool mEnable = true;
        public bool Enable { get { return this.mEnable; } set { this.mEnable = value; } }
        const float ShowTime = 0.4f;
        private Yukar.Engine.GameMain mGameMain;
        public Yukar.Engine.GameMain GameMain { get { return this.mGameMain; } set { this.mGameMain = value; } }
        public enum Button
        {
            CHECK = 0,
            DASH,
            MENU,
            CAMERA,

            FPS,
            RESET,
            ZOOM_PLUS,
            ZOOM_MINUS,

            MAX
        }

        public enum Stick
        {
            MOVE = 0,
            MAX
        }

        public enum Slider
        {
            CAMERA_VERTICAL = 0,
            CAMERA_HORIZONTAL,
            MAX
        }

        public enum Mode
        {
            Normal = 0,
            CameraShow,
            Photo,

            MAX
        }
        Mode mMode = Mode.Normal;

        public enum ShowGadget
        {
            All = 0,
            Logo,//ロゴ(KMY/SGB)
            Title,//タイトル画面
            Message,//NPCとの会話
            MessageChoices,//NPCとの会話
            Event,//イベント発生中
            Movie,//動画再生
            Menu,//メニュー表示中
            Walk,//歩行
            Battle,//戦闘及びリザルト
        }
        bool mIsShow = true;
        ShowGadget mShowGadget = ShowGadget.Walk;
        float mShowTime = 0;


        public void initialize()
        {
            var screenSize = new SharpKmyMath.Vector2(Yukar.Engine.Graphics.ScreenWidth, Yukar.Engine.Graphics.ScreenHeight);

            #region ガジェットのインスタンス作成
            for (int i1 = 0; i1 < (int)Button.MAX; ++i1)
            {
                this.mButtonList.Add(new ButtonGadget(this));
            }
            for (int i1 = 0; i1 < (int)Stick.MAX; ++i1)
            {
                this.mStickList.Add(new StickGadget(this));
            }
            for (int i1 = 0; i1 < (int)Slider.MAX; ++i1)
            {
                this.mSliderList.Add(new SliderGadget(this));
            }
            #endregion

            #region ガジェットの初期化
            {
                ButtonGadget btn = null;
                StickGadget stick = null;
                SliderGadget slider = null;
                var pos = new SharpKmyMath.Vector3(0, 0, -10);
                float posMarginTop = 0f, posMarginBottom = 0f, posMarginLeft = 0f, posMarginRight = 0f;
                var partsSize = new SharpKmyMath.Vector2(0, 0);
                float gridDiv = 8;
                SharpKmyMath.Vector2 gridSize = new SharpKmyMath.Vector2(screenSize.x / gridDiv, screenSize.y / gridDiv);
                var hidePos = new SharpKmyMath.Vector3(screenSize.x / 4, screenSize.y / 4, 0);

                //CHECK
                btn = this.getButton(Button.CHECK);
                btn.Parts = Image.Parts.CHECK;
                partsSize = Image.getSize(btn.Parts);
                pos.x = (screenSize.x - partsSize.x) - 20;
                pos.y = (screenSize.y - partsSize.y);
                btn.setShowAnim(new ButtonGadget.AnimData(pos, null));
                btn.setDisableAnim(new ButtonGadget.AnimData(pos, new SharpKmyMath.Vector4(1, 1, 1, 0.1f)));
                btn.setHideAnim(new ButtonGadget.AnimData(pos, new SharpKmyMath.Vector4(1, 1, 1, 0.0f)));
                btn.setHitRange(new SharpKmyMath.Vector4(gridSize.x * 6.25f, gridSize.y * 6.25f, gridSize.x * 8, gridSize.y * 8));

                posMarginBottom = btn.GetAnimShow().Pos.y;
                posMarginBottom += Image.getSize(btn.Parts).y;
                posMarginBottom = screenSize.y - (posMarginBottom - 20);

                //MENU
                btn = this.getButton(Button.MENU);
                btn.Parts = Image.Parts.MENU;
                partsSize = Image.getSize(btn.Parts) / 2;
                pos.x = (screenSize.x - partsSize.x);
                pos.y = (0 - partsSize.y);
                btn.setShowAnim(new ButtonGadget.AnimData(pos, null));
                btn.setDisableAnim(new ButtonGadget.AnimData(pos, null));
                btn.setHideAnim(new ButtonGadget.AnimData(pos + new SharpKmyMath.Vector3(hidePos.x, -hidePos.y, 0), null));

                //CAMERA
                btn = this.getButton(Button.CAMERA);
                btn.Parts = Image.Parts.CAMERA;
                partsSize = Image.getSize(btn.Parts) / 2;
                pos.x = (0 - partsSize.x);
                pos.y = (0 - partsSize.y);
                btn.setShowAnim(new ButtonGadget.AnimData(pos, null));
                btn.setDisableAnim(new ButtonGadget.AnimData(pos, null));
                btn.setHideAnim(new ButtonGadget.AnimData(pos + new SharpKmyMath.Vector3(-hidePos.x, -hidePos.y, 0), null));

                posMarginTop = btn.GetAnimShow().Pos.y;
                posMarginTop += Image.getSize(btn.Parts).y - 7;

                //DASH
                btn = this.getButton(Button.DASH);
                btn.Parts = Image.Parts.DASH;
                partsSize = Image.getSize(btn.Parts);
                pos.x = (screenSize.x - partsSize.x) + 30;
                pos.y = (screenSize.y - partsSize.y) - 130;
                btn.setShowAnim(new ButtonGadget.AnimData(pos, null));
                btn.setDisableAnim(new ButtonGadget.AnimData(pos, new SharpKmyMath.Vector4(1, 1, 1, 0)));
                btn.setHideAnim(new ButtonGadget.AnimData(pos, new SharpKmyMath.Vector4(1, 1, 1, 0)));
                btn.setHitRange(new SharpKmyMath.Vector4(gridSize.x * 7, gridSize.y * 4, gridSize.x * 8, gridSize.y * 6));

                posMarginRight = btn.GetAnimShow().Pos.x + (partsSize.x / 2);
                posMarginRight = screenSize.x - posMarginRight;
                posMarginLeft = posMarginRight;

                // stick
                stick = this.getStick(Stick.MOVE);
                stick.Parts = Image.Parts.STICK;
                stick.PartsCenter = Image.Parts.STICK_CENTER;
                partsSize = Image.getSize(btn.Parts) / 2;
                pos.x = (gridSize.x * 1) - partsSize.x;
                pos.y = gridSize.y * (gridDiv - 2) - partsSize.y;
                stick.setShowAnim(new ButtonGadget.AnimData(pos, null));
                stick.setDisableAnim(new ButtonGadget.AnimData(pos, new SharpKmyMath.Vector4(1, 1, 1, 0.1f)));
                stick.setHideAnim(new ButtonGadget.AnimData(pos, new SharpKmyMath.Vector4(1, 1, 1, 0.0f)));


                //FPS
                btn = this.getButton(Button.FPS);
                btn.Parts = Image.Parts.FPS;
                partsSize = Image.getSize(btn.Parts);
                pos.x = gridSize.x - 10.0f;
                pos.y = posMarginTop - partsSize.y;
                btn.setShowAnim(new ButtonGadget.AnimData(pos, null));
                btn.setDisableAnim(new ButtonGadget.AnimData(pos, null));
                btn.setHideAnim(new ButtonGadget.AnimData(pos + new SharpKmyMath.Vector3(0, -hidePos.y, 0), null));

                //RESET
                btn = this.getButton(Button.RESET);
                btn.Parts = Image.Parts.RESET;
                partsSize = Image.getSize(btn.Parts);
                pos.x = screenSize.x - gridSize.x - partsSize.x + 10.0f;
                pos.y = posMarginTop - partsSize.y;
                btn.setShowAnim(new ButtonGadget.AnimData(pos, null));
                btn.setDisableAnim(new ButtonGadget.AnimData(pos, null));
                btn.setHideAnim(new ButtonGadget.AnimData(pos + new SharpKmyMath.Vector3(0, -hidePos.y, 0), null));

                //ZOOM_PLUS
                btn = this.getButton(Button.ZOOM_PLUS);
                btn.Parts = Image.Parts.PLUS;
                partsSize = Image.getSize(btn.Parts);
                pos.x = screenSize.x - posMarginRight;
                pos.x -= partsSize.x / 2;
                pos.y = gridSize.y * 3;
                btn.setShowAnim(new ButtonGadget.AnimData(pos, null));
                btn.setDisableAnim(new ButtonGadget.AnimData(pos, null));
                btn.setHideAnim(new ButtonGadget.AnimData(pos + new SharpKmyMath.Vector3(hidePos.x, 0, 0), null));

                //ZOOM_MINUS
                btn = this.getButton(Button.ZOOM_MINUS);
                btn.Parts = Image.Parts.MINUS;
                partsSize = Image.getSize(btn.Parts);
                pos.x = screenSize.x - posMarginRight;
                pos.x -= partsSize.x / 2;
                pos.y = gridSize.y * 1.75f;
                btn.setShowAnim(new ButtonGadget.AnimData(pos, null));
                btn.setDisableAnim(new ButtonGadget.AnimData(pos, null));
                btn.setHideAnim(new ButtonGadget.AnimData(pos + new SharpKmyMath.Vector3(hidePos.x, 0, 0), null));

                //CAMERA_VERTICAL
                slider = this.getSlider(Slider.CAMERA_VERTICAL);
                slider.IsVertical = true;
                slider.Parts = Image.Parts.SLIDER;
                slider.PartsCenter = Image.Parts.SLIDER_CENTER;
                partsSize = Image.getSize(slider.Parts);
                pos.x = 0;
                pos.x += posMarginLeft - (partsSize.y / 2);
                pos.y = gridSize.y * 1.5f;
                slider.setShowAnim(new ButtonGadget.AnimData(pos, null));
                slider.setDisableAnim(new ButtonGadget.AnimData(pos, null));
                slider.setHideAnim(new ButtonGadget.AnimData(pos + new SharpKmyMath.Vector3(-hidePos.x, 0, 0), null));
                slider.setHitMargin(new SharpKmyMath.Vector2(partsSize.y / 2, partsSize.y / 2));

                //CAMERA_HORIZONTAL
                slider = this.getSlider(Slider.CAMERA_HORIZONTAL);
                slider.Parts = Image.Parts.SLIDER;
                slider.PartsCenter = Image.Parts.SLIDER_CENTER;
                partsSize = Image.getSize(slider.Parts);
                pos.x = gridSize.x * 6.25f - partsSize.x;
                pos.y = screenSize.y - partsSize.y;
                pos.y -= posMarginBottom;
                slider.setShowAnim(new ButtonGadget.AnimData(pos, null));
                slider.setDisableAnim(new ButtonGadget.AnimData(pos, null));
                slider.setHideAnim(new ButtonGadget.AnimData(pos + new SharpKmyMath.Vector3(0, hidePos.y, 0), null));
                slider.setHitMargin(new SharpKmyMath.Vector2(partsSize.y / 2, partsSize.y / 2));

                this.hide(0);
            }
            #endregion
        }

        public void update()
        {
            this.mTouch.update();

            //モードの変更
            this.updateMode();

            //ガジェットアップデート
            var gadgetList = this.getShowGadget(ShowGadget.All);
            foreach (var gadget in gadgetList) gadget.update();
        }

        void updateMode()
        {
            if (this.Touch.isTouchDown() == false) return;
            var cameraBtn = this.getButton(Button.CAMERA);

            //Photoモードはどこ押しても切り替わるようにする、それ以外はボタンを押して切り替え
            if (this.mMode != Mode.Photo)
            {
                if (cameraBtn.isTouch() == false) return;
            }

            //モード切替
            {
                this.mMode++;
                switch (this.mMode)
                {
                    case Mode.CameraShow:
                        if (this.mapFixCamera
                        && this.mapFixCameraMode) this.mMode++;
                        break;
                }
                if (Mode.MAX <= this.mMode) this.mMode = (Mode)0;

            }

            //カラーの変更
            switch (this.mMode)
            {
                case Mode.Normal:
                    cameraBtn.Color = new SharpKmyMath.Vector4(255, 255, 255, 255 * 0.50f);
                    break;
                case Mode.CameraShow:
                    cameraBtn.Color = new SharpKmyMath.Vector4(109, 247, 156, 255 * 0.75f);
                    break;
                case Mode.Photo:
                    cameraBtn.Color = new SharpKmyMath.Vector4(0, 0, 0, 0);
                    break;
            }
            this.updateModeShow();
        }

        void updateModeShow()
        {
            if (this.mIsShow)
            {
                this.show(this.mShowGadget, this.mShowTime);
            }
            else
            {
                this.hide(this.mShowTime);
            }
        }

        public void draw()
        {
            var gadgetList = this.getShowGadget(ShowGadget.All);
            foreach (var gadget in gadgetList) gadget.draw();
        }


        public void show(ShowGadget inShowGadget = ShowGadget.Walk, float inTime = ShowTime)
        {
            this.mIsShow = true;
            this.mShowGadget = inShowGadget;
            this.mShowTime = inTime;
            List<ButtonGadget> disable = new List<ButtonGadget>();
            List<ButtonGadget> hide = new List<ButtonGadget>();
            var show = this.getShowGadget(inShowGadget, ref disable, ref hide);

            foreach (var gadget in show) gadget.show(inTime);
            foreach (var gadget in disable) gadget.disable(inTime);

            // 状態がイベント中の場合は操作禁止検出後1フレーム経過してからガジェットを隠す
            // これにより、イベントが一瞬で終了する場合にガジェットを隠そうとして表示がぶれる不具合を防ぐ
            if (inShowGadget == ShowGadget.Event)
            {
                if (SharpKmyIO.Controller.elapsedFramesDuringEvent > 1)
                {
                    foreach (var gadget in hide) gadget.hide(inTime);
                }
            }
            else
            {
                foreach (var gadget in hide) gadget.hide(inTime);
            }
        }

        public void hide(float inTime = ShowTime)
        {
            this.mIsShow = false;
            this.mShowGadget = ShowGadget.All;
            this.mShowTime = inTime;
            var show = this.getShowGadget(ShowGadget.All);
            foreach (var gadget in show) gadget.hide(inTime);
        }


        public List<ButtonGadget> getShowGadget(ShowGadget inShowGadget)
        {
            List<ButtonGadget> disable = new List<ButtonGadget>();
            List<ButtonGadget> hide = new List<ButtonGadget>();
            var show = getShowGadget(inShowGadget, ref disable, ref hide);
            return show;
        }

        public List<ButtonGadget> getShowGadget(ShowGadget inShowGadget, ref List<ButtonGadget> outDisableGadget, ref List<ButtonGadget> outHideGadget)
        {
            outDisableGadget = new List<ButtonGadget>();
            outHideGadget = new List<ButtonGadget>();

            Mode mode = this.mMode;
            List<ButtonGadget> show = new List<ButtonGadget>();
            List<ButtonGadget> disable = new List<ButtonGadget>();
            switch (inShowGadget)
            {
                case ShowGadget.All:
                    foreach (var gadget in this.mButtonList) show.Add(gadget);
                    foreach (var gadget in this.mStickList) show.Add(gadget);
                    foreach (var gadget in this.mSliderList) show.Add(gadget);
                    return show;
                //case ShowGadget.Load:
                //    disable.Add(this.getButton(Button.CHECK));
                //    disable.Add(this.getStick(Stick.MOVE));
                //    break;
                case ShowGadget.Logo:
                    break;
                case ShowGadget.Title:
                    disable.Add(this.getButton(Button.CHECK));
                    disable.Add(this.getStick(Stick.MOVE));
                    break;
                case ShowGadget.Movie:
                    show.Add(this.getButton(Button.CHECK));
                    disable.Add(this.getStick(Stick.MOVE));
                    break;
                case ShowGadget.Message:
                    show.Add(this.getButton(Button.CHECK));
                    disable.Add(this.getStick(Stick.MOVE));
                    break;
                case ShowGadget.MessageChoices:
                    disable.Add(this.getButton(Button.CHECK));
                    disable.Add(this.getStick(Stick.MOVE));
                    break;
                case ShowGadget.Event:
                    show.Add(this.getButton(Button.CHECK));
                    disable.Add(this.getStick(Stick.MOVE));
                    break;
                case ShowGadget.Menu:
                    disable.Add(this.getButton(Button.CHECK));
                    disable.Add(this.getStick(Stick.MOVE));
                    break;
                case ShowGadget.Battle:
                    mode = Mode.Normal;
                    show.Add(this.getButton(Button.DASH));
                    show.Add(this.getButton(Button.CHECK));
                    show.Add(this.getStick(Stick.MOVE));
                    break;
                default:
                    show = this.getShowGadget(ShowGadget.All);
                    break;
            }

            //モードによって表示ボタンの削減
            switch (mode)
            {
                case Mode.Normal:
                    {
                        var cameraList = this.getCameraGadget();
                        foreach (var gadget in cameraList) show.Remove(gadget);
                        foreach (var gadget in cameraList) disable.Remove(gadget);
                        break;
                    }
                case Mode.Photo:
                    show = new List<ButtonGadget>();
                    disable = new List<ButtonGadget>();
                    break;
                default:
                    break;
            }

            //mapFixCamera がカメラアングルの禁止状態
            if (mapFixCamera)
            {
                show.Remove(this.getButton(Button.RESET));
                show.Remove(this.getButton(Button.ZOOM_PLUS));
                show.Remove(this.getButton(Button.ZOOM_MINUS));
                show.Remove(this.getSlider(Slider.CAMERA_HORIZONTAL));
                show.Remove(this.getSlider(Slider.CAMERA_VERTICAL));
            }
            //mapFixCameraMode がカメラモード切り替え（TP / FP）の禁止状態です
            if (mapFixCameraMode)
            {
                show.Remove(this.getButton(Button.FPS));
            }

            //隠すガジェットの抽出
            {
                outHideGadget = this.getShowGadget(ShowGadget.All);
                foreach (var gadget in show) outHideGadget.Remove(gadget);
                foreach (var gadget in disable) outHideGadget.Remove(gadget);
            }

            outDisableGadget = disable;
            return show;
        }

        public List<ButtonGadget> getCameraGadget()
        {
            List<ButtonGadget> res = new List<ButtonGadget>();
            res.Add(this.getButton(Button.FPS));
            res.Add(this.getButton(Button.RESET));
            res.Add(this.getButton(Button.ZOOM_PLUS));
            res.Add(this.getButton(Button.ZOOM_MINUS));
            res.Add(this.getSlider(Slider.CAMERA_HORIZONTAL));
            res.Add(this.getSlider(Slider.CAMERA_VERTICAL));
            return res;
        }


        public ButtonGadget getButton(Button inButton)
        {
            return this.mButtonList[(int)inButton];
        }

        public StickGadget getStick(Stick inStick)
        {
            return this.mStickList[(int)inStick];
        }

        public SliderGadget getSlider(Slider inSlider)
        {
            return this.mSliderList[(int)inSlider];
        }

        public bool getValue(string inKeyType, ref int outValue)
        {
            outValue = 0;
            if (this.Enable == false) return false;

            switch (inKeyType)
            {
                // キーボード
                case "UP_0":
                    if (this.getStick(Stick.MOVE).getTouchButton() == StickGadget.Button.UP) outValue = 1;
                    if (this.getStick(Stick.MOVE).getTouchButton() == StickGadget.Button.RIGHT_UP) outValue = 1;
                    if (this.getStick(Stick.MOVE).getTouchButton() == StickGadget.Button.LEFT_UP) outValue = 1;
                    break;
                case "DOWN_0":
                    if (this.getStick(Stick.MOVE).getTouchButton() == StickGadget.Button.DOWN) outValue = 1;
                    if (this.getStick(Stick.MOVE).getTouchButton() == StickGadget.Button.LEFT_DOWN) outValue = 1;
                    if (this.getStick(Stick.MOVE).getTouchButton() == StickGadget.Button.RIGHT_DOWN) outValue = 1;
                    break;
                case "LEFT_0":
                    if (this.getStick(Stick.MOVE).getTouchButton() == StickGadget.Button.LEFT) outValue = 1;
                    if (this.getStick(Stick.MOVE).getTouchButton() == StickGadget.Button.LEFT_DOWN) outValue = 1;
                    if (this.getStick(Stick.MOVE).getTouchButton() == StickGadget.Button.LEFT_UP) outValue = 1;
                    break;
                case "RIGHT_0":
                    if (this.getStick(Stick.MOVE).getTouchButton() == StickGadget.Button.RIGHT) outValue = 1;
                    if (this.getStick(Stick.MOVE).getTouchButton() == StickGadget.Button.RIGHT_DOWN) outValue = 1;
                    if (this.getStick(Stick.MOVE).getTouchButton() == StickGadget.Button.RIGHT_UP) outValue = 1;
                    break;
                case "DECIDE_0":
                    if (this.getButton(Button.CHECK).isTouch()) outValue = 1;
                    break;
                case "CANCEL_0":
                    if (this.mShowGadget == ShowGadget.Battle)
                    {
                        if (this.getButton(Button.DASH).isTouch()) outValue = 1;//#24031
                    }
                    if (this.getButton(Button.MENU).isTouch()) outValue = 1;
                    break;
                case "DASH_0":
                    if (this.getButton(Button.DASH).isTouch()) outValue = 1;
                    break;

                case "PAD_CAMERA_CONTROL_MODE_CHANGE":
                    if (this.getButton(Button.FPS).isTouch()) outValue = 1;
                    break;
                case "PAD_CAMERA_POSITION_RESET":
                    if (this.getButton(Button.RESET).isTouch()) outValue = 1;
                    break;
                case "PAD_CAMERA_ZOOM_IN":
                    if (this.getButton(Button.ZOOM_PLUS).isTouch()) outValue = 1;
                    break;
                case "PAD_CAMERA_ZOOM_OUT":
                    if (this.getButton(Button.ZOOM_MINUS).isTouch()) outValue = 1;
                    break;
                case "PAD_CAMERA_VERTICAL_ROT_UP":
                case "PAD_CAMERA_VERTICAL_ROT_DOWN":
                    if (this.getSlider(Slider.CAMERA_VERTICAL).getTouchButton() == SliderGadget.Button.PLUS) outValue = 1;
                    if (this.getSlider(Slider.CAMERA_VERTICAL).getTouchButton() == SliderGadget.Button.MINUS) outValue = -1;
                    break;
                case "PAD_CAMERA_HORIZONTAL_ROT_CLOCKWISE":
                case "PAD_CAMERA_HORIZONTAL_ROT_COUNTER_CLOCKWISE":
                    if (this.getSlider(Slider.CAMERA_HORIZONTAL).getTouchButton() == SliderGadget.Button.PLUS) outValue = 1;
                    if (this.getSlider(Slider.CAMERA_HORIZONTAL).getTouchButton() == SliderGadget.Button.MINUS) outValue = -1;
                    break;
            }

            if (outValue != 0) return true;
            return false;
        }

        public SharpKmyMath.Vector2 getAxis()
        {
            return getStick(Stick.MOVE).getAxis();
        }

    }

    ////////////////////////////
    //Touch
    ////////////////////////////
    public class Touch
    {
        public class Point
        {
            public enum StateEnum
            {
                None = 0,
                Down,
                Hold,
                Up,
            }

            public int Id = -1;
            public SharpKmyMath.Vector2 Pos = new SharpKmyMath.Vector2(0, 0);
            public StateEnum State = StateEnum.None;
        }

        Dictionary<int, Point> mPointDic = new Dictionary<int, Point>();
        public Point GetPoint(int inId) { return mPointDic[inId]; }
        List<Point> mPointList = new List<Point>();
        public List<Point> GetPoint() { return this.mPointList; }

        int mDraggingFrames = 0;

        public void update()
        {
            this.updatePoint();
            this.updateDic();
            this.updateState();

            if (this.isTouchPressed()) ++this.mDraggingFrames;
            else this.mDraggingFrames = 0;
        }

        public void updatePoint()
        {
            List<Point> pointList = new List<Point>();
            for (int i1 = 0; true; ++i1)
            {
                var point = this.getPointFromIndex(i1);
                if (point == null) break;
                pointList.Add(point);
            }

            //タッチされていないければ抜ける
            if (pointList.Count == 0)
            {
                foreach (var point in this.mPointList) point.State = Point.StateEnum.Up;
                this.mPointList.Clear();
                return;
            }

            //すでにタッチされている位置の更新
            var updatePoint = new List<Point>();
            var pointRemoveList = new List<Point>();
            foreach (var point in pointList)
            {
                if (this.mPointDic.ContainsKey(point.Id) == false) continue;
                pointRemoveList.Add(point);

                var point2 = this.mPointDic[point.Id];
                point2.Pos = point.Pos;
                updatePoint.Add(point2);
                this.mPointList.Remove(point2);
            }

            //アップデートに追加
            foreach (var point in pointRemoveList) pointList.Remove(point);
            updatePoint.AddRange(pointList);

            //現状に更新
            foreach (var point in this.mPointList) point.State = Point.StateEnum.Up;
            this.mPointList = updatePoint;
        }

        public void updateDic()
        {
            this.mPointDic.Clear();
            foreach (var point in this.mPointList)
            {
                this.mPointDic[point.Id] = point;
            }
        }

        public void updateState()
        {
            //if (this.mPointList.Count != 0)
            //    UnityEngine.Debug.Log("mPointList.size:" + this.mPointList.Count + "---" + Input.touchCount);

            foreach (var point in this.mPointList)
            {
                //UnityEngine.Debug.Log("id:" + point.Id + "  state:" + point.State + "   pos x:" + point.Pos.x + " y" + point.Pos.y);

                switch (point.State)
                {
                    case Point.StateEnum.None:
                        point.State = Point.StateEnum.Down;
                        break;
                    case Point.StateEnum.Down:
                        point.State = Point.StateEnum.Hold;
                        break;
                }
            }
        }

        public Point getPointFromIndex(int inIndex = 0)
        {
            var screenSize = new SharpKmyMath.Vector2(Yukar.Engine.Graphics.ScreenWidth, Yukar.Engine.Graphics.ScreenHeight);

            if (this.isTouchPressedFromIndex(inIndex) == false) return null;
            var res = new Point();

            // touchId : 2本目の指だったら1、3本目だったら2
            // タッチ座標が必ず0～960,0～540になるようにしている
            SharpKmyMath.Vector2 pos = new SharpKmyMath.Vector2();
            // アス比の違いに対応するため
            var offsetX = SharpKmyGfx.SpriteBatch.offsetX;
            var offsetY = SharpKmyGfx.SpriteBatch.offsetY;
            if (Input.touchCount > inIndex)
            {
                // iOS / Android
                var myTouch = Input.GetTouch(inIndex);
                pos.x = (int)((myTouch.position.x - offsetX) / (Screen.width - offsetX * 2) * (screenSize.x));
                pos.y = (int)((myTouch.position.y - offsetY) / (Screen.height - offsetY * 2) * (screenSize.y));
                res.Id = myTouch.fingerId + 1;
                //UnityEngine.Debug.Log("touchPos : " + inIndex + "(" + myTouch.fingerId + ") : " + pos.x + " " + pos.y + "---" + Input.touchCount);
            }
            else
            {
                // Windows
                pos.x = (int)((Input.mousePosition.x - offsetX) / (Screen.width - offsetX * 2) * (screenSize.x));
                pos.y = (int)((Input.mousePosition.y - offsetY) / (Screen.height - offsetY * 2) * (screenSize.y));
                res.Id = 0;
                //UnityEngine.Debug.Log("touchPos : " + pos.x + " " + pos.y);
            }
            if (pos.x < 0) pos.x = 0;
            if (pos.x > screenSize.x) pos.x = screenSize.x;
            if (pos.y < 0) pos.y = 0;
            if (pos.y > screenSize.y) pos.y = screenSize.y;
            pos.y = screenSize.y - pos.y;

            res.Pos = pos;
            return res;
        }

        public bool isTouchPressedFromIndex(int inIndex = 0)
        {
            if (0 <= inIndex && inIndex < Input.touchCount)
                return true;

            if (Input.touchCount == 0
            && Input.touchCount == inIndex
            && Input.GetMouseButton(0))
                return true;

            return false;
        }

        public bool isTouchPressed()
        {
            return this.mPointList.Count != 0;
        }

        public bool isTouchDown()
        {
            return this.mDraggingFrames == 1;
        }

    }

    ////////////////////////////
    //ButtonGadget
    ////////////////////////////
    public class ButtonGadget
    {
        public struct AnimData
        {
            public AnimData(SharpKmyMath.Vector3 inPos, SharpKmyMath.Vector4 inColor)
            {
                this.Pos = inPos;
                this.Color = inColor;
            }

            public SharpKmyMath.Vector3 Pos;
            public SharpKmyMath.Vector4 Color;
        }

        protected ControllerVirtual mController = null;
        protected Image.Parts mParts = Image.Parts.None;
        public Image.Parts Parts { get { return this.mParts; } set { this.mParts = value; } }

        public SharpKmyMath.Vector3 Pos { get { return this.mAnimNow.Pos; } }//set { this.mPos = value; } }

        protected AnimData mAnimNow = new AnimData();
        protected AnimData mAnimShow = new AnimData();
        protected AnimData mAnimHide = new AnimData();
        protected AnimData mAnimDisable = new AnimData();

        public AnimData GetAnimShow() { return this.mAnimShow; }

        protected SharpKmyMath.Vector4 mHitRange = null;
        protected SharpKmyMath.Vector2 mHitMargin = new SharpKmyMath.Vector2(0, 0);
        protected SharpKmyMath.Vector4 mColor = null;
        public SharpKmyMath.Vector4 Color { get { return this.mColor; } set { this.mColor = value; } }

        float mMoveStartTime = -1;
        float mMoveEndTime = -1;
        float mMoveElapsedTime = 0;
        protected AnimData mMoveSrc = new AnimData();
        protected AnimData mMoveDst = new AnimData();

        protected Touch.Point mTouchPoint = null;

        public enum State
        {
            None,
            ShowMove,
            Show,
            DisableMove,
            Disable,
            HideMove,
            Hide,
        }

        protected State mState = State.None;

        public ButtonGadget(ControllerVirtual inController)
        {
            this.mController = inController;
        }

        public void setShowAnim(AnimData inData)
        {
            this.mState = State.Show;
            this.mMoveElapsedTime = 0;
            this.mAnimShow = inData;
        }

        public void setDisableAnim(AnimData inData)
        {
            this.mAnimDisable = inData;
        }

        public void setHideAnim(AnimData inData)
        {
            this.mAnimHide = inData;
        }

        public virtual void setHitRange(SharpKmyMath.Vector4 inRange)
        {
            this.mHitRange = inRange;
        }

        public virtual void setHitMargin(SharpKmyMath.Vector2 inMargin)
        {
            this.mHitMargin = inMargin;
        }

        public bool isShow(State inState = State.None)
        {
            State state = inState;
            if (state == State.None) state = this.mState;
            if (state == State.Show) return true;
            if (state == State.ShowMove) return true;
            return false;
        }

        public bool isHide(State inState = State.None)
        {
            State state = inState;
            if (state == State.None) state = this.mState;
            if (state == State.Hide) return true;
            if (state == State.HideMove) return true;
            return false;
        }

        public bool isDisable(State inState = State.None)
        {
            State state = inState;
            if (state == State.None) state = this.mState;
            if (state == State.Disable) return true;
            if (state == State.DisableMove) return true;
            return false;
        }

        public void show(float inTime)
        {
            if (this.isShow()) return;
            this.setState(inTime, State.Show);
        }

        public void disable(float inTime)
        {
            if (this.isDisable()) return;
            this.setState(inTime, State.Disable);
        }

        public void hide(float inTime)
        {
            if (this.isHide()) return;
            this.setState(inTime, State.Hide);
        }



        void setState(float inTime, State inState)
        {
            //srcチェック
            if (this.isShow())
            {
                if (this.isShow(inState)) return;
                this.mMoveSrc = this.mAnimShow;
            }
            if (this.isDisable())
            {
                if (this.isDisable(inState)) return;
                this.mMoveSrc = this.mAnimDisable;
            }
            if (this.isHide())
            {
                if (this.isHide(inState)) return;
                this.mMoveSrc = this.mAnimHide;
            }

            //dstチェック
            if (this.isShow(inState))
            {
                this.mMoveDst = this.mAnimShow;
                this.mState = State.ShowMove;
            }
            if (this.isDisable(inState))
            {
                this.mMoveDst = this.mAnimDisable;
                this.mState = State.DisableMove;
            }
            if (this.isHide(inState))
            {
                this.mMoveDst = this.mAnimHide;
                this.mState = State.HideMove;
            }

            this.mMoveStartTime = 0;
            this.mMoveElapsedTime = this.mMoveEndTime - this.mMoveElapsedTime;
            if (this.mMoveElapsedTime < 0) this.mMoveElapsedTime = 0;
            this.mMoveEndTime = inTime;
        }

        public virtual void update()
        {
            switch (this.mState)
            {
                case State.None:
                case State.Show:
                case State.Disable:
                case State.Hide:
                    break;
                case State.ShowMove:
                case State.DisableMove:
                case State.HideMove:
                    //表示・非表示が終わっていた場合
                    this.mMoveElapsedTime += Time.deltaTime;
                    if (this.mMoveEndTime < this.mMoveElapsedTime)
                    {
                        if (this.mState == State.ShowMove) this.mState = State.Show;
                        if (this.mState == State.DisableMove) this.mState = State.Disable;
                        if (this.mState == State.HideMove) this.mState = State.Hide;
                        this.mAnimNow.Pos = this.mMoveDst.Pos;

                        var color = this.mColor;
                        if (color == null) color = Image.getColor(this.Parts);
                        if (this.mMoveDst.Color == null) this.mAnimNow.Color = null;
                        else this.mAnimNow.Color = new SharpKmyMath.Vector4(255) * this.mMoveDst.Color;
                        break;
                    }
                    {
                        //表示・非表示の移動処理
                        float per = 1 - ((this.mMoveEndTime - this.mMoveElapsedTime) / (this.mMoveEndTime - this.mMoveStartTime));
                        this.mAnimNow.Pos = this.mMoveSrc.Pos + ((this.mMoveDst.Pos - this.mMoveSrc.Pos) * per);

                        //表示・非表示のカラー処理
                        var color = this.mColor;
                        if (color == null) color = Image.getColor(this.Parts);
                        color /= 255;
                        var colorSrc = this.mMoveSrc.Color;
                        var colorDst = this.mMoveDst.Color;
                        if (colorSrc == null) colorSrc = color;
                        if (colorDst == null) colorDst = color;
                        var moveColor = colorSrc + ((colorDst - colorSrc) * per);
                        this.mAnimNow.Color = new SharpKmyMath.Vector4(255) * moveColor;
                    }
                    break;
            }
        }

        public virtual void draw()
        {
            var color = this.mAnimNow.Color;
            if (color == null) color = this.mColor;

            Image.draw(this.mParts, this.Pos, 0, color);
        }

        public virtual bool isRange(SharpKmyMath.Vector2 inPos)
        {
            var partSize = Image.getSize(this.mParts);
            var pos = this.GetAnimShow().Pos;

            //当たり判定が用意されているの出ればそれを使う
            if (this.mHitRange != null)
            {
                if (this.mHitRange.x <= inPos.x && inPos.x <= this.mHitRange.z
                   && this.mHitRange.y <= inPos.y && inPos.y <= this.mHitRange.w)
                {
                    return true;
                }
                return false;
            }

            //単純な画像あたり判定
            {
                if (pos.x <= inPos.x && inPos.x <= pos.x + partSize.x
                   && pos.y <= inPos.y && inPos.y <= pos.y + partSize.y)
                {
                    return true;
                }
            }
            return false;
        }

        public virtual bool isTouch()
        {
            if (this.isShow() == false) return false;

            //上がっていたら初期化する
            if (this.mTouchPoint != null)
            {
                if (this.mTouchPoint.State == Touch.Point.StateEnum.Up)
                {
                    this.mTouchPoint = null;
                }
            }

            //当たっていないか調べる
            if (this.mTouchPoint == null)
            {
                var pointList = this.mController.Touch.GetPoint();
                foreach (var point in pointList)
                {
                    if (point.State != Touch.Point.StateEnum.Down) continue;
                    if (this.isRange(point.Pos) == false) continue;
                    this.mTouchPoint = point;
                }
                if (this.mTouchPoint != null) return true;
                return false;
            }

            if (this.isRange(this.mTouchPoint.Pos)) return true;
            return false;
        }
    }

    ////////////////////////////
    //StickGadget
    ////////////////////////////
    public class StickGadget : ButtonGadget
    {
        Image.Parts mPartsCenter = Image.Parts.None;
        public Image.Parts PartsCenter { get { return this.mPartsCenter; } set { this.mPartsCenter = value; } }
        SharpKmyMath.Vector3 mPosCenter = new SharpKmyMath.Vector3(0);
        public bool IsFourWay = false;
        public bool IsForceTouchDown = false;

        private SharpKmyMath.Vector2 axis = new SharpKmyMath.Vector2(0.0f, 0.0f);

        public enum Button
        {
            None = 0,
            RIGHT,
            RIGHT_UP,
            UP,
            LEFT_UP,
            LEFT,
            LEFT_DOWN,
            DOWN,
            RIGHT_DOWN,
        }


        public StickGadget(ControllerVirtual inController) : base(inController)
        {
        }

        public override void update()
        {
            base.update();

            //スティックの傾きを表現
            {
                const float dist = 20.0f, distDash = 40.0f;
                var btnDash = this.mController.getButton(ControllerVirtual.Button.DASH);
                var isDash = btnDash.isTouch();

                const float r2 = 1 / 1.41421356f;

                // DisableMove状態の時にポジションをセットすると、イベントが一瞬で終了する場合にスティックの表示がぶれてしまう
                // その対策として、DisableMove状態の時はあえてポジションをセットしない
                if (this.mState != State.DisableMove)
                {
                    this.mPosCenter = this.Pos;
                }

                if(mController.GameMain != null)
                {
                    var mapScene = mController.GameMain.mapScene;
                    if(mapScene != null)
                    {
                        if(mapScene.messageWindow.isVisible() || mapScene.choicesWindow.isVisible()) 
                        {
                            this.mPosCenter = this.Pos;
                            return;
                        }
                    }
                }
                
                this.mPosCenter.z -= 1.0f;
                if (!IsFourWay)
                {
                    this.mPosCenter.x += ((isDash) ? axis.x * distDash : axis.x * dist);
                    this.mPosCenter.y -= ((isDash) ? axis.y * distDash : axis.y * dist);
                }
                else
                {
                    switch (this.getTouchButton())
                    {
                        case Button.RIGHT:
                            this.mPosCenter.x += ((isDash) ? distDash : dist);
                            break;
                        case Button.RIGHT_UP:
                            this.mPosCenter.x += ((isDash) ? distDash : dist) * r2;
                            this.mPosCenter.y -= ((isDash) ? distDash : dist) * r2;
                            break;
                        case Button.UP:
                            this.mPosCenter.y += -((isDash) ? distDash : dist);
                            break;
                        case Button.LEFT_UP:
                            this.mPosCenter.x -= ((isDash) ? distDash : dist) * r2;
                            this.mPosCenter.y -= ((isDash) ? distDash : dist) * r2;
                            break;
                        case Button.LEFT:
                            this.mPosCenter.x += -((isDash) ? distDash : dist);
                            break;
                        case Button.LEFT_DOWN:
                            this.mPosCenter.x -= ((isDash) ? distDash : dist) * r2;
                            this.mPosCenter.y += ((isDash) ? distDash : dist) * r2;
                            break;
                        case Button.DOWN:
                            this.mPosCenter.y += ((isDash) ? distDash : dist);
                            break;
                        case Button.RIGHT_DOWN:
                            this.mPosCenter.x += ((isDash) ? distDash : dist) * r2;
                            this.mPosCenter.y += ((isDash) ? distDash : dist) * r2;
                            break;
                    }
                }
            }
        }

        public override void draw()
        {
            base.draw();
            Image.draw(this.mPartsCenter, this.mPosCenter, 0, this.mAnimNow.Color);
        }

        public override bool isRange(SharpKmyMath.Vector2 inPos)
        {
            //一度判定しているのであれば離すまで判定し続ける
            if (this.mTouchPoint != null)
            {
                if (this.mTouchPoint.State == Touch.Point.StateEnum.Down
                || this.mTouchPoint.State == Touch.Point.StateEnum.Hold)
                {
                    return true;
                }
            }

            return base.isRange(inPos);
        }

        public override bool isTouch()
        {
            //通常のタッチ処理（hide disable時はタッチ判定はなし
            if (this.IsForceTouchDown == false)
            {
                return base.isTouch();
            }

            //上がっていたら初期化する
            if (this.mTouchPoint != null)
            {
                if (this.mTouchPoint.State == Touch.Point.StateEnum.Up)
                {
                    this.mTouchPoint = null;
                }
            }

            //当たっていないか調べる
            if (this.mTouchPoint == null)
            {
                var pointList = this.mController.Touch.GetPoint();
                foreach (var point in pointList)
                {
                    if (point.State != Touch.Point.StateEnum.Down) continue;
                    if (this.isRange(point.Pos) == false) continue;
                    this.mTouchPoint = point;
                }
                if (this.mTouchPoint != null) return true;
                return false;
            }

            if (this.isRange(this.mTouchPoint.Pos)) return true;
            return false;
        }

        public virtual Button getTouchButton()
        {
            var screenSize = new SharpKmyMath.Vector2(Yukar.Engine.Graphics.ScreenWidth, Yukar.Engine.Graphics.ScreenHeight);

            if (this.isTouch() == false)
            {
                axis.x = 0.0f;
                axis.y = 0.0f;
                return Button.None;
            }
            if (this.isShow() == false)
            {
                // Disable状態の時にスティックを中央に戻さない（入力の方向は反映する）ようにする
                // そのために、Disable状態の時はあえてボタンの入力方向をNoneにしない
                if (this.mState != State.Disable)
                {
                    axis.x = 0.0f;
                    axis.y = 0.0f;
                    return Button.None;
                }
                if (mController.GameMain != null)
                {
                    var mapScene = mController.GameMain.mapScene;
                    if (mapScene != null)
                    {
                        if (mapScene.messageWindow.isVisible() || mapScene.choicesWindow.isVisible())
                        {
                            axis.x = 0.0f;
                            axis.y = 0.0f;
                            return Button.None;
                        }
                    }
                }
            }

            var touchPos = this.mTouchPoint.Pos;

            var anim = this.GetAnimShow();
            var partSize = Image.getSize(this.mParts);
            var ImageizeDiv2 = new SharpKmyMath.Vector2(partSize.x / 2, partSize.y / 2);
            var pos = new SharpKmyMath.Vector2(anim.Pos.x, anim.Pos.y) + ImageizeDiv2;

            // スティックの中心と範囲外は反応しないようにする 
            if (checkIncideCircle((int)pos.x, (int)pos.y, 15, (int)touchPos.x, (int)touchPos.y)
            || checkIncideCircle((int)pos.x, (int)pos.y, (int)screenSize.y, (int)touchPos.x, (int)touchPos.y) == false)
            {
                axis.x = 0.0f;
                axis.y = 0.0f;
                return Button.None;
            }
            calculateAxix(pos, touchPos);
            bool isFourWay = this.IsFourWay;
            float pi2 = (float)(Math.PI * 2);
            float rangeRad = (float)(Math.PI / 4);
            if (isFourWay) rangeRad *= 2;
            float stickRad = (float)Math.Atan2((-touchPos.y) - (-pos.y), touchPos.x - pos.x);
            stickRad += pi2;
            if (pi2 < stickRad) stickRad -= (float)(pi2);
            stickRad += rangeRad / 2;
            if (pi2 < stickRad) stickRad -= (float)(pi2);
            //UnityEngine.Debug.Log("stickRad " + stickRad * 180/Math.PI);
            if (isFourWay)
            {
                var res = (int)(stickRad / rangeRad);
                return (Button)((res * 2) + (int)Button.RIGHT);
            }
            else
            {
                //斜め判定を緩くする
                var playRad = rangeRad / 4;
                int[] res = new int[3];
                res[0] = (int)(stickRad / rangeRad);
                res[1] = (int)((stickRad - playRad) / rangeRad);
                res[2] = (int)((stickRad + playRad) / rangeRad);

                for (var i1 = 0; i1 < 3; ++i1)
                {
                    var btn = (Button)(res[i1] + (int)Button.RIGHT);
                    switch (btn)
                    {
                        case Button.RIGHT:
                        case Button.UP:
                        case Button.LEFT:
                        case Button.DOWN:
                            return btn;

                    }
                }

                return (Button)(res[0] + (int)Button.RIGHT);
            }
        }

        private void calculateAxix(SharpKmyMath.Vector2 center, SharpKmyMath.Vector2 touchPos)
        {
            var tempAxis = SharpKmyMath.Vector3.normalize(new SharpKmyMath.Vector3(touchPos.x - center.x, touchPos.y - center.y, 0.0f));
            axis = new SharpKmyMath.Vector2(tempAxis.x, -tempAxis.y);
        }

        // 円の中に入っているか
        internal bool checkIncideCircle(int centerX, int centerY, int rad, int posX, int posY)
        {
            int dist = (centerX - posX) * (centerX - posX) + (centerY - posY) * (centerY - posY);

            if (dist < rad * rad) return true;
            return false;
        }

        public SharpKmyMath.Vector2 getAxis()
        {
            return axis;
        }


        /*
        // どの方向にドラッグしたか(現在未使用)
        SharpKmyMath.Vector2 drugStartPos = new SharpKmyMath.Vector2();
        const int drugStartDist = 32;
        const int dashStartDist = 100;
        internal void checkDrugVector(SharpKmyMath.Vector2 touchPos)
        {
            if (Controller.draggingFrames == 0) drugStartPos = touchPos;
            SharpKmyMath.Vector2 dist = new SharpKmyMath.Vector2();
            dist = touchPos - drugStartPos;
            //UnityEngine.Debug.Log(dist.x + " " + dist.y);
            //UnityEngine.Debug.Log(drugStartPos.x + " " + drugStartPos.y + " " + touchPos.x + " " + touchPos.y);

            if (dist.x * dist.x + dist.y * dist.y < drugStartDist * drugStartDist) return;
            if (Math.Abs(dist.x) > Math.Abs(dist.y)) // 左右
            {
                if (dist.x > 0) buttonFlags |= ButtonFlags.RIGHT;
                else buttonFlags |= ButtonFlags.LEFT;
            }
            else // 上下
            {
                if (dist.y > 0) buttonFlags |= ButtonFlags.UP;
                else buttonFlags |= ButtonFlags.DOWN;
            }

            if (dist.x * dist.x + dist.y * dist.y > dashStartDist * dashStartDist) buttonFlags |= ButtonFlags.DASH; // ダッシュ

        }
        */
    }

    ////////////////////////////
    //SliderGadget
    ////////////////////////////
    public class SliderGadget : ButtonGadget
    {

        Image.Parts mPartsCenter = Image.Parts.None;
        public Image.Parts PartsCenter { get { return this.mPartsCenter; } set { this.mPartsCenter = value; } }
        SharpKmyMath.Vector3 mPosCenter = new SharpKmyMath.Vector3(0);
        bool mIsVertical = false;
        public bool IsVertical { get { return this.mIsVertical; } set { this.mIsVertical = value; } }


        public enum Button
        {
            None = 0,
            MINUS,
            PLUS,
        }


        public SliderGadget(ControllerVirtual inController) : base(inController)
        {
        }

        public override void update()
        {
            base.update();

            //スティックの傾きを表現
            {
                var partSize = Image.getSize(this.mParts);
                var partCenterSize = Image.getSize(this.mPartsCenter);

                this.mPosCenter = this.Pos;
                this.mPosCenter.z -= 1.0f;
                float center = (partSize.x / 2.0f);
                float dist = center - (partCenterSize.y / 2.0f);
                if (this.mIsVertical)
                {
                    this.mPosCenter.y += center - (partCenterSize.x / 2.0f);
                    this.mPosCenter.z -= 1.0f;
                    switch (this.getTouchButton())
                    {
                        case Button.MINUS:
                            this.mPosCenter.y += +(dist);
                            break;
                        case Button.PLUS:
                            this.mPosCenter.y += -(dist);
                            break;
                    }
                }
                else
                {
                    this.mPosCenter.x += center - (partCenterSize.x / 2.0f);
                    this.mPosCenter.z -= 1.0f;
                    switch (this.getTouchButton())
                    {
                        case Button.MINUS:
                            this.mPosCenter.x += -(dist);
                            break;
                        case Button.PLUS:
                            this.mPosCenter.x += +(dist);
                            break;
                    }
                }
            }
        }

        public override void draw()
        {

            if (this.IsVertical)
            {
                float angle = 90.0f;

                var pos = this.Pos;
                var partSize = Image.getSize(this.mParts);
                pos.x -= (partSize.x / 2) - (partSize.y / 2);
                pos.y += (partSize.x / 2) - (partSize.y / 2);
                Image.draw(this.mParts, pos, angle);

                var partCenterSize = Image.getSize(this.mPartsCenter);
                pos = this.mPosCenter;
                pos.x -= (partCenterSize.x / 2) - (partCenterSize.y / 2);
                pos.y += (partCenterSize.x / 2) - (partCenterSize.y / 2);
                Image.draw(this.mPartsCenter, pos, angle);
            }
            else
            {
                Image.draw(this.mParts, this.Pos);
                Image.draw(this.mPartsCenter, this.mPosCenter);
            }
        }


        public override bool isRange(SharpKmyMath.Vector2 inPos)
        {
            //一度判定しているのであれば離すまで判定し続ける
            if (this.mTouchPoint != null)
            {
                if (this.mTouchPoint.State == Touch.Point.StateEnum.Down
                || this.mTouchPoint.State == Touch.Point.StateEnum.Hold)
                {
                    return true;
                }
            }

            //縦横を含めた判定処理
            var anim = this.GetAnimShow();
            var partSize = Image.getSize(this.mParts);
            var vrCtrlPartSizeDiv2 = new SharpKmyMath.Vector2(partSize.x / 2, partSize.y / 2);
            var pos = inPos;

            var hitRange = new SharpKmyMath.Vector4(anim.Pos.x, anim.Pos.y, anim.Pos.x + partSize.x, anim.Pos.y + partSize.y);
            if (this.mHitRange != null) hitRange = this.mHitRange;
            if (this.mIsVertical)
            {
                pos.y += vrCtrlPartSizeDiv2.x;
                if (this.mHitRange == null)
                {
                    hitRange = new SharpKmyMath.Vector4(anim.Pos.x, anim.Pos.y, anim.Pos.x + partSize.y, anim.Pos.y + partSize.x);
                }
            }

            hitRange.x -= this.mHitMargin.x;
            hitRange.y -= this.mHitMargin.y;
            hitRange.z += this.mHitMargin.x;
            hitRange.w += this.mHitMargin.y;

            if (hitRange.x <= inPos.x && inPos.x <= hitRange.z
               && hitRange.y <= inPos.y && inPos.y <= hitRange.w)
            {
                return true;
            }
            return false;
        }

        public Button getTouchButton()
        {
            if (this.isShow() == false) return Button.None;
            if (this.isTouch() == false) return Button.None;

            var touchPos = this.mTouchPoint.Pos;

            var anim = this.GetAnimShow();
            var partSize = Image.getSize(this.mParts);
            var VRCtrlPartSizeDiv2 = new SharpKmyMath.Vector2(partSize.x / 2, partSize.y / 2);
            var pos = new SharpKmyMath.Vector2(anim.Pos.x, anim.Pos.y);
            if (this.mIsVertical)
            {
                pos.x += VRCtrlPartSizeDiv2.y;
                pos.y += VRCtrlPartSizeDiv2.x;
            }
            else
            {
                pos += VRCtrlPartSizeDiv2;
            }

            // スティックの中心は反応しないようにする 
            if (checkIncideCircle((int)pos.x, (int)pos.y, 15, (int)touchPos.x, (int)touchPos.y) == 1)
            {
                return Button.None;
            }

            if (checkIncideCircle((int)pos.x, (int)pos.y, 1000, (int)touchPos.x, (int)touchPos.y) == 0) return Button.None;

            var adjTouchPosX = touchPos.x - pos.x;
            var adjTouchPosY = touchPos.y - pos.y;
            if (this.mIsVertical)
            {
                if (adjTouchPosX > adjTouchPosY && -adjTouchPosX > adjTouchPosY) return Button.PLUS;
                else if (adjTouchPosX < adjTouchPosY && -adjTouchPosX < adjTouchPosY) return Button.MINUS;
            }
            else
            {
                if (adjTouchPosX < adjTouchPosY && adjTouchPosX < -adjTouchPosY) return Button.MINUS;
                else if (adjTouchPosX > adjTouchPosY && adjTouchPosX > -adjTouchPosY) return Button.PLUS;
            }

            return Button.None;
        }

        // 円の中に入っているか
        internal int checkIncideCircle(int centerX, int centerY, int rad, int posX, int posY)
        {
            int ret = 0;
            int dist = (centerX - posX) * (centerX - posX) + (centerY - posY) * (centerY - posY);

            if (dist < rad * rad) ret = 1;
            return ret;
        }

    }

}
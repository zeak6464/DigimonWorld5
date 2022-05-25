#define DIRECTINPUT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using KeyStates = Yukar.Engine.Input.KeyStates;
using Microsoft.Xna.Framework.Input;

namespace Yukar.Engine
{
    class Input
    {
        private static InputCore sInstance;

        public enum StateType
        {
            DIRECT,         // 生の入力状態 ふつう使わない
            TRIGGER,        // 入力された瞬間だけ受け付け 方向キー以外に使用してください
            TRIGGER_UP,     // 離された瞬間だけ受け付け 普段は使わない
            REPEAT,         // TRIGGERに加えてキーリピートする ト・・・・・トトトトトト という動き 方向キーに使用してください
        }

        [FlagsAttribute]
        public enum KeyStates
        {
            NONE = 0,
            UP = (1 << 0),
            DOWN = (1 << 1),
            LEFT = (1 << 2),
            RIGHT = (1 << 3),
            DECIDE = (1 << 4),
            CANCEL = (1 << 5),
            MENU = (1 << 6),
            L = (1 << 7),
            R = (1 << 8),

            CAMERA_VERTICAL_ROT_UP = (1 << 9),
            CAMERA_VERTICAL_ROT_DOWN = (1 << 10),
            CAMERA_HORIZONTAL_ROT_CLOCKWISE = (1 << 11),
            CAMERA_HORIZONTAL_ROT_COUNTER_CLOCKWISE = (1 << 12),
            CAMERA_ZOOM_IN = (1 << 13),
            CAMERA_ZOOM_OUT = (1 << 14),
            CAMERA_POSITION_RESET = (1 << 15),
            CAMERA_CONTROL_MODE_CHANGE = (1 << 16),

            DASH = (1 << 17),

            VR_CALIBRATION = (1 << 18),
            VR_SYNC_CAMERA = (1 << 19),
        }

        internal static void Initialize(GameMain inGameMain)
        {
            sInstance = new InputCore(inGameMain);
        }

        internal static void finalize()
        {
            sInstance.finalize();
        }

        internal static void Update()
        {
            sInstance.Update();
        }

        public static KeyStates GetState(StateType type)
        {
            switch (type)
            {
                case StateType.DIRECT:
                    return sInstance.nowState;
                case StateType.TRIGGER:
                    return sInstance.triggerState;
                case StateType.TRIGGER_UP:
                    return sInstance.triggerState2;
                case StateType.REPEAT:
                    return sInstance.repeatState;
            }

            return KeyStates.NONE;
        }

        public static bool KeyTest(StateType type, KeyStates key)
        {
            var state = GetState(type);
            return Common.Util.HasFlag(state, key);
        }

        public static float GetAxis(string keyName)
        {
            return sInstance.getAxis(keyName);
        }

        internal static bool IsVirtualPadEnable()
        {
            return sInstance.vPadEnable;
        }

        internal static void SetVirtualPadEnable()
        {
            sInstance.vPadEnable = true;
        }

        internal static void SetVirtualPadDisable()
        {
            sInstance.vPadEnable = false;
        }

        internal static void Draw()
        {
            sInstance.Draw();
        }

        internal static void ChangeInput(KeyStates keyStates)
        {
            sInstance.ChangeKeyStates(keyStates);
        }
    }

    class InputCore
    {
        private const float FRAME_PER_MS = 1000f / 60f;
        private const float REPEAT_TIME = 500 / FRAME_PER_MS; // 500ms後にリピート
        private const float REPEAT_INTERVAL = 4;              // 3フレームごとにリピート
                                                              //		private const int MAX_KEYSTATE = 19;
        private const bool DECIDE_B = true;                 // B で決定かどうか
        //private bool decideB = DECIDE_B;

        public enum KeyIndexes
        {
            NONE = 0,
            UP = 1,
            DOWN = 2,
            LEFT = 3,
            RIGHT = 4,
            DECIDE = 5,
            CANCEL = 6,
            MENU = 7,
            L = 8,
            R = 9,

            CAMERA_VERTICAL_ROT_UP = 10,
            CAMERA_VERTICAL_ROT_DOWN = 11,
            CAMERA_HORIZONTAL_ROT_CLOCKWISE = 12,
            CAMERA_HORIZONTAL_ROT_COUNTER_CLOCKWISE = 13,
            CAMERA_ZOOM_IN = 14,
            CAMERA_ZOOM_OUT = 15,
            CAMERA_POSITION_RESET = 16,
            CAMERA_CONTROL_MODE_CHANGE = 17,

            DASH = 18,

            VR_CALIBRATION = 19,
            VR_SYNC_CAMERA = 20,

            NUM
        }

        private const int MAX_KEYSTATE = (int)KeyIndexes.NUM;

        private float[] keyBuffer = new float[MAX_KEYSTATE];
        private float[] oldBuffer = new float[MAX_KEYSTATE];

        internal KeyStates nowState = KeyStates.NONE;      // 生
        internal KeyStates triggerState = KeyStates.NONE;  // 押した瞬間だけ反映
        internal KeyStates triggerState2 = KeyStates.NONE; // 離した瞬間だけ反映
        internal KeyStates repeatState = KeyStates.NONE;   // ト・・・・トトトトトトト　という風に反映

        public bool vPadEnable = true;
        static SharpKmyIO.Controller controller = null;
        public SharpKmyMath.Vector2 touchPos = new SharpKmyMath.Vector2();

        public InputCore(GameMain inGameMain)
        {
            controller = new SharpKmyIO.Controller();
#if WINDOWS
            controller.addInput("UP_0", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0x26, 1f);
            controller.addInput("DOWN_0", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0x28, 1f);
            controller.addInput("LEFT_0", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0x25, 1f);
            controller.addInput("RIGHT_0", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0x27, 1f);
            controller.addInput("UP_1", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'W', 1f);
            controller.addInput("DOWN_1", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'S', 1f);
            controller.addInput("LEFT_1", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'A', 1f);
            controller.addInput("RIGHT_1", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'D', 1f);
            controller.addInput("UP_2", 0, SharpKmyIO.INPUTID.kXINPUT_DPAD_UP, 1f);
            controller.addInput("DOWN_2", 0, SharpKmyIO.INPUTID.kXINPUT_DPAD_DOWN, 1f);
            controller.addInput("LEFT_2", 0, SharpKmyIO.INPUTID.kXINPUT_DPAD_LEFT, 1f);
            controller.addInput("RIGHT_2", 0, SharpKmyIO.INPUTID.kXINPUT_DPAD_RIGHT, 1f);
            controller.addInput("PAD_X", 0, SharpKmyIO.INPUTID.kXINPUT_LEFT_THUMB_X, 1f);
            controller.addInput("PAD_Y", 0, SharpKmyIO.INPUTID.kXINPUT_LEFT_THUMB_Y, 1f);

            controller.addInput("DECIDE_0", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'Z', 1f);
            controller.addInput("DECIDE_1", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0x0D, 1f);// Enter
            controller.addInput("DECIDE_2", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0x20, 1f);// Space

            controller.addInput("CANCEL_0", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0x1B, 1f);//Esc
            controller.addInput("CANCEL_1", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'X', 1f);//x
            controller.addInput("CANCEL_2", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0xA3, 1f);//RCtrl

            controller.addInput("MENU_0", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0xA3, 1f);
            controller.addInput("MENU_1", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0xA2, 1f);

            controller.addInput("L", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0x31, 1f);
            controller.addInput("R", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0x32, 1f);

            controller.addInput("CAMERA_VERTICAL_ROT_UP", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'R', 1f);
            controller.addInput("CAMERA_VERTICAL_ROT_DOWN", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'F', 1f);
            controller.addInput("CAMERA_HORIZONTAL_ROT_CLOCKWISE", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'Q', 1f);
            controller.addInput("CAMERA_HORIZONTAL_ROT_COUNTER_CLOCKWISE", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'E', 1f);
            controller.addInput("CAMERA_ZOOM_IN", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'C', 1f);
            controller.addInput("CAMERA_ZOOM_OUT", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'V', 1f);
            controller.addInput("CAMERA_POSITION_RESET", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'K', 1f);
            controller.addInput("CAMERA_CONTROL_MODE_CHANGE", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'B', 1f);

            controller.addInput("DASH_0", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0xA0, 1f);//LShift
            controller.addInput("DASH_1", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 0xA1, 1f);//RShift

            controller.addInput("VR_CALIBRATION", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'J', 1f);	// j
            controller.addInput("VR_SYNC_CAMERA", 0, SharpKmyIO.INPUTID.kKEYBOARD_BEGIN + 'L', 1f);	// l

            if (System.Threading.Thread.CurrentThread.CurrentUICulture.Name.StartsWith("ja"))
            {
                controller.addInput("PAD_DECIDE", 0, SharpKmyIO.INPUTID.kXINPUT_B, 1f);
                controller.addInput("PAD_CANCEL", 0, SharpKmyIO.INPUTID.kXINPUT_A, 1f);
            }
            else
            {
                controller.addInput("PAD_DECIDE", 0, SharpKmyIO.INPUTID.kXINPUT_A, 1f);
                controller.addInput("PAD_CANCEL", 0, SharpKmyIO.INPUTID.kXINPUT_B, 1f);
            }

            controller.addInput("PAD_L", 0, SharpKmyIO.INPUTID.kXINPUT_LEFT_SHOULDER, 1f);
            controller.addInput("PAD_R", 0, SharpKmyIO.INPUTID.kXINPUT_RIGHT_SHOULDER, 1f);

            controller.addInput("PAD_CAMERA_VERTICAL_ROT_UP", 0, SharpKmyIO.INPUTID.kXINPUT_RIGHT_THUMB_Y, 1f);
            controller.addInput("PAD_CAMERA_VERTICAL_ROT_DOWN", 0, SharpKmyIO.INPUTID.kXINPUT_RIGHT_THUMB_Y, 1f);
            controller.addInput("PAD_CAMERA_HORIZONTAL_ROT_CLOCKWISE", 0, SharpKmyIO.INPUTID.kXINPUT_RIGHT_THUMB_X, 1f);
            controller.addInput("PAD_CAMERA_HORIZONTAL_ROT_COUNTER_CLOCKWISE", 0, SharpKmyIO.INPUTID.kXINPUT_RIGHT_THUMB_X, 1f);
            controller.addInput("PAD_CAMERA_ZOOM_IN", 0, SharpKmyIO.INPUTID.kXINPUT_LEFT_SHOULDER, 1f);
            controller.addInput("PAD_CAMERA_ZOOM_OUT", 0, SharpKmyIO.INPUTID.kXINPUT_RIGHT_SHOULDER, 1f);
            controller.addInput("PAD_CAMERA_POSITION_RESET", 0, SharpKmyIO.INPUTID.kXINPUT_RIGHT_THUMB, 1f);
            controller.addInput("PAD_CAMERA_CONTROL_MODE_CHANGE", 0, SharpKmyIO.INPUTID.kXINPUT_Y, 1f);

            controller.addInput("PAD_DASH", 0, SharpKmyIO.INPUTID.kXINPUT_X, 1f);

            controller.addInput("PAD_VR_CALIBRATION", 0, SharpKmyIO.INPUTID.kXINPUT_LEFT_TRIGGER, 1f);
            controller.addInput("PAD_VR_SYNC_CAMERA", 0, SharpKmyIO.INPUTID.kXINPUT_RIGHT_TRIGGER, 1f);
#else
            controller.setGameMain(inGameMain);
            controller.initialize();
#endif
        }

        public static SharpKmyMath.Vector2 getTouchPos(int id)
        {
#if WINDOWS
            return new SharpKmyMath.Vector2();
#else       
            return controller.getTouchPos(id);
#endif
        }

        public static bool isTouchDown(int id)
        {
#if WINDOWS
            return false;
#else       
            return controller.isTouchDown(id);
#endif
        }

        internal float getAxis(string keyName)
        {
#if WINDOWS
            return controller.getValue(keyName);
#else
            return controller.getAxis(keyName);
#endif
        }

        public void finalize()
        {
            controller.Release();
        }

        internal void Draw()
        {
#if WINDOWS
#else
            controller.draw();
#endif
        }

        internal void Update()
        {
            var touchState = Touch.GetState();

#if WINDOWS
#else
            controller.update();
#endif

            //------------------------------------
            // 各キーの最新の状態を反映
            //------------------------------------

            for (int i = 0; i < MAX_KEYSTATE; i++)
            {
                oldBuffer[i] = keyBuffer[i];
            }

            if ((controller.getValue("UP_0") > 0 || controller.getValue("UP_1") > 0 ||
                controller.getValue("UP_2") > 0 || controller.getValue("PAD_Y") > 0.5f) ||
                (vPadEnable && touchState.IsSlide(TouchSlideOrientation.Up)))
            {
                keyBuffer[(int)KeyIndexes.UP] += GameMain.getRelativeParam60FPS();
            }
            else
            {
                keyBuffer[(int)KeyIndexes.UP] = 0;
            }

            if ((controller.getValue("DOWN_0") > 0 || controller.getValue("DOWN_1") > 0 ||
                controller.getValue("DOWN_2") > 0 || controller.getValue("PAD_Y") < -0.5f) ||
                (vPadEnable && touchState.IsSlide(TouchSlideOrientation.Down)))
            {
                keyBuffer[(int)KeyIndexes.DOWN] += GameMain.getRelativeParam60FPS();
            }
            else
            {
                keyBuffer[(int)KeyIndexes.DOWN] = 0;
            }

            if ((controller.getValue("LEFT_0") > 0 || controller.getValue("LEFT_1") > 0 ||
                controller.getValue("LEFT_2") > 0 || controller.getValue("PAD_X") < -0.5f) ||
                (vPadEnable && touchState.IsSlide(TouchSlideOrientation.Left)))
            {
                keyBuffer[(int)KeyIndexes.LEFT] += GameMain.getRelativeParam60FPS();
            }
            else
            {
                keyBuffer[(int)KeyIndexes.LEFT] = 0;
            }

            if ((controller.getValue("RIGHT_0") > 0 || controller.getValue("RIGHT_1") > 0 ||
                controller.getValue("RIGHT_2") > 0 || controller.getValue("PAD_X") > 0.5f) ||
                (vPadEnable && touchState.IsSlide(TouchSlideOrientation.Right)))
            {
                keyBuffer[(int)KeyIndexes.RIGHT] += GameMain.getRelativeParam60FPS();
            }
            else
            {
                keyBuffer[(int)KeyIndexes.RIGHT] = 0;
            }

            if ((controller.getValue("DECIDE_0") > 0 ||
                 controller.getValue("DECIDE_1") > 0 ||
                 controller.getValue("DECIDE_2") > 0 ||
                 controller.getValue("PAD_DECIDE") > 0) ||
                (vPadEnable && touchState.Gesture == GestureType.Tap))
            {
                keyBuffer[(int)KeyIndexes.DECIDE] += GameMain.getRelativeParam60FPS();
            }
            else
            {
                keyBuffer[(int)KeyIndexes.DECIDE] = 0;
            }

            if ((controller.getValue("CANCEL_0") > 0 ||
                 controller.getValue("CANCEL_1") > 0 ||
                 controller.getValue("CANCEL_2") > 0 ||
                 controller.getValue("PAD_CANCEL") > 0) ||
                (vPadEnable && touchState.Gesture == GestureType.Hold))
            {
                keyBuffer[(int)KeyIndexes.CANCEL] += GameMain.getRelativeParam60FPS();
            }
            else
            {
                keyBuffer[(int)KeyIndexes.CANCEL] = 0;
            }

            if ((controller.getValue("MENU_0") > 0 ||
                 controller.getValue("MENU_1") > 0) ||
                (vPadEnable && touchState.Gesture == GestureType.Hold))
            {
                keyBuffer[(int)KeyIndexes.MENU] += GameMain.getRelativeParam60FPS();
            }
            else
            {
                keyBuffer[(int)KeyIndexes.MENU] = 0;
            }

            if ((controller.getValue("L") > 0 || controller.getValue("PAD_L") > 0))
            {
                keyBuffer[(int)KeyIndexes.L] += GameMain.getRelativeParam60FPS();
            }
            else
            {
                keyBuffer[(int)KeyIndexes.L] = 0;
            }

            if ((controller.getValue("R") > 0 || controller.getValue("PAD_R") > 0))
            {
                keyBuffer[(int)KeyIndexes.R] += GameMain.getRelativeParam60FPS();
            }
            else
            {
                keyBuffer[(int)KeyIndexes.R] = 0;
            }

            if ((controller.getValue("CAMERA_VERTICAL_ROT_UP") > 0 || controller.getValue("PAD_CAMERA_VERTICAL_ROT_UP") > 0.5f))
            {
                keyBuffer[(int)KeyIndexes.CAMERA_VERTICAL_ROT_UP] += GameMain.getRelativeParam60FPS();
            }
            else
            {
                keyBuffer[(int)KeyIndexes.CAMERA_VERTICAL_ROT_UP] = 0;
            }

            if ((controller.getValue("CAMERA_VERTICAL_ROT_DOWN") > 0 || controller.getValue("PAD_CAMERA_VERTICAL_ROT_DOWN") < -0.5f))
            {
                keyBuffer[(int)KeyIndexes.CAMERA_VERTICAL_ROT_DOWN] += GameMain.getRelativeParam60FPS();
            }
            else
            {
                keyBuffer[(int)KeyIndexes.CAMERA_VERTICAL_ROT_DOWN] = 0;
            }

            if ((controller.getValue("CAMERA_HORIZONTAL_ROT_CLOCKWISE") > 0 || controller.getValue("PAD_CAMERA_HORIZONTAL_ROT_CLOCKWISE") < -0.5f))
            {
                keyBuffer[(int)KeyIndexes.CAMERA_HORIZONTAL_ROT_CLOCKWISE] += GameMain.getRelativeParam60FPS();
            }
            else
            {
                keyBuffer[(int)KeyIndexes.CAMERA_HORIZONTAL_ROT_CLOCKWISE] = 0;
            }

            if ((controller.getValue("CAMERA_HORIZONTAL_ROT_COUNTER_CLOCKWISE") > 0 || controller.getValue("PAD_CAMERA_HORIZONTAL_ROT_COUNTER_CLOCKWISE") > 0.5f))
            {
                keyBuffer[(int)KeyIndexes.CAMERA_HORIZONTAL_ROT_COUNTER_CLOCKWISE] += GameMain.getRelativeParam60FPS();
            }
            else
            {
                keyBuffer[(int)KeyIndexes.CAMERA_HORIZONTAL_ROT_COUNTER_CLOCKWISE] = 0;
            }

            if ((controller.getValue("CAMERA_ZOOM_IN") > 0 || controller.getValue("PAD_CAMERA_ZOOM_IN") > 0))
            {
                keyBuffer[(int)KeyIndexes.CAMERA_ZOOM_IN] += GameMain.getRelativeParam60FPS();
            }
            else
            {
                keyBuffer[(int)KeyIndexes.CAMERA_ZOOM_IN] = 0;
            }

            if ((controller.getValue("CAMERA_ZOOM_OUT") > 0 || controller.getValue("PAD_CAMERA_ZOOM_OUT") > 0))
            {
                keyBuffer[(int)KeyIndexes.CAMERA_ZOOM_OUT] += GameMain.getRelativeParam60FPS();
            }
            else
            {
                keyBuffer[(int)KeyIndexes.CAMERA_ZOOM_OUT] = 0;
            }

            if ((controller.getValue("CAMERA_POSITION_RESET") > 0 || controller.getValue("PAD_CAMERA_POSITION_RESET") > 0))
            {
                keyBuffer[(int)KeyIndexes.CAMERA_POSITION_RESET] += GameMain.getRelativeParam60FPS();
            }
            else
            {
                keyBuffer[(int)KeyIndexes.CAMERA_POSITION_RESET] = 0;
            }

            if ((controller.getValue("CAMERA_CONTROL_MODE_CHANGE") > 0 || controller.getValue("PAD_CAMERA_CONTROL_MODE_CHANGE") > 0))
            {
                keyBuffer[(int)KeyIndexes.CAMERA_CONTROL_MODE_CHANGE] += GameMain.getRelativeParam60FPS();
            }
            else
            {
                keyBuffer[(int)KeyIndexes.CAMERA_CONTROL_MODE_CHANGE] = 0;
            }

            if ((controller.getValue("DASH_0") > 0 || controller.getValue("DASH_1") > 0 || controller.getValue("PAD_DASH") > 0))
            {
                keyBuffer[(int)KeyIndexes.DASH] += GameMain.getRelativeParam60FPS();
            }
            else
            {
                keyBuffer[(int)KeyIndexes.DASH] = 0;
            }

            if ((controller.getValue("VR_CALIBRATION") > 0 || controller.getValue("PAD_VR_CALIBRATION") > 0))
            {
                keyBuffer[(int)KeyIndexes.VR_CALIBRATION] += GameMain.getRelativeParam60FPS();
            }
            else
            {
                keyBuffer[(int)KeyIndexes.VR_CALIBRATION] = 0;
            }

            if ((controller.getValue("VR_SYNC_CAMERA") > 0 || controller.getValue("PAD_VR_SYNC_CAMERA") > 0))
            {
                keyBuffer[(int)KeyIndexes.VR_SYNC_CAMERA] += GameMain.getRelativeParam60FPS();
            }
            else
            {
                keyBuffer[(int)KeyIndexes.VR_SYNC_CAMERA] = 0;
            }

            //------------------------------------
            // キー情報バッファの値を噛み砕いて扱いやすくする
            //------------------------------------
            triggerState2 = KeyStates.NONE;
            nowState = KeyStates.NONE;
            triggerState = KeyStates.NONE;
            repeatState = KeyStates.NONE;
            for (int i = 0; i < MAX_KEYSTATE; i++)
            {
                var state = (KeyStates)(1 << (i - 1));
                if (keyBuffer[i] == 0 && oldBuffer[i] > 0)
                    triggerState2 |= state;
                if (keyBuffer[i] > 0)
                    nowState |= state;
                if (keyBuffer[i] > 0 && oldBuffer[i] == 0)
                    triggerState |= state;
                if (
                    ((triggerState & state) == state) ||
                    keyBuffer[i] > REPEAT_TIME &&
                    ((keyBuffer[i] % REPEAT_INTERVAL < REPEAT_INTERVAL / 2 && oldBuffer[i] % REPEAT_INTERVAL > REPEAT_INTERVAL / 2) ||
                    GameMain.getRelativeParam60FPS() > REPEAT_INTERVAL / 2)
                    )
                    repeatState |= state;
            }
        }

        internal void ChangeKeyStates(KeyStates keyStates)
        {
            nowState = keyStates;
            triggerState = keyStates;
            triggerState2 = keyStates;
            repeatState = keyStates;
        }
    }
}

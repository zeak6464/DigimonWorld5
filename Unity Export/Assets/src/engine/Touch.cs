//#define DIRECTINPUT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if DIRECTINPUT
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
#endif

namespace Yukar.Engine
{
	public struct myVector2
	{
		public float X, Y;
	}

    [Flags]
    public enum TouchSlideOrientation
    {
        None = 0,

        Left  = 1,
        Right = 2,
        Up    = 4,
        Down  = 8,
    }

	public enum GestureType
	{
		None,
		Tap,
		Hold,
	}

    public struct TouchState
    {
        public bool IsSlide(TouchSlideOrientation orientation)
        {
            return Common.Util.HasFlag(SlideOrientation, orientation);
        }

        public int TouchFrameCount { get; internal set; }

        public bool IsDecideGesture { get; internal set; }

        public TouchSlideOrientation SlideOrientation { get; internal set; }
        public GestureType Gesture;
        //public GestureSample GestureSample { get; internal set; }

		public myVector2 TouchBeginPosition;
		public myVector2 TouchCurrentPosition;
    }

    class Touch
    {
        private static TouchCore sInstance;

        internal static void Initialize(GameMain inGameMain)
        {
            sInstance = new TouchCore(inGameMain);
        }

		internal static void finalize()
		{
			sInstance.Reset();
		}

        internal static void Update(/*GameWindow window*/)
        {
            sInstance.Update(/*window*/);
        }

        public static TouchState GetState()
        {
            return sInstance.touchState;
        }
    }

    class TouchCore
    {
        private int holdGestureCount;
        //private TouchLocation currentTouchLocation;
		//Vector2 touchlocation;

        // 指がスライドしたと認識するのに必要なピクセル数
        // タッチし始めた位置を基準に現在タッチしている位置との距離で判定する
        private const int EnableTouchPixel = 32;

        // 長押しジェスチャーと認識するフレーム数
        private const int GestureHoldTriggerCount = 60;

        internal TouchState touchState;

		SharpKmyIO.Controller controller;

		public TouchCore(GameMain inGameMain)
		{
			controller = new SharpKmyIO.Controller();
#if WINDOWS
            // ウィンドウサイズの変更に対応しているのはツールで使う用なので、そちらを使う
            controller.addInput("TOUCH", 0, SharpKmyIO.INPUTID.kWIN_MOUSE_BUTTON_TOOL_L, 1);
			controller.addInput("TOUCHPOS_X", 0, SharpKmyIO.INPUTID.kWIN_MOUSE_TOOL_POS_X, 1);
            controller.addInput("TOUCHPOS_Y", 0, SharpKmyIO.INPUTID.kWIN_MOUSE_TOOL_POS_Y, 1);
            //controller.addInput("TOUCH", 0, SharpKmyIO.INPUTID.kWIN_MOUSE_BUTTON_L, 1);
            //controller.addInput("TOUCHPOS_X", 0, SharpKmyIO.INPUTID.kWIN_MOUSE_POS_X, 1);
            //controller.addInput("TOUCHPOS_Y", 0, SharpKmyIO.INPUTID.kWIN_MOUSE_POS_Y, 1);
#else
            controller.setGameMain(inGameMain);
            controller.initialize();
#endif
        }

		public void Reset()
		{
			controller.Release();
		}

        internal void Update(/*GameWindow window*/)
        {
            touchState.Gesture = GestureType.None;
            int windowWidth = 640;
			int windowHeight = 480;

            // マウスでの操作
#if DIRECTINPUT
            var mouseState = Mouse.GetState(window);
            Vector2 mousePos = mouseState.Position.ToVector2();
#else
			myVector2 mousePos = new myVector2();
            mousePos.X = controller.getValue("TOUCHPOS_X") * windowWidth;
            mousePos.Y = controller.getValue("TOUCHPOS_Y") * windowHeight;
#endif

            // マウスカーソルの位置がウィンドウの領域外だったら処理を進めない
            if (mousePos.X < 0 || mousePos.X > windowWidth || mousePos.Y < 0 || mousePos.Y > windowHeight)
            {
                touchState.TouchFrameCount = 0;
                touchState.SlideOrientation = TouchSlideOrientation.None;
                touchState.Gesture = GestureType.None;
                touchState.IsDecideGesture = false;
                return;
            }

            // マウスの左クリックをタッチパネル環境でのタップと同等のものとして扱う
#if DIRECTINPUT
            if (mouseState.LeftButton == ButtonState.Pressed)
#else
			if(controller.getValue("TOUCH") > 0)
#endif
            {
                touchState.TouchFrameCount++;
            }
            else
            {
                // いずれかの方向にスライドしている時はタップとして扱わない
                if(touchState.TouchFrameCount > 0 && touchState.SlideOrientation == TouchSlideOrientation.None)
                {
                    DecideGestureType(GestureType.Tap);
                }

                touchState.IsDecideGesture = false;
                touchState.TouchFrameCount = 0;
            }

            // タッチ時の位置を記憶しておく
            if (touchState.TouchFrameCount == 1)
            {
                touchState.TouchBeginPosition = mousePos;
                touchState.TouchCurrentPosition = mousePos;
            }
            else if (touchState.TouchFrameCount > 1)
            {
                touchState.TouchCurrentPosition = mousePos;
            }
/*s
            // タッチパネルでの操作
#if DIRECTINPUT
            var touches = TouchPanel.GetState();

            // タッチ位置
            if (touches.Count > 0)
#else
			if(controller.isHold("TOUCH"))
#endif
            {
#if DIRECTINPUT
                currentTouchLocation = touches[0];
#else
				currentTouchLocation..X mousePos.X;
#endif
            }

            if (touches.Count > 0 && currentTouchLocation.State != TouchLocationState.Invalid)
            {
                Vector2 pos = currentTouchLocation.Position;

                switch (touches[0].State)
                {
                    case TouchLocationState.Pressed:
                        touchState.TouchBeginPosition = pos;
                        touchState.TouchCurrentPosition = pos;
                        break;

                    case TouchLocationState.Moved:
                        touchState.TouchCurrentPosition = pos;
                        break;

                    case TouchLocationState.Released:
                        touchState.TouchBeginPosition = Vector2.Zero;
                        touchState.TouchCurrentPosition = Vector2.Zero;
                        break;
                }
            }

            // タッチ位置が移動した方向
            touchState.SlideOrientation = TouchSlideOrientation.None;
            Vector2 distance = touchState.TouchCurrentPosition - touchState.TouchBeginPosition;

            if (touchState.TouchFrameCount > 1)
            {
                // 縦と横で距離が長い方を優先する
                if (Math.Abs(distance.X) > Math.Abs(distance.Y))
                {
                    if (distance.X < -EnableTouchPixel)
                    {
                        touchState.SlideOrientation |= TouchSlideOrientation.Left;
                    }

                    if (distance.X > +EnableTouchPixel)
                    {
                        touchState.SlideOrientation |= TouchSlideOrientation.Right;
                    }
                }
                else
                {
                    if (distance.Y < -EnableTouchPixel)
                    {
                        touchState.SlideOrientation |= TouchSlideOrientation.Up;
                    }

                    if (distance.Y > +EnableTouchPixel)
                    {
                        touchState.SlideOrientation |= TouchSlideOrientation.Down;
                    }
                }
            }

            // 実行したジェスチャーが既にある場合はマウスボタン(もしくは指)が離れるまで新たなジェスチャーを設定しない
            if (touchState.IsDecideGesture)
            {
                touchState.SlideOrientation = TouchSlideOrientation.None;
                return;
            }
*/
			myVector2 distance = new myVector2();
			distance.X = touchState.TouchCurrentPosition.X - touchState.TouchBeginPosition.X;
			distance.Y = touchState.TouchCurrentPosition.Y - touchState.TouchBeginPosition.Y;
			float length = (float)Math.Sqrt(distance.X * distance.X + distance.Y * distance.Y);

            // 長押しジェスチャーの判定
            // スライド中は長押しと判定しないようにする
            if (touchState.TouchFrameCount > 0 && length < EnableTouchPixel)
            {
                holdGestureCount++;

                if (holdGestureCount >= GestureHoldTriggerCount)
                {
                    DecideGestureType(GestureType.Hold);
                }
            }
            else
            {
                holdGestureCount = 0;
            }
					 /*
            // タッチジェスチャー
            if (TouchPanel.IsGestureAvailable)
            {
                // ジェスチャー機能が有効になっていない場合は例外が発生するので事前にフラグを調べる
                var gesture = TouchPanel.ReadGesture();

                touchState.Gesture = gesture.GestureType;
            }
					  */
        }

        private void DecideGestureType(GestureType gestureType)
        {
            if (touchState.IsDecideGesture) return;

            touchState.IsDecideGesture = true;
            touchState.Gesture = gestureType;
        }
    }
}

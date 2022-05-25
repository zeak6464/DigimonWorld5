using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Yukar.Engine
{
    class ToastWindow
    {
        public MapScene parent;
        private WindowDrawer window;
        private TextDrawer textDrawer;

        private Vector2 windowPos = new Vector2();
        private Vector2 windowSize = new Vector2();
        private Vector2 maxWindowSize = new Vector2();
        private Vector2 textOffset = new Vector2();

        internal enum WindowState
        {
            OPENING_WINDOW,
            SHOW_WINDOW,
            CLOSING_WINDOW,
            HIDE_WINDOW,
        }
        private WindowState windowState = WindowState.HIDE_WINDOW;
        float frame = 0;
        private string text;

        private const int DEFAULT_VIEWTIME = 60;
        private int viewTime;

        internal void Initialize(Common.Resource.Window winRes, int winImgId)
        {
            // ウィンドウの読み込み
            window = new WindowDrawer(winRes, winImgId);
            textDrawer = new TextDrawer(1);
            windowPos.X = Graphics.ViewportWidth / 2;
            windowPos.Y = Graphics.ViewportHeight / 2;
            if (winRes != null)
            {
                textOffset.X = winRes.left;
                textOffset.Y = winRes.top;
            }
        }

        internal void Update()
        {
            switch (windowState)
            {
                case WindowState.OPENING_WINDOW:
                    if (frame >= MapScene.WINDOW_SHOW_FRAME)
                    {
                        windowState = WindowState.SHOW_WINDOW;
                        frame = 0;
                        windowSize.X = maxWindowSize.X;
                        windowSize.Y = maxWindowSize.Y;
                    }
                    else if(Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE) ||
                        Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CANCEL))
                    {
                        frame = MapScene.WINDOW_SHOW_FRAME - frame;
                        windowState = WindowState.CLOSING_WINDOW;
                    }
                    else
                    {
                        float delta = (float)frame / MapScene.WINDOW_SHOW_FRAME;
                        delta = 1 - (1 - delta) * (1 - delta);
                        windowSize.X = (int)(maxWindowSize.X * delta);
                        windowSize.Y = (int)(maxWindowSize.Y * delta);
                    }
                    break;
                case WindowState.SHOW_WINDOW:
                    if (frame > viewTime ||
                        Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE) ||
                        Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CANCEL))
                    {
                        windowState = WindowState.CLOSING_WINDOW;
                        frame = 0;
                    }
                    break;
                case WindowState.CLOSING_WINDOW:
                    if (frame >= MapScene.WINDOW_SHOW_FRAME)
                    {
                        windowState = WindowState.HIDE_WINDOW;
                        frame = 0;
                    }
                    else
                    {
                        float delta = 1 - (float)frame / MapScene.WINDOW_SHOW_FRAME;
                        delta = 1 - (1 - delta) * (1 - delta);
                        windowSize.X = (int)(maxWindowSize.X * delta);
                        windowSize.Y = (int)(maxWindowSize.Y * delta);
                    }
                    break;
            }
            frame += GameMain.getRelativeParam60FPS();
        }

        internal void Draw()
        {
            if (windowState != WindowState.HIDE_WINDOW)
            {
                var pos = windowPos - windowSize / 2;
                window.Draw(pos, windowSize);

                if (windowState == WindowState.SHOW_WINDOW)
                {
                    textDrawer.DrawString(text, pos + textOffset, Color.White);
                }
            }
        }

        internal void show(string str)
        {
            if (Common.Util.stringIsNullOrWhiteSpace(str))
                return;

            text = str;
            maxWindowSize = textDrawer.MeasureString(str);
            maxWindowSize.X += window.paddingLeft + window.paddingRight;
            maxWindowSize.Y += window.paddingTop + window.paddingBottom;
            windowState = WindowState.OPENING_WINDOW;
            frame = 0;
            viewTime = DEFAULT_VIEWTIME;
        }

        internal bool isVisible()
        {
            return windowState != WindowState.HIDE_WINDOW;
        }
    }
}

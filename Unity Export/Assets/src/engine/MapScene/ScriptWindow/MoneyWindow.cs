using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Yukar.Engine
{
    class MoneyWindow
    {
        public MapScene parent;
        private WindowDrawer window;
        private TextDrawer textDrawer;

        internal const int WINDOW_WIDTH = 200;
        internal const int WINDOW_HEIGHT = 32;
        internal const int TEXT_OFFSET = 8;

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

        internal void Initialize(Common.Resource.Window winRes, int winImgId)
        {
            // ウィンドウの読み込み
            window = new WindowDrawer(winRes, winImgId);
            textDrawer = new TextDrawer(1);
            maxWindowSize.X = WINDOW_WIDTH;
            maxWindowSize.Y = WINDOW_HEIGHT;
            textOffset.X = TEXT_OFFSET;
            textOffset.Y = TEXT_OFFSET;
            if (window.WindowResource != null)
            {
                maxWindowSize.X += window.paddingLeft + window.paddingRight;
                maxWindowSize.Y += window.paddingTop + window.paddingBottom;
                textOffset.X = window.paddingLeft;
                textOffset.Y = window.paddingTop;
            }
            windowPos.X = Graphics.ViewportWidth - maxWindowSize.X / 2 - 12;
            windowPos.Y = maxWindowSize.Y / 2 + 2;
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
                    else
                    {
                        float delta = (float)frame / MapScene.WINDOW_SHOW_FRAME;
                        delta = 1 - (1 - delta) * (1 - delta);
                        windowSize.X = (int)(maxWindowSize.X * delta);
                        windowSize.Y = (int)(maxWindowSize.Y * delta);
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
                var areaSize = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);

                if (windowState == WindowState.SHOW_WINDOW)
                {
                    // 所持金を描画する
                    textDrawer.DrawString(parent.owner.data.party.GetMoney() + " " + parent.menuWindow.res.gs.glossary.moneyName,
                        pos + textOffset, areaSize, TextDrawer.HorizontalAlignment.Right, TextDrawer.VerticalAlignment.Center, Color.White);
                }
            }
        }

        internal void show()
        {
            windowState = WindowState.OPENING_WINDOW;
            frame = 0;
        }

        internal void hide()
        {
            windowState = WindowState.CLOSING_WINDOW;
            frame = 0;
        }
    }
}

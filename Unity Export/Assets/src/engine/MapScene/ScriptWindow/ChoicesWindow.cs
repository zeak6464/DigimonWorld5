using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using KeyStates = Yukar.Engine.Input.KeyStates;
using StateType = Yukar.Engine.Input.StateType;

namespace Yukar.Engine
{
    class ChoicesWindow
    {
        internal const int SELECTION_POS_CENTER = 4;
        internal const int CHOICES_SELECTING = -1;
        internal const int WINDOW_WIDTH = 360;
        internal const int WINDOW_HEIGHT = 160;
        internal const int TEXT_OFFSET = 8;
        internal const int LINE_HEIGHT = 48;
        internal const int WINDOW_MARGIN = 32; // 選択肢ウィンドウを画面端から離す距離
        public MapScene parent;
        private WindowDrawer window;
        private WindowDrawer selBox;
        private TextDrawer textDrawer;

        private Vector2 windowPos = new Vector2();
        private Vector2 windowSize = new Vector2();
        private Vector2 maxWindowSize = new Vector2();
        private Vector2 textOffset = new Vector2();
        private Vector2 textSize = new Vector2();
        private Vector2 boxOffset = new Vector2();
        private Func<bool> messageSequencer;
        internal Blinker blinker = new Blinker(Color.White, new Color(0.5f, 0.5f, 0.5f, 0.5f), 30);

        private int nowSelected = 0;
        private int result = CHOICES_SELECTING;
        private bool forceProgress = false;

        private string[] choices;

        internal enum WindowState
        {
            SHOW_WINDOW,
            SHOW_CHOICES,
            HIDE_CHOICES,
            HIDE_WINDOW,
        }
        private WindowState windowState = WindowState.HIDE_WINDOW;
        public WindowState getWindowState() { return this.windowState; }
        public static int nextPosition = SELECTION_POS_CENTER;
        public static bool topMarginForMoneyWindow = false;

        internal void Initialize(Common.Resource.Window winRes, int winImgId, Common.Resource.Window selRes, int selImgId)
        {
            // ウィンドウの読み込み
            window = new WindowDrawer(winRes, winImgId);
            selBox = new WindowDrawer(selRes, selImgId);
            textDrawer = new TextDrawer(1);
            maxWindowSize.X = WINDOW_WIDTH;
            maxWindowSize.Y = WINDOW_HEIGHT;
            windowSize.X = WINDOW_WIDTH;
            windowSize.Y = WINDOW_HEIGHT;
            boxOffset.X = boxOffset.Y = TEXT_OFFSET;
        }

        internal void Show(string[] strs)
        {
            nowSelected = 0;
            result = CHOICES_SELECTING;
            choices = strs;
            int phase = 0;
            float frame = 0;

            textSize.X = 0;
            textSize.Y = 0;

            foreach (var str in strs)
            {
                var size = textDrawer.MeasureString(str);
                textSize.X = (int)Math.Max(textSize.X, size.X);
                textSize.Y = (int)Math.Max(textSize.Y, size.Y);
            }

            textOffset.X = window.paddingLeft + TEXT_OFFSET;
            textOffset.Y = window.paddingTop + TEXT_OFFSET;
            maxWindowSize.X = textSize.X + textOffset.X * 2;
            maxWindowSize.Y = window.paddingTop + window.paddingBottom + LINE_HEIGHT * strs.Length;// (textSize.Y + TEXT_OFFSET) * strs.Length - TEXT_OFFSET + textOffset.Y * 2;

            var selectionPos = new Point();
            // X座標の確定
            switch (nextPosition)
            {
                case 0: // 左上
                case 3: // 左
                case 6: // 左下
                    selectionPos.X = (int)(maxWindowSize.X / 2) + WINDOW_MARGIN;
                    break;
                case 1: // 上
                case 4: // 中央
                case 7: // 下
                    selectionPos.X = Graphics.ViewportWidth / 2;
                    break;
                case 2: // 右上
                case 5: // 右
                case 8: // 右下
                    selectionPos.X = Graphics.ViewportWidth - (int)(maxWindowSize.X / 2) - WINDOW_MARGIN;
                    break;
            }

            // Y座標の確定
            switch (nextPosition)
            {
                case 0: // 左上
                case 1: // 上
                    selectionPos.Y = (int)(maxWindowSize.Y / 2) + WINDOW_MARGIN;
                    break;
                case 2: // 右上
                    selectionPos.Y = (int)(maxWindowSize.Y / 2) + WINDOW_MARGIN + (topMarginForMoneyWindow ? WINDOW_MARGIN : 0);
                    break;

                case 3: // 左
                case 4: // 中央
                case 5: // 右
                    selectionPos.Y = Graphics.ViewportHeight / 2;
                    break;

                case 6: // 左下
                case 7: // 下
                case 8: // 右下
                    selectionPos.Y = Graphics.ViewportHeight - (int)(maxWindowSize.Y / 2) - WINDOW_MARGIN;
                    break;
            }

            SetWindowPos(selectionPos.X, selectionPos.Y);

            messageSequencer = () =>
            {
                switch (phase)
                {
                    case 0: // ウィンドウを出す
                        if (frame >= MapScene.WINDOW_SHOW_FRAME)
                        {
                            phase++;
                            frame = 0;
                            parent.LockControl();
                            SetWindowVisible(WindowState.SHOW_CHOICES);
                            SetWindowSize((int)(maxWindowSize.X), (int)(maxWindowSize.Y));
                        }
                        else
                        {
                            SetWindowVisible(WindowState.SHOW_WINDOW);
                            float delta = (float)frame / MapScene.WINDOW_SHOW_FRAME;
                            delta = 1 - (1 - delta) * (1 - delta);
                            SetWindowSize((int)(maxWindowSize.X * delta), (int)(maxWindowSize.Y * delta));
                        }
                        break;
                    case 1: // 入力待ち
#if WINDOWS
#else
                        // タッチ選択
                        if (UnityEngine.Input.GetMouseButtonDown(0) && !GameMain.IsPushedAndroidBackButton())
                        {
                            var touchPos = InputCore.getTouchPos(0);
                            var x0 = selectionPos.X - maxWindowSize.X / 2 + window.paddingLeft;
                            var x1 = selectionPos.X + maxWindowSize.X / 2 - window.paddingRight;
                            for (int i = 0; i < choices.Length; i++)
                            {
                                var y0 = selectionPos.Y - maxWindowSize.Y / 2 + window.paddingTop + LINE_HEIGHT * i;
                                var y1 = y0 + LINE_HEIGHT;
                                if (x0 < touchPos.x && touchPos.x < x1 && y0 < touchPos.y && touchPos.y < y1)
                                {
                                    if (nowSelected == i)
                                    {
                                        forceProgress = false;
                                        phase++;
                                        frame = 0;
                                        SetWindowVisible(WindowState.HIDE_CHOICES);
                                    }
                                    else
                                    {
                                        nowSelected = i;
                                        Audio.PlaySound(parent.owner.se.select);
                                    }
                                }
                            }
                        }
#endif
                        if (Input.KeyTest(StateType.REPEAT, KeyStates.UP))
                        {
                            Audio.PlaySound(parent.owner.se.select);
                            frame = 0;
                            nowSelected--;
                            if (nowSelected < 0)
                                nowSelected = choices.Length - 1;
                        }
                        else if (Input.KeyTest(StateType.REPEAT, KeyStates.DOWN))
                        {
                            Audio.PlaySound(parent.owner.se.select);
                            frame = 0;
                            nowSelected++;
                            if (nowSelected >= choices.Length)
                                nowSelected = 0;
                        }
                        else if (Input.KeyTest(StateType.TRIGGER, KeyStates.DECIDE) || forceProgress)
                        {
                            forceProgress = false;
                            phase++;
                            frame = 0;
                            SetWindowVisible(WindowState.HIDE_CHOICES);
                        }
                        break;
                    case 2: // 閉じる
                        if (frame >= MapScene.WINDOW_CLOSE_FRAME)
                        {
                            phase++;
                            frame = 0;
                            SetWindowVisible(WindowState.HIDE_WINDOW);
                            parent.UnlockControl();
                            result = nowSelected;
                        }
                        else
                        {
                            float delta = 1 - (float)frame / MapScene.WINDOW_CLOSE_FRAME;
                            delta = 1 - (1 - delta) * (1 - delta);
                            SetWindowSize((int)(maxWindowSize.X * delta), (int)(maxWindowSize.Y * delta));
                        }
                        break;
                    case 3: // おわり
                        parent.CloseMessageWindow();
                        forceProgress = false;
                        return false;
                }
                frame += GameMain.getRelativeParam60FPS();
                return true;
            };
        }

        internal int GetResult()
        {
            return result;
        }

        internal void Update()
        {
            if (messageSequencer != null)
            {
                if (!messageSequencer())
                {
                    messageSequencer = null;
                }
            }
        }

        internal void Draw()
        {
            blinker.update();

            if (isVisible())
            {
                var pos = windowPos - windowSize / 2;
                window.Draw(pos, windowSize);

                // 表示しきっている状態でのみ文字と選択枠を出す
                if (windowState == WindowState.SHOW_CHOICES)
                {
                    textOffset.X = window.paddingLeft + TEXT_OFFSET;
                    textOffset.Y = window.paddingTop + TEXT_OFFSET;

                    boxOffset.Y = (LINE_HEIGHT - textSize.Y - 4) / 2;

                    for (int i = 0; i < choices.Length; i++)
                    {
                        parent.menuWindow.res.unselBox.Draw(pos + textOffset - boxOffset, textSize + boxOffset * 2);

                        if (i == nowSelected)
                            selBox.Draw(pos + textOffset - boxOffset, textSize + boxOffset * 2, blinker.getColor());

                        textDrawer.DrawString(choices[i], pos + textOffset, Color.White);
                        textOffset.Y += textSize.Y + boxOffset.Y * 2 + 2;
                    }
                }
            }
        }

        public bool isVisible()
        {
            return windowState != WindowState.HIDE_WINDOW;
        }

        internal void SetWindowPos(int x, int y)
        {
            windowPos.X = x;
            windowPos.Y = y;
        }

        internal void SetWindowSize(int x, int y)
        {
            windowSize.X = x;
            windowSize.Y = y;
        }

        internal void SetWindowVisible(WindowState p)
        {
            windowState = p;
        }

        internal bool IsActive()
        {
            return windowState != WindowState.HIDE_WINDOW;
        }

        internal void Close()
        {
            if (windowState != WindowState.HIDE_WINDOW)
                forceProgress = true;
        }
    }
}

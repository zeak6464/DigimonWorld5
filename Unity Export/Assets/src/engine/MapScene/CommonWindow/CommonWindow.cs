using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Yukar.Common.GameData;

namespace Yukar.Engine
{
    abstract class CommonWindow
    {
        internal const int LINE_HEIGHT = 48;
        internal const int TEXT_HEIGHT = 32;
        internal const int TEXT_OFFSET = 16;
        private const int ANIM_FRAME = 8;

        internal Vector2 windowPos;
        private Vector2 windowSize;
        private Vector2 startWindowPos;     // リサイズ時のみ使用
        private Vector2 startWindowSize;    // リサイズ時のみ使用
        internal Vector2 targetWindowPos;   // リサイズ時のみ使用
        internal Vector2 maxWindowSize;
        internal bool locked;
        internal float frame;
        private int maxFrame = ANIM_FRAME;

        internal abstract void DrawCallback();
        internal abstract void UpdateCallback();

        internal int innerWidth;
        internal int innerHeight;
        internal bool upScrollVisible;
        internal bool downScrollVisible;

        internal class ImageInstance
        {
            internal int imgId = -1;
            internal Common.Resource.ResourceItem image;

            internal void Reset()
            {
            }

            internal void ChangeGraphic(Common.Resource.ResourceItem grp, int imgId)
            {
                image = grp;
                this.imgId = imgId;
            }
        }

        internal class ParamSet
        {
            internal WindowDrawer window;
            internal WindowDrawer selBox;
            internal WindowDrawer unselBox;
            internal TextDrawer textDrawer;
            internal MapCharacter upScrollChr;
            internal MapCharacter downScrollChr;
            internal ImageInstance[] partyChars;
            internal MapCharacter detailChr;
            internal Common.Rom.GameSettings gs;
            public MenuController owner;
            internal int pageImgId;

            public ParamSet clone()
            {
                var clone = new ParamSet();
                clone.window = window;
                clone.selBox = selBox;
                clone.unselBox = unselBox;
                clone.textDrawer = textDrawer;
                clone.upScrollChr = upScrollChr;
                clone.downScrollChr = downScrollChr;
                clone.partyChars = partyChars;
                clone.detailChr = detailChr;
                clone.gs = gs;
                clone.owner = owner;
                return clone;
            }
        }
        internal ParamSet p;

        internal enum WindowState
        {
            OPENING_WINDOW,
            SHOW_WINDOW,
            CLOSING_WINDOW,
            HIDE_WINDOW,
            RESIZING_WINDOW,
        }
        internal WindowState windowState = WindowState.HIDE_WINDOW;

        public virtual void Initialize(ParamSet pset, int x, int y, int maxWidth, int maxHeight)
        {
            this.p = pset;
            windowPos.X = x;
            windowPos.Y = y;
            maxWindowSize.X = maxWidth;
            maxWindowSize.Y = maxHeight;
            innerWidth = (int)maxWindowSize.X - p.window.paddingLeft - p.window.paddingRight;
            innerHeight = (int)maxWindowSize.Y - p.window.paddingTop - p.window.paddingBottom;
        }

        internal void Update()
        {
            if (windowState == WindowState.HIDE_WINDOW)
                return;

            switch (windowState)
            {
                case WindowState.OPENING_WINDOW:
                    if (frame >= maxFrame)
                    {
                        windowState = WindowState.SHOW_WINDOW;
                        frame = 0;
                        windowSize.X = maxWindowSize.X;
                        windowSize.Y = maxWindowSize.Y;
                    }
                    else
                    {
                        float delta = (float)frame / maxFrame;
                        delta = 1 - (1 - delta) * (1 - delta) * (1 - delta);
                        windowSize.X = maxWindowSize.X * delta;
                        windowSize.Y = maxWindowSize.Y * delta;
                    }
                    break;
                case WindowState.CLOSING_WINDOW:
                    if (frame >= maxFrame)
                    {
                        windowState = WindowState.HIDE_WINDOW;
                        frame = 0;
                    }
                    else
                    {
                        float delta = 1 - (float)frame / maxFrame;
                        delta = 1 - (1 - delta) * (1 - delta) * (1 - delta);
                        windowSize.X = maxWindowSize.X * delta;
                        windowSize.Y = maxWindowSize.Y * delta;
                    }
                    break;
                case WindowState.RESIZING_WINDOW:
                    if (frame >= maxFrame)
                    {
                        windowState = WindowState.SHOW_WINDOW;
                        frame = 0;
                        windowSize.X = maxWindowSize.X;
                        windowSize.Y = maxWindowSize.Y;
                        windowPos.X = targetWindowPos.X;
                        windowPos.Y = targetWindowPos.Y;
                    }
                    else
                    {
                        float delta = (float)frame / maxFrame;
                        delta = 1 - (1 - delta) * (1 - delta) * (1 - delta);
                        float invDelta = 1 - delta;
                        windowSize.X = maxWindowSize.X * delta + startWindowSize.X * invDelta;
                        windowSize.Y = maxWindowSize.Y * delta + startWindowSize.Y * invDelta;
                        windowPos.X = targetWindowPos.X * delta + startWindowPos.X * invDelta;
                        windowPos.Y = targetWindowPos.Y * delta + startWindowPos.Y * invDelta;
                    }
                    break;
            }
            frame += GameMain.getRelativeParam60FPS();

            if (windowState != WindowState.SHOW_WINDOW || locked)
                return;

            UpdateCallback();
        }

        internal virtual void Show()
        {
            if (windowState == WindowState.SHOW_WINDOW || windowState == WindowState.RESIZING_WINDOW)
                return;

            windowState = WindowState.OPENING_WINDOW;
            frame = 0;
        }

        internal virtual void Hide()
        {
            if (windowState == WindowState.HIDE_WINDOW)
                return;

            windowState = WindowState.CLOSING_WINDOW;
            frame = 0;
        }

        internal virtual void Resize()
        {
            startWindowSize = windowSize;
            startWindowPos = windowPos;
            targetWindowPos = windowPos;
            windowState = WindowState.RESIZING_WINDOW;
            frame = 0;
        }

        internal virtual void Lock()
        {
            locked = true;
        }

        internal virtual void Unlock()
        {
            locked = false;
        }

        internal void Draw()
        {
            if (windowState == WindowState.HIDE_WINDOW)
                return;

            var pos = windowPos - windowSize / 2 + GameMain.halfOffset;
            p.window.Draw(pos, windowSize);

            if (windowState != WindowState.SHOW_WINDOW && windowState != WindowState.RESIZING_WINDOW)
                return;

            int left = (int)((maxWindowSize.X - innerWidth) / 2 + pos.X);
            int top = (int)((maxWindowSize.Y - innerHeight) / 2 + pos.Y);
            Graphics.SetViewport(left, top, innerWidth, innerHeight);

            DrawCallback();

            Graphics.SetViewport(0, 0, Graphics.ScreenWidth, Graphics.ScreenHeight);

            if (upScrollVisible)
                DrawScrollMark(windowPos.X, pos.Y, p.upScrollChr);

            if (downScrollVisible)
                DrawScrollMark(windowPos.X, pos.Y + windowSize.Y, p.downScrollChr);
        }

        private void DrawScrollMark(float x, float y, MapCharacter scrollChr)
        {
            /*
            var pos = new Vector2(x, y);
            int divW = Graphics.GetDivWidth(scrollChr.imgId);
            int divH = Graphics.GetDivHeight(scrollChr.imgId);
            Graphics.DrawChipImage(scrollChr.imgId,
                (int)pos.X - divW / 2,
                (int)pos.Y - divH / 2,
                scrollChr.nowFrame, scrollChr.nowDir);
             */
        }
    }
}

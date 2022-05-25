using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

using Yukar.Common.Resource;

namespace Yukar.Engine
{
    public class WindowDrawer
    {
        Window window;
        int windowImageId;

        public Window WindowResource { get { return window; } }
        public int WindowImageID { get { return windowImageId; } }

        public WindowDrawer(Window window, int windowImageId)
        {
            this.window = window;
            this.windowImageId = windowImageId;
        }

        public void Update()
        {
        }

        public void Draw(Vector2 position)
        {
            Graphics.DrawImage(windowImageId, (int)position.X, (int)position.Y);
        }

        public void Draw(Vector2 position, Vector2 windowSize)
        {
            Draw(position, windowSize, Color.White);
        }
        public void Draw(Vector2 position, Vector2 windowSize, Color windowColor)
        {
            Draw(position, windowSize, Vector2.One, windowColor);
        }
        public void Draw(Vector2 position, Vector2 windowSize, Vector2 edgeScale, Color windowColor)
        {
            if (window == null)
                return;

            //int width  = (int)(windowSize.X);
            //int height = (int)(windowSize.Y);
            int originalWidth  = Graphics.GetImageWidth(windowImageId);
            int originalHeight = Graphics.GetImageHeight(windowImageId);

            int px = (int)position.X;
            int py = (int)position.Y;
            int wx = (int)Math.Abs(windowSize.X);
            int wy = (int)Math.Abs(windowSize.Y);

            bool reverseLR = (windowSize.X < 0);
            bool reverseTB = (windowSize.Y < 0);

            int left, right, up, bottom;

            if (wx < window.left + window.right)
            {
                left = wx / 2;
                right = wx - left;
            }
            else
            {
                left = window.left;
                right = window.right;
            }

            if (wy < window.top + window.bottom)
            {
                up = wy / 2;
                bottom = wy - up;
            }
            else
            {
                up = window.top;
                bottom = window.bottom;
            }

            // 単純なスケーリングで描画する場合
            //Graphics.DrawImage(windowImageId, new Rectangle(px, py, wx, wy), new Rectangle(0, 0, originalWidth, originalHeight));

            // 周囲の四辺と中央に分けて描画する場合
            // 下図のように9回の描画で1つのウィンドウを表示する (四隅 + 四辺 + 中央)
            // ┏━┓
            // ┃■┃
            // ┗━┛

            int destLeft = (int)( left * edgeScale.X );
            int destRight = (int)( right * edgeScale.X );
            int destUp = (int)( up * edgeScale.Y );
            int destBottom = (int)( bottom * edgeScale.Y );

            // 四隅の角を描画 左上, 右上, 左下, 右下
            // 四隅が曲線だと辺が繋がらない……? => 元の四隅のサイズより小さい幅 or 高さで描画しようとした際に破綻する window.left, right, up, bottom の値がウィンドウ全体の描画サイズを上回るとダメ 元画像より小さい領域が指定されたときは単純なスケーリングで一括描画する? => あまり綺麗に表示できなかったので描画サイズ/2で試してみる
            Graphics.DrawImage(windowImageId, new Rectangle(px, py, destLeft, destUp), CalcSourceRect(new Rectangle(0, 0, left, up), reverseLR, reverseTB), windowColor);
            Graphics.DrawImage(windowImageId, new Rectangle(px + wx - destRight, py, destRight, destUp), CalcSourceRect(new Rectangle(originalWidth - right, 0, right, up), reverseLR, reverseTB), windowColor);
            Graphics.DrawImage(windowImageId, new Rectangle(px, py + wy - destBottom, destLeft, destBottom), CalcSourceRect(new Rectangle(0, originalHeight - bottom, left, bottom), reverseLR, reverseTB), windowColor);
            Graphics.DrawImage(windowImageId, new Rectangle(px + wx - destRight, py + wy - destBottom, destRight, destBottom), CalcSourceRect(new Rectangle(originalWidth - right, originalHeight - bottom, right, bottom), reverseLR, reverseTB), windowColor);

            // 辺から左右の角を引いた長さ
            int horizonalLineWidth = (wx - destLeft - destRight);
            int verticalLineHeight = (wy - destUp - destBottom);
            int horizonalLineOriginalWidth = (originalWidth - left - right);
            int verticalLineOriginalHeight = (originalHeight - up - bottom);

            switch (window.fillType)
            {
                case Window.FillType.FILL_STREATCH:
                    // 四辺の直線部分を描画 上, 左, 下, 右
                    Graphics.DrawImage(windowImageId, new Rectangle(px + destLeft, py, horizonalLineWidth, destUp), CalcSourceRect(new Rectangle(left, 0, horizonalLineOriginalWidth, up), reverseLR, reverseTB), windowColor);
                    Graphics.DrawImage(windowImageId, new Rectangle(px, py + destUp, destLeft, verticalLineHeight), CalcSourceRect(new Rectangle(0, up, left, verticalLineOriginalHeight), reverseLR, reverseTB), windowColor);
                    Graphics.DrawImage(windowImageId, new Rectangle(px + destLeft, py + wy - destBottom, horizonalLineWidth, destBottom), CalcSourceRect(new Rectangle(left, originalHeight - bottom, horizonalLineOriginalWidth, bottom), reverseLR, reverseTB), windowColor);
                    Graphics.DrawImage(windowImageId, new Rectangle(px + wx - destRight, py + destUp, destRight, verticalLineHeight), CalcSourceRect(new Rectangle(originalWidth - right, up, right, verticalLineOriginalHeight), reverseLR, reverseTB), windowColor);

                    // 中央部分を描画
                    Graphics.DrawImage(windowImageId, new Rectangle(px + destLeft, py + destUp, horizonalLineWidth, verticalLineHeight), CalcSourceRect(new Rectangle(left, up, horizonalLineOriginalWidth, verticalLineOriginalHeight), reverseLR, reverseTB), windowColor);
                    break;

                case Window.FillType.FILL_REPEAT:
                    drawRepeat(window, windowImageId, position, windowSize, windowColor);
                    break;
            }
        }

        private static void drawRepeat(Window rom, int imgId, Vector2 position, Vector2 windowSize, Color windowColor)
        {
            int px = (int)position.X;
            int py = (int)position.Y;

            int srcWidth = Graphics.GetImageWidth(imgId);
            int srcHeight = Graphics.GetImageHeight(imgId);
            int srcTop = rom.top;
            int srcLeft = rom.left;
            int srcBottom = srcHeight - rom.bottom;
            int srcRight = srcWidth - rom.right;
            int srcCenterWidth = srcRight - srcLeft;
            int srcCenterHeight = srcBottom - srcTop;

            int destWidth = (int)windowSize.X;
            int destHeight = (int)windowSize.Y;
            int destTop = rom.top;
            int destLeft = rom.left;
            int destBottom = destHeight - rom.bottom;
            int destRight = destWidth - rom.right;
            int destCenterWidth = destRight - destLeft;
            int destCenterHeight = destBottom - destTop;
            for (int x = 0; x < destCenterWidth; x += srcCenterWidth)
            {
                int width = srcCenterWidth;
                if (width > destCenterWidth - x) width = destCenterWidth - x;

                // 上
                Graphics.DrawImage(imgId, new Rectangle(destLeft + x + px, py, width, destTop), new Rectangle(srcLeft, 0, width, srcTop), windowColor);
                // 下
                Graphics.DrawImage(imgId, new Rectangle(destLeft + x + px, destBottom + py, width, rom.bottom), new Rectangle(srcLeft, srcBottom, width, rom.bottom), windowColor);

                for (int y = 0; y < destCenterHeight; y += srcCenterHeight)
                {
                    int height = srcCenterHeight;
                    if (height > destCenterHeight - y) height = destCenterHeight - y;

                    if (x == 0)
                    {
                        // 左
                        Graphics.DrawImage(imgId, new Rectangle(px, destTop + y + py, destLeft, height), new Rectangle(0, srcTop, srcLeft, height), windowColor);
                        // 右
                        Graphics.DrawImage(imgId, new Rectangle(destRight + px, destTop + y + py, rom.right, height), new Rectangle(srcRight, srcTop, rom.right, height), windowColor);
                    }

                    // 中央
                    Graphics.DrawImage(imgId, new Rectangle(destLeft + x + px, destTop + y + py, width, height), new Rectangle(srcLeft, srcTop, width, height), windowColor);
                }
            }
        }

        private Rectangle CalcSourceRect(Rectangle rectangle, bool isReverseLR, bool isReverseTB)
        {
            var rect = rectangle;

            if (isReverseLR)
            {
                rect.X += rect.Width;
                rect.Width *= -1;
            }

            if (isReverseTB)
            {
                rect.Y += rect.Height;
                rect.Height *= -1;
            }

            return rect;
        }

        public void DrawString(string text, Vector2 position)
        {
            DrawString(0, text, position, Color.White);
        }

        public void DrawString(int fontId, string text, Vector2 position, Color color)
        {
        }

        public int paddingLeft { get { return WindowResource == null ? 0 : WindowResource.left; } }

        public int paddingRight { get { return WindowResource == null ? 0 : WindowResource.right; } }

        public int paddingTop { get { return WindowResource == null ? 0 : WindowResource.top; } }

        public int paddingBottom { get { return WindowResource == null ? 0 : WindowResource.bottom; } }
    }
}

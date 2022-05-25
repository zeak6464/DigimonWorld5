using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

namespace Yukar.Engine
{
    public class TextDrawer
    {
        public enum HorizontalAlignment
        {
            Left,
            Right,
            Center,
        }

        public enum VerticalAlignment
        {
            Top,
            Bottom,
            Center,
        }

        int fontId = 0;

        /// <summary>
        /// テキストの省略時に使用する
        /// </summary>
        private const string ELLIPSIS_TEXT = "...";

        public TextDrawer(int fontId = 0)
        {
            this.fontId = fontId;
        }

        public void Update()
        {
        }

        public void DrawStringSoloColor(string text, Vector2 position, Color color, int zOrder = 0)
        {
            Graphics.DrawStringSoloColor(fontId, text, position, color, zOrder);
        }

        internal void DrawStringSoloColor(string text, Vector2 position, Color color, float scale, int zOrder = 0)
        {
            Graphics.DrawStringSoloColor(fontId, text, position, color, scale, zOrder);
        }

        public void DrawStringSoloColor(string text, Vector2 position, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, Color color, float scale = 1.0f, int zOrder = 0)
        {
            DrawStringSoloColor(text, position, Vector2.Zero, horizontalAlignment, verticalAlignment, color, scale);
        }

        public void DrawStringSoloColor(string text, Vector2 position, Vector2 drawAreaSize, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, Color color, float scale = 1.0f)
        {
            Vector2 offset = Vector2.Zero;

            switch (horizontalAlignment)
            {
                case HorizontalAlignment.Left:      offset.X = 0; break;
                case HorizontalAlignment.Right:     offset.X = (drawAreaSize.X - (MeasureString(text).X * scale)); break;
                case HorizontalAlignment.Center:    offset.X = (drawAreaSize.X / 2 - (MeasureString(text).X / 2 * scale)); break;
            }

            switch (verticalAlignment)
            {
                case VerticalAlignment.Top:     offset.Y = 0; break;
                case VerticalAlignment.Bottom:  offset.Y = (drawAreaSize.Y - (MeasureString(text).Y * scale)); break;
                case VerticalAlignment.Center:  offset.Y = (drawAreaSize.Y / 2 - (MeasureString(text).Y / 2 * scale)); break;
            }

            var drawPosition = position + offset;

            DrawStringSoloColor(text, drawPosition, color, scale);
        }

        public void DrawString(string text, Vector2 position, Color color, int zOrder = 0)
        {
            Graphics.DrawString(fontId, text, position, color, zOrder);
        }

        internal void DrawString(string text, Vector2 position, Color color, float scale, int zOrder = 0)
        {
            Graphics.DrawString(fontId, text, position, color, scale, zOrder);
        }

        internal void DrawString(string text, Vector2 position, Color color, float scale, bool bold, bool italic)
        {
            Graphics.DrawString(fontId, text, position, color, scale, 0, bold, italic);
        }

        public void DrawString(SharpKmyGfx.Font font, string text, Vector2 position, Color color, int zOrder = 0)
        {
            Graphics.DrawString(font, text, position, color, zOrder);
        }

        internal void DrawString(SharpKmyGfx.Font font, string text, Vector2 position, Color color, float scale, int zOrder = 0)
        {
            Graphics.DrawString(font, text, position, color, scale, zOrder);
        }

        public void DrawString(string text, Vector2 position, HorizontalAlignment alignment, Color color)
        {
            DrawString(text, position, 0, alignment, color);
        }

        public void DrawString(string text, Vector2 position, VerticalAlignment alignment, Color color)
        {
            DrawString(text, position, 0, alignment, color);
        }

        public void DrawString(string text, Vector2 position, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, Color color, float scale = 1.0f)
        {
            DrawString(text, position, Vector2.Zero, horizontalAlignment, verticalAlignment, color, scale);
        }

        private Vector2 CalcTextDrawOffset(string text, int width, int textWidth, HorizontalAlignment alignment)
        {
            Vector2 offset = Vector2.Zero;

            switch (alignment)
            {
                case HorizontalAlignment.Left:      offset.X = 0; break;
                case HorizontalAlignment.Right:     offset.X = (width - textWidth); break;
                case HorizontalAlignment.Center:    offset.X = (width / 2 - textWidth / 2); break;
            }

            return offset;
        }
        private Vector2 CalcTextDrawOffset(string text, int height, int textHeight, VerticalAlignment alignment)
        {
            Vector2 offset = Vector2.Zero;

            switch (alignment)
            {
                case VerticalAlignment.Top:     offset.Y = 0; break;
                case VerticalAlignment.Bottom:  offset.Y = (height - textHeight); break;
                case VerticalAlignment.Center:  offset.Y = (height / 2 - textHeight / 2); break;
            }

            return offset;
        }

        private Vector2 CalcTextDrawOffset(string text, int width, HorizontalAlignment alignment, float scale = 1.0f)
        {
            return CalcTextDrawOffset(text, width, (int)(MeasureString(text).X * scale), alignment);
        }

        private Vector2 CalcTextDrawOffset(string text, int height, VerticalAlignment alignment, float scale = 1.0f)
        {
            return CalcTextDrawOffset(text, height, (int)(MeasureString(text).Y * scale), alignment);
        }

        private Vector2 CalcTextDrawOffset(string text, Vector2 drawAreaSize, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, float scale = 1.0f)
        {
            Vector2 offset = Vector2.Zero;

            offset.X = CalcTextDrawOffset(text, (int)(drawAreaSize.X), horizontalAlignment, scale).X;
            offset.Y = CalcTextDrawOffset(text, (int)(drawAreaSize.Y), verticalAlignment, scale).Y;

            return offset;
        }

        private Vector2 CalcTextDrawOffset(SharpKmyGfx.Font font, string text, int width, HorizontalAlignment alignment, float scale = 1.0f)
        {
            return CalcTextDrawOffset(text, width, (int)(MeasureString(font, text).X * scale), alignment);
        }

        private Vector2 CalcTextDrawOffset(SharpKmyGfx.Font font, string text, int height, VerticalAlignment alignment, float scale = 1.0f)
        {
            return CalcTextDrawOffset(text, height, (int)(MeasureString(font, text).Y * scale), alignment);
        }

        private Vector2 CalcTextDrawOffset(SharpKmyGfx.Font font, string text, Vector2 drawAreaSize, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, float scale = 1.0f)
        {
            Vector2 offset = Vector2.Zero;

            offset.X = CalcTextDrawOffset(font, text, (int)(drawAreaSize.X), horizontalAlignment, scale).X;
            offset.Y = CalcTextDrawOffset(font, text, (int)(drawAreaSize.Y), verticalAlignment, scale).Y;

            return offset;
        }

        public void DrawString(string text, Vector2 position, int width, HorizontalAlignment alignment, Color color, float scale = 1.0f)
        {
            var drawPosition = position;

            drawPosition.X += CalcTextDrawOffset(text, width, alignment, scale).X;

            DrawString(text, drawPosition, color, scale);
        }

        public void DrawString(string text, Vector2 position, int height, VerticalAlignment alignment, Color color, float scale = 1.0f)
        {
            var drawPosition = position;

            drawPosition.Y += CalcTextDrawOffset(text, height, alignment, scale).Y;

            DrawString(text, drawPosition, color, scale);
        }

        public void DrawString(string text, Vector2 position, Vector2 drawAreaSize, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, Color color, float scale = 1.0f)
        {
            var drawPosition = position + CalcTextDrawOffset(text, drawAreaSize, horizontalAlignment, verticalAlignment, scale);

            DrawString(text, drawPosition, color, scale);
        }

        public void DrawString(SharpKmyGfx.Font font, string text, Vector2 position, Vector2 drawAreaSize, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, Color color, float scale = 1.0f)
        {
            var drawPosition = position + CalcTextDrawOffset(font, text, drawAreaSize, horizontalAlignment, verticalAlignment, scale);

            DrawString(font, text, drawPosition, color, scale);
        }

        public Vector2 MeasureString(string text)
        {
            return Graphics.MeasureString(fontId, text);
        }

        public Vector2 MeasureString(SharpKmyGfx.Font font, string text)
        {
            var size = font.measureString(System.Text.Encoding.UTF8.GetBytes(text));

            return new Vector2(size.x, size.y);
        }

        public string[] SplitString(string text, int width, float scale = 1.0f)
        {
            var strList = new List<string>();
            int count = 0;

            while (count != text.Length)
            {
                string str = "";

                while (count != text.Length && MeasureString(str + text[ count ]).X * scale < width)
                {
                    str += text[ count ];

                    count++;
                }

                strList.Add(str);
            }

            return strList.ToArray();
        }

        internal string GetContentText(string text, int width, float scale)
        {
            float ellipsisLength = MeasureString(ELLIPSIS_TEXT).X * scale;

            if (MeasureString(text).X * scale < width)
            {
                return text;
            }

            var result = text;
            if(result.Length == 0)
            {
                return result;
            }
            while (MeasureString(result).X * scale > width - ellipsisLength)
            {
                result = result.Remove(result.Length - 1);
            }
            return result + ELLIPSIS_TEXT;
        }

        /// <summary>
        /// 枠内に文字が入るかどうか
        /// </summary>
        /// <param name="text">長さを図りたい</param>
        /// <param name="scale">文字の描画スケール</param>
        /// <param name="posX">文字描画開始位置</param>
        /// <param name="innerWidth">枠の幅</param>
        /// <returns></returns>
        internal bool CanTextFitInto(string text, float scale, float posX, float innerWidth)
        {
            // 余白が4.0fっぽい
            return ((MeasureString(text).X * scale) + posX - 4.0f < innerWidth);
        }
    }
}

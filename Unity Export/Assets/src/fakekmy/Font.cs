using System;
using SharpKmyMath;

namespace SharpKmyGfx
{
    public class Font
    {
        //private string fontPath;
        public int fontSize;
        //private int v1;
        //private int v2;

        public Font(string fontPath, int fontSize, int v1, int v2)
        {
            //this.fontPath = fontPath;
            this.fontSize = fontSize;
            //this.v1 = v1;
            //this.v2 = v2;
        }

        public Font()
        {
        }

        internal void Release()
        {
            // Dummy
        }

        internal static Font newSystemFont(byte[] bytes, uint fontSize, int v1, int v2)
        {
            var result = new Font();
            result.fontSize = (int)fontSize;
            return result;
        }

        internal Vector2 measureString(byte[] bytes)
        {
            var content = new UnityEngine.GUIContent(System.Text.Encoding.UTF8.GetString(bytes));
            var style = new UnityEngine.GUIStyle();
            style.font = SpriteBatch.defaultFont;
            style.fontSize = fontSize;
            var size = style.CalcSize(content);
            return new Vector2(size.x, size.y);
        }

        internal static Font newSystemFontGdi(byte[] bytes, uint fontSize, int v2, int v3)
        {
            var result = new Font();
            result.fontSize = (int)fontSize;
            return result;
        }
    }
}
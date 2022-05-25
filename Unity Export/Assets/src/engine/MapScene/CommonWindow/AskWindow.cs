using Microsoft.Xna.Framework;
namespace Yukar.Engine
{
    class AskWindow : SelectWindow
    {
        string text;
        string[] strs;
        bool[] flags;
        internal const int RESULT_OK = 0;
        internal const int RESULT_CANCEL = 1;

        internal void setInfo(string text, string p1, string p2)
        {
            this.text = text;

            strs = new string[2];
            strs[0] = p1;
            strs[1] = p2;

            flags = new bool[2];
            flags[0] = flags[1] = true;

            maxItems = 2;
            columnNum = 2;

            disableUpDown = true;
        }

        internal override void DrawCallback()
        {
            var size = new Vector2(innerWidth, TEXT_HEIGHT * 2);    // 2行ぶんつかって本文を書く
            p.textDrawer.DrawString(text, Vector2.Zero, size,
                TextDrawer.HorizontalAlignment.Center,
                TextDrawer.VerticalAlignment.Center, Color.White);

            Graphics.sInstance.mSpriteOffset.Y += innerHeight - itemHeight;

            DrawSelect(strs, flags);
        }
    }
}

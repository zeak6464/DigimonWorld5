using Microsoft.Xna.Framework;
namespace Yukar.Engine
{
    class HintDisplay
    {
        internal enum HintType
        {
            NONE,
            DEFAULT,
            STATUS,
            CONFIG,
        }

        int imgId;
        //int imgWidth;
        int imgHeight;

        HintType state;
        private CommonWindow.ParamSet p;

        float count = -1;
        const int ANIM_TIME = 20;
        bool closing = false;

        public HintDisplay(CommonWindow.ParamSet res)
        {
            p = res;

            imgId = Graphics.LoadImageDiv("./res/system/button_guide.png", 4, 4);
            //imgWidth = Graphics.GetDivWidth(imgId);
            imgHeight = Graphics.GetDivHeight(imgId);

            state = HintType.NONE;
        }

        internal void Draw()
        {
            switch (state)
            {
                case HintType.NONE:
                    break;
                case HintType.DEFAULT:
                    DrawDefaultHint();
                    break;
                case HintType.STATUS:
                    DrawStatusHint();
                    break;
            }
        }

        internal void Show(HintType hintType)
        {
            closing = false;
            state = hintType;
            count = 0;
        }

        // 通常時のヒント
        private void DrawDefaultHint()
        {
            var pos = new Vector2(200, Graphics.ScreenHeight - imgHeight);
            var size = new Vector2(320, imgHeight);

            // Y座標はアニメーションする
            addOffsetY(ref pos);

            // カーソル説明
            Graphics.DrawChipImage(imgId, (int)pos.X, (int)pos.Y, 0, 3);
            pos.X += 20;
            Graphics.DrawChipImage(imgId, (int)pos.X, (int)pos.Y, 3, 0);
            pos.X += 24;
            Graphics.DrawChipImage(imgId, (int)pos.X, (int)pos.Y, 0, 0);
            pos.X += 46;
            p.textDrawer.DrawString(p.gs.glossary.moveCursor, pos, size, TextDrawer.HorizontalAlignment.Left, TextDrawer.VerticalAlignment.Center, Color.White, 0.75f);
            pos.X += 160;

            bool isJapaneseLayout = System.Threading.Thread.CurrentThread.CurrentUICulture.Name.StartsWith("ja");

            // B・Z説明
            Graphics.DrawChipImage(imgId, (int)pos.X, (int)pos.Y, isJapaneseLayout ? 1 : 2, 3);
            pos.X += 20;
            Graphics.DrawChipImage(imgId, (int)pos.X, (int)pos.Y, 3, 0);
            pos.X += 30;
            Graphics.DrawChipImage(imgId, (int)pos.X, (int)pos.Y, 2, 0);
            pos.X += 50;
            p.textDrawer.DrawString(p.gs.glossary.decide, pos, size, TextDrawer.HorizontalAlignment.Left, TextDrawer.VerticalAlignment.Center, Color.White, 0.75f);
            pos.X += 96;

            // A・X説明
            Graphics.DrawChipImage(imgId, (int)pos.X, (int)pos.Y, isJapaneseLayout ? 2 : 1, 3);
            pos.X += 20;
            Graphics.DrawChipImage(imgId, (int)pos.X, (int)pos.Y, 3, 0);
            pos.X += 20;
            Graphics.DrawChipImage(imgId, (int)pos.X, (int)pos.Y, 1, 0);
            pos.X += 40;
            p.textDrawer.DrawString(p.gs.glossary.battle_cancel, pos, size, TextDrawer.HorizontalAlignment.Left, TextDrawer.VerticalAlignment.Center, Color.White, 0.75f);
        }

        // ステータス画面のヒント
        private void DrawStatusHint()
        {
            var pos = new Vector2(224, Graphics.ScreenHeight - imgHeight);
            var size = new Vector2(320, imgHeight);

            // Y座標はアニメーションする
            addOffsetY(ref pos);

            // カーソル説明
            Graphics.DrawChipImage(imgId, (int)pos.X, (int)pos.Y, 0, 3);
            pos.X += 20;
            Graphics.DrawChipImage(imgId, (int)pos.X, (int)pos.Y, 3, 0);
            pos.X += 20;
            Graphics.DrawChipImage(imgId, (int)pos.X, (int)pos.Y, 1, 2);
            pos.X += 40;
            p.textDrawer.DrawString(p.gs.glossary.changeCharacter, pos, size, TextDrawer.HorizontalAlignment.Left, TextDrawer.VerticalAlignment.Center, Color.White, 0.75f);
            pos.X += 160;

            // B・Z説明
            Graphics.DrawChipImage(imgId, (int)pos.X, (int)pos.Y, 1, 3);
            pos.X += 20;
            Graphics.DrawChipImage(imgId, (int)pos.X, (int)pos.Y, 3, 0);
            pos.X += 30;
            Graphics.DrawChipImage(imgId, (int)pos.X, (int)pos.Y, 2, 0);
            pos.X += 50;
            p.textDrawer.DrawString(p.gs.glossary.close, pos, size, TextDrawer.HorizontalAlignment.Left, TextDrawer.VerticalAlignment.Center, Color.White, 0.75f);
            pos.X += 96;

            // A・X説明
            Graphics.DrawChipImage(imgId, (int)pos.X, (int)pos.Y, 2, 3);
            pos.X += 20;
            Graphics.DrawChipImage(imgId, (int)pos.X, (int)pos.Y, 3, 0);
            pos.X += 20;
            Graphics.DrawChipImage(imgId, (int)pos.X, (int)pos.Y, 1, 0);
            pos.X += 40;
            p.textDrawer.DrawString(p.gs.glossary.battle_cancel, pos, size, TextDrawer.HorizontalAlignment.Left, TextDrawer.VerticalAlignment.Center, Color.White, 0.75f);
        }

        private void addOffsetY(ref Vector2 pos)
        {
            // アニメーション中以外は座標を変更しない
            if (count < 0)
                return;

            // アニメーション中だったら座標にオフセットを足す
            float delta = 1 - count / ANIM_TIME;
            delta = delta * delta * delta;

            if (closing)
                pos.Y += imgHeight * (1 - delta);
            else
                pos.Y += imgHeight * delta;
        }

        internal void Hide()
        {
            closing = true;
            count = 0;
        }

        internal void Update()
        {
            if (count >= 0)
            {
                count += GameMain.getRelativeParam60FPS();
                if (count >= ANIM_TIME)
                {
                    if(closing)
                        state = HintType.NONE;
                    count = -1;
                }
            }
        }
    }
}

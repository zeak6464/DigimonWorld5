using Microsoft.Xna.Framework;
using StatusAilments = Yukar.Common.GameData.Hero.StatusAilments;

namespace Yukar.Engine
{
    public class CharacterFaceImageDrawer
    {
        public static Color GetImageColor(BattlePlayerData player)
        {
            var color = Color.White;

            if (player.Status == StatusAilments.DOWN) color = Color.DarkSlateGray;

            return color;
        }

        public void Draw(BattlePlayerData player)
        {
            if (player.characterImageId < 0) return;

            var color = player.characterImageColor;
            if (player.imageAlpha < 1)
                color = new Color(player.imageAlpha, player.imageAlpha, player.imageAlpha, player.imageAlpha);

            Rectangle imageRect;
            Rectangle drawRect;
            GetDrawRect(player, out drawRect, out imageRect);
            Graphics.DrawImage(player.characterImageId, drawRect, imageRect, color);
        }

        public static Rectangle GetTrimming()
        {
            // 元の画像から 上 : 64px, 下 : 32px トリミングして描画する
            return new Rectangle(0, 64, 320, 544 - 64 - 32);
        }

        public static void GetDrawRect(BattlePlayerData player, out Rectangle outDrawRect, out Rectangle outImageRect)
        {
            outImageRect = GetTrimming();
            var offset = player.characterImageTween.CurrentValue;
            outDrawRect = new Rectangle((int)player.characterImagePosition.X, (int)player.characterImagePosition.Y, (int)player.characterImageSize.X, (int)player.characterImageSize.Y);

            if (player.characterImageEffectTween.IsPlayTween)
            {
                offset += player.characterImageEffectTween.CurrentValue;
            }

            // 左右反転
            if (player.isCharacterImageReverse)
            {
                outImageRect.X = outImageRect.Width;
                outImageRect.Width *= -1;

                offset.X *= -1;
            }

            outDrawRect.X += (int)offset.X;
            outDrawRect.Y += (int)offset.Y;
        }
    }

}

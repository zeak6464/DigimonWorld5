using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

using Yukar.Common;

namespace Yukar.Engine
{
    public class BattleStatusWindowDrawer
    {
		public class StatusData
		{
			public string Name { get; set; }
			public int HitPoint { get; set; }
			public int MagicPoint { get; set; }
			public int MaxHitPoint { get; set; }
			public int MaxMagicPoint { get; set; }
			public Common.GameData.Hero.StatusAilments StatusAilments { get; set; }
			public StatusIconType ParameterStatus { get; set; }
		}

        [Flags]
        public enum StatusIconType
        {
            None = 0,

            PowerUp    = 1,
            VitalityUp = 2,
            MagicUp    = 4,
            SpeedUp    = 8,

            PowerDown    = 16,
            VitalityDown = 32,
            MagicDown    = 64,
            SpeedDown    = 128,
        }

        WindowDrawer windowDrawer;
        TextDrawer textDrawer;
        internal GaugeDrawer gaugeDrawer;
        int gaugeBaseImageId;
        int gaugeImageId;
        int windowEffectId;
        Blinker blinker;

        public string HPLabelText { get; set; }
        public string MPLabelText { get; set; }

        public EffectDrawer StatusAilmentEffect { get; set; }
        public EffectDrawer PositiveEnhanceEffect { get; set; }
        public EffectDrawer NegativeEnhanceEffect { get; set; }

        public bool IsActive { get; set; }

        public BattleStatusWindowDrawer(WindowDrawer windowDrawer)
        {
            this.windowDrawer = windowDrawer;

            textDrawer = new TextDrawer(1);

            gaugeBaseImageId = Graphics.LoadImage("./res/battle/status_bar_panel.png");
            gaugeImageId = Graphics.LoadImage("./res/battle/status_bar.png");

            var windowInfo = new Common.Resource.Window() { left = 3, right = 3, top = 3, bottom = 3 };
            gaugeDrawer = new GaugeDrawer(new WindowDrawer(windowInfo, gaugeBaseImageId), new WindowDrawer(windowInfo, gaugeImageId), new WindowDrawer(windowInfo, gaugeImageId));

            HPLabelText = "HP";
            MPLabelText = "MP";

            windowEffectId = Graphics.LoadImage("./res/battle/battle_status_bg_effect.png");

            blinker = new Blinker();
            blinker.setColor(new Color(255, 255, 255, 255), new Color(255, 255, 255, 16), 30);
        }

        public void Release()
        {
            Graphics.UnloadImage(gaugeBaseImageId);
            Graphics.UnloadImage(gaugeImageId);

            Graphics.UnloadImage(windowEffectId);
        }

        public void Draw(StatusData statusData, Vector2 windowPosition, Vector2 windowSize, bool light = false)
        {
            Color windowColor = (statusData.StatusAilments == Common.GameData.Hero.StatusAilments.DOWN) ?Color.Red :Color.White;

            // 下地のウィンドウを表示する
            windowDrawer.Draw(windowPosition, windowSize, windowColor);
            if (light)
            {
                Rectangle pos = new Rectangle((int)windowPosition.X, (int)windowPosition.Y, (int)windowSize.X, (int)windowSize.Y);
                Rectangle area = new Rectangle(0, 0, Graphics.GetImageWidth(windowEffectId), Graphics.GetImageHeight(windowEffectId));

                if (windowSize.X < 0)
                {
                    pos.X -= pos.Width;
                }

                blinker.update();
                //Graphics.DrawImage(windowEffectId, (int)windowPosition.X, (int)windowPosition.Y, blinker.getColor());
                Graphics.DrawImage(windowEffectId, pos, area, blinker.getColor());
            }

            Draw(statusData, windowPosition);
        }

        internal void Draw(StatusData statusData, Vector2 windowPosition, Vector2 windowSize, Color color)
        {
            // 下地のウィンドウを表示する
            windowDrawer.Draw(windowPosition, windowSize, color);
            Draw(statusData, windowPosition);
        }

        internal void Draw(StatusData statusData, Vector2 windowPosition)
        {
            var effectSize = new Vector2(48, 48);
            var effectPosition = windowPosition + new Vector2(effectSize.X * 0.5f, -effectSize.Y * 0.5f);

            // パラメータ用のアイコン
            if (PositiveEnhanceEffect != null)
            {
                PositiveEnhanceEffect.draw((int)effectPosition.X, (int)effectPosition.Y);

                effectPosition.X += effectSize.X;
            }

            if (NegativeEnhanceEffect != null)
            {
                NegativeEnhanceEffect.draw((int)effectPosition.X, (int)effectPosition.Y);

                effectPosition.X += effectSize.X;
            }

            // 状態異常のアイコンを表示する
            if (StatusAilmentEffect != null)
            {
                StatusAilmentEffect.draw((int)effectPosition.X, (int)effectPosition.Y);

                effectPosition.X += effectSize.X;
            }

            // ターン管理用のゲージを表示する
            Vector2 gaugeSize = new Vector2(160, 8);

            gaugeDrawer.Draw(windowPosition + new Vector2(10, 40), gaugeSize, statusData.MaxHitPoint > 0 ? (float)statusData.HitPoint / statusData.MaxHitPoint : 0, GaugeDrawer.GaugeOrientetion.HorizonalRightToLeft, new Color(150, 150, 240));
            gaugeDrawer.Draw(windowPosition + new Vector2(10, 62), gaugeSize, statusData.MaxMagicPoint > 0 ? (float)statusData.MagicPoint / statusData.MaxMagicPoint : 0, GaugeDrawer.GaugeOrientetion.HorizonalRightToLeft, new Color(16, 180, 96));

            // キャラクター名などのテキストを表示する
            Vector2 textPosition = windowPosition + new Vector2(8, 0);

            textDrawer.DrawString(statusData.Name, textPosition, Color.White, 0.8f); textPosition.X += 6; textPosition.Y += 24;

            textDrawer.DrawString(HPLabelText, textPosition, Color.White, 0.75f);
            textDrawer.DrawString(string.Format("{0}", statusData.HitPoint), textPosition + new Vector2(96, 0), Color.White, 0.75f); textPosition.Y += 22;

            textDrawer.DrawString(MPLabelText, textPosition, Color.White, 0.75f);
            textDrawer.DrawString(string.Format("{0}", statusData.MagicPoint), textPosition + new Vector2(96, 0), Color.White, 0.75f); textPosition.Y += 22;
        }
    }
}

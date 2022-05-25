using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

using Yukar.Common;

namespace Yukar.Engine
{
    public class ResultStatusWindowDrawer
    {
		public class StatusData
		{
			public string Name { get; set; }
            public int CurrentLevel { get; set; }
            public int NextLevel { get; set; }
			public float GaugeParcent { get; set; }
		}

        WindowDrawer windowDrawer;
        GaugeDrawer gaugeDrawer;
        TextDrawer textDrawer;

        public string LevelLabelText { get; set; }
        public string ExpLabelText { get; set; }

        public ResultStatusWindowDrawer(WindowDrawer windowDrawer, GaugeDrawer gaugeDrawer)
        {
            this.windowDrawer = windowDrawer;
            this.gaugeDrawer = gaugeDrawer;

            textDrawer = new TextDrawer(1);

            LevelLabelText = "Lv";
            ExpLabelText = "EXP";
        }

        public void Release()
        {
        }

        public void Draw(StatusData statusData, Vector2 windowPosition, Vector2 windowSize, Color color)
        {
            // 下地のウィンドウを表示する
            windowDrawer.Draw(windowPosition, windowSize, color);
            Draw(statusData, windowPosition);
        }

        internal void Draw(StatusData statusData, Vector2 windowPosition)
        {
            //var drawIconIndexList = new List<int>();

            Vector2 textPosition = windowPosition + new Vector2(8, 0);
            Vector2 bodyAreaSize = new Vector2(110, 16);

            // Name
            textDrawer.DrawString(statusData.Name, textPosition, Color.White, 0.9f); textPosition.X += 6; textPosition.Y += 24;

            // Level
            bool isDrawNextLevel = (statusData.NextLevel > statusData.CurrentLevel);
            const float TextScale = 0.85f;

            string levelText = string.Format("{0}", statusData.CurrentLevel);

            if (isDrawNextLevel)
            {
                levelText += " → ";
            }

            textDrawer.DrawString(LevelLabelText, textPosition, Color.White, TextScale);
            textDrawer.DrawString(levelText, textPosition + new Vector2(48, 0), Color.White, TextScale);

            if (isDrawNextLevel)
            {
                textDrawer.DrawString(statusData.NextLevel.ToString(), textPosition + new Vector2(48, 0) + new Vector2(textDrawer.MeasureString(levelText).X, 0), Color.LawnGreen, TextScale);
            } 
            
            textPosition.Y += 22;

            // Exp
            textDrawer.DrawString(ExpLabelText, textPosition, Color.White, TextScale);
            gaugeDrawer.Draw(textPosition + new Vector2(48, 4), bodyAreaSize, statusData.GaugeParcent, GaugeDrawer.GaugeOrientetion.HorizonalRightToLeft);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

namespace Yukar.Engine
{
    public class GaugeDrawer
    {
        public enum GaugeOrientetion
        {
            HorizonalLeftToRight,   //   0%[======]100%
            HorizonalRightToLeft,   // 100%[======]0%
            VerticalUpToDown,
            VerticalDownToUp,
        }

        WindowDrawer baseWindowDrawer;
        WindowDrawer gaugeWindowDrawer;
        WindowDrawer gaugeMaxWindowDrawer;

        public GaugeDrawer(WindowDrawer b, WindowDrawer gauge, WindowDrawer max)
        {
            baseWindowDrawer = b;
            gaugeWindowDrawer = gauge;
            gaugeMaxWindowDrawer = max;
        }

        private Vector2 GetDrawSize(Vector2 gaugeSize, float parcent, GaugeOrientetion gaugeOrientetion)
        {
            var drawSize = gaugeSize;

            switch (gaugeOrientetion)
            {
                case GaugeOrientetion.HorizonalLeftToRight:
                    drawSize.X *= parcent;
                    //drawSize.X = drawSize.Y = 0;
                    //drawSize.Width = (int)(drawSize.Width * parcent);
                    break;
                case GaugeOrientetion.HorizonalRightToLeft:
                    drawSize.X *= parcent;
                    //drawSize.X = (int)(drawSize.Width * (1.0f - parcent));
                    //drawSize.Y = 0;
                    //drawSize.Width = (int)(drawSize.Width * parcent);
                    break;
                case GaugeOrientetion.VerticalUpToDown:
                    drawSize.Y *= parcent;
                    //drawSize.X = drawSize.Y = 0;
                    //drawSize.Height = (int)(drawSize.Height * parcent);
                    break;
                case GaugeOrientetion.VerticalDownToUp:
                    drawSize.Y *= parcent;
                    //drawSize.X = 0;
                    //drawSize.Y = (int)(drawSize.Height * (1.0f - parcent));
                    //drawSize.Height = (int)(drawSize.Height * parcent);
                    break;
            }

            return drawSize;
        }

        public void Draw(Vector2 position, Vector2 gaugeSize, float parcent, GaugeOrientetion gaugeOrientetion)
        {
            baseWindowDrawer.Draw(position, gaugeSize);

            if (parcent >= 1.0f)
            {
                gaugeMaxWindowDrawer.Draw(position, GetDrawSize(gaugeSize, parcent,gaugeOrientetion));
            }
            else
            {
                gaugeWindowDrawer.Draw(position, GetDrawSize(gaugeSize, parcent,gaugeOrientetion));
            }
        }
        public void Draw(Vector2 position, Vector2 gaugeSize, float parcent, GaugeOrientetion gaugeOrientetion, Color gaugeColor)
        {
            baseWindowDrawer.Draw(position, gaugeSize);

            gaugeWindowDrawer.Draw(position, GetDrawSize(gaugeSize, parcent, gaugeOrientetion), gaugeColor);
        }
    }
}

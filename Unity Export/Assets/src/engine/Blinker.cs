using Microsoft.Xna.Framework;
namespace Yukar.Engine
{
    public class Blinker
    {
        Color fromColor;
        Color toColor;
        Color nowColor;
        float time;
        float now;
        internal Blinker() { }
        internal Blinker(Color a, Color b, int t){
            nowColor = a;
            fromColor = a;
            toColor = b;
            time = (float)t;
        }
        internal void setColor(Color a, Color b, int t){
            nowColor = a;
            fromColor = a;
            toColor = b;
            time = (float)t;
        }
        internal void update()
        {
            now += GameMain.getRelativeParam60FPS();
            if (now > time * 2)
                now -= time * 2;

            float t;

            if (now < time)
            {
                t = (float)now / time;
            }
            else
            {
                float nowRet = now - time;
                t = 1 - (nowRet / time);
            }

            float invT = 1 - t;

            nowColor.R = (byte)(fromColor.R * t + toColor.R * invT);
            nowColor.G = (byte)(fromColor.G * t + toColor.G * invT);
            nowColor.B = (byte)(fromColor.B * t + toColor.B * invT);
            nowColor.A = (byte)(fromColor.A * t + toColor.A * invT);
        }
        internal Color getColor()
        {
            return nowColor;
        }
    }
}

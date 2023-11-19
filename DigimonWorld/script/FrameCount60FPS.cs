using Microsoft.Xna.Framework;
using System;
using System.Threading;
using Yukar.Engine;

namespace Bakin
{
    public class FrameCount60FPS : BakinObject
    {
        private string text;
        private int oldSec;
        private int fps;
        private int frameCount;

        // Create a GameTime object and set the desired frame rate to 60 FPS.
        private GameTime gameTime = new GameTime();
        private TimeSpan targetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / 60.0); // 60 FPS

        public override void Start()
        {
            if (Thread.CurrentThread.CurrentUICulture.Name.StartsWith("ja"))
            {
                text = "本プロジェクトの内容は全て開発中のものです。 © 2022 SmileBoom Co.Ltd. / rev.50426";
            }
            else
            {
                text = "All contents of this project are under development. © 2022 SmileBoom Co.Ltd. / rev.51580";
            }

            oldSec = DateTime.Now.Second;
        }

        public override void Update(GameTime gameTime)
        {
            // Ensure that the game logic updates at a fixed rate.
            this.gameTime.ElapsedGameTime = targetElapsedTime;
            this.gameTime.TotalGameTime += targetElapsedTime;

            // Your game logic here.

            base.Update(this.gameTime);
        }

        public override void AfterDraw()
        {
            Graphics.DrawFillRect(0, 0, Graphics.ScreenWidth, 24, 0, 0, 0, 32);

            Graphics.DrawString(0, text, new Vector2(4, 4), Color.White, new Rectangle(0, 0, Graphics.ScreenWidth, 24), 0.5f);

            var fpsStr = fps + " FPS";
            var scl = 0.8f;
            var sz = Graphics.MeasureString(0, fpsStr);
            Graphics.DrawString(0, fpsStr, new Vector2(Graphics.ScreenWidth - sz.X * scl - 4, -1), Color.White, new Rectangle(0, 0, Graphics.ScreenWidth, 24), scl);

            frameCount++;
            if (oldSec != DateTime.Now.Second)
            {
                oldSec = DateTime.Now.Second;
                fps = frameCount;
                frameCount = 0;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;

namespace Yukar.Engine
{
    public class VirtualPad
    {
        private int virtualPadArrowImageId = 0;

        public VirtualPad()
        {
#if WINDOWS
            using (var imageStream = new MemoryStream())
            {
                Properties.Resources.arrow.Save(imageStream, System.Drawing.Imaging.ImageFormat.Png);
                virtualPadArrowImageId = Graphics.LoadImage(imageStream);
            }
#endif
        }

        public void Update()
        {
        }

        public void Draw(myVector2 drawPosition, float padImageScale)
        {
            if (!Input.IsVirtualPadEnable())
                return;

            int imageWidth = Graphics.GetImageWidth(virtualPadArrowImageId);
            int imageHeight = Graphics.GetImageHeight(virtualPadArrowImageId);
            int imageScaledWidth = (int)(imageWidth * padImageScale);
            int imageScaledHeight = (int)(imageHeight * padImageScale);

            int virtualPadDrawPositionX = (int)drawPosition.X - imageScaledWidth / 2;
            int virtualPadDrawPositionY = (int)drawPosition.Y - imageScaledHeight / 2;

            var rect = new Rectangle(virtualPadDrawPositionX, virtualPadDrawPositionY, imageScaledWidth, imageScaledHeight);
            var source = new Rectangle(0, 0, imageWidth, imageHeight);

            Graphics.DrawImage(virtualPadArrowImageId, rect, source);
            
            //var touchState = Touch.GetState();

            //Graphics.DrawLine(touchState.TouchBeginPosition, touchState.TouchCurrentPosition, Color.White, 4);
        }
    }
}

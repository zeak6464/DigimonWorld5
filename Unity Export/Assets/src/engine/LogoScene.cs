using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;

namespace Yukar.Engine
{
    class LogoScene
    {
        private GameMain owner;
        private int logoImageId = -1;
        private int imgWidth;
        private int imgHeight;

        private int logoImageId2 = -1;
        private int imgWidth2 = 0;
        private int imgHeight2 = 0;

        private float imgAlpha;
        private float screenAlpha;
        private float wait;

        private int state;

        internal LogoScene(GameMain owner)
        {
            this.owner = owner;

#if TRIAL
            using (var imageStream = new MemoryStream())
            {
                Properties.LogoResource.logo1.Save(imageStream, System.Drawing.Imaging.ImageFormat.Png);
                logoImageId = Graphics.LoadImage(imageStream);
                imgWidth = Graphics.GetImageWidth(logoImageId);
                imgHeight = Graphics.GetImageHeight(logoImageId);
            }
            using (var imageStream = new MemoryStream())
            {
                Properties.LogoResource.logo2.Save(imageStream, System.Drawing.Imaging.ImageFormat.Png);
                logoImageId2 = Graphics.LoadImage(imageStream);
                imgWidth2 = Graphics.GetImageWidth(logoImageId2);
                imgHeight2 = Graphics.GetImageHeight(logoImageId2);
            }
#else
            var splashRes = owner.catalog.getItemFromGuid(owner.catalog.getGameSettings().title.splashImage) as Common.Resource.ResourceItem;
            if (splashRes != null)
                logoImageId = Graphics.LoadImage(splashRes.path);
            imgWidth = Graphics.GetImageWidth(logoImageId);
            imgHeight = Graphics.GetImageHeight(logoImageId);
#endif

            init();
        }

        private void init()
        {
            state = 0;
            imgAlpha = 0;
            screenAlpha = 0;
            wait = 0;
        }

        internal void finalize()
        {
            if(logoImageId >= 0)
                Graphics.UnloadImage(logoImageId);
            logoImageId = 0;
        }

		//------------------------------------------------------------------------------
		/**
		 *	描画処理
		 */
        internal void Draw()
        {
            Graphics.BeginDraw();

            int alpha = (int)imgAlpha;

            Graphics.DrawFillRect(0, 0, Graphics.ViewportWidth, Graphics.ViewportHeight, 255, 255, 255, 255);
            var logoColor = new Color(alpha, alpha, alpha, alpha);
            Graphics.DrawImage(logoImageId, (Graphics.ViewportWidth - imgWidth) / 2, (Graphics.ViewportHeight - imgHeight) / 2, logoColor);

            byte clearColor = (byte)(logoImageId2 < 0 ? 0 : screenAlpha);
            Graphics.DrawFillRect(0, 0, Graphics.ViewportWidth, Graphics.ViewportHeight, clearColor, clearColor, clearColor, (byte)screenAlpha);

            Graphics.EndDraw();

#if ENABLE_VR
			// VR描画
			if( SharpKmyVr.Func.IsReady() ) {
				VrDrawer.DrawSimple( owner.getScreenAspect() );
			}
#endif  // #if ENABLE_VR
        }

        internal void Update()
        {
            switch (state)
            {
                case 0:
                    imgAlpha += 4 * GameMain.getRelativeParam60FPS();
#if TRIAL
                    if (imgAlpha >= 255)
#else
                    if (imgAlpha >= 255 || Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE))
#endif
                    {
                        imgAlpha = 255;
                        state = 1;
                    }
                    break;
                case 1:
                    wait += GameMain.getRelativeParam60FPS();
#if TRIAL
                    if (wait >= 60)
#else
                    if (wait >= 60 || Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE))
#endif
                    {
                        state = 2;
                    }
                    break;
                case 2:
                    screenAlpha += 4 * GameMain.getRelativeParam60FPS();
                    if (screenAlpha >= 255)
                    {
                        screenAlpha = 255;
                        state = 3;
                        Graphics.UnloadImage(logoImageId);
                        logoImageId = -1;

                        // 体験版でしか来ない第２ロゴ表示処理
                        if (logoImageId2 >= 0)
                        {
                            logoImageId = logoImageId2;
                            imgWidth = imgWidth2;
                            imgHeight = imgHeight2;
                            logoImageId2 = -1;
                            state = 0;
                            screenAlpha = 0;
                            wait = 0;
                            imgAlpha = 0;
                        }
                    }
                    break;
                case 3:
                    owner.ChangeScene(GameMain.Scenes.TITLE);
                    break;
            }
        }
    }
}

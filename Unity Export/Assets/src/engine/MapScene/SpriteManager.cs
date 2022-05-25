using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Yukar.Common.GameData;

namespace Yukar.Engine
{
    class SpriteManager
    {
        public const int SYSTEM_SPRITE_INDEX = 1000;
        public const int TELOP_SPRITE_BG = 1020;
        public const int TELOP_SPRITE_TEXT = 1021;
        public const int DIALOGUE_SPRITE_A = 1022;
        public const int DIALOGUE_SPRITE_B = 1023;

        public const int DIALOGUE_SPRITE_A_POS_X = 480 - 300;
        public const int DIALOGUE_SPRITE_A_POS_Y = 272;
        public const int DIALOGUE_SPRITE_B_POS_X = 480 + 300;
        public const int DIALOGUE_SPRITE_B_POS_Y = 272;
        public const int MAX_SPRITE = 1024;
        public MapScene owner;

        abstract class SpriteDef
        {
            internal int nowX;
            internal int nowY;
            internal float nowScaleX;
            internal float nowScaleY;
            internal Color nowColor;
            internal float nowFrame;
            internal bool visible;

            int startX;
            int startY;
            float startScaleX;
            float startScaleY;
            Color startColor;

            int endX;
            int endY;
            float endScaleX;
            float endScaleY;
            Color endColor;
            internal float endFrame;

            public Guid nowImageGuId;
            public int sizeX;
            public int sizeY;
            public bool centerBase;

            public int option;  // 表情タイプに使っています
            internal Color overrideColor = Color.White;

            internal void Update()
            {
                if (nowFrame == endFrame || !visible)
                    return;

                nowFrame += Math.Min(GameMain.getRelativeParam60FPS(), 3);

                if (nowFrame > endFrame)
                {
                    nowFrame = endFrame;
                }

                float delta = nowFrame / endFrame;
                float invert = 1 - delta;

                if (startX == endX)
                    nowX = endX;
                else
                    nowX = (int)(startX * invert + endX * delta );
                if (startY == endY)
                    nowY = endY;
                else
                    nowY = (int)(startY * invert + endY * delta );
                nowScaleX = startScaleX * invert + endScaleX * delta;
                nowScaleY = startScaleY * invert + endScaleY * delta;
                nowColor.R = (byte)(startColor.R * invert + endColor.R * delta);
                nowColor.G = (byte)(startColor.G * invert + endColor.G * delta);
                nowColor.B = (byte)(startColor.B * invert + endColor.B * delta);
                nowColor.A = (byte)(startColor.A * invert + endColor.A * delta);
            }

            internal void SetAnim(int x, int y, int scale, Color color, float frame)
            {
                visible = true;

                startX = nowX;
                startY = nowY;
                startScaleX = nowScaleX;
                startScaleY = nowScaleY;
                startColor = nowColor;
                nowFrame = 0;

                endX = x;
                endY = y;
                endScaleX = (float)scale / 100;
                endScaleY = endScaleX;
                endColor = color;
                endFrame = frame;

                // 適用時間が0の時はすぐに移動する
                if (endFrame == 0)
                {
                    endFrame = 0.01f;
                    Update();
                }
            }

            internal void SetAnim(int x, int y, int zoomX, int zoomY, Color color, float frame)
            {
                visible = true;

                startX = nowX;
                startY = nowY;
                startScaleX = nowScaleX;
                startScaleY = nowScaleY;
                startColor = nowColor;
                nowFrame = 0;

                endX = x;
                endY = y;
                endScaleX = (float)zoomX / 100;
                endScaleY = (float)zoomY / 100;
                endColor = color;
                endFrame = frame;

                // 適用時間が0の時はすぐに移動する
                if (endFrame == 0)
                {
                    endFrame = 0.01f;
                    Update();
                }
            }

            internal void SetAnim(int scale, Color color, float frame)
            {
                SetAnim(nowX, nowY, scale, color, frame);
            }

            internal void SetAnim(int zoomX, int zoomY, Color color, float frame)
            {
                SetAnim(nowX, nowY, zoomX, zoomY, color, frame);
            }

            internal void Show()
            {
                visible = true;
            }

            internal void Hide()
            {
                visible = false;
                endFrame = nowFrame;
            }

            internal abstract void Draw( int zOrder );

            internal void copyFrom(SpriteDef old)
            {
                nowX = old.nowX;
                nowY = old.nowY;
                nowScaleX = old.nowScaleX;
                nowScaleY = old.nowScaleY;
                nowColor = old.nowColor;
            }

            internal void SetPos(int x, int y)
            {
                nowX = x;
                nowY = y;
            }

            internal void SetScale(int zoom)
            {
                this.nowScaleX = (float)zoom / 100;
                this.nowScaleY = nowScaleX;
            }

            internal void SetColor(Color color)
            {
                this.nowColor = color;
            }

            internal virtual void Unload()
            {
            }

            internal void SetScale(int zoomX, int zoomY)
            {
                this.nowScaleX = (float)zoomX / 100;
                this.nowScaleY = (float)zoomY / 100;
            }

            internal Common.GameData.Sprite save(int i)
            {
                var result = new Common.GameData.Sprite();

                result.index = i;
                result.type = getSpriteType();
                result.faceType = option;
                result.visible = visible;

                if (result.type == Sprite.SpriteType.TEXT)
                    result.text = ((TextSprite)this).Text;

                // アニメ中の場合は、最終フレームの情報を保存する
                if (nowFrame < endFrame)
                {
                    result.x = endX;
                    result.y = endY;
                    result.zoomX = (int)(endScaleX * 100);
                    result.zoomY = (int)(endScaleY * 100);
                    result.guid = nowImageGuId;
                    result.align = (byte)(centerBase ? 1 : 0);
                    result.color = endColor;
                }
                else
                {
                    result.x = nowX;
                    result.y = nowY;
                    result.zoomX = (int)(nowScaleX * 100);
                    result.zoomY = (int)(nowScaleY * 100);
                    result.guid = nowImageGuId;
                    result.align = (byte)(centerBase ? 1 : 0);
                    result.color = nowColor;
                }

                if (result.type == Sprite.SpriteType.TEXT)
                    result.align = ((TextSprite)this).Align;

                return result;
            }

            internal abstract Sprite.SpriteType getSpriteType();
        }

        class PictureSprite : SpriteDef
        {
            int imgId;

            internal PictureSprite(int imgId, int width, int height)
            {
                this.imgId = imgId;
                this.sizeX = width;
                this.sizeY = height;
            }

			internal override void Draw(int zOrder)
            {
                int zoomedWidth = (int)(sizeX * nowScaleX);
                int zoomedHeight = (int)(sizeY * nowScaleY);
                if (centerBase)
                {
                    var destRect = new Rectangle(nowX - zoomedWidth / 2, nowY - zoomedHeight / 2, zoomedWidth, zoomedHeight);
                    var srcRect = new Rectangle(0, 0, sizeX, sizeY);
                    Graphics.DrawImage(imgId, destRect, srcRect, blend(nowColor, overrideColor), zOrder);
                }
                else
                {
                    var destRect = new Rectangle(
                        nowX - (zoomedWidth - sizeX) / 2,
                        nowY - (zoomedHeight - sizeY) / 2,
                        zoomedWidth, zoomedHeight);
                    var srcRect = new Rectangle(0, 0, sizeX, sizeY);
                    Graphics.DrawImage(imgId, destRect, srcRect, blend(nowColor, overrideColor), zOrder);
                }
            }

            private Color blend(Color nowColor, Color overrideColor)
            {
                return new Color(nowColor.ToVector4() * overrideColor.ToVector4());
            }

            internal override void Unload()
            {
                Graphics.UnloadImage(imgId);
            }

            internal override Sprite.SpriteType getSpriteType()
            {
                return Sprite.SpriteType.PICTURE;
            }
        }

        class TextSprite : SpriteDef
        {
            TextDrawer textDrawer;
            MessageWindow.MessageEntry entry;
            TextDrawer.HorizontalAlignment alignment;

            internal string Text
            {
                get
                {
                    return entry.text;
                }
            }

            internal byte Align
            {
                get
                {
                    return (byte)alignment;
                }

                set
                {
                    alignment = (TextDrawer.HorizontalAlignment)value;
                }
            }

            internal TextSprite(string text, TextDrawer.HorizontalAlignment alignment)
            {
                textDrawer = new TextDrawer(1);
                entry = new MessageWindow.MessageEntry(null);
                entry.text = text;
                entry.separateByCommands(1, Color.White);
                entry.time = short.MaxValue;
                this.alignment = alignment;
                
                sizeX = sizeY = 0;
                for (int i = 0; i < entry.lineCount; i++)
                {
                    var size = entry.measureString(textDrawer, i);
                    sizeY += (int)size.Y;
                    if (sizeX < size.X)
                        sizeX = (int)size.X;
                }
            }

            internal override void Draw(int zOrder)
            {
                int y = nowY;
                if (alignment == TextDrawer.HorizontalAlignment.Center)
                    y -= sizeY / 2;

                for (int i = 0; i < entry.lineCount; i++)
                {
                    var size = entry.measureString(textDrawer, i);
                    size *= nowScaleX;
                    int x = nowX;
                    if (alignment == TextDrawer.HorizontalAlignment.Center)
                        x -= (int)size.X / 2;
                    else if (alignment == TextDrawer.HorizontalAlignment.Right)
                        x -= (int)size.X;
                    entry.drawStringWithCommands(textDrawer, new Vector2(x, y), i, nowScaleX, (float)nowColor.A / 255);

                    y += (int)size.Y;
                }
            }

            internal override Sprite.SpriteType getSpriteType()
            {
                return Sprite.SpriteType.TEXT;
            }
        }

        class RectSprite : SpriteDef
        {
            private Color origColor;

            internal RectSprite(Color origColor)
            {
                this.origColor = origColor;
            }

            internal override void Draw(int zOrder)
            {
                int width = (int)(nowScaleX);
                int height = (int)(nowScaleY);
                Color c = new Color(
                    nowColor.R * origColor.R / 255,
                    nowColor.G * origColor.G / 255,
                    nowColor.B * origColor.B / 255,
                    nowColor.A * origColor.A / 255
                );
                Graphics.DrawFillRect(nowX - width / 2, nowY - height / 2,
                    width, height, c.R, c.G, c.B, c.A, zOrder);
            }

            internal override Sprite.SpriteType getSpriteType()
            {
                return Sprite.SpriteType.RECT;
            }
        }

        SpriteDef[] sprites = new SpriteDef[MAX_SPRITE];

        internal void Update()
        {
            foreach (var sprite in sprites)
            {
                if (sprite != null)
                    sprite.Update();
            }
        }

        internal void Update(int p)
        {
            if (sprites[p] != null)
                sprites[p].Update();
        }

        internal void Draw(int minIndex, int maxIndex)
        {
            foreach (var sprite in sprites.Skip(minIndex).Take(maxIndex - minIndex))
            {
                if (sprite != null && sprite.visible)
                    sprite.Draw(1);
            }
        }

        internal void Hide(int p)
        {
            if (sprites[p] != null)
                sprites[p].Hide();
        }

        internal void Move(int spIndex, int zoom, float waitFrame, Color color)
        {
            if (sprites[spIndex] != null)
                sprites[spIndex].SetAnim(zoom, color, waitFrame);
        }

        internal void Move(int spIndex, int zoomX, int zoomY, float waitFrame, Color color)
        {
            if (sprites[spIndex] != null)
                sprites[spIndex].SetAnim(zoomX, zoomY, color, waitFrame);
        }

        internal void Move(int spIndex, int zoom, float waitFrame, Color color, int x, int y)
        {
            if (sprites[spIndex] != null)
                sprites[spIndex].SetAnim(x, y, zoom, color, waitFrame);
        }

        internal void Move(int spIndex, int zoomX, int zoomY, float waitFrame, Color color, int x, int y)
        {
            if (sprites[spIndex] != null)
                sprites[spIndex].SetAnim(x, y, zoomX, zoomY, color, waitFrame);
        }

		internal bool GetPosition( int idx, out int x, out int y)
		{
			if (sprites[idx] != null)
			{
				x = sprites[idx].nowX + sprites[idx].sizeX / 2;
                y = sprites[idx].nowY + sprites[idx].sizeY / 2;
				return true;
			}
			x = y = 0;
			return false;
		}
        
        internal void ShowRect(int spIndex, int scaleX, int scaleY, Color rectColor, Color color, int x, int y)
        {
            var old = sprites[spIndex];
            var neo = new RectSprite(rectColor);

            if (old != null)
            {
                neo.copyFrom(old);
                old.Unload();
            }
            neo.Show();
            neo.SetScale(scaleX, scaleY);
            neo.SetColor(color);

            sprites[spIndex] = neo;
            sprites[spIndex].SetPos(x, y);
        }

        internal void ShowText(int spIndex, string p, int zoom, Color color, int textAlign)
        {
            var old = sprites[spIndex];
            var neo = old;
            var newText = GameMain.instance.replaceForFormat(p);

            // 同じものを表示しようとしていた場合、パラメータを再解釈しない
            if (old is TextSprite && ((TextSprite)old).Text == newText)
            {
                ((TextSprite)old).Align = (byte)textAlign;
            }
            else
            {
                neo = new TextSprite(newText, (TextDrawer.HorizontalAlignment)textAlign);
                if (old != null)
                {
                    neo.copyFrom(old);
                    old.Unload();
                }
            }

            neo.Show();
            neo.SetScale(zoom);
            neo.SetColor(color);

            sprites[spIndex] = neo;
        }

        internal void ShowText(int spIndex, string p, int zoom, Color color, int x, int y, int textAlign)
        {
            ShowText(spIndex, p, zoom, color, textAlign);
            sprites[spIndex].SetPos(x, y);
        }

        internal void ShowPicture(int spIndex, Guid guid, int zoom, Color color, int type, bool centerBase = true)
        {
            var old = sprites[spIndex];

            int imgId = -1;
            var rom = owner.owner.catalog.getItemFromGuid(guid);
            if (rom is Common.Resource.Sprite)
            {
                var pic = owner.owner.catalog.getItemFromGuid(guid) as Common.Resource.Sprite;
                if (pic != null)
                    imgId = Graphics.LoadImage(pic.path);
            }
            else
            {
                if (type < 0)
                    type = 0;
                var pic = owner.owner.catalog.getItemFromGuid(guid) as Common.Resource.Face;
                if (pic != null)
                    imgId = Graphics.LoadImage(pic.getFacePath((Common.Resource.Face.FaceType)type));
            }

            int width = 0, height = 0;
            if (imgId >= 0)
            {
                width = Graphics.GetImageWidth(imgId);
                height = Graphics.GetImageHeight(imgId);
            }
            var neo = new PictureSprite(imgId, width, height);
            neo.centerBase = centerBase;

            if (old != null)
            {
                neo.copyFrom(old);
                old.Unload();
            }
            neo.Show();
            neo.SetScale(zoom);
            neo.SetColor(color);
            neo.nowImageGuId = guid;
            neo.option = type;

            sprites[spIndex] = neo;
        }

        internal void ShowPicture(int spIndex, Guid guid, int zoom, Color color, int type, int x, int y, bool centerBase = true)
        {
            ShowPicture(spIndex, guid, zoom, color, type, centerBase);
            sprites[spIndex].SetPos(x, y);
        }

        internal void ShowPicture(int spIndex, Guid guid, int zoomX, int zoomY, Color color, int type, int x, int y, bool centerBase = true)
        {
            ShowPicture(spIndex, guid, zoomX, color, type, centerBase);
            sprites[spIndex].SetScale(zoomX, zoomY);
            sprites[spIndex].SetPos(x, y);
        }

        internal void ShowPicture(int spIndex, Guid guid, int zoomX, int zoomY, Color color, int type, bool centerBase = true)
        {
            ShowPicture(spIndex, guid, zoomX, color, type);
            sprites[spIndex].SetScale(zoomX, zoomY);
        }

        internal void Clear()
        {
            for (int i = 0; i < MAX_SPRITE; i++)
            {
                if (sprites[i] != null)
                    sprites[i].Unload();
                sprites[i] = null;
            }
        }

        internal bool isMoving(int spIndex)
        {
            if (sprites[spIndex] == null)
                return false;
            return sprites[spIndex].nowFrame < sprites[spIndex].endFrame;
        }

        internal Guid GetImageGuid(int p)
        {
            if (sprites[p] == null)
                return Guid.Empty;
            return sprites[p].nowImageGuId;
        }

        internal void GetSpSize(int p, out int spSizeX, out int spSizeY)
        {
            if (sprites[p] == null)
            {
                spSizeX = 0;
                spSizeY = 0;
                return;
            }
            spSizeX = sprites[p].sizeX;
            spSizeY = sprites[p].sizeY;
        }

        internal void GetScale(int p, out int scaleX, out int scaleY)
        {
            if (sprites[p] == null)
            {
                scaleX = 0;
                scaleY = 0;
                return;
            }
            scaleX = (int)(sprites[p].nowScaleX * 100);
            scaleY = (int)(sprites[p].nowScaleY * 100);
        }

        internal void SetPosition(int idx, int x, int y)
        {
            if (sprites[idx] != null)
            {
                sprites[idx].SetPos(x, y);
            }
        }

        internal bool isVisible(int spIndex)
        {
            if (sprites[spIndex] == null)
                return false;
            return sprites[spIndex].visible;
        }

        internal Color GetColor(int spIndex)
        {
            if (sprites[spIndex] == null)
                return Color.Transparent;
            return sprites[spIndex].nowColor;
        }

        internal List<Common.GameData.Sprite> save()
        {
            var result = new List<Common.GameData.Sprite>();
            for (int i = 0; i < SYSTEM_SPRITE_INDEX; i++)
            {
                if (sprites[i] != null)
                    result.Add(sprites[i].save(i));
            }
            return result;
        }

        internal void load(List<Common.GameData.Sprite> spriteStates)
        {
            foreach (var sp in spriteStates)
            {
                ShowSprite(sp);
            }
        }

        private SpriteDef ShowSprite(Common.GameData.Sprite sp)
        {
            SpriteDef result = null;

            switch (sp.type)
            {
                case Sprite.SpriteType.PICTURE:
                    ShowPicture(sp.index, sp.guid, sp.zoomX, sp.zoomY, sp.color, sp.faceType, sp.x, sp.y, sp.align > 0 ? true : false);
                    sprites[sp.index].visible = sp.visible;
                    break;
                case Sprite.SpriteType.TEXT:
                    ShowText(sp.index, sp.text, sp.zoomX, sp.color, sp.x, sp.y, sp.align);
                    sprites[sp.index].visible = sp.visible;
                    break;
            }

            return result;
        }

        internal void SetEffectTargetColor(int spIndex, Color color)
        {
            if (sprites[spIndex] == null)
                return;
            sprites[spIndex].overrideColor = color;
        }
    }
}

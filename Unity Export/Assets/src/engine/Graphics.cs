using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using Microsoft.Xna.Framework.Content;
using System.IO;
using System;

namespace Yukar.Engine
{
    public class Graphics
    {

        public static GraphicsCore sInstance;
        //static bool sDrawing = false;
        //public static SharpKmyGfx.Render spriteBatchScene;
        static bool initialized = false;

        static int sViewportWidth;
        static int sViewportHeight;
        static SharpKmyBase.StdResourceServer commonSvr;
        static SharpKmyBase.StdResourceServer svr;
		static bool sGfxErrorMsgBoxed = false;
        static bool sSndErrorMsgBoxed = false;
        static List<string> sAddedShaderPath = new List<string>();
        public static List<string> sAddedTexturePath = new List<string>();
        static string sCommonPath;

        public static int ScreenWidth
        {
            get
            {
                return sInstance.mWidth;
            }
        }
        public static int ScreenHeight
        {
            get
            {
                return sInstance.mHeight;
            }
        }

        public static int ViewportWidth
        {
            get
            {
                return sViewportWidth;
            }
        }

        public static int ViewportHeight
        {
            get
            {
                return sViewportHeight;
            }
        }

        public static GraphicsCore Initialize(string fontName = "", bool inIsInitializeForce = false, bool useGdi = false)
        {
            if (initialized && !inIsInitializeForce)
            {
                return sInstance;
            }

            initialized = true;
            sInstance = new GraphicsCore(fontName, useGdi);
            //sInstance.mGraphics = graphics;
            sInstance.mSpriteBatchMaster = sInstance.mSpriteBatch = new SharpKmyGfx.SpriteBatch();

#if WINDOWS
            sInstance.mWidth = 960;	//TODO
            sInstance.mHeight = 544;
#else
            sInstance.mWidth = 960;	//TODO
            sInstance.mHeight = 540;
#endif
            //sInstance.mWidth = graphics.Viewport.Width;
            //sInstance.mHeight = graphics.Viewport.Height;

            sViewportWidth = sInstance.mWidth;
            sViewportHeight = sInstance.mHeight;

            //sDrawing = false;

            //spriteBatchScene = new SharpKmyGfx.Render(SharpKmyGfx.Render.getDefaultRender());
            //spriteBatchScene.setClearMask(0);
            //SharpKmyMath.Matrix4 ortho = SharpKmyMath.Matrix4.ortho(0, sInstance.mWidth, 0, sInstance.mHeight, -1000, 1000);
            //spriteBatchScene.setViewMatrix(ortho, SharpKmyMath.Matrix4.identity());
            return sInstance;
        }

        public static void AddResourceDir(string prefix)
        {
            if (svr == null)
                return;
            prefix = Common.FileUtil.GetFullPath(prefix);
            sAddedShaderPath.Add(prefix + "shd");
            svr.addShaderPath(prefix + "shd");
            sAddedTexturePath.Add(prefix + "res\\character\\2D");
            svr.addTexturePath(prefix + "res\\character\\2D");
            searchTexturePath(prefix);
        }

        public static void SetCommonResourceDir(string prefix)
        {
            // 2回目以降は何の意味もない
            if (commonSvr == null)
            {
                sCommonPath = prefix;
                commonSvr = new SharpKmyBase.StdResourceServer();
                commonSvr.addShaderPath(prefix + "shd");
                commonSvr.addTexturePath(prefix + "res\\character\\2D");
                var dirs = Common.Util.file.getDirectories(prefix, "texture", SearchOption.AllDirectories);
                foreach (var dir in dirs)
                {
                    commonSvr.addTexturePath(dir);
                }
            }
        }

        public static void AddTextureDir(string path)
        {
            if (svr == null)
                return;
            sAddedTexturePath.Add(path);
            svr.addTexturePath(path);
        }

        public static void searchTexturePath(string prefix)
        {
            if (svr == null)
                return;
            var dirs = Common.Util.file.getDirectories(prefix, "texture", SearchOption.AllDirectories);
            foreach (var dir in dirs)
            {
                if (sAddedTexturePath.Contains(dir))
                    continue;

                sAddedTexturePath.Add(dir);
                svr.addTexturePath(dir);
            }
        }

        public static void ClearResourceServer()
        {
            sAddedTexturePath.Clear();
            sAddedShaderPath.Clear();
            if (svr != null)
                svr.Release();
            svr = new SharpKmyBase.StdResourceServer();
        }

        internal static void setCurrentResourceDir(string path, bool addTextureDirPrefix = true)
        {
            if (!addTextureDirPrefix)
            {
                if (path.StartsWith(sCommonPath))
                {
                    commonSvr.bringToFront(path);
                }
                else
                {
                    svr.bringToFront(path);
                }
                return;
            }

            if (path.StartsWith(sCommonPath))
            {
                commonSvr.bringToFront(path + Path.DirectorySeparatorChar + "texture");
            }
            else
            {
                svr.bringToFront(path + Path.DirectorySeparatorChar + "texture");
            }
        }

        public static void replaceSpriteBatch(SharpKmyGfx.SpriteBatch sp)
        {
            sInstance.mSpriteBatch = sp;
        }

        public static void revertSpriteBatch()
        {
            sInstance.mSpriteBatch = sInstance.mSpriteBatchMaster;
        }

        public static SharpKmyGfx.SpriteBatch getSpriteBatch()
        {
            return sInstance.mSpriteBatch;
        }

        public static void flushSpriteBatch(SharpKmyGfx.Render scene)
        {
            /*
            if (scene == null)
            {
                scene = spriteBatchScene;
            }
             */
            //DrawImage(sInstance.mFont.getTexture(), 0, 0, 500, 500);//フォントテクスチャのチェック用
            //scene.addDrawable(sInstance.mSpriteBatch);
        }

		public static void errorCheck(string module)
		{
			uint e = SharpKmy.Entry.getGfxErrorFlag();
			if (e != 0 && !sGfxErrorMsgBoxed)
			{
				sGfxErrorMsgBoxed = true;
#if WINDOWS
                System.Windows.Forms.MessageBox.Show(System.String.Format(Properties.Resources.str_Error_GraphicError, module));
#endif
			}

			e = SharpKmy.Entry.getSndErrorFlag();
			if (e != 0 && !sSndErrorMsgBoxed)
			{
                sSndErrorMsgBoxed = true;
#if WINDOWS
                System.Windows.Forms.MessageBox.Show(System.String.Format(Properties.Resources.str_Error_SoundError, module));
#endif
			}
		}

        public static void Destroy()
        {
            //spriteBatchScene.Release();
            svr.Release();
            sInstance.finalize();
            sInstance.mSpriteBatchMaster.Release();
            sInstance = null;
            initialized = false;
        }

        public static void BeginDraw()
        {
            //sDrawing = true;
            //sInstance.mSpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        }

        public static void EndDraw()
        {
            //sInstance.mSpriteBatch.End();
            //sDrawing = false;
        }

        public static void SetViewport(int x, int y, int width, int height)
        {
            if (sInstance == null) return;
            sInstance.mSpriteOffset.X = x;
            sInstance.mSpriteOffset.Y = y;
            //SharpKmyGfx.Render.getDefaultRender().setViewport(x, y, width, height);
            //spriteBatchScene.setViewport(x, y, width, height);
            //SharpKmyMath.Matrix4 ortho = SharpKmyMath.Matrix4.ortho(0, width, 0, height, -1000, 1000);
            //spriteBatchScene.setViewMatrix(ortho, SharpKmyMath.Matrix4.identity());

            sViewportWidth = width;
            sViewportHeight = height;
            /*
            if (sDrawing)
                sInstance.mSpriteBatch.End();

            if (x + width > sInstance.mWidth)
                width = sInstance.mWidth - x;

            if (y + height > sInstance.mHeight)
                height = sInstance.mHeight - y;

            var vp = new Viewport();
            vp.X = x;
            vp.Y = y;
            vp.Width = width;
            vp.Height = height;
            sInstance.mGraphics.Viewport = vp;

            if (sDrawing)
                sInstance.mSpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            */
        }

        internal static void RestoreViewport()
        {
            SetViewport(0, 0, ScreenWidth, ScreenHeight);
        }

        public static int LoadImage(string fileName, int gridX = 0, int gridY = 0)
        {
#if WINDOWS
            var stream = getFileStream(fileName);
            if (stream == null)
                return -1;
            var result = sInstance.LoadImage(stream, fileName, gridX, gridY);
            stream.Close();
            return result;
#else
            fileName = getAvailablePath(fileName);
            return sInstance.LoadImage(null, fileName, gridX, gridY);
#endif
        }

        public static Stream getFileStream(string fileName)
        {
            if (Common.Util.file.exists(fileName))
            {
                return Common.Util.getFileStream(fileName);
            }

            var userPath = Common.Catalog.sResourceDir + fileName;
            if (Common.Util.file.exists(userPath))
            {
                return Common.Util.getFileStream(userPath);
            }

            var commonPath = Common.Catalog.sCommonResourceDir + fileName;
            if (Common.Util.file.exists(commonPath))
            {
                return Common.Util.getFileStream(commonPath);
            }

            return null;
        }

        public static ModifiedModelInstance LoadModel(string path)
        {
            return sInstance.LoadModel(path);
        }


		public static void UnloadModel(ModifiedModelInstance inst)
        {
            sInstance.UnloadModel(inst);
        }

        public static SharpKmyGfx.ParticleInstance LoadParticle(string path)
        {
            return sInstance.LoadParticle(path);
        }

		public static void UnloadParticle(SharpKmyGfx.ParticleInstance inst)
        {
            sInstance.UnloadParticle(inst);
        }

        public static Color[] getTextureColorBuffer(int idx)
        {
            return sInstance.getTextureColorBuffer(idx);
        }

        public static void setTextureColorBuffer(int idx, Color[] buffer)
        {
            sInstance.setTextureColorBuffer(idx, buffer);
        }

        public static int LoadImageDiv(string fileName, int divX, int divY)
        {
#if WINDOWS
            var stream = getFileStream(fileName);
            if (stream == null)
                return -1;
            var result = sInstance.LoadImageDiv(fileName, stream, divX, divY);
            stream.Close();
            return result;
#else
            fileName = getAvailablePath(fileName);
            return sInstance.LoadImageDiv(fileName, null, divX, divY);
#endif
        }

        private static string getAvailablePath(string fileName)
        {
            if (Common.Util.file.exists(fileName))
            {
                return fileName;
            }

            var userPath = Common.Catalog.sResourceDir + fileName;
            if (Common.Util.file.exists(userPath))
            {
                return userPath;
            }

            var commonPath = Common.Catalog.sCommonResourceDir + fileName;
            if (Common.Util.file.exists(commonPath))
            {
                return commonPath;
            }

            return fileName;
        }

        public static int LoadImage(Stream stream, int gridX = 0, int gridY = 0)
        {
            return sInstance.LoadImage(stream, null, gridX, gridY);
        }
        /*
        public static int LoadFont(ContentManager contentManager, string fontName)
        {
            return sInstance.LoadFont(contentManager, fontName);
        }

        public static int LoadEffect(ContentManager contentManager, string effName)
        {
            return sInstance.LoadEffect(contentManager, effName);
        }
        */
        public static void UnloadImage(int imageId)
        {
            if (!sInstance.mTextureDictionary.ContainsKey(imageId))
                return;
            sInstance.UnloadImage(imageId);
        }

        public static void DrawImage(SharpKmyGfx.Texture t, int x, int y, int w, int h, int zOrder = 1)
        {
            sInstance.DrawImage(t, x, y, w, h, zOrder);
        }

#if ENABLE_VR
		public static void DrawImageVr(SharpKmyGfx.Texture t, int x, int y, int w, int h, int zOrder = 1)
		{
			sInstance.DrawImageVr(t, x, y, w, h, zOrder);
		}
#endif  // #if ENABLE_VR

        public static void DrawImage(int imageId, int x, int y, int zOrder = 1)
        {
            if (!sInstance.mTextureDictionary.ContainsKey(imageId))
                return;
            sInstance.DrawImage(imageId, x, y, zOrder);
        }

        public static void DrawImage(int imageId, int x, int y, Rectangle sourceRectangle, int zOrder = 1)
        {
            if (!sInstance.mTextureDictionary.ContainsKey(imageId))
                return;
            sInstance.DrawImage(imageId, x, y, sourceRectangle, zOrder);
        }

        public static void DrawImage(int imageId, Rectangle destinationRectangle, Rectangle sourceRectangle, int zOrder = 1)
        {
            if (!sInstance.mTextureDictionary.ContainsKey(imageId))
                return;
            sInstance.DrawImage(imageId, destinationRectangle, sourceRectangle, zOrder);
        }

        public static void DrawImage(int imageId, int x, int y, Color color, int zOrder = 1)
        {
            if (!sInstance.mTextureDictionary.ContainsKey(imageId))
                return;
            sInstance.DrawImage(imageId, x, y, color, zOrder);
        }

        public static void DrawImage(int imageId, int x, int y, Rectangle sourceRectangle, Color color, int zOrder = 1)
        {
            if (!sInstance.mTextureDictionary.ContainsKey(imageId))
                return;
            sInstance.DrawImage(imageId, x, y, sourceRectangle, color, zOrder);
        }

        public static void DrawImage(int imageId, Rectangle destinationRectangle, Rectangle sourceRectangle, Color color, int zOrder = 1)
        {
            if (!sInstance.mTextureDictionary.ContainsKey(imageId))
                return;
            sInstance.DrawImage(imageId, destinationRectangle, sourceRectangle, color, zOrder);
        }

        public static void DrawChipImage(int imageId, int x, int y, int srcChipX, int srcChipY, int zOrder = 1)
        {
            if (!sInstance.mTextureDictionary.ContainsKey(imageId))
                return;
            sInstance.DrawChipImage(imageId, x, y, srcChipX, srcChipY, zOrder);
        }

        public static void DrawChipImage(int imageId, int x, int y, int srcChipX, int srcChipY, byte r, byte g, byte b, byte a, int zOrder = 1)
        {
            if (!sInstance.mTextureDictionary.ContainsKey(imageId))
                return;
            sInstance.DrawChipImage(imageId, x, y, srcChipX, srcChipY, r, g, b, a, zOrder);
        }

        public static void DrawChipImage(int imageId, int x, int y, int destSizeX, int destSizeY, int srcChipX, int srcChipY, byte r, byte g, byte b, byte a, int zOrder = 1)
        {
            if (!sInstance.mTextureDictionary.ContainsKey(imageId))
                return;
            sInstance.DrawChipImage(imageId, x, y, destSizeX, destSizeY, srcChipX, srcChipY, r, g, b, a, zOrder);
        }

        public static void DrawChipImage(int imageId, int x, int y, int destSizeX, int destSizeY, int srcChipX, int srcChipY, byte r, byte g, byte b, byte a, int angle, int zOrder = 1)
        {
            if (!sInstance.mTextureDictionary.ContainsKey(imageId))
                return;
            sInstance.DrawChipImage(imageId, x, y, destSizeX, destSizeY, srcChipX, srcChipY, r, g, b, a, angle, zOrder);
        }

        public static void DrawChipImage(int imageId, int x, int y, int destSizeX, int destSizeY, int srcChipX, int srcChipY, byte r, byte g, byte b, byte a, int angle, int zOrder, int srcChipSizeW, int srcChipSizeH)
        {
            if (!sInstance.mTextureDictionary.ContainsKey(imageId))
                return;
            sInstance.DrawChipImage(imageId, x, y, destSizeX, destSizeY, srcChipX, srcChipY, r, g, b, a, angle, zOrder, srcChipSizeW, srcChipSizeH);
        }

        public static void DrawStringSoloColor(int fontId, string text, Vector2 position, Color color, int zOrder = 0)
        {
            DrawStringSoloColor(fontId, text, position, color, 1.0f, zOrder);
        }

        public static void DrawStringSoloColor(int fontId, string text, Vector2 position, Color color, float scale, int zOrder = 0)
        {
            sInstance.DrawStringSoloColor(fontId, text, position, color, scale, zOrder);
        }

        public static void DrawStringSoloColor(SharpKmyGfx.Font font, string text, Vector2 position, Color color, int zOrder = 0)
        {
            DrawStringSoloColor(font, text, position, color, 1.0f, zOrder);
        }

        public static void DrawStringSoloColor(SharpKmyGfx.Font font, string text, Vector2 position, Color color, float scale, int zOrder = 0)
        {
            sInstance.DrawStringSoloColor(font, text, position, color, scale, zOrder);
        }

        public static void DrawString(int fontId, string text, Vector2 position, Color color, int zOrder = 0)
        {
            DrawString(fontId, text, position, color, 1.0f, zOrder);
        }

        internal static void DrawString(int fontId, string text, Vector2 position, Color color, float scale, int zOrder = 0)
        {
            sInstance.DrawString(fontId, text, position, color, scale, zOrder);
        }

        internal static void DrawString(int fontId, string text, Vector2 position, Color color, float scale, int zOrder, bool bold, bool italic)
        {
            sInstance.DrawString(fontId, text, position, color, scale, zOrder, bold, italic);
        }

        public static void DrawString(SharpKmyGfx.Font font, string text, Vector2 position, Color color, int zOrder = 0)
        {
            DrawString(font, text, position, color, 1.0f, zOrder);
        }

        internal static void DrawString(SharpKmyGfx.Font font, string text, Vector2 position, Color color, float scale, int zOrder = 0)
        {
            sInstance.DrawString(font, text, position, color, scale, zOrder);
        }

        public static void setMatrix(Matrix projection, Matrix view, int zOrder = 0)
        {
            sInstance.setMatrix(projection, view);
        }

        public static void DrawLineRect(int x, int y, int w, int h, byte r, byte g, byte b, byte a, int zOrder = 1)
        {
            sInstance.DrawLineRect(x, y, w, h, r, g, b, a, zOrder);
        }

        public static void DrawTexturedRect(Vector3 center, Vector2 halfsize, int axis, Color color)
        {
            sInstance.DrawTexturedRect(center, halfsize, axis, color);
        }

        public static Vector2 MeasureString(int fontId, string text)
        {
            return sInstance.MeasureString(fontId, text);
        }

        public static Vector2 MeasureString(SharpKmyGfx.Font font, string text)
        {
            return sInstance.MeasureString(font, text);
        }

        public static int GetImageWidth(int id)
        {
            if (!sInstance.mTextureDictionary.ContainsKey(id))
                return int.MaxValue;
            return sInstance.mTextureDictionary[id].tex.getWidth();
        }

        public static int GetImageHeight(int id)
        {
            if (!sInstance.mTextureDictionary.ContainsKey(id))
                return int.MaxValue;
            return sInstance.mTextureDictionary[id].tex.getHeight();
        }

        public static void DrawRect(int x, int y, int w, int h, byte r, byte g, byte b, byte a, int zOrder = 1)
        {
            sInstance.DrawRect(x, y, w, h, r, g, b, a, zOrder);
        }

        public static void DrawFillRect(int x, int y, int w, int h, byte r, byte g, byte b, byte a, int zOrder = 1)
        {
            sInstance.DrawFillRect(x, y, w, h, r, g, b, a, zOrder);
        }

        public static int GetDivWidth(int id)
        {
            if (!sInstance.mTextureDictionary.ContainsKey(id))
                return 0;
            return sInstance.mTextureDictionary[id].grid.X;
        }

        public static int GetDivHeight(int id)
        {
            if (!sInstance.mTextureDictionary.ContainsKey(id))
                return 0;
            return sInstance.mTextureDictionary[id].grid.Y;
        }

        public static void SetGraphics(GraphicsCore core)
        {
            sInstance = core;
        }

        public static bool IsInitialized()
        {
            return initialized;
        }

        enum DEFREADMODE
        {
            kNONE,
            kANIM,
            kMTL,
        }

        internal static string getTexturePath(int imgId)
        {
            return sInstance.mTextureDictionary[imgId].tex.loadpath;
        }

        public static void refreshFont()
        {
            sInstance.refreshFont();
        }

#if WINDOWS
        public static System.Drawing.Bitmap getRenderCapture(SharpKmyGfx.Render render, int x, int y, int width, int height, bool usealpha)
		{
			var w = width;
			var h = height;
			uint[] buffer = new uint[w * h];
			render.getColor(x, y, w, h, buffer);

			System.Drawing.Bitmap img = new System.Drawing.Bitmap(w, h);
			for (int i = 0; i < h; i++)
			{
				for (int j = 0; j < w; j++)
				{
					uint v = buffer[i * w + j];
					uint a = (v >> 24) & 0xff;
					uint b = (v >> 16) & 0xff;
					uint g = (v >> 8) & 0xff;
					uint r = (v >> 0) & 0xff;
					System.Drawing.Color c = System.Drawing.Color.FromArgb(usealpha ? (int)a : 0xff, (int)r, (int)g, (int)b);
					img.SetPixel(j, h - 1 - i, c);
				}
			}

			return img;
		}
#endif
	}

    public class GraphicsCore
    {
        //internal GraphicsDevice mGraphics;
		internal SharpKmyGfx.SpriteBatch mSpriteBatchMaster;
        internal SharpKmyGfx.SpriteBatch mSpriteBatch;
		public SharpKmyGfx.Font mFont;
        internal int mWidth;
        internal int mHeight;

		const float BYTE_TO_NORMAL = 0.003921568f;

        internal class TextureDef
        {
            public int id;
            public int refCount;
            public Point grid;
            //public Texture2D tex;
			public SharpKmyGfx.Texture tex;
        }
        internal Dictionary<int, TextureDef> mTextureDictionary;
        internal Dictionary<string, int> mCacheDictionary;
        internal Dictionary<string, ModifiedModelData> mModelDictionary;
		internal Dictionary<string, SharpKmyGfx.ParticleRoot> mParticleDictionary;
        internal int mTextureCount = 1;//#24148 
        
		public Vector2 mSpriteOffset;


#if OLD
        Matrix sProj, sView;
#endif
        private bool useSystemFont;
        private string mGameFont;
        private SharpKmyGfx.Shader mNoTexShader = null;
        private bool useGdi;

        //BasicEffect sEffect;

        //internal List<SpriteFont> mFontList;
        //internal List<Effect> mEffectList;

        internal GraphicsCore(string fontName, bool useGdi)
        {
            mTextureDictionary = new Dictionary<int, TextureDef>();
            mCacheDictionary = new Dictionary<string,int>();
			mModelDictionary = new Dictionary<string, ModifiedModelData>();
			mParticleDictionary = new Dictionary<string, SharpKmyGfx.ParticleRoot>();

            mTextureCount = 1;//#24148 
            //mTempTargetPosition = new Vector2();
            //mTempColor = new Color();
            //mFontList = new List<SpriteFont>();
            //mEffectList = new List<Effect>();
            //sEffect = null;

            setGameFont(fontName, useGdi);
            mFont = createFont(24);
        }

        public void refreshFont()
        {
            mFont.Release();
            mFont = createFont(24);
        }

        public void setGameFont(string fontName, bool useGdi)
        {
            mGameFont = fontName;
            useSystemFont = !string.IsNullOrEmpty(fontName);
            this.useGdi = useGdi;
        }

        public SharpKmyGfx.Font createFont(int fontSize)
        {
            if (!useSystemFont)
            {
                string execDir = Common.BinaryReaderWrapper.getExecDir(true);
                string fontName = "font.ttf";
                string fontPath = execDir + fontName;
                if (Common.Util.file.exists(fontName))
                {
                    return new SharpKmyGfx.Font(fontName, fontSize, 1024, 1024);
                }
                else if (Common.Util.file.exists(fontPath))
                {
                    return new SharpKmyGfx.Font(fontPath, fontSize, 1024, 1024);
                }
            }
#if true
            if (mGameFont == "") { mGameFont = "メイリオ"; }
#endif
            if (useGdi)
                return SharpKmyGfx.Font.newSystemFontGdi(System.Text.Encoding.Unicode.GetBytes(mGameFont), (uint)fontSize, 1024, 1024);
            else
                return SharpKmyGfx.Font.newSystemFont(System.Text.Encoding.Unicode.GetBytes(mGameFont), (uint)fontSize, 1024, 1024);
        }

		internal void finalize()
		{
			mFont.Release();
			foreach( var i in mTextureDictionary)
			{
				i.Value.tex.Release();
			}
            if(mNoTexShader != null)
                mNoTexShader.Release();
		}

        internal int LoadImage(Stream stream, string path, int gridX, int gridY)
        {
            int id = -1;

            if (path != null && mCacheDictionary.ContainsKey(path))
            {
                id = mCacheDictionary[path];
                mTextureDictionary[id].refCount++;
                return id;
            }

#if WINDOWS
            var result = SharpKmyGfx.Texture.load(stream);
#else
            var result = SharpKmyGfx.Texture.load(path);
#endif
            if (result == null)
			{
				result = new SharpKmyGfx.Texture();
				result.create(32, 32);
			}

            if (result != null)
            {
                result.setWrap(SharpKmyGfx.WRAPTYPE.kCLAMP);

                id = mTextureCount++;

                TextureDef texDef = new TextureDef();
                texDef.id = id;
                texDef.grid = new Point(gridX, gridY);
                texDef.tex = result;
				texDef.refCount = 1;

                if(path != null)
                    mCacheDictionary.Add(path, id);
                mTextureDictionary.Add(id, texDef);
            }

            return id;
        }

        internal Color[] getTextureColorBuffer(int idx)
        {
            TextureDef td = mTextureDictionary[idx];
            if (td != null)
            {
                Color[] retval = new Color[td.tex.getWidth() * td.tex.getHeight()];
                //td.tex.GetData(retval);
                return retval;
            }
            return null;
        }

        internal void setTextureColorBuffer(int idx, Color[] buffer)
        {
            TextureDef td = mTextureDictionary[idx];
            if (td != null)
            {
                //td.tex.SetData(buffer);
            }
        }
        
        internal ModifiedModelInstance LoadModel( string path )
        {
            path = Common.FileUtil.GetFullPath(path);
            Graphics.setCurrentResourceDir(Common.Util.file.getDirName(path));

            if (!mModelDictionary.ContainsKey(path))
            {
                SharpKmyGfx.ModelData m = SharpKmyGfx.ModelData.load(path);
				if(m == null)return null;

				ModifiedModelData mmd = new ModifiedModelData();
				mmd.init(m, path);

                if (m != null)
                {
                    mModelDictionary.Add(path, mmd);
                }
                else{
                    return null;
                }
            }

			ModifiedModelData mdl = mModelDictionary[path];

            if (mdl != null)
            {
				var inst = new ModifiedModelInstance(mdl);

                // DrawInfoに各種デフォルト値を割り当てる
				var idx = 0;
				while (true)
				{
					SharpKmyGfx.DrawInfo di = inst.inst.getDrawInfo(idx);
					if (di == null) break;

                    // テクスチャのないモデルだったら、テクスチャを使用しないシェーダーにする
                    if (di.getTexture(0) == null)
                    {
                        if(mNoTexShader == null)
                            mNoTexShader = SharpKmyGfx.Shader.load("map_notex");
                        di.setShader(mNoTexShader);
                    }

                    idx++;
				}

                return inst;
            }
            return null;
        }

		internal void UnloadModel(ModifiedModelInstance inst)
        {
			ModifiedModelData mdl = inst.modifiedModel;
            inst.Release();
            if (mdl != null)
            {
                if (mdl.model.refcount == 0)
                {
                    mModelDictionary.Remove(mdl.model.path);
					mdl.Release();
					mdl.model.Release();
                }
            }
        }

		internal SharpKmyGfx.ParticleInstance LoadParticle(string path)
        {
            Graphics.setCurrentResourceDir(Common.Util.file.getDirName(path));

            if (!mParticleDictionary.ContainsKey(path))
            {
                SharpKmyGfx.ParticleRoot m = SharpKmyGfx.ParticleRoot.load(path);
				if (m != null)
				{
					mParticleDictionary.Add(path, m);
				}
				else
				{
					return null;
				}
			}

			SharpKmyGfx.ParticleRoot def = mParticleDictionary[path];

			if (def != null)
			{
				def.refcount++;
                return new SharpKmyGfx.ParticleInstance(def);
			}
			return null;
		}

		internal void UnloadParticle(SharpKmyGfx.ParticleInstance inst)
		{
			inst.basedef.refcount--;
			inst.Release();

			if (inst.basedef.refcount == 0 && inst.basedef.path != null)
            {
                inst.basedef.Release();
                mParticleDictionary.Remove(inst.basedef.path);
			}
		}

        internal int LoadImageDiv(string fileName, Stream stream, int divX, int divY)
        {
            int id = -1;

            if (fileName != null && mCacheDictionary.ContainsKey(fileName))
            {
                id = mCacheDictionary[fileName];
                mTextureDictionary[id].refCount++;
            }
            else
            {
#if WINDOWS
                var result = SharpKmyGfx.Texture.load(stream);
#else
                var result = SharpKmyGfx.Texture.load(fileName);
#endif
				if (result == null)
				{
					result = new SharpKmyGfx.Texture();
					result.create(32, 32);
				}
                
				if (result != null)
                {
                    result.setWrap(SharpKmyGfx.WRAPTYPE.kCLAMP);

                    id = mTextureCount++;

                    TextureDef texDef = new TextureDef();
                    texDef.id = id;
                    texDef.refCount = 1;
                    texDef.tex = result;
                    texDef.grid = new Point(result.getWidth() / divX, result.getHeight() / divY);

                    mTextureDictionary.Add(id, texDef);

                    if(fileName != null)
                        mCacheDictionary.Add(fileName, id);
                }

            }

            return id;
        }
/*
        internal int LoadFont(ContentManager contentManager, string fontName)
        {
            int fontID = mFontList.Count;
            var font = contentManager.Load<SpriteFont>(fontName);
            mFontList.Add(font);

            return fontID;
        }

        internal int LoadEffect(ContentManager contentManager, string fontName)
        {
            int effID = mEffectList.Count;
            var eff = contentManager.Load<Effect>(fontName);
            mEffectList.Add(eff);

            return effID;
        }
*/

        internal void UnloadImage(int imageId)
        {
            // 先にチェックしているので不要
            //if (mTextureDictionary.ContainsKey(imageId))
            //{
            mTextureDictionary[imageId].refCount--;

            if (mTextureDictionary[imageId].refCount == 0)
            {
                mTextureDictionary[imageId].tex.Release();
                mTextureDictionary.Remove(imageId);

                if (mCacheDictionary.ContainsValue(imageId))
                {
                    mCacheDictionary.Remove(mCacheDictionary.Single(pair => pair.Value == imageId).Key);
                }
            }
            //}
        }

        internal void setMatrix(Matrix projection, Matrix view)
        {
#if OLD
            sProj = projection;
            sView = view;
#endif
        }

		internal void DrawImage(SharpKmyGfx.Texture t, int x, int y, int w, int h, int zOrder)
		{
			mSpriteBatch.drawSprite(t,
				x + (int)mSpriteOffset.X, y + (int)mSpriteOffset.Y,
				0, 0,
				w, h, 0,
				0, 0,
				1, 1,
				1, 1, 1, 1, zOrder);
		}

#if ENABLE_VR
		internal void DrawImageVr(SharpKmyGfx.Texture t, int x, int y, int w, int h, int zOrder)
		{
			mSpriteBatch.drawSpriteVr(t,
				x + (int)mSpriteOffset.X, y + (int)mSpriteOffset.Y,
				0, 0,
				w, h, 0,
				0, 0,
				1, 1,
				1, 1, 1, 1, zOrder);
		}
#endif  // #if ENABLE_VR

		internal void DrawImage(int imageId, int x, int y, int zOrder)
        {
			SharpKmyGfx.Texture t = mTextureDictionary[imageId].tex;
			//int w = t.getWidth();
			//int h = t.getHeight();
			mSpriteBatch.drawSprite(t,
				x + (int)mSpriteOffset.X, y + (int)mSpriteOffset.Y, 
				0, 0,
				t.getWidth(), t.getHeight(), 0, 
				0, 0, 
				1, 1,
				1, 1, 1, 1, zOrder);
        }

		internal void DrawImage(int imageId, int x, int y, Rectangle sourceRectangle, int zOrder)
        {
			SharpKmyGfx.Texture t = mTextureDictionary[imageId].tex;

			int w = sourceRectangle.Width;
			int h = sourceRectangle.Height;
			mSpriteBatch.drawSprite(t,
				x + (int)mSpriteOffset.X, y + (int)mSpriteOffset.Y, 
				0,0,
				w, h, 
				0,
				sourceRectangle.Left / (float)t.getWidth(),
				sourceRectangle.Top / (float)t.getHeight(),
				sourceRectangle.Right / (float)t.getWidth(),
				sourceRectangle.Bottom / (float)t.getHeight(),
				1, 1, 1, 1, zOrder);
        }

		internal void DrawImage(int imageId, Rectangle destinationRectangle, Rectangle sourceRectangle, int zOrder)
        {
			SharpKmyGfx.Texture t = mTextureDictionary[imageId].tex;

			int w = destinationRectangle.Width;
			int h = destinationRectangle.Height;
			mSpriteBatch.drawSprite(t,
				destinationRectangle.Left + (int)mSpriteOffset.X,
				destinationRectangle.Top + (int)mSpriteOffset.Y,
				0,0,
				w,
				h,
				0,
				sourceRectangle.Left / (float)t.getWidth(),
				sourceRectangle.Top / (float)t.getHeight(),
				sourceRectangle.Right / (float)t.getWidth(),
				sourceRectangle.Bottom / (float)t.getHeight(),
				1, 1, 1, 1, zOrder);
		}

		internal void DrawImage(int imageId, int x, int y, Color color, int zOrder)
        {
			SharpKmyGfx.Texture t = mTextureDictionary[imageId].tex;
            var colorVec = color.ToVector4();

			int w = t.getWidth();
			int h = t.getHeight();
			mSpriteBatch.drawSprite(t,
				x + (int)mSpriteOffset.X, y + (int)mSpriteOffset.Y,
				0,0,
				w,h,
				0,
				0,0,
				1,1,
				colorVec.X, colorVec.Y, colorVec.Z, colorVec.W, zOrder);
		}

		internal void DrawImage(int imageId, int x, int y, Rectangle sourceRectangle, Color color, int zOrder)
        {
            SharpKmyGfx.Texture t = mTextureDictionary[imageId].tex;
            var colorVec = color.ToVector4();

			int w = sourceRectangle.Width;
			int h = sourceRectangle.Height;
			mSpriteBatch.drawSprite(t,
				x + (int)mSpriteOffset.X, y + (int)mSpriteOffset.Y,
				0, 0,
				w,h,
				0,
				sourceRectangle.Left / (float)t.getWidth(),
				sourceRectangle.Top / (float)t.getHeight(),
				sourceRectangle.Right / (float)t.getWidth(),
                sourceRectangle.Bottom / (float)t.getHeight(),
				colorVec.X, colorVec.Y, colorVec.Z, colorVec.W, zOrder);
        }

		internal void DrawImage(int imageId, Rectangle destinationRectangle, Rectangle sourceRectangle, Color color, int zOrder)
        {
            SharpKmyGfx.Texture t = mTextureDictionary[imageId].tex;
            var colorVec = color.ToVector4();

			int w = destinationRectangle.Width;
			int h = destinationRectangle.Height;
			mSpriteBatch.drawSprite(t,
				destinationRectangle.Left + (int)mSpriteOffset.X,
				destinationRectangle.Top + (int)mSpriteOffset.Y,
				0,0,
				w,h,
				0,
				sourceRectangle.Left / (float)t.getWidth(),
				sourceRectangle.Top / (float)t.getHeight(),
				sourceRectangle.Right / (float)t.getWidth(),
                sourceRectangle.Bottom / (float)t.getHeight(),
				colorVec.X, colorVec.Y, colorVec.Z, colorVec.W, zOrder);
        }

		internal void DrawChipImage(int imageId, int x, int y, int srcChipX, int srcChipY, int zOrder)
        {
            x += (int)mSpriteOffset.X;
            y += (int)mSpriteOffset.Y;

            TextureDef texDef = mTextureDictionary[imageId];

			float u0 = (float)(srcChipX * texDef.grid.X) / texDef.tex.getWidth();
			float v0 = (float)(srcChipY * texDef.grid.Y) / texDef.tex.getHeight();
			float u1 = (float)((srcChipX + 1) * texDef.grid.X) / texDef.tex.getWidth();
			float v1 = (float)((srcChipY + 1) * texDef.grid.Y) / texDef.tex.getHeight();
			//TODO 反転
			int w = texDef.grid.X;
			int h = texDef.grid.Y;
			mSpriteBatch.drawSprite(texDef.tex
				, x + w /2, y + h/2
				, - w / 2, - h /2
				, w, h
				, 0
				, u0, v0, u1, v1,
				1, 1, 1, 1, zOrder);
        }

        internal void DrawChipImage(int imageId, int x, int y, int srcChipX, int srcChipY, byte r, byte g, byte b, byte a, int zOrder)
        {
            x += (int)mSpriteOffset.X;
            y += (int)mSpriteOffset.Y;

            TextureDef texDef = mTextureDictionary[imageId];

			float u0 = (float)(srcChipX * texDef.grid.X) / texDef.tex.getWidth();
			float v0 = (float)(srcChipY * texDef.grid.Y) / texDef.tex.getHeight();
			float u1 = (float)((srcChipX + 1) * texDef.grid.X) / texDef.tex.getWidth();
			float v1 = (float)((srcChipY + 1) * texDef.grid.Y) / texDef.tex.getHeight();
			//TODO 反転
			int w = texDef.grid.X;
			int h = texDef.grid.Y;
			mSpriteBatch.drawSprite(texDef.tex
				, x + w / 2, y + h /2
				,-w / 2, -h / 2
				,w, h
				, 0
				, u0, v0, u1, v1
				, r * BYTE_TO_NORMAL, g * BYTE_TO_NORMAL, b * BYTE_TO_NORMAL, a * BYTE_TO_NORMAL, zOrder);
		}

		internal void DrawChipImage(int imageId, int x, int y, int destSizeX, int destSizeY, int srcChipX, int srcChipY, byte r, byte g, byte b, byte a, int zOrder)
        {
            x += (int)mSpriteOffset.X;
            y += (int)mSpriteOffset.Y;

            TextureDef texDef = mTextureDictionary[imageId];
			//TODO 反転
			float u0 = (float)(srcChipX * texDef.grid.X) / texDef.tex.getWidth();
			float v0 = (float)(srcChipY * texDef.grid.Y) / texDef.tex.getHeight();
			float u1 = (float)((srcChipX + 1) * texDef.grid.X) / texDef.tex.getWidth();
			float v1 = (float)((srcChipY + 1) * texDef.grid.Y) / texDef.tex.getHeight();
			//TODO 反転
			mSpriteBatch.drawSprite(texDef.tex 
				,x + destSizeX / 2, y + destSizeY / 2
				,-destSizeX / 2, -destSizeY / 2
				,destSizeX, destSizeY
				,0 
				,u0, v0, u1, v1
				, r * BYTE_TO_NORMAL, g * BYTE_TO_NORMAL, b * BYTE_TO_NORMAL, a * BYTE_TO_NORMAL, zOrder);
 		}

        internal void DrawChipImage(int imageId, int x, int y, int destSizeX, int destSizeY, int srcChipX, int srcChipY, byte r, byte g, byte b, byte a, int angle, int zOrder)
        {
            x += (int)mSpriteOffset.X;
            y += (int)mSpriteOffset.Y;

            TextureDef texDef = mTextureDictionary[imageId];

            float unit = 1.0f / texDef.tex.getWidth();

			float rot = (float)angle;
            float u0 = (float)(srcChipX * texDef.grid.X) / texDef.tex.getWidth() + unit;
            float v0 = (float)(srcChipY * texDef.grid.Y) / texDef.tex.getHeight() + unit;
            float u1 = (float)((srcChipX + 1) * texDef.grid.X) / texDef.tex.getWidth() - unit;
            float v1 = (float)((srcChipY + 1) * texDef.grid.Y) / texDef.tex.getHeight() - unit;
			//TODO 反転
			mSpriteBatch.drawSprite(texDef.tex
				, x + destSizeX / 2, y + destSizeY / 2
				,-destSizeX / 2, -destSizeY / 2 
				,destSizeX, destSizeY 
				,rot
				, u0, v0, u1, v1
				, r * BYTE_TO_NORMAL, g * BYTE_TO_NORMAL, b * BYTE_TO_NORMAL, a * BYTE_TO_NORMAL, zOrder);
		}

        internal void DrawChipImage(int imageId, int x, int y, int destSizeX, int destSizeY, int srcChipX, int srcChipY, byte r, byte g, byte b, byte a, int angle, int zOrder, int srcChipSizeW, int srcChipSizeH)
        {
            x += (int)mSpriteOffset.X;
            y += (int)mSpriteOffset.Y;

            TextureDef texDef = mTextureDictionary[imageId];

            float unit = 1.0f / texDef.tex.getWidth();

            float rot = (float)angle;
            float u0 = (float)(srcChipX * texDef.grid.X) / texDef.tex.getWidth() + unit;
            float v0 = (float)(srcChipY * texDef.grid.Y) / texDef.tex.getHeight() + unit;
            float u1 = (float)((srcChipX + srcChipSizeW) * texDef.grid.X) / texDef.tex.getWidth() - unit;
            float v1 = (float)((srcChipY + srcChipSizeH) * texDef.grid.Y) / texDef.tex.getHeight() - unit;
            //TODO 反転
            mSpriteBatch.drawSprite(texDef.tex
                , x + destSizeX / 2, y + destSizeY / 2
                , -destSizeX / 2, -destSizeY / 2
                , destSizeX, destSizeY
                , rot
                , u0, v0, u1, v1
                , r * BYTE_TO_NORMAL, g * BYTE_TO_NORMAL, b * BYTE_TO_NORMAL, a * BYTE_TO_NORMAL, zOrder);
        }

        internal void DrawLineRect(int x, int y, int w, int h, byte r, byte g, byte b, byte a, int zOrder)
        {
			mSpriteBatch.drawLineRect(x, y, w, h, 0, r / 255f, g / 255f, b / 255f, a / 255f, 1, zOrder);
        }

        internal void DrawTexturedRect(Vector3 center, Vector2 halfsize, int axis, Color color)
        {
#if OLD
            if (sEffect == null) sEffect = new BasicEffect(mGraphics);

            VertexPositionColor[] v = new VertexPositionColor[4]{
                new VertexPositionColor(center + new Vector3(-halfsize.X, 0,  halfsize.Y),color),
                new VertexPositionColor(center + new Vector3( halfsize.X, 0,  halfsize.Y),color),
                new VertexPositionColor(center + new Vector3( halfsize.X, 0, -halfsize.Y),color),
                new VertexPositionColor(center + new Vector3(-halfsize.X, 0, -halfsize.Y),color),
            };

            VertexBuffer vb = new VertexBuffer(mGraphics, typeof(VertexPositionColor), 4, BufferUsage.None);
            vb.SetData(v);

            Int16[] i = new Int16[8] { 0, 1, 1, 2, 2, 3, 3, 0 };
            IndexBuffer ib = new IndexBuffer(mGraphics, IndexElementSize.SixteenBits, 8, BufferUsage.None);
            ib.SetData(i);


            sEffect.Projection = sProj;
            sEffect.View = sView;
            sEffect.World = Matrix.Identity;

            foreach (EffectPass pass in sEffect.CurrentTechnique.Passes)
            {
                pass.Apply();

                Graphics.sInstance.mGraphics.SetVertexBuffer(vb);
                Graphics.sInstance.mGraphics.Indices = ib;
                Graphics.sInstance.mGraphics.DrawIndexedPrimitives(
                    Microsoft.Xna.Framework.Graphics.PrimitiveType.LineList,
                    0,
                    0,
                    4,
                    0,
                    4);
            }
#endif
        }

        internal void DrawStringSoloColor(int fontId, string text, Vector2 position, Color color, float scale, int zOrder)
        {
            DrawStringSoloColor(mFont, text, position, color, scale, zOrder);
        }

        internal void DrawStringSoloColor(SharpKmyGfx.Font font, string text, Vector2 position, Color color, float scale, int zOrder)
        {
            if (text == string.Empty) return;

            var bytes = System.Text.Encoding.UTF8.GetBytes(text);

            mSpriteBatch.drawText(font, bytes, (int)position.X + (int)mSpriteOffset.X, (int)position.Y + (int)mSpriteOffset.Y,
               color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f, scale, zOrder);
        }

        internal void DrawString(SharpKmyGfx.Font font, string text, Vector2 position, Color color, float scale, int zOrder)
        {
            // 空文字列を描画するとそのフレームの文字描画が全部豆腐になるので対策
            if (string.IsNullOrEmpty(text))
                return;

            // 縁取りの色を決める
            float hemmingColor = 0;
            float floatAlpha = 1.0f * color.A / 255;
            if (color.R + color.G + color.B <= color.A)
            {
                // 暗いので白
                hemmingColor = floatAlpha;
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(text);

            // 縁取り
            int[] offsX = { -1, -1, -1, 0, 0, 1, 1, 1 };
            int[] offsY = { -1, 0, 1, -1, 1, -1, 0, 1 };
            float[] alphaScale = { 0.25f, 1.0f, 0.25f, 1.0f, 1.0f, 0.25f, 1.0f, 0.25f };
            for (int i = 0; i < offsX.Length; i++)
            {
                mSpriteBatch.drawText(font, bytes,
                    (int)position.X + (int)mSpriteOffset.X + offsX[i],
                    (int)position.Y + (int)mSpriteOffset.Y + offsY[i],
                   hemmingColor * alphaScale[i] * floatAlpha,
                   hemmingColor * alphaScale[i] * floatAlpha,
                   hemmingColor * alphaScale[i] * floatAlpha,
                   floatAlpha * floatAlpha * floatAlpha * alphaScale[i], scale, zOrder);
            }

            // 本体
            mSpriteBatch.drawText(font, bytes, (int)position.X + (int)mSpriteOffset.X, (int)position.Y + (int)mSpriteOffset.Y,
               color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f, scale, zOrder);
        }

        internal void DrawString(int fontId, string text, Vector2 position, Color color, float scale, int zOrder, bool bold, bool italic)
        {
            var font = mFont;

            // 空文字列を描画するとそのフレームの文字描画が全部豆腐になるので対策
            if (string.IsNullOrEmpty(text))
                return;

            // 縁取りの色を決める
            float hemmingColor = 0;
            float floatAlpha = 1.0f * color.A / 255;
            if (color.R + color.G + color.B <= color.A)
            {
                // 暗いので白
                hemmingColor = floatAlpha;
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(text);

            if (!bold)
            {
                // 縁取り
                int[] offsX = { -1, -1, -1, 0, 0, 1, 1, 1 };
                int[] offsY = { -1, 0, 1, -1, 1, -1, 0, 1 };
                float[] alphaScale = { 0.25f, 1.0f, 0.25f, 1.0f, 1.0f, 0.25f, 1.0f, 0.25f };
                for (int i = 0; i < offsX.Length; i++)
                {
                    mSpriteBatch.drawText(font, bytes,
                        (int)position.X + (int)mSpriteOffset.X + offsX[i],
                        (int)position.Y + (int)mSpriteOffset.Y + offsY[i],
                       hemmingColor * alphaScale[i] * floatAlpha,
                       hemmingColor * alphaScale[i] * floatAlpha,
                       hemmingColor * alphaScale[i] * floatAlpha,
                       floatAlpha * floatAlpha * floatAlpha * alphaScale[i], scale, zOrder, italic);
                }

                // 本体
                mSpriteBatch.drawText(font, bytes, (int)position.X + (int)mSpriteOffset.X, (int)position.Y + (int)mSpriteOffset.Y,
                   color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f, scale, zOrder, italic);
            }
            else
            {
                // 縁取り
                int[] offsX = { -1, -1, -1, 0, 0, 2, 2, 3, 3, 3 };
                int[] offsY = { -1, 0, 1, -1, 1, -1, 1, -1, 0, 1 };
                float[] alphaScale = { 0.25f, 1.0f, 0.25f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0.25f, 1.0f, 0.25f };
                for (int i = 0; i < offsX.Length; i++)
                {
                    mSpriteBatch.drawText(font, bytes,
                        (int)position.X + (int)mSpriteOffset.X + offsX[i],
                        (int)position.Y + (int)mSpriteOffset.Y + offsY[i],
                       hemmingColor * alphaScale[i] * floatAlpha,
                       hemmingColor * alphaScale[i] * floatAlpha,
                       hemmingColor * alphaScale[i] * floatAlpha,
                       floatAlpha * floatAlpha * floatAlpha * alphaScale[i], scale, zOrder, italic);
                }

                // 本体
                mSpriteBatch.drawText(font, bytes, (int)position.X + (int)mSpriteOffset.X, (int)position.Y + (int)mSpriteOffset.Y,
                   color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f, scale, zOrder, italic);

                mSpriteBatch.drawText(font, bytes, (int)position.X + (int)mSpriteOffset.X + 1, (int)position.Y + (int)mSpriteOffset.Y,
                   color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f, scale, zOrder, italic);

                mSpriteBatch.drawText(font, bytes, (int)position.X + (int)mSpriteOffset.X + 2, (int)position.Y + (int)mSpriteOffset.Y,
                   color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f, scale, zOrder, italic);
            }
        }

        internal void DrawString(int fontId, string text, Vector2 position, Color color, float scale, int zOrder)
        {
            DrawString(mFont, text, position, color, scale, zOrder);
        }

        internal Vector2 MeasureString(SharpKmyGfx.Font font, string text)
        {
            // 空文字列をはかると落ちるので修正
            if (string.IsNullOrEmpty(text))
                return Vector2.Zero;

            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            SharpKmyMath.Vector2 sz = font.measureString(bytes);
			return new Vector2(sz.x, sz.y);
        }

        internal Vector2 MeasureString(int fontId, string text)
        {
            return MeasureString(mFont, text);
        }

        internal void DrawFillRect(int x, int y, int w, int h, byte r, byte g, byte b, byte a, int zOrder)
        {
            x += (int)mSpriteOffset.X;
            y += (int)mSpriteOffset.Y;

			mSpriteBatch.drawRect(x, y, w, h, 0, r / 255f, g / 255f, b / 255f, a / 255f, 1, zOrder);
        }

		internal void DrawRect(int x, int y, int w, int h, byte r, byte g, byte b, byte a, int zOrder)
        {
            DrawFillRect(x, y, 1, h, r, g, b, a, zOrder);                 // 左
			DrawFillRect(x + 1, y, w - 2, 1, r, g, b, a, zOrder);          // 上
			DrawFillRect(x + w - 1, y, 1, h, r, g, b, a, zOrder);          // 右
			DrawFillRect(x + 1, y + h - 1, w - 2, 1, r, g, b, a, zOrder);  // 下
        }
    }
}

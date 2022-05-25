using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SharpKmyGfx
{
    public enum WRAPTYPE
    {
        kCLAMP,
    }

    public enum TEXTUREFILTER
    {
        kNEAREST,
    }

    public enum TEXTUREFFORMAT
    {
        kRGBA,
    }

    public class Texture
    {
        static Dictionary<IntPtr, int> sTextureRefDic = new Dictionary<IntPtr, int>();
        Texture2D mObj = null;
        internal Texture2D obj { get { return this.mObj; } }
        internal string loadpath = "";

        bool mIsAsset = false;
        IntPtr mRefUniqId = IntPtr.Zero;

        private void Ref()
        {
            var ptr = this.mObj.GetNativeTexturePtr();
            if (this.mRefUniqId != ptr)
            {
                this.Unref();
                this.mRefUniqId = ptr;
            }

            if (sTextureRefDic.ContainsKey(ptr))
            {
                sTextureRefDic[ptr]++;
            }
            else
            {
                sTextureRefDic[ptr] = 1;
            }
        }

        private bool Unref()
        {
            if (this.mObj == null) return false;
            if (this.mRefUniqId == IntPtr.Zero) return false;
            var ptr = this.mRefUniqId;
            if (sTextureRefDic.ContainsKey(ptr) == false) return false;
            sTextureRefDic[ptr]--;
            if (0 < sTextureRefDic[ptr])return false;
            sTextureRefDic.Remove(ptr);

            if (UnityEntry.IsImportMapScene() == false)
            {
                if (this.mIsAsset) Resources.UnloadAsset(this.mObj);
                else MonoBehaviour.Destroy(this.mObj);
            }

            this.mObj = null;
            this.mRefUniqId = IntPtr.Zero;
            return true;
        }

        internal void Release()
        {
            this.Unref();
            this.mObj = null;
            this.mRefUniqId = IntPtr.Zero;
        }

        internal static Texture loadFromResourceServer(string name)
        {
            string fullPath = "";
            List<string> DirList = Yukar.Engine.Graphics.sAddedTexturePath;
            foreach (string dir in DirList)
            {
                if (File.Exists(dir + "/" + name))
                {
                    fullPath = dir + "/" + name;
                    return load(fullPath);
                }
            }
            return null;
        }

        internal static Texture load(string path)
        {
            var result = new Texture();
            result.mObj = createTexture2DFromPath(path, ref result.mIsAsset);
            result.Ref();
            return result;
        }

        private static Texture2D createTexture2DFromPath(string path, ref bool outIsAsset)
        {
            path = Yukar.Common.UnityUtil.pathConvertToUnityResource(path);
            Texture2D result = null;

            outIsAsset = false;
            if (Yukar.Common.FileUtil.language != null)
            {
                var appendPath = Yukar.Common.FileUtil.language + "/" + path;
                result = Resources.Load<Texture2D>(appendPath);
                outIsAsset = true;
            }

            if (result == null)
            {
                result = Resources.Load<Texture2D>(path);
                outIsAsset = true;
            }

            if (result == null)
            {
                Debug.Log("Texture data " + path + " is not found.");
                result = new Texture2D(1, 1);
                outIsAsset = false;
            }
            return result;
        }

        internal void setWrap(WRAPTYPE wrapType)
        {
            // Dummy
        }

        internal void setMagFilter(object kNEAREST)
        {
            if(this.mObj != null)
                this.mObj.filterMode = FilterMode.Point;
        }

        internal int getHeight()
        {
            return this.mObj.height;
        }

        internal int getWidth()
        {
            return this.mObj.width;
        }

        internal static void getColor(string path, uint[] colors)
        {
            var tex = load(path);
            var pix = tex.obj.GetPixels32();
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = ((uint)pix[i].r << 24) | ((uint)pix[i].g << 16) | ((uint)pix[i].b << 8) | ((uint)pix[i].a);
            }
            tex.Release();
        }

        internal void create(int width, int height)
        {
            this.Unref();
            this.mObj = new Texture2D(width, height, TextureFormat.ARGB32, false);
            this.mIsAsset = false;
            this.Ref();
        }

        internal void storeSubPixel2D(int x, int y, int w, int h, uint[] pix, TEXTUREFFORMAT format, int sx, int sy, int swidth)
        {
            if (this.mObj == false) return;
            
            var colors = new UnityEngine.Color32[w * h];

            for (int _y = 0; _y < h; _y++)
            {
                for (int _x = 0; _x < w; _x++)
                {
                    var color = pix[(_y + sy) * swidth + (sx + _x)];
                    byte r = (byte)((color >> 24) & 0xFF);
                    byte g = (byte)((color >> 16) & 0xFF);
                    byte b = (byte)((color >> 8) & 0xFF);
                    byte a = (byte)((color >> 0) & 0xFF);
                    if (a == 0)
                        colors[_y * w + _x] = new UnityEngine.Color32(0, 0, 0, 0);
                    else
                        colors[_y * w + _x] = new UnityEngine.Color32(r, g, b, a);
                }
            }

            this.mObj.SetPixels32(x, y, w, h, colors);
            this.mObj.Apply();
        }

        internal static Texture load(Stream stream)
        {
            var result = new Texture();
            result.mObj = new Texture2D(1, 1);
            var bin = new byte[stream.Length];
            stream.Read(bin, 0, bin.Length);
            result.mObj.LoadImage(bin);
            result.mIsAsset = false;
            result.Ref();
            return result;
        }
    }
}
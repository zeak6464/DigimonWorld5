using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;

namespace Yukar.Engine
{
	public class MapBillboard : SharpKmyGfx.Drawable
    {
        private const float BIL_ADJUST_LIMIT = 3.0f;

        //SharpKmyGfx.VertexBuffer vb;
        SharpKmyGfx.VertexPositionTextureColor[] vertices;
		SharpKmyGfx.Shader legacytex;

        int divX;
        public int divY;
        int iX;
        int iY;
        public Vector2 sz;
		public SharpKmyMath.Vector3 mOrigin = new SharpKmyMath.Vector3(0, 0, 0);
		SharpKmyGfx.Texture mTex;
		public int layer;
		SharpKmyGfx.DrawInfo di;
        public SharpKmyGfx.Color color = SharpKmyGfx.Color.White;
        public SharpKmyGfx.BLENDTYPE blend = SharpKmyGfx.BLENDTYPE.kOPAQUE;
        private float pixelunit;
        internal int loopCount;

        public MapBillboard(string path, int divX, int divY, float scale = 1.0f)
        {
            pixelunit = 48f / scale;

            changeGraphic(path, divX, divY);

            vertices = new SharpKmyGfx.VertexPositionTextureColor[6];
            legacytex = SharpKmyGfx.Shader.load("map_nolit");
            layer = -1;
            di = new SharpKmyGfx.DrawInfo();
            loopCount = 0;
        }

        internal void changeGraphic(string path, int divX, int divY)
        {
            if (mTex != null)
                mTex.Release();

            Graphics.setCurrentResourceDir(Common.Util.file.getDirName(path), false);
            mTex = SharpKmyGfx.Texture.loadFromResourceServer(Common.Util.file.getFileNameWithoutExtension(path));
            if (mTex == null)
            {
                mTex = SharpKmyGfx.Texture.load(path);
                if (mTex == null)
                    return;
            }

            mTex.setWrap(SharpKmyGfx.WRAPTYPE.kCLAMP);
            mTex.setMagFilter(SharpKmyGfx.TEXTUREFILTER.kNEAREST);
            this.divX = divX;
            this.divY = divY;
            float h = mTex.getHeight();
            float w = mTex.getWidth();
            sz = new Vector2((w / divX) / pixelunit, (h / divY) / pixelunit);
            iX = 0;
            loopCount = 0;
        }

        public void finalize()
        {
			base.Release();
            //Graphics.UnloadImage(imgid);
			if(mTex != null)mTex.Release();
			if(legacytex != null)legacytex.Release();
            //imgid = -1;
			//if(vb != null)vb.Release();
			if(di != null)di.Release();
        }

        public void setIndex(int iX, int iY)
        {
            this.iX = iX;
            this.iY = iY;
        }

		//public void draw(SharpKmyMath.Matrix4 proj, SharpKmyMath.Matrix4 view, SharpKmyMath.Matrix4 iview, SharpKmyMath.Vector3 pos)
		public override void draw(SharpKmyGfx.Render scn)
        {
			//視点によってジオメトリを変化させるケース。
			//シャドウ用のシーンで呼ばれるケースもあるので、
			//頂点バッファは共有のにした方がよい
            if (mTex == null) return;

			SharpKmyMath.Matrix4 vm = new SharpKmyMath.Matrix4();
			SharpKmyMath.Matrix4 pm = new SharpKmyMath.Matrix4();
			scn.getViewMatrix( ref pm, ref vm);
			SharpKmyMath.Matrix4 ivm = SharpKmyMath.Matrix4.inverse(vm);

			
			di.setShader(legacytex);
			di.setTexture(0, mTex);
			di.setLayer(layer);

#if WINDOWS
			SharpKmyMath.Vector3 rightvec = new SharpKmyMath.Vector3(ivm.m00, ivm.m01, ivm.m02);
            SharpKmyMath.Vector3 upvec = new SharpKmyMath.Vector3(ivm.m10, ivm.m11, ivm.m12);
            float mUpHeight = sz.Y - 0.5f;
            float mBtmHeight = 0.5f;
#else
            SharpKmyMath.Vector3 rightvec = new SharpKmyMath.Vector3(-ivm.m00, ivm.m01, -ivm.m02);
            SharpKmyMath.Vector3 upvec = new SharpKmyMath.Vector3(-ivm.m10, ivm.m11, -ivm.m12);
            float mUpHeight = sz.Y - 0.5f;
            float mBtmHeight = 0.5f;
#endif
            float mHalfWidth = sz.X * 0.5f;
			SharpKmyMath.Vector3 mDrawOrigin = mOrigin + new SharpKmyMath.Vector3(0, mBtmHeight, 0);
            
            var p1 = -rightvec * mHalfWidth + upvec * mUpHeight + mDrawOrigin;
            var p2 = rightvec * mHalfWidth + upvec * mUpHeight + mDrawOrigin;
            var p3 = -rightvec * mHalfWidth - upvec * mBtmHeight + mDrawOrigin;
            var p4 = rightvec * mHalfWidth - upvec * mBtmHeight + mDrawOrigin;

            // エンジン専用の頂点補正(壁に埋まらないようにする)
            if (!MapData.sInstance.isEditor)
            {
                SharpKmyMath.Vector3 campos = ivm.translation();
#if !WINDOWS
            campos.x *= -1;
#endif

                SharpKmyMath.Vector3 lowerCenter = mOrigin;
                SharpKmyMath.Vector3 upperCenter = mDrawOrigin + upvec * mUpHeight;

                //カメラ->スプライト上辺中心(XZ成分)
                var dirXZ = new SharpKmyMath.Vector3((upperCenter - campos).x, 0, (upperCenter - campos).z);
                var dirXZlen = dirXZ.length();

                //カメラ->スプライト可変中心(XZ成分)
                //var camposXZ = new SharpKmyMath.Vector3(campos.x, 0, campos.z);
                //var screenXZ = new SharpKmyMath.Vector3(lowerCenter.x, 0, lowerCenter.z);
                var cameraDist = campos - lowerCenter;
                var screenDist = cameraDist;
                screenDist.y = 0;

                float scale = screenDist.length() / dirXZlen;

                //p1 = p1 * scale + campos * (1 - scale);
                //p2 = p2 * scale + campos * (1 - scale);

                //Console.WriteLine(scale);

                // スケールが1を超える場合は1に戻す
                if (scale > 1.0f)
                    scale = 1.0f;
                // 補正結果が一定の値を超える場合は歪みが大きすぎるのでリミットをかける
                else if ((1.0 - scale) * cameraDist.length() > BIL_ADJUST_LIMIT)
                    scale = 1.0f - BIL_ADJUST_LIMIT / cameraDist.length();

                p1 -= (p1 - campos) * (1 - scale);
                p2 -= (p2 - campos) * (1 - scale);
            }

            SharpKmyMath.Vector3[] positions = new SharpKmyMath.Vector3[6]{
                p1, p2, p3,
                p3, p2, p4,
            };

            Vector2 tip = new Vector2(1f / (float)divX, 1f / (float)divY);

            // 奇数枚数のテクスチャだと無理数のせいで左端に前のコマの右端が見えてしまう事があるので、オフセットを算出して足す
            var offsetX = 1f / mTex.getWidth() / 10;
            int iX = divX <= this.iX ? divX - 1 : this.iX;
            int iY = divY <= this.iY ? divY - 1 : this.iY;

            Vector2[] tc = new Vector2[6]{
                new Vector2(tip.X * iX + offsetX, tip.Y * iY),
                new Vector2(tip.X * (iX+1), tip.Y * iY),
                new Vector2(tip.X * iX + offsetX, tip.Y * (iY+1)),

                new Vector2(tip.X * iX + offsetX, tip.Y * (iY+1)),
                new Vector2(tip.X * (iX+1), tip.Y * iY),
                new Vector2(tip.X * (iX+1), tip.Y * (iY+1)),
            };

            for (int i = 0; i < 6; i++)
            {
				vertices[i].pos = positions[i];
                vertices[i].tc = new SharpKmyMath.Vector2(tc[i].X, 1f-tc[i].Y);
				vertices[i].color = new SharpKmyGfx.Color(1,1,1,1);
            }

            MapData.setFogParam(di);

            //vb.setData(vertices);
            di.setColor(color);
			di.setBlend(blend);
			di.setVolatileVertex(vertices);
			di.setIndexCount(6);
			//di.setVertexBuffer(vertices.Length);
			scn.draw(di);
			
        }

        public bool isHit(SharpKmyMath.Vector3 inPos)
        {
            return this.isHit(inPos, new SharpKmyMath.Vector3(-1));
        }

        public bool isHit(SharpKmyMath.Vector3 inPos, SharpKmyMath.Vector3 inMaxSize)
        {
#if WINDOWS
            return false;
#else
            var ltPos = this.vertices[2].pos;
            var rbPos = this.vertices[1].pos;

            var diff = rbPos - ltPos;
            for(int i1 = 0; i1 < 3; ++i1)
            {
                if (diff[i1] < inMaxSize[i1]) continue;
                var over = diff[i1] - inMaxSize[i1];
                over /= 2;
                ltPos.set(i1, ltPos[i1] + over);
                rbPos.set(i1, rbPos[i1] - over);
            }
            
            var screenPosLT = UnityEngine.Camera.main.WorldToScreenPoint(new UnityEngine.Vector3(-ltPos.x, ltPos.y, ltPos.z));
            var screenPosRB = UnityEngine.Camera.main.WorldToScreenPoint(new UnityEngine.Vector3(-rbPos.x, rbPos.y, rbPos.z));
            if (screenPosLT.x < inPos.x && inPos.x < screenPosRB.x
            && screenPosLT.y < inPos.y && inPos.y < screenPosRB.y)
            {
                return true;
            }
            return false;
#endif
        }
    }
}

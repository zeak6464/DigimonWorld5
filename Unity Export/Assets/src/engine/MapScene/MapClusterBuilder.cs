using System;
using System.Collections.Generic;
using Yukar.Common.Rom;

namespace Yukar.Engine
{
    public class MapClusterBuilder
    {
		const int EDGE_NONE = 0;
		const int EDGE_FAR = 1;
		const int EDGE_NEAR = 2;
		const int EDGE_LEFT = 4;
		const int EDGE_RIGHT = 8;

		const int EDGE_LEFT_NEAR = EDGE_LEFT | EDGE_NEAR;
		const int EDGE_LEFT_FAR = EDGE_LEFT | EDGE_FAR;

		const int EDGE_RIGHT_NEAR = EDGE_RIGHT | EDGE_NEAR;
		const int EDGE_RIGHT_FAR = EDGE_RIGHT | EDGE_FAR;

		const int EDGE_LEFT_RIGHT = EDGE_RIGHT | EDGE_LEFT;
		const int EDGE_FAR_NEAR = EDGE_FAR | EDGE_NEAR;

		const int EDGE_NEAR_CAPE = EDGE_LEFT | EDGE_RIGHT | EDGE_NEAR;
		const int EDGE_FAR_CAPE = EDGE_LEFT | EDGE_RIGHT | EDGE_FAR;
		const int EDGE_LEFT_CAPE = EDGE_LEFT | EDGE_FAR | EDGE_NEAR;
		const int EDGE_RIGHT_CAPE = EDGE_RIGHT | EDGE_FAR | EDGE_NEAR;

		const int EDGE_ISLAND = EDGE_LEFT | EDGE_RIGHT | EDGE_FAR | EDGE_NEAR;

        public const float s_centerOffset = 0.5f;

		public const float WATER_TOP_OFFST = 0.1f;

        //ビルド時のリスト
		List<SharpKmyGfx.VertexPositionNormalTexture2Color> op_vlist;

        //描画時のバッファ配列
        MapData owner;

        public Common.Rom.Map data;
        public int ix, iz;
        public int currentX, currentZ, currentY, currentHeight;
		public bool[] capped = new bool[4];

        //public static float seaofs = -0.025f;

		public SharpKmyGfx.VertexBuffer vb, avb;

		bool geomInvalid = true;
        private int bgChipIndex = -1;

        public MapClusterBuilder(int x, int z, MapData owner, Common.Rom.Map dat):base()
        {
            data = dat;

            ix = x;
            iz = z;

            this.owner = owner;

			vb = new SharpKmyGfx.VertexBuffer();
			avb = new SharpKmyGfx.VertexBuffer();

			//キャッシュがあればそれを使う
			var c = dat.getCache(ix, iz);
			if(c != null){
				op_vlist = c;
                vb.setData(op_vlist);
                geomInvalid = false;
            }
            else
			{
				op_vlist = new List<SharpKmyGfx.VertexPositionNormalTexture2Color>();
			}
        }

		public void Dispose()
		{
			vb.Release();
			avb.Release();
            op_vlist = null;
		}

		public void geomInvalidate()
		{
			geomInvalid = true;
		}

		bool getWorldPosition(int localx, int localz, ref int wx, ref int wz, bool strict = false)
		{
			wx = (int)ix * (int)Yukar.Engine.MapData.TIPS_IN_CLUSTER + localx;
			wz = (int)iz * (int)Yukar.Engine.MapData.TIPS_IN_CLUSTER + localz;

            if (!strict && owner.mapRom.outsideType == Map.OutsideType.REPEAT)
            {
                if (wx < 0) wx = data.Width + wx;
                if (wz < 0) wz = data.Height + wz;
                if (wx >= data.Width) wx = wx - data.Width;
                if (wz >= data.Height) wz = wz - data.Height;
            }

			return !(wx < 0 || wz < 0 || wx >= data.Width || wz >= data.Height);
		}

        int getHeightLocal(int x, int z)
        {
			int wx = 0;
			int wz = 0;
			if (getWorldPosition(x, z, ref wx, ref wz))
			{
				return data.getTerrainHeight(wx, wz);
			}

			if (owner.mapRom.outsideType == Map.OutsideType.MAPCHIP)
			{
				return 1;
			}
			else
			{
				return 0;
			}
        }

        int getAttribLocal(int x, int z)
        {
			int wx = 0;
			int wz = 0;
			if (getWorldPosition(x, z, ref wx, ref wz))
			{
				return data.getTerrainAttrib(wx, wz);
			}
			if (owner.mapRom.outsideType == Map.OutsideType.MAPCHIP)
			{
				return getBgChipIndex();   //水定義チップを取得する
			}
			else
			{
				return -1;
			}
        }

        private int getBgChipIndex()
        {
            if(bgChipIndex < 0)
                bgChipIndex = owner.getChipIndex(owner.getBgChip());
            return bgChipIndex;
        }

        SharpKmyMath.Vector3[] createBaseVertices(float _x, float _y, float _z)
        {
            float x = _x + ix * Yukar.Engine.MapData.TIPS_IN_CLUSTER + s_centerOffset;
            float z = _z + iz * Yukar.Engine.MapData.TIPS_IN_CLUSTER + s_centerOffset;
            float y = _y;
            float sz = 1;
            SharpKmyMath.Vector3[] tp = new SharpKmyMath.Vector3[17]{
                //上
                new SharpKmyMath.Vector3(x + 0.0f, y + 0, z + 0.0f) * sz,        //0

                new SharpKmyMath.Vector3(x - 0.5f, y + 0, z - 0.5f) * sz,
                new SharpKmyMath.Vector3(x + 0.0f, y + 0, z - 0.5f) * sz,//far   //2
                new SharpKmyMath.Vector3(x + 0.5f, y + 0, z - 0.5f) * sz,
                new SharpKmyMath.Vector3(x + 0.5f, y + 0, z + 0.0f) * sz,//right //4
                new SharpKmyMath.Vector3(x + 0.5f, y + 0, z + 0.5f) * sz,
                new SharpKmyMath.Vector3(x + 0.0f, y + 0, z + 0.5f) * sz,//near  //6
                new SharpKmyMath.Vector3(x - 0.5f, y + 0, z + 0.5f) * sz,
                new SharpKmyMath.Vector3(x - 0.5f, y + 0, z + 0.0f) * sz,//left  //8

                new SharpKmyMath.Vector3(x - 0.5f, y - 1, z - 0.5f) * sz,
                new SharpKmyMath.Vector3(x + 0.0f, y - 1, z - 0.5f) * sz,//far   //10
                new SharpKmyMath.Vector3(x + 0.5f, y - 1, z - 0.5f) * sz,
                new SharpKmyMath.Vector3(x + 0.5f, y - 1, z + 0.0f) * sz,//right //12
                new SharpKmyMath.Vector3(x + 0.5f, y - 1, z + 0.5f) * sz,
                new SharpKmyMath.Vector3(x + 0.0f, y - 1, z + 0.5f) * sz,//near  //14
                new SharpKmyMath.Vector3(x - 0.5f, y - 1, z + 0.5f) * sz,
                new SharpKmyMath.Vector3(x - 0.5f, y - 1, z + 0.0f) * sz,//left  //16

                };

            return tp;
        }

		void modifyVertices(SharpKmyMath.Vector3[] list, int type, bool isLow)
		{
			float oneSideOffset = 0.125f;
			float twoSideOffset = 0.125f;
			float capeOffset = 0.125f;
			int ofs = isLow ? 8 : 0;

			switch (type)
			{
				case EDGE_NEAR:
					list[6+ofs].z -= oneSideOffset;
					break;

				case EDGE_FAR:
					list[2+ofs].z += oneSideOffset;
					break;

				case EDGE_LEFT:
					list[8+ofs].x += oneSideOffset;
					break;

				case EDGE_RIGHT:
					list[4+ofs].x -= oneSideOffset;
					break;

				case EDGE_LEFT_FAR:
					list[1+ofs].x += twoSideOffset;
					list[1+ofs].z += twoSideOffset;
					break;

				case EDGE_LEFT_NEAR:
					list[7+ofs].x += twoSideOffset;
					list[7+ofs].z -= twoSideOffset;
					break;

				case EDGE_RIGHT_FAR:
					list[3+ofs].x -= twoSideOffset;
					list[3+ofs].z += twoSideOffset;
					break;

				case EDGE_RIGHT_NEAR:
					list[5+ofs].x -= twoSideOffset;
					list[5+ofs].z -= twoSideOffset;
					break;

				case EDGE_FAR_CAPE:
					list[1+ofs].x += capeOffset;
					list[1+ofs].z += capeOffset;

					list[3+ofs].x -= capeOffset;
					list[3+ofs].z += capeOffset;
					break;

				case EDGE_NEAR_CAPE:
					list[5+ofs].x -= capeOffset;
					list[5+ofs].z -= capeOffset;

					list[7+ofs].x += capeOffset;
					list[7+ofs].z -= capeOffset;

					break;

				case EDGE_RIGHT_CAPE:
					list[3+ofs].x -= capeOffset;
					list[3+ofs].z += capeOffset;

					list[5+ofs].x -= capeOffset;
					list[5+ofs].z -= capeOffset;

					break;

				case EDGE_LEFT_CAPE:
					list[1+ofs].x += capeOffset;
					list[1+ofs].z += capeOffset;

					list[7+ofs].x += capeOffset;
					list[7+ofs].z -= capeOffset;
					break;

				case EDGE_ISLAND:
					modifyVertices(list, EDGE_LEFT_CAPE,isLow);
					modifyVertices(list, EDGE_RIGHT_CAPE, isLow);
					break;

				case EDGE_LEFT_RIGHT:
					modifyVertices(list, EDGE_LEFT, isLow);
					modifyVertices(list, EDGE_RIGHT, isLow);
					break;

				case EDGE_FAR_NEAR:
					modifyVertices(list, EDGE_FAR, isLow);
					modifyVertices(list, EDGE_NEAR, isLow);
					break;
			}
		}

		int getEdgeStatusOther(int ofsx, int ofsz, int y)
		{
			return getEdgeStatus(currentX + ofsx, currentZ + ofsz, -ofsx, -ofsz, y);
		}

		public static SharpKmyGfx.Color getColorByHeight(float height)
        {
            float f = (height * 0.03f + 0.7f);
            if (f > 1) f = 1;
			return new SharpKmyGfx.Color(f, f, f, 1);
        }

		enum OCCLUDE_NORMAL
		{
			TOP,
			POSX,
			POSZ,
			NEGX,
			NEGZ,
		};

		float getHeightScore(int x, int z, int refY)
		{
			if (getHeightLocal(x, z) > refY) return 1;
			return 0;
		}

		float getOcclude(int x, int z, int y, OCCLUDE_NORMAL nrm, int vidx)
		{
			if (vidx >= 9) vidx -= 8;

			float val = 0;
			switch(nrm)
			{
				case OCCLUDE_NORMAL.TOP:
					switch(vidx)
					{
						case 0:
							val = 0;
							break;

						case 1:
							val =  (getHeightScore(x - 1, z - 1, y) + getHeightScore(x, z - 1, y) + getHeightScore(x - 1, z, y) + getHeightScore(x, z, y)) / 4;
							break;

						case 2:
							val =  (getHeightScore(x,z-1,y) + getHeightScore(x,z,y))/2;
							break;

						case 3:
							val =  (getHeightScore(x, z - 1, y) + getHeightScore(x + 1, z - 1, y) + getHeightScore(x, z, y) + getHeightScore(x + 1, z, y)) / 4;
							break;

						case 4:
							val =  (getHeightScore(x + 1, z, y) + getHeightScore(x, z, y)) / 2;
							break;

						case 5:
							val =  (getHeightScore(x, z + 1, y) + getHeightScore(x + 1, z + 1, y) + getHeightScore(x, z, y) + getHeightScore(x + 1, z, y)) / 4;
							break;

						case 6:
							val =  (getHeightScore(x, z, y) + getHeightScore(x, z+1, y)) / 2;
							break;

						case 7:
							val =  (getHeightScore(x, z + 1, y) + getHeightScore(x - 1, z + 1, y) + getHeightScore(x, z, y) + getHeightScore(x - 1, z, y)) / 4;
							break;

						case 8:
							val =  (getHeightScore(x-1, z, y) + getHeightScore(x, z, y)) / 2;
							break;
					}
					break;

				case OCCLUDE_NORMAL.POSX:
					switch(vidx)
					{
						case 3:
							val =  (getHeightScore(x + 1, z - 1, y - 1) + getHeightScore(x + 1, z, y - 1) + getHeightScore(x + 1, z - 1, y) + getHeightScore(x + 1, z, y)) / 4;
							break;

						case 4:
							val =  (getHeightScore(x + 1, z, y - 1) + getHeightScore(x + 1, z, y)) / 2;
							break;

						case 5:
							val =  (getHeightScore(x + 1, z + 1, y - 1) + getHeightScore(x + 1, z, y - 1) + getHeightScore(x + 1, z + 1, y) + getHeightScore(x + 1, z, y)) / 4;
							break;

					}

					break;

				case OCCLUDE_NORMAL.NEGX:
					switch (vidx)
					{
						case 1:
							val =  (getHeightScore(x - 1, z - 1, y - 1) + getHeightScore(x - 1, z, y - 1) + getHeightScore(x - 1, z - 1, y) + getHeightScore(x - 1, z, y)) / 4;
							break;

						case 8:
							val =  (getHeightScore(x - 1, z, y - 1) + getHeightScore(x - 1, z, y)) / 2;
							break;

						case 7:
							val =  (getHeightScore(x - 1, z + 1, y - 1) + getHeightScore(x - 1, z, y - 1) + getHeightScore(x - 1, z + 1, y) + getHeightScore(x - 1, z, y)) / 4;
							break;
					}

					break;

				case OCCLUDE_NORMAL.POSZ:
					switch(vidx)
					{
						case 7:
							val =  (getHeightScore(x - 1, z + 1, y - 1) + getHeightScore(x - 1, z + 1, y) + getHeightScore(x, z + 1, y - 1) + getHeightScore(x, z + 1, y)) / 4;
							break;

						case 6:
							val =  (getHeightScore(x, z + 1, y - 1) + getHeightScore(x, z + 1, y)) / 2;
							break;

						case 5:
							val =  (getHeightScore(x + 1, z + 1, y - 1) + getHeightScore(x + 1, z + 1, y) + getHeightScore(x, z + 1, y - 1) + getHeightScore(x, z + 1, y)) / 4;
							break;
					}

					break;

				case OCCLUDE_NORMAL.NEGZ:
					switch (vidx)
					{
						case 1:
							val =  (getHeightScore(x - 1, z - 1, y - 1) + getHeightScore(x - 1, z - 1, y) + getHeightScore(x, z - 1, y - 1) + getHeightScore(x, z - 1, y)) / 4;
							break;

						case 2:
							val =  (getHeightScore(x, z - 1, y - 1) + getHeightScore(x, z - 1, y)) / 2;
							break;

						case 3:
							val =  (getHeightScore(x + 1, z - 1, y - 1) + getHeightScore(x + 1, z - 1, y) + getHeightScore(x, z - 1, y - 1) + getHeightScore(x, z - 1, y)) / 4;
							break;
					}

					break;
			}

			return 1-val;
		}



		SharpKmyGfx.Color[] getTopFaceColor(int x, int z, int refY)
		{
			//float[] oc = getOcclude(x,z,refY);
			SharpKmyGfx.Color bc = getColorByHeight(refY);
			SharpKmyGfx.Color[] ret = new SharpKmyGfx.Color[9];
			for (int i = 0; i < 9; i++)
			{
				ret[i].r = (getOcclude(x, z, refY, OCCLUDE_NORMAL.TOP, i) * bc.r);
				ret[i].g = (getOcclude(x, z, refY, OCCLUDE_NORMAL.TOP, i) * bc.g);
				ret[i].b = (getOcclude(x, z, refY, OCCLUDE_NORMAL.TOP, i) * bc.b);
			}
			return ret;
		}

		void createTopFace(SharpKmyMath.Vector3[] positionlist, int index)
        {
			SharpKmyMath.Vector2 tl = owner.getTerrainTexCoordTopLeft(index, 0);
			SharpKmyMath.Vector2 br = owner.getTerrainTexCoordBottomRight(index, 0);
			SharpKmyMath.Vector2 ofs = br - tl;

			SharpKmyMath.Vector2[] tc = new SharpKmyMath.Vector2[9]{

				tl + ofs * 0.5f,
				new SharpKmyMath.Vector2(tl.x + ofs.x * 0.0f, tl.y + ofs.y * 0.0f),
				new SharpKmyMath.Vector2(tl.x + ofs.x * 0.5f, tl.y + ofs.y * 0.0f),
				new SharpKmyMath.Vector2(tl.x + ofs.x * 1.0f, tl.y + ofs.y * 0.0f),

				new SharpKmyMath.Vector2(tl.x + ofs.x * 1.0f, tl.y + ofs.y * 0.5f),
				new SharpKmyMath.Vector2(tl.x + ofs.x * 1.0f, tl.y + ofs.y * 1.0f),

				new SharpKmyMath.Vector2(tl.x + ofs.x * 0.5f, tl.y + ofs.y * 1.0f),
				new SharpKmyMath.Vector2(tl.x + ofs.x * 0.0f, tl.y + ofs.y * 1.0f),
				new SharpKmyMath.Vector2(tl.x + ofs.x * 0.0f, tl.y + ofs.y * 0.5f),

			};

			SharpKmyGfx.Color[] oc = getTopFaceColor(currentX, currentZ, currentY);
			int wx = 0;
			int wz = 0;
			Int16[] staticIndexList = { 0, 8, 2, 0, 2, 4, 0, 4, 6, 0, 6, 8, 8, 1, 2, 2, 3, 4, 4, 5, 6, 6, 7, 8 };
			SharpKmyGfx.VertexPositionNormalTexture2Color tmp = new SharpKmyGfx.VertexPositionNormalTexture2Color();
			getWorldPosition(currentX, currentZ, ref wx, ref wz);
			tmp.tc2.x = (float)(wx + 0.5f) / MapData.MASKMAPSIZE;
			tmp.tc2.y = (float)(wz + 0.5f) / MapData.MASKMAPSIZE;
			for (int c = 0; c < staticIndexList.Length; c++)
            {
                int i = staticIndexList[c];
				tmp.pos = positionlist[i];
				tmp.normal.x = 0;
				tmp.normal.y = 1;
				tmp.normal.z = 0;
				tmp.color = oc[i];
                tmp.tc = tc[i];
                op_vlist.Add(tmp);
            }
        }

		void createSideFace(OCCLUDE_NORMAL nrm, SharpKmyMath.Vector3[] positionlist, int index)
        {
			SharpKmyMath.Vector2 tl = owner.getTerrainTexCoordTopLeft(index, currentHeight - currentY + 1);
			SharpKmyMath.Vector2 br = owner.getTerrainTexCoordBottomRight(index, currentHeight - currentY + 1);
			SharpKmyMath.Vector2 ofs = br - tl;
			SharpKmyMath.Vector2[] tc = new SharpKmyMath.Vector2[6]{
				new SharpKmyMath.Vector2( tl.x + ofs.x * 1.0f, tl.y + ofs.y * 0.0f),
				new SharpKmyMath.Vector2( tl.x + ofs.x * 1.0f, tl.y + ofs.y * 1.0f),
				new SharpKmyMath.Vector2( tl.x + ofs.x * 0.5f, tl.y + ofs.y * 0.0f),
				new SharpKmyMath.Vector2( tl.x + ofs.x * 0.5f, tl.y + ofs.y * 1.0f),
				new SharpKmyMath.Vector2( tl.x + ofs.x * 0.0f, tl.y + ofs.y * 0.0f),
				new SharpKmyMath.Vector2( tl.x + ofs.x * 0.0f, tl.y + ofs.y * 1.0f),
            };

			Int16[] vilist = null;
		
			switch(nrm)
			{
				case OCCLUDE_NORMAL.NEGZ:
					vilist = new Int16[] { 1, 9, 2, 10, 3, 11 };
					break;

				case OCCLUDE_NORMAL.POSZ:
					vilist = new Int16[] { 5, 13, 6, 14, 7, 15 };
					break;

				case OCCLUDE_NORMAL.NEGX:
					vilist = new Int16[] { 7, 15, 8, 16, 1, 9 };
					break;

				case OCCLUDE_NORMAL.POSX:
					vilist = new Int16[] { 3, 11, 4, 12, 5, 13 };
					break;
			}
			

            int[] iofslist = { 0, 1, 3, 3, 2, 0, 4, 2, 3, 3, 5, 4 };
			SharpKmyGfx.VertexPositionNormalTexture2Color tmp = new SharpKmyGfx.VertexPositionNormalTexture2Color();
			int wx = 0;
			int wz = 0;
			getWorldPosition(currentX, currentZ, ref wx, ref wz);
			tmp.tc2.x = (float)(wx + 0.5f) / MapData.MASKMAPSIZE;
			tmp.tc2.y = (float)(wz + 0.5f) / MapData.MASKMAPSIZE;

			//var upoc = getOccludeSide(currentX, currentZ, currentY - 1);
			//var lowoc = getOccludeSide(currentX, currentZ, currentY - 2);

            for (int ci = 0; ci < iofslist.Length; ci++)
            {
                int i = iofslist[ci];
                tmp.pos = positionlist[vilist[i]];
				var c = getColorByHeight(tmp.pos.y);
				if (vilist[i] > 8)
				{
					//int ocidx = vilist[i] - 8;
					float oc = getOcclude(currentX, currentZ, currentY-1, nrm, vilist[i]);
					tmp.color.r = c.r * oc;
					tmp.color.g = c.g * oc;
					tmp.color.b = c.b * oc;
				}
				else
				{
					float oc = getOcclude(currentX, currentZ, currentY, nrm, vilist[i]);
					tmp.color.r = c.r * oc;
					tmp.color.g = c.g * oc;
					tmp.color.b = c.b * oc;
				}
                tmp.tc = tc[i];
                op_vlist.Add(tmp);
            }
        }

		bool createStair(int localx, int localz)
		{
			int wx = 0;
			int wz = 0;
			if (!getWorldPosition(localx, localz, ref wx, ref wz)) return false;

			SharpKmyMath.Vector3[] slopeBaseVtx = new SharpKmyMath.Vector3[]
			{
				new SharpKmyMath.Vector3(0,0,0),
				new SharpKmyMath.Vector3(0,1,0),
				new SharpKmyMath.Vector3(0,0,1),
				new SharpKmyMath.Vector3(1,0,0),
				new SharpKmyMath.Vector3(1,1,0),
				new SharpKmyMath.Vector3(1,0,1),
			};

			SharpKmyMath.Vector3[] ridgeSlopeBaseVtx = new SharpKmyMath.Vector3[]
			{
				new SharpKmyMath.Vector3(0,1,0),
				new SharpKmyMath.Vector3(1,0,0),
				new SharpKmyMath.Vector3(1,0,1),
				new SharpKmyMath.Vector3(0,0,1),

                // 地形に開く穴を隠すために追加
				new SharpKmyMath.Vector3(0,0,0),
			};

			SharpKmyMath.Vector3[] valleySlopeBaseVtx = new SharpKmyMath.Vector3[]
			{
				new SharpKmyMath.Vector3(0,1,0),
				new SharpKmyMath.Vector3(1,1,0),
				new SharpKmyMath.Vector3(1,0,1),
				new SharpKmyMath.Vector3(0,1,1),

                // 地形に開く穴を隠すために追加
				new SharpKmyMath.Vector3(0,0,0),
				new SharpKmyMath.Vector3(1,0,0),
				new SharpKmyMath.Vector3(0,0,1),
			};

			SharpKmyMath.Vector3[] stairBaseVtx = new SharpKmyMath.Vector3[]
			{                
				new SharpKmyMath.Vector3(0,0,0),
				new SharpKmyMath.Vector3(0,1,0),
				new SharpKmyMath.Vector3(0,1,0.33f),
				new SharpKmyMath.Vector3(0,0.66f,0.33f),
				new SharpKmyMath.Vector3(0,0.66f,0.66f),
				new SharpKmyMath.Vector3(0,0.33f,0.66f),
				new SharpKmyMath.Vector3(0,0.33f,1),
				new SharpKmyMath.Vector3(0,0,1),

				new SharpKmyMath.Vector3(1,0,0),
				new SharpKmyMath.Vector3(1,1,0),
				new SharpKmyMath.Vector3(1,1,0.33f),
				new SharpKmyMath.Vector3(1,0.66f,0.33f),
				new SharpKmyMath.Vector3(1,0.66f,0.66f),
				new SharpKmyMath.Vector3(1,0.33f,0.66f),
				new SharpKmyMath.Vector3(1,0.33f,1),
				new SharpKmyMath.Vector3(1,0,1),
                
			};

			SharpKmyMath.Vector3[] ridgeStairBaseVtx = new SharpKmyMath.Vector3[]
			{
				//t1
				new SharpKmyMath.Vector3(0,1,0),
				new SharpKmyMath.Vector3(0.33f, 1, 0),
				new SharpKmyMath.Vector3(0.33f, 1, 0.33f),
				new SharpKmyMath.Vector3(0, 1, 0.33f),

				//t2-1
				new SharpKmyMath.Vector3(0, 0.66f, 0.33f),
				new SharpKmyMath.Vector3(0.33f, 0.66f, 0.33f),
				new SharpKmyMath.Vector3(0.66f, 0.66f, 0.66f),
				new SharpKmyMath.Vector3(0, 0.66f, 0.66f),
				//t2-2
				new SharpKmyMath.Vector3(0.33f, 0.66f, 0.33f),
				new SharpKmyMath.Vector3(0.33f, 0.66f, 0),
				new SharpKmyMath.Vector3(0.66f, 0.66f, 0),
				new SharpKmyMath.Vector3(0.66f, 0.66f, 0.66f),

				//t3-1
				new SharpKmyMath.Vector3(0, 0.33f, 0.66f),
				new SharpKmyMath.Vector3(0.66f, 0.33f, 0.66f),
				new SharpKmyMath.Vector3(1, 0.33f, 1),
				new SharpKmyMath.Vector3(0, 0.33f, 1),
				//t3-2
				new SharpKmyMath.Vector3(0.66f, 0.33f, 0.66f),
				new SharpKmyMath.Vector3(0.66f, 0.33f, 0),
				new SharpKmyMath.Vector3(1, 0.33f, 0),
				new SharpKmyMath.Vector3(1, 0.33f, 1),
				
				//
				//k1-1
				new SharpKmyMath.Vector3(0, 1, 0.33f),
				new SharpKmyMath.Vector3(0.33f, 1, 0.33f),
				new SharpKmyMath.Vector3(0.33f, 0.66f, 0.33f),
				new SharpKmyMath.Vector3(0, 0.66f, 0.33f),

				//k1-2
				new SharpKmyMath.Vector3(0.33f, 1, 0.33f),
				new SharpKmyMath.Vector3(0.33f, 1, 0),
				new SharpKmyMath.Vector3(0.33f, 0.66f, 0),
				new SharpKmyMath.Vector3(0.33f, 0.66f, 0.33f),

				//k2-1
				new SharpKmyMath.Vector3(0, 0.66f, 0.66f),
				new SharpKmyMath.Vector3(0.66f, 0.66f, 0.66f),
				new SharpKmyMath.Vector3(0.66f, 0.33f, 0.66f),
				new SharpKmyMath.Vector3(0, 0.33f, 0.66f),
				//k2-2
				new SharpKmyMath.Vector3(0.66f, 0.66f, 0.66f),
				new SharpKmyMath.Vector3(0.66f, 0.66f, 0),
				new SharpKmyMath.Vector3(0.66f, 0.33f, 0),
				new SharpKmyMath.Vector3(0.66f, 0.33f, 0.66f),
				//k3-1
				new SharpKmyMath.Vector3(0, 0.33f, 1),
				new SharpKmyMath.Vector3(1, 0.33f, 1),
				new SharpKmyMath.Vector3(1, 0, 1),
				new SharpKmyMath.Vector3(0, 0, 1),
				//k3-2
				new SharpKmyMath.Vector3(1, 0.33f, 1),
				new SharpKmyMath.Vector3(1, 0.33f, 0),
				new SharpKmyMath.Vector3(1, 0, 0),
				new SharpKmyMath.Vector3(1, 0, 1),

				//S(Zaxis)
				new SharpKmyMath.Vector3(0,0,0),
				new SharpKmyMath.Vector3(0,1,0),
				new SharpKmyMath.Vector3(0,1,0.33f),
				new SharpKmyMath.Vector3(0,0.66f,0.33f),
				new SharpKmyMath.Vector3(0,0.66f,0.66f),
				new SharpKmyMath.Vector3(0,0.33f,0.66f),
				new SharpKmyMath.Vector3(0,0.33f,1),
				new SharpKmyMath.Vector3(0,0,1),

				//S(Xaxis)
				new SharpKmyMath.Vector3(0,0,0),
				new SharpKmyMath.Vector3(0,1,0),
				new SharpKmyMath.Vector3(0.33f,1,0),
				new SharpKmyMath.Vector3(0.33f,0.66f,0),
				new SharpKmyMath.Vector3(0.66f,0.66f,0),
				new SharpKmyMath.Vector3(0.66f,0.33f,0),
				new SharpKmyMath.Vector3(1,0.33f,0),
				new SharpKmyMath.Vector3(1,0,0),
		};

			SharpKmyMath.Vector3[] valleyStairBaseVtx = new SharpKmyMath.Vector3[]
			{
				//t1-1
				new SharpKmyMath.Vector3(0, 1, 1),
				new SharpKmyMath.Vector3(0, 1, 0),
				new SharpKmyMath.Vector3(0.33f, 1, 0.33f),
				new SharpKmyMath.Vector3(0.33f, 1, 1),
				//t1-2
				new SharpKmyMath.Vector3(0, 1, 0),
				new SharpKmyMath.Vector3(1, 1, 0),
				new SharpKmyMath.Vector3(1, 1, 0.33f),
				new SharpKmyMath.Vector3(0.33f, 1, 0.33f),

				//t2-1
				new SharpKmyMath.Vector3(0.33f, 0.66f, 1),
				new SharpKmyMath.Vector3(0.33f, 0.66f, 0.33f),
				new SharpKmyMath.Vector3(0.66f, 0.66f, 0.66f),
				new SharpKmyMath.Vector3(0.66f, 0.66f, 1),
				//t2-2
				new SharpKmyMath.Vector3(0.33f, 0.66f, 0.33f),
				new SharpKmyMath.Vector3(1, 0.66f, 0.33f),
				new SharpKmyMath.Vector3(1, 0.66f, 0.66f),
				new SharpKmyMath.Vector3(0.66f, 0.66f, 0.66f),

				//t3
				new SharpKmyMath.Vector3(0.66f, 0.33f, 0.66f),
				new SharpKmyMath.Vector3(1, 0.33f, 0.66f),
				new SharpKmyMath.Vector3(1, 0.33f, 1),
				new SharpKmyMath.Vector3(0.66f, 0.33f, 1),
	
				
				//k1-1
				new SharpKmyMath.Vector3(0.33f, 1, 1),
				new SharpKmyMath.Vector3(0.33f, 1, 0.33f),
				new SharpKmyMath.Vector3(0.33f, 0.66f, 0.33f),
				new SharpKmyMath.Vector3(0.33f, 0.66f, 1),
				//k1-2
				new SharpKmyMath.Vector3(0.33f, 1, 0.33f),
				new SharpKmyMath.Vector3(1, 1, 0.33f),
				new SharpKmyMath.Vector3(1, 0.66f, 0.33f),
				new SharpKmyMath.Vector3(0.33f, 0.66f, 0.33f),

				//k2-1
				new SharpKmyMath.Vector3(0.66f, 0.66f, 1),
				new SharpKmyMath.Vector3(0.66f, 0.66f, 0.66f),
				new SharpKmyMath.Vector3(0.66f, 0.33f, 0.66f),
				new SharpKmyMath.Vector3(0.66f, 0.33f, 1),
				//k2-2
				new SharpKmyMath.Vector3(0.66f, 0.66f, 0.66f),
				new SharpKmyMath.Vector3(1, 0.66f, 0.66f),
				new SharpKmyMath.Vector3(1, 0.33f, 0.66f),
				new SharpKmyMath.Vector3(0.66f, 0.33f, 0.66f),
				//36verts

				//S(Zaxis-align)
				new SharpKmyMath.Vector3(1,0,0),
				new SharpKmyMath.Vector3(1,1,0),
				new SharpKmyMath.Vector3(1,1,0.33f),
				new SharpKmyMath.Vector3(1,0.66f,0.33f),
				new SharpKmyMath.Vector3(1,0.66f,0.66f),
				new SharpKmyMath.Vector3(1,0.33f,0.66f),
				new SharpKmyMath.Vector3(1,0.33f,1),
				new SharpKmyMath.Vector3(1,0,1),

				//S(Xaxis-align)
				new SharpKmyMath.Vector3(0,0,1),
				new SharpKmyMath.Vector3(0,1,1),
				new SharpKmyMath.Vector3(0.33f,1,1),
				new SharpKmyMath.Vector3(0.33f,0.66f,1),
				new SharpKmyMath.Vector3(0.66f,0.66f,1),
				new SharpKmyMath.Vector3(0.66f,0.33f,1),
				new SharpKmyMath.Vector3(1,0.33f,1),
				new SharpKmyMath.Vector3(1,0,1),
				//52verts

				new SharpKmyMath.Vector3(0,1,1),
				new SharpKmyMath.Vector3(0,0,1),
				new SharpKmyMath.Vector3(0,0,0),
				new SharpKmyMath.Vector3(0,1,0),

				new SharpKmyMath.Vector3(0,1,0),
				new SharpKmyMath.Vector3(0,0,0),
				new SharpKmyMath.Vector3(1,0,0),
				new SharpKmyMath.Vector3(1,1,0),
			};

            int[] slopeIndices = new int[]   { 1, 2, 0, 4, 3, 5, 1, 4, 5, 5, 2, 1, 1, 0, 3, 3, 4, 1 };
			int[] slopeTCIndices = new int[] { 4, 7, 6, 4, 6, 7, 0, 1, 2, 2, 3, 0, 4, 7, 6, 6, 5, 4 };
            int[] ridgeSlopeIndices = new int[] { 0, 1, 2, 2, 3, 0, /* 山 */0, 4, 1, 0, 3, 4 };
			int[] ridgeSlopeTCIndices = new int[] { 0, 1, 2, 2, 3, 0, 4,7,6, 4,6,7 };
            int[] valleySlopeIndices = new int[] { 0, 1, 2, 2, 3, 0 ,/* 谷 */ 3,6,4, 4,0,3, 0,4,5, 5,1,0, 3,2,6, 1,5,2 };
			int[] valleySlopeTCIndices = new int[] { 0, 1, 2, 2, 3, 0, 4,7,6,6,5,4,4,7,6,6,5,4,4,6,7,4,7,6 };

			int[] ridgeStairIndices = new int[]{
			  0,1,2,2,3,0,
			  4,5,6,6,7,4,               8,9,10,10,11,8,
			  12,13,14,14,15,12, 		 16,17,18,18,19,16,
			  20,21,22,22,23,20,		 24,25,26,26,27,24,
			  28,29,30,30,31,28,		 32,33,34,34,35,32,
			  36,37,38,38,39,36,		 40,41,42,42,43,40,

			  //side
			  44,45,46,44,46,47,44,47,48,44,48,49,44,49,50,44,50,51,
			  52,54,53,52,55,54,52,56,55,52,57,56,52,58,57,52,59,58,

			};

			int[] valleyStairIndices = new int[]{
			  0,1,2,2,3,0,  			 4,5,6,6,7,4,
			  8,9,10,10,11,8,			 12,13,14,14,15,12,
			  16,17,18,18,19,16,		  20,21,22,22,23,20,
			  24,25,26,26,27,24,		  28,29,30,30,31,28,
			  32,33,34,34,35,32,

			  36,38,37,36,39,38,36,40,39,36,41,40,36,42,41,36,43,42,
			  44,45,46,44,46,47,44,47,48,44,48,49,44,49,50,44,50,51,

			  //back
			  52,53,54,54,55,52,
			  56,57,58,58,59,56,
			};

			int[] stairIndices = new int[] {
				0,1,2,
				0,2,3,
				0,3,4,
				0,4,5,
				0,5,6,
				0,6,7,

				8,10,9,
				8,11,10,
				8,12,11,
				8,13,12,
				8,14,13,
				8,15,14,

				1,9,10,
				10,2,1,
				3,11,12,
				12,4,3,
				5,13,14,
				14,6,5,
				
				2,10,11,
				11,3,2,
				4,12,13,
				13,5,4,
				6,14,15,
				15,7,6,

                0,9,1,
                9,0,8,
            };
            int[] stairTCIndices = new int[]{
				0,1,2,
				0,2,3,
				0,3,4,
				0,4,5,
				0,5,6,
				0,6,7,

				8,10,9,
				8,11,10,
				8,12,11,
				8,13,12,
				8,14,13,
				8,15,14,

				16,17,18,18,19,16,
				20,21,22,22,23,20,
				24,25,26,26,27,24,
				28,29,30,30,31,28,
				32,33,34,34,35,32,
				36,37,38,38,39,36,

				21,23,22,
				23,21,20,
			};

			Common.Rom.Map.StairRef sr = null;
			MapData.STAIR_STAT stat = owner.getStairStat(wx, wz, ref sr);
			if (stat == MapData.STAIR_STAT.NONE) return false;

#if WINDOWS
            // 元
            float[] angle = new float[] { (float)Math.PI, 0, -0.5f * (float)Math.PI, 0.5f * (float)Math.PI };
#else
            // 階段・坂反転対策のため
            float[] angle = new float[] { (float)Math.PI, 0, 0.5f * (float)Math.PI, -0.5f * (float)Math.PI };
#endif

			/*Matrix m = Matrix.CreateTranslation(-0.5f, 0, -0.5f) *
				Matrix.CreateRotationY(angle[(int)stat % 4]) *
				Matrix.CreateTranslation(0.5f, 0, 0.5f) *
				Matrix.CreateTranslation(wx, currentHeight, wz);*/
			SharpKmyMath.Matrix4 m = SharpKmyMath.Matrix4.translate(wx, currentHeight, wz) *
				SharpKmyMath.Matrix4.translate(0.5f, 0, 0.5f) *
				SharpKmyMath.Matrix4.rotateY(angle[(int)stat % 4]) *
				SharpKmyMath.Matrix4.translate(-0.5f, 0, -0.5f);
			SharpKmyGfx.VertexPositionNormalTexture2Color t = new SharpKmyGfx.VertexPositionNormalTexture2Color();
			t.tc2.x = (float)(MapData.MASKMAPSIZE - 1) / MapData.MASKMAPSIZE;
			t.tc2.y = (float)(MapData.MASKMAPSIZE - 1) / MapData.MASKMAPSIZE;
			if ((int)stat < 4)
			{
				if (owner.isStair(sr.gfxId))
				{
					SharpKmyMath.Vector2[] tc = owner.getStairTexCoord(sr.gfxId);
					for (int i = 0; i < stairIndices.Length; i++)
					{
						t.pos = (m * stairBaseVtx[stairIndices[i]]).getXYZ();// Vector3.Transform(stairBaseVtx[stairIndices[i]], m);
                        t.tc = tc[stairTCIndices[i]];
						t.color = getColorByHeight(currentHeight);
						op_vlist.Add(t);
					}
				}
				else
				{
					SharpKmyMath.Vector2[] tc = owner.getSlopeTexCoord(sr.gfxId);
					for (int i = 0; i < slopeIndices.Length; i++)
					{
						t.pos = (m * slopeBaseVtx[slopeIndices[i]]).getXYZ();//Vector3.Transform(slopeBaseVtx[slopeIndices[i]], m);
						t.tc = tc[slopeTCIndices[i]];
						t.color = getColorByHeight(currentHeight);
						op_vlist.Add(t);
					}
				}
			}
			else if ((int)stat < 8)
			{
				//尾根の場合
                if (owner.isStair(sr.gfxId))
				{
					SharpKmyMath.Vector2[] tc = owner.getRidgeStairTexCoord(sr.gfxId);
					for (int i = 0; i < ridgeStairIndices.Length; i++)
					{
						t.pos = (m * ridgeStairBaseVtx[ridgeStairIndices[i]]).getXYZ();//Vector3.Transform(ridgeStairBaseVtx[ridgeStairIndices[i]], m);
						t.tc = tc[ridgeStairIndices[i]];
						t.color = getColorByHeight(currentHeight);
						op_vlist.Add(t);
					}
				}
				else
				{
					SharpKmyMath.Vector2[] tc = owner.getSlopeTexCoord(sr.gfxId);
					for (int i = 0; i < ridgeSlopeIndices.Length; i++)
					{
						t.pos = (m * ridgeSlopeBaseVtx[ridgeSlopeIndices[i]]).getXYZ();// Vector3.Transform(ridgeSlopeBaseVtx[ridgeValleySlopeIndices[i]], m);
						t.tc = tc[ridgeSlopeTCIndices[i]];
						t.color = getColorByHeight(currentHeight);
						op_vlist.Add(t);
					}
				}
			}
			else if((int)stat < 16)
			{
				//谷の場合
                if (owner.isStair(sr.gfxId))
				{
					SharpKmyMath.Vector2[] tc = owner.getValleyStairTexCoord(sr.gfxId);
					for (int i = 0; i < valleyStairIndices.Length; i++)
					{
						t.pos = (m * valleyStairBaseVtx[valleyStairIndices[i]]).getXYZ();// Vector3.Transform(valleyStairBaseVtx[valleyStairIndices[i]], m);
						t.tc = tc[valleyStairIndices[i]];
						t.color = getColorByHeight(currentHeight);
						op_vlist.Add(t);
					}
				}
				else
				{
					SharpKmyMath.Vector2[] tc = owner.getSlopeTexCoord(sr.gfxId);
					for (int i = 0; i < valleySlopeIndices.Length; i++)
					{
						t.pos = (m * valleySlopeBaseVtx[valleySlopeIndices[i]]).getXYZ();// Vector3.Transform(valleySlopeBaseVtx[ridgeValleySlopeIndices[i]], m);
						t.tc = tc[valleySlopeTCIndices[i]];
						t.color = getColorByHeight(currentHeight);
						op_vlist.Add(t);
					}
				}

			}

			return true;
		}

        bool isCliff(int curX, int curZ, int ofsx, int ofsz, int height)
        {
			int h = getHeightLocal(curX + ofsx, curZ + ofsz);
			int a = getAttribLocal(curX + ofsx, curZ + ofsz);
			int sa = getAttribLocal(curX, curZ);

			Common.Rom.Map.StairRef sr = null;
			MapData.STAIR_STAT ss = owner.getStairStat(curX + ofsx + ix * MapData.TIPS_IN_CLUSTER, curZ + ofsz + iz * MapData.TIPS_IN_CLUSTER, ref sr);

			if (ss != MapData.STAIR_STAT.NONE && h == height - 1)
			{
				if (ss == MapData.STAIR_STAT.NEG_Z && ofsz == 1 && ofsx == 0) return false;
				if (ss == MapData.STAIR_STAT.NEG_X && ofsx == 1 && ofsz == 0) return false;
				if (ss == MapData.STAIR_STAT.POS_Z && ofsz == -1 && ofsx == 0) return false;
				if (ss == MapData.STAIR_STAT.POS_X && ofsx == -1 && ofsz == 0) return false;
				if (ss == MapData.STAIR_STAT.VALLEY_NEGZ_NEGX &&(( ofsz == 1 && ofsx == 0) || (ofsz == 0 && ofsx == 1))) return false;
				if (ss == MapData.STAIR_STAT.VALLEY_NEGZ_POSX && ((ofsz == 1 && ofsx == 0) || (ofsz == 0 && ofsx == -1))) return false;
				if (ss == MapData.STAIR_STAT.VALLEY_POSZ_NEGX && ((ofsz == -1 && ofsx == 0) || (ofsz == 0 && ofsx == 1))) return false;
				if (ss == MapData.STAIR_STAT.VALLEY_POSZ_POSX && ((ofsz == -1 && ofsx == 0) || (ofsz == 0 && ofsx == -1))) return false;

			}

			if (h < height)return true;
			if( h == height && (owner.getTerrainLiquidArea(a) && !owner.getTerrainLiquidArea(sa))) return true;
			if (currentHeight == height && (owner.getTerrainLiquidArea(a) && !owner.getTerrainLiquidArea(sa))) return true;
            return false;
        }

		int getEdgeStatus(int curX, int curZ, int x, int z, int y)
		{
			bool _left = false;
			bool _right = false;
			bool _far = false;
			bool _near = false;
			int num = 0;
			bool isParallel = false;
			//左
			if (isCliff(curX ,curZ, - 1, 0, y))
			{
				_left = true;
				num++;
			}

			//右
			if (isCliff(curX, curZ, 1, 0, y))
			{
				_right = true;
				if (_left) isParallel = true;
				num++;
			}

			//奥
			if (isCliff(curX, curZ, 0, -1, y))
			{
				_far = true;
				num++;
			}

			//手前
			if (isCliff(curX, curZ, 0, 1, y))
			{
				_near = true;
				if (_far) isParallel = true;
				num++;
			}

			int edge = EDGE_NONE;
			if (num == 2)
			{
				if (!isParallel)//コーナーの場合
				{
					if (_left && _far) edge = EDGE_LEFT_FAR;
					else if (_left && _near) edge = EDGE_LEFT_NEAR;
					else if (_right && _far) edge = EDGE_RIGHT_FAR;
					else if (_right && _near) edge = EDGE_RIGHT_NEAR;
				}
				else
				{
					if (_left)//left & right
					{
						edge = EDGE_LEFT_RIGHT;
					}
					else
					{
						edge = EDGE_FAR_NEAR;
					}
				}
			}
			else if (num == 1)
			{
				if (_left) edge = EDGE_LEFT;
				if (_right) edge = EDGE_RIGHT;
				if (_far) edge = EDGE_FAR;
				if (_near) edge = EDGE_NEAR;
			}
			else if (num == 3)
			{
				if (_left && _far && _near) edge = EDGE_LEFT_CAPE;
				if (_right && _far && _near) edge = EDGE_RIGHT_CAPE;
				if (_far && _left && _right) edge = EDGE_FAR_CAPE;
				if (_near && _left && _right) edge = EDGE_NEAR_CAPE;
			}
			else if (num == 4)
			{
				edge = EDGE_ISLAND;
			}

			return edge;
		}

		void addWorldEdgeLiquidCap(int x, int z)
		{
			int wx = 0, wz = 0;
			float wy = 1f - WATER_TOP_OFFST;
			getWorldPosition(x, z, ref wx, ref wz);

			SharpKmyGfx.VertexPositionNormalTexture2Color tmp = new SharpKmyGfx.VertexPositionNormalTexture2Color();
			tmp.color = getColorByHeight(currentY);

            SharpKmyMath.Vector2 tl = owner.getTerrainTexCoordTopLeft(getBgChipIndex(), 0);
            SharpKmyMath.Vector2 br = owner.getTerrainTexCoordBottomRight(getBgChipIndex(), 0);
			SharpKmyMath.Vector2 uvc = (tl + br) * 0.5f;

			if (wx == 0)
			{
				tmp.pos = new SharpKmyMath.Vector3(wx, wy, wz);
				tmp.tc = new SharpKmyMath.Vector2(tl.x, tl.y);
				op_vlist.Add(tmp);
				tmp.pos = new SharpKmyMath.Vector3(wx + 0.5f, wy, wz + 0.5f);
				tmp.tc = uvc;
				op_vlist.Add(tmp);
				tmp.pos = new SharpKmyMath.Vector3(wx, wy, wz + 1);
				tmp.tc = new SharpKmyMath.Vector2(tl.x, br.y);
				op_vlist.Add(tmp);
			}

			if (wx == owner.mapRom.Width - 1)
			{
				tmp.pos = new SharpKmyMath.Vector3(wx+1, wy, wz);
				tmp.tc = new SharpKmyMath.Vector2(br.x, tl.y);
				op_vlist.Add(tmp);
				tmp.pos = new SharpKmyMath.Vector3(wx + 1, wy, wz + 1);
				tmp.tc = uvc;
				op_vlist.Add(tmp);
				tmp.pos = new SharpKmyMath.Vector3(wx + 0.5f, wy, wz + 0.5f);
				tmp.tc = new SharpKmyMath.Vector2(br.x, br.y);
				op_vlist.Add(tmp);
			}

			if (wz == 0)
			{
				tmp.pos = new SharpKmyMath.Vector3(wx, wy, wz);
				tmp.tc = new SharpKmyMath.Vector2(tl.x, tl.y);
				op_vlist.Add(tmp);
				tmp.pos = new SharpKmyMath.Vector3(wx + 1, wy, wz);
				tmp.tc = uvc;
				op_vlist.Add(tmp);
				tmp.pos = new SharpKmyMath.Vector3(wx + 0.5f, wy, wz + 0.5f);
				tmp.tc = new SharpKmyMath.Vector2(br.x, tl.y);
				op_vlist.Add(tmp);
			}

			if (wz == owner.mapRom.Height - 1)
			{
				tmp.pos = new SharpKmyMath.Vector3(wx, wy, wz + 1);
				tmp.tc = new SharpKmyMath.Vector2(tl.x, br.y);
				op_vlist.Add(tmp);
				tmp.pos = new SharpKmyMath.Vector3(wx + 0.5f, wy, wz + 0.5f);
				tmp.tc = uvc;
				op_vlist.Add(tmp);
				tmp.pos = new SharpKmyMath.Vector3(wx + 1, wy, wz + 1);
				tmp.tc = new SharpKmyMath.Vector2(br.x, br.y);
				op_vlist.Add(tmp);
			}

		}


		void createLiquidCap(SharpKmyMath.Vector3[] list)
		{
			//return;

			SharpKmyMath.Vector3[] modifylist = new SharpKmyMath.Vector3[17];
			for (int i = 0; i < 17; i++) modifylist[i] = list[i];

			int sa = getAttribLocal(currentX, currentZ);

			SharpKmyMath.Vector2 tl = owner.getTerrainTexCoordTopLeft(sa, 0);
			SharpKmyMath.Vector2 br = owner.getTerrainTexCoordBottomRight(sa, 0);
			SharpKmyMath.Vector2 uvc = (tl + br) * 0.5f;

			//SharpKmyMath.Vector2 tl2 = owner.getTerrainTexCoordTopLeft(sa, 1);
			//SharpKmyMath.Vector2 br2 = owner.getTerrainTexCoordBottomRight(sa, 1);
			//SharpKmyMath.Vector2 uvc2 = (tl2 + br2) * 0.5f;

			SharpKmyGfx.VertexPositionNormalTexture2Color tmp = new SharpKmyGfx.VertexPositionNormalTexture2Color();
			tmp.color = getColorByHeight(currentY);

			SharpKmyGfx.Color[] oc = getTopFaceColor(currentX, currentZ, currentY);

			//NEGZ
			int h = getHeightLocal(currentX, currentZ - 1);
			int a = getAttribLocal(currentX, currentZ - 1);
			if (a != sa && !owner.getTerrainLiquidArea(a))
			{
				if (h >= currentY && !capped[0])
				{
					capped[0] = true;
					modifylist[2] += new SharpKmyMath.Vector3(0, 0, -0.5f);
					modifylist[10] += new SharpKmyMath.Vector3(0, 0, -0.5f);

					tmp.pos = list[1];
					tmp.tc = new SharpKmyMath.Vector2(tl.x, br.y);
					tmp.color = oc[1];
					op_vlist.Add(tmp);

					tmp.pos = list[2] + new SharpKmyMath.Vector3(0, 0, -0.5f);
					tmp.tc = uvc;
					tmp.color = oc[2];
					op_vlist.Add(tmp);
		
					tmp.pos = list[3];
					tmp.tc = new SharpKmyMath.Vector2(br.x, br.y);
					tmp.color = oc[3];
					op_vlist.Add(tmp);

					if (currentY > getHeightLocal(currentX - 1, currentZ - 1) ||
						currentY > getHeightLocal(currentX + 1, currentZ - 1))
					{
						createSideFace(OCCLUDE_NORMAL.NEGZ, modifylist, sa);
					}
				}
			}

			//POSZ
			h = getHeightLocal(currentX, currentZ + 1);
			a = getAttribLocal(currentX, currentZ + 1);
			if (a != sa && !owner.getTerrainLiquidArea(a))
			{
				if (h >= currentY && !capped[1])
				{
					capped[1] = true;
					modifylist[6] += new SharpKmyMath.Vector3(0, 0, 0.5f);
					modifylist[14] += new SharpKmyMath.Vector3(0, 0, 0.5f);

					tmp.pos = list[5];
					tmp.tc = new SharpKmyMath.Vector2(tl.x, tl.y);
					tmp.color = oc[5];
					op_vlist.Add(tmp);

					tmp.pos = list[6] + new SharpKmyMath.Vector3(0, 0, 0.5f);
					tmp.tc = uvc;
					tmp.color = oc[6];
					op_vlist.Add(tmp);
					
					tmp.pos = list[7];
					tmp.tc = new SharpKmyMath.Vector2(br.x, tl.y);
					tmp.color = oc[7];
					op_vlist.Add(tmp);

					if (currentY > getHeightLocal(currentX - 1, currentZ + 1) ||
						currentY > getHeightLocal(currentX + 1, currentZ + 1))
					{
						createSideFace(OCCLUDE_NORMAL.POSZ, modifylist, sa);
					}
				}
			}

			//NEGX
			h = getHeightLocal(currentX - 1, currentZ);
			a = getAttribLocal(currentX - 1, currentZ);
			if (a != sa && !owner.getTerrainLiquidArea(a))
			{
				if (h >= currentY && !capped[2])
				{
					capped[2] = true;
					modifylist[8] += new SharpKmyMath.Vector3(-0.5f, 0, 0);
					modifylist[16] += new SharpKmyMath.Vector3(-0.5f, 0, 0);

					tmp.pos = list[7];
					tmp.tc = new SharpKmyMath.Vector2(tl.x, br.y);
					tmp.color = oc[7];
					op_vlist.Add(tmp);

					tmp.pos = list[8] + new SharpKmyMath.Vector3(-0.5f, 0, 0);
					tmp.tc = uvc;
					tmp.color = oc[8];
					op_vlist.Add(tmp);

					tmp.pos = list[1];
					tmp.tc = new SharpKmyMath.Vector2(tl.x, tl.y);
					tmp.color = oc[1];
					op_vlist.Add(tmp);

					if (currentY > getHeightLocal(currentX - 1, currentZ + 1) ||
						currentY > getHeightLocal(currentX - 1, currentZ - 1))
					{
						createSideFace(OCCLUDE_NORMAL.NEGX, modifylist, sa);
					}
				}
			}

			//POSX
			h = getHeightLocal(currentX + 1, currentZ);
			a = getAttribLocal(currentX + 1, currentZ);
			if (a != sa && !owner.getTerrainLiquidArea(a))
			{
				if (h >= currentY && !capped[3])
				{
					capped[3] = true;
					modifylist[4] += new SharpKmyMath.Vector3(0.5f, 0, 0);
					modifylist[12] += new SharpKmyMath.Vector3(0.5f, 0, 0);

					tmp.pos = list[3];
					tmp.tc = new SharpKmyMath.Vector2(br.x, tl.y);
					tmp.color = oc[3];
					op_vlist.Add(tmp);

					tmp.pos = list[4] + new SharpKmyMath.Vector3(+0.5f, 0, 0);
					tmp.tc = uvc;
					tmp.color = oc[4];
					op_vlist.Add(tmp);

					tmp.pos = list[5];
					tmp.tc = new SharpKmyMath.Vector2(br.x, br.y);
					tmp.color = oc[5];
					op_vlist.Add(tmp);

					if (currentY > getHeightLocal(currentX + 1, currentZ + 1) ||
						currentY > getHeightLocal(currentX + 1, currentZ - 1))
					{
						createSideFace(OCCLUDE_NORMAL.POSX, modifylist, sa);
					}
				}
			}
		}

		public void updateInternal()
		{
			op_vlist.Clear();

			for (int z = 0; z < Yukar.Engine.MapData.TIPS_IN_CLUSTER; z++)
			{
				currentZ = z;
				for (int x = 0; x < Yukar.Engine.MapData.TIPS_IN_CLUSTER; x++)
				{
					capped[0] = false;
					capped[1] = false;
					capped[2] = false;
					capped[3] = false;

					currentX = x;

					// チップ状態を取得
					int height = getHeightLocal(x, z);
					int atr = getAttribLocal(x, z);

					// マップ範囲外チェック
					if (owner.mapRom.outsideType == Map.OutsideType.REPEAT)
					{
						int wx = 0;
						int wz = 0;
						if (!getWorldPosition(x, z, ref wx, ref wz, true)) continue;
					}
					else
					{
						if (atr < 0) continue; ;
					}

					currentHeight = height;

					for (int y = height; y > 0; y--)
					{
						currentY = y;

						float ofs = owner.getTerrainLiquidArea(atr) ? -WATER_TOP_OFFST : 0;
						SharpKmyMath.Vector3[] v = createBaseVertices(x, y + ofs, z);

						//周囲の状況からエッジステータスを取る
						int edge = getEdgeStatus(currentX, currentZ, x, z, y);
						int edgeLow = getEdgeStatus(currentX, currentZ, x, z, y == 0 ? y : y - 1);

						if (owner.getTerrainLiquidArea(atr))
						{
							//水の蓋
							createLiquidCap(v);
							//modifyLiquidVertices(v);
						}

						if (!owner.getTerrainSquareShape(atr))
						{
							//必要に応じて格子を変形
							modifyVertices(v, edge, false);
							modifyVertices(v, edgeLow, true);
						}

						//上
						if (y == height)//最上面の場合
						{
							if (!createStair(x, z))
							{
								createTopFace(v, atr);
							}
						}

						//側面（ある場合）
						{
							if ((edge & EDGE_LEFT) != 0) createSideFace(OCCLUDE_NORMAL.NEGX, v, atr);
							if ((edge & EDGE_RIGHT) != 0) createSideFace(OCCLUDE_NORMAL.POSX, v, atr);
							if ((edge & EDGE_FAR) != 0) createSideFace(OCCLUDE_NORMAL.NEGZ, v, atr);
							if ((edge & EDGE_NEAR) != 0) createSideFace(OCCLUDE_NORMAL.POSZ, v, atr);
						}

						//外縁部の海と接している場合
						if (owner.mapRom.outsideType == Map.OutsideType.MAPCHIP && y == 1 && !owner.getTerrainLiquidArea(atr))
						{
							addWorldEdgeLiquidCap(x, z);
						}

					}
				}
			}

			SharpKmyMath.Vector3 nrm = new SharpKmyMath.Vector3(0);
			for (int i = 0; i < op_vlist.Count; i++)
			{
				if (i % 3 == 0)
				{
					SharpKmyMath.Vector3 p0 = op_vlist[i].pos;
					SharpKmyMath.Vector3 p1 = op_vlist[i + 1].pos;
					SharpKmyMath.Vector3 p2 = op_vlist[i + 2].pos;

					nrm = SharpKmyMath.Vector3.crossProduct((p2 - p0), (p1 - p0));//時計回りで作っているので
					nrm = SharpKmyMath.Vector3.normalize(nrm);
				}
				SharpKmyGfx.VertexPositionNormalTexture2Color v = op_vlist[i];
				v.normal = nrm;
				op_vlist[i] = v;
			}
		}

        public void updateGeometry()
        {
			if (geomInvalid)
			{
				//情報が無効だった場合
				updateInternal();
				geomInvalid = false;

				//ここでキャッシュ登録
				data.setCache(ix, iz, op_vlist);

                createBuffer();
            }
		}

#if WINDOWS
        public void exportFBX(System.String fname,System.String tname)
        {
            if (op_vlist.Count != 0)
            {
                List<FBXExporter.VertexPositionNormalTexture2Color> vl = new List<FBXExporter.VertexPositionNormalTexture2Color>();
                for (int i = 0; i < op_vlist.Count; i++)
                {
                    FBXExporter.VertexPositionNormalTexture2Color v;
                    v.pos.x = op_vlist[i].pos.x;
                    v.pos.y = op_vlist[i].pos.y;
                    v.pos.z = op_vlist[i].pos.z;
                    v.tc.x = op_vlist[i].tc.x;
                    v.tc.y = op_vlist[i].tc.y;
                    v.tc2.x = op_vlist[i].tc2.x;
                    v.tc2.y = op_vlist[i].tc2.y;
                    v.color.r = op_vlist[i].color.r;
                    v.color.g = op_vlist[i].color.g;
                    v.color.b = op_vlist[i].color.b;
                    v.color.a = op_vlist[i].color.a;
                    v.normal.x = op_vlist[i].normal.x;
                    v.normal.y = op_vlist[i].normal.y;
                    v.normal.z = op_vlist[i].normal.z;
                    vl.Add(v);
                }
                FBXExporter.FBXConv.Export(vl, fname, tname);
            }
        }

        public void mergeFBX(List<FBXExporter.VertexPositionNormalTexture2Color> vl)
        {
            if (op_vlist.Count != 0)
            {
                for (int i = 0; i < op_vlist.Count; i++)
                {
                    FBXExporter.VertexPositionNormalTexture2Color v;
                    v.pos.x = op_vlist[i].pos.x;
                    v.pos.y = op_vlist[i].pos.y;
                    v.pos.z = op_vlist[i].pos.z;
                    v.tc.x = op_vlist[i].tc.x;
                    v.tc.y = op_vlist[i].tc.y;
                    v.tc2.x = op_vlist[i].tc2.x;
                    v.tc2.y = op_vlist[i].tc2.y;
                    v.color.r = op_vlist[i].color.r;
                    v.color.g = op_vlist[i].color.g;
                    v.color.b = op_vlist[i].color.b;
                    v.color.a = op_vlist[i].color.a;
                    v.normal.x = op_vlist[i].normal.x;
                    v.normal.y = op_vlist[i].normal.y;
                    v.normal.z = op_vlist[i].normal.z;
                    vl.Add(v);
                }
            }
        }
#endif

         void createBuffer()
        {
            //バッファの作成
            if (op_vlist.Count != 0)
            {
				vb.setData(op_vlist);

            }

        }

	}
}

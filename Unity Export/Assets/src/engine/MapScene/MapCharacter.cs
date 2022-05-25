using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yukar.Common;

using Microsoft.Xna.Framework;
using System.IO;
//using Microsoft.Xna.Framework.Graphics;

namespace Yukar.Engine
{
    // マップ上のキャラクター位置を格納するクラス
    public class MapCharacter
    {
        private readonly string[] DO_SUBMERGE_2D_MOTION_NAME = { "chair", "bed" };
        private const int CURRENTPAGE_NOT_INITED = -2;

        public float x;//移動先はあたり判定などを考慮して、あらかじめ予約する
        public float z;
        public float offsetX; // チップからチップへの移動中に加算する値（表示用）
        public float offsetZ;
        public float offsetY;
        public float y;//マップから求める。
        [Flags]
        public enum HideCauses
        {
            NONE = 0,
            BY_EVENT = 1,
            BY_FOLLOWERS = 1 << 1,
            BY_BATTLE = 1 << 2,
            BY_CAMERA = 1 << 4,
        }
        public HideCauses hide;
        public bool collidable;
        public bool fixDirection;
        public bool mapHeroSymbol;
        public string currentMotion;    // 状態としてのモーション名(イベントで一時的にwalkになるなどの際は更新されない)
        public string nowMotion;        // 現在再生しているモーション名(一時的なものも含む)
        public bool lockMotion;         // モーションが一周するまでユーザー操作によるモーション変化が起こらないようにするためのフラグ

        public float mMoveStep;
        public const float DEFAULT_SPEED = 0.1f;
        int mAnimCntIncl = 1;

        Vector3 mCurDir;
        Vector3 mTgtDir;
        int mDirection;
		float	mDirectionRad;

        internal Yukar.Common.Resource.ResourceItem character;
        ModifiedModelInstance mdl = null;
		MapBillboard bil = null;
        public MapBillboard getMapBillboard() { return this.bil; }
        SharpKmyGfx.ParticleInstance ptcl = null;
        int iX;//ビルボードアイテムのインデックス
        public int billboardAnimRemain;
        SharpKmyGfx.Texture shadow_tex;
		SharpKmyGfx.Shader mBaseShader = null;
		SharpKmyGfx.Shader mSolidColorShader = null;
		SharpKmyGfx.BoxFillPrimitive mTentativeBox = null;
		SharpKmyGfx.QuadTexturedPrimitive shadowquad;

        List<ModifiedModelInstance> m_motions = new List<ModifiedModelInstance>();

		bool isDestroyed = false;

		//float mTestTime = 0;
		//int mTestAnimIdx = 0;

        // 現在シート判断用
        internal Common.Rom.Event rom;
        internal int currentPage = CURRENTPAGE_NOT_INITED;
        public Guid moveScript = Guid.Empty;
        public bool expand;
        public const float BODY_SIZE = 0.3f;
        public bool fixHeight;  // 移動時に高さをアジャストしないフラグ(戦闘時で逃げる時用)
        public bool useOverrideColor;
        private SharpKmyGfx.Color overrideColor;
        private Common.Resource.Character2DAnimSet now2dMotion;
        public bool isCommonEvent;
        private float recentYAngle;

        public enum CheckResult
        {
            NONE,
            INVALID,
            VALID,
            VALID_SLOPE_TO_SLOPE,
        }

        public MapCharacter()
        {
            x = 0.5f;
            y = 0.5f;
            offsetX = 0;
            offsetZ = 0;
            mMoveStep = DEFAULT_SPEED;
            hide = HideCauses.NONE;
#if false
			mCurDir = new Vector3(0, 0, 1f);
            mTgtDir = new Vector3(0, 0, 1f);
            mDirection = Util.DIR_SER_DOWN;
#else
			setDirection( Util.DIR_SER_DOWN, true, true );
#endif
			collidable = false;
			mapHeroSymbol = false;
			
            mdl = null;
            bil = null;
        }

        public MapCharacter(Common.Rom.Event ev) : this()
        {
            this.rom = ev;
        }

        public void setPosition(float x, float z)
        {
            this.x = x + 0.5f;
            this.z = z + 0.5f;
			offsetX = offsetZ = 0;
        }
      
		//------------------------------------------------------------------------------
		/**
		 *	方向を設定
		 */
        public void setDirection(int dir, bool immed = false, bool bForce = false )
        {
            if (dir == Util.DIR_SER_NONE)
                return;

            if (fixDirection && !bForce )
                return;

            switch (dir)
            {
                case Util.DIR_SER_RIGHT:
                    mTgtDir = new Vector3(1, 0, 0);
                    break;

                case Util.DIR_SER_DOWN:
                    mTgtDir = new Vector3(0, 0, 1);
                    break;

                case Util.DIR_SER_LEFT:
                    mTgtDir = new Vector3(-1, 0, 0);
                    break;

                case Util.DIR_SER_UP:
                    mTgtDir = new Vector3(0, 0, -1);
                    break;
            } 

            if (immed)
                mCurDir = mTgtDir;

			// 方向（デジタル値）を設定
            mDirection = dir;

			// 方向（ラジアン値）を設定
			mDirectionRad = convertDirectionToRadian( dir );
        }
        
        internal void setTgtDir(Vector3 dir)
        {
            mTgtDir = dir;
        }

        internal void setCurDir(Vector3 dir)
        {
            mCurDir = dir;
        }

        //------------------------------------------------------------------------------
        /**
		 *	方向を設定（ラジアン値指定）
		 */
        public void setDirectionFromRadian(float rad, bool immed = false, bool bForce = false )
        {
            if (fixDirection && !bForce )
                return;

			SharpKmyMath.Vector3 vec = (SharpKmyMath.Matrix4.rotateY( rad ) * SharpKmyMath.Matrix4.translate(0,0,1)).translation();
			mTgtDir.X = vec.x;
			mTgtDir.Y = vec.y;
			mTgtDir.Z = vec.z;

            if (immed)
                mCurDir = mTgtDir;

			// 方向（デジタル値）を設定
			mDirection = convertDirectionToDigital( rad );

			// 方向（ラジアン値）を設定
			mDirectionRad = rad;
        }

		//------------------------------------------------------------------------------
		/**
		 *	方向値を変換（ラジアン → デジタル）
		 */
		public static int convertDirectionToDigital( float rad )
		{
			int dir = Util.DIR_SER_UP;
			float deg = MathHelper.ToDegrees( rad );

			if( deg <= -135 || deg >= 135 ) {
				dir = Util.DIR_SER_UP;
			} else if( deg > -135 && deg <= -45 ) {
				dir = Util.DIR_SER_LEFT;
			} else if( deg > -45 && deg < 45 ) {
				dir = Util.DIR_SER_DOWN;
			} else if( deg >= 45 && deg < 135 ) {
				dir = Util.DIR_SER_RIGHT;
			}

			return dir;
		}

		//------------------------------------------------------------------------------
		/**
		 *	方向値を変換（デジタル → ラジアン）
		 */
		public static float convertDirectionToRadian( int dir )
		{
			float rad = 0.0f;

			switch( dir )
			{
			case Util.DIR_SER_UP:		rad = MathHelper.ToRadians(180);	break;
			case Util.DIR_SER_DOWN:		rad = MathHelper.ToRadians(0);	    break;
			case Util.DIR_SER_LEFT:		rad = MathHelper.ToRadians(-90);	break;
			case Util.DIR_SER_RIGHT:	rad = MathHelper.ToRadians(90);	    break;
			default:
				rad = 0.0f;
				break;
			}

			return rad;
		}

        public void playMotion( string name, float blendTime = 0.2f, bool isTemporary = false, bool doLockMotion = false )
        {
            if (!isTemporary)
                currentMotion = name;
            nowMotion = name;
            lockMotion = doLockMotion;

            if (mdl != null)
            {
                mdl.inst.playMotion(name, blendTime);
            }
            else if (bil != null)
            {
                // 指定されたモーションを探す
                var image = character as Common.Resource.Character;
                if (image == null)
                    return;

                var motion = image.subItemList.FirstOrDefault(x => x.name == name || x.name.ToLower() == name);
                if (motion == null)
                    motion = image;

                if (now2dMotion == motion)
                    return;

                now2dMotion = motion;
                bil.changeGraphic(now2dMotion.path, now2dMotion.animationFrame, now2dMotion.getDivY()); //= new MapBillboard(now2dMotion.path, now2dMotion.animationFrame, now2dMotion.getDivY());
                updatePosAngle(recentYAngle, 0);
                iX = 0;
                billboardAnimRemain = now2dMotion.animationSpeed;
            }
		}
        
        internal bool isChangeMotionAvailable()
        {
            if (!lockMotion)
                return true;

            if (mdl != null && mdl.inst.getMotionLoopCount() == 0)
                return false;

            if (bil != null && bil.loopCount == 0)
                return false;

            return true;
        }

		public bool isBillboard()
		{
			return bil != null;
		}

		void genTantative()
		{
			if (mTentativeBox == null)
			{
				mTentativeBox = new SharpKmyGfx.BoxFillPrimitive();
				mTentativeBox.setColor(0.3f, 1f, 0.2f, 1f);
				mTentativeBox.setSize(0.5f, 0.5f, 0.5f);
				mTentativeBox.setShaderName("map_notex");
				mTentativeBox.setCull(SharpKmyGfx.CommonPrimitive.CULLTYPE.BACK);
			}
		}

		public void ChangeGraphic(Yukar.Common.Resource.ResourceItem image, MapData map)
        {
            Reset(true);

            character = image;

            if (image == null)
                return;

            if (map != null) y = getAdjustedYPosition(map);

			switch (getDisplayType())
			{
				case Common.Resource.Character.DisplayType.MODEL:
                    #region ＜MODEL＞
                    mdl = Graphics.LoadModel(image.path);
					if (mdl == null)
					{
						genTantative();
					}
					else
					{
                        //テクスチャフィルタを解除して最大近傍法にする
                        for (int i = 0; i < 100; i++)
                        {
                            SharpKmyGfx.DrawInfo d = mdl.inst.getDrawInfo(i);
                            if (d == null) break;

                            SharpKmyGfx.Texture t = d.getTexture(0);
                            if (t != null)
                            {
                                t.setMagFilter(SharpKmyGfx.TEXTUREFILTER.kNEAREST);
                            }
                        }

						//アンビエントマップがあるかどうか・・・
						string texdirname = Path.Combine(Util.file.getDirName(image.path), "texture");
						string ambtexname = Util.file.getFileNameWithoutExtension(image.path) + "_ambient.png";
						if (Util.file.exists(Path.Combine(texdirname, ambtexname)))
						{
							//スロット１にアンビエントマップを入れる
							var di = mdl.inst.getDrawInfo(0);
							var atex = SharpKmyGfx.Texture.loadFromResourceServer(ambtexname);
							if (atex != null)
							{
								di.setTexture(1, atex);
								atex.Release();//removeref;
							}

							//アンビエント有シェーダー
							var ashd = SharpKmyGfx.Shader.load("map_with_amb");
							di.setShader(ashd);
							mBaseShader = ashd;
						}
                        
                        if (loadSeparatedMotions(image.path, mdl, m_motions).Count == 0 && mdl.modifiedModel.motions.Count == 0)
						{
							//define not found.
							mdl.inst.addMotion("walk", mdl.inst.getModel(), 1f, 60f, true);
							mdl.inst.addMotion("wait", mdl.inst.getModel(), 91f, 180f, true);
							mdl.inst.addMotion("run", mdl.inst.getModel(), 201f, 230f, true);
						}
						mdl.inst.playMotion("wait", 0);
					}
					break;
                    #endregion
                case Common.Resource.Character.DisplayType.BILLBOARD:
                    #region ＜BILLBOARD＞
                    if (image is Common.Resource.Character)
                    {
                        var chr = image as Common.Resource.Character;
                        now2dMotion = chr;
                        bil = new MapBillboard(chr.path, chr.animationFrame, chr.getDivY(), 48f / chr.resolution);
                        bil.blend = SharpKmyGfx.BLENDTYPE.kALPHA;
                        billboardAnimRemain = chr.animationSpeed;
                    }
                    else if (image is Common.Resource.Monster)
                    {
                        var chr = image as Common.Resource.Monster;
                        bil = new MapBillboard(chr.path, 1, 1, 0.5f);
                        bil.blend = SharpKmyGfx.BLENDTYPE.kPREMULTIPLIED;
                        billboardAnimRemain = int.MaxValue;
                    }

                    /*
                    string shadowPath = Common.Catalog.sCommonResourceDir + "res\\system\\circle_shadow.png";
                    shadow_tex = SharpKmyGfx.Texture.load(shadowPath);

                    shadowquad = new SharpKmyGfx.QuadTexturedPrimitive();
			        shadowquad.setAxis(2);
			        shadowquad.setTexture(0, shadow_tex);
			        shadowquad.setQuad(0.5f, 0.5f);
			        shadowquad.setBlend(SharpKmyGfx.CommonPrimitive.BLENDTYPE.BLEND_PREMULTIPLIED);
                    */
					break;
                    #endregion
                case Common.Resource.Character.DisplayType.PARTICLE:
                    #region ＜PARTICLE＞
                    ptcl = Graphics.LoadParticle(image.path);
					if (ptcl == null)
					{
						genTantative();
					}
					else
					{
						ptcl.start(SharpKmyMath.Matrix4.translate(x, y, z));
						{
							var shd = SharpKmyGfx.Shader.load("particle");
							var dlist = ptcl.getDrawInfo();
							if (dlist != null)
							{
								foreach (var i in dlist)
								{
									i.setShader(shd);
								}
							}
							shd.Release();
						}
					}
					break;
                    #endregion
            }

			mSolidColorShader = SharpKmyGfx.Shader.load("legacy_notex");
			if(mBaseShader == null)mBaseShader = SharpKmyGfx.Shader.load("map");
		}

        internal Vector3 getPosition()
        {
            return new Vector3(x + offsetX, y + offsetY, z + offsetZ);
        }

        public static List<string> loadSeparatedMotions(string path, ModifiedModelInstance mdl, List<ModifiedModelInstance> m_motions)
        {
            var result = new List<string>();
            string motiondirname = Common.Util.file.getDirName(path) + Path.DirectorySeparatorChar + "motion";
            if (mdl != null && Common.Util.file.dirExists(motiondirname))
            {
                //モーションファイルが直下にある
                //各自持ちにしてしまう・・・
                var files = Common.Util.file.getFiles(motiondirname + Path.DirectorySeparatorChar);
                foreach (var file in files)
                {
                    result.Add(loadMotion(file, mdl, m_motions));
                }
            }
            return result;
        }

        private static string loadMotion(string path, ModifiedModelInstance mdl, List<ModifiedModelInstance> m_motions)
        {
            var fileName = Util.file.getFileName(path);
            var isLooped = fileName.IndexOf("_o_") < 0;
            var start = fileName.LastIndexOf('_') + 1;
            var end = fileName.LastIndexOf('.');
            var name = fileName.Substring(start, end - start);
            if (!Util.file.exists(path))
            {
                //UnityEngine.Debug.Log("motion is not exist " + path);
                return "";
            }
            var mot = Graphics.LoadModel(path);
            if (mot != null)
            {
                m_motions.Add(mot);
                if (mdl != null) mdl.inst.addMotion(name, mot.modifiedModel.model, isLooped);
            }
            return name;
        }

        private Common.Resource.Character.DisplayType getDisplayType()
        {
            var chr = character as Common.Resource.Character;
            if (chr != null)
                return chr.getDisplayType();

            var mobj = character as Common.Resource.MapObject;
            if (mobj != null)
                return Common.Resource.Character.DisplayType.MODEL;

            return Common.Resource.Character.DisplayType.BILLBOARD;
        }

        public void Reset(bool forChangeGraphic = false)
        {
            if (mdl != null) Graphics.UnloadModel(mdl);
            mdl = null;

            if (bil != null) bil.finalize();
            bil = null;

			if (ptcl != null) Graphics.UnloadParticle(ptcl);
			ptcl = null;
			if (shadow_tex != null) shadow_tex.Release();
			shadow_tex = null;
			if (shadowquad != null) shadowquad.Release();
			shadowquad = null;

			if(mSolidColorShader!=null)mSolidColorShader.Release();
			mSolidColorShader = null;

			if (mBaseShader != null) mBaseShader.Release();
			mBaseShader = null;

			if (!forChangeGraphic && !isDestroyed)
			{
				isDestroyed = true;
			}

			foreach (var m in m_motions)
			{
                if (m != null) Graphics.UnloadModel(m);
            }
            m_motions = new List<ModifiedModelInstance>();

			if (mTentativeBox != null)
			{
				mTentativeBox.Release();
				mTentativeBox = null;
			}
        }

        public int getDirection()
        {
            return mDirection;
        }

        public Vector3 getCurDir()
        {
            return mCurDir;
        }

        public Vector3 getTgtDir()
        {
            return mTgtDir;
        }

		//------------------------------------------------------------------------------
		/**
		 *	現在向いている方向をラジアン値で取得
		 */
		public float getDirectionRadian()
		{
			return mDirectionRad;
		}

        public void ChangeSpeed(int speed)
        {
            if (speed < -4)
                speed = -4;
            else if (speed > 3)
                speed = 3;

            switch (speed)
            {
                case -4: mMoveStep = DEFAULT_SPEED / 8; return;
                case -3: mMoveStep = DEFAULT_SPEED / 4; return;
                case -2: mMoveStep = DEFAULT_SPEED / 2; return;
                case -1: mMoveStep = DEFAULT_SPEED / 1.5f; return;
                case 0: mMoveStep = DEFAULT_SPEED; return;
                case 1: mMoveStep = DEFAULT_SPEED * 1.5f; return;
                case 2: mMoveStep = DEFAULT_SPEED * 2; return;
                case 3: mMoveStep = DEFAULT_SPEED * 3; return;
            }
        }

        public bool Walk(int dx, int dz, bool changeDir, MapData map, EventHeightMap objMap, bool ignoreHeight = false, bool hitEvent = true)
        {
            // 移動中は受付不可
            if (IsMoving())
                return false;

            if (changeDir)
            {
                var oldDir = mDirection;
                var oldTgtdir = mTgtDir;

                setDirection(calcDirection(dx, dz));

                // マップオブジェクトベースの場合は、方向転換自体が可能かどうかも調べる
                if (character is Common.Resource.MapObject && !checkMapObjectSpinable(this, map, objMap))
                {
                    mDirection = oldDir;
                    mTgtDir = oldTgtdir;
                    return false;
                }
            }

            //ここで移動できるか判定（呼び出し順が優先じゅんということで）
            bool inSlope = false;
            if (character is Common.Resource.MapObject)
            {
                checkMapObjectWalkable(this, ref dx, ref dz, map, objMap, ignoreHeight, hitEvent);
            }
            else
            {
                var res = checkWalkable(this, ref dx, ref dz, map, objMap, ignoreHeight, hitEvent);
                if (res == CheckResult.VALID_SLOPE_TO_SLOPE)
                    inSlope = true;
            }

            // 座標加算
            x += dx;
            z += dz;
            
            offsetX = dx * -1;
            offsetZ = dz * -1;
            offsetY = y - getAdjustedYPosition(map, x, y, z);
            if (Math.Abs(offsetY) < 1 || inSlope)
                offsetY = 0;
            else
                y = getAdjustedYPosition(map, x, y, z);

            return dx != 0 || dz != 0;
        }

        private int calcDirection(float dx, float dz)
        {
            if (Math.Abs(dx) > Math.Abs(dz))
            {
                if (dx > 0)
                    return Util.DIR_SER_RIGHT;
                else
                    return Util.DIR_SER_LEFT;
            }
            else
            {
                if (dz > 0)
                    return Util.DIR_SER_DOWN;
                else
                    return Util.DIR_SER_UP;
            }
        }

        internal Vector2 Walk(float moveX, float moveZ, bool changeDir, MapData map, EventHeightMap objMap, bool ignoreHeight, bool hitEvent, MapEngine inMap = null)
        {
            //大きな値が入った場合の対策
            if(BODY_SIZE < Math.Abs(moveX) || BODY_SIZE < Math.Abs(moveZ)){
                Vector2 res = Vector2.Zero;
                float div = 0;
                if(Math.Abs(moveZ) < Math.Abs(moveX)){
                    div = Math.Abs(moveX) / BODY_SIZE;
                }
                else
                {
                    div = Math.Abs(moveZ) / BODY_SIZE;
                }
                div += 1;
                moveX /= (int)div;
                moveZ /= (int)div;
                for (var i1 = 0; i1 < (int)div; ++i1)
                {
                    res += this.Walk(moveX, moveZ, changeDir, map, objMap, ignoreHeight, hitEvent, inMap);
                }
                return res;
            }

            // 方向転換
            if (changeDir)
            {
#if ENABLE_VR
                if (SharpKmyVr.Func.IsReady())
                {
                    float rad = (float)Math.Atan2(moveX, moveZ);
                    setDirectionFromRadian(rad);
                }
                else
                {
                    setDirection(calcDirection(moveX, moveZ));
                }
#else   // #if ENABLE_VR
                setDirection(calcDirection(moveX, moveZ));
#endif  // #if ENABLE_VR
            }

            // 移動方向に応じたオフセットを加算する(intでキャストした切れ目だけで判断すると体が半分埋まるので)
            var dir = (float)Math.Atan2(moveX, moveZ);
            var bodyDisX = Math.Sin(dir) * BODY_SIZE;
            var bodyDisY = Math.Cos(dir) * BODY_SIZE;

            int newX = (int)(x + moveX + bodyDisX);
            int newZ = (int)(z + moveZ + bodyDisY);

            //移動
            int dx = newX - (int)x;
            int dz = newZ - (int)z;

            // 移動方向の検証
            var resX = CheckResult.VALID;
            var resZ = CheckResult.VALID;
            var resXZ = CheckResult.VALID;
            MapCharacter resChrX = null;
            MapCharacter resChrZ = null;
            MapCharacter resChrXZ = null;
            if (dx != 0)
            {
                int tmpDx = dx; int tmpZero = 0;
                resX = checkWalkable(this, ref tmpDx, ref tmpZero, map, objMap, ignoreHeight, hitEvent);
                if (inMap != null) resChrX = inMap.checkCharCollision(this, dx, 0);
            }
            if (dz != 0)
            {
                int tmpDz = dz; int tmpZero = 0;
                resZ = checkWalkable(this, ref tmpZero, ref tmpDz, map, objMap, ignoreHeight, hitEvent);
                if (inMap != null) resChrZ = inMap.checkCharCollision(this, 0, dz);
            }
            if (dx != 0 || dz != 0)
            {
                int tmpDx = dx; int tmpDz = dz;
                resXZ = checkWalkable(this, ref tmpDx, ref tmpDz, map, objMap, ignoreHeight, hitEvent);
                if (inMap != null) resChrXZ = inMap.checkCharCollision(this, dx, dz);
            }

            //マップ内イベントの移動方向の検証
            //十字移動できるのであれば、斜めのマスを確認
            if (resX != CheckResult.INVALID && resZ != CheckResult.INVALID
                && resChrX == null && resChrZ == null)
            {
                if (resXZ == CheckResult.INVALID
                || resChrXZ != null && resChrXZ.collidable)
                {
                    if (Math.Abs(moveZ) < Math.Abs(moveX))
                        moveZ = 0;
                    else
                        moveX = 0;
                }
            }

            //十字のどちらかにいけない場合
            if (resX == CheckResult.INVALID || (resChrX != null && resChrX.collidable)) moveX = 0;
            if (resZ == CheckResult.INVALID || (resChrZ != null && resChrZ.collidable)) moveZ = 0;

            x += moveX;
            z += moveZ;
            adjustXZPosition(map);
            return new Vector2(moveX, moveZ);
        }

        private void adjustXZPosition(MapData map)
        {
            // 範囲外だったら範囲内に戻す
            if (x < BODY_SIZE)
                x = BODY_SIZE;
            else if (x > map.mapRom.Width - BODY_SIZE)
                x = map.mapRom.Width - BODY_SIZE;
            if (z < BODY_SIZE)
                z = BODY_SIZE;
            else if (z > map.mapRom.Height - BODY_SIZE)
                z = map.mapRom.Height - BODY_SIZE;
        }

        private static bool checkMapObjectSpinable(MapCharacter self, MapData map, EventHeightMap objMap)
        {
            Common.Resource.MapObject mo = self.character as Common.Resource.MapObject;

            IntPoint r;
            r.x = (int)self.x;
            r.y = (int)self.z;
            r.r = 0;
            r.Dir = self.getDirection();

            //int fh = (int)map.getFurnitureHeight(r.x, r.y);//起点の配置高さ

            var adjustedObjectHeightMap = map.heightMapRotate(mo, r.r);

            int hsz = mo.heightMapHalfSize;
            for (int x = -hsz; x < hsz; x++)
            {
                for (int y = -hsz; y < hsz; y++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    var bh = adjustedObjectHeightMap[x + hsz, y + hsz];//家具にあたり判定がせっていしてある
                    if (bh.height > 0)
                    {
                        checkWalkable(self, r.x, r.y, ref x, ref y, map, objMap, false);
                    }
                    if (x == 0 && y == 0)
                        return false;
                }
            }

            return true;
        }

        private static void checkMapObjectWalkable(MapCharacter self, ref int dx, ref int dz, MapData map, EventHeightMap objMap, bool enableForcedMove, bool hitEvent)
        {
            Common.Resource.MapObject mo = self.character as Common.Resource.MapObject;

            IntPoint r;
            r.x = (int)self.x;
            r.y = (int)self.z;
            r.r = 0;
            r.Dir = self.getDirection();

            //int fh = (int)map.getFurnitureHeight(r.x, r.y);//起点の配置高さ

            var adjustedObjectHeightMap = map.heightMapRotate(mo, r.r);

            int hsz = mo.heightMapHalfSize;
            for (int x = -hsz; x < hsz; x++)
            {
                for (int y = -hsz; y < hsz; y++)
                {
                    int ax = x + r.x;//実際のマップの位置
                    int ay = y + r.y;
                    var bh = adjustedObjectHeightMap[x + hsz, y + hsz];//家具にあたり判定がせっていしてある
                    // 自分自身の位置に高さがない場合1にする
                    if (ax == (int)self.x && ay == (int)self.z)
                    {
                        bh.height = bh.height > 0 ? bh.height : 1;
                    }
                    if (bh.height > 0)
                    {
                        checkWalkable(self, ax, ay, ref dx, ref dz, map, objMap, enableForcedMove, hitEvent);
                    }
                    if (dx == 0 && dz == 0)
                        return;
                }
            }

        }

        private static CheckResult checkWalkable(MapCharacter self, ref int dx, ref int dz, MapData map, EventHeightMap objMap, bool enableForcedMove, bool hitEvent = true)
        {
            int x = (int)self.x;
            int z = (int)self.z;
            return checkWalkable(self, x, z, ref dx, ref dz, map, objMap, enableForcedMove, hitEvent);
        }

        private static CheckResult checkWalkable(MapCharacter self, int x, int z, ref int dx, ref int dz, MapData map, EventHeightMap objMap, bool enableForcedMove, bool hitEvent = true)
        {
            float tx = x + (float)dx;
            float tz = z + (float)dz;

            Common.Rom.Map.StairRef sr = null;
            float ch = getCollisionHeight(self, map, objMap, x, z, false);
            float th = map.getTerrainHeight(x + dx, z + dz);
            float dh = getCollisionHeight(self, map, objMap, x + dx, z + dz, hitEvent);
            MapData.STAIR_STAT css = map.getStairStat(x, z, ref sr);
            MapData.STAIR_STAT dss = map.getStairStat(x + dx, z + dz, ref sr);
            bool ground = map.isWalkable(x + dx, z + dz);
            bool cRoof = map.getCellIsRoof(x, z);
            bool dRoof = map.getCellIsRoof(x + dx, z + dz);
            SharpKmyMath.Vector2 sz = map.getSize();
            // マップ範囲チェック
            if (tx < 0 || tz < 0)
            {
                dx = dz = 0;
            }
            else if (tx >= sz.x || tz >= sz.y)
            {
                dx = dz = 0;
            }

            // 移動不可チップかどうかのチェック
            else if (!ground && !enableForcedMove)
            {
                dx = dz = 0;
            }

            // 移動判定
            else
            {
                var invalid = doCheckWalkable(css, dss, cRoof ? self.y : ch, dh, dx, dz);
                // ダメだった場合は、下を通れるかも調べる
                if (invalid == CheckResult.INVALID && dRoof && self.y < dh - 1)
                {
                    // 地面との当たり判定を取っている
                    invalid = doCheckWalkable(css, dss, cRoof ? self.y : ch, th, dx, dz);

                    //ダメなら向こう側が橋か判定する。
                    if (invalid == CheckResult.INVALID)
                    {
                        var dy = map.getFurnitureHeight(x + dx, z + dz, (int)self.y);
                        // 前のオブジェクトと高さが等しいければおそらく橋
                        if (dy == self.y)
                        {
                            invalid = CheckResult.VALID;
                        }
                    }
                }

                if (enableForcedMove)
                {
                    invalid = CheckResult.VALID;
                }
                else if (invalid == CheckResult.INVALID)
                {
                    dx = dz = 0;
                }

                return invalid;
            }

            return CheckResult.INVALID;
        }

        private static CheckResult doCheckWalkable(MapData.STAIR_STAT css, MapData.STAIR_STAT dss, float ch, float dh, int dx, int dz)
        {
            CheckResult result = CheckResult.INVALID;

            if (ch == dh)
            {
                //同じフロアの移動なので問題ない。
                if (css == MapData.STAIR_STAT.NONE && dss == MapData.STAIR_STAT.NONE) result = CheckResult.VALID;
                //同じフロアのLV1に上がるので問題ない。
                if (css == MapData.STAIR_STAT.NONE && dss == MapData.STAIR_STAT.NEG_Z && dz < 0) result = CheckResult.VALID;
                if (css == MapData.STAIR_STAT.NONE && dss == MapData.STAIR_STAT.POS_Z && dz > 0) result = CheckResult.VALID;
                if (css == MapData.STAIR_STAT.NONE && dss == MapData.STAIR_STAT.NEG_X && dx < 0) result = CheckResult.VALID;
                if (css == MapData.STAIR_STAT.NONE && dss == MapData.STAIR_STAT.POS_X && dx > 0) result = CheckResult.VALID;
                //同じフロアのLV0に降りるので問題ない。
                if (css == MapData.STAIR_STAT.NEG_Z && dss == MapData.STAIR_STAT.NONE && dz > 0) result = CheckResult.VALID;
                if (css == MapData.STAIR_STAT.POS_Z && dss == MapData.STAIR_STAT.NONE && dz < 0) result = CheckResult.VALID;
                if (css == MapData.STAIR_STAT.NEG_X && dss == MapData.STAIR_STAT.NONE && dx > 0) result = CheckResult.VALID;
                if (css == MapData.STAIR_STAT.POS_X && dss == MapData.STAIR_STAT.NONE && dx < 0) result = CheckResult.VALID;
                //両方スロープなのでOKとする
                if (css != MapData.STAIR_STAT.NONE && dss != MapData.STAIR_STAT.NONE) result = CheckResult.VALID;

                if (css == MapData.STAIR_STAT.NONE && dss == MapData.STAIR_STAT.RIDGE_NEGZ_NEGX && (dx < 0 || dz < 0)) result = CheckResult.VALID;
                if (css == MapData.STAIR_STAT.NONE && dss == MapData.STAIR_STAT.RIDGE_NEGZ_POSX && (dx > 0 || dz < 0)) result = CheckResult.VALID;
                if (css == MapData.STAIR_STAT.NONE && dss == MapData.STAIR_STAT.RIDGE_POSZ_NEGX && (dx < 0 || dz > 0)) result = CheckResult.VALID;
                if (css == MapData.STAIR_STAT.NONE && dss == MapData.STAIR_STAT.RIDGE_POSZ_POSX && (dx > 0 || dz > 0)) result = CheckResult.VALID;

                if (dss == MapData.STAIR_STAT.NONE && css == MapData.STAIR_STAT.RIDGE_NEGZ_NEGX && (dx > 0 || dz > 0)) result = CheckResult.VALID;
                if (dss == MapData.STAIR_STAT.NONE && css == MapData.STAIR_STAT.RIDGE_NEGZ_POSX && (dx < 0 || dz > 0)) result = CheckResult.VALID;
                if (dss == MapData.STAIR_STAT.NONE && css == MapData.STAIR_STAT.RIDGE_POSZ_NEGX && (dx > 0 || dz < 0)) result = CheckResult.VALID;
                if (dss == MapData.STAIR_STAT.NONE && css == MapData.STAIR_STAT.RIDGE_POSZ_POSX && (dx < 0 || dz < 0)) result = CheckResult.VALID;
            }
            else if (Math.Abs(ch - dh + 0.9) < 1)   // 1.0でなく0.9なのは、足が階段に埋まるのを防ぐために階段にいる間、本来の高さより0.06浮かしているから
            {
                //上のフロアのLV0に上がるので問題ない。
                if (css == MapData.STAIR_STAT.NEG_Z && dss == MapData.STAIR_STAT.NONE && dz < 0) result = CheckResult.VALID;
                if (css == MapData.STAIR_STAT.POS_Z && dss == MapData.STAIR_STAT.NONE && dz > 0) result = CheckResult.VALID;
                if (css == MapData.STAIR_STAT.NEG_X && dss == MapData.STAIR_STAT.NONE && dx < 0) result = CheckResult.VALID;
                if (css == MapData.STAIR_STAT.POS_X && dss == MapData.STAIR_STAT.NONE && dx > 0) result = CheckResult.VALID;

                if (css == MapData.STAIR_STAT.VALLEY_NEGZ_NEGX && dss == MapData.STAIR_STAT.NONE && (dx < 0 || dz < 0)) result = CheckResult.VALID;
                if (css == MapData.STAIR_STAT.VALLEY_NEGZ_POSX && dss == MapData.STAIR_STAT.NONE && (dx > 0 || dz < 0)) result = CheckResult.VALID;
                if (css == MapData.STAIR_STAT.VALLEY_POSZ_NEGX && dss == MapData.STAIR_STAT.NONE && (dx < 0 || dz > 0)) result = CheckResult.VALID;
                if (css == MapData.STAIR_STAT.VALLEY_POSZ_POSX && dss == MapData.STAIR_STAT.NONE && (dx > 0 || dz > 0)) result = CheckResult.VALID;

                //連続したスロープ
                if (css != MapData.STAIR_STAT.NONE && dss != MapData.STAIR_STAT.NONE)
                {
                    if (css == dss)//同じ向きのスロープ
                    {
                        //進行方向が正しければOK.
                        if (css == MapData.STAIR_STAT.NEG_Z && dz < 0) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.POS_Z && dz > 0) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.NEG_X && dx < 0) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.POS_X && dx > 0) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                    }
                    else
                    {
                        if (css == MapData.STAIR_STAT.NEG_Z && dss == MapData.STAIR_STAT.RIDGE_NEGZ_NEGX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.NEG_Z && dss == MapData.STAIR_STAT.RIDGE_NEGZ_POSX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.POS_Z && dss == MapData.STAIR_STAT.RIDGE_POSZ_NEGX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.POS_Z && dss == MapData.STAIR_STAT.RIDGE_POSZ_POSX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.NEG_X && dss == MapData.STAIR_STAT.RIDGE_NEGZ_NEGX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.POS_X && dss == MapData.STAIR_STAT.RIDGE_NEGZ_POSX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.NEG_X && dss == MapData.STAIR_STAT.RIDGE_POSZ_NEGX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.POS_X && dss == MapData.STAIR_STAT.RIDGE_POSZ_POSX) result = CheckResult.VALID_SLOPE_TO_SLOPE;

                        if (css == MapData.STAIR_STAT.VALLEY_NEGZ_NEGX && dss == MapData.STAIR_STAT.NEG_Z) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.VALLEY_NEGZ_POSX && dss == MapData.STAIR_STAT.NEG_Z) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.VALLEY_POSZ_NEGX && dss == MapData.STAIR_STAT.POS_Z) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.VALLEY_POSZ_POSX && dss == MapData.STAIR_STAT.POS_Z) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.VALLEY_NEGZ_NEGX && dss == MapData.STAIR_STAT.NEG_X) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.VALLEY_NEGZ_POSX && dss == MapData.STAIR_STAT.POS_X) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.VALLEY_POSZ_NEGX && dss == MapData.STAIR_STAT.NEG_X) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.VALLEY_POSZ_POSX && dss == MapData.STAIR_STAT.POS_X) result = CheckResult.VALID_SLOPE_TO_SLOPE;

                        if (css == MapData.STAIR_STAT.VALLEY_NEGZ_NEGX && dss == MapData.STAIR_STAT.RIDGE_NEGZ_NEGX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.VALLEY_NEGZ_POSX && dss == MapData.STAIR_STAT.RIDGE_NEGZ_POSX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.VALLEY_POSZ_NEGX && dss == MapData.STAIR_STAT.RIDGE_POSZ_NEGX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.VALLEY_POSZ_POSX && dss == MapData.STAIR_STAT.RIDGE_POSZ_POSX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.VALLEY_NEGZ_NEGX && dss == MapData.STAIR_STAT.RIDGE_NEGZ_NEGX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.VALLEY_NEGZ_POSX && dss == MapData.STAIR_STAT.RIDGE_NEGZ_POSX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.VALLEY_POSZ_NEGX && dss == MapData.STAIR_STAT.RIDGE_POSZ_NEGX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.VALLEY_POSZ_POSX && dss == MapData.STAIR_STAT.RIDGE_POSZ_POSX) result = CheckResult.VALID_SLOPE_TO_SLOPE;

                        if (css == MapData.STAIR_STAT.RIDGE_NEGZ_NEGX && dss == MapData.STAIR_STAT.VALLEY_NEGZ_NEGX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.RIDGE_NEGZ_POSX && dss == MapData.STAIR_STAT.VALLEY_NEGZ_POSX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.RIDGE_POSZ_NEGX && dss == MapData.STAIR_STAT.VALLEY_POSZ_NEGX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.RIDGE_POSZ_POSX && dss == MapData.STAIR_STAT.VALLEY_POSZ_POSX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.RIDGE_NEGZ_NEGX && dss == MapData.STAIR_STAT.VALLEY_NEGZ_NEGX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.RIDGE_NEGZ_POSX && dss == MapData.STAIR_STAT.VALLEY_NEGZ_POSX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.RIDGE_POSZ_NEGX && dss == MapData.STAIR_STAT.VALLEY_POSZ_NEGX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.RIDGE_POSZ_POSX && dss == MapData.STAIR_STAT.VALLEY_POSZ_POSX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                    }
                }
            }
            else if (Math.Abs(ch - dh - 1) < 1)
            {
                //下のフロアのLV2に降りるので問題ない。
                if (css == MapData.STAIR_STAT.NONE && dss == MapData.STAIR_STAT.POS_Z && dz < 0) result = CheckResult.VALID;
                if (css == MapData.STAIR_STAT.NONE && dss == MapData.STAIR_STAT.NEG_Z && dz > 0) result = CheckResult.VALID;
                if (css == MapData.STAIR_STAT.NONE && dss == MapData.STAIR_STAT.POS_X && dx < 0) result = CheckResult.VALID;
                if (css == MapData.STAIR_STAT.NONE && dss == MapData.STAIR_STAT.NEG_X && dx > 0) result = CheckResult.VALID;

                if (dss == MapData.STAIR_STAT.VALLEY_NEGZ_NEGX && css == MapData.STAIR_STAT.NONE && (dx > 0 || dz > 0)) result = CheckResult.VALID;
                if (dss == MapData.STAIR_STAT.VALLEY_NEGZ_POSX && css == MapData.STAIR_STAT.NONE && (dx < 0 || dz > 0)) result = CheckResult.VALID;
                if (dss == MapData.STAIR_STAT.VALLEY_POSZ_NEGX && css == MapData.STAIR_STAT.NONE && (dx > 0 || dz < 0)) result = CheckResult.VALID;
                if (dss == MapData.STAIR_STAT.VALLEY_POSZ_POSX && css == MapData.STAIR_STAT.NONE && (dx < 0 || dz < 0)) result = CheckResult.VALID;

                //連続したスロープ
                if (css != MapData.STAIR_STAT.NONE && dss != MapData.STAIR_STAT.NONE)
                {
                    if (css == dss)//同じ向きのスロープ
                    {
                        //進行方向が正しければOK.
                        if (css == MapData.STAIR_STAT.NEG_Z && dz > 0) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.POS_Z && dz < 0) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.NEG_X && dx > 0) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.POS_X && dx < 0) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                    }
                    else
                    {
                        if (css == MapData.STAIR_STAT.RIDGE_NEGZ_NEGX && dss == MapData.STAIR_STAT.NEG_Z) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.RIDGE_NEGZ_POSX && dss == MapData.STAIR_STAT.NEG_Z) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.RIDGE_POSZ_NEGX && dss == MapData.STAIR_STAT.POS_Z) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.RIDGE_POSZ_POSX && dss == MapData.STAIR_STAT.POS_Z) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.RIDGE_NEGZ_NEGX && dss == MapData.STAIR_STAT.NEG_X) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.RIDGE_NEGZ_POSX && dss == MapData.STAIR_STAT.POS_X) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.RIDGE_POSZ_NEGX && dss == MapData.STAIR_STAT.NEG_X) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.RIDGE_POSZ_POSX && dss == MapData.STAIR_STAT.POS_X) result = CheckResult.VALID_SLOPE_TO_SLOPE;

                        if (css == MapData.STAIR_STAT.NEG_Z && dss == MapData.STAIR_STAT.VALLEY_NEGZ_NEGX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.NEG_Z && dss == MapData.STAIR_STAT.VALLEY_NEGZ_POSX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.POS_Z && dss == MapData.STAIR_STAT.VALLEY_POSZ_NEGX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.POS_Z && dss == MapData.STAIR_STAT.VALLEY_POSZ_POSX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.NEG_X && dss == MapData.STAIR_STAT.VALLEY_NEGZ_NEGX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.POS_X && dss == MapData.STAIR_STAT.VALLEY_NEGZ_POSX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.NEG_X && dss == MapData.STAIR_STAT.VALLEY_POSZ_NEGX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.POS_X && dss == MapData.STAIR_STAT.VALLEY_POSZ_POSX) result = CheckResult.VALID_SLOPE_TO_SLOPE;

                        if (css == MapData.STAIR_STAT.RIDGE_NEGZ_NEGX && dss == MapData.STAIR_STAT.VALLEY_NEGZ_NEGX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.RIDGE_NEGZ_POSX && dss == MapData.STAIR_STAT.VALLEY_NEGZ_POSX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.RIDGE_POSZ_NEGX && dss == MapData.STAIR_STAT.VALLEY_POSZ_NEGX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.RIDGE_POSZ_POSX && dss == MapData.STAIR_STAT.VALLEY_POSZ_POSX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.RIDGE_NEGZ_NEGX && dss == MapData.STAIR_STAT.VALLEY_NEGZ_NEGX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.RIDGE_NEGZ_POSX && dss == MapData.STAIR_STAT.VALLEY_NEGZ_POSX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.RIDGE_POSZ_NEGX && dss == MapData.STAIR_STAT.VALLEY_POSZ_NEGX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.RIDGE_POSZ_POSX && dss == MapData.STAIR_STAT.VALLEY_POSZ_POSX) result = CheckResult.VALID_SLOPE_TO_SLOPE;

                        if (css == MapData.STAIR_STAT.VALLEY_NEGZ_NEGX && dss == MapData.STAIR_STAT.RIDGE_NEGZ_NEGX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.VALLEY_NEGZ_POSX && dss == MapData.STAIR_STAT.RIDGE_NEGZ_POSX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.VALLEY_POSZ_NEGX && dss == MapData.STAIR_STAT.RIDGE_POSZ_NEGX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.VALLEY_POSZ_POSX && dss == MapData.STAIR_STAT.RIDGE_POSZ_POSX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.VALLEY_NEGZ_NEGX && dss == MapData.STAIR_STAT.RIDGE_NEGZ_NEGX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.VALLEY_NEGZ_POSX && dss == MapData.STAIR_STAT.RIDGE_NEGZ_POSX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.VALLEY_POSZ_NEGX && dss == MapData.STAIR_STAT.RIDGE_POSZ_NEGX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                        if (css == MapData.STAIR_STAT.VALLEY_POSZ_POSX && dss == MapData.STAIR_STAT.RIDGE_POSZ_POSX) result = CheckResult.VALID_SLOPE_TO_SLOPE;
                    }
                }
            }

            return result;
        }

        private static float getCollisionHeight(MapCharacter self, MapData map, EventHeightMap objMap, int x, int y, bool hitEvent)
        {
            float mapResult = map.getCollisionHeight(x, y);
            if (x >= objMap.width || 0 > x ||
                y >= objMap.height || 0 > y || !hitEvent)
                return mapResult;

            int result = 0;
            var cell = objMap.get(x, y);
            foreach (var info in cell)
            {
                if (info.chr == null)
                    continue;

                if (info.chr != self && info.col && info.chr.y <= self.y && info.chr.y + info.h > self.y)
                {
                    result = info.h;
                    break;
                }
            }

            return Math.Max(mapResult, result);
        }

        public bool IsMoving()
        {
            return offsetX != 0 || offsetZ != 0 || offsetY != 0;
        }

        public float getAdjustedYPosition(MapData map, float dx = -1, float dy = -1, float dz = -1)
		{
            if (dx < 0)
            {
                dx = x + offsetX;
                dy = y + offsetY;
                dz = z + offsetZ;
            }

			//高さをここで補正する
			if (map != null)
			{
                return map.getRoofAdjustedHeight(dx, dz, dy);
			}

			return 0;
		}
        
        // エディタ以外からの利用は非推奨
        public void update()
        {
            Update(null, 0, false);
        }

        public void Update(MapData map, float yangle, bool isLockDirection)
        {
            temporarySetVisiblityForUnity(false);

            float _speed = mMoveStep * GameMain.getRelativeParam60FPS();

            if (offsetY < 0)
            {
                offsetY += _speed;
                if (offsetY > 0) offsetY = 0.001f;  // わずかに浮かす事で、↓の getAdjustedYPosition の実行を抑制する
            }
            else if(offsetX != 0 || offsetZ != 0)
            {
                if (offsetX > 0)
                {
                    offsetX -= _speed;
                    if (offsetX < 0) offsetX = 0;
                }
                else if (offsetX < 0)
                {
                    offsetX += _speed;
                    if (offsetX > 0) offsetX = 0;
                }

                if (offsetZ > 0)
                {
                    offsetZ -= _speed;
                    if (offsetZ < 0) offsetZ = 0;
                }
                else if (offsetZ < 0)
                {
                    offsetZ += _speed;
                    if (offsetZ > 0) offsetZ = 0;
                }

                if (offsetY == 0)
                    y = getAdjustedYPosition(map);
            }
            else if (offsetY > 0)
            {
                offsetY -= _speed;
                if (offsetY < 0) offsetY = 0;
            }
            else if (!fixHeight)
            {
                y = getAdjustedYPosition(map);
            }

            // 向きの更新
            Vector3 cross = Vector3.Cross(mCurDir, mTgtDir);
            float dot = Vector3.Dot(mCurDir, mTgtDir);

            float angle = 0f;
            angle = cross.Y;
            if(dot<0)
            {
                if (angle > 0) angle += (float)Math.PI * 0.5f * GameMain.getRelativeParam60FPS();
				else angle -= (float)Math.PI * 0.5f * GameMain.getRelativeParam60FPS();
            }

            float maxrot = (float)Math.PI / 10f * GameMain.getRelativeParam60FPS();

            if (Math.Abs(angle) > maxrot)
            {
                angle = Math.Sign(angle) * maxrot;
            }

            Matrix m = Matrix.CreateRotationY(angle);
            mCurDir = Vector3.Transform(mCurDir, m);

            // ビルボードアニメ更新
            billboardAnimRemain -= (int)(1000 * GameMain.getElapsedTime());
            var chr = now2dMotion as Common.Resource.Character2DAnimSet;
            if (billboardAnimRemain <= 0 && chr != null)
            {
                billboardAnimRemain = chr.animationSpeed;

                switch (chr.animationType)
                {
                    case Common.Resource.Character.AnimationType.ANIMTYPE_LOOP:
                        iX++;
                        if (iX >= chr.animationFrame)
                        {
                            iX = 0;
                            if (bil != null)    // タイミングによっては既に開放されている事がある
                                bil.loopCount++;
                        }
                        break;
                    case Common.Resource.Character.AnimationType.ANIMTYPE_SINGLE:
                        iX++;
                        if (iX >= chr.animationFrame)
                        {
                            iX = chr.animationFrame - 1;
                            if (bil != null)    // タイミングによっては既に開放されている事がある
                                bil.loopCount++;
                        }
                        break;
                    case Common.Resource.Character.AnimationType.ANIMTYPE_REVERSE:
                        iX += mAnimCntIncl;
                        if (iX >= chr.animationFrame)
                        {
                            iX = chr.animationFrame - 2;
                            mAnimCntIncl *= -1;
                        }
                        if (iX < 0)
                        {
                            iX = 1;
                            mAnimCntIncl *= -1;
                            if(bil != null)     // タイミングによっては既に開放されている事がある
                                bil.loopCount++;
                        }
                        break;
                }
            }

            updatePosAngle(yangle, GameMain.getElapsedTime());
        }

        internal void forceRestartParticle()
        {
            if (ptcl != null)
            {
                ptcl.reset();
                ptcl.start(SharpKmyMath.Matrix4.translate(x + offsetX, y + offsetY, z + offsetZ));
            }
        }

        internal void updatePosAngle(float yangle, float elapsed)
        {
            if (mdl != null)
            {
                SharpKmyMath.Matrix4 lm = SharpKmyMath.Matrix4.translate(x + offsetX, y + offsetY, z + offsetZ);
                // マップオブジェクトの場合は、billboard 指定があるかどうかチェックする
                if (character is Common.Resource.MapObject &&
                    ((Common.Resource.MapObject)character).isBillboard)
                {
                    lm *= SharpKmyMath.Matrix4.rotateY(yangle * (float)Math.PI / 180);
                }
                else
                {
                    // マップオブジェクト以外は、普通に現在の向きを向かせる
                    lm *= SharpKmyMath.Matrix4.rotateY((float)Math.Atan2(mCurDir.X, mCurDir.Z));
                }
                mdl.inst.setL2W(lm);
                mdl.inst.update(elapsed);
                mdl.update();
            }
            if (bil != null)
            {
                int dir = mDirection;

                // 4 or 1方向
                if (bil.divY <= 4)
                {
                    int[] nextDir = { Util.DIR_SER_RIGHT, Util.DIR_SER_LEFT, Util.DIR_SER_UP, Util.DIR_SER_DOWN };
                    int shiftCount = (int)Math.Round(yangle / 90) % 4;
                    if (shiftCount < 0)
                        shiftCount += 4;
                    for (int i = 0; i < shiftCount; i++)
                        dir = nextDir[dir];
                }
                // 8方向
                else
                {
                    dir = (int)Math.Round(-mDirectionRad * 4 / Math.PI) + 4;
                    int shiftCount = (int)Math.Round(yangle / 45) % 8;
                    if (shiftCount < 0)
                        shiftCount += 8;
                    dir = (dir + shiftCount) % 8;
                    if (dir < 0)
                        dir += 8;
                }

                bil.setIndex(iX, dir);
                recentYAngle = yangle;
            }
            if (ptcl != null)
            {
                ptcl.update(elapsed, SharpKmyMath.Matrix4.translate(x + offsetX, y + offsetY, z + offsetZ));
            }

            if (shadowquad != null) shadowquad.setMatrix(SharpKmyMath.Matrix4.translate(x + offsetX, y + 0.1f, z + offsetZ));
        }

        private void temporarySetVisiblityForUnity(bool v)
        {
#if WINDOWS
#else
            if (mdl == null) return;
            if (mdl.inst == null) return;
            if (mdl.inst.instance == null) return;

            mdl.inst.setVisibility(v);
#endif
        }

        public bool draw(SharpKmyGfx.Render scn)
		{
            if (hide > HideCauses.NONE)
                return false;

            // モデルの色つけを利用していて、なおかつ色がゼロだったら描画しない
            if (useOverrideColor && overrideColor.a + overrideColor.r + overrideColor.g + overrideColor.b == 0)
                return false;

            temporarySetVisiblityForUnity(true);
            
            SharpKmyMath.Vector3 p = new SharpKmyMath.Vector3(x, y + offsetY, z);
            var size = 1.5f;
            if (ptcl != null)
            {
                size = 10f;
            }
            else if(bil != null)
            {
                size = bil.sz.Length();
            }
            else if (mdl != null)
            {
                size = Math.Max(size, mdl.inst.getModel().getSize().length());
            }
			if (!scn.viewVolumeCheck(p, size)) return false;

			if (mdl != null)
			{
				int idx = 0;

                // 主人公モデルは、描けなかったところを半透明で表現しなければならないので、特殊な処理を入れる
				if (mapHeroSymbol && MapData.sInstance != null && !MapData.sInstance.isShadowScene(scn))
				{
                    SharpKmyGfx.BLENDTYPE[] blendTypes = new SharpKmyGfx.BLENDTYPE[256];
                    SharpKmyGfx.Color[] colors = new SharpKmyGfx.Color[256];

                    // まずは普通に描く
					idx = 0;
                    mdl.reapplyMtlScrollState();
					while (true)
					{
						SharpKmyGfx.DrawInfo di = mdl.inst.getDrawInfo(idx);
						if (di == null) break;

						di.setStencilEnable(true);
						di.setStencilFunc(SharpKmyGfx.FUNCTYPE.kALWAYS, 1, ~0);
						di.setStencilOp(SharpKmyGfx.STENCILOP.kREPLACE, SharpKmyGfx.STENCILOP.kKEEP, SharpKmyGfx.STENCILOP.kREPLACE);
                        colors[idx] = di.getColor();
                        //di.setColor(new SharpKmyGfx.Color(1f, 1f, 1f, 1f));
						di.setLayer(1000);
                        di.setDepthFunc(SharpKmyGfx.FUNCTYPE.kLEQUAL);
                        blendTypes[idx] = di.getBlend();
						//di.setBlend(SharpKmyGfx.BLENDTYPE.kOPAQUE);
                        MapData.setFogParam(di);

                        idx++;
					}

					mdl.inst.draw(scn);

                    // Discard で描かれなかった部分のstencilも埋める
                    idx = 0;
                    while (true)
                    {
                        SharpKmyGfx.DrawInfo di = mdl.inst.getDrawInfo(idx);
                        if (di == null) break;

                        di.setShader(mSolidColorShader);
                        di.setStencilEnable(true);
                        di.setStencilFunc(SharpKmyGfx.FUNCTYPE.kALWAYS, 1, ~0);
                        di.setStencilOp(SharpKmyGfx.STENCILOP.kREPLACE, SharpKmyGfx.STENCILOP.kKEEP, SharpKmyGfx.STENCILOP.kREPLACE);
                        di.setColor(new SharpKmyGfx.Color(0.001f, 0.001f, 0.001f, 0.001f));
                        di.setDepthFunc(SharpKmyGfx.FUNCTYPE.kLEQUAL);
                        di.setBlend(SharpKmyGfx.BLENDTYPE.kADD);

                        idx++;
                    }

                    mdl.inst.draw(scn);

                    // 遮蔽物の向こう側を描く
					idx = 0;
					while (true)
					{
						SharpKmyGfx.DrawInfo di = mdl.inst.getDrawInfo(idx);
						if (di == null) break;

						di.setShader(mSolidColorShader);
						di.setStencilEnable(true);
                        if (useOverrideColor)
                        {
                            di.setStencilFunc(SharpKmyGfx.FUNCTYPE.kEQUAL, 1, ~0);
                        }
                        else
                        {
                            di.setStencilFunc(SharpKmyGfx.FUNCTYPE.kEQUAL, 0, ~0);//書けなかったところを抽出
                        }
						di.setStencilOp(SharpKmyGfx.STENCILOP.kINC, SharpKmyGfx.STENCILOP.kINC, SharpKmyGfx.STENCILOP.kINC);
#if WINDOWS
                        di.setColor(useOverrideColor ? overrideColor : new SharpKmyGfx.Color(0.95f, 0.95f, 0.95f, 0.35f));
#else
                        // TODO Unityでも遮蔽物の向こう側の描画に対応する
                        if (useOverrideColor)
                            di.setColor(overrideColor);
#endif
                        di.setDepthFunc(SharpKmyGfx.FUNCTYPE.kALWAYS);
                        di.setBlend(SharpKmyGfx.BLENDTYPE.kALPHA);
                        di.setLayer(1003);

                        idx++;
					}

					mdl.inst.draw(scn);

                    idx = 0;
                    while (true)
                    {
                        SharpKmyGfx.DrawInfo di = mdl.inst.getDrawInfo(idx);
                        if (di == null) break;
                        di.setBlend(blendTypes[idx]);
                        di.setShader(mBaseShader);
                        di.setColor(colors[idx]);
                        idx++;
                    }
				}

                // 主人公以外のモデルの描画処理
				else
				{
					idx = 0;
					while (true)
					{
						SharpKmyGfx.DrawInfo di = mdl.inst.getDrawInfo(idx);
						if (di == null) break;
						idx++;

						di.setStencilEnable(false);
						//di.setColor(new SharpKmyGfx.Color(1f,1f,1f,1f));
						di.setDepthFunc(SharpKmyGfx.FUNCTYPE.kLEQUAL);
						//di.setBlend(SharpKmyGfx.BLENDTYPE.kOPAQUE);
                        di.setLayer(1001);
                        MapData.setFogParam(di);
					}
					mdl.inst.draw(scn);
				}
	
			}
			else if (bil != null)
			{
                SharpKmyMath.Vector3 pos = new SharpKmyMath.Vector3(x + offsetX, y + offsetY, z + offsetZ);
                if (now2dMotion != null && DO_SUBMERGE_2D_MOTION_NAME.Contains(now2dMotion.name))
                    pos.y -= 0.5f;
				bil.mOrigin = pos;
                bil.draw(scn);
                if (shadowquad != null) shadowquad.draw(scn);
			}
			else if(ptcl != null)
			{
				var dlist = ptcl.getDrawInfo();
				if (dlist != null)
				{
					foreach (var i in dlist)
					{
						MapData.setFogParam(i);
					}
				}
				ptcl.draw(scn);
			}else if (mTentativeBox != null)
			{
                mTentativeBox.setMatrix(SharpKmyMath.Matrix4.translate(x + offsetX, y + offsetY, z + offsetZ));
				mTentativeBox.draw(scn);
			}

            return true;
		}

        public void ChangeColor(byte r, byte g, byte b, byte a)
        {
            var col = new SharpKmyGfx.Color(
                    (float)r / 255,
                    (float)g / 255,
                    (float)b / 255,
                    (float)a / 255);
            if (bil != null)
            {
                bil.color = col;
                overrideColor = col;
            }
            else if (mdl != null)
            {
                if (r + g + b + a == 255 * 4)
                    col.a = 0;
                overrideColor = col;
            }
        }

        internal bool IsRotating()
        {
            return (mCurDir - mTgtDir).Length() > 0.001f;
        }

        public Common.Resource.ResourceItem getGraphic()
        {
            return character;
        }

        public ModifiedModelInstance getModelInstance()
        {
            return mdl;
        }

        public void setBillboardSize(float x, float y)
        {
            if (bil != null)
                bil.sz = new Vector2(x, y);
        }

        public void setBlend(SharpKmyGfx.BLENDTYPE type)
        {
            if (bil != null)
                bil.blend = type;
        }

        internal float getHeight()
        {
            if (mdl != null)
            {
                return mdl.inst.getModel().getCenter().y * 2;
            }
            else if(bil != null)
            {
                return bil.sz.Y;
            }

            return 0;
        }

        internal bool contains2dMotion(string motion)
        {
            // 空白だった場合は、デフォルトモーションの事だとみなして true を返す
            if (string.IsNullOrEmpty(motion))
                return true;

            var image = character as Common.Resource.Character;
            if (image == null)
                return false;

            // モーションがあれば true
            return image.subItemList.Exists(x => x.name == motion);
        }

        internal Vector3 getOffset()
        {
            return new Vector3(offsetX, offsetY, offsetZ);
        }
    }
}

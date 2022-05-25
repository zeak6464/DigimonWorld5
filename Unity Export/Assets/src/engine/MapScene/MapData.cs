using System;
using System.Collections.Generic;
using System.Linq;
using Yukar.Common.Rom;
using Microsoft.Xna.Framework;

#if WINDOWS
#else
using Eppy;
#endif

namespace Yukar.Engine
{
    using System.Collections;

#if WINDOWS
    using Light = SharpKmyGfx.Light;
#else
    using Light = SharpKmyGfx.Light;
#endif

    public class ChipInfo
    {
        public int index;
        public Common.Resource.MapChip info;
        public List<SharpKmyMath.Vector2> uvlist = new List<SharpKmyMath.Vector2>();
        public Color repColor = Color.White;
        public int animIndex = 0;
        public uint[] srcdata;
    }

    public class StairInfo
    {
        public int index;
        public Common.Resource.MapChip info;
        public List<SharpKmyMath.Vector2> uvlist = new List<SharpKmyMath.Vector2>();
        public Color repColor = Color.White;
    }

    public class MapObjectInstance
    {
        public Common.Rom.Map.MapObjectRef info;
        //public SharpKmyGfx.StaticModelBatcher multiinstance;
        public ModifiedModelInstance minst = null;
        public SharpKmyGfx.BoxFillPrimitive mTentativeBox = null;
        public Common.Resource.MapObject mo;
        public SharpKmyMath.Vector3 lastPosition;
        public float[] uscroll;
        public float[] vscroll;
        public MapObjectInstance()
        {
            uscroll = new float[32];
            vscroll = new float[32];
            for (int i = 0; i < 32; i++)
            {
                uscroll[i] = 0;
                vscroll[i] = 0;
            }
        }
    }

    public class ChipSetInfo
    {
        public List<ChipInfo> chipVariations = new List<ChipInfo>();
        public List<StairInfo> stairVariations = new List<StairInfo>();
        public List<SharpKmyGfx.Texture> t_temp = new List<SharpKmyGfx.Texture>();
        public List<SharpKmyGfx.Texture> g_temp = new List<SharpKmyGfx.Texture>();

        public void Reset()
        {
            foreach (SharpKmyGfx.Texture t in t_temp)
            {
                t.Release();
            }

            foreach (SharpKmyGfx.Texture t in g_temp)
            {
                t.Release();
            }

            t_temp.Clear();
            g_temp.Clear();
            chipVariations.Clear();
            stairVariations.Clear();
        }

        public Color getTerrainReplesentativeColor(Guid guid)
        {
            foreach (ChipInfo ci in chipVariations)
            {
                if (ci.info.wave) continue;

                if (ci.info.guId == guid)
                {
                    return ci.repColor;
                }
            }
            return Color.Gray;
        }

        public SharpKmyGfx.Texture build(Common.Rom.Map map)
        {
            Reset();

            SharpKmyGfx.Texture ret = new SharpKmyGfx.Texture();

            var catalog = Common.Catalog.sInstance;

            //サイズを確定する
            //それぞれの高さも保存する
            Dictionary<int, Guid> chips = map.getChipList();
            Dictionary<int, Guid> stairs = map.getStairList();
            if (chips.Count > 0 || stairs.Count > 0)
            {
                int count = 0;
                foreach (var chip in chips)
                {
                    var guid = chip.Value;
                    var ci = catalog.getItemFromGuid(guid) as Common.Resource.MapChip;
                    if (ci == null)
                        continue;

                    var img = SharpKmyGfx.Texture.load(ci.path);

                    if (img != null)
                    {
                        t_temp.Add(img);

                        int h = t_temp.Last().getHeight() / 48;
                        chipVariations.Add(new ChipInfo());
                        chipVariations.Last().index = chip.Key;
                        chipVariations.Last().info = ci;
                        count += h;
                    }
                }

                foreach (var item in stairs)
                {
                    var si = catalog.getItemFromGuid(item.Value) as Common.Resource.MapChip;
                    if (si == null)
                        continue;

                    var img = SharpKmyGfx.Texture.load(si.path);
                    if (img != null)
                    {
                        g_temp.Add(img);

                        int h = g_temp.Last().getHeight() / 48;
                        stairVariations.Add(new StairInfo());
                        stairVariations.Last().index = item.Key;
                        stairVariations.Last().info = si;
                        count += h;
                    }
                }

                int height = (count + 9) / 10 * 64/*48??*/;
                ret.create(512, height);

                //コピーしていく。
                int xofs = 0;
                int yofs = 0;
                foreach (SharpKmyGfx.Texture t in t_temp)
                {
                    ChipInfo ci = chipVariations[t_temp.IndexOf(t)];

                    int h = t.getHeight() / 48;
                    uint[] src = new uint[t.getHeight() * t.getWidth()];
                    //t.getColor(src);
                    SharpKmyGfx.Texture.getColor(ci.info.path, src);

                    uint rc = src[src.Length - 1];
                    ci.repColor = new Color((int)((rc >> 0) & 0xff), (int)((rc >> 8) & 0xff), (int)((rc >> 16) & 0xff), 255);
                    ci.srcdata = src;

                    for (int c = 0; c < h; c++)
                    {
                        ci.uvlist.Add(new SharpKmyMath.Vector2(xofs, yofs));
                        ret.storeSubPixel2D(xofs, height - yofs - 48, 48, 48, src, SharpKmyGfx.TEXTUREFFORMAT.kRGBA, 0, (h - 1 - c) * 48, t.getWidth());
                        xofs += 48;
                        if (xofs + 47 >= 512)
                        {
                            xofs = 0;
                            yofs += 48;
                        }
                    }
                }

                foreach (SharpKmyGfx.Texture t in g_temp)
                {
                    int h = t.getHeight() / 48;
                    uint[] src = new uint[t.getHeight() * t.getWidth()];
                    //t.getColor(src);
                    StairInfo si = stairVariations[g_temp.IndexOf(t)];
                    SharpKmyGfx.Texture.getColor(si.info.path, src);

                    uint rc = src[src.Length - 1];
                    stairVariations[g_temp.IndexOf(t)].repColor = new Color((int)((rc >> 16) & 0xff), (int)((rc >> 8) & 0xff), (int)((rc >> 0) & 0xff), 255);

                    for (int c = 0; c < h; c++)
                    {
                        stairVariations[g_temp.IndexOf(t)].uvlist.Add(new SharpKmyMath.Vector2(xofs, yofs));
                        ret.storeSubPixel2D(xofs, height - yofs - 48, 48, 48, src, SharpKmyGfx.TEXTUREFFORMAT.kRGBA, 0, (h - 1 - c) * 48, t.getWidth());
                        xofs += 48;
                        if (xofs + 47 >= 512)
                        {
                            xofs = 0;
                            yofs += 48;
                        }
                    }
                }

                return ret;
            }

            ret.create(256, 256);
            return ret;
        }

#if WINDOWS
        public System.Drawing.Bitmap exportMapTexture(Common.Rom.Map map)
        {
            Reset();

            var catalog = Common.Catalog.sInstance;
            System.Drawing.Bitmap ret;

            //サイズを確定する
            //それぞれの高さも保存する
            Dictionary<int, Guid> chips = map.getChipList();
            Dictionary<int, Guid> stairs = map.getStairList();
            if (chips.Count > 0 || stairs.Count > 0)
            {
                int count = 0;
                foreach (var chip in chips)
                {
                    var guid = chip.Value;
                    var ci = catalog.getItemFromGuid(guid) as Common.Resource.MapChip;
                    if (ci == null)
                        continue;

                    var img = SharpKmyGfx.Texture.load(ci.path);

                    if (img != null)
                    {
                        t_temp.Add(img);

                        int h = t_temp.Last().getHeight() / 48;
                        chipVariations.Add(new ChipInfo());
                        chipVariations.Last().index = chip.Key;
                        chipVariations.Last().info = ci;
                        count += h;
                    }
                }

                foreach (var item in stairs)
                {
                    var si = catalog.getItemFromGuid(item.Value) as Common.Resource.MapChip;
                    if (si == null)
                        continue;

                    var img = SharpKmyGfx.Texture.load(si.path);
                    if (img != null)
                    {
                        g_temp.Add(img);

                        int h = g_temp.Last().getHeight() / 48;
                        stairVariations.Add(new StairInfo());
                        stairVariations.Last().index = item.Key;
                        stairVariations.Last().info = si;
                        count += h;
                    }
                }

                int height = (count + 9) / 10 * 64/*48??*/;
                ret = new System.Drawing.Bitmap(512, height);
                System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(ret);
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;

                //コピーしていく。
                int xofs = 0;
                int yofs = 0;
                foreach (SharpKmyGfx.Texture t in t_temp)
                {
                    int h = t.getHeight() / 48;
                    ChipInfo ci = chipVariations[t_temp.IndexOf(t)];
                    System.Drawing.Bitmap tmp = new System.Drawing.Bitmap(ci.info.path);

                    uint[] src = new uint[t.getHeight() * t.getWidth()];
                    SharpKmyGfx.Texture.getColor(ci.info.path, src);

                    uint rc = src[src.Length - 1];
                    ci.repColor = new Color((int)((rc >> 0) & 0xff), (int)((rc >> 8) & 0xff), (int)((rc >> 16) & 0xff), 256);
                    ci.srcdata = src;

                    for (int c = 0; c < h; c++)
                    {
                        System.Drawing.Rectangle srcD = new System.Drawing.Rectangle(0, c * 48, 48, 48);
                        System.Drawing.Rectangle dstD = new System.Drawing.Rectangle(xofs, yofs, 48, 48);
                        g.DrawImage(tmp, dstD, srcD, System.Drawing.GraphicsUnit.Pixel);

                        ci.uvlist.Add(new SharpKmyMath.Vector2(xofs, yofs));

                        xofs += 48;
                        if (xofs + 47 >= 512)
                        {
                            xofs = 0;
                            yofs += 48;
                        }
                    }

                    tmp.Dispose();
                }

                foreach (SharpKmyGfx.Texture t in g_temp)
                {
                    int h = t.getHeight() / 48;
                    StairInfo si = stairVariations[g_temp.IndexOf(t)];
                    System.Drawing.Bitmap tmp = new System.Drawing.Bitmap(si.info.path);

                    uint[] src = new uint[t.getHeight() * t.getWidth()];
                    SharpKmyGfx.Texture.getColor(si.info.path, src);

                    uint rc = src[src.Length - 1];
                    stairVariations[g_temp.IndexOf(t)].repColor =
                        new Color((int)((rc >> 16) & 0xff), (int)((rc >> 8) & 0xff), (int)((rc >> 0) & 0xff), 255);

                    for (int c = 0; c < h; c++)
                    {
                        System.Drawing.Rectangle srcD = new System.Drawing.Rectangle(0, c * 48, 48, 48);
                        System.Drawing.Rectangle dstD = new System.Drawing.Rectangle(xofs, yofs, 48, 48);
                        g.DrawImage(tmp, dstD, srcD, System.Drawing.GraphicsUnit.Pixel);

                        stairVariations[g_temp.IndexOf(t)].uvlist.Add(new SharpKmyMath.Vector2(xofs, yofs));

                        xofs += 48;
                        if (xofs + 47 >= 512)
                        {
                            xofs = 0;
                            yofs += 48;
                        }
                    }

                    tmp.Dispose();
                }
                g.Dispose();

                return ret;
            }

            ret = new System.Drawing.Bitmap(256, 256);
            return ret;
        }
#endif

        public Common.Resource.MapChip getInfo(int attr)
        {
            foreach (var chip in chipVariations)
            {
                if (chip.index == attr)
                    return chip.info;
            }
            return null;
        }
    }

    public delegate void MapDrawCallBack(SharpKmyGfx.Render scn, bool isShadowScene);

    public class MapData : SharpKmyGfx.Drawable
    {
        public enum STAIR_STAT
        {
            NONE = -1,
            POS_Z,
            NEG_Z,
            POS_X,
            NEG_X,

            RIDGE_POSZ_POSX,
            RIDGE_NEGZ_NEGX,
            RIDGE_NEGZ_POSX,
            RIDGE_POSZ_NEGX,

            VALLEY_POSZ_POSX,
            VALLEY_NEGZ_NEGX,
            VALLEY_NEGZ_POSX,
            VALLEY_POSZ_NEGX,
        };
        public static int TIPS_IN_CLUSTER = 16;
        const int s_bbMax = 1024;
        const int s_evMax = 1024;

        private int MIN_HEIGHT = 1;
        private int MAX_HEIGHT = 20;

        MapClusterBuilder[,] mClusters;
        int mHcount, mDcount;
        //SharpKmyGfx.VertexPositionTextureColor[] bb_new_geometry;
        SharpKmyGfx.VertexBuffer bb_new_vertexBuffer;
        public Map mapRom;

        public event MapDrawCallBack mapDrawCallBack;

        SharpKmyGfx.Texture maptexture;
        SharpKmyGfx.Texture masktexture;
        SharpKmyGfx.Shader terrainShader;
        SharpKmyGfx.Shader waterShader;
        Light light = null;
        SharpKmyMath.Vector3 shadowcenter = new SharpKmyMath.Vector3();

        WaterMesh waterMesh = null;
        public SkyDrawer sky = null;
        EnvironmentEffect envEffect = null;

        //ビューボリュームクリップ用
        SharpKmyMath.Vector3 lvn;
        SharpKmyMath.Vector3 rvn;
        SharpKmyMath.Vector3 bvn;
        SharpKmyMath.Vector3 tvn;
        float lx;
        float rx;
        float tx;
        float bx;
        float nearZ, farZ;

        public ChipSetInfo chipSetInfo = new ChipSetInfo();
        public bool shadowUneditableArea = false;
        public bool isExpandViewRange = false;
        public bool isEditor = false;
        public static MapData sInstance;
        public static SharpKmyGfx.Render pickupscene = null;
        List<MapObjectInstance> mapobjects = new List<MapObjectInstance>();

        //List<ModifiedModelData> mapobjectmodels = new List<ModifiedModelData>();
        //List<SharpKmyGfx.StaticModelBatcher> multimodelinstances = new List<SharpKmyGfx.StaticModelBatcher>();
        //bool onInitialize = false;
        float elapsedTime = 0;
        Map currentrom = null;

        //FBXエクスポート
        //System.String exportDir;

        //デバッグ
        float elapsedSum = 0;
        float lastElapsed = 60;
        int elapsedCount = 0;
        SharpKmyGfx.SpriteBatch sprite;
        SharpKmyGfx.Font fnt;
        bool mShowPerformance = false;
        SharpKmyGfx.DrawInfo di = null;
        internal SharpKmyGfx.DrawInfo GetDrawInfo() { return di; }

        public enum PICKUP_DRAW_MAPOBJECT
        {
            kALL,
            kBUILDING_ONLY,
            kNONE,
        };

        public PICKUP_DRAW_MAPOBJECT drawMapObject = PICKUP_DRAW_MAPOBJECT.kALL;//0:書かない

        public const int MASKMAPSIZE = 512;

        public Func<int, int, string, bool> progressListener;
        private int progress = 0;
        private int maxProgress = 100;
        private void reportProgress(string state = "")
        {
            if (progressListener != null)
                progressListener(progress++, maxProgress, state);
        }
        private void reportMaxProgress(int max)
        {
            maxProgress = max;
        }

        public float getAspect()
        {
            return (float)Graphics.sInstance.mWidth / Graphics.sInstance.mHeight;
        }

        internal SkyDrawer GetSkyDrawer()
        {
            return this.sky;
        }

        internal EnvironmentEffect GetEnvironmentEffect()
        {
            return this.envEffect;
        }

        public SharpKmyMath.Vector3 GetEnvironmentEffectPos()
        {
            var envEffectPos = shadowcenter;
            envEffectPos.y = getAdjustedHeight(shadowcenter.x, shadowcenter.z);
            return envEffectPos;
        }


        public bool ShowPerformance
        {
            get { return mShowPerformance; }
            set { mShowPerformance = value; }
        }

        public int getClusterHCount() { return mHcount; }
        public int getClusterDCount() { return mDcount; }

        public SharpKmyMath.Rectangle GetDrawArea()
        {
            var size = new SharpKmyMath.Rectangle(0, 0, mHcount * MapData.TIPS_IN_CLUSTER, mDcount * MapData.TIPS_IN_CLUSTER);

            switch (mapRom.outsideType)
            {
                case Map.OutsideType.MAPCHIP:
                    size.x -= MapData.TIPS_IN_CLUSTER;
                    size.y -= MapData.TIPS_IN_CLUSTER;
                    size.width += MapData.TIPS_IN_CLUSTER;
                    size.height += MapData.TIPS_IN_CLUSTER;
                    break;

                case Map.OutsideType.REPEAT:
                    size.x -= mapRom.Width;
                    size.y -= mapRom.Height;
                    size.width += mapRom.Width;
                    size.height += mapRom.Height;
                    break;
            }


            return size;
        }

        public void updateUndoGeometry(int x, int y, int w, int h)
        {
            int cx = x / TIPS_IN_CLUSTER;
            int cz = y / TIPS_IN_CLUSTER;
            int ex = (x + w) / TIPS_IN_CLUSTER + 1;
            int ez = (y + h) / TIPS_IN_CLUSTER + 1;

            if (ex > mClusters.GetLength(0))
                ex = mClusters.GetLength(0);
            if (ez > mClusters.GetLength(1))
                ez = mClusters.GetLength(1);

            for (int _z = cz; _z < ez; _z++)
            {
                for (int _x = cx; _x < ex; _x++)
                {
                    callUpdateGeometry(mClusters[_x, _z]);
                }
            }
            updateMapHeightAndWalkableState();
        }

        public MapData()
        {
#if WINDOWS
            di = new SharpKmyGfx.DrawInfo();
#else
            if ( UnityEntry.IsDivideMapScene() == false
            || UnityEntry.IsImportMapScene())
            {
                di = new SharpKmyGfx.DrawInfo();
            }
#endif

            restoreStaticInstance();
            terrainShader = SharpKmyGfx.Shader.load("terrain");
            waterShader = SharpKmyGfx.Shader.load("map");
            masktexture = new SharpKmyGfx.Texture();
            masktexture.create(MASKMAPSIZE, MASKMAPSIZE);
            clearTerrainSelection();

            sprite = new SharpKmyGfx.SpriteBatch();
            fnt = SharpKmyGfx.Font.newSystemFont(System.Text.Encoding.Unicode.GetBytes("Arial"), 10, 512, 512);

            //exportDir = System.IO.Directory.GetCurrentDirectory();
        }

        public void restoreStaticInstance()
        {
            sInstance = this;
        }

        const string INVALID_NAME_REGEX =
            "[\\x00-\\x1f<>:\"/\\\\|?*]" +
            "|^(CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9]|CLOCK\\$)(\\.|$)" +
            "|[\\. |\\.. ]";
#if false
        static int testCnt = 0;
        static string[] testValue ={
            ".","..","/","CON",
            "/a/bb//ccc", "..a", "b..","..c..","..d../?"
        };
#endif

        public static bool isValidName(String inName)
        {
            var isMatch = System.Text.RegularExpressions.Regex.IsMatch(inName,
                INVALID_NAME_REGEX, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return isMatch == false;
        }


        public static string convertValidName(Map inMap)
        {
            var name = inMap.mapSceneName;
            if (string.IsNullOrEmpty(name))
                name = inMap.guId.ToString();
            return name + ".unity";
        }

        public IEnumerator setRom(Map data, bool isForcedUpdate = false)
        {
            if (!isEditor && currentrom == data && !isForcedUpdate) yield break;
            var coroutine = setRomImpl(data);
            while (coroutine.MoveNext()) yield return null;
            currentrom = data;
        }

        public Map getRom()
        {
            return currentrom;
        }

        public IEnumerator setRomImpl(Map data, bool isChangeScene = false)
        {
            var isLoadScene = true;
            progress = 0;
            reportProgress();

#if WINDOWS
            Reset();

            SharpKmy.Entry.gfxMemoryCleanup();
#else
            if (UnityEntry.IsDivideMapScene() == false)
            {
                Reset();
                SharpKmy.Entry.gfxMemoryCleanup();
            }
            else
            {
                isLoadScene = UnityEntry.IsImportMapScene();

                Reset();

                if (isLoadScene == false)
                {
                    SharpKmy.Entry.gfxMemoryCleanup();

                    if (sky != null) sky.Reset();

                    //解放
                    Yukar.Common.UnityUtil.unloadSceneAsync();
                    yield return null;
                    while (Yukar.Common.UnityUtil.isUnloadScene() == false)
                    {
                        yield return null;
                    }

                    //読み込み
                    Yukar.Common.UnityUtil.loadSceneAsync(data);
                    yield return null;
                    while(Yukar.Common.UnityUtil.isLoadScene() == false)
                    {
                        yield return null;
                    }
                }
            }
#endif

            mapRom = data;

            mHcount = (data.Width + MapData.TIPS_IN_CLUSTER - 1) / MapData.TIPS_IN_CLUSTER;
            mDcount = (data.Height + MapData.TIPS_IN_CLUSTER - 1) / MapData.TIPS_IN_CLUSTER;

            reportMaxProgress(mHcount * mDcount * 2);

            mClusters = new MapClusterBuilder[mHcount, mDcount];

            //ビルボードのリアルタイムバッファ
            //bb_new_geometry = new SharpKmyGfx.VertexPositionTextureColor[s_bbMax * 6];
            bb_new_vertexBuffer = new SharpKmyGfx.VertexBuffer();

            maptexture = chipSetInfo.build(data);
            // 背景チップの初期値が海じゃなくなった時はこのコメントアウトを外す
            //if (data.getChipList().Count == 0)
            //    getChipIndex(Common.Catalog.sInstance.getItemFromGuid(getSeaGuid()) as Common.Resource.MapChip);

            if (isLoadScene)
            {
                light = Light.createDirection(
                    new SharpKmyGfx.Color(data.dirLightColor.R / 255f, data.dirLightColor.G / 255f, data.dirLightColor.B / 255f, 1f),
                    1,
                    getLightMatrix(),
                    30,
                    0,
                    60,
                    1024//ライトマップサイズ
                    );

                fogColor = new SharpKmyGfx.Color(data.fogColor[0], data.fogColor[1], data.fogColor[2], data.fogColor[3]);

                waterMesh = new WaterMesh(this);

#if WINDOWS
#else
                if (!UnityEntry.IsReimportMapScene())
#endif
                sky = new SkyDrawer(this);

                envEffect = new EnvironmentEffect(this);
            }

            for (int x = 0; x < mHcount; x++)
            {
                for (int z = 0; z < mDcount; z++)
                {
                    mClusters[x, z] = new MapClusterBuilder(x, z, this, data);
                    reportProgress(String.Format("Terrain {0}/{1}", x * mDcount + z + 1, mHcount * mDcount));

                    yield return null;
                }
            }

            //マップオブジェクトインスタンス
            if (isLoadScene)
            {
                int mid = 0;
                //リスト検索が地味に重たいので、呼び出し側で用意します。
                var list = Common.Catalog.sInstance.getFilteredItemList(typeof(Common.Resource.MapObject));
                //onInitialize = true;//初期化中はインスタンス数を設定しない
                while (true)
                {
                    Common.Rom.Map.MapObjectRef r = mapRom.getMapObject(mid++);
                    if (r == null) break;
                    //var loadedPath = 
                    addMapObjectInstance(r, list);
                    //reportProgress(loadedPath);

                    //var cluster = getClusterByPosition(r.x, r.y);

                    yield return null;
                }
                //onInitialize = false;

                recalcMapObjectInstanceMaxCount();
            }

            updateMapHeightAndWalkableState();

            if (isLoadScene)
            {
                int index = 0;

                while (true)
                {
                    var stair = mapRom.getStair(index);

                    if (stair == null) break;

                    if (stair.stat < (int)STAIR_STAT.NONE || stair.stat > (int)STAIR_STAT.VALLEY_POSZ_NEGX)
                    {
                        stair.stat = getStairDirAuto(stair.x, stair.y);
                    }

                    index++;
                }

                for (int x = 0; x < mHcount; x++)
                {
                    for (int z = 0; z < mDcount; z++)
                    {
                        //mClusters[x, z].geomInvalidate();//キャッシュがある場合はそれを使うのでここで無効化はしない。
                        mClusters[x, z].updateGeometry();
                        reportProgress(String.Format("Geometry {0}/{1}", x * mDcount + z + 1, mHcount * mDcount));
                        yield return null;
                    }
                }
            }
        }

        public bool isShadowScene(SharpKmyGfx.Render scn)
        {
            if (light == null)
                return false;
            return SharpKmyGfx.Render.isSameScene(light.getShadowMapRender(), scn);
        }

        public void setShadowCenter(SharpKmyMath.Vector3 p)
        {
            shadowcenter = p;
            if (light != null)
                light.setPosture(getLightMatrix());
        }

        public SharpKmyMath.Matrix4 getLightMatrix()
        {
            return SharpKmyMath.Matrix4.translate(shadowcenter.x, shadowcenter.y, shadowcenter.z) *
                SharpKmyMath.Matrix4.rotateY(mapRom.shadowRotateY) *
                SharpKmyMath.Matrix4.rotateX(mapRom.shadowRotateX) *
                SharpKmyMath.Matrix4.translate(0, 0, 10 + 20 * 0.6f / -mapRom.shadowRotateX);
        }

        public void Reset()
        {
            if (mClusters != null)
            {
                for (int x = 0; x < mHcount; x++)
                {
                    for (int z = 0; z < mDcount; z++)
                    {
                        mClusters[x, z].Dispose();
                    }
                }
                mClusters = null;
            }

            foreach (var m in mapobjects)
            {
                if (m.mTentativeBox != null) m.mTentativeBox.Release();
                if (m.minst != null) Graphics.UnloadModel(m.minst);
            }
            mapobjects.Clear();

            chipSetInfo.Reset();

            if (maptexture != null) maptexture.Release();
            maptexture = null;

            if (bb_new_vertexBuffer != null)
            {
                bb_new_vertexBuffer.Release();
                bb_new_vertexBuffer = null;
            }

            //bb_new_geometry = null;

            if (waterMesh != null)
            {
                waterMesh.Reset();
                waterMesh = null;
            }

            if (sky != null)
            {
                sky.Reset();
                sky = null;
            }

            if (envEffect != null)
            {
                envEffect.Reset();
                envEffect = null;
            }

            if (light != null)
            {
                light.Release();
                light = null;
            }
            /*
            foreach (var m in multimodelinstances)
            {
                m.Release();
            }
            multimodelinstances.Clear();
             */
            /*
            foreach(var m in mapobjectmodels)
            {
                m.mdl.Release();
            }
            mapobjectmodels.Clear();
             */
            // 回転マップキャッシュをクリア
            heightMapCache.Clear();

            currentrom = null;
        }

        public void Destroy()
        {
            Reset();
            if (di != null) di.Release();
            terrainShader.Release();
            waterShader.Release();
            masktexture.Release();
            Release();
            sprite.Release();
            fnt.Release();

            di = null;
            terrainShader = null;
            waterShader = null;
            masktexture = null;
            sprite = null;
            fnt = null;
            sInstance = null;
            chipSetInfo = null;
            mapobjects = null;
            mapRom = null;
        }

        public SharpKmyMath.Vector2 getSize()
        {
            return new SharpKmyMath.Vector2(mapRom.Width, mapRom.Height);
        }

        uint[,] maskColorMap = new uint[256, 256];
        public const uint COLOR_CLEAR = 0xff000000;
        public const uint COLOR_EVENT_RANGE = 0x89000088;

        public void clearTerrainSelection()
        {
            uint[] colors = new uint[MASKMAPSIZE * MASKMAPSIZE];
            for (int i = 0; i < MASKMAPSIZE * MASKMAPSIZE; i++)
            {
                colors[i] = COLOR_CLEAR;
            }
            for (int i = 0; i < 256 * 256; i++)
            {
                maskColorMap[i % 256, i / 256] = COLOR_CLEAR;
            }
            masktexture.storeSubPixel2D(0, 0, MASKMAPSIZE, MASKMAPSIZE, colors, SharpKmyGfx.TEXTUREFFORMAT.kRGBA, 0, 0, MASKMAPSIZE);
        }

        public uint getTerrainSelectionColor(int x, int z)
        {
            if ((x < 0) || (z < 0) || (x >= maskColorMap.GetLength(0)) || (z >= maskColorMap.GetLength(1)))
            {
                return 0;
            }

            return maskColorMap[x, z];
        }

        public void setTerrainSelectionColor(int x, int z, int w, int h, uint color)
        {
            if (x < 0 || z < 0 || w == 0 || h == 0)
                return;

            if (maskColorMap[x, z] == color &&
                maskColorMap[x + w - 1, z + h - 1] == color)
                return;
            // 矩形選択している際にかなり高頻度でこの関数が呼び出されるのでイベントの範囲も上書きしないようにした
            if (maskColorMap[x, z] == COLOR_EVENT_RANGE &&
                maskColorMap[x + w - 1, z + h - 1] == COLOR_EVENT_RANGE)
            {
                return;
            }
            uint[] colors = new uint[w * h];
            for (int i = 0; i < w * h; i++)
            {
                colors[i] = color;
            }
            masktexture.storeSubPixel2D(x, z, w, h, colors, SharpKmyGfx.TEXTUREFFORMAT.kRGBA, 0, 0, w);

            maskColorMap[x, z] = color;
            //for (var i = x; i < x + w; ++i)
            //{
            //    for (var j = z; j < z + h; ++j)
            //    {
            //        maskColorMap[i, j] = color;
            //    }
            //}
        }

        public void clearTerrainSelectionColor(uint color)
        {
            var w = mapRom.Width;
            var h = mapRom.Height;

            uint[] colors = new uint[w * h];
            for (int i = 0; i < w * h; i++)
            {
                colors[i] = color;
            }
            masktexture.storeSubPixel2D(0, 0, w, h, colors, SharpKmyGfx.TEXTUREFFORMAT.kRGBA, 0, 0, w);

            for (var i = 0; i < w; ++i)
            {
                for (var j = 0; j < h; ++j)
                {
                    maskColorMap[i, j] = color;
                }
            }
        }

        public void deselectTerrain(int x, int z, int w, int h)
        {
            if (maskColorMap[x, z] == COLOR_CLEAR &&
                maskColorMap[x + w - 1, z + h - 1] == COLOR_CLEAR)
                return;

            uint[] colors = new uint[w * h];
            for (int i = 0; i < w * h; i++)
            {
                colors[i] = COLOR_CLEAR;
            }
            masktexture.storeSubPixel2D(x, z, w, h, colors, SharpKmyGfx.TEXTUREFFORMAT.kRGBA, 0, 0, w);

            maskColorMap[x, z] = COLOR_CLEAR;
        }

        public void setEventRangeColor(List<Map.EventRef> events)
        {
            foreach (var eventRef in events)
            {
                setEventRangeColor(eventRef, COLOR_EVENT_RANGE);
            }

        }

        private void setEventRangeColor(Map.EventRef eventRef, uint color)
        {
            var catalog = Common.Catalog.sInstance;
            var eventRom = catalog.getItemFromGuid(eventRef.guId) as Event;
            if (eventRom.sheetList.Count == 0)
                return;
            var sheet = eventRom.sheetList[0];

            // dailyの物がすべて255でも-1に設定し直せないので残しておく
            if (sheet.movingLimitLeft == 255 && sheet.movingLimitRight == 255 && sheet.movingLimitUp == 255 && sheet.movingLimitDown == 255)
            {
                return;
            }
            if(!sheet.isUsingMovingLimit())
            {
                return;
            }
            var drawArea = GetDrawArea();
            var minX = eventRef.x - sheet.movingLimitLeft;
            minX = minX < 0 ? 0 : minX;
            var maxX = eventRef.x + sheet.movingLimitRight;
            maxX = maxX > (int)drawArea.width ? (int)drawArea.width : maxX;
            var minY = eventRef.y - sheet.movingLimitUp;
            minY = minY < 0 ? 0 : minY;
            var maxY = eventRef.y + sheet.movingLimitDown;
            maxY = maxY > (int)drawArea.height ? (int)drawArea.height : maxY;

            if (maskColorMap[minX, minY] == color && maskColorMap[maxX, maxY] == color)
            {
                return;
            }

            var width = maxX - minX + 1;
            var height = maxY - minY + 1;
            uint[] colors = new uint[width * height];
            for (int i = 0; i < width * height; i++)
            {
                colors[i] = color;
            }
            masktexture.storeSubPixel2D(minX, minY, width, height, colors, SharpKmyGfx.TEXTUREFFORMAT.kRGBA, 0, 0, width);
            for (var i = minX; i <= maxX; ++i)
            {
                for (var j = minY; j <= maxY; ++j)
                {
                    maskColorMap[i, j] = color;
                }
            }
        }

        public void Update(float elapsed)
        {
            float interval = 0.33f;
            elapsedTime += elapsed;
            if (elapsedTime >= interval)
            {
                foreach (ChipInfo ci in chipSetInfo.chipVariations)
                {
                    SharpKmyGfx.Texture t = chipSetInfo.t_temp[chipSetInfo.chipVariations.IndexOf(ci)];
                    int w = t.getWidth();
                    int acount = w / 48;
                    if (acount == 1) continue;

                    ci.animIndex++;
                    ci.animIndex = ci.animIndex % acount;

                    int h = t.getHeight() / 48;

                    for (int c = 0; c < h; c++)
                    {
                        maptexture.storeSubPixel2D((int)ci.uvlist[c].x, maptexture.getHeight() - (int)ci.uvlist[c].y - 48, 48, 48, ci.srcdata, SharpKmyGfx.TEXTUREFFORMAT.kRGBA, 48 * ci.animIndex, (h - 1 - c) * 48, t.getWidth());
                    }
                }
                elapsedTime -= interval;
            }

            if (light != null)
                light.addShadowMapDrawable(this);

            //Graphics.sInstance.DrawImage(light.getShadowMapTexture(), 0, 0,250,250, 0);//シャドウマップの状態表示テスト用
        }

        public Light getLight()
        {
            return light;
        }

        int[,] furnitureHeight;
        int[,] collisionHeight;
        bool[,] walkableMap;
        bool[,] roofMap;

        // XZ座標が等しいマップオブジェクトをまとめたXZ別のリスト
        MapObjectLIst mapObjectList = new MapObjectLIst();
        /// <summary>
        /// XZ座標が等しいマップオブジェクトをまとめたXZ別のリスト
        /// </summary>
        class MapObjectLIst
        {
            /// <summary>
            /// XZ座標が等しいマップオブジェクトをまとめたもの
            /// </summary>
            public class MapObjectsSamePositionXZ
            {
                public static int GetPositionHash(int x, int z) { return x + z * 10000; }

                // 二次元マップのXZ
                public int MapPositionXZ { get; set; }
                public List<MapObjectInstance> MapObjects { get; set; }

                private MapObjectsSamePositionXZ()
                {
                    MapPositionXZ = 0;
                    MapObjects = new List<MapObjectInstance>();
                }

                /// <summary>
                /// コンストラクタ
                /// </summary>
                /// <param name="mapPositionXZ">座標</param>
                public MapObjectsSamePositionXZ(int mapPositionXZ)
                {
                    MapPositionXZ = mapPositionXZ;
                    MapObjects = new List<MapObjectInstance>();
                }

                /// <summary>
                /// マップオブジェクトを追加する
                /// 追加するものが内包されていた場合追加しない
                /// </summary>
                /// <param name="mapObject">追加するマップオブジェクト</param>
                public void addMapObject(MapObjectInstance mapObject)
                {
                    if (MapObjects.Contains(mapObject))
                    {
                        return;
                    }
                    MapObjects.Add(mapObject);
                }
            }

            public List<MapObjectsSamePositionXZ> MapObjectsSamePosition { get; set; }

            public MapObjectLIst()
            {
                MapObjectsSamePosition = new List<MapObjectsSamePositionXZ>();
            }

            /// <summary>
            /// 位置(XZ)をもとにその位置にあるマップオブジェクトのリストを取得する
            /// </summary>
            /// <param name="positionXZ">取得したいマップオブジェクトの位置 XZ</param>
            /// <returns>存在していればマップオブジェクトのリスト なにもなければnull</returns>
            public List<MapObjectInstance> getMapObjects(int positionXZ)
            {
                foreach (var mapObjects in MapObjectsSamePosition)
                {
                    if (mapObjects.MapPositionXZ == positionXZ)
                    {
                        return mapObjects.MapObjects;
                    }
                }
                return null;
            }

            public void addNewPositonMapObject(int positionXZ, MapObjectInstance mapObject)
            {
                MapObjectsSamePosition.Add(new MapObjectsSamePositionXZ(positionXZ));
                MapObjectsSamePosition[MapObjectsSamePosition.Count - 1].addMapObject(mapObject);
            }

            public void clearAll()
            {
                foreach (var mapObjects in MapObjectsSamePosition)
                {
                    mapObjects.MapObjects.Clear();
                }
                MapObjectsSamePosition.Clear();
            }
        }


        // heightMapRotate()で使う2次元配列変数resultをここで確保しておく
        // heightMapRotate()の中で配列を確保しているとメモリを浪費してGCが頻発する原因となるので, その対策のため
        // 2次元配列の要素数はMapObject.csのheightMapHalfSize(= 32)の2倍の値, つまり64*64で不変
        public class HeightMapResult
        {
            const int SZ = 64;
            Common.Resource.MapObject.CollisionInfo[] buffer = new Common.Resource.MapObject.CollisionInfo[SZ * SZ];

            public Common.Resource.MapObject.CollisionInfo this[int x, int y]
            {
                get
                {
                    int index = x + y * SZ;
                    return buffer[index];
                }
                set
                {
                    int index = x + y * SZ;
                    buffer[index] = value;
                }
            }

            public Common.Resource.MapObject.CollisionInfo this[int idx]
            {
                get
                {
                    return buffer[idx];
                }
                set
                {
                    buffer[idx] = value;
                }
            }
        }
        //

        // 回転マップのデータを毎回生成していると負荷が非常に大きくなるため, キャッシュを使う
        // 各マップについて, オブジェクト名と回転角度を結合した文字列をキーとするマップに結果をキャッシュする
        Dictionary<Guid, HeightMapResult[]> heightMapCache = new Dictionary<Guid, HeightMapResult[]>();

        internal HeightMapResult heightMapRotate(Common.Resource.MapObject mo, int angle)
        {
            while (angle < 0) angle += 4;
            while (angle >= 4) angle -= 4;

            // キャッシュ済みのデータがあればそれを返す
            if (heightMapCache.ContainsKey(mo.guId) && heightMapCache[mo.guId][angle] != null)
            {
                return heightMapCache[mo.guId][angle];
            }

            var result = new HeightMapResult();
            var src = mo.collisionMap;
            int w = src.GetLength(0);
            int h = src.GetLength(1);
            int hsz = w / 2;

            for (int x = 0; x < w; x++)
            {
                for (int z = 0; z < h; z++)
                {
                    int __x = 0;
                    int __z = 0;
                    switch (angle)
                    {
                        case 0:
                            __x = x;
                            __z = z;
                            break;

                        case 3:
                            {
                                int _x = x - hsz;
                                int _z = z - hsz;
                                __x = _z + hsz;
                                __z = -_x + hsz;
                            }
                            break;

                        case 2:
                            {
                                int _x = x - hsz;
                                int _z = z - hsz;
                                __x = -_x + hsz;
                                __z = -_z + hsz;

                            }
                            break;

                        case 1:
                            {
                                int _x = x - hsz;
                                int _z = z - hsz;
                                __x = -_z + hsz;
                                __z = _x + hsz;
                            }
                            break;
                    }

                    if (__x >= 0 && __x < w && __z >= 0 && __z < w) result[x, z] = src[__x, __z];
                    else result[x, z] = new Common.Resource.MapObject.CollisionInfo();
                }
            }

            // 新規に生成した結果はキャッシュしておく
            if (!heightMapCache.ContainsKey(mo.guId))
                heightMapCache[mo.guId] = new HeightMapResult[4];
            heightMapCache[mo.guId][angle] = result;

            return result;
        }

        public void updateMapHeightAndWalkableState(bool isEnableCalcBuildingObjectHeight = true)
        {
            if (furnitureHeight == null || furnitureHeight.GetLength(0) != mapRom.Width || furnitureHeight.GetLength(1) != mapRom.Height)
            {
                furnitureHeight = new int[mapRom.Width, mapRom.Height];
                collisionHeight = new int[mapRom.Width, mapRom.Height];
                walkableMap = new bool[mapRom.Width, mapRom.Height];
                roofMap = new bool[mapRom.Width, mapRom.Height];
            }
            //地形
            for (int x = 0; x < mapRom.Width; x++)
            {
                for (int y = 0; y < mapRom.Height; y++)
                {
                    furnitureHeight[x, y] = (int)getTerrainHeight(x, y);
                    walkableMap[x, y] = getTerrainWalkable(mapRom.getTerrainAttrib(x, y));
                    roofMap[x, y] = false;
                }
            }

            //建物
            int mid = 0;
            while (isEnableCalcBuildingObjectHeight)
            {
                Common.Rom.Map.MapObjectRef r = mapRom.getMapObject(mid++);
                if (r == null) break;

                RomItem i = Common.Catalog.sInstance.getItemFromGuid(r.guId);
                if (i == null) continue;

                if (i.GetType() == typeof(Common.Resource.MapObject))
                {
                    Common.Resource.MapObject mo = (Common.Resource.MapObject)i;

                    if (!mo.isFoothold) continue;

                    int th = (int)getTerrainHeight(r.x, r.y);//基本の高さ

                    //var adjustedObjectHeightMap = heightMapRotate(mo.collisionMap, r.r);
                    var adjustedObjectHeightMap = heightMapRotate(mo, r.r);

                    int hsz = mo.heightMapHalfSize;
                    for (int x = -hsz; x < hsz; x++)
                    {
                        for (int y = -hsz; y < hsz; y++)
                        {
                            int ax = x + r.x;//実際のマップの位置
                            int ay = y + r.y;

                            if (ax < 0 || ax >= mapRom.Width || ay < 0 || ay >= mapRom.Height) continue;

                            var bh = adjustedObjectHeightMap[x + hsz, y + hsz];//建物にあたり判定がせっていしてある
                            int ch = furnitureHeight[ax, ay];//既に別の建物が設定している場合を考慮

                            if (bh.height > 0 && ch < th + bh.height)
                            {
                                furnitureHeight[ax, ay] = th + bh.height;//他の高さの方が現在の高さより高い場合に割り当てる
                                walkableMap[ax, ay] = true;//建物の上は歩ける
                                roofMap[ax, ay] = bh.isRoof;
                            }
                        }
                    }
                }
            }

            for (int x = 0; x < mapRom.Width; x++)
            {
                for (int y = 0; y < mapRom.Height; y++)
                {
                    collisionHeight[x, y] = furnitureHeight[x, y];
                }
            }


            //家具
            mid = 0;
            while (true)
            {
                Common.Rom.Map.MapObjectRef r = mapRom.getMapObject(mid++);
                if (r == null) break;

                Common.Resource.MapObject mo = Common.Catalog.sInstance.getItemFromGuid(r.guId) as Common.Resource.MapObject;
                if (mo == null || mo.isFoothold) continue;

                {
                    int fh = (int)getFurnitureHeight(r.x, r.y);//起点の配置高さ

                    //var adjustedObjectHeightMap = heightMapRotate(mo.collisionMap, r.r);
                    var adjustedObjectHeightMap = heightMapRotate(mo, r.r);

                    int hsz = mo.heightMapHalfSize;
                    for (int x = -hsz; x < hsz; x++)
                    {
                        for (int y = -hsz; y < hsz; y++)
                        {
                            int ax = x + r.x;//実際のマップの位置
                            int ay = y + r.y;

                            if (ax < 0 || ax >= mapRom.Width || ay < 0 || ay >= mapRom.Height) continue;

                            var bh = adjustedObjectHeightMap[x + hsz, y + hsz];//家具にあたり判定がせっていしてある
                            int ch = collisionHeight[ax, ay];//既に別の家具が設定している場合を考慮

                            if (bh.height > 0 && ch < fh + bh.height)
                            {
                                collisionHeight[ax, ay] = fh + bh.height;//他の高さの方が現在の高さより高い場合に割り当てる
                                walkableMap[ax, ay] = true;//家具の上も歩ける
                                roofMap[ax, ay] = bh.isRoof;
                            }
                        }
                    }
                }
            }
            if (!isEditor)
            {
                mapObjectList.clearAll();
                createMapObjectsInfo();
            }
        }
        /*
        public SharpKmyGfx.ModelInstance getObjectModel(int index)
        {
            List<Common.Rom.RomItem> list = Common.Catalog.sInstance.getFilteredItemList(typeof(Common.Resource.MapObject));
            if (list.Count > index && list[index].GetType() == typeof(Common.Resource.MapObject))
            {
                var path = ((Common.Resource.MapObject)list[index]).path;
                var result = Graphics.LoadModel(path);
                Graphics.LoadMotionDef(path, result);
                return result;
            }
            return null;
        }
        */

        private void createMapObjectsInfo()
        {
            // マップオブジェクトをすべて捜査してXZが2つとも等しい物をまとめる
#if WINDOWS
            foreach (var mapObject in mapobjects)
            {
                createMapObjectPosition(mapObject);
            }
#else
            for(int i=0; i<mapRom.getMapObjectNum(); i++)
            {
                var mr = mapRom.getMapObject(i);
                var inst = new MapObjectInstance();
                inst.mo = Common.Catalog.sInstance.getItemFromGuid(mr.guId) as Common.Resource.MapObject;
                if (inst.mo == null)
                    continue;
                inst.info = mr;
                createMapObjectPosition(inst);
            }
#endif
        }

        private void createMapObjectPosition(MapObjectInstance mapObject)
        {
            var mapObjectLength = mapObject.mo.maxx;
            var mapObjectPositionXZ = MapObjectLIst.MapObjectsSamePositionXZ.GetPositionHash(mapObject.info.x, mapObject.info.y);
            var rotation = mapObject.info.r;
            //mapObjectPositionXZの初期位置もいれるために + 1
            for (int i = 0; i < mapObjectLength + 1; ++i)
            {
                // 中身がなれば挿入
                if (mapObjectList.MapObjectsSamePosition.Count == 0)
                {
                    mapObjectList.addNewPositonMapObject(mapObjectPositionXZ, mapObject);
                    mapObjectPositionXZ = calculateMapObjectPosition(mapObjectPositionXZ, rotation);
                    continue;
                }

                bool haveFindedSamePosition = false;
                foreach (var mapObjectsSamePosXZ in mapObjectList.MapObjectsSamePosition)
                {
                    // XZが同一の物が見つかった
                    if (mapObjectsSamePosXZ.MapPositionXZ == mapObjectPositionXZ)
                    {
                        mapObjectsSamePosXZ.addMapObject(mapObject);
                        haveFindedSamePosition = true;
                        break;
                    }
                }

                // 同一の物が見つからない場合新しく挿入する
                if (!haveFindedSamePosition)
                {
                    mapObjectList.addNewPositonMapObject(mapObjectPositionXZ, mapObject);
                }
                mapObjectPositionXZ = calculateMapObjectPosition(mapObjectPositionXZ, rotation);
            }
        }

        private int calculateMapObjectPosition(int mapObjectPositionXZ, int mapObjectRotation)
        {
            var x = 0;
            var z = 0;
            switch (mapObjectRotation)
            {
                // 右
                case 0:
                    ++x;
                    break;
                //奥
                case 1:
                    --z;
                    break;
                // 左
                case 2:
                    --x;
                    break;
                //手前
                case 3:
                    ++z;
                    break;
            }
            var result = mapObjectPositionXZ + MapObjectLIst.MapObjectsSamePositionXZ.GetPositionHash(x, z);
            return result;
        }

        public bool isStair(int index)
        {
            for (int i = 0; i < chipSetInfo.stairVariations.Count; i++)
            {
                if (chipSetInfo.stairVariations[i].index == index)
                {
                    return chipSetInfo.stairVariations[i].info.stair;
                }
            }
            return false;
        }

        public SharpKmyMath.Vector2[] getStairTexCoord(int index)
        {
            //(8 + 8 + 6 * 4)
            SharpKmyMath.Vector2[] v = new SharpKmyMath.Vector2[40]
            {
                new SharpKmyMath.Vector2(1,47),
                new SharpKmyMath.Vector2(1,1),
                new SharpKmyMath.Vector2(16,1),
                new SharpKmyMath.Vector2(16,16),
                new SharpKmyMath.Vector2(32,16),
                new SharpKmyMath.Vector2(32,32),
                new SharpKmyMath.Vector2(47,32),
                new SharpKmyMath.Vector2(47,47),

                new SharpKmyMath.Vector2(1,47),
                new SharpKmyMath.Vector2(1,1),
                new SharpKmyMath.Vector2(16,1),
                new SharpKmyMath.Vector2(16,16),
                new SharpKmyMath.Vector2(32,16),
                new SharpKmyMath.Vector2(32,32),
                new SharpKmyMath.Vector2(47,32),
                new SharpKmyMath.Vector2(47,47),

                //u3
                new SharpKmyMath.Vector2(1,1),
                new SharpKmyMath.Vector2(47,1),
                new SharpKmyMath.Vector2(47,16),
                new SharpKmyMath.Vector2(1,16),

                //u2
                new SharpKmyMath.Vector2(1,16),
                new SharpKmyMath.Vector2(47,16),
                new SharpKmyMath.Vector2(47,32),
                new SharpKmyMath.Vector2(1,32),

                //u1
                new SharpKmyMath.Vector2(1,32),
                new SharpKmyMath.Vector2(47,32),
                new SharpKmyMath.Vector2(47,47),
                new SharpKmyMath.Vector2(1,47),

                //k3
                new SharpKmyMath.Vector2(1,1),
                new SharpKmyMath.Vector2(47,1),
                new SharpKmyMath.Vector2(47,16),
                new SharpKmyMath.Vector2(1,16),

                //k2
                new SharpKmyMath.Vector2(1,16),
                new SharpKmyMath.Vector2(47,16),
                new SharpKmyMath.Vector2(47,32),
                new SharpKmyMath.Vector2(1,32),

                //k1
                new SharpKmyMath.Vector2(1,32),
                new SharpKmyMath.Vector2(47,32),
                new SharpKmyMath.Vector2(47,47),
                new SharpKmyMath.Vector2(1,47),
            };

            float w = maptexture.getWidth();
            float h = maptexture.getHeight();

            foreach (var i in chipSetInfo.stairVariations)
            {
                if (getChipIndex(i.info) == index && i.uvlist.Count >= 3)
                {
                    for (int k = 0; k < 8; k++)
                    {
                        v[k].x = (float)(v[k].x + i.uvlist[2].x) / w;
                        v[k].y = (float)(v[k].y + i.uvlist[2].y) / h;
                        v[k].y = 1 - v[k].y;
                    }

                    for (int k = 8; k < 16; k++)
                    {
                        v[k].x = (float)(v[k].x + i.uvlist[2].x) / w;
                        v[k].y = (float)(v[k].y + i.uvlist[2].y) / h;
                        v[k].y = 1 - v[k].y;
                    }

                    for (int k = 16; k < 28; k++)
                    {
                        v[k].x = (float)(v[k].x + i.uvlist[0].x) / w;
                        v[k].y = (float)(v[k].y + i.uvlist[0].y) / h;
                        v[k].y = 1 - v[k].y;
                    }

                    for (int k = 28; k < 40; k++)
                    {
                        v[k].x = (float)(v[k].x + i.uvlist[1].x) / w;
                        v[k].y = (float)(v[k].y + i.uvlist[1].y) / h;
                        v[k].y = 1 - v[k].y;
                    }
                }
            }

            return v;
        }

        public SharpKmyMath.Vector2[] getRidgeStairTexCoord(int index)
        {
            int B = 1;
            int C = 16;
            int D = 32;
            int E = 47;
            SharpKmyMath.Vector2[] v = new SharpKmyMath.Vector2[]
            {
                //t1
                new SharpKmyMath.Vector2(1,B),
                new SharpKmyMath.Vector2(16,B),
                new SharpKmyMath.Vector2(16,C),
                new SharpKmyMath.Vector2(1,C),

                //t2-1
                new SharpKmyMath.Vector2(1,C),
                new SharpKmyMath.Vector2(16,C),
                new SharpKmyMath.Vector2(32,D),
                new SharpKmyMath.Vector2(1,D),
                //t2-2
                new SharpKmyMath.Vector2(32,C),
                new SharpKmyMath.Vector2(47,C),
                new SharpKmyMath.Vector2(47,D),
                new SharpKmyMath.Vector2(16,D),


                //t3-1
                new SharpKmyMath.Vector2(1,D),
                new SharpKmyMath.Vector2(32,D),
                new SharpKmyMath.Vector2(47,E),
                new SharpKmyMath.Vector2(1,E),
                //t3-2
                new SharpKmyMath.Vector2(16,D),
                new SharpKmyMath.Vector2(47,D),
                new SharpKmyMath.Vector2(47,E),
                new SharpKmyMath.Vector2(1,E),

                //k1-1
                new SharpKmyMath.Vector2(1,B),
                new SharpKmyMath.Vector2(16,B),
                new SharpKmyMath.Vector2(16,C),
                new SharpKmyMath.Vector2(1,C),
                //k1-2
                new SharpKmyMath.Vector2(32,B),
                new SharpKmyMath.Vector2(47,B),
                new SharpKmyMath.Vector2(47,C),
                new SharpKmyMath.Vector2(32,C),

                //k2-1
                new SharpKmyMath.Vector2(1,C),
                new SharpKmyMath.Vector2(32,C),
                new SharpKmyMath.Vector2(32,D),
                new SharpKmyMath.Vector2(1,D),
                //k2-2
                new SharpKmyMath.Vector2(16,C),
                new SharpKmyMath.Vector2(47,C),
                new SharpKmyMath.Vector2(47,D),
                new SharpKmyMath.Vector2(16,D),

                //k3-1
                new SharpKmyMath.Vector2(1,D),
                new SharpKmyMath.Vector2(47,D),
                new SharpKmyMath.Vector2(47,E),
                new SharpKmyMath.Vector2(1,E),
                //k3-2
                new SharpKmyMath.Vector2(1,D),
                new SharpKmyMath.Vector2(47,D),
                new SharpKmyMath.Vector2(47,E),
                new SharpKmyMath.Vector2(1,E),

                //side
                new SharpKmyMath.Vector2(1,47),
                new SharpKmyMath.Vector2(1,1),
                new SharpKmyMath.Vector2(16,1),
                new SharpKmyMath.Vector2(16,16),
                new SharpKmyMath.Vector2(32,16),
                new SharpKmyMath.Vector2(32,32),
                new SharpKmyMath.Vector2(47,32),
                new SharpKmyMath.Vector2(47,47),

                //side
                new SharpKmyMath.Vector2(1,47),
                new SharpKmyMath.Vector2(1,1),
                new SharpKmyMath.Vector2(16,1),
                new SharpKmyMath.Vector2(16,16),
                new SharpKmyMath.Vector2(32,16),
                new SharpKmyMath.Vector2(32,32),
                new SharpKmyMath.Vector2(47,32),
                new SharpKmyMath.Vector2(47,47),

            };

            float w = maptexture.getWidth();
            float h = maptexture.getHeight();

            foreach (var i in chipSetInfo.stairVariations)
            {
                if (getChipIndex(i.info) == index && i.uvlist.Count >= 3)
                {
                    for (int k = 0; k < 20; k++)
                    {
                        v[k].x = (float)(v[k].x + i.uvlist[0].x) / w;
                        v[k].y = (float)(v[k].y + i.uvlist[0].y) / h;
                        v[k].y = 1 - v[k].y;
                    }

                    for (int k = 20; k < 44; k++)
                    {
                        v[k].x = (float)(v[k].x + i.uvlist[1].x) / w;
                        v[k].y = (float)(v[k].y + i.uvlist[1].y) / h;
                        v[k].y = 1 - v[k].y;
                    }

                    for (int k = 44; k < 60; k++)
                    {
                        v[k].x = (float)(v[k].x + i.uvlist[2].x) / w;
                        v[k].y = (float)(v[k].y + i.uvlist[2].y) / h;
                        v[k].y = 1 - v[k].y;
                    }
                    break;
                }
            }

            return v;
        }

        public SharpKmyMath.Vector2[] getValleyStairTexCoord(int index)
        {
            SharpKmyMath.Vector2[] v = new SharpKmyMath.Vector2[]
            {
                //t1-1
                new SharpKmyMath.Vector2(1,1),
                new SharpKmyMath.Vector2(47,1),
                new SharpKmyMath.Vector2(32,16),
                new SharpKmyMath.Vector2(1,16),
                //t1-2
                new SharpKmyMath.Vector2(1,1),
                new SharpKmyMath.Vector2(47,1),
                new SharpKmyMath.Vector2(47,16),
                new SharpKmyMath.Vector2(16,16),
                //t2-1
                new SharpKmyMath.Vector2(1,16),
                new SharpKmyMath.Vector2(32,16),
                new SharpKmyMath.Vector2(16,32),
                new SharpKmyMath.Vector2(1,32),
                //t2-2
                new SharpKmyMath.Vector2(16,16),
                new SharpKmyMath.Vector2(47,16),
                new SharpKmyMath.Vector2(47,32),
                new SharpKmyMath.Vector2(32,32),
                //t3
                new SharpKmyMath.Vector2(32,32),
                new SharpKmyMath.Vector2(47,32),
                new SharpKmyMath.Vector2(47,47),
                new SharpKmyMath.Vector2(32,47),


                //k1-1
                new SharpKmyMath.Vector2(1,1),
                new SharpKmyMath.Vector2(32,1),
                new SharpKmyMath.Vector2(32, 16),
                new SharpKmyMath.Vector2(1,16),
                //k1-2
                new SharpKmyMath.Vector2(16,1),
                new SharpKmyMath.Vector2(47,1),
                new SharpKmyMath.Vector2(47, 16),
                new SharpKmyMath.Vector2(16,16),
                //k2-1
                new SharpKmyMath.Vector2(1,16),
                new SharpKmyMath.Vector2(16,16),
                new SharpKmyMath.Vector2(16,32),
                new SharpKmyMath.Vector2(1,32),
                //k2-2
                new SharpKmyMath.Vector2(32,16),
                new SharpKmyMath.Vector2(47,16),
                new SharpKmyMath.Vector2(47,32),
                new SharpKmyMath.Vector2(32,32),

                //side
                new SharpKmyMath.Vector2(1,47),
                new SharpKmyMath.Vector2(1,1),
                new SharpKmyMath.Vector2(16,1),
                new SharpKmyMath.Vector2(16,16),
                new SharpKmyMath.Vector2(32,16),
                new SharpKmyMath.Vector2(32,32),
                new SharpKmyMath.Vector2(47,32),
                new SharpKmyMath.Vector2(47,47),

                //side
                new SharpKmyMath.Vector2(1,47),
                new SharpKmyMath.Vector2(1,1),
                new SharpKmyMath.Vector2(16,1),
                new SharpKmyMath.Vector2(16,16),
                new SharpKmyMath.Vector2(32,16),
                new SharpKmyMath.Vector2(32,32),
                new SharpKmyMath.Vector2(47,32),
                new SharpKmyMath.Vector2(47,47),

                //back
                new SharpKmyMath.Vector2(1,1),
                new SharpKmyMath.Vector2(1,1),
                new SharpKmyMath.Vector2(1,1),
                new SharpKmyMath.Vector2(1,1),

                //back
                new SharpKmyMath.Vector2(1,1),
                new SharpKmyMath.Vector2(1,1),
                new SharpKmyMath.Vector2(1,1),
                new SharpKmyMath.Vector2(1,1),
        };

            float w = maptexture.getWidth();
            float h = maptexture.getHeight();

            foreach (var i in chipSetInfo.stairVariations)
            {
                if (getChipIndex(i.info) == index && i.uvlist.Count >= 3)
                {
                    for (int k = 0; k < 20; k++)
                    {
                        v[k].x = (float)(v[k].x + i.uvlist[0].x) / w;
                        v[k].y = (float)(v[k].y + i.uvlist[0].y) / h;
                        v[k].y = 1 - v[k].y;
                    }

                    for (int k = 20; k < 36; k++)
                    {
                        v[k].x = (float)(v[k].x + i.uvlist[1].x) / w;
                        v[k].y = (float)(v[k].y + i.uvlist[1].y) / h;
                        v[k].y = 1 - v[k].y;
                    }

                    for (int k = 36; k < 52; k++)
                    {
                        v[k].x = (float)(v[k].x + i.uvlist[2].x) / w;
                        v[k].y = (float)(v[k].y + i.uvlist[2].y) / h;
                        v[k].y = 1 - v[k].y;
                    }

                    for (int k = 52; k < 60; k++)
                    {
                        v[k].x = (float)(v[k].x + i.uvlist[0].x) / w;
                        v[k].y = (float)(v[k].y + i.uvlist[0].y) / h;
                        v[k].y = 1 - v[k].y;
                    }
                    break;
                }
            }

            return v;
        }

        public SharpKmyMath.Vector2[] getSlopeTexCoord(int index)
        {
            //(3 + 3 + 4)個
            SharpKmyMath.Vector2[] v = new SharpKmyMath.Vector2[8]{

                new SharpKmyMath.Vector2(1f,1f),
                new SharpKmyMath.Vector2(47f,1f),
                new SharpKmyMath.Vector2(47f,47f),
                new SharpKmyMath.Vector2(1f,47f),

                // 地形に開く穴を塞ぐ部分のテクスチャUV (暫定なので後で調整する)
                new SharpKmyMath.Vector2(1f,1f),
                new SharpKmyMath.Vector2(47f,1f),
                new SharpKmyMath.Vector2(47f,47f),
                new SharpKmyMath.Vector2(1f,47f),

            };

            float w = maptexture.getWidth();
            float h = maptexture.getHeight();

            foreach (var i in chipSetInfo.stairVariations)
            {
                if (getChipIndex(i.info) == index && i.uvlist.Count >= 2)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        v[k].x = (float)(v[k].x + i.uvlist[0].x) / w;
                        v[k].y = (float)(v[k].y + i.uvlist[0].y) / h;
                        v[k].y = 1 - v[k].y;
                    }
                    for (int k = 4; k < 8; k++)
                    {
                        v[k].x = (float)(v[k].x + i.uvlist[1].x) / w;
                        v[k].y = (float)(v[k].y + i.uvlist[1].y) / h;
                        v[k].y = 1 - v[k].y;
                    }
                }
            }

            return v;
        }

        public SharpKmyMath.Vector2 getTerrainTexCoordTopLeft(int index, int height)
        {
            foreach (ChipInfo ci in chipSetInfo.chipVariations)
            {
                if (ci.info.wave) continue;

                if (getChipIndex(ci.info, false) == index)
                {
                    if (height >= ci.uvlist.Count) height = ci.uvlist.Count - 1;
                    if (height < 0)
                        break;
                    SharpKmyMath.Vector2 ret = ci.uvlist[height];
                    ret.x += 0.1f;
                    ret.y += 0.1f;
                    ret.x /= (float)maptexture.getWidth();
                    ret.y /= (float)maptexture.getHeight();
                    ret.y = 1f - ret.y;
                    return ret;
                }
            }
            return new SharpKmyMath.Vector2(0, 0);
        }

        public SharpKmyMath.Vector2 getTerrainTexCoordBottomRight(int index, int height)
        {
            foreach (ChipInfo ci in chipSetInfo.chipVariations)
            {
                if (ci.info.wave) continue;

                if (getChipIndex(ci.info, false) == index)
                {
                    if (height >= ci.uvlist.Count) height = ci.uvlist.Count - 1;
                    if (height < 0)
                        break;
                    SharpKmyMath.Vector2 ret = ci.uvlist[height];
                    ret.x += 47.9f;
                    ret.y += 47.9f;
                    ret.y = maptexture.getHeight() - ret.y;
                    ret.x /= (float)maptexture.getWidth();
                    ret.y /= (float)maptexture.getHeight();
                    ///ret.y = 1f - ret.y;
                    return ret;
                }
            }
            return new SharpKmyMath.Vector2(0, 0);
        }

        public SharpKmyMath.Vector2 getWaveTexCoordTopLeft(int index)
        {
            foreach (ChipInfo ci in chipSetInfo.chipVariations)
            {
                if (!ci.info.wave) continue;

                if (getChipIndex(ci.info, false) == index)
                {
                    SharpKmyMath.Vector2 ret = ci.uvlist[0];
                    ret.x += 1.0f;
                    ret.y += 1.0f;
                    ret.x /= (float)maptexture.getWidth();
                    ret.y /= (float)maptexture.getHeight();
                    ret.y = 1f - ret.y;
                    return ret;
                }
            }
            return new SharpKmyMath.Vector2(0, 0);
        }

        public SharpKmyMath.Vector2 getWaveTexCoordBottomRight(int index)
        {
            foreach (ChipInfo ci in chipSetInfo.chipVariations)
            {
                if (!ci.info.wave) continue;

                if (getChipIndex(ci.info, false) == index)
                {
                    SharpKmyMath.Vector2 ret = ci.uvlist[0];
                    ret.x += 47f;
                    ret.y += 47f;
                    ret.x /= (float)maptexture.getWidth();
                    ret.y /= (float)maptexture.getHeight();
                    ret.y = 1f - ret.y;
                    return ret;
                }
            }
            return new SharpKmyMath.Vector2(0, 0);
        }

        public Common.Resource.MapChip getTerrainInfo(int index)
        {
            foreach (ChipInfo ci in chipSetInfo.chipVariations)
            {
                if (ci.info.wave) continue;

                if (getChipIndex(ci.info, false) == index)
                {
                    return ci.info;
                }
            }
            return null;
        }

        public bool getTerrainWalkable(int index)
        {
            foreach (ChipInfo ci in chipSetInfo.chipVariations)
            {
                if (ci.info.wave) continue;

                if (getChipIndex(ci.info, false) == index)
                {
                    return ci.info.walkable;
                }
            }
            return true;
        }

        public bool getTerrainSquareShape(int index)
        {
            foreach (ChipInfo ci in chipSetInfo.chipVariations)
            {
                if (ci.info.wave) continue;

                if (getChipIndex(ci.info, false) == index)
                {
                    return ci.info.squareShape;
                }
            }
            return false;
        }

        public bool getTerrainLiquidArea(int index)
        {
            foreach (ChipInfo ci in chipSetInfo.chipVariations)
            {
                if (ci.info.wave) continue;

                if (getChipIndex(ci.info, false) == index)
                {
                    return ci.info.liquid;
                }
            }
            return false;
        }

        bool viewVolumeCheckSub(int x, int z, int flgx, int flgz, SharpKmyMath.Matrix4 view)
        {
            SharpKmyMath.Vector3 center = new SharpKmyMath.Vector3((flgx * mHcount + x) * MapData.TIPS_IN_CLUSTER,
                0,
                (flgz * mDcount + z) * MapData.TIPS_IN_CLUSTER)

                + new SharpKmyMath.Vector3(MapData.TIPS_IN_CLUSTER / 2, 1, MapData.TIPS_IN_CLUSTER / 2);

            return viewVolumeCheck(view, center, 4f * 1.42f);
        }

        static float fognear = 1;
        static float fogfar = 1;
        static SharpKmyGfx.Color fogDisabledColor = new SharpKmyGfx.Color(0, 0, 0, 0);
        static SharpKmyGfx.Color fogColor = fogDisabledColor;

        void updateFogParam(SharpKmyMath.Matrix4 pm)
        {
            if (mapRom == null)
                return;

            float fognearbase = -20;
            float fogfarbase = -200;
            if (isExpandViewRange && isEditor)
            {
                fognearbase = -100;
                fogfarbase = -500;
            }
            else if (!isExpandViewRange)
            {
                fognearbase = -75;
                fogfarbase = -135;
            }
            var fognearclip = pm * new SharpKmyMath.Vector4(0, 0, fognearbase, 1);
            var fogfarclip = pm * new SharpKmyMath.Vector4(0, 0, fogfarbase, 1);

            fognear = fognearclip.z / fognearclip.w;
            fogfar = fogfarclip.z / fogfarclip.w;
        }

        static public void setFogParam(SharpKmyGfx.DrawInfo di)
        {
            var blend = di.getBlend();
            if (blend == SharpKmyGfx.BLENDTYPE.kADD)
            {
                di.setFPColor("fogcolor", fogDisabledColor);
            }
            else
            {
                di.setFPColor("fogcolor", fogColor);
            }
            di.setFPValue("fognear", fognear);
            di.setFPValue("fogfar", fogfar);
        }

        float clusterviewrange = 0;
        int tic = MapData.TIPS_IN_CLUSTER;

        public void afterViewPositionFixProc(SharpKmyGfx.Render scn)
        {
            SharpKmyMath.Matrix4 vm = new SharpKmyMath.Matrix4(), pm = new SharpKmyMath.Matrix4();
            scn.getViewMatrix(ref pm, ref vm);
            SharpKmyMath.Matrix4 ivm = SharpKmyMath.Matrix4.inverse(vm);

            //可視範囲を考える
            SharpKmyMath.Vector3 campos = ivm.translation();
            float distance = (campos - shadowcenter).length();
            SharpKmyMath.Vector4 testpos = new SharpKmyMath.Vector4(0, 0, -distance, 1);
            SharpKmyMath.Vector4 cliptestpos = pm * testpos;
            float clipz = cliptestpos.z / cliptestpos.w;
            testpos = new SharpKmyMath.Vector4(1, 1, clipz, 1);
            SharpKmyMath.Matrix4 ipm = SharpKmyMath.Matrix4.inverse(pm);
            SharpKmyMath.Vector4 viewtestpos = ipm * testpos;
            float viewxrange = viewtestpos.x / viewtestpos.w;
            float viewyrange = viewtestpos.y / viewtestpos.w;

            float viewrange = viewxrange > viewyrange ? viewxrange : viewyrange;
#if WINDOWS
            clusterviewrange = viewrange + tic * 2.0f;
#else
            clusterviewrange = float.MaxValue;
#endif
            if (isExpandViewRange) clusterviewrange *= 1.5f;

            if (!isShadowScene(scn))
            {
                updateFogParam(pm);
                if (isExpandViewRange && viewrange < 15) viewrange = 15;

                if (light != null)
                    light.setRadius(viewrange * 3f);
            }

        }

        public override void draw(SharpKmyGfx.Render scn)
        {
            //if (!isShadowScene(scn)) return;

            // TODO GameMain.DoReset 後に、解放済みのシーンが1フレームだけ描画されてしまうので、その対策
            if (mClusters == null)
                return;

            base.draw(scn);

            if (light != null)
            {
                light.setColor(new SharpKmyGfx.Color(mapRom.dirLightColor.R / 255f, mapRom.dirLightColor.G / 255f, mapRom.dirLightColor.B / 255f, 1));
                // ---
                scn.setLight(light);
                scn.setAmbientColor(new SharpKmyMath.Vector3(mapRom.ambLightColor.R / 255f, mapRom.ambLightColor.G / 255f, mapRom.ambLightColor.B / 255f));
            }

            scn.setClearColor(mapRom.bgcolor[0], mapRom.bgcolor[1], mapRom.bgcolor[2], mapRom.bgcolor[3]);

            if (di != null)
            {
                di.setParam(0);
                di.setTexture(0, maptexture);
                di.setTexture(1, masktexture);
            }

            SharpKmyMath.Matrix4 vm = new SharpKmyMath.Matrix4(), pm = new SharpKmyMath.Matrix4();
            scn.getViewMatrix(ref pm, ref vm);
            SharpKmyMath.Matrix4 ivm = SharpKmyMath.Matrix4.inverse(vm);

            //SharpKmyMath.Matrix4 proj = scn.getProjMatrix();
            initializeVolumeCheck(pm);

            //SharpKmyMath.Vector3 mUpVector = new SharpKmyMath.Vector3(0, 1, 0);
            //SharpKmyMath.Vector3 mRightVector = new SharpKmyMath.Vector3(ivm.m00, ivm.m01, ivm.m02);
            SharpKmyMath.Vector3 mFrontVector = new SharpKmyMath.Vector3(ivm.m20, 0, ivm.m22);
            bool billboardable = mFrontVector.length() > 0.1f;
            float billboardAngle = 0;
            if (billboardable)
            {
                mFrontVector /= mFrontVector.length();
                billboardAngle = (float)Math.Acos(mFrontVector.z);
#if WINDOWS
#else
                billboardAngle += (float)Math.PI; 
#endif
                if (mFrontVector.x < 0) billboardAngle *= -1;
            }

            if (di != null)
            {
                //water
                if (mapRom.outsideType == Map.OutsideType.MAPCHIP && waterMesh.vb != null)
                {
                    int waterwidth = 128 / tic;
                    for (int z = -waterwidth * tic; z < mapRom.Height + waterwidth * tic; z += tic)
                    {
                        for (int x = -waterwidth * tic; x < mapRom.Width + waterwidth * tic; x += tic)
                        {
                            if (z >= 0 && z < mapRom.Height && x >= 0 && x < mapRom.Width) continue;

                            SharpKmyMath.Vector3 p = new SharpKmyMath.Vector3(x + tic * 0.5f, 0, z + tic * 0.5f);
                            if (Math.Abs(p.x - shadowcenter.x) > clusterviewrange ||
                                Math.Abs(p.z - shadowcenter.z) > clusterviewrange) continue;

                            di.setL2W(SharpKmyMath.Matrix4.translate(x, 0, z));
                            di.setBlend(SharpKmyGfx.BLENDTYPE.kOPAQUE);
#if WINDOWS
                            di.setVertexBuffer(waterMesh.vb);
#else
                            di.setColor(new SharpKmyGfx.Color(1, 1, 1, 1));
                            di.setVertexBuffer(waterMesh.vb, 2, mHcount, mDcount);
#endif
                            di.setIndexCount(waterMesh.vb.vtxcount);
                            di.setShader(waterShader);
                            setFogParam(di);
                            scn.draw(di);
                            continue;
                        }
                    }
                }

                int flgz = 0;
                int flgx = 0;
                di.setShader(terrainShader);
                MapData.setFogParam(di);
                DrawTerrain(0, 0, clusterviewrange, scn);
                if (mapRom.outsideType == Map.OutsideType.REPEAT)
                {
                    for (flgz = -1; flgz <= 1; flgz++)
                    {
                        for (flgx = -1; flgx <= 1; flgx++)
                        {
                            DrawTerrain(flgx, flgz, clusterviewrange, scn);
                        }
                    }
                }
            }

            bool drawBuilding = true;
            bool drawFurniture = true;

            if (pickupscene != null && SharpKmyGfx.Render.isSameScene(pickupscene, scn))
            {
                //ピックアップ時に、必要に応じて描画するものを選別する
                if (drawMapObject == PICKUP_DRAW_MAPOBJECT.kBUILDING_ONLY) drawFurniture = false;
                if (drawMapObject == PICKUP_DRAW_MAPOBJECT.kNONE)
                {
                    drawFurniture = false;
                    drawBuilding = false;
                }
            }
            else
            {
                // 空
                if (sky != null)
                    sky.Draw(scn);
            }

            {
                //マップオブジェクトの仮描画
                foreach (var m in mapobjects)
                {
                    if (m.minst != null && m.minst.modifiedModel.batcher != null) m.minst.modifiedModel.batcher.clearInstances();
                }

                List<SharpKmyGfx.StaticModelBatcher> list = new List<SharpKmyGfx.StaticModelBatcher>();

                foreach (MapObjectInstance p in mapobjects)
                {
                    if (!drawBuilding && p.mo.isFoothold) continue;
                    if (!drawFurniture && !p.mo.isFoothold) continue;
                    //if (p.mo == null) continue;

                    //if (p.multiinstance != null)
                    {
                        int h = 0;
                        if (p.mo.isFoothold)
                        {
                            h = (int)getTerrainHeight((int)p.info.x, (int)p.info.y);//TODO家具の場合は建物のあたり判定を考慮
                        }
                        else
                        {
                            h = (int)getFurnitureHeight(p.info.x, p.info.y);
                        }
                        p.lastPosition = new SharpKmyMath.Vector3(p.info.x, h, p.info.y);

                        float yangle = (float)Math.PI * 0.5f * p.info.r;
                        if (billboardable && !isShadowScene(scn) && p.mo.isBillboard)
                        {
                            yangle = billboardAngle;
                        }

                        SharpKmyMath.Matrix4 m = SharpKmyMath.Matrix4.translate(p.lastPosition.x, p.lastPosition.y, p.lastPosition.z)
                            * SharpKmyMath.Matrix4.translate(0.5f, 0, 0.5f)
                            * SharpKmyMath.Matrix4.rotateY(yangle);

                        if (Math.Abs(p.lastPosition.x - shadowcenter.x) > clusterviewrange ||
                            Math.Abs(p.lastPosition.z - shadowcenter.z) > clusterviewrange) continue;

                        if (p.minst != null)
                        {
                            //フォグの設定
                            int didx = 0;
                            while (true)
                            {
                                SharpKmyGfx.DrawInfo d = p.minst.inst.getDrawInfo(didx);
                                if (d == null) break;
                                MapData.setFogParam(d);
                                if (d.getBlend() == SharpKmyGfx.BLENDTYPE.kADD && pickupscene != null && SharpKmyGfx.Render.isSameScene(pickupscene, scn))
                                {
#if WINDOWS
                                    d.setParam(0);
#endif
                                    d.setDepthFunc(SharpKmyGfx.FUNCTYPE.kNEVER);
                                }
                                else
                                {
#if WINDOWS
                                    // ここのIndexOf()が処理時間的にかなりのボトルネックになっているため, Unityでは実行をスルーさせる
                                    d.setParam(mapobjects.IndexOf(p) + 1);
#endif
                                    d.setDepthFunc(SharpKmyGfx.FUNCTYPE.kLEQUAL);
                                }
                                didx++;
                            }

                            if (pickupscene != null && SharpKmyGfx.Render.isSameScene(pickupscene, scn))
                            {
                                p.minst.inst.setL2W(m);
                                p.minst.inst.update(0);
                                p.minst.inst.draw(scn);
                            }
                            else if (p.mo.isAnimated)
                            {
                                p.minst.update();
                                p.minst.inst.setL2W(m);
                                p.minst.inst.update(isShadowScene(scn) ? GameMain.getElapsedTime() : 0);
                                p.minst.inst.draw(scn);
                            }
                            else
                            {
#if WINDOWS
#else
                                SharpKmyGfx.StaticModelBatcher.setNextDrawInstance(p);

                                if (p.minst.modifiedModel.batcher == null)
                                {
                                    p.minst.modifiedModel.batcher = new SharpKmyGfx.StaticModelBatcher(p.minst.modifiedModel.model, 0);
                                }
#endif
                                if (p.minst.modifiedModel.batcher != null)
                                {
                                    p.minst.modifiedModel.batcher.addInstance(m);
                                    if (list.IndexOf(p.minst.modifiedModel.batcher) == -1)
                                    {
                                        list.Add(p.minst.modifiedModel.batcher);
                                    }
                                }
#if WINDOWS
#else
                                SharpKmyGfx.StaticModelBatcher.setNextDrawInstance(null);
#endif
                            }
                        }
                        else
                        {
                            p.mTentativeBox.setMatrix(SharpKmyMath.Matrix4.translate(p.lastPosition.x + 0.5f, p.lastPosition.y, p.lastPosition.z + 0.5f));
                            p.mTentativeBox.draw(scn);
                        }
                    }
                }

                //マップオブジェクトの仮描画
                foreach (var m in list)
                {
#if WINDOWS
                    // バッチドロー対象のフォグをアップデート
                    int didx = 0;
                    while (true)
                    {
                        var di = m.getDrawInfo(didx);
                        if (di == null) break;
                        setFogParam(di);
                        didx++;
                    }
#endif
                    m.draw(scn);
                }
            }

            // 環境エフェクト
            if (pickupscene != null && SharpKmyGfx.Render.isSameScene(pickupscene, scn))
            {

            }
            else
            {
                if (envEffect != null)
                {
                    var envEffectPos = this.GetEnvironmentEffectPos();
                    envEffect.Draw(scn, envEffectPos);
                }
            }

            if (mapDrawCallBack != null) mapDrawCallBack(scn, isShadowScene(scn));

            elapsedSum += SharpKmy.Entry.getFrameElapsed();
            elapsedCount++;
            if (elapsedCount == 20)
            {
                lastElapsed = 20 / elapsedSum;
                elapsedSum = 0;
                elapsedCount = 0;
            }

            if (mShowPerformance)
            {
                int _w = Graphics.ScreenWidth;
                int _h = Graphics.ScreenHeight;
                SharpKmyMath.Matrix4 proj = new SharpKmyMath.Matrix4();
                SharpKmyMath.Matrix4 view = new SharpKmyMath.Matrix4();
                scn.getViewMatrix(ref proj, ref view);
                scn.setViewMatrix(SharpKmyMath.Matrix4.ortho(0, _w, 0, _h, -100, 100), SharpKmyMath.Matrix4.identity());

                string s = "KMYFPS(*est):" + lastElapsed.ToString() + "\n" + "Mram Remain:" +
                    ((float)SharpKmy.Entry.getMramFreeSize() * 100 / SharpKmy.Entry.getMramSize()).ToString() + "%" + "\n" +
                    "Draw Call:" + SharpKmy.Entry.getDrawCallCount();

                var bytes = System.Text.Encoding.UTF8.GetBytes(s);
                sprite.drawText(fnt, bytes, 10, 10, 0, 1, 0, 1, 1, 0);

                //sprite.drawSprite(light.getShadowMapTexture(),128,128,0,0,250,250,0,0,0,1,1,1,1,1,1,0);//シャドウマップの状態表示テスト用

                sprite.draw(scn);


                scn.setViewMatrix(proj, view);
            }

        }

        private void DrawTerrain(int flgx, int flgz, float clusterviewrange, SharpKmyGfx.Render scn)
        {
            SharpKmyMath.Vector3 repeatoffset = new SharpKmyMath.Vector3(flgx * mapRom.Width, 0, flgz * mapRom.Height);
            di.setL2W(SharpKmyMath.Matrix4.translate(repeatoffset.x, repeatoffset.y, repeatoffset.z));

            if ((flgz != 0 || flgx != 0) && shadowUneditableArea) di.setColor(new SharpKmyGfx.Color(0.6f, 0.6f, 0.6f, 1));
            else di.setColor(new SharpKmyGfx.Color(1f, 1f, 1f, 1f));

            for (int x = 0; x < mHcount; x++)
            {
                for (int z = 0; z < mDcount; z++)
                {
                    SharpKmyMath.Vector3 p = new SharpKmyMath.Vector3(
                        repeatoffset.x + (x + 0.5f) * MapData.TIPS_IN_CLUSTER,
                        1,
                        repeatoffset.z + (z + 0.5f) * MapData.TIPS_IN_CLUSTER
                        );

                    if (Math.Abs(p.x - shadowcenter.x) > clusterviewrange ||
                         Math.Abs(p.z - shadowcenter.z) > clusterviewrange) continue;

                    MapClusterBuilder c = getClusterByClusterIndex(x, z);

                    if (c.vb.vtxcount > 0)
                    {
                        di.setBlend(SharpKmyGfx.BLENDTYPE.kOPAQUE);
#if WINDOWS
                        di.setVertexBuffer(c.vb);
#else
                        di.setVertexBuffer(c.vb, (flgx == 0 && flgz == 0) ? 0 : 1, mHcount, mDcount);
#endif
                        di.setIndexCount(c.vb.vtxcount);
                        di.setCull(SharpKmyGfx.CULLTYPE.kNONE);
                        scn.draw(di);
                    }

                    if (c.avb.vtxcount > 0)
                    {
                        di.setBlend(SharpKmyGfx.BLENDTYPE.kALPHA);
#if WINDOWS
                        di.setVertexBuffer(c.avb);
#else
                        di.setVertexBuffer(c.avb, (flgx == 0 && flgz == 0) ? 0 : 1, mHcount, mDcount);
#endif
                        di.setIndexCount(c.avb.vtxcount);
                        scn.draw(di);
                    }
                }
            }
        }

        public MapObjectInstance getMapObjectInstance(int idx)
        {
            if (idx < mapobjects.Count) return mapobjects[idx];
            return null;
        }

        public MapObjectInstance getMapObjectInstance(Map.MapObjectRef inRef)
        {
            foreach (var obj in mapobjects)
            {
                if (obj.info == inRef)
                {
                    return obj;
                }
            }
            return null;
        }

        void initializeVolumeCheck(SharpKmyMath.Matrix4 proj)
        {
            SharpKmyMath.Matrix4 iproj = SharpKmyMath.Matrix4.inverse(proj);

            //Near
            SharpKmyMath.Vector4 lv = iproj * (new SharpKmyMath.Vector4(-1, 0, -1, 1));
            SharpKmyMath.Vector4 rv = iproj * (new SharpKmyMath.Vector4(1, 0, -1, 1));
            SharpKmyMath.Vector4 tv = iproj * (new SharpKmyMath.Vector4(0, 1, -1, 1));
            SharpKmyMath.Vector4 bv = iproj * (new SharpKmyMath.Vector4(0, -1, -1, 1));

            lv /= lv.w;
            rv /= rv.w;
            tv /= tv.w;
            bv /= bv.w;

            //Far
            SharpKmyMath.Vector4 lv2 = iproj * (new SharpKmyMath.Vector4(-1f, 0f, 1f, 1f));
            SharpKmyMath.Vector4 rv2 = iproj * (new SharpKmyMath.Vector4(1f, 0f, 1f, 1f));
            SharpKmyMath.Vector4 tv2 = iproj * (new SharpKmyMath.Vector4(0f, 1f, 1f, 1f));
            SharpKmyMath.Vector4 bv2 = iproj * (new SharpKmyMath.Vector4(0f, -1f, 1f, 1f));

            lv2 /= lv2.w;
            rv2 /= rv2.w;
            tv2 /= tv2.w;
            bv2 /= bv2.w;

            nearZ = lv.z;
            farZ = lv2.z;

            lv = lv2 - lv;
            rv = rv2 - rv;
            tv = tv2 - tv;
            bv = bv2 - bv;

            lvn = new SharpKmyMath.Vector3(lv.z, 0, -lv.x);
            rvn = new SharpKmyMath.Vector3(-rv.z, 0, rv.x);
            bvn = new SharpKmyMath.Vector3(0, bv.z, -bv.y);
            tvn = new SharpKmyMath.Vector3(0, -tv.z, tv.y);


            lvn = SharpKmyMath.Vector3.normalize(lvn);
            rvn = SharpKmyMath.Vector3.normalize(rvn);
            tvn = SharpKmyMath.Vector3.normalize(tvn);
            bvn = SharpKmyMath.Vector3.normalize(bvn);

            lx = SharpKmyMath.Vector3.dotProduct(lvn, new SharpKmyMath.Vector3(lv2.x, lv2.y, lv2.z));
            rx = SharpKmyMath.Vector3.dotProduct(rvn, new SharpKmyMath.Vector3(rv2.x, rv2.y, rv2.z));
            tx = SharpKmyMath.Vector3.dotProduct(tvn, new SharpKmyMath.Vector3(tv2.x, tv2.y, tv2.z));
            bx = SharpKmyMath.Vector3.dotProduct(bvn, new SharpKmyMath.Vector3(bv2.x, bv2.y, bv2.z));
        }


        bool viewVolumeCheck(SharpKmyMath.Matrix4 view, SharpKmyMath.Vector3 center, float radius)
        {
            SharpKmyMath.Vector3 vc = (view * center).getXYZ();

            if (vc.z - radius > nearZ) return false;
            if (vc.z + radius < farZ) return false;

            float dl = SharpKmyMath.Vector3.dotProduct(lvn, vc) - lx;// Vector3.Dot(lvn, vc) - lx;
            float dr = SharpKmyMath.Vector3.dotProduct(rvn, vc) - rx;
            float dt = SharpKmyMath.Vector3.dotProduct(tvn, vc) - tx;
            float db = SharpKmyMath.Vector3.dotProduct(bvn, vc) - bx;
            if (dl > radius) return false;
            if (dr > radius) return false;
            if (dt > radius) return false;
            if (db > radius) return false;

            return true;
        }

        static void copyImage(Color[] tgt, int tx, int ty, Color[] src, int sx, int sy, int w, int h, int stride)
        {
            int copybase = sy * 512 + sx;
            int tgtbase = ty * 512 + tx;

            for (int _x = 0; _x < w; _x++)
            {
                for (int _y = 0; _y < h; _y++)
                {
                    int ofs = _y * stride + _x;

                    tgt[tgtbase + ofs] = src[copybase + ofs];
                }
            }

        }

        public MapClusterBuilder getClusterByClusterIndex(int idxx, int idxz)
        {
            if (mClusters == null) return null;

            if (idxx < mHcount && idxz < mDcount && idxx >= 0 && idxz >= 0)
            {
                return mClusters[idxx, idxz];
            }
            return null;
        }

        bool getAdjustedPosition(float x, float z, ref float ax, ref float az)
        {
            ax = x;
            az = z;
            if (mapRom.outsideType == Map.OutsideType.REPEAT)
            {
                while (ax < 0)
                {
                    ax += mapRom.Width;
                }
                while (az < 0)
                {
                    az += mapRom.Height;
                }
                while (ax >= mapRom.Width)
                {
                    ax -= mapRom.Width;
                }
                while (az >= mapRom.Height)
                {
                    az -= mapRom.Height;
                }
            }

            return !(ax < 0 || ax >= mapRom.Width || az < 0 || az >= mapRom.Height);
        }

        public MapClusterBuilder getClusterByPosition(int x, int z)
        {
            if (mapRom.outsideType == Map.OutsideType.REPEAT)
            {
                while (x < 0)
                {
                    x += mapRom.Width;
                }
                while (z < 0)
                {
                    z += mapRom.Height;
                }
                while (x >= mapRom.Width)
                {
                    x -= mapRom.Width;
                }
                while (z >= mapRom.Height)
                {
                    z -= mapRom.Height;
                }
            }
            if (x < 0 || z < 0 || x >= mapRom.Width || z >= mapRom.Height) return null;

            return getClusterByClusterIndex(x / MapData.TIPS_IN_CLUSTER, z / MapData.TIPS_IN_CLUSTER);
        }

        public float getTerrainHeight(int x, int z)
        {
            float ax = 0;
            float az = 0;
            if (getAdjustedPosition(x, z, ref ax, ref az))
            {
                return mapRom.getTerrainHeight((int)ax, (int)az);
            }
            return 0;
        }

        public float getCharacterHeight(int x, int z)
        {
            float ax = 0;
            float az = 0;
            if (getAdjustedPosition(x, z, ref ax, ref az))
            {
                return getAdjustedHeight(ax, az);
            }
            return 0;
        }

        public float getCollisionHeight(int x, int z)
        {
            float ax = 0;
            float az = 0;
            if (getAdjustedPosition(x, z, ref ax, ref az))
            {
                return collisionHeight[(int)ax, (int)az];
            }
            return 0;
        }

        public float getFurnitureHeight(int x, int z, int height = -1)
        {
            // 高さが0以上のときののみマップオブジェクトを参照する
            if (height > -1)
            {
                var mapObjects = mapObjectList.getMapObjects(MapObjectLIst.MapObjectsSamePositionXZ.GetPositionHash(x, z));
                if (mapObjects != null)
                {
                    var mapObjectHeight = 0;
                    foreach (var mapObject in mapObjects)
                    {
                        // 地面の高さをとると一段下で取得されるので + 1
                        mapObjectHeight = (int)getTerrainHeight(mapObject.info.x, mapObject.info.y) + 1;
                        if (mapObjectHeight == height)
                        {
                            return mapObjectHeight;
                        }
                    }
                }
            }

            float ax = 0;
            float az = 0;
            if (getAdjustedPosition(x, z, ref ax, ref az))
            {
                return furnitureHeight[(int)ax, (int)az];
            }
            else return 1;
        }

        public bool getCellIsRoof(int x, int z)
        {
            float ax = 0;
            float az = 0;
            if (getAdjustedPosition(x, z, ref ax, ref az))
            {
                return roofMap[(int)ax, (int)az];
            }
            else return false;
        }

        public float getAdjustedHeight(float x, float z)
        {
            float ax = 0;
            float az = 0;
            if (!getAdjustedPosition(x, z, ref ax, ref az)) return 10000;//マップの内部？

            int ix = (int)ax;
            int iz = (int)az;
            float bh = collisionHeight[ix, iz];

            Common.Rom.Map.StairRef sr = null;
            STAIR_STAT ss = getStairStat(ix, iz, ref sr);
            if (ss == STAIR_STAT.NONE) return bh;

            float ox = x - (float)(ix + MapClusterBuilder.s_centerOffset);
            float oz = z - (float)(iz + MapClusterBuilder.s_centerOffset);

            switch (ss)
            {
                case STAIR_STAT.NEG_Z:
                    return bh + 0.6f - oz;

                case STAIR_STAT.POS_Z:
                    return bh + 0.6f + oz;

                case STAIR_STAT.NEG_X:
                    return bh + 0.6f - ox;

                case STAIR_STAT.POS_X:
                    return bh + 0.6f + ox;

                case STAIR_STAT.RIDGE_NEGZ_NEGX:
                    if (ox > 0) return bh + 0.6f - ox;
                    if (oz > 0) return bh + 0.6f - oz;
                    return bh + 0.6f;

                case STAIR_STAT.RIDGE_NEGZ_POSX:
                    if (ox < 0) return bh + 0.6f + ox;
                    if (oz > 0) return bh + 0.6f - oz;
                    return bh + 0.6f;

                case STAIR_STAT.RIDGE_POSZ_NEGX:
                    if (ox > 0) return bh + 0.6f - ox;
                    if (oz < 0) return bh + 0.6f + oz;
                    return bh + 0.6f;

                case STAIR_STAT.RIDGE_POSZ_POSX:
                    if (ox < 0) return bh + 0.6f + ox;
                    if (oz < 0) return bh + 0.6f + oz;
                    return bh + 0.6f;

                case STAIR_STAT.VALLEY_POSZ_POSX:
                    if (ox > 0) return bh + 0.6f + ox;
                    if (oz > 0) return bh + 0.6f + oz;
                    return bh + 0.6f;

                case STAIR_STAT.VALLEY_POSZ_NEGX:
                    if (ox < 0) return bh + 0.6f - ox;
                    if (oz > 0) return bh + 0.6f + oz;
                    return bh + 0.6f;

                case STAIR_STAT.VALLEY_NEGZ_POSX:
                    if (ox > 0) return bh + 0.6f + ox;
                    if (oz < 0) return bh + 0.6f - oz;
                    return bh + 0.6f;

                case STAIR_STAT.VALLEY_NEGZ_NEGX:
                    if (ox < 0) return bh + 0.6f - ox;
                    if (oz < 0) return bh + 0.6f - oz;
                    return bh + 0.6f;
                default:
                    return bh;

            }

        }

        public int getStairDir(int x, int z)
        {
            int idx = 0;
            while (true)
            {
                Common.Rom.Map.StairRef _sr = mapRom.getStair(idx++);
                if (_sr == null) break;

                if (_sr.x == x && _sr.y == z)
                {
                    return _sr.stat;
                }
            }

            return (int)STAIR_STAT.NONE;
        }

        public int getStairDirAuto(int x, int z)
        {
            // 階段を配置するときは建物の高さを無視する
            updateMapHeightAndWalkableState(false);

            float hC = getTerrainHeight(x, z);
            float[] heights = new float[8]{
            getFurnitureHeight(x, z + 1),//up near//POSZ
            getFurnitureHeight(x, z - 1),//up far	//NEGZ
            getFurnitureHeight(x +1, z),//up left//POSX
            getFurnitureHeight(x -1 , z),

            getFurnitureHeight(x+1,z+1),
            getFurnitureHeight(x-1,z-1),
            getFurnitureHeight(x-1,z+1),
            getFurnitureHeight(x+1,z-1),

            };//up right//NEGX
            bool[] flgs = new bool[8] { false, false, false, false, false, false, false, false };
            int plusonecount = 0;
            int nanamecount = 0;

            for (int i = 0; i < 4; i++)
            {
                //東西南北
                if (heights[i] == hC + 1)
                {
                    flgs[i] = true;
                    plusonecount++;
                }

                //ナナメ位置も見ておく
                if (heights[i + 4] == hC + 1)
                {
                    nanamecount++;
                    flgs[i + 4] = true;
                }
            }

            // 地形の高さ情報を元に戻す
            updateMapHeightAndWalkableState();



            if (plusonecount == 1)
            {
                for (int i = 0; i < 4; i++)//東西南北のうち１つなら確定
                {
                    if (flgs[i]) return i;
                }
            }
            else if (plusonecount == 3)//３つの場合も残り一つから上るように設定できる
            {
                if (!flgs[0]) return (int)STAIR_STAT.NEG_Z;
                if (!flgs[1]) return (int)STAIR_STAT.POS_Z;
                if (!flgs[2]) return (int)STAIR_STAT.NEG_X;
                if (!flgs[3]) return (int)STAIR_STAT.POS_X;
            }
            else if (nanamecount == 1 && plusonecount == 0)//斜め方向に１つだけの場合
            {
                //尾根にしておく。
                if (flgs[4]) return (int)STAIR_STAT.RIDGE_POSZ_POSX;
                if (flgs[5]) return (int)STAIR_STAT.RIDGE_NEGZ_NEGX;
                if (flgs[6]) return (int)STAIR_STAT.RIDGE_POSZ_NEGX;
                if (flgs[7]) return (int)STAIR_STAT.RIDGE_NEGZ_POSX;
            }
            else
            {
                //谷は
                //上下左右にスロープがあるかどうかを見て判断
                bool[] nflgs = new bool[4] { false, false, false, false };
                int idx = 0;
                while (true)
                {
                    //TODO高さもチェックすべき
                    Common.Rom.Map.StairRef _sr = mapRom.getStair(idx++);
                    if (_sr == null) break;
                    if (_sr.x == x && _sr.y == z + 1) nflgs[0] = true;//pos z neighbor
                    if (_sr.x == x && _sr.y == z - 1) nflgs[1] = true;//neg z neighbor
                    if (_sr.x == x + 1 && _sr.y == z) nflgs[2] = true;//pos x neighbor
                    if (_sr.x == x - 1 && _sr.y == z) nflgs[3] = true;//pos x neighbor
                }

                int ncount = 0;
                for (int i = 0; i < 4; i++)
                {
                    if (nflgs[i]) ncount++;
                }

                if (ncount == 2)
                {
                    if (nflgs[0] && nflgs[2]) return (int)STAIR_STAT.VALLEY_NEGZ_NEGX;
                    if (nflgs[0] && nflgs[3]) return (int)STAIR_STAT.VALLEY_NEGZ_POSX;
                    if (nflgs[1] && nflgs[2]) return (int)STAIR_STAT.VALLEY_POSZ_NEGX;
                    if (nflgs[1] && nflgs[3]) return (int)STAIR_STAT.VALLEY_POSZ_POSX;
                }

            }



            //最終的には東西南北どこかにフィットするケースを選ぶ
            for (int i = 0; i < 4; i++)
            {
                if (flgs[i]) return i;
            }

            return 0;
        }

        public STAIR_STAT getStairStat(int x, int z, ref Common.Rom.Map.StairRef sr)
        {
            int idx = 0;
            while (true)
            {
                Common.Rom.Map.StairRef _sr = mapRom.getStair(idx++);
                if (_sr == null) break;

                if (_sr.x == x && _sr.y == z)
                {
                    sr = _sr;
                    return (STAIR_STAT)getStairDir(x, z);
                }
            }

            return STAIR_STAT.NONE;
        }

        public bool isWalkable(int x, int z)
        {
            if (x >= 0 && z >= 0 && x < mapRom.Width && z < mapRom.Height)
            {
                return walkableMap[x, z];
            }
            return false;
        }



        private string addMapObjectInstance(Common.Rom.Map.MapObjectRef mor, List<Common.Rom.RomItem> list)
        {
            //int maxcount = 1;
            //if (onInitialize) maxcount = 0;

            string path = null;

            //リスト検索は呼び出し側で行うことにしました。
            foreach (var i in list)
            {
                if (i.guId == mor.guId)
                {
                    path = ((Common.Resource.MapObject)i).path;
                }
            }

            /*
            list = Common.Catalog.sInstance.getFilteredItemList(typeof(Common.Resource.Furniture));
            foreach (var i in list)
            {
                if (i.guId == mor.guId)
                {
                    path = ((Common.Resource.Furniture)i).path;
                }
            }
            */

            if (path != null)
            {
                //読み込み済みのモデルから探す
                ModifiedModelInstance mmi = Yukar.Engine.Graphics.LoadModel(path);
                //UnityEngine.Debug.Log(path + " x: " + mor.x.ToString() + " y: "+ mor.y.ToString());

                MapObjectInstance p = new MapObjectInstance();
                p.info = mor;
                RomItem ri = Common.Catalog.sInstance.getItemFromGuid(mor.guId);
                p.mo = ri as Common.Resource.MapObject;
                mapobjects.Add(p);

                if (mmi == null)
                {
                    //代替え
                    p.mTentativeBox = new SharpKmyGfx.BoxFillPrimitive();
                    p.mTentativeBox.setColor(0.3f, 0.2f, 1f, 1f);
                    p.mTentativeBox.setSize((p.mo.maxx - p.mo.minx + 1) * 0.45f, (p.mo.maxy - 0 + 1) * 0.45f, (p.mo.maxz - p.mo.minz + 1) * 0.45f);
                    p.mTentativeBox.setShaderName("map_notex");
                    p.mTentativeBox.setCull(SharpKmyGfx.CommonPrimitive.CULLTYPE.BACK);
                }
                else
                {
                    for (int i = 0; i < 100; i++)
                    {
                        SharpKmyGfx.DrawInfo d = mmi.inst.getDrawInfo(i);
                        if (d == null) break;

                        SharpKmyGfx.Texture t = d.getTexture(0);
                        if (t != null) t.setMagFilter(SharpKmyGfx.TEXTUREFILTER.kNEAREST);
                    }

                    //SharpKmyGfx.StaticModelBatcher multi = new SharpKmyGfx.StaticModelBatcher(mmi.modifiedModel.model, maxcount);
                    mmi.modifiedModel.createBacher();
                    if (mmi.modifiedModel.batcher != null)
                    {
                        for (int i = 0; i < 100; i++)
                        {
                            SharpKmyGfx.DrawInfo d = mmi.modifiedModel.batcher.getDrawInfo(i);
                            if (d == null) break;

                            SharpKmyGfx.Texture t = d.getTexture(0);
                            if (t != null)
                            {
                                t.setMagFilter(SharpKmyGfx.TEXTUREFILTER.kNEAREST);
                            }
                        }
                    }

                    //最初のアニメーションを開始する
                    if (mmi.modifiedModel.motions.Count > 0)
                    {
                        mmi.inst.playMotion(mmi.modifiedModel.motions[0].name, 0);
                    }

                    // ライトオンかつlightONモーションがある時はそれを再生する
                    var lightMotionName = "lightON";
                    if (mapRom.lightOnForBuildings)
                    {
                        bool found = false;
                        foreach (var mot in mmi.modifiedModel.motions)
                        {
                            if (mot.name == lightMotionName)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found)
                            mmi.inst.playMotion(lightMotionName, 0);
                    }

                    //p.multiinstance = multi;
                    p.minst = mmi;

                }
            }
            else
            {
                //throw (new Exception("invalid mapobject guid"));
            }

            return path;
        }

        private void recalcMapObjectInstanceMaxCount()
        {
            foreach (var p in mapobjects)
            {
                if (p.minst != null) p.minst.modifiedModel.userCount = 0;
            }

            foreach (var p in mapobjects)
            {
                if (p.minst != null) p.minst.modifiedModel.userCount++;
            }

            foreach (var p in mapobjects)
            {
                if (p.minst == null) continue;
                if (p.minst.modifiedModel.userCount > 0)
                {
                    if (p.minst.modifiedModel.userCount > 32) p.minst.modifiedModel.userCount = 32;
                    p.minst.modifiedModel.setBacherMaxCount(p.minst.modifiedModel.userCount);
                    p.minst.modifiedModel.userCount = 0;
                }
            }
        }

        public Common.Rom.Map.MapObjectRef addMapObject(int x, int z, Guid guid)
        {
            //同じところには置けないルール
            foreach (MapObjectInstance mi in mapobjects)
            {
                if (mi.info.x == x && mi.info.y == z && mi.info.r == 0 && mi.info.guId == guid) return null;
            }

            Common.Rom.Map.MapObjectRef mor = new Map.MapObjectRef();
            mor.x = x;
            mor.y = z;
            mor.guId = guid;
            mor.r = 0;
            mapRom.addMapObject(mor);

            addMapObjectInstance(mor, Common.Catalog.sInstance.getFilteredItemList(typeof(Common.Resource.MapObject)));

            updateMapHeightAndWalkableState();
            recalcMapObjectInstanceMaxCount();

            return mor;
        }

        public void addMapObject(Common.Rom.Map.MapObjectRef mr)
        {
            mapRom.addMapObject(mr);
            addMapObjectInstance(mr, Common.Catalog.sInstance.getFilteredItemList(typeof(Common.Resource.MapObject)));
            updateMapHeightAndWalkableState();
            recalcMapObjectInstanceMaxCount();
        }

        public void removeMapObject(Common.Rom.Map.MapObjectRef m)
        {
            foreach (MapObjectInstance mi in mapobjects)
            {
                if (mi.info == m)
                {
                    //mi.multiinstance.Dispose();
                    //mi.mapobjectmdl.Release();
                    Graphics.UnloadModel(mi.minst);
                    //mi.multiinstance.Release();
                    mapobjects.Remove(mi);
                    break;
                }
            }

            mapRom.removeMapObject(m);
            updateMapHeightAndWalkableState();
            recalcMapObjectInstanceMaxCount();
        }

        public Common.Rom.Map.EventRef addEvent(int x, int z)
        {
            //同じところには置けないルール
            int idx = 0;
            while (true)
            {
                Common.Rom.Map.EventRef sr = mapRom.getEvent(idx++);
                if (sr == null) break;
                if (sr.x == x && sr.y == z) return null;
            }

            //表示インスタンスをここで作る必要があるかも

            Common.Rom.Map.EventRef er = new Map.EventRef();
            er.x = x;
            er.y = z;
            mapRom.addEvent(er);

            return er;
        }

        public void addEvent(Common.Rom.Map.EventRef ev)
        {
            mapRom.addEvent(ev);
        }

        public void removeEvent(Map.EventRef ev)
        {
            mapRom.removeEvent(ev);
        }

        public Common.Rom.Map.StairRef addStair(int x, int z, int index)
        {
            //同じところには置けないルール
            int idx = 0;
            while (true)
            {
                Common.Rom.Map.StairRef sr = mapRom.getStair(idx++);
                if (sr == null) break;

                if (sr.x == x && sr.y == z) return null;

            }

            MapClusterBuilder c = getClusterByPosition(x, z);
            if (c != null)
            {
                Common.Rom.Map.StairRef s = new Map.StairRef();
                s.x = x;
                s.y = z;
                s.stat = getStairDirAuto(x, z);
                s.gfxId = index;
                mapRom.addStair(s);
                c.geomInvalidate();
                c.updateGeometry();//必要なら近隣も処理する

                {
                    List<MapClusterBuilder> list = new List<MapClusterBuilder>();

                    //int cx = c.ix;
                    //int cy = c.iz;

                    bool far = false;
                    bool near = false;
                    bool left = false;
                    bool right = false;

                    if (z % MapData.TIPS_IN_CLUSTER == 0)
                    {
                        far = true;
                    }
                    if (z % MapData.TIPS_IN_CLUSTER == MapData.TIPS_IN_CLUSTER - 1)
                    {
                        near = true;
                    }
                    if (x % MapData.TIPS_IN_CLUSTER == 0)
                    {
                        left = true;
                    }
                    if (x % MapData.TIPS_IN_CLUSTER == MapData.TIPS_IN_CLUSTER - 1)
                    {
                        right = true;
                    }

                    if (far)
                    {
                        if (right)
                        {
                            c = getClusterByPosition(x + 1, z - 1);
                            if (c != null) list.Add(c);
                        }
                        else if (left)
                        {
                            c = getClusterByPosition(x - 1, z - 1);
                            if (c != null) list.Add(c);
                        }
                    }
                    if (near)
                    {
                        if (right)
                        {
                            c = getClusterByPosition(x + 1, z + 1);
                            if (c != null) list.Add(c);
                        }
                        else if (left)
                        {
                            c = getClusterByPosition(x - 1, z + 1);
                            if (c != null) list.Add(c);
                        }
                    }
                    if (far)
                    {
                        c = getClusterByPosition(x, z - 1);
                        if (c != null) list.Add(c);
                    }
                    if (near)
                    {
                        c = getClusterByPosition(x, z + 1);
                        if (c != null) list.Add(c);
                    }
                    if (right)
                    {
                        c = getClusterByPosition(x + 1, z);
                        if (c != null) list.Add(c);
                    }
                    if (left)
                    {
                        c = getClusterByPosition(x - 1, z);
                        if (c != null) list.Add(c);
                    }

                    foreach (var i in list)
                    {
                        if (i != null)
                        {
                            i.geomInvalidate();
                            i.updateGeometry();
                        }
                    }


                }


                return s;
            }

            return null;
        }

        public void addStair(Common.Rom.Map.StairRef sr)
        {
            mapRom.addStair(sr);
            sr.stat = getStairDirAuto(sr.x, sr.y);
            MapClusterBuilder c = getClusterByPosition(sr.x, sr.y);
            if (c != null)
            {
                callUpdateGeometry(c);
            }
        }

        public void removeStair(Common.Rom.Map.StairRef s)
        {
            mapRom.removeStair(s);
        }

        public void setTerrainHeight(int x, int z, int height)
        {
            setTerrain(x, z, height, mapRom.getTerrainAttrib(x, z));
        }

        public void setTerrainAttr(int x, int z, int attr)
        {
            setTerrain(x, z, mapRom.getTerrainHeight(x, z), attr);
        }

        bool geomUpdatable = true;

        public void updateGeomEnable(bool flg)
        {
            geomUpdatable = flg;
            if (flg)
            {
                updateMapHeightAndWalkableState();
                for (int y = 0; y < mClusters.GetLength(1); y++)
                {
                    for (int x = 0; x < mClusters.GetLength(0); x++)
                    {
                        mClusters[x, y].updateGeometry();
                    }
                }
            }
        }

        void callUpdateGeometry(MapClusterBuilder c)
        {
            c.geomInvalidate();
            if (geomUpdatable)
            {
                c.updateGeometry();
            }
        }

        public void setTerrain(int x, int z, int height, int attrib)
        {
            if (height < MIN_HEIGHT) return;
            if (height > MAX_HEIGHT) return;

            if (height == getTerrainHeight(x, z) &&
                attrib == mapRom.getTerrainAttrib(x, z)) return;

            mapRom.setTerrain(x, z, attrib, height);

            if (geomUpdatable) updateMapHeightAndWalkableState();

            MapClusterBuilder c = getClusterByPosition(x, z);
            if (c != null) callUpdateGeometry(c);

            //周囲のクラスタのアップデートが必要かどうか
            int lx = x % MapData.TIPS_IN_CLUSTER;
            int lz = z % MapData.TIPS_IN_CLUSTER;
            if (lx == 0)
            {
                c = getClusterByPosition(x - 1, z);
                if (c != null) callUpdateGeometry(c);
            }
            if (lx == MapData.TIPS_IN_CLUSTER - 1)
            {
                c = getClusterByPosition(x + 1, z);
                if (c != null) callUpdateGeometry(c);
            }
            if (lz == 0)
            {
                c = getClusterByPosition(x, z - 1);
                if (c != null) callUpdateGeometry(c);
            }
            if (lz == MapData.TIPS_IN_CLUSTER - 1)
            {
                c = getClusterByPosition(x, z + 1);
                if (c != null) callUpdateGeometry(c);
            }
            if (lx == 0 && lz == 0)
            {
                c = getClusterByPosition(x - 1, z - 1);
                if (c != null) callUpdateGeometry(c);
            }
            if (lx == MapData.TIPS_IN_CLUSTER - 1 && lz == 0)
            {
                c = getClusterByPosition(x + 1, z - 1);
                if (c != null) callUpdateGeometry(c);
            }
            if (lx == 0 && lz == MapData.TIPS_IN_CLUSTER - 1)
            {
                c = getClusterByPosition(x - 1, z + 1);
                if (c != null) callUpdateGeometry(c);
            }
            if (lx == MapData.TIPS_IN_CLUSTER - 1 && lz == MapData.TIPS_IN_CLUSTER - 1)
            {
                c = getClusterByPosition(x + 1, z + 1);
                if (c != null) callUpdateGeometry(c);
            }

            if (geomUpdatable) updateMapHeightAndWalkableState();
        }

        public int getChipIndex(Common.Resource.MapChip parts, bool autoAdd = true)
        {
            if (parts == null)
                return -1;

            Dictionary<Guid, int> revList;
            if (parts.type == Common.Resource.ChipType.STAIR_OR_SLOPE)
                revList = mapRom.getStairListRev();
            else
                revList = mapRom.getChipListRev();
        
            if (!revList.ContainsKey(parts.guId))
            {
                if (autoAdd)
                {
                    Dictionary<int, Guid> list;
                    if (parts.type == Common.Resource.ChipType.STAIR_OR_SLOPE)
                        list = mapRom.getStairList();
                    else
                        list = mapRom.getChipList();
                    
                    int index = -1;
                    for (int i = 0; i < short.MaxValue; i++)
                    {
                        if (!list.ContainsKey(i))
                        {
                            index = i;
                            break;
                        }
                    }
                    if (index < 0)
                        return -1;

                    list.Add(index, parts.guId);
                    revList.Add(parts.guId, index);
                    if (maptexture != null) maptexture.Release();
                    maptexture = chipSetInfo.build(mapRom);
                    //maptexture.setMinFilter(SharpKmyGfx.TEXTUREFILTER.kBILINEAR);
                    updateGeom();
                }
                else
                {
                    return -1;
                }
            }
            return revList[parts.guId];
        }

        public void updateGeom()
        {
            if (mClusters != null && mClusters[0, 0] != null)
            {
                for (int y = 0; y < mClusters.GetLength(1); y++)
                {
                    for (int x = 0; x < mClusters.GetLength(0); x++)
                    {
                        mClusters[x, y].geomInvalidate();
                        mClusters[x, y].updateGeometry();
                    }
                }
            }


            if (waterMesh != null)
                waterMesh.Reset();
#if WINDOWS
            waterMesh = new WaterMesh(this);
#else
            if (Yukar.Common.Rom.GameSettings.IsDivideMapScene == false
            || UnityEntry.IsImportMapScene())
            {
                waterMesh = new WaterMesh(this);
            }
#endif
        }

        public Common.Resource.MapChip getBgChip()
        {
            var result = Common.Catalog.sInstance.getItemFromGuid(mapRom.bgChip) as Common.Resource.MapChip;
            return result;
        }

        public Guid getSeaGuid()
        {
            var result = Common.Catalog.sInstance.getFullList().Find(
                x => x is Common.Resource.MapChip && x.name.IndexOf("Sea") >= 0 &&
                    ((Common.Resource.MapChip)x).isSystemResource());
            if (result != null)
                return result.guId;
            return Guid.Empty;
        }

        public float getRoofAdjustedHeight(float dx, float dz, float dy)
        {
            var tmpY = getAdjustedHeight(dx, dz);

            // くぐれる地形 かつ、初めての座標決定(Y=0)以外 もしくは、建物扱いのオブジェ以外 だった場合は地面からの距離と近い方を採用する
            int ax = (int)(dx);
            int ay = (int)(dz);
            var furnitureY = getFurnitureHeight(ax, ay, (int)dy);
            var terrainY = getTerrainHeight(ax, ay);

            if ((dy >= 1 || terrainY == furnitureY) && getCellIsRoof(ax, ay))
            {
                var diffA = Math.Abs(dy - furnitureY);
                var diffB = Math.Abs(dy - terrainY);
                if (diffA > diffB)
                {
                    return terrainY;
                }
                else
                {
                    return furnitureY;
                }
            }
            else
            {
                return tmpY;
            }
        }

#if WINDOWS && ENABLE_EXPORT_RESOURCE
        string exportDir;
        public void exportMapModel(string fileName, bool division)
        {
            if (mClusters != null)
            {
                // テクスチャ出力
                System.Drawing.Bitmap mapTex = chipSetInfo.exportMapTexture(mapRom);
                mapTex.Save(fileName + ".png", System.Drawing.Imaging.ImageFormat.Png);
                mapTex.Dispose();

                if (division)
                {
                    // FBX分割出力
                    for (int x = 0; x < mHcount; x++)
                    {
                        for (int z = 0; z < mDcount; z++)
                        {
                            System.String fn = fileName + x + z;
                            MapClusterBuilder c = getClusterByClusterIndex(x, z);
                            c.geomInvalidate();
                            c.updateGeometry();
                            c.exportFBX(fn, fileName);
                        }
                    }
                }
                else
                {
                    // FBXマージ出力
                    List<FBXExporter.VertexPositionNormalTexture2Color> vl = new List<FBXExporter.VertexPositionNormalTexture2Color>();
                    for (int x = 0; x < mHcount; x++)
                    {
                        for (int z = 0; z < mDcount; z++)
                        {
                            MapClusterBuilder c = getClusterByClusterIndex(x, z);
                            c.geomInvalidate();
                            c.updateGeometry();
                            c.mergeFBX(vl);
                        }
                    }
                    FBXExporter.FBXConv.Export(vl, fileName, fileName);
                }

                // 最後に保存したフォルダを記録
                exportDir = Common.Util.file.getDirName(fileName);
            }
        }
#endif
    }

    public class SkyDrawer
    {
        public List<ModifiedModelInstance> skyModelList = new List<ModifiedModelInstance>();
        private SharpKmyMath.Matrix4 proj;
        Vector3 offset = new Vector3();

        public SkyDrawer(MapData owner)
        {
            var catalog = Common.Catalog.sInstance;
            Common.Resource.MapBackground rom = null;
            var mapRom = owner.mapRom;

            if (mapRom.bgType == Map.BgType.MODEL)
            {
                rom = catalog.getItemFromGuid(mapRom.skyModel) as Common.Resource.MapBackground;
            }

            if (rom != null)
                skyModelList.Add(Graphics.LoadModel(rom.path));

            foreach (var model in skyModelList.ToArray())
            {
                if (model == null || model.inst == null)
                {
                    skyModelList.Remove(model);
                    continue;
                }

                int didx = 0;
                while (true)
                {
                    SharpKmyGfx.DrawInfo d = model.inst.getDrawInfo(didx);
                    if (d == null) break;
                    d.setDepthWrite(false);
                    d.setCull(SharpKmyGfx.CULLTYPE.kBACK);
                    d.setLayer(-1);
                    didx++;
                }
            }
        }

        public void Reset()
        {
            skyModelList.ForEach(x => Graphics.UnloadModel(x));
            skyModelList.Clear();
        }

        internal void Draw(SharpKmyGfx.Render scn)
        {
#if ENABLE_VR

            var mtxView = new SharpKmyMath.Matrix4();
            var mtxProjSave = new SharpKmyMath.Matrix4();
            var mtxProj = proj;

            scn.getViewMatrix(ref mtxProjSave, ref mtxView);

            if (SharpKmyVr.Func.IsReady())
            {
                if (SharpKmyGfx.Render.isSameScene(scn, SharpKmyGfx.Render.getRenderL()))
                {
                    mtxProj = SharpKmyVr.Func.GetProjectionMatrix(SharpKmyVr.EyeType.Left);
                }
                else if (SharpKmyGfx.Render.isSameScene(scn, SharpKmyGfx.Render.getRenderR()))
                {
                    mtxProj = SharpKmyVr.Func.GetProjectionMatrix(SharpKmyVr.EyeType.Right);
                }
            }

            scn.setViewMatrix(mtxProj, mtxView);

#if WINDOWS
            SharpKmyMath.Matrix4 lm = SharpKmyMath.Matrix4.translate(offset.X, offset.Y, offset.Z);

            skyModelList.ForEach(x => x.inst.setL2W(lm));
#else
            skyModelList.ForEach(x => x.inst.instance.transform.localScale = x.inst.template.obj.transform.localScale * SharpKmyGfx.ModelData.SCALE_FOR_UNITY);
            skyModelList.ForEach(x => x.inst.instance.transform.localPosition = Common.UnityUtil.convertToUnityVector3(offset));
#endif

            skyModelList.ForEach(x => x.inst.draw(scn));
            scn.setViewMatrix(mtxProjSave, mtxView);

#else   // #if ENABLE_VR

            var tmp = new SharpKmyMath.Matrix4();
            var view = new SharpKmyMath.Matrix4();
            scn.getViewMatrix(ref tmp, ref view);
            scn.setViewMatrix(proj, view);
            skyModelList.ForEach(x => x.inst.draw(scn));
            scn.setViewMatrix(tmp, view);

#endif  // #if ENABLE_VR
        }

        internal void setPlayerOffset(SharpKmyMath.Vector3 po)
        {
#if WINDOWS
            offset.X = po.x;
#else
            offset.X = -po.x;
#endif
            offset.Y = 0;
            offset.Z = po.z;
        }

        public void setProj(SharpKmyMath.Matrix4 matrix4)
        {
            this.proj = matrix4;
        }
    }

    internal class EnvironmentEffect
    {
        public SharpKmyGfx.ParticleInstance env;
        //private MapData owner;

        public EnvironmentEffect(MapData owner)
        {
            //this.owner = owner;
            string path = null;
            switch (owner.mapRom.envEffect)
            {
                case Map.EnvironmentEffectType.RAIN:
                    path = Common.Catalog.sCommonResourceDir + "res\\character\\3D\\particle\\ef001_Rain.ptcl";
                    break;
                case Map.EnvironmentEffectType.SNOW:
                    path = Common.Catalog.sCommonResourceDir + "res\\character\\3D\\particle\\ef002_Snow.ptcl";
                    break;
                case Map.EnvironmentEffectType.STORM:
                    path = Common.Catalog.sCommonResourceDir + "res\\character\\3D\\particle\\ef010_Storm.ptcl";
                    break;
                case Map.EnvironmentEffectType.MIST:
                    path = Common.Catalog.sCommonResourceDir + "res\\character\\3D\\particle\\ef014_Mist.ptcl";
                    break;
                case Map.EnvironmentEffectType.CONFETTI:
                    path = Common.Catalog.sCommonResourceDir + "res\\character\\3D\\particle\\ef012_Paper.ptcl";
                    break;
                case Map.EnvironmentEffectType.COLD_WIND:
                    path = Common.Catalog.sCommonResourceDir + "res\\character\\3D\\particle\\ef015_ColdWind.ptcl";
                    break;
            }
            if (path != null)
                env = Graphics.LoadParticle(path);

            if (env != null)
            {
                env.start(SharpKmyMath.Matrix4.identity());
                var shd = SharpKmyGfx.Shader.load("particle");
                var dlist = env.getDrawInfo();
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

        public void Reset()
        {
            if (env != null)
                Graphics.UnloadParticle(env);
        }

        internal void Draw(SharpKmyGfx.Render scn, SharpKmyMath.Vector3 pos)
        {
            if (env != null)
            {
                env.update(GameMain.getElapsedTime(), SharpKmyMath.Matrix4.translate(pos.x, pos.y, pos.z));
                var dlist = env.getDrawInfo();
                if (dlist != null)
                {
                    foreach (var i in dlist)
                    {
                        MapData.setFogParam(i);
                    }
                }

                env.draw(scn);
            }
        }
    }

    class WaterMesh
    {
        public SharpKmyGfx.VertexPositionNormalTextureColor[] vtx;
        public SharpKmyGfx.VertexBuffer vb;

        public WaterMesh(MapData owner)
        {
            var seaRom = owner.getBgChip();
            if (seaRom == null)
                return;

            int cc = MapData.TIPS_IN_CLUSTER;
            int vc = cc * cc * 6;

            vb = new SharpKmyGfx.VertexBuffer();
            vtx = new SharpKmyGfx.VertexPositionNormalTextureColor[vc];

            int vstep = 0;

            //SharpKmyGfx.Color color = MapClusterBuilder.getColorByHeight(1);

            SharpKmyMath.Vector2 tc0 = owner.getTerrainTexCoordTopLeft(owner.getChipIndex(seaRom), 0);
            SharpKmyMath.Vector2 tc1 = owner.getTerrainTexCoordBottomRight(owner.getChipIndex(seaRom), 0);

            SharpKmyGfx.Color c = MapClusterBuilder.getColorByHeight(1);

            float height = 1 - (seaRom.liquid ? MapClusterBuilder.WATER_TOP_OFFST : 0);
            for (int z = 0; z < cc; z++)
            {
                for (int x = 0; x < cc; x++)
                {
                    vtx[vstep + 0].pos = new SharpKmyMath.Vector3(x + 0, height, z + 0);
                    vtx[vstep + 1].pos = new SharpKmyMath.Vector3(x + 1, height, z + 0);
                    vtx[vstep + 2].pos = new SharpKmyMath.Vector3(x + 1, height, z + 1);

                    vtx[vstep + 3].pos = (new SharpKmyMath.Vector3(x + 1, height, z + 1));
                    vtx[vstep + 4].pos = (new SharpKmyMath.Vector3(x + 0, height, z + 1));
                    vtx[vstep + 5].pos = (new SharpKmyMath.Vector3(x + 0, height, z + 0));

                    vtx[vstep + 0].color = (c);
                    vtx[vstep + 1].color = (c);
                    vtx[vstep + 2].color = (c);
                    vtx[vstep + 3].color = (c);
                    vtx[vstep + 4].color = (c);
                    vtx[vstep + 5].color = (c);

                    vtx[vstep + 0].tc = new SharpKmyMath.Vector2(tc0.x, tc0.y);
                    vtx[vstep + 1].tc = new SharpKmyMath.Vector2(tc1.x, tc0.y);
                    vtx[vstep + 2].tc = new SharpKmyMath.Vector2(tc1.x, tc1.y);

                    vtx[vstep + 3].tc = new SharpKmyMath.Vector2(tc1.x, tc1.y);
                    vtx[vstep + 4].tc = new SharpKmyMath.Vector2(tc0.x, tc1.y);
                    vtx[vstep + 5].tc = new SharpKmyMath.Vector2(tc0.x, tc0.y);

                    vtx[vstep + 0].normal = (new SharpKmyMath.Vector3(0, 1, 0));
                    vtx[vstep + 1].normal = (new SharpKmyMath.Vector3(0, 1, 0));
                    vtx[vstep + 2].normal = (new SharpKmyMath.Vector3(0, 1, 0));
                    vtx[vstep + 3].normal = (new SharpKmyMath.Vector3(0, 1, 0));
                    vtx[vstep + 4].normal = (new SharpKmyMath.Vector3(0, 1, 0));
                    vtx[vstep + 5].normal = (new SharpKmyMath.Vector3(0, 1, 0));

                    vstep += 6;
                }
            }

            if (vb != null && vtx != null)
            {
                vb.setData(vtx);
            }
        }

        public void Reset()
        {
            if (vb != null)
                vb.Release();
            vb = null;
        }

    }
}

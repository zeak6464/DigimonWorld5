using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Yukar.Common.Rom
{
    public partial class Map : RomItem
    {
        const int nowVersion = 8;   // マップ設定のロムバージョン 下位互換がなくなる時は Catalog の sRomVersion の方を更新してください。

        private int[,] heightMap;
        private int[,] attribMap;

        //地形のキャッシュ
        private class CacheInfo : IEquatable<CacheInfo>
        {
            public Guid guid;
            public int x;
            public int y;

            public bool Equals(CacheInfo other)
            {
                return guid == other.guid && x == other.x && y == other.y;
            }

            public override int GetHashCode()
            {
                return guid.GetHashCode() ^ (x + (y << 8)).GetHashCode();
            }
        }
        const int MAX_CACHE_NUM = 512;
        static Dictionary<CacheInfo, List<SharpKmyGfx.VertexPositionNormalTexture2Color>> caches =
            new Dictionary<CacheInfo, List<SharpKmyGfx.VertexPositionNormalTexture2Color>>();
        static List<CacheInfo> cachesLog = new List<CacheInfo>();

        public Guid chipSet;
        public Color dirLightColor;
        public Color ambLightColor;
        public bool lightOnForBuildings;

        public string category;

        public bool HasCategory { get { return category != ""; } }

        public class MapObjectBase
        {
            public int x;
            public int y;
        }

        public class EventRef : MapObjectBase
        {
            public Guid guId;
            public int prevX, prevY;
        }

        public class MapObjectRef : MapObjectBase
        {
            public int r;//回転0~3
            public Guid guId;
        }

        public class StairRef : MapObjectBase
        {
            public int gfxId;
            public int stat;
        }

        public class ObjectGroup : RomItem
        {
            public static Map currentMap;
            public List<MapObjectRef> mapobjects = new List<MapObjectRef>();

            public override void save(System.IO.BinaryWriter writer)
            {
                if (currentMap == null)
                    return;

                var indexes = mapobjects
                    .Where(obj => currentMap.mapobjects.Contains(obj))
                    .Select(obj => currentMap.mapobjects.IndexOf(obj)).ToArray();

                if (indexes.Length == 0)
                    return;

                writer.Write(indexes.Length);
                foreach (var index in indexes)
                {
                    writer.Write(index);
                }
            }

            public override void load(System.IO.BinaryReader reader)
            {
                if (currentMap == null)
                    return;

                mapobjects.Clear();
                var count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    int index = reader.ReadInt32();
                    if (currentMap.mapobjects.Count > index)
                    {
                        mapobjects.Add(currentMap.mapobjects[index]);
                    }
                }
            }
        }

        /**
		 *	カメラ制御モード
		 */
        public enum CameraControlMode
        {
            UNKNOWN = -1,   // 不明
            NORMAL,         // 通常
            VIEW,           // FPS
#if ENABLE_GHOST_MOVE
			GHOST,			// ゴースト
#endif // #if ENABLE_GHOST_MOVE
            NUM             // 定義数
        }

        List<EventRef> events;
        List<MapObjectRef> mapobjects;
        List<StairRef> stairs;
        public List<ObjectGroup> groups;

        public const int maxAreaBattleSettings = 999;
        public const int maxEncounts = 999;
        public const int minEncountSteps = 1;
        public const int maxEncountSteps = 100;
        const int defaultEncountSteps = 20;
        const int defaultEncountMinStepPercent = 75;
        const int defaultEncountMaxStepPercent = 125;
        const int defaultEncountMinSteps = defaultEncountSteps * defaultEncountMinStepPercent / 100; // 出現までの最小移動歩数(初期値)
        const int defaultEncountMaxSteps = defaultEncountSteps * defaultEncountMaxStepPercent / 100; // 出現までの最大移動歩数(初期値)
        const int defaultEncountPercent = 100; // 出現確率(初期値)
        const int defaultMaxMonsters = 3; // 最大同時出現数(初期値)
        const int defaultEncountWeight = 10; // リーダーモンスター選択用の重み(初期値)

        public class BattleSetting : RomItem
        {
            static BattleSetting empty;

            public static BattleSetting Empty
            {
                get
                {
                    if (empty == null)
                    {
                        empty = new BattleSetting();

                        empty.encountMinSteps =
                        empty.encountMaxSteps =
                        empty.encountPercent = 0;

                        empty.maxMonsters = 0;

                        empty.battleBgCenterX =
                        empty.battleBgCenterY = 0;
                    }

                    return empty;
                }
            }

            public int encountMinSteps = defaultEncountMinSteps; // 出現までの最小移動歩数
            public int encountMaxSteps = defaultEncountMaxSteps; // 出現までの最大移動歩数
            public int encountPercent = defaultEncountPercent; // 出現確率

            public List<Encount> encounts = new List<Encount>(); // 出現モンスター
            public int maxMonsters = defaultMaxMonsters; // 最大同時出現数

            public Guid battleBg; // 背景

            // 背景の中央の座標（ゲーム中のマップ選択時）
            public int battleBgCenterX;
            public int battleBgCenterY;

            public bool useDefaultBattleBgm = true;
            public Guid battleBgm; // BGM
            public Guid battleBgs; // 環境音

            public override void save(System.IO.BinaryWriter inWriter)
            {
                base.save(inWriter);

                inWriter.Write(encountMinSteps);
                inWriter.Write(encountMaxSteps);
                inWriter.Write(encountPercent);

                inWriter.Write(encounts.Count);

                foreach (var item in encounts)
                {
                    inWriter.Write(item.weight);
                    inWriter.Write(item.enemy.ToByteArray());
                }

                inWriter.Write(maxMonsters);

                inWriter.Write(battleBg.ToByteArray());

                inWriter.Write(battleBgCenterX);
                inWriter.Write(battleBgCenterY);

                inWriter.Write(useDefaultBattleBgm);
                inWriter.Write(battleBgm.ToByteArray());
                inWriter.Write(battleBgs.ToByteArray());
            }

            public override void load(System.IO.BinaryReader inReader)
            {
                base.load(inReader);

                Load(inReader);
            }

            public virtual void Load(System.IO.BinaryReader inReader)
            {
                encountMinSteps = Math.Min(Math.Max(inReader.ReadInt32(), minEncountSteps), maxEncountSteps);
                encountMaxSteps = Math.Min(Math.Max(inReader.ReadInt32(), minEncountSteps), maxEncountSteps);

                if (encountMaxSteps < encountMinSteps)
                {
                    var tmp = encountMinSteps;

                    encountMinSteps = encountMaxSteps;
                    encountMaxSteps = tmp;
                }

                encountPercent = inReader.ReadInt32();

                var cnt = inReader.ReadInt32();

                for (int i = 0; i < cnt; i++)
                {
                    var encount = new Encount();

                    encount.weight = inReader.ReadInt32();
                    encount.enemy = Util.readGuid(inReader);

                    encounts.Add(encount);
                }

                maxMonsters = inReader.ReadInt32();

                battleBg = Util.readGuid(inReader);
                battleBgCenterX = inReader.ReadInt32();
                battleBgCenterY = inReader.ReadInt32();

                useDefaultBattleBgm = inReader.ReadBoolean();
                battleBgm = Util.readGuid(inReader);
                battleBgs = Util.readGuid(inReader);
            }

            public List<Encount> getAvailableEncounts(Catalog catalog)
            {
                return encounts.Where(x => catalog.getItemFromGuid(x.enemy) is Monster).ToList();
            }

            public int getAvailableMonsterCount(Catalog catalog)
            {
                return encounts.Count(x => catalog.getItemFromGuid(x.enemy) is Monster);
            }
        }

        public class AreaBattleSetting : BattleSetting
        {
            public bool enable = true;
            public Rectangle areaRect; // マップ上の対象範囲

            public override void save(BinaryWriter inWriter)
            {
                base.save(inWriter);

                inWriter.Write(areaRect.X);
                inWriter.Write(areaRect.Y);
                inWriter.Write(areaRect.Width);
                inWriter.Write(areaRect.Height);

                inWriter.Write(enable);
            }

            public override void Load(BinaryReader inReader)
            {
                base.Load(inReader);

                areaRect.X = inReader.ReadInt32();
                areaRect.Y = inReader.ReadInt32();
                areaRect.Width = inReader.ReadInt32();
                areaRect.Height = inReader.ReadInt32();

                enable = inReader.ReadBoolean();
            }
        }

        public BattleSetting mapBattleSetting;
        public List<AreaBattleSetting> areaBattleSettings = new List<AreaBattleSetting>();
        public class Encount
        {
            public int weight = defaultEncountWeight;
            public Guid enemy;
        }

        public Guid mapBgm;
        public Guid mapBgs;
        public bool fixCamera = false;
        public bool fixCameraMode = false;
        public CameraControlMode cameraMode = CameraControlMode.NORMAL; // ROMは本当は書き換えちゃダメなんだけど、エンジンで書き換えてしまっている・・・そのまま保存しないよう要注意
        public CameraControlMode origCameraMode = CameraControlMode.NORMAL; // こっちにもともとのROMの設定を入れておくことにする
        public ThirdPersonCameraSettings tpCamera;
        public FirstPersonCameraSettings fpCamera;

        public OutsideType outsideType;
        public float[] bgcolor;
        public Guid bgChip = Guid.Empty;
        public EnvironmentEffectType envEffect;
        public BgType bgType;
        public Guid skyModel;
        public float[] fogColor;
        public float[] skyPos = new float[] { 0, 0, 0 };
        public bool skyFollowPlayer = true;

        public DateTime modifiedTime;
        public DateTime loadedTime;    // DIFFの時、日付を差分と認識しないようにするための変数
        private Dictionary<int, Guid> chipList = new Dictionary<int, Guid>();
        private Dictionary<int, Guid> stairList = new Dictionary<int, Guid>();

        private Dictionary<Guid, int> chipListRev = new Dictionary<Guid, int>();
        private Dictionary<Guid, int> stairListRev = new Dictionary<Guid, int>();

        public float shadowRotateY = -0.7f;
        public float shadowRotateX = -1.2f;

        public string mapSceneName = "";
        public bool mapSceneNameModified = false;

        public Map()
        {
            heightMap = new int[TERRAIN_DEFAULT_WIDTH, TERRAIN_DEFAULT_HEIGHT];
            attribMap = new int[TERRAIN_DEFAULT_WIDTH, TERRAIN_DEFAULT_HEIGHT];
            for (int x = 0; x < TERRAIN_DEFAULT_WIDTH; x++)
            {
                for (int z = 0; z < TERRAIN_DEFAULT_HEIGHT; z++)
                {
                    heightMap[x, z] = 1;
                    attribMap[x, z] = 0;
                }
            }

            /*
			clusters = new MapCluster[DEFAULT_WIDTH, DEFAULT_HEIGHT];
			for (int z = 0; z < DEFAULT_HEIGHT; z++)
			{
				for (int x = 0; x < DEFAULT_WIDTH; x++)
				{
					clusters[x, z] = new MapCluster();
				}
			}
			*/

            events = new List<EventRef>();
            mapobjects = new List<MapObjectRef>();
            stairs = new List<StairRef>();
            groups = new List<ObjectGroup>();
            bgcolor = new float[4];
            fogColor = new float[4];
            dirLightColor = new Color(214, 202, 188);
            ambLightColor = new Color(188, 174, 156);

            category = "";

            mapBattleSetting = new BattleSetting();

            areaBattleSettings = new List<AreaBattleSetting>();
        }

        //地形キャッシュの設定
        public void setCache(int x, int z, List<SharpKmyGfx.VertexPositionNormalTexture2Color> c)
        {
            var key = new CacheInfo() { guid = guId, x = x, y = z };

            caches[key] = c;
            cachesLog.Remove(key);
            cachesLog.Add(key);

            if (cachesLog.Count > MAX_CACHE_NUM)
            {
                caches.Remove(cachesLog[0]);
                cachesLog.RemoveAt(0);
            }
        }
        public void clearCache()
        {
            foreach (var key in caches.Keys.ToArray())
            {
                if (key.guid == guId)
                {
                    caches.Remove(key);
                    cachesLog.Remove(key);
                }
            }
        }
        public static void clearAllCaches()
        {
            caches.Clear();
            cachesLog.Clear();
        }
        //地形キャッシュの取得
        public List<SharpKmyGfx.VertexPositionNormalTexture2Color> getCache(int x, int z)
        {
            var key = new CacheInfo() { guid = guId, x = x, y = z };

            if (!caches.ContainsKey(key))
                return null;

            return caches[key];
        }
        public int Width
        {
            get { return heightMap.GetLength(0); }
        }

        public int Height
        {
            get { return heightMap.GetLength(1); }
        }

        public bool InnerOfData(int x, int z)
        {
            return !outOfData(x, z);
        }

        private bool outOfData(int x, int z)
        {
            return (x < 0 || z < 0 || x >= Width || z >= Height);
        }

        public int getTerrainAttrib(int x, int z)
        {
            if (outOfData(x, z)) return 0;
            return attribMap[x, z];
        }

        public int getTerrainHeight(int x, int z)
        {
            if (outOfData(x, z)) return 0;
            return heightMap[x, z];
        }

        public void setTerrain(int x, int z, int attrib, int height)
        {
            if (outOfData(x, z)) return;
            heightMap[x, z] = height;
            attribMap[x, z] = attrib;
        }

        public void setTerrainAttrib(int x, int z, int attrib)
        {
            if (outOfData(x, z)) return;
            attribMap[x, z] = attrib;
        }

        public void setTerrainHeight(int x, int z, int height)
        {
            if (outOfData(x, z)) return;
            heightMap[x, z] = height;
        }

        public void addMapObject(MapObjectRef mo)
        {
            mapobjects.Add(mo);
        }

        public void removeMapObject(MapObjectRef mo)
        {
            mapobjects.Remove(mo);
        }

        public MapObjectRef getMapObject(int index)
        {
            if (index < mapobjects.Count) return mapobjects[index];
            return null;
        }

        public void addStair(StairRef s)
        {
            stairs.Add(s);
        }

        public void removeStair(StairRef s)
        {
            stairs.Remove(s);
        }

        public StairRef getStair(int index)
        {
            if (index < stairs.Count) return stairs[index];
            return null;
        }

        public void setSize(int w, int h)
        {
            int[,] nattrib = new int[w, h];
            int[,] nheight = new int[w, h];

            for (int x = 0; x < w; x++)
            {
                for (int z = 0; z < h; z++)
                {
                    if (x < Width && z < Height)
                    {
                        nattrib[x, z] = attribMap[x, z];
                        nheight[x, z] = heightMap[x, z];
                    }
                    else
                    {
                        nattrib[x, z] = 0;
                        nheight[x, z] = 1;
                    }

                }
            }

            heightMap = nheight;
            attribMap = nattrib;

        }


        public void addEvent(EventRef ev)
        {
            events.Add(ev);
        }

        public void removeEvent(EventRef ev)
        {
            events.Remove(ev);
        }

        public EventRef getEvent(int idx)
        {
            if (idx >= events.Count) return null;
            return events[idx];
        }

        public EventRef getEvent(int x, int z)
        {
            foreach (EventRef ev in events)
            {
                if (ev.x == x && ev.y == z) return ev;
            }
            return null;
        }

        public EventRef getEvent(Guid evGuid)
        {
            return events.FirstOrDefault(x => x.guId == evGuid);
        }

        public override void save(System.IO.BinaryWriter writer)
        {
            base.save(writer);
            writer.Write(nowVersion);//バージョン

            writer.Write(category);

            // 時間を書き出し
            writer.Write(modifiedTime.Ticks);

            // イベントを書き出し
            writer.Write(events.Count);
            foreach (var ev in events)
            {
                writer.Write(ev.x);
                writer.Write(ev.y);
                writer.Write(ev.guId.ToByteArray());
            }

            // エンカウント設定を書き出し
            writeChunk(writer, mapBattleSetting);

            writer.Write(areaBattleSettings.Count);

            foreach (var item in areaBattleSettings)
            {
                writeChunk(writer, item);
            }

            writer.Write((int)outsideType);
            writer.Write((int)(bgcolor[0] * 255f));
            writer.Write((int)(bgcolor[1] * 255f));
            writer.Write((int)(bgcolor[2] * 255f));
            writer.Write((int)(bgcolor[3] * 255f));

            // マップ設定を書き出し
            writer.Write(chipSet.ToByteArray());

            //マップデータ書き出し
            writer.Write(heightMap.GetLength(0));
            writer.Write(heightMap.GetLength(1));
            for (int z = 0; z < heightMap.GetLength(1); z++)
            {
                for (int x = 0; x < heightMap.GetLength(0); x++)
                {
                    writer.Write((short)heightMap[x, z]);
                }
            }

            var usedTerrainIndexMap = new bool[short.MaxValue];
            var usedStairIndexMap = new bool[short.MaxValue];
            for (int z = 0; z < heightMap.GetLength(1); z++)
            {
                for (int x = 0; x < heightMap.GetLength(0); x++)
                {
                    writer.Write((short)attribMap[x, z]);
                    usedTerrainIndexMap[attribMap[x, z]] = true;
                }
            }

            //マップオブジェクト書き出し
            writer.Write(mapobjects.Count);
            foreach (var m in mapobjects)
            {
                writer.Write(m.x);
                writer.Write(m.y);
                writer.Write(m.r);
                writer.Write(m.guId.ToByteArray());
            }

            writer.Write(stairs.Count);
            foreach (var s in stairs)
            {
                writer.Write(s.x);
                writer.Write(s.y);
                writer.Write(s.gfxId);
                writer.Write(s.stat);
                usedStairIndexMap[s.gfxId] = true;
            }

            writer.Write((int)dirLightColor.A);
            writer.Write((int)dirLightColor.R);
            writer.Write((int)dirLightColor.G);
            writer.Write((int)dirLightColor.B);

            writer.Write((int)ambLightColor.A);
            writer.Write((int)ambLightColor.R);
            writer.Write((int)ambLightColor.G);
            writer.Write((int)ambLightColor.B);

            // BGMや戦闘の設定を書き出し
            writer.Write(mapBgm.ToByteArray());
            writer.Write(mapBgs.ToByteArray());
            writer.Write(fixCamera);

            // マップチップ状況を書き出し
            var list = chipList.Where(x => usedTerrainIndexMap[x.Key] || bgChip == x.Value).ToArray();
            writer.Write(list.Length);
            foreach (var chip in list)
            {
                writer.Write(chip.Key);
                writer.Write(chip.Value.ToByteArray());
            }
            list = stairList.Where(x => usedStairIndexMap[x.Key]).ToArray();
            writer.Write(list.Length);
            foreach (var chip in list)
            {
                writer.Write(chip.Key);
                writer.Write(chip.Value.ToByteArray());
            }

            writer.Write(lightOnForBuildings);
            writer.Write(bgChip.ToByteArray());
            writer.Write((int)envEffect);
            writer.Write(0);    // skyTypeがあった場所

            writer.Write(shadowRotateX);
            writer.Write(shadowRotateY);

            writer.Write((int)(fogColor[0] * 255f));
            writer.Write((int)(fogColor[1] * 255f));
            writer.Write((int)(fogColor[2] * 255f));
            writer.Write((int)(fogColor[3] * 255f));

            writer.Write(fixCameraMode);
            writer.Write((int)cameraMode);

            // カメラ設定があるかどうか
            var cameraSettingsAvailable = cameraMode == CameraControlMode.NORMAL ? tpCamera != null : fpCamera != null;
            writer.Write(cameraSettingsAvailable);
            if (cameraSettingsAvailable)
                writeChunk(writer, cameraMode == CameraControlMode.NORMAL ? (RomItem)tpCamera : (RomItem)fpCamera);

            // マップオブジェクトグループの書き出し
            writer.Write(groups.Count);
            ObjectGroup.currentMap = this;
            foreach (var group in groups)
            {
                writeChunk(writer, group);
            }
            ObjectGroup.currentMap = null;

            // 遠景設定の書き出し
            writer.Write((int)bgType);
            writer.Write(skyModel.ToByteArray());
            writer.Write(skyPos[0]);
            writer.Write(skyPos[1]);
            writer.Write(skyPos[2]);
            writer.Write(skyFollowPlayer);

            writer.Write(mapSceneName);
            writer.Write(mapSceneNameModified);
        }

        public override void load(System.IO.BinaryReader reader)
        {
            base.load(reader);

            int version = reader.ReadInt32();

            if (version >= 6)
            {
                category = reader.ReadString();
            }

            if (version >= 4)
            {
                modifiedTime = new DateTime(reader.ReadInt64());
                loadedTime = modifiedTime;
            }

            // イベントを読み出し
            int count = reader.ReadInt32();
            events.Clear();
            for (int i = 0; i < count; i++)
            {
                var ev = new EventRef();
                ev.x = reader.ReadInt32();
                ev.y = reader.ReadInt32();
                ev.guId = Util.readGuid(reader);
                if (events.Find(x => x.guId == ev.guId) == null)
                    events.Add(ev);
            }

            // エンカウント設定を読みだし
            if (version < 7)
            {
                var encountSteps = reader.ReadInt32();

                mapBattleSetting.encountMinSteps = encountSteps * defaultEncountMinStepPercent / 100;
                mapBattleSetting.encountMaxSteps = encountSteps * defaultEncountMaxStepPercent / 100;

                count = reader.ReadInt32();

                if (count > 64) // 暫定
                    return;

                for (int i = 0; i < count; i++)
                {
                    var enc = new Encount();
                    enc.weight = reader.ReadInt32();

                    reader.ReadInt32();

                    enc.enemy = Util.readGuid(reader);

                    mapBattleSetting.encounts.Add(enc);
                }
            }
            else
            {
                if (version < 8)
                    mapBattleSetting.Load(reader);  // あとで消したい
                else
                    readChunk(reader, mapBattleSetting);

                count = reader.ReadInt32();

                for (int i = 0; i < count; i++)
                {
                    var battleSettings = new AreaBattleSetting();

                    if (version < 8)
                        battleSettings.Load(reader);  // あとで消したい
                    else
                        readChunk(reader, battleSettings);

                    areaBattleSettings.Add(battleSettings);
                }
            }

            outsideType = (OutsideType)reader.ReadInt32();
            bgcolor[0] = reader.ReadInt32() / 255f;
            bgcolor[1] = reader.ReadInt32() / 255f;
            bgcolor[2] = reader.ReadInt32() / 255f;
            bgcolor[3] = reader.ReadInt32() / 255f;

            // マップ設定を読み出し
            chipSet = Util.readGuid(reader);

            //マップデータ読み込み

            int w = reader.ReadInt32();
            int h = reader.ReadInt32();

            heightMap = new int[w, h];
            attribMap = new int[w, h];

            if (version < 5)
            {
                for (int z = 0; z < heightMap.GetLength(1); z++)
                {
                    for (int x = 0; x < heightMap.GetLength(0); x++)
                    {
                        heightMap[x, z] = (int)reader.ReadByte();
                    }
                }

                for (int z = 0; z < heightMap.GetLength(1); z++)
                {
                    for (int x = 0; x < heightMap.GetLength(0); x++)
                    {
                        attribMap[x, z] = (int)reader.ReadByte();
                    }
                }
            }
            else
            {
                for (int z = 0; z < heightMap.GetLength(1); z++)
                {
                    for (int x = 0; x < heightMap.GetLength(0); x++)
                    {
                        heightMap[x, z] = (int)reader.ReadInt16();
                    }
                }

                for (int z = 0; z < heightMap.GetLength(1); z++)
                {
                    for (int x = 0; x < heightMap.GetLength(0); x++)
                    {
                        attribMap[x, z] = (int)reader.ReadInt16();
                    }
                }
            }

            //マップオブジェクト読み込み
            int mocount = reader.ReadInt32();
            for (int i = 0; i < mocount; i++)
            {
                MapObjectRef mor = new MapObjectRef();
                mor.x = reader.ReadInt32();
                mor.y = reader.ReadInt32();
                if (version >= 2) mor.r = reader.ReadInt32();
                else mor.r = 0;
                mor.guId = Util.readGuid(reader);
                mapobjects.Add(mor);
            }

            int scount = reader.ReadInt32();
            for (int i = 0; i < scount; i++)
            {
                StairRef sr = new StairRef();
                sr.x = reader.ReadInt32();
                sr.y = reader.ReadInt32();
                sr.gfxId = reader.ReadInt32();
                if (version >= 3) sr.stat = reader.ReadInt32();
                else sr.stat = ILLEGAL_STAIR_STAT;    // 自動計算用に異常値を入れておく ( MapDataクラスの STAIR_STAT に無い値 )
                stairs.Add(sr);
            }

            try
            {
                reader.ReadInt32(); // Alpha
                dirLightColor = new Color(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), 255);
                reader.ReadInt32(); // Alpha
                ambLightColor = new Color(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), 255);
            }
            catch (System.IO.EndOfStreamException)
            {
                // スルーしても問題なし
            }

            // BGMや戦闘の設定を書き出し
            if (version < 7)
            {
                mapBattleSetting.battleBg = Util.readGuid(reader);
            }

            mapBgm = Util.readGuid(reader);
            mapBgs = Util.readGuid(reader);

            if (version < 7)
            {
                mapBattleSetting.battleBgm = Util.readGuid(reader);
                mapBattleSetting.battleBgs = Util.readGuid(reader);
                mapBattleSetting.maxMonsters = reader.ReadInt32();
            }

            fixCamera = reader.ReadBoolean();

            if (version < 7)
            {
                mapBattleSetting.useDefaultBattleBgm = reader.ReadBoolean();
            }

            // チップ情報を読み込み
            var ccount = reader.ReadInt32();
            for (int i = 0; i < ccount; i++)
            {
                int index = reader.ReadInt32();
                var guid = Util.readGuid(reader);
                chipList.Add(index, guid);

                // 逆引き用辞書にも登録する
                if (!chipListRev.ContainsKey(guid))
                    chipListRev.Add(guid, index);
            }
            ccount = reader.ReadInt32();
            for (int i = 0; i < ccount; i++)
            {
                int index = reader.ReadInt32();
                var guid = Util.readGuid(reader);
                stairList.Add(index, guid);

                // 逆引き用辞書にも登録する
                if (!stairListRev.ContainsKey(guid))
                    stairListRev.Add(guid, index);
            }

            lightOnForBuildings = reader.ReadBoolean();
            bgChip = Util.readGuid(reader);
            envEffect = (EnvironmentEffectType)reader.ReadInt32();
            skyModel = MAPBG_DICT_ID_TO_GUID[reader.ReadInt32()];
            bgType = BgType.COLOR;
            if (skyModel != Guid.Empty)
                bgType = BgType.MODEL;

            shadowRotateX = reader.ReadSingle();
            shadowRotateY = reader.ReadSingle();

            fogColor[0] = reader.ReadInt32() / 255f;
            fogColor[1] = reader.ReadInt32() / 255f;
            fogColor[2] = reader.ReadInt32() / 255f;
            fogColor[3] = reader.ReadInt32() / 255f;

            fixCameraMode = reader.ReadBoolean();
            cameraMode = (CameraControlMode)reader.ReadInt32();
            origCameraMode = cameraMode;

            var cameraSettingsAvailable = reader.ReadBoolean();
            if (cameraSettingsAvailable)
            {
                if (cameraMode == CameraControlMode.NORMAL)
                {
                    tpCamera = new ThirdPersonCameraSettings();
                    readChunk(reader, tpCamera);
                }
                else
                {
                    fpCamera = new FirstPersonCameraSettings();
                    readChunk(reader, fpCamera);
                }
            }

            if (Resource.ResourceItem.sCurrentSourceMode == Resource.ResourceSource.RES_SYSTEM)
            {
                setToReadOnly();
            }

            if (version < 7)
            {
                mapBattleSetting.battleBgCenterX = reader.ReadInt32();
                mapBattleSetting.battleBgCenterY = reader.ReadInt32();
            }

            // マップオブジェクトグループの読み込み
            int gcount = reader.ReadInt32();
            ObjectGroup.currentMap = this;
            groups.Clear();
            for (int i = 0; i < gcount; i++)
            {
                var rom = new ObjectGroup();
                readChunk(reader, rom);
                groups.Add(rom);
            }
            ObjectGroup.currentMap = null;

            // 遠景設定の読み込み
            bgType = (BgType)reader.ReadInt32();
            skyModel = Util.readGuid(reader);
            skyPos[0] = reader.ReadSingle();
            skyPos[1] = reader.ReadSingle();
            skyPos[2] = reader.ReadSingle();
            skyFollowPlayer = reader.ReadBoolean();

            mapSceneName = reader.ReadString();
            mapSceneNameModified = reader.ReadBoolean();
        }

        private void setToReadOnly()
        {
            category = READ_ONLY_CATEGORY;
        }

        public bool isReadOnly()
        {
            return category == READ_ONLY_CATEGORY;
        }

        internal override bool isOptionMatched(string option)
        {
            return category == option;
        }

        public Dictionary<int, Guid> getChipList()
        {
            if (chipList.Count == 0)
            {
                var catalog = Catalog.sInstance;
                var rom = catalog.getItemFromGuid(chipSet);
                var oldRom = rom as Common.Resource.MapChipOld;
                if (oldRom != null)
                {
                    var oldInfoList = oldRom.getChipPathList();
                    foreach (var oldInfo in oldInfoList)
                    {
                        var newRom = catalog.getItemFromPath(oldInfo.path) as Common.Resource.MapChip;
                        // TODO 波に対応
                        if (newRom.wave)
                            continue;
                        chipList.Add(oldInfo.index, newRom.guId);

                        // 逆引き用辞書にも登録する
                        if (!chipListRev.ContainsKey(newRom.guId))
                            chipListRev.Add(newRom.guId, oldInfo.index);
                    }
                }
            }

            return chipList;
        }

        public Dictionary<int, Guid> getStairList()
        {
            if (stairList.Count == 0)
            {
                var catalog = Catalog.sInstance;
                var oldRom = catalog.getItemFromGuid(chipSet) as Common.Resource.MapChipOld;
                if (oldRom != null)
                {
                    var oldInfoList = oldRom.getStairPathList();
                    foreach (var oldInfo in oldInfoList)
                    {
                        var newRom = catalog.getItemFromPath(oldInfo.path) as Common.Resource.MapChip;
                        if (newRom != null && !stairList.ContainsKey(oldInfo.index))
                            stairList.Add(oldInfo.index, newRom.guId);

                        // 逆引き用辞書にも登録する
                        if (!stairListRev.ContainsKey(newRom.guId))
                            stairListRev.Add(newRom.guId, oldInfo.index);
                    }
                }
            }

            return stairList;
        }


        public Dictionary<Guid, int> getChipListRev()
        {
            if (chipList.Count == 0)
            {
                getChipList();
            }

            return chipListRev;
        }

        public Dictionary<Guid, int> getStairListRev()
        {
            if (stairList.Count == 0)
            {
                getStairList();
            }

            return stairListRev;
        }

        public List<EventRef> getEvents()
        {
            return events;
        }

        public int getMapObjectNum()
        {
            return mapobjects.Count;
        }

        public Guid getBattleBg(Catalog catalog, BattleSetting inBattleSetting)
        {
            inBattleSetting.battleBg = doConvertBattleBg(catalog, inBattleSetting.battleBg);
            return inBattleSetting.battleBg;
        }
    }
}

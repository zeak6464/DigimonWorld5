using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

using Yukar.Common.Rom;
using System.Xml;
using System.Threading;

namespace Yukar.Common
{
    public class Catalog
    {
        List<RomItem> itemList;
        Dictionary<Guid, Resource.ResourceItem> missingItemDic = new Dictionary<Guid, Resource.ResourceItem>();

        internal static void SetCurrentDirectory(string v)
        {
            throw new NotImplementedException();
        }

        Dictionary<Guid, RomItem> itemDic;

        static Dictionary<short, Type> romSignatureToTypeDic;
        static Dictionary<Type, short> romTypeToSignatureDic;

        public const short ROM_VERSION = 9;
        private const string ROM_HEADER = "YUKAR";
        public const string ROM_EXTENSION = ".sgr";
        public const string BACKUPDIR_PREFIX = "sgbs_backup_";

        public enum OVERWRITE_RULES
        {
            NEVER,
            ALWAYS,
            EQUAL_TO_IGNOREITEM,
        }

        // 普通のROM
        public const short SIGNATURE_ROOT = 0x0001;
        public const short SIGNATURE_SETTINGS = 0x0100;
        public const short SIGNATURE_HERO = 0x0200;
        public const short SIGNATURE_MAP = 0x0300;
        public const short SIGNATURE_FIELD = 0x0310;
        public const short SIGNATURE_MAPOBJECT_REF = 0x0320;
        public const short SIGNATURE_STAIR_REF = 0x0330;
        public const short SIGNATURE_ITEM = 0x0400;
        public const short SIGNATURE_SKILL = 0x0500;
        public const short SIGNATURE_BATTLE_COMMAND = 0x0600;
        public const short SIGNATURE_EVENT = 0x0700;
        public const short SIGNATURE_SCRIPT = 0x0710;
        public const short SIGNATURE_MONSTER = 0x0800;
        public const short SIGNATURE_EFFECT = 0x0900;

        // リソース系のROM
        public const short SIGNATURE_RESOURCE = 0x1100; // タイプ未設定のリソース
        public const short SIGNATURE_BGM = 0x1101;
        public const short SIGNATURE_SE = 0x1102;
        public const short SIGNATURE_MAPCHIP_OLD = 0x1103;
        public const short SIGNATURE_CHARACTER = 0x1104;
        public const short SIGNATURE_ICON = 0x1105;

        public const short SIGNATURE_MONSTER_IMG = 0x1106;
        public const short SIGNATURE_SPRITE = 0x1107;
        public const short SIGNATURE_WINDOW = 0x1108;
        public const short SIGNATURE_EFFECT_SOURCE = 0x1109;
        public const short SIGNATURE_SYSTEM_IMG = 0x1110;
		public const short SIGNATURE_BUILDING = 0x1111;
        public const short SIGNATURE_MAPITEM = 0x1112;
        public const short SIGNATURE_FACE = 0x1113;
        public const short SIGNATURE_BGS = 0x1114;
        public const short SIGNATURE_BATTLE_BG = 0x1115;
        public const short SIGNATURE_MAPCHIP = 0x1116;
        public const short SIGNATURE_ITEM_MODEL = 0x1117;
        public const short SIGNATURE_MAP_BG = 0x1118;
        public const short SIGNATURE_IGNORE = 0x1200;

        public const int RESULT_ERROR = -1;
        public const int RESULT_IO_ERROR = 1;
        public const int RESULT_NOERROR = 0;
        public const int GUID_SIZE = 16;
        public const int GUID_STR_SIZE = 36;

        private GameSettings gameSettings;

		public static Catalog sInstance = null;
        internal static int sRomVersion;    // Catalogのload中にRomItemから参照する用
        public int romVersion = ROM_VERSION;
        public int lastUsedSwitch;
        public static string sResourceDir = ".\\";
        public static string sCommonResourceDir;
        public static Func<int, int, string, bool> sProgressListener;
        public static bool sEngineMode;   // ロムへの強制的な変更の影響をひとまずエンジンでのみ見たい等の場合の緊急用。なるべく使わない。

        private int currentProgress = 0;
        private int maxProgress = 0;
        public static string sDlcDir;
        public struct DlcInfo
        {
            public Guid guid;
            public string path;
            public string name;
            public bool allowExport;
            public List<Guid> disallowExportGuidList;
        }
        public static Dictionary<Guid, DlcInfo> sDlcDictionary = new Dictionary<Guid, DlcInfo>();

        public Catalog()
        {
            itemList = new List<RomItem>();
            itemDic = new Dictionary<Guid, RomItem>();

            romSignatureToTypeDic = new Dictionary<short, Type>();
            romTypeToSignatureDic = new Dictionary<Type, short>();

            // 各タイプにシグネチャをセットする
            romSignatureToTypeDic.Add(SIGNATURE_SETTINGS, typeof(GameSettings));
            romTypeToSignatureDic.Add(typeof(GameSettings), SIGNATURE_SETTINGS);
            romSignatureToTypeDic.Add(SIGNATURE_HERO, typeof(Hero));
            romTypeToSignatureDic.Add(typeof(Hero), SIGNATURE_HERO);
            romSignatureToTypeDic.Add(SIGNATURE_MAP, typeof(Map));
            romTypeToSignatureDic.Add(typeof(Map), SIGNATURE_MAP);
            romSignatureToTypeDic.Add(SIGNATURE_FIELD, typeof(Field));
            romTypeToSignatureDic.Add(typeof(Field), SIGNATURE_FIELD);
            romSignatureToTypeDic.Add(SIGNATURE_MAPOBJECT_REF, typeof(Map.MapObjectRef));
            romTypeToSignatureDic.Add(typeof(Map.MapObjectRef), SIGNATURE_MAPOBJECT_REF);
            romSignatureToTypeDic.Add(SIGNATURE_STAIR_REF, typeof(Map.StairRef));
            romTypeToSignatureDic.Add(typeof(Map.StairRef), SIGNATURE_STAIR_REF);
            romSignatureToTypeDic.Add(SIGNATURE_ITEM, typeof(Item));
            romTypeToSignatureDic.Add(typeof(Item), SIGNATURE_ITEM);
            romSignatureToTypeDic.Add(SIGNATURE_SKILL, typeof(Skill));
            romTypeToSignatureDic.Add(typeof(Skill), SIGNATURE_SKILL);
            romSignatureToTypeDic.Add(SIGNATURE_BATTLE_COMMAND, typeof(BattleCommand));
            romTypeToSignatureDic.Add(typeof(BattleCommand), SIGNATURE_BATTLE_COMMAND);
            romSignatureToTypeDic.Add(SIGNATURE_MONSTER, typeof(Monster));
            romTypeToSignatureDic.Add(typeof(Monster), SIGNATURE_MONSTER);
            romSignatureToTypeDic.Add(SIGNATURE_EFFECT, typeof(Effect));
            romTypeToSignatureDic.Add(typeof(Effect), SIGNATURE_EFFECT);

            romSignatureToTypeDic.Add(SIGNATURE_EVENT, typeof(Event));
            romTypeToSignatureDic.Add(typeof(Event), SIGNATURE_EVENT);
            romSignatureToTypeDic.Add(SIGNATURE_SCRIPT, typeof(Script));
            romTypeToSignatureDic.Add(typeof(Script), SIGNATURE_SCRIPT);

            romSignatureToTypeDic.Add(SIGNATURE_RESOURCE, typeof(Resource.ResourceItem));
            romTypeToSignatureDic.Add(typeof(Resource.ResourceItem), SIGNATURE_RESOURCE);
            romSignatureToTypeDic.Add(SIGNATURE_BGM, typeof(Resource.Bgm));
            romTypeToSignatureDic.Add(typeof(Resource.Bgm), SIGNATURE_BGM);
            romSignatureToTypeDic.Add(SIGNATURE_SE, typeof(Resource.Se));
            romTypeToSignatureDic.Add(typeof(Resource.Se), SIGNATURE_SE);
            romSignatureToTypeDic.Add(SIGNATURE_MAPCHIP, typeof(Resource.MapChip));
            romTypeToSignatureDic.Add(typeof(Resource.MapChip), SIGNATURE_MAPCHIP);
            romSignatureToTypeDic.Add(SIGNATURE_CHARACTER, typeof(Resource.Character));
            romTypeToSignatureDic.Add(typeof(Resource.Character), SIGNATURE_CHARACTER);
            romSignatureToTypeDic.Add(SIGNATURE_ICON, typeof(Resource.Icon));
            romTypeToSignatureDic.Add(typeof(Resource.Icon), SIGNATURE_ICON);
            romSignatureToTypeDic.Add(SIGNATURE_MONSTER_IMG, typeof(Resource.Monster));
            romTypeToSignatureDic.Add(typeof(Resource.Monster), SIGNATURE_MONSTER_IMG);
            romSignatureToTypeDic.Add(SIGNATURE_SPRITE, typeof(Resource.Sprite));
            romTypeToSignatureDic.Add(typeof(Resource.Sprite), SIGNATURE_SPRITE);
            romSignatureToTypeDic.Add(SIGNATURE_WINDOW, typeof(Resource.Window));
            romTypeToSignatureDic.Add(typeof(Resource.Window), SIGNATURE_WINDOW);
            romSignatureToTypeDic.Add(SIGNATURE_EFFECT_SOURCE, typeof(Resource.EffectSource));
            romTypeToSignatureDic.Add(typeof(Resource.EffectSource), SIGNATURE_EFFECT_SOURCE);
			romSignatureToTypeDic.Add(SIGNATURE_BUILDING, typeof(Resource.MapObject));
			romTypeToSignatureDic.Add(typeof(Resource.MapObject), SIGNATURE_BUILDING);
            romSignatureToTypeDic.Add(SIGNATURE_FACE, typeof(Resource.Face));
            romTypeToSignatureDic.Add(typeof(Resource.Face), SIGNATURE_FACE);
            romSignatureToTypeDic.Add(SIGNATURE_BGS, typeof(Resource.Bgs));
            romTypeToSignatureDic.Add(typeof(Resource.Bgs), SIGNATURE_BGS);
            romSignatureToTypeDic.Add(SIGNATURE_BATTLE_BG, typeof(Resource.BattleBackground));
            romTypeToSignatureDic.Add(typeof(Resource.BattleBackground), SIGNATURE_BATTLE_BG);
            romSignatureToTypeDic.Add(SIGNATURE_IGNORE, typeof(Rom.IgnoreItem));
            romTypeToSignatureDic.Add(typeof(Rom.IgnoreItem), SIGNATURE_IGNORE);
            romSignatureToTypeDic.Add(SIGNATURE_MAPCHIP_OLD, typeof(Resource.MapChipOld));
            romTypeToSignatureDic.Add(typeof(Resource.MapChipOld), SIGNATURE_MAPCHIP_OLD);
            romSignatureToTypeDic.Add(SIGNATURE_ITEM_MODEL, typeof(Resource.Item));
            romTypeToSignatureDic.Add(typeof(Resource.Item), SIGNATURE_ITEM_MODEL);
            romSignatureToTypeDic.Add(SIGNATURE_MAP_BG, typeof(Resource.MapBackground));
            romTypeToSignatureDic.Add(typeof(Resource.MapBackground), SIGNATURE_MAP_BG);
        }

        public void addItem(RomItem item, OVERWRITE_RULES overwrite = OVERWRITE_RULES.NEVER)
        {
            if (item.guId == Guid.Empty)
                return;

            // 同じキーがあったら追加しない
            if (itemDic.ContainsKey(item.guId))
            {
                if (overwrite == OVERWRITE_RULES.NEVER)
                    return;
                
                var old = itemDic[item.guId];
                if (old is Common.Rom.IgnoreItem || overwrite == OVERWRITE_RULES.ALWAYS)
                {
                    itemDic.Remove(item.guId);
                    itemList.Remove(old);

                    if (item is Common.Rom.GameSettings)
                        gameSettings = item as Common.Rom.GameSettings;
                }
                else
                {
                    return;
                }
            }

            itemList.Add(item);
            itemDic.Add(item.guId, item);
        }

        public Resource.ErrorType addNewItem(Common.Resource.ResourceItem item, bool overwrite = false)
        {
            if (item.guId == Guid.Empty)
                return Resource.ErrorType.NONE;

            // 同じキーがあったら追加しない
            if (itemDic.ContainsKey(item.guId))
            {
                if (overwrite)
                {
                    var old = itemDic[item.guId];
                    itemDic.Remove(item.guId);
                    itemList.Remove(old);
                }
                else
                {
                    return Resource.ErrorType.NONE;
                }
            }

            // エラーだったら追加しない
            var result = item.verify();
            if (result != Resource.ErrorType.NONE)
                return result;

            itemList.Add(item);
            itemDic.Add(item.guId, item);

            return Resource.ErrorType.NONE;
        }

        public void deleteItem(RomItem item)
        {
            deleteItem(item.guId);
            /*
            itemList.Remove(item);
            itemDic.Remove(item.guId);
            */
        }

        public void deleteItem(Guid guid)
        {
            if (!itemDic.ContainsKey(guid))
                return;

            var item = itemDic[guid];
            itemList.Remove(item);
            itemDic.Remove(guid);
        }

        public RomItem getItemFromGuid(Guid guId)
        {
            if (!itemDic.ContainsKey(guId) || itemDic[guId] is Rom.IgnoreItem)
                return null;

            return itemDic[guId];
        }

        public RomItem getItemFromGuid(Guid guId, bool includeIgnoreItem)
        {
            if (!itemDic.ContainsKey(guId) || (!includeIgnoreItem && itemDic[guId] is Rom.IgnoreItem))
                return null;

            return itemDic[guId];
        }

        public T getItemFromGuid<T>(Guid guId) where T : RomItem
        {
            if (!itemDic.ContainsKey(guId) || itemDic[guId] is Rom.IgnoreItem)
                return null;

            return itemDic[guId] as T;
        }

        public RomItem getItemFromName(string name, Type type)
        {
            return itemList.Find(
                delegate(RomItem item)
                {
                    return (item.GetType().IsSubclassOf(type) | item.GetType() == type) & item.name == name;
                }
            );
        }

        public Common.Resource.ResourceItem getItemFromPath(string path)
        {
            path = Path.GetFullPath(path).ToUpper();
            return (Common.Resource.ResourceItem)itemList.FirstOrDefault(
                rom =>
                {
                    var res = rom as Common.Resource.ResourceItem;
                    if (res == null)
                        return false;

                    foreach (var resPath in res.getPathList())
                    {
                        if (path == Path.GetFullPath(resPath).ToUpper()) return true;
                    }

                    return false;
                }
            );
        }

        public List<RomItem> getFilteredItemList(Type type, bool excludeReadOnlyMap = true)
        {
            if (type == typeof(Common.Rom.Map) && excludeReadOnlyMap)
            {
                return itemList.FindAll(
                    delegate(RomItem item)
                    {
                        var map = item as Common.Rom.Map;
                        return map != null && !map.isReadOnly();
                    }
                );
            }
            else
            {
                return itemList.FindAll(
                    delegate(RomItem item)
                    {
                        return item.GetType().IsSubclassOf(type) | item.GetType() == type;
                    }
                );
            }
        }

        public List<RomItem> getFilteredItemList(Type type, string option)
        {
            return itemList.FindAll(
                delegate(RomItem item)
                {
                    var typeMatched = item.GetType().IsSubclassOf(type) | item.GetType() == type;
                    var optionMatched = item.isOptionMatched(option);
                    return typeMatched && optionMatched;
                }
            );
        }

        public RomItem getItemFromIndex(int idx)
        {
            return itemList.ElementAt(idx);
        }

        public int save(bool includeReadonlyMap = false, bool createBackup = true)
        {
            // 既存のロムを削除
            try
            {
                clearOldRomFiles(createBackup);
            }
            catch (IOException)
            {
                return RESULT_IO_ERROR;
            }
            catch (Exception)
            {
                return RESULT_ERROR;
            }
            
            int onError = 0;

            // 各ロムを保存
            var types = new Type[] {
                typeof(Common.Rom.GameSettings), typeof(Common.Rom.Hero), typeof(Common.Rom.Item),
                typeof(Common.Rom.Skill), typeof(Common.Rom.Monster), typeof(Common.Rom.Effect),
                typeof(Common.Resource.ResourceItem),
            };
            foreach (var type in types)
            {
                try
                {
                    using (var stream = new FileStream(type.Name + ROM_EXTENSION, FileMode.Create))
                    {
                        var romList = getFilteredItemList(type);

                        // 主人公はバトルコマンドもセットで保存する
                        if (type == typeof(Common.Rom.Hero))
                        {
                            romList.AddRange(getFilteredItemList(typeof(Common.Rom.BattleCommand)));
                        }
                        // ゲーム設定はコモンイベントもセットで保存する
                        else if (type == typeof(Common.Rom.GameSettings))
                        {
                            var events = new List<Guid>();

                            events.AddRange(gameSettings.commonEvents);
                            events.AddRange(gameSettings.battleEvents);

                            foreach (var evRef in events)
                            {
                                romList.AddRange(getEventRomList(evRef));
                            }
                        }
                        // リソースはシステムリソースを除外する
                        else if (type == typeof(Common.Resource.ResourceItem))
                        {
                            romList = romList.FindAll(
                                (Common.Rom.RomItem rom) =>
                                {
                                    return ((Common.Resource.ResourceItem)rom).source != Resource.ResourceSource.RES_SYSTEM;
                                }
                            );

                            // 除外したシステムリソースを保存する
                            romList.AddRange(getFilteredItemList(typeof(Common.Rom.IgnoreItem)));
                        }

                        save(romList, stream, type == typeof(Common.Resource.ResourceItem));
                    }
                }
                catch (IOException)
                {
                    onError = RESULT_IO_ERROR;
                }
                catch (Exception)
                {
                    onError = RESULT_ERROR;
                }
            }

            // マップを保存
            var maps = getFilteredItemList(typeof(Common.Rom.Map));
            if (includeReadonlyMap)
                maps.AddRange(getFilteredItemList(typeof(Common.Rom.Map),
                    Common.Rom.Map.READ_ONLY_CATEGORY));
            int count = 0;
            foreach (Common.Rom.Map map in maps)
            {
                try
                {
                    map.loadedTime = map.modifiedTime;

                    var romList = new List<Common.Rom.RomItem>();
                    romList.Add(map);

                    // イベントもセットで追加する
                    foreach (var evRef in map.getEvents())
                    {
                        var ev = getItemFromGuid(evRef.guId) as Common.Rom.Event;
                        if (ev == null)
                            continue;

                        romList.Add(ev);

                        // スクリプトもセットで追加する
                        foreach (var sheet in ev.sheetList)
                        {
                            var sc = getItemFromGuid(sheet.script);
                            if (sc != null)
                                romList.Add(sc);
                        }
                    }

                    // マップには連番を付ける
                    Directory.CreateDirectory(".\\map\\");
                    using (var stream = new FileStream(".\\map\\" + map.GetType().Name + "_" + count.ToString("D3") + ROM_EXTENSION, FileMode.Create))
                    {
                        save(romList, stream);
                    }
                }
                catch (IOException)
                {
                    onError = RESULT_IO_ERROR;
                }
                catch (Exception)
                {
                    onError = RESULT_ERROR;
                }

                count++;
            }

            // 正常終了した場合はバックアップを削除
            if (onError == RESULT_NOERROR)
                clearBackup();

            return onError;
        }

        public List<RomItem> getEventRomList(Guid evRef)
        {
            var result = new List<RomItem>();

            var ev = getItemFromGuid(evRef) as Common.Rom.Event;
            if (ev == null)
                return result;

            result.Add(ev);

            // スクリプトもセットで追加する
            foreach (var sheet in ev.sheetList)
            {
                var sc = getItemFromGuid(sheet.script);
                if (sc != null)
                    result.Add(sc);
            }

            return result;
        }

        public bool checkDiffForSavedFiles()
        {
            bool diff = false;

            // 各ロムを保存
            var types = new Type[] {
                typeof(Common.Rom.GameSettings), typeof(Common.Rom.Hero), typeof(Common.Rom.Item),
                typeof(Common.Rom.Skill), typeof(Common.Rom.Monster), typeof(Common.Rom.Effect),
                typeof(Common.Resource.ResourceItem),
            };
            foreach (var type in types)
            {
                var path = type.Name + ROM_EXTENSION;

                // 保存先がまだなかったら差異ありとする
                if (!Util.file.exists(path))
                    return true;

                // 前回セーブしたデータと比較する
                var prev = Common.Util.getFileStream(path);

                var now = new MemoryStream();
                var romList = getFilteredItemList(type);

                // 主人公はバトルコマンドもセットで保存する
                if (type == typeof(Common.Rom.Hero))
                {
                    romList.AddRange(getFilteredItemList(typeof(Common.Rom.BattleCommand)));
                }
                // ゲーム設定はコモンイベントもセットで保存する
                else if (type == typeof(Common.Rom.GameSettings))
                {
                    var events = new List<Guid>();

                    events.AddRange(gameSettings.commonEvents);
                    events.AddRange(gameSettings.battleEvents);

                    foreach (var evRef in events)
                    {
                        var ev = getItemFromGuid(evRef) as Common.Rom.Event;
                        if (ev == null)
                            continue;

                        romList.Add(ev);

                        // スクリプトもセットで追加する
                        foreach (var sheet in ev.sheetList)
                        {
                            var sc = getItemFromGuid(sheet.script);
                            if (sc != null)
                                romList.Add(sc);
                        }
                    }
                }
                // リソースはシステムリソースを除外する
                else if (type == typeof(Common.Resource.ResourceItem))
                {
                    romList = romList.FindAll(
                        (Common.Rom.RomItem rom) =>
                        {
                            return ((Common.Resource.ResourceItem)rom).source != Resource.ResourceSource.RES_SYSTEM;
                        }
                    );

                    // 除外したシステムリソースを保存する
                    romList.AddRange(getFilteredItemList(typeof(Common.Rom.IgnoreItem)));
                }
                save(romList, now, type == typeof(Common.Resource.ResourceItem));

                diff = diff | Util.compareBinary(now, prev);

                prev.Close();
                now.Close();

                if (diff)
                    return diff;
            }

            // マップを保存
            var maps = getFilteredItemList(typeof(Common.Rom.Map));
            int count = 0;
            foreach (Common.Rom.Map map in maps)
            {
                var modifiedTime = map.modifiedTime;
                map.modifiedTime = map.loadedTime;

                var romList = new List<Common.Rom.RomItem>();
                romList.Add(map);

                // イベントもセットで追加する
                foreach (var evRef in map.getEvents())
                {
                    var ev = getItemFromGuid(evRef.guId) as Common.Rom.Event;
                    if (ev == null)
                        continue;

                    romList.Add(ev);

                    // スクリプトもセットで追加する
                    foreach (var sheet in ev.sheetList)
                    {
                        var sc = getItemFromGuid(sheet.script);
                        if (sc != null)
                            romList.Add(sc);
                    }
                }

                // マップには連番を付ける
                string path = ".\\map\\" + map.GetType().Name + "_" + count.ToString("D3") + ROM_EXTENSION;
                if (Util.file.exists(path))
                {
                    var prev = Common.Util.getFileStream(path);
                    var now = new MemoryStream();
                    save(romList, now);

                    diff = diff | Util.compareBinary(now, prev);

                    prev.Close();
                    now.Close();
                }
                else
                {
                    // ファイルがなければ差分がある事確定
                    diff = true;
                }

                map.modifiedTime = modifiedTime;

                if (diff)
                    return diff;

                count++;
            }

            return diff;
        }

        public static void resetCurrentRomVersion()
        {
            sRomVersion = ROM_VERSION;
        }

        private void clearOldRomFiles(bool createBackup)
        {
            // バックアップフォルダを作成する
            var backupDir = BACKUPDIR_PREFIX + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            if (createBackup && !Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);

            // フォルダ内の全てのロムを消す
            var files = Util.file.getFiles(".\\", "*" + ROM_EXTENSION).ToList();
            if (Util.file.dirExists(".\\map"))
                files.AddRange(Util.file.getFiles(".\\map\\", "*" + ROM_EXTENSION));

            // まずバックアップする
            if (createBackup)
            {
                foreach (var path in files)
                {
                    var dest = backupDir + Path.DirectorySeparatorChar + Path.GetFileName(path);
                    if (!File.Exists(dest))
                        File.Copy(path, dest);
                }
            }

            // 読み取り専用でないかどうか調べる
            foreach (var path in files)
            {
                var attr = File.GetAttributes(path);
                if (attr.HasFlag(FileAttributes.ReadOnly))
                    throw new UnauthorizedAccessException();
            }

            // 削除する
            foreach (var path in files)
            {
                File.Delete(path);
            }
        }
        
        private void clearBackup()
        {
            // バックアップフォルダを削除する
            var dirs = Directory.GetDirectories(".", BACKUPDIR_PREFIX + "*");

            foreach(var dir in dirs)
            {
                // 検出したフォルダ内のファイルがすべてsgrかどうか調べる
                bool isBackupDir = true;
                var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
                foreach(var file in files)
                {
                    if (Path.GetExtension(file).ToUpper() != ".SGR")
                    {
                        isBackupDir = false;
                        break;
                    }
                }

                // 全部sgrだったら消す
                if (isBackupDir)
                {
                    try
                    {
                        DeleteFilesAndFoldersRecursively(dir);
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine(e.Message + " " + e.StackTrace);
                    }
                }
            }
        }

        public static void DeleteFilesAndFoldersRecursively(string target_dir)
        {
            foreach (string file in Directory.GetFiles(target_dir))
            {
                File.Delete(file);
            }

            foreach (string subDir in Directory.GetDirectories(target_dir))
            {
                DeleteFilesAndFoldersRecursively(subDir);
            }

            Thread.Sleep(1); // This makes the difference between whether it works or not. Sleep(0) is not enough.
            Directory.Delete(target_dir);
        }

        public int save(Stream stream)
        {
            return save(itemList, stream);
        }

        public int save(List<RomItem> romList, Stream stream, bool writeMissingRomList = false)
        {
            //bool defaultPath = stream == null;
            var writer = new BinaryWriter(stream, Encoding.UTF8);

            // ファイルヘッダー
            writer.Write(System.Text.Encoding.ASCII.GetBytes(ROM_HEADER));

            // システム設定を出力
            {
                var tmpStream = new MemoryStream();
                var tmpWriter = new BinaryWriter(tmpStream);
                saveSystemContainer(tmpWriter);
                if(writeMissingRomList)
                    saveMissingRomList(tmpWriter);

                writer.Write((int)tmpStream.Length);                            // サイズを書き込む
                writer.Write(SIGNATURE_ROOT);                                   // シグネチャを書き込む
                writer.Write(tmpStream.GetBuffer(), 0, (int)tmpStream.Length);  // バイト列を書き込む

                tmpWriter.Close();
            }

            // カタログの各データを書き込む
            foreach (var item in romList)
            {
                if (item == null)
                {
                    Console.WriteLine("不正なROMデータがあります");
                    continue;
                }

                // 未対応のカテゴリは書き出せない
                if (!romTypeToSignatureDic.ContainsKey(item.GetType()))
                {
                    Console.WriteLine(String.Format("カテゴリ {0} は未対応です。", item.GetType().FullName));
                    continue;
                }

                var tmpStream = new MemoryStream();
                var tmpWriter = new BinaryWriter(tmpStream);
                item.save(tmpWriter);

                writer.Write((int)tmpStream.Length);                            // サイズを書き込む
                writer.Write(romTypeToSignatureDic[item.GetType()]);            // シグネチャを書き込む
                writer.Write(tmpStream.GetBuffer(), 0, (int)tmpStream.Length);  // バイト列を書き込む

                tmpWriter.Close();
            }

            return RESULT_NOERROR;
        }

        private void saveMissingRomList(BinaryWriter tmpWriter)
        {
            tmpWriter.Write(missingItemDic.Count);
            foreach (var item in missingItemDic)
            {
                tmpWriter.Write(item.Key.ToByteArray());
                tmpWriter.Write(item.Value.name);
                tmpWriter.Write(item.Value.path);
            }
        }

        private void loadMissingRomList(BinaryReaderWrapper tmpReader)
        {
            int count = tmpReader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var res = new Resource.ResourceItem();
                res.guId = Util.readGuid(tmpReader);
                res.name = tmpReader.ReadString();
                res.path = tmpReader.ReadString();
            }
        }

        private void saveSystemContainer(BinaryWriter writer)
        {
            writer.Write(ROM_VERSION);
            writer.Write(lastUsedSwitch);
        }

        private void loadSystemContainer(BinaryReader reader)
        {
            romVersion = sRomVersion = reader.ReadInt16();
            lastUsedSwitch = reader.ReadInt32();
        }

        public Resource.ErrorType addNewResourcesType(Type type, string directory, string[] ext, bool removeInvalidResources = false)
        {
            reportProgress(directory);

            DirectoryInfo current;
            string char_path = directory;
            var result = Resource.ErrorType.NONE;
            var lastError = Resource.ErrorType.NONE;

            current = new DirectoryInfo(char_path);
            if (!current.Exists)
                return Resource.ErrorType.PATH_NOT_FOUND;
			if (ext != null)
			{
				foreach (FileInfo fileInfo in current.GetFiles())
				{
					string e = fileInfo.Extension.ToUpper();
					string rpath = char_path + "\\" + fileInfo.Name;

					bool found = false;
					foreach (string _ext in ext)
					{
						if (_ext == e) found = true;
					}

					if (found && getItemFromPath(rpath) == null)
					{
						Resource.ResourceItem item = Activator.CreateInstance(type) as Common.Resource.ResourceItem;
						item.name = fileInfo.Name.Substring(0, fileInfo.Name.IndexOf('.'));
						item.setPath(rpath);
                        lastError = item.verify();
                        if (lastError == Resource.ErrorType.NONE)
                        {
                            addItem(item);
                        }
                        else
                        {
                            if (removeInvalidResources)
                                File.Delete(rpath);
                            result = lastError;
                        }
					}
				}
			}
			else
			{
				//ディレクトリタイプのリソース
				foreach (DirectoryInfo info in current.GetDirectories())
				{
                    string path = directory + "\\" + info.Name;

                    if (getItemFromPath(path) != null)
                        continue;

					Resource.ResourceItem item = Activator.CreateInstance(type) as Common.Resource.ResourceItem;
					item.name = info.Name;
                    item.setPath(path);
                    if (lastError == Resource.ErrorType.NONE)
                    {
                        addItem(item);
                    }
                    else
                    {
                        if (removeInvalidResources)
                            Directory.Delete(path);
                        result = lastError;
                    }
				}
			}

            // 再帰をかける
            foreach (DirectoryInfo info in current.GetDirectories())
            {
                // 共通モーション用フォルダは無視
                if (info.Name == "motion")
                    continue;

                lastError = addNewResourcesType(type, directory + Path.DirectorySeparatorChar + info.Name, ext, removeInvalidResources);
                if (lastError != Resource.ErrorType.NONE)
                {
                    result = lastError;
                }
            }

            return result;
        }

        public int load(bool autoAddNewResource = true, string useBackup = null)
        {
            currentProgress = 0;

            sInstance = this;
            BinaryReaderWrapper.init();
            Resource.ResourceItem.sCurrentSourceMode = Resource.ResourceSource.RES_USER;

            var defaultResourceDir = sResourceDir;
            if (useBackup != null)
                sResourceDir = useBackup + Path.DirectorySeparatorChar;

            // フォルダ内の全てのロムを読み込む
            {
                var files = Util.file.getFiles(sResourceDir, "*" + ROM_EXTENSION).ToList();
                if(Util.file.dirExists(sResourceDir + "map"))
                    files.AddRange(Util.file.getFiles(sResourceDir + "map\\", "*" + ROM_EXTENSION));
                maxProgress = files.Count + 17;
                foreach (var path in files)
                {
                    var stream = Util.getFileStream(path);
                    load(stream);
                    stream.Close();
                    reportProgress(path);
                }
            }

            sResourceDir = defaultResourceDir;

            // システムリソース内の全てのロムを読み込む
            if (sCommonResourceDir != null)
            {
                Resource.ResourceItem.sCurrentSourceMode = Resource.ResourceSource.RES_SYSTEM;
                var files = Util.file.getFiles(sCommonResourceDir, "*" + ROM_EXTENSION);
                foreach (var path in files)
                {
                    var stream = Common.Util.getFileStream(path);
                    load(stream);
                    stream.Close();
                }
                Resource.ResourceItem.sCurrentSourceMode = Resource.ResourceSource.RES_USER;
            }

            // サンプルの戦闘背景を読み込む
            var battleBgDir = sCommonResourceDir + "\\..\\3dbattle";
            if (Directory.Exists(battleBgDir))
            {
                Resource.ResourceItem.sCurrentSourceMode = Resource.ResourceSource.RES_SYSTEM;
                var files = Util.file.getFiles(battleBgDir, "*" + ROM_EXTENSION);
                foreach (var path in files)
                {
                    var stream = Common.Util.getFileStream(path);
                    load(stream);
                    stream.Close();
                }
                Resource.ResourceItem.sCurrentSourceMode = Resource.ResourceSource.RES_USER;
            }
            
            Common.BinaryReaderWrapper.SetToUseDictionary(BinaryReaderWrapper.DictionaryType.NOT_USE);

            if (autoAddNewResource)
            {
                //未追加のデータを読み込む
                addNewResourcesType(typeof(Resource.Character), ".\\res\\character\\2D", new string[] { ".PNG" });
                addNewResourcesType(typeof(Resource.Character), ".\\res\\character\\3D", new string[] { ".FBX", ".PTCL" });
                addNewResourcesType(typeof(Resource.Bgm), ".\\res\\bgm", new string[] { ".WAV", ".OGG" });
                addNewResourcesType(typeof(Resource.Se), ".\\res\\se", new string[] { ".WAV", ".OGG" });
                addNewResourcesType(typeof(Resource.Window), ".\\res\\window", new string[] { ".PNG" });
                addNewResourcesType(typeof(Resource.Icon), ".\\res\\icon", new string[] { ".PNG" });
                addNewResourcesType(typeof(Resource.Sprite), ".\\res\\image", new string[] { ".PNG" });
                addNewResourcesType(typeof(Resource.Monster), ".\\res\\monster", new string[] { ".PNG" });
                addNewResourcesType(typeof(Resource.MapChip), ".\\res\\map", new string[] { ".PNG" });
                addNewResourcesType(typeof(Resource.MapObject), ".\\res\\mapobject", new string[] { ".FBX" });
                addNewResourcesType(typeof(Resource.MapBackground), ".\\res\\map_bg", new string[] { ".FBX" });
                addNewResourcesType(typeof(Resource.Face), ".\\res\\face", null);
                addNewResourcesType(typeof(Resource.Bgs), ".\\res\\bgs", new string[] { ".WAV", ".OGG" });
                addNewResourcesType(typeof(Resource.BattleBackground), ".\\res\\battle_bg", new string[] { ".PNG" });
                addNewResourcesType(typeof(Resource.Item), ".\\res\\item", new string[] { ".FBX" });
            }

            if (sProgressListener != null)
                sProgressListener(1, 1, "");

            return RESULT_NOERROR;
        }

        public void addDlcRoms(Guid dlcGuid)
        {
            // DLCパス内のリソースロムを読み込む
            if (sDlcDictionary.ContainsKey(dlcGuid))
            {
                var dlcPath = sDlcDictionary[dlcGuid].path;
                Resource.ResourceItem.sCurrentSourceMode = Resource.ResourceSource.RES_DLC;
                Resource.ResourceItem.sCurrentSourceGuid = dlcGuid;
                var files = Util.file.getFiles(dlcPath, "ResourceItem" + ROM_EXTENSION);
                foreach (var path in files)
                {
                    var stream = Common.Util.getFileStream(path);
                    load(stream);
                    stream.Close();
                }
                Resource.ResourceItem.sCurrentSourceMode = Resource.ResourceSource.RES_USER;
            }

            // DLCパス内の全てのロムを読み込む
            /*
            if (sDlcDictionary.ContainsKey(dlcGuid))
            {
                var dlcPath = sDlcDictionary[dlcGuid].path;
                Resource.ResourceItem.sCurrentSourceMode = Resource.ResourceSource.RES_DLC;
                Resource.ResourceItem.sCurrentSourceGuid = dlcGuid;
                var files = Util.file.getFiles(dlcPath, "*" + ROM_EXTENSION);
                foreach (var path in files)
                {
                    var stream = Common.Util.getFileStream(path);
                    load(stream);
                    stream.Close();
                }
                if (Util.file.dirExists(dlcPath + Path.DirectorySeparatorChar + "map"))
                {
                    files = Util.file.getFiles(dlcPath + Path.DirectorySeparatorChar + "map", "*" + ROM_EXTENSION);
                    foreach (var path in files)
                    {
                        var stream = Common.Util.getFileStream(path);
                        load(stream);
                        stream.Close();
                    }
                }
                Resource.ResourceItem.sCurrentSourceMode = Resource.ResourceSource.RES_USER;
            }
            */
        }

        private void reportProgress(string path)
        {
            if (sProgressListener != null)
            {
                sProgressListener(currentProgress, maxProgress, path + "(" + currentProgress + "/" + maxProgress + ")");
                currentProgress++;
            }
        }

        public int load(Stream stream, OVERWRITE_RULES overwrite = OVERWRITE_RULES.NEVER)
        {
            int chunkSize;
            short signature;
            long curPos;

            // 一応ヘッダーが一致するかどうかを調べる
            var reader = new BinaryReaderWrapper(stream, Encoding.UTF8);
            string text = System.Text.Encoding.ASCII.GetString(reader.ReadBytes(ROM_HEADER.Length));
            if (text != ROM_HEADER)
            {
                reader.Close();
                return RESULT_ERROR;
            }

            // システム設定を読み込む
            try
            {
                chunkSize = reader.ReadInt32();
                signature = reader.ReadInt16();
                curPos = stream.Position;

                var tmpStream = new MemoryStream();
                var buffer = new byte[chunkSize];
                stream.Read(buffer, 0, chunkSize);
                tmpStream.Write(buffer, 0, chunkSize);
                tmpStream.Position = 0;
                var tmpReader = new BinaryReaderWrapper(tmpStream, Encoding.UTF8);
                loadSystemContainer(tmpReader);
                loadMissingRomList(tmpReader);
                tmpReader.Close();

                stream.Seek(curPos + chunkSize, SeekOrigin.Begin);              // チャンク分シークする
            }
            catch (EndOfStreamException e)
            {
                Console.WriteLine(e.Message);
            }

            if (sRomVersion > ROM_VERSION)
            {
                reader.Close();
                resetCurrentRomVersion();
                return RESULT_ERROR;
            }

            // カタログの各データを読み込む
            while (stream.Position < stream.Length)
            {
                try
                {
                    chunkSize = reader.ReadInt32();
                    signature = reader.ReadInt16();
                }
                catch (EndOfStreamException e)
                {
                    Console.WriteLine(e.Message);
                    break;
                }

                var tmpStream = new MemoryStream();
                var buffer = new byte[chunkSize];
                stream.Read(buffer, 0, chunkSize);
                tmpStream.Write(buffer, 0, chunkSize);
                tmpStream.Position = 0;
                var tmpReader = new BinaryReaderWrapper(tmpStream, Encoding.UTF8);

                if (!romSignatureToTypeDic.ContainsKey(signature))
                    continue;

                var rom = createItem(romSignatureToTypeDic[signature]);     // シグネチャからインスタンスを作成する
                try
                {
                    rom.load(tmpReader); // 読み込む
                }
                catch (EndOfStreamException e)
                {
                    Console.WriteLine(e.Message);
                }
                catch (System.FormatException e)
                {
                    Console.WriteLine(e.Message);
                }
                catch (System.IO.FileNotFoundException e)
                {
                    var res = rom as Resource.ResourceItem;
                    if (res != null && !missingItemDic.ContainsKey(res.guId))
                    {
                        missingItemDic.Add(res.guId, res);
                    }

                    //ファイル名が変わっていたら死ぬ？
                    Console.WriteLine(rom.name + " : " + e.Message);
                    rom = null;
                }
                if (rom != null)
                {
                    addItem(rom, overwrite);                               // カタログに追加
                }

                tmpReader.Close();
            }

            reader.Close();

            var gsList = getFilteredItemList(typeof(Common.Rom.GameSettings));
            if (gsList.Count > 0)
                gameSettings = gsList[0] as Common.Rom.GameSettings;

            resetCurrentRomVersion();
            return RESULT_NOERROR;
        }

        public static RomItem createItem(Type type)
        {
            if (type == null)
            {
                Console.WriteLine("未対応のカテゴリのアイテムを追加しようとしました");
                return null;
            }
            return Activator.CreateInstance(type) as Common.Rom.RomItem;
        }

        public void appendItem(RomItem oldItem, RomItem newItem)
        {
            deleteItem(oldItem);
            addItem(newItem);
        }

        public static short getSignatureFromType(Type type)
        {
            return romTypeToSignatureDic[type];
        }

        public RomItem findFirstItem(Type type)
        {
            return itemList.Find(
                delegate(RomItem item)
                {
                    return item.GetType().IsSubclassOf(type) | item.GetType() == type;
                }
            );
        }

        public GameSettings getGameSettings()
        {
            if (gameSettings == null)
            {
                var list = getFilteredItemList(typeof(Common.Rom.GameSettings));
                if (list.Count > 0)
                {
                    gameSettings = list[0] as GameSettings;
                }
                else
                {
                    gameSettings = new GameSettings();
                    addItem(gameSettings);
                }
            }

            return gameSettings;
        }

        public static Type getType(BinaryReader reader)
        {
            var signature = reader.ReadInt16();
            if (romSignatureToTypeDic.ContainsKey(signature) == false) return null;
            return romSignatureToTypeDic[signature];
        }

        public static void writeType(BinaryWriter writer, Type currentCategory)
        {
            var signature = romTypeToSignatureDic[currentCategory];
            writer.Write(signature);
        }

        public void sort(Type targetType)
        {
            var list = getFilteredItemList(targetType);
            list.Sort(
                delegate(RomItem a, RomItem b)
                {
                    var asig = romTypeToSignatureDic[a.GetType()];
                    var bsig = romTypeToSignatureDic[b.GetType()];

                    if (asig > bsig)
                        return -1;
                    else if (asig < bsig)
                        return 1;

                    return String.Compare(a.name, b.name);
                }
            );

            foreach (var item in list)
            {
                itemList.Remove(item);
            }

            itemList.AddRange(list);
        }

        public void reorder(IEnumerable<RomItem> order)
        {
            itemList.RemoveAll(x => order.Contains(x));
            itemList.AddRange(order);
        }

        public static Type getTypeFromSignature(short signature)
        {
            return romSignatureToTypeDic[signature];
        }

        public List<Common.Rom.RomItem> getFullList()
        {
            return itemList;
        }

        public static string getRomFileName(Type type)
        {
            // TODO イベントとかスクリプトとかを単独保存するならこれじゃいけない
            return type.Name + Common.Catalog.ROM_EXTENSION;
        }

        public List<DlcInfo> getUsingDlcInfoList()
        {
            var list = getGameSettings().dlcList;
            var result = new List<DlcInfo>();
            foreach (var entry in list)
            {
                if (sDlcDictionary.ContainsKey(entry) && !result.Contains(sDlcDictionary[entry]))
                {
                    result.Add(sDlcDictionary[entry]);
                }
            }
            return result;
        }

        public static void createDlcList(bool jpPreferred)
        {
            sDlcDictionary.Clear();
            var dirs = Util.file.getDirectories(sDlcDir);
            foreach (var dir in dirs)
            {
                if (dir == "basic" || dir == "common")
                    continue;

                var infoPath = Util.getDLCInfoPath(dir, jpPreferred);
                if (infoPath == null)
                    continue;

                var infoReader = Util.file.getXmlReader(infoPath);
                var info = new DlcInfo();
                info.path = dir;
                while (infoReader.Read())
                {
                    if (infoReader.NodeType == XmlNodeType.Element)
                    {
                        switch (infoReader.LocalName)
                        {
                            case "guid":
                                info.guid = new Guid(infoReader.ReadString());
                                break;
                            case "title":
                                info.name = infoReader.ReadString();
                                break;
                        }
                    }
                }

                // コピー可否フラグを読むためにGameSettings.sgrを読み込む
                var gsPath = info.path + Path.DirectorySeparatorChar + "GameSettings" + ROM_EXTENSION;
                info.allowExport = true;    // サンプルゲーム型以外のものは全てエクスポートを許可する

                if (File.Exists(gsPath))
                {
                    var tmpCatalog = new Catalog();
                    var stream = Common.Util.getFileStream(gsPath);
                    tmpCatalog.load(stream);
                    stream.Close();
                    info.allowExport = tmpCatalog.getGameSettings().allowExport;
                    info.disallowExportGuidList = tmpCatalog.getGameSettings().notAllowExportResources;
                }

                if (info.name != null && !sDlcDictionary.ContainsKey(info.guid))
                    sDlcDictionary.Add(info.guid, info);
            }
        }

        public void clear(bool userResOnly)
        {
            if (userResOnly)
            {
                itemDic = itemDic.Values.Where(x =>
                {
                    var res = x as Common.Resource.ResourceItem;
                    if (res == null)
                        return true;
                    return res.source == Resource.ResourceSource.RES_SYSTEM;
                }).ToDictionary(x => x.guId);
                itemList.Clear();
                itemList.AddRange(itemDic.Values);
            }
            else
            {
                itemDic.Clear();
                itemList.Clear();
            }
        }

        public Resource.ResourceItem getRemovedRomInfo(Guid guid)
        {
            if (!missingItemDic.ContainsKey(guid))
                return null;
            return missingItemDic[guid];
        }

        public void verifyAllResource()
        {
            itemList = itemList.Where(x =>
            {
                var res = x as Common.Resource.ResourceItem;
                if (res == null)
                    return true;
                return Util.file.exists(res.path) || Util.file.dirExists(res.path);
            }).ToList();
            itemDic = itemList.ToDictionary(x => x.guId);
        }

        public void deleteAndIgnoreItem(RomItem rom)
        {
            deleteItem(rom);

            if (rom != null)
            {
                var ignoreRom = new Common.Rom.IgnoreItem();
                ignoreRom.name = rom.name;
                ignoreRom.guId = rom.guId;
                addItem(ignoreRom);
            }
        }

        public void restoreSystemResources(string fileName)
        {
            if (sCommonResourceDir != null)
            {
                Resource.ResourceItem.sCurrentSourceMode = Resource.ResourceSource.RES_SYSTEM;

                var stream = Common.Util.getFileStream(sCommonResourceDir + Path.DirectorySeparatorChar + fileName);
                load(stream, OVERWRITE_RULES.EQUAL_TO_IGNOREITEM);
                stream.Close();

                Resource.ResourceItem.sCurrentSourceMode = Resource.ResourceSource.RES_USER;
            }
        }
    }
}

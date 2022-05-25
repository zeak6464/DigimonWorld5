using System;
using System.IO;
using System.Linq;
using System.Text;
using Yukar.Common.GameData;

namespace Yukar.Common
{
    //----------------------------------------------------------
    // GameData版Catalog的なクラス
    //----------------------------------------------------------

    public class GameDataManager
    {
        const string SAVE_FILENAME = "Save_{0:D2}.sgs";
        public const string SAVE_DATENAME = ".date";    // 時刻を別データにする場合。差し当たってはUnity用
        static byte[] SIGNATURE = { 0x59, 0x55, 0x4b, 0x52, 0x44, 0x41, 0x54, 0x41 }; // "YUKRDATA"
        const int CURRENT_DATA_VERSION = 1;
        public static int sRecentDataVersion;   // 直前に読み込み開始したセーブデータのデータバージョン

        public Party party;
        public SystemData system;
        public StartSettings start;
        public static string dataPath = "";



        public void inititalize(Common.Catalog catalog)
        {
            var gameSettings = catalog.getGameSettings();
            if (gameSettings == null)
                return;

            // パーティ初期化
            party = new Party(catalog);
            foreach (var guid in gameSettings.party)
            {
                var hero = party.AddMember(catalog.getItemFromGuid(guid) as Common.Rom.Hero);
                if (hero != null)
                    hero.consistency();
            }
            party.SetMoney(gameSettings.money);

            // システムデータ初期化
            var old = system;
            system = new SystemData();
            if (old != null)
            {
                // リセット前のセッションデータがある場合は、サウンド設定のみ引き継ぐ
                system.copyConfigFrom(old);
            }

            // 初期マップ設定初期化
            start = new StartSettings();
            start.map = gameSettings.startMap;
            start.x = gameSettings.startX;
            start.y = gameSettings.startY;
            start.events = null;
        }

        public static void InititalizeAccount()
        {
#if UNITY_SWITCH && !UNITY_EDITOR
            GameDataManagerSwitch.InititalizeAccount();
#endif
        }

        public static string GetDataPath(int index, bool legacyPath = false)
        {
#if UNITY_SWITCH && !UNITY_EDITOR
            return String.Format(SAVE_FILENAME, index);
#else
            if (legacyPath)
                return dataPath + String.Format(SAVE_FILENAME, index);
            return dataPath + "savedata" + Path.DirectorySeparatorChar + String.Format(SAVE_FILENAME, index);
#endif
        }

#if WINDOWS
        public static void Save(GameDataManager data, int index)
        {
            // savedata 内ではないパスにあれば、それを消す
            var legacyPath = GetDataPath(index, true);
            if (File.Exists(legacyPath))
            {
                File.Delete(legacyPath);
            }

            var path = GetDataPath(index);
            Directory.CreateDirectory(Common.Util.file.getDirName(path));
            var stream = new FileStream(path, FileMode.Create);
            var writer = new BinaryWriter(stream);

            // シグネチャとデータバージョンを書く
            writer.Write(SIGNATURE);
            writer.Write(CURRENT_DATA_VERSION);

            var chunks = new IGameDataItem[] { data.party, data.system, data.start };
            foreach (var chunk in chunks)
            {
                saveChunk(chunk, writer);
            }

            writer.Close();
        }

        public static GameDataManager Load(Catalog catalog, int index)
        {
            var result = new GameDataManager();
            var fileName = GetDataPath(index);

            // ファイルが存在しなかったら、ゲーム開始データを読み込むだけ
            if (!File.Exists(fileName))
            {
                // savedata 内ではないパスにあれば、それを読み込む
                fileName = GetDataPath(index, true);
                if (!File.Exists(fileName))
                {
                    result.inititalize(catalog);
                    return result;
                }
            }

            result.party = new Party(catalog);
            result.system = new SystemData();
            result.start = new StartSettings();

            var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            var reader = new BinaryReader(stream);

            // シグネチャとデータバージョンを読み込む
            var signature = reader.ReadBytes(SIGNATURE.Length);

            if (!signature.SequenceEqual(SIGNATURE))
            {
                // シグネチャが一致しなかったら、ゲーム開始データを読み込むだけ
                result.inititalize(catalog);
                reader.Close();
                return result;
            }

            sRecentDataVersion = reader.ReadInt32();

            var chunks = new IGameDataItem[] { result.party, result.system, result.start };
            foreach (var chunk in chunks)
            {
                readChunk(catalog, chunk, reader);
            }

            reader.Close();
            return result;
        }
#else
        public static void Save(GameDataManager data, int index)
        {
            var path = GetDataPath(index);
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            // シグネチャとデータバージョンを書く
            writer.Write(SIGNATURE);
            writer.Write(CURRENT_DATA_VERSION);

            var chunks = new IGameDataItem[] { data.party, data.system, data.start };
            foreach (var chunk in chunks)
            {
                saveChunk(chunk, writer);
            }

            // PlyaerPrefsに保存する
            stream.Seek(0, SeekOrigin.Begin);
#if UNITY_SWITCH && !UNITY_EDITOR
            GameDataManagerSwitch.Save(stream, path);
#else
            var base64 = Convert.ToBase64String(stream.ToArray());
            UnityEngine.PlayerPrefs.SetString(path, base64);                                    // データ本体
            UnityEngine.PlayerPrefs.SetString(path + SAVE_DATENAME, DateTime.Now.ToString());   // 時間
            UnityEngine.PlayerPrefs.Save();
#endif

            writer.Close();
        }

        public static GameDataManager Load(Catalog catalog, int index)
        {
            var result = new GameDataManager();
            var path = GetDataPath(index);

#if UNITY_SWITCH && !UNITY_EDITOR
            var bytes = GameDataManagerSwitch.Load(catalog, path);
            if(bytes == null){
                result.inititalize(catalog);
                return result;
            }
#else
            var base64 = UnityEngine.PlayerPrefs.GetString(path, null);
            // ファイルが存在しなかったら、ゲーム開始データを読み込むだけ
            if (base64 == null)
            {
                result.inititalize(catalog);
                return result;
            }

            var bytes = Convert.FromBase64String(base64);
#endif

            var stream = new MemoryStream(bytes);
            var reader = new BinaryReader(stream);

            result.party = new Party(catalog);
            result.system = new SystemData();
            result.start = new StartSettings();

            // シグネチャとデータバージョンを読み込む
            var signature = reader.ReadBytes(SIGNATURE.Length);

            if (!signature.SequenceEqual(SIGNATURE))
            {
                // シグネチャが一致しなかったら、ゲーム開始データを読み込むだけ
                result.inititalize(catalog);
                reader.Close();
                return result;
            }

            sRecentDataVersion = reader.ReadInt32();

            var chunks = new IGameDataItem[] { result.party, result.system, result.start };
            foreach (var chunk in chunks)
            {
                readChunk(catalog, chunk, reader);
            }

            reader.Close();
            return result;
        }

        public static string LoadDate(int idx)
        {
            var path = GameDataManager.GetDataPath(idx);
#if UNITY_SWITCH && !UNITY_EDITOR
            return GameDataManagerSwitch.LoadDate(path);
#else
            if (!UnityEngine.PlayerPrefs.HasKey(path))
                return "";

             return UnityEngine.PlayerPrefs.GetString(path + GameDataManager.SAVE_DATENAME, DateTime.Now.ToString());
#endif
        }
#endif//WINDOWS

        internal static void saveChunk(IGameDataItem chunk, BinaryWriter writer)
        {
            var stream = new MemoryStream();
            var tmpWriter = new BinaryWriter(stream);
            chunk.save(tmpWriter);
            writer.Write((int)stream.Length);
            writer.Write(stream.GetBuffer(), 0, (int)stream.Length);
            tmpWriter.Close();
        }

        internal static void readChunk(Catalog catalog, IGameDataItem chunk, BinaryReader reader)
        {
            var chunkSize = reader.ReadInt32();
            var curPos = reader.BaseStream.Position;

            var tmpStream = new MemoryStream();
            var buffer = reader.ReadBytes(chunkSize);
            tmpStream.Write(buffer, 0, chunkSize);
            tmpStream.Position = 0;
            var tmpReader = new BinaryReader(tmpStream, Encoding.UTF8);
            try
            {
                chunk.load(catalog, tmpReader); // 読み込む
            }
            catch (EndOfStreamException e)
            {
                Console.WriteLine(e.Message);
            }

            tmpReader.Close();

            reader.BaseStream.Seek(curPos + chunkSize, SeekOrigin.Begin); // チャンク分シークする
        }
    }
}

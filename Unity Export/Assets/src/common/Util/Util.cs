using SharpKmyMath;
using System;
using System.Collections.Generic;
using System.IO;

namespace Yukar.Common
{
    public static class Util
    {
        // フラグ(主にキー入力用)
        public const int DIR_UP = 1;
        public const int DIR_DOWN = 2;
        public const int DIR_LEFT = 4;
        public const int DIR_RIGHT = 8;

        // 通し番号(大抵はこっち)
        public const int DIR_SER_NONE = -1;
        public const int DIR_SER_UP = 0;
        public const int DIR_SER_DOWN = 1;
        public const int DIR_SER_LEFT = 2;
        public const int DIR_SER_RIGHT = 3;

        // メニューウィンドウの特殊な状態
        public const int RESULT_SELECTING = -1;
        public const int RESULT_CANCEL = -2;

        public static FileUtil file = new FileUtil();

        public static string UserAppDataPath
        {
            get
            {
                return GetFileSystemPath(Environment.SpecialFolder.ApplicationData, true);
            }
        }

        public static string DocumentsPath
        {
            get
            {
                return GetFileSystemPath(Environment.SpecialFolder.MyDocuments, false);
            }
        }

        public static string TempDir
        {
            get
            {
                return Path.GetTempPath() + "sgb_rpg";
            }
        }

        private static string GetFileSystemPath(Environment.SpecialFolder folder, bool fullAppPath)
        {
            // パスを取得
            string path = Environment.GetFolderPath(folder);    // ベース・パス

            if (fullAppPath)
            {
                path += Path.DirectorySeparatorChar + "SmileBoom" +                     // 会社名
                        Path.DirectorySeparatorChar + "SMILE GAME BUILDER RPG EDITION"; // 製品名
            }
            else
            {
                path += Path.DirectorySeparatorChar + "SmileBoom" + // 会社名
                        Path.DirectorySeparatorChar + "sgb" +       // 製品名
                        Path.DirectorySeparatorChar + "rpg";        // ジャンル名
            }

#if WINDOWS
            // パスのフォルダを作成
            lock (typeof(System.Windows.Forms.Application))
#endif
            {
                if (!Util.file.dirExists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            return path;
        }

        public static String getRelativePath(String baseDir, String targetPath)
        {
            if (targetPath == null)
                return "";

            //"%"を"%25"に変換しておく（デコード対策）
            baseDir = baseDir.Replace("%", "%25") + "\\";
            targetPath = targetPath.Replace("%", "%25");

            //相対パスを取得する
            Uri u1 = new Uri(baseDir);
            Uri u2 = new Uri(targetPath);
            Uri relativeUri = u1.MakeRelativeUri(u2);
            string relativePath = relativeUri.ToString();

            //URLデコードする（エンコード対策）
            relativePath = Uri.UnescapeDataString(relativePath);

            //"%25"を"%"に戻す
            relativePath = relativePath.Replace("%25", "%");

            //"/"を"\"に変換する
            relativePath = relativePath.Replace('/', '\\');

            return relativePath;
        }

        public static String getDefaultResourceFolderName(string path, Resource.ResourceItem rom)
        {
            return getDefaultResourceFolderName(path, rom.GetType());
        }

        public static String getDefaultResourceFolderName(string path, Type type)
        {
            switch (Common.Catalog.getSignatureFromType(type))
            {
                case Common.Catalog.SIGNATURE_EFFECT_SOURCE:
                    return "res\\effect";
                case Common.Catalog.SIGNATURE_WINDOW:
                    return "res\\window";
                case Common.Catalog.SIGNATURE_SPRITE:
                    return "res\\image";
                case Common.Catalog.SIGNATURE_BATTLE_BG:
                    return "res\\battle_bg";
                case Common.Catalog.SIGNATURE_CHARACTER:
                    var result = "res\\character\\";
                    if (Path.GetExtension(path).ToUpper() == ".PNG")
                        result += "2D";
                    else
                        result += "3D";
                    return result;
                case Common.Catalog.SIGNATURE_MAPCHIP:
                    return "res\\map";
                case Common.Catalog.SIGNATURE_ITEM_MODEL:
                    return "res\\item";
                case Common.Catalog.SIGNATURE_MAP_BG:
                    return "res\\map_bg";
                case Common.Catalog.SIGNATURE_ICON:
                    return "res\\icon";
                case Common.Catalog.SIGNATURE_MONSTER_IMG:
                    return "res\\monster";
                case Common.Catalog.SIGNATURE_FACE:
                    return "res\\face";
                case Common.Catalog.SIGNATURE_BGM:
                    return "res\\bgm";
                case Common.Catalog.SIGNATURE_SE:
                    return "res\\se";
                case Common.Catalog.SIGNATURE_BGS:
                    return "res\\bgs";
                case Common.Catalog.SIGNATURE_BUILDING:
                case Common.Catalog.SIGNATURE_MAPITEM:
                    return "res\\mapobject";
                default:
                    return "";
            }
        }

#if true
        // Guidを読み込む
        static public Guid readGuid(System.IO.BinaryReader reader)
        {
            var buf = reader.ReadBytes(Catalog.GUID_SIZE);
            if (buf.Length != Catalog.GUID_SIZE)
                return Guid.Empty;

            return new Guid(buf);
        }
#else
        public static readonly Dictionary<Guid, Guid> GUID_REPLACE_TABLE = new Dictionary<Guid, Guid>()
        {
            {new Guid("60d18a23-aed0-4e23-8958-137b8e0efe27"), new Guid("a648647a-0a37-4420-b430-590964e76290")},
            {new Guid("c659997e-907e-4a39-96b8-82b557573568"), new Guid("d0d16d42-c912-4aa8-8ed0-a758c08516d7")},
            {new Guid("a1d8d375-3f35-4f4c-ae71-e5a74f666517"), new Guid("08d3f297-9b4d-4fb7-b0a9-59143e0f28f7")},
            {new Guid("826aa50c-22b2-4986-876f-c714af0401c0"), new Guid("0b708c35-c724-4f3a-ae34-76014c9f4623")},
            {new Guid("3ef2467b-d08c-4238-bac5-7099304bdb72"), new Guid("bfc1dbfa-0a73-4ebb-8814-09ac4bbd1a0c")},
            {new Guid("e2ac86d3-c5ad-4931-a018-546fd111416f"), new Guid("051630e3-3cb3-480c-a683-9a76deda1e36")},
            {new Guid("600dd07e-0c14-406c-a856-df9eb99a9ee1"), new Guid("99b5a807-ca3c-4b08-89ac-0f9ce51b3942")},
        };

        // Guidを読み込む
        static public Guid readGuid(System.IO.BinaryReader reader)
        {
            var buf = reader.ReadBytes(Catalog.GUID_SIZE);
            if (buf.Length != Catalog.GUID_SIZE)
                return Guid.Empty;

            var result = new Guid(buf); 

            // 置き換えテーブルを反映
            if (GUID_REPLACE_TABLE.ContainsKey(result))
            {
                result = GUID_REPLACE_TABLE[result];
            }

            return result;
        }
#endif

        public static int getReverseDir(int dir)
        {
            switch (dir)
            {
                case Util.DIR_SER_UP: return Util.DIR_SER_DOWN;
                case Util.DIR_SER_DOWN: return Util.DIR_SER_UP;
                case Util.DIR_SER_LEFT: return Util.DIR_SER_RIGHT;
                case Util.DIR_SER_RIGHT: return Util.DIR_SER_LEFT;
            }

            return -1;
        }

        public static Common.Rom.Script createMoveScript(Rom.Event.MoveType moveType, int timing, int[] movingLimits, string motion)
        {
            var script = new Common.Rom.Script();
            script.name = "AutoMove";
            script.trigger = Rom.Script.Trigger.PARALLEL_MV;

            // ループ
            var cmd = new Common.Rom.Script.Command();
            cmd.type = Common.Rom.Script.Command.FuncType.LOOP;
            script.commands.Add(cmd);

            int type = 0;
            switch (moveType)
            {
                case Rom.Event.MoveType.RANDOM:
                    type = (int)Common.Rom.Script.Command.MoveType.RANDOM;
                    break;
                case Rom.Event.MoveType.FOLLOW:
                    type = (int)Common.Rom.Script.Command.MoveType.FOLLOW;
                    break;
                case Rom.Event.MoveType.ESCAPE:
                    type = (int)Common.Rom.Script.Command.MoveType.ESCAPE;
                    break;
            }

            // 移動
            for (int i = 0; i < 8; i++)
            {
                addMove(script, type, movingLimits, motion);
                addWait(script, timing);
            }

            // ランダムを混ぜる
            for (int i = 0; i < 2; i++)
            {
                addMove(script, (int)Common.Rom.Script.Command.MoveType.RANDOM, movingLimits, motion);
                addWait(script, timing);
            }

            // ENDLOOP
            cmd = new Common.Rom.Script.Command();
            cmd.type = Common.Rom.Script.Command.FuncType.ENDLOOP;
            script.commands.Add(cmd);

            return script;
        }

        public static List<string> getParticleTextureNameList(string path)
        {
            var result = new List<string>();

            using (var reader = new StreamReader(path))
            {
                const string TEXTUREDEF = "texture ";
                while (!reader.EndOfStream)
                {
                    var text = reader.ReadLine();
                    if (!text.StartsWith(TEXTUREDEF))
                        continue;

                    var texFileName = text.Substring(TEXTUREDEF.Length);
                    texFileName = Path.ChangeExtension(texFileName, "png");

                    if (!result.Contains(texFileName))
                        result.Add(texFileName);
                }
            }

            return result;
        }

        private static void addWait(Rom.Script script, int timing)
        {
            int wait = 30;
            switch (timing)
            {
                case -3: wait = 120; break;
                case -2: wait = 100; break;
                case -1: wait = 80; break;
                case 0: wait = 60; break;
                case 1: wait = 30; break;
                case 2: wait = 15; break;
                case 3: wait = 0; break;
            }

            if (wait > 0)
            {
                // ウェイト
                var cmd = new Common.Rom.Script.Command();
                cmd.type = Common.Rom.Script.Command.FuncType.WAIT;
                cmd.indent = 1;
                cmd.attrList.Add(new Common.Rom.Script.IntAttr(wait));
                cmd.attrList.Add(new Common.Rom.Script.IntAttr(0));
                script.commands.Add(cmd);
            }
        }

        private static void addMove(Rom.Script script, int type, int[] movingLimits, string motion)
        {
            // 移動
            var cmd = new Common.Rom.Script.Command();
            cmd.type = Common.Rom.Script.Command.FuncType.ROUTE_MOVE;
            cmd.indent = 1;
            cmd.attrList.Add(new Common.Rom.Script.IntAttr(type));
            cmd.attrList.Add(new Common.Rom.Script.IntAttr(1)); //「イベントから触れた」スクリプトのトリガーになるかどうか
            foreach (var movingLimit in movingLimits)
            {
                cmd.attrList.Add(new Common.Rom.Script.IntAttr(movingLimit));
            }
            cmd.attrList.Add(new Common.Rom.Script.StringAttr(motion));
            script.commands.Add(cmd);
        }

        public static string createSkillDescription(Catalog catalog, GameData.Party party, Rom.Skill skill, bool useNewLine = true)
        {
            var glossary = catalog.getGameSettings().glossary;
            string result = "";

            if (skill.option.consumptionHitpoint > 0)
                result += string.Format(glossary.skillCostStr, glossary.hp, skill.option.consumptionHitpoint);

            if (skill.option.consumptionMagicpoint > 0)
            {
                if (!IsNullOrWhiteSpace(result))
                    result += " / ";

                result += string.Format(glossary.skillCostStr, glossary.mp, skill.option.consumptionMagicpoint);
            }

            var itemRom = catalog.getItemFromGuid(skill.option.consumptionItem);
            if (itemRom != null)
            {
                if (!IsNullOrWhiteSpace(result))
                    result += " / ";

                result += string.Format(glossary.skillCostStr, glossary.item, itemRom.name + "(" + skill.option.consumptionItemAmount /*+ "/" + party.GetItemNum(skill.option.consumptionItem)*/ + ")");
            }

            if (!IsNullOrWhiteSpace(result))
            {
                if (useNewLine)
                    result += "\n";
                else
                    result += " / ";
            }

            result += skill.description;

            return result;
        }

        public static string createEquipmentDescription(Rom.GameSettings gs, Rom.Item item)
        {
            string result = "";

            if (item.weapon != null)
            {
                if (item.weapon.attrAttack > 0)
                {
                    result += gs.glossary.attackPower + " " + item.weapon.attack + " + " +
                        gs.glossary.attrNames[item.weapon.attribute] + item.weapon.attrAttack;
                }
                else
                {
                    result += gs.glossary.attackPower + " " + item.weapon.attack;
                }
            }
            else if (item.equipable != null)
            {
                result += gs.glossary.defense + " " + item.equipable.defense;
            }

            result += " / ";

            result += item.description;

            return result;
        }

        // 差分があったら true を返すメソッド
        public static bool compareBinary(Stream a, Stream b, bool withSeek = true)
        {
            if (withSeek)
            {
                a.Seek(0, SeekOrigin.Begin);
                b.Seek(0, SeekOrigin.Begin);
            }

            if (a.Length != b.Length)
                return true;

            for (int i = 0; i < a.Length; i++)
            {
                if (a.ReadByte() != b.ReadByte())
                {
                    return true;
                }
            }

            return false;
        }

        public static string getSaveDate(int idx, bool multiLine = false)
        {
#if WINDOWS
            var path = Yukar.Common.GameDataManager.GetDataPath(idx);
            if (!Util.file.exists(path))
                path = Yukar.Common.GameDataManager.GetDataPath(idx, true);

            if (!Util.file.exists(path))
                return "";
            var date = File.GetLastWriteTime(path);
            bool isJapanese = System.Globalization.CultureInfo.CurrentCulture.Name == "ja-JP";
#else
            var dateStr = GameDataManager.LoadDate(idx);
            if (dateStr == "")
            {
                return "";
            }
            var date = new DateTime();
            DateTime.TryParse(dateStr, out date);
            bool isJapanese = UnityEngine.Application.systemLanguage == UnityEngine.SystemLanguage.Japanese;
#endif

            if (isJapanese)
            {
                return date.ToString("yy/MM/dd") + (multiLine ? "\n" : " ") + date.ToString("HH:mm:ss");
            }
            else
            {
                return date.ToString("MM/dd/yy") + (multiLine ? "\n" : " ") + date.ToString("HH:mm:ss");
            }
        }

        public static bool stringIsNullOrWhiteSpace(string value)
        {
            return string.IsNullOrEmpty(value) || value.Trim().Length == 0;
        }

        internal static string searchFile(string dir, string fileName)
        {
            // 検索先自体がなかったらエラー
            if (!file.dirExists(dir))
            {
                return null;
            }

            // dir 内を検索
            var entries = file.getFileSystemEntries(dir);
            foreach (string entry in entries)
            {
                var name = file.getFileName(entry);
                if (name.ToLower() == fileName)
                    return entry;
            }

            // なかったら子フォルダに対しても呼び出す
            var dirs = file.getDirectories(dir);
            foreach (string childDir in dirs)
            {
                var result = searchFile(childDir, fileName);
                if (result != null)
                    return result;
            }

            return null;
        }

        // data.zip か ローカルファイルからストリームを取得する
        public static Stream getFileStream(string path)
        {
            return file.getFileStream(path);
        }

        public static string getDLCInfoPath(string dir, bool jpPreferred)
        {
            if (jpPreferred)
            {
                var infoPath = dir + Path.DirectorySeparatorChar + "dlcinfo_jp.xml";
                if (!Util.file.exists(infoPath))
                {
                    infoPath = dir + Path.DirectorySeparatorChar + "dlcinfo.xml";
                    if (!Util.file.exists(infoPath))
                    {
                        infoPath = dir + Path.DirectorySeparatorChar + "dlcinfo_en.xml";
                        if (!Util.file.exists(infoPath))
                            return null;
                    }
                }
                return infoPath;
            }
            else
            {
                var infoPath = dir + Path.DirectorySeparatorChar + "dlcinfo_en.xml";
                if (!Util.file.exists(infoPath))
                {
                    infoPath = dir + Path.DirectorySeparatorChar + "dlcinfo.xml";
                    if (!Util.file.exists(infoPath))
                    {
                        infoPath = dir + Path.DirectorySeparatorChar + "dlcinfo_jp.xml";
                        if (!Util.file.exists(infoPath))
                            return null;
                    }
                }
                return infoPath;
            }
        }

        public static Common.Rom.Map searchEventContainsMap(Catalog catalog, Guid guid)
        {
            var mapList = catalog.getFilteredItemList(typeof(Common.Rom.Map));
            foreach (Common.Rom.Map map in mapList)
            {
                int idx = 0;
                while (true)
                {
                    var evRef = map.getEvent(idx);
                    idx++;
                    if (evRef == null) break;

                    if (evRef.guId == guid) return map;
                }
            }
            return null;
        }

        public static bool HasFlag(this Enum self, Enum flag)
        {
            try
            {
                var selfValue = Convert.ToUInt32(self);
                var flagValue = Convert.ToUInt32(flag);

                return (selfValue & flagValue) == flagValue;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        public static bool IsNullOrWhiteSpace(string value)
        {
            return value == null || value.Trim() == "";
        }

#if WINDOWS
        public static Microsoft.Xna.Framework.Color ToXnaColor(System.Drawing.Color color)
        {
            return new Microsoft.Xna.Framework.Color(color.R, color.G, color.B, color.A);
        }

        public static System.Drawing.Color ToDrawingColor(Microsoft.Xna.Framework.Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
#endif

        public static float getScreenPos(Matrix4 pp, Matrix4 vv, Vector4 v4, out int x, out int y, int viewportX = 1, int viewportY = 1)
        {
#if WINDOWS
            x = (int)((v4.x * 0.5f + 0.5f) * viewportX);
            y = (int)((v4.y * -0.5f + 0.5f) * viewportY);
#else
            float defaultAspect = (float)viewportY / viewportX;
            float screenAspect = (float)UnityEngine.Screen.height / UnityEngine.Screen.width;
            float fixRatio = defaultAspect / screenAspect;
            x = (int)((v4.x * fixRatio * 0.25f + 0.5f) * viewportX);
            y = (int)((v4.y * - 0.25f + 0.5f) * viewportY);
#endif
            return v4.z;
        }

        public static List<string> ParseFormula(string formula)
        {
            var words = new List<string>();
            string word = "";
            foreach (var chr in formula)
            {
                switch (chr)
                {
                    case '(':
                    case ')':
                    case '*':
                    case '/':
                    case '%':
                    case '+':
                    case '-':
                    case ',':
                        if (!string.IsNullOrEmpty(word))
                            words.Add(word);
                        words.Add(chr.ToString());
                        word = "";
                        break;
                    case ' ':
                        break;
                    default:
                        word += chr;
                        break;
                }
            }
            if (!string.IsNullOrEmpty(word))
                words.Add(word);

            return words;
        }

        public static List<string> SortToRPN(List<string> words)
        {
            var result = new List<string>();
            var stack = new Stack<string>();
            var prStk = new Stack<int>();
            int curNest = 0;
            int priority = 0;

            prStk.Push(0);

            foreach (var word in words)
            {
                switch (word)
                {
                    case "(":
                        priority = 0;
                        curNest += 10;
                        break;
                    case ")":
                        priority = 0;
                        curNest -= 10;
                        break;
                    case "min":
                    case "max":
                    case "rand":
                        priority = 5 + curNest;
                        break;
                    case "*":
                    case "/":
                    case "%":
                        priority = 4 + curNest;
                        break;
                    case "+":
                    case "-":
                        priority = 3 + curNest;
                        break;
                    case ",":
                        priority = 1 + curNest;
                        break;
                    default:
                        priority = 0;

                        // 数値や変数はそのまま結果に入れる
                        result.Add(word);
                        break;
                }

                // 演算子だったらスタックに積んだものと比較する
                if (priority > 0)
                {
                    // スタックの先頭の演算子の優先順位を下回るまでpopする
                    while (priority <= prStk.Peek())
                    {
                        result.Add(stack.Pop());
                        prStk.Pop();
                    }

                    // 演算子をスタックに積む
                    stack.Push(word);
                    prStk.Push(priority);
                }
            }

            // スタックに残っている演算子を全てpopする
            while (stack.Count > 0)
            {
                result.Add(stack.Pop());
            }

            return result;
        }
    }
}

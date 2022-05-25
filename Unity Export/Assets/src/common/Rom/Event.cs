using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yukar.Common.Rom
{
    public class Event : RomItem
    {
        public enum Priority
        {
            UNDER,      // 主人公よりも下
            EQUAL,      // 主人公と同じ(重ならない)
            OVER,       // 主人公よりも上
        }

        public enum MoveType
        {
            NONE,
            RANDOM,
            FOLLOW,
            ESCAPE,
        }

        public Guid templateType = Guid.Empty; // テンプレートのGuid
        public string templateInfo = "";       // テンプレートエディタで設定した内容を格納しておく所
        public Guid Graphic
        {
            get
            {
                if (sheetList.Count == 0)
                    return Guid.Empty;
                else
                    return sheetList[0].graphic;
            }
        }
        public int Direction
        {
            get
            {
                if (sheetList.Count == 0 || sheetList[0].direction == Util.DIR_SER_NONE)
                    return defaultDirection;
                else
                    return sheetList[0].direction;
            }
        }
        public string Motion
        {
            get
            {
                if (sheetList.Count == 0)
                    return "";
                else
                    return sheetList[0].graphicMotion;
            }
        }

        private Dictionary<Guid, Guid> guidConverter; // 消去したGuidを存在するGuidにすり替える

        // 条件
        public class Condition
        {
            public enum Type
            {
                COND_TYPE_SWITCH,
                COND_TYPE_VARIABLE,
                COND_TYPE_MONEY,
                COND_TYPE_ITEM,
                COND_TYPE_HERO,
                COND_TYPE_ITEM_WITH_EQUIPMENT,
                COND_TYPE_BATTLE,
                RESERVED_1,
                RESERVED_2,
                RESERVED_3,
            }
            public Type type;
            public Script.Command.ConditionType cond;
            public int index;
            public int option;
            public Guid refGuid;
        }

        // イベントシート
        public class Sheet : RomItem
        {
            public List<Condition> condList = new List<Condition>();

            public Guid graphic;
            public int direction = Util.DIR_SER_NONE;
            public Event.Priority priority;
            public bool collidable = true;
            public int moveSpeed;
            public bool fixDirection;
            public Event.MoveType moveType;
            public int moveTiming;
            public Guid script;
            public string graphicMotion = "";
            public int movingLimitRight = NOT_USING_MOVING_LIMIT;
            public int movingLimitLeft = NOT_USING_MOVING_LIMIT;
            public int movingLimitUp = NOT_USING_MOVING_LIMIT;
            public int movingLimitDown = NOT_USING_MOVING_LIMIT;
            public const int NOT_USING_MOVING_LIMIT = -1;
            public const int MAX_MOVING_LIMIT = 255;

            public override void save(System.IO.BinaryWriter writer)
            {
                writer.Write(condList.Count);
                foreach (var cond in condList)
                {
                    writer.Write((int)cond.type);
                    writer.Write((int)cond.cond);
                    writer.Write(cond.index);
                    writer.Write(cond.option);
                    writer.Write(cond.refGuid.ToByteArray());
                }

                writer.Write(name);
                writer.Write(graphic.ToByteArray());
                writer.Write(direction);
                writer.Write((int)priority);
                writer.Write(collidable);
                writer.Write(moveSpeed);
                writer.Write(fixDirection);
                writer.Write((int)moveType);
                writer.Write(moveTiming);
                writer.Write(script.ToByteArray());
                writer.Write(graphicMotion == null ? "" : graphicMotion);
                writer.Write(movingLimitRight);
                writer.Write(movingLimitLeft);
                writer.Write(movingLimitUp);
                writer.Write(movingLimitDown);
            }

            public override void load(System.IO.BinaryReader reader)
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    var cond = new Condition();
                    cond.type = (Condition.Type)reader.ReadInt32();
                    cond.cond = (Script.Command.ConditionType)reader.ReadInt32();
                    cond.index = reader.ReadInt32();
                    cond.option = reader.ReadInt32();
                    cond.refGuid = Util.readGuid(reader);
                    condList.Add(cond);
                }

                name = reader.ReadString();
                graphic = Util.readGuid(reader);
                direction = reader.ReadInt32();
                priority = (Event.Priority)reader.ReadInt32();
                collidable = reader.ReadBoolean();
                moveSpeed = reader.ReadInt32();
                fixDirection = reader.ReadBoolean();
                moveType = (Event.MoveType)reader.ReadInt32();
                moveTiming = reader.ReadInt32();
                script = Util.readGuid(reader);
                if (Catalog.sRomVersion >= 7)
                    graphicMotion = reader.ReadString();
                if (Catalog.sRomVersion >= 9)
                {
                    movingLimitRight = reader.ReadInt32();
                    movingLimitLeft = reader.ReadInt32();
                    movingLimitUp = reader.ReadInt32();
                    movingLimitDown = reader.ReadInt32();
                }
            }

            internal void saveToText(Action<string, string, int> write)
            {
                write("シート", name, 1);
                write("グラフィック", graphic.ToString(), 0);
                if (!string.IsNullOrEmpty(graphicMotion))
                    write("モーション", graphicMotion, 0);
                write("向き", "" + direction, 0);
                write("向き固定", "" + fixDirection, 0);
                write("衝突判定", "" + collidable, 0);
                write("移動速度", "" + moveSpeed, 0);
                write("移動頻度", "" + moveTiming, 0);
                write("移動タイプ", "" + moveType, 0);
                write("移動制限右", "" + movingLimitRight, 0);
                write("移動制限左", "" + movingLimitLeft, 0);
                write("移動制限上", "" + movingLimitUp, 0);
                write("移動制限下", "" + movingLimitDown, 0);

                foreach (var cond in condList)
                {
                    write("条件", "" + cond.type, 1);
                    write("比較演算子", "" + cond.cond, 0);
                    write("インデックス", "" + cond.index, 0);
                    write("オプション", "" + cond.option, 0);
                    if (cond.refGuid != Guid.Empty)
                    {
                        write("Guid参照", cond.refGuid.ToString(), 0);
                    }
                    write("条件終了", null, -1);
                }

                write("スクリプト", null, 1);
                var scriptRom = Catalog.sInstance.getItemFromGuid(script) as Common.Rom.Script;    // sInstance は使いたくないが仕方ない
                scriptRom.saveToText(write);
                write("スクリプト終了", null, -1);
                write("シート終了", null, -1);
            }

            internal void loadFromText(System.IO.StreamReader reader)
            {
                bool endOfSheet = false;
                Condition cond = null;
                while (!reader.EndOfStream && !endOfSheet)
                {
                    var chars = new char[] { '\t' };
                    var strs = reader.ReadLine().Split(chars, StringSplitOptions.RemoveEmptyEntries);
                    switch (strs[0])
                    {
                        case "グラフィック": graphic = new Guid(strs[1]); break;
                        case "モーション": if (strs.Length > 1) graphicMotion = strs[1]; break;
                        case "向き": direction = int.Parse(strs[1]); break;
                        case "向き固定": fixDirection = bool.Parse(strs[1]); break;
                        case "衝突判定": collidable = bool.Parse(strs[1]); break;
                        case "移動速度": moveSpeed = int.Parse(strs[1]); break;
                        case "移動頻度": moveTiming = int.Parse(strs[1]); break;
                        case "移動タイプ": moveType = (MoveType)Enum.Parse(typeof(MoveType), strs[1]); break;
                        case "移動制限右": movingLimitRight = int.Parse(strs[1]); break;
                        case "移動制限左": movingLimitLeft = int.Parse(strs[1]); break;
                        case "移動制限上": movingLimitUp = int.Parse(strs[1]); break;
                        case "移動制限下": movingLimitDown = int.Parse(strs[1]); break;

                        // シート条件系
                        case "条件":
                            cond = new Condition();
                            cond.type = (Condition.Type)Enum.Parse(typeof(Condition.Type), strs[1]);
                            break;
                        case "比較演算子": cond.cond = (Script.Command.ConditionType)Enum.Parse(typeof(Script.Command.ConditionType), strs[1]); break;
                        case "インデックス": cond.index = int.Parse(strs[1]); break;
                        case "オプション": cond.option = int.Parse(strs[1]); break;
                        case "Guid参照": cond.refGuid = new Guid(strs[1]); break;
                        case "条件終了": condList.Add(cond); break;

                        // スクリプト
                        case "スクリプト":
                            var scriptRom = new Script();
                            scriptRom.loadFromText(reader);
                            script = scriptRom.guId;
                            Catalog.sInstance.addItem(scriptRom);
                            break;

                        case "シート終了": endOfSheet = true; break;
                    }
                }
            }

            public int[] getMovingLimits()
            {
                var right = movingLimitRight;
                right = right == NOT_USING_MOVING_LIMIT ? MAX_MOVING_LIMIT : right;
                var left = movingLimitLeft;
                left = left == NOT_USING_MOVING_LIMIT ? MAX_MOVING_LIMIT : left;
                var up = movingLimitUp;
                up = up == NOT_USING_MOVING_LIMIT ? MAX_MOVING_LIMIT : up;
                var down = movingLimitDown;
                down = down == NOT_USING_MOVING_LIMIT ? MAX_MOVING_LIMIT : down;
                return new int[] { right, left, up, down };
            }

            public bool isUsingMovingLimit()
            {
                if (movingLimitDown == NOT_USING_MOVING_LIMIT)
                {
                    return false;
                }
                if (movingLimitLeft == NOT_USING_MOVING_LIMIT)
                {
                    return false;
                }
                if (movingLimitRight == NOT_USING_MOVING_LIMIT)
                {
                    return false;
                }
                if (movingLimitUp == NOT_USING_MOVING_LIMIT)
                {
                    return false;
                }
                return true;
            }

            public bool isAllMovingLimitMax()
            {
                if (movingLimitDown != MAX_MOVING_LIMIT)
                {
                    return false;
                }
                if (movingLimitLeft != MAX_MOVING_LIMIT)
                {
                    return false;
                }
                if (movingLimitRight != MAX_MOVING_LIMIT)
                {
                    return false;
                }
                if (movingLimitUp != MAX_MOVING_LIMIT)
                {
                    return false;
                }
                return true;
            }

            public void resetMovingLimit()
            {
                movingLimitDown = NOT_USING_MOVING_LIMIT;
                movingLimitLeft = NOT_USING_MOVING_LIMIT;
                movingLimitUp = NOT_USING_MOVING_LIMIT;
                movingLimitRight = NOT_USING_MOVING_LIMIT;
            }

            public void clampMovingLimit()
            {
                movingLimitDown = clampMovingLimit(movingLimitDown);
                movingLimitLeft = clampMovingLimit(movingLimitLeft);
                movingLimitRight = clampMovingLimit(movingLimitRight);
                movingLimitUp = clampMovingLimit(movingLimitUp);
            }

            private int clampMovingLimit(int movingLimit)
            {
                movingLimit = movingLimit > MAX_MOVING_LIMIT ? MAX_MOVING_LIMIT : movingLimit;
                movingLimit = movingLimit < NOT_USING_MOVING_LIMIT ? 0 : movingLimit;
                return movingLimit;
            }
        }
        public List<Sheet> sheetList = new List<Sheet>();
        public int defaultDirection = Util.DIR_SER_DOWN;

        public Event()
        {
            initializeGuidConverter();
        }

        public override void save(System.IO.BinaryWriter writer)
        {
            base.save(writer);

            writer.Write(sheetList.Count);
            foreach (var sheet in sheetList)
            {
                writeChunk(writer, sheet);
            }

            writer.Write(templateType.ToByteArray());
            writer.Write(templateInfo);
            writer.Write(defaultDirection);
        }

        public override void load(System.IO.BinaryReader reader)
        {
            base.load(reader);
            BinaryReaderWrapper.currentEventName = name;
            BinaryReaderWrapper.currentEventGuid = guId;

            Priority priority = Priority.EQUAL;
            //Guid graphic;
            if (Catalog.sRomVersion < 3)
            {
                //graphic = Util.readGuid(reader);
                reader.ReadInt32();
                priority = (Priority)reader.ReadInt32();
            }

            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                if (Catalog.sRomVersion < 3)
                {
                    var sheet = new Sheet();
                    sheet.script = Util.readGuid(reader);
                    sheetList.Add(sheet);
                }
                else if (Catalog.sRomVersion < 9)
                {
                    var sheet = new Sheet();
                    sheet.load(reader);
                    sheetList.Add(sheet);
                }
                else
                {
                    var sheet = new Sheet();
                    readChunk(reader, sheet);
                    sheetList.Add(sheet);
                }
            }

            if (Catalog.sRomVersion < 3)
            {
                var collidable = reader.ReadBoolean();
                var speed = reader.ReadInt32();
                var fixDirection = reader.ReadBoolean();
                var moveType = (MoveType)reader.ReadInt32();
                reader.ReadInt32(); // テンプレートの通し番号が入っているが、Guidに仕様変更したので再現できない

                foreach (var sheet in sheetList)
                {
                    sheet.name = name;
                    sheet.graphic = Graphic;
                    sheet.direction = Direction;
                    sheet.priority = priority;
                    sheet.collidable = collidable;
                    sheet.moveSpeed = speed;
                    sheet.fixDirection = fixDirection;
                    sheet.moveType = moveType;
                    sheet.moveTiming = 0;
                }
            }
            else
            {
                // サムネイル用にシート0のグラフィックをイベントインスタンスにも割り当てておく
                //if (sheetList.Count > 0)
                //{
                //    graphic = sheetList[0].graphic;
                //}

                templateType = Util.readGuid(reader);
                templateType = convertGuid(templateType);
                templateInfo = reader.ReadString();
                defaultDirection = reader.ReadInt32();
            }
        }

        public void addNewSheet(Catalog catalog, string name, Common.Rom.Script.Trigger trigger = Script.Trigger.TALK)
        {
            var script = new Common.Rom.Script();
            script.trigger = trigger;
            catalog.addItem(script);

            var sheet = new Sheet();
            sheet.name = name;
            sheet.priority = Priority.EQUAL;
            sheet.script = script.guId;

            sheetList.Add(sheet);
        }

        public void saveToText(System.IO.StreamWriter writer)
        {
            int indent = 0;
            Action<string, string, int> action = (string elem, string value, int addIndent) =>
            {
                // スコープの終了の場合は先にインデントを引く
                if (addIndent < 0)
                    indent += addIndent;

                // インデントを書く
                for (int i = 0; i < indent; i++)
                {
                    writer.Write("\t");
                }

                // 本文を書く
                writer.Write(elem);
                if (value != null)
                {
                    value = value.Replace("\r", ""); // \r はあったら消す
                    value = value.Replace("\n", "\\n"); // 改行は \n という文字列に変換しておく
                    writer.Write("\t" + value);
                }
                writer.Write("\n");

                // スコープの開始の場合は後からインデントを足す
                if (addIndent > 0)
                    indent += addIndent;
            };

            action("Guid", Guid.NewGuid().ToString(), 0);    // テンプレートのGUIDになるので、新たに割り振る
            action("イベント名", name, 0);

            // シートを書き出す
            foreach (var sheet in sheetList)
            {
                sheet.saveToText(action);
            }
        }

        public bool loadFromText(System.IO.StreamReader reader)
        {
            while (!reader.EndOfStream)
            {
                var chars = new char[] { '\t' };
                var strs = reader.ReadLine().Split(chars, StringSplitOptions.RemoveEmptyEntries);
                switch (strs[0])
                {
                    case "テンプレート定義": return true;
                    case "イベント名": name = strs[1]; break;
                    case "Guid": /*最終的に templateType に入れる*/ break;
                    case "シート":
                        var sheet = new Sheet();
                        sheet.name = strs[1];
                        sheet.loadFromText(reader);
                        sheetList.Add(sheet);
                        break;
                }
            }

            return false;
        }

        /// <summary>
        /// guidConverterの初期化を行います
        /// <para>消したイベントと中身が同じイベントをAddの第2引数に追加してください/para>
        /// <para>deletedGuidsに削除したものを追加してください</para>
        /// <para>高度なイベント内で設定してあるものはAddの第二引数のものに上書きされます</para>
        /// </summary>
        private void initializeGuidConverter()
        {
            guidConverter = new Dictionary<Guid, Guid>();
            guidConverter.Add(new Guid("62f2400c-2930-4ad5-821d-3dcc317b2785"), new Guid("ce458271-858e-4522-bb70-20b87e375393")); // 410_door2_link
            guidConverter.Add(new Guid("8b14c017-cf5f-4284-ae34-4983980b426c"), new Guid("bab4d731-52cd-4b4d-ac88-3d1a7e8102b7")); // 411_door2_linkbyItem
            guidConverter.Add(new Guid("2dacbad7-fed4-46a9-81be-4b21c8982880"), new Guid("ce458271-858e-4522-bb70-20b87e375393")); // 414_door4_link
            guidConverter.Add(new Guid("36eb1a55-6553-4356-9d37-19f1b5f1519b"), new Guid("bab4d731-52cd-4b4d-ac88-3d1a7e8102b7")); // 415_door4_linkbyItem
            guidConverter.Add(new Guid("0ce0d19d-8871-48cb-8c3d-93558c5b375d"), new Guid("ce458271-858e-4522-bb70-20b87e375393")); // 417_door6_link
            guidConverter.Add(new Guid("5eff2af5-f13c-479f-aa52-cdc8abb64f1c"), new Guid("bab4d731-52cd-4b4d-ac88-3d1a7e8102b7")); // 418_door6_linkbyItem
            guidConverter.Add(new Guid("7307bcbe-1474-4820-981f-4ec45659ac1a"), new Guid("ce458271-858e-4522-bb70-20b87e375393")); // 426_door9_link
            guidConverter.Add(new Guid("ccb9714e-728c-4f60-a597-2dc4e961d3a8"), new Guid("bab4d731-52cd-4b4d-ac88-3d1a7e8102b7")); // 427_door9_linkbyItem
        }

        private Guid convertGuid(Guid before)
        {
            var temp = before;
            guidConverter.TryGetValue(before, out before);
            if (before == Guid.Empty)
            {
                before = temp;
            }
            return before;
        }
    }
}

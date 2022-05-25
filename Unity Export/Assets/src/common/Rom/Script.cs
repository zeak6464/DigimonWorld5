using System;
using System.Collections.Generic;

namespace Yukar.Common.Rom
{
    public class Script : RomItem
    {
        public const int MAX_ATTR = 250;
        public const int MAX_LABEL = 100;
        public const int MAX_ATTR_PER_UNIT = (sizeof(uint) * 8) / Attr.ATTRTYPE_LENGTH - 1;

        public bool ignoreHeight;
        public bool expandArea;
        public enum Trigger
        {
            NONE,       // 条件なし(一応あるけど基本廃止)
            AUTO,       // 自動的に開始
            TALK,       // 話しかける
            HIT,        // 主人公から触れる
            HIT_FROM_EV,// イベントから触れる
            AUTO_REPEAT,// 自動的に開始(繰り返し)
            PARALLEL,   // 自動的に開始(並列)
            PARALLEL_MV,// 自動的に開始(並列・ロックあり・移動設定用)
            AUTO_PARALLEL, // 自動的に開始(並列・一度のみ)

            BATTLE_START,// バトル開始時
            BATTLE_TURN,// バトルターン毎
            BATTLE_PARALLEL,//バトル並列
            BATTLE_END,//バトル終了時
        }
        public Trigger trigger;
        public abstract class Attr
        {
            public const int ATTR_INT = 1;
            public const int ATTR_GUID = 2;
            public const int ATTR_STRING = 3;
            public const int ATTRTYPE_LENGTH = 2;

            internal abstract byte type { get; }
            public abstract void save(System.IO.BinaryWriter writer);
            public abstract void load(System.IO.BinaryReader reader);

            public Guid GetGuid()
            {
                if (!(this is GuidAttr))
                    return Guid.Empty;
                return ((GuidAttr)this).value;
            }
            public int GetInt()
            {
                if (!(this is IntAttr))
                    return 0;
                return ((IntAttr)this).value;
            }
            public string GetString()
            {
                if (!(this is StringAttr))
                    return "";
                return ((StringAttr)this).value;
            }
            public bool GetBool()
            {
                if (!(this is IntAttr))
                    return false;
                return ((IntAttr)this).value != 0;
            }
            public float GetFloat()
            {
                if (!(this is IntAttr))
                    return 0;
                return ((IntAttr)this).value / 1000f;
            }
        }
        public class IntAttr : Attr
        {
            public int value;

            public IntAttr() { }
            public IntAttr(int p)
            {
                value = p;
            }
            internal override byte type{ get{return ATTR_INT;} }
            public override void save(System.IO.BinaryWriter writer)
            {
                writer.Write(value);
            }
            public override void load(System.IO.BinaryReader reader)
            {
                value = reader.ReadInt32();
            }
        }
        public class GuidAttr : Attr
        {
            public Guid value;

            public GuidAttr() { }
            public GuidAttr(Guid guid)
            {
                value = guid;
            }
            internal override byte type { get { return ATTR_GUID; } }
            public override void save(System.IO.BinaryWriter writer)
            {
                writer.Write(value.ToByteArray());
            }
            public override void load(System.IO.BinaryReader reader)
            {
                value = Util.readGuid(reader);
            }
        }
        public class StringAttr : Attr
        {
            public string value;

            public StringAttr() { }
            public StringAttr(string p)
            {
                value = p;
            }
            internal override byte type { get { return ATTR_STRING; } }
            public override void save(System.IO.BinaryWriter writer)
            {
                writer.Write(value);
            }
            public override void load(System.IO.BinaryReader reader)
            {
                value = reader.ReadString();
            }
        }
        public class Command
        {
            public enum FuncType
            {
                IF,
                LOOP,
                LABEL,
                JUMP,
                BREAK,
                EXEC,
                PAUSE,
                RESUME,
                END,
                WAIT,
                ROTATE,
                WALK,
                WALKSPEED,
                PRIORITY,
                GRAPHIC,
                MOVE,
                PLROTATE,
                PLWALK,
                SW_PLLOCK,
                _UNLOCK,         // 操作禁止のアンロックが入っていたが、↑に統合したので欠番
                PLMOVE,
                PLAYBGM,
                PLAYSE,
                SPHIDE,
                SPMOVE,
                SPPICTURE,
                SPTEXT,
                EFFECT,
                SCREEN,
                MESSAGE,
                CHOICES,
                VARIABLE,
                SWITCH,
                ITEM,
                MONEY,
                STATUS,
                BATTLE,
                SHOP,
                INN,
                SAVE,
                EXIT,
                FIELD,
                PARTY,
                DIALOGUE,
                TELOP,
                EMOTE,
                IFSWITCH,
                IFVARIABLE,
                IFITEM,
                IFMONEY,
                IFPARTY,
                PLHIDE,
                EVHIDE,
                CAMERA,
                PLPRIORITY,
                CHG_EXP,
                CHG_SKILL,
                CHG_STTAILM,
                CHG_HPMP,
                FULLRECOV,
                SCREEN_FADE,
                SCREEN_COLOR,
                SCREEN_SHAKE,
                SCREEN_FLASH,
                PLAYJINGLE,
                BOSSBATTLE,
                SW_ENCOUNTING,
                SW_SAVE,
                SW_MENU,
                PLGRAPHIC,
                HLVARIABLE,
                ELSE,
                ENDIF,
                ENDLOOP,
                BRANCH,
                BLANK,
                STOPSE,
                SW_CAMLOCK,
                SW_DASH,
                PLWALKSPEED,
                SCRIPT_CS,
                DEBUGFUNC,
                CHANGE_HERO_NAME,
                STRING_VARIABLE,
                IF_STRING_VARIABLE,
                CHANGE_STRING_VARIABLE,
                ROUTE_MOVE,
                IF_INVENTORY_EMPTY,
                ITEM_THROW_OUT,
                WALK_IN_ROWS,
                WALK_IN_ROWS_ORDER,
                CHANGE_GAMEOVER_ACTION,
                CHANGE_PLAYER_HEIGHT,
                CHANGE_HEIGHT,
                FALL_PLAYER,
                FALL_EVENT,
                CHANGE_PLAYER_MOVABLE,
                CHANGE_MOVABLE,
                SW_JUMP,
                JUMP_CONFIG,
                JOINT_WEAPON,
                SHOT_EVENT_OLD,
                DESTROY_EVENT,
                CHANGE_PLAYER_SCALE,
                CHANGE_SCALE,
                SHOT_EVENT,
                INVINCIBLE,
                GET_TERRAIN,
                PLSNAP,
                EVSNAP,
                CAM_PROJ,
                CAM_POSANGLE,
                CAM_OPTION,
                MODIFY_MATERIAL,
                PLWALK_TGT,
                EVWALK_TGT,

                // バトル用
                BTL_SW_UI,
                BTL_HEAL,
                BTL_SELECTOR,
                BTL_ACTION,
                BTL_STATUS,
                BTL_STOP,
                BTL_APPEAR,
                BTL_VARIABLE,
                BTL_IFBATTLE,

                EQUIP,
                COMMENT,
                BTL_IFMONSTER,
                BTL_SW_CAMERA,
                WEBBROWSER,
                PLAYBGS,
            }

            public enum PosType
            {
                NO_MOVE,
                CONSTANT,
                VARIABLE,
            }

            public enum WaitUnitType
            {
                FRAMES,
                SECONDS,
            }

            public enum MoveType
            {
                UP,
                DOWN,
                LEFT,
                RIGHT,
                RANDOM,
                FOLLOW,
                ESCAPE,
                SPIN,
            }

            public enum IfSourceType
            {
                SWITCH,
                VARIABLE,
                MONEY,
                ITEM,
                HERO,
                HERO_STATUS,
            }

            public enum IfSourceType2
            {
                CONSTANT,
                VARIABLE,
            }

            public enum IfHeroSourceType
            {
                STATUS_AILMENTS,
                LEVEL,
                HITPOINT,
                MAGICPOINT,
                ATTACKPOWER,
                Defense,
                POWER,
                VITALITY,
                MAGIC,
                SPEED,
                EQUIPMENT_WEIGHT,
            }

            public enum IfHeroAilmentType
            {
                POISON,
                DOWN,
            }

            public enum ConditionType
            {
                EQUAL,
                NOT_EQUAL,
                EQUAL_GREATER,
                EQUAL_LOWER,
                GREATER,
                LOWER,
            };

            public enum EffectTarget
            {
                THIS_EVENT,
                HERO,
                SCREEN,
                SPRITE,
            };

            public enum ScreenEffectType
            {
                FADE_IN,
                FADE_OUT,
                COLOR_CHANGE,
                SHAKE,
            };

            public enum VarDestType
            {
                VARIABLE,
                VARIABLE_REF,
            }

            public enum VarSourceType
            {
                CONSTANT,
                RANDOM,
                VARIABLE,
                VARIABLE_REF,
                MONEY,
                ITEM,
                HERO_STATUS,
                RTC,
                PLAYTIME,
                MAPSIZE_X,
                MAPSIZE_Y,
                MAP_WEATHER,
                KEY_INPUT,
                CAMERA,
                POS_EVENT_X,
                POS_EVENT_Y,
                POS_EVENT_HEIGHT,
                POS_PLAYER_X,
                POS_PLAYER_Y,
                POS_PLAYER_HEIGHT,
                POS_EVENT_DIR,
                POS_PLAYER_DIR,
                EVENT_STATUS,

                BATTLE_STATUS_PARTY,
                BATTLE_STATUS_MONSTER,
                BATTLE_RESULT,
                BATTLE_SKILL_TARGET,

                POS_EVENT_SCREEN_X,
                POS_EVENT_SCREEN_Y,
                POS_PLAYER_SCREEN_X,
                POS_PLAYER_SCREEN_Y,

                // 追加するときはここに
                PARTY_NUMBER,

                // エディタ用
                POS_TYPE_EVENT,
                POS_TYPE_PLAYER,
            }

            public enum VarTimeSourceType
            {
                YEAR,
                MONTH,
                DAY,
                WEEKDAY,
                HOUR,
                MINUTE,
                SECOND,
            }

            public enum VarHeroSourceType
            {
                LEVEL,
                HITPOINT,
                MAGICPOINT,
                MAXHITPOINT,
                MAXMAGICPOINT,
                ATTACKPOWER,
                DEFENSE,
                DEXTERITY,
                EVASION,
                SPEED,
                EXP,
                MAGICPOWER,
                HP_PERCENT,
                MP_PERCENT,
                STATUSAILMENTS,
                STATUSAILMENTS_POISON,
                PARTYINDEX,
                MONEY,  // Dummy
            }

            public enum OperatorType
            {
                ASSIGNMENT,
                ADDING,
                SUBTRACTION,
                MULTIPLICATION,
                DIVISION,
                RANDOM,
                SURPLUS,
            }

            public FuncType type;
            public List<Attr> attrList = new List<Attr>();
            public int indent;

            //-------------------------------------------------------------------
            // イベントエディタ用プロパティ
            //-------------------------------------------------------------------
            internal class ToolData
            {
#if WINDOWS
                internal System.Drawing.Rectangle graphRect = new System.Drawing.Rectangle(0, 0, 0, 0);
                internal System.Drawing.Rectangle insertRect = new System.Drawing.Rectangle(0, 0, 0, 0);
#endif
            }
            static Dictionary<Command, ToolData> sToolDataTable = new Dictionary<Command, ToolData>();
            public static void clearToolDataTable() { sToolDataTable.Clear(); }
            private ToolData getStruct(Command command)
            {
                if (sToolDataTable.ContainsKey(command))
                {
                    return sToolDataTable[command];
                }

                var result = new ToolData();
                sToolDataTable.Add(command, result);
                return result;
            }
#if WINDOWS
            public System.Drawing.Rectangle graphRect { get { return getStruct(this).graphRect; } set { getStruct(this).graphRect = value; } }
            public System.Drawing.Rectangle insertRect { get { return getStruct(this).insertRect; } set { getStruct(this).insertRect = value; } }
#endif
            //-------------------------------------------------------------------

            public void save(System.IO.BinaryWriter writer)
            {
                writer.Write((int)type);
                writer.Write(indent);
                int attrTypes = 0;
                attrList.Reverse();
                int count = 0;
                foreach (var attr in attrList)
                {
                    if (count % MAX_ATTR_PER_UNIT == MAX_ATTR_PER_UNIT - 1)
                    {
                        writer.Write(0x40000000 | attrTypes);
                        attrTypes = 0;
                    }

                    attrTypes = (attrTypes << Attr.ATTRTYPE_LENGTH) | attr.type;
                    count++;
                }
                writer.Write(attrTypes);
                attrList.Reverse();
                foreach (var attr in attrList)
                {
                    attr.save(writer);
                }
            }
            public void load(System.IO.BinaryReader reader)
            {
                type = (FuncType)reader.ReadInt32();
                indent = reader.ReadInt32();
                var attrTypeArray = new List<byte>();

                bool continued = true;
                int totalCount = 0;
                while (continued)
                {
                    continued = false;
                    uint attrs = reader.ReadUInt32();
                    uint attrTypes = attrs;
                    int count = 0;
                    while (attrTypes != 0)
                    {
                        // 引数リストの最上位が0以外だったら、次の4バイトも引数リストと見なす
                        if (count == MAX_ATTR_PER_UNIT)
                        {
                            continued = true;
                        }
                        else
                        {
                            attrTypeArray.Insert(count, (byte)(attrTypes & 0x3));
                            totalCount++;
                        }
                        attrTypes >>= Attr.ATTRTYPE_LENGTH;
                        count++;
                    }
                }

                for (int i = 0; i < totalCount; i++)
                {
                    Attr attr = null;
                    switch (attrTypeArray[i])
                    {
                        case Attr.ATTR_INT:
                            attr = new IntAttr();
                            break;
                        case Attr.ATTR_GUID:
                            attr = new GuidAttr();
                            break;
                        case Attr.ATTR_STRING:
                            attr = new StringAttr();
                            break;
                        default:
                            continue;   // ここには来たらダメ
                    }
                    attr.load(reader);
                    attrList.Add(attr);
                }
            }

            public bool isIfType()
            {
                return type == FuncType.IF || type == FuncType.BOSSBATTLE || type == FuncType.IFPARTY || type == FuncType.IFMONEY ||
                       type == FuncType.IFITEM || type == FuncType.IFVARIABLE || type == FuncType.IFSWITCH || type == FuncType.SHOP ||
                       type == FuncType.INN || type == FuncType.IF_STRING_VARIABLE || type == FuncType.IF_INVENTORY_EMPTY ||
                       type == FuncType.BTL_IFBATTLE || type == FuncType.BTL_IFMONSTER;
            }

            public bool isScopedType(bool includeBranch = false)
            {
                if (includeBranch)
                    return isIfType() || type == FuncType.LOOP || type == FuncType.ELSE || type == FuncType.CHOICES || type == FuncType.BRANCH;
                else
                    return isIfType() || type == FuncType.CHOICES || type == FuncType.LOOP;
            }

            public bool isScopeEndedType(bool includeBranch = false)
            {
                if (includeBranch)
                    return type == FuncType.ENDIF || type == FuncType.ENDLOOP || type == FuncType.BRANCH || type == FuncType.ELSE;
                else
                    return type == FuncType.ENDIF || type == FuncType.ENDLOOP;
            }

            public void pushAttr(int p)
            {
                var attr = new IntAttr(p);
                attrList.Add(attr);
            }
            public void pushAttr(Guid guid)
            {
                var attr = new GuidAttr(guid);
                attrList.Add(attr);
            }
            public void pushAttr(uint p)
            {
                var attr = new IntAttr((int)p);
                attrList.Add(attr);
            }
            public void pushAttr(string p)
            {
                if (p == null)
                    p = "";
                var attr = new StringAttr(p);
                attrList.Add(attr);
            }
            public void pushAttr(bool p)
            {
                var attr = new IntAttr(p ? 1 : 0);
                attrList.Add(attr);
            }
            public void pushAttr(float p)
            {
                var attr = new IntAttr((int)(p * 1000));
                attrList.Add(attr);
            }
        }
        public List<Command> commands = new List<Command>();
        
        public override void save(System.IO.BinaryWriter writer)
        {
            base.save(writer);

            writer.Write((int)trigger);
            writer.Write(commands.Count);
            foreach (var command in commands)
            {
                command.save(writer);
            }
            writer.Write(ignoreHeight);
            writer.Write(expandArea);
        }

        public override void load(System.IO.BinaryReader reader)
        {
            base.load(reader);

            trigger = (Trigger)reader.ReadInt32();
            var scopeStack = new Stack<Command>();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var command = new Command();
                command.load(reader);
                commands.Add(command);

                // 古いVerとの互換用
                if (Catalog.sRomVersion < 5 &&
                    (command.type == Command.FuncType.SHOP || command.type == Command.FuncType.INN))
                {
                    var cmd = new Command();
                    cmd.type = Command.FuncType.ELSE;
                    commands.Add(cmd);
                    cmd = new Command();
                    cmd.type = Command.FuncType.ENDIF;
                    commands.Add(cmd);
                }

                // インデントを再算出してみる
                Action setNewIndent = () =>
                {
                    if (command.indent != scopeStack.Count)
                    {
                        // 違ってたら一応警告を出す
                        Console.WriteLine("Wrong line : " + BinaryReaderWrapper.currentEventName
                            + " / " + i + " " + command.type.ToString());
                    }
                    command.indent = scopeStack.Count;
                };

                // インデントは念のため再計算する
                if (command.isScopedType(true))
                {
                    if (!command.isScopedType(false))
                    {
                        if(scopeStack.Count > 0)
                            scopeStack.Pop();
                        else
                            // 違ってたら一応警告を出す
                            Console.WriteLine("Indent mismatched : " + BinaryReaderWrapper.currentEventName
                                + " / " + i + " " + command.type.ToString());
                    }
                    setNewIndent();
                    scopeStack.Push(command);
                }
                else if (command.isScopeEndedType())
                {
                    if (scopeStack.Count == 0)
                    {
                        // 数の合わないendifは削除する
                        commands.Remove(command);
                        i--;
                    }
                    else
                    {
                        scopeStack.Pop();
                        setNewIndent();
                    }
                }
                else
                {
                    setNewIndent();
                }
            }
            while (scopeStack.Count > 0)
            {
                // 足りないEndIfを足す
                var command = new Command();
                command.indent = scopeStack.Count - 1;
                command.type = Command.FuncType.ENDIF;
                if (scopeStack.Peek().type == Command.FuncType.LOOP)
                    command.type = Command.FuncType.ENDLOOP;
                else if(scopeStack.Peek().type != Command.FuncType.CHOICES)
                {
                    // Elseも足す場合の処理 TODO 判定して足すようにする
                    //var elseCommand = new Command();
                    //elseCommand.indent = scopeStack.Count - 1;
                    //elseCommand.type = Command.FuncType.ELSE;
                    //commands.Add(elseCommand);
                }
                else
                {
                    // Branchも足す場合の処理 TODO 判定して足すようにする
                    //for(int i=1; i<scopeStack.Peek().attrList[0].GetInt(); i++)
                    //{
                    //    var elseCommand = new Command();
                    //    elseCommand.indent = scopeStack.Count - 1;
                    //    elseCommand.type = Command.FuncType.BRANCH;
                    //    elseCommand.pushAttr(i);
                    //    commands.Add(elseCommand);
                    //}
                }
                commands.Add(command);

                scopeStack.Pop();
            }
            ignoreHeight = reader.ReadBoolean();
            expandArea = reader.ReadBoolean();
        }

        internal void saveToText(Action<string, string, int> write)
        {
            write("開始条件", "" + trigger, 0);
            write("高さ無視", "" + ignoreHeight, 0);
            write("判定拡張", "" + expandArea, 0);
            foreach (var command in commands)
            {
                write("コマンド", "" + command.type, 1);
                foreach (var attr in command.attrList)
                {
                    var attrType = attr.GetType();
                    if (attrType == typeof(IntAttr))
                    {
                        write("整数", "" + attr.GetInt(), 0);
                    }
                    else if (attrType == typeof(StringAttr))
                    {
                        write("文字列", attr.GetString(), 0);
                    }
                    else if (attrType == typeof(GuidAttr))
                    {
                        write("Guid", attr.GetGuid().ToString(), 0);
                    }
                }
                write("コマンド終了", null, -1);
            }
        }

        internal void loadFromText(System.IO.StreamReader reader)
        {
            bool endOfScript = false;
            Command command = null;
            int currentIndent = 0;
            while (!reader.EndOfStream && !endOfScript)
            {
                var chars = new char[] { '\t' };
                var strs = reader.ReadLine().Split(chars, StringSplitOptions.RemoveEmptyEntries);
                switch (strs[0])
                {
                    case "開始条件": trigger = (Trigger)Enum.Parse(typeof(Trigger), strs[1]); break;
                    case "高さ無視": ignoreHeight = bool.Parse(strs[1]); break;
                    case "判定拡張": expandArea = bool.Parse(strs[1]); break;

                    // コマンド系
                    case "コマンド":
                        command = new Command();
                        command.type = (Command.FuncType)Enum.Parse(typeof(Command.FuncType), strs[1]);
                        break;
                    case "整数": command.pushAttr(int.Parse(strs[1])); break;
                    case "文字列":
                        if (strs.Length == 1)
                            command.pushAttr("");
                        else
                            command.pushAttr(strs[1].Replace("\\n", "\r\n"));
                        break;
                    case "Guid": command.pushAttr(new Guid(strs[1])); break;
                    case "コマンド終了":
                        Action setNewIndent = () =>
                        {
                            if (currentIndent < 0)
                                currentIndent = 0;
                            command.indent = currentIndent;
                        };

                        // インデントは念のため再計算する
                        if (command.isScopedType(true))
                        {
                            if (!command.isScopedType(false))
                                currentIndent--;
                            setNewIndent();
                            currentIndent++;
                        }
                        else if (command.isScopeEndedType())
                        {
                            currentIndent--;
                            setNewIndent();
                        }
                        else
                        {
                            setNewIndent();
                        }
                        commands.Add(command);
                        break;

                    case "スクリプト終了": endOfScript = true; break;
                }
            }
        }
    }
}

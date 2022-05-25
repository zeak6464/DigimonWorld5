using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Yukar.Common.Rom
{
    public class GameSettings : RomItem
    {
        public class SystemGraphics : IChunk
        {
            public Guid menuSelected = new Guid("5d558bfc-3e58-49ed-96b4-8ccbb216431a");
            public Guid menuSelectable = new Guid("f8dccb3a-eee5-4b5f-888f-e2f1e31738b1");
            public Guid battleMessage = new Guid("7ad48689-9bf1-48a3-8892-a75df6ffce4d");
            public Guid battleSkillItemList = new Guid("3632255d-dc2e-43d4-a31d-5a71d1434b10");
            public Guid battleCommands2D = new Guid("89e017db-f483-4916-b523-d151be406b2c");
            public Guid battleCommands3D = new Guid("3f20ff7d-b860-47ff-91c8-491fa362cadb");
            public Guid battleStatus3D = new Guid("43a77fae-6682-43a1-9f21-01659ac17ac7");

            public void load(System.IO.BinaryReader reader)
            {
                menuSelectable = Util.readGuid(reader);
                menuSelected = Util.readGuid(reader);
                battleMessage = Util.readGuid(reader);
                battleSkillItemList = Util.readGuid(reader);
                battleCommands2D = Util.readGuid(reader);
                if (reader.BaseStream.Position >= reader.BaseStream.Length)
                    return;
                battleCommands3D = Util.readGuid(reader);
                battleStatus3D = Util.readGuid(reader);
            }

            public void save(System.IO.BinaryWriter writer)
            {
                writer.Write(menuSelectable.ToByteArray());
                writer.Write(menuSelected.ToByteArray());
                writer.Write(battleMessage.ToByteArray());
                writer.Write(battleSkillItemList.ToByteArray());
                writer.Write(battleCommands2D.ToByteArray());
                writer.Write(battleCommands3D.ToByteArray());
                writer.Write(battleStatus3D.ToByteArray());
            }
        }

        public class TitleScreen : RomItem
        {
            public enum ContentAlignment
            {
                TopLeft = 1,
                TopCenter = 2,
                TopRight = 4,
                MiddleLeft = 16,
                MiddleCenter = 32,
                MiddleRight = 64,
                BottomLeft = 256,
                BottomCenter = 512,
                BottomRight = 1024,
            }

            public enum TitleAnimation
            {
                None,
                FadeIn,
                PopUp,
                SlideInFromTop,
                SlideInFromBottom,
                SlideInFromLeft,
                SlideInFromRight,
            }

            public enum TitleMode
            {
                Graphics,
                Text,
            }

            public enum TitleTextPosition
            {
                Left,
                Center,
                Right
            }

            public enum TitleTextSize
            {
                Large,
                Middle,
                Small,
            }

            public enum TitleTextEffect
            {
                None,
                Shadow,
                Outline,
                Boild,
                Italic,
            }

            public enum SelectItemSortOrientation
            {
                Vertical,
                Horizontal,
                DiagonalRightUp,
                DiagonalRightDown,
            }

            public enum SelectItemTextOutline
            {
                Enable,
                Disable,
            }

            public enum CursorAnimation
            {
                Flash,
                Blink,
                Pan,
            }

            public string titleText
            {
                get
                {
                    if (string.IsNullOrEmpty(gs.subTitle))
                        return gs.name;
                    return gs.name + "\r\n" + gs.subTitle;
                }
            }

            public string TitleMainText { get { return gs.name; } }
            public string TitleSubText { get { return gs.subTitle; } }

            public ContentAlignment titleLogoDisplayPosition;
            public TitleAnimation titleAnimation;
            public TitleMode titleMode;
            public Guid titleGraphics;
            public Color titleTextColor;
            public TitleTextPosition titleTextPosition;
            public TitleTextSize titleTextSize;
            public TitleTextEffect titleTextEffect;
            public Guid titleTextDisplayWindow;

            public Guid titleBackgroundImage;
            public Guid bgm;
            public Guid bgs;

            public string selectItemNewGameText = "";
            public string selectItemContinueText = "";
            public string selectItemOptionText = "";
            public SelectItemSortOrientation selectItemSortOrientation;
            public ContentAlignment selectItemPosition;
            public Color selectItemTextColor;
            public SelectItemTextOutline selectItemTextOutline;
            public CursorAnimation cursorAnimation;
            public Guid cursorGraphics;
            public GameSettings gs;
            public Guid splashImage;
            private Guid DEFAULT_SPLASH_GUID = new Guid("24920c6f-8426-4cfa-bc8b-f79518e982d6");

            public override void load(System.IO.BinaryReader reader)
            {
                titleLogoDisplayPosition = (ContentAlignment)reader.ReadInt32();
                titleAnimation = (TitleAnimation)reader.ReadInt32();

                titleMode = (TitleMode)reader.ReadInt32();

                titleGraphics = Util.readGuid(reader);

                reader.ReadString();    // タイトル文字列設定があったが、今はダミー

                titleTextColor = convertFromOldColor(reader.ReadString());
                titleTextPosition = (TitleTextPosition)reader.ReadInt32();
                titleTextSize = (TitleTextSize)reader.ReadInt32();
                titleTextEffect = (TitleTextEffect)reader.ReadInt32();
                titleTextDisplayWindow = Util.readGuid(reader);

                titleBackgroundImage = Util.readGuid(reader);
                bgm = Util.readGuid(reader);
                bgs = Util.readGuid(reader);

                selectItemNewGameText = reader.ReadString();
                selectItemContinueText = reader.ReadString();
                selectItemOptionText = reader.ReadString();
                selectItemSortOrientation = (SelectItemSortOrientation)reader.ReadInt32();
                selectItemPosition = (ContentAlignment)reader.ReadInt32();
                selectItemTextColor = convertFromOldColor(reader.ReadString());
                selectItemTextOutline = (SelectItemTextOutline)reader.ReadInt32();

                cursorAnimation = (CursorAnimation)reader.ReadInt32();
                cursorGraphics = Util.readGuid(reader);
                splashImage = DEFAULT_SPLASH_GUID;
                if (reader.BaseStream.Length > reader.BaseStream.Position)
                    splashImage = Util.readGuid(reader);
            }

            private Color convertFromOldColor(string colorString)
            {
#if WINDOWS
                var color = System.Drawing.ColorTranslator.FromHtml(colorString);
                return new Color(color.R, color.G, color.B, color.A);
#else
                if (colorString.Length >= 7 && colorString[0] == '#')
                {
                    try
                    {
                        int cur = 1;
                        int len = 2;
                        byte r = Convert.ToByte(colorString.Substring(cur, len), 16); cur += len;
                        byte g = Convert.ToByte(colorString.Substring(cur, len), 16); cur += len;
                        byte b = Convert.ToByte(colorString.Substring(cur, len), 16); cur += len;
                        return new Color(r, g, b);
                    }
                    catch (FormatException)
                    {
                        // 何もしない
                    }
                }
                return new Color(255, 255, 255, 255);   // TODO
#endif
            }

            public override void save(System.IO.BinaryWriter writer)
            {
                writer.Write((int)titleLogoDisplayPosition);
                writer.Write((int)titleAnimation);

                writer.Write((int)titleMode);

                writer.Write(titleGraphics.ToByteArray());

                writer.Write("");   // タイトル文字列設定があったが、今はダミー

                saveColorToString(titleTextColor, writer);
                writer.Write((int)titleTextPosition);
                writer.Write((int)titleTextSize);
                writer.Write((int)titleTextEffect);
                writer.Write(titleTextDisplayWindow.ToByteArray());

                writer.Write(titleBackgroundImage.ToByteArray());
                writer.Write(bgm.ToByteArray());
                writer.Write(bgs.ToByteArray());

                writer.Write(selectItemNewGameText);
                writer.Write(selectItemContinueText);
                writer.Write(selectItemOptionText);
                writer.Write((int)selectItemSortOrientation);
                writer.Write((int)selectItemPosition);
                saveColorToString(selectItemTextColor, writer);
                writer.Write((int)selectItemTextOutline);

                writer.Write((int)cursorAnimation);
                writer.Write(cursorGraphics.ToByteArray());
                writer.Write(splashImage.ToByteArray());
            }

            private void saveColorToString(Color color, System.IO.BinaryWriter writer)
            {
                // この処理は.net frameworkでしか使えないので、自前で保存する
                //writer.Write(System.Drawing.ColorTranslator.ToHtml(color));
                writer.Write("#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2"));
            }
        }

        public class BattleCamera : RomItem
        {
            public ThirdPersonCameraSettings start;
            public ThirdPersonCameraSettings normal;
            public ThirdPersonCameraSettings attackStart;
            public ThirdPersonCameraSettings attackEnd;
            public ThirdPersonCameraSettings skillStart;
            public ThirdPersonCameraSettings skillEnd;
            public ThirdPersonCameraSettings itemStart;
            public ThirdPersonCameraSettings itemEnd;
            public ThirdPersonCameraSettings resultStart;
            public ThirdPersonCameraSettings resultEnd;
            public int startTime = 60;
            public int attackTime = 30;
            public int skillTime = 40;
            public int itemTime = 40;
            public int resultTime = 60;

            public enum TweenType
            {
                LINEAR,
                EASE_IN,
                EASE_OUT,
                EASE_IN_OUT,
            }
            public TweenType startTween = TweenType.EASE_OUT;
            public TweenType attackTween = TweenType.EASE_OUT;
            public TweenType skillTween = TweenType.EASE_OUT;
            public TweenType itemTween = TweenType.EASE_OUT;
            public TweenType resultTween = TweenType.EASE_OUT;

            public BattleCamera()
            {
                start = new Rom.ThirdPersonCameraSettings()
                {
                    distance = 100,
                    fov = 3.5f,
                    xAngle = -89,
                    yAngle = 0,
                    x = 0,
                    height = 0,
                    y = 0,
                };
                normal = new Rom.ThirdPersonCameraSettings()
                {
                    distance = 30,
                    fov = 7,
                    xAngle = -19,
                    yAngle = 339,
                    x = 0,
                    height = 0,
                    y = 0,
                };
                attackStart = new Rom.ThirdPersonCameraSettings()
                {
                    distance = 30,
                    fov = 7,
                    xAngle = -19,
                    yAngle = 339,
                    x = 0,
                    height = 0,
                    y = 0,
                };
                attackEnd = new Rom.ThirdPersonCameraSettings()
                {
                    distance = 32,
                    fov = 7,
                    xAngle = -35,
                    yAngle = 339,
                    x = 0,
                    height = 0,
                    y = 0,
                };
                skillStart = new Rom.ThirdPersonCameraSettings()
                {
                    distance = 9,
                    fov = 15,
                    xAngle = -59,
                    yAngle = 284,
                    x = 0,
                    height = 0,
                    y = 0,
                };
                skillEnd = new Rom.ThirdPersonCameraSettings()
                {
                    distance = 3.5f,
                    fov = 21,
                    xAngle = 0,
                    yAngle = 180,
                    x = 0,
                    height = 0,
                    y = 0,
                };
                itemStart = new Rom.ThirdPersonCameraSettings()
                {
                    distance = 9,
                    fov = 15,
                    xAngle = -59,
                    yAngle = 284,
                    x = 0,
                    height = 0,
                    y = 0,
                };
                itemEnd = new Rom.ThirdPersonCameraSettings()
                {
                    distance = 3.5f,
                    fov = 21,
                    xAngle = 0,
                    yAngle = 180,
                    x = 0,
                    height = 0,
                    y = 0,
                };
                resultStart = new Rom.ThirdPersonCameraSettings()
                {
                    distance = 3.0f,
                    fov = 21,
                    xAngle = -10,
                    yAngle = 180,
                    x = 0,
                    height = 1,
                    y = 0,
                };
                resultEnd = new Rom.ThirdPersonCameraSettings()
                {
                    distance = 3.5f,
                    fov = 21,
                    xAngle = 0,
                    yAngle = 180,
                    x = 0,
                    height = 1,
                    y = 0,
                };
            }

            public override void load(System.IO.BinaryReader reader)
            {
                readChunk(reader, start);
                readChunk(reader, normal);
                readChunk(reader, attackStart);
                readChunk(reader, attackEnd);
                readChunk(reader, skillStart);
                readChunk(reader, skillEnd);
                readChunk(reader, itemStart);
                readChunk(reader, itemEnd);
                readChunk(reader, resultStart);
                readChunk(reader, resultEnd);

                startTime = reader.ReadInt32();
                attackTime = reader.ReadInt32();
                skillTime = reader.ReadInt32();
                itemTime = reader.ReadInt32();
                resultTime = reader.ReadInt32();

                startTween = (TweenType)reader.ReadInt32();
                attackTween = (TweenType)reader.ReadInt32();
                skillTween = (TweenType)reader.ReadInt32();
                itemTween = (TweenType)reader.ReadInt32();
                resultTween = (TweenType)reader.ReadInt32();
            }

            public override void save(System.IO.BinaryWriter writer)
            {
                writeChunk(writer, start);
                writeChunk(writer, normal);
                writeChunk(writer, attackStart);
                writeChunk(writer, attackEnd);
                writeChunk(writer, skillStart);
                writeChunk(writer, skillEnd);
                writeChunk(writer, itemStart);
                writeChunk(writer, itemEnd);
                writeChunk(writer, resultStart);
                writeChunk(writer, resultEnd);

                writer.Write(startTime);
                writer.Write(attackTime);
                writer.Write(skillTime);
                writer.Write(itemTime);
                writer.Write(resultTime);

                writer.Write((int)startTween);
                writer.Write((int)attackTween);
                writer.Write((int)skillTween);
                writer.Write((int)itemTween);
                writer.Write((int)resultTween);
            }
        }

        public TitleScreen title = new TitleScreen();
        public BattleCamera battleCamera = new BattleCamera();

        public class ExportSettings : RomItem
        {
            public struct Unity
            {
                public string exportDestination;
                public string companyName;
                public string productName;
                public string applicationIdentifier;
                public bool visibleVirtualPad;
            }
            public Unity unity;

            public struct Windows
            {
                public string exportDestination;
                public string iconPath;
            }
            public Windows windows;

            public class Backup : IChunk
            {
                public string exportDestination = "";
                public string exportFileName = "";
                public string authorId = "";
                public string authorName = "";
                public string comment = "";

                public void load(BinaryReader reader)
                {
                    exportDestination = reader.ReadString();
                    exportFileName = reader.ReadString();
                    authorId = reader.ReadString();
                    authorName = reader.ReadString();
                    comment = reader.ReadString();
                }

                public void save(BinaryWriter writer)
                {
                    writer.Write(exportDestination);
                    writer.Write(exportFileName);
                    writer.Write(authorId);
                    writer.Write(authorName);
                    writer.Write(comment);
                }
            }
            public Backup backup = new Backup();

            public ExportSettings()
            {
                unity.exportDestination = "";
                unity.companyName = "";
                unity.productName = "";
                unity.applicationIdentifier = "";
                unity.visibleVirtualPad = true;

                windows.exportDestination = "";
                windows.iconPath = "";
            }

            public override void load(System.IO.BinaryReader reader)
            {
                unity.exportDestination = reader.ReadString();
                unity.companyName = reader.ReadString();
                unity.productName = reader.ReadString();
                unity.applicationIdentifier = reader.ReadString();

                windows.exportDestination = reader.ReadString();
                windows.iconPath = reader.ReadString();

                unity.visibleVirtualPad = reader.ReadBoolean();

                readChunk(reader, backup);
            }

            public override void save(System.IO.BinaryWriter writer)
            {
                writer.Write(unity.exportDestination);
                writer.Write(unity.companyName);
                writer.Write(unity.productName);
                writer.Write(unity.applicationIdentifier);

                writer.Write(windows.exportDestination);
                writer.Write(windows.iconPath);

                writer.Write(unity.visibleVirtualPad);

                writeChunk(writer, backup);
            }
        }

        public ExportSettings exportSettings = new ExportSettings();

        public struct CameraPreset
        {
            public string name;
            public bool writable;
            public Common.Rom.ThirdPersonCameraSettings tpPreset;
            public Common.Rom.FirstPersonCameraSettings fpPreset;
        }
        public List<CameraPreset> tpCameraPresets = new List<CameraPreset>();
        public List<CameraPreset> fpCameraPresets = new List<CameraPreset>();
        public ThirdPersonCameraSettings tpCamera = new ThirdPersonCameraSettings();
        public FirstPersonCameraSettings fpCamera = new FirstPersonCameraSettings();

        public List<Guid> party = new List<Guid>();

        public class ItemStack
        {
            public Guid guid;
            public int num;
        }
        public List<ItemStack> items = new List<ItemStack>();

        public static readonly bool IsDivideMapScene = true;
        public const int MAX_SWITCH = 1000;
        public const int MAX_VARIABLE = 1000;
        public const int MAX_STR_VARIABLE = 256;
        public const int LOCAL_SWITCH_OFFSET = 0x10000;
        public const int MAX_ATTRIBUTE = 11;
        public string[] switchNames = new string[MAX_SWITCH];
        public string[] variableNames = new string[MAX_VARIABLE];
        public string[] strVariableNames = new string[MAX_STR_VARIABLE];
        public string description = "";
        public string subTitle = "";
        public string gameFont = "";

        public int money;

        public int startX;
        public int startY;
        public Guid startMap;
        public Guid messageWindow;
        public Guid selectedBox;
        public Guid worldMapCursor;
        public Guid pageIcon;
        public Guid scrollIcon;
        public Guid equipEffectIcon;
        public Guid route;
        public Guid battleBgm;
        public Guid battleWinBgm;
        public Guid gameoverBgm;

        public Guid seBuy;
        public Guid seCancel;
        public Guid seDecide;
        public Guid seItem;
        public Guid seSelect;
        public Guid seSkill;

        public Guid seLevelUp;
        public Guid menuWindow;
        public Guid choicesWindow;
        public Guid seEscape;
        public Guid seDefeat;
        public Guid innJingle;
        public Guid seTextInput;
        public Guid seTextDelete;

        public bool allowExport;
        public List<Guid> notAllowExportResources = new List<Guid>();

        public List<Guid> dlcList = new List<Guid>();

        public class Glossary
        {
            public const string NEED_INITIALIZE = "%%NEED_INITIALIZE%%";

            public string moneyName = "";
            public string innOK = "";
            public string innCancel = "";
            public string notEnoughMoney = "";
            public string buy = "";
            public string sell = "";
            public string shopCancel = "";
            public string tradeComplete = "";
            public string sellComplete = "";
            public string[] attrNames = new string[] { "", "", "", "", "", "", "", "", "", "", "", "", "" };
            public string noItem = "";
            public string expendableItem = "";
            public string weapon = "";
            public string attackPower = "";
            public string armor = "";
            public string defense = "";
            public string shield = "";
            public string evasion = "";
            public string head = "";
            public string accessory = "";
            public string holdNum = "";
            public string item = "";
            public string skill = "";
            public string equipment = "";
            public string status = "";
            public string save = "";
            public string exit = "";
            public string use = "";
            public string throwaway = "";
            public string level = "";
            public string hp = "";
            public string mp = "";
            public string ailments = "";
            public string normal = "";
            public string poison = "";
            public string dead = "";
            public string skillUser = "";
            public string noSkill = "";
            public string equipTarget = "";
            public string removeItem = "";
            public string nothing = "";
            public string showStatus = "";
            public string power = "";
            public string vitarity = "";
            public string magic = "";
            public string speed = "";
            public string exp = "";
            public string levelGrowthRate = "";
            public string nextLevel = "";
            public string gameover = "";
            public string battle_attacktarget = NEED_INITIALIZE;
            public string battle_target = NEED_INITIALIZE;
            public string battle_wait = NEED_INITIALIZE;
            public string battle_sleep = NEED_INITIALIZE;
            public string battle_paralysis = NEED_INITIALIZE;
            public string battle_critical = NEED_INITIALIZE;
            public string battle_guard = NEED_INITIALIZE;
            public string battle_charge = NEED_INITIALIZE;
            public string battle_attack = NEED_INITIALIZE;
            public string battle_skill = NEED_INITIALIZE;
            public string battle_item = NEED_INITIALIZE;
            public string battle_start = NEED_INITIALIZE;
            public string battle_win = NEED_INITIALIZE;
            public string battle_lose = NEED_INITIALIZE;
            public string battle_escape = NEED_INITIALIZE;
            public string battle_get_money = NEED_INITIALIZE;
            public string battle_get_item = NEED_INITIALIZE;
            public string battle_get_exp = NEED_INITIALIZE;
            public string battle_levelup = NEED_INITIALIZE;
            public string battle_escape_command = "";
            public string battle_cancel = "";
            public string battle_miss = "";
            public string battle_back = "";
            public string battle_result_continue = "";
            public string skillCostStr = "";
            public string dexterity = "";
            public string close = "";
            public string decide = "";
            public string moveCursor = "";
            public string changeCharacter = "";
            public string changeSetting = "";
            public string saved = "";
            public string askOverwrite = "";
            public string askExitGame = "";
            public string yes = "";
            public string no = "";
            public string message_moment = "";
            public string message_fast = "";
            public string message_normal = "";
            public string message_slow = "";
            public string config_memory = "";
            public string config_not_memory = "";
            public string battle_result = "";
            public string bgm = "";
            public string se = "";
            public string message_speed = "";
            public string cursor_position = "";
            public string restore_defaults = "";
            public string config = "";
            public string elementAttackPower = "";
            public string money = "";
            public string playTime = "";
            public string battle_enemy_escape = NEED_INITIALIZE;
            public string battle_poison = NEED_INITIALIZE;
            public string battle_confusion = NEED_INITIALIZE;
            public string battle_fascination = NEED_INITIALIZE;
            public string battle_recover_sleep = NEED_INITIALIZE;
            public string battle_recover_paralysis = NEED_INITIALIZE;
            public string battle_recover_poison = NEED_INITIALIZE;
            public string battle_recover_fascination = NEED_INITIALIZE;
            public string battle_recover_confusion = NEED_INITIALIZE;
            public string battle_recover_dead = NEED_INITIALIZE;
            public string battle_escape_failed = NEED_INITIALIZE;
            public string battle_lv = "";
            public string battle_exp = "";
            public string saveData = "";

            public string inventoryFull = "";
            public string inventoryWitchItemThrowAway = "";
            public string inventoryAllOfStackThrowAway = "";
            public string inventoryAbandonItem = "";
            public string inventoryFullRemoveEquip = "";

            public string nowLoading = NEED_INITIALIZE;
        }
        public Glossary glossary = new Glossary();

        public enum ControlMode
        {
            DUNGEON_RPG_LIKE,
            FPS_LIKE,
        }
        public ControlMode controlMode = ControlMode.DUNGEON_RPG_LIKE;

        public enum GridMode
        {
            SNAP,
            FREE,
        }
        public GridMode gridMode = GridMode.SNAP;

        public enum BattleType
        {
            CLASSIC,
            MODERN,
        }
        public BattleType battleType = BattleType.CLASSIC;

        public Common.Resource.Icon.Ref escapeIcon = new Resource.Icon.Ref()
        {
            guId = new Guid("2df74ef4-0b65-4223-9ce8-ae471aa76a5b"),
            x = 6,
            y = 0,
        };
        public Common.Resource.Icon.Ref returnIcon = new Resource.Icon.Ref()
        {
            guId = new Guid("2df74ef4-0b65-4223-9ce8-ae471aa76a5b"),
            x = 1,
            y = 0,
        };
        public Common.Resource.Icon.Ref newItemIcon = new Resource.Icon.Ref()
        {
            guId = new Guid("2df74ef4-0b65-4223-9ce8-ae471aa76a5b"),
            x = 0,
            y = 0,
        };

        public bool vrEnabled;
        public List<Guid> commonEvents = new List<Guid>();
        public List<Guid> battleEvents = new List<Guid>(); // 複製元イベント

#if WINDOWS
        public int screenWidth = 960;
        public int screenHeight = 544;
#else
        public int screenWidth = 960;
        public int screenHeight = 540;
#endif

        public int inventoryMax = 999;

        public const int DEFAULT_VOLUME = 50;
        public int defaultBgmVolume = DEFAULT_VOLUME;
        public int defaultSeVolume = DEFAULT_VOLUME;

        public bool gameFontUseOldMethod = false;
        public SystemGraphics systemGraphics = new SystemGraphics();

        public Color[] damageNumColors = new Color[5]
        {// HP-          MP-           Critical   HP+                       MP+
            Color.White, Color.Orchid, Color.Red, new Color(191, 191, 255), Color.LightGreen
        };

        public override void save(System.IO.BinaryWriter writer)
        {
            base.save(writer);

            writer.Write(party.Count);
            foreach (var member in party)
            {
                writer.Write(member.ToByteArray());
            }

            writer.Write(items.Count);
            foreach (var item in items)
            {
                writer.Write(item.guid.ToByteArray());
                writer.Write(item.num);
            }

            writer.Write(money);

            writer.Write(startX);
            writer.Write(startY);
            writer.Write(startMap.ToByteArray());

            // 名前のあるスイッチ数を保存する
            var count = 0;
            for (int i = 0; i < MAX_SWITCH; i++)
            {
                if (switchNames[i] != null) count++;
            }
            writer.Write(count);
            // スイッチ名を保存
            for (int i = 0; i < MAX_SWITCH; i++)
            {
                if (switchNames[i] != null)
                {
                    writer.Write(i);
                    writer.Write(switchNames[i]);
                }
            }
            // 名前のある変数数を保存する
            count = 0;
            for (int i = 0; i < MAX_VARIABLE; i++)
            {
                if (variableNames[i] != null) count++;
            }
            writer.Write(count);
            // 変数名を保存
            for (int i = 0; i < MAX_VARIABLE; i++)
            {
                if (variableNames[i] != null)
                {
                    writer.Write(i);
                    writer.Write(variableNames[i]);
                }
            }

            writer.Write(messageWindow.ToByteArray());
            writer.Write(selectedBox.ToByteArray());
            writer.Write(worldMapCursor.ToByteArray());
            writer.Write(pageIcon.ToByteArray());

            writer.Write(scrollIcon.ToByteArray());
            writer.Write(equipEffectIcon.ToByteArray());

            writer.Write(glossary.moneyName);
            writer.Write(glossary.innOK);
            writer.Write(glossary.innCancel);
            writer.Write(glossary.notEnoughMoney);
            writer.Write(glossary.buy);
            writer.Write(glossary.sell);
            writer.Write(glossary.shopCancel);
            writer.Write(glossary.tradeComplete);
            writer.Write(glossary.sellComplete);

            writer.Write(glossary.attrNames.Length);
            foreach (var attrName in glossary.attrNames)
            {
                writer.Write(attrName);
            }

            writer.Write(route.ToByteArray());

            writer.Write(glossary.battle_result);

            writer.Write(glossary.saved);
            writer.Write(glossary.askOverwrite);
            writer.Write(glossary.askExitGame);

            writer.Write(glossary.decide);
            writer.Write(glossary.moveCursor);
            writer.Write(glossary.changeCharacter);
            writer.Write(glossary.changeSetting);

            writer.Write(glossary.close);
            writer.Write(glossary.dexterity);
            writer.Write(glossary.skillCostStr);

            writer.Write(glossary.noItem);
            writer.Write(glossary.expendableItem);
            writer.Write(glossary.weapon);
            writer.Write(glossary.attackPower);
            writer.Write(glossary.armor);
            writer.Write(glossary.defense);
            writer.Write(glossary.shield);
            writer.Write(glossary.evasion);
            writer.Write(glossary.head);
            writer.Write(glossary.accessory);
            writer.Write(glossary.holdNum);
            writer.Write(glossary.item);
            writer.Write(glossary.skill);
            writer.Write(glossary.equipment);
            writer.Write(glossary.status);
            writer.Write(glossary.save);
            writer.Write(glossary.exit);
            writer.Write(glossary.use);
            writer.Write(glossary.throwaway);
            writer.Write(glossary.level);
            writer.Write(glossary.hp);
            writer.Write(glossary.mp);
            writer.Write(glossary.ailments);
            writer.Write(glossary.normal);
            writer.Write(glossary.poison);
            writer.Write(glossary.dead);
            writer.Write(glossary.skillUser);
            writer.Write(glossary.noSkill);
            writer.Write(glossary.equipTarget);
            writer.Write(glossary.removeItem);
            writer.Write(glossary.nothing);
            writer.Write(glossary.showStatus);
            writer.Write(glossary.power);
            writer.Write(glossary.vitarity);
            writer.Write(glossary.magic);
            writer.Write(glossary.speed);
            writer.Write(glossary.exp);
            writer.Write(glossary.nextLevel);
            writer.Write(glossary.gameover);

            writer.Write(battleBgm.ToByteArray());

            writer.Write(glossary.battle_attacktarget);
            writer.Write(glossary.battle_target);
            writer.Write(glossary.battle_wait);
            writer.Write(glossary.battle_sleep);
            writer.Write(glossary.battle_paralysis);
            writer.Write(glossary.battle_critical);
            writer.Write(glossary.battle_guard);
            writer.Write(glossary.battle_charge);
            writer.Write(glossary.battle_attack);
            writer.Write(glossary.battle_skill);
            writer.Write(glossary.battle_item);
            writer.Write(glossary.battle_start);
            writer.Write(glossary.battle_win);
            writer.Write(glossary.battle_lose);
            writer.Write(glossary.battle_escape);
            writer.Write(glossary.battle_get_money);
            writer.Write(glossary.battle_get_item);
            writer.Write(glossary.battle_get_exp);
            writer.Write(glossary.battle_levelup);
            writer.Write(glossary.battle_escape_command);
            writer.Write(glossary.battle_cancel);
            writer.Write(glossary.battle_miss);
            writer.Write(glossary.battle_back);

            writer.Write(battleWinBgm.ToByteArray());
            writer.Write(gameoverBgm.ToByteArray());

            writer.Write(seBuy.ToByteArray());
            writer.Write(seCancel.ToByteArray());
            writer.Write(seDecide.ToByteArray());
            writer.Write(seSelect.ToByteArray());
            writer.Write(seItem.ToByteArray());
            writer.Write(seLevelUp.ToByteArray());
            writer.Write(menuWindow.ToByteArray());
            writer.Write(choicesWindow.ToByteArray());

            writeChunk(writer, title);

            writer.Write(glossary.yes);
            writer.Write(glossary.no);
            writer.Write(description);

            writer.Write(glossary.message_moment);
            writer.Write(glossary.message_fast);
            writer.Write(glossary.message_normal);
            writer.Write(glossary.message_slow);
            writer.Write(glossary.config_memory);
            writer.Write(glossary.config_not_memory);

            writer.Write(glossary.bgm);
            writer.Write(glossary.se);
            writer.Write(glossary.message_speed);
            writer.Write(glossary.cursor_position);
            writer.Write(glossary.restore_defaults);
            writer.Write(glossary.config);
            writer.Write(glossary.elementAttackPower);

            writer.Write(dlcList.Count);
            foreach (var guid in dlcList)
            {
                writer.Write(guid.ToByteArray());
            }

            writer.Write(glossary.money);
            writer.Write(glossary.playTime);
            writer.Write(subTitle);
            writer.Write(glossary.battle_result_continue);
            writer.Write(glossary.battle_poison);
            writer.Write(glossary.battle_confusion);
            writer.Write(glossary.battle_fascination);
            writer.Write(glossary.battle_enemy_escape);

            writer.Write(glossary.battle_recover_confusion);
            writer.Write(glossary.battle_recover_dead);
            writer.Write(glossary.battle_recover_fascination);
            writer.Write(glossary.battle_recover_paralysis);
            writer.Write(glossary.battle_recover_poison);
            writer.Write(glossary.battle_recover_sleep);

            writer.Write(glossary.battle_escape_failed);
            writer.Write(glossary.battle_lv);
            writer.Write(glossary.battle_exp);
            writer.Write(seEscape.ToByteArray());
            writer.Write(seDefeat.ToByteArray());
            writer.Write(innJingle.ToByteArray());

            writer.Write(gameFont);

            writer.Write(glossary.saveData);

            writeChunk(writer, tpCamera);
            writeChunk(writer, fpCamera);

            writer.Write(tpCameraPresets.Count);
            foreach (var preset in tpCameraPresets)
            {
                writer.Write(preset.name);
                writeChunk(writer, preset.tpPreset);
            }
            writer.Write(fpCameraPresets.Count);
            foreach (var preset in fpCameraPresets)
            {
                writer.Write(preset.name);
                writer.Write(preset.tpPreset != null);
                if (preset.tpPreset != null)
                    writeChunk(writer, preset.tpPreset);
                writeChunk(writer, preset.fpPreset);
            }

            writer.Write((int)controlMode);

            writer.Write((int)battleType);

            writer.Write(seSkill.ToByteArray());

            escapeIcon.save(writer);
            returnIcon.save(writer);
            newItemIcon.save(writer);

            writeChunk(writer, battleCamera);

            writer.Write(vrEnabled);

            // コモンイベントを保存
            writer.Write(commonEvents.Count);
            foreach (var guid in commonEvents)
            {
                writer.Write(guid.ToByteArray());
            }

            writer.Write(allowExport);
            writer.Write(notAllowExportResources.Count);
            foreach (var guid in notAllowExportResources)
            {
                writer.Write(guid.ToByteArray());
            }

            writer.Write(screenWidth);
            writer.Write(screenHeight);

            writer.Write((int)gridMode);

            writeChunk(writer, exportSettings);

            writer.Write(seTextInput.ToByteArray());
            writer.Write(seTextDelete.ToByteArray());
            writer.Write("");
            writer.Write("");
            writer.Write("");
            writer.Write("");

            writer.Write(glossary.inventoryFull);
            writer.Write(glossary.inventoryWitchItemThrowAway);
            writer.Write(glossary.inventoryAllOfStackThrowAway);
            writer.Write(glossary.inventoryAbandonItem);
            writer.Write(inventoryMax);

            // 文字列変数名を保存
            writer.Write(strVariableNames.ToList().Count(x => x != null));
            for (int i = 0; i < MAX_STR_VARIABLE; i++)
            {
                if (strVariableNames[i] != null)
                {
                    writer.Write(i);
                    writer.Write(strVariableNames[i]);
                }
            }

            writer.Write(glossary.inventoryFullRemoveEquip);
            
            writer.Write(defaultBgmVolume);
            writer.Write(defaultSeVolume);

            writer.Write(gameFontUseOldMethod);
            writeChunk(writer, systemGraphics);

            // バトルイベントを保存
            writer.Write(battleEvents.Count);
            foreach (var guid in battleEvents)
            {
                writer.Write(guid.ToByteArray());
            }

            writer.Write(glossary.nowLoading);

            // ダメージカラーを保存
            writer.Write(damageNumColors.Length);
            foreach (var color in damageNumColors)
            {
                writer.Write(color.R);
                writer.Write(color.G);
                writer.Write(color.B);
                writer.Write(color.A);
            }
        }

        public override void load(System.IO.BinaryReader reader)
        {
            base.load(reader);

            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var guid = Util.readGuid(reader);
                party.Add(guid);
            }

            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var stack = new ItemStack();
                stack.guid = Util.readGuid(reader);
                stack.num = reader.ReadInt32();
                items.Add(stack);
            }

            money = reader.ReadInt32();
            startX = reader.ReadInt32();
            startY = reader.ReadInt32();
            startMap = Util.readGuid(reader);

            // スイッチ名を読み込み
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var index = reader.ReadInt32();
                if (index < 0 || index >= MAX_SWITCH) break;
                switchNames[index] = reader.ReadString();
            }
            // 変数名を読み込み
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var index = reader.ReadInt32();
                if (index < 0 || index >= MAX_VARIABLE) break;
                variableNames[index] = reader.ReadString();
            }

            messageWindow = Util.readGuid(reader);
            selectedBox = Util.readGuid(reader);
            worldMapCursor = Util.readGuid(reader);
            pageIcon = Util.readGuid(reader);

            scrollIcon = Util.readGuid(reader);
            equipEffectIcon = Util.readGuid(reader);

            glossary.moneyName = reader.ReadString();
            glossary.innOK = reader.ReadString();
            glossary.innCancel = reader.ReadString();
            glossary.notEnoughMoney = reader.ReadString();
            glossary.buy = reader.ReadString();
            glossary.sell = reader.ReadString();
            glossary.shopCancel = reader.ReadString();
            glossary.tradeComplete = reader.ReadString();
            glossary.sellComplete = reader.ReadString();

            int attrCount = reader.ReadInt32();
            for (int i = 0; i < attrCount; i++)
            {
                glossary.attrNames[i] = reader.ReadString();
            }

            route = Util.readGuid(reader);

            glossary.battle_result = reader.ReadString();

            glossary.saved = reader.ReadString();
            glossary.askOverwrite = reader.ReadString();
            glossary.askExitGame = reader.ReadString();

            glossary.decide = reader.ReadString();
            glossary.moveCursor = reader.ReadString();
            glossary.changeCharacter = reader.ReadString();
            glossary.changeSetting = reader.ReadString();

            glossary.close = reader.ReadString();
            glossary.dexterity = reader.ReadString();
            glossary.skillCostStr = reader.ReadString();
            glossary.noItem = reader.ReadString();
            glossary.expendableItem = reader.ReadString();
            glossary.weapon = reader.ReadString();
            glossary.attackPower = reader.ReadString();
            glossary.armor = reader.ReadString();
            glossary.defense = reader.ReadString();
            glossary.shield = reader.ReadString();
            glossary.evasion = reader.ReadString();
            glossary.head = reader.ReadString();
            glossary.accessory = reader.ReadString();
            glossary.holdNum = reader.ReadString();
            glossary.item = reader.ReadString();
            glossary.skill = reader.ReadString();
            glossary.equipment = reader.ReadString();
            glossary.status = reader.ReadString();
            glossary.save = reader.ReadString();
            glossary.exit = reader.ReadString();
            glossary.use = reader.ReadString();
            glossary.throwaway = reader.ReadString();
            glossary.level = reader.ReadString();
            glossary.hp = reader.ReadString();
            glossary.mp = reader.ReadString();
            glossary.ailments = reader.ReadString();
            glossary.normal = reader.ReadString();
            glossary.poison = reader.ReadString();
            glossary.dead = reader.ReadString();
            glossary.skillUser = reader.ReadString();
            glossary.noSkill = reader.ReadString();
            glossary.equipTarget = reader.ReadString();
            glossary.removeItem = reader.ReadString();
            glossary.nothing = reader.ReadString();
            glossary.showStatus = reader.ReadString();
            glossary.power = reader.ReadString();
            glossary.vitarity = reader.ReadString();
            glossary.magic = reader.ReadString();
            glossary.speed = reader.ReadString();
            glossary.exp = reader.ReadString();
            glossary.nextLevel = reader.ReadString();
            glossary.gameover = reader.ReadString();

            battleBgm = Util.readGuid(reader);

            glossary.battle_attacktarget = reader.ReadString();
            glossary.battle_target = reader.ReadString();
            glossary.battle_wait = reader.ReadString();
            glossary.battle_sleep = reader.ReadString();
            glossary.battle_paralysis = reader.ReadString();
            glossary.battle_critical = reader.ReadString();
            glossary.battle_guard = reader.ReadString();
            glossary.battle_charge = reader.ReadString();
            glossary.battle_attack = reader.ReadString();
            glossary.battle_skill = reader.ReadString();
            glossary.battle_item = reader.ReadString();
            glossary.battle_start = reader.ReadString();
            glossary.battle_win = reader.ReadString();
            glossary.battle_lose = reader.ReadString();
            glossary.battle_escape = reader.ReadString();
            glossary.battle_get_money = reader.ReadString();
            glossary.battle_get_item = reader.ReadString();
            glossary.battle_get_exp = reader.ReadString();
            glossary.battle_levelup = reader.ReadString();
            glossary.battle_escape_command = reader.ReadString();
            glossary.battle_cancel = reader.ReadString();
            glossary.battle_miss = reader.ReadString();
            glossary.battle_back = reader.ReadString();

            battleWinBgm = Util.readGuid(reader);
            gameoverBgm = Util.readGuid(reader);

            seBuy = Util.readGuid(reader);
            seCancel = Util.readGuid(reader);
            seDecide = Util.readGuid(reader);
            seSelect = Util.readGuid(reader);
            seItem = Util.readGuid(reader);
            seSkill = seItem;

            seLevelUp = Util.readGuid(reader);
            menuWindow = Util.readGuid(reader);
            choicesWindow = Util.readGuid(reader);

            readChunk(reader, title);
            title.gs = this;

            glossary.yes = reader.ReadString();
            glossary.no = reader.ReadString();
            description = reader.ReadString();

            glossary.message_moment = reader.ReadString();
            glossary.message_fast = reader.ReadString();
            glossary.message_normal = reader.ReadString();
            glossary.message_slow = reader.ReadString();
            glossary.config_memory = reader.ReadString();
            glossary.config_not_memory = reader.ReadString();

            glossary.bgm = reader.ReadString();
            glossary.se = reader.ReadString();
            glossary.message_speed = reader.ReadString();
            glossary.cursor_position = reader.ReadString();
            glossary.restore_defaults = reader.ReadString();
            glossary.config = reader.ReadString();
            glossary.elementAttackPower = reader.ReadString();

            int dlcCount = reader.ReadInt32();
            dlcList.Clear();
            for (int i = 0; i < dlcCount; i++)
            {
                var guid = Util.readGuid(reader);
                if (!dlcList.Contains(guid))
                    dlcList.Add(guid);
            }

            glossary.money = reader.ReadString();
            glossary.playTime = reader.ReadString();
            subTitle = reader.ReadString();
            glossary.battle_result_continue = reader.ReadString();
            glossary.battle_poison = reader.ReadString();
            glossary.battle_confusion = reader.ReadString();
            glossary.battle_fascination = reader.ReadString();
            glossary.battle_enemy_escape = reader.ReadString();

            glossary.battle_recover_confusion = reader.ReadString();
            glossary.battle_recover_dead = reader.ReadString();
            glossary.battle_recover_fascination = reader.ReadString();
            glossary.battle_recover_paralysis = reader.ReadString();
            glossary.battle_recover_poison = reader.ReadString();
            glossary.battle_recover_sleep = reader.ReadString();

            glossary.battle_escape_failed = reader.ReadString();
            glossary.battle_lv = reader.ReadString();
            glossary.battle_exp = reader.ReadString();

            seEscape = Util.readGuid(reader);
            seDefeat = Util.readGuid(reader);
            innJingle = Util.readGuid(reader);

            gameFont = reader.ReadString();

            glossary.saveData = reader.ReadString();

            readChunk(reader, tpCamera);
            readChunk(reader, fpCamera);

            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var preset = new CameraPreset();
                preset.name = reader.ReadString();
                preset.tpPreset = new ThirdPersonCameraSettings();
                readChunk(reader, preset.tpPreset);
                tpCameraPresets.Add(preset);
            }
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var preset = new CameraPreset();
                preset.name = reader.ReadString();
                var hasTp = reader.ReadBoolean();
                if (hasTp)
                {
                    preset.tpPreset = new ThirdPersonCameraSettings();
                    readChunk(reader, preset.tpPreset);
                }
                preset.fpPreset = new FirstPersonCameraSettings();
                readChunk(reader, preset.fpPreset);
                fpCameraPresets.Add(preset);
            }

            controlMode = (ControlMode)reader.ReadInt32();

            battleType = (BattleType)reader.ReadInt32();

            if (reader.BaseStream.Length != reader.BaseStream.Position)
                seSkill = Util.readGuid(reader);

            if (reader.BaseStream.Length != reader.BaseStream.Position)
            {
                escapeIcon.load(reader);
                returnIcon.load(reader);
                newItemIcon.load(reader);
            }

            readChunk(reader, battleCamera);

            vrEnabled = reader.ReadBoolean();

            // コモンイベントを読み込み
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                commonEvents.Add(Util.readGuid(reader));
            }

            // DLCのエクスポートを許可するかどうかを読み込み
            allowExport = reader.ReadBoolean();
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                notAllowExportResources.Add(Util.readGuid(reader));
            }

            screenWidth = reader.ReadInt32();
            screenHeight = reader.ReadInt32();

            gridMode = (GridMode)reader.ReadInt32();

            readChunk(reader, exportSettings);

            seTextInput = Util.readGuid(reader);
            seTextDelete = Util.readGuid(reader);

            reader.ReadString();
            reader.ReadString();
            reader.ReadString();
            reader.ReadString();

            glossary.inventoryFull = reader.ReadString();
            glossary.inventoryWitchItemThrowAway = reader.ReadString();
            glossary.inventoryAllOfStackThrowAway = reader.ReadString();
            glossary.inventoryAbandonItem = reader.ReadString();
            inventoryMax = reader.ReadInt32();

            // 文字列変数名を読み込み
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var index = reader.ReadInt32();
                if (index < 0 || index >= MAX_STR_VARIABLE) break;
                strVariableNames[index] = reader.ReadString();
            }

            glossary.inventoryFullRemoveEquip = reader.ReadString();

            defaultBgmVolume = reader.ReadInt32();
            defaultSeVolume = reader.ReadInt32();

            gameFontUseOldMethod = reader.ReadBoolean();
            readChunk(reader, systemGraphics);
            
            // バトルイベントを読み込み
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                battleEvents.Add(Util.readGuid(reader));
            }

            glossary.nowLoading = reader.ReadString();

            // ダメージカラーを読み込み
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                damageNumColors[i] = new Color(
                    reader.ReadByte(),
                    reader.ReadByte(),
                    reader.ReadByte(),
                    reader.ReadByte()
                );
            }
        }

        public void resetSwVarNames()
        {
            switchNames = new string[MAX_SWITCH];
            variableNames = new string[MAX_VARIABLE];
            strVariableNames = new string[MAX_STR_VARIABLE];
        }
    }
}

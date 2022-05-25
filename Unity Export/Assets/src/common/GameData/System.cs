#if WINDOWS
#else
using Eppy;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yukar.Common.GameData
{
    public class SystemData : IGameDataItem
    {
        public Dictionary<Tuple<Guid, int>, bool> LocalSwitches = new Dictionary<Tuple<Guid, int>, bool>();
        public Dictionary<Tuple<Guid, int>, int> LocalVariables = new Dictionary<Tuple<Guid, int>, int>();
        public bool[] Switches = new bool[Rom.GameSettings.MAX_SWITCH];
        public int[] Variables = new int[Rom.GameSettings.MAX_VARIABLE];
        public string[] StrVariables = new string[MAX_STRING_VARIABLE];
        public bool menuAvailable = true;
        public bool encountAvailable = true;
        public bool saveAvailable = true;
        public bool dashAvailable = true;
        public static int sDefaultBgmVolume;
        public static int sDefaultSeVolume;
        public int bgmVolume = sDefaultBgmVolume;
        public int seVolume = sDefaultSeVolume;
        public float playTime = 0;
        public const int MAX_LOCAL_VARIABLE = 10;
        public const int MAX_STRING_VARIABLE = 256;

        public const int BATTLE_CAMERA_SITUATION_START = 0;
        public const int BATTLE_CAMERA_SITUATION_ATTACK = 1;
        public const int BATTLE_CAMERA_SITUATION_SKILL = 2;
        public const int BATTLE_CAMERA_SITUATION_ITEM = 3;
        public const int BATTLE_CAMERA_SITUATION_RESULT = 4;
        public bool[] BattleCameraEnabled = new bool[] { true, true, true, true, true };
        public enum MessageSpeed
        {
            IMMEDIATE,
            FAST,
            NORMAL,
            SLOW,
        };
        public MessageSpeed messageSpeed = MessageSpeed.FAST;
        public enum CursorPosition
        {
            KEEP,
            NOT_KEEP,
        }
        public CursorPosition cursorPosition = CursorPosition.KEEP;
        public enum ControlType
        {
            KEYBOARD_AND_GAMEPAD,
            MOUSE_TOUCH,
        }
        public ControlType controlType = ControlType.KEYBOARD_AND_GAMEPAD;
        public int moveSpeed = -1;

        public void SetSwitch(int index, bool value, Guid eventGuid)
        {
            if (index >= Rom.GameSettings.LOCAL_SWITCH_OFFSET &&
                index < Rom.GameSettings.LOCAL_SWITCH_OFFSET + MAX_LOCAL_VARIABLE)
            {
                // ローカルスイッチ
                LocalSwitches[new Tuple<Guid, int>(eventGuid, index)] = value;
            }
            else if (index >= 0 && index < Rom.GameSettings.MAX_SWITCH)
            {
                // 普通のスイッチ
                Switches[index] = value;
            }
        }
        public void SetVariable(int index, int value, Guid eventGuid)
        {
            if (index >= Rom.GameSettings.LOCAL_SWITCH_OFFSET &&
                index < Rom.GameSettings.LOCAL_SWITCH_OFFSET + MAX_LOCAL_VARIABLE)
            {
                // ローカル変数
                if(index < Rom.GameSettings.LOCAL_SWITCH_OFFSET + MAX_LOCAL_VARIABLE)
                    LocalVariables[new Tuple<Guid, int>(eventGuid, index)] = value;
            }
            else if (index >= 0 && index < Rom.GameSettings.MAX_VARIABLE)
            {
                // 普通の変数
                Variables[index] = value;
            }
        }
        public bool GetSwitch(int index, Guid eventGuid)
        {
            if (index >= Rom.GameSettings.LOCAL_SWITCH_OFFSET &&
                index < Rom.GameSettings.LOCAL_SWITCH_OFFSET + MAX_LOCAL_VARIABLE)
            {
                // ローカルスイッチ
                var key = new Tuple<Guid, int>(eventGuid, index);
                if(LocalSwitches.ContainsKey(key))
                    return LocalSwitches[key];

                return false;
            }
            else if (index >= 0 && index < Rom.GameSettings.MAX_SWITCH)
            {
                return Switches[index];
            }

            return false;
        }
        public int GetVariable(int index, Guid eventGuid)
        {
            if (index >= Rom.GameSettings.LOCAL_SWITCH_OFFSET &&
                index < Rom.GameSettings.LOCAL_SWITCH_OFFSET + MAX_LOCAL_VARIABLE)
            {
                // ローカル変数
                var key = new Tuple<Guid, int>(eventGuid, index);
                if (LocalVariables.ContainsKey(key))
                    return LocalVariables[key];

                return 0;
            }
            else if(index >= 0 && index < Rom.GameSettings.MAX_VARIABLE)
            {
                return Variables[index];
            }

            return 0;
        }

        void IGameDataItem.save(System.IO.BinaryWriter writer)
        {
            writer.Write(Switches.Length);
            foreach (var sw in Switches)
            {
                writer.Write(sw);
            }
            writer.Write(Variables.Length);
            foreach (var va in Variables)
            {
                writer.Write(va);
            }
            writer.Write(menuAvailable);
            writer.Write(encountAvailable);

            writer.Write(bgmVolume);
            writer.Write(seVolume);
            writer.Write((int)messageSpeed);
            writer.Write((int)cursorPosition);
            writer.Write((int)controlType);

            writer.Write(LocalSwitches.Count);
            foreach (var sw in LocalSwitches)
            {
                writer.Write(sw.Key.Item1.ToByteArray());
                writer.Write(sw.Key.Item2);
                writer.Write(sw.Value);
            }

            writer.Write(playTime);
            writer.Write(saveAvailable);
            writer.Write(dashAvailable);
            writer.Write(moveSpeed);

            writer.Write(LocalVariables.Count);
            foreach (var sw in LocalVariables)
            {
                writer.Write(sw.Key.Item1.ToByteArray());
                writer.Write(sw.Key.Item2);
                writer.Write(sw.Value);
            }

            writer.Write(StrVariables.Length);
            foreach (var sv in StrVariables)
            {
                if (sv == null)
                    writer.Write("");
                else
                    writer.Write(sv);
            }

            writer.Write(BattleCameraEnabled.Length);
            foreach (var sw in BattleCameraEnabled)
            {
                writer.Write(sw);
            }
        }

        void IGameDataItem.load(Catalog catalog, System.IO.BinaryReader reader)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                Switches[i] = reader.ReadBoolean();
            }
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                Variables[i] = reader.ReadInt32();
            }
            menuAvailable = reader.ReadBoolean();
            encountAvailable = reader.ReadBoolean();

            bgmVolume = reader.ReadInt32();
            seVolume = reader.ReadInt32();
            messageSpeed = (MessageSpeed)reader.ReadInt32();
            cursorPosition = (CursorPosition)reader.ReadInt32();
            controlType = (ControlType)reader.ReadInt32();

            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var guid = Util.readGuid(reader);
                int index = reader.ReadInt32();
                bool value = reader.ReadBoolean();
                LocalSwitches[new Tuple<Guid, int>(guid, index)] = value;
            }

            playTime = reader.ReadSingle();
            saveAvailable = reader.ReadBoolean();
            dashAvailable = reader.ReadBoolean();
            moveSpeed = reader.ReadInt32();

            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var guid = Util.readGuid(reader);
                int index = reader.ReadInt32();
                int value = reader.ReadInt32();
                LocalVariables[new Tuple<Guid, int>(guid, index)] = value;
            }

            count = reader.ReadInt32();
            for (int i = 0; i < count && i < StrVariables.Length; i++)
            {
                StrVariables[i] = reader.ReadString();
            }

            // 古いデータで音量がDEFAULT_VOLUMEを越えている場合はDEFAULT_VOLUMEを最大にする
            if (GameDataManager.sRecentDataVersion < 1)
            {
                adjustVolume();
            }

            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                BattleCameraEnabled[i] = reader.ReadBoolean();
            }
        }

        public void adjustVolume()
        {
            if (bgmVolume > sDefaultBgmVolume)
            {
                bgmVolume = sDefaultBgmVolume;
            }

            if (seVolume > sDefaultSeVolume)
            {
                seVolume = sDefaultSeVolume;
            }
        }

        public void restoreDefaults()
        {
            bgmVolume = sDefaultBgmVolume;
            seVolume = sDefaultSeVolume;
            messageSpeed = MessageSpeed.FAST;
            cursorPosition = CursorPosition.KEEP;
            controlType = ControlType.KEYBOARD_AND_GAMEPAD;
        }

        public string GetPlayTime()
        {
            int hour = getHour();
            int minute = getMinute();
            int second = getSecond();
            return hour.ToString("00") + ":" + minute.ToString("00") + ":" + second.ToString("00");
        }

        public int getSecond()
        {
            return (int)playTime - getMinute() * 60 - getHour() * 60 * 60;
        }

        public int getMinute()
        {
            return (int)playTime / 60 - getHour() * 60;
        }

        public int getHour()
        {
            return (int)playTime / 60 / 60;
        }

        public void update(float elapsed)
        {
            playTime += elapsed;
        }

        internal void copyConfigFrom(SystemData old)
        {
            messageSpeed = old.messageSpeed;
            cursorPosition = old.cursorPosition;
            controlType = old.controlType;
            bgmVolume = old.bgmVolume;
            seVolume = old.seVolume;
        }
    }
}

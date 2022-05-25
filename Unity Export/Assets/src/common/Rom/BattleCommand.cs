using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yukar.Common.Rom
{
    public class BattleCommand : RomItem
    {
        public enum CommandType
        {
            ATTACK,
            CHARGE,
            GUARD,
            SKILL,
            SKILLMENU,
            ITEMMENU,
            ESCAPE,
            BACK,
        }

        public string description = "";
        public Common.Resource.Icon.Ref icon;
        public CommandType type;
        public int turn;
        public int skillType = 1;
        public int power = 50;
        public int percent;
        public Guid refGuid;

        public BattleCommand()
        {
            icon = new Resource.Icon.Ref();
            turn = 100;
            skillType = 1;
        }

        public override void save(System.IO.BinaryWriter writer)
        {
            base.save(writer);

            writer.Write(description);
            icon.save(writer);
            writer.Write((int)type);
            writer.Write(turn);
            writer.Write(skillType);
            writer.Write(power);
            writer.Write(percent);
            writer.Write(refGuid.ToByteArray());
        }

        public override void load(System.IO.BinaryReader reader)
        {
            base.load(reader);

            description = reader.ReadString();
            icon.load(reader);
            type = (CommandType)reader.ReadInt32();
            turn = reader.ReadInt32();
            skillType = reader.ReadInt32();
            power = reader.ReadInt32();
            percent = reader.ReadInt32();
            refGuid = Util.readGuid(reader);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yukar.Common.Rom
{
    public class Hero : RomItem
    {
        public Guid graphic;
        public Guid face;
        public string description = "";

        public int level = 1;   // 初期レベル
        public int power = 10;
        public float powerGrowth = 2f;
        public float powerGrowthRate = 1.01f;
        public int vitality = 10;
        public float vitalityGrowth = 2f;
        public float vitalityGrowthRate = 1.01f;
        public int magic = 10;
        public float magicGrowth = 2f;
        public float magicGrowthRate = 1.01f;
        public int speed = 10;
        public float speedGrowth = 2f;
        public float speedGrowthRate = 1.01f;
        public int levelGrowthRate = 2;

        public int poisonDamegePercent = 10;

        public List<SkillLearnLevel> skillLearnLevelsList = new List<SkillLearnLevel>();
        public Dictionary<Guid, bool> availableItemsList = new Dictionary<Guid, bool>();
        public List<Guid> battleCommandList = new List<Guid>();

        public Equipments equipments = new Equipments();
        public bool[] fixEquipments = new bool[6];

        public bool isAutoBattle;
        public bool isLevelFixed;

        public int hp = 30;
        public float hpGrowth = 5f;
        public float hpGrowthRate = 1.01f;
        public int mp = 10;
        public float mpGrowth = 2f;
        public float mpGrowthRate = 1.01f;
        public bool moveForward = true;

        public class SkillLearnLevel
        {
            public Guid skill;
            public int level;

            public SkillLearnLevel(SkillLearnLevel copyFrom)
            {
                skill = copyFrom.skill;
                level = copyFrom.level;
            }

            public SkillLearnLevel()
            {
                level = 1;
            }
        }

        public class AvailableItem
        {
            public Guid item;
            public bool isAvailable;
        }

        public class Equipments
        {
            public Guid weapon;
            public Guid shield;
            public Guid head;
            public Guid body;
            public Guid[] accessory = new Guid[2];
        }

        public override void save(System.IO.BinaryWriter writer)
        {
            base.save(writer);

            writer.Write(graphic.ToByteArray());

            writer.Write(level);
            writer.Write(power);
            writer.Write(powerGrowth);
            writer.Write(powerGrowthRate);
            writer.Write(vitality);
            writer.Write(vitalityGrowth);
            writer.Write(vitalityGrowthRate);
            writer.Write(magic);
            writer.Write(magicGrowth);
            writer.Write(magicGrowthRate);
            writer.Write(speed);
            writer.Write(speedGrowth);
            writer.Write(speedGrowthRate);

            writer.Write(skillLearnLevelsList.Count);
            foreach (var item in skillLearnLevelsList)
            {
                writer.Write(item.skill.ToByteArray());
                writer.Write(item.level);
            }

            writer.Write(availableItemsList.Count);
            foreach (var item in availableItemsList)
            {
                writer.Write(item.Key.ToByteArray());
                writer.Write(item.Value);
            }

            writer.Write(equipments.weapon.ToByteArray());
            writer.Write(equipments.shield.ToByteArray());
            writer.Write(equipments.head.ToByteArray());
            writer.Write(equipments.body.ToByteArray());
            writer.Write(equipments.accessory[0].ToByteArray());

            writer.Write(isAutoBattle);
            writer.Write(isLevelFixed);

            writer.Write(battleCommandList.Count);
            foreach (var guid in battleCommandList)
            {
                writer.Write(guid.ToByteArray());
            }

            writer.Write(equipments.accessory[1].ToByteArray());
            writer.Write(description);

            writer.Write(face.ToByteArray());

            writer.Write(hp);
            writer.Write(hpGrowth);
            writer.Write(hpGrowthRate);
            writer.Write(mp);
            writer.Write(mpGrowth);
            writer.Write(mpGrowthRate);
            foreach (var fix in fixEquipments)
            {
                writer.Write(fix);
            }

            writer.Write(levelGrowthRate);

            writer.Write(poisonDamegePercent);
            writer.Write(moveForward);
        }

        public override void load(System.IO.BinaryReader reader)
        {
            base.load(reader);

            graphic = Util.readGuid(reader);

            level = reader.ReadInt32();
            power = reader.ReadInt32();
            powerGrowth = reader.ReadSingle();
            powerGrowthRate = reader.ReadSingle();
            vitality = reader.ReadInt32();
            vitalityGrowth = reader.ReadSingle();
            vitalityGrowthRate = reader.ReadSingle();
            magic = reader.ReadInt32();
            magicGrowth = reader.ReadSingle();
            magicGrowthRate = reader.ReadSingle();
            speed = reader.ReadInt32();
            speedGrowth = reader.ReadSingle();
            speedGrowthRate = reader.ReadSingle();

            int dataNum = reader.ReadInt32();
            skillLearnLevelsList.Clear();
            for (int i = 0; i < dataNum; i++)
            {
                var item = new SkillLearnLevel();
                item.skill = Util.readGuid(reader);
                item.level = reader.ReadInt32();
                skillLearnLevelsList.Add(item);
            }

            dataNum = reader.ReadInt32();
            availableItemsList.Clear();
            for (int i = 0; i < dataNum; i++)
            {
                availableItemsList.Add(Util.readGuid(reader), reader.ReadBoolean());
            }

            equipments.weapon = Util.readGuid(reader);
            equipments.shield = Util.readGuid(reader);
            equipments.head = Util.readGuid(reader);
            equipments.body = Util.readGuid(reader);
            equipments.accessory[0] = Util.readGuid(reader);

            isAutoBattle = reader.ReadBoolean();
            isLevelFixed = reader.ReadBoolean();

            dataNum = reader.ReadInt32();
            battleCommandList.Clear();
            for (int i = 0; i < dataNum; i++)
            {
                var guid = Util.readGuid(reader);
                battleCommandList.Add(guid);
            }

            equipments.accessory[1] = Util.readGuid(reader);
            description = reader.ReadString();

            face = Util.readGuid(reader);

            hp = reader.ReadInt32();
            hpGrowth = reader.ReadSingle();
            hpGrowthRate = reader.ReadSingle();
            mp = reader.ReadInt32();
            mpGrowth = reader.ReadSingle();
            mpGrowthRate = reader.ReadSingle();

            for (int i = 0; i < fixEquipments.Length; i++)
            {
                fixEquipments[i] = reader.ReadBoolean();
            }

            levelGrowthRate = reader.ReadInt32();

            poisonDamegePercent = reader.ReadInt32();
            moveForward = reader.ReadBoolean();
        }
    }
}

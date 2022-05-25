using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yukar.Common.Rom
{
    public class Monster : RomItem
    {
        public enum AIPattern
        {
            NORMAL,     // ランダム選択
            CLEVER,     // MPが足りない行動は行わない
            TRICKY,     // MPが足りない行動は行わない ＆ 一撃で殺せる対象を狙う
        }

        public enum ActionType
        {
            ATTACK,
            CRITICAL,
            DO_NOTHING,
            SKILL,
            GUARD,
            CHARGE,
            ESCAPE,
        }

        public Guid graphic;
        public Guid attackEffect;
        public string description = "";
        public int hitpoint = 1;
        public int magicpoint;
        public int attack;
        public int magic = 0;
        public int defense;
        public int evasion;
        public int dexterity = 100;

        //---------------------------------
        // 使わないですが一応予約
        public int power = 1;
        public int vitarity = 1;
        //---------------------------------

        public int speed = 1;
        public int attrADefense;
        public int attrBDefense;
        public int attrCDefense;
        public int attrDDefense;
        public int attrEDefense;
        public int attrFDefense;
        public int poisonResistant;
        public int sleepResistant;
        public int paralysisResistant;
        public int confusionResistant;
        public int fascinationResistant;
        public int deathResistant;

        public int poisonDamegePercent = 10;

        public AIPattern aiPattern;

        public class ActionPattern : RomItem
        {
            public int turn = 1;
            public int minHPPercent;
            public int maxHPPercent = 100;
            public ActionType action;
            public Guid refByAction;
            public int option = 50;

            public override void save(System.IO.BinaryWriter writer)
            {
                writer.Write(turn);
                writer.Write(minHPPercent);
                writer.Write(maxHPPercent);
                writer.Write((int)action);
                writer.Write(refByAction.ToByteArray());
                writer.Write(option);
            }

            public override void load(System.IO.BinaryReader reader)
            {
                turn = reader.ReadInt32();
                minHPPercent = reader.ReadInt32();
                maxHPPercent = reader.ReadInt32();
                action = (ActionType)reader.ReadInt32();
                refByAction = Util.readGuid(reader);
                option = reader.ReadInt32();
            }
        }

        public List<ActionPattern> actionList = new List<ActionPattern>();

        public int money;
        public int exp;
        public Guid dropItemA;
        public int dropItemAPercent;
        public Guid dropItemB;
        public int dropItemBPercent;
        public int encountType = 1;
        public bool moveForward = true;

        public override void save(System.IO.BinaryWriter writer)
        {
            base.save(writer);

            writer.Write(graphic.ToByteArray());
            writer.Write(attackEffect.ToByteArray());

            writer.Write(description);
            writer.Write(hitpoint);
            writer.Write(magicpoint);
            writer.Write(attack);
            writer.Write(defense);
            writer.Write(evasion);
            writer.Write(dexterity);
            writer.Write(power);
            writer.Write(vitarity);
            writer.Write(magic);
            writer.Write(speed);
            writer.Write(attrADefense);
            writer.Write(attrBDefense);
            writer.Write(attrCDefense);
            writer.Write(attrDDefense);
            writer.Write(attrEDefense);
            writer.Write(attrFDefense);
            writer.Write(poisonResistant);
            writer.Write(sleepResistant);
            writer.Write(paralysisResistant);
            writer.Write(confusionResistant);
            writer.Write(fascinationResistant);
            writer.Write(deathResistant);
            writer.Write((int)aiPattern);

            // 行動パターンをセーブ
            writer.Write(actionList.Count);
            foreach (var item in actionList)
            {
                RomItem.writeChunk(writer, item);
            }

            writer.Write(money);
            writer.Write(exp);
            writer.Write(dropItemA.ToByteArray());
            writer.Write(dropItemAPercent);
            writer.Write(dropItemB.ToByteArray());
            writer.Write(dropItemBPercent);
            writer.Write(encountType);

            writer.Write(poisonDamegePercent);
            writer.Write(moveForward);
        }

        public override void load(System.IO.BinaryReader reader)
        {
            base.load(reader);

            graphic = Util.readGuid(reader);
            attackEffect = Util.readGuid(reader);
            description = reader.ReadString();
            hitpoint = reader.ReadInt32();
            magicpoint = reader.ReadInt32();
            attack = reader.ReadInt32();
            defense = reader.ReadInt32();
            evasion = reader.ReadInt32();
            dexterity = reader.ReadInt32();
            power = reader.ReadInt32();
            vitarity = reader.ReadInt32();
            magic = reader.ReadInt32();
            speed = reader.ReadInt32();
            attrADefense = reader.ReadInt32();
            attrBDefense = reader.ReadInt32();
            attrCDefense = reader.ReadInt32();
            attrDDefense = reader.ReadInt32();
            attrEDefense = reader.ReadInt32();
            attrFDefense = reader.ReadInt32();
            poisonResistant = reader.ReadInt32();
            sleepResistant = reader.ReadInt32();
            paralysisResistant = reader.ReadInt32();
            confusionResistant = reader.ReadInt32();
            fascinationResistant = reader.ReadInt32();
            deathResistant = reader.ReadInt32();
            aiPattern = (AIPattern)reader.ReadInt32();

            // 行動パターンを読み込み
            if (Catalog.sRomVersion == 0)
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    var item = new ActionPattern();
                    item.turn = reader.ReadInt32();
                    item.minHPPercent = reader.ReadInt32();
                    item.maxHPPercent = reader.ReadInt32();
                    item.action = (ActionType)reader.ReadInt32();
                    item.refByAction = Util.readGuid(reader);
                    actionList.Add(item);
                }
            }
            else
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    var item = new ActionPattern();
                    RomItem.readChunk(reader, item);
                    actionList.Add(item);
                }
            }

            money = reader.ReadInt32();
            exp = reader.ReadInt32();
            dropItemA = Util.readGuid(reader);
            dropItemAPercent = reader.ReadInt32();
            dropItemB = Util.readGuid(reader);
            dropItemBPercent = reader.ReadInt32();
            encountType = reader.ReadInt32();

            poisonDamegePercent = reader.ReadInt32();
            moveForward = reader.ReadBoolean();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yukar.Common.Rom
{
    public enum ItemType
    {
        NO_EFFECT,              // 効果なし
        EXPENDABLE,             // 消耗品
        EXPENDABLE_WITH_SKILL,  // 消耗品(スキルと同じ効果)
        WEAPON,                 // 武器
        SHIELD,                 // 盾
        HEAD,                   // 頭防具
        ARMOR,                  // 体防具
        ACCESSORY,              // 装飾品
    }

    public class Item : RomItem
    {
        public String description = "";
        public int price = 1;
        public Resource.Icon.Ref icon;
        public ItemType type;
        public Guid model;
        public int maxNum = 99;

        public Item()
        {
            icon = new Resource.Icon.Ref();
        }

        public class Expendable : RomItem
        {
            public Guid effect;
            public int hitpoint;
            public int magicpoint;
            public int hitpointPercent;
            public int magicpointPercent;
            public int maxHitpoint;
            public int maxMagitpoint;
            public int power;
            public int vitality;
            public int magic;
            public int speed;
            public bool recoveryPoison;
            public bool recoverySleep;
            public bool recoveryParalysis;
            public bool recoveryConfusion;
            public bool recoveryFascination;
            public bool availableInField;
            public bool availableInBattle;
            public bool recoveryDown;
            public Guid commonExec;

            public override void save(System.IO.BinaryWriter writer)
            {
                writer.Write(effect.ToByteArray());
                writer.Write(hitpoint);
                writer.Write(magicpoint);
                writer.Write(hitpointPercent);
                writer.Write(magicpointPercent);
                writer.Write(maxHitpoint);
                writer.Write(maxMagitpoint);
                writer.Write(power);
                writer.Write(vitality);
                writer.Write(speed);
                writer.Write(recoveryPoison);
                writer.Write(recoverySleep);
                writer.Write(recoveryParalysis);
                writer.Write(recoveryConfusion);
                writer.Write(recoveryFascination);
                writer.Write(availableInField);
                writer.Write(availableInBattle);
                writer.Write(recoveryDown);

                writer.Write(magic);
                writer.Write(commonExec.ToByteArray());
            }

            public bool isEmptyEffect()
            {
                if (
                    hitpoint == 0 &&
                    magicpoint == 0 &&
                    hitpointPercent == 0 &&
                    magicpointPercent == 0 &&
                    maxHitpoint == 0 &&
                    maxMagitpoint == 0 &&
                    power == 0 &&
                    vitality == 0 &&
                    magic == 0 &&
                    speed == 0 &&
                    !recoveryPoison &&
                    !recoverySleep &&
                    !recoveryParalysis &&
                    !recoveryConfusion &&
                    !recoveryFascination &&
                    !recoveryDown
                )
                    return true;

                return false;
            }

            public override void load(System.IO.BinaryReader reader)
            {
                effect = Util.readGuid(reader);
                hitpoint = reader.ReadInt32();
                magicpoint = reader.ReadInt32();
                hitpointPercent = reader.ReadInt32();
                magicpointPercent = reader.ReadInt32();
                maxHitpoint = reader.ReadInt32();
                maxMagitpoint = reader.ReadInt32();
                power = reader.ReadInt32();
                vitality = reader.ReadInt32();
                speed = reader.ReadInt32();
                recoveryPoison = reader.ReadBoolean();
                recoverySleep = reader.ReadBoolean();
                recoveryParalysis = reader.ReadBoolean();
                recoveryConfusion = reader.ReadBoolean();
                recoveryFascination = reader.ReadBoolean();
                availableInField = reader.ReadBoolean();
                availableInBattle = reader.ReadBoolean();
                recoveryDown = reader.ReadBoolean();

                magic = reader.ReadInt32();
                commonExec = Util.readGuid(reader);
            }
        }
        public Expendable expendable;

        public class ExpendableWithSkill : RomItem{
            public Guid skill;

            // ↓アイテムパネルには設定項目はないですが、高速化のため、予めスキル情報から算出して入れておきます。
            public bool availableInField;
            public bool availableInBattle;

            public override void save(System.IO.BinaryWriter writer)
            {
                writer.Write(availableInField);
                writer.Write(availableInBattle);
                writer.Write(skill.ToByteArray());
            }

            public override void load(System.IO.BinaryReader reader)
            {
                availableInField = reader.ReadBoolean();
                availableInBattle = reader.ReadBoolean();
                skill = Util.readGuid(reader);
            }
        }
        public ExpendableWithSkill expendableWithSkill;

        public class Equipable : RomItem
        {
            public int defense;
            public int weight;
            public int hitpoint;
            public int magicpoint;
            public int power;
            public int vitality;
            public int magic;
            public int speed;
            public int evasion;
            public int dexterity;
            //public int magicAttack;
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
            public int magicCostPercent;
            public bool cannotRemove;
            public int critical;

            public override void save(System.IO.BinaryWriter writer)
            {
                writer.Write(defense);
                writer.Write(weight);
                writer.Write(hitpoint);
                writer.Write(magicpoint);
                writer.Write(power);
                writer.Write(vitality);
                writer.Write(magic);
                writer.Write(speed);
                writer.Write(evasion);
                writer.Write(dexterity);
                writer.Write(0/*magicAttack*/);
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
                writer.Write(magicCostPercent);
                writer.Write(cannotRemove);
                writer.Write(critical);
            }

            public override void load(System.IO.BinaryReader reader)
            {
                defense = reader.ReadInt32();
                weight = reader.ReadInt32();
                hitpoint = reader.ReadInt32();
                magicpoint = reader.ReadInt32();
                power = reader.ReadInt32();
                defense += reader.ReadInt32();  // 体力は防御力と合算する
                magic = reader.ReadInt32();
                speed = reader.ReadInt32();
                evasion = reader.ReadInt32();
                dexterity = reader.ReadInt32();
                /*magicAttack = */reader.ReadInt32();
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
                magicCostPercent = reader.ReadInt32();
                cannotRemove = reader.ReadBoolean();
                critical = reader.ReadInt32();
            }
        }
        public Equipable equipable;

        public class Weapon : RomItem
        {
            public Guid effect;
            public int attack;
            public int attribute;
            public int attrAttack;
            public string motion = "";
            public const int ATTR_POISON_INDEX = 7;
            public string formula = "";

            public override void save(System.IO.BinaryWriter writer)
            {
                writer.Write(effect.ToByteArray());
                writer.Write(attack);
                writer.Write(attribute);
                writer.Write(attrAttack);
                writer.Write(motion);
                writer.Write(formula);
            }

            public override void load(System.IO.BinaryReader reader)
            {
                effect = Util.readGuid(reader);
                attack = reader.ReadInt32();
                attribute = reader.ReadInt32();
                attrAttack = reader.ReadInt32();
                motion = reader.ReadString();
                formula = reader.ReadString();

                // データ互換処理
                if (Catalog.sRomVersion < 8)
                {
                    if (attribute >= ATTR_POISON_INDEX && attrAttack == 0)  // 毒以降で属性攻撃力がゼロだったら
                        attrAttack = 20;
                }
            }
        }
        public Weapon weapon;

        public bool IsSellable { get { return price > 0; } }

        public override void save(System.IO.BinaryWriter writer)
        {
            base.save(writer);

            writer.Write(description);
            writer.Write(price);
            icon.save(writer);
            writer.Write((int)type);

            switch(type){
                case ItemType.NO_EFFECT:
                    break;
                case ItemType.EXPENDABLE:
                    writeChunk(writer, expendable);
                    break;
                case ItemType.EXPENDABLE_WITH_SKILL:
                    writeChunk(writer, expendableWithSkill);
                    break;
                case ItemType.WEAPON:
                    writeChunk(writer, equipable);
                    writeChunk(writer, weapon);
                    break;
                case ItemType.SHIELD:
                    writeChunk(writer, equipable);
                    break;
                case ItemType.HEAD:
                    writeChunk(writer, equipable);
                    break;
                case ItemType.ARMOR:
                    writeChunk(writer, equipable);
                    break;
                case ItemType.ACCESSORY:
                    writeChunk(writer, equipable);
                    break;
            }

            writer.Write(model.ToByteArray());
            writer.Write(maxNum);
        }

        public override void load(System.IO.BinaryReader reader)
        {
            base.load(reader);

            description = reader.ReadString();
            price = reader.ReadInt32();
            icon = new Resource.Icon.Ref(reader);
            type = (ItemType)reader.ReadInt32();

            switch (type)
            {
                case ItemType.NO_EFFECT:
                    break;
                case ItemType.EXPENDABLE:
                    expendable = new Expendable();
                    readChunk(reader, expendable);
                    break;
                case ItemType.EXPENDABLE_WITH_SKILL:
                    expendableWithSkill = new ExpendableWithSkill();
                    readChunk(reader, expendableWithSkill);
                    break;
                case ItemType.WEAPON:
                    equipable = new Equipable();
                    weapon = new Weapon();
                    readChunk(reader, equipable);
                    readChunk(reader, weapon);
                    break;
                case ItemType.SHIELD:
                    equipable = new Equipable();
                    readChunk(reader, equipable);
                    break;
                case ItemType.HEAD:
                    equipable = new Equipable();
                    readChunk(reader, equipable);
                    break;
                case ItemType.ARMOR:
                    equipable = new Equipable();
                    readChunk(reader, equipable);
                    break;
                case ItemType.ACCESSORY:
                    equipable = new Equipable();
                    readChunk(reader, equipable);
                    break;
            }

            model = Util.readGuid(reader);
            maxNum = reader.ReadInt32();
        }
    }
}

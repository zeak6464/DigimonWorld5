using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yukar.Common.Rom
{
    public enum TargetType
    {
        NONE = -1,              // 対象なし
        PARTY_ONE,              // 味方一人
        PARTY_ALL,              // 味方全員
        ENEMY_ONE,              // 敵一人
        ENEMY_ALL,              // 敵全員
        SELF,                   // 自分
        OTHERS,                 // 自分以外のパーティメンバー
        ALL,                    // 敵味方全員
        SELF_ENEMY_ONE,         // 自分と敵一人
        SELF_ENEMY_ALL,         // 自分と敵全員
        OTHERS_ALL,             // 自分以外の敵味方全員
        PARTY_ONE_ENEMY_ALL,    // 味方一人と敵全員
        PARTY_ALL_ENEMY_ONE,    // 味方全員と敵一人
        OTHERS_ENEMY_ONE,       // 自分以外と敵一人
    }

    public class Skill : RomItem
    {
        public String description = "";
        public Resource.Icon.Ref icon;

        // 基本設定
        public class Option : RomItem
        {
            public bool availableInField;
            public bool availableInBattle;
            public int consumptionHitpoint;
            public int consumptionMagicpoint;
            public int group;
            public TargetType target;
            public bool drain;
            public bool selfDestruct;
            public string motion = "";
            public bool onlyForDown = true;
            public Guid commonExec;
            public Guid consumptionItem;
            public int consumptionItemAmount = 1;

            public override void save(System.IO.BinaryWriter writer)
            {
                writer.Write(availableInField);
                writer.Write(availableInBattle);
                writer.Write(consumptionHitpoint);
                writer.Write(consumptionMagicpoint);
                writer.Write((int)target);
                writer.Write(drain);
                writer.Write(selfDestruct);
                writer.Write(group);
                writer.Write(motion);
                writer.Write(onlyForDown);
                writer.Write(commonExec.ToByteArray());
                writer.Write(consumptionItem.ToByteArray());
                writer.Write(consumptionItemAmount);
            }

            public override void load(System.IO.BinaryReader reader)
            {
                availableInField = reader.ReadBoolean();
                availableInBattle = reader.ReadBoolean();
                consumptionHitpoint = reader.ReadInt32();
                consumptionMagicpoint = reader.ReadInt32();
                target = (TargetType)reader.ReadInt32();
                drain = reader.ReadBoolean();
                selfDestruct = reader.ReadBoolean();
                group = reader.ReadInt32();
                motion = reader.ReadString();
                onlyForDown = reader.ReadBoolean();
                commonExec = Util.readGuid(reader);
                consumptionItem = Util.readGuid(reader);
                consumptionItemAmount = reader.ReadInt32();
            }
        }
        public Option option; 

        // 効果
        public class SkillEffect : RomItem
        {
            public Guid effect;
            public int hitpoint;
            public int magicpoint;
            public int hitpointPercent;
            public int magicpointPercent;
            public int hitpoint_powerParcent;
            public int hitpoint_magicParcent;
            public int power;
            public int vitality;
            public int magic;
            public int speed;
            public int evation;
            public int dexterity;
            public bool poison;
            public bool sleep;
            public bool paralysis;
            public bool confusion;
            public bool fascination;
            public bool down;
            public int attrAdefense;
            public int attrBdefense;
            public int attrCdefense;
            public int attrDdefense;
            public int attrEdefense;
            public int attrFdefense;
            public int attribute;
            public int hitRate = 100;
            public string hitpointFormula = "";

            public override void save(System.IO.BinaryWriter writer)
            {
                writer.Write(effect.ToByteArray());
                writer.Write(hitpoint);
                writer.Write(magicpoint);
                writer.Write(hitpointPercent);
                writer.Write(magicpointPercent);
                writer.Write(power);
                writer.Write(vitality);
                writer.Write(magic);
                writer.Write(speed);
                writer.Write(dexterity);
                writer.Write(evation);
                writer.Write(poison);
                writer.Write(sleep);
                writer.Write(paralysis);
                writer.Write(confusion);
                writer.Write(fascination);
                writer.Write(down);
                writer.Write(attrAdefense);
                writer.Write(attrBdefense);
                writer.Write(attrCdefense);
                writer.Write(attrDdefense);
                writer.Write(attrEdefense);
                writer.Write(attrFdefense);
                writer.Write(attribute);
                writer.Write(hitRate);

                writer.Write(hitpoint_powerParcent);
                writer.Write(hitpoint_magicParcent);
                writer.Write(hitpointFormula);
            }

            public override void load(System.IO.BinaryReader reader)
            {
                effect = Util.readGuid(reader);
                hitpoint = reader.ReadInt32();
                magicpoint = reader.ReadInt32();
                hitpointPercent = reader.ReadInt32();
                magicpointPercent = reader.ReadInt32();
                power = reader.ReadInt32();
                vitality = reader.ReadInt32();
                magic = reader.ReadInt32();
                speed = reader.ReadInt32();
                dexterity = reader.ReadInt32();
                evation = reader.ReadInt32();
                poison = reader.ReadBoolean();
                sleep = reader.ReadBoolean();
                paralysis = reader.ReadBoolean();
                confusion = reader.ReadBoolean();
                fascination = reader.ReadBoolean();
                down = reader.ReadBoolean();
                attrAdefense = reader.ReadInt32();
                attrBdefense = reader.ReadInt32();
                attrCdefense = reader.ReadInt32();
                attrDdefense = reader.ReadInt32();
                attrEdefense = reader.ReadInt32();
                attrFdefense = reader.ReadInt32();
                attribute = reader.ReadInt32();
                hitRate = reader.ReadInt32();

                hitpoint_powerParcent = reader.ReadInt32();
                hitpoint_magicParcent = reader.ReadInt32();
                hitpointFormula = reader.ReadString();
            }
        }
        public SkillEffect friendEffect;
        public SkillEffect enemyEffect;

        public Skill(){
            option = new Option();
            option.group = 1;
            icon = new Resource.Icon.Ref();
        }

        public override void save(System.IO.BinaryWriter writer)
        {
            base.save(writer);

            writer.Write(description);
            icon.save(writer);

            writeChunk(writer, option);

            switch (option.target)
            {
                case TargetType.NONE:
                    break;
                case TargetType.PARTY_ONE:
                    writeChunk(writer, friendEffect);
                    break;
                case TargetType.PARTY_ALL:
                    writeChunk(writer, friendEffect);
                    break;
                case TargetType.ENEMY_ONE:
                    writeChunk(writer, enemyEffect);
                    break;
                case TargetType.ENEMY_ALL:
                    writeChunk(writer, enemyEffect);
                    break;
                case TargetType.SELF:
                    writeChunk(writer, friendEffect);
                    break;
                case TargetType.OTHERS:
                    writeChunk(writer, friendEffect);
                    break;
                default:
                    writeChunk(writer, friendEffect);
                    writeChunk(writer, enemyEffect);
                    break;
            }
        }

        public override void load(System.IO.BinaryReader reader)
        {
            base.load(reader);

            description = reader.ReadString();
            icon = new Resource.Icon.Ref(reader);

            readChunk(reader, option);

            friendEffect = null;
            enemyEffect = null;

            switch (option.target)
            {
                case TargetType.NONE:
                    break;
                case TargetType.PARTY_ONE:
                    friendEffect = new SkillEffect();
                    readChunk(reader, friendEffect);
                    break;
                case TargetType.PARTY_ALL:
                    friendEffect = new SkillEffect();
                    readChunk(reader, friendEffect);
                    break;
                case TargetType.ENEMY_ONE:
                    enemyEffect = new SkillEffect();
                    readChunk(reader, enemyEffect);
                    break;
                case TargetType.ENEMY_ALL:
                    enemyEffect = new SkillEffect();
                    readChunk(reader, enemyEffect);
                    break;
                case TargetType.SELF:
                    friendEffect = new SkillEffect();
                    readChunk(reader, friendEffect);
                    break;
                case TargetType.OTHERS:
                    friendEffect = new SkillEffect();
                    readChunk(reader, friendEffect);
                    break;
                default:
                    friendEffect = new SkillEffect();
                    readChunk(reader, friendEffect);
                    enemyEffect = new SkillEffect();
                    readChunk(reader, enemyEffect);
                    break;
            }
        }
    }
}

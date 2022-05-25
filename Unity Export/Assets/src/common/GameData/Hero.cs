using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yukar.Common.GameData
{
    public class Hero : IGameDataItem
    {
        public const int MAX_STATUS = 9999;
        public const int MIN_STATUS = 0;
        public const int MAX_ELEMENTS = 6;
        public const int MAX_STATUS_AILMENTS = 6;
        public const int MAX_LEVEL = 99;

        [FlagsAttribute]
        public enum StatusAilments
        {
            NONE = 0,
            POISON = 1,
            SLEEP = 2,
            PARALYSIS = 4,
            CONFUSION = 8,
            FASCINATION = 16,
            DOWN = 32,
        }

        public Rom.Hero rom;
        public int level;
        public int maxHitpoint;
        public int maxMagicpoint;
        public int hitpoint;
        public int magicpoint;
        public int power;
        public int vitality;
        public int magic;
        public int speed;
        public int exp;
        public StatusAilments statusAilments;
        public Rom.Item[] equipments = new Rom.Item[6];
        public List<Common.Rom.Skill> skills = new List<Rom.Skill>();

        // 装備で増強された分
        public class EquipmentEffect
        {
            // ステータス効果
            public int maxHitpoint;
            public int maxMagicpoint;
            public int hitpoint;
            public int magicpoint;
            public int power;
            public int vitality;
            public int magic;
            public int speed;

            // ステータス効果2
            public int attack;
            public int attackElement;
            public int elementAttack;
            public Guid attackEffect;

            public int defense;
            public int weight;
            public int evation;
            public int dexterity;
            //public int magicattack;
            public int[] elementDefense = new int[MAX_ELEMENTS];
            public int[] ailmentDefense = new int[MAX_STATUS_AILMENTS];
            public int reduceMagicCostPercent;
            public int critical;

            // キャラクター自身が持っているステータスとの兼ね合いでマイナスになってしまう可能性がある数値は保存する
            public void save(System.IO.BinaryWriter writer)
            {
                writer.Write(maxHitpoint);
                writer.Write(maxMagicpoint);
                writer.Write(hitpoint);
                writer.Write(magicpoint);
                writer.Write(power);
                writer.Write(vitality);
                writer.Write(speed);

                writer.Write(magic);
            }
            public void load(Catalog catalog, System.IO.BinaryReader reader)
            {
                maxHitpoint = reader.ReadInt32();
                maxMagicpoint = reader.ReadInt32();
                hitpoint = reader.ReadInt32();
                magicpoint = reader.ReadInt32();
                power = reader.ReadInt32();
                vitality = reader.ReadInt32();
                speed = reader.ReadInt32();

                magic = reader.ReadInt32();
            }
        }
        public EquipmentEffect equipmentEffect = new EquipmentEffect();

        // 戦闘時にスキルで一時的に強化された分
        public int powerEnhancement;
        public int vitalityEnhancement;
        public int magicEnhancement;
        public int speedEnhancement;
        public int[] elementDefense = new int[MAX_ELEMENTS];

        // 装備を EquipmentEffect に反映します
        // 装備を変更した時に必ず実行して下さい
        public void refreshEquipmentEffect(bool applyConsistency = true)
        {
            // まずクリアする
            maxHitpoint -= equipmentEffect.maxHitpoint;
            maxMagicpoint -= equipmentEffect.maxMagicpoint;
            power -= equipmentEffect.power;
            vitality -= equipmentEffect.vitality;
            magic -= equipmentEffect.magic;
            speed -= equipmentEffect.speed;
            equipmentEffect = calcEquipmentEffect(equipments);

            equipmentConsistency();

            // HP、MPは直接適用する
            maxHitpoint += equipmentEffect.maxHitpoint;
            maxMagicpoint += equipmentEffect.maxMagicpoint;
            power += equipmentEffect.power;
            vitality += equipmentEffect.vitality;
            magic += equipmentEffect.magic;
            speed += equipmentEffect.speed;

            // 装備テストなどの時は applyConsistency を false にして、実ステータスに影響が出ないようにする
            if (applyConsistency)
                consistency();
        }

        private static EquipmentEffect calcEquipmentEffect(Rom.Item[] equipments)
        {
            var result = new EquipmentEffect();    // クリア

            bool hasWeapon = false;

            foreach (var item in equipments)
            {
                if (item == null)
                    continue;

                if (item.equipable != null)
                {
                    var eq = item.equipable;
                    result.defense += eq.defense;
                    result.weight += eq.weight;
                    result.maxHitpoint += eq.hitpoint;
                    result.maxMagicpoint += eq.magicpoint;
                    result.power += eq.power;
                    result.vitality += eq.vitality;
                    result.magic += eq.magic;
                    result.speed += eq.speed;
                    result.evation += eq.evasion;
                    result.dexterity += eq.dexterity;
                    //resultresultelementDefense.magicattack += eq.magicAttack;
                    result.elementDefense[0] += eq.attrADefense;
                    result.elementDefense[1] += eq.attrBDefense;
                    result.elementDefense[2] += eq.attrCDefense;
                    result.elementDefense[3] += eq.attrDDefense;
                    result.elementDefense[4] += eq.attrEDefense;
                    result.elementDefense[5] += eq.attrFDefense;
                    result.ailmentDefense[0] += eq.poisonResistant;
                    result.ailmentDefense[1] += eq.sleepResistant;
                    result.ailmentDefense[2] += eq.paralysisResistant;
                    result.ailmentDefense[3] += eq.confusionResistant;
                    result.ailmentDefense[4] += eq.fascinationResistant;
                    result.ailmentDefense[5] += eq.deathResistant;
                    result.reduceMagicCostPercent += eq.magicCostPercent;
                    result.critical += eq.critical;
                }

                if (item.weapon != null)
                {
                    var wp = item.weapon;
                    result.attack += wp.attack;
                    result.attackElement = wp.attribute;
                    result.elementAttack = wp.attrAttack;
                    result.attackEffect = wp.effect;

                    hasWeapon = true;
                }
            }

            // 素手の時は命中率を強制的に95%にする
            if (!hasWeapon)
            {
                var effects = Catalog.sInstance.getFilteredItemList(typeof(Rom.Effect));
                if (effects.Count > 0)
                    result.attackEffect = effects[0].guId;
                result.dexterity += 95;
            }

            return result;
        }

        private void equipmentConsistency()
        {
            // 装備効果によって9999を超えていたら装備効果側を補正する
            if (maxHitpoint + equipmentEffect.maxHitpoint > MAX_STATUS)
                equipmentEffect.maxHitpoint = MAX_STATUS - maxHitpoint;
            if (maxMagicpoint + equipmentEffect.maxMagicpoint > MAX_STATUS)
                equipmentEffect.maxMagicpoint = MAX_STATUS - maxMagicpoint;
            if (power + equipmentEffect.power + equipmentEffect.attack > MAX_STATUS)
            {
                equipmentEffect.power = MAX_STATUS - power;
                equipmentEffect.attack = 0;
            }
            if (vitality + equipmentEffect.vitality + equipmentEffect.defense > MAX_STATUS)
            {
                equipmentEffect.vitality = MAX_STATUS - vitality;
                equipmentEffect.defense = 0;
            }
            if (speed + equipmentEffect.speed > MAX_STATUS)
                equipmentEffect.speed = MAX_STATUS - speed;
            if (magic + equipmentEffect.magic > MAX_STATUS)
                equipmentEffect.magic = MAX_STATUS - magic;

            // 装備効果によって0を下回っていたら装備効果側を補正する
            if (maxHitpoint + equipmentEffect.maxHitpoint < MIN_STATUS + 1)
                equipmentEffect.maxHitpoint = MIN_STATUS - maxHitpoint + 1;
            if (maxMagicpoint + equipmentEffect.maxMagicpoint < MIN_STATUS)
                equipmentEffect.maxMagicpoint = MIN_STATUS - maxMagicpoint;
            if (power + equipmentEffect.power + equipmentEffect.attack < MIN_STATUS)
            {
                equipmentEffect.power = MIN_STATUS - power;
                equipmentEffect.attack = 0;
            }
            if (vitality + equipmentEffect.vitality + equipmentEffect.defense < MIN_STATUS)
            {
                equipmentEffect.vitality = MIN_STATUS - vitality;
                equipmentEffect.defense = 0;
            }
            if (speed + equipmentEffect.speed < MIN_STATUS)
                equipmentEffect.speed = MIN_STATUS - speed;
            if (magic + equipmentEffect.magic < MIN_STATUS)
                equipmentEffect.magic = MIN_STATUS - magic;

            // 装備専用ステータスも 0 を下回らないようにする
            if (equipmentEffect.elementAttack < MIN_STATUS)
                equipmentEffect.elementAttack = MIN_STATUS;
            if (equipmentEffect.critical < MIN_STATUS)
                equipmentEffect.critical = MIN_STATUS;
            if (equipmentEffect.dexterity < MIN_STATUS)
                equipmentEffect.dexterity = MIN_STATUS;
            if (equipmentEffect.evation < MIN_STATUS)
                equipmentEffect.evation = MIN_STATUS;
        }

        // 経験値を増減するメソッド
        public void AddExp(int addexp, Catalog catalog)
        {
            // アイテムで上げたぶんを覚えておく
            var addHp = maxHitpoint - calcMaxHitPoint(rom);
            var addMp = maxMagicpoint - calcMaxMagicPoint(rom);
            var addPower = power - calcStatus(rom.power, rom.powerGrowth, rom.powerGrowthRate);
            var addVit = vitality - calcStatus(rom.vitality, rom.vitalityGrowth, rom.vitalityGrowthRate);
            var addMagic = magic - calcStatus(rom.magic, rom.magicGrowth, rom.magicGrowthRate);
            var addSpeed = speed - calcStatus(rom.speed, rom.speedGrowth, rom.speedGrowthRate);

            // 経験値を追加してレベルを算出する
            exp += addexp;
            if (exp < 0)
                exp = 0;
            else if (exp > Party.expTable[rom.levelGrowthRate, MAX_LEVEL - 1])
                exp = Party.expTable[rom.levelGrowthRate, MAX_LEVEL - 1];

            for (int i = 1; i <= MAX_LEVEL; i++)
            {
                level = i;

                if (exp < Party.expTable[rom.levelGrowthRate, i - 1])
                {
                    break;
                }
            }

            // スキルを反映
            int cur = 0;
            for (int i = 1; i <= level; i++)
            {
                if (rom.skillLearnLevelsList.Count == cur)
                {
                    break;
                }
                if (rom.skillLearnLevelsList[cur].level == i)
                {
                    // まだ持ってないスキルだったら追加
                    var skill = catalog.getItemFromGuid(rom.skillLearnLevelsList[cur].skill) as Common.Rom.Skill;
                    if(skill != null && !skills.Contains(skill))
                        skills.Add(skill);
                    cur++; i--;
                }
            }

            // 変化したレベルでステータスを算出して、控えておいたアイテム上昇分を足す
            maxHitpoint = Math.Min(MAX_STATUS, calcMaxHitPoint(rom) + addHp);
            maxMagicpoint = Math.Min(MAX_STATUS, calcMaxMagicPoint(rom) + addMp);
            power = Math.Min(MAX_STATUS, calcStatus(rom.power, rom.powerGrowth, rom.powerGrowthRate) + addPower);
            vitality = Math.Min(MAX_STATUS, calcStatus(rom.vitality, rom.vitalityGrowth, rom.vitalityGrowthRate) + addVit);
            magic = Math.Min(MAX_STATUS, calcStatus(rom.magic, rom.magicGrowth, rom.magicGrowthRate) + addMagic);
            speed = Math.Min(MAX_STATUS, calcStatus(rom.speed, rom.speedGrowth, rom.speedGrowthRate) + addSpeed);

            consistency();
        }

        public void SetLevel(int level, Catalog catalog)
        {
            exp = 0;

            AddExp((level >= 2) ? Party.expTable[rom.levelGrowthRate, level - 2 ] : 0, catalog);

            hitpoint = maxHitpoint;
            magicpoint = maxMagicpoint;
        }

        // パラメータの整合性を取るメソッド
        public void consistency()
        {
            // ほかのパラメータも0未満にはならない
            if (magicpoint < 0)
                magicpoint = 0;
            if (maxHitpoint < 1)
                maxHitpoint = 1;
            if (maxMagicpoint < 0)
                maxMagicpoint = 0;
            if (power < 0)
                power = 0;
            if (magic < 0)
                magic = 0;
            if (vitality < 0)
                vitality = 0;
            if (speed < 0)
                speed = 0;
            if (magic < 0)
                magic = 0;

            hitpoint = Math.Min(hitpoint, MAX_STATUS);
            magicpoint = Math.Min(magicpoint, MAX_STATUS);
            hitpoint = Math.Min(hitpoint, maxHitpoint);
            magicpoint = Math.Min(magicpoint, maxMagicpoint);
            power = Math.Min(power, MAX_STATUS);
            magic = Math.Min(magic, MAX_STATUS);
            vitality = Math.Min(vitality, MAX_STATUS);
            speed = Math.Min(speed, MAX_STATUS);

            // HPが0以下なら死亡にする
            if (hitpoint <= 0)
            {
                hitpoint = 0;
                statusAilments = StatusAilments.DOWN;
            }
            else if (Util.HasFlag(statusAilments, StatusAilments.DOWN))
            {
                statusAilments &= ~StatusAilments.DOWN;
            }
        }

        /*
        public Rom.Hero rom;
        public int level;
        public int maxHitpoint;
        public int maxMagicpoint;
        public int hitpoint;
        public int magicpoint;
        public int power;
        public int vitality;
        public int magic;
        public int speed;
        public int exp;
        public StatusAilments statusAilments;
        public Rom.Item[] equipments = new Rom.Item[6];
        public List<Common.Rom.Skill> skills = new List<Rom.Skill>();
         */

        void IGameDataItem.save(System.IO.BinaryWriter writer)
        {
            writer.Write(rom.guId.ToByteArray());
            writer.Write(level);
            writer.Write(maxHitpoint);
            writer.Write(maxMagicpoint);
            writer.Write(hitpoint);
            writer.Write(magicpoint);
            writer.Write(power);
            writer.Write(vitality);
            writer.Write(magic);
            writer.Write(speed);
            writer.Write(exp);
            writer.Write((int)statusAilments);
            foreach (var eq in equipments)
            {
                if (eq == null)
                    writer.Write(Guid.Empty.ToByteArray());
                else
                    writer.Write(eq.guId.ToByteArray());
            }
            writer.Write(skills.Count);
            foreach (var skill in skills)
            {
                writer.Write(skill.guId.ToByteArray());
            }
            equipmentEffect.save(writer);
        }

        void IGameDataItem.load(Catalog catalog, System.IO.BinaryReader reader)
        {
            rom = catalog.getItemFromGuid(Util.readGuid(reader)) as Common.Rom.Hero;
            level = reader.ReadInt32();
            maxHitpoint = reader.ReadInt32();
            maxMagicpoint = reader.ReadInt32();
            hitpoint = reader.ReadInt32();
            magicpoint = reader.ReadInt32();
            power = reader.ReadInt32();
            vitality = reader.ReadInt32();
            magic = reader.ReadInt32();
            speed = reader.ReadInt32();
            exp = reader.ReadInt32();
            statusAilments = (StatusAilments)reader.ReadInt32();
            for (int i = 0; i < equipments.Length; i++)
            {
                equipments[i] = catalog.getItemFromGuid(Util.readGuid(reader)) as Common.Rom.Item;
            }
            equipmentEffect = calcEquipmentEffect(equipments);
            equipmentConsistency();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var skill = catalog.getItemFromGuid(Util.readGuid(reader)) as Common.Rom.Skill;
                if (skill != null)
                    skills.Add(skill);
            }
            equipmentEffect.load(catalog, reader);
        }

        //-------------------------------------------------------------
        // 以下は主にエディタおよび、エンジンにて最初に仲間に加わる時のみ利用します
        //-------------------------------------------------------------

        public int calcStatus(int start, float growth, float growthRate)
        {
            float result = start;
            for (int i = 1; i < level; i++)
            {
                result += growth;
                growth = growth * growthRate;
            }
            if (result > MAX_STATUS)
                result = MAX_STATUS;
            return (int)result;
        }

        public int calcMaxHitPoint(Rom.Hero rom)
        {
            return calcStatus(rom.hp, rom.hpGrowth, rom.hpGrowthRate);
        }

        public int calcMaxMagicPoint(Rom.Hero rom)
        {
            return calcStatus(rom.mp, rom.mpGrowth, rom.mpGrowthRate);
        }
    }
}

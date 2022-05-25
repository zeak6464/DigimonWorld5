using System.Collections.Generic;

using Microsoft.Xna.Framework;

using Rom = Yukar.Common.Rom;
using Resource = Yukar.Common.Resource;

namespace Yukar.Engine
{
    public class BattleEnemyData : BattleCharacterBase
    {
        public enum MonsterSize
        {
            S,
            M,
            L,
            LL,
            Max,
        }

        public enum MonsterArrangementType
        {
            ForwardCenter,
            ForwardLeft,
            ForwardRight,

            MiddleCenter,
            MiddleLeft,
            MiddleRight,

            BackCenter,
            BackLeft,
            BackRight,

            Manual,
        }

        public Rom.Monster monster;

        public int imageId;
        public MonsterArrangementType arrangmentType;

        public TweenColor commandEffectColor;
        public Resource.ResourceItem image;
        public Point pos;

        public BattleEnemyData()
        {
            commandEffectColor = new TweenColor();
        }

        public void SetParameters(Rom.Monster m)
        {
            HitPoint = m.hitpoint;
            MagicPoint = m.magicpoint;
            MaxHitPointBase = m.hitpoint;
            MaxMagicPointBase = m.magicpoint;

            AttackBase = m.attack;
            ElementAttack = 0;
            DefenseBase = m.defense;
            //PowerBase = m.attack / 10;
            //VitalityBase = m.defense / 10;
            MagicBase = m.magic;
            SpeedBase = m.speed;

            EvationBase = m.evasion;
            DexterityBase = m.dexterity;

            Critical = 0;

            AttackAttribute = AttackAttributeType.None;

            ResistanceAttackAttributeBase[(int)AttackAttributeType.None] = 0;
            ResistanceAttackAttributeBase[(int)AttackAttributeType.A] = m.attrADefense;
            ResistanceAttackAttributeBase[(int)AttackAttributeType.B] = m.attrBDefense;
            ResistanceAttackAttributeBase[(int)AttackAttributeType.C] = m.attrCDefense;
            ResistanceAttackAttributeBase[(int)AttackAttributeType.D] = m.attrDDefense;
            ResistanceAttackAttributeBase[(int)AttackAttributeType.E] = m.attrEDefense;
            ResistanceAttackAttributeBase[(int)AttackAttributeType.F] = m.attrFDefense;

            var ailmentDefense = new List<int>();

            ailmentDefense.Add(m.poisonResistant);
            ailmentDefense.Add(m.sleepResistant);
            ailmentDefense.Add(m.paralysisResistant);
            ailmentDefense.Add(m.confusionResistant);
            ailmentDefense.Add(m.fascinationResistant);
            ailmentDefense.Add(m.deathResistant);

            ResistanceAilmentStatus = ailmentDefense.ToArray();

            PoisonDamegePercent = m.poisonDamegePercent;

            AttackEffect = m.attackEffect;

            Name = m.name;
        }

        public override void Update()
        {
            if (commandEffectColor.IsPlayTween)
            {
                commandEffectColor.Update();
            }
        }

        public override void ExecuteCommandStart()
        {
            // 2回点滅させる
            // Alpha 0 -> 255, 255 -> 0 を1セットとして2セット繰り返し = Tween回数4回
            commandEffectColor.Begin(new Color(Color.White, 0), new Color(Color.White, 255), 4, 4, TweenStyle.PingPong);

            base.ExecuteCommandStart();
        }

        public override bool isMovableToForward()
        {
            return monster.moveForward;
        }
    }
}

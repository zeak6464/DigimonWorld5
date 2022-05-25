using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;

using Yukar.Common;
using Yukar.Common.GameData;

using Rom = Yukar.Common.Rom;
using StatusAilments = Yukar.Common.GameData.Hero.StatusAilments;
using BattleCommand = Yukar.Common.Rom.BattleCommand;

namespace Yukar.Engine
{
    public abstract class BattleCharacterBase
    {
        public int UniqueID { get; set; }

        public int ExecuteCommandTurnCount { get; set; }
        public BattleCommandType selectedBattleCommandType;
        public BattleCommand selectedBattleCommand;
        public BattleCharacterBase[] targetCharacter;
        public List<BattleCharacterBase> commandFriendEffectCharacters;
        public List<BattleCharacterBase> commandEnemyEffectCharacters;
        public List<BattleCharacterBase> friendPartyMember; // 自分側のメンバー
        public List<BattleCharacterBase> enemyPartyMember;  // 相手側のメンバー

        public List<BattleCharacterBase> commandTargetList;
        public List<Rom.Skill> useableSkillList;
        public List<Party.ItemStack> haveItemList;

        public Rom.Skill selectedSkill;
        public Party.ItemStack selectedItem;

        public int EscapeSuccessBaseParcent { get; set; }
        public string EscapeSuccessMessage { get; set; }

        // HP, MP
        public int HitPoint { get; set; }
        public int MagicPoint { get; set; }

        public int MaxHitPoint { get { return MaxHitPointBase + MaxHitPointEnhance; } }
        public int MaxMagicPoint { get { return MaxMagicPointBase + MaxMagicPointEnhance; } }

        public int MaxHitPointBase { get; set; }
        public int MaxMagicPointBase { get; set; }

        public int MaxHitPointEnhance { get; set; }
        public int MaxMagicPointEnhance { get; set; }

        public float HitPointParcent { get { return (float)HitPoint / MaxHitPoint; } }
        public float MagicPointParcent { get { return (float)MagicPoint / MaxMagicPoint; } }

        // 能力値
        public int Attack { get { return Math.Max((int)((AttackBase + PowerBase + PowerEnhancement) * getChargeState()), 0); } }  // 武器も含めた攻撃力

        private float getChargeState()
        {
            return 1.0f + attackEnhanceEffects.Select(effect => effect.enhanceEffect).Sum() / 100.0f;
        }
        public int Defense { get { return Math.Max(DefenseBase + VitalityBase + VitalityEnhancement, 0); } }

        public int Power { get { return Math.Max((int)((PowerBase + PowerEnhancement) * getChargeState()), 0); } }  // 武器を除いた攻撃力
        public int Magic { get { return Math.Max(MagicBase + MagicEnhancement, 0); } }
        public int Speed { get { return Math.Max(SpeedBase + SpeedEnhancement, 0); } }

        public int Dexterity { get { return Math.Max(DexterityBase + DexterityEnhancement, 0); } }
        public int Evation { get { return Math.Max(EvationBase + EvationEnhancement, 0); } }

        public int ElementAttack { get; set; }

        public int AttackBase { get; set; }
        public int DefenseBase { get; set; }
        public int PowerBase { get; set; }
        public int VitalityBase { get; set; }
        public int MagicBase { get; set; }
        public int SpeedBase { get; set; }

        public int DexterityBase { get; set; }  // 通常攻撃の命中率
        public int EvationBase { get; set; }    // 回避率

        public int Critical { get; set; }

        // 能力強化
        public List<EnhanceEffect> attackEnhanceEffects;    // 攻撃
        public List<EnhanceEffect> guardEnhanceEffects;     // 防御

        public int PowerEnhancement { get; set; }
        public int VitalityEnhancement { get; set; }
        public int MagicEnhancement { get; set; }
        public int SpeedEnhancement { get; set; }
        public int DexterityEnhancement { get; set; } // 器用 => 命中
        public int EvationEnhancement { get; set; }   // 回避

        public List<EffectDrawer> positiveEffectDrawers;
        public List<EffectDrawer> negativeEffectDrawers;
        public List<EffectDrawer> statusEffectDrawers;

        public int positiveEffectIndex;
        public int negativeEffectIndex;
        public int statusEffectIndex;

        // 攻撃属性
        public AttackAttributeType AttackAttribute { get; set; }

        // 攻撃属性 耐性
        public int ResistanceAttackAttribute(int attributeIndex) { return ResistanceAttackAttributeBase[attributeIndex] + ResistanceAttackAttributeEnhance[attributeIndex]; }
        public float ResistanceAttackAttributeParcent(int attributeIndex) { return 1.0f - ResistanceAttackAttribute(attributeIndex) / 100.0f; }
        public int[] ResistanceAttackAttributeEnhance;
        public int[] ResistanceAttackAttributeBase;
        public AttributeToleranceType AttackAttributeTolerance(int attributeIndex)
        {
            if (ResistanceAttackAttribute(attributeIndex) == 0) return AttributeToleranceType.Normal;
            else if (ResistanceAttackAttribute(attributeIndex) < 0) return AttributeToleranceType.Weak;
            else if (ResistanceAttackAttribute(attributeIndex) <= 100) return AttributeToleranceType.Strong;
            else if (ResistanceAttackAttribute(attributeIndex) <= 200) return AttributeToleranceType.Absorb;
            else return AttributeToleranceType.Invalid;
        }

        // 状態異常 耐性
        public int[] ResistanceAilmentStatus;

        // 状態異常
        public StatusAilments Status { get; set; }

        public float DamageRate { get { return Math.Max((1.0f - guardEnhanceEffects.Select(effect => effect.enhanceEffect).Sum() / 100.0f), 0); } }

        public int PoisonDamegePercent { get; set; }

        // 通常攻撃, クリティカル時に再生するエフェクト
        public Guid AttackEffect { get; set; }
        public Vector2 EffectPosition { get; set; }

        public Vector2 DamageTextPosition { get; set; }

        public ReactionType CommandReactionType { get; set; }

        public string Name { get; set; }

        public bool IsSelect { get; set; }

        protected BattleCharacterBase()
        {
            commandFriendEffectCharacters = new List<BattleCharacterBase>();
            commandEnemyEffectCharacters = new List<BattleCharacterBase>();

            friendPartyMember = new List<BattleCharacterBase>();
            enemyPartyMember = new List<BattleCharacterBase>();

            commandTargetList = new List<BattleCharacterBase>();
            useableSkillList = new List<Rom.Skill>();
            haveItemList = new List<Party.ItemStack>();

            attackEnhanceEffects = new List<EnhanceEffect>();
            guardEnhanceEffects = new List<EnhanceEffect>();

            int elementCount = Hero.MAX_ELEMENTS + 1;

            ResistanceAttackAttributeEnhance = new int[elementCount];
            ResistanceAttackAttributeBase = new int[elementCount];

            positiveEffectDrawers = new List<EffectDrawer>();
            negativeEffectDrawers = new List<EffectDrawer>();
            statusEffectDrawers = new List<EffectDrawer>();

            IsSelect = false;
        }

        // 各行動に応じて何か動作をする用のハンドラー
        public Action<BattleCharacterBase, bool, bool> actionHandler;   // p2 = Action/Reaction(true/false) , p3 = Start/End(true/false)
        internal float imageAlpha = 1.0f;

        public abstract void Update();

        public bool SetStatusAilment(StatusAilments setStatusAilment)
        {
            var nextStatus = Status;
            bool isStatusChange = !Status.HasFlag(StatusAilments.DOWN);

            // 既に掛かっている状態異常がある場合は新たな状態異常を設定できるか確認する
            if (Status.HasFlag(StatusAilments.PARALYSIS))
            {
                // 「麻痺」の状態では「魅了」「混乱」「睡眠」にならない
                isStatusChange &= !setStatusAilment.HasFlag(StatusAilments.FASCINATION);
                isStatusChange &= !setStatusAilment.HasFlag(StatusAilments.CONFUSION);
                isStatusChange &= !setStatusAilment.HasFlag(StatusAilments.SLEEP);
            }
            else if (Status.HasFlag(StatusAilments.SLEEP))
            {
                // 「睡眠」の状態では「魅了」「混乱」にならない
                isStatusChange &= !setStatusAilment.HasFlag(StatusAilments.FASCINATION);
                isStatusChange &= !setStatusAilment.HasFlag(StatusAilments.CONFUSION);
            }
            else if (Status.HasFlag(StatusAilments.CONFUSION))
            {
                // 「混乱」状態では「魅了」にならない
                isStatusChange &= !setStatusAilment.HasFlag(StatusAilments.FASCINATION);
            }


            // 更新後の状態を求める
            if (setStatusAilment.HasFlag(StatusAilments.CONFUSION))
            {
                // 「魅了」を解除する
                nextStatus &= ~StatusAilments.FASCINATION;

                nextStatus |= StatusAilments.CONFUSION;
            }
            if (setStatusAilment.HasFlag(StatusAilments.FASCINATION))
            {
                nextStatus |= StatusAilments.FASCINATION;
            }
            if (setStatusAilment.HasFlag(StatusAilments.SLEEP))
            {
                // 「混乱」「魅了」を解除する
                nextStatus &= ~StatusAilments.CONFUSION;
                nextStatus &= ~StatusAilments.FASCINATION;

                nextStatus |= StatusAilments.SLEEP;
            }
            if (setStatusAilment.HasFlag(StatusAilments.PARALYSIS))
            {
                // 「混乱」「魅了」「睡眠」を解除する
                nextStatus &= ~StatusAilments.CONFUSION;
                nextStatus &= ~StatusAilments.FASCINATION;
                nextStatus &= ~StatusAilments.SLEEP;

                nextStatus |= StatusAilments.PARALYSIS;
            }
            if (setStatusAilment.HasFlag(StatusAilments.POISON))
            {
                nextStatus |= StatusAilments.POISON;
            }
            if (setStatusAilment.HasFlag(StatusAilments.DOWN))
            {
                // 他の状態異常を解除し「戦闘不能」のみにする
                nextStatus = StatusAilments.DOWN;
            }

            if (setStatusAilment == StatusAilments.NONE)
            {
                nextStatus = StatusAilments.NONE;
            }


            // 状態異常を設定
            if (isStatusChange)
            {
                Status = nextStatus;
            }

            return isStatusChange;
        }

        public void RecoveryStatusAilment(StatusAilments recoveryStatusAilment)
        {
            var nextStatus = Status;

            if (recoveryStatusAilment.HasFlag(StatusAilments.POISON))
            {
                nextStatus &= ~StatusAilments.POISON;
            }

            if (recoveryStatusAilment.HasFlag(StatusAilments.SLEEP))
            {
                nextStatus &= ~StatusAilments.SLEEP;
            }

            if (recoveryStatusAilment.HasFlag(StatusAilments.FASCINATION))
            {
                nextStatus &= ~StatusAilments.FASCINATION;
            }

            if (recoveryStatusAilment.HasFlag(StatusAilments.PARALYSIS))
            {
                nextStatus &= ~StatusAilments.PARALYSIS;
            }

            if (recoveryStatusAilment.HasFlag(StatusAilments.CONFUSION))
            {
                nextStatus &= ~StatusAilments.CONFUSION;
            }

            if (recoveryStatusAilment.HasFlag(StatusAilments.DOWN))
            {
                //nextStatus &= ~StatusAilments.DOWN;
                nextStatus = StatusAilments.NONE;
            }

            Status = nextStatus;
        }

        public void GetSkillTarget(Common.Rom.Skill skill, out BattleCharacterBase[] friendEffectTargets, out BattleCharacterBase[] enemyEffectTargets)
        {
            var friendEffectTargetList = new List<BattleCharacterBase>();
            var enemyEffectTargetList = new List<BattleCharacterBase>();

            switch (skill.option.target)
            {
                case Rom.TargetType.PARTY_ONE:
                    friendEffectTargetList.AddRange(targetCharacter);
                    break;
                case Rom.TargetType.PARTY_ALL:
                    if (skill.friendEffect.down)
                    {
                        friendEffectTargetList.AddRange(friendPartyMember);
                    }
                    else
                    {
                        friendEffectTargetList.AddRange(friendPartyMember.Where(member => member.HitPoint > 0));
                    }
                    break;
                case Rom.TargetType.ENEMY_ONE:
                    enemyEffectTargetList.AddRange(targetCharacter);
                    break;
                case Rom.TargetType.ENEMY_ALL:
                    enemyEffectTargetList.AddRange(enemyPartyMember.Where(enemy => enemy.HitPoint > 0));
                    break;
                case Rom.TargetType.SELF:
                    friendEffectTargetList.Add(this);
                    break;
                case Rom.TargetType.ALL:
                    if (skill.friendEffect.down)
                    {
                        friendEffectTargetList.AddRange(friendPartyMember);
                    }
                    else
                    {
                        friendEffectTargetList.AddRange(friendPartyMember.Where(member => member.HitPoint > 0));
                    }
                    enemyEffectTargetList.AddRange(enemyPartyMember.Where(enemy => enemy.HitPoint > 0));
                    break;
                case Rom.TargetType.OTHERS:
                    friendEffectTargetList.AddRange(targetCharacter);
                    break;
                case Rom.TargetType.SELF_ENEMY_ONE:
                    friendEffectTargetList.Add(this);
                    enemyEffectTargetList.AddRange(targetCharacter);
                    break;
                case Rom.TargetType.SELF_ENEMY_ALL:
                    friendEffectTargetList.Add(this);
                    enemyEffectTargetList.AddRange(enemyPartyMember.Where(enemy => enemy.HitPoint > 0));
                    break;
                case Rom.TargetType.OTHERS_ENEMY_ONE:
                    if (skill.friendEffect.down)
                    {
                        friendEffectTargetList.AddRange(friendPartyMember.Where(character => character != this));
                    }
                    else
                    {
                        friendEffectTargetList.AddRange(friendPartyMember.Where(character => character != this && character.HitPoint > 0));
                    }
                    enemyEffectTargetList.AddRange(targetCharacter);
                    break;
                case Rom.TargetType.OTHERS_ALL:
                    if (skill.friendEffect.down)
                    {
                        friendEffectTargetList.AddRange(friendPartyMember.Where(character => character != this));
                    }
                    else
                    {
                        friendEffectTargetList.AddRange(friendPartyMember.Where(character => character != this && character.HitPoint > 0));
                    }
                    enemyEffectTargetList.AddRange(enemyPartyMember.Where(enemy => enemy.HitPoint > 0));
                    break;
                case Rom.TargetType.PARTY_ONE_ENEMY_ALL:
                    friendEffectTargetList.AddRange(targetCharacter);
                    enemyEffectTargetList.AddRange(enemyPartyMember.Where(enemy => enemy.HitPoint > 0));
                    break;
                case Rom.TargetType.PARTY_ALL_ENEMY_ONE:
                    if (skill.friendEffect.down)
                    {
                        friendEffectTargetList.AddRange(friendPartyMember);
                    }
                    else
                    {
                        friendEffectTargetList.AddRange(friendPartyMember.Where(member => member.HitPoint > 0));
                    }
                    enemyEffectTargetList.AddRange(targetCharacter);
                    break;
            }

            friendEffectTargets = friendEffectTargetList.ToArray();
            enemyEffectTargets = enemyEffectTargetList.ToArray();
        }

        internal virtual string getDigest()
        {
            return UniqueID.ToString();
        }

        public virtual void ExecuteCommandStart()
        {
            if (actionHandler != null)
                actionHandler(this, true, true);
        }

        public virtual void ExecuteCommandEnd()
        {
            if (actionHandler != null)
                actionHandler(this, true, false);
        }

        public virtual void CommandReactionStart()
        {
            if (actionHandler != null)
                actionHandler(this, false, true);
        }

        public virtual bool isMovableToForward()
        {
            return true;
        }

        public virtual void CommandReactionEnd()
        {
            if (actionHandler != null)
                actionHandler(this, false, false);
        }
    }
}

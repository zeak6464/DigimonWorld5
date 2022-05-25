using Microsoft.Xna.Framework;

namespace Yukar.Engine
{
    public enum SelectBattleCommandState
    {
        None,

        CommandSelect,

        Attack_Command,
        Attack_MakeTargetList,
        Attack_SelectTarget,

        Guard_Command,

        Charge_Command,

        SkillSameEffect_Command,
        SkillSameEffect_MakeTargetList,
        SkillSameEffect_SelectTarget,

        Skill_Command,
        Skill_SelectSkill,
        Skill_MakeTargetList,
        Skill_SelectTarget,

        Item_Command,
        Item_SelectItem,
        Item_MakeTargetList,
        Item_SelectTarget,

        Escape_Command,

        Back_Command,

        CommandEnd,

        CommandCancel,
    }

    public enum WindowType
    {
        None,
        MessageWindow,
        PlayerCommandWindow,
        SkillListWindow,
        ItemListWindow,
        CommandTargetPlayerListWindow,
        CommandTargetMonsterListWindow,
    }

    public enum StatusWindowState
    {
        Wait,
        Active,
    }

    public enum BattleCommandType
    {
        Undecided,
        Nothing,
        Nothing_Down,
        Cancel,
        Attack,
        Critical,
        Charge,
        Guard,
        SameSkillEffect,
        Skill,
        Item,
        PlayerEscape,
        MonsterEscape,
        Skip,
        Back,
    }

    public enum ReactionType
    {
        None,
        Damage,
        Heal,
    }

    public enum StatusAilmentIndex
    {
        Poison = 0,
        Sleep,
        Paralysis,
        Confusion,
        Fascination,
        Down,
    }

    public enum AttributeToleranceType
    {
        Normal,     // 変化無し => ダメージ計算をそのまま適応
        Strong,     // 耐性あり => ダメージ減少
        Weak,       // 弱点     => ダメージ増加
        Absorb,     // 吸収     => HPを回復
        Invalid,    // 無効     => 0固定
    }

    public enum AttackAttributeType
    {
        None,
        A,
        B,
        C,
        D,
        E,
        F,

        Poison,
        Sleep,
        Paralysis,
        Death,
        Confusion,
        Fascination,
    }

    public class BattleCharacterPosition{
        public static int[,] FRIEND_POS_X = {
            { 0, 0, 0, 0},
            {-1, 1, 0, 0},
            {-2, 0, 2, 0},
            {-3,-1, 1, 3},
        };
        public const int FRIEND_POS_Y = 3;

        public static int[,] ENEMY_POS_X = {
            {0,0,0,0,0,0},
            {-2,2,0,0,0,0},
            {-3,0,3,0,0,0},
            {-3,0,0,3,0,0},
            {-3,-2,0,2,3,0},
            {-3,-3,0,0,3,3},
        };
        public static int[,] ENEMY_POS_Y = {
            {-3,0,0,0,0,0},
            {-3,-3,0,0,0,0},
            {-3,-3,-3,0,0,0},
            {-3,-3,-1,-3,0,0},
            {-1,-3,-1,-3,-1,0},
            {-3,-1,-3,-1,-3,-1},
        };
        public static BattleEnemyData.MonsterArrangementType[,] ENEMY_POS_TYPE = {
            {BattleEnemyData.MonsterArrangementType.BackCenter, BattleEnemyData.MonsterArrangementType.BackCenter, BattleEnemyData.MonsterArrangementType.BackCenter,
             BattleEnemyData.MonsterArrangementType.BackCenter, BattleEnemyData.MonsterArrangementType.BackCenter, BattleEnemyData.MonsterArrangementType.BackCenter},
            {BattleEnemyData.MonsterArrangementType.BackLeft, BattleEnemyData.MonsterArrangementType.BackRight, BattleEnemyData.MonsterArrangementType.BackCenter,
             BattleEnemyData.MonsterArrangementType.BackCenter, BattleEnemyData.MonsterArrangementType.BackCenter, BattleEnemyData.MonsterArrangementType.BackCenter},
            {BattleEnemyData.MonsterArrangementType.BackLeft, BattleEnemyData.MonsterArrangementType.BackCenter, BattleEnemyData.MonsterArrangementType.BackRight,
             BattleEnemyData.MonsterArrangementType.BackCenter, BattleEnemyData.MonsterArrangementType.BackCenter, BattleEnemyData.MonsterArrangementType.BackCenter},
            {BattleEnemyData.MonsterArrangementType.BackLeft, BattleEnemyData.MonsterArrangementType.BackCenter, BattleEnemyData.MonsterArrangementType.ForwardCenter,
             BattleEnemyData.MonsterArrangementType.BackRight, BattleEnemyData.MonsterArrangementType.BackCenter, BattleEnemyData.MonsterArrangementType.BackCenter},
            {BattleEnemyData.MonsterArrangementType.ForwardLeft, BattleEnemyData.MonsterArrangementType.BackLeft, BattleEnemyData.MonsterArrangementType.ForwardCenter,
             BattleEnemyData.MonsterArrangementType.BackRight, BattleEnemyData.MonsterArrangementType.ForwardRight, BattleEnemyData.MonsterArrangementType.BackCenter},
            {BattleEnemyData.MonsterArrangementType.BackLeft, BattleEnemyData.MonsterArrangementType.ForwardLeft, BattleEnemyData.MonsterArrangementType.BackCenter,
             BattleEnemyData.MonsterArrangementType.ForwardCenter, BattleEnemyData.MonsterArrangementType.BackRight, BattleEnemyData.MonsterArrangementType.ForwardRight},
        };
        public static Vector2 DEFAULT_BATTLE_FIELD_CENTER = new Vector2(14f, 14f);
        public static readonly Vector2 DEFAULT_BATTLE_FIELD_SIZE = new Vector2(9f, 9f);
        public enum PosType
        {
            FRIEND,
            ENEMY,
        }
        public static Vector2 getPosition(Vector2 centerOfField, PosType type, int num, int max)
        {
            max--;
            var result = centerOfField;
            if (type == PosType.FRIEND)
            {
                result.X += FRIEND_POS_X[max, num];
                result.Y += FRIEND_POS_Y;
            }
            else
            {
                result.X += ENEMY_POS_X[max, num];
                result.Y += ENEMY_POS_Y[max, num];
            }
            return result;
        }

        internal static BattleEnemyData.MonsterArrangementType getPositionType(int num, int max)
        {
            return ENEMY_POS_TYPE[max - 1, num];
        }
    }
}

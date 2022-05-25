namespace Yukar.Engine
{
    public class EnhanceEffect
    {
        public enum EnhanceEffectType
        {
            TurnEffect,         // 指定ターン経過で効果無効
            DurationEffect,     // 効果が一定割合になるまで継続 (減衰などの効果を組み合わせて設定する)
            CommandEffect,      // 指定の行動を行うまで継続 (現在は使用していないがチャージ攻撃などを想定)
        }

        private EnhanceEffect()
        {
            turnCount = 0;
        }

        public EnhanceEffect(int enhanceEffect, int durationTurn, float diff = 1.0f)
            : this()
        {
            this.type = EnhanceEffectType.TurnEffect;
            this.enhanceEffect = enhanceEffect;
            this.durationTurn = durationTurn;
            this.diff = diff;
        }

        public EnhanceEffect(int enhanceEffect, float diff)
            : this()
        {
            this.type = EnhanceEffectType.DurationEffect;
            this.enhanceEffect = enhanceEffect;
            this.diff = diff;
        }

        public EnhanceEffect(int enhanceEffect, BattleCommandType commandType)
            : this()
        {
            this.type = EnhanceEffectType.CommandEffect;
            this.enhanceEffect = enhanceEffect;
            this.commandType = commandType;
        }

        public readonly EnhanceEffectType type;

        // 現在の効果
        public int enhanceEffect;

        // 効果が適応されてから経過したターン数
        public int turnCount;

        // 効果が無効になるターン数
        public readonly int durationTurn;

        // 1ターンごとの上昇率 or 減衰率
        public readonly float diff;

        // コマンドを実行後に効果を無効にする
        public readonly BattleCommandType commandType;
    }
}

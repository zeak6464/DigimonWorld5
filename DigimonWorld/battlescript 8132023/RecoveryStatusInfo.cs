using Yukar.Engine;

namespace Yukar.Battle
{
    /// <summary>
    /// 状態変化が回復した際の情報管理クラス
    /// Information management class when state change is recovered
    /// </summary>
    class RecoveryStatusInfo
    {
        private readonly BattleCharacterBase character;
        private readonly Common.Rom.Condition condition;

        public RecoveryStatusInfo(BattleCharacterBase inCharacter, Common.Rom.Condition condition)
        {
            character = inCharacter;
            this.condition = condition;
        }

        public bool IsContinued { get => character.conditionInfoDic.ContainsKey(condition.guId); }

        public string GetMessage(Common.Rom.GameSettings gameSettings)
        {
            return string.Format(condition.messageForFinished, character?.Name ?? "");
        }
    }
}

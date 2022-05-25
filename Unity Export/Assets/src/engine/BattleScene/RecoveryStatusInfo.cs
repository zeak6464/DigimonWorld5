using StatusAilments = Yukar.Common.GameData.Hero.StatusAilments;

namespace Yukar.Engine
{
    class RecoveryStatusInfo
    {
        private readonly BattleCharacterBase character;
        private readonly StatusAilments status;

        public RecoveryStatusInfo(BattleCharacterBase character, StatusAilments status)
        {
            this.character = character;
            this.status = status;
        }

        public string GetMessage(Common.Rom.GameSettings gameSettings)
        {
            string message = "";

            switch (status)
            {
                case StatusAilments.POISON: message = string.Format(gameSettings.glossary.battle_recover_poison, character.Name); break;
                case StatusAilments.SLEEP: message = string.Format(gameSettings.glossary.battle_recover_sleep, character.Name); break;
                case StatusAilments.PARALYSIS: message = string.Format(gameSettings.glossary.battle_recover_paralysis, character.Name); break;
                case StatusAilments.CONFUSION: message = string.Format(gameSettings.glossary.battle_recover_confusion, character.Name); break;
                case StatusAilments.FASCINATION: message = string.Format(gameSettings.glossary.battle_recover_fascination, character.Name); break;
                case StatusAilments.DOWN: message = string.Format(gameSettings.glossary.battle_recover_dead, character.Name); break;
            }

            return message;
        }
    }
}

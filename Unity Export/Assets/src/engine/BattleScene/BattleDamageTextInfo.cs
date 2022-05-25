using System.Linq;
namespace Yukar.Engine
{
    public class BattleDamageTextInfo
    {
        public class CharacterInfo
        {
            public char c;
            public float timer;
        }

        public enum TextType
        {
            HitPointDamage,
            MagicPointDamage,
            CriticalDamage,
            HitPointHeal,
            MagicPointHeal,
            Miss,
        }

        public BattleDamageTextInfo(TextType textType, BattleCharacterBase target)
        {
            this.type = textType;
            this.targetCharacter = target;

            switch (textType)
            {
                case TextType.HitPointDamage:
                case TextType.CriticalDamage:
                case TextType.HitPointHeal:
                    this.text = "0";
                    break;

                case TextType.Miss:
                    this.text = Yukar.Common.Catalog.sInstance.getGameSettings().glossary.battle_miss;
                    break;
            }
        }
        public BattleDamageTextInfo(TextType textType, BattleCharacterBase target, string text)
        {
            this.type = textType;
            this.text = text;
            this.IsNumberOnlyText = (text.Count(c => char.IsNumber(c)) == text.Length);
            this.targetCharacter = target;
        }

        public readonly TextType type;
        public CharacterInfo[] textInfo;
        public readonly string text;
        public bool IsNumberOnlyText;
        public readonly BattleCharacterBase targetCharacter;
    }
}

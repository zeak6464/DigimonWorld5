using System.Linq;
using Yukar.Engine;

namespace Yukar.Battle
{
    /// <summary>
    /// バトル中のダメージ数値のポップを管理するクラス
    /// A class that manages pops of damage numbers during battle
    /// </summary>
    public class BattleDamageTextInfo
    {
        /// <summary>
        /// 表示対象と経過時間
        /// Display target and elapsed time
        /// </summary>
        public class CharacterInfo
        {
            public char c;
            public float timer;
        }

        /// <summary>
        /// 表示タイプ
        /// Display type
        /// </summary>
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

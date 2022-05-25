using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Yukar.Common.GameData;
using Resource = Yukar.Common.Resource;
using StatusAilments = Yukar.Common.GameData.Hero.StatusAilments;

namespace Yukar.Engine
{
    public class BattlePlayerData : BattleCharacterBase
    {
        public Hero player;
        public int commandSelectedCount;
        public int characterImageId;
        public int viewIndex;
        public int[] characterEmotionPatternImageId;
        public bool isCharacterImageReverse;
        public Color characterImageColor;
        public Vector2 statusWindowDrawPosition;
        public Vector2 commandSelectWindowBasePosition;
        public Vector2 characterImagePosition;
        public Vector2 characterImageSize;
        public TweenVector2 characterImageTween;
        public TweenVector2 characterImageEffectTween;
        public StatusWindowState statusWindowState;
        public BattleStatusWindowDrawer.StatusData battleStatusData;
        public BattleStatusWindowDrawer.StatusData startStatusData;
        public BattleStatusWindowDrawer.StatusData nextStatusData;

        public readonly Vector2 RelativePosition = new Vector2(30, 0);
        public const int RegisterBattleCommandCountMax = 5;
        internal bool forceSetCommand;
        public Guid currentFace;

        public BattlePlayerData()
        {
            characterImageTween = new TweenVector2();
            characterImageEffectTween = new TweenVector2();

            characterImageColor = Color.White;

            statusWindowState = StatusWindowState.Wait;
        }

        public void SetParameters(Hero p, bool isHpMpMax, bool isParameterMax,Party party)
        {
            if (isHpMpMax)
            {
                HitPoint = Hero.MAX_STATUS;
                MagicPoint = Hero.MAX_STATUS;
                MaxHitPointBase = Hero.MAX_STATUS;
                MaxMagicPointBase = Hero.MAX_STATUS;
            }
            else
            {
                HitPoint = p.hitpoint;
                MagicPoint = p.magicpoint;
                MaxHitPointBase = p.maxHitpoint;
                MaxMagicPointBase = p.maxMagicpoint;
            }

            if (isParameterMax)
            {
                AttackBase = Hero.MAX_STATUS;
                ElementAttack = Hero.MAX_STATUS;
                DefenseBase = Hero.MAX_STATUS;
                PowerBase = Hero.MAX_STATUS;
                MagicBase = Hero.MAX_STATUS;
                VitalityBase = Hero.MAX_STATUS;
                SpeedBase = Hero.MAX_STATUS;
            }
            else
            {
                AttackBase = p.equipmentEffect.attack;
                ElementAttack = p.equipmentEffect.elementAttack;
                DefenseBase = p.equipmentEffect.defense;
                PowerBase = p.power;
                MagicBase = p.magic;
                VitalityBase = p.vitality;
                SpeedBase = p.speed;
            }

            EvationBase = p.equipmentEffect.evation;
            DexterityBase = p.equipmentEffect.dexterity;

            Critical = p.equipmentEffect.critical;

            AttackAttribute = (AttackAttributeType)p.equipmentEffect.attackElement;

            var elementDefense = new List<int>();

            elementDefense.Add(0);  // 無属性攻撃の耐性値
            elementDefense.AddRange(p.equipmentEffect.elementDefense);

            ResistanceAttackAttributeBase = elementDefense.ToArray();
            ResistanceAilmentStatus = p.equipmentEffect.ailmentDefense;

            PoisonDamegePercent = p.rom.poisonDamegePercent;

            AttackEffect = p.equipmentEffect.attackEffect;

            Name = party.getHeroName(p.rom.guId);
        }

        public override void Update()
        {
            if (characterImageTween.IsPlayTween)
            {
                characterImageTween.Update();
            }

            if (characterImageEffectTween.IsPlayTween)
            {
                characterImageEffectTween.Update();
            }
        }

        public bool IsCommandSelectable
        {
            get
            {
                if (forceSetCommand) return false;
                if (player.rom.battleCommandList.Count == 0) return false;

                if (Status == StatusAilments.NONE) return true;
                if (Status == StatusAilments.POISON) return true;

                return false;
            }
        }

        public void ChangeEmotion(Resource.Face.FaceType emotion)
        {
            if (characterEmotionPatternImageId != null)
                characterImageId = characterEmotionPatternImageId[(int)emotion];
        }

        public override void ExecuteCommandStart()
        {
            ChangeEmotion(Resource.Face.FaceType.FACE_ANGER);

            // バストアップ画像の表示位置を移動させる
            characterImageTween.Begin(RelativePosition, 5);

            base.ExecuteCommandStart();
        }

        public override void ExecuteCommandEnd()
        {
            ChangeEmotion(Resource.Face.FaceType.FACE_NORMAL);

            characterImageTween.Begin(Vector2.Zero, 5);

            base.ExecuteCommandEnd();
        }

        public override void CommandReactionStart()
        {
            switch (CommandReactionType)
            {
                // TODO : 受けるダメージによって演出を変化させる スキルやアイテムの場合は効果によって変化させる そのために引数かメンバー変数を追加して対応する => 対応済み
                // できればメンバ変数では無く引数で渡したい 設定するタイミングと実際の動作タイミングが異なることが原因
                case ReactionType.Damage:
                    ChangeEmotion(Resource.Face.FaceType.FACE_SORROW);
                    characterImageEffectTween.Begin(new Vector2(-10, 0), new Vector2(10, 0), 3, 5, TweenStyle.PingPong);
                    break;
            }

            base.CommandReactionStart();
        }

        public override void CommandReactionEnd()
        {
            switch (CommandReactionType)
            {
                case ReactionType.Damage:
                    ChangeEmotion(Resource.Face.FaceType.FACE_NORMAL);
                    break;
            }

            base.CommandReactionEnd();
        }

        internal void setFaceImage(Resource.Face face)
        {
            disposeFace();

            var faceImageIdList = new List<int>();

            if (face == null)
            {
                characterImageId = -1;

                faceImageIdList.Add(-1);
                faceImageIdList.Add(-1);
                faceImageIdList.Add(-1);
                faceImageIdList.Add(-1);
                currentFace = Guid.Empty;
            }
            else
            {
                characterImageId = Graphics.LoadImage(face.getFacePath(Resource.Face.FaceType.FACE_NORMAL));

                faceImageIdList.Add(Graphics.LoadImage(face.getFacePath(Resource.Face.FaceType.FACE_NORMAL)));
                faceImageIdList.Add(Graphics.LoadImage(face.getFacePath(Resource.Face.FaceType.FACE_SMILE)));
                faceImageIdList.Add(Graphics.LoadImage(face.getFacePath(Resource.Face.FaceType.FACE_ANGER)));
                faceImageIdList.Add(Graphics.LoadImage(face.getFacePath(Resource.Face.FaceType.FACE_SORROW)));
                currentFace = face.guId;
            }

            characterEmotionPatternImageId = faceImageIdList.ToArray();
        }

        internal void disposeFace()
        {
            if (characterEmotionPatternImageId != null)
            {
                foreach (var emotionImageId in characterEmotionPatternImageId)
                {
                    if (emotionImageId >= 0) Graphics.UnloadImage(emotionImageId);
                }
            }

            characterEmotionPatternImageId = null;
        }

        internal void calcHeroLayout(int index)
        {
            viewIndex = index;
            isCharacterImageReverse = (index % 2 != 0);
            commandSelectWindowBasePosition.X = (isCharacterImageReverse ? (960 - 210 - 230) : 210);
            commandSelectWindowBasePosition.Y = (index < 2 ? 10 : 280);
        }

        internal override string getDigest()
        {
            return player.rom.guId.ToString();
        }

        public override bool isMovableToForward()
        {
            return player.rom.moveForward;
        }
    }
}

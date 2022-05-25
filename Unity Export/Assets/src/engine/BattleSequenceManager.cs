using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

using Yukar.Common;
using Yukar.Common.GameData;

using Rom = Yukar.Common.Rom;
using Resource = Yukar.Common.Resource;
using StatusAilments = Yukar.Common.GameData.Hero.StatusAilments;
using BattleCommand = Yukar.Common.Rom.BattleCommand;

namespace Yukar.Engine
{
    public class BattleSequenceManager
    {
        public enum BattleState
        {
            StartFlash,
            StartFadeOut,
            StartFadeIn,
            BattleStart,
            Wait,
            PlayerTurnStart,
            CheckTurnRecoveryStatus,
            DisplayTurnRecoveryStatus,
            SelectActivePlayer,
            CheckCommandSelect,
            SetPlayerBattleCommand,
            SelectPlayerBattleCommand,
            SetPlayerBattleCommandTarget,
            SetEnemyBattleCommand,
            ReadyExecuteCommand,
            ExecuteBattleCommand,
            SetStatusMessageText,
            DisplayStatusMessage,
            DisplayStatusDamage,
            SetCommandMessageText,
            DisplayMessageText,
            SetCommandEffect,
            DisplayCommandEffect,
            DisplayDamageText,
            CheckCommandRecoveryStatus,
            DisplayCommandRecoveryStatus,
            CheckBattleCharacterDown1,
            FadeMonsterImage1,
            BattleFinishCheck1,
            ProcessPoisonStatus,
            CheckBattleCharacterDown2,
            FadeMonsterImage2,
            BattleFinishCheck2,
            PlayerChallengeEscape,
            StartBattleFinishEvent,
            ProcessBattleFinish,
            Result,
            PlayerEscapeSuccess,
            StopByEvent,
            PlayerEscapeFail,
            MonsterEscape,
            SetFinishEffect,
            FinishFadeOut,
            FinishFadeIn,
        }

        internal void finalize()
        {
            if (!owner.IsBattle2D)
            {
                var viewer = battleViewer as BattleViewer3D;
                viewer.reset();
                viewer.finalize();
            }
        }

        // デバッグ用 テストコマンド
        enum TestCommand
        {
            None,
            DisplayCharacterEmotionAll,
        }

        public enum BattleResultState
        {
            Standby,
            NonFinish,
            Win,            // 勝利
            Lose_GameOver,  // ゲームオーバー画面に遷移
            Lose_Continue,  // 戦闘終了後もゲーム継続
            Escape,         // 逃走
            Lose_Advanced_GameOver, // 指定したゲームオーバー
            Escape_ToTitle,
        }

        enum BackGroundStyle
        {
            FillColor,
            Image,
            Model,
        }

        public BattleResultState BattleResult { get; private set; }
        public bool IsPlayingBattleEffect { get; private set; }
        public bool IsDrawingBattleScene { get; private set; }
        public int GetExp { get; private set; }
        public int DropMoney { get; private set; }
        public Rom.Item[] DropItems { get; private set; }

        public BattleViewer battleViewer;
        ResultViewer resultViewer;
        TweenColor fadeScreenColorTweener;

        Dictionary<Guid, int> iconTable;

        internal BattleState battleState;
        SelectBattleCommandState battleCommandState;
        bool escapeAvailable;
        bool gameoverOnLose;
        float battleStateFrameCount;

        GameMain owner;
        Catalog catalog;
        Rom.GameSettings gameSettings;

        internal delegate void BattleStartEventHandler();
        internal event BattleStartEventHandler BattleStartEvents;
        internal event BattleStartEventHandler BattleResultWinEvents;
        internal event BattleStartEventHandler BattleResultLoseGameOverEvents;
        internal event BattleStartEventHandler BattleResultEscapeEvents;

        // 戦闘に必要なステータス
        Party party;
        List<BattleCommand> playerBattleCommand;
        internal List<BattlePlayerData> playerData;
        internal List<BattleEnemyData> enemyMonsterData;

        internal BattleCharacterBase activeCharacter;
        internal BattlePlayerData commandSelectPlayer;
        List<BattleCharacterBase> battleEntryCharacters;
        int commandSelectedMemberCount;
        int commandExecuteMemberCount;
        StatusAilments displayedStatusAilments;
        List<RecoveryStatusInfo> recoveryStatusInfo;

        int playerEscapeFailedCount;

        BackGroundStyle backGroundStyle;
        Color backGroundColor;
        int backGroundImageId = -1;//#23959-1
                                   //ModifiedModelInstance backGroundModel;

        internal TweenFloat statusUpdateTweener;
        TweenListManager<float, TweenFloat> openingBackgroundImageScaleTweener;

        Random battleRandom;

        private const int DexterityMax = 100;
        private const int DamageMin = 0;
        private const int DamageMax = 99999;
        
        internal BattleEventController battleEvents;
        int idNumber = 7;   // モンスターとかぶらないようにする

        public BattleSequenceManager(GameMain owner, Catalog catalog, WindowDrawer battleWindow, WindowDrawer battleBar)
        {
            this.owner = owner;
            this.catalog = catalog;

            gameSettings = catalog.getGameSettings();

            BattleResult = BattleResultState.Standby;

            playerBattleCommand = new List<Rom.BattleCommand>();

            battleEntryCharacters = new List<BattleCharacterBase>();

            battleRandom = new Random();

            if (owner.IsBattle2D)
            {
                battleViewer = new BattleViewer(owner);
            }
            else
            {
                battleViewer = new BattleViewer3D(owner);
            }
            battleViewer.setOwner(this);

            resultViewer = new ResultViewer(owner);

            iconTable = new Dictionary<Guid, int>();

            statusUpdateTweener = new TweenFloat();
            fadeScreenColorTweener = new TweenColor();
            openingBackgroundImageScaleTweener = new TweenListManager<float, TweenFloat>();
        }

        public void BattleStart(Party party, Guid[] monsters, Common.Rom.Map.BattleSetting settings, bool escapeAvailable = true, bool gameoverOnLose = true,
            Point[] monsterLayout = null)
        {
            this.escapeAvailable = escapeAvailable;
            this.gameoverOnLose = gameoverOnLose;

            ChangeBattleState(BattleState.StartFlash);
            battleCommandState = SelectBattleCommandState.None;
            BattleResult = BattleResultState.NonFinish;
            IsPlayingBattleEffect = true;
            IsDrawingBattleScene = false;

            recoveryStatusInfo = new List<RecoveryStatusInfo>();

            this.party = party;

            // プレイヤーの設定
            playerData = new List<BattlePlayerData>();
            for (int index = 0; index < party.members.Count; index++)
            {
                addPlayerData(party.members[index]);
            }

            enemyMonsterData = new List<BattleEnemyData>();

            // 敵の設定
            for (int index = 0; index < monsters.Length; index++)
            {
                if (monsterLayout == null)
                    addEnemyData(monsters[index]);
                else
                    addEnemyData(monsters[index], monsterLayout[index]);
            }

            // アイコン画像の読み込み
            var iconGuidSet = new HashSet<Guid>();

            foreach (var player in playerData)
            {
                foreach (var commandGuid in player.player.rom.battleCommandList)
                {
                    var command = catalog.getItemFromGuid(commandGuid) as Common.Rom.BattleCommand;

                    if (command != null) iconGuidSet.Add(command.icon.guId);
                }

            }

            foreach (var player in playerData)
            {
                foreach (var skill in player.player.skills)
                {
                    iconGuidSet.Add(skill.icon.guId);
                }
            }

            foreach (var item in party.items)
            {
                iconGuidSet.Add(item.item.icon.guId);
            }

            iconGuidSet.Add(gameSettings.escapeIcon.guId);
            iconGuidSet.Add(gameSettings.returnIcon.guId);

            iconTable.Clear();

            foreach (var guid in iconGuidSet)
            {
                var icon = catalog.getItemFromGuid(guid) as Common.Resource.Icon;

                if (icon != null)
                {
                    var image = Graphics.LoadImage(icon.path);

                    iconTable.Add(guid, image);
                }
                else
                {
                    iconTable.Add(guid, -1);
                }
            }

            commandSelectedMemberCount = 0;

            foreach (var player in playerData)
            {
                player.friendPartyMember.Clear();
                player.enemyPartyMember.Clear();

                player.friendPartyMember.AddRange(playerData.Cast<BattleCharacterBase>());
                player.enemyPartyMember.AddRange(enemyMonsterData.Cast<BattleCharacterBase>());
            }

            foreach (var monster in enemyMonsterData)
            {
                monster.friendPartyMember.Clear();
                monster.enemyPartyMember.Clear();

                monster.friendPartyMember.AddRange(enemyMonsterData.Cast<BattleCharacterBase>());
                monster.enemyPartyMember.AddRange(playerData.Cast<BattleCharacterBase>());
            }

            playerEscapeFailedCount = 0;

            // 状態異常表示テスト
            //ChangeStatusAilment(playerData[0], StatusAilments.FASCINATION);

            // このタイミングで同期処理で読み込めるのはWINDOWSだけ(Unityではフリーズする)
#if WINDOWS
            var coroutine = battleViewer.SetBackGround(owner.mapScene.map.getBattleBg(catalog, settings));
            while (coroutine.MoveNext()) ;
#endif
            if (battleViewer is BattleViewer3D)
                ((BattleViewer3D)battleViewer).SetBackGroundCenter(settings.battleBgCenterX, settings.battleBgCenterY);
            battleViewer.BattleStart(playerData, enemyMonsterData);
            battleViewer.LoadResourceData(catalog, gameSettings);

            resultViewer.LoadResourceData(catalog);

            CheckBattleCharacterDown();

            fadeScreenColorTweener.Begin(new Color(Color.Gray, 255), new Color(Color.Gray, 0), 10);

            var bg = catalog.getItemFromGuid(settings.battleBg) as Common.Resource.BattleBackground;
            if (bg != null)
                SetBackGroundImage(Graphics.LoadImage(bg.path));
            battleEvents = new BattleEventController();
            battleEvents.init(this, owner.data, catalog, playerData, enemyMonsterData);
        }

        public BattleEnemyData addEnemyData(Guid guid, Point? layout = null, int index = -1)
        {
            var data = new BattleEnemyData();

            if (layout != null)
            {
                data.pos = layout.Value;
                data.arrangmentType = BattleEnemyData.MonsterArrangementType.Manual;
            }

            var monster = catalog.getItemFromGuid<Rom.Monster>(guid);
            var monsterRes = catalog.getItemFromGuid(monster.graphic) as Common.Resource.ResourceItem;

            data.monster = monster;
            data.EscapeSuccessBaseParcent = 100;
            data.EscapeSuccessMessage = string.Format(gameSettings.glossary.battle_enemy_escape, monster.name);
            data.ExecuteCommandTurnCount = 1;
            data.image = monsterRes;
            data.imageId = (monsterRes is Common.Resource.Monster ? Graphics.LoadImage(monsterRes.path) : -1);
            data.imageAlpha = 1.0f;
            data.SetParameters(monster);
            enemyMonsterData.Add(data);
            
            if (index < 0)
                data.UniqueID = enemyMonsterData.Count;
            else
            {
                // まずは探して解放する
                var old = enemyMonsterData.FirstOrDefault(x => x.UniqueID == index);
                if(old != null)
                {
                    disposeEnemy(old);
                    enemyMonsterData.Remove(old);
                }

                data.UniqueID = index;
            }

            return data;
        }

        public BattlePlayerData addPlayerData(Hero hero)
        {
            var data = new BattlePlayerData();

            data.UniqueID = idNumber;

            var face = catalog.getItemFromGuid(hero.rom.face) as Resource.Face;

            data.setFaceImage(face);
            
            data.player = hero;
            data.ExecuteCommandTurnCount = 1;

            data.EscapeSuccessBaseParcent = 0;
            data.EscapeSuccessMessage = gameSettings.glossary.battle_escape;

            data.Status = data.player.statusAilments;
            data.battleStatusData = new BattleStatusWindowDrawer.StatusData();
            data.startStatusData = new BattleStatusWindowDrawer.StatusData();
            data.nextStatusData = new BattleStatusWindowDrawer.StatusData();

            data.calcHeroLayout(playerData.Count);

            data.SetParameters(hero, owner.debugSettings.battleHpAndMpMax, owner.debugSettings.battleStatusMax, party);

            data.startStatusData.HitPoint = data.nextStatusData.HitPoint = data.HitPoint;
            data.startStatusData.MagicPoint = data.nextStatusData.MagicPoint = data.MagicPoint;

            playerData.Add(data);

            idNumber++;

            return data;
        }

        public void ReleaseImageData()
        {
            battleEvents.term();

            foreach (var player in playerData)
            {
                disposePlayer(player);
            }

            foreach (var enemyMonster in enemyMonsterData)
            {
                disposeEnemy(enemyMonster);
            }

            foreach (int iconImageId in iconTable.Values)
            {
                Graphics.UnloadImage(iconImageId);
            }

            if (backGroundImageId >= 0)
            {
                Graphics.UnloadImage(backGroundImageId);
                backGroundImageId = -1;//#23959 念のため初期化
            }

            battleViewer.ReleaseResourceData();
            resultViewer.ReleaseResourceData();

            BattleStartEvents = null;
            BattleResultWinEvents = null;
            BattleResultLoseGameOverEvents = null;
            BattleResultEscapeEvents = null;
        }

        private void disposePlayer(BattlePlayerData player)
        {
            if (player.characterImageId >= 0) Graphics.UnloadImage(player.characterImageId);

            player.disposeFace();

            if (player.positiveEffectDrawers != null)
            {
                foreach (var effectDrawer in player.positiveEffectDrawers) effectDrawer.finalize();
            }

            if (player.negativeEffectDrawers != null)
            {
                foreach (var effectDrawer in player.negativeEffectDrawers) effectDrawer.finalize();
            }

            if (player.statusEffectDrawers != null)
            {
                foreach (var effectDrawer in player.statusEffectDrawers) effectDrawer.finalize();
            }
        }

        private void disposeEnemy(BattleEnemyData enemyMonster)
        {
            Graphics.UnloadImage(enemyMonster.imageId);

            if (enemyMonster.positiveEffectDrawers != null)
            {
                foreach (var effectDrawer in enemyMonster.positiveEffectDrawers) effectDrawer.finalize();
            }

            if (enemyMonster.negativeEffectDrawers != null)
            {
                foreach (var effectDrawer in enemyMonster.negativeEffectDrawers) effectDrawer.finalize();
            }

            if (enemyMonster.statusEffectDrawers != null)
            {
                foreach (var effectDrawer in enemyMonster.statusEffectDrawers) effectDrawer.finalize();
            }
        }

        public void ApplyDebugSetting()
        {
            foreach (var player in playerData)
            {
                if (player.Status == StatusAilments.DOWN)
                {
                    player.Status = StatusAilments.NONE;

                    player.ChangeEmotion(Resource.Face.FaceType.FACE_NORMAL);

                    player.characterImageColor = CharacterFaceImageDrawer.GetImageColor(player);
                }

                player.SetParameters(party.members[party.members.IndexOf(player.player)], owner.debugSettings.battleHpAndMpMax, owner.debugSettings.battleStatusMax, party);

                SetBattleStatusData(player);
            }
        }

        private int CalcAttackWithWeaponDamage(BattleCharacterBase attacker, BattleCharacterBase target, AttackAttributeType attackAttribute, bool isCritical, List<BattleDamageTextInfo> textInfo)
        {
            float weaponDamage = (attacker.Attack) / 2.5f - (target.Defense) / ((isCritical) ? 8.0f : 4.0f);

            float elementDamage = 0;

            switch (attackAttribute)
            {
                case AttackAttributeType.None:
                case AttackAttributeType.A:
                case AttackAttributeType.B:
                case AttackAttributeType.C:
                case AttackAttributeType.D:
                case AttackAttributeType.E:
                case AttackAttributeType.F:
                    elementDamage = attacker.ElementAttack * target.ResistanceAttackAttributeParcent((int)attackAttribute);
                    break;
            }

            if (weaponDamage < 0)
                weaponDamage = 0;

            float totalDamage = (weaponDamage + elementDamage) * (1.0f - (float)battleRandom.NextDouble() / 10);
            
            // 式がある？
            if (attacker is BattlePlayerData)
            {
                var weapon = ((BattlePlayerData)attacker).player.equipments[0];
                if (weapon != null && !string.IsNullOrEmpty(weapon.weapon.formula))
                {
                    totalDamage = EvalFormula(weapon.weapon.formula, attacker, target, (int)attackAttribute);
                }
            }

            int damage = (int)(totalDamage * target.DamageRate * (isCritical ? 1.5f : 1.0f));

            if (damage < -DamageMax) damage = -DamageMax;
            if (damage > DamageMax) damage = DamageMax;

            BattleDamageTextInfo.TextType textType = BattleDamageTextInfo.TextType.HitPointDamage;
            string text = damage.ToString();
            if (damage < 0)
            {
                textType = BattleDamageTextInfo.TextType.HitPointHeal;
                text = (-damage).ToString();
            }
            else if (isCritical)
            {
                textType = BattleDamageTextInfo.TextType.CriticalDamage;
            }

            textInfo.Add(new BattleDamageTextInfo(textType, target, text));

            return damage;
        }

        private float EvalFormula(string formula, BattleCharacterBase attacker, BattleCharacterBase target, int attackAttribute)
        {
            // 式をパースして部品に分解する
            var words = Util.ParseFormula(formula);

            // 逆ポーランド記法に並べ替える
            words = Util.SortToRPN(words);

            return CalcRPN(words, attacker, target, attackAttribute);
        }

        internal float GetRandom(float max, float min)
        {
            if (min > max)
            {
                var tmp = min;
                min = max;
                max = tmp;
            }

            return (float)battleRandom.NextDouble() * (max - min) + min;
        }

        public float CalcRPN(List<string> words, BattleCharacterBase attacker, BattleCharacterBase target, int attackAttribute)
        {
            var stack = new Stack<float>();
            stack.Push(0);

            float a;
            foreach (var word in words)
            {
                switch (word)
                {
                    case "min":
                        stack.Push(Math.Min(stack.Pop(), stack.Pop()));
                        break;
                    case "max":
                        stack.Push(Math.Max(stack.Pop(), stack.Pop()));
                        break;
                    case "rand":
                        stack.Push(GetRandom(stack.Pop(), stack.Pop()));
                        break;
                    case "*":
                        stack.Push(stack.Pop() * stack.Pop());
                        break;
                    case "/":
                        a = stack.Pop();
                        try
                        {
                            stack.Push(stack.Pop() / a);
                        }
                        catch (DivideByZeroException e)
                        {
#if WINDOWS
                            System.Windows.Forms.MessageBox.Show(e.Message);
#else
                            UnityEngine.Debug.Log(e.Message);
#endif
                            stack.Push(0);
                        }
                        break;
                    case "%":
                        a = stack.Pop();
                        try
                        {
                            stack.Push(stack.Pop() % a);
                        }
                        catch (DivideByZeroException e)
                        {
#if WINDOWS
                            System.Windows.Forms.MessageBox.Show(e.Message);
#else
                            UnityEngine.Debug.Log(e.Message);
#endif
                            stack.Push(0);
                        }
                        break;
                    case "+":
                        stack.Push(stack.Pop() + stack.Pop());
                        break;
                    case "-":
                        a = stack.Pop();
                        stack.Push(stack.Pop() - a);
                        break;
                    case ",":
                        break;
                    default:
                        // 数値や変数はスタックに積む

                        float num;
                        if(float.TryParse(word, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out num))
                        {
                            // 数値
                            stack.Push(num);
                        }
                        else
                        {
                            // 変数
                            stack.Push(parseBattleNum(word, attacker, target, attackAttribute));
                        }
                        break;
                }
            }

            return stack.Pop();
        }

        private float parseBattleNum(string word, BattleCharacterBase attacker, BattleCharacterBase target, int attackAttribute)
        {
            BattleCharacterBase src = null;
            if (word.StartsWith("a."))
                src = attacker;
            else if (word.StartsWith("b."))
                src = target;

            if(src != null)
            {
                word = word.Substring(2, word.Length - 2);

                switch (word)
                {
                    case "lv":
                        return (src is BattlePlayerData) ? ((BattlePlayerData)src).player.level : 1;
                    case "hp":
                        return src.HitPoint;
                    case "mp":
                        return src.MagicPoint;
                    case "mhp":
                        return src.MaxHitPoint;
                    case "mmp":
                        return src.MaxMagicPoint;
                    case "atk":
                        return src.Attack;
                    case "def":
                        return src.Defense;
                    case "spd":
                        return src.Speed;
                    case "mgc":
                        return src.Magic;
                    case "eatk":
                        return src.ElementAttack;
                    case "edef":
                        return target.ResistanceAttackAttributeParcent(attackAttribute);
                }
            }

            return 0;
        }

        private void EffectSkill(BattleCharacterBase effecter, Rom.Skill skill, BattleCharacterBase[] friendEffectTargets, BattleCharacterBase[] enemyEffectTargets, List<BattleDamageTextInfo> textInfo, List<RecoveryStatusInfo> recoveryStatusInfo, out BattleCharacterBase[] friend, out BattleCharacterBase[] enemy)
        {
            var friendEffect = skill.friendEffect;
            var enemyEffect = skill.enemyEffect;
            var option = skill.option;
            int totalHitPointDamage = 0;
            int totalMagicPointDamage = 0;

            var friendEffectCharacters = new List<BattleCharacterBase>();
            var enemyEffectCharacters = new List<BattleCharacterBase>();

            foreach (var target in friendEffectTargets)
            {
                bool isEffect = false;

                // 戦闘不能状態ならばHPとMPを回復させない (スキル効果に「戦闘不能状態を回復」がある場合のみ回復効果を有効とする)
                // |----------------------------------------|--------------|----------------|
                // | ↓スキル効果          効果対象の状態→ | 戦闘不能状態 | それ以外の状態 |
                // |----------------------------------------|--------------|----------------|
                // | 「戦闘不能状態を回復」が含まれている   |     有効     |      無効      |
                // | 「戦闘不能状態を回復」が含まれていない |     無効     |      有効      |
                // |----------------------------------------|--------------|----------------|
                bool isHealParameter = ((target.Status == StatusAilments.DOWN) == friendEffect.down || !skill.option.onlyForDown);

                // HitPoint 回復 or ダメージ
                if ((friendEffect.hitpoint != 0 || friendEffect.hitpointPercent != 0 ||
                    friendEffect.hitpoint_powerParcent != 0 || friendEffect.hitpoint_magicParcent != 0 ||
                    !string.IsNullOrEmpty(friendEffect.hitpointFormula)) && isHealParameter)
                {
                    int effectValue = friendEffect.hitpoint + (int)(friendEffect.hitpointPercent / 100.0f * target.MaxHitPoint) + (int)(friendEffect.hitpoint_powerParcent / 100.0f * effecter.Attack) + (int)(friendEffect.hitpoint_magicParcent / 100.0f * effecter.Magic);
                    
                    if (effectValue > DamageMax)
                        effectValue = DamageMax;
                    if (effectValue < -DamageMax)
                        effectValue = -DamageMax;

                    if (effectValue >= 0)
                    {
                        // 回復効果の場合は属性耐性の計算を行わない
                    }
                    else
                    {
                        switch (target.AttackAttributeTolerance(friendEffect.attribute))
                        {
                            case AttributeToleranceType.Normal:
                            case AttributeToleranceType.Strong:
                            case AttributeToleranceType.Weak:
                                {
                                    float effectValueTmp = effectValue * target.ResistanceAttackAttributeParcent(skill.friendEffect.attribute) * target.DamageRate;
                                    if (effectValueTmp > DamageMax)
                                        effectValueTmp = DamageMax;
                                    if (effectValueTmp < -DamageMax)
                                        effectValueTmp = -DamageMax;
                                    effectValue = (int)effectValueTmp;
                                }
                                break;

                            case AttributeToleranceType.Absorb:
                                {
                                    float effectValueTmp = effectValue * target.ResistanceAttackAttributeParcent(skill.friendEffect.attribute) * target.DamageRate;
                                    if (effectValueTmp > DamageMax)
                                        effectValueTmp = DamageMax;
                                    if (effectValueTmp < -DamageMax)
                                        effectValueTmp = -DamageMax;
                                    effectValue = (int)effectValueTmp;
                                }
                                break;

                            case AttributeToleranceType.Invalid:
                                effectValue = 0;
                                break;
                        }

                    }

                    // 式がある？
                    if (!string.IsNullOrEmpty(friendEffect.hitpointFormula))
                    {
                        effectValue += (int)EvalFormula(friendEffect.hitpointFormula, effecter, target, friendEffect.attribute);
                        if (effectValue > DamageMax)
                            effectValue = DamageMax;
                        if (effectValue < -DamageMax)
                            effectValue = -DamageMax;
                    }

                    target.HitPoint += effectValue;

                    if (effectValue > 0)
                    {
                        textInfo.Add(new BattleDamageTextInfo(BattleDamageTextInfo.TextType.HitPointHeal, target, effectValue.ToString()));
                    }
                    else
                    {
                        totalHitPointDamage += Math.Abs(effectValue);
                        textInfo.Add(new BattleDamageTextInfo(BattleDamageTextInfo.TextType.HitPointDamage, target, Math.Abs(effectValue).ToString()));
                    }

                    isEffect = true;
                }

                // MagicPoint 回復
                if ((friendEffect.magicpoint != 0 || friendEffect.magicpointPercent != 0) && isHealParameter)
                {
                    int effectValue = ((friendEffect.magicpoint) + (int)(friendEffect.magicpointPercent / 100.0f * target.MaxMagicPoint));

                    if (effectValue > DamageMax)
                        effectValue = DamageMax;
                    if (effectValue < -DamageMax)
                        effectValue = -DamageMax;

                    target.MagicPoint += effectValue;

                    if (effectValue > 0)
                    {
                        textInfo.Add(new BattleDamageTextInfo(BattleDamageTextInfo.TextType.MagicPointHeal, target, effectValue.ToString()));
                    }
                    else
                    {
                        totalMagicPointDamage += Math.Abs(effectValue);
                        textInfo.Add(new BattleDamageTextInfo(BattleDamageTextInfo.TextType.MagicPointDamage, target, Math.Abs(effectValue).ToString()));
                    }

                    isEffect = true;
                }


                // 状態異常 回復
                if (friendEffect.poison && target.Status.HasFlag(StatusAilments.POISON))
                {
                    target.RecoveryStatusAilment(StatusAilments.POISON);
                    recoveryStatusInfo.Add(new RecoveryStatusInfo(target, StatusAilments.POISON));

                    isEffect = true;
                }

                if (friendEffect.sleep && target.Status.HasFlag(StatusAilments.SLEEP))
                {
                    target.RecoveryStatusAilment(StatusAilments.SLEEP);
                    recoveryStatusInfo.Add(new RecoveryStatusInfo(target, StatusAilments.SLEEP));

                    target.selectedBattleCommandType = BattleCommandType.Cancel;

                    isEffect = true;
                }

                if (friendEffect.paralysis && target.Status.HasFlag(StatusAilments.PARALYSIS))
                {
                    target.RecoveryStatusAilment(StatusAilments.PARALYSIS);
                    recoveryStatusInfo.Add(new RecoveryStatusInfo(target, StatusAilments.PARALYSIS));

                    target.selectedBattleCommandType = BattleCommandType.Cancel;

                    isEffect = true;
                }

                if (friendEffect.confusion && target.Status.HasFlag(StatusAilments.CONFUSION))
                {
                    target.RecoveryStatusAilment(StatusAilments.CONFUSION);
                    recoveryStatusInfo.Add(new RecoveryStatusInfo(target, StatusAilments.CONFUSION));

                    target.selectedBattleCommandType = BattleCommandType.Cancel;

                    isEffect = true;
                }

                if (friendEffect.fascination && target.Status.HasFlag(StatusAilments.FASCINATION))
                {
                    target.RecoveryStatusAilment(StatusAilments.FASCINATION);
                    recoveryStatusInfo.Add(new RecoveryStatusInfo(target, StatusAilments.FASCINATION));

                    target.selectedBattleCommandType = BattleCommandType.Cancel;

                    isEffect = true;
                }

                if (friendEffect.down && target.Status == StatusAilments.DOWN)
                {
                    target.RecoveryStatusAilment(StatusAilments.DOWN);
                    recoveryStatusInfo.Add(new RecoveryStatusInfo(target, StatusAilments.DOWN));

                    if (target is BattleEnemyData)
                        battleViewer.AddFadeInCharacter(target as BattleEnemyData);

                    isEffect = true;
                }


                // パラメータ変動
                if (friendEffect.power != 0 && Math.Abs(friendEffect.power) >= Math.Abs(target.PowerEnhancement))       // 腕力
                {
                    target.PowerEnhancement = friendEffect.power;

                    isEffect = true;
                }

                if (friendEffect.vitality != 0 && Math.Abs(friendEffect.vitality) >= Math.Abs(target.VitalityEnhancement)) // 体力
                {
                    target.VitalityEnhancement = friendEffect.vitality;

                    isEffect = true;
                }

                if (friendEffect.magic != 0 && Math.Abs(friendEffect.magic) >= Math.Abs(target.MagicEnhancement))       // 魔力
                {
                    target.MagicEnhancement = friendEffect.magic;

                    isEffect = true;
                }

                if (friendEffect.speed != 0 && Math.Abs(friendEffect.speed) >= Math.Abs(target.SpeedEnhancement))        // 素早さ
                {
                    target.SpeedEnhancement = friendEffect.speed;

                    isEffect = true;
                }

                if (friendEffect.dexterity != 0 && Math.Abs(friendEffect.dexterity) >= Math.Abs(target.DexterityEnhancement))    // 命中
                {
                    target.DexterityEnhancement = friendEffect.dexterity;

                    isEffect = true;
                }

                if (friendEffect.evation != 0 && Math.Abs(friendEffect.evation) >= Math.Abs(target.EvationEnhancement))  // 回避
                {
                    target.EvationEnhancement = friendEffect.evation;

                    isEffect = true;
                }


                // 各属性耐性
                if (friendEffect.attrAdefense != 0 && Math.Abs(friendEffect.attrAdefense) >= Math.Abs(target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.A]))
                {
                    target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.A] = friendEffect.attrAdefense;

                    isEffect = true;
                }
                if (friendEffect.attrBdefense != 0 && Math.Abs(friendEffect.attrBdefense) >= Math.Abs(target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.B]))
                {
                    target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.B] = friendEffect.attrBdefense;

                    isEffect = true;
                }
                if (friendEffect.attrCdefense != 0 && Math.Abs(friendEffect.attrCdefense) >= Math.Abs(target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.C]))
                {
                    target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.C] = friendEffect.attrCdefense;

                    isEffect = true;
                }
                if (friendEffect.attrDdefense != 0 && Math.Abs(friendEffect.attrDdefense) >= Math.Abs(target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.D]))
                {
                    target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.D] = friendEffect.attrDdefense;

                    isEffect = true;
                }
                if (friendEffect.attrEdefense != 0 && Math.Abs(friendEffect.attrEdefense) >= Math.Abs(target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.E]))
                {
                    target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.E] = friendEffect.attrEdefense;

                    isEffect = true;
                }
                if (friendEffect.attrFdefense != 0 && Math.Abs(friendEffect.attrFdefense) >= Math.Abs(target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.F]))
                {
                    target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.F] = friendEffect.attrFdefense;

                    isEffect = true;
                }

                if (target.HitPoint <= 0)
                {
                    if (totalHitPointDamage > 0)
                    {
                        // ダメージ効果があった場合は戦闘不能を付与する
                        target.Status = StatusAilments.DOWN;
                    }
                    else if (target.Status != StatusAilments.DOWN)
                    {
                        // 「戦闘不能」から回復したがHPが0のままだともう一度「戦闘不能」と扱われてしまうのでHPを回復させておく
                        target.HitPoint = 1;
                    }
                }

                // 上限チェック
                if (target.HitPoint > target.MaxHitPoint) target.HitPoint = target.MaxHitPoint;
                if (target.MagicPoint > target.MaxMagicPoint) target.MagicPoint = target.MaxMagicPoint;

                // 下限チェック
                if (target.HitPoint < 0) target.HitPoint = 0;
                if (target.MagicPoint < 0) target.MagicPoint = 0;

                if (isEffect)
                {
                    friendEffectCharacters.Add(target);
                }

                if (totalHitPointDamage > 0)
                    target.CommandReactionType = ReactionType.Damage;
                else if (totalHitPointDamage < 0)
                    target.CommandReactionType = ReactionType.Heal;
                else
                    target.CommandReactionType = ReactionType.None;
            }

            // 対象にスキル効果を反映
            foreach (var target in enemyEffectTargets)
            {
                bool isEffect = false;
                bool isDisplayMiss = false;

                if (enemyEffect.hitRate > battleRandom.Next(DexterityMax))
                {
                    // 状態異常 与える
                    if (enemyEffect.poison)
                    {
                        if (target.ResistanceAilmentStatus[(int)StatusAilmentIndex.Poison] <= battleRandom.Next(DexterityMax))
                        {
                            isEffect = target.SetStatusAilment(StatusAilments.POISON);
                        }
                        else
                        {
                            isDisplayMiss = true;
                        }
                    }

                    if (enemyEffect.sleep)
                    {
                        if (target.ResistanceAilmentStatus[(int)StatusAilmentIndex.Sleep] <= battleRandom.Next(DexterityMax))
                        {
                            isEffect = target.SetStatusAilment(StatusAilments.SLEEP);
                            if (isEffect && target.selectedBattleCommandType != BattleCommandType.Skip) target.selectedBattleCommandType = BattleCommandType.Cancel;
                        }
                        else
                        {
                            isDisplayMiss = true;
                        }
                    }

                    if (enemyEffect.paralysis)
                    {
                        if (target.ResistanceAilmentStatus[(int)StatusAilmentIndex.Paralysis] <= battleRandom.Next(DexterityMax))
                        {
                            isEffect = target.SetStatusAilment(StatusAilments.PARALYSIS);
                            if (isEffect && target.selectedBattleCommandType != BattleCommandType.Skip) target.selectedBattleCommandType = BattleCommandType.Cancel;
                        }
                        else
                        {
                            isDisplayMiss = true;
                        }
                    }

                    if (enemyEffect.confusion)
                    {
                        if (target.ResistanceAilmentStatus[(int)StatusAilmentIndex.Confusion] <= battleRandom.Next(DexterityMax))
                        {
                            isEffect = target.SetStatusAilment(StatusAilments.CONFUSION);
                            if (isEffect && target.selectedBattleCommandType != BattleCommandType.Skip) target.selectedBattleCommandType = BattleCommandType.Cancel;
                        }
                        else
                        {
                            isDisplayMiss = true;
                        }
                    }

                    if (enemyEffect.fascination)
                    {
                        if (target.ResistanceAilmentStatus[(int)StatusAilmentIndex.Fascination] <= battleRandom.Next(DexterityMax))
                        {
                            isEffect = target.SetStatusAilment(StatusAilments.FASCINATION);
                            if (isEffect && target.selectedBattleCommandType != BattleCommandType.Skip) target.selectedBattleCommandType = BattleCommandType.Cancel;
                        }
                        else
                        {
                            isDisplayMiss = true;
                        }
                    }

                    if (enemyEffect.down)
                    {
                        if (target.ResistanceAilmentStatus[(int)StatusAilmentIndex.Down] <= battleRandom.Next(DexterityMax))
                        {
                            target.Status = StatusAilments.NONE;
                            target.HitPoint = 0;

                            isEffect = true;
                        }
                        else
                        {
                            isDisplayMiss = true;
                        }
                    }

                    // HitPoint 回復 or ダメージ
                    if (enemyEffect.hitpoint != 0 || enemyEffect.hitpointPercent != 0 ||
                        enemyEffect.hitpoint_powerParcent != 0 || enemyEffect.hitpoint_magicParcent != 0 ||
                        !string.IsNullOrEmpty(enemyEffect.hitpointFormula))
                    {
                        int damage = (enemyEffect.hitpoint) + (int)(enemyEffect.hitpointPercent / 100.0f * target.MaxHitPoint) + (int)(enemyEffect.hitpoint_powerParcent / 100.0f * effecter.Attack) + (int)(enemyEffect.hitpoint_magicParcent / 100.0f * effecter.Magic);

                        if (damage > DamageMax)
                            damage = DamageMax;
                        if (damage < -DamageMax)
                            damage = -DamageMax;

                        if (damage >= 0)
                        {
                            switch (target.AttackAttributeTolerance(enemyEffect.attribute))
                            {
                                case AttributeToleranceType.Normal:
                                case AttributeToleranceType.Strong:
                                case AttributeToleranceType.Weak:
                                    {
                                        float effectValue = damage * target.ResistanceAttackAttributeParcent(enemyEffect.attribute) * target.DamageRate;
                                        if (effectValue > DamageMax)
                                            effectValue = DamageMax;
                                        if (effectValue < -DamageMax)
                                            effectValue = -DamageMax;
                                        damage = (int)effectValue;
                                    }
                                    break;

                                case AttributeToleranceType.Absorb:
                                    {
                                        float effectValue = damage * target.ResistanceAttackAttributeParcent(enemyEffect.attribute) * target.DamageRate;
                                        if (effectValue > DamageMax)
                                            effectValue = DamageMax;
                                        if (effectValue < -DamageMax)
                                            effectValue = -DamageMax;
                                        damage = (int)effectValue;
                                    }
                                    break;

                                case AttributeToleranceType.Invalid:
                                    damage = 0;
                                    break;
                            }
                        }
                        else
                        {
                            // 回復効果だったときは耐性計算を行わない
                        }

                        // 式がある？
                        if (!string.IsNullOrEmpty(enemyEffect.hitpointFormula))
                        {
                            damage += (int)(EvalFormula(enemyEffect.hitpointFormula, effecter, target, enemyEffect.attribute) * target.DamageRate);
                            if (damage > DamageMax)
                                damage = DamageMax;
                            if (damage < -DamageMax)
                                damage = -DamageMax;
                        }

                        if (damage >= 0)
                        {
                            // 攻撃
                            if (skill.option.drain) damage = Math.Min(damage, target.HitPoint);

                            target.HitPoint -= damage;

                            totalHitPointDamage += Math.Abs(damage);
                            textInfo.Add(new BattleDamageTextInfo(BattleDamageTextInfo.TextType.HitPointDamage, target, damage.ToString()));
                        }
                        else
                        {
                            // 回復
                            target.HitPoint -= damage;
                            textInfo.Add(new BattleDamageTextInfo(BattleDamageTextInfo.TextType.HitPointHeal, target, (-damage).ToString()));
                        }

                        isEffect = true;
                    }

                    // MagicPoint 減少
                    if (enemyEffect.magicpoint != 0 || enemyEffect.magicpointPercent != 0)
                    {
                        int damage = (enemyEffect.magicpoint) + (int)(enemyEffect.magicpointPercent / 100.0f * target.MaxMagicPoint);

                        if (damage > DamageMax)
                            damage = DamageMax;
                        if (damage < -DamageMax)
                            damage = -DamageMax;

                        if (skill.option.drain) damage = Math.Min(damage, target.MagicPoint);
                        
                        if (damage >= 0)
                        {
                            // 攻撃
                            if (skill.option.drain) damage = Math.Min(damage, target.HitPoint);

                            target.MagicPoint -= damage;

                            totalMagicPointDamage += Math.Abs(damage);
                            textInfo.Add(new BattleDamageTextInfo(BattleDamageTextInfo.TextType.MagicPointDamage, target, damage.ToString()));
                        }
                        else
                        {
                            // 回復
                            target.MagicPoint -= damage;
                            textInfo.Add(new BattleDamageTextInfo(BattleDamageTextInfo.TextType.MagicPointHeal, target, (-damage).ToString()));
                        }

                        isEffect = true;
                    }

                    // パラメータ変動
                    if (enemyEffect.power != 0 && Math.Abs(enemyEffect.power) >= Math.Abs(target.PowerEnhancement))     // 腕力
                    {
                        target.PowerEnhancement = -enemyEffect.power;

                        isEffect = true;
                    }

                    if (enemyEffect.vitality != 0 && Math.Abs(enemyEffect.vitality) >= Math.Abs(target.VitalityEnhancement))    // 体力
                    {
                        target.VitalityEnhancement = -enemyEffect.vitality;

                        isEffect = true;
                    }

                    if (enemyEffect.magic != 0 && Math.Abs(enemyEffect.magic) >= Math.Abs(target.MagicEnhancement))     // 魔力
                    {
                        target.MagicEnhancement = -enemyEffect.magic;

                        isEffect = true;
                    }

                    if (enemyEffect.speed != 0 && Math.Abs(enemyEffect.speed) >= Math.Abs(target.SpeedEnhancement))      // 素早さ
                    {
                        target.SpeedEnhancement = -enemyEffect.speed;

                        isEffect = true;
                    }

                    if (enemyEffect.dexterity != 0 && Math.Abs(enemyEffect.dexterity) >= Math.Abs(target.DexterityEnhancement))  // 命中
                    {
                        target.DexterityEnhancement = -enemyEffect.dexterity;

                        isEffect = true;
                    }

                    if (enemyEffect.evation != 0 && Math.Abs(enemyEffect.evation) >= Math.Abs(target.EvationEnhancement))    // 回避
                    {
                        target.EvationEnhancement = -enemyEffect.evation;

                        isEffect = true;
                    }

                    // 各属性耐性
                    if (enemyEffect.attrAdefense != 0 && Math.Abs(enemyEffect.attrAdefense) >= Math.Abs(target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.A]))
                    {
                        target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.A] = -enemyEffect.attrAdefense;

                        isEffect = true;
                    }
                    if (enemyEffect.attrBdefense != 0 && Math.Abs(enemyEffect.attrBdefense) >= Math.Abs(target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.B]))
                    {
                        target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.B] = -enemyEffect.attrBdefense;

                        isEffect = true;
                    }
                    if (enemyEffect.attrCdefense != 0 && Math.Abs(enemyEffect.attrCdefense) >= Math.Abs(target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.C]))
                    {
                        target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.C] = -enemyEffect.attrCdefense;

                        isEffect = true;
                    }
                    if (enemyEffect.attrDdefense != 0 && Math.Abs(enemyEffect.attrDdefense) >= Math.Abs(target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.D]))
                    {
                        target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.D] = -enemyEffect.attrDdefense;

                        isEffect = true;
                    }
                    if (enemyEffect.attrEdefense != 0 && Math.Abs(enemyEffect.attrEdefense) >= Math.Abs(target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.E]))
                    {
                        target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.E] = -enemyEffect.attrEdefense;

                        isEffect = true;
                    }
                    if (enemyEffect.attrFdefense != 0 && Math.Abs(enemyEffect.attrFdefense) >= Math.Abs(target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.F]))
                    {
                        target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.F] = -enemyEffect.attrFdefense;

                        isEffect = true;
                    }
                }
                else
                {
                    isDisplayMiss = true;
                }

                // 上限チェック
                if (target.HitPoint > target.MaxHitPoint) target.HitPoint = target.MaxHitPoint;
                if (target.MagicPoint > target.MaxMagicPoint) target.MagicPoint = target.MaxMagicPoint;

                // 下限チェック
                if (target.HitPoint < 0) target.HitPoint = 0;
                if (target.MagicPoint < 0) target.MagicPoint = 0;

                if (isEffect)
                {
                    enemyEffectCharacters.Add(target);
                }

                if (isDisplayMiss)
                {
                    textInfo.Add(new BattleDamageTextInfo(BattleDamageTextInfo.TextType.Miss, target, gameSettings.glossary.battle_miss));
                }

                if (isEffect)
                    target.CommandReactionType = ReactionType.Damage;
                else
                    target.CommandReactionType = ReactionType.None;
            }

            // 与えたダメージ分 自分のHP, MPを回復する
            if (option.drain)
            {
                if (totalHitPointDamage > 0)
                {
                    effecter.HitPoint += totalHitPointDamage;

                    textInfo.Add(new BattleDamageTextInfo(BattleDamageTextInfo.TextType.HitPointHeal, effecter, totalHitPointDamage.ToString()));
                }

                if (totalMagicPointDamage > 0)
                {
                    effecter.MagicPoint += totalMagicPointDamage;

                    textInfo.Add(new BattleDamageTextInfo(BattleDamageTextInfo.TextType.MagicPointHeal, effecter, totalMagicPointDamage.ToString()));
                }
            }

            if (option.selfDestruct)
            {
                effecter.HitPoint = 0;
            }

            // スキル使用者のパラメータ 上限チェック
            if (effecter.HitPoint > effecter.MaxHitPoint) effecter.HitPoint = effecter.MaxHitPoint;
            if (effecter.MagicPoint > effecter.MaxMagicPoint) effecter.MagicPoint = effecter.MaxMagicPoint;

            if (effecter.HitPoint < 0) effecter.HitPoint = 0;
            if (effecter.MagicPoint < 0) effecter.MagicPoint = 0;

            friend = friendEffectCharacters.ToArray();
            enemy = enemyEffectCharacters.ToArray();

            // イベント開始
            if (option.commonExec != Guid.Empty)
            {
                GameMain.instance.mapScene.mapEngine.checkAllSheet(option.commonExec);
                battleEvents.start(option.commonExec);
            }
        }

        private void PaySkillCost(BattleCharacterBase effecter, Rom.Skill skill)
        {
            // スキル発動時のコストとして発動者のHPとMPを消費
            if (skill.option.consumptionHitpoint > 0)
                effecter.HitPoint -= skill.option.consumptionHitpoint;

            if (skill.option.consumptionMagicpoint > 0)
                effecter.MagicPoint -= skill.option.consumptionMagicpoint;

            // 味方だったらアイテムを消費
            if(effecter is BattlePlayerData)
                party.AddItem(skill.option.consumptionItem, -skill.option.consumptionItemAmount);
        }

        private bool UseItem(Rom.Item item, BattleCharacterBase target, List<BattleDamageTextInfo> textInfo, List<RecoveryStatusInfo> recoveryStatusInfo)
        {
            var expendable = item.expendable;
            var status = target.Status;
            bool isUsedItem = false;
            bool enableItemEffect = ((status == StatusAilments.DOWN) == expendable.recoveryDown);


            // 回復
            int healHitPoint = 0;
            int healMagicPoint = 0;

            // HP回復 (固定値)
            if (expendable.hitpoint > 0 && enableItemEffect)
            {
                healHitPoint += expendable.hitpoint;
                isUsedItem = true;
            }

            // HP回復 (割合)
            if (expendable.hitpointPercent > 0 && enableItemEffect)
            {
                healHitPoint += (int)(expendable.hitpointPercent / 100.0f * target.MaxHitPoint);
                isUsedItem = true;
            }

            // MP回復 (固定値)
            if (expendable.magicpoint > 0 && enableItemEffect)
            {
                healMagicPoint += expendable.magicpoint;
                isUsedItem = true;
            }

            // MP回復 (割合)
            if (expendable.magicpointPercent > 0 && enableItemEffect)
            {
                healMagicPoint += (int)(expendable.magicpointPercent / 100.0f * target.MaxMagicPoint);
                isUsedItem = true;
            }

            if (healHitPoint > 0)
            {
                target.HitPoint += healHitPoint;

                textInfo.Add(new BattleDamageTextInfo(BattleDamageTextInfo.TextType.HitPointHeal, target, healHitPoint.ToString()));
            }

            if (healMagicPoint > 0)
            {
                target.MagicPoint += healMagicPoint;

                textInfo.Add(new BattleDamageTextInfo(BattleDamageTextInfo.TextType.MagicPointHeal, target, healMagicPoint.ToString()));
            }

            target.HitPoint = Math.Min(target.HitPoint, target.MaxHitPoint);
            target.MagicPoint = Math.Min(target.MagicPoint, target.MaxMagicPoint);

            // 最大HP 増加
            if (expendable.maxHitpoint > 0 && Math.Abs(expendable.maxHitpoint) >= Math.Abs(target.MaxHitPointEnhance) && enableItemEffect)
            {
                target.MaxHitPointEnhance = expendable.maxHitpoint;
                isUsedItem = true;
            }

            // 最大MP 増加
            if (expendable.maxMagitpoint > 0 && Math.Abs(expendable.maxMagitpoint) >= Math.Abs(target.MaxMagicPointEnhance) && enableItemEffect)
            {
                target.MaxMagicPointEnhance = expendable.maxMagitpoint;
                isUsedItem = true;
            }

            // ステータス 強化
            if (expendable.power > 0 && Math.Abs(expendable.power) >= Math.Abs(target.PowerEnhancement) && enableItemEffect)
            {
                target.PowerEnhancement = expendable.power;
                isUsedItem = true;
            }

            if (expendable.vitality > 0 && Math.Abs(expendable.power) >= Math.Abs(target.PowerEnhancement) && enableItemEffect)
            {
                target.VitalityEnhancement = expendable.vitality;
                isUsedItem = true;
            }
            if (expendable.magic > 0 && Math.Abs(expendable.magic) > Math.Abs(target.MagicEnhancement) && enableItemEffect)
            {
                target.MagicEnhancement = expendable.magic;
                isUsedItem = true;
            }
            if (expendable.speed > 0 && Math.Abs(expendable.speed) >= Math.Abs(target.SpeedEnhancement) && enableItemEffect)
            {
                target.SpeedEnhancement = expendable.speed;
                isUsedItem = true;
            }


            // 状態異常回復
            if (expendable.recoveryPoison && status.HasFlag(StatusAilments.POISON))
            {
                target.RecoveryStatusAilment(StatusAilments.POISON);
                recoveryStatusInfo.Add(new RecoveryStatusInfo(target, StatusAilments.POISON));
                isUsedItem = true;
            }

            if (expendable.recoverySleep && status.HasFlag(StatusAilments.SLEEP))
            {
                target.RecoveryStatusAilment(StatusAilments.SLEEP);
                recoveryStatusInfo.Add(new RecoveryStatusInfo(target, StatusAilments.SLEEP));

                target.selectedBattleCommandType = BattleCommandType.Cancel;

                isUsedItem = true;
            }

            if (expendable.recoveryParalysis && status.HasFlag(StatusAilments.PARALYSIS))
            {
                target.RecoveryStatusAilment(StatusAilments.PARALYSIS);
                recoveryStatusInfo.Add(new RecoveryStatusInfo(target, StatusAilments.PARALYSIS));

                target.selectedBattleCommandType = BattleCommandType.Cancel;

                isUsedItem = true;
            }

            if (expendable.recoveryConfusion && status.HasFlag(StatusAilments.CONFUSION))
            {
                target.RecoveryStatusAilment(StatusAilments.CONFUSION);
                recoveryStatusInfo.Add(new RecoveryStatusInfo(target, StatusAilments.CONFUSION));

                target.selectedBattleCommandType = BattleCommandType.Cancel;

                isUsedItem = true;
            }

            if (expendable.recoveryFascination && status.HasFlag(StatusAilments.FASCINATION))
            {
                target.RecoveryStatusAilment(StatusAilments.FASCINATION);
                recoveryStatusInfo.Add(new RecoveryStatusInfo(target, StatusAilments.FASCINATION));

                target.selectedBattleCommandType = BattleCommandType.Cancel;

                isUsedItem = true;
            }

            // 戦闘不能 回復
            // アイテム使用時の状況で想定されるケース
            // ケース1 : 対象となるキャラクターが戦闘不能時 => そのままアイテムを使用して戦闘可能状態に回復 (実装OK)
            // ケース2 : 対象となるキャラクターが戦闘可能状態だがパーティ内に戦闘不能状態のキャラクターがいる => 対象を変更してアイテムを使用
            // ケース3 : パーティ内に戦闘不能状態のキャラクターが1人もいない => アイテムを使用しない
            if (expendable.recoveryDown)
            {
                if (status == StatusAilments.DOWN)
                {
                    target.RecoveryStatusAilment(StatusAilments.DOWN);

                    if (target is BattleEnemyData)
                        battleViewer.AddFadeInCharacter(target as BattleEnemyData);

                    recoveryStatusInfo.Add(new RecoveryStatusInfo(target, StatusAilments.DOWN));

                    int recoveryHitPoint = 0;

                    recoveryHitPoint += expendable.hitpoint;
                    recoveryHitPoint += (int)(expendable.hitpointPercent / 100.0f * target.MaxHitPoint);

                    if (recoveryHitPoint <= 0)
                    {
                        recoveryHitPoint = 1;

                        textInfo.Add(new BattleDamageTextInfo(BattleDamageTextInfo.TextType.HitPointHeal, target, recoveryHitPoint.ToString()));

                        recoveryHitPoint = Math.Min(recoveryHitPoint, target.MaxHitPoint);

                        target.HitPoint = recoveryHitPoint;
                    }

                    isUsedItem = true;
                }
            }

            // イベント開始
            if (expendable.commonExec != Guid.Empty)
            {
                GameMain.instance.mapScene.mapEngine.checkAllSheet(expendable.commonExec);
                battleEvents.start(expendable.commonExec);
                isUsedItem = true;
            }

            return isUsedItem;
        }

        private void UpdateEnhanceEffect(List<EnhanceEffect> enhanceEffects)
        {
            foreach (var effect in enhanceEffects)
            {
                effect.turnCount++;
                effect.enhanceEffect = (int)(effect.enhanceEffect * effect.diff);
            }

            // 終了条件を満たした効果を無効にする
            enhanceEffects.RemoveAll(effect => effect.type == EnhanceEffect.EnhanceEffectType.TurnEffect && effect.turnCount >= effect.durationTurn);
            enhanceEffects.RemoveAll(effect => effect.type == EnhanceEffect.EnhanceEffectType.DurationEffect && effect.enhanceEffect <= 0);
        }

        private void CheckBattleCharacterDown()
        {
            foreach (var player in playerData)
            {
                if (player.HitPoint <= 0)
                {
                    // 戦闘不能時は実行予定のコマンドをキャンセルする
                    // 同一ターン内で蘇生しても行動できない仕様でOK
                    player.selectedBattleCommandType = BattleCommandType.Nothing_Down;

                    player.ChangeEmotion(Resource.Face.FaceType.FACE_SORROW);

                    player.SetStatusAilment(StatusAilments.DOWN);
                }

                SetBattleStatusData(player);
            }

            foreach (var enemy in enemyMonsterData)
            {
                if (enemy.HitPoint <= 0)
                {
                    enemy.Status = StatusAilments.DOWN;
                    enemy.selectedBattleCommandType = BattleCommandType.Nothing_Down;
                }
            }

            foreach (var player in playerData)
            {
                player.characterImageColor = CharacterFaceImageDrawer.GetImageColor(player);

                battleViewer.SetPlayerStatusEffect(player);
            }

            bool isPlaySE = false;

            foreach (var enemyMonster in enemyMonsterData)
            {
                if ((enemyMonster.HitPoint <= 0 || enemyMonster.Status == StatusAilments.DOWN))
                {
                    if (enemyMonster.imageAlpha > 0)
                    {
                        enemyMonster.HitPoint = 0;

                        enemyMonster.selectedBattleCommandType = BattleCommandType.Cancel;

                        battleViewer.AddFadeOutCharacter(enemyMonster);

                        isPlaySE = true;
                    }
                }
                else
                {
                    if (enemyMonster.imageAlpha < 0)
                    {
                        // ここだとタイミングが遅くなるので、状態異常を付与する段階で行う事にする
                        //battleViewer.AddFadeInCharacter(enemyMonster);
                    }
                }

            }

            if (isPlaySE)
            {
                Audio.PlaySound(owner.se.defeat);
            }

        }
        internal void SetBattleStatusData(BattlePlayerData player)
        {
            player.battleStatusData.Name = player.Name;
            player.battleStatusData.HitPoint = player.HitPoint;
            player.battleStatusData.MagicPoint = player.MagicPoint;
            player.battleStatusData.MaxHitPoint = player.MaxHitPoint;
            player.battleStatusData.MaxMagicPoint = player.MaxMagicPoint;
            player.battleStatusData.StatusAilments = player.Status;

            player.battleStatusData.ParameterStatus = BattleStatusWindowDrawer.StatusIconType.None;
            if (player.PowerEnhancement > 0) player.battleStatusData.ParameterStatus |= BattleStatusWindowDrawer.StatusIconType.PowerUp;
            if (player.VitalityEnhancement > 0) player.battleStatusData.ParameterStatus |= BattleStatusWindowDrawer.StatusIconType.VitalityUp;
            if (player.MagicEnhancement > 0) player.battleStatusData.ParameterStatus |= BattleStatusWindowDrawer.StatusIconType.MagicUp;
            if (player.SpeedEnhancement > 0) player.battleStatusData.ParameterStatus |= BattleStatusWindowDrawer.StatusIconType.SpeedUp;
        }

        internal void SetNextBattleStatus(BattlePlayerData player)
        {
            if (player == null)
                return;

            player.startStatusData.HitPoint = player.battleStatusData.HitPoint;
            player.startStatusData.MagicPoint = player.battleStatusData.MagicPoint;

            player.startStatusData.MaxHitPoint = player.battleStatusData.MaxHitPoint;
            player.startStatusData.MaxMagicPoint = player.battleStatusData.MaxMagicPoint;

            player.nextStatusData.HitPoint = player.HitPoint;
            player.nextStatusData.MagicPoint = player.MagicPoint;

            player.nextStatusData.MaxHitPoint = player.MaxHitPoint;
            player.nextStatusData.MaxMagicPoint = player.MaxMagicPoint;
        }

        internal bool UpdateBattleStatusData(BattlePlayerData player)
        {
            bool isUpdated = false;

            if (player.battleStatusData.HitPoint != player.nextStatusData.HitPoint)
            {
                player.battleStatusData.HitPoint = (int)((player.nextStatusData.HitPoint - player.startStatusData.HitPoint) * statusUpdateTweener.CurrentValue + player.startStatusData.HitPoint);

                isUpdated = true;
            }

            return isUpdated;
        }

        public void Update()
        {
            battleStateFrameCount += GameMain.getRelativeParam60FPS();

            if(IsDrawingBattleScene)
                battleEvents.update();

            UpdateCommandSelect();

            UpdateBattleState();

            foreach (var player in playerData)
            {
                player.Update();
            }

            foreach (var enemyMonster in enemyMonsterData)
            {
                enemyMonster.Update();
            }

            battleViewer.Update(playerData, enemyMonsterData);
        }

        private void UpdateBattleState()
        {
            switch (battleState)
            {
                case BattleState.StartFlash:
                    fadeScreenColorTweener.Update();

                    if (!fadeScreenColorTweener.IsPlayTween)
                    {
                        fadeScreenColorTweener.Begin(new Color(Color.Black, 0), new Color(Color.Black, 255), 30);
                        ChangeBattleState(BattleState.StartFadeOut);
                    }
                    break;
                case BattleState.StartFadeOut:
                    fadeScreenColorTweener.Update();

                    if (!fadeScreenColorTweener.IsPlayTween)
                    {
                        fadeScreenColorTweener.Begin(new Color(Color.Black, 255), new Color(Color.Black, 0), 30);

                        openingBackgroundImageScaleTweener.Clear();
                        openingBackgroundImageScaleTweener.Add(1.2f, 1.0f, 30);
                        openingBackgroundImageScaleTweener.Begin();

                        battleViewer.openingMonsterScaleTweener.Begin(1.2f, 1.0f, 30);

                        battleViewer.openingColorTweener.Begin(new Color(Color.White, 0), new Color(Color.White, 255), 30);

                        if (BattleStartEvents != null)
                        {
                            BattleStartEvents();
                        }

                        battleEvents.start(Rom.Script.Trigger.BATTLE_START);
                    	battleEvents.start(Rom.Script.Trigger.BATTLE_PARALLEL);

                        IsDrawingBattleScene = true;

                        ChangeBattleState(BattleState.StartFadeIn);
                    }
                    break;

                case BattleState.StartFadeIn:
                    fadeScreenColorTweener.Update();

                    openingBackgroundImageScaleTweener.Update();

                    if (!fadeScreenColorTweener.IsPlayTween)
                    {
                        ChangeBattleState(BattleState.BattleStart);
                    }
                    break;

                case BattleState.BattleStart:
                    if (owner.mapScene.isToastVisible())
                        break;

                    battleEvents.start(Rom.Script.Trigger.BATTLE_TURN);
                    ChangeBattleState(BattleState.Wait);
                    break;

                case BattleState.Wait:
                    if (isReady3DCamera() && !battleEvents.isBusy())
                    {
                        var battleResult = CheckBattleFinish();

                        if (battleResult == BattleResultState.NonFinish)
                        {
                            ChangeBattleState(BattleState.SelectActivePlayer);
                        }
                        else
                        {
                            battleEvents.setBattleResult(battleResult);
                            ChangeBattleState(BattleState.StartBattleFinishEvent);
                        }
                    }
                    break;

                case BattleState.PlayerTurnStart:
                    recoveryStatusInfo.Clear();

                    // 1回目のコマンド選択時のみ 状態異常回復判定 & 強化用ステータス減衰
                    foreach (var character in createBattleCharacterList())
                    {
                        var status = character.Status;

                        // 状態異常 回復判定
                        if (status.HasFlag(StatusAilments.SLEEP) || status.HasFlag(StatusAilments.PARALYSIS) || status.HasFlag(StatusAilments.CONFUSION) || status.HasFlag(StatusAilments.FASCINATION))
                        {
                            int recovery = battleRandom.Next(100);
                            const int StatusRecoveryParcent = 30;

                            if (recovery < StatusRecoveryParcent)
                            {
                                if (status.HasFlag(StatusAilments.SLEEP)) recoveryStatusInfo.Add(new RecoveryStatusInfo(character, StatusAilments.SLEEP));
                                if (status.HasFlag(StatusAilments.PARALYSIS)) recoveryStatusInfo.Add(new RecoveryStatusInfo(character, StatusAilments.PARALYSIS));
                                if (status.HasFlag(StatusAilments.CONFUSION)) recoveryStatusInfo.Add(new RecoveryStatusInfo(character, StatusAilments.CONFUSION));
                                if (status.HasFlag(StatusAilments.FASCINATION)) recoveryStatusInfo.Add(new RecoveryStatusInfo(character, StatusAilments.FASCINATION));

                                character.RecoveryStatusAilment(StatusAilments.SLEEP);
                                character.RecoveryStatusAilment(StatusAilments.PARALYSIS);
                                character.RecoveryStatusAilment(StatusAilments.CONFUSION);
                                character.RecoveryStatusAilment(StatusAilments.FASCINATION);
                            }
                        }
                    }

                    foreach (var player in playerData)
                    {
                        UpdateEnhanceEffect(player.attackEnhanceEffects);
                        UpdateEnhanceEffect(player.guardEnhanceEffects);

                        // 強化用のステータスを減衰させる
                        const float DampingRate = 0.8f;
                        player.MaxHitPointEnhance = (int)(player.MaxHitPointEnhance * DampingRate);
                        player.MaxMagicPointEnhance = (int)(player.MaxMagicPointEnhance * DampingRate);
                        player.PowerEnhancement = (int)(player.PowerEnhancement * DampingRate);
                        player.VitalityEnhancement = (int)(player.VitalityEnhancement * DampingRate);
                        player.MagicEnhancement = (int)(player.MagicEnhancement * DampingRate);
                        player.SpeedEnhancement = (int)(player.SpeedEnhancement * DampingRate);
                        player.EvationEnhancement = (int)(player.EvationEnhancement * DampingRate);
                        player.DexterityEnhancement = (int)(player.DexterityEnhancement * DampingRate);
                        for (int i = 0; i < player.ResistanceAttackAttributeEnhance.Length; i++)
                        {
                            player.ResistanceAttackAttributeEnhance[i] = (int)(player.ResistanceAttackAttributeEnhance[i] * DampingRate);
                        }

                        SetBattleStatusData(player);

                        battleViewer.SetPlayerStatusEffect(player);
                    }

                    foreach (var monster in enemyMonsterData)
                    {
                        // 強化用のステータスを減衰させる
                        const float DampingRate = 0.8f;
                        monster.MaxHitPointEnhance = (int)(monster.MaxHitPointEnhance * DampingRate);
                        monster.MaxMagicPointEnhance = (int)(monster.MaxMagicPointEnhance * DampingRate);
                        monster.PowerEnhancement = (int)(monster.PowerEnhancement * DampingRate);
                        monster.VitalityEnhancement = (int)(monster.VitalityEnhancement * DampingRate);
                        monster.MagicEnhancement = (int)(monster.MagicEnhancement * DampingRate);
                        monster.SpeedEnhancement = (int)(monster.SpeedEnhancement * DampingRate);
                        monster.EvationEnhancement = (int)(monster.EvationEnhancement * DampingRate);
                        monster.DexterityEnhancement = (int)(monster.DexterityEnhancement * DampingRate);
                        for (int i = 0; i < monster.ResistanceAttackAttributeEnhance.Length; i++)
                        {
                            monster.ResistanceAttackAttributeEnhance[i] = (int)(monster.ResistanceAttackAttributeEnhance[i] * DampingRate);
                        }
                    }

                    ChangeBattleState(BattleState.CheckTurnRecoveryStatus);

                    break;

                case BattleState.CheckTurnRecoveryStatus:
                    if (recoveryStatusInfo.Count == 0)
                    {
                        battleEvents.start(Rom.Script.Trigger.BATTLE_TURN);
                        ChangeBattleState(BattleState.Wait);
                    }
                    else
                    {
                        battleViewer.SetDisplayMessage(recoveryStatusInfo[0].GetMessage(gameSettings));

                        ChangeBattleState(BattleState.DisplayTurnRecoveryStatus);
                    }
                    break;

                case BattleState.DisplayTurnRecoveryStatus:
                    if (battleStateFrameCount >= 30 || Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE))
                    {
                        recoveryStatusInfo.RemoveAt(0);

                        ChangeBattleState(BattleState.CheckTurnRecoveryStatus);
                    }
                    break;

                case BattleState.SelectActivePlayer:
                    if (commandSelectedMemberCount >= playerData.Count)
                    {
                        commandSelectPlayer = null;

                        commandSelectedMemberCount = 0;

                        ChangeBattleState(BattleState.SetEnemyBattleCommand);
                    }
                    else
                    {
                        commandSelectPlayer = playerData[commandSelectedMemberCount];

                        commandSelectPlayer.commandSelectedCount++;

                        ChangeBattleState(BattleState.CheckCommandSelect);
                    }

                    break;

                case BattleState.CheckCommandSelect:
                    // 現在の状態異常に合わせて行動させる
                    if (commandSelectPlayer.Status.HasFlag(StatusAilments.DOWN))
                    {
                        commandSelectPlayer.selectedBattleCommandType = BattleCommandType.Nothing_Down;

                        ChangeBattleState(BattleState.SetPlayerBattleCommandTarget);
                    }
                    else if (commandSelectPlayer.Status.HasFlag(StatusAilments.SLEEP) || commandSelectPlayer.Status.HasFlag(StatusAilments.PARALYSIS))
                    {
                        commandSelectPlayer.selectedBattleCommandType = BattleCommandType.Nothing;

                        ChangeBattleState(BattleState.SetPlayerBattleCommandTarget);
                    }
                    else if (commandSelectPlayer.Status.HasFlag(StatusAilments.CONFUSION) || commandSelectPlayer.Status.HasFlag(StatusAilments.FASCINATION))
                    {
                        commandSelectPlayer.selectedBattleCommandType = BattleCommandType.Attack;

                        ChangeBattleState(BattleState.SetPlayerBattleCommandTarget);
                    }
                    else if (!commandSelectPlayer.IsCommandSelectable)
                    {
                        commandSelectedMemberCount++;
                        ChangeBattleState(BattleState.SelectActivePlayer);
                    }
                    else
                    {
                        ChangeBattleState(BattleState.SetPlayerBattleCommand);
                    }

                    break;

                case BattleState.SetPlayerBattleCommand:
                    playerBattleCommand.Clear();
                    battleViewer.battleCommandChoiceWindowDrawer.ClearChoiceListData();

                    // 戦闘用コマンドの登録
                    foreach (var guid in commandSelectPlayer.player.rom.battleCommandList)
                    {
                        var command = (Rom.BattleCommand)catalog.getItemFromGuid(guid);

                        playerBattleCommand.Add(command);

                        if (iconTable.ContainsKey(command.icon.guId))
                        {
                            battleViewer.battleCommandChoiceWindowDrawer.AddChoiceData(iconTable[command.icon.guId], command.icon, command.name);
                        }
                        else
                        {
                            battleViewer.battleCommandChoiceWindowDrawer.AddChoiceData(command.name);
                        }
                    }

                    bool isFirstCommandSelectPlayer = (playerData.Any(player => player.IsCommandSelectable) && commandSelectedMemberCount == playerData.IndexOf(playerData.Find(player => player.IsCommandSelectable)));

                    // 1番目にコマンドを選択できるメンバーだけ「逃げる」コマンドを追加する
                    // それ以外のメンバーには「戻る」コマンドを追加する
                    if (isFirstCommandSelectPlayer)
                    {
                        // 「逃げる」コマンドの登録
                        playerBattleCommand.Add(new BattleCommand() { type = BattleCommand.CommandType.ESCAPE });

                        battleViewer.battleCommandChoiceWindowDrawer.AddChoiceData(iconTable[gameSettings.escapeIcon.guId], gameSettings.escapeIcon, gameSettings.glossary.battle_escape_command, escapeAvailable);
                    }
                    else
                    {
                        // 「戻る」コマンドの登録
                        playerBattleCommand.Add(new BattleCommand() { type = BattleCommand.CommandType.BACK });

                        battleViewer.battleCommandChoiceWindowDrawer.AddChoiceData(iconTable[gameSettings.returnIcon.guId], gameSettings.returnIcon, gameSettings.glossary.battle_back);
                    }

                    if (battleViewer.battleCommandChoiceWindowDrawer.ChoiceItemCount < BattlePlayerData.RegisterBattleCommandCountMax)
                    {
                        battleViewer.battleCommandChoiceWindowDrawer.RowCount = BattlePlayerData.RegisterBattleCommandCountMax;
                    }
                    else
                    {
                        battleViewer.battleCommandChoiceWindowDrawer.RowCount = battleViewer.battleCommandChoiceWindowDrawer.ChoiceItemCount;
                    }

                    if (battleViewer.battleCommandChoiceWindowDrawer.ChoiceItemCount > 1)
                    {
                        commandSelectPlayer.characterImageTween.Begin(Vector2.Zero, new Vector2(30, 0), 5);
                        commandSelectPlayer.ChangeEmotion(Resource.Face.FaceType.FACE_ANGER);

                        battleViewer.battleCommandChoiceWindowDrawer.SelectDefaultItem(commandSelectPlayer, battleState);

                        commandSelectPlayer.statusWindowState = StatusWindowState.Active;

                        Vector2 commandWindowPosition = commandSelectPlayer.commandSelectWindowBasePosition;

                        if (battleViewer.battleCommandChoiceWindowDrawer.ChoiceItemCount > BattlePlayerData.RegisterBattleCommandCountMax && commandSelectPlayer.viewIndex >= 2)
                        {
                            commandWindowPosition.Y -= 50;
                        }

                        battleViewer.CommandWindowBasePosition = commandWindowPosition;
                        battleViewer.BallonImageReverse = commandSelectPlayer.isCharacterImageReverse;

                        battleViewer.OpenWindow(WindowType.PlayerCommandWindow);

                        ChangeBattleState(BattleState.SelectPlayerBattleCommand);
                        battleCommandState = SelectBattleCommandState.CommandSelect;
                    }
                    else
                    {
                        commandSelectPlayer.selectedBattleCommandType = BattleCommandType.Nothing;
                        ChangeBattleState(BattleState.SetPlayerBattleCommandTarget);
                    }

                    break;

                case BattleState.SelectPlayerBattleCommand:
                    bool isChange = false;

                    if (battleCommandState == SelectBattleCommandState.CommandEnd && commandSelectPlayer.selectedBattleCommandType != BattleCommandType.Back)
                    {
                        isChange = true;

                        battleCommandState = SelectBattleCommandState.None;

                        if (commandSelectPlayer.selectedBattleCommandType == BattleCommandType.PlayerEscape)
                        {
                            foreach (var player in playerData.Where(player => player != commandSelectPlayer))
                            {
                                player.selectedBattleCommandType = BattleCommandType.Skip;
                            }

                            ChangeBattleState(BattleState.SetEnemyBattleCommand);
                        }
                        else
                        {
                            ChangeBattleState(BattleState.SetPlayerBattleCommandTarget);
                        }
                    }

                    // 自分のコマンド選択をキャンセルしてひとつ前の人のコマンド選択に戻る
                    if (battleCommandState == SelectBattleCommandState.CommandCancel || (battleCommandState == SelectBattleCommandState.CommandEnd && commandSelectPlayer.selectedBattleCommandType == BattleCommandType.Back))
                    {
                        bool isBackCommandSelect = false;
                        int prevPlayerIndex = 0;

                        for (int index = commandSelectedMemberCount - 1; index >= 0; index--)
                        {
                            if (playerData[index].IsCommandSelectable)
                            {
                                isBackCommandSelect = true;
                                prevPlayerIndex = index;
                                break;
                            }
                        }

                        if (isBackCommandSelect)
                        {
                            isChange = true;

                            commandSelectedMemberCount = prevPlayerIndex;
                            ChangeBattleState(BattleState.SetPlayerBattleCommand);
                        }
                        else
                        {
                            battleCommandState = SelectBattleCommandState.CommandSelect;
                        }
                    }

                    if (isChange)
                    {
                        commandSelectPlayer.statusWindowState = StatusWindowState.Wait;
                        commandSelectPlayer.characterImageTween.Begin(Vector2.Zero, 5);
                        commandSelectPlayer.ChangeEmotion(Resource.Face.FaceType.FACE_NORMAL);

                        commandSelectPlayer = playerData[commandSelectedMemberCount];

                        battleViewer.CloseWindow();
                    }

                    break;

                case BattleState.SetPlayerBattleCommandTarget:
                    commandSelectPlayer.targetCharacter = GetTargetCharacters(commandSelectPlayer);

                    commandSelectedMemberCount++;

                    ChangeBattleState(BattleState.SelectActivePlayer);
                    break;

                case BattleState.SetEnemyBattleCommand:
                    foreach (var monsterData in enemyMonsterData)
                    {
                        if(monsterData.selectedBattleCommandType != BattleCommandType.Undecided)
                        {
                            // イベントパネルから行動が強制指定されている場合にここに来る
                        }
                        else if (monsterData.HitPoint > 0)
                        {
                            // 強化能力 更新
                            UpdateEnhanceEffect(monsterData.attackEnhanceEffects);
                            UpdateEnhanceEffect(monsterData.guardEnhanceEffects);

                            int hitPointRate = (int)(monsterData.HitPointParcent * 100);

                            var actionList = monsterData.monster.actionList;
                            var activeAction = actionList.Where(act => act.turn == monsterData.ExecuteCommandTurnCount &&
                                    act.minHPPercent <= hitPointRate && hitPointRate <= act.maxHPPercent);
                            var removeActions = new List<Rom.Monster.ActionPattern>();

                            // MPが足りないスキルを除外するための関数
                            Action filter = () =>
                            {
                                switch (monsterData.monster.aiPattern)
                                {
                                    case Rom.Monster.AIPattern.CLEVER:
                                    case Rom.Monster.AIPattern.TRICKY:
                                        foreach (var act in activeAction)
                                        {
                                            if (act.action == Rom.Monster.ActionType.SKILL)
                                            {
                                                var skill = catalog.getItemFromGuid(act.refByAction) as Rom.Skill;

                                                if (skill.option.consumptionHitpoint >= monsterData.HitPoint || skill.option.consumptionMagicpoint > monsterData.MagicPoint)
                                                {
                                                    removeActions.Add(act);
                                                }
                                            }
                                        }

                                        activeAction = activeAction.Except(removeActions);
                                        break;
                                }
                            };

                            filter();

                            // 現在のターン数で実行できる行動が1つも無ければ標準の行動(0ターンの行動)から選択する
                            if (activeAction.Count() == 0)
                            {
                                activeAction = actionList.Where(act => act.turn == 0 &&
                                    act.minHPPercent <= hitPointRate && hitPointRate <= act.maxHPPercent);

                                // MPが足りないスキルを除外
                                filter();
                            }

                            if (activeAction.Count() > 0)
                            {
                                var executeAction = activeAction.ElementAt(battleRandom.Next(activeAction.Count()));

                                monsterData.commandTargetList.Clear();
                                battleViewer.commandTargetSelector.Clear();

                                switch (executeAction.action)
                                {
                                    case Rom.Monster.ActionType.ATTACK:
                                        foreach (var player in playerData.Where(target => target.HitPoint > 0))
                                        {
                                            monsterData.commandTargetList.Add(player);
                                        }

                                        monsterData.selectedBattleCommandType = BattleCommandType.Attack;
                                        break;

                                    case Rom.Monster.ActionType.CRITICAL:
                                        foreach (var player in playerData.Where(target => target.HitPoint > 0))
                                        {
                                            monsterData.commandTargetList.Add(player);
                                        }

                                        monsterData.selectedBattleCommandType = BattleCommandType.Critical;
                                        break;

                                    case Rom.Monster.ActionType.SKILL:
                                        var skill = (Rom.Skill)catalog.getItemFromGuid(executeAction.refByAction);

                                        // スキルが無かったら何もしないよう修正
                                        if (skill == null)
                                        {
                                            monsterData.selectedBattleCommandType = BattleCommandType.Nothing;
                                            break;
                                        }

                                        monsterData.selectedSkill = skill;
                                        monsterData.selectedBattleCommandType = BattleCommandType.Skill;

                                        switch (monsterData.selectedSkill.option.target)
                                        {
                                            case Rom.TargetType.PARTY_ONE:
                                            case Rom.TargetType.PARTY_ONE_ENEMY_ALL:

                                                foreach (var enemy in enemyMonsterData)
                                                {
                                                    monsterData.commandTargetList.Add(enemy);
                                                }

                                                break;

                                            case Rom.TargetType.ENEMY_ONE:
                                            case Rom.TargetType.PARTY_ALL_ENEMY_ONE:
                                            case Rom.TargetType.SELF_ENEMY_ONE:
                                            case Rom.TargetType.OTHERS_ENEMY_ONE:
                                                foreach (var player in playerData.Where(player => player.HitPoint > 0))
                                                {
                                                    monsterData.commandTargetList.Add(player);
                                                }

                                                break;
                                        }
                                        break;

                                    case Rom.Monster.ActionType.CHARGE:
                                        monsterData.selectedBattleCommand = new BattleCommand();
                                        monsterData.selectedBattleCommand.power = executeAction.option;
                                        monsterData.selectedBattleCommandType = BattleCommandType.Charge;
                                        break;

                                    case Rom.Monster.ActionType.GUARD:
                                        monsterData.selectedBattleCommand = new BattleCommand();
                                        monsterData.selectedBattleCommand.power = executeAction.option;
                                        monsterData.selectedBattleCommandType = BattleCommandType.Guard;
                                        break;

                                    case Rom.Monster.ActionType.ESCAPE:
                                        monsterData.selectedBattleCommandType = BattleCommandType.MonsterEscape;
                                        break;

                                    case Rom.Monster.ActionType.DO_NOTHING:
                                        monsterData.selectedBattleCommandType = BattleCommandType.Nothing;
                                        break;
                                }

                                if (monsterData.commandTargetList.Count > 0)
                                {
                                    foreach (var target in monsterData.commandTargetList)
                                    {
                                        if (target is BattlePlayerData) battleViewer.commandTargetSelector.AddPlayer((BattlePlayerData)target);
                                        else if (target is BattleEnemyData) battleViewer.commandTargetSelector.AddMonster((BattleEnemyData)target);
                                    }

                                    switch (monsterData.monster.aiPattern)
                                    {
                                        case Rom.Monster.AIPattern.NORMAL:
                                        case Rom.Monster.AIPattern.CLEVER:
                                            battleViewer.commandTargetSelector.SetSelect(monsterData.commandTargetList[battleRandom.Next(monsterData.commandTargetList.Count)]);
                                            break;

                                        case Rom.Monster.AIPattern.TRICKY:
                                            if (battleRandom.Next(100) < 75)
                                            {
                                                // 狙えるターゲットの中で最もHPが少ないキャラクターを狙う
                                                battleViewer.commandTargetSelector.SetSelect(monsterData.commandTargetList.OrderBy(target => target.HitPoint).ElementAt(0));
                                            }
                                            else
                                            {
                                                battleViewer.commandTargetSelector.SetSelect(monsterData.commandTargetList[battleRandom.Next(monsterData.commandTargetList.Count)]);
                                            }
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                monsterData.selectedBattleCommandType = BattleCommandType.Nothing;
                            }

                            monsterData.targetCharacter = GetTargetCharacters(monsterData);

                            monsterData.ExecuteCommandTurnCount++;

                            if (actionList.Count == 0 || monsterData.ExecuteCommandTurnCount > actionList.Max(act => act.turn))
                            {
                                monsterData.ExecuteCommandTurnCount = 1;
                            }
                        }
                        else
                        {
                            monsterData.selectedBattleCommandType = BattleCommandType.Nothing_Down;
                        }
                    }

                    commandExecuteMemberCount = 0;

                    var characters = createBattleCharacterList().Where(character => character.selectedBattleCommandType != BattleCommandType.Nothing_Down);

                    // 行動順を決定する
                    // 優先順位
                    // 1.「逃げる」コマンドを選択したキャラクター
                    // 2.「防御」コマンドを選択したキャラクター : 「防御」コマンドを選択したキャラクターが複数いた場合はキャラクターIDの小さい順に行動
                    // 3.それ以外のコマンドを選択したキャラクター : 素早さのステータスが高い順に行動
                    // IEnumerable 型だと遅延評価により参照時に計算されるので1ターンの間に素早さのステータスが変わると同じキャラクターが2回行動したり順番がスキップされる現象が発生するのでList型で順番を固定する
                    battleEntryCharacters.Clear();
                    battleEntryCharacters.AddRange(characters.Where(character => character.selectedBattleCommandType == BattleCommandType.PlayerEscape));
                    battleEntryCharacters.AddRange(characters.Where(character => character.selectedBattleCommandType == BattleCommandType.MonsterEscape));
                    battleEntryCharacters.AddRange(characters.Where(character => character.selectedBattleCommandType == BattleCommandType.Guard).OrderBy(character => character.UniqueID));
                    battleEntryCharacters.AddRange(characters.Where(character => !battleEntryCharacters.Contains(character)).OrderByDescending(character => character.Speed));

                    ChangeBattleState(BattleState.ReadyExecuteCommand);

                    break;

                case BattleState.ReadyExecuteCommand:
                    activeCharacter = battleEntryCharacters[commandExecuteMemberCount];

                    if (activeCharacter.selectedBattleCommandType == BattleCommandType.Skip)
                    {
                        ChangeBattleState(BattleState.CheckBattleCharacterDown1);
                    }
                    else
                    {
                        // 状態異常による行動の変更
                        if (activeCharacter.Status.HasFlag(StatusAilments.SLEEP))
                        {
                            activeCharacter.selectedBattleCommandType = BattleCommandType.Nothing;
                        }
                        else if (activeCharacter.Status.HasFlag(StatusAilments.PARALYSIS))
                        {
                            activeCharacter.selectedBattleCommandType = BattleCommandType.Nothing;
                        }
                        else if (activeCharacter.Status.HasFlag(StatusAilments.FASCINATION))
                        {
                            activeCharacter.selectedBattleCommandType = BattleCommandType.Attack;
                        }
                        else if (activeCharacter.Status.HasFlag(StatusAilments.CONFUSION))
                        {
                            activeCharacter.selectedBattleCommandType = BattleCommandType.Attack;
                        }

                        // クリティカル判定
                        if (activeCharacter.selectedBattleCommandType == BattleCommandType.Attack && (battleRandom.Next(100) < activeCharacter.Critical))
                        {
                            activeCharacter.selectedBattleCommandType = BattleCommandType.Critical;
                        }

                        if (activeCharacter.selectedBattleCommandType == BattleCommandType.Nothing_Down || activeCharacter.selectedBattleCommandType == BattleCommandType.Cancel)
                        {
                            ChangeBattleState(BattleState.BattleFinishCheck1);
                        }
                        else
                        {
                            // 攻撃対象を変える必要があるかチェック
                            CheckAndDoReTarget();

                            activeCharacter.ExecuteCommandStart();

                            displayedStatusAilments = StatusAilments.NONE;

                            ChangeBattleState(BattleState.SetStatusMessageText);
                        }
                    }

                    break;

                case BattleState.SetStatusMessageText:
                    {
                        string message = "";
                        bool isDisplayMessage = false;

                        if (activeCharacter.Status.HasFlag(StatusAilments.SLEEP) && !displayedStatusAilments.HasFlag(StatusAilments.SLEEP))
                        {
                            isDisplayMessage = true;

                            message = string.Format(gameSettings.glossary.battle_sleep, activeCharacter.Name);

                            displayedStatusAilments |= StatusAilments.SLEEP;
                        }
                        else if (activeCharacter.Status.HasFlag(StatusAilments.PARALYSIS) && !displayedStatusAilments.HasFlag(StatusAilments.PARALYSIS))
                        {
                            isDisplayMessage = true;

                            message = string.Format(gameSettings.glossary.battle_paralysis, activeCharacter.Name);

                            displayedStatusAilments |= StatusAilments.PARALYSIS;
                        }
                        else if (activeCharacter.Status.HasFlag(StatusAilments.CONFUSION) && !displayedStatusAilments.HasFlag(StatusAilments.CONFUSION))
                        {
                            isDisplayMessage = true;

                            message = string.Format(gameSettings.glossary.battle_confusion, activeCharacter.Name);

                            displayedStatusAilments |= StatusAilments.CONFUSION;
                        }
                        else if (activeCharacter.Status.HasFlag(StatusAilments.FASCINATION) && !displayedStatusAilments.HasFlag(StatusAilments.FASCINATION))
                        {
                            isDisplayMessage = true;

                            message = string.Format(gameSettings.glossary.battle_fascination, activeCharacter.Name);

                            displayedStatusAilments |= StatusAilments.FASCINATION;
                        }

                        if (isDisplayMessage)
                        {
                            battleViewer.SetDisplayMessage(message);

                            ChangeBattleState(BattleState.DisplayStatusMessage);
                        }
                        else
                        {
                            if (activeCharacter.Status.HasFlag(StatusAilments.SLEEP) || activeCharacter.Status.HasFlag(StatusAilments.PARALYSIS))
                            {
                                ChangeBattleState(BattleState.ExecuteBattleCommand);
                            }
                            else
                            {
                                ChangeBattleState(BattleState.SetCommandMessageText);
                            }
                        }
                    }
                    break;

                case BattleState.DisplayStatusMessage:
                    if (battleStateFrameCount > 30)
                    {
                        battleViewer.CloseWindow();

                        ChangeBattleState(BattleState.SetStatusMessageText);
                    }
                    break;

                case BattleState.SetCommandMessageText:
                    {
                        string message = "";
                        string targetName = "";
                        if (activeCharacter.targetCharacter != null && activeCharacter.targetCharacter.Length > 0)
                            targetName = activeCharacter.targetCharacter[0].Name;

                        switch (activeCharacter.selectedBattleCommandType)
                        {
                            case BattleCommandType.Nothing:
                                message = string.Format(gameSettings.glossary.battle_wait, activeCharacter.Name);
                                break;
                            case BattleCommandType.Attack:
                                message = string.Format(gameSettings.glossary.battle_attack, activeCharacter.Name, "", targetName);
                                break;
                            case BattleCommandType.Critical:
                                message = string.Format(gameSettings.glossary.battle_critical, activeCharacter.Name, "", targetName);
                                break;
                            case BattleCommandType.Guard:
                                message = string.Format(gameSettings.glossary.battle_guard, activeCharacter.Name);
                                break;
                            case BattleCommandType.Charge:
                                message = string.Format(gameSettings.glossary.battle_charge, activeCharacter.Name);
                                break;
                            case BattleCommandType.SameSkillEffect:
                                message = activeCharacter.selectedBattleCommand.name;
                                break;
                            case BattleCommandType.Skill:
                                message = string.Format(gameSettings.glossary.battle_skill, activeCharacter.Name, activeCharacter.selectedSkill.name, targetName);
                                break;
                            case BattleCommandType.Item:
                                message = string.Format(gameSettings.glossary.battle_item, activeCharacter.Name, activeCharacter.selectedItem.item.name, targetName);
                                break;
                            case BattleCommandType.PlayerEscape:
                                message = gameSettings.glossary.battle_escape_command;
                                break;
                            case BattleCommandType.MonsterEscape:
                                message = string.Format(gameSettings.glossary.battle_enemy_escape, activeCharacter.Name);
                                break;
                        }

                        battleViewer.SetDisplayMessage(message);
                    }

                    ChangeBattleState(BattleState.DisplayMessageText);
                    break;

                case BattleState.DisplayMessageText:
                    if ((battleStateFrameCount > 20 || Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE)) && isReady3DCamera() && isReadyActor())
                    {
                        ChangeBattleState(BattleState.ExecuteBattleCommand);
                    }
                    break;

                case BattleState.ExecuteBattleCommand:
                    var damageTextList = new List<BattleDamageTextInfo>();

                    // 攻撃対象を変える必要があるかチェック TODO:元はこの位置で処理していたので、他に影響が出ていないか要確認
                    //CheckAndDoReTarget();

                    activeCharacter.commandFriendEffectCharacters.Clear();
                    activeCharacter.commandEnemyEffectCharacters.Clear();

                    recoveryStatusInfo.Clear();

                    // 混乱・魅了のときはターゲットを強制的に変更する
                    if (activeCharacter.selectedBattleCommandType == BattleCommandType.Attack ||
                        activeCharacter.selectedBattleCommandType == BattleCommandType.Critical)
                    {
                        if (activeCharacter.Status.HasFlag(StatusAilments.CONFUSION))
                        {
                            var targets = activeCharacter.friendPartyMember.Where(character => character.HitPoint > 0).Union(activeCharacter.enemyPartyMember.Where(character => character.HitPoint > 0));

                            activeCharacter.targetCharacter = new[] { targets.ElementAt(battleRandom.Next(targets.Count())) };
                        }
                        else if (activeCharacter.Status.HasFlag(StatusAilments.FASCINATION))
                        {
                            var targets = activeCharacter.friendPartyMember.Where(character => character.HitPoint > 0);

                            activeCharacter.targetCharacter = new[] { targets.ElementAt(battleRandom.Next(targets.Count())) };
                        }
                    }

                    switch (activeCharacter.selectedBattleCommandType)
                    {
                        case BattleCommandType.Attack:
                            foreach (var target in activeCharacter.targetCharacter)
                            {
                                if (activeCharacter.Dexterity * (100 - target.Evation) > battleRandom.Next(DexterityMax * 100))
                                {
                                    target.HitPoint -= CalcAttackWithWeaponDamage(activeCharacter, target, activeCharacter.AttackAttribute, false, damageTextList);

                                    // 攻撃を受けたら「眠り」「混乱」「魅了」の状態異常を解除する
                                    if (target.Status.HasFlag(StatusAilments.SLEEP) || target.Status.HasFlag(StatusAilments.CONFUSION) || target.Status.HasFlag(StatusAilments.FASCINATION))
                                    {
                                        if (target != activeCharacter) target.selectedBattleCommandType = BattleCommandType.Cancel;

                                        if (target.HitPoint > 0)
                                        {
                                            var status = target.Status;

                                            if (status.HasFlag(StatusAilments.SLEEP)) recoveryStatusInfo.Add(new RecoveryStatusInfo(target, StatusAilments.SLEEP));
                                            if (status.HasFlag(StatusAilments.CONFUSION)) recoveryStatusInfo.Add(new RecoveryStatusInfo(target, StatusAilments.CONFUSION));
                                            if (status.HasFlag(StatusAilments.FASCINATION)) recoveryStatusInfo.Add(new RecoveryStatusInfo(target, StatusAilments.FASCINATION));
                                        }

                                        target.RecoveryStatusAilment(StatusAilments.SLEEP);
                                        target.RecoveryStatusAilment(StatusAilments.CONFUSION);
                                        target.RecoveryStatusAilment(StatusAilments.FASCINATION);
                                    }

                                    setAttributeWithWeaponDamage(target, activeCharacter.AttackAttribute, activeCharacter.ElementAttack);

                                    target.CommandReactionType = ReactionType.Damage;

                                    activeCharacter.commandEnemyEffectCharacters.Add(target);
                                }
                                else
                                {
                                    damageTextList.Add(new BattleDamageTextInfo(BattleDamageTextInfo.TextType.Miss, target, gameSettings.glossary.battle_miss));
                                    target.CommandReactionType = ReactionType.None;
                                }
                            }
                            break;

                        case BattleCommandType.Critical:
                            foreach (var target in activeCharacter.targetCharacter)
                            {
                                target.HitPoint -= CalcAttackWithWeaponDamage(activeCharacter, target, activeCharacter.AttackAttribute, true, damageTextList);

                                // 攻撃を受けたら「眠り」「混乱」「魅了」の状態異常を解除する
                                if (target.Status.HasFlag(StatusAilments.SLEEP) || target.Status.HasFlag(StatusAilments.CONFUSION) || target.Status.HasFlag(StatusAilments.FASCINATION))
                                {
                                    if (target != activeCharacter) target.selectedBattleCommandType = BattleCommandType.Cancel;

                                    if (target.HitPoint > 0)
                                    {
                                        var status = target.Status;

                                        if (status.HasFlag(StatusAilments.SLEEP)) recoveryStatusInfo.Add(new RecoveryStatusInfo(target, StatusAilments.SLEEP));
                                        if (status.HasFlag(StatusAilments.CONFUSION)) recoveryStatusInfo.Add(new RecoveryStatusInfo(target, StatusAilments.CONFUSION));
                                        if (status.HasFlag(StatusAilments.FASCINATION)) recoveryStatusInfo.Add(new RecoveryStatusInfo(target, StatusAilments.FASCINATION));
                                    }

                                    target.RecoveryStatusAilment(StatusAilments.SLEEP);
                                    target.RecoveryStatusAilment(StatusAilments.CONFUSION);
                                    target.RecoveryStatusAilment(StatusAilments.FASCINATION);
                                }

                                setAttributeWithWeaponDamage(target, activeCharacter.AttackAttribute, activeCharacter.ElementAttack);

                                target.CommandReactionType = ReactionType.Damage;

                                activeCharacter.commandEnemyEffectCharacters.Add(target);
                            }
                            break;

                        case BattleCommandType.Guard:
                            foreach (var target in activeCharacter.targetCharacter)
                            {
                                target.CommandReactionType = ReactionType.None;

                                // 次の自分のターンが回ってくるまでダメージを軽減
                                // 問題 : 素早さのパラメータが低いと後攻ガードになってしまいガードの意味が無くなってしまう
                                // 解決案 : ガードコマンドを選択した次のターンまでガードを有効にする (実質2ターンの効果に変更する)
                                // TODO : 軽減できるのは物理ダメージだけ? 魔法ダメージはどう扱うのか確認する
                                target.guardEnhanceEffects.Add(new EnhanceEffect(activeCharacter.selectedBattleCommand.power, 1));
                            }
                            break;

                        case BattleCommandType.Charge:
                            foreach (var target in activeCharacter.targetCharacter)
                            {
                                target.CommandReactionType = ReactionType.None;
                                target.attackEnhanceEffects.Add(new EnhanceEffect(activeCharacter.selectedBattleCommand.power, 2));
                            }
                            break;

                        case BattleCommandType.SameSkillEffect:
                            {
                                BattleCharacterBase[] friendEffectTargets, enemyEffectTargets;
                                BattleCharacterBase[] friendEffectedCharacters, enemyEffectedCharacters;

                                var skill = catalog.getItemFromGuid(activeCharacter.selectedBattleCommand.refGuid) as Rom.Skill;

                                activeCharacter.GetSkillTarget(skill, out friendEffectTargets, out enemyEffectTargets);

                                EffectSkill(activeCharacter, skill, friendEffectTargets.ToArray(), enemyEffectTargets.ToArray(), damageTextList, recoveryStatusInfo, out friendEffectedCharacters, out enemyEffectedCharacters);

                                activeCharacter.targetCharacter = friendEffectedCharacters.Union(enemyEffectedCharacters).ToArray();

                                battleEvents.setLastSkillTargetIndex(friendEffectTargets, enemyEffectTargets);

                                activeCharacter.commandFriendEffectCharacters.AddRange(friendEffectedCharacters);
                                activeCharacter.commandEnemyEffectCharacters.AddRange(enemyEffectedCharacters);

                                if (activeCharacter.targetCharacter.Count() == 0)
                                {
                                    activeCharacter.targetCharacter = null;
                                }
                            }
                            break;

                        case BattleCommandType.Skill:
                            // スキル効果対象 再選択
                            {
                                BattleCharacterBase[] friendEffectTargets, enemyEffectTargets;
                                BattleCharacterBase[] friendEffectedCharacters, enemyEffectedCharacters;

                                activeCharacter.GetSkillTarget(activeCharacter.selectedSkill, out friendEffectTargets, out enemyEffectTargets);

                                if (activeCharacter.HitPoint > activeCharacter.selectedSkill.option.consumptionHitpoint &&
                                    activeCharacter.MagicPoint >= activeCharacter.selectedSkill.option.consumptionMagicpoint &&
                                    isQualifiedSkillCostItem(activeCharacter, activeCharacter.selectedSkill))
                                {
                                    PaySkillCost(activeCharacter, activeCharacter.selectedSkill);
                                    EffectSkill(activeCharacter, activeCharacter.selectedSkill, friendEffectTargets.ToArray(), enemyEffectTargets.ToArray(), damageTextList, recoveryStatusInfo, out friendEffectedCharacters, out enemyEffectedCharacters);

                                    activeCharacter.targetCharacter = friendEffectedCharacters.Union(enemyEffectedCharacters).ToArray();

                                    battleEvents.setLastSkillTargetIndex(friendEffectTargets, enemyEffectTargets);

                                    activeCharacter.commandFriendEffectCharacters.AddRange(friendEffectedCharacters);
                                    activeCharacter.commandEnemyEffectCharacters.AddRange(enemyEffectedCharacters);
                                }
                                else
                                {
                                    activeCharacter.targetCharacter = null;
                                }
                            }
                            break;

                        case BattleCommandType.Item:
                            switch (activeCharacter.selectedItem.item.type)
                            {
                                case Rom.ItemType.EXPENDABLE:
                                    if (activeCharacter.targetCharacter != null)
                                    {
                                        foreach (var target in activeCharacter.targetCharacter)
                                        {
                                            if (UseItem(activeCharacter.selectedItem.item, target, damageTextList, recoveryStatusInfo))
                                            {
                                                var selectedItem = activeCharacter.selectedItem;

                                                selectedItem.num--;

                                                // アイテム情報を更新
                                                party.SetItemNum(selectedItem.item.guId, selectedItem.num);
                                            }

                                            target.CommandReactionType = ReactionType.None;

                                            activeCharacter.commandFriendEffectCharacters.Add(target);
                                        }
                                    }
                                    break;

                                case Rom.ItemType.EXPENDABLE_WITH_SKILL:
                                    if (activeCharacter.targetCharacter != null)
                                    {
                                        BattleCharacterBase[] friendEffectTargets, enemyEffectTargets;
                                        BattleCharacterBase[] friendEffectedCharacters, enemyEffectedCharacters;

                                        var skill = catalog.getItemFromGuid(activeCharacter.selectedItem.item.expendableWithSkill.skill) as Common.Rom.Skill;

                                        if (skill != null)
                                        {
                                            activeCharacter.GetSkillTarget(skill, out friendEffectTargets, out enemyEffectTargets);

                                            EffectSkill(activeCharacter, skill, friendEffectTargets.ToArray(), enemyEffectTargets.ToArray(), damageTextList, recoveryStatusInfo, out friendEffectedCharacters, out enemyEffectedCharacters);

                                            activeCharacter.targetCharacter = friendEffectedCharacters.Union(enemyEffectedCharacters).ToArray();

                                            battleEvents.setLastSkillTargetIndex(friendEffectTargets, enemyEffectTargets);

                                            if (activeCharacter.targetCharacter.Count() > 0)
                                            {
                                                party.SetItemNum(activeCharacter.selectedItem.item.guId, activeCharacter.selectedItem.num - 1);

                                                activeCharacter.commandFriendEffectCharacters.AddRange(friendEffectedCharacters);
                                                activeCharacter.commandEnemyEffectCharacters.AddRange(enemyEffectedCharacters);
                                            }
                                        }
                                    }
                                    break;
                            }
                            break;

                        case BattleCommandType.Nothing:
                        case BattleCommandType.Nothing_Down:
                        case BattleCommandType.Cancel:
                        case BattleCommandType.Skip:
                        case BattleCommandType.PlayerEscape:
                        case BattleCommandType.MonsterEscape:
                            activeCharacter.targetCharacter = null;
                            break;
                    }

                    battleViewer.SetDamageTextInfo(damageTextList);

                    if (activeCharacter.targetCharacter != null)
                    {
                        foreach (var target in activeCharacter.targetCharacter)
                        {
                            if (target.HitPoint < 0) target.HitPoint = 0;
                            if (target.HitPoint > target.MaxHitPoint) target.HitPoint = target.MaxHitPoint;

                            if (target.MagicPoint < 0) target.MagicPoint = 0;
                            if (target.MagicPoint > target.MaxMagicPoint) target.MagicPoint = target.MaxMagicPoint;
                        }
                    }

                    switch (activeCharacter.selectedBattleCommandType)
                    {
                        case BattleCommandType.PlayerEscape:
                            ChangeBattleState(BattleState.PlayerChallengeEscape);
                            break;

                        case BattleCommandType.MonsterEscape:
                            Audio.PlaySound(owner.se.escape);
                            ChangeBattleState(BattleState.MonsterEscape);
                            break;
                        default:
                            ChangeBattleState(BattleState.SetCommandEffect);
                            break;
                    }
                    break;

                case BattleState.SetCommandEffect:
                    var friendFffectGuid = Guid.Empty;
                    var enemyEffectGuid = Guid.Empty;

                    switch (activeCharacter.selectedBattleCommandType)
                    {
                        case BattleCommandType.Attack:
                        case BattleCommandType.Critical:
                            enemyEffectGuid = activeCharacter.AttackEffect;
                            break;

                        case BattleCommandType.Guard:
                            break;

                        case BattleCommandType.Charge:
                            break;

                        case BattleCommandType.SameSkillEffect:
                            if (activeCharacter.targetCharacter != null)
                            {
                                var skill = catalog.getItemFromGuid(activeCharacter.selectedBattleCommand.refGuid) as Rom.Skill;

                                switch (skill.option.target)
                                {
                                    case Rom.TargetType.PARTY_ONE:
                                    case Rom.TargetType.PARTY_ALL:
                                    case Rom.TargetType.SELF:
                                    case Rom.TargetType.OTHERS:
                                        friendFffectGuid = skill.friendEffect.effect;
                                        break;

                                    case Rom.TargetType.ENEMY_ONE:
                                    case Rom.TargetType.ENEMY_ALL:
                                        enemyEffectGuid = skill.enemyEffect.effect;
                                        break;

                                    case Rom.TargetType.PARTY_ONE_ENEMY_ALL:
                                    case Rom.TargetType.PARTY_ALL_ENEMY_ONE:
                                    case Rom.TargetType.SELF_ENEMY_ONE:
                                    case Rom.TargetType.SELF_ENEMY_ALL:
                                    case Rom.TargetType.ALL:
                                    case Rom.TargetType.OTHERS_ENEMY_ONE:
                                    case Rom.TargetType.OTHERS_ALL:
                                        friendFffectGuid = skill.friendEffect.effect;
                                        enemyEffectGuid = skill.enemyEffect.effect;
                                        break;
                                }
                            }
                            break;

                        case BattleCommandType.Skill:
                            if (activeCharacter.targetCharacter != null)
                            {
                                switch (activeCharacter.selectedSkill.option.target)
                                {
                                    case Rom.TargetType.PARTY_ONE:
                                    case Rom.TargetType.PARTY_ALL:
                                    case Rom.TargetType.SELF:
                                    case Rom.TargetType.OTHERS:
                                        friendFffectGuid = activeCharacter.selectedSkill.friendEffect.effect;
                                        break;

                                    case Rom.TargetType.ENEMY_ONE:
                                    case Rom.TargetType.ENEMY_ALL:
                                        enemyEffectGuid = activeCharacter.selectedSkill.enemyEffect.effect;
                                        break;

                                    case Rom.TargetType.PARTY_ONE_ENEMY_ALL:
                                    case Rom.TargetType.PARTY_ALL_ENEMY_ONE:
                                    case Rom.TargetType.SELF_ENEMY_ONE:
                                    case Rom.TargetType.SELF_ENEMY_ALL:
                                    case Rom.TargetType.ALL:
                                    case Rom.TargetType.OTHERS_ENEMY_ONE:
                                    case Rom.TargetType.OTHERS_ALL:
                                        friendFffectGuid = activeCharacter.selectedSkill.friendEffect.effect;
                                        enemyEffectGuid = activeCharacter.selectedSkill.enemyEffect.effect;
                                        break;
                                }
                            }
                            break;

                        case BattleCommandType.Item:
                            switch (activeCharacter.selectedItem.item.type)
                            {
                                case Rom.ItemType.EXPENDABLE:
                                    friendFffectGuid = activeCharacter.selectedItem.item.expendable.effect;
                                    break;

                                case Rom.ItemType.EXPENDABLE_WITH_SKILL:
                                    {
                                        var skill = (Common.Rom.Skill)catalog.getItemFromGuid(activeCharacter.selectedItem.item.expendableWithSkill.skill);
                                        if (skill != null)
                                        {
                                            switch (skill.option.target)
                                            {
                                                case Rom.TargetType.PARTY_ONE:
                                                case Rom.TargetType.PARTY_ALL:
                                                case Rom.TargetType.SELF:
                                                case Rom.TargetType.OTHERS:
                                                    friendFffectGuid = skill.friendEffect.effect;
                                                    break;

                                                case Rom.TargetType.ENEMY_ONE:
                                                case Rom.TargetType.ENEMY_ALL:
                                                    enemyEffectGuid = skill.enemyEffect.effect;
                                                    break;

                                                case Rom.TargetType.ALL:                    // 敵味方全員
                                                case Rom.TargetType.SELF_ENEMY_ONE:         // 自分と敵一人
                                                case Rom.TargetType.SELF_ENEMY_ALL:         // 自分と敵全員
                                                case Rom.TargetType.OTHERS_ENEMY_ONE:       // 自分以外と敵一人
                                                case Rom.TargetType.OTHERS_ALL:             // 自分以外の敵味方全員
                                                case Rom.TargetType.PARTY_ONE_ENEMY_ALL:    // 味方一人と敵全員
                                                case Rom.TargetType.PARTY_ALL_ENEMY_ONE:    // 味方全員と敵一人
                                                    friendFffectGuid = skill.friendEffect.effect;
                                                    enemyEffectGuid = skill.enemyEffect.effect;
                                                    break;
                                            }
                                        }
                                    }
                                    break;
                            }
                            break;
                    }

                    if (activeCharacter.targetCharacter != null)
                    {
                        foreach (var target in activeCharacter.targetCharacter)
                        {
                            if (friendFffectGuid != Guid.Empty && activeCharacter.commandFriendEffectCharacters.Contains(target))
                            {
                                battleViewer.SetBattlePlayerEffect(friendFffectGuid, target);
                            }
                            else if (enemyEffectGuid != Guid.Empty && activeCharacter.commandEnemyEffectCharacters.Contains(target))
                            {
                                battleViewer.SetBattleMonsterEffect(enemyEffectGuid, target);
                            }
                        }
                    }

                    ChangeBattleState(BattleState.DisplayCommandEffect);
                    break;

                case BattleState.DisplayCommandEffect:
                    if (battleViewer.IsEffectEndPlay)
                    {
                        battleViewer.SetupDamageTextAnimation();

                        activeCharacter.ExecuteCommandEnd();

                        if (activeCharacter.targetCharacter != null)
                        {
                            foreach (var target in activeCharacter.targetCharacter)
                            {
                                target.CommandReactionStart();
                            }
                        }

                        foreach (var player in playerData)
                        {
                            if (owner.debugSettings.battleHpAndMpMax)
                            {
                                player.HitPoint = player.MaxHitPoint;
                                player.MagicPoint = player.MaxMagicPoint;
                            }

                            SetNextBattleStatus(player);
                        }

                        statusUpdateTweener.Begin(0, 1.0f, 30);

                        ChangeBattleState(BattleState.DisplayDamageText);
                    }
                    break;

                case BattleState.DisplayDamageText:
                    {
                        bool isUpdated = false;

                        foreach (var player in playerData)
                        {
                            isUpdated |= UpdateBattleStatusData(player);
                        }

                        if (isUpdated)
                        {
                            statusUpdateTweener.Update();
                        }

                        // ダメージ用テキストとステータス用ゲージのアニメーションが終わるまで待つ
                        if (!battleViewer.IsPlayDamageTextAnimation && !isUpdated && isReady3DCamera())
                        {
                            if (activeCharacter.targetCharacter != null)
                            {
                                foreach (var target in activeCharacter.targetCharacter)
                                {
                                    target.CommandReactionEnd();
                                }
                            }

                            battleViewer.CloseWindow();
                            
                            ChangeBattleState(BattleState.CheckCommandRecoveryStatus);
                        }
                    }
                    break;

                case BattleState.CheckCommandRecoveryStatus:
                    // バトルイベントが処理中だったら、スキル・アイテム名のウィンドウだけ閉じてまだ待機する
                    if (battleEvents.isBusy())
                        break;

                    if (recoveryStatusInfo.Count == 0)
                    {
                        ChangeBattleState(BattleState.CheckBattleCharacterDown1);
                    }
                    else
                    {
                        battleViewer.SetDisplayMessage(recoveryStatusInfo[0].GetMessage(gameSettings));

                        ChangeBattleState(BattleState.DisplayCommandRecoveryStatus);
                    }
                    break;

                case BattleState.DisplayCommandRecoveryStatus:
                    if (battleStateFrameCount >= 30 || Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE))
                    {
                        recoveryStatusInfo.RemoveAt(0);

                        ChangeBattleState(BattleState.CheckCommandRecoveryStatus);
                    }
                    break;

                case BattleState.CheckBattleCharacterDown1:
                    battleViewer.effectDrawTargetPlayerList.Clear();
                    battleViewer.effectDrawTargetMonsterList.Clear();

                    CheckBattleCharacterDown();

                    ChangeBattleState(BattleState.FadeMonsterImage1);
                    break;

                case BattleState.FadeMonsterImage1:
                    if (battleViewer.IsFadeEnd)
                    {
                        ChangeBattleState(BattleState.BattleFinishCheck1);
                    }

                    break;

                case BattleState.BattleFinishCheck1:
                    {
                        var battleResult = CheckBattleFinish();

                        if (battleResult == BattleResultState.NonFinish)
                        {
                            ChangeBattleState(BattleState.ProcessPoisonStatus);
                        }
                        else if (battleStateFrameCount >= 30)
                        {
                            battleEvents.setBattleResult(battleResult);
                            ChangeBattleState(BattleState.StartBattleFinishEvent);
                        }
                    }
                    break;

                case BattleState.ProcessPoisonStatus:
                    if ((playerData.Cast<BattleCharacterBase>().Contains(activeCharacter) || enemyMonsterData.Cast<BattleCharacterBase>().Contains(activeCharacter)) &&
                        activeCharacter.Status.HasFlag(StatusAilments.POISON) && (activeCharacter.PoisonDamegePercent > 0))
                    {
                        string message = string.Format(gameSettings.glossary.battle_poison, activeCharacter.Name);

                        battleViewer.SetDisplayMessage(message);

                        int damage = (int)(activeCharacter.MaxHitPoint * (activeCharacter.PoisonDamegePercent / 100.0f));

                        if (damage == 0)
                        {
                            damage = 1;
                        }

                        activeCharacter.HitPoint -= damage;

                        if (activeCharacter.HitPoint < 0)
                        {
                            activeCharacter.HitPoint = 0;
                        }

                        var damageText = new List<BattleDamageTextInfo>();

                        damageText.Add(new BattleDamageTextInfo(BattleDamageTextInfo.TextType.HitPointDamage, activeCharacter, damage.ToString()));

                        battleViewer.SetDamageTextInfo(damageText);

                        battleViewer.SetupDamageTextAnimation();

                        if (playerData.Contains(activeCharacter as BattlePlayerData))
                        {
                            statusUpdateTweener.Begin(0, 1.0f, 30);

                            if (owner.debugSettings.battleHpAndMpMax)
                            {
                                activeCharacter.HitPoint = activeCharacter.MaxHitPoint;
                                activeCharacter.MagicPoint = activeCharacter.MaxMagicPoint;
                            }

                            SetNextBattleStatus((BattlePlayerData)activeCharacter);
                        }

                        ChangeBattleState(BattleState.DisplayStatusDamage);
                    }
                    else
                    {
                        ChangeBattleState(BattleState.CheckBattleCharacterDown2);
                    }
                    break;

                case BattleState.DisplayStatusDamage:
                    {
                        bool isEndPlayerStatusUpdate = playerData.Select(player => UpdateBattleStatusData(player)).All(isUpdated => isUpdated == false);

                        if (!isEndPlayerStatusUpdate) statusUpdateTweener.Update();

                        if (!battleViewer.IsPlayDamageTextAnimation && isEndPlayerStatusUpdate)
                        {
                            battleViewer.CloseWindow();

                            ChangeBattleState(BattleState.CheckBattleCharacterDown2);
                        }
                    }
                    break;

                case BattleState.CheckBattleCharacterDown2:
                    battleViewer.effectDrawTargetPlayerList.Clear();
                    battleViewer.effectDrawTargetMonsterList.Clear();

                    CheckBattleCharacterDown();

                    ChangeBattleState(BattleState.FadeMonsterImage2);
                    break;

                case BattleState.FadeMonsterImage2:
                    if (battleViewer.IsFadeEnd)
                    {
                        ChangeBattleState(BattleState.BattleFinishCheck2);
                    }

                    break;

                case BattleState.BattleFinishCheck2:
                    {
                        var battleResult = CheckBattleFinish();

                        if (battleResult == BattleResultState.NonFinish)
                        {
                            commandExecuteMemberCount++;

                            if (commandExecuteMemberCount >= battleEntryCharacters.Count)
                            {
                                foreach (var player in playerData)
                                {
                                    player.forceSetCommand = false;
                                    player.commandSelectedCount = 0;
                                    player.selectedBattleCommandType = BattleCommandType.Undecided;
                                }
                                foreach (var enemy in enemyMonsterData)
                                {
                                    enemy.selectedBattleCommandType = BattleCommandType.Undecided;
                                }

                                ChangeBattleState(BattleState.PlayerTurnStart);
                            }
                            else
                            {
                                ChangeBattleState(BattleState.ReadyExecuteCommand);
                            }
                        }
                        else
                        {
                            battleEvents.setBattleResult(battleResult);
                            ChangeBattleState(BattleState.StartBattleFinishEvent);
                        }
                    }
                    break;

                case BattleState.StartBattleFinishEvent:
                    battleEvents.start(Rom.Script.Trigger.BATTLE_END);
                    ChangeBattleState(BattleState.ProcessBattleFinish);
                    break;

                case BattleState.ProcessBattleFinish:
                    if (battleEvents.isBusy())
                        break;

                    ProcessBattleResult(CheckBattleFinish());
                    if (!owner.debugSettings.battleHpAndMpMax)
                    {
                        ApplyPlayerDataToGameData();
                    }
                    break;

                case BattleState.Result:
                    resultViewer.Update();

                    if (battleEvents.isLocked())
                        break;

                    if (resultViewer.IsEnd && (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE) || resultViewer.clickedCloseButton))
                    {
                        resultViewer.clickedCloseButton = false;
                        Audio.PlaySound(owner.se.decide);

                        BattleResult = BattleResultState.Win;

                        ChangeBattleState(BattleState.SetFinishEffect);
                    }
                    break;

                case BattleState.PlayerChallengeEscape:
                    if (battleStateFrameCount >= 0)
                    {
                        int escapeSuccessParcent = activeCharacter.EscapeSuccessBaseParcent;

                        // 素早さの差が大きいほど成功確率が上がる
                        if (escapeSuccessParcent < 100)
                        {
                            int playerMaxSpeed = playerData.Max(player => player.Speed);
                            if (playerMaxSpeed < 1)
                                playerMaxSpeed = 1;
                            escapeSuccessParcent += (int)((1.5f - (enemyMonsterData.Max(monster => monster.Speed) / playerMaxSpeed)) * 100);
                        }

                        // 逃走に失敗するたびに成功確率が上がる
                        escapeSuccessParcent += (playerEscapeFailedCount * 15);

                        if (escapeSuccessParcent < 0) escapeSuccessParcent = 0;
                        if (escapeSuccessParcent > 100) escapeSuccessParcent = 100;

                        if (escapeSuccessParcent >= battleRandom.Next(100)) // 逃走成功
                        {
                            if (BattleResultEscapeEvents != null)
                            {
                                BattleResultEscapeEvents();
                            }

                            Audio.PlaySound(owner.se.escape);

                            battleViewer.SetDisplayMessage(activeCharacter.EscapeSuccessMessage);

                            ChangeBattleState(BattleState.PlayerEscapeSuccess);
                        }
                        else                                                // 逃走失敗
                        {
                            playerEscapeFailedCount++;

                            battleViewer.SetDisplayMessage(gameSettings.glossary.battle_escape_failed);

                            ChangeBattleState(BattleState.PlayerEscapeFail);
                        }
                    }
                    break;

                case BattleState.PlayerEscapeSuccess:
                case BattleState.StopByEvent:
                    if (battleStateFrameCount >= 90 && !battleEvents.isBusy())
                    {
                        if (!owner.debugSettings.battleHpAndMpMax)
                        {
                            ApplyPlayerDataToGameData();
                        }

                        battleViewer.CloseWindow();

                        BattleResult = BattleResultState.Escape;
                        battleEvents.setBattleResult(BattleResult);
                        battleEvents.start(Rom.Script.Trigger.BATTLE_END);
                        ChangeBattleState(BattleState.SetFinishEffect);
                    }
                    break;

                case BattleState.PlayerEscapeFail:
                    if (battleStateFrameCount >= 90)
                    {
                        activeCharacter.ExecuteCommandEnd();

                        battleViewer.CloseWindow();

                        ChangeBattleState(BattleState.CheckBattleCharacterDown1);
                    }
                    break;

                case BattleState.MonsterEscape:
                    if (battleStateFrameCount >= 60)
                    {
                        battleViewer.CloseWindow();

                        var escapedMonster = (BattleEnemyData)activeCharacter;

                        enemyMonsterData.Remove(escapedMonster);

                        foreach (var player in playerData)
                        {
                            player.enemyPartyMember.Remove(escapedMonster);
                        }

                        foreach (var monster in enemyMonsterData)
                        {
                            monster.friendPartyMember.Remove(escapedMonster);
                        }

                        ChangeBattleState(BattleState.BattleFinishCheck1);
                    }
                    break;

                case BattleState.SetFinishEffect:
                    fadeScreenColorTweener.Begin(new Color(Color.Black, 0), new Color(Color.Black, 255), 30);
                    ChangeBattleState(BattleState.FinishFadeOut);
                    break;

                case BattleState.FinishFadeOut:
                    if (battleEvents.isBusy())
                        break;

                    fadeScreenColorTweener.Update();

                    if (!fadeScreenColorTweener.IsPlayTween)
                    {
                        fadeScreenColorTweener.Begin(new Color(Color.Black, 255), new Color(Color.Black, 0), 30);

                        IsDrawingBattleScene = false;

                        owner.mapScene.ProcParallelScript();

                        ChangeBattleState(BattleState.FinishFadeIn);
                    }
                    break;

                case BattleState.FinishFadeIn:
                    fadeScreenColorTweener.Update();

                    if (!fadeScreenColorTweener.IsPlayTween)
                    {
                        IsPlayingBattleEffect = false;
                    }
                    break;
            }
        }

        private bool isQualifiedSkillCostItem(BattleCharacterBase activeCharacter, Common.Rom.Skill skill)
        {
            if (activeCharacter is BattleEnemyData)
                return true;

            return party.isOKToConsumptionItem(skill.option.consumptionItem, skill.option.consumptionItemAmount);
        }

        internal bool isContinuable()
        {
            return BattleResult == BattleSequenceManager.BattleResultState.Lose_Continue ||
                   BattleResult == BattleSequenceManager.BattleResultState.Escape ||
                   BattleResult == BattleSequenceManager.BattleResultState.Lose_Advanced_GameOver;
        }

        private void setAttributeWithWeaponDamage(BattleCharacterBase target, AttackAttributeType attackAttribute, int elementAttack)
        {
            switch (attackAttribute)
            {
                case AttackAttributeType.Poison:
                    if (-battleRandom.Next(100) + elementAttack >= target.ResistanceAilmentStatus[(int)StatusAilmentIndex.Poison])
                    {
                        target.SetStatusAilment(StatusAilments.POISON);
                    }
                    break;

                case AttackAttributeType.Sleep:
                    if (-battleRandom.Next(100) + elementAttack >= target.ResistanceAilmentStatus[(int)StatusAilmentIndex.Sleep])
                    {
                        target.SetStatusAilment(StatusAilments.SLEEP);
                    }
                    break;

                case AttackAttributeType.Paralysis:
                    if (-battleRandom.Next(100) + elementAttack >= target.ResistanceAilmentStatus[(int)StatusAilmentIndex.Paralysis])
                    {
                        target.SetStatusAilment(StatusAilments.PARALYSIS);
                    }
                    break;

                case AttackAttributeType.Confusion:
                    if (-battleRandom.Next(100) + elementAttack >= target.ResistanceAilmentStatus[(int)StatusAilmentIndex.Confusion])
                    {
                        target.SetStatusAilment(StatusAilments.CONFUSION);
                    }
                    break;

                case AttackAttributeType.Fascination:
                    if (-battleRandom.Next(100) + elementAttack >= target.ResistanceAilmentStatus[(int)StatusAilmentIndex.Fascination])
                    {
                        target.SetStatusAilment(StatusAilments.FASCINATION);
                    }
                    break;

                case AttackAttributeType.Death:
                    if (-battleRandom.Next(100) + elementAttack >= target.ResistanceAilmentStatus[(int)StatusAilmentIndex.Down])
                    {
                        target.SetStatusAilment(StatusAilments.DOWN);
                        target.HitPoint = 0;
                    }
                    break;
            }
        }

        private List<BattleCharacterBase> createBattleCharacterList()
        {
            var battleCharacters = new List<BattleCharacterBase>();
            battleCharacters.AddRange(playerData.Cast<BattleCharacterBase>());
            battleCharacters.AddRange(enemyMonsterData.Cast<BattleCharacterBase>());
            return battleCharacters;
        }

        private bool isReady3DCamera()
        {
            if (battleViewer is BattleViewer3D)
            {
                var bv3d = battleViewer as BattleViewer3D;
                return bv3d.getCurrentCameraTag() != BattleCameraController.TAG_FORCE_WAIT;
            }

            return true;
        }

        private bool isReadyActor()
        {
            if (battleViewer is BattleViewer3D)
            {
                var bv3d = battleViewer as BattleViewer3D;
                return bv3d.isCurrentActorReady();
            }

            return true;
        }


        private void CheckAndDoReTarget()
        {
            // 攻撃対象を変える必要があるかチェック
            if (IsNeedReTarget(activeCharacter))
            {
                bool isFriendRecoveryDownStatus = false;

                switch (activeCharacter.selectedBattleCommandType)
                {
                    case BattleCommandType.SameSkillEffect:
                        {
                            var skill = catalog.getItemFromGuid(activeCharacter.selectedBattleCommand.refGuid) as Rom.Skill;

                            isFriendRecoveryDownStatus = (skill.friendEffect != null && skill.friendEffect.down && skill.option.onlyForDown);
                        }
                        break;

                    case BattleCommandType.Skill:
                        isFriendRecoveryDownStatus = (
                            activeCharacter.selectedSkill.friendEffect != null &&
                            activeCharacter.selectedSkill.friendEffect.down &&
                            activeCharacter.selectedSkill.option.onlyForDown);
                        break;

                    case BattleCommandType.Item:
                        if (activeCharacter.selectedItem.item.expendable != null)
                        {
                            isFriendRecoveryDownStatus = activeCharacter.selectedItem.item.expendable.recoveryDown;
                        }
                        else if (activeCharacter.selectedItem.item.expendableWithSkill != null)
                        {
                            var skill = catalog.getItemFromGuid(activeCharacter.selectedItem.item.expendableWithSkill.skill) as Common.Rom.Skill;

                            isFriendRecoveryDownStatus = (skill.friendEffect != null && skill.friendEffect.down && skill.option.onlyForDown);
                        }
                        break;
                }

                activeCharacter.targetCharacter = ReTarget(activeCharacter, isFriendRecoveryDownStatus);
            }
        }

        private void UpdateCommandSelect()
        {
            if (battleEvents.isLocked())
                return;

            var battleCommandChoiceWindowDrawer = battleViewer.battleCommandChoiceWindowDrawer;
            var commandTargetWindowDrawer = battleViewer.commandTargetSelector;
            var skillSelectWindowDrawer = battleViewer.skillSelectWindowDrawer;
            var itemSelectWindowDrawer = battleViewer.itemSelectWindowDrawer;

            if (commandSelectPlayer != null && commandSelectPlayer.commandTargetList.Count > 0)
            {
                foreach (var target in commandSelectPlayer.commandTargetList) target.IsSelect = false;
            }

            switch (battleCommandState)
            {
                case SelectBattleCommandState.CommandSelect:
                    battleCommandChoiceWindowDrawer.Update();

                    if (battleCommandChoiceWindowDrawer.CurrentSelectItemEnable && (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE) || battleCommandChoiceWindowDrawer.decided))
                    {
                        battleCommandChoiceWindowDrawer.saveSelected();
                        battleCommandChoiceWindowDrawer.decided = false;
                        Audio.PlaySound(owner.se.decide);

                        commandSelectPlayer.selectedBattleCommand = playerBattleCommand[battleCommandChoiceWindowDrawer.CurrentSelectItemIndex];

                        switch (commandSelectPlayer.selectedBattleCommand.type)
                        {
                            case BattleCommand.CommandType.ATTACK: battleCommandState = SelectBattleCommandState.Attack_Command; break;
                            case BattleCommand.CommandType.GUARD: battleCommandState = SelectBattleCommandState.Guard_Command; break;
                            case BattleCommand.CommandType.CHARGE: battleCommandState = SelectBattleCommandState.Charge_Command; break;
                            case BattleCommand.CommandType.SKILL: battleCommandState = SelectBattleCommandState.SkillSameEffect_Command; break;
                            case BattleCommand.CommandType.SKILLMENU: battleCommandState = SelectBattleCommandState.Skill_Command; break;
                            case BattleCommand.CommandType.ITEMMENU: battleCommandState = SelectBattleCommandState.Item_Command; break;
                            case BattleCommand.CommandType.ESCAPE: battleCommandState = SelectBattleCommandState.Escape_Command; break;
                            case BattleCommand.CommandType.BACK: battleCommandState = SelectBattleCommandState.Back_Command; break;
                        }
                    }
                    else if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CANCEL))
                    {
                        Audio.PlaySound(owner.se.cancel);

                        battleCommandState = SelectBattleCommandState.CommandCancel;
                    }
                    break;

                // 通常攻撃
                case SelectBattleCommandState.Attack_Command:
                    battleCommandState = SelectBattleCommandState.Attack_MakeTargetList;
                    break;

                case SelectBattleCommandState.Attack_MakeTargetList:
                    commandSelectPlayer.commandTargetList.Clear();
                    commandTargetWindowDrawer.Clear();

                    foreach (var enemy in enemyMonsterData.Where(enemy => enemy.HitPoint > 0))
                    {
                        commandSelectPlayer.commandTargetList.Add(enemy);
                        commandTargetWindowDrawer.AddMonster(enemy);
                    }

                    commandTargetWindowDrawer.ResetSelect(commandSelectPlayer);

                    battleCommandState = SelectBattleCommandState.Attack_SelectTarget;

                    break;

                case SelectBattleCommandState.Attack_SelectTarget:
                    {
                        bool isDecide = commandTargetWindowDrawer.InputUpdate();

                        if (commandTargetWindowDrawer.Count > 0)
                        {
                            var targetMonster = commandTargetWindowDrawer.CurrentSelectCharacter;

                            targetMonster.IsSelect = true;

                            battleViewer.SetDisplayMessage(targetMonster.Name, WindowType.CommandTargetMonsterListWindow);   // TODO
                        }

                        if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE) || isDecide)
                        {
                            Audio.PlaySound(owner.se.decide);
                            commandTargetWindowDrawer.saveSelect();
                            commandSelectPlayer.selectedBattleCommandType = BattleCommandType.Attack;
                            battleCommandState = SelectBattleCommandState.CommandEnd;
                        }
                        if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CANCEL))
                        {
                            Audio.PlaySound(owner.se.cancel);

                            commandTargetWindowDrawer.Clear();

                            battleViewer.OpenWindow(WindowType.PlayerCommandWindow);

                            battleCommandState = SelectBattleCommandState.CommandSelect;
                        }
                        break;
                    }

                // 防御
                case SelectBattleCommandState.Guard_Command:
                    commandSelectPlayer.selectedBattleCommandType = BattleCommandType.Guard;
                    battleCommandState = SelectBattleCommandState.CommandEnd;
                    break;

                // チャージ
                case SelectBattleCommandState.Charge_Command:
                    commandSelectPlayer.selectedBattleCommandType = BattleCommandType.Charge;
                    battleCommandState = SelectBattleCommandState.CommandEnd;
                    break;

                case SelectBattleCommandState.SkillSameEffect_Command:
                    battleCommandState = SelectBattleCommandState.SkillSameEffect_MakeTargetList;
                    break;

                case SelectBattleCommandState.SkillSameEffect_MakeTargetList:
                    commandSelectPlayer.commandTargetList.Clear();
                    commandTargetWindowDrawer.Clear();

                    {
                        var windowType = WindowType.CommandTargetPlayerListWindow;
                        var skill = catalog.getItemFromGuid(commandSelectPlayer.selectedBattleCommand.refGuid) as Rom.Skill;
                        commandSelectPlayer.selectedSkill = skill;

                        switch (skill.option.target)
                        {
                            case Rom.TargetType.PARTY_ONE:
                            case Rom.TargetType.PARTY_ONE_ENEMY_ALL:
                                commandSelectPlayer.commandTargetList.AddRange(playerData.Cast<BattleCharacterBase>());

                                foreach (var target in commandSelectPlayer.commandTargetList)
                                {
                                    commandTargetWindowDrawer.AddPlayer((BattlePlayerData)target);
                                }

                                commandTargetWindowDrawer.ResetSelect(commandSelectPlayer);

                                battleViewer.OpenWindow(WindowType.CommandTargetPlayerListWindow);
                                battleCommandState = SelectBattleCommandState.SkillSameEffect_SelectTarget;

                                windowType = WindowType.CommandTargetPlayerListWindow;
                                break;

                            case Rom.TargetType.ENEMY_ONE:
                            case Rom.TargetType.PARTY_ALL_ENEMY_ONE:
                            case Rom.TargetType.SELF_ENEMY_ONE:
                            case Rom.TargetType.OTHERS_ENEMY_ONE:
                                commandSelectPlayer.commandTargetList.AddRange(
                                    enemyMonsterData.Where(enemy => enemy.HitPoint > 0).Cast<BattleCharacterBase>());

                                foreach (var target in commandSelectPlayer.commandTargetList)
                                {
                                    commandTargetWindowDrawer.AddMonster((BattleEnemyData)target);
                                }

                                commandTargetWindowDrawer.ResetSelect(commandSelectPlayer);

                                battleViewer.OpenWindow(WindowType.CommandTargetMonsterListWindow);
                                battleCommandState = SelectBattleCommandState.SkillSameEffect_SelectTarget;

                                windowType = WindowType.CommandTargetMonsterListWindow;
                                break;

                            default:
                                commandSelectPlayer.selectedBattleCommandType = BattleCommandType.SameSkillEffect;
                                battleCommandState = SelectBattleCommandState.CommandEnd;
                                break;
                        }

                        battleViewer.SetDisplayMessage(gameSettings.glossary.battle_target, windowType);   // TODO
                    }
                    break;

                case SelectBattleCommandState.SkillSameEffect_SelectTarget:
                    {
                        bool isDecide = commandTargetWindowDrawer.InputUpdate();

                        if (commandTargetWindowDrawer.Count > 0)
                        {
                            commandTargetWindowDrawer.CurrentSelectCharacter.IsSelect = true;
                        }

                        if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE) || isDecide)
                        {
                            Audio.PlaySound(owner.se.decide);
                            commandTargetWindowDrawer.saveSelect();

                            commandSelectPlayer.selectedBattleCommandType = BattleCommandType.SameSkillEffect;
                            battleCommandState = SelectBattleCommandState.CommandEnd;
                        }
                        if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CANCEL))
                        {
                            Audio.PlaySound(owner.se.cancel);

                            commandTargetWindowDrawer.Clear();

                            battleViewer.OpenWindow(WindowType.PlayerCommandWindow);

                            battleCommandState = SelectBattleCommandState.CommandSelect;
                        }
                        break;
                    }

                // スキル
                case SelectBattleCommandState.Skill_Command:
                    battleCommandState = SelectBattleCommandState.Skill_SelectSkill;
                    battleViewer.OpenWindow(WindowType.SkillListWindow);

                    skillSelectWindowDrawer.SelectDefaultItem(commandSelectPlayer, battleState);
                    skillSelectWindowDrawer.HeaderTitleIcon = commandSelectPlayer.selectedBattleCommand.icon;
                    skillSelectWindowDrawer.HeaderTitleText = commandSelectPlayer.selectedBattleCommand.name;
                    skillSelectWindowDrawer.FooterTitleIcon = null;
                    skillSelectWindowDrawer.FooterTitleText = "";
                    skillSelectWindowDrawer.FooterSubDescriptionText = "";

                    commandSelectPlayer.useableSkillList.Clear();
                    skillSelectWindowDrawer.ClearChoiceListData();

                    commandSelectPlayer.useableSkillList.AddRange(commandSelectPlayer.player.skills);

                    foreach (var skill in commandSelectPlayer.useableSkillList)
                    {
                        bool useableSkill = skill.option.availableInBattle &&
                            (skill.option.consumptionHitpoint < commandSelectPlayer.HitPoint &&
                             skill.option.consumptionMagicpoint <= commandSelectPlayer.MagicPoint &&
                             isQualifiedSkillCostItem(commandSelectPlayer, skill));

                        if (iconTable.ContainsKey(skill.icon.guId))
                        {
                            skillSelectWindowDrawer.AddChoiceData(iconTable[skill.icon.guId], skill.icon, skill.name, useableSkill);
                        }
                        else
                        {
                            skillSelectWindowDrawer.AddChoiceData(skill.name, useableSkill);
                        }
                    }
                    break;

                case SelectBattleCommandState.Skill_SelectSkill:
                    skillSelectWindowDrawer.FooterMainDescriptionText = "";
                    skillSelectWindowDrawer.Update();

                    if (skillSelectWindowDrawer.CurrentSelectItemType == ChoiceWindowDrawer.ItemType.Item && skillSelectWindowDrawer.ChoiceItemCount > 0)
                    {
                        commandSelectPlayer.selectedSkill = commandSelectPlayer.useableSkillList[skillSelectWindowDrawer.CurrentSelectItemIndex];

                        skillSelectWindowDrawer.FooterTitleIcon = commandSelectPlayer.selectedSkill.icon;
                        skillSelectWindowDrawer.FooterTitleText = commandSelectPlayer.selectedSkill.name;
                        skillSelectWindowDrawer.FooterMainDescriptionText = Common.Util.createSkillDescription(catalog, party, commandSelectPlayer.selectedSkill);
                    }

                    if ((Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE) || skillSelectWindowDrawer.decided) && skillSelectWindowDrawer.CurrentSelectItemType == ChoiceWindowDrawer.ItemType.Item && commandSelectPlayer.useableSkillList.Count > 0 && skillSelectWindowDrawer.CurrentSelectItemEnable)
                    {
                        skillSelectWindowDrawer.saveSelected();
                        skillSelectWindowDrawer.decided = false;
                        Audio.PlaySound(owner.se.decide);

                        battleCommandState = SelectBattleCommandState.Skill_MakeTargetList;
                    }
                    if (((Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE) || skillSelectWindowDrawer.decided) && skillSelectWindowDrawer.CurrentSelectItemType == ChoiceWindowDrawer.ItemType.Cancel) || Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CANCEL))
                    {
                        skillSelectWindowDrawer.decided = false;
                        Audio.PlaySound(owner.se.cancel);

                        battleViewer.OpenWindow(WindowType.PlayerCommandWindow);

                        battleCommandState = SelectBattleCommandState.CommandSelect;
                    }
                    break;

                case SelectBattleCommandState.Skill_MakeTargetList:
                    commandSelectPlayer.commandTargetList.Clear();
                    commandTargetWindowDrawer.Clear();

                    {
                        var windowType = WindowType.CommandTargetPlayerListWindow;

                        switch (commandSelectPlayer.selectedSkill.option.target)
                        {
                            case Rom.TargetType.PARTY_ONE:
                            case Rom.TargetType.PARTY_ONE_ENEMY_ALL:
                                commandSelectPlayer.commandTargetList.AddRange(playerData.Cast<BattleCharacterBase>());

                                foreach (var target in commandSelectPlayer.commandTargetList)
                                {
                                    commandTargetWindowDrawer.AddPlayer((BattlePlayerData)target);
                                }

                                commandTargetWindowDrawer.ResetSelect(commandSelectPlayer);

                                battleViewer.OpenWindow(WindowType.CommandTargetPlayerListWindow);
                                battleCommandState = SelectBattleCommandState.Skill_SelectTarget;

                                windowType = WindowType.CommandTargetPlayerListWindow;
                                break;

                            case Rom.TargetType.ENEMY_ONE:
                            case Rom.TargetType.PARTY_ALL_ENEMY_ONE:
                            case Rom.TargetType.SELF_ENEMY_ONE:
                            case Rom.TargetType.OTHERS_ENEMY_ONE:
                                commandSelectPlayer.commandTargetList.AddRange(
                                    enemyMonsterData.Where(enemy => enemy.HitPoint > 0).Cast<BattleCharacterBase>());

                                foreach (var target in commandSelectPlayer.commandTargetList)
                                {
                                    commandTargetWindowDrawer.AddMonster((BattleEnemyData)target);
                                }

                                commandTargetWindowDrawer.ResetSelect(commandSelectPlayer);

                                battleViewer.OpenWindow(WindowType.CommandTargetMonsterListWindow);
                                battleCommandState = SelectBattleCommandState.Skill_SelectTarget;

                                windowType = WindowType.CommandTargetMonsterListWindow;
                                break;

                            default:
                                commandSelectPlayer.selectedBattleCommandType = BattleCommandType.Skill;
                                battleCommandState = SelectBattleCommandState.CommandEnd;
                                break;
                        }

                        battleViewer.SetDisplayMessage(gameSettings.glossary.battle_target, windowType);   // TODO
                    }
                    break;

                case SelectBattleCommandState.Skill_SelectTarget:
                    {
                        bool isDecide = commandTargetWindowDrawer.InputUpdate();

                        if (commandTargetWindowDrawer.Count > 0)
                        {
                            commandTargetWindowDrawer.CurrentSelectCharacter.IsSelect = true;
                        }

                        if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE) || isDecide)
                        {
                            Audio.PlaySound(owner.se.decide);
                            commandTargetWindowDrawer.saveSelect();

                            commandSelectPlayer.selectedBattleCommandType = BattleCommandType.Skill;
                            battleCommandState = SelectBattleCommandState.CommandEnd;
                        }
                        if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CANCEL))
                        {
                            Audio.PlaySound(owner.se.cancel);

                            commandTargetWindowDrawer.Clear();

                            battleViewer.OpenWindow(WindowType.SkillListWindow);

                            battleCommandState = SelectBattleCommandState.Skill_Command;
                        }
                        break;
                    }

                // アイテム
                case SelectBattleCommandState.Item_Command:
                    itemSelectWindowDrawer.SelectDefaultItem(commandSelectPlayer, battleState);
                    itemSelectWindowDrawer.HeaderTitleIcon = commandSelectPlayer.selectedBattleCommand.icon;
                    itemSelectWindowDrawer.HeaderTitleText = commandSelectPlayer.selectedBattleCommand.name;
                    itemSelectWindowDrawer.FooterTitleIcon = null;
                    itemSelectWindowDrawer.FooterTitleText = "";
                    itemSelectWindowDrawer.FooterSubDescriptionText = "";

                    commandSelectPlayer.haveItemList.Clear();
                    itemSelectWindowDrawer.ClearChoiceListData();

                    var expendableItems = party.items.Where(itemData => itemData.item.type == Rom.ItemType.EXPENDABLE && itemData.item.expendable.availableInBattle);
                    var skillItems = party.items.Where(itemData => itemData.item.type == Rom.ItemType.EXPENDABLE_WITH_SKILL && itemData.item.expendableWithSkill.availableInBattle);

                    var useableItems = expendableItems.Union(skillItems).Where(itemData => !commandSelectPlayer.player.rom.availableItemsList.ContainsKey(itemData.item.guId) || commandSelectPlayer.player.rom.availableItemsList[itemData.item.guId]);

                    commandSelectPlayer.haveItemList.AddRange(useableItems);

                    foreach (var itemData in commandSelectPlayer.haveItemList)
                    {
                        int itemCount = itemData.num;

                        // 既にアイテムを使おうとしているメンバーがいたらその分だけ個数を減らす
                        itemCount -= (playerData.Count(player => (player != commandSelectPlayer && player.selectedBattleCommandType == BattleCommandType.Item) && (player.selectedItem.item == itemData.item)));

                        bool useableItem = (itemCount > 0);

                        if (iconTable.ContainsKey(itemData.item.icon.guId))
                        {
                            itemSelectWindowDrawer.AddChoiceData(iconTable[itemData.item.icon.guId], itemData.item.icon, itemData.item.name, itemCount, useableItem);
                        }
                        else
                        {
                            itemSelectWindowDrawer.AddChoiceData(itemData.item.name, itemCount, useableItem);
                        }
                    }

                    battleViewer.OpenWindow(WindowType.ItemListWindow);
                    battleCommandState = SelectBattleCommandState.Item_SelectItem;
                    break;

                case SelectBattleCommandState.Item_SelectItem:
                    itemSelectWindowDrawer.FooterMainDescriptionText = "";
                    itemSelectWindowDrawer.Update();

                    if (itemSelectWindowDrawer.CurrentSelectItemType == ChoiceWindowDrawer.ItemType.Item && itemSelectWindowDrawer.ChoiceItemCount > 0)
                    {
                        commandSelectPlayer.selectedItem = commandSelectPlayer.haveItemList[itemSelectWindowDrawer.CurrentSelectItemIndex];

                        itemSelectWindowDrawer.FooterTitleText = commandSelectPlayer.selectedItem.item.name;
                        itemSelectWindowDrawer.FooterMainDescriptionText = commandSelectPlayer.selectedItem.item.description;
                        //itemSelectWindowDrawer.FooterSubDescriptionText = string.Format("所持数 {0, 4}個", commandSelectPlayer.selectedItem.num);
                    }

                    if ((Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE) || itemSelectWindowDrawer.decided) && itemSelectWindowDrawer.CurrentSelectItemType == ChoiceWindowDrawer.ItemType.Item && itemSelectWindowDrawer.CurrentSelectItemEnable)
                    {
                        itemSelectWindowDrawer.saveSelected();
                        itemSelectWindowDrawer.decided = false;
                        Audio.PlaySound(owner.se.decide);

                        battleCommandState = SelectBattleCommandState.Item_MakeTargetList;
                    }
                    if (((Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE) || itemSelectWindowDrawer.decided) && itemSelectWindowDrawer.CurrentSelectItemType == ChoiceWindowDrawer.ItemType.Cancel) || Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CANCEL))
                    {
                        itemSelectWindowDrawer.decided = false;
                        Audio.PlaySound(owner.se.cancel);

                        battleViewer.OpenWindow(WindowType.PlayerCommandWindow);

                        battleCommandState = SelectBattleCommandState.CommandSelect;
                    }
                    break;

                case SelectBattleCommandState.Item_MakeTargetList:
                    commandSelectPlayer.commandTargetList.Clear();
                    commandTargetWindowDrawer.Clear();

                    {
                        var item = commandSelectPlayer.selectedItem.item;
                        var windowType = WindowType.CommandTargetPlayerListWindow;

                        if (item.type == Rom.ItemType.EXPENDABLE)
                        {
                            foreach (var player in playerData)
                            {
                                commandSelectPlayer.commandTargetList.Add(player);
                                commandTargetWindowDrawer.AddPlayer(player);
                            }
                        }
                        else if (item.type == Rom.ItemType.EXPENDABLE_WITH_SKILL)
                        {
                            var skill = catalog.getItemFromGuid(item.expendableWithSkill.skill) as Rom.Skill;

                            if (skill != null)
                            {
                                switch (skill.option.target)
                                {
                                    case Rom.TargetType.PARTY_ONE:
                                    case Rom.TargetType.PARTY_ONE_ENEMY_ALL:
                                        foreach (var player in playerData)
                                        {
                                            commandSelectPlayer.commandTargetList.Add(player);
                                            commandTargetWindowDrawer.AddPlayer(player);

                                            windowType = WindowType.CommandTargetPlayerListWindow;
                                        }
                                        break;

                                    case Rom.TargetType.SELF_ENEMY_ONE:
                                    case Rom.TargetType.ENEMY_ONE:
                                    case Rom.TargetType.PARTY_ALL_ENEMY_ONE:
                                    case Rom.TargetType.OTHERS_ENEMY_ONE:
                                        foreach (var monster in enemyMonsterData.Where(enemy => enemy.HitPoint > 0))
                                        {
                                            commandSelectPlayer.commandTargetList.Add(monster);
                                            commandTargetWindowDrawer.AddMonster(monster);

                                            windowType = WindowType.CommandTargetMonsterListWindow;
                                        }
                                        break;
                                }
                            }
                        }

                        commandTargetWindowDrawer.ResetSelect(commandSelectPlayer);

                        battleViewer.OpenWindow(windowType);
                        battleCommandState = SelectBattleCommandState.Item_SelectTarget;
                        battleViewer.SetDisplayMessage(gameSettings.glossary.battle_target, windowType);   // TODO
                    }
                    break;

                case SelectBattleCommandState.Item_SelectTarget:
                    {
                        bool isDecide = commandTargetWindowDrawer.InputUpdate();

                        if (commandTargetWindowDrawer.Count > 0)
                        {
                            commandTargetWindowDrawer.CurrentSelectCharacter.IsSelect = true;
                        }

                        if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE) || isDecide)
                        {
                            Audio.PlaySound(owner.se.decide);
                            commandTargetWindowDrawer.saveSelect();

                            commandSelectPlayer.selectedBattleCommandType = BattleCommandType.Item;
                            battleCommandState = SelectBattleCommandState.CommandEnd;
                        }
                        if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CANCEL))
                        {
                            Audio.PlaySound(owner.se.cancel);

                            commandTargetWindowDrawer.Clear();

                            battleViewer.OpenWindow(WindowType.ItemListWindow);

                            battleCommandState = SelectBattleCommandState.Item_Command;
                        }
                        break;
                    }

                // 逃げる
                case SelectBattleCommandState.Escape_Command:
                    commandSelectPlayer.selectedBattleCommandType = BattleCommandType.PlayerEscape;
                    battleCommandState = SelectBattleCommandState.CommandEnd;
                    break;

                case SelectBattleCommandState.Back_Command:
                    commandSelectPlayer.selectedBattleCommandType = BattleCommandType.Back;
                    battleCommandState = SelectBattleCommandState.CommandEnd;
                    break;
            }
        }

        private BattleCharacterBase[] MakeTargetList(Common.Rom.Skill skill)
        {
            List<BattleCharacterBase> targets = new List<BattleCharacterBase>();

            switch (skill.option.target)
            {
                case Rom.TargetType.ALL:
                    break;
            }

            return targets.ToArray();
        }
        private BattleCharacterBase[] MakeTargetList(Common.Rom.Item item)
        {
            List<BattleCharacterBase> targets = new List<BattleCharacterBase>();

            if (item.expendable != null)
            {
            }
            else if (item.expendableWithSkill != null)
            {
            }

            return targets.ToArray();
        }

        public void Draw()
        {
            Graphics.BeginDraw();

            switch (battleState)
            {
                case BattleState.StartFlash:
                case BattleState.StartFadeOut:
                case BattleState.FinishFadeIn:
                    Graphics.DrawFillRect(0, 0, Graphics.ScreenWidth, Graphics.ScreenHeight, fadeScreenColorTweener.CurrentValue.R, fadeScreenColorTweener.CurrentValue.G, fadeScreenColorTweener.CurrentValue.B, fadeScreenColorTweener.CurrentValue.A);
                    break;

                case BattleState.StartFadeIn:
                    DrawBackground();
                    battleViewer.Draw(playerData, enemyMonsterData);
                    battleEvents.Draw();
                    battleViewer.DrawWindows(playerData);
                    Graphics.DrawFillRect(0, 0, Graphics.ScreenWidth, Graphics.ScreenHeight, fadeScreenColorTweener.CurrentValue.R, fadeScreenColorTweener.CurrentValue.G, fadeScreenColorTweener.CurrentValue.B, fadeScreenColorTweener.CurrentValue.A);
                    break;

                case BattleState.SetFinishEffect:
                case BattleState.FinishFadeOut:
                    DrawBackground();
                    if (!owner.IsBattle2D)
                        ((BattleViewer3D)battleViewer).DrawField(playerData, enemyMonsterData);
                    battleEvents.Draw();
                    //resultViewer.Draw();
                    Graphics.DrawFillRect(0, 0, Graphics.ScreenWidth, Graphics.ScreenHeight, fadeScreenColorTweener.CurrentValue.R, fadeScreenColorTweener.CurrentValue.G, fadeScreenColorTweener.CurrentValue.B, fadeScreenColorTweener.CurrentValue.A);
                    break;

                case BattleState.Result:
                    DrawBackground();
                    if (!owner.IsBattle2D)
                        ((BattleViewer3D)battleViewer).DrawField(playerData, enemyMonsterData);
                    resultViewer.Draw();
                    break;

                default:
                    DrawBackground();
                    battleViewer.Draw(playerData, enemyMonsterData);
                    battleEvents.Draw();
                    battleViewer.DrawWindows(playerData);
                    break;
            }


            Graphics.EndDraw();
        }

        private void DrawBackground()
        {
            if (!owner.IsBattle2D)
                return;

            // 背景表示
            switch (backGroundStyle)
            {
                case BackGroundStyle.FillColor:
                    Graphics.DrawFillRect(0, 0, Graphics.ScreenWidth, Graphics.ScreenHeight, backGroundColor.R, backGroundColor.G, backGroundColor.B, backGroundColor.A);
                    break;

                case BackGroundStyle.Image:
                    if (openingBackgroundImageScaleTweener.IsPlayTween)
                    {
                        Graphics.DrawImage(backGroundImageId, new Rectangle((int)(Graphics.ScreenWidth / 2 - Graphics.GetImageWidth(backGroundImageId) * openingBackgroundImageScaleTweener.CurrentValue / 2), (int)(Graphics.ScreenHeight / 2 - Graphics.GetImageHeight(backGroundImageId) * openingBackgroundImageScaleTweener.CurrentValue / 2), (int)(Graphics.GetImageWidth(backGroundImageId) * openingBackgroundImageScaleTweener.CurrentValue), (int)(Graphics.GetImageHeight(backGroundImageId) * openingBackgroundImageScaleTweener.CurrentValue)), new Rectangle(0, 0, Graphics.GetImageWidth(backGroundImageId), Graphics.GetImageHeight(backGroundImageId)));
                    }
                    else
                    {
                        Graphics.DrawImage(backGroundImageId, 0, 0);
                    }
                    break;

                case BackGroundStyle.Model:
                    break;
            }
        }

        private void ChangeBattleState(BattleState nextBattleState)
        {
            battleStateFrameCount = 0;

            battleState = nextBattleState;
        }

        public BattleResultState CheckBattleFinish()
        {
            var resultState = BattleResultState.NonFinish;

            // 戦闘終了条件
            // 1.全ての敵がHP0なったら     -> 勝利 (イベント戦として倒してはいけない敵が登場するならゲームオーバーになる場合もありえる?)
            // 2.全ての味方がHP0になったら -> 敗北 (ゲームオーバー or フィールド画面に戻る(イベント戦のような特別な戦闘を想定))
            // 3.「逃げる」コマンドの成功  -> 逃走
            // 4.その他 強制的に戦闘を終了するスクリプト (例 HPが半分になったらイベントが発生し戦闘終了)
            // 「発動後に自分が戦闘不能になる」スキルなどによってプレイヤーとモンスターが同時に全滅した場合(条件の1と2を同時に満たす場合)は敗北扱いとする

            // 敵が全て倒れたら
            if (enemyMonsterData.Where(monster => monster.HitPoint > 0).Count() == 0)
            {
                resultState = BattleResultState.Win;
            }

            // 味方のHPが全員0になったら
            if (playerData.Where(player => player.HitPoint > 0).Count() == 0)
            {
                if (gameoverOnLose)
                {
                    if(owner.data.start.gameOverSettings.gameOverType != GameOverSettings.GameOverType.DEFAULT)
                    {
                        resultState = BattleResultState.Lose_Advanced_GameOver;
                    }
                    else
                    {
                        resultState = BattleResultState.Lose_GameOver;
                    }
                }
                else
                {
                    resultState = BattleResultState.Lose_Continue;
                }
            }

            // バトルイベント側で指定されているか？
            battleEvents.checkForceBattleFinish(ref resultState);

            return resultState;
        }

        private void ProcessBattleResult(BattleResultState battleResultState)
        {
            switch (battleResultState)
            {
                case BattleResultState.Win:
                    {
                        // 経験値とお金とアイテムを加える
                        int totalMoney = 0;
                        int totalExp = 0;
                        var dropItems = new List<Rom.Item>();

                        foreach (var monsterData in enemyMonsterData)
                        {
                            var monster = monsterData.monster;

                            totalMoney += monster.money;
                            totalExp += monster.exp;

                            // アイテム抽選
                            if (monster.dropItemA != Guid.Empty && battleRandom.Next(100) < monster.dropItemAPercent)
                            {
                                var item = catalog.getItemFromGuid(monster.dropItemA) as Common.Rom.Item;

                                if (item != null) dropItems.Add(item);
                            }
                            if (monster.dropItemB != Guid.Empty && battleRandom.Next(100) < monster.dropItemBPercent)
                            {
                                var item = catalog.getItemFromGuid(monster.dropItemB) as Common.Rom.Item;

                                if (item != null) dropItems.Add(item);
                            }
                        }

                        resultViewer.SetResultData(playerData.ToArray(), totalExp, totalMoney, dropItems.ToArray(), party.itemDict);

                        this.GetExp = totalExp;
                        this.DropMoney = totalMoney;
                        this.DropItems = dropItems.ToArray();

                        if (BattleResultWinEvents != null)
                        {
                            BattleResultWinEvents();
                        }
                    }

                    resultViewer.Start();
                    ChangeBattleState(BattleState.Result);
                    break;

                case BattleResultState.Lose_GameOver:
                    {
                        if (BattleResultLoseGameOverEvents != null)
                        {
                            BattleResultLoseGameOverEvents();
                        }
                    }
                    BattleResult = BattleResultState.Lose_GameOver;
                    IsPlayingBattleEffect = false;
                    IsDrawingBattleScene = false;
                    break;

                case BattleResultState.Escape_ToTitle:
                    BattleResult = BattleResultState.Escape_ToTitle;
                    IsPlayingBattleEffect = false;
                    IsDrawingBattleScene = false;
                    break;

                case BattleResultState.Lose_Continue:
                    BattleResult = BattleResultState.Lose_Continue;
                    IsPlayingBattleEffect = false;
                    IsDrawingBattleScene = false;
                    break;

                case BattleResultState.Lose_Advanced_GameOver:
                    BattleResult = BattleResultState.Lose_Advanced_GameOver;
                    IsPlayingBattleEffect = false;
                    IsDrawingBattleScene = false;
                    break;

                case BattleResultState.NonFinish:
                    // イベントで敵や味方を増やすなどして、戦闘終了条件を満たさなくなった場合にここに来るので、普通に次のターンにする
                    ChangeBattleState(BattleState.ProcessPoisonStatus);
                    break;
            }
        }

        private void ApplyPlayerDataToGameData()
        {
            for (int i = 0; i < playerData.Count; i++)
            {
                ApplyPlayerDataToGameData(playerData[i]);
            }
        }

        internal void ApplyPlayerDataToGameData(BattlePlayerData battlePlayerData)
        {
            var gameData = battlePlayerData.player;

            gameData.hitpoint = battlePlayerData.HitPoint;
            gameData.magicpoint = battlePlayerData.MagicPoint;

            // 戦闘不能と毒状態は戦闘が終わっても引き継ぐ
            // それ以外の状態異常は引き継がないで回復させる
            gameData.statusAilments = StatusAilments.NONE;

            if (battlePlayerData.Status.HasFlag(StatusAilments.POISON))
            {
                gameData.statusAilments |= StatusAilments.POISON;
            }
            if (battlePlayerData.Status == StatusAilments.DOWN)
            {
                gameData.statusAilments = StatusAilments.DOWN;
            }
        }

        internal BattleCharacterBase[] GetTargetCharacters(BattleCharacterBase character)
        {
            var targets = new List<BattleCharacterBase>();

            switch (character.selectedBattleCommandType)
            {
                case BattleCommandType.Attack:
                case BattleCommandType.Critical:
                    switch (character.Status)
                    {
                        case StatusAilments.CONFUSION:
                            targets.Add(character.friendPartyMember.Where(player => player.Status != StatusAilments.DOWN).ElementAt(battleRandom.Next(character.friendPartyMember.Count(player => player.Status != StatusAilments.DOWN))));
                            break;
                        case StatusAilments.FASCINATION:
                            targets.Add(character.enemyPartyMember.Where(target => target.Status != StatusAilments.DOWN).ElementAt(battleRandom.Next(character.enemyPartyMember.Count(target => target.Status != StatusAilments.DOWN))));
                            break;
                        default:
                            targets.Add(battleViewer.commandTargetSelector.CurrentSelectCharacter);
                            break;
                    }

                    break;

                case BattleCommandType.Charge:
                    targets.Add(character);
                    break;

                case BattleCommandType.Guard:
                    targets.Add(character);
                    break;

                case BattleCommandType.SameSkillEffect:
                    {
                        var skill = catalog.getItemFromGuid(character.selectedBattleCommand.refGuid) as Rom.Skill;

                        switch (skill.option.target)
                        {
                            case Rom.TargetType.PARTY_ONE:
                            case Rom.TargetType.ENEMY_ONE:
                            case Rom.TargetType.PARTY_ONE_ENEMY_ALL:
                            case Rom.TargetType.PARTY_ALL_ENEMY_ONE:
                            case Rom.TargetType.SELF_ENEMY_ONE:
                            case Rom.TargetType.OTHERS_ENEMY_ONE:
                                targets.Add(battleViewer.commandTargetSelector.CurrentSelectCharacter);
                                break;

                            case Rom.TargetType.PARTY_ALL:
                                if (skill.friendEffect.down && skill.option.onlyForDown)
                                {
                                    targets.AddRange(character.friendPartyMember);
                                }
                                else
                                {
                                    targets.AddRange(character.friendPartyMember.Where(member => member.HitPoint > 0));
                                }
                                break;

                            case Rom.TargetType.ENEMY_ALL:
                                targets.AddRange(character.enemyPartyMember.Where(enemy => enemy.HitPoint > 0));
                                break;

                            case Rom.TargetType.SELF:
                                targets.Add(character);
                                break;

                            case Rom.TargetType.OTHERS:
                                if (skill.friendEffect.down && skill.option.onlyForDown)
                                {
                                    targets.AddRange(character.friendPartyMember.Where(member => character != member));
                                }
                                else
                                {
                                    targets.AddRange(character.friendPartyMember.Where(member => character != member && member.HitPoint > 0));
                                }
                                break;
                        }
                    }
                    break;

                case BattleCommandType.Skill:
                    switch (character.selectedSkill.option.target)
                    {
                        case Rom.TargetType.PARTY_ONE:
                        case Rom.TargetType.ENEMY_ONE:
                        case Rom.TargetType.PARTY_ONE_ENEMY_ALL:
                        case Rom.TargetType.PARTY_ALL_ENEMY_ONE:
                        case Rom.TargetType.SELF_ENEMY_ONE:
                        case Rom.TargetType.OTHERS_ENEMY_ONE:
                            targets.Add(battleViewer.commandTargetSelector.CurrentSelectCharacter);
                            break;

                        case Rom.TargetType.PARTY_ALL:
                            if (character.selectedSkill.friendEffect.down && character.selectedSkill.option.onlyForDown)
                            {
                                targets.AddRange(character.friendPartyMember);
                            }
                            else
                            {
                                targets.AddRange(character.friendPartyMember.Where(member => member.HitPoint > 0));
                            }
                            break;

                        case Rom.TargetType.ENEMY_ALL:
                            targets.AddRange(character.enemyPartyMember.Where(enemy => enemy.HitPoint > 0));
                            break;

                        case Rom.TargetType.SELF:
                            targets.Add(character);
                            break;

                        case Rom.TargetType.OTHERS:
                            if (character.selectedSkill.friendEffect.down && character.selectedSkill.option.onlyForDown)
                            {
                                targets.AddRange(character.friendPartyMember.Where(member => character != member));
                            }
                            else
                            {
                                targets.AddRange(character.friendPartyMember.Where(member => character != member && member.HitPoint > 0));
                            }
                            break;
                    }

                    break;

                case BattleCommandType.Item:
                    switch (character.selectedItem.item.type)
                    {
                        case Rom.ItemType.EXPENDABLE:
                            targets.Add(battleViewer.commandTargetSelector.CurrentSelectCharacter);
                            break;

                        case Rom.ItemType.EXPENDABLE_WITH_SKILL:
                            var skill = (Common.Rom.Skill)catalog.getItemFromGuid(character.selectedItem.item.expendableWithSkill.skill);

                            if (skill != null)
                            {
                                switch (skill.option.target)
                                {
                                    case Rom.TargetType.PARTY_ONE:
                                    case Rom.TargetType.ENEMY_ONE:
                                    case Rom.TargetType.PARTY_ONE_ENEMY_ALL:
                                    case Rom.TargetType.PARTY_ALL_ENEMY_ONE:
                                    case Rom.TargetType.SELF_ENEMY_ONE:
                                        targets.Add(battleViewer.commandTargetSelector.CurrentSelectCharacter);
                                        break;

                                    case Rom.TargetType.PARTY_ALL:
                                        if (skill.friendEffect.down && skill.option.onlyForDown)
                                        {
                                            targets.AddRange(character.friendPartyMember);
                                        }
                                        else
                                        {
                                            targets.AddRange(character.friendPartyMember.Where(member => member.HitPoint > 0));
                                        }
                                        break;

                                    case Rom.TargetType.ENEMY_ALL:
                                        targets.AddRange(character.enemyPartyMember.Where(enemy => enemy.HitPoint > 0));
                                        break;

                                    case Rom.TargetType.SELF:
                                        targets.Add(character);
                                        break;

                                    case Rom.TargetType.OTHERS:
                                        if (skill.friendEffect.down && skill.option.onlyForDown)
                                        {
                                            targets.AddRange(character.friendPartyMember.Where(member => character != member));
                                        }
                                        else
                                        {
                                            targets.AddRange(character.friendPartyMember.Where(member => character != member && member.HitPoint > 0));
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                    break;

                case BattleCommandType.Nothing:
                    //character.targetCharacter = new BattleCharacterBase[ 0 ];
                    break;
            }

            return targets.ToArray();
        }

        private bool IsNeedReTarget(BattleCharacterBase character)
        {
            bool isRetarget = false;

            switch (character.selectedBattleCommandType)
            {
                case BattleCommandType.Attack:
                case BattleCommandType.Critical:
                    foreach (var target in character.targetCharacter)
                    {
                        if (IsNotActiveTarget(target) || target.Status == StatusAilments.DOWN)
                        {
                            isRetarget = true;
                            break;
                        }
                    }
                    break;

                case BattleCommandType.SameSkillEffect:
                    foreach (var target in character.targetCharacter)
                    {
                        var skill = catalog.getItemFromGuid(character.selectedBattleCommand.refGuid) as Rom.Skill;

                        switch (skill.option.target)
                        {
                            case Rom.TargetType.ENEMY_ONE:
                            case Rom.TargetType.PARTY_ALL_ENEMY_ONE:
                            case Rom.TargetType.SELF_ENEMY_ONE:
                            case Rom.TargetType.OTHERS_ENEMY_ONE:
                                if (skill.enemyEffect != null)
                                {
                                    if (IsNotActiveTarget(target) || target.Status == StatusAilments.DOWN)
                                    {
                                        isRetarget = true;
                                    }
                                }
                                break;

                            case Rom.TargetType.PARTY_ONE:
                            case Rom.TargetType.PARTY_ONE_ENEMY_ALL:
                                if (skill.friendEffect != null)
                                {
                                    if (IsNotActiveTarget(target) || ((target.Status == StatusAilments.DOWN) != skill.friendEffect.down && skill.option.onlyForDown))
                                    {
                                        isRetarget = true;
                                    }
                                }
                                break;
                        }
                    }
                    break;

                case BattleCommandType.Skill:
                    foreach (var target in character.targetCharacter)
                    {
                        var skill = character.selectedSkill;

                        switch (skill.option.target)
                        {
                            case Rom.TargetType.ENEMY_ONE:
                            case Rom.TargetType.PARTY_ALL_ENEMY_ONE:
                            case Rom.TargetType.SELF_ENEMY_ONE:
                            case Rom.TargetType.OTHERS_ENEMY_ONE:
                                if (skill.enemyEffect != null)
                                {
                                    if (IsNotActiveTarget(target) || target.Status == StatusAilments.DOWN)
                                    {
                                        isRetarget = true;
                                    }
                                }
                                break;

                            case Rom.TargetType.PARTY_ONE:
                            case Rom.TargetType.PARTY_ONE_ENEMY_ALL:
                                if (skill.friendEffect != null)
                                {
                                    if (IsNotActiveTarget(target) || ((target.Status == StatusAilments.DOWN) != skill.friendEffect.down && skill.option.onlyForDown))
                                    {
                                        isRetarget = true;
                                    }
                                }
                                break;
                        }
                    }
                    break;

                case BattleCommandType.Item:
                    foreach (var target in character.targetCharacter)
                    {
                        if (character.selectedItem.item.expendable != null)
                        {
                            if (IsNotActiveTarget(target) || (target.Status == StatusAilments.DOWN) != character.selectedItem.item.expendable.recoveryDown)
                            {
                                isRetarget = true;
                            }
                        }
                        if (character.selectedItem.item.expendableWithSkill != null)
                        {
                            var skill = catalog.getItemFromGuid(character.selectedItem.item.expendableWithSkill.skill) as Common.Rom.Skill;

                            switch (skill.option.target)
                            {
                                case Rom.TargetType.PARTY_ONE:
                                case Rom.TargetType.PARTY_ONE_ENEMY_ALL:
                                    if (IsNotActiveTarget(target) || target == null || (skill.friendEffect != null && ((target.Status == StatusAilments.DOWN) != skill.friendEffect.down)))
                                    {
                                        isRetarget = true;
                                    }
                                    break;

                                case Rom.TargetType.ENEMY_ONE:
                                case Rom.TargetType.PARTY_ALL_ENEMY_ONE:
                                case Rom.TargetType.SELF_ENEMY_ONE:
                                    if (IsNotActiveTarget(target) || target.Status == StatusAilments.DOWN)
                                    {
                                        isRetarget = true;
                                    }
                                    break;
                            }
                        }
                    }
                    break;
            }

            return isRetarget;
        }

        private bool IsNotActiveTarget(BattleCharacterBase target)
        {
            if (playerData.Contains(target as BattlePlayerData))
                return false;
            if (enemyMonsterData.Contains(target as BattleEnemyData))
                return false;
            return true;
        }

        private BattleCharacterBase[] ReTarget(BattleCharacterBase character, bool isFriendRecoveryDownStatus)
        {
            var targets = new List<BattleCharacterBase>();
            var friendPartyMember = character.friendPartyMember.Where(player => (player.Status == StatusAilments.DOWN) == isFriendRecoveryDownStatus);
            var targetPartyMember = character.enemyPartyMember.Where(enemy => enemy.Status != StatusAilments.DOWN);

            switch (character.selectedBattleCommandType)
            {
                case BattleCommandType.Attack:
                case BattleCommandType.Critical:
                    if (character.Status.HasFlag(StatusAilments.CONFUSION))
                    {
                        targets.Add(friendPartyMember.ElementAt(battleRandom.Next(friendPartyMember.Count())));
                    }
                    else if (character.Status.HasFlag(StatusAilments.FASCINATION))
                    {
                        if (targetPartyMember.Count() > 0)
                            targets.Add(targetPartyMember.ElementAt(battleRandom.Next(targetPartyMember.Count())));
                    }
                    else
                    {
                        if(targetPartyMember.Count() > 0)
                            targets.Add(targetPartyMember.ElementAt(battleRandom.Next(targetPartyMember.Count())));
                    }
                    break;

                // どちらも対象が自分自身なので再抽選の必要は無し
                case BattleCommandType.Charge:
                case BattleCommandType.Guard:
                    targets.Add(character);
                    break;

                case BattleCommandType.SameSkillEffect:
                    {
                        var skill = catalog.getItemFromGuid(character.selectedBattleCommand.refGuid) as Rom.Skill;

                        switch (skill.option.target)
                        {
                            case Rom.TargetType.ENEMY_ONE:
                            case Rom.TargetType.PARTY_ALL_ENEMY_ONE:
                            case Rom.TargetType.SELF_ENEMY_ONE:
                            case Rom.TargetType.OTHERS_ENEMY_ONE:
                                if (targetPartyMember.Count() > 0) targets.Add(targetPartyMember.ElementAt(battleRandom.Next(targetPartyMember.Count())));
                                break;
                            case Rom.TargetType.PARTY_ONE:
                            case Rom.TargetType.PARTY_ONE_ENEMY_ALL:
                                if (friendPartyMember.Count() > 0) targets.Add(friendPartyMember.ElementAt(battleRandom.Next(friendPartyMember.Count())));
                                break;
                        }
                    }
                    break;

                case BattleCommandType.Skill:
                    switch (character.selectedSkill.option.target)
                    {
                        case Rom.TargetType.ENEMY_ONE:
                        case Rom.TargetType.PARTY_ALL_ENEMY_ONE:
                        case Rom.TargetType.SELF_ENEMY_ONE:
                        case Rom.TargetType.OTHERS_ENEMY_ONE:
                            if (targetPartyMember.Count() > 0) targets.Add(targetPartyMember.ElementAt(battleRandom.Next(targetPartyMember.Count())));
                            break;
                        case Rom.TargetType.PARTY_ONE:
                        case Rom.TargetType.PARTY_ONE_ENEMY_ALL:
                            if (friendPartyMember.Count() > 0) targets.Add(friendPartyMember.ElementAt(battleRandom.Next(friendPartyMember.Count())));
                            break;
                    }
                    break;

                case BattleCommandType.Item:
                    switch (character.selectedItem.item.type)
                    {
                        case Rom.ItemType.EXPENDABLE:
                            if (character.selectedItem.item.expendable.recoveryDown)
                            {
                                var a = character.friendPartyMember.Where(player => player.HitPoint == 0);

                                if (a.Count() > 0) targets.Add(a.ElementAt(battleRandom.Next(a.Count())));
                            }
                            else
                            {
                                var a = character.friendPartyMember.Where(player => player.HitPoint > 0);

                                if (a.Count() > 0) targets.Add(a.ElementAt(battleRandom.Next(a.Count())));
                            }
                            break;

                        case Rom.ItemType.EXPENDABLE_WITH_SKILL:
                            var skill = catalog.getItemFromGuid(character.selectedItem.item.expendableWithSkill.skill) as Common.Rom.Skill;

                            switch (skill.option.target)
                            {
                                case Rom.TargetType.PARTY_ONE:
                                case Rom.TargetType.PARTY_ONE_ENEMY_ALL:
                                    if (skill.friendEffect.down)
                                    {
                                        var a = character.friendPartyMember.Where(player => player.HitPoint == 0);
                                        if (skill.option.onlyForDown)
                                            a = character.friendPartyMember;

                                        if (a.Count() > 0) targets.Add(a.ElementAt(battleRandom.Next(a.Count())));
                                    }
                                    else
                                    {
                                        var a = character.friendPartyMember.Where(player => player.HitPoint > 0);

                                        if (a.Count() > 0) targets.Add(a.ElementAt(battleRandom.Next(a.Count())));
                                    }
                                    break;
                                case Rom.TargetType.ENEMY_ONE:
                                case Rom.TargetType.PARTY_ALL_ENEMY_ONE:
                                case Rom.TargetType.SELF_ENEMY_ONE:
                                    {
                                        var a = character.enemyPartyMember.Where(enemy => enemy.HitPoint > 0);

                                        if (a.Count() > 0) targets.Add(a.ElementAt(battleRandom.Next(a.Count())));
                                    }
                                    break;
                            }
                            break;
                    }
                    break;
            }

            return targets.ToArray();
        }

        public void RegisterTestEffect(string effectNameKey, Rom.Effect effect, Catalog catalog)
        {
            //effectCatalog = catalog;
        }

        internal void SetBackGroundColor(Color color)
        {
            backGroundStyle = BackGroundStyle.FillColor;

            backGroundColor = color;
        }

        internal void SetBackGroundImage(string path)
        {
            SetBackGroundImage(Graphics.LoadImage(path));
        }
        internal void SetBackGroundImage(int imageId)
        {
            backGroundStyle = BackGroundStyle.Image;

            backGroundImageId = imageId;
        }

        internal void SetBackGroundModel(string path)
        {
            /*backGroundModel = */
            Graphics.LoadModel(path);
            backGroundStyle = BackGroundStyle.Model;
        }

        internal System.Collections.IEnumerator prepare()
        {
            if (!owner.IsBattle2D)
            {
                var coroutine = ((BattleViewer3D)battleViewer).prepare();
                while (coroutine.MoveNext()) yield return null;
            }
        }
        internal System.Collections.IEnumerator prepare(Guid battleBg)
        {
            if (!owner.IsBattle2D)
            {
                var coroutine = ((BattleViewer3D)battleViewer).prepare(battleBg);
                while (coroutine.MoveNext()) yield return null;
            }
        }

        internal bool isWrongFromCurrentBg(Guid battleBg)
        {
            if (!owner.IsBattle2D)
            {
                return ((BattleViewer3D)battleViewer).drawer.mapRom.guId != battleBg;
            }

            return false;
        }

        //------------------------------------------------------------------------------
        /**
         *	スクリーンフェードカラーを取得
         */
        public SharpKmyGfx.Color GetFadeScreenColor()
        {
            SharpKmyGfx.Color col = new SharpKmyGfx.Color(0, 0, 0, 0);

            switch (battleState)
            {
                case BattleState.StartFlash:
                case BattleState.StartFadeOut:
                case BattleState.FinishFadeIn:
                case BattleState.StartFadeIn:
                case BattleState.SetFinishEffect:
                case BattleState.FinishFadeOut:
                    col.r = (float)fadeScreenColorTweener.CurrentValue.R / 255.0f;
                    col.g = (float)fadeScreenColorTweener.CurrentValue.G / 255.0f;
                    col.b = (float)fadeScreenColorTweener.CurrentValue.B / 255.0f;
                    col.a = (float)fadeScreenColorTweener.CurrentValue.A / 255.0f;
                    break;
            }

            return col;
        }

        //------------------------------------------------------------------------------
        /**
         *	バトルステートを取得
         */
        public BattleState GetBattleState()
        {
            return battleState;
        }

#if ENABLE_VR

        //------------------------------------------------------------------------------
        /**
         *	VRカメラデータをセットアップ
         */
        public void SetupVrCameraData(bool bUpdateInfo)
        {
            battleViewer.SetupVrCameraData(bUpdateInfo);
        }

        //------------------------------------------------------------------------------
        /**
         *	VRカメラデータを取得
         */
        public virtual VrCameraData GetVrCameraData()
        {
            return battleViewer.GetVrCameraData();
        }

#endif // #if ENABLE_VR
    }
}

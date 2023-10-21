using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;

using Yukar.Common;

using Rom = Yukar.Common.Rom;
using Resource = Yukar.Common.Resource;
using Yukar.Engine;
using static Yukar.Engine.BattleEnum;

namespace Yukar.Battle
{
    /// <summary>
    /// 2Dバトル用の表示管理クラス / 3Dバトルの表示状態にも一部利用
    /// Display management class for 2D battle / Partly used for display status of 3D battle
    /// </summary>
    public class BattleViewer
    {
        public GameMain game { get; set; }

        protected float viewerTimer;

        protected WindowType displayWindow;

        protected WindowDrawer windowDrawer;
        protected TextDrawer textDrawer;

        internal ChoiceWindowDrawer battleCommandChoiceWindowDrawer { get; private set; }
        internal CommandTargetSelecter commandTargetSelector { get; private set; }
        internal ChoiceWindowDrawer skillSelectWindowDrawer { get; private set; }
        internal ChoiceWindowDrawer itemSelectWindowDrawer { get; private set; }
        internal ChoiceWindowDrawer stockSelectWindowDrawer { get; private set; }

        protected BattleStatusWindowDrawer battleStatusWindowDrawer;

        public Vector2 CommandWindowBasePosition { get; set; }
        public bool BallonImageReverse { get; set; }

        Catalog catalog;
        protected EffectDrawerBase playerEffectDrawer;
        protected EffectDrawerBase monsterEffectDrawer;
        protected EffectDrawerBase defeatEffectDrawer;
        public bool IsEffectAllowShowDamage { get { return playerEffectDrawer.isEndPlayingOrOverDamageTime && monsterEffectDrawer.isEndPlayingOrOverDamageTime; } }
        public bool IsEffectEndPlay { get { return playerEffectDrawer.isEndPlaying && monsterEffectDrawer.isEndPlaying; } }
        virtual public bool IsEndMotion(BattleCharacterBase chr = null) { return true; }

        // ステータス表示用各種エフェクト
        // Various effects for status display
        EffectDrawer powerUpEffectDrawer;
        EffectDrawer vitalityUpEffectDrawer;
        EffectDrawer magicUpEffectDrawer;
        EffectDrawer speedUpEffectDrawer;
        EffectDrawer dexterityUpEffectDrawer;
        EffectDrawer evationUpEffectDrawer;

        EffectDrawer powerDownEffectDrawer;
        EffectDrawer vitalityDownEffectDrawer;
        EffectDrawer magicDownEffectDrawer;
        EffectDrawer speedDownEffectDrawer;
        EffectDrawer dexterityDownEffectDrawer;
        EffectDrawer evationDownEffectDrawer;

        Dictionary<Guid, EffectDrawer> conditionEffectDrawerDic = new Dictionary<Guid, EffectDrawer>();

        protected BattleEnemyData prevSelectedMonster;

        //static readonly Vector2 DefaultWindowOffset = new Vector2(0, -32);
        public static readonly Vector2 StatusWindowSize = new Vector2(192, 72);

        protected TweenVector2 tweenBattleCommandWindowScale;
        protected Blinker choiceWindowCursolColor;

        public string displayMessageText;
        TweenVector2 messageWindowPosition;

        protected Common.Resource.Texture damageNumberImageId;
        protected IEnumerable<BattleDamageTextInfo> damageTextList;
        public bool IsPlayDamageTextAnimation { get { return damageTextList.Count() != 0; } }

        internal List<BattleCharacterBase> effectDrawTargetPlayerList;
        internal List<BattleCharacterBase> effectDrawTargetMonsterList;
        internal List<BattleCharacterBase> defeatEffectDrawTargetList;

        protected List<BattleCharacterBase> fadeoutEnemyList;
        List<BattleCharacterBase> fadeinEnemyList;
        public bool IsFadeEnd { get { return fadeoutEnemyList.Count + fadeinEnemyList.Count == 0; } }

        protected Common.Resource.Texture serifImageId;

        protected Blinker blinker;

        public TweenFloat openingMonsterScaleTweener;
        public TweenColor openingColorTweener;

        public const float FADEINOUT_SPEED = 0.025f;


        public BattleViewer(GameMain owner)
        {
            this.game = owner;

            playerEffectDrawer = new EffectDrawer();
            monsterEffectDrawer = new EffectDrawer();
            defeatEffectDrawer = new EffectDrawer();

            effectDrawTargetPlayerList = new List<BattleCharacterBase>();
            effectDrawTargetMonsterList = new List<BattleCharacterBase>();
            defeatEffectDrawTargetList = new List<BattleCharacterBase>();

            fadeoutEnemyList = new List<BattleCharacterBase>();
            fadeinEnemyList = new List<BattleCharacterBase>();

            tweenBattleCommandWindowScale = new TweenVector2();
            messageWindowPosition = new TweenVector2();

            blinker = new Blinker();
            blinker.setColor(new Color(255, 255, 255, 255), new Color(255, 255, 255, 0), 20);

            choiceWindowCursolColor = new Blinker();

            openingMonsterScaleTweener = new TweenFloat();
            openingColorTweener = new TweenColor();
        }

        public virtual void LoadResourceData(Catalog catalog, Rom.GameSettings gameSettings)
        {
            //var windows = catalog.getFilteredItemList(typeof(Resource.Window));

            var winRes = catalog.getItemFromGuid(gameSettings.systemGraphics.battleMessage) as Resource.Texture;
            Graphics.LoadImage(winRes);
            windowDrawer = new WindowDrawer(winRes);

            // 画像を直接読み込む (本来はエディタの設定に応じて読み込む)
            // Directly read the image (originally read according to the settings of the editor)
            var statusWindow = catalog.getItemFromGuid(gameSettings.GraphicBattleStatusBG) as Resource.Texture;
            if (statusWindow == null) statusWindow = Resource.Texture.getDefaultCommonTexture(Resource.Texture.CommonTexture.BattleStatusBG);
            Graphics.LoadImage(statusWindow);

            battleStatusWindowDrawer = new BattleStatusWindowDrawer(new WindowDrawer(statusWindow), catalog);
            battleStatusWindowDrawer.HPLabelText = gameSettings.glossary.hp;
            battleStatusWindowDrawer.MPLabelText = gameSettings.glossary.mp;

            commandTargetSelector = new CommandTargetSelecter();
            commandTargetSelector.owner = game;
            commandTargetSelector.battleViewer = this;

            // ウィンドウ画像を読み込む
            // load window image
            textDrawer = new TextDrawer(1);

            winRes = catalog.getItemFromGuid(gameSettings.systemGraphics.battleSkillItemList) as Resource.Texture;
            Graphics.LoadImage(winRes);
            skillSelectWindowDrawer = new ChoiceWindowDrawer(game, new WindowDrawer(winRes), textDrawer, 6, 2, 1);
            Graphics.LoadImage(winRes);
            itemSelectWindowDrawer = new ChoiceWindowDrawer(game, new WindowDrawer(winRes), textDrawer, 6, 2, 1);

            winRes = catalog.getItemFromGuid(gameSettings.systemGraphics.menuSelected) as Resource.Texture;
            Graphics.LoadImage(winRes);
            skillSelectWindowDrawer.CursorDrawer = new WindowDrawer(winRes);
            Graphics.LoadImage(winRes);
            itemSelectWindowDrawer.CursorDrawer = new WindowDrawer(winRes);

            // 戦闘コマンドウィンドウの設定
            // Combat command window settings
            if (gameSettings.battleType == Rom.GameSettings.BattleType.CLASSIC)
            {
                winRes = catalog.getItemFromGuid(gameSettings.systemGraphics.battleCommands2D) as Resource.Texture;
                Graphics.LoadImage(winRes);
                battleCommandChoiceWindowDrawer = new ChoiceWindowDrawer(game, new WindowDrawer(winRes), textDrawer, 5, 1);
                winRes = catalog.getItemFromGuid(gameSettings.systemGraphics.menuSelected) as Resource.Texture;
                Graphics.LoadImage(winRes);
                battleCommandChoiceWindowDrawer.CursorDrawer = new WindowDrawer(winRes);

                battleCommandChoiceWindowDrawer.EnableItemTextColor = Color.Black;
                battleCommandChoiceWindowDrawer.RowSpaceMarginPixel = 1;
                battleCommandChoiceWindowDrawer.ChoiceItemIconPositionOffset = new Vector2(8, 0);
                battleCommandChoiceWindowDrawer.ChoiceItemTextPositionOffset = new Vector2(8 + 32, 0);
                battleCommandChoiceWindowDrawer.ItemTextScale = 0.9f;
            }
            else
            {
                battleCommandChoiceWindowDrawer = new ChoiceWindowDrawer(game, new WindowDrawer(null), textDrawer, 5, 1);
                winRes = catalog.getItemFromGuid(gameSettings.systemGraphics.menuSelected) as Resource.Texture;
                Graphics.LoadImage(winRes);
                battleCommandChoiceWindowDrawer.CursorDrawer = new WindowDrawer(winRes);

                battleCommandChoiceWindowDrawer.EnableItemTextColor = Color.White;
                battleCommandChoiceWindowDrawer.RowSpaceMarginPixel = 1;
                battleCommandChoiceWindowDrawer.ChoiceItemIconPositionOffset = new Vector2(8, 0);
                battleCommandChoiceWindowDrawer.ChoiceItemTextPositionOffset = new Vector2(8 + 32, 0);
                battleCommandChoiceWindowDrawer.ItemTextScale = 0.75f;
            }

            winRes = catalog.getItemFromGuid(gameSettings.systemGraphics.menuSelectable) as Resource.Texture;
            if (gameSettings.battleType == Rom.GameSettings.BattleType.CLASSIC)
            {
                Graphics.LoadImage(winRes);
                battleCommandChoiceWindowDrawer.ChoiceItemBackgroundDrawer = new WindowDrawer(winRes);
            }
            Graphics.LoadImage(winRes);
            skillSelectWindowDrawer.ChoiceItemBackgroundDrawer = new WindowDrawer(winRes);
            Graphics.LoadImage(winRes);
            itemSelectWindowDrawer.ChoiceItemBackgroundDrawer = new WindowDrawer(winRes);

            // スキルウィンドウの設定
            // Skill window settings
            skillSelectWindowDrawer.RowSpaceMarginPixel = 1;
            skillSelectWindowDrawer.ColumnSpaceMarginPixel = 4;
            skillSelectWindowDrawer.IsDisplayPageIcon = true;
            skillSelectWindowDrawer.ItemNoneText = gameSettings.glossary.noSkill;
            skillSelectWindowDrawer.ChoiceItemTextPositionOffset = new Vector2(Resource.Icon.ICON_WIDTH, 0);
            skillSelectWindowDrawer.TextAreaSize = new Vector2(625, 75);
            skillSelectWindowDrawer.WindowFrameThickness = 2;
            skillSelectWindowDrawer.PageIconMarginPixel = 8;
            skillSelectWindowDrawer.CancelButtonText = gameSettings.glossary.battle_cancel;
            skillSelectWindowDrawer.CancelButtonHeight = 48;

            itemSelectWindowDrawer.RowSpaceMarginPixel = 1;
            itemSelectWindowDrawer.ColumnSpaceMarginPixel = 4;
            itemSelectWindowDrawer.IsDisplayPageIcon = true;
            itemSelectWindowDrawer.ItemNoneText = gameSettings.glossary.noItem;
            itemSelectWindowDrawer.ChoiceItemTextPositionOffset = new Vector2(Resource.Icon.ICON_WIDTH, 0);
            itemSelectWindowDrawer.ChoiceItemNumberPositionOffset = new Vector2(-8, 0);
            itemSelectWindowDrawer.TextAreaSize = new Vector2(625, 75);
            itemSelectWindowDrawer.WindowFrameThickness = 2;
            itemSelectWindowDrawer.PageIconMarginPixel = 8;
            itemSelectWindowDrawer.CancelButtonText = gameSettings.glossary.battle_cancel;
            itemSelectWindowDrawer.CancelButtonHeight = 48;
            itemSelectWindowDrawer.ItemTextScale = 0.9f;

            stockSelectWindowDrawer = new ChoiceWindowDrawer(game, new WindowDrawer(null), textDrawer, 5, 1);
            winRes = catalog.getItemFromGuid(gameSettings.systemGraphics.menuSelected) as Resource.Texture;
            Graphics.LoadImage(winRes);
            stockSelectWindowDrawer.CursorDrawer = new WindowDrawer(winRes);

            stockSelectWindowDrawer.EnableItemTextColor = Color.White;
            stockSelectWindowDrawer.RowSpaceMarginPixel = 1;
            stockSelectWindowDrawer.ChoiceItemIconPositionOffset = new Vector2(8, 0);
            stockSelectWindowDrawer.ChoiceItemTextPositionOffset = new Vector2(8 + 32, 0);
            stockSelectWindowDrawer.ItemTextScale = 0.75f;

            var spriteRes = catalog.getItemFromGuid(gameSettings.damageNumber) as Resource.Texture;

            if (spriteRes == null)
            {
                damageNumberImageId = Resource.Texture.getDefaultCommonTexture(Resource.Texture.CommonTexture.BattleNumber);
                Graphics.LoadImage(damageNumberImageId);
            }
            else
            {
                damageNumberImageId = Graphics.LoadImageDiv(spriteRes.path, 1, 10);
            }

            serifImageId = catalog.getItemFromGuid(gameSettings.GraphicBattleMessageCursorSprite) as Resource.Texture;
            if (serifImageId == null) serifImageId = Resource.Texture.getDefaultCommonTexture(Resource.Texture.CommonTexture.BattleNeedle);
            Graphics.LoadImage(serifImageId);

            this.catalog = catalog;

            powerUpEffectDrawer = new EffectDrawer();
            vitalityUpEffectDrawer = new EffectDrawer();
            magicUpEffectDrawer = new EffectDrawer();
            speedUpEffectDrawer = new EffectDrawer();
            dexterityUpEffectDrawer = new EffectDrawer();
            evationUpEffectDrawer = new EffectDrawer();

            powerDownEffectDrawer = new EffectDrawer();
            vitalityDownEffectDrawer = new EffectDrawer();
            magicDownEffectDrawer = new EffectDrawer();
            speedDownEffectDrawer = new EffectDrawer();
            dexterityDownEffectDrawer = new EffectDrawer();
            evationDownEffectDrawer = new EffectDrawer();

            powerUpEffectDrawer.load(catalog.getItemFromName("atk_up", typeof(Resource.NSprite)) as Resource.NSprite, this.catalog);
            vitalityUpEffectDrawer.load(catalog.getItemFromName("def_up", typeof(Resource.NSprite)) as Resource.NSprite, this.catalog);
            magicUpEffectDrawer.load(catalog.getItemFromName("mag_up", typeof(Resource.NSprite)) as Resource.NSprite, this.catalog);
            speedUpEffectDrawer.load(catalog.getItemFromName("agi_up", typeof(Resource.NSprite)) as Resource.NSprite, this.catalog);
            dexterityUpEffectDrawer.load(catalog.getItemFromName("hit_up", typeof(Resource.NSprite)) as Resource.NSprite, this.catalog);
            evationUpEffectDrawer.load(catalog.getItemFromName("avoid_up", typeof(Resource.NSprite)) as Resource.NSprite, this.catalog);

            powerDownEffectDrawer.load(catalog.getItemFromName("atk_down", typeof(Resource.NSprite)) as Resource.NSprite, this.catalog);
            vitalityDownEffectDrawer.load(catalog.getItemFromName("def_down", typeof(Resource.NSprite)) as Resource.NSprite, this.catalog);
            magicDownEffectDrawer.load(catalog.getItemFromName("mag_down", typeof(Resource.NSprite)) as Resource.NSprite, this.catalog);
            speedDownEffectDrawer.load(catalog.getItemFromName("agi_down", typeof(Resource.NSprite)) as Resource.NSprite, this.catalog);
            dexterityDownEffectDrawer.load(catalog.getItemFromName("hit_down", typeof(Resource.NSprite)) as Resource.NSprite, this.catalog);
            evationDownEffectDrawer.load(catalog.getItemFromName("avoid_down", typeof(Resource.NSprite)) as Resource.NSprite, this.catalog);

            powerUpEffectDrawer.initialize();
            vitalityUpEffectDrawer.initialize();
            magicUpEffectDrawer.initialize();
            speedUpEffectDrawer.initialize();
            dexterityUpEffectDrawer.initialize();
            evationUpEffectDrawer.initialize();

            powerDownEffectDrawer.initialize();
            vitalityDownEffectDrawer.initialize();
            magicDownEffectDrawer.initialize();
            speedDownEffectDrawer.initialize();
            dexterityDownEffectDrawer.initialize();
            evationDownEffectDrawer.initialize();

            ClearConditionEffectDrawer();
            foreach (Rom.Condition condition in catalog.getFilteredItemList(typeof(Rom.Condition)))
            {
                var effectDrawer = new EffectDrawer();

                effectDrawer.load(catalog.getItemFromGuid(condition.effect) as Resource.NSprite, this.catalog);

                conditionEffectDrawerDic.Add(condition.guId, effectDrawer);

                effectDrawer.initialize();
            }
        }

        internal virtual void BattleStart(List<BattlePlayerData> playerData, List<BattleEnemyData> enemyMonsterData)
        {
            refreshLayout(playerData, enemyMonsterData);
            effectDrawTargetMonsterList.Clear();
            defeatEffectDrawTargetList.Clear();
            prevSelectedMonster = null;

            displayWindow = WindowType.None;
            displayMessageText = "";
        }

        internal void refreshLayout(List<BattlePlayerData> playerData, List<BattleEnemyData> enemyMonsterData)
        {
            // ステータスウィンドウの表示位置を設定
            // Set the display position of the status window
            // パーティの人数と対応する表示位置
            // Party size and corresponding display position
            // ┏━━┓
            // ┏━━┓
            // ┃1  2┃
            // ┃1 2┃
            // ┃3  4┃
            // ┃3 4┃
            // ┗━━┛
            // ┗━━┛
            for (int playerIndex = 0; playerIndex < playerData.Count; playerIndex++)
            {
                var player = playerData[playerIndex];

                Vector2 basePosition;

                basePosition.X = Graphics.ScreenWidth * (playerIndex % 2);
                basePosition.Y = Graphics.ScreenHeight / (2 - ((playerIndex % 4) / 2));

                player.statusWindowDrawPosition.X = basePosition.X - (StatusWindowSize.X * (playerIndex % 2));
                player.statusWindowDrawPosition.Y = basePosition.Y - StatusWindowSize.Y;

                player.characterImageSize = new Vector2(192, 272);

                player.characterImagePosition = basePosition;
                player.characterImagePosition.X -= (player.characterImageSize.X * (playerIndex % 2));
                player.characterImagePosition.Y -= player.characterImageSize.Y;

                player.EffectPosition = player.statusWindowDrawPosition + StatusWindowSize / 2;
                player.DamageTextPosition = player.EffectPosition;
            }

            //int[][] monsterPositionIndex = new int[(int)BattleEnemyData.MonsterSize.Max][];

            if (enemyMonsterData == null)
                return;

            // 敵モンスターの表示位置を設定
            // Set the display position of enemy monsters
            BattleEnemyData.MonsterArrangementType[] monsterArrangements = null;

            switch (enemyMonsterData.Count)
            {
                case 1:
                    monsterArrangements = new BattleEnemyData.MonsterArrangementType[] { BattleEnemyData.MonsterArrangementType.MiddleCenter, };
                    break;

                case 2:
                    monsterArrangements = new BattleEnemyData.MonsterArrangementType[] { BattleEnemyData.MonsterArrangementType.MiddleLeft, BattleEnemyData.MonsterArrangementType.MiddleRight };
                    break;

                case 3:
                    monsterArrangements = new BattleEnemyData.MonsterArrangementType[] { BattleEnemyData.MonsterArrangementType.ForwardLeft, BattleEnemyData.MonsterArrangementType.MiddleCenter, BattleEnemyData.MonsterArrangementType.ForwardRight };
                    break;

                case 4:
                    monsterArrangements = new BattleEnemyData.MonsterArrangementType[] { BattleEnemyData.MonsterArrangementType.ForwardLeft, BattleEnemyData.MonsterArrangementType.MiddleLeft, BattleEnemyData.MonsterArrangementType.MiddleRight, BattleEnemyData.MonsterArrangementType.ForwardRight };
                    break;

                case 5:
                    monsterArrangements = new BattleEnemyData.MonsterArrangementType[] { BattleEnemyData.MonsterArrangementType.ForwardLeft, BattleEnemyData.MonsterArrangementType.MiddleLeft, BattleEnemyData.MonsterArrangementType.BackCenter, BattleEnemyData.MonsterArrangementType.MiddleRight, BattleEnemyData.MonsterArrangementType.ForwardRight };
                    break;

                case 6:
                    monsterArrangements = new BattleEnemyData.MonsterArrangementType[] { BattleEnemyData.MonsterArrangementType.ForwardLeft, BattleEnemyData.MonsterArrangementType.MiddleLeft, BattleEnemyData.MonsterArrangementType.BackLeft, BattleEnemyData.MonsterArrangementType.BackRight, BattleEnemyData.MonsterArrangementType.MiddleRight, BattleEnemyData.MonsterArrangementType.ForwardRight };
                    break;

                case 7:
                    monsterArrangements = new BattleEnemyData.MonsterArrangementType[] { BattleEnemyData.MonsterArrangementType.ForwardLeft, BattleEnemyData.MonsterArrangementType.MiddleLeft, BattleEnemyData.MonsterArrangementType.BackLeft, BattleEnemyData.MonsterArrangementType.BackCenter, BattleEnemyData.MonsterArrangementType.BackRight, BattleEnemyData.MonsterArrangementType.MiddleRight, BattleEnemyData.MonsterArrangementType.ForwardRight };
                    break;

                case 8:
                    monsterArrangements = new BattleEnemyData.MonsterArrangementType[] { BattleEnemyData.MonsterArrangementType.ForwardLeft, BattleEnemyData.MonsterArrangementType.MiddleLeft, BattleEnemyData.MonsterArrangementType.BackLeft, BattleEnemyData.MonsterArrangementType.BackCenter, BattleEnemyData.MonsterArrangementType.MiddleCenter, BattleEnemyData.MonsterArrangementType.BackRight, BattleEnemyData.MonsterArrangementType.MiddleRight, BattleEnemyData.MonsterArrangementType.ForwardRight };
                    break;

                case 9:
                    monsterArrangements = new BattleEnemyData.MonsterArrangementType[] { BattleEnemyData.MonsterArrangementType.ForwardLeft, BattleEnemyData.MonsterArrangementType.MiddleLeft, BattleEnemyData.MonsterArrangementType.BackLeft, BattleEnemyData.MonsterArrangementType.BackCenter, BattleEnemyData.MonsterArrangementType.MiddleCenter, BattleEnemyData.MonsterArrangementType.ForwardCenter, BattleEnemyData.MonsterArrangementType.ForwardRight, BattleEnemyData.MonsterArrangementType.MiddleRight, BattleEnemyData.MonsterArrangementType.BackRight };
                    break;
            }

            int monsterCount = 0;

            foreach (var monster in enemyMonsterData)
            {
                if (monster.IsManualPosition)
                {
                    continue;
                }

                monster.arrangmentType = monsterArrangements[monsterCount];

                monster.DamageTextPosition = GetMonsterDrawPosition(monster);

                monsterCount++;
            }
        }

        private void ClearConditionEffectDrawer()
        {
            foreach (var e in conditionEffectDrawerDic)
            {
                e.Value.finalize();
            }
            conditionEffectDrawerDic.Clear();

        }

        internal virtual void ReleaseResourceData()
        {
            Graphics.UnloadImage(windowDrawer.WindowResource);

            Graphics.UnloadImage(battleStatusWindowDrawer.getWindowRes());
            battleStatusWindowDrawer.Release();

            battleCommandChoiceWindowDrawer.Release();
            skillSelectWindowDrawer.Release();
            itemSelectWindowDrawer.Release();
            stockSelectWindowDrawer.Release();

            Graphics.UnloadImage(battleCommandChoiceWindowDrawer.BaseWindowDrawer.WindowResource);
            Graphics.UnloadImage(skillSelectWindowDrawer.BaseWindowDrawer.WindowResource);
            Graphics.UnloadImage(itemSelectWindowDrawer.BaseWindowDrawer.WindowResource);

            Graphics.UnloadImage(stockSelectWindowDrawer.CursorDrawer.WindowResource);
            Graphics.UnloadImage(battleCommandChoiceWindowDrawer.CursorDrawer.WindowResource);
            if (battleCommandChoiceWindowDrawer.ChoiceItemBackgroundDrawer != null)
                Graphics.UnloadImage(battleCommandChoiceWindowDrawer.ChoiceItemBackgroundDrawer.WindowResource);

            Graphics.UnloadImage(skillSelectWindowDrawer.CursorDrawer.WindowResource);
            Graphics.UnloadImage(skillSelectWindowDrawer.ChoiceItemBackgroundDrawer.WindowResource);

            Graphics.UnloadImage(itemSelectWindowDrawer.CursorDrawer.WindowResource);
            Graphics.UnloadImage(itemSelectWindowDrawer.ChoiceItemBackgroundDrawer.WindowResource);

            Graphics.UnloadImage(damageNumberImageId);
            Graphics.UnloadImage(serifImageId);

            playerEffectDrawer.finalize();
            monsterEffectDrawer.finalize();
            defeatEffectDrawer.finalize();

            powerUpEffectDrawer.finalize();
            vitalityUpEffectDrawer.finalize();
            magicUpEffectDrawer.finalize();
            speedUpEffectDrawer.finalize();
            dexterityUpEffectDrawer.finalize();
            evationUpEffectDrawer.finalize();

            powerDownEffectDrawer.finalize();
            vitalityDownEffectDrawer.finalize();
            magicDownEffectDrawer.finalize();
            speedDownEffectDrawer.finalize();
            dexterityDownEffectDrawer.finalize();
            evationDownEffectDrawer.finalize();

            ClearConditionEffectDrawer();
        }

        internal virtual void Update(List<BattlePlayerData> playerData, List<BattleEnemyData> enemyMonsterData)
        {
            viewerTimer += GameMain.getRelativeParam60FPS();

            blinker.update();
            choiceWindowCursolColor.update();

            if (tweenBattleCommandWindowScale.IsPlayTween)
            {
                tweenBattleCommandWindowScale.Update();
            }

            if (messageWindowPosition.IsPlayTween)
            {
                messageWindowPosition.Update();
            }

            if (openingMonsterScaleTweener.IsPlayTween)
            {
                openingMonsterScaleTweener.Update();
            }

            if (openingColorTweener.IsPlayTween)
            {
                openingColorTweener.Update();
            }

            if (!playerEffectDrawer.isEndPlaying)
            {
                playerEffectDrawer.update();
            }

            if (!monsterEffectDrawer.isEndPlaying)
            {
                monsterEffectDrawer.update();
            }

            if (!defeatEffectDrawer.isEndPlaying)
            {
                defeatEffectDrawer.update();
            }

            if (fadeoutEnemyList.Count > 0)
            {
                foreach (var enemy in fadeoutEnemyList)
                {
                    if (!IsEndMotion(enemy))
                    {
                        enemy.imageAlpha = 1;
                        continue;
                    }

                    enemy.imageAlpha -= (FADEINOUT_SPEED * GameMain.getRelativeParam60FPS());
                    Console.WriteLine("imageAlpha- : " + enemy.imageAlpha + " id:" + enemy.UniqueID);
                }

                fadeoutEnemyList.RemoveAll(enemy => enemy.imageAlpha <= 0);
            }

            if (fadeinEnemyList.Count > 0)
            {
                foreach (var enemy in fadeinEnemyList)
                {
                    enemy.imageAlpha += (FADEINOUT_SPEED * GameMain.getRelativeParam60FPS());
                    Console.WriteLine("imageAlpha+ : " + enemy.imageAlpha + " id:" + enemy.UniqueID);
                }

                fadeinEnemyList.RemoveAll(enemy => enemy.imageAlpha >= 1.0f);
            }

            var activeEffects = new HashSet<EffectDrawer>();
            var battleCharacters = new List<BattleCharacterBase>();
            battleCharacters.AddRange(playerData.Cast<BattleCharacterBase>());
            battleCharacters.AddRange(enemyMonsterData.Cast<BattleCharacterBase>());

            // ステータス上昇, 下降, 状態異常エフェクトの更新
            // Updating stats, descents, status effect effects
            // 1フレームで何度も更新されてしまうのを防ぐためここで一括して更新する
            // Update all at once to prevent multiple updates in one frame
            foreach (var character in battleCharacters)
            {
                activeEffects.Add(character.positiveEffectDrawers.ElementAtOrDefault(character.positiveEffectIndex));
                activeEffects.Add(character.negativeEffectDrawers.ElementAtOrDefault(character.negativeEffectIndex));
                activeEffects.Add(character.statusEffectDrawers.ElementAtOrDefault(character.statusEffectIndex));
            }

            activeEffects.RemoveWhere(drawer => drawer == null);

            foreach (var drawer in activeEffects)
            {
                drawer.update();
            }

            // 再生が終わったら次のエフェクトを再生する準備
            // Ready to play next effect when finished playing
            foreach (var character in battleCharacters)
            {
                if (character.positiveEffectDrawers.Count > character.positiveEffectIndex && character.positiveEffectDrawers[character.positiveEffectIndex].isEndPlaying)
                {
                    character.positiveEffectIndex = (character.positiveEffectIndex + 1) % character.positiveEffectDrawers.Count;
                }

                if (character.negativeEffectDrawers.Count > character.negativeEffectIndex && character.negativeEffectDrawers[character.negativeEffectIndex].isEndPlaying)
                {
                    character.negativeEffectIndex = (character.negativeEffectIndex + 1) % character.negativeEffectDrawers.Count;
                }

                if (character.statusEffectDrawers.Count > character.statusEffectIndex && character.statusEffectDrawers[character.statusEffectIndex].isEndPlaying)
                {
                    character.statusEffectIndex = (character.statusEffectIndex + 1) % character.statusEffectDrawers.Count;
                }
            }

            // ループ再生されるように終わったエフェクトを初期化する
            // Initialize finished effects to loop
            foreach (var drawer in activeEffects.Where(drawer => drawer.isEndPlaying))
            {
                drawer.initialize();
                drawer.update();
            }
        }

        internal virtual void Draw(List<BattlePlayerData> playerData, List<BattleEnemyData> enemyMonsterData)
        {
            BattleEnemyData currentSelectMonster = null;

            // 敵の画像を表示
            // Show enemy image
            foreach (var monster in enemyMonsterData.Where(monster => monster.imageAlpha > 0).OrderBy(monster => monster.IsSelect).ThenByDescending(monster => monster.arrangmentType))
            {
                var color = Color.White;
                if (monster.imageAlpha < 1)
                {
                    color = new Color(monster.imageAlpha, monster.imageAlpha, monster.imageAlpha, 0);
                }

                float scale = GetMonsterDrawScale(monster, enemyMonsterData.Count);

                bool isEnableBlinker = false;

                if (openingMonsterScaleTweener.IsPlayTween && openingColorTweener.IsPlayTween)
                {
                    scale *= openingMonsterScaleTweener.CurrentValue;
                }
                else if (effectDrawTargetMonsterList.Contains(monster))
                {
                    // エフェクトを受けてる時はエフェクトターゲット色を適用
                    // Apply effect target color when receiving effect
                    color = monsterEffectDrawer.getNowTargetColor();
                }
                else if (defeatEffectDrawTargetList.Contains(monster))
                {
                    // エフェクトを受けてる時はエフェクトターゲット色を適用
                    // Apply effect target color when receiving effect
                    color = defeatEffectDrawer.getNowTargetColor();
                }
                else
                {
                    if (displayWindow == WindowType.CommandTargetMonsterListWindow)
                    {
                        if (monster.IsSelect)
                        {
                            // ターゲットとして選択されている時は点滅させる
                            // Blink when selected as a target
                            isEnableBlinker = true;

                            currentSelectMonster = monster;
                        }
                        else
                        {
                            color.R = color.G = color.B = 64;
                            color.A = 192;
                        }
                    }
                }

                if (monster.commandEffectColor.IsPlayTween)
                {
                    color = monster.commandEffectColor.CurrentValue;
                }

                Vector2 pos = GetMonsterDrawPosition(monster);

                monster.EffectPosition = pos;

                if (monster.imageId != null)
                {
                    int imageWidth = Graphics.GetImageWidth(monster.imageId);
                    int imageHeight = Graphics.GetImageHeight(monster.imageId);
                    var sourceRect = new Rectangle(0, 0, imageWidth, imageHeight);
                    var distRect = GetMonsterDrawRect(monster, scale);

                    Graphics.DrawImage(monster.imageId, distRect, sourceRect, color);

                    if (isEnableBlinker)
                    {
                        Graphics.DrawImage(monster.imageId, distRect, sourceRect, blinker.getColor());
                    }

                    if (monsterEffectDrawer.origPos == Resource.NSprite.OrigPos.ORIG_FOOT)
                    {
                        // 足元
                        // foot
                        monster.EffectPosition = new Vector2(
                            distRect.X + distRect.Width / 2,
                            distRect.Y + distRect.Height);
                    }
                    else
                    {
                        // 通常
                        // generally
                        monster.EffectPosition = new Vector2(
                            distRect.X + distRect.Width / 2,
                            distRect.Y + distRect.Height / 2);
                    }
                }
            }

            // 選択解除
            // Deselect
            if (enemyMonsterData.Count(monster => monster.IsSelect) == 0)
            {
                prevSelectedMonster = null;
            }

            // 現在選択しているモンスターの ステータスUp情報, ステータスDown情報, 状態異常 を表示する
            // Displays status up information, status down information, and abnormal status of the currently selected monster.
            // 他のモンスターの画像に隠れないように全てのモンスターの画像を描画し終わってから描画
            // Draw after drawing all the monster images so that they are not hidden by the images of other monsters.
            if (currentSelectMonster != null)
            {
                if (currentSelectMonster != prevSelectedMonster)
                {
                    SetMonsterStatusEffect(currentSelectMonster);
                }

                prevSelectedMonster = currentSelectMonster;

                var effects = new List<EffectDrawer>();

                effects.Add(currentSelectMonster.positiveEffectDrawers.ElementAtOrDefault(currentSelectMonster.positiveEffectIndex));
                effects.Add(currentSelectMonster.negativeEffectDrawers.ElementAtOrDefault(currentSelectMonster.negativeEffectIndex));
                effects.Add(currentSelectMonster.statusEffectDrawers.ElementAtOrDefault(currentSelectMonster.statusEffectIndex));

                effects.RemoveAll(effect => effect == null);

                Vector2 effectGraphicsSize = new Vector2(48, 48);
                Vector2 effectDrawPosition = GetMonsterDrawPosition(currentSelectMonster) - ((effects.Count - 1) * 0.5f * new Vector2(effectGraphicsSize.X, 0)) - new Vector2(0, Graphics.GetImageHeight(currentSelectMonster.imageId) * 0.5f + effectGraphicsSize.Y * 0.5f);

                // エフェクトがメッセージウィンドウと重ならないように位置を調整
                // Adjust the position so that the effect does not overlap the message window
                if (effectDrawPosition.Y <= Graphics.ScreenHeight * 0.2f)
                {
                    effectDrawPosition.Y = Graphics.ScreenHeight * 0.2f;
                }

                foreach (var effectDrawer in effects)
                {
                    effectDrawer.draw((int)effectDrawPosition.X, (int)effectDrawPosition.Y);

                    effectDrawPosition.X += effectGraphicsSize.X;
                }
            }

            // プレイヤー側のパラメータを表示
            // Display player side parameters
            foreach (var player in playerData)
            {
                if (!player.IsBattle)
                {
                    continue;
                }

                Vector2 statusWindowSize = StatusWindowSize;

                // キャラクター画像の向きに応じてウィンドウの下地を反転させる
                // Invert the window background according to the orientation of the character image
                /*
                if (player.isCharacterImageReverse)
                {
                    statusWindowSize.X *= -1;
                }
                */

                battleStatusWindowDrawer.PositiveEnhanceEffect = player.positiveEffectDrawers.ElementAtOrDefault(player.positiveEffectIndex);
                battleStatusWindowDrawer.NegativeEnhanceEffect = player.negativeEffectDrawers.ElementAtOrDefault(player.negativeEffectIndex);
                battleStatusWindowDrawer.StatusAilmentEffect = player.statusEffectDrawers.ElementAtOrDefault(player.statusEffectIndex);

                if (displayWindow == WindowType.CommandTargetPlayerListWindow && player.IsSelect)
                {
                    // ターゲットとして選択されている時は点滅させる
                    // Blink when selected as a target
                    battleStatusWindowDrawer.Draw(player.battleStatusData, player.statusWindowDrawPosition, statusWindowSize, blinker.getColor());
                }
                else if (effectDrawTargetPlayerList.Contains(player))
                {
                    // エフェクトを受けてる時はエフェクトターゲット色を適用
                    // Apply effect target color when receiving effect
                    battleStatusWindowDrawer.Draw(player.battleStatusData, player.statusWindowDrawPosition, statusWindowSize, playerEffectDrawer.getNowTargetColor());
                }
                else
                {
                    bool isBloomStatusWindow = (player.statusWindowState == StatusWindowState.Active);

                    battleStatusWindowDrawer.Draw(player.battleStatusData, player.statusWindowDrawPosition, statusWindowSize, isBloomStatusWindow);
                }
            }

            if (!playerEffectDrawer.isEndPlaying)
            {
                if (playerEffectDrawer.origPos == Resource.NSprite.OrigPos.ORIG_SCREEN)
                {
                    // 画面全体を対象としたエフェクトの場合、ターゲット色だけを反映して、エフェクト自体は画面全体に出す
                    // In the case of an effect that targets the entire screen, only the target color is reflected, and the effect itself is displayed on the entire screen.
                    playerEffectDrawer.draw(Graphics.ScreenWidth / 2, Graphics.ScreenHeight / 2);
                }
                else
                {
                    playerEffectDrawer.drawFlash();

                    foreach (var target in effectDrawTargetPlayerList)
                    {
                        playerEffectDrawer.draw((int)target.EffectPosition.X, (int)target.EffectPosition.Y, false);
                    }
                }
            }

            if (!monsterEffectDrawer.isEndPlaying)
            {
                if (monsterEffectDrawer.origPos == Resource.NSprite.OrigPos.ORIG_SCREEN)
                {
                    // 画面全体を対象としたエフェクトの場合、ターゲット色だけを反映して、エフェクト自体は画面全体に出す
                    // In the case of an effect that targets the entire screen, only the target color is reflected, and the effect itself is displayed on the entire screen.
                    monsterEffectDrawer.draw(Graphics.ScreenWidth / 2, Graphics.ScreenHeight / 2);
                }
                else
                {
                    monsterEffectDrawer.drawFlash();

                    foreach (var target in effectDrawTargetMonsterList)
                    {
                        monsterEffectDrawer.draw((int)target.EffectPosition.X, (int)target.EffectPosition.Y, false);
                    }
                }
            }

            if (!defeatEffectDrawer.isEndPlaying)
            {
                if (defeatEffectDrawer.origPos == Resource.NSprite.OrigPos.ORIG_SCREEN)
                {
                    // 画面全体を対象としたエフェクトの場合、ターゲット色だけを反映して、エフェクト自体は画面全体に出す
                    // In the case of an effect that targets the entire screen, only the target color is reflected, and the effect itself is displayed on the entire screen.
                    defeatEffectDrawer.draw(Graphics.ScreenWidth / 2, Graphics.ScreenHeight / 2);
                }
                else
                {
                    defeatEffectDrawer.drawFlash();

                    foreach (var target in defeatEffectDrawTargetList)
                    {
                        defeatEffectDrawer.draw((int)target.EffectPosition.X, (int)target.EffectPosition.Y, false);
                    }
                }
            }

            if (damageTextList != null && damageTextList.Count() > 0)
            {
                var removeList = new List<BattleDamageTextInfo>();

                foreach (var info in damageTextList.Where(info => info.textInfo != null).GroupBy(info => info.targetCharacter).Select(group => group.FirstOrDefault()).Where(info => info != null))
                {
                    Color color = Color.White;
                    if (info.type < BattleDamageTextInfo.TextType.Miss)
                        color = catalog.getGameSettings().damageNumColors[(int)info.type];

                    //switch (info.type)
                    //{
                    //    case BattleDamageTextInfo.TextType.HitPointDamage: color = Color.White; break;
                    //    case BattleDamageTextInfo.TextType.MagicPointDamage: color = Color.Orchid; break;
                    //    case BattleDamageTextInfo.TextType.CriticalDamage: color = Color.Red; break;
                    //    case BattleDamageTextInfo.TextType.HitPointHeal: color = new Color(191, 191, 255); break;
                    //    case BattleDamageTextInfo.TextType.MagicPointHeal: color = Color.LightGreen; break;
                    //}

                    Vector2 basePosition = info.targetCharacter.DamageTextPosition;
                    float characterMarginX = Graphics.GetDivWidth(damageNumberImageId) * 0.8f;

                    // アニメーション用タイマーの更新
                    // Animation timer update
                    foreach (var characterInfo in info.textInfo)
                    {
                        characterInfo.timer += GameMain.getRelativeParam60FPS();

                        Vector2 position = basePosition;
                        float amount = characterInfo.timer / 30.0f * 4;

                        amount = Math.Max(amount, 0.0f);
                        amount = Math.Min(amount, 4.0f);

                        float duration = 0;

                        var timing = new float[] { 3.2f, 2.4f, 1.2f };
                        var scale = new float[] { 0.8f, 0.8f, 1.2f, 1.2f };

                        float amountBak = amount;

                        if (amount > timing[0])
                        {
                            amount -= timing[0];
                            amount /= scale[0];
                            duration = (1 - amount * amount) * 0.33f;
                        }
                        else if (amount > timing[1])
                        {
                            amount -= timing[1];
                            amount /= scale[1];
                            duration = (1 - (1 - amount) * (1 - amount)) * 0.33f;
                        }
                        else if (amount > timing[2])
                        {
                            amount -= timing[2];
                            amount /= scale[2];
                            duration = 1 - amount * amount;
                        }
                        else
                        {
                            amount /= scale[3];
                            duration = 1 - (1 - amount) * (1 - amount);
                        }

                        amount = amountBak;

                        position.Y += (duration * -25);

                        // 数字だけは画像で それ以外の文字はテキストで描画
                        // Only numbers are drawn as images, other characters are drawn as text
                        if (info.IsNumberOnlyText)
                        {
                            position += new Vector2(-1 * Graphics.GetImageWidth(damageNumberImageId) / 2.0f - ((info.text.Length - 1) * characterMarginX / 2), 0);

                            int srcChipX = 0;
                            int srcChipY = (int)(characterInfo.c - '0');

                            if (srcChipX >= 0 && srcChipY >= 0)
                            {
                                if (duration != 0 || amount > 1)
                                    Graphics.DrawChipImage(damageNumberImageId, (int)position.X, (int)position.Y, srcChipX, srcChipY, color.R, color.G, color.B, color.A);

                                basePosition.X += characterMarginX;
                            }
                        }
                        else
                        {
                            position.X -= textDrawer.MeasureString(info.text).X / 2;
                            textDrawer.DrawStringSoloColor(characterInfo.c.ToString(), position, color);
                            basePosition.X += textDrawer.MeasureString(characterInfo.c.ToString()).X;
                        }
                    }

                    if (info.textInfo.Count(textinfo => textinfo.timer < 40) == 0)
                    {
                        removeList.Add(info);
                    }
                }

                damageTextList = damageTextList.Except(removeList);
            }

            var windowPos = Vector2.Zero;
            var windowSize = Vector2.Zero;

            switch (displayWindow)
            {
                case WindowType.MessageWindow:
                    windowSize = new Vector2(Graphics.ScreenWidth - StatusWindowSize.X * 2, Graphics.ScreenHeight * 0.1f);
                    windowPos = new Vector2(StatusWindowSize.X, 8);//messageWindowPosition.CurrentValue - new Vector2(windowSize.X * 0.5f, -8);

                    windowDrawer.Draw(windowPos, windowSize, Color.White);
                    textDrawer.DrawStringSoloColor(displayMessageText, windowPos, windowSize, TextDrawer.HorizontalAlignment.Center, TextDrawer.VerticalAlignment.Center, Color.White);
                    break;

                case WindowType.PlayerCommandWindow:
                    foreach (var player in playerData)
                    {
                        //if (player.IsSelect || player.selectedBattleCommand == null || player.selectedBattleCommandType == BattleCommandType.Nothing) break;

                        //textDrawer.DrawString(player.selectedBattleCommand.name, player.statusWindowDrawPosition, Color.Black);
                    }

                    if (battleCommandChoiceWindowDrawer.ChoiceItemCount <= BattlePlayerData.RegisterBattleCommandCountMax)
                    {
                        windowSize = new Vector2(230, 260);
                    }
                    else
                    {
                        windowSize = new Vector2(230, 310);
                    }

                    windowPos = CommandWindowBasePosition;
                    windowPos.Y += (windowSize.Y / 2 - (windowSize.Y / 2 * tweenBattleCommandWindowScale.CurrentValue.Y));

                    windowSize *= tweenBattleCommandWindowScale.CurrentValue;

                    battleCommandChoiceWindowDrawer.CursorColor = choiceWindowCursolColor.getColor();
                    battleCommandChoiceWindowDrawer.Draw(windowPos, windowSize);
                    break;

                case WindowType.SkillListWindow:
                    windowPos = new Vector2(175, 10);
                    windowSize = new Vector2(625, 500);

                    skillSelectWindowDrawer.CursorColor = choiceWindowCursolColor.getColor();
                    skillSelectWindowDrawer.Draw(windowPos, windowSize, ChoiceWindowDrawer.HeaderStyle.None, ChoiceWindowDrawer.FooterStyle.InfoDescription);
                    break;

                case WindowType.ItemListWindow:
                    windowPos = new Vector2(175, 10);
                    windowSize = new Vector2(625, 500);

                    itemSelectWindowDrawer.CursorColor = choiceWindowCursolColor.getColor();
                    itemSelectWindowDrawer.Draw(windowPos, windowSize, ChoiceWindowDrawer.HeaderStyle.None, ChoiceWindowDrawer.FooterStyle.InfoDescription);
                    break;

                case WindowType.CommandTargetPlayerListWindow:
                case WindowType.CommandTargetMonsterListWindow:
                    windowDrawer.Draw(new Vector2(StatusWindowSize.X, 8), new Vector2(Graphics.ScreenWidth - StatusWindowSize.X * 2, Graphics.ScreenHeight * 0.1f));
                    textDrawer.DrawStringSoloColor(displayMessageText, new Vector2(StatusWindowSize.X, 8), new Vector2(Graphics.ScreenWidth - StatusWindowSize.X * 2, Graphics.ScreenHeight * 0.1f), TextDrawer.HorizontalAlignment.Center, TextDrawer.VerticalAlignment.Center, Color.White);
                    break;

            }

            // フキダシのひげ部分を表示
            // Display the whiskers of the balloon
            switch (displayWindow)
            {
                case WindowType.PlayerCommandWindow:
                    int width = Graphics.GetImageWidth(serifImageId);
                    int height = Graphics.GetImageHeight(serifImageId);
                    int x = (int)windowPos.X;
                    int y = (int)(windowPos.Y + (int)(windowSize.Y / 2) - height / 2);   // ウィンドウサイズによっては表示位置が微妙に変化しガタガタと震えているように見えるのを軽減するために計算途中で小数部分を切り捨てる / Depending on the window size, the display position changes slightly and the decimal part is truncated in the middle of the calculation to reduce the appearance of shaking.
                    var rect = new Rectangle(width, 0, width, height);

                    if (BallonImageReverse)
                    {
                        x += (int)windowSize.X;
                    }
                    else
                    {
                        x -= width;

                        rect.X += rect.Width;
                        rect.Width *= -1;
                    }

                    Graphics.DrawImage(serifImageId, new Rectangle(x, y, width, height), rect);
                    break;
            }
        }

        protected static Vector2 GetMonsterDrawPosition(BattleEnemyData monster)
        {
            Vector2 pos = Vector2.Zero;

            switch (monster.arrangmentType)
            {
                case BattleEnemyData.MonsterArrangementType.ForwardLeft:
                    pos.X = Graphics.ScreenWidth * 0.25f;
                    break;

                case BattleEnemyData.MonsterArrangementType.MiddleLeft:
                    pos.X = Graphics.ScreenWidth * 0.325f;
                    break;

                case BattleEnemyData.MonsterArrangementType.BackLeft:
                    pos.X = Graphics.ScreenWidth * 0.4f;
                    break;

                case BattleEnemyData.MonsterArrangementType.ForwardCenter:
                case BattleEnemyData.MonsterArrangementType.MiddleCenter:
                case BattleEnemyData.MonsterArrangementType.BackCenter:
                    pos.X = Graphics.ScreenWidth * 0.5f;
                    break;

                case BattleEnemyData.MonsterArrangementType.ForwardRight:
                    pos.X = Graphics.ScreenWidth * 0.75f;
                    break;

                case BattleEnemyData.MonsterArrangementType.MiddleRight:
                    pos.X = Graphics.ScreenWidth * 0.675f;
                    break;

                case BattleEnemyData.MonsterArrangementType.BackRight:
                    pos.X = Graphics.ScreenWidth * 0.6f;
                    break;
            }

            switch (monster.arrangmentType)
            {
                case BattleEnemyData.MonsterArrangementType.ForwardLeft:
                case BattleEnemyData.MonsterArrangementType.ForwardCenter:
                case BattleEnemyData.MonsterArrangementType.ForwardRight:
                    pos.Y = Graphics.ScreenHeight * 0.7f;
                    break;

                case BattleEnemyData.MonsterArrangementType.MiddleLeft:
                case BattleEnemyData.MonsterArrangementType.MiddleCenter:
                case BattleEnemyData.MonsterArrangementType.MiddleRight:
                    pos.Y = Graphics.ScreenHeight * 0.55f;
                    break;

                case BattleEnemyData.MonsterArrangementType.BackLeft:
                case BattleEnemyData.MonsterArrangementType.BackCenter:
                case BattleEnemyData.MonsterArrangementType.BackRight:
                    pos.Y = Graphics.ScreenHeight * 0.4f;
                    break;
            }

            return pos;
        }

        public static Rectangle GetMonsterDrawRect(BattleEnemyData monster, float scale)
        {
            int imageWidth = Graphics.GetImageWidth(monster.imageId);
            int imageHeight = Graphics.GetImageHeight(monster.imageId);
            Vector2 pos = GetMonsterDrawPosition(monster);
            var distRect = new Rectangle((int)(pos.X - imageWidth / 2 * scale), (int)(pos.Y - imageHeight / 2 * scale), (int)(imageWidth * scale), (int)(imageHeight * scale));

            return distRect;
        }

        public static float GetMonsterDrawScale(BattleEnemyData monster, int enemyCount)
        {
            float scale = 1.0f;

            if (enemyCount > 3)
            {
                switch (monster.arrangmentType)
                {
                    case BattleEnemyData.MonsterArrangementType.ForwardLeft:
                    case BattleEnemyData.MonsterArrangementType.ForwardCenter:
                    case BattleEnemyData.MonsterArrangementType.ForwardRight:
                        scale = 1.0f;
                        break;

                    case BattleEnemyData.MonsterArrangementType.MiddleLeft:
                    case BattleEnemyData.MonsterArrangementType.MiddleCenter:
                    case BattleEnemyData.MonsterArrangementType.MiddleRight:
                        scale = 0.8f;
                        break;

                    case BattleEnemyData.MonsterArrangementType.BackLeft:
                    case BattleEnemyData.MonsterArrangementType.BackCenter:
                    case BattleEnemyData.MonsterArrangementType.BackRight:
                        scale = 0.6f;
                        break;
                }
            }

            return scale;
        }

        internal void OpenWindow(WindowType windowType)
        {
            if (windowType == displayWindow) return;

            switch (windowType)
            {
                case WindowType.PlayerCommandWindow:
                    tweenBattleCommandWindowScale.Begin(new Vector2(1, 0.2f), Vector2.One, 7);
                    break;
            }

            switch (windowType)
            {
                case WindowType.PlayerCommandWindow:
                case WindowType.SkillListWindow:
                case WindowType.ItemListWindow:
                    choiceWindowCursolColor.setColor(new Color(Color.White, 255), new Color(Color.White, 128), 30);
                    break;
            }

            displayWindow = windowType;
        }

        internal void CloseWindow()
        {
            switch (displayWindow)
            {
                case WindowType.MessageWindow:
                    messageWindowPosition.Begin(new Vector2(Graphics.ScreenWidth * 1.5f, 0), 15);
                    break;
            }

            displayWindow = WindowType.None;
        }

        internal void RegisterChoiceWindowItems(WindowType windowType)
        {
        }

        internal void SetDisplayMessage(string text, WindowType windowType = WindowType.MessageWindow)
        {
            displayMessageText = text;

            if (displayMessageText == "")
            {
                return;
            }

            displayWindow = windowType;

            messageWindowPosition.Begin(new Vector2(-Graphics.ScreenWidth / 2, 0), new Vector2(Graphics.ScreenWidth / 2, 0), 15);
        }

        internal void ClearDisplayMessage()
        {
            displayMessageText = "";
        }

        // ダメージ表示テキスト設定
        // Damage display text setting
        // TODO : 使い方が紛らわしいので設計変更 or 名前変更
        // TODO: Design change or name change because usage is confusing
        internal void SetDamageTextInfo(IEnumerable<BattleDamageTextInfo> damageTextInfo)
        {
            damageTextList = damageTextInfo;
        }
        internal void AddDamageTextInfo(BattleDamageTextInfo info)
        {
            var addList = new List<BattleDamageTextInfo> { info };
            if (damageTextList == null)
                damageTextList = addList;
            else
                damageTextList = damageTextList.ToList().Union(addList);

            SetupDamageTextAnimationImpl(info);
        }
        internal void SetupDamageTextAnimation()
        {
            foreach (var damageText in damageTextList)
            {
                SetupDamageTextAnimationImpl(damageText);
            }
        }
        private void SetupDamageTextAnimationImpl(BattleDamageTextInfo damageText)
        {
            var characterData = new List<BattleDamageTextInfo.CharacterInfo>();

            for (int i = 0; i < damageText.text.Length; i++)
            {
                var characterInfo = new BattleDamageTextInfo.CharacterInfo();

                characterInfo.c = damageText.text[i];

                switch (damageText.type)
                {
                    case BattleDamageTextInfo.TextType.HitPointDamage:
                    case BattleDamageTextInfo.TextType.MagicPointDamage:
                    case BattleDamageTextInfo.TextType.CriticalDamage:
                    case BattleDamageTextInfo.TextType.HitPointHeal:
                    case BattleDamageTextInfo.TextType.MagicPointHeal:
                        characterInfo.timer = (i * -3 - 5);
                        break;
                    case BattleDamageTextInfo.TextType.Miss:
                        characterInfo.timer = (-5);
                        break;
                }

                characterData.Add(characterInfo);
            }

            damageText.textInfo = characterData.ToArray();

            GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, damageText.targetCharacter?.Name ?? "",
                string.Format("Damage / Type : {0} / Number : {1}", damageText.type.ToString(), damageText.text));
        }

        internal void AddFadeOutCharacter(BattleCharacterBase enemyMonster)
        {
            fadeoutEnemyList.Add(enemyMonster);
        }

        internal void AddFadeInCharacter(BattleCharacterBase enemyMonster)
        {
            fadeinEnemyList.Add(enemyMonster);
        }

        private EffectDrawer CreateEffectDrawer(Resource.NSprite rom)
        {
            var effectDrawer = new EffectDrawer();

            effectDrawer.load(rom, catalog);
            effectDrawer.initialize();
            effectDrawer.update();

            return effectDrawer;
        }

        private List<EffectDrawer> GetPositiveEnhanceEffects(BattleCharacterBase character)
        {
            var positiveEnhanceEffects = new List<EffectDrawer>();

            if (character.IsPowerEnhancementUp)
            {
                positiveEnhanceEffects.Add(CreateEffectDrawer(powerUpEffectDrawer.sprite));
            }
            if (character.IsVitalityEnhancementUp)
            {
                positiveEnhanceEffects.Add(CreateEffectDrawer(vitalityUpEffectDrawer.sprite));
            }
            if (character.IsMagicEnhancementUp)
            {
                positiveEnhanceEffects.Add(CreateEffectDrawer(magicUpEffectDrawer.sprite));
            }
            if (character.IsSpeedEnhancementUp)
            {
                positiveEnhanceEffects.Add(CreateEffectDrawer(speedUpEffectDrawer.sprite));
            }

            if (character.IsDexterityEnhancementUp)
            {
                positiveEnhanceEffects.Add(CreateEffectDrawer(dexterityUpEffectDrawer.sprite));
            }
            if (character.IsEvasionEnhancementUp)
            {
                positiveEnhanceEffects.Add(CreateEffectDrawer(evationUpEffectDrawer.sprite));
            }

            return positiveEnhanceEffects;
        }

        private List<EffectDrawer> GetNegativeEnhanceEffects(BattleCharacterBase character)
        {
            var negativeEnhanceEffects = new List<EffectDrawer>();

            if (character.IsPowerEnhancementDown)
            {
                negativeEnhanceEffects.Add(CreateEffectDrawer(powerDownEffectDrawer.sprite));
            }
            if (character.IsVitalityEnhancementDown)
            {
                negativeEnhanceEffects.Add(CreateEffectDrawer(vitalityDownEffectDrawer.sprite));
            }
            if (character.IsMagicEnhancementDown)
            {
                negativeEnhanceEffects.Add(CreateEffectDrawer(magicDownEffectDrawer.sprite));
            }
            if (character.IsSpeedEnhancementDown)
            {
                negativeEnhanceEffects.Add(CreateEffectDrawer(speedDownEffectDrawer.sprite));
            }

            if (character.IsDexterityEnhancementDown)
            {
                negativeEnhanceEffects.Add(CreateEffectDrawer(dexterityDownEffectDrawer.sprite));
            }
            if (character.IsEvasionEnhancementDown)
            {
                negativeEnhanceEffects.Add(CreateEffectDrawer(evationDownEffectDrawer.sprite));
            }

            return negativeEnhanceEffects;
        }

        private List<EffectDrawer> GetStatusEffect(BattleCharacterBase character)
        {
            var statusEffects = new List<EffectDrawer>();

            // 状態異常エフェクト
            // Abnormal status effect
            foreach (var e in character.conditionInfoDic)
            {
                if (conditionEffectDrawerDic.ContainsKey(e.Key))
                {
                    statusEffects.Add(CreateEffectDrawer(conditionEffectDrawerDic[e.Key].sprite));
                }
            }

            return statusEffects;
        }

        internal void SetPlayerStatusEffect(BattlePlayerData player)
        {
            foreach (var effectDrawer in player.positiveEffectDrawers) effectDrawer.finalize();
            foreach (var effectDrawer in player.negativeEffectDrawers) effectDrawer.finalize();
            foreach (var effectDrawer in player.statusEffectDrawers) effectDrawer.finalize();

            player.positiveEffectDrawers = GetPositiveEnhanceEffects(player);
            player.negativeEffectDrawers = GetNegativeEnhanceEffects(player);
            player.statusEffectDrawers = GetStatusEffect(player);

            player.positiveEffectIndex = 0;
            player.negativeEffectIndex = 0;
            player.statusEffectIndex = 0;
        }

        internal void SetMonsterStatusEffect(BattleEnemyData monster)
        {
            foreach (var effectDrawer in monster.positiveEffectDrawers) effectDrawer.finalize();
            foreach (var effectDrawer in monster.negativeEffectDrawers) effectDrawer.finalize();
            foreach (var effectDrawer in monster.statusEffectDrawers) effectDrawer.finalize();

            monster.positiveEffectDrawers = GetPositiveEnhanceEffects(monster);
            monster.negativeEffectDrawers = GetNegativeEnhanceEffects(monster);
            monster.statusEffectDrawers = GetStatusEffect(monster);

            monster.positiveEffectIndex = 0;
            monster.negativeEffectIndex = 0;
            monster.statusEffectIndex = 0;
        }

        internal void SetBattlePlayerEffect(Guid guid, BattleCharacterBase target)
        {
            SetBattleEffect(ref playerEffectDrawer, guid, target);

            effectDrawTargetPlayerList.Add(target);
        }

        internal void SetBattleMonsterEffect(Guid guid, BattleCharacterBase target)
        {
            SetBattleEffect(ref monsterEffectDrawer, guid, target);

            effectDrawTargetMonsterList.Add(target);
        }

        internal void SetDefeatEffect(Guid guid, BattleCharacterBase target)
        {
            SetBattleEffect(ref defeatEffectDrawer, guid, target);

            defeatEffectDrawTargetList.Add(target);
        }

        private void SetBattleEffect(ref EffectDrawerBase effectDrawer, Guid guid, BattleCharacterBase target)
        {
            if (!effectDrawer.isSameRom(guid))
            {
                var effect = catalog.getItemFromGuid(guid);
                effectDrawer.finalize();

                if (effect == null)
                {
                    effectDrawer = new EffectDrawer();
                }
                else
                {
                    effectDrawer = EffectDrawerBase.createAndLoad(effect, catalog);
                }

                //effectDrawer.load(effect, catalog);
            }

            effectDrawer.initialize();
        }

        // 3Dバトルの実装が落ち着いてきたらここの実装を少しずつ外していく
        // Once the implementation of the 3D battle has settled down, gradually remove this implementation
        internal void Draw_Tiny(List<BattlePlayerData> playerData, List<BattleEnemyData> enemyMonsterData)
        {
            var windowPos = Vector2.Zero;
            var windowSize = Vector2.Zero;

            switch (displayWindow)
            {
                //case WindowType.MessageWindow:
                //    windowSize = new Vector2(Graphics.ScreenWidth - StatusWindowSize.X * 2, Graphics.ScreenHeight * 0.1f);
                //    windowPos = new Vector2(StatusWindowSize.X, 8);//messageWindowPosition.CurrentValue - new Vector2(windowSize.X * 0.5f, -8);

                //    windowDrawer.Draw(windowPos, windowSize, Color.White);
                //    textDrawer.DrawStringSoloColor(displayMessageText, windowPos, windowSize, TextDrawer.HorizontalAlignment.Center, TextDrawer.VerticalAlignment.Center, Color.White);
                //    break;

                //case WindowType.SkillListWindow:
                //    windowPos = new Vector2(175, 10);
                //    windowSize = new Vector2(625, 500);

                //    skillSelectWindowDrawer.CursorColor = choiceWindowCursolColor.getColor();
                //    skillSelectWindowDrawer.Draw(windowPos, windowSize, ChoiceWindowDrawer.HeaderStyle.None, ChoiceWindowDrawer.FooterStyle.InfoDescription);
                //    break;

                //case WindowType.ItemListWindow:
                //    windowPos = new Vector2(175, 10);
                //    windowSize = new Vector2(625, 500);

                //    itemSelectWindowDrawer.CursorColor = choiceWindowCursolColor.getColor();
                //    itemSelectWindowDrawer.Draw(windowPos, windowSize, ChoiceWindowDrawer.HeaderStyle.None, ChoiceWindowDrawer.FooterStyle.InfoDescription);
                //    break;

                //case WindowType.CommandTargetPlayerListWindow:
                //case WindowType.CommandTargetMonsterListWindow:
                //    windowDrawer.Draw(new Vector2(StatusWindowSize.X, 8), new Vector2(Graphics.ScreenWidth - StatusWindowSize.X * 2, Graphics.ScreenHeight * 0.1f));
                //    textDrawer.DrawStringSoloColor(displayMessageText, new Vector2(StatusWindowSize.X, 8), new Vector2(Graphics.ScreenWidth - StatusWindowSize.X * 2, Graphics.ScreenHeight * 0.1f), TextDrawer.HorizontalAlignment.Center, TextDrawer.VerticalAlignment.Center, Color.White);
                //    break;

            }
        }

        internal virtual void SetBackGround(Guid battleBg)
        {

        }


        internal virtual bool IsVisibleWindow(WindowType windowType)
        {
            return false;
        }

        internal bool ContainsFadeOutCharacter(BattleEnemyData enemyMonster)
        {
            return fadeoutEnemyList.Contains(enemyMonster);
        }
    }
}



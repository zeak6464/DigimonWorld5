#define WINDOWS
#define ENABLE_TEST
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Yukar.Common;
using Yukar.Common.GameData;
using Yukar.Common.Resource;
using Yukar.Common.Rom;
using Yukar.Engine;
using static Yukar.Engine.BattleEnum;
using AttackAttributeType = System.Guid;
using BattleCommand = Yukar.Common.Rom.BattleCommand;
using Hero = Yukar.Common.GameData.Hero;
using Resource = Yukar.Common.Resource;
using Rom = Yukar.Common.Rom;

namespace Yukar.Battle
{
    /// <summary>
    /// BattleCharacterBase の拡張メソッド定義用クラス
    /// Class for defining extension methods of BattleCharacterBase
    /// </summary>
    static class BattleCharacterBaseEx
    {
        /// <summary>
        /// Speedが0だとゲージが進行しないので、+1した値を返すための拡張メソッド
        /// If the speed is 0, the gauge will not progress, so an extension method to return the value with +1
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int SpeedPlusOne(this BattleCharacterBase b)
        {
            return b.Speed + 1;
        }
    }

    /// <summary>
    /// バトル全般の進行管理を行うクラス
    /// A class that manages the progress of battles in general.
    /// </summary>
    public class BattleSequenceManager : BattleSequenceManagerBase
    {
        public class ExBattlePlayerData : BattlePlayerData
        {
            public ExBattlePlayerData()
            {
                turnGauge = GameMain.instance.mapScene.GetRandom(0.25f, 0f);
            }

            public float turnGauge;

            internal float GetNormalizedTurn(int turn)
            {
                return GetNormalizedTurnImpl(turn, IsDeadCondition() ? 0.001f : this.SpeedPlusOne(), turnGauge);
            }

            internal static float GetNormalizedTurnImpl(int turn, float speed, float turnGauge)
            {
                return (turn - turnGauge) / speed;
            }
        }

        public class ExBattleEnemyData : BattleEnemyData
        {
            public ExBattleEnemyData()
            {
                turnGauge = GameMain.instance.mapScene.GetRandom(0.25f, 0f);
            }

            public float turnGauge;

            internal float GetNormalizedTurn(int turn)
            {
                return ExBattlePlayerData.GetNormalizedTurnImpl(turn, IsDeadCondition() ? 0.001f : this.SpeedPlusOne(), turnGauge);
            }
        }

        /// <summary>
        /// 2Dバトルの背景タイプ
        /// 2D battle background type
        /// </summary>
        enum BackGroundStyle
        {
            FillColor,
            Image,
            Model,
        }
        private bool _isDrawingBattleSceneFlg = false;

        public BattleResultState BattleResult { get; private set; }
        public override bool IsPlayingBattleEffect { get; set; }
        public override bool IsDrawingBattleScene
        {
            get { return _isDrawingBattleSceneFlg; }
            set
            {
                _isDrawingBattleSceneFlg = value;
                setActorsVisibility(value);
            }
        }

        public BattleViewer battleViewer;
        public ResultViewer resultViewer;
        TweenColor fadeScreenColorTweener;

        Dictionary<Guid, Common.Resource.Texture> iconTable;

        internal BattleState battleState;
        BattleState prevBattleState;
        const BattleState WAIT_CTB_GAUGE = BattleState.Free1;
        internal SelectBattleCommandState battleCommandState;
        bool escapeAvailable;
        bool gameoverOnLose;
        float battleStateFrameCount;


        GameMain owner;
        Catalog catalog;
        Rom.GameSettings gameSettings;

        // 戦闘に必要なステータス
        // battle stats
        Party party;
        public int battlePlayerMax;
        public int battleEnemyMax;
        List<BattleCommand> playerBattleCommand;
        internal List<BattlePlayerData> playerData;
        internal List<BattlePlayerData> stockPlayerData;
        internal List<BattleCharacterBase> targetPlayerData;
        internal List<BattleEnemyData> enemyData;
        internal List<BattleEnemyData> stockEnemyData;
        internal List<BattleCharacterBase> targetEnemyData;
        List<BattlePlayerData> playerViewData;
        public override List<BattlePlayerData> PlayerViewDataList => playerViewData;
        List<BattleEnemyData> enemyMonsterViewData;
        public override List<BattleEnemyData> EnemyViewDataList => enemyMonsterViewData;
        Dictionary<Guid, BattleViewer3D.BattleUI.LayoutDrawerController> layoutDic = new Dictionary<AttackAttributeType, BattleViewer3D.BattleUI.LayoutDrawerController>();

        BattlePlayerData leavePlayer;
        BattlePlayerData enterPlayer;
        BattleEnemyData leaveEnemy;
        BattleEnemyData enterEnemy;


        BattleEnemyInfo[] monsterLayouts;

        internal BattleCharacterBase activeCharacter;
        int attackCount;
        internal BattlePlayerData commandSelectPlayer;
        List<BattleCharacterBase> battleEntryCharacters;
        int commandSelectedMemberCount;
        int commandExecuteMemberCount;
        List<RecoveryStatusInfo> recoveryStatusInfo;
        private BattleInfo battleInfo;

        // 状態異常
        // Abnormal status
        public List<Guid> displayedContinueConditions = new List<Guid>();
        public Dictionary<BattleCharacterBase, List<Rom.Condition>> displayedSetConditionsDic = new Dictionary<BattleCharacterBase, List<Rom.Condition>>();

        public BattleViewer3D Viewer { get { return (BattleViewer3D)battleViewer; } }

        /// <summary>
        /// 敵味方どちらが先制攻撃するか
        /// Which enemy or ally will attack first?
        /// </summary>
        enum FirstAttackType
        {
            None = 0,
            Player,
            Monster,
        }

        FirstAttackType firstAttackType = FirstAttackType.None;

        int playerEscapeFailedCount;

        BackGroundStyle backGroundStyle;
        Color backGroundColor;
        Common.Resource.Texture backGroundImageId = null;//#23959-1
                                                         //ModifiedModelInstance backGroundModel;

        internal TweenFloat statusUpdateTweener;
        TweenListManager<float, TweenFloat> openingBackgroundImageScaleTweener;

        Random battleRandom;

        internal BattleEventController battleEvents;
        public override BattleEventControllerBase BattleEvents { get => battleEvents; }
        int idNumber = Common.Rom.Map.maxMaxMonsters + 1;  // モンスターとかぶらないようにする / Avoid running into monsters


        private ResultViewer.ResultProperty resultProperty;

        private const float TIME_FOR_BATTLE_START_MESSEGE = 2f;
        private float elapsedTimeForBattleStart;
        private string battleStartWord;
        private int totalTurn;
        private int itemRate;
        private int moneyRate;
        private Guid waitForCommon;
        private Guid[] waitForCommons = new Guid[2];

        public BattleSequenceManager(GameMain owner, Catalog catalog)
        {
            this.owner = owner;
            this.catalog = catalog;

            gameSettings = catalog.getGameSettings();

            BattleResult = BattleResultState.Standby;

            playerBattleCommand = new List<BattleCommand>();

            battleEntryCharacters = new List<BattleCharacterBase>();

            battleRandom = new Random();

            if (owner.IsBattle2D)
            {
                battleViewer = new BattleViewer(owner);
            }
            else
            {
                battleViewer = new BattleViewer3D(owner);
                ((BattleViewer3D)battleViewer).setOwner(this);
            }

            resultViewer = new ResultViewer(owner);

            iconTable = new Dictionary<Guid, Common.Resource.Texture>();

            statusUpdateTweener = new TweenFloat();
            fadeScreenColorTweener = new TweenColor();
            openingBackgroundImageScaleTweener = new TweenListManager<float, TweenFloat>();

            rewards.LearnSkillResults = new List<Hero.LearnSkillResult>();
        }

        public override void Release()
        {
            if (!owner.IsBattle2D)
            {
                var viewer = battleViewer as BattleViewer3D;
                viewer.reset();
                viewer.finalize();
            }

			foreach (var item in layoutDic)
			{
                item.Value.finalize();
			}

            layoutDic.Clear();
        }

        public override void BattleStart(Party party, BattleEnemyInfo[] monsters, Common.Rom.Map.BattleSetting settings, bool escapeAvailable = true,
            bool gameoverOnLose = true, bool showMessage = true)
        {
            BattleStart(party, monsters, settings.layout?.PlayerLayouts, settings, escapeAvailable, gameoverOnLose, showMessage);

            if (owner != null)
            {
                foreach (var monster in monsters)
                {
                    owner.data.party.AddCast(monster.Id);
                }
            }
        }


        public override void BattleStart(Party party, BattleEnemyInfo[] monsters,
            Vector3[] playerLayouts, Common.Rom.Map.BattleSetting settings, bool escapeAvailable = true, bool gameoverOnLose = true, bool showMessage = true)
        {
            // エンカウントバトルの歩数リセット
            // Encounter battle step count reset
            owner.mapScene.mapEngine.genEncountStep();

            // null例外が出ないよう0個で初期化しておく
            // Initialize with 0 to prevent null exception
            playerData = new List<BattlePlayerData>();
            enemyData = new List<BattleEnemyData>();
            playerViewData = new List<BattlePlayerData>();
            enemyMonsterViewData = new List<BattleEnemyData>();

            this.escapeAvailable = escapeAvailable;
            this.gameoverOnLose = gameoverOnLose;
            this.party = party;

            // フラッシュ開始
            // flash start
            ChangeBattleState(BattleState.StartFlash);
            if (catalog.getItemFromGuid(gameSettings.transitionBattleEnter) == null)
                fadeScreenColorTweener.Begin(new Color(Color.Gray, 0), new Color(Color.Black, 0), 10);

            battleCommandState = SelectBattleCommandState.None;
            BattleResult = BattleResultState.NonFinish;
            IsPlayingBattleEffect = true;
            IsDrawingBattleScene = false;

            recoveryStatusInfo = new List<RecoveryStatusInfo>();

            battleStartWord = "";
            if (showMessage)
                battleStartWord = monsters.Length <= 1 ? gameSettings.glossary.battle_start_single : gameSettings.glossary.battle_start;

            battleInfo = new BattleInfo() { monsters = monsters, playerLayouts = playerLayouts, settings = settings };
        }

        class BattleInfo
        {
            public BattleEnemyInfo[] monsters;
            public Vector3[] playerLayouts;
            public Rom.Map.BattleSetting settings;
        }

        private void LoadBattleSceneImpl()
        {
            var monsters = battleInfo.monsters;
            var playerLayouts = battleInfo.playerLayouts;
            var settings = battleInfo.settings;

            // このタイミングで同期処理で読み込めるのはWINDOWSだけ(Unityではフリーズする)
            // Only WINDOWS can be read by synchronous processing at this timing (freezes in Unity)
#if WINDOWS
            if (battleViewer is BattleViewer3D)
                ((BattleViewer3D)battleViewer).catalog = owner.catalog;
            battleViewer.SetBackGround(owner.mapScene.map.getBattleBg(catalog, settings));
#endif

            // バトルマップの中心点が初期化されてないと敵の配置がおかしくなるので、敵の初期化の前に設定しておく
            // If the center point of the battle map is not initialized, the placement of the enemy will be strange, so set it before initializing the enemy.
            if (battleViewer is BattleViewer3D)
            {
                // ほかに battleViewer の初期化が必要な時はここに追加する
                // Add any other initialization of battleViewer here
                ((BattleViewer3D)battleViewer).SetBackGroundCenter(settings.battleBgCenterX, settings.battleBgCenterZ);
            }

            playerViewData = new List<BattlePlayerData>();
            enemyMonsterViewData = new List<BattleEnemyData>();
            targetPlayerData = new List<BattleCharacterBase>();
            targetEnemyData = new List<BattleCharacterBase>();


            battlePlayerMax = gameSettings.BattlePlayerMax;
            battleEnemyMax = monsters.Length;


            // プレイヤーの設定
            // player settings
            playerData = new List<BattlePlayerData>();
            stockPlayerData = new List<BattlePlayerData>();

            for (int index = 0; index < party.PlayerCount; index++)
            {
                addPlayerData(party.GetPlayer(index)).directionRad = 180f / 180 * (float)Math.PI;
            }

            enemyData = new List<BattleEnemyData>();
            stockEnemyData = new List<BattleEnemyData>();

            monsterLayouts = new BattleEnemyInfo[monsters.Length];

            // 敵の設定
            // enemy settings
            for (int index = 0; index < monsters.Length; index++)
            {
                if (!monsters[index].IsLayoutValid())
                    addEnemyData(monsters[index].Id, null, -1, monsters[index].Level);
                else
                    addEnemyData(monsters[index].Id, monsters[index].Layout, -1, monsters[index].Level);

                monsterLayouts[index] = monsters[index];
            }

            battlePlayerMax = Math.Min(battlePlayerMax, stockPlayerData.Count);

            for (int i = 0; i < battlePlayerMax; i++)
            {
                var player = stockPlayerData[0];

                player.IsBattle = true;
                player.IsStock = false;

                if (playerLayouts == null || playerLayouts.Length <= i)
                {
                    player.SetPosition(BattleCharacterPosition.getPosition(BattleSequenceManagerBase.battleFieldCenter, BattleCharacterPosition.PosType.FRIEND, i, battlePlayerMax));
                }
                else
                {
                    player.SetPosition(BattleSequenceManagerBase.battleFieldCenter + playerLayouts[i]);
                }

                player.calcHeroLayout(playerData.Count);

                playerData.Add(player);
                stockPlayerData.Remove(player);
                targetPlayerData.Add(player);
            }

            battleEnemyMax = Math.Min(battleEnemyMax, stockEnemyData.Count);

            for (int i = 0; i < battleEnemyMax; i++)
            {
                var enemy = stockEnemyData[0];

                enemy.IsBattle = true;
                enemy.IsStock = false;

                if (!enemy.IsManualPosition)
                {
                    enemy.SetPosition(BattleCharacterPosition.getPosition(BattleSequenceManagerBase.battleFieldCenter, BattleCharacterPosition.PosType.ENEMY, i, battleEnemyMax));
                }

                enemyData.Add(enemy);
                stockEnemyData.Remove(enemy);
                targetEnemyData.Add(enemy);
            }

            playerViewData.AddRange(playerData);
            enemyMonsterViewData.AddRange(enemyData);

            var playerAllData = new List<BattlePlayerData>();
            var enemyMonsterAllData = new List<BattleEnemyData>();

            playerAllData.AddRange(playerViewData);
            playerAllData.AddRange(stockPlayerData);
            enemyMonsterAllData.AddRange(enemyMonsterViewData);
            enemyMonsterAllData.AddRange(stockEnemyData);


            // アイコン画像の読み込み
            // Loading icon images
            var iconGuidSet = new HashSet<Guid>();

            foreach (var player in party.Players)
            {
                foreach (var commandGuid in player.rom.battleCommandList)
                {
                    var command = catalog.getItemFromGuid(commandGuid) as BattleCommand;

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

            foreach (var item in party.Items)
            {
                iconGuidSet.Add(item.item.icon.guId);
            }

            iconGuidSet.Add(gameSettings.escapeIcon.guId);
            iconGuidSet.Add(gameSettings.returnIcon.guId);

            iconTable.Clear();

            foreach (var guid in iconGuidSet)
            {
                var icon = catalog.getItemFromGuid(guid) as Common.Resource.Texture;

                if (icon != null)
                {
                    Graphics.LoadImage(icon);

                    iconTable.Add(guid, icon);
                }
                else
                {
                    iconTable.Add(guid, null);
                }
            }

            commandSelectedMemberCount = 0;

            playerEscapeFailedCount = 0;

            battleViewer.BattleStart(playerAllData, enemyMonsterAllData);

            battleViewer.LoadResourceData(catalog, gameSettings);

            resultViewer.LoadResourceData(catalog);

            CheckBattleCharacterDown();

            var bg = catalog.getItemFromGuid(settings.battleBg) as Common.Resource.Texture;
            if (bg != null)
            {
                Graphics.LoadImage(bg);
                SetBackGroundImage(bg);
            }


            // バトルコモンの読み込み
            // Loading Battle Commons
            battleEvents = new BattleEventController();
            battleEvents.init(this, catalog, playerData, enemyData, owner.mapScene.mapEngine);
            battleEvents.playerLayouts = playerLayouts;
        }

        /// <summary>
        /// 状態での並び替え（射程使用のみ）
        /// Sort by state (Range use only)
        /// </summary>
        /// <param name="inIsPlayer">プレイヤーのパーティか？</param>
        /// <param name="inIsPlayer">party of players?</param>
        /// <returns>並びが変わったか？</returns>
        /// <returns>did the line change?</returns>
        bool SortTargetData(bool inIsPlayer)
        {
            if (!gameSettings.useBehindParty)
            {
                return false;
            }

            BattleCharacterBase[] list;

            if (gameSettings.usePositionBack)
            {
                list = inIsPlayer ? playerData.ToArray<BattleCharacterBase>() : enemyData.ToArray<BattleCharacterBase>();
            }
            else
            {
                list = inIsPlayer ? targetPlayerData.ToArray<BattleCharacterBase>() : targetEnemyData.ToArray<BattleCharacterBase>();
            }

            var targetList = new List<BattleCharacterBase>(list.Length);
            var behindList = new List<BattleCharacterBase>(list.Length);
            var deadList = new List<BattleCharacterBase>(list.Length);

            foreach (var item in list)
            {
                if (item.IsDeadCondition())
                {
                    deadList.Add(item);
                }
                else if (item.IsBehindPartyCondition())
                {
                    behindList.Add(item);
                }
                else
                {
                    targetList.Add(item);
                }
            }

            targetList.AddRange(behindList);
            targetList.AddRange(deadList);

            List<BattleCharacterBase> targetDataList = inIsPlayer ? targetPlayerData : targetEnemyData;

            for (int i = 0; i < targetList.Count; i++)
            {
                if (targetList[i] != targetDataList[i])
                {
                    targetDataList.Clear();
                    targetDataList.AddRange(targetList);

                    if (inIsPlayer)
                    {
                        playerViewData.Clear();

                        foreach (var item in targetList)
                        {
                            playerViewData.Add(item as BattlePlayerData);
                        }
                    }
                    else
                    {
                        enemyMonsterViewData.Clear();

                        foreach (var item in targetList)
                        {
                            enemyMonsterViewData.Add(item as BattleEnemyData);
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        void UpdatePlayerPosition()
        {
            if (!SortTargetData(true))
            {
                return;
            }

            MovePlayerPosition();
        }

        public void MovePlayerPosition()
        {
            var playerLayouts = battleEvents.playerLayouts;

            for (int i = 0; i < targetPlayerData.Count; i++)
            {
                var player = targetPlayerData[i] as BattlePlayerData;
                Vector3 position;

                if ((playerLayouts == null) || (playerLayouts.Length <= i))
                {
                    position = BattleCharacterPosition.getPosition(BattleSequenceManagerBase.battleFieldCenter, BattleCharacterPosition.PosType.FRIEND, i, battlePlayerMax);
                }
                else
                {
                    position = BattleSequenceManagerBase.battleFieldCenter + playerLayouts[i];
                }

                var actor = Viewer.searchFromActors(player);

                actor.walk(position.X, position.Z, true);

                player.calcHeroLayout(playerData.Count);
            }
        }

        void UpdateEnemyPosition()
        {
            if (!SortTargetData(false))
            {
                return;
            }

            for (int i = 0; i < targetEnemyData.Count; i++)
            {
                var enemy = targetEnemyData[i] as BattleEnemyData;
                Vector3 position;

                if (monsterLayouts[i].IsLayoutValid())
                {
                    position = monsterLayouts[i].Layout + BattleSequenceManagerBase.battleFieldCenter;
                }
                else
                {
                    position = BattleCharacterPosition.getPosition(BattleSequenceManagerBase.battleFieldCenter, BattleCharacterPosition.PosType.ENEMY, i, battleEnemyMax);
                }

                var actor = Viewer.searchFromActors(enemy);

                actor.walk(position.X, position.Z, true);
            }
        }

        void UpdatePosition()
        {
            UpdatePlayerPosition();
            UpdateEnemyPosition();
        }

        internal void addVisibleEnemy(BattleEnemyData data)
        {
            enemyMonsterViewData.Add(data);

            var battleEnemyMax = enemyMonsterViewData.Count;

            for (int i = 0; i < battleEnemyMax; i++)
            {
                var enemy = enemyMonsterViewData[i];

                enemy.IsBattle = true;
                enemy.IsStock = false;

                if (!enemy.IsManualPosition)
                    enemy.SetPosition(BattleCharacterPosition.getPosition(BattleSequenceManagerBase.battleFieldCenter, BattleCharacterPosition.PosType.ENEMY, i, battleEnemyMax));
            }
        }

        internal void removeVisibleEnemy(BattleEnemyData data)
        {
            enemyMonsterViewData.Remove(data);
        }

        internal void addVisiblePlayer(BattlePlayerData data)
        {
            playerViewData.Add(data);

            var battlePlayerMax = playerViewData.Count;

            for (int i = 0; i < battlePlayerMax; i++)
            {
                var player = playerViewData[i];

                player.IsBattle = true;
                player.IsStock = false;

                player.SetPosition(BattleCharacterPosition.getPosition(BattleSequenceManagerBase.battleFieldCenter, BattleCharacterPosition.PosType.FRIEND, i, battlePlayerMax));
                player.directionRad = 180f / 180 * (float)Math.PI;


                player.calcHeroLayout(playerData.Count);
            }
        }

        internal void removeVisiblePlayer(BattlePlayerData data)
        {
            playerViewData.Remove(data);

            var battlePlayerMax = playerViewData.Count;

            for (int i = 0; i < battlePlayerMax; i++)
            {
                var player = playerViewData[i];

                player.IsBattle = true;
                player.IsStock = false;

                player.SetPosition(BattleCharacterPosition.getPosition(BattleSequenceManagerBase.battleFieldCenter, BattleCharacterPosition.PosType.FRIEND, i, battlePlayerMax));
                player.directionRad = 180f / 180 * (float)Math.PI;


                player.calcHeroLayout(playerData.Count);
            }
        }

        public BattleEnemyData createEnemyData(Guid guid, Vector3? layout = null, int level = -1)
        {
            var data = new ExBattleEnemyData();

            if (layout != null)
            {
                data.pos =
                data.moveTargetPos = layout.Value + BattleSequenceManagerBase.battleFieldCenter;
                data.arrangmentType = BattleEnemyData.MonsterArrangementType.Manual;
            }

            var monster = catalog.getItemFromGuid<Rom.Cast>(guid);
            var monsterRes = catalog.getItemFromGuid(owner.IsBattle2D ? monster.graphic : monster.Graphics3D) as Common.Resource.GfxResourceBase;

            Common.Resource.Texture tex = null;

            if (monsterRes != null)
            {
                var r = monsterRes.gfxResourceId.getResource() as Common.Resource.SliceAnimationSet;

                if (r != null && r.items.Count > 0)
                {
                    tex = r.items[0].texture.getResource() as Common.Resource.Texture;
                }
            }

            data.monster = monster;
            data.EscapeSuccessBasePercent = 100;
            data.EscapeSuccessMessage = string.Format(gameSettings.glossary.battle_enemy_escape, monster.name);
            data.ExecuteCommandTurnCount = 1;
            data.image = monsterRes;
            data.imageId = tex;
            data.imageAlpha = 1.0f;

            data.IsBattle = true;
            data.IsStock = true;

            data.FriendPartyRefMember = targetEnemyData;
            data.EnemyPartyRefMember = targetPlayerData;

            data.battleStatusData = new BattleStatusWindowDrawer.StatusData();
            data.startStatusData = new BattleStatusWindowDrawer.StatusData();
            data.nextStatusData = new BattleStatusWindowDrawer.StatusData();

            if (level == -1)
            {
                //data.SetParameters(monster);
                var cast = Party.createHeroFromRom(catalog, owner.data.party, monster, 1, true);
                data.SetParameters(cast, party.getHeroName(monster.guId));
            }
            else
            {
                var cast = Party.createHeroFromRom(catalog, owner.data.party, monster, level, true);
                data.SetParameters(cast, party.getHeroName(monster.guId));
            }

            data.battleStatusData.statusValue.InitializeStatus(data.baseStatusValue);
            data.battleStatusData.consumptionStatusValue.InitializeStatus(data.consumptionStatusValue);
            data.startStatusData.statusValue.InitializeStatus(data.baseStatusValue);
            data.startStatusData.consumptionStatusValue.InitializeStatus(data.consumptionStatusValue);
            data.nextStatusData.statusValue.InitializeStatus(data.baseStatusValue);
            data.nextStatusData.consumptionStatusValue.InitializeStatus(data.consumptionStatusValue);

            data.battleStatusData.HitPoint = data.startStatusData.HitPoint = data.nextStatusData.HitPoint = data.HitPoint;
            data.battleStatusData.MagicPoint = data.startStatusData.MagicPoint = data.nextStatusData.MagicPoint = data.MagicPoint;

            return data;
        }

        public BattleEnemyData addEnemyData(Guid guid, Vector3? layout = null, int index = -1, int level = -1)
        {
            var data = createEnemyData(guid, layout, level);

            stockEnemyData.Add(data);

            if (index < 0)
            {
                data.UniqueID = enemyData.Count + stockEnemyData.Count;
            }
            else
            {
                // まずは探して解放する
                // Find and release first
                var old = enemyData.FirstOrDefault(x => x.UniqueID == index);
                if (old != null)
                {
                    disposeEnemy(old);
                    enemyData.Remove(old);
                    targetEnemyData.Remove(old);
                }

                data.UniqueID = index;
            }

            return data;
        }


        public BattlePlayerData createPlayerData(Hero hero)
        {
            var data = new ExBattlePlayerData();

            var face = catalog.getItemFromGuid(hero.rom.face) as Resource.SliceAnimationSet;

            data.setFaceImage(face);

            data.player = hero;
            data.ExecuteCommandTurnCount = 1;

            data.EscapeSuccessBasePercent = 0;
            data.EscapeSuccessMessage = gameSettings.glossary.battle_escape;

            data.conditionInfoDic = new Dictionary<AttackAttributeType, Hero.ConditionInfo>(data.player.conditionInfoDic);
            data.battleStatusData = new BattleStatusWindowDrawer.StatusData();
            data.startStatusData = new BattleStatusWindowDrawer.StatusData();
            data.nextStatusData = new BattleStatusWindowDrawer.StatusData();

            data.SetParameters(hero, owner.debugSettings.battleHpAndMpMax, owner.debugSettings.battleStatusMax, party);

            data.startStatusData.statusValue.InitializeStatus(data.baseStatusValue);
            data.startStatusData.consumptionStatusValue.InitializeStatus(data.consumptionStatusValue);
            data.nextStatusData.statusValue.InitializeStatus(data.baseStatusValue);
            data.nextStatusData.consumptionStatusValue.InitializeStatus(data.consumptionStatusValue);

            data.startStatusData.HitPoint = data.nextStatusData.HitPoint = data.HitPoint;
            data.startStatusData.MagicPoint = data.nextStatusData.MagicPoint = data.MagicPoint;

            data.IsBattle = true;
            data.IsStock = true;

            data.FriendPartyRefMember = targetPlayerData;
            data.EnemyPartyRefMember = targetEnemyData;

            var layouts = catalog.getLayoutProperties();

            foreach (var id in hero.rom.battleCommandList)
			{
                var command = catalog.getItemFromGuid(id) as BattleCommand;

				switch (command.type)
				{
					case BattleCommand.CommandType.SKILLMENU:
                    case BattleCommand.CommandType.ITEMMENU:
						if ((command.refGuid != Guid.Empty) && (!layoutDic.ContainsKey(command.refGuid)))
						{
                            var usage = (command.type == BattleCommand.CommandType.SKILLMENU) ? Rom.LayoutProperties.LayoutNode.UsageInGame.BattleSkill : Rom.LayoutProperties.LayoutNode.UsageInGame.BattleItem;
                            var layout = owner.data.system.GetLayout(layouts, usage, command.refGuid);

							if (layout != null)
							{
                                var result = new BattleViewer3D.BattleUI.LayoutDrawerController();

                                result.LoadLayout(owner, catalog, layout, usage);
                                result.setBattleSequenceManager(this);

                                layoutDic.Add(command.refGuid, result);
                            }
                        }
                        break;
					default:
						break;
				}
            }

            return data;
        }

        public BattlePlayerData addPlayerData(Hero hero)
        {
            var data = createPlayerData(hero);

            data.UniqueID = idNumber;

            stockPlayerData.Add(data);

            idNumber++;

            return data;
        }

        public override void ReleaseImageData()
        {
            EffectPreloadJob.clearPreloads();
            battleEvents?.term();

            foreach (var player in playerData)
            {
                disposePlayer(player);
            }

            if (stockPlayerData != null)
            {
                foreach (var player in stockPlayerData)
                {
                    disposePlayer(player);
                }
            }



            foreach (var enemyMonster in enemyData)
            {
                disposeEnemy(enemyMonster);
            }

            if (stockEnemyData != null)
            {
                foreach (var enemyMonster in stockEnemyData)
                {
                    disposeEnemy(enemyMonster);
                }
            }



            foreach (var iconImageId in iconTable.Values)
            {
                Graphics.UnloadImage(iconImageId);
            }

            if (backGroundImageId != null)
            {
                Graphics.UnloadImage(backGroundImageId);
                backGroundImageId = null;//#23959 念のため初期化
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
            if (player == null)
            {
                return;
            }

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

            player.mapChr?.removeAllEffectDrawer();
        }

        private void disposeEnemy(BattleEnemyData enemyMonster)
        {
            if (enemyMonster == null)
            {
                return;
            }

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

            enemyMonster.mapChr?.removeAllEffectDrawer();
        }

        public override void ApplyDebugSetting()
        {
            foreach (var player in playerData)
            {
                if (player.IsDeadCondition())
                {
                    player.conditionInfoDic.Clear();

                    player.ChangeEmotion(Resource.Face.FaceType.FACE_NORMAL);
                }

                player.SetParameters(player.player, owner.debugSettings.battleHpAndMpMax, owner.debugSettings.battleStatusMax, party);

                SetBattleStatusData(player);
            }
        }

        public override int CalcAttackWithWeaponDamage(BattleCharacterBase attacker, BattleCharacterBase target, AttackAttributeType attackAttribute, bool isCritical, Random battleRandom)
        {
            float weaponDamage = (attacker.Attack) / 2.5f - (target.Defense) / ((isCritical) ? 8.0f : 4.0f);

            float elementDamage = 0;

#if true
            elementDamage = attacker.ElementAttack * target.ResistanceAttackAttributePercent(attackAttribute);
#else
            switch (attackAttribute)
            {
                case AttackAttributeType.None:
                case AttackAttributeType.A:
                case AttackAttributeType.B:
                case AttackAttributeType.C:
                case AttackAttributeType.D:
                case AttackAttributeType.E:
                case AttackAttributeType.F:
                case AttackAttributeType.G:
                case AttackAttributeType.H:
                    elementDamage = attacker.ElementAttack * target.ResistanceAttackAttributePercent((int)attackAttribute);
                    break;
            }
#endif

            if (weaponDamage < 0)
                weaponDamage = 0;

            float totalDamage = (weaponDamage + elementDamage) * (1.0f - (float)battleRandom.NextDouble() / 10);

            // 武器がある？
            // Do you have a weapon?
            Rom.NItem weapon = null;
            Rom.Cast cast = null;
            if (attacker is BattlePlayerData pl)
            {
                cast = pl.player.rom;
                weapon = pl.player.equipments[0];
            }
            else if (attacker is BattleEnemyData enm)
            {
                cast = enm.monsterGameData.rom;
                weapon = enm.monsterGameData.equipments[0];
            }
            else if(attacker is MapCharacterBattleStatus mst)
            {
                cast = mst.source;
                weapon = catalog.getItemFromGuid<Rom.NItem>(mst.source.equipWeapon);
                if (mst.hero != null)
                    weapon = mst.hero.equipments[0];
            }

            // 式がある？
            // do you have a formula?
            string formula = null;
            if (cast != null && !string.IsNullOrEmpty(cast.formula))
            {
                // 素手の計算式
                // Bare hand calculation formula
                formula = cast.formula;
            }
            if (weapon != null)
            {
                // 武器が存在するので素手の計算式は使わない
                // Since there are weapons, we do not use the formula for calculating with bare hands.
                formula = null;
                if (!string.IsNullOrEmpty(weapon.weapon.formula))
                {
                    // 武器の計算式
                    // Weapon formula
                    formula = weapon.weapon.formula;
                }
            }

            if (formula != null)
            {
#if true
                totalDamage = EvalFormula(formula, attacker, target, attackAttribute, battleRandom);
#else
                totalDamage = EvalFormula(formula, attacker, target, (int)attackAttribute, battleRandom);
#endif
            }

            int damage = (int)(totalDamage * target.DamageRate * (isCritical ? 1.5f : 1.0f));

            if (damage < -attacker.MaxDamage) damage = -attacker.MaxDamage;
            if (damage > attacker.MaxDamage) damage = attacker.MaxDamage;

            return damage;
        }

        private int CalcAttackWithWeaponDamage(BattleCharacterBase attacker, BattleCharacterBase target, AttackAttributeType attackAttribute, bool isCritical, List<BattleDamageTextInfo> textInfo)
        {
            var damage = CalcAttackWithWeaponDamage(attacker, target, attackAttribute, isCritical, battleRandom);

            BattleDamageTextInfo.TextType textType = BattleDamageTextInfo.TextType.HitPointDamage;
            Guid statusId = Guid.Empty;

            if (damage < 0)
            {
                textType = BattleDamageTextInfo.TextType.Heal;
                statusId = catalog.getGameSettings().maxHPStatusID;
            }
            else if (isCritical)
            {
                textType = BattleDamageTextInfo.TextType.CriticalDamage;
            }

            AddBattleDamageTextInfo(textInfo, textType, target, Math.Abs(damage).ToString(), statusId);

            return damage;
        }

        public float EvalFormula(string formula, BattleCharacterBase attacker, BattleCharacterBase target, AttackAttributeType attackAttribute, BattleSequenceManager battle)
        {
            return EvalFormula(formula, attacker, target, attackAttribute, battle.battleRandom);
        }

#if true
        private static float EvalFormula(string formula, BattleCharacterBase attacker, BattleCharacterBase target, AttackAttributeType attackAttribute, Random battleRandom)
#else
        private static float EvalFormula(string formula, BattleCharacterBase attacker, BattleCharacterBase target, int attackAttribute, Random battleRandom)
#endif
        {
            GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, "Attack Formula", formula);

            // 式をパースして部品に分解する
            // Parse an expression and break it down into parts
            var words = Util.ParseFormula(formula);

            // 逆ポーランド記法に並べ替える
            // Sort to Reverse Polish Notation
            words = Util.SortToRPN(words);

            return CalcRPN(words, attacker, target, attackAttribute, battleRandom);
        }

        internal static float GetRandom(Random battleRandom, float max, float min)
        {
            if (min > max)
            {
                var tmp = min;
                min = max;
                max = tmp;
            }

            return (float)battleRandom.NextDouble() * (max - min) + min;
        }

#if true
        public static float CalcRPN(List<string> words, BattleCharacterBase attacker, BattleCharacterBase target, AttackAttributeType attackAttribute, Random battleRandom)
#else
        public static float CalcRPN(List<string> words, BattleCharacterBase attacker, BattleCharacterBase target, int attackAttribute, Random battleRandom)
#endif
        {
            var stack = new Stack<float>();
            stack.Push(0);

            float a, b;
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
                        a = stack.Pop();
                        b = stack.Pop();
                        stack.Push(GetRandom(battleRandom, Math.Max(a, b), Math.Min(a, b)));
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
                        // Put numbers and variables on the stack

                        float num;
                        if (float.TryParse(word, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out num))
                        {
                            // 数値
                            // numerical value
                            stack.Push(num);
                        }
                        else
                        {
                            // 変数
                            // variable
                            stack.Push(parseBattleNum(word, attacker, target, attackAttribute));

                            GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, "Attack Formula",
                                string.Format("Parse Result / {0} : {1}", word, stack.Peek()));
                        }
                        break;
                }
            }

            return stack.Pop();
        }

#if true
        private static float parseBattleNum(string word, BattleCharacterBase attacker, BattleCharacterBase target, AttackAttributeType attackAttribute)
#else
        private static float parseBattleNum(string word, BattleCharacterBase attacker, BattleCharacterBase target, int attackAttribute)
#endif
        {
            BattleCharacterBase src = null;
            if (word.StartsWith("a."))
                src = attacker;
            else if (word.StartsWith("b."))
                src = target;

            if (src != null)
            {
                word = word.Substring(2, word.Length - 2);

                switch (word)
                {
                    case "lv":
                        if (src is BattlePlayerData)
                            return ((BattlePlayerData)src).player.level;
                        else if (src is BattleEnemyData)
                            return ((BattleEnemyData)src).monsterGameData.level;
                        else
                            return 1;
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
                        return target.ResistanceAttackAttributePercent(attackAttribute);
                    default:
                        return src.GetStatus(Common.Catalog.sInstance.getGameSettings(), word);
                }
            }
            else if(word.StartsWith("\\$["))
            {
                var valName = word.Substring(3, word.Length - 4);
                return (float)GameMain.instance.data.system.GetVariable(valName);
            }

            return 0;
        }

        private void AddBattleDamageTextInfo(List<BattleDamageTextInfo> inTextInfo, BattleDamageTextInfo.TextType inTextType, BattleCharacterBase inTarget, string inText, System.Guid inStatusId)
        {
            var color = Color.White;

            if (inStatusId != Guid.Empty)
            {
                var damage = true;

                switch (inTextType)
                {
                    case BattleDamageTextInfo.TextType.HitPointHeal:
                    case BattleDamageTextInfo.TextType.MagicPointHeal:
                    case BattleDamageTextInfo.TextType.Heal:
                        damage = false;
                        break;
                    default:
                        break;
                }

                color = catalog.getGameSettings().GetStatusColor(inStatusId, damage);
            }
            else if (inTextType == BattleDamageTextInfo.TextType.CriticalDamage)
            {
                color = catalog.getGameSettings().GetCriticalColor();
            }

            if (color.A != 0x00)
            {
                inTextInfo.Add(new BattleDamageTextInfo(inTextType, inTarget, inText, inStatusId, color));
            }
        }

        private void AddBattleMissTextInfo(List<BattleDamageTextInfo> inTextInfo, BattleCharacterBase inTarget)
        {
            inTextInfo.Add(new BattleDamageTextInfo(BattleDamageTextInfo.TextType.Miss, inTarget, gameSettings.glossary.battle_miss));
        }

        private bool Heal(BattleCharacterBase target, Rom.EffectParamSettings effectParamSettings, bool canGuard, Dictionary<Guid, string> formulaDic, List<BattleDamageTextInfo> textInfo, Guid attribute, BattleCharacterBase effecter = null, Dictionary<Guid, int> totalHealValueDic = null, Dictionary<Guid, Rom.DamageDrainEffectParam> drainPercentDic = null)
        {
            var isEffect = false;
            var useSkill = effecter != null;
            var effectValueDic = new Dictionary<Guid, int>();

            foreach (var effectParam in effectParamSettings.GetConsumptionStatusValueChangeEffectParamList())
            {
                if (effectParam.ChangeParam == 0)
                {
                    continue;
                }

                var info = gameSettings.GetCastStatusParamInfo(effectParam.Id);

                if (info == null)
                {
                    continue;
                }

                var value = 0;

                switch (effectParam.ChangeType)
                {
                    case Util.StatusChangeType.Unknwon:
                        break;
                    case Util.StatusChangeType.Direct:
                        value = effectParam.ChangeParam;
                        break;
                    case Util.StatusChangeType.Percent:
                        {
                            var baseStatus = (useSkill && (effectParam.BaseStatusId != info.guId)) ? effecter : target;

                            value = baseStatus.GetSystemStatus(gameSettings, effectParam.BaseStatusId) * effectParam.ChangeParam / 100;
                        }
                        break;
                    default:
                        break;
                }

                if (!effectValueDic.ContainsKey(info.guId))
                {
                    effectValueDic[info.guId] = 0;
                }

                effectValueDic[info.guId] += value;
            }

            foreach (var key in effectValueDic.Keys.ToArray())
            {
                var effectValue = effectValueDic[key];

                if (effectValue >= 0)
                {
                    continue;
                }

                var info = gameSettings.GetCastStatusParamInfo(key);

                if (info.Consumption && (effecter != null))
                {
                    // ダメージなので属性耐性を考慮
                    // Since it is damage, consider attribute resistance.
                    var effectValueTmp = effectValue * target.ResistanceAttackAttributePercent(attribute);

                    if (canGuard && (info.guId == gameSettings.maxHPStatusID))
                    {
                        effectValueTmp *= target.DamageRate;
                    }

                    effectValueDic[info.guId] = (int)effectValueTmp;
                }
            }

            foreach (var item in formulaDic)
            {
                var info = gameSettings.GetCastStatusParamInfo(item.Key);

                if (info == null)
                {
                    continue;
                }

                if (!effectValueDic.ContainsKey(info.guId))
                {
                    effectValueDic[info.guId] = 0;
                }

                effectValueDic[info.guId] += (int)EvalFormula(item.Value, effecter, target, attribute, battleRandom);
            }

            var minHeal = Hero.MIN_STATUS;
            var maxHeal = Hero.MAX_STATUS;

			if (useSkill)
			{
                maxHeal = effecter.MaxDamage;
                minHeal = -maxHeal;
            }

            foreach (var item in effectValueDic)
            {
                var info = gameSettings.GetCastStatusParamInfo(item.Key);

                if (info == null)
                {
                    continue;
                }

                var effectValue = Math.Max(Math.Min(item.Value, maxHeal), minHeal);

                target.consumptionStatusValue.AddStatus(item.Key, effectValue);

                BattleDamageTextInfo.TextType textType;
                var heal = effectValue > 0;

                if (useSkill)
                {
                    // ドレイン用加算
                    // Addition for drain
                    if (!heal && drainPercentDic.ContainsKey(item.Key))
                    {
                        effectValue = -Math.Min(-effectValue, target.consumptionStatusValue.GetStatus(item.Key));
                    }

                    if (!totalHealValueDic.ContainsKey(item.Key))
                    {
                        totalHealValueDic[item.Key] = 0;
                    }

                    totalHealValueDic[item.Key] += effectValue;
                }

                switch (info.Key)
                {
                    case Rom.GameSettings.MaxHPStatusKey:
                        target.HitPoint += effectValue;

                        if ((target.HitPoint > 0) && target.IsDeadCondition())
                        {
                            target.Resurrection();
                        }

                        target.ConsistancyHPPercentConditions(catalog, battleEvents);

                        if (!heal && useSkill)
                        {
                            SetCounterAction(target, effecter);
                        }
                        break;
                    case Rom.GameSettings.MaxMPStatusKey:
                        target.MagicPoint += effectValue;
                        break;
                    default:
                        break;
                }

                if (heal)
                {
                    textType = BattleDamageTextInfo.TextType.Heal;
                }
                else
                {
                    textType = BattleDamageTextInfo.TextType.Damage;
                    effectValue = Math.Abs(effectValue);
                }

                AddBattleDamageTextInfo(textInfo, textType, target, effectValue.ToString(), item.Key);

                isEffect = true;
            }

            return isEffect;
        }

        private void EffectSkill(BattleCharacterBase effecter, Rom.NSkill skill, BattleCharacterBase[] friendEffectTargets, BattleCharacterBase[] enemyEffectTargets, List<BattleDamageTextInfo> textInfo, List<RecoveryStatusInfo> recoveryStatusInfo, out BattleCharacterBase[] friend, out BattleCharacterBase[] enemy)
        {
            var gs = catalog.getGameSettings();
            var friendEffect = skill.friendEffect;
            var enemyEffect = skill.enemyEffect;
            var option = skill.option;
            int totalHitPointDamage = 0;
            int totalMagicPointDamage = 0;
            var friendTotalSkillValueDic = new Dictionary<Guid, int>();
            var friendSkillFormulaDic = new Dictionary<Guid, string>();
            var friendSkillAttribute = Guid.Empty;
            var friendSkillDrainPercentDic = new Dictionary<Guid, Rom.DamageDrainEffectParam>();
            var enemyTotalSkillDamageDic = new Dictionary<Guid, int>();
            var enemySkillFormulaDic = new Dictionary<Guid, string>();
            var enemySkillAttribute = Guid.Empty;
            var enemySkillDrainPercentDic = new Dictionary<Guid, Rom.DamageDrainEffectParam>();

            if (friendEffect != null)
            {
                foreach (var item in friendEffect.EffectParamSettings.GetEtcEffectParamList())
                {
                    switch (item.Type)
                    {
                        case Rom.EffectParamBase.EffectType.ConsumptionValueFormula:
                            if (item is Rom.ConsumptionValueFormulaEffectParam formula && !string.IsNullOrEmpty(formula.Formula))
                            {
                                friendSkillFormulaDic[formula.TargetId] = formula.Formula;
                            }
                            break;
                        case Rom.EffectParamBase.EffectType.SkillAttribute:
                            if (item is Rom.SkillAttributeEffectParam damageAttribute)
                            {
                                friendSkillAttribute = damageAttribute.Id;
                            }
                            break;
                        case Rom.EffectParamBase.EffectType.DamageDrain:
                            if (item is Rom.DamageDrainEffectParam damageDrain && (damageDrain.Percent != 0))
                            {
                                friendSkillDrainPercentDic.Add(damageDrain.TargetId, damageDrain);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            if (enemyEffect != null)
            {
                foreach (var item in enemyEffect.EffectParamSettings.GetEtcEffectParamList())
                {
                    switch (item.Type)
                    {
                        case Rom.EffectParamBase.EffectType.ConsumptionValueFormula:
                            if (item is Rom.ConsumptionValueFormulaEffectParam damageFormula && !string.IsNullOrEmpty(damageFormula.Formula))
                            {
                                enemySkillFormulaDic[damageFormula.TargetId] = damageFormula.Formula;
                            }
                            break;
                        case Rom.EffectParamBase.EffectType.SkillAttribute:
                            if (item is Rom.SkillAttributeEffectParam damageAttribute)
                            {
                                enemySkillAttribute = damageAttribute.Id;
                            }
                            break;
                        case Rom.EffectParamBase.EffectType.DamageDrain:
                            if ((item is Rom.DamageDrainEffectParam damageDrain) && (damageDrain.Percent != 0))
                            {
                                enemySkillDrainPercentDic.Add(damageDrain.TargetId, damageDrain);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            var friendEffectCharacters = new List<BattleCharacterBase>();
            var enemyEffectCharacters = new List<BattleCharacterBase>();

            foreach (var target in friendEffectTargets)
            {
                bool isEffect = false;

                // 戦闘不能状態ならばHPとMPを回復させない (スキル効果に「戦闘不能状態を回復」がある場合のみ回復効果を有効とする)
                // Does not recover HP and MP if incapacitated (Recovery effect is valid only if the skill effect has \
                // |------------------------====----------------|--------------|----------------|
                // | ↓スキル効果              効果対象の状態→ | 戦闘不能状態 | それ以外の状態 |
                // | ↓Skill effect Target status → | Incapacitated state | Other states |
                // |--------------------------------------------|--------------|----------------|
                // | 「戦闘不能者のみ有効」あり「即死回復」あり |     有効     |      無効      |
                // | \
                // | 「戦闘不能者のみ有効」あり「即死回復」なし |     無効     |      有効      |
                // | \
                // | 「戦闘不能者のみ有効」なし「即死回復」あり |     有効     |      有効      |
                // | \
                // | 「戦闘不能者のみ有効」なし「即死回復」なし |     無効     |      有効      |
                // | \
                // |--------------------------------------------|--------------|----------------|
                bool isHealParameter = false;

                // 即死回復かつ即死状態だったら有効
                // Effective if instant death recovery and instant death state
                if (friendEffect.HasDeadCondition(catalog) && target.IsDeadCondition())
                    isHealParameter = true;

                // 即死回復なし で 生存状態であれば有効
                // Effective if there is no instant death recovery and you are alive
                if (!friendEffect.HasDeadCondition(catalog) && !target.IsDeadCondition())
                    isHealParameter = true;

                // 即死回復ありでも、戦闘不能者のみ有効がオフなら全員に効果あり
                // Even if there is instant death recovery, it is effective for everyone if only effective for incapacitated is off
                if (friendEffect.HasDeadCondition(catalog) && !skill.option.onlyForDown)
                    isHealParameter = true;

                if (friendEffect.EffectParamSettings.UseOldEffectParam)
                {
                    // HitPoint 回復 or ダメージ
                    // HitPoint recovery or damage
                    if ((friendEffect.hitpoint != 0 || friendEffect.hitpointPercent != 0 ||
                        friendEffect.hitpoint_powerPercent != 0 || friendEffect.hitpoint_magicPercent != 0 ||
                        !string.IsNullOrEmpty(friendEffect.hitpointFormula)) && isHealParameter)
                    {
                        int effectValue = friendEffect.hitpoint + (int)(friendEffect.hitpointPercent / 100.0f * target.MaxHitPoint) + (int)(friendEffect.hitpoint_powerPercent / 100.0f * effecter.Attack) + (int)(friendEffect.hitpoint_magicPercent / 100.0f * effecter.Magic);

                        if (effectValue > effecter.MaxDamage)
                            effectValue = effecter.MaxDamage;
                        if (effectValue < -effecter.MaxDamage)
                            effectValue = -effecter.MaxDamage;

                        if (effectValue >= 0)
                        {
                            // 回復効果の場合は属性耐性の計算を行わない
                            // Attribute resistance is not calculated for recovery effects
                        }
                        else
                        {
#if true
                            switch (target.AttackAttributeTolerance(friendEffect.AttributeGuid))
#else
                        switch (target.AttackAttributeTolerance(friendEffect.attribute))
#endif
                            {
                                case AttributeToleranceType.Normal:
                                case AttributeToleranceType.Strong:
                                case AttributeToleranceType.Weak:
                                    {
#if true
                                        float effectValueTmp = effectValue * target.ResistanceAttackAttributePercent(skill.friendEffect.AttributeGuid) * target.DamageRate;
#else
                                    float effectValueTmp = effectValue * target.ResistanceAttackAttributePercent(skill.friendEffect.attribute) * target.DamageRate;
#endif
                                        if (effectValueTmp > effecter.MaxDamage)
                                            effectValueTmp = effecter.MaxDamage;
                                        if (effectValueTmp < -effecter.MaxDamage)
                                            effectValueTmp = -effecter.MaxDamage;
                                        effectValue = (int)effectValueTmp;
                                    }
                                    break;

                                case AttributeToleranceType.Absorb:
                                    {
#if true
                                        float effectValueTmp = effectValue * target.ResistanceAttackAttributePercent(skill.friendEffect.AttributeGuid) * target.DamageRate;
#else
                                    float effectValueTmp = effectValue * target.ResistanceAttackAttributePercent(skill.friendEffect.attribute) * target.DamageRate;
#endif
                                        if (effectValueTmp > effecter.MaxDamage)
                                            effectValueTmp = effecter.MaxDamage;
                                        if (effectValueTmp < -effecter.MaxDamage)
                                            effectValueTmp = -effecter.MaxDamage;
                                        effectValue = (int)effectValueTmp;
                                    }
                                    break;

                                case AttributeToleranceType.Invalid:
                                    effectValue = 0;
                                    break;
                            }

                        }

                        // 式がある？
                        // do you have a formula?
                        if (!string.IsNullOrEmpty(friendEffect.hitpointFormula))
                        {
#if true
                            effectValue += (int)EvalFormula(friendEffect.hitpointFormula, effecter, target, friendEffect.AttributeGuid, battleRandom);
#else
                        effectValue += (int)EvalFormula(friendEffect.hitpointFormula, effecter, target, friendEffect.attribute, battleRandom);
#endif
                            if (effectValue > effecter.MaxDamage)
                                effectValue = effecter.MaxDamage;
                            if (effectValue < -effecter.MaxDamage)
                                effectValue = -effecter.MaxDamage;
                        }

                        target.HitPoint += effectValue;
                        target.consumptionStatusValue.AddStatus(gs.maxHPStatusID, effectValue);

                        if (target.HitPoint > 0)
                        {
                            if (target.IsDeadCondition())
                            {
                                target.Resurrection();
                            }
                        }

                        target.ConsistancyHPPercentConditions(catalog, battleEvents);

                        BattleDamageTextInfo.TextType textType;
                        var heal = effectValue > 0;

                        if (heal)
                        {
                            textType = BattleDamageTextInfo.TextType.Heal;
                        }
                        else
                        {
                            textType = BattleDamageTextInfo.TextType.Damage;
                            effectValue = Math.Abs(effectValue);
                            totalHitPointDamage += effectValue;
                            SetCounterAction(target, effecter);
                        }

                        AddBattleDamageTextInfo(textInfo, textType, target, effectValue.ToString(), gs.maxHPStatusID);

                        isEffect = true;
                    }

                    // MagicPoint 回復
                    // MagicPoint Recovery
                    if ((friendEffect.magicpoint != 0 || friendEffect.magicpointPercent != 0) && isHealParameter)
                    {
                        int effectValue = ((friendEffect.magicpoint) + (int)(friendEffect.magicpointPercent / 100.0f * target.MaxMagicPoint));

                        if (effectValue > effecter.MaxDamage)
                            effectValue = effecter.MaxDamage;
                        if (effectValue < -effecter.MaxDamage)
                            effectValue = -effecter.MaxDamage;

                        target.MagicPoint += effectValue;

                        var info = gameSettings.GetCastStatusParamInfo(gs.maxMPStatusID, true);

                        if (info != null)
                        {
                            target.consumptionStatusValue.AddStatus(info.guId, effectValue);
                        }

                        BattleDamageTextInfo.TextType textType;
                        var heal = effectValue > 0;

                        if (heal)
                        {
                            textType = BattleDamageTextInfo.TextType.MagicPointHeal;
                        }
                        else
                        {
                            textType = BattleDamageTextInfo.TextType.MagicPointDamage;
                            effectValue = Math.Abs(effectValue);
                            totalMagicPointDamage += effectValue;
                        }

                        AddBattleDamageTextInfo(textInfo, textType, target, effectValue.ToString(), gs.maxMPStatusID);

                        isEffect = true;
                    }
                }
                else if (isHealParameter)
                {
                    Heal(target, friendEffect.EffectParamSettings, skill.CanGuard, friendSkillFormulaDic, textInfo, friendSkillAttribute, effecter, friendTotalSkillValueDic, friendSkillDrainPercentDic);

                    isEffect = true;
                }

                // 状態異常回復
                // status ailment recovery
                conditionRecoveryImpl(friendEffect.RecoveryList, target, ref isEffect);
                bool isDisplayMiss = false;
                conditionAssignImpl(friendEffect.AssignList, target, ref isEffect, ref isDisplayMiss);
                if (isDisplayMiss)
                {
                    // 状態異常付与に失敗した時missと出す場合はコメントアウトを外す
                    // Remove the comment out if you want to output a miss when you fail to apply the status ailment
                    //AddBattleMissTextInfo(textInfo, target);
                }

				// パラメータ変動
				// Parameter variation
				if (target.enhanceStatusValue.InitializeEnhancementEffectStatus(target.baseStatusValue.GetCalcEffectStatuses(friendEffect.EffectParamSettings), true))
				{
                    isEffect = true;
                }

                var enhancement = target.enhanceStatusValue.GetSystemStatus(gameSettings, gs.maxHPStatusID);

                if (enhancement != 0 && Math.Abs(enhancement) >= Math.Abs(target.MaxHitPointEnhance))      // 腕力 / strength
                {
                    target.MaxHitPointEnhance = enhancement;

                    isEffect = true;
                }

                enhancement = target.enhanceStatusValue.GetSystemStatus(gameSettings, gs.maxMPStatusID);

                if (enhancement != 0 && Math.Abs(enhancement) >= Math.Abs(target.MaxMagicPointEnhance))      // 腕力 / strength
                {
                    target.MaxMagicPointEnhance = enhancement;

                    isEffect = true;
                }

                enhancement = Util.CalcStatusChangeValue(target.PowerBase, friendEffect.power, friendEffect.powerStatusChangeType);

                if (enhancement != 0 && Math.Abs(enhancement) >= Math.Abs(target.PowerEnhancement))      // 腕力 / strength
                {
                    target.PowerEnhancement = enhancement;

                    isEffect = true;
                }

                enhancement = Util.CalcStatusChangeValue(target.VitalityBase, friendEffect.vitality, friendEffect.vitalityStatusChangeType);

                if (enhancement != 0 && Math.Abs(enhancement) >= Math.Abs(target.VitalityEnhancement))// 体力 / physical strength
                {
                    target.VitalityEnhancement = enhancement;

                    isEffect = true;
                }

                enhancement = Util.CalcStatusChangeValue(target.MagicBase, friendEffect.magic, friendEffect.magicStatusChangeType);

                if (enhancement != 0 && Math.Abs(enhancement) >= Math.Abs(target.MagicEnhancement))      // 魔力 / magical power
                {
                    target.MagicEnhancement = enhancement;

                    isEffect = true;
                }

                enhancement = Util.CalcStatusChangeValue(target.SpeedBase, friendEffect.speed, friendEffect.speedStatusChangeType);

                if (enhancement != 0 && Math.Abs(enhancement) >= Math.Abs(target.SpeedEnhancement))       // 素早さ / Agility
                {
                    target.SpeedEnhancement = enhancement;

                    isEffect = true;
                }

                enhancement = Util.CalcStatusChangeValue(target.DexterityBase, friendEffect.dexterity, friendEffect.dexterityStatusChangeType);

                if (enhancement != 0 && Math.Abs(enhancement) >= Math.Abs(target.DexterityEnhancement))   // 命中 / hit
                {
                    target.DexterityEnhancement = enhancement;

                    isEffect = true;
                }

                enhancement = Util.CalcStatusChangeValue(target.EvasionBase, friendEffect.evasion, friendEffect.evasionStatusChangeType);

                if (enhancement != 0 && Math.Abs(enhancement) >= Math.Abs(target.EvasionEnhancement)) // 回避 / Avoidance
                {
                    target.EvasionEnhancement = enhancement;

                    isEffect = true;
                }

                // 各属性耐性
                // Each attribute resistance
#if true
                foreach (var ai in friendEffect.AttrDefenceList)
                {
                    if ((ai.value != 0) && (ai.attribute != Guid.Empty) &&
                        (!target.ResistanceAttackAttributeEnhance.ContainsKey(ai.attribute) ||
                        Math.Abs(ai.value) >= Math.Abs(target.ResistanceAttackAttributeEnhance[ai.attribute])))
                    {
                        target.ResistanceAttackAttributeEnhance[ai.attribute] = ai.value;
                        isEffect = true;
                    }
                }
                foreach (var ci in friendEffect.ConditionDefenceList)
                {
                    if ((ci.value != 0) && (ci.condition != Guid.Empty) &&
                        (!target.ResistanceAilmentEnhance.ContainsKey(ci.condition) ||
                        Math.Abs(ci.value) >= Math.Abs(target.ResistanceAilmentEnhance[ci.condition])))
                    {
                        target.ResistanceAilmentEnhance[ci.condition] = ci.value;
                        isEffect = true;
                    }
                }
#else
                if (friendEffect.attrAdefense != 0 && Math.Abs(friendEffect.attrAdefense) >= Math.Abs(target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.A]))
                {
                    target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.A] = friendEffect.attrAdefense;

                    isEffect = true;
                }
                if (friendEffect.attrBdefense != 0 && Math.Abs(friendEffect.attrAdefense) >= Math.Abs(target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.B]))
                {
                    target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.B] = friendEffect.attrBdefense;

                    isEffect = true;
                }
                if (friendEffect.attrCdefense != 0 && Math.Abs(friendEffect.attrAdefense) >= Math.Abs(target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.C]))
                {
                    target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.C] = friendEffect.attrCdefense;

                    isEffect = true;
                }
                if (friendEffect.attrDdefense != 0 && Math.Abs(friendEffect.attrAdefense) >= Math.Abs(target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.D]))
                {
                    target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.D] = friendEffect.attrDdefense;

                    isEffect = true;
                }
                if (friendEffect.attrEdefense != 0 && Math.Abs(friendEffect.attrAdefense) >= Math.Abs(target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.E]))
                {
                    target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.E] = friendEffect.attrEdefense;

                    isEffect = true;
                }
                if (friendEffect.attrFdefense != 0 && Math.Abs(friendEffect.attrAdefense) >= Math.Abs(target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.F]))
                {
                    target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.F] = friendEffect.attrFdefense;

                    isEffect = true;
                }
                if (friendEffect.attrGdefense != 0 && Math.Abs(friendEffect.attrAdefense) >= Math.Abs(target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.G]))
                {
                    target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.G] = friendEffect.attrGdefense;

                    isEffect = true;
                }
                if (friendEffect.attrHdefense != 0 && Math.Abs(friendEffect.attrAdefense) >= Math.Abs(target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.H]))
                {
                    target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.H] = friendEffect.attrHdefense;

                    isEffect = true;
                }
#endif

                if (target.HitPoint <= 0)
                {
                    if (totalHitPointDamage > 0)
                    {
                        // ダメージ効果があった場合は戦闘不能を付与する
                        // If there is a damage effect, grant incapacity
                        target.Down(catalog, battleEvents);
                    }
                    else if (!target.IsDeadCondition())
                    {
                        // 「戦闘不能」から回復したがHPが0のままだともう一度「戦闘不能」と扱われてしまうのでHPを回復させておく
                        // Recovered from \
                        target.HitPoint = 1;
                        target.consumptionStatusValue.SetStatus(gs.maxHPStatusID, 1);
                    }
                }

                target.ConsistancyConsumptionStatus(gameSettings);

                // 上限チェック
                // upper limit check
                if (target.HitPoint > target.MaxHitPoint) target.HitPoint = target.MaxHitPoint;
                if (target.MagicPoint > target.MaxMagicPoint) target.MagicPoint = target.MaxMagicPoint;

                // 下限チェック
                // lower limit check
                if (target.HitPoint < 0) target.HitPoint = 0;
                if (target.MagicPoint < 0) target.MagicPoint = 0;

                if (isEffect)
                {
                    friendEffectCharacters.Add(target);
                }

                if (totalHitPointDamage > 0)
                    target.CommandReactionType = ReactionType.Damage;
                else if (isEffect)
                    target.CommandReactionType = ReactionType.Heal;
                else
                    target.CommandReactionType = ReactionType.None;
            }

            // 対象にスキル効果を反映
            // Reflect skill effect on target
            foreach (var target in enemyEffectTargets)
            {
                bool isEffect = false;
                bool isDisplayMiss = false;

                if (skill.HitRate > battleRandom.Next(100))
				{
                    if (!enemyEffect.EffectParamSettings.UseOldEffectParam)
                    {
                        var effectValueDic = new Dictionary<Guid, int>();

                        foreach (var effectParam in enemyEffect.EffectParamSettings.GetConsumptionStatusValueChangeEffectParamList())
                        {
                            if (effectParam.ChangeParam == 0)
                            {
                                continue;
                            }

                            var info = gameSettings.GetCastStatusParamInfo(effectParam.Id);

                            if (info == null)
                            {
                                continue;
                            }

                            var value = 0;

                            switch (effectParam.ChangeType)
                            {
                                case Util.StatusChangeType.Unknwon:
                                    break;
                                case Util.StatusChangeType.Direct:
                                    value = effectParam.ChangeParam;
                                    break;
                                case Util.StatusChangeType.Percent:
                                    {
                                        var baseStatus = (effectParam.BaseStatusId != info.guId) ? effecter : target;

                                        value = baseStatus.GetSystemStatus(gameSettings, effectParam.BaseStatusId) * effectParam.ChangeParam / 100;
                                    }
                                    break;
                                default:
                                    break;
                            }

                            if (!effectValueDic.ContainsKey(info.guId))
                            {
                                effectValueDic[info.guId] = 0;
                            }

                            effectValueDic[info.guId] += value;
                        }

                        foreach (var key in effectValueDic.Keys.ToArray())
                        {
                            var effectValue = effectValueDic[key];

                            if (effectValue <= 0)
                            {
                                continue;
                            }

                            var info = gameSettings.GetCastStatusParamInfo(key);

                            if (info.Consumption)
                            {
                                // ダメージなので属性耐性を考慮
                                // Since it is damage, consider attribute resistance.
                                var effectValueTmp = effectValue * target.ResistanceAttackAttributePercent(enemySkillAttribute);

                                if (skill.CanGuard && (info.guId == gameSettings.maxHPStatusID))
                                {
                                    effectValueTmp *= target.DamageRate;
                                }

                                effectValueDic[info.guId] = (int)effectValueTmp;
                            }
                        }

                        foreach (var item in enemySkillFormulaDic)
                        {
                            var info = gameSettings.GetCastStatusParamInfo(item.Key);

                            if (info == null)
                            {
                                continue;
                            }

                            if (!effectValueDic.ContainsKey(info.guId))
                            {
                                effectValueDic[info.guId] = 0;
                            }

                            var effectValueTmp = EvalFormula(item.Value, effecter, target, enemySkillAttribute, battleRandom);

                            if (skill.CanGuard && (info.guId == gameSettings.maxHPStatusID))
                            {
                                effectValueTmp *= target.DamageRate;
                            }

                            effectValueDic[info.guId] += (int)effectValueTmp;
                        }

                        foreach (var item in effectValueDic)
                        {
                            var info = gameSettings.GetCastStatusParamInfo(item.Key);

                            if (info == null)
                            {
                                continue;
                            }

                            var effectValue = Math.Max(Math.Min(item.Value, effecter.MaxDamage), -effecter.MaxDamage);

                            target.consumptionStatusValue.SubStatus(item.Key, effectValue);

                            BattleDamageTextInfo.TextType textType;
                            var heal = effectValue < 0;

                            if (!heal && enemySkillDrainPercentDic.ContainsKey(item.Key))
                            {
                                effectValue = Math.Min(effectValue, target.consumptionStatusValue.GetStatus(item.Key));
                            }

							if (!enemyTotalSkillDamageDic.ContainsKey(info.ConsumptionId))
							{
                                enemyTotalSkillDamageDic[info.ConsumptionId] = 0;
                            }

                            enemyTotalSkillDamageDic[info.ConsumptionId] += effectValue;

                            switch (info.Key)
                            {
                                case Rom.GameSettings.MaxHPStatusKey:
                                    target.HitPoint -= effectValue;

                                    target.ConsistancyHPPercentConditions(catalog, battleEvents);

                                    if (!heal)
                                    {
                                        CheckDamageRecovery(target, effectValue);

                                        SetCounterAction(target, effecter);

                                        totalHitPointDamage += effectValue;
                                    }
                                    break;
                                case Rom.GameSettings.MaxMPStatusKey:
                                    target.MagicPoint -= effectValue;
                                    break;
                                default:
                                    break;
                            }

                            textType = heal ? BattleDamageTextInfo.TextType.Heal : BattleDamageTextInfo.TextType.Damage;
                            effectValue = Math.Abs(effectValue);

                            AddBattleDamageTextInfo(textInfo, textType, target, effectValue.ToString(), item.Key);

                            isEffect = true;
                        }
                    }
                    else
                    {
                        // HitPoint 回復 or ダメージ
                        // HitPoint recovery or damage
                        if (enemyEffect.hitpoint != 0 || enemyEffect.hitpointPercent != 0 ||
                            enemyEffect.hitpoint_powerPercent != 0 || enemyEffect.hitpoint_magicPercent != 0 ||
                            !string.IsNullOrEmpty(enemyEffect.hitpointFormula))
                        {
                            int damage = (enemyEffect.hitpoint) + (int)(enemyEffect.hitpointPercent / 100.0f * target.MaxHitPoint) + (int)(enemyEffect.hitpoint_powerPercent / 100.0f * effecter.Attack) + (int)(enemyEffect.hitpoint_magicPercent / 100.0f * effecter.Magic);

                            if (damage > effecter.MaxDamage)
                                damage = effecter.MaxDamage;
                            if (damage < -effecter.MaxDamage)
                                damage = -effecter.MaxDamage;

                            if (damage >= 0)
                            {
#if true
                                switch (target.AttackAttributeTolerance(enemyEffect.AttributeGuid))
#else
                            switch (target.AttackAttributeTolerance(enemyEffect.attribute))
#endif
                                {
                                    case AttributeToleranceType.Normal:
                                    case AttributeToleranceType.Strong:
                                    case AttributeToleranceType.Weak:
                                        {
#if true
                                            float effectValue = damage * target.ResistanceAttackAttributePercent(enemyEffect.AttributeGuid) * target.DamageRate;
#else
                                        float effectValue = damage * target.ResistanceAttackAttributePercent(enemyEffect.attribute) * target.DamageRate;
#endif
                                            if (effectValue > effecter.MaxDamage)
                                                effectValue = effecter.MaxDamage;
                                            if (effectValue < -effecter.MaxDamage)
                                                effectValue = -effecter.MaxDamage;
                                            damage = (int)effectValue;
                                        }
                                        break;

                                    case AttributeToleranceType.Absorb:
                                        {
#if true
                                            float effectValue = damage * target.ResistanceAttackAttributePercent(enemyEffect.AttributeGuid) * target.DamageRate;
#else
                                        float effectValue = damage * target.ResistanceAttackAttributePercent(enemyEffect.attribute) * target.DamageRate;
#endif
                                            if (effectValue > effecter.MaxDamage)
                                                effectValue = effecter.MaxDamage;
                                            if (effectValue < -effecter.MaxDamage)
                                                effectValue = -effecter.MaxDamage;
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
                                // When it is a recovery effect, resistance calculation is not performed
                            }

                            // 式がある？
                            // do you have a formula?
                            if (!string.IsNullOrEmpty(enemyEffect.hitpointFormula))
                            {
#if true
                                damage += (int)(EvalFormula(enemyEffect.hitpointFormula, effecter, target, enemyEffect.AttributeGuid, battleRandom) * target.DamageRate);
#else
                                damage += (int)(EvalFormula(enemyEffect.hitpointFormula, effecter, target, enemyEffect.attribute, battleRandom) * target.DamageRate);
#endif
                                if (damage > effecter.MaxDamage)
                                    damage = effecter.MaxDamage;
                                if (damage < -effecter.MaxDamage)
                                    damage = -effecter.MaxDamage;
                            }

                            BattleDamageTextInfo.TextType textType;

                            if (damage >= 0)
                            {
                                // 攻撃
                                // attack
                                if (skill.option.drain) damage = Math.Min(damage, target.HitPoint);

                                target.HitPoint -= damage;
                                target.consumptionStatusValue.SubStatus(gs.maxHPStatusID, damage);

                                CheckDamageRecovery(target, damage);

                                totalHitPointDamage += Math.Abs(damage);
                                textType = BattleDamageTextInfo.TextType.Damage;
                                SetCounterAction(target, effecter);
                            }
                            else
                            {
                                // 回復
                                // recovery
                                target.HitPoint -= damage;
                                target.consumptionStatusValue.SubStatus(gs.maxHPStatusID, damage);
                                textType = BattleDamageTextInfo.TextType.Heal;
                            }

                            AddBattleDamageTextInfo(textInfo, textType, target, Math.Abs(damage).ToString(), gs.maxHPStatusID);

                            target.ConsistancyHPPercentConditions(catalog, battleEvents);
                            isEffect = true;
                        }

                        // MagicPoint 減少
                        // MagicPoint decrease
                        if (enemyEffect.magicpoint != 0 || enemyEffect.magicpointPercent != 0)
                        {
                            int damage = (enemyEffect.magicpoint) + (int)(enemyEffect.magicpointPercent / 100.0f * target.MaxMagicPoint);

                            if (damage > effecter.MaxDamage)
                                damage = effecter.MaxDamage;
                            if (damage < -effecter.MaxDamage)
                                damage = -effecter.MaxDamage;

                            if (skill.option.drain) damage = Math.Min(damage, target.MagicPoint);

                            BattleDamageTextInfo.TextType textType;

                            if (damage >= 0)
                            {
                                // 攻撃
                                // attack
                                if (skill.option.drain) damage = Math.Min(damage, target.HitPoint);

                                target.MagicPoint -= damage;

                                var info = gameSettings.GetCastStatusParamInfo(gs.maxMPStatusID, true);

                                if (info != null)
                                {
                                    target.consumptionStatusValue.SubStatus(info.guId, damage);
                                }

                                totalMagicPointDamage += Math.Abs(damage);
                                textType = BattleDamageTextInfo.TextType.Damage;
                            }
                            else
                            {
                                // 回復
                                // recovery
                                target.MagicPoint -= damage;

                                var info = gameSettings.GetCastStatusParamInfo(gs.maxMPStatusID, true);

                                if (info != null)
                                {
                                    target.consumptionStatusValue.SubStatus(info.guId, damage);
                                }
                                textType = BattleDamageTextInfo.TextType.Heal;
                            }

                            AddBattleDamageTextInfo(textInfo, textType, target, Math.Abs(damage).ToString(), gs.maxMPStatusID);

                            isEffect = true;
                        }
                    }

                    // 状態異常
                    // Abnormal status
                    conditionRecoveryImpl(enemyEffect.RecoveryList, target, ref isEffect);
                    // 状態異常付与に失敗した時missと出す場合は dummy のかわりに isDisplayMiss を渡す
                    // Pass isDisplayMiss instead of dummy if you want to display a miss when you fail to apply a status ailment
                    bool dummy = false;
                    conditionAssignImpl(enemyEffect.AssignList, target, ref isEffect, ref dummy);

					// パラメータ変動
					// Parameter variation
					if (target.enhanceStatusValue.InitializeEnhancementEffectStatus(target.baseStatusValue.GetCalcEffectStatuses(enemyEffect.EffectParamSettings), false))
					{
                        isEffect = true;
                    }

                    var enhancement = target.enhanceStatusValue.GetSystemStatus(gameSettings, gs.maxHPStatusID);

                    if (enhancement != 0 && Math.Abs(enhancement) >= Math.Abs(target.MaxHitPointEnhance))      // 腕力 / strength
                    {
                        target.MaxHitPointEnhance = enhancement;

                        isEffect = true;
                    }

                    enhancement = target.enhanceStatusValue.GetSystemStatus(gameSettings, gs.maxMPStatusID);

                    if (enhancement != 0 && Math.Abs(enhancement) >= Math.Abs(target.MaxMagicPointEnhance))      // 腕力 / strength
                    {
                        target.MaxMagicPointEnhance = enhancement;

                        isEffect = true;
                    }

                    enhancement = Util.CalcStatusChangeValue(target.PowerBase, enemyEffect.power, enemyEffect.powerStatusChangeType);

                    if (enhancement != 0 && Math.Abs(enhancement) >= Math.Abs(target.PowerEnhancement))    // 腕力 / strength
                    {
                        target.PowerEnhancement = -enhancement;

                        isEffect = true;
                    }

                    enhancement = Util.CalcStatusChangeValue(target.VitalityBase, enemyEffect.vitality, enemyEffect.vitalityStatusChangeType);

                    if (enhancement != 0 && Math.Abs(enhancement) >= Math.Abs(target.VitalityEnhancement))   // 体力 / physical strength
                    {
                        target.VitalityEnhancement = -enhancement;

                        isEffect = true;
                    }

                    enhancement = Util.CalcStatusChangeValue(target.MagicBase, enemyEffect.magic, enemyEffect.magicStatusChangeType);

                    if (enhancement != 0 && Math.Abs(enhancement) >= Math.Abs(target.MagicEnhancement))    // 魔力 / magical power
                    {
                        target.MagicEnhancement = -enhancement;

                        isEffect = true;
                    }

                    enhancement = Util.CalcStatusChangeValue(target.SpeedBase, enemyEffect.speed, enemyEffect.speedStatusChangeType);

                    if (enhancement != 0 && Math.Abs(enhancement) >= Math.Abs(target.SpeedEnhancement))     // 素早さ / Agility
                    {
                        target.SpeedEnhancement = -enhancement;

                        isEffect = true;
                    }

                    enhancement = Util.CalcStatusChangeValue(target.DexterityBase, enemyEffect.dexterity, enemyEffect.dexterityStatusChangeType);

                    if (enhancement != 0 && Math.Abs(enhancement) >= Math.Abs(target.DexterityEnhancement)) // 命中 / hit
                    {
                        target.DexterityEnhancement = -enhancement;

                        isEffect = true;
                    }

                    enhancement = Util.CalcStatusChangeValue(target.EvasionBase, enemyEffect.evasion, enemyEffect.evasionStatusChangeType);

                    if (enhancement != 0 && Math.Abs(enhancement) >= Math.Abs(target.EvasionEnhancement))   // 回避 / Avoidance
                    {
                        target.EvasionEnhancement = -enhancement;

                        isEffect = true;
                    }

                    // 各属性耐性
                    // Each attribute resistance
#if true
                    foreach (var ai in enemyEffect.AttrDefenceList)
                    {
                        if ((ai.value != 0) && (ai.attribute != Guid.Empty) &&
                            (!target.ResistanceAttackAttributeEnhance.ContainsKey(ai.attribute) ||
                            Math.Abs(ai.value) >= Math.Abs(target.ResistanceAttackAttributeEnhance[ai.attribute])))
                        {
                            target.ResistanceAttackAttributeEnhance[ai.attribute] = -ai.value;
                            isEffect = true;
                        }
                    }
                    foreach (var ci in enemyEffect.ConditionDefenceList)
                    {
                        if ((ci.value != 0) && (ci.condition != Guid.Empty) &&
                            (!target.ResistanceAilmentEnhance.ContainsKey(ci.condition) ||
                            Math.Abs(ci.value) >= Math.Abs(target.ResistanceAilmentEnhance[ci.condition])))
                        {
                            target.ResistanceAilmentEnhance[ci.condition] = -ci.value;
                            isEffect = true;
                        }
                    }
#else
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
                    //
                    if (enemyEffect.attrGdefense != 0 && Math.Abs(enemyEffect.attrGdefense) >= Math.Abs(target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.G]))
                    {
                        target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.G] = -enemyEffect.attrGdefense;

                        isEffect = true;
                    }
                    if (enemyEffect.attrHdefense != 0 && Math.Abs(enemyEffect.attrHdefense) >= Math.Abs(target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.H]))
                    {
                        target.ResistanceAttackAttributeEnhance[(int)AttackAttributeType.H] = -enemyEffect.attrHdefense;

                        isEffect = true;
                    }
#endif
                }
                else
                {
                    isDisplayMiss = true;
                }

                target.ConsistancyConsumptionStatus(gameSettings);

                // 上限チェック
                // upper limit check
                if (target.HitPoint > target.MaxHitPoint) target.HitPoint = target.MaxHitPoint;
                if (target.MagicPoint > target.MaxMagicPoint) target.MagicPoint = target.MaxMagicPoint;

                // 下限チェック
                // lower limit check
                if (target.HitPoint < 0) target.HitPoint = 0;
                if (target.MagicPoint < 0) target.MagicPoint = 0;

                if (isEffect || isDisplayMiss)
                {
                    enemyEffectCharacters.Add(target);
                }

                if (isDisplayMiss)
                {
                    AddBattleMissTextInfo(textInfo, target);
                }

                if (totalHitPointDamage > 0)
                    target.CommandReactionType = ReactionType.Damage;
                else if (isEffect)
                    target.CommandReactionType = ReactionType.Heal;
                else
                    target.CommandReactionType = ReactionType.None;
            }

			// 与えたダメージ分 自分のHP, MPを回復する
			// Recover HP and MP equal to the damage dealt
            if ((friendSkillDrainPercentDic.Count > 0) || (enemySkillDrainPercentDic.Count > 0))
            {
                var totalDamageDic = new Dictionary<Guid, int>();

                CalcDrainValue(totalDamageDic, friendTotalSkillValueDic, friendSkillDrainPercentDic);
                CalcDrainValue(totalDamageDic, enemyTotalSkillDamageDic, enemySkillDrainPercentDic);

                if (totalDamageDic.Count > 0)
                {
                    foreach (var item in totalDamageDic)
                    {
                        var totalDamage = item.Value;

                        if (totalDamage > 0)
						{
                            var info = gameSettings.GetCastStatusParamInfo(item.Key);

                            if (info == null)
                            {
                                continue;
                            }

                            switch (info.Key)
                            {
                                case Rom.GameSettings.MaxHPStatusKey:
                                    effecter.HitPoint += totalDamage;
                                    break;
                                case Rom.GameSettings.MaxMPStatusKey:
                                    effecter.MagicPoint += totalDamage;
                                    break;
                                default:
                                    break;
                            }

                            effecter.consumptionStatusValue.AddStatus(info.guId, totalDamage);

                            AddBattleDamageTextInfo(textInfo, BattleDamageTextInfo.TextType.Heal, effecter, totalDamage.ToString(), item.Key);
                        }
                    }
                }
                else
                {
                    if (totalHitPointDamage > 0)
                    {
                        effecter.HitPoint += totalHitPointDamage;
                        effecter.consumptionStatusValue.AddStatus(gs.maxHPStatusID, totalHitPointDamage);

                        AddBattleDamageTextInfo(textInfo, BattleDamageTextInfo.TextType.Heal, effecter, totalHitPointDamage.ToString(), gs.maxHPStatusID);
                    }

                    if (totalMagicPointDamage > 0)
                    {
                        effecter.MagicPoint += totalMagicPointDamage;

                        var info = gameSettings.GetCastStatusParamInfo(gs.maxMPStatusID, true);

                        if (info != null)
                        {
                            effecter.consumptionStatusValue.AddStatus(info.guId, totalMagicPointDamage);
                        }

                        AddBattleDamageTextInfo(textInfo, BattleDamageTextInfo.TextType.Heal, effecter, totalMagicPointDamage.ToString(), gs.maxMPStatusID);
                    }
                }
            }

            if (option.selfDestruct)
            {
                effecter.HitPoint = 0;
                effecter.consumptionStatusValue.SetStatus(gs.maxHPStatusID, 0);
            }

            effecter.ConsistancyConsumptionStatus(gameSettings);

            // スキル使用者のパラメータ 上限チェック
            // Skill user parameter upper limit check
            if (effecter.HitPoint > effecter.MaxHitPoint) effecter.HitPoint = effecter.MaxHitPoint;
            if (effecter.MagicPoint > effecter.MaxMagicPoint) effecter.MagicPoint = effecter.MaxMagicPoint;

            if (effecter.HitPoint < 0) effecter.HitPoint = 0;
            if (effecter.MagicPoint < 0) effecter.MagicPoint = 0;

            friend = friendEffectCharacters.ToArray();
            enemy = enemyEffectCharacters.ToArray();

            // イベント開始
            // event start
            var oldData = true;
            var idx = 0;

            if ((friendEffect != null) && !friendEffect.EffectParamSettings.UseOldEffectParam && (friendEffectTargets.Length > 0))
			{
                var referenceId = friendEffect.EffectParamSettings.GetReferenceIdEffectParam(Catalog.SIGNATURE_EVENT);

				if ((referenceId != null) && (referenceId.RefId != Guid.Empty))
				{
                    if (option.waitCommon)
                    {
                        waitForCommons[idx++] = referenceId.RefId;
                    }

                    // 効果がコモン発動だけだった場合、ターゲットリストはそのままにする
                    // If the effect is only a common activation, leave the target list as is.
                    if (friendEffect.EffectParamSettings.EffectParamList.Count == 1)
                        friend = friendEffectTargets;

                    battleEvents.start(referenceId.RefId);
                }

                oldData = false;
            }

            if ((enemyEffect != null) && !enemyEffect.EffectParamSettings.UseOldEffectParam && (enemyEffectTargets.Length > 0))
            {
                var referenceId = enemyEffect.EffectParamSettings.GetReferenceIdEffectParam(Catalog.SIGNATURE_EVENT);

                if ((referenceId != null) && (referenceId.RefId != Guid.Empty))
                {
                    if (option.waitCommon)
                    {
                        waitForCommons[idx++] = referenceId.RefId;
                    }

                    // 効果がコモン発動だけだった場合、ターゲットリストはそのままにする
                    // If the effect is only a common activation, leave the target list as is.
                    if (enemyEffect.EffectParamSettings.EffectParamList.Count == 1)
                        enemy = enemyEffectTargets;

                    battleEvents.start(referenceId.RefId);
                }

                oldData = false;
            }

            if (oldData && (option.commonExec != Guid.Empty))
            {
                if (option.waitCommon)
                    waitForCommon = option.commonExec;
                battleEvents.start(option.commonExec);
            }

            battleEvents.setLastSkillUserIndex(effecter);
        }

		private void CalcDrainValue(Dictionary<AttackAttributeType, int> inTotalDamageDic, Dictionary<AttackAttributeType, int> inTotalSkillValueDic, Dictionary<Guid, Rom.DamageDrainEffectParam> inSkillDrainPercentDic)
		{
            foreach (var item in inTotalSkillValueDic)
            {
				if (inSkillDrainPercentDic.ContainsKey(item.Key))
				{
                    var skillDrain = inSkillDrainPercentDic[item.Key];

                    if (!inTotalDamageDic.ContainsKey(skillDrain.RecoveryId))
                    {
                        inTotalDamageDic[skillDrain.RecoveryId] = 0;
                    }

                    inTotalDamageDic[skillDrain.RecoveryId] += (int)(item.Value * skillDrain.Percent / 100);
                }
            }
        }

        public override void FixedUpdate()
        {
            (battleViewer as BattleViewer3D)?.FixedUpdate();
        }

        private void conditionAssignImpl(List<Rom.ConditionInfo> list, BattleCharacterBase target, ref bool isEffect, ref bool isDisplayMiss)
        {
            foreach (var info in list)
            {
                if (info.value != 0)
                {
                    var condition = catalog.getItemFromGuid<Rom.Condition>(info.condition);
                    var percent = target.GetResistanceAilmentStatus(info.condition);

                    if (condition == null)
                        continue;

                    if (battleRandom.Next(100) < condition.SuccessRate * (100 - percent) / 100)
                    {
                        target.SetCondition(catalog, info.condition, battleEvents);

                        if (condition != null)
                        {
                            if ((target.selectedBattleCommandType != BattleCommandType.Skip))
                            {
                                if (target != activeCharacter &&
                                    (condition.actionDisabled || condition.attack))
                                {
                                    target.selectedBattleCommandType = BattleCommandType.Cancel;
                                }

                                if (condition.deadCondition && condition.deadConditionPercent == 0)
                                {
                                    target.HitPoint = 0;
                                    target.consumptionStatusValue.SetStatus(catalog.getGameSettings().maxHPStatusID, 0);
                                }
                            }
                        }

                        // メッセージ用変数が未生成であれば作る
                        // If the message variable is not generated, create it
                        if (!displayedSetConditionsDic.ContainsKey(target))
                        {
                            displayedSetConditionsDic.Add(target, new List<Rom.Condition>());
                        }

                        // メッセージ用に現在の状態異常をセットする
                        // set current status for message
                        if (target.conditionInfoDic.ContainsKey(info.condition))
                        {
                            condition = catalog.getItemFromGuid<Rom.Condition>(info.condition);
                            displayedSetConditionsDic[target].Add(condition);
                        }

                        isEffect = true;
                    }
                    else
                    {
                        isDisplayMiss = true;
                    }
                }
            }
        }

        private void conditionRecoveryImpl(List<Rom.ConditionInfo> list, BattleCharacterBase target, ref bool isEffect)
        {
            foreach (var info in list)
            {
                if ((info.value != 0) && target.conditionInfoDic.ContainsKey(info.condition))
                {
                    target.RecoveryCondition(info.condition);

                    var condition = catalog.getItemFromGuid<Rom.Condition>(info.condition);

                    if (condition != null)
                    {
                        recoveryStatusInfo.Add(new RecoveryStatusInfo(target, condition));

                        if (condition.actionDisabled || condition.attack)
                        {
                            target.selectedBattleCommandType = BattleCommandType.Cancel;
                        }

                        if (condition.deadCondition && condition.deadConditionPercent == 0)
                        {
                            // 戦闘不能回復効果で回復がゼロポイントの場合は強制的に1回復する
                            // If the recovery is 0 points due to the incapacity recovery effect, it will be forcibly recovered by 1.
                            if (target.HitPoint == 0)
                            {
                                target.HitPoint = 1;
                                target.consumptionStatusValue.SetStatus(catalog.getGameSettings().maxHPStatusID, 1);
                            }

                            var targetEnemy = target as BattleEnemyData;

                            if (targetEnemy != null)
                            {
                                battleViewer.AddFadeInCharacter(targetEnemy);
                            }
                        }
                    }

                    target.ConsistancyHPPercentConditions(catalog, battleEvents);
                    isEffect = true;
                }
            }
        }

        private void PaySkillCost(BattleCharacterBase effecter, Rom.NSkill skill)
        {
            // スキル発動時のコストとして発動者の消費ステータスを消費
            // Consumes the caster's consumption status as the skill activation cost.
            PaySkillCostStatus(effecter, skill);

            // 味方だったらアイテムを消費
            // Consumes items if it is an ally
            if (effecter is BattlePlayerData)
                party.AddItem(skill.option.consumptionItem, -skill.option.consumptionItemAmount);
        }

        private bool UseItem(Rom.NItem item, BattleCharacterBase target, List<BattleDamageTextInfo> textInfo, List<RecoveryStatusInfo> recoveryStatusInfo)
        {
            var gs = catalog.getGameSettings();
            var expendable = item.expendable;
            bool isUsedItem = false;
            var isDeadCondition = target.IsDeadCondition();

            {
                if (isDeadCondition)
                {
                    foreach (var info in expendable.RecoveryList)
                    {
                        if (info.value != 0)
                        {
                            var condition = catalog.getItemFromGuid<Rom.Condition>(info.condition);

                            if (isDeadCondition && condition != null)
                            {
                                if (condition.deadCondition && condition.deadConditionPercent == 0)
                                {
                                    // 蘇生アイテムなら、生きているものとする
                                    // If it's a resurrection item, it's considered alive.
                                    isDeadCondition = false;

                                    break;
                                }
                            }
                        }
					}
				}

                // ステータス 強化
                // status enhancement
                if (!isDeadCondition && target.enhanceStatusValue.InitializeEnhancementEffectStatus(target.baseStatusValue.GetCalcEffectStatuses(item.EffectParamSettings), true))
                {
                    isUsedItem = true;
                }

                // 最大HP 増加
                // Max HP increase
                if (expendable.maxHitpoint > 0 && Math.Abs(expendable.maxHitpoint) >= Math.Abs(target.MaxHitPointEnhance) && !isDeadCondition)
                {
                    target.MaxHitPointEnhance = expendable.maxHitpoint;
                    isUsedItem = true;
                }

                // 最大MP 増加
                // Max MP increase
                if (expendable.maxMagitpoint > 0 && Math.Abs(expendable.maxMagitpoint) >= Math.Abs(target.MaxMagicPointEnhance) && !isDeadCondition)
                {
                    target.MaxMagicPointEnhance = expendable.maxMagitpoint;
                    isUsedItem = true;
                }

                if (!item.EffectParamSettings.UseOldEffectParam)
                {
                    if (!isDeadCondition)
                    {
                        var healFormulaDic = new Dictionary<Guid, string>();

                        foreach (var effectParam in item.EffectParamSettings.GetEtcEffectParamList())
                        {
                            switch (effectParam.Type)
                            {
                                case Rom.EffectParamBase.EffectType.ConsumptionValueFormula:
                                    if ((effectParam is Rom.ConsumptionValueFormulaEffectParam healFormula) && (healFormula.FormulaType == Rom.ConsumptionValueFormulaEffectParam.FormulaEffectType.Heal) && !string.IsNullOrEmpty(healFormula.Formula))
                                    {
                                        healFormulaDic[healFormula.TargetId] = healFormula.Formula;
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }

                        if (Heal(target, item.EffectParamSettings, false, healFormulaDic, textInfo, Guid.Empty))
                        {
                            isUsedItem = true;
                        }

                        target.ConsistancyHPPercentConditions(catalog, battleEvents);
                    }
                }
                else
                {
                    // 回復
                    // recovery
                    int healHitPoint = 0;
                    int healMagicPoint = 0;

                    // HP回復 (固定値)
                    // HP recovery (fixed value)
                    if (expendable.hitpoint > 0 && !isDeadCondition)
                    {
                        healHitPoint += expendable.hitpoint;
                        isUsedItem = true;
                    }

                    // HP回復 (割合)
                    // HP recovery (percentage)
                    if (expendable.hitpointPercent > 0 && !isDeadCondition)
                    {
                        healHitPoint += (int)(expendable.hitpointPercent / 100.0f * target.MaxHitPoint);
                        isUsedItem = true;
                    }

                    // MP回復 (固定値)
                    // MP recovery (fixed value)
                    if (expendable.magicpoint > 0 && !isDeadCondition)
                    {
                        healMagicPoint += expendable.magicpoint;
                        isUsedItem = true;
                    }

                    // MP回復 (割合)
                    // MP recovery (percentage)
                    if (expendable.magicpointPercent > 0 && !isDeadCondition)
                    {
                        healMagicPoint += (int)(expendable.magicpointPercent / 100.0f * target.MaxMagicPoint);
                        isUsedItem = true;
                    }

                    if (healHitPoint > 0)
                    {
                        target.HitPoint += healHitPoint;
                        target.consumptionStatusValue.AddStatus(gs.maxHPStatusID, healHitPoint);

                        AddBattleDamageTextInfo(textInfo, BattleDamageTextInfo.TextType.Heal, target, healHitPoint.ToString(), gs.maxHPStatusID);
                    }

                    if (healMagicPoint > 0)
                    {
                        target.MagicPoint += healMagicPoint;

                        var info = gameSettings.GetCastStatusParamInfo(gs.maxMPStatusID, true);

                        if (info != null)
                        {
                            target.consumptionStatusValue.AddStatus(info.guId, healMagicPoint);
                        }

                        AddBattleDamageTextInfo(textInfo, BattleDamageTextInfo.TextType.Heal, target, healMagicPoint.ToString(), gs.maxMPStatusID);
                    }

                    target.ConsistancyConsumptionStatus(gameSettings);
                    target.HitPoint = Math.Min(target.HitPoint, target.MaxHitPoint);
                    target.ConsistancyHPPercentConditions(catalog, battleEvents);
                    target.MagicPoint = Math.Min(target.MagicPoint, target.MaxMagicPoint);
                }

                target.enhanceStatusValue.InitializeEnhancementEffectStatus(target.baseStatusValue.GetCalcEffectStatuses(item.EffectParamSettings), true);

                if (expendable.power > 0 && Math.Abs(expendable.power) >= Math.Abs(target.PowerEnhancement) && !isDeadCondition)
                {
                    target.PowerEnhancement = expendable.power;
                    isUsedItem = true;
                }

                if (expendable.vitality > 0 && Math.Abs(expendable.vitality) >= Math.Abs(target.VitalityEnhancement) && !isDeadCondition)
                {
                    target.VitalityEnhancement = expendable.vitality;
                    isUsedItem = true;
                }
                if (expendable.magic > 0 && Math.Abs(expendable.magic) > Math.Abs(target.MagicEnhancement) && !isDeadCondition)
                {
                    target.MagicEnhancement = expendable.magic;
                    isUsedItem = true;
                }
                if (expendable.speed > 0 && Math.Abs(expendable.speed) >= Math.Abs(target.SpeedEnhancement) && !isDeadCondition)
                {
                    target.SpeedEnhancement = expendable.speed;
                    isUsedItem = true;
                }

                // 状態異常回復
                // status ailment recovery
                foreach (var info in expendable.RecoveryList)
                {
                    if ((info.value != 0) && target.conditionInfoDic.ContainsKey(info.condition))
                    {
                        target.RecoveryCondition(info.condition);

                        var condition = catalog.getItemFromGuid<Rom.Condition>(info.condition);

                        if (condition != null)
                        {
                            recoveryStatusInfo.Add(new RecoveryStatusInfo(target, condition));

                            if (condition.actionDisabled || condition.attack)
                            {
                                target.selectedBattleCommandType = BattleCommandType.Cancel;
                            }

                            if (condition.deadCondition && condition.deadConditionPercent == 0)
                            {
                                // 戦闘不能 回復
                                // incapable of fighting recovery
                                // アイテム使用時の状況で想定されるケース
                                // Assumed cases when using items
                                // ケース1 : 対象となるキャラクターが戦闘不能時 => そのままアイテムを使用して戦闘可能状態に回復 (実装OK)
                                // Case 1 : When the target character is unable to fight =\u003e Use the item as it is to recover to a fighting state (implementation OK)
                                // ケース2 : 対象となるキャラクターが戦闘可能状態だがパーティ内に戦闘不能状態のキャラクターがいる => 対象を変更してアイテムを使用
                                // Case 2 : The target character is ready to fight, but there is a character in the party who is unable to fight =\u003e Change the target and use the item
                                // ケース3 : パーティ内に戦闘不能状態のキャラクターが1人もいない => アイテムを使用しない
                                // Case 3 : No characters in the party are incapacitated =\u003e Do not use items
                                var targetEnemy = target as BattleEnemyData;

                                if (targetEnemy != null)
                                {
                                    battleViewer.AddFadeInCharacter(targetEnemy);
                                }

                                int recoveryHitPoint = 0;

                                recoveryHitPoint += expendable.hitpoint;
                                recoveryHitPoint += (int)(expendable.hitpointPercent / 100.0f * target.MaxHitPoint);

                                if (recoveryHitPoint <= 0)
                                {
                                    recoveryHitPoint = 1;

                                    AddBattleDamageTextInfo(textInfo, BattleDamageTextInfo.TextType.Heal, target, recoveryHitPoint.ToString(), gs.maxHPStatusID);

                                    recoveryHitPoint = Math.Min(recoveryHitPoint, target.MaxHitPoint);

                                    target.HitPoint = recoveryHitPoint;
                                    target.consumptionStatusValue.SetStatus(gs.maxHPStatusID, recoveryHitPoint);
                                }
                            }
                        }

                        target.ConsistancyHPPercentConditions(catalog, battleEvents);
                        isUsedItem = true;
                    }
                }

                bool isEffect = false;
                bool isDisplayMiss = false;

                conditionAssignImpl(item.EffectParamSettings.GetAttachConditionList(), target, ref isEffect, ref isDisplayMiss);

                if (isDisplayMiss)
                {
                    // 状態異常付与に失敗した時missと出す場合はコメントアウトを外す
                    // Remove the comment out if you want to output a miss when you fail to apply the status ailment
                    //AddBattleMissTextInfo(textInfo, target);
                }
            }

            // イベント開始
            // event start
            if (expendable.commonExec != Guid.Empty)
            {
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
            // Disable effects that meet the end condition
            enhanceEffects.RemoveAll(effect => effect.type == EnhanceEffect.EnhanceEffectType.TurnEffect && effect.turnCount >= effect.durationTurn);
            enhanceEffects.RemoveAll(effect => effect.type == EnhanceEffect.EnhanceEffectType.DurationEffect && effect.enhanceEffect <= 0);
        }

        private void CheckBattleCharacterDown()
        {
            foreach (var player in playerData)
            {
                player.ConsistancyHPPercentConditions(catalog, battleEvents);

                if (player.HitPoint <= 0)
                {
                    // 戦闘不能時は実行予定のコマンドをキャンセルする
                    // Cancels scheduled commands when incapacitated
                    // 同一ターン内で蘇生しても行動できない仕様でOK
                    // It is OK with specifications that can not act even if revived in the same turn
                    player.selectedBattleCommandType = BattleCommandType.Nothing_Down;

                    player.ChangeEmotion(Resource.Face.FaceType.FACE_SORROW);

                    player.Down(catalog, battleEvents);
                }

                SetBattleStatusData(player);
            }

            foreach (var enemy in enemyData)
            {
                enemy.ConsistancyHPPercentConditions(catalog, battleEvents);

                if (enemy.HitPoint <= 0)
                {
                    enemy.Down(catalog, battleEvents);
                    enemy.selectedBattleCommandType = BattleCommandType.Nothing_Down;
                }
            }

            foreach (var player in playerData)
            {
                battleViewer.SetPlayerStatusEffect(player);
            }

            bool isPlaySE = false;

            foreach (var enemyMonster in enemyData)
            {
                if ((enemyMonster.HitPoint <= 0 || enemyMonster.IsDeadCondition()) && battleViewer.IsEndMotion(enemyMonster))
                {
                    if (!battleViewer.ContainsFadeOutCharacter(enemyMonster) && enemyMonster.imageAlpha > 0)
                    {
                        enemyMonster.HitPoint = 0;
                        enemyMonster.consumptionStatusValue.SetStatus(catalog.getGameSettings().maxHPStatusID, 0);

                        enemyMonster.selectedBattleCommandType = BattleCommandType.Cancel;

                        // フェードアウト開始を遅らせる
                        // Delay start of fadeout
                        enemyMonster.imageAlpha = 1 + BattleViewer.FADEINOUT_SPEED * 20;
                        battleViewer.AddFadeOutCharacter(enemyMonster);

                        isPlaySE = true;
                    }
                }
                else
                {
                    if (enemyMonster.imageAlpha < 0)
                    {
                        // ここだとタイミングが遅くなるので、状態異常を付与する段階で行う事にする
                        // Since the timing will be delayed here, I decided to do it at the stage of granting status ailments.
                        //battleViewer.AddFadeInCharacter(enemyMonster);
                    }
                }

            }

            if (isPlaySE)
            {
                Audio.PlaySound(owner.se.defeat);
            }

        }
        private void SetBattleStatusData(BattleCharacterBase player)
        {
            player.battleStatusData.statusValue.InitializeStatus(player.baseStatusValue);
            player.battleStatusData.consumptionStatusValue.InitializeStatus(player.consumptionStatusValue);

            player.battleStatusData.Name = player.Name;
            player.battleStatusData.HitPoint = player.HitPoint;
            player.battleStatusData.MagicPoint = player.MagicPoint;
            player.battleStatusData.MaxHitPoint = player.MaxHitPoint;
            player.battleStatusData.MaxMagicPoint = player.MaxMagicPoint;

            player.battleStatusData.ParameterStatus = BattleStatusWindowDrawer.StatusIconType.None;

            if (player.IsPowerEnhancementUp)
            {
                player.battleStatusData.ParameterStatus |= BattleStatusWindowDrawer.StatusIconType.PowerUp;
            }
            else if (player.IsPowerEnhancementDown)
            {
                player.battleStatusData.ParameterStatus |= BattleStatusWindowDrawer.StatusIconType.PowerDown;
            }

            if (player.IsVitalityEnhancementUp)
            {
                player.battleStatusData.ParameterStatus |= BattleStatusWindowDrawer.StatusIconType.VitalityUp;
            }
            else if (player.IsVitalityEnhancementDown)
            {
                player.battleStatusData.ParameterStatus |= BattleStatusWindowDrawer.StatusIconType.VitalityDown;
            }

            if (player.IsMagicEnhancementUp)
            {
                player.battleStatusData.ParameterStatus |= BattleStatusWindowDrawer.StatusIconType.MagicUp;
            }
            else if (player.IsMagicEnhancementDown)
            {
                player.battleStatusData.ParameterStatus |= BattleStatusWindowDrawer.StatusIconType.MagicDown;
            }

            if (player.IsSpeedEnhancementUp)
            {
                player.battleStatusData.ParameterStatus |= BattleStatusWindowDrawer.StatusIconType.SpeedUp;
            }
            else if (player.IsSpeedEnhancementDown)
            {
                player.battleStatusData.ParameterStatus |= BattleStatusWindowDrawer.StatusIconType.SpeedDown;
            }
        }

        internal void SetNextBattleStatus(BattleCharacterBase player)
        {
            if (player == null)
                return;

            player.startStatusData.statusValue.InitializeStatus(player.battleStatusData.statusValue);
            player.startStatusData.consumptionStatusValue.InitializeStatus(player.battleStatusData.consumptionStatusValue);

            player.startStatusData.HitPoint = player.battleStatusData.HitPoint;
            player.startStatusData.MagicPoint = player.battleStatusData.MagicPoint;

            player.startStatusData.MaxHitPoint = player.battleStatusData.MaxHitPoint;
            player.startStatusData.MaxMagicPoint = player.battleStatusData.MaxMagicPoint;

            player.nextStatusData.statusValue.InitializeStatus(player.baseStatusValue);
            player.nextStatusData.consumptionStatusValue.InitializeStatus(player.consumptionStatusValue);

            player.nextStatusData.HitPoint = player.HitPoint;
            player.nextStatusData.MagicPoint = player.MagicPoint;

            player.nextStatusData.MaxHitPoint = player.MaxHitPoint;
            player.nextStatusData.MaxMagicPoint = player.MaxMagicPoint;
        }

        internal bool UpdateBattleStatusData(BattleCharacterBase player)
        {
            bool isUpdated = false;
            var nowConsumptionStatusValue = player.battleStatusData.consumptionStatusValue;
            var startConsumptionStatusValue = player.startStatusData.consumptionStatusValue;
            var nextConsumptionStatusValue = player.nextStatusData.consumptionStatusValue;

            foreach (var info in catalog.getGameSettings().CastStatusParamInfoList)
			{
                if (info.Consumption)
                {
                    var now = nowConsumptionStatusValue.GetStatus(info.guId);
                    var start = startConsumptionStatusValue.GetStatus(info.guId);
                    var next = nextConsumptionStatusValue.GetStatus(info.guId);

					if (now != next)
					{
                        nowConsumptionStatusValue.SetStatus(info.guId, (int)((next - start) * statusUpdateTweener.CurrentValue + start));

                        isUpdated = true;
                    }
                }
            }

            if (player.battleStatusData.HitPoint != player.nextStatusData.HitPoint)
            {
                player.battleStatusData.HitPoint = (int)((player.nextStatusData.HitPoint - player.startStatusData.HitPoint) * statusUpdateTweener.CurrentValue + player.startStatusData.HitPoint);

                isUpdated = true;
            }

            if (player.battleStatusData.MagicPoint != player.nextStatusData.MagicPoint)
            {
                player.battleStatusData.MagicPoint = (int)((player.nextStatusData.MagicPoint - player.startStatusData.MagicPoint) * statusUpdateTweener.CurrentValue + player.startStatusData.MagicPoint);

                isUpdated = true;
            }

            return isUpdated;
        }

        public override void Update()
        {
            if (battleState == BattleState.SelectPlayerBattleCommand ||
                battleState == BattleState.Result ||
                battleState == BattleState.FinishFadeIn ||
                battleState == BattleState.FinishFadeOut ||
                (battleEvents?.isBusy() ?? false))
                GameMain.setGameSpeed();
            else
                GameMain.setGameSpeed(owner.debugSettings.battleFastForward ? 4 : battleSpeed);

            battleStateFrameCount += GameMain.getRelativeParam60FPS();

            battleEvents?.update();

            UpdateCommandSelect();

            UpdateMoveTargetPos();

            UpdateBattleState();

            if (playerData != null)
            {
                foreach (var player in playerData)
                {
                    player.Update();
                }



                foreach (var enemyMonster in enemyData)
                {
                    enemyMonster.Update();
                }



                battleViewer.Update(playerViewData, enemyMonsterViewData);
            }
        }

        /// <summary>
        /// 射程用の位置情報の更新
        /// Location update for range
        /// </summary>
        private void UpdateMoveTargetPos()
        {
            for (int i = 0; i < targetPlayerData?.Count; i++)
			{
                var player = targetPlayerData[i] as BattlePlayerData;
                var actor = Viewer.searchFromActors(player);

				if (actor != null)
				{
                    var pos = actor.getRangePos();
                    player.moveTargetPos.X = pos.X;
                    player.moveTargetPos.Z = pos.Z;
                }
            }

            for (int i = 0; i < targetEnemyData?.Count; i++)
			{
                var enemy = targetEnemyData[i] as BattleEnemyData;
                var actor = Viewer.searchFromActors(enemy);

                if (actor != null)
                {
                    var pos = actor.getRangePos();
                    enemy.moveTargetPos.X = pos.X;
                    enemy.moveTargetPos.Z = pos.Z;
                }
            }
        }

        private void UpdateBattleState()
        {
            switch (battleState)
            {
                case BattleState.StartFlash:
                    UpdateBattleState_StartFlash();
                    break;
                case BattleState.StartFadeOut:
                    UpdateBattleState_StartFadeOut();
                    break;
                case BattleState.StartFadeIn:
                    UpdateBattleState_StartFadeIn();
                    break;
                case BattleState.BattleSetting:
                    UpdateBattleState_BattleSetting();
                    break;
                case BattleState.BattleStart:
                    UpdateBattleState_BattleStart();
                    break;
                case BattleState.Wait:
                    UpdateBattleState_Wait();
                    break;
                case BattleState.LockInEquipment:
                    UpdateBattleState_LockInEquipment();
                    break;
                case BattleState.PlayerTurnStart:
                    UpdateBattleState_PlayerTurnStart();
                    break;
                case BattleState.CheckTurnRecoveryStatus:
                    UpdateBattleState_CheckTurnRecoveryStatus();
                    break;
                case BattleState.DisplayTurnRecoveryStatus:
                    UpdateBattleState_DisplayTurnRecoveryStatus();
                    break;
                case WAIT_CTB_GAUGE:
                    UpdateBattleState_WaitCtbGauge();
                    break;
                case BattleState.SetEnemyBattleCommand:
                    UpdateBattleState_SetEnemyBattleCommand();
                    break;
                case BattleState.SelectActivePlayer:
                    UpdateBattleState_SelectActivePlayer();
                    break;
                case BattleState.CheckCommandSelect:
                    UpdateBattleState_CheckCommandSelect();
                    break;
                case BattleState.SetPlayerBattleCommand:
                    UpdateBattleState_SetPlayerBattleCommand();
                    break;
                case BattleState.WaitEventsBeforeCommandSelect:
                    UpdateBattleState_WaitEventsBeforeCommandSelect();
                    break;
                case BattleState.SelectPlayerBattleCommand:
                    UpdateBattleState_SelectPlayerBattleCommand();
                    break;
                case BattleState.SetPlayerBattleCommandTarget:
                    UpdateBattleState_SetPlayerBattleCommandTarget();
                    break;
                case BattleState.SortBattleActions:
                    UpdateBattleState_SortBattleActions();
                    break;
                case BattleState.ReadyExecuteCommand:
                    UpdateBattleState_ReadyExecuteCommand();
                    break;
                case BattleState.SetStatusMessageText:
                    UpdateBattleState_SetStatusMessageText();
                    break;
                case BattleState.DisplayStatusMessage:
                    UpdateBattleState_DisplayStatusMessage();
                    break;
                case BattleState.SetCommandMessageText:
                    UpdateBattleState_SetCommandMessageText();
                    break;
                case BattleState.DisplayMessageText:
                    UpdateBattleState_DisplayMessageText();
                    break;
                case BattleState.ExecuteBattleCommand:
                    UpdateBattleState_ExecuteBattleCommand();
                    break;
                case BattleState.SetCommandEffect:
                    UpdateBattleState_SetCommandEffect();
                    break;
                case BattleState.DisplayCommandEffect:
                    UpdateBattleState_DisplayCommandEffect();
                    break;
                case BattleState.DisplayDamageText:
                    UpdateBattleState_DisplayDamageText();
                    break;
                case BattleState.SetConditionMessageText:
                    UpdateBattleState_SetConditionMessageText();
                    break;
                case BattleState.DisplayConditionMessageText:
                    UpdateBattleState_DisplayConditionMessageText();
                    break;
                case BattleState.CheckCommandRecoveryStatus:
                    UpdateBattleState_CheckCommandRecoveryStatus();
                    break;
                case BattleState.DisplayCommandRecoveryStatus:
                    UpdateBattleState_DisplayCommandRecoveryStatus();
                    break;
                case BattleState.CheckBattleCharacterDown1:
                    UpdateBattleState_CheckBattleCharacterDown1();
                    break;
                case BattleState.FadeMonsterImage1:
                    UpdateBattleState_FadeMonsterImage1();
                    break;
                case BattleState.BattleFinishCheck1:
                    UpdateBattleState_BattleFinishCheck1();
                    break;
                case BattleState.ProcessPoisonStatus:
                    UpdateBattleState_ProcessPoisonStatus();
                    break;
                case BattleState.DisplayStatusDamage:
                    UpdateBattleState_DisplayStatusDamage();
                    break;
                case BattleState.CheckBattleCharacterDown2:
                    UpdateBattleState_CheckBattleCharacterDown2();
                    break;
                case BattleState.FadeMonsterImage2:
                    UpdateBattleState_FadeMonsterImage2();
                    break;
                case BattleState.BattleFinishCheck2:
                    UpdateBattleState_BattleFinishCheck2();
                    break;
                case BattleState.StartBattleFinishEvent:
                    UpdateBattleState_StartBattleFinishEvent();
                    break;
                case BattleState.ProcessBattleFinish:
                    UpdateBattleState_ProcessBattleFinish();
                    break;
                case BattleState.ResultInit:
                    UpdateBattleState_ResultInit();
                    break;
                case BattleState.Result:
                    UpdateBattleState_Result();
                    break;
                case BattleState.PlayerChallengeEscape:
                    UpdateBattleState_PlayerChallengeEscape();
                    break;
                case BattleState.PlayerEscapeSuccess:
                    UpdateBattleState_PlayerEscapeSuccess();
                    break;
                case BattleState.StopByEvent:
                    UpdateBattleState_StopByEvent();
                    break;
                case BattleState.PlayerEscapeFail:
                    UpdateBattleState_PlayerEscapeFail();
                    break;
                case BattleState.MonsterEscape:
                    UpdateBattleState_MonsterEscape();
                    break;
                case BattleState.SetFinishEffect:
                    UpdateBattleState_SetFinishEffect();
                    break;
                case BattleState.FinishFadeOut:
                    UpdateBattleState_FinishFadeOut();
                    break;
                case BattleState.FinishFadeIn:
                    UpdateBattleState_FinishFadeIn();
                    break;
            }
        }

        private void UpdateBattleState_StartFlash()
        {
            fadeScreenColorTweener.Update();

            if (!fadeScreenColorTweener.IsPlayTween)
            {
                if (catalog.getItemFromGuid(gameSettings.transitionBattleEnter) == null)
                    fadeScreenColorTweener.Begin(new Color(Color.Black, 0), new Color(Color.Black, 255), 30);
                else
                    owner.mapScene.SetWipe(gameSettings.transitionBattleEnter);
                ChangeBattleState(BattleState.StartFadeOut);
            }
        }

        private void UpdateBattleState_StartFadeOut()
        {
            if (catalog.getItemFromGuid(gameSettings.transitionBattleEnter) == null)
                fadeScreenColorTweener.Update();

            if (!fadeScreenColorTweener.IsPlayTween && !owner.mapScene.IsWiping())
            {
                owner.mapScene.SetWipe(Guid.Empty);
                fadeScreenColorTweener.Begin(new Color(Color.Black, 255), new Color(Color.Black, 0), 30);

                openingBackgroundImageScaleTweener.Clear();
                openingBackgroundImageScaleTweener.Add(1.2f, 1.0f, 30);
                openingBackgroundImageScaleTweener.Begin();

                battleViewer.openingMonsterScaleTweener.Begin(1.2f, 1.0f, 30);
                battleViewer.openingColorTweener.Begin(new Color(Color.White, 0), new Color(Color.White, 255), 30);

                LoadBattleSceneImpl();
                StartBattle();

                battleEvents.start(Rom.Script.Trigger.BATTLE_START);
                battleEvents.start(Rom.Script.Trigger.BATTLE_PARALLEL);

                IsDrawingBattleScene = true;
                ((BattleViewer3D)battleViewer).Show();

                ChangeBattleState(BattleState.StartFadeIn);

                // #13413 暗転をカットできる可能性があるので、バトルイベントを先行で1フレーム回してみる
                // #13413 There is a possibility of cutting the blackout, so try playing the battle event one frame in advance.
                battleEvents.update();
            }
        }

        private void StartBattle()
        {
            battleViewer.ClearDisplayMessage();
            if (!string.IsNullOrEmpty(battleStartWord) && enemyData.Count > 0)
            {
                battleViewer.SetDisplayMessage(string.Format(battleStartWord, enemyData[0].Name));
                battleViewer.OpenWindow(WindowType.MessageWindow);
                elapsedTimeForBattleStart = 0f;
            }
        }

        private void UpdateBattleState_StartFadeIn()
        {
            fadeScreenColorTweener.Update();

            openingBackgroundImageScaleTweener.Update();

            if (!fadeScreenColorTweener.IsPlayTween)
            {
                ChangeBattleState(BattleState.BattleSetting);
            }
        }

        private void UpdateBattleState_BattleSetting()
        {
            if (battleViewer.IsVisibleWindow(WindowType.MessageWindow))
            {
                elapsedTimeForBattleStart += GameMain.getRelativeParam60FPS();
                if (elapsedTimeForBattleStart > TIME_FOR_BATTLE_START_MESSEGE)
                {
                    battleViewer.OpenWindow(WindowType.None);
                }
                return;
            }



            ChangeBattleState(BattleState.BattleStart);
        }

        private void UpdateBattleState_BattleStart()
        {
            if (owner.mapScene.isToastVisible() || !isReady3DCamera())
            {
                return;
            }

            (battleViewer as BattleViewer3D)?.restoreCamera();
            battleEvents.start(Rom.Script.Trigger.BATTLE_TURN);
            totalTurn = 1;
            ChangeBattleState(BattleState.Wait);

            var firstAttackRate = party.CalcConditionFirstAttackRate();

            if ((0 < firstAttackRate) && (battleRandom.Next(100) < firstAttackRate))
            {
                firstAttackType = FirstAttackType.Player;

                if (!string.IsNullOrEmpty(gameSettings.glossary.battle_player_first_attack))
                {
                    owner.mapScene.ShowToast(gameSettings.glossary.battle_player_first_attack);
                }
            }
            else if ((firstAttackRate < 0) && (battleRandom.Next(100) < -firstAttackRate))
            {
                firstAttackType = FirstAttackType.Monster;

                if (!string.IsNullOrEmpty(gameSettings.glossary.battle_monster_first_attack))
                {
                    owner.mapScene.ShowToast(gameSettings.glossary.battle_monster_first_attack);
                }
            }
            else
            {
                firstAttackType = FirstAttackType.None;
            }
        }

        private void UpdateBattleState_Wait()
        {
            if (owner.mapScene.isToastVisible())
            {
                return;
            }

            if (isReady3DCamera() && !battleEvents.isBusy())
            {
                var battleResult = CheckBattleFinish();

                if (battleResult == BattleResultState.NonFinish)
                {
                    switch (firstAttackType)
                    {
                        case FirstAttackType.Player:// 先制した / took the lead
                            firstAttackType = FirstAttackType.None;

                            foreach (var player in playerData)
                            {
                                ((ExBattlePlayerData)player).turnGauge = 1;
                            }

                            //ChangeBattleState(BattleState.PlayerTurnStart);
                            break;
                        case FirstAttackType.Monster:// 先制された / preempted
                            firstAttackType = FirstAttackType.None;

                            foreach (var monsterData in enemyData)
                            {
                                ((ExBattleEnemyData)monsterData).turnGauge = 1;
                            }

                            //SkipPlayerBattleCommand();
                            break;
                        case FirstAttackType.None:
                        default:
                            ChangeBattleState(BattleState.LockInEquipment);
                            break;
                    }
                }
                else
                {
                    battleEvents.setBattleResult(battleResult);
                    ChangeBattleState(BattleState.StartBattleFinishEvent);
                }
            }
        }

        private void UpdateBattleState_LockInEquipment()
        {
            // 外された装備を袋に仕舞う
            // Put away the removed equipment in the bag
            if (!owner.mapScene.IsTrashVisible())
            {
                var isShowTrashWindow = false;

                foreach (var player in playerData)
                {
                    var hero = player.player;

                    while (hero.releaseEquipments.Count > 0)
                    {
                        var equipment = hero.releaseEquipments[0];

                        hero.releaseEquipments.RemoveAt(0);

                        if ((party.GetItemNum(equipment.guId) == 0) && (party.checkInventoryEmptyNum() <= 0))
                        {
                            owner.mapScene.ShowTrashWindow(equipment.guId, 1);

                            isShowTrashWindow = true;

                            break;
                        }
                        else
                        {
                            party.AddItem(equipment.guId, 1);
                        }
                    }

                    if (isShowTrashWindow)
                    {
                        break;
                    }
                }

                if (!isShowTrashWindow)
                {
                    activeCharacter = null;
                    ChangeBattleState(BattleState.PlayerTurnStart);
                }
            }
        }



        private void UpdateBattleState_PlayerTurnStart()
        {
            if (battleEvents.isBusy())
                return;

            recoveryStatusInfo.Clear();

            if (activeCharacter != null)
            {
                StatusUpdateImpl(activeCharacter);
            }
            else
            {
                foreach (var character in createBattleCharacterList())
                {
                    StatusUpdateImpl(character);
                }
            }

            ChangeBattleState(BattleState.CheckTurnRecoveryStatus);
        }

        private void StatusUpdateImpl(BattleCharacterBase battleCharacter)
        {
            // 1回目のコマンド選択時のみ 状態異常回復判定 & 強化用ステータス減衰
            // Only when the command is selected for the first time Status abnormality recovery judgment \u0026 strengthening status attenuation
            var character = battleCharacter;
            if (character != null)
            //foreach (var character in createBattleCharacterList())
            {
                // 状態異常 回復判定
                // Status Abnormal Recovery Judgment
                var recoveryList = new List<Hero.ConditionInfo>(character.conditionInfoDic.Count);

                foreach (var e in character.conditionInfoDic)
                {
                    var info = e.Value;

                    if ((info.recovery & Hero.ConditionInfo.RecoveryType.Probability) != 0)
                    {
                        if (battleRandom.Next(100) < info.probabilityRate)
                        {
                            recoveryList.Add(info);

                            continue;
                        }
                    }

                    if ((info.recovery & Hero.ConditionInfo.RecoveryType.Turn) != 0)
                    {
                        info.turnCount--;

                        if (info.turnCount <= 0)
                        {
                            recoveryList.Add(info);

                            continue;
                        }
                    }
                }

                foreach (var info in recoveryList)
                {
                    character.RecoveryCondition(info.condition);

                    if (info.rom != null)
                    {
                        recoveryStatusInfo.Add(new RecoveryStatusInfo(character, info.rom));
                    }
                }
            }

            var player = battleCharacter as ExBattlePlayerData;
            if (player != null)
            //foreach (var player in playerData)
            {
                UpdateEnhanceEffect(player.attackEnhanceEffects);
                UpdateEnhanceEffect(player.guardEnhanceEffects);

                // 強化用のステータスを減衰させる
                // Attenuates stats for enhancement
                const float DampingRate = 0.8f;
                player.MaxHitPointEnhance = (int)(player.MaxHitPointEnhance * DampingRate);
                player.MaxMagicPointEnhance = (int)(player.MaxMagicPointEnhance * DampingRate);
                player.PowerEnhancement = (int)(player.PowerEnhancement * DampingRate);
                player.VitalityEnhancement = (int)(player.VitalityEnhancement * DampingRate);
                player.MagicEnhancement = (int)(player.MagicEnhancement * DampingRate);
                player.SpeedEnhancement = (int)(player.SpeedEnhancement * DampingRate);
                player.EvasionEnhancement = (int)(player.EvasionEnhancement * DampingRate);
                player.DexterityEnhancement = (int)(player.DexterityEnhancement * DampingRate);

                player.enhanceStatusValue.MulStatusAll(DampingRate);

                foreach (var element in player.ResistanceAttackAttributeEnhance.Keys.ToArray())
                {
                    player.ResistanceAttackAttributeEnhance[element] = (int)(player.ResistanceAttackAttributeEnhance[element] * DampingRate);
                }

                foreach (var element in player.ResistanceAilmentEnhance.Keys.ToArray())
                {
                    player.ResistanceAilmentEnhance[element] = (int)(player.ResistanceAilmentEnhance[element] * DampingRate);
                }

                SetBattleStatusData(player);

                battleViewer.SetPlayerStatusEffect(player);
            }

            var monster = battleCharacter as ExBattlePlayerData;
            if (monster != null)
            //foreach (var monster in enemyData)
            {
                // 強化用のステータスを減衰させる
                // Attenuates stats for enhancement
                const float DampingRate = 0.8f;
                monster.MaxHitPointEnhance = (int)(monster.MaxHitPointEnhance * DampingRate);
                monster.MaxMagicPointEnhance = (int)(monster.MaxMagicPointEnhance * DampingRate);
                monster.PowerEnhancement = (int)(monster.PowerEnhancement * DampingRate);
                monster.VitalityEnhancement = (int)(monster.VitalityEnhancement * DampingRate);
                monster.MagicEnhancement = (int)(monster.MagicEnhancement * DampingRate);
                monster.SpeedEnhancement = (int)(monster.SpeedEnhancement * DampingRate);
                monster.EvasionEnhancement = (int)(monster.EvasionEnhancement * DampingRate);
                monster.DexterityEnhancement = (int)(monster.DexterityEnhancement * DampingRate);

                monster.enhanceStatusValue.MulStatusAll(DampingRate);

                foreach (var element in monster.ResistanceAttackAttributeEnhance.Keys.ToArray())
                {
                    monster.ResistanceAttackAttributeEnhance[element] = (int)(monster.ResistanceAttackAttributeEnhance[element] * DampingRate);
                }

                foreach (var element in monster.ResistanceAilmentEnhance.Keys.ToArray())
                {
                    monster.ResistanceAilmentEnhance[element] = (int)(monster.ResistanceAilmentEnhance[element] * DampingRate);
                }
            }
            UpdatePosition();
            ChangeBattleState(BattleState.CheckTurnRecoveryStatus);
        }

        private void UpdateBattleState_CheckTurnRecoveryStatus()
        {
            if (battleEvents.isBusy())
                return;

            if (recoveryStatusInfo.Count == 0)
            {
                if (activeCharacter != null)
                {
                    if (commandSelectPlayer != null)
                        ChangeBattleState(BattleState.CheckCommandSelect);
                    else
                        ChangeBattleState(BattleState.SetEnemyBattleCommand);
                }
                else
                {
                    ChangeBattleState(WAIT_CTB_GAUGE);
                }
                //ChangeBattleState(BattleState.SetEnemyBattleCommand);
            }
            else
            {
                ((BattleViewer3D)battleViewer).RecoveryConditionMotion(recoveryStatusInfo[0]);
                battleViewer.SetDisplayMessage(recoveryStatusInfo[0].GetMessage(gameSettings));

                ChangeBattleState(BattleState.DisplayTurnRecoveryStatus);
            }
        }

        private void UpdateBattleState_DisplayTurnRecoveryStatus()
        {
            if (battleStateFrameCount >= 30 || Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE, Input.GameState.MENU))
            {
                recoveryStatusInfo.RemoveAt(0);

                ChangeBattleState(BattleState.CheckTurnRecoveryStatus);
            }
        }

        private void UpdateBattleState_SelectActivePlayer()
        {
            if (commandSelectedMemberCount >= playerData.Count)
            {
                commandSelectPlayer = null;

                commandSelectedMemberCount = 0;

                ChangeBattleState(BattleState.SortBattleActions);
            }
            else
            {
                commandSelectPlayer = playerData[commandSelectedMemberCount];

                commandSelectPlayer.commandSelectedCount++;

                ChangeBattleState(BattleState.CheckCommandSelect);
            }
        }

        private void UpdateBattleState_CheckCommandSelect()
        {
            // 現在の状態異常に合わせて行動させる
            // Act according to the current abnormal state
            if (commandSelectPlayer.IsDeadCondition())
            {
                commandSelectPlayer.selectedBattleCommandType = BattleCommandType.Nothing_Down;

                ChangeBattleState(BattleState.SetPlayerBattleCommandTarget);
            }
            else
            {
                var setConditionBattleCommandType = false;

                foreach (var e in commandSelectPlayer.conditionInfoDic)
                {
                    var condition = e.Value.rom;

                    if (condition != null)
                    {
                        if (condition.actionDisabled)
                        {
                            commandSelectPlayer.selectedBattleCommandType = BattleCommandType.Nothing;

                            setConditionBattleCommandType = true;
                            break;
                        }
                        else if (condition.attack)
                        {
                            commandSelectPlayer.selectedBattleCommandType = BattleCommandType.Attack;

                            setConditionBattleCommandType = true;

                            // 行動不能が優先なので、ここでは終わらない
                            // Incapacity is the priority, so it doesn't end here
                        }
                    }
                }

                if (setConditionBattleCommandType)
                {
                    ChangeBattleState(BattleState.SetPlayerBattleCommandTarget);
                }
                else
                {
                    if (!commandSelectPlayer.IsCommandSelectable)
                    {
                        // #13124 で修正 バトルイベントでコマンドをセットした場合に対応
                        // Fixed in #13124 Compatible with setting commands in battle events.
                        ChangeBattleState(BattleState.SetPlayerBattleCommandTarget);
                        commandSelectPlayer.forceSetCommand = false;

                        // #13124 でコメントアウト
                        // Comment out with #13124
                        //ResetCtbGauge(activeCharacter);
                        //ChangeBattleState(WAIT_CTB_GAUGE);

                        // 元の処理
                        // original processing
                        //commandSelectedMemberCount++;
                        //ChangeBattleState(BattleState.SelectActivePlayer);
                    }
                    else
                    {
                        Audio.PlaySound(owner.se.menuOpen);
                        ChangeBattleState(BattleState.SetPlayerBattleCommand);
                    }
                }
            }
        }

        private void UpdateBattleState_SetPlayerBattleCommand()
        {
            if (battleEvents.isBusy())
                return;
            battleEvents.clearCurrentProcessingTrigger();

            playerBattleCommand.Clear();
            battleViewer.battleCommandChoiceWindowDrawer.ClearChoiceListData();

            // 戦闘用コマンドの登録
            // Registering Combat Commands


            {
                var enableAttack = false;

                foreach (var enemy in enemyData)
                {
                    if ((enemy.HitPoint > 0) && IsHitRange(commandSelectPlayer, enemy))
                    {
                        enableAttack = true;

                        break;
                    }
                }

                foreach (var guid in commandSelectPlayer.player.rom.battleCommandList)
                {
                    var command = (Rom.BattleCommand)catalog.getItemFromGuid(guid);

                    if (command.type == BattleCommand.CommandType.NONE)
                    {
                        continue;
                    }

                    playerBattleCommand.Add(command);

                    var enable = true;

                    switch (command.type)
                    {
                        case BattleCommand.CommandType.ATTACK:
                            enable = enableAttack;
                            break;
                        case BattleCommand.CommandType.POSITION:
                            enable = targetPlayerData.Count > 1;
                            break;
                        default:
                            break;
                    }

                    if (iconTable.ContainsKey(command.icon.guId))
                    {
                        battleViewer.battleCommandChoiceWindowDrawer.AddChoiceData(iconTable[command.icon.guId], command.icon, command.name, enable);
                    }
                    else
                    {
                        battleViewer.battleCommandChoiceWindowDrawer.AddChoiceData(command.name, enable);
                    }
                }
            }

            //bool isFirstCommandSelectPlayer = playerData.Any(player => player.IsCommandSelectable) && commandSelectedMemberCount == playerData.IndexOf(playerData.Find(player => player.IsCommandSelectable));

            // 1番目にコマンドを選択できるメンバーだけ「逃げる」コマンドを追加する
            // Add the \
            // それ以外のメンバーには「戻る」コマンドを追加する
            // Add \
            //if (isFirstCommandSelectPlayer)
            {
                // 「逃げる」コマンドの登録
                // Registering the \
                playerBattleCommand.Add(new BattleCommand() { type = BattleCommand.CommandType.ESCAPE });

                battleViewer.battleCommandChoiceWindowDrawer.AddChoiceData(battleViewer.battleCommandChoiceWindowDrawer.escapeImageId, battleViewer.battleCommandChoiceWindowDrawer.escapeIcon, gameSettings.glossary.battle_escape_command, escapeAvailable);
                //battleViewer.battleCommandChoiceWindowDrawer.AddChoiceData(battleViewer.battleCommandChoiceWindowDrawer.escapeImageId, gameSettings.glossary.battle_escape_command, escapeAvailable);
            }
            //else
            //{
            //    // 「戻る」コマンドの登録
            // // register \
            //    playerBattleCommand.Add(new BattleCommand() { type = BattleCommand.CommandType.BACK });

            //    //battleViewer.battleCommandChoiceWindowDrawer.AddChoiceData(iconTable[gameSettings.returnIcon.guId], gameSettings.returnIcon, gameSettings.glossary.battle_back);
            //    battleViewer.battleCommandChoiceWindowDrawer.AddChoiceData(battleViewer.battleCommandChoiceWindowDrawer.returnImageId, gameSettings.glossary.battle_back);
            //}

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
                battleEvents.start(Rom.Script.Trigger.BATTLE_BEFORE_COMMAND_SELECT);
                ChangeBattleState(BattleState.WaitEventsBeforeCommandSelect);
            }
            else
            {
                commandSelectPlayer.selectedBattleCommandType = BattleCommandType.Nothing;
                ChangeBattleState(BattleState.SetPlayerBattleCommandTarget);
            }
        }

        private void UpdateBattleState_WaitEventsBeforeCommandSelect()
        {
            if (!battleEvents.isBusy())
            {
                battleEvents.clearCurrentProcessingTrigger();

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
        }

        private void UpdateBattleState_SelectPlayerBattleCommand()
        {
            bool isChange = false;

            if (battleCommandState == SelectBattleCommandState.CommandEnd && commandSelectPlayer.selectedBattleCommandType != BattleCommandType.Back)
            {
                isChange = true;

                battleCommandState = SelectBattleCommandState.None;

                if (commandSelectPlayer.selectedBattleCommandType == BattleCommandType.PlayerEscape)
                {
                    battleEvents.start(Rom.Script.Trigger.BATTLE_AFTER_COMMAND_SELECT);
                    SkipPlayerBattleCommand(true);
                }
                else
                {
                    ChangeBattleState(BattleState.SetPlayerBattleCommandTarget);
                }
            }

            // 自分のコマンド選択をキャンセルしてひとつ前の人のコマンド選択に戻る
            // Cancel your own command selection and return to the previous person's command selection
            if (battleCommandState == SelectBattleCommandState.CommandCancel || (battleCommandState == SelectBattleCommandState.CommandEnd && commandSelectPlayer.selectedBattleCommandType == BattleCommandType.Back))
            {
                bool isBackCommandSelect = false;
                int prevPlayerIndex = 0;

                // 戻れる？
                // Can you go back?
                isBackCommandSelect = true;// バトルプラグインではキャンセルイベント発動の条件を合わせるため、常にキャンセル可能にする / In the battle plugin, it is always possible to cancel in order to match the conditions for triggering a cancellation event.
                //for (int index = commandSelectedMemberCount - 1; index >= 0; index--)
                //{
                //    if (index < playerData.Count && playerData[index].IsCommandSelectable)
                //    {
                //        isBackCommandSelect = true;
                //        prevPlayerIndex = index;
                //        break;
                //    }
                //}

                // 戻る
                // return
                if (isBackCommandSelect)
                {
                    isChange = true;
                    battleEvents.start(Rom.Script.Trigger.BATTLE_CANCEL_COMMAND_SELECT);
                    //commandSelectedMemberCount = prevPlayerIndex;
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

                //commandSelectPlayer = playerData[commandSelectedMemberCount];

                battleViewer.CloseWindow();
            }
        }

        private void UpdateBattleState_SetPlayerBattleCommandTarget()
        {
            commandSelectPlayer.targetCharacter = GetTargetCharacters(commandSelectPlayer);

            commandSelectedMemberCount++;

            // エフェクト先読み
            // Effect lookahead
            GetCommandEffectImpl(commandSelectPlayer, catalog, out var efA, out var efB);
            preloadEffect(efA);
            preloadEffect(efB);

            battleEvents.start(Rom.Script.Trigger.BATTLE_AFTER_COMMAND_SELECT);
            commandSelectPlayer = null;
            ChangeBattleState(BattleState.ReadyExecuteCommand);
        }

        private class EffectPreloadJob : SharpKmyBase.Job
        {
            private static Dictionary<Guid, SharpKmyGfx.ParticleInstance> preloadTargets = new Dictionary<Guid, SharpKmyGfx.ParticleInstance>();
            private Guid guid;
            private Catalog catalog;

            public EffectPreloadJob(Guid guid, Catalog catalog)
            {
                this.guid = guid;
                this.catalog = catalog;
            }

            public override void func()
            {
                var pt = catalog.getItemFromGuid(guid) as Resource.Particle;
                while (pt != null)
                {
                    if (preloadTargets.ContainsKey(guid))
                        break;

                    var inst = new SharpKmyGfx.ParticleInstance(pt.path, 0);
                    pt = catalog.getItemFromGuid(pt.BattleSettingChainTarget) as Resource.Particle;
                    preloadTargets.Add(guid, inst);
                }
            }

            public static void clearPreloads()
            {
                foreach(var inst in preloadTargets.Values)
                {
                    inst.Release();
                }
                preloadTargets.Clear();
            }
        }

        private void preloadEffect(Guid guid)
        {
            var job = new EffectPreloadJob(guid, catalog);
            SharpKmyBase.Job.addJob(job);
        }

        private void UpdateBattleState_SortBattleActions()
        {
            if (battleEvents.isBusy())
                return;
            battleEvents.clearCurrentProcessingTrigger();

            commandExecuteMemberCount = 0;

            // 行動順を決定する
            // determine the order of action
            // 優先順位
            // Priority
            // 1.「逃げる」コマンドを選択したキャラクター
            // 1. Character who selected the \
            // 2.「防御」コマンドを選択したキャラクター : 「防御」コマンドを選択したキャラクターが複数いた場合はキャラクターIDの小さい順に行動
            // 2. Characters who selected the \
            // 3.それ以外のコマンドを選択したキャラクター : 素早さのステータスが高い順に行動
            // 3. Characters who chose other commands: Act in order of high speed status
            // IEnumerable 型だと遅延評価により参照時に計算されるので1ターンの間に素早さのステータスが変わると同じキャラクターが2回行動したり順番がスキップされる現象が発生するのでList型で順番を固定する
            // If it is IEnumerable type, it is calculated at the time of reference by lazy evaluation, so if the speed status changes during one turn, the same character will act twice or the order will be skipped, so the order is fixed with List type
            var characters = createBattleCharacterList().Where(character => character.selectedBattleCommandType != BattleCommandType.Nothing_Down);
            battleEntryCharacters.Clear();
            battleEntryCharacters.AddRange(characters.Where(character => character.selectedBattleCommandType == BattleCommandType.PlayerEscape));
            battleEntryCharacters.AddRange(characters.Where(character => character.selectedBattleCommandType == BattleCommandType.MonsterEscape));
            battleEntryCharacters.AddRange(characters.Where(character => character.selectedBattleCommandType == BattleCommandType.Guard).OrderBy(character => character.UniqueID));
            battleEntryCharacters.AddRange(characters.Where(character => !battleEntryCharacters.Contains(character)).OrderByDescending(character => character.Speed));

            ChangeBattleState(BattleState.ReadyExecuteCommand);
        }

        /// <summary>
        /// 行動ゲージが貯まるのを待つ
        /// Wait for the action gauge to accumulate
        /// </summary>
        private void UpdateBattleState_WaitCtbGauge()
        {
            var limited = playerData.Select(x => { return new { data = (BattleCharacterBase)x, turn = ((ExBattlePlayerData)x).turnGauge }; })
                    .Concat(enemyData.Select(x => { return new { data = (BattleCharacterBase)x, turn = ((ExBattleEnemyData)x).turnGauge }; }))
                    .OrderByDescending(x => x.turn)
                    .FirstOrDefault(x => x.turn >= 1)?.data;

            if (limited is ExBattlePlayerData)
            {
                activeCharacter = limited;
                commandSelectPlayer = (ExBattlePlayerData)limited;
                ChangeBattleState(BattleState.PlayerTurnStart);
                battleEvents.start(Rom.Script.Trigger.BATTLE_TURN);
            }
            else if(limited is ExBattleEnemyData)
            {
                activeCharacter = limited;
                commandSelectPlayer = null;
                ChangeBattleState(BattleState.PlayerTurnStart);
                battleEvents.start(Rom.Script.Trigger.BATTLE_TURN);
            }
            else
            {
                // 一番素早いキャラでもゲージが貯まるまで10フレームかかるよう係数を作る
                // Create a coefficient so that even the fastest character takes 10 frames to fill the gauge
                float speedCoeff = Math.Max(1000, playerData.Cast<BattleCharacterBase>().Concat(enemyData.Cast<BattleCharacterBase>()).Max(x => x.SpeedPlusOne()) * 10);

                foreach (ExBattlePlayerData player in playerData)
                {
                    if (!player.IsDeadCondition())
                        player.turnGauge += player.SpeedPlusOne() / speedCoeff;
                }
                foreach (ExBattleEnemyData enemy in enemyData)
                { 
                    if (!enemy.IsDeadCondition())
                        enemy.turnGauge += enemy.SpeedPlusOne() / speedCoeff;
                }
            }
        }

        /// <summary>
        /// 指定した順目でターンが回ってくるキャラを取得する
        /// Get a character whose turn comes around in the specified order
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        internal BattleCharacterBase GetTurnOrdererdCharacter(int index)
        {
            var range = Enumerable.Range(1, index + 1);

            // indexターン先までを配列化する
            // Array up to index turn destination
            return range.SelectMany(x => playerData.Select(y => { return new { data = (BattleCharacterBase)y, turn = ((ExBattlePlayerData)y).GetNormalizedTurn(x) }; }))
                .Concat(range.SelectMany(x => enemyData.Select(y => { return new { data = (BattleCharacterBase)y, turn = ((ExBattleEnemyData)y).GetNormalizedTurn(x) }; })))
                .OrderBy(x => x.turn)
                .ElementAtOrDefault(index)?.data;
        }

        private void UpdateBattleState_SetEnemyBattleCommand()
        {
            if (owner.mapScene.isToastVisible() || !battleViewer.IsEffectEndPlay)
            {
                return;
            }

            SelectEnemyCommand(activeCharacter as BattleEnemyData, false, false);
            ((BattleViewer3D)battleViewer).setEnemyActionReady(activeCharacter as BattleEnemyData);
            //foreach (var monsterData in enemyData)
            //{
            //    SelectEnemyCommand(monsterData, false, false);
            //    ((BattleViewer3D)battleViewer).setEnemyActionReady(monsterData);
            //}

            // エフェクト先読み
            // Effect lookahead
            GetCommandEffectImpl(activeCharacter, catalog, out var efA, out var efB);
            preloadEffect(efA);
            preloadEffect(efB);
            ChangeBattleState(BattleState.ReadyExecuteCommand);
            //ChangeBattleState(BattleState.SelectActivePlayer);
        }

        /// <summary>
        /// 敵のコマンドを選択する
        /// Select Enemy Command
        /// </summary>
        /// <param name="monsterData">コマンドを決定する敵</param>
        /// <param name="monsterData">Enemy to determine command</param>
        /// <param name="isContinous">連続行動かどうか</param>
        /// <param name="isContinous">continuous action or not</param>
        /// <param name="isCounter">カウンターかどうか</param>
        /// <param name="isCounter">counter or not</param>
        private void SelectEnemyCommand(BattleEnemyData monsterData, bool isContinous, bool isCounter,
            int turn = -1, IEnumerable<Rom.ActionInfo> activeAction = null)
        {
            monsterData.counterAction = BattleEnemyData.CounterState.NONE;

            if (monsterData.selectedBattleCommandType != BattleCommandType.Undecided)
            {
                // イベントパネルから行動が強制指定されている場合にここに来る
                // Comes here if an action is forced from the event panel
            }
            else if (monsterData.HitPoint > 0)
            {
                if (!isCounter && !isContinous)
                {
                    // 強化能力 更新
                    // Enhanced Ability Update
                    UpdateEnhanceEffect(monsterData.attackEnhanceEffects);
                    UpdateEnhanceEffect(monsterData.guardEnhanceEffects);
                }

                if (activeAction == null)
                    activeAction = GetActiveActions(monsterData, isCounter, turn);

                if (activeAction.Count() > 0)
                {
                    var executeAction = activeAction.ElementAt(battleRandom.Next(activeAction.Count()));

                    foreach (var action in activeAction)
                    {
                        if (action == executeAction)
                            continue;

                        if (action.type == Rom.ActionType.SKILL)
                            GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, monsterData.Name,
                                string.Format("Selectable Action / Skill : {0}", catalog.getItemFromGuid(action.refByAction)?.name ?? "Nothing"));
                        else
                            GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, monsterData.Name,
                                string.Format("Selectable Action / Type : {0}", action.type.ToString()));
                    }

                    if (executeAction.type == Rom.ActionType.SKILL)
                        GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, monsterData.Name,
                            string.Format("Choiced Action / Skill : {0}", catalog.getItemFromGuid(executeAction.refByAction)?.name ?? "Nothing"));
                    else
                        GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, monsterData.Name,
                            string.Format("Choiced Action / Type : {0}", executeAction.type.ToString()));

                    monsterData.commandTargetList.Clear();
                    battleViewer.commandTargetSelector.Clear();

                    monsterData.continuousAction = executeAction.continuous;
                    if (!monsterData.continuousAction)
                        monsterData.alreadyExecuteActions.Clear();

                    // 同じ行は2回まで
                    // Same line up to 2 times
                    if (monsterData.alreadyExecuteActions.Contains(executeAction))
                    {
                        monsterData.continuousAction = false;
                        GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, monsterData.Name,
                            string.Format("Cancel Action By Already Executed.", executeAction.type.ToString()));
                    }
                    else
                    {
                        monsterData.alreadyExecuteActions.Add(executeAction);
                    }

                    if (!isCounter && !isContinous)
                        monsterData.currentActionTurn = monsterData.ExecuteCommandTurnCount;

                    switch (executeAction.type)
                    {
                        case Rom.ActionType.ATTACK:
                        case Rom.ActionType.CRITICAL:
                        case Rom.ActionType.FORCE_CRITICAL:
                            foreach (var player in targetPlayerData.Where(target => (target.HitPoint > 0) && IsHitRange(monsterData, target)))
                            {
                                monsterData.commandTargetList.Add(player);
                            }

                            if (monsterData.commandTargetList.Count == 0)
                            {
                                goto case Rom.ActionType.DO_NOTHING;
                            }

                            switch (executeAction.type)
                            {
                                case Rom.ActionType.ATTACK:
                                    monsterData.selectedBattleCommandType = BattleCommandType.Attack;
                                    break;
                                case Rom.ActionType.CRITICAL:
                                    monsterData.selectedBattleCommandType = BattleCommandType.Critical;
                                    break;
                                case Rom.ActionType.FORCE_CRITICAL:
                                    monsterData.selectedBattleCommandType = BattleCommandType.ForceCritical;
                                    break;
                                default:
                                    break;
                            }
                            break;

                        case Rom.ActionType.SKILL:
                            var skill = (Rom.NSkill)catalog.getItemFromGuid(executeAction.refByAction);

                            // スキルが無かったら何もしないよう修正
                            // Fixed to do nothing if there is no skill
                            if (skill == null)
                            {
                                GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, monsterData.Name,
                                    string.Format("Cancel Action By Missing Skill.", executeAction.type.ToString()));
                                monsterData.selectedBattleCommandType = BattleCommandType.Nothing;
                                break;
                            }

                            monsterData.selectedSkill = skill;
                            monsterData.selectedBattleCommandType = BattleCommandType.Skill;

                            switch (monsterData.selectedSkill.option.target)
                            {
                                case Rom.TargetType.PARTY_ONE:
                                case Rom.TargetType.PARTY_ONE_ENEMY_ALL:

                                    foreach (var enemy in targetEnemyData)
                                    {
                                        monsterData.commandTargetList.Add(enemy);
                                    }

                                    break;

                                case Rom.TargetType.ENEMY_ONE:
                                case Rom.TargetType.PARTY_ALL_ENEMY_ONE:
                                case Rom.TargetType.SELF_ENEMY_ONE:
                                case Rom.TargetType.OTHERS_ENEMY_ONE:
                                    foreach (var player in targetPlayerData.Where(player => player.HitPoint > 0))
                                    {
                                        monsterData.commandTargetList.Add(player);
                                    }

                                    break;
                            }
                            break;

                        case Rom.ActionType.CHARGE:
                            monsterData.selectedBattleCommand = new BattleCommand();
                            monsterData.selectedBattleCommand.power = executeAction.option;
                            monsterData.selectedBattleCommandType = BattleCommandType.Charge;
                            break;

                        case Rom.ActionType.GUARD:
                            monsterData.selectedBattleCommand = new BattleCommand();
                            monsterData.selectedBattleCommand.power = executeAction.option;
                            monsterData.selectedBattleCommandType = BattleCommandType.Guard;
                            break;

                        case Rom.ActionType.ESCAPE:
                            monsterData.selectedBattleCommandType = BattleCommandType.MonsterEscape;
                            break;

                        case Rom.ActionType.DO_NOTHING:
                            monsterData.selectedBattleCommandType = BattleCommandType.Nothing;
                            break;
                    }

                    if (monsterData.commandTargetList.Count > 0)
                    {
                        battleViewer.commandTargetSelector.AddBattleCharacters(monsterData.commandTargetList);
                        var result = TargetSelectWithHateRate(monsterData.commandTargetList, monsterData);
                        battleViewer.commandTargetSelector.SetSelect(result);
                    }
                }
                else
                {
                    GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, monsterData.Name, "No Selectable Action");

                    monsterData.continuousAction = false;
                    monsterData.selectedBattleCommandType = BattleCommandType.Nothing;
                }

                monsterData.targetCharacter = GetTargetCharacters(monsterData);

                // ターン指定でなければモンスターのアクションを一つ進める
                // Advance the monster's action by one if it is not a turn designation
                if (turn < 0)
                {
                    monsterData.ExecuteCommandTurnCount++;

                    if (monsterData.monster.battleActions.Count == 0 || monsterData.ExecuteCommandTurnCount > monsterData.monster.battleActions.Max(act => act.turn))
                    {
                        monsterData.ExecuteCommandTurnCount = 1;
                    }
                }
            }
            else
            {
                monsterData.continuousAction = false;
                monsterData.selectedBattleCommandType = BattleCommandType.Nothing_Down;
            }
        }

        /// <summary>
        /// モンスターの現在のターンで有効なアクションの一覧を取得する
        /// Get a list of valid actions for the monster's current turn
        /// </summary>
        /// <param name="monsterData"></param>
        /// <param name="isContinous"></param>
        /// <param name="isCounter"></param>
        /// <returns></returns>
        private IEnumerable<Rom.ActionInfo> GetActiveActions(BattleEnemyData monsterData, bool isCounter, int turn = -1)
        {
            int hitPointRate = (int)(monsterData.HitPointPercent * 100);
            int magicPointRate = (int)(monsterData.MagicPointPercent * 100);

            turn = turn > 0 ? turn : monsterData.ExecuteCommandTurnCount;

            if (isCounter)
                GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, monsterData.Name,
                    string.Format("Select Counter Action / Turn No. : {0}", turn));
            else
                GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, monsterData.Name,
                    string.Format("Select Action / Turn No. : {0}", turn));

            var actionList = monsterData.monster.battleActions;
            var activeAction = actionList.Where(act => act.turn == turn
                && checkCondition(act, monsterData, hitPointRate, magicPointRate, isCounter));
            var removeActions = new List<Rom.ActionInfo>();

            // MPが足りないスキルを除外するための関数
            // Function for excluding skills with insufficient MP
            void filter()
            {
                switch (monsterData.monster.aiPattern)
                {
                    case Rom.AIPattern.CLEVER:
                    case Rom.AIPattern.TRICKY:
                        foreach (var act in activeAction)
                        {
                            if (act.type == Rom.ActionType.SKILL)
                            {
                                var skill = catalog.getItemFromGuid(act.refByAction) as Rom.NSkill;

                                if (skill == null)
                                {
                                    removeActions.Add(act);
                                }
                                else if (!IsQualifiedSkillCostStatus(monsterData, skill))
                                {
                                    removeActions.Add(act);
                                }
                            }
                        }

                        activeAction = activeAction.Except(removeActions);
                        break;
                }
            }

            filter();

            // 現在のターン数で実行できる行動が1つも無ければ標準の行動(0ターンの行動)から選択する
            // If there is no action that can be executed in the current number of turns, select from standard actions (0 turn actions)
            if (activeAction.Count() == 0)
            {
                activeAction = actionList.Where(act => act.turn == 0 && checkCondition(act, monsterData, hitPointRate, magicPointRate, isCounter));

                // MPが足りないスキルを除外
                // Exclude skills with insufficient MP
                filter();
            }

            return activeAction;
        }

        private BattleCharacterBase TargetSelectWithHateRate(IEnumerable<BattleCharacterBase> list, BattleEnemyData data = null)
        {
            var hateRateSumList = new List<int>();
            int cnt = 0;
            foreach (var target in list)
            {
                var hateRate = Math.Max(0, target.HateCondition + 100);
                hateRateSumList.Add(hateRate + hateRateSumList.LastOrDefault());
                cnt++;
            }
            if (hateRateSumList.Last() == 0)
            {
                hateRateSumList.Clear();
                cnt = 1;
                foreach (var target in list)
                {
                    hateRateSumList.Add(cnt);
                    cnt++;
                }
            }

            switch (data?.monster.aiPattern ?? Rom.AIPattern.NORMAL)
            {
                case Rom.AIPattern.NORMAL:
                case Rom.AIPattern.CLEVER:
                {
                    var rnd = battleRandom.Next(hateRateSumList.Last());
                    return list.ElementAt(hateRateSumList.FindIndex(x => rnd < x));
                }

                case Rom.AIPattern.TRICKY:
                    if (battleRandom.Next(100) < 75)
                    {
                        // 狙えるターゲットの中で最もHPが少ないキャラクターを狙う
                        // Aim for the character with the lowest HP among all possible targets.
                        return list.OrderBy(target => target.HitPoint).ElementAt(0);
                    }
                    else
                    {
                        var rnd = battleRandom.Next(hateRateSumList.Last());
                        return list.ElementAt(hateRateSumList.FindIndex(x => rnd < x));
                    }
            }

            return null;
        }

        private bool checkCondition(Rom.ActionInfo act, BattleEnemyData monsterData, int hitPointRate, int magicPointRate, bool isCounter)
        {
            bool ok = true;
            bool containsCounter = false;
            foreach (var cond in act.conditions)
            {
                if (!ok)
                    break;

                switch (cond.type)
                {
                    case Rom.ActionConditionType.HP:
                        if (hitPointRate < cond.min || cond.max < hitPointRate)
                            ok = false;
                        break;
                    case Rom.ActionConditionType.MP:
                        if (magicPointRate < cond.min || cond.max < magicPointRate)
                            ok = false;
                        break;
                    case Rom.ActionConditionType.AVRAGE_LEVEL:
                        var average = playerData.Average(x => x.player.level);
                        if (average < cond.min || cond.max < average)
                            ok = false;
                        break;
                    case Rom.ActionConditionType.TURN:
                        if (totalTurn < cond.min || cond.max < totalTurn)
                            ok = false;
                        break;
                    case Rom.ActionConditionType.STATE:
                        if (!monsterData.conditionInfoDic.ContainsKey(cond.refByConiditon))
                            ok = false;
                        break;
                    case Rom.ActionConditionType.SWITCH:
                        if (!owner.data.system.GetSwitch(cond.option, Guid.Empty, false))
                            ok = false;
                        break;
                    case Rom.ActionConditionType.COUNTER:
                        containsCounter = true;
                        break;
                    case Rom.ActionConditionType.CONSUMPTION_STATUS:
                        {
                            var info = gameSettings.GetCastStatusParamInfo(cond.refByConiditon);

                            if (info != null)
                            {
                                var percent = monsterData.GetStatus(gameSettings, info.ConsumptionPercentId);

                                if ((percent < cond.min) || (cond.max < percent))
                                {
                                    ok = false;
                                }
                            }

                        }
                        break;
                }
            }

            if (containsCounter != isCounter)
                ok = false;

            return ok;
        }

        private void UpdateBattleState_ReadyExecuteCommand()
        {
            if (battleEvents.isBusy())
                return;
            battleEvents.clearCurrentProcessingTrigger();

            // キャストの行動の都度、位置調整する時はコメントを外す
            // Remove the comment when adjusting the position each time the cast acts
            //UpdatePosition();

            //activeCharacter = battleEntryCharacters[commandExecuteMemberCount];
            
            attackCount = 0;

            // 前回の行動がカウンターだった場合は、改めて行動をセットする
            // If the previous action was a counter, set the action again
            if (activeCharacter is BattleEnemyData)
            {
                var enm = (BattleEnemyData)activeCharacter;
                if (enm.counterAction == BattleEnemyData.CounterState.COUNTER)
                {
                    enm.counterAction = BattleEnemyData.CounterState.AFTER;
                }
                else if (enm.counterAction == BattleEnemyData.CounterState.AFTER)
                {
                    activeCharacter.selectedBattleCommandType = BattleCommandType.Undecided;
                    SelectEnemyCommand(enm, false, false, enm.currentActionTurn);
                }
            }


            if (activeCharacter.selectedBattleCommandType == BattleCommandType.Skip)
            {
                ChangeBattleState(BattleState.CheckBattleCharacterDown1);
            }
            else
            {
                // 状態異常による行動の変更
                // Behavior changes due to status ailments
                foreach (var e in activeCharacter.conditionInfoDic)
                {
                    var condition = e.Value.rom;

                    if (condition != null)
                    {
                        if (activeCharacter.selectedBattleCommandType != BattleCommandType.PlayerEscape)
                        {
                            if (condition.actionDisabled)
                            {
                                activeCharacter.selectedBattleCommandType = BattleCommandType.Nothing_Down;
                                if (activeCharacter is BattleEnemyData)
                                {
                                    ((BattleEnemyData)activeCharacter).continuousAction = false;
                                    ((BattleEnemyData)activeCharacter).alreadyExecuteActions.Clear();
                                }

                                GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, activeCharacter.Name,
                                    string.Format("Change Action By Condition / Condition : {0} / Type : {1}",
                                    condition.name, activeCharacter.selectedBattleCommandType.ToString()));
                                break;
                            }
                            else if (condition.attack)
                            {
                                activeCharacter.selectedBattleCommandType = BattleCommandType.Attack;
                                if (activeCharacter is BattleEnemyData)
                                {
                                    ((BattleEnemyData)activeCharacter).continuousAction = false;
                                    ((BattleEnemyData)activeCharacter).alreadyExecuteActions.Clear();
                                }

                                // 行動不能が優先なので、ここでは終わらない
                                // Incapacity is the priority, so it doesn't end here
                                GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, activeCharacter.Name,
                                    string.Format("Change Action By Condition / Condition : {0} / Type : {1}",
                                    condition.name, activeCharacter.selectedBattleCommandType.ToString()));
                            }
                        }
                    }
                }

                if (activeCharacter.selectedBattleCommandType == BattleCommandType.Nothing_Down || activeCharacter.selectedBattleCommandType == BattleCommandType.Cancel)
                {
                    ChangeBattleState(BattleState.BattleFinishCheck1);
                }
                else
                {
                    switch (activeCharacter.selectedBattleCommandType)
                    {
                        case BattleCommandType.PlayerEscape:
                        case BattleCommandType.MonsterEscape:
                            break;
                        default:
                            // 攻撃対象を変える必要があるかチェック
                            // Check if the attack target needs to be changed
                            var dontCancel = CheckAndDoReTarget();

                            // ターゲットが無になった場合は発動しない
                            // Does not activate if the target is empty
                            if (!dontCancel && activeCharacter.targetCharacter?.Length == 0)
                            {
                                ChangeBattleState(BattleState.BattleFinishCheck1);

                                return;
                            }
                            break;
                    }

                    battleEvents.start(Rom.Script.Trigger.BATTLE_BEFORE_ACTION);

                    displayedContinueConditions.Clear();

                    ChangeBattleState(BattleState.SetStatusMessageText);
                }
            }

            if (activeCharacter is BattleEnemyData)
            {
                var enm = (BattleEnemyData)activeCharacter;
                if (enm.counterAction == BattleEnemyData.CounterState.COUNTER)
                {
                    GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, activeCharacter.Name,
                        string.Format("Execute Counter Action / Type : {0}", activeCharacter.selectedBattleCommandType.ToString(), enm.currentActionTurn));
                }
                else if (enm.continuousAction)
                {
                    GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, activeCharacter.Name,
                        string.Format("Execute Continuous Action / Type : {0}", activeCharacter.selectedBattleCommandType.ToString(), activeCharacter.ExecuteCommandTurnCount));
                }
                else
                {
                    GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, activeCharacter.Name,
                        string.Format("Execute Action / Type : {0}", activeCharacter.selectedBattleCommandType.ToString(), activeCharacter.ExecuteCommandTurnCount));
                }
            }
            else
            {
                GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, activeCharacter.Name,
                    string.Format("Execute Action / Type : {0}", activeCharacter.selectedBattleCommandType.ToString(), activeCharacter.ExecuteCommandTurnCount));
            }
        }

        private void UpdateBattleState_ExecuteBattleCommand()
        {
            var gs = catalog.getGameSettings();
            var damageTextList = new List<BattleDamageTextInfo>();

            // 攻撃対象を変える必要があるかチェック TODO:元はこの位置で処理していたので、他に影響が出ていないか要確認
            // Check if it is necessary to change the attack target TODO: Originally it was processed at this position, so it is necessary to check if there is any other influence
            //CheckAndDoReTarget();

            activeCharacter.commandFriendEffectCharacters.Clear();
            activeCharacter.commandEnemyEffectCharacters.Clear();

            recoveryStatusInfo.Clear();

            // 強制攻撃の時はターゲットを強制的に変更する
            // Forcibly change the target during a forced attack
            switch (activeCharacter.selectedBattleCommandType)
            {
                case BattleCommandType.Attack:
                case BattleCommandType.Critical:
                case BattleCommandType.ForceCritical:
                case BattleCommandType.Miss:
                    if (attackCount == 0)
                    {
                        var attackConditionTarget = GetAttackConditionTargetCharacter(activeCharacter);

                        if (attackConditionTarget != null)
                        {
                            activeCharacter.targetCharacter = new[] { attackConditionTarget };
                        }
                    }
                    break;
                default:
                    break;
            }

            attackCount++;

            switch (activeCharacter.selectedBattleCommandType)
            {
                case BattleCommandType.Attack:
                case BattleCommandType.Critical:
                case BattleCommandType.ForceCritical:
                case BattleCommandType.Miss:
                    {
                        var isMiss = activeCharacter.selectedBattleCommandType == BattleCommandType.Miss;
                        var isForceCritical = activeCharacter.selectedBattleCommandType == BattleCommandType.ForceCritical;

                        foreach (var target in activeCharacter.targetCharacter)
                        {
                            GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, activeCharacter.Name,
                                string.Format("Hit Judgment / Dexterity : {0} / Evation : {1}", activeCharacter.Dexterity, target.Evasion));

                            if (!isMiss && (isForceCritical || IsHit(activeCharacter, target)))
                            {
                                var isCritical = isForceCritical || CheckCritical(activeCharacter, battleRandom);
                                var damage = CalcAttackWithWeaponDamage(activeCharacter, target, activeCharacter.AttackAttribute, isCritical, damageTextList);

                                target.HitPoint -= damage;
                                target.consumptionStatusValue.SubStatus(gs.maxHPStatusID, damage);

                                CheckDamageRecovery(target, damage);

                                setAttributeWithWeaponDamage(target, activeCharacter.AttackAttribute, activeCharacter.ElementAttack);

                                target.CommandReactionType = ReactionType.Damage;

                                activeCharacter.commandEnemyEffectCharacters.Add(target);

                                SetCounterAction(target, activeCharacter);
                            }
                            else
                            {
                                AddBattleMissTextInfo(damageTextList, target);
                                target.CommandReactionType = ReactionType.None;
                            }
                        }
                    }
                break;

                case BattleCommandType.Guard:
                    foreach (var target in activeCharacter.targetCharacter)
                    {
                        target.CommandReactionType = ReactionType.None;

                        // 次の自分のターンが回ってくるまでダメージを軽減
                        // Reduce damage until your next turn comes around
                        // 問題 : 素早さのパラメータが低いと後攻ガードになってしまいガードの意味が無くなってしまう
                        // Problem : If the quickness parameter is low, it will become the second guard and the meaning of guarding will be lost.
                        // 解決案 : ガードコマンドを選択した次のターンまでガードを有効にする (実質2ターンの効果に変更する)
                        // Solution: Keep the guard active until the next turn after selecting the guard command (effectively change the effect to 2 turns)
                        // TODO : 軽減できるのは物理ダメージだけ? 魔法ダメージはどう扱うのか確認する
                        // TODO: Only physical damage can be reduced? Check how magic damage is handled
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

                    var skill = catalog.getItemFromGuid(activeCharacter.selectedBattleCommand.refGuid) as Rom.NSkill;

                    GetSkillTarget(skill, out friendEffectTargets, out enemyEffectTargets);



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
                // Skill effect target reselection
                {
                    BattleCharacterBase[] friendEffectTargets, enemyEffectTargets;
                    BattleCharacterBase[] friendEffectedCharacters, enemyEffectedCharacters;

                    GetSkillTarget(activeCharacter.selectedSkill, out friendEffectTargets, out enemyEffectTargets);

                    if (IsQualifiedSkillCostStatus(activeCharacter, activeCharacter.selectedSkill) &&
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

                        string statusName;

                        if (!IsQualifiedSkillCostStatus(activeCharacter, activeCharacter.selectedSkill, out statusName))
						{
                            activeCharacter.skillFailCauses = statusName;
                        }
                        else if (!isQualifiedSkillCostItem(activeCharacter, activeCharacter.selectedSkill))
                            activeCharacter.skillFailCauses = catalog.getItemFromGuid(activeCharacter.selectedSkill.ConsumptionItem)?.name ?? gameSettings.glossary.item;
                    }
                }
                break;

                case BattleCommandType.Item:
                {
                    var selectedItem = activeCharacter.selectedItem;

                    if (selectedItem.item.IsExpandableWithSkill)
                    {
                        if (activeCharacter.targetCharacter != null)
                        {
                            BattleCharacterBase[] friendEffectTargets, enemyEffectTargets;
                            BattleCharacterBase[] friendEffectedCharacters, enemyEffectedCharacters;

                            var skill = catalog.getItemFromGuid(selectedItem.item.expendableWithSkill.skill) as Common.Rom.NSkill;

                            if (skill != null)
                            {
                                GetSkillTarget(skill, out friendEffectTargets, out enemyEffectTargets);

                                EffectSkill(activeCharacter, skill, friendEffectTargets.ToArray(), enemyEffectTargets.ToArray(), damageTextList, recoveryStatusInfo, out friendEffectedCharacters, out enemyEffectedCharacters);

                                activeCharacter.targetCharacter = friendEffectedCharacters.Union(enemyEffectedCharacters).ToArray();

                                battleEvents.setLastSkillTargetIndex(friendEffectTargets, enemyEffectTargets);

                                if (activeCharacter.targetCharacter.Count() > 0)
                                {
                                    // アイテムの数を減らす
                                    // reduce the number of items
                                    if (selectedItem.item.Consumption)
                                        selectedItem.num--;
                                    party.SetItemNum(selectedItem.item.guId, selectedItem.num);

                                    activeCharacter.commandFriendEffectCharacters.AddRange(friendEffectedCharacters);
                                    activeCharacter.commandEnemyEffectCharacters.AddRange(enemyEffectedCharacters);
                                }
                            }
                        }
                    }
                    else if (selectedItem.item.IsExpandable)
                    {
                        if (activeCharacter.targetCharacter != null)
                        {
                            var useRest = activeCharacter.targetCharacter.Length;

                            foreach (var target in activeCharacter.targetCharacter)
                            {
                                if (IsHitRange(activeCharacter, selectedItem.item, target) && UseItem(selectedItem.item, target, damageTextList, recoveryStatusInfo))
                                {
                                    useRest--;

                                    // アイテムの数を減らす
                                    // reduce the number of items
                                    if (selectedItem.item.Consumption)
                                        selectedItem.num--;
                                    party.SetItemNum(selectedItem.item.guId, selectedItem.num);
                                }

                                target.CommandReactionType = ReactionType.Heal;


                                if (useRest <= 0)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
                break;

                case BattleCommandType.Position:
                    if (activeCharacter.targetCharacter != null)
                    {
                        var targetCharacter = activeCharacter.targetCharacter[0];
                        var idx = playerData.IndexOf(activeCharacter as BattlePlayerData);
                        var targetIdx = playerData.IndexOf(targetCharacter as BattlePlayerData);

                        playerData[idx] = targetCharacter as BattlePlayerData;
                        playerData[targetIdx] = activeCharacter as BattlePlayerData;

                        idx = targetPlayerData.IndexOf(activeCharacter);
                        targetIdx = targetPlayerData.IndexOf(targetCharacter);

                        targetPlayerData[idx] = targetCharacter;
                        targetPlayerData[targetIdx] = activeCharacter;

                        idx = playerViewData.IndexOf(activeCharacter as BattlePlayerData);
                        targetIdx = playerViewData.IndexOf(targetCharacter as BattlePlayerData);

                        playerViewData[idx] = targetCharacter as BattlePlayerData;
                        playerViewData[targetIdx] = activeCharacter as BattlePlayerData;

                        MovePlayerPosition();
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
                    target.ConsistancyConsumptionStatus(gameSettings);

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
                case BattleCommandType.Item:
                    ChangeBattleState(BattleState.SetCommandEffect);
                    break;
                default:
                    ChangeBattleState(BattleState.SetCommandEffect);
                    break;
            }
        }

        private void GetSkillTarget(Rom.NSkill skill, out BattleCharacterBase[] friendEffectTargets, out BattleCharacterBase[] enemyEffectTargets)
        {
            activeCharacter.GetSkillTarget(skill, out friendEffectTargets, out enemyEffectTargets,
                activeCharacter.FriendPartyRefMember.Where(x => IsHitRange(activeCharacter, skill.Range, x)),
                activeCharacter.EnemyPartyRefMember.Where(x => IsHitRange(activeCharacter, skill.Range, x)));
        }

        private void SetCounterAction(BattleCharacterBase target, BattleCharacterBase source)
        {
            var enm = target as BattleEnemyData;
            if (enm == null)
                return;

            var actions = GetActiveActions(enm, true, enm.currentActionTurn);
            if (actions.FirstOrDefault() != null)
            {
                target.selectedBattleCommandType = BattleCommandType.Undecided;
                SelectEnemyCommand(enm, false, true, enm.currentActionTurn, actions);
                enm.counterAction = BattleEnemyData.CounterState.COUNTER;
                // 味方を対象にするスキルなら自分をターゲットにしておく
                // If the skill targets an ally, target yourself
                if (target.selectedBattleCommandType == BattleCommandType.Skill &&
                    (target.selectedSkill.option.target == Rom.TargetType.PARTY_ONE || target.selectedSkill.option.target == Rom.TargetType.PARTY_ALL))
                    target.targetCharacter[0] = target;
                // その他は攻撃してきた相手を狙う
                // Others aim at the opponent who attacked
                else if (target.targetCharacter == null)
                    target.targetCharacter = new BattleCharacterBase[] { source };
                else if (target.targetCharacter.Length == 1)
                    target.targetCharacter[0] = source;

                if (target is ExBattleEnemyData)
                    ((ExBattleEnemyData)target).turnGauge = 1;
                //battleEntryCharacters.Insert(commandExecuteMemberCount + 1, target);
            }
            else
            {
                GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, enm.Name, "No Counter Action.");
            }
        }

        // 攻撃を受けた時の状態異常 回復判定
        // Status abnormality recovery judgment when attacked
        private void CheckDamageRecovery(BattleCharacterBase target, int damage)
        {
            var recoveryList = new List<Hero.ConditionInfo>(target.conditionInfoDic.Count);

            foreach (var e in target.conditionInfoDic)
            {
                var info = e.Value;

                if ((info.recovery & Hero.ConditionInfo.RecoveryType.Damage) != 0)
                {
                    info.damageValue -= damage;

                    if (info.damageValue <= 0)
                    {
                        recoveryList.Add(info);

                        break;
                    }
                }
            }

            foreach (var info in recoveryList)
            {
                if (target != activeCharacter)
                {
                    target.selectedBattleCommandType = BattleCommandType.Cancel;
                }

                target.RecoveryCondition(info.condition);

                if ((target.HitPoint > 0) && (info.rom != null))
                {
                    recoveryStatusInfo.Add(new RecoveryStatusInfo(target, info.rom));
                }
            }
        }

        private void UpdateBattleState_SetStatusMessageText()
        {
            // バトルイベント完了を待つ
            // Wait for battle event to complete
            if (battleEvents.isBusy())
                return;
            battleEvents.clearCurrentProcessingTrigger();

            string message = "";
            bool isDisplayMessage = false;
            var isActionDisabled = false;

            foreach (var e in activeCharacter.conditionInfoDic)
            {
                var condition = e.Value.rom;

                if (!displayedContinueConditions.Contains(e.Key))
                {
                    displayedContinueConditions.Add(e.Key);

                    if ((condition != null) && !condition.slipDamage)
                    {
                        message = string.Format(condition.messageForContinue, activeCharacter.Name);

                        if (!string.IsNullOrEmpty(message))
                        {
                            isDisplayMessage = true;
                            GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, "Message", message);
                            break;
                        }
                    }
                }

                if (!isActionDisabled || (condition != null))
                {
                    isActionDisabled = isActionDisabled || condition.actionDisabled;
                }
            }

            if (isDisplayMessage)
            {
                battleViewer.SetDisplayMessage(message);

                ChangeBattleState(BattleState.DisplayStatusMessage);
            }
            else
            {
                // コマンドに応じたアクションをアクターにとらせる
                // Make actors take actions according to commands
                activeCharacter.ExecuteCommandStart();

                if (isActionDisabled)
                {
                    ChangeBattleState(BattleState.ExecuteBattleCommand);
                }
                else
                {
                    ChangeBattleState(BattleState.SetCommandMessageText);
                }
            }
        }

        private void UpdateBattleState_DisplayStatusMessage()
        {
            if (battleStateFrameCount > 30)
            {
                battleViewer.CloseWindow();

                ChangeBattleState(BattleState.SetStatusMessageText);
            }
        }

        private void UpdateBattleState_DisplayStatusDamage()
        {
            bool isEndPlayerStatusUpdate = playerData.Select(player => UpdateBattleStatusData(player)).All(isUpdated => isUpdated == false);
            isEndPlayerStatusUpdate |= enemyData.Select(enemy => UpdateBattleStatusData(enemy)).All(isUpdated => isUpdated == false);

            if (!isEndPlayerStatusUpdate) statusUpdateTweener.Update();

            if (!battleViewer.IsPlayDamageTextAnimation && isEndPlayerStatusUpdate)
            {
                battleViewer.CloseWindow();

                ChangeBattleState(BattleState.CheckBattleCharacterDown2);
            }
        }

        private void UpdateBattleState_SetCommandMessageText()
        {
            string message = "";

            switch (activeCharacter.selectedBattleCommandType)
            {
                case BattleCommandType.Nothing:
                    message = string.Format(gameSettings.glossary.battle_wait, activeCharacter.Name);
                    break;
                case BattleCommandType.Attack:
                case BattleCommandType.Miss:
                    message = string.Format(gameSettings.glossary.battle_attack, activeCharacter.Name);
                    break;
                case BattleCommandType.Critical:
                case BattleCommandType.ForceCritical:
                    message = string.Format(gameSettings.glossary.battle_critical, activeCharacter.Name);
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
                    message = string.Format(gameSettings.glossary.battle_skill, activeCharacter.Name, activeCharacter.selectedSkill.name);
                    break;
                case BattleCommandType.Item:
                    message = string.Format(gameSettings.glossary.battle_item, activeCharacter.Name, activeCharacter.selectedItem.item.name);
                    break;
                case BattleCommandType.PlayerEscape:
                    message = gameSettings.glossary.battle_escape_command;
                    break;
                case BattleCommandType.MonsterEscape:
                    message = string.Format(gameSettings.glossary.battle_enemy_escape, activeCharacter.Name);
                    break;
            }

            battleViewer.SetDisplayMessage(message);
            GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, "Message", message);

            ChangeBattleState(BattleState.DisplayMessageText);
        }

        private void UpdateBattleState_DisplayMessageText()
        {
            if ((battleStateFrameCount > 20 || battleViewer.HasNoMessageWindow() || Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE, Input.GameState.MENU)) && isReady3DCamera() && isReadyActor())
            {
                ChangeBattleState(BattleState.ExecuteBattleCommand);
            }
        }

        private bool IsBusyWaitForCommon(Guid inEventId)
        {
            return ((inEventId != Guid.Empty) && battleEvents.isBusy(inEventId));
        }

        private void UpdateBattleState_SetCommandEffect()
        {
            for (int i = 0; i < waitForCommons.Length; i++)
			{
				if (IsBusyWaitForCommon(waitForCommons[i]))
				{
                    return;
                }

                waitForCommons[i] = Guid.Empty;
            }

            if (IsBusyWaitForCommon(waitForCommon))
            {
                return;
            }

            waitForCommon = Guid.Empty;

            ClearCommandEffect();
            GetCommandEffectImpl(activeCharacter, catalog, out var friendEffectGuid, out var enemyEffectGuid);
          
            var isDigiEvo = GameMain.instance.data.system.GetSwitch("executeDigievo", Guid.Empty, false);

            if (activeCharacter.selectedBattleCommandType == BattleCommandType.SameSkillEffect)
            {
                
                if (isDigiEvo) // CUSTOM
                {
                    if(activeCharacter.mapChr.isChangeMotionAvailable() && activeCharacter.mapChr.currentMotion != "wait" && battleEvents.GetChoicesResult() == -1) activeCharacter.mapChr.playMotion("wait", 0.2f, false, false);
                    Tools.PushLog("Starting digievolution");
                    if (!PrepareEvolution()) return;
                    ChoiceEvolution();
                }
            }


            if (activeCharacter.targetCharacter != null)
            {
                if (!activeCharacter.IsHero)
                {
                    // 敵がスキルを使用した場合、敵味方を反転させる
                    // Inverts allies and enemies when using a skill
                    var tmp = friendEffectGuid;

                    friendEffectGuid = enemyEffectGuid;
                    enemyEffectGuid = tmp;
                }

                foreach (var target in activeCharacter.targetCharacter)
                {
                    if (friendEffectGuid != Guid.Empty && target.IsHero)
                    {
                        battleViewer.SetBattlePlayerEffect(friendEffectGuid, target);
                    }
                    else if (enemyEffectGuid != Guid.Empty && !target.IsHero)
                    {
                        battleViewer.SetBattleMonsterEffect(enemyEffectGuid, target);
                    }
                }
            }

            if (!isDigiEvo || selectingEvoDone) // Custom
            {
                ChangeBattleState(BattleState.DisplayCommandEffect);
                EffectDrawer3D.ResetStartWait();
            }
        }

        private void ClearCommandEffect()
        {
            battleViewer.effectDrawTargetPlayerList.Clear();
            battleViewer.effectDrawTargetMonsterList.Clear();
            battleViewer.defeatEffectDrawTargetList.Clear();
        }

        private static void GetCommandEffectImpl(BattleCharacterBase activeCharacter, Catalog catalog, out Guid friendEffectGuid, out Guid enemyEffectGuid)
        {
            friendEffectGuid = Guid.Empty;
            enemyEffectGuid = Guid.Empty;
    
            switch (activeCharacter.selectedBattleCommandType)
            {
                case BattleCommandType.Attack:
                case BattleCommandType.Critical:
                case BattleCommandType.ForceCritical:
                case BattleCommandType.Miss:
                    EffectDrawer3D.setStartWait(0.1f);
                    // 混乱・魅了で反転している場合があるので、どちらにも代入する
                    // It may be reversed due to confusion or fascination, so substitute it for both.
                    friendEffectGuid = activeCharacter.AttackEffect;
                    enemyEffectGuid = activeCharacter.AttackEffect;
                    break;

                case BattleCommandType.Guard:
                    break;

                case BattleCommandType.Charge:
                    break;

                case BattleCommandType.SameSkillEffect:

                    if (activeCharacter.targetCharacter != null)
                    {
                        var skill = catalog.getItemFromGuid(activeCharacter.selectedBattleCommand.refGuid) as Rom.NSkill;

                        switch (skill.option.target)
                        {
                            case Rom.TargetType.PARTY_ONE:
                            case Rom.TargetType.PARTY_ALL:
                            case Rom.TargetType.SELF:
                            case Rom.TargetType.OTHERS:
                                friendEffectGuid = skill.friendEffect.effect;
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
                                friendEffectGuid = skill.friendEffect.effect;
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
                                friendEffectGuid = activeCharacter.selectedSkill.friendEffect.effect;
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
                                friendEffectGuid = activeCharacter.selectedSkill.friendEffect.effect;
                                enemyEffectGuid = activeCharacter.selectedSkill.enemyEffect.effect;
                                break;
                        }
                    }
                    break;

                case BattleCommandType.Item:
                    if (activeCharacter.selectedItem.item.IsExpandableWithSkill)
                    {
                        var skill = (Common.Rom.NSkill)catalog.getItemFromGuid(activeCharacter.selectedItem.item.expendableWithSkill.skill);
                        if (skill != null)
                        {
                            switch (skill.option.target)
                            {
                                case Rom.TargetType.PARTY_ONE:
                                case Rom.TargetType.PARTY_ALL:
                                case Rom.TargetType.SELF:
                                case Rom.TargetType.OTHERS:
                                    friendEffectGuid = skill.friendEffect.effect;
                                    break;

                                case Rom.TargetType.ENEMY_ONE:
                                case Rom.TargetType.ENEMY_ALL:
                                    enemyEffectGuid = skill.enemyEffect.effect;
                                    break;

                                case Rom.TargetType.ALL:                   // 敵味方全員 / All enemies and allies
                                case Rom.TargetType.SELF_ENEMY_ONE:        // 自分と敵一人 / me and one enemy
                                case Rom.TargetType.SELF_ENEMY_ALL:        // 自分と敵全員 / myself and all my enemies
                                case Rom.TargetType.OTHERS_ENEMY_ONE:      // 自分以外と敵一人 / Other than yourself and one enemy
                                case Rom.TargetType.OTHERS_ALL:            // 自分以外の敵味方全員 / All enemies and allies other than yourself
                                case Rom.TargetType.PARTY_ONE_ENEMY_ALL:   // 味方一人と敵全員 / One ally and all enemies
                                case Rom.TargetType.PARTY_ALL_ENEMY_ONE:   // 味方全員と敵一人 / All allies and one enemy
                                    friendEffectGuid = skill.friendEffect.effect;
                                    enemyEffectGuid = skill.enemyEffect.effect;
                                    break;
                            }
                        }
                    }
                    else if (activeCharacter.selectedItem.item.IsExpandable)
                    {
                        {
                            friendEffectGuid = activeCharacter.selectedItem.item.expendable.effect;
                        }
                    }
                    break;
            }
        }

        private void UpdateBattleState_DisplayCommandEffect()
        {
            if (battleViewer.IsEffectAllowShowDamage && (battleViewer.IsEndMotion(activeCharacter) || activeCharacter.selectedBattleCommandType == BattleCommandType.Nothing))
            {
                battleViewer.SetupDamageTextAnimation();

                if (activeCharacter.selectedBattleCommandType == BattleCommandType.Skill && activeCharacter.targetCharacter == null)
                {
                    battleViewer.SetDisplayMessage(string.Format(gameSettings.glossary.battle_skill_failed, activeCharacter.skillFailCauses));
                }

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
                        foreach (var info in gameSettings.CastStatusParamInfoList)
                        {
                            if (info.Consumption)
                            {
                                player.consumptionStatusValue.SetStatus(info.guId, player.baseStatusValue.GetStatus(info.guId));
                            }
                        }

                        player.HitPoint = player.MaxHitPoint;
                        player.MagicPoint = player.MaxMagicPoint;
                    }

                    SetNextBattleStatus(player);
                }

                foreach (var enemy in enemyData)
                {
                    SetNextBattleStatus(enemy);
                }

               

                statusUpdateTweener.Begin(0, 1.0f, 30);

                ChangeBattleState(BattleState.DisplayDamageText);
            }
        }

        /// <summary>
        /// 連続攻撃の種別
        /// Continuous attack type
        /// </summary>
        enum ContinuousType
        {
            NONE,
            ATTACK,
            CONTINUOUS_ACTION,
        }
        private ContinuousType CheckAttackContinue()
        {
            // 状態異常で攻撃回数が追加されているか？
            // Is the number of attacks added due to status ailments?
            switch (activeCharacter.selectedBattleCommandType)
            {
                case BattleCommandType.Attack:
                case BattleCommandType.Critical:
                case BattleCommandType.ForceCritical:
                case BattleCommandType.Miss:
                    if (attackCount < 1 + activeCharacter.AttackAddCondition)
                    {
                        foreach (var target in activeCharacter.targetCharacter)
                        {
                            if (!target.IsDeadCondition())
                            {
                                GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, activeCharacter.Name,
                                    string.Format("Continuous Attack / Rest : {0}", activeCharacter.AttackAddCondition - attackCount));
                                return ContinuousType.ATTACK;
                            }
                        }
                    }
                    break;
            }

            // 連続行動か？
            // Continuous action?
            if (activeCharacter is BattleEnemyData)
            {
                if (((BattleEnemyData)activeCharacter).continuousAction)
                {
                    activeCharacter.ExecuteCommandEnd();
                    activeCharacter.selectedBattleCommandType = BattleCommandType.Undecided;
                    SelectEnemyCommand((BattleEnemyData)activeCharacter, true, false);

                    // 連続行動中の「何もしない」はメッセージを出さない
                    // \
                    // TODO : できれば中間に何もしないがある場合もスキップにしたい
                    // TODO : If possible, I want to skip even if there is nothing in between
                    if (activeCharacter.selectedBattleCommandType != BattleCommandType.Nothing)
                    {
                        return ContinuousType.CONTINUOUS_ACTION;
                    }
                }

                ((BattleEnemyData)activeCharacter).alreadyExecuteActions.Clear();
            }

            return ContinuousType.NONE;
        }

        private void UpdateBattleState_DisplayDamageText()
        {
            bool isUpdated = false;

            foreach (var player in playerData)
            {
                isUpdated |= UpdateBattleStatusData(player);
            }
            foreach (var enemy in enemyData)
            {
                isUpdated |= UpdateBattleStatusData(enemy);
            }

            if (isUpdated)
            {
                statusUpdateTweener.Update();
            }

            // ダメージ用テキストとステータス用ゲージのアニメーションが終わるまで待つ
            // Wait for damage text and status gauge animation to finish
            if (!battleEvents.isBusy(false) && battleViewer.IsEffectEndPlay && !battleViewer.IsPlayDamageTextAnimation && !isUpdated && isReady3DCamera() &&
                ((activeCharacter.targetCharacter != null || battleStateFrameCount > 60)))
            {
                bool complete = true;

                if (activeCharacter.targetCharacter != null)
                {
                    foreach (var target in activeCharacter.targetCharacter)
                    {
                        if (!battleViewer.IsEndMotion(target))
                        {
                            complete = false;
                            break;
                        }

                        target.CommandReactionEnd();
                    }
                }

                if (complete)
                {
                    var continuous = CheckAttackContinue();

                    battleViewer.CloseWindow();

                    var nextBattleState = BattleState.SetConditionMessageText;

                    if (continuous == ContinuousType.NONE)
                    {
                        battleEvents.start(Rom.Script.Trigger.BATTLE_AFTER_ACTION);
                        activeCharacter.ExecuteCommandEnd();
                        activeCharacter.selectedBattleCommandType = BattleCommandType.Nothing;
                    }
                    else if (continuous == ContinuousType.CONTINUOUS_ACTION)
                    {
                        battleEvents.start(Rom.Script.Trigger.BATTLE_AFTER_ACTION);
                        nextBattleState = BattleState.ReadyExecuteCommand;
                    }
                    else if (continuous == ContinuousType.ATTACK)
                    {
                        nextBattleState = BattleState.ExecuteBattleCommand;
                    }

                    ChangeBattleState(nextBattleState);
                }
            }
        }

        private void UpdateBattleState_SetConditionMessageText()
        {
            if (battleEvents.isBusy())
                return;
            battleEvents.clearCurrentProcessingTrigger();

            string message = "";
            bool isDisplayMessage = false;

            foreach (var e in displayedSetConditionsDic)
            {
                retry:

                if (e.Value.Count == 0)
                {
                    continue;
                }

                var condition = e.Value[0];

                message = string.Format(e.Key.IsHero ? condition.messageForAlly : condition.messageForEnemy, e.Key.Name);

                e.Value.Remove(condition);

                if (string.IsNullOrEmpty(message))
                {
                    goto retry;
                }
                else
                {
                    isDisplayMessage = true;

                    break;
                }
            }

            if (isDisplayMessage)
            {
                battleViewer.SetDisplayMessage(message);

                ChangeBattleState(BattleState.DisplayConditionMessageText);
            }
            else
            {
                ChangeBattleState(BattleState.CheckCommandRecoveryStatus);
            }
        }

        private void UpdateBattleState_DisplayConditionMessageText()
        {
            if ((battleStateFrameCount > 30 || Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE, Input.GameState.MENU)) && isReady3DCamera() && isReadyActor())
            {
                ChangeBattleState(BattleState.SetConditionMessageText);
            }
        }

        private void UpdateBattleState_CheckCommandRecoveryStatus()
        {
            // バトルイベントが処理中だったら、スキル・アイテム名のウィンドウだけ閉じてまだ待機する
            // If the battle event is being processed, just close the skill/item name window and wait.
            if (battleEvents.isBusy())
                return;
            battleEvents.clearCurrentProcessingTrigger();

            // 状態異常が継続している場合は表示しない（ダメージ付きの睡眠スキルなどで発生する可能性あり）
            // Does not display if abnormal status continues (may occur with sleep skills with damage)
            while (recoveryStatusInfo.Count > 0 && recoveryStatusInfo[0].IsContinued)
                recoveryStatusInfo.RemoveAt(0);

            if (recoveryStatusInfo.Count == 0)
            {
                ChangeBattleState(BattleState.CheckBattleCharacterDown1);
            }
            else
            {
                ((BattleViewer3D)battleViewer).RecoveryConditionMotion(recoveryStatusInfo[0]);
                battleViewer.SetDisplayMessage(recoveryStatusInfo[0].GetMessage(gameSettings));

                ChangeBattleState(BattleState.DisplayCommandRecoveryStatus);
            }
        }

        private void UpdateBattleState_DisplayCommandRecoveryStatus()
        {
            if (battleStateFrameCount >= 30 || Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE, Input.GameState.MENU))
            {
                recoveryStatusInfo.RemoveAt(0);

                ChangeBattleState(BattleState.CheckCommandRecoveryStatus);
            }
        }

        private void UpdateBattleState_CheckBattleCharacterDown1()
        {
            if (battleEvents.isBusy())
                return;
            battleEvents.clearCurrentProcessingTrigger();

            ClearCommandEffect();

            CheckBattleCharacterDown();

            ChangeBattleState(BattleState.FadeMonsterImage1);
        }

        private void UpdateBattleState_FadeMonsterImage1()
        {
            //if (battleViewer.IsFadeEnd)
            {
                ChangeBattleState(BattleState.BattleFinishCheck1);
            }
        }

        private void UpdateBattleState_BattleFinishCheck1()
        {
            if (battleEvents.isBusy())
                return;
            battleEvents.clearCurrentProcessingTrigger();

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

        private void UpdateBattleState_ProcessPoisonStatus()
        {
            var gs = catalog.getGameSettings();

            if ((playerData.Cast<BattleCharacterBase>().Contains(activeCharacter) || enemyData.Cast<BattleCharacterBase>().Contains(activeCharacter)))
            {
                var isSlipDamage = false;

                foreach (var e in activeCharacter.conditionInfoDic.Values.ToArray())
                {
                    var condition = e.rom;

                    if ((condition != null) && condition.slipDamage)
                    {
                        var damage = 0;

                        switch (condition.battleSlipDamageType)
                        {
                            case Common.Rom.Condition.SlipDamageType.Direct:
                                damage = condition.battleSlipDamageParam;
                                break;
                            case Common.Rom.Condition.SlipDamageType.HPPercent:
                                damage = activeCharacter.HitPoint * condition.battleSlipDamageParam;
                                damage = (damage > 100) ? damage / 100 : 1;
                                break;
                            case Common.Rom.Condition.SlipDamageType.MaxHPPercent:
                                damage = activeCharacter.MaxHitPoint * condition.battleSlipDamageParam;
                                damage = (damage > 100) ? damage / 100 : 1;
                                break;
                            default:
                                break;
                        }

                        // キャストのパラメータによる軽減
                        // Mitigation by casting parameters
                        damage = (int)Math.Ceiling((double)damage * (100 - activeCharacter.GetStatus(gameSettings, Rom.GameSettings.PoisonDamageReductionPercentStatusKey)) / 100);

                        if (damage > 0)
                        {
                            activeCharacter.HitPoint -= damage;
                            activeCharacter.consumptionStatusValue.SubStatus(gs.maxHPStatusID, damage);

                            if (activeCharacter.HitPoint < 0)
                            {
                                activeCharacter.HitPoint = 0;
                            }

                            activeCharacter.consumptionStatusValue.Max(gs.maxHPStatusID, 0);

                            // 毒ダメージで睡眠を解除する場合はコメントアウトを外す
                            // Uncomment out if you want to cancel sleep with poison damage
                            //CheckDamageRecovery(activeCharacter, damage);

                            string message = string.Format(condition.messageForContinue, activeCharacter.Name);

                            battleViewer.SetDisplayMessage(message);

                            // TODO 複数表示対応
                            // TODO Multiple display support
                            var damageText = new List<BattleDamageTextInfo>();

                            AddBattleDamageTextInfo(damageText, BattleDamageTextInfo.TextType.Damage, activeCharacter, damage.ToString(), gs.maxHPStatusID);

                            battleViewer.SetDamageTextInfo(damageText);

                            battleViewer.SetupDamageTextAnimation();

                            isSlipDamage = true;
                        }
                    }
                }

                if (isSlipDamage)
                {
                    statusUpdateTweener.Begin(0, 1.0f, 30);

                    if (owner.debugSettings.battleHpAndMpMax)
                    {
                        foreach (var info in gameSettings.CastStatusParamInfoList)
                        {
                            if (info.Consumption)
                            {
                                activeCharacter.consumptionStatusValue.SetStatus(info.guId, activeCharacter.baseStatusValue.GetStatus(info.guId));
                            }
                        }

                        activeCharacter.HitPoint = activeCharacter.MaxHitPoint;
                        activeCharacter.MagicPoint = activeCharacter.MaxMagicPoint;
                    }

                    SetNextBattleStatus(activeCharacter);

                    ChangeBattleState(BattleState.DisplayStatusDamage);
                }
                else
                {
                    ChangeBattleState(BattleState.CheckBattleCharacterDown2);
                }
            }
            else
            {
                ChangeBattleState(BattleState.CheckBattleCharacterDown2);
            }
        }

        private void UpdateBattleState_CheckBattleCharacterDown2()
        {
            ClearCommandEffect();

            CheckBattleCharacterDown();

            ChangeBattleState(BattleState.FadeMonsterImage2);
        }

        private void UpdateBattleState_FadeMonsterImage2()
        {
            //if (battleViewer.IsFadeEnd)
            {
                ChangeBattleState(BattleState.BattleFinishCheck2);
            }
        }

        private void UpdateBattleState_BattleFinishCheck2()
        {
            var battleResult = CheckBattleFinish();

            if (battleResult == BattleResultState.NonFinish)
            {
                ResetCtbGauge(activeCharacter);
                ChangeBattleState(WAIT_CTB_GAUGE);
                if (activeCharacter != null)
                    activeCharacter.selectedBattleCommandType = BattleCommandType.Undecided;
                //{
                //    commandExecuteMemberCount++;

                //    if (commandExecuteMemberCount >= battleEntryCharacters.Count)
                //    {
                //        foreach (var player in playerData)
                //        {
                //            player.forceSetCommand = false;
                //            player.commandSelectedCount = 0;
                //            player.selectedBattleCommandType = BattleCommandType.Undecided;
                //        }
                //        foreach (var enemy in enemyData)
                //        {
                //            enemy.selectedBattleCommandType = BattleCommandType.Undecided;
                //        }

                //        battleEvents.start(Rom.Script.Trigger.BATTLE_TURN);
                //        totalTurn++;
                //        ChangeBattleState(BattleState.Wait);
                //    }
                //    else
                //    {
                //        ChangeBattleState(BattleState.ReadyExecuteCommand);
                //    }
                //}
            }
            else
            {
                battleEvents.setBattleResult(battleResult);
                ChangeBattleState(BattleState.StartBattleFinishEvent);
            }
        }

        private void ResetCtbGauge(BattleCharacterBase chr)
        {
            if (chr != null)
            {
                if(chr is ExBattlePlayerData)
                    ((ExBattlePlayerData)chr).turnGauge -= 1;
                else if (chr is ExBattleEnemyData)
                    ((ExBattleEnemyData)chr).turnGauge -= 1;
            }
        }

        private void UpdateBattleState_PlayerChallengeEscape()
        {
            if (battleStateFrameCount >= 0)
            {

                var escapeCharacter = activeCharacter;
                int escapeSuccessPercent = escapeCharacter.EscapeSuccessBasePercent;

                // 素早さの差が大きいほど成功確率が上がる
                // The greater the speed difference, the higher the chance of success.
                if (escapeSuccessPercent < 100)
                {

                    int playerMaxSpeed = playerData.Max(player => player.Speed);

                    if (playerMaxSpeed < 1)
                        playerMaxSpeed = 1;
                    escapeSuccessPercent += (int)((1.5f - (enemyData.Max(monster => monster.Speed) / playerMaxSpeed)) * 100);
                }

                // 逃走に失敗するたびに成功確率が上がる
                // Every time you fail to escape, the chance of success increases.
                escapeSuccessPercent += (playerEscapeFailedCount * 15);

                if (escapeSuccessPercent < 0) escapeSuccessPercent = 0;
                if (escapeSuccessPercent > 100) escapeSuccessPercent = 100;

                if (escapeSuccessPercent >= battleRandom.Next(100))// 逃走成功 / escape success
                {
                    if (BattleResultEscapeEvents != null)
                    {
                        BattleResultEscapeEvents();
                    }

                    Audio.PlaySound(owner.se.escape);

                    battleViewer.SetDisplayMessage(string.Format(escapeCharacter.EscapeSuccessMessage, escapeCharacter.Name));

                    ChangeBattleState(BattleState.PlayerEscapeSuccess);
                }
                else                                               // 逃走失敗 / escape failure
                {
                    playerEscapeFailedCount++;

                    battleViewer.SetDisplayMessage(gameSettings.glossary.battle_escape_failed);

                    ChangeBattleState(BattleState.PlayerEscapeFail);
                }
            }
        }

        private void UpdateBattleState_StartBattleFinishEvent()
        {
            if (owner.mapScene.isToastVisible() || !battleViewer.IsEffectEndPlay)
            {
                return;
            }



            battleEvents.start(Rom.Script.Trigger.BATTLE_END);
            ChangeBattleState(BattleState.ProcessBattleFinish);
        }

        private void UpdateBattleState_ProcessBattleFinish()
        {
            if (battleEvents.isBusy())
                return;
            battleEvents.clearCurrentProcessingTrigger();

            ApplyPlayerDataToGameData(!owner.debugSettings.battleHpAndMpMax);
            ProcessBattleResult(CheckBattleFinish());
        }

        private void UpdateBattleState_ResultInit()
        {
            if (!battleEvents.isBusy())
            {
                battleEvents.clearCurrentProcessingTrigger();
                ChangeBattleState(BattleState.Result);
            }
        }

        private void UpdateBattleState_Result()
        {
            resultProperty = resultViewer.Update();

            if (resultViewer.IsEnd && (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE, Input.GameState.MENU) ||
                        Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.TOUCH, Input.GameState.SYSTEM) || resultViewer.clickedCloseButton))
            {
                resultViewer.clickedCloseButton = false;
                Audio.PlaySound(owner.se.decide);

                BattleResult = BattleResultState.Win;

                // バトル終了に遷移
                // Transition to battle end
                ChangeBattleState(BattleState.SetFinishEffect);
            }
        }


        private void UpdateBattleState_PlayerEscapeSuccess()
        {
            if (battleStateFrameCount >= 90 && !battleEvents.isBusy())
            {
                ApplyPlayerDataToGameData(!owner.debugSettings.battleHpAndMpMax);

                battleViewer.CloseWindow();

                BattleResult = BattleResultState.Escape;
                battleEvents.setBattleResult(BattleResult);
                battleEvents.start(Rom.Script.Trigger.BATTLE_END);
                ChangeBattleState(BattleState.SetFinishEffect);
            }
        }

        private void UpdateBattleState_StopByEvent()
        {
            UpdateBattleState_PlayerEscapeSuccess();
        }

        private void UpdateBattleState_PlayerEscapeFail()
        {
            if (battleStateFrameCount >= 90)
            {
                activeCharacter.ExecuteCommandEnd();

                battleViewer.CloseWindow();

                battleEvents.start(Rom.Script.Trigger.BATTLE_AFTER_ACTION);

                ChangeBattleState(BattleState.CheckBattleCharacterDown1);
            }
        }

        private void UpdateBattleState_MonsterEscape()
        {
            if (battleStateFrameCount >= 60)
            {
                battleViewer.CloseWindow();

                var escapedMonster = (BattleEnemyData)activeCharacter;

                enemyData.Remove(escapedMonster);
                targetEnemyData.Remove(escapedMonster);

                battleEvents.start(Rom.Script.Trigger.BATTLE_AFTER_ACTION);

                ChangeBattleState(BattleState.BattleFinishCheck1);
            }
        }

        private void UpdateBattleState_SetFinishEffect()
        {
            if (catalog.getItemFromGuid(gameSettings.transitionBattleLeave) == null)
                fadeScreenColorTweener.Begin(new Color(Color.Black, 0), new Color(Color.Black, 255), 30);
            else
                owner.mapScene.SetWipe(gameSettings.transitionBattleLeave);
            ChangeBattleState(BattleState.FinishFadeOut);
        }

        private void UpdateBattleState_FinishFadeOut()
        {
            if (battleEvents.isBusy())
                return;
            battleEvents.clearCurrentProcessingTrigger();

            if (catalog.getItemFromGuid(gameSettings.transitionBattleLeave) == null)
                fadeScreenColorTweener.Update();

            if (!fadeScreenColorTweener.IsPlayTween && !owner.mapScene.IsWiping())
            {
                owner.mapScene.SetWipe(Guid.Empty);
                fadeScreenColorTweener.Begin(new Color(Color.Black, 255), new Color(Color.Black, 0), 15);

                IsDrawingBattleScene = false;
                ((BattleViewer3D)battleViewer).Hide();

                ChangeBattleState(BattleState.FinishFadeIn);
            }
        }

        private void UpdateBattleState_FinishFadeIn()
        {
            fadeScreenColorTweener.Update();

            if (!fadeScreenColorTweener.IsPlayTween)
            {
                IsPlayingBattleEffect = false;
            }
        }

        private void SkipPlayerBattleCommand(bool inIsEscape = false)
        {
            foreach (var player in playerData)
            {
                player.selectedBattleCommandType = BattleCommandType.Skip;
            }

            if (inIsEscape)
            {
                //playerData[0].selectedBattleCommandType = BattleCommandType.PlayerEscape;
                activeCharacter.selectedBattleCommandType = BattleCommandType.PlayerEscape;
            }

            ChangeBattleState(BattleState.SortBattleActions);
        }

        private bool isQualifiedSkillCostItem(BattleCharacterBase activeCharacter, Common.Rom.NSkill skill)
        {
            if (activeCharacter is BattleEnemyData)
                return true;

            return party.isOKToConsumptionItem(skill.option.consumptionItem, skill.option.consumptionItemAmount);
        }

        public override bool isContinuable()
        {
            return BattleResult == BattleResultState.Lose_Continue ||
                   BattleResult == BattleResultState.Escape ||
                   BattleResult == BattleResultState.Lose_Advanced_GameOver;
        }

        private void setAttributeWithWeaponDamage(BattleCharacterBase target, AttackAttributeType attackAttribute, int elementAttack)
        {
            var condition = catalog.getItemFromGuid<Rom.Condition>(attackAttribute);

            if (condition == null)
            {
                return;
            }

            var percent = target.GetResistanceAilmentStatus(attackAttribute);

            if (battleRandom.Next(100) < condition.SuccessRate * (100 - percent) / 100)
            {
                target.SetCondition(catalog, attackAttribute, battleEvents);

                if (!condition.deadCondition)
                {
                    if (!displayedSetConditionsDic.ContainsKey(target))
                    {
                        displayedSetConditionsDic.Add(target, new List<Rom.Condition>());
                    }

                    // 連続攻撃で、同じ状態が付加されないようにチェック
                    // Check so that the same state is not added in consecutive attacks
                    if (!displayedSetConditionsDic[target].Contains(condition) && target.conditionInfoDic.ContainsKey(attackAttribute))
                    {
                        displayedSetConditionsDic[target].Add(condition);
                    }
                }
                else if (condition.deadConditionPercent == 0)
                {
                    target.HitPoint = 0;
                    target.consumptionStatusValue.SetStatus(catalog.getGameSettings().maxHPStatusID, 0);
                }
            }
        }

        private List<BattleCharacterBase> createBattleCharacterList()
        {
            var battleCharacters = new List<BattleCharacterBase>();
            battleCharacters.AddRange(targetPlayerData);
            battleCharacters.AddRange(targetEnemyData);
            return battleCharacters;
        }

        private bool isReady3DCamera()
        {
            if (battleViewer is BattleViewer3D)
            {
                var bv3d = battleViewer as BattleViewer3D;
#if false
                return bv3d.getCurrentCameraTag() != BattleCameraController.TAG_FORCE_WAIT;
#else
                return !bv3d.camManager.isPlayAnim || bv3d.camManager.isWaitCameraPlaying || bv3d.camManager.isSkillCameraPlaying(true);
#endif
            }

            return true;
        }

        private bool isReadyActor()
        {
            if (battleViewer is BattleViewer3D)
            {
                var bv3d = battleViewer as BattleViewer3D;
                return bv3d.isActiveCharacterReady();
            }

            return true;
        }


        private bool CheckAndDoReTarget()
        {
            // 攻撃対象を変える必要があるかチェック
            // Check if the attack target needs to be changed
            if (IsReTarget(activeCharacter))
            {
                bool isFriendRecoveryDownStatus = false;

                switch (activeCharacter.selectedBattleCommandType)
                {
                    case BattleCommandType.SameSkillEffect:
                        {
                            var skill = catalog.getItemFromGuid(activeCharacter.selectedBattleCommand.refGuid) as Rom.NSkill;

                            isFriendRecoveryDownStatus = (skill.friendEffect != null && skill.friendEffect.HasDeadCondition(catalog) && skill.option.onlyForDown);
                        }
                        break;

                    case BattleCommandType.Skill:
                        isFriendRecoveryDownStatus = (
                            activeCharacter.selectedSkill.friendEffect != null &&
                            activeCharacter.selectedSkill.friendEffect.HasDeadCondition(catalog) &&
                            activeCharacter.selectedSkill.option.onlyForDown);
                        break;

                    case BattleCommandType.Item:
                        if (activeCharacter.selectedItem.item.expendable != null)
                        {
                            isFriendRecoveryDownStatus = activeCharacter.selectedItem.item.expendable.HasRecoveryDeadCondition(catalog);
                        }
                        else if (activeCharacter.selectedItem.item.expendableWithSkill != null)
                        {
                            var skill = catalog.getItemFromGuid(activeCharacter.selectedItem.item.expendableWithSkill.skill) as Common.Rom.NSkill;

                            isFriendRecoveryDownStatus = (skill.friendEffect != null && skill.friendEffect.HasDeadCondition(catalog) && skill.option.onlyForDown);
                        }
                        break;
                    case BattleCommandType.Position:
                        activeCharacter.targetCharacter = null;
                        return false;
                }

                activeCharacter.targetCharacter = ReTarget(activeCharacter, isFriendRecoveryDownStatus);

                GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, activeCharacter.Name,
                    string.Format("Retarget / Type : {0}", activeCharacter.selectedBattleCommandType.ToString()));

                return false;
            }

            return true;
        }

        private void UpdateCommandSelect()
        {
            var battleCommandChoiceWindowDrawer = battleViewer.battleCommandChoiceWindowDrawer;
            var commandTargetWindowDrawer = battleViewer.commandTargetSelector;
            var skillSelectWindowDrawer = battleViewer.skillSelectWindowDrawer;
            var itemSelectWindowDrawer = battleViewer.itemSelectWindowDrawer;
            var stockSelectWindowDrawer = battleViewer.stockSelectWindowDrawer;

            if (commandSelectPlayer != null && commandSelectPlayer.commandTargetList.Count > 0)
            {
                foreach (var target in commandSelectPlayer.commandTargetList) target.IsSelect = false;
            }

            switch (battleCommandState)
            {
                case SelectBattleCommandState.CommandSelect:
                    //battleCommandChoiceWindowDrawer.Update();

                    if (Viewer.ui.command.Decided &&
                        Viewer.ui.command.Index >= 0 &&
                        battleCommandChoiceWindowDrawer.GetChoicesData()[Viewer.ui.command.Index].enable)
                    //if (battleCommandChoiceWindowDrawer.CurrentSelectItemEnable && (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE, Input.GameState.MENU) || battleCommandChoiceWindowDrawer.decided))
                    {
                        battleCommandChoiceWindowDrawer.saveSelected();
                        battleCommandChoiceWindowDrawer.decided = false;
                        Audio.PlaySound(owner.se.decide);

                        commandSelectPlayer.selectedBattleCommand = playerBattleCommand[Viewer.ui.command.Index];

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
                            case BattleCommand.CommandType.POSITION: battleCommandState = SelectBattleCommandState.Position_MakeTargetList; break;
                        }

                    }

                    else if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CANCEL, Input.GameState.MENU))
                    {
                        Audio.PlaySound(owner.se.cancel);
                        commandSelectPlayer.selectedBattleCommandType = BattleCommandType.Nothing;
                        battleCommandState = SelectBattleCommandState.CommandCancel;
                    }
                    break;

                // 通常攻撃
                // normal attack
                case SelectBattleCommandState.Attack_Command:

                    {
                        battleCommandState = SelectBattleCommandState.Attack_MakeTargetList;
                    }
                    break;

                case SelectBattleCommandState.Attack_MakeTargetList:
                {
                    commandSelectPlayer.commandTargetList.Clear();
                    commandTargetWindowDrawer.Clear();

                    commandSelectPlayer.commandTargetList.AddRange(targetEnemyData.Where(enemy => (enemy.HitPoint > 0) && IsHitRange(commandSelectPlayer, enemy)));
                    commandTargetWindowDrawer.AddBattleCharacters(commandSelectPlayer.commandTargetList);

                    commandTargetWindowDrawer.ResetSelect(commandSelectPlayer);

                    battleCommandState = SelectBattleCommandState.Attack_SelectTarget;
                }
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

                    if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE, Input.GameState.MENU) || isDecide)
                    {
                        Audio.PlaySound(owner.se.decide);
                        commandTargetWindowDrawer.saveSelect();
                        commandSelectPlayer.selectedBattleCommandType = BattleCommandType.Attack;
                        battleCommandState = SelectBattleCommandState.CommandEnd;
                    }
                    if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CANCEL, Input.GameState.MENU))
                    {
                        Audio.PlaySound(owner.se.cancel);

                        commandTargetWindowDrawer.Clear();

                        battleViewer.ClearDisplayMessage();
                        battleViewer.OpenWindow(WindowType.PlayerCommandWindow);

                        battleCommandState = SelectBattleCommandState.CommandSelect;
                    }
                    break;
                }

                // 防御
                // defense
                case SelectBattleCommandState.Guard_Command:
                    commandSelectPlayer.selectedBattleCommandType = BattleCommandType.Guard;
                    battleCommandState = SelectBattleCommandState.CommandEnd;
                    break;

                // チャージ
                // charge
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
                        var skill = catalog.getItemFromGuid(commandSelectPlayer.selectedBattleCommand.refGuid) as Rom.NSkill;
                        commandSelectPlayer.selectedSkill = skill;

                        switch (skill.option.target)
                        {
                            case Rom.TargetType.PARTY_ONE:
                            case Rom.TargetType.PARTY_ONE_ENEMY_ALL:
                                commandSelectPlayer.commandTargetList.AddRange(targetPlayerData.Where(x => IsHitRange(commandSelectPlayer, skill.Range, x)));

                                commandTargetWindowDrawer.AddBattleCharacters(commandSelectPlayer.commandTargetList);

                                commandTargetWindowDrawer.ResetSelect(commandSelectPlayer);

                                battleViewer.OpenWindow(WindowType.CommandTargetPlayerListWindow);
                                battleCommandState = SelectBattleCommandState.SkillSameEffect_SelectTarget;

                                windowType = WindowType.CommandTargetPlayerListWindow;
                                break;

                            case Rom.TargetType.ENEMY_ONE:
                            case Rom.TargetType.PARTY_ALL_ENEMY_ONE:
                            case Rom.TargetType.SELF_ENEMY_ONE:
                            case Rom.TargetType.OTHERS_ENEMY_ONE:
                                commandSelectPlayer.commandTargetList.AddRange(targetEnemyData.Where(enemy => (enemy.HitPoint > 0) && IsHitRange(commandSelectPlayer, skill.Range, enemy)));

                                commandTargetWindowDrawer.AddBattleCharacters(commandSelectPlayer.commandTargetList);

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

                    if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE, Input.GameState.MENU) || isDecide)
                    {
                        Audio.PlaySound(owner.se.decide);
                        commandTargetWindowDrawer.saveSelect();

                        commandSelectPlayer.selectedBattleCommandType = BattleCommandType.SameSkillEffect;
                        battleCommandState = SelectBattleCommandState.CommandEnd;
                    }
                    if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CANCEL, Input.GameState.MENU))
                    {
                        Audio.PlaySound(owner.se.cancel);

                        commandTargetWindowDrawer.Clear();

                        battleViewer.ClearDisplayMessage();
                        battleViewer.OpenWindow(WindowType.PlayerCommandWindow);

                        battleCommandState = SelectBattleCommandState.CommandSelect;
                    }
                    break;
                }

                // スキル
                // skill
                case SelectBattleCommandState.Skill_Command:
                    battleCommandState = SelectBattleCommandState.Skill_SelectSkill;
                    Viewer.ui.skillList = layoutDic.ContainsKey(commandSelectPlayer.selectedBattleCommand.refGuid) ? layoutDic[commandSelectPlayer.selectedBattleCommand.refGuid] : null;
                    battleViewer.OpenWindow(WindowType.SkillListWindow);

                    skillSelectWindowDrawer.SelectDefaultItem(commandSelectPlayer, battleState);
                    skillSelectWindowDrawer.HeaderTitleIcon = commandSelectPlayer.selectedBattleCommand.icon;
                    skillSelectWindowDrawer.HeaderTitleText = commandSelectPlayer.selectedBattleCommand.name;
                    skillSelectWindowDrawer.FooterTitleIcon = null;
                    skillSelectWindowDrawer.FooterTitleText = "";
                    skillSelectWindowDrawer.FooterSubDescriptionText = "";

                    MakeSkillList();
                    break;

                case SelectBattleCommandState.Skill_SelectSkill:
                    //skillSelectWindowDrawer.FooterMainDescriptionText = "";
                    //skillSelectWindowDrawer.Update();

                    //if (skillSelectWindowDrawer.CurrentSelectItemType == ChoiceWindowDrawer.ItemType.Item && skillSelectWindowDrawer.ChoiceItemCount > 0)
                    //{
                    //    commandSelectPlayer.selectedSkill = commandSelectPlayer.useableSkillList[skillSelectWindowDrawer.CurrentSelectItemIndex];

                    //    skillSelectWindowDrawer.FooterTitleIcon = commandSelectPlayer.selectedSkill.icon;
                    //    skillSelectWindowDrawer.FooterTitleText = commandSelectPlayer.selectedSkill.name;
                    //    skillSelectWindowDrawer.FooterMainDescriptionText = Common.Util.createSkillDescription(gameSettings.glossary, commandSelectPlayer.selectedSkill);
                    //}

                    // 決定
                    // decision
                    if (Viewer.ui.skillList.Decided &&
                        Viewer.ui.skillList.Index >= 0 &&
                        commandSelectPlayer.useableSkillList.Count > Viewer.ui.skillList.Index &&
                        skillSelectWindowDrawer.GetChoicesData()[Viewer.ui.skillList.Index].enable)
                    //if ((Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE, Input.GameState.MENU) || skillSelectWindowDrawer.decided) && skillSelectWindowDrawer.CurrentSelectItemType == ChoiceWindowDrawer.ItemType.Item && commandSelectPlayer.useableSkillList.Count > 0 && skillSelectWindowDrawer.CurrentSelectItemEnable)
                    {
                        commandSelectPlayer.selectedSkill = commandSelectPlayer.useableSkillList[Viewer.ui.skillList.Index];

                        skillSelectWindowDrawer.saveSelected();
                        skillSelectWindowDrawer.decided = false;
                        Audio.PlaySound(owner.se.decide);

                        battleCommandState = SelectBattleCommandState.Skill_MakeTargetList;
                    }

                    // キャンセル
                    // cancel
                    if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CANCEL, Input.GameState.MENU))
                    //if (((Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE, Input.GameState.MENU) || skillSelectWindowDrawer.decided) && skillSelectWindowDrawer.CurrentSelectItemType == ChoiceWindowDrawer.ItemType.Cancel) || Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CANCEL, Input.GameState.MENU))
                    {
                        skillSelectWindowDrawer.decided = false;
                        Audio.PlaySound(owner.se.cancel);

                        battleViewer.ClearDisplayMessage();
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
                                commandSelectPlayer.commandTargetList.AddRange(targetPlayerData.Where(x => IsHitRange(commandSelectPlayer, commandSelectPlayer.selectedSkill.Range, x)));

                                commandTargetWindowDrawer.AddBattleCharacters(commandSelectPlayer.commandTargetList);

                                commandTargetWindowDrawer.ResetSelect(commandSelectPlayer);

                                battleViewer.OpenWindow(WindowType.CommandTargetPlayerListWindow);
                                battleCommandState = SelectBattleCommandState.Skill_SelectTarget;

                                windowType = WindowType.CommandTargetPlayerListWindow;
                                break;

                            case Rom.TargetType.ENEMY_ONE:
                            case Rom.TargetType.PARTY_ALL_ENEMY_ONE:
                            case Rom.TargetType.SELF_ENEMY_ONE:
                            case Rom.TargetType.OTHERS_ENEMY_ONE:
                                commandSelectPlayer.commandTargetList.AddRange(targetEnemyData.Where(enemy => (enemy.HitPoint > 0) && IsHitRange(commandSelectPlayer, commandSelectPlayer.selectedSkill.Range, enemy)));

                                commandTargetWindowDrawer.AddBattleCharacters(commandSelectPlayer.commandTargetList);

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

                        if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE, Input.GameState.MENU) || isDecide)
                        {
                            Audio.PlaySound(owner.se.decide);
                            commandTargetWindowDrawer.saveSelect();

                            commandSelectPlayer.selectedBattleCommandType = BattleCommandType.Skill;
                            battleCommandState = SelectBattleCommandState.CommandEnd;
                        }
                    }

                    if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CANCEL, Input.GameState.MENU))
                    {
                        Audio.PlaySound(owner.se.cancel);

                        commandTargetWindowDrawer.Clear();

                        battleViewer.OpenWindow(WindowType.SkillListWindow);

                        battleCommandState = SelectBattleCommandState.Skill_Command;
                    }
                    break;
                }

                // アイテム
                // item
                case SelectBattleCommandState.Item_Command:
                    Viewer.ui.itemList = layoutDic.ContainsKey(commandSelectPlayer.selectedBattleCommand.refGuid) ? layoutDic[commandSelectPlayer.selectedBattleCommand.refGuid] : null;

                    itemSelectWindowDrawer.SelectDefaultItem(commandSelectPlayer, battleState);
                    itemSelectWindowDrawer.HeaderTitleIcon = commandSelectPlayer.selectedBattleCommand.icon;
                    itemSelectWindowDrawer.HeaderTitleText = commandSelectPlayer.selectedBattleCommand.name;
                    itemSelectWindowDrawer.FooterTitleIcon = null;
                    itemSelectWindowDrawer.FooterTitleText = "";
                    itemSelectWindowDrawer.FooterSubDescriptionText = "";

                    commandSelectPlayer.haveItemList.Clear();
                    itemSelectWindowDrawer.ClearChoiceListData();

                    var expendableItems = party.Items.Where(itemData => itemData.item.IsExpandable && !itemData.item.IsExpandableWithSkill && itemData.item.expendable.availableInBattle);
                    var skillItems = party.Items.Where(itemData => itemData.item.IsExpandableWithSkill && itemData.item.expendableWithSkill.availableInBattle);
                    var useableItems = expendableItems.Union(skillItems);

                    var filterTagProperties = Viewer.ui.itemList.FilterTagProperties;

                    if ((filterTagProperties?.Count ?? 0) == 0)
                    {
                        commandSelectPlayer.haveItemList.AddRange(useableItems);
                    }
                    else
                    {
                        commandSelectPlayer.haveItemList.AddRange(useableItems.Where(x => ItemFilter.FilterRomItemWithUserTag(x.item, filterTagProperties)));
                    }

                    foreach (var itemData in commandSelectPlayer.haveItemList)
                    {
                        int itemCount = itemData.num;

                        if (itemData.item.IsExpandable)
                        {
                            // 既にアイテムを使おうとしているメンバーがいたらその分だけ個数を減らす
                            // If there are members already trying to use the item, reduce the number accordingly.
                            itemCount -= (playerData.Count(player => (player != commandSelectPlayer && player.selectedBattleCommandType == BattleCommandType.Item) && (player.selectedItem.item == itemData.item)));
                        }

                        bool useableItem = (itemCount > 0);

                        if (iconTable.ContainsKey(itemData.item.icon.guId))
                        {
                            itemSelectWindowDrawer.AddChoiceData(iconTable[itemData.item.icon.guId], itemData.item.icon, itemData.item.name, itemCount, itemData.item, useableItem);
                        }
                        else
                        {
                            itemSelectWindowDrawer.AddChoiceData(itemData.item.name, itemCount, itemData.item, useableItem);
                        }
                    }

                    battleViewer.OpenWindow(WindowType.ItemListWindow);
                    battleCommandState = SelectBattleCommandState.Item_SelectItem;
                    break;

                case SelectBattleCommandState.Item_SelectItem:
                    //itemSelectWindowDrawer.FooterMainDescriptionText = "";
                    //itemSelectWindowDrawer.Update();

                    //if (itemSelectWindowDrawer.CurrentSelectItemType == ChoiceWindowDrawer.ItemType.Item && itemSelectWindowDrawer.ChoiceItemCount > 0)
                    //{
                    //    commandSelectPlayer.selectedItem = commandSelectPlayer.haveItemList[itemSelectWindowDrawer.CurrentSelectItemIndex];

                    //    itemSelectWindowDrawer.FooterTitleText = commandSelectPlayer.selectedItem.item.name;
                    //    itemSelectWindowDrawer.FooterMainDescriptionText = commandSelectPlayer.selectedItem.item.description;
                    //    //itemSelectWindowDrawer.FooterSubDescriptionText = string.Format("所持数 {0, 4}個", commandSelectPlayer.selectedItem.num);
                    // //itemSelectWindowDrawer.FooterSubDescriptionText = string.Format(\
                    //}

                    // 決定
                    // decision
                    if (Viewer.ui.skillList.Decided &&
                        Viewer.ui.itemList.Index >= 0 &&
                        commandSelectPlayer.haveItemList.Count > Viewer.ui.itemList.Index &&
                        itemSelectWindowDrawer.GetChoicesData()[Viewer.ui.itemList.Index].enable)
                    //if ((Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE, Input.GameState.MENU) || itemSelectWindowDrawer.decided) && itemSelectWindowDrawer.CurrentSelectItemType == ChoiceWindowDrawer.ItemType.Item && itemSelectWindowDrawer.CurrentSelectItemEnable)
                    {
                        commandSelectPlayer.selectedItem = commandSelectPlayer.haveItemList[Viewer.ui.itemList.Index];

                        itemSelectWindowDrawer.saveSelected();
                        itemSelectWindowDrawer.decided = false;
                        Audio.PlaySound(owner.se.decide);

                        battleCommandState = SelectBattleCommandState.Item_MakeTargetList;
                    }

                    // キャンセル
                    // cancel
                    if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CANCEL, Input.GameState.MENU))
                    //if (((Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE, Input.GameState.MENU) || itemSelectWindowDrawer.decided) && itemSelectWindowDrawer.CurrentSelectItemType == ChoiceWindowDrawer.ItemType.Cancel) || Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CANCEL, Input.GameState.MENU))
                    {
                        itemSelectWindowDrawer.decided = false;
                        Audio.PlaySound(owner.se.cancel);

                        battleViewer.ClearDisplayMessage();
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

                        // スキルを優先にしないと、消耗品は全てプレイヤーが対象になってしまう
                        // If you don't prioritize skills, all consumables will target the player
                        if (item.IsExpandableWithSkill)
                        {
                            var skill = catalog.getItemFromGuid(item.expendableWithSkill.skill) as Rom.NSkill;

                            if (skill != null)
                            {
                                switch (skill.option.target)
                                {
                                    case Rom.TargetType.PARTY_ONE:
                                    case Rom.TargetType.PARTY_ONE_ENEMY_ALL:
                                        foreach (BattlePlayerData player in targetPlayerData)
                                        {
                                            if (player.player.isAvailableItem(item) && IsHitRange(commandSelectPlayer, item, player))
                                            {
                                                commandSelectPlayer.commandTargetList.Add(player);
                                                windowType = WindowType.CommandTargetPlayerListWindow;
                                            }
                                        }

                                        commandTargetWindowDrawer.AddBattleCharacters(commandSelectPlayer.commandTargetList);
                                        break;

                                    case Rom.TargetType.SELF_ENEMY_ONE:
                                    case Rom.TargetType.ENEMY_ONE:
                                    case Rom.TargetType.PARTY_ALL_ENEMY_ONE:
                                    case Rom.TargetType.OTHERS_ENEMY_ONE:
                                    {
                                        foreach (var monster in targetEnemyData.Where(enemy => enemy.HitPoint > 0))
                                        {
                                            if (IsHitRange(commandSelectPlayer, item, monster))
                                            {
                                                commandSelectPlayer.commandTargetList.Add(monster);
                                            }
                                            windowType = WindowType.CommandTargetMonsterListWindow;
                                        }

                                        commandTargetWindowDrawer.AddBattleCharacters(commandSelectPlayer.commandTargetList);
                                    }
                                    break;

                                    default:
                                        commandSelectPlayer.selectedBattleCommandType = BattleCommandType.Item;
                                        battleCommandState = SelectBattleCommandState.CommandEnd;
                                        return;
                                }
                            }
                        }
                        else if (item.IsExpandable)
                        {
                            {
                                foreach (var player in playerData)
                                {
                                    player.IsSelectDisabled = !player.player.isAvailableItem(item);
                                }

                                commandSelectPlayer.commandTargetList.AddRange(targetPlayerData.Where(target => IsHitRange(commandSelectPlayer, item, target)));
                                commandTargetWindowDrawer.AddBattleCharacters(commandSelectPlayer.commandTargetList);
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

                    if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE, Input.GameState.MENU) || isDecide)
                    {
                        if (!(commandTargetWindowDrawer.CurrentSelectCharacter?.IsSelectDisabled ?? true))
                        {
                            Audio.PlaySound(owner.se.decide);
                            commandTargetWindowDrawer.saveSelect();

                            commandSelectPlayer.selectedBattleCommandType = BattleCommandType.Item;
                            battleCommandState = SelectBattleCommandState.CommandEnd;

                            break;
                        }
                    }
                    else if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CANCEL, Input.GameState.MENU))
                    {
                        Audio.PlaySound(owner.se.cancel);

                        commandTargetWindowDrawer.Clear();

                        battleViewer.OpenWindow(WindowType.ItemListWindow);

                        battleCommandState = SelectBattleCommandState.Item_Command;
                    }
                    break;
                }



                // 場所変え
                // change place
                case SelectBattleCommandState.Position_MakeTargetList:
                {
                    commandSelectPlayer.commandTargetList.Clear();
                    commandTargetWindowDrawer.Clear();

                    var windowType = WindowType.CommandTargetPlayerListWindow;

                    // 順番が回ってきた時に解除されている可能性があるので、強制的にパーティの後ろに移動しているキャストがいても選択はさせる
                    // There is a possibility that it will be canceled when the turn comes around, so even if there is a cast who is forcibly moving behind the party, it will be selected.
                    foreach (var player in playerData)
                    {
                            if (gameSettings.usePositionBack)
                            {
                                player.IsSelectDisabled = player.IsBehindPartyCondition();
                            }
                            else
                            {
                                player.IsSelectDisabled = false;
                            }
                        }

                    // 自分自身を外す
                    // remove yourself
                    commandSelectPlayer.commandTargetList.AddRange(targetPlayerData.Where(target => commandSelectPlayer != target));

                    commandTargetWindowDrawer.AddBattleCharacters(commandSelectPlayer.commandTargetList);

                    commandTargetWindowDrawer.ResetSelect(commandSelectPlayer);

                    battleViewer.OpenWindow(windowType);
                    battleCommandState = SelectBattleCommandState.Position_SelectTarget;
                    battleViewer.SetDisplayMessage(gameSettings.glossary.battle_positiontarget, windowType);
                }
                break;

                case SelectBattleCommandState.Position_SelectTarget:
                {
                    bool isDecide = commandTargetWindowDrawer.InputUpdate();

                    if (commandTargetWindowDrawer.Count > 0)
                    {
                        commandTargetWindowDrawer.CurrentSelectCharacter.IsSelect = true;
                    }

                    if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE, Input.GameState.MENU) || isDecide)
                    {
                        if (!(commandTargetWindowDrawer.CurrentSelectCharacter?.IsSelectDisabled ?? true))
                        {
                            Audio.PlaySound(owner.se.decide);
                            commandTargetWindowDrawer.saveSelect();

                            commandSelectPlayer.selectedBattleCommandType = BattleCommandType.Position;
                            battleCommandState = SelectBattleCommandState.CommandEnd;

                            break;
                        }
                    }
                    else if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CANCEL, Input.GameState.MENU))
                    {
                        Audio.PlaySound(owner.se.cancel);

                        battleViewer.ClearDisplayMessage();
                        battleViewer.OpenWindow(WindowType.PlayerCommandWindow);

                        battleCommandState = SelectBattleCommandState.CommandSelect;
                    }
                    break;
                }

                // 逃げる
                // run away
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

        public void MakeSkillList()
        {
            var skillSelectWindowDrawer = battleViewer.skillSelectWindowDrawer;

            commandSelectPlayer.useableSkillList.Clear();
            skillSelectWindowDrawer.ClearChoiceListData();

            var filterTagProperties = Viewer.ui.skillList.FilterTagProperties;

            if ((filterTagProperties?.Count ?? 0) == 0)
            {
                commandSelectPlayer.useableSkillList.AddRange(commandSelectPlayer.player.skills);
            }
            else
            {
                commandSelectPlayer.useableSkillList.AddRange(commandSelectPlayer.player.skills.Where(x => ItemFilter.FilterRomItemWithUserTag(x, filterTagProperties)));
            }

            Viewer.ui.skillList.FilteredSkills = commandSelectPlayer.useableSkillList;
            Viewer.ui.skillList.FilteredSkillUser = commandSelectPlayer.player;

            foreach (var skill in commandSelectPlayer.useableSkillList)
            {
                bool useableSkill = skill.option.availableInBattle &&
                    IsQualifiedSkillCostStatus(commandSelectPlayer, skill) &&
                    isQualifiedSkillCostItem(commandSelectPlayer, skill);

                if (iconTable.ContainsKey(skill.icon.guId))
                {
                    skillSelectWindowDrawer.AddChoiceData(iconTable[skill.icon.guId], skill.icon, skill.name, useableSkill);
                }
                else
                {
                    skillSelectWindowDrawer.AddChoiceData(skill.name, useableSkill);
                }
            }
        }

        private BattleCharacterBase[] MakeTargetList(Common.Rom.NSkill skill)
        {
            List<BattleCharacterBase> targets = new List<BattleCharacterBase>();

            switch (skill.option.target)
            {
                case Rom.TargetType.ALL:
                    break;
            }

            return targets.ToArray();
        }
        private BattleCharacterBase[] MakeTargetList(Common.Rom.NItem item)
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

        public override void Draw()
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
                    battleViewer.Draw(playerViewData, enemyMonsterViewData);
                    Graphics.DrawFillRect(0, 0, Graphics.ScreenWidth, Graphics.ScreenHeight, fadeScreenColorTweener.CurrentValue.R, fadeScreenColorTweener.CurrentValue.G, fadeScreenColorTweener.CurrentValue.B, fadeScreenColorTweener.CurrentValue.A);
                    break;

                case BattleState.SetFinishEffect:
                case BattleState.FinishFadeOut:
                    DrawBackground();
                    if (!owner.IsBattle2D)
                        ((BattleViewer3D)battleViewer).DrawField(playerViewData, enemyMonsterViewData);
                    //resultViewer.Draw();
                    if (BattleResult != BattleResultState.Win)
                        battleViewer.Draw(playerViewData, enemyMonsterViewData);
                    Viewer.DrawResult(false, playerData, resultProperty);
                    Graphics.DrawFillRect(0, 0, Graphics.ScreenWidth, Graphics.ScreenHeight, fadeScreenColorTweener.CurrentValue.R, fadeScreenColorTweener.CurrentValue.G, fadeScreenColorTweener.CurrentValue.B, fadeScreenColorTweener.CurrentValue.A);
                    break;

                case BattleState.ResultInit:
                    DrawBackground();
                    if (!owner.IsBattle2D)
                        ((BattleViewer3D)battleViewer).DrawField(playerViewData, enemyMonsterViewData);
                    break;
                case BattleState.Result:
                    DrawBackground();
                    if (!owner.IsBattle2D)
                        ((BattleViewer3D)battleViewer).DrawField(playerViewData, enemyMonsterViewData);
                    Viewer.DrawResult(true, playerData, resultProperty);
                    //resultViewer.Draw();
                    break;

                default:
                    DrawBackground();
                    battleViewer.Draw(playerViewData, enemyMonsterViewData);
                    break;
            }


            Graphics.EndDraw();

            battleEvents?.Draw();
        }

        private void DrawBackground()
        {
            if (!owner.IsBattle2D)
                return;

            // 背景表示
            // background display
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
            //GameMain.PushLog(DebugDialog.LogEntry.LogType.BATTLE, "System", "Set State to : " + nextBattleState.ToString());
            battleStateFrameCount = 0;

            prevBattleState = battleState;
            battleState = nextBattleState;
        }

        public BattleResultState CheckBattleFinish()
        {
            var resultState = BattleResultState.NonFinish;

            // 戦闘終了条件
            // Combat end condition
            // 1.全ての敵がHP0なったら     -> 勝利 (イベント戦として倒してはいけない敵が登場するならゲームオーバーになる場合もありえる?)
            // 1. When all enemies have 0 HP -\u003e Victory
            // 2.全ての味方がHP0になったら -> 敗北 (ゲームオーバー or フィールド画面に戻る(イベント戦のような特別な戦闘を想定))
            // 2. When all allies' HP reaches 0 -\u003e Defeat
            // 3.「逃げる」コマンドの成功  -> 逃走
            // 3. \
            // 4.その他 強制的に戦闘を終了するスクリプト (例 HPが半分になったらイベントが発生し戦闘終了)
            // 4.Other Scripts to forcibly end the battle (ex. When the HP becomes half, an event occurs and the battle ends)
            // 「発動後に自分が戦闘不能になる」スキルなどによってプレイヤーとモンスターが同時に全滅した場合(条件の1と2を同時に満たす場合)は敗北扱いとする
            // If the player and monsters are annihilated at the same time by a skill such as \


            // 敵が全て倒れたら
            // when all the enemies fall
            if ((enemyData.Where(monster => monster.HitPoint > 0).Count() + stockEnemyData.Where(monster => monster.HitPoint > 0).Count()) == 0)
            {
                resultState = BattleResultState.Win;
            }

            // 味方のHPが全員0になったら
            // When all allies' HP reaches 0
            if ((playerData.Where(player => player.HitPoint > 0).Count() + stockPlayerData.Where(player => player.HitPoint > 0).Count()) == 0)
            {
                if (gameoverOnLose)
                {
                    if (owner.data.start.gameOverSettings.gameOverType != GameOverSettings.GameOverType.DEFAULT)
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
            // Is it specified on the battle event side?
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
                    // Add experience points, money and items
                    int totalMoney = 0;
                    int totalExp = 0;
                    var dropItems = new List<Rom.NItem>();

                    if (itemRate > 0)
                    {
                        var defeatEnemyDatas = new List<BattleEnemyData>(enemyData.Count + stockEnemyData.Count);

                        defeatEnemyDatas.AddRange(enemyData);
                        defeatEnemyDatas.AddRange(stockEnemyData);

                        foreach (var monsterData in enemyData)
                        {

                            var monster = monsterData.monster;

                            totalMoney += monsterData.RewardsGold * moneyRate / 100;
                            totalExp += monsterData.RewardsExp;

                            // アイテム抽選
                            // Item lottery
                            foreach (var dropItem in monster.dropItems)
                            {
                                if (dropItem.item != Guid.Empty)
                                {
                                    var item = catalog.getItemFromGuid(dropItem.item) as Common.Rom.NItem;

                                    if ((item != null) && (battleRandom.Next(100) < dropItem.percent * itemRate / 100))
                                    {
                                        if (item.useEnhance && (item.baseItem == null))
                                        {
                                            item = item.CreateEnhancedItem();

                                            catalog.addEnhancedItem(item);
                                        }

                                        if (catalog.getItemFromGuid<Rom.Event>(item.scriptOnNew) is Rom.Event ev)
                                        {
                                            battleEvents.AddEvent(ev, item);
                                        }

                                        dropItems.Add(item);
                                    }
                                }
                            }
                        }
                    }

                    resultViewer.SetResultData(playerViewData.ToArray(), totalExp, totalMoney, dropItems.ToArray(), party.ItemDict);

                    this.rewards.GetExp = totalExp;
                    this.rewards.DropMoney = totalMoney;
                    this.rewards.DropItems = dropItems.ToArray();


                    if (BattleResultWinEvents != null)
                    {
                        BattleResultWinEvents();
                    }
                }

                resultViewer.Start();
                ChangeBattleState(BattleState.ResultInit);
                break;

                case BattleResultState.Lose_GameOver:
                {
                    if (BattleResultLoseGameOverEvents != null)
                    {
                        BattleResultLoseGameOverEvents();
                    }
                }
                BattleResult = BattleResultState.Lose_GameOver;
                ChangeBattleState(BattleState.SetFinishEffect);
                break;

                case BattleResultState.Escape_ToTitle:
                    BattleResult = BattleResultState.Escape_ToTitle;
                    ChangeBattleState(BattleState.SetFinishEffect);
                    break;

                case BattleResultState.Lose_Continue:
                    BattleResult = BattleResultState.Lose_Continue;
                    ChangeBattleState(BattleState.SetFinishEffect);
                    break;

                case BattleResultState.Lose_Advanced_GameOver:
                    BattleResult = BattleResultState.Lose_Advanced_GameOver;
                    ChangeBattleState(BattleState.SetFinishEffect);
                    break;

                case BattleResultState.NonFinish:
                    // イベントで敵や味方を増やすなどして、戦闘終了条件を満たさなくなった場合にここに来るので、普通に次のターンにする
                    // If you increase the number of enemies and allies in the event and the battle end conditions are no longer met, you will come here, so normally you will make it the next turn.
                    ChangeBattleState(BattleState.ProcessPoisonStatus);

                    // イベントによりバトル継続になった場合、全員行動済みとしておく
                    // If the battle continues due to an event, all players will be treated as having completed their actions.
                    commandExecuteMemberCount = battleEntryCharacters.Count;
                    break;
            }
        }

        private void ApplyPlayerDataToGameData(bool applyHpMp)
        {
            for (int i = 0; i < playerData.Count; i++)
            {
                ApplyPlayerDataToGameData(playerData[i], applyHpMp);
            }
        }

        internal void ApplyPlayerDataToGameData(BattlePlayerData battlePlayerData, bool applyHpMp)
        {
            var gameData = battlePlayerData.player;

            if (applyHpMp)
            {
                foreach (var info in Catalog.sInstance.getGameSettings().CastStatusParamInfoList)
                {
                    if (info.Consumption)
                    {
                        gameData.consumptionStatusValue.SetStatus(info.guId, battlePlayerData.consumptionStatusValue.GetStatus(info.guId));
                    }
                }

                gameData.hitpoint = battlePlayerData.HitPoint;
                gameData.magicpoint = battlePlayerData.MagicPoint;
            }

            // まずはすべて適用する
            // apply all first
            gameData.conditionInfoDic.Clear();
            foreach (var e in battlePlayerData.conditionInfoDic)
            {
                gameData.conditionInfoDic.Add(e.Key, e.Value);
            }
            gameData.refreshConditionEffect();

            // 報酬系データの計算
            // Calculation of reward system data
            gameData.conditionAddExpRateForResult = gameData.conditionAddExpRate;
            itemRate = party.CalcConditionDropItemRate();
            moneyRate = party.CalcConditionRewardRate();

            // バトル終了時に解除される状態の解除
            // Canceling the state that is canceled at the end of the battle
            gameData.conditionInfoDic.Clear();
            foreach (var e in battlePlayerData.conditionInfoDic)
            {
                if ((e.Value.recovery & Hero.ConditionInfo.RecoveryType.BattleFinished) == 0)
                {
                    gameData.conditionInfoDic.Add(e.Key, e.Value);
                }
            }
            gameData.refreshConditionEffect();
        }

        private BattleCharacterBase GetAttackConditionTargetCharacter(BattleCharacterBase inCharacter)
        {
            foreach (var e in inCharacter.conditionInfoDic)
            {
                var condition = e.Value.rom;

                if (condition != null)
                {
                    if (condition.attack)
                    {
                        var tempTargets = new List<BattleCharacterBase>();

                        switch (condition.attackTarget)
                        {
                            case Rom.TargetType.PARTY_ALL:
                                tempTargets.AddRange(inCharacter.FriendPartyRefMember.Where(player => player.HitPoint > 0));
                                break;
                            case Rom.TargetType.ENEMY_ALL:
                                tempTargets.AddRange(inCharacter.EnemyPartyRefMember.Where(enemy => enemy.HitPoint > 0));
                                break;
                            case Rom.TargetType.SELF:
                                tempTargets.Add(inCharacter);
                                break;
                            case Rom.TargetType.OTHERS:
                                tempTargets.AddRange(inCharacter.FriendPartyRefMember.Where(player => (player != inCharacter) && player.HitPoint > 0));
                                break;
                            case Rom.TargetType.ALL:
                                tempTargets.AddRange(inCharacter.FriendPartyRefMember.Where(player => player.HitPoint > 0));
                                tempTargets.AddRange(inCharacter.EnemyPartyRefMember.Where(enemy => enemy.HitPoint > 0));
                                break;
                            case Rom.TargetType.OTHERS_ALL:
                                tempTargets.AddRange(inCharacter.FriendPartyRefMember.Where(player => (player != inCharacter) && player.HitPoint > 0));
                                tempTargets.AddRange(inCharacter.EnemyPartyRefMember.Where(enemy => enemy.HitPoint > 0));
                                break;
                            default:
                                break;
                        }

                        var targets = new List<BattleCharacterBase>();

                        targets.AddRange(tempTargets.Where(enemy => IsHitRange(inCharacter, enemy)));

                        if (targets.Count > 0)
                        {
                            return targets.ElementAt(battleRandom.Next(targets.Count()));
                        }

                        break;
                    }
                }
            }

            return null;
        }

        internal bool IsHitRange(BattleCharacterBase inCharacter, float inRange, BattleCharacterBase inTarget)
        {
            var gs = Catalog.sInstance.getGameSettings();

            if (!gs.useRange || (inRange == 0))
            {
                return true;
            }

            float distance;

            if (gs.checkRange == Rom.GameSettings.CheckRangeType.Line)
            {
                // Z行チェックの時の射程は１マス加算
                // When checking the Z line, the range is added by 1 square.
                inRange++;

                distance = Math.Abs(inCharacter.moveTargetPos.Z - inTarget.moveTargetPos.Z);
            }
            else
            {
                inRange = inRange * inRange;

                var diffX = inCharacter.moveTargetPos.X - inTarget.moveTargetPos.X;
                var diffZ = inCharacter.moveTargetPos.Z - inTarget.moveTargetPos.Z;

                distance = (diffX * diffX + diffZ * diffZ);
            }

            return (distance <= inRange);
        }

        internal bool IsHitRange(BattleCharacterBase inCharacter, BattleCharacterBase inTarget)
        {
            var hero = inCharacter.Hero;
            var weapon = hero.equipments[Hero.WEAPON_INDEX];
            var range = (weapon == null) ? hero.rom.range : weapon.range;

            return IsHitRange(inCharacter, range, inTarget);
        }

        internal bool IsHitRange(BattleCharacterBase inCharacter, Common.Rom.NItem inItem, BattleCharacterBase inTarget)
        {
            return IsHitRange(inCharacter, inItem.range, inTarget);
        }

        internal bool IsHit(BattleCharacterBase inCharacter, BattleCharacterBase inTarget)
        {
            if (!IsHitRange(inCharacter, inTarget))
            {
                return false;
            }

            return inCharacter.Dexterity * (100 - inTarget.Evasion) > battleRandom.Next(100 * 100);
        }

        internal BattleCharacterBase[] GetTargetCharacters(BattleCharacterBase character)
        {
            var targets = new List<BattleCharacterBase>();

            switch (character.selectedBattleCommandType)
            {
                case BattleCommandType.Attack:
                case BattleCommandType.Critical:
                case BattleCommandType.ForceCritical:
                case BattleCommandType.Miss:
                {
                    var attackConditionTarget = GetAttackConditionTargetCharacter(character);

                    if (attackConditionTarget != null)
                    {
                        targets.Add(attackConditionTarget);
                    }
                    else
                    {
                        targets.Add(battleViewer.commandTargetSelector.CurrentSelectCharacter);
                    }

                    if (character.selectedBattleCommandType == BattleCommandType.Critical)
                    {
                        // 必中ではないクリティカルが攻撃に失敗したらミス
                        // If a critical attack fails, it is a miss.
                        character.selectedBattleCommandType = IsHit(character, targets[0]) ? BattleCommandType.ForceCritical : BattleCommandType.Miss;
                    }
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
                    var skill = catalog.getItemFromGuid(character.selectedBattleCommand.refGuid) as Rom.NSkill;

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
                        case Rom.TargetType.ALL:
                            if (character.selectedSkill.friendEffect.HasDeadCondition(catalog) && character.selectedSkill.option.onlyForDown)
                            {
                                targets.AddRange(character.FriendPartyRefMember);
                            }
                            else
                            {
                                targets.AddRange(character.FriendPartyRefMember.Where(member => member.HitPoint > 0));
                            }
                            targets.AddRange(character.EnemyPartyRefMember.Where(enemy => enemy.HitPoint > 0));
                            break;

                        case Rom.TargetType.PARTY_ALL:
                            if (skill.friendEffect.HasDeadCondition(catalog) && skill.option.onlyForDown)
                            {
                                targets.AddRange(character.FriendPartyRefMember);
                            }
                            else
                            {
                                targets.AddRange(character.FriendPartyRefMember.Where(member => member.HitPoint > 0));
                            }
                            break;

                        case Rom.TargetType.ENEMY_ALL:
                            targets.AddRange(character.EnemyPartyRefMember.Where(enemy => enemy.HitPoint > 0));
                            break;

                        case Rom.TargetType.SELF:
                        case Rom.TargetType.SELF_ENEMY_ALL:
                            targets.Add(character);
                            break;

                        case Rom.TargetType.OTHERS:
                        case Rom.TargetType.OTHERS_ALL:
                            if (skill.friendEffect.HasDeadCondition(catalog) && skill.option.onlyForDown)
                            {
                                targets.AddRange(character.FriendPartyRefMember.Where(member => character != member));
                            }
                            else
                            {
                                targets.AddRange(character.FriendPartyRefMember.Where(member => character != member && member.HitPoint > 0));
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

                        case Rom.TargetType.ALL:
                            if (character.selectedSkill.friendEffect.HasDeadCondition(catalog) && character.selectedSkill.option.onlyForDown)
                            {
                                targets.AddRange(character.FriendPartyRefMember);
                            }
                            else
                            {
                                targets.AddRange(character.FriendPartyRefMember.Where(member => member.HitPoint > 0));
                            }
                            targets.AddRange(character.EnemyPartyRefMember.Where(enemy => enemy.HitPoint > 0));
                            break;

                        case Rom.TargetType.PARTY_ALL:
                            if (character.selectedSkill.friendEffect.HasDeadCondition(catalog) && character.selectedSkill.option.onlyForDown)
                            {
                                targets.AddRange(character.FriendPartyRefMember);
                            }
                            else
                            {
                                targets.AddRange(character.FriendPartyRefMember.Where(member => member.HitPoint > 0));
                            }
                            break;

                        case Rom.TargetType.ENEMY_ALL:
                            targets.AddRange(character.EnemyPartyRefMember.Where(enemy => enemy.HitPoint > 0));
                            break;

                        case Rom.TargetType.SELF:
                        case Rom.TargetType.SELF_ENEMY_ALL:
                            targets.Add(character);
                            break;

                        case Rom.TargetType.OTHERS:
                        case Rom.TargetType.OTHERS_ALL:
                            if (character.selectedSkill.friendEffect.HasDeadCondition(catalog) && character.selectedSkill.option.onlyForDown)
                            {
                                targets.AddRange(character.FriendPartyRefMember.Where(member => character != member));
                            }
                            else
                            {
                                targets.AddRange(character.FriendPartyRefMember.Where(member => character != member && member.HitPoint > 0));
                            }
                            break;
                    }

                    break;

                case BattleCommandType.Item:
                    if (character.selectedItem.item.IsExpandable)
                    {
                        targets.Add(battleViewer.commandTargetSelector.CurrentSelectCharacter);
                    }
                    else if (character.selectedItem.item.IsExpandableWithSkill)
                    {
                        var skill = (Common.Rom.NSkill)catalog.getItemFromGuid(character.selectedItem.item.expendableWithSkill.skill);

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

                                case Rom.TargetType.ALL:
                                    if (character.selectedSkill.friendEffect.HasDeadCondition(catalog) && character.selectedSkill.option.onlyForDown)
                                    {
                                        targets.AddRange(character.FriendPartyRefMember);
                                    }
                                    else
                                    {
                                        targets.AddRange(character.FriendPartyRefMember.Where(member => member.HitPoint > 0));
                                    }
                                    targets.AddRange(character.EnemyPartyRefMember.Where(enemy => enemy.HitPoint > 0));
                                    break;

                                case Rom.TargetType.PARTY_ALL:
                                    if (skill.friendEffect.HasDeadCondition(catalog) && skill.option.onlyForDown)
                                    {
                                        targets.AddRange(character.FriendPartyRefMember);
                                    }
                                    else
                                    {
                                        targets.AddRange(character.FriendPartyRefMember.Where(member => member.HitPoint > 0));
                                    }
                                    break;

                                case Rom.TargetType.ENEMY_ALL:
                                    targets.AddRange(character.EnemyPartyRefMember.Where(enemy => enemy.HitPoint > 0));
                                    break;

                                case Rom.TargetType.SELF:
                                case Rom.TargetType.SELF_ENEMY_ALL:
                                    targets.Add(character);
                                    break;

                                case Rom.TargetType.OTHERS:
                                case Rom.TargetType.OTHERS_ALL:
                                    if (skill.friendEffect.HasDeadCondition(catalog) && skill.option.onlyForDown)
                                    {
                                        targets.AddRange(character.FriendPartyRefMember.Where(member => character != member));
                                    }
                                    else
                                    {
                                        targets.AddRange(character.FriendPartyRefMember.Where(member => character != member && member.HitPoint > 0));
                                    }
                                    break;
                            }
                        }
                    }
                    break;

                case BattleCommandType.Position:
                    targets.Add(battleViewer.commandTargetSelector.CurrentSelectCharacter);
                    break;

                case BattleCommandType.Nothing:
                    //character.targetCharacter = new BattleCharacterBase[ 0 ];
                    break;
            }

            return targets.ToArray();
        }

        private bool IsReTarget(BattleCharacterBase character)
        {
            bool isRetarget = false;

            switch (character.selectedBattleCommandType)
            {
                case BattleCommandType.Attack:
                case BattleCommandType.Critical:
                case BattleCommandType.ForceCritical:
                case BattleCommandType.Miss:
                    // 状態異常時に、行動指定「何もしない」を行うと、targetがnullになっていることがある
                    // When the status is abnormal, if you specify the action \
                    if (character.targetCharacter == null)
                    {
                        isRetarget = true;
                        break;
                    }
                    foreach (var target in character.targetCharacter)
                    {
                        if (IsNotActiveTarget(target) || target.IsDeadCondition() || !IsHitRange(character, target))
                        {
                            isRetarget = true;
                            break;
                        }

                        // 敵の場合は「狙われやすさ」がセットされていたら必ず再抽選する
                        // For enemies, if the \
                        else if(character is BattleEnemyData && character.EnemyPartyRefMember.Exists(x => x.HateCondition != 0))
                        {
                            isRetarget = true;
                            break;
                        }
                    }
                    break;

                case BattleCommandType.SameSkillEffect:
                    foreach (var target in character.targetCharacter)
                    {
                        var skill = catalog.getItemFromGuid(character.selectedBattleCommand.refGuid) as Rom.NSkill;

                        switch (skill.option.target)
                        {
                            case Rom.TargetType.ENEMY_ONE:
                            case Rom.TargetType.PARTY_ALL_ENEMY_ONE:
                            case Rom.TargetType.SELF_ENEMY_ONE:
                            case Rom.TargetType.OTHERS_ENEMY_ONE:
                                if (skill.enemyEffect != null)
                                {
                                    if (IsNotActiveTarget(target) || target.IsDeadCondition() || !IsHitRange(character, skill.Range, target))
                                    {
                                        isRetarget = true;
                                    }
                                }
                                break;

                            case Rom.TargetType.PARTY_ONE:
                            case Rom.TargetType.PARTY_ONE_ENEMY_ALL:
                                if (skill.friendEffect != null)
                                {
                                    if (IsNotActiveTarget(target) || !IsHitRange(character, skill.Range, target))
                                    {
                                        isRetarget = true;
                                    }
                                    // 蘇生効果ありで対象が死んでいないか戦闘不能者のみの場合は無効
                                    // Invalid if there is a resurrection effect and the target is not dead or only incapacitated
                                    else if (skill.friendEffect.HasDeadCondition(catalog))
                                    {
                                        if (!target.IsDeadCondition() && skill.IsOnlyForDown(catalog))
                                            isRetarget = true;
                                    }
                                    else
                                    {
                                        if (target.IsDeadCondition())
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
                                    if (IsNotActiveTarget(target) || target.IsDeadCondition() || character.EnemyPartyRefMember.Exists(x => x.HateCondition != 0) || !IsHitRange(character, skill.Range, target))
                                    {
                                        isRetarget = true;
                                    }
                                }
                                break;

                            case Rom.TargetType.PARTY_ONE:
                            case Rom.TargetType.PARTY_ONE_ENEMY_ALL:
                                if (skill.friendEffect != null)
                                {
                                    if (IsNotActiveTarget(target) || !IsHitRange(character, skill.Range, target))
                                    {
                                        isRetarget = true;
                                    }
                                    // 蘇生効果ありで対象が死んでいないか戦闘不能者のみの場合は無効
                                    // Invalid if there is a resurrection effect and the target is not dead or only incapacitated
                                    else if (skill.friendEffect.HasDeadCondition(catalog))
                                    {
                                        if (!target.IsDeadCondition() && skill.IsOnlyForDown(catalog))
                                            isRetarget = true;
                                    }
                                    else
                                    {
                                        if (target.IsDeadCondition())
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
                        if (character.selectedItem.item.expendableWithSkill != null)
                        {
                            var skill = catalog.getItemFromGuid(character.selectedItem.item.expendableWithSkill.skill) as Common.Rom.NSkill;

                            if (skill != null)
                            {
                                switch (skill.option.target)
                                {
                                    case Rom.TargetType.PARTY_ONE:
                                    case Rom.TargetType.PARTY_ONE_ENEMY_ALL:
                                        if (IsNotActiveTarget(target) || target == null || !IsHitRange(character, skill.Range, target) || (skill.friendEffect != null
                                            && ((target.IsDeadCondition() != skill.friendEffect.HasDeadCondition(catalog)) && skill.IsOnlyForDown(catalog))))
                                        {
                                            isRetarget = true;
                                        }
                                        break;

                                    case Rom.TargetType.ENEMY_ONE:
                                    case Rom.TargetType.PARTY_ALL_ENEMY_ONE:
                                    case Rom.TargetType.SELF_ENEMY_ONE:
                                        if (IsNotActiveTarget(target) || target.IsDeadCondition() || !IsHitRange(character, skill.Range, target))
                                        {
                                            isRetarget = true;
                                        }
                                        break;
                                }
                            }
                        }
                        else if (character.selectedItem.item.expendable != null)
                        {
                            if (IsNotActiveTarget(target) || (target.IsDeadCondition() != character.selectedItem.item.expendable.HasRecoveryDeadCondition(catalog)))
                            {
                                isRetarget = true;
                            }
                        }
                    }
                    break;
                case BattleCommandType.Position:
                    if (character.targetCharacter.Length != 1)
                    {
                        isRetarget = true;
                    }
                    else if (gameSettings.useBehindParty)
                    {
                        isRetarget = character.targetCharacter[0].IsBehindPartyCondition() || character.IsBehindPartyCondition();
                    }
                    break;
            }

            return isRetarget;
        }

        private bool IsNotActiveTarget(BattleCharacterBase target)
        {
            if (playerData.Contains(target as BattlePlayerData))
                return false;
            if (enemyData.Contains(target as BattleEnemyData))
                return false;
            return true;
        }

        private BattleCharacterBase[] ReTarget(BattleCharacterBase character, bool isFriendRecoveryDownStatus)
        {
            var targets = new List<BattleCharacterBase>();
            var friendPartyMember = character.FriendPartyRefMember.Where(player => player.IsDeadCondition() == isFriendRecoveryDownStatus);
            var targetPartyMember = character.EnemyPartyRefMember.Where(enemy => !enemy.IsDeadCondition());

            switch (character.selectedBattleCommandType)
            {
                case BattleCommandType.Attack:
                case BattleCommandType.Critical:
                case BattleCommandType.ForceCritical:
                case BattleCommandType.Miss:
                {
                    var attackConditionTarget = GetAttackConditionTargetCharacter(character);

                    if (attackConditionTarget != null)
                    {
                        targets.Add(attackConditionTarget);
                    }
                    else
                    {
                        var tempTargets = new List<BattleCharacterBase>(targetPartyMember.Where(enemy => IsHitRange(character, enemy)));

                        if (tempTargets.Count() > 0)
                        {
                            targets.Add(TargetSelectWithHateRate(tempTargets, character as BattleEnemyData));
                        }
                    }
                }
                break;

                // どちらも対象が自分自身なので再抽選の必要は無し
                // In both cases, the target is yourself, so there is no need to re-lottery.
                case BattleCommandType.Charge:
                case BattleCommandType.Guard:
                    targets.Add(character);
                    break;

                case BattleCommandType.SameSkillEffect:
                {
                    var skill = catalog.getItemFromGuid(character.selectedBattleCommand.refGuid) as Rom.NSkill;

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

                    targets.RemoveAll(x => !IsHitRange(character, skill.Range, x));
                }
                break;

                case BattleCommandType.Skill:
                    switch (character.selectedSkill.option.target)
                    {
                        case Rom.TargetType.ENEMY_ONE:
                        case Rom.TargetType.PARTY_ALL_ENEMY_ONE:
                        case Rom.TargetType.SELF_ENEMY_ONE:
                        case Rom.TargetType.OTHERS_ENEMY_ONE:
                            if (targetPartyMember.Count() > 0) targets.Add(TargetSelectWithHateRate(targetPartyMember, character as BattleEnemyData));
                            break;
                        case Rom.TargetType.PARTY_ONE:
                        case Rom.TargetType.PARTY_ONE_ENEMY_ALL:
                            if (friendPartyMember.Count() > 0) targets.Add(friendPartyMember.ElementAt(battleRandom.Next(friendPartyMember.Count())));
                            break;
                    }

                    targets.RemoveAll(x => !IsHitRange(character, character.selectedSkill.Range, x));
                    break;

                case BattleCommandType.Item:
                    if (character.selectedItem.item.IsExpandable)
                    {
                        if (character.selectedItem.item.expendable.HasRecoveryDeadCondition(catalog))
                        {
                            var a = character.FriendPartyRefMember.Where(player => player.HitPoint == 0
                                && ((BattlePlayerData)player).player.isAvailableItem(character.selectedItem.item));

                            if (a.Count() > 0) targets.Add(a.ElementAt(battleRandom.Next(a.Count())));
                        }
                        else
                        {
                            var a = character.FriendPartyRefMember.Where(player => player.HitPoint > 0
                                && ((BattlePlayerData)player).player.isAvailableItem(character.selectedItem.item));

                            if (a.Count() > 0) targets.Add(a.ElementAt(battleRandom.Next(a.Count())));
                        }

                        targets.RemoveAll(x => !IsHitRange(character, character.selectedItem.item.Range, x));
                    }
                    else if (character.selectedItem.item.IsExpandableWithSkill)
                    {
                        var skill = catalog.getItemFromGuid(character.selectedItem.item.expendableWithSkill.skill) as Common.Rom.NSkill;

                        switch (skill.option.target)
                        {
                            case Rom.TargetType.PARTY_ONE:
                            case Rom.TargetType.PARTY_ONE_ENEMY_ALL:
                                if (skill.friendEffect.HasDeadCondition(catalog))
                                {
                                    var a = character.FriendPartyRefMember.Where(player => (skill.option.onlyForDown || player.HitPoint == 0)
                                        && ((BattlePlayerData)player).player.isAvailableItem(character.selectedItem.item));

                                    if (a.Count() > 0) targets.Add(a.ElementAt(battleRandom.Next(a.Count())));
                                }
                                else
                                {
                                    var a = character.FriendPartyRefMember.Where(player => player.HitPoint > 0
                                        && ((BattlePlayerData)player).player.isAvailableItem(character.selectedItem.item));

                                    if (a.Count() > 0) targets.Add(a.ElementAt(battleRandom.Next(a.Count())));
                                }
                                break;
                            case Rom.TargetType.ENEMY_ONE:
                            case Rom.TargetType.PARTY_ALL_ENEMY_ONE:
                            case Rom.TargetType.SELF_ENEMY_ONE:
                            {
                                var a = character.EnemyPartyRefMember.Where(enemy => enemy.HitPoint > 0);

                                if (a.Count() > 0) targets.Add(a.ElementAt(battleRandom.Next(a.Count())));
                            }
                            break;
                        }

                        targets.RemoveAll(x => !IsHitRange(character, skill.Range, x));
                    }
                    break;
            }

            return targets.ToArray();
        }

        public override void RegisterTestEffect(string effectNameKey, Resource.NSprite effect, Catalog catalog)
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
        internal void SetBackGroundImage(Common.Resource.Texture imageId)
        {
            backGroundStyle = BackGroundStyle.Image;

            backGroundImageId = imageId;
        }

        internal void SetBackGroundModel(Common.Resource.Model model)
        {
            /*backGroundModel = */
            new SharpKmyGfx.ModelInstance(model.m_mdl, null, System.Guid.Empty);
            backGroundStyle = BackGroundStyle.Model;
        }

        public override void Prepare()
        {
            if (!owner.IsBattle2D)
            {
                ((BattleViewer3D)battleViewer).prepare();
            }
        }
        public override void Prepare(Guid battleBg)
        {
            if (!owner.IsBattle2D)
            {
                ((BattleViewer3D)battleViewer).prepare(battleBg);
            }
        }


#if !WINDOWS
        // TODO:仮実装:Error回避の為
        // TODO: Temporary implementation: To avoid errors
        internal IEnumerator prepare_enum()
        {
            prepare();
            yield return null;
        }
        internal IEnumerator prepare_enum(Guid battleBg)
        {
            prepare(battleBg);
            yield return null;
        }
#endif




        public override bool IsWrongFromCurrentBg(Guid battleBg)
        {
            if (!owner.IsBattle2D)
            {
                return ((BattleViewer3D)battleViewer).mapDrawer.mapRom.guId != battleBg;
            }

            return false;
        }

        internal void UpdateCollisionDepotInUnity()
        {
            var battleViewer3D = battleViewer as BattleViewer3D;
            if (battleViewer3D == null)
            {
                return;
            }
            battleViewer3D.UpdateCollisionDepotInUnity();
        }

        internal void setActorsVisibility(bool flg)
        {
            var battleViewer3D = battleViewer as BattleViewer3D;
            if (battleViewer3D == null)
            {
                return;
            }
            battleViewer3D.setFriendsVisibility(flg);
        }

        //------------------------------------------------------------------------------
        /**
         *	スクリーンフェードカラーを取得
         */
        public override SharpKmyGfx.Color GetFadeScreenColor()
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
        public override BattleState GetBattleState()
        {
            return battleState;
        }


        public override BattleResultState GetBattleResult()
        {
            return BattleResult;
        }

        public override MapData GetMapDrawer()
        {
            return (battleViewer as BattleViewer3D)?.mapDrawer;
        }

        public override SharpKmyMath.Matrix4 GetCameraProjectionMatrix()
        {
            return (battleViewer as BattleViewer3D)?.p ?? SharpKmyMath.Matrix4.identity();
        }

        public override SharpKmyMath.Matrix4 GetCameraViewMatrix()
        {
            return (battleViewer as BattleViewer3D)?.v ?? SharpKmyMath.Matrix4.identity();
        }

        public override void ReloadUI(Rom.LayoutProperties.LayoutNode.UsageInGame usage)
        {
            (battleViewer as BattleViewer3D)?.ReloadUI(usage);
        }

        public override MapScene GetEventController()
        {
            return battleEvents;
        }

        // CUSTOM STUFF

        private bool showingShoices;
        private int currentIndexSelected;
        private bool selectingEvoDone;
        private string[] currentEvolutionList;
        private int prevDirection = -1;
        private bool PrepareEvolution()
        {
            if (catalog.getItemFromGuid(activeCharacter.Hero.rom.guId) is Rom.Cast activeRom)
            {
                var unhandledList = Tools.GetTagMultipleValues(activeRom.tags, "$evo");
                var handledList = new List<string>();
                var lastTarget = (int)BattleEventController.lastSkillTargetIndex;

                foreach (var unhandledtag in unhandledList)
                {
                    int requiredLevel = 0;
                    var evoAndLevel = unhandledtag.Split(',');

                    if (string.IsNullOrEmpty(evoAndLevel[0])) continue;
                    if (evoAndLevel.Length == 2)
                        int.TryParse(evoAndLevel[1], out requiredLevel);

                    if (party.Players[lastTarget].level < requiredLevel) continue;

                    handledList.Add(evoAndLevel[0]);
                }

                currentEvolutionList = handledList.ToArray();
                if (currentEvolutionList.Length >= 1) return true;
            }

            return false;
        }


        private void ChoiceEvolution()
        {
            if (!battleEvents.IsVisibleChoices() && !showingShoices)
            {
                battleEvents.ShowChoices(currentEvolutionList, 4);
                showingShoices = true;
            }

            GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DigiBattle", battleEvents.GetChoicesResult().ToString());
            var choice = battleEvents.GetChoicesResult();
            if (choice != -1)
            {
                // digiNameToEvo = evolutionList[choice];
                GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DigiBattle", "option selected");
                currentIndexSelected = choice;
                selectingEvoDone = true;

                float wait = 0f;
                prevDirection = -1;
                bool goEffect = false;
                bool rotate()
                {
                    var currentTarget = (int)BattleEventControllerBase.lastSkillTargetIndex;
                    var playerToEvolve = playerData[currentTarget];

                    if (prevDirection == -1) prevDirection = playerToEvolve.mapChr.getDirectionDigital();

                    if (playerToEvolve.MagicPoint <= 1)
                    {
                        return false;
                    }

                    if (wait <= 3.3f)
                    {
                        wait += GameMain.getElapsedTime();
                        var vector3 = playerToEvolve.mapChr.getRotation();
                        vector3.Y += (GameMain.getElapsedTime() * 420 * wait * 1.2f);
                        playerToEvolve.mapChr.setRotation(vector3);
                        return true;
                    }

                   // playerToEvolve.mapChr.setDirection(prevDirection, true, true);
                  //  playerToEvolve.mapChr.playMotion("attack", 0.2f, false, true);

                    ExecuteDigiEvolution(); // CUSTOM

                    //bool waitToAnim()
                    //{
                    //    if (!playerToEvolve.mapChr.isChangeMotionAvailable()) return true;
                    //    playerToEvolve.mapChr.playMotion("wait", 0.2f, false, false);

                    //    return false;
                    //}

                 //   GameMain.instance.pushTask(waitToAnim);

                    return false;
                }


                GameMain.instance.pushTask(rotate);

            }
        }
        private void ExecuteDigiEvolution() //CUSTOM
        {
            if (GameMain.instance.data.system.GetSwitch("executeDigievo"))
            {
                PerformDigiEvolution();
                showingShoices = false;
                selectingEvoDone = false;
                GameMain.instance.data.system.SetSwitch("executeDigievo", false, Guid.Empty, false);
            }
        }

        private void PerformDigiEvolution()
        {
            if (currentEvolutionList.Length <= currentIndexSelected) return;
            if (!(catalog.getItemFromName(currentEvolutionList[currentIndexSelected], typeof(Rom.Cast)) is Rom.Cast rom)) return;

            bool test()
            {
                var hero = Party.createHeroFromRom(catalog, rom);
                if (hero != null)
                {
                    var currentTarget = (int)BattleEventControllerBase.lastSkillTargetIndex;
                    var playerToEvolve = playerData[currentTarget];

                    if (playerToEvolve.MagicPoint <= 1)
                    {
                        return false;
                    }

                    var battleviewer3d = battleViewer as BattleViewer3D;
                    var positionPreEvo = battleviewer3d.friends[currentTarget].mapChr.pos;
                    var positionCache = new Vector3(positionPreEvo.X, positionPreEvo.Y, positionPreEvo.Z);
                    var graphic = catalog.getItemFromGuid(hero.rom.Graphic, false) as GfxResourceBase;
                    hero.SetLevel(party.Players[currentTarget].level, catalog, party);
                    playerData[currentTarget].MagicPoint /= 2;
                    var percentageToReduce = 100 - (playerData[currentTarget].MagicPointPercent * 100);
                    playerToEvolve.SetParameters(hero, owner.debugSettings.battleHpAndMpMax, owner.debugSettings.battleStatusMax, party);
                    playerToEvolve.player = hero;
                     playerToEvolve.mapChr.ChangeGraphic(graphic, battleEvents.mapDrawer);
                    playerToEvolve.MagicPoint -= (int)((percentageToReduce / 100) * playerToEvolve.MagicPoint);


                    playerToEvolve.mapChr.setDirection(prevDirection, true, true);
                    prevDirection = -1;
                    
                    battleviewer3d.prepareFriends(playerData);
                    battleviewer3d.friends[currentTarget].mapChr.pos = positionCache;
                    SetNextBattleStatus(playerToEvolve);

                    bool waitToAnim()
                    {

                        playerToEvolve.mapChr.playMotion("attack", 0.2f, false, true);

                        if (!playerToEvolve.mapChr.isChangeMotionAvailable()) return true;
                        playerToEvolve.mapChr.playMotion("wait", 0.2f, false, false);

                        return false;
                    }

                    GameMain.instance.pushTask(waitToAnim);
                }
                return false;
            }

            GameMain.instance.pushTask(test);
        }

        internal static class Tools
        {
            public static string StringFromTo(string str, string from = "(", string to = ")")
            {
                if (string.IsNullOrEmpty(str) || !str.Contains(from) || !str.Contains(to)) return null;
                int fromInt = str.IndexOf(from) + from.Length;
                int toInt = str.LastIndexOf(to);
                return str.Substring(fromInt, toInt - fromInt);
            }
            public static void PushLog(string msg)
            {
                GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "SpecialSkills", msg);
            }

            public static string GetTagValue(string tags, string targetTag)
            {
                var type = StringFromTo(tags.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).
                FirstOrDefault(x => x.ToLower().Contains(targetTag)));
                return type;
            }

            public static List<string> GetTagMultipleValues(string tags, string targetTag)
            {
                List<string> strings = new List<string>();
                var type = tags.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).Where(x => x.ToLower().Contains(targetTag));
                foreach (var types in type)
                {
                    var handledValue = Tools.StringFromTo(types);
                    strings.Add(handledValue);
                }
                return strings;
            }
        }
    }
}

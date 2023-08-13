#define WINDOWS
using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Yukar.Common;
using Yukar.Common.GameData;
using Yukar.Engine;
using static Yukar.Engine.BattleEnum;
using Rom = Yukar.Common.Rom;

namespace Yukar.Battle
{
    /// <summary>
    /// 3Dバトルの表示管理用クラス
    /// 3D battle display management class
    /// </summary>
    public class BattleViewer3D : BattleViewer
    {
        /// <summary>
        /// バトル背景
        /// battle background
        /// </summary>
        internal class BattleField : SharpKmyGfx.Drawable
        {
            BattleViewer3D owner;
            internal BattleField(BattleViewer3D owner)
            {
                this.owner = owner;
            }
            public override void draw(SharpKmyGfx.Render scn)
            {
                owner.draw(scn);
            }
        }
        private BattleField battleField;
        internal MapData mapDrawer;
        internal List<BattleActor> friends = new List<BattleActor>(Party.MAX_PARTY);
        internal List<BattleActor> enemies = new List<BattleActor>(Party.MAX_PARTY);
        internal List<MapCharacter> extras = new List<MapCharacter>();
        private SharpKmyGfx.ParticleInstance[] turnChr = new SharpKmyGfx.ParticleInstance[Party.MAX_PARTY];
        private SharpKmyGfx.ParticleInstance skillEffect;
        internal Catalog catalog;
        internal SharpKmyMath.Matrix4 p = SharpKmyMath.Matrix4.identity(), v = SharpKmyMath.Matrix4.identity();
        SharpKmyMath.Vector3 mCamLookAtTarget = new SharpKmyMath.Vector3();
        private BattleSequenceManager owner;
#if false
        internal BattleCameraController camera;
#endif
        private Rom.GameSettings.BattleCamera btc;
        private bool isVisibility = false;


        private Common.Rom.ThirdPersonCameraSettings INITIAL_CAMERA;
        private Common.Rom.ThirdPersonCameraSettings NORMAL_CAMERA;
        private Common.Rom.ThirdPersonCameraSettings ATTACK_START_CAMERA;
        private Common.Rom.ThirdPersonCameraSettings ATTACK_END_CAMERA;
        private Common.Rom.ThirdPersonCameraSettings MAGIC_START_CAMERA;
        private Common.Rom.ThirdPersonCameraSettings MAGIC_USER_CAMERA;
        private Common.Rom.ThirdPersonCameraSettings ITEM_START_CAMERA;
        private Common.Rom.ThirdPersonCameraSettings ITEM_USER_CAMERA;
        private Common.Rom.ThirdPersonCameraSettings CHANGE_START_CAMERA;
        private Common.Rom.ThirdPersonCameraSettings CHANGE_USER_CAMERA;
        private Common.Rom.ThirdPersonCameraSettings ENTER_START_CAMERA;
        private Common.Rom.ThirdPersonCameraSettings ENTER_USER_CAMERA;
        private Common.Rom.ThirdPersonCameraSettings LEAVE_START_CAMERA;
        private Common.Rom.ThirdPersonCameraSettings LEAVE_USER_CAMERA;
        private Common.Rom.ThirdPersonCameraSettings RESULT_START_CAMERA;
        private Common.Rom.ThirdPersonCameraSettings RESULT_END_CAMERA;

        private WindowDrawer statusWindow;
        private WindowDrawer commandWindow;

        internal SharpKmyMath.Matrix4 ProjectionMatrix
        {
            get
            {
                return p;
            }
            private set
            {
                p = value;
            }
        }
        internal SharpKmyMath.Matrix4 ViewMatrix
        {
            get
            {
                return v;
            }
            private set
            {
                v = value;
            }
        }

        /// <summary>
        /// バトルレイアウトを管理するクラス
        /// A class that manages battle layouts
        /// </summary>
        internal class BattleUI
        {



            public BattleUI(GameMain owner, Catalog catalog)
            {
                this.owner = owner;
                this.catalog = catalog;
                layouts = catalog.getLayoutProperties();

                // 暫定的に名前引きする
                // provisionally named
                main = LoadLayout(Rom.LayoutProperties.LayoutNode.UsageInGame.BattleStatus);
                main.Enabled = false;
                main.AutoInputHandling = false;
                command = LoadLayout(Rom.LayoutProperties.LayoutNode.UsageInGame.BattleCommand);
                message = LoadLayout(Rom.LayoutProperties.LayoutNode.UsageInGame.BattleMessage);
                itemList = LoadLayout(Rom.LayoutProperties.LayoutNode.UsageInGame.BattleItem);
                skillList = LoadLayout(Rom.LayoutProperties.LayoutNode.UsageInGame.BattleSkill);
                result = LoadLayout(Rom.LayoutProperties.LayoutNode.UsageInGame.BattleResult);
                //selectSkill = LoadLayout(Rom.LayoutProperties.LayoutNode.UsageInGame.BattleSelectSkill);
            }

            private LayoutDrawerController LoadLayout(Rom.LayoutProperties.LayoutNode.UsageInGame usageInGame)
            {
                var result = new LayoutDrawerController();
                result.LoadLayout(layouts, owner, catalog, usageInGame);
                return result;
            }

            internal LayoutDrawerController main;
            internal LayoutDrawerController command;
            internal LayoutDrawerController message;
            internal LayoutDrawerController itemList;
            internal LayoutDrawerController skillList;
            internal LayoutDrawerController result;
            //internal LayoutDrawerController selectSkill;
            private GameMain owner;
            private Catalog catalog;
            private Rom.LayoutProperties layouts;

            /// <summary>
            /// バトルレイアウトのLayoutDrawerを管理するクラス
            /// A class that manages the LayoutDrawer of the battle layout
            /// </summary>
            internal class LayoutDrawerController
            {

                public bool AutoInputHandling { set { drawer?.EnableAutoInputUpdate(value, 0); } }

                private AbstractRenderObject.GameContent gameContent = new AbstractRenderObject.GameContent();
                LayoutDrawer drawer;
                bool state;


                public Vector2 PositionCache { get; set; }

                internal LayoutDrawerController()
                {
                    PositionCache = Vector2.Zero;
                }

                internal void Update(bool visibility, bool forceUpdateGameContent = false)
                {
                    if (drawer == null)
                        return;

                    if (state != visibility)
                    {
                        if (visibility)
                        {
                            drawer.UpdateGameContent(gameContent);
                            drawer.Show();

                            // ポジションアンカーがついている要素を一旦非表示にする
                            // Temporarily hide elements with position anchors
                            var objs = drawer.GetRenderObjects();
                            foreach (var obj in objs)
                            {
                                var item = drawer.GetMenuItem(obj);
                                if (item == null)
                                    continue;

                                if (!string.IsNullOrEmpty(item.positionAnchorTag))
                                {
                                    obj.HideWithNotAnimation();
                                }
                            }
                        }
                        else
                        {
                            drawer.Hide();
                        }
                        state = visibility;
                    }
                    if (forceUpdateGameContent) drawer.UpdateGameContent(gameContent);
                    drawer.Update();
                }

                internal void ConfigureContentProperty(int maximumContent)
                {
                    drawer.ConfigureContentProperty(maximumContent);
                }

                internal void ConfigureContentProperty(int maximumContent, int index, bool updateRenderPanel)
                {
                    drawer.ConfigureContentProperty(maximumContent, index, updateRenderPanel);
                }

                internal void ConfigureContentProperty(int maximumContent, bool updateRenderPanel, Common.Rom.MenuSettings.MenuItem.LayoutType containMenuType)
                {
                    for (var i = 0; i < drawer.RenderObjectsCount; ++i)
                    {
                        if (drawer.ContainLayoutType(containMenuType, i))
                        {
                            drawer.ConfigureContentProperty(maximumContent, i, updateRenderPanel);
                            continue;
                        }
                    }
                }

                internal void ConfigureContentProperty(int maximumContent, bool updateRenderPanel, SpecialTextRenderer.SpecialTexts specialText)
                {
                    drawer.ConfigureContentProperty(maximumContent, updateRenderPanel, specialText);
                }

                internal bool IsVisible()
                {
                    if (drawer == null) return false;
                    return drawer.IsAnytingVisible();
                }


                internal void UpdateResult()
                {
                    if (drawer == null) return;
                    drawer.ChangeResultVisible(gameContent);
                    drawer.UpdateGameContent(gameContent);
                }

                internal void UpdateLearnSkill(List<Rom.NSkill> forgetSkills)
                {
                    if (drawer == null) return;
                    var index = drawer.GetSelectIndex();
                    if (forgetSkills.Count > index)
                    {
                        Skill = forgetSkills[index];
                    }
                    drawer.UpdateGameContent(gameContent);
                }

                internal void Draw()
                {
                    drawer?.Draw();
                }

                internal void LoadLayout(Rom.LayoutProperties layouts, GameMain owner, Catalog catalog, Rom.LayoutProperties.LayoutNode.UsageInGame usageInGame)
                {
                    var layout = owner.data.system.GetChangedLayout(layouts, usageInGame);
                    if (layout != null)
                        drawer = new LayoutDrawer(owner, catalog, layout);
                }

                internal bool Enabled
                {
                    set { drawer?.SetCursorVisible(value); }
                }

                public int Maximum { set { drawer?.ConfigureContentProperty(value); } }

                public bool Decided
                {
                    get
                    { return Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE, Input.GameState.MENU) || drawer.HaveDecidedByTouch(); }
                }

                public int Index
                {
                    get { return drawer?.GetSelectIndex() ?? 0; }
                    set { drawer?.SetSelectIndex(value); }
                }

                public Hero User { get { return gameContent.Hero; } set { gameContent.Hero = value; } }

                public Rom.NSkill Skill { get { return gameContent.Skill; } set { gameContent.Skill = value; } }

                public Party.ItemStack Item { get { return gameContent.ItemStack; } set { gameContent.ItemStack = value; } }

                public Dictionary<int, List<string>> LearnSkillNames { get { return gameContent.LearnSkillNames; } set { gameContent.LearnSkillNames = value; } }

                public List<Rom.NSkill> ForgetSkills { set { gameContent.ForgetSkills = value; } }

                public Rom.NSkill LearnSkill { set { gameContent.LearnSkill = value; } }

                public List<CharacterLevelUpData> CharacterLevelUpData { get { return gameContent.CharacterLevelUpData; } set { gameContent.CharacterLevelUpData = value; } }

                public ChoiceWindowDrawer.ChoiceItemData? ChoiceItemData { get { return gameContent.ChoiceItemData; } set { gameContent.ChoiceItemData = value; } }

                //public ResultViewer.CharacterLevelUpData LevelUpCaracterData
                //{
                //    get { return gameContent.levelUpCaracterData; }
                //    set { gameContent.levelUpCaracterData = value; }
                //}

                public void ResetGameContent()
                {
                    User = null;
                    Skill = null;
                    Item = null;
                    LearnSkillNames.Clear();
                    ForgetSkills = null;
                    LearnSkill = null;
                    CharacterLevelUpData.Clear();
                    ChoiceItemData = null;
                }

                internal int GetSelectIndex()
                {
                    return drawer.GetSelectIndex();
                }

                // bool値にしたがって項目をグレーアウトさせる
                // Gray out items according to bool value
                internal void SetSelectableState(IEnumerable<bool> states)
                {
                    var renderStatus = new AbstractRenderObject.RenderStatus(true);

                    int count = 0;
                    foreach (var state in states)
                    {
                        if (!state)
                            renderStatus.indices.Add(count);
                        count++;
                    }

                    // 項目なしの場合も来るので、その場合は1個めをfalseにしておく
                    // There may be cases where there are no items, so in that case, set the first to false
                    if (count == 0)
                        renderStatus.indices.Add(0);

                    drawer.ResetSubMenuContainerRenderStatus();
                    drawer.ChangeSubMenuContainerRenderStatus(renderStatus, false);
                }

                public void Invalidate(Func<string, int, Vector2?> positionProvider = null)
                {
                    drawer.UpdateGameContent(gameContent);

                    if (positionProvider == null)
                        return;

                    var objs = drawer.GetRenderObjects();
                    foreach (var obj in objs)
                    {
                        var item = drawer.GetMenuItem(obj);
                        if (item == null)
                            continue;

                        if (!string.IsNullOrEmpty(item.positionAnchorTag))
                        {
                            var index = item.containerIndex;
                            var renderContainer = obj as RenderContainer;
                            MenuSubContainer subMenuContainer = null;
                            var parent = obj.Parent;
                            if (parent is object)
                            {
                                subMenuContainer = parent as MenuSubContainer;
                                parent = parent.Parent;
                                if (subMenuContainer is null && parent is object)
                                {
                                    subMenuContainer = parent as MenuSubContainer;
                                }
                            }
                            if (subMenuContainer is object)
                            {
                                index = subMenuContainer.MenuIndex;
                            }
                            else if (renderContainer is object)
                            {
                                index = renderContainer.MenuIndex;
                            }
                            if (index < 0)
                            {
                                index = 0;
                            }
                            var res = positionProvider(item.positionAnchorTag, index);
                            if (res != null)
                            {
                                var pos = res.Value;
                                if (item.positionAnchorTag == "Enemy" || item.positionAnchorTag == "Player")
                                {
                                    pos += item.pos;
                                }
                                obj.Position = pos;
                            }
                        }
                    }
                }

                public void InvalidateResult(Func<string, int, Vector2> positionProvider = null)
                {
                    drawer.UpdateGameContent(gameContent);

                    if (positionProvider == null)
                        return;

                    var objs = drawer.GetRenderObjects();
                    foreach (var obj in objs)
                    {
                        var item = drawer.GetMenuItem(obj);
                        if (item == null)
                            continue;

                        if (!string.IsNullOrEmpty(item.positionAnchorTag))
                        {
                            obj.SkipUpdateVisibleFromParent = true;
                            var pos = positionProvider(item.positionAnchorTag, item.containerIndex);
                            if (item.positionAnchorTag == "Enemy" || item.positionAnchorTag == "Player")
                            {
                                pos += item.pos;
                            }
                            if (pos.X >= 5000)
                            {
                                if (obj.IsVisible && !obj.IsInOutAnimating)
                                    obj.Hide();
                            }
                            else if (!obj.IsVisible)
                            {
                                obj.Show();
                                obj.SetOffsetMyPosition(pos);
                            }
                            else
                            {
                                obj.SetOffsetMyPosition(pos);
                            }
                        }
                    }
                }

                public void CreateBattleContent()
                {
                    drawer.CreateBattleContent();
                }

                internal void finalize()
                {
                    drawer.Release();
                }

                internal AbstractRenderObject GetAnchorNode(AbstractRenderObject.PositionAnchorTags tag, int index)
                {
                    return drawer.GetRenderObjectWithPositionAnchorTag(tag, index, 0);
                }
            }

            internal void calcSelectedPlayer(List<BattlePlayerData> playerData)
            {
                int selectedPlayerIndex = -1;
                int count = 0;
                foreach (var player in playerData)
                {
                    if (player.statusWindowState == StatusWindowState.Active)
                    {
                        selectedPlayerIndex = count;
                    }
                    count++;
                }

                // isSelectがある場合はそれが優先
                // If there is isSelect it takes precedence
                count = 0;
                foreach (var player in playerData)
                {
                    if (player.IsSelect)
                    {
                        selectedPlayerIndex = count;
                    }
                    count++;
                }

                main.Enabled = selectedPlayerIndex >= 0;
                if (selectedPlayerIndex >= 0)
                    main.Index = selectedPlayerIndex;
            }

            internal void finalize()
            {
                main.finalize();
                command.finalize();
                message.finalize();
                itemList.finalize();
                skillList.finalize();
                result.finalize();
                //selectSkill.finalize();
            }

            internal void reload(Common.Rom.LayoutProperties.LayoutNode.UsageInGame usage)
            {
                switch (usage)
                {
                    case Rom.LayoutProperties.LayoutNode.UsageInGame.BattleMessage:
                        message.finalize();
                        message = LoadLayout(Rom.LayoutProperties.LayoutNode.UsageInGame.BattleMessage);
                        break;
                    case Rom.LayoutProperties.LayoutNode.UsageInGame.BattleStatus:
                        main.finalize();
                        main = LoadLayout(Rom.LayoutProperties.LayoutNode.UsageInGame.BattleStatus);
                        main.Enabled = false;
                        main.AutoInputHandling = false;
                        break;
                    case Rom.LayoutProperties.LayoutNode.UsageInGame.BattleSkill:
                        skillList.finalize();
                        skillList = LoadLayout(Rom.LayoutProperties.LayoutNode.UsageInGame.BattleSkill);
                        break;
                    case Rom.LayoutProperties.LayoutNode.UsageInGame.BattleItem:
                        itemList.finalize();
                        itemList = LoadLayout(Rom.LayoutProperties.LayoutNode.UsageInGame.BattleItem);
                        break;
                    case Rom.LayoutProperties.LayoutNode.UsageInGame.BattleChange:
                        break;
                    case Rom.LayoutProperties.LayoutNode.UsageInGame.BattleCommand:
                        command.finalize();
                        command = LoadLayout(Rom.LayoutProperties.LayoutNode.UsageInGame.BattleCommand);
                        break;
                    case Rom.LayoutProperties.LayoutNode.UsageInGame.BattleResult:
                        result.finalize();
                        result = LoadLayout(Rom.LayoutProperties.LayoutNode.UsageInGame.BattleResult);
                        break;
                    case Rom.LayoutProperties.LayoutNode.UsageInGame.BattleSelectSkill:
                        break;
                    default:
                        break;
                }
            }
        }
        internal BattleUI ui;
        private string currentDisplayMessageText;
        private bool updateGameContent = false;


        private List<int> drawLevelUpIndices = new List<int>();

        internal void initCameraParams()
        {
            btc = catalog.getGameSettings().battleCamera;
            INITIAL_CAMERA = createCameraSetting(btc.start);
            NORMAL_CAMERA = createCameraSetting(btc.normal);
            ATTACK_START_CAMERA = createCameraSetting(btc.attackStart);
            ATTACK_END_CAMERA = createCameraSetting(btc.attackEnd);
            MAGIC_START_CAMERA = createCameraSetting(btc.skillStart);
            MAGIC_USER_CAMERA = createCameraSetting(btc.skillEnd);
            ITEM_START_CAMERA = createCameraSetting(btc.itemStart);
            ITEM_USER_CAMERA = createCameraSetting(btc.itemEnd);
            CHANGE_START_CAMERA = createCameraSetting(btc.itemStart);
            CHANGE_USER_CAMERA = createCameraSetting(btc.itemEnd);
            ENTER_START_CAMERA = createCameraSetting(btc.itemStart);
            ENTER_USER_CAMERA = createCameraSetting(btc.itemEnd);
            LEAVE_START_CAMERA = createCameraSetting(btc.itemStart);
            LEAVE_USER_CAMERA = createCameraSetting(btc.itemEnd);
            RESULT_START_CAMERA = createCameraSetting(btc.resultStart);
            RESULT_END_CAMERA = createCameraSetting(btc.resultEnd);
        }

        private Rom.ThirdPersonCameraSettings createCameraSetting(Rom.ThirdPersonCameraSettings settings)
        {
            var result = new Rom.ThirdPersonCameraSettings();
            result.copyFrom(settings);
            result.offset.X += BattleSequenceManagerBase.battleFieldCenter.X + 0.5f;
            result.offset.Z += BattleSequenceManagerBase.battleFieldCenter.Z + 0.5f;
            return result;
        }

        private BattleState oldState;
        private SharpKmyMath.Vector3 campos;
        internal SharpKmyMath.Vector2 shakeValue;
        private BattleActor skillUser;
        private float lightCoeffBySkill = 1f;
        public CameraManager camManager;
        public MapData battleMapDrawer;

        internal void ReloadUI(Rom.LayoutProperties.LayoutNode.UsageInGame usage)
        {
            ui?.reload(usage);
        }

        internal BattleViewer3D(GameMain owner) : base(owner)
        {
            camManager = new CameraManager();
            DisplayIdUtil.changeScene(DisplayIdUtil.SceneType.BATTLE);
            battleMapDrawer = mapDrawer = new MapData(Graphics.getSpriteBatch());
            battleMapDrawer.isExpandViewRange = false;
            battleMapDrawer.drawMapObject = MapData.PICKUP_DRAW_MAPOBJECT.kNONE;
            BattleActor.map = mapDrawer;
            ui = new BattleUI(owner, owner.catalog);

            for (int i = 0; i < turnChr.Length; i++)
            {
                turnChr[i] = null;
                var res = owner.catalog.getItemFromGuid(owner.catalog.getGameSettings().battleTurnChr) as Common.Resource.Particle;
                if (res != null)
                {
                    turnChr[i] = res.getParticleInstance(Util.BATTLE3DDISPLAYID);
                    turnChr[i]?.start(SharpKmyMath.Matrix4.identity(), res.prewarm);
                }
            }

            {
                skillEffect = null;
                var res = owner.catalog.getItemFromGuid(owner.catalog.getGameSettings().battleSkillAura) as Common.Resource.Particle;
                if (res != null)
                {
                    skillEffect = res.getParticleInstance(Util.BATTLE3DDISPLAYID);
                    skillEffect?.start(SharpKmyMath.Matrix4.identity(), res.prewarm);
                }
            }

            battleField = new BattleField(this);

        }

        public override void LoadResourceData(Catalog catalog, Rom.GameSettings gameSettings)
        {
            base.LoadResourceData(catalog, gameSettings);

            // ステータスとコマンドの窓を読み込む
            // Load status and command windows
            var winRes = catalog.getItemFromGuid(gameSettings.systemGraphics.battleCommands3D) as Common.Resource.Texture;
            Graphics.LoadImage(winRes);
            commandWindow = new WindowDrawer(winRes);
            winRes = catalog.getItemFromGuid(gameSettings.systemGraphics.battleStatus3D) as Common.Resource.Texture;
            Graphics.LoadImage(winRes);
            statusWindow = new WindowDrawer(winRes);
        }

        internal override void ReleaseResourceData()
        {
            if (statusWindow == null)
                return;

            Graphics.UnloadImage(statusWindow.WindowResource);
            Graphics.UnloadImage(commandWindow.WindowResource);

            base.ReleaseResourceData();
        }

        internal void finalize()
        {
            ui.finalize();

            for (int i = 0; i < turnChr.Length; i++)
            {
                if (turnChr[i] != null)
                {
                    Graphics.UnloadParticle(turnChr[i]);
                }
            }

            {
                if (skillEffect != null)
                {
                    Graphics.UnloadParticle(skillEffect);
                }
            }

            battleMapDrawer.Destroy();
            BattleActor.map = null;
        }

        internal void setOwner(BattleSequenceManager owner)
        {
            this.owner = owner;
        }

        internal override void SetBackGround(Guid battleBg)
        {
            DisplayIdUtil.changeScene(DisplayIdUtil.SceneType.BATTLE);
            var bgMap = catalog.getItemFromGuid(battleBg) as Common.Rom.Map;
            if (bgMap == null)
                bgMap = catalog.getItemFromGuid(Common.Rom.Map.DEFAULT_3D_BATTLEBG) as Common.Rom.Map;
            if (bgMap == null)
                bgMap = new Rom.Map();// デフォルトバトル背景もなかった場合、空のマップにする / Empty map if there is no default battle background

            if (bgMap != null &&
                (mapDrawer.currentRom == null || mapDrawer.currentRom.guId != bgMap?.guId))
            {
                if (battleMapDrawer.currentRom != null)
                    battleMapDrawer.Reset();

                var map = bgMap;

                if (map.isReadOnly())
                {
                    BattleSequenceManagerBase.battleFieldCenter = BattleCharacterPosition.DEFAULT_BATTLE_FIELD_CENTER;
                }

                limitCenterOfField(map);
                initCameraParams();

                // マップを読み込む
                // load the map
                if (map != null)
                {
                    setMapRom(map);
                }
            }

            // extrasは解放されているケースがあるので常にチェックする
            // Always check extras because there are cases where extras are released
            initializeExtras(bgMap);
        }

        internal override void BattleStart(List<BattlePlayerData> playerData, List<BattleEnemyData> enemyMonsterData)
        {
            skillUser = null;

            // まず前回のカメラを外す
            // First remove the previous camera
            camManager.ntpCamera = null;

            if (game.data.system.BattleCameraEnabled[Common.GameData.SystemData.BATTLE_CAMERA_SITUATION_START])
            {
                camManager.setCameraFromPathName(catalog, true, Guid.Empty, Rom.Camera.NAME_BATTLE_START);
                camManager.playAnimation();

            }
            else
            {
                camManager.setCameraFromPathName(catalog, true, Guid.Empty, Rom.Camera.NAME_BATTLE_WAIT);
                camManager.playAnimation();
            }

            base.BattleStart(playerData, enemyMonsterData);

            BattleActor.map = mapDrawer;
            prepareFriends(playerData);
            prepareEnemies(enemyMonsterData);

            // 前の戦闘時の情報を削除
            // Delete information from previous battle
            ui.main.ResetGameContent();
            ui.command.ResetGameContent();
            ui.message.ResetGameContent();
            ui.itemList.ResetGameContent();
            ui.skillList.ResetGameContent();
            ui.result.ResetGameContent();

        }

        /// <summary>
        /// 描画処理
        /// drawing process
        /// </summary>
        /// <param name="playerData"></param>
        /// <param name="enemyMonsterData"></param>
        internal override void Draw(List<BattlePlayerData> playerData, List<BattleEnemyData> enemyMonsterData)
        {
            DisplayIdUtil.changeScene(DisplayIdUtil.SceneType.BATTLE);
            drawPlayerEffect(playerData);
            drawEnemyInfo(enemyMonsterData);

            updateGameContent = false;

            if (owner.commandSelectPlayer != null)
            {
                if (ui.main.User != owner.commandSelectPlayer.player)
                {
                    updateGameContent = true;
                }
                ui.main.User = owner.commandSelectPlayer.player;
                ui.command.User = owner.commandSelectPlayer.player;
                ui.message.User = owner.commandSelectPlayer.player;
                ui.itemList.User = owner.commandSelectPlayer.player;
                ui.result.User = owner.commandSelectPlayer.player;
            }
            else
            {
                if (ui.main.User is object)
                {
                    updateGameContent = true;
                }
                ui.main.User = null;
                ui.command.User = null;
                ui.message.User = null;
                ui.itemList.User = null;
                ui.result.User = null;
            }
            // アイテムリスト
            // item list
            var visibleItemList = displayWindow == WindowType.ItemListWindow;
            if (visibleItemList)
            {
                ui.itemList.Item = null;
                if (owner.commandSelectPlayer.haveItemList.Count > ui.itemList.Index)
                {
                    ui.itemList.Item = owner.commandSelectPlayer.haveItemList[ui.itemList.Index];
                }
                ui.itemList.SetSelectableState(itemSelectWindowDrawer.GetChoicesData().Select(x => x.enable));
                ui.itemList.Maximum = Math.Max(itemSelectWindowDrawer.ChoiceItemCount, 1);

                if (ui.main.Item != ui.itemList.Item)
                {
                    updateGameContent = true;
                }
                ui.main.Item = ui.itemList.Item;
                ui.command.Item = ui.itemList.Item;
                ui.message.Item = ui.itemList.Item;
                ui.skillList.Item = ui.itemList.Item;
                ui.result.Item = ui.itemList.Item;
            }
            else if (displayWindow != WindowType.CommandTargetPlayerListWindow)
            {
                if (ui.main.Item is object)
                {
                    updateGameContent = true;
                }
                ui.main.Item = null;
                ui.command.Item = null;
                ui.message.Item = null;
                ui.itemList.Item = null;
                ui.skillList.Item = null;
                ui.result.Item = null;
            }
            // スキルリスト
            // skill list
            var visibleSkillList = displayWindow == WindowType.SkillListWindow;
            if (visibleSkillList)
            {
                ui.skillList.User = owner.commandSelectPlayer.player;
                ui.skillList.Skill = null;
                if (owner.commandSelectPlayer.useableSkillList.Count > ui.skillList.Index)
                {
                    ui.skillList.Skill = owner.commandSelectPlayer.useableSkillList[ui.skillList.Index];
                }
                ui.skillList.SetSelectableState(skillSelectWindowDrawer.GetChoicesData().Select(x => x.enable));
                ui.skillList.Maximum = Math.Max(skillSelectWindowDrawer.ChoiceItemCount, 1);

                if (ui.main.Skill != ui.itemList.Skill)
                {
                    updateGameContent = true;
                }
                ui.main.Skill = ui.skillList.Skill;
                ui.command.Skill = ui.skillList.Skill;
                ui.message.Skill = ui.skillList.Skill;
                ui.itemList.Skill = ui.skillList.Skill;
                ui.result.Skill = ui.skillList.Skill;
            }
            else if (displayWindow != WindowType.CommandTargetPlayerListWindow)
            {
                if (ui.main.Skill is object)
                {
                    updateGameContent = true;
                }
                ui.main.Skill = null;
                ui.command.Skill = null;
                ui.message.Skill = null;
                ui.itemList.Skill = null;
                ui.skillList.User = null;
                ui.skillList.Skill = null;
                ui.result.Skill = null;
            }
            // コマンドリスト
            // command list
            var visibleCommand = displayWindow == WindowType.PlayerCommandWindow;
            if (visibleCommand)
            {
                var index = ui.command.GetSelectIndex();
                var choicesData = battleCommandChoiceWindowDrawer.GetChoicesData();
                if (choicesData.Count > index)
                {
                    var choice = choicesData[index];
                    if (!ui.main.ChoiceItemData.HasValue)
                    {
                        updateGameContent = true;
                    }
                    else if (ui.main.ChoiceItemData.Value.number != choice.number)
                    {
                        updateGameContent = true;
                    }

                    ui.main.ChoiceItemData = choice;
                    ui.command.ChoiceItemData = choice;
                    ui.message.ChoiceItemData = choice;
                    ui.itemList.ChoiceItemData = choice;
                    ui.result.ChoiceItemData = choice;
                }
                ui.command.SetSelectableState(battleCommandChoiceWindowDrawer.GetChoicesData().Select(x => x.enable));
                ui.command.Maximum = battleCommandChoiceWindowDrawer.ChoiceItemCount;
            }
            else if (displayWindow != WindowType.CommandTargetMonsterListWindow)
            {
                if (ui.main.ChoiceItemData is object)
                {
                    updateGameContent = true;
                }
                ui.main.ChoiceItemData = null;
                ui.command.ChoiceItemData = null;
                ui.message.ChoiceItemData = null;
                ui.itemList.ChoiceItemData = null;
                ui.skillList.ChoiceItemData = null;
                ui.result.ChoiceItemData = null;
            }


            ui.main.ConfigureContentProperty(playerData.Count, true, SpecialTextRenderer.SpecialTexts.Party);
            ui.main.Update(owner.battleEvents.battleUiVisibility, updateGameContent);

            // ステータスウィンドウ
            // status window
            ui.main.Invalidate((tag, index) =>
            {
                var res = new Vector2(50000, 50000);

                if (tag == "Enemy")
                {
                    if (index >= 0 && index < enemyMonsterData.Count && enemyMonsterData[index] != null)
                    {
                        var actor = enemies.FirstOrDefault(x => x.source == enemyMonsterData[index]);
                        if (actor != null)
                            return actor.getScreenPos(p, v, MapScene.EffectPosType.Ground);
                    }
                    return res;
                }
                else if (tag == "Player")
                {
                    if (index >= 0 && playerData.Count > index)
                    {
                        var pl = playerData[index];
                        foreach (var f in friends)
                        {
                            if (f != null && f.source == pl)
                            {
                                return f.getScreenPos(p, v, MapScene.EffectPosType.Ground);
                            }
                        }
                    }
                    return res;
                }

                if (System.Text.RegularExpressions.Regex.IsMatch(tag, "\\\\Position\\[[0-9]+\\]\\[[0-9]+\\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(tag, "\\\\Position\\[[0-9]+\\]\\[[0-9]+\\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    match = System.Text.RegularExpressions.Regex.Match(match.Value, "\\[[0-9]+\\]\\[[0-9]+\\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    var indeices = match.Value.Replace("[", string.Empty).Split(']');

                    var x = 0;
                    var y = 0;
                    if (indeices.Length > 2)
                    {
                        x = Convert.ToInt32(indeices[0]);
                        y = Convert.ToInt32(indeices[1]);
                    }

                    res = new Vector2(x, y);
                }

                return null;
            });

            ui.main.CreateBattleContent();
            ui.main.Draw();
            ui.calcSelectedPlayer(playerData);

            // ダメージ
            // damage
            drawDamageText(playerData, enemyMonsterData);

            // 3D
            DrawField(playerData, enemyMonsterData);

            LayoutDrawer.extraGuid = owner.commandSelectPlayer?.player.rom.guId ?? Guid.Empty;

            // コマンドリスト
            // command list
            ui.command.Update(visibleCommand, updateGameContent);
            ui.command.Draw();

            // メッセージウィンドウ
            // message window
            var visibleMessage = displayWindow == WindowType.MessageWindow ||
                displayWindow == WindowType.CommandTargetPlayerListWindow ||
                displayWindow == WindowType.CommandTargetMonsterListWindow;


            var forceUpdateContent = currentDisplayMessageText != displayMessageText;
            currentDisplayMessageText = displayMessageText;
            ui.message.Update(visibleMessage, forceUpdateContent);
            ui.message.Draw();

            // アイテムリスト
            // item list
            ui.itemList.Update(visibleItemList, true);
            ui.itemList.Draw();


            ui.skillList.Update(visibleSkillList, true);
            ui.skillList.Draw();

            LayoutDrawer.extraGuid = Guid.Empty;

            // リザルト(ここに来る場合は非表示なので false にするだけでOK)
            // Results (If you come here, it is hidden, so just set it to false is OK)
            ui.result.Update(false, updateGameContent);


            base.Draw_Tiny(playerData, enemyMonsterData);
        }

        /// <summary>
        /// リザルト専用描画メソッド
        /// Result-only drawing method
        /// </summary>
        internal void DrawResult(bool isVisible, List<BattlePlayerData> playerData, ResultViewer.ResultProperty resultProperty)
        {
            if (resultProperty != null && resultProperty.LevelUpIndices.Count > 0)
            {
                foreach (var index in resultProperty.LevelUpIndices)
                {
                    drawLevelUpIndices.Add(index);
                    //if (ui.result.LevelUpCaracterData.player == null && resultProperty.CharacterLevelUpData.Count > index)
                    //{
                    //    ui.result.LevelUpCaracterData = resultProperty.CharacterLevelUpData[index];
                    //}
                }
            }

            if (resultProperty != null && resultProperty.LearnSkills.Count > 0)
            {
                var learnSkillNames = new Dictionary<int, List<string>>();
                foreach (var learnSkill in resultProperty.LearnSkills)
                {
                    var skillNames = new List<string>();
                    foreach (var skill in learnSkill.Item2)
                    {
                        skillNames.Add(skill.Name);
                    }
                    learnSkillNames.Add(learnSkill.Item1, skillNames);
                }
                ui.result.LearnSkillNames = learnSkillNames;
            }

            if (resultProperty != null && resultProperty.EndLevelUp)
            {
                drawLevelUpIndices.Clear();
                ui.result.LearnSkillNames = new Dictionary<int, List<string>>();
                //ui.result.LevelUpCaracterData = new ResultViewer.CharacterLevelUpData();
            }

            var itemCount = 0;
            if (owner.resultViewer.dropItemCountTable != null) itemCount = owner.resultViewer.dropItemCountTable.Count;
            ui.result.ConfigureContentProperty(itemCount, true, SpecialTextRenderer.SpecialTexts.Item);
            ui.result.ConfigureContentProperty(playerData.Count, true, SpecialTextRenderer.SpecialTexts.Party);
            if (resultProperty != null)
            {
                ui.result.CharacterLevelUpData = resultProperty.CharacterLevelUpData;
            }
            //if(resultProperty != null && drawLevelUpIndices.Count > 0 && resultProperty.CharacterLevelUpData.Count > drawLevelUpIndices[0])
            //{
            //    ui.result.LevelUpCaracterData = resultProperty.CharacterLevelUpData[drawLevelUpIndices[0]];
            //}
            ui.result.Update(isVisible, true);
            ui.result.UpdateResult();
            ui.result.CreateBattleContent();
            ui.result.InvalidateResult((tag, index) =>
            {
                var res = new Vector2(5000, 5000);

                if (index > playerData.Count - 1 || !drawLevelUpIndices.Contains(index))
                {
                    return res;
                }

                if (tag == "Player")
                {

                    var pl = playerData[index];
                    foreach (var f in friends)
                    {
                        if (f != null && f.source == pl)
                        {
                            res = f.getScreenPos(p, v, MapScene.EffectPosType.Ground);
                            break;
                        }
                    }
                }

                if (System.Text.RegularExpressions.Regex.IsMatch(tag, "Position\\[[0-9]+\\]\\[[0-9]+\\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(tag, "Position\\[[0-9]+\\]\\[[0-9]+\\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    match = System.Text.RegularExpressions.Regex.Match(match.Value, "\\[[0-9]+\\]\\[[0-9]+\\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    var indeices = match.Value.Replace("[", string.Empty).Split(']');

                    var x = 0;
                    var y = 0;
                    if (indeices.Length > 2)
                    {
                        x = Convert.ToInt32(indeices[0]);
                        y = Convert.ToInt32(indeices[1]);
                    }

                    res = new Vector2(x, y);
                }

                return res;
            });
            ui.result.Draw();
        }

        internal void DrawLearnSkill(List<Rom.NSkill> forgetSkills, Rom.NSkill leranSkill, Hero learningSkillHero)
        {
            //ui.selectSkill.ForgetSkills = forgetSkills;
            //ui.selectSkill.LearnSkill = leranSkill;
            //ui.selectSkill.User = learningSkillHero;
            //ui.selectSkill.Update(true);
            //ui.selectSkill.UpdateLearnSkill(forgetSkills);
            //ui.selectSkill.Draw();
        }

        internal bool SelectingSkill()
        {
            //if (ui.selectSkill.Decided) return false;
            if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.CANCEL, Input.GameState.MENU)) return false;
            return true;
        }

        internal bool DecideSkill()
        {
            //return ui.selectSkill.Decided;
            return true;
        }

        internal int DecideSkillIndex()
        {
            //return ui.selectSkill.Index;
            return 0;
        }

        /// <summary>
        /// フィールドを描画
        /// draw field
        /// </summary>
        /// <param name="playerData"></param>
        /// <param name="enemyMonsterData"></param>
        internal void DrawField(List<BattlePlayerData> playerData, List<BattleEnemyData> enemyMonsterData)
        {
            // エフェクト
            // effect
            drawEffect();


            SharpKmyGfx.Render.getDefaultRender().addDrawable( battleField );

        }

        /// <summary>
        /// エフェクトを描画
        /// draw effect
        /// </summary>
        private void drawEffect()
        {
            if (skillUser == null)
                skillUser = friends[0];

            effectDrawTargetMonsterList.Sort((a, b) =>
            {
                if (a.EffectPriority == b.EffectPriority)
                    return 0;
                else if (a.EffectPriority > b.EffectPriority)
                    return -1;
                else
                    return 1;
            });
            effectDrawTargetPlayerList.Sort((a, b) =>
            {
                if (a.EffectPriority == b.EffectPriority)
                    return 0;
                else if (a.EffectPriority > b.EffectPriority)
                    return -1;
                else
                    return 1;
            });

            camManager.convQuaternionToRotation(camManager.camQuat, out Vector3 rot);

            if (!monsterEffectDrawer.isEndPlaying)
            {
                if (monsterEffectDrawer.origPos == Common.Resource.NSprite.OrigPos.ORIG_SCREEN)
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
                        var actor = searchFromActors(target);
                        if (monsterEffectDrawer.drawFor3D(skillUser?.mapChr, actor?.mapChr, effectDrawTargetMonsterList.Count, rot.Y))
                            actor?.queueActorState(BattleActor.ActorStateType.DAMAGE_START);
                    }
                }
            }

            if (!playerEffectDrawer.isEndPlaying)
            {
                if (playerEffectDrawer.origPos == Common.Resource.NSprite.OrigPos.ORIG_SCREEN)
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
                        // レイアウト側にダメージポジションの指定があればそこにポップする
                        // If there is a damage position specified on the layout side, pop it there
                        var targetLayoutNode = ui.main.GetAnchorNode(AbstractRenderObject.PositionAnchorTags.DamagePosition, owner.PlayerViewDataList.IndexOf(target as BattlePlayerData));
                        if (targetLayoutNode != null)
                        {
                            var pos = targetLayoutNode.GetOriginPosition();
                            playerEffectDrawer.draw((int)pos.X, (int)pos.Y, false);
                            // おおよそデフォルトの身長分相当を下げる
                            // Approximately lower the height equivalent to the default
                            var near = Util.UnProject2World(p, v, new SharpKmyMath.Vector3(pos.X, pos.Y + 80, -1), Graphics.ScreenWidth, Graphics.ScreenHeight);
                            var far = Util.UnProject2World(p, v, new SharpKmyMath.Vector3(pos.X, pos.Y + 80, 1), Graphics.ScreenWidth, Graphics.ScreenHeight);
                            var direction = SharpKmyMath.Vector3.normalize(far - near);
                            var unprojected = near + direction * 10;
                            var actor = searchFromActors(target);
                            if (actor != null)
                            {
                                var pos3dBack = actor.mapChr.pos;
                                actor.mapChr.pos = Util.ToXnaVector(unprojected);
                                if (playerEffectDrawer.drawFor3D(skillUser?.mapChr, actor.mapChr, effectDrawTargetPlayerList.Count, rot.Y))
                                    actor?.queueActorState(BattleActor.ActorStateType.DAMAGE_START);
                                actor.mapChr.pos = pos3dBack;
                            }
                        }
                        else
                        {
                            playerEffectDrawer.draw((int)target.EffectPosition.X, (int)target.EffectPosition.Y, false);
                            var actor = searchFromActors(target);
                            if (playerEffectDrawer.drawFor3D(skillUser?.mapChr, actor?.mapChr, effectDrawTargetPlayerList.Count, rot.Y))
                                actor?.queueActorState(BattleActor.ActorStateType.DAMAGE_START);
                        }
                    }
                }
            }

            if (!defeatEffectDrawer.isEndPlaying)
            {
                if (defeatEffectDrawer.origPos == Common.Resource.NSprite.OrigPos.ORIG_SCREEN)
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
                        var actor = searchFromActors(target);
                        if (defeatEffectDrawer.drawFor3D(skillUser?.mapChr, actor?.mapChr, defeatEffectDrawTargetList.Count, rot.Y))
                            actor?.queueActorState(BattleActor.ActorStateType.KO);
                    }
                }
            }
        }

        /// <summary>
        /// エネミー情報を描画
        /// Draw enemy information
        /// </summary>
        /// <param name="enemyMonsterData"></param>
        private void drawEnemyInfo(List<BattleEnemyData> enemyMonsterData)
        {
            BattleEnemyData currentSelectMonster = null;

            // 敵の画像に関連する情報を表示
            // Display information related to enemy images
            foreach (var monster in enemyMonsterData)
            {
                var color = Color.White;

                if (monster.imageAlpha < 1 && !monster.monster.visibleWhenKO)
                {
                    color = new Color(monster.imageAlpha, monster.imageAlpha, monster.imageAlpha, monster.imageAlpha);
                }

                float scale = 1.0f;
                EffectDrawerBase effect = null;

                var actor = searchFromActors(monster);
                if (actor == null)
                    continue;

                if (openingMonsterScaleTweener.IsPlayTween && openingColorTweener.IsPlayTween)
                {
                    scale *= openingMonsterScaleTweener.CurrentValue;
                }
                else if (effectDrawTargetMonsterList.Contains(monster))
                {
                    effect = monsterEffectDrawer;
                }
                else if (effectDrawTargetPlayerList.Contains(monster))
                {
                    effect = playerEffectDrawer;
                }
                else if (defeatEffectDrawTargetList.Contains(monster))
                {
                    effect = defeatEffectDrawer;
                }
                else if (displayWindow == WindowType.CommandTargetMonsterListWindow)
                {
                    if (monster.IsSelect)
                    {
                        // ターゲットとして選択されている時は点滅させる
                        // Blink when selected as a target
                        color = blinker.getColor();
                        currentSelectMonster = monster;
                    }
                    //else if (monster.imageAlpha > 0)
                    //{
                    //    color.R = color.G = color.B = 64;
                    //    color.A = 192;
                    //}
                }
                else if (actor.overRidedColor != null)
                {
                    color = actor.overRidedColor.Value;
                    actor.overRidedColor = null;
                }

                if (effect != null)
                {
                    if (effect is EffectDrawer)
                        color = effect.getNowTargetColor();
                    monster.EffectPosition = actor.getScreenPos(p, v, MapScene.GetEffectPosType(effect.origPos));
                    monster.EffectPriority = (campos - actor.mapChr.getKmyPosition()).length();
                }

                if (monster.commandEffectColor.IsPlayTween)
                {
                    color = monster.commandEffectColor.CurrentValue;
                }

                actor.setColor(color);

                if (!monster.monster.visibleWhenKO)
                    actor.setOpacityMultiplier(Math.Min(1, monster.imageAlpha));

                monster.mapChr?.drawConditionEffect(p, v);
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
            //if (currentSelectMonster != null)
            //{
            //    if (currentSelectMonster != prevSelectedMonster)
            //    {
            //        SetMonsterStatusEffect(currentSelectMonster);
            //    }

            //    prevSelectedMonster = currentSelectMonster;

            //    var effects = new List<EffectDrawer>();

            //    effects.Add(currentSelectMonster.positiveEffectDrawers.ElementAtOrDefault(currentSelectMonster.positiveEffectIndex));
            //    effects.Add(currentSelectMonster.negativeEffectDrawers.ElementAtOrDefault(currentSelectMonster.negativeEffectIndex));
            //    //effects.Add(currentSelectMonster.statusEffectDrawers.ElementAtOrDefault(currentSelectMonster.statusEffectIndex));

            //    effects.RemoveAll(effect => effect == null);

            //    Vector2 effectGraphicsSize = new Vector2(48, 48);
            //    Vector2 effectDrawPosition = searchFromActors(currentSelectMonster).getScreenPos(p, v);
            //    effectDrawPosition.X -= (effects.Count - 1) * 0.5f * effectGraphicsSize.X;
            //    effectDrawPosition.Y -= effectGraphicsSize.Y;

            //    // エフェクトがメッセージウィンドウと重ならないように位置を調整
            // // Adjust the position so that the effect does not overlap the message window
            //    if (effectDrawPosition.Y <= Graphics.ScreenHeight * 0.2f)
            //    {
            //        effectDrawPosition.Y = Graphics.ScreenHeight * 0.2f;
            //    }

            //    foreach (var effectDrawer in effects)
            //    {
            //        effectDrawer.draw((int)effectDrawPosition.X, (int)effectDrawPosition.Y);

            //        effectDrawPosition.X += effectGraphicsSize.X;
            //    }
            //}
        }

        int STATUS_X { get { return 256; } }
        int STATUS_WIDTH { get { return 960 - STATUS_X * 2; } }
        int STATUS_Y { get { return Graphics.ScreenHeight - 24 * 4 - 4; } }


        private void drawPlayerEffect(List<BattlePlayerData> playerData)
        {
            foreach (var player in playerData)
            {
                if (player.IsBattle)
                {
                    drawPlayerEffect(player);

                    player.mapChr?.drawConditionEffect(p, v);
                }
            }
        }

        private void drawPlayerEffect(BattlePlayerData player)
        {
            var color = Color.White;
            EffectDrawerBase effect = null;
            var friend = searchFromActors(player);
            if (friend == null)
                return;

            if (friend.isBillboardDead() && friend.getActorState() == BattleActor.ActorStateType.KO)
                color = new Color(64, 64, 64, 127);
            if (displayWindow == WindowType.CommandTargetPlayerListWindow && player.IsSelect)
            {
                // ターゲットとして選択されている時は点滅させる
                // Blink when selected as a target
                color = blinker.getColor();

                if (player.IsSelectDisabled)
                {
                    color.R /= 2;
                    color.G /= 2;
                    color.B /= 2;
                }
            }
            else if (effectDrawTargetMonsterList.Contains(player))
            {
                effect = monsterEffectDrawer;
            }
            else if (effectDrawTargetPlayerList.Contains(player))
            {
                effect = playerEffectDrawer;
            }
            else if (defeatEffectDrawTargetList.Contains(player))
            {
                effect = defeatEffectDrawer;
            }
            else if (friend.overRidedColor != null)
            {
                color = friend.overRidedColor.Value;
                friend.overRidedColor = null;
            }
            if (effect != null)
            {
                // エフェクトを受けてる時はエフェクトターゲット色を適用
                // Apply effect target color when receiving effect
                color = effect.getNowTargetColor();

                // エフェクトの位置を確定
                // Confirm the position of the effect
                player.EffectPosition = friend.getScreenPos(p, v, MapScene.GetEffectPosType(effect.origPos));
            }

            friend.setColor(color);

            // レイアウト側にダメージポジションの指定があればその色を変える
            // If there is a damage position specified on the layout side, change the color
            var index = owner.PlayerViewDataList.IndexOf(player);
            var targetLayoutNode = ui.main.GetAnchorNode(AbstractRenderObject.PositionAnchorTags.DamagePosition, index);
            while (targetLayoutNode?.Parent != null && targetLayoutNode.Parent.MenuIndex == index)
                targetLayoutNode = targetLayoutNode.Parent;

            if (targetLayoutNode != null)
            {
                Action<AbstractRenderObject> setColorImpl = null;
                setColorImpl = (x) =>
                {
                    if (x is TextPanel)
                        ((TextPanel)x).SetColor(color);

                    foreach (var child in x.Children)
                    {
                        setColorImpl(child);
                    }
                };
                setColorImpl(targetLayoutNode);
            }
        }

        /// <summary>
        /// プレイヤーパネルを描画
        /// draw player panel
        /// </summary>
        /// <param name="player"></param>
        /// <param name="index"></param>
        private void drawPlayerPanel(BattlePlayerData player, int index)
        {
            // その人のターンだった時は背景を点滅させる
            // Blink the background when it is the person's turn
            var blinkColor = blinker.getColor();
            if (player.statusWindowState == StatusWindowState.Active)
            {
                Graphics.DrawFillRect(STATUS_X, STATUS_Y + 24 * index, STATUS_WIDTH / 2, 24,
                    (byte)(blinkColor.A >> 2), (byte)(blinkColor.A >> 2), (byte)(blinkColor.A >> 1), (byte)(blinkColor.A >> 3));
            }

            // HP、MPのゲージを描画
            // Draw HP and MP gauges
            var gaugeSize = new Vector2(STATUS_WIDTH / 4 - 8, 8);
            var hpPos = new Vector2(STATUS_X + STATUS_WIDTH / 2, STATUS_Y + 24 * index);
            var mpPos = new Vector2(STATUS_X + STATUS_WIDTH * 3 / 4, STATUS_Y + 24 * index);
            hpPos.Y += 24 - 8;
            mpPos.Y += 24 - 8;
            battleStatusWindowDrawer.gaugeDrawer.Draw(hpPos, gaugeSize, player.battleStatusData.MaxHitPoint > 0 ?
                (float)player.battleStatusData.HitPoint / player.battleStatusData.MaxHitPoint : 0,
                GaugeDrawer.GaugeOrientetion.HorizonalRightToLeft, new Color(150, 150, 240));
            battleStatusWindowDrawer.gaugeDrawer.Draw(mpPos, gaugeSize, player.battleStatusData.MaxMagicPoint > 0 ?
                (float)player.battleStatusData.MagicPoint / player.battleStatusData.MaxMagicPoint : 0,
                GaugeDrawer.GaugeOrientetion.HorizonalRightToLeft, new Color(16, 180, 96));

            // 名前、HP、MP を描画
            // Draw name, HP, MP
            hpPos = new Vector2(STATUS_X + STATUS_WIDTH / 2, STATUS_Y + 24 * index - 2);
            mpPos = new Vector2(STATUS_X + STATUS_WIDTH * 3 / 4, STATUS_Y + 24 * index - 2);
            textDrawer.DrawStringSoloColor(player.Name, new Vector2(STATUS_X, STATUS_Y + 24 * index), Color.White, 0.75f);
            textDrawer.DrawStringSoloColor("" + player.battleStatusData.HitPoint, hpPos, Color.White, 0.75f);
            textDrawer.DrawStringSoloColor("" + player.battleStatusData.MagicPoint, mpPos, Color.White, 0.75f);

            // 選択されている時は矢印を出す
            // Show arrow when selected
            if (player.IsSelect)
            {
                textDrawer.DrawStringSoloColor("▶", new Vector2(STATUS_X - 20, STATUS_Y + 24 * index), Color.White, 0.75f);
            }
        }

        internal BattleActor AddEnemyMember(BattleEnemyData data, int index)
        {
            // 1スタートになっている
            // 1 has started
            index--;
            if (index < 0)
                index = 0;

            while (enemies.Count <= index)
                enemies.Add(null);

            // emptyじゃなかったら先に解放する
            // If it's not empty, free it first
            if (enemies[index] != null)
            {
                enemies[index].Release();
                enemies[index] = null;
            }

            // totalを数える
            // count total
            int total = 0;
            foreach (var enm in enemies)
            {
                if (enm != null)
                    total++;
            }

            var actor = BattleActor.GenerateEnemy(catalog, data.image, index, total + 1);
            actor.source = data;
            actor.mapChr.setPosition(data.pos);
            enemies[index] = actor;
            data.mapChr = actor.mapChr;
            data.actionHandler = executeAction;

            return actor;
        }

        internal BattleActor AddPartyMember(BattlePlayerData data, Vector3[] playerLayouts)
        {
            int empty = 0;
            while (friends.Count > empty && friends[empty] != null)
                empty++;
            if (friends.Count <= empty)
                return null;
            var actor = BattleActor.GenerateFriend(catalog, data.player, empty, empty + 1);
            actor.source = data;
            friends[empty] = actor;
            data.mapChr = actor.mapChr;
            data.actionHandler = executeAction;

            if (playerLayouts != null)
            {
                data.SetPosition(BattleSequenceManagerBase.battleFieldCenter + playerLayouts[empty]);
                actor.mapChr.setPosition(data.pos);
            }

            return actor;
        }

        /// <summary>
        /// ダメージテキストを描画
        /// draw damage text
        /// </summary>
        /// <param name="playerData"></param>
        /// <param name="enemyMonsterData"></param>
        private void drawDamageText(List<BattlePlayerData> playerData, List<BattleEnemyData> enemyMonsterData)
        {
            if (damageTextList != null && damageTextList.Count() > 0)
            {
                var removeList = new List<BattleDamageTextInfo>();

                foreach (var info in damageTextList.Where(info => info != null && info.textInfo != null)
                    .OrderByDescending(info => info.targetCharacter.EffectPriority))
                {
                    Color color = Color.White;
                    if (info.type < BattleDamageTextInfo.TextType.Miss)
                        color = catalog.getGameSettings().damageNumColors[(int)info.type];

                    var actor = searchFromActors(info.targetCharacter);
                    if (actor == null)
                    {
                        removeList.Add(info);
                        continue;
                    }

                    var basePosition = actor.getScreenPos(p, v, MapScene.EffectPosType.Body);

                    // レイアウト側にダメージポジションの指定があればそこにポップする
                    // If there is a damage position specified on the layout side, pop it there
                    if (info.targetCharacter is BattlePlayerData)
                    {
                        var targetLayoutNode = ui.main.GetAnchorNode(AbstractRenderObject.PositionAnchorTags.DamagePosition, owner.PlayerViewDataList.IndexOf(info.targetCharacter as BattlePlayerData));
                        if (targetLayoutNode != null)
                        {
                            var tgt = targetLayoutNode.Parent;
                            while (tgt != null && !(tgt is MenuSubContainer))
                            {
                                tgt = tgt.Parent;
                            }
                            if (tgt is MenuSubContainer && (info.type == BattleDamageTextInfo.TextType.HitPointDamage || info.type == BattleDamageTextInfo.TextType.CriticalDamage))
                            {
                                // コンテナを揺らす処理
                                // Processing to shake the container
                                float amount = info.textInfo[0].timer / 5 + 0.5f;
                                amount = Math.Max(amount, 0.0f);
                                amount = Math.Min(amount, 4.5f);
                                var duration = amount % 2;
                                if (duration > 1)
                                    duration = 2 - duration;
                                duration -= 0.5f;

                                basePosition = targetLayoutNode.GetOriginPosition();

                                var pos = ((MenuSubContainer)tgt).GetDefaultDrawPosition();
                                pos.X += duration * 16;
                                tgt.Position = pos;
                            }
                            else
                            {
                                basePosition = targetLayoutNode.GetOriginPosition();
                            }
                        }
                    }

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

                        // 複数表示されるときは少しずらして表示する
                        // When multiple images are displayed, they are displayed in a slightly staggered manner.
                        var index = damageTextList
                            .Where(x => x != null && x.targetCharacter == info.targetCharacter)
                            .Select((p, i) => new { Value = p, Index = i })
                            .FirstOrDefault(x => x.Value == info)?.Index ?? 0;
                        if (index > 0)
                            position.Y += Graphics.GetDivHeight(damageNumberImageId) * index;

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
        }

        internal BattleActor searchFromActors(BattleCharacterBase battleCharacterBase)
        {
            foreach (var mapChr in friends)
            {
                if (mapChr == null)
                    continue;

                if (mapChr.source == battleCharacterBase)
                    return mapChr;
            }
            foreach (var mapChr in enemies)
            {
                if (mapChr == null)
                    continue;

                if (mapChr.source == battleCharacterBase)
                    return mapChr;
            }
            return null;
        }

        internal BattleActor searchFromActors(MapCharacter chr)
        {
            foreach (var mapChr in friends)
            {
                if (mapChr == null)
                    continue;

                if (mapChr.mapChr == chr)
                    return mapChr;
            }
            foreach (var mapChr in enemies)
            {
                if (mapChr == null)
                    continue;

                if (mapChr.mapChr == chr)
                    return mapChr;
            }
            return null;
        }

        void UpdateBattleActors(List<BattleActor> inBattleActors, float inAngleY, bool inIsFriend)
        {
            foreach (var actor in inBattleActors)
            {
                if (actor == null)
                {
                    continue;
                }

                var prevState = actor.getActorState();
                var prevOption = actor.getActorStateOption();

                actor.Update(mapDrawer, inAngleY, false);

                var state = actor.getActorState();
                var option = actor.getActorStateOption();
                var source = actor.source;

                // 味方の死亡エフェクト
                // Ally death effect
                if ((state != prevState || option != prevOption) &&
                    (state == BattleActor.ActorStateType.KO || state == BattleActor.ActorStateType.FATAL_DAMAGE) &&
                    source.IsDeadCondition() && inIsFriend)
                {
                    if (source.DeathEffect != Guid.Empty)
                        SetDefeatEffect(source.DeathEffect, source);
                }

                // 敵の死亡エフェクト
                // Enemy death effect
                if (fadeoutEnemyList.Count > 0 && source.imageAlpha > 0 && !defeatEffectDrawTargetList.Contains(source) &&
                    (state == BattleActor.ActorStateType.KO || state == BattleActor.ActorStateType.FATAL_DAMAGE) &&
                    source.IsDeadCondition() && !inIsFriend)
                {
                    if (source.DeathEffect != Guid.Empty)
                        SetDefeatEffect(source.DeathEffect, source);
                }
            }
        }

        internal override void Update(List<BattlePlayerData> playerData, List<BattleEnemyData> enemyMonsterData)
        {
            base.Update(playerData, enemyMonsterData);

            if (mapDrawer.currentRom == null)
                return;

            Quaternion qt = camManager.camQuat;
            Vector3 rot = new Vector3();
            camManager.convQuaternionToRotation(qt, out rot);
            float angleY = rot.Y;
            mapDrawer.Update(GameMain.getElapsedTime());

            foreach (var entry in turnChr)
            {
                entry?.setVisibility(false);
            }

            UpdateBattleActors(friends, angleY, true);

            foreach (var mapChr in extras)
            {
                mapChr.Update(mapDrawer, /*camera.Now.angle.Y*/angleY, false);
            }

            UpdateBattleActors(enemies, angleY, false);

            // バトルステートに応じてカメラを動かす
            // Move camera according to battle state
            if (game.data.system.BattleCameraEnabled[Common.GameData.SystemData.BATTLE_CAMERA_SITUATION_RESULT] &&
                owner.battleState == BattleState.Result &&
                oldState != BattleState.Result)
            {
                camManager.setCameraFromPathName(catalog, true, Guid.Empty, Rom.Camera.NAME_BATTLE_RESULT);
                camManager.playAnimation();
            }

            // 状態に応じて各キャラに演技させる
            // Let each character act according to the state
            for (int i = 0; i < playerData.Count; i++)
            {
                // ステータスを書く位置を更新する
                // Update position to write status
                var friend = searchFromActors(playerData[i]);

                if (friend == null)
                    continue;

                playerData[i].statusWindowDrawPosition = friend.getScreenPos(p, v, MapScene.EffectPosType.Head);

                Vector3 neutralPos;

                neutralPos = friend.mapChr.pos;
                //var neutralPos = BattleCharacterPosition.getPosition(CenterOfField, BattleCharacterPosition.PosType.FRIEND, i, playerData.Count);
                //if (playerData[i].isMovableToForward() &&
                //    (friend.getActorState() < BattleActor.ActorStateType.START_COMMAND_SELECT ||
                //    friend.getActorState() > BattleActor.ActorStateType.BACK_TO_WAIT))
                //    neutralPos.Z -= friends[i].frontDir;

                //neutralPos.X = playerData[i].pos.X;
                //neutralPos.Z = playerData[i].pos.Z;
                //neutralPos.Y = drawer?.getAdjustedHeight(neutralPos.X, neutralPos.Z) ?? 0;
                //friend.mapChr.setPosition(neutralPos);
                //friend.mapChr.setDirectionFromRadian(playerData[i].directionRad);
                if (i < turnChr.Length)
                    turnChr[i]?.setPosture(SharpKmyMath.Matrix4.translate(neutralPos.X, neutralPos.Y, neutralPos.Z));

                // 逃げる
                // run away
                if (owner.battleState == BattleState.PlayerEscapeSuccess)
                {
                    friend.queueActorState(BattleActor.ActorStateType.ESCAPE);
                    continue;
                }

                // 勝利
                // victory
                if (owner.battleState == BattleState.Result)
                {
                    friend.queueActorState(BattleActor.ActorStateType.WIN);
                    continue;
                }

                updateConditionEffectAndMotion(friend);

                // 自分のターンだったらその情報を表示
                // If it's your turn, display that information
                if (owner.battleState >= BattleState.SetEnemyBattleCommand &&
                    owner.battleState <= BattleState.SortBattleActions)
                {
                    if (playerData[i] == owner.commandSelectPlayer)
                    {
                        turnChr[i]?.setVisibility(true);
                        if (!friend.isActorStateQueued(BattleActor.ActorStateType.START_COMMAND_SELECT) &&
                            friend.getActorState() != BattleActor.ActorStateType.START_COMMAND_SELECT &&
                            friend.getActorState() != BattleActor.ActorStateType.COMMAND_SELECT)
                        {
                            friend.queueActorState(BattleActor.ActorStateType.START_COMMAND_SELECT);
                            friend.queueActorState(BattleActor.ActorStateType.COMMAND_SELECT);
                            continue;
                        }
                    }
                    else
                    {
                        if (!friend.isActorStateQueued(BattleActor.ActorStateType.BACK_TO_WAIT) &&
                            (friend.getActorState() == BattleActor.ActorStateType.START_COMMAND_SELECT ||
                            friend.getActorState() == BattleActor.ActorStateType.COMMAND_SELECT))
                        {
                            friend.queueActorState(BattleActor.ActorStateType.BACK_TO_WAIT);
                            friend.queueActorState(BattleActor.ActorStateType.COMMAND_STANDBY);
                            continue;
                        }
                    }
                }
            }

            // 状態に応じてモンスターにも演技させる
            // Let monsters act according to the state
            for (int i = 0; i < enemyMonsterData.Count; i++)
            {
                var actor = searchFromActors(enemyMonsterData[i]);
                if (actor == null)
                    continue;

                updateConditionEffectAndMotion(actor);

                actor.mapChr.setDirectionFromRadian(enemyMonsterData[i].directionRad);
            }

            oldState = owner.battleState;
        }

        internal void setEnemyActionReady(BattleEnemyData monsterData)
        {
            var actor = searchFromActors(monsterData);
            if (actor != null && !actor.source.IsDeadCondition())   // 敵がKOのときに一瞬起き上がるバグを修正 / Fixed a bug that caused the player to stand up momentarily when the enemy was KO'd
            {
                actor.queueActorState(BattleActor.ActorStateType.COMMAND_STANDBY);
            }
        }

        private void updateConditionEffectAndMotion(BattleActor actor)
        {
            // 行動不能時のモーションを設定する
            // Set motion when incapacitated
            var actionDisabledConditionMotion = "";
            var highestPriority = 0;
            foreach (var e in actor.source.conditionInfoDic)
            {
                var condition = e.Value.rom;
                if ((condition != null) && !string.IsNullOrEmpty(condition.motion))
                {
                    if (condition.actionDisabled && highestPriority < condition.Priority)
                    {
                        actionDisabledConditionMotion = condition.motion;
                        highestPriority = condition.Priority;
                    }
                }
            }

            // 状態異常エフェクトを同期
            // Synchronize status effects
            var removedList = actor.mapChr.ConditionEffectDrawerDic.Keys.Where(x => !actor.source.conditionInfoDic.ContainsKey(x.guId));
            var assignedList = actor.source.conditionInfoDic.Values.Where(x => !actor.mapChr.ConditionEffectDrawerDic.ContainsKey(x.rom));
            foreach (var removed in removedList.ToArray())
            {
                actor.mapChr.removeEffectDrawer(removed);
            }
            if (
                // 透明にする敵の場合は戦闘不能を除外
                // Exclude incapacity for invisible enemies
                //!(actor.source is BattleEnemyData && !((BattleEnemyData)actor.source).monster.visibleWhenKO && actor.source.IsDeadCondition()) &&
                IsEffectAllowShowDamage &&
                owner.battleState != BattleState.SetCommandEffect &&
                owner.battleState != BattleState.DisplayCommandEffect)
            {
                foreach (var assigned in assignedList.ToArray())
                    actor.mapChr.addEffectDrawer(assigned.rom);
            }

            // 行動不能
            // incapacitated
            // 基本的にはダメージ表示の後にしか遷移しないが、KO状態で他のステートが優先された場合はすぐにモーションチェンジする
            // Basically, it only transitions after the damage display, but if other states are prioritized in the KO state, the motion will change immediately.
            if (IsEffectAllowShowDamage &&
                owner.battleState != BattleState.SetCommandEffect &&
                owner.battleState != BattleState.DisplayCommandEffect &&
                actor.mapChr.isChangeMotionAvailable())
            {
                if (actor.source.IsActionDisabled())
                {
                    actor.queueActorState(BattleActor.ActorStateType.KO, actionDisabledConditionMotion);
                }
                else if (actor.getActorState() == BattleActor.ActorStateType.KO)
                {
                    actor.queueActorState(BattleActor.ActorStateType.COMMAND_STANDBY);
                }
            }
        }

        /// <summary>
        /// アクションの完了待ち
        /// Wait for action to complete
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public override bool IsEndMotion(BattleCharacterBase self)
        {
            var actor = searchFromActors(self);

            if (actor == null)
                return true;

            return actor.mapChr.isChangeMotionAvailable();
        }

        /// <summary>
        /// カメラ戻す
        /// camera back
        /// </summary>
        /// <param name="inTime"></param>
        public void restoreCamera(float inTime = 0)
        {
            skillUser = friends[0];

            camManager.setCameraFromPathName(catalog, true, Guid.Empty, Rom.Camera.NAME_BATTLE_WAIT);
            camManager.playAnimation();
        }

        private void
            executeAction(BattleCharacterBase self, bool action, bool start)
        {
            var actor = searchFromActors(self);

            if (action)
                skillUser = actor;

            // パーティから外すなどで既にアクターがいない場合がある
            // There may be cases where there are no actors already, such as removing them from the party.
            if (actor == null)
                return;

            if (action)
            {
                switch (self.selectedBattleCommandType)
                {
                    case BattleCommandType.Nothing:
                        if (!start)
                        {
                            restoreCamera();
                        }
                        break;
                    // 攻撃
                    // attack
                    case BattleCommandType.Attack:
                    case BattleCommandType.Critical:
                    case BattleCommandType.ForceCritical:
                        if (start)
                        {
                            string motion = null;
                            if (self is BattlePlayerData)
                            {
                                var pl = self as BattlePlayerData;
                                if (pl.player.equipments[0] != null)
                                    motion = pl.player.equipments[0].weapon.motion;
                            }
                            else if (self is BattleEnemyData)
                            {
                                var weapon = catalog.getItemFromGuid(((BattleEnemyData)self).monster.equipWeapon) as Common.Rom.NItem;
                                if (weapon != null)
                                    motion = weapon.weapon?.Motion;
                            }
                            if (game.data.system.BattleCameraEnabled[Common.GameData.SystemData.BATTLE_CAMERA_SITUATION_ATTACK])
                            {
                                actor.queueActorState(BattleActor.ActorStateType.ATTACK_WAIT, "", btc.attackTime);
                            }
                            else
                            {
                                actor.queueActorState(BattleActor.ActorStateType.ATTACK_WAIT, "", 10);
                            }

                            // Effekseerの場合ロード待ち0.1秒を加味する
                            // In the case of Effekseer, add 0.1 seconds to wait for loading
                            if (catalog.getItemFromGuid(actor.source.AttackEffect) is Common.Resource.Particle)
                                actor.queueActorState(BattleActor.ActorStateType.ATTACK_WAIT_FOR_EFFEKSEER_LOADING, "", 6);

                            actor.queueActorState(BattleActor.ActorStateType.ATTACK, motion);
                            var target = actor;
                            if (self.targetCharacter.Length > 0)
                                target = searchFromActors(self.targetCharacter[0]);
                            var selfPos = actor.frontDir > 0 ? actor.getPos() : target.getPos();
                            var targetPos = actor.frontDir > 0 ? target.getPos() : actor.getPos();
                            if (self is BattlePlayerData)
                                targetPos.y += target.Height * 0.66f;
                            else
                                selfPos.y += actor.Height * 0.66f;
                            var center = (selfPos * 1f + targetPos * 2f) * (1f / 3f);
                            float yAngle = (float)(Math.Atan2(selfPos.x - targetPos.x, selfPos.z - targetPos.z) / Math.PI * 180) + ATTACK_END_CAMERA.angle.Y;
                            if (game.data.system.BattleCameraEnabled[Common.GameData.SystemData.BATTLE_CAMERA_SITUATION_ATTACK])
                            {
                                camManager.setCameraFromPathName(catalog, true, Guid.Empty, Rom.Camera.NAME_BATTLE_ATTACK);
                                camManager.playAnimation();
                            }
                        }
                        else
                        {
                            actor.queueActorState(BattleActor.ActorStateType.ATTACK_END);
                            if (game.data.system.BattleCameraEnabled[Common.GameData.SystemData.BATTLE_CAMERA_SITUATION_ATTACK])
                            {
                                // カメラ戻す
                                // camera back
                                restoreCamera(btc.attackTime);
                            }
                        }
                        break;

                    // スキル
                    // skill
                    case BattleCommandType.Skill:
                    case BattleCommandType.SameSkillEffect:
                        if (start)
                        {
                            actor.queueActorState(BattleActor.ActorStateType.SKILL_WAIT);
                            actor.queueActorState(BattleActor.ActorStateType.SKILL, self.selectedSkill.option.motion, 0, () =>
                            {
                                if (actor.frontDir > 0)
                                {
                                    Audio.PlaySound(game.se.skill);
                                    skillEffect?.setVisibility(true);

                                    Vector3 neutralPos = actor.mapChr.pos;
                                    skillEffect?.setPosture(SharpKmyMath.Matrix4.translate(neutralPos.X, neutralPos.Y, neutralPos.Z));
                                }
                            });
                            //if (actor.frontDir > 0)
                            {
                                if (game.data.system.BattleCameraEnabled[Common.GameData.SystemData.BATTLE_CAMERA_SITUATION_SKILL])
                                {
                                    camManager.setCameraFromPathName(catalog, true, Guid.Empty, Rom.Camera.NAME_BATTLE_USE_SKILL);

                                    // Initializeが遅れてIsEffectEndPlay == true が返ってきてしまうので、Initialize前の動きを管理する
                                    // Since Initialize is delayed and IsEffectEndPlay == true is returned, manage the movement before Initialize
                                    bool isWaitInitialize = true;

                                    camManager.setWaitFunc(() =>
                                    {
                                        if (!IsEffectEndPlay)
                                            isWaitInitialize = false;
                                        var ok = (camManager.isSkillCameraPlaying(false) && IsEffectEndPlay && !isWaitInitialize) || owner.battleEvents.isBusy(false);
                                        return ok;
                                    });
                                    camManager.playAnimation();
                                }

                                var list = friends.Where(x => x != null).ToList();
                            }
                            if (self.selectedSkill != null)
                            {
                                lightCoeffBySkill = self.selectedSkill.option.lightCoeff;
                            }
                        }
                        else
                        {
                            actor.queueActorState(BattleActor.ActorStateType.SKILL_END);
                            skillEffect?.setVisibility(false);
                            lightCoeffBySkill = 1f;

                            // カメラ戻す
                            // camera back
                            restoreCamera(btc.skillTime / 2);
                            camManager.setWaitFunc(null);
                        }
                        break;

                    // アイテム
                    // item
                    case BattleCommandType.Item:
                        if (start)
                        {
                            actor.queueActorState(BattleActor.ActorStateType.ITEM);
                            if (actor.frontDir > 0)
                            {
                                if (game.data.system.BattleCameraEnabled[Common.GameData.SystemData.BATTLE_CAMERA_SITUATION_ITEM])
                                {
                                    camManager.setCameraFromPathName(catalog, true, Guid.Empty, Rom.Camera.NAME_BATTLE_USE_ITEM);
                                    camManager.playAnimation();
                                }
                                Audio.PlaySound(game.se.item);
                            }
                        }
                        else
                        {
                            actor.queueActorState(BattleActor.ActorStateType.ITEM_END);

                            // カメラ戻す
                            // camera back
                            restoreCamera(btc.itemTime / 2);
                        }
                        break;

                    // 交代
                    // replacement
                    case BattleCommandType.Change:
                        if (start)
                        {
                            actor.queueActorState(BattleActor.ActorStateType.CHANGE);
                            //if (actor.frontDir > 0)
                            {
                                if (game.data.system.BattleCameraEnabled[Common.GameData.SystemData.BATTLE_CAMERA_SITUATION_CHANGE])
                                {
                                    camManager.setCameraFromPathName(catalog, true, Guid.Empty, Rom.Camera.NAME_BATTLE_CHANGE);
                                    camManager.playAnimation();
                                }
                                Audio.PlaySound(game.se.change);
                            }
                        }
                        else
                        {
                            actor.queueActorState(BattleActor.ActorStateType.CHANGE_END);

                            // カメラ戻す
                            // camera back
                            restoreCamera(btc.changeTime / 2);
                        }
                        break;

                    // 交代(参加)
                    // Alternate (participate)
                    case BattleCommandType.Enter:
                        if (start)
                        {
                            self.IsStock = false;
                            actor.mapChr.setVisibility(isVisibility);
                            actor.queueActorState(BattleActor.ActorStateType.ENTER);
                            //if (actor.frontDir > 0)
                            {
                                if (game.data.system.BattleCameraEnabled[Common.GameData.SystemData.BATTLE_CAMERA_SITUATION_ENTER])
                                {
                                    camManager.setCameraFromPathName(catalog, true, Guid.Empty, Rom.Camera.NAME_BATTLE_ENTER);
                                    camManager.playAnimation();
                                }
                                Audio.PlaySound(game.se.enter);
                            }
                        }
                        else
                        {
                            actor.queueActorState(BattleActor.ActorStateType.ENTER_END);

                            // カメラ戻す
                            // camera back
                            restoreCamera(btc.enterTime / 2);
                        }
                        break;

                    // 交代(撤退)
                    // Substitution (withdrawal)
                    case BattleCommandType.Leave:
                        if (start)
                        {
                            actor.queueActorState(BattleActor.ActorStateType.LEAVE);
                            //if (actor.frontDir > 0)
                            {
                                if (game.data.system.BattleCameraEnabled[Common.GameData.SystemData.BATTLE_CAMERA_SITUATION_LEAVE])
                                {
                                    camManager.setCameraFromPathName(catalog, true, Guid.Empty, Rom.Camera.NAME_BATTLE_LEAVE);
                                    camManager.playAnimation();
                                }
                                Audio.PlaySound(game.se.leave);
                            }
                        }
                        else
                        {
                            self.IsStock = true;
                            actor.mapChr.setVisibility(false);
                            actor.queueActorState(BattleActor.ActorStateType.LEAVE_END);

                            // カメラ戻す
                            // camera back
                            restoreCamera(btc.leaveTime / 2);
                        }
                        break;


                    // ガード
                    // guard
                    case BattleCommandType.Guard:
                        if (start)
                        {
                            actor.queueActorState(BattleActor.ActorStateType.GUARD);
                        }
                        else
                        {
                            // カメラ戻す
                            // camera back
                            restoreCamera();
                        }
                        break;

                    // ためる
                    // accumulate
                    case BattleCommandType.Charge:
                        if (start)
                        {
                            actor.queueActorState(BattleActor.ActorStateType.CHARGE);
                        }
                        else
                        {
                            // カメラ戻す
                            // camera back
                            restoreCamera();
                        }
                        break;

                    // 逃げる
                    // run away
                    case BattleCommandType.PlayerEscape:
                        if (!start)
                            friends.FirstOrDefault(x => x.getActorState() != BattleActor.ActorStateType.KO)?.queueActorState(BattleActor.ActorStateType.ESCAPE_FAILED);
                        //actor.queueActorState(BattleActor.ActorStateType.ESCAPE_FAILED);
                        break;

                    // 敵が逃げる
                    // enemy flees
                    case BattleCommandType.MonsterEscape:
                        if (start)
                        {
                            actor.queueActorState(BattleActor.ActorStateType.ESCAPE);
                        }
                        break;
                }
            }
            else
            {
                switch (self.CommandReactionType)
                {
                    case ReactionType.Heal:
                        if (!self.IsActionDisabled())
                        {
                            actor.queueActorState(BattleActor.ActorStateType.COMMAND_STANDBY);
                        }
                        break;
                    case ReactionType.Damage:
                        if (start)
                        {
                            if (actor.source.HitPoint > 0)
                                actor.queueActorState(BattleActor.ActorStateType.DAMAGE);
                            else
                                actor.queueActorState(BattleActor.ActorStateType.FATAL_DAMAGE);
                        }
                        else if (!self.IsActionDisabled() && (actor.getActorState() != BattleActor.ActorStateType.FATAL_DAMAGE))
                        {
                            actor.queueActorState(BattleActor.ActorStateType.COMMAND_STANDBY);
                        }
                        break;
                    case ReactionType.None:// ミスの時はこっち / Here when you make a mistake
                        if (start)
                        {
                            actor.queueActorState(BattleActor.ActorStateType.DODGE);
                        }
                        else if (!self.IsActionDisabled() && (actor.getActorState() != BattleActor.ActorStateType.FATAL_DAMAGE))
                        {
                            actor.queueActorState(BattleActor.ActorStateType.COMMAND_STANDBY);
                        }
                        break;
                }
            }
        }

        internal void FixedUpdate()
        {
            foreach (var actor in friends)
            {
                if (actor == null)
                    continue;

                actor.mapChr.FixedUpdate();
            }

            foreach (var actor in enemies)
            {
                if (actor == null)
                    continue;

                actor.mapChr.FixedUpdate();
            }

            foreach (var actor in extras)
            {
                if (actor == null)
                    continue;

                actor.FixedUpdate();
            }
        }

        internal bool isActiveCharacterReady()
        {
            // Attackが予備動作まで済んでいるか調べる
            // Check if Attack has completed preliminary operation
            var actor = searchFromActors(owner.activeCharacter);
            if (actor != null)
            {
                switch (owner.activeCharacter.selectedBattleCommandType)
                {
                    case BattleCommandType.Attack:
                    case BattleCommandType.Critical:
                    case BattleCommandType.ForceCritical:
                        if (actor.getActorState() != BattleActor.ActorStateType.ATTACK)
                        {
                            return false;
                        }
                        break;
                    default:
                        break;
                }
            }

            return true;
        }

        /// <summary>
        /// 描画処理
        /// drawing process
        /// </summary>
        /// <param name="scn"></param>
        public void draw(SharpKmyGfx.Render scn)
        {
            if (mapDrawer.currentRom == null)
                return;

            {
                // ◆通常時
                // ◆Normal time

                var tmp = MapData.pickupscene;
                MapData.pickupscene = null;

                var asp = game.getScreenAspect();
                createCameraMatrix(out p, out v/*, Rom.Camera.Now*/, asp);
                scn.setViewMatrix(p, v);

                var refmap = Catalog.sInstance.getItemFromGuid(mapDrawer.mapRom.renderParams.reflection) as Common.Resource.Texture;
                GameMain.instance.gameView.setReflectionMap(refmap != null ? refmap.getTexture() : null);

                mapDrawer.setLightCoeff(
                    1 - (1 - camManager.m_last_light.X) * lightCoeffBySkill,
                    1 - (1 - camManager.m_last_light.Y) * lightCoeffBySkill,
                    camManager.m_last_light.Z * lightCoeffBySkill);
                mapDrawer.setDofCoeff(camManager.m_last_dof);
                mapDrawer.applyLight(GameMain.instance.gameView, p, v, catalog.getGameSettings().useBillboardChrStencilShadow, mCamLookAtTarget);
                mapDrawer.setLookAtPos(mCamLookAtTarget);
                mapDrawer.afterViewPositionFixProc(scn);
                if (mapDrawer.sky != null)
                {
                    mapDrawer.sky.setProj(SharpKmyMath.Matrix4.perspectiveFOV(
                        Microsoft.Xna.Framework.MathHelper.ToRadians(camManager.m_fovy), asp, 1, 1000));
                }

                mapDrawer.draw(scn);

                MapData.pickupscene = tmp;
                tmp = null;
            }
        }

        public void setFriendsVisibility(bool flg)
        {
            isVisibility = flg;

            foreach (var f in friends)
            {
                if (f != null)
                {
                    f.mapChr.setVisibility(flg && !(f.source?.IsStock ?? false));
                }
            }
        }

        /// <summary>
        /// カメラ行列の生成
        /// Generate Camera Matrix
        /// </summary>
        /// <param name="p"></param>
        /// <param name="v"></param>
        /// <param name="asp"></param>
        internal void createCameraMatrix(out SharpKmyMath.Matrix4 p, out SharpKmyMath.Matrix4 v/*, Rom.ThirdPersonCameraSettings camera*/, float asp)
        {
            var farclip = 2000;
            var target = SharpKmyMath.Vector3.zero;

            if (skillUser.mapChr != null)
            {
                target = skillUser.getPos();
                target.y += skillUser.Height / 2;
            }

            SharpKmyMath.Vector3 vecUp = new SharpKmyMath.Vector3(0, 1, 0);

#if WINDOWS
#else
            // イベント等のカメラワークが左右反転しているのを修正
            // Fixed that the camera work of events etc. is horizontally reversed
            campos.x *= -1;
            // 地形の高さの変位とカメラの高さの変位が±逆になっていたのを修正
            // Fixed that terrain height displacement and camera height displacement were reversed ±
            target.y *= -1;
#endif

            var lookat = target + campos;


            if (camManager.ntpCamera != null && owner.IsDrawingBattleScene)
            {
                // 新カメラ版
                // new camera version
                //target.y = c.offset.Y;
                camManager.animationCameraMatrix(catalog, mapDrawer.mapRom, target, shakeValue, asp, farclip, out p, out v, out mCamLookAtTarget, 0f, 0f);
                campos = camManager.m_intp_campos;
            }
            else
            {
                v = SharpKmyMath.Matrix4.lookat(lookat, target, vecUp);
                v = SharpKmyMath.Matrix4.inverse(v);
                p = new SharpKmyMath.Matrix4();
            }

        }

        /// <summary>
        /// カメラアニメを強制終了する
        /// Force quit camera animation
        /// </summary>
        internal void StopCameraAnimation()
        {
            if (camManager.ntpCamera != null)
            {
                var farclip = 2000;
                var target = SharpKmyMath.Vector3.zero;

                if (skillUser.mapChr != null)
                {
                    target = skillUser.getPos();
                    target.y += skillUser.Height / 2;
                }

                // 新カメラ版
                // new camera version
                //target.y = c.offset.Y;
                camManager.animationCameraMatrix(catalog, mapDrawer.mapRom, target, shakeValue, 1, farclip, out p, out v, out mCamLookAtTarget, 0f, 0f);

                // アニメーション終了時は操作可能カメラに戻す
                // Return to operable camera when animation ends
                owner.battleEvents.CopyFromCameraManager(camManager);
                campos = mCamLookAtTarget;
                camManager.ntpCamera = null;
                camManager.stopAnimation();
            }
        }

        /// <summary>
        /// キャラクタを描画
        /// draw a character
        /// </summary>
        /// <param name="scn"></param>
        private void drawCharacters(SharpKmyGfx.Render scn)
        {
            foreach (var mapChr in friends)
            {
                if (mapChr == null)
                    continue;

                if ((mapChr.source != null) && mapChr.source.IsStock)
                {
                    continue;
                }

                mapChr.Draw(scn);
            }
            foreach (var mapChr in extras)
            {
                mapChr.draw(scn);
            }
            foreach (var mapChr in enemies)
            {
                if (mapChr == null)
                    continue;

                if ((mapChr.source != null) && mapChr.source.IsStock)
                {
                    continue;
                }

                mapChr.Draw(scn);
            }
            owner.battleEvents?.Draw(scn);
        }

        private void setMapRom(Common.Rom.Map map)
        {
            if (map != null)
            {
                mapDrawer = battleMapDrawer;

                if (GameMain.instance.mapScene.loadManager.Contains(map) &&
                    !GameMain.instance.mapScene.loadManager.GetOrAddJob(map).isLoading)
                {
                    mapDrawer = GameMain.instance.mapScene.loadManager.GetOrAddJob(map).data;
                }
                else
                {
                    battleMapDrawer.setRom(map, false, false, Common.Util.BATTLE3DDISPLAYID, true);

                    battleMapDrawer.setLookAtPos(new SharpKmyMath.Vector3(
                        BattleSequenceManagerBase.battleFieldCenter.X, 1,
                        BattleSequenceManagerBase.battleFieldCenter.Z));
                }

                initializeExtras(map);
            }
        }

        private void initializeExtras(Rom.Map map)
        {
            BattleActor.map = mapDrawer;

            var events = map.getEvents();

            // 既に生成済みだったら抜ける
            // Exit if already generated
            if (extras.Count > 0 && events.Exists(x => x.guId == extras[0].rom.guId))
                return;

            // イベントをマップ上に表示する
            // Show events on the map
            foreach (var chr in extras)
            {
                chr.Reset();
            }
            extras.Clear();

            // デフォルトのバトルフィールドの場合、イベントは読み込まない
            // For the default Battlefield, the event doesn't load
            if (map.isReadOnly()) return;

            foreach (var evRef in events)
            {
                var ev = catalog.getItemFromGuid(evRef.guId) as Common.Rom.Event;
                if (ev != null)
                {
                    var chr = new MapCharacter(null, null);// コンストラクタで ev をセットすると、C#の初期化が走ってしまうので別途セットする / If you set ev in the constructor, C# initialization will run, so set it separately
                    chr.rom = ev;
                    chr.setDisplayID(Common.Util.BATTLE3DDISPLAYID);
                    chr.setRotation(evRef.rotate);
                    chr.setPosition(evRef.pos.X, evRef.pos.Y, evRef.pos.Z);
                    //chr.ChangeGraphic(catalog.getItemFromGuid(ev.Graphic) as Common.Resource.GfxResourceBase, drawer);
                    //chr.playMotion(ev.Motion, 0);
                    extras.Add(chr);
                }
            }
        }

        internal void prepare()
        {
            var mapScene = game.mapScene;
            catalog = mapScene.owner.catalog;

            prepare(mapScene.map.getBattleBg(catalog, mapScene.battleSetting));
        }

        internal void prepare(Guid battleBg)
        {
            DisplayIdUtil.changeScene(DisplayIdUtil.SceneType.BATTLE, true);

            var mapScene = game.mapScene;
            catalog = mapScene.owner.catalog;

            reset();

            if ((mapScene.battleSetting.battleBgCenterX > 0) || (mapScene.battleSetting.battleBgCenterZ > 0))
            {
                BattleSequenceManagerBase.battleFieldCenter = new Vector3(mapScene.battleSetting.battleBgCenterX + 0.5f, 1, mapScene.battleSetting.battleBgCenterZ + 0.5f);
            }
            else
            {
                BattleSequenceManagerBase.battleFieldCenter = new Vector3((mapScene.map.Width / 2) + 0.5f, 1, (mapScene.map.Height / 2) + 0.5f);
            }
            initCameraParams();
            SetBackGround(battleBg);

            // 味方キャラを読み込んでおく
            // Load allied characters
            int count = 0;
            int max = mapScene.owner.data.party.PlayerCount;

            // 最大の分確保しておく
            // reserve the maximum
            var addCnt = catalog.getGameSettings().BattlePlayerMax;
            friends.Clear();
            for (int i = 0; i < addCnt; i++)
            {
                friends.Add(null);
            }

            max = Math.Min(mapScene.owner.data.party.PlayerCount, catalog.getGameSettings().BattlePlayerMax);

            foreach (var chr in mapScene.owner.data.party.Players)
            {
                if (count >= max)
                    break;
                BattleActor.party = game.data.party;
                friends[count] = BattleActor.GenerateFriend(catalog, chr, count % max, max);
                count++;
            }
        }

        internal void SetBackGroundCenter(int battleBgCenterX, int battleBgCenterY)
        {
            if (!mapDrawer.mapRom.isReadOnly())
            {
                if (battleBgCenterX > 0)
                {
                    BattleSequenceManagerBase.battleFieldCenter = new Vector3(battleBgCenterX + 0.5f, 1, battleBgCenterY + 0.5f);
                }
                else
                {
                    BattleSequenceManagerBase.battleFieldCenter = new Vector3((mapDrawer.mapRom.Width / 2) + 0.5f, 1, (mapDrawer.mapRom.Height / 2) + 0.5f);
                }
                initCameraParams();
            }
        }

        internal void UpdateCollisionDepotInUnity()
        {
#if !WINDOWS
            mapDrawer.updateCollisionDepotInUnity();
#endif
            return;
        }

        // マップ範囲外をバトル背景に指定していた場合に補正する処理
        // Processing to correct when outside the map range is specified as the battle background
        private void limitCenterOfField(Common.Rom.Map map)
        {
            if (map == null)
                return;

            BattleSequenceManagerBase.battleFieldCenter.X = calcLimit((int)BattleSequenceManagerBase.battleFieldCenter.X,
                (int)BattleCharacterPosition.DEFAULT_BATTLE_FIELD_SIZE.X, map.Width) + 0.5f;
            BattleSequenceManagerBase.battleFieldCenter.Z = calcLimit((int)BattleSequenceManagerBase.battleFieldCenter.Z,
                (int)BattleCharacterPosition.DEFAULT_BATTLE_FIELD_SIZE.Z, map.Height) + 0.5f;
        }

        private int calcLimit(int pos, int area, int max)
        {
            if (pos + area / 2 > max)
            {
                if (max > area)
                {
                    pos = max - area / 2 - 1;
                }
                else
                {
                    pos = max / 2;
                }
            }

            return pos;
        }

        private void setFriendsPositionImpl(BattleCharacterBase chr, BattleActor friend)
        {
            Vector3 neutralPos;

            neutralPos.X = chr.pos.X;
            neutralPos.Z = chr.pos.Z;
            neutralPos.Y = mapDrawer?.getAdjustedHeight(neutralPos.X, neutralPos.Z) ?? 0;

            friend.mapChr.setPosition(neutralPos);
        }

        private void prepareFriends(List<BattlePlayerData> playerData)
        {
            // 味方キャラを読み込む
            // load an ally character
            int count = -1;
            // 最大の分確保しておく
            // reserve the maximum
            var addCnt = catalog.getGameSettings().BattlePlayerMax - friends.Count;
            for (int i = 0; i < addCnt; i++)
            {
                friends.Add(null);
            }

            var max = Math.Min(playerData.Count, catalog.getGameSettings().BattlePlayerMax);

            foreach (var chr in playerData)
            {
                chr.actionHandler = executeAction;
                count++;

                if (count >= max)
                    break;

                var friend = friends[count];
                var isCreate = true;

                if (friend != null)
                {
                    friend.source = chr;
                    if (friend.sourceEqual(game.data.party.getMemberGraphic(chr.player.rom)))
                    {
                        friend.resetState(true, count % max, max);
                        friend.mapChr.pos.Y = 0;
                        BattleActor.createWeaponModel(ref friend, catalog);
                        chr.mapChr = friend.mapChr;
                        setFriendsPositionImpl(chr, friend);
                        isCreate = false;
                    }
                    else
                    {
                        friend.Release();
                    }
                }

                if (isCreate)
                {
                    BattleActor.party = game.data.party;
                    friends[count] = BattleActor.GenerateFriend(catalog, chr.player, count % max, max);
                    friends[count].source = chr;
                    chr.mapChr = friends[count].mapChr;
                    setFriendsPositionImpl(chr, friends[count]);
                }

                if (chr.mapChr != null)
                {
                    foreach (var item in chr.conditionInfoDic)
                    {
                        chr.mapChr.addEffectDrawer(item.Value.rom);
                    }
                }
            }

            setFriendsVisibility(true);

            // キャラを破棄
            // discard character
            count++;
            for (int i = count; i < friends.Count; i++)
            {
                if (friends[i] != null)
                {
                    friends[i].Release();
                    friends[i] = null;
                }
            }
        }

        private void prepareEnemies(List<BattleEnemyData> enemyMonsterData)
        {
            // 敵キャラを読み込む
            // load an enemy character
            int count = 0;
            int max = enemyMonsterData.Count;
            var addCnt = max - enemies.Count;
            for (int i = 0; i < addCnt; i++)
            {
                enemies.Add(null);
            }

            foreach (var chr in enemyMonsterData)
            {
                chr.actionHandler = executeAction;
                if (enemies[count] != null)
                    enemies[count].Release();
                var res = Common.Catalog.sInstance.getItemFromGuid(chr.monster.graphic) as Common.Resource.GfxResourceBase;
                enemies[count] = BattleActor.GenerateEnemy(catalog, res, count, max);
                chr.mapChr = enemies[count].mapChr;
                if (chr.IsManualPosition)
                {
                    Vector3 neutralPos;

                    neutralPos.X = chr.pos.X;
                    neutralPos.Z = chr.pos.Z;
                    neutralPos.Y = mapDrawer?.getAdjustedHeight(neutralPos.X, neutralPos.Z) ?? 0;

                    enemies[count].mapChr.setPosition(neutralPos);
                }
                else
                {
                    chr.arrangmentType = BattleCharacterPosition.getPositionType(count, max);
                }
                enemies[count].source = chr;
                enemies[count].mapChr.setVisibility(!chr.IsStock);
                count++;
            }

            // キャラを破棄
            // discard character
            for (int i = count; i < enemies.Count; i++)
            {
                if (enemies[i] != null)
                {
                    enemies[i].Release();
                    enemies[i] = null;
                }
            }
        }

        internal void reset()
        {
            // マップを破棄
            // discard map
            if (battleMapDrawer != null)
            {
                battleMapDrawer.Reset();
            }

            // キャラを破棄
            // discard character
            for (int i = 0; i < friends.Count; i++)
            {
                if (friends[i] != null)
                {
                    friends[i].Release();
                    friends[i] = null;
                }
            }
            foreach (var chr in extras)
            {
                chr.Reset();
            }
            extras.Clear();
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null)
                {
                    enemies[i].Release();
                    enemies[i] = null;
                }
            }
        }

        internal override bool IsVisibleWindow(WindowType windowType)
        {
            // メッセージウィンドウ
            // message window
            var visibleMessage = windowType == WindowType.MessageWindow ||
                windowType == WindowType.CommandTargetPlayerListWindow ||
                windowType == WindowType.CommandTargetMonsterListWindow;
            if (visibleMessage)
            {
                return ui.message.IsVisible();
            }

            var visibleCommand = windowType == WindowType.PlayerCommandWindow;
            if (visibleCommand)
            {
                return ui.command.IsVisible();
            }

            var visibleItemList = windowType == WindowType.ItemListWindow;
            if (visibleItemList)
            {
                return ui.itemList.IsVisible();
            }

            // スキルリスト
            // skill list
            var visibleSkillList = windowType == WindowType.SkillListWindow;
            if (visibleSkillList)
            {
                return ui.skillList.IsVisible();
            }

            return false;
        }


        internal void Show()
        {
            mapDrawer.mapDrawCallBack += drawCharacters;
            mapDrawer.setDisplayID(Util.BATTLE3DDISPLAYID);

            mapDrawer.setRenderSettings(mapDrawer.renderSettings);
        }

        internal void Hide()
        {
            mapDrawer.mapDrawCallBack -= drawCharacters;

            if (mapDrawer != battleMapDrawer)
            {
                mapDrawer.setDisplayID(GameMain.instance.mapScene.mapDrawer == mapDrawer ? GameMain.sDefaultDisplayID : uint.MaxValue);
            }

            if (GameMain.instance.mapScene?.mapDrawer != null)
                GameMain.instance.mapScene.mapDrawer.setRenderSettings(GameMain.instance.mapScene.mapDrawer.renderSettings);
        }
    }
}

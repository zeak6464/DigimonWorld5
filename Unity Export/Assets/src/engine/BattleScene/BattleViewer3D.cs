using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;

using Yukar.Common;

using Rom = Yukar.Common.Rom;
using StatusAilments = Yukar.Common.GameData.Hero.StatusAilments;

namespace Yukar.Engine
{
    public class BattleViewer3D : BattleViewer
    {
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
        internal MapData drawer;
        internal BattleActor[] friends = new BattleActor[4];
        internal BattleActor[] enemies = new BattleActor[6];
        internal List<MapCharacter> extras = new List<MapCharacter>();
        private MapCharacter[] turnChr = new MapCharacter[4];
        private MapCharacter skillEffect = new MapCharacter();
        private Catalog catalog;
        SharpKmyMath.Matrix4 p, v;
        internal BattleCameraController camera;
        private Rom.GameSettings.BattleCamera btc;

        public static Vector2 CenterOfField = BattleCharacterPosition.DEFAULT_BATTLE_FIELD_CENTER;
        private Common.Rom.ThirdPersonCameraSettings INITIAL_CAMERA;
        private Common.Rom.ThirdPersonCameraSettings NORMAL_CAMERA;
        private Common.Rom.ThirdPersonCameraSettings ATTACK_START_CAMERA;
        private Common.Rom.ThirdPersonCameraSettings ATTACK_END_CAMERA;
        private Common.Rom.ThirdPersonCameraSettings MAGIC_START_CAMERA;
        private Common.Rom.ThirdPersonCameraSettings MAGIC_USER_CAMERA;
        private Common.Rom.ThirdPersonCameraSettings ITEM_START_CAMERA;
        private Common.Rom.ThirdPersonCameraSettings ITEM_USER_CAMERA;
        private Common.Rom.ThirdPersonCameraSettings RESULT_START_CAMERA;
        private Common.Rom.ThirdPersonCameraSettings RESULT_END_CAMERA;
        
        private WindowDrawer statusWindow;
        private WindowDrawer commandWindow;

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
            RESULT_START_CAMERA = createCameraSetting(btc.resultStart);
            RESULT_END_CAMERA = createCameraSetting(btc.resultEnd);
        }

        private Rom.ThirdPersonCameraSettings createCameraSetting(Rom.ThirdPersonCameraSettings settings)
        {
            var result = new Rom.ThirdPersonCameraSettings();
            result.copyFrom(settings);
            result.x += CenterOfField.X + 0.5f;
            result.y += CenterOfField.Y + 0.5f;
            return result;
        }

        private BattleSequenceManager.BattleState oldState;
        internal SharpKmyMath.Vector2 shakeValue;

        internal BattleViewer3D(GameMain owner) : base(owner)
        {
            UnityUtil.changeScene(UnityUtil.SceneType.BATTLE);
            drawer = new MapData();
            drawer.isExpandViewRange = false;
            drawer.drawMapObject = MapData.PICKUP_DRAW_MAPOBJECT.kNONE;
            drawer.mapDrawCallBack += drawCharacters;
            BattleActor.map = drawer;

            for (int i = 0; i < turnChr.Length; i++)
            {
                turnChr[i] = new MapCharacter();
                var res = owner.catalog.getItemFromName("ef018_GlowPanel", typeof(Common.Resource.Character)) as Common.Resource.ResourceItem;
                if (res != null)
                {
                    turnChr[i].ChangeGraphic(res, null);
                    turnChr[i].hide |= MapCharacter.HideCauses.BY_BATTLE;
                }
            }

            {
                skillEffect = new MapCharacter();
                var res = owner.catalog.getItemFromName("ef021_Glow_C", typeof(Common.Resource.Character)) as Common.Resource.ResourceItem;
                if (res != null)
                {
                    skillEffect.ChangeGraphic(res, null);
                    skillEffect.hide |= MapCharacter.HideCauses.BY_BATTLE;
                }
            }

            battleField = new BattleField(this);

#if ENABLE_VR
            // VRカメラデータ設定
            SetupVrCameraData(false);
#endif	// #if ENABLE_VR
        }

        public override void LoadResourceData(Catalog catalog, Rom.GameSettings gameSettings)
        {
            base.LoadResourceData(catalog, gameSettings);

            // ステータスとコマンドの窓を読み込む
            var winRes = catalog.getItemFromGuid(gameSettings.systemGraphics.battleCommands3D) as Common.Resource.Window;
            commandWindow = new WindowDrawer(winRes, winRes == null ? -1 : Graphics.LoadImage(winRes.path));
            winRes = catalog.getItemFromGuid(gameSettings.systemGraphics.battleStatus3D) as Common.Resource.Window;
            statusWindow = new WindowDrawer(winRes, winRes == null ? -1 : Graphics.LoadImage(winRes.path));
        }

        internal void finalize()
        {
            for (int i = 0; i < turnChr.Length; i++)
            {
                if (turnChr[i] != null)
                {
                    turnChr[i].Reset();
                }
            }

            {
                if (skillEffect != null)
                {
                    skillEffect.Reset();
                }
            }
        }

        internal override System.Collections.IEnumerator SetBackGround(Guid battleBg)
        {
            UnityUtil.changeScene(UnityUtil.SceneType.BATTLE);
            var bgMap = catalog.getItemFromGuid(battleBg) as Common.Rom.Map;
            if (bgMap == null)
                bgMap = catalog.getItemFromGuid(Common.Rom.Map.DEFAULT_3D_BATTLEBG) as Common.Rom.Map;

            if (drawer.mapRom == null || drawer.mapRom.guId != bgMap.guId)
            {
                if (drawer.mapRom != null)
                    drawer.Reset();

                var currentMap = game.mapScene.map;
                var map = new Common.Rom.Map();
                map.copyFrom(bgMap);
                map.ambLightColor = currentMap.ambLightColor;
                map.dirLightColor = currentMap.dirLightColor;
                map.envEffect = currentMap.envEffect;
                map.bgType = currentMap.bgType;
                map.skyModel = currentMap.skyModel;
                map.fogColor = currentMap.fogColor;

                if (map.isReadOnly())
                {
                    CenterOfField = BattleCharacterPosition.DEFAULT_BATTLE_FIELD_CENTER;
                }

                limitCenterOfField(map);
                initCameraParams();

                // マップを読み込む
                if (map != null)
                {
                    var coroutine = setMapRom(map);
                    while (coroutine.MoveNext()) yield return null;
                }
            }
        }

        internal override void BattleStart(List<BattlePlayerData> playerData, List<BattleEnemyData> enemyMonsterData)
        {
            if (game.data.system.BattleCameraEnabled[Common.GameData.SystemData.BATTLE_CAMERA_SITUATION_START])
            {
                camera.set(INITIAL_CAMERA, 40);
                camera.push(NORMAL_CAMERA, btc.startTime, btc.startTween, BattleCameraController.TAG_FORCE_WAIT);
            }
            else
            {
                camera.set(NORMAL_CAMERA, 0);
            }

            base.BattleStart(playerData, enemyMonsterData);

            BattleActor.map = drawer;
            prepareFriends(playerData);
            prepareEnemies(enemyMonsterData);

#if ENABLE_VR
            // VRカメラ設定
            {
                // オフセット座標設定
                if (drawer.mapRom != null)
                {
                    int x = drawer.mapRom.Width / 2;
                    int y = drawer.mapRom.Height / 2;

                    m_VrCameraData.SetOutOffsetPos(
                        drawer.mapRom.Width * 0.5f,
                        drawer.getAdjustedHeight(x, y),
                        drawer.mapRom.Height * 0.5f
                        );
                }

                // VR情報更新
                m_VrCameraData.UpdateInfo(Common.Rom.Map.CameraControlMode.NORMAL);
            }
#endif  // #if ENABLE_VR
        }

        //------------------------------------------------------------------------------
        /**
         *	描画処理
         */
        internal override void Draw(List<BattlePlayerData> playerData, List<BattleEnemyData> enemyMonsterData)
        {
            UnityUtil.changeScene(UnityUtil.SceneType.BATTLE);
            drawEnemyInfo(enemyMonsterData);

            drawPlayerInfo(playerData);

            drawDamageText(playerData, enemyMonsterData);

            DrawField(playerData, enemyMonsterData);
        }

        internal override void DrawWindows(List<BattlePlayerData> playerData)
        {
            if (displayWindow == WindowType.PlayerCommandWindow)
                drawCommandWindow();

            Draw_Tiny(playerData);
        }

        //------------------------------------------------------------------------------
        /**
         *	コマンドウィンドウを描画
         */
        private void drawCommandWindow()
        {
            // 位置とサイズを計算する
            var windowPos = Vector2.Zero;
            var windowSize = Vector2.Zero;
            if (battleCommandChoiceWindowDrawer.ChoiceItemCount <= BattlePlayerData.RegisterBattleCommandCountMax)
            {
#if WINDOWS
                windowSize = new Vector2(230, 32 * 5 + 10);
#else
                windowSize = new Vector2(230, 48 * 5 + 10);
#endif
            }
            else
            {
#if WINDOWS
                windowSize = new Vector2(230, 32 * 6 + 10);
#else
                windowSize = new Vector2(230, 48 * 6 + 10);
#endif
            }
#if WINDOWS
            windowPos = new Vector2(0, 544 - windowSize.Y);
#else
            windowPos = new Vector2(16, 32);
#endif
            windowPos.Y += (windowSize.Y / 2 - (windowSize.Y / 2 * tweenBattleCommandWindowScale.CurrentValue.Y));
            windowSize *= tweenBattleCommandWindowScale.CurrentValue;

            // 枠を書く
            commandWindow.Draw(new Vector2(windowPos.X - commandWindow.paddingLeft, windowPos.Y - commandWindow.paddingTop),
                new Vector2(windowSize.X + commandWindow.paddingLeft + commandWindow.paddingRight,
                            windowSize.Y + commandWindow.paddingBottom + commandWindow.paddingTop));
            //Graphics.DrawFillRect((int)windowPos.X + commandTextWidth - 1, (int)windowPos.Y - 12,
            //    (int)windowSize.X - commandTextWidth + 2, 4, 0, 0, 0, 127);
            //Graphics.DrawFillRect((int)windowPos.X + commandTextWidth, (int)windowPos.Y - 11,
            //    (int)windowSize.X - commandTextWidth, 2, 255, 255, 255, 255);

            // Commandと書く
            //var gs = catalog.getGameSettings();

            // コマンドウィンドウを書く
            battleCommandChoiceWindowDrawer.CursorColor = choiceWindowCursolColor.getColor();
            battleCommandChoiceWindowDrawer.Draw(windowPos, windowSize);
        }

        //------------------------------------------------------------------------------
        /**
         *	フィールドを描画
         */
        internal void DrawField(List<BattlePlayerData> playerData, List<BattleEnemyData> enemyMonsterData)
        {
            // エフェクト
            drawEffect();

#if ENABLE_VR

            if (SharpKmyVr.Func.IsReady())
            {
#if !VR_SIDE_BY_SIDE
                // 通常描画
                SharpKmyGfx.Render.getDefaultRender().addDrawable(battleField);
#endif  // #if !VR_SIDE_BY_SIDE

                // VR描画
                VrDrawer.DrawVr(battleField, m_VrCameraData);
            }
            else
            {
                // 通常描画
                SharpKmyGfx.Render.getDefaultRender().addDrawable(battleField);
            }

#else  // #if ENABLE_VR

            SharpKmyGfx.Render.getDefaultRender().addDrawable( battleField );

#endif // #if ENABLE_VR
        }

        //------------------------------------------------------------------------------
        /**
         *	エフェクトを描画
         */
        private void drawEffect()
        {
            if (monsterEffectDrawer.rom != null && !monsterEffectDrawer.isEndPlaying)
            {
                if (monsterEffectDrawer.rom.origPos == Rom.Effect.OrigPos.ORIG_SCREEN)
                {
                    // 画面全体を対象としたエフェクトの場合、ターゲット色だけを反映して、エフェクト自体は画面全体に出す
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

            if (playerEffectDrawer.rom != null && !playerEffectDrawer.isEndPlaying)
            {
                if (playerEffectDrawer.rom.origPos == Rom.Effect.OrigPos.ORIG_SCREEN)
                {
                    // 画面全体を対象としたエフェクトの場合、ターゲット色だけを反映して、エフェクト自体は画面全体に出す
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
        }

        //------------------------------------------------------------------------------
        /**
         *	エネミー情報を描画
         */
        private void drawEnemyInfo(List<BattleEnemyData> enemyMonsterData)
        {
            BattleEnemyData currentSelectMonster = null;

            // 敵の画像に関連する情報を表示
            foreach (var monster in enemyMonsterData)
            {
                var color = Color.White;
                if (monster.imageAlpha < 1)
                {
                    color = new Color(monster.imageAlpha, monster.imageAlpha, monster.imageAlpha, monster.imageAlpha);
                }

                float scale = 1.0f;
                EffectDrawer effect = null;

                var actor = searchFromActors(monster);

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
                else if (displayWindow == WindowType.CommandTargetMonsterListWindow)
                {
                    if (monster.IsSelect)
                    {
                        // ターゲットとして選択されている時は点滅させる
                        color = blinker.getColor();
                        currentSelectMonster = monster;
                    }
                    else if (monster.imageAlpha > 0)
                    {
                        color.R = color.G = color.B = 64;
                        color.A = 192;
                    }
                }
                else if (actor.overRidedColor != null)
                {
                    color = actor.overRidedColor.Value;
                    actor.overRidedColor = null;
                }

                if (effect != null)
                {
                    color = new Color(color.ToVector4() * effect.getNowTargetColor().ToVector4());
                    if (effect.rom != null)
                    {
                        monster.EffectPosition = actor.getScreenPos(p, v,
                            effect.rom.origPos == Rom.Effect.OrigPos.ORIG_FOOT ? MapScene.EffectPosType.Ground : MapScene.EffectPosType.Body);
                    }
                }

                if (monster.commandEffectColor.IsPlayTween)
                {
                    color = monster.commandEffectColor.CurrentValue;
                }

                actor.setColor(color);
            }

            // 選択解除
            if (enemyMonsterData.Count(monster => monster.IsSelect) == 0)
            {
                prevSelectedMonster = null;
            }

            // 現在選択しているモンスターの ステータスUp情報, ステータスDown情報, 状態異常 を表示する
            // 他のモンスターの画像に隠れないように全てのモンスターの画像を描画し終わってから描画
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
                Vector2 effectDrawPosition = searchFromActors(currentSelectMonster).getScreenPos(p, v);
                effectDrawPosition.X -= (effects.Count - 1) * 0.5f * effectGraphicsSize.X;
                effectDrawPosition.Y -= effectGraphicsSize.Y;

                // エフェクトがメッセージウィンドウと重ならないように位置を調整
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
        }

#if WINDOWS
        int STATUS_X { get { return Graphics.ScreenWidth / 2 + 64; } }
        int STATUS_WIDTH { get { return Graphics.ScreenWidth - STATUS_X; } }
#else
        int STATUS_X { get { return 256; } }
        int STATUS_WIDTH { get { return 960 - STATUS_X * 2; } }
#endif
        int STATUS_Y { get { return Graphics.ScreenHeight - 24 * 4 - 4; } }

        //------------------------------------------------------------------------------
        /**
         *	プレイヤー情報を描画
         */
        private void drawPlayerInfo(List<BattlePlayerData> playerData)
        {
            // 枠を書く
            if (owner.battleEvents.battleUiVisibility)
            {
                statusWindow.Draw(new Vector2(STATUS_X - statusWindow.paddingLeft, STATUS_Y - statusWindow.paddingTop),
                new Vector2(STATUS_WIDTH + statusWindow.paddingLeft + statusWindow.paddingRight,
                544 - STATUS_Y + statusWindow.paddingTop + statusWindow.paddingBottom));
            }

            // プレイヤー側のパラメータを表示
            int count = 0;
            foreach (var player in playerData)
            {
                calcEffectPos(player, count);

                if (owner.battleEvents.battleUiVisibility)
                {
                    drawStatus(player, count);
                }

                count++;
            }
        }

        private void drawStatus(BattlePlayerData player, int count)
        {
            // プレイヤー側のパラメータを表示
            battleStatusWindowDrawer.PositiveEnhanceEffect = player.positiveEffectDrawers.ElementAtOrDefault(player.positiveEffectIndex);
            battleStatusWindowDrawer.NegativeEnhanceEffect = player.negativeEffectDrawers.ElementAtOrDefault(player.negativeEffectIndex);
            battleStatusWindowDrawer.StatusAilmentEffect = player.statusEffectDrawers.ElementAtOrDefault(player.statusEffectIndex);
            
            drawPlayerPanel(player, count);

            // 状態エフェクトを描画
            if (owner.battleState >= BattleSequenceManager.BattleState.PlayerTurnStart &&
                owner.battleState <= BattleSequenceManager.BattleState.SetEnemyBattleCommand)
            {
                var effects = new List<EffectDrawer>();

                effects.Add(player.positiveEffectDrawers.ElementAtOrDefault(player.positiveEffectIndex));
                effects.Add(player.negativeEffectDrawers.ElementAtOrDefault(player.negativeEffectIndex));
                effects.Add(player.statusEffectDrawers.ElementAtOrDefault(player.statusEffectIndex));

                effects.RemoveAll(x => x == null);

                Vector2 effectGraphicsSize = new Vector2(48, 48);
                Vector2 effectDrawPosition = friends[count].getScreenPos(p, v);
                effectDrawPosition.X -= (effects.Count - 1) * 0.5f * effectGraphicsSize.X;
                effectDrawPosition.Y -= effectGraphicsSize.Y;

                // エフェクトがメッセージウィンドウと重ならないように位置を調整
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
        }

        private void calcEffectPos(BattlePlayerData player, int count)
        {
            // エフェクトの位置を確定する
            var color = Color.White;
            EffectDrawer effect = null;

            if (friends[count].isBillboardDead() && friends[count].getActorState() == BattleActor.ActorStateType.KO)
                color = new Color(64, 64, 64, 127); // ビルボードで死んでる時は特殊な色を適用する

            if (displayWindow == WindowType.CommandTargetPlayerListWindow && player.IsSelect)
            {
                // ターゲットとして選択されている時は点滅させる
                color = blinker.getColor();
            }
            else if (effectDrawTargetMonsterList.Contains(player))
            {
                effect = monsterEffectDrawer;
            }
            else if (effectDrawTargetPlayerList.Contains(player))
            {
                effect = playerEffectDrawer;
            }
            else if (friends[count].overRidedColor != null)
            {
                color = friends[count].overRidedColor.Value;
                friends[count].overRidedColor = null;
            }

            if (effect != null)
            {
                // エフェクトを受けてる時はエフェクトターゲット色を適用
                color = effect.getNowTargetColor();

                // エフェクトの位置を確定
                if (effect.rom != null)
                {
                    player.EffectPosition = friends[count].getScreenPos(p, v,
                        effect.rom.origPos == Rom.Effect.OrigPos.ORIG_FOOT ? MapScene.EffectPosType.Ground : MapScene.EffectPosType.Body);
                }
            }

            friends[count].setColor(color);
        }

        //------------------------------------------------------------------------------
        /**
         *	プレイヤーパネルを描画
         */
        private void drawPlayerPanel(BattlePlayerData player, int index)
        {
            // その人のターンだった時は背景を点滅させる
            var blinkColor = blinker.getColor();
            if (player.statusWindowState == StatusWindowState.Active)
            {
                Graphics.DrawFillRect(STATUS_X, STATUS_Y + 24 * index, STATUS_WIDTH / 2, 24,
                    (byte)(blinkColor.A >> 2), (byte)(blinkColor.A >> 2), (byte)(blinkColor.A >> 1), (byte)(blinkColor.A >> 3));
            }

            // HP、MPのゲージを描画
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
            hpPos = new Vector2(STATUS_X + STATUS_WIDTH / 2, STATUS_Y + 24 * index - 2);
            mpPos = new Vector2(STATUS_X + STATUS_WIDTH * 3 / 4, STATUS_Y + 24 * index - 2);
            textDrawer.DrawString(player.Name, new Vector2(STATUS_X, STATUS_Y + 24 * index), Color.White, 0.75f);
            textDrawer.DrawString("" + player.battleStatusData.HitPoint, hpPos, Color.White, 0.75f);
            textDrawer.DrawString("" + player.battleStatusData.MagicPoint, mpPos, Color.White, 0.75f);

            // 選択されている時は矢印を出す
            if (player.IsSelect)
            {
                textDrawer.DrawString("▶", new Vector2(STATUS_X - 20, STATUS_Y + 24 * index), Color.White, 0.75f);
            }
        }

        internal BattleActor AddEnemyMember(BattleEnemyData data, int index, bool useLayout)
        {
            // 1スタートになっている
            index--;

            // emptyじゃなかったら先に解放する
            if (enemies[index] != null)
            {
                enemies[index].Release();
                enemies[index] = null;
            }

            // totalを数える
            int total = 0;
            foreach(var enm in enemies)
            {
                if (enm != null)
                    total++;
            }

            var actor = BattleActor.GenerateEnemy(catalog, data.image, index, total + 1);
            actor.source = data;
            enemies[index] = actor;
            data.actionHandler = executeAction;

            if (useLayout)
            {
                actor.mapChr.setPosition(data.pos.X + BattleViewer3D.CenterOfField.X,
                    data.pos.Y + BattleViewer3D.CenterOfField.Y);
                actor.mapChr.y = 0;
            }

            return actor;
        }

        internal BattleActor AddPartyMember(BattlePlayerData data)
        {
            int empty = 0;
            while (friends.Length > empty && friends[empty] != null)
                empty++;
            if (friends.Length <= empty)
                return null;
            var actor = BattleActor.GenerateFriend(catalog, data.player, empty, empty + 1);
            actor.source = data;
            friends[empty] = actor;
            data.actionHandler = executeAction;

            return actor;
        }

        //------------------------------------------------------------------------------
        /**
         *	ダメージテキストを描画
         */
        private void drawDamageText(List<BattlePlayerData> playerData, List<BattleEnemyData> enemyMonsterData)
        {
            if (damageTextList != null && damageTextList.Count() > 0)
            {
                var removeList = new List<BattleDamageTextInfo>();

                foreach (var info in damageTextList.Where(info => info.textInfo != null).GroupBy(info => info.targetCharacter).Select(group => group.FirstOrDefault()).Where(info => info != null))
                {
                    Color color = Color.White;
                    if(info.type < BattleDamageTextInfo.TextType.Miss)
                        color = catalog.getGameSettings().damageNumColors[(int)info.type];

                    //switch (info.type)
                    //{
                    //    case BattleDamageTextInfo.TextType.HitPointDamage: color = Color.White; break;
                    //    case BattleDamageTextInfo.TextType.MagicPointDamage: color = Color.Orchid; break;
                    //    case BattleDamageTextInfo.TextType.CriticalDamage: color = Color.Red; break;
                    //    case BattleDamageTextInfo.TextType.HitPointHeal: color = new Color(191, 191, 255); break;
                    //    case BattleDamageTextInfo.TextType.MagicPointHeal: color = Color.LightGreen; break;
                    //}

                    var actor = searchFromActors(info.targetCharacter);
                    if (actor == null)
                    {
                        removeList.Add(info);
                        continue;
                    }

                    Vector2 basePosition = actor.getScreenPos(p, v, MapScene.EffectPosType.Body);
                    float characterMarginX = Graphics.GetDivWidth(damageNumberImageId) * 0.8f;

                    // アニメーション用タイマーの更新
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
                            textDrawer.DrawString(characterInfo.c.ToString(), position, color);
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

        internal override void Update(List<BattlePlayerData> playerData, List<BattleEnemyData> enemyMonsterData)
        {
            base.Update(playerData, enemyMonsterData);

            if (drawer.mapRom == null)
                return;

            camera.update();
            drawer.Update(GameMain.getElapsedTime());

            foreach (var entry in turnChr)
            {
                entry.Update(drawer, camera.Now.yAngle, false);
            }
            skillEffect.Update(drawer, camera.Now.yAngle, false);
            foreach (var mapChr in friends)
            {
                if (mapChr == null)
                    continue;

                mapChr.Update(drawer, camera.Now.yAngle, false);
            }
            foreach (var mapChr in extras)
            {
                mapChr.Update(drawer, camera.Now.yAngle, false);
            }
            foreach (var mapChr in enemies)
            {
                if (mapChr == null)
                    continue;

                mapChr.Update(drawer, camera.Now.yAngle, false);
            }

            // バトルステートに応じてカメラを動かす
            if (game.data.system.BattleCameraEnabled[Common.GameData.SystemData.BATTLE_CAMERA_SITUATION_RESULT] &&
                owner.battleState == BattleSequenceManager.BattleState.Result &&
                oldState != BattleSequenceManager.BattleState.Result)
            {
                camera.set(RESULT_START_CAMERA, 0);
                camera.push(RESULT_END_CAMERA, btc.resultTime, btc.resultTween);
            }

            // 状態に応じて各キャラに演技させる
            for (int i = 0; i < playerData.Count; i++)
            {
                // ステータスを書く位置を更新する
                playerData[i].statusWindowDrawPosition = friends[i].getScreenPos(p, v, MapScene.EffectPosType.Head);
                var neutralPos = friends[i].getPosWithoutOffset();
                //var neutralPos = BattleCharacterPosition.getPosition(CenterOfField, BattleCharacterPosition.PosType.FRIEND, i, playerData.Count);
                if (playerData[i].isMovableToForward() &&
                    (friends[i].getActorState() < BattleActor.ActorStateType.START_COMMAND_SELECT ||
                    friends[i].getActorState() > BattleActor.ActorStateType.BACK_TO_WAIT))
                    neutralPos.z -= friends[i].frontDir;
                //neutralPos.X = (int)neutralPos.X;
                //neutralPos.Y = (int)neutralPos.Y;
                turnChr[i].setPosition(neutralPos.x - 0.5f, neutralPos.z - 0.5f);

                // 逃げる
                if (owner.battleState == BattleSequenceManager.BattleState.PlayerEscapeSuccess)
                {
                    friends[i].queueActorState(BattleActor.ActorStateType.ESCAPE);
                    continue;
                }

                // 勝利
                if (owner.battleState == BattleSequenceManager.BattleState.Result)
                {
                    friends[i].queueActorState(BattleActor.ActorStateType.WIN);
                    continue;
                }

                // 死亡・眠り・麻痺
                if (playerData[i].Status.HasFlag(StatusAilments.DOWN) ||
                    playerData[i].Status.HasFlag(StatusAilments.PARALYSIS) ||
                    playerData[i].Status.HasFlag(StatusAilments.SLEEP))
                {
                    friends[i].queueActorState(BattleActor.ActorStateType.KO);
                    continue;
                }
                else if (friends[i].getActorState() == BattleActor.ActorStateType.KO)
                {
                    friends[i].queueActorState(BattleActor.ActorStateType.WAIT);
                    continue;
                }

                // 自分のターンだったらその情報を表示
                if (owner.battleState >= BattleSequenceManager.BattleState.SelectPlayerBattleCommand &&
                    owner.battleState <= BattleSequenceManager.BattleState.SetEnemyBattleCommand)
                {
                    if (playerData[i] == owner.commandSelectPlayer)
                    {
                        if ((turnChr[i].hide & MapCharacter.HideCauses.BY_BATTLE) != MapCharacter.HideCauses.NONE)
                        {
                            turnChr[i].forceRestartParticle();
                        }
                        turnChr[i].hide &= ~MapCharacter.HideCauses.BY_BATTLE;
                        if (!friends[i].isActorStateQueued(BattleActor.ActorStateType.START_COMMAND_SELECT) &&
                            friends[i].getActorState() != BattleActor.ActorStateType.START_COMMAND_SELECT &&
                            friends[i].getActorState() != BattleActor.ActorStateType.COMMAND_SELECT)
                        {
                            friends[i].queueActorState(BattleActor.ActorStateType.START_COMMAND_SELECT);
                            friends[i].queueActorState(BattleActor.ActorStateType.COMMAND_SELECT);
                            continue;
                        }
                    }
                    else
                    {
                        turnChr[i].hide |= MapCharacter.HideCauses.BY_BATTLE;
                        if (!friends[i].isActorStateQueued(BattleActor.ActorStateType.BACK_TO_WAIT) &&
                            (friends[i].getActorState() == BattleActor.ActorStateType.START_COMMAND_SELECT ||
                            friends[i].getActorState() == BattleActor.ActorStateType.COMMAND_SELECT))
                        {
                            friends[i].queueActorState(BattleActor.ActorStateType.BACK_TO_WAIT);
                            friends[i].queueActorState(BattleActor.ActorStateType.WAIT);
                            continue;
                        }
                    }
                }
                else
                {
                    turnChr[i].hide |= MapCharacter.HideCauses.BY_BATTLE;
                }
            }

            // 状態に応じてモンスターにも演技させる
            for (int i = 0; i < enemyMonsterData.Count; i++)
            {
                var actor = searchFromActors(enemyMonsterData[i]);

                // 死亡・眠り・麻痺
                if (enemyMonsterData[i].Status.HasFlag(StatusAilments.DOWN) ||
                    enemyMonsterData[i].Status.HasFlag(StatusAilments.PARALYSIS) ||
                    enemyMonsterData[i].Status.HasFlag(StatusAilments.SLEEP))
                {
                    actor.queueActorState(BattleActor.ActorStateType.KO);
                    continue;
                }
                else if (actor.getActorState() == BattleActor.ActorStateType.KO)
                {
                    actor.queueActorState(BattleActor.ActorStateType.WAIT);
                    continue;
                }
            }

            oldState = owner.battleState;
        }

        //------------------------------------------------------------------------------
        /**
         *	アクションの実行
         */
        // TODO モーション時間に比例するようにする
        const int ATTACK_PRE_MOTIONTIME = 20;   // 予備動作
        const int ATTACK_MOTIONTIME = 40;      // 攻撃動作

        private void executeAction(BattleCharacterBase self, bool action, bool start)
        {
            var actor = searchFromActors(self);
            
            // パーティから外すなどで既にアクターがいない場合がある
            if (actor == null)
                return;

            if (action)
            {
                switch (self.selectedBattleCommandType)
                {
                    // 攻撃
                    case BattleCommandType.Attack:
                    case BattleCommandType.Critical:
                        if (start)
                        {
                            string motion = null;
                            if (self is BattlePlayerData)
                            {
                                var pl = self as BattlePlayerData;
                                if (pl.player.equipments[0] != null)
                                    motion = pl.player.equipments[0].weapon.motion;
                            }
                            if (game.data.system.BattleCameraEnabled[Common.GameData.SystemData.BATTLE_CAMERA_SITUATION_ATTACK])
                            {
                                actor.queueActorState(BattleActor.ActorStateType.ATTACK_WAIT, "", btc.attackTime - ATTACK_PRE_MOTIONTIME);
                            }
                            actor.queueActorState(BattleActor.ActorStateType.ATTACK, motion, ATTACK_MOTIONTIME);
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
                            float yAngle = (float)(Math.Atan2(selfPos.x - targetPos.x, selfPos.z - targetPos.z) / Math.PI * 180) + ATTACK_END_CAMERA.yAngle;
                            if (game.data.system.BattleCameraEnabled[Common.GameData.SystemData.BATTLE_CAMERA_SITUATION_ATTACK])
                            {
                                camera.set(ATTACK_START_CAMERA, 0);
                                camera.push(ATTACK_END_CAMERA, btc.attackTime, center, yAngle, btc.attackTween, BattleCameraController.TAG_FORCE_WAIT);
                                camera.push(ATTACK_END_CAMERA, ATTACK_MOTIONTIME, center, yAngle, btc.attackTween);
                            }
                        }
                        else
                        {
                            actor.queueActorState(BattleActor.ActorStateType.ATTACK_END);
                            if (game.data.system.BattleCameraEnabled[Common.GameData.SystemData.BATTLE_CAMERA_SITUATION_ATTACK])
                            {
                                camera.push(NORMAL_CAMERA, btc.attackTime, btc.startTween, BattleCameraController.TAG_FORCE_WAIT);
                            }
                        }
                        break;

                    // スキル
                    case BattleCommandType.Skill:
                    case BattleCommandType.SameSkillEffect:
                        if (start)
                        {
                            actor.queueActorState(BattleActor.ActorStateType.SKILL, self.selectedSkill.option.motion, 0);
                            if (actor.frontDir > 0)
                            {
                                if (game.data.system.BattleCameraEnabled[Common.GameData.SystemData.BATTLE_CAMERA_SITUATION_SKILL] && btc.skillTime > 0)
                                {
                                    camera.set(MAGIC_START_CAMERA, 0, actor);
                                    camera.push(MAGIC_USER_CAMERA, btc.skillTime, actor, btc.skillTween, BattleCameraController.TAG_FORCE_WAIT);
                                    camera.push(MAGIC_USER_CAMERA, btc.skillTime / 2, actor, Common.Rom.GameSettings.BattleCamera.TweenType.LINEAR,
                                        BattleCameraController.TAG_FORCE_WAIT);
                                    camera.push(NORMAL_CAMERA, 0);
                                }

                                Audio.PlaySound(game.se.skill);
                                skillEffect.hide &= ~MapCharacter.HideCauses.BY_BATTLE;

                                var list = friends.Where(x => x != null).ToList();
                                //var neutralPos = BattleCharacterPosition.getPosition(CenterOfField,
                                //    BattleCharacterPosition.PosType.FRIEND, list.IndexOf(actor), list.Count);
                                //neutralPos.Y -= actor.frontDir;
                                //neutralPos.X = (int)neutralPos.X;
                                //neutralPos.Y = (int)neutralPos.Y;
                                //skillEffect.setPosition(neutralPos.X, neutralPos.Y);

                                var neutralPos = actor.getPosWithoutOffset();
                                var state = actor.getActorState();
                                if (self.isMovableToForward() &&
                                    (state < BattleActor.ActorStateType.SKILL ||
                                    state > BattleActor.ActorStateType.SKILL_END))
                                    neutralPos.z -= actor.frontDir;
                                skillEffect.setPosition(neutralPos.x - 0.5f, neutralPos.z - 0.5f);
                                skillEffect.forceRestartParticle();
                            }
                        }
                        else
                        {
                            searchFromActors(self).queueActorState(BattleActor.ActorStateType.SKILL_END);
                            skillEffect.hide |= MapCharacter.HideCauses.BY_BATTLE;
                        }
                        break;

                    // アイテム
                    case BattleCommandType.Item:
                        if (start)
                        {
                            actor.queueActorState(BattleActor.ActorStateType.ITEM);
                            if (actor.frontDir > 0)
                            {
                                if (game.data.system.BattleCameraEnabled[Common.GameData.SystemData.BATTLE_CAMERA_SITUATION_ITEM] && btc.itemTime > 0)
                                {
                                    camera.set(ITEM_START_CAMERA, 0, actor);
                                    camera.push(ITEM_USER_CAMERA, btc.itemTime, actor, btc.itemTween, BattleCameraController.TAG_FORCE_WAIT);
                                    camera.push(ITEM_USER_CAMERA, btc.itemTime / 2, actor, Common.Rom.GameSettings.BattleCamera.TweenType.LINEAR,
                                        BattleCameraController.TAG_FORCE_WAIT);
                                    camera.push(NORMAL_CAMERA, 0);
                                }
                                Audio.PlaySound(game.se.item);
                            }
                        }
                        else
                        {
                            searchFromActors(self).queueActorState(BattleActor.ActorStateType.ITEM_END);
                        }
                        break;

                    // ガード
                    case BattleCommandType.Guard:
                        if (start)
                            actor.queueActorState(BattleActor.ActorStateType.GUARD);
                        break;

                    // ためる
                    case BattleCommandType.Charge:
                        if (start)
                            actor.queueActorState(BattleActor.ActorStateType.CHARGE);
                        break;

                    // 逃げる
                    case BattleCommandType.PlayerEscape:
                        if (!start)
                            actor.queueActorState(BattleActor.ActorStateType.ESCAPE_FAILED);
                        break;

                    // 敵が逃げる
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
                    case ReactionType.Damage:
                        if (start)
                            actor.queueActorState(BattleActor.ActorStateType.DAMAGE);
                        else
                            if (!(self.Status.HasFlag(StatusAilments.DOWN) ||
                                self.Status.HasFlag(StatusAilments.PARALYSIS) ||
                                self.Status.HasFlag(StatusAilments.SLEEP)))
                        {
                            actor.queueActorState(BattleActor.ActorStateType.WAIT);
                        }
                        break;
                }
            }
        }
        
        internal bool isCurrentActorReady()
        {
            // Attackが予備動作まで済んでいないキャラクターがいるか調べる
            foreach(var actor in friends)
            {
                if (actor == null)
                    continue;

                if (actor.getActorState() == BattleActor.ActorStateType.ATTACK && actor.getActorStateCount() < ATTACK_PRE_MOTIONTIME)
                    return false;
            }

            foreach (var actor in enemies)
            {
                if (actor == null)
                    continue;

                if (actor.getActorState() == BattleActor.ActorStateType.ATTACK && actor.getActorStateCount() < ATTACK_PRE_MOTIONTIME)
                    return false;
            }

            return true;
        }

        //------------------------------------------------------------------------------
        /**
         *	描画処理
         */
        public void draw(SharpKmyGfx.Render scn)
        {
            if (drawer.mapRom == null)
                return;

#if ENABLE_VR
            bool bDraw = true;

            if (SharpKmyVr.Func.IsReady())
            {
                bool bSceneVrL = SharpKmyGfx.Render.isSameScene(scn, SharpKmyGfx.Render.getRenderL());
                bool bSceneVrR = SharpKmyGfx.Render.isSameScene(scn, SharpKmyGfx.Render.getRenderR());

                if (bSceneVrL || bSceneVrR)
                {
                    // ◆VR時（左目/右目）

                    SharpKmyVr.EyeType eyeType = (bSceneVrL) ? SharpKmyVr.EyeType.Left : SharpKmyVr.EyeType.Right;
                    SharpKmyMath.Matrix4 mtxView;
                    SharpKmyMath.Matrix4 mtxProj;

                    mtxView = SharpKmyVr.Func.GetViewMatrix(eyeType, m_VrCameraData.m_UpVec, m_VrCameraData.m_CameraPos, m_VrCameraData.GetCombinedOffsetRotateMatrix());
                    mtxProj = SharpKmyVr.Func.GetProjectionMatrix(eyeType);

                    scn.setViewMatrix(mtxProj, mtxView);
                    drawer.afterViewPositionFixProc(scn);
                    drawer.draw(scn);

                    // 2D表示用板ポリ
                    {
                        SharpKmyMath.Vector3 posCam = new SharpKmyMath.Vector3();
                        SharpKmyMath.Vector3 hmdPos = SharpKmyVr.Func.GetHmdPosePos() * m_VrCameraData.GetHakoniwaScale();
                        SharpKmyMath.Matrix4 mtxTmp1 = SharpKmyMath.Matrix4.translate(hmdPos.x, hmdPos.y, hmdPos.z);
                        posCam = (m_VrCameraData.GetCombinedOffsetRotateMatrix() * mtxTmp1).translation();
                        posCam += m_VrCameraData.GetCombinedOffsetPos();

                        VrDrawer.DrawVr2dPolygon(scn, posCam, owner.GetFadeScreenColor(), false, 1, m_VrCameraData);
                    }

                    bDraw = false;
                }
            }

            if (bDraw)
#endif //#if ENABLE_VR
            {
                // ◆通常時

                var tmp = MapData.pickupscene;
                MapData.pickupscene = null;

                var asp = game.getScreenAspect();
                createCameraMatrix(out p, out v, camera.Now, asp);
#if WINDOWS
#else
                if(shakeValue.x == 0 && shakeValue.y == 0)
                    scn.resetCameraMatrix();
#endif
                scn.setViewMatrix(p, v);
                drawer.afterViewPositionFixProc(scn);
                if (drawer.sky != null)
                {
                    drawer.sky.setProj(SharpKmyMath.Matrix4.perspectiveFOV(
                        Microsoft.Xna.Framework.MathHelper.ToRadians(camera.Now.fov), asp, 1, 1000));
                }

                drawer.draw(scn);

                MapData.pickupscene = tmp;
                tmp = null;
            }
        }

        //------------------------------------------------------------------------------
        /**
         *	カメラ行列の生成
         */
        internal void createCameraMatrix(out SharpKmyMath.Matrix4 p, out SharpKmyMath.Matrix4 v, Rom.ThirdPersonCameraSettings camera, float asp)
        {
            var c = camera;
            var nearclip = c.distance - 50 > 0 ? c.distance - 50 : 1;
            var farclip = c.distance * 3 > 100 ? c.distance * 3 : 100;
            SharpKmyMath.Vector3 vecUp = new SharpKmyMath.Vector3(0, 1, 0);

            var campos = (SharpKmyMath.Matrix4.rotateY(Microsoft.Xna.Framework.MathHelper.ToRadians(c.yAngle)) *
                SharpKmyMath.Matrix4.rotateX(Microsoft.Xna.Framework.MathHelper.ToRadians(c.xAngle)) *
                (new SharpKmyMath.Vector3(0, 0, c.distance))).getXYZ();

            var target = new SharpKmyMath.Vector3(c.x, c.height, c.y);

#if WINDOWS
#else
            // イベント等のカメラワークが左右反転しているのを修正
            campos.x *= -1;
            // 地形の高さの変位とカメラの高さの変位が±逆になっていたのを修正
            target.y *= -1;
#endif

            var lookat = target + campos;

#if ENABLE_VR

            if (SharpKmyVr.Func.IsReady())
            {
                SharpKmyMath.Matrix4 mtxUp = m_VrCameraData.GetCombinedOffsetRotateMatrix() * SharpKmyVr.Func.GetHmdPoseRotateMatrix() * SharpKmyMath.Matrix4.translate(0, 1, 0);
                vecUp.x = mtxUp.m30;
                vecUp.y = mtxUp.m31;
                vecUp.z = mtxUp.m32;
                vecUp = SharpKmyMath.Vector3.normalize(vecUp);

                SharpKmyMath.Vector3 hmdPos = SharpKmyVr.Func.GetHmdPosePos() * m_VrCameraData.GetHakoniwaScale();
                SharpKmyMath.Matrix4 mtxTmp1 = SharpKmyMath.Matrix4.translate(hmdPos.x, hmdPos.y, hmdPos.z);
                campos = (m_VrCameraData.GetCombinedOffsetRotateMatrix() * mtxTmp1).translation();
                campos += m_VrCameraData.GetCombinedOffsetPos();

                SharpKmyMath.Vector3 dirTmp = SharpKmyVr.Func.GetHmdPoseDirection();
                SharpKmyMath.Matrix4 mtxTmp = m_VrCameraData.GetCombinedOffsetRotateMatrix() * SharpKmyMath.Matrix4.translate(dirTmp.x, dirTmp.y, dirTmp.z);

                lookat = campos;
                target = campos + mtxTmp.translation();

                // 行列計算
                {
                    const float fovy = 25.0f;

                    v = SharpKmyMath.Matrix4.lookat(lookat, target, vecUp);
                    v = SharpKmyMath.Matrix4.inverse(v);
                    p = SharpKmyMath.Matrix4.translate(shakeValue.x, shakeValue.y, 0) *
                        SharpKmyMath.Matrix4.perspectiveFOV(MathHelper.ToRadians(fovy), asp, nearclip, farclip);
                    if (drawer.sky != null)
                        drawer.sky.setProj(SharpKmyMath.Matrix4.perspectiveFOV(MathHelper.ToRadians(fovy), asp, 1, 1000));
                }
            }
            else
            {
                v = SharpKmyMath.Matrix4.lookat(lookat, target, vecUp);
                v = SharpKmyMath.Matrix4.inverse(v);
                p = SharpKmyMath.Matrix4.translate(shakeValue.x, shakeValue.y, 0) * 
                    SharpKmyMath.Matrix4.perspectiveFOV(MathHelper.ToRadians(c.fov), asp, nearclip, farclip);
            }

            // カメラ情報を保存
            m_VrCameraData.m_CameraPos = campos;
            m_VrCameraData.m_UpVec = vecUp;

#else   // #if ENABLE_VR

            v = SharpKmyMath.Matrix4.lookat(lookat, target, vecUp);
            v = SharpKmyMath.Matrix4.inverse(v);
            p = SharpKmyMath.Matrix4.perspectiveFOV(Microsoft.Xna.Framework.MathHelper.ToRadians(c.fov), asp, nearclip, farclip);

#endif  // #if ENABLE_VR
        }

        //------------------------------------------------------------------------------
        /**
         *	キャラクタを描画
         */
        private void drawCharacters(SharpKmyGfx.Render scn, bool isShadowScene)
        {
            foreach (var entry in turnChr)
            {
                entry.draw(scn);
            }
            skillEffect.draw(scn);
            foreach (var mapChr in friends)
            {
                if (mapChr == null)
                    continue;

                mapChr.Draw(scn, isShadowScene);
            }
            foreach (var mapChr in extras)
            {
                mapChr.draw(scn);
            }
            foreach (var mapChr in enemies)
            {
                if (mapChr == null)
                    continue;

                mapChr.Draw(scn, isShadowScene);
            }
        }

        private System.Collections.IEnumerator setMapRom(Common.Rom.Map map)
        {
            if (map != null)
            {
                var coroutine = drawer.setRom(map);
                while (coroutine.MoveNext()) yield return null;

                drawer.setShadowCenter(new SharpKmyMath.Vector3(
                    CenterOfField.X, 1,
                    CenterOfField.Y));

                camera = new BattleCameraController(INITIAL_CAMERA);
                camera.setMapCenterHeight(drawer.getAdjustedHeight(
                    (int)CenterOfField.X,
                    (int)CenterOfField.Y) + 1f);

                // イベントをマップ上に表示する
                foreach (var chr in extras)
                {
                    chr.Reset();
                }
                extras.Clear();

                // デフォルトのバトルフィールドの場合、イベントは読み込まない
                if (map.isReadOnly())
                    yield break;

                // バトルイベントに置いてあるイベントを表示する
                foreach (var evRef in map.getEvents())
                {
                    var ev = catalog.getItemFromGuid(evRef.guId) as Common.Rom.Event;
                    if (ev != null)
                    {
                        var chr = new MapCharacter(ev);
                        chr.setDirection(ev.Direction);
                        chr.setPosition(evRef.x, evRef.y);
                        //chr.ChangeGraphic(catalog.getItemFromGuid(ev.Graphic) as Common.Resource.ResourceItem, drawer);
                        //chr.playMotion(ev.Motion, 0);
                        extras.Add(chr);
                    }
                }
            }
        }

        internal System.Collections.IEnumerator prepare()
        {
            var mapScene = game.mapScene;
            catalog = mapScene.owner.catalog;

            return prepare(mapScene.map.getBattleBg(catalog, mapScene.battleSetting));
        }

        internal System.Collections.IEnumerator prepare(Guid battleBg)
        {
            UnityUtil.changeScene(UnityUtil.SceneType.BATTLE, true);

            var mapScene = game.mapScene;
            catalog = mapScene.owner.catalog;

            reset();

            if ((mapScene.battleSetting.battleBgCenterX > 0) || (mapScene.battleSetting.battleBgCenterY > 0))
            {
                CenterOfField = new Vector2(mapScene.battleSetting.battleBgCenterX, mapScene.battleSetting.battleBgCenterY);
            }
            else
            {
                CenterOfField = new Vector2(mapScene.map.Width / 2, mapScene.map.Height / 2);
            }
            initCameraParams();
            var coroutine = SetBackGround(battleBg);
            while (coroutine.MoveNext()) yield return null;

            // 味方キャラを読み込んでおく
            int count = 0;
            int max = mapScene.owner.data.party.members.Count;
            foreach (var chr in mapScene.owner.data.party.members)
            {
                BattleActor.party = game.data.party;
                friends[count] = BattleActor.GenerateFriend(catalog, chr, count, max);
                count++;
            }
        }

        internal void SetBackGroundCenter(int battleBgCenterX, int battleBgCenterY)
        {
            if (!drawer.mapRom.isReadOnly())
            {
                if (battleBgCenterX > 0)
                {
                    CenterOfField = new Vector2(battleBgCenterX, battleBgCenterY);
                }
                else
                {
                    CenterOfField = new Vector2(drawer.mapRom.Width / 2, drawer.mapRom.Height / 2);
                }
                camera.setMapCenterHeight(drawer.getAdjustedHeight(
                    (int)CenterOfField.X,
                    (int)CenterOfField.Y) + 1f);
                initCameraParams();
            }
        }

        // マップ範囲外をバトル背景に指定していた場合に補正する処理
        private void limitCenterOfField(Common.Rom.Map map)
        {
            if (map == null)
                return;

            CenterOfField.X = calcLimit((int)CenterOfField.X,
                (int)BattleCharacterPosition.DEFAULT_BATTLE_FIELD_SIZE.X, map.Width);
            CenterOfField.Y = calcLimit((int)CenterOfField.Y,
                (int)BattleCharacterPosition.DEFAULT_BATTLE_FIELD_SIZE.Y, map.Height);
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

        private void prepareFriends(List<BattlePlayerData> playerData)
        {
            // 味方キャラを読み込む
            int count = -1;
            int max = playerData.Count;
            foreach (var chr in playerData)
            {
                chr.actionHandler = executeAction;
                count++;
                if (friends[count] != null)
                {
                    friends[count].source = chr;
                    if (friends[count].sourceEqual(game.data.party.getMemberGraphic(chr.player.rom)))
                    {
                        friends[count].resetState(true, count, max);
                        friends[count].mapChr.y = 0;
                        BattleActor.createWeaponModel(ref friends[count], catalog);
                        continue;
                    }
                    else
                    {
                        friends[count].Release();
                    }
                }
                BattleActor.party = game.data.party;
                friends[count] = BattleActor.GenerateFriend(catalog, chr.player, count, max);
                friends[count].source = chr;
            }

            // キャラを破棄
            count++;
            for (int i = count; i < friends.Length; i++)
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
            int count = 0;
            int max = enemyMonsterData.Count;
            foreach (var chr in enemyMonsterData)
            {
                chr.actionHandler = executeAction;
                if (enemies[count] != null)
                    enemies[count].Release();
                enemies[count] = BattleActor.GenerateEnemy(catalog, chr.image, count, max);
                if (chr.arrangmentType != BattleEnemyData.MonsterArrangementType.Manual)
                {
                    chr.arrangmentType = BattleCharacterPosition.getPositionType(count, max);
                }
                else
                {
                    enemies[count].mapChr.setPosition(chr.pos.X + BattleViewer3D.CenterOfField.X,
                        chr.pos.Y + BattleViewer3D.CenterOfField.Y);
                    enemies[count].mapChr.y = 0;
                }
                enemies[count].source = chr;
                count++;
            }

            // キャラを破棄
            for (int i = count; i < enemies.Length; i++)
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
            if (drawer != null)
            {
                drawer.Reset();
                drawer.mapRom = null;
            }

            // キャラを破棄
            for (int i = 0; i < friends.Length; i++)
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
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null)
                {
                    enemies[i].Release();
                    enemies[i] = null;
                }
            }
        }

        internal string getCurrentCameraTag()
        {
            return camera.CurrentTag;
        }

#if ENABLE_VR

        //------------------------------------------------------------------------------
        /**
         *	VRカメラデータをセットアップ
         */
        public override void SetupVrCameraData(bool bUpdateInfo)
        {
            // パラメータセットアップ
            m_VrCameraData.SetupParam(VrCameraData.ParamType.RotateY, 7, 8, true, 0.0f, 0.0f);
            m_VrCameraData.SetupParam(VrCameraData.ParamType.Distance, 0, 4, false, 7.5f, 1.5f);
            m_VrCameraData.SetupParam(VrCameraData.ParamType.Height, 1, 4, false, 1.0f, 1.5f);

            // 箱庭視点時のスケール
            m_VrCameraData.SetHakoniwaScale(1.0f);

            // 情報更新
            if (bUpdateInfo)
            {
                m_VrCameraData.UpdateInfo(Common.Rom.Map.CameraControlMode.UNKNOWN);
            }
        }

        //------------------------------------------------------------------------------
        /**
         *	VRカメラデータを取得
         */
        public override VrCameraData GetVrCameraData()
        {
            return m_VrCameraData;
        }

#endif  // #if ENABLE_VR
    }
}

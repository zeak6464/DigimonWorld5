#define ENABLE_TEST
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Yukar.Common;
using Yukar.Common.Rom;
using System.Linq;
using Yukar.Engine;
using static Yukar.Engine.BattleEnum;

namespace Yukar.Battle
{
    /// <summary>
    /// バトルイベント管理クラス
    /// Battle event management class
    /// </summary>
    public class BattleEventController : BattleEventControllerBase
    {
        Queue<MemberChangeData> memberChangeQueue = new Queue<MemberChangeData>();

        private List<MapCharacter> dummyChrs = new List<MapCharacter>();
        private List<MapCharacter> extras = new List<MapCharacter>();
        private Catalog catalog;
        private BattleSequenceManager battle;

        public bool battleUiVisibility = true;
        private List<ScriptRunner> mapRunnerBorrowed = new List<ScriptRunner>();

        /// <summary>
        /// メンバー交代をキューイングしておくための構造体
        /// Structure for queuing member changes
        /// </summary>
        internal class ActionData
        {
            internal Script.Command cmd;
            internal Guid evGuid;
        }
        private List<ActionData> actionQueue = new List<ActionData>();

        private BattleResultState reservedResult = BattleResultState.NonFinish;
        internal Vector3[] playerLayouts;
        private bool isBattleEndEventStarted;
        private bool isBattleForciblyTerminate;

        public override CameraManager CameraManager { get => (battle.battleViewer as BattleViewer3D)?.camManager; }

        public override SharpKmyMath.Vector2 ShakeValue
        {
            set
            {
                var viewer = battle.battleViewer as BattleViewer3D;
                if (viewer != null)
                {
                    viewer.shakeValue = value;
                }

                base.ShakeValue = value;
            }
        }

        public BattleEventController() : base()
        {
            var dummyRef = new Map.EventRef();
            hero = new MapCharacter(dummyRef);
            isBattle = true;
        }

        internal void init(BattleSequenceManager battle, Catalog catalog,
            List<BattlePlayerData> playerData, List<BattleEnemyData> enemyMonsterData,
            MapEngine mapEngine)
        {
            this.battle = battle;
            this.catalog = catalog;
            this.mapEngine = mapEngine;

            var events = new List<Event>();
            foreach (var guid in catalog.getGameSettings().battleEvents)
            {
                var ev = catalog.getItemFromGuid<Event>(guid);
                if (ev == null || !ev.IsValid())
                    continue;

                AddEvent(ev);
            }

            memberChangeQueue.Clear();
            extras = null;
            cameraControlMode = Map.CameraControlMode.NORMAL;
        }

        internal void AddEvent(Event inEvent, RomItem parentRom = null)
		{
            var newEventRef = new Map.EventRef();
            newEventRef.guId = inEvent.guId;
            newEventRef.pos.X =
            newEventRef.pos.Z = -1;

            var dummyChr = new MapCharacter(inEvent, newEventRef, this);

            dummyChr.isCommonEvent = true;

            checkAllSheet(dummyChr, true, true, false, parentRom);
            dummyChrs.Add(dummyChr);
        }

        private void initBattleFieldPlacedEvents()
        {
            if (extras != null)
                return;

            var v3d = battle.battleViewer as BattleViewer3D;

            if (v3d == null)
                return;

            // マップ配置イベントの表示状態を更新する
            // Update the display state of the map placement event
            extras = v3d.extras;
            foreach (var chr in extras)
            {
                checkAllSheet(chr, true, false, true);
            }

            // バトルフィールドと移動マップが一致している場合、グラフィック変更や削除などの状態もチェックする
            // If the battlefield and movement map match, also check the state of graphic changes, deletions, etc.
            if(v3d.mapDrawer.mapRom.guId == owner.mapScene.map.guId)
            {
                foreach (var chr in extras.ToArray())
                {
                    var original = owner.mapScene.mapCharList.FirstOrDefault(x => x.guId == chr.rom.guId);

                    // 既に消えている場合は削除
                    // Delete if already gone
                    if (original == null)
                    {
                        chr.Reset();
                        extras.Remove(chr);
                    }
                    else
                    {
                        // 向き、位置をコピー
                        // copy direction and position
                        chr.setPosition(original.getPosition());
                        chr.setRotation(original.getRotation());

                        // グラフィックが変化していれば反映
                        // Reflect if graphics change
                        var res = original.getGraphic() as Common.Resource.GfxResourceBase;
                        if(chr.getGraphic() != res)
                            chr.ChangeGraphic(res, v3d.mapDrawer);

                        // スケール、透明状態、モーションをコピー
                        // Copy scale, transparency and motion
                        chr.setScale(original.getScale());
                        chr.playMotion(original.currentMotion);
                        chr.hide = original.hide;

                        // サブグラフィックの状態もコピー
                        // Copy subgraphic state
                        chr.LoadSubGraphicState(original.SaveSubGraphicState().ToList());
                    }
                }
            }
        }

        internal void term()
        {
            foreach (var runner in runnerDic.getList())
            {
                runner.finalize();
            }
            runnerDic.Clear();

            foreach (var mapChr in dummyChrs)
            {
                mapChr.Reset();
            }
            dummyChrs.Clear();

            foreach (var runner in mapRunnerBorrowed)
            {
                runner.owner = owner.mapScene;
            }
            mapRunnerBorrowed.Clear();

            foreach (var mapChr in mapCharList)
            {
                mapChr.Reset();
            }
            mapCharList.Clear();

            releaseMenu();
            spManager.Clear();
            spManager = null;
            owner = null;
        }

        internal void update()
        {
            if (owner == null || battle == null)
                return;

            // バトルビューアからのカメラ情報をセットしておく
            // Set the camera information from the battle viewer
            var viewer = battle.battleViewer as BattleViewer3D;
            if (viewer != null)
            {
                // 前フレームのデータを反映(heroには注視点を入れておく)
                // Reflect the data of the previous frame (put the gaze point in the hero)
#if false
                var now = viewer.camera.Now;
                hero.pos = now.offset;
                xAngle = now.angle.X;
                yAngle = now.angle.Y;
                dist = now.distance;
                eyeHeight = now.eyeHeight;
                fovy = now.fov;
                nearClip = now.nearClip;
#else
                var camManager = viewer.camManager;
                Quaternion qt = camManager.camQuat;
                Vector3 rot = new Vector3();

                camManager.convQuaternionToRotation(qt, out rot);

                hero.pos = new Vector3(camManager.m_intp_target.x, camManager.m_intp_target.y, camManager.m_intp_target.z);
                //xAngle = camManager.m_view_angle.x;
                //yAngle = camManager.m_view_angle.y;
                //dist = camManager.m_distance;
                //camOffset = camManager.m_last_offset;
                //fovy = camManager.m_fovy;
                //nearClip = camManager.m_nearClip;
#endif

                mapDrawer = viewer.mapDrawer;
                map = viewer.mapDrawer.mapRom;
            }

            // バトルフィールドに置いてあるイベントの初期化
            // Initialize the event placed in the battlefield
            initBattleFieldPlacedEvents();

            // 各ウィンドウのアップデート
            // Update each window
            updateWindows();
            spManager.Update();

            // スクリプト処理直前のCSharp割り込み
            // CSharp interrupt just before script processing
            foreach (var mapChr in dummyChrs)
            {
                mapChr.BeforeUpdate();
            }

            // イベント処理
            // event handling
            var isEventProcessed = procScript();
            if (!isEventProcessed)
            {
                // シートチェンジ判定
                // Sheet change judgment
                foreach (var mapChr in dummyChrs)
                {
                    checkAllSheet(mapChr, false, true, false);
                    mapChr.UpdateOnlyScript();
                }
                if (extras != null)
                {
                    foreach (var chrs in extras)
                    {
                        checkAllSheet(chrs, false, false, true);
                    }
                }
            }

            // Face用のキャラクターを更新
            // Updated characters for Face
            foreach (var mapChr in mapCharList)
            {
                mapChr.update();
            }
            UpdateFace();
        }

        private bool procScript()
        {
            bool result = false;

            // 通常スクリプトのアップデート
            // Normal script update
            var runners = runnerDic.getList().Union(mapRunnerBorrowed).ToArray();
            foreach (var runner in runners)
            {
                if (exclusiveRunner == null || (runner == exclusiveRunner && !exclusiveInverse) ||
                    (runner != exclusiveRunner && exclusiveInverse))
                {
                    if (!runner.isParallelTriggers())
                    {
                        bool isFinished = runner.Update();
                        // 完了したスクリプトがある場合は、ページ遷移をチェックする
                        // Check page transitions if there is a completed script
                        if (isFinished)
                            break;
                        // 並列動作しないので、自動移動以外は最初に見つかったRunningしか実行しない
                        // Since it does not operate in parallel, only the first found Running other than automatic movement is executed
                        if (runner.state == ScriptRunner.ScriptState.Running)
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }

            // その他の並列スクリプトのアップデート
            // Other parallel script updates
            foreach (var runner in runners)
            {
                if (runner.isParallelTriggers())
                {
                    runner.Update();
                }
            }

            return result;
        }

        internal void Draw(SharpKmyGfx.Render scn)
        {
            foreach (var mapChr in mapCharList)
            {
                mapChr.draw(scn);
            }
        }

        public override void ShowFace(Guid inLeftId, string inLeftMotion, bool inIsLeftFlip, Guid inRightId, string inRightMotion, bool inIsRightFlip, bool inIsLeftActive, bool inUsedLighting)
        {
            owner.mapScene.ShowFace(inLeftId, inLeftMotion, inIsLeftFlip, inRightId, inRightMotion, inIsRightFlip, inIsLeftActive, inUsedLighting);
        }

        public override void HideFace()
        {
            owner.mapScene.HideFace();
        }

        internal new void Draw()
        {
            if (owner == null)
                return;

            Graphics.BeginDraw();

            // スプライト描画
            // sprite drawing
            spManager.Draw(0, SpriteManager.SYSTEM_SPRITE_INDEX);

            owner.mapScene.DrawFace();

            // エフェクト描画
            // effect drawing
            DrawEffects();

            // スクリーンカラーを適用
            // Apply Screen Color
            Graphics.DrawFillRect(0, 0, Graphics.ViewportWidth, Graphics.ViewportHeight,
                screenColor.R, screenColor.G, screenColor.B, screenColor.A);

            // スプライト描画
            // sprite drawing
            spManager.Draw(SpriteManager.SYSTEM_SPRITE_INDEX, SpriteManager.MAX_SPRITE);

            // ウィンドウ描画
            // window drawing
            drawWindows();

            // スクリプトからの描画
            // drawing from script
            foreach (var mapChr in dummyChrs)
            {
                mapChr.AfterDraw();
            }

            Graphics.EndDraw();
        }

        public override void GetCharacterScreenPos(MapCharacter chr, out int x, out int y, EffectPosType pos = EffectPosType.Ground)
        {
            var viewer = battle.battleViewer as BattleViewer3D;
            SharpKmyMath.Matrix4 p, v;
            if (viewer != null)
            {
                var asp = owner.getScreenAspect();
                viewer.createCameraMatrix(out p, out v/*, viewer.camera.Now*/, asp);
                GetCharacterScreenPos(chr, out x, out y, p, v, pos);
            }
            else
            {
                x = y = 10000;
            }
        }

        public override void SetEffectColor(MapCharacter selfChr, Color color)
        {
            var viewer = battle.battleViewer as BattleViewer3D;
            if (viewer == null)
                return;

            var actor = viewer.searchFromActors(selfChr);
            if (actor == null)
                return;

            actor.overRidedColor = color;
        }

        public void start(Script.Trigger trigger)
        {
            if (trigger == Script.Trigger.BATTLE_END)
            {
                // BTL_STOP 命令で再突入する可能性があるので処理しないようにする
                // Do not process the BTL_STOP instruction as it may re-enter
                if (isBattleEndEventStarted)
                    return;

                isBattleEndEventStarted = true;
            }

            foreach (var runner in runnerDic.getList().ToArray())
            {
                if (runner.state == ScriptRunner.ScriptState.Running)
                    runnerDic.bringToFront(runner);

                if (runner.Trigger == trigger)
                    runner.Run();
            }
        }

        internal bool isBusy(bool gaugeUpdate = true)
        {
            if (battle == null)
                return false;

            // ステータス変動によるゲージアニメをここでやってしまう
            // I will do a gauge animation by status change here
            bool isUpdated = false;
            if (gaugeUpdate)
            {
                foreach (var player in battle.playerData)
                {
                    isUpdated |= battle.UpdateBattleStatusData(player);
                }
                foreach (var enemy in battle.enemyData)
                {
                    isUpdated |= battle.UpdateBattleStatusData(enemy);
                }
                if (isUpdated)
                {
                    battle.statusUpdateTweener.Update();
                }
            }

            // メンバーチェンジを処理する
            // Handle member changes
            if (procMemberChange())
                isUpdated = true;

            // コマンド指定を処理する
            // process command specification
            if (battle.battleState == BattleState.Wait)
            {
                foreach (var action in actionQueue)
                {
                    if (action.cmd.type == Script.Command.FuncType.BTL_ACTION)
                    {
                        procSetAction(action.cmd, action.evGuid);
                    }
                }
                actionQueue.Clear();
            }

            var isResultInit = battle.battleState == BattleState.ResultInit;

            foreach (var runner in runnerDic.getList())
            {
                if ((!runner.isParallelTriggers() || (isResultInit && runner.isEffectTriggers())) &&
                    runner.state == ScriptRunner.ScriptState.Running)
                {
                    return true;
                }
            }

            foreach (var runner in mapRunnerBorrowed)
            {
                if (runner.state == ScriptRunner.ScriptState.Running)
                {
                    return true;
                }
            }

            // ダメージ用テキストとステータス用ゲージのアニメーションが終わるまで待つ
            // Wait for damage text and status gauge animation to finish
            if (isUpdated)
                return true;

            return false;
        }

        private void procSetAction(Script.Command curCommand, Guid evGuid)
        {
            int cur = 0;
            var tgt = getTargetData(curCommand, ref cur, evGuid);
            if (tgt == null)
                return;

            BattleCommand cmd = new BattleCommand();
            switch (curCommand.attrList[cur++].GetInt())
            {
                case 0:
                    {
                        cmd.type = BattleCommand.CommandType.ATTACK;
                        cmd.power = 100;
                        tgt.selectedBattleCommandType = BattleCommandType.Attack;
                        tgt.selectedBattleCommand = cmd;
                        battle.battleViewer.commandTargetSelector.Clear();
                        var tgt2 = getTargetData(curCommand, ref cur, evGuid);
                        if (tgt2 != null) {
                            battle.battleViewer.commandTargetSelector.AddBattleCharacters(new List<BattleCharacterBase>() { tgt2 });
                            battle.battleViewer.commandTargetSelector.SetSelect(tgt2);
                        }
                        tgt.targetCharacter = battle.GetTargetCharacters(tgt);
                    }
                    break;
                case 1:
                    cmd.type = BattleCommand.CommandType.GUARD;
                    cmd.power = (int)ScriptRunner.GetNumOrVariable(owner, evGuid, curCommand.attrList[cur++], false);
                    tgt.selectedBattleCommandType = BattleCommandType.Guard;
                    tgt.selectedBattleCommand = cmd;
                    tgt.targetCharacter = battle.GetTargetCharacters(tgt);
                    break;
                case 2:
                    cmd.type = BattleCommand.CommandType.CHARGE;
                    cmd.power = (int)ScriptRunner.GetNumOrVariable(owner, evGuid, curCommand.attrList[cur++], false);
                    tgt.selectedBattleCommandType = BattleCommandType.Charge;
                    tgt.selectedBattleCommand = cmd;
                    tgt.targetCharacter = battle.GetTargetCharacters(tgt);
                    break;
                case 3:
                    bool skipMessage = false;
                    if (curCommand.attrList.Count > cur)
                        skipMessage = curCommand.attrList[cur++].GetBool();
                    if(skipMessage)
                        tgt.selectedBattleCommandType = BattleCommandType.Cancel;
                    else
                        tgt.selectedBattleCommandType = BattleCommandType.Nothing;
                    break;
                case 4:
                    var guid = curCommand.attrList[cur++].GetGuid();
                    var skill = owner.catalog.getItemFromGuid<NSkill>(guid);
                    if (skill != null)
                    {
                        cmd.type = BattleCommand.CommandType.SKILL;
                        cmd.guId = guid;
                        tgt.selectedBattleCommandType = BattleCommandType.Skill;
                        tgt.selectedSkill = skill;
                        tgt.selectedBattleCommand = cmd;
                        battle.battleViewer.commandTargetSelector.Clear();
                        var tgt2 = getTargetData(curCommand, ref cur, evGuid);
                        if (tgt2 != null)
                        {
                            battle.battleViewer.commandTargetSelector.AddBattleCharacters(new List<BattleCharacterBase>() { tgt2 });
                            battle.battleViewer.commandTargetSelector.SetSelect(tgt2);
                        }
                        tgt.targetCharacter = battle.GetTargetCharacters(tgt);
                    }
                    else
                    {
                        tgt.selectedBattleCommandType = BattleCommandType.Nothing;
                    }
                    break;
            }

            if (tgt is BattlePlayerData)
                ((BattlePlayerData)tgt).forceSetCommand = true;
        }

        public override void applyCameraToBattle()
        {
#if false
            // カメラ情報を更新する
            // Update camera information
            var viewer = battle.battleViewer as BattleViewer3D;
            if (viewer != null)
            {
                // 前フレームのデータを反映(heroには注視点を入れておく)
                // Reflect the data of the previous frame (put the gaze point in the hero)
                var newCam = new ThirdPersonCameraSettings();
                newCam.offset = hero.pos;
                newCam.offset.Y -= viewer.camera.defaultHeight;
                newCam.angle.X = xAngle;
                newCam.angle.Y = yAngle;
                newCam.distance = dist;
                newCam.eyeHeight = eyeHeight;
                newCam.fov = fovy;
                newCam.nearClip = nearClip;
                viewer.camera.push(newCam, 0f);
            }
#endif
        }

        private bool procMemberChange()
        {
            // まだフェード中だったら次の処理をしない
            // If it's still fading, don't do the next process
            if (!battle.battleViewer.IsFadeEnd)
                return true;

            var viewer = battle.battleViewer as BattleViewer3D;
            if (viewer != null)
            {
                // まだ移動中だったら次の処理をしない
                // If it is still moving, do not proceed to the next step
                foreach (var actor in viewer.friends)
                {
                    if (actor == null)
                        continue;

                    if (actor.mapChr.moveMacros.Count > 0)
                        return true;
                }
            }

            // キューが空っぽだったら何もしない
            // do nothing if the queue is empty
            if (memberChangeQueue.Count == 0)
                return false;

            var entry = memberChangeQueue.Dequeue();
            entry.finished = true;

            if (entry.mob)
                return addRemoveMonster(entry, viewer);
            else
                return addRemoveParty(entry, viewer);
        }

        private bool addRemoveMonster(MemberChangeData entry, BattleViewer3D viewer)
        {
            // もうある場合はターゲットから消す
            // If there is more, remove it from the target
            var tgt = battle.enemyData.FirstOrDefault(x => x.UniqueID == entry.idx);
            if (tgt != null)
            {
                battle.enemyData.Remove(tgt);
                battle.targetEnemyData.Remove(tgt);

                battle.battleViewer.AddFadeOutCharacter(tgt);
                battle.removeVisibleEnemy(tgt);
            }

            var data = battle.addEnemyData((Guid)entry.id, entry.layout, entry.idx);
            battle.enemyData.Add(data);
            battle.addVisibleEnemy(data);
            battle.stockEnemyData.Remove(data);
            battle.targetEnemyData.Add(data);
            data.imageAlpha = 0;
            battle.battleViewer.AddFadeInCharacter(data);
            if (viewer != null)
            {
                var actor = viewer.AddEnemyMember(data, entry.idx);
                actor.queueActorState(BattleActor.ActorStateType.APPEAR, "walk", (int)BattleActor.ESCAPE_MAX_COUNT);
                actor.queueActorState(BattleActor.ActorStateType.APPEAR_END);
                actor.setOpacityMultiplier(0);
            }

            //battle.battleViewer.refreshLayout(battle.playerData, battle.enemyData);
            return true;
        }

        private bool addRemoveParty(MemberChangeData entry, BattleViewer3D viewer)
        {
            var tgt = searchPartyFromId(entry.id);
            bool doRefreshLayout = false;

            if (entry.cmd == MemberChangeData.Command.ADD)
            {
                // 4人以上いたら加入できない
                // You cannot join if there are more than 4 people.
                if (battle.playerData.Count >= catalog.getGameSettings().BattlePlayerMax)
                    return true;

                // 追加
                // addition
                if (tgt != null)   // 既に加入していたら何もしない / If already subscribed, do nothing
                    return true;

                if(!(entry.id is Guid))
                    return true;

                var hero = owner.data.party.GetHero((Guid)entry.id);
                var data = battle.addPlayerData(hero);
                battle.playerData.Add(data);
                battle.addVisiblePlayer(data);
                battle.stockPlayerData.Remove(data);
                battle.targetPlayerData.Add(data);
                data.imageAlpha = 0;
                if (viewer != null)
                {
                    BattleActor.party = owner.data.party;
                    var actor = viewer.AddPartyMember(data, playerLayouts);
                    actor.queueActorState(BattleActor.ActorStateType.APPEAR, "walk", (int)BattleActor.ESCAPE_MAX_COUNT);
                    actor.queueActorState(BattleActor.ActorStateType.APPEAR_END);
                    //actor.setOpacityMultiplier(0);
                    actor.mapChr.pos.Z += 1;  // 一歩下げる / take a step back
                }
                else
                {
                    battle.battleViewer.AddFadeInCharacter(data);
                }

                doRefreshLayout = true;
            }
            else if (entry.cmd == MemberChangeData.Command.SET_WAIT)
            {
                if (tgt == null)   // いないメンバーは外せない / You can't remove members who aren't there
                    return true;

                var data = tgt as BattlePlayerData;

                if (viewer != null)
                {
                    var actor = viewer.searchFromActors(tgt);
                    actor.queueActorState(BattleActor.ActorStateType.WAIT);
                }
            }
            else if (entry.cmd == MemberChangeData.Command.FADE_OUT)
            {
                // 0人になるような場合は外せない
                // If you have 0 people, you can't remove it
                if (battle.playerData.Count < 2)
                    return true;

                // 外す
                // remove
                if (tgt == null)   // いないメンバーは外せない / You can't remove members who aren't there
                    return true;

                var data = tgt as BattlePlayerData;

                if (viewer != null)
                {
                    var actor = viewer.searchFromActors(tgt);
                    actor.queueActorState(BattleActor.ActorStateType.DESTROY);
                    actor.walk(actor.X, actor.Z + 1);
                }
                else
                {
                    battle.battleViewer.AddFadeOutCharacter(data);
                }
            }
            else if (entry.cmd == MemberChangeData.Command.REMOVE)
            {
                // 0人になるような場合は外せない
                // If you have 0 people, you can't remove it
                if (battle.playerData.Count < 2)
                    return true;

                // 外す
                // remove
                if (tgt == null)   // いないメンバーは外せない / You can't remove members who aren't there
                    return true;

                // 詰める アンド アクター解放
                // stuffing and releasing actors
                if (viewer != null)
                {
                    var actor = viewer.searchFromActors(tgt);
                    int removeIndex = 0;
                    foreach (var fr in viewer.friends)
                    {
                        if (actor == fr)
                            break;
                        removeIndex++;
                    }
                    for (int i = removeIndex; i < viewer.friends.Count - 1; i++)
                    {
                        viewer.friends[i] = viewer.friends[i + 1];
                        viewer.friends[i + 1] = null;
                    }
                    viewer.friends[viewer.friends.Count - 1] = null;
                    actor.Release();
                }

                var data = tgt as BattlePlayerData;

                data.selectedBattleCommandType = BattleCommandType.Skip;

                battle.removeVisiblePlayer(data);
                //battle.stockPlayerData.Add(data);
                battle.playerData.Remove(data);
                battle.targetPlayerData.Remove(data);

                // GameDataに状態を反映する
                // Reflect state in GameData
                battle.ApplyPlayerDataToGameData(data);
                doRefreshLayout = true;
            }

            if (doRefreshLayout)
            {
                int index = 0;
                foreach (var chr in battle.playerData)
                {
                    // 3Dバトル用整列処理
                    // Alignment processing for 3D battle
                    if (viewer != null)
                    {
                        // 人が増減した場合に対応して正しい位置に移動してやる
                        // Move to the correct position in response to the increase or decrease in the number of people
                        var neutralPos = BattleCharacterPosition.getPosition(BattleSequenceManagerBase.battleFieldCenter,
                            BattleCharacterPosition.PosType.FRIEND, index, battle.playerData.Count);
                        neutralPos.X = (int)neutralPos.X;
                        neutralPos.Z = (int)neutralPos.Z;
                        neutralPos.Y = viewer.mapDrawer?.getAdjustedHeight(neutralPos.X, neutralPos.Z) ?? 0;
                        var actor = viewer.searchFromActors(chr);
                        actor.walk(neutralPos.X, neutralPos.Z);
                    }
                    else
                    {
                        chr.calcHeroLayout(index);
                    }

                    index++;
                }

                // 2Dバトル用整列処理
                // Alignment processing for 2D battle
                battle.battleViewer.refreshLayout(battle.playerData, null);
            }

            return true;
        }

        internal void checkAllSheet(MapCharacter mapChr, bool inInitialize, bool applyScript, bool applyGraphic, RomItem parentRom = null)
        {
            var data = owner.data;
            var rom = mapChr.rom;

            int nowPage = mapChr.currentPage;
            int destPage = -1;
            int index = 0;
            foreach (var sheet in rom.sheetList)
            {
                bool ok = true;
                foreach (var cond in sheet.condList)
                {
                    switch (cond.type)
                    {
                        case Common.Rom.Event.Condition.Type.COND_TYPE_SWITCH:
                            if (string.IsNullOrEmpty(cond.name))
                            {
                                var entry = catalog.getGameSettings().varDefs.getVariableEntry(mapChr.guId, VariableDefs.VarType.FLAG, cond.index);
                                if (entry != null)
                                {
                                    cond.name = entry.name;
                                    cond.local = entry.isLocal();
                                }
                            }
                            ok = data.system.GetSwitch(cond.name, cond.local ? mapChr.guId : Guid.Empty,
                                mapChr.IsDynamicGenerated) == (cond.option == 0 ? true : false);
                            break;
                        case Common.Rom.Event.Condition.Type.COND_TYPE_VARIABLE:
                            if (string.IsNullOrEmpty(cond.name))
                            {
                                var entry = catalog.getGameSettings().varDefs.getVariableEntry(mapChr.guId, VariableDefs.VarType.DOUBLE, cond.index);
                                if (entry != null)
                                {
                                    cond.name = entry.name;
                                    cond.local = entry.isLocal();
                                }
                            }
                            ok = MapEngine.checkCondition(data.system.GetVariable(cond.name, cond.local ? mapChr.guId : Guid.Empty,
                                mapChr.IsDynamicGenerated), cond.option, cond.cond);
                            break;
                        case Common.Rom.Event.Condition.Type.COND_TYPE_MONEY:
                            ok = MapEngine.checkCondition(data.party.GetMoney(), cond.option, cond.cond);
                            break;
                        case Common.Rom.Event.Condition.Type.COND_TYPE_ITEM:
                            ok = MapEngine.checkCondition(data.party.GetItemNum(cond.refGuid), cond.option, cond.cond);
                            break;
                        case Common.Rom.Event.Condition.Type.COND_TYPE_ITEM_WITH_EQUIPMENT:
                            ok = MapEngine.checkCondition(data.party.GetItemNum(cond.refGuid, true), cond.option, cond.cond);
                            break;
                        case Common.Rom.Event.Condition.Type.COND_TYPE_BATTLE:
                            ok = getBattlePhaseForCondition() == cond.option;
                            break;
                        case Common.Rom.Event.Condition.Type.COND_TYPE_HERO:
                            ok = data.party.ExistMember(cond.refGuid);
                            if (!ok && (cond.option >> 8) > 0)
                                ok = data.party.ExistInReserve(cond.refGuid);
                            if ((cond.option & 0xFF) != 0)
                                ok = !ok;
                            break;
                        case Common.Rom.Event.Condition.Type.COND_TYPE_HITPOINT:
                            //ok = MapEngine.checkCondition(mapChr.battleStatus.HitPoint, mapChr.battleStatus.MaxHitPoint * cond.option / 100, cond.cond);
                            break;
                    }
                    if (!ok)
                        break;
                }
                if (ok)
                    destPage = index;
                index++;
            }

            if (nowPage != destPage)
            {
                // 遷移先のページが有る場合
                // If there is a transition destination page
                if (destPage >= 0)
                {
                    var sheet = rom.sheetList[destPage];

                    if (applyGraphic)
                    {
                        var image = catalog.getItemFromGuid(sheet.graphic) as Common.Resource.GfxResourceBase;
                        changeCharacterGraphic(mapChr, image);
                        mapChr.playMotion(sheet.graphicMotion, inInitialize ? 0 : 0.2f);
                        mapChr.setDirection(sheet.direction, nowPage < 0);
                    }

                    if (applyScript)
                    {
                        // 前回登録していた Script を RunnerDic から外す
                        // Remove the previously registered Script from RunnerDic
                        if (nowPage >= 0)
                        {
                            var scriptId = mapChr.GetScriptId(catalog, nowPage);
                            if (runnerDic.ContainsKey(scriptId))
                            {
                                if (runnerDic[scriptId].state != ScriptRunner.ScriptState.Running)
                                {
                                    runnerDic[scriptId].finalize();
                                    runnerDic.Remove(scriptId);
                                }
                                else
                                {
                                    if (runnerDic[scriptId].Trigger == Common.Rom.Script.Trigger.PARALLEL ||
                                        runnerDic[scriptId].Trigger == Common.Rom.Script.Trigger.AUTO_PARALLEL)
                                        runnerDic[scriptId].removeTrigger = ScriptRunner.RemoveTrigger.ON_COMPLETE_CURRENT_LINE;
                                    else
                                        runnerDic[scriptId].removeTrigger = ScriptRunner.RemoveTrigger.ON_EXIT;
                                }
                            }
                        }

                        var script = mapChr.GetScript(catalog, destPage);

                        if (script != null)
                        {
                            // 付随するスクリプトを RunnerDic に登録する
                            // Register accompanying scripts with RunnerDic
                            var scriptId = mapChr.GetScriptId(catalog, destPage);

                            mapChr.expand = script.expandArea;
                            if (script.commands.Count > 0)
                            {
                                var runner = new ScriptRunner(this, mapChr, script, scriptId, parentRom);

                                // 自動的に開始(並列)が removeOnExit状態で残っている可能性があるので、関係ないGUIDに差し替える
                                // Automatically start (parallel) may remain in the removeOnExit state, so replace it with an unrelated GUID
                                if (runnerDic.ContainsKey(scriptId))
                                {
                                    var tmp = runnerDic[scriptId];
                                    tmp.key = Guid.NewGuid();
                                    runnerDic.Remove(scriptId);
                                    runnerDic.Add(tmp.key, tmp);
                                }

                                // 辞書に登録
                                // Add to dictionary
                                runnerDic.Add(scriptId, runner);

                                // 自動的に開始の場合はそのまま開始する
                                // If it starts automatically, just start
                                if (script.trigger == Common.Rom.Script.Trigger.AUTO ||
                                    script.trigger == Common.Rom.Script.Trigger.AUTO_REPEAT ||
                                    script.trigger == Common.Rom.Script.Trigger.PARALLEL ||
                                    script.trigger == Common.Rom.Script.Trigger.PARALLEL_MV ||
                                    script.trigger == Common.Rom.Script.Trigger.AUTO_PARALLEL ||
                                    script.trigger == Common.Rom.Script.Trigger.BATTLE_PARALLEL ||
                                    script.trigger == Common.Rom.Script.Trigger.GETITEM)
                                    runner.Run();
                            }
                        }
                    }
                }
                // 遷移先のページがない場合
                // When there is no transition destination page
                else
                {
                    // 前回登録していた Script を RunnerDic から外す
                    // Remove the previously registered Script from RunnerDic
                    if (nowPage >= 0)
                    {
                        if (applyGraphic)
                        {
                            changeCharacterGraphic(mapChr, null);
                        }

                        if (applyScript)
                        {
                            var scriptId = mapChr.GetScriptId(catalog, nowPage);

                            if (runnerDic.ContainsKey(scriptId))
                            {
                                if (runnerDic[scriptId].state != ScriptRunner.ScriptState.Running)
                                {
                                    runnerDic[scriptId].finalize();
                                    runnerDic.Remove(scriptId);
                                }
                                else
                                {
                                    if (runnerDic[scriptId].Trigger == Common.Rom.Script.Trigger.PARALLEL ||
                                        runnerDic[scriptId].Trigger == Common.Rom.Script.Trigger.AUTO_PARALLEL ||
                                        runnerDic[scriptId].Trigger == Common.Rom.Script.Trigger.PARALLEL_MV)
                                        runnerDic[scriptId].removeTrigger = ScriptRunner.RemoveTrigger.ON_COMPLETE_CURRENT_LINE;
                                    else
                                        runnerDic[scriptId].removeTrigger = ScriptRunner.RemoveTrigger.ON_EXIT;
                                }
                            }
                        }
                    }
                }

                mapChr.currentPage = destPage;
            }
        }

        internal void changeCharacterGraphic(MapCharacter mapChr, Common.Resource.GfxResourceBase image)
        {
            var v3d = battle.battleViewer as BattleViewer3D;

            if (v3d == null)
                return;

            // グラフィックは違ってた場合だけ変える(Resetが重いので)
            // Change the graphics only if they are different (because Reset is heavy)
            if (image != mapChr.getGraphic())
            {
                if (image != null)
                {
                    mapChr.ChangeGraphic(image, v3d.mapDrawer);
                }
                else
                {
                    mapChr.ChangeGraphic(null, v3d.mapDrawer);
                }
            }
        }

        private int getBattlePhaseForCondition()
        {
            if (battle.battleState <= BattleState.BattleStart)
                return 0;
            if (battle.battleState >= BattleState.StartBattleFinishEvent)
                return 2;

            return 1;
        }

        public override void showBattleUi(bool v)
        {
            battleUiVisibility = v;
        }

        public override void healBattleCharacter(Script.Command curCommand, Guid evGuid)
        {
            int cur = 0;
            var guid = curCommand.attrList[cur].GetGuid();
            var idx = (int)ScriptRunner.GetNumOrVariable(owner, evGuid, curCommand.attrList[cur++], false);
            var tgtIsMp = curCommand.attrList[cur++].GetBool();
            var valueAttr = curCommand.attrList[cur++];
            var value = (int)ScriptRunner.GetNumOrVariable(owner, evGuid, valueAttr, false);
            var varIdx = value;
            var invert = curCommand.attrList[cur++].GetInt();
            if (invert != 0)
                value *= -1;
            BattleCharacterBase chr = null;
            bool showDamage = false;
            if (curCommand.attrList.Count > cur)
            {
                // タイプ？
                // type?
                switch (curCommand.attrList[cur++].GetInt())
                {
                    case 0:
                        chr = searchPartyFromId(guid);
                        break;
                    case 1:
                        var ptIdx = idx - 1;
                        if (battle.playerData.Count > ptIdx && battle.playerData[ptIdx] != null)
                            chr = battle.playerData[ptIdx];
                        break;
                    case 2:
                        var mobIdx = idx;
                        chr = battle.enemyData.FirstOrDefault(x => x.UniqueID == mobIdx);
                        break;
                }

                // 変数？
                // variable?
                if (curCommand.attrList[cur++].GetBool())
                {
                    value = (int)ScriptRunner.GetVariable(owner, evGuid, valueAttr, false);
                    if (invert >= 2)
                        value *= -1;
                }

                showDamage = curCommand.attrList[cur++].GetBool();
            }
            else
            {
                chr = battle.enemyData.FirstOrDefault(x => x.UniqueID == idx);
            }

            BattleDamageTextInfo.TextType textType = BattleDamageTextInfo.TextType.Miss;

            if (chr != null)
            {
                if (tgtIsMp)
                {
                    chr.MagicPoint += value;
                    if (chr.MagicPoint > chr.MaxMagicPoint)
                        chr.MagicPoint = chr.MaxMagicPoint;
                    else if (chr.MagicPoint < 0)
                        chr.MagicPoint = 0;

                    textType = value > 0 ? BattleDamageTextInfo.TextType.MagicPointHeal : BattleDamageTextInfo.TextType.MagicPointDamage;
                }
                else
                {
                    chr.HitPoint += value;
                    if (chr.HitPoint > chr.MaxHitPoint)
                        chr.HitPoint = chr.MaxHitPoint;
                    else if (chr.HitPoint < 0)
                        chr.HitPoint = 0;

                    textType = value > 0 ? BattleDamageTextInfo.TextType.HitPointHeal : BattleDamageTextInfo.TextType.HitPointDamage;

                    if (chr.HitPoint > 0)
                    {
                        if (chr.IsDeadCondition())
                        {
                            if (chr is BattleEnemyData)
                            {
                                battle.battleViewer.AddFadeInCharacter(chr);
                            }
                            chr.Resurrection();
                            chr.ConsistancyHPPercentConditions(catalog, this);
                        }
                    }
                    else if (chr.HitPoint == 0)
                    {
                        if (!chr.IsDeadCondition(true))
                        {
                            if (chr is BattleEnemyData)
                            {
                                battle.battleViewer.AddFadeOutCharacter(chr);
                                Audio.PlaySound(owner.se.defeat);
                            }
                            chr.Down(catalog, this);
                        }
                    }
                }

                if (chr is BattlePlayerData)
                {
                    ((BattlePlayerData)chr).battleStatusData.MagicPoint = chr.MagicPoint;
                }
                battle.statusUpdateTweener.Begin(0, 1.0f, 30);
                battle.SetNextBattleStatus(chr);
            }

            if (chr != null && showDamage)
            {
                string text = value.ToString();
                if (value < 0)
                {
                    text = (-value).ToString();
                }

                battle.battleViewer.AddDamageTextInfo(new BattleDamageTextInfo(textType, chr, text));
            }
        }

        public override void healParty(Script.Command curCommand, Guid evGuid)
        {
            int cur = 0;
            var tgts = new List<BattleCharacterBase>();
            getTargetDataForMapArgs(tgts, curCommand, ref cur);

            var tgtIsMp = curCommand.attrList[cur++].GetBool();
            var value = (int)ScriptRunner.GetNumOrVariable(owner, evGuid, curCommand.attrList[cur++], false);
            var invert = curCommand.attrList[cur++].GetBool();
            if (invert)
                value *= -1;

            foreach (var chr in tgts)
            {
                if (tgtIsMp)
                {
                    chr.MagicPoint += value;
                    if (chr.MagicPoint > chr.MaxMagicPoint)
                        chr.MagicPoint = chr.MaxMagicPoint;
                    else if (chr.MagicPoint < 0)
                        chr.MagicPoint = 0;
                }
                else
                {
                    chr.HitPoint += value;
                    if (chr.HitPoint > chr.MaxHitPoint)
                        chr.HitPoint = chr.MaxHitPoint;
                    else if (chr.HitPoint < 0)
                        chr.HitPoint = 0;

                    if ((chr.HitPoint > 0) && chr.IsDeadCondition())
                        chr.Resurrection();
                    else if (chr.HitPoint == 0)
                        chr.Down(catalog, this);

                    chr.ConsistancyHPPercentConditions(catalog, this);
                }

                battle.SetNextBattleStatus(chr);
                battle.statusUpdateTweener.Begin(0, 1.0f, 30);
            }
        }

        public override MapCharacter getBattleActorMapChr(Script.Command curCommand, Guid evGuid )
        {
            int cur = 0;
            var tgt = getTargetData(curCommand, ref cur, evGuid);

            var viewer = battle.battleViewer as BattleViewer3D;
            if (viewer == null)
                return null;

            var actor = viewer.searchFromActors(tgt);
            if (actor == null)
                return null;

            return actor.mapChr;
        }

        private BattleCharacterBase getTargetData(Script.Command rom, ref int cur, Guid evGuid)
        {
            // ターゲット指定がない場合がある
            // Sometimes there is no target designation
            if (rom.attrList.Count <= cur)
                return null;

            BattleCharacterBase result = null;
            switch (rom.attrList[cur++].GetInt())
            {
                case 0:
                    var heroGuid = rom.attrList[cur++].GetGuid();
                    result = searchPartyFromId(heroGuid);
                    break;
                case 1:
                    var ptIdx = (int)ScriptRunner.GetNumOrVariable(owner, evGuid, rom.attrList[cur++], false) - 1;
                    if (ptIdx >= 0 && battle.playerData.Count > ptIdx && battle.playerData[ptIdx] != null)
                        result = battle.playerData[ptIdx];
                    break;
                case 2:
                    var mobIdx = (int)ScriptRunner.GetNumOrVariable(owner, evGuid, rom.attrList[cur++], false);
                    result = battle.enemyData.FirstOrDefault(x => x.UniqueID == mobIdx);
                    break;
            }
            return result;
        }

        private BattleCharacterBase searchPartyFromId(object heroId)
        {
            if (heroId is Guid)
            {
                var id = (Guid)heroId;

                foreach (var chr in battle.playerData)
                {
                    if (chr.player.rom.guId != id)
                        continue;

                    return chr;
                }
            }
            else if(heroId is int)
            {
                var idx = (int)heroId;

                if((0 <= idx) && (idx < battle.playerData.Count))
                {
                    return battle.playerData[idx];
                }
            }

            return null;
        }

        public override void setNextAction(Script.Command curCommand, Guid evGuid)
        {
            int cur = 0;
            var tgt = getTargetData(curCommand, ref cur, evGuid);
            if (tgt == null)
                return;

            // すでに同じ対象のものがキューに入っていたら、まず外す
            // If the same target is already in the queue, remove it first
            actionQueue.RemoveAll(x =>
            {
                int cur2 = 0;
                var tgt2 = getTargetData(x.cmd, ref cur2, evGuid);
                return tgt == tgt2 && x.cmd.type == curCommand.type;
            });

            actionQueue.Add(new ActionData() { cmd = curCommand, evGuid = evGuid });
        }

        public override void setBattleStatus(Script.Command curCommand, Guid evGuid, bool typeReduced = false)
        {
            int cur = 0;
            List<BattleCharacterBase> tgts = new List<BattleCharacterBase>();
            if (typeReduced)
            {
                getTargetDataForMapArgs(tgts, curCommand, ref cur);
            }
            else
            {
                var tgt = getTargetData(curCommand, ref cur, evGuid);

                if (tgt == null)
                {
                    return;
                }

                tgts = new List<BattleCharacterBase>(1);

                tgts.Add(tgt);
            }

            var condition = catalog.getItemFromGuid<Common.Rom.Condition>(curCommand.attrList[cur++].GetGuid());

            if (condition == null)
            {
                return;
            }

            bool add = curCommand.attrList[cur++].GetInt() == 0;

            foreach (var tgt in tgts)
            {
                if (add)
                {
                    if (condition.deadCondition && condition.deadConditionPercent == 0)
                    {
                        tgt.HitPoint = 0;
                        tgt.SetCondition(catalog, condition.guId, this);
                        battle.SetNextBattleStatus(tgt);
                    }
                    else if (!tgt.IsDeadCondition())
                    {
                        tgt.SetCondition(catalog, condition.guId, this);
                    }
                }
                else
                {
                    if (condition.deadCondition && condition.deadConditionPercent == 0)
                    {
                        if (tgt.IsDeadCondition())
                        {
                            tgt.HitPoint = 1;
                            tgt.Resurrection();
                            battle.SetNextBattleStatus(tgt);
                        }
                    }
                    else
                    {
                        tgt.RecoveryCondition(condition.guId);
                    }
                    tgt.ConsistancyHPPercentConditions(catalog, this);
                }
            }

            battle.statusUpdateTweener.Begin(0, 1.0f, 30);
        }

        private void getTargetDataForMapArgs(List<BattleCharacterBase> tgts, Script.Command curCommand, ref int cur)
        {
            var type = (Common.Rom.Script.Command.ChangePartyMemberType)curCommand.attrList[cur++].GetInt();
            var id = curCommand.attrList[cur++].GetGuid();
            var idx = curCommand.attrList[cur++].GetInt();

            switch (type)
            {
                case Script.Command.ChangePartyMemberType.Cast:
                    foreach (var chr in battle.playerData)
                    {
                        if (id == Guid.Empty || chr.player.rom.guId == id)
                        {
                            tgts.Add(chr);
                        }
                    }

                    if (tgts.Count == 0)
                    {
                        return;
                    }
                    break;
                case Script.Command.ChangePartyMemberType.Index:
                    {
                        var tgt = searchPartyFromId(idx);

                        if (tgt == null)
                        {
                            return;
                        }

                        tgts.Add(tgt);
                    }
                    break;
                default:
                    return;
            }
        }

        public override MemberChangeData addMonster(Script.Command curCommand, Guid evGuid)
        {
            int curAttr = 0;
            var idx = (int)ScriptRunner.GetNumOrVariable(owner, evGuid, curCommand.attrList[curAttr++], false);
            var guid = curCommand.attrList[curAttr++].GetGuid();

            if (catalog.getItemFromGuid(guid) == null)
                return new MemberChangeData() { finished = true };

            var useLayout = curCommand.attrList[curAttr++].GetBool();
            Vector3? layout = null;
            if(useLayout)
            {
                layout = new Vector3((int)curCommand.attrList[curAttr++].GetFloat(), 0, (int)curCommand.attrList[curAttr++].GetFloat());
            }

            Console.WriteLine("appear : " + idx);
            var result = new MemberChangeData() { mob = true, cmd = MemberChangeData.Command.ADD, idx = idx, id = guid, layout = layout };
            memberChangeQueue.Enqueue(result);
            return result;
        }

        public override void battleStop()
        {
            if (isBattleForciblyTerminate)
                return;

            isBattleForciblyTerminate = true;
            battle.battleState = BattleState.StopByEvent;
            ((BattleViewer3D)battle.battleViewer).camManager.setWaitFunc(null);
        }

        public override void fullRecovery(bool poison = true, bool revive = true)
        {
            foreach (var chr in battle.playerData)
            {
                if (revive || chr.HitPoint >= 1)
                {
                    chr.HitPoint = chr.MaxHitPoint;
                    chr.MagicPoint = chr.MaxMagicPoint;
                    if (poison)
                        chr.conditionInfoDic.Clear();

                    battle.SetNextBattleStatus(chr);
                }
            }

            battle.statusUpdateTweener.Begin(0, 1.0f, 30);
        }

        public override int getStatus(Script.Command.IfHeroSourceType srcTypePlus, Guid option, Common.GameData.Hero hero)
        {
            var tgt = searchPartyFromId(hero.rom.guId) as BattlePlayerData;
            if (tgt == null)
                return 0;

            switch (srcTypePlus)
            {
                case Script.Command.IfHeroSourceType.STATUS_AILMENTS:
                    return hero.conditionInfoDic.ContainsKey(option) ? 1 : 0;
                case Script.Command.IfHeroSourceType.LEVEL:
                    return hero.level;
                case Script.Command.IfHeroSourceType.HITPOINT:
                    return tgt.HitPoint;
                case Script.Command.IfHeroSourceType.MAGICPOINT:
                    return tgt.MagicPoint;
                case Script.Command.IfHeroSourceType.ATTACKPOWER:
                    return tgt.Attack;
                case Script.Command.IfHeroSourceType.Defense:
                    return tgt.Defense;// TODO 体力を考慮 / TODO Consider physical strength
                case Script.Command.IfHeroSourceType.POWER:
                    return tgt.Power;
                case Script.Command.IfHeroSourceType.VITALITY:
                    return tgt.VitalityBase;
                case Script.Command.IfHeroSourceType.MAGIC:
                    return tgt.Magic;
                case Script.Command.IfHeroSourceType.SPEED:
                    return tgt.Speed;
                case Script.Command.IfHeroSourceType.EQUIPMENT_WEIGHT:
                    return 0;
            }

            return 0;
        }

        public override void addStatus(Common.GameData.Hero hero, ScriptRunner.HeroStatusType type, int num)
        {
            var tgt = searchPartyFromId(hero.rom.guId) as BattlePlayerData;
            if (tgt == null)
                return;

            switch (type)
            {
                case ScriptRunner.HeroStatusType.HITPOINT:
                    tgt.MaxHitPointBase += num;
                    break;
                case ScriptRunner.HeroStatusType.MAGICPOINT:
                    tgt.MaxMagicPointBase += num;
                    break;
                case ScriptRunner.HeroStatusType.ATTACKPOWER:
                    tgt.AttackBase += num;
                    break;
                case ScriptRunner.HeroStatusType.MAGIC:
                    tgt.MagicBase += num;
                    break;
                case ScriptRunner.HeroStatusType.DEFENSE:
                    tgt.DefenseBase += num;
                    break;
                case ScriptRunner.HeroStatusType.SPEED:
                    tgt.SpeedBase += num;
                    break;
            }


            battle.SetNextBattleStatus(tgt);
            battle.statusUpdateTweener.Begin(0, 1.0f, 30);
        }

        // パラメータの整合性を取るメソッド
        // Method for parameter consistency
        public void consistency(BattlePlayerData p)
        {
            // ほかのパラメータも0未満にはならない
            // No other parameter can be less than 0
            if (p.MagicPoint < 0)
                p.MagicPoint = 0;
            if (p.MaxHitPointBase < 1)
                p.MaxHitPointBase = 1;
            if (p.MaxMagicPointBase < 0)
                p.MaxMagicPointBase = 0;
            if (p.AttackBase < 0)
                p.AttackBase = 0;
            if (p.MagicBase < 0)
                p.MagicBase = 0;
            if (p.DefenseBase < 0)
                p.DefenseBase = 0;
            if (p.SpeedBase < 0)
                p.SpeedBase = 0;

            p.HitPoint = Math.Min(p.HitPoint, Common.GameData.Hero.MAX_STATUS);
            p.MagicPoint = Math.Min(p.MagicPoint, Common.GameData.Hero.MAX_STATUS);
            p.HitPoint = Math.Min(p.HitPoint, p.MaxHitPointBase);
            p.MagicPoint = Math.Min(p.MagicPoint, p.MaxMagicPointBase);
            p.AttackBase = Math.Min(p.AttackBase, Common.GameData.Hero.MAX_STATUS);
            p.MagicBase = Math.Min(p.MagicBase, Common.GameData.Hero.MAX_STATUS);
            p.DefenseBase = Math.Min(p.DefenseBase, Common.GameData.Hero.MAX_STATUS);
            p.SpeedBase = Math.Min(p.SpeedBase, Common.GameData.Hero.MAX_STATUS);

            // HPが0以下なら死亡にする
            // If your HP is 0 or less, you will die.
            if (p.HitPoint <= 0)
            {
                p.HitPoint = 0;
                p.Down(catalog, this);
            }
            else if (p.IsDeadCondition())
            {
                p.Resurrection();
            }
        }

        public override MemberChangeData addRemoveMember(Script.Command curCommand)
        {
            int curAttr = 0;
            var type = (Common.Rom.Script.Command.ChangePartyMemberType)curCommand.attrList[curAttr++].GetInt();
            var id = curCommand.attrList[curAttr++].GetGuid();
            var idx = curCommand.attrList[curAttr++].GetInt();
            MemberChangeData lastItem = new MemberChangeData() { finished = true };

            var add = !curCommand.attrList[curAttr++].GetBool();
            if (add)
            {
                switch (type)
                {
                    case Script.Command.ChangePartyMemberType.Cast:
                        memberChangeQueue.Enqueue(new MemberChangeData() { cmd = MemberChangeData.Command.ADD, id = id });
                        lastItem = new MemberChangeData() { cmd = MemberChangeData.Command.SET_WAIT, id = id };
                        memberChangeQueue.Enqueue(lastItem);
                        break;
                    case Script.Command.ChangePartyMemberType.Index:
                        break;
                    default:
                        break;
                }
            }
            else
            {
                switch (type)
                {
                    case Script.Command.ChangePartyMemberType.Cast:
                        memberChangeQueue.Enqueue(new MemberChangeData() { cmd = MemberChangeData.Command.FADE_OUT, id = id });
                        lastItem = new MemberChangeData() { cmd = MemberChangeData.Command.REMOVE, id = id };
                        memberChangeQueue.Enqueue(lastItem);
                        break;
                    case Script.Command.ChangePartyMemberType.Index:
                        memberChangeQueue.Enqueue(new MemberChangeData() { cmd = MemberChangeData.Command.FADE_OUT, id = idx });
                        lastItem = new MemberChangeData() { cmd = MemberChangeData.Command.REMOVE, id = idx };
                        memberChangeQueue.Enqueue(lastItem);
                        break;
                    default:
                        break;
                }
            }

            return lastItem;
        }

        public override void start(Guid commonExec)
        {
            var runner = GetScriptRunner(commonExec);
            if (runner != null)
            {
                // MapSceneから借りてきたrunnerは明示的にupdateするリストに入れてやる
                // Runners borrowed from MapScene are explicitly added to the list to be updated
                if (!mapRunnerBorrowed.Contains(runner))
                {
                    mapRunnerBorrowed.Add(runner);
                    runner.owner = this;
                }

                runner.Run();
            }
        }

        public override ScriptRunner GetScriptRunner(Guid guid)
        {
            return owner.mapScene.runnerDic.getList()
                .Where(x => x.mapChr != null && x.mapChr.rom != null && x.mapChr.rom.guId == guid)
                .FirstOrDefault();
        }

        public override void RefreshHeroMapChr(Cast rom)
        {
            var viewer = battle.battleViewer as BattleViewer3D;
            if (viewer == null)
            {
                refreshFace();
                return;
            }

            // グラフィックが変わってたら適用する
            // Apply if graphics change
            foreach (var friend in viewer.friends)
            {
                if (friend == null)
                    break;

                var pl = friend.source as BattlePlayerData;
                var guid = owner.data.party.getMemberGraphic(pl.player.rom);
                var nowGuid = Guid.Empty;
                if (friend.mapChr.getGraphic() != null)
                    nowGuid = friend.mapChr.getGraphic().guId;
                if (pl != null && pl.player.rom == rom &&
                    guid != nowGuid)
                {
                    var res = catalog.getItemFromGuid<Common.Resource.GfxResourceBase>(guid);
                    friend.mapChr.ChangeGraphic(res, viewer.mapDrawer);
                }
            }
        }

        public override void RefreshHeroJoint(Guid heroGuid)
        {
            var viewer = battle.battleViewer as BattleViewer3D;
            if (viewer == null)
                return;

            var data = searchPartyFromId(heroGuid) as BattlePlayerData;
            if (data == null)
                return;

            var actor = viewer.searchFromActors(data);
            actor.mapChr.setJointInfoFromGameData(catalog, data.player, owner.data.party);
        }

        public override MapCharacter GetHeroForBattle(Cast rom = null)
        {
            var viewer = battle.battleViewer as BattleViewer3D;
            if (viewer != null)
            {
                if (rom != null)
                {
                    foreach (var friend in viewer.friends)
                    {
                        if (friend == null)
                            continue;

                        var pl = friend.source as BattlePlayerData;
                        if (pl != null && pl.player.rom == rom)
                            return friend.mapChr;
                    }
                }

                return viewer.friends[0].mapChr;
            }

            return base.GetHeroForBattle();
        }

        private void refreshFace()
        {
            // グラフィックが変わってたら適用する
            // Apply if graphics change
            foreach (var friend in battle.playerData)
            {
                if (friend == null)
                    break;

                var pl = friend as BattlePlayerData;
                var guid = owner.data.party.getMemberFace(pl.player.rom);
                if (guid != pl.currentFace)
                {
                    var res = catalog.getItemFromGuid<Common.Resource.SliceAnimationSet>(guid);
                    pl.setFaceImage(res);
                }
            }
        }

        public override void setHeroName(Guid hero, string nextHeroName)
        {
            base.setHeroName(hero, nextHeroName);

            var tgt = searchPartyFromId(hero);
            if (tgt != null)
                tgt.Name = nextHeroName;
        }

        public override int getBattleStatus(Script.Command.VarHeroSourceType srcTypePlus, Guid option, Common.GameData.Hero hero)
        {
            var battleStatus = searchPartyFromId(hero.rom.guId);
            return ScriptRunner.getBattleStatus(battleStatus, srcTypePlus, option, battle.playerData);
        }

        public override int getPartyStatus(Script.Command.VarHeroSourceType srcTypePlus, Guid option, int index)
        {
            if (battle.playerData.Count <= index)
                return 0;

            var battleStatus = battle.playerData[index];
            return ScriptRunner.getBattleStatus(battleStatus, srcTypePlus, option, battle.playerData);
        }

        public override int getPartyNumber()
        {
            return battle.playerData.Count;
        }

        public override int getEnemyStatus(Script.Command.VarHeroSourceType srcTypePlus, Guid option, int index)
        {
            var battleStatus = battle.enemyData.FirstOrDefault(x => x.UniqueID == index);
            if (battleStatus == null)
                return 0;

            return ScriptRunner.getBattleStatus(battleStatus, srcTypePlus, option, battle.playerData);
        }

        public override Guid getEnemyGuid(int index)
        {
            var battleStatus = battle.enemyData.FirstOrDefault(x => x.UniqueID == index);
            if (battleStatus == null)
                return Guid.Empty;

            return battleStatus.monster.guId;
        }

        internal void setLastSkillUserIndex(BattleCharacterBase user)
        {
            var index = 0;

            foreach (var pl in battle.playerData)
            {
                if (pl == user)
                {
                    lastSkillUseCampType = CampType.Party;
                    lastSkillUserIndex = index;

                    return;
                }

                index++;
            }

            index = 0;

            foreach (var pl in battle.enemyData)
            {
                if (pl == user)
                {
                    lastSkillUseCampType = CampType.Enemy;
                    lastSkillUserIndex = index;

                    return;
                }

                index++;
            }
        }

        internal void setLastSkillTargetIndex(BattleCharacterBase[] friendEffectTargets, BattleCharacterBase[] enemyEffectTargets)
        {
            if (friendEffectTargets.Length > 0)
            {
                int index = 0;
                foreach (var pl in battle.playerData)
                {
                    if (pl == friendEffectTargets[0])
                    {
                        lastSkillTargetIndex = index;
                        return;
                    }
                    index++;
                }

                index = 0;
                foreach (var pl in battle.enemyData)
                {
                    if (pl == friendEffectTargets[0])
                    {
                        lastSkillTargetIndex = index;
                        return;
                    }
                    index++;
                }
            }

            if (enemyEffectTargets.Length > 0)
            {
                int index = 0;
                foreach (var pl in battle.playerData)
                {
                    if (pl == enemyEffectTargets[0])
                    {
                        lastSkillTargetIndex = index;
                        return;
                    }
                    index++;
                }

                index = 0;
                foreach (var pl in battle.enemyData)
                {
                    if (pl == enemyEffectTargets[0])
                    {
                        lastSkillTargetIndex = index;
                        return;
                    }
                    index++;
                }
            }
        }

        public override bool existMember(Guid heroGuid)
        {
            return searchPartyFromId(heroGuid) != null;
        }

        public override void ReservationChangeScene(GameMain.Scenes scene)
        {
            if (scene == GameMain.Scenes.TITLE)
            {
                battle.battleState = BattleState.BattleFinishCheck1;
                reservedResult = BattleResultState.Escape_ToTitle;
            }
        }

        public override bool DoGameOver()
        {
            battle.battleState = BattleState.BattleFinishCheck1;
            reservedResult = BattleResultState.Lose_GameOver;

            return true;
        }

        internal void checkForceBattleFinish(ref BattleResultState resultState)
        {
            if (reservedResult != BattleResultState.NonFinish)
            {
                resultState = reservedResult;
            }
        }

        public override void refreshEquipmentEffect(Common.GameData.Hero hero)
        {
            var data = searchPartyFromId(hero.rom.guId) as BattlePlayerData;

            if (data == null)
                return;

            battle.ApplyPlayerDataToGameData(data);
            data.SetParameters(hero, owner.debugSettings.battleHpAndMpMax, owner.debugSettings.battleStatusMax, owner.data.party);

            var viewer = battle.battleViewer as BattleViewer3D;
            if (viewer == null)
                return;

            var actor = viewer.searchFromActors(data);
            if (actor == null)
                return;

            BattleActor.createWeaponModel(ref actor, catalog);
        }

        internal void setBattleResult(BattleResultState battleResult)
        {
            switch (battleResult)
            {
                case BattleResultState.Win:
                    lastBattleResult = 1;
                    break;
                case BattleResultState.Lose_Advanced_GameOver:
                case BattleResultState.Lose_Continue:
                case BattleResultState.Lose_GameOver:
                    lastBattleResult = 2;
                    break;
                case BattleResultState.Escape:
                case BattleResultState.Escape_ToTitle:
                    lastBattleResult = 3;
                    break;
            }
        }

        public override void StopCameraAnimation()
        {
            var viewer = battle.battleViewer as BattleViewer3D;
            if (viewer == null)
                return;

            viewer.StopCameraAnimation();
        }
    }
}

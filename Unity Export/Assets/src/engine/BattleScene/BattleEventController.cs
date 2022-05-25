using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Yukar.Common;
using Yukar.Common.Rom;
using System.Linq;

namespace Yukar.Engine
{
    class BattleEventController : MapScene
    {
        internal class MemberChangeData
        {
            public enum Command
            {
                ADD,
                REMOVE,
                FADE_OUT,
                SET_WAIT,
            }
            internal Command cmd;
            internal Guid guid;
            internal int idx;
            internal Point? layout;
            internal bool mob;
            internal bool finished;
        }
        Queue<MemberChangeData> memberChangeQueue = new Queue<MemberChangeData>();

        private List<MapCharacter> dummyChrs = new List<MapCharacter>();
        private List<MapCharacter> extras = new List<MapCharacter>();
        private GameDataManager data;
        private Catalog catalog;
        private BattleSequenceManager battle;

        public bool battleUiVisibility = true;
        private List<ScriptRunner> mapRunnerBorrowed = new List<ScriptRunner>();
        private List<Script.Command> actionQueue = new List<Script.Command>();

        public static int lastBattleResult;
        public static int lastSkillTargetIndex;
        private BattleSequenceManager.BattleResultState reservedResult = BattleSequenceManager.BattleResultState.NonFinish;

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
            hero = new MapCharacter();
            isBattle = true;
        }

        internal void init(BattleSequenceManager battle, GameDataManager data, Catalog catalog, List<BattlePlayerData> playerData, List<BattleEnemyData> enemyMonsterData)
        {
            this.battle = battle;
            this.data = data;
            this.catalog = catalog;

            // バトルイベントの初期化
            foreach (var guid in catalog.getGameSettings().battleEvents)
            {
                var ev = catalog.getItemFromGuid<Event>(guid);
                if (ev == null)
                    continue;
                var dummyChr = new MapCharacter(ev);
                dummyChr.isCommonEvent = true;
                checkAllSheet(dummyChr, true, true, false);
                dummyChrs.Add(dummyChr);
            }

            memberChangeQueue.Clear();
            extras = null;
        }

        private void initBattleFieldPlacedEvents()
        {
            if (extras != null)
                return;

            var v3d = battle.battleViewer as BattleViewer3D;

            if (v3d == null)
                return;

            extras = v3d.extras;
            foreach (var chrs in extras)
            {
                checkAllSheet(chrs, true, false, true);
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
        }

        internal void update()
        {
            // バトルフィールドに置いてあるイベントの初期化
            initBattleFieldPlacedEvents();

            // バトルビューアからのカメラ情報をセットしておく
            var viewer = battle.battleViewer as BattleViewer3D;
            if (viewer != null)
            {
                // 前フレームのデータを反映(heroには注視点を入れておく)
                var now = viewer.camera.Now;
                hero.x = BattleViewer3D.CenterOfField.X + 0.5f;
                hero.y = -1;
                hero.z = BattleViewer3D.CenterOfField.Y + 0.5f;
                xangle = now.xAngle;
                yangle = now.yAngle;
                dist = now.distance;
                fovy = now.fov;
                map = viewer.drawer.mapRom;
            }

            // 各ウィンドウのアップデート
            shopWindow.Update();
            choicesWindow.Update();
            messageWindow.SetKeepWindowFlag(choicesWindow.IsActive());
            messageWindow.Update();
            moneyWindow.Update();
            toastWindow.Update();
            inputStringWindow.Update();
            trashWindow.update();
            spManager.Update();

            var isEventProcessed = procScript();

            if (!isEventProcessed)
            {
                // シートチェンジ判定
                foreach (var mapChr in dummyChrs)
                {
                    checkAllSheet(mapChr, false, true, false);
                }
                if (extras != null)
                {
                    foreach (var chrs in extras)
                    {
                        checkAllSheet(chrs, false, false, true);
                    }
                }
            }
        }

        internal override void applyCameraToBattle()
        {
            // カメラ情報を更新する
            var viewer = battle.battleViewer as BattleViewer3D;
            if (viewer != null)
            {
                // 前フレームのデータを反映(heroには注視点を入れておく)
                var newCam = new ThirdPersonCameraSettings();
                var tgt = GetLookAtTarget();
                newCam.x = tgt.x;
                newCam.y = tgt.z;
                newCam.height = tgt.y;
                newCam.xAngle = xangle;
                newCam.yAngle = yangle;
                newCam.distance = dist;
                newCam.fov = fovy;
                viewer.camera.push(newCam, 0f);
            }
        }

        private bool procScript()
        {
            bool result = false;

            // 通常スクリプトのアップデート
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
                        if (isFinished)
                            break;
                        // 並列動作しないので、自動移動以外は最初に見つかったRunningしか実行しない
                        if (runner.state == ScriptRunner.ScriptState.Running)
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }

            // その他の並列スクリプトのアップデート
            foreach (var runner in runners)
            {
                if (runner.isParallelTriggers())
                {
                    runner.Update();
                }
            }

            return result;
        }

        internal new void Draw()
        {
            Graphics.BeginDraw();

            // スプライト描画
            spManager.Draw(0, SpriteManager.SYSTEM_SPRITE_INDEX);

            // エフェクト描画
            while (effectDrawEntries.Count > 0)
            {
                var entry = effectDrawEntries.Dequeue();
                entry.efDrawer.draw(entry.x, entry.y);
            }

            // スクリーンカラーを適用
            Graphics.DrawFillRect(0, 0, Graphics.ViewportWidth, Graphics.ViewportHeight,
                screenColor.R, screenColor.G, screenColor.B, screenColor.A);

            // スプライト描画
            spManager.Draw(SpriteManager.SYSTEM_SPRITE_INDEX, SpriteManager.MAX_SPRITE);

            // ウィンドウ描画
            messageWindow.Draw();
            choicesWindow.Draw();
            moneyWindow.Draw();
            shopWindow.Draw();
            toastWindow.Draw();
            inputStringWindow.Draw();
            trashWindow.draw();
            Graphics.EndDraw();
        }

        public override void GetCharacterScreenPos(MapCharacter chr, out int x, out int y, EffectPosType pos = EffectPosType.Ground)
        {
            var viewer = battle.battleViewer as BattleViewer3D;
            SharpKmyMath.Matrix4 p, v;
            if (viewer != null && chr != null)
            {
                var asp = owner.getScreenAspect();
                viewer.createCameraMatrix(out p, out v, viewer.camera.Now, asp);
                GetCharacterScreenPos(chr, out x, out y, p, v, pos);
            }
            else
            {
                x = y = 10000;
            }
        }

        internal override void SetEffectColor(MapCharacter selfChr, Color color)
        {
            var viewer = battle.battleViewer as BattleViewer3D;
            if (viewer == null)
                return;

            var actor = viewer.searchFromActors(selfChr);
            if (actor == null)
                return;

            actor.overRidedColor = color;
        }

        internal void start(Script.Trigger trigger)
        {
            foreach (var runner in runnerDic.getList().ToArray())
            {
                if (runner.state == ScriptRunner.ScriptState.Running)
                    runnerDic.bringToFront(runner);

                if (runner.Trigger == trigger)
                    runner.Run();
            }
        }

        internal bool isBusy()
        {
            // ステータス変動によるゲージアニメをここでやってしまう
            bool isUpdated = false;
            foreach (var player in battle.playerData)
            {
                isUpdated |= battle.UpdateBattleStatusData(player);
            }
            if (isUpdated)
            {
                battle.statusUpdateTweener.Update();
            }

            // メンバーチェンジを処理する
            if (procMemberChange())
                isUpdated = true;

            // コマンド指定を処理する
            if (battle.battleState == BattleSequenceManager.BattleState.Wait)
            {
                foreach (var action in actionQueue)
                {
                    if (action.type == Script.Command.FuncType.BTL_ACTION)
                    {
                        procSetAction(action);
                    }
                }
                actionQueue.Clear();
            }

            foreach (var runner in runnerDic.getList())
            {
                if (!runner.isParallelTriggers() &&
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
            if (isUpdated)
                return true;

            return false;
        }

        private void procSetAction(Script.Command curCommand)
        {
            int cur = 0;
            var tgt = getTargetData(curCommand, ref cur);
            if (tgt == null)
                return;

            BattleCommand cmd = new BattleCommand();
            switch (curCommand.attrList[cur++].GetInt())
            {
                case 0:
                    cmd.type = BattleCommand.CommandType.ATTACK;
                    cmd.power = 100;
                    tgt.selectedBattleCommandType = BattleCommandType.Attack;
                    tgt.selectedBattleCommand = cmd;
                    battle.battleViewer.commandTargetSelector.Clear();
                    tgt.targetCharacter = battle.GetTargetCharacters(tgt);
                    break;
                case 1:
                    cmd.type = BattleCommand.CommandType.GUARD;
                    cmd.power = curCommand.attrList[cur++].GetInt();
                    tgt.selectedBattleCommandType = BattleCommandType.Guard;
                    tgt.selectedBattleCommand = cmd;
                    tgt.targetCharacter = battle.GetTargetCharacters(tgt);
                    break;
                case 2:
                    cmd.type = BattleCommand.CommandType.CHARGE;
                    cmd.power = curCommand.attrList[cur++].GetInt();
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
                    cmd.type = BattleCommand.CommandType.SKILL;
                    cmd.guId = curCommand.attrList[cur++].GetGuid();
                    tgt.selectedBattleCommandType = BattleCommandType.Skill;
                    tgt.selectedSkill = owner.catalog.getItemFromGuid<Skill>(cmd.guId);
                    tgt.selectedBattleCommand = cmd;
                    battle.battleViewer.commandTargetSelector.Clear();
                    if (tgt.selectedSkill != null)
                        tgt.targetCharacter = battle.GetTargetCharacters(tgt);
                    break;
            }

            if (tgt is BattlePlayerData)
                ((BattlePlayerData)tgt).forceSetCommand = true;
        }

        private bool procMemberChange()
        {
            // まだフェード中だったら次の処理をしない
            if (!battle.battleViewer.IsFadeEnd)
                return true;

            var viewer = battle.battleViewer as BattleViewer3D;
            if (viewer != null)
            {
                // まだ移動中だったら次の処理をしない
                foreach (var actor in viewer.friends)
                {
                    if (actor == null)
                        continue;

                    if (actor.mapChr.IsMoving())
                        return true;
                }
            }

            // キューが空っぽだったら何もしない
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
            var tgt = battle.enemyMonsterData.FirstOrDefault(x => x.UniqueID == entry.idx);
            if (tgt != null)
            {
                foreach (var chr in battle.playerData)
                {
                    chr.enemyPartyMember.Remove(tgt);
                }

                foreach (var chr in battle.enemyMonsterData)
                {
                    chr.friendPartyMember.Remove(tgt);
                }

                battle.battleViewer.AddFadeOutCharacter(tgt);
            }

            var data = battle.addEnemyData(entry.guid, entry.layout, entry.idx);
            data.friendPartyMember.AddRange(battle.enemyMonsterData.Where(x => x != data).Cast<BattleCharacterBase>());
            data.enemyPartyMember.AddRange(battle.playerData.Cast<BattleCharacterBase>());
            data.imageAlpha = 0;
            battle.battleViewer.AddFadeInCharacter(data);
            if (viewer != null)
            {
                var actor = viewer.AddEnemyMember(data, entry.idx, entry.layout.HasValue);
                actor.queueActorState(BattleActor.ActorStateType.APPEAR);
            }

            foreach (var chr in battle.playerData)
            {
                chr.enemyPartyMember.Add(data);
            }

            foreach (var chr in battle.enemyMonsterData)
            {
                chr.friendPartyMember.Add(data);
            }

            battle.battleViewer.refreshLayout(battle.playerData, battle.enemyMonsterData);
            return true;
        }

        private bool addRemoveParty(MemberChangeData entry, BattleViewer3D viewer)
        {
            var tgt = searchPartyFromGuid(entry.guid);
            bool doRefreshLayout = false;

            if (entry.cmd == MemberChangeData.Command.ADD)
            {
                // 4人以上いたら加入できない
                if (battle.playerData.Count >= 4)
                    return true;

                // 追加
                if (tgt != null)    // 既に加入していたら何もしない
                    return true;

                var hero = owner.data.party.GetHero(entry.guid);
                var data = battle.addPlayerData(hero);
                battle.SetBattleStatusData(data);
                data.enemyPartyMember.AddRange(battle.enemyMonsterData.Cast<BattleCharacterBase>());
                data.friendPartyMember.AddRange(battle.playerData.Where(x => x != data).Cast<BattleCharacterBase>());
                data.imageAlpha = 0;
                if (viewer != null)
                {
                    BattleActor.party = owner.data.party;
                    var actor = viewer.AddPartyMember(data);
                    actor.queueActorState(BattleActor.ActorStateType.APPEAR);
                    actor.mapChr.z += 1;   // 一歩下げる
                }
                else
                {
                    battle.battleViewer.AddFadeInCharacter(data);
                }

                foreach (var chr in battle.playerData)
                {
                    chr.friendPartyMember.Add(data);
                }

                foreach (var chr in battle.enemyMonsterData)
                {
                    chr.enemyPartyMember.Add(data);
                }

                doRefreshLayout = true;
            }
            else if (entry.cmd == MemberChangeData.Command.SET_WAIT)
            {
                if (tgt == null)    // いないメンバーは外せない
                    return true;

                if (viewer != null)
                {
                    var actor = viewer.searchFromActors(tgt);
                    actor.queueActorState(BattleActor.ActorStateType.WAIT);
                }
            }
            else if (entry.cmd == MemberChangeData.Command.FADE_OUT)
            {
                // 0人になるような場合は外せない
                if (battle.playerData.Count < 2)
                    return true;

                // 外す
                if (tgt == null)    // いないメンバーは外せない
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
                if (battle.playerData.Count < 2)
                    return true;

                // 外す
                if (tgt == null)    // いないメンバーは外せない
                    return true;

                // 詰める アンド アクター解放
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
                    for (int i = removeIndex; i < viewer.friends.Length - 1; i++)
                    {
                        viewer.friends[i] = viewer.friends[i + 1];
                        viewer.friends[i + 1] = null;
                    }
                    viewer.friends[viewer.friends.Length - 1] = null;
                    actor.Release();
                }

                var data = tgt as BattlePlayerData;

                data.selectedBattleCommandType = BattleCommandType.Skip;

                battle.playerData.Remove(data);

                foreach (var chr in battle.playerData)
                {
                    chr.friendPartyMember.Remove(tgt);
                }

                foreach (var chr in battle.enemyMonsterData)
                {
                    chr.enemyPartyMember.Remove(tgt);
                }

                // GameDataに状態を反映する
                battle.ApplyPlayerDataToGameData(data);
                doRefreshLayout = true;
            }

            if (doRefreshLayout)
            {
                int index = 0;
                foreach (var chr in battle.playerData)
                {
                    // 3Dバトル用整列処理
                    if (viewer != null)
                    {
                        // 人が増減した場合に対応して正しい位置に移動してやる
                        var neutralPos = BattleCharacterPosition.getPosition(BattleViewer3D.CenterOfField,
                            BattleCharacterPosition.PosType.FRIEND, index, battle.playerData.Count);
                        neutralPos.X = (int)neutralPos.X;
                        neutralPos.Y = (int)neutralPos.Y;
                        var actor = viewer.searchFromActors(chr);
                        if (!actor.isActorStateQueued(BattleActor.ActorStateType.DESTROY))
                            actor.walk(neutralPos.X, neutralPos.Y);
                    }
                    else
                    {
                        chr.calcHeroLayout(index);
                    }

                    index++;
                }

                // 2Dバトル用整列処理
                battle.battleViewer.refreshLayout(battle.playerData, battle.enemyMonsterData);
            }

            return true;
        }

        internal void checkAllSheet(MapCharacter mapChr, bool inInitialize, bool applyScript, bool applyGraphic)
        {
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
                            ok = data.system.GetSwitch(cond.index, rom.guId) == (cond.option == 0 ? true : false);
                            break;
                        case Common.Rom.Event.Condition.Type.COND_TYPE_VARIABLE:
                            ok = MapEngine.checkCondition(data.system.GetVariable(cond.index, rom.guId), cond.option, cond.cond);
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
                            if (cond.option != 0)
                                ok = !ok;
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
                if (destPage >= 0)
                {
                    var sheet = rom.sheetList[destPage];

                    if(applyGraphic)
                    {
                        var image = catalog.getItemFromGuid(sheet.graphic) as Common.Resource.ResourceItem;
                        changeCharacterGraphic(mapChr, image);
                        mapChr.playMotion(sheet.graphicMotion, inInitialize ? 0 : 0.2f);
                        mapChr.setDirection(sheet.direction, nowPage < 0);
                    }

                    if (applyScript)
                    {
                        // 前回登録していた Script を RunnerDic から外す
                        if (nowPage >= 0)
                        {
                            var guid = rom.sheetList[nowPage].script;
                            if (runnerDic.ContainsKey(guid))
                            {
                                if (runnerDic[guid].state != ScriptRunner.ScriptState.Running)
                                {
                                    runnerDic[guid].finalize();
                                    runnerDic.Remove(guid);
                                }
                                else
                                {
                                    if (runnerDic[guid].Trigger == Common.Rom.Script.Trigger.PARALLEL)
                                        runnerDic[guid].removeTrigger = ScriptRunner.RemoveTrigger.ON_COMPLETE_CURRENT_LINE;
                                    else
                                        runnerDic[guid].removeTrigger = ScriptRunner.RemoveTrigger.ON_EXIT;
                                }
                            }
                        }
                        if (sheet.script != Guid.Empty)
                        {
                            // 付随するスクリプトを RunnerDic に登録する
                            var script = catalog.getItemFromGuid(sheet.script) as Common.Rom.Script;
                            if (script != null)
                            {
                                mapChr.expand = script.expandArea;
                                if (script.commands.Count > 0)
                                {
                                    var runner = new ScriptRunner(this, mapChr, script);

                                    // 自動的に開始(並列)が removeOnExit状態で残っている可能性があるので、関係ないGUIDに差し替える
                                    if (runnerDic.ContainsKey(sheet.script))
                                    {
                                        var tmp = runnerDic[sheet.script];
                                        tmp.key = Guid.NewGuid();
                                        runnerDic.Remove(sheet.script);
                                        runnerDic.Add(tmp.key, tmp);
                                    }

                                    // 辞書に登録
                                    runnerDic.Add(sheet.script, runner);

                                    // 自動的に開始の場合はそのまま開始する
                                    if (script.trigger == Common.Rom.Script.Trigger.AUTO ||
                                        script.trigger == Common.Rom.Script.Trigger.AUTO_REPEAT ||
                                        script.trigger == Common.Rom.Script.Trigger.PARALLEL ||
                                        script.trigger == Common.Rom.Script.Trigger.PARALLEL_MV ||
                                        script.trigger == Common.Rom.Script.Trigger.BATTLE_PARALLEL)
                                        runner.Run();
                                }
                            }
                        }
                    }
                }
                // 遷移先のページがない場合
                else
                {
                    // 前回登録していた Script を RunnerDic から外す
                    if (nowPage >= 0)
                    {
                        if (applyGraphic)
                        {
                            changeCharacterGraphic(mapChr, null);
                        }

                        if (applyScript)
                        {
                            var guid = rom.sheetList[nowPage].script;
                            if (runnerDic.ContainsKey(guid))
                            {
                                if (runnerDic[guid].state != ScriptRunner.ScriptState.Running)
                                {
                                    runnerDic[guid].finalize();
                                    runnerDic.Remove(guid);
                                }
                                else
                                {
                                    if (runnerDic[guid].Trigger == Common.Rom.Script.Trigger.PARALLEL ||
                                        runnerDic[guid].Trigger == Common.Rom.Script.Trigger.PARALLEL_MV)
                                        runnerDic[guid].removeTrigger = ScriptRunner.RemoveTrigger.ON_COMPLETE_CURRENT_LINE;
                                    else
                                        runnerDic[guid].removeTrigger = ScriptRunner.RemoveTrigger.ON_EXIT;
                                }
                            }
                        }
                    }
                }

                mapChr.currentPage = destPage;
            }
        }

        internal void changeCharacterGraphic(MapCharacter mapChr, Common.Resource.ResourceItem image)
        {
            var v3d = battle.battleViewer as BattleViewer3D;

            if (v3d == null)
                return;

            // グラフィックは違ってた場合だけ変える(Resetが重いので)
            if (image != mapChr.character)
            {
                if (image != null)
                {
                    mapChr.ChangeGraphic(image, v3d.drawer);
                }
                else
                {
                    mapChr.ChangeGraphic(null, v3d.drawer);
                }
            }
        }

        private int getBattlePhaseForCondition()
        {
            if (battle.battleState <= BattleSequenceManager.BattleState.BattleStart)
                return 0;
            if (battle.battleState >= BattleSequenceManager.BattleState.StartBattleFinishEvent)
                return 2;

            return 1;
        }

        internal void showBattleUi(bool v)
        {
            battleUiVisibility = v;
        }

        internal void healBattleCharacter(Script.Command curCommand, Guid evGuid)
        {
            int cur = 0;
            var guid = curCommand.attrList[cur].GetGuid();
            var idx = curCommand.attrList[cur++].GetInt();
            var tgtIsMp = curCommand.attrList[cur++].GetBool();
            var value = curCommand.attrList[cur++].GetInt();
            var varIdx = value;
            var invert = curCommand.attrList[cur++].GetInt();
            if (invert != 0)
                value *= -1;
            BattleCharacterBase chr = null;
            bool showDamage = false;
            if (curCommand.attrList.Count > cur)
            {
                // タイプ？
                switch (curCommand.attrList[cur++].GetInt())
                {
                    case 0:
                        chr = searchPartyFromGuid(guid);
                        break;
                    case 1:
                        var ptIdx = idx - 1;
                        if (battle.playerData.Count > ptIdx && battle.playerData[ptIdx] != null)
                            chr = battle.playerData[ptIdx];
                        break;
                    case 2:
                        var mobIdx = idx;
                        chr = battle.enemyMonsterData.FirstOrDefault(x => x.UniqueID == mobIdx);
                        break;
                }

                // 変数？
                if (curCommand.attrList[cur++].GetBool())
                {
                    value = owner.data.system.GetVariable(varIdx, evGuid);
                    if (invert >= 2)
                        value *= -1;
                }

                showDamage = curCommand.attrList[cur++].GetBool();
            }
            else
            {
                chr = battle.enemyMonsterData.FirstOrDefault(x => x.UniqueID == idx);
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
                        if (chr.Status == Common.GameData.Hero.StatusAilments.DOWN)
                        {
                            if (chr is BattleEnemyData)
                            {
                                battle.battleViewer.AddFadeInCharacter(chr);
                            }
                            chr.RecoveryStatusAilment(Common.GameData.Hero.StatusAilments.DOWN);
                        }
                    }
                    else if(chr.HitPoint == 0)
                    {
                        if (chr.Status != Common.GameData.Hero.StatusAilments.DOWN)
                        {
                            if (chr is BattleEnemyData)
                            {
                                battle.battleViewer.AddFadeOutCharacter(chr);
                                Audio.PlaySound(owner.se.defeat);
                            }
                            chr.SetStatusAilment(Common.GameData.Hero.StatusAilments.DOWN);
                        }
                    }
                }

                if (chr is BattlePlayerData)
                {
                    ((BattlePlayerData)chr).battleStatusData.MagicPoint = chr.MagicPoint;
                    battle.SetNextBattleStatus((BattlePlayerData)chr);
                    battle.statusUpdateTweener.Begin(0, 1.0f, 30);
                }
            }

            if(showDamage)
            {
                string text = value.ToString();
                if (value < 0)
                {
                    text = (-value).ToString();
                }

                battle.battleViewer.AddDamageTextInfo(new BattleDamageTextInfo(textType, chr, text));
            }
        }

        internal void healParty(Script.Command curCommand)
        {
            int cur = 0;
            var heroGuid = curCommand.attrList[cur++].GetGuid();
            var tgtIsMp = curCommand.attrList[cur++].GetBool();
            var value = curCommand.attrList[cur++].GetInt();
            var invert = curCommand.attrList[cur++].GetBool();
            if (invert)
                value *= -1;

            foreach (var chr in battle.playerData)
            {
                if (chr.player.rom.guId != heroGuid)
                    continue;

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

                    if (chr.HitPoint > 0 && (chr.Status & Common.GameData.Hero.StatusAilments.DOWN) > 0)
                        chr.RecoveryStatusAilment(Common.GameData.Hero.StatusAilments.DOWN);
                    else if (chr.HitPoint == 0)
                        chr.SetStatusAilment(Common.GameData.Hero.StatusAilments.DOWN);
                }

                chr.battleStatusData.MagicPoint = chr.MagicPoint;
                battle.SetNextBattleStatus(chr);
                battle.statusUpdateTweener.Begin(0, 1.0f, 30);

                break;
            }
        }

        internal MapCharacter getBattleActorMapChr(Script.Command curCommand)
        {
            int cur = 0;
            var tgt = getTargetData(curCommand, ref cur);

            var viewer = battle.battleViewer as BattleViewer3D;
            if (viewer == null)
                return null;

            var actor = viewer.searchFromActors(tgt);
            if (actor == null)
                return null;

            return actor.mapChr;
        }

        private BattleCharacterBase getTargetData(Script.Command rom, ref int cur)
        {
            BattleCharacterBase result = null;
            switch (rom.attrList[cur++].GetInt())
            {
                case 0:
                    var heroGuid = rom.attrList[cur++].GetGuid();
                    result = searchPartyFromGuid(heroGuid);
                    break;
                case 1:
                    var ptIdx = rom.attrList[cur++].GetInt() - 1;
                    if (battle.playerData.Count > ptIdx && battle.playerData[ptIdx] != null)
                        result = battle.playerData[ptIdx];
                    break;
                case 2:
                    var mobIdx = rom.attrList[cur++].GetInt();
                    result = battle.enemyMonsterData.FirstOrDefault(x => x.UniqueID == mobIdx);
                    break;
            }
            return result;
        }

        private BattleCharacterBase searchPartyFromGuid(Guid heroGuid)
        {
            foreach (var chr in battle.playerData)
            {
                if (chr.player.rom.guId != heroGuid)
                    continue;

                return chr;
            }

            return null;
        }

        internal void setNextAction(Script.Command curCommand)
        {
            int cur = 0;
            var tgt = getTargetData(curCommand, ref cur);
            if (tgt == null)
                return;

            // すでに同じ対象のものがキューに入っていたら、まず外す
            actionQueue.RemoveAll(x =>
            {
                int cur2 = 0;
                var tgt2 = getTargetData(x, ref cur2);
                return tgt == tgt2 && x.type == curCommand.type;
            });

            actionQueue.Add(curCommand);
        }

        internal void setBattleStatus(Script.Command curCommand, bool typeReduced = false)
        {
            int cur = 0;
            BattleCharacterBase tgt;
            if (typeReduced)
                tgt = searchPartyFromGuid(curCommand.attrList[cur++].GetGuid());
            else
                tgt = getTargetData(curCommand, ref cur);

            if (tgt == null)
                return;

            int value = curCommand.attrList[cur++].GetInt();
            bool add = curCommand.attrList[cur++].GetInt() == 0;

            var typeList = new Common.GameData.Hero.StatusAilments[]
            {
                Common.GameData.Hero.StatusAilments.POISON,
                Common.GameData.Hero.StatusAilments.SLEEP,
                Common.GameData.Hero.StatusAilments.PARALYSIS,
                Common.GameData.Hero.StatusAilments.CONFUSION,
                Common.GameData.Hero.StatusAilments.FASCINATION,
                Common.GameData.Hero.StatusAilments.DOWN,
            };
            var type = typeList[value];

            if (typeReduced && type == Common.GameData.Hero.StatusAilments.SLEEP)
                type = Common.GameData.Hero.StatusAilments.DOWN;

            if (add)
            {
                if (type != Common.GameData.Hero.StatusAilments.DOWN)
                {
                    if (!tgt.Status.HasFlag(Common.GameData.Hero.StatusAilments.DOWN))
                    {
                        tgt.Status |= type;
                    }
                }
                else
                {
                    tgt.HitPoint = 0;
                    tgt.Status |= type;
                    battle.SetNextBattleStatus(tgt as BattlePlayerData);
                    if (tgt is BattleEnemyData)
                        battle.battleViewer.AddFadeOutCharacter(tgt);
                }
            }
            else
            {
                if (type != Common.GameData.Hero.StatusAilments.DOWN)
                {
                    tgt.Status &= ~type;
                }
                else if (tgt.Status.HasFlag(Common.GameData.Hero.StatusAilments.DOWN))
                {
                    tgt.HitPoint = 1;
                    battle.SetNextBattleStatus(tgt as BattlePlayerData);
                    if (tgt is BattleEnemyData)
                        battle.battleViewer.AddFadeInCharacter(tgt);
                }
            }

            battle.statusUpdateTweener.Begin(0, 1.0f, 30);

            if (tgt is BattlePlayerData)
                battle.battleViewer.SetPlayerStatusEffect((BattlePlayerData)tgt);
        }

        internal MemberChangeData addMonster(Script.Command curCommand)
        {
            int curAttr = 0;
            var idx = curCommand.attrList[curAttr++].GetInt();
            var guid = curCommand.attrList[curAttr++].GetGuid();

            if (catalog.getItemFromGuid(guid) == null)
                return new MemberChangeData() { finished = true };

            var useLayout = curCommand.attrList[curAttr++].GetBool();
            Point? layout = null;
            if(useLayout)
            {
                layout = new Point((int)curCommand.attrList[curAttr++].GetFloat(), (int)curCommand.attrList[curAttr++].GetFloat());
            }

            Console.WriteLine("appear : " + idx);
            var result = new MemberChangeData() { mob = true, cmd = MemberChangeData.Command.ADD, idx = idx, guid = guid, layout = layout };
            memberChangeQueue.Enqueue(result);
            return result;
        }

        internal void battleStop()
        {
            if (battle.BattleResult == BattleSequenceManager.BattleResultState.NonFinish)
                battle.battleState = BattleSequenceManager.BattleState.StopByEvent;
        }

        internal void fullRecovery(bool poison = true, bool revive = true)
        {
            foreach (var chr in battle.playerData)
            {
                if (revive || chr.HitPoint >= 1)
                {
                    chr.HitPoint = chr.MaxHitPoint;
                    chr.MagicPoint = chr.MaxMagicPoint;
                    if (poison)
                        chr.Status = Common.GameData.Hero.StatusAilments.NONE;

                    chr.battleStatusData.MagicPoint = chr.MagicPoint;
                    battle.SetNextBattleStatus(chr);
                }
            }

            battle.statusUpdateTweener.Begin(0, 1.0f, 30);
        }

        internal int getStatus(Script.Command.IfHeroSourceType srcTypePlus, int option, Common.GameData.Hero hero)
        {
            var tgt = searchPartyFromGuid(hero.rom.guId) as BattlePlayerData;
            if (tgt == null)
                return 0;

            switch (srcTypePlus)
            {
                case Script.Command.IfHeroSourceType.STATUS_AILMENTS:
                    if (
                        ((option == (int)Script.Command.IfHeroAilmentType.POISON) &&
                        (hero.statusAilments & Common.GameData.Hero.StatusAilments.POISON) != 0) ||
                        ((option == (int)Script.Command.IfHeroAilmentType.DOWN) &&
                        (hero.statusAilments & Common.GameData.Hero.StatusAilments.DOWN) != 0)
                    )
                        return 1;
                    else
                        return 0;
                case Script.Command.IfHeroSourceType.LEVEL:
                    return hero.level;
                case Script.Command.IfHeroSourceType.HITPOINT:
                    return tgt.HitPoint;
                case Script.Command.IfHeroSourceType.MAGICPOINT:
                    return tgt.MagicPoint;
                case Script.Command.IfHeroSourceType.ATTACKPOWER:
                    return tgt.Attack;
                case Script.Command.IfHeroSourceType.Defense:
                    return tgt.Defense; // TODO 体力を考慮
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

        internal void addStatus(Common.GameData.Hero hero, ScriptRunner.HeroStatusType type, int num)
        {
            var tgt = searchPartyFromGuid(hero.rom.guId) as BattlePlayerData;
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
                case ScriptRunner.HeroStatusType.Defense:
                    tgt.DefenseBase += num;
                    break;
                case ScriptRunner.HeroStatusType.SPEED:
                    tgt.SpeedBase += num;
                    break;
            }

            consistency(tgt);
            tgt.battleStatusData.MaxMagicPoint = tgt.MaxMagicPoint;
            battle.SetNextBattleStatus(tgt);
            battle.statusUpdateTweener.Begin(0, 1.0f, 30);
        }

        // パラメータの整合性を取るメソッド
        public void consistency(BattlePlayerData p)
        {
            // ほかのパラメータも0未満にはならない
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
            if (p.HitPoint <= 0)
            {
                p.HitPoint = 0;
                p.SetStatusAilment(Common.GameData.Hero.StatusAilments.DOWN);
            }
            else if (p.Status.HasFlag(Common.GameData.Hero.StatusAilments.DOWN))
            {
                p.RecoveryStatusAilment(Common.GameData.Hero.StatusAilments.DOWN);
            }
        }

        internal MemberChangeData addRemoveMember(Script.Command curCommand)
        {
            int curAttr = 0;
            var guid = curCommand.attrList[curAttr++].GetGuid();
            MemberChangeData lastItem = new MemberChangeData() { finished = true };

            var add = !curCommand.attrList[curAttr++].GetBool();
            if (add)
            {
                memberChangeQueue.Enqueue(new MemberChangeData() { cmd = MemberChangeData.Command.ADD, guid = guid });
                lastItem = new MemberChangeData() { cmd = MemberChangeData.Command.SET_WAIT, guid = guid };
                memberChangeQueue.Enqueue(lastItem);
            }
            else
            {
                memberChangeQueue.Enqueue(new MemberChangeData() { cmd = MemberChangeData.Command.FADE_OUT, guid = guid });
                lastItem = new MemberChangeData() { cmd = MemberChangeData.Command.REMOVE, guid = guid };
                memberChangeQueue.Enqueue(lastItem);
            }

            return lastItem;
        }

        internal void start(Guid commonExec)
        {
            var runner = GetScriptRunner(commonExec);
            if (runner != null)
            {
                runner.Run();

                // MapSceneから借りてきたrunnerは明示的にupdateするリストに入れてやる
                if (!mapRunnerBorrowed.Contains(runner))
                {
                    mapRunnerBorrowed.Add(runner);
                    runner.owner = this;
                }
            }
        }

        internal override ScriptRunner GetScriptRunner(Guid guid)
        {
            return owner.mapScene.runnerDic.getList()
                .Where(x => x.mapChr != null && x.mapChr.rom != null && x.mapChr.rom.guId == guid)
                .FirstOrDefault();
        }

        internal void RefreshHeroMapChr(Hero rom)
        {
            var viewer = battle.battleViewer as BattleViewer3D;
            if (viewer == null)
            {
                refreshFace();
                return;
            }

            // グラフィックが変わってたら適用する
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
                    var res = catalog.getItemFromGuid<Common.Resource.ResourceItem>(guid);
                    friend.mapChr.ChangeGraphic(res, viewer.drawer);
                }
            }

            // MapSceneにも伝播する
            UnityUtil.changeScene(UnityUtil.SceneType.MAP);
            owner.mapScene.RefreshHeroMapChr();
            UnityUtil.changeScene(UnityUtil.SceneType.BATTLE);
        }

        public override MapCharacter GetHeroForBattle(Hero rom = null)
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
            foreach (var friend in battle.playerData)
            {
                if (friend == null)
                    break;

                var pl = friend as BattlePlayerData;
                var guid = owner.data.party.getMemberFace(pl.player.rom);
                if (guid != pl.currentFace)
                {
                    var res = catalog.getItemFromGuid<Common.Resource.Face>(guid);
                    pl.setFaceImage(res);
                }
            }
        }

        internal override void setHeroName(Guid hero, string nextHeroName)
        {
            base.setHeroName(hero, nextHeroName);

            var tgt = searchPartyFromGuid(hero);
            if (tgt != null)
                tgt.Name = nextHeroName;
        }

        internal int getStatus(Script.Command.VarHeroSourceType srcTypePlus, Common.GameData.Hero hero)
        {
            var battleStatus = searchPartyFromGuid(hero.rom.guId);
            return ScriptRunner.getBattleStatus(battleStatus, srcTypePlus, battle.playerData);
        }

        internal int getPartyStatus(Script.Command.VarHeroSourceType srcTypePlus, int index)
        {
            if (battle.playerData.Count <= index)
                return 0;

            var battleStatus = battle.playerData[index];
            return ScriptRunner.getBattleStatus(battleStatus, srcTypePlus, battle.playerData);
        }

        internal int getPartyNumber()
        {
            return battle.playerData.Count;
        }

        internal int getEnemyStatus(Script.Command.VarHeroSourceType srcTypePlus, int index)
        {
            var battleStatus = battle.enemyMonsterData.FirstOrDefault(x => x.UniqueID == index);
            if (battleStatus == null)
                return 0;

            return ScriptRunner.getBattleStatus(battleStatus, srcTypePlus, battle.playerData);
        }

        internal Guid getEnemyGuid(int index)
        {
            var battleStatus = battle.enemyMonsterData.FirstOrDefault(x => x.UniqueID == index);
            if (battleStatus == null)
                return Guid.Empty;

            return battleStatus.monster.guId;
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
                foreach (var pl in battle.enemyMonsterData)
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
                foreach (var pl in battle.enemyMonsterData)
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

        internal bool MoveCharacter(MapCharacter chr, int dir, MapData map, bool runHit2Script, bool ignoreHeight = false)
        {
            var viewer = battle.battleViewer as BattleViewer3D;
            if (viewer == null)
                return true;

            var dx = 0;
            var dy = 0;

            if (dir == 3)
                dx = 1;
            else if (dir == 2)
                dx = -1;
            else if (dir == 1)
                dy = 1;
            else if (dir == 0)
                dy = -1;

            return chr.Walk(dx, dy, !chr.fixDirection, viewer.drawer, BattleActor.dummyColList, ignoreHeight, chr.collidable);
        }

        internal bool existMember(Guid heroGuid)
        {
            return searchPartyFromGuid(heroGuid) != null;
        }

        internal override void ReservationChangeScene(GameMain.Scenes scene)
        {
            if (scene == GameMain.Scenes.TITLE)
            {
                battle.battleState = BattleSequenceManager.BattleState.BattleFinishCheck1;
                reservedResult = BattleSequenceManager.BattleResultState.Escape_ToTitle;
            }
        }

        internal override void DoGameOver()
        {
            battle.battleState = BattleSequenceManager.BattleState.BattleFinishCheck1;
            reservedResult = BattleSequenceManager.BattleResultState.Lose_GameOver;
        }

        internal void checkForceBattleFinish(ref BattleSequenceManager.BattleResultState resultState)
        {
            if (reservedResult != BattleSequenceManager.BattleResultState.NonFinish)
            {
                resultState = reservedResult;
            }
        }

        internal void refreshEquipmentEffect(Common.GameData.Hero hero)
        {
            var data = searchPartyFromGuid(hero.rom.guId) as BattlePlayerData;

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

        internal bool isLocked()
        {
            return playerLocked >= PLAYER_LOCK_BY_EVENT;
        }

        internal void setBattleResult(BattleSequenceManager.BattleResultState battleResult)
        {
            switch (battleResult)
            {
                case BattleSequenceManager.BattleResultState.Win:
                    lastBattleResult = 1;
                    break;
                case BattleSequenceManager.BattleResultState.Lose_Advanced_GameOver:
                case BattleSequenceManager.BattleResultState.Lose_Continue:
                case BattleSequenceManager.BattleResultState.Lose_GameOver:
                    lastBattleResult = 2;
                    break;
                case BattleSequenceManager.BattleResultState.Escape:
                case BattleSequenceManager.BattleResultState.Escape_ToTitle:
                    lastBattleResult = 3;
                    break;
            }
        }
    }
}

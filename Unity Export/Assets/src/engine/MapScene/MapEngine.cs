
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yukar.Common.GameData;
using KeyStates = Yukar.Engine.Input.KeyStates;
using StateType = Yukar.Engine.Input.StateType;
using Yukar.Common;
using Microsoft.Xna.Framework;
using System.Collections;

namespace Yukar.Engine
{
    public struct IntPoint
    {
        public int x;
        public int y;
        public int r;
        public int Dir
        {
            set
            {
                switch (value)
                {
                    case 0: r = 2; break;
                    case 1: r = 0; break;
                    case 2: r = 3; break;
                    case 3: r = 1; break;
                }
            }
        }
    };
    public struct EventHeight
    {
        public int h;
        public bool col;
        public MapCharacter chr;
    };
    public class EventHeightMap
    {
        public EventHeightMap()
        {
            map = new EventHeight[0, 0][];
        }

        EventHeight[,][] map;
        public EventHeight[] get(int x, int y)
        {
            return map[x, y];
        }

        public int width { get { return map.GetLength(0); } }
        public int height { get { return map.GetLength(1); } }

        public void init(int width, int height, int capacity = 2)
        {
            map = new EventHeight[width, height][];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    map[x, y] = new EventHeight[capacity];
                }
            }
        }

        public void clear()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var cell = map[x, y];
                    for (int z = 0; z < cell.Length; z++)
                    {
                        cell[z].chr = null;
                    }
                }
            }
        }

        public void remove(int x, int y, MapCharacter chr)
        {
            var cell = map[x, y];
            for (int i = 0; i < cell.Length; i++)
            {
                if (cell[i].chr == chr)
                    cell[i].chr = null;
            }
        }

        public void add(MapCharacter ev, bool collidable, int h, int x, int y, bool doExpand = false)
        {
            var cell = map[x, y];

            int index = -1;
            for (int i = 0; i < cell.Length; i++)
            {
                if (cell[i].chr == null)
                {
                    index = i;
                    break;
                }
            }
            if (index < 0)
            {
                index = cell.Length;
                cell = new EventHeight[index * 2];
                map[x, y].CopyTo(cell, 0);
                map[x, y] = cell;
            }
            
            cell[index].h = h;
            cell[index].col = collidable;
            cell[index].chr = ev;

            // 判定拡張するイベントだった時は周囲に判定を拡張する
            if (doExpand)
            {
                for (int i = 0; i < 4; i++)
                {
                    int ox = x; int oy = y;
                    switch (i)
                    {
                        case 0: ox--; break;
                        case 1: ox++; break;
                        case 2: oy--; break;
                        case 3: oy++; break;
                    }
                    if (ox < 0 || ox >= width || oy < 0 || oy >= height) continue;

                    add(ev, false, height, ox, oy);
                }
            }
        }
    };

    public partial class MapEngine
    {
        public MapScene owner;

        private Color nowScreenColor;
        private AudioCore.SoundDef nowBgmSound;
        private Common.Resource.Bgs nowBgsSound;
        private Guid[] nowEnemies;
        private EffectDrawer poisonEffect;

        // エンカウント用情報
        public int restStep;
#if ENABLE_VR
        float dx = 0, dy = 0;
        int dir = 0;
#else	// #if ENABLE_VR
        int dx = 0, dy = 0, dir = 0;
#endif	// #if ENABLE_VR
        float lastPlayerX, lastPlayerZ;
        KeyStates dirKey = KeyStates.NONE;

        // アナログスティックを用いた際のしきい値
        const float DEAD_ZONE = 0.50f * 0.50f;

        // マップオブジェクトを使ったイベントの配置状況を示すマップ
        EventHeightMap eventHeightMap;

        static private bool cursorVisible = true;
        internal bool IsCursorVisible() { return cursorVisible; }

        internal IEnumerator LoadMapCharacter()
        {
            var s = owner;
            var catalog = s.owner.catalog;

            // エンカウント情報を取得
            genEncountStep();

            // マップ中の全キャラクターを追加
            int idx = 0;
            while (true)
            {
                var evRef = s.map.getEvent(idx);
                idx++;
                if (evRef == null) break;
                var ev = catalog.getItemFromGuid(evRef.guId) as Common.Rom.Event;
                if (ev == null) break;
                var mapChr = new MapCharacter(ev);
                mapChr.setPosition(evRef.x, evRef.y);
                mapChr.setDirection(ev.Direction, true, true);
                s.mapCharList.Add(mapChr);

                // どのイベントシートかを確定させる
                checkAllSheet(mapChr, true);

                yield return null;
            }

            updateEventHeightMap();
        }

        internal void LoadCommonEvents()
        {
            var s = owner;
            var catalog = s.owner.catalog;

            // 全コモンイベントを追加
            foreach (var guid in catalog.getGameSettings().commonEvents)
            {
                var ev = catalog.getItemFromGuid(guid) as Common.Rom.Event;
                if (ev == null) break;
                var mapChr = new MapCharacter(ev);
                mapChr.setPosition(-1, -1);
                mapChr.collidable = false;
                mapChr.hide |= MapCharacter.HideCauses.BY_EVENT;
                mapChr.isCommonEvent = true;
                s.mapCharList.Add(mapChr);

                // 条件なしのコモンイベントはトリガーがTALKになっているので、NONEにする
                // あと、グラフィックもなしにする
                ev.sheetList.ForEach(sheet =>
                {
                    sheet.graphic = Guid.Empty;

                    var script = owner.owner.catalog.getItemFromGuid(sheet.script) as Common.Rom.Script;
                    if (script != null && script.trigger == Common.Rom.Script.Trigger.TALK)
                        script.trigger = Common.Rom.Script.Trigger.NONE;
                });

                // どのイベントシートかを確定させる
                checkAllSheet(mapChr, true);
            }
        }

        internal void checkAllSheet(Guid eventGuid)
        {
            var mapChr = owner.mapCharList.FirstOrDefault(x => (x.rom == null ? Guid.Empty : x.rom.guId) == eventGuid);
            if (mapChr != null)
                checkAllSheet(mapChr);
        }

        internal void checkAllSheet(MapCharacter mapChr, bool inInitialize = false)
        {
            var mapdata = owner.mapDrawer;
            var s = owner;
            var catalog = s.owner.catalog;
            var data = s.owner.data;
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
                            ok = checkCondition(data.system.GetVariable(cond.index, rom.guId), cond.option, cond.cond);
                            break;
                        case Common.Rom.Event.Condition.Type.COND_TYPE_MONEY:
                            ok = checkCondition(data.party.GetMoney(), cond.option, cond.cond);
                            break;
                        case Common.Rom.Event.Condition.Type.COND_TYPE_ITEM:
                            ok = checkCondition(data.party.GetItemNum(cond.refGuid), cond.option, cond.cond);
                            break;
                        case Common.Rom.Event.Condition.Type.COND_TYPE_ITEM_WITH_EQUIPMENT:
                            ok = checkCondition(data.party.GetItemNum(cond.refGuid, true), cond.option, cond.cond);
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

                    var image = catalog.getItemFromGuid(sheet.graphic) as Common.Resource.ResourceItem;
                    changeCharacterGraphic(mapChr, image);

                    mapChr.playMotion(sheet.graphicMotion, inInitialize ? 0 : 0.2f);
                    mapChr.collidable = sheet.collidable;
                    mapChr.fixDirection = false;
                    mapChr.setDirection(sheet.direction, nowPage < 0);
                    mapChr.fixDirection = sheet.fixDirection;
                    mapChr.ChangeSpeed(sheet.moveSpeed);

                    // 前回登録していた Script を RunnerDic から外す
                    if (nowPage >= 0)
                    {
                        var guid = rom.sheetList[nowPage].script;
                        if (s.runnerDic.ContainsKey(guid))
                        {
                            if (s.runnerDic[guid].state != ScriptRunner.ScriptState.Running)
                            {
                                s.runnerDic[guid].finalize();
                                s.runnerDic.Remove(guid);
                            }
                            else
                            {
                                if (s.runnerDic[guid].Trigger == Common.Rom.Script.Trigger.PARALLEL)
                                    s.runnerDic[guid].removeTrigger = ScriptRunner.RemoveTrigger.ON_COMPLETE_CURRENT_LINE;
                                else
                                    s.runnerDic[guid].removeTrigger = ScriptRunner.RemoveTrigger.ON_EXIT;
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
                                var runner = new ScriptRunner(s, mapChr, script);

                                // 自動的に開始(並列)が removeOnExit状態で残っている可能性があるので、関係ないGUIDに差し替える
                                if (s.runnerDic.ContainsKey(sheet.script))
                                {
                                    var tmp = s.runnerDic[sheet.script];
                                    tmp.key = Guid.NewGuid();
                                    s.runnerDic.Remove(sheet.script);
                                    s.runnerDic.Add(tmp.key, tmp);
                                }

                                // 辞書に登録
                                s.runnerDic.Add(sheet.script, runner);

                                // 自動的に開始の場合はそのまま開始する
                                if (script.trigger == Common.Rom.Script.Trigger.AUTO ||
                                    script.trigger == Common.Rom.Script.Trigger.AUTO_REPEAT ||
                                    script.trigger == Common.Rom.Script.Trigger.PARALLEL ||
                                    script.trigger == Common.Rom.Script.Trigger.PARALLEL_MV)
                                    runner.Run();
                            }
                        }
                    }

                    // 移動タイプ指定がある場合は、その場でスクリプトを生成する
                    if (mapChr.moveScript != Guid.Empty)
                    {
                        s.runnerDic[mapChr.moveScript].finalize();
                        s.runnerDic.Remove(mapChr.moveScript);
                        mapChr.moveScript = Guid.Empty;
                    }
                    if (sheet.moveType != Common.Rom.Event.MoveType.NONE && !mapChr.isCommonEvent)
                    {
                        var movingLimits = sheet.getMovingLimits();
                        var x = (int)mapChr.x;
                        var z = (int)mapChr.z;
                        // 右
                        movingLimits[0] = x + movingLimits[0];
                        // 左
                        movingLimits[1] = x - movingLimits[1];
                        // 上
                        movingLimits[2] = z - movingLimits[2];
                        // 下
                        movingLimits[3] = z + movingLimits[3];
                        var motion = sheet.graphicMotion;
                        var script = Util.createMoveScript(sheet.moveType, sheet.moveTiming, movingLimits, motion);
                        var runner = new ScriptRunner(s, mapChr, script);
                        mapChr.moveScript = script.guId;
                        s.runnerDic.Add(script.guId, runner);
                        runner.Run();
                    }
                }
                // 遷移先のページがない場合
                else
                {
                    mapChr.ChangeGraphic(null, mapdata);
                    mapChr.collidable = false;
                    mapChr.expand = false;

                    // 前回登録していた Script を RunnerDic から外す
                    if (nowPage >= 0)
                    {
                        var guid = rom.sheetList[nowPage].script;
                        if (s.runnerDic.ContainsKey(guid))
                        {
                            if (s.runnerDic[guid].state != ScriptRunner.ScriptState.Running)
                            {
                                s.runnerDic[guid].finalize();
                                s.runnerDic.Remove(guid);
                            }
                            else
                            {
                                if (s.runnerDic[guid].Trigger == Common.Rom.Script.Trigger.PARALLEL ||
                                    s.runnerDic[guid].Trigger == Common.Rom.Script.Trigger.PARALLEL_MV)
                                    s.runnerDic[guid].removeTrigger = ScriptRunner.RemoveTrigger.ON_COMPLETE_CURRENT_LINE;
                                else
                                    s.runnerDic[guid].removeTrigger = ScriptRunner.RemoveTrigger.ON_EXIT;
                            }
                        }
                    }

                    // 自動移動スクリプトを RunnerDic から外す
                    if (mapChr.moveScript != Guid.Empty)
                    {
                        s.runnerDic.Remove(mapChr.moveScript);
                        mapChr.moveScript = Guid.Empty;
                    }
                }

                mapChr.currentPage = destPage;
            }
        }

        internal void changeCharacterGraphic(MapCharacter mapChr, Common.Resource.ResourceItem image)
        {
            // グラフィックは違ってた場合だけ変える(Resetが重いので)
            if (image != mapChr.character)
            {
                if (image != null)
                {
                    mapChr.ChangeGraphic(image, owner.mapDrawer);
                }
                else
                {
                    mapChr.ChangeGraphic(null, owner.mapDrawer);
                }
            }
        }

        static internal bool checkCondition(int a, int b, Common.Rom.Script.Command.ConditionType conditionType)
        {
            switch (conditionType)
            {
                case Common.Rom.Script.Command.ConditionType.EQUAL:
                    return a == b;
                case Common.Rom.Script.Command.ConditionType.NOT_EQUAL:
                    return a != b;
                case Common.Rom.Script.Command.ConditionType.EQUAL_GREATER:
                    return a >= b;
                case Common.Rom.Script.Command.ConditionType.EQUAL_LOWER:
                    return a <= b;
                case Common.Rom.Script.Command.ConditionType.GREATER:
                    return a > b;
                case Common.Rom.Script.Command.ConditionType.LOWER:
                    return a < b;
            }
            return false;
        }

        private void genEncountStep()
        {
            restStep = owner.GetRandom(owner.battleSetting.encountMaxSteps, owner.battleSetting.encountMinSteps);
        }

        internal void UpdatePlayer(MapData map, float yangle)
        {
            var s = owner;

            // 毒状態判定
            bool isPoison = false;
            foreach (var member in owner.owner.data.party.members)
            {
                if (member.statusAilments.HasFlag(Hero.StatusAilments.POISON))
                {
                    isPoison = true;
                }
            }
            if (isPoison)
            {
                if (poisonEffect == null)
                {
                    poisonEffect = new EffectDrawer();
                    poisonEffect.load(s.owner.catalog.getItemFromName("Poison", typeof(Common.Rom.Effect)) as Common.Rom.Effect, s.owner.catalog);
                    poisonEffect.initialize();
                }

                if (poisonEffect.isEndPlaying)
                    poisonEffect.initialize();
                if (!owner.isBattle)
                    poisonEffect.update(false, GameMain.getRelativeParam60FPS());
            }
            else if (poisonEffect != null)
            {
                poisonEffect.finalize();
                poisonEffect = null;
            }

            if (owner.CurrentCameraMode == Common.Rom.Map.CameraControlMode.NORMAL
#if ENABLE_GHOST_MOVE
                || owner.CurrentCameraMode == Common.Rom.Map.CameraControlMode.GHOST
#endif	// #if ENABLE_GHOST_MOVE
                )
            {
                if (owner.owner.catalog.getGameSettings().gridMode == Common.Rom.GameSettings.GridMode.SNAP)
                {
                    procPlayerForLegacy(map, yangle, isPoison);
                }
                else
                {
                    procPlayerForFreeMove(isPoison);
                    s.hero.updatePosAngle(yangle, 0);
                }
            }
            else if (owner.owner.catalog.getGameSettings().controlMode == Common.Rom.GameSettings.ControlMode.DUNGEON_RPG_LIKE)
                procPlayerForLegacy(map, yangle, isPoison);
            else
                procPlayerForFirstPerson(isPoison);

            // 隊列も更新する
            updateFollowers();

            // 戦闘終了判定
            if (owner.isBattle && (owner.battleSequenceManager.BattleResult != BattleSequenceManager.BattleResultState.NonFinish && !owner.battleSequenceManager.IsPlayingBattleEffect))
            {
                ResumeMapBGM();
                owner.SetScreenColor(nowScreenColor);
                owner.EndBattleMode();
            }
        }

        private void procPlayerForFreeMove(bool isPoison)
        {
            var s = owner;

            float moveX = 0;
            float moveY = 0;

            double yangle;
#if ENABLE_VR
            if (SharpKmyVr.Func.IsReady())
            {
                yangle = SharpKmyVr.Func.GetHmdRotateY() + owner.GetVrCameraData().GetCombinedOffsetRotateY();
            }
            else
            {
                yangle = s.yangle / 180 * Math.PI;
            }
#else
            yangle = s.yangle / 180 * Math.PI;
#endif

            // 入力を受け付けて主人公の位置を更新する
            bool isEncount = false;
            if (s.playerLocked == 0 && !owner.isBattle && !owner.IsMapChangedFrame)
            {
                bool hitcollisionEvent = false;

                checkEncountAndRunNotCollidableEvent(ref isEncount, ref hitcollisionEvent);

                if (!isEncount)
                {
                    var axisX = Input.GetAxis("PAD_X");
                    var axisY = Input.GetAxis("PAD_Y");

                    var axisLength = (axisX * axisX) + (axisY * axisY);

                    if (axisLength < DEAD_ZONE)
                    {
                        axisX = 0;
                        axisY = 0;

                        if (Input.KeyTest(StateType.DIRECT, KeyStates.UP))
                        {
                            axisY += 1;
                        }
                        if (Input.KeyTest(StateType.DIRECT, KeyStates.DOWN))
                        {
                            axisY -= 1;
                        }
                        if (Input.KeyTest(StateType.DIRECT, KeyStates.LEFT))
                        {
                            axisX -= 1;
                        }
                        if (Input.KeyTest(StateType.DIRECT, KeyStates.RIGHT))
                        {
                            axisX += 1;
                        }

                        if (axisX != 0 || axisY != 0)
                            axisLength = 1;
                    }

                    if (axisLength >= DEAD_ZONE)
                    {
                        var axis = new Vector2(axisX, axisY);
                        axis.Normalize();

                        moveX += s.hero.mMoveStep * axis.X * (float)Math.Cos(yangle) * GameMain.getRelativeParam60FPS();
                        moveY -= s.hero.mMoveStep * axis.X * (float)Math.Sin(yangle) * GameMain.getRelativeParam60FPS();
                        moveX -= s.hero.mMoveStep * axis.Y * (float)Math.Sin(yangle) * GameMain.getRelativeParam60FPS();
                        moveY -= s.hero.mMoveStep * axis.Y * (float)Math.Cos(yangle) * GameMain.getRelativeParam60FPS();
                    }

                    // 移動量が限りなく0に近いけど0じゃない場合に当たり判定の不具合のもとになるので, 丸め処理を行う
                    if (Math.Abs(moveX) < 1.0E-10) moveX = 0;
                    if (Math.Abs(moveY) < 1.0E-10) moveY = 0;

                    if (Input.KeyTest(StateType.DIRECT, KeyStates.DASH) && owner.owner.data.system.dashAvailable)
                    {
                        s.hero.ChangeSpeed(owner.owner.data.system.moveSpeed + 2);
                    }
                    else
                    {
                        s.hero.ChangeSpeed(owner.owner.data.system.moveSpeed);
                    }

                    if ((moveX != 0 || moveY != 0) && !s.hero.IsMoving() && !hitcollisionEvent)
                    {
                        if (Input.KeyTest(StateType.DIRECT, KeyStates.DASH) && owner.owner.data.system.dashAvailable)
                            s.hero.playMotion("run");
                        else
                            s.hero.playMotion("walk");

                        // 動いた方を向かせる
                        var dir = (float)Math.Atan2(moveX, moveY);
                        s.hero.setDirectionFromRadian(dir);

                        dx = (int)(s.hero.x + moveX) - (int)s.hero.x;
                        dy = (int)(s.hero.z + moveY) - (int)s.hero.z;

                        // 俯瞰視点では斜め地点のイベントが接触反応する事はないので、
                        // 左右どちらにも移動量がある場合は、大きかった方を採用する
                        if (dx != 0 && dy != 0)
                        {
                            if (Math.Abs(moveX) > Math.Abs(moveY))
                                dy = 0;
                            else
                                dx = 0;
                        }

                        MapCharacter hitChr = null;
                        if (dx != 0 || dy != 0)
                            hitChr = checkCharCollision(s.hero, (int)dx, (int)dy);

                        if (hitChr == null || owner.owner.debugSettings.ignoreEvent)
                        {
                            var walkedMove = s.hero.Walk(moveX, moveY, false, s.mapDrawer, eventHeightMap, owner.owner.debugSettings.ignoreTerrainHeight, true, this);
                            if (walkedMove != Vector2.Zero)
                            {
                                if (dx != 0 || dy != 0)
                                {
                                    // マップ更新
                                    updateEventHeightMap();

                                    // 毒ダメージ
                                    if (isPoison)
                                    {
                                        foreach (var member in owner.owner.data.party.members)
                                        {
                                            if (member.statusAilments.HasFlag(Hero.StatusAilments.POISON))
                                            {
                                                member.hitpoint--;
                                                if (member.hitpoint <= 0)
                                                    member.hitpoint = 1;
                                            }
                                        }
                                    }

                                    // 毒の床？
                                    var info = s.mapDrawer.getTerrainInfo(s.map.getTerrainAttrib((int)s.hero.x, (int)s.hero.z));
                                    if (info != null && info.poison)
                                    {
                                        foreach (var member in owner.owner.data.party.members.Where(player => player.statusAilments != Hero.StatusAilments.DOWN))
                                        {
                                            member.statusAilments |= Hero.StatusAilments.POISON;
                                        }
                                    }
                                }
                            }

                            // 移動できなかった場合、当たり判定のあるイベントがその地点に有るかどうか調べる(高低差無視がついていれば実行される
                            {
                                var isNotWakedMoveX = (walkedMove.X == 0 && walkedMove.X != moveX);
                                var isNotWakedMoveY = (walkedMove.Y == 0 && walkedMove.Y != moveY);
                                if (isNotWakedMoveX)
                                {
                                    dx = (moveX == 0) ? 0 : (0 < moveX) ? 1 : -1;
                                    dy = 0;

                                    hitChr = getOtherChacaterFromMap(s.hero, (int)dx, (int)dy, true);
                                    if (hitChr != null && hitChr.collidable)
                                    {
                                        if (runEvent(hitChr))
                                            s.hero.playMotion("wait");
                                    }
                                }
                                if (isNotWakedMoveY)
                                {
                                    dx = 0;
                                    dy = (moveY == 0) ? 0 : (0 < moveY) ? 1 : -1;

                                    hitChr = getOtherChacaterFromMap(s.hero, (int)dx, (int)dy, true);
                                    if (hitChr != null && hitChr.collidable)
                                    {
                                        if (runEvent(hitChr) && s.hero.isChangeMotionAvailable())
                                            s.hero.playMotion("wait");
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (runEvent(hitChr) && s.hero.isChangeMotionAvailable())
                                s.hero.playMotion("wait");
                        }
                    }
                    else if (s.hero.isChangeMotionAvailable())
                    {
                        s.hero.playMotion("wait");
                    }

                    // 話しかける
                    if (Input.KeyTest(StateType.TRIGGER, KeyStates.DECIDE) &&
                        checkAndRunTalkableScript() && s.hero.isChangeMotionAvailable())
                    {
                        s.hero.playMotion("wait");
                    }
                }
            }
        }

#if WINDOWS
        Point oldPos;
#endif
        private void procPlayerForFirstPerson(bool isPoison)
        {
            var s = owner;

#if ENABLE_VR
#if WINDOWS
            if (SharpKmy.Entry.isWindowActive() && !s.mapFixCamera && !s.owner.isDebugWindowFocused() && !s.isBattle && !SharpKmyVr.Func.IsReady())
#else
            if (SharpKmy.Entry.isWindowActive() && !s.mapFixCamera && !s.owner.isDebugWindowFocused() && !s.isBattle && !SharpKmyVr.Func.IsReady()
            && Yukar.Engine.MenuController.state == Yukar.Engine.MenuController.State.HIDE)
#endif
#else   // #if ENABLE_VR
            if (SharpKmy.Entry.isWindowActive() && !s.isPlayerLockedByEvent() && !s.mapFixCamera && !s.owner.isDebugWindowVisible() && !s.isBattle)
#endif  // #if ENABLE_VR
            {
#if WINDOWS || UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
                // マウス移動量を取得
                setCursorVisibility(false, true);
#if WINDOWS
                var pos = System.Windows.Forms.Cursor.Position;
                var diff = new System.Drawing.PointF((float)(oldPos.X - pos.X) / 4, (float)(oldPos.Y - pos.Y) / 4);
                if (Math.Abs(diff.X) > 32 || Math.Abs(diff.Y) > 32)
                    diff = System.Drawing.PointF.Empty;
                oldPos = new Point(pos.X, pos.Y);
#else
                var power = 10.0f;
                var x = UnityEngine.Input.GetAxis("Mouse X") * power;
                var y = UnityEngine.Input.GetAxis("Mouse Y") * power;
                var diff = new Point((int)-x, (int)y);
#endif //WINDOWS

                // 角度を変更
                s.xangle += diff.Y;
                s.yangle += diff.X;
#if WINDOWS
                // ウィンドウ中央からの移動量が一定以上だった場合、ウィンドウ中央に座標を補正する
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var rect = new Rect();

                GetWindowRect(process.MainWindowHandle, ref rect);

                var center = new System.Drawing.Point((int)((rect.Right - rect.Left) * 0.5f + rect.Left), (int)((rect.Bottom - rect.Top) * 0.5f + rect.Top));
                diff = new System.Drawing.PointF((float)(center.X - pos.X) / 4, (float)(center.Y - pos.Y) / 4);
                if (Math.Abs(diff.X) > 32 || Math.Abs(diff.Y) > 32)
                {
                    System.Windows.Forms.Cursor.Position = center;
                    oldPos = new Point(center.X, center.Y);
                }
#endif //WINDOWS
#endif //WINDOWS || UNITY_EDITOR || UNITY_STANDALONE

                // 操作がロックされていなければ、主人公の向きにも適用する
                if (s.playerLocked == 0)
                    s.hero.setDirection(MapScene.DegreeToCharacterDirection(s.yangle), true);
            }
            else
            {
#if WINDOWS
                if (!SharpKmy.Entry.isWindowActive() || owner.owner.isDebugWindowFocused())
#else
                if (!SharpKmy.Entry.isWindowActive() || owner.owner.isDebugWindowFocused()
                || Yukar.Engine.MenuController.state != Yukar.Engine.MenuController.State.HIDE)
#endif
                {
                    setCursorVisibility(true, true);
                }
            }

            float moveX = 0;
            float moveY = 0;
#if ENABLE_VR
            double yangle;
            if (SharpKmyVr.Func.IsReady())
            {
                yangle = SharpKmyVr.Func.GetHmdRotateY() + owner.GetVrCameraData().GetCombinedOffsetRotateY();
            }
            else
            {
                yangle = s.yangle / 180 * Math.PI;
            }
#else   // #if ENABLE_VR
            double yangle = s.yangle / 180 * Math.PI;
#endif  // #if ENABLE_VR

            // 入力を受け付けて主人公の位置を更新する
            bool isEncount = false;
            if (s.playerLocked == 0 && !owner.isBattle && !owner.IsMapChangedFrame)
            {
                bool hitcollisionEvent = false;

                checkEncountAndRunNotCollidableEvent(ref isEncount, ref hitcollisionEvent);

                if (!isEncount)
                {
                    {
                        int count = 0;
                        bool isUp = Input.KeyTest(StateType.DIRECT, KeyStates.UP);
                        bool isDown = Input.KeyTest(StateType.DIRECT, KeyStates.DOWN);
                        bool isLeft = Input.KeyTest(StateType.DIRECT, KeyStates.LEFT);
                        bool isRight = Input.KeyTest(StateType.DIRECT, KeyStates.RIGHT);
                        if (isUp && isDown == false)
                        {
                            moveX -= (float)Math.Sin(yangle) * s.hero.mMoveStep * GameMain.getRelativeParam60FPS();
                            moveY -= (float)Math.Cos(yangle) * s.hero.mMoveStep * GameMain.getRelativeParam60FPS();
                            count++;
                        }
                        if (isDown && isUp == false)
                        {
                            moveX += (float)Math.Sin(yangle) * s.hero.mMoveStep * GameMain.getRelativeParam60FPS();
                            moveY += (float)Math.Cos(yangle) * s.hero.mMoveStep * GameMain.getRelativeParam60FPS();
                            count++;
                        }
                        if (isLeft && isRight == false)
                        {
                            double tmpY = yangle + Math.PI / 2;
                            moveX -= (float)Math.Sin(tmpY) * s.hero.mMoveStep * GameMain.getRelativeParam60FPS();
                            moveY -= (float)Math.Cos(tmpY) * s.hero.mMoveStep * GameMain.getRelativeParam60FPS();
                            count++;
                        }
                        if (isRight && isLeft == false)
                        {
                            double tmpY = yangle + Math.PI / 2;
                            moveX += (float)Math.Sin(tmpY) * s.hero.mMoveStep * GameMain.getRelativeParam60FPS();
                            moveY += (float)Math.Cos(tmpY) * s.hero.mMoveStep * GameMain.getRelativeParam60FPS();
                            count++;
                        }
                        if (0 < count)
                        {
                            moveX /= (float)Math.Sqrt(count);
                            moveY /= (float)Math.Sqrt(count);
                        }
                    }

                    // 移動量が限りなく0に近いけど0じゃない場合に当たり判定の不具合のもとになるので, 丸め処理を行う
                    if (Math.Abs(moveX) < 1.0E-10) moveX = 0;
                    if (Math.Abs(moveY) < 1.0E-10) moveY = 0;

                    if (Input.KeyTest(StateType.DIRECT, KeyStates.DASH) && owner.owner.data.system.dashAvailable)
                    {
                        s.hero.ChangeSpeed(owner.owner.data.system.moveSpeed + 2);
                    }
                    else
                    {
                        s.hero.ChangeSpeed(owner.owner.data.system.moveSpeed);
                    }

                    if ((moveX != 0 || moveY != 0) && !s.hero.IsMoving() && !hitcollisionEvent)
                    {
                        s.hero.playMotion("walk");

                        dx = (int)(s.hero.x + moveX) - (int)s.hero.x;
                        dy = (int)(s.hero.z + moveY) - (int)s.hero.z;

                        // 俯瞰視点では斜め地点のイベントが接触反応する事はないので、
                        // 左右どちらにも移動量がある場合は、大きかった方を採用する
                        if (dx != 0 && dy != 0)
                        {
                            if (Math.Abs(moveX) > Math.Abs(moveY))
                                dy = 0;
                            else
                                dx = 0;
                        }

                        MapCharacter hitChr = null;
                        if (dx != 0 || dy != 0)
#if ENABLE_VR
                            hitChr = checkCharCollision(s.hero, (int)dx, (int)dy);
#else   // #if ENABLE_VR
                            hitChr = checkCharCollision(s.hero, dx, dy);
#endif  // #if ENABLE_VR
                        if (hitChr == null || owner.owner.debugSettings.ignoreEvent)
                        {
                            var walkedMove = s.hero.Walk(moveX, moveY, false, s.mapDrawer, eventHeightMap, owner.owner.debugSettings.ignoreTerrainHeight, true, this);

                            if (walkedMove != Vector2.Zero)
                            {
                                if (dx != 0 || dy != 0)
                                {
                                    // マップ更新
                                    updateEventHeightMap();

                                    // 毒ダメージ
                                    if (isPoison)
                                    {
                                        foreach (var member in owner.owner.data.party.members)
                                        {
                                            if (member.statusAilments.HasFlag(Hero.StatusAilments.POISON))
                                            {
                                                member.hitpoint--;
                                                if (member.hitpoint <= 0)
                                                    member.hitpoint = 1;
                                            }
                                        }
                                    }

                                    // 毒の床？
                                    var info = s.mapDrawer.getTerrainInfo(s.map.getTerrainAttrib((int)s.hero.x, (int)s.hero.z));
                                    if (info != null && info.poison)
                                    {
                                        foreach (var member in owner.owner.data.party.members.Where(player => player.statusAilments != Hero.StatusAilments.DOWN))
                                        {
                                            member.statusAilments |= Hero.StatusAilments.POISON;
                                        }
                                    }
                                }
                            }

                            // 移動できなかった場合、当たり判定のあるイベントがその地点に有るかどうか調べる(高低差無視がついていれば実行される
                            {
                                var isNotWakedMoveX = (walkedMove.X == 0 && walkedMove.X != moveX);
                                var isNotWakedMoveY = (walkedMove.Y == 0 && walkedMove.Y != moveY);
                                if (isNotWakedMoveX)
                                {
                                    dx = (moveX == 0) ? 0 : (0 < moveX) ? 1 : -1;
                                    dy = 0;

#if ENABLE_VR
                                    hitChr = getOtherChacaterFromMap(s.hero, (int)dx, (int)dy, true);
#else   // #if ENABLE_VR
                                    hitChr = getOtherChacaterFromMap(s.hero, dx, dy, true);
#endif  // #if ENABLE_VR
                                    if (hitChr != null && hitChr.collidable)
                                    {
                                        if (runEvent(hitChr))
                                            s.hero.playMotion("wait");
                                    }
                                }
                                if (isNotWakedMoveY)
                                {
                                    dx = 0;
                                    dy = (moveY == 0) ? 0 : (0 < moveY) ? 1 : -1;

#if ENABLE_VR
                                    hitChr = getOtherChacaterFromMap(s.hero, (int)dx, (int)dy, true);
#else   // #if ENABLE_VR
                                    hitChr = getOtherChacaterFromMap(s.hero, dx, dy, true);
#endif  // #if ENABLE_VR
                                    if (hitChr != null && hitChr.collidable)
                                    {
                                        if (runEvent(hitChr))
                                            s.hero.playMotion("wait");
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (runEvent(hitChr))
                                s.hero.playMotion("wait");
                        }
                    }
                    else
                    {
                        s.hero.playMotion("wait");
                    }

                    // 話しかける
                    if (Input.KeyTest(StateType.TRIGGER, KeyStates.DECIDE) &&
                        checkAndRunTalkableScript())
                    {
                        s.hero.playMotion("wait");
                    }
                }
            }
        }

        int prevSecond;
        int hideCount;

        /// <summary>
        /// Changes the visibility of the mouse cursor.
        /// </summary>
        /// <param name="flag">Visible/Hide</param>
        /// <param name="applyEverySeconds">Apply in every second. (For DebugWindow)</param>
        internal void setCursorVisibility(bool flag, bool applyEverySeconds = false)
        {
            if (SharpKmy.Entry.isFullScreenMode())
                flag = false;
            
            if (cursorVisible == flag) return;
            cursorVisible = flag;
            
            updateCursorVisibility();
        }

        internal void updateCursorVisibility()
        {
            if (cursorVisible)
            {
#if WINDOWS
                hideCount--;
                Console.WriteLine("show " + hideCount);
                System.Windows.Forms.Cursor.Show();
#else
                UnityEngine.Cursor.visible = true;
                UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.None;
#endif
            }
            else
            {
#if WINDOWS
                hideCount++;
                Console.WriteLine("hide " + hideCount);
                System.Windows.Forms.Cursor.Hide();
#else
                UnityEngine.Cursor.visible = false;
                UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.Locked;
#endif
            }
        }

        private void procPlayerForLegacy(MapData map, float yangle, bool isPoison)
        {
            var s = owner;

            // 一人称視点でXとZが小数になっている場合があるので、切り捨てる
#if ENABLE_VR
            if (!SharpKmyVr.Func.IsReady())
            {
                s.hero.x = (int)s.hero.x + 0.5f;
                s.hero.z = (int)s.hero.z + 0.5f;
            }
#else   // #if ENABLE_VR
            s.hero.x = (int)s.hero.x + 0.5f;
            s.hero.z = (int)s.hero.z + 0.5f;
#endif  // #if ENABLE_VR

            // 入力を受け付けて主人公の位置を更新する
            bool isEncount = false;
            if (s.playerLocked == 0 && !owner.isBattle && !owner.IsMapChangedFrame)
            {
                bool hitcollisionEvent = false;

                checkEncountAndRunNotCollidableEvent(ref isEncount, ref hitcollisionEvent);

                if (!isEncount)
                {
#if ENABLE_VR
                    float speed = s.hero.mMoveStep;
                    float[] xtbl, ytbl;
                    int[] dtbl;

                    if (SharpKmyVr.Func.IsReady())
                    {
                        xtbl = new float[] { 0 * speed, -1 * speed, 0 * speed, 1 * speed };
                        ytbl = new float[] { -1 * speed, 0 * speed, 1 * speed, 0 * speed };
                        dtbl = new int[] { 0, 2, 1, 3 };
                    }
                    else
                    {
                        xtbl = new float[] { 0, -1, 0, 1 };
                        ytbl = new float[] { -1, 0, 1, 0 };
                        dtbl = new int[] { 0, 2, 1, 3 };
                    }
#else
                    int[] xtbl = new int[] { 0, -1, 0, 1 };
                    int[] ytbl = new int[] { -1, 0, 1, 0 };
                    int[] dtbl = new int[] { 0, 2, 1, 3 };
#endif
                    int idx = 0;//0度
                    double yrangle = MathHelper.ToRadians(yangle);

                    if (Math.Abs(Math.Cos(yrangle)) > 0.7071)
                    {
                        if (Math.Cos(yrangle) > 0)
                        {
                            idx = 0;
                        }
                        else
                        {
                            idx = 2;
                        }
                    }
                    else
                    {
                        if (Math.Sin(yrangle) > 0)
                        {
                            idx = 1;
                        }
                        else
                        {
                            idx = 3;
                        }
                    }

                    bool moveinput = true;
                    bool overrideDirection = false;

                    switch (owner.CurrentCameraMode)
                    {
                        case Common.Rom.Map.CameraControlMode.NORMAL:
#if ENABLE_VR
#if ENABLE_GHOST_MOVE
                        case Common.Rom.Map.CameraControlMode.GHOST:
#endif  // #if ENABLE_GHOST_MOVE
                            if (SharpKmyVr.Func.IsReady())
                            {
                                float x = 0.0f;
                                float y = 0.0f;

                                if (Input.KeyTest(StateType.DIRECT, KeyStates.LEFT))
                                {
                                    x = -1.0f;
                                }
                                else if (Input.KeyTest(StateType.DIRECT, KeyStates.RIGHT))
                                {
                                    x = 1.0f;
                                }
                                if (Input.KeyTest(StateType.DIRECT, KeyStates.UP))
                                {
                                    y = -1.0f;
                                }
                                else if (Input.KeyTest(StateType.DIRECT, KeyStates.DOWN))
                                {
                                    y = 1.0f;
                                }

                                if (x != 0.0f || y != 0.0f)
                                {
                                    float rotY = SharpKmyVr.Func.GetHmdRotateY() + owner.GetVrCameraData().GetCombinedOffsetRotateY();
                                    SharpKmyMath.Vector3 vec1 = SharpKmyMath.Vector3.normalize(new SharpKmyMath.Vector3(x, 0, y));
                                    SharpKmyMath.Matrix4 mtxDir = SharpKmyMath.Matrix4.translate(vec1.x, vec1.y, vec1.z);
                                    SharpKmyMath.Matrix4 mtxRot = SharpKmyMath.Matrix4.rotateY(rotY);
                                    SharpKmyMath.Vector3 vec = (mtxRot * mtxDir).translation() * speed;
                                    dx = vec.x;
                                    dy = vec.z;
                                }

                                // 移動ベクトルから方向値（デジタル）を設定
                                if (dx != 0.0f || dy != 0.0f)
                                {
                                    float rad = (float)Math.Atan2(dx, dy);
                                    dir = MapCharacter.convertDirectionToDigital(rad);
                                }

                                // 全ての移動キーが押されていなかったら停止する
                                if (!Input.KeyTest(StateType.DIRECT, KeyStates.LEFT) && !Input.KeyTest(StateType.DIRECT, KeyStates.RIGHT) && !Input.KeyTest(StateType.DIRECT, KeyStates.UP) && !Input.KeyTest(StateType.DIRECT, KeyStates.DOWN))
                                {
                                    dx = 0;
                                    dy = 0;
                                    dir = 0;
                                    moveinput = false;
                                }

                                dirKey = KeyStates.NONE;
                            }
                            else
#endif  // #if ENABLE_VR
                            {
                                // 一度入力したら同じ方向に動き続ける
                                // 途中でカメラの向きが変わっても移動する方向は変えない
                                // バトル画面などのフェード中に移動キーが押されても自然に移動できるように移動中の方向と違うキーが入力されても向きを変える
                                if (Input.KeyTest(StateType.TRIGGER, KeyStates.RIGHT) || (Input.KeyTest(StateType.DIRECT, KeyStates.RIGHT) && dirKey != KeyStates.RIGHT && dirKey != KeyStates.LEFT))
                                {
                                    dx = xtbl[(3 + idx) % 4];
                                    dy = ytbl[(3 + idx) % 4];
                                    dir = dtbl[(3 + idx) % 4];
                                    dirKey = KeyStates.RIGHT;
                                }
                                else if (Input.KeyTest(StateType.TRIGGER, KeyStates.LEFT) || (Input.KeyTest(StateType.DIRECT, KeyStates.LEFT) && dirKey != KeyStates.RIGHT && dirKey != KeyStates.LEFT))
                                {
                                    dx = xtbl[(1 + idx) % 4];
                                    dy = ytbl[(1 + idx) % 4];
                                    dir = dtbl[(1 + idx) % 4];
                                    dirKey = KeyStates.LEFT;
                                }
                                else if (Input.KeyTest(StateType.TRIGGER, KeyStates.DOWN) || (Input.KeyTest(StateType.DIRECT, KeyStates.DOWN) && dirKey != KeyStates.UP && dirKey != KeyStates.DOWN))
                                {
                                    dx = xtbl[(2 + idx) % 4];
                                    dy = ytbl[(2 + idx) % 4];
                                    dir = dtbl[(2 + idx) % 4];
                                    dirKey = KeyStates.DOWN;
                                }
                                else if (Input.KeyTest(StateType.TRIGGER, KeyStates.UP) || (Input.KeyTest(StateType.DIRECT, KeyStates.UP) && dirKey != KeyStates.UP && dirKey != KeyStates.DOWN))
                                {
                                    dx = xtbl[(0 + idx) % 4];
                                    dy = ytbl[(0 + idx) % 4];
                                    dir = dtbl[(0 + idx) % 4];
                                    dirKey = KeyStates.UP;
                                }

                                // 全ての移動キーが押されていなかったら停止する
                                if (!Input.KeyTest(StateType.DIRECT, KeyStates.LEFT) && !Input.KeyTest(StateType.DIRECT, KeyStates.RIGHT) && !Input.KeyTest(StateType.DIRECT, KeyStates.UP) && !Input.KeyTest(StateType.DIRECT, KeyStates.DOWN))
                                {
                                    dx = 0;
                                    dy = 0;
                                    dir = 0;
                                    moveinput = false;
                                    dirKey = KeyStates.NONE;
                                }
                            }
                            break;

                        case Common.Rom.Map.CameraControlMode.VIEW:
                            // 操作がロックされていなければ、主人公の向きにも適用する
                            s.hero.setDirection(MapScene.DegreeToCharacterDirection(yangle), true);

                            if (Input.KeyTest(StateType.TRIGGER, KeyStates.LEFT) || (Input.KeyTest(Input.StateType.DIRECT, KeyStates.LEFT) && !s.IsCameraScroll))
                            {
#if ENABLE_VR
                                if (SharpKmyVr.Func.IsReady())
                                {
                                    SharpKmyMath.Vector3 vecDir = SharpKmyVr.Func.GetHmdPoseDirection();
                                    SharpKmyMath.Vector3 yAxis = new SharpKmyMath.Vector3(0, 1, 0);
                                    vecDir.y = 0.0f;
                                    vecDir = SharpKmyMath.Vector3.normalize(vecDir);
                                    vecDir *= -speed;
                                    SharpKmyMath.Vector3 vec = SharpKmyMath.Vector3.crossProduct(vecDir, yAxis);
                                    dx = vec.x;
                                    dy = vec.z;
                                }
                                else
                                {
                                    if (Input.KeyTest(StateType.DIRECT, KeyStates.DASH))
                                    {
                                        if (dirKey != KeyStates.LEFT)
                                        {
                                            dx = xtbl[(1 + idx) % 4];
                                            dy = ytbl[(1 + idx) % 4];
                                        }
                                    }
                                    else
                                    {
                                        s.hero.setDirection(MapScene.DegreeToCharacterDirection(yangle), true);
                                        owner.ScrollCameraY(90);

                                        dx = 0;
                                        dy = 0;
                                    }
                                }
#else   // #if ENABLE_VR
                                if (Input.KeyTest(StateType.DIRECT, KeyStates.DASH))
                                {
                                    if (dirKey != KeyStates.LEFT)
                                    {
                                        dx = xtbl[(1 + idx) % 4];
                                        dy = ytbl[(1 + idx) % 4];
                                    }
                                }
                                else
                                {
                                    owner.ScrollCameraY(90);

                                    dx = 0;
                                    dy = 0;
                                }
#endif  // #if ENABLE_VR

                                dirKey = KeyStates.LEFT;
                            }
                            else if (Input.KeyTest(StateType.TRIGGER, KeyStates.RIGHT) || (Input.KeyTest(Input.StateType.DIRECT, KeyStates.RIGHT) && !s.IsCameraScroll))
                            {
#if ENABLE_VR
                                if (SharpKmyVr.Func.IsReady())
                                {
                                    SharpKmyMath.Vector3 vecDir = SharpKmyVr.Func.GetHmdPoseDirection();
                                    SharpKmyMath.Vector3 yAxis = new SharpKmyMath.Vector3(0, 1, 0);
                                    vecDir.y = 0.0f;
                                    vecDir = SharpKmyMath.Vector3.normalize(vecDir);
                                    vecDir *= speed;
                                    SharpKmyMath.Vector3 vec = SharpKmyMath.Vector3.crossProduct(vecDir, yAxis);
                                    dx = vec.x;
                                    dy = vec.z;
                                }
                                else
                                {
                                    if (Input.KeyTest(StateType.DIRECT, KeyStates.DASH))
                                    {
                                        if (dirKey != KeyStates.RIGHT)
                                        {
                                            dx = xtbl[(3 + idx) % 4];
                                            dy = ytbl[(3 + idx) % 4];
                                        }
                                    }
                                    else
                                    {
                                        s.hero.setDirection(MapScene.DegreeToCharacterDirection(yangle), true);
                                        owner.ScrollCameraY(-90);

                                        dx = 0;
                                        dy = 0;
                                    }
                                }
#else   // #if ENABLE_VR
                                if (Input.KeyTest(StateType.DIRECT, KeyStates.DASH))
                                {
                                    if (dirKey != KeyStates.RIGHT)
                                    {
                                        dx = xtbl[(3 + idx) % 4];
                                        dy = ytbl[(3 + idx) % 4];
                                    }
                                }
                                else
                                {
                                    owner.ScrollCameraY(-90);

                                    dx = 0;
                                    dy = 0;
                                }
#endif  // #if ENABLE_VR

                                dirKey = KeyStates.RIGHT;
                            }
                            else if (Input.KeyTest(StateType.TRIGGER, KeyStates.UP) || (Input.KeyTest(StateType.DIRECT, KeyStates.UP) && dirKey != KeyStates.UP))
                            {
#if ENABLE_VR
                                if (SharpKmyVr.Func.IsReady())
                                {
                                    SharpKmyMath.Vector3 vecDir = SharpKmyVr.Func.GetHmdPoseDirection();
                                    vecDir.y = 0.0f;
                                    vecDir = SharpKmyMath.Vector3.normalize(vecDir);
                                    vecDir *= speed;
                                    dx = vecDir.x;
                                    dy = vecDir.z;
                                }
                                else
                                {
                                    dx = xtbl[(0 + idx) % 4];
                                    dy = ytbl[(0 + idx) % 4];
                                }
#else   // #if ENABLE_VR
                                dx = xtbl[(0 + idx) % 4];
                                dy = ytbl[(0 + idx) % 4];
#endif  // #if ENABLE_VR

                                dirKey = KeyStates.UP;
                            }
                            else if (Input.KeyTest(StateType.TRIGGER, KeyStates.DOWN) || (Input.KeyTest(StateType.DIRECT, KeyStates.DOWN) && dirKey != KeyStates.DOWN))
                            {
#if ENABLE_VR
                                if (SharpKmyVr.Func.IsReady())
                                {
                                    SharpKmyMath.Vector3 vecDir = SharpKmyVr.Func.GetHmdPoseDirection();
                                    vecDir.y = 0.0f;
                                    vecDir = SharpKmyMath.Vector3.normalize(vecDir);
                                    vecDir *= -speed;
                                    dx = vecDir.x;
                                    dy = vecDir.z;
                                }
                                else
                                {
                                    dx = xtbl[(2 + idx) % 4];
                                    dy = ytbl[(2 + idx) % 4];
                                }
#else   // #if ENABLE_VR
                                dx = xtbl[(2 + idx) % 4];
                                dy = ytbl[(2 + idx) % 4];
#endif  // #if ENABLE_VR

                                dirKey = KeyStates.DOWN;
                            }

                            if (Input.KeyTest(StateType.DIRECT, KeyStates.LEFT) ||
                                Input.KeyTest(StateType.DIRECT, KeyStates.RIGHT) ||
                                Input.KeyTest(StateType.DIRECT, KeyStates.UP) ||
                                Input.KeyTest(StateType.DIRECT, KeyStates.DOWN))
                            {
                                dir = dtbl[idx];
                                overrideDirection = true;
                            }
                            else
                            {
                                dx = 0;
                                dy = 0;
                                moveinput = false;
                                dirKey = KeyStates.NONE;
                            }
                            break;
                    }

                    if (moveinput && !hitcollisionEvent)
                    {
                        if (Input.KeyTest(StateType.DIRECT, KeyStates.DASH) && owner.owner.data.system.dashAvailable)
                        {
                            s.hero.ChangeSpeed(owner.owner.data.system.moveSpeed + 2);
                            if (s.hero.isChangeMotionAvailable()) s.hero.playMotion("run");
                        }
                        else
                        {
                            s.hero.ChangeSpeed(owner.owner.data.system.moveSpeed);
                            if (s.hero.isChangeMotionAvailable()) s.hero.playMotion("walk");
                        }
                    }
                    else
                    {
                        s.hero.ChangeSpeed(owner.owner.data.system.moveSpeed);

                        if (!s.hero.IsMoving() && s.hero.isChangeMotionAvailable() && (s.hero.currentMotion == "walk" || s.hero.currentMotion == "run"))
                            s.hero.playMotion("wait");
                    }

#if ENABLE_VR
                    if ((dx != 0 || dy != 0) && !s.hero.IsMoving() && !hitcollisionEvent)
                    {
                        int dx2 = 0;
                        int dy2 = 0;
                        if (Math.Abs(dx) > Math.Abs(dy))
                        {
                            dx2 = (int)(s.hero.x + dx) - (int)s.hero.x;
                        }
                        else
                        {
                            dy2 = (int)(s.hero.z + dy) - (int)s.hero.z;
                        }
                        var hitChr = checkCharCollision(s.hero, dx2, dy2);
#else   // #if ENABLE_VR
                    if ((dx | dy) != 0 && !s.hero.IsMoving() && !hitcollisionEvent)
                    {
                        var hitChr = checkCharCollision(s.hero, dx, dy);
#endif  // #if ENABLE_VR
                        if (hitChr == null || owner.owner.debugSettings.ignoreEvent)
                        {
                            bool walked;

#if ENABLE_VR
                            if (SharpKmyVr.Func.IsReady())
                            {
                                var move = s.hero.Walk(dx, dy, !overrideDirection, map, eventHeightMap,
                                    owner.owner.debugSettings.ignoreTerrainHeight, hitcollisionEvent);
                                walked = (move != Vector2.Zero);
                            }
                            else
                            {
                                walked = s.hero.Walk((int)dx, (int)dy, !overrideDirection, map, eventHeightMap,
                                    owner.owner.debugSettings.ignoreTerrainHeight, hitcollisionEvent);
                            }
#else   // #if ENABLE_VR
                            walked = s.hero.Walk(dx, dy, !overrideDirection, map, eventHeightMap, owner.owner.debugSettings.ignoreTerrainHeight, hitcollisionEvent);
#endif  // #if ENABLE_VR

                            if (walked)
                            {
                                // 毒ダメージ
                                if (isPoison)
                                {
                                    foreach (var member in owner.owner.data.party.members)
                                    {
                                        if (member.statusAilments.HasFlag(Hero.StatusAilments.POISON))
                                        {
                                            member.hitpoint--;
                                            if (member.hitpoint <= 0)
                                                member.hitpoint = 1;
                                        }
                                    }
                                }

                                // 毒の床？
                                var info = s.mapDrawer.getTerrainInfo(s.map.getTerrainAttrib((int)s.hero.x, (int)s.hero.z));
                                if (info != null && info.poison)
                                {
                                    foreach (var member in owner.owner.data.party.members.Where(player => player.statusAilments != Hero.StatusAilments.DOWN))
                                    {
                                        member.statusAilments |= Hero.StatusAilments.POISON;
                                    }
                                }
                            }
                            else
                            {
                                // 移動できなかった場合、当たり判定のあるイベントがその地点に有るかどうか調べる(高低差無視がついていれば実行される)
#if ENABLE_VR
                                hitChr = getOtherChacaterFromMap(s.hero, (int)dx, (int)dy, true);
#else   // #if ENABLE_VR
                                hitChr = getOtherChacaterFromMap(s.hero, dx, dy, true);
#endif  // #if ENABLE_VR
                                if (hitChr != null && hitChr.collidable)
                                {
                                    overrideDirection = true;
                                    if (runEvent(hitChr) && s.hero.isChangeMotionAvailable())
                                        s.hero.playMotion("wait");
                                }
                            }
                        }
                        else
                        {
                            overrideDirection = true;
                            if (runEvent(hitChr) && s.hero.isChangeMotionAvailable())
                                s.hero.playMotion("wait");
                        }
                    }

                    if (overrideDirection) s.hero.setDirection(dir);

                    // 話しかける
                    if (Input.KeyTest(StateType.TRIGGER, KeyStates.DECIDE) &&
                        checkAndRunTalkableScript() && s.hero.isChangeMotionAvailable())
                    {
                        s.hero.playMotion("wait");
                    }
                }
            }
            else
            {
                dirKey = KeyStates.NONE;

                lastPlayerX = s.hero.x;
                lastPlayerZ = s.hero.z;
            }
        }

        private void checkEncountAndRunNotCollidableEvent(ref bool isEncount, ref bool hitcollisionEvent)
        {
            var s = owner;

            if (!s.hero.IsMoving())
            {
                float x = (int)s.hero.x + 0.5f;
                float z = (int)s.hero.z + 0.5f;
                if (Math.Abs(x - lastPlayerX) <= 1.5f && Math.Abs(z - lastPlayerZ) <= 1.5f &&
                    (x != lastPlayerX || z != lastPlayerZ))
                {
                    // 移動後のイベントチェック(コリジョンオフのイベントは乗ったときに発動する)
                    if (!s.owner.debugSettings.ignoreEvent && checkAndRunCollisionScript(false))
                    {
                        hitcollisionEvent = true;
                    }
                    else
                    {
                        // イベントに乗っている間はエンカウントしない
                        isEncount = procEncount();
                    }
                }

                lastPlayerX = x;
                lastPlayerZ = z;
            }
        }

        internal Guid[] GetEncountMonsters(Common.Rom.Map.BattleSetting inBattleSetting)
        {
            var monsterList = new List<Guid>();

            int encountMonsterCount = owner.GetRandom(inBattleSetting.maxMonsters + 1, 1);
            var encounts = inBattleSetting.getAvailableEncounts(owner.owner.catalog);
            var mainMonster = owner.owner.catalog.getItemFromGuid(ChoiceMonsterFromWeight(encounts)) as Common.Rom.Monster;

            switch (mainMonster.encountType)
            {
                case 0: // 単独で出現
                    monsterList.Add(mainMonster.guId);
                    break;

                case 1: // 同じモンスターと群れで出現
                    for (int i = 0; i < encountMonsterCount; i++)
                    {
                        monsterList.Add(mainMonster.guId);
                    }
                    break;

                case 2: // 他のモンスターと群れで出現
                    {
                        var otherEncounts = new List<Common.Rom.Map.Encount>();

                        foreach (var item in encounts)
                        {
                            var monster = owner.owner.catalog.getItemFromGuid(item.enemy) as Common.Rom.Monster;

                            if ((monster.encountType == 1) || (monster.encountType == 2))
                            {
                                otherEncounts.Add(item);
                            }
                        }

                        monsterList.Add(mainMonster.guId);

                        while (monsterList.Count < encountMonsterCount)
                        {
                            monsterList.Add(ChoiceMonsterFromWeight(otherEncounts));
                        }
                    }
                    break;
            }

            return monsterList.ToArray();
        }

        private Guid ChoiceMonsterFromWeight(List<Common.Rom.Map.Encount> inEncounts)
        {
            var decisionWeights = new List<int>(inEncounts.Count);
            var totalWeight = 0;

            foreach (var encount in inEncounts)
            {
                totalWeight += encount.weight;

                decisionWeights.Add(totalWeight);
            }

            var rand = owner.GetRandom(totalWeight);

            for (int i = 0; i < decisionWeights.Count; i++)
            {
                if (rand < decisionWeights[i])
                {
                    return inEncounts[i].enemy;
                }
            }

            return Guid.Empty;
        }

        private bool procEncount()
        {
            if (owner.owner.data.system.encountAvailable && !owner.owner.debugSettings.ignoreBattleEncount)
            {
                var prevEncountsCnt = owner.battleSetting.encounts.Count;

                restStep--;

                if (updateBattleSetting())
                {
                    var prevRestStep = restStep;

                    genEncountStep();

                    // エリアの境界をジグザグに進んでもバトルを発生させるために、直前の残り歩数の方が少なかった時はそちらの歩数を使用する
                    if ((prevEncountsCnt > 0) && (prevRestStep < restStep))
                    {
                        restStep = prevRestStep;
                    }
                }
            }

            if ((restStep <= 0) && (owner.battleSetting.getAvailableMonsterCount(owner.owner.catalog) > 0))
            {
                genEncountStep();

                // 出現確率判定
                if (owner.GetRandom(100) < owner.battleSetting.encountPercent)
                {
                    owner.hero.playMotion("wait");

                    nowScreenColor = owner.GetNowScreenColor();

                    // 2Dバトルは若干暗め、3Dバトルは透明
                    if (owner.owner.IsBattle2D)
                    {
                        owner.SetScreenColor(new Color(0, 0, 0, 64));
                    }
                    else
                    {
                        owner.SetScreenColor(new Color(0, 0, 0, 0));
                    }

                    Common.Resource.Bgm battleBgm = owner.getMapBattleBgm();
                    Common.Resource.Bgs battleBgs = owner.getMapBattleBgs();
                    PlayBattleBGM(battleBgm, battleBgs);

                    nowEnemies = GetEncountMonsters(owner.battleSetting);

                    RegisterBattleEvents();


#if WINDOWS
                    var coroutine = battleStartImpl(nowEnemies);
                    while (coroutine.MoveNext()) ;
#else
                    UnityUtil.changeScene(UnityUtil.SceneType.MAP);
                    // 透明状態を反映するために、キャラクタのDrawを通す
                    foreach (var mapChr in owner.mapCharList)
                    {
                        mapChr.draw(SharpKmyGfx.Render.getDefaultRender());
                    }
                    UnityEntry.capture();

                    owner.isBattleLoading = true;
                    UnityEntry.startCoroutine(battleStartImpl(nowEnemies));
#endif

                    return true;
                }
            }

            return false;
        }

        private IEnumerator battleStartImpl(Guid[] nowEnemies)
        {
            UnityUtil.changeScene(UnityUtil.SceneType.BATTLE);

            var bgGuid = owner.map.getBattleBg(owner.owner.catalog, owner.battleSetting);
            if (owner.battleSequenceManager.isWrongFromCurrentBg(bgGuid))
            {
                var coroutine = owner.battleSequenceManager.prepare(bgGuid);
                while (coroutine.MoveNext()) yield return null;
            }

            owner.battleSequenceManager.BattleStart(owner.owner.data.party, nowEnemies, owner.battleSetting);
            owner.StartBattleMode();
            owner.isBattleLoading = false;
            
#if WINDOWS
#else
            UnityEntry.reserveClearFB();
#endif
        }

        private bool updateBattleSetting()
        {
            var map = owner.map;
            var hero = owner.hero;
            var x = (int)hero.x;
            var z = (int)hero.z;
            var battleSetting = map.mapBattleSetting;

            foreach (var item in map.areaBattleSettings)
            {
                if (item.enable && item.areaRect.Contains(x, z))
                {
                    battleSetting = item;

                    break;
                }
            }

            if (owner.battleSetting == battleSetting)
            {
                return false;
            }

            owner.battleSetting = battleSetting;

            return true;
        }

        internal void RegisterBattleEvents()
        {
            owner.battleSequenceManager.BattleStartEvents += owner.ShowBattleStartMessage;
            owner.battleSequenceManager.BattleResultWinEvents += PlayBattleWinBGM;
            //owner.battleSequenceManager.BattleResultLoseGameOverEvents += BattleLoseGameover;
            owner.battleSequenceManager.BattleResultEscapeEvents += owner.ShowBattleEscapeMessage;
        }

        internal void PlayBattleBGM(Common.Resource.Bgm bgm, Common.Resource.Bgs bgs)
        {
            nowBgmSound = Audio.GetNowBgm(false);
            nowBgsSound = Audio.GetNowBgsRom() as Common.Resource.Bgs;

            if (bgm != null)
            {
                if (nowBgmSound == null || nowBgmSound.rom != bgm)
                {
                    Audio.GetNowBgm();
                    Audio.PlayBgm(bgm);
                }
            }
            else
            {
                Audio.GetNowBgm();  // ついでにpauseしてくれるので、stopの必要はないはず
                //Audio.StopBgm();
            }

            if (bgs != null)
                Audio.PlayBgs(bgs);
            else
                Audio.StopBgs();
        }

        internal void PlayBattleWinBGM()
        {
            var gs = owner.owner.catalog.getGameSettings();
            var sound = owner.owner.catalog.getItemFromGuid(gs.battleWinBgm) as Common.Resource.Bgm;

            if (sound == null)
            {
                Audio.StopBgm();
            }
            else
            {
                Audio.PlayBgm(sound);
            }
        }

        internal void ResumeMapBGM()
        {
            bool doResume = false;

            switch (owner.battleSequenceManager.BattleResult)
            {
                case BattleSequenceManager.BattleResultState.Win:
                case BattleSequenceManager.BattleResultState.Lose_Continue:
                case BattleSequenceManager.BattleResultState.Escape:
                case BattleSequenceManager.BattleResultState.Lose_Advanced_GameOver:
                    doResume = true;
                    break;
            }

            // 同じBGMを2回再生させないように現在再生しているBGMとマップで再生するBGMを比較する
            // しかしGetNowBGMメソッド内でBGMのPauseが発生するため現在はそのままSwapさせて構わない
            //if(isSwapBGM && (nowBgmSound != sound)) Audio.SwapBgm(nowBgmPlayer, nowBgmSound);

            // 戦闘の抜け方に関わらず、BGSは止める
            Audio.StopBgs();

            if (doResume)
            {
                Audio.SwapBgm(nowBgmSound);
                if (nowBgsSound != null)
                    Audio.PlayBgs(nowBgsSound);

                nowBgmSound = null;
                nowBgsSound = null;
            }
        }

        internal void CreateHeroChr(float x, float y, MapData mapdata, float height = 0f)
        {
            var s = owner;
            var catalog = s.owner.catalog;

            // 主人公のMapChrを作成
            if (s.owner.data.party.members.Count > 0)
            {
                var mapChr = new MapCharacter();
                mapChr.collidable = true;
                mapChr.setPosition(x, y);

                // グラフィックを設定
                var guid = s.owner.data.party.getMemberGraphic(0);
                var grp = catalog.getItemFromGuid(guid) as Common.Resource.Character;
                if (grp != null)
                {
                    mapChr.ChangeGraphic(grp, mapdata);
                    mapChr.playMotion("wait");
                }

                s.mapCharList.Add(mapChr);
                s.hero = mapChr;
            }

            if (s.hero == null)
            {
                // パーティが万が一0人でも落ちないようにするための措置
                // エディタ側でエラーにしているので、ここには基本的に来ない
                var mapChr = new MapCharacter();
                mapChr.collidable = true;
                mapChr.setPosition(x, y);
                s.mapCharList.Add(mapChr);
                s.hero = mapChr;
            }
            s.hero.mapHeroSymbol = true;

            if (height > 0)
            {
                s.hero.y = height;
            }

            // 隊列歩行用キャラを初期化
            createFollowers((int)x, (int)y);
        }

        internal bool MoveCharacter(MapCharacter chr, int dir, MapData map, bool runHit2Script, bool ignoreHeight = false)
        {
            //var s = owner;

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

            bool walked = false;
            if ((dx | dy) != 0 && !chr.IsMoving())
            {
                var hitChr = checkCharCollision(chr, dx, dy);
                int checkDir = -1;
                if (hitChr == null)
                {
                    walked = chr.Walk(dx, dy, !chr.fixDirection, map, eventHeightMap, ignoreHeight, chr.collidable);
                    if (walked)
                    {
                        owner.mapEngine.applyCharacterMoveToEventHeightMap(chr, dir);
                    }
                }
                else
                {
                    if (!chr.fixDirection)
                        chr.setDirection(dir);

                    checkDir = Util.getReverseDir(dir);
                }

                // イベントからぶつかってきた時 のスクリプトを実行
                if (!owner.owner.debugSettings.ignoreEvent && runHit2Script && (walked || hitChr == owner.hero))
                {
                    checkAndRunCollisionScriptFromEvent(chr, checkDir);
                }
            }

            return walked;
        }

        internal MapCharacter checkCharCollision(MapCharacter self, int dx, int dz)
        {
            //var s = owner;

            if (self.character is Common.Resource.MapObject)
            {
                if (!self.collidable)
                    return null;

                var points = getPoints(self);
                for (int i = 0; i < points.Count; i += 2)
                {
                    var chr = getOtherChacaterFromMap(self, points[i] + dx, points[i + 1] + dz);
                    if (chr != null && chr.collidable)
                        return chr;
                }
                return null;
            }
            else
            {
                return getOtherChacaterFromMap(self, dx, dz);
            }
        }

        private MapCharacter getOtherChacaterFromMap(MapCharacter self, int dx, int dz, bool ignoreHeight = false)
        {
            int x = (int)self.x + dx;
            int z = (int)self.z + dz;
            if (x < 0 || owner.map.Width <= x || z < 0 || owner.map.Height <= z || !self.collidable)
                return null;

            // 移動後のY座標を算出する
            float y = self.getAdjustedYPosition(owner.mapDrawer, x, self.y, z);

            var cell = eventHeightMap.get(x, z);
            foreach (var info in cell)
            {
                if (info.chr == null)
                    continue;

                if (info.chr == self)
                    continue;

                if (ignoreHeight)
                    return info.chr;

                // 1.0f は階段上下にキャラがいる可能性があるため
                var h = 1;
                if (info.col && info.chr.y < y + h && info.chr.y + info.h > y)
                    return info.chr;
            }
            return null;
        }

        public void updateEventHeightMap()
        {
            if (eventHeightMap == null ||
                owner.map.Width != eventHeightMap.width ||
                owner.map.Height != eventHeightMap.height)
            {
                eventHeightMap = new EventHeightMap();
                eventHeightMap.init(owner.map.Width, owner.map.Height);
            }

            // クリア
            eventHeightMap.clear();

            foreach (var ev in owner.mapCharList)
            {
                IntPoint r;
                r.x = (int)ev.x;
                r.y = (int)ev.z;
                r.r = 0;

                // キャラクタベースだった場合
                //int fh = (int)owner.mapDrawer.getFurnitureHeight(r.x, r.y);//起点の配置高さ
                if (!(ev.character is Common.Resource.MapObject))
                {
                    if (r.x < 0 || r.y < 0 || r.x >= eventHeightMap.width || r.y >= eventHeightMap.height)
                        continue;
                    int bh = ev.collidable ? 2 : 0;
                    eventHeightMap.add(ev, ev.collidable, bh, r.x, r.y, ev.expand);
                    continue;
                }

                // マップオブジェクトベースだった場合
                Common.Resource.MapObject mo = ev.character as Common.Resource.MapObject;
                r.Dir = ev.getDirection();
                var adjustedObjectHeightMap = owner.mapDrawer.heightMapRotate(mo, r.r);
                int hsz = mo.heightMapHalfSize;
                for (int x = -hsz; x < hsz; x++)
                {
                    for (int y = -hsz; y < hsz; y++)
                    {
                        int ax = x + r.x;//実際のマップの位置
                        int ay = y + r.y;

                        if (ax < 0 || ax >= owner.map.Width || ay < 0 || ay >= owner.map.Height) continue;

                        var bh = adjustedObjectHeightMap[x + hsz, y + hsz];//家具にあたり判定がせっていしてある
                        if (bh.height == 0 && x == 0 && y == 0)
                            bh.height = 1;

                        if (bh.height > 0 && !bh.isRoof)
                        {
                            bh.height = ev.collidable ? bh.height : 0;
                            eventHeightMap.add(ev, ev.collidable, bh.height, ax, ay, ev.expand);
                        }
                    }
                }
            }
        }

        private List<int> getPoints(MapCharacter ev)
        {
            var result = new List<int>();
            Common.Resource.MapObject mo = ev.character as Common.Resource.MapObject;
            IntPoint r;
            r.x = (int)ev.x;
            r.y = (int)ev.z;
            r.r = 0;
            r.Dir = ev.getDirection();
            var adjustedObjectHeightMap = owner.mapDrawer.heightMapRotate(mo, r.r);
            int hsz = mo.heightMapHalfSize;
            for (int x = -hsz; x < hsz; x++)
            {
                for (int y = -hsz; y < hsz; y++)
                {
                    var bh = adjustedObjectHeightMap[x + hsz, y + hsz];
                    if (bh.height > 0 && !bh.isRoof)
                    {
                        result.Add(x);
                        result.Add(y);
                    }
                }
            }
            if (result.Count == 0)
            {
                result.Add(0);
                result.Add(0);
            }
            return result;
        }

        internal void applyCharacterMoveToEventHeightMap(MapCharacter chr, int moveDir)
        {
            if (chr.getGraphic() is Common.Resource.MapObject || chr.expand)
            {
                updateEventHeightMap();
            }
            else
            {
                var nx = (int)chr.x;
                var ny = (int)chr.z;

                if (dir == 3)
                    nx += 1;
                else if (dir == 2)
                    nx -= 1;
                else if (dir == 1)
                    ny += 1;
                else if (dir == 0)
                    ny -= 1;

                if (nx >= 0 && nx < owner.map.Width && ny >= 0 && ny < owner.map.Height)
                    eventHeightMap.remove(nx, ny, chr);
                eventHeightMap.add(chr, chr.collidable, chr.collidable ? 2 : 0, (int)chr.x, (int)chr.z, chr.expand);
            }
        }

        internal void draw()
        {
            // 毒エフェクト描画
            if (poisonEffect != null && owner.playerLocked == 0 && !isViewMode())
            {
                int x, y;
                owner.GetCharacterScreenPos(owner.GetHero(), out x, out y, MapScene.EffectPosType.Head);
                poisonEffect.draw(x, y);
            }
        }

        internal MapCharacter GetMapChr(Guid guid)
        {
            foreach (var chr in owner.mapCharList)
            {
                if (chr.rom != null && chr.rom.guId == guid)
                    return chr;
            }

            return null;
        }

        internal void checkAllEvent()
        {
            foreach (var mapChr in owner.mapCharList)
            {
                if (mapChr.rom != null)
                    checkAllSheet(mapChr);
            }
        }

#if WINDOWS
        private struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

#endif //WINDOWS
    }
}

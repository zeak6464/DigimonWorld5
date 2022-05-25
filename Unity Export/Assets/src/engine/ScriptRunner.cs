using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Script = Yukar.Common.Rom.Script;
using Command = Yukar.Common.Rom.Script.Command;
using Yukar.Common;
using Microsoft.Xna.Framework;
using KeyStates = Yukar.Engine.Input.KeyStates;
using StateType = Yukar.Engine.Input.StateType;
using System.Collections;
using SharpKmyMath;
using Yukar.Common.Rom;
using Yukar.Common.GameData;

#if WINDOWS
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;
#endif

namespace Yukar.Engine
{
    class ScriptRunner
    {
        private const int COND_EQUAL = 0;
        private const int COND_NOT_EQUAL = 1;

        internal MapCharacter mapChr;
        internal MapCharacter followChr;    // コモンイベントの呼び出し元イベントを操作する用

        internal const int HUNDRED_NS_TO_MS = 10000;
        internal const int DEFAULT_TIMEOUT = 30 * HUNDRED_NS_TO_MS;    // 通常は 30ms 以上処理してたら脱出する
        internal const int INITIAL_TIMEOUT = 5000 * HUNDRED_NS_TO_MS;  // マップ読み込み時のみ、 5s までの処理を許可する
        internal static int sTimeoutTick = DEFAULT_TIMEOUT;

        private MapCharacter selfChr
        {
            get { return mapChr.isCommonEvent ? followChr : mapChr; }
        }

        internal Script script;
        internal MapScene owner;
        public enum ScriptState
        {
            Stopped,
            Paused,
            Running,
            DeepPaused, // コマンドによる停止と通常の停止が組み合わさった時
        }

#if WINDOWS
        private static Dictionary<string, Assembly> sAsmDict = new Dictionary<string, Assembly>();
#endif

        // ↓この3つは保存しないとダメ
        public ScriptState state = ScriptState.Stopped;
        private Stack<int> stack = new Stack<int>();
        private int cur;
        internal void save(Common.GameData.Event ev)
        {
            ev.scriptState = (int)state;
            ev.scriptStack = stack;
            ev.scriptCur = cur;
        }
        internal void load(Common.GameData.Event stat)
        {
            if (!isParallelTriggers())
            {
                if ((ScriptState)stat.scriptState == ScriptState.Running && state == ScriptState.Stopped)
                    owner.LockControl(10, mapChr.rom?.name ?? "");
                else if ((ScriptState)stat.scriptState == ScriptState.Stopped && state == ScriptState.Running)
                    owner.UnlockControl(10, mapChr.rom?.name ?? "");
            }

            state = (ScriptState)stat.scriptState;
            stack = stat.scriptStack;
            cur = stat.scriptCur;
        }

        internal bool isParallelTriggers()
        {
            return script.trigger == Script.Trigger.NONE ||
               script.trigger == Script.Trigger.PARALLEL ||
               script.trigger == Script.Trigger.PARALLEL_MV ||
               script.trigger == Script.Trigger.AUTO_PARALLEL ||
               script.trigger == Script.Trigger.BATTLE_PARALLEL;
        }

        private Stack<int>[] labels = new Stack<int>[Script.MAX_LABEL];
        private Func<bool> waiter;
        private List<Action> finalizerQueue = new List<Action>();       // コマンドを中断する際に後始末が必要な場合に使うもの
        private Command curCommand;

        private int loadedSeId = -1;    // 最後に鳴らしたSE
        private EffectDrawer efDrawer;  // 最後に再生したバトルエフェクト

        public enum RemoveTrigger
        {
            NONE,
            ON_EXIT,
            ON_COMPLETE_CURRENT_LINE,
        }
        public RemoveTrigger removeTrigger = RemoveTrigger.NONE;
        internal Guid key;

        public Script.Trigger Trigger
        {
            get { return script.trigger; }
        }

        public ScriptRunner(MapScene owner, MapCharacter mapChr, Script script)
        {
            this.owner = owner;
            this.mapChr = mapChr;
            this.script = script;
            if (script != null)
                this.key = script.guId;
        }

        internal bool Run()
        {
            if (state != ScriptState.Stopped)
                return false;

            state = ScriptState.Running;
            cur = -1;
            waiter = null;

            if (script.trigger == Script.Trigger.TALK && mapChr.character is Common.Resource.Character)
            {
                var hero = owner.GetHero();
                mapChr.setDirection(calcDir(hero, mapChr));
            }
            if (!isParallelTriggers())
            {
                //var hero = owner.GetHero();
                owner.LockControl(11, mapChr.rom?.name ?? "");
            }

            /*
            if (script.trigger == Script.Trigger.TALK)
            {
                var hero = owner.GetHero();
                mapChr.setDirection(calcDir(hero, mapChr));
                owner.PauseAllScript(mapChr, this, true);
                owner.LockControl();
            }
            else if (script.trigger == Script.Trigger.HIT)
            {
                owner.LockControl();
            }
            */

            return true;
        }

        internal void finalize()
        {
            if (state == ScriptState.Running)
            {
                if (script.trigger != Script.Trigger.NONE &&
                    script.trigger != Script.Trigger.PARALLEL &&
                    script.trigger != Script.Trigger.PARALLEL_MV)
                    owner.UnlockControl(11, mapChr.rom?.name ?? "");

                // 会話用スプライトを非表示にする
                hideDialogueCharacters();

                while (finalizerQueue.Count > 0)
                {
                    finalizerQueue.First()();
                    finalizerQueue.RemoveAt(0);
                }
            }

            unloadSound();
            unloadBattleEffect();

            state = ScriptState.Stopped;
        }

        private void unloadBattleEffect()
        {
            if (efDrawer != null)
            {
                var ef = efDrawer;
                float effectCount = 0;
                owner.owner.pushTask(() =>
                {
                    // エフェクトがまだ再生中だったら待つ
                    //if (!ef.isEndPlaying)
                    //    return true;

                    // 再生終了後、10秒くらい待つ
                    effectCount += GameMain.getElapsedTime();
                    if (effectCount < 10)
                        return true;

                    ef.finalize();
                    return false;
                });
                efDrawer = null;
            }
        }

        private void unloadSound()
        {
            if (loadedSeId != -1)
            {
                int seId = loadedSeId;
                owner.owner.pushTask(() =>
                {
                    // SEがまだ再生中だったら待つ
                    if (Audio.IsSePlaying(seId))
                        return true;

                    Audio.UnloadSound(seId);
                    return false;
                });
                loadedSeId = -1;
            }
        }

        internal bool Update()
        {
            // 実行中以外はリターン
            if (state != ScriptState.Running)
                return false;

            // ウェイト中はリターン
            if (waiter != null && waiter())
                return false;

            // 実行前状態かどうか
            if (cur < 0)
            {
                if (removeTrigger != RemoveTrigger.NONE)
                    cur = script.commands.Count;    // すでに removeOnExit がセットされていたら、最後まで実行した事にする
                else
                    cur = 0;                        // 実行中状態にする
            }

            // スクリプト終了か次がセリフコマンドじゃなかったら会話用スプライトを非表示にする
            if ((script.commands.Count <= cur || script.commands[cur].type != Command.FuncType.DIALOGUE))
            {
                hideDialogueCharacters();
            }

            // 現在時刻をとっておく
            long now = DateTime.Now.Ticks;

            // 実行
            waiter = null;
            while (waiter == null)
            {
                // 最後まで実行したら終了
                if (isFinished() || removeTrigger == RemoveTrigger.ON_COMPLETE_CURRENT_LINE)
                {
                    if (script.trigger != Script.Trigger.NONE &&
                        script.trigger != Script.Trigger.PARALLEL &&
                        script.trigger != Script.Trigger.PARALLEL_MV &&
                        script.trigger != Script.Trigger.BATTLE_PARALLEL)
                        owner.UnlockControl(12, mapChr.rom?.name ?? "");
                    /*
                    if (script.trigger == Script.Trigger.TALK)
                    {
                        owner.ResumeAllScript(mapChr, true);
                        owner.UnlockControl();
                    }
                    else if (script.trigger == Script.Trigger.HIT)
                    {
                        owner.UnlockControl();
                    }
                    */

                    state = ScriptState.Stopped;

                    // 繰り返し実行だった場合、もう一度開始する
                    if ((script.trigger == Script.Trigger.AUTO_REPEAT || script.trigger == Script.Trigger.PARALLEL ||
                        script.trigger == Common.Rom.Script.Trigger.BATTLE_PARALLEL)
                        && removeTrigger == RemoveTrigger.NONE)
                    {
                        Run();
                        return true;
                    }

                    // 破棄を予約されていたら自分を破棄する
                    if (removeTrigger != RemoveTrigger.NONE)
                    {
                        finalize();
                        owner.runnerDic.Remove(key);
                    }

                    break;
                }

                // 前回のウェイトから timeoutTick 以上経っていたら強制的にbreak
                if (DateTime.Now.Ticks - now > sTimeoutTick)
                    break;

                curCommand = script.commands[cur];
                switch (curCommand.type)
                {
                    // IF
                    case Command.FuncType.IF:
                        if (!FuncIf())
                        {
                            // 当てはまらなかったらELSEかENDIFまで飛ぶ
                            SearchElseOrEndIf();
                        }
                        break;

                    // ループ開始位置をスタックに積む
                    case Command.FuncType.LOOP:
                        stack.Push(cur);
                        break;

                    // ラベル定義
                    case Command.FuncType.LABEL:
                        if (curCommand.attrList.Count == 1)
                        {
                            var attr = curCommand.attrList[0] as Script.IntAttr;
                            PushLabel(attr.value, stack);
                        }
                        break;

                    // ジャンプ
                    case Command.FuncType.JUMP:
                        if (curCommand.attrList.Count == 2)
                        {
                            var jumpType = curCommand.attrList[0] as Script.IntAttr;
                            var attr = curCommand.attrList[1] as Script.IntAttr;
                            if (jumpType.value == 0)
                            {
                                JumpLabel(attr.value);
                            }
                            else
                            {
                                JumpLabel(owner.owner.data.system.GetVariable(attr.value, mapChr.rom.guId));
                            }
                        }
                        break;

                    // ループ脱出
                    case Command.FuncType.BREAK:
                        SearchEndLoop();
                        break;

                    // ほかのスクリプトを実行
                    case Command.FuncType.EXEC:
                        if (curCommand.attrList.Count == 2)
                        {
                            var guidAttr = curCommand.attrList[0] as Script.GuidAttr;
                            
                            var btlEvtCtl = owner as BattleEventController;

                            // バトルだったら先にシート遷移をチェックする
                            if (btlEvtCtl != null)
                            {
                                GameMain.instance.mapScene.mapEngine.checkAllSheet(guidAttr.value);
                            }

                            var runner = owner.GetScriptRunner(guidAttr.value);

                            var intAttr = curCommand.attrList[1] as Script.IntAttr;

                            // ランナーが取得できなかった（条件を満たしたシートが1つもない）場合は脱出
                            if (runner == null)
                                break;

                            runner.followChr = selfChr;
                            bool result = runner.Run();

                            // バトルだったら借りてるスクリプトのリストに入れる
                            if (result && btlEvtCtl != null)
                            {
                                btlEvtCtl.start(guidAttr.value);
                            }

                            // 実行に失敗したか、ウェイトをオンにしない場合は脱出
                            if (!result || intAttr.value == 0)
                                break;

                            // 実行キューのトップに持ってくる
                            //owner.runnerDic.bringToFront(runner);

                            // 実行完了までウェイト
                            waiter = () =>
                            {
                                if (runner.state == ScriptState.Running)
                                    return true;

                                return false;
                            };
                        }
                        break;

                    // 指定のスクリプトを止める
                    case Command.FuncType.PAUSE:
                        if (curCommand.attrList.Count > 0)
                        {
                            var guidAttr = curCommand.attrList[0] as Script.GuidAttr;

                            if (guidAttr.value == Guid.Empty)
                            {
                                owner.PauseAllScript(mapChr, this);
                            }
                            else
                            {
                                var runner = owner.GetScriptRunner(guidAttr.value);
                                runner.Pause();
                            }
                        }
                        break;

                    // 指定のスクリプトを再開する
                    case Command.FuncType.RESUME:
                        if (curCommand.attrList.Count > 0)
                        {
                            var guidAttr = curCommand.attrList[0] as Script.GuidAttr;

                            if (guidAttr.value == Guid.Empty)
                            {
                                owner.ResumeAllScript(mapChr);
                            }
                            else
                            {
                                var runner = owner.GetScriptRunner(guidAttr.value);
                                runner.Resume();
                            }
                        }
                        break;

                    // スクリプトを終了する
                    case Command.FuncType.END:
                        cur = script.commands.Count;
                        break;

                    // 指定時間ウェイト
                    case Command.FuncType.WAIT:
                        if (curCommand.attrList.Count == 2)
                        {
                            var intAttr = curCommand.attrList[0] as Script.IntAttr;
                            float waitFrame = intAttr.value;

                            intAttr = curCommand.attrList[1] as Script.IntAttr;
                            if (intAttr.value == 1)  // 秒に変換
                            {
                                waitFrame = waitFrame * 60 / 1000;
                            }

                            waiter = () =>
                            {
                                waitFrame -= GameMain.getRelativeParam60FPS();
                                if (waitFrame > 0)
                                    return true;

                                return false;
                            };
                        }
                        break;

                    // 指定方向を向く
                    case Command.FuncType.ROTATE:
                        if (selfChr == null)
                            break;
                        FuncRotate(selfChr, false);
                        break;

                    // 指定方向へ歩く
                    case Command.FuncType.WALK:
                        if (selfChr == null)
                            break;
                        FuncMove(selfChr, false);
                        break;

                    // 歩行速度変更
                    case Command.FuncType.WALKSPEED:
                        if (selfChr == null)
                            break;
                        if (curCommand.attrList.Count == 1)
                        {
                            var intAttr = curCommand.attrList[0] as Script.IntAttr;
                            selfChr.ChangeSpeed(intAttr.value);
                        }
                        break;

                    // 主人公の歩行速度変更
                    case Command.FuncType.PLWALKSPEED:
                        if (curCommand.attrList.Count == 1)
                        {
                            var intAttr = curCommand.attrList[0] as Script.IntAttr;
                            owner.owner.data.system.moveSpeed = intAttr.value;
                            owner.GetHeroForBattle().ChangeSpeed(owner.owner.data.system.moveSpeed);
                        }
                        break;

                    // プライオリティ変更
                    case Command.FuncType.PRIORITY:
                        if (selfChr == null)
                            break;
                        if (curCommand.attrList.Count == 1)
                        {
                            var intAttr = curCommand.attrList[0] as Script.IntAttr;
                            selfChr.collidable = intAttr.value == 0 ? true : false;
                        }
                        break;

                    // グラフィック変更
                    case Command.FuncType.GRAPHIC:
                        if (selfChr == null)
                            break;
                        if (curCommand.attrList.Count >= 1)
                        {
                            var guidAttr = curCommand.attrList[0] as Script.GuidAttr;
                            owner.ChangeGraphic(selfChr, guidAttr.value);
                            if (curCommand.attrList.Count >= 2)
                                selfChr.playMotion(curCommand.attrList[1].GetString());
                        }
                        break;

                    // イベントの位置変更
                    case Command.FuncType.MOVE:
                        if (selfChr == null)
                            break;
                        if (curCommand.attrList.Count >= 2)
                        {
                            int curAttr = 0;
                            int x = 0, y = 0;
                            if (curCommand.attrList[curAttr] is Script.IntAttr)
                            {
                                var intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
                                x = intAttr.value;
                                intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
                                y = intAttr.value;
                            }
                            else
                            {
                                curAttr++;
                                x = owner.owner.data.system.GetVariable(curCommand.attrList[curAttr++].GetInt(), mapChr.rom.guId);
                                y = owner.owner.data.system.GetVariable(curCommand.attrList[curAttr++].GetInt(), mapChr.rom.guId);
                            }

                            var guid = curCommand.attrList[curAttr++].GetGuid();
                            MapCharacter chr = null;
                            if (guid != Guid.Empty)
                            {
                                // 一致するキャラを探す
                                chr = owner.mapEngine.GetMapChr(guid);
                            }

                            if (chr == null)
                                chr = selfChr;

                            chr.setPosition(x, y);
                            if (curCommand.attrList.Count > curAttr)
                            {
                                int dir = curCommand.attrList[curAttr++].GetInt() - 1;
                                if (dir >= 0)
                                    chr.setDirection(dir, true);
                            }
                        }
                        break;

                    // プレイヤーの向き変更
                    case Command.FuncType.PLROTATE:
                        FuncRotate(owner.GetHeroForBattle(), (owner.CurrentCameraMode == Common.Rom.Map.CameraControlMode.VIEW));
                        break;

                    // プレイヤーの移動
                    case Command.FuncType.PLWALK:
                        FuncMove(owner.GetHeroForBattle(), (owner.CurrentCameraMode == Common.Rom.Map.CameraControlMode.VIEW));
                        break;

                    // 操作禁止・解除
                    case Command.FuncType.SW_PLLOCK:
                        {
                            var doLock = true;
                            if (curCommand.attrList.Count >= 1)
                            {
                                doLock = curCommand.attrList[0].GetInt() != 0;
                            }
                            if (doLock)
                                owner.playerLocked |= MapScene.PLAYER_LOCK_BY_EVENT;
                            else
                                owner.playerLocked &= ~MapScene.PLAYER_LOCK_BY_EVENT;
                        }
                        break;

                    // 操作禁止解除(基本的には廃止)
                    case Command.FuncType._UNLOCK:
                        owner.playerLocked &= ~MapScene.PLAYER_LOCK_BY_EVENT;
                        break;

                    // カメラ操作禁止・解除
                    case Command.FuncType.SW_CAMLOCK:
                        {
                            var doLock = true;
                            if (curCommand.attrList.Count > 0)
                            {
                                doLock = curCommand.attrList[0].GetInt() != 0;
                            }
                            var doModeLock = true;
                            if (curCommand.attrList.Count > 1)
                            {
                                doModeLock = curCommand.attrList[1].GetInt() != 0;
                            }
                            owner.setCamLockState(doLock, doModeLock);
                            if (curCommand.attrList.Count > 4)
                            {
                                owner.setCamControlLock(
                                    curCommand.attrList[2].GetBool(),
                                    curCommand.attrList[3].GetBool(),
                                    curCommand.attrList[4].GetBool());
                            }
                        }
                        break;

                    // プレイヤーの場所移動
                    case Command.FuncType.PLMOVE:
                        if (curCommand.attrList.Count >= 2)
                        {
                            int curAttr = 0;
                            RomItem mapRom = null;
                            int x = 0, y = 0;

                            if (curCommand.attrList[curAttr] is Script.GuidAttr)
                            {
                                var guidAttr = curCommand.attrList[curAttr++] as Script.GuidAttr;
                                var intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
                                x = intAttr.value;
                                intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
                                y = intAttr.value;
                                mapRom = owner.owner.catalog.getItemFromGuid(guidAttr.value);
                            }
                            else
                            {
                                x = owner.owner.data.system.GetVariable(curCommand.attrList[curAttr++].GetInt(), mapChr.rom.guId);
                                y = owner.owner.data.system.GetVariable(curCommand.attrList[curAttr++].GetInt(), mapChr.rom.guId);
                            }

                            // マップの指定がなかった場合は、イベント自身が配置されているマップから判別する
                            if (mapRom == null && mapChr != null && mapChr.rom != null)
                            {
                                mapRom = Util.searchEventContainsMap(owner.owner.catalog, mapChr.rom.guId);
                            }

                            int dir = -1;
                            if (curCommand.attrList.Count > curAttr)
                            {
                                dir = curCommand.attrList[curAttr++].GetInt() - 1;
                            }

                            if (!owner.isBattle && owner.map != null && mapRom != null && mapRom.guId != owner.map.guId)
                            {
                                // マップが違っていたら別マップへのワープ
                                if (ScriptMutexLock(ref owner.excludedScript))
                                    break;
                                owner.ExclusionAllEvents(this, true);
                                owner.ReservationChangeMap(mapRom.guId, x, y);

                                waiter = () =>
                                {
                                    if (dir >= 0)
                                    {
                                        owner.GetHero().setDirection(dir, true);

                                        if (owner.CurrentCameraMode == Common.Rom.Map.CameraControlMode.VIEW)
                                        {
                                            owner.resetViewCamera();
                                        }
                                    }
                                    return false;
                                };
                            }
                            else
                            {
                                // マップが同じだったら、位置だけのワープ
                                if(owner.mapEngine != null) owner.mapEngine.clearPosLogList();
                                owner.GetHeroForBattle().setPosition(x, y);

                                if (dir >= 0)
                                {
                                    owner.GetHeroForBattle().setDirection(dir, true);

                                    if (owner.CurrentCameraMode == Common.Rom.Map.CameraControlMode.VIEW)
                                    {
                                        owner.resetViewCamera();
                                    }
                                }
                            }
                        }
                        break;

                    // BGM再生
                    case Command.FuncType.PLAYBGM:
                        if (curCommand.attrList.Count >= 1)
                        {
                            // ファンファーレ用のタスクをクリアする
                            owner.owner.clearTask("Fanfare");

                            var guidAttr = curCommand.attrList[0] as Script.GuidAttr;
                            var sound = owner.owner.catalog.getItemFromGuid(guidAttr.value) as Common.Resource.Bgm;
                            float volume = 1.0f;
                            float tempo = 1.0f;
                            if (curCommand.attrList.Count > 1)
                            {
                                volume = (float)curCommand.attrList[1].GetInt() / 100;
                                tempo = (float)curCommand.attrList[2].GetInt() / 100;
                            }
                            if (curCommand.attrList.Count > 3 && curCommand.attrList[3].GetInt() > 0)
                            {
                                sound = owner.owner.catalog.getItemFromGuid(owner.map.mapBgm) as Common.Resource.Bgm;
                            }
                            if (sound == null)
                            {
                                // BGMを止める
                                Audio.StopBgm();
                            }
                            else
                            {
                                Audio.PlayBgm(sound, volume, tempo);
                            }
                        }
                        break;

                    // BGS再生
                    case Command.FuncType.PLAYBGS:
                        if (curCommand.attrList.Count >= 1)
                        {
                            var guidAttr = curCommand.attrList[0] as Script.GuidAttr;
                            var sound = owner.owner.catalog.getItemFromGuid(guidAttr.value) as Common.Resource.Bgs;
                            float volume = 1.0f;
                            float tempo = 1.0f;
                            if (curCommand.attrList.Count > 1)
                            {
                                volume = (float)curCommand.attrList[1].GetInt() / 100;
                                tempo = (float)curCommand.attrList[2].GetInt() / 100;
                            }
                            if (curCommand.attrList.Count > 3 && curCommand.attrList[3].GetInt() > 0)
                            {
                                sound = owner.owner.catalog.getItemFromGuid(owner.map.mapBgm) as Common.Resource.Bgs;
                            }
                            if (sound == null)
                            {
                                // BGMを止める
                                Audio.StopBgs();
                            }
                            else
                            {
                                Audio.PlayBgs(sound, volume, tempo);
                            }
                        }
                        break;

                    // SE再生
                    case Command.FuncType.PLAYSE:
                        if (curCommand.attrList.Count >= 1)
                        {
                            unloadSound();

                            var guidAttr = curCommand.attrList[0] as Script.GuidAttr;
                            if (guidAttr.value != Guid.Empty)
                            {
                                float volume = 1.0f;
                                float tempo = 1.0f;
                                if (curCommand.attrList.Count > 1)
                                {
                                    volume = (float)curCommand.attrList[1].GetInt() / 100;
                                    tempo = (float)curCommand.attrList[2].GetInt() / 100;
                                }

                                var sound = owner.owner.catalog.getItemFromGuid(guidAttr.value) as Common.Resource.Se;
                                loadedSeId = Audio.LoadSound(sound);
                                Audio.PlaySound(loadedSeId, 0f, volume, tempo);
                            }

                            // 読み込みでタイムアウトになる可能性があるので、タイムアウト対策
                            now = DateTime.Now.Ticks;
                        }
                        break;

                    // スプライト非表示
                    case Command.FuncType.SPHIDE:
                        if (curCommand.attrList.Count >= 1)
                        {
                            int curAttr = 0;
                            int spIndex = curCommand.attrList[curAttr++].GetInt();

                            bool fadeOut = false;
                            bool resetZoomOnFadeOut = true;
                            if (curCommand.attrList.Count > curAttr)
                            {
                                fadeOut = curCommand.attrList[curAttr++].GetInt() == 1;

                                if (curCommand.attrList.Count > curAttr)
                                {
                                    resetZoomOnFadeOut = curCommand.attrList[curAttr++].GetInt() == 1;
                                }
                            }

                            if (fadeOut)
                            {
                                int zoomX = 100, zoomY = 100;
                                if(!resetZoomOnFadeOut)
                                    owner.spManager.GetScale(spIndex, out zoomX, out zoomY);
                                owner.spManager.Move(spIndex, zoomX, zoomY, 30, new Color(0, 0, 0, 0));

                                waiter = () =>
                                {
                                    if (!owner.spManager.isMoving(spIndex))
                                    {
                                        owner.spManager.Hide(spIndex);
                                        return false;
                                    }

                                    return true;
                                };
                            }
                            else
                            {
                                owner.spManager.Hide(spIndex);
                            }
                        }
                        break;

                    case Command.FuncType.SPMOVE:
                        if (curCommand.attrList.Count > 0)
                        {
                            int curAttr = 0;
                            var intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
                            int spIndex = intAttr.value;
                            intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
                            int zoom = intAttr.value;
                            intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
                            float waitFrame = intAttr.value;
                            intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
                            if (intAttr.value == 1)  // 秒に変換
                            {
                                waitFrame = waitFrame * 60 / 1000;
                            }
                            intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                            var color = new Color(
                                (intAttr.value & 0xFF000000) >> 24,
                                (intAttr.value & 0x00FF0000) >> 16,
                                (intAttr.value & 0x0000FF00) >> 8,
                                (intAttr.value & 0x000000FF));
                            intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                            int x = 0, y = 0;
                            var posType = (Command.PosType)intAttr.value;
                            switch (posType)
                            {
                                case Yukar.Common.Rom.Script.Command.PosType.NO_MOVE:   // これには来ない
                                    owner.spManager.Move(spIndex, zoom, waitFrame, color);
                                    break;
                                case Yukar.Common.Rom.Script.Command.PosType.CONSTANT:
                                    intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                                    x = intAttr.value;
                                    intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                                    y = intAttr.value;
                                    break;
                                case Yukar.Common.Rom.Script.Command.PosType.VARIABLE:
                                    intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                                    x = owner.owner.data.system.GetVariable(intAttr.value, mapChr.rom.guId);
                                    intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                                    y = owner.owner.data.system.GetVariable(intAttr.value, mapChr.rom.guId);
                                    break;
                            }

                            if (posType != Command.PosType.NO_MOVE)
                            {
                                int zoom2 = zoom;
                                if (curCommand.attrList.Count > curAttr)
                                    zoom2 = curCommand.attrList[curAttr++].GetInt();
                                owner.spManager.Move(spIndex, zoom, zoom2, waitFrame, color, x, y);
                            }

                            // 移動完了までウェイト
                            waiter = () =>
                            {
                                if (owner.spManager.isMoving(spIndex))
                                    return true;

                                return false;
                            };
                        }
                        break;

                    case Command.FuncType.SPPICTURE:
                        if (curCommand.attrList.Count > 0)
                        {
                            int curAttr = 0;
                            var intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
                            int spIndex = intAttr.value;
                            var guidAttr = curCommand.attrList[curAttr++] as Script.GuidAttr;
                            intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
                            int zoom = intAttr.value;
                            intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                            var color = new Color(
                                (int)((intAttr.value & 0xFF000000) >> 24),
                                (int)((intAttr.value & 0x00FF0000) >> 16),
                                (int)((intAttr.value & 0x0000FF00) >> 8),
                                (int)((intAttr.value & 0x000000FF)));
                            var startColor = color;
                            intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                            int x, y, type;
                            switch ((Yukar.Common.Rom.Script.Command.PosType)intAttr.value)
                            {
                                case Yukar.Common.Rom.Script.Command.PosType.NO_MOVE:
                                    intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                                    type = -2; x = y = 0;
                                    owner.spManager.ShowPicture(spIndex, guidAttr.value, zoom, startColor, type);
                                    break;
                                case Yukar.Common.Rom.Script.Command.PosType.CONSTANT:
                                    intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                                    x = intAttr.value;
                                    intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                                    y = intAttr.value;
                                    intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                                    type = intAttr.value;
                                    break;
                                case Yukar.Common.Rom.Script.Command.PosType.VARIABLE:
                                    intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                                    x = owner.owner.data.system.GetVariable(intAttr.value, mapChr.rom.guId);
                                    intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                                    y = owner.owner.data.system.GetVariable(intAttr.value, mapChr.rom.guId);
                                    intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                                    type = intAttr.value;
                                    break;
                                default:
                                    type = x = y = 0;   // ここには来ないが、一応
                                    break;
                            }

                            if (type != -2)
                            {
                                bool fadeIn = false;
                                if (curCommand.attrList.Count > curAttr)
                                    fadeIn = curCommand.attrList[curAttr++].GetInt() == 1;
                                if (fadeIn)
                                    startColor = new Color(0, 0, 0, 0);

                                int zoom2 = zoom;
                                if (curCommand.attrList.Count > curAttr)
                                    zoom2 = curCommand.attrList[curAttr++].GetInt();

                                owner.spManager.ShowPicture(spIndex, guidAttr.value, zoom, zoom2, startColor, type, x, y, false);

                                if (fadeIn)
                                {
                                    owner.spManager.Move(spIndex, zoom, zoom2, 30, color);
                                    waiter = () =>
                                    {
                                        return owner.spManager.isMoving(spIndex);
                                    };
                                }
                            }
                        }
                        break;
                    case Command.FuncType.SPTEXT:
                        if (curCommand.attrList.Count > 0)
                        {
                            int curAttr = 0;
                            var intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
                            int spIndex = intAttr.value;
                            var stringAttr = curCommand.attrList[curAttr++] as Script.StringAttr;
                            intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
                            int zoom = intAttr.value;
                            intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                            var color = new Color(
                                (intAttr.value & 0xFF000000) >> 24,
                                (intAttr.value & 0x00FF0000) >> 16,
                                (intAttr.value & 0x0000FF00) >> 8,
                                (intAttr.value & 0x000000FF));
                            intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                            int x, y;
                            switch ((Yukar.Common.Rom.Script.Command.PosType)intAttr.value)
                            {
                                case Yukar.Common.Rom.Script.Command.PosType.NO_MOVE:
                                    intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                                    owner.spManager.ShowText(spIndex, stringAttr.value, zoom, color, intAttr.value);
                                    break;
                                case Yukar.Common.Rom.Script.Command.PosType.CONSTANT:
                                    intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                                    x = intAttr.value;
                                    intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                                    y = intAttr.value;
                                    intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                                    owner.spManager.ShowText(spIndex, stringAttr.value, zoom, color, x, y, intAttr.value);
                                    break;
                                case Yukar.Common.Rom.Script.Command.PosType.VARIABLE:
                                    intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                                    x = owner.owner.data.system.GetVariable(intAttr.value, mapChr.rom.guId);
                                    intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                                    y = owner.owner.data.system.GetVariable(intAttr.value, mapChr.rom.guId);
                                    intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                                    owner.spManager.ShowText(spIndex, stringAttr.value, zoom, color, x, y, intAttr.value);
                                    break;
                            }
                        }
                        break;

                    // エフェクト表示
                    case Command.FuncType.EFFECT:
                        FuncEffect();
                        break;

                    // 画面効果
                    case Command.FuncType.SCREEN:
                        FuncScreenEffect();
                        break;

                    // 文章表示
                    case Command.FuncType.MESSAGE:
                        if (curCommand.attrList.Count >= 2)
                        {
                            if (ScriptMutexLock(ref owner.excludedScript))
                                break;

                            int curAttr = 0;
                            var stringAttr = curCommand.attrList[curAttr++] as Script.StringAttr;
                            var intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;

                            var window = MessageWindow.WindowType.MESSAGE;
                            if (curCommand.attrList.Count > curAttr &&
                                curCommand.attrList[curAttr++].GetInt() == 1)
                                window = MessageWindow.WindowType.NONE;
                            int id = owner.PushMessage(stringAttr.value, intAttr.value, window);

                            // 表示完了までウェイト
                            waiter = () =>
                            {
                                if (owner.IsQueuedMessage(id))
                                    return true;

                                ScriptMutexUnlock(ref owner.excludedScript);
                                return false;
                            };
                        }
                        break;

                    // 選択肢表示
                    case Command.FuncType.CHOICES:
                        if (curCommand.attrList.Count > 0)
                        {
                            if (ScriptMutexLock(ref owner.excludedScript))
                                break;

                            int curAttr = 0;
                            var intAttr = curCommand.attrList[curAttr++] as Script.IntAttr; // 選択肢の数
                            if (curCommand.attrList.Count <= intAttr.value)
                                break;

                            var strs = new string[intAttr.value];
                            for (int i = 0; i < intAttr.value; i++)
                            {
                                var stringAttr = curCommand.attrList[curAttr++] as Script.StringAttr;
                                strs[i] = owner.owner.replaceForFormat(stringAttr.value);
                            }

                            // 選択肢の位置を反映
                            int selectionPos = ChoicesWindow.SELECTION_POS_CENTER;
                            if (curCommand.attrList.Count > curAttr)
                            {
                                selectionPos = curCommand.attrList[curAttr++].GetInt();
                            }
                            ChoicesWindow.nextPosition = selectionPos;
                            ChoicesWindow.topMarginForMoneyWindow = false;

                            owner.ShowChoices(strs);

                            // 表示完了までウェイト
                            waiter = () =>
                            {
                                var result = owner.GetChoicesResult();
                                if (result == ChoicesWindow.CHOICES_SELECTING)
                                    return true;

                                // result に従って分岐する
                                if (result > 0)
                                {
                                    SearchBranch(result);
                                }

                                ScriptMutexUnlock(ref owner.excludedScript);
                                return false;
                            };
                        }
                        break;

                    // 変数操作
                    case Command.FuncType.VARIABLE:
                    case Command.FuncType.HLVARIABLE:
                    case Command.FuncType.BTL_VARIABLE:
                        FuncVariable();
                        break;

                    // スイッチ操作
                    case Command.FuncType.SWITCH:
                        if (curCommand.attrList.Count == 2)
                        {
                            int curAttr = 0;
                            var intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
                            int index = intAttr.value;
                            intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
                            switch (intAttr.value)
                            {
                                case 0: // オン
                                    owner.owner.data.system.SetSwitch(index, true, mapChr.rom.guId);
                                    break;
                                case 1: // オフ
                                    owner.owner.data.system.SetSwitch(index, false, mapChr.rom.guId);
                                    break;
                                case 2: // 反転
                                    owner.owner.data.system.SetSwitch(index, !owner.owner.data.system.GetSwitch(index, mapChr.rom.guId), mapChr.rom.guId);
                                    break;
                            }
                        }
                        break;

                    // アイテムの増減
                    case Command.FuncType.ITEM:
                        FuncItem();
                        break;

                    // お金の増減
                    case Command.FuncType.MONEY:
                        FuncMoney();
                        break;

                    // ステータス操作
                    case Command.FuncType.STATUS:
                        FuncStatus();
                        break;

                    // 戦闘開始(もう使わない)
                    case Command.FuncType.BATTLE:
                        {
                            if (owner.isBattle || owner.isGameOver)
                                break;

                            int curAttr = 0;
                            var intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;

                            int monsterNum = intAttr.value;     // モンスターの数
                            var monsterList = new List<Guid>(); // モンスターのGUIDリスト

                            for (int i = 0; i < monsterNum; i++)
                            {
                                var guidAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.GuidAttr;
                                monsterList.Add(guidAttr.value);
                            }

                            owner.battleSequenceManager.BattleStartEvents += owner.ShowBattleStartMessage;
                            owner.battleSequenceManager.BattleResultWinEvents += owner.mapEngine.PlayBattleWinBGM;
                            //owner.battleSequenceManager.BattleResultLoseGameOverEvents += owner.mapEngine.BattleLoseGameover;
                            owner.battleSequenceManager.BattleResultEscapeEvents += owner.ShowBattleEscapeMessage;

                            owner.StartBattleMode(this);

                            bool escapeAvailable = false;
                            Common.Resource.Bgm battleBgm = null;
                            if (curCommand.attrList.Count > curAttr)
                            {
                                escapeAvailable = curCommand.attrList[curAttr++].GetInt() == 0;
                            }
                            if (curCommand.attrList.Count > curAttr)
                            {
                                battleBgm = owner.owner.catalog.getItemFromGuid(curCommand.attrList[curAttr++].GetGuid()) as Common.Resource.Bgm;
                            }
                            if (battleBgm == null)
                                battleBgm = owner.getMapBattleBgm();

                            owner.mapEngine.PlayBattleBGM(battleBgm, owner.getMapBattleBgs());

                            // ここで負けると戦闘不能になる戦闘を呼び出し
                            owner.battleSequenceManager.BattleStart(owner.owner.data.party, monsterList.ToArray(), owner.battleSetting, escapeAvailable);

                            // 戦闘完了までウェイト
                            waiter = () =>
                            {
                                // ここで false を返すと、マップ画面のスクリプト停止状態が解除されます。
                                // 戦闘画面の状態を監視して、戦闘が終わったら false を返すように実装してみてください。
                                if (owner.battleSequenceManager.BattleResult == BattleSequenceManager.BattleResultState.NonFinish || owner.battleSequenceManager.IsPlayingBattleEffect)
                                {
                                    return true;
                                }
                                else
                                {
                                    owner.mapEngine.ResumeMapBGM();
                                    owner.EndBattleMode();
                                    return owner.battleSequenceManager.BattleResult == BattleSequenceManager.BattleResultState.Lose_GameOver;
                                }
                            };
                        }
                        break;

                    // お店
                    case Command.FuncType.SHOP:
                        if (CheckScriptMutexLock(ref owner.excludedScript))
                            break;

                        FuncShop();
                        break;

                    // 宿屋
                    case Command.FuncType.INN:
                        if (CheckScriptMutexLock(ref owner.excludedScript))
                            break;

                        FuncInn();
                        break;

                    // セーブ画面
                    case Command.FuncType.SAVE:
                        if (owner.isBattle)
                            break;

                        if (ScriptMutexLock(ref owner.excludedScript))
                            break;

                        owner.LockControl(0, mapChr.rom?.name ?? "");
                        owner.menuWindow.ShowSaveScreen();

                        // メニューを閉じるまでウェイト
                        waiter = () =>
                        {
                            if (owner.menuWindow.IsClosing())
                                return true;

                            ScriptMutexUnlock(ref owner.excludedScript);
                            owner.UnlockControl(0, mapChr.rom?.name ?? "");
                            return false;
                        };
                        break;

                    // ゲーム終了
                    case Command.FuncType.EXIT:
                        if (ScriptMutexLock(ref owner.excludedScript))
                            break;

                        if (curCommand.attrList.Count == 1)
                        {
                            var intAttr = curCommand.attrList[0] as Script.IntAttr;
                            switch (intAttr.value)
                            {
                                case 0: // タイトルへ
                                    owner.ReservationChangeScene(GameMain.Scenes.TITLE);
                                    break;
                                case 1: // ゲームオーバー
                                    owner.DoGameOver();
                                    break;
                            }
                        }
                        break;

                    // フィールドに移動（廃止）
                    case Command.FuncType.FIELD:
                        break;

                    // パーティ増減
                    case Command.FuncType.PARTY:
                        // バトル中だったらバトルメンバーの増減とする
                        {
                            var btlEvtCtl = owner as BattleEventController;
                            if (btlEvtCtl != null)
                            {
                                var queue = btlEvtCtl.addRemoveMember(curCommand);
                                waiter = () => { return !queue.finished; };
                                break;
                            }
                        }
                        
                        {
                            int curAttr = 0;
                            var guidAttr = curCommand.attrList[curAttr++] as Script.GuidAttr;
                            var intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;

                            if (intAttr.value == 0)
                            {
                                var hero = owner.owner.catalog.getItemFromGuid(guidAttr.value) as Common.Rom.Hero;
                                if (hero != null && owner.owner.data.party.members.Count < Common.GameData.Party.MAX_PARTY && !owner.owner.data.party.ExistMember(hero.guId))
                                    owner.owner.data.party.AddMember(hero);
                            }
                            else if (owner.owner.data.party.members.Count >= 2)
                            {
                                owner.owner.data.party.RemoveMember(guidAttr.value);
                            }

                            owner.RefreshHeroMapChr();
                        }
                        break;

                    // セリフ
                    case Command.FuncType.DIALOGUE:
                        if (CheckScriptMutexLock(ref owner.excludedScript))
                            break;

                        FuncDialogue();
                        break;

                    // テロップ
                    case Command.FuncType.TELOP:
                        if (CheckScriptMutexLock(ref owner.excludedScript))
                            break;

                        FuncTelop();
                        break;

                    // 感情マーク
                    case Command.FuncType.EMOTE:
                        FuncEffect();
                        break;

                    // IF系
                    case Command.FuncType.IFSWITCH:
                        {
                            int curAttr = 0;
                            int index = curCommand.attrList[curAttr++].GetInt();
                            bool cond = curCommand.attrList[curAttr++].GetInt() == 0 ? true : false;
                            if (owner.owner.data.system.GetSwitch(index, mapChr.rom.guId) != cond)
                                SearchElseOrEndIf();
                        }
                        break;
                    case Command.FuncType.IFVARIABLE:
                        {
                            int curAttr = 0;
                            int index = curCommand.attrList[curAttr++].GetInt();
                            int value = curCommand.attrList[curAttr++].GetInt();
                            int cond = curCommand.attrList[curAttr++].GetInt();
                            switch (cond)
                            {
                                case 0: // 等しい
                                    if (owner.owner.data.system.GetVariable(index, mapChr.rom.guId) != value) // 等しくない時はelseかendifまで飛ぶ
                                        SearchElseOrEndIf();
                                    break;
                                case 1: // 以上
                                    if (owner.owner.data.system.GetVariable(index, mapChr.rom.guId) < value) // 以上でない時はelseかendifまで飛ぶ
                                        SearchElseOrEndIf();
                                    break;
                                case 2: // 以下
                                    if (owner.owner.data.system.GetVariable(index, mapChr.rom.guId) > value)  // 以下でない時はelseかendifまで飛ぶ
                                        SearchElseOrEndIf();
                                    break;
                                case 3: // 等しくない
                                    if (owner.owner.data.system.GetVariable(index, mapChr.rom.guId) == value)
                                        SearchElseOrEndIf();
                                    break;
                                case 4: // 上
                                    if (owner.owner.data.system.GetVariable(index, mapChr.rom.guId) <= value)
                                        SearchElseOrEndIf();
                                    break;
                                case 5: // 下
                                    if (owner.owner.data.system.GetVariable(index, mapChr.rom.guId) >= value)
                                        SearchElseOrEndIf();
                                    break;
                            }
                        }
                        break;
                    case Command.FuncType.IFITEM:
                        {
                            int curAttr = 0;
                            var guid = curCommand.attrList[curAttr++].GetGuid();
                            int value = curCommand.attrList[curAttr++].GetInt();
                            int cond = curCommand.attrList[curAttr++].GetInt();
                            bool incEquip = false;
                            if (curCommand.attrList.Count > curAttr)
                                incEquip = curCommand.attrList[curAttr++].GetInt() == 0;
                            switch (cond)
                            {
                                case 0: // 以上
                                    if (owner.owner.data.party.GetItemNum(guid, incEquip) < value) // 以上でない時はelseかendifまで飛ぶ
                                        SearchElseOrEndIf();
                                    break;
                                case 1: // 未満
                                    if (owner.owner.data.party.GetItemNum(guid, incEquip) >= value)  // 未満でない時はelseかendifまで飛ぶ
                                        SearchElseOrEndIf();
                                    break;
                            }
                        }
                        break;
                    case Command.FuncType.IFMONEY:
                        {
                            int curAttr = 0;
                            int value = curCommand.attrList[curAttr++].GetInt();
                            int cond = curCommand.attrList[curAttr++].GetInt();
                            switch (cond)
                            {
                                case 0: // 以上
                                    if (owner.owner.data.party.GetMoney() < value) // 以上でない時はelseかendifまで飛ぶ
                                        SearchElseOrEndIf();
                                    break;
                                case 1: // 未満
                                    if (owner.owner.data.party.GetMoney() >= value)  // 未満でない時はelseかendifまで飛ぶ
                                        SearchElseOrEndIf();
                                    break;
                            }
                        }
                        break;
                    case Command.FuncType.IFPARTY:
                        {
                            int curAttr = 0;
                            var guid = curCommand.attrList[curAttr++].GetGuid();
                            bool cond = curCommand.attrList[curAttr++].GetInt() == 0 ? true : false;

                            // バトル中だったらバトルメンバーの情報を取得する
                            var btlEvtCtl = owner as BattleEventController;
                            if (btlEvtCtl != null)
                            {
                                if(btlEvtCtl.existMember(guid) != cond)
                                    SearchElseOrEndIf();
                            }
                            else if (owner.owner.data.party.ExistMember(guid) != cond)
                                SearchElseOrEndIf();
                        }
                        break;

                    // プレイヤー透明
                    case Command.FuncType.PLHIDE:
                        if (curCommand.attrList[0].GetInt() == 1)
                            owner.GetHeroForBattle().hide |= MapCharacter.HideCauses.BY_EVENT;
                        else
                            owner.GetHeroForBattle().hide &= ~MapCharacter.HideCauses.BY_EVENT;
                        break;

                    // イベント透明
                    case Command.FuncType.EVHIDE:
                        if (selfChr == null)
                            break;
                        if (curCommand.attrList[0].GetInt() == 1)
                            selfChr.hide |= MapCharacter.HideCauses.BY_EVENT;
                        else
                            selfChr.hide &= ~MapCharacter.HideCauses.BY_EVENT;
                        break;

                    // プレイヤすり抜け
                    case Command.FuncType.PLPRIORITY:
                        owner.GetHero().collidable = curCommand.attrList[0].GetInt() == 0;
                        break;

                    // プレイヤのグラフィック変更
                    case Command.FuncType.PLGRAPHIC:
                        if (curCommand.attrList.Count >= 4)
                        {
                            int curAttr = 0;

                            var hero = owner.owner.catalog.getItemFromGuid(curCommand.attrList[curAttr++].GetGuid()) as Common.Rom.Hero;
                            if (hero == null)
                                break;

                            var face = curCommand.attrList[curAttr++].GetGuid();
                            var model = curCommand.attrList[curAttr++].GetGuid();
                            owner.owner.data.party.AddChangedGraphic(hero.guId, face, model);

                            if (owner is BattleEventController)
                                ((BattleEventController)owner).RefreshHeroMapChr(hero);
                            else if (owner.GetHero().getGraphic() == null || owner.GetHero().getGraphic().guId != model)
                                owner.RefreshHeroMapChr();
                                
                            var animationName = curCommand.attrList[curAttr++].GetString();
                            var lockMotion = false;
                            if (curCommand.attrList.Count > curAttr) lockMotion = curCommand.attrList[curAttr++].GetInt() != 0;
                            owner.GetHeroForBattle(hero).playMotion(animationName, 0.2f, true, lockMotion);
                            owner.menuWindow.RefreshPartyChr();
                        }
                        break;

                    // 経験値
                    case Command.FuncType.CHG_EXP:
                        {
                            int curAttr = 0;
                            var guid = curCommand.attrList[curAttr++].GetGuid();
                            var value = curCommand.attrList[curAttr++].GetInt();
                            bool add = curCommand.attrList[curAttr++].GetInt() == 0;
                            if (!add)
                                value *= -1;

                            if (guid == Guid.Empty)
                            {
                                foreach (var hero in owner.owner.data.party.members)
                                {
                                    hero.AddExp(value, owner.owner.catalog);
                                }
                            }
                            else
                            {
                                var hero = owner.owner.data.party.GetMember(guid);
                                if (hero != null)
                                    hero.AddExp(value, owner.owner.catalog);
                            }
                        }
                        break;

                    // スキル変更
                    case Command.FuncType.CHG_SKILL:
                        {
                            int curAttr = 0;
                            var heroGuid = curCommand.attrList[curAttr++].GetGuid();
                            var skillGuid = curCommand.attrList[curAttr++].GetGuid();
                            bool add = curCommand.attrList[curAttr++].GetInt() == 0;

                            var hero = owner.owner.data.party.GetMember(heroGuid);
                            var skill = owner.owner.catalog.getItemFromGuid(skillGuid) as Common.Rom.Skill;
                            if (hero != null && skill != null)
                            {
                                if (add)
                                {
                                    if (!hero.skills.Contains(skill))
                                        hero.skills.Add(skill);
                                }
                                else
                                {
                                    if (hero.skills.Contains(skill))
                                        hero.skills.Remove(skill);
                                }
                            }
                        }

                        break;

                    // ステータス異常変更
                    case Command.FuncType.CHG_STTAILM:
                        if (CheckScriptMutexLock(ref owner.excludedScript))
                            break;

                        // バトルで使われた時はバトルに作用するようにする
                        {
                            var btlEvtCtl = owner as BattleEventController;
                            if (btlEvtCtl != null)
                            {
                                btlEvtCtl.setBattleStatus(curCommand, true);
                                break;
                            }
                        }

                        {
                            int curAttr = 0;
                            var heroGuid = curCommand.attrList[curAttr++].GetGuid();
                            int value = curCommand.attrList[curAttr++].GetInt();
                            bool add = curCommand.attrList[curAttr++].GetInt() == 0;

                            var hero = owner.owner.data.party.GetMember(heroGuid);
                            if (hero != null)
                            {
                                if (add)
                                {
                                    if (value == 0)
                                    {
                                        if (!hero.statusAilments.HasFlag(Common.GameData.Hero.StatusAilments.DOWN))
                                        {
                                            hero.statusAilments |= Common.GameData.Hero.StatusAilments.POISON;
                                        }
                                    }
                                    else
                                    {
                                        hero.hitpoint = 0;
                                        hero.consistency();

                                        if (owner.owner.data.party.isGameOver())
                                        {
                                            ScriptMutexLock(ref owner.excludedScript);
                                            owner.DoGameOver();
                                        }
                                    }
                                }
                                else
                                {
                                    if (value == 0)
                                    {
                                        hero.statusAilments &= ~Common.GameData.Hero.StatusAilments.POISON;
                                    }
                                    else if (hero.statusAilments.HasFlag(Common.GameData.Hero.StatusAilments.DOWN))
                                    {
                                        hero.hitpoint = 1;
                                        hero.consistency();
                                    }
                                }
                            }
                        }
                        break;

                    // HPMP変更
                    case Command.FuncType.CHG_HPMP:
                        if (CheckScriptMutexLock(ref owner.excludedScript))
                            break;
                        
                        // バトルで使われた時はバトルに作用するようにする
                        {
                            var btlEvtCtl = owner as BattleEventController;
                            if (btlEvtCtl != null)
                            {
                                btlEvtCtl.healParty(curCommand);
                                break;
                            }
                        }

                        {
                            int curAttr = 0;
                            var heroGuid = curCommand.attrList[curAttr++].GetGuid();
                            bool toHp = curCommand.attrList[curAttr++].GetInt() == 0;
                            int value = curCommand.attrList[curAttr++].GetInt();
                            bool add = curCommand.attrList[curAttr++].GetInt() == 0;
                            if (!add)
                                value *= -1;

                            var hero = owner.owner.data.party.GetMember(heroGuid);
                            if (hero != null)
                            {
                                if (toHp)
                                {
                                    hero.hitpoint += value;
                                }
                                else
                                {
                                    hero.magicpoint += value;
                                }
                                hero.consistency();

                                if (owner.owner.data.party.isGameOver())
                                {
                                    ScriptMutexLock(ref owner.excludedScript);
                                    owner.DoGameOver();
                                }
                            }
                        }
                        break;

                    // 全回復
                    case Command.FuncType.FULLRECOV:
                        {
                            var btlEvtCtl = owner as BattleEventController;
                            if (btlEvtCtl != null)
                            {
                                btlEvtCtl.fullRecovery();
                                break;
                            }
                        }

                        {
                            foreach (var hero in owner.owner.data.party.members)
                            {
                                hero.hitpoint += 9999;
                                hero.magicpoint += 9999;
                                hero.statusAilments = Common.GameData.Hero.StatusAilments.NONE;
                                hero.consistency();
                            }
                        }
                        break;

                    // フェード
                    case Command.FuncType.SCREEN_FADE:
                        {
                            int waitFrame = curCommand.attrList[0].GetInt() * 60 / 1000;
                            bool fadeIn = curCommand.attrList[1].GetInt() == 0;
                            if (fadeIn)
                            {
                                FuncScreenColor(owner.GetNowScreenColor(), new Color(0, 0, 0, 0), waitFrame, curCommand.attrList[2].GetInt() == 0);
                            }
                            else
                            {
                                FuncScreenColor(owner.GetNowScreenColor(), new Color(0, 0, 0, 255), waitFrame, curCommand.attrList[2].GetInt() == 0);
                            }
                        }
                        break;

                    // 色調変更
                    case Command.FuncType.SCREEN_COLOR:
                        {
                            int waitFrame = curCommand.attrList[0].GetInt() * 60 / 1000;
                            long color = curCommand.attrList[1].GetInt();
                            float a = (float)((color & 0xFF000000) >> 24) / 255;
                            float r = (float)((color & 0x00FF0000) >> 16) / 255 * a;
                            float g = (float)((color & 0x0000FF00) >> 8) / 255 * a;
                            float b = (float)(color & 0x000000FF) / 255 * a;
                            var tgtColor = new Microsoft.Xna.Framework.Color(r, g, b, a);
                            FuncScreenColor(owner.GetNowScreenColor(), tgtColor, waitFrame, curCommand.attrList[2].GetInt() == 0);
                        }
                        break;

                    // シェイク
                    case Command.FuncType.SCREEN_SHAKE:
                        {
                            int waitFrame = curCommand.attrList[0].GetInt() * 60 / 1000;
                            int force = curCommand.attrList[1].GetInt();
                            switch (force)
                            {
                                case 0:
                                    force = 1;
                                    break;
                                case 1:
                                    force = 4;
                                    break;
                                case 2:
                                    force = 7;
                                    break;
                            }
                            FuncScreenShake(force, waitFrame, curCommand.attrList[2].GetInt() == 0);
                        }
                        break;

                    // フラッシュ
                    case Command.FuncType.SCREEN_FLASH:
                        {
                            int waitFrame = curCommand.attrList[0].GetInt() * 60 / 1000;
                            FuncScreenFlash(waitFrame);
                        }
                        break;

                    // ジングル
                    case Command.FuncType.PLAYJINGLE:
                        {
                            var guid = curCommand.attrList[0].GetGuid();
                            var bgm = owner.owner.catalog.getItemFromGuid(guid) as Common.Resource.Bgm;
                            if (bgm != null)
                            {
                                float volume = 1.0f;
                                float tempo = 1.0f;
                                if (curCommand.attrList.Count > 1)
                                {
                                    volume = (float)curCommand.attrList[1].GetInt() / 100;
                                    tempo = (float)curCommand.attrList[2].GetInt() / 100;
                                }

                                // 再生しようとしているものを既に再生していたら、テンポとボリュームを反映させるだけ
                                var nowBgm = Audio.GetNowBgm(false);
                                if (nowBgm != null && nowBgm.rom == bgm)
                                {
                                    Audio.PlayBgm(bgm, volume, tempo);
                                    break;
                                }

                                nowBgm = Audio.GetNowBgm();
                                Audio.PlayBgm(bgm, volume, tempo);
                                Func<bool> task = () =>
                                {
                                    // 違うBGMになっていたら様子を見る
                                    var tmpNow = Audio.GetNowBgm(false);
                                    if (tmpNow != null && tmpNow.rom != bgm)
                                        return true;

                                    // BGMがまだ再生中だったら待つ
                                    if (Audio.IsBgmPlaying())
                                        return true;

                                    Audio.SwapBgm(nowBgm);
                                    return false;
                                };
                                if (curCommand.attrList.Count >= 4 && curCommand.attrList[3].GetInt() != 0)
                                {
                                    waiter = task;
                                }
                                else
                                {
                                    owner.owner.pushTask("Fanfare", task);
                                }
                            }
                        }
                        break;

                    // ボスバトル
                    case Command.FuncType.BOSSBATTLE:
                        if (CheckScriptMutexLock(ref owner.excludedScript))
                            break;

                        // すでにバトル中だったら抜ける
                        {
                            var btlEvtCtl = owner as BattleEventController;
                            if (btlEvtCtl != null)
                                break;
                        }

                        {
                            if (owner.isBattle || owner.isGameOver || owner.isMapChangeReserved)
                                break;

                            int curAttr = 0;
                            var intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;

                            int monsterNum = intAttr.value;     // モンスターの数
                            var monsterList = new List<Guid>(); // モンスターのGUIDリスト

                            if (monsterNum == 0)
                                break;

                            for (int i = 0; i < monsterNum; i++)
                            {
                                var guidAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.GuidAttr;
                                if (owner.owner.catalog.getItemFromGuid(guidAttr.value) is Common.Rom.Monster)
                                    monsterList.Add(guidAttr.value);
                            }

                            // 参照切れとかで monsterNum があっても、モンスター指定が空っぽの場合がありうる
                            if (monsterList.Count == 0)
                                break;

                            owner.battleSequenceManager.BattleStartEvents += owner.ShowBattleStartMessage;
                            owner.battleSequenceManager.BattleResultWinEvents += owner.mapEngine.PlayBattleWinBGM;
                            //owner.battleSequenceManager.BattleResultLoseGameOverEvents += owner.mapEngine.BattleLoseGameover;
                            owner.battleSequenceManager.BattleResultEscapeEvents += owner.ShowBattleEscapeMessage;

                            bool continuableOnLose = curCommand.attrList[curAttr++].GetInt() == 0;
                            bool escapeAvailable = curCommand.attrList[curAttr++].GetInt() == 0;
                            curAttr++;
                            //bool checkForWin = curCommand.attrList[curAttr++].GetInt() == 0;
                            Common.Resource.Bgm battleBgm = owner.owner.catalog.getItemFromGuid(curCommand.attrList[curAttr++].GetGuid()) as Common.Resource.Bgm;
                            if (battleBgm == null)
                                battleBgm = owner.getMapBattleBgm();

                            var bgGuid = curCommand.attrList[curAttr++].GetGuid();
                            if (bgGuid != Guid.Empty)
                                bgGuid = Common.Rom.Map.doConvertBattleBg(owner.owner.catalog, bgGuid);
                            if (bgGuid == Guid.Empty)
                                bgGuid = owner.battleSetting.battleBg;

                            owner.mapEngine.PlayBattleBGM(battleBgm, owner.getMapBattleBgs());

                            int centerX = owner.battleSetting.battleBgCenterX, centerY = owner.battleSetting.battleBgCenterY;
                            Point[] layout = null;

                            // 3Dバトルだった時は、敵のレイアウトデータがあるかどうかもチェックして読み込む
                            if (owner.owner.catalog.getGameSettings().battleType == GameSettings.BattleType.MODERN)
                            {
                                // 非公開Ver(1.0.5.1～1.0.5.15)用 互換処理
                                if (curCommand.attrList.Count == curAttr + 2)
                                {
                                    centerX = curCommand.attrList[curAttr++].GetInt();
                                    centerY = curCommand.attrList[curAttr++].GetInt();
                                }

                                const int SIGNATURE_BATTLEBG_CENTER = 1000;
                                const int SIGNATURE_MONSTER_LAYOUT = 1001;
                                while (curCommand.attrList.Count > curAttr)
                                {
                                    var sig = curCommand.attrList[curAttr++].GetInt();
                                    switch (sig)
                                    {
                                        case SIGNATURE_BATTLEBG_CENTER:
                                            centerX = curCommand.attrList[curAttr++].GetInt();
                                            centerY = curCommand.attrList[curAttr++].GetInt();
                                            break;
                                        case SIGNATURE_MONSTER_LAYOUT:
                                            layout = new Point[monsterNum];
                                            for (int i = 0; i < layout.Length; i++)
                                            {
                                                layout[i] = new Point(curCommand.attrList[curAttr++].GetInt(),
                                                    curCommand.attrList[curAttr++].GetInt());
                                            }
                                            break;
                                    }
                                }
                            }

#if WINDOWS
                            {
#else
                            bool changeBg = owner.battleSequenceManager.isWrongFromCurrentBg(bgGuid);
                            if (UnityEntry.IsDivideMapScene() && changeBg)
                            { }
                            else
                            {
#endif
                                UnityUtil.changeScene(UnityUtil.SceneType.BATTLE, true);
                                owner.StartBattleMode(this);
                                var modifiedBattleSettings = new Common.Rom.Map.BattleSetting();
                                modifiedBattleSettings.battleBg = bgGuid;
                                modifiedBattleSettings.battleBgCenterX = centerX;
                                modifiedBattleSettings.battleBgCenterY = centerY;
                                owner.battleSequenceManager.BattleStart(owner.owner.data.party, monsterList.ToArray(),
                                    modifiedBattleSettings, escapeAvailable, continuableOnLose, layout);
                            }

                            // 顔グラフィックを消しておく
                            hideDialogueCharacters();

                            // 戦闘完了までウェイト
                            ScriptMutexLock(ref owner.excludedScript);
#if WINDOWS
#else
                            int preparingState = 0;
                            IEnumerator coroutine = null;
#endif
                            waiter = () =>
                            {
#if WINDOWS
#else
                                // 戦闘背景を読み込む
                                if (UnityEntry.IsDivideMapScene() && changeBg)
                                {
                                    switch (preparingState)
                                    {
                                        case 0:
                                            coroutine = owner.battleSequenceManager.prepare(bgGuid);
                                            preparingState++;
                                            return true;
                                        case 1:
                                            UnityUtil.changeScene(UnityUtil.SceneType.BATTLE, true);
                                            while (coroutine.MoveNext()) return true;
                                            preparingState++;
                                            return true;
                                        case 2:
                                            UnityUtil.changeScene(UnityUtil.SceneType.BATTLE, true);
                                            owner.StartBattleMode(this);
                                            var modifiedBattleSettings = new Common.Rom.Map.BattleSetting();
                                            modifiedBattleSettings.battleBg = bgGuid;
                                            modifiedBattleSettings.battleBgCenterX = centerX;
                                            modifiedBattleSettings.battleBgCenterY = centerY;
                                            owner.battleSequenceManager.BattleStart(owner.owner.data.party, monsterList.ToArray(), modifiedBattleSettings,
                                                escapeAvailable, continuableOnLose, layout);
                                            preparingState++;
                                            return true;
                                        default:
                                            break;
                                    }
                                }
#endif

                                // ここで false を返すと、マップ画面のスクリプト停止状態が解除されます。
                                // 戦闘画面の状態を監視して、戦闘が終わったら false を返すように実装してみてください。
                                if (owner.battleSequenceManager.BattleResult == BattleSequenceManager.BattleResultState.NonFinish || owner.battleSequenceManager.IsPlayingBattleEffect)
                                {
                                    return true;
                                }
                                else
                                {
#if WINDOWS
#else
                                    // 元の戦闘背景に戻す
                                    if (UnityEntry.IsDivideMapScene() && changeBg)
                                    {
                                        switch (preparingState)
                                        {
                                            case 3:
                                                owner.mapEngine.ResumeMapBGM();
                                                owner.EndBattleMode();

                                                if (owner.battleSequenceManager.isContinuable())
                                                {
                                                    cur--; SearchElseOrEndIf(); cur++;
                                                }

                                                coroutine = owner.battleSequenceManager.prepare();
                                                preparingState++;
                                                return true;
                                            case 4:
                                                UnityUtil.changeScene(UnityUtil.SceneType.BATTLE, true);
                                                while (coroutine.MoveNext())return true;
                                                preparingState++;
                                                return true;
                                            default:
                                                break;
                                        }
                                    }
                                    else
#endif
                                    {
                                        if (owner.battleSequenceManager.isContinuable())
                                        {
                                            cur--; SearchElseOrEndIf(); cur++;
                                        }

                                        owner.mapEngine.ResumeMapBGM();
                                        owner.EndBattleMode();
                                    }

                                    ScriptMutexUnlock(ref owner.excludedScript);
                                    return owner.battleSequenceManager.BattleResult == BattleSequenceManager.BattleResultState.Lose_GameOver;
                                }
                            };
                        }
                        break;

                    // セーブ禁止・解除
                    case Command.FuncType.SW_SAVE:
                        owner.setSaveAvailable(curCommand.attrList[0].GetInt() == 0);
                        break;

                    // ダッシュ禁止・解除
                    case Command.FuncType.SW_DASH:
                        owner.owner.data.system.dashAvailable = curCommand.attrList[0].GetInt() == 0;
                        break;

                    // エンカウント禁止・解除
                    case Command.FuncType.SW_ENCOUNTING:
                        owner.setEncountFlag(curCommand.attrList[0].GetInt() == 0);
                        break;

                    // メニュー禁止・解除
                    case Command.FuncType.SW_MENU:
                        owner.setMenuAvailable(curCommand.attrList[0].GetInt() == 0);
                        break;

                    // カメラ操作命令
                    case Command.FuncType.CAMERA:
                        if (CheckScriptMutexLock(ref owner.cameraControlledScript))
                            break;

                        FuncCamera();
                        break;

                    // ELSE
                    case Command.FuncType.ELSE:
                        SearchElseOrEndIf();
                        break;

                    // ENDIF は何もしない
                    case Command.FuncType.ENDIF:
                        break;

                    // ループ開始位置に戻る
                    case Command.FuncType.ENDLOOP:
                        cur = stack.Peek();
                        break;

                    // 選択肢の分岐 この行を実行するという事は、
                    // その上の選択肢から流れてきたという事なので、ENDIFにジャンプするだけでOK
                    case Command.FuncType.BRANCH:
                        SearchElseOrEndIf();    // ELSEにも引っかかってしまうが、存在し得ないので大丈夫
                        break;

                    // SE停止
                    case Command.FuncType.STOPSE:
                        if (curCommand.attrList.Count >= 1)
                        {
                            unloadSound();

                            var guidAttr = curCommand.attrList[0] as Script.GuidAttr;
                            if (guidAttr.value != Guid.Empty)
                            {
                                var sound = owner.owner.catalog.getItemFromGuid(guidAttr.value) as Common.Resource.Se;
                                if (sound != null && Audio.IsSePlaying(sound))
                                {
                                    loadedSeId = Audio.GetSeId(sound);
                                    Audio.PlaySound(loadedSeId, 0f, 0f);
                                }
                            }
                            else
                            {
                                Audio.StopAllSound();
                            }
                        }
                        break;

                    // CS実行
                    case Command.FuncType.SCRIPT_CS:
                        if (curCommand.attrList.Count >= 1)
                        {
                            ProcScriptCS();
                        }
                        break;
                    // パーティメンバーの名前を変更する
                    case Command.FuncType.CHANGE_HERO_NAME:
                        ChangeHeroName();
                        break;
                    case Command.FuncType.STRING_VARIABLE:
                        if (curCommand.attrList.Count > 0)
                        {
                            var index = curCommand.attrList[0].GetInt();
                            var input = "";

                            if (curCommand.attrList[1] is Common.Rom.Script.StringAttr)
                            {
                                input = curCommand.attrList[1].GetString();
                            }
                            else if (curCommand.attrList[1] is Common.Rom.Script.IntAttr)
                            {
                                input = owner.owner.data.system.StrVariables[curCommand.attrList[1].GetInt()];
                            }
                            else if (curCommand.attrList[1] is Common.Rom.Script.GuidAttr)
                            {
                                input = owner.owner.data.party.getHeroName(curCommand.attrList[1].GetGuid());
                            }
                            
                            var changeType = curCommand.attrList[2].GetInt();

                            if (index < 0)
                            {
                                // 主人公の名前に代入するタイプ
                                var guid = curCommand.attrList[3].GetGuid();
                                var systemString = owner.owner.data.party.getHeroName(guid);
                                switch (changeType)
                                {
                                    // 代入
                                    case 0:
                                        owner.setHeroName(guid, input);
                                        break;
                                    // 先頭に追加
                                    case 1:
                                        owner.setHeroName(guid, input + systemString);
                                        break;
                                    // 後尾に追加
                                    case 2:
                                        owner.setHeroName(guid, systemString + input);
                                        break;
                                }
                            }
                            else
                            {
                                // 文字列変数に代入するタイプ
                                var systemString = owner.owner.data.system.StrVariables[index];
                                switch (changeType)
                                {
                                    // 代入
                                    case 0:
                                        owner.owner.data.system.StrVariables[index] = input;
                                        break;
                                    // 先頭に追加
                                    case 1:
                                        owner.owner.data.system.StrVariables[index] = input + systemString;
                                        break;
                                    // 後尾に追加
                                    case 2:
                                        owner.owner.data.system.StrVariables[index] = systemString + input;
                                        break;
                                }
                            }
                        }
                        break;
                    case Command.FuncType.IF_STRING_VARIABLE:
                        if (curCommand.attrList.Count > 0)
                        {
                            var index = curCommand.attrList[0].GetInt();
                            var input = curCommand.attrList[1].GetString();
                            var checkType = curCommand.attrList[2].GetInt();
                            var systemString = "";
                            if (index < 0)
                            {
                                // 主人公の名前を参照するタイプ
                                var guid = curCommand.attrList[3].GetGuid();
                                systemString = owner.owner.data.party.getHeroName(guid);
                            }
                            else
                            {
                                // 文字列変数を参照するタイプ
                                if (owner.owner.data.system.StrVariables[index] != null)
                                    systemString = owner.owner.data.system.StrVariables[index];
                            }

                            switch (checkType)
                            {
                                // 等しいか
                                case 0:
                                    if (systemString != input)
                                    {
                                        SearchElseOrEndIf();
                                    }
                                    break;
                                // 先頭がinputから始まる
                                case 1:
                                    if (!systemString.StartsWith(input))
                                    {
                                        SearchElseOrEndIf();
                                    }
                                    break;
                                // 後尾がinputで終わる
                                case 2:
                                    if (!systemString.EndsWith(input))
                                    {
                                        SearchElseOrEndIf();
                                    }
                                    break;
                                // 含んでいるか
                                case 3:
                                    if (!systemString.Contains(input))
                                    {
                                        SearchElseOrEndIf();
                                    }
                                    break;
                            }
                        }
                        break;
                    case Command.FuncType.CHANGE_STRING_VARIABLE:
                        ChangeStringVariable();
                        break;
                    case Command.FuncType.ROUTE_MOVE:
                        FuncRouteMove(selfChr, false);
                        break;
                    case Command.FuncType.IF_INVENTORY_EMPTY:
                        if (curCommand.attrList.Count > 0)
                        {
                            var num = curCommand.attrList[0].GetInt();
                            if (owner.owner.data.party.checkInventoryEmptyNum() < num)
                                SearchElseOrEndIf();
                        }
                        break;
                    case Command.FuncType.ITEM_THROW_OUT:
                        if (owner.owner.data.party.items.Count == 0)
                            break;
                        owner.ShowTrashWindow(Guid.Empty, 0);
                        waiter = () =>
                        {
                            return owner.IsTrashVisible();
                        };
                        break;
                    case Command.FuncType.WALK_IN_ROWS:
                        if (curCommand.attrList.Count > 0)
                        {
                            GameMain.instance.mapScene.mapEngine.setFollowersVisible(curCommand.attrList[0].GetInt() > 0);
                        }
                        break;
                    case Command.FuncType.WALK_IN_ROWS_ORDER:
                        if (curCommand.attrList.Count > 0)
                        {
                            var mapScene = owner;
                            if (owner is BattleEventController)
                            {
                                // バトル中は本当のMapSceneを探してそちらを操作する
                                mapScene = ((BattleEventController)owner).owner.mapScene;
                            }

                            int cur = 0;
                            var count = curCommand.attrList[cur++].GetInt();
                            mapScene.mapEngine.startFollowerSort();    // 隊列の操作開始
                            for (int i = 0; i < count; i++)
                            {
                                var ft = (Common.GameData.Followers.Entry.FollowerType)curCommand.attrList[cur++].GetInt();
                                switch (ft)
                                {
                                    case Common.GameData.Followers.Entry.FollowerType.PARTY_MEMBER:
                                        var index = curCommand.attrList[cur++].GetInt();
                                        mapScene.mapEngine.pushFollower(index);
                                        break;
                                    case Common.GameData.Followers.Entry.FollowerType.GRAPHIC:
                                        var guid = curCommand.attrList[cur++].GetGuid();
                                        mapScene.mapEngine.pushFollower(guid);
                                        break;
                                }
                            }
                            mapScene.mapEngine.endFollowerSort();  // 隊列の操作終了
                        }
                        break;
                    case Command.FuncType.CHANGE_GAMEOVER_ACTION:
                        ChangeGameOverAction();
                        break;
                    case Command.FuncType.BTL_SW_UI:
                        {
                            var btlEvtCtl = owner as BattleEventController;
                            if (btlEvtCtl != null)
                                btlEvtCtl.showBattleUi(!curCommand.attrList[0].GetBool());
                        }
                        break;
                    case Command.FuncType.BTL_HEAL:
                        {
                            var btlEvtCtl = owner as BattleEventController;
                            if (btlEvtCtl != null)
                                btlEvtCtl.healBattleCharacter(curCommand, mapChr.rom.guId);
                        }
                        break;
                    case Command.FuncType.BTL_SELECTOR:
                        {
                            var btlEvtCtl = owner as BattleEventController;
                            if (btlEvtCtl != null)
                                followChr = btlEvtCtl.getBattleActorMapChr(curCommand);
                        }
                        break;
                    case Command.FuncType.BTL_ACTION:
                        {
                            var btlEvtCtl = owner as BattleEventController;
                            if (btlEvtCtl != null)
                                btlEvtCtl.setNextAction(curCommand);
                        }
                        break;
                    case Command.FuncType.BTL_STATUS:
                        {
                            var btlEvtCtl = owner as BattleEventController;
                            if (btlEvtCtl != null)
                                btlEvtCtl.setBattleStatus(curCommand);
                        }
                        break;
                    case Command.FuncType.BTL_STOP:
                        {
                            var btlEvtCtl = owner as BattleEventController;
                            if (btlEvtCtl != null)
                                btlEvtCtl.battleStop();
                        }
                        break;
                    case Command.FuncType.BTL_APPEAR:
                        {
                            var btlEvtCtl = owner as BattleEventController;
                            if (btlEvtCtl != null)
                            {
                                var queue = btlEvtCtl.addMonster(curCommand);
                                waiter = () => { return !queue.finished; };
                                break;
                            }
                        }
                        break;

                    case Command.FuncType.BTL_IFBATTLE:
                        if (!(owner is BattleEventController)) // バトル中でない時はelseかendifまで飛ぶ
                            SearchElseOrEndIf();
                        break;

                    case Command.FuncType.EQUIP:
                        FuncEquipment();
                        break;

                    case Command.FuncType.BTL_IFMONSTER:
                        if (!(owner is BattleEventController)) // バトル中でない時はelseかendifまで飛ぶ
                        {
                            SearchElseOrEndIf();
                            break;
                        }

                        {
                            var guid = curCommand.attrList[0].GetGuid();
                            var index = curCommand.attrList[1].GetInt();
                            if (((BattleEventController)owner).getEnemyGuid(index) != guid) // バトル中でない時はelseかendifまで飛ぶ
                                SearchElseOrEndIf();
                        }
                        break;

                    case Command.FuncType.BTL_SW_CAMERA:
                        var data = owner.owner.data.system;
                        for(int i=0; i<curCommand.attrList.Count; i++)
                        {
                            data.BattleCameraEnabled[i] = curCommand.attrList[i].GetBool();
                        }
                        break;

                    case Command.FuncType.WEBBROWSER:
#if WINDOWS
                        var browser = new Extra.WebBrowser(curCommand.attrList.Count < 2 || curCommand.attrList[1].GetBool());
                        browser.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
                        browser.Location = new System.Drawing.Point(0, 0);
                        //browser.Location = new System.Drawing.Point(
                        //    (int)SharpKmy.Entry.getMainWindowPosX(),
                        //    (int)SharpKmy.Entry.getMainWindowPosY()
                        //    );
                        browser.setUrl(curCommand.attrList[0].GetString());
                        browser.setCloseButtonText(owner.owner.catalog.getGameSettings().glossary.close);

                        if (SharpKmy.Entry.isFullScreenMode())
                        {
                            SharpKmy.Entry.fullScreenMode(false);
                        }

                        float browserPhase = 0;
                        float fadeTime = 10;
                        waiter = () =>
                        {
                            int bottomBarHeight = (int)(544 / SharpKmy.Entry.getMainWindowHeight() * 36);

                            if (browserPhase < fadeTime)
                            {
                                float delta = browserPhase / fadeTime;
                                delta = delta * delta;
                                byte a = (byte)(delta * 255);
                                Graphics.DrawFillRect(0, 0, 960, 544, a, a, a, a);
                                browserPhase++;

                                int pos = (int)(delta * bottomBarHeight);
                                Graphics.DrawFillRect(0, 544 - pos, 960, pos, 0, 0, 0, 255);
                            }
                            else if (browserPhase == fadeTime)
                            {
                                Graphics.DrawFillRect(0, 0, 960, 544, 255, 255, 255, 255);
                                Graphics.DrawFillRect(0, 544 - bottomBarHeight, 960, bottomBarHeight, 0, 0, 0, 255);
                                browser.Show();
                                browser.ActivateMainWindow();
                                browserPhase++;
                            }
                            else if (browserPhase == fadeTime + 1)
                            {
                                Graphics.DrawFillRect(0, 0, 960, 544, 255, 255, 255, 255);
                                Graphics.DrawFillRect(0, 544 - bottomBarHeight, 960, bottomBarHeight, 0, 0, 0, 255);
                                SharpKmy.Entry.suspendSwapBuffer(true);

                                // キーを待ち受け
                                var scrollY = Input.GetAxis("PAD_Y");
                                const float DEAD_ZONE = 0.5f;

                                if (scrollY < -DEAD_ZONE)
                                {
                                    browser.scroll(scrollY);
                                }
                                else if (scrollY > DEAD_ZONE)
                                {
                                    browser.scroll(scrollY);
                                }
                                if (Input.KeyTest(StateType.REPEAT, KeyStates.UP))
                                {
                                    browser.scroll(-1);
                                }
                                else if (Input.KeyTest(StateType.REPEAT, KeyStates.DOWN))
                                {
                                    browser.scroll(1);
                                }
                                if (Input.KeyTest(StateType.TRIGGER, KeyStates.CANCEL))
                                    browser.Close();

                                // ウィンドウ追従
                                //browser.Location = new System.Drawing.Point(
                                //    (int)SharpKmy.Entry.getMainWindowPosX(),
                                //    (int)SharpKmy.Entry.getMainWindowPosY()
                                //    );
                                browser.Size = new System.Drawing.Size(
                                    (int)SharpKmy.Entry.getMainWindowWidth(),
                                    (int)SharpKmy.Entry.getMainWindowHeight()
                                    );

                                // TODO アクティブ状態に追従

                                if (browser.Visible)
                                    return true;

                                browserPhase++;
                                SharpKmy.Entry.suspendSwapBuffer(false);
                                browser.ActivateMainWindow();
                            }
                            else if (browserPhase <= fadeTime * 2)
                            {
                                float delta = (fadeTime * 2 - browserPhase) / fadeTime;
                                delta = delta * delta;
                                byte a = (byte)(delta * 255);
                                Graphics.DrawFillRect(0, 0, 960, 544, a, a, a, a);
                                browserPhase++;

                                int pos = (int)(delta * bottomBarHeight);
                                Graphics.DrawFillRect(0, 544 - pos, 960, pos, 0, 0, 0, 255);
                                browserPhase++;
                            }
                            else
                            {
                                browser.Dispose();
                                return false;
                            }

                            return true;
                        };
#endif
                        break;

                    // なんでもテスト用命令
                    case Command.FuncType.DEBUGFUNC:
                        if (curCommand.attrList.Count >= 1)
                        {
                            var intAttr = curCommand.attrList[0] as Script.IntAttr;
                            if (intAttr.value == 0)
                            {
#if WINDOWS
#else
                                //スイッチの初期化
                                owner.owner.data.system.SetSwitch(902, false, mapChr.rom.guId);
                                owner.owner.data.system.SetSwitch(901, false, mapChr.rom.guId);
                                owner.owner.data.system.SetSwitch(900, false, mapChr.rom.guId);

                                // ここに広告表示処理を書く
                                {
                                    var ad = UnityAdsManager.GetVideo();
                                    if (ad.IsError()) ad.Load();
                                    ad.Show();
                                }

                                waiter = () =>
                                {
                                    var ad = UnityAdsManager.GetVideo();
                                    // これが true を返している間はスクリプトの進行が止まります
                                    if (ad.IsShow()) return true;

                                    // 広告を見た場合などに特定のスイッチをオンにする時は下記の処理を行って下さい。
                                    // 123 は適当です。グランブームの場合、スイッチ名を見るにスイッチ524以降は使っていないようです。
                                    // ちなみにスイッチ番号はデータ上は1000以上も取扱できますが、
                                    // イベントエディタの分岐条件などには1000までしか入力できません。
                                    //owner.owner.data.system.SetSwitch(123, true, mapChr.rom.guId);
                                    if (ad.IsError())
                                    {
                                        //エラー時の処理
                                        owner.owner.data.system.SetSwitch(902, true, mapChr.rom.guId);
                                        return false;
                                    }
                                    if(ad.IsReward() == false)
                                    {
                                        //報酬が受け取れないとき
                                        owner.owner.data.system.SetSwitch(901, true, mapChr.rom.guId);
                                        return false;
                                    }
                                    //広告を見たとき
                                    owner.owner.data.system.SetSwitch(900, true, mapChr.rom.guId);
                                    return false;
                                };
#endif
                            }
                        }
                        break;
                }
                cur++;
            }

            return isFinished();
        }

        private void ProcScriptCS()
        {
            var path = curCommand.attrList[0].GetString();

#if WINDOWS
            var asm = getAssembly(path);
            if (asm == null)
                return;

            //コンパイルしたファイル名に対応したクラスのTypeを取得
            var name = Common.Util.file.getFileNameWithoutExtension(path);
            var typeName = "SGB." + name;

            var type = asm.GetType(typeName);
            if (type == null)
                return;
#else
            var name = Common.Util.file.getFileNameWithoutExtension(path);
            var typeName = "SGB." + name;
            var type = Type.GetType(typeName);
            if (type == null)
                return;
#endif

            var mi = type.GetMethod("start");
            if (mi == null)
                return;

            mi.Invoke(null, new object[] { owner });

            var upd = type.GetMethod("update");
            if (upd == null)
                return;

            waiter = () =>
            {
                return (bool)upd.Invoke(null, null);
            };
        }

#if WINDOWS
        private Assembly getAssembly(string path)
        {
            if (sAsmDict.ContainsKey(path))
                return sAsmDict[path];

            if (!Common.Util.file.exists(path))
                return null;

            var stream = Common.Util.file.getStreamReader(path);
            var source = stream.ReadToEnd();
            stream.Close();

            using (var cscp = new CSharpCodeProvider(new Dictionary<string, string> { { "CompilerVersion", "v3.5" } }))
            {
                CompilerParameters cps = new CompilerParameters();
                CompilerResults cres;
                //メモリ内で出力を生成する
                cps.GenerateInMemory = true;
                cps.ReferencedAssemblies.Add(Assembly.GetEntryAssembly().Location);
                cps.ReferencedAssemblies.Add(Common.BinaryReaderWrapper.getExecDir(true) + "common.dll");
                cps.ReferencedAssemblies.Add("System.Windows.Forms.dll");
                //コンパイルする
                cres = cscp.CompileAssemblyFromSource(cps, source);

                if (cres.Errors.Count > 0)
                {
                    string errors = path + "のコンパイル中にエラーが発生しました。\n";
                    foreach (CompilerError err in cres.Errors)
                    {
                        if (errors[errors.Length - 1] != '\n')
                            errors += "\n";

                        errors += err.Line + "行目の" + err.Column + "文字目 → " + err.ErrorText;
                    }
                    owner.messageWindow.PushMessage(errors, 0, MessageWindow.WindowType.MESSAGE);
                    return null;
                }

                //コンパイルしたアセンブリを取得
                Assembly asm = cres.CompiledAssembly;
                if (asm != null)
                    sAsmDict.Add(path, asm);
                return asm;
            }
        }
#endif

        // ロックできるかどうかチェックするメソッド。出来なかったらtrue
        private bool CheckScriptMutexLock(ref ScriptRunner mutex)
        {
            if (mutex != null)
            {
                cur--;
                return true;
            }
            return false;
        }

        private void ScriptMutexUnlock(ref ScriptRunner mutex)
        {
            if (mutex == this)
            {
                mutex = null;
            }
        }

        // ロックできるようだったらロックするメソッド。出来なかったらtrue
        private bool ScriptMutexLock(ref ScriptRunner mutex)
        {
            if (mutex != null)
            {
                cur--;
                return true;
            }
            mutex = this;
            return false;
        }

        private void FuncCamera()
        {
            var cmd = curCommand;
            int cur = 0;

            var mode = cmd.attrList[cur++].GetInt();
            var camMode = (mode == 0 || mode == 3) ? Common.Rom.Map.CameraControlMode.NORMAL : Common.Rom.Map.CameraControlMode.VIEW;

            // バトル中は主人公カメラモードは使用不可
            if (owner is BattleEventController && camMode == Map.CameraControlMode.VIEW)
                return;

            if (camMode != owner.CurrentCameraMode)
                owner.changeCameraMode(camMode);

            if (mode != 2)
            {
                var startParam = new Common.Rom.ThirdPersonCameraSettings();    // 三人称用パラメータですべて賄う
                var startPos = owner.GetLookAtTarget();
                var heroPos = owner.GetHeroPos();
                var fixCameraPosToHero = startPos.x == heroPos.x && startPos.y == heroPos.y && startPos.z == heroPos.z;
                startParam.xAngle = owner.xangle % 360;
                startParam.yAngle = owner.yangle % 360;
                startParam.fov = owner.fovy;
                startParam.distance = camMode == Common.Rom.Map.CameraControlMode.NORMAL ? owner.dist : owner.eyeHeight;

                var endParam = new Common.Rom.ThirdPersonCameraSettings();
                SharpKmyMath.Vector3 endPos;
                if (mode == 3)
                {
                    endPos = new SharpKmyMath.Vector3(
                        (float)cmd.attrList[cur++].GetInt() / 1000,
                        (float)cmd.attrList[cur++].GetInt() / 1000,
                        (float)cmd.attrList[cur++].GetInt() / 1000);
                }
                else
                {
                    endPos = owner.GetHeroPos();
                }
                endParam.xAngle = (float)cmd.attrList[cur++].GetInt() / 1000 % 360;
                endParam.yAngle = (float)cmd.attrList[cur++].GetInt() / 1000 % 360;
                endParam.fov = (float)cmd.attrList[cur++].GetInt() / 1000;
                endParam.distance = (float)cmd.attrList[cur++].GetInt() / 1000;

                float wait = (float)cmd.attrList[cur++].GetInt() * 60 / 1000;

                // 回転軸
                int rotateDir = 0;
                if (cmd.attrList.Count > cur)
                {
                    rotateDir = cmd.attrList[cur++].GetInt();
                }

                // 補間
                var interpolateType = GameSettings.BattleCamera.TweenType.LINEAR;
                if (cmd.attrList.Count > cur)
                    interpolateType = (GameSettings.BattleCamera.TweenType)cmd.attrList[cur++].GetInt();

                float diffY = startParam.yAngle - endParam.yAngle;
                float absY = Math.Abs(diffY);
                switch (rotateDir)
                {
                    case 0: //近い方を採用する
                        if (absY > 180)
                            endParam.yAngle += diffY * 360 / absY;
                        break;
                    case 1: // 左回り
                        if (diffY < 0)
                            endParam.yAngle -= 360;
                        break;
                    case 2: // 右回り
                        if (diffY > 0)
                            endParam.yAngle += 360;
                        break;
                }

                owner.isCameraMoveByCameraFunc++;
                ScriptMutexLock(ref owner.cameraControlledScript);
                Action finalizer = () =>
                {
                    owner.isCameraMoveByCameraFunc--;
                    owner.xangle = endParam.xAngle % 360;
                    owner.yangle = endParam.yAngle % 360;
                    owner.fovy = endParam.fov;

                    if (camMode == Common.Rom.Map.CameraControlMode.NORMAL)
                    {
                        owner.dist = endParam.distance;
                        owner.SetLookAtTarget(mode == 3, endPos);
                    }
                    else
                    {
                        owner.eyeHeight = endParam.distance;
                    }

                    owner.applyCameraToBattle();
                    ScriptMutexUnlock(ref owner.cameraControlledScript);
                };
                addFinalizer(finalizer);

                if (wait == 0)
                {
                    executeFinalizer(finalizer);
                    return;
                }

                // 移動完了までウェイト
                float now = 0;
                waiter = () =>
                {
                    now += GameMain.getRelativeParam60FPS();

                    float delta = now / wait;

                    // 主人公に注目するタイプの場合、毎フレーム主人公の位置でstartPos/endPosを更新する
                    if (mode != 3)
                    {
                        if (fixCameraPosToHero)
                            startPos = owner.GetHeroPos();
                        endPos = owner.GetHeroPos();
                    }

                    if (delta < 1)
                    {
                        switch (interpolateType)
                        {
                            case GameSettings.BattleCamera.TweenType.EASE_OUT:
                                delta = 1f - (1f - delta) * (1f - delta);
                                break;
                            case GameSettings.BattleCamera.TweenType.EASE_IN:
                                delta = delta * delta;
                                break;
                            case GameSettings.BattleCamera.TweenType.EASE_IN_OUT:
                                if (delta < 0.5f)
                                {
                                    delta *= 2;
                                    delta = delta * delta;
                                    delta /= 2;
                                }
                                else
                                {
                                    delta = (delta - 0.5f) * 2;
                                    delta = 1f - (1f - delta) * (1f - delta);
                                    delta = (delta / 2) + 0.5f;
                                }
                                break;
                        }
                        float inverted = 1 - delta;

                        owner.xangle = inverted * startParam.xAngle + delta * endParam.xAngle;
                        owner.yangle = inverted * startParam.yAngle + delta * endParam.yAngle;
                        owner.fovy = inverted * startParam.fov + delta * endParam.fov;
                        float distOrEyeHeight = inverted * startParam.distance + delta * endParam.distance;

                        if (camMode == Common.Rom.Map.CameraControlMode.NORMAL)
                        {
                            owner.dist = distOrEyeHeight;
                            var camPos = new SharpKmyMath.Vector3(
                                inverted * startPos.x + delta * endPos.x,
                                inverted * startPos.y + delta * endPos.y,
                                inverted * startPos.z + delta * endPos.z);
                            owner.SetLookAtTarget(true, camPos);
                        }
                        else
                        {
                            owner.eyeHeight = distOrEyeHeight;
                        }
                        owner.applyCameraToBattle();
                        return true;
                    }

                    executeFinalizer(finalizer);
                    return false;
                };
            }
            // 主人公カメラ(回転なし)
            else
            {
                var startParam = new Common.Rom.FirstPersonCameraSettings();
                startParam.fov = owner.fovy;
                startParam.eyeHeight = owner.eyeHeight;

                var endParam = new Common.Rom.FirstPersonCameraSettings();
                endParam.fov = (float)cmd.attrList[cur++].GetInt() / 1000;
                endParam.eyeHeight = (float)cmd.attrList[cur++].GetInt() / 1000;

                float wait = (float)cmd.attrList[cur++].GetInt() * 60 / 1000;

                owner.isCameraMoveByCameraFunc++;
                ScriptMutexLock(ref owner.cameraControlledScript);
                Action finalizer = () =>
                {
                    owner.isCameraMoveByCameraFunc--;
                    owner.fovy = endParam.fov;
                    owner.eyeHeight = endParam.eyeHeight;
                    ScriptMutexUnlock(ref owner.cameraControlledScript);
                };
                addFinalizer(finalizer);

                if (wait == 0)
                {
                    executeFinalizer(finalizer);
                    return;
                }

                // 回転方向指定（無視）
                if (cmd.attrList.Count > cur)
                    cur++;

                // 補間
                var interpolateType = GameSettings.BattleCamera.TweenType.LINEAR;
                if (cmd.attrList.Count > cur)
                    interpolateType = (GameSettings.BattleCamera.TweenType)cmd.attrList[cur++].GetInt();

                // 移動完了までウェイト
                float now = 0;
                waiter = () =>
                {
                    now += GameMain.getRelativeParam60FPS();

                    float delta = now / wait;

                    if (delta < 1)
                    {
                        switch (interpolateType)
                        {
                            case GameSettings.BattleCamera.TweenType.EASE_OUT:
                                delta = 1f - (1f - delta) * (1f - delta);
                                break;
                            case GameSettings.BattleCamera.TweenType.EASE_IN:
                                delta = delta * delta;
                                break;
                            case GameSettings.BattleCamera.TweenType.EASE_IN_OUT:
                                if (delta < 0.5f)
                                {
                                    delta *= 2;
                                    delta = delta * delta;
                                    delta /= 2;
                                }
                                else
                                {
                                    delta = (delta - 0.5f) * 2;
                                    delta = 1f - (1f - delta) * (1f - delta);
                                    delta = (delta / 2) + 0.5f;
                                }
                                break;
                        }
                        float inverted = 1 - delta;

                        owner.fovy = inverted * startParam.fov + delta * endParam.fov;
                        owner.eyeHeight = inverted * startParam.eyeHeight + delta * endParam.eyeHeight;
                        return true;
                    }

                    executeFinalizer(finalizer);
                    return false;
                };
            }
        }

        private void executeFinalizer(Action finalizer)
        {
            if (finalizerQueue.Contains(finalizer))
            {
                finalizer();
                finalizerQueue.Remove(finalizer);
            }
        }

        public bool isFinished()
        {
            return script.commands.Count <= cur && waiter == null;
        }

        private void hideDialogueCharacters()
        {
            if (owner.dialoguePushedRunner == this)
            {
                owner.dialoguePushedRunner = null;
                int scaleX, scaleY;
                owner.spManager.GetScale(SpriteManager.DIALOGUE_SPRITE_A, out scaleX, out scaleY);
                owner.spManager.Move(SpriteManager.DIALOGUE_SPRITE_A, scaleX, scaleY, 15, Color.Transparent,
                        SpriteManager.DIALOGUE_SPRITE_A_POS_X - 20,
                        SpriteManager.DIALOGUE_SPRITE_A_POS_Y + 20);
                owner.spManager.GetScale(SpriteManager.DIALOGUE_SPRITE_B, out scaleX, out scaleY);
                owner.spManager.Move(SpriteManager.DIALOGUE_SPRITE_B, scaleX, scaleY, 15, Color.Transparent,
                        SpriteManager.DIALOGUE_SPRITE_B_POS_X + 20,
                        SpriteManager.DIALOGUE_SPRITE_B_POS_Y + 20);
            }
        }

        private void FuncTelop()
        {
            var msg = curCommand.attrList[0].GetString();

            // あまりに長い場合は適当に分割して先にグリフを取得する
            int ptr = 0;
            const int MAX_STR = 1024;
            var cmd = curCommand;
            if (msg.Length > MAX_STR)
            {
                waiter = () =>
                {
                    var len = MAX_STR;
                    if (len + ptr >= msg.Length)
                        len = msg.Length - ptr;
                    owner.menuWindow.res.textDrawer.MeasureString(msg.Substring(ptr, len));
                    ptr += MAX_STR;

                    if (ptr < msg.Length)
                        return true;

                    FuncTelopImpl(msg, cmd);
                    return true;
                };
            }
            else
               FuncTelopImpl(msg, curCommand);
        }

        private void FuncTelopImpl(string msg, Command cmd)
        {
            int cur = 1;
            var bgType = cmd.attrList[cur++].GetInt();
            int bgImgId = -1;
            int bgSizeX = 1, bgSizeY = 1;
            int spSizeX, spSizeY;
            int scaleX = 100, scaleY = 100;
            int scHW = Graphics.ScreenWidth / 2;
            int scHH = Graphics.ScreenHeight / 2;
            const int MARGIN = 20;
            Guid bgGuid = Guid.Empty;

            // 画像のサイズを調べるために先行して読み込む
            if (bgType == 1)
            {
                bgGuid = cmd.attrList[cur++].GetGuid();
                var rom = owner.owner.catalog.getItemFromGuid(bgGuid) as Common.Resource.Sprite;
                if (rom != null)
                {
                    bgImgId = Graphics.LoadImage(rom.path);
                    bgSizeX = Graphics.GetImageWidth(bgImgId);
                    bgSizeY = Graphics.GetImageHeight(bgImgId);
                }
                else
                {
                    bgType = 2;
                }
            }

            bool isScroll = cmd.attrList[cur++].GetInt() == 1 ? true : false;

            //-------------------------------------------------------------------
            // スクロールする版
            //-------------------------------------------------------------------
            if (isScroll)
            {
                owner.spManager.ShowText(SpriteManager.TELOP_SPRITE_TEXT, msg, 100, Color.White, scHW, scHH, 2);
                owner.spManager.GetSpSize(SpriteManager.TELOP_SPRITE_TEXT, out spSizeX, out spSizeY);
                spSizeX += MARGIN; spSizeY += MARGIN;
                owner.spManager.SetPosition(SpriteManager.TELOP_SPRITE_TEXT, scHW, Graphics.ScreenHeight + spSizeY / 2);
                int showTime = (Graphics.ScreenHeight + spSizeY) * 2;   // テキストの縦幅に応じて流れる時間も変わる
                owner.spManager.Move(SpriteManager.TELOP_SPRITE_TEXT, 100, showTime, Color.White, scHW, -spSizeY / 2);

                // 背景が黒か画像のときは背景も表示する
                if (bgType == 1)
                {
                    owner.spManager.ShowPicture(SpriteManager.TELOP_SPRITE_BG, bgGuid, scaleX, scaleY, Color.Transparent, -1, scHW, scHH);
                    owner.spManager.Move(SpriteManager.TELOP_SPRITE_BG, scaleX, scaleY, 20, Color.White, scHW, scHH);
                }
                else if (bgType == 0)
                {
                    scaleX = Graphics.ScreenWidth * 100 / bgSizeX;// spSizeX * 100 / bgSizeX;
                    scaleY = Graphics.ScreenHeight * 100 / bgSizeY;
                    owner.spManager.ShowRect(SpriteManager.TELOP_SPRITE_BG, scaleX, scaleY, new Color(0, 0, 0, 127),
                        Color.Transparent, 480, 272);
                    owner.spManager.Move(SpriteManager.TELOP_SPRITE_BG, scaleX, scaleY, 20, Color.White, scHW, scHH);
                }

                // 表示完了までウェイト
                bool hiding = false;
                ScriptMutexLock(ref owner.excludedScript);
                waiter = () =>
                {
                    if (owner.spManager.isMoving(SpriteManager.TELOP_SPRITE_TEXT))
                    {
                        // 決定・ダッシュキーが押されてる時は4倍速で流す
                        if (Input.KeyTest(Input.StateType.DIRECT, KeyStates.DECIDE) || Input.KeyTest(Input.StateType.DIRECT, KeyStates.DASH))
                        {
                            owner.spManager.Update(SpriteManager.TELOP_SPRITE_TEXT);
                            owner.spManager.Update(SpriteManager.TELOP_SPRITE_TEXT);
                            owner.spManager.Update(SpriteManager.TELOP_SPRITE_TEXT);
                        }
                        return true;
                    }
                    else if (!hiding)
                    {
                        // 背景が黒か画像のときは背景も消す
                        if (bgType <= 1)
                        {
                            owner.spManager.Move(SpriteManager.TELOP_SPRITE_BG, scaleX, scaleY, 20, Color.Transparent, scHW, scHH);
                            // 画像の時は画像の解放もする
                            if (bgType == 1)
                                Graphics.UnloadImage(bgImgId);
                        }

                        hiding = true;
                        return true;
                    }
                    else if (owner.spManager.isMoving(SpriteManager.TELOP_SPRITE_BG))
                    {
                        return true;
                    }

                    owner.spManager.Hide(SpriteManager.TELOP_SPRITE_BG);
                    owner.spManager.Hide(SpriteManager.TELOP_SPRITE_TEXT);
                    ScriptMutexUnlock(ref owner.excludedScript);
                    return false;
                };
            }
            //-------------------------------------------------------------------
            // スクロールしない版
            //-------------------------------------------------------------------
            else
            {
                owner.spManager.ShowText(SpriteManager.TELOP_SPRITE_TEXT, msg, 100, Color.Transparent, scHW, scHH, 2);
                owner.spManager.Move(SpriteManager.TELOP_SPRITE_TEXT, 100, 20, Color.White);
                owner.spManager.GetSpSize(SpriteManager.TELOP_SPRITE_TEXT, out spSizeX, out spSizeY);
                spSizeX += MARGIN; spSizeY += MARGIN;

                // 背景が黒か画像のときは背景も表示する
                if (bgType == 1)
                {
                    owner.spManager.ShowPicture(SpriteManager.TELOP_SPRITE_BG, bgGuid, scaleX, scaleY, Color.Transparent, -1, scHW, scHH);
                    owner.spManager.Move(SpriteManager.TELOP_SPRITE_BG, scaleX, scaleY, 20, Color.White, scHW, scHH);
                }
                else if (bgType == 0)
                {
                    scaleX = Graphics.ScreenWidth * 100 / bgSizeX;// spSizeX * 100 / bgSizeX;
                    scaleY = spSizeY * 100 / bgSizeY;
                    owner.spManager.ShowRect(SpriteManager.TELOP_SPRITE_BG, scaleX, scaleY, new Color(0, 0, 0, 127),
                        Color.Transparent, 480, 272);
                    owner.spManager.Move(SpriteManager.TELOP_SPRITE_BG, scaleX, scaleY, 20, Color.White, scHW, scHH);
                }

                // 表示完了までウェイト
                bool hiding = false;
                int id = owner.PushMessage("", 2, MessageWindow.WindowType.NONE);
                ScriptMutexLock(ref owner.excludedScript);
                waiter = () =>
                {
                    if (owner.IsQueuedMessage(id))
                    {
                        return true;
                    }
                    else if (!hiding && !owner.spManager.isMoving(SpriteManager.TELOP_SPRITE_TEXT))
                    {
                        owner.spManager.Move(SpriteManager.TELOP_SPRITE_TEXT, 100, 20, Color.Transparent);

                        // 背景が黒か画像のときは背景も消す
                        if (bgType <= 1)
                        {
                            owner.spManager.Move(SpriteManager.TELOP_SPRITE_BG, scaleX, scaleY, 20, Color.Transparent, scHW, scHH);
                            // 画像の時は画像の解放もする
                            if (bgType == 1)
                                Graphics.UnloadImage(bgImgId);
                        }

                        hiding = true;
                        return true;
                    }
                    else if (owner.spManager.isMoving(SpriteManager.TELOP_SPRITE_TEXT))
                    {
                        return true;
                    }

                    owner.spManager.Hide(SpriteManager.TELOP_SPRITE_BG);
                    owner.spManager.Hide(SpriteManager.TELOP_SPRITE_TEXT);
                    ScriptMutexUnlock(ref owner.excludedScript);
                    return false;
                };
            }
            //-------------------------------------------------------------------
        }

        private void FuncDialogue()
        {
            if (curCommand.attrList.Count >= 8)
            {
                int cur = 0;
                var msg = curCommand.attrList[cur++].GetString();
                var winPos = curCommand.attrList[cur++].GetInt();
                var activeIsA = curCommand.attrList[7].GetInt() == 0 ? true : false;
                var flipA = false;
                var flipB = true;
                if (curCommand.attrList.Count == 10)
                {
                    flipA = curCommand.attrList[8].GetBool();
                    flipB = curCommand.attrList[9].GetBool();
                }

                cur++;  //オートカメラ引数は無視

                // A 表示処理
                var guid = curCommand.attrList[cur++].GetGuid();
                var index = curCommand.attrList[cur++].GetInt();
                int scaleX = flipA ? -100 : 100;
                if (guid != Guid.Empty)
                {
                    int posX = SpriteManager.DIALOGUE_SPRITE_A_POS_X;
                    int posY = SpriteManager.DIALOGUE_SPRITE_A_POS_Y;
                    var color = Color.White;
                    if (owner.spManager.GetImageGuid(SpriteManager.DIALOGUE_SPRITE_A) != guid)
                    {
                        posX -= 20;
                        posY += 20;
                        color = Color.Transparent;
                    }
                    if (owner.spManager.isVisible(SpriteManager.DIALOGUE_SPRITE_A))
                    {
                        color = owner.spManager.GetColor(SpriteManager.DIALOGUE_SPRITE_A);
                        owner.spManager.ShowPicture(SpriteManager.DIALOGUE_SPRITE_A,
                            guid, scaleX, 100, color, index);
                    }
                    else
                    {
                        owner.spManager.ShowPicture(SpriteManager.DIALOGUE_SPRITE_A,
                            guid, scaleX, 100, color, index, posX, posY);
                    }

                    // A 移動処理
                    posX = SpriteManager.DIALOGUE_SPRITE_A_POS_X;
                    posY = SpriteManager.DIALOGUE_SPRITE_A_POS_Y;
                    color = Color.White;
                    if (!activeIsA)
                    {
                        posX -= 20;
                        posY += 20;
                        color = new Color(191, 191, 191, 255);
                    }
                    owner.spManager.Move(SpriteManager.DIALOGUE_SPRITE_A, scaleX, 100, 15, color, posX, posY);
                }
                else
                {
                    owner.spManager.Move(SpriteManager.DIALOGUE_SPRITE_A, scaleX, 100, 15, Color.Transparent,
                            SpriteManager.DIALOGUE_SPRITE_A_POS_X - 20,
                            SpriteManager.DIALOGUE_SPRITE_A_POS_Y + 20);
                }

                // B 表示処理
                guid = curCommand.attrList[cur++].GetGuid();
                index = curCommand.attrList[cur++].GetInt();
                scaleX = flipB ? -100 : 100;
                if (guid != Guid.Empty)
                {
                    var posX = SpriteManager.DIALOGUE_SPRITE_B_POS_X;
                    var posY = SpriteManager.DIALOGUE_SPRITE_B_POS_Y;
                    var color = Color.White;
                    if (owner.spManager.GetImageGuid(SpriteManager.DIALOGUE_SPRITE_B) != guid)
                    {
                        posX += 20;
                        posY += 20;
                        color = Color.Transparent;
                    }
                    if (owner.spManager.isVisible(SpriteManager.DIALOGUE_SPRITE_B))
                    {
                        color = owner.spManager.GetColor(SpriteManager.DIALOGUE_SPRITE_B);
                        owner.spManager.ShowPicture(SpriteManager.DIALOGUE_SPRITE_B,
                            guid, scaleX, 100, color, index);
                    }
                    else
                    {
                        owner.spManager.ShowPicture(SpriteManager.DIALOGUE_SPRITE_B,
                            guid, scaleX, 100, color, index, posX, posY);
                    }

                    // B 移動処理
                    posX = SpriteManager.DIALOGUE_SPRITE_B_POS_X;
                    posY = SpriteManager.DIALOGUE_SPRITE_B_POS_Y;
                    color = Color.White;
                    if (activeIsA)
                    {
                        posX += 20;
                        posY += 20;
                        color = new Color(191, 191, 191, 255);
                    }
                    owner.spManager.Move(SpriteManager.DIALOGUE_SPRITE_B, scaleX, 100, 15, color, posX, posY);
                }
                else
                {
                    owner.spManager.Move(SpriteManager.DIALOGUE_SPRITE_B, scaleX, 100, 15, Color.Transparent,
                            SpriteManager.DIALOGUE_SPRITE_B_POS_X + 20,
                            SpriteManager.DIALOGUE_SPRITE_B_POS_Y + 20);
                }

                // 表示完了までウェイト
                ScriptMutexLock(ref owner.excludedScript);
                int id = owner.PushMessage(msg, winPos, MessageWindow.WindowType.DIALOGUE);
                waiter = () =>
                {
                    if (owner.IsQueuedMessage(id))
                        return true;

                    owner.dialoguePushedRunner = this;
                    ScriptMutexUnlock(ref owner.excludedScript);
                    return false;
                };
            }
        }

        private delegate void IntFunc(int num);
        private void FuncInn()
        {
            var intAttr = curCommand.attrList[0] as Script.IntAttr;

            var strs = new String[2];
            var gs = owner.owner.catalog.getGameSettings();
            strs[0] = gs.glossary.innOK + " : " + intAttr.value + gs.glossary.moneyName;
            strs[1] = gs.glossary.innCancel;


            // 選択肢の位置を反映
            int selectionPos = ChoicesWindow.SELECTION_POS_CENTER;
            if (curCommand.attrList.Count > 3)
            {
                selectionPos = curCommand.attrList[3].GetInt();
            }
            ChoicesWindow.nextPosition = selectionPos;
            ChoicesWindow.topMarginForMoneyWindow = true;

            owner.ShowChoices(strs);
            owner.ShowMoneyWindow(true);
            ScriptMutexLock(ref owner.excludedScript);

            int phase = 0;
            float frame = 0;

            IntFunc setPhase = (int num) => { phase = num; frame = 0; };
            Color color = new Color(0, 0, 0, 0);
            float delta;
            byte tmp;

            bool recoverPoison = curCommand.attrList[1].GetInt() > 0;
            bool revive = curCommand.attrList[2].GetInt() > 0;

            AudioCore.SoundDef nowBgm = null;
            Common.Resource.Bgm innJingle = null;

            owner.LockControl();

            // 表示完了までウェイト
            waiter = () =>
            {
                const int PHASE_SELECTING = 0;
                const int PHASE_FADEOUT = 1;
                const int PHASE_WAIT = 2;
                const int PHASE_FADEIN = 3;
                const int PHASE_END = 4;

                const int FADE_FRAME = 60;

                switch (phase)
                {
                    case PHASE_SELECTING: // 選択中
                        var result = owner.GetChoicesResult();
                        if (result != ChoicesWindow.CHOICES_SELECTING)
                            owner.ShowMoneyWindow(false);
                        if (result == 0)
                        {
                            if (owner.owner.data.party.GetMoney() >= intAttr.value)
                            {
                                owner.owner.data.party.AddMoney(-intAttr.value);
                                setPhase(PHASE_FADEOUT);

                                innJingle = owner.owner.catalog.getItemFromGuid(gs.innJingle) as Common.Resource.Bgm;
                                if (innJingle != null)
                                {
                                    nowBgm = Audio.GetNowBgm();
                                    Audio.PlayBgm(innJingle);
                                }
                            }
                            else
                            {
                                owner.PushMessage(gs.glossary.notEnoughMoney, 1, MessageWindow.WindowType.MESSAGE);
                                setPhase(PHASE_END);
                                cur--; SearchElseOrEndIf(); cur++;
                            }
                        }
                        else if (result == 1)
                        {
                            setPhase(PHASE_END);
                            cur--; SearchElseOrEndIf(); cur++;
                        }
                        break;
                    case PHASE_FADEOUT:
                        if (frame >= FADE_FRAME)
                        {
                            setPhase(PHASE_WAIT);
                            color.A = 255;
                            owner.SetScreenColor(color);
                        }
                        else
                        {
                            delta = (float)frame / FADE_FRAME;
                            tmp = (byte)((float)255 * delta);
                            color.A = tmp;
                            owner.SetScreenColor(color);
                        }
                        break;
                    case PHASE_WAIT:
                        if (innJingle == null)
                        {
                            if (frame >= 60)
                            {
                                setPhase(PHASE_FADEIN);
                            }
                        }
                        else
                        {
                            if (!Audio.IsBgmPlaying())
                            {
                                Audio.SwapBgm(nowBgm);
                                setPhase(PHASE_FADEIN);
                            }
                        }
                        fullRecovery(recoverPoison, revive);
                        owner.SetScreenColor(color);
                        break;
                    case PHASE_FADEIN:
                        if (frame >= FADE_FRAME)
                        {
                            setPhase(PHASE_END);
                            color.A = 0;
                            owner.SetScreenColor(color);
                        }
                        else
                        {
                            delta = (float)frame / FADE_FRAME;
                            tmp = (byte)((float)255 * (1 - delta));
                            color.A = tmp;
                            owner.SetScreenColor(color);
                        }
                        break;
                    case PHASE_END:
                        ScriptMutexUnlock(ref owner.excludedScript);
                        owner.UnlockControl();
                        return false;
                }

                frame += GameMain.getRelativeParam60FPS();
                return true;
            };
        }

        private void fullRecovery(bool poison, bool revive)
        {
            // バトルで使われた時はバトルに作用するようにする
            {
                var btlEvtCtl = owner as BattleEventController;
                if (btlEvtCtl != null)
                {
                    btlEvtCtl.fullRecovery(poison, revive);
                    return;
                }
            }

            foreach (var hero in owner.owner.data.party.members)
            {
                if (revive || hero.hitpoint >= 1)
                {
                    hero.hitpoint = hero.maxHitpoint;
                    hero.magicpoint = hero.maxMagicpoint;
                    if (poison)
                        hero.statusAilments = Common.GameData.Hero.StatusAilments.NONE;
                    hero.consistency();
                }
            }
        }

        private void FuncShop()
        {
            const int SELECTION_POS_TYPE_OFFSET = 0x7F000000;

            owner.LockControl();
            ScriptMutexLock(ref owner.excludedScript);
            int curAttr = 0;
            var intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
            int count = intAttr.value;
            var items = new List<Common.Rom.Item>();
            for (int i = 0; i < count; i++)
            {
                var guidAttr = curCommand.attrList[curAttr++] as Script.GuidAttr;
                if (guidAttr == null)
                    continue;
                var itemRom = owner.owner.catalog.getItemFromGuid(guidAttr.value) as Common.Rom.Item;
                if (itemRom != null)
                    items.Add(itemRom);
            }
            var prices = new int[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                if (curCommand.attrList.Count > curAttr)
                {
                    prices[i] = curCommand.attrList[curAttr++].GetInt();
                }
                else
                {
                    prices[i] = items[i].price;
                }
            }

            // 選択肢の位置を反映
            int selectionPos = ChoicesWindow.SELECTION_POS_CENTER;
            if (curCommand.attrList.Count > curAttr && curCommand.attrList[curAttr].GetInt() >= SELECTION_POS_TYPE_OFFSET)
            {
                selectionPos = curCommand.attrList[curAttr].GetInt() - SELECTION_POS_TYPE_OFFSET;
            }
            ChoicesWindow.nextPosition = selectionPos;
            ChoicesWindow.topMarginForMoneyWindow = true;

            owner.ShowShop(items.ToArray(), prices);

            // 表示完了までウェイト
            waiter = () =>
            {
                if (owner.IsShopVisible())
                    return true;

                if (!owner.GetShopResult())
                {
                    cur--; SearchElseOrEndIf(); cur++;
                }

                owner.UnlockControl();
                ScriptMutexUnlock(ref owner.excludedScript);
                return false;
            };
        }

        public enum HeroStatusType
        {
            HITPOINT,
            MAGICPOINT,
            ATTACKPOWER,
            Defense,
            MAGIC,
            SPEED,
        }

        private void FuncStatus()
        {
            int curAttr = 0;

            //---------------操作対象---------------

            var hero = owner.owner.data.party.GetMember(curCommand.attrList[curAttr++].GetGuid());
            if (hero == null)   // 未加入だった場合はなにもしない
                return;
            var type = (HeroStatusType)curCommand.attrList[curAttr++].GetInt();

            //---------------演算---------------

            var num = curCommand.attrList[curAttr++].GetInt();
            var opType = curCommand.attrList[curAttr++].GetInt();

            switch (opType)
            {
                case 0: // 足す
                    break;
                case 1: // 引く
                    num *= -1;
                    break;
            }
            
            // バトル中はバトルにも反映する
            var btlEvtCtl = owner as BattleEventController;
            if (btlEvtCtl != null)
            {
                btlEvtCtl.addStatus(hero, type, num);
                return;
            }

            switch (type)
            {
                case HeroStatusType.HITPOINT:
                    hero.maxHitpoint += num;
                    break;
                case HeroStatusType.MAGICPOINT:
                    hero.maxMagicpoint += num;
                    break;
                case HeroStatusType.ATTACKPOWER:
                    hero.power += num;
                    break;
                case HeroStatusType.MAGIC:
                    hero.magic += num;
                    break;
                case HeroStatusType.Defense:
                    hero.vitality += num;
                    break;
                case HeroStatusType.SPEED:
                    hero.speed += num;
                    break;
            }

            hero.consistency();
            hero.refreshEquipmentEffect();
        }

        private void FuncMoney()
        {
            int curAttr = 0;

            //---------------左辺値---------------

            int left = owner.owner.data.party.GetMoney();

            //---------------右辺値---------------

            var intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;     // 右辺タイプ
            var sourceType = (Command.VarSourceType)intAttr.value;

            int right = 0;

            switch (sourceType)
            {
                case Command.VarSourceType.CONSTANT:
                    intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
                    right = intAttr.value;
                    break;
                case Command.VarSourceType.VARIABLE:
                    intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
                    right = owner.owner.data.system.GetVariable(intAttr.value, mapChr.rom.guId);
                    break;
                case Command.VarSourceType.VARIABLE_REF:
                    intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
                    right = owner.owner.data.system.GetVariable(owner.owner.data.system.GetVariable(intAttr.value, mapChr.rom.guId), mapChr.rom.guId);
                    break;
            }

            //---------------演算---------------

            intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
            var opType = (Command.OperatorType)intAttr.value;

            int result = 0;

            switch (opType)
            {
                case Command.OperatorType.ASSIGNMENT:
                    result = right;
                    break;
                case Command.OperatorType.ADDING:
                    result = left + right;
                    break;
                case Command.OperatorType.SUBTRACTION:
                    result = left - right;
                    break;
            }

            owner.owner.data.party.SetMoney(result);
        }

        private void FuncItem()
        {
            int curAttr = 0;

            //---------------操作対象---------------

            var guidAttr = curCommand.attrList[curAttr++] as Script.GuidAttr;

            //---------------左辺値---------------

            int left = owner.owner.data.party.GetItemNum(guidAttr.value);

            //---------------右辺値---------------

            var intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;     // 右辺タイプ
            var sourceType = (Command.VarSourceType)intAttr.value;

            int right = 0;

            switch (sourceType)
            {
                case Command.VarSourceType.CONSTANT:
                    intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
                    right = intAttr.value;
                    break;
                case Command.VarSourceType.VARIABLE:
                    intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
                    right = owner.owner.data.system.GetVariable(intAttr.value, mapChr.rom.guId);
                    break;
                case Command.VarSourceType.VARIABLE_REF:
                    intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
                    right = owner.owner.data.system.GetVariable(owner.owner.data.system.GetVariable(intAttr.value, mapChr.rom.guId), mapChr.rom.guId);
                    break;
            }

            //---------------演算---------------

            intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
            var opType = (Command.OperatorType)intAttr.value;

            int result = 0;

            switch (opType)
            {
                case Command.OperatorType.ASSIGNMENT:
                    result = right;
                    break;
                case Command.OperatorType.ADDING:
                    result = left + right;
                    break;
                case Command.OperatorType.SUBTRACTION:
                    result = left - right;
                    break;
            }

            // 新規にアイテムスタックが増える場合、インベントリの空きをチェックする
            if (owner.owner.data.party.GetItemNum(guidAttr.value) == 0 && result > 0 &&
                owner.owner.data.party.checkInventoryEmptyNum() <= 0)
            {
                owner.ShowTrashWindow(guidAttr.value, result);

                // 表示完了までウェイト
                waiter = () =>
                {
                    if (owner.IsTrashVisible())
                        return true;

                    return false;
                };
            }
            else
            {
                owner.owner.data.party.SetItemNum(guidAttr.value, result);
            }
        }
        
        private void FuncEquipment()
        {
            int cur = 0;

            var heroGuid = curCommand.attrList[cur++].GetGuid();
            var hero = owner.owner.data.party.GetHero(heroGuid);

            if (hero == null)
                return;

            var equipType = curCommand.attrList[cur++].GetInt();
            if (equipType < 0 || equipType > 5)
                return;
            
            var itemGuid = curCommand.attrList[cur++].GetGuid();
            var item = owner.owner.catalog.getItemFromGuid<Item>(itemGuid);
            var stack = owner.owner.data.party.GetItemNum(itemGuid);

            // アイテムの種類が指定してあって、なおかつ所持していない場合は無視する
            if (item != null && stack == 0)
                return;

            // 現在のアイテムが有る場合は袋に戻す
            var nowEquipped = hero.equipments[equipType];

            hero.equipments[equipType] = item;
            hero.refreshEquipmentEffect();

            // バトルの時はバトルにも反映
            if (owner is BattleEventController)
                ((BattleEventController)owner).refreshEquipmentEffect(hero);

            // 袋から減らす
            owner.owner.data.party.AddItem(itemGuid, -1);

            // 装備しているアイテムがないときはそのまま脱出
            if (nowEquipped == null)
                return;

            // 新規にアイテムスタックが増える場合、インベントリの空きをチェックする
            if (owner.owner.data.party.GetItemNum(nowEquipped.guId) == 0 &&
                owner.owner.data.party.checkInventoryEmptyNum() <= 0)
            {
                owner.ShowTrashWindow(nowEquipped.guId, 1);

                // 表示完了までウェイト
                waiter = () =>
                {
                    if (owner.IsTrashVisible())
                        return true;

                    return false;
                };
            }
            else
            {
                owner.owner.data.party.AddItem(nowEquipped.guId, 1);
            }
        }

        private void FuncVariable()
        {
            int curAttr = 0;

            //---------------操作対象---------------

            var intAttr = curCommand.attrList[curAttr++] as Script.IntAttr; // 書き込み先タイプ
            var destType = (Command.VarDestType)intAttr.value;

            intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;     // 書き込み先
            int destIndex;

            switch (destType)
            {
                case Command.VarDestType.VARIABLE:
                    destIndex = intAttr.value;
                    break;
                case Command.VarDestType.VARIABLE_REF:
                    destIndex = owner.owner.data.system.GetVariable(intAttr.value, mapChr.rom.guId);
                    break;
                default:
                    destIndex = 0;
                    break;
            }

            //---------------左辺値---------------

            int left = owner.owner.data.system.GetVariable(destIndex, mapChr.rom.guId);           // 左辺値

            //---------------右辺値---------------

            intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;     // 右辺タイプ
            var sourceType = (Command.VarSourceType)intAttr.value;

            int right = 0;
            int dummy;

            switch (sourceType)
            {
                case Command.VarSourceType.CONSTANT:
                    intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
                    right = intAttr.value;
                    break;
                case Command.VarSourceType.RANDOM:
                    {
                        int min = curCommand.attrList[curAttr++].GetInt();
                        int max = curCommand.attrList[curAttr++].GetInt();
                        right = owner.GetRandom(max + 1, min);
                    }
                    break;
                case Command.VarSourceType.VARIABLE:
                    intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
                    right = owner.owner.data.system.GetVariable(intAttr.value, mapChr.rom.guId);
                    break;
                case Command.VarSourceType.VARIABLE_REF:
                    intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
                    int srcIndex = owner.owner.data.system.GetVariable(intAttr.value, mapChr.rom.guId);
                    // 読み込み先が変数インデックスの範囲外だった場合は何も起こらないよう修正
                    if (srcIndex < 0 || srcIndex >= Common.Rom.GameSettings.MAX_VARIABLE)
                        return;
                    right = owner.owner.data.system.GetVariable(srcIndex, mapChr.rom.guId);
                    break;
                case Command.VarSourceType.MONEY:
                    right = owner.owner.data.party.GetMoney();
                    break;
                case Command.VarSourceType.ITEM:
                    {
                        var guidAttr = curCommand.attrList[curAttr++] as Script.GuidAttr;
                        right = owner.owner.data.party.GetItemNum(guidAttr.value);
                    }
                    break;
                case Command.VarSourceType.HERO_STATUS:
                    {
                        var guidAttr = curCommand.attrList[curAttr++] as Script.GuidAttr;

                        // バトル中はバトルから情報をもらう
                        var btlEvtCtl = owner as BattleEventController;
                        if (btlEvtCtl != null && btlEvtCtl.existMember(guidAttr.value))
                        {
                            var hero = owner.owner.data.party.GetMember(guidAttr.value, true);
                            var srcTypePlus = (Command.VarHeroSourceType)curCommand.attrList[curAttr++].GetInt();
                            right = btlEvtCtl.getStatus(srcTypePlus, hero);
                        }
                        else
                        {
                            var battleStatus = owner.owner.data.party.GetMember(guidAttr.value, true);
                            if (battleStatus == null)
                            {
                                // 見つからなかったら、ROMからインスタンスを作る(控えメンバーにはあえて入れない)
                                var heroRom = owner.owner.catalog.getItemFromGuid(guidAttr.value) as Common.Rom.Hero;
                                if (heroRom != null)
                                {
                                    battleStatus = Common.GameData.Party.createHeroFromRom(owner.owner.catalog, heroRom);
                                }
                            }

                            intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;

                            // それでも見つからなかったら脱出
                            if (battleStatus == null)
                            {
                                right = 0;
                                break;
                            }

                            var srcTypePlus = (Command.VarHeroSourceType)intAttr.value;
                            right = getPartyStatus(battleStatus, srcTypePlus);
                        }
                    }
                    break;
                case Command.VarSourceType.RTC:
                    {
                        var timeType = (Command.VarTimeSourceType)curCommand.attrList[curAttr++].GetInt();
                        switch (timeType)
                        {
                            case Command.VarTimeSourceType.YEAR: right = DateTime.Now.Year; break;
                            case Command.VarTimeSourceType.MONTH: right = DateTime.Now.Month; break;
                            case Command.VarTimeSourceType.DAY: right = DateTime.Now.Day; break;
                            case Command.VarTimeSourceType.WEEKDAY: right = (int)DateTime.Now.DayOfWeek; break;
                            case Command.VarTimeSourceType.HOUR: right = DateTime.Now.Hour; break;
                            case Command.VarTimeSourceType.MINUTE: right = DateTime.Now.Minute; break;
                            case Command.VarTimeSourceType.SECOND: right = DateTime.Now.Second; break;
                        }
                    }
                    break;
                case Command.VarSourceType.PLAYTIME:
                    {
                        var timeType = (Command.VarTimeSourceType)curCommand.attrList[curAttr++].GetInt();
                        switch (timeType)
                        {
                            case Command.VarTimeSourceType.HOUR: right = owner.owner.data.system.getHour(); break;
                            case Command.VarTimeSourceType.MINUTE: right = owner.owner.data.system.getMinute(); break;
                            case Command.VarTimeSourceType.SECOND: right = owner.owner.data.system.getSecond(); break;
                        }
                    }
                    break;
                case Command.VarSourceType.MAPSIZE_X:
                    right = owner.map.Width;
                    break;
                case Command.VarSourceType.MAPSIZE_Y:
                    right = owner.map.Height;
                    break;
                case Command.VarSourceType.MAP_WEATHER:
                    right = (int)owner.map.envEffect;
                    break;
                case Command.VarSourceType.KEY_INPUT:
                    {
                        Input.KeyStates key = KeyStates.DECIDE;
                        bool repeatType = false;
                        switch (curCommand.attrList[curAttr++].GetInt())
                        {
                            case 0: key = KeyStates.UP; break;
                            case 1: key = KeyStates.DOWN; break;
                            case 2: key = KeyStates.LEFT; break;
                            case 3: key = KeyStates.RIGHT; break;
                            case 4: key = KeyStates.DECIDE; break;
                            case 5: key = KeyStates.CANCEL; break;
                            case 6: key = KeyStates.DASH; break;
                            case 7: key = KeyStates.CAMERA_CONTROL_MODE_CHANGE; break;
                            case 8: key = KeyStates.CAMERA_VERTICAL_ROT_UP; break;
                            case 9: key = KeyStates.CAMERA_VERTICAL_ROT_DOWN; break;
                            case 10:key = KeyStates.CAMERA_HORIZONTAL_ROT_COUNTER_CLOCKWISE; break;
                            case 11:key = KeyStates.CAMERA_HORIZONTAL_ROT_CLOCKWISE; break;
                            case 12:key = KeyStates.CAMERA_ZOOM_IN; break;
                            case 13:key = KeyStates.CAMERA_ZOOM_OUT; break;
                            case 14:key = KeyStates.CAMERA_POSITION_RESET; break;
                            case 15:key = KeyStates.UP; repeatType = true; break;
                            case 16:key = KeyStates.DOWN; repeatType = true; break;
                            case 17:key = KeyStates.LEFT; repeatType = true; break;
                            case 18:key = KeyStates.RIGHT; repeatType = true; break;
                        }
                        if (!repeatType)
                        {
                            // リピートなし
                            if (Input.KeyTest(StateType.TRIGGER, key)) right = 2;
                            else if (Input.KeyTest(StateType.TRIGGER_UP, key)) right = -1;
                            else if (Input.KeyTest(StateType.DIRECT, key)) right = 1;
                            else right = 0;
                        }
                        else
                        {
                            // リピートタイプ
                            if(Input.KeyTest(StateType.REPEAT, key)) right = 1;
                            else right = 0;
                        }
                    }
                    break;
                case Command.VarSourceType.CAMERA:
                    switch (curCommand.attrList[curAttr++].GetInt())
                    {
                        case 0: right = getRoundedInt(owner.xangle); break;
                        case 1: right = getRoundedInt(owner.yangle); break;
                        case 2: right = getRoundedInt(owner.fovy); break;
                        case 3: right = getRoundedInt(owner.dist); break;
                        case 4: right = getRoundedInt(owner.eyeHeight); break;
                        case 5: right = (int)owner.CurrentCameraMode; break;
                    }
                    break;
                case Command.VarSourceType.POS_EVENT_X:
                    if (selfChr == null)
                        break;
                    right = (int)selfChr.x;
                    break;
                case Command.VarSourceType.POS_EVENT_Y:
                    if (selfChr == null)
                        break;
                    right = (int)selfChr.z;
                    break;
                case Command.VarSourceType.POS_EVENT_HEIGHT:
                    if (selfChr == null)
                        break;
                    right = (int)selfChr.y;
                    break;
                case Command.VarSourceType.POS_PLAYER_X:
                    right = (int)owner.hero.x;
                    break;
                case Command.VarSourceType.POS_PLAYER_Y:
                    right = (int)owner.hero.z;
                    break;
                case Command.VarSourceType.POS_PLAYER_HEIGHT:
                    right = (int)owner.hero.y;
                    break;
                case Command.VarSourceType.POS_EVENT_DIR:
                    if (selfChr == null)
                        break;
                    right = selfChr.getDirection();
                    break;
                case Command.VarSourceType.POS_PLAYER_DIR:
                    right = owner.hero.getDirection();
                    break;
                case Command.VarSourceType.POS_PLAYER_SCREEN_X:
                    owner.GetCharacterScreenPos(owner.hero, out right, out dummy, MapScene.EffectPosType.Body);
                    break;
                case Command.VarSourceType.POS_PLAYER_SCREEN_Y:
                    owner.GetCharacterScreenPos(owner.hero, out dummy, out right, MapScene.EffectPosType.Body);
                    break;
                case Command.VarSourceType.POS_EVENT_SCREEN_X:
                    owner.GetCharacterScreenPos(selfChr, out right, out dummy, MapScene.EffectPosType.Body);
                    break;
                case Command.VarSourceType.POS_EVENT_SCREEN_Y:
                    owner.GetCharacterScreenPos(selfChr, out dummy, out right, MapScene.EffectPosType.Body);
                    break;
                case Command.VarSourceType.BATTLE_STATUS_PARTY:
                    var heroIndex = curCommand.attrList[curAttr++].GetInt() - 1;
                    if (owner is BattleEventController)
                    {
                        var btlEvtCtl = owner as BattleEventController;
                        var srcTypePlus = (Command.VarHeroSourceType)curCommand.attrList[curAttr++].GetInt();
                        right = btlEvtCtl.getPartyStatus(srcTypePlus, heroIndex);
                    }
                    else if(owner.owner.data.party.members.Count > heroIndex)
                    {
                        var hero = owner.owner.data.party.members[heroIndex];
                        var srcTypePlus = (Command.VarHeroSourceType)curCommand.attrList[curAttr++].GetInt();
                        right = getPartyStatus(hero, srcTypePlus);
                    }
                    break;
                case Command.VarSourceType.BATTLE_STATUS_MONSTER:
                    if (owner is BattleEventController)
                    {
                        var btlEvtCtl = owner as BattleEventController;
                        var index = curCommand.attrList[curAttr++].GetInt();
                        var srcTypePlus = (Command.VarHeroSourceType)curCommand.attrList[curAttr++].GetInt();
                        right = btlEvtCtl.getEnemyStatus(srcTypePlus, index);
                    }
                    break;
                case Command.VarSourceType.BATTLE_RESULT:
                    right = BattleEventController.lastBattleResult;
                    break;
                case Command.VarSourceType.BATTLE_SKILL_TARGET:
                    right = BattleEventController.lastSkillTargetIndex;
                    break;
                case Common.Rom.Script.Command.VarSourceType.PARTY_NUMBER:
                    if (owner is BattleEventController)
                    {
                        var btlEvtCtl = owner as BattleEventController;
                        right = btlEvtCtl.getPartyNumber();
                    }
                    else
                    {
                        right = owner.owner.data.party.members.Count;
                    }
                    break;
            }

            //---------------演算---------------

            intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
            var opType = (Command.OperatorType)intAttr.value;

            int result = 0;

            switch (opType)
            {
                case Command.OperatorType.ASSIGNMENT:
                    result = right;
                    break;
                case Command.OperatorType.ADDING:
                    result = left + right;
                    break;
                case Command.OperatorType.SUBTRACTION:
                    result = left - right;
                    break;
                case Command.OperatorType.MULTIPLICATION:
                    result = left * right;
                    break;
                case Command.OperatorType.DIVISION:
                    if (right != 0)
                        result = left / right;
                    break;
                case Command.OperatorType.RANDOM:
                    if (right >= 0)
                    {
                        result = left + owner.GetRandom(right + 1);
                    }
                    else
                    {
                        result = left + owner.GetRandom(1, right);
                    }
                    break;
                case Command.OperatorType.SURPLUS:
                    if (right != 0)
                        result = left % right;
                    break;
            }

            owner.owner.data.system.SetVariable(destIndex, result, mapChr.rom.guId);
        }

        private static int getPartyStatus(Common.GameData.Hero battleStatus, Command.VarHeroSourceType srcTypePlus)
        {
            switch (srcTypePlus)
            {
                case Command.VarHeroSourceType.LEVEL:
                    return battleStatus.level;
                case Command.VarHeroSourceType.HITPOINT:
                    return battleStatus.hitpoint;
                case Command.VarHeroSourceType.MAGICPOINT:
                    return battleStatus.magicpoint;
                case Command.VarHeroSourceType.MAXHITPOINT:
                    return battleStatus.maxHitpoint;
                case Command.VarHeroSourceType.MAXMAGICPOINT:
                    return battleStatus.maxMagicpoint;
                case Command.VarHeroSourceType.HP_PERCENT:
                    if (battleStatus.maxHitpoint == 0)
                        return 0;
                    return battleStatus.hitpoint * 100 / battleStatus.maxHitpoint;
                case Command.VarHeroSourceType.MP_PERCENT:
                    if (battleStatus.maxMagicpoint == 0)
                        return 0;
                    return battleStatus.magicpoint * 100 / battleStatus.maxMagicpoint;
                case Command.VarHeroSourceType.ATTACKPOWER:
                    return battleStatus.power + battleStatus.equipmentEffect.attack;
                case Command.VarHeroSourceType.MAGICPOWER:
                    return battleStatus.magic;
                case Command.VarHeroSourceType.DEFENSE:
                    return battleStatus.vitality + battleStatus.equipmentEffect.defense;
                case Command.VarHeroSourceType.DEXTERITY:
                    return battleStatus.equipmentEffect.dexterity;
                case Command.VarHeroSourceType.EVASION:
                    return battleStatus.equipmentEffect.evation;
                case Command.VarHeroSourceType.SPEED:
                    return battleStatus.speed;
                case Command.VarHeroSourceType.EXP:
                    return battleStatus.exp;
                case Command.VarHeroSourceType.STATUSAILMENTS:
                    for (int i = Common.GameData.Hero.MAX_STATUS_AILMENTS; i > 0; i--)
                    {
                        if (battleStatus.statusAilments.HasFlag((Common.GameData.Hero.StatusAilments)(1 << (i - 1))))
                            return i;
                    }
                    return 0;
                case Command.VarHeroSourceType.STATUSAILMENTS_POISON:
                    for (int i = 1; i > 0; i--) // 1=POISON
                    {
                        if (battleStatus.statusAilments.HasFlag((Common.GameData.Hero.StatusAilments)(1 << (i - 1))))
                            return i;
                    }
                    return 0;
                case Command.VarHeroSourceType.PARTYINDEX:
                    return GameMain.instance.data.party.members.IndexOf(battleStatus) + 1;
            }

            return 0;
        }

        internal static int getBattleStatus(BattleCharacterBase battleStatus, Command.VarHeroSourceType srcTypePlus, List<BattlePlayerData> party)
        {
            switch (srcTypePlus)
            {
                case Command.VarHeroSourceType.LEVEL:
                    if (battleStatus is BattlePlayerData)
                        return ((BattlePlayerData)battleStatus).player.level;
                    return 0;
                case Command.VarHeroSourceType.HITPOINT:
                    return battleStatus.HitPoint;
                case Command.VarHeroSourceType.MAGICPOINT:
                    return battleStatus.MagicPoint;
                case Command.VarHeroSourceType.MAXHITPOINT:
                    return battleStatus.MaxHitPoint;
                case Command.VarHeroSourceType.MAXMAGICPOINT:
                    return battleStatus.MaxMagicPoint;
                case Command.VarHeroSourceType.HP_PERCENT:
                    if (battleStatus.MaxHitPoint == 0)
                        return 0;
                    return battleStatus.HitPoint * 100 / battleStatus.MaxHitPoint;
                case Command.VarHeroSourceType.MP_PERCENT:
                    if (battleStatus.MaxMagicPoint == 0)
                        return 0;
                    return battleStatus.MagicPoint * 100 / battleStatus.MaxMagicPoint;
                case Command.VarHeroSourceType.ATTACKPOWER:
                    return battleStatus.Attack;
                case Command.VarHeroSourceType.MAGICPOWER:
                    return battleStatus.Magic;
                case Command.VarHeroSourceType.DEFENSE:
                    return battleStatus.Defense;
                case Command.VarHeroSourceType.DEXTERITY:
                    return battleStatus.Dexterity;
                case Command.VarHeroSourceType.EVASION:
                    return battleStatus.Evation;
                case Command.VarHeroSourceType.SPEED:
                    return battleStatus.Speed;
                case Command.VarHeroSourceType.EXP:
                    if (battleStatus is BattleEnemyData)
                        return ((BattleEnemyData)battleStatus).monster.exp;
                    else if(battleStatus is BattlePlayerData)
                        return ((BattlePlayerData)battleStatus).player.exp;
                    return 0;
                case Command.VarHeroSourceType.STATUSAILMENTS:
                    for(int i=Common.GameData.Hero.MAX_STATUS_AILMENTS; i>0; i--)
                    {
                        if (battleStatus.Status.HasFlag((Common.GameData.Hero.StatusAilments)(1 << (i - 1))))
                            return i;
                    }
                    return 0;
                case Command.VarHeroSourceType.STATUSAILMENTS_POISON:
                    for (int i = 1; i > 0; i--) // 1=POISON
                    {
                        if (battleStatus.Status.HasFlag((Common.GameData.Hero.StatusAilments)(1 << (i - 1))))
                            return i;
                    }
                    return 0;
                case Command.VarHeroSourceType.PARTYINDEX:
                    return party.IndexOf((BattlePlayerData)battleStatus) + 1;
            }

            return 0;
        }

        private int getRoundedInt(float p)
        {
            if (p == 0)
                return (int)p;
            return (int)(p + (p / Math.Abs(p)) * 0.5f);
        }

        private void SearchBranch(int branchIndex)
        {
            int indent = curCommand.indent;
            Command cmd;
            while (true)
            {
                cmd = script.commands[cur];

                // スクリプトの終わりまで行っちゃったら脱出
                if (script.commands.Count == cur + 1)
                    break;

                // ENDIFも脱出
                if (cmd.indent == indent && cmd.type == Command.FuncType.ENDIF)
                    break;

                // 目的のBRANCHが見つかったら脱出
                if (cmd.indent == indent && cmd.type == Command.FuncType.BRANCH && cmd.attrList.Count == 1)
                {
                    var intAttr = cmd.attrList[0] as Script.IntAttr;
                    if (intAttr != null && intAttr.value == branchIndex)
                    {
                        cur++;
                        break;
                    }
                }

                cur++;
            }
        }

        private void FuncScreenEffect()
        {
            int curAttr = 0;
            var intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;

            var type = (Command.ScreenEffectType)intAttr.value;

            intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
            float waitFrame = intAttr.value;

            intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
            if (intAttr.value == 1)  // 秒に変換
            {
                waitFrame = waitFrame * 60 / 1000;
            }

            //float maxWaitFrame = waitFrame;
            var nowColor = new Microsoft.Xna.Framework.Color();
            var tgtColor = new Microsoft.Xna.Framework.Color();

            switch (type)
            {
                case Command.ScreenEffectType.FADE_IN:
                    nowColor = new Microsoft.Xna.Framework.Color(0, 0, 0, 255);
                    tgtColor = new Microsoft.Xna.Framework.Color(0, 0, 0, 0);
                    FuncScreenColor(nowColor, tgtColor, waitFrame);
                    break;
                case Command.ScreenEffectType.FADE_OUT:
                    nowColor = new Microsoft.Xna.Framework.Color(0, 0, 0, 0);
                    tgtColor = new Microsoft.Xna.Framework.Color(0, 0, 0, 255);
                    FuncScreenColor(nowColor, tgtColor, waitFrame);
                    break;
                case Command.ScreenEffectType.COLOR_CHANGE:
                    {
                        nowColor = owner.GetNowScreenColor();

                        intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                        int r = (byte)((intAttr.value & 0xFF000000) >> 24);
                        int g = (intAttr.value & 0x00FF0000) >> 16;
                        int b = (intAttr.value & 0x0000FF00) >> 8;
                        int a = (intAttr.value & 0x000000FF);
                        tgtColor = new Microsoft.Xna.Framework.Color(r, g, b, a);

                        FuncScreenColor(nowColor, tgtColor, waitFrame);
                    }
                    break;
                case Command.ScreenEffectType.SHAKE:
                    intAttr = curCommand.attrList[curAttr++] as Common.Rom.Script.IntAttr;
                    FuncScreenShake(intAttr.value, waitFrame);
                    break;

                default:
                    break;
            }
        }

        private void FuncScreenShake(int force, float waitFrame, bool async = false)
        {
            // 表示完了までウェイト
            Func<bool> func = () =>
            {
                waitFrame -= GameMain.getRelativeParam60FPS();

                SharpKmyMath.Vector2[] list = new SharpKmyMath.Vector2[]{
                    new SharpKmyMath.Vector2(1f,0),
                    new SharpKmyMath.Vector2(-0.7f,-0.7f),
                    new SharpKmyMath.Vector2(0,1f),
                    new SharpKmyMath.Vector2(0.7f,-0.7f),
                    new SharpKmyMath.Vector2(0,-1f),
                    new SharpKmyMath.Vector2(0.7f,0.7f),
                    new SharpKmyMath.Vector2(-1f,0),
                    new SharpKmyMath.Vector2(-0.7f,0.7f),
                };

                if (waitFrame > 0)
                {
                    int interval = 3;//次のシェイク位置に行くまでの間隔
                    float baseRadius = 0.01f;
                    var shake = list[(((int)waitFrame) / interval) % 4] * force * baseRadius;
                    owner.ShakeValue = shake;
                    shakeForUnity(shake, false);
                }
                else
                {
                    var shake = new SharpKmyMath.Vector2(0, 0);
                    owner.ShakeValue = shake;
                    shakeForUnity(shake, true);
                    return false;
                }
                return true;
            };

            if (async)
                owner.owner.pushTask(func);
            else
                waiter = func;
        }

        private void shakeForUnity(SharpKmyMath.Vector2 shakeValue, bool isFinish)
        {
#if !WINDOWS
            UnityEngine.Camera.main.ResetProjectionMatrix();
            // カメラのプロジェクション行列を揺れ幅の分だけ平行移動させることでシェイクする
            UnityEngine.Matrix4x4 translation = UnityEngine.Matrix4x4.Translate(new UnityEngine.Vector3(shakeValue.x, shakeValue.y, 0));
            UnityEngine.Matrix4x4 shakedProjection = translation * UnityEngine.Camera.main.projectionMatrix;
            UnityEngine.Camera.main.projectionMatrix = shakedProjection;

            if (isFinish)
            {
                UnityEngine.Camera.main.ResetProjectionMatrix(); // プロジェクション行列の設定をリセットしておく
            }
#endif
        }

        private void FuncScreenFlash(float waitFrame)
        {
            float maxWaitFrame = waitFrame;

            // 表示完了までウェイト
            waiter = () =>
            {
                waitFrame -= GameMain.getRelativeParam60FPS();

                float delta = waitFrame / maxWaitFrame;
                if (delta < 0)
                    delta = 0;

                delta *= 2;
                if (delta > 1)
                    delta = 2.0f - delta;

                var color = new Color(delta, delta, delta, delta);

                owner.SetScreenColor(color);

                if (waitFrame < 0)
                {
                    return false;
                }
                return true;
            };
        }

        private void FuncScreenColor(Color nowColor, Color tgtColor, float waitFrame, bool async = false)
        {
            float maxWaitFrame = waitFrame;

            Action finalizer = () =>
            {
                owner.SetScreenColor(tgtColor);
            };
            addFinalizer(finalizer);

            // 表示完了までウェイト
            Func<bool> func = () =>
            {
                //if (owner.isBattle)
                //    return true;

                waitFrame -= GameMain.getRelativeParam60FPS();

                var color = new Microsoft.Xna.Framework.Color();
                color.PackedValue = nowColor.PackedValue;

                float delta = waitFrame / maxWaitFrame;
                if (delta < 0)
                    delta = 0;

                color.R = (byte)((float)nowColor.R * delta + (float)tgtColor.R * (1 - delta));
                color.G = (byte)((float)nowColor.G * delta + (float)tgtColor.G * (1 - delta));
                color.B = (byte)((float)nowColor.B * delta + (float)tgtColor.B * (1 - delta));
                color.A = (byte)((float)nowColor.A * delta + (float)tgtColor.A * (1 - delta));

                owner.SetScreenColor(color);

                if (waitFrame < 0)
                {
                    executeFinalizer(finalizer);
                    return false;
                }
                return true;
            };

            if (async)
                owner.owner.pushTask(func);
            else
                waiter = func;
        }

        private void FuncEffect()
        {
            int curAttr = 0;
            var intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
            var tgt = (Command.EffectTarget)intAttr.value;
            var spNo = 0;

            if (tgt == Command.EffectTarget.SPRITE)
            {
                intAttr = curCommand.attrList[curAttr++] as Script.IntAttr;
                spNo = intAttr.value;
            }

            var guidAttr = curCommand.attrList[curAttr++] as Script.GuidAttr;
            var rom = owner.owner.catalog.getItemFromGuid(guidAttr.value) as Common.Rom.Effect;
            if (rom == null)
                return;

            EffectDrawer drawer = null;
            bool sync = true;
            if (curCommand.attrList.Count > curAttr)
                sync = curCommand.attrList[curAttr++].GetBool();

            if (sync)
            {
                // 同期処理の場合、イベントでdrawerを使い回す
                if (efDrawer != null && efDrawer.rom != null && efDrawer.rom.guId != rom.guId)
                {
                    unloadBattleEffect();
                }

                if (efDrawer == null)
                {
                    efDrawer = new EffectDrawer();
                    efDrawer.load(rom, owner.owner.catalog);
                }
                efDrawer.initialize();
                efDrawer.update();
                drawer = efDrawer;
            }
            else
            {
                // 非同期処理の場合、その場限りのdrawerを作る
                drawer = new EffectDrawer();
                drawer.load(rom, owner.owner.catalog);
                drawer.initialize();
                drawer.update();
            }

            // 描画対象がスクリーンのエフェクトは強制的に画面全体にする
            if (rom.origPos == Common.Rom.Effect.OrigPos.ORIG_SCREEN)
                tgt = Command.EffectTarget.SCREEN;

            Action finalizer = () =>
            {
                // 非同期の場合、後始末を行う
                if (!sync)
                    drawer.finalize();
            };
            addFinalizer(finalizer);

            var selfChrForEffectTarget = selfChr;

            // 表示完了までウェイト
            Func<bool> proc = () =>
            {
                var curFrameIsEnd = drawer.isEndPlaying;

                drawer.update();

                int x = 0;
                int y = 0;
                
                switch (tgt)
                {
                    case Command.EffectTarget.THIS_EVENT:
                        {
                            if (selfChrForEffectTarget == null)
                            {
                                // コモンイベントで描画対象がいなかったらどこか遠いところに描画する
                                if (owner is BattleEventController)
                                {
                                    x = Graphics.ViewportWidth / 2;
                                    y = Graphics.ViewportHeight / 2;
                                }
                                else
                                {
                                    x = 10000;
                                }
                            }
                            else
                            {
                                owner.GetCharacterScreenPos(selfChrForEffectTarget, out x, out y,
                                    rom.origPos == Common.Rom.Effect.OrigPos.ORIG_FOOT ? MapScene.EffectPosType.Ground : MapScene.EffectPosType.Body);
                                var color = drawer.getNowTargetColor();
                                selfChrForEffectTarget.mapHeroSymbol = true;
                                selfChrForEffectTarget.useOverrideColor = true;
                                selfChrForEffectTarget.ChangeColor(color.R, color.G, color.B, color.A);
                                owner.SetEffectColor(selfChrForEffectTarget, color);
                            }
                        }
                        break;
                    case Command.EffectTarget.HERO:
                        {
                            var hero = owner.GetHeroForBattle();
                            owner.GetCharacterScreenPos(hero, out x, out y,
                                rom.origPos == Common.Rom.Effect.OrigPos.ORIG_FOOT ? MapScene.EffectPosType.Ground : MapScene.EffectPosType.Body);
                            var color = drawer.getNowTargetColor();
                            hero.useOverrideColor = true;
                            hero.ChangeColor(color.R, color.G, color.B, color.A);
                            owner.SetEffectColor(hero, color);
                        }
                        break;
                    case Command.EffectTarget.SCREEN:
                        x = Graphics.ViewportWidth / 2;
                        y = Graphics.ViewportHeight / 2;
                        break;
                    case Command.EffectTarget.SPRITE:
                        owner.SetEffectTargetColor(spNo, drawer.getNowTargetColor());
                        if (!owner.GetSpritePos(spNo, out x, out y)) return false;
                        break;
                }

                owner.PushEffectDrawEntry(drawer, x, y);
                //efDrawer.draw(x, y);

                if (curFrameIsEnd)
                {
                    // 同期の場合、ここではeffectDrawer自体はファイナライズしない
                    switch (tgt)
                    {
                        case Command.EffectTarget.THIS_EVENT:
                            if (selfChrForEffectTarget == null)
                                break;

                            if (owner is BattleEventController)
                            {
                                // 何もしない
                            }
                            else
                            {
                                selfChrForEffectTarget.mapHeroSymbol = false;
                                selfChrForEffectTarget.useOverrideColor = false;
                            }
                            break;
                        case Command.EffectTarget.HERO:
                            var hero = owner.GetHero();
                            hero.useOverrideColor = false;
                            break;
                        case Command.EffectTarget.SPRITE:
                            owner.SetEffectTargetColor(spNo, Color.White);
                            break;
                    }
                    
                    executeFinalizer(finalizer);
                    return false;
                }

                return true;
            };

            if (sync)
                waiter = proc;
            else
                owner.owner.pushTask(proc);
        }

        internal void Resume(bool callByAuto = false)
        {
            if (state == ScriptState.Paused && !callByAuto)
            {
                state = ScriptState.Running;
            }
            else if (state == ScriptState.DeepPaused && callByAuto)
            {
                state = ScriptState.Running;
            }
        }

        internal void Pause(bool callByAuto = false)
        {
            if (state == ScriptState.Running || state == ScriptState.DeepPaused && !callByAuto)
            {
                if (callByAuto)
                    state = ScriptState.DeepPaused; // 自動で停止した場合は、 DeepPaused 状態にする
                else
                    state = ScriptState.Paused;
            }
        }

        private void SearchEndLoop()
        {
            Command cmd;
            int tmpCur = cur;
            var tmpStack = new Stack<int>(stack.Reverse());
            int nest = tmpStack.Count;
            while (true)
            {
                tmpCur++;
                if (script.commands.Count == tmpCur)
                {
                    break;
                }

                cmd = script.commands[tmpCur];
                if (cmd.type == Command.FuncType.LOOP)
                {
                    tmpStack.Push(cur);
                }
                else if (cmd.type == Command.FuncType.ENDLOOP)
                {
                    tmpStack.Pop();

                    if (tmpStack.Count == nest - 1)
                    {
                        // 見つかったので脱出
                        stack = new Stack<int>(tmpStack.Reverse());
                        cur = tmpCur;
                        break;
                    }
                }

                else if (cmd.type == Command.FuncType.LABEL && cmd.attrList.Count == 1)
                {
                    var attr = cmd.attrList[0] as Script.IntAttr;
                    PushLabel(attr.value, tmpStack);
                }
            }
        }

        private void PushLabel(int index, Stack<int> curStack)
        {
            if (labels[index] != null)
                return;

            labels[index] = new Stack<int>(curStack.Reverse());   // 現在のスタックをコピー
            labels[index].Push(cur);                    // 現在の行を積む
        }

        private void SearchLabel(int index)
        {
            Command cmd;
            int tmpCur = cur;
            var tmpStack = new Stack<int>(stack.Reverse());
            while (true)
            {
                tmpCur++;
                if (script.commands.Count == tmpCur)
                {
                    break;
                }

                cmd = script.commands[tmpCur];
                if (cmd.type == Command.FuncType.LOOP)
                {
                    tmpStack.Push(cur);
                }

                if (cmd.type == Command.FuncType.ENDLOOP)
                {
                    tmpStack.Pop();
                }

                if (cmd.type == Command.FuncType.LABEL && cmd.attrList.Count == 1)
                {
                    var foundIndex = cmd.attrList[0].GetInt();
                    labels[foundIndex] = new Stack<int>(tmpStack.Reverse());
                    labels[foundIndex].Push(tmpCur);
                }
            }
        }

        private void JumpLabel(int index)
        {
            if (labels[index] != null)
            {
                stack = new Stack<int>(labels[index].Reverse());  // ラベル定義のスタックをコピー
                cur = stack.Pop();                    // ジャンプ先の行をスタックから取得
            }
            else
            {
                SearchLabel(index);
                JumpLabel(index);
            }
        }

        private void SearchElseOrEndIf()
        {
            int indent = curCommand.indent;
            Command cmd;
            while (true)
            {
                cur++;
                if (script.commands.Count <= cur)
                    break;
                cmd = script.commands[cur];
                if (cmd.indent == indent &&
                    (cmd.type == Command.FuncType.ELSE || cmd.type == Command.FuncType.ENDIF))
                    break;
            }
        }

        private bool FuncIf()
        {
            int left = 0;
            int right = 0;
            int cur = 0;
            int cond = 0;

            var intAttr = curCommand.attrList[cur++] as Script.IntAttr;
            var srcType = (Command.IfSourceType)intAttr.value;
            Command.IfHeroSourceType srcTypePlus = 0;

            // 左辺
            switch (srcType)
            {
                case Command.IfSourceType.SWITCH:
                    intAttr = curCommand.attrList[cur++] as Script.IntAttr;
                    left = owner.owner.data.system.GetSwitch(intAttr.value, mapChr.rom.guId) ? 1 : 0;
                    break;
                case Command.IfSourceType.VARIABLE:
                    intAttr = curCommand.attrList[cur++] as Script.IntAttr;
                    left = owner.owner.data.system.GetVariable(intAttr.value, mapChr.rom.guId);
                    break;
                case Command.IfSourceType.MONEY:
                    left = owner.owner.data.party.GetMoney();
                    break;
                case Command.IfSourceType.ITEM:
                    {
                        var guidAttr = curCommand.attrList[cur++] as Script.GuidAttr;
                        left = owner.owner.data.party.GetItemNum(guidAttr.value);
                    }
                    break;
                case Command.IfSourceType.HERO:
                    {
                        var guidAttr = curCommand.attrList[cur++] as Script.GuidAttr;
                        left = owner.owner.data.party.ExistMember(guidAttr.value) ? 1 : 0;
                    }
                    break;
                case Command.IfSourceType.HERO_STATUS:
                    {
                        var guidAttr = curCommand.attrList[cur++] as Script.GuidAttr;
                        var hero = owner.owner.data.party.GetMember(guidAttr.value);
                        intAttr = curCommand.attrList[cur++] as Script.IntAttr;
                        srcTypePlus = (Command.IfHeroSourceType)intAttr.value;

                        // バトル中はバトルから情報をもらう
                        var btlEvtCtl = owner as BattleEventController;
                        if (btlEvtCtl != null && owner.owner.data.party.ExistMember(guidAttr.value))
                        {
                            int option = 0;
                            if (srcTypePlus == Command.IfHeroSourceType.STATUS_AILMENTS)
                                option = curCommand.attrList[cur++].GetInt();
                            left = btlEvtCtl.getStatus(srcTypePlus, option, hero);
                        }
                        else
                        {
                            switch (srcTypePlus)
                            {
                                case Command.IfHeroSourceType.STATUS_AILMENTS:
                                    intAttr = curCommand.attrList[cur++] as Script.IntAttr;
                                    if (
                                        ((intAttr.value == (int)Command.IfHeroAilmentType.POISON) &&
                                        (hero.statusAilments & Common.GameData.Hero.StatusAilments.POISON) != 0) ||
                                        ((intAttr.value == (int)Command.IfHeroAilmentType.DOWN) &&
                                        (hero.statusAilments & Common.GameData.Hero.StatusAilments.DOWN) != 0)
                                    )
                                        left = 1;
                                    else
                                        left = 0;
                                    break;
                                case Command.IfHeroSourceType.LEVEL:
                                    left = hero.level;
                                    break;
                                case Command.IfHeroSourceType.HITPOINT:
                                    left = hero.hitpoint;
                                    break;
                                case Command.IfHeroSourceType.MAGICPOINT:
                                    left = hero.magicpoint;
                                    break;
                                case Command.IfHeroSourceType.ATTACKPOWER:
                                    left = hero.equipmentEffect.attack; // TODO 力を考慮
                                    break;
                                case Command.IfHeroSourceType.Defense:
                                    left = hero.equipmentEffect.defense; // TODO 体力を考慮
                                    break;
                                case Command.IfHeroSourceType.POWER:
                                    left = hero.power;
                                    break;
                                case Command.IfHeroSourceType.VITALITY:
                                    left = hero.vitality;
                                    break;
                                case Command.IfHeroSourceType.MAGIC:
                                    left = hero.magic;
                                    break;
                                case Command.IfHeroSourceType.SPEED:
                                    left = hero.speed;
                                    break;
                                case Command.IfHeroSourceType.EQUIPMENT_WEIGHT:
                                    left = hero.equipmentEffect.weight;
                                    break;
                            }
                        }
                    }
                    break;
            }

            // 右辺
            if (srcType == Command.IfSourceType.SWITCH || srcType == Command.IfSourceType.HERO ||
                srcType == Command.IfSourceType.HERO_STATUS && srcTypePlus == 0)
            {
                intAttr = curCommand.attrList[cur++] as Script.IntAttr;
                cond = intAttr.value == 0 ? COND_EQUAL : COND_NOT_EQUAL;
                right = 1;
            }
            else
            {
                intAttr = curCommand.attrList[cur++] as Script.IntAttr;
                switch ((Command.IfSourceType2)intAttr.value)
                {
                    case Command.IfSourceType2.CONSTANT:
                        {
                            intAttr = curCommand.attrList[cur++] as Script.IntAttr;
                            right = intAttr.value;
                            var intAttr2 = curCommand.attrList[cur++] as Script.IntAttr;
                            cond = intAttr2.value;
                        }
                        break;
                    case Command.IfSourceType2.VARIABLE:
                        {
                            intAttr = curCommand.attrList[cur++] as Script.IntAttr;
                            right = owner.owner.data.system.GetVariable(intAttr.value, mapChr.rom.guId);
                            var intAttr2 = curCommand.attrList[cur++] as Script.IntAttr;
                            cond = intAttr2.value;
                        }
                        break;
                }
            }

            switch ((Command.ConditionType)cond)
            {
                case Command.ConditionType.EQUAL:
                    return left == right;
                case Command.ConditionType.NOT_EQUAL:
                    return left != right;
                case Command.ConditionType.EQUAL_GREATER:
                    return left >= right;
                case Command.ConditionType.EQUAL_LOWER:
                    return left <= right;
                case Command.ConditionType.GREATER:
                    return left > right;
                case Command.ConditionType.LOWER:
                    return left < right;
            }
            return false;
        }

        private void FuncRotate(MapCharacter chr, bool isCameraRotate)
        {
            int[] nextDir = { Util.DIR_SER_RIGHT, Util.DIR_SER_LEFT, Util.DIR_SER_UP, Util.DIR_SER_DOWN };

            // 引数に従ってキャラクターの向きを変更
            int rotateCount = 0;
            var attr = curCommand.attrList[0] as Script.IntAttr;
            int dir = attr.value;   // 上下左右はそのまま代入
            if (dir > (int)Command.MoveType.RIGHT)
            {
                // 特殊な方向指定に対応
                switch ((Command.MoveType)dir)
                {
                    case Command.MoveType.RANDOM:
                        dir = owner.GetRandom(4, 0);
                        break;
                    case Command.MoveType.FOLLOW:
                        {
                            var tgt = owner.GetHero();
                            if (tgt == chr) tgt = mapChr;
                            dir = calcDir(tgt, chr);
                        }
                        break;
                    case Command.MoveType.ESCAPE:
                        {
                            var tgt = owner.GetHero();
                            if (tgt == chr) tgt = mapChr;
                            dir = calcDir(tgt, chr, true);
                        }
                        break;
                    case Command.MoveType.SPIN:
                        {
                            dir = chr.getDirection();
                            dir = nextDir[dir];
                            rotateCount = 3;
                        }
                        break;
                }
            }

            if (isCameraRotate)
            {
                int cameraRotateDegreeY = 0;

                switch ((Command.MoveType)attr.value)
                {
                    case Command.MoveType.SPIN:
                        cameraRotateDegreeY = -360;
                        break;

                    default:
                        cameraRotateDegreeY = GetCameraRotateAngleY(chr.getDirection(), dir);
                        break;
                }

                owner.EventScrollCameraY(cameraRotateDegreeY);
            }

            bool oldFixed = chr.fixDirection;
            chr.fixDirection = false;
            chr.setDirection(dir);

            Action finalizer = () =>
            {
                owner.isCameraMoveByCameraFunc--;
            };
            addFinalizer(finalizer);

            // 移動完了までウェイト
            int count = 0;
            owner.isCameraMoveByCameraFunc++;
            waiter = () =>
            {
                if (count < 20 && chr.IsRotating())
                {
                    count++;
                    return true;
                }

                // くるっと一回転する処理
                if (rotateCount > 0)
                {
                    dir = nextDir[dir];
                    chr.setDirection(dir);
                    rotateCount--;
                    return true;
                }

                if (owner.IsCameraScroll) return true;

                chr.fixDirection = oldFixed;
                executeFinalizer(finalizer);
                return false;
            };
        }

        private void addFinalizer(Action finalizer)
        {
            finalizerQueue.Add(finalizer);
        }

        public static int calcDir(MapCharacter tgt, MapCharacter chr, bool reverse = false)
        {
            float dx = chr.x - tgt.x;
            float dy = chr.z - tgt.z;
            float ax = Math.Abs(dx);
            float ay = Math.Abs(dy);
            if (reverse)
            {
                dx *= -1;
                dy *= -1;
            }

            // 左右
            if (ax > ay)
            {
                if (dx < 0)
                {
                    return Util.DIR_SER_RIGHT;
                }
                else
                {
                    return Util.DIR_SER_LEFT;
                }
            }
            // 上下
            else
            {
                if (dy < 0)
                {
                    return Util.DIR_SER_DOWN;
                }
                else
                {
                    return Util.DIR_SER_UP;
                }
            }
        }

        private int calcSecondDir(int firstDir, MapCharacter tgt, MapCharacter chr, bool reverse = false)
        {

            float dx = chr.x - tgt.x;
            float dy = chr.z - tgt.z;
            if (firstDir == Util.DIR_SER_DOWN || firstDir == Util.DIR_SER_UP)
            {
                if (dx < 0)
                {
                    return Util.DIR_SER_RIGHT;
                }
                else
                {
                    return Util.DIR_SER_LEFT;
                }
            }
            else
            {
                if (dy < 0)
                {
                    return Util.DIR_SER_DOWN;
                }
                else
                {
                    return Util.DIR_SER_UP;
                }
            }
        }

        private void FuncMove(MapCharacter chr, bool isCameraRotate)
        {
            // 引数に従ってキャラクターを移動
            int curAttr = 0;
            var attr = curCommand.attrList[curAttr++] as Script.IntAttr;
            int dir = attr.value;   // 上下左右の移動はそのまま代入

            bool runHit2Script = false;
            if (curCommand.attrList.Count == 2 &&
                curCommand.attrList[curAttr++].GetInt() == 1)
                runHit2Script = true;

            int walkCount = 1;
            bool fixDir = false;
            bool abortOnFail = true;
            bool through = false;
            bool ignoreHeight = false;

            if (curCommand.attrList.Count >= 5)
            {
                walkCount = curCommand.attrList[curAttr++].GetInt();
                fixDir = curCommand.attrList[curAttr++].GetInt() == 0 ? false : true;
                abortOnFail = curCommand.attrList[curAttr++].GetInt() == 0 ? false : true;
                through = curCommand.attrList[curAttr++].GetInt() == 0 ? false : true;

                if (curCommand.attrList.Count > curAttr)
                {
                    ignoreHeight = curCommand.attrList[curAttr++].GetInt() == 0 ? false : true;
                }
            }

            // 移動完了までウェイト
            waiter = () =>
            {
                if (chr.IsMoving() || (isCameraRotate && owner.IsCameraScroll))
                {
                    return true;
                }

                if (walkCount > 0)
                {
                    if (attr.value > (int)Command.MoveType.RIGHT)
                    {
                        // 特殊な方向指定に対応
                        switch ((Command.MoveType)attr.value)
                        {
                            case Command.MoveType.RANDOM:
                                dir = owner.GetRandom(4, 0);
                                break;
                            case Command.MoveType.FOLLOW:
                                {
                                    var tgt = owner.GetHero();
                                    if (tgt == chr) tgt = mapChr;
                                    dir = calcDir(tgt, chr);
                                }
                                break;
                            case Command.MoveType.ESCAPE:
                                {
                                    var tgt = owner.GetHero();
                                    if (tgt == chr) tgt = mapChr;
                                    dir = calcDir(tgt, chr, true);
                                }
                                break;
                        }
                    }

                    if (!fixDir && isCameraRotate)
                    {
                        owner.EventScrollCameraY(GetCameraRotateAngleY(chr.getDirection(), dir));
                    }


                    bool nowCollidableState = chr.collidable;
                    if (through)
                        chr.collidable = false;

                    bool nowFixDirState = chr.fixDirection;
                    if (fixDir)
                        chr.fixDirection = true;

                    bool result = false;
                    if (owner is BattleEventController)
                        result = ((BattleEventController)owner).MoveCharacter(chr, dir, owner.mapDrawer, runHit2Script, ignoreHeight);
                    else
                        result = owner.mapEngine.MoveCharacter(chr, dir, owner.mapDrawer, runHit2Script, ignoreHeight);
                    if (result)
                    {
                        //chr.ChangeSpeed(-3);
                        chr.playMotion("walk", 0.2f, true);
                        walkCount--;
                    }
                    else
                    {
                        if (abortOnFail)
                        {
                            chr.playMotion("wait");
                            walkCount = 0;
                        }
                    }

                    chr.collidable = nowCollidableState;
                    chr.fixDirection = nowFixDirState;
                    return true;
                }

                if (script.commands.Count <= cur)
                {
                    chr.playMotion("wait");
                }
                else if (chr == owner.GetHero())
                {
                    if (script.commands[cur].type != Command.FuncType.PLWALK)
                    {
                        chr.playMotion("wait");
                    }
                }
                else
                {
                    if (script.commands[cur].type != Command.FuncType.WALK)
                    {
                        chr.playMotion("wait");
                    }
                }
                return false;
            };
        }

        private void FuncRouteMove(MapCharacter chr, bool isCameraRotate)
        {
            // 引数に従ってキャラクターを移動
            int curAttr = 0;
            var attr = curCommand.attrList[curAttr++] as Script.IntAttr;
            int dir = attr.value;   // 上下左右の移動はそのまま代入

            bool runHit2Script = false;
            if (curCommand.attrList.Count == 7 &&
                curCommand.attrList[curAttr++].GetInt() == 1)
                runHit2Script = true;

            int walkCount = 1;
            bool fixDir = false;
            bool abortOnFail = true;
            bool through = false;
            bool ignoreHeight = false;

            int secondDir = -1; // 追跡、逃走の際に目的地に移動できなかった際に使用

            var limitRight = curCommand.attrList[curAttr++].GetInt() + 0.5f;
            var limitLeft = curCommand.attrList[curAttr++].GetInt() + 0.5f;
            var limitUp = curCommand.attrList[curAttr++].GetInt() + 0.5f;
            var limitDown = curCommand.attrList[curAttr++].GetInt() + 0.5f;
            var motion = curCommand.attrList[curAttr++].GetString();
            // 移動完了までウェイト
            waiter = () =>
            {
                if (chr.IsMoving() || (isCameraRotate && owner.IsCameraScroll))
                {
                    return true;
                }

                if (walkCount > 0)
                {
                    if (attr.value > (int)Command.MoveType.RIGHT)
                    {
                        // 特殊な方向指定に対応
                        switch ((Command.MoveType)attr.value)
                        {
                            case Command.MoveType.RANDOM:
                                dir = owner.GetRandom(4, 0);
                                break;
                            case Command.MoveType.FOLLOW:
                                {
                                    var tgt = owner.GetHero();
                                    if (tgt == chr) tgt = mapChr;
                                    dir = calcDir(tgt, chr);
                                    secondDir = calcSecondDir(dir, tgt, chr);
                                }
                                break;
                            case Command.MoveType.ESCAPE:
                                {
                                    var tgt = owner.GetHero();
                                    if (tgt == chr) tgt = mapChr;
                                    dir = calcDir(tgt, chr, true);
                                    secondDir = calcSecondDir(dir, tgt, chr);
                                }
                                break;
                        }
                    }


                    // 移動範囲制限の処理
                    var characterPosX = chr.x;
                    var characterPosZ = chr.z;
                    // 移動向きに合わせて移動先の位置を求める
                    switch (dir)
                    {
                        // 上
                        case Util.DIR_SER_UP:
                            if (characterPosZ - 1.0f < limitUp)
                            {
                                return false;
                            }
                            break;
                        // 下

                        case Util.DIR_SER_DOWN:
                            if (characterPosZ + 1.0f > limitDown)

                            {
                                return false;
                            }
                            break;
                        // 左
                        case Util.DIR_SER_LEFT:
                            if (characterPosX - 1.0f < limitLeft)
                            {
                                return false;
                            }
                            break;
                        // 右
                        case Util.DIR_SER_RIGHT:
                            if (characterPosX + 1.0f > limitRight)
                            {
                                return false;
                            }
                            break;
                    }

                    if (!fixDir && isCameraRotate)
                    {
                        owner.EventScrollCameraY(GetCameraRotateAngleY(chr.getDirection(), dir));
                    }


                    bool nowCollidableState = chr.collidable;
                    if (through)
                        chr.collidable = false;

                    bool nowFixDirState = chr.fixDirection;
                    if (fixDir)
                        chr.fixDirection = true;

                    bool result = owner.mapEngine.MoveCharacter(chr, dir, owner.mapDrawer, runHit2Script, ignoreHeight);
                    // 二番目に近いであろう位置に移動する
                    if (!result && secondDir != -1)
                    {
                        result = owner.mapEngine.MoveCharacter(chr, dir, owner.mapDrawer, runHit2Script, ignoreHeight);
                    }
                    if (result)
                    {
                        //chr.ChangeSpeed(-3);
                        if (motion == "run")
                        {
                            chr.playMotion("run", 0.2f, true);
                        }
                        else
                        {
                            chr.playMotion("walk", 0.2f, true);
                        }
                        walkCount--;
                    }
                    else
                    {
                        if (abortOnFail)
                        {
                            if (motion != "run")
                            {
                                chr.playMotion("wait");
                            }
                            walkCount = 0;
                        }
                    }

                    chr.collidable = nowCollidableState;
                    chr.fixDirection = nowFixDirState;
                    return true;
                }
                
                if (script.commands.Count <= cur)
                {
                }
                else
                {
                    if (script.commands[cur].type == Command.FuncType.WAIT)
                    {
                        if (motion != "run")
                        {
                            chr.playMotion("wait");
                        }
                    }
                }
                return false;
            };
        }

        private int GetCameraRotateAngleY(int originalDir, int targetDir)
        {
            int cameraScrollDegreeY = 0;
            int[] dirDegreeTable = { 0, 180, 90, 270 };

            int degreeCW = (dirDegreeTable[targetDir] - dirDegreeTable[originalDir]);
            int degreeCCW = (degreeCW > 0) ? degreeCW - 360 : 360 - Math.Abs(degreeCW);

            cameraScrollDegreeY = (Math.Abs(degreeCW) < Math.Abs(degreeCCW) ? degreeCW : degreeCCW);

            return cameraScrollDegreeY;
        }

        private void ChangeHeroName()
        {
            if (curCommand.attrList.Count < 7)
            {
                return;
            }
            var count = 0;
            // 変更する主人公
            var hero = curCommand.attrList[count++] as Common.Rom.Script.GuidAttr;
            var heroName = owner.owner.data.party.getHeroName(hero.value);
            var nextHeroName = heroName;
            // 入力できる長さ
            var stringLength = curCommand.attrList[count++].GetInt();
            // 表示位置
            var drawHeight = curCommand.attrList[count++].GetInt();
            // 入力文字
            var inputStrings = new string[4];
            inputStrings[0] = curCommand.attrList[count++].GetString();
            inputStrings[1] = curCommand.attrList[count++].GetString();
            inputStrings[2] = curCommand.attrList[count++].GetString();
            inputStrings[3] = curCommand.attrList[count++].GetString();
            owner.SetInputData(inputStrings, stringLength, heroName);
            owner.LockControl();
            owner.ShowInputWindow(true, drawHeight);
            waiter = () =>
            {
                bool complete = false;
                if (owner.haveFinshedInputing())
                {
                    nextHeroName = owner.GetInputString();
                    owner.setHeroName(hero.value, nextHeroName);
                    owner.ShowInputWindow(false, drawHeight);
                    owner.UnlockControl();
                    complete = true;
                }
                if (!complete) return true;
                return false;
            };
            return;
        }

        private void ChangeStringVariable()
        {
            if (curCommand.attrList.Count < 7)
            {
                return;
            }
            var count = 0;
            // 変更する文字列のインデックス
            var index = curCommand.attrList[count++].GetInt();
            // 文字列の長さ
            var stringLength = curCommand.attrList[count++].GetInt();
            // 表示位置
            var drawHeight = curCommand.attrList[count++].GetInt();
            // 入力文字
            var inputStrings = new string[4];
            inputStrings[0] = curCommand.attrList[count++].GetString();
            inputStrings[1] = curCommand.attrList[count++].GetString();
            inputStrings[2] = curCommand.attrList[count++].GetString();
            inputStrings[3] = curCommand.attrList[count++].GetString();
            var input = "";
            owner.SetInputData(inputStrings, stringLength, "");
            owner.LockControl();
            owner.ShowInputWindow(true, drawHeight);
            waiter = () =>
            {
                bool complete = false;
                if (owner.haveFinshedInputing())
                {
                    input = owner.GetInputString();
                    if (input != "")
                    {
                        owner.owner.data.system.StrVariables[index] = input;
                    }
                    owner.ShowInputWindow(false, drawHeight);
                    owner.UnlockControl();
                    complete = true;
                }
                if (!complete) return true;
                return false;
            };
            return;
        }

        private void ChangeGameOverAction()
        {
            if (curCommand.attrList.Count < 8)
            {
                return;
            }
            var index = 0;
            var gameOverSetting = owner.owner.data.start.gameOverSettings;
            // ゲームオーバー時挙動
            gameOverSetting.gameOverType = (Common.GameData.GameOverSettings.GameOverType)curCommand.attrList[index++].GetInt();
            // マップ
            gameOverSetting.mapGuid = curCommand.attrList[index++].GetGuid();
            // x
            gameOverSetting.x = curCommand.attrList[index++].GetInt();
            // y
            gameOverSetting.y = curCommand.attrList[index++].GetInt();
            // 復活時の種類(0が先頭1が全員)
            gameOverSetting.rivivalType = (Common.GameData.GameOverSettings.RivivalType)curCommand.attrList[index++].GetInt();
            // 復活時のHp
            gameOverSetting.rivivalHp = curCommand.attrList[index++].GetInt();
            // 復活時のMp
            gameOverSetting.rivivalMp = curCommand.attrList[index++].GetInt();
            // 共通イベントのGuid
            gameOverSetting.eventGuid = curCommand.attrList[index++].GetGuid();
        }
    }
}

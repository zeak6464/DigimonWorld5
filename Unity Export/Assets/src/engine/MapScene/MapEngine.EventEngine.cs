
using System;
using System.Collections.Generic;
using Yukar.Common;
namespace Yukar.Engine
{
    partial class MapEngine
    {
        internal bool checkAndRunCollisionScript(bool addPos)
        {
            MapCharacter tgt = null;
            List<MapCharacter> list = findEventCharacter(-1);
            if (list.Count > 0)
            {
                foreach (var chr in list)
                {
                    if (!checkHeightDiff(owner.hero, chr)) continue;

                    if (!chr.collidable || chr.expand)
                    {
                        tgt = chr;
                        break;
                    }
                }
            }

            // 高さが合うイベントが無かったら、先頭のものを適当に選ぶ
            if (list.Count > 0 && tgt == null)
                tgt = list[0];

            return runEvent(tgt);
        }

        private bool checkAndRunCollisionScriptFromEvent(MapCharacter self, int selfDir)
        {
            bool found = false;
            List<MapCharacter> list = findEventCharacter(selfDir);
            if (list.Count > 0)
            {
                foreach (var chr in list)
                {
                    if (chr == self)
                    {
                        found = true;
                        break;
                    }
                }
            }

            if (found && self.rom != null)
            {
                return runEvent(self);
            }

            return false;
        }

        private bool checkAndRunTalkableScript()
        {
            bool result = false;
            List<MapCharacter> list;

            list = findEventCharacter(-1);//直下
            list.AddRange(findEventCharacter(owner.hero.getDirection()));

            var talkableRunnerDic = new Dictionary<MapCharacter, List<ScriptRunner>>();

            if (list.Count > 0)
            {
                foreach (var runner in owner.runnerDic.getList())
                {
                    if (list.Contains(runner.mapChr) && runner.Trigger == Common.Rom.Script.Trigger.TALK)
                    {
                        // 高さを比較する
                        if (!runner.script.ignoreHeight && !checkHeightDiff(owner.GetHero(), runner.mapChr))
                            continue;

                        // 動作開始候補に加える
                        if (!talkableRunnerDic.ContainsKey(runner.mapChr))
                            talkableRunnerDic.Add(runner.mapChr, new List<ScriptRunner>());
                        talkableRunnerDic[runner.mapChr].Add(runner);
                    }
                }
            }

            foreach (var tgt in list)
            {
                if (talkableRunnerDic.ContainsKey(tgt))
                {
                    foreach (var runner in talkableRunnerDic[tgt])
                    {
                        runner.Run();
                        result = true;
                    }
                    break;
                }
            }

            return result;
        }

        List<MapCharacter> findEventCharacter(int direction)
        {
            var s = owner;

            int nx = (int)s.hero.x;
            int nz = (int)s.hero.z;

            switch (direction)
            {
                case Util.DIR_SER_UP: nz--; break;
                case Util.DIR_SER_DOWN: nz++; break;
                case Util.DIR_SER_LEFT: nx--; break;
                case Util.DIR_SER_RIGHT: nx++; break;
                default: break;
            }

            var result = new List<MapCharacter>();

            if (nx < 0 || owner.map.Width <= nx || nz < 0 || owner.map.Height <= nz || !s.hero.collidable)
                return result;

            foreach (var info in eventHeightMap.get(nx, nz))
            {
                if (info.chr == null)
                    continue;

                if (info.chr.rom != null)
                    result.Add(info.chr);
            }
            return result;
        }

        private bool checkHeightDiff(MapCharacter a, MapCharacter b)
        {
            return Math.Abs(a.y - b.y) < 0.95f;
        }

        private bool runEvent(MapCharacter tgt)
        {
            var s = owner;

            foreach (var runner in s.runnerDic.getList())
            {
                // 高さを比較する
                if (!runner.script.ignoreHeight && !checkHeightDiff(owner.GetHero(), runner.mapChr))
                    continue;

                // 動作開始
                if (runner.mapChr == tgt &&
                    (runner.Trigger == Common.Rom.Script.Trigger.HIT ||
                    runner.Trigger == Common.Rom.Script.Trigger.HIT_FROM_EV))
                {
                    runner.Run();
                    if (runner.Update())// とりあえず1フレーム動かしてしまう
                    {
                        // 完了していたら他のイベントのチェックもいまやる
                        checkAllEvent();
                        if (owner.playerLocked > 0)
                            return true;    // 結果他のイベントが起動したら wait 状態にする

                        return false;   // 何も動いてなかったら何もしなかった事にする
                    }
                    return true;
                }
            }

            return false;
        }
    }
}
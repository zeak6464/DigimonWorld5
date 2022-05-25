using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Yukar.Common.GameData;

namespace Yukar.Engine
{
    // パーティがついてくる処理とかをここに書く

    partial class MapEngine
    {
        private const int CLEAR_LOG_MARGIN = 2;
        public const int MAX_FOLLOWERS = 64;
        private const int MAX_POSLOG = 1024;

        internal class Follower
        {
            internal Followers.Entry entry = new Followers.Entry();
            internal MapCharacter mapChr;
            internal int prevIndex;

            public Follower(MapScene s, int index)
            {
                if (s.owner.data.party.members.Count > index)
                {
                    mapChr = new MapCharacter();
                    var grp = s.owner.catalog.getItemFromGuid(
                        s.owner.data.party.getMemberGraphic(index)) as Common.Resource.ResourceItem;
                    mapChr.ChangeGraphic(grp, s.mapDrawer);
                    mapChr.mapHeroSymbol = true;
                    s.mapCharList.Add(mapChr);
                }

                entry.partyIndex = index;
                entry.type = Followers.Entry.FollowerType.PARTY_MEMBER;
            }

            public Follower(MapScene s, Guid guid)
            {
                mapChr = new MapCharacter();
                var grp = s.owner.catalog.getItemFromGuid(guid) as Common.Resource.ResourceItem;
                if (grp != null)
                    mapChr.ChangeGraphic(grp, s.mapDrawer);

                entry.graphic = grp.guId;
                entry.type = Followers.Entry.FollowerType.GRAPHIC;

                s.mapCharList.Add(mapChr);
            }

            public Follower(MapScene s, Followers.Entry entry)
            {
                this.entry = entry;
                recreate(s);
            }

            internal void setPosition(int x, int y)
            {
                if(mapChr != null)
                    mapChr.setPosition(x, y);
            }

            internal bool recreate(MapScene s)
            {
                Vector3 curDir = new Vector3(0, 0, 1);

                if (s.mapCharList.Contains(mapChr))
                {
                    curDir = mapChr.getCurDir();
                    mapChr.Reset();
                    s.mapCharList.Remove(mapChr);
                    mapChr = null;
                }

                if (entry.graphic != Guid.Empty)
                {
                    mapChr = new MapCharacter();
                    var grp = s.owner.catalog.getItemFromGuid(entry.graphic) as Common.Resource.ResourceItem;
                    if (grp != null)
                        mapChr.ChangeGraphic(grp, s.mapDrawer);
                    s.mapCharList.Add(mapChr);
                    mapChr.setCurDir(curDir);
                    mapChr.setTgtDir(curDir);
                    return true;
                }
                else if(entry.partyIndex >= 0 &&
                    s.owner.data.party.members.Count > entry.partyIndex)
                {
                    mapChr = new MapCharacter();
                    var grp = s.owner.catalog.getItemFromGuid(
                        s.owner.data.party.getMemberGraphic(entry.partyIndex)) as Common.Resource.ResourceItem;
                    mapChr.ChangeGraphic(grp, s.mapDrawer);
                    mapChr.mapHeroSymbol = true;
                    s.mapCharList.Add(mapChr);
                    mapChr.setCurDir(curDir);
                    mapChr.setTgtDir(curDir);
                    return true;
                }

                return false;   // 作成できなかった or 不要
            }

            internal void finalize(MapScene s)
            {
                // 明示的にファイナライズをしたい時に呼ぶ。
                // 解放が必要なのは mapChr だけで、普段はMapSceneが全て処理してくれるので、この処理が必要なのはそれ以外の時だけ。
                if (s.mapCharList.Contains(mapChr))
                {
                    mapChr.Reset();
                    s.mapCharList.Remove(mapChr);
                }
            }

            internal void setVisibility(bool visibility)
            {
                if (mapChr != null)
                {
                    if(visibility)
                        mapChr.hide &= ~MapCharacter.HideCauses.BY_FOLLOWERS;
                    else
                        mapChr.hide |= MapCharacter.HideCauses.BY_FOLLOWERS;
                }
            }

            internal void updateMotion(MapCharacter hero)
            {
                if (mapChr == null)
                    return;
                
                if (hero.nowMotion != null)
                    mapChr.playMotion(hero.nowMotion);
                mapChr.mMoveStep = hero.mMoveStep;
            }
        }
        internal List<Follower> followers = new List<Follower>();
        internal List<Follower> followersSortingBuffer = new List<Follower>();  // ソート中だけ使うリスト
        internal struct PosLog
        {
            internal Vector3 dirVec;
            internal float dirRad;
            internal float x;
            internal float z;
            internal float diff;
        }
        private List<PosLog> posLogList = new List<PosLog>();

        public bool visibility {
            get { return owner.owner.data.start.followers.visible; }
            set { owner.owner.data.start.followers.visible = value; }
        }

        private void createFollowers(int x, int y)
        {
            //posLogList.Clear();
            var data = owner.owner.data.start.followers;

            if (data.list.Count == 0)
            {
                // 初回はパーティメンバーで生成する
                for (int i = 0; i < Party.MAX_PARTY; i++)
                {
                    var follower = new Follower(owner, i);
                    follower.setPosition(x, y);
                    followers.Add(follower);

                    data.list.Add(follower.entry);
                }
            }
            else if(followers.Count == 0)
            {
                // GameDataにあってこちらにない場合はセーブデータからの復帰なので再現する
                foreach(var entry in data.list)
                {
                    var follower = new Follower(owner, entry);
                    follower.setPosition(x, y);
                    followers.Add(follower);
                }
            }
            else
            {
                // 初回以降は元の配列を活かす
                foreach (var follower in followers.ToArray())
                {
                    follower.recreate(owner);
                }
            }

            // 表示状態を再現する
            setFollowersVisible(data.visible, false);
            updateFollowers();
        }
        
        private void moveFollowersForFreeMove()
        {
            var s = owner;

            // 現在の主人公の座標を記録する
            if(posLogList.Count == 0 ||
                (s.hero.x + s.hero.offsetX) != posLogList[0].x ||
                (s.hero.z + s.hero.offsetZ) != posLogList[0].z)
            {
                int div = 1;
                float diff = 0, diffX = 0, diffZ = 0;
                if (posLogList.Count > 0)
                {
                    diffX = s.hero.x + s.hero.offsetX - posLogList[0].x;
                    diffZ = s.hero.z + s.hero.offsetZ - posLogList[0].z;
                    diff = (float)Math.Sqrt(Math.Pow(Math.Abs(diffX), 2) + Math.Pow(Math.Abs(diffZ), 2));

                    // 差分が大きい場合は微分して登録する
                    div = Math.Max(1, (int)(diff * 200));
                    diffX /= div;
                    diffZ /= div;
                    diff /= div;
                }
                for (int i = div - 1; i >= 0; i--)
                {
                    posLogList.Insert(0, new PosLog()
                    {
                        dirVec = s.hero.getCurDir(),
                        dirRad = s.hero.getDirectionRadian(),
                        x = s.hero.x + s.hero.offsetX - diffX * i,
                        z = s.hero.z + s.hero.offsetZ - diffZ * i,
                        diff = diff,
                    });
                }
            }

            // 隊列の位置を更新する
            int count = 0;
            foreach (var cur in followers)
            {
                if (cur.mapChr == null)
                    continue;

                var nextIndex = getIndexByTotalStep(visibility ? count++ : 0,
                    cur.prevIndex, cur.mapChr.mMoveStep);
                var nextPos = posLogList[nextIndex];
                var dx = nextPos.x - (cur.mapChr.x + cur.mapChr.offsetX);
                var dy = nextPos.z - (cur.mapChr.z + cur.mapChr.offsetZ);
                if (dx == 0 && dy == 0)
                    continue;

                cur.mapChr.Walk(dx, dy, false, s.mapDrawer, eventHeightMap, true, false);
                cur.mapChr.setDirectionFromRadian(nextPos.dirRad, true, true);
                cur.mapChr.setCurDir(nextPos.dirVec);
                cur.mapChr.setTgtDir(nextPos.dirVec);
                cur.mapChr.updatePosAngle(owner.yangle, 0);

                cur.prevIndex = nextIndex;
            }

            // もう使わなそうなやつは消す
            while (posLogList.Count > followers[followers.Count - 1].prevIndex + CLEAR_LOG_MARGIN &&
                posLogList.Count > MAX_POSLOG)
                posLogList.RemoveAt(posLogList.Count - 1);
        }

        private int getIndexByTotalStep(float step, int prev, float moveStep)
        {
            if (prev >= posLogList.Count)
                return posLogList.Count - 1;

            float total = 0;
            int index = posLogList.Count - 1;
            for(int i = 0; i < posLogList.Count; i++)
            {
                total += posLogList[i].diff;
                if (total >= step)
                {
                    index = i;
                    break;
                }
            }
            if (index == prev)
                return index;
            int dir = 1;
            if (index < prev)
                dir = -1;
            int result = prev;
            total = 0;
            while(total < moveStep)
            {
                if (index == result)
                    break;

                result += dir;
                total += posLogList[result].diff;
            }
            return result;
        }

        internal void updateFollowers()
        {
            var s = owner;
            foreach (var follower in followers)
            {
                follower.updateMotion(s.hero);
            }

            // 一人称視点の時は非表示にする
            setFollowersVisible(!isViewMode() && visibility, false,
                (s.hero.hide & MapCharacter.HideCauses.BY_EVENT) != MapCharacter.HideCauses.NONE);

            // 自由移動ではない場合でも座標だけは記録する
            moveFollowersForFreeMove();

            // 先頭の向き、高さはs.heroのものをコピーする
            var front = followers.Find(x => x.mapChr != null);
            if (front != null)
            {
                front.mapChr.setDirectionFromRadian(s.hero.getDirectionRadian());

                var dir = s.hero.getCurDir();
                front.mapChr.setCurDir(dir);
                front.mapChr.setTgtDir(dir);

                front.mapChr.y = s.hero.y;
                front.mapChr.offsetY = s.hero.offsetY;
            }
        }

        private bool isViewMode()
        {
            return owner.CurrentCameraMode == Common.Rom.Map.CameraControlMode.VIEW;
        }
        
        internal void setFollowersVisible(bool flag, bool setToCurrent = true, bool forceHide = false)
        {
            if (flag)
                owner.hero.hide |= MapCharacter.HideCauses.BY_FOLLOWERS;
            else
                owner.hero.hide &= ~MapCharacter.HideCauses.BY_FOLLOWERS;
            var prevPos = new Vector3();
            for (int i = 0; i < followers.Count; i++)
            {
                // 場所移動などで、前のキャラとの距離が極端に近い時は、後ろのキャラを非表示にする
                if (followers[i].mapChr != null &&
                    (prevPos - followers[i].mapChr.getPosition()).Length() < 0.1f)
                {
                    followers[i].setVisibility(false);
                    continue;
                }

                followers[i].setVisibility((flag || followers[i].prevIndex > 0) && !forceHide);

                if (followers[i].mapChr != null)
                {
                    prevPos = followers[i].mapChr.getPosition();
                }
            }
            if(setToCurrent)
                visibility = flag;
        }

        // 隊列の操作開始
        internal void startFollowerSort()
        {
            followersSortingBuffer.AddRange(followers);
            followers.Clear();
        }

        // 隊列に新しいエントリをpush
        internal void pushFollower(int partyIndex)
        {
            // すでにそれがあるかどうか探す
            Follower pushedChr = null;
            foreach(var chr in followersSortingBuffer)
            {
                if(chr.entry.type == Followers.Entry.FollowerType.PARTY_MEMBER && chr.entry.partyIndex == partyIndex)
                {
                    pushedChr = chr;
                    break;
                }
            }
            if(pushedChr != null)
            {
                followers.Add(pushedChr);
                followersSortingBuffer.Remove(pushedChr);
                return;
            }
                
            // なかったら作って追加
            var follower = new Follower(owner, partyIndex);
            followers.Add(follower);
        }

        // 隊列に新しいエントリをpush
        internal void pushFollower(Guid guid)
        {
            // すでにそれがあるかどうか探す
            Follower pushedChr = null;
            foreach (var chr in followersSortingBuffer)
            {
                if (chr.entry.type == Followers.Entry.FollowerType.GRAPHIC && chr.mapChr.getGraphic().guId == guid)
                {
                    pushedChr = chr;
                    break;
                }
            }
            if (pushedChr != null)
            {
                followers.Add(pushedChr);
                followersSortingBuffer.Remove(pushedChr);
                return;
            }

            // なかったら作って追加
            var follower = new Follower(owner, guid);
            followers.Add(follower);
        }

        // 隊列の操作終了
        internal void endFollowerSort()
        {
            // 使われなくなったマップキャラを開放する
            foreach(var unused in followersSortingBuffer)
            {
                unused.finalize(owner);
            }
            followersSortingBuffer.Clear();

            // セーブデータ用バッファの並びも更新する
            var data = owner.owner.data.start.followers.list;
            data.Clear();
            foreach (var follower in followers)
            {
                data.Add(follower.entry);
            }
        }

        // 座標ログのクリア
        internal void clearPosLogList()
        {
            posLogList.Clear();
        }
    }
}
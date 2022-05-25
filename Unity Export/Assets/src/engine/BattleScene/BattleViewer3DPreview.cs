using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yukar.Engine
{
    public class BattleViewer3DPreview : SharpKmyGfx.Drawable
    {
        internal BattleActor[] friends = new BattleActor[4];
        internal BattleActor[] enemies = new BattleActor[6];
        public delegate void GetUpdateInfo(out MapData drawer, out float yangle);
        private GetUpdateInfo getUpdateInfo;
        private BattleCameraController camera;
        public bool hideMonsters;

        public BattleViewer3DPreview(GetUpdateInfo infoGetter, Common.Catalog catalog)
        {
            var gs = catalog.getGameSettings();
            getUpdateInfo = infoGetter;

            // 味方キャラを読み込む
            int count = -1;
            int max = gs.party.Count(x => x != Guid.Empty);
            foreach (var chr in gs.party)
            {
                if (chr == Guid.Empty)
                    continue;
                var chrRom = catalog.getItemFromGuid(chr) as Common.Rom.Hero;
                if (chrRom == null)
                    continue;
                count++;
                friends[count] = BattleActor.GenerateFriend(catalog, chrRom, count, max);
            }

            // 敵キャラを読み込む
            count = 0;
            var monsters = catalog.getFilteredItemList(typeof(Common.Rom.Monster));
            if (monsters.Count > 0)
            {
                var rand = new Random();
                max = rand.Next(1, 6);
                for (; count < max; )
                {
                    var chr = monsters[rand.Next(monsters.Count)] as Common.Rom.Monster;
                    var grp = catalog.getItemFromGuid(chr.graphic) as Common.Resource.ResourceItem;
                    enemies[count] = BattleActor.GenerateEnemy(catalog, grp, count, max);
                    count++;
                }
            }

            camera = new BattleCameraController();
        }

        public void finalize()
        {
            // キャラを破棄
            for (int i = 0; i < friends.Length; i++)
            {
                if (friends[i] != null)
                {
                    friends[i].Release();
                    friends[i] = null;
                }
            }
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null)
                {
                    enemies[i].Release();
                    enemies[i] = null;
                }
            }
        }

        private void Update(MapData drawer, float yangle)
        {
            foreach (var mapChr in friends)
            {
                if (mapChr == null)
                    continue;

                mapChr.Update(drawer, yangle, false);
            }
            foreach (var mapChr in enemies)
            {
                if (mapChr == null)
                    continue;

                mapChr.Update(drawer, yangle, false);
            }

            camera.update();
        }

        public void setTweenData(Common.Rom.ThirdPersonCameraSettings start, Common.Rom.ThirdPersonCameraSettings end,
            float tweenTime, Common.Rom.GameSettings.BattleCamera.TweenType tweenType)
        {
            camera.set(start, 0);
            camera.push(end, tweenTime, tweenType);
        }

        public override void draw(SharpKmyGfx.Render scn)
        {
            MapData drawer;
            float yangle;
            getUpdateInfo(out drawer, out yangle);
            Update(drawer, yangle);

            foreach (var mapChr in friends)
            {
                if (mapChr == null)
                    continue;

                mapChr.Draw(scn, false);
            }

            if (!hideMonsters)
            {
                foreach (var mapChr in enemies)
                {
                    if (mapChr == null)
                        continue;

                    mapChr.Draw(scn, false);
                }
            }
        }

        public Common.Rom.ThirdPersonCameraSettings getCurrentAngle()
        {
            return camera.Now;
        }

        public SharpKmyMath.Vector3 getFriendPos(int p)
        {
            if (friends[p] == null)
                return new SharpKmyMath.Vector3();
            return friends[p].getPos();
        }
    }
}

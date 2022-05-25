using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yukar.Common.Rom
{
    public class Field : RomItem
    {
        public const int FIELD_GRID = 24;

        public Guid refMap;
        public int nextId;

        public Dictionary<int, Spot> spots = new Dictionary<int,Spot>();
        public List<Route> routes = new List<Route>();

        public class Spot
        {
            public int id;

            public string name;
            public Common.Resource.Icon.Ref icon = new Resource.Icon.Ref();
            public int x;
            public int y;
            public int switchIndex = -1;

            public Guid map;
            public int entX;
            public int entY;
        }

        public class Route
        {
            public int[] spotId = new int[2];
            public int switchIndex = -1;
        }

        public Field()
        {
        }

        public override void save(System.IO.BinaryWriter writer)
        {
            base.save(writer);

            writer.Write(refMap.ToByteArray());
            writer.Write(nextId);

            writer.Write(spots.Count);
            foreach (var entry in spots.Values)
            {
                writer.Write(entry.id);
                writer.Write(entry.name);
                entry.icon.save(writer);
                writer.Write(entry.x);
                writer.Write(entry.y);
                writer.Write(entry.switchIndex);

                writer.Write(entry.map.ToByteArray());
                writer.Write(entry.entX);
                writer.Write(entry.entY);
            }

            writer.Write(routes.Count);
            foreach (var entry in routes)
            {
                writer.Write(entry.spotId[0]);
                writer.Write(entry.spotId[1]);
                writer.Write(entry.switchIndex);
            }
        }

        public override void load(System.IO.BinaryReader reader)
        {
            base.load(reader);

            refMap = Util.readGuid(reader);
            nextId = reader.ReadInt32();

            spots.Clear();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var entry = new Spot();
                entry.id = reader.ReadInt32();
                entry.name = reader.ReadString();
                entry.icon.load(reader);
                entry.x = reader.ReadInt32();
                entry.y = reader.ReadInt32();
                entry.switchIndex = reader.ReadInt32();
                entry.map = Util.readGuid(reader);
                entry.entX = reader.ReadInt32();
                entry.entY = reader.ReadInt32();
                spots.Add(entry.id, entry);
            }

            routes.Clear();
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var entry = new Route();
                entry.spotId[0] = reader.ReadInt32();
                entry.spotId[1] = reader.ReadInt32();
                entry.switchIndex = reader.ReadInt32();
                routes.Add(entry);
            }
        }

        public int getEmptyID()
        {
            return nextId++;
        }

        public Spot findSpot(int x, int y)
        {
            foreach (var spot in spots.Values)
            {
                if (spot.x == x && spot.y == y)
                    return spot;
            }

            return null;
        }

        public Common.Rom.Field.Route findRoute(int a, int b)
        {
            foreach (var route in routes)
            {
                if (route.spotId[0] == a && route.spotId[1] == b ||
                    route.spotId[0] == b && route.spotId[1] == a)
                {
                    return route;
                }
            }

            return null;
        }

        // 参照先のスポットがなくなってしまったルートは削除
        public void consistency()
        {
            for (int i = routes.Count - 1; i >= 0; i--)
            {
                foreach (var spotId in routes[i].spotId)
                {
                    if (!spots.ContainsKey(spotId))
                    {
                        routes.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public Route checkHitRoute(int x, int y)
        {
            foreach (var route in routes)
            {
                var spotA = spots[route.spotId[0]];
                var spotB = spots[route.spotId[1]];

                int halfX = Common.Resource.Icon.ICON_WIDTH / 2;
                int halfY = Common.Resource.Icon.ICON_HEIGHT / 2;

                int minX = Math.Min(spotA.x, spotB.x) * FIELD_GRID - halfX;
                int maxX = Math.Max(spotA.x, spotB.x) * FIELD_GRID + halfX;
                int minY = Math.Min(spotA.y, spotB.y) * FIELD_GRID - halfY;
                int maxY = Math.Max(spotA.y, spotB.y) * FIELD_GRID + halfY;

                if (minX < x && x < maxX && minY < y && y < maxY)
                {
                    if (checkHitRouteImpl(x, y,
                        spotA.x * FIELD_GRID, spotA.y * FIELD_GRID,
                        spotB.x * FIELD_GRID, spotB.y * FIELD_GRID))
                        return route;
                }
            }

            return null;
        }

        // 線分との当たり判定
        private bool checkHitRouteImpl(int x, int y, int ax, int ay, int bx, int by)
        {
            // 両方のスポットからの距離をはかる
            int diffAX = ax - x;
            int diffAY = ay - y;
            double distA = Math.Sqrt(diffAX * diffAX + diffAY * diffAY);
            int diffBX = bx - x;
            int diffBY = by - y;
            double distB = Math.Sqrt(diffBX * diffBX + diffBY * diffBY);

            // 比率を求める
            double delta = distA / (distA + distB);

            // 比率に応じて中点を求める
            int diffX = bx - ax;
            int diffY = by - ay;
            int centerX = (int)(ax + diffX * delta);
            int centerY = (int)(ay + diffY * delta);

            // 中点からの距離を求める
            diffX = x - centerX;
            diffY = y - centerY;
            double dist = Math.Sqrt(diffX * diffX + diffY * diffY);

            Console.WriteLine("" + dist);

            // 距離が一定以内だったらクリック成立
            if (dist < Common.Resource.Icon.ICON_WIDTH / 2)
                return true;

            return false;
        }
    }
}

#define WINDOWS
using System;
using System.Collections.Generic;
using System.Linq;
using Yukar.Engine;

namespace Yukar.Battle
{
    /// <summary>
    /// バトルコマンドのターゲット選択処理を行うクラス
    /// A class that performs target selection processing for battle commands
    /// </summary>
    class CommandTargetSelecter
    {
        public GameMain owner;
        public BattleViewer battleViewer;

        private BattleCharacterBase[,] Table;
        private int horizontalIndex;
        private int verticalIndex;

        public BattleCharacterBase[] All { get { return Table.Cast<BattleCharacterBase>().Where(character => character != null).ToArray(); } }
        public int Count { get { return All.Count(); } }
        public BattleCharacterBase CurrentSelectCharacter { get { return GetCharacter(horizontalIndex, verticalIndex); } }

        public static Dictionary<BattleCharacterBase, BattleCharacterBase> recentSelectedTarget =
            new Dictionary<BattleCharacterBase, BattleCharacterBase>();
        private BattleCharacterBase lastChr;

        private BattleCharacterBase GetCharacter(int horizontal, int vertical)
        {
            return Table[vertical, horizontal];
        }

        private bool GetTableIndex(BattleCharacterBase inCh, ref int outX, ref int outY)
        {
            var size0 = Table.GetLength(0);
            for (int y = 0; y < size0; y++)
            {
                var size1 = Table.GetLength(1);
                for (int x = 0; x < size1; ++x)
                {
                    if (Table[x, y] == inCh)
                    {
                        outX = x;
                        outY = y;
                        return true;
                    }

                }
            }
            return false;
        }

        public CommandTargetSelecter()
        {
            Table = new BattleCharacterBase[9, 9];
        }

        void AddPlayer(BattlePlayerData player, int index)
        {
			if (index < Table.GetLength(1))
			{
                Table[0, index] = player;
            }
        }

        public void AddBattleCharacters(List<BattleCharacterBase> inBattleCharacters)
        {
            if (inBattleCharacters.Count == 0 || inBattleCharacters[0] is BattlePlayerData)
            {
                int idx = 0;
                foreach (var item in inBattleCharacters)
                {
                    AddPlayer(item as BattlePlayerData, idx++);
                }
            }
            else
            {
                var monsters = new List<BattleCharacterBase>(inBattleCharacters.Count);

                monsters.AddRange(inBattleCharacters);

                monsters.Sort(
                    delegate (BattleCharacterBase a, BattleCharacterBase b)
                    {
                        if (a.pos.X > b.pos.X)
                        {
                            return 1;
                        }
                        else if (a.pos.X < b.pos.X)
                        {
                            return -1;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                );

                var leftX = monsters[0].pos.X;
                var rightX = monsters[0].pos.X;
                var frontZ = monsters[0].pos.Z;
                var backZ = monsters[0].pos.Z;

                foreach (var item in monsters)
                {
                    if (item.pos.X < leftX)
                    {
                        leftX = item.pos.X;
                    }
                    if (item.pos.X > rightX)
                    {
                        rightX = item.pos.X;
                    }
                    if (item.pos.Z > frontZ)
                    {
                        frontZ = item.pos.Z;
                    }
                    if (item.pos.Z < backZ)
                    {
                        frontZ = item.pos.Z;
                    }
                }

                var divX = (rightX - leftX) / (Table.GetLength(0) - 1);
                var divZ = (frontZ - backZ) / (Table.GetLength(1) - 1);

                leftX -= divX / 2;
                frontZ += divZ / 2;

                foreach (var item in monsters)
                {
                    var x = (divX == 0) ? 0 : (int)Math.Floor((item.pos.X - leftX) / divX);
                    var z = (divZ == 0) ? 0 : (int)Math.Floor((frontZ - item.pos.Z) / divZ);

                    if (x < 0)
                        x = 0;
                    if (z < 0)
                        z = 0;
                    if (x > Table.GetLength(1))
                        x = Table.GetLength(1) - 1;
                    if (z > Table.GetLength(0))
                        z = Table.GetLength(0) - 1;

                    while (Table[z, x] != null)
                    {
                        x++;
                        if (Table.GetLength(1) <= x)
                        {
                            x = 0;
                            z++;
                        }

                        if (Table.GetLength(0) <= z)
                        {
                            z = 0;
                            break;
                        }
                    }

                    Table[z, x] = item;
                }
            }
        }

        void AddMonster(BattleEnemyData monster)
        {
            switch (monster.arrangmentType)
            {
                case BattleEnemyData.MonsterArrangementType.BackLeft: Table[0, 0] = monster; break;
                case BattleEnemyData.MonsterArrangementType.MiddleLeft: Table[1, 0] = monster; break;
                case BattleEnemyData.MonsterArrangementType.ForwardLeft: Table[2, 0] = monster; break;

                case BattleEnemyData.MonsterArrangementType.BackCenter: Table[0, 1] = monster; break;
                case BattleEnemyData.MonsterArrangementType.MiddleCenter: Table[1, 1] = monster; break;
                case BattleEnemyData.MonsterArrangementType.ForwardCenter: Table[2, 1] = monster; break;

                case BattleEnemyData.MonsterArrangementType.BackRight: Table[0, 2] = monster; break;
                case BattleEnemyData.MonsterArrangementType.MiddleRight: Table[1, 2] = monster; break;
                case BattleEnemyData.MonsterArrangementType.ForwardRight: Table[2, 2] = monster; break;

                case BattleEnemyData.MonsterArrangementType.Manual: 
                    Table[(int)(monster.pos.Z - BattleSequenceManagerBase.battleFieldCenter.Z) + 4,
                        (int)(monster.pos.X - BattleSequenceManagerBase.battleFieldCenter.X) + 4] = monster; break;
            }
        }

        public void Clear()
        {
            for (int i = 0; i < Table.GetLength(0); i++)
            {
                for (int j = 0; j < Table.GetLength(1); j++)
                {
                    Table[i, j] = null;
                }
            }
        }

        public bool SetSelect(BattleCharacterBase character)
        {
            for (int h = 0; h < Table.GetLength(0); h++)
            {
                for (int v = Table.GetLength(1) - 1; v >= 0; v--)
                {
                    if (GetCharacter(h, v) == character)
                    {
                        horizontalIndex = h;
                        verticalIndex = v;

                        return true;
                    }
                }
            }

            return false;
        }

        public void ResetSelect(BattleCharacterBase chr)
        {
            lastChr = chr;
            if (owner.data.system.cursorPosition == Common.GameData.SystemData.CursorPosition.KEEP &&
                recentSelectedTarget.ContainsKey(chr))
            {
                if(SetSelect(recentSelectedTarget[chr]))
                    return;
            }

            horizontalIndex = 0;
            verticalIndex = 0;

            for (int h = 0; h < Table.GetLength(0); h++)
            {
                for (int v = Table.GetLength(1) - 1; v >= 0; v--)
                {
                    if (GetCharacter(h, v) != null)
                    {
                        horizontalIndex = h;
                        verticalIndex = v;

                        return;
                    }
                }
            }
        }

        public bool InputUpdate()
        {
            bool isDecide = false;
            int nextHorizontalIndex = horizontalIndex, nextVerticalIndex = verticalIndex;
            bool cursorMoved = false;

            void MoveToMinusImpl()
            {
                if (horizontalIndex > 0)
                {
                    for (int h = nextHorizontalIndex - 1; h >= 0; h--)
                    {
                        if (GetCharacter(h, nextVerticalIndex) != null)
                        {
                            nextHorizontalIndex = h;
                            cursorMoved = true;
                            break;
                        }
                    }

                    for (int h = nextHorizontalIndex - 1; h >= 0 && !cursorMoved; h--)
                    {
                        for (int v = 0; v < Table.GetLength(1); v++)
                        {
                            if (GetCharacter(h, v) != null)
                            {
                                nextHorizontalIndex = h;
                                nextVerticalIndex = v;
                                cursorMoved = true;
                                break;
                            }
                        }
                    }
                }
            }

            void MoveToPlusImpl()
            {
                if (horizontalIndex < Table.GetLength(1))
                {
                    for (int h = nextHorizontalIndex + 1; h < Table.GetLength(1); h++)
                    {
                        if (GetCharacter(h, nextVerticalIndex) != null)
                        {
                            nextHorizontalIndex = h;
                            cursorMoved = true;
                            break;
                        }
                    }

                    for (int h = nextHorizontalIndex + 1; h < Table.GetLength(1) && !cursorMoved; h++)
                    {
                        for (int v = 0; v < Table.GetLength(1); v++)
                        {
                            if (GetCharacter(h, v) != null)
                            {
                                nextHorizontalIndex = h;
                                nextVerticalIndex = v;
                                cursorMoved = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (Input.KeyTest(Input.StateType.REPEAT, Input.KeyStates.LEFT, Input.GameState.MENU))
            {
                if(GetCharacter(horizontalIndex, verticalIndex) is BattleEnemyData)
                    MoveToMinusImpl();
                else
                    MoveToPlusImpl();
            }

            if (Input.KeyTest(Input.StateType.REPEAT, Input.KeyStates.RIGHT, Input.GameState.MENU))
            {
                if (GetCharacter(horizontalIndex, verticalIndex) is BattleEnemyData)
                    MoveToPlusImpl();
                else
                    MoveToMinusImpl();
            }

            if (Input.KeyTest(Input.StateType.REPEAT, Input.KeyStates.UP, Input.GameState.MENU))
            {
                if (verticalIndex > 0)
                {
                    for (int v = nextVerticalIndex - 1; v >= 0; v--)
                    {
                        if (GetCharacter(nextHorizontalIndex, v) != null)
                        {
                            nextVerticalIndex = v;
                            cursorMoved = true;
                            break;
                        }
                    }

                    for (int v = nextVerticalIndex - 1; v >= 0 && !cursorMoved; v--)
                    {
                        for (int h = 0; h < Table.GetLength(1); h++)
                        {
                            if (GetCharacter(h, v) != null)
                            {
                                nextHorizontalIndex = h;
                                nextVerticalIndex = v;
                                cursorMoved = true;
                                break;
                            }
                        }
                    }
                }

                // 動かなかったら左右に動くかも試してみる
                // If it doesn't move, try moving left and right
                if (!cursorMoved)
                {
                    if (GetCharacter(horizontalIndex, verticalIndex) is BattleEnemyData)
                        MoveToPlusImpl();
                    else
                        MoveToMinusImpl();
                }
            }

            if (Input.KeyTest(Input.StateType.REPEAT, Input.KeyStates.DOWN, Input.GameState.MENU))
            {
                if (verticalIndex < Table.GetLength(1))
                {
                    for (int v = nextVerticalIndex + 1; v < Table.GetLength(1); v++)
                    {
                        if (GetCharacter(nextHorizontalIndex, v) != null)
                        {
                            nextVerticalIndex = v;
                            cursorMoved = true;
                            break;
                        }
                    }

                    for (int v = nextVerticalIndex + 1; v < Table.GetLength(1) && !cursorMoved; v++)
                    {
                        for (int h = 0; h < Table.GetLength(1); h++)
                        {
                            if (GetCharacter(h, v) != null)
                            {
                                nextHorizontalIndex = h;
                                nextVerticalIndex = v;
                                cursorMoved = true;
                                break;
                            }
                        }
                    }
                }

                // 動かなかったら左右に動くかも試してみる
                // If it doesn't move, try moving left and right
                if (!cursorMoved)
                {
                    if (GetCharacter(horizontalIndex, verticalIndex) is BattleEnemyData)
                        MoveToMinusImpl();
                    else
                        MoveToPlusImpl();
                }
            }
			
#if WINDOWS
            //タッチ判定
            if (Touch.IsDown())
            {
                if (!owner.IsBattle2D)
                {
                    var bv3d = battleViewer as BattleViewer3D;
                    var mousPos = Touch.GetTouchPosition(0);
                    var near = Common.Util.UnProject2World(bv3d.ProjectionMatrix, bv3d.ViewMatrix, new SharpKmyMath.Vector3(mousPos.x, mousPos.y, -1f), Graphics.ScreenWidth, Graphics.ScreenHeight);
                    var far = Common.Util.UnProject2World(bv3d.ProjectionMatrix, bv3d.ViewMatrix, new SharpKmyMath.Vector3(mousPos.x, mousPos.y, 1f), Graphics.ScreenWidth, Graphics.ScreenHeight);
                    var direction = SharpKmyMath.Vector3.normalize(far - near);
                    var ray = new Microsoft.Xna.Framework.Ray(new Microsoft.Xna.Framework.Vector3(near.x, near.y, near.z), new Microsoft.Xna.Framework.Vector3(direction.x, direction.y, direction.z));
                    //コライダの追加
                    foreach (var ch in Table)
                    {
                        if (ch == null) continue;
                        var actors = bv3d.searchFromActors(ch);
                        var mdl = actors.mapChr.getModelInstance();
                        if (mdl == null) continue;
#if true
						//TODO?
						if (mdl.minX == 0 && mdl.minY == 0 && mdl.minZ == 0 && mdl.maxX == 0 && mdl.maxY == 0 && mdl.maxZ == 0)
                        {
                            var actorPos = actors.mapChr.getPosition();
                            // Unityに近づけるため少し小さくする
                            // Make it a little smaller to be closer to Unity
                            var modelSize = mdl.mdl.getSize() * 0.60f;
                            modelSize.x = modelSize.x < 1.0f ? 1.0f : modelSize.x;
                            modelSize.y = modelSize.y < 1.0f ? 1.0f : modelSize.y;
                            modelSize.z = modelSize.z < 1.0f ? 1.0f : modelSize.z;
                            var min = new Microsoft.Xna.Framework.Vector3(actorPos.X - modelSize.x * 0.5f, actorPos.Y, actorPos.Z - modelSize.z * 0.5f);
                            var max = new Microsoft.Xna.Framework.Vector3(actorPos.X + modelSize.x * 0.5f, actorPos.Y + modelSize.y, actorPos.Z + modelSize.z * 0.5f);
							mdl.minX = min.X;
							mdl.minY = min.Y;
							mdl.minZ = min.Z;
							mdl.maxX = max.X;
							mdl.maxY = max.Y;
							mdl.maxZ = max.Z;
                        }
#endif
                    }

                    //判定
                    foreach (var ch in Table)
                    {
                        if (ch == null) continue;
                        var actors = bv3d.searchFromActors(ch);
                        var mdl = actors.mapChr.getModelInstance();
                        var bill = actors.mapChr.getMapBillboard();
                        var actorPos = actors.mapChr.getKmyPosition();

                        // TODO 2022/11/09
                        //if (bill != null)
                        //{
                        //    //画像の判定
                        // // image judgment
                        //    if (bill.isHit(new SharpKmyMath.Vector3(mousPos.x, mousPos.y, 0), actorPos, new SharpKmyMath.Vector3(2, 2, 0)))
                        //    {
                        //        this.GetTableIndex(ch, ref nextVerticalIndex, ref nextHorizontalIndex);
                        //        if (horizontalIndex == nextHorizontalIndex && verticalIndex == nextVerticalIndex)
                        //        {
                        //            isDecide = true;
                        //        }
                        //        break;
                        //    }
                        //}
                        //else
                        {
#if false//DEBUG
							//TODO?
                            var boxViewer = new SharpKmyGfx.LinePrimitive();
                            boxViewer.addVertex(new SharpKmyMath.Vector3(mdl.boundingBox.Min.X, mdl.boundingBox.Min.Y, mdl.boundingBox.Max.Z), SharpKmyGfx.Color.Red);
                            boxViewer.addVertex(new SharpKmyMath.Vector3(mdl.boundingBox.Min.X, mdl.boundingBox.Max.Y, mdl.boundingBox.Max.Z), SharpKmyGfx.Color.Red);
                            boxViewer.addVertex(new SharpKmyMath.Vector3(mdl.boundingBox.Min.X, mdl.boundingBox.Max.Y, mdl.boundingBox.Max.Z), SharpKmyGfx.Color.Red);
                            boxViewer.addVertex(new SharpKmyMath.Vector3(mdl.boundingBox.Max.X, mdl.boundingBox.Max.Y, mdl.boundingBox.Max.Z), SharpKmyGfx.Color.Red);
                            boxViewer.addVertex(new SharpKmyMath.Vector3(mdl.boundingBox.Max.X, mdl.boundingBox.Max.Y, mdl.boundingBox.Max.Z), SharpKmyGfx.Color.Red);
                            boxViewer.addVertex(new SharpKmyMath.Vector3(mdl.boundingBox.Max.X, mdl.boundingBox.Min.Y, mdl.boundingBox.Max.Z), SharpKmyGfx.Color.Red);
                            boxViewer.addVertex(new SharpKmyMath.Vector3(mdl.boundingBox.Max.X, mdl.boundingBox.Min.Y, mdl.boundingBox.Max.Z), SharpKmyGfx.Color.Red);
                            boxViewer.addVertex(new SharpKmyMath.Vector3(mdl.boundingBox.Min.X, mdl.boundingBox.Min.Y, mdl.boundingBox.Max.Z), SharpKmyGfx.Color.Red);
                            boxViewer.addVertex(new SharpKmyMath.Vector3(mdl.boundingBox.Min.X, mdl.boundingBox.Min.Y, mdl.boundingBox.Max.Z), SharpKmyGfx.Color.Red);
                            boxViewer.addVertex(new SharpKmyMath.Vector3(mdl.boundingBox.Min.X, mdl.boundingBox.Min.Y, mdl.boundingBox.Min.Z), SharpKmyGfx.Color.Red);
                            boxViewer.addVertex(new SharpKmyMath.Vector3(mdl.boundingBox.Min.X, mdl.boundingBox.Min.Y, mdl.boundingBox.Min.Z), SharpKmyGfx.Color.Red);
                            boxViewer.addVertex(new SharpKmyMath.Vector3(mdl.boundingBox.Min.X, mdl.boundingBox.Max.Y, mdl.boundingBox.Min.Z), SharpKmyGfx.Color.Red);
                            boxViewer.addVertex(new SharpKmyMath.Vector3(mdl.boundingBox.Min.X, mdl.boundingBox.Max.Y, mdl.boundingBox.Min.Z), SharpKmyGfx.Color.Red);
                            boxViewer.addVertex(new SharpKmyMath.Vector3(mdl.boundingBox.Min.X, mdl.boundingBox.Max.Y, mdl.boundingBox.Max.Z), SharpKmyGfx.Color.Red);
                            boxViewer.addVertex(new SharpKmyMath.Vector3(mdl.boundingBox.Min.X, mdl.boundingBox.Max.Y, mdl.boundingBox.Min.Z), SharpKmyGfx.Color.Red);
                            boxViewer.addVertex(new SharpKmyMath.Vector3(mdl.boundingBox.Max.X, mdl.boundingBox.Max.Y, mdl.boundingBox.Min.Z), SharpKmyGfx.Color.Red);
                            boxViewer.addVertex(new SharpKmyMath.Vector3(mdl.boundingBox.Max.X, mdl.boundingBox.Max.Y, mdl.boundingBox.Min.Z), SharpKmyGfx.Color.Red);
                            boxViewer.addVertex(new SharpKmyMath.Vector3(mdl.boundingBox.Max.X, mdl.boundingBox.Min.Y, mdl.boundingBox.Min.Z), SharpKmyGfx.Color.Red);
                            boxViewer.addVertex(new SharpKmyMath.Vector3(mdl.boundingBox.Max.X, mdl.boundingBox.Min.Y, mdl.boundingBox.Min.Z), SharpKmyGfx.Color.Red);
                            boxViewer.addVertex(new SharpKmyMath.Vector3(mdl.boundingBox.Max.X, mdl.boundingBox.Min.Y, mdl.boundingBox.Max.Z), SharpKmyGfx.Color.Red);
                            boxViewer.addVertex(new SharpKmyMath.Vector3(mdl.boundingBox.Max.X, mdl.boundingBox.Max.Y, mdl.boundingBox.Min.Z), SharpKmyGfx.Color.Red);
                            boxViewer.addVertex(new SharpKmyMath.Vector3(mdl.boundingBox.Max.X, mdl.boundingBox.Max.Y, mdl.boundingBox.Max.Z), SharpKmyGfx.Color.Red);
                            boxViewer.setWidth(2);
                            boxViewer.setDepthFunc(SharpKmyGfx.CommonPrimitive.FUNCTYPE.ALWAYS);
                            bv3d.LinePrimitives.Add(boxViewer);
#endif
                            //3Dモデルの判定
#if true
                            //TODO?
                            Microsoft.Xna.Framework.BoundingBox bb;

                            if (mdl != null)
                            {
                                bb = new Microsoft.Xna.Framework.BoundingBox(
                                    new Microsoft.Xna.Framework.Vector3(mdl.minX, mdl.minY, mdl.minZ),
                                    new Microsoft.Xna.Framework.Vector3(mdl.maxX, mdl.maxY, mdl.maxZ)
                                    );
                            }
                            else
                            {
                                bb = new Microsoft.Xna.Framework.BoundingBox(
                                    new Microsoft.Xna.Framework.Vector3(-0.5f + actorPos.x, actorPos.y, -0.5f + actorPos.z),
                                    new Microsoft.Xna.Framework.Vector3(0.5f + actorPos.x, 1.5f + actorPos.y, 0.5f + actorPos.z)
                                    );
                            }
							if (bb.Intersects(ray) != null)
                            {
                                this.GetTableIndex(ch, ref nextVerticalIndex, ref nextHorizontalIndex);
                                if (horizontalIndex == nextHorizontalIndex && verticalIndex == nextVerticalIndex)
                                {
                                    isDecide = true;
                                }
                                break;
                            }
#endif
                        }

                    }
                }
            }

#else
							//タッチ判定
			if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                if (owner.IsBattle2D)
                {
#if false //#24327
                    var touchPos = InputCore.getTouchPos(0);
                    foreach (var ch in Table)
                    {
                        if (ch == null) continue;

                        Microsoft.Xna.Framework.Rectangle rect = Microsoft.Xna.Framework.Rectangle.Empty;
                        //敵の範囲取得
                        var monster = ch as BattleEnemyData;
                        if (monster != null)
                        {
                            var height = 150;
                            var scale = BattleViewer.GetMonsterDrawScale(monster, this.Count);
                            rect = BattleViewer.GetMonsterDrawRect(monster, scale);
                            //2体超える場合は押しやすいようにサイズ調整
                            if (2 < this.Count)
                            {
                                rect.Y += rect.Height - height;
                                rect.Y -= height / 3;
                                rect.Height = height;
                            }
                        }

                        //味方の範囲取得
                        var player = ch as BattlePlayerData;
                        if (player != null)
                        {
                            rect = CharacterFaceImageDrawer.GetDrawRect(player);
                        }

                        //判定
                        if (rect.IsEmpty == false)
                        {
                            if (rect.X <= touchPos.x && touchPos.x <= rect.X + rect.Width
                            && rect.Y <= touchPos.y && touchPos.y <= rect.Y + rect.Height)
                            {
                                this.GetTableIndex(ch, ref nextVerticalIndex, ref nextHorizontalIndex);
                                if (horizontalIndex == nextHorizontalIndex && verticalIndex == nextVerticalIndex)
                                {
                                    isDecide = true;
                                }
                                break;
                            }
                        }
                    }
#endif
                }
                else
                {
                    var mousPos = UnityEngine.Input.mousePosition;
                    UnityEngine.Ray ray = UnityEngine.Camera.main.ScreenPointToRay(mousPos);
                    var bv3d = battleViewer as BattleViewer3D;

                    //コライダの追加
                    foreach (var ch in Table)
                    {
                        if (ch == null) continue;
                        var actors = bv3d.searchFromActors(ch);
                        var mdl = actors.mapChr.getModelInstance();
                        if (mdl == null || mdl.instance == null) continue;
                        var obj = mdl.instance;
                        if (obj.GetComponent<UnityEngine.BoxCollider>() == null)
                        {
                            var coll = obj.AddComponent<UnityEngine.BoxCollider>();
                            coll.isTrigger = true;
                            var bounds = obj.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>().bounds;
                            var x = bounds.size.x * 0.005f;
                            var y = bounds.size.y * 0.005f;
                            var z = bounds.size.z * 0.005f;
                            x = x < 0.01f ? 0.01f : x;
                            y = y < 0.01f ? 0.01f : y;
                            z = z < 0.01f ? 0.01f : z;
                            coll.center = new UnityEngine.Vector3(0, bounds.size.y * 0.005f, 0);
                            coll.size = new UnityEngine.Vector3(x, y, z);
                            //coll.center = new UnityEngine.Vector3(0, 0.01f, 0);
                            //coll.size = new UnityEngine.Vector3(0.01f, 0.02f, 0.01f);
                        }
                    }

                    //判定
                    foreach (var ch in Table)
                    {
                        if (ch == null) continue;
                        var actors = bv3d.searchFromActors(ch);
                        var mdl = actors.mapChr.getModelInstance();
                        if (mdl == null || mdl.instance == null)
                        {
                            //画像の判定
                            var bill = actors.mapChr.getMapBillboard();
                            if (bill.isHit(new SharpKmyMath.Vector3(mousPos.x, mousPos.y, mousPos.z), new SharpKmyMath.Vector3(2, 2, 0)))
                            {
                                this.GetTableIndex(ch, ref nextVerticalIndex, ref nextHorizontalIndex);
                                if (horizontalIndex == nextHorizontalIndex && verticalIndex == nextVerticalIndex)
                                {
                                    isDecide = true;
                                }
                                break;
                            }
                        }
                        else
                        {
                            //3Dモデルの判定
                            var obj = mdl.instance;
                            UnityEngine.RaycastHit hit;
                            if (UnityEngine.Physics.Raycast(ray, out hit))
                            {
                                var objectHit = hit.transform;
                                if (obj.transform == objectHit)
                                {
                                    this.GetTableIndex(ch, ref nextVerticalIndex, ref nextHorizontalIndex);
                                    if (horizontalIndex == nextHorizontalIndex && verticalIndex == nextVerticalIndex)
                                    {
                                        isDecide = true;
                                    }
                                    break;
                                }
                            }
                        }

                    }
                }
            }
#endif

            // 動いたかどうか、再判定する
            // re-evaluate whether it has moved
            if (horizontalIndex != nextHorizontalIndex || verticalIndex != nextVerticalIndex)
                cursorMoved = true;

            if (cursorMoved)
            {
                horizontalIndex = nextHorizontalIndex;
                verticalIndex = nextVerticalIndex;
                Audio.PlaySound(owner.se.select);
            }

            return isDecide;
        }

        internal void saveSelect()
        {
            if (lastChr != null && CurrentSelectCharacter != null)
            {
                recentSelectedTarget[lastChr] = CurrentSelectCharacter;
                lastChr = null;
            }
        }
    }
}

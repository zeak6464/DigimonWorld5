using System;
using System.Collections.Generic;
using System.Linq;

namespace Yukar.Engine
{
    class CommandTargetSelecter
    {
        public GameMain owner;
        public BattleViewer battleViewer;

        private BattleCharacterBase[,] Table;
        private int horizontalIndex = -1;
        private int verticalIndex;

        public BattleCharacterBase[] All { get { return Table.Cast<BattleCharacterBase>().Where(character => character != null).ToArray(); } }
        public int Count { get { return All.Count(); } }
        public BattleCharacterBase CurrentSelectCharacter { get { return GetCharacter(horizontalIndex, verticalIndex); } }
        public void ClearCurrentSelectCharacter() { horizontalIndex = -1; }

        public static Dictionary<BattleCharacterBase, BattleCharacterBase> recentSelectedTarget =
            new Dictionary<BattleCharacterBase, BattleCharacterBase>();
        private BattleCharacterBase lastChr;

        private BattleCharacterBase GetCharacter(int horizontal, int vertical)
        {
            if (horizontalIndex < 0)
                return null;

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
                    if(Table[x, y] == inCh)
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

        public void AddPlayer(BattlePlayerData player)
        {
            if (owner.IsBattle2D)
            {
                switch (player.viewIndex)
                {
                    case 0: Table[0, 0] = player; break;
                    case 1: Table[0, 2] = player; break;
                    case 2: Table[2, 0] = player; break;
                    case 3: Table[2, 2] = player; break;
                }
            }
            else
            {
                switch (player.viewIndex)
                {
                    case 0: Table[0, 0] = player; break;
                    case 1: Table[0, 1] = player; break;
                    case 2: Table[0, 2] = player; break;
                    case 3: Table[0, 3] = player; break;
                }
            }
        }

        public void AddMonster(BattleEnemyData monster)
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

                case BattleEnemyData.MonsterArrangementType.Manual: Table[monster.pos.Y + 4, monster.pos.X + 4] = monster; break;
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

            if (Input.KeyTest(Input.StateType.REPEAT, Input.KeyStates.LEFT))
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

            if (Input.KeyTest(Input.StateType.REPEAT, Input.KeyStates.RIGHT))
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

            if (Input.KeyTest(Input.StateType.REPEAT, Input.KeyStates.UP))
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
            }

            if (Input.KeyTest(Input.StateType.REPEAT, Input.KeyStates.DOWN))
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
            }

#if WINDOWS
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
                        if (mdl == null || mdl.inst == null) continue;
                        var obj = mdl.inst.instance;
                        if (obj.GetComponent<UnityEngine.BoxCollider>() == null)
                        {
                            var coll = obj.AddComponent<UnityEngine.BoxCollider>();
                            coll.isTrigger = true;
                            coll.center = new UnityEngine.Vector3(0, 0.01f, 0);
                            coll.size = new UnityEngine.Vector3(0.01f, 0.02f, 0.01f);
                        }
                    }

                    //判定
                    foreach (var ch in Table)
                    {
                        if (ch == null) continue;
                        var actors = bv3d.searchFromActors(ch);
                        var mdl = actors.mapChr.getModelInstance();
                        if (mdl == null || mdl.inst == null)
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
                            var obj = mdl.inst.instance;

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

        private void updateCursorFor3D()
        {
            throw new NotImplementedException();
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

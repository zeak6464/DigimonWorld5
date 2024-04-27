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
        Common.Rom.GameSettings gameSettings;

        private List<BattleCharacterBase> targetList = new List<BattleCharacterBase>();
        private int targetIndex { get; set; }

        public int Count { get => targetList.Count; }
        public BattleCharacterBase CurrentSelectCharacter { get { return targetList.Count > targetIndex ? targetList[targetIndex] : null; } }

        public static Dictionary<BattleCharacterBase, BattleCharacterBase> recentSelectedTarget =
            new Dictionary<BattleCharacterBase, BattleCharacterBase>();
        private BattleCharacterBase lastChr;

        private bool GetTargetIndex(BattleCharacterBase inCh, ref int outTargetIndex)
        {
			for (int i = 0; i < targetList.Count; i++)
			{
                if (targetList[i] == inCh)
                {
                    outTargetIndex = i;

                    return true;
                }
            }

            return false;
        }

        public CommandTargetSelecter()
        {
            gameSettings = Common.Catalog.sInstance.getGameSettings();
        }

        public void AddBattleCharacters(List<BattleCharacterBase> inBattleCharacters)
        {
            targetList.AddRange(inBattleCharacters);
        }

        public void Clear()
        {
            targetList.Clear();

            targetIndex = 0;
        }

        public bool SetSelect(BattleCharacterBase character)
        {
			for (int i = 0; i < targetList.Count; i++)
			{
				if (targetList[i] == character)
				{
                    targetIndex = i;

                    return true;
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

            SetSelect(chr);
        }

        public bool InputUpdate()
        {
            bool isDecide = false;
            int nextTargetIndex = targetIndex;
            bool cursorMoved = false;

            void MoveToMinusImpl()
            {
                if (nextTargetIndex > 0)
                {
                    nextTargetIndex--;
                }
                else
                {
                    nextTargetIndex = targetList.Count - 1;
                }

                cursorMoved = nextTargetIndex != targetIndex;
            }

            void MoveToPlusImpl()
            {
                if (nextTargetIndex < targetList.Count - 1)
                {
                    nextTargetIndex++;
                }
                else
                {
                    nextTargetIndex = 0;
                }

                cursorMoved = nextTargetIndex != targetIndex;
            }

            if (Input.KeyTest(Input.StateType.REPEAT, Input.KeyStates.LEFT, Input.GameState.MENU))
            {
                if (gameSettings.commandTargetSelecterReverseLR)
                {
                    MoveToPlusImpl();
                }
                else
                {
                    MoveToMinusImpl();
                }
            }

            if (Input.KeyTest(Input.StateType.REPEAT, Input.KeyStates.RIGHT, Input.GameState.MENU))
            {
                if (gameSettings.commandTargetSelecterReverseLR)
                {
                    MoveToMinusImpl();
                }
                else
                {
                    MoveToPlusImpl();
                }
            }

            if (Input.KeyTest(Input.StateType.REPEAT, Input.KeyStates.UP, Input.GameState.MENU))
            {
                if (gameSettings.commandTargetSelecterReverseUD)
                {
                    MoveToPlusImpl();
                }
                else
                {
                    MoveToMinusImpl();
                }
            }

            if (Input.KeyTest(Input.StateType.REPEAT, Input.KeyStates.DOWN, Input.GameState.MENU))
            {
                if (gameSettings.commandTargetSelecterReverseUD)
                {
                    MoveToMinusImpl();
                }
                else
                {
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
                    foreach (var ch in targetList)
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
                    foreach (var ch in targetList)
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
                        //        this.GetTargetIndex(ch, ref nextTargetIndex);
                        //        if (horizontalIndex == nextTargetIndex)
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
                                this.GetTargetIndex(ch, ref nextTargetIndex);
                                if (targetIndex == nextTargetIndex)
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
                                this.GetTargetIndex(ch, ref nextTargetIndex);
                                if (horizontalIndex == nextTargetIndex)
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
                                this.GetTargetIndex(ch, ref nextTargetIndex);
                                if (horizontalIndex == nextTargetIndex)
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
                                    this.GetTargetIndex(ch, ref nextTargetIndex);
                                    if (target == nextTargetIndex)
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
            if (cursorMoved || (targetIndex != nextTargetIndex))
            {
                targetIndex = nextTargetIndex;
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

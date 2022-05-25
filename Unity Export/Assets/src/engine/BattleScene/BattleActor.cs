using System;
using System.Collections.Generic;
using System.Linq;

namespace Yukar.Engine
{
    internal class BattleActor
    {
        internal enum ActorStateType
        {
            WAIT,
            START_COMMAND_SELECT,
            COMMAND_SELECT,
            BACK_TO_WAIT,
            ATTACK,
            ATTACK_WAIT,
            ATTACK_END,
            DAMAGE,
            GUARD,
            CHARGE,
            KO,
            ESCAPE,
            ESCAPE_FAILED,
            WIN,
            SKILL,
            SKILL_END,
            ITEM,
            ITEM_END,
            APPEAR,
            DESTROY,
        }
        internal struct ActorState
        {
            internal ActorStateType type;
            internal string option;
            internal int wait;
        }

        internal MapCharacter mapChr;
        private ActorState state;
        internal static MapData map;
        internal static EventHeightMap dummyColList = new EventHeightMap();
        public BattleCharacterBase source;
        internal int frontDir = 1;
        private Queue<ActorState> stateQueue = new Queue<ActorState>();

        private Dictionary<string, string> substitudeMotionDic = new Dictionary<string, string>()
        {
            {"attack", "run"},
            {"charge", "wait"},
            {"guard", "wait"},
            {"skill", "happy"},
            {"item", "happy"},
            {"damage", "disappointed"},
            {"KO", "keepdown"},
            {"win", "jump"},
            {"escape", "run"},
            {"wait2", "wait"},
        };

        private ModifiedModelInstance weaponModel;
        private ModifiedModelInstance shieldModel;
        private Common.Rom.Item weaponSource;
        private Common.Rom.Item shieldSource;

        private float stateCount = 0;
        private const float ESCAPE_MAX_COUNT = 20;

        private string armRName = "R_itemhook";
        private string armLName = "L_itemhook";
        public static Common.GameData.Party party;
        internal Microsoft.Xna.Framework.Color? overRidedColor;

        public static BattleActor GenerateFriend(Common.Catalog catalog, Common.GameData.Hero chr, int count, int max)
        {
            var result = new BattleActor();
            var mapChr = new MapCharacter();
            var res = catalog.getItemFromGuid(chr.rom.graphic) as Common.Resource.ResourceItem;
            if (party != null)
            {
                res = catalog.getItemFromGuid(party.getMemberGraphic(chr.rom)) as Common.Resource.ResourceItem;
                party = null;
            }
            mapChr.ChangeGraphic(res, null);
            if (mapChr.isBillboard())
                mapChr.setBlend(SharpKmyGfx.BLENDTYPE.kPREMULTIPLIED);
            mapChr.mapHeroSymbol = true;
            mapChr.useOverrideColor = true;
            mapChr.ChangeColor(255, 255, 255, 255);
            result.mapChr = mapChr;
            result.resetState(true, count, max);

            createWeaponModel(ref result, catalog, chr);

            // つけるノード名を確定する
            var mdl = mapChr.getModelInstance();
            if (mdl != null)
            {
                var mtx = mdl.inst.getNodeMatrix(result.armRName);
                if (equalMatrix(mtx, SharpKmyMath.Matrix4.identity()))
                {
                    result.armRName = "hook_R_hand";
                    result.armLName = "hook_L_hand";
                }
            }

            return result;
        }

        internal static BattleActor GenerateFriend(Common.Catalog catalog, Common.Rom.Hero chrRom, int count, int max)
        {
            var chr = Common.GameData.Party.createHeroFromRom(catalog, chrRom);
            return GenerateFriend(catalog, chr, count, max);
        }

        public static void createWeaponModel(ref BattleActor result, Common.Catalog catalog, Common.GameData.Hero chr = null)
        {
            if(chr == null)
                chr = ((BattlePlayerData)result.source).player;
            var weapon = chr.equipments[0];  // 武器
            if (weapon != result.weaponSource)
            {
                if (result.weaponSource != null && result.weaponModel != null)
                {
                    Graphics.UnloadModel(result.weaponModel);
                    result.weaponModel = null;
                }

                if (weapon != null)
                {
                    var res = catalog.getItemFromGuid(weapon.model) as Common.Resource.ResourceItem;
                    if (res != null)
                        result.weaponModel = Graphics.LoadModel(res.path);
                }
            }
            result.weaponSource = weapon;

            var shield = chr.equipments[1];  // 盾
            if (shield != result.shieldSource)
            {
                if (result.shieldSource != null && result.shieldModel != null)
                {
                    Graphics.UnloadModel(result.shieldModel);
                    result.shieldModel = null;
                }

                if (shield != null)
                {
                    var res = catalog.getItemFromGuid(shield.model) as Common.Resource.ResourceItem;
                    if (res != null)
                        result.shieldModel = Graphics.LoadModel(res.path);
                }
            }
            result.shieldSource = shield;
        }

        internal static BattleActor GenerateEnemy(Common.Catalog catalog, Common.Resource.ResourceItem chr, int count, int max)
        {
            var result = new BattleActor();
            var mapChr = new MapCharacter();
            mapChr.ChangeGraphic(chr, null);
            mapChr.mapHeroSymbol = true;
            mapChr.useOverrideColor = true;
            mapChr.ChangeColor(255, 255, 255, 255);
            result.mapChr = mapChr;
            result.resetState(false, count, max);
            return result;
        }

        internal void resetState(bool isPlayer, int count, int max)
        {
            Microsoft.Xna.Framework.Vector2 pos;

            if (isPlayer)
            {
                pos = BattleCharacterPosition.getPosition(BattleViewer3D.CenterOfField,
                    BattleCharacterPosition.PosType.FRIEND, count, max);
                mapChr.setDirection(0, true);
                frontDir = 1;
            }
            else
            {
                pos = BattleCharacterPosition.getPosition(BattleViewer3D.CenterOfField,
                    BattleCharacterPosition.PosType.ENEMY, count, max);
                mapChr.setDirection(1, true);
                frontDir = -1;
            }

            // アニメ再開
            mapChr.billboardAnimRemain = 0;

            mapChr.setPosition(pos.X, pos.Y);
            mapChr.y = mapChr.getAdjustedYPosition(map);
            playMotion("wait2");
            mapChr.hide = MapCharacter.HideCauses.NONE;
            mapChr.fixHeight = false;
            stateQueue.Clear();
            state.type = ActorStateType.WAIT;
        }

        private void playMotion(string motion)
        {
            playMotion(motion, mapChr, null);

            if (weaponModel != null)
                playMotion(motion, null, weaponModel);
        }

        private void playMotion(string motion, MapCharacter chr, ModifiedModelInstance mdl)
        {
            if (chr != null)
                mdl = chr.getModelInstance();
            
            if (mdl != null)    // 3Dモデルがある時
            {
                // モーションが見つからなかったら、代替モーションを再生する
                if (!mdl.inst.containsMotion(motion) &&
                substitudeMotionDic.ContainsKey(motion))
                {
                    mdl.inst.playMotion(substitudeMotionDic[motion], 0.2f);
                    return;
                }

                // 見つかったら普通に再生する
                mdl.inst.playMotion(motion, 0.2f);
            }
            else if (chr != null && chr.isBillboard())  // 2Dの時
            {
                // モーションが見つからなかったら、代替モーションを再生する
                if (!chr.contains2dMotion(motion) &&
                substitudeMotionDic.ContainsKey(motion))
                {
                    chr.playMotion(substitudeMotionDic[motion]);
                    return;
                }

                // 見つかったら普通に再生する
                chr.playMotion(motion);
            }
        }

        internal void walk(float x, float z)
        {
            mapChr.Walk((int)(x - (int)mapChr.x), (int)(z - (int)mapChr.z), false, map, dummyColList, true, false);
        }

        internal void Release()
        {
            if (mapChr != null)
                mapChr.Reset();
            mapChr = null;

            if (weaponModel != null)
                Graphics.UnloadModel(weaponModel);
            weaponModel = null;

            if (shieldModel != null)
                Graphics.UnloadModel(shieldModel);
            shieldModel = null;
        }

        internal void Draw(SharpKmyGfx.Render scn, bool isShadowScene)
        {
#if WINDOWS
#else
            if (weaponModel != null)
            {
                weaponModel.inst.instance.SetActive(false);
            }
            if (shieldModel != null)
            {
                shieldModel.inst.instance.SetActive(false);
            }
#endif

            if (mapChr.hide > 0)
                return;

            if (isShadowScene && mapChr.isBillboard())
                return;

            if (!mapChr.draw(scn))
                return;

            var mdl = mapChr.getModelInstance();
            if (mdl != null)
            {
                if (weaponModel != null)
                {
#if WINDOWS
#else
                    weaponModel.inst.instance.SetActive(true);
#endif
                    weaponModel.update();
                    weaponModel.inst.update(GameMain.getElapsedTime());
                    //var pos = getPos();
                    var mtx = mdl.inst.getNodeMatrix(armRName);
                    weaponModel.inst.setL2W(mtx);
                    weaponModel.inst.draw(scn);
                }
                if (shieldModel != null)
                {
#if WINDOWS
#else
                    shieldModel.inst.instance.SetActive(true);
#endif
                    shieldModel.update();
                    shieldModel.inst.update(GameMain.getElapsedTime());
                    //var pos = getPos();
                    var mtx = mdl.inst.getNodeMatrix(armLName);
                    shieldModel.inst.setL2W(mtx);
                    shieldModel.inst.draw(scn);
                }
            }
        }

        private static bool equalMatrix(SharpKmyMath.Matrix4 mtx1, SharpKmyMath.Matrix4 mtx2)
        {
            return
                mtx1.m00 == mtx2.m00 &&
                mtx1.m01 == mtx2.m01 &&
                mtx1.m02 == mtx2.m02 &&
                mtx1.m03 == mtx2.m03 &&
                mtx1.m10 == mtx2.m10 &&
                mtx1.m11 == mtx2.m11 &&
                mtx1.m12 == mtx2.m12 &&
                mtx1.m13 == mtx2.m13 &&
                mtx1.m20 == mtx2.m20 &&
                mtx1.m21 == mtx2.m21 &&
                mtx1.m22 == mtx2.m22 &&
                mtx1.m23 == mtx2.m23 &&
                mtx1.m30 == mtx2.m30 &&
                mtx1.m31 == mtx2.m31 &&
                mtx1.m32 == mtx2.m32 &&
                mtx1.m33 == mtx2.m33;
        }

        internal void Update(MapData drawer, float yangle, bool isLockDirection)
        {
            mapChr.Update(drawer, yangle, isLockDirection);

            if (!mapChr.IsMoving() && stateQueue.Count > 0 && stateCount >= state.wait)
            {
                setActorState(stateQueue.Dequeue());
            }

            // マップ範囲外の時は高さに10000とかが入っているので、補正する
            if (map != null && map.mapRom != null)
            {
                if (mapChr.x < 0 || mapChr.z < 0 || mapChr.x >= map.mapRom.Width || mapChr.z >= map.mapRom.Height)
                {
                    mapChr.y = 1;
                    mapChr.offsetY = 0;
                    mapChr.fixHeight = true;
                }
                else
                {
                    mapChr.fixHeight = false;
                }
            }

            // 逃げる時は走らせる
            if (state.type == ActorStateType.ESCAPE)
            {
                mapChr.Walk(0f, mapChr.mMoveStep * frontDir * GameMain.getRelativeParam60FPS(), true, map, dummyColList, true, false);
                var stepCount = Math.Min(ESCAPE_MAX_COUNT, stateCount) / ESCAPE_MAX_COUNT;
                byte color = (byte)(255 - (stepCount * 255));
                if(mapChr.isBillboard())
                    mapChr.ChangeColor(color, color, color, color);
                else
                    mapChr.ChangeColor(0, 0, 0, (byte)(256 - color));
                if (stepCount == 1.0)
                    mapChr.hide |= MapCharacter.HideCauses.BY_BATTLE;
            }

            // ハケる
            if (state.type == ActorStateType.DESTROY)
            {
                var stepCount = Math.Min(ESCAPE_MAX_COUNT, stateCount) / ESCAPE_MAX_COUNT;
                byte color = (byte)(255 - (stepCount * 255));
                if(mapChr.isBillboard())
                    mapChr.ChangeColor(color, color, color, color);
                else
                    mapChr.ChangeColor(0, 0, 0, (byte)(256 - color));
                if (stepCount == 1.0)
                    mapChr.hide |= MapCharacter.HideCauses.BY_BATTLE;
            }

            // 登場
            if (state.type == ActorStateType.APPEAR)
            {
                var stepCount = Math.Min(ESCAPE_MAX_COUNT, stateCount) / ESCAPE_MAX_COUNT;
                if (stepCount > 1)
                    stepCount = 1;
                byte color = (byte)(255 - (stepCount * 255));
                if(mapChr.isBillboard())
                    mapChr.ChangeColor(color, color, color, color);
                else
                    mapChr.ChangeColor(0, 0, 0, (byte)(256 - color));
            }

            stateCount += GameMain.getRelativeParam60FPS();
        }

        internal void queueActorState(ActorStateType state)
        {
            stateQueue.Enqueue(new ActorState() { type = state });
        }

        internal void queueActorState(ActorStateType state, string motion, int wait)
        {
            stateQueue.Enqueue(new ActorState() { type = state, option = motion, wait = wait });
        }

        internal void setActorState(ActorState state)
        {
            if (this.state.type == state.type)
                return;
            var grpName = "NO GRAPHIC!!";
            if (this.mapChr.getGraphic() != null)
                grpName = this.mapChr.getGraphic().name;
            Console.WriteLine("Set state : " + state.ToString() + " / To : " + grpName);

            var nowState = this.state.type;
            this.state = state;
            stateCount = 0;

            switch (state.type)
            {
                case ActorStateType.WAIT:   // 待機中
                    // ガード中はのけぞらない
                    if (nowState == ActorStateType.GUARD)
                    {
                        this.state.type = nowState;
                        break;
                    }
                    else if (nowState == ActorStateType.KO && mapChr.isBillboard())
                    {
                        // アニメ再開
                        mapChr.billboardAnimRemain = 0;
                    }

                    playMotion("wait2");
                    break;

                case ActorStateType.START_COMMAND_SELECT: // コマンド選択開始
                    if (source.isMovableToForward())
                    {
                        mapChr.Walk(0, -frontDir, false, map, dummyColList, true);
                        playMotion("walk");
                    }
                    break;

                case ActorStateType.COMMAND_SELECT: // コマンド選択中
                    // 死んでたら何もしない
                    if (nowState == ActorStateType.KO)
                    {
                        this.state.type = nowState;
                        break;
                    }

                    playMotion("wait2");
                    break;

                case ActorStateType.BACK_TO_WAIT: // コマンド選択し終わった時
                    if (source.isMovableToForward())
                    {
                        mapChr.Walk(0, frontDir, false, map, dummyColList, true);
                        playMotion("walk");
                    }
                    break;

                case ActorStateType.ATTACK_WAIT: // 攻撃待ち
                    break;

                case ActorStateType.ATTACK: // 攻撃時
                    if(source.isMovableToForward())
                        mapChr.Walk(0, -frontDir, false, map, dummyColList, true);
                    if (!string.IsNullOrEmpty(state.option))
                        playMotion(state.option);
                    else
                        playMotion("attack");
                    break;

                case ActorStateType.ATTACK_END: // 攻撃終了時
                    if (source.isMovableToForward())
                        mapChr.Walk(0, frontDir, false, map, dummyColList, true);
                    playMotion("wait2");
                    break;

                case ActorStateType.SKILL: // 攻撃時
                    if (source.isMovableToForward())
                        mapChr.Walk(0, -frontDir, false, map, dummyColList, true);
                    if (!string.IsNullOrEmpty(state.option))
                        playMotion(state.option);
                    else
                        playMotion("skill");
                    break;

                case ActorStateType.SKILL_END: // 攻撃終了時
                    if (source.isMovableToForward())
                        mapChr.Walk(0, frontDir, false, map, dummyColList, true);
                    playMotion("wait2");
                    break;

                case ActorStateType.ITEM: // 攻撃時
                    if (source.isMovableToForward())
                        mapChr.Walk(0, -frontDir, false, map, dummyColList, true);
                    playMotion("item");
                    break;

                case ActorStateType.ITEM_END: // 攻撃終了時
                    if (source.isMovableToForward())
                        mapChr.Walk(0, frontDir, false, map, dummyColList, true);
                    playMotion("wait2");
                    break;

                case ActorStateType.DAMAGE: // ダメージを受けた時
                    // ガード中はのけぞらない
                    if (nowState == ActorStateType.GUARD || nowState == ActorStateType.KO)
                    {
                        this.state.type = nowState;
                        break;
                    }

                    playMotion("damage");
                    break;

                case ActorStateType.GUARD:  // ガード
                    playMotion("guard");
                    break;

                case ActorStateType.CHARGE:  // ためる
                    playMotion("charge");
                    break;

                case ActorStateType.KO: // 戦闘不能
                    playMotion("KO");
                    if (isBillboardDead())
                    {
                        // ビルボードだったらアニメを止める
                        mapChr.billboardAnimRemain = int.MaxValue;
                    }
                    break;

                case ActorStateType.ESCAPE: // 逃げる
                    // 死んでたら何もしない
                    if (nowState == ActorStateType.KO)
                    {
                        this.state.type = nowState;
                        break;
                    }

                    playMotion("escape");
                    mapChr.fixHeight = true;
                    break;

                case ActorStateType.ESCAPE_FAILED: // 逃げ失敗
                    playMotion("wait2");
                    if (source.isMovableToForward())
                        mapChr.Walk(0, frontDir, false, map, dummyColList, true);
                    //stateQueue.Clear();
                    break;

                case ActorStateType.WIN: // 勝利
                    // 死んでたら何もしない
                    if (nowState == ActorStateType.KO)
                    {
                        this.state.type = nowState;
                        break;
                    }

                    playMotion("win");
                    break;

                case ActorStateType.APPEAR: // 登場
                    // 死んでたら何もしない
                    if (nowState == ActorStateType.KO)
                    {
                        this.state.type = nowState;
                        break;
                    }

                    playMotion("walk");
                    mapChr.fixHeight = true;
                    break;
            }
        }

        internal ActorStateType getActorState()
        {
            return state.type;
        }

        internal float getActorStateCount()
        {
            return stateCount;
        }

        internal bool sourceEqual(Guid guid)
        {
            var grp = Guid.Empty;
            if (mapChr.getGraphic() != null)
                grp = mapChr.getGraphic().guId;
            return grp == guid;
        }

        internal void setColor(Microsoft.Xna.Framework.Color color)
        {
            if (state.type != ActorStateType.ESCAPE)
                mapChr.ChangeColor(color.R, color.G, color.B, color.A);
        }

        internal Microsoft.Xna.Framework.Vector2 getScreenPos(SharpKmyMath.Matrix4 pp, SharpKmyMath.Matrix4 vv, MapScene.EffectPosType posType = MapScene.EffectPosType.Body)
        {
            int x, y;
            MapScene.GetCharacterScreenPos(mapChr, out x, out y, pp, vv, posType);
            return new Microsoft.Xna.Framework.Vector2(x, y);
        }

        public float Z { get { return mapChr.z + mapChr.offsetZ; } }

        public float Height { get { return mapChr.getHeight(); } }

        public float X { get { return mapChr.x + mapChr.offsetX; } }

        internal SharpKmyMath.Vector3 getPos()
        {
            return new SharpKmyMath.Vector3(
                mapChr.x + mapChr.offsetX,
                mapChr.y + mapChr.offsetY,
                mapChr.z + mapChr.offsetZ);
        }

        internal SharpKmyMath.Vector3 getPosWithoutOffset()
        {
            return new SharpKmyMath.Vector3(
                mapChr.x,
                mapChr.y,
                mapChr.z);
        }

        internal bool isBillboardDead()
        {
            return mapChr.isBillboard() &&
                (!(mapChr.character is Common.Resource.Character) ||
                !((Common.Resource.Character)mapChr.character).subItemList.Exists(x => x.name == "KO"));
        }

        internal bool isActorStateQueued(ActorStateType actorStateType)
        {
            return stateQueue.ToList().Exists(x => x.type == actorStateType);
        }
    }
}

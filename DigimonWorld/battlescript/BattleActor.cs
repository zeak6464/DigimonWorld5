#define WINDOWS
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Yukar.Engine;
using static Yukar.Engine.BattleEnum;

namespace Yukar.Battle
{
    /// <summary>
    /// バトル中、3D空間上で敵味方のキャラクターを表示して様々な演技をさせるためのクラス
    /// A class that displays enemy and ally characters in 3D space during battle and lets them perform various actions.
    /// </summary>
    public class BattleActor
    {
        /// <summary>
        /// アクターの演技種別
        /// Actor performance type
        /// </summary>
        internal enum ActorStateType
        {
            NONE,
            WAIT,
            START_COMMAND_SELECT,
            COMMAND_SELECT,
            BACK_TO_WAIT,
            ATTACK_WAIT,
            ATTACK_WAIT_FOR_EFFEKSEER_LOADING,
            ATTACK,
            ATTACK_END,
            DAMAGE_START,
            DAMAGE,
            DODGE,
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
            CHANGE,
            CHANGE_END,
            ENTER,
            ENTER_END,
            LEAVE,
            LEAVE_END,
            APPEAR,
            APPEAR_END,
            DESTROY,
            FATAL_DAMAGE,  // とどめの一撃のときはこれが呼ばれる / This is called for the killing blow
            COMMAND_STANDBY,
            SKILL_WAIT,
        }
        /// <summary>
        /// アクターの状態定義
        /// Actor state definition
        /// </summary>
        internal struct ActorState
        {
            internal ActorStateType type;
            internal string option;
            internal int wait;
            internal Action handler;
        }

        internal MapCharacter mapChr;
        private ActorState state;
        internal static MapData map;
        public BattleCharacterBase source;
        internal int frontDir = 1;
        private Queue<ActorState> stateQueue = new Queue<ActorState>();

        private Dictionary<string, string> substitudeMotionDic = new Dictionary<string, string>()
        {
            {"command_wait", "battle_wait"},
            {"attack_wait", "battle_wait"},
            {"skill_wait", "battle_wait"},
            {"item_wait", "battle_wait"},
            {"attack", "run"},
            {"charge", "battle_wait"},
            {"guard", "battle_wait"},
            {"skill", "happy"},
            {"item", "happy"},
            {"change", "skill"},
            {"enter", "skill"},
            {"leave", "run"},
            {"damage", "disappointed"},
            {"KO", "keepdown"},
            {"win", "jump"},
            {"escape", "run"},
            {"battle_wait", "wait2"},
            {"wait2", "wait"},
            {"battle_walk", "walk"},
        };

        public static readonly string[] R_ARM_NODE_NAMES = { "R_itemhook", "hook_R_hand" };
        public static readonly string[] L_ARM_NODE_NAMES = { "L_itemhook", "hook_L_hand" };

        private float stateCount = 0;
        internal const float ESCAPE_MAX_COUNT = 20;
        public static Common.GameData.Party party;
        internal Microsoft.Xna.Framework.Color? overRidedColor;
        private const float MOVE_SPEED = 0.1f;

        public static BattleActor GenerateFriend(Common.Catalog catalog, Common.GameData.Hero chr, int count, int max)
        {
            var result = new BattleActor();
            var mapChr = new MapCharacter(null);
            mapChr.setDisplayID(Common.Util.BATTLE3DDISPLAYID);
            var res = catalog.getItemFromGuid(chr.rom.graphic) as Common.Resource.GfxResourceBase;
            if (party != null)
            {
                res = catalog.getItemFromGuid(party.getMemberGraphic(chr.rom)) as Common.Resource.GfxResourceBase;
                party = null;
            }
            mapChr.ChangeGraphic(res, null);
			if (mapChr.isBillboard())
			{
				mapChr.setBlend(SharpKmyGfx.BLENDTYPE.kPREMULTIPLIED);
				mapChr.getMapBillboard().setVisibility(false);
				mapChr.getMapBillboard().setDisplayID(Common.Util.BATTLE3DDISPLAYID);
			}
            else if(mapChr.getModelInstance() != null)
			{
				mapChr.getModelInstance().setVisibility(false);
				mapChr.getModelInstance().setDisplayID(Common.Util.BATTLE3DDISPLAYID);
			}
            //mapChr.setHeroSymbol(true);
            mapChr.useOverrideColor = true;
            mapChr.ChangeColor(255, 255, 255, 255);
            result.mapChr = mapChr;
            result.resetState(true, count, max);

            createWeaponModel(ref result, catalog, chr);

            return result;
        }

        internal static BattleActor GenerateFriend(Common.Catalog catalog, Common.Rom.Cast chrRom, int count, int max)
        {
            var chr = Common.GameData.Party.createHeroFromRom(catalog, chrRom);
            return GenerateFriend(catalog, chr, count, max);
        }

        public static void createWeaponModel(ref BattleActor result, Common.Catalog catalog, Common.GameData.Hero chr = null)
        {
            if(chr == null)
                chr = ((BattlePlayerData)result.source).player;

            var weapon = chr.equipments[0]; // 武器 / weapon
            var shield = chr.equipments[1]; // 盾 / shield

            var weaponModel = catalog.getItemFromGuid(weapon?.model ?? Guid.Empty) as Common.Resource.GfxResourceBase;
            var shieldModel = catalog.getItemFromGuid(shield?.model ?? Guid.Empty) as Common.Resource.GfxResourceBase;

            result.mapChr.clearJointInfo();
            result.mapChr.addJointInfo(Common.GameData.Party.JointTarget.LEFT_HAND, shieldModel, false);
            result.mapChr.addJointInfo(Common.GameData.Party.JointTarget.RIGHT_HAND, weaponModel, false);
        }

        internal static BattleActor GenerateEnemy(Common.Catalog catalog, Common.Resource.GfxResourceBase chr, int count, int max)
        {
            var result = new BattleActor();
            var mapChr = new MapCharacter(null);
            mapChr.ChangeGraphic(chr, null);
            //mapChr.setHeroSymbol(true);
            mapChr.useOverrideColor = true;
            mapChr.ChangeColor(255, 255, 255, 255);
            mapChr.setDisplayID(Common.Util.BATTLE3DDISPLAYID);
            if (mapChr.isBillboard())
            {
                mapChr.getMapBillboard().setDisplayID(Common.Util.BATTLE3DDISPLAYID);
            }
            else if (mapChr.getModelInstance() != null)
            {
                mapChr.getModelInstance().setDisplayID(Common.Util.BATTLE3DDISPLAYID);
            }
            result.mapChr = mapChr;
            result.resetState(false, count, max);
            return result;
        }

        internal void resetState(bool isPlayer, int count, int max)
        {
            Microsoft.Xna.Framework.Vector3 pos;


            if (isPlayer)
            {
                pos = BattleCharacterPosition.getPosition(BattleSequenceManagerBase.battleFieldCenter,
                    BattleCharacterPosition.PosType.FRIEND, count, max);
                mapChr.setDirection(0, true);
                frontDir = 1;
            }
            else
            {
                pos = BattleCharacterPosition.getPosition(BattleSequenceManagerBase.battleFieldCenter,
                    BattleCharacterPosition.PosType.ENEMY, count, max);
                mapChr.setDirection(1, true);
                frontDir = -1;
            }

            // アニメ再開
            // anime restart
            mapChr.billboardAnimRemain = 0;

            float height = 1;
            if (map?.mapRom != null)
                height = map.getAdjustedHeight(pos.X, pos.Z);
            mapChr.setPosition(pos.X, height, pos.Z);
            mapChr.hide = MapCharacter.HideCauses.NONE;
            mapChr.fixHeight = false;
            stateQueue.Clear();
            queueActorState(ActorStateType.WAIT);
            //state.type = ActorStateType.WAIT;
            //playMotion("wait");

            // 色をもとに戻す
            // restore color
            mapChr.ChangeColor(255, 255, 255, 255);
            mapChr.setOpacityMultiplier(1.0f);
        }

        private bool playMotion(string motion)
        {
            return playMotion(motion, mapChr, null);
        }

        private bool playMotion(string motion, MapCharacter chr, SharpKmyGfx.ModelInstance mdl)
        {
            if (chr != null)
                mdl = chr.getModelInstance();
            
            if (mdl != null)   // 3Dモデルがある時 / When you have a 3D model
            {
                // 見つかったら普通に再生する
                // If found, play normally
                if (mdl.containsMotion(motion))
                {
                    chr.playMotion(motion);
                    return true;
                }

                // モーションが見つからなかったら、代替モーションを再生する
                // If no motion found, play alternate motion
                while (true)
                {
                    if (mdl.containsMotion(motion))
                    {
                        chr.playMotion(motion);
                        return true;
                    }
                    else
                    {
                        if (!substitudeMotionDic.ContainsKey(motion))
                            break;
                        motion = substitudeMotionDic[motion];
                    }
                }
            }
            else if (chr != null && chr.isBillboard()) // 2Dの時 / 2D time
            {
                // 見つかったら普通に再生する
                // If found, play normally
                if (chr.contains2dMotion(motion))
                {
                    chr.playMotion(motion);
                    return true;
                }

                // モーションが見つからなかったら、代替モーションを再生する
                // If no motion found, play alternate motion
                while (true)
                {
                    if (chr.contains2dMotion(motion))
                    {
                        chr.playMotion(motion);
                        return true;
                    }
                    else
                    {
                        if (!substitudeMotionDic.ContainsKey(motion))
                            break;
                        motion = substitudeMotionDic[motion];
                    }
                }
            }

            return false;
        }

        internal void walk(float x, float z, bool useDir = true)
        {
            var moveX = mapChr.pos.X - x;
            var moveZ = mapChr.pos.Z - z;

			if ((moveX != 0) || (moveZ != 0))
			{
                if (useDir)
                {
                    MapCharacterMoveMacro.addXZTweener(mapChr,
                        ScriptRunner.calcDirImpl(moveX, moveZ), true, true, map);
                }
                else
                {
                    MapCharacterMoveMacro.addXZTweener(mapChr, x, z, map);
                }
            }
        }

        internal void Release()
        {
            if (mapChr != null)
                mapChr.Reset();
            mapChr = null;
        }

        internal void Draw(SharpKmyGfx.Render scn)
        {
#if WINDOWS
#else
            // TODO:未実装
            // TODO: not implemented
        //  if (weaponModel != null)
        //  {
        //      weaponModel.instance.SetActive(false);
        //  }
        //  if (shieldModel != null)
        //  {
        //      shieldModel.instance.SetActive(false);
        //  }
#endif

            if (mapChr.hide > 0)
                return;

            //if (isShadowScene && mapChr.isBillboard())
             //   return;

            if (!mapChr.draw(scn))
                return;
        }

        internal void Update(MapData drawer, float yangle, bool isLockDirection)
        {
            mapChr.Update(drawer, yangle, isLockDirection);

            // 物理実装以降、自動で高さを合わせてくれなくなったので、自前で生成してやる
            // After the physical implementation, the height is no longer adjusted automatically, so I will generate it myself
            if(!mapChr.fixHeight)
                mapChr.pos.Y = drawer.getAdjustedHeight(mapChr.pos.X, mapChr.pos.Z);

            if (stateQueue.Count > 0 && isReady())
            {
                setActorState(stateQueue.Dequeue());
            }

            // マップ範囲外の時は高さに10000とかが入っているので、補正する
            // When outside the map range, 10000 is included in the height, so correct it
            if (map != null && map.mapRom != null)
            {
                if (mapChr.pos.X < 0 || mapChr.pos.Z < 0 || mapChr.pos.X >= map.mapRom.Width || mapChr.pos.Z >= map.mapRom.Height)
                {
                    mapChr.pos.Y = 1;
                }
            }

            // 逃げる時は走らせる
            // run when you run away
            if (state.type == ActorStateType.ESCAPE)
            {
                if(!source.IsActionDisabled())
                    mapChr.Move(0f, MOVE_SPEED * frontDir * GameMain.getRelativeParam60FPS(), true);
                var stepCount = Math.Min(ESCAPE_MAX_COUNT, stateCount) / ESCAPE_MAX_COUNT;
                var alpha = 1.0f - stepCount;
                if (source != null)
                    source.imageAlpha = alpha;
                mapChr.setOpacityMultiplier(alpha);
				if (stepCount == 1.0)
                    mapChr.hide |= MapCharacter.HideCauses.BY_BATTLE;
            }

            // コマンド選択中
            // command selected
            if(state.type == ActorStateType.COMMAND_SELECT)
            {
                switch (((BattleSequenceManager)BattleSequenceManagerBase.Get()).battleCommandState)
                {
                    case SelectBattleCommandState.CommandSelect:
                        playMotion("command_wait");
                        break;
                    default:
                        playMotion("attack_wait");
                        break;
                }
            }

            // ハケる
            // Brush
            if (state.type == ActorStateType.DESTROY)
            {
                var stepCount = Math.Min(ESCAPE_MAX_COUNT, stateCount) / ESCAPE_MAX_COUNT;
                mapChr.setOpacityMultiplier(1.0f - stepCount);
                if (stepCount == 1.0)
                    mapChr.hide |= MapCharacter.HideCauses.BY_BATTLE;
            }

            // 登場
            // Appearance
            if (state.type == ActorStateType.APPEAR)
            {
                var stepCount = Math.Min(ESCAPE_MAX_COUNT, stateCount) / ESCAPE_MAX_COUNT;
                if (stepCount > 1)
                    stepCount = 1;
                mapChr.setOpacityMultiplier(stepCount);
            }

            // 攻撃はモーション完了次第battle_waitにする
            // Attack will be battle_wait as soon as the motion is completed
            if (state.type == ActorStateType.ATTACK &&
                mapChr.isChangeMotionAvailable() &&
                mapChr.moveMacros.Count == 0 &&
                ((mapChr.getModelInstance()?.containsMotion("wait2") ?? false) ||
                (mapChr.getModelInstance()?.containsMotion("battle_wait") ?? false)))   // 2Dキャラは元の挙動のままのほうが都合が良いので、mdlだけチェックする / It is convenient for 2D characters to keep their original behavior, so check only mdl
            {
                playWaitMotion();
            }

            stateCount += GameMain.getRelativeParam60FPS();
        }

        internal void queueActorState(ActorStateType state)
        {
            stateQueue.Enqueue(new ActorState() { type = state });
        }

        internal void queueActorState(ActorStateType state, string motion, int wait = 0, Action handler = null)
        {
            stateQueue.Enqueue(new ActorState() { type = state, option = motion, wait = wait, handler = handler });
        }

        internal void setActorState(ActorState state)
        {
            if (this.state.type == state.type && this.state.option == state.option)
                return;
            var grpName = "NO GRAPHIC!!";
            if (this.mapChr.getGraphic() != null)
                grpName = this.mapChr.getGraphic().name;
            Console.WriteLine("Set state : " + state.ToString() + " / To : " + grpName);

            var nowState = this.state.type;
            var nowOption = this.state.option;
            this.state = state;
            stateCount = 0;

            switch (state.type)
            {
                case ActorStateType.WAIT:  // 待機中 / Waiting
                    // ガード中はのけぞらない
                    // Don't lean back while guarding
                    if (nowState == ActorStateType.GUARD)
                    {
                        this.state.type = nowState;
                        this.state.option = nowOption;
                        break;
                    }
                    else if (nowState == ActorStateType.KO && mapChr.isBillboard())
                    {
                        // アニメ再開
                        // anime restart
                        mapChr.billboardAnimRemain = 0;
                    }

                    playWaitMotion();
                    break;

                case ActorStateType.START_COMMAND_SELECT:// コマンド選択開始 / Start command selection
                    // 死んでたら何もしない
                    // nothing when dead
                    if (nowState == ActorStateType.KO)
                    {
                        this.state.type = nowState;
                        this.state.option = nowOption;
                        break;
                    }

                    //if (source.isMovableToForward())
                    //{
                    //    MapCharacterMoveMacro.addSimpleXZTweener(mapChr, ScriptRunner.calcDirImpl(0, frontDir), MOVE_SPEED);
                    //    playMotion("battle_walk");
                    //}
                    break;

                case ActorStateType.COMMAND_SELECT:// コマンド選択中 / command selected
                    // 死んでたら何もしない
                    // nothing when dead
                    if (nowState == ActorStateType.KO)
                    {
                        this.state.type = nowState;
                        this.state.option = nowOption;
                        break;
                    }

                    playWaitMotion();
                    break;

                case ActorStateType.BACK_TO_WAIT:// コマンド選択し終わった時 / After selecting a command
                    // 死んでたら何もしない
                    // nothing when dead
                    if (nowState == ActorStateType.KO)
                    {
                        this.state.type = nowState;
                        this.state.option = nowOption;
                        break;
                    }

                    //if (source.isMovableToForward())
                    //{
                    //    MapCharacterMoveMacro.addSimpleXZTweener(mapChr, ScriptRunner.calcDirImpl(0, -frontDir), MOVE_SPEED);
                    //    playMotion("battle_walk");
                    //}
                    break;

                case ActorStateType.COMMAND_STANDBY:// コマンド選択し終わった～コマンド発動前 / Finished command selection ~ Before command activation
                    switch (source.selectedBattleCommandType)
                    {
                        case BattleCommandType.Critical:
                        case BattleCommandType.ForceCritical:
                        case BattleCommandType.Attack:
                            playMotion("attack_wait");
                            break;
                        case BattleCommandType.Charge:
                            playMotion("charge");
                            break;
                        case BattleCommandType.Guard:
                            playMotion("guard");
                            break;
                        case BattleCommandType.Skill:
                            var mot = source.selectedSkill?.option.standByMotion;
                            if (string.IsNullOrEmpty(mot) || !playMotion(mot))
                                playMotion("skill_wait");
                            break;
                        case BattleCommandType.Item:
                            playMotion("item_wait");
                            break;
                        default:
                            // ガード中はのけぞらない
                            // Don't lean back while guarding
                            if (nowState == ActorStateType.GUARD)
                            {
                                this.state.type = nowState;
                                this.state.option = nowOption;
                                break;
                            }

                            playWaitMotion();
                            break;
                    }
                    break;

                case ActorStateType.ATTACK_WAIT:// 攻撃待ち / waiting for attack
                    if (source.isUseWalkInAttack())
                    {
                        MapCharacterMoveMacro.addSimpleXZTweener(mapChr, ScriptRunner.calcDirImpl(0, frontDir), MOVE_SPEED, () => playMotion("wait"));
                        playMotion("battle_walk");
                    }
                    break;

                case ActorStateType.ATTACK:// 攻撃時 / when attacking
                    if (!source.isUseWalkInAttack() && source.isMovableToForward(true))
                    {
                        MapCharacterMoveMacro.addSimpleXZTweener(mapChr, ScriptRunner.calcDirImpl(0, frontDir), MOVE_SPEED);
                    }

                    if (string.IsNullOrEmpty(state.option) || !playMotion(state.option))
                        playMotion("attack");
                    mapChr.lockMotion = true;
                    break;

                case ActorStateType.ATTACK_END:// 攻撃終了時 / End of attack
                    if (source.isMovableToForward(true))
                    {
                        MapCharacterMoveMacro.addSimpleXZTweener(mapChr, ScriptRunner.calcDirImpl(0, -frontDir), MOVE_SPEED,
                            () => { if (nowState != ActorStateType.KO) playWaitMotion(); });

                        // 自分に睡眠・即死スキルなどを使って倒れている場合がある
                        // You may fall down using sleep or instant death skills on yourself.
                        if (nowState != ActorStateType.KO)
                            playMotion("battle_walk");
                    }
                    else
                    {
                        // 状態異常のモーションがあればそれを再生する
                        // If there is a status ailment motion, play it
                        playWaitMotion();
                    }
                    break;

                case ActorStateType.SKILL_END:
                case ActorStateType.ITEM_END:
                    if (source.isMovableToForward())
                    {
                        MapCharacterMoveMacro.addSimpleXZTweener(mapChr, ScriptRunner.calcDirImpl(0, -frontDir), MOVE_SPEED,
                            () => { if (nowState != ActorStateType.KO) playWaitMotion(); });

                        // 自分に睡眠・即死スキルなどを使って倒れている場合がある
                        // You may fall down using sleep or instant death skills on yourself.
                        if(nowState != ActorStateType.KO)
                            playMotion("battle_walk");
                    }
                    else
                    {
                        // 状態異常のモーションがあればそれを再生する
                        // If there is a status ailment motion, play it
                        playWaitMotion();
                    }
                    break;

                case ActorStateType.SKILL_WAIT:// スキル待ち / waiting for skill
                    if (source.isMovableToForward())
                    {
                        MapCharacterMoveMacro.addSimpleXZTweener(mapChr, ScriptRunner.calcDirImpl(0, frontDir), MOVE_SPEED);
                        playMotion("battle_walk");
                    }
                    break;

                case ActorStateType.SKILL:// 攻撃時 / when attacking
                    if (!string.IsNullOrEmpty(state.option))
                        playMotion(state.option);
                    else
                        playMotion("skill");
                    mapChr.lockMotion = true;
                    break;

                case ActorStateType.ITEM:// 攻撃時 / when attacking
                    if (source.isMovableToForward())
                    {
                        MapCharacterMoveMacro.addSimpleXZTweener(mapChr, ScriptRunner.calcDirImpl(0, frontDir), MOVE_SPEED);
                    }
                    playMotion("item");
                    mapChr.lockMotion = true;
                    break;

                case ActorStateType.CHANGE:
                    playMotion("change");
                    mapChr.lockMotion = true;
                    break;

                case ActorStateType.CHANGE_END:
                    // 状態異常のモーションがあればそれを再生する
                    // If there is a status ailment motion, play it
                    playWaitMotion();
                    break;

                case ActorStateType.ENTER:
                    playMotion("enter");
                    mapChr.lockMotion = true;
                    break;

                case ActorStateType.ENTER_END:
                    playMotion("wait");
                    break;

                case ActorStateType.APPEAR_END:
                    mapChr.setOpacityMultiplier(1);
                    break;

                case ActorStateType.LEAVE:
                    playMotion("leave");
                    mapChr.lockMotion = true;
                    break;

                case ActorStateType.LEAVE_END:
                    playMotion("wait");
                    break;


                case ActorStateType.DAMAGE_START:
                    // ガード中はのけぞらない
                    // Don't lean back while guarding
                    if (nowState == ActorStateType.GUARD || nowState == ActorStateType.KO)
                    {
                        this.state.type = nowState;
                        this.state.option = nowOption;
                        break;
                    }

                    playMotion("guard");
                    break;

                case ActorStateType.DAMAGE:// ダメージを受けた時 / when damaged
                    // ガード中はのけぞらない
                    // Don't lean back while guarding
                    if (nowState == ActorStateType.GUARD || nowState == ActorStateType.KO)
                    {
                        this.state.type = nowState;
                        this.state.option = nowOption;
                        break;
                    }

                    playMotion("damage");
                    mapChr.lockMotion = true;
                    break;

                case ActorStateType.DODGE:// 相手の攻撃をよけたとき / When dodging an opponent's attack
                    // ガード中はのけぞらない
                    // Don't lean back while guarding
                    if (nowState == ActorStateType.GUARD || nowState == ActorStateType.KO)
                    {
                        this.state.type = nowState;
                        this.state.option = nowOption;
                        break;
                    }

                    playMotion("dodge");
                    mapChr.lockMotion = true;
                    break;

                case ActorStateType.FATAL_DAMAGE:// ダメージを受けた時 / when damaged
                    playMotion("KO");
                    mapChr.lockMotion = true;
                    break;

                case ActorStateType.GUARD: // ガード / guard
                    playMotion("guard");
                    break;

                case ActorStateType.CHARGE: // ためる / accumulate
                    playMotion("charge");
                    break;

                case ActorStateType.KO:// 戦闘不能 / Unable to fight
                    // 致命傷を受けた後、KOが付与されるまでは何もしない
                    // After being fatally wounded, do nothing until KO is granted
                    if (nowState == ActorStateType.FATAL_DAMAGE)
                    {
                        if (!source.IsDeadCondition(true))
                        {
                            this.state.type = nowState;
                            this.state.option = nowOption;
                        }
                        break;
                    }

                    if (!string.IsNullOrEmpty(state.option))
                    {
                        playMotion(state.option);
                    }
                    else
                    {
                        playMotion("KO");
                    }
                    mapChr.lockMotion = true;
                    break;

                case ActorStateType.ESCAPE:// 逃げる / run away
                    // 死んでたらモーション変更しない
                    // Don't change motion when dead
                    if (nowState != ActorStateType.KO)
                    {
                        playMotion("escape");
                    }

                    mapChr.fixHeight = true;
                    break;

                case ActorStateType.ESCAPE_FAILED:// 逃げ失敗 / failed to escape
                    playWaitMotion();
                    if (source.isMovableToForward())
                    {
                        MapCharacterMoveMacro.addSimpleXZTweener(mapChr, ScriptRunner.calcDirImpl(0, -frontDir), MOVE_SPEED);
                    }
                    break;

                case ActorStateType.WIN:// 勝利 / victory
                    // 死んでたら何もしない
                    // nothing when dead
                    if (nowState == ActorStateType.KO)
                    {
                        this.state.type = nowState;
                        this.state.option = nowOption;
                        break;
                    }

                    playMotion("win");
                    break;

                case ActorStateType.APPEAR:// 登場 / Appearance
                    // 死んでたら何もしない
                    // nothing when dead
                    if (nowState == ActorStateType.KO)
                    {
                        this.state.type = nowState;
                        this.state.option = nowOption;
                        break;
                    }

                    playMotion("battle_walk");
                    mapChr.fixHeight = true;
                    break;
            }

            if (state.handler != null)
                state.handler();
        }

        private void playWaitMotion()
        {
            // 状態異常のモーションがあればそれを再生する
            // If there is a status ailment motion, play it
            var conditionMotion = getConditionMotion();
            if (conditionMotion == null || !playMotion(conditionMotion))
            {
                playMotion("battle_wait");
            }
        }

        private string getConditionMotion()
        {
            if (source == null)
                return null;

            string motion = null;
            int highestPriority = 0;
            foreach (var e in source.conditionInfoDic)
            {
                var condition = e.Value.rom;
                if ((condition != null) && !string.IsNullOrEmpty(condition.motion) && highestPriority < condition.Priority && condition.actionDisabled==false )
                {
                    motion = condition.motion;
                    highestPriority = condition.Priority;
                }
            }
            return motion;
        }

        internal ActorStateType getActorState()
        {
            return state.type;
        }

        internal string getActorStateOption()
        {
            return state.option;
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

		internal void setOpacityMultiplier( float v)
		{
			mapChr.setOpacityMultiplier(v);
		}

        internal Microsoft.Xna.Framework.Vector2 getScreenPos(SharpKmyMath.Matrix4 pp, SharpKmyMath.Matrix4 vv, MapScene.EffectPosType posType = MapScene.EffectPosType.Body)
        {
            int x, y;
            MapScene.GetCharacterScreenPos(mapChr, out x, out y, pp, vv, posType);
            return new Microsoft.Xna.Framework.Vector2(x, y);
        }

        public float Z { get { return mapChr.pos.Z; } }

        public float Height { get { return mapChr.getHeight(); } }

        public float X { get { return mapChr.pos.X; } }

        internal SharpKmyMath.Vector3 getPos()
        {
            return mapChr.getKmyPosition();
        }

        internal bool isBillboardDead()
        {
            return mapChr.isBillboard() &&
                (!(mapChr.getGraphic() is Common.Resource.Character) ||
                !((Common.Resource.Character)mapChr.getGraphic()).subItemList.Exists(x => x.name == "KO"));
        }

        internal bool isActorStateQueued(ActorStateType actorStateType)
        {
            return stateQueue.ToList().Exists(x => x.type == actorStateType);
        }

        internal bool isReady()
        {
            return stateCount >= state.wait && mapChr.moveMacros.Count == 0;
        }
    }
}

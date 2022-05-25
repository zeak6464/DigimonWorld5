using System;

namespace Yukar.Engine
{
    public class BattleTestScene
    {
        private GameMain owner;
        private MapScene mapScene;
        private BattleSequenceManager battleSequenceManager;
        private Common.Resource.Bgm battleBGM;
        private Common.Resource.Bgs battleBGS;
        private bool isGameover;
        public Microsoft.Xna.Framework.Point[] layout;
        public Common.Rom.BattleBgSettings battleBg;

        private bool IsBattleScene { get { return battleSequenceManager.IsDrawingBattleScene || battleSequenceManager.IsPlayingBattleEffect; } }

        public Guid[] BattleMonsters { get; set; }

        public Common.Rom.Map.BattleSetting BattleSetting { get; set; }

        public BattleTestScene(GameMain owner)
        {
            this.owner = owner;
            this.mapScene = owner.mapScene;
            this.battleSequenceManager = owner.mapScene.battleSequenceManager;

            //var window = owner.catalog.getItemFromGuid(owner.catalog.getGameSettings().messageWindow) as Common.Resource.Window;
            //var windowDrawer = new WindowDrawer(window, Graphics.LoadImage(window.path));
        }

        internal void Restart()
        {
            if (battleSequenceManager.BattleResult == BattleSequenceManager.BattleResultState.NonFinish)
                battleSequenceManager.battleState = BattleSequenceManager.BattleState.StopByEvent;
        }

        internal void Update()
        {
            mapScene.isBattle = false;
            //mapScene.Update();
            mapScene.toastWindow.Update();  // updateのかわりにtoastだけ明示的に更新してやる

            if (IsBattleScene)
            {
                battleSequenceManager.Update();

                switch (battleSequenceManager.BattleResult)
                {
                    case BattleSequenceManager.BattleResultState.Lose_Continue:
                    case BattleSequenceManager.BattleResultState.Lose_GameOver:
                    case BattleSequenceManager.BattleResultState.Lose_Advanced_GameOver:
                        isGameover = true;
                        break;
                }
            }

            if (battleSequenceManager.BattleResult != BattleSequenceManager.BattleResultState.NonFinish && !battleSequenceManager.IsDrawingBattleScene)
            {
                if (!isGameover || Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE))
                {
                    // 戦闘ごとにHP, MPを全回復 状態異常を回復
                    foreach (var hero in owner.data.party.members)
                    {
                        hero.hitpoint = hero.maxHitpoint;
                        hero.magicpoint = hero.maxMagicpoint;
                        hero.statusAilments = Common.GameData.Hero.StatusAilments.NONE;
                    }

                    if (battleBGM == null) battleBGM = mapScene.getMapBattleBgm();
                    if (battleBGS == null) battleBGS = mapScene.getMapBattleBgs();

                    mapScene.mapEngine.RegisterBattleEvents();
                    mapScene.mapEngine.PlayBattleBGM(battleBGM, battleBGS);

                    if (BattleMonsters == null)
                    {
                        battleSequenceManager.BattleStart(owner.data.party, mapScene.mapEngine.GetEncountMonsters(BattleSetting), BattleSetting);
                    }
                    else
                    {
                        if (battleBg != null)
                        {
                            var modifiedBattleSettings = new Common.Rom.Map.BattleSetting();
                            modifiedBattleSettings.battleBg = battleBg.bgRom.guId;
                            modifiedBattleSettings.battleBgCenterX = battleBg.centerX;
                            modifiedBattleSettings.battleBgCenterY = battleBg.centerY;
                            battleSequenceManager.BattleStart(owner.data.party, BattleMonsters, modifiedBattleSettings,
                                true, true, layout);
                        }
                        else
                        {
                            battleSequenceManager.BattleStart(owner.data.party, BattleMonsters, owner.mapScene.battleSetting,
                                true, true, layout);
                        }
                    }

                    isGameover = false;
                }
            }
        }

        internal void Draw()
        {
            Graphics.BeginDraw();

            if (owner.IsBattle2D)
                Graphics.DrawFillRect(0, 0, Graphics.ScreenWidth, Graphics.ScreenHeight, 0, 0, 0, 255);

            if (IsBattleScene || isGameover) battleSequenceManager.Draw();

            mapScene.toastWindow.Draw();

            if (isGameover) mapScene.DrawGameOver();

            Graphics.EndDraw();
        }
    }
}

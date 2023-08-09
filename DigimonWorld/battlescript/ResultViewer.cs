using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Yukar.Common;
using Rom = Yukar.Common.Rom;
using Resource = Yukar.Common.Resource;
using Yukar.Engine;
using static Yukar.Engine.BattleEnum;

namespace Yukar.Battle
{
    /// <summary>
    /// バトル結果表示を管理するクラス
    /// A class that manages the display of battle results
    /// </summary>
    public class ResultViewer
    {
        /// <summary>
        /// 報酬による変化を記録しておくための構造体
        /// A structure for recording changes due to rewards
        /// </summary>
        internal class ResultProperty
        {
            /// <summary>
            /// レベルアップによるスキル獲得を記録しておくための構造体
            /// A structure for recording skill acquisition by leveling up
            /// </summary>
            internal class SkillPropery
            {
                internal string Name { get; set; }
                internal Rom.NSkill Skill { get; set; }

                internal SkillPropery(string name, Rom.NSkill skill)
                {
                    Name = name;
                    Skill = skill;
                }
            }

            internal bool EndLevelUp { get; set; }

            internal List<int> LevelUpIndices { get; set; }

            internal List<Tuple<int, List<SkillPropery>>> LearnSkills { get; set; }

            internal List<CharacterLevelUpData> CharacterLevelUpData { get; set; }

            internal ResultProperty()
            {
                EndLevelUp = false;
                LevelUpIndices = new List<int>();
                LearnSkills = new List<Tuple<int, List<SkillPropery>>>();
                CharacterLevelUpData = new List<CharacterLevelUpData>();
            }
        }

        /// <summary>
        /// バトル結果表示の進行状態
        /// Battle result display progress
        /// </summary>
        enum ResultState
        {
            StartWait,
            WindowOpen,
            Exp,
            Money,
            Item,
            InventoryMax,
            ItemNextPage,
            AddExp,
            CheckLevelUp,
            LevelUpWindowOpen,
            LevelUp,
            LearnSkill,
            LevelUpWindowClose,
            End,
        }

        /// <summary>
        /// バトル結果表示の表示要素ごとのフラグ
        /// Flags for each display element of the battle result display
        /// </summary>
        [Flags]
        enum ResultWindowInfo
        {
            Title = 0x01,
            Exp = 0x02,
            Money = 0x04,
            Item = 0x08,
            NextButton = 0x10,
            CloseButton = 0x20,
        }

        GameMain owner;
        Catalog catalog;

        ResultState resultState;
        ResultWindowInfo? resultWindowInfo;
        WindowDrawer resultWindowDrawer;
        WindowDrawer resultButtonDrawer;
        WindowDrawer resultCursolDrawer;
        ResultStatusWindowDrawer resultStatusWindowDrawer;
        TextDrawer textDrawer;
        float resultFrameCount;
        bool isResultEffectSkip;

        Rom.GameSettings gameSettings;
        Resource.Texture systemIconImageId = null;
        Resource.Texture newImageId = null;
        Resource.Texture result3dImageId = null;
        internal Dictionary<Rom.NItem, Common.Resource.Texture> itemIconTable;

        WindowDrawer levelupWindowDrawer;

        internal int exp;
        internal int money;
        //Rom.Item[] dropItems;
        internal Dictionary<Rom.NItem, int> dropItemCountTable;
        Dictionary<Rom.NItem, bool> dropItemFirstGetTable;
        internal CharacterLevelUpData[] characterLevelUpData;
        internal List<int> levelUpIndices { get; set; } = new List<int>();

        int countupExp;
        int itemPageCount;
        int processedDropItemIndex;
        float alphaForProcessDropItem;

        TweenVector2 resultWindowSizeTweener;
        //TweenVector2 starEffectUV;
        TweenFloat levelupWindowSizeTweener;
        Vector2[] resultStatusDrawPosition;
        Vector2[] resultStatusWindowSize;

        float itemNewIconAnimationFrameCount;
        bool isItemNewIconVisible;

        Blinker cursorColor;

        readonly Vector2 ResultWindowSize = new Vector2(560, 440);

        int ItemPageDisplayCount = 6;

        Resource.Texture levelUpImageId;

        public bool IsEnd { get { return resultState == ResultState.End; } }
        public bool clickedCloseButton = false;

        private bool autoProceedResult = true;

        public ResultViewer(GameMain owner)
        {
            this.owner = owner;

            textDrawer = new TextDrawer(1);

            itemIconTable = new Dictionary<Rom.NItem, Common.Resource.Texture>();

            resultWindowSizeTweener = new TweenVector2();
            //starEffectUV = new TweenVector2();
            levelupWindowSizeTweener = new TweenFloat();

            cursorColor = new Blinker();

            autoProceedResult = owner.catalog.getGameSettings().AutoProceedResult;
        }

        public void LoadResourceData(Catalog catalog)
        {
            this.catalog = catalog;

            var winRes = catalog.getItemFromName("battle_skilllist", typeof(Resource.Window)) as Resource.Texture;
            Graphics.LoadImage(winRes);
            resultWindowDrawer = new WindowDrawer(winRes);

            winRes = catalog.getItemFromName("battle_unselected", typeof(Resource.Window)) as Resource.Texture;
            Graphics.LoadImage(winRes);
            resultButtonDrawer = new WindowDrawer(winRes);

            winRes = catalog.getItemFromName("battle_selected", typeof(Resource.Window)) as Resource.Texture;
            Graphics.LoadImage(winRes);
            resultCursolDrawer = new WindowDrawer(winRes);

            gameSettings = catalog.getGameSettings();
            var expPanel = catalog.getItemFromGuid(gameSettings.GraphicExpPanel) as Resource.Texture;
            if (expPanel != null && gameSettings.GraphicExpPanel == Resource.Texture.GetCommonTextureGuid(Resource.Texture.CommonTexture.ExpPanel))
            {
                expPanel.slice = Resource.Texture.Slice.Window;
                expPanel.left = 8;
                expPanel.top = 8;
                expPanel.bottom = 8;
                expPanel.right = 8;
            }
            if (expPanel == null) expPanel = Resource.Texture.getDefaultCommonTexture(Resource.Texture.CommonTexture.ExpPanel);
            Graphics.LoadImage(expPanel);

            var expBar = catalog.getItemFromGuid(gameSettings.GraphicExpBar) as Resource.Texture;
            if (expBar != null && gameSettings.GraphicExpBar == Resource.Texture.GetCommonTextureGuid(Resource.Texture.CommonTexture.ExpBar))
            {
                expBar.slice = Resource.Texture.Slice.Window;
                expBar.left = 8;
                expBar.top = 8;
                expBar.bottom = 8;
                expBar.right = 8;
            }
            if (expBar == null) expBar = Resource.Texture.getDefaultCommonTexture(Resource.Texture.CommonTexture.ExpBar);
            Graphics.LoadImage(expBar);

            var expBarMax = catalog.getItemFromGuid(gameSettings.GraphicExpBarMax) as Resource.Texture;
            if (expBarMax != null && gameSettings.GraphicExpBarMax == Resource.Texture.GetCommonTextureGuid(Resource.Texture.CommonTexture.ExpBarMax))
            {
                expBarMax.slice = Resource.Texture.Slice.Window;
                expBarMax.left = 8;
                expBarMax.top = 8;
                expBarMax.bottom = 8;
                expBarMax.right = 8;
            }
            if (expBarMax == null) expBarMax = Resource.Texture.getDefaultCommonTexture(Resource.Texture.CommonTexture.ExpBarMax);
            Graphics.LoadImage(expBarMax);

            GaugeDrawer gaugeDrawer = new GaugeDrawer(new WindowDrawer(expPanel),
                new WindowDrawer(expBar),
                new WindowDrawer(expBarMax));


            var statusWindow = catalog.getItemFromGuid(gameSettings.GraphicBattleStatusBG) as Resource.Texture;
            if (statusWindow == null) statusWindow = Resource.Texture.getDefaultCommonTexture(Resource.Texture.CommonTexture.BattleStatusBG);
            Graphics.LoadImage(statusWindow);
            resultStatusWindowDrawer = new ResultStatusWindowDrawer(new WindowDrawer(statusWindow), gaugeDrawer);


            var lvPanel = catalog.getItemFromGuid(gameSettings.GraphicLvPanel) as Resource.Texture;
            if (lvPanel != null && gameSettings.GraphicLvPanel == Resource.Texture.GetCommonTextureGuid(Resource.Texture.CommonTexture.LvPanel))
            {
                lvPanel.slice = Resource.Texture.Slice.Window;
                lvPanel.left = 10;
                lvPanel.top = 10;
                lvPanel.bottom = 10;
                lvPanel.right = 10;
            }
            if (lvPanel == null) lvPanel = Resource.Texture.getDefaultCommonTexture(Resource.Texture.CommonTexture.LvPanel);
            Graphics.LoadImage(lvPanel);
            levelupWindowDrawer = new WindowDrawer(lvPanel);

            levelUpImageId = catalog.getItemFromGuid(gameSettings.GraphicLevelUpSprite) as Resource.Texture;
            if(levelUpImageId is object)
            {
                Graphics.LoadImage(levelUpImageId);
            }

            systemIconImageId = catalog.getItemFromGuid(gameSettings.newItemIcon.guId) as Common.Resource.Texture;
            if (systemIconImageId != null) Graphics.LoadImage(systemIconImageId);

            newImageId = owner.catalog.getItemFromGuid(owner.catalog.getGameSettings().GraphicNewSprite) as Resource.Texture;
            if (newImageId == null) newImageId = Resource.Texture.getDefaultCommonTexture(Resource.Texture.CommonTexture.NewIcon);
            Graphics.LoadImage(newImageId);

            resultStatusWindowDrawer.LevelLabelText = gameSettings.glossary.battle_lv;
            resultStatusWindowDrawer.ExpLabelText = gameSettings.glossary.battle_exp;

            if (!owner.IsBattle2D)
            {
                ItemPageDisplayCount = 12;

                result3dImageId = catalog.getItemFromGuid(gameSettings.GraphicBattleResult) as Resource.Texture;
                if (result3dImageId == null) result3dImageId = Resource.Texture.getDefaultCommonTexture(Resource.Texture.CommonTexture.ResultBack);
                Graphics.LoadImage(result3dImageId);
            }
            else
            {
                ItemPageDisplayCount = 6;
            }
        }

        public void ReleaseResourceData()
        {
            Graphics.UnloadImage(resultWindowDrawer.WindowResource);
            Graphics.UnloadImage(resultButtonDrawer.WindowResource);
            Graphics.UnloadImage(resultCursolDrawer.WindowResource);
            resultStatusWindowDrawer.Release();
            Graphics.UnloadImage(levelupWindowDrawer.WindowResource);
            Graphics.UnloadImage(resultCursolDrawer.WindowResource);

            if (systemIconImageId != null) Graphics.UnloadImage(systemIconImageId);
            if (result3dImageId != null) Graphics.UnloadImage(result3dImageId);
            if (levelUpImageId != null) Graphics.UnloadImage(levelUpImageId);
            if (newImageId != null) Graphics.UnloadImage(newImageId);

            foreach (var iconImageId in itemIconTable.Values)
            {
                Graphics.UnloadImage(iconImageId);
            }

            itemIconTable.Clear();
        }

        internal void SetResultData(BattlePlayerData[] partyPlayer, int exp, int money, Rom.NItem[] items, Dictionary<Guid, int> itemDictonary)
        {
            var list = new List<CharacterLevelUpData>();

            foreach (var player in partyPlayer)
            {
                var addExp = exp * (100 + player.player.conditionAddExpRate) / 100;
                var data = new CharacterLevelUpData();

                data.player = player;
                data.expRate = 100 + player.player.conditionAddExpRate;

                data.castLevelUpData.levelGrowthRate = player.player.rom.levelGrowthRate;
                data.castLevelUpData.startLevel = player.player.level;
                data.castLevelUpData.addExp = addExp;
                data.castLevelUpData.baseExp =
                data.castLevelUpData.totalExp = player.player.exp;
                data.castLevelUpData.upLevel = 0;

                var jobCast = player.player.jobCast;

                if ((jobCast == null) || (jobCast.rom == null))
                {
                    data.jobLevelUpData.setEmptyParam();
                }
                else
                {
                    data.jobLevelUpData.levelGrowthRate = jobCast.rom.levelGrowthRate;
                    data.jobLevelUpData.startLevel = jobCast.level;
                    data.jobLevelUpData.upLevel = 0;
                    data.jobLevelUpData.addExp = Common.Rom.GameSettings.CalcExp(addExp, gameSettings.JobExpFormulaWords);
                    data.jobLevelUpData.baseExp =
                    data.jobLevelUpData.totalExp = jobCast.exp;
                }

                jobCast = player.player.sideJobCast;

                if ((jobCast == null) || (jobCast.rom == null))
                {
                    data.sideJobLevelUpData.setEmptyParam();
                }
                else
                {
                    data.sideJobLevelUpData.levelGrowthRate = jobCast.rom.levelGrowthRate;
                    data.sideJobLevelUpData.startLevel = jobCast.level;
                    data.sideJobLevelUpData.upLevel = 0;
                    data.sideJobLevelUpData.addExp = Common.Rom.GameSettings.CalcExp(addExp, gameSettings.SideJobExpFormulaWords);
                    data.sideJobLevelUpData.baseExp =
                    data.sideJobLevelUpData.totalExp = jobCast.exp;
                }

                data.learnSkillNames = new List<string>();
                data.learnSkills = new List<Rom.NSkill>();
                data.learnSkillHistory = new List<Rom.NSkill>();

                data.displayedSkills = new HashSet<Guid>();

                data.resultStatus = new ResultStatusWindowDrawer.StatusData();

                data.resultStatus.Name = player.Name;
                data.resultStatus.CurrentLevel = player.player.level;
                data.resultStatus.NextLevel = player.player.level;

                list.Add(data);
            }

            var drawPosition = new List<Vector2>();
            var windowSize = new List<Vector2>();

            foreach (var player in partyPlayer)
            {
                drawPosition.Add(player.statusWindowDrawPosition);
                windowSize.Add(player.isCharacterImageReverse ? new Vector2(-BattleViewer.StatusWindowSize.X, BattleViewer.StatusWindowSize.Y) : BattleViewer.StatusWindowSize);
            }

            resultStatusDrawPosition = drawPosition.ToArray();
            resultStatusWindowSize = windowSize.ToArray();

            characterLevelUpData = list.ToArray();

            this.exp = exp;
            this.money = money;
            //this.dropItems = items;

            dropItemCountTable = new Dictionary<Rom.NItem, int>();

            // 同じアイテムをまとめる
            // Group similar items together
            foreach (var group in items.GroupBy(item => item))
            {
                dropItemCountTable.Add(group.Key, group.Count());
            }

            dropItemFirstGetTable = new Dictionary<Rom.NItem, bool>();

            foreach (var item in dropItemCountTable.Keys)
            {
                dropItemFirstGetTable.Add(item, !itemDictonary.ContainsKey(item.guId));
            }

            foreach (var item in dropItemCountTable.Keys)
            {
                var icon = catalog.getItemFromGuid(item.icon.guId) as Common.Resource.Texture;

                if (icon != null && !itemIconTable.ContainsKey(item))
                {
                    Graphics.LoadImage(icon);

                    itemIconTable.Add(item, icon);
                }
            }

            resultFrameCount = 0;
            itemNewIconAnimationFrameCount = 0;

            isItemNewIconVisible = false;

            countupExp = 0;
            itemPageCount = 1;

            processedDropItemIndex = 0;
            alphaForProcessDropItem = 1.0f;
        }

        internal void Start()
        {
            resultWindowInfo = null;

            isResultEffectSkip = false;

            resultWindowSizeTweener.Begin(new Vector2(0, 0), new Vector2(560, 440), 15);

            ChangeResultState(ResultState.WindowOpen);
        }

        internal ResultProperty Update()
        {
            var resultProperty = new ResultProperty();
            resultFrameCount += GameMain.getRelativeParam60FPS();

            if (resultWindowSizeTweener.IsPlayTween)
            {
                resultWindowSizeTweener.Update();
            }

            if (levelupWindowSizeTweener.IsPlayTween)
            {
                levelupWindowSizeTweener.Update();
            }

            // リザルト画面 遷移
            // Result screen transition
            // 1. ウィンドウ表示
            // 1. Window display
            // 2. リザルト内容表示
            // 2. Result display
            // 3. レベルアップ処理
            // 3. Level up process
            switch (resultState)
            {
                case ResultState.WindowOpen:
                    resultWindowSizeTweener.Update();

                    if (!resultWindowSizeTweener.IsPlayTween)
                    {
                        resultWindowInfo = new ResultWindowInfo();

                        resultWindowInfo |= ResultWindowInfo.Title;

                        ChangeResultState(ResultState.Exp);
                    }
                    break;

                case ResultState.Exp:
                    if (resultFrameCount >= 10 || isResultEffectSkip)
                    {
                        resultWindowInfo |= ResultWindowInfo.Exp;

                        ChangeResultState(ResultState.Money);
                    }
                    break;

                case ResultState.Money:
                    if (resultFrameCount >= 10 || isResultEffectSkip)
                    {
                        resultWindowInfo |= ResultWindowInfo.Money;

                        ChangeResultState(ResultState.Item);

                        processedDropItemIndex = 0;
                    }
                    break;

                case ResultState.Item:
                    if ((resultFrameCount >= 10) || Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE, Input.GameState.MENU) || isResultEffectSkip)
                    {
                        resultWindowInfo |= ResultWindowInfo.Item;

                        // 新規にアイテムスタックが増える場合、インベントリの空きをチェックする
                        // Check inventory space when new item stack increases
                        while (processedDropItemIndex < itemPageCount * ItemPageDisplayCount &&
                            processedDropItemIndex < dropItemCountTable.Count)
                        {
                            // アイテムに空きがなかったら捨てるダイアログを表示する
                            // Display a dialog to discard items if there are no free items
                            var dropItem = dropItemCountTable.ElementAt(processedDropItemIndex);
                            if (owner.data.party.GetItemNum(dropItem.Key.guId) == 0 &&
                                owner.data.party.checkInventoryEmptyNum() <= 0)
                            {
                                owner.mapScene.ShowTrashWindow(dropItem.Key.guId,
                                    dropItemCountTable.ElementAt(processedDropItemIndex).Value);
                                ChangeResultState(ResultState.InventoryMax);
                            }
                            // 空きがあった場合は普通に加える
                            // Add normally if there is space
                            else
                            {
                                owner.data.party.AddItem(dropItem.Key.guId, dropItem.Value);
                            }

                            processedDropItemIndex++;

                            if (resultState == ResultState.InventoryMax)
                                break;
                        }

                        if (resultState == ResultState.InventoryMax)
                            break;

                        if (itemPageCount * ItemPageDisplayCount >= dropItemCountTable.Keys.Count)
                        {
                            ChangeResultState(ResultState.AddExp);
                        }
                        else
                        {
                            resultWindowInfo |= ResultWindowInfo.NextButton;

                            ChangeResultState(ResultState.ItemNextPage);
                        }
                    }
                    break;

                case ResultState.InventoryMax:
                    if (!owner.mapScene.IsTrashVisible())
                        ChangeResultState(ResultState.Item);
                    break;

                case ResultState.ItemNextPage:
                    itemNewIconAnimationFrameCount += GameMain.getRelativeParam60FPS();

                    if (itemNewIconAnimationFrameCount >= 30)
                    {
                        itemNewIconAnimationFrameCount = 0;
                        isItemNewIconVisible = !isItemNewIconVisible;
                    }

                    if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE, Input.GameState.MENU))
                    {
                        itemPageCount++;

                        resultWindowInfo &= ~ResultWindowInfo.NextButton;

                        ChangeResultState(ResultState.Item);
                    }
                    break;

                case ResultState.AddExp:
                    if (countupExp >= exp || characterLevelUpData.All(character => !character.IsAddExp))
                    {
                        resultWindowInfo |= ResultWindowInfo.CloseButton;

                        cursorColor.setColor(new Color(Color.White, 255), new Color(Color.White, 0), 30);

                        ChangeResultState(ResultState.End);
                    }
                    else
                    {
                        int addExp = 1;

                        // 獲得した経験値が多いと表示に時間が掛かるので一度に加算する経験値を多くする
                        // If you have a lot of experience points, it will take time to display, so add more experience points at once
                        if (exp > 100)
                        {
                            var addExpCharacters = characterLevelUpData.Where(character => character.IsAddExp);

                            addExp = (int)(exp * 0.05f);   // 獲得した経験値の5% / 5% of the experience gained

                            addExp = Math.Min(addExp, exp - countupExp);   // 足していない残りEXP全て / All remaining EXP not added

                            addExp = Math.Min(addExp, (int)addExpCharacters.Select(character => character.castLevelUpData.NextLevelNeedExp * 0.05f).Min());            // 次のレベルアップまでに必要な経験値の5% / 5% of the experience required to level up to the next level
                            addExp = Math.Min(addExp, addExpCharacters.Select(character => character.castLevelUpData.NextLevelNeedExp - character.castLevelUpData.CurrentExp).Min());  // 次のレベルアップまでに必要な残り経験値 / Remaining experience required for next level up

                            if (addExp <= 0) addExp = 1;
                        }

                        countupExp += addExp;

                        for (int index = 0; index < characterLevelUpData.Length; index++)
                        {
                            // 戦闘不能のメンバーには経験値を加算しない
                            // Do not add experience points to incapacitated members
                            // レベル上限に達していた場合も加算しない
                            // Even if you have reached the level cap, it will not be added
                            if (!characterLevelUpData[index].IsAddExp)
                            {
                                continue;
                            }

                            characterLevelUpData[index].setAddExpScaleFactor((float)countupExp / characterLevelUpData[index].castLevelUpData.addExp);

                            // レベルアップ
                            // Level up
                            if (characterLevelUpData[index].castLevelUpData.IsLevelUp || characterLevelUpData[index].jobLevelUpData.IsLevelUp || characterLevelUpData[index].sideJobLevelUpData.IsLevelUp)
                            {
                                levelUpIndices.Add(index);
                            }
                        }

                        ChangeResultState(ResultState.CheckLevelUp);
                    }
                    break;

                case ResultState.CheckLevelUp:
                    foreach (var index in levelUpIndices.ToArray())
                    {
                        levelUpIndices.Remove(index);
                        resultProperty.LevelUpIndices.Add(index);

                        characterLevelUpData[index].onLevelUpEffect = true;
                        characterLevelUpData[index].easingProgress = 0;
                        characterLevelUpData[index].onLevelUpText = false;

                        if (characterLevelUpData[index].castLevelUpData.IsLevelUp)
                        {
                            characterLevelUpData[index].castLevelUpData.upLevel++;
                        }
                        if (characterLevelUpData[index].jobLevelUpData.IsLevelUp)
                        {
                            characterLevelUpData[index].jobLevelUpData.upLevel++;
                        }
                        if (characterLevelUpData[index].sideJobLevelUpData.IsLevelUp)
                        {
                            characterLevelUpData[index].sideJobLevelUpData.upLevel++;
                        }

                        characterLevelUpData[index].resultStatus.NextLevel = characterLevelUpData[index].castLevelUpData.CurrentLevel;

                        // 取得できるスキルがあるか確認
                        // Check if you have the skills to acquire
                        // 一度表示したスキルは何度も表示しない
                        // Skills that have been displayed once will not be displayed again
                        var learnSkills = characterLevelUpData[index].player.player.getAvailableSkillsWithJob(characterLevelUpData[index].castLevelUpData.startLevel + characterLevelUpData[index].castLevelUpData.upLevel, characterLevelUpData[index].jobLevelUpData.startLevel + characterLevelUpData[index].jobLevelUpData.upLevel, characterLevelUpData[index].sideJobLevelUpData.startLevel + characterLevelUpData[index].sideJobLevelUpData.upLevel, true);

                        characterLevelUpData[index].learnSkills.Clear();
                        characterLevelUpData[index].learnSkillNames.Clear();

                        var skills = new List<ResultProperty.SkillPropery>();
                        foreach (var skillId in learnSkills)
                        {
                            var skill = catalog.getItemFromGuid(skillId);

                            if (skill != null)
                            {
                                var nskill = skill as Rom.NSkill;
                                if (nskill != null)
                                {
                                    skills.Add(new ResultProperty.SkillPropery(skill.name, nskill));
                                    characterLevelUpData[index].learnSkills.Add(nskill);
                                    characterLevelUpData[index].learnSkillHistory.Add(nskill);

                                }

                                characterLevelUpData[index].learnSkillNames.Add(skill.name);
                            }
                        }

                        resultProperty.LearnSkills.Add(new Tuple<int, List<ResultProperty.SkillPropery>>(index, skills));
                        characterLevelUpData[index].displayedSkills.UnionWith(learnSkills);

                        characterLevelUpData[index].player.ChangeEmotion(Resource.Face.FaceType.FACE_SMILE);

                        if (gameSettings.singleLevelUpEffect)
                        {
                            break;
                        }
                    }

                    if (resultProperty.LevelUpIndices.Count > 0)
                    {
                        levelupWindowSizeTweener.Begin(0, 1.0f, 15);

                        Audio.PlaySound(owner.se.levelup);

                        ChangeResultState(ResultState.LevelUpWindowOpen);
                    }
                    else
                    {
                        ChangeResultState(ResultState.AddExp);
                    }
                    break;

                case ResultState.LevelUpWindowOpen:
                    if (!levelupWindowSizeTweener.IsPlayTween)
                    {
                        for (int index = 0; index < characterLevelUpData.Length; index++)
                        {
                            characterLevelUpData[index].onLevelUpText = true;
                        }

                        ChangeResultState(ResultState.LevelUp);
                    }
                    break;

                case ResultState.LevelUp:

                    var frameCount = resultFrameCount;
                    if (frameCount > 120) frameCount = 120;
                    for (int index = 0; index < characterLevelUpData.Length; index++)
                    {
                        characterLevelUpData[index].easingProgress = frameCount / 120;
                    }

                    if ((resultFrameCount >= 120 && autoProceedResult) || Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE, Input.GameState.MENU))
                    {
                        frameCount = 120f;
                        for (int index = 0; index < characterLevelUpData.Length; index++)
                        {
                            characterLevelUpData[index].onLevelUpText = false;
                            characterLevelUpData[index].easingProgress = frameCount / 120;
                        }

                        levelupWindowSizeTweener.Begin(0, 15);
                        ChangeResultState(ResultState.LevelUpWindowClose);
                    }
                    break;

                case ResultState.LevelUpWindowClose:
                    if (!levelupWindowSizeTweener.IsPlayTween)
                    {
                        for (int index = 0; index < characterLevelUpData.Length; index++)
                        {
                            characterLevelUpData[index].onLevelUpEffect = false;
                        }

                        resultProperty.EndLevelUp = true;

                        ChangeResultState(ResultState.CheckLevelUp);
                    }
                    break;

                case ResultState.End:
                    itemNewIconAnimationFrameCount += GameMain.getRelativeParam60FPS();

                    if (itemNewIconAnimationFrameCount >= 30)
                    {
                        itemNewIconAnimationFrameCount = 0;
                        isItemNewIconVisible = !isItemNewIconVisible;
                    }

                    cursorColor.update();
                    break;
            }

            for (int index = 0; index < characterLevelUpData.Length; index++)
            {
                if (characterLevelUpData[index].castLevelUpData.IsLevelMax)
                {
                    characterLevelUpData[index].resultStatus.GaugePercent = 1.0f;
                }
                else
                {
                    // 小数点第2位で切り捨て
                    // Truncate to two decimal places
                    characterLevelUpData[index].resultStatus.GaugePercent = (float)(Math.Floor(((double)characterLevelUpData[index].castLevelUpData.CurrentExp / characterLevelUpData[index].castLevelUpData.NextLevelNeedExp) * 100) / 100);
                }
            }

            // アイテムを捨てるウィンドウが重なってる時、リザルトを薄くするための機能
            // A function to make the results lighter when the drop item window overlaps
            if (resultState == ResultState.InventoryMax)
                alphaForProcessDropItem = Math.Max(0.25f, alphaForProcessDropItem * 0.9f);
            else
                alphaForProcessDropItem = 1 - (1 - alphaForProcessDropItem) * 0.9f;


            for (int index = 0; index < characterLevelUpData.Length; index++)
            {
                resultProperty.CharacterLevelUpData.Add(characterLevelUpData[index]);
            }

            return resultProperty;
        }

        public void Draw()
        {
            if (owner.IsBattle2D)
                DrawFor2D();
            else
                DrawFor3D();
        }

        private void DrawFor2D()
        {
            var glossary = gameSettings.glossary;

            for (int index = 0; index < characterLevelUpData.Length; index++)
            {
                // レベルアップ情報を表示
                // Display level up information
                if (characterLevelUpData[index].onLevelUpEffect)
                {
                    // レベルアップウィンドウ
                    // Level up window
                    Vector2 levelupWindowOriginalSize = new Vector2(192, 32);
                    Vector2 levelupWindowSize = levelupWindowOriginalSize;

                    levelupWindowSize.Y *= levelupWindowSizeTweener.CurrentValue;

                    var levelupWindowPosition = resultStatusDrawPosition[index] - new Vector2(0, levelupWindowSize.Y);

                    Vector2 skillWindowOffset = new Vector2(0, 40);

                    levelupWindowDrawer.Draw(levelupWindowPosition, levelupWindowSize);

                    if (characterLevelUpData[index].onLevelUpText) textDrawer.DrawStringSoloColor(glossary.battle_levelup, levelupWindowPosition, (int)levelupWindowSize.X, TextDrawer.HorizontalAlignment.Center, Color.Red);
                    //if (characterLevelUpData[index].onLevelUpText) Graphics.DrawImage(levelUpImageId, (int)levelupWindowPosition.X, (int)levelupWindowPosition.Y);

                    // スキルウィンドウ
                    // skill window
                    Vector2 skillWindowOriginalSize = new Vector2(192, 32);
                    Vector2 skillWindowPosition = levelupWindowPosition;

                    foreach (var skillName in characterLevelUpData[index].learnSkillNames)
                    {
                        // 習得したスキル名が長いときは途中で改行して表示する
                        // If the learned skill name is long, it will be displayed with a line break in the middle.
                        // 最大2行まで表示する
                        // Display up to 2 lines

                        skillWindowPosition -= skillWindowOffset;
                    }
                }

                var statusWindowColor = characterLevelUpData[index].player.IsDeadCondition() ? Color.Red : Color.White;

                resultStatusWindowDrawer.Draw(characterLevelUpData[index].resultStatus, resultStatusDrawPosition[index], resultStatusWindowSize[index], statusWindowColor);
            }

            Vector2 windowPosition = (new Vector2(Graphics.ScreenWidth, Graphics.ScreenHeight) / 2) - (resultWindowSizeTweener.CurrentValue / 2);

            // リザルトウィンドウの下地を表示
            // Show result window background
            resultWindowDrawer.Draw(windowPosition, resultWindowSizeTweener.CurrentValue,
                new Color(alphaForProcessDropItem, alphaForProcessDropItem, alphaForProcessDropItem, alphaForProcessDropItem));

            // リザルトウィンドウの各項目を表示
            // Display each item in the result window
            if (resultWindowInfo.HasValue)
            {
                Color TextColor = Color.White;
                Color LineColor = Color.Gray;

                TextColor.A = LineColor.A = (byte)(alphaForProcessDropItem * 255);
                TextColor.R = (byte)(TextColor.R * alphaForProcessDropItem);
                TextColor.G = (byte)(TextColor.G * alphaForProcessDropItem);
                TextColor.B = (byte)(TextColor.B * alphaForProcessDropItem);
                LineColor.R = (byte)(LineColor.R * alphaForProcessDropItem);
                LineColor.G = (byte)(LineColor.G * alphaForProcessDropItem);
                LineColor.B = (byte)(LineColor.B * alphaForProcessDropItem);

                // Title
                if (resultWindowInfo.Value.HasFlag(ResultWindowInfo.Title))
                {
                    textDrawer.DrawStringSoloColor(glossary.battle_result, windowPosition + new Vector2(8, 4), TextColor);
                }

                // EXP
                textDrawer.DrawStringSoloColor(glossary.exp, windowPosition + new Vector2(50, 40), TextColor);

                Graphics.DrawFillRect((int)windowPosition.X + 50, (int)windowPosition.Y + 70, 460, 1, LineColor.R, LineColor.G, LineColor.B, LineColor.A);

                if (resultWindowInfo.Value.HasFlag(ResultWindowInfo.Exp))
                {
                    textDrawer.DrawStringSoloColor(exp.ToString(), windowPosition + new Vector2(150, 40), 100, TextDrawer.HorizontalAlignment.Right, TextColor);
                }

                // Money
                textDrawer.DrawStringSoloColor(glossary.moneyName, windowPosition + new Vector2(50, 90), TextColor);

                Graphics.DrawFillRect((int)windowPosition.X + 50, (int)windowPosition.Y + 120, 460, 1, LineColor.R, LineColor.G, LineColor.B, LineColor.A);

                if (resultWindowInfo.Value.HasFlag(ResultWindowInfo.Money))
                {
                    textDrawer.DrawStringSoloColor(money.ToString(), windowPosition + new Vector2(150, 90), 100, TextDrawer.HorizontalAlignment.Right, TextColor);
                }

                // Item
                textDrawer.DrawStringSoloColor(glossary.item, windowPosition + new Vector2(50, 140), TextColor);

                Graphics.DrawFillRect((int)windowPosition.X + 50, (int)windowPosition.Y + 170, 460, 1, LineColor.R, LineColor.G, LineColor.B, LineColor.A);

                if (resultWindowInfo.Value.HasFlag(ResultWindowInfo.Item))
                {
                    int itemCount = 0;
                    var itemDataDrawPosition = windowPosition + new Vector2(50, 180);
                    var itemDataDrawPositionOffset = Vector2.Zero;

                    foreach (var item in dropItemCountTable.Keys.Skip((itemPageCount - 1) * ItemPageDisplayCount).Take(ItemPageDisplayCount))
                    {
                        Vector2 pos = itemDataDrawPosition + itemDataDrawPositionOffset;

                        var icon = item.icon;
                        var rect = icon.getRect();

                        if (itemIconTable.ContainsKey(item))
                        {
                            Graphics.DrawImage(itemIconTable[item], (int)pos.X, (int)pos.Y, new Rectangle(rect.X, rect.Y, rect.Width, rect.Height), TextColor);
                        }

                        // New Icon
                        if (isItemNewIconVisible && dropItemFirstGetTable[item])
                        {
                            //Graphics.DrawImage(systemIconImageId, (int)pos.X, (int)pos.Y,
                            //   new Rectangle(gameSettings.newItemIcon.x * Resource.Icon.ICON_WIDTH,
                            //       gameSettings.newItemIcon.y * Resource.Icon.ICON_HEIGHT, Resource.Icon.ICON_WIDTH, Resource.Icon.ICON_HEIGHT));

                            Graphics.DrawImage(newImageId, (int)pos.X, (int)pos.Y);
                        }

                        pos.X += Resource.Icon.ICON_WIDTH;

                        const int ItemTextOffsetX = 460;
                        Vector2 textAreaSize = new Vector2(ItemTextOffsetX - Resource.Icon.ICON_WIDTH, Resource.Icon.ICON_HEIGHT);

                        string itemName = item.name;

                        // 同じアイテムを2個以上入手した場合は個数を表示して1行にまとめる
                        // If you get 2 or more of the same item, display the number and put it in one line
                        if (dropItemCountTable[item] > 1) itemName += string.Format(" ×{0}", dropItemCountTable[item]);

                        // アイテム名が長いとウィンドウからはみ出すので縮小して表示する
                        // If the item name is long, it will protrude from the window, so it will be displayed in a reduced size.
                        float scale = 1.0f;
                        float scaleX = 1.0f;

                        scaleX = textAreaSize.X / textDrawer.MeasureString(itemName).X;

                        if (scaleX < 1.0f) scale = scaleX;

                        textDrawer.DrawStringSoloColor(itemName, pos, textAreaSize, TextDrawer.HorizontalAlignment.Left, TextDrawer.VerticalAlignment.Center, TextColor, scale);

                        itemDataDrawPositionOffset.Y += textAreaSize.Y;

                        itemCount++;
                    }
                }

                if (resultWindowInfo.Value.HasFlag(ResultWindowInfo.NextButton) || resultWindowInfo.Value.HasFlag(ResultWindowInfo.CloseButton))
                {
                    Vector2 buttonPosition = windowPosition + new Vector2(ResultWindowSize.X * 0.25f, ResultWindowSize.Y * 0.875f);
                    Vector2 buttonSize = new Vector2(ResultWindowSize.X * 0.5f, ResultWindowSize.Y * 0.1f);

                    resultButtonDrawer.Draw(buttonPosition, buttonSize);
                    resultCursolDrawer.Draw(buttonPosition, buttonSize, cursorColor.getColor());

                    string text = "";

                    if (resultWindowInfo.Value.HasFlag(ResultWindowInfo.NextButton)) text = glossary.battle_result_continue;
                    else if (resultWindowInfo.Value.HasFlag(ResultWindowInfo.CloseButton)) text = glossary.close;

                    textDrawer.DrawStringSoloColor(text, buttonPosition, buttonSize, TextDrawer.HorizontalAlignment.Center, TextDrawer.VerticalAlignment.Center, TextColor);

                    if (this.owner.IsBattle2D == false)
                    {
                        if (Touch.IsDown()) clickCloseButton(Touch.GetTouchPosition(0), buttonPosition, buttonSize);
                    }
                }
            }
        }

        private void clickCloseButton(SharpKmyMath.Vector2 touchPos, Vector2 pos, Vector2 size)
        {
            var x0 = pos.X;
            var y0 = pos.Y;
            if (x0 < touchPos.x && touchPos.x < x0 + size.X && y0 < touchPos.y && touchPos.y < y0 + size.Y)
            {
                clickedCloseButton = true;
            }

        }

        private void DrawFor3D()
        {
            Graphics.DrawImage(result3dImageId, 0, 0);

            var glossary = gameSettings.glossary;

            for (int index = 0; index < (characterLevelUpData == null ? 0 : characterLevelUpData.Length); index++)
            {
                // レベルアップ情報を表示
                // Display level up information
                if (characterLevelUpData[index].onLevelUpEffect)
                {
                    // レベルアップウィンドウ
                    // Level up window
                    Vector2 levelupWindowOriginalSize = new Vector2(192, 32);
                    Vector2 levelupWindowSize = levelupWindowOriginalSize;

                    levelupWindowSize.Y *= levelupWindowSizeTweener.CurrentValue;

                    var levelupWindowPosition = characterLevelUpData[index].player.statusWindowDrawPosition - new Vector2(levelupWindowSize.X / 2, 0);

                    Vector2 skillWindowOffset = new Vector2(0, 40);

                    levelupWindowDrawer.Draw(levelupWindowPosition, levelupWindowSize);

                    if (characterLevelUpData[index].onLevelUpText) textDrawer.DrawStringSoloColor(glossary.battle_levelup, levelupWindowPosition, (int)levelupWindowSize.X, TextDrawer.HorizontalAlignment.Center, Color.Red);
                    //if (characterLevelUpData[index].onLevelUpText) Graphics.DrawImage(levelUpImageId, (int)levelupWindowPosition.X, (int)levelupWindowPosition.Y);
                    // スキルウィンドウ
                    // skill window
                    Vector2 skillWindowOriginalSize = new Vector2(192, 32);
                    Vector2 skillWindowPosition = levelupWindowPosition;

                    foreach (var skillName in characterLevelUpData[index].learnSkillNames)
                    {
                        skillWindowPosition -= skillWindowOffset;
                    }
                }
            }

            // リザルトウィンドウの各項目を表示
            // Display each item in the result window
            if (resultWindowInfo.HasValue)
            {
                Color TextColor = Color.White;
                Color LineColor = Color.Gray;

                TextColor.A = LineColor.A = (byte)(alphaForProcessDropItem * 255);
                TextColor.R = (byte)(TextColor.R * alphaForProcessDropItem);
                TextColor.G = (byte)(TextColor.G * alphaForProcessDropItem);
                TextColor.B = (byte)(TextColor.B * alphaForProcessDropItem);
                LineColor.R = (byte)(LineColor.R * alphaForProcessDropItem);
                LineColor.G = (byte)(LineColor.G * alphaForProcessDropItem);
                LineColor.B = (byte)(LineColor.B * alphaForProcessDropItem);

                // Title
                if (resultWindowInfo.Value.HasFlag(ResultWindowInfo.Title))
                {
                    textDrawer.DrawStringSoloColor(glossary.battle_result, new Vector2(8, 0), TextColor);
                }

                // EXP
                textDrawer.DrawStringSoloColor(glossary.exp, new Vector2(32, 34), TextColor, 0.75f);

                if (resultWindowInfo.Value.HasFlag(ResultWindowInfo.Exp))
                {
                    textDrawer.DrawStringSoloColor(exp.ToString(), new Vector2(160, 34), 100, TextDrawer.HorizontalAlignment.Right, TextColor, 0.75f);
                }

                // Money
                textDrawer.DrawStringSoloColor(glossary.moneyName, new Vector2(480 + 32, 34), TextColor, 0.75f);

                if (resultWindowInfo.Value.HasFlag(ResultWindowInfo.Money))
                {
                    textDrawer.DrawStringSoloColor(money.ToString(), new Vector2(480 + 160, 34), 100, TextDrawer.HorizontalAlignment.Right, TextColor, 0.75f);
                }

                // Item
                textDrawer.DrawStringSoloColor(glossary.item, new Vector2(32, 66), TextColor, 0.75f);

                if (resultWindowInfo.Value.HasFlag(ResultWindowInfo.Item))
                {
                    int itemCount = 0;
                    var itemDataDrawPosition = new Vector2(96, 96);
                    var itemDataDrawPositionOffset = Vector2.Zero;

                    foreach (var item in dropItemCountTable.Keys.Skip((itemPageCount - 1) * ItemPageDisplayCount).Take(ItemPageDisplayCount))
                    {
                        Vector2 pos = itemDataDrawPosition + itemDataDrawPositionOffset;

                        var icon = item.icon;
                        var rect = icon.getRect();

                        if (itemIconTable.ContainsKey(item))
                        {
                            Graphics.DrawImage(itemIconTable[item], (int)pos.X, (int)pos.Y, new Rectangle(rect.X, rect.Y, rect.Width, rect.Height), TextColor);
                        }

                        // New Icon
                        if (isItemNewIconVisible && dropItemFirstGetTable[item])
                        {
                            //Graphics.DrawImage(systemIconImageId, (int)pos.X, (int)pos.Y,
                            //    new Rectangle(gameSettings.newItemIcon.x * Resource.Icon.ICON_WIDTH,
                            //        gameSettings.newItemIcon.y * Resource.Icon.ICON_HEIGHT, Resource.Icon.ICON_WIDTH, Resource.Icon.ICON_HEIGHT));

                            Graphics.DrawImage(newImageId, (int)pos.X, (int)pos.Y);
                        }

                        pos.X += Resource.Icon.ICON_WIDTH;

                        const int ItemTextOffsetX = 460;
                        Vector2 textAreaSize = new Vector2(ItemTextOffsetX - Resource.Icon.ICON_WIDTH, Resource.Icon.ICON_HEIGHT);

                        string itemName = item.name;

                        // 同じアイテムを2個以上入手した場合は個数を表示して1行にまとめる
                        // If you get 2 or more of the same item, display the number and put it in one line
                        if (dropItemCountTable[item] > 1) itemName += string.Format(" ×{0}", dropItemCountTable[item]);

                        // アイテム名が長いとウィンドウからはみ出すので縮小して表示する
                        // If the item name is long, it will protrude from the window, so it will be displayed in a reduced size.
                        float scale = 1.0f;
                        float scaleX = 1.0f;

                        scaleX = textAreaSize.X / textDrawer.MeasureString(itemName).X;

                        if (scaleX < 1.0f) scale = scaleX;

                        textDrawer.DrawStringSoloColor(itemName, pos, textAreaSize, TextDrawer.HorizontalAlignment.Left, TextDrawer.VerticalAlignment.Center, TextColor, scale);

                        const int X_OFFSET_INC = 280;
                        if (itemDataDrawPositionOffset.X < X_OFFSET_INC * 2)
                        {
                            itemDataDrawPositionOffset.X += X_OFFSET_INC;
                        }
                        else
                        {
                            itemDataDrawPositionOffset.X = 0;
                            itemDataDrawPositionOffset.Y += textAreaSize.Y;
                        }

                        itemCount++;
                    }
                }
            }
        }

        private void ChangeResultState(ResultState nextResultState)
        {
            resultState = nextResultState;

            resultFrameCount = 0;
        }

        internal List<CharacterLevelUpData> CheckSkillOver()
        {
            var result = new List<CharacterLevelUpData>();
            var skillMax = catalog.getGameSettings().ownedSkillMax;
            if (skillMax == 0) return result;

            foreach (var characterLevelUp in characterLevelUpData)
            {
                var currentSkillCount = characterLevelUp.player.player.skills.Count;
                if (currentSkillCount + characterLevelUp.learnSkillHistory.Count > skillMax) result.Add(characterLevelUp);
            }
            return result;
        }

    }
}

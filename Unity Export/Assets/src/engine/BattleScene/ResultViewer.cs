using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;

using Yukar.Common;
using Yukar.Common.GameData;

using Rom = Yukar.Common.Rom;
using Resource = Yukar.Common.Resource;
using StatusAilments = Yukar.Common.GameData.Hero.StatusAilments;

namespace Yukar.Engine
{
    public class ResultViewer
    {
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
            LevelUpWindowOpen,
            LevelUp,
            LearnSkill,
            LevelUpWindowClose,
            End,
        }

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

        struct CharacterLevelUpData
        {
            public BattlePlayerData player;
            public ResultStatusWindowDrawer.StatusData resultStatus;
            public int startLevel;
            public int upLevel;
            public int totalExp;
            public List<string> learnSkills;
            public HashSet<Guid> displayedSkills;
            public bool onLevelUpEffect;
            public bool onLevelUpText;

            public bool IsAddExp { get { return !(player.Status == StatusAilments.DOWN || IsLevelMax); } }

            public int CurrentLevel { get { return startLevel + upLevel; } }
            public int CurrentLevelNeedTotalExp { get { return (CurrentLevel >= 2) ? Party.expTable[player.player.rom.levelGrowthRate, CurrentLevel - 2] : 0; } }
            public int CurrentExp { get { return totalExp - CurrentLevelNeedTotalExp; } }
            public int NextLevelNeedExp { get { return Party.expTable[player.player.rom.levelGrowthRate, CurrentLevel - 1] - CurrentLevelNeedTotalExp; } }
            public bool IsLevelMax { get { return (CurrentLevel >= Party.expTable.GetLength(1)); } }
        }

        GameMain owner;
        Catalog catalog;

        ResultState resultState;
        ResultWindowInfo? resultWindowInfo;
        WindowDrawer resultWindowDrawer;
        WindowDrawer resultButtonDrawer;
        WindowDrawer resultCursolDrawer;
        ResultStatusWindowDrawer resultStatusWindowDrawer;
        CharacterFaceImageDrawer characterFaceImageDrawer;
        TextDrawer textDrawer;
        float resultFrameCount;
        bool isResultEffectSkip;

        Rom.GameSettings gameSettings;
        int systemIconImageId = -1;
        int result3dImageId = -1;
        Dictionary<Rom.Item, int> itemIconTable;

        WindowDrawer levelupWindowDrawer;
        WindowDrawer learnSkillWindowDrawer;

        int exp;
        int money;
        //Rom.Item[] dropItems;
        Dictionary<Rom.Item, int> dropItemCountTable;
        Dictionary<Rom.Item, bool> dropItemFirstGetTable;
        CharacterLevelUpData[] characterLevelUpData;

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

        Blinker cursolColor;

        readonly Vector2 ResultWindowSize = new Vector2(560, 440);

        int ItemPageDisplayCount = 6;

        public bool IsEnd { get { return resultState == ResultState.End; } }
        public bool clickedCloseButton = false;

        public ResultViewer(GameMain owner)
        {
            this.owner = owner;

            textDrawer = new TextDrawer(1);

            characterFaceImageDrawer = new CharacterFaceImageDrawer();

            itemIconTable = new Dictionary<Rom.Item, int>();

            resultWindowSizeTweener = new TweenVector2();
            //starEffectUV = new TweenVector2();
            levelupWindowSizeTweener = new TweenFloat();

            cursolColor = new Blinker();
        }

        public void LoadResourceData(Catalog catalog)
        {
            this.catalog = catalog;

            var winRes = catalog.getItemFromName("battle_skilllist", typeof(Resource.Window)) as Resource.Window;
            resultWindowDrawer = new WindowDrawer(winRes, winRes == null ? -1 : Graphics.LoadImage(winRes.path));

            winRes = catalog.getItemFromName("battle_unselected", typeof(Resource.Window)) as Resource.Window;
            resultButtonDrawer = new WindowDrawer(winRes, winRes == null ? -1 : Graphics.LoadImage(winRes.path));

            winRes = catalog.getItemFromName("battle_selected", typeof(Resource.Window)) as Resource.Window;
            resultCursolDrawer = new WindowDrawer(winRes, winRes == null ? -1 : Graphics.LoadImage(winRes.path));

            var windowInfo = new Resource.Window() { left = 8, right = 8, top = 8, bottom = 8 };
            GaugeDrawer gaugeDrawer = new GaugeDrawer(new WindowDrawer(windowInfo, Graphics.LoadImage("./res/battle/exp_panel.png")), new WindowDrawer(windowInfo, Graphics.LoadImage("./res/battle/exp_bar.png")), new WindowDrawer(windowInfo, Graphics.LoadImage("./res/battle/exp_bar_max.png")));

            resultStatusWindowDrawer = new ResultStatusWindowDrawer(new WindowDrawer(new Resource.Window(), Graphics.LoadImage("./res/battle/battle_status_bg.png")), gaugeDrawer);

            winRes = catalog.getItemFromName("battle_message", typeof(Resource.Window)) as Resource.Window;
            levelupWindowDrawer = new WindowDrawer(winRes, Graphics.LoadImage("./res/battle/lv_panel.png"));
            learnSkillWindowDrawer = new WindowDrawer(winRes, Graphics.LoadImage("./res/battle/skill_panel.png"));

            gameSettings = catalog.getGameSettings();

            var icon = catalog.getItemFromGuid(gameSettings.newItemIcon.guId) as Common.Resource.Icon;
            if(icon != null)
                systemIconImageId = Graphics.LoadImage(icon.path);

            resultStatusWindowDrawer.LevelLabelText = gameSettings.glossary.battle_lv;
            resultStatusWindowDrawer.ExpLabelText = gameSettings.glossary.battle_exp;

            if (!owner.IsBattle2D)
            {
                ItemPageDisplayCount = 12;
                result3dImageId = Graphics.LoadImage("./res/battle/result.png");
            }
            else
            {
                ItemPageDisplayCount = 6;
            }
        }

        internal void SetResultData(BattlePlayerData[] partyPlayer, int exp, int money, Rom.Item[] items, Dictionary<Guid, int> itemDictonary)
        {
            var list = new List<CharacterLevelUpData>();

            foreach (var player in partyPlayer)
            {
                var data = new CharacterLevelUpData();

                data.player = player;

                data.startLevel = player.player.level;
                data.totalExp = player.player.exp;
                data.upLevel = 0;

                data.learnSkills = new List<string>();

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

            dropItemCountTable = new Dictionary<Rom.Item, int>();

            // 同じアイテムをまとめる
            foreach (var group in items.GroupBy(item => item))
            {
                dropItemCountTable.Add(group.Key, group.Count());
            }

            dropItemFirstGetTable = new Dictionary<Rom.Item, bool>();

            foreach (var item in dropItemCountTable.Keys)
            {
                dropItemFirstGetTable.Add(item, !itemDictonary.ContainsKey(item.guId));
            }

            foreach (var item in dropItemCountTable.Keys)
            {
                var icon = catalog.getItemFromGuid(item.icon.guId) as Common.Resource.Icon;

                if (icon != null && !itemIconTable.ContainsKey(item))
                {
                    var imageId = Graphics.LoadImage(icon.path);

                    itemIconTable.Add(item, imageId);
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

        public void ReleaseResourceData()
        {
            if (systemIconImageId >= 0) Graphics.UnloadImage(systemIconImageId);
            if (result3dImageId >= 0) Graphics.UnloadImage(result3dImageId);

            foreach (int iconImageId in itemIconTable.Values)
            {
                Graphics.UnloadImage(iconImageId);
            }

            itemIconTable.Clear();
        }

        public void Start()
        {
            resultWindowInfo = null;

            isResultEffectSkip = false;

            resultWindowSizeTweener.Begin(new Vector2(0, 0), new Vector2(560, 440), 15);

            ChangeResultState(ResultState.WindowOpen);
        }

        public void Update()
        {
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
            // 1. ウィンドウ表示
            // 2. リザルト内容表示
            // 3. レベルアップ処理
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
                    if (resultFrameCount >= 10 || isResultEffectSkip)
                    {
                        resultWindowInfo |= ResultWindowInfo.Item;

                        // 新規にアイテムスタックが増える場合、インベントリの空きをチェックする
                        while (processedDropItemIndex < itemPageCount * ItemPageDisplayCount &&
                            processedDropItemIndex < dropItemCountTable.Count)
                        {
                            // アイテムに空きがなかったら捨てるダイアログを表示する
                            var dropItem = dropItemCountTable.ElementAt(processedDropItemIndex);
                            if (owner.data.party.GetItemNum(dropItem.Key.guId) == 0 &&
                                owner.data.party.checkInventoryEmptyNum() <= 0)
                            {
                                owner.mapScene.ShowTrashWindow(dropItem.Key.guId,
                                    dropItemCountTable.ElementAt(processedDropItemIndex).Value);
                                ChangeResultState(ResultState.InventoryMax);
                            }
                            // 空きがあった場合は普通に加える
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
                            // 3Dバトルでレベルアップテキストもレベルアップ音もなしだった場合は経験値取得演出をスキップする
                            var gs = catalog.getGameSettings();
                            if (gs.battleType == Rom.GameSettings.BattleType.MODERN &&
                                Util.stringIsNullOrWhiteSpace(gs.glossary.battle_levelup) &&
                                gs.battleWinBgm == Guid.Empty)
                            {
                                ChangeResultState(ResultState.End);
                            }
                            else
                            {
                                ChangeResultState(ResultState.AddExp);
                            }
                        }
                        else
                        {
                            resultWindowInfo |= ResultWindowInfo.NextButton;

                            ChangeResultState(ResultState.ItemNextPage);
                        }
                    }
                    break;

                case ResultState.InventoryMax:
                    if(!owner.mapScene.IsTrashVisible())
                        ChangeResultState(ResultState.Item);
                    break;

                case ResultState.ItemNextPage:
                    itemNewIconAnimationFrameCount += GameMain.getRelativeParam60FPS();

                    if (itemNewIconAnimationFrameCount >= 30)
                    {
                        itemNewIconAnimationFrameCount = 0;
                        isItemNewIconVisible = !isItemNewIconVisible;
                    }

                    if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE))
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

                        cursolColor.setColor(new Color(Color.White, 255), new Color(Color.White, 0), 30);

                        ChangeResultState(ResultState.End);
                    }
                    else
                    {
                        int addExp = 1;

                        // 獲得した経験値が多いと表示に時間が掛かるので一度に加算する経験値を多くする
                        if (exp > 100)
                        {
                            var addExpCharacters = characterLevelUpData.Where(character => character.IsAddExp);

                            addExp = (int)(exp * 0.05f);    // 獲得した経験値の5%

                            addExp = Math.Min(addExp, exp - countupExp);    // 足していない残りEXP全て

                            addExp = Math.Min(addExp, (int)addExpCharacters.Select(character => character.NextLevelNeedExp * 0.05f).Min());             // 次のレベルアップまでに必要な経験値の5%
                            addExp = Math.Min(addExp, addExpCharacters.Select(character => character.NextLevelNeedExp - character.CurrentExp).Min());   // 次のレベルアップまでに必要な残り経験値

                            if (addExp <= 0) addExp = 1;
                        }

                        countupExp += addExp;

                        bool isLevelUp = false;

                        for (int index = 0; index < characterLevelUpData.Length; index++)
                        {
                            // 戦闘不能のメンバーには経験値を加算しない
                            // レベル上限に達していた場合も加算しない
                            if (!characterLevelUpData[index].IsAddExp) continue;

                            characterLevelUpData[index].totalExp += addExp;

                            // レベルアップ
                            if (characterLevelUpData[index].CurrentExp >= characterLevelUpData[index].NextLevelNeedExp)
                            {
                                isLevelUp = true;

                                if (!Util.stringIsNullOrWhiteSpace(catalog.getGameSettings().glossary.battle_levelup))
                                {
                                    characterLevelUpData[index].onLevelUpEffect = true;
                                }
                                characterLevelUpData[index].onLevelUpText = false;

                                characterLevelUpData[index].upLevel++;

                                characterLevelUpData[index].resultStatus.NextLevel = characterLevelUpData[index].startLevel + characterLevelUpData[index].upLevel;

                                // 取得できるスキルがあるか確認
                                // 一度表示したスキルは何度も表示しない
                                var learnSkills = characterLevelUpData[index].player.player.rom.skillLearnLevelsList.Where(skillInfo => !characterLevelUpData[index].displayedSkills.Contains(skillInfo.skill) && skillInfo.level == (characterLevelUpData[index].startLevel + characterLevelUpData[index].upLevel));

                                characterLevelUpData[index].learnSkills.Clear();

                                foreach (var skillInfo in learnSkills)
                                {
                                    var skill = catalog.getItemFromGuid(skillInfo.skill);

                                    if (skill != null)
                                    {
                                        characterLevelUpData[index].learnSkills.Add(skill.name);
                                    }
                                }

                                characterLevelUpData[index].displayedSkills.UnionWith(learnSkills.Select(skillInfo => skillInfo.skill));

                                characterLevelUpData[index].player.ChangeEmotion(Resource.Face.FaceType.FACE_SMILE);
                            }
                        }

                        if (isLevelUp)
                        {
                            levelupWindowSizeTweener.Begin(0, 1.0f, 15);

                            Audio.PlaySound(owner.se.levelup);

                            ChangeResultState(ResultState.LevelUpWindowOpen);
                        }
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
                    if (resultFrameCount >= 120 || Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE))
                    {
                        for (int index = 0; index < characterLevelUpData.Length; index++)
                        {
                            characterLevelUpData[index].onLevelUpText = false;
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

                        ChangeResultState(ResultState.AddExp);
                    }
                    break;

                case ResultState.End:
                    itemNewIconAnimationFrameCount += GameMain.getRelativeParam60FPS();

                    if (itemNewIconAnimationFrameCount >= 30)
                    {
                        itemNewIconAnimationFrameCount = 0;
                        isItemNewIconVisible = !isItemNewIconVisible;
                    }

                    cursolColor.update();
                    break;
            }

            for (int index = 0; index < characterLevelUpData.Length; index++)
            {
                if (characterLevelUpData[index].IsLevelMax)
                {
                    characterLevelUpData[index].resultStatus.GaugeParcent = 1.0f;
                }
                else
                {
                    // 小数点第2位で切り捨て
                    characterLevelUpData[index].resultStatus.GaugeParcent = (float)(Math.Floor(((double)characterLevelUpData[index].CurrentExp / characterLevelUpData[index].NextLevelNeedExp) * 100) / 100);
                }
            }

            // アイテムを捨てるウィンドウが重なってる時、リザルトを薄くするための機能
            if (resultState == ResultState.InventoryMax)
                alphaForProcessDropItem = Math.Max(0.25f, alphaForProcessDropItem * 0.9f);
            else
                alphaForProcessDropItem = 1 - (1 - alphaForProcessDropItem) * 0.9f;
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
                characterFaceImageDrawer.Draw(characterLevelUpData[index].player);

                // レベルアップ情報を表示
                if (characterLevelUpData[index].onLevelUpEffect)
                {
                    // レベルアップウィンドウ
                    Vector2 levelupWindowOriginalSize = new Vector2(192, 32);
                    Vector2 levelupWindowSize = levelupWindowOriginalSize;

                    levelupWindowSize.Y *= levelupWindowSizeTweener.CurrentValue;

                    var levelupWindowPosition = resultStatusDrawPosition[index] - new Vector2(0, levelupWindowSize.Y);

                    Vector2 skillWindowOffset = new Vector2(0, 40);

                    levelupWindowDrawer.Draw(levelupWindowPosition, levelupWindowSize);

                    if (characterLevelUpData[index].onLevelUpText) textDrawer.DrawString(glossary.battle_levelup, levelupWindowPosition, (int)levelupWindowSize.X, TextDrawer.HorizontalAlignment.Center, Color.Red);

                    // スキルウィンドウ
                    Vector2 skillWindowOriginalSize = new Vector2(192, 32);
                    Vector2 skillWindowPosition = levelupWindowPosition;

                    foreach (var skillName in characterLevelUpData[index].learnSkills)
                    {
                        const float TextScale = 0.85f;

                        // 習得したスキル名が長いときは途中で改行して表示する
                        // 最大2行まで表示する
                        var splitSkillNames = textDrawer.SplitString(skillName, (int)(levelupWindowSize.X - learnSkillWindowDrawer.paddingLeft - learnSkillWindowDrawer.paddingRight), TextScale);
                        int lineCount = splitSkillNames.Length;

                        lineCount = Math.Min(lineCount, 2);

                        Vector2 skillWindowSize = skillWindowOriginalSize;

                        skillWindowSize.Y *= (lineCount * levelupWindowSizeTweener.CurrentValue);

                        skillWindowPosition.Y -= skillWindowSize.Y;

                        learnSkillWindowDrawer.Draw(skillWindowPosition, skillWindowSize);

                        if (characterLevelUpData[index].onLevelUpText)
                        {
                            Vector2 textPosition = skillWindowPosition;

                            Vector2 textAreaSize = skillWindowSize;

                            textAreaSize.Y /= lineCount;

                            foreach (string str in splitSkillNames.Take(lineCount))
                            {
                                textDrawer.DrawString(str, textPosition, textAreaSize, TextDrawer.HorizontalAlignment.Center, TextDrawer.VerticalAlignment.Center, Color.White, TextScale);

                                textPosition.Y += 24;
                            }
                        }

                        skillWindowPosition -= skillWindowOffset;
                    }
                }

                var statusWindowColor = (characterLevelUpData[index].player.Status == StatusAilments.DOWN) ? Color.Red : Color.White;

                resultStatusWindowDrawer.Draw(characterLevelUpData[index].resultStatus, resultStatusDrawPosition[index], resultStatusWindowSize[index], statusWindowColor);
            }

            Vector2 windowPosition = (new Vector2(Graphics.ScreenWidth, Graphics.ScreenHeight) / 2) - (resultWindowSizeTweener.CurrentValue / 2);

            // リザルトウィンドウの下地を表示
            resultWindowDrawer.Draw(windowPosition, resultWindowSizeTweener.CurrentValue,
                new Color(alphaForProcessDropItem, alphaForProcessDropItem, alphaForProcessDropItem, alphaForProcessDropItem));

            // リザルトウィンドウの各項目を表示
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
                    textDrawer.DrawString(glossary.battle_result, windowPosition + new Vector2(8, 4), TextColor);
                }

                // EXP
                textDrawer.DrawString(glossary.exp, windowPosition + new Vector2(50, 40), TextColor);

                Graphics.DrawFillRect((int)windowPosition.X + 50, (int)windowPosition.Y + 70, 460, 1, LineColor.R, LineColor.G, LineColor.B, LineColor.A);

                if (resultWindowInfo.Value.HasFlag(ResultWindowInfo.Exp))
                {
                    textDrawer.DrawString(exp.ToString(), windowPosition + new Vector2(150, 40), 100, TextDrawer.HorizontalAlignment.Right, TextColor);
                }

                // Money
                textDrawer.DrawString(glossary.moneyName, windowPosition + new Vector2(50, 90), TextColor);

                Graphics.DrawFillRect((int)windowPosition.X + 50, (int)windowPosition.Y + 120, 460, 1, LineColor.R, LineColor.G, LineColor.B, LineColor.A);

                if (resultWindowInfo.Value.HasFlag(ResultWindowInfo.Money))
                {
                    textDrawer.DrawString(money.ToString(), windowPosition + new Vector2(150, 90), 100, TextDrawer.HorizontalAlignment.Right, TextColor);
                }

                // Item
                textDrawer.DrawString(glossary.item, windowPosition + new Vector2(50, 140), TextColor);

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
                            Graphics.DrawImage(systemIconImageId, (int)pos.X, (int)pos.Y,
                                new Rectangle(gameSettings.newItemIcon.x * Resource.Icon.ICON_WIDTH,
                                    gameSettings.newItemIcon.y * Resource.Icon.ICON_HEIGHT, Resource.Icon.ICON_WIDTH, Resource.Icon.ICON_HEIGHT));
                        }

                        pos.X += Resource.Icon.ICON_WIDTH;

                        const int ItemTextOffsetX = 460;
                        Vector2 textAreaSize = new Vector2(ItemTextOffsetX - Resource.Icon.ICON_WIDTH, Resource.Icon.ICON_HEIGHT);

                        string itemName = item.name;

                        // 同じアイテムを2個以上入手した場合は個数を表示して1行にまとめる
                        if (dropItemCountTable[item] > 1) itemName += string.Format(" ×{0}", dropItemCountTable[item]);

                        // アイテム名が長いとウィンドウからはみ出すので縮小して表示する
                        float scale = 1.0f;
                        float scaleX = 1.0f;

                        scaleX = textAreaSize.X / textDrawer.MeasureString(itemName).X;

                        if (scaleX < 1.0f) scale = scaleX;

                        textDrawer.DrawString(itemName, pos, textAreaSize, TextDrawer.HorizontalAlignment.Left, TextDrawer.VerticalAlignment.Center, TextColor, scale);

                        itemDataDrawPositionOffset.Y += textAreaSize.Y;

                        itemCount++;
                    }
                }

                if (resultWindowInfo.Value.HasFlag(ResultWindowInfo.NextButton) || resultWindowInfo.Value.HasFlag(ResultWindowInfo.CloseButton))
                {
                    Vector2 buttonPosition = windowPosition + new Vector2(ResultWindowSize.X * 0.25f, ResultWindowSize.Y * 0.875f);
                    Vector2 buttonSize = new Vector2(ResultWindowSize.X * 0.5f, ResultWindowSize.Y * 0.1f);

                    resultButtonDrawer.Draw(buttonPosition, buttonSize);
                    resultCursolDrawer.Draw(buttonPosition, buttonSize, cursolColor.getColor());

                    string text = "";

                    if (resultWindowInfo.Value.HasFlag(ResultWindowInfo.NextButton)) text = glossary.battle_result_continue;
                    else if (resultWindowInfo.Value.HasFlag(ResultWindowInfo.CloseButton)) text = glossary.close;

                    textDrawer.DrawString(text, buttonPosition, buttonSize, TextDrawer.HorizontalAlignment.Center, TextDrawer.VerticalAlignment.Center, TextColor);
#if WINDOWS
#else
                    if (this.owner.IsBattle2D == false)
                    {
                        if (UnityEngine.Input.GetMouseButtonDown(0)) clickCloseButton(InputCore.getTouchPos(0), buttonPosition, buttonSize);
                    }
#endif
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
                if (characterLevelUpData[index].onLevelUpEffect)
                {
                    // レベルアップウィンドウ
                    Vector2 levelupWindowOriginalSize = new Vector2(192, 32);
                    Vector2 levelupWindowSize = levelupWindowOriginalSize;

                    levelupWindowSize.Y *= levelupWindowSizeTweener.CurrentValue;

                    var levelupWindowPosition = characterLevelUpData[index].player.statusWindowDrawPosition - new Vector2(levelupWindowSize.X / 2, 0);

                    Vector2 skillWindowOffset = new Vector2(0, 40);

                    levelupWindowDrawer.Draw(levelupWindowPosition, levelupWindowSize);

                    if (characterLevelUpData[index].onLevelUpText) textDrawer.DrawString(glossary.battle_levelup, levelupWindowPosition, (int)levelupWindowSize.X, TextDrawer.HorizontalAlignment.Center, Color.Red);

                    // スキルウィンドウ
                    Vector2 skillWindowOriginalSize = new Vector2(192, 32);
                    Vector2 skillWindowPosition = levelupWindowPosition;

                    foreach (var skillName in characterLevelUpData[index].learnSkills)
                    {
                        const float TextScale = 0.85f;

                        // 習得したスキル名が長いときは途中で改行して表示する
                        // 最大2行まで表示する
                        var splitSkillNames = textDrawer.SplitString(skillName, (int)(levelupWindowSize.X - learnSkillWindowDrawer.paddingLeft - learnSkillWindowDrawer.paddingRight), TextScale);
                        int lineCount = splitSkillNames.Length;

                        lineCount = Math.Min(lineCount, 2);

                        Vector2 skillWindowSize = skillWindowOriginalSize;

                        skillWindowSize.Y *= (lineCount * levelupWindowSizeTweener.CurrentValue);

                        skillWindowPosition.Y -= skillWindowSize.Y;

                        learnSkillWindowDrawer.Draw(skillWindowPosition, skillWindowSize);

                        if (characterLevelUpData[index].onLevelUpText)
                        {
                            Vector2 textPosition = skillWindowPosition;

                            Vector2 textAreaSize = skillWindowSize;

                            textAreaSize.Y /= lineCount;

                            foreach (string str in splitSkillNames.Take(lineCount))
                            {
                                textDrawer.DrawString(str, textPosition, textAreaSize, TextDrawer.HorizontalAlignment.Center, TextDrawer.VerticalAlignment.Center, Color.White, TextScale);

                                textPosition.Y += 24;
                            }
                        }

                        skillWindowPosition -= skillWindowOffset;
                    }
                }

                //var statusWindowColor = (characterLevelUpData[index].player.Status == StatusAilments.DOWN) ? Color.Red : Color.White;

                //resultStatusWindowDrawer.Draw(characterLevelUpData[index].resultStatus, resultStatusDrawPosition[index], resultStatusWindowSize[index], statusWindowColor);
            }

            // リザルトウィンドウの各項目を表示
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
                    textDrawer.DrawString(glossary.battle_result, new Vector2(8, 0), TextColor);
                }

                // EXP
                textDrawer.DrawString(glossary.exp, new Vector2(32, 34), TextColor, 0.75f);

                if (resultWindowInfo.Value.HasFlag(ResultWindowInfo.Exp))
                {
                    textDrawer.DrawString(exp.ToString(), new Vector2(160, 34), 100, TextDrawer.HorizontalAlignment.Right, TextColor, 0.75f);
                }

                // Money
                textDrawer.DrawString(glossary.moneyName, new Vector2(480 + 32, 34), TextColor, 0.75f);

                if (resultWindowInfo.Value.HasFlag(ResultWindowInfo.Money))
                {
                    textDrawer.DrawString(money.ToString(), new Vector2(480 + 160, 34), 100, TextDrawer.HorizontalAlignment.Right, TextColor, 0.75f);
                }

                // Item
                textDrawer.DrawString(glossary.item, new Vector2(32, 66), TextColor, 0.75f);

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
                            Graphics.DrawImage(systemIconImageId, (int)pos.X, (int)pos.Y,
                                new Rectangle(gameSettings.newItemIcon.x * Resource.Icon.ICON_WIDTH,
                                    gameSettings.newItemIcon.y * Resource.Icon.ICON_HEIGHT, Resource.Icon.ICON_WIDTH, Resource.Icon.ICON_HEIGHT));
                        }

                        pos.X += Resource.Icon.ICON_WIDTH;

                        const int ItemTextOffsetX = 460;
                        Vector2 textAreaSize = new Vector2(ItemTextOffsetX - Resource.Icon.ICON_WIDTH, Resource.Icon.ICON_HEIGHT);

                        string itemName = item.name;

                        // 同じアイテムを2個以上入手した場合は個数を表示して1行にまとめる
                        if (dropItemCountTable[item] > 1) itemName += string.Format(" ×{0}", dropItemCountTable[item]);

                        // アイテム名が長いとウィンドウからはみ出すので縮小して表示する
                        float scale = 1.0f;
                        float scaleX = 1.0f;

                        scaleX = textAreaSize.X / textDrawer.MeasureString(itemName).X;

                        if (scaleX < 1.0f) scale = scaleX;

                        textDrawer.DrawString(itemName, pos, textAreaSize, TextDrawer.HorizontalAlignment.Left, TextDrawer.VerticalAlignment.Center, TextColor, scale);

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
    }
}

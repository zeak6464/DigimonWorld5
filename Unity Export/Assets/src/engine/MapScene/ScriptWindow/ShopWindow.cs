using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Yukar.Common.GameData;
using Yukar.Common;
using KeyStates = Yukar.Engine.Input.KeyStates;
using StateType = Yukar.Engine.Input.StateType;

namespace Yukar.Engine
{
    class ShopWindow
    {
        internal const int CHOICES_SELECTING = -1;
        internal const int FULL_WIDTH = 936;    // 増減表示も含めた大きさ
        internal const int WINDOW_WIDTH = 400;
        internal const int WINDOW_HEIGHT = 400;
        internal const int CONFIRM_WIDTH = 480;
        internal const int CONFIRM_HEIGHT = 112;
        internal const int DETAIL_WIDTH = FULL_WIDTH - WINDOW_WIDTH;
        internal const int DESC_HEIGHT = 64;
        internal const int TEXT_OFFSET = 16;
        internal const int SELECT_OFFSET = 12;
        internal const int LINE_HEIGHT = 32;
        internal const int CONFIRM_NUM_OFFSET = 80;
        internal const int SELL_NUM_OFFSET = 128;
        public MapScene parent;
        private WindowDrawer window;
        private WindowDrawer selBox;
        private TextDrawer textDrawer;
        private ShopWindow.ItemList itemList;
        internal Blinker blinker = new Blinker(Color.White, new Color(0.5f, 0.5f, 0.5f, 0.5f), 30);

        private Vector2 windowPos = new Vector2();
        private Vector2 windowSize = new Vector2();
        private Vector2 maxWindowSize = new Vector2();
        private Vector2 boxOffset = new Vector2();
        //private Vector2 selSize = new Vector2();

        private Vector2 descPos = new Vector2();
        private Vector2 maxDescSize = new Vector2();
        private Vector2 descSize = new Vector2();

        private Vector2 confirmPos = new Vector2();
        private Vector2 maxConfirmSize = new Vector2();
        private Vector2 confirmSize = new Vector2();

        private Vector2 detailPos = new Vector2();
        private Vector2 maxDetailSize = new Vector2();
        private Vector2 detailSize = new Vector2();

        private int nowSelected = 0;
        private int confirmNum = 1;
        private int maxConfirmNum = 99;
        private Common.Rom.Item[] items;
        private int[] numList;
        private enum ShopType
        {
            BUY,
            SELL,
        }
        private ShopType type;
        private Dictionary<Guid, int> iconDic = new Dictionary<Guid, int>();
        private Common.Rom.GameSettings gs;

        internal enum WindowState
        {
            HIDE_WINDOW,
            OPEN_SHOPMENU,
            SHOW_SHOPMENU,
            OPENING_WINDOW,
            CLOSING_WINDOW,
            SHOW_WINDOW,
            OPENING_CONFIRM_WINDOW,
            CLOSING_CONFIRM_WINDOW,
            SHOW_CONFIRM_WINDOW,
        }
        private WindowState windowState = WindowState.HIDE_WINDOW;
        public WindowState getWndowState() { return windowState; }
        float frame = 0;
        private Common.Rom.Item[] buyList;
        private int[] priceList;
        private int[] buyPriceList;
        private bool traded;
        private bool sellOnly;
        public int cursorImgId;

        private enum ConfilmTouchState
        {
            NONE,
            UP,
            DOWN,
            DECIDE,
            CANCEL,
        }
        private ConfilmTouchState confilmTouchState = ConfilmTouchState.NONE;

        internal void Initialize(
            Common.Resource.Window winRes, int winImgId,
            Common.Resource.Window selRes, int selImgId,
            Common.Resource.Window unselRes, int unselImgId,
            Common.Resource.Character scrollRes, int scrollImgId)
        {
            gs = parent.owner.catalog.getGameSettings();

            // ウィンドウの読み込み
            window = new WindowDrawer(winRes, winImgId);
            selBox = new WindowDrawer(selRes, selImgId);
            textDrawer = new TextDrawer(1);

            // 各種座標とサイズを設定
            maxWindowSize.X = WINDOW_WIDTH;
            maxWindowSize.Y = WINDOW_HEIGHT;
            windowPos.X = (Graphics.ViewportWidth + WINDOW_WIDTH - FULL_WIDTH) / 2;
            windowPos.Y = Graphics.ViewportHeight / 2;
            boxOffset.X = boxOffset.Y = TEXT_OFFSET - SELECT_OFFSET;
            //selSize.X = WINDOW_WIDTH - SELECT_OFFSET * 2;
            //selSize.Y = LINE_HEIGHT + SELECT_OFFSET / 2;
            itemList = new ShopWindow.ItemList();
            itemList.Initialize(parent.menuWindow.res,
                (int)windowPos.X, (int)windowPos.Y,
                (int)maxWindowSize.X, (int)maxWindowSize.Y);

            maxDetailSize.X = DETAIL_WIDTH;
            maxDetailSize.Y = WINDOW_HEIGHT;
            detailPos.X = (Graphics.ViewportWidth - DETAIL_WIDTH + FULL_WIDTH) / 2;
            detailPos.Y = Graphics.ViewportHeight / 2;

            maxDescSize.X = FULL_WIDTH;
            maxDescSize.Y = DESC_HEIGHT;
            descPos.X = Graphics.ViewportWidth / 2;
            descPos.Y = windowPos.Y + WINDOW_HEIGHT / 2 + DESC_HEIGHT / 2;

            maxConfirmSize.X = CONFIRM_WIDTH;
            maxConfirmSize.Y = CONFIRM_HEIGHT;
            confirmPos.X = Graphics.ViewportWidth / 2;
            confirmPos.Y = Graphics.ViewportHeight / 2;
            if (window.WindowResource != null)
            {
                var borderHeight = window.paddingTop + window.paddingBottom;
                maxConfirmSize.Y += borderHeight;
            }

            cursorImgId = Graphics.LoadImageDiv("updown.png", 2, 1);

        }

		public void Reset()
        {
        }

        internal void Show(Common.Rom.Item[] items, int[] prices)
        {
            this.buyList = items;
            this.buyPriceList = prices;

            windowState = WindowState.OPEN_SHOPMENU;
            frame = 0;

            parent.ShowMoneyWindow(true);
            traded = false;

            if (items.Length == 0)
                sellOnly = true;
        }

        internal void Update()
        {
            itemList.Update();
            blinker.update();

            switch (windowState)
            {
                case WindowState.OPEN_SHOPMENU:
                    if (!sellOnly)
                    {
                        var strs = new String[3];
                        strs[0] = gs.glossary.buy;
                        strs[1] = gs.glossary.sell;
                        strs[2] = gs.glossary.shopCancel;
                        parent.ShowChoices(strs);
                    }
                    else
                    {
                        var strs = new String[2];
                        strs[0] = gs.glossary.sell;
                        strs[1] = gs.glossary.shopCancel;
                        parent.ShowChoices(strs);
                    }
                    windowState = WindowState.SHOW_SHOPMENU;
                    break;
                case WindowState.SHOW_SHOPMENU: // 選択中
                    var result = parent.GetChoicesResult();
                    if (result != CHOICES_SELECTING && sellOnly)
                        result++;
                    switch (result)
                    {
                        case CHOICES_SELECTING:
                            if (Input.KeyTest(StateType.TRIGGER, KeyStates.CANCEL))
                            {
                                Audio.PlaySound(parent.owner.se.cancel);
                                parent.CloseChoices();
                                parent.ShowMoneyWindow(false);
                                windowState = WindowState.HIDE_WINDOW;
                            }
                            break;
                        case 0:
                            // インベントリがいっぱいの場合は購入リストに行けない
                            if (parent.owner.data.party.checkInventoryEmptyNum() == 0)
                            {
                                Audio.PlaySound(parent.owner.se.cancel);
                                parent.ShowToast(parent.owner.catalog.getGameSettings().glossary.inventoryFull);
                                frame = 0;
                                windowState = WindowState.OPEN_SHOPMENU;
                                break;
                            }

                            Audio.PlaySound(parent.owner.se.decide);
                            type = ShopType.BUY;
                            windowState = WindowState.OPENING_WINDOW;
                            nowSelected = 0;
                            items = buyList;
                            priceList = buyPriceList;
                            CreateDic();
                            frame = 0;
                            numList = null;
                            itemList.setList(items, iconDic, priceList, true);
                            itemList.Show();
                            break;
                        case 1:
                            Audio.PlaySound(parent.owner.se.decide);
                            type = ShopType.SELL;
                            windowState = WindowState.OPENING_WINDOW;
                            nowSelected = 0;
                            items = CreateSellList();
                            CreateDic();
                            frame = 0;
                            itemList.setList(items, iconDic, numList, false);
                            itemList.Show();
                            break;
                        case 2:
                            Audio.PlaySound(parent.owner.se.cancel);
                            windowState = WindowState.HIDE_WINDOW;
                            parent.ShowMoneyWindow(false);
                            break;
                    }
                    break;
                case WindowState.SHOW_WINDOW:
                    {
                        if (itemList.result == Util.RESULT_SELECTING)
                        {
                            nowSelected = itemList.selected;
                        }
                        else if (itemList.result == Util.RESULT_CANCEL)
                        {
                            itemList.Hide();
                            frame = 0;
                            windowState = WindowState.CLOSING_WINDOW;
                        }
                        else
                        {
                            bool cancelled = false;
                            if (type == ShopType.BUY && priceList[nowSelected] <= parent.owner.data.party.GetMoney())
                            {
                                frame = 0;
                                windowState = WindowState.OPENING_CONFIRM_WINDOW;
                                maxConfirmNum = items[nowSelected].maxNum;
                                if (priceList[nowSelected] != 0)
                                    maxConfirmNum = parent.owner.data.party.GetMoney() / priceList[nowSelected];
                                maxConfirmNum = Math.Min(items[nowSelected].maxNum - parent.owner.data.party.GetItemNum(items[nowSelected].guId), maxConfirmNum);
                                confirmNum = 1;
                            }
                            else if (type == ShopType.SELL && numList[nowSelected] > 0 && priceList[nowSelected] > 0)
                            {
                                frame = 0;
                                windowState = WindowState.OPENING_CONFIRM_WINDOW;
                                maxConfirmNum = numList[nowSelected];
                                confirmNum = 0;
                            }
                            else
                            {
                                Audio.PlaySound(parent.owner.se.cancel);
                                cancelled = true;
                            }

                            if (!cancelled)
                            {
                                Audio.PlaySound(parent.owner.se.decide);
                                itemList.Lock();
                            }
                            itemList.result = Util.RESULT_SELECTING;
                        }
                    }
                    break;
                case WindowState.SHOW_CONFIRM_WINDOW:
                    {
                        if (Input.KeyTest(StateType.REPEAT, KeyStates.UP))
                        {
                            Audio.PlaySound(parent.owner.se.select);
                            frame = 0;
                            confirmNum++;
                            confilmTouchState = ConfilmTouchState.NONE;
                        }
                        else if (Input.KeyTest(StateType.REPEAT, KeyStates.DOWN))
                        {
                            Audio.PlaySound(parent.owner.se.select);
                            frame = 0;
                            confirmNum--;
                            confilmTouchState = ConfilmTouchState.NONE;
                        }

                        if (confirmNum > maxConfirmNum)
                            confirmNum = 0;
                        else if (confirmNum < 0)
                            confirmNum = maxConfirmNum;

                        if (Input.KeyTest(StateType.TRIGGER, KeyStates.DECIDE) || confilmTouchState == ConfilmTouchState.CANCEL)
                        {
                            if (confilmTouchState == ConfilmTouchState.CANCEL)
                            {
                                confilmTouchState = ConfilmTouchState.NONE;
                                confirmNum = 0;
                            }
                            frame = 0;
                            windowState = WindowState.CLOSING_CONFIRM_WINDOW;
                            if (confirmNum > 0)
                            {
                                Audio.PlaySound(parent.owner.se.buy);
                                if (type == ShopType.BUY)
                                {
                                    // 購入処理
                                    parent.owner.data.party.AddMoney(-priceList[nowSelected] * confirmNum);
                                    parent.owner.data.party.AddItem(items[nowSelected].guId, confirmNum);
                                    parent.ShowToast(String.Format(gs.glossary.tradeComplete, items[nowSelected].name));
                                    traded = true;
                                }
                                else
                                {
                                    // 売却処理
                                    parent.owner.data.party.AddMoney((priceList[nowSelected] / 2) * confirmNum);
                                    parent.owner.data.party.AddItem(items[nowSelected].guId, -confirmNum);
                                    parent.ShowToast(String.Format(gs.glossary.sellComplete, items[nowSelected].name));
                                    items = CreateSellList();

                                    if (nowSelected >= items.Length)
                                    {
                                        nowSelected = items.Length - 1;
                                    }

                                    traded = true;
                                }
                            }
                            else
                            {
                                Audio.PlaySound(parent.owner.se.cancel);
                            }
                        }
                        else if (Input.KeyTest(StateType.TRIGGER, KeyStates.CANCEL))
                        {
                            Audio.PlaySound(parent.owner.se.cancel);
                            frame = 0;
                            windowState = WindowState.CLOSING_CONFIRM_WINDOW;
                        }
                    }
                    break;
                case WindowState.OPENING_CONFIRM_WINDOW:
                    if (frame >= MapScene.WINDOW_SHOW_FRAME)
                    {
                        windowState = WindowState.SHOW_CONFIRM_WINDOW;
                        frame = 0;
                        confirmSize.X = maxConfirmSize.X;
                        confirmSize.Y = maxConfirmSize.Y;
                    }
                    else
                    {
                        float delta = (float)frame / MapScene.WINDOW_SHOW_FRAME;
                        delta = 1 - (1 - delta) * (1 - delta);
                        confirmSize.X = (int)(maxConfirmSize.X * delta);
                        confirmSize.Y = (int)(maxConfirmSize.Y * delta);
                    }
                    break;
                case WindowState.CLOSING_CONFIRM_WINDOW:
                    if (frame >= MapScene.WINDOW_SHOW_FRAME)
                    {
                        windowState = WindowState.SHOW_WINDOW;
                        if (numList == null)
                            itemList.setList(items, iconDic, priceList, true);
                        else
                            itemList.setList(items, iconDic, numList, false);
                        itemList.Unlock();
                        frame = 0;

                        // 購入しようとしてインベントリがいっぱい or 売却しようとしてインベントリが空の場合は購入リストを閉じる
                        if ((parent.owner.data.party.checkInventoryEmptyNum() == 0 && type == ShopType.BUY) ||
                            (parent.owner.data.party.items.Count == 0 && type == ShopType.SELL))
                        {
                            itemList.Hide();
                            windowState = WindowState.CLOSING_WINDOW;
                            break;
                        }
                    }
                    else
                    {
                        float delta = 1 - (float)frame / MapScene.WINDOW_SHOW_FRAME;
                        delta = 1 - (1 - delta) * (1 - delta);
                        confirmSize.X = (int)(maxConfirmSize.X * delta);
                        confirmSize.Y = (int)(maxConfirmSize.Y * delta);
                    }
                    break;
                case WindowState.OPENING_WINDOW:
                    if (frame >= MapScene.WINDOW_SHOW_FRAME)
                    {
                        windowState = WindowState.SHOW_WINDOW;
                        frame = 0;

                        float delta = 1;
                        windowSize.X = (int)(maxWindowSize.X * delta);
                        windowSize.Y = (int)(maxWindowSize.Y * delta);

                        descSize.X = (int)(maxDescSize.X * delta);
                        descSize.Y = (int)(maxDescSize.Y * delta);

                        detailSize.X = (int)(maxDetailSize.X * delta);
                        detailSize.Y = (int)(maxDetailSize.Y * delta);
                    }
                    else
                    {
                        float delta = (float)frame / MapScene.WINDOW_SHOW_FRAME;
                        delta = 1 - (1 - delta) * (1 - delta);
                        windowSize.X = (int)(maxWindowSize.X * delta);
                        windowSize.Y = (int)(maxWindowSize.Y * delta);

                        descSize.X = (int)(maxDescSize.X * delta);
                        descSize.Y = (int)(maxDescSize.Y * delta);

                        detailSize.X = (int)(maxDetailSize.X * delta);
                        detailSize.Y = (int)(maxDetailSize.Y * delta);
                    }
                    break;
                case WindowState.CLOSING_WINDOW:
                    if (frame >= MapScene.WINDOW_SHOW_FRAME)
                    {
                        windowState = WindowState.OPEN_SHOPMENU;
                        frame = 0;
                    }
                    else
                    {
                        float delta = 1 - (float)frame / MapScene.WINDOW_SHOW_FRAME;
                        delta = 1 - (1 - delta) * (1 - delta);
                        windowSize.X = (int)(maxWindowSize.X * delta);
                        windowSize.Y = (int)(maxWindowSize.Y * delta);

                        descSize.X = (int)(maxDescSize.X * delta);
                        descSize.Y = (int)(maxDescSize.Y * delta);

                        detailSize.X = (int)(maxDetailSize.X * delta);
                        detailSize.Y = (int)(maxDetailSize.Y * delta);
                    }
                    break;
            }
            frame += GameMain.getRelativeParam60FPS();
        }

        private void CreateDic()
        {
            iconDic.Clear();

            foreach (var item in items)
            {
                if (iconDic.ContainsKey(item.icon.guId))
                    continue;

                var icon = parent.owner.catalog.getItemFromGuid(item.icon.guId) as Common.Resource.Icon;
                if (icon == null)
                    continue;

                var imgId = Graphics.LoadImage(icon.path, Common.Resource.Icon.ICON_WIDTH, Common.Resource.Icon.ICON_HEIGHT);
                iconDic.Add(icon.guId, imgId);
            }
        }

        private Common.Rom.Item[] CreateSellList()
        {
            int num = parent.owner.data.party.items.Count;
            var array = new Common.Rom.Item[num];
            numList = new int[num];
            priceList = new int[num];
            for (int i = 0; i < num; i++)
            {
                array[i] = parent.owner.data.party.items[i].item;
                numList[i] = parent.owner.data.party.items[i].num;
                priceList[i] = parent.owner.data.party.items[i].item.price;
            }

            if (array.Length == 0)
            {
                array = new Common.Rom.Item[1];
                array[0] = new Common.Rom.Item();
                array[0].name = gs.glossary.noItem;

                numList = new int[1];
                numList[0] = 0;
            }
            return array;
        }

        internal void Draw()
        {
            var pos = windowPos - windowSize / 2;
            var dPos = descPos - descSize / 2;
            var cPos = confirmPos - confirmSize / 2;
            var dtPos = detailPos - detailSize / 2;

            // 購入・売却ウィンドウ描画
            if (windowState >= WindowState.OPENING_WINDOW)
            {
                DrawMenuWindow(pos);
                DrawDescriptionWindow(dPos);
                DrawDetailWindow(dtPos);
            }

            // 購入・売却項目描画
            if (windowState >= WindowState.SHOW_WINDOW)
            {
                DrawMenu(pos);
                DrawDescription();
                var dtSize = detailSize;
                if (window.WindowResource != null)
                {
                    dtPos.X += window.paddingLeft;
                    dtPos.Y += window.paddingTop;
                    dtSize.X -= window.paddingLeft + window.paddingRight;
                    dtSize.Y -= window.paddingTop + window.paddingBottom;
                }
                DrawDetail(dtPos, dtSize);
            }

            // 確認ウィンドウ描画
            if (windowState >= WindowState.OPENING_CONFIRM_WINDOW)
            {
                DrawConfirmWindow(cPos);
            }

            // 確認ウィンドウ内容描画
            if (windowState >= WindowState.SHOW_CONFIRM_WINDOW)
            {
                var cSize = confirmSize;
                if (window.WindowResource != null)
                {
                    cPos.X += window.paddingLeft;
                    cPos.Y += window.paddingTop;
                    cSize.X -= window.paddingLeft + window.paddingRight;
                    cSize.Y -= window.paddingTop + window.paddingBottom;
                }
                DrawConfirm(cPos, cSize);
            }
        }

        private void DrawDetailWindow(Vector2 dtPos)
        {
            window.Draw(dtPos, detailSize);
        }

        private void DrawDetail(Vector2 dtPos, Vector2 dtSize)
        {
            if (itemList.returnSelected > 0)
                return;

            var str = "";
            var item = items[nowSelected];
            var party = parent.owner.data.party;

            // 基本性能の文章を作る
            switch (item.type)
            {
                case Common.Rom.ItemType.EXPENDABLE:
                    str = gs.glossary.expendableItem;
                    break;
                case Common.Rom.ItemType.EXPENDABLE_WITH_SKILL:
                    str = gs.glossary.expendableItem;
                    break;
                case Common.Rom.ItemType.WEAPON:
                    str = gs.glossary.weapon + "\n" + gs.glossary.attackPower + " : " + item.weapon.attack;
                    if (item.weapon.attrAttack > 0)
                    {
                        str += " + " + item.weapon.attrAttack + "\n" + gs.glossary.attrNames[item.weapon.attribute];
                    }
                    break;
                case Common.Rom.ItemType.ARMOR:
                    str = gs.glossary.armor + "\n" + gs.glossary.defense + " : " + item.equipable.defense;
                    break;
                case Common.Rom.ItemType.SHIELD:
                    str = gs.glossary.shield + "\n" + gs.glossary.defense + " : " + item.equipable.defense + "\n" + gs.glossary.evasion + " : " + item.equipable.evasion + "%";
                    break;
                case Common.Rom.ItemType.HEAD:
                    str = gs.glossary.head + "\n" + gs.glossary.defense + " : " + item.equipable.defense;
                    break;
                case Common.Rom.ItemType.ACCESSORY:
                    str = gs.glossary.accessory + "\n" + gs.glossary.defense + " : " + item.equipable.defense;
                    break;
            }

            // アイテムの基本性能を描画する
            var offset = Vector2.Zero;
            textDrawer.DrawString(str, dtPos + offset, Color.White);

            // 所持数を描画する
            int innerWidth = (int)dtSize.X;
            offset.X += DETAIL_WIDTH / 2;
            //offset = new Vector2(TEXT_OFFSET, WINDOW_HEIGHT - LINE_HEIGHT - TEXT_OFFSET);
            str = gs.glossary.holdNum + " : " + party.GetItemNum(item.guId);
            textDrawer.DrawString(str, dtPos + offset, Color.White);

            if (item.equipable == null)
                return;

            // 装備して能力が上がるか下がるか表示する
            for (int i = 0; i < party.members.Count; i++)
            {
                const int CONTENT_PADDING = 0;
                const int DETAIL_OFFSET_Y = 112;
                int lineHeight = (int)((dtSize.Y - DETAIL_OFFSET_Y) / 2) - CONTENT_PADDING;
                int lineWidth = innerWidth / 2 - CONTENT_PADDING;

                // 主人公キャラを描画する
                offset.X = 0;
                offset.Y = DETAIL_OFFSET_Y;
                if (i % 2 == 1)
                {
                    offset.X += lineWidth + CONTENT_PADDING;
                }
                if (i / 2 > 0)
                {
                    offset.Y += lineHeight + CONTENT_PADDING;
                }

                var pos = dtPos + offset;
                var size = new Vector2(128, lineHeight);
                const int NAME_HEIGHT = 28;
                const string TEXT_SPACE = "  ";
                var color = Color.White;
                var area = new Vector2();
                var hero = party.members[i];
                bool available = true;
                bool nowUsing = false;
                var paramNames = new string[] { gs.glossary.attackPower, gs.glossary.elementAttackPower };
                var paramNumbers = new int[] { 0, 0 };
                var paramEffectNumbers = new int[] { 0, 0 };

                // 装備可否などをチェック
                if (!hero.rom.availableItemsList.ContainsKey(item.guId) ||
                    hero.rom.availableItemsList[item.guId])
                {
                    int index = 0;

                    switch (item.type)
                    {
                        case Common.Rom.ItemType.WEAPON:
                            index = 0;
                            paramNumbers[0] = hero.equipmentEffect.attack + hero.power;
                            paramNumbers[1] = hero.equipmentEffect.elementAttack;
                            break;
                        case Common.Rom.ItemType.SHIELD:
                            index = 1;
                            paramNames[0] = gs.glossary.defense;
                            paramNames[1] = gs.glossary.evasion;
                            paramNumbers[0] = hero.equipmentEffect.defense + hero.vitality;
                            paramNumbers[1] = Math.Min(100, hero.equipmentEffect.evation);
                            break;
                        case Common.Rom.ItemType.HEAD:
                            index = 2;
                            paramNames = new string[] { gs.glossary.defense };
                            paramNumbers = new int[] { hero.equipmentEffect.defense + hero.vitality };
                            break;
                        case Common.Rom.ItemType.ARMOR:
                            index = 3;
                            paramNames = new string[] { gs.glossary.defense };
                            paramNumbers = new int[] { hero.equipmentEffect.defense + hero.vitality };
                            break;
                        case Common.Rom.ItemType.ACCESSORY:
                            index = 4;
                            paramNames = new string[] { gs.glossary.defense };
                            paramNumbers = new int[] { hero.equipmentEffect.defense + hero.vitality };
                            break;
                    }

                    if (hero.rom.fixEquipments[index])
                    {
                        available = false;
                        color = Color.Gray;
                    }
                    else if (hero.equipments[index] == item)
                    {
                        nowUsing = true;
                    }
                    else
                    {
                        // 変えてみてステータスを収集する
                        var backup = hero.equipments[index];
                        hero.equipments[index] = item;
                        hero.refreshEquipmentEffect(false);

                        if (item.type == Common.Rom.ItemType.WEAPON)
                        {
                            paramEffectNumbers[0] = hero.equipmentEffect.attack + hero.power;
                            paramEffectNumbers[1] = hero.equipmentEffect.elementAttack;
                        }
                        else
                        {
                            paramEffectNumbers[0] = hero.equipmentEffect.defense + hero.vitality;
                            paramEffectNumbers[1] = Math.Min(100, hero.equipmentEffect.evation);
                        }

                        // 元に戻す
                        hero.equipments[index] = backup;
                        hero.refreshEquipmentEffect(false);
                    }
                }
                else
                {
                    available = false;
                    color = Color.Gray;
                }

                // 名前枠
                pos = dtPos + offset;
                area.X = lineWidth;
                area.Y = NAME_HEIGHT;
                Graphics.DrawFillRect((int)pos.X, (int)pos.Y, (int)area.X, (int)area.Y, 32, 32, 32, 16);

                // 顔グラ
                pos = dtPos + offset;
                size = new Vector2(128, lineHeight);
                DrawChr(pos, size, parent.menuWindow.res.partyChars[i], 1.25f, color);

                // 名前
                pos = dtPos + offset;
                area.X = lineWidth;
                area.Y = NAME_HEIGHT;
                parent.menuWindow.res.textDrawer.DrawString(hero.rom.name + TEXT_SPACE, pos, area,
                    TextDrawer.HorizontalAlignment.Right,
                    TextDrawer.VerticalAlignment.Center, color, 0.75f);

                pos.X += lineWidth / 2 + CONTENT_PADDING;
                pos.Y += area.Y + CONTENT_PADDING;

                // 装備可能な時は効果を書く
                if (available)
                {
                    if (nowUsing)
                    {
                        parent.menuWindow.res.textDrawer.DrawString("    E", pos, Color.Green, 0.75f);
                    }
                    else
                    {
                        for (int j = 0; j < paramNames.Length; j++)
                        {
                            parent.menuWindow.res.textDrawer.DrawString(paramNames[j], pos, Color.White, 0.75f);
                            pos.Y += 20;
                            var numTxtSize = parent.menuWindow.res.textDrawer.MeasureString("" + paramNumbers[j]) * 0.75f;
                            parent.menuWindow.res.textDrawer.DrawString("" + paramNumbers[j], pos, Color.White, 0.75f);
                            pos.X += numTxtSize.X;
                            var color2 = Color.White;
                            if (paramNumbers[j] > paramEffectNumbers[j])
                                color2 = Color.LightPink;
                            else
                                color2 = Color.LightBlue;
                            parent.menuWindow.res.textDrawer.DrawString(" → " + paramEffectNumbers[j], pos, color2, 0.75f);
                            pos.X -= numTxtSize.X;
                            pos.Y += 20;
                        }
                    }
                }

                /*
                var hero = party.members[i];
                //DrawChr(dtPos + offset, partyChars[i], partyChars[i].getDirection());

                // 上下を判断する
                int dir = Util.DIR_SER_RIGHT;
                int left = 0;
                int right = 0;

                if (!hero.rom.availableItemsList.ContainsKey(item.guId) ||
                    hero.rom.availableItemsList[item.guId])
                {
                    switch (item.type)
                    {
                        case Common.Rom.ItemType.WEAPON:
                            if (hero.equipments[0] != null)
                                left = hero.equipments[0].weapon.attack + hero.equipments[0].weapon.attrAttack;
                            right = item.weapon.attack + item.weapon.attrAttack;
                            break;
                        case Common.Rom.ItemType.SHIELD:
                            if (hero.equipments[1] != null)
                                left = hero.equipments[1].equipable.defense;
                            right = item.equipable.defense;
                            break;
                        case Common.Rom.ItemType.HEAD:
                            if (hero.equipments[2] != null)
                                left = hero.equipments[2].equipable.defense;
                            right = item.equipable.defense;
                            break;
                        case Common.Rom.ItemType.ARMOR:
                            if (hero.equipments[3] != null)
                                left = hero.equipments[3].equipable.defense;
                            right = item.equipable.defense;
                            break;
                        case Common.Rom.ItemType.ACCESSORY:
                            if (hero.equipments[4] != null)
                                left = hero.equipments[4].equipable.defense;
                            right = item.equipable.defense;
                            break;
                    }

                    if (left > right)
                    {
                        dir = Util.DIR_SER_DOWN;
                    }
                    else if (left < right)
                    {
                        dir = Util.DIR_SER_UP;
                    }
                    else
                    {
                        dir = Util.DIR_SER_LEFT;
                    }
                }

                // 上下を描画する
                //offset.X += (DETAIL_WIDTH - DETAIL_OFFSET_X) / 4;
                //DrawChr(dtPos + offset, detailChr, dir);
                */
            }
        }

        internal void DrawChr(Vector2 pos, Vector2 area, CommonWindow.ImageInstance chr, float scale, Color color)
        {
            if (chr.imgId == -1)
                return;

            var dest = new Rectangle((int)pos.X, (int)pos.Y, (int)area.X, (int)area.Y);
            var src = new Rectangle(0, 0, (int)(area.X * scale), (int)(area.Y * scale));
            src.X = (Graphics.GetImageWidth(chr.imgId) - src.Width) / 2;
            src.Y = (Graphics.GetImageHeight(chr.imgId) - src.Height) / 3;

            Graphics.DrawImage(chr.imgId, dest, src, color);
        }

        private void DrawDescriptionWindow(Vector2 dPos)
        {
            window.Draw(dPos, descSize);
        }

        private void DrawDescription()
        {
            if (itemList.returnSelected > 0)
                return;

            var str = items[nowSelected].description;
            //var size = textDrawer.MeasureString(str);
            //textDrawer.DrawString(str, descPos - size / 2, Color.White);

            var area = new Vector2(descSize.X - TEXT_OFFSET * 2, descSize.Y);
            var pos = descPos - descSize / 2;
            pos.X += TEXT_OFFSET;
            var drawSize = textDrawer.MeasureString(str);
            float scale = Math.Min(1.0f, Math.Min(area.X / drawSize.X, descSize.Y / drawSize.Y));

            textDrawer.DrawString(str, pos, area,
                TextDrawer.HorizontalAlignment.Center, TextDrawer.VerticalAlignment.Center, Color.White, scale);
        }

        private void DrawConfirmWindow(Vector2 cPos)
        {
            window.Draw(cPos, confirmSize);
        }

        // 購入数表示の上下タップで数値変更
        private void ChangeConfirmNumByTouch(Vector2 pos, Vector2 size)
        {
#if WINDOWS
#else
            var cPos = confirmPos - confirmSize / 2;
            var cSize = confirmSize;
            var touchPos = InputCore.getTouchPos(0);
            var x0 = pos.X;
            var yBase = pos.Y + size.Y / 2;
            var x1 = pos.X + size.X;
            //UnityEngine.Debug.Log("touchPos : " + touchPos.x + " " + touchPos.y);
            if (x0 < touchPos.x && touchPos.x < x1 && cPos.Y - size.Y < touchPos.y && touchPos.y < yBase)
            {
                SharpKmyIO.Controller.repeatTouchDown = false;
                SharpKmyIO.Controller.repeatTouchUp = true;
            }
            else if (x0 < touchPos.x && touchPos.x < x1 && yBase < touchPos.y && touchPos.y < cPos.Y + cSize.Y)
            {
                SharpKmyIO.Controller.repeatTouchUp = false;
                SharpKmyIO.Controller.repeatTouchDown = true; //confilmTouchState = ConfilmTouchState.DOWN;
            }
            else if (touchPos.x < cPos.X || cPos.X + cSize.X < touchPos.x || touchPos.y < cPos.Y || cPos.Y + cSize.Y < touchPos.y)
            {
                confilmTouchState = ConfilmTouchState.CANCEL;
                SharpKmyIO.Controller.repeatTouchUp = false;
                SharpKmyIO.Controller.repeatTouchDown = false;
            }
#endif
        }

        private void DrawConfirm(Vector2 cPos, Vector2 cSize)
        {
            var item = items[nowSelected];
            int innerWidth = (int)cSize.X;

            // 購入数が変更できることがわかるように選択肢枠を描く
            var pos = cPos;
            pos.X += innerWidth - CONFIRM_NUM_OFFSET - TEXT_OFFSET;
            pos.Y += TEXT_OFFSET / 2;
            var size = new Vector2(CONFIRM_NUM_OFFSET, LINE_HEIGHT + TEXT_OFFSET);
            itemList.p.unselBox.Draw(pos, size);
            selBox.Draw(pos, size, blinker.getColor());

#if WINDOWS
#else
            // 上下矢印アイコンの描画
            if (maxConfirmNum != 0)
            {
                int imgWidth = Graphics.GetImageWidth(cursorImgId) / 2;
                int imgHeight = Graphics.GetImageHeight(cursorImgId);
                Graphics.DrawChipImage(cursorImgId, (int)pos.X + (int)size.X / 2 - imgWidth / 2, (int)pos.Y - imgHeight, 0, 0);
                Graphics.DrawChipImage(cursorImgId, (int)pos.X + (int)size.X / 2 - imgWidth / 2, (int)pos.Y + (int)size.Y, 1, 0);
            }

            // 購入数のタッチ選択
            if (UnityEngine.Input.GetMouseButton(0) && !GameMain.IsPushedAndroidBackButton()) ChangeConfirmNumByTouch(pos, size);
            if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                SharpKmyIO.Controller.repeatTouchUp = false;
                SharpKmyIO.Controller.repeatTouchDown = false;
            }
#endif

            // アイコンを描画
            pos = cPos;
            pos.X += TEXT_OFFSET;
            pos.Y += (LINE_HEIGHT - Common.Resource.Icon.ICON_HEIGHT) / 2 + TEXT_OFFSET;
            if (item.icon.guId != Guid.Empty)
            {
                Graphics.DrawChipImage(iconDic[item.icon.guId], (int)pos.X, (int)pos.Y, item.icon.x, item.icon.y);
            }

            // 品目を描画
            pos = cPos;
            pos.X += Common.Resource.Icon.ICON_WIDTH + boxOffset.X + TEXT_OFFSET;
            var str = item.name;
            size = textDrawer.MeasureString(str);
            pos.Y += (LINE_HEIGHT - size.Y) / 2 + TEXT_OFFSET;
            textDrawer.DrawString(str, pos, Color.White);

            // 購入数を描画
            pos = cPos;
            str = "x" + confirmNum;
            size = textDrawer.MeasureString(str);
            pos.X += innerWidth - size.X - TEXT_OFFSET * 2;
            pos.Y += (LINE_HEIGHT - size.Y) / 2 + TEXT_OFFSET;
            textDrawer.DrawString(str, pos, Color.White);

            // 価格を描画
            pos = cPos;
            var unit = gs.glossary.moneyName;
            int price = priceList[nowSelected];
            if (type == ShopType.SELL)
                price /= 2;
            price *= confirmNum;
            if (price > Common.GameData.Party.MAX_MONEY)
                price = Common.GameData.Party.MAX_MONEY;
            str = "" + price + unit;
            size = textDrawer.MeasureString(str);
            pos.X += innerWidth - size.X - TEXT_OFFSET * 2;
            pos.Y += CONFIRM_HEIGHT - size.Y - TEXT_OFFSET;
            textDrawer.DrawString(str, pos, Color.White);

            // スクロールマークを描画
            //if (confirmNum < maxConfirmNum)
            //    DrawScrollMark(confirmPos.X, confirmPos.Y - CONFIRM_HEIGHT / 2, upScrollChr);
            //if (confirmNum > 0)
            //    DrawScrollMark(confirmPos.X, confirmPos.Y + CONFIRM_HEIGHT / 2, downScrollChr);
        }

        private void DrawMenuWindow(Vector2 pos)
        {
            window.Draw(pos, windowSize);
        }

        internal class ItemList : PagedSelectWindow
        {
            string[] strs;
            bool[] flags;
            int[] nums;
            Common.Resource.Icon.Ref[] icons;

            internal Common.Rom.Item selectedItem;
            internal List<Common.GameData.Party.ItemStack> items;
            //private string prefix;
            //private string suffix;

            internal ItemList()
            {
                iconDic = new Dictionary<Guid, int>();
            }

            internal override void Show()
            {
                setColumnNum(1);
                setRowNum(5, true, 1);

                base.Show();
            }

            internal override void Hide()
            {
                base.Hide();
            }

            internal void CreateDic(List<Party.ItemStack> items)
            {
                foreach (var item in items)
                {
                    if (iconDic.ContainsKey(item.item.icon.guId))
                        continue;

                    var icon = p.owner.parent.owner.catalog.getItemFromGuid(item.item.icon.guId) as Common.Resource.Icon;
                    if (icon == null)
                        continue;

                    var imgId = Graphics.LoadImage(icon.path, Common.Resource.Icon.ICON_WIDTH, Common.Resource.Icon.ICON_HEIGHT);
                    iconDic.Add(icon.guId, imgId);
                }
            }

            internal void CreateDic(List<Common.GameData.Hero> members)
            {
                foreach (var hero in members)
                {
                    CreateDic(hero.equipments);
                }
            }

            private void CreateDic(Common.Rom.Item[] items)
            {
                foreach (var item in items)
                {
                    if (item == null || iconDic.ContainsKey(item.icon.guId))
                        continue;

                    var icon = p.owner.parent.owner.catalog.getItemFromGuid(item.icon.guId) as Common.Resource.Icon;
                    if (icon == null)
                        continue;

                    var imgId = Graphics.LoadImage(icon.path, Common.Resource.Icon.ICON_WIDTH, Common.Resource.Icon.ICON_HEIGHT);
                    iconDic.Add(icon.guId, imgId);
                }
            }

            internal override void UpdateCallback()
            {
                base.UpdateCallback();

                if (returnSelected > 0)
                {
                    selectedItem = null;
                }
                else if (items.Count > 0)
                {
                    selectedItem = items[selected].item;
                }
                else
                {
                    selectedItem = null;
                }
            }

            internal override void DrawCallback()
            {
                DrawSelect(strs, flags, nums, icons);
                DrawReturnBox();
            }

            internal void setList(Common.Rom.Item[] array, Dictionary<Guid, int> iconDic, int[] numArray, bool numIsPrice)
            {
                this.iconDic = iconDic;
                this.items = new List<Party.ItemStack>();
                int index = 0;
                if (numArray == null)
                {
                    //prefix = "";
                    //suffix = p.gs.glossary.moneyName;
                }
                else
                {
                    //prefix = "x";
                    //suffix = "";
                }
                foreach (var item in array)
                {
                    var stack = new Party.ItemStack();
                    stack.item = item;
                    if (numArray == null)
                    {
                        stack.num = item.price;
                    }
                    else
                    {
                        stack.num = numArray[index];
                    }
                    this.items.Add(stack);

                    index++;
                }

                if (items.Count > 0)
                {
                    maxItems = items.Count;
                    strs = new string[items.Count];
                    flags = new bool[items.Count];
                    nums = new int[items.Count];
                    icons = new Common.Resource.Icon.Ref[items.Count];

                    for (int i = 0; i < items.Count; i++)
                    {
                        var numStringLength = (int)(p.textDrawer.MeasureString(items[i].num.ToString()).X * NumScale);
                        strs[i] = p.textDrawer.GetContentText(items[i].item.name, innerWidth - Common.Resource.Icon.ICON_WIDTH * 2 - numStringLength, NameScale);
                        flags[i] = true;
                        if (numIsPrice)
                        {
                            if (p.owner.parent.owner.data.party.GetMoney() < numArray[i])
                                flags[i] = false;
                        }
                        else
                        {
                            if (items[i].item.price == 0)
                                flags[i] = false;
                        }
                        nums[i] = items[i].num;
                        icons[i] = items[i].item.icon;
                    }
                }
                else
                {
                    maxItems = 1;
                    strs = new string[] { p.gs.glossary.noItem };
                    flags = new bool[] { false };
                    nums = null;
                    icons = null;
                }
            }
        }

        private void DrawMenu(Vector2 pos)
        {
            itemList.Draw();

            /*
            Graphics.SetViewport(SELECT_OFFSET + (int)pos.X, SELECT_OFFSET + (int)pos.Y,
                WINDOW_WIDTH - SELECT_OFFSET * 2, WINDOW_HEIGHT - SELECT_OFFSET * 2);

            var origPos = Vector2.Zero;
            origPos.Y -= scrollPos;

            // 選択肢を描画
            pos = origPos;
            pos.Y += nowSelected * LINE_HEIGHT;
            selBox.Draw(pos, selSize);

            // アイコンを描画
            pos = origPos + boxOffset;
            pos.Y += (LINE_HEIGHT - Common.Resource.Icon.ICON_HEIGHT) / 2;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].icon.guId != Guid.Empty)
                {
                    byte color = 255;
                    if (type == ShopType.BUY && priceList[i] > parent.owner.data.party.money || type == ShopType.SELL && numList[i] == 0)
                        color = 127;
                    Graphics.DrawChipImage(iconDic[items[i].icon.guId], (int)pos.X, (int)pos.Y,
                        items[i].icon.x, items[i].icon.y, color, color, color, 255);
                }
                pos.Y += LINE_HEIGHT;
            }

            // 品目を描画
            pos = origPos + boxOffset;
            pos.X += Common.Resource.Icon.ICON_WIDTH + boxOffset.X;
            for (int i = 0; i < items.Length; i++)
            {
                Color color = Color.White;
                if (type == ShopType.BUY && priceList[i] > parent.owner.data.party.money || type == ShopType.SELL && numList[i] == 0)
                    color = Color.Gray;

                var str = items[i].name;
                var size = textDrawer.MeasureString(str);
                size.X = 0;
                size.Y = (LINE_HEIGHT - size.Y) / 2;
                textDrawer.DrawString(str, pos + size, color);
                pos.Y += LINE_HEIGHT;
            }

            // 個数を描画
            if (type == ShopType.SELL)
            {
                pos = origPos + boxOffset;
                pos.X += WINDOW_WIDTH - SELL_NUM_OFFSET - TEXT_OFFSET * 2;
                for (int i = 0; i < items.Length; i++)
                {
                    if (numList[i] == 0)
                        continue;

                    var str = "x" + numList[i];
                    var size = textDrawer.MeasureString(str);
                    size.Y = -(LINE_HEIGHT - size.Y) / 2;
                    textDrawer.DrawString(str, pos - size, Color.White);
                    pos.Y += LINE_HEIGHT;
                }
            }

            // 価格を描画
            var unit = gs.glossary.moneyName;
            pos = origPos + boxOffset;
            pos.X += WINDOW_WIDTH - TEXT_OFFSET * 2;
            for (int i = 0; i < items.Length; i++)
            {
                if (type == ShopType.SELL && numList[i] == 0)
                    continue;

                Color color = Color.White;
                if (type == ShopType.BUY && priceList[i] > parent.owner.data.party.money)
                    color = Color.Gray;

                int price = priceList[i];
                if (type == ShopType.SELL)
                    price /= 2;

                var str = "" + price + unit;
                var size = textDrawer.MeasureString(str);
                size.Y = -(LINE_HEIGHT - size.Y) / 2;
                textDrawer.DrawString(str, pos - size, color);
                pos.Y += LINE_HEIGHT;
            }

            Graphics.RestoreViewport();

            // スクロールマークを描画
            if (windowState == WindowState.SHOW_WINDOW)
            {
                if (scrollPos > 0)
                    DrawScrollMark(windowPos.X, windowPos.Y - WINDOW_HEIGHT / 2, upScrollChr);
                if (scrollPos + WINDOW_HEIGHT - SELECT_OFFSET * 2 < items.Length * LINE_HEIGHT)
                    DrawScrollMark(windowPos.X, windowPos.Y + WINDOW_HEIGHT / 2, downScrollChr);
            }
            */
        }

        private void DrawScrollMark(float x, float y, MapCharacter scrollChr)
        {
            /*
            var pos = new Vector2(x, y);
            int divW = Graphics.GetDivWidth(scrollChr.imgId);
            int divH = Graphics.GetDivHeight(scrollChr.imgId);
            Graphics.DrawChipImage(scrollChr.imgId,
                (int)pos.X - divW / 2,
                (int)pos.Y - divH / 2,
                scrollChr.nowFrame, scrollChr.nowDir);
             */
        }

        internal bool IsVisible()
        {
            return windowState != WindowState.HIDE_WINDOW;
        }

        internal bool GetTraded()
        {
            return traded;
        }
    }
}

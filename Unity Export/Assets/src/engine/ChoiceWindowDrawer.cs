using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Input;

using Yukar.Common.Resource;
#if WINDOWS
#else
using Eppy;
#endif

namespace Yukar.Engine
{
    public class ChoiceWindowDrawer
    {
        public enum ItemType
        {
            Item,
            Cancel,
        }

        public enum ItemSortOrientation
        {
            Scroll_Z,
            Scroll_MirrorN,

            PageTurn_Z,
            PageTurn_MirrorN,
        }

        public enum HeaderStyle
        {
            None,
        }

        public enum FooterStyle
        {
            None,
            InfoDescription,
            PageText,
        }

        private struct ChoiceItemData
        {
            public Icon.Ref iconRef;
            public int iconImageId;
            public string text;
            public string number;

            public bool enable;
        }

        GameMain owner;

        // Private Fields
        List<ChoiceItemData> choiceItemList;
        ItemSortOrientation itemSortOrientation;
        TextDrawer textDrawer;
        int currentSelectItemRow;
        int currentSelectItemColumn;
        int displayItemRow;
        int displayItemColumn;

        const string EllipsisText = "...";
        
        private static Dictionary<Tuple<string, BattleSequenceManager.BattleState>, int> recentBattleMenuSelected =
            new Dictionary<Tuple<string, BattleSequenceManager.BattleState>, int>();
        private Tuple<string, BattleSequenceManager.BattleState> lastTuple;

        // Properties
        public WindowDrawer BaseWindowDrawer { get; private set; }

        public int ChoiceItemCount { get { return choiceItemList.Count; } }
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
        public int ChoiceItemTopIndex { get { return 0; } }
        public int ChoiceItemLastIndex { get { return choiceItemList.Count - 1; } }

        public ItemType CurrentSelectItemType { get; private set; }
        public int CurrentSelectItemIndex { get { return RowColumnToIndex(currentSelectItemRow, currentSelectItemColumn); } }
        public bool CurrentSelectItemEnable { get { return choiceItemList[CurrentSelectItemIndex].enable; } }

        public WindowDrawer ChoiceItemBackgroundDrawer { get; set; }

        public Color EnableItemTextColor { get; set; }
        public Color DisableItemTextColor { get; set; }
        public float ItemTextScale { get; set; }
        public Vector2 ChoiceItemIconPositionOffset { get; set; }
        public Vector2 ChoiceItemTextPositionOffset { get; set; }
        public Vector2 ChoiceItemNumberPositionOffset { get; set; }

        public string ItemNoneText { get; set; }

        public int RowSpaceMarginPixel { get; set; }
        public int ColumnSpaceMarginPixel { get; set; }

        public WindowDrawer CursorDrawer { get; set; }
        public Color CursorColor { get; set; }

        public string CancelButtonText { get; set; }
        public int CancelButtonHeight { get; set; }

        public Vector2 TextAreaSize { get; set; }

        public int WindowFrameThickness { get; set; }

        public bool IsDisplayPageIcon;
        public int PageIconId { get; set; }
        public int PageIconMarginPixel { get; set; }

        private int CurrentPageCount { get { return CurrentSelectItemIndex / (RowCount * ColumnCount) + 1; } }
        private int TotalPageCount { get { return (ChoiceItemCount - 1) / (RowCount * ColumnCount) + 1; } }
        private int CurrentPageTopRowCount { get { return (CurrentPageCount - 1) * RowCount; } }
        private int CurrentPageLastRowCount { get { return Math.Min((CurrentPageCount * RowCount) - 1, ChoiceItemCount / ColumnCount - 1 + ((ChoiceItemCount % ColumnCount - 1 >= currentSelectItemColumn) ? 1 : 0)); } }

        public Icon.Ref HeaderTitleIcon { get; set; }
        public string HeaderTitleText { get; set; }

        public Icon.Ref FooterTitleIcon { get; set; }
        public string FooterTitleText { get; set; }
        public string FooterMainDescriptionText { get; set; }
        public string FooterSubDescriptionText { get; set; }

        public bool IsPlayItemSelectSe { get; set; }

        public bool decided = false;
        internal int marginRow = 0;
        internal int cursorImgId = -1;
        internal int imgWidth;
        internal int imgHeight;

        public ChoiceWindowDrawer(GameMain owner, WindowDrawer windowDrawer, TextDrawer textDrawer, int row, int column, int margin = 0)
        {
            this.owner = owner;

            this.BaseWindowDrawer = windowDrawer;
            this.textDrawer = textDrawer;

            marginRow = margin;
            RowCount = row;
            ColumnCount = column;

            choiceItemList = new List<ChoiceItemData>();

            currentSelectItemRow = 0;
            currentSelectItemColumn = 0;

            displayItemRow = 0;
            displayItemColumn = 0;

            RowSpaceMarginPixel = 0;
            ColumnSpaceMarginPixel = 0;

            itemSortOrientation = ItemSortOrientation.PageTurn_Z;

            ItemTextScale = 1.0f;

            CursorColor = Color.White;

            EnableItemTextColor = Color.White;
            DisableItemTextColor = Color.Gray;

            IsDisplayPageIcon = false;
            PageIconId = Graphics.LoadImage("./res/system/page_icons.png");

            cursorImgId = Graphics.LoadImage("./res/system/arrow.png");
            imgWidth = Graphics.GetImageWidth(cursorImgId);
            imgHeight = Graphics.GetImageHeight(cursorImgId);

            ItemNoneText = "";

            CurrentSelectItemType = ItemType.Item;

            IsPlayItemSelectSe = true;
        }

        public void Release()
        {
            if (PageIconId >= 0) Graphics.UnloadImage(PageIconId);
        }

        // 選択肢追加 (テキストのみ)
        public void AddChoiceData(string text, bool enable = true)
        {
            var item = new ChoiceItemData();

            item.text = text;
            item.enable = enable;

            choiceItemList.Add(item);
        }

        // 選択肢追加 (アイコン画像 + テキスト)
        public void AddChoiceData(int iconImageId, Icon.Ref iconRef, string text, bool enable = true)
        {
            var item = new ChoiceItemData();

            item.text = text;
            item.iconImageId = iconImageId;
            item.iconRef = iconRef;
            item.enable = enable;

            choiceItemList.Add(item);
        }

        // 選択肢追加 (テキスト + 数値)
        public void AddChoiceData(string text, int number, bool enable = true)
        {
            var item = new ChoiceItemData();

            item.text = text;
            item.number = number.ToString();
            item.enable = enable;

            choiceItemList.Add(item);
        }

        // 選択肢追加 (アイコン画像 + テキスト + 数値)
        public void AddChoiceData(int iconImageId, Icon.Ref iconRef, string text, int number, bool enable = true)
        {
            var item = new ChoiceItemData();

            item.iconImageId = iconImageId;
            item.iconRef = iconRef;
            item.text = text;
            item.number = number.ToString();
            item.enable = enable;

            choiceItemList.Add(item);
        }

        // 登録されている選択肢を消去する
        public void ClearChoiceListData()
        {
            choiceItemList.Clear();
        }

        public void Update()
        {
            #region ＜入力処理＞
            if (Input.KeyTest(Input.StateType.REPEAT, Input.KeyStates.LEFT))
            {
                switch (CurrentSelectItemType)
                {
                    case ItemType.Item: SelectPrevColumnItem(); break;
                }
            }
            if (Input.KeyTest(Input.StateType.REPEAT, Input.KeyStates.RIGHT))
            {
                switch (CurrentSelectItemType)
                {
                    case ItemType.Item: SelectNextColumnItem(); break;
                }
            }

            if (Input.KeyTest(Input.StateType.REPEAT, Input.KeyStates.UP))
            {
                switch (CurrentSelectItemType)
                {
                    case ItemType.Item: SelectPrevRowItem(); break;
                    case ItemType.Cancel:
                        currentSelectItemRow = CurrentPageLastRowCount;
                        CurrentSelectItemType = ItemType.Item;

                        Audio.PlaySound(owner.se.select);
                        break;
                }
            }
            if (Input.KeyTest(Input.StateType.REPEAT, Input.KeyStates.DOWN))
            {
                switch (CurrentSelectItemType)
                {
                    case ItemType.Item: SelectNextRowItem(); break;
                    case ItemType.Cancel:
                        currentSelectItemRow = CurrentPageTopRowCount;
                        CurrentSelectItemType = ItemType.Item;

                        Audio.PlaySound(owner.se.select);
                        break;
                }
            }
            #endregion

            //int currentSelectItemIndex = RowColumnToIndex(currentSelectItemRow, currentSelectItemColumn);

            // 行と列が上限や下限を超えていないかチェックする
            if (RowColumnToIndex(currentSelectItemRow, currentSelectItemColumn) < 0)
                IndexToRowColumn(0, out currentSelectItemRow, out currentSelectItemColumn);
            if (RowColumnToIndex(currentSelectItemRow, currentSelectItemColumn) > ChoiceItemLastIndex)
                IndexToRowColumn(ChoiceItemLastIndex, out currentSelectItemRow, out currentSelectItemColumn);

            switch (itemSortOrientation)
            {
                case ItemSortOrientation.Scroll_Z:
                    if (currentSelectItemRow < displayItemRow) displayItemRow = currentSelectItemRow;
                    if (currentSelectItemRow >= displayItemRow + RowCount) displayItemRow = currentSelectItemRow - (RowCount - 1);
                    break;

                case ItemSortOrientation.Scroll_MirrorN:
                    if (currentSelectItemColumn < displayItemColumn) displayItemColumn = currentSelectItemColumn;
                    if (currentSelectItemColumn >= displayItemColumn + ColumnCount) displayItemColumn = currentSelectItemColumn - (ColumnCount - 1);
                    break;

                case ItemSortOrientation.PageTurn_Z:
                    break;

                case ItemSortOrientation.PageTurn_MirrorN:
                    break;
            }

            if (ChoiceItemCount == 0)
            {
                CurrentSelectItemType = ItemType.Cancel;
            }
        }

        internal void clickSelect(SharpKmyMath.Vector2 touchPos, int index, Vector2 windowPosition, Vector2 drawPos, Vector2 itemSize)
        {
            var x0 = drawPos.X + windowPosition.X;
            var y0 = drawPos.Y + windowPosition.Y;
            if (x0 < touchPos.x && touchPos.x < x0 + itemSize.X && y0 < touchPos.y && touchPos.y < y0 + itemSize.Y)
            {
                if (index == RowColumnToIndex(currentSelectItemRow, currentSelectItemColumn))
                {
                    decided = true;
                    return;
                }
                SelectItem(index);
                CurrentSelectItemType = ItemType.Item;
                Audio.PlaySound(owner.se.select);
            }
        }

        internal void clickCancelSelect(SharpKmyMath.Vector2 touchPos, Vector2 windowPosition, Vector2 pos, Vector2 size)
        {
            var x0 = pos.X + windowPosition.X;
            var y0 = pos.Y + windowPosition.Y;
            if (x0 < touchPos.x && touchPos.x < x0 + size.X && y0 < touchPos.y && touchPos.y < y0 + size.Y)
            {
                if (CurrentSelectItemType == ItemType.Cancel) decided = true;
                CurrentSelectItemType = ItemType.Cancel;
                return;
            }
        }

        internal void clickPageCursor(SharpKmyMath.Vector2 touchPos, Vector2 windowPosition, Vector2 size)
        {
            var x0 = windowPosition.X;
            var y0 = windowPosition.Y + size.Y - imgHeight - GetFooterItemHeight();
            var x1 = x0 + size.X / 2;
            var y1 = y0 + imgHeight;
            var x2 = x1 + size.X / 2;
            if (x0 < touchPos.x && touchPos.x < x1 && y0 < touchPos.y && touchPos.y < y1)
            {
                SelectPrevColumnItem(2);
                Audio.PlaySound(owner.se.select);
            }
            else if (x1 < touchPos.x && touchPos.x < x2 && y0 < touchPos.y && touchPos.y < y1)
            {
                SelectNextColumnItem(2);
                Audio.PlaySound(owner.se.select);
            }
        }
            

        public void SelectItem(int itemIndex)
        {
            IndexToRowColumn(itemIndex, out currentSelectItemRow, out currentSelectItemColumn);
            
            //#24346 ページを考慮する
            int page = 0;
            if (itemIndex != 0)
            {
                page = itemIndex / (RowCount * ColumnCount);
            }
            currentSelectItemRow += RowCount * page;
        }

        public void SelectDefaultItem(BattleCharacterBase chr, BattleSequenceManager.BattleState state)
        {
            int index = ChoiceItemTopIndex;

            string digest = null;
            if (chr != null)
                digest = chr.getDigest();
            lastTuple = new Tuple<string, BattleSequenceManager.BattleState>(digest, state);
            if (owner.data.system.cursorPosition == Common.GameData.SystemData.CursorPosition.KEEP &&
                recentBattleMenuSelected.ContainsKey(lastTuple))
            {
                index = recentBattleMenuSelected[lastTuple];
            }

            IndexToRowColumn(index, out currentSelectItemRow, out currentSelectItemColumn);

            CurrentSelectItemType = ItemType.Item;
        }

        public void SelectTopItem()
        {
            IndexToRowColumn(ChoiceItemTopIndex, out currentSelectItemRow, out currentSelectItemColumn);

            CurrentSelectItemType = ItemType.Item;
        }

        public void SelectLastItem()
        {
            IndexToRowColumn(ChoiceItemLastIndex, out currentSelectItemRow, out currentSelectItemColumn);

            CurrentSelectItemType = ItemType.Item;
        }

        public void SelectPrevRowItem(int moveRowCount = 1)
        {
            int nextSelectItemRow = currentSelectItemRow - moveRowCount;

            if (nextSelectItemRow < CurrentPageTopRowCount)
            {
                if (!string.IsNullOrEmpty(CancelButtonText))
                {
                    CurrentSelectItemType = ItemType.Cancel;
                }
                else
                {
                    currentSelectItemRow = CurrentPageLastRowCount;
                }
            }
            else
            {
                currentSelectItemRow = nextSelectItemRow;
                CurrentSelectItemType = ItemType.Item;
            }

            Audio.PlaySound(owner.se.select);
        }

        public void SelectNextRowItem(int moveRowCount = 1)
        {
            int nextSelectItemRow = currentSelectItemRow + moveRowCount;

            // 現在のページの最終行 or 登録されている最後の項目の次の行を選択しようとしたら
            if (nextSelectItemRow > CurrentPageLastRowCount)
            {
                if (!string.IsNullOrEmpty(CancelButtonText))
                {
                    CurrentSelectItemType = ItemType.Cancel;
                }
                else
                {
                    currentSelectItemRow = CurrentPageTopRowCount;
                }
            }
            else
            {
                currentSelectItemRow = nextSelectItemRow;
                CurrentSelectItemType = ItemType.Item;
            }

            Audio.PlaySound(owner.se.select);
        }

        public void SelectPrevColumnItem(int moveColumnCount = 1)
        {
            int nextSelectItemColumn = currentSelectItemColumn - moveColumnCount;

            if (nextSelectItemColumn < 0)
            {
                if (CurrentPageCount > 1)   // 前のページに移動
                {
                    currentSelectItemRow -= RowCount;
                    currentSelectItemColumn = ColumnCount - 1;
                }
                else // 最後のページにループする
                {
                    // ループ後は最後のページの右端の項目を選択するが項目数によっては右端以外の項目を選択する場合もある

                    int lastPageItemCount = ChoiceItemCount % (ColumnCount * RowCount);

                    if (lastPageItemCount > 0 && lastPageItemCount < ColumnCount)    // 最後のページの行が1行なら存在している右端を選択する
                    {
                        currentSelectItemRow = (TotalPageCount - 1) * RowCount + ((ChoiceItemCount / ColumnCount) % RowCount);
                        currentSelectItemColumn = (ColumnCount - 1) - (ChoiceItemCount % ColumnCount);
                    }
                    else                                                    // 右端の一番下の行に移動
                    {
                        // 現在の行数か最後のページにある行数の小さい方にする
                        // 例 : 現在5行目, 最後のページにある行は3行 -> 移動後は3行目の右端を選択している
                        currentSelectItemRow = (TotalPageCount - 1) * RowCount + Math.Min((currentSelectItemRow % RowCount), ((ChoiceItemCount / ColumnCount) - 1) % RowCount);
                        currentSelectItemColumn = ColumnCount - 1;
                    }
                }
            }
            else // 前の列に移動
            {
                currentSelectItemColumn = nextSelectItemColumn;
                CurrentSelectItemType = ItemType.Item;
            }

            Audio.PlaySound(owner.se.select);
        }

        public void SelectNextColumnItem(int moveColumnCount = 1)
        {
            int nextSelectItemColumn = currentSelectItemColumn + moveColumnCount;

            if (CurrentSelectItemIndex == ChoiceItemLastIndex)  // 最後の項目
            {
                if (ChoiceItemCount % (RowCount * ColumnCount) == 1)  // ページに項目が1つだけなら最初のページに戻る
                {
                    currentSelectItemRow = 0;
                    currentSelectItemColumn = 0;
                }
                else if ((ChoiceItemCount % ColumnCount) == 0)    // 最後の行にぴったり項目が埋まっていたら最初のページに戻る
                {
                    currentSelectItemRow %= RowCount;
                    currentSelectItemColumn = 0;
                }
                else        // 同じページの最後の行から1つ手前を選択
                {
                    currentSelectItemRow = CurrentPageLastRowCount - 1;
                    currentSelectItemColumn = nextSelectItemColumn;
                }
            }
            else if (nextSelectItemColumn >= ColumnCount)   // ページの列を超えた
            {
                currentSelectItemColumn = 0;

                if (CurrentPageCount < TotalPageCount)      // 次のページに移動
                {
                    currentSelectItemRow = Math.Min(currentSelectItemRow + RowCount, ChoiceItemCount / ColumnCount + ((ChoiceItemCount % ColumnCount == 0) ? 0 : 1) - 1);
                }
                else                                        // 最初のページにループする
                {
                    currentSelectItemRow %= RowCount;
                    currentSelectItemColumn = 0;
                }

                if (CurrentSelectItemIndex >= ChoiceItemCount)
                {
                    currentSelectItemRow = ((ChoiceItemCount / ColumnCount + 1) - 1);
                }
            }
            else    // 次の列に移動
            {
                currentSelectItemColumn = nextSelectItemColumn;
                CurrentSelectItemType = ItemType.Item;
            }

            Audio.PlaySound(owner.se.select);
        }

        public void Draw(Vector2 windowPosition, Vector2 windowSize, HeaderStyle headerStyle = HeaderStyle.None, FooterStyle footerStyle = FooterStyle.None)
        {
            Graphics.SetViewport((int)(windowPosition.X), (int)(windowPosition.Y), (int)windowSize.X, (int)windowSize.Y);

            Vector2 size = windowSize;

            size.Y -= TextAreaSize.Y;

            // 下地のウィンドウを描画
            BaseWindowDrawer.Draw(Vector2.Zero, new Vector2(size.X, size.Y));

            var windowResource = BaseWindowDrawer.WindowResource;
            int w = (int)size.X;
            int h = (int)size.Y;
            if (windowResource != null)
            {
                w -= windowResource.left + windowResource.right;
                h -= windowResource.top + windowResource.bottom + GetFooterItemHeight();
            }
            Vector2 itemSize = new Vector2((int)(w / ColumnCount), (int)(h / (RowCount + marginRow)));

            if (marginRow > 0 && TotalPageCount > 1)
            {
                Graphics.DrawImage(cursorImgId, 0, (int)size.Y - imgHeight - GetFooterItemHeight());
                var srcRect = new Rectangle((int)size.X, (int)size.Y - imgHeight - GetFooterItemHeight(), -imgWidth, imgHeight);
                var destRect = new Rectangle(0, 0, imgWidth, imgHeight);
                Graphics.DrawImage(cursorImgId, srcRect, destRect);
#if WINDOWS
#else
                if (this.owner.IsBattle2D == false)
                {
                    if (UnityEngine.Input.GetMouseButtonDown(0)) clickPageCursor(InputCore.getTouchPos(0), windowPosition, size + new Vector2(0, BaseWindowDrawer.paddingTop));
                }
#endif
            }
            // 登録されている選択肢を描画
            foreach (int index in Enumerable.Range(0, RowCount * ColumnCount))
            {
                int displayItemIndex = index;

                switch (itemSortOrientation)
                {
                    case ItemSortOrientation.Scroll_Z:
                    case ItemSortOrientation.Scroll_MirrorN:
                        displayItemIndex += RowColumnToIndex(displayItemRow, displayItemColumn);
                        break;

                    case ItemSortOrientation.PageTurn_Z:
                    case ItemSortOrientation.PageTurn_MirrorN:
                        displayItemIndex += ((RowCount * ColumnCount) * (CurrentPageCount - 1));
                        break;
                }

                int itemWidth = (int)itemSize.X;
                int itemHeight = (int)itemSize.Y;

                if (ColumnCount > 1) itemWidth -= (ColumnSpaceMarginPixel * 2);
                if (RowCount > 1) itemHeight -= (RowSpaceMarginPixel * 2);

                int r, c;

                IndexToRowColumn(index, out r, out c);

                Vector2 itemDrawPosition = CalcChoiceItemPosition(r, c, size);

                if (displayItemIndex < 0 || (displayItemIndex >= choiceItemList.Count))
                {
                    if (ChoiceItemCount == 0 && index == 0)
                    {
                        if (ChoiceItemBackgroundDrawer != null)
                        {
                            ChoiceItemBackgroundDrawer.Draw(CalcChoiceItemPosition(index, size), new Vector2(itemWidth, itemHeight));
                        }

                        textDrawer.DrawString(ItemNoneText, itemDrawPosition + ChoiceItemTextPositionOffset, new Vector2(w / ColumnCount, h / (RowCount + marginRow)), TextDrawer.HorizontalAlignment.Left, TextDrawer.VerticalAlignment.Center, EnableItemTextColor, ItemTextScale);
                    }

                    continue;
                }
#if WINDOWS
#else
                if (this.owner.IsBattle2D == false)
                {
                    if (UnityEngine.Input.GetMouseButtonDown(0)) clickSelect(InputCore.getTouchPos(0), displayItemIndex, windowPosition, itemDrawPosition, new Vector2(itemWidth, itemHeight));
                }
#endif

                var displayItem = choiceItemList[displayItemIndex];

                // 各選択項目の背景を描画
                if (ChoiceItemBackgroundDrawer != null)
                {
                    ChoiceItemBackgroundDrawer.Draw(CalcChoiceItemPosition(index, size), new Vector2(itemWidth, itemHeight));
                }

                // カーソルを描画
                if (CursorDrawer != null && CurrentSelectItemType == ItemType.Item && choiceItemList.Count > 0 && displayItemIndex == CurrentSelectItemIndex)
                {
                    CursorDrawer.Draw(CalcChoiceItemPosition(index, size), new Vector2(itemWidth, itemHeight), CursorColor);
                }

                if (displayItem.iconRef != null)
                {
                    int iconDrawPositionX = (int)(itemDrawPosition.X + ChoiceItemIconPositionOffset.X);
                    int iconDrawPositionY = (int)(itemDrawPosition.Y + ChoiceItemIconPositionOffset.Y + (itemHeight - Icon.ICON_HEIGHT) / 2);

                    // System.Drawing.Rectangle => Microsoft.Xna.Framework.Rectangle
                    var rect = displayItem.iconRef.getRect();
                    var iconRect = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);

                    // アイコンを描画
                    Graphics.DrawImage(displayItem.iconImageId, iconDrawPositionX, iconDrawPositionY, iconRect);
                }

                Color textColor = displayItem.enable ? EnableItemTextColor : DisableItemTextColor;
                string itemText = textDrawer.GetContentText(displayItem.text, (int)size.X / ColumnCount - Common.Resource.Icon.ICON_WIDTH * 2, 1f);

                // テキストを描画
                textDrawer.DrawString(itemText, itemDrawPosition + ChoiceItemTextPositionOffset, new Vector2(itemWidth, itemHeight), TextDrawer.HorizontalAlignment.Left, TextDrawer.VerticalAlignment.Center, textColor, ItemTextScale);
                textDrawer.DrawString(displayItem.number, itemDrawPosition + ChoiceItemNumberPositionOffset, new Vector2(itemWidth, itemHeight), TextDrawer.HorizontalAlignment.Right, TextDrawer.VerticalAlignment.Center, textColor, ItemTextScale);
            }

            if (IsDisplayPageIcon && PageIconId >= 0 && TotalPageCount > 1)
            {
                for (int i = 1; i <= TotalPageCount; i++)
                {
                    var rect = new Rectangle();

                    rect.X = 0;
                    rect.Y = 0;
                    rect.Width = Graphics.GetImageWidth(PageIconId) / 2;
                    rect.Height = Graphics.GetImageHeight(PageIconId);

                    if (i != CurrentPageCount)
                    {
                        rect.X = Graphics.GetImageWidth(PageIconId) / 2;
                    }

                    int x = (int)(windowResource.left + (w / 2) + (rect.Width * (i - 1 - TotalPageCount / 2.0f)));
                    int y = (int)(size.Y - windowResource.bottom - GetFooterItemHeight() + PageIconMarginPixel);
                    Graphics.DrawImage(PageIconId, x, y, rect);
                }
            }

            if (!string.IsNullOrEmpty(CancelButtonText))
            {
                var cancelButtonSize = new Vector2(w / 2, CancelButtonHeight);
                var bottomSize = windowResource == null ? 0 : windowResource.bottom;
                var pos = new Vector2(size.X / 2 - cancelButtonSize.X / 2, size.Y - bottomSize - cancelButtonSize.Y);

                ChoiceItemBackgroundDrawer.Draw(pos, cancelButtonSize);

                if (CurrentSelectItemType == ItemType.Cancel)
                {
                    CursorDrawer.Draw(pos, cancelButtonSize, CursorColor);
                }
#if WINDOWS
#else
                if (this.owner.IsBattle2D == false)
                {
                    if (UnityEngine.Input.GetMouseButtonDown(0)) clickCancelSelect(InputCore.getTouchPos(0), windowPosition, pos, cancelButtonSize);
                }
#endif
                textDrawer.DrawString(CancelButtonText, pos, cancelButtonSize, TextDrawer.HorizontalAlignment.Center, TextDrawer.VerticalAlignment.Center, EnableItemTextColor);
            }

            BaseWindowDrawer.Draw(new Vector2(0, size.Y - WindowFrameThickness), TextAreaSize);

            // 説明文などのテキストとアイコンを描画
            //const float NewLineMarginY = 32;

            switch (footerStyle)
            {
                case FooterStyle.InfoDescription:
                    Vector2 footerTextOffset = Vector2.Zero;

                    if (!string.IsNullOrEmpty(FooterMainDescriptionText))
                    {
                        int leftMargin = windowResource == null ? 0 : windowResource.left;
                        int rightMargin = windowResource == null ? 0 : windowResource.right;
                        var area = new Vector2(windowSize.X - leftMargin - rightMargin, TextAreaSize.Y);
                        var pos = new Vector2(leftMargin, windowSize.Y - TextAreaSize.Y);
                        var drawSize = textDrawer.MeasureString(FooterMainDescriptionText);
                        float scale = Math.Min(1.0f, Math.Min(area.X / drawSize.X, area.Y / drawSize.Y));
                        textDrawer.DrawString(FooterMainDescriptionText, pos, area,
                            TextDrawer.HorizontalAlignment.Left, TextDrawer.VerticalAlignment.Center, Color.White, scale);
                        //foreach (string text in FooterMainDescriptionText.Replace(Environment.NewLine, "\n").Split("\n".ToCharArray()))
                        //{
                        //    int posX = windowResource == null ? 0 : windowResource.left;
                        //    int rightMargin = windowResource == null ? 0 : windowResource.right;

                        //    var drawSize = textDrawer.MeasureString(text);
                        //    var scale = (float)Math.Min(1.0, (windowSize.X - posX - rightMargin) / drawSize.X);

                        //    textDrawer.DrawString(text, new Vector2(posX, windowSize.Y - TextAreaSize.Y) + footerTextOffset, Color.White, scale);

                        //    footerTextOffset.Y += NewLineMarginY;
                        //}
                    }
                    break;

                case FooterStyle.PageText:
                    textDrawer.DrawString(string.Format("◀ {0}/{1} ▶", CurrentPageCount, TotalPageCount), new Vector2(windowResource.left, windowSize.Y - windowResource.bottom), w, TextDrawer.HorizontalAlignment.Center, Color.White);
                    break;
            }

            Graphics.RestoreViewport();
        }

        private int RowColumnToIndex(int row, int column)
        {
            int index = 0;

            switch (itemSortOrientation)
            {
                case ItemSortOrientation.Scroll_Z:
                    index = (row * ColumnCount + column);
                    break;
                
                case ItemSortOrientation.Scroll_MirrorN:
                    index = (column * RowCount + row);
                    break;

                case ItemSortOrientation.PageTurn_Z:
                    index = (row * ColumnCount) + (column);
                    break;

                case ItemSortOrientation.PageTurn_MirrorN:
                    index = (column * RowCount) + (row);
                    break;
            }

            return index;
        }

        private void IndexToRowColumn(int index, out int row, out int column)
        {
            row = 0;
            column = 0;

            switch (itemSortOrientation)
            {
                case ItemSortOrientation.Scroll_Z:
                    row = index / ColumnCount;
                    column = index % ColumnCount;
                    break;

                case ItemSortOrientation.Scroll_MirrorN:
                    row = index % RowCount;
                    column = index / RowCount;
                    break;

                case ItemSortOrientation.PageTurn_Z:
                    row = index % (RowCount * ColumnCount) / ColumnCount;
                    column = index % (RowCount * ColumnCount) % ColumnCount;
                    break;

                case ItemSortOrientation.PageTurn_MirrorN:
                    row = index % (RowCount * ColumnCount) % RowCount;
                    column = index % (RowCount * ColumnCount) / RowCount;
                    break;
            }
        }

        // 選択肢の描画位置を計算する
        private Vector2 CalcChoiceItemPosition(int index, Vector2 windowSize)
        {
            int row, column;

            IndexToRowColumn(index, out row, out column);

            return CalcChoiceItemPosition(row, column, windowSize);
        }
        private Vector2 CalcChoiceItemPosition(int row, int column, Vector2 windowSize)
        {
            Vector2 offset = Vector2.Zero;

            if (ColumnCount > 1) offset.X = ColumnSpaceMarginPixel;
            if (RowCount > 1) offset.Y = RowSpaceMarginPixel;

            var result = CalcChoiceItemAreaSize(row, column, windowSize) + offset;

            if (BaseWindowDrawer.WindowResource != null)
                result += new Vector2(BaseWindowDrawer.WindowResource.left, BaseWindowDrawer.WindowResource.top);

            return result;
        }

        // 選択肢の描画に必要なピクセル数を計算する
        private Vector2 CalcChoiceItemAreaSize(int index, Vector2 windowSize)
        {
            int row, column;

            IndexToRowColumn(index, out row, out column);

            return CalcChoiceItemAreaSize(row, column, windowSize);
        }
        private Vector2 CalcChoiceItemAreaSize(int row, int column, Vector2 windowSize)
        {
            int width = (int)windowSize.X;
            int height = (int)windowSize.Y;
            if (BaseWindowDrawer.WindowResource != null)
            {
                width -= BaseWindowDrawer.WindowResource.left + BaseWindowDrawer.WindowResource.right;
                height -= BaseWindowDrawer.WindowResource.top + BaseWindowDrawer.WindowResource.bottom + GetFooterItemHeight();
            }

            return new Vector2(column * (width / ColumnCount), row * (height / (RowCount + marginRow)));
        }

        // キャンセルボタンやページアイコンの表示に必要なピクセル数を取得する
        private int GetFooterItemHeight()
        {
            int height = 0;

            if (IsDisplayPageIcon && PageIconId >= 0)
            {
                height += height += Graphics.GetImageHeight(PageIconId);
                height += (PageIconMarginPixel * 2);
            }

            if (!string.IsNullOrEmpty(CancelButtonText)) height += CancelButtonHeight;

            return height;
        }

        internal void saveSelected()
        { 
            if(lastTuple != null)
            {
                Console.WriteLine("save " + CurrentSelectItemIndex);
                recentBattleMenuSelected[lastTuple] = CurrentSelectItemIndex;
            }
        }
    }
}

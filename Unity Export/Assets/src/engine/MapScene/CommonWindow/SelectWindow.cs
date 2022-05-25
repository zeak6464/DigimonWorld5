using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using KeyStates = Yukar.Engine.Input.KeyStates;
using StateType = Yukar.Engine.Input.StateType;
using Yukar.Common;
using Yukar.Common.GameData;

namespace Yukar.Engine
{
    internal class PagedSelectWindow : CommonWindow
    {
        private const string EllipsisText = "...";

        internal int maxItems;
        internal int selected = 0;
        internal int result = Util.RESULT_SELECTING;
        internal int columnNum = 1;
        internal int rowNum;
        internal int scrollPos = 0;
        internal int pageNum = 0;
        internal int maxPage = 0;
        internal int itemNumPerPage;
        internal int itemHeight = LINE_HEIGHT;
        internal int itemOffset = 0;
        internal string closeText;
        internal Dictionary<Guid, int> iconDic = null;
        internal Blinker blinker = new Blinker(Color.White, new Color(0.5f, 0.5f, 0.5f, 0.5f), 30);

        internal int returnSelected = 0;
        internal bool disableLeftAndRight = false;
        internal bool disableCancel = false;
        internal int pageMarkOffsetY = 0;

        internal int marginRow = 0;
        internal int cursorImgId = -1;
        internal int cursorImgWidth;
        internal int cursorImgHeight;

        protected static readonly float NameScale = 0.9f;
        protected static readonly float NumScale = 0.75f;

        internal override void UpdateCallback()
        {
            if (cursorImgId < 0)
            {
                cursorImgId = Graphics.LoadImage("./res/system/arrow.png");
                cursorImgWidth = Graphics.GetImageWidth(cursorImgId);
                cursorImgHeight = Graphics.GetImageHeight(cursorImgId);
            }

            upScrollVisible = scrollPos > 0;
            downScrollVisible = scrollPos + innerHeight < ((maxItems + columnNum - 1) / columnNum) * LINE_HEIGHT;
            ProcSelect();
            blinker.update();

#if WINDOWS
#else
            if (UnityEngine.Input.GetMouseButtonDown(0)) clickSelect(InputCore.getTouchPos(0));
            //if (InputCore.isTouchDown(0)) clickSelect(InputCore.getTouchPos(0));
#endif
        }

        internal override void DrawCallback()
        {
        }

        internal void ProcSelect(bool isMute = false)
        {
            if (itemNumPerPage > 0)
                maxPage = (int)Math.Ceiling((double)maxItems / itemNumPerPage);

            //var maxItemsEvenNum = (maxItems / columnNum) * columnNum;

            if (maxItems == 0)
                returnSelected = 1;

            if (maxItems > 0)
                ProcCursor(isMute);
            ProcDecideAndCancel(isMute);

            // 値を制限する
            if (selected >= maxItems)
                selected = maxItems - 1;
            if (selected < 0)
                selected = 0;

            // スクロールする
            if (itemNumPerPage > 0)
                pageNum = selected / itemNumPerPage;
            /*
            int selectedLine = (selected / columnNum);
            if (selectedLine * itemHeight < scrollPos)
            {
                int targetPos = selectedLine * itemHeight;
                scrollPos = (scrollPos + targetPos) / 2;
            }
            else if (selectedLine * itemHeight + itemHeight > scrollPos + innerHeight)
            {
                int targetPos = selectedLine * itemHeight + itemHeight - innerHeight;
                scrollPos = (scrollPos + targetPos) / 2;
            }
            */
#if WINDOWS
#else
            // Androidの戻るボタンが押されたら、メニューを全部閉じる
            if (GameMain.IsPushedAndroidBackButton())
            {
                result = Util.RESULT_CANCEL;
            }
#endif
        }

        private void ProcDecideAndCancel(bool isMute)
        {
            if (Input.KeyTest(StateType.TRIGGER, KeyStates.DECIDE))
            {
                if (returnSelected > 0)
                {
                    result = Util.RESULT_CANCEL;
                    if (!isMute) Audio.PlaySound(p.owner.parent.owner.se.cancel);
                }
                else
                {
                    result = selected;
                }
            }
            else if (Input.KeyTest(StateType.TRIGGER, KeyStates.CANCEL) && !disableCancel)
            {
                result = Util.RESULT_CANCEL;
                if (!isMute) Audio.PlaySound(p.owner.parent.owner.se.cancel);
            }

        }

        private void ProcCursor(bool isMute)
        {
            if (Input.KeyTest(StateType.REPEAT, KeyStates.UP))
            {
                if (returnSelected > 0)
                {
                    if (returnSelected == 1)
                    {
                        selected -= columnNum;
                        if (pageNum * itemNumPerPage > selected)
                        {
                            selected += itemNumPerPage;
                        }
                        while (selected >= maxItems)
                        {
                            selected -= columnNum;
                        }
                    }
                    returnSelected = 0;
                }
                else
                {
                    selected -= columnNum;
                    if (pageNum * itemNumPerPage > selected)
                    {
                        returnSelected = 1;
                        selected += columnNum;
                    }
                }

                if (!isMute) Audio.PlaySound(p.owner.parent.owner.se.select);
            }
            else if (Input.KeyTest(StateType.REPEAT, KeyStates.DOWN))
            {
                if (returnSelected > 0)
                {
                    if (returnSelected == 2)
                    {
                        selected += columnNum;
                        if (maxItems <= selected)
                        {
                            selected = pageNum * itemNumPerPage + selected % columnNum;
                        }
                        else if ((pageNum + 1) * itemNumPerPage <= selected)
                        {
                            selected -= itemNumPerPage;
                        }
                    }
                    returnSelected = 0;
                }
                else
                {
                    selected += columnNum;
                    if ((pageNum + 1) * itemNumPerPage <= selected || maxItems <= selected)
                    {
                        returnSelected = 2;
                        selected -= columnNum;
                    }
                }

                if (!isMute) Audio.PlaySound(p.owner.parent.owner.se.select);
            }
            else if (!disableLeftAndRight && Input.KeyTest(StateType.REPEAT, KeyStates.LEFT))
            {
                if (returnSelected > 0)
                {
                }
                else if (selected % columnNum == 0)
                {
                    selected -= itemNumPerPage - columnNum + 1;
                    if (selected < 0)
                        selected += maxPage * itemNumPerPage;
                }
                else
                {
                    selected--;
                }

                if (!isMute) Audio.PlaySound(p.owner.parent.owner.se.select);
            }
            else if (!disableLeftAndRight && Input.KeyTest(StateType.REPEAT, KeyStates.RIGHT))
            {
                if (returnSelected > 0)
                {
                }
                else if (selected % columnNum == columnNum - 1)
                {
                    selected += itemNumPerPage - columnNum + 1;
                    if (selected >= maxPage * itemNumPerPage)
                    {
                        selected -= maxPage * itemNumPerPage;
                    }
                    else if (selected >= maxItems)
                    {
                        selected = maxItems - columnNum + maxItems % columnNum;
                    }
                }
                else
                {
                    selected++;
                }

                if (!isMute) Audio.PlaySound(p.owner.parent.owner.se.select);
            }
        }

        internal void DrawSelect(string[] strs, bool[] flags, int[] nums = null, Common.Resource.Icon.Ref[] icons = null, int x = 0, int y = 0)
        {
            // メニューのコンテンツ描画
            var pos = new Vector2(x, y);
            var size = new Vector2(innerWidth / columnNum, itemHeight);
            int max = Math.Min((pageNum + 1) * itemNumPerPage, maxItems);
            for (int i = pageNum * itemNumPerPage; i < max; i++)
            {
                // 選択枠を表示
                pos.X++; pos.Y++;
                size.X -= 2; size.Y -= 2;
                if (!locked)
                    p.unselBox.Draw(pos, size);
                if (selected == i && returnSelected == 0)
                    p.selBox.Draw(pos, size, blinker.getColor());
                pos.X--; pos.Y--;
                size.X += 2; size.Y += 2;

                int num = nums == null ? -1 : nums[i];
                Common.Resource.Icon.Ref icon = icons == null ? null : icons[i];
                DrawMenuItem(i, pos, flags[i]);
                DrawMenuItem(strs[i], pos, flags[i], num, icon);

                if (i % columnNum == columnNum - 1)
                {
                    pos.X = x;
                    pos.Y += itemHeight + itemOffset;
                }
                else
                {
                    pos.X += innerWidth / columnNum;
                }
            }
            // セーブデータリストの最終段の高さ
            if (maxPage >= 2)
            {
                bool isOverWidth = innerWidth < (maxPage * Graphics.GetImageWidth(p.pageImgId) / 2);
                int pageMarkWidth = Graphics.GetImageWidth(p.pageImgId) / 2;

                // 下(ページアイコン)を書
                pos.X = (innerWidth - maxPage * pageMarkWidth) / 2;
                pos.Y = innerHeight - getReturnBoxHeight() - Graphics.GetImageHeight(p.pageImgId) - pageMarkOffsetY;

                if (isOverWidth)
                {
                    // カレントページより左
                    for (int i = 0; i < pageNum; i++)
                    {
                        pos.X = (innerWidth - pageMarkWidth) * i / maxPage;
                        Graphics.DrawImage(p.pageImgId, (int)pos.X, (int)pos.Y,
                            new Rectangle(pageMarkWidth, 0, pageMarkWidth, Graphics.GetImageHeight(p.pageImgId)));
                    }

                    // カレントページより右
                    for (int i = maxPage - 1; i > pageNum; i--)
                    {
                        pos.X = (innerWidth - pageMarkWidth) * i / maxPage;
                        Graphics.DrawImage(p.pageImgId, (int)pos.X, (int)pos.Y,
                            new Rectangle(pageMarkWidth, 0, pageMarkWidth, Graphics.GetImageHeight(p.pageImgId)));
                    }

                    // カレントページ
                    pos.X = (innerWidth - pageMarkWidth) * pageNum / maxPage;
                    Graphics.DrawImage(p.pageImgId, (int)pos.X, (int)pos.Y,
                        new Rectangle(0, 0, pageMarkWidth, Graphics.GetImageHeight(p.pageImgId)));
                }
                else
                {
                    // ページ数がウィンドウ幅より小さい
                    for (int i = 0; i < maxPage; i++)
                    {
                        int imageSourceX = pageMarkWidth;
                        if (i == pageNum)
                            imageSourceX = 0;
                        Graphics.DrawImage(p.pageImgId, (int)pos.X, (int)pos.Y,
                            new Rectangle(imageSourceX, 0, pageMarkWidth, Graphics.GetImageHeight(p.pageImgId)));
                        pos.X += pageMarkWidth;
                    }
                }

                if (marginRow > 0)
                {
                    int buttonY = (int)getPageButtonPosY();
                    int lowestY = itemHeight * itemNumPerPage / columnNum;
                    if (buttonY < lowestY)
                        buttonY = lowestY;
                    Graphics.DrawImage(cursorImgId, 0, buttonY);
                    var destRect = new Rectangle(innerWidth, buttonY, -cursorImgWidth, cursorImgHeight);
                    var srcRect = new Rectangle(0, 0, cursorImgWidth, cursorImgHeight);
                    Graphics.DrawImage(cursorImgId, destRect, srcRect);
                }
            }
        }

        internal void clickSelect(SharpKmyMath.Vector2 touchPos)
        {
            var offsetX = (int)windowPos.X - innerWidth / 2;
            var offsetY = (int)windowPos.Y - innerHeight / 2;
            var boxPos = new Vector2(0, 0);
            var boxSize = new Vector2(innerWidth / columnNum, itemHeight);
            int max = Math.Min((pageNum + 1) * itemNumPerPage, maxItems);

            //UnityEngine.Debug.Log("touchPos" + touchPos.x + " " + touchPos.y);
            //UnityEngine.Debug.Log("window : " + windowPos.X + " " + windowPos.Y + " " + innerWidth + " " + innerHeight);
            //UnityEngine.Debug.Log("calc : " + (windowPos.X - innerWidth/2) + " " + (windowPos.Y - innerHeight/2));
            //UnityEngine.Debug.Log("psw : " + MenuController.state);
            // やめるボタン
            var size = new Vector2(innerWidth / columnNum, getReturnBoxHeight());
            var pos = new Vector2((innerWidth - size.X) / 2 + offsetX, innerHeight - size.Y + offsetY);
            if (pos.X < touchPos.x && touchPos.x < pos.X + size.X && pos.Y < touchPos.y && touchPos.y < pos.Y + size.Y)
            {
                if (returnSelected > 0)
                {
                    result = Util.RESULT_CANCEL;
                    Audio.PlaySound(p.owner.parent.owner.se.cancel);
                    return;
                }
                else
                {
                    returnSelected = 1;
                    Audio.PlaySound(p.owner.parent.owner.se.select);
                    return;
                }
            }
            // ページ遷移
            if (maxPage >= 2)
            {
                pos.Y = getPageButtonPosY() + offsetY;
                if (pos.Y < touchPos.y && touchPos.y < pos.Y + Graphics.GetImageHeight(p.pageImgId) + cursorImgHeight)
                {
                    if (offsetX < touchPos.x && touchPos.x < (int)windowPos.X)
                    {
                        selected -= itemNumPerPage;
                        if (selected < 0) selected += maxPage * itemNumPerPage;
                        Audio.PlaySound(p.owner.parent.owner.se.select);
                        return;
                    }
                    else if ((int)windowPos.X < touchPos.x && touchPos.x < offsetX + innerWidth)
                    {
                        selected += itemNumPerPage;
                        if (selected >= maxPage * itemNumPerPage)
                        {
                            selected -= maxPage * itemNumPerPage;
                        }
                        else if (selected >= maxItems)
                        {
                            selected = maxItems - columnNum + maxItems % columnNum;
                        }
                        Audio.PlaySound(p.owner.parent.owner.se.select);
                        return;
                    }
                }
            }
            // それ以外
            for (int i = pageNum * itemNumPerPage; i < max; i++)
            {
                if (i % columnNum == 0)
                {
                    boxPos.X = 0;
                    // Configの時のみitemOffset = 16
                    boxPos.Y = (itemHeight + itemOffset) * (i % itemNumPerPage) / columnNum;
                    if (MenuController.state == MenuController.State.EQUIPLIST) boxPos.Y += 36; // 暫定
                }
                else boxPos.X += boxSize.X;
                var x0 = boxPos.X + offsetX;
                var x1 = boxPos.X + offsetX + boxSize.X;
                var y0 = offsetY + boxPos.Y;
                var y1 = offsetY + boxPos.Y + boxSize.Y;
                //UnityEngine.Debug.Log(i + " " + x0 + " " + y0 + " " + x1 + " " + y1);
                if (x0 < touchPos.x && touchPos.x < x1 && y0 < touchPos.y && touchPos.y < y1)
                {
                    if (returnSelected > 0)
                    {
                        returnSelected = 0;
                        selected = i;
                        Audio.PlaySound(p.owner.parent.owner.se.select);
                    }
                    else
                    {
                        if (selected == i)
                        {
                            result = selected;
                            break;
                        }
                        else
                        {
                            selected = i;
                            Audio.PlaySound(p.owner.parent.owner.se.select);
                            break;
                        }
                    }
                }
            }
        }

        private float getPageButtonPosY()
        {
            return innerHeight - getReturnBoxHeight() - Graphics.GetImageHeight(p.pageImgId) - pageMarkOffsetY - cursorImgHeight;
        }

        public List<Vector2> getReturnBoxRect()
        {
            var size = new Vector2(innerWidth / columnNum, getReturnBoxHeight());
            var pos = new Vector2((innerWidth - size.X) / 2, innerHeight - size.Y);
            var ret = new List<Vector2>();
            ret.Add(pos);
            ret.Add(size);
            return ret;
        }

        internal void DrawReturnBox(bool isConfig = false)
        {
            // やめるボタン
            var rect = getReturnBoxRect();
            var pos = new Vector2(rect[0].X, rect[0].Y);
            var size = new Vector2(rect[1].X, rect[1].Y);
            if (!locked)
                p.unselBox.Draw(pos, size);
            if (returnSelected > 0)
                p.selBox.Draw(pos, size, blinker.getColor());
            if (isConfig) closeText = p.gs.glossary.close;
            p.textDrawer.DrawString(closeText, pos, size, TextDrawer.HorizontalAlignment.Center, TextDrawer.VerticalAlignment.Center, Color.White, 0.9f);
        }

        internal void setColumnNum(int num)
        {
            columnNum = num;
            pageNum = 0;
            itemNumPerPage = columnNum * rowNum;
        }
        internal void setRowNum(int num, bool useReturnButton, int margin = 0)
        {
            rowNum = num;
            marginRow = margin;
            itemHeight = innerHeight / (rowNum + marginRow);
            pageNum = 0;
            itemNumPerPage = columnNum * rowNum;

            if (useReturnButton)
                itemHeight -= (getReturnBoxHeight() + 20) / num;

            // Shop, Equip, Skill, Itemはページ遷移分のスペースを足す(marginRow>=1)
        }

        internal int getReturnBoxHeight()
        {
            return Math.Min(itemHeight, LINE_HEIGHT);
        }

        internal override void Show()
        {
            if (!locked && p.owner.parent.owner.data.system.cursorPosition == SystemData.CursorPosition.NOT_KEEP)
                selected = 0;

            base.Show();
            returnSelected = 0;
            closeText = p.gs.glossary.battle_cancel;
        }

        internal override void Hide()
        {
            base.Hide();
            result = Util.RESULT_SELECTING;
        }

        internal virtual void DrawMenuItem(int index, Vector2 pos, bool enabled)
        {
            pos.Y -= scrollPos;
        }

        internal void DrawMenuItem(String str, Vector2 pos, bool enabled = true, int num = -1, Common.Resource.Icon.Ref icon = null)
        {
            pos.Y -= scrollPos;
            var origPos = pos;
            var textColor = enabled ? Color.White : Color.Gray;

            // アイコン
            int iconOffset = (itemHeight - Common.Resource.Icon.ICON_WIDTH) / 2;
            if (icon != null)
            {
                pos.X += iconOffset;
                pos.Y += iconOffset;
                if (iconDic.ContainsKey(icon.guId))
                {
                    Graphics.DrawChipImage(iconDic[icon.guId], (int)pos.X, (int)pos.Y, icon.x, icon.y);
                }
                pos.X += Common.Resource.Icon.ICON_WIDTH;
                pos.Y -= iconOffset;
            }

            pos.X += iconOffset / 2;

            // 名前
            p.textDrawer.DrawString(str, pos, itemHeight, TextDrawer.VerticalAlignment.Center, textColor, NameScale);

            // 個数
            if (num >= 0)
            {
                var numStr = "" + num;
                var size = p.textDrawer.MeasureString("" + numStr) * NumScale;
                pos = origPos;
                pos.X += innerWidth / columnNum - size.X - TEXT_OFFSET / 2;
                p.textDrawer.DrawString(numStr, pos, itemHeight, TextDrawer.VerticalAlignment.Center, textColor, NumScale);
            }
        }

        internal string GetContentText(string text)
        {
            const int MAX_NAME_WIDTH = 256;
            float ellipsisLength = p.textDrawer.MeasureString(EllipsisText).X;

            if (p.textDrawer.MeasureString(text).X < MAX_NAME_WIDTH)
                return text;

            var result = text;
            while (p.textDrawer.MeasureString(result).X > MAX_NAME_WIDTH - ellipsisLength)
            {
                result = result.Remove(result.Length - 1);
            }
            return result + EllipsisText;
        }
    }

    internal class SelectWindow : CommonWindow
    {
        internal int maxItems;
        internal int selected = 0;
        internal int result = Util.RESULT_SELECTING;
        internal int columnNum = 1;
        internal int scrollPos = 0;
        internal int itemHeight = LINE_HEIGHT;
        internal Dictionary<Guid, int> iconDic = null;
        internal Blinker blinker = new Blinker(Color.White, new Color(0.5f, 0.5f, 0.5f, 0.5f), 30);

        internal bool disableUpDown;
        internal bool disableLeftRight;

        //internal InputCore inputCore = new InputCore();

        internal override void UpdateCallback()
        {
            upScrollVisible = scrollPos > 0;
            downScrollVisible = scrollPos + innerHeight < ((maxItems + columnNum - 1) / columnNum) * LINE_HEIGHT;

            ProcSelect();
            blinker.update();
#if WINDOWS
#else
            if (UnityEngine.Input.GetMouseButtonDown(0)) clickSelect(InputCore.getTouchPos(0));
            //if (InputCore.isTouchDown(0)) clickSelect(InputCore.getTouchPos(0));
#endif       
        }

        internal override void DrawCallback()
        {
        }

        internal void ProcSelect()
        {
            var maxItemsEvenNum = (maxItems / columnNum) * columnNum;

            if (Input.KeyTest(StateType.REPEAT, KeyStates.UP) && !disableUpDown)
            {
                selected -= columnNum;
                if (selected < 0)
                {
                    selected += maxItemsEvenNum;
                    if (selected + columnNum < maxItems)
                    {
                        selected += columnNum;
                    }
                }
                Audio.PlaySound(p.owner.parent.owner.se.select);
            }
            else if (Input.KeyTest(StateType.REPEAT, KeyStates.DOWN) && !disableUpDown)
            {
                selected += columnNum;
                if (selected >= maxItems)
                {
                    selected -= maxItemsEvenNum;
                    if (selected - columnNum >= 0)
                    {
                        selected -= columnNum;
                    }
                }
                Audio.PlaySound(p.owner.parent.owner.se.select);
            }
            else if (Input.KeyTest(StateType.REPEAT, KeyStates.LEFT) && !disableLeftRight)
            {
                selected--;
                if (selected < 0)
                    selected += maxItems;
                Audio.PlaySound(p.owner.parent.owner.se.select);
            }
            else if (Input.KeyTest(StateType.REPEAT, KeyStates.RIGHT) && !disableLeftRight)
            {
                selected++;
                if (selected >= maxItems)
                    selected -= maxItems;
                Audio.PlaySound(p.owner.parent.owner.se.select);
            }
            else if (Input.KeyTest(StateType.TRIGGER, KeyStates.DECIDE))
            {
                result = selected;
            }
            else if (Input.KeyTest(StateType.TRIGGER, KeyStates.CANCEL))
            {
                result = Util.RESULT_CANCEL;
                Audio.PlaySound(p.owner.parent.owner.se.cancel);
            }

            // スクロールする
            int selectedLine = (selected / columnNum);
            if (selectedLine * itemHeight < scrollPos)
            {
                int targetPos = selectedLine * itemHeight;
                scrollPos = (scrollPos + targetPos) / 2;
            }
            else if (selectedLine * itemHeight + itemHeight > scrollPos + innerHeight)
            {
                int targetPos = selectedLine * itemHeight + itemHeight - innerHeight;
                scrollPos = (scrollPos + targetPos) / 2;
            }

#if WINDOWS
#else
            if (GameMain.IsPushedAndroidBackButton())
            {
                result = Util.RESULT_CANCEL;
            }
#endif
        }

        internal void DrawSelect(string[] strs, bool[] flags, int[] nums = null, Common.Resource.Icon.Ref[] icons = null)
        {
            // メニューのコンテンツ描画
            var pos = new Vector2(0, 0);
            var size = new Vector2(innerWidth / columnNum, itemHeight);
            for (int i = 0; i < maxItems; i++)
            {
                // 選択枠を表示
                if (!locked)
                    p.unselBox.Draw(pos, size);
                if (selected == i)
                    p.selBox.Draw(pos, size, blinker.getColor());

                if (nums != null && icons != null)
                {
                    DrawMenuItem(strs[i], pos, flags[i], nums[i], icons[i]);
                }
                else
                {
                    DrawMenuItem(strs[i], pos, flags[i]);
                }

                if (i % columnNum == columnNum - 1)
                {
                    pos.X = 0;
                    pos.Y += itemHeight;
                }
                else
                {
                    pos.X += innerWidth / columnNum;
                }
            }
        }

        internal void clickSelect(SharpKmyMath.Vector2 touchPos)
        {
            var offsetX = (int)windowPos.X - innerWidth / 2;
            var offsetY = (int)windowPos.Y - innerHeight / 2;

            var boxPos = new Vector2(0, 0);
            var boxSize = new Vector2(innerWidth / columnNum, itemHeight);

            //UnityEngine.Debug.Log("touchPos : " + touchPos.x + " " + touchPos.y);
            //UnityEngine.Debug.Log("offset : " + offsetX + " "  + offsetY);
            //UnityEngine.Debug.Log(boxPos.X + " " + boxPos.Y + " " + boxSize.X + " " + boxSize.Y + " " + innerWidth + " " + innerHeight);
            for (int i = 0; i < maxItems; i++)
            {
                if (i % columnNum == 0)
                {
                    boxPos.X = 0;
                    boxPos.Y = itemHeight * i / columnNum;

                    // ここで表示ケースに応じてタッチ位置を調整している
                    if (this is AskWindow || GameMain.IsPushedAndroidBackButton())
                        boxPos.Y += innerHeight - itemHeight;
                }
                else boxPos.X += boxSize.X;
                var x0 = boxPos.X + offsetX;
                var x1 = boxPos.X + offsetX + boxSize.X;
                var y0 = offsetY + boxPos.Y;
                var y1 = offsetY + boxPos.Y + boxSize.Y;
                //UnityEngine.Debug.Log(i + " " + x0 + " " + y0 + " " + x1 + " " + y1);
                if (x0 < touchPos.x && touchPos.x < x1 && y0 < touchPos.y && touchPos.y < y1)
                {
                    if (selected == i)
                    {
                        result = selected;
                        break;
                    }
                    else
                    {
                        selected = i;
                        Audio.PlaySound(p.owner.parent.owner.se.select);
                        break;
                    }
                }
            }
        }

        internal void setColumnNum(int num)
        {
            columnNum = num;
        }

        private int getReturnBoxHeight()
        {
            return Math.Min(itemHeight, LINE_HEIGHT);
        }

        internal override void Show()
        {
            if (!locked && p.owner.parent.owner.data.system.cursorPosition == SystemData.CursorPosition.NOT_KEEP)
                selected = 0;

            result = Util.RESULT_SELECTING;
            base.Show();
        }

        internal override void Hide()
        {
            base.Hide();
        }

        internal void DrawMenuItem(String str, Vector2 pos, bool enabled = true, int num = -1, Common.Resource.Icon.Ref icon = null)
        {
            pos.Y -= scrollPos;
            var origPos = pos;
            var textColor = enabled ? Color.White : Color.Gray;

            // アイコン
            if (icon != null)
            {
                int offsetY = (itemHeight - Common.Resource.Icon.ICON_WIDTH) / 2;
                pos.X += offsetY;
                pos.Y += offsetY;
                if (iconDic.ContainsKey(icon.guId))
                {
                    Graphics.DrawChipImage(iconDic[icon.guId], (int)pos.X, (int)pos.Y, icon.x, icon.y);
                }
                pos.X += Common.Resource.Icon.ICON_WIDTH;
                pos.Y -= offsetY;
            }

            pos.X += TEXT_OFFSET / 2;

            // 名前
            p.textDrawer.DrawString(str, pos, itemHeight, TextDrawer.VerticalAlignment.Center, textColor, 0.9f);

            // 個数
            if (num >= 0)
            {
                var numStr = "" + num;
                var size = p.textDrawer.MeasureString("" + numStr);
                pos = origPos;
                pos.X += innerWidth / columnNum - size.X;
                p.textDrawer.DrawString(numStr, pos, itemHeight, TextDrawer.VerticalAlignment.Center, textColor, 0.75f);
            }
        }
    }

    internal class MainMenu : SelectWindow
    {
        internal const int ITEM = 0;
        internal const int SKILL = 1;
        internal const int EQUIPMENT = 2;
        internal const int STATUS = 3;
        //internal const int SORT = 4;
        internal const int SAVE = 4;
        internal const int EXIT = 5;
        internal const int CONFIG = 6;
        internal const int CLOSE = 7;
        private string[] strs;
        private bool[] flags;

        internal MainMenu()
        {
            maxItems = 8;
            disableLeftRight = true;
        }

        public override void Initialize(CommonWindow.ParamSet pset, int x, int y, int maxWidth, int maxHeight)
        {
            base.Initialize(pset, x, y, maxWidth, maxHeight);
            innerWidth += 4;
            innerHeight += 4;

            strs = new String[]{
                p.gs.glossary.item,
                p.gs.glossary.skill,
                p.gs.glossary.equipment,
                p.gs.glossary.status,
                p.gs.glossary.save,
                p.gs.glossary.exit,
                p.gs.glossary.config,
                p.gs.glossary.close,
            };
            flags = new bool[]{
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
            };
        }

        internal override void DrawCallback()
        {
            itemHeight = innerHeight / maxItems;

            DrawSelect(strs, flags);
        }

        internal override void Show()
        {
            result = Util.RESULT_SELECTING;
            flags[4] = p.owner.parent.owner.data.system.saveAvailable;
            base.Show();
        }

        internal override void Unlock()
        {
            base.Unlock();
            result = Util.RESULT_SELECTING;
        }
    }

    internal class DetailWindow : CommonWindow
    {
        internal string detail = "";
        internal string detail_right;

        internal override void Show()
        {
            base.Show();
            detail_right = null;
        }

        internal override void DrawCallback()
        {
            var area = new Vector2(innerWidth - TEXT_OFFSET * 2, innerHeight);
            var pos = new Vector2(TEXT_OFFSET, 0);
            var drawSize = p.textDrawer.MeasureString(detail + (detail_right != null ? (" " + detail_right) : ""));
            float scale = Math.Min(1.0f, Math.Min(area.X / drawSize.X, maxWindowSize.Y / drawSize.Y));

            if (detail_right == null)
            {
                p.textDrawer.DrawString(detail, pos, area,
                    TextDrawer.HorizontalAlignment.Center, TextDrawer.VerticalAlignment.Center, Color.White, scale);
            }
            else
            {
                p.textDrawer.DrawString(detail, pos, area,
                    TextDrawer.HorizontalAlignment.Left, TextDrawer.VerticalAlignment.Center, Color.White, scale);

                p.textDrawer.DrawString(detail_right, pos, area,
                    TextDrawer.HorizontalAlignment.Right, TextDrawer.VerticalAlignment.Center, Color.White, scale * 0.8f);
            }
        }

        internal override void UpdateCallback()
        {
        }
    }
}

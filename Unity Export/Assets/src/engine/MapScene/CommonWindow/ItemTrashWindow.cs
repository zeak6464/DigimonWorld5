using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Yukar.Common;
using Yukar.Common.GameData;
using Yukar.Common.Rom;

namespace Yukar.Engine
{
    internal class ItemTrashController
    {
        internal enum TrashMode
        {
            ADD_NEW_ITEM,       // アイテムの追加を伴う場合
            JUST_THROW_AWAY,    // 単に捨てるだけの場合
        }
        internal TrashMode mode;

        internal enum TrashState
        {
            HIDE,
            INIT,
            ITEMSELECT,
            ASKSELECT,
        }
        private TrashState state;

        public MapScene owner;

        private DetailWindow guide;
        private TrashListWindow itemList;
        private AskWindow ask;
        private int newItemNum;
        internal CommonWindow.ParamSet res;
        private Item throwAwayItem;

        private const int WINDOW_Y_CENTERING_OFFSET = 12;

        internal ItemTrashController()
        {
            guide = new DetailWindow();
            itemList = new TrashListWindow();
            ask = new AskWindow();
            
            itemList.owner = this;
        }

        internal void initialize(MapScene mapScene)
        {
            owner = mapScene;
            res = owner.menuWindow.res;

            guide.Initialize(res, Graphics.ScreenWidth / 2,
                MenuController.WINDOW_POS_Y + MenuController.RIGHT_SUBWINDOW_HEIGHT / 2 + WINDOW_Y_CENTERING_OFFSET,
                MenuController.RIGHT_MAINWINDOW_WIDTH, MenuController.RIGHT_SUBWINDOW_HEIGHT);
            guide.detail = res.gs.glossary.inventoryFull + " " + res.gs.glossary.inventoryWitchItemThrowAway;
            itemList.Initialize(res, Graphics.ScreenWidth / 2,
                MenuController.RIGHT_MAINWINDOW_POS_Y + MenuController.RIGHT_MAINWINDOW_HEIGHT / 2 + WINDOW_Y_CENTERING_OFFSET,
                MenuController.RIGHT_MAINWINDOW_WIDTH, MenuController.RIGHT_MAINWINDOW_HEIGHT);
            ask.Initialize(res, Graphics.ScreenWidth / 2, Graphics.ScreenHeight / 2,
                MenuController.ASKWINDOW_WIDTH, MenuController.ASKWINDOW_HEIGHT);
        }

        internal void show(TrashMode mode, Guid newItem, int newItemNum)
        {
            this.mode = mode;
            guide.Show();
            itemList.newItemGuid = newItem;
            itemList.disableCancel = true;
            itemList.Unlock();
            itemList.Show();

            this.newItemNum = newItemNum;

            state = TrashState.ITEMSELECT;
            owner.LockControl();
        }

        internal void update()
        {
            guide.Update();
            itemList.Update();
            ask.Update();

            switch (state)
            {
                case TrashState.ITEMSELECT:
                    if(itemList.result != Util.RESULT_SELECTING)
                    {
                        if (itemList.returnSelected > 0 &&
                            (itemList.result == Util.RESULT_CANCEL || itemList.selectedItem == null))
                        {
                            if (mode == TrashMode.JUST_THROW_AWAY || itemList.newItem.IsSellable)
                            {
                                state = TrashState.HIDE;
                                itemList.Hide();
                                guide.Hide();
                                owner.UnlockControl();
                            }
                            // 捨てられない時はキャンセル音が流れるだけ
                            else
                            {
                                itemList.result = Util.RESULT_SELECTING;
                                Audio.PlaySound(owner.owner.se.cancel);
                            }
                        }
                        else if(itemList.flags[itemList.result])
                        {
                            Audio.PlaySound(owner.owner.se.decide);
                            itemList.Hide();
                            guide.Hide();
                            ask.setInfo(
                                string.Format(res.gs.glossary.inventoryAllOfStackThrowAway, itemList.selectedItem.name),
                                res.gs.glossary.yes, res.gs.glossary.no);
                            ask.selected = 1;
                            ask.Show();
                            throwAwayItem = itemList.selectedItem;
                            state = TrashState.ASKSELECT;
                        }
                        else
                        {
                            // 捨てられないアイテムで決定ボタンを押した時にここに来るので、選択中状態に戻す
                            itemList.result = Util.RESULT_SELECTING;
                            Audio.PlaySound(owner.owner.se.cancel);
                        }
                    }
                    break;
                case TrashState.ASKSELECT:
                    if (ask.result != Util.RESULT_SELECTING)
                    {
                        if (ask.result == AskWindow.RESULT_OK)
                        {
                            Audio.PlaySound(owner.owner.se.decide);
                            state = TrashState.HIDE;
                            itemList.Hide();
                            guide.Hide();
                            ask.Hide();
                            owner.UnlockControl();

                            // 選んだアイテムを捨てて、新しいアイテムを加える
                            owner.owner.data.party.SetItemNum(throwAwayItem.guId, 0);
                            owner.owner.data.party.SetItemNum(itemList.newItemGuid, newItemNum);
                        }
                        else
                        {
                            Audio.PlaySound(owner.owner.se.cancel);

                            // 前の画面に戻る
                            ask.Hide();
                            itemList.Show();
                            itemList.result = Util.RESULT_SELECTING;
                            guide.Show();
                            state = TrashState.ITEMSELECT;
                        }
                    }
                    break;
            }
        }

        internal void draw()
        {
            guide.Draw();
            itemList.Draw();
            ask.Draw();
        }

        internal bool isVisible()
        {
            return state != TrashState.HIDE;
        }
    }

    internal class TrashListWindow : ItemList
    {
        internal ItemTrashController owner;
        internal Guid newItemGuid;
        internal Common.Rom.Item newItem;

        internal TrashListWindow()
        {
            iconDic = new Dictionary<Guid, int>();
        }

        internal override void Show()
        {
            var items = p.owner.parent.owner.data.party.items; // アイテム袋
            ClearDic();
            CreateDic(items);
            if(newItemGuid != Guid.Empty)
                AddToDic(newItemGuid);

            base.Show();

            Audio.PlaySound(owner.owner.owner.se.item);

            // 売れない&捨てられないアイテムをdisable状態にする
            for (int i = 0; i < items.Count; i++)
                flags[i] = items[i].item.IsSellable;
        }

        internal void AddToDic(Guid itemGuid)
        {
            newItem = owner.owner.owner.catalog.getItemFromGuid(itemGuid) as Common.Rom.Item;

            if (iconDic.ContainsKey(newItem.icon.guId))
                return;

            var icon = p.owner.parent.owner.catalog.getItemFromGuid(newItem.icon.guId) as Common.Resource.Icon;
            if (icon == null)
                return;

            var imgId = Graphics.LoadImage(icon.path, Common.Resource.Icon.ICON_WIDTH, Common.Resource.Icon.ICON_HEIGHT);
            iconDic.Add(icon.guId, imgId);
        }

        internal override void Hide()
        {
            base.Hide();
        }

        internal override void UpdateCallback()
        {
            base.UpdateCallback();
        }

        internal override void DrawCallback()
        {
            DrawSelect(strs, flags, nums, icons);

            if (owner.mode == ItemTrashController.TrashMode.JUST_THROW_AWAY)
                DrawReturnBox();
            else
                DrawAbandonButton(newItem.IsSellable);
        }

        private void DrawAbandonButton(bool enabled)
        {
            // やめるボタン
            var textColor = enabled ? Color.White : Color.Gray;
            var size = new Vector2(innerWidth, getReturnBoxHeight());
            var pos = new Vector2((innerWidth - size.X) / 2, innerHeight - size.Y);

            if (!locked)
                p.unselBox.Draw(pos, size);
            if (returnSelected > 0)
                p.selBox.Draw(pos, size, blinker.getColor());

            var word = String.Format(owner.res.gs.glossary.inventoryAbandonItem, newItem.name);

            // アイコン
            if (iconDic.ContainsKey(newItem.icon.guId))
            {
                var icon = iconDic[newItem.icon.guId];
                var iconSizeX = Graphics.GetDivWidth(icon);
                var iconSizeY = Graphics.GetDivHeight(icon);
                var iconPosX = (int)((innerWidth - p.textDrawer.MeasureString(word).X * 0.9) / 2) - iconSizeX;
                var iconPosY = (int)(pos.Y + (size.Y - iconSizeY) / 2);
                Graphics.DrawChipImage(icon, iconPosX, iconPosY, newItem.icon.x, newItem.icon.y);
            }

            // 文字
            p.textDrawer.DrawString(word, pos, size, TextDrawer.HorizontalAlignment.Center, TextDrawer.VerticalAlignment.Center, textColor, 0.9f);
        }
    }
}

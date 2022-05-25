using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Yukar.Common;
using Yukar.Common.GameData;

namespace Yukar.Engine
{

    internal class ItemMenu : SelectWindow
    {
        internal const int USE = 0;
        internal const int THROWAWAY = 1;

        internal bool throwable = false;
        internal bool usable = false;

        internal ItemMenu()
        {
            maxItems = 2;
        }

        internal override void DrawCallback()
        {
            DrawSelect(new String[]{
                p.gs.glossary.use,
                p.gs.glossary.throwaway,
            }, new bool[]{
                usable,
                throwable,
            });
        }
    }

    internal class ItemList : PagedSelectWindow
    {
        internal string[] strs;
        internal bool[] flags;
        internal int[] nums;
        internal Common.Resource.Icon.Ref[] icons;
        internal Common.Rom.Item selectedItem;

        internal ItemList()
        {
            iconDic = new Dictionary<Guid, int>();
        }

        internal override void Show()
        {
            setColumnNum(2);
            setRowNum(6, true, 1);

            base.Show();

            var items = p.owner.parent.owner.data.party.items; // アイテム袋
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
                    strs[i] = p.textDrawer.GetContentText(items[i].item.name, innerWidth / 2 - Common.Resource.Icon.ICON_WIDTH * 2 - numStringLength, NameScale);
                    flags[i] = false;
                    if (GetUsableOnField(items[i].item))
                    {
                        flags[i] = true;
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

        internal override void Hide()
        {
            base.Hide();
        }

        internal bool GetUsableOnField(Common.Rom.Item item)
        {
            return (item.type == Common.Rom.ItemType.EXPENDABLE && item.expendable.availableInField) ||
                (item.type == Common.Rom.ItemType.EXPENDABLE_WITH_SKILL && item.expendableWithSkill.availableInField);
        }

        internal void ClearDic()
        {
            foreach (var icon in iconDic)
            {
                Graphics.UnloadImage(icon.Value);
            }

            iconDic.Clear();
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

            var items = p.owner.parent.owner.data.party.items; // アイテム袋

            if (returnSelected > 0)
            {
                selectedItem = null;
            }
            else if (items.Count > 0)
            {
                if (items.Count <= selected)
                    selected = items.Count - 1;
                selectedItem = items[selected].item;
            }
            else
            {
                selected = 0;
                selectedItem = null;
            }
        }

        internal override void DrawCallback()
        {
            DrawSelect(strs, flags, nums, icons);
            DrawReturnBox();
        }
    }

    internal class ItemNumWindow : CommonWindow
    {
        internal Common.GameData.Party.ItemStack stack; // アイテム詳細を表示する場合
        internal Common.Rom.Item destItem;
        internal Common.Rom.Skill skill;                // スキル詳細を表示する場合
        internal StatusDigest status;                   // ステータスを書いてもらうためのもの
        internal int statusIndex;

        internal bool restMp;
        internal Common.Resource.Icon.Ref icon;
        internal int imgId;
        internal bool showDescription = true;
        internal bool showStatusDetail = false;
        internal bool showDiff = false;

        internal override void Hide()
        {
            status = null;
            stack = null;
            skill = null;
            showDescription = true;
            showStatusDetail = false;
            showDiff = false;
            base.Hide();
        }

        internal override void DrawCallback()
        {
            var pos = Vector2.Zero;
            var size = new Vector2(innerWidth, LINE_HEIGHT);
            //var origPos = pos;
            var textColor = Color.White;
            if (stack != null && stack.num == 0)
                textColor = Color.Gray;
            if (skill != null && restMp)
                textColor = Color.Gray;

            // キャラクターのステータスを表示しないといけない場合は、書いてもらう
            if (status != null)
            {
                size.Y = status.itemHeight;
                status.DrawDigest(pos, size, statusIndex);
                pos.Y += size.Y;
                size.Y = LINE_HEIGHT;
            }

            if (stack != null || skill != null)
                p.selBox.Draw(pos, size);

            // 名前とアイコン
            if (stack != null)
            {
                DrawIcon(ref pos);
                p.textDrawer.DrawString(stack.item.name, pos, LINE_HEIGHT, TextDrawer.VerticalAlignment.Center, textColor, 0.9f);
            }
            else if (skill != null)
            {
                DrawIcon(ref pos);
                p.textDrawer.DrawString(skill.name, pos, LINE_HEIGHT, TextDrawer.VerticalAlignment.Center, textColor, 0.9f);
            }

            // 個数
            if (stack != null && stack.num >= 0)
            {
                var numStr = p.gs.glossary.holdNum + " : " + stack.num;
                //var textSize = p.textDrawer.MeasureString("" + numStr);
                //pos.X = origPos.X + innerWidth - textSize.X - TEXT_OFFSET;
                pos.X = TEXT_OFFSET;
                pos.Y += LINE_HEIGHT + TEXT_OFFSET;
                p.textDrawer.DrawString(numStr, pos, LINE_HEIGHT, TextDrawer.VerticalAlignment.Center, textColor, 0.75f);
            }
            // この処理いらなくなってた・・・
            /*
            else if(skill != null)
            {
                var consumption = skill.option.consumptionMagicpoint;
                var consumptionStr = p.gs.glossary.mp;
                if (consumption == 0)
                {
                    consumption = skill.option.consumptionHitpoint;
                    if (consumption > 0)
                    {
                        consumptionStr = p.gs.glossary.hp;
                    }
                }
                var numStr = String.Format(p.gs.glossary.skillCostStr, consumptionStr, consumption);
                var textSize = p.textDrawer.MeasureString("" + numStr);
                //pos.X = origPos.X + innerWidth - textSize.X - TEXT_OFFSET;
                pos.X = TEXT_OFFSET;
                pos.Y += LINE_HEIGHT + TEXT_OFFSET;
                p.textDrawer.DrawString(numStr, pos, LINE_HEIGHT, TextDrawer.VerticalAlignment.Center, textColor, 0.9f);
            }
            */

            // 説明
            if (showDescription)
            {
                pos.X = TEXT_OFFSET;
                pos.Y += LINE_HEIGHT + TEXT_OFFSET;
                size.X -= TEXT_OFFSET * 2;
                size.Y = innerHeight - (int)pos.Y;
                float scale = 0.75f;
                string text = "";
                if (stack != null)
                {
                    text = stack.item.description;
                }
                else if (skill != null)
                {
                    text = Util.createSkillDescription(p.owner.parent.owner.catalog, p.owner.parent.owner.data.party, skill);
                }

                var drawSize = p.textDrawer.MeasureString(text);
                scale = Math.Min(scale, size.X / drawSize.X);

                p.textDrawer.DrawString(text, pos, size,
                    TextDrawer.HorizontalAlignment.Left, TextDrawer.VerticalAlignment.Top, textColor, scale);
            }

            // キャラクターのステータスを表示しないといけない場合は、書いてもらう
            if (status != null && showStatusDetail)
            {
                pos.X = TEXT_OFFSET;
                pos.Y = status.itemHeight + LINE_HEIGHT + TEXT_OFFSET;
                status.DrawDetail(pos, statusIndex);

                if (showDiff)
                {
                    pos.X += 168;
                    status.DrawEquipDiff(pos, stack != null ? stack.item : null, destItem, statusIndex);
                }
            }
        }

        private void DrawIcon(ref Vector2 pos)
        {
            int offset = (LINE_HEIGHT - Common.Resource.Icon.ICON_WIDTH) / 2;
            Graphics.DrawChipImage(imgId, (int)pos.X + offset, (int)pos.Y + offset, icon.x, icon.y);
            pos.X += Common.Resource.Icon.ICON_WIDTH + offset * 2;
        }

        internal override void UpdateCallback()
        {
        }
    }
}

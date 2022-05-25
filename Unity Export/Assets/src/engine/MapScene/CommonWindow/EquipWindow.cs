using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Yukar.Common.GameData;

namespace Yukar.Engine
{
    class EquipWindow : PagedSelectWindow
    {
        public int heroIndex;

        internal const int WEAPON = 0;
        internal const int SHIELD = 1;
        internal const int HEAD = 2;
        internal const int ARMOR = 3;
        internal const int ACCESSORY1 = 4;
        internal const int ACCESSORY2 = 5;

        internal string[] strs;
        internal bool[] flags;
        internal Common.Resource.Icon.Ref[] icons;

        int offsetX = 48;
        int offsetY = 36;
        int imgId;

        internal EquipWindow()
        {
            maxItems = 6;
            iconDic = new Dictionary<Guid, int>();
            setColumnNum(1);
            setRowNum(maxItems, true);
            itemHeight = LINE_HEIGHT;
            imgId = Graphics.LoadImageDiv("./res/system/equip_icons.png", 6, 1);
        }

        internal override void Show()
        {
            base.Show();

            applyHeroIndex();

            innerWidth -= offsetX;
        }

        internal override void Hide()
        {
            innerWidth += offsetX;
            base.Hide();
        }

        internal override void DrawCallback()
        {
            DrawSelect(strs, flags, null, icons, offsetX / 2, offsetY);
            DrawReturnBox();

            var pos = new Vector2(-offsetX / 2, 0);

            p.textDrawer.DrawString(p.gs.glossary.equipment, pos, Color.White);

            pos.Y = offsetY + (LINE_HEIGHT - Common.Resource.Icon.ICON_HEIGHT) / 2;

            var iconIndexes = new int[] { 4, 3, 2, 5, 0, 0 };
            for (int i = 0; i < maxItems; i++)
            {
                Graphics.DrawChipImage(imgId, (int)pos.X, (int)pos.Y, iconIndexes[i], 0);
                pos.Y += LINE_HEIGHT;
            }
        }

        internal override void UpdateCallback()
        {
            base.UpdateCallback();
        }

        internal void applyHeroIndex()
        {
            var hero = p.owner.parent.owner.data.party.members[heroIndex];

            strs = new String[]{
                (hero.equipments[0] != null ? hero.equipments[0].name : p.gs.glossary.nothing),
                (hero.equipments[1] != null ? hero.equipments[1].name : p.gs.glossary.nothing),
                (hero.equipments[2] != null ? hero.equipments[2].name : p.gs.glossary.nothing),
                (hero.equipments[3] != null ? hero.equipments[3].name : p.gs.glossary.nothing),
                (hero.equipments[4] != null ? hero.equipments[4].name : p.gs.glossary.nothing),
                (hero.equipments[5] != null ? hero.equipments[5].name : p.gs.glossary.nothing),
            };

            for (var i = 0; i < strs.Length; ++i)
            {
                strs[i] = p.textDrawer.GetContentText(strs[i], innerWidth - Common.Resource.Icon.ICON_WIDTH * 2 - offsetX / 2, NameScale);
            }

            flags = new bool[]{
                !hero.rom.fixEquipments[0],
                !hero.rom.fixEquipments[1],
                !hero.rom.fixEquipments[2],
                !hero.rom.fixEquipments[3],
                !hero.rom.fixEquipments[4],
                !hero.rom.fixEquipments[5],
            };

            icons = new Common.Resource.Icon.Ref[]{
                (hero.equipments[0] != null ? hero.equipments[0].icon : null),
                (hero.equipments[1] != null ? hero.equipments[1].icon : null),
                (hero.equipments[2] != null ? hero.equipments[2].icon : null),
                (hero.equipments[3] != null ? hero.equipments[3].icon : null),
                (hero.equipments[4] != null ? hero.equipments[4].icon : null),
                (hero.equipments[5] != null ? hero.equipments[5].icon : null),
            };
        }
    }

    class EquipDetail : CommonWindow
    {
        internal Common.Rom.Item sourceItem = null;
        internal Common.Rom.Item destItem = null;

        internal override void DrawCallback()
        {
            var pos = new Vector2(4, 4);
            if (sourceItem != null)
                p.textDrawer.DrawString(sourceItem.name, pos, Color.White);

            var specint = 0;
            var spec = "";
            getSpec(sourceItem, out specint, out spec);
            pos.X += 240;
            p.textDrawer.DrawString(spec, pos, Color.White);

            pos.X += 90;
            p.textDrawer.DrawString("→", pos, Color.White);

            var specint2 = 0;
            var spec2 = "";
            getSpec(destItem, out specint2, out spec2);
            pos.X += 30;
            var textColor = Color.White;
            if (specint > specint2)
            {
                textColor = Color.Red;
            }
            else if (specint < specint2)
            {
                textColor = Color.Green;
            }
            p.textDrawer.DrawString(spec2, pos, textColor);
        }

        private void getSpec(Common.Rom.Item item, out int specint, out string spec)
        {
            if (item != null)
            {
                if (item.weapon != null)
                {
                    specint = item.weapon.attack;
                    spec = "" + item.weapon.attack;
                    if (item.weapon.attrAttack > 0)
                    {
                        specint += item.weapon.attrAttack;
                        spec += "+" + item.weapon.attrAttack;
                    }
                    return;
                }
                else if (item.equipable != null)
                {
                    specint = item.equipable.defense;
                    spec = "" + item.equipable.defense;
                    return;
                }
            }

            specint = 0;
            spec = "0";
        }

        internal override void UpdateCallback()
        {
        }
    }

    internal class EquipItemList : PagedSelectWindow
    {
        List<Party.ItemStack> items;

        internal string[] strs;
        internal bool[] flags;
        internal int[] nums;
        internal Common.Resource.Icon.Ref[] icons;

        internal Common.Rom.Item selectedItem;
        internal Common.Rom.Hero hero;
        internal Common.Rom.ItemType targetType;

        internal EquipItemList()
        {
            iconDic = new Dictionary<Guid, int>();
        }

        internal override void Show()
        {
            setColumnNum(2);
            setRowNum(6, true, 1);

            base.Show();

            var allItems = p.owner.parent.owner.data.party.items; // アイテム袋
            items = allItems.Where(i => i.item.type == targetType).ToList();

            // "はずす" を追加する ← 旧)武器以外には "はずす" 項目を用意する
            //if(targetType != Common.Rom.ItemType.WEAPON)
            items.Insert(0, new Party.ItemStack());

            if (items.Count > 0)
            {
                maxItems = items.Count;
                strs = new string[items.Count];
                flags = new bool[items.Count];
                nums = new int[items.Count];
                icons = new Common.Resource.Icon.Ref[items.Count];

                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].item != null)
                    {
                        var numStringLength = (int)(p.textDrawer.MeasureString(items[i].num.ToString()).X * NumScale);
                        strs[i] = p.textDrawer.GetContentText(items[i].item.name, innerWidth / 2 - Common.Resource.Icon.ICON_WIDTH * 2 - numStringLength, NameScale);
                        flags[i] = true;
                        if (hero.availableItemsList.ContainsKey(items[i].item.guId))
                            flags[i] = hero.availableItemsList[items[i].item.guId];
                        nums[i] = items[i].num;
                        icons[i] = items[i].item.icon;
                    }
                    else
                    {
                        strs[i] = p.gs.glossary.removeItem;
                        flags[i] = true;
                        nums[i] = -1;
                        icons[i] = null;
                    }
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

        internal bool GetUsableOnField(Common.Rom.Item item)
        {
            return item.type == Common.Rom.ItemType.EXPENDABLE && item.expendable.availableInField ||
                item.type == Common.Rom.ItemType.EXPENDABLE_WITH_SKILL && item.expendableWithSkill.availableInField;
        }

        internal override void UpdateCallback()
        {
            base.UpdateCallback();

            if (items.Count > 0)
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
}

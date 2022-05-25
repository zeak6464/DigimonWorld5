using System;
using System.Collections.Generic;

namespace Yukar.Engine
{
    internal class SkillList : PagedSelectWindow
    {
        string[] strs;
        internal bool[] flags;
        int[] nums;
        Common.Resource.Icon.Ref[] icons;

        internal Common.Rom.Skill selectedItem;
        public int heroIndex;

        internal SkillList()
        {
            iconDic = new Dictionary<Guid, int>();
        }

        internal override void Show()
        {
            selectedItem = null;

            setColumnNum(2);
            setRowNum(6, true, 1);

            base.Show();

            var hero = p.owner.parent.owner.data.party.members[heroIndex];
            var items = hero.skills;
            if (items.Count > 0)
            {
                maxItems = items.Count;
                strs = new string[items.Count];
                flags = new bool[items.Count];
                //nums = new int[items.Count];
                icons = new Common.Resource.Icon.Ref[items.Count];

                for (int i = 0; i < items.Count; i++)
                {
                    strs[i] = p.textDrawer.GetContentText(items[i].name, innerWidth / 2 - Common.Resource.Icon.ICON_WIDTH * 2, NameScale);
                    flags[i] = false;
                    if (GetUsableOnField(items[i]) &&
                        items[i].option.consumptionHitpoint <= hero.hitpoint &&
                        items[i].option.consumptionMagicpoint <= hero.magicpoint &&
                        p.owner.parent.owner.data.party.isOKToConsumptionItem(items[i].option.consumptionItem, items[i].option.consumptionItemAmount))
                    {
                        flags[i] = true;
                    }
                    /*
                    if (items[i].option.consumptionMagicpoint > 0)
                        nums[i] = items[i].option.consumptionMagicpoint;
                    else
                        nums[i] = items[i].option.consumptionHitpoint;
                    */
                    icons[i] = items[i].icon;
                }
            }
            else
            {
                maxItems = 1;
                strs = new string[] { p.gs.glossary.noSkill };
                flags = new bool[] { false };
                nums = null;
                icons = null;
            }
        }

        private bool GetUsableOnField(Common.Rom.Skill skill)
        {
            return skill.option.availableInField &&
                (skill.friendEffect != null || MenuController.isInstantSkill(p.owner.parent.owner.catalog, skill));
        }

        internal void ClearDic()
        {
            foreach (var icon in iconDic)
            {
                Graphics.UnloadImage(icon.Value);
            }

            iconDic.Clear();
        }

        internal void CreateDic(List<Common.GameData.Hero> members)
        {
            foreach (var hero in members)
            {
                CreateDic(hero.skills);
            }
        }

        internal void CreateDic(List<Common.Rom.Skill> items)
        {
            foreach (var item in items)
            {
                if (iconDic.ContainsKey(item.icon.guId))
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

            var items = p.owner.parent.owner.data.party.members[heroIndex].skills;

            if (items.Count > 0)
            {
                if (items.Count <= selected)
                    selected = items.Count - 1;
                selectedItem = items[selected];
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

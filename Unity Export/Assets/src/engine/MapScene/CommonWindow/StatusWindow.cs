using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Yukar.Common.GameData;
using Yukar.Common;

namespace Yukar.Engine
{
    class StatusWindow : StatusDigest
    {
        public int heroIndex;

        private int nextHeroIndex;
        private int prevHeroIndex;
        const float CHANGE_COUNT = 15;
        private float changeCount = -1;
        private bool moveDirIsRight;
        private const int SEPARATOR_X = 260;

        internal override void Show()
        {
            showReturn = true;
            base.Show();
            closeText = p.gs.glossary.close;
        }

        internal override void UpdateCallback()
        {
            // 親のUpdateを呼ばないので、ここでupdateする
            blinker.update();

            if (Input.KeyTest(Input.StateType.REPEAT, Input.KeyStates.LEFT))
            {
                moveDirIsRight = false;
                nextHeroIndex = heroIndex - 1;
                if (nextHeroIndex < 0)
                {
                    nextHeroIndex = p.owner.parent.owner.data.party.members.Count - 1;
                }
                DoChangeHero();
                Audio.PlaySound(p.owner.parent.owner.se.select);
            }
            else if (Input.KeyTest(Input.StateType.REPEAT, Input.KeyStates.RIGHT))
            {
                moveDirIsRight = true;
                nextHeroIndex = heroIndex + 1;
                if (nextHeroIndex >= p.owner.parent.owner.data.party.members.Count)
                {
                    nextHeroIndex = 0;
                }
                DoChangeHero();
                Audio.PlaySound(p.owner.parent.owner.se.select);
            }

            maxItems = 0;
            ProcSelect();

            if (changeCount >= 0)
                changeCount += GameMain.getRelativeParam60FPS();
        }

        private void DoChangeHero()
        {
            prevHeroIndex = heroIndex;
            heroIndex = nextHeroIndex;
            changeCount = 0;
        }

        internal override void DrawCallback()
        {
            var hero = p.owner.parent.owner.data.party.members[heroIndex];
            itemHeight = LINE_HEIGHT;

            // 名前を書く
            var pos = new Vector2(TEXT_OFFSET, 0);
            p.textDrawer.DrawString(p.owner.parent.owner.data.party.getHeroName(hero.rom.guId), pos, Color.White);

            // 左側(基本ステータス)を書く
            pos.X = TEXT_OFFSET;
            pos.Y = 32;
            DrawBasicStatus(pos, heroIndex, SEPARATOR_X - TEXT_OFFSET);

            // 左下(詳細ステータス)を書く
            pos.X = TEXT_OFFSET;
            pos.Y = 112;
            DrawDetail(pos, heroIndex, SEPARATOR_X - TEXT_OFFSET);

            // 右上(装備)を書く
            pos.X = SEPARATOR_X + TEXT_OFFSET;
            pos.Y = 32;
            DrawEquipList(pos);

            // 右下(経験値等)を書く
            pos.X = SEPARATOR_X + TEXT_OFFSET;
            pos.Y = 256;
            DrawExp(pos, heroIndex, SEPARATOR_X - TEXT_OFFSET);

            // 下(キャラ説明)を書く
            pos.X = TEXT_OFFSET;
            pos.Y = 310;
            var size = new Vector2(
                innerWidth - pos.X - TEXT_OFFSET,
                innerHeight - pos.Y - LINE_HEIGHT - TEXT_OFFSET - 10);
            p.selBox.Draw(pos, size, Color.Black);

            const int FIXED_BORDER_SIZE = 16;
            pos.X += FIXED_BORDER_SIZE;
            pos.Y += FIXED_BORDER_SIZE;
            size.X -= FIXED_BORDER_SIZE * 2;
            size.Y -= FIXED_BORDER_SIZE * 2;
            float descriptionTextScale = 0.8f;
            float descLineHeight = LINE_HEIGHT / 2;
            float descLineSpace = LINE_HEIGHT / 2 * 0.25f;
            var strs = MessageWindow.SplitStringInnerWidth(hero.rom.description, (int)(size.X * 1.0f / descriptionTextScale), p.textDrawer).Take(3);
            descriptionTextScale = Math.Min(descriptionTextScale, size.Y / (descLineHeight * strs.Count() - descLineSpace));
            strs = MessageWindow.SplitStringInnerWidth(hero.rom.description, (int)(size.X * 1.0f / descriptionTextScale), p.textDrawer).Take(3);
            descriptionTextScale = Math.Min(descriptionTextScale, size.Y / (descLineHeight * strs.Count() - descLineSpace));

            foreach (string text in strs)
            {
                p.textDrawer.DrawString(text, pos, TextDrawer.HorizontalAlignment.Left, TextDrawer.VerticalAlignment.Center, Color.White, descriptionTextScale);
                pos.Y += (descLineHeight + descLineSpace) * descriptionTextScale;
            }

            // 下(ページアイコン)を書く
            int memberCount = p.owner.parent.owner.data.party.members.Count;
            /*pos.X = maxWindowSize.X * 0.5f - (memberCount * 0.5f * Graphics.GetImageWidth(p.pageImgId) / 2);
            pos.Y = 395;
            for (int i = 0; i < memberCount; i++)
            {
                int imageSourceX;

                if (i == heroIndex)
                {
                    imageSourceX = 0;
                }
                else
                {
                    imageSourceX = Graphics.GetImageWidth(p.pageImgId) / 2;
                }

                Graphics.DrawImage(p.pageImgId, (int)pos.X, (int)pos.Y, new Rectangle(imageSourceX, 0, Graphics.GetImageWidth(p.pageImgId) / 2, Graphics.GetImageHeight(p.pageImgId)));

                pos.X += Graphics.GetImageWidth(p.pageImgId) / 2;
            }*/
            if (memberCount >= 2)
            {
                // 下(ページアイコン)を書く
                pos.X = (innerWidth - memberCount * Graphics.GetImageWidth(p.pageImgId) / 2) / 2;
                pos.Y = innerHeight - itemHeight - Graphics.GetImageHeight(p.pageImgId);
                for (int i = 0; i < memberCount; i++)
                {
                    int imageSourceX;

                    if (i == heroIndex)
                    {
                        imageSourceX = 0;
                    }
                    else
                    {
                        imageSourceX = Graphics.GetImageWidth(p.pageImgId) / 2;
                    }

                    Graphics.DrawImage(p.pageImgId, (int)pos.X, (int)pos.Y, new Rectangle(imageSourceX, 0, Graphics.GetImageWidth(p.pageImgId) / 2, Graphics.GetImageHeight(p.pageImgId)));

                    pos.X += Graphics.GetImageWidth(p.pageImgId) / 2;
                }
            }

            // 立ち絵を書く
            pos.X = -windowPos.X + maxWindowSize.X / 2;
            pos.Y = -windowPos.Y + maxWindowSize.Y / 2;
            size.X = -pos.X;
            size.Y = Graphics.ScreenHeight;
            float color = 1.0f;
            if (changeCount >= CHANGE_COUNT)
            {
                changeCount = -1;
            }
            else if (changeCount >= 0)
            {
                color = changeCount / CHANGE_COUNT;
                color = color * color * color;

                // キャラの切り替わり中は前のキャラも書く
                DrawChr(pos, size, 1 - color, prevHeroIndex, moveDirIsRight);
            }
            DrawChr(pos, size, color, heroIndex, !moveDirIsRight);

            // 閉じるボタンを書く
            DrawReturnBox();
        }

        private void DrawChr(Vector2 pos, Vector2 size, float delta, int heroIndex, bool offsetToRight)
        {
            var color = new Color(delta, delta, delta, delta);

            if (offsetToRight)
            {
                pos.X += 64 * (1 - delta);
            }
            else
            {
                pos.X -= 64 * (1 - delta);
            }

            int imgId = p.partyChars[heroIndex].imgId;
            var imgWidth = Graphics.GetImageWidth(imgId);
            var imgHeight = Graphics.GetImageHeight(imgId);
            Graphics.DrawImage(imgId,
                (int)(pos.X + (size.X - imgWidth) / 2),
                (int)(pos.Y + size.Y - imgHeight), color);
        }

        private void DrawEquipList(Vector2 pos)
        {
            var hero = p.owner.parent.owner.data.party.members[heroIndex];
            foreach (var item in hero.equipments)
            {
                Common.Resource.Icon.Ref icon = new Common.Resource.Icon.Ref();
                var str = p.gs.glossary.nothing;
                if (item != null)
                {
                    str = item.name;
                    icon = item.icon;
                }
                str = p.textDrawer.GetContentText(str, innerWidth - SEPARATOR_X - TEXT_OFFSET - Common.Resource.Icon.ICON_WIDTH * 2, NameScale);
                DrawMenuItem(str, pos, true, -1, icon);
                pos.Y += Common.Resource.Icon.ICON_HEIGHT;
            }
        }
    }


    internal class StatusDigest : PagedSelectWindow
    {
        public bool selectable;
        public bool allSelect;
        public bool disableDead;
        public bool[] selectableStates;

        public const int DIGEST_AREA_OFFSET = 6;
        public const int DIGEST_BOX_OFFSET = 2;
        public bool showReturn;
        private static int sStatusIconImgId;

        internal override void Show()
        {
            disableDead = false;
            base.Show();

            result = Util.RESULT_SELECTING;
            allSelect = false;
            selectableStates = new bool[4] { true, true, true, true };
            maxItems = p.owner.parent.owner.data.party.members.Count;

            if (windowState == WindowState.SHOW_WINDOW || windowState == WindowState.RESIZING_WINDOW)
                return;

            refreshLayoutInfo();

            if (showReturn)
            {
                windowPos.Y = 16 + maxWindowSize.Y / 2;
            }
            else
            {
                windowPos.Y = 16 + 64 + maxWindowSize.Y / 2;
            }
            innerHeight = (int)maxWindowSize.Y - p.window.paddingTop - p.window.paddingBottom;
        }

        internal override void Resize()
        {
            base.Resize();

            refreshLayoutInfo();

            innerHeight = (int)maxWindowSize.Y - p.window.paddingTop - p.window.paddingBottom;
        }

        private void refreshLayoutInfo()
        {
            maxWindowSize.Y = 422;  // itemHeight算出のため、一旦元に戻す
            innerHeight = (int)maxWindowSize.Y - p.window.paddingTop - p.window.paddingBottom;

            setColumnNum(2);
            setRowNum(2, false);

            if (showReturn)
            {
                maxWindowSize.Y = 486;
                targetWindowPos.Y = 16 + maxWindowSize.Y / 2;
            }
            else
            {
                maxWindowSize.Y = 422;
                targetWindowPos.Y = 16 + 64 + maxWindowSize.Y / 2;
            }
        }

        internal override void DrawCallback()
        {
            var area = new Vector2(innerWidth / columnNum, itemHeight);

            if (selectable)
            {
                for (int i = 0; i < p.owner.parent.owner.data.party.members.Count; i++)
                {
                    if (selectableStates[i])
                        DrawSelectableBox(i, area);
                }

                if (returnSelected > 0)
                {
                    // なにもしない
                }
                else if (allSelect)
                {
                    for (int i = 0; i < p.owner.parent.owner.data.party.members.Count; i++)
                    {
                        if (selectableStates[i])
                            DrawSelectBox(i, area);
                    }
                }
                else
                {
                    DrawSelectBox(selected, area);
                }
            }

            for (int i = 0; i < p.owner.parent.owner.data.party.members.Count; i++)
            {
                // 座標算出
                var pos = Vector2.Zero;
                pos.X += (i % 2) * area.X;
                pos.Y += (i / 2) * area.Y;
                var origArea = area;

                // 少しオフセットする
                pos.X += DIGEST_AREA_OFFSET;
                pos.Y += DIGEST_AREA_OFFSET;
                origArea.X -= DIGEST_AREA_OFFSET * 2;
                origArea.Y -= DIGEST_AREA_OFFSET * 2;

                var party = p.owner.parent.owner.data.party;

                DrawDigest(pos, origArea, party, i);
            }

            if (showReturn)
                DrawReturnBox();
        }

        internal void DrawDigest(Vector2 pos, Vector2 size, int index)
        {
            var party = p.owner.parent.owner.data.party;
            DrawDigest(pos, size, party, index);
        }

        internal void DrawDigest(Vector2 origPos, Vector2 origArea, Party party, int index)
        {
            var member = party.members[index];
            var color = Color.White;
            var area = origArea;
            var pos = origPos;

            if (disableDead && member.statusAilments.HasFlag(Hero.StatusAilments.DOWN))
            {
                color = Color.Gray;
            }

            const int MARGIN = 4;
            const int TEXT_HEIGHT = 20;
            const int NAME_HEIGHT = 28;
            const int CONTENT_PADDING = 8;
            const string TEXT_SPACE = "  ";

            // 名前枠
            pos = origPos;
            pos.X += MARGIN;
            pos.Y += MARGIN;
            area.X = origArea.X - MARGIN * 2;
            area.Y = NAME_HEIGHT;
            Graphics.DrawFillRect((int)pos.X, (int)pos.Y, (int)area.X, (int)area.Y, 32, 32, 32, 16);

            // 顔グラ
            const int CHR_WIDTH = 160;
            pos.X = origPos.X + MARGIN;
            pos.Y = origPos.Y + MARGIN;
            area.X = CHR_WIDTH;
            area.Y = origArea.Y - MARGIN * 2;
            DrawChr(pos, area, p.partyChars[index],
                member.statusAilments.HasFlag(Common.GameData.Hero.StatusAilments.DOWN) ? Color.Gray : Color.White);

            // 名前
            pos = origPos;
            pos.X += MARGIN;
            pos.Y += MARGIN;
            area.X = origArea.X - MARGIN * 2;
            area.Y = NAME_HEIGHT;
            p.textDrawer.DrawString(party.getHeroName(member.rom.guId) + TEXT_SPACE, pos, area,
                TextDrawer.HorizontalAlignment.Right,
                TextDrawer.VerticalAlignment.Center, color, 0.75f);
            pos.Y += area.Y + CONTENT_PADDING;

            // LV
            // レベルのラベル文字列の全角・半角文字数を調べる
            string lvLabel = p.gs.glossary.level;
            area.X = p.textDrawer.MeasureString(lvLabel + "99").X * 0.75f + MARGIN * 3;
            area.Y = TEXT_HEIGHT;
            pos.X = origPos.X + origArea.X - area.X - MARGIN;
            Graphics.DrawFillRect((int)pos.X, (int)pos.Y, (int)area.X, (int)area.Y, 32, 32, 32, 16);
            pos.X += MARGIN;
            p.textDrawer.DrawString(lvLabel, pos, area, TextDrawer.HorizontalAlignment.Left, TextDrawer.VerticalAlignment.Center, color, 0.75f);
            pos.X -= MARGIN;
            area.X -= MARGIN;
            p.textDrawer.DrawString("" + member.level, pos, area, TextDrawer.HorizontalAlignment.Right, TextDrawer.VerticalAlignment.Center, color, 0.75f);
            area.X -= MARGIN;
            pos.Y += area.Y + CONTENT_PADDING;

            // HPとMP
            area.X = 136;
            area.Y = TEXT_HEIGHT * 5;
            pos.X = origPos.X + origArea.X - area.X - MARGIN;
            pos.Y = origPos.Y + origArea.Y - area.Y - MARGIN;
            Graphics.DrawFillRect((int)pos.X, (int)pos.Y, (int)area.X, (int)area.Y, 32, 32, 32, 16);

            // HP
            area.Y = TEXT_HEIGHT;
            p.textDrawer.DrawString(" " + p.gs.glossary.hp, pos, area, TextDrawer.HorizontalAlignment.Left, TextDrawer.VerticalAlignment.Center, color, 0.75f);
            pos.Y += TEXT_HEIGHT;
            p.textDrawer.DrawString("/", pos, area, TextDrawer.HorizontalAlignment.Center, TextDrawer.VerticalAlignment.Center, color, 0.75f);
            var oldArea = area.X;
            var oldPos = pos.X;
            var slashWidth = p.textDrawer.MeasureString("/").X * 0.75f;
            area.X = (area.X - slashWidth) / 2 - MARGIN;
            p.textDrawer.DrawString("" + member.hitpoint, pos, area, TextDrawer.HorizontalAlignment.Right, TextDrawer.VerticalAlignment.Center, color, 0.75f);
            pos.X += area.X + slashWidth + MARGIN * 2;
            p.textDrawer.DrawString("" + member.maxHitpoint, pos, area, TextDrawer.HorizontalAlignment.Left, TextDrawer.VerticalAlignment.Center, color, 0.75f);
            area.X = oldArea;
            pos.X = oldPos;
            pos.Y += TEXT_HEIGHT;

            // MP
            area.Y = TEXT_HEIGHT;
            p.textDrawer.DrawString(" " + p.gs.glossary.mp, pos, area, TextDrawer.HorizontalAlignment.Left, TextDrawer.VerticalAlignment.Center, color, 0.75f);
            pos.Y += TEXT_HEIGHT;
            p.textDrawer.DrawString("/", pos, area, TextDrawer.HorizontalAlignment.Center, TextDrawer.VerticalAlignment.Center, color, 0.75f);
            area.X = (area.X - slashWidth) / 2 - MARGIN;
            p.textDrawer.DrawString("" + member.magicpoint, pos, area, TextDrawer.HorizontalAlignment.Right, TextDrawer.VerticalAlignment.Center, color, 0.75f);
            pos.X += area.X + slashWidth + MARGIN * 2;
            p.textDrawer.DrawString("" + member.maxMagicpoint, pos, area, TextDrawer.HorizontalAlignment.Left, TextDrawer.VerticalAlignment.Center, color, 0.75f);
            area.X = oldArea;
            pos.X = oldPos;
            pos.Y += TEXT_HEIGHT;

            // 状態異常
            var scale = 0.75f;
            var statBefore = " " + p.gs.glossary.ailments + ": ";
            var stat = "";
            if (member.statusAilments == Hero.StatusAilments.NONE)
            {
                stat = p.gs.glossary.normal;
            }
            else if (member.statusAilments.HasFlag(Hero.StatusAilments.POISON))
            {
                stat = p.gs.glossary.poison;
                color = Color.MediumPurple;

                Graphics.DrawChipImage(sStatusIconImgId, (int)origPos.X + 160, (int)origPos.Y + 40, 7, 1);
            }
            else if (member.statusAilments.HasFlag(Hero.StatusAilments.DOWN))
            {
                stat = p.gs.glossary.dead;
                color = Color.IndianRed;

                Graphics.DrawChipImage(sStatusIconImgId, (int)origPos.X + 160, (int)origPos.Y + 40, 6, 1);
            }
            var totalWidth = p.textDrawer.MeasureString(statBefore).X + p.textDrawer.MeasureString(stat).X;
            if (scale > (area.X - MARGIN) / totalWidth)
                scale = (area.X - MARGIN) / totalWidth;

            p.textDrawer.DrawString(statBefore, pos, area, TextDrawer.HorizontalAlignment.Left, TextDrawer.VerticalAlignment.Center, color, scale);
            var w = p.textDrawer.MeasureString(statBefore) * scale;
            pos.X += w.X;
            area.X -= w.X;
            p.textDrawer.DrawString(stat, pos, area, TextDrawer.HorizontalAlignment.Left, TextDrawer.VerticalAlignment.Center, color, scale);
            pos.X -= w.X;
            pos.Y += TEXT_HEIGHT;
        }

        internal void DrawBasicStatus(Vector2 pos, int index, int width = 160)
        {
            var strs = new string[]{
                p.gs.glossary.level, p.gs.glossary.hp, p.gs.glossary.mp,
            };

            var member = p.owner.parent.owner.data.party.members[index];
            var nums = new string[]{
                "" + member.level, member.hitpoint + "/" + member.maxHitpoint, member.magicpoint + "/" + member.maxMagicpoint
            };

            for (int i = 0; i < strs.Length; i++)
            {
                pos.Y += 4;
                p.textDrawer.DrawString(strs[i], pos, Color.Lavender, 0.75f);
                pos.Y -= 4;
                p.textDrawer.DrawString(nums[i], pos, width, TextDrawer.HorizontalAlignment.Right, Color.White);
                pos.Y += LINE_HEIGHT / 2;
            }
        }

        internal void DrawExp(Vector2 pos, int index, int width)
        {
            var strs = new string[]{
                p.gs.glossary.exp, p.gs.glossary.nextLevel,
            };
            var member = p.owner.parent.owner.data.party.members[index];
            int nextexp = Common.GameData.Party.expTable[member.rom.levelGrowthRate, member.level - 1] - member.exp;
            var nums = new string[]{
                "" + member.exp, "" + nextexp
            };

            for (int i = 0; i < strs.Length; i++)
            {
                // 最大レベルの時は描画しない
                if (i == 1 && member.level == Common.GameData.Hero.MAX_LEVEL)
                    continue;

                pos.Y += 4;
                p.textDrawer.DrawString(strs[i], pos, Color.Lavender, 0.75f);
                pos.Y -= 4;
                p.textDrawer.DrawString(nums[i], pos, width, TextDrawer.HorizontalAlignment.Right, Color.White);
                pos.Y += LINE_HEIGHT / 2;
            }
        }

        internal void DrawDetail(Vector2 pos, int index, int width = 160)
        {
            var strs = new string[]{
                p.gs.glossary.attackPower, p.gs.glossary.elementAttackPower, p.gs.glossary.magic,
                p.gs.glossary.defense, p.gs.glossary.speed,
                p.gs.glossary.dexterity, p.gs.glossary.evasion,
            };

            var member = p.owner.parent.owner.data.party.members[index];
            var nums = new int[]{
                member.equipmentEffect.attack + member.power,
                member.equipmentEffect.elementAttack,
                member.magic,
                member.equipmentEffect.defense + member.vitality,
                member.speed, Math.Min(100, member.equipmentEffect.dexterity),
                Math.Min(100, member.equipmentEffect.evation)
            };

            var suffix = new string[]{
                "", "", "", "", "", "%", "%",
            };

            for (int i = 0; i < strs.Length; i++)
            {
                pos.Y += 4;
                p.textDrawer.DrawString(strs[i], pos, Color.Lavender, 0.75f);
                pos.Y -= 4;
                p.textDrawer.DrawString(nums[i] + suffix[i], pos, width, TextDrawer.HorizontalAlignment.Right, Color.White);
                pos.Y += LINE_HEIGHT / 2;
            }
        }

        internal void DrawEquipDiff(Vector2 pos, Common.Rom.Item srcItem, Common.Rom.Item destItem, int index)
        {

            var member = p.owner.parent.owner.data.party.members[index];
            var nums = new int[]{
                member.equipmentEffect.attack + member.power,
                member.equipmentEffect.elementAttack,
                member.magic,
                member.equipmentEffect.defense + member.vitality,
                member.speed, Math.Min(100, member.equipmentEffect.dexterity),
                Math.Min(100, member.equipmentEffect.evation)
            };

            int eqIdx = 0;
            for (int i = 0; i < member.equipments.Length; i++)
            {
                if (member.equipments[i] == srcItem)
                {
                    eqIdx = i;
                    break;
                }
            }

            member.equipments[eqIdx] = destItem;
            member.refreshEquipmentEffect(false);

            var nums2 = new int[]{
                member.equipmentEffect.attack + member.power,
                member.equipmentEffect.elementAttack,
                member.magic,
                member.equipmentEffect.defense + member.vitality,
                member.speed, Math.Min(100, member.equipmentEffect.dexterity),
                Math.Min(100, member.equipmentEffect.evation)
            };

            member.equipments[eqIdx] = srcItem;
            member.refreshEquipmentEffect(false);

            var suffix = new string[]{
                "", "", "", "", "", "%", "%",
            };

            for (int i = 0; i < nums.Length; i++)
            {
                if (nums[i] != nums2[i])
                {
                    p.textDrawer.DrawString("→", pos, Color.White);
                    var color = Color.LightPink;
                    if (nums[i] < nums2[i])
                        color = Color.LightBlue;
                    p.textDrawer.DrawString(nums2[i] + suffix[i], pos, 96, TextDrawer.HorizontalAlignment.Right, color);
                }
                pos.Y += LINE_HEIGHT / 2;
            }
        }

        internal void DrawChr(Vector2 pos, Vector2 area, CommonWindow.ImageInstance chr, Color color, float scale = 1.25f)
        {
            if (chr.imgId == -1)
                return;

            var dest = new Rectangle((int)pos.X, (int)pos.Y, (int)area.X, (int)area.Y);
            var src = new Rectangle(0, 0, (int)(area.X * scale), (int)(area.Y * scale));
            src.X = (Graphics.GetImageWidth(chr.imgId) - src.Width) / 2;
            src.Y = (Graphics.GetImageHeight(chr.imgId) - src.Height) / 3;

            Graphics.DrawImage(chr.imgId, dest, src, color);
        }

        private void DrawSelectableBox(int sel, Vector2 origArea)
        {
            // 選択可能枠
            var selectedPos = Vector2.Zero;
            selectedPos.X += (sel % 2) * origArea.X;
            selectedPos.Y += (sel / 2) * origArea.Y;

            selectedPos.X += DIGEST_BOX_OFFSET;
            selectedPos.Y += DIGEST_BOX_OFFSET;
            origArea.X -= DIGEST_BOX_OFFSET * 2;
            origArea.Y -= DIGEST_BOX_OFFSET * 2;
            p.unselBox.Draw(selectedPos, origArea);
        }

        private void DrawSelectBox(int sel, Vector2 origArea)
        {
            // 選択枠
            var selectedPos = Vector2.Zero;
            selectedPos.X += (sel % 2) * origArea.X;
            selectedPos.Y += (sel / 2) * origArea.Y;

            selectedPos.X += DIGEST_BOX_OFFSET;
            selectedPos.Y += DIGEST_BOX_OFFSET;
            origArea.X -= DIGEST_BOX_OFFSET * 2;
            origArea.Y -= DIGEST_BOX_OFFSET * 2;
            p.selBox.Draw(selectedPos, origArea, blinker.getColor());
        }

        internal override void UpdateCallback()
        {
            if (selectable)
                base.UpdateCallback();

            // 死亡キャラ選択不可の時に死んでいるキャラが選択されたらキャンセルする
            if (disableDead && result >= 0 &&
                p.owner.parent.owner.data.party.members[result].statusAilments.HasFlag(Hero.StatusAilments.DOWN))
            {
                result = Util.RESULT_SELECTING;
            }
        }

        internal static string SystemIconImageFilePath { get { return "./res/icon/icon_system.png"; } }
        internal void CreateDic()
        {
            // TODO 変更に対応
            sStatusIconImgId = Graphics.LoadImage(
                SystemIconImageFilePath,
                Common.Resource.Icon.ICON_WIDTH,
                Common.Resource.Icon.ICON_HEIGHT);
        }
    }
}

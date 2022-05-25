using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yukar.Common.GameData;
using Yukar.Common;
using KeyStates = Yukar.Engine.Input.KeyStates;
using StateType = Yukar.Engine.Input.StateType;
using Microsoft.Xna.Framework;

namespace Yukar.Engine
{
    class MenuController
    {
        public MapScene parent;

        //private Dictionary<Guid, int> iconDic = new Dictionary<Guid, int>();
        private Common.Rom.GameSettings gs;
        internal CommonWindow.ParamSet res;

        internal enum State
        {
            HIDE,
            MAIN,
            ITEM,
            ITEMTARGET,
            SKILL,
            SKILLLIST,
            SKILLTARGET,
            EQUIP,
            EQUIPLIST,
            EQUIPITEMLIST,
            STATUS,
            STATUSLIST,
            SAVE,
            SAVE_DIRECT,
            ASKSAVE,
            ASKSAVE_DIRECT,
            LOAD,
            EXIT,
            CONFIG,
        }
        public static State state = State.HIDE;

        private MainMenu mainMenu;
        private ItemList itemList;
        private DetailWindow detail;
        private StatusDigest digest;
        private ItemNumWindow itemNum;
        private SkillList skillList;
        private EquipWindow equip;
        private EquipItemList equipItem;
        private StatusWindow status;
        private HintDisplay hint;
        private SaveFileList saveList;
        private SaveDataDetail save;
        private AskWindow ask;
        private ConfigWindow config;

        internal const int BORDER_WIDTH = 2; // 枠を二重に描画しないように少し重ねる
        internal const int WINDOW_POS_X = 12;
        internal const int WINDOW_POS_Y = 16;
        internal const int MAINMENU_POS_X = 60;
        internal const int MAINMENU_POS_Y = 40;
        internal const int MAINMENU_WIDTH = 208;
        internal const int MAINMENU_HEIGHT = 440;
        internal const int LEFT_WINDOW_WIDTH = 312;
        internal const int RIGHT_MAINWINDOW_POS_X = LEFT_WINDOW_WIDTH + WINDOW_POS_X - BORDER_WIDTH;
        internal const int RIGHT_MAINWINDOW_POS_Y = WINDOW_POS_Y + RIGHT_SUBWINDOW_HEIGHT - BORDER_WIDTH;
        internal const int RIGHT_MAINWINDOW_WIDTH = 624;
        internal const int RIGHT_MAINWINDOW_HEIGHT = 422;
        internal const int RIGHT_SUBWINDOW_HEIGHT = 66;
        internal const int RIGHT_WINDOW_TOTALHEIGHT = RIGHT_MAINWINDOW_HEIGHT + RIGHT_SUBWINDOW_HEIGHT - BORDER_WIDTH;
        internal const int WINDOW_TOTALWIDTH = LEFT_WINDOW_WIDTH + RIGHT_MAINWINDOW_WIDTH;
        internal const int ASKWINDOW_WIDTH = 640, ASKWINDOW_HEIGHT = 160;

        internal void Initialize(
            Common.Resource.Window winRes, int winImgId,
            Common.Resource.Window selRes, int selImgId,
            Common.Resource.Window unselRes, int unselImgId,
            Common.Resource.Character scrollRes, int scrollImgId)
        {
            gs = parent.owner.catalog.getGameSettings();
            res = new CommonWindow.ParamSet();
            res.gs = gs;
            res.owner = this;

            // ウィンドウの読み込み
            res.window = new WindowDrawer(winRes, winImgId);
            res.unselBox = new WindowDrawer(unselRes, unselImgId);
            res.selBox = new WindowDrawer(selRes, selImgId);
            res.textDrawer = new TextDrawer(1);
            res.pageImgId = Graphics.LoadImage("./res/system/page_icons.png");

            // 詳細表示用キャラ作成
            RefreshPartyChr();

            // 詳細表示のアップダウン表示用キャラ作成
            res.detailChr = new MapCharacter();
            var catalog = parent.owner.catalog;
            var detRes = catalog.getItemFromGuid(gs.equipEffectIcon) as Common.Resource.Character;
            if (detRes != null)
            {
                res.detailChr.ChangeGraphic(detRes, null);
            }

            // ヒント表示を初期化
            hint = new HintDisplay(res);

            // 各種座標とサイズを設定
            mainMenu = new MainMenu();
            mainMenu.Initialize(res, MAINMENU_POS_X + MAINMENU_WIDTH / 2, MAINMENU_POS_Y + MAINMENU_HEIGHT / 2, MAINMENU_WIDTH, MAINMENU_HEIGHT);
            itemList = new ItemList();
            itemList.Initialize(res, RIGHT_MAINWINDOW_POS_X + RIGHT_MAINWINDOW_WIDTH / 2, RIGHT_MAINWINDOW_POS_Y + RIGHT_MAINWINDOW_HEIGHT / 2, RIGHT_MAINWINDOW_WIDTH, RIGHT_MAINWINDOW_HEIGHT);
            detail = new DetailWindow();
            detail.Initialize(res, RIGHT_MAINWINDOW_POS_X + RIGHT_MAINWINDOW_WIDTH / 2, WINDOW_POS_Y + RIGHT_SUBWINDOW_HEIGHT / 2, RIGHT_MAINWINDOW_WIDTH, RIGHT_SUBWINDOW_HEIGHT);
            digest = new StatusDigest();
            digest.Initialize(res, RIGHT_MAINWINDOW_POS_X + RIGHT_MAINWINDOW_WIDTH / 2, RIGHT_MAINWINDOW_POS_Y + RIGHT_MAINWINDOW_HEIGHT / 2, RIGHT_MAINWINDOW_WIDTH, RIGHT_MAINWINDOW_HEIGHT);
            itemNum = new ItemNumWindow();
            itemNum.Initialize(res, WINDOW_POS_X + LEFT_WINDOW_WIDTH / 2, WINDOW_POS_Y + RIGHT_WINDOW_TOTALHEIGHT / 2, LEFT_WINDOW_WIDTH, RIGHT_WINDOW_TOTALHEIGHT);
            skillList = new SkillList();
            skillList.Initialize(res, RIGHT_MAINWINDOW_POS_X + RIGHT_MAINWINDOW_WIDTH / 2, RIGHT_MAINWINDOW_POS_Y + RIGHT_MAINWINDOW_HEIGHT / 2, RIGHT_MAINWINDOW_WIDTH, RIGHT_MAINWINDOW_HEIGHT);
            equip = new EquipWindow();
            equip.Initialize(res, RIGHT_MAINWINDOW_POS_X + RIGHT_MAINWINDOW_WIDTH / 2, RIGHT_MAINWINDOW_POS_Y + RIGHT_MAINWINDOW_HEIGHT / 2, RIGHT_MAINWINDOW_WIDTH, RIGHT_MAINWINDOW_HEIGHT);
            equipItem = new EquipItemList();
            equipItem.Initialize(res, RIGHT_MAINWINDOW_POS_X + RIGHT_MAINWINDOW_WIDTH / 2, RIGHT_MAINWINDOW_POS_Y + RIGHT_MAINWINDOW_HEIGHT / 2, RIGHT_MAINWINDOW_WIDTH, RIGHT_MAINWINDOW_HEIGHT);
            status = new StatusWindow();
            status.Initialize(res, RIGHT_MAINWINDOW_POS_X + RIGHT_MAINWINDOW_WIDTH / 2, RIGHT_MAINWINDOW_POS_Y + RIGHT_WINDOW_TOTALHEIGHT / 2, RIGHT_MAINWINDOW_WIDTH, RIGHT_WINDOW_TOTALHEIGHT);
            // 初期化のタイミングでディクショナリを生成しておかないとゲーム起動時にキャラの戦闘不能 or 毒アイコンが正しく表示されない
            status.CreateDic();
            //
            saveList = new SaveFileList();
            saveList.Initialize(res, WINDOW_POS_X + LEFT_WINDOW_WIDTH / 2, WINDOW_POS_Y + RIGHT_WINDOW_TOTALHEIGHT / 2, LEFT_WINDOW_WIDTH, RIGHT_WINDOW_TOTALHEIGHT);
            save = new SaveDataDetail();
            save.Initialize(res, RIGHT_MAINWINDOW_POS_X + RIGHT_MAINWINDOW_WIDTH / 2, WINDOW_POS_Y + RIGHT_WINDOW_TOTALHEIGHT / 2, RIGHT_MAINWINDOW_WIDTH, RIGHT_WINDOW_TOTALHEIGHT);
            config = new ConfigWindow();
            config.Initialize(res, WINDOW_POS_X + WINDOW_TOTALWIDTH / 2, WINDOW_POS_Y + RIGHT_WINDOW_TOTALHEIGHT / 2, WINDOW_TOTALWIDTH, RIGHT_WINDOW_TOTALHEIGHT);

            ask = new AskWindow();
            ask.Initialize(res, Graphics.ScreenWidth / 2, Graphics.ScreenHeight / 2, ASKWINDOW_WIDTH, ASKWINDOW_HEIGHT);
        }

        public void Reset()
        {
            res.detailChr.Reset();
            UnloadPartyFaces(res.partyChars);
        }

        internal void RefreshPartyChr()
        {
            res.partyChars = RefreshPartyChr(parent.owner.data.party, res.partyChars);
        }

        internal CommonWindow.ImageInstance[] RefreshPartyChr(Party party, CommonWindow.ImageInstance[] imageInstances)
        {
            // 新しいのを読み込む
            var result = new CommonWindow.ImageInstance[party.members.Count];
            for (int i = 0; i < party.members.Count; i++)
            {
                var grp = parent.owner.catalog.getItemFromGuid(party.getMemberFace(i)) as Common.Resource.Face;
                result[i] = new CommonWindow.ImageInstance();
                if (grp != null)
                {
                    int imgId = Graphics.LoadImage(grp.getFacePath(Common.Resource.Face.FaceType.FACE_NORMAL));
                    result[i].ChangeGraphic(grp, imgId);
                }
            }

            // 古いのを解放する
            UnloadPartyFaces(imageInstances);

            return result;
        }

        private void UnloadPartyFaces(CommonWindow.ImageInstance[] imageInstances)
        {
            if (imageInstances != null)
            {
                foreach (var chr in imageInstances)
                {
                    if (chr.imgId > 0)
                        Graphics.UnloadImage(chr.imgId);
                }
            }
        }

        internal void Update()
        {
            mainMenu.Update();
            //itemMenu.Update();
            itemList.Update();
            detail.Update();
            digest.Update();
            itemNum.Update();
            skillList.Update();
            equip.Update();
            equipItem.Update();
            status.Update();
            hint.Update();
            saveList.Update();
            save.Update();
            ask.Update();
            config.Update();
            //UnityEngine.Debug.Log(state);

            switch (state)
            {
                case State.HIDE:
                    break;
                case State.MAIN:
                    SetMenuWindowDefaultDetailText();

                    switch (mainMenu.result)
                    {
                        case Util.RESULT_CANCEL:
                            parent.UnlockControl();
                            parent.ExclusionAllEvents(null);
                            mainMenu.Hide();
                            digest.Hide();
                            hint.Hide();
                            detail.Hide();
                            itemList.ClearDic();
                            skillList.ClearDic();
                            state = State.HIDE;
                            break;
                        case MainMenu.ITEM: // アイテム
                            Audio.PlaySound(parent.owner.se.decide);
                            mainMenu.Lock();
                            digest.Hide();
                            digest.result = Util.RESULT_SELECTING;
                            state = State.ITEM;
                            itemList.Show();
                            SetMenuWindowDetailText("", null);
                            break;
                        case MainMenu.SKILL: // スキル
                            Audio.PlaySound(parent.owner.se.decide);
                            mainMenu.Lock();
                            state = State.SKILL;
                            digest.result = Util.RESULT_SELECTING;
                            digest.selectable = true;
                            digest.showReturn = true;
                            digest.disableDead = true;
                            digest.Resize();
                            detail.Hide();
                            break;
                        case MainMenu.EQUIPMENT: // 装備
                            Audio.PlaySound(parent.owner.se.decide);
                            mainMenu.Lock();
                            state = State.EQUIP;
                            digest.result = Util.RESULT_SELECTING;
                            digest.selectable = true;
                            digest.showReturn = true;
                            digest.Resize();
                            detail.Hide();
                            break;
                        case MainMenu.STATUS: // ステータス
                            Audio.PlaySound(parent.owner.se.decide);
                            mainMenu.Lock();
                            state = State.STATUS;
                            digest.result = Util.RESULT_SELECTING;
                            digest.selectable = true;
                            digest.showReturn = true;
                            digest.Resize();
                            detail.Hide();
                            break;
                        case MainMenu.SAVE: // セーブ
                            if (!res.owner.parent.owner.data.system.saveAvailable)
                            {
                                Audio.PlaySound(parent.owner.se.cancel);
                                mainMenu.result = Util.RESULT_SELECTING;
                                break;
                            }
                            Audio.PlaySound(parent.owner.se.decide);
                            mainMenu.Hide();
                            state = State.SAVE;
                            digest.Hide();
                            save.Show();
                            saveList.Show();
                            detail.Hide();
                            break;
                        case MainMenu.EXIT: // 終了確認
                            Audio.PlaySound(parent.owner.se.decide);
                            mainMenu.Lock();
                            state = State.EXIT;
                            ask.setInfo(res.gs.glossary.askExitGame, res.gs.glossary.yes, res.gs.glossary.no);
                            ask.selected = 1;
                            ask.Show();
                            break;
                        case MainMenu.CONFIG: // コンフィグ
                            Audio.PlaySound(parent.owner.se.decide);
                            mainMenu.Hide();
                            state = State.CONFIG;
                            digest.Hide();
                            config.Show();
                            detail.Hide();
                            break;
                        case MainMenu.CLOSE: // クローズ(キャンセルと同じ処理)
                            Audio.PlaySound(parent.owner.se.cancel);
                            goToClose(); //#23959-2
                            break;
                    }
                    break;
                case State.ITEM:
                    if (itemList.result == Util.RESULT_SELECTING)
                    {
                        if (itemList.selectedItem != null)
                        {
                            detail.detail = itemList.selectedItem.description;
                        }
                        else
                        {
                            detail.detail = "";
                        }
                    }
                    else if (itemList.result == Util.RESULT_CANCEL)
                    {
                        mainMenu.Unlock();
                        itemList.Hide();
                        detail.Hide();
                        goToMain();
                    }
                    else
                    {
                        if (itemList.selectedItem == null || !itemList.GetUsableOnField(itemList.selectedItem))
                        {
                            Audio.PlaySound(parent.owner.se.cancel);
                            itemList.result = Util.RESULT_SELECTING;
                            break;
                        }

                        Audio.PlaySound(parent.owner.se.decide);

                        if (isInstantItem(parent.owner.catalog, itemList.selectedItem))
                        {
                            itemNum.stack = parent.owner.data.party.items[itemList.selected];
                            UseItem(0, itemNum.stack);
                            itemList.Hide();
                            break;
                        }

                        state = State.ITEMTARGET;
                        mainMenu.Hide();
                        itemList.Hide();
                        itemList.Unlock();
                        itemNum.icon = itemList.selectedItem.icon;
                        itemNum.imgId = -1;
                        if (itemList.iconDic.ContainsKey(itemNum.icon.guId))
                            itemNum.imgId = itemList.iconDic[itemNum.icon.guId];
                        itemNum.skill = null;
                        itemNum.stack = parent.owner.data.party.items[itemList.selected];
                        itemNum.Show();
                        detail.Hide();

                        digest.showReturn = true;
                        digest.Show();
                        if (itemNum.stack.item.type == Common.Rom.ItemType.EXPENDABLE_WITH_SKILL && itemNum.stack.item.expendableWithSkill != null)
                        {
                            var skill = parent.owner.catalog.getItemFromGuid(itemNum.stack.item.expendableWithSkill.skill) as Common.Rom.Skill;
                            if (skill != null)
                                digest.allSelect = skill != null && skill.option.target == Common.Rom.TargetType.PARTY_ALL || skill.option.target == Common.Rom.TargetType.ALL;
                        }
                        digest.selectable = true;
                        break;
                    }
                    break;
                case State.ITEMTARGET:
                    if (digest.result == Util.RESULT_CANCEL)
                    {
                        digest.showReturn = false;
                        digest.Hide();
                        detail.Show();
                        itemList.Show();
                        itemNum.Hide();
                        mainMenu.Show();
                        state = State.ITEM;
                    }
                    else if (digest.result != Util.RESULT_SELECTING)
                    {
                        UseItem(digest.result, itemNum.stack);
                        digest.result = Util.RESULT_SELECTING;
                    }
                    break;
                case State.SKILL:
                    if (digest.result == Util.RESULT_CANCEL)
                    {
                        digest.selectable = false;
                        digest.showReturn = false;
                        digest.Resize();
                        mainMenu.Unlock();
                        itemList.Hide();
                        detail.Hide();
                        goToMain();
                    }
                    else if (digest.result != Util.RESULT_SELECTING)
                    {
                        Audio.PlaySound(parent.owner.se.decide);
                        skillList.heroIndex = digest.result;
                        skillList.Show();

                        mainMenu.Hide();
                        detail.Show();

                        itemNum.Show();
                        itemNum.status = digest;
                        itemNum.statusIndex = skillList.heroIndex;

                        digest.Hide();
                        state = State.SKILLLIST;
                    }
                    break;
                case State.SKILLLIST:
                    if (skillList.result == Util.RESULT_CANCEL)
                    {
                        state = State.SKILL;
                        skillList.Hide();
                        detail.Hide();
                        itemNum.Hide();
                        mainMenu.Show();
                        detail.detail = gs.glossary.skillUser;
                        digest.Show();
                        digest.disableDead = true;
                    }
                    else if (skillList.result == Util.RESULT_SELECTING)
                    {
                        if (skillList.selectedItem != null && skillList.returnSelected == 0)
                        {
                            detail.detail = Util.createSkillDescription(
                                res.owner.parent.owner.catalog, res.owner.parent.owner.data.party, skillList.selectedItem, false);
                        }
                        else
                        {
                            detail.detail = "";
                        }
                    }
                    else
                    {
                        if (skillList.selectedItem == null)
                        {
                            Audio.PlaySound(parent.owner.se.cancel);
                            skillList.result = Util.RESULT_SELECTING;
                            break;
                        }

                        if (!skillList.flags[skillList.result])
                        {
                            Audio.PlaySound(parent.owner.se.cancel);
                            skillList.result = Util.RESULT_SELECTING;
                            break;
                        }

                        Audio.PlaySound(parent.owner.se.decide);

                        if (isInstantSkill(parent.owner.catalog, skillList.selectedItem))
                        {
                            var hero = parent.owner.data.party.members[skillList.heroIndex];
                            UseSkill(hero, 0, skillList.selectedItem);
                            skillList.Hide();
                            break;
                        }

                        state = State.SKILLTARGET;
                        skillList.Hide();

                        itemNum.icon = skillList.selectedItem.icon;
                        itemNum.imgId = skillList.iconDic.ContainsKey(itemNum.icon.guId) ?
                            skillList.iconDic[itemNum.icon.guId] : -1;
                        itemNum.skill = skillList.selectedItem;

                        detail.Hide();

                        digest.Show();
                        digest.allSelect =
                            skillList.selectedItem.option.target == Common.Rom.TargetType.PARTY_ALL ||
                            skillList.selectedItem.option.target == Common.Rom.TargetType.PARTY_ALL_ENEMY_ONE ||
                            skillList.selectedItem.option.target == Common.Rom.TargetType.ALL ||
                            skillList.selectedItem.option.target == Common.Rom.TargetType.OTHERS ||
                            skillList.selectedItem.option.target == Common.Rom.TargetType.OTHERS_ENEMY_ONE ||
                            skillList.selectedItem.option.target == Common.Rom.TargetType.OTHERS_ALL ||
                            skillList.selectedItem.option.target == Common.Rom.TargetType.SELF ||
                            skillList.selectedItem.option.target == Common.Rom.TargetType.SELF_ENEMY_ALL ||
                            skillList.selectedItem.option.target == Common.Rom.TargetType.SELF_ENEMY_ONE;

                        if (
                            skillList.selectedItem.option.target == Common.Rom.TargetType.SELF ||
                            skillList.selectedItem.option.target == Common.Rom.TargetType.SELF_ENEMY_ALL ||
                            skillList.selectedItem.option.target == Common.Rom.TargetType.SELF_ENEMY_ONE)
                        {
                            digest.selectableStates = new bool[] { false, false, false, false };
                            digest.selectableStates[skillList.heroIndex] = true;
                        }
                        else if (
                            skillList.selectedItem.option.target == Common.Rom.TargetType.OTHERS ||
                            skillList.selectedItem.option.target == Common.Rom.TargetType.OTHERS_ENEMY_ONE ||
                            skillList.selectedItem.option.target == Common.Rom.TargetType.OTHERS_ALL)
                        {
                            digest.selectableStates[skillList.heroIndex] = false;
                        }

                        digest.selectable = true;
                        break;
                    }
                    break;
                case State.SKILLTARGET:
                    {
                        var hero = parent.owner.data.party.members[skillList.heroIndex];
                        itemNum.restMp =
                            hero.hitpoint <= skillList.selectedItem.option.consumptionHitpoint ||
                            hero.magicpoint < skillList.selectedItem.option.consumptionMagicpoint ||
                            !parent.owner.data.party.isOKToConsumptionItem(skillList.selectedItem.option.consumptionItem, skillList.selectedItem.option.consumptionItemAmount);
                    }

                    if (digest.result == Util.RESULT_CANCEL)
                    {
                        digest.Hide();
                        skillList.Show();
                        detail.Show();

                        itemNum.skill = null;

                        state = State.SKILLLIST;
                    }
                    else if (digest.result != Util.RESULT_SELECTING)
                    {
                        if (!itemNum.restMp)
                        {
                            var hero = parent.owner.data.party.members[skillList.heroIndex];
                            UseSkill(hero, digest.result, skillList.selectedItem);
                            digest.result = Util.RESULT_SELECTING;
                        }
                        else
                        {
                            Audio.PlaySound(parent.owner.se.cancel);
                            digest.result = Util.RESULT_SELECTING;
                        }
                    }
                    break;
                case State.EQUIP:
                    if (digest.result == Util.RESULT_CANCEL)
                    {
                        digest.selectable = false;
                        digest.showReturn = false;
                        digest.Resize();
                        mainMenu.Unlock();
                        goToMain();
                    }
                    else if (digest.result != Util.RESULT_SELECTING)
                    {
                        Audio.PlaySound(parent.owner.se.decide);
                        mainMenu.Hide();

                        equip.iconDic = itemList.iconDic;
                        equip.heroIndex = digest.result;
                        equip.Show();

                        itemNum.Show();
                        itemNum.showStatusDetail = true;
                        itemNum.status = digest;
                        itemNum.statusIndex = equip.heroIndex;

                        digest.Hide();

                        detail.Show();
                        detail.detail = "";
                        state = State.EQUIPLIST;
                    }
                    break;
                case State.EQUIPLIST:
                    if (equip.result == Util.RESULT_CANCEL)
                    {
                        state = State.EQUIP;
                        equip.Hide();
                        digest.Show();

                        detail.Hide();
                        itemNum.Hide();
                        mainMenu.Show();
                    }
                    else if (equip.result == Util.RESULT_SELECTING)
                    {
                        if (Input.KeyTest(StateType.REPEAT, KeyStates.LEFT))
                        {
                            equip.heroIndex--;
                            if (equip.heroIndex < 0)
                                equip.heroIndex = parent.owner.data.party.members.Count - 1;
                            equip.applyHeroIndex();
                            itemNum.statusIndex = equip.heroIndex;
                            break;
                        }
                        else if (Input.KeyTest(StateType.REPEAT, KeyStates.RIGHT))
                        {
                            equip.heroIndex++;
                            if (parent.owner.data.party.members.Count <= equip.heroIndex)
                                equip.heroIndex = 0;
                            equip.applyHeroIndex();
                            itemNum.statusIndex = equip.heroIndex;
                            break;
                        }

                        var hero = parent.owner.data.party.members[equip.heroIndex];
                        var item = hero.equipments[equip.selected];
                        if (item != null && equip.returnSelected == 0)
                        {
                            detail.detail = Util.createEquipmentDescription(gs, item);
                        }
                        else
                        {
                            detail.detail = "";
                        }
                    }
                    else
                    {
                        var hero = parent.owner.data.party.members[equip.heroIndex];

                        if (hero.rom.fixEquipments[equip.result])
                        {
                            Audio.PlaySound(parent.owner.se.cancel);
                            equip.result = Util.RESULT_SELECTING;
                            break;
                        }

                        Audio.PlaySound(parent.owner.se.decide);
                        var item = hero.equipments[equip.selected];

                        //equipDetail.sourceItem = item;

                        state = State.EQUIPITEMLIST;

                        var type = Common.Rom.ItemType.WEAPON;
                        switch (equip.selected)
                        {
                            case 1:
                                type = Common.Rom.ItemType.SHIELD;
                                break;
                            case 2:
                                type = Common.Rom.ItemType.HEAD;
                                break;
                            case 3:
                                type = Common.Rom.ItemType.ARMOR;
                                break;
                            case 4:
                                type = Common.Rom.ItemType.ACCESSORY;
                                break;
                            case 5:
                                type = Common.Rom.ItemType.ACCESSORY;
                                break;
                            case 6:
                                type = Common.Rom.ItemType.ACCESSORY;
                                break;
                        }

                        if (item != null)
                        {
                            itemNum.icon = item.icon;
                            if (equip.iconDic.ContainsKey(itemNum.icon.guId))
                            {
                                itemNum.imgId = equip.iconDic[itemNum.icon.guId];
                            }
                            itemNum.stack = new Party.ItemStack();
                            itemNum.stack.item = hero.equipments[equip.selected];
                            itemNum.stack.num = -1;
                            itemNum.showDescription = false;
                        }

                        equipItem.iconDic = itemList.iconDic;
                        equipItem.hero = hero.rom;
                        equipItem.targetType = type;
                        equipItem.Show();

                        equip.Hide();
                    }
                    break;
                case State.EQUIPITEMLIST:
                    if (equipItem.result == Util.RESULT_CANCEL)
                    {
                        equip.Show();
                        state = State.EQUIPLIST;
                        equipItem.Hide();
                        itemNum.stack = null;
                        itemNum.showDiff = false;
                    }
                    else if (equipItem.result == Util.RESULT_SELECTING)
                    {
                        itemNum.showDiff = equipItem.windowState == CommonWindow.WindowState.SHOW_WINDOW && equipItem.returnSelected == 0;

                        var item = equipItem.selectedItem;
                        if (item != null && equipItem.returnSelected == 0)
                        {
                            detail.detail = Util.createEquipmentDescription(gs, item);
                        }
                        else
                        {
                            detail.detail = "";
                        }

                        // 装備可能アイテムではなかった時は、装備効果を表示しない(もともと装備しているアイテムと同じものを比較する)
                        if (equipItem.flags.Length <= equipItem.selected || !equipItem.flags[equipItem.selected])
                        {
                            var hero = parent.owner.data.party.members[equip.heroIndex];
                            item = hero.equipments[equip.selected];
                        }
                        itemNum.destItem = item;
                    }
                    else if (equipItem.flags[equipItem.result])
                    {
                        var hero = parent.owner.data.party.members[equip.heroIndex];
                        var item = hero.equipments[equip.selected];
                        if (item != null &&
                            (parent.owner.data.party.GetItemNum(item.guId) >= item.maxNum || // アイテムがすでに上限の個数ある
                            (parent.owner.data.party.GetItemNum(item.guId) == 0 &&
                             parent.owner.data.party.checkInventoryEmptyNum() == 0)) && // アイテム袋がいっぱい
                            (equipItem.selectedItem == null || item.guId != equipItem.selectedItem.guId))
                        {
                            // 持ちきれなかったのでキャンセル
                            Audio.PlaySound(parent.owner.se.cancel);
                            parent.ShowToast(gs.glossary.inventoryFullRemoveEquip);
                        }
                        else
                        {
                            // 装備変更
                            Audio.PlaySound(parent.owner.se.decide);
                            hero.equipments[equip.selected] = equipItem.selectedItem;
                            hero.refreshEquipmentEffect();

                            // アイテムの数を変更
                            // (同じアイテムが選ばれている可能性があるので、外すほうが先)
                            if (equipItem.selectedItem != null)
                            {
                                parent.owner.data.party.AddItem(equipItem.selectedItem.guId, -1);
                            }
                            if (item != null)
                            {
                                parent.owner.data.party.AddItem(item.guId, 1);
                            }
                        }

                        equip.Show();
                        state = State.EQUIPLIST;
                        equipItem.Hide();
                        itemNum.stack = null;
                        itemNum.showDiff = false;
                    }
                    else
                    {
                        Audio.PlaySound(parent.owner.se.cancel);
                        equipItem.result = Util.RESULT_SELECTING;
                    }
                    break;
                case State.STATUS:
                    if (digest.result == Util.RESULT_CANCEL)
                    {
                        mainMenu.Unlock();

                        digest.selectable = false;
                        digest.showReturn = false;
                        digest.Resize();
                        detail.Hide();
                        goToMain();
                    }
                    else if (digest.result != Util.RESULT_SELECTING)
                    {
                        Audio.PlaySound(parent.owner.se.decide);
                        mainMenu.Hide();

                        status.iconDic = itemList.iconDic;
                        status.heroIndex = digest.result;
                        status.Show();

                        hint.Show(HintDisplay.HintType.STATUS);

                        digest.Hide();
                        detail.Hide();
                        state = State.STATUSLIST;
                    }
                    break;
                case State.STATUSLIST:
                    var rect = status.getReturnBoxRect();
                    var pos = new Vector2(rect[0].X, rect[0].Y);
                    var size = new Vector2(rect[1].X, rect[1].Y);
                    var offsetX = (int)status.windowPos.X - status.innerWidth / 2;
                    var offsetY = (int)status.windowPos.Y - status.innerHeight / 2;
                    pos = new Vector2(pos.X + offsetX, pos.Y + offsetY);
                    SharpKmyMath.Vector2 touchPos = new SharpKmyMath.Vector2();
#if WINDOWS
#else
                    if (UnityEngine.Input.GetMouseButtonDown(0)) {
                        touchPos = InputCore.getTouchPos(0);
                    }
#endif
                    if (status.result == Util.RESULT_SELECTING
                    && pos.X < touchPos.x && touchPos.x < pos.X + size.X && pos.Y < touchPos.y && touchPos.y < pos.Y + size.Y)
                    {
                        status.result = Util.RESULT_CANCEL;
                        Audio.PlaySound(parent.owner.se.cancel);
                    }

                    if (status.result != Util.RESULT_SELECTING)
                    {
                        mainMenu.Show();

                        status.Hide();
                        state = State.STATUS;
                        digest.Show();

                        hint.Show(HintDisplay.HintType.DEFAULT);
                    }
                    break;
                case State.SAVE:
                case State.SAVE_DIRECT:
                    if (saveList.returnSelected != 0)
                        save.CurrentIndex = -1;
                    else
                        save.CurrentIndex = saveList.selected;

                    if (saveList.result != Util.RESULT_SELECTING)
                    {
                        if (saveList.result == Util.RESULT_CANCEL)
                        {
                            save.Hide();
                            saveList.Hide();

                            if (state == State.SAVE_DIRECT)
                            {
                                Audio.PlaySound(parent.owner.se.cancel);
                                parent.UnlockControl();
                                parent.ExclusionAllEvents(null);
                                state = State.HIDE;
                            }
                            else
                            {
                                mainMenu.Show();
                                goToMain();
                            }
                        }
                        else
                        {
                            if (save.isExisted())
                            {
                                Audio.PlaySound(parent.owner.se.decide);
                                if (state == State.SAVE_DIRECT)
                                    state = State.ASKSAVE_DIRECT;
                                else
                                    state = State.ASKSAVE;
                                saveList.Lock();
                                saveList.result = Util.RESULT_SELECTING;
                                ask.setInfo(res.gs.glossary.askOverwrite, res.gs.glossary.yes, res.gs.glossary.no);
                                ask.Show();
                            }
                            else
                            {
                                Audio.PlaySound(parent.owner.se.item);
                                save.DoSave();
                                saveList.refreshList();
                                saveList.result = Util.RESULT_SELECTING;
                            }
                        }
                    }
                    break;
                case State.ASKSAVE:
                case State.ASKSAVE_DIRECT:
                    if (ask.result != Util.RESULT_SELECTING)
                    {
                        ask.Hide();

                        if (ask.result == AskWindow.RESULT_OK)
                        {
                            Audio.PlaySound(parent.owner.se.item);
                            save.DoSave();
                            saveList.refreshList();
                            saveList.Unlock();
                            if (state == State.ASKSAVE_DIRECT)
                                state = State.SAVE_DIRECT;
                            else
                                state = State.SAVE;
                        }
                        else
                        {
                            Audio.PlaySound(parent.owner.se.cancel);
                            saveList.Unlock();
                            if (state == State.ASKSAVE_DIRECT)
                                state = State.SAVE_DIRECT;
                            else
                                state = State.SAVE;
                        }
                    }
                    break;
                case State.EXIT:
                    if (ask.result != Util.RESULT_SELECTING)
                    {
                        ask.Hide();

                        if (ask.result == AskWindow.RESULT_OK)
                        {
                            Audio.PlaySound(parent.owner.se.decide);
                            goToMain();
                            goToClose();//#23959-2
                            parent.ReservationChangeScene(GameMain.Scenes.TITLE);
                        }
                        else
                        {
                            Audio.PlaySound(parent.owner.se.cancel);
                            mainMenu.Unlock();
                            goToMain();
                        }

                        SetMenuWindowDefaultDetailText();
                    }
                    break;
                case State.CONFIG:
                    if (config.result != Util.RESULT_SELECTING)
                    {
                        if (config.result == ConfigWindow.RESTORE_INDEX)
                        {
                            // 初期設定に戻す
                            Audio.PlaySound(parent.owner.se.item);
                            config.RestoreDefaults();
                            config.result = Util.RESULT_SELECTING;
                        }
                        else if (config.result == Util.RESULT_CANCEL)
                        {
                            config.Apply();
                            config.Hide();
                            mainMenu.Show();
                            digest.Show();
                            goToMain();
                        }
                        else
                        {
                            config.result = Common.Util.RESULT_SELECTING;
#if WINDOWS
                            //configタップ対応のため、windowsのみ実行
                            Audio.PlaySound(parent.owner.se.cancel);
                            config.Apply();
                            config.Hide();
                            mainMenu.Show();
                            digest.Show();
                            goToMain();
#endif
                        }
                    }
                    break;
            }
        }

        internal static bool isInstantItem(Catalog catalog, Common.Rom.Item item)
        {
            if (item.type != Common.Rom.ItemType.EXPENDABLE_WITH_SKILL)
                return false;

            // 敵味方ともに効果なし・マップで使用可能な場合にtrue
            var skill = catalog.getItemFromGuid(item.expendableWithSkill.skill) as Common.Rom.Skill;
            return isInstantSkill(catalog, skill);
        }

        internal static bool isInstantSkill(Catalog catalog, Common.Rom.Skill skill)
        {
            // 敵味方ともに効果なし・マップで使用可能な場合にtrue
            var ev = catalog.getItemFromGuid(skill.option.commonExec) as Common.Rom.Event;
            return skill.option.target == Common.Rom.TargetType.NONE && ev != null;
        }

        private void SetMenuWindowDefaultDetailText()
        {
            SetMenuWindowDetailText(
                detail.detail = parent.map.name,
                res.gs.glossary.money + " : " + parent.owner.data.party.GetMoney() + res.gs.glossary.moneyName + "\n" +
                res.gs.glossary.playTime + " " + parent.owner.data.system.GetPlayTime()
            );
        }

        private void SetMenuWindowDetailText(string leftText, string rightText)
        {
            detail.detail = leftText;
            detail.detail_right = rightText;
        }

        private void UseSkill(Common.GameData.Hero user, int target, Common.Rom.Skill skill)
        {
            var hero = parent.owner.data.party.members[target];

            bool effected = false;

            // スキルの効果対象がいるか検索する
            // 検索と同時に魔法もかけるようにした場合、先にHP,MPを減らすと常に魔法が発動するので先に使えるかどうか判定しておく
            if (digest.allSelect)
            {
                int index = 0;
                foreach (var he in parent.owner.data.party.members)
                {
                    if (digest.selectableStates[index] && canUseSkillEffect(user, he, skill))
                    {
                        effected = true;
                    }
                    index++;
                }
            }
            else
            {
                if (canUseSkillEffect(user, hero, skill))
                {
                    effected = true;
                }
            }
            // 効果対象がいない場合終了
            if(!effected)
            {
                Audio.PlaySound(parent.owner.se.cancel);
                return;
            }

            // 効果発動
            // mpとhp減少
            user.hitpoint -= skill.option.consumptionHitpoint;
            user.magicpoint -= skill.option.consumptionMagicpoint;
            parent.owner.data.party.AddItem(skill.option.consumptionItem, -skill.option.consumptionItemAmount);
            Audio.PlaySound(parent.owner.se.skill);
            if (digest.allSelect)
            //if (skill.option.target == Common.Rom.TargetType.ALL || skill.option.target == Common.Rom.TargetType.PARTY_ALL)
            {
                int index = 0;
                foreach (var he in parent.owner.data.party.members)
                {
                    if (digest.selectableStates[index] && SkillEffect(user, he, skill))
                    {
                        effected = true;
                    }
                    index++;
                }
            }
            else
            {
                if (SkillEffect(user, hero, skill))
                {
                    effected = true;
                }
            }
            return;
        }

        private bool UseItem(int target, Party.ItemStack itemStack)
        {
            var hero = parent.owner.data.party.members[target];

            // 数が 0 だったら使えない
            if (itemNum.stack.num == 0)
                return false;

            bool effected = false;

            // 効果発動
            if (itemStack.item.type == Common.Rom.ItemType.EXPENDABLE)
            {
                var effect = itemStack.item.expendable;

                if (effect.recoveryDown)
                {
                    // 蘇生機能つきのアイテムは死んでいるキャラにしか使えない
                    if (hero.statusAilments.HasFlag(Hero.StatusAilments.DOWN))
                    {
                        hero.statusAilments &= ~Hero.StatusAilments.DOWN;
                        hero.hitpoint = 1;
                        effected = true;
                    }
                    else
                    {
                        return false;
                    }
                }

                if (!hero.statusAilments.HasFlag(Hero.StatusAilments.DOWN))
                {
                    int healHp = 0;
                    healHp += effect.hitpoint;
                    healHp += hero.maxHitpoint * effect.hitpointPercent / 100;
                    if (healHp > 0 && effected) healHp--;  // 先に蘇生して HP を 1 にしたぶんを引く

                    if (healHp > 0 && hero.hitpoint < hero.maxHitpoint)
                    {
                        hero.hitpoint += healHp;
                        effected = true;
                    }

                    int healMp = 0;
                    healMp += effect.magicpoint;
                    healMp += hero.maxMagicpoint * effect.magicpointPercent / 100;

                    if (healMp > 0 && hero.magicpoint < hero.maxMagicpoint)
                    {
                        hero.magicpoint += healMp;
                        effected = true;
                    }
                }

                if (effect.power > 0)
                {
                    effected = true;
                    hero.power += effect.power;
                }
                if (effect.vitality > 0)
                {
                    effected = true;
                    hero.vitality += effect.vitality;
                }
                if (effect.magic > 0)
                {
                    effected = true;
                    hero.magic += effect.magic;
                }
                if (effect.speed > 0)
                {
                    effected = true;
                    hero.speed += effect.speed;
                }
                if (effect.maxHitpoint > 0)
                {
                    effected = true;
                    hero.maxHitpoint += effect.maxHitpoint;
                }
                if (effect.maxMagitpoint > 0)
                {
                    effected = true;
                    hero.maxMagicpoint += effect.maxMagitpoint;
                }

                if (effect.recoveryPoison && hero.statusAilments.HasFlag(Hero.StatusAilments.POISON))
                {
                    hero.statusAilments &= ~Hero.StatusAilments.POISON;
                    effected = true;
                }

                hero.consistency();
                hero.refreshEquipmentEffect();

                if (effect.commonExec != Guid.Empty)
                {
                    var runner = parent.ExecCommon(effect.commonExec);
                    if (runner != null)
                    {
                        goToClose();
                        itemNum.Hide();
                        mainMenu.Unlock();
                        digest.showReturn = false;
                        digest.selectableStates = new bool[4] { true, true, true, true };
                        effected = true;
                    }
                }
            }
            else if (itemStack.item.type == Common.Rom.ItemType.EXPENDABLE_WITH_SKILL)
            {
                var skill = parent.owner.catalog.getItemFromGuid(itemStack.item.expendableWithSkill.skill) as Common.Rom.Skill;
                if (skill != null)
                {
                    if (skill.option.target == Common.Rom.TargetType.ALL || skill.option.target == Common.Rom.TargetType.PARTY_ALL)
                    {
                        foreach (var he in parent.owner.data.party.members)
                        {
                            if (SkillEffect(null, he, skill))
                            {
                                effected = true;
                            }
                        }
                    }
                    else
                    {
                        if (SkillEffect(null, hero, skill))
                        {
                            effected = true;
                        }
                    }
                }
            }

            if (effected)
            {
                Audio.PlaySound(parent.owner.se.item);

                itemStack.num--;

                // 数が 0 になったらアイテム一覧から削除
                if (itemStack.num == 0)
                {
                    parent.owner.data.party.items.Remove(itemStack);
                }
            }
            else
            {
                Audio.PlaySound(parent.owner.se.cancel);
            }

            return effected;
        }

        private bool canUseSkillEffect(Hero user, Hero hero, Common.Rom.Skill skill)
        {
            bool effected = false;

            if (skill.friendEffect != null)
            {
                var effect = skill.friendEffect;
                var isDown = hero.statusAilments.HasFlag(Hero.StatusAilments.DOWN);

                if (effect.down)
                {
                    // 蘇生機能つきのスキルは死んでいるキャラにしか使えない
                    if (isDown)
                    {
                        effected = true;
                        isDown = false;
                    }
                    else if (skill.option.onlyForDown)
                    {
                        return false;
                    }
                }

                if (!isDown)
                {
                    int healHp = 0;
                    healHp += effect.hitpoint;
                    healHp += hero.maxHitpoint * effect.hitpointPercent / 100;

                    if (user != null)
                    {
                        healHp += user.magic * effect.hitpoint_magicParcent / 100;
                        healHp += user.power * effect.hitpoint_powerParcent / 100;
                        healHp += (int)EvalFormula(effect.hitpointFormula, user, hero, skill.friendEffect.attribute);
                    }
                    if (healHp > 0 && effected) healHp--;  // 先に蘇生して HP を 1 にしたぶんを引く

                    if (healHp > 0 && hero.hitpoint < hero.maxHitpoint)
                    {
                        effected = true;
                    }

                    int healMp = 0;
                    healMp += effect.magicpoint;
                    healMp += hero.maxMagicpoint * effect.magicpointPercent / 100;

                    if (healMp > 0 && hero.magicpoint < hero.maxMagicpoint)
                    {
                        effected = true;
                    }
                }

                if (effect.poison && hero.statusAilments.HasFlag(Hero.StatusAilments.POISON))
                {
                    effected = true;
                }
            }

            if (skill.option.commonExec != Guid.Empty)
            {
                var runner = parent.GetScriptRunner(skill.option.commonExec);
                if (runner != null)
                {
                    effected = true;
                }
            }

            return effected;
        }

        private float EvalFormula(string formula, Hero user, Hero target, int attackAttribute)
        {
            // 式をパースして部品に分解する
            var words = Util.ParseFormula(formula);

            // 逆ポーランド記法に並べ替える
            words = Util.SortToRPN(words);

            return CalcRPN(words, user, target, attackAttribute);
        }

        public float CalcRPN(List<string> words, Hero user, Hero target, int attackAttribute)
        {
            var stack = new Stack<float>();
            stack.Push(0);

            float a;
            foreach (var word in words)
            {
                switch (word)
                {
                    case "min":
                        stack.Push(Math.Min(stack.Pop(), stack.Pop()));
                        break;
                    case "max":
                        stack.Push(Math.Max(stack.Pop(), stack.Pop()));
                        break;
                    case "rand":
                        stack.Push(parent.GetRandom(stack.Pop(), stack.Pop()));
                        break;
                    case "*":
                        stack.Push(stack.Pop() * stack.Pop());
                        break;
                    case "/":
                        a = stack.Pop();
                        try
                        {
                            stack.Push(stack.Pop() / a);
                        }
                        catch (DivideByZeroException e)
                        {
#if WINDOWS
                            System.Windows.Forms.MessageBox.Show(e.Message);
#else
                            UnityEngine.Debug.Log(e.Message);
#endif
                            stack.Push(0);
                        }
                        break;
                    case "%":
                        a = stack.Pop();
                        try
                        {
                            stack.Push(stack.Pop() % a);
                        }
                        catch (DivideByZeroException e)
                        {
#if WINDOWS
                            System.Windows.Forms.MessageBox.Show(e.Message);
#else
                            UnityEngine.Debug.Log(e.Message);
#endif
                            stack.Push(0);
                        }
                        break;
                    case "+":
                        stack.Push(stack.Pop() + stack.Pop());
                        break;
                    case "-":
                        a = stack.Pop();
                        stack.Push(stack.Pop() - a);
                        break;
                    case ",":
                        break;
                    default:
                        // 数値や変数はスタックに積む

                        float num;
                        if (float.TryParse(word, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out num))
                        {
                            // 数値
                            stack.Push(num);
                        }
                        else
                        {
                            // 変数
                            stack.Push(parseBattleNum(word, user, target, attackAttribute));
                        }
                        break;
                }
            }

            return stack.Pop();
        }

        private float parseBattleNum(string word, Hero user, Hero target, int attackAttribute)
        {
            Hero src = null;
            if (word.StartsWith("a."))
                src = user;
            else if (word.StartsWith("b."))
                src = target;

            if (src != null)
            {
                word = word.Substring(2, word.Length - 2);

                switch (word)
                {
                    case "hp":
                        return src.hitpoint;
                    case "mp":
                        return src.magicpoint;
                    case "mhp":
                        return src.maxHitpoint;
                    case "mmp":
                        return src.maxMagicpoint;
                    case "atk":
                        return src.power;
                    case "def":
                        return src.vitality;
                    case "spd":
                        return src.speed;
                    case "mgc":
                        return src.magic;
                    case "eatk":
                        return src.equipmentEffect.elementAttack;
                    case "edef":
                        if (attackAttribute < 0 || src.equipmentEffect.elementDefense.Length <= attackAttribute)
                            return 1;
                        return 1.0f - (float)src.equipmentEffect.elementDefense[attackAttribute] / 100;
                }
            }

            return 0;
        }

        private bool SkillEffect(Hero user, Hero hero, Common.Rom.Skill skill)
        {
            bool effected = false;

            if (skill.friendEffect != null)
            {
                var effect = skill.friendEffect;
                var isDown = hero.statusAilments.HasFlag(Hero.StatusAilments.DOWN);

                if (effect.down)
                {
                    // 蘇生機能つきのスキルは死んでいるキャラにしか使えない
                    if (isDown)
                    {
                        hero.statusAilments &= ~Hero.StatusAilments.DOWN;
                        hero.hitpoint = 1;
                        effected = true;
                        isDown = false;
                    }
                    else if (skill.option.onlyForDown)
                    {
                        return false;
                    }
                }

                if (!isDown)
                {
                    int healHp = 0;
                    healHp += effect.hitpoint;
                    healHp += hero.maxHitpoint * effect.hitpointPercent / 100;
                    if (user != null)
                    {
                        healHp += user.magic * effect.hitpoint_magicParcent / 100;
                        healHp += user.power * effect.hitpoint_powerParcent / 100;
                        healHp += (int)EvalFormula(effect.hitpointFormula, user, hero, effect.attribute);
                    }
                    if (healHp > 0 && effected) healHp--;  // 先に蘇生して HP を 1 にしたぶんを引く

                    if (healHp > 0 && hero.hitpoint < hero.maxHitpoint)
                    {
                        hero.hitpoint += healHp;
                        effected = true;
                    }

                    int healMp = 0;
                    healMp += effect.magicpoint;
                    healMp += hero.maxMagicpoint * effect.magicpointPercent / 100;

                    if (healMp > 0 && hero.magicpoint < hero.maxMagicpoint)
                    {
                        hero.magicpoint += healMp;
                        effected = true;
                    }
                }

                if (effect.poison && hero.statusAilments.HasFlag(Hero.StatusAilments.POISON))
                {
                    hero.statusAilments &= ~Hero.StatusAilments.POISON;
                    effected = true;
                }

                hero.consistency();
            }

            if (skill.option.commonExec != Guid.Empty && state != State.HIDE)
            {
                var runner = parent.ExecCommon(skill.option.commonExec);
                if (runner != null)
                {
                    goToClose();
                    itemNum.Hide();
                    mainMenu.Unlock();
                    digest.showReturn = false;
                    digest.selectableStates = new bool[4] { true, true, true, true };
                    effected = true;
                }
            }

            return effected;
        }

        private void goToMain()
        {
            state = State.MAIN;
            mainMenu.Show();
            digest.Show();
            digest.selectable = false;
            detail.Show();
        }

        internal void goToClose()
        {
            parent.UnlockControl();
            parent.ExclusionAllEvents(null);
            mainMenu.Hide();
            digest.Hide();
            hint.Hide();
            itemList.ClearDic();
            skillList.ClearDic();
            detail.Hide();
            state = State.HIDE;
        }

        internal void Draw()
        {
            mainMenu.Draw();
            itemList.Draw();
            detail.Draw();
            //itemMenu.Draw();
            digest.Draw();
            itemNum.Draw();
            skillList.Draw();
            equip.Draw();
            equipItem.Draw();
            status.Draw();
#if WINDOWS
            hint.Draw();
#endif
            save.Draw();
            saveList.Draw();
            ask.Draw();
            config.Draw();
        }

        internal void Show()
        {
            parent.LockControl();
            parent.ExclusionAllEvents();
            itemList.CreateDic(parent.owner.data.party.items);
            itemList.CreateDic(parent.owner.data.party.members);
            skillList.CreateDic(parent.owner.data.party.members);
            status.CreateDic();
            goToMain();
            hint.Show(HintDisplay.HintType.DEFAULT);
            Audio.PlaySound(parent.owner.se.item);
        }

        internal void ShowSaveScreen()
        {
            parent.LockControl();
            parent.ExclusionAllEvents();
            state = State.SAVE_DIRECT;
            save.Show();
            saveList.Show();
            Audio.PlaySound(parent.owner.se.item);
        }

        internal bool IsClosing()
        {
            return mainMenu.windowState == CommonWindow.WindowState.CLOSING_WINDOW;
        }

        internal bool isVisible()
        {
            return state != State.HIDE;
        }
    }
}

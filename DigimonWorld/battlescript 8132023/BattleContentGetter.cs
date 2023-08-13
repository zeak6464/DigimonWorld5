using System;
using System.Collections.Generic;
using System.Linq;
using Yukar.Common;
using Yukar.Common.Resource;
using Yukar.Common.Rom;
using Yukar.Engine;
using Yukar.Engine.GameContentCreatorSub;
using static Yukar.Engine.BattleEnum;
using static Yukar.Engine.GameContentCreator;

namespace Yukar.Battle
{
    /// <summary>
    /// レイアウト用・バトル中の様々な情報を取得してバトルレイアウトに受け渡すクラス
    /// A class for layouts that acquires various information during the battle and passes it to the battle layout.
    /// </summary>
    class BattleContentGetter : BattleContentGetterBase
    {
        public override string GetContent(GameContentParser.CachedResult.Content content, AbstractRenderObject.GameContent gameContent,
            ref bool needChangeSize, GameMain gameMain, Common.Catalog catalog, bool isPreview,
            GameContentCreator owner, RenderContent result, ref GameContentParser.StatusUpDown refStatusUpDown)
        {
            var battle = BattleSequenceManagerBase.Get() as BattleSequenceManager;
            if (battle == null || !(gameMain?.mapScene?.isBattle ?? false))
                return null;

            // 置き換えに使うゲーム情報
            // Game information used for replacement
            #region
            var index = owner.ContentIndex + owner.OnePageContenAmount * owner.ContentPage;
            if (index < 0) index = 0;

            var battleCommands = battle.battleViewer.battleCommandChoiceWindowDrawer;
            var choices = battleCommands?.GetChoicesData() ?? new List<ChoiceWindowDrawer.ChoiceItemData>();

            var battleItems = battle.battleViewer.itemSelectWindowDrawer;
            var items = battleItems?.GetChoicesData() ?? new List<ChoiceWindowDrawer.ChoiceItemData>();
            #endregion

            switch (content.type)
            {
                // バトルコマンド関連
                // Battle command related
                #region BATTLECOMMAND
                case GameContentParser.ContentType.COMMANDNAME:
                {
                    if (choices.Count > index)
                    {
                        return choices[index].text;
                    }
                    else
                    {
                        return "";
                    }
                }
                case GameContentParser.ContentType.COMMANDIMAGE:
                {
                    if (choices.Count > index && choices[index].iconRef != null)
                    {
                        result.ReserveImageIcon = choices[index].iconRef;
                        result.ReserveIconImageId = catalog.getItemFromGuid(choices[index].iconRef.guId) as Texture;

                        if (result.ReserveIconImageId != null)
                        {
                            Graphics.LoadImage(result.ReserveIconImageId);
                        }
                    }
                    else if (choices.Count > index && choices[index].imageId != null)
                    {
                        result.ReserveImageId = choices[index].imageId;
                        if (result.ReserveImageId != null)
                        {
                            Graphics.LoadImage(result.ReserveImageId);
                        }
                    }
                    return "";
                }
                case GameContentParser.ContentType.CURRENTCOMMANDNAME:
                {
                    if (gameContent.ItemStack != null)
                    {
                        return gameContent.ItemStack.item.name;
                    }
                    if (gameContent.Skill != null)
                    {
                        return gameContent.Skill.name;
                    }
                    if (gameContent.ChoiceItemData != null)
                    {
                        return gameContent.ChoiceItemData.Value.text;
                    }
                    return "";
                }
                #endregion
                // バトルエネミー関連
                // Battle enemy related
                #region BATTLEENEMY
                case GameContentParser.ContentType.ENEMYNAME:
                {
                    var heros = battle.EnemyViewDataList;
                    if (heros != null && heros.Count > index)
                    {
                        var partyName = battle.EnemyViewDataList[index].Name;
                        return partyName;
                    }
                    else
                    {
                        return "";
                    }
                }
                case GameContentParser.ContentType.ENEMYSTATUS_A:
                {
                    var heros = battle.EnemyViewDataList;

                    if (heros != null && heros.Count > index)
                    {
                        var hero = heros[index];
                        var rewardsRate = Catalog.sInstance.getGameSettings().useLevelDependenceOfBattleRewards ? hero.monsterGameData.level : 1;
                        var status = new int[] {
                            hero.monsterGameData.level,
                            hero.MaxHitPoint,
                            hero.battleStatusData.HitPoint,
                            hero.MaxMagicPoint,
                            hero.battleStatusData.MagicPoint,
                            hero.Power,
                            hero.VitalityBase,
                            hero.Magic,
                            hero.Speed,
                            hero.monster.exp * rewardsRate,
                            hero.monster.levelUpExpList[hero.monsterGameData.level - 1] - hero.monsterGameData.exp,
                            hero.Attack,
                            hero.ElementAttack,
                            hero.Defense,
                            Math.Min(100, hero.Dexterity),
                            Math.Min(100, hero.Evasion),
                            Math.Min(100, hero.Critical),
                            hero.monster.money * rewardsRate};

                        // LV99のときは数値を返さない
                        // Does not return a number when LV99
                        if (content.attrs[0] == 10 && hero.monsterGameData.level == Common.GameData.Hero.MAX_LEVEL)
                            return "-";

                        if (status.Length > content.attrs[0])
                        {
                            return status[content.attrs[0]].ToString();
                        }
                        return "";
                    }
                    return "";
                }
                case GameContentParser.ContentType.ENEMYIMAGE:
                {
                    var heros = battle.EnemyViewDataList;
                    if (heros.Count > index)
                    {
                        owner.SetPartyImageThumbnail(result, heros[index].monster);
                    }
                    if (!result.IsContentIncluded()) result.DrawText = "";
                    return "";
                }
                case GameContentParser.ContentType.ENEMYIMAGEICON:
                {
                    var heros = battle.EnemyViewDataList;
                    if (heros.Count > index)
                    {
                        var hero = heros[index];

                        owner.SetPartyIconThumbnail(result, hero.monster);
                    }
                    if (!result.IsContentIncluded()) result.DrawText = "";
                    return "";
                }
                case GameContentParser.ContentType.ENEMYGRAPHIC:
                {
                    var heros = battle.EnemyViewDataList;
                    if (heros.Count > index)
                    {
                        owner.SetPartyThumbnail(result, heros[index].monster);
                    }
                    if (!result.IsContentIncluded()) result.DrawText = "";
                    return "";
                }
                case GameContentParser.ContentType.ENEMYCONDITION_A:
                {
                    var heros = battle.EnemyViewDataList;
                    if (heros != null && heros.Count > index)
                    {
                        var hero = heros[index];
                        if (hero.conditionInfoDic.Count == 0 && content.attrs[0] == 0)
                        {
                            return catalog.getGameSettings().glossary.normal;
                        }
                        else
                        {
                            var conditionName = owner.SetConditionName(hero.conditionInfoDic, content.attrs[0]);
                            if (!string.IsNullOrEmpty(conditionName))
                            {
                                return conditionName;
                            }
                        }
                    }
                    else
                    {
                        return "";
                    }
                }
                break;
                case GameContentParser.ContentType.ENEMYCONDITION:
                {
                    var heros = battle.EnemyViewDataList;
                    if (heros.Count > index)
                    {
                        var hero = heros[index];
                        if (hero.conditionInfoDic.Count == 0)
                        {
                            return catalog.getGameSettings().glossary.normal;
                        }
                        else
                        {
                            var conditionName = owner.SetConditionName(hero.conditionInfoDic);
                            if (!string.IsNullOrEmpty(conditionName))
                            {
                                return conditionName;
                            }
                        }
                    }
                    else
                    {
                        return "";
                    }
                }
                break;
                case GameContentParser.ContentType.ENEMYCONDITIONICON_A:
                {
                    var heros = battle.EnemyViewDataList;
                    var partyIndex = index;
                    if (heros != null && heros.Count > partyIndex)
                    {
                        var hero = heros[partyIndex];
                        owner.SetConditionIconThumbnail(result, hero.conditionInfoDic, content.attrs[0]);
                    }
                    if (!result.IsContentIncluded()) result.DrawText = "";
                    return "";
                }
                case GameContentParser.ContentType.ENEMYCONDITIONICON:
                {
                    var heros = battle.EnemyViewDataList;
                    index = Math.Min(index, heros.Count - 1);
                    index = Math.Max(0, index);
                    if (heros != null && heros.Count > index)
                    {
                        var hero = heros[index];
                        owner.SetConditionIconThumbnail(result, hero.conditionInfoDic);
                    }
                    if (!result.IsContentIncluded()) result.DrawText = "";
                    return "";
                }
                case GameContentParser.ContentType.ENEMYCLASSICON:
                case GameContentParser.ContentType.ENEMYSUBCLASSICON:
                {
                    var isSubClass = content.type == GameContentParser.ContentType.ENEMYSUBCLASSICON;
                    var heros = battle.EnemyViewDataList;
                    if (heros.Count > index)
                    {
                        var hero = heros[index];
                        owner.SetHeroJobIcon(result, hero.monsterGameData, isSubClass);
                    }
                    if (!result.IsContentIncluded()) result.DrawText = "";
                }
                break;
                case GameContentParser.ContentType.ENEMYCLASSLEVEL:
                case GameContentParser.ContentType.ENEMYSUBCLASSLEVEL:
                {
                    var isSubClass = content.type == GameContentParser.ContentType.ENEMYSUBCLASSLEVEL;
                    var heros = battle.EnemyViewDataList;

                    if (heros.Count > index)
                    {
                        result.HasPartyElement = true;

                        var jobCast = isSubClass ? heros[index].monsterGameData.sideJobCast : heros[index].monsterGameData.jobCast;
                        var levelUpData = new LevelUpData();
                        levelUpData.setEmptyParam();
                        return owner.GetJobStatus(jobCast, 0, levelUpData);
                    }
                    else
                    {
                        return "";
                    }
                }
                case GameContentParser.ContentType.ENEMYCLASS:
                case GameContentParser.ContentType.ENEMYSUBCLASS:
                {
                    var isSubClass = content.type == GameContentParser.ContentType.ENEMYSUBCLASS;
                    var heros = battle.EnemyViewDataList;
                    if (heros.Count > index)
                    {
                        var hero = heros[index].monsterGameData;
                        var jobCast = isSubClass ? hero.sideJobCast : hero.jobCast;
                        return jobCast?.rom?.name ?? "";
                    }
                    else return "";
                }
                case GameContentParser.ContentType.ENEMYATTRIBUTEICON:
                {
                    var heros = battle.EnemyViewDataList;
                    if (heros.Count > index)
                    {
                        var hero = heros[index];
                        owner.SetHeroAttributeIconThumbnail(result, hero.monsterGameData);

                    }
                    if (!result.IsContentIncluded()) result.DrawText = "";
                }
                break;
                case GameContentParser.ContentType.ENEMYRESISTANCEICON_A:
                {

                    var heros = battle.EnemyViewDataList;
                    if (heros.Count > index)
                    {
                        var hero = heros[index];
                        if (owner.SetHeroResistanceIconThumbnail(result, hero.monsterGameData, content.attrs[0]))
                        {
                            result.HasPartyElement = true;
                        }
                    }
                    if (!result.IsContentIncluded()) result.DrawText = "";
                }
                break;
                case GameContentParser.ContentType.ENEMYRESISTANCE_A:
                {

                    var heros = battle.EnemyViewDataList;
                    if (heros.Count > index)
                    {
                        var hero = heros[index];
                        return owner.GetHeroResistance(hero.monsterGameData, content.attrs[0]);
                    }
                    return "";
                }
                case GameContentParser.ContentType.ENEMYEXTRAPARAM_A_A:
                {
                    var heros = battle.EnemyViewDataList;
                    var partyIndex = content.attrs[0];

                    if (heros.Count > partyIndex && content.options.Count > 1)
                    {
                        return owner.GetHeroExtraParameter(heros[partyIndex].monsterGameData, content.options[1]);
                    }
                    return "";
                }
                case GameContentParser.ContentType.ENEMYEXTRAPARAM_A:
                {

                    var heros = battle.EnemyViewDataList;
                    if (heros.Count > index)
                    {
                        return owner.GetHeroExtraParameter(heros[index].monsterGameData, content.option);
                    }
                    return "";
                }
                #endregion
                // バトルリザルト関連
                // Battle result related
                #region BATTLERESULT
                case GameContentParser.ContentType.BATTLEMESSAGE:
                {
                    return battle.Viewer.displayMessageText;
                }
                case GameContentParser.ContentType.RESULTEXP:
                {
                    if (battle.resultViewer.dropItemCountTable == null)
                        return null;
                    return battle.resultViewer.exp.ToString();
                }
                case GameContentParser.ContentType.RESULTEXP_A:
                {
                    if (battle.resultViewer.dropItemCountTable == null)
                        return null;
                    if (!battle.resultViewer.characterLevelUpData[content.attrs[0]].castLevelUpData.IsAddExp)
                        return "0";
                    return (battle.resultViewer.exp * battle.resultViewer.characterLevelUpData[content.attrs[0]].expRate / 100).ToString();
                }
                case GameContentParser.ContentType.RESULTMONEY:
                {
                    if (battle.resultViewer.dropItemCountTable == null)
                        return null;
                    return battle.resultViewer.money.ToString();
                }
                case GameContentParser.ContentType.RESULTITEMNAME:
                {
                    if (battle.resultViewer.dropItemCountTable == null)
                        return null;

                    var drops = battle.resultViewer.dropItemCountTable.Keys.ToArray();

                    if (drops.Length > index)
                        return drops[index].name;
                    else
                        return "";
                }
                case GameContentParser.ContentType.RESULTITEMIMAGE:
                {
                    if (battle.resultViewer.dropItemCountTable == null)
                        return null;
                    var drops = battle.resultViewer.dropItemCountTable.Keys.ToArray();
                    if (battle.resultViewer.itemIconTable.Count == 0)
                    {
                        result.DrawText = "";
                        return "";
                    }
                    if (drops.Length > index)
                    {
                        var item = drops[index];
                        SetItemIconThumbnail(result, item, catalog);
                    }
                    else
                    {
                        result.DrawText = "";
                    }

                    return "";
                }
                case GameContentParser.ContentType.RESULTITEMNUM:
                {
                    if (battle.resultViewer.dropItemCountTable == null)
                        return null;

                    var drops = battle.resultViewer.dropItemCountTable.Keys.ToArray();

                    if (drops.Length > index)
                    {
                        var num = battle.resultViewer.dropItemCountTable[drops[index]];
                        return "" + num.ToString();
                    }
                    else
                        return "";
                }
                case GameContentParser.ContentType.LEVELUPNAME:
                {
                    var chr = gameContent.CharacterLevelUpData.Count > index ? (CharacterLevelUpData?)gameContent.CharacterLevelUpData[index] : null;
                    if (chr?.player != null)
                    {
                        result.HasLevelUpElement = true;
                        return chr.Value.player.Name;
                    }
                    result.HasLevelUpElement = false;
                    return "";
                }
                case GameContentParser.ContentType.LEVELUPBEFORESTATUS_A:
                {
                    var chr = gameContent.CharacterLevelUpData.Count > index ? (CharacterLevelUpData?)gameContent.CharacterLevelUpData[index] : null;
                    if (chr?.player != null)
                    {
                        var hero = chr.Value.player.player;
                        var cast = hero.rom;
                        var level = chr.Value.castLevelUpData.CurrentLevel;
                        var preLevel = level - 1;
                        if (preLevel == 0) preLevel = 1;
                        var preStatus = CalclateHeroStatus(hero, cast, preLevel, chr.Value, catalog, gameContent);
                        if (preStatus.Length > content.attrs[0])
                        {
                            result.HasLevelUpElement = true;
                            return preStatus[content.attrs[0]].ToString();
                        }
                    };
                    result.HasLevelUpElement = false;
                    return "";
                }
                case GameContentParser.ContentType.LEVELUPAFTERSTATUS_A:
                {
                    var chr = gameContent.CharacterLevelUpData.Count > index ? (CharacterLevelUpData?)gameContent.CharacterLevelUpData[index] : null;
                    if (chr?.player != null)
                    {
                        var hero = chr.Value.player.player;
                        var cast = hero.rom;
                        var level = chr.Value.castLevelUpData.CurrentLevel;
                        var nowStatus = CalclateHeroStatus(hero, cast, level, chr.Value, catalog, gameContent);
                        if (nowStatus.Length > content.attrs[0])
                        {
                            var preLevel = level - 1;

                            if (preLevel == 0)
                            {
                                preLevel = 1;
                            }

                            var preStatus = CalclateHeroStatus(hero, cast, preLevel, chr.Value, catalog, gameContent);

                            refStatusUpDown = CompareStatus(preStatus[content.attrs[0]], nowStatus[content.attrs[0]]);

                            result.HasLevelUpElement = true;
                            return nowStatus[content.attrs[0]].ToString();
                        }
                    };
                    result.HasLevelUpElement = false;
                    return "";
                }
                case GameContentParser.ContentType.LEVELUPSTATUS_A:
                {
                    var chr = gameContent.CharacterLevelUpData.Count > index ? (CharacterLevelUpData?)gameContent.CharacterLevelUpData[index] : null;
                    if (chr?.player != null)
                    {
                        var hero = chr.Value.player.player;
                        var cast = hero.rom;
                        var level = chr.Value.castLevelUpData.CurrentLevel;
                        var preLevel = level - 1;
                        if (preLevel == 0) preLevel = 1;
                        var preStatus = CalclateHeroStatus(hero, cast, preLevel, chr.Value, catalog, gameContent);
                        if (content.attrs[0] >= preStatus.Length)
                        {
                            result.HasLevelUpElement = false;
                            return "";
                        }
                        var nowStatus = CalclateHeroStatus(hero, cast, level, chr.Value, catalog, gameContent);

                        var diff = nowStatus[content.attrs[0]] - preStatus[content.attrs[0]];
                        if (diff == 0)
                        {
                            result.HasLevelUpElement = false;
                            return "";
                        }

                        refStatusUpDown = CompareStatus(preStatus[content.attrs[0]], nowStatus[content.attrs[0]]);

                        var arrow = " → ";

                        result.HasLevelUpElement = true;
                        return arrow + nowStatus[content.attrs[0]].ToString();
                    };
                    result.HasLevelUpElement = false;
                    return "";
                }
                case GameContentParser.ContentType.LEVELUPDIFFSTATUS_A:
                {
                    var chr = gameContent.CharacterLevelUpData.Count > index ? (CharacterLevelUpData?)gameContent.CharacterLevelUpData[index] : null;
                    if (chr?.player != null)
                    {
                        var hero = chr.Value.player.player;
                        var cast = hero.rom;
                        var level = chr.Value.castLevelUpData.CurrentLevel;
                        var preLevel = level - 1;
                        if (preLevel == 0) preLevel = 1;
                        var preStatus = CalclateHeroStatus(hero, cast, preLevel, chr.Value, catalog, gameContent);
                        if (content.attrs[0] >= preStatus.Length)
                        {
                            result.HasLevelUpElement = false;
                            return "";
                        }
                        var nowStatus = CalclateHeroStatus(hero, cast, level, chr.Value, catalog, gameContent);
                        var arrow = GetStatusDiffText(preStatus[content.attrs[0]], nowStatus[content.attrs[0]], out refStatusUpDown);

                        result.HasLevelUpElement = refStatusUpDown != GameContentParser.StatusUpDown.NoChange;

                        return result.HasLevelUpElement ? arrow : string.Empty;
                    }
                    result.HasLevelUpElement = false;
                    return "";
                }
                case GameContentParser.ContentType.LEVELUPUPDATESTATUS_A:
                {
                    var chr = gameContent.CharacterLevelUpData.Count > index ? (CharacterLevelUpData?)gameContent.CharacterLevelUpData[index] : null;
                    if (chr?.player != null)
                    {
                        var hero = chr.Value.player.player;
                        var cast = hero.rom;
                        var level = chr.Value.castLevelUpData.CurrentLevel;
                        var preLevel = level - 1;
                        if (preLevel == 0) preLevel = 1;
                        var preStatus = CalclateHeroStatus(hero, cast, preLevel, chr.Value, catalog, gameContent);
                        if (content.attrs[0] >= preStatus.Length)
                        {
                            result.HasLevelUpElement = false;
                            return "";
                        }
                        var nowStatus = CalclateHeroStatus(hero, cast, level, chr.Value, catalog, gameContent);

                        var diff = nowStatus[content.attrs[0]] - preStatus[content.attrs[0]];
                        var easingTime = chr.Value.easingProgress;
                        if (0.35f > easingTime) easingTime = 0f;
                        else if (easingTime > 0.65f) easingTime = 1.0f;
                        else easingTime = (easingTime - 0.35f) * 3.33f;
                        var diffEasingValue = (int)Engine.Easing.EasingFunction.GetEasingResult(new Engine.Easing.Linear(), easingTime, 1f, 0f, diff);

                        refStatusUpDown = CompareStatus(preStatus[content.attrs[0]], nowStatus[content.attrs[0]]);

                        result.HasLevelUpElement = true;
                        return (preStatus[content.attrs[0]] + diffEasingValue).ToString();
                    };
                    result.HasLevelUpElement = false;
                    return "";
                }
                case GameContentParser.ContentType.LEVELUPUPDATEDIFFSTATUS_A:
                {
                    var chr = gameContent.CharacterLevelUpData.Count > index ? (CharacterLevelUpData?)gameContent.CharacterLevelUpData[index] : null;
                    if (chr?.player != null)
                    {
                        var hero = chr.Value.player.player;
                        var cast = hero.rom;
                        var level = chr.Value.castLevelUpData.CurrentLevel;
                        var preLevel = level - 1;
                        if (preLevel == 0) preLevel = 1;
                        var preStatus = CalclateHeroStatus(hero, cast, preLevel, chr.Value, catalog, gameContent);
                        if (content.attrs[0] >= preStatus.Length)
                        {
                            result.HasLevelUpElement = false;
                            return "";
                        }
                        var nowStatus = CalclateHeroStatus(hero, cast, level, chr.Value, catalog, gameContent);

                        var diff = nowStatus[content.attrs[0]] - preStatus[content.attrs[0]];
                        var easingTime = chr.Value.easingProgress;
                        if (0.35f > easingTime) easingTime = 0f;
                        else if (easingTime > 0.65f) easingTime = 1.0f;
                        else easingTime = (easingTime - 0.35f) * 3.33f;
                        var diffEasingValue = (int)Engine.Easing.EasingFunction.GetEasingResult(new Engine.Easing.Linear(), easingTime, 1f, diff, 0f);
                        var arrow = GetStatusDiffText(0, diffEasingValue, out refStatusUpDown);

                        result.HasLevelUpElement = refStatusUpDown != GameContentParser.StatusUpDown.NoChange;

                        return result.HasLevelUpElement ? arrow : string.Empty;
                    };
                    result.HasLevelUpElement = false;
                    return "";
                }
                case GameContentParser.ContentType.LEVELUPUPDATEDIFFSTATUS2_A:
                {
                    var chr = gameContent.CharacterLevelUpData.Count > index ? (CharacterLevelUpData?)gameContent.CharacterLevelUpData[index] : null;
                    if (chr?.player != null)
                    {
                        var hero = chr.Value.player.player;
                        var cast = hero.rom;
                        var level = chr.Value.castLevelUpData.CurrentLevel;
                        var preLevel = level - 1;
                        if (preLevel == 0) preLevel = 1;
                        var preStatus = CalclateHeroStatus(hero, cast, preLevel, chr.Value, catalog, gameContent);
                        if (content.attrs[0] >= preStatus.Length)
                        {
                            result.HasLevelUpElement = false;
                            return "";
                        }
                        var easingTime = chr.Value.easingProgress;

                        if (easingTime >= 0.9f)
                        {
                            result.HasLevelUpElement = false;
                            return "";
                        }

                        var nowStatus = CalclateHeroStatus(hero, cast, level, chr.Value, catalog, gameContent);
                        var arrow = GetStatusDiffText(preStatus[content.attrs[0]], nowStatus[content.attrs[0]], out refStatusUpDown);

                        result.HasLevelUpElement = refStatusUpDown != GameContentParser.StatusUpDown.NoChange;

                        return result.HasLevelUpElement ? arrow : string.Empty;
                    }
                    result.HasLevelUpElement = false;
                    return "";
                }
                #endregion
                // バトルパーティ関連
                // Battle party related
                #region BATTLEPARTY
                case GameContentParser.ContentType.PARTYNAME_A:
                {
                    var heros = battle.PlayerViewDataList;
                    var partyIndex = content.attrs[0];
                    if (heros != null && heros.Count > partyIndex)
                    {
                        var partyName = battle.PlayerViewDataList[partyIndex].Name;
                        result.HasPartyElement = true;
                        return partyName;
                    }
                    else
                    {
                        return "";
                    }
                }
                case GameContentParser.ContentType.PARTYNAME:
                {
                    var heros = battle.PlayerViewDataList;
                    if (heros != null && heros.Count > index)
                    {
                        var partyName = battle.PlayerViewDataList[index].Name;
                        result.HasPartyElement = true;
                        return partyName;
                    }
                    else
                    {
                        return "";
                    }
                }
                case GameContentParser.ContentType.PARTYIMAGE_A:
                {
                    if (isPreview)
                    {
                        var casts = catalog.getFilteredItemList(typeof(Cast));
                        var partyIndex = content.attrs[0];
                        partyIndex = Math.Min(partyIndex, casts.Count - 1);
                        partyIndex = Math.Max(0, partyIndex);
                        if (casts.Count > partyIndex)
                        {
                            owner.SetPartyImageThumbnail(result, casts[partyIndex] as Cast);
                        }
                        else
                        {
                            return "PartyImageX";
                        }
                        if (!result.IsContentIncluded()) result.DrawText = "";
                    }
                    else
                    {
                        var heros = battle.PlayerViewDataList;
                        var partyIndex = content.attrs[0];

                        if (heros.Count > partyIndex)
                        {
                            owner.SetPartyImageThumbnail(result, heros[partyIndex].player.rom);
                        }
                        if (!result.IsContentIncluded()) result.DrawText = "";
                        result.HasPartyElement = true;
                        return "";
                    }
                }
                break;
                case GameContentParser.ContentType.PARTYIMAGE:
                {
                    if (isPreview)
                    {
                        var casts = catalog.getFilteredItemList(typeof(Cast));
                        index = Math.Min(index, casts.Count - 1);
                        index = Math.Max(0, index);
                        if (casts.Count > index)
                        {
                            owner.SetPartyImageThumbnail(result, casts[index] as Cast);
                        }
                        else
                        {
                            return "PartyImage";
                        }
                        if (!result.IsContentIncluded()) result.DrawText = "";
                    }
                    else
                    {
                        var heros = battle.PlayerViewDataList;
                        if (heros.Count > index)
                        {
                            owner.SetPartyImageThumbnail(result, heros[index].player.rom);
                        }
                        if (!result.IsContentIncluded()) result.DrawText = "";
                        result.HasPartyElement = true;
                        return "";
                    }
                }
                break;
                case GameContentParser.ContentType.PARTYIMAGEICON_A:
                {
                    if (isPreview)
                    {
                        var casts = catalog.getFilteredItemList(typeof(Cast));
                        var partyIndex = content.attrs[0];
                        partyIndex = Math.Min(partyIndex, casts.Count - 1);
                        partyIndex = Math.Max(0, partyIndex);
                        if (casts.Count > partyIndex)
                        {
                            if (!owner.SetPartyIconThumbnail(result, casts[partyIndex] as Cast))
                            {
                                return "PartyImageIconX";
                            }
                        }
                        else
                        {
                            return "PartyImageIconX";
                        }
                        if (!result.IsContentIncluded()) result.DrawText = "";
                    }
                    else
                    {
                        var heros = battle.PlayerViewDataList;
                        var partyIndex = content.attrs[0];

                        if (heros.Count > partyIndex)
                        {
                            owner.SetPartyIconThumbnail(result, heros[partyIndex].player.rom);
                        }
                        if (!result.IsContentIncluded()) result.DrawText = "";
                        result.HasPartyElement = true;
                        return "";
                    }
                }
                break;
                case GameContentParser.ContentType.PARTYIMAGEICON:
                {
                    if (isPreview)
                    {
                        var casts = catalog.getFilteredItemList(typeof(Cast));
                        index = Math.Min(index, casts.Count - 1);
                        index = Math.Max(0, index);
                        if (casts.Count > index)
                        {
                            if (!owner.SetPartyIconThumbnail(result, casts[index] as Cast))
                            {
                                return "PartyImageIcon";
                            }
                        }
                        else
                        {
                            return "PartyImageIcon";
                        }
                        if (!result.IsContentIncluded()) result.DrawText = "";
                    }
                    else
                    {
                        var heros = battle.PlayerViewDataList;
                        if (heros.Count > index)
                        {
                            owner.SetPartyIconThumbnail(result, heros[index].player.rom);
                        }
                        if (!result.IsContentIncluded()) result.DrawText = "";
                        result.HasPartyElement = true;
                        return "";
                    }
                }
                break;
                case GameContentParser.ContentType.PARTYGRAPHIC_A:
                {
                    if (isPreview)
                    {
                        var casts = catalog.getFilteredItemList(typeof(Cast));
                        var partyIndex = content.attrs[0];
                        partyIndex = Math.Min(partyIndex, casts.Count - 1);
                        partyIndex = Math.Max(0, partyIndex);
                        if (casts.Count > partyIndex)
                        {
                            owner.SetPartyThumbnail(result, casts[partyIndex] as Cast);
                        }
                        else
                        {
                            return "PartyGraphicX";
                        }
                        if (!result.IsContentIncluded()) result.DrawText = "";
                    }
                    else
                    {
                        var heros = battle.PlayerViewDataList;
                        var partyIndex = content.attrs[0];

                        if (heros.Count > partyIndex)
                        {
                            owner.SetPartyThumbnail(result, heros[partyIndex].player.rom);
                        }
                        if (!result.IsContentIncluded()) result.DrawText = "";
                        result.HasPartyElement = true;
                        return "";
                    }
                }
                break;
                case GameContentParser.ContentType.PARTYGRAPHIC:
                {
                    if (isPreview)
                    {
                        var casts = catalog.getFilteredItemList(typeof(Cast));
                        index = Math.Min(index, casts.Count - 1);
                        index = Math.Max(0, index);
                        if (casts.Count > index)
                        {
                            owner.SetPartyThumbnail(result, casts[index] as Cast);
                        }
                        else
                        {
                            return "PartyGraphic";
                        }
                        if (!result.IsContentIncluded()) result.DrawText = "";
                    }
                    else
                    {
                        var heros = battle.PlayerViewDataList;
                        if (heros.Count > index)
                        {
                            owner.SetPartyThumbnail(result, heros[index].player.rom);
                        }
                        if (!result.IsContentIncluded()) result.DrawText = "";
                        result.HasPartyElement = true;
                        return "";
                    }
                }
                break;
                case GameContentParser.ContentType.PARTYSTATUS_A_A:
                {
                    var heros = battle.PlayerViewDataList;
                    var partyIndex = content.attrs[0];
                    if (heros != null && partyIndex > -1 && heros.Count > partyIndex)
                    {
                        var hero = heros[partyIndex];
                        var status = new int[] {
                                hero.player.level,
                                hero.MaxHitPoint,
                                hero.battleStatusData.HitPoint,
                                hero.MaxMagicPoint,
                                hero.battleStatusData.MagicPoint,
                                hero.Power,
                                hero.VitalityBase,
                                hero.Magic,
                                hero.Speed,
                                hero.player.exp,
                                hero.player.rom.levelUpExpList[hero.player.level - 1] - hero.player.exp,
                                hero.Attack,
                                hero.ElementAttack,
                                hero.Defense,
                                Math.Min(100, hero.Dexterity),
                                Math.Min(100, hero.Evasion),
                                Math.Min(100, hero.Critical),
                                hero.player.rom.money};


                        // LV99のときは数値を返さない
                        // Does not return a number when LV99
                        if (content.attrs[0] == 10 && hero.player.level == Common.GameData.Hero.MAX_LEVEL)
                            return "-";

                        if (status.Length > content.attrs[1])
                        {
                            if (gameContent.CharacterLevelUpData.Count > partyIndex)
                            {
                                var characterLevelUpData = gameContent.CharacterLevelUpData[partyIndex];
                                status[0] = characterLevelUpData.castLevelUpData.CurrentLevel;
                                status[9] = characterLevelUpData.castLevelUpData.CurrentExp;
                                status[10] = characterLevelUpData.castLevelUpData.CurrentNextLevelNeedExp;
                            }

                            return status[content.attrs[1]].ToString();
                        }
                        return "";
                    }
                    return "";
                }
                case GameContentParser.ContentType.PARTYSTATUS_A:
                {
                    var heros = battle.PlayerViewDataList;

                    if (heros.Count > index)
                    {
                        var hero = heros[index];
                        var status = new int[] {
                            hero.player.level,
                            hero.MaxHitPoint,
                            hero.battleStatusData.HitPoint,
                            hero.MaxMagicPoint,
                            hero.battleStatusData.MagicPoint,
                            hero.Power,
                            hero.VitalityBase,
                            hero.Magic,
                            hero.Speed,
                            hero.player.exp,
                            hero.player.rom.levelUpExpList[hero.player.level - 1] - hero.player.exp,
                            hero.Attack,
                            hero.ElementAttack,
                            hero.Defense,
                            Math.Min(100, hero.Dexterity),
                            Math.Min(100, hero.Evasion),
                            Math.Min(100, hero.Critical),
                            hero.player.rom.money};

                        // LV99のときは数値を返さない
                        // Does not return a number when LV99
                        if (content.attrs[0] == 10 && hero.player.level == Common.GameData.Hero.MAX_LEVEL)
                            return "-";

                        if (gameContent.CharacterLevelUpData.Count > index)
                        {
                            var characterLevelUpData = gameContent.CharacterLevelUpData[index];
                            status[0] = characterLevelUpData.castLevelUpData.CurrentLevel;
                            status[9] = characterLevelUpData.castLevelUpData.CurrentExp;
                            status[10] = characterLevelUpData.castLevelUpData.CurrentNextLevelNeedExp;
                        }
                        if (status.Length > content.attrs[0])
                        {
                            return status[content.attrs[0]].ToString();
                        }
                        return "";
                    }
                    return "";
                }
                case GameContentParser.ContentType.PARTYCONDITION_A_A:
                {
                    var heros = battle.PlayerViewDataList;
                    var partyIndex = content.attrs[0];
                    if (heros != null && heros.Count > partyIndex)
                    {
                        var hero = heros[partyIndex];
                        if (hero.conditionInfoDic.Count == 0 && content.attrs[1] == 0)
                        {
                            return catalog.getGameSettings().glossary.normal;
                        }
                        else
                        {
                            var conditionName = owner.SetConditionName(hero.conditionInfoDic, content.attrs[1]);
                            if (!string.IsNullOrEmpty(conditionName))
                            {
                                result.HasPartyElement = true;
                                return conditionName;
                            }
                        }
                    }
                    else
                    {
                        return "";
                    }
                }
                break;
                case GameContentParser.ContentType.PARTYCONDITION_A:
                {
                    var heros = battle.PlayerViewDataList;
                    var partyIndex = index;
                    if (heros != null && heros.Count > partyIndex)
                    {
                        var hero = heros[partyIndex];
                        if (hero.conditionInfoDic.Count == 0 && content.attrs[0] == 0)
                        {
                            return catalog.getGameSettings().glossary.normal;
                        }
                        else
                        {
                            var conditionName = owner.SetConditionName(hero.conditionInfoDic, content.attrs[0]);
                            if (!string.IsNullOrEmpty(conditionName))
                            {
                                result.HasPartyElement = true;
                                return conditionName;
                            }
                        }
                    }
                    else
                    {
                        return "";
                    }
                }
                break;
                case GameContentParser.ContentType.PARTYCONDITION:
                {
                    var heros = battle.PlayerViewDataList;
                    index = Math.Min(index, heros.Count - 1);
                    index = Math.Max(0, index);

                    if (heros.Count > index)
                    {
                        var hero = heros[index];
                        if (hero.conditionInfoDic.Count == 0)
                        {
                            return catalog.getGameSettings().glossary.normal;
                        }
                        else
                        {
                            var conditionName = owner.SetConditionName(hero.conditionInfoDic);
                            if (!string.IsNullOrEmpty(conditionName))
                            {
                                result.HasPartyElement = true;
                                return conditionName;
                            }
                        }
                    }
                    else
                    {
                        return "";
                    }
                }
                break;
                case GameContentParser.ContentType.PARTYCONDITIONICON_A_A:
                {
                    var heros = battle.PlayerViewDataList;
                    var partyIndex = content.attrs[0];
                    if (heros != null && heros.Count > partyIndex)
                    {
                        var hero = heros[partyIndex];
                        owner.SetConditionIconThumbnail(result, hero.conditionInfoDic, content.attrs[1]);
                    }
                    if (!result.IsContentIncluded()) result.DrawText = "";
                    result.HasPartyElement = true;
                    return "";
                }

                case GameContentParser.ContentType.PARTYCONDITIONICON_A:
                {
                    var heros = battle.PlayerViewDataList;
                    var partyIndex = index;
                    if (heros != null && heros.Count > partyIndex)
                    {
                        var hero = heros[partyIndex];
                        owner.SetConditionIconThumbnail(result, hero.conditionInfoDic, content.attrs[0]);
                    }
                    if (!result.IsContentIncluded()) result.DrawText = "";
                    result.HasPartyElement = true;
                    return "";
                }
                case GameContentParser.ContentType.PARTYCONDITIONICON:
                {
                    var heros = battle.PlayerViewDataList;
                    index = Math.Min(index, heros.Count - 1);
                    index = Math.Max(0, index);
                    if (heros != null && heros.Count > index)
                    {
                        var hero = heros[index];
                        owner.SetConditionIconThumbnail(result, hero.conditionInfoDic);
                    }
                    if (!result.IsContentIncluded()) result.DrawText = "";
                    result.HasPartyElement = true;
                    return "";
                }
                case GameContentParser.ContentType.PARTYCLASS_A:
                case GameContentParser.ContentType.PARTYSUBCLASS_A:
                {
                    var isSubClass = content.type == GameContentParser.ContentType.PARTYSUBCLASS_A;

                    var heros = battle.PlayerViewDataList;
                    var partyIndex = content.attrs[0];
                    if (heros.Count > partyIndex)
                    {
                        result.HasPartyElement = true;

                        var jobCast = isSubClass ? heros[partyIndex].Hero.sideJobCast : heros[partyIndex].Hero.jobCast;

                        return jobCast?.rom?.name ?? "";
                    }
                    else
                    {
                        return "";
                    }
                }
                case GameContentParser.ContentType.PARTYCLASS:
                case GameContentParser.ContentType.PARTYSUBCLASS:
                {
                    var isSubClass = content.type == GameContentParser.ContentType.PARTYSUBCLASS;

                    var heros = battle.PlayerViewDataList;
                    if (heros.Count > index)
                    {
                        result.HasPartyElement = true;

                        var jobCast = isSubClass ? heros[index].Hero.sideJobCast : heros[index].Hero.jobCast;

                        return jobCast?.rom?.name ?? "";
                    }
                    else return "";
                }
                case GameContentParser.ContentType.PARTYCLASSLEVEL_A:
                case GameContentParser.ContentType.PARTYSUBCLASSLEVEL_A:
                {
                    var isSubClass = content.type == GameContentParser.ContentType.PARTYSUBCLASSLEVEL_A;

                    var heros = battle.PlayerViewDataList;
                    var partyIndex = content.attrs[0];

                    if (heros.Count > partyIndex)
                    {
                        var jobCast = isSubClass ? heros[partyIndex].Hero.sideJobCast : heros[partyIndex].Hero.jobCast;

                        if (jobCast != null)
                        {
                            result.HasPartyElement = true;

                            var levelUpData = (gameContent.CharacterLevelUpData.Count > partyIndex) ? (isSubClass ? gameContent.CharacterLevelUpData[partyIndex].sideJobLevelUpData : gameContent.CharacterLevelUpData[partyIndex].jobLevelUpData) : new BattleEnum.LevelUpData();

                            return owner.GetJobStatus(jobCast, 0, levelUpData);
                        }
                    }

                    return "";
                }
                case GameContentParser.ContentType.PARTYCLASSLEVEL:
                case GameContentParser.ContentType.PARTYSUBCLASSLEVEL:
                {
                    var isSubClass = content.type == GameContentParser.ContentType.PARTYSUBCLASSLEVEL;

                    var heros = battle.PlayerViewDataList;

                    if (heros.Count > index)
                    {
                        result.HasPartyElement = true;

                        var jobCast = isSubClass ? heros[index].Hero.sideJobCast : heros[index].Hero.jobCast;

                        var levelUpData = (gameContent.CharacterLevelUpData.Count > index) ? (isSubClass ? gameContent.CharacterLevelUpData[index].sideJobLevelUpData : gameContent.CharacterLevelUpData[index].jobLevelUpData) : new BattleEnum.LevelUpData();

                        return owner.GetJobStatus(jobCast, 0, levelUpData);
                    }
                    else
                    {
                        return "";
                    }
                }
                case GameContentParser.ContentType.PARTYCLASSICON_A:
                case GameContentParser.ContentType.PARTYSUBCLASSICON_A:
                {
                    var isSubClass = content.type == GameContentParser.ContentType.PARTYSUBCLASSICON_A;

                    var heros = battle.PlayerViewDataList;
                    var partyIndex = content.attrs[0];
                    if (heros.Count > partyIndex)
                    {
                        var hero = heros[partyIndex].Hero;
                        if (owner.SetHeroJobIcon(result, hero, isSubClass))
                        {
                            result.HasPartyElement = true;
                        }
                    }

                    if (!result.IsContentIncluded()) result.DrawText = "";
                }
                break;
                case GameContentParser.ContentType.PARTYCLASSICON:
                case GameContentParser.ContentType.PARTYSUBCLASSICON:
                {
                    var isSubClass = content.type == GameContentParser.ContentType.PARTYSUBCLASSICON;
                    var heros = battle.PlayerViewDataList;
                    if (heros.Count > index)
                    {
                        var hero = heros[index].Hero;
                        if (owner.SetHeroJobIcon(result, hero, isSubClass))
                        {
                            result.HasPartyElement = true;
                        }
                    }
                    if (!result.IsContentIncluded()) result.DrawText = "";
                }
                break;
                case GameContentParser.ContentType.PARTYATTRIBUTEICON_A:
                {
                    var heros = battle.PlayerViewDataList;
                    var partyIndex = content.attrs[0];
                    if (heros != null && heros.Count > partyIndex)
                    {
                        var hero = heros[partyIndex];
                        owner.SetHeroAttributeIconThumbnail(result, hero.Hero);
                    }
                    if (!result.IsContentIncluded()) result.DrawText = "";
                    result.HasPartyElement = true;
                    return "";
                }
                case GameContentParser.ContentType.PARTYATTRIBUTEICON:
                {
                    var heros = battle.PlayerViewDataList;
                    index = Math.Min(index, heros.Count - 1);
                    index = Math.Max(0, index);
                    if (heros != null && heros.Count > index)
                    {
                        var hero = heros[index];
                        owner.SetHeroAttributeIconThumbnail(result, hero.Hero);
                    }
                    if (!result.IsContentIncluded()) result.DrawText = "";
                    result.HasPartyElement = true;
                    return "";
                }
                case GameContentParser.ContentType.PARTYRESISTANCEICON_A_A:
                {
                    var heros = battle.PlayerViewDataList;
                    var partyIndex = content.attrs[0];
                    if (heros != null && heros.Count > partyIndex)
                    {
                        var hero = heros[partyIndex];
                        owner.SetHeroResistanceIconThumbnail(result, hero.Hero, content.attrs[1]);
                    }
                    if (!result.IsContentIncluded()) result.DrawText = "";
                    result.HasPartyElement = true;
                    return "";
                }
                case GameContentParser.ContentType.PARTYRESISTANCEICON_A:
                {
                    var heros = battle.PlayerViewDataList;
                    var partyIndex = index;
                    if (heros != null && heros.Count > partyIndex)
                    {
                        var hero = heros[partyIndex];
                        owner.SetHeroResistanceIconThumbnail(result, hero.Hero, content.attrs[0]);
                    }
                    if (!result.IsContentIncluded()) result.DrawText = "";
                    result.HasPartyElement = true;
                    return "";
                }
                case GameContentParser.ContentType.CURRENTPARTYIMAGE:
                {
                    var hero = gameContent?.Hero;

                    for (var i = 0; i < gameMain.data.party.PlayersInMenu.Count; ++i)
                    {
                        if (hero == gameMain.data.party.PlayersInMenu[i])
                        {
                            owner.SetPartyImageThumbnail(result, i);
                            result.HasPartyElement = true;
                            break;
                        }
                    }

                    if (!result.IsContentIncluded()) result.DrawText = "";
                }
                break;
                case GameContentParser.ContentType.CURRENTPARTYIMAGEICON:
                {
                    var hero = gameContent?.Hero;
                    if (hero != null)
                    {
                        owner.SetPartyIconThumbnail(result, hero.rom);
                    }
                    if (!result.IsContentIncluded()) result.DrawText = "";
                    result.HasPartyElement = true;
                }
                break;
                case GameContentParser.ContentType.CURRENTPARTYGRAPHIC:
                {
                    var hero = gameContent?.Hero;

                    for (var i = 0; i < gameMain.data.party.PlayersInMenu.Count; ++i)
                    {
                        if (hero == gameMain.data.party.PlayersInMenu[i])
                        {
                            owner.SetPartyThumbnail(result, i);
                            result.HasPartyElement = true;
                            break;
                        }
                    }

                    if (!result.IsContentIncluded()) result.DrawText = "";
                }
                break;
                #endregion
                // アイテム関連
                // Item related
                #region ITEM
                case GameContentParser.ContentType.ITEMNAME:
                {
                    if (items.Count > index)
                    {
                        result.HasItemElement = true;
                        return items[index].text;
                    }
                    else
                    {
                        return "";
                    }
                }
                case GameContentParser.ContentType.ITEMIMAGE:
                {
                    if (items.Count > index && items[index].iconRef != null)
                    {
                        result.ReserveImageIcon = items[index].iconRef;
                        result.ReserveIconImageId = catalog.getItemFromGuid(items[index].iconRef.guId) as Texture;

                        if (result.ReserveIconImageId != null)
                        {
                            Graphics.LoadImage(result.ReserveIconImageId);
                        }
                        result.HasItemElement = true;
                    }
                    else if (items.Count > index)
                    {
                        var item = items[index].item;
                        if (item is object)
                        {
                            SetItemIconThumbnail(result, item, catalog);
                        }
                    }
                    else
                    {
                        result.DrawText = "";
                    }

                    return "";
                }
                case GameContentParser.ContentType.ITEMNUM:
                {
                    if (items.Count > index)
                    {
                        result.HasItemElement = true;
                        return items[index].number;
                    }
                    else
                    {
                        return "";
                    }
                }
                #endregion
            }

            return null;
        }

        private static int[] CalclateHeroStatus(Common.GameData.Hero hero, Cast cast, int level, CharacterLevelUpData clud, Catalog catalog, AbstractRenderObject.GameContent gameContent)
        {
            var chr = gameContent.CharacterLevelUpData.FirstOrDefault(x => x.player.player.rom.guId == cast.guId);

            var status = new int[] {
                level,
                hero.calcStatus(cast.hpParam, level) + hero.itemEffect.maxHitpoint,
                chr.player.HitPoint,
                hero.calcStatus(cast.mpParam, level) + hero.itemEffect.maxMagicpoint,
                chr.player.MagicPoint,
                hero.calcStatus(cast.powerParam, level) + hero.itemEffect.power,
                hero.calcStatus(cast.vitalityParam, level) + hero.itemEffect.vitality,
                hero.calcStatus(cast.magicParam, level) + hero.itemEffect.magic,
                hero.calcStatus(cast.speedParam, level) + hero.itemEffect.speed,
                chr.castLevelUpData.CurrentExp,
                chr.castLevelUpData.CurrentNextLevelNeedExp,
                hero.calcStatus(cast.powerParam, level) + hero.attackBase,
                chr.player.ElementAttack,
                hero.calcStatus(cast.vitalityParam, level) + hero.defenseBase,
                Math.Min(100, chr.player.Dexterity),
                Math.Min(100, chr.player.Evasion),
                Math.Min(100, chr.player.Critical),
                0
            };

            var jobStatus = GetJobStatus(Common.GameData.Party.createCastJobFromRom(catalog, hero.jobCast?.rom, false, clud.jobLevelUpData.totalExp));
            var sideJobStatus = GetJobStatus(Common.GameData.Party.createCastJobFromRom(catalog, hero.sideJobCast?.rom, true, clud.sideJobLevelUpData.totalExp));

            if ((jobStatus != null) || (sideJobStatus != null))
			{
                // 職業のパラメータを加算
                // Add Occupation Parameters
                var len = (jobStatus == null) ? sideJobStatus.Length : jobStatus.Length;

				if (status.Length < len)
				{
                    len = status.Length;
                }

                for (int i = 0; i < len; i++)
				{
                    var paramIdx = i;

					switch (i)
					{
                        case 0:
                        case 9:
                        case 10:
                            continue;
                        case 11:
                            paramIdx = 5;
                            break;
                        case 13:
                            paramIdx = 6;
                            break;
                        default:
                            break;
					}

                    if (jobStatus != null)
                    {
                        status[i] += jobStatus[paramIdx];
                    }
                    if (sideJobStatus != null)
                    {
                        status[i] += sideJobStatus[paramIdx];
                    }
                }
            }

            return status;
        }

        private static bool SetItemIconThumbnail(RenderContent inResult, Common.Rom.NItem inItem, Catalog catalog)
        {
            if (inItem == null)
            {
                return false;
            }

            inResult.ItemIcon = inItem.Icon;
            inResult.ItemIconImageId = catalog.getItemFromGuid(inResult.ItemIcon.guId) as Texture;

            if (inResult.ItemIconImageId != null)
            {
                // 初使用のイメージは余計に1度読み込んでおいて、解放されないようにする
                // Load the image for the first time extra once so that it is not freed
                if (!CachedImageDic.ContainsKey(inResult.ItemIconImageId))
                {
                    Graphics.LoadImage(inResult.ItemIconImageId);
                    CachedImageDic[inResult.ItemIconImageId] = true;
                }
                Graphics.LoadImage(inResult.ItemIconImageId);
                inResult.ItemIconImageId.getTexture().setWrap(SharpKmyGfx.WRAPTYPE.kCLAMP);
                return true;
            }

            inResult.ItemIconThumbnail = Thumbnail.create(catalog.getItemFromGuid<GfxResourceBase>(inItem.model), false);

            return (inResult.ItemIconThumbnail != null);
        }

        public override bool GetSliderValue(GameContentParser.CachedResult.Content content, AbstractRenderObject.GameContent gameContent,
          GameMain gameMain, int menuIndex,
          SliderRenderer owner)
        {
            var battle = BattleSequenceManagerBase.Get() as BattleSequenceManager;
            if (battle == null || gameContent == null)
                return false;
            if (gameMain?.mapScene?.isBattle ?? false)
            {
                var heros = battle.PlayerViewDataList;
                var enemies = battle.EnemyViewDataList;
                var index = 0;
                owner.ShouldDrawVariableSlider = true;
                if (content.attrs.Count > 0)
                {
                    index = content.attrs[0];
                }
                switch (content.type)
                {
                    case GameContentParser.ContentType.PARTYHP_A:
                        if (heros.Count > index)
                        {
                            var hero = heros[index];
                            owner.SetVariable(
                                owner.VariableText,
                                hero.battleStatusData.HitPoint,
                                hero.MaxHitPoint,
                                0,
                                true);
                            return true;
                        }
                        else
                        {
                            owner.ShouldDrawVariableSlider = false;
                            return true;
                        }
                    case GameContentParser.ContentType.PARTYHP:
                        if (owner.PageIndex > -1 && heros.Count > owner.PageIndex)
                        {
                            var hero = heros[owner.PageIndex];
                            owner.SetVariable(
                                owner.VariableText,
                                hero.battleStatusData.HitPoint,
                                hero.MaxHitPoint,
                                0,
                                true);
                            return true;
                        }
                        else
                        {
                            owner.ShouldDrawVariableSlider = false;
                            return true;
                        }
                    case GameContentParser.ContentType.PARTYMP_A:
                        if (heros.Count > index)
                        {
                            var hero = heros[index];
                            // MAXMPが0だった場合、ゲージが空っぽで表示できるようにする
                            // If MAXMP is 0, allow the gauge to be displayed empty
                            var max = (hero.battleStatusData.MagicPoint == hero.MaxMagicPoint && hero.MaxMagicPoint == 0) ? 1 : hero.MaxMagicPoint;
                            owner.SetVariable(
                                owner.VariableText,
                                hero.battleStatusData.MagicPoint,
                                max,
                                0,
                                true);
                            return true;
                        }
                        else
                        {
                            owner.ShouldDrawVariableSlider = false;
                            return true;
                        }
                    case GameContentParser.ContentType.PARTYMP:
                        if (owner.PageIndex > -1 && heros.Count > owner.PageIndex)
                        {
                            var hero = heros[owner.PageIndex];
                            // MAXMPが0だった場合、ゲージが空っぽで表示できるようにする
                            // If MAXMP is 0, allow the gauge to be displayed empty
                            var max = (hero.battleStatusData.MagicPoint == hero.MaxMagicPoint && hero.MaxMagicPoint == 0) ? 1 : hero.MaxMagicPoint;
                            owner.SetVariable(
                                owner.VariableText,
                                hero.battleStatusData.MagicPoint,
                                max,
                                0,
                                true);
                            return true;
                        }
                        else
                        {
                            owner.ShouldDrawVariableSlider = false;
                            return true;
                        }
                    case GameContentParser.ContentType.PARTYEXP_A:
                        if (gameContent.CharacterLevelUpData.Count > index)
                        {
                            var characterLevelUpData = gameContent.CharacterLevelUpData[index];
                            var value = characterLevelUpData.castLevelUpData.totalExp - characterLevelUpData.castLevelUpData.CurrentLevelNeedTotalExp;
                            var maximum = characterLevelUpData.castLevelUpData.CurrentNextLevelExp - characterLevelUpData.castLevelUpData.CurrentLevelNeedTotalExp;
                            if (characterLevelUpData.onLevelUpEffect)
                            {
                                value = characterLevelUpData.castLevelUpData.CurrentLevelNeedTotalExp;
                                maximum = characterLevelUpData.castLevelUpData.CurrentLevelNeedTotalExp;
                            }
                            owner.SetVariable(
                               owner.VariableText,
                               value,
                               maximum,
                               0,
                               true);
                            return true;
                        }
                        else
                        {
                            owner.ShouldDrawVariableSlider = false;
                            return true;
                        }
                    case GameContentParser.ContentType.PARTYEXP:
                        if (owner.PageIndex > -1 && gameContent.CharacterLevelUpData.Count > owner.PageIndex)
                        {
                            var characterLevelUpData = gameContent.CharacterLevelUpData[owner.PageIndex];
                            var value = characterLevelUpData.castLevelUpData.totalExp - characterLevelUpData.castLevelUpData.CurrentLevelNeedTotalExp;
                            var maximum = characterLevelUpData.castLevelUpData.CurrentNextLevelExp - characterLevelUpData.castLevelUpData.CurrentLevelNeedTotalExp;
                            if (characterLevelUpData.onLevelUpEffect)
                            {
                                value = characterLevelUpData.castLevelUpData.CurrentLevelNeedTotalExp;
                                maximum = characterLevelUpData.castLevelUpData.CurrentLevelNeedTotalExp;
                            }
                            owner.SetVariable(
                               owner.VariableText,
                               value,
                               maximum,
                               0,
                               true);
                            return true;
                        }
                        else
                        {
                            owner.ShouldDrawVariableSlider = false;
                            return true;
                        }
                    case GameContentParser.ContentType.ENEMYSTATUS_0:
                        if (menuIndex > -1 && enemies.Count > menuIndex)
                        {
                            var enemy = enemies[menuIndex];
                            owner.SetVariable(
                                owner.VariableText,
                                enemy.battleStatusData.HitPoint,
                                enemy.MaxHitPoint,
                                0,
                                true);
                            owner.ChangeDrawableParentContainer(enemy.battleStatusData.HitPoint > 0 && enemy.imageAlpha > 0.5f);
                            return true;
                        }
                        else
                        {
                            owner.ChangeDrawableParentContainer(false);
                            return true;
                        }
                    case GameContentParser.ContentType.ENEMYSTATUS_1:
                        if (menuIndex > -1 && enemies.Count > menuIndex)
                        {
                            var enemy = enemies[menuIndex];

                            // MAXMPが0だった場合、ゲージが空っぽで表示できるようにする
                            // If MAXMP is 0, allow the gauge to be displayed empty
                            var max = (enemy.battleStatusData.MagicPoint == enemy.MaxMagicPoint && enemy.MaxMagicPoint == 0) ? 1 : enemy.MaxMagicPoint;
                            owner.SetVariable(
                                owner.VariableText,
                                enemy.battleStatusData.MagicPoint,
                                max,
                                0,
                                true);
                            //owner.ChangeDrawableParentContainer(enemy.battleStatusData.MagicPoint > 0 && enemy.imageAlpha > 0.5f);
                            return true;
                        }
                        else
                        {
                            owner.ChangeDrawableParentContainer(false);
                            return true;
                        }
                    case GameContentParser.ContentType.VARIABLE_A:
                        var variable = gameMain.data.system.GetVariableReference(content.option, Guid.Empty, false);
                        if (variable is object)
                        {
                            owner.HasVariable = true;
                        }
                        owner.SetVariable(owner.VariableText, (int)gameMain.data.system.GetVariable(content.option, Guid.Empty, false), true);
                        return true;
                    case GameContentParser.ContentType.VARIABLE_A_A:
                        var variableArray = gameMain.data.system.GetVariableReferenceFromArray(content.option, content.attrs[0]);
                        if (variableArray is object)
                        {
                            owner.HasVariable = true;
                        }
                        owner.SetVariable(owner.VariableText, (int)gameMain.data.system.GetFromArray(content.option, content.attrs[0]), true);
                        return true;
                }
            }
            else
            {
                if (content.type == GameContentParser.ContentType.PARTYHP_A ||
                    content.type == GameContentParser.ContentType.PARTYMP_A ||
                    content.type == GameContentParser.ContentType.ENEMYHP_A ||
                    content.type == GameContentParser.ContentType.ENEMYMP_A)
                {
                    owner.SetVariable(
                        owner.VariableText,
                        0,
                        0,
                        0,
                        true);
                }
            }

            return false;
        }
    }
}

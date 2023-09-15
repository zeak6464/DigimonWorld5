using System;
using System.Collections.Generic;
using System.Linq;
using Yukar.Common.GameData;
using Yukar.Common.Rom;
using Yukar.Engine;
using static Yukar.Engine.MapData;

namespace Bakin
{
    public class Digievo : BakinObject
    {
        private bool showingShoices;
        private string[] currentChoiseList = { "test", "test2" };
        private int currentIndexSelected;
        private bool selectingEvoDone;
        private bool activateScript = false;
        int phase = 0;
        private bool isIni;
        private Yukar.Common.GameData.Hero DigiToEvo;
        private int lastChoice;
        private int lastLevel;
        private bool locked;
        List<int> repetidosList = new List<int>();
        private ScriptRunner _generatorRunner;
        private Yukar.Common.Rom.Event _evoCast;
        private bool startOffset;
        private bool startEffect;

        public override void Start()
        {

            // キャラクターが生成される時に、このメソッドがコールされます。
            // This method is called when the character is created.
        }

        public override void Update()
        {
            if (!activateScript) return;

            if (!isIni)
            {
                currentChoiseList = mapScene.owner.data.party.Players.

                    Select(x => x.rom.name).ToArray();

                isIni = true;
                if (currentChoiseList.Length == 0)
                {
                    CloseEvo();
                    mapScene.menuWindow.Show();
                }
            }

            ChoiseSelection();
            // キャラクターが生存している間、
            // 毎フレームこのキャラクターのアップデート前にこのメソッドがコールされます。
            // This method is called every frame before this character updates while the character is alive.
        }

        public override void BeforeUpdate()
        {
            // キャラクターが生存している間、
            // 毎フレーム、イベント内容の実行前にこのメソッドがコールされます。
            // This method will be called every frame while the character is alive, before the event content is executed.
        }

        public override void Destroy()
        {
            // キャラクターが破棄される時に、このメソッドがコールされます。
            // This method is called when the character is destroyed.
        }

        public override void AfterDraw()
        {
            // このフレームの2D描画処理の最後に、このメソッドがコールされます。
            // This method is called at the end of the 2D drawing process for this frame.
        }
        private void ChoiseSelection()
        {
            if (!mapScene.IsVisibleChoices() && !showingShoices)
            {
                if (!locked)
                {
                    mapScene.LockControl();
                    locked = true;
                }

                mapScene.ShowChoices(currentChoiseList, 4);
                showingShoices = true;
            }

            if (phase == 0) Graphics.DrawString(0, "Who do you want to digivolve?", new Microsoft.Xna.Framework.Vector2(500, 500), Microsoft.Xna.Framework.Color.White, 2);
            if (phase != 0) Graphics.DrawString(0, "Choose a digivolution", new Microsoft.Xna.Framework.Vector2(500, 500), Microsoft.Xna.Framework.Color.White, 2);

            var choice = mapScene.GetChoicesResult();

            if (choice != -1)
            {
                if (phase == 0)
                {
                    SetEvolutionList(choice);
                }
                else
                {
                    PerformDigievolution(choice);
                }

                mapScene.menuWindow.ResetLayout(LayoutProperties.LayoutNode.UsageInGame.Choice);
                showingShoices = false;
            }

            if (Input.KeyTest(Input.StateType.TRIGGER_UP, Input.KeyStates.CANCEL, Input.GameState.MENU))
            {
                CloseEvo();
            }
        }

        private void PerformDigievolution(int choice)
        {
            if (mapScene.owner.catalog.getItemFromName(currentChoiseList[choice], typeof(Cast)) is Cast castToAdd)
            {
                var heroToAdd = Party.createHeroFromRom(catalog, mapScene.owner.data.party, castToAdd, lastLevel);
                mapChr.setPosition(mapScene.hero.pos);
                var effect = mapScene.mapCharList.FirstOrDefault(x => x?.rom?.name == "EvoEffect");

                if (effect == null)
                {
                    GenerateEvent();
                }

                ScriptRunner runner = null;
                mapScene.mapEngine.followerVisible = false;
                float wait = 0f;

                bool Func()
                {
                    if (!_generatorRunner.isFinished()) return true;
                 
                    if(wait <= 0.2)
                    {
                        wait += GameMain.getElapsedTime();
                        return true;
                    }
                    if (effect == null)
                    {
                        effect = mapScene.mapCharList.FirstOrDefault(x => x?.rom?.name == "EvoEffect");
                        if (effect == null) return true;

                        Tools.PushLog(mapScene.ChangeGraphic(effect, DigiToEvo.rom.graphic).ToString());
                        mapScene.hero.setVisibility(false);
                        effect.setPosition(mapScene.hero.pos);
                        runner = mapScene.GetScriptRunner(_evoCast.guId);
                        runner?.Run();
                    }

                    if (!StartEvoEffect()) return true;

                    if (wait <= 3.7f)
                    {
                        wait += GameMain.getElapsedTime();
                        var vector3 = effect.getRotation();
                        vector3.Y += (GameMain.getElapsedTime() * 280 * wait * 1.2f);
                        effect.setRotation(vector3);
                        return true;
                    }
                    else if (wait != 999)
                    {
                        wait = 999f;

                        if (effect != null)
                        {
                            Tools.PushLog(mapScene.ChangeGraphic(effect, heroToAdd.rom.graphic).ToString());
                            effect = mapScene.mapCharList.FirstOrDefault(x => x?.rom?.name == "EvoEffect");
                        }

                        effect.setDirection(1, true, true);
                        effect.playMotion("attack", 0.2f, false, true);
                    }

                    if (effect.isChangeMotionAvailable())
                    {
                        effect.playMotion("wait", 0.4f, false, false);
                    }

                    if (runner.isFinished())
                    {
                        if (locked)
                        {
                            mapScene.UnlockControl();
                            locked = false;
                        }
                        mapScene.owner.data.party.SetPlayer(heroToAdd, lastChoice);
                        mapScene.RefreshHeroMapChr();
                        mapScene.mapEngine.followerVisible = true;
                        mapScene.hero.setVisibility(true);
                        effect.setVisibility(false);
                        mapScene.mapCharList.Remove(effect);
                        mapScene.runnerDic.Remove(new Guid("23ec3512-0560-43af-8844-9a8a89b319bb"));
                        _generatorRunner = null;
                        effect = null;

                        mapScene.owner.data.system.SetSwitch("startEvoEffect", false);
                        return false;
                    }
                    return true;
                }

                mapScene.owner.pushTask(Func);
            }

            activateScript = false;
            isIni = false;
            phase = 0;
        }

        private bool GenerateEvent()
        {
            Script script = new Script()
            {
                guId = new Guid(),
                name = "GENERATOR"
            };

            script.commands.Add(new Script.Command()
            {
                type = Script.Command.FuncType.SHOT_EVENT
            });

             _evoCast = catalog.getItemFromName<Yukar.Common.Rom.Event>("EvoEffect");
            if (_evoCast == null) return false;

            Script.GuidAttr guidAttr = new Script.GuidAttr()
            {
                value = _evoCast.guId
            };
            Script.IntAttr source = new Script.IntAttr()
            {
                value = 1
            };
            Script.IntAttr angulo = new Script.IntAttr(180);

            Script.IntAttr elResto = new Script.IntAttr(0);

            script.commands[0].attrList.Add(guidAttr);
            script.commands[0].attrList.Add(elResto);
            script.commands[0].attrList.Add(elResto);
            script.commands[0].attrList.Add(angulo);
            script.commands[0].attrList.Add(source);
            script.commands[0].attrList.Add(elResto);
            script.commands[0].attrList.Add(elResto);
            script.commands[0].attrList.Add(elResto);
            script.commands[0].attrList.Add(elResto);

            _generatorRunner = new ScriptRunner(mapScene, mapChr, script);


            Guid evoEffectGuid = new Guid("23ec3512-0560-43af-8844-9a8a89b319bb");
            if (!mapScene.runnerDic.ContainsKey(evoEffectGuid)) mapScene.runnerDic.Add(evoEffectGuid, _generatorRunner);

            _generatorRunner.Run();
            return true;
        }
        private void SetEvolutionList(int choice)
        {

            DigiToEvo = mapScene.owner.data.party.Players[choice];
            lastChoice = mapScene.owner.data.party.Players.IndexOf(DigiToEvo);

            var unhandledList = Tools.GetTagMultipleValues(DigiToEvo.rom.tags, "$evo");

            if (unhandledList.Count < 1)
            {
                showingShoices = false;
                return;
            }

            var handledList = new List<string>();

            lastLevel = DigiToEvo.level;
            foreach (var unhandledtag in unhandledList)
            {
                int requiredLevel = 0;
                var evoAndLevel = unhandledtag.Split(',');

                if (string.IsNullOrEmpty(evoAndLevel[0])) continue;
                if (evoAndLevel.Length == 2)
                    int.TryParse(evoAndLevel[1], out requiredLevel);

                if (lastLevel < requiredLevel) continue;

                handledList.Add(evoAndLevel[0]);
            }

            currentChoiseList = handledList.ToArray();

            phase++;
        }

        [BakinFunction(Description = "Open Digievo menu")]
        public void OpenDigievoMenu()
        {
            GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "dasdsa", "HMMMMMMM");
            activateScript = true;
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
        }

        public bool StartEvoEffect()
        {
            return mapScene.owner.data.system.GetSwitch("startEvoEffect");
        }
        private void CloseEvo()
        {
            if (locked)
            {
                mapScene.UnlockControl();
                locked = false;
            }

            showingShoices = false;
            isIni = false;
            activateScript = false;
            phase = 0;
            mapScene.menuWindow.ResetLayout(Yukar.Common.Rom.LayoutProperties.LayoutNode.UsageInGame.Choice);
            mapScene.menuWindow.Show();
        }

        internal static class Tools
        {
            public static string StringFromTo(string str, string from = "(", string to = ")")
            {
                if (string.IsNullOrEmpty(str) || !str.Contains(from) || !str.Contains(to)) return null;
                int fromInt = str.IndexOf(from) + from.Length;
                int toInt = str.LastIndexOf(to);
                return str.Substring(fromInt, toInt - fromInt);
            }
            public static void PushLog(string msg)
            {
                GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "SpecialSkills", msg);
            }

            public static string GetTagValue(string tags, string targetTag)
            {
                var type = StringFromTo(tags.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).
                FirstOrDefault(x => x.ToLower().Contains(targetTag)));
                return type;
            }

            public static List<string> GetTagMultipleValues(string tags, string targetTag)
            {
                List<string> strings = new List<string>();
                var type = tags.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).Where(x => x.ToLower().Contains(targetTag));
                foreach (var types in type)
                {
                    var handledValue = Tools.StringFromTo(types);
                    strings.Add(handledValue);
                }
                return strings;
            }
        }
    }
}

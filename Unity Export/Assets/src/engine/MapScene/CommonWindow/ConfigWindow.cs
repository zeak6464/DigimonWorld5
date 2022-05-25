using Microsoft.Xna.Framework;
using KeyStates = Yukar.Engine.Input.KeyStates;
using StateType = Yukar.Engine.Input.StateType;

namespace Yukar.Engine
{
    class ConfigWindow : PagedSelectWindow
    {
        string[] strs;
        bool[] flags;
        int[] settings;
        int[] maxIndexes;

        //public int cursorImgId;
        public int selectedImgId;

        string[] messageSpeedTexts;

        string[] menuTypeTexts;

        /*
        string[] controlTypeTexts = 
        {
            "キーボード・コントローラ",
            "マウス",
        };
        */

        string[][] settingTextsArray;
        public const int RESTORE_INDEX = 4;

        internal ConfigWindow()
        {
            cursorImgId = Graphics.LoadImage("./res/system/arrow.png");
            selectedImgId = Graphics.LoadImage("./res/system/arrow_selected.png");
            disableLeftAndRight = true;
        }

        internal override void Show()
        {
            CreateMenu();

            Reset();

            maxItems = 5;
            setColumnNum(1);
            setRowNum(8, true);
            itemOffset = 16;

            base.Show();
        }

        private void CreateMenu()
        {
            messageSpeedTexts = new string[]{
                p.gs.glossary.message_moment,
                p.gs.glossary.message_fast,
                p.gs.glossary.message_normal,
                p.gs.glossary.message_slow,
            };

            menuTypeTexts = new string[]{
                p.gs.glossary.config_memory,
                p.gs.glossary.config_not_memory,
            };

            settingTextsArray = new string[][]{
                null,
                null,
                messageSpeedTexts,
                menuTypeTexts,
                //controlTypeTexts,
            };

            strs = new string[5];
            strs[0] = p.gs.glossary.bgm;
            strs[1] = p.gs.glossary.se;
            strs[2] = p.gs.glossary.message_speed;
            strs[3] = p.gs.glossary.cursor_position;
            //strs[4] = "操作方法";
            strs[4] = p.gs.glossary.restore_defaults;

            flags = new bool[5];
            flags[0] = flags[1] = flags[2] = flags[3] = flags[4] = true;

            settings = new int[4];
            maxIndexes = new int[4];
        }

        internal override void UpdateCallback()
        {
            base.UpdateCallback();

            if (selected < 0 || selected > 3 || returnSelected != 0)
                return;

            if (Input.KeyTest(StateType.REPEAT, KeyStates.LEFT))
            {
                if (settings[selected] == 0)
                    return;

                settings[selected]--;

                Audio.setMasterVolume((float)settings[0] / 100, (float)settings[1] / 100);
                Audio.PlaySound(p.owner.parent.owner.se.select);
            }
            else if (Input.KeyTest(StateType.REPEAT, KeyStates.RIGHT))
            {
                if (settings[selected] == maxIndexes[selected])
                    return;

                settings[selected]++;

                Audio.setMasterVolume((float)settings[0] / 100, (float)settings[1] / 100);
                Audio.PlaySound(p.owner.parent.owner.se.select);
            }

            editValuesByTouch();
        }

        // コンフィグタッチ選択
        // 押しっぱなしにも対応すべき
        internal void editValuesByTouch()
        {
#if !WINDOWS
            const int SEPARATE_X = 320;
            const int CONTENT_OFFSET_X = 20;    // 正しい？
            var offsetX = (int)windowPos.X - innerWidth / 2;
            int imgWidth = Graphics.GetImageWidth(cursorImgId);
            int imgHeight = Graphics.GetImageHeight(cursorImgId);
            if (UnityEngine.Input.GetMouseButton(0))
            {
                var touchPos = InputCore.getTouchPos(0);
                SharpKmyIO.Controller.repeatTouchLeft = false;
                SharpKmyIO.Controller.repeatTouchRight = false;
                for (int i = 0; i < maxItems - 1; i++)
                {
                    int cursorY = i * (itemHeight + itemOffset) + itemHeight / 2;
                    var x0 = SEPARATE_X + offsetX; // 仮
                    var y0 = cursorY;
                    var x1 = x0 + imgWidth;
                    var y1 = y0 + imgHeight;
                    if (touchPos.x < x1)
                    {
                        if (x0 < touchPos.x && touchPos.x < x1 && y0 < touchPos.y && touchPos.y < y1)
                        {
                            SharpKmyIO.Controller.repeatTouchRight = false;
                            SharpKmyIO.Controller.repeatTouchLeft = true;
                            break;
                        }
                    }
                    else if (touchPos.x > innerWidth - offsetX)
                    {
                        x0 = innerWidth - offsetX;
                        x1 = x0 + imgWidth;
                        if (x0 < touchPos.x && touchPos.x < x1 && y0 < touchPos.y && touchPos.y < y1)
                        {
                            SharpKmyIO.Controller.repeatTouchLeft = false;
                            SharpKmyIO.Controller.repeatTouchRight = true;
                            break;
                        }
                    }
                    else
                    {
                        // ゲージ直接操作
                        // yは今後使う可能性を考慮して残しておく
                        var pos = new Vector2(SEPARATE_X + imgWidth + CONTENT_OFFSET_X + offsetX, i * (itemHeight + itemOffset));
                        var size = new Vector2(innerWidth - pos.X - imgWidth - CONTENT_OFFSET_X + offsetX, itemHeight);
                        if (y0 < touchPos.y && touchPos.y < y1 && pos.X < touchPos.x && touchPos.x < pos.X + size.X)
                        {
                            selected = i;
                            if (selected > 1) break;
                            settings[selected] = (int)((touchPos.x - pos.X) / size.X * 101);
                            break;
                        }
                    }
                }
            }
            if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                SharpKmyIO.Controller.repeatTouchLeft = false;
                SharpKmyIO.Controller.repeatTouchRight = false;
                Audio.setMasterVolume((float)settings[0] / 100, (float)settings[1] / 100);

                // 効果音のよりも上を触っていたら音を出す
                if (selected <= 1)
                {
                    Audio.PlaySound(p.owner.parent.owner.se.select);
                }
            }
#endif // #! WINDOWS
        }

        internal override void DrawCallback()
        {
            const int SEPARATE_X = 320;
            const int CONTENT_OFFSET_X = 20;

            // 右側を空ける
            var oldWidth = innerWidth;
            innerWidth = SEPARATE_X;
            DrawSelect(strs, flags);
            DrawReturnBox(true);
            innerWidth = oldWidth;

            int imgWidth = Graphics.GetImageWidth(cursorImgId);
            int imgHeight = Graphics.GetImageHeight(cursorImgId);

            if (selected >= 0 && selected <= 3 && returnSelected == 0)
            {
                // 設定変更ボタンを書く
                int cursorY = selected * (itemHeight + itemOffset) + (itemHeight - imgHeight) / 2;
                var srcRect = new Rectangle(innerWidth, cursorY, -imgWidth, imgHeight);
                var destRect = new Rectangle(0, 0, imgWidth, imgHeight);

                if (settings[selected] > 0)
                    Graphics.DrawImage(cursorImgId, SEPARATE_X, cursorY);
                if (settings[selected] < maxIndexes[selected])
                    Graphics.DrawImage(cursorImgId, srcRect, destRect);
            }

            // 各設定を書く
            for (int i = 0; i < 4; i++)
            {
                var pos = new Vector2(SEPARATE_X + imgWidth + CONTENT_OFFSET_X, i * (itemHeight + itemOffset));
                var size = new Vector2(innerWidth - pos.X - imgWidth - CONTENT_OFFSET_X, itemHeight);

                if (i < 2)
                    DrawGauge(i, pos, size);
                else
                    DrawString(i, pos, size);

                DrawSeparater(i, pos, size);
            }

        }

        private void DrawSeparater(int i, Vector2 pos, Vector2 size)
        {
            Graphics.DrawFillRect(0, (int)(pos.Y + size.Y + itemOffset / 2),
                innerWidth, 1, 64, 64, 64, 0);
        }

        private void DrawGauge(int i, Vector2 pos, Vector2 size)
        {
            // 数字を書く
            p.textDrawer.DrawString("" + settings[i], pos, size,
                TextDrawer.HorizontalAlignment.Center,
                TextDrawer.VerticalAlignment.Top, Color.White, 0.75f);

            // ゲージを書く
            pos.Y += itemHeight - 24;
            size.Y = 16;
            Graphics.DrawFillRect((int)pos.X, (int)(pos.Y), (int)size.X, (int)size.Y, 128, 128, 128, 128);

            pos.X += 2;
            pos.Y += 2;
            size.X -= 4;
            size.Y -= 4;
            Graphics.DrawFillRect((int)pos.X, (int)(pos.Y), (int)size.X, (int)size.Y, 0, 0, 0, 128);

            size.X = size.X * settings[i] / 100;
            Graphics.DrawFillRect((int)pos.X, (int)(pos.Y), (int)size.X, (int)size.Y, 64, 48, 192, 255);

            size.Y /= 2;
            pos.Y += size.Y / 2;
            Graphics.DrawFillRect((int)pos.X, (int)(pos.Y), (int)size.X, (int)size.Y, 32, 16, 96, 255);

            pos.Y += size.Y / 2;
            Graphics.DrawFillRect((int)pos.X, (int)(pos.Y), (int)size.X, (int)size.Y, 16, 8, 64, 255);
        }

        private void DrawString(int i, Vector2 pos, Vector2 size)
        {
            p.textDrawer.DrawString(settingTextsArray[i][settings[i]], pos, size,
                TextDrawer.HorizontalAlignment.Center,
                TextDrawer.VerticalAlignment.Center, Color.White);
        }

        internal void Apply()
        {
            p.owner.parent.owner.data.system.bgmVolume = settings[0];
            p.owner.parent.owner.data.system.seVolume = settings[1];
            p.owner.parent.owner.data.system.messageSpeed = (Common.GameData.SystemData.MessageSpeed)settings[2];
            p.owner.parent.owner.data.system.cursorPosition = (Common.GameData.SystemData.CursorPosition)settings[3];
            //p.owner.parent.owner.data.system.controlType = (Common.GameData.SystemData.ControlType)settings[4];
        }

        internal void Reset()
        {
            settings[0] = p.owner.parent.owner.data.system.bgmVolume;
            maxIndexes[0] = 100;
            settings[1] = p.owner.parent.owner.data.system.seVolume;
            maxIndexes[1] = 100;
            settings[2] = (int)p.owner.parent.owner.data.system.messageSpeed;
            maxIndexes[2] = (int)Common.GameData.SystemData.MessageSpeed.SLOW;
            settings[3] = (int)p.owner.parent.owner.data.system.cursorPosition;
            maxIndexes[3] = (int)Common.GameData.SystemData.CursorPosition.NOT_KEEP;
            //settings[4] = (int)p.owner.parent.owner.data.system.controlType;
            //maxIndexes[4] = (int)Common.GameData.SystemData.ControlType.MOUSE_TOUCH;
        }

        internal void RestoreDefaults()
        {
            p.owner.parent.owner.data.system.restoreDefaults();

            settings[0] = p.owner.parent.owner.data.system.bgmVolume;
            settings[1] = p.owner.parent.owner.data.system.seVolume;
            settings[2] = (int)p.owner.parent.owner.data.system.messageSpeed;
            settings[3] = (int)p.owner.parent.owner.data.system.cursorPosition;
            //settings[4] = (int)p.owner.parent.owner.data.system.controlType;
            Audio.setMasterVolume((float)settings[0] / 100, (float)settings[1] / 100);
        }
    }
}

using System;
using System.Collections.Generic;
using Title = Yukar.Common.Rom.GameSettings.TitleScreen;

namespace Yukar.Engine
{
    class TitleScene
    {
        private GameMain owner;
        private Title title;
        private TitleDrawer titleDrawer;

        private SaveFileList list;
        private SaveDataDetail detail;
        private float screenAlpha;
        private int selectedSaveIndex;
        private ConfigWindow config;

        private bool isVisibleLoadingText;

        private enum State
        {
            FADEIN,
            TITLE,
            CONTINUE,
            FADEOUT_FOR_CONTINUE,
            FADEOUT_FOR_NEWGAME,
            CONFIG,
        }
        private State state;

        internal TitleScene(GameMain owner, Yukar.Common.Catalog catalog)
        {
            this.owner = owner;
            this.title = catalog.getGameSettings().title;

            titleDrawer = new TitleDrawer();
            titleDrawer.SetTitleData(title, catalog);

            // おまじない(最初の2フレーム、何故かアニメ完了後のタイトルが見えちゃうので)
            titleDrawer.Update();
            titleDrawer.Update();

            var res = owner.getMenuParamSet();
            list = new SaveFileList();
            list.Initialize(res, 8 + 312 / 2, 16 + 488 / 2, 312, 486);
            detail = new SaveDataDetail();
            detail.Initialize(res, 328 + 624 / 2, 16 + 488 / 2, 624, 486);
            config = new ConfigWindow();
            config.Initialize(res, 16 + 936 / 2, 16 + 488 / 2, 936, 486);

            InitializeSelectMenu(list);
        }

        internal void initialize()
        {
            state = State.FADEIN;
            screenAlpha = 255;
            isVisibleLoadingText = false;

            var res = owner.getMenuParamSet();
            list.Initialize(res, 8 + 312 / 2, 16 + 488 / 2, 312, 486);
            detail.Initialize(res, 328 + 624 / 2, 16 + 488 / 2, 624, 486);
            config.Initialize(res, 16 + 936 / 2, 16 + 488 / 2, 936, 486);

            InitializeSelectMenu(list);

            if (title.bgm != Guid.Empty)
            {
                var bgm = owner.catalog.getItemFromGuid(title.bgm) as Common.Resource.Bgm;

                if (bgm != null) Audio.PlayBgm(bgm);
                else Audio.StopBgm();
            }
            else
            {
                Audio.StopBgm();
            }

            if (title.bgs != Guid.Empty)
            {
                var bgs = owner.catalog.getItemFromGuid(title.bgs) as Common.Resource.Bgs;

                if (bgs != null) Audio.PlayBgs(bgs);
                else Audio.StopBgs();
            }
            else
            {
                Audio.StopBgs();
            }
        }

        private void InitializeSelectMenu(SaveFileList saveFileList)
        {
            // セーブファイルがあったらカーソルを「続きから」に移動しておく
            // セーブファイルが1つも無かったら「続きから」の項目を選択できないようにする
            if (saveFileList.availableDataCount >= 1)
            {
                titleDrawer.SetSelectItemEnable(TitleDrawer.SelectItemKind.Continue, true);

                titleDrawer.SetSelectItem(TitleDrawer.SelectItemKind.Continue);
            }
            else
            {
                titleDrawer.SetSelectItemEnable(TitleDrawer.SelectItemKind.Continue, false);

                titleDrawer.SetSelectItem(TitleDrawer.SelectItemKind.NewGame);
            }
        }

        internal void finalize()
        {
            titleDrawer.Release();
        }

        internal void Update()
        {
            if (state != State.FADEIN && state != State.FADEOUT_FOR_NEWGAME && state != State.FADEOUT_FOR_CONTINUE)
                titleDrawer.Update();
            list.Update();
            detail.Update();
            config.Update();

            if (state == State.FADEIN)
            {
                isVisibleLoadingText = false;

                screenAlpha -= 8 * GameMain.getRelativeParam60FPS();
                if (screenAlpha < 0)
                    screenAlpha = 0;

                if (screenAlpha == 0)
                    state = State.TITLE;
            }
            else if (!titleDrawer.IsPlayAnimation && state == State.TITLE)
            {
                UpdateInput();
            }
            else if (state == State.CONTINUE)
            {
                detail.CurrentIndex = list.selected;

                if (list.result != Common.Util.RESULT_SELECTING)
                {
                    if (list.result != Common.Util.RESULT_CANCEL && detail.isExisted())
                    {
                        Audio.PlaySound(owner.se.decide);
                        state = State.FADEOUT_FOR_CONTINUE;
                        selectedSaveIndex = list.result;
                    }
                    else
                    {
                        Audio.PlaySound(owner.se.cancel);
                        state = State.TITLE;
                    }

                    list.Hide();
                    detail.Hide();
                }
            }
            else if (state == State.FADEOUT_FOR_NEWGAME)
            {
                screenAlpha += 8 * GameMain.getRelativeParam60FPS();

                if (screenAlpha >= 255 && isVisibleLoadingText)
                {
                    screenAlpha = 255;
                    owner.DoReset();
                    owner.ChangeScene(GameMain.Scenes.MAP);
                }

                if (screenAlpha >= 200) isVisibleLoadingText = true;
            }
            else if (state == State.FADEOUT_FOR_CONTINUE)
            {
                screenAlpha += 8 * GameMain.getRelativeParam60FPS();

                if (screenAlpha >= 255 && isVisibleLoadingText)
                {
                    screenAlpha = 255;
                    owner.DoLoad(selectedSaveIndex);
                    owner.ChangeScene(GameMain.Scenes.MAP);
                }

                if (screenAlpha >= 200) isVisibleLoadingText = true;
            }
            else if (state == State.CONFIG)
            {
                if (config.result != Common.Util.RESULT_SELECTING)
                {
                    if (config.result == ConfigWindow.RESTORE_INDEX)
                    {
                        // 初期設定に戻す
                        Audio.PlaySound(owner.se.item);
                        config.RestoreDefaults();
                        config.result = Common.Util.RESULT_SELECTING;
                        return;
                    }
                    else if (config.result != Common.Util.RESULT_CANCEL)
                    {
                        config.result = Common.Util.RESULT_SELECTING;
                        config.Apply();
#if WINDOWS
#else
                        return;
#endif
                    }
                    else
                    {
                        //Audio.PlaySound(owner.se.decide); #24214
                        config.Apply();
                    }

                    state = State.TITLE;
                    Audio.PlaySound(owner.se.cancel);
                    config.Hide();
                }
            }
        }

        private void UpdateInput()
        {
            bool isMovePrevItem = false;
            bool isMoveNextItem = false;

            switch (title.selectItemSortOrientation)
            {
                case Title.SelectItemSortOrientation.Vertical:
                case Title.SelectItemSortOrientation.DiagonalRightUp:
                case Title.SelectItemSortOrientation.DiagonalRightDown:
                    isMovePrevItem = Input.KeyTest(Input.StateType.REPEAT, Input.KeyStates.UP);
                    isMoveNextItem = Input.KeyTest(Input.StateType.REPEAT, Input.KeyStates.DOWN);
                    break;

                case Title.SelectItemSortOrientation.Horizontal:
                    isMovePrevItem = Input.KeyTest(Input.StateType.REPEAT, Input.KeyStates.LEFT);
                    isMoveNextItem = Input.KeyTest(Input.StateType.REPEAT, Input.KeyStates.RIGHT);
                    break;
            }

            var decidedByTouch = false;
#if WINDOWS
#else
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                var touchPos = InputCore.getTouchPos(0);
                int x0 = 0, y0 = 0, x1 = 0, y1 = 0;
                List<SharpKmyMath.Vector2> selectItemPos = titleDrawer.getSelectItemPos();
                List<SharpKmyMath.Vector2> selectItemSize = titleDrawer.getSelectItemSize();
                for (var i = 0; i < selectItemPos.Count; i++)
                {
                    switch (title.selectItemSortOrientation)
                    {
                        case Title.SelectItemSortOrientation.Vertical:
                            x0 = (int)(selectItemPos[i].x - selectItemSize[i].x / 2);
                            y0 = (int)(selectItemPos[i].y - selectItemSize[i].y * i / 2);
                            x1 = (int)(selectItemPos[i].x + selectItemSize[i].x / 2);
                            y1 = (int)(selectItemPos[i].y + selectItemSize[i].y * (2 - i) / 2);
                            break;
                        case Title.SelectItemSortOrientation.DiagonalRightUp:
                            x0 = (int)(selectItemPos[i].x - selectItemSize[i].x * (2 - i) / 2);
                            y0 = (int)(selectItemPos[i].y - selectItemSize[i].y * i / 2);
                            x1 = (int)(selectItemPos[i].x + selectItemSize[i].x * i / 2);
                            y1 = (int)(selectItemPos[i].y + selectItemSize[i].y * (2 - i) / 2);
                            break;
                        case Title.SelectItemSortOrientation.DiagonalRightDown:
                            x0 = (int)(selectItemPos[i].x - selectItemSize[i].x * i / 2);
                            y0 = (int)(selectItemPos[i].y - selectItemSize[i].y * i / 2);
                            x1 = (int)(selectItemPos[i].x + selectItemSize[i].x * (2 - i) / 2);
                            y1 = (int)(selectItemPos[i].y + selectItemSize[i].y * (2 - i) / 2);
                            break;
                        case Title.SelectItemSortOrientation.Horizontal:
                            x0 = (int)(selectItemPos[i].x);
                            y0 = (int)(selectItemPos[i].y - selectItemSize[i].y / 2);
                            x1 = (int)(selectItemPos[i].x + selectItemSize[i].x);
                            y1 = (int)(selectItemPos[i].y + selectItemSize[i].y / 2);
                            break;
                    }
                    if (x0 < touchPos.x && touchPos.x < x1 && y0 < touchPos.y && touchPos.y < y1)
                    {
                        if ((int)titleDrawer.CurrentSelectItemKind == i) decidedByTouch = true;
                        else Audio.PlaySound(owner.se.select);
                        titleDrawer.SetSelectItem((TitleDrawer.SelectItemKind)Enum.ToObject(typeof(TitleDrawer.SelectItemKind), i));
                    }
                }
            }

#endif

            if (isMovePrevItem)
            {
                titleDrawer.MoveCursorPrevItem();
            }

            if (isMoveNextItem)
            {
                titleDrawer.MoveCursorNextItem();
            }

            if (isMovePrevItem || isMoveNextItem)
            {
                titleDrawer.ResetCursorAnimation();
                Audio.PlaySound(owner.se.select);
            }

            if (Input.KeyTest(Input.StateType.TRIGGER, Input.KeyStates.DECIDE) || decidedByTouch)
            {
                if (titleDrawer.IsEnableCurrentSelectItem)
                {
                    Audio.PlaySound(owner.se.decide);

                    switch (titleDrawer.CurrentSelectItemKind)
                    {
                        case TitleDrawer.SelectItemKind.NewGame:
                            state = State.FADEOUT_FOR_NEWGAME;
                            break;

                        case TitleDrawer.SelectItemKind.Continue:
                            showLoadMenu();
                            break;

                        case TitleDrawer.SelectItemKind.Option:
                            showConfig();
                            break;
                    }
                }
                else
                {
                    Audio.PlaySound(owner.se.cancel);
                }
            }
        }

        private void showConfig()
        {
            state = State.CONFIG;
            config.Show();
        }

        private void showLoadMenu()
        {
            state = State.CONTINUE;
            list.Show();
            detail.Show();
        }

        internal void Draw()
        {
            Graphics.BeginDraw();

            titleDrawer.Draw(Graphics.ScreenWidth, Graphics.ScreenHeight, state == State.FADEOUT_FOR_CONTINUE || state == State.TITLE);
            list.Draw();
            detail.Draw();
            config.Draw();
            Graphics.DrawFillRect(0, 0, Graphics.ViewportWidth, Graphics.ViewportHeight, 0, 0, 0, (byte)screenAlpha, 1);

            if (isVisibleLoadingText)
            {
                owner.DrawLoadingText();
            }

            Graphics.EndDraw();

#if ENABLE_VR
            // VR描画
            if (SharpKmyVr.Func.IsReady())
            {
                VrDrawer.DrawSimple(owner.getScreenAspect());
            }
#endif  // #if ENABLE_VR
        }
    }
}

using System.Collections.Generic;
using KeyStates = Yukar.Engine.Input.KeyStates;
using StateType = Yukar.Engine.Input.StateType;
using Microsoft.Xna.Framework;

namespace Yukar.Engine
{
    using InputObjects = List<List<InputObject>>;
    #region InputStringWindow
    /// <summary>
    /// 文字を入力するウィンドウ
    /// </summary>
    class InputStringWindow
    {
        internal CommonWindow.ParamSet res;
        private OutputStringWindow outputStringWindow; // 文字列の出力用ウィンドウ
        private InputWindow inputWindow; // 入力用のウィンドウ

        private const int WINDOW_WIDTH = 500;
        private const int OUTPUT_WINDOW_HEIGHT = 100;
        private const int DISTANCE_FROM_INPUT_2_OUTPUT = 70;


        #region accessor
        public string GetInputString()
        {
            return outputStringWindow.GetInputs();
        }

        public void SetInputString(string inputString)
        {
            outputStringWindow.SetInputs(inputString);
        }


        public void SetStringLength(int stringLength)
        {
            outputStringWindow.SetStringLength(stringLength);
        }

        #endregion // accessor

        internal InputStringWindow()
        {
            outputStringWindow = new OutputStringWindow();
            inputWindow = new InputWindow();
            outputStringWindow.SetInputWindow(inputWindow);
        }

        public void Initialize(Common.Resource.Window winRes, int winImgId,
            Common.Resource.Window selRes, int selImgId,
            Common.Resource.Window unselRes, int unselImgId,
            Common.Resource.Character scrollRes, int scrollImgId,
            MenuController menuController)
        {
            res = new CommonWindow.ParamSet();
            res.owner = menuController;
            // ウィンドウの読み込み
            res.window = new WindowDrawer(winRes, winImgId);
            res.unselBox = new WindowDrawer(unselRes, unselImgId);
            res.selBox = new WindowDrawer(selRes, selImgId);
            res.textDrawer = new TextDrawer(1);
        }

        public void Move(int x, int y)
        {
            outputStringWindow.Initialize(res, x, y, WINDOW_WIDTH, OUTPUT_WINDOW_HEIGHT);
            inputWindow.Initialize(res, x, y + DISTANCE_FROM_INPUT_2_OUTPUT, WINDOW_WIDTH, 0);
        }

        public void LoadStrings(string[] loadStrings)
        {
            inputWindow.LoadStrings(loadStrings);
        }

        internal void Show()
        {
            outputStringWindow.Show();
            inputWindow.Show();
        }

        internal void Hide()
        {
            outputStringWindow.Hide();
            inputWindow.Hide();
        }

        internal void Update()
        {
            inputWindow.Update();
            outputStringWindow.Update();
        }

        internal void Draw()
        {
            outputStringWindow.Draw();
            inputWindow.Draw();
        }

        public bool haveFinisedInputing()
        {
            if (inputWindow.GetInputState() == InputObject.InputType.END || inputWindow.GetInputState() == InputObject.InputType.CANCEL)
            {
                return true;
            }
            return false;
        }
    }
    #endregion // InputStringWindow

    #region OutputStringWindow
    /// <summary>
    /// 文字列の出力用ウィンドウ
    /// </summary>
    class OutputStringWindow : CommonWindow
    {
        private string inputs = ""; // 入力された文字
        private string defaultInputs;
        private InputWindow inputWindow;
        private int stringLength;

        #region const
        private const float DRAW_POSITION_X = 240.0f; // 文字を描画する初期位置 X
        private const float DRAW_POSITION_Y = 15.0f; // 文字を描画する初期値 Y
        private const float OFFSET_WIDTH = 30.0f; // 文字と文字の幅
        private const float HIGHLIGHT_OFFSET_X = -19.0f; // 文字と同じように位置を指定するとハイライトがずれるのでその調整X
        private const float HIGHLIGHT_OFFSET_Y = -3.0f; // 文字と同じように位置を指定するとハイライトがずれるのでその調整Y
        #endregion // const

        #region accessor
        /// <summary>
        /// 入力文字の取得
        /// </summary>
        /// <returns></returns>
        public string GetInputs()
        {
            return inputs;
        }

        public void SetInputs(string inputs)
        {
            if (stringLength >= inputs.Length)
            {
                this.inputs = inputs;
                defaultInputs = inputs;
            }
            else
            {
                this.inputs = "";
                defaultInputs = "";
            }
            return;
        }

        /// <summary>
        /// 入力できる文字列の制限値の変更
        /// </summary>
        /// <param name="stringLength"></param>
        public void SetStringLength(int stringLength)
        {
            this.stringLength = stringLength;
        }

        /// <summary>
        /// 入力ウィンドウを設定
        /// </summary>
        /// <param name="inputWindow"></param>
        public void SetInputWindow(InputWindow inputWindow)
        {
            this.inputWindow = inputWindow;
        }

        #endregion // accessor

        /// <summary>
        /// コンストラクタ
        /// </summary>
        internal OutputStringWindow()
        {
            stringLength = 10;
        }

        /// <summary>
        /// 初期化 描画する位置を計算する
        /// </summary>
        /// <param name="pset">親ウィンドウのパラメータ</param>
        /// <param name="x">表示したい位置X</param>
        /// <param name="y">表示したい位置Y</param>
        /// <param name="maxWidth">表示する幅</param>
        /// <param name="maxHeight">表示する高さ</param>
        public override void Initialize(ParamSet pset, int x, int y, int maxWidth, int maxHeight)
        {
            base.Initialize(pset, x, y, maxWidth, maxHeight);
        }

        internal override void Show()
        {
            base.Show();
        }

        /// <summary>
        /// 入力内容に対して文字を変更する
        /// </summary>
        internal override void UpdateCallback()
        {
            switch (inputWindow.GetInputState())
            {
                case InputObject.InputType.NONE:
                    break;
                case InputObject.InputType.CHARACTER:
                    if (inputs.Length < stringLength)
                    {
                        Audio.PlaySound(p.owner.parent.owner.se.textInput);
                        inputs += inputWindow.GetCurrentInput();
                    }
                    else
                    {
                        Audio.PlaySound(p.owner.parent.owner.se.textDelete);
                    }
                    break;
                case InputObject.InputType.SPACE:
                    if (inputs.Length < stringLength)
                    {
                        Audio.PlaySound(p.owner.parent.owner.se.textInput);
                        inputs += ' ';
                    }
                    else
                    {
                        Audio.PlaySound(p.owner.parent.owner.se.textDelete);
                    }
                    break;
                case InputObject.InputType.DELETE:
                    if (inputs.Length > 0)
                    {
                        inputs = inputs.Remove(inputs.Length - 1);
                    }
                    Audio.PlaySound(p.owner.parent.owner.se.textDelete);
                    break;
                case InputObject.InputType.END:
                    Audio.PlaySound(p.owner.parent.owner.se.decide);
                    break;
                case InputObject.InputType.CANCEL:
                    Audio.PlaySound(p.owner.parent.owner.se.cancel);
                    inputs = defaultInputs;
                    break;
                default:
                    break;
            }
        }
        internal override void DrawCallback()
        {
            var halfStringLength = (stringLength - 1) * 0.5f;
            var drawPosition = new Vector2(DRAW_POSITION_X - (OFFSET_WIDTH * halfStringLength), DRAW_POSITION_Y);

            // 文字の描画
            for (int i = 0; i < inputs.Length; ++i)
            {
                p.textDrawer.DrawString(inputs[i].ToString(), drawPosition, 0, TextDrawer.HorizontalAlignment.Center, Color.White);

                drawPosition.X += OFFSET_WIDTH;
            }
            // ハイライトウィンドウ
            if (inputs.Length < stringLength)
            {
                p.selBox.Draw(new Vector2(drawPosition.X + HIGHLIGHT_OFFSET_X, drawPosition.Y + HIGHLIGHT_OFFSET_Y), new Vector2(40, 40), inputWindow.GetBlinker().getColor());
            }
            else
            {
                drawPosition.X -= OFFSET_WIDTH;
                p.selBox.Draw(new Vector2(drawPosition.X + HIGHLIGHT_OFFSET_X, drawPosition.Y + HIGHLIGHT_OFFSET_Y), new Vector2(40, 40), inputWindow.GetBlinker().getColor());
            }

            // 下の白い棒
            drawPosition = new Vector2(DRAW_POSITION_X - OFFSET_WIDTH * halfStringLength, DRAW_POSITION_Y);
            drawPosition.X -= OFFSET_WIDTH * 0.35f;
            drawPosition.Y += 35.0f;
            for (int i = 0; i < stringLength; ++i)
            {
                Graphics.DrawFillRect((int)drawPosition.X, (int)drawPosition.Y, (int)(OFFSET_WIDTH * 0.8), 5, 255, 255, 255, 255);
                drawPosition.X += OFFSET_WIDTH;
            }
        }
    }
    #endregion // OutputStringWindow

    #region InputWindow
    /// <summary>
    /// 入力用のウィンドウ
    /// </summary>
    class InputWindow : CommonWindow
    {
        private List<InputObjects> inputObjectsList; // 入力文字
        private int maxListIndex; // リストのインデックスの最大値
        private int selectListIndex; // 選択しているインデックス
        private int maxRow;
        private char currentInput; // 最後に選択した文字
        private int selectingRow; // 選んでいる行
        private int selectingColumn; // 選んでいる列
        private InputObject.InputType inputState; // 入力の状態
        private Blinker blinker = new Blinker(Color.White, new Color(0.5f, 0.5f, 0.5f, 0.5f), 30); // ハイライトの色
        private float offsetWidth; // 文字と文字の間X
        #region const
        private const float START_DRAW_POSITION_X = 10.0f; // 文字の描画開始位置X
        private const float DISTANCE_FROM_LASTCHARACTER_2_WINDOW_EDGE = 50; // 文字末尾からウィンドウの端までの距離 (ウィンドウをリサイズする際に使用)
        private const float START_DRAW_POSITION_Y = 5.0f; // 文字の描画開始位置Y
        private const float DRAW_OFFSET_HEIGHT = 30.0f; // 文字と文字の間Y
        private const float HIGHLIGHT_OFFSET_X = -13.0f; // ハイライトがずれるのでその調整X
        private const float HIGHLIGHT_OFFSET_Y = -3.0f; // ハイライトがずれるのでその調整Y
        #endregion // const

#if !WINDOWS
        private Vector2 firstCharacterPositionTopAndLeft; // 1文字目の左上の位置
        private SharpKmyMath.Vector2 currentTouchPosition; // 最後に触った位置
        private bool canExecute; // コマンドを実行できるかどうか
#endif // #if !WINDWOS

        #region accessor
        public char GetCurrentInput()
        {
            return currentInput;
        }

        public InputObject.InputType GetInputState()
        {
            return inputState;
        }

        public Blinker GetBlinker()
        {
            return blinker;
        }
        #endregion // accessor

        internal InputWindow()
        {
            inputObjectsList = new List<InputObjects>();
            currentInput = '\0';
            selectingRow = 0;
            selectingColumn = 0;
            inputState = InputObject.InputType.NONE;
        }

        public override void Initialize(ParamSet pset, int x, int y, int maxWidth, int maxHeight)
        {
            base.Initialize(pset, x, y, maxWidth, maxHeight);
            // 列数はコマンドの部分を3列分確保して真ん中に描画する
            selectListIndex = 0;
            selectingRow = 0;
            selectingColumn = 0;
            Resize();
            windowPos.Y = y + maxWindowSize.Y * 0.5f;
#if !WINDOWS
            var windowTopAndLeft = new Vector2(windowPos.X - maxWindowSize.X * 0.5f, windowPos.Y - maxWindowSize.Y * 0.5f);
            firstCharacterPositionTopAndLeft = windowTopAndLeft;
            // 何故かずれるので生の値
            firstCharacterPositionTopAndLeft.X += START_DRAW_POSITION_X - 5.0f;
            firstCharacterPositionTopAndLeft.Y += START_DRAW_POSITION_Y + 10.0f;

            currentTouchPosition = new SharpKmyMath.Vector2(0.0f, 0.0f);
#endif // #if !WINDOWS
        }

        internal override void Show()
        {
            base.Show();
        }

        internal override void Hide()
        {
            base.Hide();
            inputState = InputObject.InputType.NONE;
        }

        internal override void UpdateCallback()
        {
            UpdateSelectRowColumn();
#if !WINDOWS 
            UpdateSelectRowColumnByTouch();
#endif // #if !WINDOWS 
            UpdateInput();
            blinker.update();
        }
        internal override void DrawCallback()
        {
            DrawInputObjects();
            DrawHighlightWindow();
        }

        /// <summary>
        /// ウィンドウのサイズを文字の数に合わせて変更する
        /// </summary>
        internal override void Resize()
        {
            offsetWidth = (maxWindowSize.X - START_DRAW_POSITION_X - DISTANCE_FROM_LASTCHARACTER_2_WINDOW_EDGE) / (inputObjectsList[0][0].Count - 1);
            maxWindowSize.Y = (maxRow + 1) * DRAW_OFFSET_HEIGHT + DRAW_OFFSET_HEIGHT * 0.25f;
        }

        /// <summary>
        /// 文字の読み込み ここの処理を変えれば表示する文字を変更できる
        /// </summary>
        /// <param name="loadStrings">読み込みたい文字列 スペースは\0に置換される</param>
        public void LoadStrings(string[] loadStrings)
        {
            inputObjectsList.Clear();
            for (var i = 0; i < 4; ++i)
            {
                inputObjectsList.Add(new InputObjects());
            }

            if (loadStrings[0].Length == 0)
            {
                string[] backOnly = { "\\c" };
                loadStrings = backOnly;
            }

            var index = 0;
            maxRow = 0;
            foreach (var inputString in loadStrings)
            {
                if (inputString == "")
                {
                    continue;
                }
                var row = LoadCharacter(inputString, index++);
                if (row > maxRow)
                {
                    maxRow = row;
                }
            }
            maxListIndex = index;

            Resize();
        }

        int LoadCharacter(string loadString, int index)
        {
            // 改行ごとの分割
            string[] lines = loadString.Split(new string[] { "\r\n", "\n" }, System.StringSplitOptions.None);
            bool canEnd = false;
            foreach (var line in lines)
            {
                if (line.Contains("\\c") || line.Contains("\\e"))
                {
                    canEnd = true;
                }
            }
            if (!canEnd)
            {
                lines[0] += "\\c";
            }
            var maxLength = 0;
            // 最大値を求める
            foreach (var line in lines)
            {
                // \\\\と\\sは一文字扱いなのでnull文字に置換する
                var stringLength = line.Replace("\\\\", "\n").Replace("\\s", "s").Length;
                if (maxLength < stringLength)
                {
                    maxLength = stringLength;
                }
            }
            // 空いている部分にNULL文字を入れる
            for (int i = 0; i < lines.Length; ++i)
            {
                // \\\\と\\sは一文字扱いなのでnull文字に置換する
                var replaceText = lines[i].Replace("\\\\", "\n").Replace("\\s", "\0");
                for (int j = 0; j < maxLength; ++j)
                {

                    if (replaceText.Length - 1 < j)
                    {
                        lines[i] += "\0";
                    }
                }
                // 空白文字は使わないのでnull文字に置換する
                lines[i] = lines[i].Replace(" ", "\0").Replace("　", "\0");
            }

            var inputState = InputObject.InputType.NONE;
            var imageId = 0;
            // 代入
            for (int i = 0; i < lines.Length; ++i)
            {
                if (lines[i].Length < 1)
                {
                    continue;
                }
                inputObjectsList[index].Add(new List<InputObject>());
                for (int j = 0; j < lines[i].Length; ++j)
                {
                    var charcter = lines[i][j];
                    // NULL文字の場合空のオブジェクトを生成する
                    if (charcter == '\0')
                    {
                        inputObjectsList[index][i].Add(new InputObject(charcter, InputObject.InputType.NONE));
                    }
                    // エスケープシーケンスではない場合文字を入力
                    else if (charcter != '\\')
                    {
                        inputObjectsList[index][i].Add(new InputObject(charcter, InputObject.InputType.CHARACTER));
                    }
                    else
                    {
                        // 一文字先の文字
                        var nextCharacter = lines[i][++j];
                        switch (nextCharacter)
                        {
                            case 'd':
                                inputState = InputObject.InputType.DELETE;
                                imageId = Graphics.LoadImage("./res/system/delete.png");
                                break;
                            case 'c':
                                inputState = InputObject.InputType.CANCEL;
                                imageId = Graphics.LoadImage("./res/system/back.png");
                                break;
                            case 's':
                                inputState = InputObject.InputType.CHARACTER;
                                imageId = 0;
                                nextCharacter = ' ';
                                break;
                            case 'e':
                                inputState = InputObject.InputType.END;
                                imageId = Graphics.LoadImage("./res/system/end.png");
                                break;
                            case '\\':
                                inputState = InputObject.InputType.CHARACTER;
                                imageId = 0;
                                break;
                            case 't':
                                inputState = InputObject.InputType.TYPE;
                                var imagePath = "./res/system/type" + index.ToString() + ".png";
                                imageId = Graphics.LoadImage(imagePath);
                                break;
                            default:
                                inputState = InputObject.InputType.NONE;
                                imageId = 0;
                                nextCharacter = '\0';
                                break;
                        }
                        // 特殊文字を挿入
                        inputObjectsList[index][i].Add(new InputObject(nextCharacter, inputState, imageId));
                        // 文字は抜ける
                        if (inputState == InputObject.InputType.CHARACTER)
                        {
                            continue;
                        }
                        // 画像なので一文字分あける
                        inputObjectsList[index][i].Add(new InputObject('\0', inputState));
                    }
                }
            }
            return inputObjectsList[index].Count;
        }

        /// <summary>
        /// カーソルの位置制御
        /// </summary>
        private void UpdateSelectRowColumn()
        {
            if (Input.KeyTest(StateType.REPEAT, KeyStates.UP))
            {
                var nextRow = SearchNextRow(true);
                if (selectingRow != nextRow)
                {
                    selectingRow = nextRow;
                    Audio.PlaySound(p.owner.parent.owner.se.select);
                }
            }
            if (Input.KeyTest(StateType.REPEAT, KeyStates.DOWN))
            {
                var nextRow = SearchNextRow(false);
                if (selectingRow != nextRow)
                {
                    selectingRow = nextRow;
                    Audio.PlaySound(p.owner.parent.owner.se.select);
                }
            }
            if (Input.KeyTest(StateType.REPEAT, KeyStates.LEFT))
            {
                var nextColumn = SearchNextColumn(true);
                if (selectingColumn != nextColumn)
                {
                    selectingColumn = nextColumn;
                    Audio.PlaySound(p.owner.parent.owner.se.select);
                }
            }
            if (Input.KeyTest(StateType.REPEAT, KeyStates.RIGHT))
            {
                var nextColumn = SearchNextColumn(false);
                if (selectingColumn != nextColumn)
                {
                    selectingColumn = nextColumn;
                    Audio.PlaySound(p.owner.parent.owner.se.select);
                }
            }
        }

        /// <summary>
        /// 次の行の位置を取得する
        /// </summary>
        /// <param name="up"><c>true</c> 上に検索 <c>false</c> 下に検索</param>
        /// <returns>次の行の位置</returns>
        private int SearchNextRow(bool up)
        {
            var result = SearchNextRow(up, selectingColumn);
            // 現在地が文字の場合終了
            if (inputObjectsList[selectListIndex][selectingRow][selectingColumn].GetInputState() == InputObject.InputType.CHARACTER)
            {
                // InpiutStateがNoneではなく文字が\0の場合画像の後半部分
                if (inputObjectsList[selectListIndex][result][selectingColumn].GetCharacter() == '\0')
                {
                    --selectingColumn;
                }
                return result;
            }

            var resultSecond = SearchNextRow(up, selectingColumn + 1);
            // 近い方を採用する
            if (up)
            {
                // 結果が両方選択している行より上
                if (result < selectingRow && resultSecond < selectingRow)
                {
                    if (result < resultSecond)
                    {
                        result = resultSecond;
                        ++selectingColumn;
                    }
                }
                // 2つ目だけ上
                else if (resultSecond < selectingRow)
                {
                    result = resultSecond;
                    ++selectingColumn;
                }
                // 両方下
                else
                {
                    if (result < resultSecond)
                    {
                        result = resultSecond;
                        ++selectingColumn;
                    }
                }
            }
            else
            {
                // 結果が両方選択している行より下
                if (result > selectingRow && resultSecond > selectingRow)
                {
                    if (result > resultSecond)
                    {
                        result = resultSecond;
                        ++selectingColumn;
                    }
                }
                // 2つ目だけ下
                else if (resultSecond > selectingRow)
                {
                    result = resultSecond;
                    ++selectingColumn;
                }
                // 両方上
                else
                {
                    if (result > resultSecond)
                    {
                        result = resultSecond;
                        ++selectingColumn;
                    }
                }
            }
            // InputStateがNoneではなく文字が\0の場合画像の後半部分
            if (inputObjectsList[selectListIndex][result][selectingColumn].GetCharacter() == '\0')
            {
                --selectingColumn;
            }

            return result;
        }

        /// <summary>
        /// 次の行の位置を取得する
        /// </summary>
        /// <param name="up"><c>true</c> 上に検索 <c>false</c> 下に検索</param>
        /// <param name="column">検索する行</param>
        /// <returns>次の行の位置</returns>
        private int SearchNextRow(bool up, int column)
        {
            List<int> rowsContainedSelectingColumn = new List<int>(); // 選択してる列が存在する行のまとめ
            // 同じ行がある列を取得
            for (int i = 0; i < inputObjectsList[selectListIndex].Count; ++i)
            {
                var charactersSameRow = inputObjectsList[selectListIndex][i];
                if (charactersSameRow[column].GetInputState() != InputObject.InputType.NONE)
                {
                    rowsContainedSelectingColumn.Add(i);
                }
            }
            var indexOfSelectingRaw = 0;
            // 取得した列の何番目が現在選択している列か検索
            for (int i = 0; i < rowsContainedSelectingColumn.Count; ++i)
            {
                if (rowsContainedSelectingColumn[i] == selectingRow)
                {
                    indexOfSelectingRaw = i;
                }
            }

            var result = selectingRow; // 1つ目の配列の結果
            if (up)
            {
                // 検索したものが一番上だったら一番下にする
                if (indexOfSelectingRaw == 0)
                {
                    result = rowsContainedSelectingColumn[rowsContainedSelectingColumn.Count - 1];
                }
                else
                {
                    result = rowsContainedSelectingColumn[indexOfSelectingRaw - 1];
                }
            }
            else
            {
                // 検索したものが一番下だったら一番上にする
                if (indexOfSelectingRaw == rowsContainedSelectingColumn.Count - 1)
                {
                    result = rowsContainedSelectingColumn[0];
                }
                else
                {
                    result = rowsContainedSelectingColumn[indexOfSelectingRaw + 1];
                }
            }

            return result;
        }

        /// <summary>
        /// 次の列の位置を取得する
        /// </summary>
        /// <param name="left"><c>true</c> 左に検索 <c>false</c> 右に検索</param>
        /// <returns>次の列の位置</returns>
        int SearchNextColumn(bool left)
        {
            var nextColumn = selectingColumn;
            var charactersSameRow = inputObjectsList[selectListIndex][selectingRow];
            var nextCharacter = '\0';
            var variation = 0; // インデックスの変化量
            if (left)
            {
                variation = -1;
            }
            else
            {
                variation = 1;
            }

            // 端まで検索
            nextColumn += variation;
            if (nextColumn >= 0 && nextColumn < inputObjectsList[selectListIndex][0].Count)
            {
                nextCharacter = charactersSameRow[nextColumn].GetCharacter();
                while (nextCharacter == '\0')
                {
                    // 端まで行ったら一旦終了
                    if (nextColumn == 0 || nextColumn >= inputObjectsList[selectListIndex][0].Count - 1)
                    {
                        break;
                    }
                    nextColumn += variation;
                    nextCharacter = charactersSameRow[nextColumn].GetCharacter();
                }
            }

            // 内容があれば次の内容の位置を返す
            if (nextCharacter != '\0')
            {
                return nextColumn;
            }

            // 頭もしくは尻尾から今選択している文字まで検索する
            if (left)
            {
                nextColumn = inputObjectsList[selectListIndex][0].Count - 1;
            }
            else
            {
                nextColumn = 0;
            }

            nextCharacter = charactersSameRow[nextColumn].GetCharacter();
            while (nextCharacter == '\0')
            {
                nextColumn += variation;
                nextCharacter = charactersSameRow[nextColumn].GetCharacter();
                // 選択している位置もしくは端に到達した場合
                if (nextColumn == selectingColumn || nextColumn == 0 || nextColumn == inputObjectsList[selectListIndex][0].Count - 1)
                {
                    if (nextCharacter == '\0')
                    {
                        return selectingColumn;
                    }
                }
            }

            return nextColumn;
        }

#if !WINDOWS
        /// <summary>
        /// タッチ時の選択している行列更新
        /// <para>画像を触った際、列をずらす</para>
        /// </summary>
        private void UpdateSelectRowColumnByTouch()
        {
            if (!UnityEngine.Input.GetMouseButton(0))
            {
                return;
            }
            currentTouchPosition = InputCore.getTouchPos(0);
            var touchingRow = CalculateTouchingRow(currentTouchPosition.y);
            var tempSelectedRow = selectingRow;
            if(touchingRow >= 0)
            {
                selectingRow = touchingRow;
            } 
            var touchingColumn = CalculateTouchingColumn(currentTouchPosition.x);
            
            canExecute = false;
            if (touchingColumn < 0 || touchingRow < 0)
            {
                selectingRow = tempSelectedRow;
                return;
            }

            var charactersSameRow = inputObjectsList[selectListIndex][touchingRow];
            var inputObject = charactersSameRow[touchingColumn];
            if (inputObject.GetInputState() == InputObject.InputType.NONE)
            {
                return;
            }
            // Noneかつ\0なら画像の後ろ側なので前側に変更する
            if (inputObject.GetCharacter() == '\0')
            {
                --touchingColumn;
            }
            canExecute = true;
            selectingRow = touchingRow;
            selectingColumn = touchingColumn;
        }


        /// <summary>
        /// タッチした行を取得する
        /// </summary>
        /// <param name="touchPositionY"></param>
        /// <returns></returns>
        private int CalculateTouchingRow(float touchPositionY)
        {
            var touchingRow = 0;
            // 範囲よりも上なら-1を返す
            if (touchPositionY < firstCharacterPositionTopAndLeft.Y)
            {
                return -1;
            }
            // 範囲よりも下なら-1を返す
            if (touchPositionY > firstCharacterPositionTopAndLeft.Y + (DRAW_OFFSET_HEIGHT * inputObjectsList[selectListIndex].Count))
            {
                return -1;
            }

            touchingRow = (int)((touchPositionY - firstCharacterPositionTopAndLeft.Y) / DRAW_OFFSET_HEIGHT);
            if (touchingRow > inputObjectsList[selectListIndex].Count - 1)
            {
                touchingRow = -1;
            }
            return touchingRow;
        }

        /// <summary>
        /// タッチした列を取得する
        /// </summary>
        /// <param name="touchPositionX">触っている位置X</param>
        /// <returns></returns>
        private int CalculateTouchingColumn(float touchPositionX)
        {
            var touchingColumn = 0;
            // 範囲よりも左なら-1を返す
            if (touchPositionX < firstCharacterPositionTopAndLeft.X)
            {
                return -1;
            }
            // 範囲よりも右なら-1を返す
            var edgeOfInput = firstCharacterPositionTopAndLeft.X + (offsetWidth * inputObjectsList[selectListIndex][0].Count);
            if (touchPositionX > edgeOfInput)
            {
                return -1;
            }

            touchingColumn = (int)((touchPositionX - firstCharacterPositionTopAndLeft.X) / offsetWidth);
            
            if (touchingColumn >= inputObjectsList[selectListIndex][0].Count)
            {
                return -1;
            }
            // 文字から離れすぎている場合はタッチしない
            var distanceFromInputObject = touchPositionX - firstCharacterPositionTopAndLeft.X - (touchingColumn * offsetWidth);
            var allowedDistance = 45;
            // 文字でなければ許容値を2倍で扱う
            if (inputObjectsList[selectListIndex][selectingRow][touchingColumn].GetInputState() == InputObject.InputType.NONE)
            {
                return -1;
            }
                if (inputObjectsList[selectListIndex][selectingRow][touchingColumn].GetInputState ()!= InputObject.InputType.CHARACTER)
            {
                allowedDistance *= 2;
            }
            if (distanceFromInputObject > allowedDistance)
            {
                touchingColumn = -1;
            }
            return touchingColumn;
        }
#endif // #if !WINDOWS

        /// <summary>
        /// 入力内容の更新
        /// </summary>
        private void UpdateInput()
        {
            inputState = InputObject.InputType.NONE;
            if (Input.KeyTest(StateType.TRIGGER, KeyStates.DECIDE))
            {
                ExecuteCommand();
            }
            if (Input.KeyTest(StateType.TRIGGER, KeyStates.CANCEL))
            {
                inputState = InputObject.InputType.DELETE;
            }
#if !WINDOWS
            UpdateInputByTounch();
#endif // #if !WINDOWS
        }

        /// <summary>
        /// コマンドの実行(文字入力、決定や削除など)
        /// </summary>
        private void ExecuteCommand()
        {
            inputState = inputObjectsList[selectListIndex][selectingRow][selectingColumn].GetInputState();
            if (inputState == InputObject.InputType.CHARACTER)
            {
                currentInput = inputObjectsList[selectListIndex][selectingRow][selectingColumn].GetCharacter();
            }
            else if (inputState == InputObject.InputType.TYPE)
            {
                if (++selectListIndex >= maxListIndex)
                {
                    selectListIndex = 0;
                }
                offsetWidth = (maxWindowSize.X - START_DRAW_POSITION_X - DISTANCE_FROM_LASTCHARACTER_2_WINDOW_EDGE) / (inputObjectsList[selectListIndex][0].Count - 1);

                // 配列の範囲外の場合は丸める
                selectingRow = selectingRow > inputObjectsList[selectListIndex].Count - 1 ? inputObjectsList[selectListIndex].Count - 1 : selectingRow;
                selectingColumn = selectingColumn > inputObjectsList[selectListIndex][0].Count - 1 ? inputObjectsList[selectListIndex][0].Count - 1 : selectingColumn;

                var inputObject = inputObjectsList[selectListIndex][selectingRow][selectingColumn];
                // 画像の後ろ部分なので前にする
                if (inputObject.GetInputState() != InputObject.InputType.NONE && inputObject.GetCharacter() == '\0')
                {
                    --selectingColumn;
                }
                // ページを切り替えた際、選択している位置が存在しない場合に正常値に変更する
                var willNeedMoveing = false;
                if (selectingColumn >= inputObjectsList[selectListIndex][0].Count - 1 || selectingRow >= inputObjectsList[selectListIndex].Count - 1)
                {
                    willNeedMoveing = true;
                }
                else if (inputObject.GetInputState() == InputObject.InputType.NONE)
                {
                    willNeedMoveing = true;
                }
                if (willNeedMoveing)
                {
                    selectingRow = 0;
                    selectingColumn = 0;
                    foreach (var inputObjects in inputObjectsList[selectListIndex])
                    {
                        foreach (var inputObj in inputObjects)
                        {
                            if (inputObj.GetInputState() != InputObject.InputType.NONE)
                            {
                                return;
                            }
                            ++selectingColumn;
                        }
                        selectingColumn = 0;
                        ++selectingRow;
                    }
                }
            }

        }

#if !WINDOWS
        private void UpdateInputByTounch()
        {
            // 範囲内をタッチしてるか
            if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                if (canExecute)
                {
                    ExecuteCommand();
                }
                return;
            }
        }
#endif // #if !WINDOWS

        /// <summary>
        /// 文字と画像を描画する
        /// </summary>
        private void DrawInputObjects()
        {
            var drawPosition = new Vector2(START_DRAW_POSITION_X, START_DRAW_POSITION_Y - maxWindowSize.Y * 0.5f);
            var row = 0;
            var drawCharacters = inputObjectsList[selectListIndex];
            foreach (var charactersOnSameRow in drawCharacters)
            {
                foreach (var characterDeawer in charactersOnSameRow)
                {
                    // 描画
                    characterDeawer.Draw(p, drawPosition);
                    drawPosition.X += offsetWidth;
                }
                ++row;
                drawPosition.X = START_DRAW_POSITION_X;
                drawPosition.Y += DRAW_OFFSET_HEIGHT;
            }
        }

        /// <summary>
        /// 点滅するウィンドウの描画
        /// </summary>
        private void DrawHighlightWindow()
        {
            var drawPosition = new Vector2(START_DRAW_POSITION_X + HIGHLIGHT_OFFSET_X + selectingColumn * offsetWidth, START_DRAW_POSITION_Y - maxWindowSize.Y * 0.5f + HIGHLIGHT_OFFSET_Y + selectingRow * DRAW_OFFSET_HEIGHT);
            if (inputObjectsList[selectListIndex][selectingRow][selectingColumn].GetInputState() == InputObject.InputType.CHARACTER)
            {
                p.selBox.Draw(drawPosition, new Vector2(40, 40), blinker.getColor());
            }
            else
            {
                // 画像ににすると中心がずれるようなので位置をずらす
                drawPosition.X = drawPosition.X + 5;
                p.selBox.Draw(drawPosition, new Vector2(80, 40), blinker.getColor());
            }

        }
    }
    #endregion // InputWindow

    #region CharacterDrawer
    /// <summary>
    /// 入力時に使用する物
    /// </summary>
    class InputObject
    {
        private char character; // 描画する文字
        private InputType inputState; // 入力の状態
        private int imageId; // 描画する画像
        public enum InputType
        {
            NONE,
            CHARACTER,
            SPACE,
            DELETE,
            END,
            CANCEL,
            TYPE,
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="character">描画する文字</param>
        public InputObject(char character, InputType inputState, int imageId = 0)
        {
            this.character = character;
            this.inputState = inputState;
            this.imageId = imageId;
        }

        #region accessor
        public char GetCharacter()
        {
            return character;
        }
        public InputType GetInputState()
        {
            return inputState;
        }
        public int GetImageId()
        {
            return imageId;
        }
        #endregion // accessor

        /// <summary>
        /// 描画する 色は設定した値
        /// </summary>
        ///  <param name="paramSet">ウィンドウのパラメータ</param>
        /// <param name="position">描画する位置</param>
        public void Draw(CommonWindow.ParamSet paramSet, Vector2 position)
        {
            if (inputState == InputType.CHARACTER)
            {
                if (character != ' ')
                {
                    paramSet.textDrawer.DrawString(character.ToString(), position, Microsoft.Xna.Framework.Color.White);
                }
                else
                {
                    Graphics.DrawImage(imageId, (int)position.X, (int)position.Y);
                }
            }
            else if (inputState != InputType.NONE)
            {
                Graphics.DrawImage(imageId, (int)position.X, (int)position.Y);
            }
        }

        /// <summary>
        /// オブジェクトの複製(ディープコピー)
        /// </summary>
        /// <returns>ディープコピーしたオブジェクト</returns>
        public InputObject Clone()
        {
            var clone = new InputObject(character, inputState, imageId);
            return clone;
        }
    }
    #endregion // CharacterDrawer
}
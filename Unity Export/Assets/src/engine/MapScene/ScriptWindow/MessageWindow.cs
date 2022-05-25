using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Yukar.Common.GameData;
using KeyStates = Yukar.Engine.Input.KeyStates;
using StateType = Yukar.Engine.Input.StateType;

namespace Yukar.Engine
{
    class MessageWindow
    {
        internal const int WINDOW_WIDTH = 720;
        internal const int WINDOW_HEIGHT = 160;
        internal const int WINDOW_OFFSET_Y = 32;
        internal const int TEXT_OFFSET = 8;
        private Vector2 windowPos = new Vector2();
        private Vector2 windowSize = new Vector2();
        private Vector2 textOffset = new Vector2();
        private MessageEntry curMessageEntry;
        internal enum WindowState
        {
            SHOW_WINDOW,
            SHOW_PAGEMARK,
            HIDE_PAGEMARK,
            HIDE_WINDOW,
        }
        internal enum WindowType
        {
            NONE,
            MESSAGE,
            DIALOGUE,
        }
        private WindowState windowState = WindowState.HIDE_WINDOW;
        public WindowState getWndowState() { return windowState; }
        public WindowType getWndowType() { return curMessage.winType; }

        public class MessageEntry
        {
            public int id;
            public string text;
            public int winVAligh;
            public WindowType winType;

            private MessageParts currentSett;

            public MessageEntry(MessageEntry inherit)
            {
                if (inherit != null)
                {
                    currentSett = inherit.currentSett;
                    currentSett.endTime = 0;
                    currentSett.text = "";
                    currentSett.line = 0;

                    id = inherit.id + 1;
                    text = inherit.text;    // 分割もとのテキストをそのまま代入しているだけで、正確じゃないので注意
                    winVAligh = inherit.winVAligh;
                    winType = inherit.winType;
                }
            }

            public class MessageParts
            {
                public float startTime { get { return endTime - speed * text.Length - (wait ? 60 : 0); } } // 開始時間はテキストから逆算する
                public float endTime;
                public float speed;
                public string text = "";
                public int size = 100;
                public Color color = Color.White;
                public int line;
                public bool bold;
                public bool italic;
                public bool underline;
                public bool wait;
                public bool noWait;
                public string ruby;

                public void CopyFrom(MessageParts src)
                {
                    endTime = src.endTime;
                    text = src.text;
                    bold = src.bold;
                    size = src.size;
                    italic = src.italic;
                    underline = src.underline;
                    color = src.color;
                    line = src.line;
                    wait = src.wait;
                    noWait = src.noWait;
                    speed = src.speed;
                }
            }
            public List<MessageParts> parts = new List<MessageParts>();
            internal float time;

            public int lineCount { get { return (parts.Count == 0 ? 0 : (parts.Last().line + 1)); } }

            internal void separateByCommands(float defaultSpeed, Color defaultColor)
            {
                if (parts.Count > 0)
                    return;

                if (currentSett == null)
                {
                    currentSett = new MessageParts();
                    currentSett.speed = defaultSpeed;
                    currentSett.color = defaultColor;
                }
                bool nextIsCommand = false;
                float currentSpeed = defaultSpeed;

                // メッセージ中のコマンドを分解してセットしてゆく
                for (int i = 0; i < text.Length; i++)
                {
                    Func<string[]> getParam = () =>
                    {
                        if (text.Length <= i + 1 || text[i + 1] != '[')
                            return new string[0];

                        var idx = text.IndexOf(']', i);
                        if (idx < 0)
                            return new string[0];

                        var result = text.Substring(i + 2, idx - i - 2).Split(',');
                        i = idx;
                        return result;
                    };

                    var chr = text[i];

                    Action newLine = () =>
                    {
                        if (string.IsNullOrEmpty(currentSett.text))
                        {
                            currentSett.text = " ";
                        }

                        var part = new MessageParts();
                        part.CopyFrom(currentSett);
                        parts.Add(part);
                        currentSett.text = "";

                        currentSett.line++;
                    };

                    if (chr == '\n')
                    {
                        newLine();
                        continue;
                    }
                    if (chr == '\\')
                    {
                        if (nextIsCommand)
                        {
                            // \\\\になってるときは\\を出力する
                            nextIsCommand = false;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(currentSett.text))
                            {
                                var part = new MessageParts();
                                part.CopyFrom(currentSett);
                                parts.Add(part);
                                currentSett.text = "";
                            }

                            nextIsCommand = true;
                            continue;
                        }
                    }
                    else if (nextIsCommand)
                    {
                        var param = getParam();

                        switch (chr)
                        {
                            case 'b':
                                currentSett.bold = !currentSett.bold;
                                break;
                            case 'i':
                                currentSett.italic = !currentSett.italic;
                                break;
                            case 'u':
                                currentSett.underline = !currentSett.underline;
                                break;
                            case 'z':
                                if (param.Length == 0)
                                    currentSett.size = 100;
                                else
                                    int.TryParse(param[0], out currentSett.size);
                                break;
                            case 'c':
                                if (param.Length == 0 || param[0].Length < 6)
                                    currentSett.color = defaultColor;
                                else
                                {
                                    byte r, g, b;
                                    byte.TryParse(param[0].Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out r);
                                    byte.TryParse(param[0].Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out g);
                                    byte.TryParse(param[0].Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out b);
                                    currentSett.color = new Color(r, g, b);
                                }
                                break;
                            case 'r':
                                if (param.Length == 0)
                                    break;

                                // パラメータひとつだった時は、次の1文字にルビがつく
                                if (param.Length == 1)
                                {
                                    i++;
                                    if (text.Length > i)
                                    {
                                        var part = new MessageParts();
                                        part.CopyFrom(currentSett);
                                        part.text = text[i].ToString();
                                        part.ruby = param[0];
                                        parts.Add(part);
                                        currentSett.endTime += currentSpeed;
                                    }
                                }
                                else
                                {
                                    var part = new MessageParts();
                                    part.CopyFrom(currentSett);
                                    part.text = param[0];
                                    part.ruby = param[1];
                                    parts.Add(part);
                                    currentSett.endTime += currentSpeed * param[0].Length;
                                }
                                break;
                            case 'w':
                                if(param.Length == 0)
                                {
                                    currentSett.endTime += 15;
                                }
                                else
                                {
                                    float wait = 0;
                                    float.TryParse(param[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out wait);
                                    currentSett.endTime += (int)(wait * 60);
                                }
                                break;
                            case '>':
                                currentSett.speed = 0;
                                currentSpeed = 0;
                                break;
                            case '<':
                                currentSett.speed = defaultSpeed;
                                currentSpeed = defaultSpeed;
                                break;
                            case '!':
                                {
                                    var part = new MessageParts();
                                    part.CopyFrom(currentSett);
                                    part.wait = true;
                                    part.endTime += 60;
                                    parts.Add(part);
                                    currentSett.endTime += 60;
                                }
                                break;
                            case '^':
                                {
                                    var part = new MessageParts();
                                    part.CopyFrom(currentSett);
                                    part.noWait = true;
                                    parts.Add(part);
                                }
                                break;
                            case 'n':
                                newLine();
                                break;
                        }

                        nextIsCommand = false;
                        continue;
                    }

                    currentSett.text += chr;
                    currentSett.endTime += currentSpeed;
                }

                // 最後が ^ タグでなければ、最後の一個をpushしてやる
                if(parts.Count == 0 || !parts.Last().noWait)
                {
                    var part = new MessageParts();
                    part.CopyFrom(currentSett);
                    parts.Add(part);
                }
            }

            internal bool isEmpty()
            {
                return text == "\n" || string.IsNullOrEmpty(text);
            }

            internal bool isWait()
            {
                var part = getCurrentPart();
                if (part == null)
                    return false;
                return part.wait;
            }

            internal MessageParts getCurrentPart()
            {
                foreach(var part in parts)
                {
                    if (time < part.endTime)
                        return part;
                }

                return null;
            }

            internal MessageEntry[] splitByLines(int lineCount)
            {
                int curLine = 0;
                float curTime = 0;

                var result = new List<MessageEntry>();
                var curEntry = new MessageEntry(this);
                curEntry.id--;
                var last = parts.LastOrDefault();

                foreach (var part in parts)
                {
                    part.line %= 3;

                    if (curLine == 2 && part.line == 0)
                    {
                        curTime = part.startTime;
                        result.Add(curEntry);
                        curEntry = new MessageEntry(curEntry);
                    }

                    curEntry.parts.Add(part);
                    part.endTime -= curTime;

                    if (part == last)
                    {
                        result.Add(curEntry);
                    }

                    curLine = part.line;
                }

                return result.ToArray();
            }

            internal string getLine(int line)
            {
                var result = "";

                foreach(var part in parts)
                {
                    if (part.line == line)
                        result += part.text;
                    else if (part.line > line)
                        break;
                }

                return result;
            }

            internal Vector2 measureString(TextDrawer textDrawer, int line, int startIndex = 0, int length = short.MaxValue)
            {
                Vector2 result = Vector2.Zero;
                int total = 0;
                int endIndex = startIndex + length;

                foreach (var part in parts)
                {
                    if (part.line == line)
                    {
                        string str = part.text;
                        int start = total;
                        int end = total + str.Length;

                        // startIndex より前
                        if (end <= startIndex)
                        {
                            str = "";
                        }
                        // startIndex + length より後
                        else if (endIndex <= start)
                        {
                            str = "";
                        }
                        else
                        {
                            // 途中で切れている？
                            int index = startIndex - start;
                            if (start <= startIndex && startIndex < end)
                                str = str.Substring(index, Math.Min(str.Length - index, length));
                            else if (start < endIndex && endIndex <= end)
                                str = str.Substring(Math.Max(0, index), endIndex - start - Math.Max(0, index));
                            //else // 単に切らなくていいケース
                        }

                        var defaultSize = textDrawer.MeasureString(str);
                        var size = defaultSize * part.size / 100;
                        result.X += size.X;
                        if (result.Y < defaultSize.Y)
                            result.Y = defaultSize.Y;

                        total = end;
                    }
                    else if (part.line > line)
                        break;
                }

                return result;
            }

            internal void splitByIndex(int line, int startIndex)
            {
                bool found = false;
                int total = 0;
                int insertIndex = 1;

                foreach (var part in parts.ToArray())
                {
                    if (found)
                    {
                        part.line++;
                    }
                    else if (part.line == line)
                    {
                        int start = total;
                        int end = total + part.text.Length;

                        if (start < startIndex && startIndex < end)
                        {
                            var splitted = new MessageParts();
                            splitted.CopyFrom(part);

                            int count = startIndex - start;
                            part.text = part.text.Substring(0, count);
                            splitted.text = splitted.text.Substring(count);
                            part.endTime -= splitted.text.Length * part.speed;
                            splitted.line++;

                            parts.Insert(insertIndex, splitted);
                            found = true;
                        }
                        else if(start == startIndex)
                        {
                            part.line++;
                            found = true;
                        }

                        total = end;
                    }

                    insertIndex++;
                }
            }
            
            public void drawStringWithCommands(TextDrawer textDrawer, Vector2 pos, int drawLine = -1, float scale = 1, float alpha = 1)
            {
                var origX = pos.X;
                int line = 0;
                if (drawLine >= 0)
                    line = drawLine;

                foreach (var part in parts)
                {
                    if (drawLine >= 0 && drawLine != part.line)
                        continue;

                    if (time < part.startTime)
                        break;

                    var text = part.text;
                    if (time < part.endTime)
                    {
                        // 途中に引っかかるタイミングだったら
                        var index = (int)((time - part.startTime) / part.speed);
                        if (text.Length > index)
                            text = text.Substring(0, index);
                    }

                    var defaultSize = textDrawer.MeasureString(text) * scale;
                    var size = defaultSize * part.size / 100;
                    Vector2 diff = Vector2.Zero;
                    diff.Y = (defaultSize.Y - size.Y) / 2;

                    if (line != part.line)
                    {
                        pos.X = origX;
                        pos.Y += defaultSize.Y;
                        line = part.line;
                    }

                    var color = part.color;
                    if (alpha < 1)
                    {
                        color.A = (byte)(alpha * 255);
                        color.G = (byte)(alpha * color.G);
                        color.B = (byte)(alpha * color.B);
                        color.R = (byte)(alpha * color.R);
                    }
                    
                    textDrawer.DrawString(text, pos + diff, color, (float)part.size / 100 * scale, part.bold, part.italic);

                    // 下線
                    if (part.underline)
                    {
                        var rectPos = pos + diff;
                        rectPos.Y += size.Y - 4;
                        Graphics.DrawFillRect((int)rectPos.X, (int)rectPos.Y, (int)size.X, 2, color.R, color.G, color.B, color.A);
                    }

                    // ルビ
                    if (!string.IsNullOrEmpty(part.ruby))
                    {
                        var scl = 0.5f * scale;

                        var rubySize = textDrawer.MeasureString(part.ruby) * scl;
                        if (rubySize.X > size.X)
                        {
                            // ルビの方がでかいときは縮小する
                            scl = size.X / rubySize.X * scl;
                            rubySize = textDrawer.MeasureString(part.ruby) * scl;
                        }

                        diff.X -= (rubySize.X - size.X) / 2;
                        diff.Y -= rubySize.Y / 2;
                        textDrawer.DrawString(part.ruby, pos + diff, color, scl);
                    }

                    pos.X += size.X;

                    if (part.bold || part.italic)
                        pos.X += 2;
                }
            }
        }
        private MessageEntry curMessage;
        private int messageCount = 0;
        Queue<MessageEntry> messageQueue = new Queue<MessageEntry>();
        private Func<bool> messageSequencer;
        private WindowDrawer msgWindow;
        private WindowDrawer window;
        private TextDrawer textDrawer;
        public MapScene parent;
        private bool keepWindow;
        public bool isKeepWindow() { return keepWindow; }

        private float count;
        private Common.Resource.Character pageRes;
        private int pageImgId;
        private Color msgTextColor;
        private Color textColor;

        public static string[] SplitStringInnerWidth(string text, int width, TextDrawer textDrawer)
        {
            var splitters = new string[] { "\r\n", "\n" };
            var strs = text.Split(splitters, StringSplitOptions.None);
            var strList = new List<string>();

            foreach (var line in strs)
            {
                var size = textDrawer.MeasureString(line);
                if (size.X > width)
                {
                    // 幅に収まらない行は分割する
                    string buf = line;
                    int len = 1;
                    string oldSubStr = "";
                    while (true)
                    {
                        if (buf.Length <= len)
                        {
                            strList.Add(buf);
                            break;
                        }

                        // 現在の文字が英単語の一部だった場合は、全て含まれるまでlenを進める
                        int wrappedLen = len;
                        while (isWord(buf[wrappedLen - 1]) && buf.Length > wrappedLen)
                        {
                            wrappedLen++;
                        }

                        // 次の文字が 、とか 。 だった場合は次の文字を含める
                        if (buf.Length > wrappedLen && isNotGoodForPrefix(buf[wrappedLen]))
                            wrappedLen++;

                        // ワードラップ分だけでwidthを超える場合は仕方ないので途中で切る
                        len--;
                        var wrapped = buf.Substring(len, wrappedLen - len);
                        size = textDrawer.MeasureString(wrapped);
                        len++;
                        if (size.X > width)
                            wrappedLen = len;

                        // width を超えていた場合、len までを一区切りにしてリストに入れ、buf を縮める
                        var substr = buf.Substring(0, wrappedLen);
                        size = textDrawer.MeasureString(substr);
                        if (size.X > width)
                        {
                            strList.Add(oldSubStr);
                            len--;
                            buf = buf.Substring(len, buf.Length - len);
                            len = 1;
                        }
                        else
                        {
                            oldSubStr = substr;
                            len++;
                        }
                    }
                }
                else
                {
                    // それ以外はそのままリストに入れる
                    strList.Add(line);
                }
            }

            return strList.ToArray();
        }

        private static bool isNotGoodForPrefix(char p)
        {
            return p == '、' || p == '。';
        }

        private static bool isWord(char p)
        {
            return (p >= 'a' && p <= 'z') || (p >= 'A' && p <= 'Z') || (p >= 'A' && p <= '?') || (p >= '0' && p <= '9') || p == ',' || p == '.' || p == '\'' ||
                p == '"' || p == '(' || p == '<' || p == ')' || p == '>' || p == '[' || p == '{' || p == ']' || p == '}';
        }

        internal void Initialize(Common.Resource.Window winRes, int winImgId,
            Common.Resource.Window winRes2, int winImgId2,
            Common.Resource.Character pageRes, int pageImgId)
        {
            // ウィンドウの読み込み
            msgWindow = new WindowDrawer(winRes2, winImgId2);
            if (winRes2 != null)
            {
                var avg = getAverageColor(3, winRes2.path);
                msgTextColor = avg > 127 ? Color.Black : Color.White;
            }
            else
            {
                msgTextColor = Color.White;
            }
            window = new WindowDrawer(winRes, winImgId);
            if (winRes != null)
            {
                var avg = getAverageColor(3, winRes.path);
                textColor = avg > 127 ? Color.Black : Color.White;
            }
            else
            {
                textColor = Color.White;
            }
            textDrawer = new TextDrawer(1);
            if (winRes != null)
            {
                textOffset.X = winRes.left + TEXT_OFFSET;
                textOffset.Y = winRes.top + TEXT_OFFSET;
            }
            windowSize.X = WINDOW_WIDTH;
            windowSize.Y = WINDOW_HEIGHT;

            // ページ送りキャラ作成
            this.pageImgId = pageImgId;
            this.pageRes = pageRes;
        }

        private int getAverageColor(int resolution, string path)
        {
#if WINDOWS
            var bmp = new System.Drawing.Bitmap(Yukar.Common.FileUtil.getMemoryStream(path));
            int avg = 0;
            for (int i = 0; i < resolution; i++)
            {
                for (int j = 0; j < resolution; j++)
                {
                    var c = bmp.GetPixel(
                        bmp.Width / (resolution + 2) * (i + 1),
                        bmp.Height / (resolution + 2) * (j + 1)
                        );
                    avg += c.R + c.G + c.B;
                }
            }
            bmp.Dispose();
            return avg / resolution / resolution / 3;
#else
            var tex = SharpKmyGfx.Texture.load(path);
            float avg = 0;
            for (int i = 0; i < resolution; i++)
            {
                for (int j = 0; j < resolution; j++)
                {
                    var c = tex.obj.GetPixel(
                        tex.obj.width / (resolution + 2) * (i + 1),
                        tex.obj.height / (resolution + 2) * (j + 1)
                        );
                    avg += c.r + c.g + c.b;
                }
            }
            tex.Release();
            return (int)(avg * 255) / resolution / resolution / 3;
#endif
        }

        internal void Update()
        {
            if (isVisible())
            {
                count += GameMain.getElapsedTime();
                window.Update();
                textDrawer.Update();
            }

            // メッセージを処理
            if (messageSequencer != null)
            {
                if (!messageSequencer())
                {
                    messageSequencer = null;
                }
            }
            else if (messageQueue.Count > 0)
            {
                ProcMessage();
                Update();   // MessageSequencer を早速処理する
            }
        }

        public bool isVisible()
        {
            return windowState != WindowState.HIDE_WINDOW;
        }

        private void ProcMessage()
        {
            curMessage = messageQueue.First();
            int y = 0;
            switch (curMessage.winVAligh)
            {
                case 0:
                    y = WINDOW_OFFSET_Y + WINDOW_HEIGHT / 2;
                    break;
                case 1:
                    y = Graphics.ViewportHeight / 2;
                    break;
                case 2:
                    y = Graphics.ViewportHeight - WINDOW_OFFSET_Y - WINDOW_HEIGHT / 2;
                    break;
            }

            SetWindowPos(Graphics.ViewportWidth / 2, y);

            // 閉じるまでウェイト
            float frame = 0;
            int phase = windowState == WindowState.HIDE_WINDOW ? 0 : 1; // ウィンドウがまだ出てる場合はすぐテキスト表示
            messageSequencer = () =>
            {
                switch (phase)
                {
                    case 0: // ウィンドウを出す
                        if (frame >= MapScene.WINDOW_SHOW_FRAME)
                        {
                            phase++;
                            frame = 0;
                            SetWindowSize(WINDOW_WIDTH, WINDOW_HEIGHT);
                        }
                        else
                        {
                            if (frame == 0)
                            {
                                parent.LockControl();   // 操作を禁止する
                                frame += 0.001f;
                            }
                            SetWindowVisible(WindowState.SHOW_WINDOW);
                            float delta = (float)frame / MapScene.WINDOW_SHOW_FRAME;
                            delta = 1 - (1 - delta) * (1 - delta);
                            SetWindowSize((int)(WINDOW_WIDTH * delta), (int)(WINDOW_HEIGHT * delta));
                        }
                        break;
                    case 1: // テキストを出す
                        //var spd = getMessageSpeed();
                        //if (Input.KeyTest(Input.StateType.DIRECT, KeyStates.DASH))  // DASHキーを押している時は瞬時に表示する
                        //    spd = 10000;

                        var last = curMessage.parts.LastOrDefault();

                        // 入力待ち
                        if (curMessage.isWait())
                        {
                            var curPart = curMessage.getCurrentPart();
                            frame = curPart.startTime + 30;
                            curMessage.time = frame;
                            if (Input.KeyTest(StateType.TRIGGER, KeyStates.DECIDE))
                            {
                                frame = curPart.endTime;
                                curPart.wait = false;
                            }
                        }

                        // 表示完了
                        if (curMessage.isEmpty() || last != null && frame >= last.endTime)
                        {
                            phase++;
                            frame = 0;
                            SetWindowVisible(WindowState.SHOW_PAGEMARK);
                            curMessage.time = 100000;
                            SetMessageText(curMessage);
                        }

                        // 表示途中
                        else
                        {
                            SetWindowSize(WINDOW_WIDTH, WINDOW_HEIGHT);
                            curMessage.time = frame;
                            SetMessageText(curMessage);
                        }
                        break;
                    case 2: // 入力待ち
                        if ((curMessage.parts.Count > 0 && curMessage.parts.Last().noWait) ||
                            Input.KeyTest(StateType.TRIGGER, KeyStates.DECIDE))
                        {
                            phase++;
                            frame = 0;
                            SetWindowVisible(WindowState.HIDE_PAGEMARK);
                            messageQueue.Dequeue();
                            return true;
                        }
                        break;
                    case 3: // 閉じる
                        if (keepWindow && frame == 0)
                        {
                            // 選択肢ウィンドウが出ていて、閉じている最中でない場合は閉じない
                            return false;
                        }
                        else if (messageQueue.Count > 0 &&
                            messageQueue.First().winVAligh == curMessage.winVAligh &&
                            messageQueue.First().winType == curMessage.winType)
                        {
                            // 次のメッセージがキューに溜まっていて、なおかつウィンドウ位置・タイプが同じな場合は閉じない
                            SetMessageText(null);
                            return false;
                        }
                        else if (frame >= MapScene.WINDOW_CLOSE_FRAME)
                        {
                            phase++;
                            frame = 0;
                            SetWindowVisible(WindowState.HIDE_WINDOW);
                            parent.UnlockControl();   // 操作禁止を解除する
                        }
                        else
                        {
                            SetMessageText(null);
                            float delta = 1 - frame / MapScene.WINDOW_CLOSE_FRAME;
                            delta = 1 - (1 - delta) * (1 - delta);
                            SetWindowSize((int)(WINDOW_WIDTH * delta), (int)(WINDOW_HEIGHT * delta));
                        }
                        break;
                    case 4: // おわり
                        return false;
                }
                frame += GameMain.getRelativeParam60FPS();
                return true;
            };
        }

        private float getMessageSpeed()
        {
            switch (parent.owner.data.system.messageSpeed)
            {
                case SystemData.MessageSpeed.IMMEDIATE:
                    return 10000;
                case SystemData.MessageSpeed.FAST:
                    return 2;
                case SystemData.MessageSpeed.NORMAL:
                    return 1;
                case SystemData.MessageSpeed.SLOW:
                    return 0.5f;
            }

            return 1;
        }

        internal void Draw()
        {
            if (isVisible())
            {
                var pos = windowPos - windowSize / 2;
                switch(curMessage.winType)
                {
                    case WindowType.NONE:
                        break;
                    case WindowType.DIALOGUE:
                        window.Draw(pos, windowSize);
                        break;
                    case WindowType.MESSAGE:
                        msgWindow.Draw(pos, windowSize);
                        break;
                }
                if(curMessageEntry != null)
                    curMessageEntry.drawStringWithCommands(textDrawer, pos + textOffset);

                if (windowState == WindowState.SHOW_PAGEMARK || (curMessageEntry != null && curMessageEntry.isWait()))
                {
                    pos.X = windowPos.X;
                    pos.Y = pos.Y + windowSize.Y - textOffset.Y;

                    int divW = Graphics.GetDivWidth(pageImgId);
                    int divH = Graphics.GetDivHeight(pageImgId);
                    Graphics.DrawChipImage(pageImgId,
                        (int)pos.X - divW / 2,
                        (int)pos.Y - divH / 2,
                        (int)(count * 1000 / pageRes.animationSpeed) % pageRes.animationFrame, 0);
                }
            }
        }

        internal void SetWindowPos(int x, int y)
        {
            windowPos.X = x;
            windowPos.Y = y;
        }

        private void SetMessageText(MessageEntry curMessage)
        {
            curMessageEntry = curMessage;
        }

        internal void SetWindowSize(int x, int y)
        {
            windowSize.X = x;
            windowSize.Y = y;
        }

        internal void SetWindowVisible(WindowState p)
        {
            windowState = p;
        }

        internal int PushMessage(string str, int winAlign, WindowType winType)
        {
            str = parent.owner.replaceForFormat(str);

            var entry = new MessageEntry(null);
            entry.id = messageCount;
            entry.text = str;
            entry.winVAligh = winAlign;
            entry.winType = winType;
            prepareEntry(entry);
            wordWrap(entry, WINDOW_WIDTH - (int)textOffset.X * 2);
            MessageEntry[] entries = entry.splitByLines(3);
            messageCount += entries.Length;

            foreach(var e in entries)
                messageQueue.Enqueue(e);

            return messageCount - 1;    // 最後のメッセージのIDを返す
        }

        private void wordWrap(MessageEntry entry, int width)
        {
            for (int i = 0; i < entry.lineCount; i++)
            {
                var size = entry.measureString(textDrawer, i);
                if (size.X > width)
                {
                    // 幅に収まらない行は分割する
                    int ptr = 1;
                    while (true)
                    {
                        // その行を最後までみたら脱出
                        string buf = entry.getLine(i);
                        if (buf.Length <= ptr)
                        {
                            break;
                        }

                        // 現在の文字が英単語の一部だった場合は、全て含まれるまでlenを進める
                        int wrappedLen = ptr;
                        while (isWord(buf[wrappedLen - 1]) && buf.Length > wrappedLen)
                        {
                            wrappedLen++;
                        }

                        // 次の文字が 、とか 。 だった場合は次の文字を含める
                        if (buf.Length > wrappedLen && isNotGoodForPrefix(buf[wrappedLen]))
                            wrappedLen++;

                        // ワードラップ分だけでwidthを超える場合は仕方ないので途中で切る
                        ptr--;
                        size = entry.measureString(textDrawer, i, ptr, wrappedLen - ptr);
                        ptr++;
                        if (size.X > width)
                            wrappedLen = ptr;

                        // width を超えていた場合、len までを一区切りにしてリストに入れ、buf を縮める
                        size = entry.measureString(textDrawer, i, 0, wrappedLen);
                        if (size.X > width)
                        {
                            ptr--;
                            entry.splitByIndex(i, ptr);
                            i++;
                            ptr = 1;
                        }
                        else
                        {
                            ptr++;
                        }
                    }
                }
            }
        }

        private void prepareEntry(MessageEntry entry)
        {
            var defaultColor = Color.White;
            switch (entry.winType)
            {
                case WindowType.NONE:
                    break;
                case WindowType.DIALOGUE:
                    defaultColor = textColor;
                    break;
                case WindowType.MESSAGE:
                    defaultColor = msgTextColor;
                    break;
            }
            entry.separateByCommands(1 / getMessageSpeed(), defaultColor);
        }

        internal bool IsQueuedMessage(int id)
        {
            try
            {
                messageQueue.First(tgt => tgt.id == id);
                return true;
            }
            catch (InvalidOperationException)
            {
                // キューに入ってない場合は↑の例外が出るのを利用してます
                return false;
            }
        }

        internal void SetKeepWindowFlag(bool p)
        {
            keepWindow = p;
        }

        internal void Close()
        {
            if (windowState == WindowState.HIDE_WINDOW || messageQueue.Count > 0 && messageQueue.First().winVAligh == curMessage.winVAligh)
                return;

            // 閉じるまでウェイト
            int frame = 0;
            int phase = 3;
            messageSequencer = () =>
            {
                switch (phase)
                {
                    case 3: // 閉じる
                        if (frame == MapScene.WINDOW_CLOSE_FRAME)
                        {
                            phase++;
                            frame = 0;
                            SetWindowVisible(WindowState.HIDE_WINDOW);
                            parent.UnlockControl();
                        }
                        else
                        {
                            SetMessageText(null);
                            float delta = 1 - (float)frame / MapScene.WINDOW_CLOSE_FRAME;
                            delta = 1 - (1 - delta) * (1 - delta);
                            SetWindowSize((int)(WINDOW_WIDTH * delta), (int)(WINDOW_HEIGHT * delta));
                        }
                        break;
                    case 4: // おわり
                        return false;
                }
                frame++;
                return true;
            };
        }
    }
}

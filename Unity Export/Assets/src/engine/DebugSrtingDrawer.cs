using System.Collections.Generic;
using System.Diagnostics;

namespace Yukar.Engine
{
    /// <summary>
    /// 画面上に文字を表示するクラス
    /// <para>本当はC++のポインタのようなもので実装したい</para>
    /// </summary>
    class DebugSrtingDrawer
    {
        private class DebugInfo
        {
            public DebugInfo(string variableName, string debugString)
            {
                this.variableName = variableName;
                this.debugString = debugString;
            }
            public string variableName;
            public string debugString;
        }
#if DEBUG
        static private DebugSrtingDrawer _instance = new DebugSrtingDrawer();
#endif // #if DEBUG
        private List<DebugInfo> debugInfos = new List<DebugInfo>();

        DebugSrtingDrawer()
        {
        }

        public static DebugSrtingDrawer GetInstance()
        {
#if DEBUG
            return _instance;
#else
            return null;
#endif //#if DEBUG
        }

        /// <summary>
        /// デバック変数に追加する
        /// <para>登録した変数名と同じ物がある場合の場合値を書き換える</para>
        /// </summary>
        /// <param name="variableName">登録したい変数名</param>
        /// <param name="debugValue">登録したい変数の値</param>
        [Conditional("DEBUG")]
        public void AddDebugInfo(string variableName, bool debugValue)
        {
            var debugString = variableName + " : " + debugValue.ToString();
            AddDebugInfo(variableName, debugString);
        }

        /// <summary>
        /// デバック変数に追加する
        /// <para>登録した変数名と同じ物がある場合の場合値を書き換える</para>
        /// </summary>
        /// <param name="variableName">登録したい変数名</param>
        /// <param name="debugValue">登録したい変数の値</param>
        [Conditional("DEBUG")]
        public void AddDebugInfo(string variableName, int debugValue)
        {
            var debugString = variableName + " : " + debugValue.ToString();
            AddDebugInfo(variableName, debugString);
        }

        /// <summary>
        /// デバック変数に追加する
        /// <para>登録した変数名と同じ物がある場合の場合値を書き換える</para>
        /// </summary>
        /// <param name="variableName">登録したい変数名</param>
        /// <param name="debugValue">登録したい変数の値</param>
        [Conditional("DEBUG")]
        public void AddDebugInfo(string variableName, float debugValue)
        {
            var debugString = variableName + " : " + debugValue.ToString();
            AddDebugInfo(variableName, debugString);
        }

        /// <summary>
        /// デバック変数に追加する
        /// <para>登録した変数名と同じ物がある場合の場合値を書き換える</para>
        /// </summary>
        /// <param name="variableName">登録したい変数名</param>
        /// <param name="debugValue">登録したい変数の値</param>
        [Conditional("DEBUG")]
        public void AddDebugInfo(string variableName, double debugValue)
        {
            var debugString = variableName + " : " + debugValue.ToString();
            AddDebugInfo(variableName, debugString);
        }

        /// <summary>
        /// デバック変数に追加する
        /// <para>登録した変数名と同じ物がある場合の場合値を書き換える</para>
        /// </summary>
        /// <param name="variableName">登録したい変数名</param>
        /// <param name="debugValue">登録したい変数の値</param>
        [Conditional("DEBUG")]
        public void AddDebugInfo(string variableName, Microsoft.Xna.Framework.Vector2 debugValue)
        {
            var debugString = variableName + " :  x = " + debugValue.X.ToString() + " y = " + debugValue.Y.ToString();
            AddDebugInfo(variableName, debugString);
        }

        /// <summary>
        /// デバック変数に追加する
        /// <para>登録した変数名と同じ物がある場合の場合値を書き換える</para>
        /// </summary>
        /// <param name="variableName">登録したい変数名</param>
        /// <param name="debugValue">登録したい変数の値</param>
        [Conditional("DEBUG")]
        public void AddDebugInfo(string variableName, Microsoft.Xna.Framework.Vector3 debugValue)
        {
            var debugString = variableName + " :  x = " + debugValue.X.ToString() + " y = " + debugValue.Y.ToString() + " z = " + debugValue.Z.ToString();
            AddDebugInfo(variableName, debugString);
        }

#if UNITY_EDITOR
        /// <summary>
        /// デバック変数に追加する
        /// <para>登録した変数名と同じ物がある場合の場合値を書き換える</para>
        /// </summary>
        /// <param name="variableName">登録したい変数名</param>
        /// <param name="debugValue">登録したい変数の値</param>
        [Conditional("DEBUG")]
        public void AddDebugInfo(string variableName, UnityEngine.Vector2 debugValue)
        {
            var debugString = variableName + " :  x = " + debugValue.x.ToString() + " y = " + debugValue.y.ToString();
            AddDebugInfo(variableName, debugString);
        }

        /// <summary>
        /// デバック変数に追加する
        /// <para>登録した変数名と同じ物がある場合の場合値を書き換える</para>
        /// </summary>
        /// <param name="variableName">登録したい変数名</param>
        /// <param name="debugValue">登録したい変数の値</param>
        [Conditional("DEBUG")]
        public void AddDebugInfo(string variableName, UnityEngine.Vector3 debugValue)
        {
            var debugString = variableName + " :  x = " + debugValue.x.ToString() + " y = " + debugValue.y.ToString() + " z = " + debugValue.z.ToString();
            AddDebugInfo(variableName, debugString);
        }
#endif // #if UNITY_EDITOR

        /// <summary>
        /// デバック変数に追加する
        /// <para>登録した変数名と同じ物がある場合の場合値を書き換える</para>
        /// </summary>
        /// <param name="variableName">登録したい変数名</param>
        /// <param name="debugValue">登録したい変数の値</param>
        [Conditional("DEBUG")]
        public void AddDebugInfo(string variableName, string debugString)
        {
            foreach (var debugInfo in debugInfos)
            {
                if (debugInfo.variableName == variableName)
                {
                    if (debugInfo.debugString == debugString)
                    {
                        return;
                    }
                    debugInfo.debugString = debugString;
                    return;
                }
            }
            debugInfos.Add(new DebugInfo(variableName, debugString));
        }

        /// <summary>
        /// デバック変数を表示する
        /// <para>1フレームに1度だけコールすれば良い</para>
        /// <para>スクリーン上に映す</para>
        /// </summary>
        [Conditional("DEBUG")]
        public void Draw()
        {
            var drawPostion = new Microsoft.Xna.Framework.Vector2(0.0f, 0.0f);
            foreach (var debugInfo in debugInfos)
            {
                Graphics.DrawString(1, debugInfo.debugString, drawPostion, Microsoft.Xna.Framework.Color.White, 0.6f);
                drawPostion.Y += 16.0f;
            }
        }


    }
}
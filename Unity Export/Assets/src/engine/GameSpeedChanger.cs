using System.Diagnostics;

namespace Yukar.Engine
{
    /// <summary>
    /// ゲームスピードを変更する
    /// </summary>
    class GameSpeedChanger
    {
#if DEBUG
        private static GameSpeedChanger _instance = new GameSpeedChanger();
#endif // DEBUG
        private float gameSpeed;

        GameSpeedChanger()
        {
            gameSpeed = 1.0f;
        }

        public static GameSpeedChanger GetInstance()
        {
#if DEBUG
            return _instance;
#else
            return null;
#endif // DEBUG

        }

        [Conditional("DEBUG")]
        public void ChangeGameSpeed(float gameSpeed)
        {
            this.gameSpeed = gameSpeed;
        }

        [Conditional("DEBUG")]
        public void Update()
        {
            var elapasedTime = GameMain.getElapsedTime() * gameSpeed;
            GameMain.setElapsedTime(elapasedTime);
        }

        /// <summary>
        /// Vsync無効化
        /// </summary>
        [Conditional("DEBUG")]
        public static void DisableVsync()
        {
#if WINDOWS
            SharpKmy.Entry.enableVSync(false);
#elif UNITY_EDITOR
            UnityEngine.QualitySettings.vSyncCount = 0;
            UnityEngine.Application.targetFrameRate = -1;
#endif // #if WINDOWS
        }
    }
}
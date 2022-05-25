using System;
using Yukar.Engine;

namespace SharpKmyBase
{
    public class Task
    {

        protected static void shutdown()
        {
            throw new NotImplementedException();
        }

        public virtual void update(float elapsed)
        {

        }

        public virtual void initialize()
        {

        }

        public virtual void finalize()
        {

        }

        //メニュー＞ゲームを終了する
        protected static void removeTask(GameMain gameMain)
        {
            // Unityではタイトルに戻るようにする
            gameMain.ChangeScene(GameMain.Scenes.TITLE);
        }
    }
}
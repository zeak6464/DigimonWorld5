using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Yukar.Engine;

namespace FrameCount
{
    public class FrameCountClass : BakinObject
    {

        // fps計測用メンバ変数
        static int oldSec = 0;
        static int fps = 30;
        static int frameCount;

        public int FPSCount()
        {

            // fps計測
            frameCount++; //フレームカウンターを加算
            if (oldSec != DateTime.Now.Second) //１秒たつとTrue
            {
                oldSec = DateTime.Now.Second; //タイマーを現在秒で更新
                fps = frameCount; //１秒間にカウントしたフレーム数を代入
                frameCount = 0; //フレームカウンターを０に初期化
            }
            
            return frameCount;
        }

        public int setOldSec()
        {
            //タイマー初期化
            oldSec = DateTime.Now.Second;
            return oldSec;
        }

        public int GetFPS()
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.


            
            if (fps < 30) return 30; //FPSの下限は30
            else return fps; // FPSを取得
            
        }
    }
}

using System;
using Yukar.Engine;

namespace IsVisible
{
    public class IsVisibleClass : BakinObject
    {
        public override void Start()
        {
            // キャラクターが生成される時に、このメソッドがコールされます。
            // This method is called when the character is created.
        }

        public override void Update()
        {
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


        public float IsVisible(int x, int y)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            float result = 0;            

            if (x>= 0 && x <= Graphics.ScreenWidth && y >= 0 && y <= Graphics.ScreenHeight) result = 1;
            else result = 0;
            
            return result;
        }
    }
}

using System;
using Yukar.Common;
using Yukar.Engine;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;


namespace UseAreaLimitedCamera
{
    public class UseAreaLimitedCameraClass : BakinObject
    {

        static float memoryLimitPosMinX = 0f; //カメラ追従限界座標
        static float memoryLimitPosMaxX = 16384f; //カメラ追従限界座標
        static float memoryLimitPosMinZ = 0f; //カメラ追従限界座標
        static float memoryLimitPosMaxZ = 16384f; //カメラ追従限界座標

/*
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
*/
        [BakinFunction(Description="カメラ移動エリア左端Ｘ座標を設定")]
        public void SetCameraAreaLimit_Min_X(float limitPosMinX)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            memoryLimitPosMinX = limitPosMinX;
            
        }

        [BakinFunction(Description="カメラ移動エリア右端Ｘ座標を設定")]
        public void SetCameraAreaLimit_Max_X(float limitPosMaxX)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            memoryLimitPosMaxX = limitPosMaxX;
            
        }

        [BakinFunction(Description="カメラ移動エリア上端Ｚ座標を設定")]
        public void SetCameraAreaLimit_Min_Z(float limitPosMinZ)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            memoryLimitPosMinZ = limitPosMinZ;
            
        }

        [BakinFunction(Description="カメラ移動エリア下端Ｚ座標を設定")]
        public void SetCameraAreaLimit_Max_Z(float limitPosMaxZ)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            memoryLimitPosMaxZ = limitPosMaxZ;
            
        }

        [BakinFunction(Description="カメラ移動エリア左端Ｘ座標を出力")]
        public float GetCameraAreaLimit_Min_X()
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            return memoryLimitPosMinX;
            
        }

        [BakinFunction(Description="カメラ移動エリア右端Ｘ座標を出力")]
        public float GetCameraAreaLimit_Max_X()
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            return memoryLimitPosMaxX;
            
        }

        [BakinFunction(Description="カメラ移動エリア上端Ｚ座標を出力")]
        public float GetCameraAreaLimit_Min_Z()
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            return memoryLimitPosMinZ;
            
        }

        [BakinFunction(Description="カメラ移動エリア下端Ｚ座標を出力")]
        public float GetCameraAreaLimit_Max_Z()
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            return memoryLimitPosMaxZ;
            
        }
        
        [BakinFunction(Description="カメラの移動を開始する。")]
        public void CameraMove(MapScene mapScene, Vector3 playerPos)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.

            mapScene.SetLookAtTarget(null, Util.ToKmyVector(playerPos) ); //OK
            // mapScene.SetLookAtTarget(mapScene.hero, Util.ToKmyVector(new Vector3(0, 0, 0) ) ); //カクツキありのため使用しない
        }

        [BakinFunction(Description="カメラのＸ方向移動を停止する。")]
        public void StopCamera_X(MapScene mapScene, Vector3 playerPos, float limitPos)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            // var pos = new Vector3((float)GameMain.instance.data.system.GetVariable("X"), (float)GameMain.instance.data.system.GetVariable("Y"), (float)GameMain.instance.data.system.GetVariable("Z")); //OK
            // var ofsetPos = new Vector3(0, 0, 0);
            // var playerPos = mapScene.hero.getPosition();
            // CameraStopClass Func = new CameraStopClass();

            
            // var dist = mapScene.dist;
            // var evtChr = mapScene.mapCharList.FirstOrDefault(x => x.rom?.name == "兵士");
            // mapScene.dist = 25.0f;
            // if (evtChr != null) mapScene.SetLookAtTarget(evtChr, Util.ToKmyVector(pos)); //OK
            // mapScene.SetLookAtTarget(mapScene.hero, Util.ToKmyVector(ofsetPos) ); //OK
            // mapScene.SetLookAtTarget(evtChr, Util.ToKmyVector(pos)); //OK
            // if (sw == true) 
            // {
            //     // var limitPos = OutputPlayerPosition().X;
                mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(limitPos, playerPos.Y, playerPos.Z) ) );
            // } else {
            //     mapScene.SetLookAtTarget(null, Util.ToKmyVector(playerPos) ); //OK
            //     // mapScene.SetLookAtTarget(mapScene.hero, Util.ToKmyVector(ofsetPos) ); //カクつきありのため不採用
            //     // InputPlayerPosition(playerPos);
            // }
        }

        [BakinFunction(Description="カメラのＺ方向移動を停止する。")]
        public void StopCamera_Z(MapScene mapScene, Vector3 playerPos, float limitPos)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            

            // if (sw == true) 
            // {
                // var limitPos = OutputPlayerPosition().Z;
                mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(playerPos.X, playerPos.Y, limitPos) ) );
            // } else {
            //     mapScene.SetLookAtTarget(null, Util.ToKmyVector(playerPos) ); //OK
            //     // mapScene.SetLookAtTarget(mapScene.hero, Util.ToKmyVector(ofsetPos) ); //カクつきありのため不採用
            //     // InputPlayerPosition(playerPos);
            // }
        }


        [BakinFunction(Description="カメラのＸＺ方向移動を停止する。")]
        public void StopCamera_XZ(MapScene mapScene, Vector3 playerPos, float limitPosX, float limitPosZ)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            

            // if (sw == true) 
            // {
                // var limitPos = OutputPlayerPosition();
                mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(limitPosX, playerPos.Y, limitPosZ) ) );
            // } else {
            //     mapScene.SetLookAtTarget(null, Util.ToKmyVector(playerPos) ); //OK
            //     // mapScene.SetLookAtTarget(mapScene.hero, Util.ToKmyVector(ofsetPos) ); //カクつきありのため不採用
            //     // InputPlayerPosition(playerPos);
            // }
        }
    }
}

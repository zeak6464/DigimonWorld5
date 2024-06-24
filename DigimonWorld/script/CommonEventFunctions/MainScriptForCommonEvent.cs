//Version 20230930

//外部ファイルを読み込み
//注意　ExtensionBをインポートすること！
// @@include ExtensionB_GetScreenResolution.cs
// @@include ExtensionB_Raycast.cs
// @@include ExtensionB_ConfigGetter.cs
// @@include ExtensionB_GetEventPosition.cs
// @@include ExtensionB_UseAreaLimitedCamera.cs

using System;
using Yukar.Common;
using Yukar.Engine;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;


//外部ファイルの名前空間を使用
using GetScreenResolution;
using Raycast;
using ConfigGetter;
using GetEventPosition;
using UseAreaLimitedCamera;

namespace Bakin
{
    public class MainScriptForCommonEvent : BakinObject
    {

        
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
        [BakinFunction(Description="Please select a command. \n　We plan to add more command types from time to time. \n　Ver.20231001\n　This program was written by SANA translated by MelonToucan")] public void ___Command_List___(){}



        [BakinFunction(Description="Below is the command to get the screen resolution.")] public void ___Screen_Resolution_Category___(){}

        [BakinFunction(Description="Gets the width of the screen in pixels.")]
        public float GetScreenWidth()
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            GetScreenResolutionClass Func = new GetScreenResolutionClass(); //インスタンス生成

            return Func.GetScreenWidth();
        }


        [BakinFunction(Description="Gets the vertical width of the screen in pixels.")]
        public float GetScreenHeight()
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            GetScreenResolutionClass Func = new GetScreenResolutionClass(); //インスタンス生成

            return Func.GetScreenHeight();
        }

        [BakinFunction(Description="Below are some special raycast commands.")] public void ___Raycast_Category___(){}
        

        
        [BakinFunction(Description="Fires a raycast in front of the player and gets the name of the event that the raycast hit. \n　Please set the raycast distance as an argument. \n　If the argument is a negative value, a raycast will be fired towards the back.")]
        public string Raycast_PlayerSight_GetEventName(float maxDistance)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.

            var hero = mapScene.hero;
            var from = mapScene.hero.getPosition();


            from.Y += 0.5f; //デフォルトでは足元からレイキャストが発射されているため高さを上げる
            
            //デバッグ用　キャストの座標指定
            // var evtChr = mapScene.mapCharList.FirstOrDefault(x => x.rom?.name == "兵士");
            // var to = evtChr.getPosition();
            // to.Y += 0.5f;

            
            var rot = mapScene.hero.getDirectionRadian();
            // var rot = mapScene.hero.getRotation().Y * Math.PI / 180; //プレイヤーの向き角度
            var to = new Vector3( (float)Math.Sin(rot), 0, (float)Math.Cos(rot) ); //プレイヤーの向きベクトル
            to = Vector3.Add(from, to);//プレイヤーの現在座標にプレイヤーの向きベクトルを加算合成

            RaycastClass Func = new RaycastClass(); //インスタンス生成　イベント名取得用
            RaycastClass Func2 = new RaycastClass(); //インスタンス生成　イベント距離取得用
            RaycastClass Func3 = new RaycastClass(); //インスタンス生成　オブジェクト距離取得用

            if (Func2.Raycast_GetALLObjectDistance(hero, from, to, maxDistance) == Func3.Raycast_GetEventDistance(hero, from, to, maxDistance) ) {
                return Func.Raycast_GetEventName(hero, from, to, maxDistance); //キャストイベントとの間に障害物がないときのみ名前を取得
            } else {
                return "NONE";
            }

            
        }

        [BakinFunction(Description="Fires a raycast from player to detect distance from Object,Terrain, or Event. \n　Please set the raycast distance as an argument. \n　If the argument is a negative value, a raycast will be fired towards the back.")]
        public float Raycast_PlayerSight_GetALLObjectDistance(float maxDistance)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.

            var hero = mapScene.hero;
            var from = mapScene.hero.getPosition();


            from.Y += 0.5f; //デフォルトでは足元からレイキャストが発射されているため高さを上げる
            
            //デバッグ用　キャストの座標指定
            // var evtChr = mapScene.mapCharList.FirstOrDefault(x => x.rom?.name == "兵士");
            // var to = evtChr.getPosition();
            // to.Y += 0.5f;
            var rot = mapScene.hero.getDirectionRadian();
            // var rot = mapScene.hero.getRotation().Y * Math.PI / 180; //プレイヤーの向き角度
            var to = new Vector3( (float)Math.Sin(rot), 0, (float)Math.Cos(rot) ); //プレイヤーの向きベクトル
            to = Vector3.Add(from, to);//プレイヤーの現在座標にプレイヤーの向きベクトルを加算合成

            RaycastClass Func = new RaycastClass(); //インスタンス生成

            return Func.Raycast_GetALLObjectDistance(hero, from, to, maxDistance);
            
        }

        [BakinFunction(Description="Fires a raycast in front of the player and obtains the distance to the point where the raycast hits. \n　Please set the raycast distance as an argument. \n　If the argument is a negative value, a raycast will be fired towards the back.")]
        public float Raycast_PlayerSight_GetEventDistance(float maxDistance)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.

            var hero = mapScene.hero;
            var from = mapScene.hero.getPosition();


            from.Y += 0.5f; //デフォルトでは足元からレイキャストが発射されているため高さを上げる
            
            //デバッグ用　キャストの座標指定
            // var evtChr = mapScene.mapCharList.FirstOrDefault(x => x.rom?.name == "兵士");
            // var to = evtChr.getPosition();
            // to.Y += 0.5f;
            var rot = mapScene.hero.getDirectionRadian();
            // var rot = mapScene.hero.getRotation().Y * Math.PI / 180; //プレイヤーの向き角度
            var to = new Vector3( (float)Math.Sin(rot), 0, (float)Math.Cos(rot) ); //プレイヤーの向きベクトル
            to = Vector3.Add(from, to);//プレイヤーの現在座標にプレイヤーの向きベクトルを加算合成

            RaycastClass Func = new RaycastClass(); //インスタンス生成

            return Func.Raycast_GetEventDistance(hero, from, to, maxDistance);
            
        }
        [BakinFunction(Description="Below is the command to get cast event information.")] public void ___CastEvent_Category___(){}

        [BakinFunction(Description="Gets the X coordinate of the cast event with the name set in the argument.")]
        public float GetCastEventPosition_X(string castName)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            var Chr = mapScene.mapCharList.FirstOrDefault(x => x.rom?.name == castName);
            GetEventPositionClass Func = new GetEventPositionClass();

            return Func.GetEventPosition(Chr).X;
        }


        [BakinFunction(Description="Gets the Y coordinate of the cast event with the name set in the argument.")]
        public float GetCastEventPosition_Y(string castName)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            var Chr = mapScene.mapCharList.FirstOrDefault(x => x.rom?.name == castName);
            GetEventPositionClass Func = new GetEventPositionClass();
            
            return Func.GetEventPosition(Chr).Y;
        }


        [BakinFunction(Description="Gets the Z coordinate of the cast event with the name set in the argument.")]
        public float GetCastEventPosition_Z(string castName)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            var Chr = mapScene.mapCharList.FirstOrDefault(x => x.rom?.name == castName);
            GetEventPositionClass Func = new GetEventPositionClass();
            
            return Func.GetEventPosition(Chr).Z;
        }



        [BakinFunction(Description="Below are commands related to the camera.")] public void ___Camera_Category___(){}


        [BakinFunction(Description="Switch to a special camera with a limited area that follows the player. \n　Enabled with argument of 1 or more. \n　Camera tracking area can be set with a separate command.")]
        public void UseAreaLimitedCamera(int sw)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            // bool sw = false;
            // var limitPos = 8.0f;
            // var ofsetPos = new Vector3(0, 0, 0);
            var playerPos = mapScene.hero.getPosition();
            UseAreaLimitedCameraClass Func = new UseAreaLimitedCameraClass();
            // int sw = 0;
            float limitPosMinX = Func.GetCameraAreaLimit_Min_X();
            float limitPosMaxX = Func.GetCameraAreaLimit_Max_X();
            float limitPosMinZ = Func.GetCameraAreaLimit_Min_Z();
            float limitPosMaxZ = Func.GetCameraAreaLimit_Max_Z();;

            if (sw > 0) {

                if (playerPos.X <= limitPosMinX && playerPos.Z <= limitPosMinZ) 
                {
                    // sw = 0;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "0");
                    Func.StopCamera_XZ(mapScene, playerPos, limitPosMinX, limitPosMinZ);

                } else if ( (limitPosMinX < playerPos.X && playerPos.X < limitPosMaxX ) && playerPos.Z <= limitPosMinZ) {

                    // sw = 1;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "1");
                    Func.StopCamera_Z(mapScene, playerPos, limitPosMinZ);

                } else if (playerPos.X >= limitPosMaxX && playerPos.Z <= limitPosMinZ) {
                    // sw = 2;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "2");
                    Func.StopCamera_XZ(mapScene, playerPos, limitPosMaxX, limitPosMinZ);

                } else if (playerPos.X >= limitPosMaxX && (limitPosMinZ < playerPos.Z && playerPos.Z < limitPosMaxZ) ) {

                    // sw = 3;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "3");
                    Func.StopCamera_X(mapScene, playerPos, limitPosMaxX);

                } else if (playerPos.X >= limitPosMaxX && playerPos.Z >= limitPosMaxZ) {

                    // sw = 4;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "4");
                    Func.StopCamera_XZ(mapScene, playerPos, limitPosMaxX, limitPosMaxZ);

                } else if ( (limitPosMinX < playerPos.X && playerPos.X < limitPosMaxX ) && playerPos.Z >= limitPosMaxZ) {
                    // sw = 5;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "5");
                    Func.StopCamera_Z(mapScene, playerPos, limitPosMaxZ);

                } else if (playerPos.X <= limitPosMinX && playerPos.Z >= limitPosMaxZ) {
                    // sw = 6;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "6");
                    Func.StopCamera_XZ(mapScene, playerPos, limitPosMinX, limitPosMaxZ);

                } else if (playerPos.X <= limitPosMinX && (limitPosMinZ < playerPos.Z && playerPos.Z < limitPosMaxZ)) {
                    // sw = 7;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "7");
                    Func.StopCamera_X(mapScene, playerPos, limitPosMinX);

                } else {
                    // sw = 8;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "8");
                    Func.CameraMove(mapScene, playerPos);
                }

            }

        }

        [BakinFunction(Description="Extension to the AreaLimitedCamera command. \n　You can set the X coordinate of the left edge of the camera tracking area as an argument. \n*Please set a value smaller than the argument of the SetCameraAreaLimit_Right command.")]
        public void SetCameraAreaLimit_Left(float limitPosMinX)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            UseAreaLimitedCameraClass Func = new UseAreaLimitedCameraClass();
            Func.SetCameraAreaLimit_Min_X(limitPosMinX);

        }


        [BakinFunction(Description="Extension to the AreaLimitedCamera command. \n　You can set the X coordinate of the right edge of the camera tracking area in the argument. \n*Please make the value larger than the argument of the SetCameraAreaLimit_Left command.")]
        public void SetCameraAreaLimit_Right(float limitPosMaxX)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            UseAreaLimitedCameraClass Func = new UseAreaLimitedCameraClass();
            Func.SetCameraAreaLimit_Max_X(limitPosMaxX);

        }


        [BakinFunction(Description="Extension to the AreaLimitedCamera command. \n　You can set the Z coordinate of the top of the camera tracking area as an argument. \n*Please make the value smaller than the argument of the SetCameraAreaLimit_Bottom command.")]
        public void SetCameraAreaLimit_Top(float limitPosMinZ)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            UseAreaLimitedCameraClass Func = new UseAreaLimitedCameraClass();
            Func.SetCameraAreaLimit_Min_Z(limitPosMinZ);

        }


        [BakinFunction(Description="Extension to the AreaLimitedCamera command. \n　You can set the Z coordinate of the bottom edge of the camera tracking area as an argument. \n*Please make the value larger than the argument of the SetCameraAreaLimit_Top command.")]
        public void SetCameraAreaLimit_Bottom(float limitPosMaxZ)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            UseAreaLimitedCameraClass Func = new UseAreaLimitedCameraClass();
            Func.SetCameraAreaLimit_Max_Z(limitPosMaxZ);

        }

/*
        [BakinFunction(Description="")]
        public string GetCastName(string guid)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            var evt = mapScene.mapCharList.FirstOrDefault(x => x.rom?.guId == new Guid(guid) );
            if (evt != null)
            {
                return evt.rom.name;
            } else {
                return "";
            }
        }
*/

    }
}

using System;
using Yukar.Common;
using Yukar.Engine;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;


namespace SpecialCamera
{
    public class SpecialCameraClass : BakinObject
    {

        static float memoryLimitPosMinX = 0f; //カメラ追従限界座標
        static float memoryLimitPosMaxX = 16384f; //カメラ追従限界座標
        static float memoryLimitPosMinZ = 0f; //カメラ追従限界座標
        static float memoryLimitPosMaxZ = 16384f; //カメラ追従限界座標
        static float memoryPosY = -1; //カメラ固定高さ 初期値は必ず-1としないとプラグインを入れるだけで勝手にカメラ固定となるバグが生じる


        static Vector3 focusPoint = new Vector3(0, 0, 0); //UseSmoothFollowCameraの注視点用メンバ変数

        static bool UseAreaLimitedCamera_SW = false;
        static bool UseSmoothFollowCameraXZ_SW = false;
        static bool UseSmoothFollowCameraY_SW = false;
        static bool CalculateSmoothFollowCamera_X_SW = true;
        static bool CalculateSmoothFollowCamera_Z_SW = true;


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





        //カメラ移動エリア左端Ｘ座標を設定
        public void SetCameraAreaLimit_Min_X(float limitPosMinX)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            memoryLimitPosMinX = limitPosMinX;
            
        }

        //カメラ移動エリア右端Ｘ座標を設定
        public void SetCameraAreaLimit_Max_X(float limitPosMaxX)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            memoryLimitPosMaxX = limitPosMaxX;
            
        }

        //カメラ移動エリア上端Ｚ座標を設定
        public void SetCameraAreaLimit_Min_Z(float limitPosMinZ)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            memoryLimitPosMinZ = limitPosMinZ;
            
        }

        //カメラ移動エリア下端Ｚ座標を設定
        public void SetCameraAreaLimit_Max_Z(float limitPosMaxZ)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            memoryLimitPosMaxZ = limitPosMaxZ;
            
        }

        //カメラ移動エリア左端Ｘ座標を出力
        public float GetCameraAreaLimit_Min_X()
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            return memoryLimitPosMinX;
            
        }

        //カメラ移動エリア右端Ｘ座標を出力
        public float GetCameraAreaLimit_Max_X()
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            return memoryLimitPosMaxX;
            
        }

        //カメラ移動エリア上端Ｚ座標を出力
        public float GetCameraAreaLimit_Min_Z()
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            return memoryLimitPosMinZ;
            
        }

        //カメラ移動エリア下端Ｚ座標を出力
        public float GetCameraAreaLimit_Max_Z()
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            return memoryLimitPosMaxZ;
            
        }
        
        //カメラの移動を開始する
        public void CameraMove(MapScene mapScene, Vector3 playerPos)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.

            // mapScene.SetLookAtTarget(mapScene.hero, Util.ToKmyVector(new Vector3(0, 0, 0) ) ); //カクツキありのため使用しない

            CalculateSmoothFollowCamera_X_SW = true; //SmoothFollowCameraの計算を実行
            CalculateSmoothFollowCamera_Z_SW = true; //SmoothFollowCameraの計算を実行

            if (memoryPosY >= 0)
            {

                if (UseSmoothFollowCameraXZ_SW)
                {

                    mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(focusPoint.X, memoryPosY, focusPoint.Z) ) );

                } else {

                    mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(playerPos.X, memoryPosY, playerPos.Z) ) );

                }

            } else if (UseSmoothFollowCameraY_SW && !UseSmoothFollowCameraXZ_SW) {

                mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(playerPos.X, focusPoint.Y, playerPos.Z) ) );

            } else if (UseSmoothFollowCameraXZ_SW && !UseSmoothFollowCameraY_SW) {

                mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(focusPoint.X, playerPos.Y, focusPoint.Z) ) ); //OK

            } else if (UseSmoothFollowCameraY_SW && UseSmoothFollowCameraXZ_SW) {

                mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(focusPoint.X, focusPoint.Y, focusPoint.Z) ) ); //OK

            } else {

                mapScene.SetLookAtTarget(null, Util.ToKmyVector(playerPos) ); //OK

            }

        }

        //カメラを固定する高さを設定
        public void SetCameraHight(float PosY)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            // UseSmoothFollowCameraY起動時は停止　これによりイベントパネルとして先に実行された方が優先される
            if(!UseSmoothFollowCameraY_SW) {

                if (PosY >= 0) memoryPosY = PosY;
                else memoryPosY = -1;

            }
            
        }

        //カメラのＸ方向移動を停止する
        public void StopCamera_X(MapScene mapScene, Vector3 playerPos, float fps, float limitPos)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            CalculateSmoothFollowCamera_X_SW = false; //SmoothFollowCameraの計算を停止
            CalculateSmoothFollowCamera_Z_SW = true; //SmoothFollowCameraの計算を停止

            if (memoryPosY >= 0)
            {

                if (UseSmoothFollowCameraXZ_SW)
                {
                    if(focusPoint.X < limitPos) focusPoint.X += Math.Abs(focusPoint.X - limitPos) / fps; //マップ左端でOK
                    
                    if(focusPoint.X > limitPos) focusPoint.X -= Math.Abs(focusPoint.X - limitPos) / fps; //マップ右端でOK 
                    
                    mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(focusPoint.X, memoryPosY, focusPoint.Z) ) );

                } else {

                    mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(limitPos, memoryPosY, playerPos.Z) ) );

                }

            } else if (UseSmoothFollowCameraY_SW && !UseSmoothFollowCameraXZ_SW) {

                mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(limitPos, focusPoint.Y, playerPos.Z) ) );

            } else if (UseSmoothFollowCameraXZ_SW && !UseSmoothFollowCameraY_SW) {

                if(focusPoint.X < limitPos) focusPoint.X += Math.Abs(focusPoint.X - limitPos) / fps; //マップ左端でOK
                
                if(focusPoint.X > limitPos) focusPoint.X -= Math.Abs(focusPoint.X - limitPos) / fps; //マップ右端でOK 
                
                mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(focusPoint.X, playerPos.Y, focusPoint.Z) ) );
                
            } else if (UseSmoothFollowCameraY_SW && UseSmoothFollowCameraXZ_SW) {

                if(focusPoint.X < limitPos) focusPoint.X += Math.Abs(focusPoint.X - limitPos) / fps; //マップ左端でOK
                
                if(focusPoint.X > limitPos) focusPoint.X -= Math.Abs(focusPoint.X - limitPos) / fps; //マップ右端でOK 
                
                mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(focusPoint.X, focusPoint.Y, focusPoint.Z) ) );

            } else {

                mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(limitPos, playerPos.Y, playerPos.Z) ) );

            }

        }

        //カメラのＺ方向移動を停止する
        public void StopCamera_Z(MapScene mapScene, Vector3 playerPos, float fps, float limitPos)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            

            CalculateSmoothFollowCamera_X_SW = true; //SmoothFollowCameraの計算を停止
            CalculateSmoothFollowCamera_Z_SW = false; //SmoothFollowCameraの計算を停止

            if (memoryPosY >= 0)
            {

                if (UseSmoothFollowCameraXZ_SW)
                {
                        
                    if(focusPoint.Z < limitPos) focusPoint.Z += Math.Abs(focusPoint.Z - limitPos) / fps; //マップ下端でOK
                    
                    if(focusPoint.Z > limitPos) focusPoint.Z -= Math.Abs(focusPoint.Z - limitPos) / fps; //マップ上端でOK
                    
                    mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(focusPoint.X, memoryPosY, focusPoint.Z) ) );

                } else {

                    mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(playerPos.X, memoryPosY, limitPos) ) );

                }

            } else if (UseSmoothFollowCameraY_SW && !UseSmoothFollowCameraXZ_SW) {

                mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(playerPos.X, focusPoint.Y, limitPos) ) );

            } else if (UseSmoothFollowCameraXZ_SW && !UseSmoothFollowCameraY_SW) {

                if(focusPoint.Z < limitPos) focusPoint.Z += Math.Abs(focusPoint.Z - limitPos) / fps; //マップ下端でOK
                
                if(focusPoint.Z > limitPos) focusPoint.Z -= Math.Abs(focusPoint.Z - limitPos) / fps; //マップ上端でOK
                
                mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(focusPoint.X, playerPos.Y, focusPoint.Z) ) );

            } else if (UseSmoothFollowCameraY_SW && UseSmoothFollowCameraXZ_SW) {

                if(focusPoint.Z < limitPos) focusPoint.Z += Math.Abs(focusPoint.Z - limitPos) / fps; //マップ下端でOK
                
                if(focusPoint.Z > limitPos) focusPoint.Z -= Math.Abs(focusPoint.Z - limitPos) / fps; //マップ上端でOK
                
                mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(focusPoint.X, focusPoint.Y, focusPoint.Z) ) );

            } else {

                mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(playerPos.X, playerPos.Y, limitPos) ) );

            }

        }


        //カメラのＸＺ方向移動を停止する
        public void StopCamera_XZ(MapScene mapScene, Vector3 playerPos, float fps, float limitPosX, float limitPosZ)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            

            CalculateSmoothFollowCamera_X_SW = false; //SmoothFollowCameraの計算を停止
            CalculateSmoothFollowCamera_Z_SW = false; //SmoothFollowCameraの計算を停止


            if (memoryPosY >= 0)
            {

                if (UseSmoothFollowCameraXZ_SW)
                {
                        
                    if(focusPoint.X < limitPosX) focusPoint.X += Math.Abs(focusPoint.X - limitPosX) / fps; //マップ左端でOK
                    
                    if(focusPoint.X > limitPosX) focusPoint.X -= Math.Abs(focusPoint.X - limitPosX) / fps; //マップ右端でOK 

                    if(focusPoint.Z < limitPosZ) focusPoint.Z += Math.Abs(focusPoint.Z - limitPosZ) / fps; //マップ下端でOK
                    
                    if(focusPoint.Z > limitPosZ) focusPoint.Z -= Math.Abs(focusPoint.Z - limitPosZ) / fps; //マップ上端でOK
                    
                    mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(focusPoint.X, memoryPosY, focusPoint.Z) ) );

                } else {

                    mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(limitPosX, memoryPosY, limitPosZ) ) );

                }

            } else if (UseSmoothFollowCameraY_SW && !UseSmoothFollowCameraXZ_SW) {

                mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(limitPosX, focusPoint.Y, limitPosZ) ) );

            } else if (UseSmoothFollowCameraXZ_SW && !UseSmoothFollowCameraY_SW) {

                if(focusPoint.X < limitPosX) focusPoint.X += Math.Abs(focusPoint.X - limitPosX) / fps; //マップ左端でOK
                
                if(focusPoint.X > limitPosX) focusPoint.X -= Math.Abs(focusPoint.X - limitPosX) / fps; //マップ右端でOK 

                if(focusPoint.Z < limitPosZ) focusPoint.Z += Math.Abs(focusPoint.Z - limitPosZ) / fps; //マップ下端でOK
                
                if(focusPoint.Z > limitPosZ) focusPoint.Z -= Math.Abs(focusPoint.Z - limitPosZ) / fps; //マップ上端でOK
                
                mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(focusPoint.X, playerPos.Y, focusPoint.Z) ) );

            } else if (UseSmoothFollowCameraY_SW && UseSmoothFollowCameraXZ_SW) {

                if(focusPoint.X < limitPosX) focusPoint.X += Math.Abs(focusPoint.X - limitPosX) / fps; //マップ左端でOK
                
                if(focusPoint.X > limitPosX) focusPoint.X -= Math.Abs(focusPoint.X - limitPosX) / fps; //マップ右端でOK 

                if(focusPoint.Z < limitPosZ) focusPoint.Z += Math.Abs(focusPoint.Z - limitPosZ) / fps; //マップ下端でOK
                
                if(focusPoint.Z > limitPosZ) focusPoint.Z -= Math.Abs(focusPoint.Z - limitPosZ) / fps; //マップ上端でOK

                mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(focusPoint.X, focusPoint.Y, focusPoint.Z) ) );

            } else {

                mapScene.SetLookAtTarget(null, Util.ToKmyVector(new Vector3(limitPosX, playerPos.Y, limitPosZ) ) );

            }

        }


        public void UseAreaLimitedCamera_Simple(MapScene mapScene, Vector3 playerPos, float fps, float limit)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            

            
            if (limit >= 0) {

                UseAreaLimitedCamera_SW = true;

                float limitPosMinX = limit;
                float limitPosMaxX = mapScene.map.Width - limit;
                float limitPosMinZ = limit;
                float limitPosMaxZ = mapScene.map.Height - limit;
                
                if (playerPos.X <= limitPosMinX && playerPos.Z <= limitPosMinZ) 
                {
                    // sw = 0;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "0");
                    StopCamera_XZ(mapScene, playerPos, fps, limitPosMinX, limitPosMinZ);

                } else if ( (limitPosMinX < playerPos.X && playerPos.X < limitPosMaxX ) && playerPos.Z <= limitPosMinZ) {

                    // sw = 1;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "1");
                    StopCamera_Z(mapScene, playerPos, fps, limitPosMinZ);

                } else if (playerPos.X >= limitPosMaxX && playerPos.Z <= limitPosMinZ) {
                    // sw = 2;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "2");
                    StopCamera_XZ(mapScene, playerPos, fps, limitPosMaxX, limitPosMinZ);

                } else if (playerPos.X >= limitPosMaxX && (limitPosMinZ < playerPos.Z && playerPos.Z < limitPosMaxZ) ) {

                    // sw = 3;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "3");
                    StopCamera_X(mapScene, playerPos, fps, limitPosMaxX);

                } else if (playerPos.X >= limitPosMaxX && playerPos.Z >= limitPosMaxZ) {

                    // sw = 4;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "4");
                    StopCamera_XZ(mapScene, playerPos, fps, limitPosMaxX, limitPosMaxZ);

                } else if ( (limitPosMinX < playerPos.X && playerPos.X < limitPosMaxX ) && playerPos.Z >= limitPosMaxZ) {
                    // sw = 5;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "5");
                    StopCamera_Z(mapScene, playerPos, fps, limitPosMaxZ);

                } else if (playerPos.X <= limitPosMinX && playerPos.Z >= limitPosMaxZ) {
                    // sw = 6;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "6");
                    StopCamera_XZ(mapScene, playerPos, fps, limitPosMinX, limitPosMaxZ);

                } else if (playerPos.X <= limitPosMinX && (limitPosMinZ < playerPos.Z && playerPos.Z < limitPosMaxZ)) {
                    // sw = 7;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "7");
                    StopCamera_X(mapScene, playerPos, fps, limitPosMinX);

                } else {
                    // sw = 8;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "8");
                    CameraMove(mapScene, playerPos); //OK


                    // CameraMove(mapScene, focusPoint); //テスト

                }

            } else {

                UseAreaLimitedCamera_SW = false;

            }

        }


        public void UseAreaLimitedCamera(MapScene mapScene, Vector3 playerPos, float fps, int sw)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be d.
            
            if (sw > 0) {

                UseAreaLimitedCamera_SW = true;

                float limitPosMinX = GetCameraAreaLimit_Min_X();
                float limitPosMaxX = GetCameraAreaLimit_Max_X();
                float limitPosMinZ = GetCameraAreaLimit_Min_Z();
                float limitPosMaxZ = GetCameraAreaLimit_Max_Z();

                if (playerPos.X <= limitPosMinX && playerPos.Z <= limitPosMinZ) 
                {
                    // sw = 0;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "0");
                    StopCamera_XZ(mapScene, playerPos, fps, limitPosMinX, limitPosMinZ);

                } else if ( (limitPosMinX < playerPos.X && playerPos.X < limitPosMaxX ) && playerPos.Z <= limitPosMinZ) {

                    // sw = 1;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "1");
                    StopCamera_Z(mapScene, playerPos, fps, limitPosMinZ);

                } else if (playerPos.X >= limitPosMaxX && playerPos.Z <= limitPosMinZ) {
                    // sw = 2;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "2");
                    StopCamera_XZ(mapScene, playerPos, fps, limitPosMaxX, limitPosMinZ);

                } else if (playerPos.X >= limitPosMaxX && (limitPosMinZ < playerPos.Z && playerPos.Z < limitPosMaxZ) ) {

                    // sw = 3;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "3");
                    StopCamera_X(mapScene, playerPos, fps, limitPosMaxX);

                } else if (playerPos.X >= limitPosMaxX && playerPos.Z >= limitPosMaxZ) {

                    // sw = 4;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "4");
                    StopCamera_XZ(mapScene, playerPos, fps, limitPosMaxX, limitPosMaxZ);

                } else if ( (limitPosMinX < playerPos.X && playerPos.X < limitPosMaxX ) && playerPos.Z >= limitPosMaxZ) {
                    // sw = 5;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "5");
                    StopCamera_Z(mapScene, playerPos, fps, limitPosMaxZ);

                } else if (playerPos.X <= limitPosMinX && playerPos.Z >= limitPosMaxZ) {
                    // sw = 6;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "6");
                    StopCamera_XZ(mapScene, playerPos, fps, limitPosMinX, limitPosMaxZ);

                } else if (playerPos.X <= limitPosMinX && (limitPosMinZ < playerPos.Z && playerPos.Z < limitPosMaxZ)) {
                    // sw = 7;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "7");
                    StopCamera_X(mapScene, playerPos, fps, limitPosMinX);

                } else {
                    // sw = 8;
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "C#", "8");
                    CameraMove(mapScene, playerPos); //OK

                }

            } else {

                UseAreaLimitedCamera_SW = false;

            }

        }

//次回　UseAreaLimitedCamera有効時のみ動作するように変更する
        public void UseSmoothFollowCamera_XZ(MapScene mapScene, Vector3 playerPos, float fps, float dampingTime)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            if (dampingTime > 0)
            {
                //初回起動時だけプレイヤーを注視点にする（これをしないとワールドゼロポイントが最初に映る）
                if(!UseSmoothFollowCameraXZ_SW) {

                    focusPoint = playerPos; //focusPointを一度だけプレイヤー座標で初期化する
                    UseSmoothFollowCameraXZ_SW = true;

                }

                CalculateSmoothFollowCamera_X(mapScene, playerPos, fps, dampingTime);
                CalculateSmoothFollowCamera_Z(mapScene, playerPos, fps, dampingTime);

            } else {

                UseSmoothFollowCameraXZ_SW = false;

            }
        }

    


        public void CalculateSmoothFollowCamera_X(MapScene mapScene, Vector3 playerPos, float fps, float dampingTime)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            if (CalculateSmoothFollowCamera_X_SW) {

                if (dampingTime > 0)
                {


                    if (0 < dampingTime && dampingTime < 1) dampingTime = 1;

                    dampingTime /= 10; //10で除算であればカクツキ少

                    //左へプレイヤーが移動したときのカメラを遅延させる
                    if (focusPoint.X >= playerPos.X) 
                    {

                        focusPoint.X -= Math.Abs(focusPoint.X - playerPos.X) / (fps * dampingTime);                       

                    }

                    //右へプレイヤーが移動したときのカメラを遅延させる
                    if (focusPoint.X < playerPos.X) 
                    {

                        focusPoint.X += Math.Abs(focusPoint.X - playerPos.X) / (fps * dampingTime);                       

                    }

                } 
            }
        }

        public void CalculateSmoothFollowCamera_Z(MapScene mapScene, Vector3 playerPos, float fps, float dampingTime)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            if (CalculateSmoothFollowCamera_Z_SW) {
                

                if (dampingTime > 0)
                {

                    if (0 < dampingTime && dampingTime < 1) dampingTime = 1;

                    dampingTime /= 10; //10で除算であればカクツキ少

            
                    //奥へプレイヤーが移動したときのカメラを遅延させる
                    if (focusPoint.Z >= playerPos.Z) 
                    {

                        focusPoint.Z -= Math.Abs(focusPoint.Z - playerPos.Z) / (fps * dampingTime);                       

                    }

                    //手前へプレイヤーが移動したときのカメラを遅延させる
                    if (focusPoint.Z < playerPos.Z) 
                    {

                        focusPoint.Z += Math.Abs(focusPoint.Z - playerPos.Z) / (fps * dampingTime);                       

                    }

                }
            } 
        }

        public void UseSmoothFollowCamera_Y(MapScene mapScene, Vector3 playerPos, float fps, float dampingTime)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.

            if (dampingTime > 0)
            {

                //初回起動時だけプレイヤーを注視点にする（これをしないとワールドゼロポイントが最初に映る）
                if(!UseSmoothFollowCameraY_SW) {

                    focusPoint = playerPos; //focusPointを一度だけプレイヤー座標で初期化する
                    UseSmoothFollowCameraY_SW = true; //SmoothFollowCameraをオンにするとSetCameraHightがオフになる　これによりイベントパネルとして先に実行された方が優先される

                }

                if (0 < dampingTime && dampingTime < 1) dampingTime = 1;

                    dampingTime /= 10; //10で除算であればカクツキ少


                //上へプレイヤーが移動したときのカメラを遅延させる
                if (focusPoint.Y < playerPos.Y) 
                {

                    focusPoint.Y += Math.Abs(focusPoint.Y - playerPos.Y) / (fps * dampingTime);                       

                }

                //下へプレイヤーが移動したときのカメラを遅延させる
                if (focusPoint.Y >= playerPos.Y) 
                {

                    focusPoint.Y -= Math.Abs(focusPoint.Y - playerPos.Y) / (fps * dampingTime);                       

                }
                
            } else {

                UseSmoothFollowCameraY_SW = false; //SmoothFollowCameraをオフにするとSetCameraHightがオンになる

            }

        }
    }

}
//Version 20231103

//外部ファイルを読み込み
//注意　ExtensionBまたはExtensionCをインポートすること！
// @@include ExtensionB_SpecialCamera.cs
// @@include ExtensionC_FrameCount.cs

using System;
using Yukar.Common;
using Yukar.Engine;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;


//外部ファイルの名前空間を使用
using SpecialCamera;
using FrameCount;

namespace Bakin
{
    public class CameraPlugin2 : BakinObject
    {
        public override void Start()
        {
            // キャラクターが生成される時に、このメソッドがコールされます。
            // This method is called when the character is created.
            
            new FrameCountClass().setOldSec();
        }


        public override void AfterDraw()
        {
            // このフレームの2D描画処理の最後に、このメソッドがコールされます。
            // This method is called at the end of the 2D drawing process for this frame.
            
            // fps計測
            new FrameCountClass().FPSCount();            
        }
        
        
        
        [BakinFunction(Description="　This is a plug-in that expands camera functionality.\n　Ver.20231103\n　This program was written by SANA \n Translated by MelonToucan")] public void ___CameraPlugin2_Command_List___(){}

        [BakinFunction(Description="　Below are commands related to the camera that can limit the area that follows the player.\n　It only works on the (Run in parallel) sheet.")] public void ___AreaLimitedCamera___(){}


        [BakinFunction(Description="　Switch to a special camera with a limited area that follows the player.\n\n　You can set the width of the camera non-following area from the top, bottom, left, and right of the map using parameters.\n\n　The function is enabled when the parameter is 0 or more.")]
        public void UseAreaLimitedCamera_Simple(float limit)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            new SpecialCameraClass().UseAreaLimitedCamera_Simple(mapScene, mapScene.hero.getPosition(), new FrameCountClass().GetFPS(), limit);
            
            

        }

        [BakinFunction(Description="　Switch to a special camera with a limited area that follows the player.\n\n　The function is enabled when the parameter is 1 or more.\n\n　The camera tracking area can be set using option commands.")]
        public void UseAreaLimitedCamera(int sw)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be d.

            
            new SpecialCameraClass().UseAreaLimitedCamera(mapScene, mapScene.hero.getPosition(), new FrameCountClass().GetFPS(), sw);
        

        }

        [BakinFunction(Description="　Below are the optional commands for:\n\nUseAreaLimitedCamera_Simple\nUseAreaLimitedCamera.")] public void ___Options___(){}

        [BakinFunction(Description="　Set the X coordinate of the left edge of the camera tracking area as a parameter. \n\n*Please set a value smaller than the parameter of the SetCameraAreaLimit_Right command*")]
        public void SetCameraAreaLimit_Left(float limitPosMinX)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            new SpecialCameraClass().SetCameraAreaLimit_Min_X(limitPosMinX);

        }


        [BakinFunction(Description=" Set the X coordinate of the right edge of the camera tracking area as a parameter. \n\n*Please set a value greater than the parameter of the SetCameraAreaLimit_Left command*")]
        public void SetCameraAreaLimit_Right(float limitPosMaxX)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            new SpecialCameraClass().SetCameraAreaLimit_Max_X(limitPosMaxX);

        }


        [BakinFunction(Description=" Set the Z coordinate of the top of the camera tracking area as a parameter. \n\n*Please set a value smaller than the parameter of the SetCameraAreaLimit_Bottom command*")]
        public void SetCameraAreaLimit_Top(float limitPosMinZ)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            new SpecialCameraClass().SetCameraAreaLimit_Min_Z(limitPosMinZ);

        }


        [BakinFunction(Description="　Set the Z coordinate of the bottom edge of the camera tracking area as a parameter. \n\n*Please set a value larger than the parameter of the SetCameraAreaLimit_Top command*")]
        public void SetCameraAreaLimit_Bottom(float limitPosMaxZ)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            new SpecialCameraClass().SetCameraAreaLimit_Max_Z(limitPosMaxZ);
            
        }

        [BakinFunction(Description="　Fixes the height of the camera gaze point. \n\n　The height of the gaze point can be fixed at the Y value of the specified world coordinate. \n\n　The function is enabled when the parameter is 0 or more. \n\n*Cannot be used at the same time as UseSmoothFollowCamera_Y.")]
        public void SetCameraHight(float PosY)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            if (PosY >= 0)
            {

                new SpecialCameraClass().SetCameraHight(PosY);
                
            }

        }
        


        [BakinFunction(Description="　Enable camera to now follow the player's XZ axis movements with a delay. \n　You can set the delay degree in the parameter. \n　The function is enabled when the parameter is 1 or more.")]
        public void UseSmoothFollowCamera_XZ(float dampingTime)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            new SpecialCameraClass().UseSmoothFollowCamera_XZ(mapScene, mapScene.hero.getPosition(), new FrameCountClass().GetFPS(), dampingTime); //OK

        }

        
        [BakinFunction(Description="　Enable camera to now follow the player's Y-axis movements with a delay. \n\n　You can set the delay degree in the parameter. \n\n　The function is enabled when the parameter is 1 or more. \n\n*Cannot be used at the same time as SetCameraHight")]
        public void UseSmoothFollowCamera_Y(float dampingTime)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            new SpecialCameraClass().UseSmoothFollowCamera_Y(mapScene, mapScene.hero.getPosition(), new FrameCountClass().GetFPS(), dampingTime);

        }

        
    }
}

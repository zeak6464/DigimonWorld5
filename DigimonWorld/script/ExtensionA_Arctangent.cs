using System;
using Yukar.Engine;
using Yukar.Common;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;

namespace Arctangent
{
    public class ArctangentClass : BakinObject
    {

/*
        public override void Start()
        {
            // キャラクターが生成される時に、このメソッドがコールされます。
            // This method is called when the character is created.
            
            
            // var EnemyCount = GameMain.instance.data.system.GetVariable("EnemyCount");
            // EnemyCount =+ 1;
            // GameMain.instance.data.system.SetVariable("EnemyCount",EnemyCount);


            // MessageBox.Show(mapChr.guId.ToString() );//メッセージボックスの表示方法　動作確認済み

            GUID = mapChr.guId.ToString(); //GUID取得

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

            // var GUID = mapChr.guId.ToString(); //GUID取得

            //プレイヤー座標
            var PlayerPos = mapScene.hero.getPosition();
            var PlayerPos2D = new Vector3(PlayerPos.X, 0, PlayerPos.Z);

            //エネミー座標
            var EnemyPos = mapChr.getPosition();
            var EnemyPos2D = new Vector3(EnemyPos.X, 0, EnemyPos.Z);

            //二次元距離計算
            var Distance2D = Vector3.Distance(EnemyPos2D, PlayerPos2D);
            
            //三次元距離計算
            var Distance3D = Vector3.Distance(EnemyPos, PlayerPos);
        
            //正規化ベクトル計算（二次元）
            var NVector2D = Vector3.Subtract(EnemyPos2D, PlayerPos2D);
            NVector2D = Vector3.Normalize(NVector2D);

            //正規化ベクトル計算（三次元）
            var NVector = Vector3.Subtract(EnemyPos, PlayerPos);
            NVector = Vector3.Normalize(NVector);


            //ローカル変数ボックスを生成し数値を代入
            GameMain.instance.data.system.SetVariable("3DDistanceFromPlayer", Distance3D, new Guid(GUID), false);
            
            GameMain.instance.data.system.SetVariable("2DDistanceFromPlayer", Distance2D, new Guid(GUID), false);
            
            GameMain.instance.data.system.SetVariable("3DNVectorX", NVector.X, new Guid(GUID), false);
            GameMain.instance.data.system.SetVariable("3DNVectorY", NVector.Y, new Guid(GUID), false);
            GameMain.instance.data.system.SetVariable("3DNVectorZ", NVector.Z, new Guid(GUID), false);

            GameMain.instance.data.system.SetVariable("2DNVectorX", NVector2D.X, new Guid(GUID), false);
            GameMain.instance.data.system.SetVariable("2DNVectorZ", NVector2D.Z, new Guid(GUID), false);


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
        
        
        public float HorizontalAngle(float x, float z)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            var HorizontalAngle = (Math.Atan2(x, z) )* (180 / Math.PI);
            return (float)HorizontalAngle;
        }
        
        public float VerticalAngle(Vector3 a, Vector3 b)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            var a2D = new Vector3(a.X, 0, a.Z);
            var b2D = new Vector3(b.X, 0, b.Z);
            var dist = Vector3.Distance(a2D, b2D);
            var VerticalAngle = (Math.Atan2(a.Y - b.Y, dist) ) * (180 / Math.PI) * -1;
            return (float)VerticalAngle;
        }
        
    }
            //水平角度
            // var HorizontalAngleToEndPos = (Math.Atan2(NVector2DToEndPos.X, NVector2DToEndPos.Z) )* (180/ Math.PI);
            // var HorizontalAngleToStartPos = (Math.Atan2(NVector2DToEndPos.X * -1, NVector2DToEndPos.Z * -1) )* (180/ Math.PI);

            //垂直角度
            // var VerticalAngleToEndPos = (Math.Atan2(endingPosition.Y - startingPosition.Y, Distance2D) ) * (180/ Math.PI) * -1;
            // var VerticalAngleToStartPos = (Math.Atan2(startingPosition.Y - endingPosition.Y, Distance2D) ) * (180/ Math.PI) * -1;
}

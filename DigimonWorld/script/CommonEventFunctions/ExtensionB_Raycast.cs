using System;
using Yukar.Common;
using Yukar.Engine;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;

namespace Raycast
{
    public class RaycastClass : BakinObject
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
        public string Raycast_GetEventName(MapCharacter Chr, Vector3 from, Vector3 to, float maxDistance)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            // if (maxDistance > 0) to = Vector3.Add(from, Vector3.Multiply(Vector3.Normalize(Vector3.Subtract(to, from) ), maxDistance) );
            to = Vector3.Add(from, Vector3.Multiply(Vector3.Normalize(Vector3.Subtract(to, from) ), maxDistance) ); //fromからtoまでの正規化ベクトルにmaxDistanceを乗算してレイキャスト距離を設定
            
            var hit = new SharpKmyPhysics.RayCastHit();
            var result = Chr.getRigidbody().getPhysicsBase().rayCast(Util.ToKmyVector(from), Util.ToKmyVector(to), (ushort)CollisionType.EVENT, hit);

            if(result)
            {
                var evt = hit?.node.getNotifyTarget() as MapCharacter;
                return evt.rom?.name;
            } else {
                return "NONE";
            }
            
        }

        public float Raycast_GetEventDistance(MapCharacter Chr, Vector3 from, Vector3 to, float maxDistance)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            // if (maxDistance > 0) to = Vector3.Add(from, Vector3.Multiply(Vector3.Normalize(Vector3.Subtract(to, from) ), maxDistance) );
            to = Vector3.Add(from, Vector3.Multiply(Vector3.Normalize(Vector3.Subtract(to, from) ), maxDistance) ); //fromからtoまでの正規化ベクトルにmaxDistanceを乗算してレイキャスト距離を設定
            
            var hit = new SharpKmyPhysics.RayCastHit();
            var result = Chr.getRigidbody().getPhysicsBase().rayCast(Util.ToKmyVector(from), Util.ToKmyVector(to), (ushort)CollisionType.EVENT, hit);
            if(result)
            {
                return hit.distance;
            } else {
                return -1;
            }
            
        }

        public float Raycast_GetALLObjectDistance(MapCharacter Chr, Vector3 from, Vector3 to, float maxDistance)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            // if (maxDistance > 0) to = Vector3.Add(from, Vector3.Multiply(Vector3.Normalize(Vector3.Subtract(to, from) ), maxDistance) );
            to = Vector3.Add(from, Vector3.Multiply(Vector3.Normalize(Vector3.Subtract(to, from) ), maxDistance) ); //fromからtoまでの正規化ベクトルにmaxDistanceを乗算してレイキャスト距離を設定
            
            var hit = new SharpKmyPhysics.RayCastHit();
            var result = Chr.getRigidbody().getPhysicsBase().rayCast(Util.ToKmyVector(from), Util.ToKmyVector(to), (ushort)CollisionType.ALL, hit);

            if(result)
            {
                return hit.distance;
            } else {
                return -1;
            }

            // var resultM = Chr.getRigidbody().getPhysicsBase().rayCast(Util.ToKmyVector(from), Util.ToKmyVector(to), (ushort)CollisionType.MAP, hit);
            // var targetDistance = hit.distance;
            // var targetPos = hit.position;
            // var evt = hit.node.getNotifyTarget();
            


        }

    }
}

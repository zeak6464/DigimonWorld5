using System;
using SharpKmyMath;
using Yukar.Common;
using Yukar.Engine;
using UnityEngine;

namespace SharpKmyGfx
{
    public class Light
    {
        private static GameObject lightObject;
        private static UnityEngine.Light lightComponent;

        static bool isEnable()
        {
            //分割しない場合は有効
            if (UnityEntry.IsDivideMapScene() == false) return true;
            if (UnityEntry.IsImportMapScene()) return true;//分割読み込み中は有効
            return false;
        }

        private void setDirLightColor(Color color)
        {
            if (isEnable() == false) return;
            if (lightComponent == null) return;
            lightComponent.color = new UnityEngine.Color(color.r, color.g, color.b, color.a);
        }

        internal void setAmbLightColor(Color color)
        {
            if (isEnable() == false) return;

            RenderSettings.ambientLight = new UnityEngine.Color(color.r, color.g, color.b, color.a);
            RenderSettings.ambientLight *= 1.33f;   // KMYとアンビエントの効き具合が違うので調整
        }

        internal static Light createDirection(Color _color, int v1, Matrix4 matrix4, int v2, int v3, int v4, int v5)
        {
            initLight();

            return new Light();
        }

        private static void initLight()
        {
            refind();
            
            if (isEnable() == false) return;

            //lightComponent.intensity = 0.5f;
            // アンビエントライトのモードを単色に設定
            // UnityのLighting WindowにおけるSourceをColorに設定している
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        }

        public static void refind()
        {
            // Unityシーン中の光源オブジェクト(Directional Light)を取得
            lightObject = GameObject.Find("Directional Light");
            if (lightObject == null)
            {
                lightComponent = null;
                return;
            }
            lightComponent = lightObject.GetComponent<UnityEngine.Light>();
        }


        internal Render getShadowMapRender()
        {
            return null;
        }

        internal void setPosture(Matrix4 matrix4)
        {
            if (isEnable() == false) return;
            if (lightComponent == null) return;
			// 光の位置座標を取得
			UnityEngine.Vector3 position = UnityUtil.ExtractPosition(matrix4.m);
			if ( !float.IsInfinity(position.x) && !float.IsNaN(position.x) )
				lightComponent.transform.position = position;
            // 光の回転角度を取得
			Quaternion rotation = UnityUtil.ExtractRotation(matrix4.m);
			UnityEngine.Vector3 eulerAngles = rotation.eulerAngles; // x座標値をいじるためオイラー角として取り出し
            //eulerAngles.x += 45; // x軸についてKMYとUnityでは45度分ズレがあるみたいなので+45している
            eulerAngles.y += 180; // x軸に+45よりもy軸半回転の方がなんとなく綺麗なのでこちらを採用
            eulerAngles.z += 180; // x軸に+45よりもz軸半回転の方がなんとなく綺麗なのでこちらを採用
            lightComponent.transform.rotation = Quaternion.Euler(eulerAngles);
		}

        internal void Release()
        {
        }

        internal void addShadowMapDrawable(MapData mapData)
        {
        }

        internal void setRadius(float v)
        {
            if (isEnable() == false) return;
            if (lightComponent == null) return;
            lightComponent.range = v; // 一応radiusの値をrangeとして設定しているが, ディレクショナルライトの場合は無意味
        }

        internal void setColor(Color color)
        {
            setDirLightColor(color);
        }
    }
}
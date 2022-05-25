using System;
using SharpKmyMath;
using UnityEngine;

namespace SharpKmyGfx
{
    public class Render : MonoBehaviour
    {
        private static GameObject defaultRenderObj = null;
        private static GameObject guiRenderObj = null;
        private static Render defaultRender = null;
        private static Render guiRender = null;
        //private static Render defaultRender = new Render();
        //private static Render guiRender = new Render();
        private static GameObject mainCamera;
        //Color clearColor;
        Matrix4 viewMatrix;
        Matrix4 ProjMatrix;
        Light kmyLight;

        internal static void InitializeRender()
        {
            defaultRenderObj = Yukar.Common.UnityUtil.createObject(
               Yukar.Common.UnityUtil.ParentType.ROOT, "defaultRender");
            guiRenderObj = Yukar.Common.UnityUtil.createObject(
            Yukar.Common.UnityUtil.ParentType.ROOT, "guiRender");
            defaultRender = defaultRenderObj.AddComponent<Render>();
            guiRender = guiRenderObj.AddComponent<Render>();
        }

        internal static void InitializeCamera()
        {
            mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }

        internal static void refind()
        {
            var staticObj = GameObject.Find("sgb_static");
            if (staticObj == null) return;
            var newMainCameraT = staticObj.transform.Find("Main Camera");
            if (newMainCameraT == null) return;
            var newMainCamera = newMainCameraT.gameObject;
            if (mainCamera != null)
            {
                mainCamera.SetActive(false);
            }
            mainCamera = newMainCamera;
            mainCamera.SetActive(true);
        }
        

        internal static Render getRenderL()
        {
            return getDefaultRender();
        }

        internal static Render getRenderR()
        {
            return null;
        }

        internal static bool isSameScene(Render scn1, Render scn2)
        {
            return scn1 == scn2;
        }

        internal static Render getDefaultRender()
        {
            return defaultRender;
        }

        internal static Render getRender2D()
        {
            return guiRender;
        }

        internal void setViewport(int v1, int v2, int v3, int v4)
        {
        }

        internal void setViewMatrix(Matrix4 proj, Matrix4 view)
        {
#if !ENABLE_VR_UNITY
            if (view.Equals(Matrix4.identity())) return;
            viewMatrix = view;
            ProjMatrix = proj;
            var inv = Matrix4.inverse(view);

            if (mainCamera == null)
            {
                InitializeCamera();
            }

            var camera = mainCamera.GetComponent<Camera>() as Camera;
            //camera.projectionMatrix = proj.m; // fieldOfView設定を反映させるためにコメントアウト。near,farを個別に設定する必要あり
            //camera.worldToCameraMatrix = view.m;
            mainCamera.transform.localPosition = Yukar.Common.UnityUtil.ExtractPosition(inv.m);
            mainCamera.transform.localRotation = Yukar.Common.UnityUtil.ExtractRotation(inv.m);

            // x,y軸反転(暫定)
            UnityEngine.Vector3 tmp = mainCamera.transform.localRotation.eulerAngles;
            tmp.x *= -1; tmp.y *= -1;
            mainCamera.transform.localRotation = Quaternion.Euler(tmp);
            // 位置補完(暫定) // 地形の高さとカメラの高さをリンクさせる過程で不要に
            //tmp = mainCamera.transform.localPosition;
            //tmp.y += 5.0f;
            //mainCamera.transform.localPosition = tmp;

            // 視野角
			var theta = 4 * Mathf.Atan(1 / proj.m11);
            camera.fieldOfView = theta * 180 / Mathf.PI;
#else
            var camera = GameObject.Find("CameraContainer");
            if (view.Equals(Matrix4.identity())) return;
            viewMatrix = view;
            ProjMatrix = proj;
            var inv = Matrix4.inverse(view);
            camera.transform.localPosition = Yukar.Common.UnityUtil.ExtractPosition(inv.m);
            camera.transform.localRotation = Yukar.Common.UnityUtil.ExtractRotation(inv.m);

            UnityEngine.Vector3 tmp = camera.transform.localRotation.eulerAngles;
            tmp.x *= -1; tmp.y *= -1;
            camera.transform.localRotation = Quaternion.Euler(tmp);
#endif
        }

        internal void addDrawable(Drawable drawable)
        {
            drawable.draw(this);
            // TODO
        }

        internal bool viewVolumeCheck(SharpKmyMath.Vector3 p, float size)
        {
            return true;
        }

        internal void draw(DrawInfo di)
        {
            if (di.drawable != null)
                di.drawable.draw(this);
        }

        internal void setAmbientColor(SharpKmyMath.Vector3 vector3)
        {
            kmyLight.setAmbLightColor(new SharpKmyGfx.Color(vector3.x, vector3.y, vector3.z, 1));
        }

        internal void setLight(Light light)
        {
            kmyLight = light;
            //throw new NotImplementedException();
        }

        internal void setClearColor(float v1, float v2, float v3, float v4)
        {
            //clearColor = new Color(v1, v2, v3, v4);
            //throw new NotImplementedException();
        }

        internal void getViewMatrix(ref Matrix4 pm, ref Matrix4 vm)
        {
            pm = ProjMatrix;
            vm = viewMatrix;
            //throw new NotImplementedException();
        }

        internal Texture getColorTexture()
        {
            throw new NotImplementedException();
        }

        internal void resetCameraMatrix()
        {
            if (mainCamera == null)
            {
                InitializeCamera();
            }

            var camera = mainCamera.GetComponent<Camera>() as Camera;
            camera.ResetProjectionMatrix();
        }
    }
}
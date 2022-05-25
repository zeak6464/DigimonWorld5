using System;
using SharpKmyMath;

namespace SharpKmyVr
{
    public enum EyeType
    {
        Left,
        Right
    }

    public class Func
    {
        public static bool IsReady()
        {
            return false;
        }

        internal static Vector3 GetHmdPosePos()
        {
            return new Vector3();
        }

        internal static Matrix4 GetHmdPoseRotateMatrix()
        {
            return Matrix4.identity();
        }

        internal static Vector3 GetHmdPoseDirection()
        {
            return new Vector3();
        }

        internal static Matrix4 GetProjectionMatrix(EyeType left)
        {
            return Matrix4.identity();
        }

        internal static float GetHmdRotateY()
        {
            return 0;
        }

        internal static Matrix4 GetViewMatrix(EyeType eyeType, Vector3 m_UpVec, Vector3 m_CameraPos, Matrix4 matrix4)
        {
            throw new NotImplementedException();
        }
    }
}
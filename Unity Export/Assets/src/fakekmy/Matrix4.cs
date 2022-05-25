using System;

namespace SharpKmyMath
{
    public struct Matrix4
    {
        public UnityEngine.Matrix4x4 m;

        public float m00 { get { return m.m00; } }
        public float m01 { get { return m.m10; } }
        public float m02 { get { return m.m20; } }
        public float m03 { get { return m.m30; } }
        public float m10 { get { return m.m01; } }
        public float m11 { get { return m.m11; } }
        public float m12 { get { return m.m21; } }
        public float m13 { get { return m.m31; } }
        public float m20 { get { return m.m02; } }
        public float m21 { get { return m.m12; } }
        public float m22 { get { return m.m22; } }
        public float m23 { get { return m.m32; } }
        public float m30 { get { return m.m03; } }
        public float m31 { get { return m.m13; } }
        public float m32 { get { return m.m23; } }
        public float m33 { get { return m.m33; } }

        internal static Matrix4 ortho(float l, float r, float t, float b, int n, int f)
        {
            Matrix4 result;
            result.m = UnityEngine.Matrix4x4.Ortho(l, r, b, t, n, f);
            return result;
        }

        internal static Matrix4 identity()
        {
            Matrix4 result;
            result.m = UnityEngine.Matrix4x4.identity;
            return result;
        }

        internal static Matrix4 translate(float x, float y, float z)
        {
            Matrix4 result;
            result.m = UnityEngine.Matrix4x4.Translate(new UnityEngine.Vector3(x, y, z));
            return result;
        }

        internal static Matrix4 rotateZ(float r)
        {
            Matrix4 retval;
            retval = identity();
            retval.m.m00 = (float)Math.Cos(r);
            retval.m.m01 = (float)Math.Sin(r);
            retval.m.m10 = -(float)Math.Sin(r);
            retval.m.m11 = (float)Math.Cos(r);
            return retval;
        }

        internal static Matrix4 rotateY(float r)
        {
            Matrix4 retval;
            retval = identity();
            retval.m.m00 = (float)Math.Cos(r);
            retval.m.m02 = -(float)Math.Sin(r);
            retval.m.m20 = (float)Math.Sin(r);
            retval.m.m22 = (float)Math.Cos(r);
            return retval;
        }

        internal static Matrix4 rotateX(float r)
        {
            Matrix4 retval;
            retval = identity();
            retval.m.m11 = (float)Math.Cos(r);
            retval.m.m12 = (float)Math.Sin(r);
            retval.m.m21 = -(float)Math.Sin(r);
            retval.m.m22 = (float)Math.Cos(r);
            return retval;
        }

        public static Matrix4 operator *(Matrix4 a, Matrix4 b)
        {
            Matrix4 result;
            result.m = a.m * b.m;
            return result;
        }

        public static Vector4 operator *(Matrix4 mtx, Vector3 vec)
        {
            var uniVec = mtx.m * new UnityEngine.Vector4(vec.x, vec.y, vec.z, 1.0f);
            var result = new Vector4(uniVec.x, uniVec.y, uniVec.z, uniVec.w);
            return result;
        }

        public static Vector4 operator *(Matrix4 mtx, Vector4 vec)
        {
            var uniVec = mtx.m * new UnityEngine.Vector4(vec.x, vec.y, vec.z, vec.w);
            var result = new Vector4(uniVec.x, uniVec.y, uniVec.z, uniVec.w);
            return result;
        }

        internal Vector3 translation()
        {
            var vec = Yukar.Common.UnityUtil.ExtractPosition(m);
            return new Vector3(vec.x, vec.y, vec.z);
        }

        internal static Matrix4 lookat(Vector3 eye, Vector3 target, Vector3 upvec)
        {
            Matrix4 ret;
            ret.m = UnityEngine.Matrix4x4.LookAt(eye.getUnityVector3(), target.getUnityVector3(), upvec.getUnityVector3());
            return ret;
        }

        internal static Matrix4 inverse(Matrix4 v)
        {
            Matrix4 ret;
            ret.m = v.m.inverse;
            return ret;
        }

        internal static Matrix4 perspectiveFOV(float fov, float asp, float znear, float zfar)
        {
            Matrix4 retval = identity();
            retval.m = UnityEngine.Matrix4x4.Perspective(fov * 180 / (float)Math.PI, asp, znear, zfar);
            return retval;
        }
    }
}
using System;

namespace SharpKmyMath
{
    public struct Vector3
    {
        public Vector3(float _x, float _y, float _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }

        public Vector3(float init)
        {
            x = init;
            y = init;
            z = init;
        }

        public float this[int index]
        {
            private set
            {
                switch (index)
                {
                    case 0: this.x = value; return;
                    case 1: this.y = value; return;
                    case 2: this.z = value; return;
                }
            }
            get {

                switch (index)
                {
                    case 0: return this.x;
                    case 1: return this.y;
                    case 2: return this.z;
                }
                return float.NaN;
            }
        }

        public void set(int inIndex, float inValue) { this[inIndex] = inValue; }

        public static Vector3 operator *(Vector3 vec, float scale)
        {
            return new Vector3(vec.x * scale, vec.y * scale, vec.z * scale);
        }
        public static Vector3 operator /(Vector3 vec, float scale)
        {
            return new Vector3(vec.x / scale, vec.y / scale, vec.z / scale);
        }

        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static Vector3 operator -(Vector3 v)
        {
            Vector3 ret;
            ret.x = -v.x;
            ret.y = -v.y;
            ret.z = -v.z;
            return ret;
        }
        
        public float x, y, z;

        internal static Vector3 crossProduct(Vector3 v1, Vector3 v2)
        {
            Vector3 tmp;
            tmp.x = v1.y * v2.z - v1.z * v2.y;
            tmp.y = v1.z * v2.x - v1.x * v2.z;
            tmp.z = v1.x * v2.y - v1.y * v2.x;
            return tmp;
        }
        public float length()
        {
            return (float)Math.Sqrt(x * x + y * y + z * z);
        }

        public static Vector3 normalize(Vector3 v)
        {
            float l = (float)Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
            Vector3 ret;
            ret.x = v.x / l;
            ret.y = v.y / l;
            ret.z = v.z / l;
            return ret;
        }

        internal static float dotProduct(Vector3 v1, Vector3 v2)
        {
            return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
        }

        public UnityEngine.Vector3 getUnityVector3()
        {
            return new UnityEngine.Vector3(x, y, z);
        }
    }
}
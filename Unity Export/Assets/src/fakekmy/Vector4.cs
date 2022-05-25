namespace SharpKmyMath
{
    public class Vector4
    {
        public Vector4(float _x, float _y, float _z, float _w)
        {
            x = _x;
            y = _y;
            z = _z;
            w = _w;
        }

        public Vector4(float init)
        {
            x = init;
            y = init;
            z = init;
            w = init;
        }

        public Vector3 getXYZ()
        {
            return new Vector3(x, y, z);
        }


        public static Vector4 operator *(Vector4 vec, float scale)
        {
            return new Vector4(vec.x * scale, vec.y * scale, vec.z * scale, vec.w * scale);
        }

        public static Vector4 operator /(Vector4 vec, float scale)
        {
            return new Vector4(vec.x / scale, vec.y / scale, vec.z / scale, vec.w / scale);
        }


        public static Vector4 operator *(Vector4 vec, Vector4 vec2)
        {
            return new Vector4(vec.x * vec2.x, vec.y * vec2.y, vec.z * vec2.z, vec.w * vec2.w);
        }

        public static Vector4 operator +(Vector4 vec, Vector4 vec2)
        {
            return new Vector4(vec.x + vec2.x, vec.y + vec2.y, vec.z + vec2.z, vec.w + vec2.w);
        }

        public static Vector4 operator -(Vector4 vec, Vector4 vec2)
        {
            return new Vector4(vec.x - vec2.x, vec.y - vec2.y, vec.z - vec2.z, vec.w - vec2.w);
        }

        public float x, y, z, w;
    }
}
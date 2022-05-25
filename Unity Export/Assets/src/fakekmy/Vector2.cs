namespace SharpKmyMath
{
    public struct Vector2
    {
        public float x, y;

        public Vector2(float _x, float _y)
        {
            x = _x;
            y = _y;
        }

        public static Vector2 operator +(Vector2 v, Vector2 v2)
        {
            Vector2 ret;
            ret.x = v.x + v2.x;
            ret.y = v.y + v2.y;
            return ret;
        }

        public static Vector2 operator -(Vector2 v, Vector2 v2)
        {
            Vector2 ret;
            ret.x = v.x - v2.x;
            ret.y = v.y - v2.y;
            return ret;
        }
        
        public static Vector2 operator /(Vector2 v, Vector2 v2)
        {
            Vector2 ret;
            ret.x = v.x / v2.x;
            ret.y = v.y / v2.y;
            return ret;
        }

        public static Vector2 operator *(Vector2 v, Vector2 v2)
        {
            Vector2 ret;
            ret.x = v.x * v2.x;
            ret.y = v.y * v2.y;
            return ret;
        }

        public static Vector2 operator +(Vector2 v, float f)
        {
            Vector2 ret;
            ret.x = v.x + f;
            ret.y = v.y + f;
            return ret;
        }

        public static Vector2 operator -(Vector2 v, float f)
        {
            Vector2 ret;
            ret.x = v.x - f;
            ret.y = v.y - f;
            return ret;
        }

        public static Vector2 operator /(Vector2 v, float f)
        {
            Vector2 ret;
            ret.x = v.x / f;
            ret.y = v.y / f;
            return ret;
        }

        public static Vector2 operator *(Vector2 v, float f)
        {
            Vector2 ret;
            ret.x = v.x * f;
            ret.y = v.y * f;
            return ret;
        }

        public static Vector2 operator -(Vector2 v)
        {
            Vector2 ret;
            ret.x = -v.x;
            ret.y = -v.y;
            return ret;
        }
    }
}
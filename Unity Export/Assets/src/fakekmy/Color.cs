using System;
using SharpKmyMath;

namespace SharpKmyGfx
{
    public struct Color
    {
        public Color(float _r, float _g, float _b, float _a)
        {
            r = _r;
            g = _g;
            b = _b;
            a = _a;
        }

        public float r, g, b, a;
        internal static readonly Color White = new Color(1, 1, 1, 1);
    }
}
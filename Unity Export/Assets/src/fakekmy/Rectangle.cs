using System;

namespace SharpKmyMath
{
    public class Rectangle
    {
        public float x, y, width, height;

        public float Top { get { return y; } }
        public float Left { get { return x; } }
        public float Bottom { get { return y + height; } }
        public float Right { get { return x + width; } }

        public Rectangle(float _x, float _y, float _width, float _height)
        {
            x = _x;
            y = _y;
            width = _width;
            height = _height;
        }
    }
}
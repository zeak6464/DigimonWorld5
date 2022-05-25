using System;
using SharpKmyMath;

namespace SharpKmyGfx
{
    public class CommonPrimitive
    {
        public enum CULLTYPE
        {
            BACK,
        }
    }

    public class BoxFillPrimitive
    {
        public BoxFillPrimitive()
        {
        }

        internal void setColor(float v1, float v2, float v3, float v4)
        {
            throw new NotImplementedException();
        }

        internal void setSize(float v1, float v2, float v3)
        {
            throw new NotImplementedException();
        }

        internal void setShaderName(string v)
        {
            throw new NotImplementedException();
        }

        internal void setCull(CommonPrimitive.CULLTYPE cull)
        {
            throw new NotImplementedException();
        }

        internal void draw(Render scn)
        {
            throw new NotImplementedException();
        }

        internal void setMatrix(Matrix4 matrix4)
        {
            throw new NotImplementedException();
        }

        internal void Release()
        {
            throw new NotImplementedException();
        }
    }
}
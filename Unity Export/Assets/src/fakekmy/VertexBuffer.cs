using System;
using System.Collections.Generic;

namespace SharpKmyGfx
{
    public class VertexBuffer
    {
        internal int vtxcount;
        internal VertexPositionNormalTextureColor[] buf;
        public VertexPositionNormalTexture2Color[] buf2;
        public DrawInfo owner;

        internal void setData(List<VertexPositionNormalTexture2Color> op_vlist)
        {
            buf2 = op_vlist.ToArray();
            vtxcount = op_vlist.ToArray().Length;
            //UnityEngine.Debug.Log("vtxcount2 : " + vtxcount);

            // TODO
        }

        internal void Release()
        {
            //MakeTerrainMesh mtm = new MakeTerrainMesh();
            //mtm.release();
            if(owner != null)
                owner.removeVertexBuffer(this);
            buf = null;
        }

        internal void setData(VertexPositionNormalTextureColor[] vtx)
        {
            buf = vtx;
            vtxcount = buf.Length;
            //UnityEngine.Debug.Log("vtxcount : " + vtxcount);
        }
    }
}
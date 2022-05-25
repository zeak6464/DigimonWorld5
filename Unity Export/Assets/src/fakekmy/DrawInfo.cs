using System;
using SharpKmyMath;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SharpKmyGfx
{
    public enum FUNCTYPE
    {
        kALWAYS,
        kLEQUAL,
        kPREMULTIPLIED,
        kNEVER,
        kEQUAL,
    }

    public enum STENCILOP
    {
        kREPLACE,
        kKEEP,
        kINC,
    }

    public enum BLENDTYPE
    {
        kOPAQUE,
        kALPHA,
        kADD,
        kPREMULTIPLIED,
    }

    public enum CULLTYPE
    {
        kNONE,
        kBACK,
    }

    public class DrawInfo
    {
        public Drawable drawable;
        internal Texture texture;
        internal Shader shader;
        internal BLENDTYPE blendType = BLENDTYPE.kOPAQUE;
        internal FUNCTYPE funcType = FUNCTYPE.kLEQUAL;
        internal CULLTYPE cullType = CULLTYPE.kNONE;

        internal bool depthWrite = true;
        internal int layer = 0;
        //internal Color modColor;
        internal Matrix4 m_l2w;
        internal int param;
        internal int indexCount;
        internal Color color;

        // マップ描画用
        internal List<TerrainMesh> terrains;

        // ビルボード描画用
        internal VolatilePremitive plane;

        internal void Release()
        {
            if (plane != null)
            {
                UnityEngine.Object.Destroy(plane.gameObject);
                plane = null;
            }
        }

        internal void setStencilEnable(bool v)
        {
        }

        internal void setStencilFunc(FUNCTYPE kALWAYS, int v1, int v2)
        {
        }

        internal void setStencilOp(STENCILOP kREPLACE1, STENCILOP kKEEP, STENCILOP kREPLACE2)
        {
        }

        internal void setColor(Color color)
        {
            if (plane != null)
            {
                plane.setColor(color);
            }

            this.color = color;
        }

        internal void setLayer(int v)
        {
        }

        internal void setDepthFunc(FUNCTYPE kLEQUAL)
        {
        }

        internal BLENDTYPE getBlend()
        {
            return blendType;
        }

        internal void setFPColor(string v, Color fogDisabledColor)
        {
            //throw new NotImplementedException();
        }

        internal void setFPValue(string v, float fognear)
        {
            // throw new NotImplementedException();
        }

        internal Texture getTexture(int v)
        {
            return texture;
        }

        internal void setL2W(Matrix4 matrix4)
        {
            m_l2w = matrix4;
        }

        internal void setBlend(BLENDTYPE blend)
        {
            blendType = blend;
        }

        internal void setVertexBuffer(VertexBuffer vb, int state = 0, int mHcount = 0, int mDcount = 0)
        {
            // state
            // inMap            0
            // outMap repeat    1
            // outMap water     2
            if (terrains == null)
                terrains = new List<TerrainMesh>();

            // Repeatなら同一vbからのterrain生成を許可
            if ((state == 0 && !terrains.Exists(x => x.vb == vb)) ||
                (state == 1 && terrains.Count < mHcount * mDcount * 9))
            {
                var tm = new TerrainMesh("MapTerrain_" + (terrains.Count + 1));
                vb.owner = this;
                tm.vb = vb;
                tm.tex = texture;
                tm.makeTerrainMesh(m_l2w.translation(), color);
                terrains.Add(tm);
            }
            // 範囲外を指定の地形で埋める
            else if (state == 2 && !terrains.Exists(x => x.vb == vb))
            {
                var tic = Yukar.Engine.MapData.TIPS_IN_CLUSTER;
                for (int x = 0; x < mHcount + 2; x++)
                {
                    for (int z = 0; z < mDcount + 2; z++)
                    {
                        if (!(x == 0 || x == mHcount + 1 || z == 0 || z == mDcount + 1)) continue;

                        var tm = new TerrainMesh("MapTerrain_" + (terrains.Count + 1));
                        vb.owner = this;
                        tm.vb = vb;
                        tm.tex = texture;
                        tm.makeTerrainMesh(new SharpKmyMath.Vector3(-tic + x * tic, 0, -tic + z * tic), color);
                        terrains.Add(tm);
                    }
                }
            }
        }

        internal void setVolatileVertex(VertexPositionTextureColor[] vertices)
        {
            if (plane == null)
            {
                var obj = Yukar.Common.UnityUtil.createObject(
                    Yukar.Common.UnityUtil.ParentType.OBJECT, "BillBoard");
                plane = obj.AddComponent<VolatilePremitive>();
                plane.initialize(blendType);
                plane.setTexture(this.texture);
            }

            plane.setVolatileVertex(vertices);
        }

        internal void removeVertexBuffer(VertexBuffer vb)
        {
            while (true)
            {
                var tm = terrains.FirstOrDefault(x => x.vb == vb);
                if (tm != null)
                {
                    tm.release();
                    terrains.Remove(tm);
                }
                else
                {
                    break;
                }
            }
        }

        internal void setIndexCount(int vtxcount)
        {
            indexCount = vtxcount;
            //throw new NotImplementedException();
        }

        internal void setShader(Shader waterShader)
        {
            shader = waterShader;
            //throw new NotImplementedException();
        }

        internal void setCull(CULLTYPE newType)
        {
            cullType = newType;
            //throw new NotImplementedException();
        }

        internal void setVPMatrix(string name, Matrix4 m)
        {
            if (name != "texcoord0")
                return;

            var model = drawable as ModelInstance;
            if (model == null)
                return;

            var mesh = model.template.getMesh(model, indexCount);
            if (mesh == null)
                return;

            Material material = null;
            if (UnityEntry.IsImportMapScene())
            {
                material = mesh.material = new Material(mesh.sharedMaterial);
            }
            else
            {
                material = mesh.material;
            }
            material.SetTextureOffset("_MainTex", new UnityEngine.Vector2(m.m30, m.m31));
        }

        internal void setDepthWrite(bool v)
        {
            depthWrite = v;
            //throw new NotImplementedException();
        }

        internal void setParam(int v)
        {
            param = v;
            //throw new NotImplementedException();
        }

        internal void setTexture(int texIndex, Texture texture)
        {
            if (texIndex == 0)
                this.texture = texture;

            if (plane != null)
                plane.setTexture(texture);
        }

        internal Color getColor()
        {
            return color;
        }
    }

    internal class VolatilePremitive : MonoBehaviour
    {
        public VertexPositionTextureColor[] vb = null;
        private bool visible;
        private MeshFilter filter;
        private MeshRenderer meshRenderer;

        internal void setVolatileVertex(VertexPositionTextureColor[] vertices)
        {
            if (filter.sharedMesh != null)
                Destroy(filter.sharedMesh);

            var vc = vertices.Length;
            List<UnityEngine.Vector3> tmpV = new List<UnityEngine.Vector3>();
            List<UnityEngine.Vector2> tmpU = new List<UnityEngine.Vector2>();
            List<int> indexes = new List<int>();
            var mesh = new Mesh();
            for (int i = 0; i < vc; i++)
            {
                tmpV.Add(new UnityEngine.Vector3(vertices[i].pos.x * (-1), vertices[i].pos.y, vertices[i].pos.z));
                tmpU.Add(new UnityEngine.Vector2(vertices[i].tc.x, vertices[i].tc.y));
                indexes.Add(indexes.Count);
            }
            while (indexes.Count % 3 > 0)
            {
                indexes.RemoveAt(indexes.Count - 1);
            }
            mesh.vertices = tmpV.ToArray();
            mesh.uv = tmpU.ToArray();
            mesh.triangles = indexes.ToArray();
            mesh.RecalculateNormals();
            filter.sharedMesh = mesh;
            visible = true;
        }

        private void Update()
        {
            if (meshRenderer.enabled != visible)
                meshRenderer.enabled = visible;
            visible = false;
        }

        internal void initialize(BLENDTYPE blendType)
        {
            Material material = Resources.Load<Material>("MatForPremultiplied");
            filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = null;
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

            switch (blendType)
            {
                case SharpKmyGfx.BLENDTYPE.kALPHA:
                    material = Resources.Load<Material>("MatForAlpha");
                    break;
                case SharpKmyGfx.BLENDTYPE.kPREMULTIPLIED:
                    material = Resources.Load<Material>("MatForPremultiplied");
                    break;
            }

            meshRenderer.material = material;
            meshRenderer.material.SetFloat("_Mode", 3.0f);  // Opaque, Cutout, Transparent
        }

        internal void setColor(Color color)
        {
            meshRenderer.material.SetColor("_Color", Yukar.Common.UnityUtil.convertToUnityColor(color));
        }

        internal void setTexture(Texture texture)
        {
            meshRenderer.material.mainTexture = texture.obj;
        }
    }

    public class TerrainMesh
    {
        public VertexBuffer vb = new VertexBuffer();
        public Texture tex = new Texture();
        private GameObject obj;
        public GameObject GetGameObject() { return this.obj; }

        public TerrainMesh(string name)
        {
            if (UnityEntry.IsImportMapScene())
                obj = Yukar.Common.UnityUtil.findTerrainObject(
                    int.Parse(name.Substring(name.IndexOf('_') + 1)));

            if(obj == null)
                obj = Yukar.Common.UnityUtil.createObject(
                    Yukar.Common.UnityUtil.ParentType.OBJECT, name);
        }

        internal void makeTerrainMesh(SharpKmyMath.Vector3 offset, Color color)
        {
            var vc = vb.vtxcount;
            //var tic = Yukar.Engine.MapData.TIPS_IN_CLUSTER;

            List<UnityEngine.Vector3> tmpV = new List<UnityEngine.Vector3>();
            List<UnityEngine.Vector2> tmpU = new List<UnityEngine.Vector2>();
            List<Color32> tmpC = new List<Color32>();
            List<int> indexes = new List<int>();
            var mesh = new Mesh();
            mesh.Clear();
            for (int i = 0; i < vc; i++)
            {
                if (vb.buf == null && vb.buf2 != null)
                {
                    tmpV.Add(new UnityEngine.Vector3((vb.buf2[i].pos.x + offset.x) * (-1), vb.buf2[i].pos.y + offset.y, vb.buf2[i].pos.z + offset.z));
                    tmpU.Add(new UnityEngine.Vector2(vb.buf2[i].tc.x, vb.buf2[i].tc.y));
                    tmpC.Add(new Color32(
                        (byte)(vb.buf2[i].color.r * color.r * 255),
                        (byte)(vb.buf2[i].color.g * color.g * 255),
                        (byte)(vb.buf2[i].color.b * color.b * 255),
                        (byte)(vb.buf2[i].color.a * color.a * 255)));
                    indexes.Add(indexes.Count);
                }
                else if (vb.buf2 == null)
                {
                    tmpV.Add(new UnityEngine.Vector3((vb.buf[i].pos.x + offset.x) * (-1), vb.buf[i].pos.y + offset.y, vb.buf[i].pos.z + offset.z));
                    tmpU.Add(new UnityEngine.Vector2(vb.buf[i].tc.x, vb.buf[i].tc.y));
                    tmpC.Add(new Color32(
                        (byte)(vb.buf[i].color.r * color.r * 255),
                        (byte)(vb.buf[i].color.g * color.g * 255),
                        (byte)(vb.buf[i].color.b * color.b * 255),
                        (byte)(vb.buf[i].color.a * color.a * 255)));
                    indexes.Add(indexes.Count);
                }
                else continue;
            }
            while (indexes.Count % 3 > 0)
            {
                indexes.RemoveAt(indexes.Count - 1);
            }
            mesh.vertices = tmpV.ToArray();
            mesh.uv = tmpU.ToArray();
            mesh.colors32 = tmpC.ToArray();
            mesh.triangles = indexes.ToArray();
            mesh.RecalculateNormals();
            if (obj.GetComponent<MeshFilter>() == null) obj.AddComponent<MeshFilter>();
            if (obj.GetComponent<MeshRenderer>() == null) obj.AddComponent<MeshRenderer>();
            var filter = obj.GetComponent<MeshFilter>();
            var renderer = obj.GetComponent<MeshRenderer>();
            //renderer.material.SetFloat("_Mode", 3.0f);  // Opaque, Cutout, Transparent
            //mesh.SetIndices(mesh.GetIndices(0), MeshTopology.Triangles, 0);
            filter.sharedMesh = mesh;
            //mesh.RecalculateBounds();

            // テクスチャの補間モードをニアレストネイバーにしておく
            tex.setMagFilter(SharpKmyGfx.TEXTUREFILTER.kNEAREST);
            // ---

            Material material = Resources.Load<Material>("MapMesh");
            if (UnityEntry.IsImportMapScene())
            {
                material = new Material(material);
                material.mainTexture = tex.obj;
                renderer.material = material;
            }
            else
            {
                //material.mainTexture = tex.obj;#24733
                renderer.material = material;
                renderer.material.mainTexture = tex.obj;
            }
        }

        public void release()
        {
            //UnityEngine.Debug.Log(obj.name);
            if (UnityEntry.IsImportMapScene())
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
            else
            {
                UnityEngine.Object.Destroy(obj);
            }
        }

        internal void setTexture(Texture maptexture)
        {
            tex = maptexture;
            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.material.mainTexture = tex.obj;
        }
    }
}
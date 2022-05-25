using System;
using System.Collections.Generic;

namespace ResourceManager
{
    public class Image
    {
        static List<Data> sData = new List<Data>();
        static Data GetData(File inFile) { return sData[(int)inFile]; }

        public enum File
        {
            None = 0,
            VRCtrlParts,
            VRCtrlPartsB,
        }

        public enum Parts
        {
            None = 0,
            STICK,
            STICK_CENTER,
            CHECK,
            MENU,
            CAMERA,
            DASH,

            SLIDER,
            SLIDER_CENTER,
            FPS,
            RESET,
            PLUS,
            MINUS,
        }

        static public void initialize()
        {
            Data data = null;
            if (0 < sData.Count) return;

            sData.Add(null);
            data = new Data();
            if (data.Load("VRCtrlParts.png", 6, 1)) sData.Add(data);
            data = new Data();
            if (data.Load("VRCtrlPartsB.png", 12, 1)) sData.Add(data);
        }

        static public void finalize()
        {
            foreach (var image in sData) image.Unload();
        }

        static public SharpKmyMath.Vector2 getSize(Parts inParts)
        {
            Data data = null;
            SharpKmyMath.Vector2 pos = new SharpKmyMath.Vector2(0, 0);
            SharpKmyMath.Vector2 size = new SharpKmyMath.Vector2(0, 0);
            SharpKmyMath.Vector2 scale = new SharpKmyMath.Vector2(0, 0);
            SharpKmyMath.Vector4 color = new SharpKmyMath.Vector4(0, 0, 0, 0);
            getChip(inParts, ref data, ref pos, ref size, ref scale, ref color);

            return new SharpKmyMath.Vector2(size.x * data.Size.x * scale.x, size.y * data.Size.y * scale.y);
        }

        static public SharpKmyMath.Vector4 getColor(Parts inParts)
        {
            Data data = null;
            SharpKmyMath.Vector2 pos = new SharpKmyMath.Vector2(0, 0);
            SharpKmyMath.Vector2 size = new SharpKmyMath.Vector2(0, 0);
            SharpKmyMath.Vector2 scale = new SharpKmyMath.Vector2(0, 0);
            SharpKmyMath.Vector4 color = new SharpKmyMath.Vector4(0, 0, 0, 0);
            getChip(inParts, ref data, ref pos, ref size, ref scale, ref color);

            return color;
        }

        static void getChip(Parts inParts, ref Data outData,
            ref SharpKmyMath.Vector2 outPos, ref SharpKmyMath.Vector2 outSize, ref SharpKmyMath.Vector2 outScale,
            ref SharpKmyMath.Vector4 outColor)
        {
            var VRCtrlPartsColor = new SharpKmyMath.Vector4(255, 255, 255, 255 * 0.4f);
            var VRCtrlPartsBScale = 0.75f;

            outData = null;
            outPos = new SharpKmyMath.Vector2(0, 0);
            outSize = new SharpKmyMath.Vector2(1, 1);
            outScale = new SharpKmyMath.Vector2(1, 1);
            outColor = new SharpKmyMath.Vector4(255, 255, 255, 255);
            switch (inParts)
            {
                case Parts.STICK:
                    outData = GetData(File.VRCtrlParts);
                    outPos.x = 0;
                    outColor = VRCtrlPartsColor;
                    break;
                case Parts.STICK_CENTER:
                    outData = GetData(File.VRCtrlParts);
                    outPos.x = 4;
                    outColor = VRCtrlPartsColor;
                    break;
                case Parts.CHECK:
                    outData = GetData(File.VRCtrlParts);
                    outPos.x = 1;
                    outColor = VRCtrlPartsColor;
                    break;
                case Parts.MENU:
                    outData = GetData(File.VRCtrlParts);
                    outPos.x = 2;
                    outColor = VRCtrlPartsColor;
                    break;
                case Parts.CAMERA:
                    outData = GetData(File.VRCtrlParts);
                    outPos.x = 3;
                    outColor = VRCtrlPartsColor;
                    break;
                case Parts.DASH:
                    outData = GetData(File.VRCtrlParts);
                    outPos.x = 5;
                    outColor = VRCtrlPartsColor;
                    break;

                case Parts.SLIDER:
                    outData = GetData(File.VRCtrlPartsB);
                    outPos.x = 0;
                    outSize.x = 4;
                    outScale *= VRCtrlPartsBScale;
                    outScale.x *= 0.8f;
                    outColor = VRCtrlPartsColor;
                    break;
                case Parts.SLIDER_CENTER:
                    outData = GetData(File.VRCtrlPartsB);
                    outPos.x = 4;
                    outSize.x = 2;
                    outScale *= VRCtrlPartsBScale;
                    outColor = VRCtrlPartsColor;
                    break;
                case Parts.FPS:
                    outData = GetData(File.VRCtrlPartsB);
                    outPos.x = 6;
                    outSize.x = 2;
                    outScale *= VRCtrlPartsBScale;
                    outColor = VRCtrlPartsColor;
                    break;
                case Parts.RESET:
                    outData = GetData(File.VRCtrlPartsB);
                    outPos.x = 8;
                    outSize.x = 2;
                    outScale *= VRCtrlPartsBScale;
                    outColor = VRCtrlPartsColor;
                    break;
                case Parts.MINUS:
                    outData = GetData(File.VRCtrlPartsB);
                    outPos.x = 10;
                    outScale *= VRCtrlPartsBScale;
                    outColor = VRCtrlPartsColor;
                    break;
                case Parts.PLUS:
                    outData = GetData(File.VRCtrlPartsB);
                    outPos.x = 11;
                    outScale *= VRCtrlPartsBScale;
                    outColor = VRCtrlPartsColor;
                    break;
            }
        }

        static public void draw(Parts inParts, SharpKmyMath.Vector3 inPos, float inAngle = 0, SharpKmyMath.Vector4 inColor = null)
        {
            if (inParts == Parts.None) return;
            Data data = null;
            SharpKmyMath.Vector2 pos = new SharpKmyMath.Vector2(0, 0);
            SharpKmyMath.Vector2 size = new SharpKmyMath.Vector2(0, 0);
            SharpKmyMath.Vector2 scale = new SharpKmyMath.Vector2(0, 0);
            SharpKmyMath.Vector4 color = new SharpKmyMath.Vector4(0, 0, 0, 0);
            getChip(inParts, ref data, ref pos, ref size, ref scale, ref color);
            if (inColor != null) color = inColor;
            Yukar.Engine.Graphics.DrawChipImage(data.Id,
                (int)inPos.x, (int)inPos.y,
                (int)(data.Size.x * size.x * scale.x), (int)(data.Size.y * size.y * scale.y),
                (int)pos.x, (int)pos.y,
                (byte)(color.x * color.w / 255),    // 乗算済みアルファを使うように変更した場合を想定しておく
                (byte)(color.y * color.w / 255),
                (byte)(color.z * color.w / 255), (byte)color.w,
                (int)inAngle, (int)inPos.z,
                (int)size.x, (int)size.y);

        }

        //////////////////////////////
        //Data
        //////////////////////////////
        class Data
        {
            int mId = -1;
            public int Id { get { return this.mId; } }
            SharpKmyMath.Vector2 mSize = new SharpKmyMath.Vector2(0, 0);
            public SharpKmyMath.Vector2 Size { get { return this.mSize; } }

            public bool Load(string inFileName, int inDivX, int inDivY)
            {
                this.Unload();
                this.mId = Yukar.Engine.Graphics.LoadImageDiv(inFileName, inDivX, inDivY);
                if (this.mId < 0) return false;
                this.mSize.x = Yukar.Engine.Graphics.GetDivWidth(this.mId);
                this.mSize.y = Yukar.Engine.Graphics.GetDivHeight(this.mId);
                return true;
            }

            public void Unload()
            {
                if (this.mId < 0) return;
                Yukar.Engine.Graphics.UnloadImage(this.mId);
                this.mId = -1;
            }
        }
    }
}
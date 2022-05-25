using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
namespace Yukar.Common.GameData
{
    public class Sprite : IGameDataItem
    {
        public enum SpriteType
        {
            PICTURE,
            TEXT,
            RECT,
        }

        public int index;
        public SpriteType type;
        public Guid guid;
        public int zoomX, zoomY;
        public Microsoft.Xna.Framework.Color color;
        public byte align;
        public int x;
        public int y;
        public bool visible = true;
        public int faceType;
        public string text = "";

        public void save(BinaryWriter writer)
        {
            writer.Write(index);
            writer.Write((int)type);
            writer.Write(guid.ToByteArray());
            writer.Write(zoomX);
            writer.Write(color.PackedValue);
            writer.Write(align);
            writer.Write(x);
            writer.Write(y);
            writer.Write(visible);
            writer.Write(faceType);
            writer.Write(text);
            writer.Write(zoomY);
        }

        public void load(Catalog catalog, BinaryReader reader)
        {
            index = reader.ReadInt32();
            type = (SpriteType)reader.ReadInt32();
            guid = Util.readGuid(reader);
            zoomX = zoomY = reader.ReadInt32();
            color.PackedValue = reader.ReadUInt32();
            align = reader.ReadByte();
            x = reader.ReadInt32();
            y = reader.ReadInt32();
            visible = reader.ReadBoolean();
            faceType = reader.ReadInt32();
            text = reader.ReadString();

            if(reader.BaseStream.Position < reader.BaseStream.Length)
                zoomY = reader.ReadInt32();
        }
    }
}

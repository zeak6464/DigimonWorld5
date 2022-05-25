using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yukar.Common.Resource
{
    public class Window : ResourceItem
    {
        public enum FillType
        {
            FILL_STREATCH,
            FILL_REPEAT,
        }

        public FillType fillType = FillType.FILL_STREATCH;
        public int top = 0;
        public int bottom = 0;
        public int left = 0;
        public int right = 0;

        public override void save(System.IO.BinaryWriter writer)
        {
            base.save(writer);

            writer.Write(top);
            writer.Write(bottom);
            writer.Write(left);
            writer.Write(right);
            writer.Write((int)fillType);
        }

        public override void load(System.IO.BinaryReader reader)
        {
            base.load(reader);

            top = reader.ReadInt32();
            bottom = reader.ReadInt32();
            left = reader.ReadInt32();
            right = reader.ReadInt32();
            fillType = (FillType)reader.ReadInt32();
        }
    }
}

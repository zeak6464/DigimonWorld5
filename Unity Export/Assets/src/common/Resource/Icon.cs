using Microsoft.Xna.Framework;
using System;

namespace Yukar.Common.Resource
{
    public class Icon : ResourceItem
    {
        public const int ICON_WIDTH = 32;
        public const int ICON_HEIGHT = 32;

        // 参照用
        public class Ref
        {
            public Guid guId;
            public byte x;
            public byte y;

            public Ref(System.IO.BinaryReader reader)
            {
                load(reader);
            }

            public Ref()
            {
            }

            public void save(System.IO.BinaryWriter writer)
            {
                writer.Write(guId.ToByteArray());
                writer.Write(x);
                writer.Write(y);
            }

            public void load(System.IO.BinaryReader reader)
            {
                guId = Util.readGuid(reader);
                x = reader.ReadByte();
                y = reader.ReadByte();
            }

#if WINDOWS
            public System.Drawing.Rectangle getDrawingRect()
            {
                return new System.Drawing.Rectangle(x * ICON_WIDTH, y * ICON_HEIGHT, ICON_WIDTH, ICON_HEIGHT);
            }
#endif

            public Rectangle getRect()
            {
                return new Rectangle(x * ICON_WIDTH, y * ICON_HEIGHT, ICON_WIDTH, ICON_HEIGHT);
            }
        }

        public override void save(System.IO.BinaryWriter writer)
        {
            base.save(writer);
        }

        public override void load(System.IO.BinaryReader reader)
        {
            base.load(reader);
        }

        public override ErrorType verify()
        {
#if WINDOWS
            var bmp = System.Drawing.Bitmap.FromFile(path);
            if (bmp.Width % ICON_WIDTH > 0 || bmp.Height % ICON_HEIGHT > 0)
            {
                bmp.Dispose();
                return ErrorType.INVALID_ICON;
            }

            bmp.Dispose();
#endif
            return ErrorType.NONE;
        }
    }
}

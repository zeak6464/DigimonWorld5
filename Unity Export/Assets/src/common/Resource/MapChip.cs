using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Yukar.Common.Resource
{
    public enum ChipType
    {
        TERRAIN,
        STAIR_OR_SLOPE,
    }

    public class MapChip : ResourceItem
    {
        public const int MAPCHIP_GRID_SIZE = 48;

        public ChipType type;
        public string category;
        public int _index;   // 互換用
        public bool squareShape;
        public bool walkable;
        public bool liquid;
        public bool wave;
        public bool stair;
        public bool slope;
        public bool poison;

        public MapChip()
        {
            getChipInfo();
        }

        internal override bool isOptionMatched(string option)
        {
            return category == option;
        }

        public override void setPath(string path)
        {
            base.setPath(path);

            // カテゴリを確定する
            category = Util.file.getDirName(path).Split(Path.DirectorySeparatorChar).Last() + (isSystemResource() ? "_sys" : "");
        }

        private void getChipInfo()
        {
            string fname = Util.file.getFileName(path);
            this.walkable = true;

            int begin = fname.IndexOf('_');
            if (begin < 0) return;
            int end = fname.IndexOf('_', begin + 1);
            if (end < 0) end = fname.IndexOf('.', begin + 1);

            string number = fname.Substring(0, begin);
            bool isNumberPrefix = false;
            if (number.Length > 0)
            {
                isNumberPrefix = int.TryParse(number, out _index);
            }
            if (!isNumberPrefix)
            {
                end = begin;
                begin = -1;
            }
            if (category == "artifact")
                _index += 100;
            string name = fname.Substring(begin + 1, end - begin - 1);
            if (name.Length == 0) return;

            this.name = name;
            this.type = ChipType.TERRAIN;
            this.walkable = !(fname.IndexOf("_uw", begin + 1) >= 0);
            this.squareShape = (fname.IndexOf("_sq", begin + 1) >= 0);
            this.liquid = (fname.IndexOf("_lq", begin + 1) >= 0);
            this.wave = (fname.IndexOf("_wv", begin + 1) >= 0);
            this.stair = (fname.IndexOf("_st", begin + 1) >= 0);
            this.slope = (fname.IndexOf("_sl", begin + 1) >= 0);

            if (stair || slope)
                type = ChipType.STAIR_OR_SLOPE;
        }

        public override void save(System.IO.BinaryWriter writer)
        {
            base.save(writer);

            byte flags = 0;
            if (walkable) flags |= 0x1;
            if (squareShape) flags |= 0x2;
            if (liquid) flags |= 0x4;
            if (wave) flags |= 0x8;
            if (stair) flags |= 0x10;
            if (slope) flags |= 0x20;
            if (poison) flags |= 0x40;
            writer.Write(flags);
        }

        public override void load(System.IO.BinaryReader reader)
        {
            base.load(reader);

            // プロパティがまだない頃のロムだったら、ファイル名から生成する
            if (reader.BaseStream.Position == reader.BaseStream.Length)
            {
                getChipInfo();
            }
            else
            {
                var flags = reader.ReadByte();
                walkable = (flags & 0x1) != 0;
                squareShape = (flags & 0x2) != 0;
                liquid = (flags & 0x4) != 0;
                wave = (flags & 0x8) != 0;
                stair = (flags & 0x10) != 0;
                slope = (flags & 0x20) != 0;
                poison = (flags & 0x40) != 0;
                if (stair || slope)
                    type = ChipType.STAIR_OR_SLOPE;
                else
                    type = ChipType.TERRAIN;
            }
        }

        public override ErrorType verify()
        {
#if WINDOWS
            var bmp = System.Drawing.Bitmap.FromFile(path);
            if (bmp.Width % MAPCHIP_GRID_SIZE > 0 || bmp.Height % MAPCHIP_GRID_SIZE > 0)
            {
                bmp.Dispose();
                return ErrorType.INVALID_MAPCHIP;
            }

            bmp.Dispose();
#endif
            return ErrorType.NONE;
        }
    }
}

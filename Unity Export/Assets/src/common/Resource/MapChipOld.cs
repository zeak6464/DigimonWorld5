using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Yukar.Common.Resource
{
    public enum ChipTypeOld
    {
        TERRAIN,
        ARTIFACT,
    }

    public class ChipItemInfoOld
    {
        public string path;
        public string name;
        public int index;
        public bool squareShape;
        public bool walkable;
        public bool liquid;
        public bool wave;
        public ChipTypeOld type;
    }

    public class StairItemInfoOld
    {
        public string path;
        public string name;
        public int index;
        public bool stair;
    }

    public class MapChipOld : ResourceItem
    {
        public const int MAPCHIP_NUM_X = 40;
        public const int MAPCHIP_GRID_SIZE = 48;
        public const int MAPCHIP_BANK_NUM_X = 8;
        public const int MAPCHIP_NUM_Y = 40;
        public const int MAPCHIP_COLLISION_ON = Util.DIR_UP | Util.DIR_DOWN | Util.DIR_LEFT | Util.DIR_RIGHT;
        public const int MAPCHIP_DIRCOLLISION_ON = MAPCHIP_COLLISION_ON + 1;    // 16になるはず

        public byte[] attributes;

        public MapChipOld()
        {
            attributes = new byte[MAPCHIP_NUM_X * MAPCHIP_NUM_Y];
        }

        public override void save(System.IO.BinaryWriter writer)
        {
            base.save(writer);

            writer.Write((int)attributes.Length);   // 長さを書き込む
            writer.Write(attributes);               // データを書き込む
        }

        public override void load(System.IO.BinaryReader reader)
        {
            base.load(reader);

            var length = reader.ReadInt32();
            attributes = new byte[MAPCHIP_NUM_X * MAPCHIP_NUM_Y];
            for (int i = 0; i < length; i++)
            {
                attributes[i] = reader.ReadByte();
            }
        }

        public List<ChipItemInfoOld> getChipPathList()
        {
            List<ChipItemInfoOld> ret = new List<ChipItemInfoOld>();

            searchChips(ret, 0, path + "\\terrain", ChipTypeOld.TERRAIN);
            searchChips(ret, 100, path + "\\artifact", ChipTypeOld.ARTIFACT);

            return ret;
        }

        private void searchChips(List<ChipItemInfoOld> ret, int indexOffset, string char_path, ChipTypeOld chipType)
        {
            DirectoryInfo current = new DirectoryInfo(char_path);

            if (current.Exists)
            {
                foreach (FileInfo fileInfo in current.GetFiles())
                {
                    string e = fileInfo.Extension.ToUpper();
                    string rpath = char_path + "\\" + fileInfo.Name;

                    if (e == ".PNG")
                    {
                        string fname = Util.file.getFileName(rpath);

                        int begin = fname.IndexOf('_');
                        if (begin < 0) continue;
                        int end = fname.IndexOf('_', begin + 1);
                        if (end < 0) end = fname.IndexOf('.', begin + 1);

                        string name = fname.Substring(begin + 1, end - begin - 1);
                        if (name.Length == 0) continue;

                        string number = fname.Substring(0, begin);
                        int index = 0;
                        if (number.Length > 0)
                        {
                            try
                            {
                                index = int.Parse(number) + indexOffset;
                            }
                            catch (Exception)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }

                        ret.Add(new ChipItemInfoOld());
                        ret.Last().index = index;
                        ret.Last().name = name;
                        ret.Last().path = rpath;
                        ret.Last().type = chipType;
                        ret.Last().walkable = !(fname.IndexOf("_uw", begin + 1) >= 0);
                        ret.Last().squareShape = (fname.IndexOf("_sq", begin + 1) >= 0);
                        ret.Last().liquid = (fname.IndexOf("_lq", begin + 1) >= 0);
                        ret.Last().wave = (fname.IndexOf("_wv", begin + 1) >= 0);
                    }
                }
            }
        }

        public List<StairItemInfoOld> getStairPathList()
        {
            DirectoryInfo current;
            string char_path = path + "\\stair";
            current = new DirectoryInfo(char_path);

            List<StairItemInfoOld> ret = new List<StairItemInfoOld>();

            if (current.Exists)
            {
                foreach (FileInfo fileInfo in current.GetFiles())
                {
                    string e = fileInfo.Extension.ToUpper();
                    string rpath = char_path + "\\" + fileInfo.Name;

                    if (e == ".PNG")
                    {
                        string fname = Util.file.getFileName(rpath);

                        int begin = fname.IndexOf('_');
                        if (begin < 0) continue;
                        int end = fname.IndexOf('_', begin + 1);
                        if (end < 0) end = fname.IndexOf('.', begin + 1);

                        string name = fname.Substring(begin + 1, end - begin - 1);
                        if (name.Length == 0) continue;

                        string number = fname.Substring(0, begin);
                        int index = 0;
                        if (number.Length > 0)
                        {
                            try
                            {
                                index = int.Parse(number);
                            }
                            catch (Exception)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }

                        ret.Add(new StairItemInfoOld());
                        ret.Last().index = index;
                        ret.Last().name = name;
                        ret.Last().path = rpath;
                        ret.Last().stair = (fname.IndexOf("_st", begin + 1) >= 0);
                    }
                }
            }

            return ret;
        }

    }
}

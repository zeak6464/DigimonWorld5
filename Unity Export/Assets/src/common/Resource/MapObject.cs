using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Yukar.Common.Resource
{
    public class MapObject : ResourceItem
    {
		public int heightMapHalfSize = 32;
        public struct CollisionInfo
        {
            public int height;
            public bool isRoof;
        }
        public CollisionInfo[,] collisionMap = null;

		public int maxx, maxz, maxy;
		public int minx, minz;

		public bool isAnimated = true;
		public bool isBillboard = false;
        public bool isFoothold = false;

        public string category = "";

		public MapObject()
		{
            collisionMap = new CollisionInfo[heightMapHalfSize * 2, heightMapHalfSize * 2];
			minx = 0; maxx = 0;
			minz = 0; maxz = 0;
			maxy = 1;
        }

        internal override bool isOptionMatched(string option)
        {
            return category == option;
        }

        public override void setPath(string path)
        {
            base.setPath(path);

            // カテゴリを確定する
            category = Common.Util.file.getDirName(path).Split(Path.DirectorySeparatorChar).Last() + (isSystemResource() ? "_sys" : "");
        }

		public override void load(System.IO.BinaryReader reader)
		{
			base.load(reader);
			heightMapHalfSize = reader.ReadInt32();
            collisionMap = new CollisionInfo[heightMapHalfSize * 2, heightMapHalfSize * 2];

            if (Catalog.sRomVersion < 2)
            {
                for (int x = 0; x < heightMapHalfSize * 2; x++)
                {
                    for (int y = 0; y < heightMapHalfSize * 2; y++)
                    {
                        collisionMap[x, y].height = reader.ReadInt32();
                    }
                }
            }
            else
            {
                int count = reader.ReadInt16();
                for (int i = 0; i < count; i++)
                {
                    int x = reader.ReadInt16();
                    int y = reader.ReadInt16();
                    var info = reader.ReadInt32();
                    collisionMap[x, y].height = info & 0x0000FFFF;
                    collisionMap[x, y].isRoof = (info & 0x00010000) > 0;
                }
            }

			updateMinMax();

			var type = reader.ReadByte();  // タイプが入っていたが、読み飛ばす
            if (type == 1)
                isFoothold = true;

			isAnimated = reader.ReadBoolean();
			isBillboard = reader.ReadBoolean();
            isFoothold = reader.ReadBoolean();
		}

		public override void save(System.IO.BinaryWriter writer)
		{
			base.save(writer);

			writer.Write(heightMapHalfSize);

            short count = 0;
            foreach (var info in collisionMap)
            {
                if (info.height != 0)
                    count++;
            }

            writer.Write(count);
            for (short x = 0; x < heightMapHalfSize * 2; x++)
			{
                for (short y = 0; y < heightMapHalfSize * 2; y++)
				{
                    if (collisionMap[x, y].height != 0)
                    {
                        writer.Write(x);
                        writer.Write(y);
                        writer.Write(collisionMap[x, y].height | (collisionMap[x, y].isRoof ? 0x00010000 : 0));
                    }
				}
			}

			writer.Write((byte)0);
			writer.Write(isAnimated);
            writer.Write(isBillboard);
            writer.Write(isFoothold);
		}


		public void updateMinMax()
		{
			bool found = false;
			minz = minx = heightMapHalfSize;
			maxz = maxx = 0;
			maxy = 0;

			for (int x = 0; x < heightMapHalfSize * 2; x++)
			{
				for (int z = 0; z < heightMapHalfSize * 2; z++)
				{
                    int h = collisionMap[x, z].height;
					if (h > 0)
					{
						found = true;
						if (minx > x) minx = x;
						if (minz > z) minz = z;
						if (maxx < x) maxx = x;
						if (maxz < z) maxz = z;
						if (maxy < h) maxy = h;
					}
					
				}
			}

			if (!found)
			{
				minx = maxx = minz = maxz = 0;
				maxy = 1;
			}
			else
			{
				minx -= heightMapHalfSize;
				maxx -= heightMapHalfSize;
				minz -= heightMapHalfSize;
				maxz -= heightMapHalfSize;
			}

		}

        public override List<string> getRelatedFiles()
        {
            var result = new List<string>();
            result.Add(path);
            result.Add(Path.ChangeExtension(path, "def"));

            var dir = Common.Util.file.getDirName(path);

            // 全モーションを追加する
            var motion = dir + Path.DirectorySeparatorChar + "motion";
            if (Util.file.dirExists(motion))
                result.AddRange(Util.file.getFiles(motion));

#if WINDOWS
            // 必要なテクスチャを追加する
            var model = SharpKmyGfx.ModelData.load(path);
            if (model != null)
            {
                var texList = model.getTextureNameList();
                if (texList != null)
                {
                    foreach (var texPathOrig in texList)
                    {
                        var texFileName = Util.file.getFileName(texPathOrig);
                        texFileName = Path.ChangeExtension(texFileName, "png");

                        var texDir = dir + Path.DirectorySeparatorChar + "texture" + Path.DirectorySeparatorChar;
                        if (File.Exists(texDir + texFileName))
                        {
                            Console.WriteLine("texture export : " + texDir + texFileName);
                            result.Add(texDir + texFileName);
                            continue;
                        }

                        texDir = Common.Util.file.getDirName(dir) + Path.DirectorySeparatorChar + "texture" + Path.DirectorySeparatorChar;
                        if (File.Exists(texDir + texFileName))
                        {
                            Console.WriteLine("texture export : " + texDir + texFileName);
                            result.Add(texDir + texFileName);
                            continue;
                        }
                    }
                }
                model.Release();
                model = null;
            }
#endif

            /*
            var threeD = "mapobject";
            if (path.LastIndexOf(threeD) > 0)
            {
                threeD = path.Substring(0, path.LastIndexOf(threeD) + threeD.Length);
            }
            else
            {
                threeD = Common.Util.file.getDirName(path);
            }
            var dirs = Util.file.getDirectories(threeD, "texture", SearchOption.AllDirectories);
            foreach (var dir in dirs)
            {
                result.AddRange(Util.file.getFiles(dir));
            }*/

            return result;
        }

        public override ErrorType verify()
        {
            return getModelError(path);
        }

        internal static ErrorType getModelError(string path)
        {
#if WINDOWS
            // モデルを仮読み込みして、必要なテクスチャがあるか調べる
            var model = SharpKmyGfx.ModelData.load(path);
            if (model != null)
            {
                var texList = model.getTextureNameList();
                model.Release();
                model = null;

                if (texList != null)
                {
                    foreach (var texPathOrig in texList)
                    {
                        var fullPath = Path.GetFullPath(path);
                        var srcDir = fullPath.Substring(0, fullPath.LastIndexOf("\\res\\"));
                        var texFileName = Util.file.getFileName(texPathOrig);
                        texFileName = Path.ChangeExtension(texFileName, "png");
                        var texPath = Util.file.getFiles(srcDir, texFileName, SearchOption.AllDirectories);
                        if (texPath.Length == 0)
                        {
                            // テクスチャが見つからない
                            return ErrorType.TEXTURE_NOT_FOUND;
                        }
                    }
                }
                return ErrorType.NONE;
            }
            else
            {
                return ErrorType.INVALID_MODEL_FORMAT;
            }
#else
            return ErrorType.NONE;
#endif
        }
    }
}

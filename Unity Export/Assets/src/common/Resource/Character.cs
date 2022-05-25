using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Yukar.Common.Resource
{
    public class Character2DAnimSet : ResourceItem
    {
        public enum DirectionType
        {
            DIRTYPE_SINGLE,
            DIRTYPE_4,
            DIRTYPE_8,
        }

        public enum DisplayType
        {
            BILLBOARD,
            MODEL,
            PARTICLE,
        }

        public enum AnimationType
        {
            ANIMTYPE_SINGLE,
            ANIMTYPE_REVERSE,
            ANIMTYPE_LOOP,
        }

        public int animationFrame = 2;
        public int animationSpeed = 500;
        public DirectionType directionType = DirectionType.DIRTYPE_4;
        public AnimationType animationType = AnimationType.ANIMTYPE_REVERSE;

        public override void save(BinaryWriter writer)
        {
            base.save(writer);

            writer.Write(animationFrame);
            writer.Write(animationSpeed);
            writer.Write((int)directionType);
            writer.Write((int)animationType);
        }

        public override void load(BinaryReader reader)
        {
            base.load(reader);

            // 素材お引越し用
            /*
            if (path.IndexOf("res\\character") >= 0)
            {
                var type = getDisplayType();
                var typeStr = "\\2D";
                if(type != DisplayType.BILLBOARD)
                    typeStr = "\\3D";

                path = path.Replace("res\\character", "res\\character" + typeStr);
            }
            */
            animationFrame = reader.ReadInt32();
            animationSpeed = reader.ReadInt32();
            directionType = (DirectionType)reader.ReadInt32();
            animationType = (AnimationType)reader.ReadInt32();
        }

        public int getDivY()
        {
            switch (directionType)
            {
                case DirectionType.DIRTYPE_SINGLE:
                    return 1;
                case DirectionType.DIRTYPE_4:
                    return 4;
                case DirectionType.DIRTYPE_8:
                    return 8;
            }

            return 1;
        }
    }

    public class Character : Character2DAnimSet
    {
        private bool loading = false;
        public List<Character2DAnimSet> subItemList = new List<Character2DAnimSet>();
        public float resolution = 48;

        public override void save(System.IO.BinaryWriter writer)
        {
            // もし親がCUSTOMIZEDだったら子すべてを変更したことにする
            if (source == ResourceSource.RES_SYSTEM_CUSTOMIZED)
            {
                foreach (var subItem in subItemList)
                {
                    subItem.source = ResourceSource.RES_SYSTEM_CUSTOMIZED;
                }
            }

            base.save(writer);

            writer.Write(subItemList.Count);
            foreach (var subItem in subItemList)
            {
                writeChunk(writer, subItem);
            }

            writer.Write(resolution);
        }

        public override void load(System.IO.BinaryReader reader)
        {
            loading = true;
            base.load(reader);
            loading = false;

            try
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        var subItem = new Character2DAnimSet();
                        readChunk(reader, subItem);
                        subItemList.Add(subItem);
                    }
                    catch (FileNotFoundException e)
                    {
                        // ファイルがない時はサブアイテムをリストから外す
                        Console.WriteLine("Not Found : " + e.FileName);
                    }
                }
            }
            catch (EndOfStreamException)
            {
                // 何もしない
            }

            resolution = reader.ReadSingle();

            searchSubItems();
        }

        public override void setPath(string path)
        {
            base.setPath(path);

            if(!loading)
                searchSubItems();
        }

        public void searchSubItems()
        {
            if (isSystemResource() || getDisplayType() != DisplayType.BILLBOARD)
                return;

            // エディタの時はサブアイテムがあるかどうかを検索する
            //(引数のpathはbase.setPathでフルパスに変換する前の状態なので使わない事)
            var fullPath = Util.file.getDirName(path);
            var prefix = Util.file.getFileNameWithoutExtension(path) + "_";
            var entries = Util.file.getFiles(fullPath, prefix + "*.png", SearchOption.TopDirectoryOnly);

            foreach (var entry in entries)
            {
                if (subItemList.Exists(x => Path.GetFullPath(x.path).ToUpper() == Path.GetFullPath(entry).ToUpper()))
                    continue;

                var subItem = new Character2DAnimSet();
                subItem.name = Util.file.getFileNameWithoutExtension(entry).Substring(prefix.Length);
                subItem.source = source;
                subItem.animationFrame = animationFrame;
                subItem.animationSpeed = animationSpeed;
                subItem.animationType = animationType;
                subItem.directionType = directionType;
                subItem.setPath(entry);
                subItemList.Add(subItem);
            }
        }

        public DisplayType getDisplayType()
        {
            if (System.IO.Path.GetExtension(path).ToUpper() == ".FBX")
            {
                return DisplayType.MODEL;
            }
            else if (System.IO.Path.GetExtension(path).ToUpper() == ".PTCL")
            {
                return DisplayType.PARTICLE;
            }
            else
            {
                return DisplayType.BILLBOARD;
            }
        }

        public override List<string> getRelatedFiles()
        {
            var result = new List<string>();

            switch (getDisplayType()) { 
                case DisplayType.BILLBOARD:
                    return getPathList().ToList();//base.getRelatedFiles();
                case DisplayType.MODEL:
#if WINDOWS
                    result.Add(path);
                    result.Add(Path.ChangeExtension(path, "def"));

                    var dir = Common.Util.file.getDirName(path);

                    // 全モーションを追加する
                    var motion = dir + Path.DirectorySeparatorChar + "motion";
                    if (Util.file.dirExists(motion))
                        result.AddRange(Util.file.getFiles(motion));

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
                                var texFileNameAmbient = Util.file.getFileNameWithoutExtension(texPathOrig) + "_ambient.png";

                                var texDir = dir + Path.DirectorySeparatorChar + "texture" + Path.DirectorySeparatorChar;
                                if (File.Exists(texDir + texFileName))
                                {
                                    Console.WriteLine("texture export : " + texDir + texFileName);
                                    result.Add(texDir + texFileName);
                                    result.Add(texDir + texFileNameAmbient);
                                    continue;
                                }

                                texDir = Common.Util.file.getDirName(dir) + Path.DirectorySeparatorChar + "texture" + Path.DirectorySeparatorChar;
                                if (File.Exists(texDir + texFileName))
                                {
                                    Console.WriteLine("texture export : " + texDir + texFileName);
                                    result.Add(texDir + texFileName);
                                    result.Add(texDir + texFileNameAmbient);
                                    continue;
                                }
                            }
                        }
                        model.Release();
                        model = null;
                    }
#endif
                    return result;
                case DisplayType.PARTICLE:
                    {
                        result.Add(path);
                        var ptclDir = Common.Util.file.getDirName(path);
                        var texList = Util.getParticleTextureNameList(path);

                        foreach (var texFileName in texList)
                        {
                            var texDir = ptclDir + Path.DirectorySeparatorChar + "texture" + Path.DirectorySeparatorChar;
                            if (File.Exists(texDir + texFileName))
                            {
                                Console.WriteLine("texture export : " + texDir + texFileName);
                                result.Add(texDir + texFileName);
                                continue;
                            }

                            texDir = Common.Util.file.getDirName(ptclDir) + Path.DirectorySeparatorChar + "texture" + Path.DirectorySeparatorChar;
                            if (File.Exists(texDir + texFileName))
                            {
                                Console.WriteLine("texture export : " + texDir + texFileName);
                                result.Add(texDir + texFileName);
                                continue;
                            }
                        }
                    }
                    return result;
            }

            return null;
        }

        public override string[] getPathList()
        {
            if (subItemList.Count == 0)
                return base.getPathList();

            var pathList = subItemList.Select(x => x.path).ToList();
            pathList.Insert(0, path);
            return pathList.ToArray();
        }

        public bool containsMotions(string currentMotion)
        {
            throw new NotImplementedException();
        }

        public override ErrorType verify()
        {
#if WINDOWS
            if (Path.GetExtension(path).ToUpper() != ".FBX")
                return ErrorType.NONE;

            return MapObject.getModelError(path);
#else
            return ErrorType.NONE;
#endif
        }
    }
}

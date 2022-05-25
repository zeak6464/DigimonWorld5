using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Yukar.Common.Resource
{
    public class Item : ResourceItem
    {
        public override void save(System.IO.BinaryWriter writer)
        {
            base.save(writer);
        }

        public override void load(System.IO.BinaryReader reader)
        {
            base.load(reader);
        }

        public override List<string> getRelatedFiles()
        {
            var result = new List<string>();
            result.Add(path);
            result.Add(Path.ChangeExtension(path, "def"));
            
#if WINDOWS
            var dir = Common.Util.file.getDirName(path);
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

            return result;
        }
    }
}

using System;
using System.IO;
using MotionInfo = Yukar.Engine.ModifiedModelData.MotionInfo;
using DEFREADMODE = Yukar.Engine.ModifiedModelData.DEFREADMODE;
using System.Collections.Generic;
using System.Linq;

public class MaterialInfo
{
    public string name;
    public int blend;
    public string shader;
}

internal class TinyDefLoader
{
    public List<MotionInfo> motions = new List<MotionInfo>();
    public Dictionary<string, MaterialInfo> materials = new Dictionary<string, MaterialInfo>();
    private string mtlname;

    public TinyDefLoader(string defPath)
    {
        parse(defPath);
    }
    
    public void parse(string defpath)
    {
        if (File.Exists(defpath))
        {
            var reader = new StreamReader(defpath);
            DEFREADMODE mode = DEFREADMODE.kNONE;

            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                string[] words = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (words.Length == 0) continue;
                if (words[0] == "anim")
                {
                    mode = DEFREADMODE.kANIM;
                }
                else if (words[0] == "mtl" && words.Length >= 2)
                {
                    mode = DEFREADMODE.kMTL;
                    mtlname = words[1];
                    if (mtlname.IndexOf("Material::") == 0)
                    {
                        mtlname = mtlname.Substring(10);
                    }

                    var mtl = new MaterialInfo();
                    mtl.name = mtlname;
                    materials.Add(mtlname, mtl);
                }
                else if (words[0] == "preview")
                {
                    mode = DEFREADMODE.kPREVIEW;
                }
                else
                {
                    switch (mode)
                    {
                        case DEFREADMODE.kANIM:
                            Func<int, string> getWord = null;
                            getWord = (int index) =>
                            {
                                if (words.Length <= index)
                                {
                                    switch (index)
                                    {
                                        case 1:
                                            return "0";
                                        case 2:
                                            return getWord(1);
                                        case 3:
                                        default:
                                            return "";
                                    }
                                }
                                return words[index];
                            };
                            if (words.Length >= 2)
                            {
                                motions.Add(new MotionInfo());
                                motions.Last().name = getWord(0);
                                int.TryParse(getWord(1), out motions.Last().start);
                                motions.Last().end = motions.Last().start;
                                int.TryParse(getWord(2), out motions.Last().end);
                                motions.Last().loop = getWord(3) == "loop";
                            }
                            break;

                        case DEFREADMODE.kMTL:
                            //必要ならマテリアル（シェーダー、ブレンドなど）設定
                            if (words[0] == "blend" && words.Length >= 2)
                            {
                                var mtl = materials[mtlname];
                                if (words[1] == "add")
                                {
                                    mtl.blend = 2;
                                }
                                else if (words[1] == "modulate")
                                {
                                    mtl.blend = 4;
                                }
                            }
                            else if (words[0] == "shader" && words.Length >= 2)
                            {
                                var mtl = materials[mtlname];
                                mtl.shader = words[1];
                            }
                            break;

                        case DEFREADMODE.kPREVIEW:
                            break;
                        case DEFREADMODE.kNONE:
                            break;
                    }
                }
            }

            reader.Close();
        }
        else
        {
            // 特になにもしない
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yukar.Common.Resource
{
    // SelectResourceDialog の InitDirTree でだけ使う
    public enum ResourceType
    {
        RES_NONE,
        RES_IMAGE,
        RES_SOUND,
        RES_MODEL,
        RES_FOLDER_IMAGE,
        RES_FOLDER_MODEL,
        RES_ROM,    // ".sgr" 参照用
        RES_FOLDER_FACE,
        RES_MODEL_AND_PARTICLE,
    }

    public enum ResourceSource
    {
        RES_USER,
        RES_SYSTEM,
        RES_DLC,
        RES_SYSTEM_CUSTOMIZED,
    }

    public enum ErrorType
    {
        NONE,
        INVALID_MAPCHIP,
        PATH_NOT_FOUND,
        INVALID_MODEL_FORMAT,
        TEXTURE_NOT_FOUND,
        INVALID_ICON,
    }

    public class ResourceItem : Rom.RomItem
    {
        public static ResourceSource sCurrentSourceMode;

        // DLC専用
        private const string DLC_PREFIX = "dlc:";
        private const string DLC_SEPARATER = "|";
        // カスタマイズ済みシステム素材専用
        private const string SYS_PREFIX = "sys:";
        public Guid dlcGuid = Guid.Empty;
        public String relatedPath = "";
        public static Guid sCurrentSourceGuid;

        public String path = "";
        public ResourceSource source = ResourceSource.RES_USER;

        public override void save(System.IO.BinaryWriter writer)
        {
            base.save(writer);
            if (source == ResourceSource.RES_SYSTEM_CUSTOMIZED)
            {
                writer.Write(SYS_PREFIX + relatedPath);
            }
            else if (source == ResourceSource.RES_DLC)
            {
                writer.Write(DLC_PREFIX + dlcGuid.ToString() + DLC_SEPARATER + relatedPath);
            }
            else
            {
                writer.Write(path);
            }
        }

        public override void load(System.IO.BinaryReader reader)
        {
            source = sCurrentSourceMode;
            base.load(reader);
            var path = reader.ReadString();
            if (path.StartsWith(DLC_PREFIX))
            {
                source = ResourceSource.RES_DLC;
                dlcGuid = new Guid(path.Substring(DLC_PREFIX.Length, Catalog.GUID_STR_SIZE));
                path = path.Substring(DLC_PREFIX.Length + Catalog.GUID_STR_SIZE + DLC_SEPARATER.Length);
            }
            else if (path.StartsWith(SYS_PREFIX))
            {
                source = ResourceSource.RES_SYSTEM_CUSTOMIZED;
                path = path.Substring(SYS_PREFIX.Length);
            }
            setPath(path);
        }

        public virtual void setPath(string path)
        {
            // path はすべて相対パスで記録するのが前提のため、
            // ここを通らせる事で一旦絶対パスとしては絶対に成立しない状態にする
            if (!path.StartsWith(".\\"))
            {
                path = ".\\" + path;
                Console.WriteLine(path);
            }

            if (isSystemResource())
            {
                relatedPath = path;
                path = Catalog.sCommonResourceDir + path;
            }
            else if (source == ResourceSource.RES_DLC)
            {
                if (dlcGuid == Guid.Empty)
                    dlcGuid = sCurrentSourceGuid;
                relatedPath = path;
                if (Catalog.sDlcDictionary.ContainsKey(dlcGuid))
                {
                    path = Catalog.sDlcDictionary[dlcGuid].path + System.IO.Path.DirectorySeparatorChar + relatedPath;
                }
                else
                {
                    dlcGuid = Guid.Empty;
                    source = ResourceSource.RES_USER;
                }
            }else
            {
                // ユーザーリソース
            }

            this.path = path;

            var fileName = Util.file.getFileName(path).ToLower();
#if WINDOWS
#else
            if (!Util.file.isContainsFileName(fileName))
            {
                throw (new System.IO.FileNotFoundException());
            }
#endif

            if (!Util.file.skipUnexistedFiles && !Util.file.exists(path) && !Util.file.dirExists(path))
            {
                // 探す
                var origPath = path;
                path = Util.searchFile(Util.file.getDirName(origPath), fileName);
#if WINDOWS
                if (path == null)
                    path = Util.searchFile(".\\res", fileName);
#endif
                if (path == null)
                {
#if WINDOWS
#else
                    UnityEngine.Debug.Log(GetType().Name + " / " + origPath + " is not found.");
                    UnityEngine.Debug.Log("Dir : " + Util.file.getDirName(origPath) + " / FileName : " + fileName);
#endif
                    throw (new System.IO.FileNotFoundException());
                }

                this.path = path;
            }
        }

        public virtual List<string> getRelatedFiles()
        {
            var result = new List<string>();
            result.Add(path);
            return result;
        }

        public virtual ErrorType verify()
        {
            return ErrorType.NONE;
        }

        public bool isSystemResource()
        {
            return source == ResourceSource.RES_SYSTEM || source == ResourceSource.RES_SYSTEM_CUSTOMIZED;
        }

        public void setToModified()
        {
            if (source == ResourceSource.RES_SYSTEM)
                source = ResourceSource.RES_SYSTEM_CUSTOMIZED;
        }

        public virtual string[] getPathList()
        {
            return new string[] { path };
        }
    }
}

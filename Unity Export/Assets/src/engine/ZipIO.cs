#if WINDOWS
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Yukar.Engine
{
    class ZipIO : Common.FileUtil
    {
        private ZipArchive zip;
        private Dictionary<string, ZipArchiveEntry> entryDic;
        private string prefix;  // 絶対パスから相対パスに変換する用

        public ZipIO(string zipPath, string prefix)
        {
            zip = ZipFile.OpenRead(zipPath);
            entryDic = zip.Entries.ToDictionary(x => x.FullName.ToLower());
            this.prefix = prefix + Path.DirectorySeparatorChar;
        }

        public override StreamReader getStreamReader(string path, Encoding encoding = null)
        {
            if(encoding != null)
                return new StreamReader(getFileStream(path), encoding);

            return new StreamReader(getFileStream(path));
        }

        public override Stream getFileStream(string path)
        {
            if (path.StartsWith(prefix))
            {
                path = path.Substring(prefix.Length, path.Length - prefix.Length);
            }

            path = path.Replace("/", "\\").Replace(".\\", "").ToLower();

            if (entryDic.ContainsKey(path))
            {
                var mem = new MemoryStream();
                var stream = entryDic[path].Open();
                stream.CopyTo(mem);
                stream.Close();
                mem.Seek(0, SeekOrigin.Begin);
                return mem;
            }

            return null;
        }
    }
}
#endif
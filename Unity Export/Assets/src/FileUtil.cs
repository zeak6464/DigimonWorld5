using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
#if WINDOWS
#else
using UnityEngine;
#endif

namespace Yukar.Common
{
    public class FileUtil
    {
        public bool skipUnexistedFiles;
        public enum FileFuncFlags : uint
        {
            FO_MOVE = 0x1,
            FO_COPY = 0x2,
            FO_DELETE = 0x3,
            FO_RENAME = 0x4
        }

        [Flags]
        public enum FILEOP_FLAGS : ushort
        {
            FOF_MULTIDESTFILES = 0x1,
            FOF_CONFIRMMOUSE = 0x2,
            /// <summary>
            /// Don't create progress/report
            /// </summary>
            FOF_SILENT = 0x4,
            FOF_RENAMEONCOLLISION = 0x8,
            /// <summary>
            /// Don't prompt the user.
            /// </summary>
            FOF_NOCONFIRMATION = 0x10,
            /// <summary>
            /// Fill in SHFILEOPSTRUCT.hNameMappings.
            /// Must be freed using SHFreeNameMappings
            /// </summary>
            FOF_WANTMAPPINGHANDLE = 0x20,
            FOF_ALLOWUNDO = 0x40,
            /// <summary>
            /// On *.*, do only files
            /// </summary>
            FOF_FILESONLY = 0x80,
            /// <summary>
            /// Don't show names of files
            /// </summary>
            FOF_SIMPLEPROGRESS = 0x100,
            /// <summary>
            /// Don't confirm making any needed dirs
            /// </summary>
            FOF_NOCONFIRMMKDIR = 0x200,
            /// <summary>
            /// Don't put up error UI
            /// </summary>
            FOF_NOERRORUI = 0x400,
            /// <summary>
            /// Dont copy NT file Security Attributes
            /// </summary>
            FOF_NOCOPYSECURITYATTRIBS = 0x800,
            /// <summary>
            /// Don't recurse into directories.
            /// </summary>
            FOF_NORECURSION = 0x1000,
            /// <summary>
            /// Don't operate on connected elements.
            /// </summary>
            FOF_NO_CONNECTED_ELEMENTS = 0x2000,
            /// <summary>
            /// During delete operation, 
            /// warn if nuking instead of recycling (partially overrides FOF_NOCONFIRMATION)
            /// </summary>
            FOF_WANTNUKEWARNING = 0x4000,
            /// <summary>
            /// Treat reparse points as objects, not containers
            /// </summary>
            FOF_NORECURSEREPARSE = 0x8000
        }

        public static string toLower(string orig)
        {
            return System.Text.RegularExpressions.Regex.Replace(orig, "[A-Z]+", m => m.Groups[0].Value.ToLower());
        }

        public string getFileName(string path)
        {
#if WINDOWS
            return Path.GetFileName(path);
#else
            path = removeRelativePathElement(path);
            var index = path.LastIndexOf("/");
            if (index < 0)
                return path;
            var result = path.Substring(index + 1, path.Length - index - 1);
            return result;
#endif
        }

        public string getFileNameWithoutExtension(string path)
        {
#if WINDOWS
            return Path.GetFileNameWithoutExtension(path);
#else
            var fileName = getFileName(path);
            var index = fileName.LastIndexOf(".");
            if (index < 0)
                return fileName;
            var result = fileName.Substring(0, index);
            return result;
#endif
        }

        internal bool isContainsFileName(string fileName)
        {
#if WINDOWS
            return false;
#else
            return entryList.Exists(x => x.name == fileName);
#endif
        }

        //[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        //If you use the above you may encounter an invalid memory access exception (when using ANSI
        //or see nothing (when using unicode) when you use FOF_SIMPLEPROGRESS flag.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public FileFuncFlags wFunc;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pFrom;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pTo;
            public FILEOP_FLAGS fFlags;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHFileOperation([In] ref SHFILEOPSTRUCT lpFileOp);

        public static void copyFile(IntPtr handle, List<string> srcList, List<string> destList)
        {
            SHFILEOPSTRUCT shfos;
            shfos.hwnd = handle;
            shfos.wFunc = FileFuncFlags.FO_COPY;
            shfos.pFrom = "";
            foreach (var src in srcList)
                shfos.pFrom += src + "\0";
            shfos.pFrom += "\0";
            shfos.pTo = "";
            foreach (var dest in destList)
                shfos.pTo += dest + "\0";
            shfos.pTo += "\0";
            shfos.fFlags = FILEOP_FLAGS.FOF_ALLOWUNDO;
            shfos.fAnyOperationsAborted = true;
            shfos.hNameMappings = IntPtr.Zero;
            shfos.lpszProgressTitle = null;

            SHFileOperation(ref shfos);
        }

        public static void copyFile(IntPtr handle, string src, string dest)
        {
            SHFILEOPSTRUCT shfos;
            shfos.hwnd = handle;
            shfos.wFunc = FileFuncFlags.FO_COPY;
            shfos.pFrom = src + "\0\0";
            shfos.pTo = dest + "\0\0";
            shfos.fFlags = FILEOP_FLAGS.FOF_ALLOWUNDO;
            shfos.fAnyOperationsAborted = true;
            shfos.hNameMappings = IntPtr.Zero;
            shfos.lpszProgressTitle = null;

            SHFileOperation(ref shfos);
        }

#if WINDOWS
#else
        internal struct AssetEntry
        {
            internal enum EntryType
            {
                FILE,
                DIRECTORY,
            }
            internal EntryType t;
            internal string name;
            internal string path;
            internal string dir;
            internal int[] extras;
        }
        internal static List<AssetEntry> entryList;
        internal static string language;

        static public void initialize()
        {
            // 言語フォルダ存在チェック
            if(Resources.Load(language + "/assets") as TextAsset == null)
            {
                language = null;
            }

            entryList = new List<AssetEntry>();
            var list = Resources.Load("assets") as TextAsset;
            var entries = list.text.Split('\n');
            foreach (var line in entries)
            {
                var words = line.Split('\t');
                if (words.Length < 2)
                    continue;
                int count = 0;
                var entry = new AssetEntry();
                entry.t = (words[count++] == "F") ? AssetEntry.EntryType.FILE : AssetEntry.EntryType.DIRECTORY;
                entry.path = toLower(Catalog.sResourceDir + words[count++]);
                if (entry.t == AssetEntry.EntryType.FILE)
                    entry.dir = toLower(Catalog.sResourceDir + words[count++]);
                else
                    entry.dir = toLower(entry.path.Substring(0, entry.path.LastIndexOf("/") + 1));
                entry.name = entry.path.Substring(entry.dir.Length, entry.path.Length - entry.dir.Length);

                if (words.Length > count)
                {
                    entry.extras = new int[words.Length - count];
                    int extraCount = 0;
                    while(words.Length > count)
                    {
                        int value = 0;
                        int.TryParse(words[count++], out value);
                        entry.extras[extraCount++] = value;
                    }
                }

                entryList.Add(entry);
            }
        }

        internal int[] getExtras(string path)
        {
            path = toLower(removeRelativePathElement(path));
            return entryList.FirstOrDefault(x => x.path == path).extras;
        }
#endif

        virtual public bool exists(string path)
        {
#if WINDOWS
            return File.Exists(path);
#else
            path = toLower(removeRelativePathElement(path));
            return entryList.Exists(x => x.path == path && x.t == AssetEntry.EntryType.FILE);
#endif
        }

        virtual public StreamReader getStreamReader(string path, Encoding encoding = null)
        {
#if WINDOWS
            if (encoding != null)
                return new StreamReader(path, encoding);
            return new StreamReader(path);
#else
            path = removeRelativePathElement(path);
            path = UnityUtil.pathConvertToUnityResource(path, false);
            var textAsset = Resources.Load<TextAsset>(path);
            var stream = new MemoryStream(textAsset.bytes);
            if (encoding != null)
                return new StreamReader(stream, encoding);
            return new StreamReader(stream);
#endif
        }

        virtual public string[] getDirectories(string path)
        {
#if WINDOWS
            return Directory.GetDirectories(path);
#else
            path = toLower(removeRelativePathElementForDir(path));
            var result = entryList.Where(x => (x.dir == path) &&
                x.t == AssetEntry.EntryType.DIRECTORY).Select(x => x.path).ToArray();
            return result;
#endif
        }

        private string removeRelativePathElementForDir(string path)
        {
            var result = toLower(removeRelativePathElement(path));
            if (!result.EndsWith("/"))
                return result + "/";
            return result;
        }

        virtual public string[] getDirectories(string path, string pattern, SearchOption searchOption)
        {
#if WINDOWS
            return Directory.GetDirectories(path, pattern, searchOption);
#else
            path = toLower(removeRelativePathElementForDir(path));
            string[] result;
            if (searchOption == SearchOption.TopDirectoryOnly)
            {
                result = entryList.Where(x => (x.dir == path) &&
                    x.path.EndsWith("/" + pattern) &&
                    x.t == AssetEntry.EntryType.DIRECTORY).Select(x => x.path).ToArray();
            }else
            {
                result = entryList.Where(x => x.path.StartsWith(path) &&
                    x.path.EndsWith("/" + pattern) &&
                    x.t == AssetEntry.EntryType.DIRECTORY).Select(x => x.path).ToArray();
            }
            return result;
#endif
        }

        virtual public bool dirExists(string path)
        {
#if WINDOWS
            return Directory.Exists(path);
#else
            path = toLower(removeRelativePathElement(path));
            return entryList.Exists(x => x.path == path && x.t == AssetEntry.EntryType.DIRECTORY);
#endif
        }

        virtual public string getDirName(string path)
        {
#if WINDOWS
            return Path.GetDirectoryName(path);
#else
            path = toLower(GetFullPath(path));
            return path.Substring(0, path.LastIndexOf('/'));
#endif
        }

        virtual public string[] getFiles(string path)
        {
#if WINDOWS
            return Directory.GetFiles(path);
#else
            path = toLower(removeRelativePathElement(path));
            var result = entryList.Where(x => x.dir == path && x.t == AssetEntry.EntryType.FILE).Select(x => x.path).ToArray();
            return result;
#endif
        }

        virtual public string[] getFiles(string path, string pattern, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
#if WINDOWS
            return Directory.GetFiles(path, pattern, searchOption);
#else
            //ワイルドカードの文字列を正規表現の文字列に変換する
            var regexPattern = System.Text.RegularExpressions.Regex.Replace(
                pattern, ".", m =>
                {
                    string s = m.Value;
                    if (s.Equals("?"))
                    {
                        //?は任意の1文字を示す正規表現(.)に変換
                        return ".";
                    }
                    else if (s.Equals("*"))
                    {
                        //*は0文字以上の任意の文字列を示す正規表現(.*)に変換
                        return ".*";
                    }
                    else
                    {
                        //上記以外はエスケープする
                        return System.Text.RegularExpressions.Regex.Escape(s);
                    }
                }
            );
            var regex = new System.Text.RegularExpressions.Regex(regexPattern);

            path = toLower(removeRelativePathElementForDir(path));
            string[] result;
            if (searchOption == SearchOption.TopDirectoryOnly)
            {
                result = entryList.Where(x => (x.dir == path) &&
                    regex.IsMatch(Util.file.getFileName(x.path)) &&
                    x.t == AssetEntry.EntryType.FILE).Select(x => x.path).ToArray();
            }
            else
            {
                result = entryList.Where(x => x.path.StartsWith(path) &&
                    regex.IsMatch(Util.file.getFileName(x.path)) &&
                    x.t == AssetEntry.EntryType.FILE).Select(x => x.path).ToArray();
            }
            return result;
#endif
        }

        virtual public Stream getFileStream(string path)
        {
#if WINDOWS
            return new FileStream(path, FileMode.Open, FileAccess.Read);
#else
            path = removeRelativePathElement(path);
            path = UnityUtil.pathConvertToUnityResource(path, false);
            
            if (language != null && path.EndsWith(".sgr"))
            {
                var appendPath = language + "/" + path;
                var ast = Resources.Load<TextAsset>(appendPath);
                if (ast != null)
                    path = appendPath;
            }

            var textAsset = Resources.Load<TextAsset>(path);
            return new MemoryStream(textAsset.bytes);
#endif
        }

        public static string GetFullPath(string path)
        {
#if WINDOWS
            return Path.GetFullPath(path);
#else
            if (!toLower(path).StartsWith(toLower(Catalog.sResourceDir)))
                path = Catalog.sResourceDir + path;

            return removeRelativePathElement(path);
#endif
        }

        private static string removeRelativePathElement(string path)
        {
            path = path.Replace("\\", "/");
            var phrases = new List<string>(path.Split('/'));
            int count = 0;
            while(phrases.Count > count)
            {
                if (phrases[count] == ".")
                {
                    phrases.RemoveAt(count);
                }
                else if(phrases[count] == "..")
                {
                    count--;
                    phrases.RemoveAt(count);
                    phrases.RemoveAt(count);
                }else
                {
                    count++;
                }
            }
            path = phrases[0];
            phrases.RemoveAt(0);
            foreach (var phrase in phrases)
            {
                path += "/" + phrase;
            }
            return path;
        }

        internal XmlTextReader getXmlReader(string path)
        {
#if WINDOWS
            return new XmlTextReader(path);
#else
            path = removeRelativePathElement(path);
            path = UnityUtil.pathConvertToUnityResource(path, true);
            var res = Resources.Load<TextAsset>(path);
            var stream = new MemoryStream(res.bytes);
            return new XmlTextReader(stream);
#endif
        }

        internal string[] getFileSystemEntries(string path)
        {
#if WINDOWS
            return Directory.GetFileSystemEntries(path);
#else
            path = removeRelativePathElement(path);
            var result = entryList.Where(x => x.path.StartsWith(path)).Select(x => x.path).ToArray();
            return result;
#endif
        }
    }
}

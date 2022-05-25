#if WINDOWS
#else
using Eppy;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Yukar.Common
{
    public class BinaryReaderWrapper : BinaryReader
    {
        public BinaryReaderWrapper(Stream stream, Encoding encoding)
            : base(stream, encoding)
        {
        }

        public enum DictionaryType
        {
            NOT_USE,
            SYSTEM,
            USER,
        }
        private static DictionaryType dictType = DictionaryType.NOT_USE;
        static Dictionary<String, String> wordDic;
        private static Dictionary<Tuple<Guid, string>, string> userDic;

        // 読み込み時に他のクラスから情報を集める用
        // 最後にセットしたままなので、ちゃんとした情報じゃないかも
        public static Guid currentGuid;
        public static Guid currentEventGuid;
        private static string projectDir;
        public static string currentEventName;

        public static string getExecDir(bool withSeparator = false)
        {
#if WINDOWS
            if (withSeparator)
            {
                return Common.Util.file.getDirName(System.Windows.Forms.Application.ExecutablePath) + System.IO.Path.DirectorySeparatorChar;
            }

            return Common.Util.file.getDirName(System.Windows.Forms.Application.ExecutablePath);
#else
            return "./";
#endif
        }
        
        internal static void init()
        {
            if (dictType == DictionaryType.SYSTEM)
            {
                wordDic = createSystemDict();
            }
            else
            {
                wordDic = null;
            }
        }

        public static Dictionary<string, string> createSystemDict()
        {
            var path = projectDir + Path.DirectorySeparatorChar + "dic.txt";
            if (!Util.file.exists(path))
            {
                path = getExecDir(true) + "dic.txt";
                if (!Util.file.exists(path))
                {
                    return null;
                }
            }

            var dict = new Dictionary<string, string>();
            var dicFile = new StreamReader(path, Encoding.UTF8);

            while (!dicFile.EndOfStream)
            {
                var line = dicFile.ReadLine().Replace("\\n", "\r\n");
                var sep = line.Split('\t');

                if (!dict.ContainsKey(sep[0]))
                    dict.Add(sep[0], sep[1]);
            }

            dicFile.Close();

            return dict;
        }

        public override string ReadString()
        {
            var result = base.ReadString();

            switch (dictType) {
                case DictionaryType.NOT_USE:
                    break;
                case DictionaryType.SYSTEM:
                    if (wordDic == null)
                        break;

                    // タブを含む場合は、テンプレートとみなして分解して個別に照合する
                    if (result.Contains('\t'))
                    {
                        var words = result.Split('\t');
                        for (int i = 0; i < words.Length; i++)
                        {
                            var minWords = words[i].Split('\n');
                            for (int j = 0; j < minWords.Length; j++)
                            {
                                var word = minWords[j].Replace("\\n", "\r\n");
                                foreach (var item in wordDic)
                                {
                                    if (item.Key == word)
                                    {
                                        minWords[j] = item.Value.Replace("\r\n", "\\n");
                                        break;
                                    }
                                }
                            }
                            words[i] = string.Join("\n", minWords);
                        }
                        result = string.Join("\t", words);
                    }
                    // タブを含まない場合は単純に比較する
                    else if (wordDic.ContainsKey(result))
                    {
                        result = wordDic[result];
                    }
                    break;
                case DictionaryType.USER:
                    // タブを含む場合は、テンプレートとみなして分解して個別に照合する
                    if (result.Contains('\t'))
                    {
                        var words = result.Split('\t');
                        for (int i = 0; i < words.Length; i++ )
                        {
                            var word = words[i].Replace("\n", "").Replace("\\n", "\r\n");
                            if (userDic != null)
                            {
                                foreach (var item in userDic)
                                {
                                    if (item.Key.Item2 == word)
                                    {
                                        words[i] = item.Value.Replace("\r\n", "\\n");
                                        break;
                                    }
                                }
                            }
                        }
                        result = string.Join("\t", words);
                    }
                    // タブを含まない場合は単純に比較する
                    else
                    {
                        var tuple = new Tuple<Guid, string>(currentGuid, result);
                        if (userDic != null && userDic.ContainsKey(tuple))
                        {
                            result = userDic[tuple];
                        }
                    }
                    break;
            }

            return result;
        }

        public static void SetToUseDictionary(DictionaryType type, string loadingPath = null)
        {
            projectDir = loadingPath;
            dictType = type;
            init();
        }

        public static void SetToUseDictionary(DictionaryType type, Dictionary<Tuple<Guid, string>, string> dict)
        {
            dictType = type;
            userDic = dict;
            init();
        }
    }
}

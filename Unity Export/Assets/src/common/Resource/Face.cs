using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Yukar.Common.Resource
{
    public class Face : ResourceItem
    {
        public enum FaceType
        {
            FACE_NORMAL,
            FACE_SMILE,
            FACE_ANGER,
            FACE_SORROW,
        };

        private string[] filePathArray = null;
        public bool isSingle;

        public string getFacePath(FaceType faceType)
        {
            if (filePathArray == null)
                createPathArray();

            return filePathArray[(int)faceType];
        }

        public void createPathArray()
        {
            filePathArray = new string[4];
            isSingle = false;

            // 拡張子が .png のファイルを列挙する
            foreach (string pngPath in Util.file.getFiles(path, "*.png")) {
                if (pngPath.EndsWith("normal.png"))
                    filePathArray[0] = pngPath;
                if (pngPath.EndsWith("smile.png"))
                    filePathArray[1] = pngPath;
                if (pngPath.EndsWith("anger.png"))
                    filePathArray[2] = pngPath;
                if (pngPath.EndsWith("sorrow.png"))
                    filePathArray[3] = pngPath;
            }

            if (filePathArray[0] == null)
            {
                var pngList = Util.file.getFiles(path + Path.DirectorySeparatorChar, "*.png");
                var pngPath = "";
                if (pngList.Length > 0)
                    pngPath = pngList[0];
                filePathArray[0] = filePathArray[1] = filePathArray[2] = filePathArray[3] = pngPath;
                isSingle = true;
            }
            else
            {
                if (filePathArray[1] == null)
                {
                    filePathArray[1] = filePathArray[0];
                }
                if (filePathArray[2] == null)
                {
                    filePathArray[2] = filePathArray[0];
                }
                if (filePathArray[3] == null)
                {
                    filePathArray[3] = filePathArray[0];
                }
            }
        }

        public override List<string> getRelatedFiles()
        {
            if (filePathArray == null)
                createPathArray();
            return filePathArray.ToList();
        }
    }
}

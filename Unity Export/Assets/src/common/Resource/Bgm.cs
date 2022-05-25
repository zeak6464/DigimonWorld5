using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Yukar.Common.Resource
{
    public enum SoundType{
        SOUND_WAV,
        SOUND_OGG,
    }

    public class Bgm : ResourceItem
    {
        public static Bgm sNotChangeItem =
            new Bgm(){ guId = new Guid("00000000-0000-0000-0000-000000000001") };
        
        public bool isLoop = true;  // ループするかどうか
        public SoundType soundType; // OggかWavか ※SetPathで算出されるのでSaveしなくてOK

        public override void save(System.IO.BinaryWriter writer)
        {
            base.save(writer);
            writer.Write(isLoop);
        }

        public override void load(System.IO.BinaryReader reader)
        {
            base.load(reader);
            isLoop = reader.ReadBoolean();
        }

        public override void setPath(string path)
        {
            base.setPath(path);

            if (Path.GetExtension(path).ToLower() == ".ogg")
            {
                soundType = SoundType.SOUND_OGG;
            }
            else
            {
                soundType = SoundType.SOUND_WAV;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Yukar.Common.Resource
{
    public class Bgs : ResourceItem
    {
        public SoundType soundType; // OggかWavか ※SetPathで算出されるのでSaveしなくてOK

        public override void save(System.IO.BinaryWriter writer)
        {
            base.save(writer);
        }

        public override void load(System.IO.BinaryReader reader)
        {
            base.load(reader);
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

using System.Collections.Generic;
using System.IO;
using Yukar.Common.Rom;

namespace Bakin
{
    public class PresetChunk : IChunk
    {
        private string name = "default";
        List<PhaseChunk> presets = new List<PhaseChunk>();

        public PhaseChunk PresetAtIndex(int index)
        {
            if (Presets != null && index >= 0 && index < Presets.Count)
            {
                return Presets[index];
            }
            return null;
        }
        public PresetChunk(string name)
        {
            Name = name;
            presets.Add(new PhaseChunk(0));
            presets.Add(new PhaseChunk(1));
            presets.Add(new PhaseChunk(2));
            presets.Add(new PhaseChunk(3));
        }

        public string Name { get; set; }
        public List<PhaseChunk> Presets { get => presets; set => presets = value; }

        public void load(BinaryReader reader)
        {
            Name = reader.ReadString();

            for (int i = 0; i < presets.Count; i++)
            {
                presets[i].load(reader);
            }
        }

        public void save(BinaryWriter writer)
        {
            writer.Write(Name);

            foreach (var preset in presets)
            {
                preset.save(writer);
            }
        }


        public override string ToString()
        {
            return $"{Name}";
        }
    }
}

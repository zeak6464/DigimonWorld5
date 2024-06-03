using System;
using System.IO;
using Yukar.Common.Rom;

namespace Bakin
{
    public class Interiors : IChunk
    {
        public Guid guid;

        public string Name { get; set; }
        public Interiors(Guid guid, string name)
        {
            this.guid = guid;
            Name = name;
        }
        public Interiors()
        {
        }
        public void load(BinaryReader reader)
        {
            guid = new Guid(reader.ReadString());
            Name = reader.ReadString();
        }

        public void save(BinaryWriter writer)
        {
            writer.Write(guid.ToString());
            writer.Write(Name);
        }
    }

}

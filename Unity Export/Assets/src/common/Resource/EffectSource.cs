namespace Yukar.Common.Resource
{
    public class EffectSource : ResourceItem
    {
        public byte xDiv = 1;
        public byte yDiv = 1;

        public override void save(System.IO.BinaryWriter writer)
        {
            base.save(writer);

            writer.Write(xDiv);
            writer.Write(yDiv);
        }

        public override void load(System.IO.BinaryReader reader)
        {
            base.load(reader);

            xDiv = reader.ReadByte();
            yDiv = reader.ReadByte();
        }
    }
}

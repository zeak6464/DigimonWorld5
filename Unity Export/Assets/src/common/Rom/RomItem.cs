using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Yukar.Common.Rom
{
    public interface IChunk
    {
        void save(BinaryWriter writer);

        void load(BinaryReader reader);
    }

    public abstract class RomItem : IChunk
    {
        public Guid guId;
        public String name;

        public RomItem()
        {
            guId = Guid.NewGuid();
        }

        public virtual void save(System.IO.BinaryWriter writer)
        {
            writer.Write(guId.ToByteArray());
            if (name == null)
                name = "";
            writer.Write(name);
        }

        public virtual void load(System.IO.BinaryReader reader)
        {
            if (Catalog.sRomVersion < 6)
            {
                name = reader.ReadString();
                guId = Util.readGuid(reader);
            }
            else
            {
                guId = Util.readGuid(reader);
                BinaryReaderWrapper.currentGuid = guId;
                name = reader.ReadString();
            }
        }

        public void copyFrom(Common.Rom.RomItem src)
        {
            if (src != null)
            {
                var stream = new MemoryStream();
                var writer = new BinaryWriter(stream);
                src.save(writer);
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new BinaryReader(stream);
                this.load(reader);
                stream.Close();
            }
        }

        // カタログには登録しないRomItem(子構造体)を書き出す
        static public void writeChunk(System.IO.BinaryWriter writer, IChunk rom)
        {
            var tmpStream = new MemoryStream();
            BinaryWriter tmpWriter = (BinaryWriter)Activator.CreateInstance(writer.GetType(), new object[] { tmpStream });
            rom.save(tmpWriter);
            writer.Write((int)tmpStream.Length);                            // サイズを書き込む
            writer.Write(tmpStream.GetBuffer(), 0, (int)tmpStream.Length);  // バイト列を書き込む
            tmpWriter.Close();
        }

        // カタログには登録しないRomItem(子構造体)を読み出す
        static public void readChunk(System.IO.BinaryReader reader, IChunk rom)
        {
            var chunkSize = reader.ReadInt32();
            var curPos = reader.BaseStream.Position;

            if (chunkSize <= reader.BaseStream.Length - reader.BaseStream.Position)
            {
                var tmpStream = new MemoryStream();
                var buffer = reader.ReadBytes(chunkSize);
                tmpStream.Write(buffer, 0, chunkSize);
                tmpStream.Position = 0;
                var tmpReader = new BinaryReaderWrapper(tmpStream, Encoding.UTF8);
                try
                {
                    rom.load(tmpReader);                                               // 読み込む
                }
                catch (EndOfStreamException e)
                {
                    Console.WriteLine(e.Message);
                }

                tmpReader.Close();
            }

            reader.BaseStream.Seek(curPos + chunkSize, SeekOrigin.Begin);   // チャンク分シークする
        }

        internal virtual bool isOptionMatched(string option)
        {
            return false;
        }

        public static T Clone<T>(RomItem inSrc) where T : new()
        {
            var dst = new T();

            (dst as RomItem).copyFrom(inSrc);

            return dst;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace Yukar.Common.GameData
{
    public class Event : IGameDataItem
    {
        public Guid guId = Guid.Empty;
        public Guid graphic = Guid.Empty;
        public string motion;
        public int dir;
        public float x;
        public float y;
        public float z;

        public bool scriptRunning = false;
        public int scriptState = 0;
        public Stack<int> scriptStack = new Stack<int>();
        public int scriptCur = 0;

        public void save(BinaryWriter writer)
        {
            if (motion == null)
                motion = "";

            writer.Write(guId.ToByteArray());
            writer.Write(graphic.ToByteArray());
            writer.Write(motion);
            writer.Write(dir);
            writer.Write(x);
            writer.Write(y);
            writer.Write(z);

            writer.Write(scriptRunning);
            if (scriptRunning)
            {
                writer.Write(scriptState);
                writer.Write(scriptStack.Count);
                foreach (var num in scriptStack.ToArray())
                {
                    writer.Write(num);
                }
                writer.Write(scriptCur);
            }
        }

        public void load(Catalog catalog, BinaryReader reader)
        {
            guId = Util.readGuid(reader);
            graphic = Util.readGuid(reader);
            motion = reader.ReadString();
            dir = reader.ReadInt32();
            x = reader.ReadSingle();
            y = reader.ReadSingle();
            z = reader.ReadSingle();

            scriptStack.Clear();
            scriptRunning = reader.ReadBoolean();
            if (scriptRunning)
            {
                scriptState = reader.ReadInt32();
                int stackCount = reader.ReadInt32();
                for (int i = 0; i < stackCount; i++)
                {
                    scriptStack.Push(reader.ReadInt32());
                }
                scriptCur = reader.ReadInt32();
            }
        }
    }
}

using System.Collections.Generic;
using System.IO;
using Yukar.Common;

namespace Bakin
{
    public class DayNightChunk : Yukar.Common.Rom.IChunk
    {
        public bool isDisabled = false;
        public string totalTimeVar = "totalTime";
        public string absoluteTimeVar = "absoluteTime";
        public string currentDayVar = "currentDay";
        public int startHour = 4;
        public int endHour = 20;
        public bool useFog = true;
        public bool useVignette = true;
        public bool useShadows = true;

        public List<Interiors> interiorList = new List<Interiors>();

        public void load(BinaryReader reader)
        {
            isDisabled = reader.ReadBoolean();
            totalTimeVar = reader.ReadString();
            absoluteTimeVar = reader.ReadString();
            currentDayVar = reader.ReadString();
            startHour = reader.ReadInt32();
            endHour = reader.ReadInt32();
            useFog = reader.ReadBoolean();
            useVignette = reader.ReadBoolean();
            useShadows = reader.ReadBoolean();



            if (Util.isEndOfStream(reader)) return;

        }

        public void save(BinaryWriter writer)
        {
            writer.Write(isDisabled);
            writer.Write(totalTimeVar);
            writer.Write(absoluteTimeVar);
            writer.Write(currentDayVar);
            writer.Write(startHour);
            writer.Write(endHour);
            writer.Write(useFog);
            writer.Write(useVignette);
            writer.Write(useShadows);
        }
    }
}

using System.IO;

namespace Bakin
{
    public class PhaseChunk : Yukar.Common.Rom.IChunk
    {
        public PhaseChunk(int type)
        {
            this.type = type;
        }


        internal int type = 0;

        public int lightColor = -14236;
        public float lightIntensity = 1f;
        public float shadowX = 1f;
        public float shadowY = 1f;
        public bool buildingLights = false;

        public int ambientColor = -14798176;
        public float ambientIntensity = 1f;
        public float IblIntensity = 1f;


        public bool useFog = true;
        public int fogColor = -4594945;
        public float fogIntensity = 1f;
        public float fogDensity = 0.010f;

        public bool useVignette = false;
        public float vignetteValue = 0f;


        // nextUpdate 1.1.0

        public float fogApplicability = 1f;
        public float fogDamping = 0.100f;
        public float fogStartDistance = 80f;

        public bool useAutoExposure = false;
        public float autoExpoMaxBrightness = 0.3f;
        public float autoExpoMinScale = 0.4f;
        public float autoExpoMaxScale = 1.5f;

        public void load(BinaryReader reader)
        {
            type = reader.ReadInt32();
            lightColor = reader.ReadInt32();
            lightIntensity = reader.ReadSingle();
            shadowX = reader.ReadSingle();
            shadowY = reader.ReadSingle();
            buildingLights = reader.ReadBoolean();
            ambientColor = reader.ReadInt32();
            ambientIntensity = reader.ReadSingle();
            IblIntensity = reader.ReadSingle();
            useFog = reader.ReadBoolean();
            fogColor = reader.ReadInt32();
            fogIntensity = reader.ReadSingle();
            fogDensity = reader.ReadSingle();
            useVignette = reader.ReadBoolean();
            vignetteValue = reader.ReadSingle();
            fogApplicability = reader.ReadSingle();
            fogDamping = reader.ReadSingle();
            fogStartDistance = reader.ReadSingle();
            useAutoExposure = reader.ReadBoolean();
            autoExpoMaxBrightness = reader.ReadSingle();
            autoExpoMinScale = reader.ReadSingle();
            autoExpoMaxScale = reader.ReadSingle();
        }

        public void save(BinaryWriter writer)
        {
            writer.Write(type);
            writer.Write(lightColor);
            writer.Write(lightIntensity);
            writer.Write(shadowX);
            writer.Write(shadowY);
            writer.Write(buildingLights);
            writer.Write(ambientColor);
            writer.Write(ambientIntensity);
            writer.Write(IblIntensity);
            writer.Write(useFog);
            writer.Write(fogColor);
            writer.Write(fogIntensity);
            writer.Write(fogDensity);
            writer.Write(useVignette);
            writer.Write(vignetteValue);
            writer.Write(fogApplicability);
            writer.Write(fogDamping);
            writer.Write(fogStartDistance);
            writer.Write(useAutoExposure);
            writer.Write(autoExpoMaxBrightness);
            writer.Write(autoExpoMinScale);
            writer.Write(autoExpoMaxScale);
        }
    }

}

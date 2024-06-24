using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Yukar.Common;
using Yukar.Engine;
// @@include DayNightChunk.cs
namespace Bakin
{
    public class DayNightManager
    {
        public static DayNightManager instance;
        public static string ownerName = string.Empty;
        readonly Guid SettingsChunk = new Guid("88431A79-F9A5-499F-96ED-4CAEC55F59D8");
        readonly Guid PresetChunk = new Guid("0e3f9b24-1ae6-4932-8091-b288f4f64528");
        readonly Guid interiorChunk = new Guid("808edb43-61b3-4548-be1c-042df0cfd7c1");
        internal DayNightChunk systemChunk = new DayNightChunk();
        private List<PresetChunk> presetsData = new List<PresetChunk>();
        private List<Interiors> interiorData = new List<Interiors>();
        internal string totalTimeVar = "totalTime";
        internal string absoluteTimeVar = "absoluteTime";
        internal string currentDayVar = "currentDay";
        internal string dayNightSettingsVar = "dayNightSettings";

        private int absoluteTime;

        private const float TotalMinutesInDay = 1440f; // 24 horas * 60 minutos
        private float currentMinutes;

        private Color dawnColor = new Color(204, 152, 102);
        private Color dayColor = new Color(135, 206, 250);
        private Color duskColor = new Color(190, 146, 85);
        private Color nightColor = new Color(54, 67, 111);

        private Color dawnAmbientColor = new Color(63, 53, 236);
        private Color dayAmbientColor = new Color(135, 206, 250);
        private Color duskAmbientColor = new Color(200, 134, 70);
        private Color nightAmbientColor = new Color(25, 33, 49);

        private float dawnShadowX = -1f;
        private float dayShadowX = -2f;
        private float duskShadowX = -2.6f;
        private float nightShadowX = -3.5f;

        private float dawnShadowY = 2.3f;
        private float dayShadowY = 2.0f;
        private float duskShadowY = 1.8f;
        private float nightShadowY = 1.6f;

        private float dawnIntensity = 1f;
        private float dayIntensity = 1.2f;
        private float duskIntensity = 1f;
        private float nightIntensity = 0.1f;

        private float dawnAmbientIntensity = 1f;
        private float dayAmbientIntensity = 1f;
        private float duskAmbientIntensity = 1f;
        private float nightAmbientIntensity = 0.2f;

        private float dawnIBLIntensity = 0.4f;
        private float dayIBLIntensity = 0.5f;
        private float duskIBLIntensity = 0.6f;
        private float nightIBLIntensity = 1f;
        private State currentState;

        private int totalDay;
        private int startHour = 4 * 60;
        private int endHour = 20 * 60;

        private bool dawnUseLightBuildings = true;
        private bool dayUseLightBuildings;
        private bool duskUseLightBuildings;
        private bool nightUseLightBuildings = true;
        private bool dawnUseFog = true;
        private bool dayUseFog;
        private bool duskUseFog;
        private bool nightUseFog = true;

        private Color dawnFogColor = new Color(204, 237, 255);
        private Color dayFogColor;
        private Color duskFogColor;
        private Color nightFogColor = new Color(11, 15, 191);
        private float dawnFogIntensity = 0.5f;
        private float dayFogIntensity;
        private float duskFogIntensity;
        private float nightFogIntensity = 1f;
        private float dawnFogDensity = 0.010f;
        private float dayFogDensity;
        private float duskFogDensity;
        private float nightFogDensity = 0.015f;
        private bool dawnUseVignette = true;
        private bool dayUseVignette;
        private bool duskUseVignette;
        private bool nightUseVignette = true;
        private float dawnVignetteIntensity = 0f;
        private float dayVignetteIntensity;
        private float duskVignetteIntensity;
        private float nightVignetteIntensity = 3.3f;

        public Color DawnColor { get => dawnColor; set { dawnColor = value; SaveCurrentGameSettings(); } }
        public Color DayColor { get => dayColor; set { dayColor = value; SaveCurrentGameSettings(); } }
        public Color DuskColor { get => duskColor; set { duskColor = value; SaveCurrentGameSettings(); } }
        public Color NightColor { get => nightColor; set { nightColor = value; SaveCurrentGameSettings(); } }


        public Color DawnAmbientColor { get => dawnAmbientColor; set { dawnAmbientColor = value; SaveCurrentGameSettings(); } }
        public Color DayAmbientColor { get => dayAmbientColor; set { dayAmbientColor = value; SaveCurrentGameSettings(); } }
        public Color DuskAmbientColor { get => duskAmbientColor; set { duskAmbientColor = value; SaveCurrentGameSettings(); } }
        public Color NightAmbientColor { get => nightAmbientColor; set { nightAmbientColor = value; SaveCurrentGameSettings(); } }

        public float DawnShadowX { get => dawnShadowX; set { dawnShadowX = value; SaveCurrentGameSettings(); } }
        public float DayShadowX { get => dayShadowX; set { dayShadowX = value; SaveCurrentGameSettings(); } }
        public float DuskShadowX { get => duskShadowX; set { duskShadowX = value; SaveCurrentGameSettings(); } }
        public float NightShadowX { get => nightShadowX; set { nightShadowX = value; SaveCurrentGameSettings(); } }

        public float DawnShadowY { get => dawnShadowY; set { dawnShadowY = value; SaveCurrentGameSettings(); } }
        public float DayShadowY { get => dayShadowY; set { dayShadowY = value; SaveCurrentGameSettings(); } }
        public float DuskShadowY { get => duskShadowY; set { duskShadowY = value; SaveCurrentGameSettings(); } }
        public float NightShadowY { get => nightShadowY; set { nightShadowY = value; SaveCurrentGameSettings(); } }

        public float DawnIntensity { get => dawnIntensity; set { dawnIntensity = value; SaveCurrentGameSettings(); } }
        public float DayIntensity { get => dayIntensity; set { dayIntensity = value; SaveCurrentGameSettings(); } }
        public float DuskIntensity { get => duskIntensity; set { duskIntensity = value; SaveCurrentGameSettings(); } }
        public float NightIntensity { get => nightIntensity; set { nightIntensity = value; SaveCurrentGameSettings(); } }

        public float DawnAmbientIntensity { get => dawnAmbientIntensity; set { dawnAmbientIntensity = value; SaveCurrentGameSettings(); } }
        public float DayAmbientIntensity { get => dayAmbientIntensity; set { dayAmbientIntensity = value; SaveCurrentGameSettings(); } }
        public float DuskAmbientIntensity { get => duskAmbientIntensity; set { duskAmbientIntensity = value; SaveCurrentGameSettings(); } }
        public float NightAmbientIntensity { get => nightAmbientIntensity; set { nightAmbientIntensity = value; SaveCurrentGameSettings(); } }

        public float DawnIBLIntensity { get => dawnIBLIntensity; set { dawnIBLIntensity = value; SaveCurrentGameSettings(); } }
        public float DayIBLIntensity { get => dayIBLIntensity; set { dayIBLIntensity = value; SaveCurrentGameSettings(); } }
        public float DuskIBLIntensity { get => duskIBLIntensity; set { duskIBLIntensity = value; SaveCurrentGameSettings(); } }
        public float NightIBLIntensity { get => nightIBLIntensity; set { nightIBLIntensity = value; SaveCurrentGameSettings(); } }
        public string CurrentLutName { get; internal set; }
        public int AbsoluteTime { get => absoluteTime; set => absoluteTime = value; }
        public int StartHour { get => startHour; set => startHour = value * 60; }
        public int EndHour { get => endHour; set => endHour = value * 60; }
        public bool UseFog { get => useFog; private set => useFog = value; }
        public bool UseVignette { get => useVignette; private set => useVignette = value; }
        public bool UseShadows { get => useShadows; private set => useShadows = value; }

        private bool useFog;
        private bool useVignette;
        private bool useShadows;

        public DayNightManager()
        {
            GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"Creating Day-Night Cycle instance");

            // Load ExtraChunk

            var entries = GameMain.instance.catalog.getFilteredExtraChunkList(SettingsChunk);
            var presets = GameMain.instance.catalog.getFilteredExtraChunkList(PresetChunk);
            var interiors = GameMain.instance.catalog.getFilteredExtraChunkList(interiorChunk);

            if (entries.Count > 0)
                entries[0].readChunk(systemChunk);

            if (presets.Count > 0)
            {
                foreach (var preset in presets)
                {
                    if (presetsData.FirstOrDefault(x => x.Name == preset.Name) != null) continue;

                    PresetChunk tempChunk = new PresetChunk("");
                    preset.readChunk(tempChunk);

                    presetsData.Add(tempChunk);
                }
            }

            if (interiors.Count > 0)
            {
                foreach (var interior in interiors)
                {
                    if (interiorData.FirstOrDefault(x => x.guid == interior.guId) != null) continue;

                    Interiors tempChunk = new Interiors();
                    interior.readChunk(tempChunk);

                    interiorData.Add(tempChunk);
                }
            }

            //   LoadChunk();
        }

        internal void LoadChunk()
        {

            instance.totalTimeVar = systemChunk.totalTimeVar;
            instance.absoluteTimeVar = systemChunk.absoluteTimeVar;
            instance.currentDayVar = systemChunk.currentDayVar;
            instance.StartHour = systemChunk.startHour;
            instance.EndHour = systemChunk.endHour;
            instance.useFog = systemChunk.useFog;
            instance.useVignette = systemChunk.useVignette;
            instance.useShadows = systemChunk.useShadows;

            LoadCurrentGameSettings();
            GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"Loading dayNight Chunk");
        }

        private void LoadCurrentGameSettings()
        {
            var currentSettings = GameMain.instance.data.system.GetStrVariable(dayNightSettingsVar, Guid.Empty, false);

            if (string.IsNullOrEmpty(currentSettings)) return;

            var curSettings = currentSettings.Split('|');

            if (currentSettings.Length < 27) return;

            DoLoad(curSettings);
        }

        private void DoLoad(string[] curSettingsArr)
        {
            int index = 0;

            try
            {
                dawnColor = JagTools.GetStringRGBColor(curSettingsArr[index++], out bool _);
                dayColor = JagTools.GetStringRGBColor(curSettingsArr[index++], out bool _);
                duskColor = JagTools.GetStringRGBColor(curSettingsArr[index++], out bool _);
                nightColor = JagTools.GetStringRGBColor(curSettingsArr[index++], out bool _);

                dawnAmbientColor = JagTools.GetStringRGBColor(curSettingsArr[index++], out bool _);
                dayAmbientColor = JagTools.GetStringRGBColor(curSettingsArr[index++], out bool _);
                duskAmbientColor = JagTools.GetStringRGBColor(curSettingsArr[index++], out bool _);
                nightAmbientColor = JagTools.GetStringRGBColor(curSettingsArr[index++], out bool _);

                dawnShadowX = float.Parse(curSettingsArr[index++]);
                dayShadowX = float.Parse(curSettingsArr[index++]);
                duskShadowX = float.Parse(curSettingsArr[index++]);
                nightShadowX = float.Parse(curSettingsArr[index++]);

                dawnShadowY = float.Parse(curSettingsArr[index++]);
                dayShadowY = float.Parse(curSettingsArr[index++]);
                duskShadowY = float.Parse(curSettingsArr[index++]);
                nightShadowY = float.Parse(curSettingsArr[index++]);

                dawnIntensity = float.Parse(curSettingsArr[index++]);
                dayIntensity = float.Parse(curSettingsArr[index++]);
                duskIntensity = float.Parse(curSettingsArr[index++]);
                nightIntensity = float.Parse(curSettingsArr[index++]);

                dawnAmbientIntensity = float.Parse(curSettingsArr[index++]);
                dayAmbientIntensity = float.Parse(curSettingsArr[index++]);
                duskAmbientIntensity = float.Parse(curSettingsArr[index++]);
                nightAmbientIntensity = float.Parse(curSettingsArr[index++]);

                dawnIBLIntensity = float.Parse(curSettingsArr[index++]);
                dayIBLIntensity = float.Parse(curSettingsArr[index++]);
                duskIBLIntensity = float.Parse(curSettingsArr[index++]);
                nightIBLIntensity = float.Parse(curSettingsArr[index++]);

                if (index >= curSettingsArr.Length) return; // version handler - pre release

                dawnUseLightBuildings = bool.Parse(curSettingsArr[index++]);
                dayUseLightBuildings = bool.Parse(curSettingsArr[index++]);
                duskUseLightBuildings = bool.Parse(curSettingsArr[index++]);
                nightUseLightBuildings = bool.Parse(curSettingsArr[index++]);

                dawnUseFog = bool.Parse(curSettingsArr[index++]);
                dayUseFog = bool.Parse(curSettingsArr[index++]);
                duskUseFog = bool.Parse(curSettingsArr[index++]);
                nightUseFog = bool.Parse(curSettingsArr[index++]);

                dawnFogColor = JagTools.GetStringRGBColor(curSettingsArr[index++], out bool _);
                dayFogColor = JagTools.GetStringRGBColor(curSettingsArr[index++], out bool _);
                duskFogColor = JagTools.GetStringRGBColor(curSettingsArr[index++], out bool _);
                nightFogColor = JagTools.GetStringRGBColor(curSettingsArr[index++], out bool _);

                dawnFogIntensity = float.Parse(curSettingsArr[index++]);
                dayFogIntensity = float.Parse(curSettingsArr[index++]);
                duskFogIntensity = float.Parse(curSettingsArr[index++]);
                nightFogIntensity = float.Parse(curSettingsArr[index++]);

                dawnFogDensity = float.Parse(curSettingsArr[index++]);
                dayFogDensity = float.Parse(curSettingsArr[index++]);
                duskFogDensity = float.Parse(curSettingsArr[index++]);
                nightFogDensity = float.Parse(curSettingsArr[index++]);

                dawnUseVignette = bool.Parse(curSettingsArr[index++]);
                dayUseVignette = bool.Parse(curSettingsArr[index++]);
                duskUseVignette = bool.Parse(curSettingsArr[index++]);
                nightUseVignette = bool.Parse(curSettingsArr[index++]);

                dawnVignetteIntensity = float.Parse(curSettingsArr[index++]);
                dayVignetteIntensity = float.Parse(curSettingsArr[index++]);
                duskVignetteIntensity = float.Parse(curSettingsArr[index++]);
                nightVignetteIntensity = float.Parse(curSettingsArr[index++]);


                GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"Game dayNight loaded successful");
            }
            catch (Exception ex)
            {
                GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"Error loading dayNight game settings");
                GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"{ex}");
            }

        }

        private void SaveCurrentGameSettings()
        {
            string settingsToSave = "";

            // save light color
            settingsToSave += $"{dawnColor.R},{dawnColor.G},{dawnColor.B}|";
            settingsToSave += $"{dayColor.R},{dayColor.G},{dayColor.B}|";
            settingsToSave += $"{duskColor.R},{duskColor.G},{duskColor.B}|";
            settingsToSave += $"{nightColor.R},{nightColor.G},{nightColor.B}|";

            // save ambient color
            settingsToSave += $"{dawnAmbientColor.R},{dawnAmbientColor.G},{dawnAmbientColor.B}|";
            settingsToSave += $"{dayAmbientColor.R},{dayAmbientColor.G},{dayAmbientColor.B}|";
            settingsToSave += $"{duskAmbientColor.R},{duskAmbientColor.G},{duskAmbientColor.B}|";
            settingsToSave += $"{nightAmbientColor.R},{nightAmbientColor.G},{nightAmbientColor.B}|";

            // shadows angle: 
            settingsToSave += $"{dawnShadowX}|";
            settingsToSave += $"{dayShadowX}|";
            settingsToSave += $"{duskShadowX}|";
            settingsToSave += $"{nightShadowX}|";

            settingsToSave += $"{dawnShadowY}|";
            settingsToSave += $"{dayShadowY}|";
            settingsToSave += $"{duskShadowY}|";
            settingsToSave += $"{nightShadowY}|";


            // light intensity:
            settingsToSave += $"{dawnIntensity}|";
            settingsToSave += $"{dayIntensity}|";
            settingsToSave += $"{duskIntensity}|";
            settingsToSave += $"{nightIntensity}|";

            // ambient intensity:
            settingsToSave += $"{dawnAmbientIntensity}|";
            settingsToSave += $"{dayAmbientIntensity}|";
            settingsToSave += $"{duskAmbientIntensity}|";
            settingsToSave += $"{nightAmbientIntensity}|";

            // IBL intensity:
            settingsToSave += $"{dawnIBLIntensity}|";
            settingsToSave += $"{dayIBLIntensity}|";
            settingsToSave += $"{duskIBLIntensity}|";
            settingsToSave += $"{nightIBLIntensity}|";

            // Light buildings

            settingsToSave += $"{dawnUseLightBuildings}|";
            settingsToSave += $"{dayUseLightBuildings}|";
            settingsToSave += $"{duskUseLightBuildings}|";
            settingsToSave += $"{nightUseLightBuildings}|";

            // Fog usage
            settingsToSave += $"{dawnUseFog}|";
            settingsToSave += $"{dayUseFog}|";
            settingsToSave += $"{duskUseFog}|";
            settingsToSave += $"{nightUseFog}|";

            // Fog Color
            settingsToSave += $"{dawnFogColor.R},{dawnFogColor.G},{dawnFogColor.B}|";
            settingsToSave += $"{dayFogColor.R},{dayFogColor.G},{dayFogColor.B}|";
            settingsToSave += $"{duskFogColor.R},{duskFogColor.G},{duskFogColor.B}|";
            settingsToSave += $"{nightFogColor.R},{nightFogColor.G},{nightFogColor.B}|";

            // Fog Intensity 

            settingsToSave += $"{dawnFogIntensity}|";
            settingsToSave += $"{dayFogIntensity}|";
            settingsToSave += $"{duskFogIntensity}|";
            settingsToSave += $"{nightFogIntensity}|";

            // Fog Density

            settingsToSave += $"{dawnFogDensity}|";
            settingsToSave += $"{dayFogDensity}|";
            settingsToSave += $"{duskFogDensity}|";
            settingsToSave += $"{nightFogDensity}|";

            // Vignette usage 

            settingsToSave += $"{dawnUseVignette}|";
            settingsToSave += $"{dayUseVignette}|";
            settingsToSave += $"{duskUseVignette}|";
            settingsToSave += $"{nightUseVignette}|";

            // Vignette intensity 

            settingsToSave += $"{dawnVignetteIntensity}|";
            settingsToSave += $"{dayVignetteIntensity}|";
            settingsToSave += $"{duskVignetteIntensity}|";
            settingsToSave += $"{nightVignetteIntensity}|";


            GameMain.instance.data.system.SetVariable(dayNightSettingsVar, settingsToSave, Guid.Empty, false);

        }


        public float UpdateMinutes()
        {
            // var hourToMin = GameMain.instance.data.system.GetVariable("hour", Guid.Empty, false) * 60;
            var Mins = (float)GameMain.instance.data.system.GetVariable(totalTimeVar, Guid.Empty, false)/* + hourToMin*/;
            var Day = (int)GameMain.instance.data.system.GetVariable(currentDayVar, Guid.Empty, false) /* + hourToMin*/;

            totalDay = Day;
            currentMinutes = Mins;

            UpdateAbsoluteTime();

            return currentMinutes;
        }

        public Color GetCurrentColor(float currentMinutes, int type = 0)
        {
            var normalizedTime = GetNormalizedTime(currentMinutes);

            if (normalizedTime < 0.25f)
            {
                // amanecer
                if (type == 1) return Color.Lerp(NightAmbientColor, DawnAmbientColor, normalizedTime / 0.25f);
                if (type == 2) return Color.Lerp(nightFogColor, dawnFogColor, normalizedTime / 0.25f);

                return Color.Lerp(nightColor, dawnColor, normalizedTime / 0.25f);
            }
            else if (normalizedTime < 0.5f)
            {
                // mañana
                if (type == 1) return Color.Lerp(dawnAmbientColor, DayAmbientColor, (normalizedTime - 0.25f) / 0.25f);
                if (type == 2) return Color.Lerp(dawnFogColor, dayFogColor, (normalizedTime - 0.25f) / 0.25f);

                return Color.Lerp(dawnColor, dayColor, (normalizedTime - 0.25f) / 0.25f);
            }
            else if (normalizedTime < 0.75f)
            {
                // tarde
                if (type == 1) return Color.Lerp(DayAmbientColor, duskAmbientColor, (normalizedTime - 0.5f) / 0.25f);
                if (type == 2) return Color.Lerp(dayFogColor, duskFogColor, (normalizedTime - 0.5f) / 0.25f);

                return Color.Lerp(dayColor, duskColor, (normalizedTime - 0.5f) / 0.25f);
            }
            else
            {
                // noche
                if (type == 1) return Color.Lerp(duskAmbientColor, NightAmbientColor, (normalizedTime - 0.75f) / 0.25f);
                if (type == 2) return Color.Lerp(duskFogColor, nightFogColor, (normalizedTime - 0.75f) / 0.25f);

                return Color.Lerp(duskColor, nightColor, (normalizedTime - 0.75f) / 0.25f);
            }
        }
        public float GetCurrentShadowAngle(float currentMinutes, int type)
        {
            var normalizedTime = GetNormalizedTime(currentMinutes);

            if (normalizedTime < 0.25f)
            {
                float amount = normalizedTime / 0.25f;
                // amanecer
                if (type == 1) return MathHelper.Lerp(dawnShadowY - 0.2f, dawnShadowY, amount);

                return MathHelper.Lerp(0, dawnShadowX, amount);
            }
            else if (normalizedTime < 0.5f)
            {
                // mañana
                float amount = (normalizedTime - 0.25f) / 0.25f;
                if (type == 1) return MathHelper.Lerp(dawnShadowY, dayShadowY, amount);

                return MathHelper.Lerp(dawnShadowX, dayShadowX, amount);
            }
            else if (normalizedTime < 0.75f)
            {
                // tarde
                float amount = (normalizedTime - 0.5f) / 0.25f;
                if (type == 1) return MathHelper.Lerp(dayShadowY, duskShadowY, amount);

                return MathHelper.Lerp(dayShadowX, duskShadowX, amount);
            }
            else
            {
                // noche
                float amount = (normalizedTime - 0.75f) / 0.25f;
                if (type == 1) return MathHelper.Lerp(duskShadowY, nightShadowY, amount);

                return MathHelper.Lerp(duskShadowX, nightShadowX, amount);
            }
        }

        public float GetCurrentIntensity(float currentMinutes, int type)
        {
            var normalizedTime = GetNormalizedTime(currentMinutes);

            if (normalizedTime < 0.25f)
            {
                // amanecer
                StateSetter(State.Dawn);
                float amount = normalizedTime / 0.25f;
                if (type == 1) return MathHelper.Lerp(nightAmbientIntensity, dawnAmbientIntensity, amount);
                if (type == 2) return MathHelper.Lerp(nightIBLIntensity, dawnIBLIntensity, amount);
                if (type == 3) return MathHelper.Lerp(nightFogIntensity, dawnFogIntensity, amount);
                if (type == 4) return MathHelper.Lerp(nightVignetteIntensity, dawnVignetteIntensity, amount);

                return MathHelper.Lerp(nightIntensity, dawnIntensity, amount);
            }
            else if (normalizedTime < 0.5f)
            {
                // mañana
                StateSetter(State.Day);
                float amount = (normalizedTime - 0.25f) / 0.25f;
                if (type == 1) return MathHelper.Lerp(dawnAmbientIntensity, dayAmbientIntensity, amount);
                if (type == 2) return MathHelper.Lerp(dawnIBLIntensity, dayIBLIntensity, amount);
                if (type == 3) return MathHelper.Lerp(dawnFogIntensity, dayFogIntensity, amount);
                if (type == 4) return MathHelper.Lerp(dawnVignetteIntensity, dayVignetteIntensity, amount);

                return MathHelper.Lerp(dawnIntensity, dayIntensity, amount);
            }
            else if (normalizedTime < 0.75f)
            {
                // tarde
                StateSetter(State.Dusk);
                float amount = (normalizedTime - 0.5f) / 0.25f;
                if (type == 1) return MathHelper.Lerp(dayAmbientIntensity, duskAmbientIntensity, amount);
                if (type == 2) return MathHelper.Lerp(dayIBLIntensity, duskIBLIntensity, amount);
                if (type == 3) return MathHelper.Lerp(dayFogIntensity, duskFogIntensity, amount);
                if (type == 4) return MathHelper.Lerp(dayVignetteIntensity, duskVignetteIntensity, amount);

                return MathHelper.Lerp(dayIntensity, duskIntensity, amount);
            }
            else
            {
                // noche
                StateSetter(State.Night);
                float amount = (normalizedTime - 0.75f) / 0.25f;
                if (type == 1) return MathHelper.Lerp(duskAmbientIntensity, nightAmbientIntensity, amount);
                if (type == 2) return MathHelper.Lerp(duskIBLIntensity, nightIBLIntensity, amount);
                if (type == 3) return MathHelper.Lerp(duskFogIntensity, nightFogIntensity, amount);
                if (type == 4) return MathHelper.Lerp(duskVignetteIntensity, nightVignetteIntensity, amount);

                return MathHelper.Lerp(duskIntensity, nightIntensity, amount);
            }
        }

        private float GetNormalizedTime(float currentMinutes)
        {

            float normalizedTime = (currentMinutes - StartHour) / (EndHour - StartHour);
            normalizedTime = MathHelper.Clamp(normalizedTime, 0f, 1f);

            return normalizedTime;
        }

        internal State GetCurrentState()
        {
            return currentState;
        }

        private void StateSetter(State state)
        {
            if (currentState == state) return;

            currentState = state;
        }

        private void LutChanger()
        {

        }

        internal void AddMinutes(float time)
        {
            time *= GameMain.getElapsedTime();
            var currentTime = GameMain.instance.data.system.GetVariable(totalTimeVar, Guid.Empty, false);
            currentTime += time;

            if (currentTime > TotalMinutesInDay)
            {
                totalDay += 1;
                GameMain.instance.data.system.SetVariable(currentDayVar, totalDay);

                currentTime = 0;
            }
            else if (currentTime < 0) currentTime = 1440;


            GameMain.instance.data.system.SetVariable(totalTimeVar, currentTime);
        }

        internal void SetCurrentPreset(string presetName)
        {
            var presetToUse = presetsData.FirstOrDefault(x => x.Name == presetName);

            if (presetToUse == null) return;

            GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"{presetToUse.Name} found!");

            var dawnSettings = presetToUse.Presets[0];
            var daySettings = presetToUse.Presets[1];
            var duskSettings = presetToUse.Presets[2];
            var nightSettings = presetToUse.Presets[3];

            dawnColor = Util.ToXnaColor(System.Drawing.Color.FromArgb(dawnSettings.lightColor));
            dawnIntensity = dawnSettings.lightIntensity;
            dawnShadowX = dawnSettings.shadowX;
            dawnShadowY = dawnSettings.shadowY;
            dawnAmbientColor = Util.ToXnaColor(System.Drawing.Color.FromArgb(dawnSettings.ambientColor));
            dawnAmbientIntensity = dawnSettings.ambientIntensity;
            dawnIBLIntensity = dawnSettings.IblIntensity;
            dawnUseLightBuildings = dawnSettings.buildingLights;
            dawnUseFog = dawnSettings.useFog;
            dawnUseVignette = dawnSettings.useVignette;
            dawnFogIntensity = dawnSettings.fogIntensity;
            dawnFogDensity = dawnSettings.fogDensity;
            dawnFogColor = Util.ToXnaColor(System.Drawing.Color.FromArgb(dawnSettings.fogColor));
            dawnVignetteIntensity = dawnSettings.vignetteValue;

            dayColor = Util.ToXnaColor(System.Drawing.Color.FromArgb(daySettings.lightColor));
            dayIntensity = daySettings.lightIntensity;
            dayShadowX = daySettings.shadowX;
            dayShadowY = daySettings.shadowY;
            dayAmbientColor = Util.ToXnaColor(System.Drawing.Color.FromArgb(daySettings.ambientColor));
            dayAmbientIntensity = daySettings.ambientIntensity;
            dayIBLIntensity = daySettings.IblIntensity;
            dayUseLightBuildings = daySettings.buildingLights;
            dayUseFog = daySettings.useFog;
            dayUseVignette = daySettings.useVignette;
            dayFogIntensity = daySettings.fogIntensity;
            dayFogDensity = daySettings.fogDensity;
            dayFogColor = Util.ToXnaColor(System.Drawing.Color.FromArgb(daySettings.fogColor));
            dayVignetteIntensity = daySettings.vignetteValue;

            duskColor = Util.ToXnaColor(System.Drawing.Color.FromArgb(duskSettings.lightColor));
            duskIntensity = duskSettings.lightIntensity;
            duskShadowX = duskSettings.shadowX;
            duskShadowY = duskSettings.shadowY;
            duskAmbientColor = Util.ToXnaColor(System.Drawing.Color.FromArgb(duskSettings.ambientColor));
            duskAmbientIntensity = duskSettings.ambientIntensity;
            duskIBLIntensity = duskSettings.IblIntensity;
            duskUseLightBuildings = duskSettings.buildingLights;
            duskUseFog = duskSettings.useFog;
            duskUseVignette = duskSettings.useVignette;
            duskFogIntensity = duskSettings.fogIntensity;
            duskFogDensity = duskSettings.fogDensity;
            duskFogColor = Util.ToXnaColor(System.Drawing.Color.FromArgb(duskSettings.fogColor));
            duskVignetteIntensity = duskSettings.vignetteValue;

            nightColor = Util.ToXnaColor(System.Drawing.Color.FromArgb(nightSettings.lightColor));
            nightIntensity = nightSettings.lightIntensity;
            nightShadowX = nightSettings.shadowX;
            nightShadowY = nightSettings.shadowY;
            nightAmbientColor = Util.ToXnaColor(System.Drawing.Color.FromArgb(nightSettings.ambientColor));
            nightAmbientIntensity = nightSettings.ambientIntensity;
            nightIBLIntensity = nightSettings.IblIntensity;
            nightUseLightBuildings = nightSettings.buildingLights;
            nightUseFog = nightSettings.useFog;
            nightUseVignette = nightSettings.useVignette;
            nightFogIntensity = nightSettings.fogIntensity;
            nightFogDensity = nightSettings.fogDensity;
            nightFogColor = Util.ToXnaColor(System.Drawing.Color.FromArgb(nightSettings.fogColor));
            nightVignetteIntensity = nightSettings.vignetteValue;

            SaveCurrentGameSettings();
        }

        internal void UpdateAbsoluteTime()
        {
            absoluteTime = (int)GameMain.instance.data.system.GetVariable(totalTimeVar, Guid.Empty, false);
            absoluteTime += (int)GameMain.instance.data.system.GetVariable(currentDayVar, Guid.Empty, false) * 1440;

            GameMain.instance.data.system.SetVariable(absoluteTimeVar, absoluteTime);
        }


        internal string GetCurrentLut(State state)
        {
            switch (state)
            {
                case State.Dawn:
                    return "Night_lut";
                case State.Day:
                    return "none";
                case State.Dusk:
                    return "none";
                case State.Night:
                    return "Night_lut";
            }

            return "none";
        }

        internal bool GetCurrentBL()
        {
            switch (currentState)
            {
                case State.Dawn:
                    return dawnUseLightBuildings;

                case State.Day:
                    return dayUseLightBuildings;

                case State.Dusk:
                    return duskUseLightBuildings;

                case State.Night:
                    return nightUseLightBuildings;
            }

            return false;
        }

        internal bool IsInInterior()
        {
            if (interiorData.Count == 0) return false;

            if (interiorData.FirstOrDefault(x => x.guid == GameMain.instance.mapScene.mapDrawer.mapRom.guId) == null) return false;

           // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"In an interior!");
            return true;
        }

        public enum State
        {
            Dawn = 0,
            Day = 1,
            Dusk = 2,
            Night = 3
        }
    }



}

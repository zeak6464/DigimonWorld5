using System;
using Yukar.Engine;
using Texture = Yukar.Common.Resource.Texture;

// @@include JagTools.cs
// @@include Interiors.cs
// @@include PhaseChunk.cs
// @@include PresetChunk.cs
// @@include DayNightChunk.cs
// @@include DayNightManager.cs
// @@include MainRender.cs
// @@include RenderData.cs

namespace Bakin
{
    // ============================================
    // DayNight Plugin - 1.0.1
    // By Jagonz
    // https://jagonz.itch.io/day-night-plugin
    // Copyright © 2024
    // ============================================

    public class MainDayNight : BakinObject
    {
        private DayNightManager dayNightSystem;
        private MainRender renderManager;

        public override void Start()
        {
            InstanceManager();
            renderManager = new MainRender();
            renderManager.Start();
        }

        public override void Update()
        {
            InstanceManager();
            CheckInstances();
        }

        private void CheckInstances()
        {
            if (renderManager?.mapScene != GameMain.instance.mapScene)
            {
                renderManager?.Start();
            }
        }

        private void InstanceManager()
        {
            if (DayNightManager.instance == null && mapChr != null && mapChr.isCommonEvent)
            {
                GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"Initializing instance owner: {mapChr?.rom?.Name} commonEvent");

                DayNightManager.instance = new DayNightManager();
                DayNightManager.instance.LoadChunk();
                DayNightManager.ownerName = mapChr?.rom?.Name;
                dayNightSystem = DayNightManager.instance;

            }
            else if (dayNightSystem != DayNightManager.instance && DayNightManager.instance != null)
            {
                GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"Assigning instance to {mapChr?.rom?.Name}");
                dayNightSystem = DayNightManager.instance;
            }
        }
        public override void Destroy()
        {
            if (mapChr.isCommonEvent && mapChr?.rom?.Name == DayNightManager.ownerName)
            {
                GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"Destroying dayNight instance");
                DayNightManager.instance = null;
            }
        }

        [BakinFunction(Description = "")]
        public void _____________DAY_NIGHT_SYSTEM_____________()
        {
        }

        [BakinFunction(Description = "Update day-night system using the current \"totalTime\" variable.\n\n現在の 'totalTime' 変数を使用して昼夜システムを更新")]
        public void UpdateDayCycle()
        {
            if (dayNightSystem == null)
            {
                InstanceManager();
                return;
            }
               
            if (dayNightSystem.systemChunk.isDisabled) return;
            if (dayNightSystem.IsInInterior()) return;
      
            var currentTime = dayNightSystem.UpdateMinutes();

            GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"CURRENT TIME {currentTime}");
            var currentColor = dayNightSystem.GetCurrentColor(currentTime);
            var currentAmbientColor = dayNightSystem.GetCurrentColor(currentTime, 1);
            var currentFogColor = dayNightSystem.GetCurrentColor(currentTime, 2);


            var currentShadowX = dayNightSystem.GetCurrentShadowAngle(currentTime, 0);
            var currentShadowY = dayNightSystem.GetCurrentShadowAngle(currentTime, 1);
            var currentIntensity = dayNightSystem.GetCurrentIntensity(currentTime, 0);
            var currentAmbientIntensity = dayNightSystem.GetCurrentIntensity(currentTime, 1);
            var currentIBLIntensity = dayNightSystem.GetCurrentIntensity(currentTime, 2);

            var currentFogIntensity = dayNightSystem.GetCurrentIntensity(currentTime, 3);
            var currentFogDensity = dayNightSystem.GetCurrentIntensity(currentTime, 5);
            var currentVignetteIntensity = dayNightSystem.GetCurrentIntensity(currentTime, 4);

            bool buildingLights = dayNightSystem.GetCurrentBL();

            // string lut = dayNightSystem.GetCurrentLut(dayNightSystem.GetCurrentState());

            //SetLut(lut);

            CheckInstances();

            if (dayNightSystem.UseShadows)
            {
                renderManager?.SetShadowAngleX(currentShadowX);
                renderManager?.SetShadowAngleY(currentShadowY);
            }

            renderManager?.SetLightIntensity(currentIntensity);
            renderManager?.SetAmbientLightIntensity(currentAmbientIntensity);
            renderManager?.SetIBLIntensity(currentIBLIntensity);

            renderManager?.SetLightColor($"{currentColor.R},{currentColor.G},{currentColor.B}");
            renderManager?.SetAmbientColor($"{currentAmbientColor.R},{currentAmbientColor.G},{currentAmbientColor.B}");

            renderManager?.SetBuildingLight(buildingLights);

            if (dayNightSystem.UseFog)
            {
                renderManager?.SetFogColor($"{currentFogColor.R},{currentFogColor.G},{currentFogColor.B}");
                renderManager?.SetFogIntensity(currentFogIntensity);
                renderManager?.SetFogDensity(currentFogDensity);
            }
            if (dayNightSystem.UseVignette)
            {
                renderManager?.SetVignetteIntensity(currentVignetteIntensity);
            }
        }



        [BakinFunction(Description = "Set the current Dawn, Day, Dusk, Night settings by using a render preset." +
            "\n\n現在の夜明け、昼、夕暮れ、夜の設定をレンダープリセットを使用して設定してください")]
        public void SetCurrentPreset(string presetName)
        {
            if (dayNightSystem == null) return;
            InstanceManager();

            dayNightSystem.SetCurrentPreset(presetName);
        }

        [BakinFunction(Description = "Increment \"totalTime\" variable using delta time \nUse this in a repeat parallel event page. \nExample: 4 (this will add 4 minutes per second)." +
            "\n\nデルタタイムを使用して 'totalTime' 変数を増加させます。\nこれをリピート並列イベントページで使用してください。\n例：4（これにより、1秒あたりに4分追加されます）")]
        public void AddTime(float time)
        {
            bool func()
            {
                if (dayNightSystem == null) return true;

                dayNightSystem.AddMinutes(time);

                return false;
            }

            mapScene.owner.pushTask(func);

        }


        [BakinFunction(Description = "Set current hour of the day, use 24H format here. \n Example: if current time is 08:23 and you use value: 15 \nthe hour will be 15:23." +
            "\n\n日中の現在の時間を設定し、ここでは24時間形式を使用してください。\n例：現在の時間が08:23で、値を15に設定した場合、時間は15:23になります")]
        public void SetCurrentHour(float value)
        {
            bool func()
            {
                if (dayNightSystem == null) return true;
                InstanceManager();

                value = (value * 60) + GetCurrentMinute(1);
                GameMain.instance.data.system.SetVariable(dayNightSystem.totalTimeVar, value, Guid.Empty, false);
                dayNightSystem.UpdateMinutes();
                return false;
            }

            mapScene.owner.pushTask(func);

        }

        [BakinFunction(Description = "Set the current minute of the current hour, \n Example: if current time is 08:15 and you use value: 54 \nthe hour will be 08:54." +
            "\n\n現在の時間の現在の分を設定します。\n例：現在の時間が08:15で、値を54に設定した場合、時間は08:54になります")]
        public void SetCurrentMinute(float value)
        {
            bool func()
            {
                if (dayNightSystem == null) return true;
                InstanceManager();
                float currentHour = GetCurrentHour(1);
                GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"valor de hora {currentHour}");
                currentHour = (currentHour * 60) + value;
                GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"valor de hora despues de agregar {currentHour}");

                GameMain.instance.data.system.SetVariable(dayNightSystem.totalTimeVar, currentHour, Guid.Empty, false);
                dayNightSystem.UpdateMinutes();
                return false;
            }

            mapScene.owner.pushTask(func);
        }

        [BakinFunction(Description = "Set the current day." +
            "\n\n現在の日を設定")]
        public void SetDay(int value)
        {
            bool func()
            {
                if (dayNightSystem == null) return true;
                InstanceManager();
                GameMain.instance.data.system.SetVariable(dayNightSystem.currentDayVar, value, Guid.Empty, false);

                dayNightSystem.UpdateMinutes();
                return false;
            }

            mapScene.owner.pushTask(func);
        }

        [BakinFunction(Description = "Returns the current hour in the specified format \n If int parameter is equal to 1 the format will be 24H otherwise it will return a 12H format"
            + "\n\n 指定された形式で現在の時間を返します。\n整数パラメータが1と等しい場合、フォーマットは24時間制になります。\nそれ以外の場合は12時間制の形式で返されます.")]
        public int GetCurrentHour(int is24Hours)
        {
            if (dayNightSystem == null) return 0;
            var currentMinutes = dayNightSystem.UpdateMinutes();
            float hour;
            if (is24Hours == 1)
            {
                hour = currentMinutes / 60;
                return (int)hour;
            }

            hour = (int)(currentMinutes / 60) % 12;
            int minutes = (int)currentMinutes % 60;

            if (hour == 0)
            {
                hour = 12;
            }

            return (int)hour;
        }

        [BakinFunction(Description = "Returns the current minutes of the hour." +
            "\n\n現在の時間の分を返します")]
        public float GetCurrentMinute(int type = 0)
        {
            if (dayNightSystem == null) return 0;

            var currentMinutes = dayNightSystem.UpdateMinutes();

            var minutes = currentMinutes % 60;

            return type == 0 ? (int)minutes : minutes;
        }
        [BakinFunction(Description = "Returns AM or PM string according to the current time" +
            "\n\n現在の時間に応じてAMまたはPMの文字列を返します")]
        public string GetAMPM()
        {
            if (dayNightSystem == null) return "";
            var currentMinutes = dayNightSystem.UpdateMinutes();
            string amPm = currentMinutes < 720 ? "AM" : "PM";

            return amPm;
        }

        [BakinFunction(Description = "Returns the current time in string format \n 0 = 12H format : 1 = 24H format." +
            "\n\n文字列形式で現在の時間を返します。0 = 12時間制；1 = 24時間制")]
        public string GetCurrentTimeString(int is24H)
        {
            bool is24Hours = is24H != 0;

            return GetFormattedTime(is24Hours);
        }

        internal string GetTimeAbsoluteTimeBased(int absoluteTime)
        {

            var currentAbsolute = (int)GameMain.instance.data.system.GetVariable(dayNightSystem.absoluteTimeVar, Guid.Empty, false);

            int minutes = (int)absoluteTime % 60;


            var currentDay = absoluteTime /= 1440;

            return $"Day: {currentDay}";
        }


        [BakinFunction(Description = "Return 1 if the current absolute time is greater or equal to the absolute time parameter otherwise it will return 0." +
            "\n\n現在の絶対時間が絶対時間パラメータ以上であれば1を返し、それ以外の場合は0を返します")]
        public int IsTimeExpired(int value)
        {
            if (dayNightSystem == null) return 0;

            return dayNightSystem.AbsoluteTime >= value ? 1 : 0;
        }

        [BakinFunction(Description = "Get current day phase string")]
        public string GetPhaseString()
        {
          
                if (dayNightSystem == null) return "";
                InstanceManager();
            
                var currentState = dayNightSystem.GetCurrentState();

                 if (currentState == DayNightManager.State.Dawn) return "Dawn";
                 if (currentState == DayNightManager.State.Day) return "Day";
                 if (currentState == DayNightManager.State.Dusk) return "Dusk";
                 if (currentState == DayNightManager.State.Night) return "Night";

            return "";
        }
        private void SetLut(string lut)
        {
            if (dayNightSystem.CurrentLutName == lut) return;

            dayNightSystem.CurrentLutName = lut;

            var lutResource = catalog.getItemFromName<Texture>(lut);

            if (lutResource == null && lut != "none") return;


            GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"se establecerá: {lut}");

            var guid = Guid.Empty;
            if (lut != "none") guid = lutResource.guId;

            mapScene.mapDrawer.renderSettings.lut = guid;
            mapScene.owner.gameView.setLutMap(guid);

            // mapScene.mapDrawer.reloadAsset(false, true);
        }

        [BakinFunction(Description = "")]
        public void ____________PHASE__COLORS________________() { }

        [BakinFunction(Description = "Set Dawn color for the automatic day-night system." +
            "\n\n自動昼夜システムの夜明け色を設定")]
        public void SetDawnColor(string color)
        {
            bool func()
            {
                var targetColor = JagTools.GetStringRGBColor(color, out bool fail);

                if (fail) return false;
                if (dayNightSystem == null)
                {
                    //  GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"Waiting for instance");
                    return true;
                }

                InstanceManager();

                // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"Setting dawn color!");
                dayNightSystem.DawnColor = targetColor;

                return false;
            }
            mapScene.owner.pushTask(func);

        }

        [BakinFunction(Description = "Set Day color for the automatic day-night system." +
            "\n\n自動昼夜システムの昼の色を設定")]
        public void SetDayColor(string color)
        {
            bool func()
            {
                var targetColor = JagTools.GetStringRGBColor(color, out bool fail);

                if (fail) return false; if (dayNightSystem == null) return true;
                InstanceManager();
                dayNightSystem.DayColor = targetColor;

                return false;
            }
            mapScene.owner.pushTask(func);
        }

        [BakinFunction(Description = "Set Dusk color for the automatic day-night system." +
            "\n\n自動昼夜システムの夕暮れ色を設定")]
        public void SetDuskColor(string color)
        {
            bool func()
            {
                var targetColor = JagTools.GetStringRGBColor(color, out bool fail);

                if (fail) return false;
                if (dayNightSystem == null) return true;
                InstanceManager();

                dayNightSystem.DuskColor = targetColor;

                return false;
            }
            mapScene.owner.pushTask(func);

        }

        [BakinFunction(Description = "Set Night color for the automatic day-night system." +
            "\n\n自動昼夜システムの夜の色を設定")]
        public void SetNightColor(string color)
        {
            bool func()
            {
                var targetColor = JagTools.GetStringRGBColor(color, out bool fail);

                if (fail) return false;
                if (dayNightSystem == null) return true;
                InstanceManager();


                DayNightManager.instance.NightColor = targetColor;
                return false;
            }
            mapScene.owner.pushTask(func);
        }

        [BakinFunction(Description = "")]
        public void ____________PHASE_AMBIENT_COLORS__________() { }

        [BakinFunction(Description = "Set Dawn Ambient color for the automatic day-night system" +
            "\n\n自動昼夜システムの夜明けの環境色を設定")]
        public void SetDawnAmbientColor(string color)
        {
            bool func()
            {
                var targetColor = JagTools.GetStringRGBColor(color, out bool fail);

                if (fail) return false;
                if (dayNightSystem == null)
                {
                    // GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"NULO POR SIEMPRE!");
                    return true;
                }
                InstanceManager();
              //  GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"Setting dawn ambient color!");
                dayNightSystem.DawnAmbientColor = targetColor;

                return false;
            }
            mapScene.owner.pushTask(func);



        }

        [BakinFunction(Description = "Set Day Ambient color for the automatic day-night system." +
            "\n\n自動昼夜システムの昼の環境色を設定")]
        public void SetDayAmbientColor(string color)
        {
            bool func()
            {
                var targetColor = JagTools.GetStringRGBColor(color, out bool fail);

                if (fail) return false;
                if (dayNightSystem == null) return true;
                InstanceManager();

                dayNightSystem.DayAmbientColor = targetColor;
                return false;
            }
            mapScene.owner.pushTask(func);
        }

        [BakinFunction(Description = "Set Dusk Ambient color for the automatic day-night system." +
            "\n\n自動昼夜システムの夕暮れの環境色を設定")]
        public void SetDuskAmbientColor(string color)
        {
            bool func()
            {
                var targetColor = JagTools.GetStringRGBColor(color, out bool fail);

                if (fail) return false;
                if (dayNightSystem == null) return true;
                InstanceManager();

                dayNightSystem.DuskAmbientColor = targetColor;
                return false;
            }
            mapScene.owner.pushTask(func);
        }

        [BakinFunction(Description = "Set Night Ambient color for the automatic day-night system." +
            "\n\n自動昼夜システムの夜の環境色を設定")]
        public void SetNightAmbientColor(string color)
        {
            bool func()
            {
                var targetColor = JagTools.GetStringRGBColor(color, out bool fail);

                if (fail) return false;
                if (dayNightSystem == null) return true;
                InstanceManager();

                dayNightSystem.NightAmbientColor = targetColor;
                return false;
            }
            mapScene.owner.pushTask(func);
        }

        [BakinFunction(Description = "")]
        public void ____________PHASE_INTENSITY_________________() { }

        [BakinFunction(Description = "Set Dawn Light intensity." +
            "\n\n夜明けの光の強度を設定")]
        public void SetDawnLightIntensity(float intensity)
        {
            bool func()
            {
                if (dayNightSystem == null) return true;
                InstanceManager();
                dayNightSystem.DawnIntensity = intensity;

                return false;
            }
            mapScene.owner.pushTask(func);

        }

        [BakinFunction(Description = "Set Day Light intensity." +
            "\n\n昼の光の強度を設定")]
        public void SetDayLightIntensity(float intensity)
        {
            bool func()
            {
                if (dayNightSystem == null) return true;
                InstanceManager();
                dayNightSystem.DayIntensity = intensity;
                return false;
            }
            mapScene.owner.pushTask(func);
        }

        [BakinFunction(Description = "Set Dusk Light intensity." +
            "\n\n夕暮れの光の強度を設定")]
        public void SetDuskLightIntensity(float intensity)
        {
            bool func()
            {
                if (dayNightSystem == null) return true;
                InstanceManager();
                dayNightSystem.DuskIntensity = intensity;

                return false;
            }
            mapScene.owner.pushTask(func);



        }

        [BakinFunction(Description = "Set Night Light intensity." +
            "\n\n夜の光の強度を設定")]
        public void SetNightLightIntensity(float intensity)
        {
            bool func()
            {
                if (dayNightSystem == null) return true;
                InstanceManager();
                dayNightSystem.NightIntensity = intensity;

                return false;
            }
            mapScene.owner.pushTask(func);

        }

        [BakinFunction(Description = "")]
        public void ____________PHASE_AMBIENT_INTENSITY_________() { }
        [BakinFunction(Description = "Set Dawn Ambient intensity." +
            "\n\n夜明けの環境の強度を設定")]
        public void SetDawnAmbientIntensity(float intensity)
        {
            bool func()
            {
                if (dayNightSystem == null) return true;
                InstanceManager();
                dayNightSystem.DawnAmbientIntensity = intensity;

                return false;
            }
            mapScene.owner.pushTask(func);


        }

        [BakinFunction(Description = "Set Day Ambient intensity." +
            "\n\n昼の環境の強度を設定")]
        public void SetDayAmbientIntensity(float intensity)
        {
            bool func()
            {
                if (dayNightSystem == null) return true;
                InstanceManager();
                dayNightSystem.DayAmbientIntensity = intensity;

                return false;
            }
            mapScene.owner.pushTask(func);

        }

        [BakinFunction(Description = "Set Dusk Ambient intensity." +
            "\n\n夕暮れの環境の強度を設定")]
        public void SetDuskAmbientIntensity(float intensity)
        {
            bool func()
            {
                if (dayNightSystem == null) return true;
                InstanceManager();
                dayNightSystem.DuskAmbientIntensity = intensity;

                return false;
            }
            mapScene.owner.pushTask(func);

        }

        [BakinFunction(Description = "Set Night Ambient intensity." +
            "\n\n夜の環境の強度を設定")]
        public void SetNightAmbientIntensity(float intensity)
        {
            bool func()
            {
                if (dayNightSystem == null) return true;
                InstanceManager();
                dayNightSystem.NightAmbientIntensity = intensity;

                return false;
            }
            mapScene.owner.pushTask(func);

        }
        [BakinFunction(Description = "")]
        public void ____________PHASE_IBL_INTENSITY______________() { }

        [BakinFunction(Description = "Set Dawn IBL intensity." +
            "\n\n夜明けのIBL強度を設定")]
        public void SetDawnIBLIntensity(float intensity)
        {
            bool func()
            {
                if (dayNightSystem == null) return true;
                InstanceManager();
                dayNightSystem.DawnIBLIntensity = intensity;

                return false;
            }
            mapScene.owner.pushTask(func);

        }

        [BakinFunction(Description = "Set Day IBL intensity." +
            "\n\n昼のIBL強度を設定")]
        public void SetDayIBLIntensity(float intensity)
        {
            bool func()
            {
                if (dayNightSystem == null) return true;
                InstanceManager();
                dayNightSystem.DayIBLIntensity = intensity;

                return false;
            }
            mapScene.owner.pushTask(func);

        }

        [BakinFunction(Description = "Set Dusk IBL intensity." +
            "\n\n夕暮れのIBL強度を設定")]
        public void SetDuskIBLIntensity(float intensity)
        {
            bool func()
            {
                if (dayNightSystem == null) return true;
                InstanceManager();
                dayNightSystem.DuskIBLIntensity = intensity;

                return false;
            }
            mapScene.owner.pushTask(func);

        }

        [BakinFunction(Description = "Set Night IBL intensity." +
            "\n\n夜のIBL強度を設定")]
        public void SetNightIBLIntensity(float intensity)
        {
            bool func()
            {
                if (dayNightSystem == null) return true;
                InstanceManager();
                dayNightSystem.NightIBLIntensity = intensity;

                return false;
            }
            mapScene.owner.pushTask(func);

        }

        [BakinFunction(Description = "")]
        public void ____________PHASE_SHADOW_ANGLE_X_________() { }

        [BakinFunction(Description = "Set Dawn X angle for the automatic day-night system." +
            "\n\n自動昼夜システムの夜明けのX角度を設定")]
        public void SetDawnX(float angle)
        {
            bool func()
            {
                if (dayNightSystem == null) return true;
                InstanceManager();
                dayNightSystem.DawnShadowX = angle;

                return false;
            }
            mapScene.owner.pushTask(func);

        }

        [BakinFunction(Description = "Set Day X angle for the automatic day-night system." +
            "\n\n自動昼夜システムの昼のX角度を設定")]
        public void SetDayX(float angle)
        {
            bool func()
            {
                if (dayNightSystem == null) return true;
                InstanceManager();
                dayNightSystem.DayShadowX = angle;
                return false;
            }
            mapScene.owner.pushTask(func);

        }

        [BakinFunction(Description = "Set Dusk X angle for the automatic day-night system." +
            "\n\n自動昼夜システムの夕暮れのX角度を設定")]
        public void SetDuskX(float angle)
        {
            bool func()
            {
                if (dayNightSystem == null) return true;
                InstanceManager();
                dayNightSystem.DuskShadowX = angle;

                return false;
            }
            mapScene.owner.pushTask(func);

        }

        [BakinFunction(Description = "Set Night x angle for the automatic day-night system." +
            "\n\n自動昼夜システムの夜のX角度を設定")]
        public void SetNightX(float angle)
        {
            bool func()
            {
                if (dayNightSystem == null) return true;
                InstanceManager();
                dayNightSystem.NightShadowX = angle;

                return false;
            }
            mapScene.owner.pushTask(func);

        }
        [BakinFunction(Description = "")]
        public void ____________PHASE_SHADOW_ANGLE_Y__________() { }
        [BakinFunction(Description = "Set Dawn Y angle for the automatic day-night system." +
            "\n\n自動昼夜システムの夜明けのY角度を設定")]
        public void SetDawnY(float angle)
        {
            bool func()
            {
                if (dayNightSystem == null) return true;
                InstanceManager();
                dayNightSystem.DawnShadowY = angle;

                return false;
            }
            mapScene.owner.pushTask(func);

        }

        [BakinFunction(Description = "Set Day Y angle for the automatic day-night system." +
            "\n\n自動昼夜システムの昼のY角度を設定")]
        public void SetDayY(float angle)
        {
            bool func()
            {
                if (dayNightSystem == null) return true;
                InstanceManager();
                dayNightSystem.DayShadowY = angle;

                return false;
            }
            mapScene.owner.pushTask(func);

        }

        [BakinFunction(Description = "Set Dusk Y angle for the automatic day-night system." +
            "\n\n自動昼夜システムの夕暮れのY角度を設定")]
        public void SetDuskY(float angle)
        {
            bool func()
            {
                if (dayNightSystem == null) return true;
                InstanceManager();
                dayNightSystem.DuskShadowY = angle;

                return false;
            }
            mapScene.owner.pushTask(func);

        }

        [BakinFunction(Description = "Set Night Y angle for the automatic day-night system." +
            "\n\n自動昼夜システムの夜のY角度を設定")]
        public void SetNightY(float angle)
        {
            bool func()
            {
                if (dayNightSystem == null) return true;
                InstanceManager();
                dayNightSystem.NightShadowY = angle;

                return false;
            }
            mapScene.owner.pushTask(func);

        }

        private string GetFormattedTime(bool is24HourFormat)
        {
            if (dayNightSystem == null) return "";
            var hours = GetCurrentHour(is24HourFormat ? 1 : 0);
            var minutes = GetCurrentMinute();

            if (!is24HourFormat && hours == 0)
            {
                hours = 12;
            }

            var amPm = GetAMPM();

            return is24HourFormat ? $"{(int)hours:D2}:{(int)minutes:D2}" : $"{(int)hours:D2}:{(int)minutes:D2} {amPm}";
        }


    }


}

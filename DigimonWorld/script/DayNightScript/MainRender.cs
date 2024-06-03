using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Yukar.Common;
using Yukar.Common.Resource;
using Yukar.Common.Rom;
using Yukar.Engine;
using Color = Microsoft.Xna.Framework.Color;
using Material = Yukar.Common.Resource.Material;
using Texture = Yukar.Common.Resource.Texture;
// @@include RenderData.cs

namespace Bakin
{
    public class MainRender : BakinObject
    {
        private Guid currenGuidData;

        public override void Start()
        {
            mapScene = GameMain.instance.mapScene;
            catalog = GameMain.instance.catalog;
        }

        [BakinFunction(Description = "Map light settings")]
        public void __________LIGHT__________() { }

        [BakinFunction(Description = "Change map light intensity \n Example: 1.4")]
        public void SetLightIntensity(float value)
        {
            if (value < 0) value = 0;
            else if (value > 10000) value = 10000;

            mapScene.mapDrawer.renderSettings.dirLightIntensity = value;
        }

        [BakinFunction(Description = "Change map light color using a string \n Example: string = \"50,255,190\" ")]
        public void SetLightColor(string value)
        {
            var color = GetStringColor(value, out bool fail);

            if (fail) return;

            mapScene.owner.gameView.setDirectionalLightColor(Util.ToKmyColor(color));
            mapScene.mapDrawer.renderSettings.dirLightColor = color;
        }

        [BakinFunction(Description = "Returns current map light intensity in float format")]
        public float GetLightIntensity()
        {
            var currentIntensity = mapScene.mapDrawer.renderSettings.dirLightIntensity;
            return currentIntensity;
        }

        [BakinFunction(Description = "Returns current map light color in string format (R,G,B)")]
        public string GetLightColor()
        {
            var currentColor = mapScene.mapDrawer.renderSettings.dirLightColor;

            return $"{currentColor.R},{currentColor.G},{currentColor.B}";
        }

        private Color GetStringColor(string value, out bool fail)
        {
            var colorArray = value.Split(',');

            if (colorArray.Length != 3) fail = true;
            fail = false;
            var colorValues = new int[colorArray.Length];

            for (int i = 0; i < colorValues.Length; i++)
            {
                if (int.TryParse(colorArray[i], out colorValues[i])) continue;

                fail = true;
            }

            return new Color(colorValues[0], colorValues[1], colorValues[2]);
        }

        [BakinFunction(Description = "Set shadow angle X \n Example: -1.0")]
        public void SetShadowAngleX(float value)
        {
            if (value < -100) value = -100;
            else if (value > 100) value = 100;

            mapScene.mapDrawer.renderSettings.ShadowRotateX = value;
        }

        [BakinFunction(Description = "Set shadow angle Y \n Example: -0.6")]
        public void SetShadowAngleY(float value)
        {
            if (value < -100) value = -100;
            else if (value > 100) value = 100;

            mapScene.mapDrawer.renderSettings.ShadowRotateY = value;
        }


        [BakinFunction(Description = "Set shadow distance \n Example: -1.2")]
        public void SetShadowRange(float value)
        {
            if (value < 1) value = 1;
            else if (value > 10000) value = 10000;

            mapScene.mapDrawer.renderSettings.ShadowDistance = value;
        }

        [BakinFunction(Description = "Map ambient light settings")]
        public void __________AMBIENT_LIGHT____() { }

        [BakinFunction(Description = "Change map Ambient intensity \n Example: 1.2")]
        public void SetAmbientLightIntensity(float value)
        {
            if (value < 0) value = 0;
            else if (value > 10000) value = 10000;

            mapScene.mapDrawer.renderSettings.AmbLightIntensity = value;
        }

        [BakinFunction(Description = "Change ambient IBL light intensity \n Example: 0.4")]
        public void SetIBLIntensity(float value)
        {
            if (value < 0) value = 0;
            else if (value > 10000) value = 10000;

            mapScene.mapDrawer.renderSettings.IBLIntensity = value;
        }

        [BakinFunction(Description = "Change map ambient light color using a string \n Example: string = \"50,120,100\" ")]
        public void SetAmbientColor(string value)
        {
            var colorArray = value.Split(',');

            if (colorArray.Length != 3) return;

            var colorValues = new int[colorArray.Length];

            for (int i = 0; i < colorValues.Length; i++)
            {
                if (int.TryParse(colorArray[i], out colorValues[i])) continue;

                return;
            }
            var color = new Microsoft.Xna.Framework.Color(colorValues[0], colorValues[1], colorValues[2]);

            mapScene.owner.gameView.setAmbientLight(Util.ToKmyColor(color));
            mapScene.mapDrawer.renderSettings.ambLightColor = color;

        }

        [BakinFunction(Description = "Returns current map ambient light intensity in float format")]
        public float GetAmbientIntensity()
        {
            var currentIntensity = mapScene.mapDrawer.renderSettings.ambientIntensity;
            return currentIntensity;
        }

        [BakinFunction(Description = "Returns current map IBL intensity in float format")]
        public float GetIBLIntensity()
        {
            var currentIntensity = mapScene.mapDrawer.renderSettings.iblIntensity;
            return currentIntensity;
        }

        [BakinFunction(Description = "Returns current map ambient light color in string format (R,G,B)")]
        public string GetAmbientColor()
        {
            var currentColor = mapScene.mapDrawer.renderSettings.ambLightColor;
            return $"{currentColor.R},{currentColor.G},{currentColor.B}";
        }


        [BakinFunction(Description = "Map auto exposure settings")]
        public void __________AUTO_EXPOSURE____________() { }

        [BakinFunction(Description = "Enable/disable auto exposure \n Example: 1 to enable, 0 to disable")]
        public void UseAutoExposure(int value)
        {
            if (value != 1) value = 0;
            mapScene.mapDrawer.renderSettings.UseAutoExposure = value == 1;
        }

        [BakinFunction(Description = "Set target exposure brightness \n Example: 0.4")]
        public void SetStandartBrightness(float value)
        {
            if (value < 0.001f) value = 0.001f;
            else if (value > 1) value = 1;

            mapScene.mapDrawer.renderSettings.ExposureTargetBrightness = value;
        }

        [BakinFunction(Description = "Set minimun exposure scale \n Example: 0.3")]
        public void SetMinimunScale(float value)
        {
            if (value < 0) value = 0;
            else if (value > 1) value = 1;

            mapScene.mapDrawer.renderSettings.ExposureMinScale = value;
        }

        [BakinFunction(Description = "Set maximum exposure scale \n Example: 1.8")]
        public void SetMaximumScale(float value)
        {
            if (value < 0) value = 0;
            else if (value > 10000) value = 10000;

            mapScene.mapDrawer.renderSettings.ExposureMaxScale = value;
        }


        [BakinFunction(Description = "Map background settings")]
        public void ____BACKGROUND____() { }


        [BakinFunction(Description = "Set background type (0 = color, 1 = skybox)")]
        public void SetBackgroundType(int value)
        {
            if (value < 0) value = 0;
            else if (value > 1) value = 1;
            mapScene.mapDrawer.renderSettings.BgType = value;

            // mapScene.mapDrawer.setRenderSettings(test);
        }

        [BakinFunction(Description = "Change background color \n Example: string = \"100,175,100\" ")]
        public void SetBackgroundColor(string value)
        {
            var colorArray = value.Split(',');

            if (colorArray.Length != 3) return;

            var colorValues = new int[colorArray.Length];

            for (int i = 0; i < colorValues.Length; i++)
            {
                if (int.TryParse(colorArray[i], out colorValues[i])) continue;

                return;
            }

            var color = new Microsoft.Xna.Framework.Color(colorValues[0], colorValues[1], colorValues[2]);
            mapScene.owner.gameView.setBackGroundColor(Util.ToKmyColor(color));
            mapScene.mapDrawer.renderSettings.bgColor = color;
        }

        [BakinFunction(Description = "Set the current skybox by exact name \n example = \"sb_obj_sky_Night\"")]
        public void SetSkyBox(string value)
        {

            if (!(catalog.getFilteredItemList<GfxResourceBase>().FirstOrDefault(x => x.Name == value) is GfxResourceBase material)) return;

            // RenderSettings test = new RenderSettings();
            mapScene.mapDrawer.renderSettings.skyModel = material.guId;

            // mapScene.mapDrawer.renderSettings.bgMaterial = material.guId;
            mapScene.mapDrawer.reloadAsset(true, isChangeEnvironmentEffect: false);

            //bool func()
            //{
            //    if (!(catalog.getFullList().FirstOrDefault(x => x.Name == value) is GfxResourceBase material)) return false;

            //    // RenderSettings test = new RenderSettings();
            //    mapScene.mapDrawer.renderSettings.skyModel = material.guId;

            //    // mapScene.mapDrawer.renderSettings.bgMaterial = material.guId;
            //    mapScene.mapDrawer.reloadAsset(true, isChangeEnvironmentEffect: false);

            //    // mapScene.owner.gameView.setSkyBoxMaterial(test.bgMaterial);
            //    return false;
            //}

            //mapScene.owner.pushTask(func);
            // mapScene.mapDrawer.setRenderSettings(test);
        }

        [BakinFunction(Description = "Set the current skybox scale \n example = 0.8")]
        public void SetSkyBoxScale(float value)
        {
            if (value < 0) value = 0;
            else if (value > 100) value = 100;
            mapScene.mapDrawer.renderSettings.SkyModelScale = value;
            // mapScene.mapDrawer.setRenderSettings(test);
        }

        [BakinFunction(Description = "Set the enviroment map by exact name \n example = \"enviroment_Night001\"")]
        public void SetEnviromentMap(string value)
        {
            bool func()
            {
                var testing = catalog.getFilteredItemList<Texture>().Cast<Texture>().Where(x => x.getExtension().Contains(".HDR")).ToList();
                var enviromentList = new List<Texture>();

                foreach (var enviroment in testing)
                {
                    if (!enviroment.Name.Equals(value)) continue;

                    GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"Assigning enviroment map: {enviroment.Name}");
                    mapScene.mapDrawer.renderSettings.reflection = enviroment.guId;
                    mapScene.owner.gameView.setReflectionMap(enviroment.getTexture());
                    //  enviromentList.Add(enviroment);
                }

                return false;
            }

            mapScene.owner.pushTask(func);
        }

        [BakinFunction(Description = "Change EnviromentMap intensity \n Example: 1.2")]
        public void SetEnviromentMapIntensity(float value)
        {
            if (value < 0) value = 0;
            else if (value > 10000) value = 10000;

            mapScene.mapDrawer.renderSettings.ReflectionIntensity = value;
            mapScene.owner.gameView.setReflectionIntensity(value);
        }


        [BakinFunction(Description = "Set the current background material by exact name \n example = \"sb_obj_sky_Night\"")]
        public void SetBackgroundMaterial(string value)
        {
            bool func()
            {

                var material = catalog.getFilteredItemList<Material>().FirstOrDefault(x => x.Name == value) as Material;

                if (material.getMaterial().getShaderName().Contains("_skybox"))
                {
                    GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"Assigning bg material: {material.name}");
                    mapScene.mapDrawer.renderSettings.bgMaterial = material.guId;
                    mapScene.owner.gameView.setSkyBoxMaterial(material.guId);
                }

                return false;
            }

            mapScene.owner.pushTask(func);
        }

        [BakinFunction(Description = "Render functions")]
        public void __________RENDER_FUNCTIONS___________() { }

        [BakinFunction(Description = "Save current render settings, use loadTempRenderSettings to use these settings again")]
        public void SaveCurrentRenderSettings()
        {
            // CheckForData();

            var currentSettings = mapScene.mapDrawer.renderSettings.Clone();
            currentSettings.Name = "tempRender";

            RenderData.SaveRender(mapScene.mapDrawer.renderSettings);

            GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"{mapScene.mapDrawer.renderSettings.toTSV()}");

            //ExtraChunk test = new ExtraChunk(currenGuidData);
            //test.name = "specialDayData";
            //test.writeChunk(currentSettings);
            //catalog.addItem(test, Catalog.OVERWRITE_RULES.ALWAYS);
            //GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"Nombre del item {test.Name}");
            //superDataTest();
        }


        private void superDataTest()
        {
            bool error = false;
            int index = 0;


            int files = 0;
            var currentPath = "savedata" + Path.DirectorySeparatorChar;
            List<string> filesList = new List<string>();
            if (Directory.Exists(currentPath))
            {
                GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"EXISTE SAVEDATA");
                files = Directory.GetFiles(currentPath).Length;
                GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"Hay {files} save datas");
            }


            for (int i = 0; i < files; i++)
            {
                var loadedData = GameDataManager.Load(catalog, i);
                var currentData = loadedData.system.GetStrVariable("dayNightDataGuid", Guid.Empty, false);
                if (!string.IsNullOrEmpty(currentData))
                {

                    if (currentData == GameMain.instance.data.system.GetStrVariable("dayNightDataGuid", Guid.Empty, false))
                    {
                        GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"El index slot actual es {i}");
                    }

                    GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"Se encontró data especial");
                    if (!filesList.Contains(currentData)) filesList.Add(currentData);
                }
            }

            var toDelete = catalog.getFilteredItemList<ExtraChunk>().Where(x => x.Name == "specialDayData");

            foreach (var item in toDelete)
            {
                if (filesList.Contains(item.guId.ToString())) continue;

                catalog.deleteItem(item);
            }


            var extraChunks = catalog.getFilteredItemList<ExtraChunk>().Cast<ExtraChunk>().ToList();
            GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"Number of renders: {extraChunks.Count()}");

            mapScene.ShowMessage($"Number of renders: {extraChunks.Count()}", 0, MenuControllerBase.WindowTypes.MESSAGE, Guid.Empty);
        }



        [BakinFunction(Description = "Load stored render settings (variable to render)")]
        public void LoadTempRenderSettings()
        {
            RenderData.LoadRender();

            //CheckForData();

            //if (catalog.getFilteredItemList<ExtraChunk>().FirstOrDefault(x => x.guId == currenGuidData) is ExtraChunk settingsToLoad)
            //{
            //    RenderSettings renderToLoad = new RenderSettings();
            //    settingsToLoad.readChunk(renderToLoad);

            //    mapScene.mapDrawer.renderSettings = renderToLoad;
            //}
        }

        private void CheckForData()
        {
            var guidData = mapScene.owner.data.system.GetStrVariable("dayNightDataGuid", Guid.Empty, false);

            if (string.IsNullOrEmpty(guidData))
            {
                currenGuidData = Guid.NewGuid();
            }
            else
            {
                Guid.TryParse(guidData, out currenGuidData);
            }

            mapScene.owner.data.system.SetVariable("dayNightDataGuid", currenGuidData.ToString(), Guid.Empty, false);
        }

        // [BakinFunction(Description = "Lerp functions")]
        //public void ____LERP_FUNCTIONS____() { }


        //[BakinFunction(Description = "Lerp between current light and target light intensity, float \n Use -lightTime- variable to set the target time to finish the lerp.")]
        //public void LerpLightIntensity(float intensity)
        //{
        //    float time = 0f;
        //    float targetTime = (float)mapScene.owner.data.system.GetVariable("lightTime", Guid.Empty, false);
        //    var startIntensity = mapScene.mapDrawer.renderSettings.DirLightIntensity;
        //    var targetIntensity = intensity;
        //    bool lerpLight()
        //    {
        //        float interpolateFactor = time / targetTime;

        //        time += GameMain.getElapsedTime();

        //        interpolateFactor = MathHelper.Clamp(interpolateFactor, 0, 1f);

        //        var currentIntensity = MathHelper.Lerp(startIntensity, targetIntensity, interpolateFactor);

        //        mapScene.mapDrawer.renderSettings.DirLightIntensity = currentIntensity;

        //        if (interpolateFactor >= 1f)
        //        {

        //            mapScene.mapDrawer.renderSettings.DirLightIntensity = targetIntensity;
        //            return false;
        //        }

        //        return true;
        //    }

        //    if (mapScene.owner.hasTask("lerplight")) mapScene.owner.clearTask("lerplight");
        //    mapScene.owner.pushTask("lerplight", lerpLight);
        //}

        //[BakinFunction(Description = "Lerp between current color and target color string (R,G,B) \n Use -lightColorTime- variable to set the target time to finish the lerp.")]
        //public void LerpLightColor(string color)
        //{
        //    LerpStringColor(color, 0);
        //}

        //[BakinFunction(Description = "Lerp between current ambient light and target ambient light intensity, float \n Use -ambientTime- variable to set the target time to finish the lerp.")]
        //public void LerpAmbientIntensity(float intensity)
        //{
        //    float time = 0f;
        //    float targetTime = (float)mapScene.owner.data.system.GetVariable("ambientTime", Guid.Empty, false);
        //    var startIntensity = mapScene.mapDrawer.renderSettings.AmbLightIntensity;
        //    var targetIntensity = intensity;
        //    bool lerpLight()
        //    {
        //        float interpolateFactor = time / targetTime;

        //        time += GameMain.getElapsedTime();

        //        interpolateFactor = MathHelper.Clamp(interpolateFactor, 0, 1f);

        //        var currentIntensity = MathHelper.Lerp(startIntensity, targetIntensity, interpolateFactor);

        //        mapScene.mapDrawer.renderSettings.AmbLightIntensity = currentIntensity;

        //        if (interpolateFactor >= 1f)
        //        {
        //            mapScene.mapDrawer.renderSettings.AmbLightIntensity = targetIntensity;
        //            return false;
        //        }

        //        return true;
        //    }

        //    if (mapScene.owner.hasTask("lerpambient")) mapScene.owner.clearTask("lerpambient");
        //    mapScene.owner.pushTask("lerpambient", lerpLight);
        //}

        //[BakinFunction(Description = "Lerp between current color and target color string (R,G,B) \n Use -lightColorTime- variable to set the target time to finish the lerp.")]
        //public void LerpAmbientColor(string color)
        //{
        //    LerpStringColor(color, 1);
        //}
        private void LerpStringColor(string color, int type)
        {
            var taskName = type == 0 ? "lerpLightColor" : "lerpAmbientColor";
            var timeVar = type == 0 ? "lightColorTime" : "lightAmbientTime";
            float time = 0f;
            float targetTime = (float)mapScene.owner.data.system.GetVariable(timeVar, Guid.Empty, false);
            var startColor = type == 0 ? mapScene.mapDrawer.renderSettings.dirLightColor : mapScene.mapDrawer.renderSettings.ambLightColor;
            var targetColor = GetStringColor(color, out bool fail);

            if (fail) return;

            bool lerpColor()
            {
                float interpolateFactor = time / targetTime;

                time += GameMain.getElapsedTime();

                interpolateFactor = MathHelper.Clamp(interpolateFactor, 0, 1f);
                interpolateFactor = interpolateFactor * interpolateFactor * (3f - 2f * interpolateFactor);

                var currentColor = Microsoft.Xna.Framework.Color.Lerp(startColor, targetColor, interpolateFactor);

                if (type == 0)
                {
                    mapScene.mapDrawer.renderSettings.dirLightColor = currentColor;
                }
                else
                {
                    mapScene.mapDrawer.renderSettings.ambLightColor = currentColor;
                }

                if (interpolateFactor >= 1f)
                {
                    GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"Finalizing color lerp");
                    if (type == 0)
                    {
                        mapScene.mapDrawer.renderSettings.dirLightColor = targetColor;
                    }
                    else
                    {
                        mapScene.mapDrawer.renderSettings.ambLightColor = targetColor;
                    }
                    return false;
                }

                return true;
            }

            mapScene.owner.clearTask(taskName);
            mapScene.owner.pushTask(taskName, lerpColor);
        }

        //[BakinFunction(Description = "Lerp between current X angle and target angle for shadow rotation, float \n Use -xShadowTime- variable to set the target time to finish the lerp.")]
        //public void LerpShadowX(float angle)
        //{
        //    LerpShadow(angle, 0);
        //}
        //[BakinFunction(Description = "Lerp between current Y angle and target angle for shadow rotation, float \n Use -yShadowTime- variable to set the target time to finish the lerp.")]
        //public void LerpShadowY(float angle)
        //{
        //    LerpShadow(angle, 1);
        //}
        private void LerpShadow(float angle, int type)
        {
            var taskName = type == 0 ? "lerpShadowX" : "lerpShadowY";
            var timeVar = type == 0 ? "xShadowTime" : "yShadowTime";

            float time = 0f;
            float targetTime = (float)mapScene.owner.data.system.GetVariable(timeVar, Guid.Empty, false);
            var startAngle = mapScene.mapDrawer.renderSettings.shadowRotateX;
            var targetAngle = angle;
            bool lerpShadowX()
            {
                float interpolateFactor = time / targetTime;

                interpolateFactor = MathHelper.Clamp(interpolateFactor, 0, 1f);

                interpolateFactor = interpolateFactor * interpolateFactor * (3f - 2f * interpolateFactor);
                //  interpolateFactor = (float)Math.Tanh(2 * interpolateFactor - 1) / 2 + 0.5f;

                var currentShadowAngle = MathHelper.Lerp(startAngle, targetAngle, interpolateFactor);

                time += GameMain.getElapsedTime();
                if (type == 0)
                {
                    mapScene.mapDrawer.renderSettings.ShadowRotateX = currentShadowAngle;
                }
                else
                {
                    mapScene.mapDrawer.renderSettings.ShadowRotateY = currentShadowAngle;
                }

                if (interpolateFactor >= 1f)
                {
                    GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"Finalizing lerp shadow");
                    if (type == 0)
                    {
                        mapScene.mapDrawer.renderSettings.ShadowRotateX = targetAngle;
                    }
                    else
                    {
                        mapScene.mapDrawer.renderSettings.ShadowRotateY = targetAngle;
                    }
                    return false;
                }

                return true;
            }

            mapScene.owner.clearTask(taskName);
            mapScene.owner.pushTask(taskName, lerpShadowX);
        }

        internal void SetBuildingLight(bool buildingLights)
        {
            if (mapScene.mapDrawer.renderSettings.lightOnForBuildings == buildingLights) return;
            mapScene.mapDrawer.renderSettings.LightOnForBuildings = buildingLights;
            mapScene.mapDrawer.updateBuildingLight();
        }

        internal void SetFogColor(string value)
        {
            var color = GetStringColor(value, out bool fail);

            if (fail) return;
            mapScene.mapDrawer.renderSettings.FogEnable = true;
            mapScene.owner.gameView.setFogColor(Util.ToKmyColor(color));
            mapScene.mapDrawer.renderSettings.fogColor = color;
        }

        internal void SetFogIntensity(float value)
        {
            if (value < 0) value = 0;
            else if (value > 10000) value = 10000;

            mapScene.mapDrawer.renderSettings.FogEnable = true;
            mapScene.mapDrawer.renderSettings.FogIntensity = value;
        }

        internal void SetFogDensity(float value)
        {
            if (value < 0) value = 0;
            else if (value > 10000) value = 10000;

            mapScene.mapDrawer.renderSettings.FogEnable = true;
            mapScene.mapDrawer.renderSettings.FogDepthDensity = value;
        }

        internal void SetVignetteIntensity(float value)
        {
            if (value < 0) value = 0;
            else if (value > 10000) value = 10000;

            mapScene.mapDrawer.renderSettings.UseVIGNETTE = true;
            mapScene.mapDrawer.renderSettings.Vignette = value;
        }

    }
}

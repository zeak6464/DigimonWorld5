using System;
using System.Collections.Generic;
using Yukar.Common.Rom;
using Yukar.Engine;

namespace Bakin
{
    internal class RenderData
    {

        public static void SaveRender(RenderSettings render)
        {
            List<string> parameters = new List<string>
            {
                render.bgType.ToString(),
                render.skyModel.ToString(),
                render.bgColor.PackedValue.ToString(),
                render.reflection.ToString(),
                render.reflectionIntensity.ToString(),
                render.dirLightColor.ToString(),
                render.dirLightIntensity.ToString(),
                render.shadowRotateX.ToString(),
                render.shadowRotateY.ToString(),
                render.shadowBias.ToString(),
                render.shadowDistance.ToString(),
                render.shadowMargin.ToString(),
                render.lightOnForBuildings.ToString(),
                render.ambLightColor.PackedValue.ToString(),
                render.iblIntensity.ToString(),
                render.fogColor.PackedValue.ToString(),
                render.fogStart.ToString(),
                render.fogDepthDensity.ToString(),
                render.fogHeightFallOff.ToString(),
                render.fogApply.ToString(),
                render.useBloom.ToString(),
                render.bloomApply.ToString(),
                render.highlightThreshold.ToString(),
                render.useDof.ToString(),
                render.dofSmoothStart.ToString(),
                render.dofSmoothRange.ToString(),
                render.dofRadius.ToString(),
                render.useVignette.ToString(),
                render.vignette.ToString(),
                render.lut.ToString(),
                render.useSSAO.ToString(),
                render.ssaoColor.ToString(),
                render.ssaoRadius.ToString(),
                render.ssaoMinLimit.ToString(),
                render.ssaoMaxLimit.ToString(),
                render.ssaoShadowScale.ToString(),
                render.ssaoSampleCount.ToString(),
                render.ssaoContrast.ToString(),
                render.useDeferred.ToString(),
                render.useChromaticAbberation.ToString(),
                render.chromaticAbberationSize.ToString(),
                render.useAutoExposure.ToString(),
                render.exposureTargetBrightness.ToString(),
                render.exposureMinScale.ToString(),
                render.exposureMaxScale.ToString(),
                render.useSSSSS.ToString(),
                render.sssssRadius.ToString(),
                render.sssssApply.ToString(),
                render.sssssSampleCount.ToString(),
                render.fogEnable.ToString(),
                render.bgMaterial.ToString(),
                render.fogIntensity.ToString(),
                render.ambientIntensity.ToString(),
                render.skyModelScale.ToString(),
                render.version.ToString(),
                render.billboardLightModulation.ToString(),
                render.shadowCascadeCount.ToString(),
                render.shadowCascadeStep.X.ToString(),
                render.shadowCascadeStep.Y.ToString(),
                render.shadowCascadeStep.Z.ToString()
            };

            string finalData = "";
            for (int i = 0; i < parameters.Count; i++)
            {
                finalData += parameters[i] + "|";

                // mapScene.owner.data.system.SetToArray("tempRender", i, parameters[i]);
            }
            GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"Saving render settings");
            GameMain.instance.data.system.SetVariable("tempRender", finalData, Guid.Empty, false);
        }
        public static void LoadRender()
        {

            var tempRender = GameMain.instance.data.system.GetStrVariable("tempRender", Guid.Empty, false).Split('|');

            GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"Tamaño: {tempRender.Length}");

            if (tempRender.Length < 60) return;

            GameMain.PushLog(DebugDialog.LogEntry.LogType.EVENT, "DEBUG", $"Loading render setting");

            var index = 0;
            string[] parameters = tempRender;
            var curretRender = GameMain.instance.mapScene.mapDrawer.renderSettings;

            Enum.TryParse(parameters[index++], out curretRender.bgType);
            Guid.TryParse(parameters[index++], out curretRender.skyModel);
            uint.TryParse(parameters[index++], out uint bgColor); curretRender.bgColor.PackedValue = bgColor;
            Guid.TryParse(parameters[index++], out curretRender.reflection);
            float.TryParse(parameters[index++], out curretRender.reflectionIntensity);
            uint.TryParse(parameters[index++], out uint dirLightColor); curretRender.dirLightColor.PackedValue = dirLightColor;
            float.TryParse(parameters[index++], out curretRender.dirLightIntensity);
            float.TryParse(parameters[index++], out curretRender.shadowRotateX);
            float.TryParse(parameters[index++], out curretRender.shadowRotateY);
            float.TryParse(parameters[index++], out curretRender.shadowBias);
            float.TryParse(parameters[index++], out curretRender.shadowDistance);
            float.TryParse(parameters[index++], out curretRender.shadowMargin);
            bool.TryParse(parameters[index++], out curretRender.lightOnForBuildings);
            uint.TryParse(parameters[index++], out uint ambLightColor); curretRender.ambLightColor.PackedValue = ambLightColor;
            float.TryParse(parameters[index++], out curretRender.iblIntensity);
            uint.TryParse(parameters[index++], out uint fogColor); curretRender.fogColor.PackedValue = fogColor;
            float.TryParse(parameters[index++], out curretRender.fogStart);
            float.TryParse(parameters[index++], out curretRender.fogDepthDensity);
            float.TryParse(parameters[index++], out curretRender.fogHeightFallOff);
            float.TryParse(parameters[index++], out curretRender.fogApply);
            bool.TryParse(parameters[index++], out curretRender.useBloom);
            float.TryParse(parameters[index++], out curretRender.bloomApply);
            float.TryParse(parameters[index++], out curretRender.highlightThreshold);
            bool.TryParse(parameters[index++], out curretRender.useDof);
            float.TryParse(parameters[index++], out curretRender.dofSmoothStart);
            float.TryParse(parameters[index++], out curretRender.dofSmoothRange);
            float.TryParse(parameters[index++], out curretRender.dofRadius);
            bool.TryParse(parameters[index++], out curretRender.useVignette);
            float.TryParse(parameters[index++], out curretRender.vignette);
            Guid.TryParse(parameters[index++], out curretRender.lut);
            bool.TryParse(parameters[index++], out curretRender.useSSAO);
            uint.TryParse(parameters[index++], out uint ssaoColor); curretRender.ssaoColor.PackedValue = ssaoColor;
            float.TryParse(parameters[index++], out curretRender.ssaoRadius);
            float.TryParse(parameters[index++], out curretRender.ssaoMinLimit);
            float.TryParse(parameters[index++], out curretRender.ssaoMaxLimit);
            float.TryParse(parameters[index++], out curretRender.ssaoShadowScale);
            int.TryParse(parameters[index++], out curretRender.ssaoSampleCount);
            float.TryParse(parameters[index++], out curretRender.ssaoContrast);
            bool.TryParse(parameters[index++], out curretRender.useDeferred);
            bool.TryParse(parameters[index++], out curretRender.useChromaticAbberation);
            float.TryParse(parameters[index++], out curretRender.chromaticAbberationSize);
            bool.TryParse(parameters[index++], out curretRender.useAutoExposure);
            float.TryParse(parameters[index++], out curretRender.exposureTargetBrightness);
            float.TryParse(parameters[index++], out curretRender.exposureMinScale);
            float.TryParse(parameters[index++], out curretRender.exposureMaxScale);
            bool.TryParse(parameters[index++], out curretRender.useSSSSS);
            float.TryParse(parameters[index++], out curretRender.sssssRadius);
            float.TryParse(parameters[index++], out curretRender.sssssApply);
            float.TryParse(parameters[index++], out curretRender.sssssSampleCount);
            bool.TryParse(parameters[index++], out curretRender.fogEnable);
            Guid.TryParse(parameters[index++], out curretRender.bgMaterial);
            float.TryParse(parameters[index++], out curretRender.fogIntensity);
            float.TryParse(parameters[index++], out curretRender.ambientIntensity);
            float.TryParse(parameters[index++], out curretRender.skyModelScale);
            int.TryParse(parameters[index++], out curretRender.version);
            float.TryParse(parameters[index++], out curretRender.billboardLightModulation);
            int.TryParse(parameters[index++], out curretRender.shadowCascadeCount);
            float.TryParse(parameters[index++], out curretRender.shadowCascadeStep.X);
            float.TryParse(parameters[index++], out curretRender.shadowCascadeStep.Y);
            float.TryParse(parameters[index++], out curretRender.shadowCascadeStep.Z);

            GameMain.instance.mapScene.mapDrawer.setRenderSettings(curretRender);
        }

    }
}

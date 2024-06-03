using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Yukar.Common;
using Yukar.Common.Resource;
using Yukar.Common.Rom;
using Yukar.Engine;

namespace Bakin
{
    public class OutlineManager : BakinObject
    {
        List<Material> ogMaterial = new List<Material>();
        List<Material> newMaterial = new List<Material>();
        public override void Start()
        {
            // キャラクターが生成される時に、このメソッドがコールされます。
            // This method is called when the character is created.
        }

        public override void Update()
        {
            // キャラクターが生存している間、
            // 毎フレームこのキャラクターのアップデート前にこのメソッドがコールされます。
            // This method is called every frame before this character updates while the character is alive.
        }

        public override void FixedUpdate()
        {
            // キャラクターが生存している間、
            // 物理エンジンのアップデートに同期してこのメソッドが毎秒必ず60回コールされます。
            // This method will be called 60 times every second, synchronously with physics engine updates while the character is alive.
        }

        public override void BeforeUpdate()
        {
            // キャラクターが生存している間、
            // 毎フレーム、イベント内容の実行前にこのメソッドがコールされます。
            // This method will be called every frame while the character is alive, before the event content is executed.
        }

        public override void Destroy()
        {
            var instance = GetRightInstance();
            if (instance == null) return;

            var matCount = instance?.getMaterialCount();
            if (matCount == null) return;

            for (int i = 0; i < matCount.Value; i++)
            {
                if (ogMaterial.Count <= i) break;
                if (!(ogMaterial[i]?.getMaterial() is SharpKmyGfx.Material matToUse)) continue;
                instance?.setMaterial(i, matToUse);
            }
            // キャラクターが破棄される時に、このメソッドがコールされます。
            // This method is called when the character is destroyed.
        }

        public override void AfterDraw()
        {
            // このフレームの2D描画処理の最後に、このメソッドがコールされます。
            // This method is called at the end of the 2D drawing process for this frame.
        }


        [BakinFunction(Description = "1 = true, 0 = false")]
        public void UseOutline(int value)
        {

            if (!CheckMaterial()) return;

            var instance = GetRightInstance();
            if (instance == null) return;
         
            var matCount = instance?.getMaterialCount();
            if (matCount == null) return;

            for (int i = 0; i < matCount.Value; i++)
            {
                instance?.getMaterialInstance((uint)i)?.
                    getBaseMaterial()?.
                    setDrawOutlineEnable(value != 0);

                instance?.getMaterialInstance((uint)i)?.
                getBaseMaterial()?.
                setOverrideOutlineSetting(value != 0);      
            }
        }

        [BakinFunction(Description = "Set the outline thickness")]
        public void SetOutlineThickness(float value)
        {
            if (!CheckMaterial()) return;

            var instance = GetRightInstance();
            if (instance == null) return;

            var matCount = instance?.getMaterialCount();
            if (matCount == null) return;

            for (int i = 0; i < matCount.Value; i++)
            {
                instance?.getMaterialInstance((uint)i)?.
                    getBaseMaterial()?.
                    setOutlineWidth(value);
            }

        }

        [BakinFunction(Description = "Set current outline color.\n Digits HEX color \n Example: \"FF0000ff\" ")]
        public void SetOutlineColor(string color)
        {
            if (!CheckMaterial()) return;   
            var instance = GetRightInstance();
                           
            if (instance == null) return; 
            var matCount = instance?.getMaterialCount();
            if (matCount == null) return;    
            var value = HexToColor(color);
            for (int i = 0; i < matCount.Value; i++)
            {
                instance?.getMaterialInstance((uint)i)?.
                    getBaseMaterial()?.
                    setOutlineColor(Util.ToKmyColor(value));
            }
        }

        private SharpKmyGfx.ModelInstance GetRightInstance()
        {
           return mapChr.isCommonEvent ? mapScene?.GetHero()?.getModelInstance() : mapChr?.getModelInstance();
        }

        [BakinFunction(Description = "Reset the material color")]
        public void ResetMaterialColor()
        {
            if (!CheckMaterial()) return;
            var instance = GetRightInstance();
            if (instance == null) return;

            var matCount = instance?.getMaterialCount();
            if (matCount == null) return;

            for (int i = 0; i < matCount.Value; i++)
            {
                var matToUse = ogMaterial[i]?.getMaterial();
                if (matToUse == null) continue;
                instance?.setMaterial(i, matToUse);
            }

        }
        private bool CheckMaterial()
        {
            if (ogMaterial.Count != 0) return true;
       
            var instance = mapChr.isCommonEvent ? mapScene.GetHero() : mapChr;

            if (!(instance?.getGraphic() is GfxResourceBase graphic)) return false;

            if (graphic.materialSet.list.Count == 0) return false;

            for (int i = 0; i < graphic.materialSet.list.Count; i++)
            {
                var mat = catalog.getItemFromGuid<Material>(graphic.materialSet.list[i].guid);

                ogMaterial.Add(mat);
                newMaterial.Add(RomItem.Clone<Material>(mat));

                //  graphic.materialSet.setMaterial(i, newMaterial[i], false); ;
                //  mapChr.getModelInstance().getModel().setMaterialGUID(i, ogMaterial[i].guId);
                var matToUse = newMaterial[i]?.getMaterial();
                if (matToUse == null) continue;

                instance.getModelInstance().setMaterial(i, matToUse);
            }

            return true;
        }

        private Color HexToColor(string rgba)
        {
            rgba = rgba.Replace("#", "");
            if(rgba.Length < 6) return Color.Black;

            if (rgba.Length == 6) rgba += "FF";

            try
            {
                ulong i_rgba = ulong.Parse(rgba, System.Globalization.NumberStyles.HexNumber);
                float i_r = (float)(i_rgba % 0x100000000 / 0x1000000) / 0xFF;
                float i_g = (float)(i_rgba % 0x1000000 / 0x10000) / 0xFF;
                float i_b = (float)(i_rgba % 0x10000 / 0x100) / 0xFF;
                float i_a = (float)(i_rgba % 0x100) / 0xFF;
                return new Color((byte)(i_r * 255), (byte)(i_g * 255), (byte)(i_b * 255), (byte)(i_a * 254)); // R,G,B,A で指定する Aは254まで
            }
            catch (Exception)
            {
                return Color.Black;
            }

        }
    }
}

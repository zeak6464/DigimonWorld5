// Version 20230924

// Import external files
// Note: Make sure to import ExtensionA!
// @@include ExtensionA_Stencil.cs
// @@include ExtensionA_Distance.cs
// @@include ExtensionA_NormalizedVector.cs
// @@include ExtensionA_Arctangent.cs
// @@include ExtensionA_IsVisible.cs

using System;
using Yukar.Common;
using Yukar.Engine;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;

// Use namespaces from external files
using Stencil;
using Distance;
using NormalizedVector;
using Arctangent;
using IsVisible;

namespace Bakin
{
    public class MainScriptForCast : BakinObject // Make sure the class name matches the file name
    {
        string GUID; // Member variable for GUID retrieval

        public override void Start()
        {
            // This method is called when the character is created.

            // GUID = mapChr.guId.ToString(); // Get the GUID
            // GameMain.instance.data.system.SetVariable("GUID", GUID, new Guid(GUID), false); // Assign the GUID to a local variable
        }

        public override void Update()
        {
            // This method is called every frame before this character updates while the character is alive.

            /****************************************************************************************************
             * Screen Coordinates (In Development)
             ****************************************************************************************************/
            /*
            int ScreenPosX = 0;
            int ScreenPosY = 0;
            float result = 0;
            var ScreenWidth = Graphics.ScreenWidth;
            var ScreenHeight = Graphics.ScreenHeight;

            MapScene.EffectPosType pos = MapScene.EffectPosType.Body;
            // mapScene.GetCharacterScreenPos(mapScene.hero,out x,out y, pos); // Verified operation
            mapScene.GetCharacterScreenPos(mapChr, out ScreenPosX, out ScreenPosY, pos); //
            GameMain.instance.data.system.SetVariable("ScreenX", ScreenPosX);
            GameMain.instance.data.system.SetVariable("ScreenY", ScreenPosY);

            if (ScreenPosX >= 0 && ScreenPosX <= ScreenWidth && ScreenPosY >= 0 && ScreenPosY <= ScreenHeight) result = 1;
            else result = 0;
            GameMain.instance.data.system.SetVariable("result", result);
            */
        }

        public override void BeforeUpdate()
        {
            // This method will be called every frame while the character is alive, before the event content is executed.
        }

        public override void Destroy()
        {
            // This method is called when the character is destroyed.
        }

        public override void AfterDraw()
        {
            // This method is called at the end of the 2D drawing process for this frame.
        }

        [BakinFunction(Description = "Select a command.\nCommand types will be added continuously.\nVer.20230924\nThis program was written by SANA")]
        public void ___Command_List___() { }

        [BakinFunction(Description = "The following commands are related to vectors.")]
        public void ___Vector_Category___() { }

        [BakinFunction(Description = "Calculate the 3D distance from the player to this event.\n3D distance considers X, Z, and Y coordinates.")]
        public float Distance3D()
        {
            var PlayerPos = mapScene.hero.getPosition();
            var ThisCastPos = mapChr.getPosition();
            DistanceClass Func = new DistanceClass();
            return Func.Distance3D(PlayerPos, ThisCastPos);
        }

        [BakinFunction(Description = "Calculate the 2D distance from the player to this event.\n2D distance considers only X and Z coordinates.")]
        public float Distance2D()
        {
            var PlayerPos = mapScene.hero.getPosition();
            var ThisCastPos = mapChr.getPosition();
            DistanceClass Func = new DistanceClass();
            return Func.Distance2D(PlayerPos, ThisCastPos);
        }

        [BakinFunction(Description = "Calculate the X component of a 3D normalized vector.\nArguments: Greater than or equal to 0 - Start: Player, End: This event\nLess than 0 - Start: This event, End: Player")]
        public float NormalizedVector3D_X(float sw)
        {
            var PlayerPos = mapScene.hero.getPosition();
            var ThisCastPos = mapChr.getPosition();
            NormalizedVectorClass Func = new NormalizedVectorClass();
            var NV = Func.NormalizedVector3D_X(ThisCastPos, PlayerPos);
            if (sw < 0) NV *= -1;
            return NV;
        }

        [BakinFunction(Description = "Calculate the Z component of a 3D normalized vector.\nArguments: Greater than or equal to 0 - Start: Player, End: This event\nLess than 0 - Start: This event, End: Player")]
        public float NormalizedVector3D_Z(float sw)
        {
            var PlayerPos = mapScene.hero.getPosition();
            var ThisCastPos = mapChr.getPosition();
            NormalizedVectorClass Func = new NormalizedVectorClass();
            var NV = Func.NormalizedVector3D_Z(ThisCastPos, PlayerPos);
            if (sw < 0) NV *= -1;
            return NV;
        }

        [BakinFunction(Description = "Calculate the Y component of a 3D normalized vector.\nArguments: Greater than or equal to 0 - Start: Player, End: This event\nLess than 0 - Start: This event, End: Player")]
        public float NormalizedVector3D_Y(float sw)
        {
            var PlayerPos = mapScene.hero.getPosition();
            var ThisCastPos = mapChr.getPosition();
            NormalizedVectorClass Func = new NormalizedVectorClass();
            var NV = Func.NormalizedVector3D_Y(ThisCastPos, PlayerPos);
            if (sw < 0) NV *= -1;
            return NV;
        }

        [BakinFunction(Description = "Calculate the X component of a 2D normalized vector.\nArguments: Greater than or equal to 0 - Start: Player, End: This event\nLess than 0 - Start: This event, End: Player")]
        public float NormalizedVector2D_X(float sw)
        {
            var PlayerPos = mapScene.hero.getPosition();
            var ThisCastPos = mapChr.getPosition();
            NormalizedVectorClass Func = new NormalizedVectorClass();
            var NV = Func.NormalizedVector2D_X(ThisCastPos, PlayerPos);
            if (sw < 0) NV *= -1;
            return NV;
        }

        [BakinFunction(Description = "Calculate the Z component of a 2D normalized vector.\nArguments: Greater than or equal to 0 - Start: Player, End: This event\nLess than 0 - Start: This event, End: Player")]
        public float NormalizedVector2D_Z(float sw)
        {
            var PlayerPos = mapScene.hero.getPosition();
            var ThisCastPos = mapChr.getPosition();
            NormalizedVectorClass Func = new NormalizedVectorClass();
            var NV = Func.NormalizedVector2D_Z(ThisCastPos, PlayerPos);
            if (sw < 0) NV *= -1;
            return NV;
        }

        [BakinFunction(Description = "Calculate the horizontal angle.\nArguments: Greater than or equal to 0 - Start: Player, End: This event\nLess than 0 - Start: This event, End: Player")]
        public float HorizontalAngle(float sw)
        {
            var PlayerPos = mapScene.hero.getPosition();
            var ThisCastPos = mapChr.getPosition();
            NormalizedVectorClass FuncA = new NormalizedVectorClass();
            var x = FuncA.NormalizedVector2D_X(ThisCastPos, PlayerPos);
            var z = FuncA.NormalizedVector2D_Z(ThisCastPos, PlayerPos);
            ArctangentClass FuncB = new ArctangentClass();
            var NV = FuncB.HorizontalAngle(x, z);
            if (sw < 0) NV = FuncB.HorizontalAngle(x * -1, z * -1);
            return NV;
        }

        [BakinFunction(Description = "Calculate the vertical angle.\nArguments: Greater than or equal to 0 - Start: Player, End: This event\nLess than 0 - Start: This event, End: Player")]
        public float VerticalAngle(float sw)
        {
            var PlayerPos = mapScene.hero.getPosition();
            var ThisCastPos = mapChr.getPosition();
            ArctangentClass Func = new ArctangentClass();
            var NV = Func.VerticalAngle(ThisCastPos, PlayerPos);
            if (sw < 0) NV = Func.VerticalAngle(PlayerPos, ThisCastPos);
            return NV;
        }

        [BakinFunction(Description = "The following commands are related to the camera.")]
        public void ___Camera_Category___() { }

        [BakinFunction(Description = "Check if this event is visible on the screen.\nVisible on screen: Result 1\nNot visible on screen: Result 0")]
        public float IsVisible()
        {
            int ScreenPosX = 0;
            int ScreenPosY = 0;
            mapScene.GetCharacterScreenPos(mapChr, out ScreenPosX, out ScreenPosY, MapScene.EffectPosType.Body);
            IsVisibleClass Func = new IsVisibleClass();
            return Func.IsVisible(ScreenPosX, ScreenPosY);
        }

        [BakinFunction(Description = "The following commands are related to graphics.")]
        public void ___Graphics_Category___() { }

        [BakinFunction(Description = "Display a silhouette when this event is hidden behind an object or terrain.\nArgument greater than 1: Show silhouette\nArgument less than or equal to 0: Hide silhouette")]
        public void Stencil(float sw)
        {
            StencilClass Func = new StencilClass();
            MapCharacter evt = mapChr;
            Func.Stencil(evt, sw);

            if (sw > 0)
            {
                evt.setHeroSymbol(true); // Show the silhouette
            }
            else
            {
                mapChr.setHeroSymbol(false); // Hide the silhouette
            }
        }
    }
}

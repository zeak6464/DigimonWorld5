using System;
// using Microsoft.Xna;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Yukar.Engine;

namespace Bakin
{
    public class FpView : BakinObject
    {

        // -------------------------------------- //
        // FpView 0.8.1
        // By Jagonz  
        // More scripts and plugins: https://jagonz.itch.io
        // Bakin tutorials: https://www.youtube.com/@JagonzBakin
        // -------------------------------------- //

        // ==== ==== Settings ==== ====  //

        bool activateScript = true;
        bool useDashBugfix = true; // WARNING: This field must be "true" until Smile Boom fixes a bug that slows the camera when the player is using "Dash" button

        string disableFpCameraSwitch = "disableFpCamera"; // Turn on a switch with this name to disable the script, (turn off to enable it again).
        string disablePlayerAutoRotation = "disableAutoRotation"; // Turn on a switch with this name to disable the player auto rotation with the camera, (turn off to enable it again).

        float verticalLimit = 83f;  // How much the player can look up and down in degrees
                                    // bool useFpJoystick = false; // Use settings from this script in a joystick (this always works)

        // ==== Advanced settings ==== // 

        bool useDynamicSensitivity = true; // set this field to true if you want to add the possibility of change sensitivity at runtime inside the game.

        string updateSensSwitch = "updateSensitivity"; // Turn on this switch in Bakin when you want to update the sensitivity in game.

        string xSensitivityVariable = "xSensitivity";  // Name of the variable to store the X sensitivity in Bakin (Only works if useDynamicSensitivity it's true)
        string ySensitivityVariable = "ySensitivity";  // Name of the variable to store the Y sensitivity in Bakin (Only works if useDynamicSensitivity it's true)

        string flipHorizontalCameraSwitch = "flip horizontal camera"; // If you turn on a switch with the same name as this var in bakin the camera will flip it's movement
        string flipVerticalCameraSwitch = "flip vertical camera";     // to flip the camera it's necesary to you use "useDynamicSensitivity = true" and turn on "updateSensSwitch"


        string xJoistickSensVariable = "xJoystickSensitivity";  // Name of the variable to store the X sensitivity of a controller in Bakin (Only works if useDynamicSensitivity it's true)
        string yJoistickSensVariable = "yJoystickSensitivity";  // Name of the variable to store the Y sensitivity of a controller in Bakin (Only works if useDynamicSensitivity it's true)
        string flipHorizontalJoystickSwitch = "flip horizontal joystick";
        string flipVerticalJoystickSwitch = "flip vertical joystick";

        // ============================================ //

        Screen primaryScreen;
        private Rectangle scSize;
        private MapCharacter mainCharacter;
        private Microsoft.Xna.Framework.Vector3 charRotation;
        private float xSensitivity, ySensitivity;
        private float xJoystickSens, yJoystickSens;
        private bool sensInitialized;
        private int waitFrames;


        private float bugFixMultiplier = 3f;


        public float XSENSITIVITY
        {
            get { return xSensitivity; }
            set { xSensitivity = value; }
        }
        public float YSENSITIVITY
        {
            get { return ySensitivity; }
            set { ySensitivity = value; }
        }
        public override void Start()
        {
            primaryScreen = Screen.PrimaryScreen;
            scSize = primaryScreen.Bounds;
            // キャラクターが生成される時に、このメソッドがコールされます。
            // This method is called when the character is created.
        }

        public override void Update()
        {
            if (mapScene == null || !activateScript)
            {
                mapScene = GameMain.instance.mapScene;
                return;
            }
           
            if (mapScene.owner.data.system.GetSwitch(disableFpCameraSwitch)) return;
            if (!sensInitialized) { InitializeSens(); return; }

            if (mapScene.owner.data.system.GetSwitch(updateSensSwitch) && useDynamicSensitivity) UpdateSensSettings();

            SensitivityCheck();

            if (mapScene.mapFixCamera) waitFrames += 1;

          //  CameraFix();
            // キャラクターが生存している間、
            // 毎フレームこのキャラクターのアップデート前にこのメソッドがコールされます。
            // This method is called every frame before this character updates while the character is alive.
        }



        public override void AfterDraw()
        {
            if (mapScene == null || !activateScript) return;
            if (mapScene.owner.data.system.GetSwitch(disableFpCameraSwitch)) return;
            if (!mapScene.owner.data.system.GetSwitch(disablePlayerAutoRotation)) PlayerRotation();

            CameraFix();

            // このフレームの2D描画処理の最後に、このメソッドがコールされます。
            // This method is called at the end of the 2D drawing process for this frame.
        }
        private void SensitivityCheck()
        {
            bugFixMultiplier = useDashBugfix ? 3f : 1f;
            mapScene.owner.catalog.getGameSettings().MouseHorz = Input.KeyTest(0, Input.KeyStates.DASH, 0) ? xSensitivity * bugFixMultiplier : xSensitivity;
            mapScene.owner.catalog.getGameSettings().MouseVert = Input.KeyTest(0, Input.KeyStates.DASH, 0) ? ySensitivity * bugFixMultiplier : ySensitivity;

            mapScene.owner.catalog.getGameSettings().RStickHorz = Input.KeyTest(0, Input.KeyStates.DASH, 0) ? xJoystickSens * bugFixMultiplier : xJoystickSens;
            mapScene.owner.catalog.getGameSettings().RStickVert = Input.KeyTest(0, Input.KeyStates.DASH, 0) ? yJoystickSens * bugFixMultiplier : yJoystickSens;
        }

        private void InitializeSens()
        {

            xSensitivity = !useDynamicSensitivity ? mapScene.owner.catalog.getGameSettings().MouseHorz : (float)mapScene.owner.data.system.GetVariable(xSensitivityVariable);
            ySensitivity = !useDynamicSensitivity ? mapScene.owner.catalog.getGameSettings().MouseVert : (float)mapScene.owner.data.system.GetVariable(ySensitivityVariable);
            xJoystickSens = !useDynamicSensitivity ? mapScene.owner.catalog.getGameSettings().RStickHorz : (float)mapScene.owner.data.system.GetVariable(xJoistickSensVariable);
            yJoystickSens = !useDynamicSensitivity ? mapScene.owner.catalog.getGameSettings().RStickHorz : (float)mapScene.owner.data.system.GetVariable(yJoistickSensVariable);

            mapScene.owner.catalog.getGameSettings().CameraLimiterEnableVertical = true;
            mapScene.owner.catalog.getGameSettings().CameraLimiterVerticalMin = -verticalLimit;
            mapScene.owner.catalog.getGameSettings().CameraLimiterVerticalMax = verticalLimit;

            sensInitialized = true;
        }
        private void UpdateSensSettings()
        {
 
            xSensitivity = (float)mapScene.owner.data.system.GetVariable(xSensitivityVariable);
            ySensitivity = (float)mapScene.owner.data.system.GetVariable(ySensitivityVariable);
            xJoystickSens = (float)mapScene.owner.data.system.GetVariable(xJoistickSensVariable);
            yJoystickSens = (float)mapScene.owner.data.system.GetVariable(yJoistickSensVariable);

            mapScene.owner.catalog.getGameSettings().MouseHorz = xSensitivity;
            mapScene.owner.catalog.getGameSettings().MouseVert = ySensitivity;

            mapScene.owner.catalog.getGameSettings().RStickHorz = xJoystickSens;
            mapScene.owner.catalog.getGameSettings().RStickHorz = yJoystickSens;
            flipCamera();


            mapScene.owner.data.system.SetSwitch(updateSensSwitch, false);
        }

        private void CursorPositionX(int X) { Cursor.Position = new Point(X, Cursor.Position.Y); }


        private void CursorPositionY(int Y) { Cursor.Position = new Point(Cursor.Position.X, Y); }


        private void CameraFix()
        {

            if (IsOutOfLimits() && GameIsFocus())
            {
                mapScene.mapFixCamera = true;
                CursorPositionX(scSize.Width / 2);
                CursorPositionY(scSize.Height / 2);
            }

            if (waitFrames >= 1 && !IsOutOfLimits())
            {
                mapScene.mapFixCamera = false; 
                waitFrames = 0;
            }
        }

        private bool IsOutOfLimits()
        {
            if ((Cursor.Position.X <= scSize.Left + 4 || Cursor.Position.X >= scSize.Right - 4
                || Cursor.Position.Y <= scSize.Top + 4 || Cursor.Position.Y >= scSize.Bottom - 4))
            {
                return true;
            }
            return false;
        }
        private void flipCamera()
        {
            mapScene.owner.catalog.getGameSettings().InvertMouseHorz = mapScene.owner.data.system.GetSwitch(flipHorizontalCameraSwitch);
            mapScene.owner.catalog.getGameSettings().InvertRStickHorz = mapScene.owner.data.system.GetSwitch(flipHorizontalJoystickSwitch);
            mapScene.owner.catalog.getGameSettings().InvertMouseVert = mapScene.owner.data.system.GetSwitch(flipVerticalCameraSwitch);
            mapScene.owner.catalog.getGameSettings().InvertRStickVert = mapScene.owner.data.system.GetSwitch(flipVerticalJoystickSwitch);
        }
        private void PlayerRotation()
        {

            if (mainCharacter == null || mainCharacter != mapScene.GetHero())
            {
                mainCharacter = mapScene.GetHero();
                return;
            }

            charRotation = mainCharacter.getRotation();
            charRotation.Y = mapScene.yAngle - 180f;
            mainCharacter.setRotation(charRotation);

        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        public static bool GameIsFocus()
        {
            var activatedHandle = GetForegroundWindow();
            if (activatedHandle == IntPtr.Zero)
            {
                return false;
            }

            var procId = Process.GetCurrentProcess().Id;
            int activeProcId;
            GetWindowThreadProcessId(activatedHandle, out activeProcId);

            return activeProcId == procId;
        }
    }
}

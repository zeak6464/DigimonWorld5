using System;

namespace SharpKmy
{
    public class Entry
    {
        internal static int getMainWindowWidth()
        {
            return UnityEngine.Screen.width;
        }

        internal static int getMainWindowHeight()
        {
            return UnityEngine.Screen.height;
        }

        internal static void fullScreenMode(bool v)
        {
            // Dummy
        }

        internal static float getMainWindowAspect()
        {
            return (float)UnityEngine.Screen.width / (float)UnityEngine.Screen.height;
        }

        internal static bool isFullScreenMode()
        {
            // Dummy
            return false;
        }

        internal static uint getGfxErrorFlag()
        {
            // Dummy
            return 0;
        }

        internal static bool isWindowActive()
        {
            // Dummy
            return true;
        }

        internal static void gfxMemoryCleanup()
        {
            // StaticModelBatcher ‚É“o˜^‚³‚ê‚½object‚ÌƒŠƒXƒg‚ðƒNƒŠƒA‚·‚é
            SharpKmyGfx.StaticModelBatcher.instances.Clear();

            UnityEngine.Resources.UnloadUnusedAssets();
        }

        internal static float getFrameElapsed()
        {
            return UnityEngine.Time.deltaTime * 60 / 1000;
        }

        internal static float getMramFreeSize()
        {
            return 64 * 1024 * 1024;    // Dummy
        }

        internal static float getMramSize()
        {
            return 64 * 1024 * 1024;    // Dummy
        }

        internal static string getDrawCallCount()
        {
            return "100";    // Dummy
        }

        internal static uint getSndErrorFlag()
        {
            // Dummy
            return 0;
        }
    }
}
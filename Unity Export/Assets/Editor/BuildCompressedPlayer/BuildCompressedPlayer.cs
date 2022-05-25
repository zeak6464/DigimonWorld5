using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BuildCompressedPlayer
{
    [MenuItem("Build/StandaloneWindows")]
    private static void Build_StandaloneWindows()
    {
        Debug.Log("##########StandaloneWindows Build Start#########");
        CustomImporter.CreateScenes();

        BuildPipeline.BuildPlayer(
            EditorBuildSettings.scenes,
            "Build/StandaloneWindows",
            BuildTarget.StandaloneWindows,
#if UNITY_2018_1_OR_NEWER
            BuildOptions.CompressWithLz4
#else
            BuildOptions.CompressWithLz4 | BuildOptions.Il2CPP
#endif
        );
    }

    [MenuItem("Build/Android")]
    private static void Build_Android()
    {
        Debug.Log("##########Android Build Start#########");
        CustomImporter.CreateScenes();

        BuildPipeline.BuildPlayer(
            EditorBuildSettings.scenes,
            "Build/Android/export.apk",
            BuildTarget.Android,
#if UNITY_2018_1_OR_NEWER
            BuildOptions.CompressWithLz4
#else
            BuildOptions.CompressWithLz4 | BuildOptions.Il2CPP
#endif
        );
    }

    [MenuItem("Build/iOS")]
    private static void Build_iOS()
    {
        Debug.Log("##########iOS Build Start#########");
        CustomImporter.CreateScenes();

        BuildPipeline.BuildPlayer(
            EditorBuildSettings.scenes,
            "Build/iOS",
            BuildTarget.iOS,
#if UNITY_2018_1_OR_NEWER
            BuildOptions.CompressWithLz4
#else
            BuildOptions.CompressWithLz4 | BuildOptions.Il2CPP
#endif
        );
    }

    [MenuItem("Build/WebGL")]
    private static void Build_WebGL()
    {
        Debug.Log("##########WebGL Build Start#########");
        CustomImporter.CreateScenes();

        BuildPipeline.BuildPlayer(
            EditorBuildSettings.scenes,
            "Build/WebGL",
            BuildTarget.WebGL,
#if UNITY_2018_1_OR_NEWER
            BuildOptions.CompressWithLz4
#else
            BuildOptions.CompressWithLz4 | BuildOptions.Il2CPP
#endif
        );
    }


    ////////////////////////////
    //BuildJenkins
    ////////////////////////////
    public static void SetBundleVersion()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        int i, len = args.Length;
        for (i = 0; i < len; ++i)
        {
            switch (args[i])
            {
                case "/SetVersion":
                    {
                        var version = args[i + 1];
                        if (EditorApplication.isUpdating != true) { SetVersion(version); }
                        EditorApplication.update += () => { SetVersion(version); };
                        break;
                    }
                case "/SetBuildNumber":
                    {
                        var num = args[i + 1];
                        if (EditorApplication.isUpdating != true) { SetBuildNumber(num); }
                        EditorApplication.update += () => { SetBuildNumber(num); };
                        break;

                    }
            }
        }
    }

    public static void SetVersion(string version)
    {
        Debug.Log("SetVersion: " + version);
        PlayerSettings.bundleVersion = version;
        //PlayerSettings.shortBundleVersion = version;
    }

    public static void SetBuildNumber(string num)
    {
        Debug.Log("SetBuildNumber: " + num);
        PlayerSettings.iOS.buildNumber = num;
        PlayerSettings.Android.bundleVersionCode = int.Parse(num);
    }

}

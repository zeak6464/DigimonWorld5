using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UnityDownloadExpansionFile : MonoBehaviour
{

    private void Awake()
    {
#if !UNITY_EDITOR && UNITY_ANDROID && ENABLE_OBB
        if (GooglePlayDownloader.RunningOnAndroid() == false)return;

        string expPath = GooglePlayDownloader.GetExpansionFilePath();
        if (expPath == null) return;

#if true
        string mainPath = GooglePlayDownloader.GetMainOBBPath(expPath);
        if (mainPath != null) return;
#else
        string mainPath = GooglePlayDownloader.GetMainOBBPath(expPath);
        string patchPath = GooglePlayDownloader.GetPatchOBBPath(expPath);
        if (mainPath != null && patchPath != null) return;
#endif

        GooglePlayDownloader.FetchOBB();
#endif //!UNITY_EDITOR && UNITY_ANDROID && ENABLE_OBB
    }

    private void Update()
    {
        SceneManager.LoadScene("Entry");
    }

}

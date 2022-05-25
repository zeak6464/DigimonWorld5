#if UNITY_EDITOR_WIN

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

/// <summary>
/// PlayerPrefsの中身を表示する
/// </summary>
public class PlayerPrefsWindow : EditorWindow
{
    static private List<PlayerPrefsData> playerPrefsData = new List<PlayerPrefsData>();
    Vector2 leftScrollPos = Vector2.zero;

    private struct PlayerPrefsData
    {
        public string Key { get; set; }
        public SaveData value;

        public PlayerPrefsData(string key, SaveData value)
        {
            Key = key;
            this.value = value;
        }
        public void SetSaveData(string value, string type)
        {
            this.value.value = value;
            this.value.type = type;
        }
    }

    private struct SaveData
    {
        public string value;
        public string type;
        public SaveData(string value, string type)
        {
            this.value = value;
            this.type = type;
        }
    }

    [MenuItem("Window/Save Data Manager")]
    private static void OpenEditor()
    {
        GetWindow<PlayerPrefsWindow>("Save Data Manager");
        GetAllKeys();
    }
    void OnGUI()
    {
        var rect = EditorGUILayout.GetControlRect();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh",GUILayout.Width(100)))
        {
            GetAllKeys();
        }
        if (GUILayout.Button("All Clear", GUILayout.Width(100)))
        {
            DeleteAll();
        }
        if (GUILayout.Button("Import", GUILayout.Width(100)))
        {
            Import();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginVertical();
        EditorGUIUtility.labelWidth = rect.width * 0.5f;
        EditorGUILayout.LabelField("Save Data List");
        leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos, GUI.skin.box);
        {
            // スクロール範囲
            foreach (var data in playerPrefsData.ToArray())
            {
                EditorGUILayout.TextField(data.Key, data.value.value);

                if (data.Key.Contains("date"))
                {
                    EditorGUILayout.BeginHorizontal();
                    string key = data.Key.Substring(0, "savedata/Save_00".Length);
                    string name = data.Key.Substring("savedata/".Length, "Save_00".Length);
                    if (GUILayout.Button("Export", GUILayout.Width(100)))
                    {
                        Export(key, name);
                    }
                    if (GUILayout.Button("Delete", GUILayout.Width(100)))
                    {
                        Delete(key);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                }
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

    }

    private void Delete(string key)
    {
        foreach (var data in playerPrefsData.ToArray())
        {
            if(data.Key.StartsWith(key))
            {
                playerPrefsData.Remove(data);
                PlayerPrefs.DeleteKey(data.Key);
            }
        }
    }

    private void Export(string key, string name)
    {
        // 保存先のファイルパスを取得する
        var filePath = EditorUtility.SaveFilePanel("Export", "", name, "sgs");

        if (!string.IsNullOrEmpty(filePath))
        {
            string value = "";
            DateTime date = DateTime.Now;
            foreach (var data in playerPrefsData)
            {
                if (data.Key.StartsWith(key))
                {
                    if (data.Key.Contains("date"))
                        date = DateTime.Parse(data.value.value);
                    else
                        value = data.value.value;
                }
            }

            File.WriteAllBytes(filePath, Convert.FromBase64String(value));
            File.SetLastWriteTime(filePath, date);
        }
    }

    private void Import()
    {
        // 読み込み元のファイルパスを取得する
        var filePath = EditorUtility.OpenFilePanel("Import", "", "sgs");

        if (!string.IsNullOrEmpty(filePath))
        {
            // あいているインデックスを探す
            for (int index = 0; index < 40; index++)
            {
                var path = Yukar.Common.GameDataManager.GetDataPath(index);
                if (playerPrefsData.Exists(x => x.Key.StartsWith(path)))
                    continue;

                // あいてたのでそこに入れる
                var base64 = Convert.ToBase64String(File.ReadAllBytes(filePath));
                UnityEngine.PlayerPrefs.SetString(path, base64);
                UnityEngine.PlayerPrefs.SetString(path + Yukar.Common.GameDataManager.SAVE_DATENAME, File.GetLastWriteTime(filePath).ToString());
                UnityEngine.PlayerPrefs.Save();

                GetAllKeys();
                break;
            }
        }
    }

    /// <summary>
    /// PlayerPrefsで使用されているキーをすべて取得する
    /// <para>特定のレジストリのキーを取得すること同じ</para>
    /// <para>参考URL https://dobon.net/vb/dotnet/system/registrykey.html </para>
    /// </summary>
    private static void GetAllKeys()
    {
        var subKeyNames = GetAllKeysImpl();
        //Debug.Log("セーブデータの数 : " + subKeyNames.Length);
        playerPrefsData.Clear();
        foreach (var key in subKeyNames)
        {
            if (!key.StartsWith("savedata"))
                continue;

            var saveData = GetData(key);
            playerPrefsData.Add(new PlayerPrefsData(key, saveData));
        }

        return;
    }

    private static string[] GetAllKeysImpl()
    {
        // editorで使用する際キーは以下の位置にある
        // HKCU\Software\Unity\UnityEditor\[company name]\[product name]
        // 参考URL http://www.atmarkit.co.jp/fdotnet/dotnettips/643regenumvalues/regenumvalues.html
        string baseKeyName = @"Software\Unity\UnityEditor\{0}\{1}\";
        baseKeyName = string.Format(baseKeyName, UnityEditor.PlayerSettings.companyName, UnityEditor.PlayerSettings.productName);

        // すべてのサブキーを取得する
        Microsoft.Win32.RegistryKey parentKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(baseKeyName, false);
        if (parentKey == null)
        {
            //Debug.LogWarning("レジストリキー : " + baseKeyName + "は存在しません。");
            return null;
        }
        var result = parentKey.GetValueNames();
        parentKey.Close();
        return result;
    }

    private void DeleteAll()
    {
        // 確認する
        var ok = EditorUtility.DisplayDialog("Warning", "Clear All saved data?", "OK", "Cancel");
        if (!ok)
            return;
        
        string baseKeyName = @"Software\Unity\UnityEditor\{0}\{1}\";
        baseKeyName = string.Format(baseKeyName, UnityEditor.PlayerSettings.companyName, UnityEditor.PlayerSettings.productName);
        //Debug.Log(baseKeyName);
        Microsoft.Win32.RegistryKey parentKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(baseKeyName, false);
        if (parentKey == null)
        {
            //Debug.LogWarning("レジストリキー : " + baseKeyName + "は存在しません。");
            return;
        }
        Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(baseKeyName);
        playerPrefsData.Clear();
    }

    private static SaveData GetData(string key)
    {
        var resultInt = PlayerPrefs.GetInt(key, 6700417);
        if (resultInt != 6700417)
        {
            return new SaveData(resultInt.ToString(), "int");
        }
        var resultString = PlayerPrefs.GetString(key, "");
        if (resultString != "")
        {
            return new SaveData(resultString.ToString(), "string");
        }
        var resultFloat = PlayerPrefs.GetFloat(key);
        return new SaveData(resultFloat.ToString(), "float");
    }
}

#endif // #if UNITY_EDITOR_WIN
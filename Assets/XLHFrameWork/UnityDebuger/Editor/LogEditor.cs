
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class LogEditor
{
    [MenuItem("Tools/打开日志系统")]
    public static void LoadReport()
    {
        ScriptingDefineSymbols.AddScriptingDefineSymbol("OPEN_LOG");
        GameObject reportObj = GameObject.Find("Reporter");
        if (reportObj==null)
        {
            reportObj= GameObject.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/XLHFramework/UnityDebuger/Unity-Logs-Viewer/Reporter.prefab"));
            reportObj.name = "Reporter";
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.Refresh();
            Debug.Log("Open Log Finish!");
        }
    }
    [MenuItem("Tools/关闭日志系统")]
    public static void CloseReport()
    {
        ScriptingDefineSymbols.RemoveScriptingDefineSymbol("OPEN_LOG");
        GameObject reportObj = GameObject.Find("Reporter");
        if (reportObj!=null)
        {
            GameObject.DestroyImmediate(reportObj);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.Refresh();
            Debug.Log("Cloase Log Finish!");
        }
    }
}
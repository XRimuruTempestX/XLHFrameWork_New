using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "UISetting", menuName = "UI框架/UISetting")]
[Serializable]
public class UISetting : ScriptableObject
{
    private static UISetting instance;
    public static UISetting Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<UISetting>("UISetting");
            }
            return instance;
        }
    }
    
    public bool SINGMAXSK_SYSTEM = false; //是否开启单遮

    public string nameSpace = "UIFrameworlk";

    /// <summary>
    /// 带;号
    /// </summary>
    [LabelText("引用命名控件， 需要带上;")]
    public List<string> referenceSpace;

    [FolderPath,LabelText("UI数据脚本生成路径")]
    public string uiScriptRootPath = "";

}

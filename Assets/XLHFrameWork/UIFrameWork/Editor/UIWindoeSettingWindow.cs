using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using XLHFrameWork.UIFrameWork.Config;

namespace XLHFrameWork.UIFrameWork.Editor
{
    public class UIWindoeSettingWindow : OdinEditorWindow
    {
        [OnValueChanged("UISettingValueChange")]
        public bool SINGMAXSK_SYSTEM = false; //是否开启单遮

        [OnValueChanged("UISettingValueChange")]
        public string nameSpace = "UIFrameworlk";

        /// <summary>
        /// 带;号
        /// </summary>
        [LabelText("引用命名控件， 需要带上;")]
        [OnValueChanged("UISettingValueChange")]
        public List<string> referenceSpace;

        [FolderPath,LabelText("UI数据脚本生成路径")]
        [OnValueChanged("UISettingValueChange")]
        public string uiScriptRootPath = "";
        
        [FolderPath,LabelText("窗口预制体存放路径")][OnValueChanged("UIWindowPathValueChange")]
        public List<string> windowPathRootList = new List<string>();
        
        [LabelText("窗口预制体存放路径")][OnValueChanged("UIWindowPathValueChange")]
        public List<UiWindowPath.WindowInfo> windowInfoList = new List<UiWindowPath.WindowInfo>();
        
        private UiWindowPath uiWindowPath;

        [MenuItem("XLHFrameWork/UI框架配置")]
        public static void ShowWindow()
        {
            GetWindow<UIWindoeSettingWindow>().Show();
        }

        private void Awake()
        {
            SINGMAXSK_SYSTEM = UISetting.Instance.SINGMAXSK_SYSTEM;
            nameSpace = UISetting.Instance.nameSpace;
            referenceSpace = UISetting.Instance.referenceSpace;
            uiScriptRootPath =  UISetting.Instance.uiScriptRootPath;
            
            uiWindowPath = AssetDatabase.LoadAssetAtPath<UiWindowPath>("Assets/XLHFrameWork/UIFrameWork/UIFrameWorkConfig/UIPathConfig/UWindowPath.asset");
            windowPathRootList = uiWindowPath.windowPathRootList;
            windowInfoList = uiWindowPath.windowInfoList;
        }

        private void UISettingValueChange()
        {
            UISetting.Instance.SINGMAXSK_SYSTEM = SINGMAXSK_SYSTEM;
            UISetting.Instance.nameSpace = nameSpace;
            UISetting.Instance.referenceSpace = referenceSpace;
            UISetting.Instance.uiScriptRootPath = uiScriptRootPath;
        }

        private void UIWindowPathValueChange()
        {
            uiWindowPath.windowPathRootList = windowPathRootList;
            uiWindowPath.windowInfoList = windowInfoList;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EditorUtility.SetDirty(uiWindowPath);
            AssetDatabase.SaveAssets();
        }
    }
}

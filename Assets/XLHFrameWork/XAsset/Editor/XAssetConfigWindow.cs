using System;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using XLHFrameWork.XAsset.Config;
using XLHFrameWork.XAsset.Editor.BundleBuild;

namespace XLHFrameWork.XAsset.Editor
{
    public class XAssetConfigWindow : OdinMenuEditorWindow
    {

        [MenuItem("XLHFrameWork/XAsset/打包配置面板")]
        private static void OpenWindow()
        {
            var window = GetWindow<XAssetConfigWindow>();
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(800, 600);
        }
        
        
        /*[SerializeField][InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        private BundleSettings bundleSettings;*/

        protected override void OnEnable()
        {
            /*bundleSettings = AssetDatabase.LoadAssetAtPath<BundleSettings>(
                "Assets/XLHFrameWork/XAsset/Resources/AssetsBundleSettings.asset");*/
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            OdinMenuTree tree = new OdinMenuTree(supportsMultiSelect: true)
            {
                {"Home", null, EditorIcons.House},
                {"Home/AssetBundle", BuildBundleConfigura.Instance, EditorIcons.SettingsCog},
                {"BundleSetting",BundleSettings.Instance, EditorIcons.SettingsCog}
            };
            
            return tree;
        }
       
    }
}

using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using XLHFramework.GCFrameWorlk.Editor;

namespace XLHFrameWork.GCFrameWork.Editor
{
    public class GCFrameWorkWindow : OdinEditorWindow
    {
        [SerializeField][InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Hidden)]
        public WorldConfig worldConfig;
        
        [MenuItem("XLHFrameWork/GCFrameWorkWindow")]
        public static void ShowWindow()
        {
            GCFrameWorkWindow window = GetWindow<GCFrameWorkWindow>();
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(800, 600);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            worldConfig = AssetDatabase.LoadAssetAtPath<WorldConfig>("Assets/XLHFrameWork/GCFrameWork/Editor/WorldConfig.asset");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EditorUtility.SetDirty(worldConfig);
            AssetDatabase.SaveAssets();
        }
    }
}

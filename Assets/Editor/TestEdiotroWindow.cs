using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using XLHFrameWork.XAsset.Config;

public class TestEdiotroWindow : OdinEditorWindow
{
    [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
    public BundleSettings bundleSettings;


    protected override void OnEnable()
    {
        base.OnEnable();
        bundleSettings =
            AssetDatabase.LoadAssetAtPath<BundleSettings>(
                "Assets/XLHFrameWork/XAsset/Resources/AssetsBundleSettings.asset");
    }

    [MenuItem("Tools/My SO Window")]
    private static void OpenWindow()
    {
        GetWindow<TestEdiotroWindow>().Show();
    }
}

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class BundleModuleConfig : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("XAsset/BundleModuleConfig")]
    public static void ShowExample()
    {
        BundleModuleConfig wnd = GetWindow<BundleModuleConfig>();
        wnd.titleContent = new GUIContent("BundleModuleConfig");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;
        VisualTreeAsset bundleSettingWindow = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/XLHFrameWork/XAsset/Editor/XAssetWindow/BundleModuleConfig.uxml");
        VisualElement bundleSettingWindowVE =  bundleSettingWindow.Instantiate();
        bundleSettingWindowVE.style.flexGrow = 1;
        root.Add(bundleSettingWindowVE);
    }
}

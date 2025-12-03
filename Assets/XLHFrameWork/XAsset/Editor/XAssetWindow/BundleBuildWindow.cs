using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using XLHFrameWork.XAsset.Config;
using XLHFrameWork.XAsset.Editor.BundleBuild;
using BuildAssetBundleOptions = XLHFrameWork.XAsset.Config.BuildAssetBundleOptions;
using BuildTarget = XLHFrameWork.XAsset.Config.BuildTarget;

public class BundleBuildWindow : EditorWindow
{
    [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;

    private VisualElement rightVE;
    private List<string> menuList;
    private ListView listView;
    private Label detailLabel;
    private ScrollView scrollView;

    private Label noticeTitle;
    private TextField notice;
    private TextField appVersion;
    private TextField hotPatchVersion;

    private Button leftBtn;
    private Button rightBtn;

    [MenuItem("XLHFrameWork/XAsset/BundleBuildWindow")]
    public static void ShowExample()
    {
        var wnd = GetWindow<BundleBuildWindow>();
        wnd.titleContent = new GUIContent("BundleBuildWindow");
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        root.style.flexDirection = FlexDirection.Row;
        // 加载 UXML
        VisualElement ui = m_VisualTreeAsset.Instantiate();
        ui.style.flexGrow = 1;
        root.Add(ui);
        
        VisualElement _Root = root.Q<VisualElement>("Root");
        
        VisualTreeAsset rightVEAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/XLHFrameWork/XAsset/Editor/XAssetWindow/rightVE.uxml");
        VisualElement rVE =  rightVEAsset.Instantiate();
        rVE.style.flexGrow = 1;
        _Root.Add(rVE);
        

        VisualTreeAsset BundleSetting =
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/XLHFrameWork/XAsset/Editor/XAssetWindow/BundleSetting.uxml");
        VisualElement bundleSettingVE = BundleSetting.Instantiate();
        bundleSettingVE.style.flexGrow = 1;
        // 获取控件
        listView = root.Q<ListView>("MenuView");
        detailLabel = root.Q<Label>("DetailLabel");
        rightVE = rVE.Q<VisualElement>("rightVE");
        noticeTitle = rVE.Q<Label>("noticeTitle");
        appVersion = rVE.Q<TextField>("appVersion");
        hotPatchVersion = rVE.Q<TextField>("hotPatchVersion");
        notice =  rVE.Q<TextField>("notice");
        leftBtn =  rVE.Q<Button>("leftBtn");
        rightBtn = rVE.Q<Button>("rightBtn");

        menuList = new List<string>() { "AssetBundle", "HotPatch", "BundleSetting" };


        leftBtn.clicked += () =>
        {
            for (int i = 0; i < BuildBundleConfigura.Instance.AssetBundleConfig.Count; i++)
            {
                if (BuildBundleConfigura.Instance.AssetBundleConfig[i].isBuild)
                {
                    BuildBundleCompiler.BuildAssetBundle(BuildBundleConfigura.Instance.AssetBundleConfig[i], BuildType.AssetBundle);
                }
            }
        };
        
        // 设置 ListView 数据
        listView.itemsSource = menuList;
        listView.selectionType = SelectionType.Single;

        // 你的 ListView 必须在 UXML 里带 itemTemplate，否则需要加 makeItem：
        if (listView.makeItem == null)
        {
            listView.makeItem = () =>
            {
                var lable = new Label();
                lable.style.unityTextAlign = TextAnchor.MiddleCenter;
                lable.style.marginLeft = 5;
                return lable;
            };
        };

        listView.bindItem = (elem, index) =>
        {
            var lable = elem as Label;
            lable.text = menuList[index];
            lable.RegisterCallback<ClickEvent>(evt =>
            {
                
                Debug.Log("点击了：" + menuList[index]);
                if (index == 0 || index == 1)
                {
                    if (!_Root.Contains(rVE))
                    {
                        _Root.Add(rVE);
                    }

                    if (_Root.Contains(bundleSettingVE))
                    {
                        _Root.Remove(bundleSettingVE);
                    }
                    ShowItem(rVE);
                    if (index == 0)
                    {
                        notice.style.opacity = 0;
                        appVersion.style.opacity = 0;
                        noticeTitle.style.opacity = 0;
                        hotPatchVersion.style.opacity = 0;
                    }
                    else if (index == 1)
                    {
                        notice.style.opacity = 1;
                        appVersion.style.opacity = 1;
                        noticeTitle.style.opacity = 1;
                        hotPatchVersion.style.opacity = 1;
                    }
                }
                else
                {
                    if (!_Root.Contains(bundleSettingVE))
                    {
                        _Root.Add(bundleSettingVE);
                    }
                    if(_Root.Contains(rVE))
                        _Root.Remove(rVE);
                    _Root.Add(bundleSettingVE);

                    BundleSettingWindow(bundleSettingVE);
                }
            });
        };

        listView.onSelectionChange += selection =>
        {
            if (selection.Any())
                detailLabel.text = $"当前选择：{selection.First()}";
        };

        listView.selectedIndex = 0;
        noticeTitle.style.opacity = 0;
        appVersion.style.opacity = 0;
        notice.style.opacity = 0;
        hotPatchVersion.style.opacity = 0;

        if (listView.selectedIndex == 0 || listView.selectedIndex == 1)
        {
            ShowItem(rVE);
        }
    }

    private void ShowItem(VisualElement root)
    {
        scrollView = root.Q<ScrollView>("ContentView");
        scrollView.contentContainer.Clear();
        VisualTreeAsset itemButton =
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/XLHFrameWork/XAsset/Editor/XAssetWindow/menu-item.uxml");

        var container = scrollView.contentContainer;
        container.style.flexDirection = FlexDirection.Row;
        container.style.flexWrap = Wrap.Wrap;

        /*if (BuildBundleConfigura.Instance.AssetBundleConfig.Count == 0)
        {
            var item = itemButton.Instantiate();
            var button = item.Q<Button>("menu-item");
            button.AddToClassList("menu-button");
            button.text = "+";
            button.clicked += () =>
            {
                //button.AddToClassList("selected");
                BundleModuleConfig.ShowExample();
                Debug.Log("添加");
            };
            container.Add(item);
        }*/


        foreach (var assetItem in BuildBundleConfigura.Instance.AssetBundleConfig)
        {
            BundleModuleData dataItem = assetItem;
            var item = itemButton.Instantiate();
            var button = item.Q<Button>("menu-item");
            button.RemoveFromClassList("selected");
            button.RemoveFromClassList("menu-button");

            // 初始化样式
            if (assetItem.isBuild)
            {
                button.AddToClassList("selected");
            }
            else
            {
                button.AddToClassList("menu-button");
            }

            button.text = assetItem.moduleName;
            button.clicked += () =>
            {
                dataItem.isBuild = !dataItem.isBuild;
                button.ToggleInClassList("selected");
                button.ToggleInClassList("menu-button");
                if (Time.realtimeSinceStartup - dataItem.lastClickBtnTime < 0.2f)
                {
                    BundleModuleConfig.ShowExample(assetItem);
                    Debug.Log("123123");
                }

                dataItem.lastClickBtnTime = Time.realtimeSinceStartup;
            };
            container.Add(item);
        }

        var item2 = itemButton.Instantiate();
        var button2 = item2.Q<Button>("menu-item");
        button2.AddToClassList("menu-button");
        button2.text = "+";
        button2.clicked += () =>
        {
            //button.AddToClassList("selected");
            BundleModuleConfig.ShowExample();
            Debug.Log("添加");
        };
        container.Add(item2);
    }

    #region BundleSettingWindow

    private TextField assetDownLoadUrl;
    private EnumField bundleHotType;
    private EnumField loadAssetType;
    private IntegerField MAX_THREAD_COUNT;
    private Toggle isEncrypt;
    private TextField encryptKey;
    private TextField ABSUFFIX;
    private EnumField buildbundleOptions;
    private EnumField buildTarget;
    private TextField XAssetRootPath;
    
    private void BundleSettingWindow(VisualElement root)
    {
        assetDownLoadUrl = root.Q<TextField>("AssetBundleDownLoadUrl");
        bundleHotType = root.Q<EnumField>("bundleHotType");
        loadAssetType = root.Q<EnumField>("loadAssetType");
        MAX_THREAD_COUNT = root.Q<IntegerField>("MAX_THREAD_COUNT");
        isEncrypt = root.Q<Toggle>("isEncrypt");
        encryptKey = root.Q<TextField>("encryptKey");
        ABSUFFIX = root.Q<TextField>("ABSUFFIX");
        buildbundleOptions = root.Q<EnumField>("buildbundleOptions");
        buildTarget = root.Q<EnumField>("buildTarget");
        XAssetRootPath = root.Q<TextField>("XAssetRootPath");

        SetInitialValues();
        
        assetDownLoadUrl.RegisterValueChangedCallback(evt => SaveSetting("AssetBundleDownLoadUrl", evt.newValue));
        bundleHotType.RegisterValueChangedCallback(evt => SaveSetting("bundleHotType", evt.newValue));
        loadAssetType.RegisterValueChangedCallback(evt => SaveSetting("loadAssetType", evt.newValue));
        MAX_THREAD_COUNT.RegisterValueChangedCallback(evt => SaveSetting("MAX_THREAD_COUNT", evt.newValue));
        isEncrypt.RegisterValueChangedCallback(evt => SaveSetting("isEncrypt", evt.newValue));
        encryptKey.RegisterValueChangedCallback(evt => SaveSetting("encryptKey", evt.newValue));
        ABSUFFIX.RegisterValueChangedCallback(evt => SaveSetting("ABSUFFIX", evt.newValue));
        buildbundleOptions.RegisterValueChangedCallback(evt => SaveSetting("buildbundleOptions", evt.newValue));
        buildTarget.RegisterValueChangedCallback(evt => SaveSetting("buildTarget", evt.newValue));
        XAssetRootPath.RegisterValueChangedCallback(evt => SaveSetting("XAssetRootPath", evt.newValue));
    }
    
    private void SetInitialValues()
    {
        assetDownLoadUrl.value = BundleSettings.Instance.AssetBundleDownLoadUrl;
        bundleHotType.value = BundleSettings.Instance.bundleHotType;
        loadAssetType.value = BundleSettings.Instance.loadAssetType;
        MAX_THREAD_COUNT.value = BundleSettings.Instance.MAX_THREAD_COUNT;
        isEncrypt.value = BundleSettings.Instance.bundleEncrypt.isEncrypt;
        encryptKey.value = BundleSettings.Instance.bundleEncrypt.encryptKey;
        ABSUFFIX.value = BundleSettings.Instance.ABSUFFIX;
        buildbundleOptions.value = BundleSettings.Instance.buildbundleOptions;
        buildTarget.value = BundleSettings.Instance.buildTarget;
        XAssetRootPath.value = BundleSettings.Instance.XAssetRootPath;
    }

    // 保存设置的方法
    private void SaveSetting(string key, object value)
    {
        // 根据 key 保存对应的值
        switch (key)
        {
            case "AssetBundleDownLoadUrl":
                BundleSettings.Instance.AssetBundleDownLoadUrl = (string)value;
                break;
            case "bundleHotType":
                BundleSettings.Instance.bundleHotType = (BundleHotEnum)value;
                break;
            case "loadAssetType":
                BundleSettings.Instance.loadAssetType = (LoadAssetEnum)value;
                break;
            case "MAX_THREAD_COUNT":
                BundleSettings.Instance.MAX_THREAD_COUNT = (int)value;
                break;
            case "isEncrypt":
                BundleSettings.Instance.bundleEncrypt.isEncrypt = (bool)value;
                break;
            case "encryptKey":
                BundleSettings.Instance.bundleEncrypt.encryptKey = (string)value;
                break;
            case "ABSUFFIX":
                BundleSettings.Instance.ABSUFFIX = (string)value;
                break;
            case "buildbundleOptions":
                BundleSettings.Instance.buildbundleOptions = (BuildAssetBundleOptions)value;
                break;
            case "buildTarget":
                BundleSettings.Instance.buildTarget = (BuildTarget)value;
                break;
            case "XAssetRootPath":
                BundleSettings.Instance.XAssetRootPath = (string)value;
                break;
        }
    }

    #endregion

    private void OnFocus()
    {
        rootVisualElement.Clear();
        CreateGUI();
    }

    
    private void OnDestroy()
    {
        BuildBundleConfigura.Instance.ClearclickTime();
        EditorUtility.SetDirty(BundleSettings.Instance);
        AssetDatabase.SaveAssets();
    }
}


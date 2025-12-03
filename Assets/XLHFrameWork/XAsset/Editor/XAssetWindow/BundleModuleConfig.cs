using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using XLHFrameWork.XAsset.Config;
using XLHFrameWork.XAsset.Editor.BundleBuild;

public class BundleModuleConfig : EditorWindow
{
    [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;

    private static BundleModuleData bundleModuleData = null;

    private TextField moduleName;
    private Toggle isAddressableAsset;
    private Button prefabsBtn;
    private Button rootPathBtn;
    private Button singlePathBtn;
    private ScrollView scrollView;

    
    private int selectedIndex = 0;

    [MenuItem("XAsset/BundleModuleConfig")]
    public static void ShowExample(BundleModuleData data = null)
    {
        
        if (data != null)
        {
            bundleModuleData = data;
        }
        else
        {
            bundleModuleData = new BundleModuleData();
        }
        
        BundleModuleConfig wnd = GetWindow<BundleModuleConfig>();
       
        wnd.titleContent = new GUIContent("BundleModuleConfig");
        
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;
        VisualTreeAsset bundleSettingWindow = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/XLHFrameWork/XAsset/Editor/XAssetWindow/BundleModuleConfig.uxml");
        VisualElement bundleSettingWindowVE = bundleSettingWindow.Instantiate();
        bundleSettingWindowVE.style.flexGrow = 1;
        root.Add(bundleSettingWindowVE);
        selectedIndex = 0;
        moduleName = root.Q<TextField>("moduleName");
        isAddressableAsset = root.Q<Toggle>("isAddressableAsset");
        prefabsBtn = root.Q<Button>("prefabsBtn");
        rootPathBtn = root.Q<Button>("rootPathBtn");
        singlePathBtn = root.Q<Button>("singlePathBtn");
        scrollView = root.Q<ScrollView>("contentSV");

        moduleName.value = bundleModuleData.moduleName;
        
        moduleName.RegisterValueChangedCallback(evt =>
        {
            if(string.IsNullOrEmpty(evt.newValue))
                return;
            string moduleNameStr = evt.newValue.ToString();
            if(moduleNameStr == bundleModuleData.moduleName)
                return;
            if (BuildBundleConfigura.Instance.CheckRepeatName(moduleNameStr))
            {
                EditorUtility.DisplayDialog("重复名字！", "模块名重复，请重新赋值", "OK");
                moduleName.value = bundleModuleData.moduleName;
                return;
            }
            bundleModuleData.moduleName = moduleNameStr;

        });
        isAddressableAsset.value = bundleModuleData.isAddressableAsset;
        isAddressableAsset.RegisterValueChangedCallback(evt =>
        {
            isAddressableAsset.value = evt.newValue;
            bundleModuleData.isAddressableAsset = isAddressableAsset.value;
        });
        prefabsBtn.clicked += () =>
        {
            selectedIndex = 0;
            ShowPrefabsContent();
        };
        rootPathBtn.clicked += () =>
        {
            selectedIndex = 1;
            ShowRootPathContent();
        };
        singlePathBtn.clicked += () =>
        {
            selectedIndex = 2;
            ShowSinglePathContent();
        };
        
        ShowPrefabsContent();
        
        var addBtn = new Button();
        
        addBtn.text = "添加";
        //  addBtn.style.flexGrow = 1;
        addBtn.style.height = 50;
        addBtn.clicked += () =>
        {
            if (selectedIndex == 0)
            {
                bundleModuleData.prefabPathArr.Add("");
                ShowPrefabsContent();
            }
            else if (selectedIndex == 1)
            {
                bundleModuleData.rootFolderPathArr.Add("");
                ShowRootPathContent();
            }
            else if (selectedIndex == 2)
            {
                bundleModuleData.singleFolderPathArr.Add("");
                ShowSinglePathContent();
            }
        };
        root.Add(addBtn);

        var saveBtn = new Button();
        saveBtn.text = "Save";
        //  saveBtn.style.flexGrow = 1;
        saveBtn.style.height = 50;

        saveBtn.clicked += () =>
        {
            BuildBundleConfigura.Instance.SaveModuleData(bundleModuleData);
            EditorUtility.DisplayDialog("保存模块", "保存成功", "确认");
            Close();
        };
        root.Add(saveBtn);

        var deleteModuleBtn = new Button();
        deleteModuleBtn.text = "DeleteModule";
        //  deleteModuleBtn.style.flexGrow = 1;
        deleteModuleBtn.style.height = 50;
        deleteModuleBtn.clicked += () =>
        {
            BuildBundleConfigura.Instance.RemoveModuleByName(bundleModuleData.moduleName);
            EditorUtility.DisplayDialog("删除模块", "删除成功", "确认");
            Close();
        };
        root.Add(deleteModuleBtn);
    }

    private void ShowPrefabsContent()
    {
        scrollView.Clear();

        for (int i = 0; i < bundleModuleData.prefabPathArr.Count; i++)
        {
            int index = i;
            var ve = new VisualElement();
            ve.style.flexDirection = FlexDirection.Row;
            ve.style.flexGrow = 1;
            string path = bundleModuleData.prefabPathArr[i];
            DefaultAsset prefabsPath = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
            
            var objf = new ObjectField();
            objf.style.flexGrow = 1;
            objf.value = prefabsPath;
            objf.RegisterValueChangedCallback(evt =>
            {
                var newObj = evt.newValue as DefaultAsset;
                if (newObj != null)
                {
                    string path = AssetDatabase.GetAssetPath(newObj);
                    bundleModuleData.prefabPathArr[index] =  path;
                }
            });
            ve.Add(objf);


            Button mBtn = new Button();
            mBtn.text = "修改";
            mBtn.style.width = 50;
            mBtn.clicked += () =>
            {
                string path = EditorUtility.OpenFolderPanel("选择文件夹", Application.dataPath, "")
                    .Replace(Application.dataPath, "Assets");
                DefaultAsset assetPath = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
                objf.value = assetPath;
            };
            ve.Add(mBtn);

            Button deleteBtn = new Button();
            deleteBtn.text = "删除";
            deleteBtn.style.width = 50;
            deleteBtn.clicked += () =>
            {
                bundleModuleData.prefabPathArr.RemoveAt(index);
                ShowPrefabsContent();
            };
            ve.Add(deleteBtn);
            scrollView.Add(ve);
        }
    }
    
    private void ShowRootPathContent()
    {
        scrollView.Clear();
        
        for (int i = 0; i < bundleModuleData.rootFolderPathArr.Count; i++)
        {
            int index = i;
            var ve = new VisualElement();
            ve.style.flexDirection = FlexDirection.Row;
            ve.style.flexGrow = 1;
            string path = bundleModuleData.rootFolderPathArr[i];
            DefaultAsset prefabsPath = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
            
            var objf = new ObjectField();
            objf.style.flexGrow = 1;
            objf.value = prefabsPath;
            
            objf.RegisterValueChangedCallback(evt =>
            {
                var newObj = evt.newValue as DefaultAsset;
                if (newObj != null)
                {
                    string path = AssetDatabase.GetAssetPath(newObj);
                    bundleModuleData.rootFolderPathArr[index] =  path;
                }
            });
            
            ve.Add(objf);


            Button mBtn = new Button();
            mBtn.text = "修改";
            mBtn.style.width = 50;
            mBtn.clicked += () =>
            {
                string path = EditorUtility.OpenFolderPanel("选择文件夹", Application.dataPath, "")
                    .Replace(Application.dataPath, "Assets");
                DefaultAsset assetPath = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
                objf.value = assetPath;
            };
            ve.Add(mBtn);

            Button deleteBtn = new Button();
            deleteBtn.text = "删除";
            deleteBtn.style.width = 50;
            deleteBtn.clicked += () =>
            {
                bundleModuleData.rootFolderPathArr.RemoveAt(index);
                ShowPrefabsContent();
            };
            ve.Add(deleteBtn);
            scrollView.Add(ve);
        }
    }
    
    private void ShowSinglePathContent()
    {
        scrollView.Clear();
        for (int i = 0; i < bundleModuleData.singleFolderPathArr.Count; i++)
        {
            int index = i;
            var ve = new VisualElement();
            ve.style.flexDirection = FlexDirection.Row;
            ve.style.flexGrow = 1;
            string path = bundleModuleData.singleFolderPathArr[i];
            DefaultAsset prefabsPath = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
            
            var objf = new ObjectField();
            objf.style.flexGrow = 1;
            objf.value = prefabsPath;
            
            objf.RegisterValueChangedCallback(evt =>
            {
                var newObj = evt.newValue as DefaultAsset;
                if (newObj != null)
                {
                    string path = AssetDatabase.GetAssetPath(newObj);
                    bundleModuleData.singleFolderPathArr[index] =  path;
                }
            });
            
            ve.Add(objf);


            Button mBtn = new Button();
            mBtn.text = "修改";
            mBtn.style.width = 50;
            mBtn.clicked += () =>
            {
                string path = EditorUtility.OpenFolderPanel("选择文件夹", Application.dataPath, "")
                    .Replace(Application.dataPath, "Assets");
                DefaultAsset assetPath = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
                objf.value = assetPath;
            };
            ve.Add(mBtn);

            Button deleteBtn = new Button();
            deleteBtn.text = "删除";
            deleteBtn.style.width = 50;
            deleteBtn.clicked += () =>
            {
                bundleModuleData.singleFolderPathArr.RemoveAt(index);
                ShowPrefabsContent();
            };
            ve.Add(deleteBtn);
            scrollView.Add(ve);
        }
    }
}
using Newtonsoft.Json;

namespace XLHFrameWork.XAsset.Config
{
    [System.Serializable]
    public class BundleModuleData  
    {
        //AssetBundle模块id
        public long bundleid;
        //模块名称
        public string moduleName;
        //是否寻址资源
        public bool isAddressableAsset;
        //是否打包
        public bool isBuild;
#if UNITY_EDITOR
        //是否添加模块按钮
        [JsonIgnore]
        public bool isAddModule;
#endif
    
        //上一次点击按钮的时间
        public float lastClickBtnTime;


 
        public string[] prefabPathArr ;


        public string[] rootFolderPathArr;

        public BundleFileInfo[] signFolderPathArr;
    }
    [System.Serializable]
    public class BundleFileInfo
    {

        public string abName="AB Name";

        public string bundlePath="BundlePath...";
    }
}
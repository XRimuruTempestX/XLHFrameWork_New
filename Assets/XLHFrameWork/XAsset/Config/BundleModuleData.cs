using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;

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


 
        public List<string> prefabPathArr = new List<string>() ;


        public List<string> rootFolderPathArr  = new List<string>();

        public List<string> singleFolderPathArr  = new List<string>();
    }
   
}
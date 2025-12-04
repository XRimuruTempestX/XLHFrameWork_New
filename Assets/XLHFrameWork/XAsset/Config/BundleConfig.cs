using System.Collections.Generic;

namespace XLHFrameWork.XAsset.Config
{
    [System.Serializable]
    public class BundleConfig
    {
        /// <summary>
        /// 所有AssetBundle的信息列表
        /// </summary>
        public List<BundleInfo> bundleInfoList;
    }

    [System.Serializable]
    //AssetBundle信息
    public class BundleInfo
    {
        /// <summary>
        /// 文件路径
        /// </summary>
        public string path;

        /// <summary>
        /// Crc
        /// </summary>
        public uint crc;

        /// <summary>
        /// AssetBundle名称
        /// </summary>
        public string bundleName;

        /// <summary>
        /// 资源名字
        /// </summary>
        public string assetName;

        /// <summary>
        /// AB模块
        /// </summary>
        public string bundleModule;

        /// <summary>
        /// 是否寻址资源
        /// </summary>
        public bool isAddressableAsset;

        /// <summary>
        /// 依赖项
        /// </summary>
        public List<string> bundleDependce;
    }

    /// <summary>
    /// 内嵌的AssetBundle的信息
    /// </summary>
    public class BuiltinBundleInfo
    {
        public string fileName;

        public string md5;

        public float size; //文件大小 用来计算文件解压进度显示
    }
}
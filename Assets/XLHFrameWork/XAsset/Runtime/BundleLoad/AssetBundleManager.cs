using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using XLHFrameWork.XAsset.Config;
using XLHFrameWork.XAsset.Runtime.Helper;

namespace XLHFrameWork.XAsset.Runtime.BundleLoad
{
    public class BundleItem
    {
        /// <summary>
        /// 文件加载路径
        /// </summary>
        public string path;
        /// <summary>
        /// 文件加载路径crc
        /// </summary>
        public uint crc;
        /// <summary>
        /// AssetBundle名称
        /// </summary>
        public string bundleName;
        /// <summary>
        /// 资源名称
        /// </summary>
        public string assetName;
        /// <summary>
        /// 是否寻址资源
        /// </summary>
        public bool isAddressableAsset;
        /// <summary>
        /// AssetBundle所属的模块
        /// </summary>
        public BundleModuleEnum bundleModuleType;
        /// <summary>
        /// AssetBundle依赖项
        /// </summary>
        public List<string> bundleDependce;
        /// <summary>
        /// AssetBundle
        /// </summary>
        public AssetBundle assetBundle;
        /// <summary>
        /// 通过AssetBundle加载出的对象
        /// </summary>
        public UnityEngine.Object obj;
        /// <summary>
        /// 通过AssetBundle加载出的对象数组
        /// </summary>
        public UnityEngine.Object[] objArr;
        /// <summary>
        /// 引用计数
        /// </summary>
        public int refCount;
    }
    
    /// <summary>
    /// AssetBundle缓存
    /// </summary>
    public class AssetBundleCache
    {
        public AssetBundle assetBundle;
        
        public int referenceCount;

        public void Release()
        {
            assetBundle = null;
            referenceCount = 0;
        }
        
    }
    
    public class AssetBundleManager : Singleton<AssetBundleManager>
    {
        /// <summary>
        /// 已经加载的资源模块
        /// </summary>
        private List<BundleModuleEnum> mAlreadyLoadBundleModuleList = new List<BundleModuleEnum>();
        /// <summary>
        /// 所有模块的AssetBundle的资源对象字典
        /// </summary>
        private Dictionary<uint, BundleItem> mAllBundleAssetDic = new Dictionary<uint, BundleItem>();
        
        /// <summary>
        /// 所有模块的已经加载过的AssetBundle的资源对象字典
        /// </summary>
        private Dictionary<string, AssetBundleCache> mAllAlreadyLoadBundleDic = new Dictionary<string, AssetBundleCache>();
        /// <summary>
        /// 所有AB模块字典
        /// </summary>
        private Dictionary<string, BundleModuleData> mAllBundleModuleDic = new Dictionary<string, BundleModuleData>();
        
        /// <summary>
        /// 正在加载中的Unitask
        /// </summary>
        private Dictionary<string, UniTask<AssetBundle>> mLoadUniTaskDic = new Dictionary<string, UniTask<AssetBundle>>();
        
        /// <summary>
        /// 缓存池
        /// </summary>
        private AssetBundleCachePool assetbundleCachePool = new AssetBundleCachePool();
        
        /// <summary>
        /// AssetBundle配置文件加载路径
        /// </summary>
        private string mBundleConfigPath;
        /// <summary>
        /// AssetBundle配置文件名称
        /// </summary>
        private string mBundleConfigName;
        
        /// <summary>
        /// AssetBundle配置文件名称
        /// </summary>
        private string mAssetsBundleConfigName;

        /// <summary>
        /// 加载AssetBundle配置文件
        /// </summary>
        /// <param name="bundleModule"></param>
        /// <returns></returns>
        public async UniTask<bool> InitAssetModule(BundleModuleEnum bundleModule)
        {
            try
            {
                if (mAlreadyLoadBundleModuleList.Contains(bundleModule))
                {
                    Debug.LogWarning("该模块资源已经被加载过了" + bundleModule);
                    return false;
                }

                LoadAllBundleModule();

                if (GeneratorBundleConfigPath(bundleModule))
                {
                    AssetBundle bundleConfig = null;
                    if (BundleSettings.Instance.bundleEncrypt.isEncrypt)
                    {
                        bundleConfig = await AssetBundle.LoadFromMemoryAsync(AES.AESFileByteDecrypt(mBundleConfigPath,BundleSettings.Instance.bundleEncrypt.encryptKey));
                    }
                    else
                    {
                        bundleConfig = await AssetBundle.LoadFromFileAsync(mBundleConfigPath);
                    }

                    string bundleConfigJson = (await bundleConfig.LoadAssetAsync<TextAsset>(mAssetsBundleConfigName) as TextAsset)?.text;
                    mAlreadyLoadBundleModuleList.Add(bundleModule);
                    if (bundleConfigJson != null)
                    {
                        BundleConfig bundleManife = JsonConvert.DeserializeObject<BundleConfig>(bundleConfigJson);
                        foreach (var info in bundleManife.bundleInfoList)
                        {
                            if (!mAllBundleAssetDic.ContainsKey(info.crc))
                            {
                                BundleItem item = new BundleItem();
                                item.path = info.path;
                                item.crc = info.crc;
                                item.bundleModuleType = bundleModule;
                                item.assetName = info.assetName;
                                item.bundleDependce = info.bundleDependce;
                                item.bundleName = info.bundleName;
                                item.isAddressableAsset = info.isAddressableAsset;
                                mAllBundleAssetDic.Add(item.crc, item);
                            }
                            else
                            {
                                Debug.LogWarning("AssetBundle Already Exists! BundleName:" + info.bundleName);
                            }
                        }
                        
                        bundleConfig.Unload(false);
                        Debug.Log("初始化AssetModule成功 ： " + bundleModule);
                        return true;
                    }
                    return false;
                }
                else
                {
                    Debug.LogError("不存在AssetBundleConfig");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("加载AssetBundleConfig失败，：" + e.Message);
                return false;
            }
        }

        private void LoadAllBundleModule()
        {
            if (mAllBundleModuleDic.Count > 0)
            {
                return;
            }
            
            TextAsset textAsset = Resources.Load<TextAsset>("bundlemoduleCfg");
            if (textAsset != null)
            {
                List<BundleModuleData> bundlemoduleDatas = JsonConvert.DeserializeObject<List<BundleModuleData>>(textAsset.text);
                foreach (var item in bundlemoduleDatas)
                {
                    mAllBundleModuleDic.TryAdd(item.moduleName, item);
                }
            }
        }

        /// <summary>
        /// 生成AssetBundleConfig配置文件路径
        /// </summary>
        /// <param name="bundleModule"></param>
        /// <returns></returns>
        private bool GeneratorBundleConfigPath(BundleModuleEnum bundleModule)
        {
            mAssetsBundleConfigName = bundleModule.ToString().ToLower() + "assetbundleconfig";
            mBundleConfigName = bundleModule.ToString().ToLower() + "bundleconfig" + BundleSettings.Instance.ABSUFFIX;
            mBundleConfigPath = BundleSettings.Instance.GetHotAssetsPath(bundleModule) + mBundleConfigName;
            if (!File.Exists(mBundleConfigPath))
            {
                mBundleConfigPath = BundleSettings.Instance.GetAssetsDecompressPath(bundleModule) + mBundleConfigName;
                if (!File.Exists(mBundleConfigPath))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 根据AssetBundleName查询AssetBundle有哪些资源
        /// </summary>
        /// <param name="bundleName"></param>
        /// <returns></returns>
        public List<BundleItem> GetBundleItemByName(string bundleName)
        {
            List<BundleItem> itemList = new List<BundleItem>();
            foreach (var item in mAllBundleAssetDic.Values)
            {
                if (string.Equals(item.bundleName, bundleName))
                {
                    itemList.Add(item);
                }
            }
            return itemList;
        }

        /// <summary>
        /// 根据资源路径的crc获取资源详细信息
        /// </summary>
        /// <param name="crc"></param>
        /// <returns></returns>
        public BundleItem GetBundleItemByCrc(uint crc)
        {
            mAllBundleAssetDic.TryGetValue(crc,out var item);
            return item;
        }

        /// <summary>
        /// 通过资源路径crc加载该资源所在AssetBundle
        /// </summary>
        /// <param name="crc"></param>
        /// <returns></returns>
        public async UniTask<BundleItem> LoadAssetBundle(uint crc)
        {
            mAllBundleAssetDic.TryGetValue(crc, out var item);

            if (item != null)
            {
                //如果已经存在，则直接返回
                if (item.assetBundle != null)
                {
                    return item;
                }
                
                //没有存在
                item.assetBundle = await LoadAssetBundle(item.bundleName,item.bundleModuleType);

                if (item.assetBundle == null)
                {
                    Debug.LogError("加载AB包失败：" + item.bundleName);
                    return null;
                }
                
                //加载依赖包
                List<UniTask> taskList = new List<UniTask>();
                foreach (var bundleName in item.bundleDependce)
                {
                    taskList.Add(LoadAssetBundle(bundleName,item.bundleModuleType));
                }
                //到这 该资源的AB包以及依赖包都已经加载 并且存在 mAllBundleAssetDic当中
                await UniTask.WhenAll(taskList);
                return item;
            }
            else
            {
                Debug.LogError("资源不存在 AssetbundleConfig , LoadAssetBundle failed! Crc:"+crc);
                return null;
            }
        }

        /// <summary>
        /// 通过AssetBundle Name 加载 AssetBundle
        /// </summary>
        /// <param name="bundleName"></param>
        /// <param name="bundleModuleType"></param>
        /// <returns></returns>
        private async UniTask<AssetBundle> LoadAssetBundle(string bundleName, BundleModuleEnum bundleModuleType)
        {
            AssetBundleCache bundle = null;
            mAllAlreadyLoadBundleDic.TryGetValue(bundleName, out bundle);
            if (bundle == null || (bundle != null && bundle.assetBundle == null))
            {
                //如果有两个同时加载这个AB包  第二个走这里的逻辑
                if (bundle != null && mLoadUniTaskDic.TryGetValue(bundleName, out var value))
                {
                    bundle.assetBundle = await value;
                    bundle.referenceCount++;
                    return bundle.assetBundle;
                }
                
                bundle = assetbundleCachePool.Get();
                
                //计算AssetBundle加载路径
                string hotFilePath = BundleSettings.Instance.GetHotAssetsPath(bundleModuleType) + bundleName;
                bool isHotPath = File.Exists(hotFilePath);
                string bundlePath = isHotPath ? hotFilePath : BundleSettings.Instance.GetAssetsDecompressPath(bundleModuleType) + bundleName;

                if (BundleSettings.Instance.bundleEncrypt.isEncrypt)
                {
                    byte[] bytes = AES.AESFileByteDecrypt(bundlePath, BundleSettings.Instance.bundleEncrypt.encryptKey);
                    bundle.assetBundle = await AssetBundle.LoadFromMemoryAsync(bytes);
                }
                else
                {
                    bundle.assetBundle = await AssetBundle.LoadFromFileAsync(hotFilePath);
                }
                
                if (bundle.assetBundle ==null)
                {
                    Debug.LogError("AssetBundle load failed bundlePath:"+ bundlePath);
                    return null;
                }
                //AssetBundle引用计数增加
                bundle.referenceCount++;
                mAllAlreadyLoadBundleDic.Add(bundleName,bundle);
            }
            else
            {
                //已经加载过了
                bundle.referenceCount++;
            }
            return bundle.assetBundle;
        }

        /// <summary>
        /// 释放AssetBundle 并且释放AssetBundle占用的内存资源
        /// </summary>
        /// <param name="assetItem"></param>
        /// <param name="unLoad"></param>
        public void ReleaseAssets(BundleItem assetItem, bool unLoad)
        {
            //if(assetItem != null)
        }
        
    }
}
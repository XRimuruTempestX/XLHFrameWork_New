using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
        private Dictionary<string, AssetBundleCache> mAllAlreadyLoadBundleDic =
            new Dictionary<string, AssetBundleCache>();

        /// <summary>
        /// 所有AB模块字典
        /// </summary>
        private Dictionary<string, BundleModuleData> mAllBundleModuleDic = new Dictionary<string, BundleModuleData>();

        /// <summary>
        /// 正在加载中的Unitask
        /// </summary>
        private Dictionary<string, UniTask<AssetBundle>> mLoadUniTaskDic =
            new Dictionary<string, UniTask<AssetBundle>>();

        /// <summary>
        /// 缓存池
        /// </summary>
        private AssetBundleCachePool assetbundleCachePool = new AssetBundleCachePool(5000);

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

        private CancellationTokenSource _cancellationTokenSource;

        private void Log(string msg)
        {
            Debug.Log("[AssetBundleManager] " + msg);
        }

        private void LogWarn(string msg)
        {
            Debug.LogWarning("[AssetBundleManager] " + msg);
        }

        private void LogError(string msg)
        {
            Debug.LogError("[AssetBundleManager] " + msg);
        }

        /// <summary>
        /// 加载AssetBundle配置文件
        /// </summary>
        /// <param name="bundleModule"></param>
        /// <returns></returns>
        public async UniTask<bool> InitAssetModule(BundleModuleEnum bundleModule)
        {
            AssetBundle bundleConfig = null;
            try
            {
                Log("InitAssetModule start module=" + bundleModule);
                if (mAlreadyLoadBundleModuleList.Contains(bundleModule))
                {
                    LogWarn("该模块资源已经被加载过了" + bundleModule);
                    return false;
                }

                if (_cancellationTokenSource == null)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                }

                LoadAllBundleModule();

                if (GeneratorBundleConfigPath(bundleModule))
                {
                    Log("BundleConfigPath resolved configName=" + mBundleConfigName + ", jsonAssetPath=" + mAssetsBundleConfigName + ", filePath=" + mBundleConfigPath);
                    var sw = new System.Diagnostics.Stopwatch();
                    sw.Start();
                    if (BundleSettings.Instance.bundleEncrypt.isEncrypt)
                    {
                        Log("Encrypt=TRUE, loading bundle config from memory");
                        bundleConfig = await AssetBundle
                            .LoadFromMemoryAsync(AES.AESFileByteDecrypt(mBundleConfigPath,
                                BundleSettings.Instance.bundleEncrypt.encryptKey))
                            .WithCancellation(_cancellationTokenSource.Token);
                    }
                    else
                    {
                        Log("Encrypt=FALSE, loading bundle config from file");
                        bundleConfig = await AssetBundle.LoadFromFileAsync(mBundleConfigPath)
                            .WithCancellation(_cancellationTokenSource.Token);
                    }

                    sw.Stop();
                    Log("Bundle config AssetBundle loaded in " + sw.ElapsedMilliseconds + " ms");
                    int assetNameCount = 0;
                    foreach (var allAssetName in bundleConfig.GetAllAssetNames())
                    {
                        assetNameCount++;
                        Log("BundleConfig contains asset: " + allAssetName);
                    }
                    Log("mAssetsBundleConfigName=" + mAssetsBundleConfigName + ", assetCount=" + assetNameCount);
                    string bundleConfigJson =
                        (await bundleConfig.LoadAssetAsync<TextAsset>(mAssetsBundleConfigName) as TextAsset)?.text;
                    mAlreadyLoadBundleModuleList.Add(bundleModule);
                    if (bundleConfigJson != null)
                    {
                        BundleConfig bundleManife = JsonConvert.DeserializeObject<BundleConfig>(bundleConfigJson);
                        Log("Deserialized BundleConfig, bundleInfo count=" + bundleManife.bundleInfoList.Count);
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
                                Log("Register bundle item crc=" + item.crc + ", bundle=" + item.bundleName + ", asset=" + item.assetName);
                            }
                            else
                            {
                                LogWarn("AssetBundle Already Exists! BundleName:" + info.bundleName);
                            }
                        }

                        bundleConfig.Unload(false);
                        Log("初始化AssetModule成功 module=" + bundleModule + ", totalItems=" + mAllBundleAssetDic.Count);
                        return true;
                    }
                    LogError("bundleConfigJson is null !!! ");

                    return false;
                }
                else
                {
                    LogError("不存在AssetBundleConfig module=" + bundleModule);
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                LogWarn("Load canceled");
                bundleConfig?.Unload(true);
                return false;
            }
            catch (Exception e)
            {
                LogError("加载AssetBundleConfig失败: " + e.Message);
                return false;
            }
        }

        private void LoadAllBundleModule()
        {
            if (mAllBundleModuleDic.Count > 0)
            {
                Log("Bundle module dic already loaded, count=" + mAllBundleModuleDic.Count);
                return;
            }

            TextAsset textAsset = Resources.Load<TextAsset>("bundlemoduleCfg");
            if (textAsset != null)
            {
                List<BundleModuleData> bundlemoduleDatas =
                    JsonConvert.DeserializeObject<List<BundleModuleData>>(textAsset.text);
                foreach (var item in bundlemoduleDatas)
                {
                    mAllBundleModuleDic.TryAdd(item.moduleName, item);
                    Log("Register bundle module name=" + item.moduleName );
                }
                Log("Bundle module loaded count=" + mAllBundleModuleDic.Count);
            }
            else
            {
                LogWarn("bundlemoduleCfg resource not found");
            }
        }

        /// <summary>
        /// 生成AssetBundleConfig配置文件路径
        /// </summary>
        /// <param name="bundleModule"></param>
        /// <returns></returns>
        private bool GeneratorBundleConfigPath(BundleModuleEnum bundleModule)
        {
            mAssetsBundleConfigName = "Assets/XLHFrameWork/XAsset/Config/" + bundleModule.ToString().ToLower() + "assetbundleconfig.json";
            mBundleConfigName = bundleModule.ToString().ToLower() + "bundleconfig" + BundleSettings.Instance.ABSUFFIX;
            mBundleConfigPath = BundleSettings.Instance.GetHotAssetsPath(bundleModule) + mBundleConfigName;
            if (!File.Exists(mBundleConfigPath))
            {
                mBundleConfigPath = BundleSettings.Instance.GetAssetsDecompressPath(bundleModule) + mBundleConfigName;
                if (!File.Exists(mBundleConfigPath))
                {
                    LogWarn("Bundle config file not found for module=" + bundleModule + ", candidates: hot and decompress");
                    return false;
                }
                Log("Using decompress bundle config path=" + mBundleConfigPath);
            }
            else
            {
                Log("Using hot bundle config path=" + mBundleConfigPath);
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
            mAllBundleAssetDic.TryGetValue(crc, out var item);
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
                    Log("AssetBundle already loaded for crc=" + crc + ", bundle=" + item.bundleName);
                    return item;
                }

                //没有存在
                Log("Loading AssetBundle by crc=" + crc + ", bundle=" + item.bundleName + ", module=" + item.bundleModuleType);
                item.assetBundle = await LoadAssetBundle(item.bundleName, item.bundleModuleType);

                if (item.assetBundle == null)
                {
                    LogError("加载AB包失败：" + item.bundleName);
                    return null;
                }

                //加载依赖包
                List<UniTask> taskList = new List<UniTask>();
                foreach (var bundleName in item.bundleDependce)
                {
                    Log("Loading dependency bundle=" + bundleName + " for main=" + item.bundleName);
                    taskList.Add(LoadAssetBundle(bundleName, item.bundleModuleType));
                }

                //到这 该资源的AB包以及依赖包都已经加载 并且存在 mAllBundleAssetDic当中
                await UniTask.WhenAll(taskList);
                Log("All dependencies loaded for bundle=" + item.bundleName + ", depCount=" + item.bundleDependce.Count);
                return item;
            }
            else
            {
                LogError("资源不存在 AssetbundleConfig , LoadAssetBundle failed! Crc=" + crc);
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
                try
                {
                    bundle = assetbundleCachePool.Get();
                    //计算AssetBundle加载路径
                    string hotFilePath = BundleSettings.Instance.GetHotAssetsPath(bundleModuleType) + bundleName;
                    bool isHotPath = File.Exists(hotFilePath);
                    string bundlePath = isHotPath
                        ? hotFilePath
                        : BundleSettings.Instance.GetAssetsDecompressPath(bundleModuleType) + bundleName;
                    Log("Begin load bundle name=" + bundleName + ", module=" + bundleModuleType + ", useHotPath=" + isHotPath + ", path=" + bundlePath);
                    var sw = new System.Diagnostics.Stopwatch();
                    sw.Start();

                    if (BundleSettings.Instance.bundleEncrypt.isEncrypt)
                    {
                        byte[] bytes = AES.AESFileByteDecrypt(bundlePath,
                            BundleSettings.Instance.bundleEncrypt.encryptKey);

                        if (mLoadUniTaskDic.TryGetValue(bundleName, out var task))
                        {
                            Log("Existing load task found for bundle=" + bundleName + " (encrypted)");
                            bundle.assetBundle = await task;
                        }
                        else
                        {
                            var assetUniTask = AssetBundle.LoadFromMemoryAsync(bytes)
                                .ToUniTask(cancellationToken: _cancellationTokenSource.Token);
                            mLoadUniTaskDic.TryAdd(bundleName, assetUniTask);
                            Log("Created new load task for bundle=" + bundleName + " (encrypted) bytes=" + bytes.Length);
                            bundle.assetBundle = await assetUniTask;
                        }
                    }
                    else
                    {
                        if (mLoadUniTaskDic.TryGetValue(bundleName, out var task))
                        {
                            Log("Existing load task found for bundle=" + bundleName + " (file)");
                            bundle.assetBundle = await task;
                        }
                        else
                        {
                            var assetUniTask = AssetBundle.LoadFromFileAsync(hotFilePath)
                                .ToUniTask(cancellationToken: _cancellationTokenSource.Token);
                            mLoadUniTaskDic.TryAdd(bundleName, assetUniTask);
                            Log("Created new load task for bundle=" + bundleName + " (file) path=" + hotFilePath);
                            bundle.assetBundle = await assetUniTask;
                        }
                    }

                    if (bundle.assetBundle == null)
                    {
                        LogError("AssetBundle load failed path=" + bundlePath);
                        return null;
                    }

                    //AssetBundle引用计数增加
                    bundle.referenceCount++;
                    mAllAlreadyLoadBundleDic.Add(bundleName, bundle);
                    sw.Stop();
                    Log("Bundle loaded name=" + bundleName + ", refCount=" + bundle.referenceCount + ", elapsed=" + sw.ElapsedMilliseconds + " ms");
                }
                catch (OperationCanceledException e)
                {
                    LogError("加载任务取消: " + e.Message);
                    bundle?.assetBundle?.Unload(true);
                }
            }
            else
            {
                //已经加载过了
                bundle.referenceCount++;
                Log("Bundle already loaded name=" + bundleName + ", newRefCount=" + bundle.referenceCount);
            }

            return bundle?.assetBundle;
        }

        /// <summary>
        /// 释放AssetBundle 并且释放AssetBundle占用的内存资源
        /// </summary>
        /// <param name="assetItem"></param>
        /// <param name="unLoad"></param>
        public void ReleaseAssets(BundleItem assetItem, bool unLoad)
        {
            if (assetItem != null)
            {
                if (assetItem.obj != null)
                    assetItem.obj = null;
                if (assetItem.objArr != null)
                    assetItem.objArr = null;

                
                ReleaseAssetBundle(assetItem, unLoad);

                if (assetItem.bundleDependce != null)
                {
                    foreach (var bundleName in assetItem.bundleDependce)
                    {
                        Log("Release dependency bundle=" + bundleName + ", unLoad=" + unLoad);
                        ReleaseAssetBundle(null, unLoad, bundleName);
                    }
                }
                Log("ReleaseAssets done for bundle=" + assetItem.bundleName + ", unLoad=" + unLoad);
            }
            else
            {
                LogError("assetitem is null, release Assets failed");
            }
        }

        public void CancleToken()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            LogWarn("Cancellation token canceled and disposed");
        }

        private void ReleaseAssetBundle(BundleItem assetItem, bool unLoad, string bundleName = "")
        {
            string assetBundleName = assetItem == null ? bundleName : assetItem.bundleName;
            if (!string.IsNullOrEmpty(assetBundleName) &&
                mAllAlreadyLoadBundleDic.TryGetValue(assetBundleName, out var bundleCacheItem))
            {
                if (bundleCacheItem.assetBundle != null)
                {
                    bundleCacheItem.referenceCount--;
                    if (bundleCacheItem.referenceCount <= 0)
                    {
                        bundleCacheItem.assetBundle.Unload(unLoad);
                        mAllAlreadyLoadBundleDic.Remove(assetBundleName);
                        assetbundleCachePool.Release(bundleCacheItem);
                        mLoadUniTaskDic.Remove(assetBundleName);
                        Log("AssetBundle unloaded name=" + assetBundleName + ", unLoad=" + unLoad);
                    }
                    else
                    {
                        Log("AssetBundle decref name=" + assetBundleName + ", refCount=" + bundleCacheItem.referenceCount);
                    }
                }
                else
                {
                    LogWarn("ReleaseAssetBundle but bundle is null name=" + assetBundleName);
                }
            }
            else
            {
                LogWarn("ReleaseAssetBundle miss name=" + assetBundleName);
            }
        }
    }
}

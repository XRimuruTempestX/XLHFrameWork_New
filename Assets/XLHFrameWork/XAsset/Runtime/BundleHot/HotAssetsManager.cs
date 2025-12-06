using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using XLHFrameWork.XAsset.Config;

namespace XLHFrameWork.XAsset.Runtime.BundleHot
{
    public class HotAssetsManager : IHotAssets
    {
        public class DownLoadModule
        {
            public BundleModuleEnum bundleModule;
            public HotAssetsModule hotAssetsModule;
            public bool checkAssetversion;
            public Action<BundleModuleEnum> starHot;
            public Action<BundleModuleEnum> waiteDownloadCallBack;
            public Action<HotFileInfo> onDownLoadSuccess;
            public Action<HotFileInfo> onDownLoadFailed;
            public Action<HotAssetsModule> onDownLoadFinish;
        }
        
        public class ModuleDownProgress
        {
            public float currentDownLoadSizeM;
            public float totalDownLoadSizeM;
            public float speedDownLoadSizeM;
        }

        private int MAX_THREAD_COUNT = 3;

        /// <summary>
        /// 所有热更模块
        /// </summary>
        private Dictionary<BundleModuleEnum, DownLoadModule> mAllAssetsModuledic =
            new Dictionary<BundleModuleEnum, DownLoadModule>();

        /// <summary>
        /// 正在下载热更资源模块字典
        /// </summary>
        private Dictionary<BundleModuleEnum, DownLoadModule> mDownLoadingAssetsModuleDic =
            new Dictionary<BundleModuleEnum, DownLoadModule>();

        /// <summary>
        /// 正在下载的令牌
        /// </summary>
        private Dictionary<BundleModuleEnum, CancellationTokenSource> mAllDownLoadAssetsModuleTokenSourceDic =
            new Dictionary<BundleModuleEnum, CancellationTokenSource>();

        private Queue<DownLoadModule> mWaitDownLoadQueue = new Queue<DownLoadModule>();

        /// <summary>
        /// 所有模块各自的下载进度
        /// </summary>
        public static Dictionary<BundleModuleEnum, ModuleDownProgress>  mAllDownLoadAssetsModuleProgress =  new Dictionary<BundleModuleEnum, ModuleDownProgress>();

        /// <summary>
        /// 开始热更
        /// </summary>
        /// <param name="bundleModule">热更模块</param>
        /// <param name="startHotCallBack">开始热更回调</param>
        /// <param name="waiteDownloadCallBack">等待下载回调</param>
        /// <param name="onDownLoadSuccess">下载成功回调</param>
        /// <param name="onDownLoadFailed">下载单个文件失败回调</param>
        /// <param name="onDownLoadFinish">下载完成回调</param>
        /// <param name="isCheckAssetsVersion">是否检测版本</param>
        /// <returns></returns>
        public async UniTask StartHotAsset(BundleModuleEnum bundleModule, Action<BundleModuleEnum> startHotCallBack,
            Action<BundleModuleEnum> waiteDownloadCallBack,
            Action<HotFileInfo> onDownLoadSuccess, Action<HotFileInfo> onDownLoadFailed,
            Action<HotAssetsModule> onDownLoadFinish, bool isCheckAssetsVersion = true)
        {
            if (BundleSettings.Instance.bundleHotType == BundleHotEnum.NoHot || !isCheckAssetsVersion)
            {
                Debug.Log("不需要热更或检测版本 直接返回");
                onDownLoadFinish?.Invoke(null);
                return;
            }

            //读取配置表中开启的最大线程
            MAX_THREAD_COUNT = BundleSettings.Instance.MAX_THREAD_COUNT;
            var downLoadModule = GetOrNewHotAssetsModule(bundleModule, startHotCallBack, waiteDownloadCallBack,
                onDownLoadSuccess, onDownLoadFailed, onDownLoadFinish, isCheckAssetsVersion);
            HotAssetsModule assetsModule = downLoadModule.hotAssetsModule;
            if (mDownLoadingAssetsModuleDic.Count < MAX_THREAD_COUNT)
            {
                //开始热更
                var data = await CheckAssetsVersion(assetsModule.CurBundleModuleEnum);
                if (data.Item1)
                {
                    assetsModule.onDownLoadFinish += HotModuleAssetsFinish;
                    if (!mDownLoadingAssetsModuleDic.ContainsKey(assetsModule.CurBundleModuleEnum))
                    {
                        mDownLoadingAssetsModuleDic.Add(assetsModule.CurBundleModuleEnum, downLoadModule);
                    }
                    MultipleThreadBalancing();
                    mAllDownLoadAssetsModuleProgress.TryAdd(assetsModule.CurBundleModuleEnum, new ModuleDownProgress()
                    {
                        currentDownLoadSizeM = 0,
                        totalDownLoadSizeM = data.Item2,
                    });
                    assetsModule.StartDownLoadHotAssets(downLoadModule.starHot);
                }
                else
                {
                    Debug.Log($"{bundleModule}无需热更");
                    assetsModule.onDownLoadFinish?.Invoke(assetsModule);
                    //初始化资源加载框架 TODO...
                    return;
                }
            }
            else
            {
                downLoadModule.waiteDownloadCallBack?.Invoke(downLoadModule.bundleModule);
                mWaitDownLoadQueue.Enqueue(downLoadModule);
            }
        }

        public async UniTask<(bool, float)> CheckAssetsVersion(BundleModuleEnum bundleModule)
        {
            if (BundleSettings.Instance.bundleHotType == BundleHotEnum.NoHot)
            {
                Debug.Log("不需要进行热更 直接返回");
                return (false, 0);
            }

            DownLoadModule assetsModule = GetHotAssetsModule(bundleModule);
            if (assetsModule == null)
            {
                Debug.LogError("字典没有这个模块：" + bundleModule);
                return (false, 0);
            }
            else
            {
                var result = await assetsModule.hotAssetsModule.CheckAssetsVersion();
                return result;
            }
        }

        private DownLoadModule GetHotAssetsModule(BundleModuleEnum bundleModule)
        {
            DownLoadModule downLoadModule = null;

            if (mAllAssetsModuledic.ContainsKey(bundleModule))
            {
                downLoadModule = mAllAssetsModuledic[bundleModule];
            }

            return downLoadModule;
        }

        private DownLoadModule GetOrNewHotAssetsModule(BundleModuleEnum bundleModule,
            Action<BundleModuleEnum> startHotCallBack, Action<BundleModuleEnum> waiteDownloadCallBack,
            Action<HotFileInfo> onDownLoadSuccess, Action<HotFileInfo> onDownLoadFailed,
            Action<HotAssetsModule> onDownLoadFinish, bool isCheckAssetsVersion = true)
        {
            DownLoadModule downLoadModule;
            if (mAllAssetsModuledic.ContainsKey(bundleModule))
            {
                downLoadModule = mAllAssetsModuledic[bundleModule];
            }
            else
            {
                downLoadModule = new DownLoadModule();
                downLoadModule.bundleModule = bundleModule;
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                downLoadModule.hotAssetsModule = new HotAssetsModule(bundleModule, onDownLoadSuccess, onDownLoadFailed,
                    onDownLoadFinish, cancellationTokenSource);
                downLoadModule.starHot = startHotCallBack;
                downLoadModule.waiteDownloadCallBack = waiteDownloadCallBack;
                downLoadModule.onDownLoadSuccess = onDownLoadSuccess;
                downLoadModule.onDownLoadFailed = onDownLoadFailed;
                downLoadModule.onDownLoadFinish += onDownLoadFinish;
                mAllDownLoadAssetsModuleTokenSourceDic.TryAdd(bundleModule, cancellationTokenSource);
                mAllAssetsModuledic.Add(bundleModule,downLoadModule);
                
            }

            return downLoadModule;
        }


        /// <summary>
        /// 模块热更完成回调
        /// </summary>
        /// <param name="bundleModule"></param>
        private void HotModuleAssetsFinish(HotAssetsModule bundleModule)
        {
            RemoveDownLoadProgress(bundleModule.CurBundleModuleEnum);
            if (mDownLoadingAssetsModuleDic.ContainsKey(bundleModule.CurBundleModuleEnum))
            {
                mDownLoadingAssetsModuleDic.Remove(bundleModule.CurBundleModuleEnum);
                mAllDownLoadAssetsModuleTokenSourceDic.Remove(bundleModule.CurBundleModuleEnum);
            }

            if (mWaitDownLoadQueue.Count > 0)
            {
                DownLoadModule downLoadModule = mWaitDownLoadQueue.Dequeue();
                StartHotAsset(downLoadModule.bundleModule, downLoadModule.starHot, downLoadModule.waiteDownloadCallBack,
                    downLoadModule.onDownLoadSuccess
                    , downLoadModule.onDownLoadFailed, downLoadModule.onDownLoadFinish,
                    downLoadModule.checkAssetversion).Forget();
            }
            else
            {
                MultipleThreadBalancing();
            }

            /*//TODO... 下载完成后立马初始化框架
            XAsset.Instance.InitlizateResAsync(bundleModule.CurBundleModuleEnum).Forget();*/
        }

        /// <summary>
        /// 多线程均衡
        /// </summary>
        private void MultipleThreadBalancing()
        {
            int count = mDownLoadingAssetsModuleDic.Count;
            float threadCount = MAX_THREAD_COUNT * 1.0f / count;
            //主下载线程个数
            int mainThreadCount = 0;
            //通过(int) 进行强转  (int)强转：表示向下强转
            int threadBalancingCount = (int)threadCount;

            if ((int)threadCount < threadCount)
            {
                //向上取整
                mainThreadCount = Mathf.CeilToInt(threadCount);
                //向下取整
                threadBalancingCount = Mathf.FloorToInt(threadCount);
            }

            int i = 0;
            foreach (var item in mDownLoadingAssetsModuleDic.Values)
            {
                if (mainThreadCount != 0 && i == 0)
                {
                    item.hotAssetsModule.SetDownLoadThreadCount(mainThreadCount); //设置主下载线程个数
                }
                else
                {
                    item.hotAssetsModule.SetDownLoadThreadCount(threadBalancingCount);
                }

                i++;
            }
        }

        /// <summary>
        /// 获取模块下载进度
        /// </summary>
        /// <returns> xx/xxM </returns>
        public static string GetDownLoadProgress( BundleModuleEnum modules)
        {
            float curDownSize = 0;
            float totalSize = 0;
            if (mAllDownLoadAssetsModuleProgress.ContainsKey(modules))
            {
                totalSize +=  mAllDownLoadAssetsModuleProgress[modules].totalDownLoadSizeM;
                curDownSize += mAllDownLoadAssetsModuleProgress[modules].currentDownLoadSizeM;
            }
            return  $"[下载] {curDownSize:F2}MB / {totalSize:F2}MB ({curDownSize/totalSize:P0})  " +
                    $" 速度 {mAllDownLoadAssetsModuleProgress[modules].speedDownLoadSizeM:F1} KB/s";
        }

        /// <summary>
        /// 移除下载模块下载数据
        /// </summary>
        /// <param name="bundleModule"></param>
        public static void RemoveDownLoadProgress(BundleModuleEnum bundleModule)
        {
            if (mAllDownLoadAssetsModuleProgress.ContainsKey(bundleModule))
            {
                mAllDownLoadAssetsModuleProgress.Remove(bundleModule);
            }
        }
    }
}
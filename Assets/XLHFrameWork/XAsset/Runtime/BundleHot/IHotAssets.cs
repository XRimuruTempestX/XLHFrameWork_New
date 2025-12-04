using System;
using Cysharp.Threading.Tasks;
using XLHFrameWork.XAsset.Config;

namespace XLHFrameWork.XAsset.Runtime.BundleHot
{
    public interface IHotAssets
    {
        /// <summary>
        /// 开始下载
        /// </summary>
        /// <param name="bundleModule"></param>
        /// <param name="startHotCallBack"></param>
        /// <param name="hotFinishCallBack"></param>
        /// <param name="waiteDownloadCallBack"></param>
        /// <param name="isCheckAssetsVersion"></param>
        /// <returns></returns>
        UniTask StartHotAsset(BundleModuleEnum bundleModule, Action<BundleModuleEnum> startHotCallBack,Action<BundleModuleEnum> hotFinishCallBack,
            Action<BundleModuleEnum> waiteDownloadCallBack,bool isCheckAssetsVersion = true);
        
        /// <summary>
        /// 检测版本是否需要热更  return 是否热更结果，热更大小
        /// </summary>
        /// <param name="bundleModule"></param>
        /// <returns></returns>
        UniTask<(bool,float)> CheckAssetsVersion(BundleModuleEnum bundleModule);
        
    }
}
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using XLHFrameWork.XAsset.Config;
using XLHFrameWork.XAsset.Runtime.BundleHot;
using XLHFrameWork.XAsset.Runtime.BundleLoad;
using XLHFrameWork.XAsset.Runtime.Helper;
using Object = UnityEngine.Object;

namespace XLHFrameWork.XAsset.Runtime
{
    public class XAssetFrameWork : Singleton<XAssetFrameWork>
    {
        private IHotAssets mHotAssets = null;

        private IResourceInterface mResourceMgr = null;

        private void Initialize()
        {
            mHotAssets = new HotAssetsManager();
            mResourceMgr = new XLHResourceManager();
            mResourceMgr.Initlizate();
        }

        public async UniTask InitlizateResAsync(BundleModuleEnum bundleModule)
        {
            if (mHotAssets == null || mResourceMgr == null)
            {
                Initialize();
            }
            await mResourceMgr.InitAssetModule(bundleModule);
        }

        /// <summary>
        /// 热更模块  ----->>>>>>>  游戏初始化调用
        /// </summary>
        /// <param name="bundleModule"></param>
        /// <param name="startHotCallBack"></param>
        /// <param name="waiteDownloadCallBack"></param>
        /// <param name="onDownLoadSuccess"></param>
        /// <param name="onDownLoadFailed"></param>
        /// <param name="onDownLoadFinish"></param>
        /// <param name="isCheckAssetsVersion"></param>
        public async UniTask StartHotAsset(BundleModuleEnum bundleModule, Action<BundleModuleEnum> startHotCallBack,
            Action<BundleModuleEnum> waiteDownloadCallBack,
            Action<HotFileInfo> onDownLoadSuccess, Action<HotFileInfo> onDownLoadFailed,
            Action<HotAssetsModule> onDownLoadFinish, bool isCheckAssetsVersion = true)
        {
            if (mHotAssets == null || mResourceMgr == null)
            {
                Initialize();
            }

            mHotAssets?.StartHotAsset(bundleModule, startHotCallBack, waiteDownloadCallBack,onDownLoadSuccess,onDownLoadFailed,onDownLoadFinish,isCheckAssetsVersion);
           // await InitlizateResAsync(bundleModule);
        }

        /// <summary>
        /// 加载并实例化一个GameObject
        /// </summary>
        /// <param name="path"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public async UniTask<GameObject> InstantiateAsync(string path,Transform parent = null)
        {
            return await mResourceMgr.InstantiateAsync(path,parent);
        }


        /// <summary>
        /// 加载非实例化资源
        /// </summary>
        /// <param name="path"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async UniTask<T> LoadAssetAsync<T>(string path)  where T : Object
        {
            return await mResourceMgr.LoadAssetAsync<T>(path);
        }

        /// <summary>
        /// 释放实例化的资源
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="isDestroy">false 放入缓存池中  , true 销毁实例化的资源</param>
        public void ReleaseGameObject(GameObject obj , bool isDestroy = false)
        {
            mResourceMgr.Release(obj, isDestroy);
        }

        /// <summary>
        /// 清空所有加载的资源   
        /// </summary>
        /// <param name="isClearAll">false 不会释放已经加载的资源，  true 释放所有由框架加载的资源</param>
        public void ReleaseAllAssets(bool isClearAll = false)
        {
            mResourceMgr.ClearResourcesAssets(isClearAll);
        }
    }
}
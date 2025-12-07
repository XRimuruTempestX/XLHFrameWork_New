using Cysharp.Threading.Tasks;
using UnityEngine;
using XLHFrameWork.XAsset.Config;

namespace XLHFrameWork.XAsset.Runtime.BundleLoad
{
    public interface IResourceInterface
    {
        void Initlizate();

        UniTask<bool> InitAssetModule(BundleModuleEnum bundleModule);

        UniTask PreLoadObjAsync(string path, int count = 1);

        UniTask<GameObject> InstantiateAsync(string path,Transform parent);
        
        UniTask<T> LoadAssetAsync<T>(string path, string suffix = "") where T : Object;

        void Release(GameObject obj, bool destroyCache = false);
        
        void ClearAllAsyncLoadTask();

        void ClearResourcesAssets(bool absoluteCleaning);//是否深度清理
        
    }
}
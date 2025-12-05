using XLHFrameWork.PoolManager;
using XLHFrameWork.XAsset.Runtime.BundleLoad;

namespace XLHFrameWork.XAsset.Runtime.Helper
{
    public class AssetBundleCachePool : ClassPool<AssetBundleCache>
    {
        protected override void OnRelease(AssetBundleCache obj)
        {
            obj.Release();
        }
    }
}
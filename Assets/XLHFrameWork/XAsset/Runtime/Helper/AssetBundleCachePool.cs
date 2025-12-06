using XLHFrameWork.PoolManager;
using XLHFrameWork.XAsset.Runtime.BundleLoad;

namespace XLHFrameWork.XAsset.Runtime.Helper
{
    public class AssetBundleCachePool : ClassPool<AssetBundleCache>
    {

        public AssetBundleCachePool(int capacity) : base(capacity)
        {
            
        }
        
        protected override void OnRelease(AssetBundleCache obj)
        {
            obj.Release();
        }
    }
}
using XLHFrameWork.PoolManager;
using XLHFrameWork.XAsset.Runtime.BundleLoad;

namespace XLHFrameWork.XAsset.Runtime.Helper
{
    public class CacheObjectPool : ClassPool<CacheObject>
    {
        protected override void OnRelease(CacheObject obj)
        {
            base.OnRelease(obj);
            obj.Release();
        }
    }
}
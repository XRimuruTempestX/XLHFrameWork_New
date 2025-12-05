using UnityEngine;
using UnityEngine.Pool;

namespace XLHFrameWork.PoolManager
{
    public class ClassPool<T> where T : class,new()
    {
        private IObjectPool<T> pool;

        public ClassPool(int maxSize = 100)
        {
            pool = new ObjectPool<T>(
                createFunc: ()=>new T(),
                actionOnGet:OnGet,
                actionOnRelease:OnRelease,
                actionOnDestroy:null,
                maxSize:maxSize
                );
        }
        
        protected virtual void OnGet(T obj){}
        protected virtual void OnRelease(T obj){}
        
        public T Get() => pool.Get();
        
        public void Release(T obj) => pool.Release(obj);
    }
}

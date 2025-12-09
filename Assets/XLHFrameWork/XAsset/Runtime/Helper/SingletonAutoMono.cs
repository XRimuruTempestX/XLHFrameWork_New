using UnityEngine;

namespace XLHFrameWork.XAsset.Runtime.Helper
{
    
    /// <summary>
    /// 自动挂载式的 继承Mono的单例模式基类
    /// 推荐使用 
    /// 无需手动挂载 无需动态添加 无需关心切场景带来的问题
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SingletonAutoMono<T> : MonoBehaviour where T:MonoBehaviour
    {
        private static T instance;

        public static T Instance
        {
            get
            {
                if(instance == null)
                {
                    GameObject obj = new GameObject();
                    obj.name = typeof(T).ToString();
                    instance = obj.AddComponent<T>();
                    DontDestroyOnLoad(obj);
                }
                return instance;
            }
        }

    }

}

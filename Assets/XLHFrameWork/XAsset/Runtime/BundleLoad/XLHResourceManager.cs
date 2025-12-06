using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using XLHFrameWork.XAsset.Config;
using XLHFrameWork.XAsset.Runtime.BundleHot;
using XLHFrameWork.XAsset.Runtime.Helper;
using Object = UnityEngine.Object;

namespace XLHFrameWork.XAsset.Runtime.BundleLoad
{

    public class CacheObject
    {
        public uint crc;
        public string path;
        public int insid;
        public Object obj;

        public void Release()
        {
            crc = 0;
            path = "";
            insid = 0;
            obj = null;
        }
    }
    
    public class XLHResourceManager : IResourceInterface
    {
        
        /// <summary>
        /// 对象池字典
        /// </summary>
        private Dictionary<uint, List<CacheObject>> mObjectPoolDic =  new Dictionary<uint, List<CacheObject>>();
        
        /// <summary>
        /// 记录加载出来的对象的缓存对象
        /// </summary>
        private Dictionary<int, CacheObject> mAllObjectDic = new Dictionary<int, CacheObject>();
        
        /// <summary>
        /// 已经加载出来的对象id ---> crc
        /// </summary>
        private Dictionary<int, uint> mAlreadyLoadAssetDic = new Dictionary<int, uint>();
        
        /// <summary>
        /// 缓存对象类对象池
        /// </summary>
        private CacheObjectPool mCacheObjectPool = new CacheObjectPool();


        private GameObject mRoot;
        
        public void Initlizate()
        {
            mRoot = new  GameObject();
            mRoot.name = "PoolRoot";
            GameObject.DontDestroyOnLoad(mRoot);
        }

        public async UniTask<bool> InitAssetModule(BundleModuleEnum bundleModule)
        {
            return await AssetBundleManager.Instance.InitAssetModule(bundleModule);
        }

        public UniTask PreLoadObjAsync(string path, int count = 1)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 加载并且实例化出一个对象
        /// </summary>
        /// <param name="path"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public async UniTask<GameObject> InstantiateAsync(string path, Transform parent)
        {
            path = path.EndsWith(".prefab") ? path : path + ".prefab";

            CacheObject cacheObj = GetCacheObjFromPools(Crc32.GetCrc32(path));
            if (cacheObj != null && cacheObj.obj != null)
            {
                (cacheObj.obj as GameObject).transform.SetParent(parent);
                (cacheObj.obj as GameObject).SetActive(true);
                return cacheObj.obj as  GameObject;
            }
            cacheObj = mCacheObjectPool.Get();
            GameObject loadObj = await LoadResourceAsync<GameObject>(path);
            if (loadObj == null)
            {
                Debug.LogError("加载游戏对象失败 -------->" + path);
                GameObject errObj = new GameObject("Load_Error_GameObject");
                return errObj;
            }

            GameObject instObj = GameObject.Instantiate(loadObj, parent);
            int insId = instObj.gameObject.GetInstanceID();
            cacheObj.crc = Crc32.GetCrc32(path);
            cacheObj.path = path;
            cacheObj.insid = insId;
            cacheObj.obj = instObj;
            mAllObjectDic.TryAdd(insId, cacheObj);
            mAlreadyLoadAssetDic.TryAdd(insId, Crc32.GetCrc32(path));
            return instObj;
        }

        /// <summary>
        /// 加载非实例化资源接口
        /// </summary>
        /// <param name="path"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async UniTask<T> LoadAssetAsync<T>(string path, string suffix = "") where T : Object
        {
            path = path + suffix;
            CacheObject cacheObj = GetCacheObjFromPools(Crc32.GetCrc32(path));
            if (cacheObj != null && cacheObj.obj != null)
            {
                return cacheObj.obj as T;
            }

            cacheObj = mCacheObjectPool.Get();
            T loadObj = await LoadResourceAsync<T>(path);
            if (loadObj == null)
            {
                Debug.LogError("加载资源失败 -------->" + path);
                return default(T);
            }
            int insId = loadObj.GetInstanceID();
            cacheObj.crc = Crc32.GetCrc32(path);
            cacheObj.path = path;
            cacheObj.insid = insId;
            cacheObj.obj = loadObj;
            mAllObjectDic.TryAdd(insId, cacheObj);
            mAlreadyLoadAssetDic.TryAdd(insId, Crc32.GetCrc32(path));
            return loadObj;
        }

        /// <summary>
        /// 用于接在非实例化资源接口
        /// </summary>
        /// <param name="path"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async UniTask<T> LoadResourceAsync<T>(string path) where T : Object
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("path is null or empty");
                return null;
            }

            uint crc = Crc32.GetCrc32(path);

            BundleItem item = AssetBundleManager.Instance.GetBundleItemByCrc(crc);
            if (item != null)
            {
                if (item.obj != null)
                    return (T)item.obj;

                T obj = null;

#if UNITY_EDITOR
                if (BundleSettings.Instance.loadAssetType == LoadAssetEnum.Editor)
                {
                    obj = LoadAssetsFromEditor<T>(path);
                    if (obj == null)
                    {
                        Debug.LogError("Load object is null : " + path);
                        return null;
                    }
                    item.obj = obj;
                    return obj;
                }
#endif
                item = await AssetBundleManager.Instance.LoadAssetBundle(crc);
                if (item.obj != null)
                {
                    return item.obj as T;
                }

                T loadObj = await item.assetBundle.LoadAssetAsync<T>(item.path) as T;
                return loadObj;
            }
            else
            {
                Debug.LogError("item is null : " + path);
                Debug.LogError("item is null : " + crc.ToString());
                return null;
            }
        }


        /// <summary>
        /// 从对象池中取出缓存对象
        /// </summary>
        /// <param name="crc"></param>
        /// <returns></returns>
        private CacheObject GetCacheObjFromPools(uint crc)
        {
            mObjectPoolDic.TryGetValue(crc, out List<CacheObject> objectList);
            if (objectList != null && objectList.Count > 0)
            {
                CacheObject obj = objectList[^1];
                objectList.Remove(obj);
                return obj;
            }

            return null;
        }

#if UNITY_EDITOR
        private T LoadAssetsFromEditor<T>(string path) where T : Object
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        }
#endif

        /// <summary>
        /// 释放一个实例化资源
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="destroyCache"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void Release(GameObject obj, bool destroyCache = false)
        {
            int insid = obj.GetInstanceID();
            
            mAllObjectDic.TryGetValue(insid, out CacheObject cacheObj);
            if (cacheObj == null)
            {
                Debug.LogError("这个对象并不是从缓存池中加载 ------------>>>>>" + obj.name);
                return;
            }

            uint crc = cacheObj.crc;
            
            if (destroyCache)
            {
                GameObject.Destroy(obj);
                mAllObjectDic.Remove(insid);
                mObjectPoolDic.TryGetValue(cacheObj.crc, out List<CacheObject> objectList);
                if (objectList != null)
                {
                    if (objectList.Contains(cacheObj))
                    {
                        objectList.Remove(cacheObj);
                    }
                    mCacheObjectPool.Release(cacheObj);
                }
                //如果池子已经没有这个对象
                BundleItem item = AssetBundleManager.Instance.GetBundleItemByCrc(crc);
                AssetBundleManager.Instance.ReleaseAssets(item,true);
                mAlreadyLoadAssetDic.Remove(insid);
            }
            else
            {
             //   mAllObjectDic.TryGetValue(insid, out var cacheObejct);
                mObjectPoolDic.TryGetValue(crc,out var objectList);
                //如果池子还没有
                if (objectList == null)
                {
                    objectList = new List<CacheObject>();
                    objectList.Add(cacheObj);
                    mObjectPoolDic.Add(crc, objectList);
                }
                else
                {
                    objectList.Add(cacheObj);
                }

                if (cacheObj.obj != null)
                {
                    (cacheObj.obj as GameObject)?.transform.SetParent(mRoot.transform);
                }
                else
                {
                    Debug.LogError("缓存obj is null  释放失败");
                }
            }
        }

        public void ClearAllAsyncLoadTask()
        {
            throw new System.NotImplementedException();
        }

        public void ClearResourcesAssets(bool absoluteCleaning)
        {

            if (absoluteCleaning)
            {
                foreach (var item in mAllObjectDic)
                {
                    if (item.Value.obj != null)
                    {
                        //销毁Gameobject对象，回收缓存类对象，等待下次复用
                        GameObject.Destroy(item.Value.obj as  GameObject);
                        mCacheObjectPool.Release(item.Value);
                    }
                }
                mAllObjectDic.Clear();
                mObjectPoolDic.Clear();
            }
            else
            {
                foreach (var objList in mObjectPoolDic.Values)
                {
                    if (objList != null)
                    {
                        foreach (var cacheObejct in objList)
                        {
                            if (cacheObejct != null)
                            {
                                //销毁Gameobject对象，回收缓存类对象，等待下次复用
                                GameObject.Destroy(cacheObejct.obj as  GameObject);
                                mCacheObjectPool.Release(cacheObejct);
                            }
                        }
                    }
                }
                mObjectPoolDic.Clear();
            }
            
            foreach (var crc in mAlreadyLoadAssetDic.Values)
            {
                BundleItem item =  AssetBundleManager.Instance.GetBundleItemByCrc(crc);
                AssetBundleManager.Instance.ReleaseAssets(item,absoluteCleaning);
            }

            //清理列表
            mAlreadyLoadAssetDic.Clear();
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }
        
    }
}
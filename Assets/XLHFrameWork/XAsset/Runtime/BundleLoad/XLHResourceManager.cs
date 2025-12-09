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
        private Dictionary<uint, List<CacheObject>> mObjectPoolDic = new Dictionary<uint, List<CacheObject>>();

        /// <summary>
        /// 记录加载出来的对象的缓存对象
        /// </summary>
        private Dictionary<int, CacheObject> mAllObjectDic = new Dictionary<int, CacheObject>();

        /// <summary>
        /// 已经加载出来的对象crc ---> id
        /// </summary>
        private Dictionary<uint, List<int>> mAlreadyLoadAssetDic = new Dictionary<uint, List<int>>();

        /// <summary>
        /// 缓存对象类对象池
        /// </summary>
        private CacheObjectPool mCacheObjectPool = new CacheObjectPool();


        private GameObject mRoot;

        private void Log(string msg)
        {
            Debug.Log("[XLHResourceManager] " + msg);
        }

        private void LogWarn(string msg)
        {
            Debug.LogWarning("[XLHResourceManager] " + msg);
        }

        private void LogError(string msg)
        {
            Debug.LogError("[XLHResourceManager] " + msg);
        }

        public void Initlizate()
        {
            mRoot = new GameObject();
            mRoot.name = "PoolRoot";
            GameObject.DontDestroyOnLoad(mRoot);
            Log("Initlizate done, pool root created name=" + mRoot.name);
        }

        public async UniTask<bool> InitAssetModule(BundleModuleEnum bundleModule)
        {
            Log("InitAssetModule pass-through module=" + bundleModule);
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
            Log("InstantiateAsync start path=" + path + ", parent=" + (parent == null ? "null" : parent.name));
            path = path.EndsWith(".prefab") ? path : path + ".prefab";
            Log("InstantiateAsync normalized path=" + path);

            CacheObject cacheObj = GetCacheObjFromPools(Crc32.GetCrc32(path));
            if (cacheObj != null && cacheObj.obj != null)
            {
                Log("Pool hit crc=" + cacheObj.crc + ", objId=" + cacheObj.insid + ", name=" +
                    (cacheObj.obj as GameObject)?.name);
                (cacheObj.obj as GameObject).transform.SetParent(parent);
                (cacheObj.obj as GameObject).SetActive(true);
                return cacheObj.obj as GameObject;
            }

            cacheObj = mCacheObjectPool.Get();
            Log("Pool miss, loading resource path=" + path);
            GameObject loadObj = await LoadResourceAsync<GameObject>(path);
            if (loadObj == null)
            {
                LogError("加载游戏对象失败 path=" + path);
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
            if (mAlreadyLoadAssetDic.TryGetValue(Crc32.GetCrc32(path), out List<int> list))
            {
                if (list == null)
                {
                    list = new List<int>();
                    list.Add(cacheObj.insid);
                }
                else
                {
                    list.Add(cacheObj.insid);
                }
            }
            else
            {
                mAlreadyLoadAssetDic.Add(Crc32.GetCrc32(path), new List<int>() { cacheObj.insid });
            }

            Log("InstantiateAsync done name=" + instObj.name + ", insId=" + insId + ", crc=" + cacheObj.crc);
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
            Log("LoadAssetAsync start path=" + path + ", type=" + typeof(T).Name);
            CacheObject cacheObj = GetCacheObjFromPools(Crc32.GetCrc32(path));
            if (cacheObj != null && cacheObj.obj != null)
            {
                Log("Pool hit crc=" + cacheObj.crc + ", objId=" + cacheObj.insid);
                return cacheObj.obj as T;
            }

            cacheObj = mCacheObjectPool.Get();
            T loadObj = await LoadResourceAsync<T>(path);
            if (loadObj == null)
            {
                LogError("加载资源失败 path=" + path);
                return default(T);
            }

            int insId = loadObj.GetInstanceID();
            cacheObj.crc = Crc32.GetCrc32(path);
            cacheObj.path = path;
            cacheObj.insid = insId;
            cacheObj.obj = loadObj;
            mAllObjectDic.TryAdd(insId, cacheObj);
            if (mAlreadyLoadAssetDic.TryGetValue(Crc32.GetCrc32(path), out List<int> list))
            {
                if (list == null)
                {
                    list = new List<int>();
                    list.Add(cacheObj.insid);
                }
                else
                {
                    list.Add(cacheObj.insid);
                }
            }
            else
            {
                mAlreadyLoadAssetDic.Add(Crc32.GetCrc32(path), new List<int>() { cacheObj.insid });
            }

            Log("LoadAssetAsync done objId=" + insId + ", crc=" + cacheObj.crc);
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
                LogError("path is null or empty");
                return null;
            }

            uint crc = Crc32.GetCrc32(path);
            Log("LoadResourceAsync start path=" + path + ", crc=" + crc + ", type=" + typeof(T).Name);

            BundleItem item = AssetBundleManager.Instance.GetBundleItemByCrc(crc);
            if (item.obj != null)
            {
                Log("BundleItem already has obj, returning cached for path=" + path);
                return (T)item.obj;
            }

            T obj = null;

#if UNITY_EDITOR
            if (BundleSettings.Instance.loadAssetType == LoadAssetEnum.Editor)
            {
                obj = LoadAssetsFromEditor<T>(path);
                if (obj == null)
                {
                    LogError("Load object is null path=" + path);
                    return null;
                }

                // item.obj = obj;
                Log("Editor mode load success path=" + path);
                return obj;
            }
#endif
            item = await AssetBundleManager.Instance.LoadAssetBundle(crc);
            if (item.obj != null)
            {
                Log("Item.obj present after bundle load path=" + path);
                return item.obj as T;
            }

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            T loadObj = await item.assetBundle.LoadAssetAsync<T>(item.path) as T;
            item.obj = loadObj;
            sw.Stop();
            Log("AssetBundle LoadAssetAsync done assetPath=" + item.path + ", elapsed=" + sw.ElapsedMilliseconds +
                " ms");
            return loadObj;
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
                Log("GetCacheObjFromPools hit crc=" + crc + ", remaining=" + objectList.Count);
                return obj;
            }

            Log("GetCacheObjFromPools miss crc=" + crc);
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
                LogError("对象并非从缓存池加载 name=" + obj.name);
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
                    Log("Release From Pool destroyCache=true insId=" + insid + ", crc=" + crc);
                    mCacheObjectPool.Release(cacheObj);
                }
                
                Log("Release destroyCache=true insId=" + insid + ", crc=" + crc);
                
                if (mAlreadyLoadAssetDic.TryGetValue(cacheObj.crc, out List<int> insIdList))
                {
                    if (insIdList != null)
                    {
                        foreach (int insId in insIdList)
                        {
                            if (insId == cacheObj.insid)
                            {
                                insIdList.Remove(insId);
                                break;
                            }
                        }

                        if (insIdList.Count == 0)
                        {
                            mAlreadyLoadAssetDic.Remove(cacheObj.crc);
                            BundleItem item = AssetBundleManager.Instance.GetBundleItemByCrc(crc);
                            AssetBundleManager.Instance.ReleaseAssets(item, true);
                        }
                    }
                }
            }
            else
            {
                //   mAllObjectDic.TryGetValue(insid, out var cacheObejct);
                mObjectPoolDic.TryGetValue(crc, out var objectList);
                //如果池子还没有
                if (objectList == null)
                {
                    objectList = new List<CacheObject>();
                    objectList.Add(cacheObj);
                    mObjectPoolDic.Add(crc, objectList);
                }
                else
                {
                    if (!objectList.Contains(cacheObj))
                    {
                        objectList.Add(cacheObj);
                    }
                }

                if (cacheObj.obj != null)
                {
                    (cacheObj.obj as GameObject)?.SetActive(false);
                    (cacheObj.obj as GameObject)?.transform.SetParent(mRoot.transform);
                }
                else
                {
                    LogError("缓存obj is null  释放失败");
                }

                Log("Release to pool insId=" + insid + ", poolCount=" +
                    (mObjectPoolDic.TryGetValue(crc, out var listTmp) ? listTmp.Count : 0));
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
                Log("ClearResourcesAssets absoluteCleaning=true start");
                foreach (var item in mAllObjectDic)
                {
                    if (item.Value.obj != null)
                    {
                        if (item.Value.obj is GameObject)
                            GameObject.Destroy(item.Value.obj as GameObject);
                        mCacheObjectPool.Release(item.Value);
                    }
                }

                mAllObjectDic.Clear();
                mObjectPoolDic.Clear();
                Log("Absolute cleaning done");
            }
            else
            {
                Log("ClearResourcesAssets absoluteCleaning=false start");

                /*foreach (var insList in mAlreadyLoadAssetDic.Values)
                {
                    foreach (var item in insList)
                    {
                        if (mAllObjectDic.TryGetValue(item, out var cacheObj))
                        {
                            if (cacheObj.obj is GameObject)
                            {
                                Release(cacheObj.obj as GameObject, false);
                            }
                        }
                    }
                }*/
                
                foreach (var objList in mObjectPoolDic.Values)
                {
                    if (objList != null)
                    {
                        foreach (var cacheObejct in objList)
                        {
                            if (cacheObejct != null)
                            {
                                //销毁Gameobject对象，回收缓存类对象，等待下次复用
                                if(cacheObejct.obj is GameObject)
                                    GameObject.Destroy(cacheObejct.obj as GameObject);
                                mCacheObjectPool.Release(cacheObejct);
                            }
                        }
                    }
                }
                mAllObjectDic.Clear();
                mObjectPoolDic.Clear();
                Log("Partial cleaning done");
            }

            foreach (var crc in mAlreadyLoadAssetDic.Keys)
            {
                BundleItem item = AssetBundleManager.Instance.GetBundleItemByCrc(crc);
                AssetBundleManager.Instance.ReleaseAssets(item, absoluteCleaning);
            }

            //清理列表
            mAlreadyLoadAssetDic.Clear();
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
            Log("ClearResourcesAssets finished, absoluteCleaning=" + absoluteCleaning);
        }
    }
}
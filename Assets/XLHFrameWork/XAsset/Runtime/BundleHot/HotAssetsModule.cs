using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using XLHFrameWork.XAsset.Config;
using XLHFrameWork.XAsset.Runtime.Helper;

namespace XLHFrameWork.XAsset.Runtime.BundleHot
{
    public class HotAssetsModule
    {
        /// <summary>
        /// 当前应用版本
        /// </summary>
        private string mAppVersion;

        /// <summary>
        /// 热更资源下载储存路径
        /// </summary>
        public string HotAssetsSavePath
        {
            get { return Application.persistentDataPath + "/HotAssets/" + CurBundleModuleEnum + "/"; }
        }

        /// <summary>
        /// 所有热更的资源列表
        /// </summary>
        public List<HotFileInfo> mAllHotAssetsList = new List<HotFileInfo>();

        /// <summary>
        /// 需要下载的资源列表
        /// </summary>
        public List<HotFileInfo> mNeedDownLoadAssetsList = new List<HotFileInfo>();

        /// <summary>
        /// 服务端资源清单
        /// </summary>
        private HotAssetsManifest mServerHotAssetsManifest;

        /// <summary>
        /// 本地资源清单
        /// </summary>
        private HotAssetsManifest mLocalHotAssetsManifest;

        /// <summary>
        /// 服务端资源热更清单储存路径
        /// </summary>
        private string mServerHotAssetsManifestPath
        {
            get { return Application.persistentDataPath + "/Server" + CurBundleModuleEnum + "AssetsHotManifest.json"; }
        }

        /// <summary>
        /// 本地资源热更清单文件储存路径
        /// </summary>
        private string mLocalHotAssetManifestPath
        {
            get { return Application.persistentDataPath + "/Local" + CurBundleModuleEnum + "AssetsHotManifest.json"; }
        }

        /// <summary>
        /// 热更公告
        /// </summary>
        public string UpdateNoticeContent
        {
            get { return mServerHotAssetsManifest.updateNotice; }
        }

        /// <summary>
        /// 当前下载的资源模块类型
        /// </summary>
        public BundleModuleEnum CurBundleModuleEnum { get; set; }

        /// <summary>
        /// 最大下载资源大小
        /// </summary>
        public float AssetsMaxSizeM { get; set; }

        /// <summary>
        /// 资源已经下载的大小
        /// </summary>
        public float AssetsDownLoadSizeM;

        /// <summary>
        /// 资源下载器
        /// </summary>
        private AssetsDownLoader mAssetsDownLoader;

        private Action<HotFileInfo> onDownLoadSuccess;
        private Action<HotFileInfo> onDownLoadFailed;
        private Action<HotAssetsModule> onDownLoadFinish;

        private CancellationTokenSource mCancellationTokenSource;


        public HotAssetsModule(BundleModuleEnum bundleModule, Action<HotFileInfo> onDownLoadSuccess,
            Action<HotFileInfo> onDownLoadFailed,
            Action<HotAssetsModule> onDownLoadFinish, CancellationTokenSource cancellationTokenSource)
        {
            this.CurBundleModuleEnum = bundleModule;
            this.mCancellationTokenSource = cancellationTokenSource;
            this.onDownLoadSuccess = onDownLoadSuccess;
            this.onDownLoadFailed = onDownLoadFailed;
            this.onDownLoadFinish = onDownLoadFinish;
            this.mCancellationTokenSource = cancellationTokenSource;
            this.mAppVersion = Application.version;
        }
        
        /// <summary>
        /// 开始下载热更资源
        /// </summary>
        /// <param name="startDonwLoadCallBack"></param>
        public void StartDownLoadHotAssets(Action startDonwLoadCallBack)
        {
            //优先下载AssetBUndle配置文件，下载完成后呢，调用回调，让开发者及时加载配置文件
            //热更资源下载完成之后同样给与回调，供开发者动态加载刚下载完成的资源
            List<HotFileInfo> downLoadList = new List<HotFileInfo>();
            for (int i = 0; i < mNeedDownLoadAssetsList.Count; i++)
            {
                HotFileInfo hotFile = mNeedDownLoadAssetsList[i];
                //如果包含Config 说明是配置文件，需要优先下载
                if (hotFile.abName.Contains("config"))
                {
                    downLoadList.Insert(0, hotFile);
                }
                else
                {
                    downLoadList.Add(hotFile);
                }
            }
            //获取资源下载队列
            Queue<HotFileInfo> downLoadQueue = new Queue<HotFileInfo>();
            foreach (var item in downLoadList)
            {
                downLoadQueue.Enqueue(item);
            }
            //通过资源下载器，开始下载资源
            mAssetsDownLoader = new AssetsDownLoader(this,downLoadQueue,mServerHotAssetsManifest.downLoadURL,HotAssetsSavePath,OnDownLoadSuccess
                ,OnDownLoadFailed,OnDownLoadAllFinish,mCancellationTokenSource.Token);

            startDonwLoadCallBack?.Invoke();
            //开始下载队列中的资源
            mAssetsDownLoader.StartDownLoadQueue();

        }

        /// <summary>
        /// 检查版本是否需要热更
        /// </summary>
        /// <returns></returns>
        public async UniTask<(bool, float)> CheckAssetsVersion()
        {
            mNeedDownLoadAssetsList.Clear();

            bool downloadSuccess = await DownLoadAssetsManifest();
            if (downloadSuccess)
            {
                //检查当前版本是否需要热更
                if (CheckModuleAssetsIsHot())
                {
                    HotAssetsPatch serverHotPatch = mServerHotAssetsManifest.hotAssetsPatchList[^1];
                    if (ComputeNeedHotAssetsList(serverHotPatch))
                    {
                        return (true,AssetsMaxSizeM);
                    }
                    else
                    {
                        Debug.Log("无需热更");
                        return (false, 0);
                    }
                }
                else
                {
                    Debug.Log("无需热更");
                    return (false, 0);
                }
            }
            else
            {
                Debug.LogError("下载服务器资源清单失败");
                return (false, 0);
            }
        }
        
        /// <summary>
        /// 计算需要热更的文件列表
        /// </summary>
        /// <param name="serverAssetsPath"></param>
        /// <returns></returns>
        public bool ComputeNeedHotAssetsList(HotAssetsPatch serverAssetsPath)
        {
            if (!Directory.Exists(HotAssetsSavePath))
            {
                Directory.CreateDirectory(HotAssetsSavePath);
            }
            if(File.Exists(mLocalHotAssetManifestPath))
                mLocalHotAssetsManifest = JsonConvert.DeserializeObject<HotAssetsManifest>(File.ReadAllText(mLocalHotAssetManifestPath));
            AssetsMaxSizeM = 0;
            foreach (var item in serverAssetsPath.hotAssetsList)
            {
                //获取本地AssetBundle文件路径
                string localHotFilePath = HotAssetsSavePath + item.abName;
                //获取本地解压后的AssetBundle文件路径
                string localCompressFilePath = BundleSettings.Instance.GetAssetsDecompressPath(CurBundleModuleEnum)+ item.abName;
                mAllHotAssetsList.Add(item);
                //如果本地热更文件不存在，或者本地文件与服务端不一致 就需要热更
                if (!File.Exists(localHotFilePath) ||item.md5!= MD5.GetMd5FromFile(localHotFilePath))//验证资源是否损、是否需要热更坏或被篡改
                {
                    //检测本地内嵌解压后的资源是否存在，进行二次验证，如仍不一致，则需要确定热更
                    if (!File.Exists(localCompressFilePath) || item.md5 != MD5.GetMd5FromFile(localCompressFilePath))
                    {
                        mNeedDownLoadAssetsList.Add(item);
                        AssetsMaxSizeM += item.size / 1024f;
                    }
                }
            }
            
            return mNeedDownLoadAssetsList.Count > 0;
        }
        
        /// <summary>
        /// 检测是否需要热更
        /// </summary>
        /// <returns></returns>
        public bool CheckModuleAssetsIsHot()
        {
            if (mServerHotAssetsManifest == null) return false;

            // 如果服务端版本全版本生效，直接热更
            if (mServerHotAssetsManifest.appVersion == "0.0.0") return true;

            // 应用版本不一致，不热更
            if (mServerHotAssetsManifest.appVersion != mAppVersion) return false;

            // 本地清单不存在，需要热更
            if (!File.Exists(mLocalHotAssetManifestPath)) return true;

            var localManifest = JsonConvert.DeserializeObject<HotAssetsManifest>(
                File.ReadAllText(mLocalHotAssetManifestPath));

            // 如果本地没有补丁，但服务端有补丁，需要热更
            if ((localManifest.hotAssetsPatchList?.Count ?? 0) == 0 &&
                (mServerHotAssetsManifest.hotAssetsPatchList?.Count ?? 0) > 0)
            {
                return true;
            }

            // 获取本地和服务端最后一个补丁
            var localPatch = localManifest.hotAssetsPatchList.Count > 0
                ? localManifest.hotAssetsPatchList[^1]
                : null;
            var serverPatch = mServerHotAssetsManifest.hotAssetsPatchList.Count > 0
                ? mServerHotAssetsManifest.hotAssetsPatchList[^1]
                : null;

            // 补丁版本不同，需要热更
            if (localPatch == null || serverPatch == null || localPatch.patchVersion != serverPatch.patchVersion)
                return true;

            // 如果想更精确，可以比对每个文件 md5
            foreach (var file in serverPatch.hotAssetsList)
            {
                var localFile = localPatch.hotAssetsList.Find(f => f.abName == file.abName);
                if (localFile == null || localFile.md5 != file.md5)
                    return true;
            }

            // 版本和文件一致，不需要热更
            return false;
        }


        /// <summary>
        /// 下载资源热更清单
        /// </summary>
        /// <returns></returns>
        private async UniTask<bool> DownLoadAssetsManifest()
        {
            string url =
                $"{BundleSettings.Instance.AssetBundleDownLoadUrl}/HotAssets/{CurBundleModuleEnum}/{BundleSettings.Instance.HotManifestName(CurBundleModuleEnum)}";
            try
            {
                using (var req = UnityWebRequest.Get(url))
                {
                    await req.SendWebRequest().WithCancellation(cancellationToken: mCancellationTokenSource.Token);

                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        throw new Exception("服务器资源清单下载失败");
                    }

                    mServerHotAssetsManifest = JsonConvert.DeserializeObject<HotAssetsManifest>(req.downloadHandler.text);

                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("服务器资源清单下载失败");
                return false;
            }
        }

        #region 回调


        /// <summary>
        /// 是否所有都下载成功
        /// </summary>
        private bool allSuccessDownLoad = true;
        
        /// <summary>
        /// 全部下载完成回调
        /// </summary>
        /// <param name="hotAssetsModule"></param>
        private void OnDownLoadAllFinish(HotAssetsModule hotAssetsModule)
        {
            if (allSuccessDownLoad)
            {
                string directory = Path.GetDirectoryName(mLocalHotAssetManifestPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                string json = JsonConvert.SerializeObject(mServerHotAssetsManifest);
                File.WriteAllText(mLocalHotAssetManifestPath,json);
            }
            onDownLoadFinish?.Invoke(hotAssetsModule);
        }

        /// <summary>
        /// 单个文件下载失败
        /// </summary>
        /// <param name="hotFileInfo"></param>
        private void OnDownLoadFailed(HotFileInfo hotFileInfo)
        {
            onDownLoadFailed?.Invoke(hotFileInfo);
            allSuccessDownLoad = false;
        }

        /// <summary>
        /// 单个文件下载成功
        /// </summary>
        /// <param name="hotFileInfo"></param>
        private void OnDownLoadSuccess(HotFileInfo hotFileInfo)
        {
            onDownLoadSuccess?.Invoke(hotFileInfo);
        }

        #endregion
        
        
    }
}
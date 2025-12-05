using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using XLHFrameWork.XAsset.Runtime.Helper;

namespace XLHFrameWork.XAsset.Runtime.BundleHot
{
    public class AssetsDownLoader
    {
        /// <summary>
        /// 最大下载线程个数
        /// </summary>
        public int MAX_THREAD_COUNT = 3;

        /// <summary>
        /// 资源文件下载地址
        /// </summary>
        private string mAssetsDownLoadUrl;

        /// <summary>
        /// 热更文件储存路径
        /// </summary>
        private string mHotAssetsSavePath;

        /// <summary>
        /// 当前热更的资源模块
        /// </summary>
        private HotAssetsModule mCurHotAssetsModule;

        /// <summary>
        /// 文件下载队列
        /// </summary>
        private Queue<HotFileInfo> mDownLoadQueue;

        /// <summary>
        /// 文件下载成功回调
        /// </summary>
        private Action<HotFileInfo> OnDownLoadSuccess;

        /// <summary>
        /// 文件下载失败回调
        /// </summary>
        private Action<HotFileInfo> OnDownLoadFailed;

        /// <summary>
        /// 所有文件下载完成的回调
        /// </summary>
        private Action<HotAssetsModule> OnDownLoadFinish;

        private CancellationToken cancellationToken;

        /// <summary>
        /// 正在下载的文件
        /// </summary>
        private List<HotFileInfo> mAllDownLoadFileList = new List<HotFileInfo>();

        /// <summary>
        /// 资源下载器
        /// </summary>
        /// <param name="assetModule">资源下载模块</param>
        /// <param name="downLoadQueue">下载队列</param>
        /// <param name="downloadUrl">下载地址</param>
        /// <param name="hotAssetsSavePath">保存路径</param>
        /// <param name="onDownLoadSuccess">单个文件下载成功回调</param>
        /// <param name="onDownLoadFailed">单个文件下载失败回调</param>
        /// <param name="onDownLoadFinish">所有文件下载完成糊掉</param>
        public AssetsDownLoader(HotAssetsModule assetModule, Queue<HotFileInfo> downLoadQueue, string downloadUrl,
            string hotAssetsSavePath,
            Action<HotFileInfo> onDownLoadSuccess, Action<HotFileInfo> onDownLoadFailed,
            Action<HotAssetsModule> onDownLoadFinish, CancellationToken cancellationToken)
        {
            this.mCurHotAssetsModule = assetModule;
            this.mDownLoadQueue = downLoadQueue;
            this.mAssetsDownLoadUrl = downloadUrl;
            this.mHotAssetsSavePath = hotAssetsSavePath;
            this.OnDownLoadSuccess = onDownLoadSuccess;
            this.OnDownLoadFailed = onDownLoadFailed;
            this.OnDownLoadFinish = onDownLoadFinish;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// 开始下载
        /// </summary>
        /// <returns></returns>
        public void StartDownLoadQueue()
        {
            List<UniTask> tasks = new List<UniTask>();
            //根据分配的线程数量开启下载
            for (int i = 0; i < MAX_THREAD_COUNT; i++)
            {
                if (mDownLoadQueue.Count > 0)
                {
                    HotFileInfo fileInfo = mDownLoadQueue.Dequeue();
                    mAllDownLoadFileList.Add(fileInfo);
                    tasks.Add(DownLoadAssetBundle(fileInfo));
                }
            }

            UniTask.WhenAll(tasks).Forget();
        }

        /// <summary>
        /// 开始下载下一个
        /// </summary>
        private void DownLoadNextBundle()
        {
            
            if (mAllDownLoadFileList.Count > MAX_THREAD_COUNT)
            {
                Debug.Log("下载数量超出了最大数量... 等待中....");
                return;
            }

            if (mDownLoadQueue.Count > 0)
            {
                StartDownLoadNextBundle();
                if (mAllDownLoadFileList.Count < MAX_THREAD_COUNT)
                {
                    int idleThreadCount = MAX_THREAD_COUNT - mAllDownLoadFileList.Count;
                    for (int i = 0; i < idleThreadCount; i++)
                    {
                        if (mDownLoadQueue.Count > 0)
                        {
                            StartDownLoadNextBundle();
                        }
                    }
                }
            }
            else
            {
                if (mAllDownLoadFileList.Count == 0)
                {
                    Debug.Log("所有文件已经下载完成");
                    OnDownLoadFinish?.Invoke(mCurHotAssetsModule);
                }
            }
        }

        /// <summary>
        /// 开始下载下一个AB包
        /// </summary>
        private void StartDownLoadNextBundle()
        {
            HotFileInfo hotFileInfo = mDownLoadQueue.Dequeue();
            mAllDownLoadFileList.Add(hotFileInfo);
            DownLoadAssetBundle(hotFileInfo).Forget();
        }

        /// <summary>
        /// 开启下载
        /// </summary>
        /// <param name="fileInfo"></param>
        private async UniTask DownLoadAssetBundle(HotFileInfo fileInfo)
        {
            try
            {
                string fileUrl = mAssetsDownLoadUrl + "/" + fileInfo.abName;
                string fileSavePath = mHotAssetsSavePath + "/" + fileInfo.abName;

                long localSize = 0;

                string directory = Path.GetDirectoryName(fileSavePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                if (File.Exists(fileSavePath))
                {
                    FileInfo fileinfo = new FileInfo(fileSavePath);
                    localSize = fileinfo.Length;
                }

                float totalBytes = fileInfo.size * 1024f; // KB → Byte

                var handler = new DownloadHandlerFile(fileSavePath, true);
                handler.removeFileOnAbort = false;

                using (var req = new UnityWebRequest(fileUrl, UnityWebRequest.kHttpVerbGET))
                {
                    req.downloadHandler = handler;
                    if (localSize > 0)
                        req.SetRequestHeader("Range", $"bytes={localSize}-");

                    var operation = req.SendWebRequest();

                    long lastDownloadedBytes = localSize;

                    while (!operation.isDone)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        long currentDownloadedBytes = (long)req.downloadedBytes + localSize;

                        long deltaBytes = currentDownloadedBytes - lastDownloadedBytes;
                        lastDownloadedBytes = currentDownloadedBytes;

                        PrintProgress(fileInfo, currentDownloadedBytes, totalBytes, deltaBytes);

                        await UniTask.Yield();
                    }

                    {
                        long finalDownloadedBytes = (long)req.downloadedBytes + localSize;
                        long deltaBytes = finalDownloadedBytes - lastDownloadedBytes; 

                        PrintProgress(fileInfo, finalDownloadedBytes, totalBytes, deltaBytes);

                        Debug.Log($"下载结束 {fileInfo.abName}，最后一段下载 {deltaBytes} 字节");
                    }

                    if (req.result != UnityWebRequest.Result.Success)
                        throw new Exception($"下载失败: {req.error}");

                    if (MD5.GetMd5FromFile(fileSavePath) != fileInfo.md5)
                        throw new Exception("文件校验失败");

                    OnDownLoadSuccess?.Invoke(fileInfo);
                    mAllDownLoadFileList.Remove(fileInfo);
                    DownLoadNextBundle();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"下载异常: {fileInfo.abName}, {ex}");
                OnDownLoadFailed?.Invoke(fileInfo);
                mAllDownLoadFileList.Remove(fileInfo);
            }
        }


        private void PrintProgress(HotFileInfo fileInfo, long currentDownloadedBytes, float totalBytes, long deltaBytes)
        {
            float progress = currentDownloadedBytes / totalBytes;

            float downloadedMB = currentDownloadedBytes / 1024f / 1024f;
            float totalMB = totalBytes / 1024f / 1024f;

            float speedKB = (deltaBytes / 1024f) / Time.deltaTime;

            /*Debug.Log(
                $"[{fileInfo.abName}] {downloadedMB:F2}MB / {totalMB:F2}MB ({progress:P0})  " +
                $"帧增量 {deltaBytes} B | 速度 {speedKB:F1} KB/s"
            );*/
            
            HotAssetsManager.mAllDownLoadAssetsModuleProgress[mCurHotAssetsModule.CurBundleModuleEnum].currentDownLoadSizeM += (deltaBytes/1024f/1024f);
            HotAssetsManager.mAllDownLoadAssetsModuleProgress[mCurHotAssetsModule.CurBundleModuleEnum]
                .speedDownLoadSizeM = speedKB / 1024f;
            Debug.Log(HotAssetsManager.GetDownLoadProgress(mCurHotAssetsModule.CurBundleModuleEnum));
        }
    }
}
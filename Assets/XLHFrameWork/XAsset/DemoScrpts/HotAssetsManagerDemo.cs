using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using XLHFrameWork.XAsset.Config;
using XLHFrameWork.XAsset.Runtime.BundleHot;

namespace XLHFrameWork.XAsset.DemoScrpts
{
    public class HotAssetsManagerDemo : MonoBehaviour
    {
        private void Start()
        {
            HotAssetsManager hotAssetsManager = new HotAssetsManager();
            hotAssetsManager.StartHotAsset(BundleModuleEnum.cc, (module) =>
            {
                Debug.Log($"{module}开始下载---------->>>>>>>>");
            }, (module) =>
            {
                Debug.Log($"{module} 需要等待...");
            }, (hotfile) =>
            {
                Debug.Log($"{hotfile.abName}下载成功--------->>>>>>>");
            }, (fileinfo) =>
            {
                Debug.Log($"{fileinfo}下载失败-------->>>>>>>");
            }, (assetmoudle) =>
            {
                Debug.Log("全部下载完成------------>>>>>>>>>>>");
            }).Forget();
        }
    }
}
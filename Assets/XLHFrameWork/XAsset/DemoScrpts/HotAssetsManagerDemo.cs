using System;
using System.Resources;
using Cysharp.Threading.Tasks;
using UnityEngine;
using XLHFrameWork.XAsset.Config;
using XLHFrameWork.XAsset.Runtime;
using XLHFrameWork.XAsset.Runtime.BundleHot;
using XLHFrameWork.XAsset.Runtime.BundleLoad;

namespace XLHFrameWork.XAsset.DemoScrpts
{
    public class HotAssetsManagerDemo : MonoBehaviour
    {
        private async void Start()
        {
            /*HotAssetsManager hotAssetsManager = new HotAssetsManager();
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
            }, async (assetmoudle) =>
            {
                Debug.Log("全部下载完成------------>>>>>>>>>>>");
                XLHResourceManager resourceManager = new XLHResourceManager();
                await resourceManager.InitAssetModule(BundleModuleEnum.cc);
                resourceManager.Initlizate();
                await resourceManager.InstantiateAsync("Assets/Test/Cube (1).prefab",null);
            }).Forget();*/

            await XAssetFrameWork.Instance.StartHotAsset(BundleModuleEnum.cc,
                (module) => { Debug.Log($"{module}开始下载---------->>>>>>>>"); },
                (module) => { Debug.Log($"{module} 需要等待..."); },
                (hotfile) => { Debug.Log($"{hotfile.abName}下载成功--------->>>>>>>"); },
                (fileinfo) => { Debug.Log($"{fileinfo}下载失败-------->>>>>>>"); }, async (assetmoudle) =>
                {
                    Debug.Log("全部下载完成------------>>>>>>>>>>>");
                    await XAssetFrameWork.Instance.InitlizateResAsync(BundleModuleEnum.cc);
                    await XAssetFrameWork.Instance.InstantiateAsync("Assets/Test/Image.prefab", null);
                });
        }
    }
}
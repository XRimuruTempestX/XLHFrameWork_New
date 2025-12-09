using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XLHFrameWork.XAsset.Config;
using XLHFrameWork.XAsset.PathConfig;

namespace XLHFrameWork.XAsset.Editor.BundleBuild
{
    [CreateAssetMenu(menuName ="XAsset/模块配置",fileName = "BuildBundleConfigura",order =4)]
    public class BuildBundleConfigura : ScriptableObject
    {
        private static BuildBundleConfigura _instance;

        public static BuildBundleConfigura Instance
        {
            get
            {
                if (_instance==null)
                {
                     _instance = AssetDatabase.LoadAssetAtPath<BuildBundleConfigura>(XAssetPath.BuildBundleConfiguraPath);
                }
                return _instance;
            } 
        }

        /// <summary>
        /// 模块资源配置
        /// </summary>
        [SerializeField]
        public List<BundleModuleData> AssetBundleConfig = new List<BundleModuleData>();

        /// <summary>
        /// 根据模块名称获取模块数据
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        public BundleModuleData GetBundleDataByName(string moduleName)
        {
            foreach (var item in AssetBundleConfig)
            {
                if (string.Equals(item.moduleName,moduleName))
                {
                    return item;
                }
            }
            return null;
        }
        /// <summary>
        /// 通过模块名称移除模块资源
        /// </summary>
        /// <param name="moduleName"></param>
        public void RemoveModuleByName(string moduleName)
        {
            for (int i = 0; i < AssetBundleConfig.Count; i++)
            {
                if (AssetBundleConfig[i].moduleName==moduleName)
                {
                    AssetBundleConfig.Remove(AssetBundleConfig[i]);
                    break;
                }
            }
            Save();
        }
        /// <summary>
        /// 储存新的模块资源
        /// </summary>
        /// <param name="moduleData"></param>
        public void SaveModuleData(BundleModuleData moduleData)
        {
            if (AssetBundleConfig.Contains(moduleData))
            {
                for (int i = 0; i < AssetBundleConfig.Count; i++)
                {
                    if (AssetBundleConfig[i]==moduleData)
                    {
                        AssetBundleConfig[i] = moduleData;
                        break;
                    }
                }
            }
            else
            {
                AssetBundleConfig.Add(moduleData);
            }
     
            Save();
        }
        private void Save()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
#endif
        }

#if UNITY_EDITOR

        public void ClearclickTime()
        {
            foreach (var item in _instance.AssetBundleConfig)
            {
                item.lastClickBtnTime = 0;
            }
        }

        public bool CheckRepeatName(string moduleName)
        {
            return AssetBundleConfig.Any(x => x.moduleName == moduleName);
        }


        public bool showAssetBundleBtn;

        public bool showBuildHotPathBtn;

        //热更补丁版本
        public int hotPatchVersion;

        //app版本号
        public string appVersion;

        //更新信息
        public string notice;
        
        
        /// <summary>
        /// 打包AB包
        /// </summary>
        public void AssetBundleBuild()
        {
            for (int i = 0; i < BuildBundleConfigura.Instance.AssetBundleConfig.Count; i++)
            {
                if (BuildBundleConfigura.Instance.AssetBundleConfig[i].isBuild)
                {
                    BuildBundleCompiler.BuildAssetBundle(BuildBundleConfigura.Instance.AssetBundleConfig[i], BuildType.AssetBundle);
                }
            }
        }

        /// <summary>
        /// 内嵌资源
        /// </summary>
        public void EnbeddedAssetBundle()
        {
            for (int i = 0; i < BuildBundleConfigura.Instance.AssetBundleConfig.Count; i++)
            {
                if (BuildBundleConfigura.Instance.AssetBundleConfig[i].isBuild)
                {
                    BuildBundleCompiler.CopyBundleToStramingAssets(BuildBundleConfigura.Instance.AssetBundleConfig[i]);
                }
            }
        }

        /// <summary>
        /// 构建热更补丁
        /// </summary>
        public void BuildHotPatch()
        {
            for (int i = 0; i < BuildBundleConfigura.Instance.AssetBundleConfig.Count; i++)
            {
                var data = BuildBundleConfigura.Instance.AssetBundleConfig[i];
                if (BuildBundleConfigura.Instance.AssetBundleConfig[i].isBuild)
                {
                    BuildBundleCompiler.BuildAssetBundle(data, BuildType.HotPatch,hotPatchVersion,appVersion,notice);
                }
            }
        }

        /// <summary>
        /// 上传热更文件至服务器
        /// </summary>
        public void UpdateHotPatchToServe()
        {
            //TODO..
        }
#endif
    }
}
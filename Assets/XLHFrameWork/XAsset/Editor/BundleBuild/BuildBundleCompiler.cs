using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;
using XLHFrameWork.XAsset.Config;
using BuildTarget = XLHFrameWork.XAsset.Config.BuildTarget;

namespace XLHFrameWork.XAsset.Editor.BundleBuild
{
    public enum BuildType
    {
        AssetBundle,
        HotPatch,
    }

    public class BuildBundleCompiler
    {
        /// <summary>
        /// 更新公告
        /// </summary>
        private static string mUpdateNotice;

        /// <summary>
        /// 热更补丁版本
        /// </summary>
        private static int mHotPatchVersion;

        /// <summary>
        /// 热更应用版本
        /// </summary>
        private static string mHotAppVersion;

        /// <summary>
        /// 打包类型
        /// </summary>
        private static BuildType mBuildType;

        /// <summary>
        /// 打包模块数据
        /// </summary>
        private static BundleModuleData mBuildModuleData;

        /// <summary>
        /// 打包模块类型
        /// </summary>
        private static BundleModuleEnum mBundleModuleEnum;

        /// <summary>
        /// 所有AssetBundle文件路径列表
        /// </summary>
        private static List<string> mAllBundlePathList = new List<string>();

        /// <summary>
        /// 所有文件夹的Bundle列表
        /// </summary>
        private static Dictionary<string, List<string>> mAllFolderBundleDic = new Dictionary<string, List<string>>();

        /// <summary>
        /// 所有预制体的Bundle字典
        /// </summary>
        private static Dictionary<string, List<string>> mAllPrefabsBundleDic = new Dictionary<string, List<string>>();

        /// <summary>
        /// 要打包的Bundle资产数组
        /// </summary>
        private static List<AssetBundleBuild> mBundleBuildList = new List<AssetBundleBuild>();

        /// <summary>
        /// AssetBundle文件输出路径
        /// </summary>
        private static string mBundleOutPutPath
        {
            get
            {
                return Application.dataPath + "/../AssetBundle/" + mBundleModuleEnum + "/" +
                       BundleSettings.Instance.GetPlatformName() + "/";
            }
        }

        /// <summary>
        /// 热更资源文件输出路径
        /// </summary>
        private static string mHotAssetsOutPutPath
        {
            get
            {
                return Application.dataPath + "/../HotAssets/" + mBundleModuleEnum + "/" + mHotAppVersion + "/" +
                       mHotPatchVersion + "/" + BundleSettings.Instance.GetPlatformName() + "/";
            }
        }

        /// <summary>
        /// 框架Resources路径
        /// </summary>
        private static string mResourcesPath
        {
            get { return Application.dataPath + "/" + BundleSettings.Instance.XAssetRootPath + "/Resources/"; }
        }

        /// <summary>
        /// 打包AssetBundle
        /// </summary>
        /// <param name="moduleData">资源模块配置数据</param>
        /// <param name="buildType">打包类型</param>
        /// <param name="hotPatchVersion">热更补丁版本</param>
        /// <param name="updateNotice">更新公告</param>
        public static void BuildAssetBundle(BundleModuleData moduleData, BuildType buildType = BuildType.AssetBundle,
            int hotPatchVersion = 0, string hotAppVersion = "0.0.0", string updateNotice = "")
        {
            //初始化打包数据
            bool initStatus = Initlization(moduleData, buildType, hotPatchVersion, hotAppVersion, updateNotice);
            if (!initStatus)
            {
                return;
            }

            //打包所有的文件夹
            BuildAllFolder();
            //打包父节点下的所有子文件夹
            BuildRootSubFolder();
            //打包所有预制体
            BuildAllPrefabs();
            //开始调用UnityAPI进行打包AssetBundle
            BuildAllAssetBundle();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="moduleData"></param>
        /// <param name="buildType"></param>
        /// <param name="hotPatchVersion"></param>
        /// <param name="updateNotice"></param>
        private static bool Initlization(BundleModuleData moduleData, BuildType buildType = BuildType.AssetBundle,
            int hotPatchVersion = 0, string hotAppVersion = "0.0.0", string updateNotice = "")
        {
            //清理数据以防下次打包时有数据残留
            mAllBundlePathList.Clear();
            mAllFolderBundleDic.Clear();
            mAllPrefabsBundleDic.Clear();
            mBundleBuildList.Clear();

            mBuildType = buildType;
            mUpdateNotice = updateNotice;
            mBuildModuleData = moduleData;
            mHotPatchVersion = hotPatchVersion;
            mHotAppVersion = hotAppVersion;
            try
            {
                mBundleModuleEnum = (BundleModuleEnum)Enum.Parse(typeof(BundleModuleEnum), moduleData.moduleName);
            }
            catch (Exception)
            {
                Debug.LogError(
                    $"{moduleData.moduleName} Enum Not find! Plase Gennerate Enum : Menu ZMFrame-GeneratorModuleEnum");
                return false;
            }

            if (Directory.Exists(mBundleOutPutPath))
            {
                Directory.Delete(mBundleOutPutPath, true);
            }

            Directory.CreateDirectory(mBundleOutPutPath);
            return true;
        }

        /// <summary>
        /// 打包所有文件夹AssetBundle
        /// </summary>
        private static void BuildAllFolder()
        {
            if (mBuildModuleData.singleFolderPathArr == null || mBuildModuleData.singleFolderPathArr.Count == 0)
            {
                return;
            }

            for (int i = 0; i < mBuildModuleData.singleFolderPathArr.Count; i++)
            {
                //获取文件夹路径
                string path = mBuildModuleData.singleFolderPathArr[i].Replace(@"\", "/");
                DirectoryInfo info = new DirectoryInfo(path);
                FileInfo[] pathArr = info.GetFiles("*", SearchOption.AllDirectories);

                foreach (var fileInfo in pathArr)
                {
                    int removeStartIndex = fileInfo.FullName.LastIndexOf("Assets", StringComparison.Ordinal);
                    string filePath = fileInfo.FullName
                        .Substring(removeStartIndex, fileInfo.FullName.Length - removeStartIndex).Replace("\\", "/");

                    if (filePath.EndsWith(".cs") || filePath.EndsWith(".meta")) continue;

                    mAllBundlePathList.Add(filePath);
                    //获取以模块名+_+AbName的格式的AssetBundle包名
                    string dirPath = Path.GetDirectoryName(filePath);
                    string dirName = Path.GetFileName(dirPath);
                    string bundleName = GenerateBundleName(dirName);
                    if (!mAllFolderBundleDic.ContainsKey(bundleName))
                    {
                        mAllFolderBundleDic.Add(bundleName, new List<string> { filePath });
                    }
                    else
                    {
                        mAllFolderBundleDic[bundleName].Add(filePath);
                    }
                }
            }
        }

        /// <summary>
        /// 打包父文件夹下的所有子文件夹
        /// </summary>
        private static void BuildRootSubFolder()
        {
            //检测父文件夹是否有配置，如果没配置就直接跳过
            if (mBuildModuleData.rootFolderPathArr == null || mBuildModuleData.rootFolderPathArr.Count == 0)
            {
                return;
            }

            for (int i = 0; i < mBuildModuleData.rootFolderPathArr.Count; i++)
            {
                string path = mBuildModuleData.rootFolderPathArr[i] + "/";
                //获取父文夹的所有的子文件夹
                string[] folderArr = Directory.GetDirectories(path);
                foreach (var item in folderArr)
                {
                    path = item.Replace(@"\", "/");
                    int nameIndex = path.LastIndexOf("/", StringComparison.Ordinal) + 1;
                    //获取文件夹同名的AssetBundle名称
                    string bundleName = GenerateBundleName(path.Substring(nameIndex, path.Length - nameIndex));
                    //处理子文件夹资源的代码
                    string[] filePathArr = Directory.GetFiles(path, "*");
                    foreach (var filePath in filePathArr)
                    {
                        //过滤.meta文件
                        if (!filePath.EndsWith(".meta"))
                        {
                            string abFilePath = filePath.Replace(@"\", "/");
                            if (!IsRepeatBundleFile(abFilePath))
                            {
                                mAllBundlePathList.Add(abFilePath);
                                if (!mAllFolderBundleDic.ContainsKey(bundleName))
                                {
                                    mAllFolderBundleDic.Add(bundleName, new List<string> { abFilePath });
                                }
                                else
                                {
                                    mAllFolderBundleDic[bundleName].Add(abFilePath);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 打包指定文件夹下的所有预制体
        /// </summary>
        private static void BuildAllPrefabs()
        {
            if (mBuildModuleData.prefabPathArr == null || mBuildModuleData.prefabPathArr.Count == 0)
            {
                return;
            }

            //获取所有预制体的GUID
            string[] guidArr = AssetDatabase.FindAssets("t:Prefab", mBuildModuleData.prefabPathArr.ToArray());

            for (int i = 0; i < guidArr.Length; i++)
            {
                string filePath = AssetDatabase.GUIDToAssetPath(guidArr[i]);
                //计算AssetBundle名称
                string bundleName = GenerateBundleName(Path.GetFileNameWithoutExtension(filePath));
                //如果该AssetBUndle不存在，就计算打包数据
                if (!mAllBundlePathList.Contains(filePath))
                {
                    //获取预制体所有的依赖项
                    string[] dependsArr = AssetDatabase.GetDependencies(filePath);
                    List<string> dependsList = new List<string>();
                    for (int k = 0; k < dependsArr.Length; k++)
                    {
                        string path = dependsArr[k];
                        //如果不是冗余文件，就归纳进打包
                        if (!IsRepeatBundleFile(path))
                        {
                            mAllBundlePathList.Add(path);
                            dependsList.Add(path);
                        }
                    }

                    if (!mAllPrefabsBundleDic.ContainsKey(bundleName))
                    {
                        mAllPrefabsBundleDic.Add(bundleName, dependsList);
                    }
                    else
                    {
                        Debug.LogError("重复预制体名字，当前模块下有预制体文件重复 Name:" + bundleName);
                    }
                }
            }
        }

        /// <summary>
        /// 是否是重复的Bundle文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static bool IsRepeatBundleFile(string path)
        {
            if (path.EndsWith(".cs"))
            {
                return true;
            }

            foreach (var item in mAllBundlePathList)
            {
                if (string.Equals(item, path) || item.Contains(path) || path.EndsWith(".cs"))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 打包AssetBundle
        /// </summary>
        public static void BuildAllAssetBundle()
        {
            try
            {
                //生成所有要打包的Bundle
                GenerateBundleBuilder();
                //生成一份AssetBundle配置
                // WriteAssetBundleConfig();

                AssetDatabase.Refresh();
              //  BuildAllBundles(mBundleOutPutPath, BundleSettings.Instance.buildTarget, true);
                //调用UnityAPI打包AssetBundle
                /*AssetBundleManifest manifest= BuildPipeline.BuildAssetBundles(mBundleOutPutPath,mBundleBuildList.ToArray(), (UnityEditor.BuildAssetBundleOptions)Enum.Parse(typeof(UnityEditor.BuildAssetBundleOptions),BundleSettings.Instance.buildbundleOptions.ToString())
                    , (UnityEditor.BuildTarget)Enum.Parse(typeof(UnityEditor.BuildTarget), BundleSettings.Instance.buildTarget.ToString()));
                if (manifest==null)
                {
                    Debug.LogError("AssetBundle Build failed!");
                }
                else
                {
                    Debug.Log("AssetBundle Build Successs!:"+ manifest);
                    //DeleteAllBundleManifestFile();
                  //  EncryptAllBundle();
                    if (mBuildType== BuildType.HotPatch)
                    {
                   //     GeneratorHotAssets();
                        EditorUtility.RevealInFinder(mHotAssetsOutPutPath);
                    }
                }*/
            }
            finally
            {
                //   EditorUtility.ClearProgressBar();
            }
        }


        /// <summary>
        /// 生成Bundle打包列表
        /// </summary>
        /// <param name="clear"></param>
        public static void GenerateBundleBuilder(bool clear = false)
        {
            //收集所有要打包的文件夹Bundle
            foreach (var item in mAllFolderBundleDic)
            {
                mBundleBuildList.Add(new AssetBundleBuild()
                {
                    assetBundleName = $"{item.Key.ToLower()}{BundleSettings.Instance.ABSUFFIX}",
                    assetNames = item.Value.ToArray()
                });
            }

            //收集所有要打包的预制体Bundle
            foreach (var item in mAllPrefabsBundleDic)
            {
                mBundleBuildList.Add(new AssetBundleBuild()
                {
                    assetBundleName = $"{item.Key.ToLower()}{BundleSettings.Instance.ABSUFFIX}",
                    assetNames = item.Value.ToArray()
                });
            }

            //收集至Bundle打包配置文件
            /*string bundleConfigPath = Application.dataPath + "/" + BundleSettings.Instance.XAssetRootPath + "/Config/" + mBundleModuleEnum.ToString().ToLower() + "assetbundleconfig.json";
            mBundleBuildList.Add(new AssetBundleBuild(){ assetBundleName = mBundleModuleEnum.ToString().ToLower() + "bundleconfig"+BundleSettings.Instance.ABSUFFIX,assetNames = new []
            {
                $"{bundleConfigPath.Replace(Application.dataPath, "Assets/")}"
            }});*/
        }

        private static string GenerateBundleName(string abName)
        {
            return mBundleModuleEnum.ToString() + "_" + abName;
        }
    }
}
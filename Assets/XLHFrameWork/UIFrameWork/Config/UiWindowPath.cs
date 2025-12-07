using System;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace XLHFrameWork.UIFrameWork.Config
{
    [CreateAssetMenu(fileName = "UWindowPath", menuName = "UI框架/UWindowPath")]
    public class UiWindowPath : ScriptableObject
    {
        [Serializable]
        public class WindowInfo
        {
            [LabelText("窗口名字")]
            public string windowName;
            [LabelText("资源路径")]
            public string windowPath;
        }
        
        [FolderPath,LabelText("窗口预制体存放路径")]
        public List<string> windowPathRootList = new List<string>();
        
        [LabelText("窗口预制体存放路径")]
        public List<WindowInfo> windowInfoList = new List<WindowInfo>();

        /// <summary>
        /// 获取窗口加载路径
        /// </summary>
        /// <param name="windowName"></param>
        /// <returns></returns>
        public string GetWindowPath(string windowName)
        {
            foreach (var item in windowInfoList)
            {
                if (windowName == item.windowName)
                {
                    return item.windowPath;
                }
            }
            return null;
        }

#if UNITY_EDITOR
        [Button("生成窗口键值对", ButtonSizes.Large)]
        public void GeneratorWindowInfo()
        {
            windowInfoList.Clear();
            foreach (var path in windowPathRootList)
            {
                DirectoryInfo directory = Directory.CreateDirectory(path);
                FileInfo[] fileInfos = directory.GetFiles();

                foreach (FileInfo fileInfo in fileInfos)
                {
                    if (fileInfo.Extension == ".meta")
                    {
                        continue;
                    }
                    string filePath = fileInfo.FullName.Replace("\\","/");
                    string windowName = fileInfo.Name.Substring(0, fileInfo.Name.LastIndexOf('.'));
                    windowInfoList.Add(new WindowInfo()
                    {
                        windowName = windowName,
                        windowPath = filePath.Replace(Application.dataPath, "Assets")
                    });
                }
            }  
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
#endif
    }
}

using System.IO;

namespace XLHFrameWork.XAsset.Runtime.Helper
{
    public class FileHelper
    {
        public static void DeleteFolder(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                string[] pathsArr = Directory.GetFiles(folderPath, "*");
                foreach (var path in pathsArr)
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                Directory.Delete(folderPath);
            }
        }
        
        /// <summary>
        /// 写入文件
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="data"></param>
        public static void WriteFile(string filePath,byte[] data)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            FileStream stream= File.Create(filePath);
            stream.Write(data,0,data.Length);
            stream.Dispose();
            stream.Close();
        }
        /// <summary>
        /// 异步写入文件
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="data"></param>
        public static async void WriteFileAsync(string filePath,string data)
        {
            await File.WriteAllTextAsync(filePath, data);
        }
    }
}
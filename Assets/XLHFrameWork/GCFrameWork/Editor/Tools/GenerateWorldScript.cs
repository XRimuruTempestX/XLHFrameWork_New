using System.IO;
using System.Text;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using XLHFramework.GCFrameWork.World;

namespace XLHFramework.GCFrameWorlk.Editor.Tools
{
    public class GenerateWorldScript : OdinEditorWindow
    {
        [TextArea(4, 50)]
        public string UnityTextAreaField = "";

        [EnumPaging,LabelText("生成世界"),OnValueChanged("GenerateScript")]
        public WorldEnum worldEnum = WorldEnum.HallWorld;
        
        [LabelText("脚本生成路径"), FolderPath]
        public string scriptPath = "";
        
        private StringBuilder sb = new StringBuilder();
        
        private WorldConfig worldConfig;

        private GameObject selectObject;

        private void Awake()
        {

            worldConfig = AssetDatabase.LoadAssetAtPath<WorldConfig>("Assets/XLHFrameWork/GCFrameWork/Editor/WorldConfig.asset");
            selectObject = Selection.activeGameObject;
            GenerateScript();
        }

        [MenuItem("GameObject/GC框架/生成世界层脚本",false,0)]
        public static void Generate()
        {
            GenerateWorldScript  window = GetWindow<GenerateWorldScript>();
            window.position = new Rect(470,543,1200,845);

            window.Show();
        }

        [Button("生成脚本", ButtonSizes.Large)]
        public void GenerateButton()
        {
            if (string.IsNullOrEmpty(UnityTextAreaField) || string.IsNullOrEmpty(scriptPath))
            {
                Debug.LogError("路径为空或者代码为空");
                return;
            }

            string filePath = scriptPath + "/" + selectObject.name + ".cs";
            File.WriteAllText(filePath, UnityTextAreaField);
            EditorUtility.DisplayDialog("脚本生成",$"脚本生成成功！\nfilePath:{filePath}","确认");
            AssetDatabase.Refresh();
            Close();
        }

        private void GenerateScript()
        {
            sb.Clear();
            if (worldEnum != WorldEnum.Null)
            {
                sb.AppendLine("using UnityEngine;");
                sb.AppendLine("using XLHFramework.GCFrameWork.World;");
                sb.AppendLine();
                string nameSpace = "";
                foreach (var item in worldConfig.worldConfig)
                {
                    if (item.worldName == worldEnum.ToString())
                    {
                        nameSpace = item.worldNameSpace;
                    }
                }
                sb.AppendLine($"namespace {nameSpace}");
                sb.AppendLine("{");

                sb.AppendLine($"\tpublic class {selectObject.name} : World");
                sb.AppendLine("\t{");

                sb.AppendLine("\t\tpublic override void OnCreate()");
                sb.AppendLine("\t\t{");
                sb.AppendLine();
                sb.AppendLine("\t\t}");
                sb.AppendLine();
                sb.AppendLine("\t\tpublic override void OnDestroy()");
                sb.AppendLine("\t\t{");
                sb.AppendLine();
                sb.AppendLine("\t\t}");
            
                sb.AppendLine("\t}");
            
                sb.AppendLine("}");
                UnityTextAreaField = sb.ToString();
            }
        }
    }
}

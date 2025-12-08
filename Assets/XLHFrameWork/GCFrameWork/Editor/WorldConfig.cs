using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace XLHFramework.GCFrameWorlk.Editor
{
    [CreateAssetMenu(fileName = "WorldConfig", menuName = "GC框架/WorldConfig")]
    public class WorldConfig : ScriptableObject
    {
        [Serializable]
        public class WorldConfigData
        {
            [LabelText("世界名字")]
            public string worldName;

            [LabelText("世界命名空间")]
            public string worldNameSpace;
        }
        
        [LabelText("游戏世界配置")]
        public List<WorldConfigData>  worldConfig = new List<WorldConfigData>();

        [LabelText("世界枚举类生成路径"), FolderPath]
        public string generateSpriptPath = "";

        [LabelText("世界执行顺序脚本路径"),FolderPath]
        public string generateExctionPath = "";

        [Button("生成世界枚举类" , ButtonSizes.Large)]
        public void GenerateWorldEnumScript()
        {
            string fileName = "WorldEnum.cs";

            if (File.Exists(generateSpriptPath + "/" + fileName))
            {
                File.Delete(generateSpriptPath + "/" + fileName);
            }

            string script = GenerateScript();
            File.WriteAllText(generateSpriptPath + "/" + fileName, script);
            
            foreach (WorldConfigData worldConfigData in worldConfig)
            {
                string excutionScript =  GenerateExecutionOrderScript(worldConfigData.worldName);
                string path = generateExctionPath + "/" + worldConfigData.worldName + "ScriptExecutionOrder.cs";
                File.WriteAllText(path, excutionScript);
            }

            AssetDatabase.Refresh();
        }

        private string GenerateScript()
        {
            
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("namespace XLHFramework.GCFrameWork.World");
            sb.AppendLine("{");

            sb.AppendLine("\tpublic enum WorldEnum");
            sb.AppendLine("\t{");

            sb.AppendLine($"\t\tNull,");
            foreach (WorldConfigData worldConfigData in worldConfig)
            {
                sb.AppendLine($"\t\t{worldConfigData.worldName},");
            }
            
            sb.AppendLine("\t}");
            
            sb.AppendLine("}");
            
            return sb.ToString();
        }

        private string GenerateExecutionOrderScript(string worldName)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("using System.Collections;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using XLHFramework.GCFrameWork.Base;");
            
            sb.AppendLine();
            sb.AppendLine("namespace XLHFramework.GCFrameWork.Runtime");
            sb.AppendLine("{");

            sb.AppendLine($"\tpublic class {worldName}ScriptExecutionOrder : IBehaviourExecution");
            sb.AppendLine("\t{");

            sb.AppendLine($"\t\tpublic static string worldName = \"{worldName}\";");
            sb.AppendLine();
            sb.AppendLine("\t\tprivate static readonly string[] LogicBehaviorExecutions = new string[] {};");
            sb.AppendLine();
            sb.AppendLine("\t\tprivate static readonly string[] DataBehaviorExecutions = new string[] {};");
            sb.AppendLine();
            sb.AppendLine("\t\tprivate static readonly string[] MsgBehaviorExecutions = new string[] {};");
            sb.AppendLine();

            sb.AppendLine("\t\tpublic string[] GetDataBehaviourExecution(){ return DataBehaviorExecutions; }");
            sb.AppendLine();
            sb.AppendLine("\t\tpublic string[] GetLogicBehaviourExecution(){ return LogicBehaviorExecutions; }");
            sb.AppendLine();
            sb.AppendLine("\t\tpublic string[] GetMsgBehaviourExecution(){ return MsgBehaviorExecutions; }");

            sb.AppendLine("\t}");
            
            sb.AppendLine("}");
            
            return sb.ToString();
        }
        
    }
}

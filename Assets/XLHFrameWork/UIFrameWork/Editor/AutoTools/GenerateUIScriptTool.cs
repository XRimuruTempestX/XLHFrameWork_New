using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace XLHFramework.UIFrameWork.Editor.AutoTools
{
    public class GenerateUIScriptTool : UnityEditor.Editor
    {
        private static List<Transform> objChild = new List<Transform>();


        [MenuItem("GameObject/UI框架/GenerateUIScriptTool", false, 0)]
        public static void GenerateUIScript()
        {
            GameObject obj = Selection.activeGameObject;
            EditorPrefs.DeleteKey("BeginBind");
            if (obj == null)
            {
                Debug.LogError("obj is null");
                return;
            }

            objChild.Clear();

            objChild = GetChildNode(obj.transform);

            foreach (Transform child in objChild)
            {
                Debug.Log($"name : {child.name}");
            }

            string dataScript = GenerateBindScript(obj.name);
            string windowScript = GenerateWindowBaseScript(obj.name);


            if (UISetting.Instance.uiScriptRootPath == null)
            {
                Debug.LogError("脚本存放路径为空！！！！！");
            }

            if (!Directory.Exists(UISetting.Instance.uiScriptRootPath + "/Bind"))
            {
                Directory.CreateDirectory(UISetting.Instance.uiScriptRootPath + "/Bind");
            }

            if (!Directory.Exists(UISetting.Instance.uiScriptRootPath + "/Window"))
            {
                Directory.CreateDirectory(UISetting.Instance.uiScriptRootPath + "/Window");
            }

            if (File.Exists(UISetting.Instance.uiScriptRootPath + $"/Bind/{obj.name}DataWindow.cs"))
            {
                File.Delete(UISetting.Instance.uiScriptRootPath + $"/Bind/{obj.name}DataWindow.cs");
            }

            if (File.Exists(UISetting.Instance.uiScriptRootPath + $"/Window/{obj.name}.cs"))
            {
                File.Delete(UISetting.Instance.uiScriptRootPath + $"/Window/{obj.name}.cs");
            }

            File.WriteAllText(UISetting.Instance.uiScriptRootPath + $"/Bind/{obj.name}DataWindow.cs", dataScript);
            File.WriteAllText(UISetting.Instance.uiScriptRootPath + $"/Window/{obj.name}.cs", windowScript);
            AssetDatabase.Refresh();
            Debug.Log(dataScript);
            Debug.Log(windowScript);


            //实现自动挂载
            EditorPrefs.SetString("BeginBind", "begin");
        }


        [UnityEditor.Callbacks.DidReloadScripts]
        private static void BindItem()
        {
            string key = EditorPrefs.GetString("BeginBind");
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            EditorPrefs.DeleteKey("BeginBind");
            GameObject obj = Selection.activeGameObject;

            List<Transform> objChild = new List<Transform>();
            objChild = GetChildNode(obj.transform);

            Type type = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == $"{obj.name}DataWindow");
            if (type == null)
            {
                Debug.LogError("未找到类型 " + $"{obj.name}DataWindow");
                return;
            }

            Component cpt = obj.GetComponent(type);

            if (cpt == null)
            {
                cpt = obj.AddComponent(type);
            }

            foreach (var item in objChild)
            {
                string itemName = item.name.Substring(item.name.LastIndexOf(']') + 1);
                string itemType = item.name.Substring(1, item.name.LastIndexOf(']') - 1);
                FieldInfo field = type.GetField(itemName);
                Debug.Log(field);
                if (itemType == "Button")
                {
                    Button btn = item.GetComponent<Button>();
                    if (btn != null)
                    {
                        field.SetValue(cpt, btn);
                    }
                }
                else if (itemType == "Toggle")
                {
                    Toggle toggle = item.GetComponent<Toggle>();
                    if (toggle != null)
                    {
                        field.SetValue(cpt, toggle);
                    }
                }
                else if (itemType == "Slider")
                {
                    Slider slider = item.GetComponent<Slider>();
                    if (slider != null)
                    {
                        field.SetValue(cpt, slider);
                    }
                }
                else if (itemType == "InputField")
                {
                    InputField inputField = item.GetComponent<InputField>();
                    if (inputField != null)
                    {
                        field.SetValue(cpt, inputField);
                    }
                }
                else if (itemType == "TMP_InputField")
                {
                    TMP_InputField inputField = item.GetComponent<TMP_InputField>();
                    if (inputField != null)
                    {
                        field.SetValue(cpt, inputField);
                    }
                }
                else if (itemType == "Dropdown")
                {
                    Dropdown dropdown = item.GetComponent<Dropdown>();
                    if (dropdown != null)
                    {
                        field.SetValue(cpt, dropdown);
                    }
                }
                else if(itemType == "TextMeshProUGUI")
                {
                    TextMeshProUGUI textField = item.GetComponent<TextMeshProUGUI>();
                    if (textField != null)
                    {
                        field.SetValue(cpt, textField);
                    }
                }
            }
        }

        /// <summary>
        /// 筛选节点
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        private static List<Transform> GetChildNode(Transform parent)
        {
            List<Transform> list = new List<Transform>();

            foreach (Transform child in parent)
            {
                if (child.name.Contains("[") && child.name.Contains("]") && !child.name.Contains("#"))
                    list.Add(child);
                list.AddRange(GetChildNode(child));
            }

            return list;
        }

        /// <summary>
        /// 生成绑定代码
        /// </summary>
        /// <returns></returns>
        private static string GenerateBindScript(string windowName)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var item in UISetting.Instance.referenceSpace)
            {
                sb.AppendLine(item);
            }

            sb.AppendLine();

            sb.AppendLine($"namespace {UISetting.Instance.nameSpace}");
            sb.AppendLine("{");

            sb.AppendLine($"\tpublic class {windowName}DataWindow : MonoBehaviour");
            sb.AppendLine("\t{");

            foreach (var item in objChild)
            {
                string itemName = item.name.Substring(item.name.LastIndexOf(']') + 1);
                string itemType = item.name.Substring(1, item.name.LastIndexOf(']') - 1);
                sb.AppendLine($"\t\tpublic {itemType} {itemName};");
            }

            sb.AppendLine();

            sb.AppendLine($"\t\tpublic void InitComponent(WindowBase target)");
            sb.AppendLine("\t\t{");

            sb.AppendLine($"\t\t\t{windowName} mWindow = ({windowName})target;");

            sb.AppendLine();

            foreach (var item in objChild)
            {
                string itemName = item.name.Substring(item.name.LastIndexOf(']') + 1);
                string itemType = item.name.Substring(1, item.name.LastIndexOf(']') - 1);

                if (itemType == "Button")
                {
                    sb.AppendLine($"\t\t\t{itemName}.BindButtonClick(mWindow.Add{itemName}Listener);");
                }
                else if (itemType == "Toggle")
                {
                    sb.AppendLine($"\t\t\t{itemName}.BindToggleChanged(mWindow.Add{itemName}Listener);");
                }
                else if (itemType == "Slider")
                {
                    sb.AppendLine($"\t\t\t{itemName}.BindSliderValueChanged(mWindow.Add{itemName}Listener);");
                }
                else if (itemType == "InputField")
                {
                    sb.AppendLine(
                        $"\t\t\t{itemName}.BindInputFieldValueChanged(mWindow.Add{itemName}ValueChangedListener);");
                    sb.AppendLine($"\t\t\t{itemName}.BindInputFieldEndEdit(mWindow.Add{itemName}EndEditListener);");
                }
                else if (itemType == "TMP_InputField")
                {
                    sb.AppendLine(
                        $"\t\t\t{itemName}.BindTMP_InputFieldValueChanged(mWindow.Add{itemName}ValueChangedListener);");
                    sb.AppendLine($"\t\t\t{itemName}.BindTMP_InputFieldEndEdit(mWindow.Add{itemName}EndEditListener);");
                }
                else if (itemType == "Dropdown")
                {
                    sb.AppendLine($"\t\t\t{itemName}.BindDropdownValueChanged(mWindow.Add{itemName}Listener);");
                }
            }

            sb.AppendLine("\t\t}");

            sb.AppendLine("\t}");


            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// 生成ui窗口逻辑脚本
        /// </summary>
        /// <param name="windowName"></param>
        private static string GenerateWindowBaseScript(string windowName)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var item in UISetting.Instance.referenceSpace)
            {
                sb.AppendLine(item);
            }

            sb.AppendLine();

            sb.AppendLine($"namespace {UISetting.Instance.nameSpace}");
            sb.AppendLine("{");

            sb.AppendLine($"\tpublic class {windowName} : WindowBase");
            sb.AppendLine("\t{");

            sb.AppendLine($"\t\tpublic {windowName}DataWindow dataCompt;");

            sb.AppendLine();

            sb.AppendLine("\t\tpublic override void OnAwake()");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\tbase.OnAwake();");
            sb.AppendLine($"\t\t\tdataCompt = gameObject.GetComponent<{windowName}DataWindow>();");
            sb.AppendLine($"\t\t\tdataCompt.InitComponent(this);");
            sb.AppendLine("\t\t}");
            sb.AppendLine();

            sb.AppendLine("\t\tpublic override async UniTask AnimationBegin()");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\tawait base.AnimationBegin();");
            sb.AppendLine("\t\t}");
            sb.AppendLine();

            sb.AppendLine("\t\tpublic override async UniTask OnShow()");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\tawait base.OnShow();");
            sb.AppendLine("\t\t}");
            sb.AppendLine();

            sb.AppendLine("\t\tpublic override async UniTask AnimationEnd()");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\tawait base.AnimationEnd();");
            sb.AppendLine("\t\t}");
            sb.AppendLine();

            sb.AppendLine("\t\tpublic override async UniTask OnHide()");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\tawait base.OnHide();");
            sb.AppendLine("\t\t}");
            sb.AppendLine();

            sb.AppendLine("\t\tpublic override void OnDestroy()");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\tbase.OnDestroy();");
            sb.AppendLine("\t\t}");

            sb.AppendLine();

            foreach (var item in objChild)
            {
                string itemName = item.name.Substring(item.name.LastIndexOf(']') + 1);
                string itemType = item.name.Substring(1, item.name.LastIndexOf(']') - 1);

                if (itemType == "Button")
                {
                    sb.AppendLine($"\t\tpublic void Add{itemName}Listener()");
                    sb.AppendLine("\t\t{");
                    sb.AppendLine("\t\t\t");
                    sb.AppendLine("\t\t}");
                }
                else if (itemType == "Toggle")
                {
                    sb.AppendLine($"\t\tpublic void Add{itemName}Listener(bool value)");
                    sb.AppendLine("\t\t{");
                    sb.AppendLine("\t\t\t");
                    sb.AppendLine("\t\t}");
                }
                else if (itemType == "Slider")
                {
                    sb.AppendLine($"\t\tpublic void Add{itemName}Listener(float value)");
                    sb.AppendLine("\t\t{");
                    sb.AppendLine("\t\t\t");
                    sb.AppendLine("\t\t}");
                }
                else if (itemType == "InputField")
                {
                    sb.AppendLine($"\t\tpublic void Add{itemName}ValueChangedListener(string value)");
                    sb.AppendLine("\t\t{");
                    sb.AppendLine("\t\t\t");
                    sb.AppendLine("\t\t}");

                    sb.AppendLine($"\t\tpublic void Add{itemName}EndEditListener(string value)");
                    sb.AppendLine("\t\t{");
                    sb.AppendLine("\t\t\t");
                    sb.AppendLine("\t\t}");
                }
                else if (itemType == "TMP_InputField")
                {
                    sb.AppendLine($"\t\tpublic void Add{itemName}ValueChangedListener(string value)");
                    sb.AppendLine("\t\t{");
                    sb.AppendLine("\t\t\t");
                    sb.AppendLine("\t\t}");

                    sb.AppendLine($"\t\tpublic void Add{itemName}EndEditListener(string value)");
                    sb.AppendLine("\t\t{");
                    sb.AppendLine("\t\t\t");
                    sb.AppendLine("\t\t}");
                }
                else if (itemType == "Dropdown")
                {
                    sb.AppendLine($"\t\tpublic void Add{itemName}Listener(int value)");
                    sb.AppendLine("\t\t{");
                    sb.AppendLine("\t\t\t");
                    sb.AppendLine("\t\t}");
                }
            }

            sb.AppendLine("\t}");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
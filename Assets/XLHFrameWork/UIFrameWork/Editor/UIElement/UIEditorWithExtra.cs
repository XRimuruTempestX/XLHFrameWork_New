
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace XLHFramework.UIFrameWork.Editor.UIElement
{

    [CustomEditor(typeof(Button))]
    public class ButtonEditorWithExtraButton : UnityEditor.Editor
    {
        private string textValue = "";

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            GameObject obj = Selection.activeGameObject;
            
            textValue = EditorGUILayout.TextField("组件名字：" , textValue);
            
            if (GUILayout.Button("修改名字"))
            {
                string oldName = textValue;
                string newName = $"[Button]{oldName}Btn";
                obj.name = newName;
            }
        }
    }
    
    
    [CustomEditor(typeof(Image))]
    public class ImageEditorWithExtraImage : UnityEditor.Editor
    {
        private string textValue = "";

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            GameObject obj = Selection.activeGameObject;
            
            textValue = EditorGUILayout.TextField("组件名字：" , textValue);
            
            if (GUILayout.Button("修改名字"))
            {
                string oldName = textValue;
                string newName = $"[Image]{oldName}Img";
                obj.name = newName;
            }
        }
    }
    
    [CustomEditor(typeof(TextMeshProUGUI))]
    public class TextMeshProUGUIEditorWithExtraTextMeshProUGUI : UnityEditor.Editor
    {
        private string textValue = "";

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            GameObject obj = Selection.activeGameObject;
            
            textValue = EditorGUILayout.TextField("组件名字：" , textValue);
            
            if (GUILayout.Button("修改名字"))
            {
                string oldName = textValue;
                string newName = $"[TextMeshProUGUI]{oldName}TextMeshProUGUI";
                obj.name = newName;
            }
        }
    }
    
    [CustomEditor(typeof(Slider))]
    public class SliderEditorWithExtraSlider : UnityEditor.Editor
    {
        private  string textValue = "";
        

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            GameObject obj = Selection.activeGameObject;
            
            textValue = EditorGUILayout.TextField("组件名字：" , textValue);
            
            if (GUILayout.Button("修改名字"))
            {
                string oldName = textValue;
                string newName = $"[Slider]{oldName}Slider";
                obj.name = newName;
            }
        }
    }
    
    [CustomEditor(typeof(Toggle))]
    public class ToggleEditorWithExtraToggle : UnityEditor.Editor
    {
        private string textValue = "";

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            GameObject obj = Selection.activeGameObject;
            
            textValue = EditorGUILayout.TextField("组件名字：" , textValue);
            
            if (GUILayout.Button("修改名字"))
            {
                string oldName = textValue;
                string newName = $"[Toggle]{oldName}Toggle";
                obj.name = newName;
            }
        }
    }
    
    [CustomEditor(typeof(TMP_InputField))]
    public class TMP_InputFieldEditorWithExtraTMP_InputField : UnityEditor.Editor
    {
        private string textValue = "";

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            GameObject obj = Selection.activeGameObject;
            
            textValue = EditorGUILayout.TextField("组件名字：" , textValue);
            
            if (GUILayout.Button("修改名字"))
            {
                string oldName = textValue;
                string newName = $"[TMP_InputField]{oldName}TMP_InputField";
                obj.name = newName;
            }
        }
    }
}

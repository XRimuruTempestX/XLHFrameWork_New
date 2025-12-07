using System;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;

namespace XLHFramework.UIFrameWork.Agent
{
    public static class UIEventExtensions
    {
        
        /// <summary>
        /// 按钮点击
        /// </summary>
        /// <param name="btn"></param>
        /// <param name="action"></param>
        public static void BindButtonClick(this Button btn, UnityAction action)
        {
            if(btn == null)
                return;
            
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
        }

        /// <summary>
        /// toggle 切换
        /// </summary>
        /// <param name="toggle"></param>
        /// <param name="action"></param>
        public static void BindToggleChanged(this Toggle toggle, UnityAction<bool> action)
        {
            if (toggle == null)
                return;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(action);
        }

        /// <summary>
        /// 绑定slider
        /// </summary>
        /// <param name="slider"></param>
        /// <param name="action"></param>
        public static void BindSliderValueChanged(this Slider slider, UnityAction<float> action)
        {
            if (slider == null)
                return;
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(action);
        }

        /// <summary>
        /// 绑定InputField valuechanged
        /// </summary>
        /// <param name="inputField"></param>
        /// <param name="action"></param>
        public static void BindInputFieldValueChanged(this InputField inputField, UnityAction<string> action)
        {
            if (inputField == null)
                return;
            inputField.onValueChanged.RemoveAllListeners();
            inputField.onValueChanged.AddListener(action);
        }

        /// <summary>
        /// 绑定InputField EndEdit
        /// </summary>
        /// <param name="inputField"></param>
        /// <param name="action"></param>
        public static void BindInputFieldEndEdit(this InputField inputField, UnityAction<string> action)
        {
            if (inputField == null)
                return;
            inputField.onEndEdit.RemoveAllListeners();
            inputField.onEndEdit.AddListener(action);
        }

        /// <summary>
        /// 绑定TMP_InputField valueChanged
        /// </summary>
        /// <param name="inputField"></param>
        /// <param name="action"></param>
        public static void BindTMP_InputFieldValueChanged(this TMP_InputField inputField, UnityAction<string> action)
        {
            if (inputField == null)
                return;
            inputField.onValueChanged.RemoveAllListeners();
            inputField.onValueChanged.AddListener(action);
        }

        /// <summary>
        /// 绑定TMP_InputField EndEdit
        /// </summary>
        /// <param name="inputField"></param>
        /// <param name="action"></param>
        public static void BindTMP_InputFieldEndEdit(this TMP_InputField inputField, UnityAction<string> action)
        {
            if (inputField == null)
                return;
            inputField.onEndEdit.RemoveAllListeners();
            inputField.onEndEdit.AddListener(action);
        }

        /// <summary>
        /// 绑定DropdownValueChanged
        /// </summary>
        /// <param name="dropdown"></param>
        /// <param name="action"></param>
        public static void BindDropdownValueChanged(this Dropdown dropdown, UnityAction<int> action)
        {
            if(dropdown == null)
                return;
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(action);
        }
        
    }
}
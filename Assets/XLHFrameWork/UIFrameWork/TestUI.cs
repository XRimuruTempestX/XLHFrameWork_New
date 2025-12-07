using System;
using Cysharp.Threading.Tasks;
using UIFrameworlk;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using XLHFramework.UIFrameWork.Agent;
using XLHFramework.UIFrameWork.Runtime.Base;
using XLHFramework.UIFrameWork.Runtime.Core;

namespace XLHFramework.UIFrameWork
{
    public class TestUI : MonoBehaviour
    {
        private void Start()
        {
            //UIManager.Instance.Initialize();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                //UIManager.Instance.PopUpWindow<TestWindow>().Forget();
                UIManager.Instance.PushAndPopStackWindow<TestWindow>().Forget();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                UIManager.Instance.DestroyWinodw<TestWindow>().Forget();
            }
        }
    }
    
}

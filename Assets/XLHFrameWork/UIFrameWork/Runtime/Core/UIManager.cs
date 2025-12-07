using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using XLHFrameWork.UIFrameWork.Config;
using XLHFramework.UIFrameWork.Runtime.Base;
using XLHFrameWork.XAsset.PathConfig;
using XLHFrameWork.XAsset.Runtime;
using XLHFrameWork.XAsset.Runtime.BundleLoad;

namespace XLHFramework.UIFrameWork.Runtime.Core
{
    public class UIManager
    {
        private static UIManager instance;

        public static UIManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new UIManager();
                return instance;
            }
        }

        private Camera uiCamera;

        public Camera UICamera => uiCamera;

        /// <summary>
        /// 所有UI存放的节点
        /// </summary>
        private Transform mUIRoot;

        /// <summary>
        /// 所有加载出来的UI窗口  无论时显示还是隐藏
        /// </summary>
        private Dictionary<string, WindowBase> mAllWindowDic = new Dictionary<string, WindowBase>();

        /// <summary>
        /// 所有正在显示中的UI窗口
        /// </summary>
        private Dictionary<string, WindowBase> mVisibleWindowDic = new Dictionary<string, WindowBase>();

        /// <summary>
        /// 存放堆栈UI窗口
        /// </summary>
        private List<WindowBase> mWindowStack = new List<WindowBase>();

        private bool mStartPopStackWndStatus = false;

        private UiWindowPath windowPathConfig;


        private GameObject uiCameraGameObject;
        private GameObject uiRootGameObject;
        private GameObject eventSystemGameObject;

        /// <summary>
        /// 初始化框架
        /// </summary>
        public async UniTask Initialize()
        {
            uiCameraGameObject = await XAssetFrameWork.Instance.InstantiateAsync(XAssetPath.UICamaeraPath);
            uiCamera = uiCameraGameObject.GetComponent<Camera>();
            GameObject.DontDestroyOnLoad(uiCameraGameObject);

            eventSystemGameObject = await XAssetFrameWork.Instance.InstantiateAsync(XAssetPath.UIEventSystemPath);
            GameObject.DontDestroyOnLoad(eventSystemGameObject);

            if (uiRootGameObject == null)
            {
                uiRootGameObject = new GameObject("UIRoot");
                mUIRoot = uiRootGameObject.transform;
                GameObject.DontDestroyOnLoad(uiRootGameObject);
            }

            windowPathConfig = await XAssetFrameWork.Instance.LoadAssetAsync<UiWindowPath>(XAssetPath.UIWindowPath);

        }

        #region 显示和隐藏窗口

        private async UniTask<WindowBase> InitializeWindow(WindowBase windowBase, string windowName)
        {
            GameObject windowObj = await LoadWindow(windowName);

            if (windowObj != null)
            {
                windowBase.gameObject = windowObj;
                windowBase.transform = windowObj.transform;
                windowObj.transform.SetParent(mUIRoot);
                windowBase.Name = windowName;
                windowBase.canvas = windowObj.GetComponent<Canvas>();
                windowBase.canvas.worldCamera = uiCamera;
                windowBase.transform.SetAsLastSibling();
                windowBase.OnAwake();
                windowBase.SetVisible(true);
                await windowBase.OnShow();
                RectTransform rect = windowObj.GetComponent<RectTransform>();
                rect.anchorMax = Vector2.one;
                rect.anchorMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.offsetMin = Vector2.zero;

                if (mAllWindowDic.ContainsKey(windowName))
                {
                    Debug.LogError($"字典里存在这个窗口 ： {windowName}");
                    return null;
                }

                mAllWindowDic.Add(windowName, windowBase);
                mVisibleWindowDic.Add(windowName, windowBase);

                //设置遮罩
                SetWidowMaskVisible();
                return windowBase;
            }

            Debug.LogError("没有加载到对应的窗口 ：" + windowName);
            return null;
        }

        /// <summary>
        /// 弹出窗口
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async UniTask<T> PopUpWindow<T>() where T : WindowBase, new()
        {
            Type type = typeof(T);
            string windowName = type.Name;
            Debug.Log($"开始弹出窗口： {windowName}");
            WindowBase window = GetWindow(windowName);
            if (window != null)
            {
                return await ShowWindow(windowName) as T;
            }

            T t = new T();
            Debug.Log($"弹出新窗口：{t.Name}");
            return await InitializeWindow(t, windowName) as T;
        }

        private async UniTask<WindowBase> PopUpWindow(WindowBase window)
        {
            System.Type type = window.GetType();
            string wndName = type.Name;
            WindowBase wnd = GetWindow(wndName);
            if (wnd != null)
            {
                return await ShowWindow(wndName);
            }

            return await InitializeWindow(window, wndName);
        }


        private async UniTask<WindowBase> ShowWindow(string windowName)
        {
            //存在在字典中
            WindowBase window = null;
            if (mAllWindowDic.ContainsKey(windowName))
            {
                window = mAllWindowDic[windowName];
                if (window.gameObject != null && window.Visible == false)
                {
                    mVisibleWindowDic.Add(windowName, window);
                    window.transform.SetAsLastSibling();
                    window.SetVisible(true);
                    SetWidowMaskVisible();
                    await window.OnShow();
                }
                else if (window.gameObject != null && window.Visible)
                {
                    //证明正在显示  则调用OnShow刷新数据
                    await window.OnShow();
                }
            }
            else
            {
                Debug.LogError($"该窗口一次都没有弹出 不存在字典中  ： {windowName}");
            }

            return null;
        }

        /// <summary>
        /// 查找是否已经加载过这个窗口
        /// </summary>
        private WindowBase GetWindow(string windowName)
        {
            if (mAllWindowDic.TryGetValue(windowName, out WindowBase window))
            {
                return window;
            }
            return null;
        }

        public T GetWindow<T>() where T : WindowBase
        {
            System.Type type = typeof(T);
            foreach (var item in mVisibleWindowDic.Values)
            {
                if (item.Name == type.Name)
                {
                    return (T)item;
                }
            }
            Debug.LogError("该窗口没有获取到：" + type.Name);
            return null;
        }
        
        public async UniTask HideWindow(string wndName)
        {
            WindowBase window = GetWindow(wndName);
            await HideWindow(window);
        }
        public async UniTask HideWindow<T>() where T : WindowBase
        {
            await HideWindow(typeof(T).Name);
        }
        private async UniTask HideWindow(WindowBase window)
        {
            if (window != null && window.Visible)
            {
                mVisibleWindowDic.Remove(window.Name);
                await window.OnHide();
                window.SetVisible(false);//隐藏弹窗物体
                SetWidowMaskVisible();
            }
            //在出栈的情况下，上一个界面隐藏时，自动打开栈种的下一个界面
            await PopNextStackWindow(window);
        }
        
        
        private async UniTask DestroyWindow(string wndName, bool isClear = true)
        {
            WindowBase window = GetWindow(wndName);
            await DestoryWindow(window, isClear);
        }
        public async UniTask DestroyWinodw<T>(bool isClear = false) where T : WindowBase
        {
            await DestroyWindow(typeof(T).Name, isClear);
        }
        private async UniTask DestoryWindow(WindowBase window, bool isClear = true)
        {
            if (window != null)
            {
                if (mAllWindowDic.ContainsKey(window.Name))
                {
                    mAllWindowDic.Remove(window.Name);
                    mVisibleWindowDic.Remove(window.Name);
                }
                if (window.Visible)
                   await window.OnHide();
                window.SetVisible(false);
                SetWidowMaskVisible();

                window.OnDestroy();
                GameObjectDestoryWindow(window.gameObject, isClear);
                //在出栈的情况下，上一个界面销毁时，自动打开栈种的下一个界面
                await PopNextStackWindow(window);
                window = null;
            }
        }
        
        
        /*
        public void DestroyAllWindow(List<string> filterlist = null)
        {
            for (int i = mAllWindowList.Count - 1; i >= 0; i--)
            {
                WindowBase window = mAllWindowList[i];
                if (window == null || (filterlist != null && filterlist.Contains(window.Name)))
                {
                    continue;
                }
                DestroyWindow(window.Name, true);
            }
            Resources.UnloadUnusedAssets();
        }
        */
        

        private void SetWidowMaskVisible()
        {
            if (!UISetting.Instance.SINGMAXSK_SYSTEM)
                return;
            WindowBase maxOrderWindowBase = null; // 最大渲染层级的窗口
            int maxOrder = 0;
            int maxIndex = 0;

            foreach (var window in mVisibleWindowDic.Values)
            {
                window.SetMaskVisible(false);
                if (maxOrderWindowBase == null)
                {
                    maxOrderWindowBase = window;
                    maxOrder = window.canvas.sortingOrder;
                    maxIndex = window.transform.GetSiblingIndex();
                }
                else
                {
                    if (maxOrder < window.canvas.sortingOrder)
                    {
                        maxOrderWindowBase = window;
                        maxOrder = window.canvas.sortingOrder;
                    }
                    else if (maxOrder == window.canvas.sortingOrder && maxIndex < window.transform.GetSiblingIndex())
                    {
                        maxOrderWindowBase = window;
                        maxIndex = window.transform.GetSiblingIndex();
                    }
                }
            }

            if (maxOrderWindowBase != null)
            {
                maxOrderWindowBase.SetMaskVisible(true);
            }
        }

        #endregion

        #region 堆栈系统

        /// <summary>
        /// 开始弹出
        /// </summary>
        public async UniTask StartPopFirstStackWindow()
        {
            if (mStartPopStackWndStatus) return;
            mStartPopStackWndStatus = true; //已经开始进行堆栈弹出的流程，
            await PopStackWindow();
        }

        /// <summary>
        /// 进栈一个界面
        /// </summary>
        /// <param name="popCallBack">压栈弹窗弹出回调</param>
        /// <param name="single">是否只允许存在一个</param>
        /// <param name="pushToStackTop">是否压到栈顶(优先弹出)</param>
        /// <typeparam name="T">准备压栈的弹窗</typeparam>
        public async UniTask PushWindowToStack<T>(Action<WindowBase> popCallBack = null, bool single = false,
            bool pushToStackTop = false) where T : WindowBase, new()
        {
            string winName = typeof(T).Name;
            if (single)
            {
                //压栈去重
                foreach (var item in mWindowStack)
                {
                    if (item.Name.Equals(winName)) return;
                }

                //压栈去显
                WindowBase win = GetWindow<T>();
                if (win != null)
                {
                    Debug.Log($"{winName} 弹窗已显示，single模式不处理压栈");
                    await win.OnShow();
                    return;
                }
            }

            Debug.Log($"Stack Window Push :{winName}");

            T wndBase = new T { PopStackListener = popCallBack, Name = winName };

            if (pushToStackTop)
            {
                mWindowStack.Insert(0, wndBase);
                return;
            }

            mWindowStack.Add(wndBase);
        }

        /// <summary>
        /// 压入并直接弹出首个
        /// </summary>
        /// <param name="popCallBack">压栈弹窗弹出回调</param>
        /// <param name="single">是否只允许存在一个</param>
        /// <param name="pushToStackTop">是否压到栈顶</param>
        /// <typeparam name="T"></typeparam>
        public async UniTask PushAndPopStackWindow<T>(Action<WindowBase> popCallBack = null, bool single = false,
            bool pushToStackTop = false) where T : WindowBase, new()
        {
            await PushWindowToStack<T>(popCallBack, single, pushToStackTop);
            await StartPopFirstStackWindow();
        }

        /// <summary>
        /// 弹出堆栈中的下一个窗口
        /// </summary>
        /// <param name="windowBase"></param>
        private async UniTask PopNextStackWindow(WindowBase windowBase)
        {
            if (windowBase != null && mStartPopStackWndStatus && windowBase.PopStack)
            {
                windowBase.PopStack = false;
                await PopStackWindow();
            }
        }

        /// <summary>
        /// 弹出堆栈弹窗
        /// </summary>
        /// <returns></returns>
        private async UniTask<bool> PopStackWindow()
        {
            if (mWindowStack.Count > 0)
            {
                WindowBase window = mWindowStack[0];
                mWindowStack.RemoveAt(0);
                WindowBase popWindow = await PopUpWindow(window);
                popWindow.PopStackListener = window.PopStackListener;
                popWindow.PopStack = true;
                popWindow.PopStackListener?.Invoke(popWindow);
                popWindow.PopStackListener = null;
                return true;
            }
            else
            {
                mStartPopStackWndStatus = false;
                return false;
            }
        }

        #endregion

        #region 加载窗口预制体

        private async UniTask<GameObject> LoadWindow(string windowName)
        {
            string windowPath = windowPathConfig.GetWindowPath(windowName);
            if (windowPath != null)
            {
                GameObject obj = await XAssetFrameWork.Instance.InstantiateAsync(windowName);
                obj.name = windowName;
                return obj;
            }
            return null;
        }
        
        private void GameObjectDestoryWindow(GameObject windowObj, bool isClear = true)
        {
            //GameObject.Destroy(windowObj);
            XAssetFrameWork.Instance.ReleaseGameObject(windowObj);
        }

        #endregion
    }
}
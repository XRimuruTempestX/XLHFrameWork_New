using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XLHFramework.UIFrameWork.Runtime.Base
{
    public abstract class WindowBehaviour
    {
        public GameObject gameObject;

        public Transform transform;

        public Canvas canvas;

        public string Name;

        public bool Visible;

        public bool Update;

        public bool PopStack;
        
        public Action<WindowBase> PopStackListener { get; set; } //堆栈弹出回调


        public virtual void OnAwake(){}

        public abstract UniTask OnShow();

        public abstract UniTask OnHide();

        public virtual void OnUpdate(){}
        
        public virtual void OnDestroy(){}
        
        public virtual void SetVisible(bool visible){}
    }
}

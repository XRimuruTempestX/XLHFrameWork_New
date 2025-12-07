using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XLHFramework.UIFrameWork.Runtime.Base
{
    public class WindowBase : WindowBehaviour
    {
        private CanvasGroup mUIMaskCanvasGroup;
        
        private CanvasGroup mWindowCanvasGroup;

        protected Transform mUIContent;
        
        private void InitializeBaseComponent()
        {
            mWindowCanvasGroup = transform.GetComponent<CanvasGroup>();
            mUIMaskCanvasGroup = transform.Find("UIMask").GetComponent<CanvasGroup>();
            mUIContent = transform.Find("UIContent");
        }

        public override void OnAwake()
        {
            base.OnAwake();
            InitializeBaseComponent();
        }


        public virtual async UniTask AnimationBegin()
        {
            
        }
        
        public override async UniTask OnShow()
        {
            await AnimationBegin(); // 等待ui动画播放完成
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
        }

        public virtual async UniTask AnimationEnd()
        {
            
        }
        public override async UniTask OnHide()
        {
            await AnimationEnd(); // 等待UI动画关闭完成
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        public override void SetVisible(bool visible)
        {
            base.SetVisible(visible);
            Visible = visible;
            mWindowCanvasGroup.alpha = visible ? 1 : 0;
            mWindowCanvasGroup.interactable = visible;
            mWindowCanvasGroup.blocksRaycasts = visible;
        }

        public void SetMaskVisible(bool visible)
        {
            if (!UISetting.Instance.SINGMAXSK_SYSTEM) return;
            mUIMaskCanvasGroup.alpha = visible ? 1 : 0;
            mUIMaskCanvasGroup.blocksRaycasts = visible;
        }
    }
}
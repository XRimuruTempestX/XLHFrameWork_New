using XLHFramework.UIFrameWork.Agent; 
using XLHFramework.UIFrameWork.Runtime.Base;
using Cysharp.Threading.Tasks; 
using UnityEngine.UI;
using UnityEngine; 
using TMPro;

namespace UIFrameworlk
{
	public class TestWindow : WindowBase
	{
		public TestWindowDataWindow dataCompt;

		public override void OnAwake()
		{
			base.OnAwake();
			dataCompt = gameObject.GetComponent<TestWindowDataWindow>();
			dataCompt.InitComponent(this);
		}

		public override async UniTask AnimationBegin()
		{
			await base.AnimationBegin();
		}

		public override async UniTask OnShow()
		{
			await base.OnShow();
		}

		public override async UniTask AnimationEnd()
		{
			await base.AnimationEnd();
		}

		public override async UniTask OnHide()
		{
			await base.OnHide();
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
		}

		public void AddccccBtnListener()
		{
			
		}
		public void AddaaSliderListener(float value)
		{
			
		}
	}
}

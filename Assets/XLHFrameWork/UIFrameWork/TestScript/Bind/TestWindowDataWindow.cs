using XLHFramework.UIFrameWork.Agent; 
using XLHFramework.UIFrameWork.Runtime.Base;
using Cysharp.Threading.Tasks; 
using UnityEngine.UI;
using UnityEngine; 
using TMPro;

namespace UIFrameworlk
{
	public class TestWindowDataWindow : MonoBehaviour
	{
		public Button ccccBtn;
		public TextMeshProUGUI ccTextMeshProUGUI;
		public Slider aaSlider;

		public void InitComponent(WindowBase target)
		{
			TestWindow mWindow = (TestWindow)target;

			ccccBtn.BindButtonClick(mWindow.AddccccBtnListener);
			aaSlider.BindSliderValueChanged(mWindow.AddaaSliderListener);
		}
	}
}

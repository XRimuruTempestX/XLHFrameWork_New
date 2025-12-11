using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using XLHFrameWork.UniTaskTimer;

public class TimerDemo : MonoBehaviour
{
    private TimerHandle countdown;
    TimerHandle interval;

    void Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();
        //countdown = UniTaskTimer.Countdown(15f, false, p => Debug.Log("Countdown " + Mathf.RoundToInt(p * 100) + "%"), () => Debug.Log("Countdown Done"), () => Debug.Log("Countdown Canceled"), ct);
        interval = UniTaskTimer.Interval(1f, 10, false, i => Debug.Log("Tick " + i), () => Debug.Log("Interval Done"), () => Debug.Log("Interval Canceled"), ct);
        UniTask.Void(async () =>
        {
            // 等待 3 秒后暂停，再等待 2 秒后恢复
            await UniTask.Delay(TimeSpan.FromSeconds(3), false, PlayerLoopTiming.Update, ct);
            interval.Pause();
            Debug.Log("Interval Paused");
            await UniTask.Delay(TimeSpan.FromSeconds(2), false, PlayerLoopTiming.Update, ct);
            interval.Resume();
            Debug.Log("Interval Resumed");
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

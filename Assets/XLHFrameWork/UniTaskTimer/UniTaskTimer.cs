using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XLHFrameWork.UniTaskTimer
{

    public enum TimerState
    {
        Pending,
        Running,
        Paused,
        Completed,
        Canceled,
    }

    public sealed class TimerHandle
    {
        /// <summary>唯一ID</summary>
        public Guid Id { get; }
        /// <summary>当前计时器状态</summary>
        public TimerState State { get; private set; }
        /// <summary>倒计时目标时长（秒），仅倒计时模式使用</summary>
        public float Duration { get; }
        /// <summary>累计时长（秒），用于倒计时完成与进度计算</summary>
        public float Elapsed { get; private set; }
        /// <summary>是否使用不受 timeScale 影响的时间（unscaledDeltaTime）</summary>
        public bool Unscaled { get; }
        /// <summary>是否为间隔计时模式</summary>
        public bool Repeating { get; }
        /// <summary>间隔触发的间隔时长（秒），仅间隔模式使用</summary>
        public float Interval { get; }
        /// <summary>是否开启跳帧补偿（在卡顿帧补齐遗漏触发）</summary>
        public bool CatchUp { get; }
        /// <summary>每帧最多补偿次数上限</summary>
        public int MaxCatchUps { get; }
        /// <summary>倍速系数（对 dt 生效），默认 1</summary>
        public float Speed { get; }
        /// <summary>帧时序（用于 Yield/Delay 的循环），默认 Update</summary>
        public PlayerLoopTiming Timing { get; }
        /// <summary>每帧补偿处理的最大预算（毫秒），0 表示不限</summary>
        public int MaxCatchUpBudgetMs { get; }
        /// <summary>因预算限制丢弃的补偿次数</summary>
        public int DroppedCatchUps { get; private set; }
        /// <summary>累计漂移秒数（被丢弃的累积量）</summary>
        public float DriftSeconds { get; private set; }
        /// <summary>取消令牌（供外部取消计时器）</summary>
        public CancellationToken Token => _cts.Token;
        /// <summary>完成任务（不返回结果，用于简单等待）</summary>
        public UniTask Completion => _tcs.Task;
        /// <summary>间隔模式的最终 Tick 数（任务完成时返回）</summary>
        public UniTask<int> CompletionTicks => _tcsTicks != null ? _tcsTicks.Task : default;
        /// <summary>倒计时的实际耗时（任务完成时返回）</summary>
        public UniTask<float> CompletionElapsed => _tcsElapsed != null ? _tcsElapsed.Task : default;
        /// <summary>倒计时进度事件（0~1）</summary>
        public event Action<float> OnProgress;
        /// <summary>间隔计时的单次 Tick 事件</summary>
        public event Action<int> OnTick;
        /// <summary>补偿模式下的批量 Tick 事件（本帧内补偿次数）</summary>
        public event Action<int> OnTickBatch;
        /// <summary>计时器完成事件</summary>
        public event Action OnCompleted;
        /// <summary>计时器取消事件</summary>
        public event Action OnCanceled;

        /// <summary>内部取消源（与外部令牌链接）</summary>
        readonly CancellationTokenSource _cts;
        /// <summary>完成任务源（无结果）</summary>
        readonly UniTaskCompletionSource _tcs;
        /// <summary>间隔计时的结果任务源（返回最终 Tick 数）</summary>
        readonly UniTaskCompletionSource<int> _tcsTicks;
        /// <summary>倒计时的结果任务源（返回实际耗时）</summary>
        readonly UniTaskCompletionSource<float> _tcsElapsed;

        /// <summary>
        /// 倒计时构造：仅使用 Duration 推进，不做补偿。
        /// </summary>
        internal TimerHandle(float duration, bool unscaled, CancellationToken externalToken)
        {
            Id = Guid.NewGuid();
            State = TimerState.Pending;
            Duration = duration;
            Unscaled = unscaled;
            Repeating = false;
            Interval = 0f;
            CatchUp = false;
            MaxCatchUps = 0;
            Speed = 1f;
            Timing = PlayerLoopTiming.Update;
            MaxCatchUpBudgetMs = 0;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            _tcs = new UniTaskCompletionSource();
            _tcsElapsed = new UniTaskCompletionSource<float>();
        }

        /// <summary>
        /// 间隔构造：支持 CatchUp（跳帧补偿）与每帧补偿上限。
        /// </summary>
        internal TimerHandle(float interval, int? count, bool unscaled, bool catchUp, int maxCatchUps, CancellationToken externalToken)
        {
            Id = Guid.NewGuid();
            State = TimerState.Pending;
            Duration = 0f;
            Unscaled = unscaled;
            Repeating = true;
            Interval = interval;
            CatchUp = catchUp;
            MaxCatchUps = Mathf.Max(0, maxCatchUps);
            Speed = 1f;
            Timing = PlayerLoopTiming.Update;
            MaxCatchUpBudgetMs = 0;
            _repeatCount = count;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            _tcs = new UniTaskCompletionSource();
            _tcsTicks = new UniTaskCompletionSource<int>();
        }

        /// <summary>
        /// 间隔高级构造：叠加倍速/帧时序/预算限流。
        /// </summary>
        internal TimerHandle(float interval, int? count, bool unscaled, bool catchUp, int maxCatchUps, float speed, PlayerLoopTiming timing, int maxCatchUpBudgetMs, CancellationToken externalToken)
        {
            Id = Guid.NewGuid();
            State = TimerState.Pending;
            Duration = 0f;
            Unscaled = unscaled;
            Repeating = true;
            Interval = interval;
            CatchUp = catchUp;
            MaxCatchUps = Mathf.Max(0, maxCatchUps);
            Speed = Mathf.Max(0.0001f, speed);
            Timing = timing;
            MaxCatchUpBudgetMs = Mathf.Max(0, maxCatchUpBudgetMs);
            _repeatCount = count;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            _tcs = new UniTaskCompletionSource();
            _tcsTicks = new UniTaskCompletionSource<int>();
        }
        
        /// <summary>目标 Tick 次数（空表示无限）</summary>
        int? _repeatCount;
        /// <summary>当前已触发的 Tick 次数</summary>
        int _ticks;
        /// <summary>补偿模式下的时间累加器（秒）</summary>
        float _accumulator;


        /// <summary>
        /// 倒计时
        /// </summary>
        internal async UniTask RunCountdown()
        {
            State = TimerState.Running;
            Elapsed = 0f;
            try
            {
                while (Elapsed < Duration)
                {
                    if(_cts.IsCancellationRequested)
                        break;
                    if (State == TimerState.Paused)
                    {
                        await UniTask.Yield(Timing, _cts.Token);
                        continue;
                    }
                    
                    var dt = Unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                    dt *= Speed;
                    Elapsed += dt;
                    var p = Mathf.Clamp01(Elapsed / Duration);
                    OnProgress?.Invoke(p);
                    await UniTask.Yield(Timing, _cts.Token);
                }

                if (!_cts.IsCancellationRequested)
                {
                    _tcsElapsed?.TrySetResult(Elapsed);
                    TryComplete();
                }
            }
            catch (OperationCanceledException)
            {
                TryCancel();
            }
            catch (Exception e)
            {
                throw;
            }
        }
        
        /// <summary>
        /// 间隔主循环
        /// CatchUp=false 使用 Delay 固定节拍；
        /// CatchUp=true 使用累加器补偿，并提供每帧补偿上限与预算限流。
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        internal async UniTask RunInterval()
        {
            State = TimerState.Running;
            _ticks = 0;
            try
            {
                if (CatchUp)
                {
                    _accumulator = 0f;
                    var start = Time.realtimeSinceStartup;
                    while (!_cts.IsCancellationRequested)
                    {
                        if (State == TimerState.Paused)
                        {
                            await UniTask.Yield(Timing, _cts.Token);
                            continue;
                        }
                        
                        var dt = Unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                        dt *= Speed;
                        _accumulator += dt;
                        var catchups = 0;
                        //跳帧补偿 : 每帧最多补偿MaxCatchUps 次，避免多次补偿
                        while (_accumulator >= Interval && catchups < MaxCatchUps)
                        {
                            _accumulator -= Interval;
                            _ticks++;
                            catchups++;
                            OnTick?.Invoke(_ticks);
                            if(_repeatCount.HasValue && _ticks >= _repeatCount.Value)
                                goto FINISH;
                        }
                        
                        // 批量回调：一次补偿多次 Tick 时聚合通知
                        if (catchups > 1) OnTickBatch?.Invoke(catchups);
                        // 预算限流：超过本帧预算则丢弃剩余补偿并累计漂移
                        if (MaxCatchUpBudgetMs > 0)
                        {
                            var elapsedMs = (Time.realtimeSinceStartup - start) * 1000f;
                            if (elapsedMs >= MaxCatchUpBudgetMs && _accumulator >= Interval)
                            {
                                DroppedCatchUps += Mathf.FloorToInt(_accumulator / Interval);
                                DriftSeconds += _accumulator;
                                _accumulator = 0f;
                            }
                        }

                        await UniTask.Yield(Timing, _cts.Token);
                    }
                }
                else
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        if (State == TimerState.Paused)
                        {
                            await UniTask.Yield(Timing, _cts.Token);
                            continue;
                        }

                        // 固定节拍：按 Interval/Speed 计算等待时间
                        var delay = TimeSpan.FromSeconds(Mathf.Max(0.0001f, Interval / Mathf.Max(0.0001f, Speed)));
                        await UniTask.Delay(delay, Unscaled, Timing, _cts.Token);
                        if (_cts.IsCancellationRequested) break;
                        _ticks++;
                        OnTick?.Invoke(_ticks);
                        if (_repeatCount.HasValue && _ticks >= _repeatCount.Value) break;
                    }
                }
                FINISH:
                if (!_cts.IsCancellationRequested)
                {
                    _tcsTicks?.TrySetResult(_ticks);
                    TryComplete();
                }
            }
            catch (OperationCanceledException)
            {
                TryCancel();
            }
            catch (Exception e)
            {
                throw;
            }
            
        }
        
        /// <summary>
        /// 统一完成通路：设置状态、派发 OnCompleted 并结束 Completion/结果任务。
        /// </summary>
        void TryComplete()
        {
            if (State == TimerState.Canceled || State == TimerState.Completed) return;
            State = TimerState.Completed;
            OnCompleted?.Invoke();
            _tcs.TrySetResult();
        }

        /// <summary>
        /// 统一取消通路：设置状态、派发 OnCanceled 并取消所有任务。
        /// </summary>
        void TryCancel()
        {
            State = TimerState.Canceled;
            OnCanceled?.Invoke();
            _tcs.TrySetCanceled();
            _tcsTicks?.TrySetCanceled();
            _tcsElapsed?.TrySetCanceled();
        }
        
        /// <summary>
        /// 暂停计时器（停止推进但不重置状态）
        /// </summary>
        public void Pause()
        {
            if (State == TimerState.Running) State = TimerState.Paused;
        }

        /// <summary>
        /// 恢复计时器（从暂停状态回到运行）
        /// </summary>
        public void Resume()
        {
            if (State == TimerState.Paused) State = TimerState.Running;
        }

    }
    
    public static class UniTaskTimer
    {
        /// <summary>
        /// 创建倒计时计时器
        /// </summary>
        /// <param name="seconds"></param>
        /// <param name="unscaled"></param>
        /// <param name="onProgress"></param>
        /// <param name="onComplete"></param>
        /// <param name="onCancel"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static TimerHandle Countdown(float seconds, bool unscaled = false, Action<float> onProgress = null,
            Action onComplete = null, Action onCancel = null,CancellationToken ct = default)
        {
            var h = new TimerHandle(seconds, unscaled, ct);
            if(onProgress != null) h.OnProgress += onProgress;
            if(onComplete != null) h.OnCompleted += onComplete;
            if(onCancel != null) h.OnCanceled += onCancel;
            h.RunCountdown().Forget();
            return h;
        }
        
        
        /// <summary>
        /// 创建倒计时计时器
        /// </summary>
        public static TimerHandle Countdown(TimeSpan duration, bool unscaled = false, Action<float> onProgress = null,
            Action onCompleted = null, Action onCanceled = null, CancellationToken ct = default)
        {
            return Countdown((float)duration.TotalSeconds, unscaled, onProgress, onCompleted, onCanceled, ct);
        }

        /// <summary>
        /// 创建间隔计时器 不做补偿
        /// </summary>
        /// <param name="seconds"></param>
        /// <param name="count"></param>
        /// <param name="unscaled"></param>
        /// <param name="onTick"></param>
        /// <param name="onCompleted"></param>
        /// <param name="onCanceled"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static TimerHandle Interval(float seconds, int? count = null, bool unscaled = false,
            Action<int> onTick = null, Action onCompleted = null, Action onCanceled = null, CancellationToken ct = default)
        {
            var h = new TimerHandle(seconds,count,unscaled,false,0,ct);
            if (onTick != null) h.OnTick += onTick;
            if (onCompleted != null) h.OnCompleted += onCompleted;
            if (onCanceled != null) h.OnCanceled += onCanceled;
            h.RunInterval().Forget();
            return h;
        }
        
        /// <summary>
        /// 创建间隔计时器（TimeSpan 重载，固定节拍）。
        /// </summary>
        public static TimerHandle Interval(TimeSpan interval, int? count = null, bool unscaled = false,
            Action<int> onTick = null, Action onCompleted = null, Action onCanceled = null,
            CancellationToken ct = default)
        {
            return Interval((float)interval.TotalSeconds, count, unscaled, onTick, onCompleted, onCanceled, ct);
        }
        
        /// <summary>
        /// 创建间隔计时器（开启补偿，默认每帧最多补偿 5 次）。
        /// </summary>
        public static TimerHandle Interval(float seconds, int? count, bool unscaled, bool catchUp, Action<int> onTick,
            Action onCompleted = null, Action onCanceled = null, CancellationToken ct = default)
        {
            var h = new TimerHandle(seconds, count, unscaled, catchUp, 5, ct);
            if (onTick != null) h.OnTick += onTick;
            if (onCompleted != null) h.OnCompleted += onCompleted;
            if (onCanceled != null) h.OnCanceled += onCanceled;
            h.RunInterval().Forget();
            return h;
        }
        
        /// <summary>
        /// 创建间隔计时器（TimeSpan 重载，开启补偿）。
        /// </summary>
        public static TimerHandle Interval(TimeSpan interval, int? count, bool unscaled, bool catchUp,
            Action<int> onTick, Action onCompleted = null, Action onCanceled = null, CancellationToken ct = default)
        {
            return Interval((float)interval.TotalSeconds, count, unscaled, catchUp, onTick, onCompleted, onCanceled,
                ct);
        }
        
        /// <summary>
        /// 创建间隔计时器（补偿模式，指定每帧补偿上限）。
        /// </summary>
        public static TimerHandle Interval(float seconds, int? count, bool unscaled, bool catchUp, int maxCatchUps,
            Action<int> onTick, Action onCompleted = null, Action onCanceled = null, CancellationToken ct = default)
        {
            var h = new TimerHandle(seconds, count, unscaled, catchUp, maxCatchUps, ct);
            if (onTick != null) h.OnTick += onTick;
            if (onCompleted != null) h.OnCompleted += onCompleted;
            if (onCanceled != null) h.OnCanceled += onCanceled;
            h.RunInterval().Forget();
            return h;
        }
        
        /// <summary>
        /// 创建间隔计时器（TimeSpan 重载，补偿模式并指定上限）。
        /// </summary>
        public static TimerHandle Interval(TimeSpan interval, int? count, bool unscaled, bool catchUp, int maxCatchUps,
            Action<int> onTick, Action onCompleted = null, Action onCanceled = null, CancellationToken ct = default)
        {
            return Interval((float)interval.TotalSeconds, count, unscaled, catchUp, maxCatchUps, onTick, onCompleted,
                onCanceled, ct);
        }
        
        /// <summary>
        /// 创建间隔计时器（高级）：补偿上限、倍速、帧时序与预算限流。
        /// </summary>
        public static TimerHandle Interval(float seconds, int? count, bool unscaled, bool catchUp, int maxCatchUps,
            float speed, PlayerLoopTiming timing, int maxCatchUpBudgetMs, Action<int> onTick, Action onCompleted = null,
            Action onCanceled = null, CancellationToken ct = default)
        {
            var h = new TimerHandle(seconds, count, unscaled, catchUp, maxCatchUps, speed, timing, maxCatchUpBudgetMs,
                ct);
            if (onTick != null) h.OnTick += onTick;
            if (onCompleted != null) h.OnCompleted += onCompleted;
            if (onCanceled != null) h.OnCanceled += onCanceled;
            h.RunInterval().Forget();
            return h;
        }
        
        /// <summary>
        /// 仅延时（采用 UniTask.Delay），可选择是否忽略 timeScale。
        /// </summary>
        public static UniTask Delay(float seconds, bool unscaled = false, CancellationToken ct = default)
        {
            return UniTask.Delay(TimeSpan.FromSeconds(seconds), unscaled, PlayerLoopTiming.Update, ct);
        }
    }
}
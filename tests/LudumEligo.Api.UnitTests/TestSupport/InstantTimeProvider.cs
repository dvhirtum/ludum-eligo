namespace LudumEligo.Api.UnitTests.TestSupport;

/// <summary>
/// A clock whose timers fire at once: each timer advances the clock by its due time and then runs its callback.
/// Code that waits with <c>Task.Delay(…, timeProvider)</c> runs without real waiting, and <see cref="Delays"/>
/// records every wait.
/// </summary>
public sealed class InstantTimeProvider(DateTimeOffset start) : TimeProvider
{
    private readonly Lock _lock = new();
    private readonly List<TimeSpan> _delays = [];
    private DateTimeOffset _now = start;

    public InstantTimeProvider()
        : this(new DateTimeOffset(2026, 11, 13, 12, 0, 0, TimeSpan.Zero))
    {
    }

    public IReadOnlyList<TimeSpan> Delays
    {
        get
        {
            lock (_lock)
            {
                return [.. _delays];
            }
        }
    }

    public override DateTimeOffset GetUtcNow()
    {
        lock (_lock)
        {
            return _now;
        }
    }

    public void Advance(TimeSpan by)
    {
        lock (_lock)
        {
            _now += by;
        }
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new InstantTimer();
        if (dueTime != Timeout.InfiniteTimeSpan)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                if (timer.IsDisposed)
                {
                    return;
                }

                lock (_lock)
                {
                    _now += dueTime;
                    _delays.Add(dueTime);
                }

                callback(state);
            });
        }

        return timer;
    }

    private sealed class InstantTimer : ITimer
    {
        public bool IsDisposed { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

        public void Dispose() => IsDisposed = true;

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}

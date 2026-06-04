using Avalonia.Threading;
using Collectary.Presentation.Services;

namespace Collectary.UI.Services;

public sealed class DispatcherSyncScheduler : ISyncScheduler
{
    private DispatcherTimer? _timer;

    public void Start(TimeSpan interval, Func<Task> onTickAsync)
    {
        Stop();
        _timer = new DispatcherTimer { Interval = interval };
        _timer.Tick += (_, _) => _ = onTickAsync();
        _timer.Start();
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer = null;
    }

    public void Dispose() => Stop();
}

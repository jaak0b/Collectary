using Collectary.Presentation.Services;

namespace Collectary.UI.Services;

public sealed class DispatcherSyncScheduler : ISyncScheduler
{
    private readonly object _gate = new();
    private Timer? _timer;
    private CancellationTokenSource? _cancellation;
    private int _running;

    public event Action<Exception>? TickFailed;

    public void Start(TimeSpan interval, Func<CancellationToken, Task> onTickAsync)
    {
        Stop();
        lock (_gate)
        {
            _cancellation = new CancellationTokenSource();
            var token = _cancellation.Token;
            _timer = new Timer(_ => RunTick(onTickAsync, token), null, interval, interval);
        }
    }

    private async void RunTick(Func<CancellationToken, Task> onTickAsync, CancellationToken token)
    {
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0) return;
        try
        {
            if (!token.IsCancellationRequested)
                await onTickAsync(token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            TickFailed?.Invoke(ex);
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            _cancellation?.Cancel();
            _timer?.Dispose();
            _timer = null;
            _cancellation?.Dispose();
            _cancellation = null;
        }
    }

    public void Dispose() => Stop();
}

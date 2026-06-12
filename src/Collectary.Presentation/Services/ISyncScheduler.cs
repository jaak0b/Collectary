namespace Collectary.Presentation.Services;

public interface ISyncScheduler : IDisposable
{
    event Action<Exception>? TickFailed;

    void Start(TimeSpan interval, Func<CancellationToken, Task> onTickAsync);

    void Stop();
}

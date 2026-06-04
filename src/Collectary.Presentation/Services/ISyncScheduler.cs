namespace Collectary.Presentation.Services;

public interface ISyncScheduler : IDisposable
{
    void Start(TimeSpan interval, Func<Task> onTickAsync);
    void Stop();
}

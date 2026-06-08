using Collectary.Presentation.Services;

namespace Collectary.UI.Tests.Infrastructure;

public sealed class InlineUiDispatcher : IUiDispatcher
{
    public void Post(Action action) => action();
}

public sealed class InlineBackgroundRunner : IBackgroundRunner
{
    public Task<T> RunAsync<T>(Func<Task<T>> work) => work();
}

public sealed class RecordingUiDispatcher : IUiDispatcher
{
    private readonly Queue<Action> _pending = new();

    public int PostCount { get; private set; }

    public void Post(Action action)
    {
        PostCount++;
        _pending.Enqueue(action);
    }

    public void Drain()
    {
        while (_pending.Count > 0)
            _pending.Dequeue()();
    }
}

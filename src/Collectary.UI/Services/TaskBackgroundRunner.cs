using Collectary.Presentation.Services;

namespace Collectary.UI.Services;

public sealed class TaskBackgroundRunner : IBackgroundRunner
{
    public Task<T> RunAsync<T>(Func<Task<T>> work) => Task.Run(work);
}

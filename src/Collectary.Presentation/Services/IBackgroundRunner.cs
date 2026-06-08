namespace Collectary.Presentation.Services;

public interface IBackgroundRunner
{
    Task<T> RunAsync<T>(Func<Task<T>> work);
}

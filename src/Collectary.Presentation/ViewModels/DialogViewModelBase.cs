namespace Collectary.Presentation.ViewModels;

public abstract class DialogViewModelBase : ViewModelBase
{
    private readonly TaskCompletionSource<object?> _completion = new();

    public Task<object?> Completion => _completion.Task;

    public event Action<object?>? Closed;

    protected void Close(object? result)
    {
        _completion.TrySetResult(result);
        Closed?.Invoke(result);
    }
}

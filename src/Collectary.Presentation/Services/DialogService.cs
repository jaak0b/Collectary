namespace Collectary.UI.Services;

public static class DialogService
{
    public static IDialogService Instance { get; set; } = new NoopDialogService();

    private sealed class NoopDialogService : IDialogService
    {
        public Task<bool> ConfirmDeleteAsync(string itemName) => Task.FromResult(false);
        public Task ShowMessageAsync(string message, string title = "") => Task.CompletedTask;
    }
}

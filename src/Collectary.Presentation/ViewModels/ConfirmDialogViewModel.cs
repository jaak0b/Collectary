using CommunityToolkit.Mvvm.Input;

namespace Collectary.Presentation.ViewModels;

public partial class ConfirmDialogViewModel : DialogViewModelBase
{
    public ConfirmDialogViewModel(string message, string confirmLabel, string cancelLabel, string title)
    {
        Message = message;
        ConfirmLabel = confirmLabel;
        CancelLabel = cancelLabel;
        Title = title;
    }

    public string Message { get; }

    public string ConfirmLabel { get; }

    public string CancelLabel { get; }

    public string Title { get; }

    [RelayCommand]
    private void Confirm() => Close(true);

    [RelayCommand]
    private void Cancel() => Close(false);
}

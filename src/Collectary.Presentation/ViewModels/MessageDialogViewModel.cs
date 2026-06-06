using CommunityToolkit.Mvvm.Input;

namespace Collectary.Presentation.ViewModels;

public partial class MessageDialogViewModel : DialogViewModelBase
{
    public MessageDialogViewModel(string message, string title)
    {
        Message = message;
        Title = title;
    }

    public string Message { get; }

    public string Title { get; }

    [RelayCommand]
    private void Ok() => Close(null);
}

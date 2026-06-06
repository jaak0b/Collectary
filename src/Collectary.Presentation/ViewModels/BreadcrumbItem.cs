using System.Windows.Input;

namespace Collectary.Presentation.ViewModels;

public class BreadcrumbItem
{
    public string Title { get; }
    public bool IsHome { get; }
    public bool IsCurrent { get; }
    public ICommand? NavigateCommand { get; }
    public object? CommandParameter { get; }

    public BreadcrumbItem(string title, ICommand? navigateCommand, object? commandParameter, bool isHome, bool isCurrent)
    {
        Title = title;
        NavigateCommand = navigateCommand;
        CommandParameter = commandParameter;
        IsHome = isHome;
        IsCurrent = isCurrent;
    }
}

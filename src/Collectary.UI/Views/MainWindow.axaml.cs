using Avalonia.Controls;
using Collectary.UI.Services;

namespace Collectary.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AvaloniaDialogService.Instance.Owner = this;
        DialogService.Instance = AvaloniaDialogService.Instance;
    }
}

using Avalonia.Controls;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Views;

public partial class CloudFolderPicker : Window
{
    public CloudFolderPicker()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is CloudFolderPickerViewModel vm)
                vm.CloseRequested = result => Close(result);
        };
    }
}

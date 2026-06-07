using Avalonia;
using Avalonia.Controls;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Views;

public partial class CameraScannerView : UserControl
{
    public CameraScannerView() => InitializeComponent();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is CameraScannerViewModel vm)
            _ = vm.StartAsync();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (DataContext is CameraScannerViewModel vm)
            vm.NotifyClosedExternally();
    }
}

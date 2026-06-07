using System;
using Avalonia.Controls;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Views;

public partial class BarcodeFieldEditorView : UserControl
{
    public BarcodeFieldEditorView()
    {
        InitializeComponent();
        if (ScanButton.Flyout is { } flyout)
            flyout.Opened += OnScanFlyoutOpened;
    }

    private void OnScanFlyoutOpened(object? sender, EventArgs e)
    {
        if (DataContext is BarcodeFieldEditorViewModel vm)
            vm.EnsureCameraAvailabilityCommand.Execute(null);
    }
}

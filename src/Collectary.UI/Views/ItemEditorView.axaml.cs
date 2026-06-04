using Avalonia.Controls;
using Collectary.UI.Controls;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Views;

public partial class ItemEditorView : UserControl
{
    public ItemEditorView()
    {
        InitializeComponent();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (DataContext is ItemEditorViewModel vm)
            vm.IsNarrow = e.NewSize.Width is > 0 and < ResponsiveSplitLayout.NarrowThreshold;
    }
}

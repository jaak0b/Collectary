using Avalonia.Controls;
using Collectary.UI.Controls;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Views;

public partial class ListEntryEditorView : UserControl
{
    public ListEntryEditorView()
    {
        InitializeComponent();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (DataContext is ListEntryEditorViewModel vm)
            vm.IsNarrow = e.NewSize.Width is > 0 and < ResponsiveSplitLayout.NarrowThreshold;
    }
}

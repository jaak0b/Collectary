using Avalonia;
using Avalonia.Controls;
using Collectary.UI.Controls;
using Collectary.UI.ViewModels.SystemFields;

namespace Collectary.UI.Views.SystemFields;

public partial class SystemFieldLibraryView : UserControl
{
    private readonly ResponsiveSplitLayout _layout;
    private readonly ListReorderBehavior _reorder;

    public SystemFieldLibraryView()
    {
        InitializeComponent();
        _layout = new ResponsiveSplitLayout(SplitGrid, MasterPane, PaneSplitter, DetailPane);
        _reorder = new ListReorderBehavior(FieldListBox,
            (from, to) => _ = (DataContext as SystemFieldLibraryViewModel)?.ReorderAsync(from, to));
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _reorder.Attach();
        _layout.Attach(Bounds.Width);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _reorder.Detach();
        _layout.Detach();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        _layout.Apply(e.NewSize.Width);
    }
}

using Avalonia;
using Avalonia.Controls;
using Collectary.UI.Controls;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Views;

public partial class HomeView : UserControl
{
    private readonly PointerReorderBehavior _reorder;

    public HomeView()
    {
        InitializeComponent();
        _reorder = new PointerReorderBehavior(PresetItemsControl, OnMove, OnCommit, OnDragActive);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _reorder.Attach();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _reorder.Detach();
    }

    private void OnMove(int from, int to) => (DataContext as HomeViewModel)?.Rows.Move(from, to);

    private async void OnCommit()
    {
        if (DataContext is HomeViewModel vm) await vm.SavePresetOrderAsync();
    }

    private void OnDragActive(object? item, bool active)
    {
        if (item is IDraggableRow row) row.IsDragging = active;
    }
}

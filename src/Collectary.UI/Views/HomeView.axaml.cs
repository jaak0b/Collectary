using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Views;

public partial class HomeView : UserControl
{
    private static readonly DataFormat<object> PresetIndexFormat =
        DataFormat.CreateInProcessFormat<object>("Collectary.presetIndex");

    public HomeView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        PresetItemsControl.AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        PresetItemsControl.AddHandler(DragDrop.DropEvent, OnPresetDrop);
        PresetItemsControl.AddHandler(DragDrop.DragOverEvent, OnPresetDragOver);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        PresetItemsControl.RemoveHandler(PointerPressedEvent, OnPointerPressed);
        PresetItemsControl.RemoveHandler(DragDrop.DropEvent, OnPresetDrop);
        PresetItemsControl.RemoveHandler(DragDrop.DragOverEvent, OnPresetDragOver);
    }

    private async void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (!IsDragHandle(e.Source)) return;
        var index = GetItemIndex(PresetItemsControl, e.Source as Visual);
        if (index < 0) return;
        var data = new DataTransfer();
        data.Add(DataTransferItem.Create(PresetIndexFormat, index));
        await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move);

    }

    private void OnPresetDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(PresetIndexFormat) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnPresetDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not HomeViewModel vm) return;
        if (!e.DataTransfer.Contains(PresetIndexFormat)) return;
        if (e.DataTransfer.TryGetValue(PresetIndexFormat) is not int from) return;
        var to = GetDropIndex(PresetItemsControl, e.GetPosition(PresetItemsControl));
        if (from != to)
        {
            vm.Rows.Move(from, to);
            await vm.SavePresetOrderAsync();
        }
    }

    private static bool IsDragHandle(object? source)
    {
        var current = source as Visual;
        while (current is not null)
        {
            if (current is Control { Tag: "DragHandle" }) return true;
            current = current.GetVisualParent();
        }
        return false;
    }

    private static int GetItemIndex(ItemsControl list, Visual? source)
    {
        if (source is null) return -1;
        for (var i = 0; i < list.ItemCount; i++)
        {
            var container = list.ContainerFromIndex(i);
            if (container is not null && container.IsVisualAncestorOf(source))
                return i;
        }
        return -1;
    }

    private static int GetDropIndex(ItemsControl list, Point dropPoint)
    {
        for (var i = 0; i < list.ItemCount; i++)
        {
            var container = list.ContainerFromIndex(i);
            if (container is null) continue;
            var bounds = container.Bounds;
            if (dropPoint.Y < bounds.Y + bounds.Height / 2)
                return i;
        }
        return Math.Max(0, list.ItemCount - 1);
    }
}

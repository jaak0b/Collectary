using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Collectary.UI.Controls;

public sealed class ListReorderBehavior
{
    private static readonly DataFormat<object> IndexFormat =
        DataFormat.CreateInProcessFormat<object>("Collectary.reorderIndex");

    private readonly ListBox _list;
    private readonly Action<int, int> _onReorder;

    public ListReorderBehavior(ListBox list, Action<int, int> onReorder)
    {
        _list = list;
        _onReorder = onReorder;
    }

    public void Attach()
    {
        _list.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        _list.AddHandler(DragDrop.DropEvent, OnDrop);
        _list.AddHandler(DragDrop.DragOverEvent, OnDragOver);
    }

    public void Detach()
    {
        _list.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        _list.RemoveHandler(DragDrop.DropEvent, OnDrop);
        _list.RemoveHandler(DragDrop.DragOverEvent, OnDragOver);
    }

    private async void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(_list).Properties.IsLeftButtonPressed) return;
        if (!IsDragHandle(e.Source)) return;
        var index = GetItemIndex(_list, e.Source as Visual);
        if (index < 0) return;
        var data = new DataTransfer();
        data.Add(DataTransferItem.Create(IndexFormat, index));
        await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(IndexFormat) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (!e.DataTransfer.Contains(IndexFormat)) return;
        if (e.DataTransfer.TryGetValue(IndexFormat) is not int from) return;
        var to = GetDropIndex(_list, e.GetPosition(_list));
        _onReorder(from, to);
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

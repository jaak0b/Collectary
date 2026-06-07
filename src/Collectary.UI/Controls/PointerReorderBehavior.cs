using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Collectary.UI.Controls;

public sealed class PointerReorderBehavior
{
    private readonly ItemsControl _list;
    private readonly Action<int, int> _onMove;
    private readonly Action _onCommit;
    private readonly Action<object?, bool>? _onDragActive;
    private readonly double _dragThreshold = 4;

    private int _fromIndex = -1;
    private int _currentIndex = -1;
    private object? _draggedItem;
    private Point _pressPoint;
    private bool _pending;
    private bool _dragging;
    private bool _movedAtLeastOnce;

    public PointerReorderBehavior(
        ItemsControl list,
        Action<int, int> onMove,
        Action onCommit,
        Action<object?, bool>? onDragActive = null)
    {
        _list = list;
        _onMove = onMove;
        _onCommit = onCommit;
        _onDragActive = onDragActive;
    }

    public void Attach()
    {
        _list.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        _list.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        _list.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
        _list.AddHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost);
    }

    public void Detach()
    {
        _list.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        _list.RemoveHandler(InputElement.PointerMovedEvent, OnPointerMoved);
        _list.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
        _list.RemoveHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsDragHandle(e.Source)) return;
        var index = GetItemIndex(e.Source as Visual);
        if (index < 0) return;

        _fromIndex = index;
        _currentIndex = index;
        _draggedItem = _list.ContainerFromIndex(index)?.DataContext;
        _pressPoint = e.GetPosition(_list);
        _pending = true;
        _dragging = false;
        _movedAtLeastOnce = false;
        e.PreventGestureRecognition();
        e.Pointer.Capture(_list);
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_pending) return;
        var position = e.GetPosition(_list);
        if (!_dragging)
        {
            var delta = position - _pressPoint;
            if (Math.Abs(delta.X) < _dragThreshold && Math.Abs(delta.Y) < _dragThreshold) return;
            _dragging = true;
            _onDragActive?.Invoke(_draggedItem, true);
        }

        var target = ResolveTarget(position.Y);
        if (target != _currentIndex)
        {
            _onMove(_currentIndex, target);
            _currentIndex = target;
            _movedAtLeastOnce = true;
        }

        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_pending) return;
        var commit = _movedAtLeastOnce;
        Reset();
        e.Pointer.Capture(null);
        if (commit)
        {
            _onCommit();
            e.Handled = true;
        }
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e) => Reset();

    private void Reset()
    {
        if (_draggedItem is not null)
        {
            _onDragActive?.Invoke(_draggedItem, false);
            _draggedItem = null;
        }
        _pending = false;
        _dragging = false;
        _movedAtLeastOnce = false;
        _fromIndex = -1;
        _currentIndex = -1;
    }

    private int ResolveTarget(double pointerY)
    {
        var lastRealized = -1;
        for (var i = 0; i < _list.ItemCount; i++)
        {
            var container = _list.ContainerFromIndex(i);
            if (container is null) continue;
            lastRealized = i;
            var bounds = container.Bounds;
            if (pointerY < bounds.Y) return i == 0 ? 0 : i;
            if (pointerY <= bounds.Y + bounds.Height)
                return DirectionalTarget(i, bounds, pointerY);
        }
        return lastRealized < 0 ? _currentIndex : lastRealized;
    }

    private int DirectionalTarget(int index, Rect bounds, double pointerY)
    {
        if (index == _currentIndex) return _currentIndex;
        var midpoint = bounds.Y + bounds.Height / 2;
        if (index > _currentIndex)
            return pointerY > midpoint ? index : _currentIndex;
        return pointerY < midpoint ? index : _currentIndex;
    }

    private bool IsDragHandle(object? source)
    {
        var current = source as Visual;
        while (current is not null)
        {
            if (current is Control { Tag: "DragHandle" }) return true;
            current = current.GetVisualParent();
        }
        return false;
    }

    private int GetItemIndex(Visual? source)
    {
        if (source is null) return -1;
        for (var i = 0; i < _list.ItemCount; i++)
        {
            var container = _list.ContainerFromIndex(i);
            if (container is not null && container.IsVisualAncestorOf(source))
                return i;
        }
        return -1;
    }
}

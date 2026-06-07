using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Collectary.UI.Controls;

namespace Collectary.UI.Tests.Controls;

[TestFixture]
public class PointerReorderBehaviorTest
{
    private sealed class Recorder
    {
        public List<(int From, int To)> Moves { get; } = new();
        public int Commits { get; set; }
        public List<(object? Item, bool Active)> DragActive { get; } = new();
    }

    private static ListBox BuildList(ObservableCollection<string> items, bool withHandle)
    {
        return new ListBox
        {
            ItemsSource = items,
            Width = 200,
            Height = 400,
            ItemTemplate = new FuncDataTemplate<string>((_, _) => new Border
            {
                Height = 40,
                Background = Brushes.Transparent,
                Tag = withHandle ? "DragHandle" : null,
            }, true),
        };
    }

    private static PointerReorderBehavior LiveBehavior(ListBox list, ObservableCollection<string> items, Recorder rec)
    {
        return new PointerReorderBehavior(
            list,
            (from, to) => { rec.Moves.Add((from, to)); items.Move(from, to); },
            () => rec.Commits++,
            (item, active) => rec.DragActive.Add((item, active)));
    }

    private static Point Center(Control container, Visual relativeTo, double yFraction = 0.5) =>
        container.TranslatePoint(
            new Point(container.Bounds.Width / 2, container.Bounds.Height * yFraction),
            relativeTo)!.Value;

    [Test]
    public void DraggingDownAcrossRows_MovesLiveAndCommitsOnce()
    {
        var items = new ObservableCollection<string> { "A", "B", "C", "D" };
        var list = BuildList(items, withHandle: true);
        var rec = new Recorder();
        var behavior = LiveBehavior(list, items, rec);
        var window = new Window { Content = list, Width = 200, Height = 400 };
        window.Show();
        behavior.Attach();
        Dispatcher.UIThread.RunJobs();

        var start = Center(list.ContainerFromIndex(0)!, window);
        var overRow1 = Center(list.ContainerFromIndex(1)!, window, yFraction: 0.8);
        var overRow2 = Center(list.ContainerFromIndex(2)!, window, yFraction: 0.8);

        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(overRow1);
        Dispatcher.UIThread.RunJobs();
        window.MouseMove(overRow2);
        Dispatcher.UIThread.RunJobs();
        window.MouseUp(overRow2, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.That(rec.Moves, Has.Count.GreaterThanOrEqualTo(2), "Each row crossed should move the item live.");
        Assert.That(rec.Moves[0], Is.EqualTo((0, 1)));
        Assert.That(items, Is.EqualTo(new[] { "B", "C", "A", "D" }).AsCollection);
        Assert.That(rec.Commits, Is.EqualTo(1), "Persistence happens exactly once, on release.");
    }

    [Test]
    public void ManyMoves_CommitsExactlyOnce()
    {
        var items = new ObservableCollection<string> { "A", "B", "C", "D" };
        var list = BuildList(items, withHandle: true);
        var rec = new Recorder();
        var behavior = LiveBehavior(list, items, rec);
        var window = new Window { Content = list, Width = 200, Height = 400 };
        window.Show();
        behavior.Attach();
        Dispatcher.UIThread.RunJobs();

        window.MouseDown(Center(list.ContainerFromIndex(0)!, window), MouseButton.Left);
        for (var i = 1; i <= 3; i++)
        {
            window.MouseMove(Center(list.ContainerFromIndex(i)!, window, yFraction: 0.8));
            Dispatcher.UIThread.RunJobs();
        }
        window.MouseUp(Center(list.ContainerFromIndex(3)!, window, yFraction: 0.8), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.That(rec.Commits, Is.EqualTo(1));
    }

    [Test]
    public void TapWithoutMovement_DoesNotMoveOrCommit()
    {
        var items = new ObservableCollection<string> { "A", "B", "C" };
        var list = BuildList(items, withHandle: true);
        var rec = new Recorder();
        var behavior = LiveBehavior(list, items, rec);
        var window = new Window { Content = list, Width = 200, Height = 400 };
        window.Show();
        behavior.Attach();
        Dispatcher.UIThread.RunJobs();

        var spot = Center(list.ContainerFromIndex(0)!, window);
        window.MouseDown(spot, MouseButton.Left);
        window.MouseUp(spot, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.That(rec.Moves, Is.Empty);
        Assert.That(rec.Commits, Is.EqualTo(0));
    }

    [Test]
    public void DragActive_TogglesOnDraggedItem()
    {
        var items = new ObservableCollection<string> { "A", "B", "C" };
        var list = BuildList(items, withHandle: true);
        var rec = new Recorder();
        var behavior = LiveBehavior(list, items, rec);
        var window = new Window { Content = list, Width = 200, Height = 400 };
        window.Show();
        behavior.Attach();
        Dispatcher.UIThread.RunJobs();

        window.MouseDown(Center(list.ContainerFromIndex(0)!, window), MouseButton.Left);
        window.MouseMove(Center(list.ContainerFromIndex(1)!, window, yFraction: 0.8));
        Dispatcher.UIThread.RunJobs();
        window.MouseUp(Center(list.ContainerFromIndex(1)!, window, yFraction: 0.8), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.That(rec.DragActive, Has.Count.EqualTo(2));
        Assert.That(rec.DragActive[0], Is.EqualTo(((object?)"A", true)));
        Assert.That(rec.DragActive[1], Is.EqualTo(((object?)"A", false)));
    }

    [Test]
    public void JitterAtRowBoundary_DoesNotOscillate()
    {
        var items = new ObservableCollection<string> { "A", "B", "C", "D" };
        var list = BuildList(items, withHandle: true);
        var rec = new Recorder();
        var behavior = LiveBehavior(list, items, rec);
        var window = new Window { Content = list, Width = 200, Height = 400 };
        window.Show();
        behavior.Attach();
        Dispatcher.UIThread.RunJobs();

        window.MouseDown(Center(list.ContainerFromIndex(0)!, window), MouseButton.Left);
        window.MouseMove(Center(list.ContainerFromIndex(1)!, window, yFraction: 0.8));
        Dispatcher.UIThread.RunJobs();
        var upperOfRow0 = Center(list.ContainerFromIndex(0)!, window, yFraction: 0.75);
        window.MouseMove(upperOfRow0);
        Dispatcher.UIThread.RunJobs();
        window.MouseUp(upperOfRow0, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.That(rec.Moves, Is.EqualTo(new[] { (0, 1) }).AsCollection,
            "A small back-jitter that does not cross the far half must not move the item back.");
    }

    [Test]
    public void TappingButtonInsideList_StillFiresClick()
    {
        var clicked = false;
        var button = new Button { Content = "Open", Width = 160, Height = 40 };
        button.Click += (_, _) => clicked = true;
        var list = new ListBox
        {
            ItemsSource = new[] { "only" },
            Width = 200,
            Height = 400,
            ItemTemplate = new FuncDataTemplate<string>((_, _) => button, true),
        };
        var behavior = new PointerReorderBehavior(list, (_, _) => { }, () => { });
        var window = new Window { Content = list, Width = 200, Height = 400 };
        window.Show();
        behavior.Attach();
        Dispatcher.UIThread.RunJobs();

        var spot = Center(button, window);
        window.MouseDown(spot, MouseButton.Left);
        window.MouseUp(spot, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.That(clicked, Is.True,
            "A button inside the reorderable list must still receive its click; the reorder behavior must not cancel the button's pointer capture.");
    }

    [Test]
    public void PointerPressOutsideHandle_DoesNotReorder()
    {
        var items = new ObservableCollection<string> { "A", "B", "C" };
        var list = BuildList(items, withHandle: false);
        var rec = new Recorder();
        var behavior = LiveBehavior(list, items, rec);
        var window = new Window { Content = list, Width = 200, Height = 400 };
        window.Show();
        behavior.Attach();
        Dispatcher.UIThread.RunJobs();

        window.MouseDown(Center(list.ContainerFromIndex(0)!, window), MouseButton.Left);
        window.MouseMove(Center(list.ContainerFromIndex(2)!, window, yFraction: 0.8));
        window.MouseUp(Center(list.ContainerFromIndex(2)!, window, yFraction: 0.8), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.That(rec.Moves, Is.Empty);
        Assert.That(rec.Commits, Is.EqualTo(0));
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Collectary.Presentation.Layout;

namespace Collectary.UI.Controls;

public class BreadcrumbBarPanel : Panel
{
    static BreadcrumbBarPanel()
    {
        AffectsMeasure<BreadcrumbBarPanel>(OverflowReservationProperty);
    }

    public static readonly StyledProperty<double> OverflowReservationProperty =
        AvaloniaProperty.Register<BreadcrumbBarPanel, double>(nameof(OverflowReservation), 44.0);

    public static readonly AttachedProperty<bool> IsOverflowProperty =
        AvaloniaProperty.RegisterAttached<BreadcrumbBarPanel, Control, bool>("IsOverflow");

    public static void SetIsOverflow(Control control, bool value) => control.SetValue(IsOverflowProperty, value);

    public static bool GetIsOverflow(Control control) => control.GetValue(IsOverflowProperty);

    public double OverflowReservation
    {
        get => GetValue(OverflowReservationProperty);
        set => SetValue(OverflowReservationProperty, value);
    }

    public event EventHandler? CollapsedChanged;

    private const double TrailingMargin = 12;

    private readonly BreadcrumbCollapseEngine _engine = new();
    private IReadOnlyList<int> _collapsedIndices = Array.Empty<int>();
    private bool _showOverflow;

    public IReadOnlyList<int> CollapsedIndices => _collapsedIndices;

    private Control? OverflowChild() => Children.FirstOrDefault(GetIsOverflow);

    private IReadOnlyList<Control> Crumbs() => Children.Where(c => !GetIsOverflow(c)).ToList();

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children)
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));

        var crumbs = Crumbs();
        if (crumbs.Count == 0)
        {
            SetCollapsed(Array.Empty<int>(), false);
            return new Size(0, 0);
        }

        var overflow = OverflowChild();
        double overflowWidth = overflow?.DesiredSize.Width ?? OverflowReservation;
        double available = double.IsInfinity(availableSize.Width)
            ? crumbs.Sum(c => c.DesiredSize.Width)
            : availableSize.Width;

        var widths = crumbs.Select(c => c.DesiredSize.Width).ToList();
        var plan = _engine.Resolve(widths, available, overflowWidth, 0, crumbs.Count - 1);

        SetCollapsed(plan.CollapsedIndices, plan.ShowOverflow);

        double width = plan.VisibleIndices.Sum(i => widths[i])
            + (plan.ShowOverflow ? overflowWidth : 0);
        double height = Children.Max(c => c.DesiredSize.Height);
        return new Size(Math.Min(width, available), height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var crumbs = Crumbs();
        var overflow = OverflowChild();
        double height = finalSize.Height;

        if (overflow is not null && !_showOverflow)
            Hide(overflow, height);

        if (crumbs.Count == 0)
            return finalSize;

        var widths = crumbs.Select(c => c.DesiredSize.Width).ToList();
        double available = finalSize.Width;
        var plan = _engine.Resolve(widths, available, overflow?.DesiredSize.Width ?? OverflowReservation, 0, crumbs.Count - 1);
        var visible = new HashSet<int>(plan.VisibleIndices);

        for (int i = 0; i < crumbs.Count; i++)
            if (!visible.Contains(i))
                Hide(crumbs[i], height);

        double x = 0;
        double remaining = Math.Max(0, finalSize.Width - TrailingMargin);

        if (plan.VisibleIndices.Contains(0))
            x += Place(crumbs[0], ref remaining, x, height);

        if (plan.ShowOverflow && overflow is not null)
            x += Place(overflow, ref remaining, x, height);

        foreach (var i in plan.VisibleIndices.Where(i => i != 0))
            x += Place(crumbs[i], ref remaining, x, height);

        return finalSize;
    }

    private double Place(Control child, ref double remaining, double x, double height)
    {
        child.Opacity = 1;
        child.IsHitTestVisible = true;
        double w = Math.Min(child.DesiredSize.Width, Math.Max(0, remaining));
        child.Arrange(new Rect(x, 0, w, height));
        remaining -= w;
        return w;
    }

    // Collapsed crumbs stay in the tree because the overflow decision needs their measured widths;
    // IsVisible=false would zero those widths and oscillate, so they are made inert instead.
    // They are arranged at their natural width (not zero) so a trimming TextBlock never lays out at width 0.
    private void Hide(Control child, double height)
    {
        child.Opacity = 0;
        child.IsHitTestVisible = false;
        child.Arrange(new Rect(0, 0, child.DesiredSize.Width, height));
    }

    private void SetCollapsed(IReadOnlyList<int> collapsed, bool showOverflow)
    {
        _showOverflow = showOverflow;
        if (collapsed.SequenceEqual(_collapsedIndices)) return;
        _collapsedIndices = collapsed;
        Dispatcher.UIThread.Post(() => CollapsedChanged?.Invoke(this, EventArgs.Empty), DispatcherPriority.Render);
    }
}

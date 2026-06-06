using Avalonia;
using Avalonia.Controls;
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
            _collapsedIndices = Array.Empty<int>();
            _showOverflow = false;
            return new Size(0, 0);
        }

        var overflow = OverflowChild();
        double overflowWidth = overflow?.DesiredSize.Width ?? OverflowReservation;
        double available = double.IsInfinity(availableSize.Width)
            ? crumbs.Sum(c => c.DesiredSize.Width)
            : availableSize.Width;

        var widths = crumbs.Select(c => c.DesiredSize.Width).ToList();
        var plan = _engine.Resolve(widths, available, overflowWidth, 0, crumbs.Count - 1);

        _collapsedIndices = plan.CollapsedIndices;
        _showOverflow = plan.ShowOverflow;

        double width = crumbs[0].DesiredSize.Width
            + (plan.ShowOverflow ? overflowWidth : 0)
            + plan.VisibleIndices.Where(i => i != 0).Sum(i => widths[i]);
        double height = Children.Max(c => c.DesiredSize.Height);
        return new Size(Math.Min(width, available), height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var crumbs = Crumbs();
        var overflow = OverflowChild();

        if (overflow is not null && !_showOverflow)
            overflow.Arrange(default);

        if (crumbs.Count == 0)
            return finalSize;

        var widths = crumbs.Select(c => c.DesiredSize.Width).ToList();
        double available = finalSize.Width;
        var plan = _engine.Resolve(widths, available, overflow?.DesiredSize.Width ?? OverflowReservation, 0, crumbs.Count - 1);
        var visible = new HashSet<int>(plan.VisibleIndices);

        for (int i = 0; i < crumbs.Count; i++)
            if (!visible.Contains(i))
                crumbs[i].Arrange(default);

        double x = 0;
        double height = finalSize.Height;

        x += PlaceSequential(crumbs[0], ref available, x, height);

        if (plan.ShowOverflow && overflow is not null)
            x += PlaceSequential(overflow, ref available, x, height);

        foreach (var i in plan.VisibleIndices.Where(i => i != 0))
            x += PlaceSequential(crumbs[i], ref available, x, height);

        return finalSize;
    }

    private double PlaceSequential(Control child, ref double remaining, double x, double height)
    {
        double w = Math.Min(child.DesiredSize.Width, Math.Max(0, remaining));
        child.Arrange(new Rect(x, 0, w, height));
        remaining -= w;
        return w;
    }
}

using Avalonia;
using Avalonia.Controls;
using Collectary.Search.ViewModels;

namespace Collectary.Search.Avalonia.Controls;

public class SearchRowPanel : Panel
{
    static SearchRowPanel()
    {
        AffectsMeasure<SearchRowPanel>(FiltersExpandedProperty);
    }

    public static readonly StyledProperty<bool> FiltersExpandedProperty =
        AvaloniaProperty.Register<SearchRowPanel, bool>(nameof(FiltersExpanded));

    public bool FiltersExpanded
    {
        get => GetValue(FiltersExpandedProperty);
        set => SetValue(FiltersExpandedProperty, value);
    }

    private const double ClusterSpacing = 8;
    private const double RowSpacing = 6;

    private readonly ResponsiveSearchBarLayout _layout = new();
    private bool _cachedStacked;
    private bool _cachedControlsStacked;
    private double _cachedNatural;
    private double _cachedTopRowNatural;

    private Control Search => Children[0];
    private Control Chips => Children[1];
    private Control Trailing => Children[2];
    private Control Toggle => Children[3];

    public bool IsStacked => _cachedStacked;

    public bool IsControlsStacked => _cachedControlsStacked;

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children)
            child.Measure(Size.Infinity);

        double natural = Search.DesiredSize.Width
            + ClusterSpacing + Chips.DesiredSize.Width
            + ClusterSpacing + Trailing.DesiredSize.Width;

        double available = double.IsInfinity(availableSize.Width) ? natural : availableSize.Width;
        _cachedStacked = _layout.ShouldStack(available, natural);
        _cachedNatural = natural;
        _cachedTopRowNatural = Search.DesiredSize.Width
            + ClusterSpacing + Trailing.DesiredSize.Width
            + ClusterSpacing + Toggle.DesiredSize.Width;

        if (!_cachedStacked)
        {
            _cachedControlsStacked = false;
            double rowHeight = Math.Max(Search.DesiredSize.Height,
                Math.Max(Chips.DesiredSize.Height, Trailing.DesiredSize.Height));
            return new Size(available, rowHeight);
        }

        _cachedControlsStacked = _layout.ShouldStack(available, _cachedTopRowNatural);

        double topPortion = TopPortionHeight();
        if (!FiltersExpanded)
            return new Size(available, topPortion);

        Chips.Measure(new Size(available, double.PositiveInfinity));
        return new Size(available, topPortion + RowSpacing + Chips.DesiredSize.Height);
    }

    private double TopPortionHeight() => _cachedControlsStacked
        ? Search.DesiredSize.Height + RowSpacing + ControlsRowHeight()
        : TopRowHeight();

    private double TopRowHeight() => Math.Max(Search.DesiredSize.Height,
        Math.Max(Trailing.DesiredSize.Height, Toggle.DesiredSize.Height));

    private double ControlsRowHeight() =>
        Math.Max(Trailing.DesiredSize.Height, Toggle.DesiredSize.Height);

    protected override Size ArrangeOverride(Size finalSize) =>
        _layout.ShouldStack(finalSize.Width, _cachedNatural)
            ? ArrangeStacked(finalSize)
            : ArrangeWide(finalSize);

    private Size ArrangeWide(Size finalSize)
    {
        _cachedControlsStacked = false;
        Hide(Toggle);
        double height = finalSize.Height;
        double chipsW = Chips.DesiredSize.Width;
        double trailingW = Trailing.DesiredSize.Width;
        double searchW = Math.Max(0, finalSize.Width - chipsW - trailingW - 2 * ClusterSpacing);

        Search.Arrange(new Rect(0, 0, searchW, height));
        double x = searchW + ClusterSpacing;
        Show(Chips).Arrange(new Rect(x, 0, chipsW, height));
        x += chipsW + ClusterSpacing;
        Show(Trailing).Arrange(new Rect(x, 0, trailingW, height));
        return finalSize;
    }

    private Size ArrangeStacked(Size finalSize)
    {
        double trailingW = Trailing.DesiredSize.Width;
        double toggleW = Toggle.DesiredSize.Width;
        _cachedControlsStacked = _layout.ShouldStack(finalSize.Width, _cachedTopRowNatural);

        double topPortion = _cachedControlsStacked
            ? ArrangeStackedControls(finalSize, trailingW, toggleW)
            : ArrangeSharedTopRow(finalSize, trailingW, toggleW);

        if (!FiltersExpanded)
        {
            Hide(Chips);
            return new Size(finalSize.Width, topPortion);
        }

        double y = topPortion + RowSpacing;
        Show(Chips).Arrange(new Rect(0, y, finalSize.Width, Chips.DesiredSize.Height));
        return new Size(finalSize.Width, y + Chips.DesiredSize.Height);
    }

    private double ArrangeSharedTopRow(Size finalSize, double trailingW, double toggleW)
    {
        double topRow = TopRowHeight();
        double searchW = Math.Max(0, finalSize.Width - trailingW - toggleW - 2 * ClusterSpacing);

        Search.Arrange(new Rect(0, 0, searchW, topRow));
        double x = searchW + ClusterSpacing;
        Show(Trailing).Arrange(new Rect(x, 0, trailingW, topRow));
        x += trailingW + ClusterSpacing;
        Show(Toggle).Arrange(new Rect(x, 0, toggleW, topRow));
        return topRow;
    }

    private double ArrangeStackedControls(Size finalSize, double trailingW, double toggleW)
    {
        double searchH = Search.DesiredSize.Height;
        double controlsH = ControlsRowHeight();

        Search.Arrange(new Rect(0, 0, finalSize.Width, searchH));
        double y = searchH + RowSpacing;
        Show(Trailing).Arrange(new Rect(0, y, trailingW, controlsH));
        Show(Toggle).Arrange(new Rect(trailingW + ClusterSpacing, y, toggleW, controlsH));
        return y + controlsH;
    }

    // Collapsed clusters stay measurable: IsVisible=false would zero their measured width,
    // which flips the stack decision and oscillates. Made inert via opacity instead.
    private void Hide(Control child)
    {
        child.Opacity = 0;
        child.IsHitTestVisible = false;
        child.Arrange(new Rect(0, 0, child.DesiredSize.Width, child.DesiredSize.Height));
    }

    private Control Show(Control child)
    {
        child.Opacity = 1;
        child.IsHitTestVisible = true;
        return child;
    }
}

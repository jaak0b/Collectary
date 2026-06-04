using Avalonia;
using Avalonia.Controls;
using Collectary.Presentation.Layout;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Controls;

public class FieldGridPanel : Panel
{
    static FieldGridPanel()
    {
        AffectsMeasure<FieldGridPanel>(
            ColumnCountProperty,
            MinColumnWidthProperty,
            ColumnSpacingProperty);
    }

    public static readonly StyledProperty<int> ColumnCountProperty =
        AvaloniaProperty.Register<FieldGridPanel, int>(nameof(ColumnCount), 1);

    public static readonly StyledProperty<double> MinColumnWidthProperty =
        AvaloniaProperty.Register<FieldGridPanel, double>(nameof(MinColumnWidth), 180.0);

    public static readonly StyledProperty<double> ColumnSpacingProperty =
        AvaloniaProperty.Register<FieldGridPanel, double>(nameof(ColumnSpacing), 8.0);

    public int ColumnCount
    {
        get => GetValue(ColumnCountProperty);
        set => SetValue(ColumnCountProperty, value);
    }

    public double MinColumnWidth
    {
        get => GetValue(MinColumnWidthProperty);
        set => SetValue(MinColumnWidthProperty, value);
    }

    public double ColumnSpacing
    {
        get => GetValue(ColumnSpacingProperty);
        set => SetValue(ColumnSpacingProperty, value);
    }

    private IReadOnlyList<FieldRow> _cachedRows = Array.Empty<FieldRow>();
    private double _cachedLayoutWidth = -1;

    private static int GetSpan(Control child) =>
        child.DataContext is FieldEditorViewModelBase vm
            ? Math.Max(1, vm.ColumnSpan)
            : 1;

    private IReadOnlyList<FieldRow> GetRows(double availableWidth)
    {
        int cols = FieldLayoutEngine.ComputeEffectiveCols(ColumnCount, availableWidth, MinColumnWidth);
        var fields = Children.Select((c, i) => (i, GetSpan(c)));
        return FieldLayoutEngine.PackRows(fields, cols);
    }

    private double ColumnWidth(double totalWidth, int cols) =>
        cols <= 1
            ? totalWidth
            : (totalWidth - ColumnSpacing * (cols - 1)) / cols;

    private double SlotX(int colStart, double colW) => colStart * (colW + ColumnSpacing);

    private double SlotWidth(int span, double colW) =>
        span == 1 ? colW : span * colW + (span - 1) * ColumnSpacing;

    protected override Size MeasureOverride(Size availableSize)
    {
        double w = double.IsInfinity(availableSize.Width)
            ? ColumnCount * (MinColumnWidth + ColumnSpacing) - ColumnSpacing
            : availableSize.Width;

        _cachedRows = GetRows(w);
        _cachedLayoutWidth = w;

        int cols = FieldLayoutEngine.ComputeEffectiveCols(ColumnCount, w, MinColumnWidth);
        double colW = ColumnWidth(w, cols);
        double totalH = 0;

        foreach (var row in _cachedRows)
        {
            double rowH = 0;
            foreach (var slot in row.Slots)
            {
                var child = Children[slot.FieldIndex];
                child.Measure(new Size(SlotWidth(slot.Span, colW), double.PositiveInfinity));
                rowH = Math.Max(rowH, child.DesiredSize.Height);
            }
            totalH += rowH;
        }

        return new Size(w, totalH);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var rows = Math.Abs(finalSize.Width - _cachedLayoutWidth) < 0.5
            ? _cachedRows
            : GetRows(finalSize.Width);

        int cols = FieldLayoutEngine.ComputeEffectiveCols(ColumnCount, finalSize.Width, MinColumnWidth);
        double colW = ColumnWidth(finalSize.Width, cols);
        double y = 0;

        foreach (var row in rows)
        {
            double rowH = row.Slots.Max(s => Children[s.FieldIndex].DesiredSize.Height);
            foreach (var slot in row.Slots)
                Children[slot.FieldIndex].Arrange(
                    new Rect(SlotX(slot.ColStart, colW), y, SlotWidth(slot.Span, colW), rowH));
            y += rowH;
        }

        return new Size(finalSize.Width, y);
    }
}

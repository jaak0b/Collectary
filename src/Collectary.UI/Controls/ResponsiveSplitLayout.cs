using System;
using Avalonia;
using Avalonia.Controls;
using Collectary.Presentation.Services;

namespace Collectary.UI.Controls;

public sealed class ResponsiveSplitLayout
{
    public const double NarrowThreshold = 720;
    private const double MasterMinWidth = 240;
    private const double DetailMinWidth = 300;

    private readonly Grid _splitGrid;
    private readonly Control _masterPane;
    private readonly Control _paneSplitter;
    private readonly Control _detailPane;

    private double _ratio = 0.4;
    private bool? _isNarrow;

    public ResponsiveSplitLayout(Grid splitGrid, Control masterPane, Control paneSplitter, Control detailPane)
    {
        _splitGrid = splitGrid;
        _masterPane = masterPane;
        _paneSplitter = paneSplitter;
        _detailPane = detailPane;
    }

    public void Attach(double width)
    {
        _ratio = Math.Clamp(AppPreferences.Load().FieldPaneRatio, 0.15, 0.85);
        Apply(width);
    }

    public void Detach()
    {
        CaptureRatio();
        AppPreferences.Update(p => p with { FieldPaneRatio = _ratio });
    }

    public void Apply(double width)
    {
        var narrow = width is > 0 and < NarrowThreshold;
        if (narrow == _isNarrow) return;

        if (narrow)
        {
            CaptureRatio();
            _splitGrid.ColumnDefinitions = new ColumnDefinitions("*");
            _splitGrid.RowDefinitions = new RowDefinitions("Auto,*");
            Grid.SetColumn(_masterPane, 0); Grid.SetRow(_masterPane, 0);
            Grid.SetColumn(_detailPane, 0); Grid.SetRow(_detailPane, 1);
            _detailPane.Margin = new Thickness(0, 12, 0, 0);
            _paneSplitter.IsVisible = false;
        }
        else
        {
            _splitGrid.RowDefinitions = new RowDefinitions("*");
            _splitGrid.ColumnDefinitions = new ColumnDefinitions
            {
                new ColumnDefinition(new GridLength(_ratio, GridUnitType.Star)) { MinWidth = MasterMinWidth },
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(new GridLength(1 - _ratio, GridUnitType.Star)) { MinWidth = DetailMinWidth }
            };
            Grid.SetColumn(_masterPane, 0); Grid.SetRow(_masterPane, 0);
            Grid.SetColumn(_paneSplitter, 1); Grid.SetRow(_paneSplitter, 0);
            Grid.SetColumn(_detailPane, 2); Grid.SetRow(_detailPane, 0);
            _detailPane.Margin = new Thickness(0);
            _paneSplitter.IsVisible = true;
        }

        _isNarrow = narrow;
    }

    private void CaptureRatio()
    {
        if (_isNarrow != false) return;
        var cols = _splitGrid.ColumnDefinitions;
        if (cols.Count < 3) return;
        var first = cols[0].Width.Value;
        var second = cols[2].Width.Value;
        var total = first + second;
        if (total > 0)
            _ratio = Math.Clamp(first / total, 0.15, 0.85);
    }
}

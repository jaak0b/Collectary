using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;
using Collectary.Presentation.ViewModels.Import;
using Collectary.UI.Controls;

namespace Collectary.UI.Views.Import;

public partial class ExcelImportView : UserControl
{
    public ExcelImportView()
    {
        InitializeComponent();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (DataContext is ExcelImportViewModel vm)
            vm.IsNarrow = e.NewSize.Width is > 0 and < ResponsiveSplitLayout.NarrowThreshold;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is not ExcelImportViewModel vm) return;

        BuildPreviewColumns(vm);
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ExcelImportViewModel.ColumnHeaders))
                Dispatcher.UIThread.InvokeAsync(() => BuildPreviewColumns(vm));
        };
    }

    private void BuildPreviewColumns(ExcelImportViewModel vm)
    {
        PreviewGrid.Columns.Clear();
        for (var i = 0; i < vm.ColumnHeaders.Count; i++)
            PreviewGrid.Columns.Add(new DataGridTextColumn
            {
                Header = vm.ColumnHeaders[i],
                Binding = new Binding($"[{i}]"),
                Width = DataGridLength.Auto
            });
    }
}

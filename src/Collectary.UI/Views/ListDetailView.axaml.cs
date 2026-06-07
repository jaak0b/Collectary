using Avalonia.Controls;
using Collectary.Presentation.Localization;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Views.Helpers;

namespace Collectary.UI.Views;

public partial class ListDetailView : UserControl
{
    public ListDetailView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is ListDetailViewModel vm)
        {
            BuildColumns(vm);

            LocalizationService.Instance.LanguageChanged += (_, _) =>
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => BuildColumns(vm));
        }
    }

    private void BuildColumns(ListDetailViewModel vm)
    {
        var loc = LocalizationService.Instance;
        EntryGrid.Columns.Clear();

        EntryGrid.Columns.Add(GridColumnFactory.ActionColumn<ListEntryRowViewModel>(new (string, Action<ListEntryRowViewModel>)[]
        {
            (loc["Edit"], row => vm.EditEntryCommand.Execute(row)),
            (loc["Delete"], row => vm.DeleteEntryCommand.Execute(row))
        }));
        EntryGrid.FrozenColumnCount = 1;

        EntryGrid.Columns.Add(new DataGridTextColumn
        {
            Header = loc["NumberSign"],
            Binding = new Avalonia.Data.Binding(nameof(ListEntryRowViewModel.EntryNumber)),
            Width = DataGridLength.Auto
        });

        var cellIndex = 0;
        foreach (var field in vm.ColumnFields)
            EntryGrid.Columns.Add(GridColumnFactory.ValueColumn<ListEntryRowViewModel>(field.Label, cellIndex++));

        GridColumnFactory.AttachRowContextMenu<ListEntryRowViewModel>(EntryGrid, new (string, Action<ListEntryRowViewModel>)[]
        {
            (loc["Edit"], row => vm.EditEntryCommand.Execute(row)),
            (loc["Delete"], row => vm.DeleteEntryCommand.Execute(row))
        });
    }
}

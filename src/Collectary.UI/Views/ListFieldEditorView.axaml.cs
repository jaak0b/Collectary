using Avalonia.Controls;
using Collectary.UI.Localization;
using Collectary.UI.ViewModels;
using Collectary.UI.Views.Helpers;

namespace Collectary.UI.Views;

public partial class ListFieldEditorView : UserControl
{
    public ListFieldEditorView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is ListFieldEditorViewModel vm && vm.IsGridInline)
        {
            BuildGrid(vm);

            LocalizationService.Instance.LanguageChanged += (_, _) =>
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => BuildGrid(vm));
        }
    }

    private void BuildGrid(ListFieldEditorViewModel vm)
    {
        var loc = LocalizationService.Instance;
        EntryGrid.Columns.Clear();
        EntryGrid.Columns.Add(new DataGridTextColumn
        {
            Header = loc["NumberSign"],
            Binding = new Avalonia.Data.Binding(nameof(ListEntryRowViewModel.EntryNumber)),
            Width = DataGridLength.Auto
        });

        var cellIndex = 0;
        foreach (var field in vm.ColumnFields)
            EntryGrid.Columns.Add(GridColumnFactory.ValueColumn<ListEntryRowViewModel>(field.Label, cellIndex++));

        EntryGrid.Columns.Add(GridColumnFactory.ActionColumn<ListEntryRowViewModel>(new (string, Action<ListEntryRowViewModel>)[]
        {
            (loc["Edit"], row => vm.EditEntryCommand.Execute(row)),
            (loc["Delete"], row => vm.DeleteEntryCommand.Execute(row))
        }));

        GridColumnFactory.AttachRowContextMenu<ListEntryRowViewModel>(EntryGrid, new (string, Action<ListEntryRowViewModel>)[]
        {
            (loc["Edit"], row => vm.EditEntryCommand.Execute(row)),
            (loc["Delete"], row => vm.DeleteEntryCommand.Execute(row))
        });
    }
}

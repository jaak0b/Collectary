using Avalonia.Controls;
using Avalonia.Data;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Localization;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Views.Helpers;

namespace Collectary.UI.Views;

public partial class PresetDetailView : UserControl
{
    public PresetDetailView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is PresetDetailViewModel vm)
        {
            BuildColumns(vm);

            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(PresetDetailViewModel.ListColumns))
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => BuildColumns(vm));
            };

            LocalizationService.Instance.LanguageChanged += (_, _) =>
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => BuildColumns(vm));
        }
    }

    private void BuildColumns(PresetDetailViewModel vm)
    {
        var loc = LocalizationService.Instance;
        ItemGrid.Columns.Clear();

        GridColumnFactory.AttachRowContextMenu<ItemRowViewModel>(ItemGrid, new (string, Action<ItemRowViewModel>)[]
        {
            (loc["Edit"], row => vm.EditItemCommand.Execute(row)),
            (loc["Delete"], row => vm.DeleteItemCommand.Execute(row))
        });

        var cellIndex = 0;
        foreach (var column in vm.ListColumns)
        {
            if (column.Field is DisplayNameFieldDefinition)
            {
                ItemGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = loc["DisplayNameField"],
                    Binding = new Binding(nameof(ItemRowViewModel.DisplayName)),
                    Width = DataGridLength.Auto
                });
            }
            else
            {
                ItemGrid.Columns.Add(GridColumnFactory.ValueColumn<ItemRowViewModel>(column.Header, cellIndex++));
            }
        }

        ItemGrid.Columns.Add(GridColumnFactory.ActionColumn<ItemRowViewModel>(new (string, Action<ItemRowViewModel>)[]
        {
            (loc["Edit"], row => vm.EditItemCommand.Execute(row)),
            (loc["Delete"], row => vm.DeleteItemCommand.Execute(row))
        }));
    }
}

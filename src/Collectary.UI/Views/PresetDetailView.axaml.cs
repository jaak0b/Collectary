using Avalonia.Controls;
using Avalonia.Data;
using CommunityToolkit.Mvvm.Messaging;
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
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, static (recipient, _) =>
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                var view = (PresetDetailView)recipient;
                view.RebuildColumns();
                view.SearchBar.RefreshLocalization();
            }));
    }

    private void RebuildColumns()
    {
        if (DataContext is PresetDetailViewModel vm) BuildColumns(vm);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is PresetDetailViewModel vm)
        {
            BuildColumns(vm);

            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(PresetDetailViewModel.ListColumns)
                    or nameof(PresetDetailViewModel.ShowCollectionColumn))
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => BuildColumns(vm));
            };
        }
    }

    private void BuildColumns(PresetDetailViewModel vm)
    {
        var loc = LocalizationService.Instance;
        ItemGrid.Columns.Clear();

        if (vm.ShowCollectionColumn)
        {
            ItemGrid.Columns.Add(new DataGridTextColumn
            {
                Header = loc["CollectionColumn"],
                Binding = new Binding(nameof(ItemRowViewModel.CollectionName)),
                Width = DataGridLength.Auto
            });
        }

        GridColumnFactory.AttachRowContextMenu<ItemRowViewModel>(ItemGrid, new (string, Action<ItemRowViewModel>)[]
        {
            (loc["Edit"], row => vm.EditItemCommand.Execute(row)),
            (loc["Delete"], row => vm.DeleteItemCommand.Execute(row))
        });

        var cellIndex = 0;
        foreach (var column in vm.ListColumns)
        {
            if (column.Field.IsTitleField)
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

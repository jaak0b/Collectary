using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
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
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => ((PresetDetailView)recipient).RebuildColumns()));
        SearchBox.AddHandler(KeyDownEvent, OnSearchKeyDown, RoutingStrategies.Tunnel);
        SearchBox.TextChanged += OnSearchTextChanged;
        SearchBox.LostFocus += (_, _) => Query?.CloseSuggestionsCommand.Execute(null);
        SuggestionList.Tapped += async (_, _) => await AcceptSelectedSuggestionAsync();
    }

    private ItemQueryViewModel? Query => (DataContext as PresetDetailViewModel)?.Query;

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (Query is not { } query) return;
        switch (e.Key)
        {
            case Key.Down:
                query.MoveSelection(1);
                e.Handled = true;
                break;
            case Key.Up:
                query.MoveSelection(-1);
                e.Handled = true;
                break;
            case Key.Escape:
                query.CloseSuggestionsCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Tab:
                if (query.AreSuggestionsOpen && query.SelectedSuggestion is not null)
                {
                    _ = AcceptSelectedSuggestionAsync();
                    e.Handled = true;
                }
                break;
            case Key.Enter:
                if (query.AreSuggestionsOpen && query.SelectedSuggestion is not null)
                    _ = AcceptSelectedSuggestionAsync();
                else
                    query.RunCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (Query is not { } query || !SearchBox.IsKeyboardFocusWithin) return;
        query.CaretIndex = SearchBox.CaretIndex;
        query.RefreshSuggestionsCommand.Execute(null);
    }

    private void OnChipButtonLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: FilterChipViewModel { IsFlyoutOpen: true } chip } button) return;
        chip.IsFlyoutOpen = false;
        button.Flyout?.ShowAt(button);
    }

    private void OnAddableFieldSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox list || list.SelectedItem is not string label) return;
        list.SelectedItem = null;
        MoreButton.Flyout?.Hide();
        (DataContext as PresetDetailViewModel)?.BasicFilter.AddChipCommand.Execute(label);
    }

    private async Task AcceptSelectedSuggestionAsync()
    {
        if (Query is not { } query || query.SelectedSuggestion is null) return;
        await query.AcceptSuggestionCommand.ExecuteAsync(query.SelectedSuggestion);
        SearchBox.CaretIndex = query.CaretIndex;
        SearchBox.Focus();
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

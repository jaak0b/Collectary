using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Collectary.Search.Avalonia.ViewModels;

namespace Collectary.Search.Avalonia.Controls;

public partial class SearchBar : UserControl
{
    private const double UnconstrainedRowWidth = 100000;

    private readonly ResponsiveSearchBarLayout _responsiveLayout = new();
    private SearchBarViewModel? _viewModel;
    private bool _basicStacked;

    public SearchBar()
    {
        InitializeComponent();
        SearchBox.AddHandler(KeyDownEvent, OnSearchKeyDown, RoutingStrategies.Tunnel);
        SearchBox.TextChanged += OnSearchTextChanged;
        SearchBox.LostFocus += (_, _) => Query?.CloseSuggestionsCommand.Execute(null);
        SuggestionList.Tapped += async (_, _) => await AcceptSelectedSuggestionAsync();
        SizeChanged += (_, e) => ApplyResponsiveLayout(e.NewSize.Width);
        DataContextChanged += (_, _) => HookViewModel();
        AttachedToVisualTree += (_, _) => HookViewModel();
        DetachedFromVisualTree += (_, _) => UnhookViewModel();
    }

    internal void ApplyResponsiveLayout(double width)
    {
        AdvancedPanel.Classes.Set("narrow",
            _responsiveLayout.ShouldStack(width, NaturalRowWidth(SearchBox, AdvancedButtons)));
        _basicStacked = _responsiveLayout.ShouldStack(
            width, NaturalRowWidth(ItemsSearchBox, ChipArea, SortControls));
        BasicPanel.Classes.Set("narrow", _basicStacked);
        UpdateFilterCollapse();
    }

    private double NaturalRowWidth(params Control[] clusters)
    {
        var total = 0d;
        foreach (var cluster in clusters)
        {
            // Avalonia's WrapPanel reports a zero desired width when measured with an infinite
            // width, so constrain to a finite-but-unreachable width to get the real row width.
            cluster.Measure(new Size(UnconstrainedRowWidth, double.PositiveInfinity));
            total += cluster.DesiredSize.Width;
        }
        return total;
    }

    private void HookViewModel()
    {
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel = DataContext as SearchBarViewModel;
        if (_viewModel is not null)
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        UpdateFilterCollapse();
    }

    private void UnhookViewModel()
    {
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SearchBarViewModel.IsFilterPanelExpanded))
            UpdateFilterCollapse();
    }

    private void UpdateFilterCollapse() =>
        BasicPanel.Classes.Set("filters-collapsed", _basicStacked && _viewModel?.IsFilterPanelExpanded != true);

    public void RefreshLocalization() => (DataContext as SearchBarViewModel)?.RefreshLocalization();

    private ItemQueryViewModel? Query => (DataContext as SearchBarViewModel)?.Query;

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
        (DataContext as SearchBarViewModel)?.BasicFilter.AddChipCommand.Execute(label);
    }

    private async Task AcceptSelectedSuggestionAsync()
    {
        if (Query is not { } query || query.SelectedSuggestion is null) return;
        await query.AcceptSuggestionCommand.ExecuteAsync(query.SelectedSuggestion);
        SearchBox.CaretIndex = query.CaretIndex;
        SearchBox.Focus();
    }
}

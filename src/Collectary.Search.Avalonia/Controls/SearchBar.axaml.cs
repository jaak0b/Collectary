using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Collectary.Search.ViewModels;

namespace Collectary.Search.Avalonia.Controls;

public partial class SearchBar : UserControl
{
    private readonly ResponsiveSearchBarLayout _responsiveLayout = new();

    private const double AdvancedStackWidth = 740;

    public SearchBar()
    {
        InitializeComponent();
        SearchBox.AddHandler(KeyDownEvent, OnSearchKeyDown, RoutingStrategies.Tunnel);
        SearchBox.TextChanged += OnSearchTextChanged;
        SearchBox.LostFocus += (_, _) => Query?.CloseSuggestionsCommand.Execute(null);
        SuggestionList.Tapped += async (_, _) => await AcceptSelectedSuggestionAsync();
        SizeChanged += (_, e) => ApplyResponsiveLayout(e.NewSize.Width);
    }

    internal void ApplyResponsiveLayout(double width) =>
        AdvancedPanel.Classes.Set("narrow", _responsiveLayout.ShouldStack(width, AdvancedStackWidth));

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

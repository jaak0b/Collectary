using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Core.Search;
using Collectary.Presentation.DI;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;

namespace Collectary.Presentation.ViewModels;

public partial class PresetDetailViewModel : ViewModelBase
{
    private readonly IItemUseCase _itemUseCase;
    private readonly IPresetUseCase _presetUseCase;
    private readonly ISearchFieldCatalog _searchCatalog;
    private readonly IListCellBuilder _listCellBuilder;
    private readonly IDialogService _dialogService;
    private readonly Action<Preset, EffectiveFields, Item?> _navigateToItemEditor;
    private readonly Action _navigateBack;

    [ObservableProperty]
    public partial Preset Preset { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<ItemRowViewModel> ItemRows { get; set; } = new();

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool ShowCollectionColumn { get; set; }

    [ObservableProperty]
    public partial bool IsBasicMode { get; set; }

    public ItemQueryViewModel Query { get; }

    public BasicFilterViewModel BasicFilter { get; }

    public IReadOnlyList<ListColumn> ListColumns { get; private set; } = [];

    public PresetDetailViewModel(
        Preset preset,
        IItemUseCase itemUseCase,
        IPresetUseCase presetUseCase,
        IItemSearchService searchService,
        ISearchFieldCatalog searchCatalog,
        IListCellBuilder listCellBuilder,
        IDialogService dialogService,
        Action<Preset, EffectiveFields, Item?> navigateToItemEditor,
        Action navigateBack)
    {
        Preset = preset;
        _itemUseCase = itemUseCase;
        _presetUseCase = presetUseCase;
        _searchCatalog = searchCatalog;
        _listCellBuilder = listCellBuilder;
        _dialogService = dialogService;
        _navigateToItemEditor = navigateToItemEditor;
        _navigateBack = navigateBack;
        Query = new ItemQueryViewModel(
            searchService,
            searchCatalog,
            new QuerySuggestionEngine(new QueryLexer(), new PseudoFieldCatalog()),
            ApplyResultsAsync);
        BasicFilter = new BasicFilterViewModel(searchCatalog, RunQueryTextAsync);
    }

    public async Task LoadAsync()
    {
        try
        {
            ErrorMessage = null;
            var effectiveFields = await _presetUseCase.GetEffectiveFieldsAsync(Preset.Id);
            var columns = BuildListColumns(effectiveFields);
            ListColumns = columns;
            OnPropertyChanged(nameof(ListColumns));
            Query.ResetSnapshot();
            await BasicFilter.LoadAsync();
            var defaultQuery = DefaultQueryFor(Preset.Name);
            IsBasicMode = AppPreferences.Load().SearchBasicMode && BasicFilter.TryLoadFromText(defaultQuery);
            Query.QueryText = defaultQuery;
            await Query.RunCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Failed to load preset detail for preset {PresetId}", Preset.Id);
            ErrorMessage = LocalizationService.Instance["CouldNotLoad"];
        }
    }

    private string DefaultQueryFor(string presetName) =>
        "preset = \"" + presetName.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    private async Task RunQueryTextAsync(string text)
    {
        Query.QueryText = text;
        await Query.RunCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private void SwitchToAdvanced()
    {
        Query.QueryText = BasicFilter.ToQueryText();
        IsBasicMode = false;
        AppPreferences.Update(p => p with { SearchBasicMode = false });
    }

    [RelayCommand]
    private void SwitchToBasic()
    {
        if (!BasicFilter.TryLoadFromText(Query.QueryText))
        {
            Query.QueryMessage = LocalizationService.Instance["SearchTooComplexForBasic"];
            return;
        }
        IsBasicMode = true;
        Query.QueryMessage = null;
        AppPreferences.Update(p => p with { SearchBasicMode = true });
    }

    private async Task ApplyResultsAsync(ItemSearchResult result)
    {
        var listFields = ListColumns.Select(c => c.Field).ToList();
        var presetIds = result.Items.Select(i => i.PresetId).Distinct().ToList();
        var showCollection = presetIds.Count > 1 || (presetIds.Count == 1 && presetIds[0] != Preset.Id);
        var namesById = showCollection
            ? (await _searchCatalog.GetSnapshotAsync()).Presets
                .GroupBy(p => p.Id)
                .ToDictionary(g => g.Key, g => g.First().Name)
            : new Dictionary<Guid, string>();
        ShowCollectionColumn = showCollection;
        ItemRows = new ObservableCollection<ItemRowViewModel>(result.Items.Select(item =>
            new ItemRowViewModel(item, listFields, _listCellBuilder)
            {
                CollectionName = namesById.GetValueOrDefault(item.PresetId),
            }));
    }

    private List<ListColumn> BuildListColumns(EffectiveFields effective)
    {
        var groupById = effective.Groups.ToDictionary(g => g.Id);
        var groupsByParent = effective.Groups.ToLookup(g => g.ParentGroupId);
        var fieldsByGroup = effective.Fields.ToLookup(f => effective.GroupByFieldId.GetValueOrDefault(f.Id));
        var columns = new List<ListColumn>();

        void Walk(Guid? scope, IReadOnlyList<string> pathNames)
        {
            var fields = fieldsByGroup[scope].Select(f => (Order: f.DisplayOrder, Field: (FieldDefinition?)f, Group: (FieldGroup?)null));
            var groups = groupsByParent[scope].Select(g => (Order: g.DisplayOrder, Field: (FieldDefinition?)null, Group: (FieldGroup?)g));

            foreach (var entry in fields.Concat(groups).OrderBy(e => e.Order))
            {
                if (entry.Field is { } field)
                {
                    if (field is not IListDisplayable { ShowInList: true }) continue;
                    if (!field.IsTitleField && !_listCellBuilder.HasListCellViewModel(field.GetType())) continue;

                    var prefix = scope is { } s && groupById.TryGetValue(s, out var direct) && direct.PrefixColumnHeaders && pathNames.Count > 0
                        ? string.Join(" › ", pathNames) + " › "
                        : string.Empty;
                    columns.Add(new ListColumn(field, prefix + field.Label));
                }
                else if (entry.Group is { } group && group.ShowInList)
                {
                    Walk(group.Id, pathNames.Append(group.Name).ToList());
                }
            }
        }

        Walk(null, []);
        return columns;
    }

    [RelayCommand]
    private void Back() => _navigateBack();

    [RelayCommand]
    private async Task AddItemAsync()
    {
        var effectiveFields = await _presetUseCase.GetEffectiveFieldsAsync(Preset.Id);
        _navigateToItemEditor(Preset, effectiveFields, null);
    }

    [RelayCommand]
    private async Task EditItemAsync(ItemRowViewModel row)
    {
        var owningPreset = row.Item.PresetId == Preset.Id
            ? Preset
            : await _presetUseCase.GetPresetAsync(row.Item.PresetId);
        if (owningPreset is null) return;
        var effectiveFields = await _presetUseCase.GetEffectiveFieldsAsync(owningPreset.Id);
        _navigateToItemEditor(owningPreset, effectiveFields, row.Item);
    }

    [RelayCommand]
    private async Task DeleteItemAsync(ItemRowViewModel row)
    {
        if (!await _dialogService.ConfirmDeleteAsync(row.DisplayName)) return;
        await _itemUseCase.DeleteItemAsync(row.Item.Id);
        ItemRows.Remove(row);
    }
}

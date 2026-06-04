using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;

namespace Collectary.Presentation.ViewModels;

public partial class ItemEditorViewModel : ViewModelBase, IGroupedFieldHost
{
    private readonly IItemUseCase _itemUseCase;
    private readonly Preset _preset;
    private readonly ItemEditingContext _context;
    private Item? _existing;
    private readonly Action _onSaved;
    private readonly Action _onCancelled;

    [ObservableProperty]
    public partial string DisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<FieldEditorViewModelBase> FieldEditors { get; set; } = new();

    public int UngroupedColumnCount => _preset.ColumnCount;

    public ObservableCollection<FieldEditorViewModelBase> UngroupedEditors { get; private set; } = new();

    public ObservableCollection<ViewModelBase> LayoutRegions { get; private set; } = new();

    public bool IsNarrow { set => _context.IsNarrow = value; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public ItemEditorViewModel(
        IItemUseCase itemUseCase,
        IPresetUseCase presetUseCase,
        Preset preset,
        EffectiveFields effectiveFields,
        Action onSaved,
        Action onCancelled,
        ItemEditingContext context,
        Item? existing = null)
    {
        _itemUseCase = itemUseCase;
        _preset = preset;
        _context = context;
        _existing = existing;
        _onSaved = onSaved;
        _onCancelled = onCancelled;

        if (existing is not null)
            DisplayName = existing.DisplayName;

        foreach (var definition in effectiveFields.Fields)
        {
            if (definition is DisplayNameFieldDefinition dn)
            {
                FieldEditors.Add(new DisplayNameFieldEditorViewModel(dn, existing?.DisplayName ?? ""));
                continue;
            }

            var existingValue = existing?.Values.FirstOrDefault(v => v.FieldDefinitionId == definition.Id);
            var editor = context.EditorRegistry.Create(definition, existingValue, context);
            if (editor is not null)
                FieldEditors.Add(editor);
        }

        var layout = new FieldGroupLayout(
            FieldEditors, effectiveFields.Groups, effectiveFields.GroupByFieldId, context);
        AppLogger.Log.Debug("ItemEditor layout: groups={GroupCount} ungrouped={UngroupedCount} regions={RegionCount}",
            effectiveFields.Groups.Count, layout.UngroupedEditors.Count, layout.LayoutRegions.Count);
        foreach (var g in effectiveFields.Groups)
            AppLogger.Log.Debug("  Group id={Id} name={Name} parentGroupId={ParentGroupId}",
                g.Id, g.Name, g.ParentGroupId);
        foreach (var kv in effectiveFields.GroupByFieldId)
            AppLogger.Log.Debug("  Field {FieldId} -> groupId={GroupId}", kv.Key, kv.Value);
        UngroupedEditors = layout.UngroupedEditors;
        LayoutRegions = layout.LayoutRegions;
    }

    public async Task PersistAsync()
    {
        var item = _existing ?? new Item { PresetId = _preset.Id, CreatedAt = DateTime.UtcNow };
        var dnEditor = FieldEditors.OfType<DisplayNameFieldEditorViewModel>().FirstOrDefault();
        item.DisplayName = (dnEditor?.Text ?? DisplayName).Trim();
        item.Values = FieldEditors
            .Where(e => e is not DisplayNameFieldEditorViewModel)
            .Select(e => e.GetCurrentValue())
            .ToList();

        if (_existing is null)
        {
            await _itemUseCase.CreateItemAsync(item);
            _existing = item;
        }
        else
        {
            await _itemUseCase.UpdateItemAsync(item);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        try { await PersistAsync(); }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Failed to save item");
            ErrorMessage = LocalizationService.Instance["CouldNotSave"];
        }
    }

    [RelayCommand]
    private async Task SaveAndGoBackAsync()
    {
        ErrorMessage = null;
        try { await PersistAsync(); _onSaved(); }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Failed to save item");
            ErrorMessage = LocalizationService.Instance["CouldNotSave"];
        }
    }

    [RelayCommand]
    private void Cancel() => _onCancelled();

}

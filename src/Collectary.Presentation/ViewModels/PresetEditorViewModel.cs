using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels.SystemFields;

namespace Collectary.Presentation.ViewModels;

public partial class PresetEditorViewModel : FieldListEditorViewModel
{
    private readonly IPresetUseCase _presetUseCase;
    private readonly ISystemFieldUseCase _systemFieldUseCase;
    private readonly IDialogService _dialogService;
    private readonly Preset? _existing;
    private readonly Action _onSaved;
    private readonly Action _onCancelled;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int ColumnCount { get; set; } = 1;

    [ObservableProperty]
    public partial ObservableCollection<Preset> AvailableParents { get; set; } = new();

    [ObservableProperty]
    public partial Preset? SelectedParent { get; set; }

    private readonly ObservableCollection<IEditorNode> _rootRows = new();

    public ObservableCollection<SystemFieldRowViewModel> AvailableSystemFields { get; } = new();

    protected override int GetRootColumnCount() => ColumnCount;

    partial void OnColumnCountChanged(int value)
    {
        foreach (var field in _rootRows.OfType<FieldDefinitionRowViewModel>())
            field.SetParentColumnCount(value);
    }

    public PresetEditorViewModel(
        IPresetUseCase presetUseCase,
        ISystemFieldUseCase systemFieldUseCase,
        IDialogService dialogService,
        Action onSaved,
        Action onCancelled,
        Preset? existing = null)
    {
        _presetUseCase = presetUseCase;
        _systemFieldUseCase = systemFieldUseCase;
        _dialogService = dialogService;
        _existing = existing;
        _onSaved = onSaved;
        _onCancelled = onCancelled;

        var groupNodes = new List<FieldGroupRowViewModel>();
        var fieldRows = new List<FieldDefinitionRowViewModel>();

        if (existing is not null)
        {
            Name = existing.Name;
            ColumnCount = existing.ColumnCount;
            foreach (var g in existing.Groups)
                groupNodes.Add(new FieldGroupRowViewModel(g));

            foreach (var f in existing.Fields.Where(f => f.ParentListFieldDefinitionId == null))
                fieldRows.Add(new FieldDefinitionRowViewModel(f));
            foreach (var r in existing.SystemFieldRefs)
            {
                var row = new FieldDefinitionRowViewModel(r.SystemField.Definition, isSystemField: true)
                {
                    AssignedGroupId = r.GroupId,
                    DisplayOrder = r.DisplayOrder
                };
                fieldRows.Add(row);
            }
        }

        if (fieldRows.All(f => !f.IsDisplayName))
            fieldRows.Insert(0, new FieldDefinitionRowViewModel(
                new DisplayNameFieldDefinition { IsRequired = true, ShowInList = true, DisplayOrder = -1 }));

        var tree = new EditorNodeTreeBuilder().Build(groupNodes, fieldRows);
        foreach (var node in tree) _rootRows.Add(node);
        foreach (var root in groupNodes.Where(g => g.ParentGroupId is null))
            root.ApplyListGate(true);
        foreach (var group in groupNodes)
            group.RefreshChildColumnSpans();

        foreach (var field in _rootRows.OfType<FieldDefinitionRowViewModel>())
            field.SetParentColumnCount(ColumnCount);

        InitRoot(LocalizationService.Instance["Collection"], _rootRows, supportsGroups: true);
    }

    public async Task LoadAsync()
    {
        try
        {
            var all = await _presetUseCase.GetAllPresetsAsync();
            var eligible = _existing is null
                ? all
                : all.Where(p => p.Id != _existing.Id).ToList();

            AvailableParents = new ObservableCollection<Preset>(eligible);

            if (_existing?.ParentPresetId is { } parentId)
                SelectedParent = AvailableParents.FirstOrDefault(p => p.Id == parentId);
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Failed to load available parent presets");
            await _dialogService.ShowMessageAsync(LocalizationService.Instance["CouldNotLoad"], LocalizationService.Instance["CouldNotLoad"]);
        }

        try
        {
            var systemFields = await _systemFieldUseCase.GetAllAsync();
            AvailableSystemFields.Clear();
            foreach (var sf in systemFields)
                AvailableSystemFields.Add(
                    new SystemFieldRowViewModel(sf) { AddToCollectionCommand = AddSystemFieldCommand });
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Failed to load system fields");
            await _dialogService.ShowMessageAsync(LocalizationService.Instance["CouldNotLoad"], LocalizationService.Instance["CouldNotLoad"]);
        }
    }

    [RelayCommand]
    private void AddSystemField(SystemFieldRowViewModel sfRow)
    {
        if (CurrentRows.OfType<FieldDefinitionRowViewModel>()
            .Any(r => r.IsSystemField && r.SystemFieldOwnerId == sfRow.SystemField.Id))
            return;
        var row = new FieldDefinitionRowViewModel(sfRow.SystemField.Definition, isSystemField: true)
        {
            DisplayOrder = CurrentRows.Count
        };
        CurrentRows.Add(row);
        PopulateCurrentLevelGroups();
        SelectedNode = row;
    }

    private async Task<bool> PersistAsync()
    {
        var preset = _existing ?? new Preset { CreatedAt = DateTime.UtcNow };
        preset.Name = Name.Trim();
        preset.ColumnCount = ColumnCount;
        preset.ParentPresetId = SelectedParent?.Id;

        var flat = new EditorNodeTreeBuilder().Flatten(_rootRows);

        preset.Groups = flat.Groups
            .Select(g =>
            {
                var built = g.Build(g.DisplayOrder);
                built.ParentListFieldDefinitionId = null;
                built.PresetId = preset.Id;
                return built;
            })
            .ToList();

        preset.Fields = flat.Fields
            .Where(row => !row.IsSystemField)
            .Select(row =>
            {
                var def = row.BuildDefinition();
                def.DisplayOrder = row.DisplayOrder;
                def.PresetId = preset.Id;
                return def;
            })
            .ToList();

        preset.SystemFieldRefs = flat.Fields
            .Where(row => row.IsSystemField && row.SystemFieldOwnerId.HasValue)
            .Select(row => new PresetSystemField
            {
                PresetId = preset.Id,
                SystemFieldId = row.SystemFieldOwnerId!.Value,
                GroupId = row.AssignedGroupId,
                DisplayOrder = row.DisplayOrder
            })
            .ToList();

        AppLogger.Log.Debug("Persisting preset {Name}: groups={Groups} fields={Fields} systemRefs={Refs}",
            preset.Name, preset.Groups.Count, preset.Fields.Count, preset.SystemFieldRefs.Count);
        foreach (var g in preset.Groups)
            AppLogger.Log.Debug("  group id={Id} name={Name} parent={Parent} mode={Mode}",
                g.Id, g.Name, g.ParentGroupId, g.DisplayMode);
        foreach (var f in preset.Fields)
            AppLogger.Log.Debug("  field id={Id} label={Label} groupId={GroupId}", f.Id, f.Label, f.GroupId);

        try
        {
            if (_existing is null)
                await _presetUseCase.CreatePresetAsync(preset);
            else
                await _presetUseCase.UpdatePresetAsync(preset);
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Failed to save preset");
            await _dialogService.ShowMessageAsync(LocalizationService.Instance["CouldNotSave"], LocalizationService.Instance["CouldNotSave"]);
            return false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await PersistAsync();
    }

    [RelayCommand]
    private async Task SaveAndGoBackAsync()
    {
        if (await PersistAsync())
            _onSaved();
    }

    [RelayCommand]
    private void Cancel() => _onCancelled();
}

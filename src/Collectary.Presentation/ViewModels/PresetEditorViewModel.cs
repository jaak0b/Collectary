using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels.SharedFields;

namespace Collectary.Presentation.ViewModels;

public partial class PresetEditorViewModel : FieldListEditorViewModel, ISystemBackHandler
{
    private readonly IPresetUseCase _presetUseCase;
    private readonly ISharedFieldUseCase _sharedFieldUseCase;
    private readonly IDialogService _dialogService;
    private readonly Mapping.IFieldEditorMapper _mapper;
    private readonly Preset? _existing;
    private Preset? _createdPreset;
    private Preset? ExistingOrCreated => _existing ?? _createdPreset;
    private readonly Action _onSaved;
    private readonly Action _onCancelled;

    public Action? OnAnySuccessfulSave { get; set; }

    public bool IsHeaderVisible => !IsNested && (!IsNarrow || SelectedNode == null);

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(IsNested) or nameof(IsNarrow) or nameof(SelectedNode))
            OnPropertyChanged(nameof(IsHeaderVisible));
    }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int ColumnCount { get; set; } = 1;

    public IReadOnlyList<FieldLabelLayoutOption> FieldLabelLayoutOptions { get; } =
    [
        new(null, LocalizationService.Instance["FieldLabel_Inherit"]),
        new(FieldLabelLayout.Beside, LocalizationService.Instance["FieldLabel_Beside"]),
        new(FieldLabelLayout.Above, LocalizationService.Instance["FieldLabel_Above"]),
        new(FieldLabelLayout.Adaptive, LocalizationService.Instance["FieldLabel_Adaptive"]),
    ];

    [ObservableProperty]
    public partial FieldLabelLayoutOption SelectedFieldLabelLayout { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<Preset> AvailableParents { get; set; } = new();

    [ObservableProperty]
    public partial Preset? SelectedParent { get; set; }

    private readonly ObservableCollection<IEditorNode> _rootRows = new();

    public ObservableCollection<SharedFieldRowViewModel> AvailableSharedFields { get; } = new();

    protected override int GetRootColumnCount() => ColumnCount;

    partial void OnColumnCountChanged(int value)
    {
        foreach (var field in _rootRows.OfType<FieldDefinitionRowViewModel>())
            field.SetParentColumnCount(value);
    }

    public PresetEditorViewModel(
        IPresetUseCase presetUseCase,
        ISharedFieldUseCase sharedFieldUseCase,
        IDialogService dialogService,
        Mapping.IFieldEditorMapper mapper,
        Action onSaved,
        Action onCancelled,
        Preset? existing = null,
        Preset? seed = null)
    {
        _presetUseCase = presetUseCase;
        _sharedFieldUseCase = sharedFieldUseCase;
        _dialogService = dialogService;
        _mapper = mapper;
        _existing = existing;
        _onSaved = onSaved;
        _onCancelled = onCancelled;

        var groupNodes = new List<FieldGroupRowViewModel>();
        var fieldRows = new List<FieldDefinitionRowViewModel>();

        var source = existing ?? seed;
        if (source is not null)
        {
            Name = source.Name;
            ColumnCount = source.ColumnCount;
            foreach (var g in source.Groups)
                groupNodes.Add(new FieldGroupRowViewModel(g));

            foreach (var f in source.Fields.Where(f => f.ParentListFieldDefinitionId == null))
                fieldRows.Add(new FieldDefinitionRowViewModel(f));
            foreach (var r in source.SharedFieldRefs)
            {
                var row = new FieldDefinitionRowViewModel(r.SharedField.Definition, isSharedField: true)
                {
                    AssignedGroupId = r.GroupId,
                    DisplayOrder = r.DisplayOrder
                };
                fieldRows.Add(row);
            }
        }

        SelectedFieldLabelLayout = FieldLabelLayoutOptions.First(o => o.Value == source?.FieldLabelLayout);

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
            var sharedFields = await _sharedFieldUseCase.GetAllAsync();
            AvailableSharedFields.Clear();
            foreach (var sf in sharedFields)
                AvailableSharedFields.Add(
                    new SharedFieldRowViewModel(sf) { AddToCollectionCommand = AddSharedFieldCommand });
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Failed to load system fields");
            await _dialogService.ShowMessageAsync(LocalizationService.Instance["CouldNotLoad"], LocalizationService.Instance["CouldNotLoad"]);
        }
    }

    [RelayCommand]
    private void AddSharedField(SharedFieldRowViewModel sfRow)
    {
        if (CurrentRows.OfType<FieldDefinitionRowViewModel>()
            .Any(r => r.IsSharedField && r.SharedFieldOwnerId == sfRow.SharedField.Id))
            return;
        var row = new FieldDefinitionRowViewModel(sfRow.SharedField.Definition, isSharedField: true)
        {
            DisplayOrder = CurrentRows.Count
        };
        CurrentRows.Add(row);
        PopulateCurrentLevelGroups();
        SelectedNode = row;
    }

    private async Task<bool> PersistAsync()
    {
        var preset = ExistingOrCreated ?? new Preset { CreatedAt = DateTime.UtcNow };
        preset.Name = Name.Trim();
        preset.ColumnCount = ColumnCount;
        preset.FieldLabelLayout = SelectedFieldLabelLayout?.Value;
        preset.ParentPresetId = SelectedParent?.Id;

        var flat = new EditorNodeTreeBuilder().Flatten(_rootRows);

        preset.Groups = flat.Groups
            .Select(g => _mapper.ToGroup(g, presetId: preset.Id, parentListFieldDefinitionId: null))
            .ToList();

        preset.Fields = flat.Fields
            .Where(row => !row.IsSharedField)
            .Select(row =>
            {
                var def = _mapper.ToDefinition(row);
                def.DisplayOrder = row.DisplayOrder;
                def.PresetId = preset.Id;
                return def;
            })
            .ToList();

        preset.SharedFieldRefs = flat.Fields
            .Where(row => row.IsSharedField && row.SharedFieldOwnerId.HasValue)
            .Select(row => new PresetSharedField
            {
                PresetId = preset.Id,
                SharedFieldId = row.SharedFieldOwnerId!.Value,
                GroupId = row.AssignedGroupId,
                DisplayOrder = row.DisplayOrder
            })
            .ToList();

        AppLogger.Log.Debug("Persisting preset {Name}: groups={Groups} fields={Fields} systemRefs={Refs}",
            preset.Name, preset.Groups.Count, preset.Fields.Count, preset.SharedFieldRefs.Count);
        foreach (var g in preset.Groups)
            AppLogger.Log.Debug("  group id={Id} name={Name} parent={Parent} mode={Mode}",
                g.Id, g.Name, g.ParentGroupId, g.DisplayMode);
        foreach (var f in preset.Fields)
            AppLogger.Log.Debug("  field id={Id} label={Label} groupId={GroupId}", f.Id, f.Label, f.GroupId);

        try
        {
            if (ExistingOrCreated is null)
            {
                await _presetUseCase.CreatePresetAsync(preset);
                _createdPreset = preset;
            }
            else
                await _presetUseCase.UpdatePresetAsync(preset);
            OnAnySuccessfulSave?.Invoke();
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
    private async Task BackAsync()
    {
        if (!await PersistAsync()) return;
        if (IsNarrow && SelectedNode is not null) { SelectedNode = null; return; }
        if (NavigateUpOneLevel()) return;
        _onSaved();
    }

    [RelayCommand]
    private void Cancel() => _onCancelled();

    public async Task<bool> HandleSystemBackAsync()
    {
        await BackCommand.ExecuteAsync(null);
        return true;
    }
}

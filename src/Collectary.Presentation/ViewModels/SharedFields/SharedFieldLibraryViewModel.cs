using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;

namespace Collectary.Presentation.ViewModels.SharedFields;

public partial class SharedFieldLibraryViewModel : FieldListEditorViewModel, ISystemBackHandler
{
    private readonly ISharedFieldUseCase _useCase;
    private readonly IDialogService _dialogService;
    private readonly Mapping.IFieldEditorMapper _mapper;
    private readonly Action _onDone;

    private readonly ObservableCollection<IEditorNode> _rootRows = new();
    private readonly Dictionary<Guid, SharedField> _sharedFieldsById = new();

    public SharedFieldLibraryViewModel(ISharedFieldUseCase useCase, IDialogService dialogService, Mapping.IFieldEditorMapper mapper, Action onDone)
    {
        _useCase = useCase;
        _dialogService = dialogService;
        _mapper = mapper;
        _onDone = onDone;
        InitRoot(LocalizationService.Instance["SharedFields"], _rootRows, supportsGroups: false);
    }

    public async Task LoadAsync()
    {
        try
        {
            var all = await _useCase.GetAllAsync();
            _rootRows.Clear();
            _sharedFieldsById.Clear();
            foreach (var sf in all)
            {
                _sharedFieldsById[sf.Id] = sf;
                _rootRows.Add(new FieldDefinitionRowViewModel(sf.Definition));
            }
            RefreshCurrentLevel();
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Failed to load system fields");
            await _dialogService.ShowMessageAsync(LocalizationService.Instance["CouldNotLoad"], LocalizationService.Instance["CouldNotLoad"]);
        }
    }

    protected override async Task AddField(FieldDefinition definition)
    {
        if (Levels.Count > 1)
        {
            await base.AddField(definition);
            return;
        }

        var sharedField = new SharedField { Name = definition.Label, Definition = definition };
        sharedField.Definition.SharedFieldId = sharedField.Id;
        try
        {
            await _useCase.CreateAsync(sharedField);
            _sharedFieldsById[sharedField.Id] = sharedField;
            var row = new FieldDefinitionRowViewModel(sharedField.Definition) { DisplayOrder = CurrentRows.Count };
            CurrentRows.Add(row);
            SelectedNode = row;
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Failed to create system field");
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            foreach (var rootRow in _rootRows.OfType<FieldDefinitionRowViewModel>())
            {
                if (rootRow.SharedFieldOwnerId is not { } sfId) continue;
                if (!_sharedFieldsById.TryGetValue(sfId, out var sharedField)) continue;
                var def = _mapper.ToDefinition(rootRow);
                sharedField.Name = def.Label;
                sharedField.Definition = def;
                await _useCase.UpdateAsync(sharedField);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Failed to save system fields");
            await _dialogService.ShowMessageAsync(LocalizationService.Instance["CouldNotSave"], LocalizationService.Instance["CouldNotSave"]);
        }
    }

    protected override async Task RemoveField(IEditorNode node)
    {
        if (Levels.Count > 1)
        {
            await base.RemoveField(node);
            return;
        }

        if (node is not FieldDefinitionRowViewModel row || row.SharedFieldOwnerId is not { } sfId) return;
        try
        {
            await _useCase.DeleteAsync(sfId);
            _sharedFieldsById.Remove(sfId);
            if (ReferenceEquals(SelectedNode, row)) SelectedNode = null;
            CurrentRows.Remove(row);
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Failed to delete system field");
        }
    }

    public async Task ReorderAsync(int from, int to)
    {
        MoveField(from, to);
        await CommitReorderAsync();
    }

    public async Task CommitReorderAsync()
    {
        if (IsNested) return;

        try
        {
            var orderedIds = CurrentRows
                .OfType<FieldDefinitionRowViewModel>()
                .Select(r => r.SharedFieldOwnerId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();
            await _useCase.ReorderAsync(orderedIds);
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Failed to reorder system fields");
            await _dialogService.ShowMessageAsync(LocalizationService.Instance["CouldNotSave"], LocalizationService.Instance["CouldNotSave"]);
        }
    }

    [RelayCommand]
    private async Task SaveAndGoBackAsync()
    {
        await SaveAsync();
        if (NavigateUpOneLevel()) return;
        _onDone();
    }

    [RelayCommand]
    private void Cancel() => _onDone();

    public async Task<bool> HandleSystemBackAsync()
    {
        await SaveAndGoBackCommand.ExecuteAsync(null);
        return true;
    }
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;

namespace Collectary.Presentation.ViewModels.SystemFields;

public partial class SystemFieldLibraryViewModel : FieldListEditorViewModel
{
    private readonly ISystemFieldUseCase _useCase;
    private readonly IDialogService _dialogService;
    private readonly Action _onDone;

    private readonly ObservableCollection<IEditorNode> _rootRows = new();
    private readonly Dictionary<Guid, SystemField> _systemFieldsById = new();

    public SystemFieldLibraryViewModel(ISystemFieldUseCase useCase, IDialogService dialogService, Action onDone)
    {
        _useCase = useCase;
        _dialogService = dialogService;
        _onDone = onDone;
        InitRoot(LocalizationService.Instance["SystemFields"], _rootRows, supportsGroups: false);
    }

    public async Task LoadAsync()
    {
        try
        {
            var all = await _useCase.GetAllAsync();
            _rootRows.Clear();
            _systemFieldsById.Clear();
            foreach (var sf in all)
            {
                _systemFieldsById[sf.Id] = sf;
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

        var systemField = new SystemField { Name = definition.Label, Definition = definition };
        systemField.Definition.SystemFieldId = systemField.Id;
        try
        {
            await _useCase.CreateAsync(systemField);
            _systemFieldsById[systemField.Id] = systemField;
            var row = new FieldDefinitionRowViewModel(systemField.Definition) { DisplayOrder = CurrentRows.Count };
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
                if (rootRow.SystemFieldOwnerId is not { } sfId) continue;
                if (!_systemFieldsById.TryGetValue(sfId, out var systemField)) continue;
                var def = rootRow.BuildDefinition();
                systemField.Name = def.Label;
                systemField.Definition = def;
                await _useCase.UpdateAsync(systemField);
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

        if (node is not FieldDefinitionRowViewModel row || row.SystemFieldOwnerId is not { } sfId) return;
        try
        {
            await _useCase.DeleteAsync(sfId);
            _systemFieldsById.Remove(sfId);
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

        if (IsNested) return;

        try
        {
            var orderedIds = CurrentRows
                .OfType<FieldDefinitionRowViewModel>()
                .Select(r => r.SystemFieldOwnerId)
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
        _onDone();
    }

    [RelayCommand]
    private void Cancel() => _onDone();
}

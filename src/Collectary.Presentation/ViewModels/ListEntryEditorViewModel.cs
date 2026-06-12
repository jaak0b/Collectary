using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;

namespace Collectary.Presentation.ViewModels;

public partial class ListEntryEditorViewModel : ViewModelBase, IGroupedFieldHost, ISystemBackHandler
{
    private readonly ListEntry _entry;
    private readonly ItemEditingContext _context;
    private readonly int _ungroupedColumnCount;

    public Guid EntryId => _entry.Id;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EntryLabel))]
    public partial int EntryNumber { get; set; }

    public string EntryLabel => $"{LocalizationService.Instance["Entry"]} {EntryNumber}";

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public ObservableCollection<FieldEditorViewModelBase> FieldEditors { get; } = new();

    public int UngroupedColumnCount => _ungroupedColumnCount;

    public double FieldMinColumnWidth => _context.FieldMinColumnWidth;

    public ObservableCollection<FieldEditorViewModelBase> UngroupedEditors { get; } = new();

    public ObservableCollection<ViewModelBase> LayoutRegions { get; } = new();

    public bool IsNarrow { set => _context.IsNarrow = value; }

    public ListEntryEditorViewModel(
        ListFieldDefinition definition,
        ListEntry entry,
        int entryNumber,
        ItemEditingContext context)
    {
        _entry = entry;
        EntryNumber = entryNumber;
        _context = context;
        _ungroupedColumnCount = definition.ColumnCount;

        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, static (recipient, _) =>
            ((ListEntryEditorViewModel)recipient).OnPropertyChanged(nameof(EntryLabel)));

        var groupByFieldId = new Dictionary<Guid, Guid?>();
        foreach (var subDef in definition.SubFields.OrderBy(f => f.DisplayOrder))
        {
            var existingValue = entry.SubValues.FirstOrDefault(v => v.FieldDefinitionId == subDef.Id);
            var editor = context.EditorRegistry.Create(subDef, existingValue, context);
            if (editor is not null)
            {
                editor.LabelAbove = context.LabelAbove;
                FieldEditors.Add(editor);
                groupByFieldId[subDef.Id] = subDef.GroupId;
            }
        }

        var layout = new FieldGroupLayout(FieldEditors, definition.Groups, groupByFieldId, context);
        AppLogger.Log.Debug("ListEntry layout: groups={GroupCount} ungrouped={UngroupedCount} regions={RegionCount}",
            definition.Groups.Count, layout.UngroupedEditors.Count, layout.LayoutRegions.Count);
        foreach (var g in definition.Groups)
            AppLogger.Log.Debug("  Group id={Id} name={Name} parentGroupId={ParentGroupId} parentListId={ParentListId}",
                g.Id, g.Name, g.ParentGroupId, g.ParentListFieldDefinitionId);
        foreach (var editor in layout.UngroupedEditors) UngroupedEditors.Add(editor);
        foreach (var region in layout.LayoutRegions) LayoutRegions.Add(region);
    }

    public List<FieldValue> CollectValues() =>
        FieldEditors.Select(e => e.GetCurrentValue()).ToList();

    private async Task<bool> TrySaveAsync()
    {
        ErrorMessage = null;
        var error = await new FieldEditorGate().AwaitReadyAndValidateAsync(FieldEditors);
        if (error is not null) { ErrorMessage = error; return false; }
        try { await _context.SaveAsync(); return true; }
        catch (FieldValidationException ex) { ErrorMessage = ex.Message; return false; }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Failed to save list entry");
            ErrorMessage = LocalizationService.Instance["CouldNotSave"];
            return false;
        }
    }

    [RelayCommand]
    private async Task Save() => await TrySaveAsync();

    [RelayCommand]
    private async Task Back() { if (await TrySaveAsync()) _context.GoBack(); }

    [RelayCommand]
    private void GoBack() => _context.GoBack();

    public async Task<bool> HandleSystemBackAsync()
    {
        await BackCommand.ExecuteAsync(null);
        return true;
    }
}

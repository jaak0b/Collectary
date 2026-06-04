using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;

namespace Collectary.Presentation.ViewModels;

public partial class ListEntryEditorViewModel : ViewModelBase, IGroupedFieldHost
{
    private readonly ListEntry _entry;
    private readonly ItemEditingContext _context;
    private readonly int _ungroupedColumnCount;

    public Guid EntryId => _entry.Id;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EntryLabel))]
    public partial int EntryNumber { get; set; }

    public string EntryLabel => $"{LocalizationService.Instance["Entry"]} {EntryNumber}";

    public ObservableCollection<FieldEditorViewModelBase> FieldEditors { get; } = new();

    public int UngroupedColumnCount => _ungroupedColumnCount;

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

        LocalizationService.Instance.LanguageChanged += (_, _) => OnPropertyChanged(nameof(EntryLabel));

        var groupByFieldId = new Dictionary<Guid, Guid?>();
        foreach (var subDef in definition.SubFields.OrderBy(f => f.DisplayOrder))
        {
            var existingValue = entry.SubValues.FirstOrDefault(v => v.FieldDefinitionId == subDef.Id);
            var editor = context.EditorRegistry.Create(subDef, existingValue, context);
            if (editor is not null)
            {
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

    [RelayCommand]
    private async Task Save() => await _context.SaveAsync();

    [RelayCommand]
    private async Task SaveAndGoBack() { await _context.SaveAsync(); _context.GoBack(); }

    [RelayCommand]
    private void GoBack() => _context.GoBack();
}

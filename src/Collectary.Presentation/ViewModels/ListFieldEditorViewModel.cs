using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.DI;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;

namespace Collectary.Presentation.ViewModels;

public partial class ListFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly ListFieldDefinition _definition;
    private readonly ListFieldValue _value;
    private readonly ItemEditingContext _context;

    public string Label => _definition.Label;
    public bool IsRequired => _definition.IsRequired;
    public ListInlineStyle InlineStyle => _definition.InlineStyle;
    public bool IsGridInline => _definition.InlineStyle == ListInlineStyle.Grid;
    public bool IsCardInline => _definition.InlineStyle == ListInlineStyle.Card;

    public IReadOnlyList<FieldDefinition> ColumnFields { get; }

    public ObservableCollection<ListEntryEditorViewModel> Entries { get; } = new();
    public ObservableCollection<ListEntryRowViewModel> EntryRows { get; } = new();

    public int EntryCount => Entries.Count;

    public ListFieldEditorViewModel(ListFieldDefinition definition, ListFieldValue value, ItemEditingContext context)
    {
        _definition = definition;
        _value = value;
        _context = context;

        ColumnFields = definition.SubFields
            .OrderBy(f => f.DisplayOrder)
            .Where(f => f is IListDisplayable { ShowInList: true } && context.ListCellBuilder.HasListCellViewModel(f.GetType()))
            .ToList();

        var i = 1;
        foreach (var entry in value.Entries.OrderBy(e => e.DisplayOrder))
            Entries.Add(new ListEntryEditorViewModel(definition, entry, i++, context));

        RebuildRows();
    }

    private void RebuildRows()
    {
        EntryRows.Clear();
        foreach (var entry in Entries)
            EntryRows.Add(new ListEntryRowViewModel(entry, ColumnFields, _context.ListCellBuilder));
        OnPropertyChanged(nameof(EntryCount));
    }

    [RelayCommand]
    private void Open() => _context.OpenList(this);

    [RelayCommand]
    private void AddEntry()
    {
        var entry = new ListEntry { ListFieldValueId = _value.Id };
        var vm = new ListEntryEditorViewModel(_definition, entry, Entries.Count + 1, _context);
        Entries.Add(vm);
        RebuildRows();
        _context.OpenEntry(vm, vm.EntryLabel);
    }

    [RelayCommand]
    private void EditEntry(ListEntryRowViewModel row) => _context.OpenEntry(row.Entry, row.EntryLabel);

    [RelayCommand]
    private void DeleteEntry(ListEntryRowViewModel row)
    {
        Entries.Remove(row.Entry);
        RenumberEntries();
        RebuildRows();
    }

    [RelayCommand]
    private async Task Save()
    {
        try { await _context.SaveAsync(); }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Failed to save item");
            await _context.Dialogs.ShowMessageAsync(LocalizationService.Instance["CouldNotSave"], LocalizationService.Instance["CouldNotSave"]);
        }
    }

    [RelayCommand]
    private async Task SaveAndGoBack()
    {
        try { await _context.SaveAsync(); }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Failed to save item");
            await _context.Dialogs.ShowMessageAsync(LocalizationService.Instance["CouldNotSave"], LocalizationService.Instance["CouldNotSave"]);
            return;
        }
        _context.GoBack();
    }

    [RelayCommand]
    private void GoBack() => _context.GoBack();

    private void RenumberEntries()
    {
        for (var i = 0; i < Entries.Count; i++)
            Entries[i].EntryNumber = i + 1;
    }

    public override FieldDefinition Definition => _definition;

    public override void Randomize(Services.ISampleData data)
    {
        var count = data.Int(2, 3);
        for (var n = 0; n < count; n++)
        {
            var entry = new ListEntry { ListFieldValueId = _value.Id };
            var vm = new ListEntryEditorViewModel(_definition, entry, Entries.Count + 1, _context);
            foreach (var sub in vm.FieldEditors)
                sub.Randomize(data);
            Entries.Add(vm);
        }
        RebuildRows();
    }

    public override FieldValue GetCurrentValue()
    {
        _value.Entries = Entries
            .Select((e, i) => new ListEntry
            {
                Id = e.EntryId,
                ListFieldValueId = _value.Id,
                DisplayOrder = i,
                SubValues = e.CollectValues()
            })
            .ToList();
        foreach (var entry in _value.Entries)
            foreach (var sv in entry.SubValues)
                sv.ListEntryId = entry.Id;
        return _value;
    }
}

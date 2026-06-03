using Collectary.Core.Domain;
using Collectary.UI.DI;
using Collectary.UI.ViewModels.ListCells;

namespace Collectary.UI.ViewModels;

public class ListEntryRowViewModel
{
    public ListEntryEditorViewModel Entry { get; }
    public int EntryNumber => Entry.EntryNumber;
    public string EntryLabel => Entry.EntryLabel;
    public IReadOnlyList<ListCellViewModelBase?> ListCells { get; }

    public ListEntryRowViewModel(
        ListEntryEditorViewModel entry,
        IReadOnlyList<FieldDefinition> columnFields,
        IListCellBuilder listCellBuilder)
    {
        Entry = entry;
        ListCells = listCellBuilder.Build(entry.CollectValues(), columnFields);
    }
}

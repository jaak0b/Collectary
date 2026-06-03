using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.UI.DI;
using Collectary.UI.ViewModels.ListCells;

namespace Collectary.UI.ViewModels;

public class ItemRowViewModel
{
    public Item Item { get; }
    public string DisplayName => Item.DisplayName;
    public IReadOnlyList<ListCellViewModelBase?> ListCells { get; }
    public bool HasListCells => ListCells.Count > 0;

    public ItemRowViewModel(Item item, IReadOnlyList<FieldDefinition> listFields, IListCellBuilder listCellBuilder)
    {
        Item = item;
        ListCells = listCellBuilder.Build(item.Values, listFields);
    }
}

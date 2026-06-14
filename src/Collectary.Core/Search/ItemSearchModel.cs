using Collectary.Core.Domain;

namespace Collectary.Core.Search;

public sealed class ItemSearchModel : ItemValueModel<Item, FieldValue>
{
    public ItemSearchModel() : base(
        item => item.Values,
        value => value.FieldDefinitionId,
        value => value.IsEmpty,
        item => item.Values,
        value => value.FieldDefinitionId)
    {
    }
}

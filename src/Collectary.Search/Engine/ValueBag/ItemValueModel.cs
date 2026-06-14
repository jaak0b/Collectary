using System.Linq.Expressions;

namespace Collectary.Search;

public class ItemValueModel<TItem, TValueBase>
    where TValueBase : class
{
    public Func<TItem, IEnumerable<TValueBase>> Values { get; }
    public Func<TValueBase, Guid> DefinitionId { get; }
    public Func<TValueBase, bool> IsEmpty { get; }
    public Expression<Func<TItem, IEnumerable<TValueBase>>> ValuesExpression { get; }
    public Expression<Func<TValueBase, Guid>> DefinitionIdExpression { get; }

    public ItemValueModel(
        Func<TItem, IEnumerable<TValueBase>> values,
        Func<TValueBase, Guid> definitionId,
        Func<TValueBase, bool> isEmpty,
        Expression<Func<TItem, IEnumerable<TValueBase>>> valuesExpression,
        Expression<Func<TValueBase, Guid>> definitionIdExpression)
    {
        Values = values;
        DefinitionId = definitionId;
        IsEmpty = isEmpty;
        ValuesExpression = valuesExpression;
        DefinitionIdExpression = definitionIdExpression;
    }
}

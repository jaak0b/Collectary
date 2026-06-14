using System.Linq.Expressions;

namespace Collectary.Search;

public interface IFieldConditionMatcher<TItem>
{
    Expression<Func<TItem, bool>>? ServerFilter(IReadOnlyCollection<Guid> definitionIds);
    bool Matches(TItem item, IReadOnlyCollection<Guid> definitionIds);
}

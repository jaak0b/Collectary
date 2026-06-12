using System.Linq.Expressions;
using Collectary.Core.Search;

namespace Collectary.Core.Domain;

public interface ISearchableFieldDefinition
{
    IReadOnlyList<QueryOperatorKind> SupportedOperators { get; }
    IEnumerable<string> ValueSuggestions();
    bool TryCreateMatcher(QueryOperatorKind op, IReadOnlyList<string> operands,
        out IFieldConditionMatcher? matcher, out QueryErrorCode? error);
    IComparable? SortKey(Item item, FieldValue? value);
}

public interface IFieldConditionMatcher
{
    Expression<Func<Item, bool>>? ServerFilter(IReadOnlyCollection<Guid> definitionIds);
    bool Matches(Item item, IReadOnlyCollection<Guid> definitionIds);
}

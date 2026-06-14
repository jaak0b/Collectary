namespace Collectary.Search;

public interface ISearchField<TItem>
{
    Func<TItem, IComparable?>? SortKey { get; }

    bool TryBind(QueryOperatorKind op, IReadOnlyList<string> operands,
        out BoundFieldMatch<TItem>? match, out QueryErrorCode? error, out QueryErrorCode? notice);
}

public interface ISearchCatalog<TItem>
{
    bool IsKnownLabel(string label);
    IReadOnlyList<ISearchField<TItem>> FieldsFor(string label);
}

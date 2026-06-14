namespace Collectary.Search;

public class StringListFieldSearch<TItem, TValueBase, TValue>
    where TValueBase : class
    where TValue : class, TValueBase
{
    private readonly AsciiCaseFolding _folding = new();
    private readonly ItemValueModel<TItem, TValueBase> _model;
    private readonly Func<TValue, IReadOnlyList<string>> _getter;

    public StringListFieldSearch(
        ItemValueModel<TItem, TValueBase> model, Func<TValue, IReadOnlyList<string>> getter)
    {
        _model = model;
        _getter = getter;
    }

    public IReadOnlyList<QueryOperatorKind> Operators =>
    [
        QueryOperatorKind.Equals, QueryOperatorKind.NotEquals,
        QueryOperatorKind.Contains, QueryOperatorKind.NotContains,
        QueryOperatorKind.In, QueryOperatorKind.IsEmpty, QueryOperatorKind.IsNotEmpty,
    ];

    public bool TryCreateMatcher(QueryOperatorKind op, IReadOnlyList<string> operands,
        out IFieldConditionMatcher<TItem>? matcher, out QueryErrorCode? error)
    {
        matcher = null;
        error = null;
        switch (op)
        {
            case QueryOperatorKind.Equals:
            {
                var folded = _folding.Fold(operands[0]);
                matcher = Entries(entries => entries.Any(e => _folding.AreEqual(e, folded)));
                return true;
            }
            case QueryOperatorKind.NotEquals:
            {
                var folded = _folding.Fold(operands[0]);
                matcher = Entries(entries =>
                    entries.Count > 0 && !entries.Any(e => _folding.AreEqual(e, folded)));
                return true;
            }
            case QueryOperatorKind.Contains:
            {
                var folded = _folding.Fold(operands[0]);
                matcher = Entries(entries => entries.Any(e => _folding.Contains(e, folded)));
                return true;
            }
            case QueryOperatorKind.NotContains:
            {
                var folded = _folding.Fold(operands[0]);
                matcher = Entries(entries =>
                    entries.Count > 0 && !entries.Any(e => _folding.Contains(e, folded)));
                return true;
            }
            case QueryOperatorKind.In:
            {
                var folded = operands.Select(_folding.Fold).ToList();
                matcher = Entries(entries => entries.Any(e => folded.Contains(_folding.Fold(e))));
                return true;
            }
            case QueryOperatorKind.IsEmpty:
                matcher = new ValueEmptinessMatcher<TItem, TValueBase>(_model, expectPresent: false);
                return true;
            case QueryOperatorKind.IsNotEmpty:
                matcher = new ValueEmptinessMatcher<TItem, TValueBase>(_model, expectPresent: true);
                return true;
            default:
                error = QueryErrorCode.OperatorNotSupported;
                return false;
        }
    }

    public IComparable? SortKey(TItem item, TValueBase? value) =>
        value is TValue typed ? string.Join(", ", _getter(typed)) : null;

    private TypedValueMatcher<TItem, TValueBase, TValue> Entries(Func<IReadOnlyList<string>, bool> predicate) =>
        new(_model, v => predicate(_getter(v)), null);
}

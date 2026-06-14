namespace Collectary.Search;

public class QueryBinder<TItem>
{
    private readonly ISearchCatalog<TItem> _catalog;

    public QueryBinder(ISearchCatalog<TItem> catalog) => _catalog = catalog;

    public QueryBindResult<TItem> Bind(ParsedQuery query) => new Run(_catalog).Bind(query);

    private sealed class Run
    {
        private readonly ISearchCatalog<TItem> _catalog;
        private readonly List<QueryError> _errors = new();
        private readonly List<QueryNotice> _notices = new();

        public Run(ISearchCatalog<TItem> catalog) => _catalog = catalog;

        public QueryBindResult<TItem> Bind(ParsedQuery query)
        {
            BoundNode<TItem>? root = null;
            if (query.Root is not null)
            {
                root = BindNode(query.Root);
                if (root is null) return new QueryBindResult<TItem>(null, _errors, _notices);
            }
            var orderBy = new List<BoundOrderBy<TItem>>();
            foreach (var clause in query.OrderBy)
            {
                var bound = BindOrderBy(clause);
                if (bound is null) return new QueryBindResult<TItem>(null, _errors, _notices);
                orderBy.Add(bound);
            }
            return new QueryBindResult<TItem>(
                new BoundQuery<TItem> { Root = root, OrderBy = orderBy }, _errors, _notices);
        }

        private BoundNode<TItem>? BindNode(QueryNode node) => node switch
        {
            AndNode and => BindPair(and.Left, and.Right, (l, r) => new BoundAndNode<TItem>(l, r)),
            OrNode or => BindPair(or.Left, or.Right, (l, r) => new BoundOrNode<TItem>(l, r)),
            NotNode not => BindNode(not.Operand) is { } inner ? new BoundNotNode<TItem>(inner) : null,
            ConditionNode condition => BindCondition(condition),
            _ => throw new InvalidOperationException($"Unknown query node {node.GetType().Name}."),
        };

        private BoundNode<TItem>? BindPair(
            QueryNode left, QueryNode right, Func<BoundNode<TItem>, BoundNode<TItem>, BoundNode<TItem>> combine)
        {
            var boundLeft = BindNode(left);
            if (boundLeft is null) return null;
            var boundRight = BindNode(right);
            return boundRight is null ? null : combine(boundLeft, boundRight);
        }

        private BoundNode<TItem>? BindCondition(ConditionNode node)
        {
            var bindings = new List<BoundFieldMatch<TItem>>();
            var successNotices = new List<QueryErrorCode>();
            var failures = new List<QueryErrorCode>();
            var operands = node.Operands.Select(o => o.Text).ToList();

            foreach (var field in _catalog.FieldsFor(node.Field))
            {
                if (field.TryBind(node.Operator, operands, out var match, out var error, out var notice))
                {
                    bindings.Add(match!);
                    if (notice is { } success) successNotices.Add(success);
                }
                else if (error is { } failure)
                {
                    failures.Add(failure);
                }
            }

            if (bindings.Count == 0)
            {
                AddConditionError(node, failures, _catalog.IsKnownLabel(node.Field));
                return null;
            }
            foreach (var notice in successNotices)
                _notices.Add(new QueryNotice(notice, node.Field));
            foreach (var failure in failures)
                _notices.Add(new QueryNotice(failure, node.Field));
            return new BoundConditionNode<TItem> { Operator = node.Operator, Bindings = bindings };
        }

        private void AddConditionError(ConditionNode node, List<QueryErrorCode> failures, bool knownLabel)
        {
            if (!knownLabel)
            {
                _errors.Add(new QueryError(
                    QueryErrorCode.UnknownField, node.FieldStart, node.FieldLength, node.Field));
                return;
            }
            if (failures.Count == 0)
            {
                _errors.Add(new QueryError(
                    QueryErrorCode.FieldNotSearchable, node.FieldStart, node.FieldLength, node.Field));
                return;
            }
            var code = failures[0];
            var operand = node.Operands.FirstOrDefault();
            if (code == QueryErrorCode.InvalidValue && operand is not null)
                _errors.Add(new QueryError(code, operand.Start, operand.Length, node.Field));
            else
                _errors.Add(new QueryError(code, node.FieldStart, node.FieldLength, node.Field));
        }

        private BoundOrderBy<TItem>? BindOrderBy(OrderByField clause)
        {
            var sources = new List<Func<TItem, IComparable?>>();
            foreach (var field in _catalog.FieldsFor(clause.Field))
                if (field.SortKey is { } key)
                    sources.Add(key);

            if (sources.Count == 0)
            {
                _errors.Add(new QueryError(
                    _catalog.IsKnownLabel(clause.Field)
                        ? QueryErrorCode.FieldNotSearchable
                        : QueryErrorCode.UnknownField,
                    clause.Start, clause.Length, clause.Field));
                return null;
            }
            return new BoundOrderBy<TItem>(
                item => sources.Select(source => source(item)).FirstOrDefault(key => key is not null),
                clause.Descending);
        }
    }
}

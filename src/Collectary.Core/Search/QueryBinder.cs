using Collectary.Core.Domain;
using Collectary.Core.Ports;

namespace Collectary.Core.Search;

public class QueryBinder
{
    private readonly PseudoFieldCatalog _pseudo;

    public QueryBinder(PseudoFieldCatalog pseudo) => _pseudo = pseudo;

    public QueryBindResult Bind(ParsedQuery query, SearchCatalogSnapshot snapshot) =>
        new Run(_pseudo, snapshot).Bind(query);

    private sealed class Run
    {
        private readonly PseudoFieldCatalog _pseudo;
        private readonly SearchCatalogSnapshot _snapshot;
        private readonly List<QueryError> _errors = new();
        private readonly List<QueryNotice> _notices = new();

        public Run(PseudoFieldCatalog pseudo, SearchCatalogSnapshot snapshot)
        {
            _pseudo = pseudo;
            _snapshot = snapshot;
        }

        public QueryBindResult Bind(ParsedQuery query)
        {
            BoundNode? root = null;
            if (query.Root is not null)
            {
                root = BindNode(query.Root);
                if (root is null) return new QueryBindResult(null, _errors, _notices);
            }
            var orderBy = new List<BoundOrderBy>();
            foreach (var clause in query.OrderBy)
            {
                var bound = BindOrderBy(clause);
                if (bound is null) return new QueryBindResult(null, _errors, _notices);
                orderBy.Add(bound);
            }
            return new QueryBindResult(
                new BoundQuery { Root = root, OrderBy = orderBy }, _errors, _notices);
        }

        private BoundNode? BindNode(QueryNode node) => node switch
        {
            AndNode and => BindPair(and.Left, and.Right, (l, r) => new BoundAndNode(l, r)),
            OrNode or => BindPair(or.Left, or.Right, (l, r) => new BoundOrNode(l, r)),
            NotNode not => BindNode(not.Operand) is { } inner ? new BoundNotNode(inner) : null,
            ConditionNode condition => BindCondition(condition),
            _ => throw new InvalidOperationException($"Unknown query node {node.GetType().Name}."),
        };

        private BoundNode? BindPair(QueryNode left, QueryNode right, Func<BoundNode, BoundNode, BoundNode> combine)
        {
            var boundLeft = BindNode(left);
            if (boundLeft is null) return null;
            var boundRight = BindNode(right);
            return boundRight is null ? null : combine(boundLeft, boundRight);
        }

        private BoundNode? BindCondition(ConditionNode node)
        {
            var bindings = new List<BoundFieldMatch>();
            var failures = new List<QueryErrorCode>();
            var operands = node.Operands.Select(o => o.Text).ToList();

            var pseudoOutcome = _pseudo.TryCreateMatcher(node.Field, node.Operator, operands, _snapshot);
            if (pseudoOutcome?.Matcher is { } pseudoMatcher)
            {
                bindings.Add(new BoundFieldMatch(pseudoMatcher, []));
                if (pseudoOutcome.Notice is { } pseudoNotice)
                    _notices.Add(new QueryNotice(pseudoNotice, node.Field));
            }
            else if (pseudoOutcome?.Error is { } pseudoError)
            {
                failures.Add(pseudoError);
            }

            var group = _snapshot.FindField(node.Field);
            foreach (var definition in group?.Definitions ?? [])
            {
                if (definition is not ISearchableFieldDefinition searchable) continue;
                if (searchable.TryCreateMatcher(node.Operator, operands, out var matcher, out var code))
                    bindings.Add(new BoundFieldMatch(matcher!, [definition.Id]));
                else if (code is { } failure)
                    failures.Add(failure);
            }

            if (bindings.Count == 0)
            {
                AddConditionError(node, failures, knownLabel: pseudoOutcome is not null || group is not null);
                return null;
            }
            foreach (var failure in failures)
                _notices.Add(new QueryNotice(failure, node.Field));
            return new BoundConditionNode { Operator = node.Operator, Bindings = bindings };
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

        private BoundOrderBy? BindOrderBy(OrderByField clause)
        {
            var sources = new List<Func<Item, IComparable?>>();
            if (_pseudo.SortKey(clause.Field, _snapshot) is { } pseudoKey)
                sources.Add(pseudoKey);

            var group = _snapshot.FindField(clause.Field);
            var sortableDefinitions = (group?.Definitions ?? [])
                .OfType<ISearchableFieldDefinition>()
                .Cast<FieldDefinition>()
                .ToList();
            foreach (var definition in sortableDefinitions)
            {
                var searchable = (ISearchableFieldDefinition)definition;
                var definitionId = definition.Id;
                sources.Add(item => searchable.SortKey(
                    item, item.Values.FirstOrDefault(v => v.FieldDefinitionId == definitionId)));
            }

            if (sources.Count == 0)
            {
                if (group is null)
                    _errors.Add(new QueryError(
                        QueryErrorCode.UnknownField, clause.Start, clause.Length, clause.Field));
                else
                    _errors.Add(new QueryError(
                        QueryErrorCode.FieldNotSearchable, clause.Start, clause.Length, clause.Field));
                return null;
            }
            return new BoundOrderBy(
                item => sources.Select(source => source(item)).FirstOrDefault(key => key is not null),
                clause.Descending);
        }
    }
}

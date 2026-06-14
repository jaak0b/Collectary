using Collectary.Core.Domain;
using Collectary.Core.Ports;

namespace Collectary.Core.Search;

public sealed class ItemSearchCatalog : ISearchCatalog<Item>
{
    private readonly PseudoFieldCatalog _pseudo;
    private readonly SearchCatalogSnapshot _snapshot;

    public ItemSearchCatalog(PseudoFieldCatalog pseudo, SearchCatalogSnapshot snapshot)
    {
        _pseudo = pseudo;
        _snapshot = snapshot;
    }

    public bool IsKnownLabel(string label) =>
        IsPseudo(label) || _snapshot.FindField(label) is not null;

    public IReadOnlyList<ISearchField<Item>> FieldsFor(string label)
    {
        var fields = new List<ISearchField<Item>>();
        if (IsPseudo(label))
            fields.Add(new PseudoSearchField(_pseudo, _snapshot, label));
        var group = _snapshot.FindField(label);
        foreach (var definition in group?.Definitions ?? [])
            if (definition is ISearchableFieldDefinition searchable)
                fields.Add(new DefinitionSearchField(searchable, definition.Id));
        return fields;
    }

    private bool IsPseudo(string label) =>
        _pseudo.Labels.Any(l => string.Equals(l, label, StringComparison.OrdinalIgnoreCase));

    private sealed class PseudoSearchField : ISearchField<Item>
    {
        private readonly PseudoFieldCatalog _pseudo;
        private readonly SearchCatalogSnapshot _snapshot;
        private readonly string _label;

        public PseudoSearchField(PseudoFieldCatalog pseudo, SearchCatalogSnapshot snapshot, string label)
        {
            _pseudo = pseudo;
            _snapshot = snapshot;
            _label = label;
        }

        public Func<Item, IComparable?>? SortKey => _pseudo.SortKey(_label, _snapshot);

        public bool TryBind(QueryOperatorKind op, IReadOnlyList<string> operands,
            out BoundFieldMatch<Item>? match, out QueryErrorCode? error, out QueryErrorCode? notice)
        {
            match = null;
            error = null;
            notice = null;
            var outcome = _pseudo.TryCreateMatcher(_label, op, operands, _snapshot);
            if (outcome?.Matcher is { } matcher)
            {
                match = new BoundFieldMatch<Item>(matcher, []);
                notice = outcome.Notice;
                return true;
            }
            error = outcome?.Error;
            return false;
        }
    }

    private sealed class DefinitionSearchField : ISearchField<Item>
    {
        private readonly ISearchableFieldDefinition _definition;
        private readonly Guid _definitionId;

        public DefinitionSearchField(ISearchableFieldDefinition definition, Guid definitionId)
        {
            _definition = definition;
            _definitionId = definitionId;
        }

        public Func<Item, IComparable?>? SortKey => item =>
            _definition.SortKey(item, item.Values.FirstOrDefault(v => v.FieldDefinitionId == _definitionId));

        public bool TryBind(QueryOperatorKind op, IReadOnlyList<string> operands,
            out BoundFieldMatch<Item>? match, out QueryErrorCode? error, out QueryErrorCode? notice)
        {
            match = null;
            error = null;
            notice = null;
            if (_definition.TryCreateMatcher(op, operands, out var matcher, out var code))
            {
                match = new BoundFieldMatch<Item>(matcher!, [_definitionId]);
                return true;
            }
            error = code;
            return false;
        }
    }
}

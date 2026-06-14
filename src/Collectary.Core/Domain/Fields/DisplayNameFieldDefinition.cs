using Collectary.Core.Ports;
using Collectary.Search;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("DisplayNameField")]
[FieldIcon(IconGlyphs.Tag)]
public class DisplayNameFieldDefinition : FieldDefinition, IListDisplayable, ISearchableFieldDefinition
{
    public override int DefaultColumnSpan => 2;
    public override bool IsTitleField => true;
    public bool ShowInList { get; set; } = true;
    public override Type ValueType => typeof(TextFieldValue);
    public override FieldValue CreateEmptyValue() => throw new NotSupportedException();

    private PseudoFieldCatalog NameSearch => new();

    public IReadOnlyList<QueryOperatorKind> SupportedOperators => NameSearch.OperatorsFor("name");

    public IEnumerable<string> ValueSuggestions() => [];

    public bool TryCreateMatcher(QueryOperatorKind op, IReadOnlyList<string> operands,
        out IFieldConditionMatcher? matcher, out QueryErrorCode? error)
    {
        var outcome = NameSearch.TryCreateMatcher("name", op, operands, new SearchCatalogSnapshot());
        matcher = outcome?.Matcher;
        error = outcome?.Error;
        return matcher is not null;
    }

    public IComparable? SortKey(Item item, FieldValue? value) => item.DisplayName;
}

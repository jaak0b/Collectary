using System.Globalization;
using Collectary.Search;

namespace Collectary.Core.Domain.Fields;

/// <summary>Stores a country as an ISO 3166-1 alpha-2 code (e.g. "DE"), shown with its flag and name.</summary>
[LocalizedName("FieldType_Country")]
[FieldIcon(IconGlyphs.Globe)]
[FieldCatalog(4, FieldCategory.Choice)]
public class CountryFieldDefinition : FieldDefinition<CountryFieldValue>, IListDisplayable, ITextImportable, ISearchableFieldDefinition
{
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => int.MaxValue;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        var text = raw.Trim();
        if (text.Length != 2 || !text.All(char.IsLetter)) return false;
        var code = text.ToUpperInvariant();
        if (!IsKnownCountryCode(code)) return false;
        value = new CountryFieldValue { FieldDefinitionId = Id, Code = code };
        return true;
    }

    private bool IsKnownCountryCode(string code)
    {
        try
        {
            return string.Equals(new RegionInfo(code).TwoLetterISORegionName, code, StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private StringFieldSearch<CountryFieldValue> Search => new(v => v.Code, v => v.Code);

    public IReadOnlyList<QueryOperatorKind> SupportedOperators => Search.Operators;

    public IEnumerable<string> ValueSuggestions() => [];

    public bool TryCreateMatcher(QueryOperatorKind op, IReadOnlyList<string> operands,
        out IFieldConditionMatcher? matcher, out QueryErrorCode? error) =>
        Search.TryCreateMatcher(op, operands, out matcher, out error);

    public IComparable? SortKey(Item item, FieldValue? value) => Search.SortKey(item, value);
}

public class CountryFieldValue : FieldValue<CountryFieldDefinition>
{
    public string? Code { get; set; }

    public override bool IsEmpty => string.IsNullOrWhiteSpace(Code);

    public override void CopyFrom(FieldValue source)
    {
        if (source is CountryFieldValue s) Code = s.Code;
    }

    public override string ToString() => Code ?? "";
}

namespace Collectary.Core.Domain.Fields;

/// <summary>Stores a country as an ISO 3166-1 alpha-2 code (e.g. "DE"), shown with its flag and name.</summary>
[LocalizedName("FieldType_Country")]
[FieldIcon(IconGlyphs.Globe)]
[FieldCatalog(2, FieldCategory.Choice)]
public class CountryFieldDefinition : FieldDefinition<CountryFieldValue>, IListDisplayable, ITextImportable
{
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => int.MaxValue;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        var text = raw.Trim();
        if (text.Length != 2 || !text.All(char.IsLetter)) return false;
        value = new CountryFieldValue { FieldDefinitionId = Id, Code = text.ToUpperInvariant() };
        return true;
    }
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

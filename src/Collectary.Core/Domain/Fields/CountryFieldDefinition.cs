namespace Collectary.Core.Domain.Fields;

/// <summary>Stores a country as an ISO 3166-1 alpha-2 code (e.g. "DE"), shown with its flag and name.</summary>
[LocalizedName("FieldType_Country")]
[FieldIcon("🏳")]
[FieldCatalog(2, FieldCategory.Choice)]
public class CountryFieldDefinition : FieldDefinition<CountryFieldValue>, IListDisplayable
{
    public bool ShowInList { get; set; }
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

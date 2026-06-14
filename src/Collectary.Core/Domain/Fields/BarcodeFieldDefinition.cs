using Collectary.Search;

namespace Collectary.Core.Domain.Fields;

/// <summary>The barcode/QR symbologies the scanner can decode and round-trip.</summary>
public enum BarcodeSymbology
{
    Unknown,
    Ean13,
    Ean8,
    UpcA,
    UpcE,
    Code39,
    Code93,
    Code128,
    Itf,
    Codabar,
    QrCode,
    DataMatrix,
    Aztec,
    Pdf417
}

[LocalizedName("FieldType_Barcode")]
[FieldIcon(IconGlyphs.Barcode)]
[FieldCatalog(13, FieldCategory.TextAndNumbers)]
public class BarcodeFieldDefinition : FieldDefinition<BarcodeFieldValue>, IListDisplayable, ITextImportable, ISearchableFieldDefinition
{
    public override int DefaultColumnSpan => 2;
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => int.MaxValue;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        if (string.IsNullOrWhiteSpace(raw)) return false;
        value = new BarcodeFieldValue { FieldDefinitionId = Id, Code = raw.Trim() };
        return true;
    }

    private StringFieldSearch<BarcodeFieldValue> Search => new(v => v.Code, v => v.Code);

    public IReadOnlyList<QueryOperatorKind> SupportedOperators => Search.Operators;

    public IEnumerable<string> ValueSuggestions() => [];

    public bool TryCreateMatcher(QueryOperatorKind op, IReadOnlyList<string> operands,
        out IFieldConditionMatcher? matcher, out QueryErrorCode? error) =>
        Search.TryCreateMatcher(op, operands, out matcher, out error);

    public IComparable? SortKey(Item item, FieldValue? value) => Search.SortKey(item, value);
}

public class BarcodeFieldValue : FieldValue<BarcodeFieldDefinition>
{
    public string? Code { get; set; }
    public BarcodeSymbology Symbology { get; set; } = BarcodeSymbology.Unknown;

    public override bool IsEmpty => string.IsNullOrWhiteSpace(Code);

    public override void CopyFrom(FieldValue source)
    {
        if (source is BarcodeFieldValue s)
        {
            Code = s.Code;
            Symbology = s.Symbology;
        }
    }

    public override string ToString() => Code ?? "";
}

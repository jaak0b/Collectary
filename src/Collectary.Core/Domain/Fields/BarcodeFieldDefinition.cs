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
[FieldCatalog(12, FieldCategory.TextAndNumbers)]
public class BarcodeFieldDefinition : FieldDefinition<BarcodeFieldValue>, IListDisplayable
{
    public override int DefaultColumnSpan => 2;
    public bool ShowInList { get; set; }
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

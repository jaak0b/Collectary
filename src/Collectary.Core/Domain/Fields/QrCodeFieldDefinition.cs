namespace Collectary.Core.Domain.Fields;

/// <summary>
/// Stores a short text payload and renders it as a scannable QR code — e.g. a storage-location label
/// you print and stick on a box, then scan back with a <see cref="BarcodeFieldDefinition"/> field.
/// </summary>
[LocalizedName("FieldType_QrCode")]
[FieldIcon(IconGlyphs.QrCode)]
[FieldCatalog(5, FieldCategory.Visual)]
public class QrCodeFieldDefinition : FieldDefinition<QrCodeFieldValue>, IListDisplayable, ITextImportable
{
    public override int DefaultColumnSpan => 2;
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => int.MaxValue;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        if (string.IsNullOrWhiteSpace(raw)) return false;
        value = new QrCodeFieldValue { FieldDefinitionId = Id, Content = raw.Trim() };
        return true;
    }
}

public class QrCodeFieldValue : FieldValue<QrCodeFieldDefinition>
{
    public string? Content { get; set; }

    public override bool IsEmpty => string.IsNullOrWhiteSpace(Content);

    public override void CopyFrom(FieldValue source)
    {
        if (source is QrCodeFieldValue s) Content = s.Content;
    }

    public override string ToString() => Content ?? "";
}

using System.Globalization;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Percentage")]
[FieldIcon(IconGlyphs.Percent)]
[FieldCatalog(4, FieldCategory.TextAndNumbers)]
public class PercentageFieldDefinition : FieldDefinition<PercentageFieldValue>, IListDisplayable, ITextImportable
{
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => 300;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        var cleaned = raw.Trim();
        if (cleaned.EndsWith('%')) cleaned = cleaned[..^1].Trim();
        if (!decimal.TryParse(cleaned, NumberStyles.Number, culture, out var d)) return false;
        value = new PercentageFieldValue { FieldDefinitionId = Id, Value = d };
        return true;
    }
}

public class PercentageFieldValue : FieldValue<PercentageFieldDefinition>
{
    public decimal? Value { get; set; }
    public override bool IsEmpty => Value is null;
    public override void CopyFrom(FieldValue source) { if (source is PercentageFieldValue s) Value = s.Value; }
    public override string ToString() => Value.HasValue ? $"{Value:F1} %" : "";
}

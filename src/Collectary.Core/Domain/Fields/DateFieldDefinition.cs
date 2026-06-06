using System.Globalization;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Date")]
[FieldIcon(IconGlyphs.Calendar)]
[FieldCatalog(6, FieldCategory.TextAndNumbers)]
public class DateFieldDefinition : FieldDefinition<DateFieldValue>, IListDisplayable, ITextImportable
{
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => 70;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        if (!DateTime.TryParse(raw, culture, DateTimeStyles.None, out var dt)) return false;
        value = new DateFieldValue { FieldDefinitionId = Id, Value = dt.Date };
        return true;
    }
}

public class DateFieldValue : FieldValue<DateFieldDefinition>
{
    public DateTime? Value { get; set; }
    public override bool IsEmpty => Value is null;
    public override void CopyFrom(FieldValue source) { if (source is DateFieldValue s) Value = s.Value; }
    public override string ToString() => Value?.ToString("d") ?? "";
}

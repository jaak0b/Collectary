namespace Collectary.Core.Domain.Fields;

/// <summary>A from–to date pair — an ownership period, a drink-window, a manufacturing era.</summary>
[LocalizedName("FieldType_DateRange")]
[FieldIcon("📆")]
[FieldCatalog(17, FieldCategory.TextAndNumbers)]
public class DateRangeFieldDefinition : FieldDefinition<DateRangeFieldValue>, IListDisplayable
{
    public override int DefaultColumnSpan => 2;
    public bool ShowInList { get; set; }
}

public class DateRangeFieldValue : FieldValue<DateRangeFieldDefinition>
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    public override bool IsEmpty => From is null && To is null;

    public override void CopyFrom(FieldValue source)
    {
        if (source is DateRangeFieldValue s)
        {
            From = s.From;
            To = s.To;
        }
    }

    public override string ToString()
    {
        if (From is null && To is null) return "";
        return $"{Format(From)} – {Format(To)}";
    }

    private string Format(DateTime? d) =>
        d?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? "?";
}

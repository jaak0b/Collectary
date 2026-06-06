namespace Collectary.Core.Domain.Fields;

using System.Globalization;

/// <summary>A from–to date pair — an ownership period, a drink-window, a manufacturing era.</summary>
[LocalizedName("FieldType_DateRange")]
[FieldIcon(IconGlyphs.DateRange)]
[FieldCatalog(7, FieldCategory.TextAndNumbers)]
public class DateRangeFieldDefinition : FieldDefinition<DateRangeFieldValue>, IListDisplayable, ITextImportable
{
    public override int DefaultColumnSpan => 2;
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => 80;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        var separators = new[] { " – ", "–", " — ", "—", " - ", " to ", " bis ", ".." };
        foreach (var sep in separators)
        {
            var idx = raw.IndexOf(sep, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) continue;
            var left = raw[..idx].Trim();
            var right = raw[(idx + sep.Length)..].Trim();
            if (!DateTime.TryParse(left, culture, DateTimeStyles.None, out var from)
                || !DateTime.TryParse(right, culture, DateTimeStyles.None, out var to))
                continue;
            value = new DateRangeFieldValue { FieldDefinitionId = Id, From = from.Date, To = to.Date };
            return true;
        }
        return false;
    }
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

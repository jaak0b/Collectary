using System.Globalization;
using System.Text.RegularExpressions;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Duration")]
[FieldIcon(IconGlyphs.Timer)]
[FieldCatalog(9, FieldCategory.TextAndNumbers)]
public class DurationFieldDefinition : FieldDefinition<DurationFieldValue>, IListDisplayable, ITextImportable
{
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => 340;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        var text = raw.Trim();
        if (text.Length == 0) return false;

        if (int.TryParse(text, NumberStyles.Integer, culture, out var plainMinutes))
        {
            value = new DurationFieldValue { FieldDefinitionId = Id, TotalMinutes = plainMinutes };
            return true;
        }

        if (text.Contains(':'))
        {
            var parts = text.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[0], out var hours) || !int.TryParse(parts[1], out var minutes))
                return false;
            value = new DurationFieldValue { FieldDefinitionId = Id, TotalMinutes = hours * 60 + minutes };
            return true;
        }

        var match = Regex.Match(text, @"^\s*(?:(\d+)\s*h(?:ours?|rs?)?)?\s*(?:(\d+)\s*m(?:in(?:utes?)?)?)?\s*$", RegexOptions.IgnoreCase);
        if (!match.Success || (!match.Groups[1].Success && !match.Groups[2].Success)) return false;
        var h = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
        var m = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
        value = new DurationFieldValue { FieldDefinitionId = Id, TotalMinutes = h * 60 + m };
        return true;
    }
}

public class DurationFieldValue : FieldValue<DurationFieldDefinition>
{
    public int? TotalMinutes { get; set; }
    public override bool IsEmpty => TotalMinutes is null;
    public override void CopyFrom(FieldValue source) { if (source is DurationFieldValue s) TotalMinutes = s.TotalMinutes; }
    public override string ToString()
    {
        if (TotalMinutes is null) return "";
        var h = TotalMinutes.Value / 60;
        var m = TotalMinutes.Value % 60;
        return h > 0 ? $"{h} h {m:D2} min" : $"{m} min";
    }
}

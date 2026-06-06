using System.Globalization;

namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Rating")]
[FieldIcon(IconGlyphs.Star)]
[FieldCatalog(1, FieldCategory.Visual)]
public class RatingFieldDefinition : FieldDefinition<RatingFieldValue>, IListDisplayable, ITextImportable
{
    public int MaxStars { get; set; } = 5;
    public bool ShowInList { get; set; }

    public int ImportInferenceOrder => 200;

    public bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value)
    {
        value = CreateEmptyValue();
        if (!int.TryParse(raw, NumberStyles.Integer, culture, out var stars) || stars < 0 || stars > MaxStars) return false;
        value = new RatingFieldValue { FieldDefinitionId = Id, Stars = stars };
        return true;
    }

    public override void ApplyTypeSpecificProperties(FieldDefinition source)
    {
        if (source is RatingFieldDefinition src) MaxStars = src.MaxStars;
    }
}

public class RatingFieldValue : FieldValue<RatingFieldDefinition>
{
    public int? Stars { get; set; }
    public override bool IsEmpty => Stars is null;
    public override void CopyFrom(FieldValue source) { if (source is RatingFieldValue s) Stars = s.Stars; }
    public override string ToString() => Stars.HasValue ? new string('★', Stars.Value) : "";
}

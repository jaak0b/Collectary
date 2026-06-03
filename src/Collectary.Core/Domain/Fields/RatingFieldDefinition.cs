namespace Collectary.Core.Domain.Fields;

[LocalizedName("FieldType_Rating")]
[FieldIcon("★")]
public class RatingFieldDefinition : FieldDefinition<RatingFieldValue>, IListDisplayable
{
    public int MaxStars { get; set; } = 5;
    public bool ShowInList { get; set; }
}

public class RatingFieldValue : FieldValue<RatingFieldDefinition>
{
    public int? Stars { get; set; }
    public override bool IsEmpty => Stars is null;
    public override void CopyFrom(FieldValue source) { if (source is RatingFieldValue s) Stars = s.Stars; }
    public override string ToString() => Stars.HasValue ? new string('★', Stars.Value) : "";
}

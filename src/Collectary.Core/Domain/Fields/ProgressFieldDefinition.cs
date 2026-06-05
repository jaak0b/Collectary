namespace Collectary.Core.Domain.Fields;

/// <summary>Tracks "owned X of Y" completion — cards in a set, pieces of a build, issues in a run.</summary>
[LocalizedName("FieldType_Progress")]
[FieldIcon(IconGlyphs.DataBar)]
[FieldCatalog(6, FieldCategory.Visual)]
public class ProgressFieldDefinition : FieldDefinition<ProgressFieldValue>, IListDisplayable
{
    public bool ShowInList { get; set; }
}

public class ProgressFieldValue : FieldValue<ProgressFieldDefinition>
{
    public int? Have { get; set; }
    public int? Total { get; set; }

    public override bool IsEmpty => Have is null && Total is null;

    public override void CopyFrom(FieldValue source)
    {
        if (source is ProgressFieldValue s)
        {
            Have = s.Have;
            Total = s.Total;
        }
    }

    public override string ToString() => $"{Have ?? 0}/{Total ?? 0}";
}

namespace Collectary.Core.Domain.Fields;

public enum AutoNumberStrategy { HighestPlusOne, FillGaps }

public enum DuplicateHandling { Error, Warn, Allow }

[LocalizedName("FieldType_AutoNumber")]
[FieldIcon(IconGlyphs.NumberSymbol)]
[FieldCatalog(17, FieldCategory.TextAndNumbers)]
public class AutoNumberFieldDefinition : FieldDefinition<AutoNumberFieldValue>, IListDisplayable
{
    public bool Editable { get; set; }
    public AutoNumberStrategy Strategy { get; set; } = AutoNumberStrategy.HighestPlusOne;
    public DuplicateHandling OnDuplicate { get; set; } = DuplicateHandling.Error;
    public bool ShowInList { get; set; } = true;

    public int NextNumber(IReadOnlyCollection<int> used) => Strategy switch
    {
        AutoNumberStrategy.FillGaps => Enumerable.Range(1, used.Count + 1).First(n => !used.Contains(n)),
        _ => (used.Count == 0 ? 0 : used.Max()) + 1,
    };

    public override void ApplyTypeSpecificProperties(FieldDefinition source)
    {
        if (source is not AutoNumberFieldDefinition src) return;
        Editable = src.Editable;
        Strategy = src.Strategy;
        OnDuplicate = src.OnDuplicate;
    }
}

public class AutoNumberFieldValue : FieldValue<AutoNumberFieldDefinition>
{
    public int? Value { get; set; }
    public override bool IsEmpty => Value is null;
    public override void CopyFrom(FieldValue source) { if (source is AutoNumberFieldValue s) Value = s.Value; }
    public override string ToString() => Value?.ToString() ?? "";
}

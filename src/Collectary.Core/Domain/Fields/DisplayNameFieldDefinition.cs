namespace Collectary.Core.Domain.Fields;

[LocalizedName("DisplayNameField")]
[FieldIcon("🏷")]
public class DisplayNameFieldDefinition : FieldDefinition, IListDisplayable
{
    public override int DefaultColumnSpan => 2;
    public bool ShowInList { get; set; } = true;
    public override Type ValueType => typeof(TextFieldValue);
    public override FieldValue CreateEmptyValue() => throw new NotSupportedException();
}

namespace Collectary.Core.Domain.Fields;

[LocalizedName("DisplayNameField")]
[FieldIcon(IconGlyphs.Tag)]
public class DisplayNameFieldDefinition : FieldDefinition, IListDisplayable
{
    public override int DefaultColumnSpan => 2;
    public override bool IsTitleField => true;
    public bool ShowInList { get; set; } = true;
    public override Type ValueType => typeof(TextFieldValue);
    public override FieldValue CreateEmptyValue() => throw new NotSupportedException();
}

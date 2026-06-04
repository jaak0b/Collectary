namespace Collectary.Core.Domain;

/// <summary>
/// Resolves whether a field's label should render above its input, given the preset's choice
/// (null = inherit), the global default, and the preset's column count (for <see cref="FieldLabelLayout.Adaptive"/>).
/// </summary>
public class FieldLabelLayoutResolver
{
    public bool ResolveLabelAbove(FieldLabelLayout? presetValue, FieldLabelLayout globalDefault, int columnCount)
    {
        var effective = presetValue ?? globalDefault;
        return effective switch
        {
            FieldLabelLayout.Above => true,
            FieldLabelLayout.Adaptive => columnCount > 1,
            _ => false
        };
    }
}

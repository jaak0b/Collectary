namespace Collectary.Core.Domain.Fields;

/// <summary>
/// Groups addable field types in the "Add field" menu. Categories render in declaration
/// order, separated by a divider, in both the preset editor and the system-field library.
/// </summary>
public enum FieldCategory
{
    Text,
    Numbers,
    DateTime,
    Choice,
    MediaAndFiles,
    Structure,
}

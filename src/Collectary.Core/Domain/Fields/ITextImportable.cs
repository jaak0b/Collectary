namespace Collectary.Core.Domain.Fields;

public interface ITextImportable
{
    int ImportInferenceOrder { get; }
    bool TryImportFromText(string raw, IFormatProvider culture, out FieldValue value);
    void ApplyImportDefaults() { }
    bool EnforcesUniqueImportValues => false;
}

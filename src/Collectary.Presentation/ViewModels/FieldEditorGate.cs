namespace Collectary.Presentation.ViewModels;

/// <summary>
/// Shared save-gate for a set of field editors: waits until every editor has finished its async
/// initialisation, then returns the first validation error (or null when all are valid). Used by both
/// the item editor and the list-entry editor so no save path can skip readiness or validation.
/// </summary>
public class FieldEditorGate
{
    public async Task<string?> AwaitReadyAndValidateAsync(IEnumerable<FieldEditorViewModelBase> editors)
    {
        var list = editors.ToList();
        await Task.WhenAll(list.Select(e => e.Ready));
        return list.Select(e => e.Validate()).FirstOrDefault(e => !string.IsNullOrWhiteSpace(e));
    }
}

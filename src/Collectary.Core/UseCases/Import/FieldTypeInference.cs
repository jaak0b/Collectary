using System.Globalization;
using System.Reflection;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Domain.Import;
using Collectary.Core.Ports;

namespace Collectary.Core.UseCases.Import;

public sealed class FieldTypeInference : IFieldTypeInference
{
    private sealed record InferenceCandidate(ITextImportable Probe, Type Type);

    private readonly IReadOnlyList<InferenceCandidate> _candidates;

    public FieldTypeInference()
    {
        _candidates = typeof(FieldDefinition).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && typeof(FieldDefinition).IsAssignableFrom(t))
            .Where(t => t.GetCustomAttribute<FieldCatalogAttribute>() is not null)
            .Select(t => (Instance: Activator.CreateInstance(t) as FieldDefinition, Type: t))
            .Where(x => x.Instance is ITextImportable importable && importable.ImportInferenceOrder != int.MaxValue)
            .Select(x => new InferenceCandidate((ITextImportable)x.Instance!, x.Type))
            .OrderBy(x => x.Probe.ImportInferenceOrder)
            .ToList();
    }

    public FieldDefinition Infer(IReadOnlyList<WorkbookCell> columnCells, CultureInfo culture)
    {
        var samples = columnCells
            .Where(cell => cell.Kind != WorkbookCellKind.Blank && !string.IsNullOrWhiteSpace(cell.Text))
            .Take(50)
            .ToList();

        if (samples.Count == 0) return new TextFieldDefinition();

        foreach (var candidate in _candidates)
            if (samples.All(cell => candidate.Probe.TryImportFromText(cell.Text!, cell.EffectiveCulture(culture), out _)))
                return (FieldDefinition)Activator.CreateInstance(candidate.Type)!;

        return new TextFieldDefinition();
    }
}

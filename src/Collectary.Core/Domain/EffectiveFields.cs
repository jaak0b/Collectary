namespace Collectary.Core.Domain;

public class EffectiveFields
{
    public IReadOnlyList<FieldDefinition> Fields { get; init; } = [];
    public IReadOnlyList<FieldGroup> Groups { get; init; } = [];
    public IReadOnlyDictionary<Guid, Guid?> GroupByFieldId { get; init; } =
        new Dictionary<Guid, Guid?>();
}

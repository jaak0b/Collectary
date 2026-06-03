namespace Collectary.Core.Domain;

public abstract class FieldDefinition : DomainObject
{
    public Guid? PresetId { get; set; }
    public Guid? ParentListFieldDefinitionId { get; set; }
    public Guid? SystemFieldId { get; set; }
    public Guid? GroupId { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public int ColumnSpan { get; set; } = 1;

    public abstract Type ValueType { get; }
    public abstract FieldValue CreateEmptyValue();

    public FieldValue GetOrCreateEmptyValue(FieldValue? existing)
    {
        if (existing is null) return CreateEmptyValue();
        if (existing.GetType() == ValueType) return existing;
        throw new InvalidOperationException(
            $"Value type mismatch: expected {ValueType.Name} but got {existing.GetType().Name} " +
            $"for field definition {Id} ({GetType().Name}).");
    }
}

public abstract class FieldDefinition<TValue> : FieldDefinition
    where TValue : FieldValue, new()
{
    public override Type ValueType => typeof(TValue);

    public override FieldValue CreateEmptyValue() => new TValue { FieldDefinitionId = Id };

    public new TValue GetOrCreateEmptyValue(FieldValue? existing)
    {
        if (existing is null) return new TValue { FieldDefinitionId = Id };
        if (existing is TValue typed) return typed;
        throw new InvalidOperationException(
            $"Value type mismatch: expected {typeof(TValue).Name} but got {existing.GetType().Name} " +
            $"for field definition {Id} ({GetType().Name}).");
    }
}

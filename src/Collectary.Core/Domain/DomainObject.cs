namespace Collectary.Core.Domain;

public abstract class DomainObject
{
    public Guid Id { get; init; } = Guid.NewGuid();
}

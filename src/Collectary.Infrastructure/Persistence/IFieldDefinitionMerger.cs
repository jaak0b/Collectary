using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Infrastructure.Persistence;

public interface IFieldDefinitionMerger
{
    void MergePreset(InventoryDbContext db, Preset tracked, Preset incoming);

    void Apply(InventoryDbContext db, FieldDefinition existing, FieldDefinition updated);

    void SyncSubFields(InventoryDbContext db, ListFieldDefinition existing, ListFieldDefinition updated);

    HashSet<Guid> SyncGroups(
        InventoryDbContext db,
        ICollection<FieldGroup> existing,
        IReadOnlyList<FieldGroup> updated,
        Action<FieldGroup> assignOwner);
}

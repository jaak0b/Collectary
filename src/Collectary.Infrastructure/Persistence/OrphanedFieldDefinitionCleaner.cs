using Collectary.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Collectary.Infrastructure.Persistence;

/// <summary>
/// Removes orphaned rows from the Table-Per-Type <c>FieldDefinitions</c> base table — base rows with no
/// matching subtype-table row, left behind by an earlier field-type removal. EF can't determine such a
/// row's concrete type, so any read of the hierarchy throws ("No discriminators matched the discriminator
/// value ''") and brings down sync. The orphan is already unreadable, so deleting it loses nothing usable.
/// Subtype tables are discovered from the model, so a new field type is covered with no change here.
/// </summary>
public class OrphanedFieldDefinitionCleaner
{
    public async Task<int> CleanAsync(InventoryDbContext db)
    {
        var subtypeTables = db.Model.GetEntityTypes()
            .Where(t => typeof(FieldDefinition).IsAssignableFrom(t.ClrType) && t.ClrType != typeof(FieldDefinition))
            .Select(t => t.GetTableName())
            .Where(name => !string.IsNullOrEmpty(name) && name != "FieldDefinitions")
            .Distinct()
            .ToList();

        if (subtypeTables.Count == 0) return 0;

        var union = string.Join(" UNION ", subtypeTables.Select(t => $"SELECT \"Id\" FROM \"{t}\""));
        return await db.Database.ExecuteSqlRawAsync(
            $"DELETE FROM \"FieldDefinitions\" WHERE \"Id\" NOT IN ({union})");
    }
}

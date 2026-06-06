using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Collectary.Infrastructure.Tests.Persistence;

[TestFixture]
public class OrphanedFieldDefinitionCleanerTest : DbIntegrationTestBase
{
    private async Task<Guid> SeedOrphanAsync()
    {
        using var db = DbFactory();
        var preset = new Preset { Name = "P" };
        preset.Fields.Add(new TextFieldDefinition { Label = "victim" });
        preset.Fields.Add(new IntegerFieldDefinition { Label = "keeper" });
        db.Presets.Add(preset);
        await db.SaveChangesAsync();

        var victimId = preset.Fields[0].Id;
        // Corruption an earlier field-type removal could leave: a base row whose subtype row is gone.
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"TextFieldDefinitions\" WHERE \"Id\" = {0}", victimId);
        return victimId;
    }

    [Test]
    public async Task OrphanedBaseRow_MakesTheHierarchyQueryThrow()
    {
        await SeedOrphanAsync();

        using var db = DbFactory();
        Assert.That(async () => await db.FieldDefinitions.ToListAsync(), Throws.Exception,
            "precondition: an orphaned base row breaks reads of the whole hierarchy");
    }

    [Test]
    public async Task CleanAsync_RemovesOrphan_AndKeepsHealthyDefinitions()
    {
        await SeedOrphanAsync();

        using (var db = DbFactory())
            await new OrphanedFieldDefinitionCleaner().CleanAsync(db);

        using var verify = DbFactory();
        var defs = await verify.FieldDefinitions.ToListAsync();
        Assert.Multiple(() =>
        {
            Assert.That(defs, Has.Exactly(1).Items, "the orphan is removed; the healthy definition stays");
            Assert.That(defs[0], Is.TypeOf<IntegerFieldDefinition>());
        });
    }

    [Test]
    public async Task CleanAsync_WithNoOrphans_DeletesNothing()
    {
        using (var db = DbFactory())
        {
            var preset = new Preset { Name = "P" };
            preset.Fields.Add(new TextFieldDefinition { Label = "ok" });
            db.Presets.Add(preset);
            await db.SaveChangesAsync();
        }

        int removed;
        using (var db = DbFactory())
            removed = await new OrphanedFieldDefinitionCleaner().CleanAsync(db);

        Assert.That(removed, Is.EqualTo(0));
    }
}

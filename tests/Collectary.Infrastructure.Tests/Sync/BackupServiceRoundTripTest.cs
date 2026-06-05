using System.Text;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.UseCases;
using Collectary.Infrastructure.Persistence;
using Collectary.Infrastructure.Storage;
using Collectary.Infrastructure.Sync;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Collectary.Infrastructure.Tests.Sync;

[TestFixture]
public class BackupServiceRoundTripTest : DbIntegrationTestBase
{
    private readonly List<SqliteConnection> _connections = new();
    private readonly List<string> _dirs = new();

    [TearDown]
    public void CleanUp()
    {
        foreach (var connection in _connections) connection.Dispose();
        SqliteConnection.ClearAllPools();
        foreach (var dir in _dirs)
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
    }

    private EfSyncStore NewStore(out Func<InventoryDbContext> factory)
    {
        var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        connection.Open();
        _connections.Add(connection);
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite(connection).Options;
        using (var db = new InventoryDbContext(options)) db.Database.EnsureCreated();
        factory = () => new InventoryDbContext(options);
        return new EfSyncStore(factory, new FieldDefinitionMerger());
    }

    private FileSystemImageStore NewImageStore()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"collectary-blobs-{Guid.NewGuid():N}");
        _dirs.Add(dir);
        return new FileSystemImageStore(dir);
    }

    [Test]
    public async Task ExportThenImportIntoFreshStore_RestoresEntitiesImagesAndDocuments()
    {
        var source = NewStore(out _);
        var sourceImages = NewImageStore();
        var serializer = new SyncSerializer();

        var presetId = Guid.NewGuid();
        var imgFieldId = Guid.NewGuid();
        var docFieldId = Guid.NewGuid();
        var preset = new Preset { Id = presetId, Name = "Coins", Revision = 1 };
        preset.Fields.Add(new ImageFieldDefinition { Id = imgFieldId, Label = "Photo", PresetId = presetId });
        preset.Fields.Add(new FileAttachmentFieldDefinition { Id = docFieldId, Label = "Cert", PresetId = presetId });
        await source.ApplyPresetAsync(preset);

        var imgKey = $"{Guid.NewGuid()}_photo.png";
        var docKey = $"{Guid.NewGuid()}_cert.pdf";
        await sourceImages.ImportAsync(imgKey, new MemoryStream(Encoding.UTF8.GetBytes("IMAGE-BYTES")));
        await sourceImages.ImportAsync(docKey, new MemoryStream(Encoding.UTF8.GetBytes("DOC-BYTES")));

        var itemId = Guid.NewGuid();
        var item = new Item { Id = itemId, DisplayName = "Penny", PresetId = presetId, Revision = 1 };
        item.Values.Add(new ImageFieldValue { Id = Guid.NewGuid(), FieldDefinitionId = imgFieldId, ItemId = itemId, ImageKey = imgKey, FileName = "photo.png" });
        item.Values.Add(new FileAttachmentFieldValue { Id = Guid.NewGuid(), FieldDefinitionId = docFieldId, ItemId = itemId, Files = [new(docKey, "cert.pdf")] });
        await source.ApplyItemAsync(item);

        using var ms = new MemoryStream();
        await new BackupService(source, serializer, sourceImages).ExportAsync(ms);
        ms.Position = 0;

        var target = NewStore(out _);
        var targetImages = NewImageStore();
        var result = await new BackupService(target, serializer, targetImages).ImportAsync(ms);

        var restoredItem = (await target.GetAllItemsAsync()).Single(i => i.Id == itemId);
        var restoredImage = restoredItem.Values.OfType<ImageFieldValue>().Single();
        var restoredDoc = restoredItem.Values.OfType<FileAttachmentFieldValue>().Single();
        var presetRestored = (await target.GetAllPresetsAsync()).Any(p => p.Id == presetId);

        Assert.Multiple(() =>
        {
            Assert.That(presetRestored, Is.True);
            Assert.That(restoredItem.DisplayName, Is.EqualTo("Penny"));
            Assert.That(restoredImage.ImageKey, Is.EqualTo(imgKey));
            Assert.That(restoredDoc.Files.Single().Key, Is.EqualTo(docKey));
            Assert.That(targetImages.Exists(imgKey), Is.True);
            Assert.That(targetImages.Exists(docKey), Is.True);
            Assert.That(result.HasConflicts, Is.False);
        });

        using var imgReader = new StreamReader(targetImages.Open(imgKey));
        using var docReader = new StreamReader(targetImages.Open(docKey));
        Assert.Multiple(() =>
        {
            Assert.That(imgReader.ReadToEnd(), Is.EqualTo("IMAGE-BYTES"));
            Assert.That(docReader.ReadToEnd(), Is.EqualTo("DOC-BYTES"));
        });
    }

    [Test]
    public async Task ExportThenImportIntoFreshStore_RestoresBlobsOfSoftDeletedItems()
    {
        var source = NewStore(out _);
        var sourceImages = NewImageStore();
        var serializer = new SyncSerializer();

        var presetId = Guid.NewGuid();
        var imgFieldId = Guid.NewGuid();
        var preset = new Preset { Id = presetId, Name = "Coins", Revision = 1 };
        preset.Fields.Add(new ImageFieldDefinition { Id = imgFieldId, Label = "Photo", PresetId = presetId });
        await source.ApplyPresetAsync(preset);

        var imgKey = $"{Guid.NewGuid()}_photo.png";
        await sourceImages.ImportAsync(imgKey, new MemoryStream(Encoding.UTF8.GetBytes("IMAGE-BYTES")));

        var itemId = Guid.NewGuid();
        var item = new Item { Id = itemId, DisplayName = "Penny", PresetId = presetId, Revision = 1, IsDeleted = true, DeletedAt = DateTime.UtcNow };
        item.Values.Add(new ImageFieldValue { Id = Guid.NewGuid(), FieldDefinitionId = imgFieldId, ItemId = itemId, ImageKey = imgKey, FileName = "photo.png" });
        await source.ApplyItemAsync(item);

        using var ms = new MemoryStream();
        await new BackupService(source, serializer, sourceImages).ExportAsync(ms);
        ms.Position = 0;

        var target = NewStore(out _);
        var targetImages = NewImageStore();
        await new BackupService(target, serializer, targetImages).ImportAsync(ms);

        var restoredItem = (await target.GetAllItemsAsync()).Single(i => i.Id == itemId);
        Assert.Multiple(() =>
        {
            Assert.That(restoredItem.IsDeleted, Is.True);
            Assert.That(targetImages.Exists(imgKey), Is.True);
        });
        using var imgReader = new StreamReader(targetImages.Open(imgKey));
        Assert.That(imgReader.ReadToEnd(), Is.EqualTo("IMAGE-BYTES"));
    }

    [Test]
    public async Task ExportThenImport_RestoresPresetAndItemThatReferenceASystemField()
    {
        var source = NewStore(out _);
        var sourceImages = NewImageStore();
        var serializer = new SyncSerializer();

        var sysId = Guid.NewGuid();
        var systemField = new SystemField
        {
            Id = sysId, Name = "Rarity", Revision = 1,
            Definition = new TextFieldDefinition { SystemFieldId = sysId },
        };
        await source.ApplySystemFieldAsync(systemField);

        var presetId = Guid.NewGuid();
        var preset = new Preset { Id = presetId, Name = "Cards", Revision = 1 };
        preset.SystemFieldRefs.Add(new PresetSystemField { PresetId = presetId, SystemFieldId = sysId, DisplayOrder = 0 });
        await source.ApplyPresetAsync(preset);

        var itemId = Guid.NewGuid();
        var item = new Item { Id = itemId, DisplayName = "Charizard", PresetId = presetId, Revision = 1 };
        item.Values.Add(new TextFieldValue { Id = Guid.NewGuid(), FieldDefinitionId = systemField.Definition.Id, ItemId = itemId, Value = "Holo" });
        await source.ApplyItemAsync(item);

        using var ms = new MemoryStream();
        await new BackupService(source, serializer, sourceImages).ExportAsync(ms);
        ms.Position = 0;

        var target = NewStore(out _);
        var targetImages = NewImageStore();
        var result = await new BackupService(target, serializer, targetImages).ImportAsync(ms);

        var systemFieldRestored = (await target.GetAllSystemFieldsAsync()).Any(s => s.Id == sysId);
        var restoredPreset = (await target.GetAllPresetsAsync()).Single(p => p.Id == presetId);
        var restoredItem = (await target.GetAllItemsAsync()).Single(i => i.Id == itemId);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasConflicts, Is.False);
            Assert.That(systemFieldRestored, Is.True);
            Assert.That(restoredPreset.SystemFieldRefs.Single().SystemFieldId, Is.EqualTo(sysId));
            Assert.That(((TextFieldValue)restoredItem.Values.Single()).Value, Is.EqualTo("Holo"));
        });
    }
}

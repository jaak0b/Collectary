using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using Collectary.Infrastructure.Persistence;
using Collectary.Infrastructure.Storage;
using Collectary.Infrastructure.Sync;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Collectary.Infrastructure.Tests.Sync;

[TestFixture]
public class SyncEndToEndTest
{
    private string _folder = null!;
    private readonly List<string> _dbPaths = new();
    private readonly List<string> _dirs = new();
    private Client _a = null!;
    private Client _b = null!;

    [SetUp]
    public void SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _a = MakeClient();
        _b = MakeClient();
    }

    [TearDown]
    public void TearDown()
    {
        SqliteConnection.ClearAllPools();
        foreach (var path in _dbPaths)
            if (File.Exists(path)) File.Delete(path);
        _dbPaths.Clear();
        foreach (var dir in _dirs)
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        _dirs.Clear();
        if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
    }

    private sealed class Client
    {
        public required PresetRepository Presets { get; init; }
        public required ItemRepository Items { get; init; }
        public required SharedFieldRepository SharedFields { get; init; }
        public required IPresetUseCase PresetUseCase { get; init; }
        public required EfSyncStore Store { get; init; }
        public required FileSystemImageStore Images { get; init; }
        public required SyncService Sync { get; init; }
    }

    private Client MakeClient()
    {
        var path = Path.Combine(Path.GetTempPath(), $"collectary-sync-{Guid.NewGuid():N}.db");
        _dbPaths.Add(path);
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite($"Data Source={path};Pooling=False").Options;
        using (var db = new InventoryDbContext(options)) db.Database.EnsureCreated();

        Func<InventoryDbContext> factory = () => new InventoryDbContext(options);
        var merger = new FieldDefinitionMerger();
        var status = new AlwaysConfiguredSyncStatus();
        var presets = new PresetRepository(factory, merger, null, null, status);
        var items = new ItemRepository(factory, null, null, status);
        var sharedFields = new SharedFieldRepository(factory, merger, null, status);
        var store = new EfSyncStore(factory, merger);

        var imageDir = Path.Combine(Path.GetTempPath(), $"collectary-img-{Guid.NewGuid():N}");
        _dirs.Add(imageDir);
        var images = new FileSystemImageStore(imageDir);

        return new Client
        {
            Presets = presets,
            Items = items,
            SharedFields = sharedFields,
            PresetUseCase = new PresetUseCase(presets, items, new AllowAllAuthorization()),
            Store = store,
            Images = images,
            Sync = new SyncService(new FileSystemSyncBackend(_folder), store, new SyncSerializer(),
                new FixedDeviceIdentity(Guid.NewGuid()), status, images),
        };
    }

    private static Preset MakePreset(string name)
    {
        var preset = new Preset { Name = name };
        preset.Fields.Add(new TextFieldDefinition { Label = "Title", PresetId = preset.Id });
        return preset;
    }

    private async Task SyncBothAsync()
    {
        await _a.Sync.SyncAsync();
        await _b.Sync.SyncAsync();
        await _a.Sync.SyncAsync();
    }

    [Test]
    public async Task Create_PropagatesSharedFieldPresetAndItem()
    {
        var sf = new SharedField { Name = "Year", Definition = new IntegerFieldDefinition { Label = "Year" } };
        sf.Definition.SharedFieldId = sf.Id;
        await _a.SharedFields.AddAsync(sf);
        var preset = MakePreset("Model trains");
        await _a.Presets.AddAsync(preset);
        await _a.Items.AddAsync(new Item { PresetId = preset.Id, DisplayName = "Loco 42" });

        await SyncBothAsync();

        var sharedFields = (await _b.Store.GetAllSharedFieldsAsync()).Select(x => x.Name).ToList();
        var presets = (await _b.Store.GetAllPresetsAsync()).Select(x => x.Name).ToList();
        var items = (await _b.Store.GetAllItemsAsync()).Select(x => x.DisplayName).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(sharedFields, Does.Contain("Year"));
            Assert.That(presets, Does.Contain("Model trains"));
            Assert.That(items, Does.Contain("Loco 42"));
        });
    }

    [Test]
    public async Task PresetRename_PropagatesAtoB()
    {
        var preset = MakePreset("Old");
        await _a.Presets.AddAsync(preset);
        await SyncBothAsync();

        var local = await _a.Presets.GetByIdAsync(preset.Id);
        local!.Name = "New";
        await _a.Presets.UpdateAsync(local);
        await SyncBothAsync();

        Assert.That((await _b.Presets.GetByIdAsync(preset.Id))!.Name, Is.EqualTo("New"));
    }

    [Test]
    public async Task PresetRename_DoesNotDestroyItemValuesOnPeer()
    {
        var preset = new Preset { Name = "Trains" };
        var field = new TextFieldDefinition { Label = "Title", PresetId = preset.Id };
        preset.Fields.Add(field);
        await _a.Presets.AddAsync(preset);
        var item = new Item { PresetId = preset.Id, DisplayName = "Loco 42" };
        item.Values.Add(new TextFieldValue { FieldDefinitionId = field.Id, Value = "Flying Scotsman", ItemId = item.Id });
        await _a.Items.AddAsync(item);
        await SyncBothAsync();
        Assume.That(((TextFieldValue)(await _b.Items.GetByIdAsync(item.Id))!.Values.Single()).Value,
            Is.EqualTo("Flying Scotsman"), "precondition: item value reached B");

        var local = await _a.Presets.GetByIdAsync(preset.Id);
        local!.Name = "Locomotives";
        await _a.Presets.UpdateAsync(local);
        await SyncBothAsync();

        var onB = await _b.Items.GetByIdAsync(item.Id);
        var presetNameOnB = (await _b.Presets.GetByIdAsync(preset.Id))!.Name;
        Assert.Multiple(() =>
        {
            Assert.That(presetNameOnB, Is.EqualTo("Locomotives"));
            Assert.That(((TextFieldValue)onB!.Values.Single()).Value, Is.EqualTo("Flying Scotsman"),
                "a preset rename must not wipe item field values on the peer");
        });
    }

    [Test]
    public async Task ItemEdit_PropagatesAtoB()
    {
        var preset = MakePreset("P");
        await _a.Presets.AddAsync(preset);
        var item = new Item { PresetId = preset.Id, DisplayName = "First" };
        await _a.Items.AddAsync(item);
        await SyncBothAsync();

        var local = await _a.Items.GetByIdAsync(item.Id);
        local!.DisplayName = "Renamed";
        await _a.Items.UpdateAsync(local);
        await SyncBothAsync();

        Assert.That((await _b.Items.GetByIdAsync(item.Id))!.DisplayName, Is.EqualTo("Renamed"));
    }

    [Test]
    public async Task SharedFieldEdit_PropagatesInPlace()
    {
        var sf = new SharedField { Name = "Year", Definition = new IntegerFieldDefinition { Label = "Year" } };
        sf.Definition.SharedFieldId = sf.Id;
        await _a.SharedFields.AddAsync(sf);
        await SyncBothAsync();

        var local = await _a.SharedFields.GetByIdAsync(sf.Id);
        local!.Name = "Release year";
        await _a.SharedFields.UpdateAsync(local);
        await SyncBothAsync();

        var onB = (await _b.Store.GetAllSharedFieldsAsync()).Where(x => x.Id == sf.Id).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(onB, Has.Count.EqualTo(1));
            Assert.That(onB[0].Name, Is.EqualTo("Release year"));
        });
    }

    [Test]
    public async Task PresetDelete_RemovesRowOnPeer()
    {
        var preset = MakePreset("Doomed");
        await _a.Presets.AddAsync(preset);
        await SyncBothAsync();

        await _a.Presets.DeleteAsync(preset.Id);
        await SyncBothAsync();

        var visible = await _b.Presets.GetByIdAsync(preset.Id);
        var rows = (await _b.Store.GetAllPresetsAsync()).Where(p => p.Id == preset.Id).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(visible, Is.Null);
            Assert.That(rows, Is.Empty, "a hard-deleted preset leaves no row on the peer");
        });
    }

    [Test]
    public async Task ItemDelete_PropagatesAsTombstone()
    {
        var preset = MakePreset("P");
        await _a.Presets.AddAsync(preset);
        var item = new Item { PresetId = preset.Id, DisplayName = "Temp" };
        await _a.Items.AddAsync(item);
        await SyncBothAsync();

        await _a.Items.DeleteAsync(item.Id);
        await SyncBothAsync();

        Assert.That(await _b.Items.GetByIdAsync(item.Id), Is.Null);
    }

    [Test]
    public async Task SharedFieldDelete_PropagatesAsTombstone()
    {
        var sf = new SharedField { Name = "Doomed", Definition = new IntegerFieldDefinition { Label = "Doomed" } };
        sf.Definition.SharedFieldId = sf.Id;
        await _a.SharedFields.AddAsync(sf);
        await SyncBothAsync();
        Assume.That((await _b.SharedFields.GetAllAsync()).Any(x => x.Id == sf.Id), Is.True, "precondition: field synced to B");

        await _a.SharedFields.DeleteAsync(sf.Id);
        await SyncBothAsync();

        var visible = (await _b.SharedFields.GetAllAsync()).Any(x => x.Id == sf.Id);
        var rows = (await _b.Store.GetAllSharedFieldsAsync()).Where(x => x.Id == sf.Id).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(visible, Is.False, "deleted shared field must disappear on the peer");
            Assert.That(rows, Is.Empty, "a hard-deleted shared field leaves no row on the peer");
        });
    }

    [Test]
    public async Task Bidirectional_NonConflictingEdits_BothConverge()
    {
        var p1 = MakePreset("A-collection");
        await _a.Presets.AddAsync(p1);
        var p2 = MakePreset("B-collection");
        await _b.Presets.AddAsync(p2);

        await _a.Sync.SyncAsync();
        await _b.Sync.SyncAsync();
        await _a.Sync.SyncAsync();

        var p2OnA = await _a.Presets.GetByIdAsync(p2.Id);
        var p1OnB = await _b.Presets.GetByIdAsync(p1.Id);
        Assert.Multiple(() =>
        {
            Assert.That(p2OnA?.Name, Is.EqualTo("B-collection"));
            Assert.That(p1OnB?.Name, Is.EqualTo("A-collection"));
        });
    }

    [Test]
    public async Task SameEntityEditedOnBoth_AutoMergesToOneWinner_NoConflicts()
    {
        var preset = MakePreset("Orig");
        await _a.Presets.AddAsync(preset);
        await SyncBothAsync();

        var a = await _a.Presets.GetByIdAsync(preset.Id);
        a!.Name = "A-edit";
        await _a.Presets.UpdateAsync(a);

        var b = await _b.Presets.GetByIdAsync(preset.Id);
        b!.Name = "B-edit";
        await _b.Presets.UpdateAsync(b);

        await _a.Sync.SyncAsync();
        await _b.Sync.SyncAsync();
        await _a.Sync.SyncAsync();
        await _b.Sync.SyncAsync();

        var aName = (await _a.Presets.GetByIdAsync(preset.Id))!.Name;
        var bName = (await _b.Presets.GetByIdAsync(preset.Id))!.Name;
        Assert.Multiple(() =>
        {
            Assert.That(aName, Is.EqualTo(bName), "both devices deterministically converge to the same winner with no conflict prompt");
            Assert.That(aName, Is.AnyOf("A-edit", "B-edit"));
        });
    }

    [Test]
    public async Task PresetReferencingSharedField_RoundTripsAndResolvesEffectiveFields()
    {
        var sf = new SharedField { Name = "Rarity", Definition = new TextFieldDefinition { Label = "Rarity" } };
        sf.Definition.SharedFieldId = sf.Id;
        await _a.SharedFields.AddAsync(sf);

        var preset = MakePreset("Cards");
        preset.SharedFieldRefs.Add(new PresetSharedField { PresetId = preset.Id, SharedFieldId = sf.Id, DisplayOrder = 1 });
        await _a.Presets.AddAsync(preset);

        await SyncBothAsync();

        var effective = await _b.PresetUseCase.GetEffectiveFieldsAsync(preset.Id);
        Assert.That(effective.Fields.Select(f => f.Label), Does.Contain("Rarity"));
    }

    [Test]
    public async Task ItemWithNestedListValues_RoundTrips()
    {
        var preset = new Preset { Name = "Albums" };
        var list = new ListFieldDefinition { Label = "Tracks", PresetId = preset.Id };
        var sub = new TextFieldDefinition { Label = "Track", ParentListFieldDefinitionId = list.Id };
        list.SubFields.Add(sub);
        preset.Fields.Add(list);
        await _a.Presets.AddAsync(preset);

        var item = new Item { PresetId = preset.Id, DisplayName = "Album 1" };
        var listValue = new ListFieldValue { FieldDefinitionId = list.Id, ItemId = item.Id };
        var entry = new ListEntry { ListFieldValueId = listValue.Id };
        entry.SubValues.Add(new TextFieldValue { FieldDefinitionId = sub.Id, Value = "Intro", ListEntryId = entry.Id });
        listValue.Entries.Add(entry);
        item.Values.Add(listValue);
        await _a.Items.AddAsync(item);

        await SyncBothAsync();

        var onB = await _b.Items.GetByIdAsync(item.Id);
        var clonedEntry = ((ListFieldValue)onB!.Values.Single()).Entries.Single();
        Assert.That(((TextFieldValue)clonedEntry.SubValues.Single()).Value, Is.EqualTo("Intro"));
    }

    private async Task<(Guid presetId, Guid itemId, string key)> SeedItemWithImageAsync(Client client, byte[] bytes)
    {
        var preset = new Preset { Name = "Trains" };
        var imageField = new ImageFieldDefinition { Label = "Photo", PresetId = preset.Id };
        preset.Fields.Add(imageField);
        await client.Presets.AddAsync(preset);

        string key;
        using (var ms = new MemoryStream(bytes))
            key = await client.Images.SaveAsync(ms, "loco.png");

        var item = new Item { PresetId = preset.Id, DisplayName = "Loco 42" };
        item.Values.Add(new ImageFieldValue { FieldDefinitionId = imageField.Id, ImageKey = key, ItemId = item.Id });
        await client.Items.AddAsync(item);
        return (preset.Id, item.Id, key);
    }

    [Test]
    public async Task Image_ReferencedByItem_PropagatesAtoB()
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        var (_, _, key) = await SeedItemWithImageAsync(_a, bytes);

        await SyncBothAsync();

        Assert.That(_b.Images.Exists(key), Is.True);
        using var stream = _b.Images.Open(key);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        Assert.That(buffer.ToArray(), Is.EqualTo(bytes));
    }

    [Test]
    public async Task Image_OfDeletedItem_IsGarbageCollectedOnBothDevices()
    {
        var (_, itemId, key) = await SeedItemWithImageAsync(_a, new byte[] { 9, 9, 9 });
        await SyncBothAsync();
        Assume.That(_b.Images.Exists(key), Is.True, "precondition: image propagated to B");

        await _a.Items.DeleteAsync(itemId);
        await SyncBothAsync();

        Assert.Multiple(() =>
        {
            Assert.That(_a.Images.Exists(key), Is.False, "a hard-deleted item's image is no longer referenced and is cleaned up");
            Assert.That(_b.Images.Exists(key), Is.False, "the peer drops the orphaned image once the deletion propagates");
        });
    }


    [Test]
    public async Task RepeatedSync_IsStableWithNoChurn()
    {
        await _a.Presets.AddAsync(MakePreset("Coins"));
        await SyncBothAsync();

        var second = await _b.Sync.SyncAsync();

        Assert.Multiple(() =>
        {
            Assert.That(second.Pushed, Is.EqualTo(0));
            Assert.That(second.Pulled, Is.EqualTo(0), "a no-op second sync transfers nothing");
        });
    }
}

internal sealed class AlwaysConfiguredSyncStatus : ISyncStatus
{
    public bool IsConfigured => true;
    public int TombstoneRetentionDays => 30;
}

internal sealed class AllowAllAuthorization : ICollectionAuthorization
{
    public Task<bool> CanReadAsync(Guid presetId) => Task.FromResult(true);
    public Task<bool> CanWriteAsync(Guid presetId) => Task.FromResult(true);
    public Task<bool> IsOwnerAsync(Guid presetId) => Task.FromResult(true);
}

using System.IO.Compression;
using System.Text;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using FakeItEasy;

namespace Collectary.Core.Tests.UseCases;

[TestFixture]
public class BackupServiceTest
{
    private ISyncStore _store = null!;
    private ISyncSerializer _serializer = null!;
    private IImageStore _imageStore = null!;
    private BackupService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _store = A.Fake<ISyncStore>();
        _serializer = A.Fake<ISyncSerializer>();
        _imageStore = A.Fake<IImageStore>();
        A.CallTo(() => _store.GetAllPresetsAsync()).Returns(Array.Empty<Preset>());
        A.CallTo(() => _store.GetAllItemsAsync()).Returns(Array.Empty<Item>());
        A.CallTo(() => _store.GetAllSharedFieldsAsync()).Returns(Array.Empty<SharedField>());
        A.CallTo(() => _store.GetAllUsersAsync()).Returns(Array.Empty<User>());
        A.CallTo(() => _store.GetAllSharesAsync()).Returns(Array.Empty<CollectionShare>());
        A.CallTo(() => _store.GetReferencedImageKeysAsync()).Returns(Array.Empty<string>());
        _sut = new BackupService(_store, _serializer, _imageStore);
    }

    private static ZipArchive OpenRead(MemoryStream stream)
    {
        stream.Position = 0;
        return new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
    }

    private static string ReadEntry(ZipArchive archive, string name)
    {
        using var reader = new StreamReader(archive.GetEntry(name)!.Open());
        return reader.ReadToEnd();
    }

    [Test]
    public async Task ExportAsync_WritesManifestEntitiesAndBlobs()
    {
        var preset = new Preset { Id = Guid.NewGuid(), Name = "P", Revision = 1 };
        var item = new Item { Id = Guid.NewGuid(), DisplayName = "I", Revision = 1 };
        var field = new SharedField { Id = Guid.NewGuid(), Name = "F", Revision = 1, Definition = new Collectary.Core.Domain.Fields.TextFieldDefinition() };
        A.CallTo(() => _store.GetAllPresetsAsync()).Returns(new[] { preset });
        A.CallTo(() => _store.GetAllItemsAsync()).Returns(new[] { item });
        A.CallTo(() => _store.GetAllSharedFieldsAsync()).Returns(new[] { field });
        A.CallTo(() => _store.GetReferencedImageKeysAsync()).Returns(new[] { "blob-key" });
        A.CallTo(() => _imageStore.Exists("blob-key")).Returns(true);
        A.CallTo(() => _serializer.Serialize(preset)).Returns("PRESET");
        A.CallTo(() => _serializer.Serialize(item)).Returns("ITEM");
        A.CallTo(() => _serializer.Serialize(field)).Returns("FIELD");
        A.CallTo(() => _imageStore.Open("blob-key")).ReturnsLazily(() => new MemoryStream(Encoding.UTF8.GetBytes("PNGBYTES")));

        using var output = new MemoryStream();
        await _sut.ExportAsync(output);

        using var archive = OpenRead(output);
        Assert.Multiple(() =>
        {
            Assert.That(archive.GetEntry("manifest.json"), Is.Not.Null);
            Assert.That(ReadEntry(archive, $"presets/{preset.Id:N}.json"), Is.EqualTo("PRESET"));
            Assert.That(ReadEntry(archive, $"items/{item.Id:N}.json"), Is.EqualTo("ITEM"));
            Assert.That(ReadEntry(archive, $"sharedfields/{field.Id:N}.json"), Is.EqualTo("FIELD"));
            Assert.That(archive.Entries.Count(e => e.FullName.StartsWith("blobs/")), Is.EqualTo(1));
        });

        var blobEntry = archive.Entries.Single(e => e.FullName.StartsWith("blobs/"));
        using var blobReader = new StreamReader(blobEntry.Open());
        Assert.That(blobReader.ReadToEnd(), Is.EqualTo("PNGBYTES"));
    }

    private MemoryStream BuildZip(Action<ZipArchive> build)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            build(archive);
        stream.Position = 0;
        return stream;
    }

    private static void AddText(ZipArchive archive, string name, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(name).Open());
        writer.Write(content);
    }

    [Test]
    public async Task ExportAsync_IncludesProfilesAndShares()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "alice", DisplayName = "Alice", Revision = 1 };
        var share = new CollectionShare { Id = Guid.NewGuid(), PresetId = Guid.NewGuid(), SharedWithUserId = Guid.NewGuid(), GrantedByUserId = Guid.NewGuid(), Revision = 1 };
        A.CallTo(() => _store.GetAllUsersAsync()).Returns(new[] { user });
        A.CallTo(() => _store.GetAllSharesAsync()).Returns(new[] { share });
        A.CallTo(() => _serializer.Serialize(user)).Returns("USER");
        A.CallTo(() => _serializer.Serialize(share)).Returns("SHARE");

        using var output = new MemoryStream();
        await _sut.ExportAsync(output);

        using var archive = OpenRead(output);
        Assert.Multiple(() =>
        {
            Assert.That(ReadEntry(archive, $"users/{user.Id:N}.json"), Is.EqualTo("USER"),
                "a backup must include user profiles or restoring it loses them");
            Assert.That(ReadEntry(archive, $"shares/{share.Id:N}.json"), Is.EqualTo("SHARE"),
                "a backup must include collection shares or restoring it loses access grants");
        });
    }

    [Test]
    public async Task ImportAsync_AppliesProfilesAndShares()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "alice", DisplayName = "Alice", Revision = 1 };
        var share = new CollectionShare { Id = Guid.NewGuid(), PresetId = Guid.NewGuid(), SharedWithUserId = Guid.NewGuid(), GrantedByUserId = Guid.NewGuid(), Revision = 1 };
        A.CallTo(() => _serializer.Deserialize<User>("U")).Returns(user);
        A.CallTo(() => _serializer.Deserialize<CollectionShare>("S")).Returns(share);
        using var zip = BuildZip(a =>
        {
            AddText(a, $"users/{user.Id:N}.json", "U");
            AddText(a, $"shares/{share.Id:N}.json", "S");
        });

        var result = await _sut.ImportAsync(zip);

        Assert.Multiple(() =>
        {
            Assert.That(result.Applied, Is.EqualTo(2));
        });
        A.CallTo(() => _store.ApplyUserAsync(A<User>.That.Matches(u => u.Id == user.Id && !u.IsDirty))).MustHaveHappenedOnceExactly();
        A.CallTo(() => _store.ApplyShareAsync(A<CollectionShare>.That.Matches(s => s.Id == share.Id && !s.IsDirty))).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ImportAsync_AppliesRemoteWhenLocalAbsent()
    {
        var remote = new Preset { Id = Guid.NewGuid(), Name = "Remote", Revision = 3 };
        A.CallTo(() => _serializer.Deserialize<Preset>("R")).Returns(remote);
        using var zip = BuildZip(a => AddText(a, $"presets/{remote.Id:N}.json", "R"));

        var result = await _sut.ImportAsync(zip);

        A.CallTo(() => _store.ApplyPresetAsync(A<Preset>.That.Matches(p => p.Id == remote.Id && !p.IsDirty && p.BaseRevision == 3)))
            .MustHaveHappenedOnceExactly();
        Assert.That(result.Applied, Is.EqualTo(1));
        Assert.That(result.Conflicts, Is.Empty);
    }

    [Test]
    public async Task ImportAsync_RecordsConflictWhenLocalDirtyAndBackupNewer_AndDoesNotApply()
    {
        var id = Guid.NewGuid();
        var local = new Preset { Id = id, Name = "Local", Revision = 3, BaseRevision = 1, IsDirty = true };
        var remote = new Preset { Id = id, Name = "Remote", Revision = 5 };
        A.CallTo(() => _store.GetAllPresetsAsync()).Returns(new[] { local });
        A.CallTo(() => _serializer.Deserialize<Preset>("R")).Returns(remote);
        using var zip = BuildZip(a => AddText(a, $"presets/{id:N}.json", "R"));

        var result = await _sut.ImportAsync(zip);

        A.CallTo(() => _store.ApplyPresetAsync(A<Preset>._)).MustNotHaveHappened();
        Assert.That(result.Conflicts.Single().Id, Is.EqualTo(id));
        Assert.That(result.Conflicts.Single().LocalLabel, Is.EqualTo("Local"));
        Assert.That(result.Applied, Is.Zero);
    }

    [Test]
    public async Task ImportAsync_WhenBackupOlderThanCleanLocal_KeepsLocalAndDoesNotApply()
    {
        var id = Guid.NewGuid();
        var local = new Preset { Id = id, Name = "Local", Revision = 5, BaseRevision = 5, IsDirty = false };
        var remote = new Preset { Id = id, Name = "Remote", Revision = 3 };
        A.CallTo(() => _store.GetAllPresetsAsync()).Returns(new[] { local });
        A.CallTo(() => _serializer.Deserialize<Preset>("R")).Returns(remote);
        using var zip = BuildZip(a => AddText(a, $"presets/{id:N}.json", "R"));

        var result = await _sut.ImportAsync(zip);

        A.CallTo(() => _store.ApplyPresetAsync(A<Preset>._)).MustNotHaveHappened();
        Assert.That(result.Applied, Is.Zero);
        Assert.That(result.Conflicts, Is.Empty);
    }

    [Test]
    public async Task ImportAsync_WhenBackupNewerThanCleanLocal_Applies()
    {
        var id = Guid.NewGuid();
        var local = new Preset { Id = id, Name = "Local", Revision = 2, BaseRevision = 2, IsDirty = false };
        var remote = new Preset { Id = id, Name = "Remote", Revision = 5 };
        A.CallTo(() => _store.GetAllPresetsAsync()).Returns(new[] { local });
        A.CallTo(() => _serializer.Deserialize<Preset>("R")).Returns(remote);
        using var zip = BuildZip(a => AddText(a, $"presets/{id:N}.json", "R"));

        var result = await _sut.ImportAsync(zip);

        A.CallTo(() => _store.ApplyPresetAsync(A<Preset>.That.Matches(p => p.Id == id && p.BaseRevision == 5 && !p.IsDirty)))
            .MustHaveHappenedOnceExactly();
        Assert.That(result.Applied, Is.EqualTo(1));
        Assert.That(result.Conflicts, Is.Empty);
    }

    [Test]
    public async Task ImportAsync_WhenBackupSameRevisionAsLocal_Skips()
    {
        var id = Guid.NewGuid();
        var local = new Preset { Id = id, Name = "Local", Revision = 2, BaseRevision = 2, IsDirty = false };
        var remote = new Preset { Id = id, Name = "Remote", Revision = 2 };
        A.CallTo(() => _store.GetAllPresetsAsync()).Returns(new[] { local });
        A.CallTo(() => _serializer.Deserialize<Preset>("R")).Returns(remote);
        using var zip = BuildZip(a => AddText(a, $"presets/{id:N}.json", "R"));

        var result = await _sut.ImportAsync(zip);

        A.CallTo(() => _store.ApplyPresetAsync(A<Preset>._)).MustNotHaveHappened();
        Assert.That(result.Applied, Is.Zero);
        Assert.That(result.Conflicts, Is.Empty);
    }

    [Test]
    public async Task ImportAsync_AppliesSharedFieldsBeforePresets()
    {
        var sysId = Guid.NewGuid();
        var presetId = Guid.NewGuid();
        var sf = new SharedField { Id = sysId, Name = "S", Revision = 1, Definition = new Collectary.Core.Domain.Fields.TextFieldDefinition() };
        var preset = new Preset { Id = presetId, Name = "P", Revision = 1 };
        A.CallTo(() => _serializer.Deserialize<SharedField>("S")).Returns(sf);
        A.CallTo(() => _serializer.Deserialize<Preset>("P")).Returns(preset);
        using var zip = BuildZip(a =>
        {
            AddText(a, $"presets/{presetId:N}.json", "P");
            AddText(a, $"sharedfields/{sysId:N}.json", "S");
        });

        await _sut.ImportAsync(zip);

        A.CallTo(() => _store.ApplySharedFieldAsync(A<SharedField>._)).MustHaveHappened()
            .Then(A.CallTo(() => _store.ApplyPresetAsync(A<Preset>._)).MustHaveHappened());
    }

    [Test]
    public async Task ImportAsync_SkipsEntryThatDeserializesToNull()
    {
        A.CallTo(() => _serializer.Deserialize<Preset>("R")).Returns((Preset)null!);
        using var zip = BuildZip(a => AddText(a, $"presets/{Guid.NewGuid():N}.json", "R"));

        var result = await _sut.ImportAsync(zip);

        A.CallTo(() => _store.ApplyPresetAsync(A<Preset>._)).MustNotHaveHappened();
        Assert.That(result.Applied, Is.Zero);
    }

    [Test]
    public void ImportAsync_SkipsBlobEntryWithUndecodableName()
    {
        A.CallTo(() => _imageStore.Exists(A<string>._)).Returns(false);
        using var zip = BuildZip(a => AddText(a, "blobs/not!base64", "x"));

        Assert.That(async () => await _sut.ImportAsync(zip), Throws.Nothing);
        A.CallTo(() => _imageStore.ImportAsync(A<string>._, A<Stream>._)).MustNotHaveHappened();
    }

    [Test]
    public void ImportAsync_ThrowsOnUnsupportedFormatVersion()
    {
        using var zip = BuildZip(a => AddText(a, "manifest.json", "{\"formatVersion\":2}"));

        Assert.That(async () => await _sut.ImportAsync(zip),
            Throws.InstanceOf<NotSupportedException>().With.Message.Contains("2"));
    }

    [Test]
    public async Task ImportAsync_CountsAppliedItems()
    {
        var item = new Item { Id = Guid.NewGuid(), DisplayName = "I", Revision = 1 };
        A.CallTo(() => _serializer.Deserialize<Item>("I")).Returns(item);
        using var zip = BuildZip(a => AddText(a, $"items/{item.Id:N}.json", "I"));

        var result = await _sut.ImportAsync(zip);

        A.CallTo(() => _store.ApplyItemAsync(A<Item>.That.Matches(i => i.Id == item.Id))).MustHaveHappenedOnceExactly();
        Assert.That(result.Applied, Is.EqualTo(1));
    }

    [Test]
    public void ExportAsync_SkipsReferencedBlobThatIsMissing()
    {
        A.CallTo(() => _store.GetReferencedImageKeysAsync()).Returns(new[] { "missing", "present" });
        A.CallTo(() => _imageStore.Exists("missing")).Returns(false);
        A.CallTo(() => _imageStore.Exists("present")).Returns(true);
        A.CallTo(() => _imageStore.Open("present")).ReturnsLazily(() => new MemoryStream(Encoding.UTF8.GetBytes("P")));

        using var output = new MemoryStream();
        Assert.That(async () => await _sut.ExportAsync(output), Throws.Nothing);

        using var archive = OpenRead(output);
        Assert.That(archive.Entries.Count(e => e.FullName.StartsWith("blobs/")), Is.EqualTo(1));
        A.CallTo(() => _imageStore.Open("missing")).MustNotHaveHappened();
    }

    [Test]
    public async Task ImportAsync_NeverDeletesLocalsAbsentFromZip()
    {
        A.CallTo(() => _store.GetAllPresetsAsync()).Returns(new[] { new Preset { Id = Guid.NewGuid(), Name = "Keep" } });
        using var zip = BuildZip(_ => { });

        await _sut.ImportAsync(zip);

        A.CallTo(() => _store.DeleteLocallyAsync(A<SyncEntityKind>._, A<Guid>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task ImportAsync_IgnoresBareDirectoryEntries()
    {
        using var zip = BuildZip(a => a.CreateEntry("presets/"));

        var result = await _sut.ImportAsync(zip);

        A.CallTo(() => _serializer.Deserialize<Preset>(A<string>._)).MustNotHaveHappened();
        Assert.That(result.Applied, Is.Zero);
    }

    [Test]
    public async Task ImportAsync_ImportsBlobWhenAbsentAndSkipsWhenPresent()
    {
        A.CallTo(() => _imageStore.Exists("present")).Returns(true);
        A.CallTo(() => _imageStore.Exists("absent")).Returns(false);
        using var zip = BuildZip(a =>
        {
            AddText(a, "blobs/" + Convert.ToBase64String(Encoding.UTF8.GetBytes("present")).Replace('+', '-').Replace('/', '_'), "x");
            AddText(a, "blobs/" + Convert.ToBase64String(Encoding.UTF8.GetBytes("absent")).Replace('+', '-').Replace('/', '_'), "y");
        });

        await _sut.ImportAsync(zip);

        A.CallTo(() => _imageStore.ImportAsync("absent", A<Stream>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _imageStore.ImportAsync("present", A<Stream>._)).MustNotHaveHappened();
    }
}

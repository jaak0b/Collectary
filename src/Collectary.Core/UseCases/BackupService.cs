using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Collectary.Core.Domain;
using Collectary.Core.Ports;

namespace Collectary.Core.UseCases;

public class BackupService : IBackupService
{
    private const int FormatVersion = 1;
    private const string ManifestEntry = "manifest.json";
    private const string ManifestContent = "{\"formatVersion\":1}";
    private const string BlobDir = "blobs/";

    private readonly ISyncStore _store;
    private readonly ISyncSerializer _serializer;
    private readonly IImageStore _imageStore;
    private readonly SyncKindCatalog _catalog = new();

    public BackupService(ISyncStore store, ISyncSerializer serializer, IImageStore imageStore)
    {
        _store = store;
        _serializer = serializer;
        _imageStore = imageStore;
    }

    public async Task ExportAsync(Stream output)
    {
        using var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);

        await WriteEntryAsync(archive, ManifestEntry, ManifestContent);

        foreach (var kind in _catalog.Describe(_store, _serializer))
            foreach (var entity in await kind.GetLocal())
                await WriteEntryAsync(archive, $"{kind.WireString}/{EntryName(((DomainObject)entity).Id)}", kind.Serialize(entity));

        foreach (var key in await _store.GetReferencedImageKeysAsync())
            if (_imageStore.Exists(key))
                await WriteBlobAsync(archive, key);
    }

    public async Task<BackupImportResult> ImportAsync(Stream input)
    {
        using var archive = new ZipArchive(input, ZipArchiveMode.Read);
        await EnsureSupportedFormatAsync(archive);
        var conflicts = new List<SyncConflict>();

        var applied = 0;
        foreach (var kind in _catalog.Describe(_store, _serializer))
            applied += await MergeAsync(archive, kind, conflicts);

        await ImportBlobsAsync(archive);

        return new BackupImportResult(applied, conflicts);
    }

    private async Task EnsureSupportedFormatAsync(ZipArchive archive)
    {
        var entry = archive.GetEntry(ManifestEntry);
        if (entry is null) return;

        using var doc = JsonDocument.Parse(await ReadEntryAsync(entry));
        if (doc.RootElement.TryGetProperty("formatVersion", out var version)
            && version.TryGetInt32(out var value)
            && value != FormatVersion)
            throw new NotSupportedException($"Backup format version {value} is not supported.");
    }

    private async Task<int> MergeAsync(ZipArchive archive, SyncKind kind, List<SyncConflict> conflicts)
    {
        var localById = (await kind.GetLocal()).ToDictionary(l => ((DomainObject)l).Id);
        var applied = 0;

        foreach (var entry in EntriesIn(archive, $"{kind.WireString}/"))
        {
            var remote = kind.Deserialize(await ReadEntryAsync(entry));
            if (remote is null) continue;

            var remoteId = ((DomainObject)remote).Id;
            if (localById.TryGetValue(remoteId, out var local))
            {
                if (remote.Revision <= local.Revision) continue;
                if (local.IsDirty)
                {
                    conflicts.Add(new SyncConflict(kind.Kind, remoteId, kind.Label(local), kind.Label(remote), local.Revision, remote.Revision));
                    continue;
                }
            }

            remote.MarkPulled();
            await kind.Apply(remote);
            applied++;
        }

        return applied;
    }

    private async Task ImportBlobsAsync(ZipArchive archive)
    {
        foreach (var entry in EntriesIn(archive, BlobDir))
        {
            var key = DecodeKey(entry.FullName[BlobDir.Length..]);
            if (key is null) continue;
            if (_imageStore.Exists(key)) continue;
            await using var stream = entry.Open();
            await _imageStore.ImportAsync(key, stream);
        }
    }

    private IEnumerable<ZipArchiveEntry> EntriesIn(ZipArchive archive, string dir) =>
        archive.Entries.Where(e =>
            e.FullName.StartsWith(dir, StringComparison.Ordinal) && e.FullName.Length > dir.Length);

    private async Task WriteEntryAsync(ZipArchive archive, string name, string content)
    {
        await using var stream = archive.CreateEntry(name).Open();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
    }

    private async Task WriteBlobAsync(ZipArchive archive, string key)
    {
        await using var dest = archive.CreateEntry(BlobDir + EncodeKey(key)).Open();
        await using var src = _imageStore.Open(key);
        await src.CopyToAsync(dest);
    }

    private async Task<string> ReadEntryAsync(ZipArchiveEntry entry)
    {
        await using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private string EntryName(Guid id) => $"{id:N}.json";

    private string EncodeKey(string key) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(key)).Replace('+', '-').Replace('/', '_');

    private string? DecodeKey(string name)
    {
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(name.Replace('-', '+').Replace('_', '/')));
        }
        catch (FormatException)
        {
            return null;
        }
    }
}

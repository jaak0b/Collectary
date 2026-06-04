namespace Collectary.Infrastructure.Sync;

/// <summary>
/// Single source of truth for the sync document layout shared by every <c>ISyncBackend</c>:
/// JSON documents are named <c>{id:N}.{revision}.json</c>; blob keys are validated to be a plain
/// file name (no path separators or traversal).
/// </summary>
public class SyncFileNaming
{
    public string DocumentName(Guid id, long revision) => $"{id:N}.{revision}.json";

    public bool TryParseDocument(string fileName, out Guid id, out long revision)
    {
        id = Guid.Empty;
        revision = 0;

        var name = fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? fileName[..^".json".Length]
            : fileName;

        var dot = name.LastIndexOf('.');
        if (dot <= 0) return false;

        return Guid.TryParse(name[..dot], out id) && long.TryParse(name[(dot + 1)..], out revision);
    }

    public bool BelongsTo(string fileName, Guid id) =>
        fileName.StartsWith($"{id:N}.", StringComparison.OrdinalIgnoreCase)
        && fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

    public string SafeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key is "." or ".."
            || Path.GetFileName(key) != key
            || key.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException($"Unsafe blob key: '{key}'", nameof(key));
        return key;
    }
}

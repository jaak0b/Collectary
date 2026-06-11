namespace Collectary.Infrastructure.Sync;

/// <summary>
/// Single source of truth for the sync document layout shared by every <c>ISyncBackend</c>:
/// JSON documents are named <c>{id:N}.json</c>; blob keys are validated to be a plain
/// file name (no path separators or traversal).
/// </summary>
public class SyncFileNaming
{
    public string DocumentName(Guid id) => $"{id:N}.json";

    public bool TryParseId(string fileName, out Guid id)
    {
        id = Guid.Empty;
        if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) return false;
        return Guid.TryParse(fileName[..^".json".Length], out id);
    }

    public bool BelongsTo(string fileName, Guid id) =>
        fileName.StartsWith($"{id:N}.", StringComparison.OrdinalIgnoreCase)
        && fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

    // A fixed set of disallowed characters rather than Path.GetInvalidFileNameChars(), which varies by
    // OS — a blob key stored on Android (Linux) must validate identically on a Windows desktop.
    private static readonly char[] ReservedChars = "\\/:*?\"<>|".ToCharArray();

    public string SafeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key is "." or ".."
            || key.IndexOfAny(ReservedChars) >= 0
            || key.Any(char.IsControl))
            throw new ArgumentException($"Unsafe blob key: '{key}'", nameof(key));
        return key;
    }
}

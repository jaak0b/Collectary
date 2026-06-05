namespace Collectary.Core.Ports;

public record BackupImportResult(int Applied, IReadOnlyList<SyncConflict> Conflicts)
{
    public bool HasConflicts => Conflicts.Count > 0;
}

public interface IBackupService
{
    Task ExportAsync(Stream output);
    Task<BackupImportResult> ImportAsync(Stream input);
}

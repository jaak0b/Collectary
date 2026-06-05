using Collectary.Core.Ports;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class SettingsViewModelBackupTest
{
    private string _dir = null!;
    private string _original = null!;

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_dir);
        _original = AppPreferences.FilePath;
        AppPreferences.FilePath = Path.Combine(_dir, "preferences.json");
    }

    [TearDown]
    public void TearDown()
    {
        AppPreferences.FilePath = _original;
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private static SettingsViewModel Make(
        Func<Task<bool>>? exportBackup = null,
        Func<Task<BackupImportResult?>>? importBackup = null) =>
        new(() => { }, exportBackup: exportBackup, importBackup: importBackup);

    [Test]
    public async Task ExportBackup_WhenWritten_SetsStatus()
    {
        var sut = Make(exportBackup: () => Task.FromResult(true));

        await sut.ExportBackupCommand.ExecuteAsync(null);

        Assert.That(sut.BackupStatus, Is.EqualTo(LocalizationService.Instance["Backup_Exported"]));
    }

    [Test]
    public async Task ExportBackup_WhenCancelled_LeavesStatusNull()
    {
        var sut = Make(exportBackup: () => Task.FromResult(false));
        sut.BackupStatus = "stale";

        await sut.ExportBackupCommand.ExecuteAsync(null);

        Assert.That(sut.BackupStatus, Is.Null);
    }

    [Test]
    public async Task ExportBackup_WhenCallbackThrows_SetsErrorStatus()
    {
        var sut = Make(exportBackup: () => throw new InvalidOperationException("boom"));

        await sut.ExportBackupCommand.ExecuteAsync(null);

        Assert.That(sut.BackupStatus, Is.EqualTo(LocalizationService.Instance["Backup_Error"]));
    }

    [Test]
    public async Task ImportBackup_NoConflicts_ReportsAppliedCount()
    {
        var sut = Make(importBackup: () => Task.FromResult<BackupImportResult?>(new BackupImportResult(3, Array.Empty<SyncConflict>())));

        await sut.ImportBackupCommand.ExecuteAsync(null);

        Assert.That(sut.BackupStatus, Is.EqualTo(string.Format(LocalizationService.Instance["Backup_Imported"], 3)));
    }

    [Test]
    public async Task ImportBackup_WithConflicts_ListsConflictLabels()
    {
        var first = new SyncConflict(SyncEntityKind.Item, Guid.NewGuid(), "Alpha", "RemoteA", 5, 2);
        var second = new SyncConflict(SyncEntityKind.Item, Guid.NewGuid(), "Beta", "RemoteB", 5, 2);
        var sut = Make(importBackup: () => Task.FromResult<BackupImportResult?>(new BackupImportResult(2, new[] { first, second })));

        await sut.ImportBackupCommand.ExecuteAsync(null);

        Assert.That(sut.BackupStatus, Does.Contain("Alpha, Beta"));
    }

    [Test]
    public async Task ImportBackup_WhenCancelled_LeavesStatusNull()
    {
        var sut = Make(importBackup: () => Task.FromResult<BackupImportResult?>(null));
        sut.BackupStatus = "stale";

        await sut.ImportBackupCommand.ExecuteAsync(null);

        Assert.That(sut.BackupStatus, Is.Null);
    }

    [Test]
    public async Task ImportBackup_WhenCallbackThrows_SetsErrorStatus()
    {
        var sut = Make(importBackup: () => throw new InvalidOperationException("boom"));

        await sut.ImportBackupCommand.ExecuteAsync(null);

        Assert.That(sut.BackupStatus, Is.EqualTo(LocalizationService.Instance["Backup_Error"]));
    }
}

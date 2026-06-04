using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class SettingsViewModelSyncTest
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

    private static SettingsViewModel Make(Func<Task<string?>>? pickFolder = null) =>
        new(() => { }, pickFolder);

    [Test]
    public async Task ChooseSyncFolder_SetsLocationAndPersists()
    {
        var sut = Make(() => Task.FromResult<string?>(@"C:\shared\collectary"));

        await sut.ChooseSyncFolderCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(sut.SyncLocation, Is.EqualTo(@"C:\shared\collectary"));
            Assert.That(sut.IsSyncConfigured, Is.True);
            Assert.That(AppPreferences.Load().SyncLocation, Is.EqualTo(@"C:\shared\collectary"));
        });
    }

    [Test]
    public async Task ChooseSyncFolder_WhenPickerReturnsNull_KeepsExisting()
    {
        var sut = Make(() => Task.FromResult<string?>(null));
        sut.SyncLocation = @"C:\existing";

        await sut.ChooseSyncFolderCommand.ExecuteAsync(null);

        Assert.That(sut.SyncLocation, Is.EqualTo(@"C:\existing"));
    }

    [Test]
    public void DisableSync_ClearsLocation()
    {
        var sut = Make();
        sut.SyncLocation = @"C:\shared";

        sut.DisableSyncCommand.Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(sut.SyncLocation, Is.Null);
            Assert.That(sut.IsSyncConfigured, Is.False);
            Assert.That(AppPreferences.Load().SyncLocation, Is.Null);
        });
    }

    [Test]
    public void AutoSyncSettings_Persist()
    {
        var sut = Make();
        sut.SyncLocation = @"C:\shared";

        sut.AutoSyncEnabled = true;
        sut.AutoSyncIntervalMinutes = 15;

        var prefs = AppPreferences.Load();
        Assert.Multiple(() =>
        {
            Assert.That(prefs.AutoSyncEnabled, Is.True);
            Assert.That(prefs.AutoSyncIntervalMinutes, Is.EqualTo(15));
        });
    }

    [Test]
    public void Constructor_LoadsSyncPreferences()
    {
        AppPreferences.Save(AppPreferences.Load() with
        {
            SyncLocation = @"C:\loaded",
            AutoSyncEnabled = true,
            AutoSyncIntervalMinutes = 9,
        });

        var sut = Make();

        Assert.Multiple(() =>
        {
            Assert.That(sut.SyncLocation, Is.EqualTo(@"C:\loaded"));
            Assert.That(sut.AutoSyncEnabled, Is.True);
            Assert.That(sut.AutoSyncIntervalMinutes, Is.EqualTo(9));
        });
    }
}

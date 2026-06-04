using Collectary.Presentation.Services;

namespace Collectary.UI.Tests.Services;

[TestFixture]
public class AppPreferencesTest
{
    private string _dir = null!;
    private string _original = null!;

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
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

    [Test]
    public void Update_AppliesMutationAndPersists()
    {
        AppPreferences.Save(new AppPreferencesData());

        var result = AppPreferences.Update(p => p with { AutoSyncEnabled = true, SyncLocation = "X" });

        Assert.Multiple(() =>
        {
            Assert.That(result.AutoSyncEnabled, Is.True);
            Assert.That(AppPreferences.Load().SyncLocation, Is.EqualTo("X"));
        });
    }

    [Test]
    public async Task Update_ConcurrentMutations_NeverCorruptFileAndDoNotLoseDistinctFields()
    {
        AppPreferences.Save(new AppPreferencesData());

        var flipSidebar = Task.Run(() => AppPreferences.Update(p => p with { SidebarOpen = false }));
        var flipAutoSync = Task.Run(() => AppPreferences.Update(p => p with { AutoSyncEnabled = true }));
        var setLocation = Task.Run(() => AppPreferences.Update(p => p with { SyncLocation = "loc" }));
        await Task.WhenAll(flipSidebar, flipAutoSync, setLocation);

        var final = AppPreferences.Load();
        Assert.Multiple(() =>
        {
            Assert.That(final.SidebarOpen, Is.False);
            Assert.That(final.AutoSyncEnabled, Is.True);
            Assert.That(final.SyncLocation, Is.EqualTo("loc"));
        });
    }
}

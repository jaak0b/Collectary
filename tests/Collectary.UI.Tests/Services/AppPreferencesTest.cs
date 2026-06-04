using Collectary.Core.Domain;
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
    public void RoundTrips_ThemingFields()
    {
        AppPreferences.Save(new AppPreferencesData(
            ColorTheme: "Nord", Skin: "Flat", AccentColor: "#FF8800"));

        var loaded = AppPreferences.Load();

        Assert.Multiple(() =>
        {
            Assert.That(loaded.ColorTheme, Is.EqualTo("Nord"));
            Assert.That(loaded.Skin, Is.EqualTo("Flat"));
            Assert.That(loaded.AccentColor, Is.EqualTo("#FF8800"));
        });
    }

    [Test]
    public void RoundTrips_CustomColorsAndExpertMode()
    {
        AppPreferences.Save(new AppPreferencesData(
            CustomColors: new Dictionary<string, string> { ["Background"] = "#FF00FF", ["TextPrimary"] = "#101010" },
            ExpertColorMode: true));

        var loaded = AppPreferences.Load();

        Assert.Multiple(() =>
        {
            Assert.That(loaded.ExpertColorMode, Is.True);
            Assert.That(loaded.CustomColors, Is.Not.Null);
            Assert.That(loaded.CustomColors!["Background"], Is.EqualTo("#FF00FF"));
            Assert.That(loaded.CustomColors!["TextPrimary"], Is.EqualTo("#101010"));
        });
    }

    [Test]
    public void Update_PreservesCustomColors_AlongsideOtherFields()
    {
        AppPreferences.Save(new AppPreferencesData(
            CustomColors: new Dictionary<string, string> { ["Border"] = "#222222" }));

        AppPreferences.Update(p => p with { SidebarOpen = false });

        var final = AppPreferences.Load();
        Assert.Multiple(() =>
        {
            Assert.That(final.SidebarOpen, Is.False);
            Assert.That(final.CustomColors!["Border"], Is.EqualTo("#222222"));
        });
    }

    [Test]
    public void Default_FieldLabelLayout_IsAdaptive() =>
        Assert.That(new AppPreferencesData().FieldLabelLayout, Is.EqualTo(FieldLabelLayout.Adaptive));

    [Test]
    public void RoundTrips_FieldLabelLayout()
    {
        AppPreferences.Save(new AppPreferencesData(FieldLabelLayout: FieldLabelLayout.Above));

        Assert.That(AppPreferences.Load().FieldLabelLayout, Is.EqualTo(FieldLabelLayout.Above));
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
        // ConfigureAwait(false): the headless Avalonia SynchronizationContext installed by
        // SetupWithoutStarting never pumps a message loop, so resuming on it would deadlock when
        // this fixture runs after a control-creating Flows test in the full suite.
        await Task.WhenAll(flipSidebar, flipAutoSync, setLocation).ConfigureAwait(false);

        var final = AppPreferences.Load();
        Assert.Multiple(() =>
        {
            Assert.That(final.SidebarOpen, Is.False);
            Assert.That(final.AutoSyncEnabled, Is.True);
            Assert.That(final.SyncLocation, Is.EqualTo("loc"));
        });
    }
}

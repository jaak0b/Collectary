using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class AppPreferencesTest
{
    private string _original = null!;
    private string _dir = null!;

    [SetUp]
    public void SetUp()
    {
        _original = AppPreferences.FilePath;
        _dir = Path.Combine(Path.GetTempPath(), $"collectary-prefs-{Guid.NewGuid():N}");
        AppPreferences.FilePath = Path.Combine(_dir, "preferences.json");
    }

    [TearDown]
    public void TearDown()
    {
        AppPreferences.FilePath = _original;
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Test]
    public void Load_WhenFileMissing_ReturnsDefaults()
    {
        var data = AppPreferences.Load();

        Assert.That(data, Is.EqualTo(new AppPreferencesData()));
    }

    [Test]
    public void Save_CreatesDirectoryAndFile()
    {
        AppPreferences.Save(new AppPreferencesData());

        Assert.That(File.Exists(AppPreferences.FilePath), Is.True);
    }

    [Test]
    public void SaveThenLoad_RoundTripsAllFields()
    {
        var saved = new AppPreferencesData(AppTheme.Dark, "de", 0.73);

        AppPreferences.Save(saved);
        var loaded = AppPreferences.Load();

        Assert.That(loaded.Theme, Is.EqualTo(AppTheme.Dark));
        Assert.That(loaded.Language, Is.EqualTo("de"));
        Assert.That(loaded.FieldPaneRatio, Is.EqualTo(0.73));
    }

    [Test]
    public void Load_WhenJsonCorrupt_ReturnsDefaults()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(AppPreferences.FilePath, "{ this is not valid json");

        var data = AppPreferences.Load();

        Assert.That(data, Is.EqualTo(new AppPreferencesData()));
    }

    [Test]
    public void Load_WhenJsonLiteralNull_ReturnsDefaults()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(AppPreferences.FilePath, "null");

        var data = AppPreferences.Load();

        Assert.That(data, Is.EqualTo(new AppPreferencesData()));
    }

    [Test]
    public void Load_ReadsExistingFileContents()
    {
        AppPreferences.Save(new AppPreferencesData(AppTheme.Dark, "en", 0.5));

        var loaded = AppPreferences.Load();

        Assert.That(loaded.Theme, Is.EqualTo(AppTheme.Dark));
    }
}

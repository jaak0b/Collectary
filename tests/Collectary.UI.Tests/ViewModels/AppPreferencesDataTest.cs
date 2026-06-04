using Collectary.Core.Domain;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class AppPreferencesDataTest
{
    [Test]
    public void HasDefaults()
    {
        var data = new AppPreferencesData();
        Assert.That(data.Theme, Is.EqualTo(AppTheme.Light));
        Assert.That(data.Language, Is.EqualTo("en"));
        Assert.That(data.FieldPaneRatio, Is.EqualTo(0.4));
    }

    [Test]
    public void SyncProvider_DefaultsToFolder()
    {
        var data = new AppPreferencesData();
        Assert.Multiple(() =>
        {
            Assert.That(data.SyncProvider, Is.EqualTo(CloudProvider.Folder));
            Assert.That(data.OneDriveRootFolderId, Is.Null);
            Assert.That(data.OneDriveAccount, Is.Null);
        });
    }

    [Test]
    public void HasThemingDefaults()
    {
        var data = new AppPreferencesData();
        Assert.Multiple(() =>
        {
            Assert.That(data.ColorTheme, Is.EqualTo("Light"));
            Assert.That(data.Skin, Is.EqualTo("Windows11"));
            Assert.That(data.AccentColor, Is.Null);
        });
    }

    [Test]
    public void EffectiveColorTheme_FreshDefaults_IsLight()
    {
        Assert.That(new AppPreferencesData().EffectiveColorTheme(), Is.EqualTo("Light"));
    }

    [Test]
    public void EffectiveColorTheme_LegacyDarkWithDefaultColorTheme_MigratesToDark()
    {
        var legacy = new AppPreferencesData(Theme: AppTheme.Dark);
        Assert.That(legacy.EffectiveColorTheme(), Is.EqualTo("Dark"));
    }

    [Test]
    public void EffectiveColorTheme_ExplicitColorTheme_WinsOverLegacyTheme()
    {
        var data = new AppPreferencesData(Theme: AppTheme.Dark, ColorTheme: "Nord");
        Assert.That(data.EffectiveColorTheme(), Is.EqualTo("Nord"));
    }
}

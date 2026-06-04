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
}

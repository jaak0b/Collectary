using Collectary.UI.Localization;
using Collectary.UI.Services;

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
}

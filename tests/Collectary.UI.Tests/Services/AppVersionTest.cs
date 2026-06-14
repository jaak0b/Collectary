using Collectary.Presentation.Services;

namespace Collectary.UI.Tests.Services;

[TestFixture]
public class AppVersionTest
{
    [Test]
    public void Display_StripsGitBuildMetadata()
    {
        var version = new AppVersion("0.1.203+9785672a1b");

        Assert.That(version.Display, Is.EqualTo("0.1.203"));
    }

    [Test]
    public void Display_KeepsPlainSemanticVersionUnchanged()
    {
        var version = new AppVersion("0.1.203");

        Assert.That(version.Display, Is.EqualTo("0.1.203"));
    }

    [Test]
    public void Display_KeepsPrereleaseTagButDropsMetadata()
    {
        var version = new AppVersion("1.2.3-beta.4+abcdef0");

        Assert.That(version.Display, Is.EqualTo("1.2.3-beta.4"));
    }

    [Test]
    public void Display_StripsAtTheFirstPlusWhenMetadataContainsMore()
    {
        var version = new AppVersion("1.0.0+build+extra");

        Assert.That(version.Display, Is.EqualTo("1.0.0"));
    }

    [Test]
    public void Display_WhenValueIsOnlyMetadata_IsEmpty()
    {
        var version = new AppVersion("+abcdef0");

        Assert.That(version.Display, Is.Empty);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Display_FallsBackWhenMissing(string? raw)
    {
        var version = new AppVersion(raw);

        Assert.That(version.Display, Is.EqualTo("0.0.0"));
    }
}

using System.Reflection;
using Collectary.Presentation.Services;

namespace Collectary.UI.Tests.Services;

[TestFixture]
public class AssemblyAppVersionProviderTest
{
    [Test]
    public void PresentationAssembly_UsesTheGitVersioningBaseVersion()
    {
        var sut = new AssemblyAppVersionProvider();

        Assert.That(sut.Display, Does.StartWith("0.1."),
            "Nerdbank.GitVersioning should stamp the version.json base (0.1) plus the git height; "
            + "the bare SDK default would be 1.0.0.");
    }

    [Test]
    public void Display_ReadsAndFormatsTheRealAssemblyVersion()
    {
        var sut = new AssemblyAppVersionProvider();

        Assert.Multiple(() =>
        {
            Assert.That(sut.Display, Does.Not.Contain("+"));
            Assert.That(sut.Display, Does.Match(@"^\d+\.\d+\.\d+"));
            Assert.That(sut.Display, Is.Not.EqualTo("0.0.0"));
        });
    }

    [Test]
    public void Display_FormatsWhateverTheGivenAssemblyCarries()
    {
        var sut = new AssemblyAppVersionProvider(typeof(AssemblyAppVersionProviderTest).Assembly);

        Assert.That(sut.Display, Does.Not.Contain("+"));
    }
}

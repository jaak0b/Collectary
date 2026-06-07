using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Collectary.UI.Tests.Packaging;

[TestFixture]
public class AndroidMsalBrowserDependencyTest
{
    private const string BrowserPackage = "Xamarin.AndroidX.Browser";

    [Test]
    public void AndroidHead_ReferencesXamarinAndroidXBrowser_SoMsalCustomTabsOnResumeDoesNotCrash()
    {
        var csproj = XDocument.Load(Path.Combine(
            RepoRoot(), "src", "Collectary.UI.Android", "Collectary.UI.Android.csproj"));

        var referenced = csproj.Descendants("PackageReference")
            .Any(e => string.Equals((string?)e.Attribute("Include"), BrowserPackage,
                System.StringComparison.OrdinalIgnoreCase));

        Assert.That(referenced, Is.True,
            $"{BrowserPackage} must be referenced by the Android head; MSAL's AuthenticationActivity "
            + "loads it on resume during interactive OneDrive sign-in and crashes without it.");
    }

    [Test]
    public void CentralPackageManagement_PinsXamarinAndroidXBrowserVersion()
    {
        var props = XDocument.Load(Path.Combine(RepoRoot(), "Directory.Packages.props"));

        var pinned = props.Descendants("PackageVersion")
            .Any(e => string.Equals((string?)e.Attribute("Include"), BrowserPackage,
                System.StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace((string?)e.Attribute("Version")));

        Assert.That(pinned, Is.True,
            $"{BrowserPackage} must have a pinned version in Directory.Packages.props.");
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Directory.Packages.props")))
            dir = dir.Parent;
        Assert.That(dir, Is.Not.Null, "Could not locate the repository root (Directory.Packages.props).");
        return dir!.FullName;
    }
}

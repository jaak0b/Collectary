using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Collectary.UI.Tests.Packaging;

[TestFixture]
public class AndroidMsalRedirectManifestTest
{
    private static readonly XNamespace Android = "http://schemas.android.com/apk/res/android";

    [TestCase("com.collectary.app", TestName = "Release identity")]
    [TestCase("com.collectary.app.debug", TestName = "Debug identity")]
    public void BrowserTabActivity_CatchesTheOneDriveRedirectForBoth(string host)
    {
        var manifest = XDocument.Load(Path.Combine(
            RepoRoot(), "src", "Collectary.UI.Android", "Properties", "AndroidManifest.xml"));

        var browserTab = manifest.Descendants("activity").Single(a =>
            (string?)a.Attribute(Android + "name") == "microsoft.identity.client.BrowserTabActivity");

        var caught = browserTab.Descendants("data").Any(d =>
            (string?)d.Attribute(Android + "scheme") == "msauth"
            && (string?)d.Attribute(Android + "host") == host);

        Assert.That(caught, Is.True,
            $"the OneDrive sign-in redirect host '{host}' must be caught by BrowserTabActivity; without it "
            + "the Chrome Custom Tab cannot hand control back to the app and the sign-in hangs. The debug "
            + "build installs under com.collectary.app.debug, so its redirect host differs from release.");
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

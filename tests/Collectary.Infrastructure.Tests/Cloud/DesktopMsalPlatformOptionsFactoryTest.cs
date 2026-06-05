using Collectary.Infrastructure.Cloud.Auth;

namespace Collectary.Infrastructure.Tests.Cloud;

/// <summary>
/// The desktop OneDrive MSAL options: a loopback redirect (so MSAL's system-browser flow can
/// receive the auth code) and a non-null token-cache configurator (the DPAPI-backed
/// <c>MsalCacheHelper</c>). No interactive parent window is needed on desktop.
/// </summary>
[TestFixture]
public class DesktopMsalPlatformOptionsFactoryTest
{
    private MsalPlatformOptions Build() =>
        new DesktopMsalPlatformOptionsFactory("cache-dir").Create();

    [Test]
    public void Create_UsesLoopbackRedirect() =>
        Assert.That(Build().RedirectUri, Is.EqualTo("http://localhost"));

    [Test]
    public void Create_HasNoParentActivityProvider() =>
        Assert.That(Build().ParentActivityProvider, Is.Null);

    [Test]
    public void Create_ConfiguresTokenCache() =>
        Assert.That(Build().ConfigureTokenCache, Is.Not.Null);
}

using Collectary.Infrastructure.Cloud.Auth;

namespace Collectary.Infrastructure.Tests.Cloud;

/// <summary>
/// The Android OneDrive MSAL options: a custom-scheme redirect (<c>msauth://{package}/{hash}</c>)
/// that the manifest's BrowserTabActivity catches, the current Activity as the interactive parent,
/// and no token-cache configurator (MSAL uses its built-in Keystore-backed cache on Android).
/// </summary>
[TestFixture]
public class AndroidMsalPlatformOptionsFactoryTest
{
    private static readonly Func<object?> Parent = () => "activity";

    private MsalPlatformOptions Build(string applicationId = "com.collectary.app", string hash = "abc123") =>
        new AndroidMsalPlatformOptionsFactory(applicationId, hash, Parent).Create();

    [Test]
    public void Create_ComposesMsauthRedirect() =>
        Assert.That(Build().RedirectUri, Is.EqualTo("msauth://com.collectary.app/abc123"));

    [Test]
    public void Create_UrlEncodesSignatureHash() =>
        Assert.That(Build(hash: "a+b/c=").RedirectUri, Is.EqualTo("msauth://com.collectary.app/a%2Bb%2Fc%3D"));

    [Test]
    public void Create_PassesParentActivityProvider() =>
        Assert.That(Build().ParentActivityProvider, Is.SameAs(Parent));

    [Test]
    public void Create_HasNoTokenCacheConfigurator() =>
        Assert.That(Build().ConfigureTokenCache, Is.Null);
}

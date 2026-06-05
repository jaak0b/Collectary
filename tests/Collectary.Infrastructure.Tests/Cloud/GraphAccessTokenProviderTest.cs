using System;
using Collectary.Infrastructure.Cloud.Auth;
using Collectary.Infrastructure.Tests.Infrastructure;

namespace Collectary.Infrastructure.Tests.Cloud;

[TestFixture]
public class GraphAccessTokenProviderTest
{
    [Test]
    public async Task GetAuthorizationToken_SignedIn_ReturnsToken()
    {
        var auth = new FakeCloudAuthClient(token: "abc123");
        var sut = new GraphAccessTokenProvider(auth);

        var token = await sut.GetAuthorizationTokenAsync(new Uri("https://graph.microsoft.com/v1.0/me"), null, CancellationToken.None);

        Assert.That(token, Is.EqualTo("abc123"));
    }

    [Test]
    public void GetAuthorizationToken_NotSignedIn_Throws()
    {
        var auth = new FakeCloudAuthClient { IsSignedIn = false };
        var sut = new GraphAccessTokenProvider(auth);

        Assert.That(
            async () => await sut.GetAuthorizationTokenAsync(new Uri("https://graph.microsoft.com/v1.0/me"), null, CancellationToken.None),
            Throws.InstanceOf<InvalidOperationException>().With.Message.Contains("token"));
    }

    [Test]
    public void AllowedHostsValidator_AllowsGraphHost() =>
        Assert.That(new GraphAccessTokenProvider(new FakeCloudAuthClient()).AllowedHostsValidator.AllowedHosts,
            Does.Contain("graph.microsoft.com"));
}

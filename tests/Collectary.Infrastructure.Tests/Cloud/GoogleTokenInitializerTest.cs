using System.Net.Http;
using Collectary.Infrastructure.Cloud.Auth;
using Collectary.Infrastructure.Tests.Infrastructure;
using Google.Apis.Http;

namespace Collectary.Infrastructure.Tests.Cloud;

/// <summary>
/// <see cref="GoogleTokenInitializer"/> attaches the bearer token from the auth client to each Drive
/// request, and leaves the header untouched when there is no token to send.
/// </summary>
[TestFixture]
public class GoogleTokenInitializerTest
{
    [Test]
    public async Task InterceptAsync_WithToken_SetsBearerHeader()
    {
        var sut = new GoogleTokenInitializer(new FakeCloudAuthClient(token: "abc123"));
        using var request = new HttpRequestMessage();

        await sut.InterceptAsync(request, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(request.Headers.Authorization!.Scheme, Is.EqualTo("Bearer"));
            Assert.That(request.Headers.Authorization!.Parameter, Is.EqualTo("abc123"));
        });
    }

    [Test]
    public async Task InterceptAsync_NotSignedIn_LeavesHeaderUnset()
    {
        var sut = new GoogleTokenInitializer(new FakeCloudAuthClient { IsSignedIn = false });
        using var request = new HttpRequestMessage();

        await sut.InterceptAsync(request, CancellationToken.None);

        Assert.That(request.Headers.Authorization, Is.Null);
    }

    [Test]
    public async Task InterceptAsync_EmptyToken_LeavesHeaderUnset()
    {
        var sut = new GoogleTokenInitializer(new FakeCloudAuthClient(token: string.Empty));
        using var request = new HttpRequestMessage();

        await sut.InterceptAsync(request, CancellationToken.None);

        Assert.That(request.Headers.Authorization, Is.Null);
    }

    [Test]
    public void Initialize_RegistersInterceptor_OnTheClient()
    {
        var sut = new GoogleTokenInitializer(new FakeCloudAuthClient());
        var handler = new ConfigurableMessageHandler(new HttpClientHandler());
        using var client = new ConfigurableHttpClient(handler);

        sut.Initialize(client);

#pragma warning disable CS0618 // ExecuteInterceptors is the only way to observe what Initialize registered.
        Assert.That(handler.ExecuteInterceptors, Does.Contain(sut));
#pragma warning restore CS0618
    }
}

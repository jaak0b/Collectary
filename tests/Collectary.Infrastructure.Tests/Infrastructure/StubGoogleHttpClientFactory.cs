using Google.Apis.Http;

namespace Collectary.Infrastructure.Tests.Infrastructure;

/// <summary>
/// Google.Apis <see cref="IHttpClientFactory"/> that routes all Drive traffic through a stub handler,
/// so <c>DriveService</c> can be tested with no real network.
/// </summary>
public sealed class StubGoogleHttpClientFactory : HttpClientFactory
{
    private readonly HttpMessageHandler _handler;

    public StubGoogleHttpClientFactory(HttpMessageHandler handler) => _handler = handler;

    protected override HttpMessageHandler CreateHandler(CreateHttpClientArgs args) => _handler;
}

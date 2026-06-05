using System.Net.Http.Headers;
using Collectary.Core.Ports;
using Google.Apis.Http;

namespace Collectary.Infrastructure.Cloud.Auth;

/// <summary>
/// Attaches a bearer token (obtained/refreshed via <see cref="ICloudAuthClient"/>) to every Google
/// Drive request — the Google.Apis equivalent of the Graph auth provider.
/// </summary>
public class GoogleTokenInitializer : IConfigurableHttpClientInitializer, IHttpExecuteInterceptor
{
    private readonly ICloudAuthClient _auth;

    public GoogleTokenInitializer(ICloudAuthClient auth) => _auth = auth;

    public void Initialize(ConfigurableHttpClient httpClient) =>
        httpClient.MessageHandler.AddExecuteInterceptor(this);

    public async Task InterceptAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _auth.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}

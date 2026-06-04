using Collectary.Core.Ports;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Collectary.Infrastructure.Cloud.Auth;

/// <summary>
/// Bridges our <see cref="ICloudAuthClient"/> to the Microsoft Graph (Kiota) auth pipeline:
/// every Graph request asks this provider for a bearer token, which it obtains (and silently
/// refreshes) from the auth client.
/// </summary>
public class GraphAccessTokenProvider : IAccessTokenProvider
{
    private readonly ICloudAuthClient _auth;

    public GraphAccessTokenProvider(ICloudAuthClient auth) => _auth = auth;

    public AllowedHostsValidator AllowedHostsValidator { get; } = new(new[] { "graph.microsoft.com" });

    public async Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        var token = await _auth.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
            throw new InvalidOperationException(
                "No valid access token is available; the cloud account must be (re-)connected.");
        return token;
    }
}

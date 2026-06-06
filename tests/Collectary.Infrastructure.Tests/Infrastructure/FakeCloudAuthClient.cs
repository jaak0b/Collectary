using Collectary.Core.Domain;
using Collectary.Core.Ports;

namespace Collectary.Infrastructure.Tests.Infrastructure;

/// <summary>A signed-in <see cref="ICloudAuthClient"/> that hands out a fixed access token.</summary>
public class FakeCloudAuthClient : ICloudAuthClient
{
    public FakeCloudAuthClient(CloudProvider provider = CloudProvider.OneDrive, string token = "test-token")
    {
        Provider = provider;
        Token = token;
    }

    public string Token { get; set; }

    public CloudProvider Provider { get; }

    public bool IsSignedIn { get; set; } = true;

    public string? Account { get; set; } = "tester@example.com";

    public Task SignInInteractiveAsync(CancellationToken ct)
    {
        IsSignedIn = true;
        return Task.CompletedTask;
    }

    public bool RestoreSucceeds { get; set; }

    public Task TryRestoreSessionAsync(CancellationToken ct)
    {
        if (RestoreSucceeds) IsSignedIn = true;
        return Task.CompletedTask;
    }

    public Task SignOutAsync()
    {
        IsSignedIn = false;
        return Task.CompletedTask;
    }

    public Task<string?> GetAccessTokenAsync(CancellationToken ct) =>
        Task.FromResult<string?>(IsSignedIn ? Token : null);
}

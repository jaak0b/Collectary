using Collectary.Core.Domain;

namespace Collectary.Core.Ports;

/// <summary>
/// Per-provider OAuth gateway. The only auth abstraction the rest of the app sees — the concrete
/// SDK (MSAL, Google.Apis) stays behind this port so view models and cloud stores remain
/// SDK-agnostic and testable with a fake.
/// </summary>
public interface ICloudAuthClient
{
    CloudProvider Provider { get; }

    /// <summary>True once a token (interactive or cached) has been acquired.</summary>
    bool IsSignedIn { get; }

    /// <summary>A human-readable label for the signed-in account (e.g. email), or null.</summary>
    string? Account { get; }

    /// <summary>Runs the interactive sign-in flow (opens the system browser).</summary>
    Task SignInInteractiveAsync(CancellationToken ct);

    /// <summary>
    /// Rehydrates the session from the persisted token cache (no UI), so a returning user is signed in
    /// again after an app restart instead of appearing signed-out. Safe to call when nothing is cached.
    /// </summary>
    Task TryRestoreSessionAsync(CancellationToken ct);

    /// <summary>Clears the cached account and tokens.</summary>
    Task SignOutAsync();

    /// <summary>
    /// Returns a valid access token, refreshing silently if needed. Null when no account is
    /// available and interactive sign-in has not been performed.
    /// </summary>
    Task<string?> GetAccessTokenAsync(CancellationToken ct);
}

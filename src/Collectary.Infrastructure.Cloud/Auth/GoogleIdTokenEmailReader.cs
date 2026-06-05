using System.Diagnostics.CodeAnalysis;
using Google.Apis.Auth;

namespace Collectary.Infrastructure.Cloud.Auth;

/// <summary>
/// Reads the account email from a Google <c>id_token</c> after fully validating it
/// (<see cref="GoogleJsonWebSignature.ValidateAsync(string)"/> checks the signature, issuer and
/// expiry against Google's published certs). We never trust an unsigned payload — a tampered token
/// could otherwise spoof the displayed account. On any validation failure we trust nothing and
/// return null so the caller falls back to a generic label.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Validates the id_token against Google's published certs over the network; verified manually.")]
public class GoogleIdTokenEmailReader : IIdTokenEmailReader
{
    public async Task<string?> ReadEmailAsync(string? idToken, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(idToken)) return null;
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
            return string.IsNullOrEmpty(payload.Email) ? null : payload.Email;
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}

namespace Collectary.Infrastructure.Cloud.Auth;

/// <summary>
/// Extracts the verified <c>email</c> claim from an OIDC <c>id_token</c>, or null when the token is
/// absent, invalid, or carries no email. Abstracted so the validation can be faked in tests while the
/// production implementation verifies the token's signature against the issuer's certificates.
/// </summary>
public interface IIdTokenEmailReader
{
    Task<string?> ReadEmailAsync(string? idToken, CancellationToken ct);
}

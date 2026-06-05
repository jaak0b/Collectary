using System.Net.Mail;
using Collectary.Core.Auth;
using Collectary.Core.Domain;
using Collectary.Core.Ports;

namespace Collectary.Core.UseCases;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly ICredentialStore _credentials;
    private readonly ICredentialHasher _hasher;
    private readonly UserSession _session;

    // A real (but never-matching) PBKDF2 record verified on the login miss path so that present and
    // absent usernames take comparable time, denying a timing oracle for account enumeration. The
    // iteration count/key length mirror Pbkdf2CredentialHasher so the dummy work matches a real hash.
    private readonly PasswordHash _dummyHash =
        new(new byte[64], new byte[16], 210_000, "PBKDF2-HMAC-SHA512");

    public AuthService(IUserRepository users, ICredentialStore credentials, ICredentialHasher hasher, UserSession session)
    {
        _users = users;
        _credentials = credentials;
        _hasher = hasher;
        _session = session;
    }

    public User? CurrentUser => _session.CurrentUser;

    public async Task<User> RegisterAsync(string username, string displayName, string password, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password is required.", nameof(password));

        var existing = await _users.GetByUsernameAsync(username);
        if (existing is not null)
            throw new UsernameTakenException(username);

        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        if (normalizedEmail is not null && !IsValidEmail(normalizedEmail))
            throw new ArgumentException("Email is not a valid address.", nameof(email));

        var user = new User
        {
            Username = username,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? username : displayName,
            Email = normalizedEmail,
        };

        await _users.AddAsync(user);
        await _credentials.SaveAsync(user.Id, _hasher.Hash(password));
        _session.SetCurrentUser(user);
        return user;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        var user = await _users.GetByUsernameAsync(username);
        var credential = user is null ? null : await _credentials.GetAsync(user.Id);

        // Always run a verify — against the real or the dummy hash — so the response time does not
        // reveal whether the username (or its credential) exists.
        if (!_hasher.Verify(password, credential ?? _dummyHash) || user is null || credential is null)
            return null;

        _session.SetCurrentUser(user);
        return user;
    }

    public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        if (string.IsNullOrEmpty(newPassword))
            throw new ArgumentException("Password is required.", nameof(newPassword));

        var credential = await _credentials.GetAsync(userId);
        if (credential is null || !_hasher.Verify(currentPassword, credential))
            throw new InvalidCredentialsException();

        await _credentials.SaveAsync(userId, _hasher.Hash(newPassword));
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            // MailAddress.Address round-trips the parsed address; reject when it differs (e.g. when
            // the input had trailing junk MailAddress would otherwise tolerate).
            return new MailAddress(email).Address == email;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public void Logout() => _session.Clear();
}

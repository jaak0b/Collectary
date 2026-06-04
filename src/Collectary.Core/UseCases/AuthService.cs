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

        var user = new User
        {
            Username = username,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? username : displayName,
            Email = string.IsNullOrWhiteSpace(email) ? null : email,
        };

        await _users.AddAsync(user);
        await _credentials.SaveAsync(user.Id, _hasher.Hash(password));
        _session.SetCurrentUser(user);
        return user;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        var user = await _users.GetByUsernameAsync(username);
        if (user is null)
            return null;

        var credential = await _credentials.GetAsync(user.Id);
        if (credential is null || !_hasher.Verify(password, credential))
            return null;

        _session.SetCurrentUser(user);
        return user;
    }

    public void Logout() => _session.Clear();
}

using Collectary.Core.Auth;
using Collectary.Core.Domain;
using Collectary.Core.Ports;

namespace Collectary.Core.UseCases;

public class AccountBootstrapper : IAccountBootstrapper
{
    public const string DefaultUsername = "default";

    private readonly IAuthService _auth;
    private readonly IUserRepository _users;
    private readonly IPresetRepository _presets;
    private readonly UserSession _session;

    public AccountBootstrapper(IAuthService auth, IUserRepository users, IPresetRepository presets, UserSession session)
    {
        _auth = auth;
        _users = users;
        _presets = presets;
        _session = session;
    }

    public async Task<User> EnsureDefaultUserAsync()
    {
        var existing = await _users.GetByUsernameAsync(DefaultUsername);
        if (existing is not null)
        {
            _session.SetCurrentUser(existing);
            return existing;
        }

        return await _auth.RegisterAsync(DefaultUsername, "Default", Guid.NewGuid().ToString("N"));
    }

    public Task BackfillOwnerlessAsync(Guid ownerId) =>
        _presets.BackfillOwnerlessAsync(ownerId);
}

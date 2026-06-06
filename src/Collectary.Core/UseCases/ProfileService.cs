using System.Text;
using Collectary.Core.Auth;
using Collectary.Core.Domain;
using Collectary.Core.Ports;

namespace Collectary.Core.UseCases;

public class ProfileService : IProfileService
{
    private readonly IUserRepository _users;
    private readonly UserSession _session;

    public ProfileService(IUserRepository users, UserSession session)
    {
        _users = users;
        _session = session;
    }

    public User? CurrentProfile => _session.CurrentUser;

    public Task<IReadOnlyList<User>> GetProfilesAsync() => _users.GetAllAsync();

    public async Task<User> CreateProfileAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Profile name is required.", nameof(name));

        var displayName = name.Trim();
        var username = await UniqueUsernameAsync(Slug(displayName));

        var profile = new User { Username = username, DisplayName = displayName };
        await _users.AddAsync(profile);
        return profile;
    }

    public void SelectProfile(User profile) => _session.SetCurrentUser(profile);

    public void SignOut() => _session.Clear();

    private async Task<string> UniqueUsernameAsync(string baseName)
    {
        var candidate = baseName;
        var suffix = 1;
        while (await _users.GetByUsernameAsync(candidate) is not null)
        {
            suffix++;
            candidate = $"{baseName}-{suffix}";
        }

        return candidate;
    }

    private string Slug(string displayName)
    {
        var builder = new StringBuilder(displayName.Length);
        foreach (var ch in displayName.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
                builder.Append(ch);
            else if (char.IsWhiteSpace(ch) || ch is '-' or '_')
                builder.Append('-');
        }

        var slug = builder.ToString().Trim('-');
        return slug.Length == 0 ? "profile" : slug;
    }
}

using System.Linq;
using System.Text;
using Collectary.Core.Auth;
using Collectary.Core.Domain;
using Collectary.Core.Ports;

namespace Collectary.Core.UseCases;

public class ProfileService : IProfileService
{
    private readonly IUserRepository _users;
    private readonly UserSession _session;
    private readonly IPresetUseCase _presets;
    private readonly UsernameUniquifier _uniquifier = new();

    public ProfileService(IUserRepository users, UserSession session, IPresetUseCase presets)
    {
        _users = users;
        _session = session;
        _presets = presets;
    }

    public User? CurrentProfile => _session.CurrentUser;

    public Task<IReadOnlyList<User>> GetProfilesAsync() => _users.GetAllAsync();

    public async Task<int> CountOwnedCollectionsAsync()
    {
        if (_session.CurrentUser is not { } me) return 0;
        return (await _presets.GetAllPresetsAsync()).Count(p => p.OwnerId == me.Id);
    }

    public async Task DeleteCurrentProfileAsync()
    {
        if (_session.CurrentUser is not { } me) return;

        var owned = (await _presets.GetAllPresetsAsync()).Where(p => p.OwnerId == me.Id).ToList();
        foreach (var preset in ChildrenFirst(owned))
            await _presets.DeletePresetAsync(preset.Id);

        await _users.DeleteAsync(me.Id);
    }

    private IEnumerable<Preset> ChildrenFirst(IReadOnlyList<Preset> presets)
    {
        var byId = presets.ToDictionary(p => p.Id);
        return presets.OrderByDescending(DepthWithin);

        int DepthWithin(Preset preset)
        {
            var depth = 0;
            var visited = new HashSet<Guid>();
            var current = preset;
            while (visited.Add(current.Id)
                   && current.ParentPresetId is { } parentId
                   && byId.TryGetValue(parentId, out var parent))
            {
                depth++;
                current = parent;
            }

            return depth;
        }
    }

    public async Task<User> CreateProfileAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Profile name is required.", nameof(name));

        var displayName = name.Trim();
        var username = await _uniquifier.MakeUniqueAsync(Slug(displayName),
            async candidate => await _users.GetByUsernameAsync(candidate) is not null);

        var profile = new User { Username = username, DisplayName = displayName };
        await _users.AddAsync(profile);
        return profile;
    }

    public void SelectProfile(User profile) => _session.SetCurrentUser(profile);

    public void SignOut() => _session.Clear();

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

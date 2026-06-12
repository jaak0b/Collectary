using Collectary.Core.Domain;

namespace Collectary.Core.Ports;

public interface IProfileService
{
    User? CurrentProfile { get; }
    Task<IReadOnlyList<User>> GetProfilesAsync();
    Task<User> CreateProfileAsync(string name);
    void SelectProfile(User profile);
    void SignOut();
    Task<int> CountOwnedCollectionsAsync();
    Task DeleteCurrentProfileAsync();
}

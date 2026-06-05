using Collectary.Core.Domain;

namespace Collectary.Core.Ports;

public interface IAuthService
{
    User? CurrentUser { get; }
    Task<User> RegisterAsync(string username, string displayName, string password, string? email = null);
    Task<User?> LoginAsync(string username, string password);
    Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    void Logout();
}

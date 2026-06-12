using Collectary.Core.Domain;

namespace Collectary.Core.Ports;

public interface IUserRepository
{
    Task<IReadOnlyList<User>> GetAllAsync();
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUsernameAsync(string username);
    Task AddAsync(User user);
    Task DeleteAsync(Guid id);
}

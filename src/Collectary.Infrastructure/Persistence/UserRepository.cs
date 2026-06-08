using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Microsoft.EntityFrameworkCore;

namespace Collectary.Infrastructure.Persistence;

public class UserRepository : IUserRepository
{
    private readonly Func<InventoryDbContext> _dbFactory;

    public UserRepository(Func<InventoryDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<IReadOnlyList<User>> GetAllAsync()
    {
        using var db = _dbFactory();
        return await db.Users.AsNoTracking().ToListAsync();
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        using var db = _dbFactory();
        return await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        var normalized = (username ?? string.Empty).ToLowerInvariant();
        using var db = _dbFactory();
        return await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalized);
    }

    public async Task AddAsync(User user)
    {
        using var db = _dbFactory();
        user.UpdatedAt = DateTime.UtcNow;
        ((ISyncable)user).StampModified(user.Id);
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }
}

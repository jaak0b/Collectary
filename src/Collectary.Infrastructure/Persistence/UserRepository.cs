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
        using var db = _dbFactory();
        return await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task AddAsync(User user)
    {
        using var db = _dbFactory();
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }
}

using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Microsoft.EntityFrameworkCore;

namespace Collectary.Infrastructure.Persistence;

public class UserRepository : IUserRepository
{
    private readonly Func<InventoryDbContext> _dbFactory;
    private readonly ISyncStatus? _syncStatus;
    private readonly ICurrentUser? _currentUser;

    public UserRepository(Func<InventoryDbContext> dbFactory, ISyncStatus? syncStatus = null, ICurrentUser? currentUser = null)
    {
        _dbFactory = dbFactory;
        _syncStatus = syncStatus;
        _currentUser = currentUser;
    }

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

    public async Task DeleteAsync(Guid id)
    {
        using var db = _dbFactory();
        var user = await db.Users.FindAsync(id);
        if (user is null) return;

        if (_syncStatus?.IsConfigured == true)
            ((ISyncable)user).StampDeleted(_currentUser?.AuthenticatedId);
        else
            db.Users.Remove(user);

        await db.SaveChangesAsync();
    }
}

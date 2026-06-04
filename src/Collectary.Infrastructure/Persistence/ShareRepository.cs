using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Microsoft.EntityFrameworkCore;

namespace Collectary.Infrastructure.Persistence;

public class ShareRepository : IShareRepository
{
    private readonly Func<InventoryDbContext> _dbFactory;

    public ShareRepository(Func<InventoryDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task AddOrUpdateAsync(CollectionShare share)
    {
        using var db = _dbFactory();
        var existing = await db.CollectionShares
            .FirstOrDefaultAsync(s => s.PresetId == share.PresetId && s.SharedWithUserId == share.SharedWithUserId);
        if (existing is null)
        {
            db.CollectionShares.Add(share);
        }
        else
        {
            existing.Permission = share.Permission;
            existing.GrantedByUserId = share.GrantedByUserId;
        }

        await db.SaveChangesAsync();
    }

    public async Task RemoveAsync(Guid presetId, Guid sharedWithUserId)
    {
        using var db = _dbFactory();
        var existing = await db.CollectionShares
            .FirstOrDefaultAsync(s => s.PresetId == presetId && s.SharedWithUserId == sharedWithUserId);
        if (existing is not null)
        {
            db.CollectionShares.Remove(existing);
            await db.SaveChangesAsync();
        }
    }

    public async Task RemoveAllForPresetAsync(Guid presetId)
    {
        using var db = _dbFactory();
        var all = await db.CollectionShares.Where(s => s.PresetId == presetId).ToListAsync();
        db.CollectionShares.RemoveRange(all);
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<CollectionShare>> GetByPresetAsync(Guid presetId)
    {
        using var db = _dbFactory();
        return await db.CollectionShares.AsNoTracking().Where(s => s.PresetId == presetId).ToListAsync();
    }

    public async Task<IReadOnlyList<CollectionShare>> GetForUserAsync(Guid userId)
    {
        using var db = _dbFactory();
        return await db.CollectionShares.AsNoTracking().Where(s => s.SharedWithUserId == userId).ToListAsync();
    }

    public async Task<CollectionShare?> GetAsync(Guid presetId, Guid sharedWithUserId)
    {
        using var db = _dbFactory();
        return await db.CollectionShares.AsNoTracking()
            .FirstOrDefaultAsync(s => s.PresetId == presetId && s.SharedWithUserId == sharedWithUserId);
    }
}

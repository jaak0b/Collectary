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
        var existing = await db.CollectionShares.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.PresetId == share.PresetId && s.SharedWithUserId == share.SharedWithUserId);
        if (existing is null)
        {
            share.UpdatedAt = DateTime.UtcNow;
            ((ISyncable)share).StampModified(share.GrantedByUserId);
            db.CollectionShares.Add(share);
        }
        else
        {
            existing.Permission = share.Permission;
            existing.GrantedByUserId = share.GrantedByUserId;
            existing.IsDeleted = false;
            existing.DeletedAt = null;
            existing.UpdatedAt = DateTime.UtcNow;
            ((ISyncable)existing).StampModified(share.GrantedByUserId);
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
            db.Tombstones.Add(new Tombstone { Id = existing.Id });
            await db.SaveChangesAsync();
        }
    }

    public async Task RemoveAllForPresetAsync(Guid presetId)
    {
        using var db = _dbFactory();
        var live = await db.CollectionShares
            .Where(s => s.PresetId == presetId).ToListAsync();
        foreach (var share in live)
        {
            db.CollectionShares.Remove(share);
            db.Tombstones.Add(new Tombstone { Id = share.Id });
        }
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

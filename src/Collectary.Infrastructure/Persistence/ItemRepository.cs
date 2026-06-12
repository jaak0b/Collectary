using System.Linq.Expressions;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Logging;
using Collectary.Core.Ports;
using Microsoft.EntityFrameworkCore;

namespace Collectary.Infrastructure.Persistence;

public class ItemRepository : IItemRepository
{
    private readonly Func<InventoryDbContext> _dbFactory;
    private readonly IAppLogger _logger;
    private readonly ICurrentUser? _currentUser;

    public ItemRepository(Func<InventoryDbContext> dbFactory, IAppLogger? logger = null, ICurrentUser? currentUser = null)
    {
        _dbFactory = dbFactory;
        _logger = logger ?? new NullAppLogger();
        _currentUser = currentUser;
    }

    private IQueryable<Item> WithDetails(IQueryable<Item> query) =>
        query
            .Include(i => i.Values)
            .Include(i => i.Values).ThenInclude(v => ((ListFieldValue)v).Entries).ThenInclude(e => e.SubValues);

    public async Task<IReadOnlyList<Item>> GetByPresetAsync(Guid presetId)
    {
        using var db = _dbFactory();
        var query = await ScopedAsync(db, WithDetails(db.Items).AsNoTracking());
        return await query.Where(i => i.PresetId == presetId).ToListAsync();
    }

    public async Task<IReadOnlyCollection<int>> GetUsedAutoNumbersAsync(Guid fieldDefinitionId, Guid? excludeItemId)
    {
        using var db = _dbFactory();
        var authorizedItems = await ScopedAsync(db, db.Items.AsNoTracking());
        var query =
            from v in db.Set<AutoNumberFieldValue>().AsNoTracking()
            join i in authorizedItems on v.ItemId equals i.Id
            where v.FieldDefinitionId == fieldDefinitionId
                  && v.Value != null
                  && (excludeItemId == null || v.ItemId != excludeItemId)
            select v.Value;
        var numbers = await query.Distinct().ToListAsync();
        return numbers.Where(n => n.HasValue).Select(n => n!.Value).ToList();
    }

    public async Task<Item?> GetByIdAsync(Guid id)
    {
        using var db = _dbFactory();
        var query = await ScopedAsync(db, WithDetails(db.Items).AsNoTracking());
        return await query.FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IReadOnlyList<Item>> SearchAsync(Expression<Func<Item, bool>>? serverFilter)
    {
        using var db = _dbFactory();
        var query = await ScopedAsync(db, WithDetails(db.Items).AsNoTracking());
        if (serverFilter is not null) query = query.Where(serverFilter);
        return await query.ToListAsync();
    }

    private async Task<IQueryable<Item>> ScopedAsync(InventoryDbContext db, IQueryable<Item> query)
    {
        if (_currentUser?.IsAuthenticated != true) return query;

        var uid = _currentUser.UserId;
        var sharedIds = await db.CollectionShares
            .Where(s => s.SharedWithUserId == uid)
            .Select(s => s.PresetId)
            .ToListAsync();
        var authorizedPresetIds = await db.Presets
            .Where(p => p.OwnerId == uid || sharedIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();
        return query.Where(i => authorizedPresetIds.Contains(i.PresetId));
    }

    public async Task AddAsync(Item item)
    {
        using var db = _dbFactory();
        item.UpdatedAt = DateTime.UtcNow;
        ((ISyncable)item).StampModified(_currentUser?.AuthenticatedId);
        db.Items.Add(item);
        await db.SaveChangesAsync();
        _logger.Debug("Persisted new item id={Id} preset={PresetId} values={Values}",
            item.Id, item.PresetId, item.Values.Count);
    }

    public async Task UpdateAsync(Item item)
    {
        using var db = _dbFactory();
        var tracked = await WithDetails(db.Items)
            .FirstOrDefaultAsync(i => i.Id == item.Id);
        if (tracked is null) return;

        tracked.DisplayName = item.DisplayName;
        tracked.UpdatedAt = item.UpdatedAt;
        ((ISyncable)tracked).StampModified(_currentUser?.AuthenticatedId);

        var updatedIds = item.Values.Select(v => v.Id).ToHashSet();
        var toRemove = tracked.Values
            .Where(existing => !updatedIds.Contains(existing.Id))
            .ToList();
        db.FieldValues.RemoveRange(toRemove);
        _logger.Debug("Updating item id={Id} preset={PresetId} values={Values} removedValues={Removed}",
            item.Id, item.PresetId, item.Values.Count, toRemove.Count);

        var existingById = tracked.Values.ToDictionary(v => v.Id);
        foreach (var updatedValue in item.Values)
        {
            if (!existingById.TryGetValue(updatedValue.Id, out var existingValue))
            {
                updatedValue.ItemId = tracked.Id;
                tracked.Values.Add(updatedValue);
            }
            else
            {
                if (existingValue is ListFieldValue existingList && updatedValue is ListFieldValue updatedList)
                    SyncListEntries(db, existingList, updatedList);
                else
                    existingValue.CopyFrom(updatedValue);
            }
        }

        await db.SaveChangesAsync();
    }

    private void SyncListEntries(InventoryDbContext db, ListFieldValue existing, ListFieldValue updated)
    {
        var updatedIds = updated.Entries.Select(e => e.Id).ToHashSet();
        var toRemove = existing.Entries
            .Where(e => !updatedIds.Contains(e.Id))
            .ToList();
        db.ListEntries.RemoveRange(toRemove);

        var existingById = existing.Entries.ToDictionary(e => e.Id);
        foreach (var updatedEntry in updated.Entries)
        {
            if (!existingById.TryGetValue(updatedEntry.Id, out var existingEntry))
            {
                updatedEntry.ListFieldValueId = existing.Id;
                foreach (var sv in updatedEntry.SubValues)
                    sv.ListEntryId = updatedEntry.Id;
                existing.Entries.Add(updatedEntry);
            }
            else
            {
                existingEntry.DisplayOrder = updatedEntry.DisplayOrder;
                SyncSubValues(db, existingEntry, updatedEntry);
            }
        }
    }

    private void SyncSubValues(InventoryDbContext db, ListEntry existing, ListEntry updated)
    {
        var updatedIds = updated.SubValues.Select(v => v.Id).ToHashSet();
        var toRemove = existing.SubValues
            .Where(e => !updatedIds.Contains(e.Id))
            .ToList();
        db.FieldValues.RemoveRange(toRemove);

        var existingById = existing.SubValues.ToDictionary(v => v.Id);
        foreach (var updatedSub in updated.SubValues)
        {
            if (!existingById.TryGetValue(updatedSub.Id, out var existingSub))
            {
                updatedSub.ListEntryId = existing.Id;
                existing.SubValues.Add(updatedSub);
            }
            else
            {
                if (existingSub is ListFieldValue existingNestedList && updatedSub is ListFieldValue updatedNestedList)
                    SyncListEntries(db, existingNestedList, updatedNestedList);
                else
                    existingSub.CopyFrom(updatedSub);
            }
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        using var db = _dbFactory();
        var item = await db.Items.FindAsync(id);
        if (item is null) return;

        HardDelete(db, item);
        await db.SaveChangesAsync();
    }

    public async Task DeleteByPresetAsync(Guid presetId)
    {
        using var db = _dbFactory();
        var items = await db.Items.Where(i => i.PresetId == presetId).ToListAsync();
        foreach (var item in items) HardDelete(db, item);
        await db.SaveChangesAsync();
    }

    private void HardDelete(InventoryDbContext db, Item item)
    {
        db.Items.Remove(item);
        db.Tombstones.Add(new Tombstone { Id = item.Id });
    }
}

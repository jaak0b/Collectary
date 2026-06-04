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

    public ItemRepository(Func<InventoryDbContext> dbFactory, IAppLogger? logger = null)
    {
        _dbFactory = dbFactory;
        _logger = logger ?? new NullAppLogger();
    }

    private IQueryable<Item> WithDetails(IQueryable<Item> query) =>
        query
            .Include(i => i.Values)
            .Include(i => i.Values).ThenInclude(v => ((ListFieldValue)v).Entries).ThenInclude(e => e.SubValues);

    public async Task<IReadOnlyList<Item>> GetByPresetAsync(Guid presetId)
    {
        using var db = _dbFactory();
        return await WithDetails(db.Items)
            .AsNoTracking()
            .Where(i => i.PresetId == presetId)
            .ToListAsync();
    }

    public async Task<Item?> GetByIdAsync(Guid id)
    {
        using var db = _dbFactory();
        return await WithDetails(db.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task AddAsync(Item item)
    {
        using var db = _dbFactory();
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
        if (item is not null)
        {
            db.Items.Remove(item);
            await db.SaveChangesAsync();
        }
    }

    public async Task DeleteByPresetAsync(Guid presetId)
    {
        using var db = _dbFactory();
        var items = await db.Items.Where(i => i.PresetId == presetId).ToListAsync();
        db.Items.RemoveRange(items);
        await db.SaveChangesAsync();
    }
}

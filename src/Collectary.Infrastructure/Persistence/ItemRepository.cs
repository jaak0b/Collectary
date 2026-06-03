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

    public async Task<IReadOnlyList<Item>> GetByPresetAsync(Guid presetId)
    {
        using var db = _dbFactory();
        return await db.Items
            .Include(i => i.Values)
            .Include(i => i.Values).ThenInclude(v => ((ListFieldValue)v).Entries).ThenInclude(e => e.SubValues)
            .Where(i => i.PresetId == presetId)
            .ToListAsync();
    }

    public async Task<Item?> GetByIdAsync(Guid id)
    {
        using var db = _dbFactory();
        return await db.Items
            .Include(i => i.Values)
            .Include(i => i.Values).ThenInclude(v => ((ListFieldValue)v).Entries).ThenInclude(e => e.SubValues)
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
        var tracked = await db.Items
            .Include(i => i.Values)
            .Include(i => i.Values).ThenInclude(v => ((ListFieldValue)v).Entries).ThenInclude(e => e.SubValues)
            .FirstOrDefaultAsync(i => i.Id == item.Id);
        if (tracked is null) return;

        tracked.DisplayName = item.DisplayName;
        tracked.UpdatedAt = item.UpdatedAt;

        var toRemove = tracked.Values
            .Where(existing => item.Values.All(updated => updated.Id != existing.Id))
            .ToList();
        db.FieldValues.RemoveRange(toRemove);
        _logger.Debug("Updating item id={Id} preset={PresetId} values={Values} removedValues={Removed}",
            item.Id, item.PresetId, item.Values.Count, toRemove.Count);

        foreach (var updatedValue in item.Values)
        {
            var existingValue = tracked.Values.FirstOrDefault(v => v.Id == updatedValue.Id);
            if (existingValue is null)
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

    private static void SyncListEntries(InventoryDbContext db, ListFieldValue existing, ListFieldValue updated)
    {
        var toRemove = existing.Entries
            .Where(e => updated.Entries.All(u => u.Id != e.Id))
            .ToList();
        db.ListEntries.RemoveRange(toRemove);

        foreach (var updatedEntry in updated.Entries)
        {
            var existingEntry = existing.Entries.FirstOrDefault(e => e.Id == updatedEntry.Id);
            if (existingEntry is null)
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

    private static void SyncSubValues(InventoryDbContext db, ListEntry existing, ListEntry updated)
    {
        var toRemove = existing.SubValues
            .Where(e => updated.SubValues.All(u => u.Id != e.Id))
            .ToList();
        db.FieldValues.RemoveRange(toRemove);

        foreach (var updatedSub in updated.SubValues)
        {
            var existingSub = existing.SubValues.FirstOrDefault(v => v.Id == updatedSub.Id);
            if (existingSub is null)
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

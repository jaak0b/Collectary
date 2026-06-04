using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Microsoft.EntityFrameworkCore;

namespace Collectary.Infrastructure.Persistence;

public class EfSyncStore : ISyncStore
{
    private readonly Func<InventoryDbContext> _dbFactory;
    private readonly IFieldDefinitionMerger _merger;

    public EfSyncStore(Func<InventoryDbContext> dbFactory, IFieldDefinitionMerger merger)
    {
        _dbFactory = dbFactory;
        _merger = merger;
    }

    public async Task<IReadOnlyList<Preset>> GetAllPresetsAsync()
    {
        using var db = _dbFactory();
        return await WithPresetDetails(db.Presets.IgnoreQueryFilters().AsNoTracking()).ToListAsync();
    }

    public async Task<IReadOnlyList<Item>> GetAllItemsAsync()
    {
        using var db = _dbFactory();
        return await WithItemDetails(db.Items.IgnoreQueryFilters().AsNoTracking()).ToListAsync();
    }

    public async Task<IReadOnlyList<SystemField>> GetAllSystemFieldsAsync()
    {
        using var db = _dbFactory();
        return await WithSystemFieldDetails(db.SystemFields.IgnoreQueryFilters().AsNoTracking()).ToListAsync();
    }

    public Task ApplyPresetAsync(Preset preset) =>
        ReplaceAtomicallyAsync(db => WithPresetDetails(db.Presets.IgnoreQueryFilters()), preset.Id,
            (db, e) => db.Presets.Add(e), preset);

    public Task ApplyItemAsync(Item item) =>
        ReplaceAtomicallyAsync(db => WithItemDetails(db.Items.IgnoreQueryFilters()), item.Id,
            (db, e) => db.Items.Add(e), item);

    private async Task ReplaceAtomicallyAsync<T>(
        Func<InventoryDbContext, IQueryable<T>> query, Guid id, Action<InventoryDbContext, T> add, T replacement)
        where T : DomainObject
    {
        using var db = _dbFactory();
        await using var tx = await db.Database.BeginTransactionAsync();

        var existing = await query(db).FirstOrDefaultAsync(e => e.Id == id);
        if (existing is not null)
        {
            db.Remove(existing);
            await db.SaveChangesAsync();
        }

        add(db, replacement);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task ApplySystemFieldAsync(SystemField systemField)
    {
        using var db = _dbFactory();
        var tracked = await WithSystemFieldDetails(db.SystemFields.IgnoreQueryFilters())
            .FirstOrDefaultAsync(sf => sf.Id == systemField.Id);

        if (tracked is null)
        {
            db.SystemFields.Add(systemField);
        }
        else
        {
            tracked.Name = systemField.Name;
            tracked.SortOrder = systemField.SortOrder;
            CopySyncMetadata(systemField, tracked);
            _merger.Apply(db, tracked.Definition, systemField.Definition);
        }

        await db.SaveChangesAsync();
    }

    public async Task MarkSyncedAsync(SyncEntityKind kind, Guid id, long baseRevision, bool dirty, long? revision = null)
    {
        using var db = _dbFactory();
        ISyncable? tracked = kind switch
        {
            SyncEntityKind.Preset => await db.Presets.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id),
            SyncEntityKind.Item => await db.Items.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.Id == id),
            SyncEntityKind.SystemField => await db.SystemFields.IgnoreQueryFilters().FirstOrDefaultAsync(sf => sf.Id == id),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown sync entity kind"),
        };
        if (tracked is null) return;

        tracked.BaseRevision = baseRevision;
        tracked.IsDirty = dirty;
        if (revision.HasValue) tracked.Revision = revision.Value;
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<PurgedTombstone>> PurgeTombstonesAsync(DateTime cutoff)
    {
        using var db = _dbFactory();
        var presets = await db.Presets.IgnoreQueryFilters()
            .Where(p => p.IsDeleted && !p.IsDirty && p.DeletedAt != null && p.DeletedAt < cutoff).ToListAsync();
        var items = await db.Items.IgnoreQueryFilters()
            .Where(i => i.IsDeleted && !i.IsDirty && i.DeletedAt != null && i.DeletedAt < cutoff).ToListAsync();
        var systemFields = await db.SystemFields.IgnoreQueryFilters()
            .Where(s => s.IsDeleted && !s.IsDirty && s.DeletedAt != null && s.DeletedAt < cutoff).ToListAsync();

        db.Presets.RemoveRange(presets);
        db.Items.RemoveRange(items);
        db.SystemFields.RemoveRange(systemFields);
        await db.SaveChangesAsync();

        var purged = new List<PurgedTombstone>();
        purged.AddRange(presets.Select(p => new PurgedTombstone(SyncEntityKind.Preset, p.Id)));
        purged.AddRange(items.Select(i => new PurgedTombstone(SyncEntityKind.Item, i.Id)));
        purged.AddRange(systemFields.Select(s => new PurgedTombstone(SyncEntityKind.SystemField, s.Id)));
        return purged;
    }

    public async Task DeleteLocallyAsync(SyncEntityKind kind, Guid id)
    {
        using var db = _dbFactory();
        switch (kind)
        {
            case SyncEntityKind.Preset:
                db.Presets.RemoveRange(await db.Presets.IgnoreQueryFilters().Where(p => p.Id == id).ToListAsync());
                break;
            case SyncEntityKind.Item:
                db.Items.RemoveRange(await db.Items.IgnoreQueryFilters().Where(i => i.Id == id).ToListAsync());
                break;
            case SyncEntityKind.SystemField:
                db.SystemFields.RemoveRange(await db.SystemFields.IgnoreQueryFilters().Where(s => s.Id == id).ToListAsync());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown sync entity kind");
        }
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<string>> GetReferencedImageKeysAsync()
    {
        using var db = _dbFactory();
        var items = await WithItemDetails(db.Items.AsNoTracking()).ToListAsync();
        var keys = new HashSet<string>();
        foreach (var item in items)
            foreach (var value in item.Values)
                CollectImageKeys(value, keys);
        return keys.ToList();
    }

    private void CollectImageKeys(FieldValue value, HashSet<string> keys)
    {
        if (value is ImageFieldValue image && !string.IsNullOrEmpty(image.ImageKey))
            keys.Add(image.ImageKey);

        if (value is ListFieldValue list)
            foreach (var entry in list.Entries)
                foreach (var sub in entry.SubValues)
                    CollectImageKeys(sub, keys);
    }

    private void CopySyncMetadata(ISyncable source, ISyncable target)
    {
        target.UpdatedAt = source.UpdatedAt;
        target.IsDeleted = source.IsDeleted;
        target.DeletedAt = source.DeletedAt;
        target.Revision = source.Revision;
        target.BaseRevision = source.BaseRevision;
        target.IsDirty = source.IsDirty;
        target.LastModifiedByUserId = source.LastModifiedByUserId;
    }

    private IQueryable<Preset> WithPresetDetails(IQueryable<Preset> query) =>
        query
            .Include(p => p.Fields)
            .Include(p => p.Fields).ThenInclude(f => ((ListFieldDefinition)f).SubFields)
            .Include(p => p.Fields).ThenInclude(f => ((ListFieldDefinition)f).Groups)
            .Include(p => p.Groups)
            .Include(p => p.SystemFieldRefs)
            .AsSplitQuery();

    private IQueryable<Item> WithItemDetails(IQueryable<Item> query) =>
        query
            .Include(i => i.Values)
            .Include(i => i.Values).ThenInclude(v => ((ListFieldValue)v).Entries).ThenInclude(e => e.SubValues);

    private IQueryable<SystemField> WithSystemFieldDetails(IQueryable<SystemField> query) =>
        query
            .Include(sf => sf.Definition).ThenInclude(d => ((ListFieldDefinition)d).SubFields)
            .Include(sf => sf.Definition).ThenInclude(d => ((ListFieldDefinition)d).Groups);
}

using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Logging;
using Collectary.Core.Ports;
using Microsoft.EntityFrameworkCore;

namespace Collectary.Infrastructure.Persistence;

public class EfSyncStore : ISyncStore
{
    private readonly Func<InventoryDbContext> _dbFactory;
    private readonly IFieldDefinitionMerger _merger;
    private readonly IAppLogger _logger;

    public EfSyncStore(Func<InventoryDbContext> dbFactory, IFieldDefinitionMerger merger, IAppLogger? logger = null)
    {
        _dbFactory = dbFactory;
        _merger = merger;
        _logger = logger ?? new NullAppLogger();
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

    public async Task<IReadOnlyList<SharedField>> GetAllSharedFieldsAsync()
    {
        using var db = _dbFactory();
        return await WithSharedFieldDetails(db.SharedFields.IgnoreQueryFilters().AsNoTracking()).ToListAsync();
    }

    public async Task ApplyPresetAsync(Preset preset)
    {
        using var db = _dbFactory();
        var tracked = await WithPresetDetails(db.Presets.IgnoreQueryFilters())
            .FirstOrDefaultAsync(p => p.Id == preset.Id);

        if (tracked is null)
        {
            db.Presets.Add(preset);
        }
        else
        {
            tracked.Name = preset.Name;
            tracked.ColumnCount = preset.ColumnCount;
            tracked.FieldLabelLayout = preset.FieldLabelLayout;
            tracked.ParentPresetId = preset.ParentPresetId;
            tracked.DisplayOrder = preset.DisplayOrder;
            tracked.OwnerId = preset.OwnerId;
            _merger.MergePreset(db, tracked, preset);
            CopySyncMetadata(preset, tracked);
        }

        await db.SaveChangesAsync();
    }

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

    public async Task ApplySharedFieldAsync(SharedField sharedField)
    {
        using var db = _dbFactory();
        var tracked = await WithSharedFieldDetails(db.SharedFields.IgnoreQueryFilters())
            .FirstOrDefaultAsync(sf => sf.Id == sharedField.Id);

        if (tracked is null)
        {
            db.SharedFields.Add(sharedField);
        }
        else
        {
            tracked.Name = sharedField.Name;
            tracked.SortOrder = sharedField.SortOrder;
            CopySyncMetadata(sharedField, tracked);
            _merger.Apply(db, tracked.Definition, sharedField.Definition);
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
            SyncEntityKind.SharedField => await db.SharedFields.IgnoreQueryFilters().FirstOrDefaultAsync(sf => sf.Id == id),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown sync entity kind"),
        };
        if (tracked is null)
        {
            _logger.Warning("MarkSynced skipped: {Kind} {Id} is no longer present locally", kind, id);
            return;
        }

        tracked.BaseRevision = baseRevision;
        if (dirty || tracked.Revision == baseRevision)
            tracked.IsDirty = dirty;
        if (revision.HasValue) tracked.Revision = revision.Value;
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<PurgedTombstone>> PurgeTombstonesAsync(DateTime cutoff)
    {
        using var db = _dbFactory();

        var presetIds = await PurgeKindAsync(db.Presets, cutoff);
        var itemIds = await PurgeKindAsync(db.Items, cutoff);
        var sharedIds = await PurgeKindAsync(db.SharedFields, cutoff);

        var purged = new List<PurgedTombstone>();
        purged.AddRange(presetIds.Select(id => new PurgedTombstone(SyncEntityKind.Preset, id)));
        purged.AddRange(itemIds.Select(id => new PurgedTombstone(SyncEntityKind.Item, id)));
        purged.AddRange(sharedIds.Select(id => new PurgedTombstone(SyncEntityKind.SharedField, id)));
        return purged;
    }

    private static async Task<IReadOnlyList<Guid>> PurgeKindAsync<T>(DbSet<T> set, DateTime cutoff)
        where T : DomainObject, ISyncable
    {
        var expired = set.IgnoreQueryFilters()
            .Where(e => e.IsDeleted && !e.IsDirty && e.DeletedAt != null && e.DeletedAt < cutoff);
        var ids = await expired.Select(e => e.Id).ToListAsync();
        await expired.ExecuteDeleteAsync();
        return ids;
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
            case SyncEntityKind.SharedField:
                db.SharedFields.RemoveRange(await db.SharedFields.IgnoreQueryFilters().Where(s => s.Id == id).ToListAsync());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown sync entity kind");
        }
        await db.SaveChangesAsync();
    }

    public Task<IReadOnlyList<string>> GetReferencedImageKeysAsync() =>
        CollectImageKeysAsync(includeDeleted: true);

    public Task<IReadOnlyList<string>> GetLiveReferencedImageKeysAsync() =>
        CollectImageKeysAsync(includeDeleted: false);

    private async Task<IReadOnlyList<string>> CollectImageKeysAsync(bool includeDeleted)
    {
        using var db = _dbFactory();
        var source = db.Items.AsNoTracking();
        if (includeDeleted) source = source.IgnoreQueryFilters();
        var items = await WithItemDetails(source).ToListAsync();
        var keys = new HashSet<string>();
        foreach (var item in items)
            foreach (var value in item.Values)
                keys.UnionWith(value.ReferencedBlobKeys());
        return keys.ToList();
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
            .Include(p => p.SharedFieldRefs)
            .AsSplitQuery();

    private IQueryable<Item> WithItemDetails(IQueryable<Item> query) =>
        query
            .Include(i => i.Values)
            .Include(i => i.Values).ThenInclude(v => ((ListFieldValue)v).Entries).ThenInclude(e => e.SubValues)
            .AsSplitQuery();

    private IQueryable<SharedField> WithSharedFieldDetails(IQueryable<SharedField> query) =>
        query
            .Include(sf => sf.Definition).ThenInclude(d => ((ListFieldDefinition)d).SubFields)
            .Include(sf => sf.Definition).ThenInclude(d => ((ListFieldDefinition)d).Groups);
}

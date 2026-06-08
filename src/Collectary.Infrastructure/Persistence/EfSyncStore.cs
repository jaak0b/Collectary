using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Logging;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using Microsoft.EntityFrameworkCore;

namespace Collectary.Infrastructure.Persistence;

public class EfSyncStore : ISyncStore
{
    private readonly Func<InventoryDbContext> _dbFactory;
    private readonly IFieldDefinitionMerger _merger;
    private readonly IAppLogger _logger;
    private readonly UsernameUniquifier _uniquifier = new();
    private readonly IReadOnlyDictionary<SyncEntityKind, EntityOps> _ops;

    public EfSyncStore(Func<InventoryDbContext> dbFactory, IFieldDefinitionMerger merger, IAppLogger? logger = null)
    {
        _dbFactory = dbFactory;
        _merger = merger;
        _logger = logger ?? new NullAppLogger();
        _ops = new Dictionary<SyncEntityKind, EntityOps>
        {
            [SyncEntityKind.Preset] = OpsFor<Preset>(),
            [SyncEntityKind.Item] = OpsFor<Item>(),
            [SyncEntityKind.SharedField] = OpsFor<SharedField>(),
            [SyncEntityKind.User] = OpsFor<User>(),
            [SyncEntityKind.Share] = OpsFor<CollectionShare>(),
        };
    }

    private sealed record EntityOps(
        Func<InventoryDbContext, Guid, Task<ISyncable?>> Find,
        Func<InventoryDbContext, Guid, Task> Delete,
        Func<InventoryDbContext, DateTime, Task<IReadOnlyList<Guid>>> Purge);

    private EntityOps OpsFor<T>() where T : DomainObject, ISyncable => new(
        async (db, id) => await db.Set<T>().IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id),
        async (db, id) => db.Set<T>().RemoveRange(await db.Set<T>().IgnoreQueryFilters().Where(e => e.Id == id).ToListAsync()),
        (db, cutoff) => PurgeKindAsync(db.Set<T>(), cutoff));

    private EntityOps OpsFor(SyncEntityKind kind) =>
        _ops.TryGetValue(kind, out var ops)
            ? ops
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown sync entity kind");

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

    public async Task<IReadOnlyList<User>> GetAllUsersAsync()
    {
        using var db = _dbFactory();
        return await db.Users.IgnoreQueryFilters().AsNoTracking().ToListAsync();
    }

    public async Task<IReadOnlyList<CollectionShare>> GetAllSharesAsync()
    {
        using var db = _dbFactory();
        return await db.CollectionShares.IgnoreQueryFilters().AsNoTracking().ToListAsync();
    }

    public async Task ApplyUserAsync(User user)
    {
        using var db = _dbFactory();
        var tracked = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == user.Id);

        if (tracked is null)
        {
            user.Username = await UniqueUsernameAsync(db, user.Username, user.Id);
            db.Users.Add(user);
        }
        else
        {
            tracked.Username = await UniqueUsernameAsync(db, user.Username, user.Id);
            tracked.DisplayName = user.DisplayName;
            CopySyncMetadata(user, tracked);
        }

        await db.SaveChangesAsync();
    }

    public async Task ApplyShareAsync(CollectionShare share)
    {
        using var db = _dbFactory();
        var tracked = await db.CollectionShares.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == share.Id);

        if (tracked is null)
        {
            db.CollectionShares.Add(share);
        }
        else
        {
            tracked.PresetId = share.PresetId;
            tracked.SharedWithUserId = share.SharedWithUserId;
            tracked.GrantedByUserId = share.GrantedByUserId;
            tracked.Permission = share.Permission;
            CopySyncMetadata(share, tracked);
        }

        await db.SaveChangesAsync();
    }

    private async Task<string> UniqueUsernameAsync(InventoryDbContext db, string username, Guid selfId)
    {
        var reserved = (await db.Users.IgnoreQueryFilters()
                .Where(u => u.Id != selfId)
                .Select(u => u.Username)
                .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return await _uniquifier.MakeUniqueAsync(username, candidate => Task.FromResult(reserved.Contains(candidate)));
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
        var ops = OpsFor(kind);
        using var db = _dbFactory();
        var tracked = await ops.Find(db, id);
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
        var purged = new List<PurgedTombstone>();
        foreach (var (kind, ops) in _ops)
            foreach (var id in await ops.Purge(db, cutoff))
                purged.Add(new PurgedTombstone(kind, id));
        return purged;
    }

    private async Task<IReadOnlyList<Guid>> PurgeKindAsync<T>(DbSet<T> set, DateTime cutoff)
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
        var ops = OpsFor(kind);
        using var db = _dbFactory();
        await ops.Delete(db, id);
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

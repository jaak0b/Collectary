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
            [SyncEntityKind.Preset] = OpsFor<Preset>(ReparentOrphanedChildrenAsync),
            [SyncEntityKind.Item] = OpsFor<Item>(),
            [SyncEntityKind.SharedField] = OpsFor<SharedField>(),
            [SyncEntityKind.User] = OpsFor<User>(),
            [SyncEntityKind.Share] = OpsFor<CollectionShare>(),
        };
    }

    private sealed record EntityOps(
        Func<InventoryDbContext, Guid, Task> Delete,
        Func<InventoryDbContext, IReadOnlyCollection<Guid>, Task<IReadOnlyList<ISyncable>>> FindMany,
        Func<InventoryDbContext, ISet<Guid>, Task> DeleteMany,
        Func<InventoryDbContext, Task<bool>> AnyDirty,
        Func<InventoryDbContext, ISet<Guid>, Task>? PreDelete = null);

    private EntityOps OpsFor<T>(Func<InventoryDbContext, ISet<Guid>, Task>? preDelete = null)
        where T : DomainObject, ISyncable => new(
        Delete: async (db, id) => db.Set<T>().RemoveRange(await db.Set<T>().Where(e => e.Id == id).ToListAsync()),
        FindMany: async (db, ids) =>
            (await db.Set<T>().Where(e => ids.Contains(e.Id)).ToListAsync()).Cast<ISyncable>().ToList(),
        DeleteMany: async (db, ids) =>
            db.Set<T>().RemoveRange(await db.Set<T>().Where(e => ids.Contains(e.Id)).ToListAsync()),
        AnyDirty: db => db.Set<T>().AnyAsync(e => e.IsDirty),
        PreDelete: preDelete);

    private async Task ReparentOrphanedChildrenAsync(InventoryDbContext db, ISet<Guid> idSet)
    {
        var orphanedChildren = await db.Presets
            .Where(p => p.ParentPresetId != null && idSet.Contains(p.ParentPresetId.Value) && !idSet.Contains(p.Id))
            .ToListAsync();
        foreach (var child in orphanedChildren)
            child.ParentPresetId = null;
    }

    private EntityOps OpsFor(SyncEntityKind kind) =>
        _ops.TryGetValue(kind, out var ops)
            ? ops
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown sync entity kind");

    public async Task<IReadOnlyList<Preset>> GetAllPresetsAsync()
    {
        using var db = _dbFactory();
        return await WithPresetDetails(db.Presets.AsNoTracking()).ToListAsync();
    }

    public async Task<IReadOnlyList<Item>> GetAllItemsAsync()
    {
        using var db = _dbFactory();
        return await WithItemDetails(db.Items.AsNoTracking()).ToListAsync();
    }

    public async Task<IReadOnlyList<SharedField>> GetAllSharedFieldsAsync()
    {
        using var db = _dbFactory();
        return await WithSharedFieldDetails(db.SharedFields.AsNoTracking()).ToListAsync();
    }

    public async Task<IReadOnlyList<User>> GetAllUsersAsync()
    {
        using var db = _dbFactory();
        return await db.Users.AsNoTracking().ToListAsync();
    }

    public async Task<IReadOnlyList<CollectionShare>> GetAllSharesAsync()
    {
        using var db = _dbFactory();
        return await db.CollectionShares.AsNoTracking().ToListAsync();
    }

    public async Task ApplyUserAsync(User user)
    {
        using var db = _dbFactory();
        var tracked = await db.Users.FirstOrDefaultAsync(u => u.Id == user.Id);

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
        var tracked = await db.CollectionShares.FirstOrDefaultAsync(s => s.Id == share.Id);

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
        var reserved = (await db.Users
                .Where(u => u.Id != selfId)
                .Select(u => u.Username)
                .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return await _uniquifier.MakeUniqueAsync(username, candidate => Task.FromResult(reserved.Contains(candidate)));
    }

    public async Task ApplyPresetAsync(Preset preset)
    {
        using var db = _dbFactory();
        var tracked = await WithPresetDetails(db.Presets)
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
        ReplaceAtomicallyAsync(db => WithItemDetails(db.Items), item.Id,
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
        var tracked = await WithSharedFieldDetails(db.SharedFields)
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

    public async Task<IReadOnlyList<Guid>> GetTombstoneIdsAsync()
    {
        using var db = _dbFactory();
        return await db.Tombstones.AsNoTracking().Select(t => t.Id).ToListAsync();
    }

    public async Task ApplyDeletionsAsync(IReadOnlyCollection<Guid> ids)
    {
        if (ids.Count == 0) return;
        using var db = _dbFactory();
        var idSet = ids.ToHashSet();

        foreach (var ops in _ops.Values)
            if (ops.PreDelete is not null)
                await ops.PreDelete(db, idSet);

        foreach (var ops in _ops.Values)
            await ops.DeleteMany(db, idSet);

        var already = (await db.Tombstones.Where(t => idSet.Contains(t.Id)).Select(t => t.Id).ToListAsync()).ToHashSet();
        foreach (var id in idSet)
            if (!already.Contains(id))
                db.Tombstones.Add(new Tombstone { Id = id });

        await db.SaveChangesAsync();
    }

    public async Task StampPushedAsync(IReadOnlyCollection<PushStamp> stamps)
    {
        if (stamps.Count == 0) return;
        using var db = _dbFactory();
        foreach (var group in stamps.GroupBy(s => s.Kind))
        {
            var ids = group.Select(s => s.Id).ToList();
            var tracked = (await OpsFor(group.Key).FindMany(db, ids)).ToDictionary(e => e.Id);
            foreach (var stamp in group)
            {
                if (!tracked.TryGetValue(stamp.Id, out var entity))
                {
                    _logger.Warning("StampPushed skipped: {Kind} {Id} is no longer present locally", stamp.Kind, stamp.Id);
                    continue;
                }

                entity.Lamport = stamp.Lamport;
                entity.LastModifiedByDeviceId = stamp.DeviceId;
                entity.BaseRevision = entity.Revision;
                entity.IsDirty = false;
            }
        }

        await db.SaveChangesAsync();
    }

    public async Task<bool> HasDirtyEntitiesAsync()
    {
        using var db = _dbFactory();
        foreach (var ops in _ops.Values)
            if (await ops.AnyDirty(db)) return true;
        return false;
    }

    public async Task<long> GetMaxObservedLamportAsync()
    {
        using var db = _dbFactory();
        var state = await db.SyncStates.AsNoTracking().FirstOrDefaultAsync(s => s.Id == 1);
        return state?.MaxObservedLamport ?? 0;
    }

    public async Task SetMaxObservedLamportAsync(long value)
    {
        using var db = _dbFactory();
        var state = await db.SyncStates.FirstOrDefaultAsync(s => s.Id == 1);
        if (state is null)
            db.SyncStates.Add(new SyncState { Id = 1, MaxObservedLamport = value });
        else if (value > state.MaxObservedLamport)
            state.MaxObservedLamport = value;
        else
            return;
        await db.SaveChangesAsync();
    }

    public async Task<string?> GetSyncFingerprintAsync()
    {
        using var db = _dbFactory();
        var state = await db.SyncStates.AsNoTracking().FirstOrDefaultAsync(s => s.Id == 1);
        return state?.SyncFingerprint;
    }

    public async Task SetSyncFingerprintAsync(string fingerprint)
    {
        using var db = _dbFactory();
        var state = await db.SyncStates.FirstOrDefaultAsync(s => s.Id == 1);
        if (state is null)
            db.SyncStates.Add(new SyncState { Id = 1, SyncFingerprint = fingerprint });
        else
            state.SyncFingerprint = fingerprint;
        await db.SaveChangesAsync();
    }

    public async Task DeleteLocallyAsync(SyncEntityKind kind, Guid id)
    {
        var ops = OpsFor(kind);
        using var db = _dbFactory();
        await ops.Delete(db, id);
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<string>> GetReferencedImageKeysAsync()
    {
        using var db = _dbFactory();
        var items = await WithItemDetails(db.Items.AsNoTracking()).ToListAsync();
        var keys = new HashSet<string>();
        foreach (var item in items)
            foreach (var value in item.Values)
                keys.UnionWith(value.ReferencedBlobKeys());
        return keys.ToList();
    }

    private void CopySyncMetadata(ISyncable source, ISyncable target)
    {
        target.UpdatedAt = source.UpdatedAt;
        target.Revision = source.Revision;
        target.BaseRevision = source.BaseRevision;
        target.IsDirty = source.IsDirty;
        target.LastModifiedByUserId = source.LastModifiedByUserId;
        target.Lamport = source.Lamport;
        target.LastModifiedByDeviceId = source.LastModifiedByDeviceId;
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

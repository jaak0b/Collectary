using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Logging;
using Collectary.Core.Ports;
using Microsoft.EntityFrameworkCore;

namespace Collectary.Infrastructure.Persistence;

public class PresetRepository : IPresetRepository
{
    private readonly Func<InventoryDbContext> _dbFactory;
    private readonly IFieldDefinitionMerger _merger;
    private readonly IAppLogger _logger;
    private readonly ICurrentUser? _currentUser;

    public PresetRepository(Func<InventoryDbContext> dbFactory, IFieldDefinitionMerger merger, IAppLogger? logger = null, ICurrentUser? currentUser = null)
    {
        _dbFactory = dbFactory;
        _merger = merger;
        _logger = logger ?? new NullAppLogger();
        _currentUser = currentUser;
    }

    private IQueryable<Preset> WithDetails(IQueryable<Preset> query) =>
        query
            .Include(p => p.Fields)
            .Include(p => p.Fields).ThenInclude(f => ((ListFieldDefinition)f).SubFields)
            .Include(p => p.Fields).ThenInclude(f => ((ListFieldDefinition)f).Groups)
            .Include(p => p.Groups)
            .Include(p => p.SharedFieldRefs).ThenInclude(r => r.SharedField).ThenInclude(sf => sf.Definition)
            .Include(p => p.SharedFieldRefs).ThenInclude(r => r.SharedField).ThenInclude(sf => ((ListFieldDefinition)sf.Definition).SubFields)
            .Include(p => p.SharedFieldRefs).ThenInclude(r => r.SharedField).ThenInclude(sf => ((ListFieldDefinition)sf.Definition).Groups)
            .AsSplitQuery();

    public async Task<IReadOnlyList<Preset>> GetAllAsync()
    {
        using var db = _dbFactory();
        var query = WithDetails(db.Presets).AsNoTracking();
        if (_currentUser?.IsAuthenticated == true)
        {
            var uid = _currentUser.UserId;
            var sharedIds = await db.CollectionShares
                .Where(s => s.SharedWithUserId == uid)
                .Select(s => s.PresetId)
                .ToListAsync();
            query = query.Where(p => p.OwnerId == uid || sharedIds.Contains(p.Id));
        }

        return await query.OrderBy(p => p.DisplayOrder).ToListAsync();
    }

    public async Task<Preset?> GetByIdAsync(Guid id)
    {
        using var db = _dbFactory();
        return await WithDetails(db.Presets)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IReadOnlyList<Preset>> GetChildrenAsync(Guid parentId)
    {
        using var db = _dbFactory();
        return await WithDetails(db.Presets)
            .AsNoTracking()
            .Where(p => p.ParentPresetId == parentId)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync();
    }

    public async Task AddAsync(Preset preset)
    {
        using var db = _dbFactory();
        if (_currentUser?.IsAuthenticated == true)
            preset.OwnerId ??= _currentUser.UserId;

        preset.UpdatedAt = DateTime.UtcNow;
        ((ISyncable)preset).StampModified(_currentUser?.AuthenticatedId);
        db.Presets.Add(preset);
        await db.SaveChangesAsync();
        _logger.Debug("Added preset id={Id} name={Name} fields={Fields} groups={Groups} systemRefs={Refs}",
            preset.Id, preset.Name, preset.Fields.Count, preset.Groups.Count, preset.SharedFieldRefs.Count);
    }

    public async Task UpdateAsync(Preset preset)
    {
        using var db = _dbFactory();
        var tracked = await WithDetails(db.Presets)
            .FirstOrDefaultAsync(p => p.Id == preset.Id);
        if (tracked is null) return;

        tracked.Name = preset.Name;
        tracked.ColumnCount = preset.ColumnCount;
        tracked.FieldLabelLayout = preset.FieldLabelLayout;
        tracked.ParentPresetId = preset.ParentPresetId;

        _logger.Debug("Updating preset id={Id} name={Name} fields={Fields} groups={Groups} systemRefs={Refs}",
            preset.Id, preset.Name, preset.Fields.Count, preset.Groups.Count, preset.SharedFieldRefs.Count);

        _merger.MergePreset(db, tracked, preset);

        tracked.UpdatedAt = DateTime.UtcNow;
        ((ISyncable)tracked).StampModified(_currentUser?.AuthenticatedId);

        await db.SaveChangesAsync();
    }

    public async Task UpdateDisplayOrdersAsync(IReadOnlyList<Preset> ordered)
    {
        using var db = _dbFactory();
        var ids = ordered.Select(p => p.Id).ToList();
        var lookup = await db.Presets
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);
        for (var i = 0; i < ordered.Count; i++)
            if (lookup.TryGetValue(ordered[i].Id, out var tracked))
                tracked.DisplayOrder = i;
        await db.SaveChangesAsync();
    }

    public async Task BackfillOwnerlessAsync(Guid ownerId)
    {
        using var db = _dbFactory();
        var ownerless = await db.Presets.IgnoreQueryFilters()
            .Where(p => p.OwnerId == null)
            .ToListAsync();
        foreach (var preset in ownerless)
            preset.OwnerId = ownerId;
        await db.SaveChangesAsync();
    }

    public async Task TransferOwnershipAsync(Guid presetId, Guid newOwnerId)
    {
        using var db = _dbFactory();
        var all = await db.Presets.IgnoreQueryFilters().ToListAsync();
        var byParent = all.ToLookup(p => p.ParentPresetId);

        var root = all.FirstOrDefault(p => p.Id == presetId);
        if (root is null) return;

        var queue = new Queue<Preset>();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            current.OwnerId = newOwnerId;
            current.UpdatedAt = DateTime.UtcNow;
            ((ISyncable)current).StampModified(_currentUser?.AuthenticatedId);
            foreach (var child in byParent[current.Id])
                queue.Enqueue(child);
        }

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        using var db = _dbFactory();
        var preset = await db.Presets.FindAsync(id);
        if (preset is null) return;

        db.Presets.Remove(preset);
        db.Tombstones.Add(new Tombstone { Id = id });

        await db.SaveChangesAsync();
    }
}

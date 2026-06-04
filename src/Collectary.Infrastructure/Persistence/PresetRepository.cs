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

    public PresetRepository(Func<InventoryDbContext> dbFactory, IFieldDefinitionMerger merger, IAppLogger? logger = null)
    {
        _dbFactory = dbFactory;
        _merger = merger;
        _logger = logger ?? new NullAppLogger();
    }

    public async Task<IReadOnlyList<Preset>> GetAllAsync()
    {
        using var db = _dbFactory();
        return await db.Presets
            .Include(p => p.Fields)
            .Include(p => p.Fields).ThenInclude(f => ((ListFieldDefinition)f).SubFields)
            .Include(p => p.Fields).ThenInclude(f => ((ListFieldDefinition)f).Groups)
            .Include(p => p.Groups)
            .Include(p => p.SystemFieldRefs).ThenInclude(r => r.SystemField).ThenInclude(sf => sf.Definition)
            .Include(p => p.SystemFieldRefs).ThenInclude(r => r.SystemField).ThenInclude(sf => ((ListFieldDefinition)sf.Definition).SubFields)
            .Include(p => p.SystemFieldRefs).ThenInclude(r => r.SystemField).ThenInclude(sf => ((ListFieldDefinition)sf.Definition).Groups)
            .AsSplitQuery()
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync();
    }

    public async Task<Preset?> GetByIdAsync(Guid id)
    {
        using var db = _dbFactory();
        return await db.Presets
            .Include(p => p.Fields)
            .Include(p => p.Fields).ThenInclude(f => ((ListFieldDefinition)f).SubFields)
            .Include(p => p.Fields).ThenInclude(f => ((ListFieldDefinition)f).Groups)
            .Include(p => p.Groups)
            .Include(p => p.SystemFieldRefs).ThenInclude(r => r.SystemField).ThenInclude(sf => sf.Definition)
            .Include(p => p.SystemFieldRefs).ThenInclude(r => r.SystemField).ThenInclude(sf => ((ListFieldDefinition)sf.Definition).SubFields)
            .Include(p => p.SystemFieldRefs).ThenInclude(r => r.SystemField).ThenInclude(sf => ((ListFieldDefinition)sf.Definition).Groups)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IReadOnlyList<Preset>> GetChildrenAsync(Guid parentId)
    {
        using var db = _dbFactory();
        return await db.Presets
            .Include(p => p.Fields)
            .Include(p => p.Fields).ThenInclude(f => ((ListFieldDefinition)f).SubFields)
            .Include(p => p.Fields).ThenInclude(f => ((ListFieldDefinition)f).Groups)
            .Include(p => p.Groups)
            .Include(p => p.SystemFieldRefs).ThenInclude(r => r.SystemField).ThenInclude(sf => sf.Definition)
            .Include(p => p.SystemFieldRefs).ThenInclude(r => r.SystemField).ThenInclude(sf => ((ListFieldDefinition)sf.Definition).SubFields)
            .Include(p => p.SystemFieldRefs).ThenInclude(r => r.SystemField).ThenInclude(sf => ((ListFieldDefinition)sf.Definition).Groups)
            .AsSplitQuery()
            .Where(p => p.ParentPresetId == parentId)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync();
    }

    public async Task AddAsync(Preset preset)
    {
        using var db = _dbFactory();
        db.Presets.Add(preset);
        await db.SaveChangesAsync();
        _logger.Debug("Added preset id={Id} name={Name} fields={Fields} groups={Groups} systemRefs={Refs}",
            preset.Id, preset.Name, preset.Fields.Count, preset.Groups.Count, preset.SystemFieldRefs.Count);
    }

    public async Task UpdateAsync(Preset preset)
    {
        using var db = _dbFactory();
        var tracked = await db.Presets
            .Include(p => p.Fields)
            .Include(p => p.Fields).ThenInclude(f => ((ListFieldDefinition)f).SubFields)
            .Include(p => p.Fields).ThenInclude(f => ((ListFieldDefinition)f).Groups)
            .Include(p => p.Groups)
            .Include(p => p.SystemFieldRefs)
            .FirstOrDefaultAsync(p => p.Id == preset.Id);
        if (tracked is null) return;

        tracked.Name = preset.Name;
        tracked.ColumnCount = preset.ColumnCount;
        tracked.ParentPresetId = preset.ParentPresetId;

        _logger.Debug("Updating preset id={Id} name={Name} fields={Fields} groups={Groups} systemRefs={Refs}",
            preset.Id, preset.Name, preset.Fields.Count, preset.Groups.Count, preset.SystemFieldRefs.Count);

        var removedGroupIds = _merger.SyncGroups(
            db, tracked.Groups, preset.Groups, g => g.PresetId = tracked.Id);
        foreach (var f in preset.Fields)
            if (f.GroupId is Guid fid && removedGroupIds.Contains(fid)) f.GroupId = null;
        foreach (var r in preset.SystemFieldRefs)
            if (r.GroupId is Guid rid && removedGroupIds.Contains(rid)) r.GroupId = null;

        var refsToRemove = tracked.SystemFieldRefs
            .Where(e => preset.SystemFieldRefs.All(u => u.SystemFieldId != e.SystemFieldId))
            .ToList();
        foreach (var r in refsToRemove) tracked.SystemFieldRefs.Remove(r);

        foreach (var updatedRef in preset.SystemFieldRefs)
        {
            var existingRef = tracked.SystemFieldRefs.FirstOrDefault(r => r.SystemFieldId == updatedRef.SystemFieldId);
            if (existingRef is null)
                tracked.SystemFieldRefs.Add(new PresetSystemField
                {
                    PresetId = tracked.Id,
                    SystemFieldId = updatedRef.SystemFieldId,
                    GroupId = updatedRef.GroupId,
                    DisplayOrder = updatedRef.DisplayOrder
                });
            else
            {
                existingRef.DisplayOrder = updatedRef.DisplayOrder;
                existingRef.GroupId = updatedRef.GroupId;
            }
        }

        var trackedTopLevel = tracked.Fields
            .Where(f => f.ParentListFieldDefinitionId == null)
            .ToList();

        var toRemove = trackedTopLevel
            .Where(existing => preset.Fields.All(updated => updated.Id != existing.Id))
            .ToList();
        db.FieldDefinitions.RemoveRange(toRemove);

        foreach (var updatedField in preset.Fields)
        {
            var existingField = trackedTopLevel.FirstOrDefault(f => f.Id == updatedField.Id);
            if (existingField is null)
            {
                updatedField.PresetId = tracked.Id;
                tracked.Fields.Add(updatedField);
            }
            else
            {
                existingField.DisplayOrder = updatedField.DisplayOrder;
                _merger.Apply(db, existingField, updatedField);
            }
        }

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

    public async Task DeleteAsync(Guid id)
    {
        using var db = _dbFactory();
        var preset = await db.Presets.FindAsync(id);
        if (preset is not null)
        {
            db.Presets.Remove(preset);
            await db.SaveChangesAsync();
        }
    }
}

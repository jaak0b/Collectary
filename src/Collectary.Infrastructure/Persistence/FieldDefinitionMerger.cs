using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Logging;
using Collectary.Core.Ports;

namespace Collectary.Infrastructure.Persistence;

public class FieldDefinitionMerger : IFieldDefinitionMerger
{
    private readonly IAppLogger _logger;

    public FieldDefinitionMerger(IAppLogger? logger = null)
    {
        _logger = logger ?? new NullAppLogger();
    }

    public void MergePreset(InventoryDbContext db, Preset tracked, Preset incoming)
    {
        var removedGroupIds = SyncGroups(db, tracked.Groups, incoming.Groups, g => g.PresetId = tracked.Id);
        foreach (var f in incoming.Fields)
            if (f.GroupId is Guid fid && removedGroupIds.Contains(fid)) f.GroupId = null;
        foreach (var r in incoming.SharedFieldRefs)
            if (r.GroupId is Guid rid && removedGroupIds.Contains(rid)) r.GroupId = null;

        var refsToRemove = tracked.SharedFieldRefs
            .Where(e => incoming.SharedFieldRefs.All(u => u.SharedFieldId != e.SharedFieldId))
            .ToList();
        foreach (var r in refsToRemove) tracked.SharedFieldRefs.Remove(r);

        foreach (var updatedRef in incoming.SharedFieldRefs)
        {
            var existingRef = tracked.SharedFieldRefs.FirstOrDefault(r => r.SharedFieldId == updatedRef.SharedFieldId);
            if (existingRef is null)
                tracked.SharedFieldRefs.Add(new PresetSharedField
                {
                    PresetId = tracked.Id,
                    SharedFieldId = updatedRef.SharedFieldId,
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
            .Where(existing => incoming.Fields.All(updated => updated.Id != existing.Id))
            .ToList();
        db.FieldDefinitions.RemoveRange(toRemove);

        foreach (var updatedField in incoming.Fields)
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
                Apply(db, existingField, updatedField);
            }
        }
    }

    public void Apply(InventoryDbContext db, FieldDefinition existing, FieldDefinition updated)
    {
        existing.Label = updated.Label;
        existing.IsRequired = updated.IsRequired;
        existing.ColumnSpan = updated.ColumnSpan;
        existing.GroupId = updated.GroupId;

        if (existing is IListDisplayable existingLd && updated is IListDisplayable updatedLd)
            existingLd.ShowInList = updatedLd.ShowInList;

        existing.ApplyTypeSpecificProperties(updated);

        if (existing is ListFieldDefinition existingList && updated is ListFieldDefinition updatedList)
            SyncSubFields(db, existingList, updatedList);
    }

    public void SyncSubFields(InventoryDbContext db, ListFieldDefinition existing, ListFieldDefinition updated)
    {
        var removedGroupIds = SyncGroups(db, existing.Groups, updated.Groups,
            g => g.ParentListFieldDefinitionId = existing.Id);
        foreach (var sub in updated.SubFields)
            if (sub.GroupId is Guid id && removedGroupIds.Contains(id)) sub.GroupId = null;

        var toRemove = existing.SubFields
            .Where(e => updated.SubFields.All(u => u.Id != e.Id))
            .ToList();
        db.FieldDefinitions.RemoveRange(toRemove);

        foreach (var updatedSub in updated.SubFields)
        {
            var existingSub = existing.SubFields.FirstOrDefault(e => e.Id == updatedSub.Id);
            if (existingSub is null)
            {
                updatedSub.ParentListFieldDefinitionId = existing.Id;
                existing.SubFields.Add(updatedSub);
            }
            else
            {
                Apply(db, existingSub, updatedSub);
                existingSub.DisplayOrder = updatedSub.DisplayOrder;
            }
        }
    }

    public HashSet<Guid> SyncGroups(
        InventoryDbContext db,
        ICollection<FieldGroup> existing,
        IReadOnlyList<FieldGroup> updated,
        Action<FieldGroup> assignOwner)
    {
        var removed = existing
            .Where(e => updated.All(u => u.Id != e.Id))
            .ToList();
        foreach (var group in removed)
        {
            db.Set<FieldGroup>().Remove(group);
            existing.Remove(group);
        }

        var added = 0;
        var updatedCount = 0;
        foreach (var updatedGroup in updated)
        {
            var existingGroup = existing.FirstOrDefault(g => g.Id == updatedGroup.Id);
            if (existingGroup is null)
            {
                assignOwner(updatedGroup);
                existing.Add(updatedGroup);
                added++;
            }
            else
            {
                updatedCount++;
                existingGroup.Name = updatedGroup.Name;
                existingGroup.DisplayOrder = updatedGroup.DisplayOrder;
                existingGroup.DisplayMode = updatedGroup.DisplayMode;
                existingGroup.ColumnCount = updatedGroup.ColumnCount;
                existingGroup.DefaultCollapsed = updatedGroup.DefaultCollapsed;
                existingGroup.ParentGroupId = updatedGroup.ParentGroupId;
                existingGroup.ShowInList = updatedGroup.ShowInList;
                existingGroup.PrefixColumnHeaders = updatedGroup.PrefixColumnHeaders;
            }
        }

        _logger.Debug("SyncGroups: added={Added} updated={Updated} removed={Removed}",
            added, updatedCount, removed.Count);

        return removed.Select(g => g.Id).ToHashSet();
    }
}

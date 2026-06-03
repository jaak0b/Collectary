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

    public void Apply(InventoryDbContext db, FieldDefinition existing, FieldDefinition updated)
    {
        existing.Label = updated.Label;
        existing.IsRequired = updated.IsRequired;
        existing.ColumnSpan = updated.ColumnSpan;
        existing.GroupId = updated.GroupId;

        if (existing is IListDisplayable existingLd && updated is IListDisplayable updatedLd)
            existingLd.ShowInList = updatedLd.ShowInList;

        if (existing is ColorFieldDefinition existingCd && updated is ColorFieldDefinition updatedCd)
            existingCd.Format = updatedCd.Format;

        if (existing is ImageFieldDefinition existingImg && updated is ImageFieldDefinition updatedImg)
        {
            existingImg.DisplayWidth = updatedImg.DisplayWidth;
            existingImg.DisplayHeight = updatedImg.DisplayHeight;
            existingImg.SizeMode = updatedImg.SizeMode;
        }

        if (existing is SingleChoiceFieldDefinition existingSc && updated is SingleChoiceFieldDefinition updatedSc)
            ReplaceChoices(existingSc.Choices, updatedSc.Choices);

        if (existing is MultiChoiceFieldDefinition existingMc && updated is MultiChoiceFieldDefinition updatedMc)
            ReplaceChoices(existingMc.Choices, updatedMc.Choices);

        if (existing is CurrencyFieldDefinition existingCurr && updated is CurrencyFieldDefinition updatedCurr)
            existingCurr.CurrencySymbol = updatedCurr.CurrencySymbol;

        if (existing is RatingFieldDefinition existingRating && updated is RatingFieldDefinition updatedRating)
            existingRating.MaxStars = updatedRating.MaxStars;

        if (existing is ListFieldDefinition existingList && updated is ListFieldDefinition updatedList)
        {
            existingList.ColumnCount = updatedList.ColumnCount;
            existingList.InlineStyle = updatedList.InlineStyle;
            SyncSubFields(db, existingList, updatedList);
        }
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

    private static void ReplaceChoices(ICollection<ChoiceOption> existing, IEnumerable<ChoiceOption> updated)
    {
        existing.Clear();
        foreach (var c in updated)
            existing.Add(new ChoiceOption { Id = c.Id, Value = c.Value, DisplayOrder = c.DisplayOrder });
    }
}

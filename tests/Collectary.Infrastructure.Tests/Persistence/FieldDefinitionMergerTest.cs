using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Infrastructure.Persistence;

namespace Collectary.Infrastructure.Tests.Persistence;

file sealed class RecordingLogger : IAppLogger
{
    public int DebugCallCount { get; private set; }
    public void Verbose(string t, params object?[] a) { }
    public void Debug(string t, params object?[] a) => DebugCallCount++;
    public void Information(string t, params object?[] a) { }
    public void Warning(string t, params object?[] a) { }
    public void Error(Exception e, string t, params object?[] a) { }
}

[TestFixture]
public class FieldDefinitionMergerTest : DbIntegrationTestBase
{
    private FieldDefinitionMerger _sut = null!;

    [SetUp]
    public new void BaseSetUp()
    {
        base.BaseSetUp();
        _sut = new FieldDefinitionMerger();
    }

    [Test]
    public void Apply_CopiesScalarsAndGroupId()
    {
        using var db = DbFactory();
        var groupId = Guid.NewGuid();
        var existing = new TextFieldDefinition { Label = "Old", IsRequired = false, GroupId = null };
        var updated = new TextFieldDefinition { Label = "New", IsRequired = true, GroupId = groupId };

        _sut.Apply(db, existing, updated);

        Assert.That(existing.Label, Is.EqualTo("New"));
        Assert.That(existing.IsRequired, Is.True);
        Assert.That(existing.GroupId, Is.EqualTo(groupId));
    }

    [Test]
    public void Apply_ReplacesChoices()
    {
        using var db = DbFactory();
        var existing = new SingleChoiceFieldDefinition { Label = "C", Choices = [new ChoiceOption { Value = "A" }] };
        var updated = new SingleChoiceFieldDefinition
        {
            Label = "C",
            Choices = [new ChoiceOption { Value = "X" }, new ChoiceOption { Value = "Y" }]
        };

        _sut.Apply(db, existing, updated);

        Assert.That(existing.Choices.Select(c => c.Value), Is.EqualTo(new[] { "X", "Y" }));
    }

    [Test]
    public void SyncGroups_AddsNewGroupAndAssignsOwner()
    {
        using var db = DbFactory();
        var existing = new List<FieldGroup>();
        var presetId = Guid.NewGuid();
        var updated = new List<FieldGroup> { new() { Name = "Specs", DisplayOrder = 0 } };

        var removed = _sut.SyncGroups(db, existing, updated, g => g.PresetId = presetId);

        Assert.That(existing, Has.Count.EqualTo(1));
        Assert.That(existing[0].PresetId, Is.EqualTo(presetId));
        Assert.That(removed, Is.Empty);
    }

    [Test]
    public void SyncGroups_UpdatesAllScalarProperties()
    {
        using var db = DbFactory();
        var id = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var existing = new List<FieldGroup>
        {
            new() { Id = id, Name = "Old", DisplayOrder = 0, DisplayMode = GroupDisplayMode.Card, ShowInList = true }
        };
        var updated = new List<FieldGroup>
        {
            new()
            {
                Id = id, Name = "New", DisplayOrder = 3, DisplayMode = GroupDisplayMode.Tab,
                DefaultCollapsed = true, ParentGroupId = parentId, ShowInList = false, PrefixColumnHeaders = true
            }
        };

        _sut.SyncGroups(db, existing, updated, _ => { });

        var g = existing.Single();
        Assert.That(g.Name, Is.EqualTo("New"));
        Assert.That(g.DisplayOrder, Is.EqualTo(3));
        Assert.That(g.DisplayMode, Is.EqualTo(GroupDisplayMode.Tab));
        Assert.That(g.DefaultCollapsed, Is.True);
        Assert.That(g.ParentGroupId, Is.EqualTo(parentId));
        Assert.That(g.ShowInList, Is.False);
        Assert.That(g.PrefixColumnHeaders, Is.True);
    }

    [Test]
    public void SyncGroups_RemovesMissingAndReturnsTheirIds()
    {
        using var db = DbFactory();
        var keep = new FieldGroup { Id = Guid.NewGuid(), Name = "Keep" };
        var drop = new FieldGroup { Id = Guid.NewGuid(), Name = "Drop" };
        var existing = new List<FieldGroup> { keep, drop };
        var updated = new List<FieldGroup> { keep };

        var removed = _sut.SyncGroups(db, existing, updated, _ => { });

        Assert.That(existing, Has.Count.EqualTo(1));
        Assert.That(existing.Single().Id, Is.EqualTo(keep.Id));
        Assert.That(removed, Is.EquivalentTo(new[] { drop.Id }));
    }

    [Test]
    public void SyncGroups_CallsLoggerDebug()
    {
        using var db = DbFactory();
        var logger = new RecordingLogger();
        var sut = new FieldDefinitionMerger(logger);

        sut.SyncGroups(db, new List<FieldGroup>(), new List<FieldGroup>(), _ => { });

        Assert.That(logger.DebugCallCount, Is.GreaterThan(0));
    }

    [Test]
    public void SyncGroups_NullLoggerUsesDefault_DoesNotThrow()
    {
        using var db = DbFactory();
        var sut = new FieldDefinitionMerger(null);

        Assert.DoesNotThrow(() => sut.SyncGroups(db, new List<FieldGroup>(), new List<FieldGroup>(), _ => { }));
    }

    [Test]
    public void SyncSubFields_WhenSubFieldExistsInUpdated_DoesNotRemoveIt()
    {
        using var db = DbFactory();
        var keptId = Guid.NewGuid();
        var existing = new ListFieldDefinition
        {
            Id = Guid.NewGuid(),
            SubFields = [new TextFieldDefinition { Id = keptId, Label = "Kept" }],
            Groups = []
        };
        var updated = new ListFieldDefinition
        {
            Id = existing.Id,
            SubFields = [new TextFieldDefinition { Id = keptId, Label = "KeptUpdated" }, new TextFieldDefinition { Label = "New" }],
            Groups = []
        };

        _sut.SyncSubFields(db, existing, updated);

        Assert.That(existing.SubFields.Any(f => f.Id == keptId), Is.True);
    }

    [Test]
    public void SyncGroups_RemovedGroupIdIsReturned()
    {
        using var db = DbFactory();
        var removedGroup = new FieldGroup { Id = Guid.NewGuid(), Name = "Remove" };
        var existing = new List<FieldGroup> { removedGroup };
        var updated = new List<FieldGroup>();

        var removed = _sut.SyncGroups(db, existing, updated, _ => { });

        Assert.That(removed, Does.Contain(removedGroup.Id));
    }

    [Test]
    public void SyncGroups_DoesNotRemoveGroupStillPresentInUpdated()
    {
        using var db = DbFactory();
        var kept = new FieldGroup { Id = Guid.NewGuid(), Name = "Keep" };
        var existing = new List<FieldGroup> { kept };
        var updated = new List<FieldGroup> { new() { Id = kept.Id, Name = "Keep" } };

        var removed = _sut.SyncGroups(db, existing, updated, _ => { });

        Assert.That(removed, Is.Empty, "A group present in the updated set must not be removed");
        Assert.That(existing, Has.Count.EqualTo(1));
    }

    [Test]
    public void SyncSubFields_NullsGroupIdOfSubFieldWhoseGroupWasRemoved()
    {
        using var db = DbFactory();
        var removedGroup = new FieldGroup { Id = Guid.NewGuid(), Name = "Gone", DisplayOrder = 0 };
        var subId = Guid.NewGuid();
        var existing = new ListFieldDefinition
        {
            Id = Guid.NewGuid(),
            Groups = [removedGroup],
            SubFields = [new TextFieldDefinition { Id = subId, Label = "S", GroupId = removedGroup.Id }]
        };
        var updated = new ListFieldDefinition
        {
            Id = existing.Id,
            Groups = [],
            SubFields = [new TextFieldDefinition { Id = subId, Label = "S", GroupId = removedGroup.Id }]
        };

        _sut.SyncSubFields(db, existing, updated);

        var sub = updated.SubFields.Single(f => f.Id == subId);
        Assert.That(sub.GroupId, Is.Null, "A sub-field assigned to a removed group must be ungrouped");
    }

    [Test]
    public void SyncSubFields_KeepsGroupIdOfSubFieldWhoseGroupSurvives()
    {
        using var db = DbFactory();
        var keptGroup = new FieldGroup { Id = Guid.NewGuid(), Name = "Kept", DisplayOrder = 0 };
        var subId = Guid.NewGuid();
        var existing = new ListFieldDefinition
        {
            Id = Guid.NewGuid(),
            Groups = [keptGroup],
            SubFields = [new TextFieldDefinition { Id = subId, Label = "S", GroupId = keptGroup.Id }]
        };
        var updated = new ListFieldDefinition
        {
            Id = existing.Id,
            Groups = [new FieldGroup { Id = keptGroup.Id, Name = "Kept", DisplayOrder = 0 }],
            SubFields = [new TextFieldDefinition { Id = subId, Label = "S", GroupId = keptGroup.Id }]
        };

        _sut.SyncSubFields(db, existing, updated);

        var sub = updated.SubFields.Single(f => f.Id == subId);
        Assert.That(sub.GroupId, Is.EqualTo(keptGroup.Id), "A sub-field in a surviving group keeps its assignment");
    }

    [Test]
    public void Apply_ListField_SyncsSubFieldsAndGroups()
    {
        using var db = DbFactory();
        var keptSub = new TextFieldDefinition { Id = Guid.NewGuid(), Label = "Kept", DisplayOrder = 0 };
        var existing = new ListFieldDefinition
        {
            Id = Guid.NewGuid(),
            InlineStyle = ListInlineStyle.Card,
            SubFields = [keptSub],
            Groups = []
        };
        var updated = new ListFieldDefinition
        {
            Id = existing.Id,
            InlineStyle = ListInlineStyle.Grid,
            SubFields =
            [
                new TextFieldDefinition { Id = keptSub.Id, Label = "KeptUpdated", DisplayOrder = 0 },
                new TextFieldDefinition { Id = Guid.NewGuid(), Label = "Added", DisplayOrder = 1 }
            ],
            Groups = [new FieldGroup { Id = Guid.NewGuid(), Name = "SubGroup", DisplayOrder = 0 }]
        };

        _sut.Apply(db, existing, updated);

        Assert.That(existing.InlineStyle, Is.EqualTo(ListInlineStyle.Grid));
        Assert.That(existing.SubFields, Has.Count.EqualTo(2));
        Assert.That(existing.SubFields.Any(f => f.Label == "Added"), Is.True);
        Assert.That(existing.Groups, Has.Count.EqualTo(1));
        Assert.That(existing.Groups[0].ParentListFieldDefinitionId, Is.EqualTo(existing.Id));
    }
}

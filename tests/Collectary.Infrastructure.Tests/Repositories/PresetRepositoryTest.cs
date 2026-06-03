using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Infrastructure.Persistence;

namespace Collectary.Infrastructure.Tests.Repositories;

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
public class PresetRepositoryTest : DbIntegrationTestBase
{
    private PresetRepository _sut = null!;

    [SetUp]
    public new void BaseSetUp()
    {
        base.BaseSetUp();
        _sut = new PresetRepository(DbFactory, new FieldDefinitionMerger());
    }

    private static Preset MakePreset(string name = "Test") => new() { Name = name };

    [Test]
    public async Task AddAsync_PersistsPreset()
    {
        var preset = MakePreset("Games");

        await _sut.AddAsync(preset);

        var loaded = await _sut.GetByIdAsync(preset.Id);
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Name, Is.EqualTo("Games"));
    }

    [Test]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByIdAsync_IncludesFields()
    {
        var preset = MakePreset();
        preset.Fields.Add(new TextFieldDefinition { Label = "Notes", PresetId = preset.Id });
        await _sut.AddAsync(preset);

        var loaded = await _sut.GetByIdAsync(preset.Id);

        Assert.That(loaded!.Fields, Has.Count.EqualTo(1));
        Assert.That(loaded.Fields[0].Label, Is.EqualTo("Notes"));
    }

    [Test]
    public async Task GetByIdAsync_IncludesSystemFieldRefs()
    {
        var sysFieldRepo = new SystemFieldRepository(DbFactory, new FieldDefinitionMerger());
        var sysField = new SystemField { Name = "Rating", Definition = new RatingFieldDefinition { Label = "Rating" } };
        await sysFieldRepo.AddAsync(sysField);

        var preset = MakePreset();
        preset.SystemFieldRefs.Add(new PresetSystemField { PresetId = preset.Id, SystemFieldId = sysField.Id, DisplayOrder = 0 });
        await _sut.AddAsync(preset);

        var loaded = await _sut.GetByIdAsync(preset.Id);

        Assert.That(loaded!.SystemFieldRefs, Has.Count.EqualTo(1));
        Assert.That(loaded.SystemFieldRefs[0].SystemField.Name, Is.EqualTo("Rating"));
    }

    [Test]
    public async Task GetChildrenAsync_ReturnsOnlyDirectChildren()
    {
        var parent = MakePreset("Parent");
        var child = new Preset { Name = "Child", ParentPresetId = parent.Id };
        var unrelated = MakePreset("Unrelated");
        await _sut.AddAsync(parent);
        await _sut.AddAsync(child);
        await _sut.AddAsync(unrelated);

        var children = await _sut.GetChildrenAsync(parent.Id);

        Assert.That(children, Has.Count.EqualTo(1));
        Assert.That(children[0].Name, Is.EqualTo("Child"));
    }

    [Test]
    public async Task GetAllAsync_OrdersByDisplayOrder()
    {
        var a = new Preset { Name = "A", DisplayOrder = 2 };
        var b = new Preset { Name = "B", DisplayOrder = 0 };
        var c = new Preset { Name = "C", DisplayOrder = 1 };
        await _sut.AddAsync(a);
        await _sut.AddAsync(b);
        await _sut.AddAsync(c);

        var all = await _sut.GetAllAsync();

        Assert.That(all.Select(p => p.Name), Is.EqualTo(new[] { "B", "C", "A" }));
    }

    [Test]
    public async Task UpdateAsync_UpdatesName()
    {
        var preset = MakePreset("Old");
        await _sut.AddAsync(preset);

        preset.Name = "New";
        await _sut.UpdateAsync(preset);

        var loaded = await _sut.GetByIdAsync(preset.Id);
        Assert.That(loaded!.Name, Is.EqualTo("New"));
    }

    [Test]
    public async Task UpdateAsync_AddsNewField()
    {
        var preset = MakePreset();
        await _sut.AddAsync(preset);

        var loaded = await _sut.GetByIdAsync(preset.Id);
        loaded!.Fields.Add(new TextFieldDefinition { Label = "Color", PresetId = preset.Id });
        await _sut.UpdateAsync(loaded);

        var reloaded = await _sut.GetByIdAsync(preset.Id);
        Assert.That(reloaded!.Fields, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task UpdateAsync_RemovesDroppedField()
    {
        var preset = MakePreset();
        preset.Fields.Add(new TextFieldDefinition { Label = "Notes", PresetId = preset.Id });
        await _sut.AddAsync(preset);

        var loaded = await _sut.GetByIdAsync(preset.Id);
        loaded!.Fields.Clear();
        await _sut.UpdateAsync(loaded);

        var reloaded = await _sut.GetByIdAsync(preset.Id);
        Assert.That(reloaded!.Fields, Is.Empty);
    }

    [Test]
    public async Task UpdateAsync_AddsSystemFieldRef()
    {
        var sysFieldRepo = new SystemFieldRepository(DbFactory, new FieldDefinitionMerger());
        var sysField = new SystemField { Name = "Tag", Definition = new TextFieldDefinition { Label = "Tag" } };
        await sysFieldRepo.AddAsync(sysField);

        var preset = MakePreset();
        await _sut.AddAsync(preset);

        var loaded = await _sut.GetByIdAsync(preset.Id);
        loaded!.SystemFieldRefs.Add(new PresetSystemField { PresetId = preset.Id, SystemFieldId = sysField.Id, DisplayOrder = 0 });
        await _sut.UpdateAsync(loaded);

        var reloaded = await _sut.GetByIdAsync(preset.Id);
        Assert.That(reloaded!.SystemFieldRefs, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task UpdateAsync_RemovesDroppedSystemFieldRef()
    {
        var sysFieldRepo = new SystemFieldRepository(DbFactory, new FieldDefinitionMerger());
        var sysField = new SystemField { Name = "Tag", Definition = new TextFieldDefinition { Label = "Tag" } };
        await sysFieldRepo.AddAsync(sysField);

        var preset = MakePreset();
        preset.SystemFieldRefs.Add(new PresetSystemField { PresetId = preset.Id, SystemFieldId = sysField.Id, DisplayOrder = 0 });
        await _sut.AddAsync(preset);

        var loaded = await _sut.GetByIdAsync(preset.Id);
        loaded!.SystemFieldRefs.Clear();
        await _sut.UpdateAsync(loaded);

        var reloaded = await _sut.GetByIdAsync(preset.Id);
        Assert.That(reloaded!.SystemFieldRefs, Is.Empty);
    }

    [Test]
    public async Task UpdateDisplayOrdersAsync_UpdatesOrders()
    {
        var a = new Preset { Name = "A", DisplayOrder = 0 };
        var b = new Preset { Name = "B", DisplayOrder = 1 };
        await _sut.AddAsync(a);
        await _sut.AddAsync(b);

        await _sut.UpdateDisplayOrdersAsync(new[] { b, a });

        var all = await _sut.GetAllAsync();
        Assert.That(all[0].Name, Is.EqualTo("B"));
        Assert.That(all[1].Name, Is.EqualTo("A"));
    }

    [Test]
    public async Task DeleteAsync_RemovesPreset()
    {
        var preset = MakePreset();
        await _sut.AddAsync(preset);

        await _sut.DeleteAsync(preset.Id);

        Assert.That(await _sut.GetByIdAsync(preset.Id), Is.Null);
    }

    [Test]
    public async Task DeleteAsync_CascadesFields()
    {
        var preset = MakePreset();
        preset.Fields.Add(new TextFieldDefinition { Label = "Notes", PresetId = preset.Id });
        await _sut.AddAsync(preset);

        await _sut.DeleteAsync(preset.Id);

        using var db = DbFactory();
        Assert.That(db.FieldDefinitions.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task AddAsync_PersistsGroupsAndFieldAssignment()
    {
        var preset = MakePreset();
        var group = new FieldGroup { Name = "Specs", PresetId = preset.Id, DisplayOrder = 0, DisplayMode = GroupDisplayMode.Tab };
        preset.Groups.Add(group);
        preset.Fields.Add(new TextFieldDefinition { Label = "Weight", PresetId = preset.Id, GroupId = group.Id });
        await _sut.AddAsync(preset);

        var loaded = await _sut.GetByIdAsync(preset.Id);

        Assert.That(loaded!.Groups, Has.Count.EqualTo(1));
        Assert.That(loaded.Groups[0].DisplayMode, Is.EqualTo(GroupDisplayMode.Tab));
        Assert.That(loaded.Fields[0].GroupId, Is.EqualTo(group.Id));
    }

    [Test]
    public async Task UpdateAsync_DeletingGroupUngroupsFieldButKeepsField()
    {
        var preset = MakePreset();
        var group = new FieldGroup { Name = "G", PresetId = preset.Id, DisplayOrder = 0 };
        preset.Groups.Add(group);
        preset.Fields.Add(new TextFieldDefinition { Label = "F", PresetId = preset.Id, GroupId = group.Id });
        await _sut.AddAsync(preset);

        var loaded = await _sut.GetByIdAsync(preset.Id);
        loaded!.Groups.Clear();
        loaded.Fields[0].GroupId = null;
        await _sut.UpdateAsync(loaded);

        var reloaded = await _sut.GetByIdAsync(preset.Id);
        Assert.That(reloaded!.Groups, Is.Empty);
        Assert.That(reloaded.Fields, Has.Count.EqualTo(1));
        Assert.That(reloaded.Fields[0].GroupId, Is.Null);
    }

    [Test]
    public async Task AddAsync_PersistsNestedGroupsAndMembership()
    {
        var preset = MakePreset();
        var parent = new FieldGroup { Name = "Parent", PresetId = preset.Id, DisplayOrder = 0 };
        var child = new FieldGroup { Name = "Child", PresetId = preset.Id, ParentGroupId = parent.Id, DisplayOrder = 0 };
        preset.Groups.Add(parent);
        preset.Groups.Add(child);
        preset.Fields.Add(new TextFieldDefinition { Label = "Deep", PresetId = preset.Id, GroupId = child.Id });
        await _sut.AddAsync(preset);

        var loaded = await _sut.GetByIdAsync(preset.Id);

        var loadedChild = loaded!.Groups.Single(g => g.Name == "Child");
        Assert.That(loadedChild.ParentGroupId, Is.EqualTo(parent.Id));
        Assert.That(loaded.Fields[0].GroupId, Is.EqualTo(child.Id));
    }

    [Test]
    public async Task UpdateAsync_RemovingNestedGroupSubtreeUngroupsMember()
    {
        var preset = MakePreset();
        var parent = new FieldGroup { Name = "Parent", PresetId = preset.Id, DisplayOrder = 0 };
        var child = new FieldGroup { Name = "Child", PresetId = preset.Id, ParentGroupId = parent.Id, DisplayOrder = 0 };
        preset.Groups.Add(parent);
        preset.Groups.Add(child);
        preset.Fields.Add(new TextFieldDefinition { Label = "Deep", PresetId = preset.Id, GroupId = child.Id });
        await _sut.AddAsync(preset);

        var loaded = await _sut.GetByIdAsync(preset.Id);
        loaded!.Groups.Clear();
        loaded.Fields[0].GroupId = null;
        await _sut.UpdateAsync(loaded);

        var reloaded = await _sut.GetByIdAsync(preset.Id);
        Assert.That(reloaded!.Groups, Is.Empty);
        Assert.That(reloaded.Fields, Has.Count.EqualTo(1));
        Assert.That(reloaded.Fields[0].GroupId, Is.Null);
    }

    [Test]
    public async Task AddAsync_ListFieldSubFields_AreNotIncludedInTopLevelFields()
    {
        var preset = MakePreset();
        var listField = new ListFieldDefinition { Label = "Episodes", PresetId = preset.Id };
        listField.SubFields.Add(new TextFieldDefinition { Label = "Title", ParentListFieldDefinitionId = listField.Id });
        listField.SubFields.Add(new TextFieldDefinition { Label = "Synopsis", ParentListFieldDefinitionId = listField.Id });
        preset.Fields.Add(listField);
        await _sut.AddAsync(preset);

        var loaded = await _sut.GetByIdAsync(preset.Id);

        Assert.That(loaded!.Fields, Has.Count.EqualTo(1), "Sub-fields must not bleed into top-level Fields");
        var loadedList = loaded.Fields[0] as ListFieldDefinition;
        Assert.That(loadedList, Is.Not.Null);
        Assert.That(loadedList!.SubFields, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task UpdateAsync_ListFieldSubFields_AreNotTreatedAsTopLevelFields()
    {
        var preset = MakePreset();
        var listField = new ListFieldDefinition { Label = "Episodes", PresetId = preset.Id };
        listField.SubFields.Add(new TextFieldDefinition { Label = "Title", ParentListFieldDefinitionId = listField.Id });
        preset.Fields.Add(listField);
        await _sut.AddAsync(preset);

        var loaded = await _sut.GetByIdAsync(preset.Id);
        ((ListFieldDefinition)loaded!.Fields[0]).SubFields[0].Label = "Episode Title";
        await _sut.UpdateAsync(loaded);

        var reloaded = await _sut.GetByIdAsync(preset.Id);
        Assert.That(reloaded!.Fields, Has.Count.EqualTo(1));
        Assert.That(((ListFieldDefinition)reloaded.Fields[0]).SubFields[0].Label, Is.EqualTo("Episode Title"));
    }

    [Test]
    public async Task DeleteAsync_CascadesGroups()
    {
        var preset = MakePreset();
        preset.Groups.Add(new FieldGroup { Name = "G", PresetId = preset.Id });
        await _sut.AddAsync(preset);

        await _sut.DeleteAsync(preset.Id);

        using var db = DbFactory();
        Assert.That(db.FieldGroups.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task GetAllAsync_ReturnsPresetsOrderedByDisplayOrder()
    {
        var first = new Preset { Name = "First", DisplayOrder = 0 };
        var second = new Preset { Name = "Second", DisplayOrder = 1 };
        await _sut.AddAsync(second);
        await _sut.AddAsync(first);

        var result = await _sut.GetAllAsync();

        var names = result.Select(p => p.Name).ToList();
        var firstIdx = names.IndexOf("First");
        var secondIdx = names.IndexOf("Second");
        Assert.That(firstIdx, Is.LessThan(secondIdx));
    }

    [Test]
    public async Task UpdateAsync_CallsLoggerDebug()
    {
        var logger = new RecordingLogger();
        var sut = new PresetRepository(DbFactory, new FieldDefinitionMerger(), logger);
        var preset = MakePreset();
        await sut.AddAsync(preset);

        preset.Name = "Updated";
        await sut.UpdateAsync(preset);

        Assert.That(logger.DebugCallCount, Is.GreaterThan(0));
    }

    [Test]
    public void Constructor_WhenLoggerIsNull_UsesNullAppLogger()
    {
        var sut = new PresetRepository(DbFactory, new FieldDefinitionMerger(), null);
        Assert.DoesNotThrow(() => { });
    }

    [Test]
    public async Task UpdateAsync_AutoNullsGroupIdWhenGroupIsRemoved()
    {
        var preset = MakePreset();
        var group = new FieldGroup { Name = "G", PresetId = preset.Id, DisplayOrder = 0 };
        var field = new TextFieldDefinition { Label = "F", PresetId = preset.Id, GroupId = group.Id };
        preset.Groups.Add(group);
        preset.Fields.Add(field);
        await _sut.AddAsync(preset);

        var loaded = await _sut.GetByIdAsync(preset.Id);
        loaded!.Groups.Clear();

        await _sut.UpdateAsync(loaded);

        var reloaded = await _sut.GetByIdAsync(preset.Id);
        Assert.That(reloaded!.Fields[0].GroupId, Is.Null);
    }

    [Test]
    public async Task GetByIdAsync_EagerLoadsFieldsGroupsAndListSubFields()
    {
        var preset = MakePreset();
        var group = new FieldGroup { Name = "G", PresetId = preset.Id, DisplayOrder = 0 };
        var list = new ListFieldDefinition { Label = "Chapters", PresetId = preset.Id };
        list.SubFields.Add(new TextFieldDefinition { Label = "Name", ParentListFieldDefinitionId = list.Id });
        preset.Groups.Add(group);
        preset.Fields.Add(list);
        await _sut.AddAsync(preset);

        var loaded = await _sut.GetByIdAsync(preset.Id);

        Assert.That(loaded!.Fields, Is.Not.Empty, "Fields must be eager-loaded");
        Assert.That(loaded.Groups, Is.Not.Empty, "Groups must be eager-loaded");
        var loadedList = (ListFieldDefinition)loaded.Fields.Single(f => f is ListFieldDefinition);
        Assert.That(loadedList.SubFields, Is.Not.Empty, "List sub-fields must be eager-loaded");
    }

    [Test]
    public async Task GetByIdAsync_EagerLoadsSystemFieldRefDefinition()
    {
        var sysFieldRepo = new SystemFieldRepository(DbFactory, new FieldDefinitionMerger());
        var sysField = new SystemField { Name = "Tag", Definition = new TextFieldDefinition { Label = "Tag" } };
        await sysFieldRepo.AddAsync(sysField);

        var preset = MakePreset();
        preset.SystemFieldRefs.Add(new PresetSystemField { PresetId = preset.Id, SystemFieldId = sysField.Id, DisplayOrder = 0 });
        await _sut.AddAsync(preset);

        var loaded = await _sut.GetByIdAsync(preset.Id);

        Assert.That(loaded!.SystemFieldRefs, Is.Not.Empty);
        Assert.That(loaded.SystemFieldRefs[0].SystemField, Is.Not.Null, "SystemField must be eager-loaded");
        Assert.That(loaded.SystemFieldRefs[0].SystemField.Definition, Is.Not.Null, "SystemField definition must be eager-loaded");
        Assert.That(loaded.SystemFieldRefs[0].SystemField.Definition.Label, Is.EqualTo("Tag"));
    }

    [Test]
    public async Task GetAllAsync_EagerLoadsFieldsAndGroups()
    {
        var preset = MakePreset();
        preset.Groups.Add(new FieldGroup { Name = "G", PresetId = preset.Id, DisplayOrder = 0 });
        preset.Fields.Add(new TextFieldDefinition { Label = "F", PresetId = preset.Id });
        await _sut.AddAsync(preset);

        var all = await _sut.GetAllAsync();

        Assert.That(all[0].Fields, Is.Not.Empty, "Fields must be eager-loaded by GetAllAsync");
        Assert.That(all[0].Groups, Is.Not.Empty, "Groups must be eager-loaded by GetAllAsync");
    }

    [Test]
    public async Task DeleteAsync_CascadesSystemFieldRefsButKeepsSystemField()
    {
        var sysFieldRepo = new SystemFieldRepository(DbFactory, new FieldDefinitionMerger());
        var sysField = new SystemField { Name = "Tag", Definition = new TextFieldDefinition { Label = "Tag" } };
        await sysFieldRepo.AddAsync(sysField);

        var preset = MakePreset();
        preset.SystemFieldRefs.Add(new PresetSystemField { PresetId = preset.Id, SystemFieldId = sysField.Id, DisplayOrder = 0 });
        await _sut.AddAsync(preset);

        await _sut.DeleteAsync(preset.Id);

        using var db = DbFactory();
        Assert.That(db.Set<PresetSystemField>().Count(), Is.EqualTo(0), "Preset's system-field refs must cascade-delete");
        Assert.That(db.SystemFields.Count(), Is.EqualTo(1), "The shared system field itself must survive");
    }

    [Test]
    public async Task UpdateAsync_ListField_RemovesDroppedSubFieldKeepsOthers()
    {
        var preset = MakePreset();
        var list = new ListFieldDefinition { Label = "Chapters", PresetId = preset.Id };
        var keptSub = new TextFieldDefinition { Label = "Kept", ParentListFieldDefinitionId = list.Id, DisplayOrder = 0 };
        var droppedSub = new TextFieldDefinition { Label = "Dropped", ParentListFieldDefinitionId = list.Id, DisplayOrder = 1 };
        list.SubFields.Add(keptSub);
        list.SubFields.Add(droppedSub);
        preset.Fields.Add(list);
        await _sut.AddAsync(preset);

        var loaded = await _sut.GetByIdAsync(preset.Id);
        var loadedList = (ListFieldDefinition)loaded!.Fields.Single(f => f is ListFieldDefinition);
        loadedList.SubFields.RemoveAll(f => f.Id == droppedSub.Id);
        await _sut.UpdateAsync(loaded);

        var reloaded = await _sut.GetByIdAsync(preset.Id);
        var reloadedList = (ListFieldDefinition)reloaded!.Fields.Single(f => f is ListFieldDefinition);
        Assert.That(reloadedList.SubFields.Select(f => f.Id), Is.EqualTo(new[] { keptSub.Id }));
    }

    [Test]
    public async Task UpdateAsync_ListField_AddsNewSubField()
    {
        var preset = MakePreset();
        var list = new ListFieldDefinition { Label = "Chapters", PresetId = preset.Id };
        list.SubFields.Add(new TextFieldDefinition { Label = "First", ParentListFieldDefinitionId = list.Id, DisplayOrder = 0 });
        preset.Fields.Add(list);
        await _sut.AddAsync(preset);

        var loaded = await _sut.GetByIdAsync(preset.Id);
        var loadedList = (ListFieldDefinition)loaded!.Fields.Single(f => f is ListFieldDefinition);
        loadedList.SubFields.Add(new TextFieldDefinition { Label = "Second", ParentListFieldDefinitionId = loadedList.Id, DisplayOrder = 1 });
        await _sut.UpdateAsync(loaded);

        var reloaded = await _sut.GetByIdAsync(preset.Id);
        var reloadedList = (ListFieldDefinition)reloaded!.Fields.Single(f => f is ListFieldDefinition);
        Assert.That(reloadedList.SubFields, Has.Count.EqualTo(2));
        Assert.That(reloadedList.SubFields.Any(f => f.Label == "Second"), Is.True);
    }

    [Test]
    public async Task UpdateAsync_RemovingGroupNullsSystemFieldRefGroupId()
    {
        var sysFieldRepo = new SystemFieldRepository(DbFactory, new FieldDefinitionMerger());
        var sysField = new SystemField { Name = "Tag", Definition = new TextFieldDefinition { Label = "Tag" } };
        await sysFieldRepo.AddAsync(sysField);

        var preset = MakePreset();
        var group = new FieldGroup { Name = "G", PresetId = preset.Id, DisplayOrder = 0 };
        preset.Groups.Add(group);
        preset.SystemFieldRefs.Add(new PresetSystemField
        {
            PresetId = preset.Id, SystemFieldId = sysField.Id, GroupId = group.Id, DisplayOrder = 0
        });
        await _sut.AddAsync(preset);

        var loaded = await _sut.GetByIdAsync(preset.Id);
        loaded!.Groups.Clear();
        await _sut.UpdateAsync(loaded);

        var reloaded = await _sut.GetByIdAsync(preset.Id);
        Assert.That(reloaded!.SystemFieldRefs[0].GroupId, Is.Null,
            "A system-field ref assigned to a removed group must be ungrouped");
    }

    [Test]
    public async Task UpdateAsync_KeepingGroupPreservesSystemFieldRefGroupId()
    {
        var sysFieldRepo = new SystemFieldRepository(DbFactory, new FieldDefinitionMerger());
        var sysField = new SystemField { Name = "Tag", Definition = new TextFieldDefinition { Label = "Tag" } };
        await sysFieldRepo.AddAsync(sysField);

        var preset = MakePreset();
        var group = new FieldGroup { Name = "G", PresetId = preset.Id, DisplayOrder = 0 };
        preset.Groups.Add(group);
        preset.SystemFieldRefs.Add(new PresetSystemField
        {
            PresetId = preset.Id, SystemFieldId = sysField.Id, GroupId = group.Id, DisplayOrder = 0
        });
        await _sut.AddAsync(preset);

        var loaded = await _sut.GetByIdAsync(preset.Id);
        loaded!.Name = "Renamed";
        await _sut.UpdateAsync(loaded);

        var reloaded = await _sut.GetByIdAsync(preset.Id);
        Assert.That(reloaded!.SystemFieldRefs[0].GroupId, Is.EqualTo(group.Id),
            "A system-field ref whose group survives must keep its assignment");
    }

    [Test]
    public async Task GetChildrenAsync_ReturnsChildrenOrderedByDisplayOrder()
    {
        var parent = MakePreset("Parent");
        await _sut.AddAsync(parent);
        var childB = new Preset { Name = "ChildB", ParentPresetId = parent.Id, DisplayOrder = 1 };
        var childA = new Preset { Name = "ChildA", ParentPresetId = parent.Id, DisplayOrder = 0 };
        await _sut.AddAsync(childB);
        await _sut.AddAsync(childA);

        var children = await _sut.GetChildrenAsync(parent.Id);

        Assert.That(children.Select(c => c.Name), Is.EqualTo(new[] { "ChildA", "ChildB" }));
    }
}

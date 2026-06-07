using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;

namespace Collectary.Core.Tests.UseCases;

[TestFixture]
public class PresetUseCaseTest
{
    private IPresetRepository _presets = null!;
    private IItemRepository _items = null!;
    private ICollectionAuthorization _auth = null!;
    private PresetUseCase _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _presets = A.Fake<IPresetRepository>();
        _items = A.Fake<IItemRepository>();
        _auth = A.Fake<ICollectionAuthorization>();
        A.CallTo(() => _auth.CanWriteAsync(A<Guid>._)).Returns(true);
        A.CallTo(() => _auth.CanReadAsync(A<Guid>._)).Returns(true);
        A.CallTo(() => _auth.IsOwnerAsync(A<Guid>._)).Returns(true);
        _sut = new PresetUseCase(_presets, _items, _auth);
    }

    [Test]
    public async Task GetAllPresetsAsync_ReturnsRepositoryResult()
    {
        var presets = new List<Preset> { new() { Name = "A" }, new() { Name = "B" } };
        A.CallTo(() => _presets.GetAllAsync()).Returns(presets);

        var result = await _sut.GetAllPresetsAsync();

        Assert.That(result, Is.EqualTo(presets));
    }

    [Test]
    public async Task GetWritablePresetsAsync_ExcludesReadOnlyShares()
    {
        var writable = new Preset { Name = "Mine" };
        var readOnly = new Preset { Name = "Shared (read-only)" };
        A.CallTo(() => _presets.GetAllAsync()).Returns(new List<Preset> { writable, readOnly });
        A.CallTo(() => _auth.CanWriteAsync(writable.Id)).Returns(true);
        A.CallTo(() => _auth.CanWriteAsync(readOnly.Id)).Returns(false);

        var result = await _sut.GetWritablePresetsAsync();

        Assert.That(result.Select(p => p.Name), Is.EqualTo(new[] { "Mine" }));
    }

    [Test]
    public async Task GetPresetAsync_ReturnsRepositoryResult()
    {
        var id = Guid.NewGuid();
        var preset = new Preset { Name = "Test" };
        A.CallTo(() => _presets.GetByIdAsync(id)).Returns(preset);

        var result = await _sut.GetPresetAsync(id);

        Assert.That(result, Is.EqualTo(preset));
    }

    [Test]
    public async Task GetChildPresetsAsync_ReturnsRepositoryResult()
    {
        var parentId = Guid.NewGuid();
        var children = new List<Preset> { new() { Name = "Child" } };
        A.CallTo(() => _presets.GetChildrenAsync(parentId)).Returns(children);

        var result = await _sut.GetChildPresetsAsync(parentId);

        Assert.That(result, Is.EqualTo(children));
    }

    [Test]
    public async Task CreatePresetAsync_DelegatesToRepository()
    {
        var preset = new Preset { Name = "New" };

        await _sut.CreatePresetAsync(preset);

        A.CallTo(() => _presets.AddAsync(preset)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task UpdatePresetAsync_DelegatesToRepository()
    {
        var preset = new Preset { Name = "Updated" };

        await _sut.UpdatePresetAsync(preset);

        A.CallTo(() => _presets.UpdateAsync(preset)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task UpdatePresetOrderAsync_DelegatesToRepository()
    {
        var ordered = new List<Preset> { new() { Name = "A" }, new() { Name = "B" } };

        await _sut.UpdatePresetOrderAsync(ordered);

        A.CallTo(() => _presets.UpdateDisplayOrdersAsync(ordered)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task DeletePresetAsync_DeletesItemsBeforePreset()
    {
        var id = Guid.NewGuid();
        var callOrder = new List<string>();
        A.CallTo(() => _items.DeleteByPresetAsync(id))
            .Invokes(() => callOrder.Add("items"));
        A.CallTo(() => _presets.DeleteAsync(id))
            .Invokes(() => callOrder.Add("preset"));

        await _sut.DeletePresetAsync(id);

        Assert.That(callOrder, Is.EqualTo(new[] { "items", "preset" }));
    }

    [Test]
    public async Task GetEffectiveFieldsAsync_ReturnsOwnFieldsMergedAndOrdered()
    {
        var presetId = Guid.NewGuid();
        var fieldA = new TextFieldDefinition { Label = "A", DisplayOrder = 2 };
        var fieldB = new TextFieldDefinition { Label = "B", DisplayOrder = 1 };
        var displayName = new DisplayNameFieldDefinition { Label = "Name", DisplayOrder = 0 };

        var preset = new Preset
        {
            Name = "Test",
            Fields = [fieldA, fieldB, displayName],
            SharedFieldRefs = []
        };
        A.CallTo(() => _presets.GetByIdAsync(presetId)).Returns(preset);

        var result = await _sut.GetEffectiveFieldsAsync(presetId);

        Assert.That(result.Fields.Select(f => f.Label), Is.EqualTo(new[] { "Name", "B", "A" }));
    }

    [Test]
    public async Task GetEffectiveFieldsAsync_ReturnsEmptyWhenPresetNotFound()
    {
        var id = Guid.NewGuid();
        A.CallTo(() => _presets.GetByIdAsync(id)).Returns((Preset?)null);

        var result = await _sut.GetEffectiveFieldsAsync(id);

        Assert.That(result.Fields, Is.Empty);
    }

    [Test]
    public async Task GetEffectiveFieldsAsync_InheritsParentFieldsExcludingDisplayName()
    {
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var parentDisplayName = new DisplayNameFieldDefinition { Label = "Name", DisplayOrder = 0 };
        var parentField = new TextFieldDefinition { Label = "ParentField", DisplayOrder = 1 };
        var childField = new TextFieldDefinition { Label = "ChildField", DisplayOrder = 0 };

        var parentPreset = new Preset
        {
            Name = "Parent",
            Fields = [parentDisplayName, parentField],
            SharedFieldRefs = []
        };
        var childPreset = new Preset
        {
            Name = "Child",
            ParentPresetId = parentId,
            Fields = [childField],
            SharedFieldRefs = []
        };

        A.CallTo(() => _presets.GetByIdAsync(childId)).Returns(childPreset);
        A.CallTo(() => _presets.GetByIdAsync(parentId)).Returns(parentPreset);

        var result = await _sut.GetEffectiveFieldsAsync(childId);

        Assert.That(result.Fields.Select(f => f.Label), Is.EqualTo(new[] { "ParentField", "ChildField" }));
    }

    [Test]
    public async Task GetEffectiveFieldsAsync_ReturnsGroupsOrderedByDisplayOrder()
    {
        var presetId = Guid.NewGuid();
        var groupB = new FieldGroup { Name = "B", DisplayOrder = 1 };
        var groupA = new FieldGroup { Name = "A", DisplayOrder = 0 };
        var preset = new Preset { Name = "P", Groups = [groupB, groupA] };
        A.CallTo(() => _presets.GetByIdAsync(presetId)).Returns(preset);

        var result = await _sut.GetEffectiveFieldsAsync(presetId);

        Assert.That(result.Groups.Select(g => g.Name), Is.EqualTo(new[] { "A", "B" }));
    }

    [Test]
    public async Task GetEffectiveFieldsAsync_MapsOwnFieldGroupId()
    {
        var presetId = Guid.NewGuid();
        var group = new FieldGroup { Name = "G", DisplayOrder = 0 };
        var field = new TextFieldDefinition { Label = "F", GroupId = group.Id };
        var preset = new Preset { Name = "P", Groups = [group], Fields = [field] };
        A.CallTo(() => _presets.GetByIdAsync(presetId)).Returns(preset);

        var result = await _sut.GetEffectiveFieldsAsync(presetId);

        Assert.That(result.GroupByFieldId[field.Id], Is.EqualTo(group.Id));
    }

    [Test]
    public async Task GetEffectiveFieldsAsync_MapsSharedFieldGroupIdFromReference()
    {
        var presetId = Guid.NewGuid();
        var group = new FieldGroup { Name = "G", DisplayOrder = 0 };
        var def = new TextFieldDefinition { Label = "Sys" };
        var sharedField = new SharedField { Name = "Sys", Definition = def };
        var preset = new Preset
        {
            Name = "P",
            Groups = [group],
            SharedFieldRefs = [new PresetSharedField { SharedFieldId = sharedField.Id, GroupId = group.Id, SharedField = sharedField }]
        };
        A.CallTo(() => _presets.GetByIdAsync(presetId)).Returns(preset);

        var result = await _sut.GetEffectiveFieldsAsync(presetId);

        Assert.That(result.GroupByFieldId[def.Id], Is.EqualTo(group.Id));
    }

    [Test]
    public async Task GetEffectiveFieldsAsync_PreservesNestedGroupParentId()
    {
        var presetId = Guid.NewGuid();
        var parentGroup = new FieldGroup { Name = "Parent", DisplayOrder = 0 };
        var childGroup = new FieldGroup { Name = "Child", DisplayOrder = 1, ParentGroupId = parentGroup.Id };
        var preset = new Preset { Name = "P", Groups = [parentGroup, childGroup] };
        A.CallTo(() => _presets.GetByIdAsync(presetId)).Returns(preset);

        var result = await _sut.GetEffectiveFieldsAsync(presetId);

        var resolvedChild = result.Groups.Single(g => g.Name == "Child");
        Assert.That(resolvedChild.ParentGroupId, Is.EqualTo(parentGroup.Id));
    }

    [Test]
    public async Task GetEffectiveFieldsAsync_ResolvesGroupsThroughParentChain()
    {
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var parentGroup = new FieldGroup { Name = "ParentGroup", DisplayOrder = 0 };
        var childGroup = new FieldGroup { Name = "ChildGroup", DisplayOrder = 1 };
        var parent = new Preset { Name = "Parent", Groups = [parentGroup] };
        var child = new Preset { Name = "Child", ParentPresetId = parentId, Groups = [childGroup] };
        A.CallTo(() => _presets.GetByIdAsync(parentId)).Returns(parent);
        A.CallTo(() => _presets.GetByIdAsync(childId)).Returns(child);

        var result = await _sut.GetEffectiveFieldsAsync(childId);

        Assert.That(result.Groups.Select(g => g.Name), Is.EqualTo(new[] { "ParentGroup", "ChildGroup" }));
    }

    [Test]
    public async Task GetEffectiveFieldsAsync_CallsLoggerDebug()
    {
        var logger = A.Fake<IAppLogger>();
        var sut = new PresetUseCase(_presets, _items, _auth, logger);
        var presetId = Guid.NewGuid();
        A.CallTo(() => _presets.GetByIdAsync(presetId)).Returns(new Preset { Name = "P" });

        await sut.GetEffectiveFieldsAsync(presetId);

        A.CallTo(() => logger.Debug(A<string>._, A<object?[]>._)).MustHaveHappened();
    }

    [Test]
    public void Constructor_WhenLoggerIsNull_UsesNullAppLogger()
    {
        var sut = new PresetUseCase(_presets, _items, _auth, null);

        Assert.DoesNotThrowAsync(async () =>
        {
            var presetId = Guid.NewGuid();
            A.CallTo(() => _presets.GetByIdAsync(presetId)).Returns(new Preset { Name = "P" });
            await sut.GetEffectiveFieldsAsync(presetId);
        });
    }

    [Test]
    public void UpdatePresetAsync_WhenNotAuthorized_Throws()
    {
        var preset = new Preset { Name = "P" };
        var auth = A.Fake<ICollectionAuthorization>();
        A.CallTo(() => auth.CanWriteAsync(preset.Id)).Returns(false);
        var sut = new PresetUseCase(_presets, _items, auth);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.UpdatePresetAsync(preset));
    }

    [Test]
    public async Task UpdatePresetAsync_WhenAuthorized_Updates()
    {
        var preset = new Preset { Name = "P" };
        var auth = A.Fake<ICollectionAuthorization>();
        A.CallTo(() => auth.CanWriteAsync(preset.Id)).Returns(true);
        var sut = new PresetUseCase(_presets, _items, auth);

        await sut.UpdatePresetAsync(preset);

        A.CallTo(() => _presets.UpdateAsync(preset)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public void DeletePresetAsync_WhenNotOwner_Throws()
    {
        var id = Guid.NewGuid();
        var auth = A.Fake<ICollectionAuthorization>();
        A.CallTo(() => auth.IsOwnerAsync(id)).Returns(false);
        var sut = new PresetUseCase(_presets, _items, auth);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.DeletePresetAsync(id));
    }

    [Test]
    public async Task DeletePresetAsync_WhenOwner_DeletesItemsThenPreset()
    {
        var id = Guid.NewGuid();
        var auth = A.Fake<ICollectionAuthorization>();
        A.CallTo(() => auth.IsOwnerAsync(id)).Returns(true);
        var sut = new PresetUseCase(_presets, _items, auth);

        await sut.DeletePresetAsync(id);

        A.CallTo(() => _items.DeleteByPresetAsync(id)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _presets.DeleteAsync(id)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task GetEffectiveFieldsAsync_GroupByFieldIdCountsGroupedFields()
    {
        var presetId = Guid.NewGuid();
        var group = new FieldGroup { Name = "G", DisplayOrder = 0 };
        var groupedField = new TextFieldDefinition { Label = "Grouped", GroupId = group.Id };
        var ungroupedField = new TextFieldDefinition { Label = "Ungrouped", GroupId = null };
        var preset = new Preset { Name = "P", Groups = [group], Fields = [groupedField, ungroupedField] };
        A.CallTo(() => _presets.GetByIdAsync(presetId)).Returns(preset);

        var result = await _sut.GetEffectiveFieldsAsync(presetId);

        Assert.That(result.GroupByFieldId.Count(kv => kv.Value is not null), Is.EqualTo(1));
    }
}

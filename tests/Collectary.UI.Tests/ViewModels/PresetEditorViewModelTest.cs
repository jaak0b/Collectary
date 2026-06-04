using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;
using Collectary.Presentation.ViewModels.SystemFields;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class PresetEditorViewModelTest
{
    private IPresetUseCase _presetUseCase = null!;
    private ISystemFieldUseCase _systemFieldUseCase = null!;
    private IDialogService _dialogService = null!;

    [SetUp]
    public void SetUp()
    {
        _presetUseCase = A.Fake<IPresetUseCase>();
        _systemFieldUseCase = A.Fake<ISystemFieldUseCase>();
        _dialogService = A.Fake<IDialogService>();

        A.CallTo(() => _presetUseCase.GetAllPresetsAsync()).Returns(new List<Preset>());
        A.CallTo(() => _systemFieldUseCase.GetAllAsync()).Returns(new List<SystemField>());
    }

    private PresetEditorViewModel CreateSut(Preset? existing = null, Action? onSaved = null, Action? onCancelled = null) =>
        new(_presetUseCase, _systemFieldUseCase, _dialogService,
            onSaved: onSaved ?? (() => { }),
            onCancelled: onCancelled ?? (() => { }),
            existing: existing);

    [Test]
    public async Task SaveAndGoBackAsync_CallsCreateForNewPreset()
    {
        var sut = CreateSut();
        sut.Name = "My Collection";

        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        A.CallTo(() => _presetUseCase.CreatePresetAsync(
            A<Preset>.That.Matches(p => p.Name == "My Collection")))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task SaveAndGoBackAsync_CallsUpdateForExistingPreset()
    {
        var existing = new Preset { Name = "Old Name" };
        var sut = CreateSut(existing: existing);
        sut.Name = "New Name";

        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        A.CallTo(() => _presetUseCase.UpdatePresetAsync(
            A<Preset>.That.Matches(p => p.Name == "New Name")))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task SaveAndGoBackAsync_InvokesOnSavedCallbackAfterSuccess()
    {
        var onSavedInvoked = false;
        var sut = CreateSut(onSaved: () => { onSavedInvoked = true; });
        sut.Name = "Test";

        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        Assert.That(onSavedInvoked, Is.True);
    }

    [Test]
    public void Cancel_InvokesOnCancelledCallback()
    {
        var onCancelledInvoked = false;
        var sut = CreateSut(onCancelled: () => { onCancelledInvoked = true; });

        sut.CancelCommand.Execute(null);

        Assert.That(onCancelledInvoked, Is.True);
    }

    [Test]
    public async Task LoadAsync_PopulatesAvailableParentsExcludingSelf()
    {
        var self = new Preset { Name = "Self" };
        var other = new Preset { Name = "Other" };
        A.CallTo(() => _presetUseCase.GetAllPresetsAsync()).Returns(new List<Preset> { self, other });

        var sut = CreateSut(existing: self);
        await sut.LoadAsync();

        Assert.That(sut.AvailableParents.Count, Is.EqualTo(1));
        Assert.That(sut.AvailableParents[0].Name, Is.EqualTo("Other"));
    }

    [Test]
    public async Task LoadAsync_AllPresetsEligibleAsParentsWhenCreatingNew()
    {
        var presetA = new Preset { Name = "A" };
        var presetB = new Preset { Name = "B" };
        A.CallTo(() => _presetUseCase.GetAllPresetsAsync()).Returns(new List<Preset> { presetA, presetB });

        var sut = CreateSut(existing: null);
        await sut.LoadAsync();

        Assert.That(sut.AvailableParents.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task LoadAsync_PreSelectsExistingParent()
    {
        var parent = new Preset { Name = "Parent" };
        var child = new Preset { Name = "Child", ParentPresetId = parent.Id };
        A.CallTo(() => _presetUseCase.GetAllPresetsAsync()).Returns(new List<Preset> { parent });

        var sut = CreateSut(existing: child);
        await sut.LoadAsync();

        Assert.That(sut.SelectedParent, Is.EqualTo(parent));
    }

    [Test]
    public async Task SaveAndGoBackAsync_DoesNotInvokeOnSavedWhenPersistFails()
    {
        A.CallTo(() => _presetUseCase.CreatePresetAsync(A<Preset>._))
            .Throws<InvalidOperationException>();

        var onSavedInvoked = false;
        var sut = CreateSut(onSaved: () => { onSavedInvoked = true; });
        sut.Name = "Test";

        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        Assert.That(onSavedInvoked, Is.False);
    }

    [Test]
    public async Task SaveAndGoBackAsync_WhenPersistFails_ShowsDialog()
    {
        A.CallTo(() => _presetUseCase.CreatePresetAsync(A<Preset>._))
            .Throws<InvalidOperationException>();

        var sut = CreateSut();
        sut.Name = "Test";
        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        A.CallTo(() => _dialogService.ShowMessageAsync(A<string>._, A<string>._))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task LoadAsync_WhenPresetsThrows_ShowsDialog()
    {
        A.CallTo(() => _presetUseCase.GetAllPresetsAsync()).Throws<InvalidOperationException>();

        var sut = CreateSut();
        await sut.LoadAsync();

        A.CallTo(() => _dialogService.ShowMessageAsync(A<string>._, A<string>._))
            .MustHaveHappened();
    }

    private static (SystemField sf, SystemFieldRowViewModel row) MakeSystemFieldRow(string label)
    {
        var sf = new SystemField { Name = label, Definition = new TextFieldDefinition { Label = label } };
        sf.Definition.SystemFieldId = sf.Id;
        return (sf, new SystemFieldRowViewModel(sf));
    }

    [Test]
    public void AddSystemFieldCommand_AddsRowToCurrentRows()
    {
        var (_, sfRow) = MakeSystemFieldRow("Tag");
        var sut = CreateSut();
        var before = sut.CurrentRows.Count;

        sut.AddSystemFieldCommand.Execute(sfRow);

        Assert.That(sut.CurrentRows.Count, Is.EqualTo(before + 1));
    }

    [Test]
    public void AddSystemFieldCommand_WhenAlreadyPresent_DoesNotAddDuplicate()
    {
        var (_, sfRow) = MakeSystemFieldRow("Tag");
        var sut = CreateSut();
        sut.AddSystemFieldCommand.Execute(sfRow);
        var countAfterFirst = sut.CurrentRows.Count;

        sut.AddSystemFieldCommand.Execute(sfRow);

        Assert.That(sut.CurrentRows.Count, Is.EqualTo(countAfterFirst));
    }

    [Test]
    public async Task PersistAsync_SetsParentPresetIdFromSelectedParent()
    {
        var parent = new Preset { Name = "Parent" };
        A.CallTo(() => _presetUseCase.GetAllPresetsAsync()).Returns(new List<Preset> { parent });
        var sut = CreateSut();
        await sut.LoadAsync();
        sut.Name = "Child";
        sut.SelectedParent = parent;

        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        A.CallTo(() => _presetUseCase.CreatePresetAsync(
            A<Preset>.That.Matches(p => p.ParentPresetId == parent.Id)))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public void AddGroupCommand_AddsGroupNodeToCurrentRows()
    {
        var sut = CreateSut();
        var before = sut.CurrentRows.OfType<FieldGroupRowViewModel>().Count();

        sut.AddGroupCommand.Execute(null);

        Assert.That(sut.CurrentRows.OfType<FieldGroupRowViewModel>().Count(), Is.EqualTo(before + 1));
    }

    [Test]
    public async Task PersistAsync_PersistsGroups()
    {
        var sut = CreateSut();
        sut.Name = "P";
        sut.AddGroupCommand.Execute(null);

        Preset? captured = null;
        A.CallTo(() => _presetUseCase.CreatePresetAsync(A<Preset>._))
            .Invokes(call => captured = call.GetArgument<Preset>(0));

        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        Assert.That(captured!.Groups, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task DrillIntoGroup_AddedFieldBecomesMember()
    {
        var sut = CreateSut();
        sut.AddGroupCommand.Execute(null);
        var group = sut.CurrentRows.OfType<FieldGroupRowViewModel>().First();

        sut.DrillIntoCommand.Execute(group);
        await sut.AddTextFieldCommand.ExecuteAsync(null);
        var field = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().First();

        Assert.That(field.AssignedGroupId, Is.EqualTo(group.Id));
    }

    [Test]
    public async Task AddingSecondFieldToGroup_DoesNotChurnFirstFieldAvailableGroups()
    {
        var sut = CreateSut();
        sut.AddGroupCommand.Execute(null);
        var group = sut.CurrentRows.OfType<FieldGroupRowViewModel>().First();
        sut.DrillIntoCommand.Execute(group);
        await sut.AddTextFieldCommand.ExecuteAsync(null);
        var field1 = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().First();

        var churned = false;
        field1.AvailableGroups.CollectionChanged += (_, _) => churned = true;

        await sut.AddTextFieldCommand.ExecuteAsync(null);

        Assert.That(churned, Is.False, "Re-populating an unchanged group set must not clear/refill AvailableGroups, which would make the bound ComboBox push back a spurious null selection and eject the field from its group.");
        Assert.That(field1.AssignedGroupId, Is.EqualTo(group.Id));
    }

    [Test]
    public void DrillIntoGroup_PushesBreadcrumbLevel()
    {
        var sut = CreateSut();
        sut.AddGroupCommand.Execute(null);
        var group = sut.CurrentRows.OfType<FieldGroupRowViewModel>().First();
        group.Name = "Specs";

        sut.DrillIntoCommand.Execute(group);

        Assert.That(sut.Levels.Count, Is.EqualTo(2));
        Assert.That(sut.Levels[0].IsCurrent, Is.False);
        Assert.That(sut.Levels[1].IsCurrent, Is.True);
        Assert.That(sut.Levels[1].Title, Is.EqualTo("Specs"));
    }

    [Test]
    public async Task RemoveGroupNode_RehomesMembersAsUngrouped()
    {
        var sut = CreateSut();
        sut.AddGroupCommand.Execute(null);
        var group = sut.CurrentRows.OfType<FieldGroupRowViewModel>().First();
        sut.DrillIntoCommand.Execute(group);
        await sut.AddTextFieldCommand.ExecuteAsync(null);
        var field = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().First();

        sut.NavigateToLevelCommand.Execute(sut.Levels[0]);
        await sut.RemoveFieldCommand.ExecuteAsync(group);

        Assert.That(field.AssignedGroupId, Is.Null);
        Assert.That(sut.CurrentRows, Does.Contain(field));
    }

    [Test]
    public void Constructor_NewPreset_AutoInsertsDisplayNameField()
    {
        var sut = CreateSut();

        var displayNameRows = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Count(r => r.IsDisplayName);
        Assert.That(displayNameRows, Is.EqualTo(1));
    }

    [Test]
    public void Constructor_ExistingPresetWithDisplayName_DoesNotDuplicateIt()
    {
        var existing = new Preset
        {
            Name = "P",
            Fields = [new DisplayNameFieldDefinition { IsRequired = true }]
        };

        var sut = CreateSut(existing: existing);

        var displayNameRows = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Count(r => r.IsDisplayName);
        Assert.That(displayNameRows, Is.EqualTo(1));
    }

    [Test]
    public async Task PersistAsync_PersistsDisplayNameAsOwnField()
    {
        var sut = CreateSut();
        sut.Name = "P";

        Preset? captured = null;
        A.CallTo(() => _presetUseCase.CreatePresetAsync(A<Preset>._)).Invokes(c => captured = c.GetArgument<Preset>(0));

        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        Assert.That(captured!.Fields.Count(f => f is DisplayNameFieldDefinition), Is.EqualTo(1));
    }

    [Test]
    public void Constructor_WithExistingPresetHavingGroups_LoadsGroupNodes()
    {
        var group = new FieldGroup { Name = "Specs" };
        var existing = new Preset { Name = "P", Groups = [group] };

        var sut = CreateSut(existing: existing);

        var groupRow = sut.CurrentRows.OfType<FieldGroupRowViewModel>().FirstOrDefault();
        Assert.That(groupRow, Is.Not.Null);
        Assert.That(groupRow!.Name, Is.EqualTo("Specs"));
    }

    [Test]
    public void Constructor_WithExistingPresetHavingSystemFieldRefs_LoadsSystemRows()
    {
        var def = new TextFieldDefinition { Label = "Tag" };
        var sf = new SystemField { Name = "Tag", Definition = def };
        def.SystemFieldId = sf.Id;
        var sfRef = new PresetSystemField { SystemFieldId = sf.Id, SystemField = sf, DisplayOrder = 1 };
        var existing = new Preset { Name = "P", SystemFieldRefs = [sfRef] };

        var sut = CreateSut(existing: existing);

        var sysRow = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>()
            .FirstOrDefault(r => r.IsSystemField);
        Assert.That(sysRow, Is.Not.Null);
        Assert.That(sysRow!.DisplayOrder, Is.EqualTo(1));
    }

    [Test]
    public async Task LoadAsync_WhenSystemFieldsThrows_ShowsDialog()
    {
        A.CallTo(() => _systemFieldUseCase.GetAllAsync()).Throws<InvalidOperationException>();

        var sut = CreateSut();
        await sut.LoadAsync();

        A.CallTo(() => _dialogService.ShowMessageAsync(A<string>._, A<string>._))
            .MustHaveHappened();
    }

    [Test]
    public async Task SaveAsync_CallsPersistWithoutNavigating()
    {
        var onSavedCalled = false;
        var sut = CreateSut(onSaved: () => onSavedCalled = true);
        sut.Name = "Test";

        await sut.SaveCommand.ExecuteAsync(null);

        A.CallTo(() => _presetUseCase.CreatePresetAsync(A<Preset>._)).MustHaveHappenedOnceExactly();
        Assert.That(onSavedCalled, Is.False);
    }

    [Test]
    public async Task PersistAsync_SeparatesOwnFieldsFromSystemFieldRefs()
    {
        var (sf, sfRow) = MakeSystemFieldRow("Tag");
        var sut = CreateSut();
        sut.Name = "Test";
        sut.AddSystemFieldCommand.Execute(sfRow);

        Preset? captured = null;
        A.CallTo(() => _presetUseCase.CreatePresetAsync(A<Preset>._))
            .Invokes(call => captured = call.GetArgument<Preset>(0));

        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.SystemFieldRefs, Has.Count.EqualTo(1));
        Assert.That(captured.SystemFieldRefs[0].SystemFieldId, Is.EqualTo(sf.Id));
    }

    [Test]
    public void Constructor_GroupedField_HasCorrectColumnSpanOptionsAfterBuild()
    {
        var group = new FieldGroup { Name = "Specs", ColumnCount = 3 };
        var field = new TextFieldDefinition { Label = "Notes", GroupId = group.Id };
        var existing = new Preset
        {
            Name = "P",
            Groups = [group],
            Fields = [field]
        };

        var sut = CreateSut(existing: existing);

        var groupRow = sut.CurrentRows.OfType<FieldGroupRowViewModel>()
            .First(g => g.Name == "Specs");
        var fieldRow = groupRow.ChildNodes.OfType<FieldDefinitionRowViewModel>()
            .First(f => f.Label == "Notes");

        Assert.That(fieldRow.ColumnSpanOptions, Is.EqualTo(new[] { 1, 2, 3 }),
            "Grouped fields must see all span options for their group's column count after Build()");
    }

    [Test]
    public void AddTextField_WhenPresetIsMultiColumn_NewFieldGetsColumnSpanOptions()
    {
        var sut = CreateSut();
        sut.ColumnCount = 3;

        sut.AddTextFieldCommand.Execute(null);

        var added = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>()
            .Last(f => !f.IsDisplayName);
        Assert.That(added.IsInMultiColumnContext, Is.True,
            "A newly added field in a multi-column preset must offer span options");
        Assert.That(added.ColumnSpanOptions, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void AddListField_WhenPresetIsMultiColumn_ListGetsColumnSpanOptions()
    {
        var sut = CreateSut();
        sut.ColumnCount = 3;

        sut.AddListFieldCommand.Execute(null);

        var added = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>()
            .Last(f => f.IsList);
        Assert.That(added.IsInMultiColumnContext, Is.True,
            "A list field in a multi-column preset must offer its own column-span (width) options");
        Assert.That(added.ColumnSpanOptions, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public async Task Persist_GroupColumnCount_DoesNotOverwritePresetColumnCount()
    {
        var sut = CreateSut();
        sut.Name = "P";
        sut.ColumnCount = 5;
        sut.AddGroupCommand.Execute(null);
        var group = sut.CurrentRows.OfType<FieldGroupRowViewModel>().First();
        group.ColumnCount = 2;

        Preset? captured = null;
        A.CallTo(() => _presetUseCase.CreatePresetAsync(A<Preset>._))
            .Invokes(call => captured = call.GetArgument<Preset>(0));

        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        Assert.That(captured!.ColumnCount, Is.EqualTo(5),
            "Setting a group's ColumnCount must not change the preset's ColumnCount");
        Assert.That(captured.Groups.Single().ColumnCount, Is.EqualTo(2));
    }

    [Test]
    public void AddTextField_WhenPresetIsSingleColumn_NewFieldHasNoSpanChoice()
    {
        var sut = CreateSut();

        sut.AddTextFieldCommand.Execute(null);

        var added = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>()
            .Last(f => !f.IsDisplayName);
        Assert.That(added.IsInMultiColumnContext, Is.False);
        Assert.That(added.ColumnSpanOptions, Is.EqualTo(new[] { 1 }));
    }
}

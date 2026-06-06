using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;
using Collectary.Presentation.ViewModels.Mapping;
using Collectary.Presentation.ViewModels.SharedFields;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class PresetEditorViewModelTest
{
    private IPresetUseCase _presetUseCase = null!;
    private ISharedFieldUseCase _sharedFieldUseCase = null!;
    private IDialogService _dialogService = null!;

    [SetUp]
    public void SetUp()
    {
        _presetUseCase = A.Fake<IPresetUseCase>();
        _sharedFieldUseCase = A.Fake<ISharedFieldUseCase>();
        _dialogService = A.Fake<IDialogService>();

        A.CallTo(() => _presetUseCase.GetAllPresetsAsync()).Returns(new List<Preset>());
        A.CallTo(() => _sharedFieldUseCase.GetAllAsync()).Returns(new List<SharedField>());
    }

    private readonly IFieldEditorMapper _mapper = new TestFieldEditorMapper().Create();

    private PresetEditorViewModel CreateSut(Preset? existing = null, Action? onSaved = null, Action? onCancelled = null, Preset? seed = null) =>
        new(_presetUseCase, _sharedFieldUseCase, _dialogService, _mapper,
            onSaved: onSaved ?? (() => { }),
            onCancelled: onCancelled ?? (() => { }),
            existing: existing,
            seed: seed);

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
    public async Task HandleSystemBackAsync_SavesNewPresetAndReturnsTrue()
    {
        var onSavedInvoked = false;
        var sut = CreateSut(onSaved: () => { onSavedInvoked = true; });
        sut.Name = "Captured";

        var handled = await ((ISystemBackHandler)sut).HandleSystemBackAsync();

        Assert.Multiple(() =>
        {
            Assert.That(handled, Is.True);
            Assert.That(onSavedInvoked, Is.True);
        });
        A.CallTo(() => _presetUseCase.CreatePresetAsync(
            A<Preset>.That.Matches(p => p.Name == "Captured")))
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
    public async Task SaveAndGoBackAsync_WhenNested_NavigatesUpOneLevelWithoutExiting()
    {
        var exited = false;
        var sut = CreateSut(onSaved: () => exited = true);
        sut.Name = "P";
        sut.AddGroupCommand.Execute(null);
        var group = sut.CurrentRows.OfType<FieldGroupRowViewModel>().First();
        sut.DrillIntoCommand.Execute(group);

        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        Assert.That(sut.Levels.Count, Is.EqualTo(1));
        Assert.That(exited, Is.False);
    }

    [Test]
    public async Task SaveAndGoBackAsync_WhenNested_StillPersists()
    {
        var sut = CreateSut();
        sut.Name = "P";
        sut.AddGroupCommand.Execute(null);
        var group = sut.CurrentRows.OfType<FieldGroupRowViewModel>().First();
        sut.DrillIntoCommand.Execute(group);

        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        A.CallTo(() => _presetUseCase.CreatePresetAsync(A<Preset>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task SaveAndGoBackAsync_WhenNestedAndPersistFails_StaysNested()
    {
        A.CallTo(() => _presetUseCase.CreatePresetAsync(A<Preset>._)).Throws<InvalidOperationException>();
        var sut = CreateSut();
        sut.Name = "P";
        sut.AddGroupCommand.Execute(null);
        var group = sut.CurrentRows.OfType<FieldGroupRowViewModel>().First();
        sut.DrillIntoCommand.Execute(group);

        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        Assert.That(sut.Levels.Count, Is.EqualTo(2));
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

    private static (SharedField sf, SharedFieldRowViewModel row) MakeSharedFieldRow(string label)
    {
        var sf = new SharedField { Name = label, Definition = new TextFieldDefinition { Label = label } };
        sf.Definition.SharedFieldId = sf.Id;
        return (sf, new SharedFieldRowViewModel(sf));
    }

    [Test]
    public void AddSharedFieldCommand_AddsRowToCurrentRows()
    {
        var (_, sfRow) = MakeSharedFieldRow("Tag");
        var sut = CreateSut();
        var before = sut.CurrentRows.Count;

        sut.AddSharedFieldCommand.Execute(sfRow);

        Assert.That(sut.CurrentRows.Count, Is.EqualTo(before + 1));
    }

    [Test]
    public void AddSharedFieldCommand_WhenAlreadyPresent_DoesNotAddDuplicate()
    {
        var (_, sfRow) = MakeSharedFieldRow("Tag");
        var sut = CreateSut();
        sut.AddSharedFieldCommand.Execute(sfRow);
        var countAfterFirst = sut.CurrentRows.Count;

        sut.AddSharedFieldCommand.Execute(sfRow);

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
    public void Constructor_NewPreset_DefaultsFieldLabelLayoutToInherit()
    {
        var sut = CreateSut();

        Assert.That(sut.SelectedFieldLabelLayout.Value, Is.Null);
    }

    [Test]
    public void Constructor_LoadsFieldLabelLayoutFromExisting()
    {
        var sut = CreateSut(existing: new Preset { Name = "P", FieldLabelLayout = FieldLabelLayout.Above });

        Assert.That(sut.SelectedFieldLabelLayout.Value, Is.EqualTo(FieldLabelLayout.Above));
    }

    [Test]
    public async Task PersistAsync_WritesSelectedFieldLabelLayout()
    {
        var sut = CreateSut();
        sut.SelectedFieldLabelLayout = sut.FieldLabelLayoutOptions.First(o => o.Value == FieldLabelLayout.Beside);

        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        A.CallTo(() => _presetUseCase.CreatePresetAsync(
            A<Preset>.That.Matches(p => p.FieldLabelLayout == FieldLabelLayout.Beside)))
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
        await sut.AddFieldAsync<TextFieldDefinition>();
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
        await sut.AddFieldAsync<TextFieldDefinition>();
        var field1 = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().First();

        var churned = false;
        field1.AvailableGroups.CollectionChanged += (_, _) => churned = true;

        await sut.AddFieldAsync<TextFieldDefinition>();

        Assert.That(churned, Is.False, "Re-populating an unchanged group set must not clear/refill AvailableGroups, which would make the bound ComboBox push back a spurious null selection and eject the field from its group.");
        Assert.That(field1.AssignedGroupId, Is.EqualTo(group.Id));
    }

    [Test]
    public void DrillBreadcrumbs_WhenAtRoot_IsEmpty()
    {
        var sut = CreateSut();

        Assert.That(sut.DrillBreadcrumbs, Is.Empty);
    }

    [Test]
    public void DrillBreadcrumbs_WhenDrilledIntoGroup_ContainsThatLevelOnly()
    {
        var sut = CreateSut();
        sut.AddGroupCommand.Execute(null);
        var group = sut.CurrentRows.OfType<FieldGroupRowViewModel>().First();
        group.Name = "Specs";

        sut.DrillIntoCommand.Execute(group);

        Assert.That(sut.DrillBreadcrumbs.Count, Is.EqualTo(1));
        Assert.That(sut.DrillBreadcrumbs[0].Title, Is.EqualTo("Specs"));
    }

    [Test]
    public void DrillBreadcrumbs_AfterNavigatingBackToRoot_IsEmpty()
    {
        var sut = CreateSut();
        sut.AddGroupCommand.Execute(null);
        var group = sut.CurrentRows.OfType<FieldGroupRowViewModel>().First();
        sut.DrillIntoCommand.Execute(group);

        sut.NavigateToLevelCommand.Execute(sut.Levels[0]);

        Assert.That(sut.DrillBreadcrumbs, Is.Empty);
    }

    private static void DrillLevels(PresetEditorViewModel sut, int depth)
    {
        for (var i = 0; i < depth; i++)
        {
            sut.AddGroupCommand.Execute(null);
            var group = sut.CurrentRows.OfType<FieldGroupRowViewModel>().Last();
            sut.DrillIntoCommand.Execute(group);
        }
    }

    [Test]
    public void DrillBreadcrumbs_WhenDeep_ContainsEveryNonRootLevelInOrder()
    {
        var sut = CreateSut();
        DrillLevels(sut, 4);

        Assert.That(sut.DrillBreadcrumbs.Count, Is.EqualTo(4));
        Assert.That(sut.DrillBreadcrumbs[^1], Is.SameAs(sut.Levels[^1]));
        Assert.That(sut.DrillBreadcrumbs[^1].IsCurrent, Is.True);
    }

    [Test]
    public void DrillBreadcrumbs_WhenShallow_IsEmpty()
    {
        var sut = CreateSut();
        DrillLevels(sut, 1);

        Assert.That(sut.DrillBreadcrumbs.Count, Is.EqualTo(1));
        Assert.That(sut.DrillBreadcrumbs[0], Is.SameAs(sut.Levels[^1]));
    }

    [Test]
    public void ResetToRoot_WhenNested_ReturnsToRootLevel()
    {
        var sut = CreateSut();
        sut.AddGroupCommand.Execute(null);
        var group = sut.CurrentRows.OfType<FieldGroupRowViewModel>().First();
        sut.DrillIntoCommand.Execute(group);

        sut.ResetToRoot();

        Assert.That(sut.Levels.Count, Is.EqualTo(1));
        Assert.That(sut.Levels[0].IsCurrent, Is.True);
        Assert.That(sut.DrillBreadcrumbs, Is.Empty);
    }

    [Test]
    public void ResetToRoot_WhenAtRoot_LeavesSingleLevel()
    {
        var sut = CreateSut();

        sut.ResetToRoot();

        Assert.That(sut.Levels.Count, Is.EqualTo(1));
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
        await sut.AddFieldAsync<TextFieldDefinition>();
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
    public void Constructor_WithExistingPresetHavingSharedFieldRefs_LoadsSystemRows()
    {
        var def = new TextFieldDefinition { Label = "Tag" };
        var sf = new SharedField { Name = "Tag", Definition = def };
        def.SharedFieldId = sf.Id;
        var sfRef = new PresetSharedField { SharedFieldId = sf.Id, SharedField = sf, DisplayOrder = 1 };
        var existing = new Preset { Name = "P", SharedFieldRefs = [sfRef] };

        var sut = CreateSut(existing: existing);

        var sysRow = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>()
            .FirstOrDefault(r => r.IsSharedField);
        Assert.That(sysRow, Is.Not.Null);
        Assert.That(sysRow!.DisplayOrder, Is.EqualTo(1));
    }

    [Test]
    public async Task LoadAsync_WhenSharedFieldsThrows_ShowsDialog()
    {
        A.CallTo(() => _sharedFieldUseCase.GetAllAsync()).Throws<InvalidOperationException>();

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
    public async Task PersistAsync_SeparatesOwnFieldsFromSharedFieldRefs()
    {
        var (sf, sfRow) = MakeSharedFieldRow("Tag");
        var sut = CreateSut();
        sut.Name = "Test";
        sut.AddSharedFieldCommand.Execute(sfRow);

        Preset? captured = null;
        A.CallTo(() => _presetUseCase.CreatePresetAsync(A<Preset>._))
            .Invokes(call => captured = call.GetArgument<Preset>(0));

        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.SharedFieldRefs, Has.Count.EqualTo(1));
        Assert.That(captured.SharedFieldRefs[0].SharedFieldId, Is.EqualTo(sf.Id));
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

        sut.AddField<TextFieldDefinition>();

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

        sut.AddField<ListFieldDefinition>();

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

        sut.AddField<TextFieldDefinition>();

        var added = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>()
            .Last(f => !f.IsDisplayName);
        Assert.That(added.IsInMultiColumnContext, Is.False);
        Assert.That(added.ColumnSpanOptions, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void Seed_PreFillsNameAndFields()
    {
        var seed = new Preset
        {
            Name = "Books",
            ColumnCount = 2,
            Fields =
            [
                new DisplayNameFieldDefinition { IsRequired = true },
                new TextFieldDefinition { Label = "Author" }
            ]
        };

        var sut = CreateSut(seed: seed);

        Assert.That(sut.Name, Is.EqualTo("Books"));
        Assert.That(sut.ColumnCount, Is.EqualTo(2));
        Assert.That(sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Any(r => r.Label == "Author"), Is.True);
    }

    [Test]
    public void Seed_DoesNotDuplicateDisplayNameField()
    {
        var seed = new Preset
        {
            Name = "P",
            Fields = [new DisplayNameFieldDefinition { IsRequired = true }, new TextFieldDefinition { Label = "X" }]
        };

        var sut = CreateSut(seed: seed);

        Assert.That(sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Count(r => r.IsDisplayName), Is.EqualTo(1));
    }

    [Test]
    public async Task Seed_IsCreateMode_SaveCallsCreateNotUpdate()
    {
        var seed = new Preset
        {
            Name = "Movies",
            Fields = [new DisplayNameFieldDefinition { IsRequired = true }, new TextFieldDefinition { Label = "Director" }]
        };
        var sut = CreateSut(seed: seed);

        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        A.CallTo(() => _presetUseCase.CreatePresetAsync(A<Preset>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _presetUseCase.UpdatePresetAsync(A<Preset>._)).MustNotHaveHappened();
    }

    [Test]
    public void IsHeaderVisible_WhenNotNestedAndWide_ReturnsTrue()
    {
        var sut = CreateSut();
        sut.IsNarrow = false;

        Assert.That(sut.IsHeaderVisible, Is.True);
    }

    [Test]
    public void IsHeaderVisible_WhenDrilledIn_ReturnsFalse()
    {
        var sut = CreateSut();
        sut.AddField<ListFieldDefinition>();
        var listRow = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().First(r => r.IsList);

        sut.DrillIntoCommand.Execute(listRow);

        Assert.That(sut.IsHeaderVisible, Is.False);
    }

    [Test]
    public void IsHeaderVisible_WhenNarrowAndFieldSelected_ReturnsFalse()
    {
        var sut = CreateSut();
        sut.AddField<TextFieldDefinition>();
        sut.IsNarrow = true;

        sut.SelectedNode = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Last();

        Assert.That(sut.IsHeaderVisible, Is.False);
    }

    [Test]
    public void IsHeaderVisible_WhenNarrowAndNothingSelected_ReturnsTrue()
    {
        var sut = CreateSut();
        sut.IsNarrow = true;
        sut.SelectedNode = null;

        Assert.That(sut.IsHeaderVisible, Is.True);
    }

    [Test]
    public void IsHeaderVisible_WhenWideAndFieldSelected_ReturnsTrue()
    {
        var sut = CreateSut();
        sut.AddField<TextFieldDefinition>();
        sut.IsNarrow = false;

        sut.SelectedNode = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Last();

        Assert.That(sut.IsHeaderVisible, Is.True);
    }

    [Test]
    public void IsHeaderVisible_RaisesPropertyChanged_WhenDrilledIn()
    {
        var sut = CreateSut();
        sut.AddField<ListFieldDefinition>();
        var listRow = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().First(r => r.IsList);
        var raised = new List<string>();
        sut.PropertyChanged += (_, e) => { if (e.PropertyName is not null) raised.Add(e.PropertyName); };

        sut.DrillIntoCommand.Execute(listRow);

        Assert.That(raised, Does.Contain(nameof(PresetEditorViewModel.IsHeaderVisible)));
    }

    [Test]
    public void IsHeaderVisible_RaisesPropertyChanged_WhenNarrowChanges()
    {
        var sut = CreateSut();
        var raised = new List<string>();
        sut.PropertyChanged += (_, e) => { if (e.PropertyName is not null) raised.Add(e.PropertyName); };

        sut.IsNarrow = true;

        Assert.That(raised, Does.Contain(nameof(PresetEditorViewModel.IsHeaderVisible)));
    }

    [Test]
    public void IsHeaderVisible_RaisesPropertyChanged_WhenSelectionChanges()
    {
        var sut = CreateSut();
        sut.AddField<TextFieldDefinition>();
        sut.IsNarrow = true;
        sut.SelectedNode = null;
        var raised = new List<string>();
        sut.PropertyChanged += (_, e) => { if (e.PropertyName is not null) raised.Add(e.PropertyName); };

        sut.SelectedNode = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Last();

        Assert.That(raised, Does.Contain(nameof(PresetEditorViewModel.IsHeaderVisible)));
    }
}

using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Presentation.DI;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class ItemEditorViewModelTest
{
    private IItemUseCase _itemUseCase = null!;
    private IPresetUseCase _presetUseCase = null!;
    private IFieldEditorRegistry _editorRegistry = null!;

    [SetUp]
    public void SetUp()
    {
        _itemUseCase = A.Fake<IItemUseCase>();
        _presetUseCase = A.Fake<IPresetUseCase>();
        _editorRegistry = A.Fake<IFieldEditorRegistry>();
    }

    private ItemEditingContext MakeContext() => new(
        editorRegistry: _editorRegistry,
        listCellBuilder: A.Fake<IListCellBuilder>(),
        goBack: () => { },
        pickAndStoreImageAsync: () => Task.FromResult<(string, string, Avalonia.Media.Imaging.Bitmap)?>(null),
        exportImageAsync: (_, _) => Task.CompletedTask,
        loadImageBitmap: _ => null,
        deleteImageAsync: _ => Task.CompletedTask);

    private ItemEditorViewModel CreateSut(
        Preset? preset = null,
        IReadOnlyList<FieldDefinition>? fields = null,
        Item? existing = null,
        Action? onSaved = null,
        Action? onCancelled = null,
        ItemEditingContext? context = null,
        EffectiveFields? effective = null)
    {
        var ctx = context ?? MakeContext();
        var sut = new ItemEditorViewModel(
            _itemUseCase,
            _presetUseCase,
            preset ?? new Preset(),
            effective ?? new EffectiveFields { Fields = fields ?? [] },
            onSaved: onSaved ?? (() => { }),
            onCancelled: onCancelled ?? (() => { }),
            context: ctx,
            existing: existing);
        ctx.SaveAsync = sut.PersistAsync;
        return sut;
    }

    private FieldEditorViewModelBase FakeEditorFor(FieldDefinition definition)
    {
        var editor = A.Fake<FieldEditorViewModelBase>();
        A.CallTo(() => editor.Definition).Returns(definition);
        A.CallTo(() => _editorRegistry.Create(definition, A<FieldValue?>._, A<ItemEditingContext>._)).Returns(editor);
        return editor;
    }

    [Test]
    public void Constructor_Adaptive_MultiColumn_SetsLabelAboveTrue()
    {
        var def = new TextFieldDefinition { Label = "A" };
        var editor = FakeEditorFor(def);
        var ctx = MakeContext();
        ctx.GlobalFieldLabelLayout = FieldLabelLayout.Adaptive;

        CreateSut(preset: new Preset { ColumnCount = 2 }, fields: [def], context: ctx);

        Assert.Multiple(() =>
        {
            Assert.That(ctx.LabelAbove, Is.True);
            Assert.That(editor.LabelAbove, Is.True);
        });
    }

    [Test]
    public void Constructor_Adaptive_SingleColumn_SetsLabelAboveFalse()
    {
        var def = new TextFieldDefinition { Label = "A" };
        var editor = FakeEditorFor(def);
        var ctx = MakeContext();
        ctx.GlobalFieldLabelLayout = FieldLabelLayout.Adaptive;

        CreateSut(preset: new Preset { ColumnCount = 1 }, fields: [def], context: ctx);

        Assert.Multiple(() =>
        {
            Assert.That(ctx.LabelAbove, Is.False);
            Assert.That(editor.LabelAbove, Is.False);
        });
    }

    [Test]
    public void Constructor_PresetOverride_BeatsGlobalDefault()
    {
        var def = new TextFieldDefinition { Label = "A" };
        var editor = FakeEditorFor(def);
        var ctx = MakeContext();
        ctx.GlobalFieldLabelLayout = FieldLabelLayout.Above;

        CreateSut(preset: new Preset { ColumnCount = 2, FieldLabelLayout = FieldLabelLayout.Beside },
            fields: [def], context: ctx);

        Assert.That(editor.LabelAbove, Is.False);
    }

    [Test]
    public void Constructor_CreatesEditorsForEachEffectiveField()
    {
        var fields = new List<FieldDefinition>
        {
            new TextFieldDefinition { Label = "A" },
            new BoolFieldDefinition { Label = "B" }
        };
        var fakeEditorA = A.Fake<FieldEditorViewModelBase>();
        var fakeEditorB = A.Fake<FieldEditorViewModelBase>();
        A.CallTo(() => _editorRegistry.Create(fields[0], A<FieldValue?>._, A<ItemEditingContext>._)).Returns(fakeEditorA);
        A.CallTo(() => _editorRegistry.Create(fields[1], A<FieldValue?>._, A<ItemEditingContext>._)).Returns(fakeEditorB);

        var sut = CreateSut(fields: fields);

        Assert.That(sut.FieldEditors.Count, Is.EqualTo(2));
    }

    [Test]
    public void Constructor_SkipsNullEditorsFromRegistry()
    {
        var fields = new List<FieldDefinition> { new TextFieldDefinition { Label = "A" } };
        A.CallTo(() => _editorRegistry.Create(A<FieldDefinition>._, A<FieldValue?>._, A<ItemEditingContext>._))
            .Returns(null);

        var sut = CreateSut(fields: fields);

        Assert.That(sut.FieldEditors, Is.Empty);
    }

    [Test]
    public void Constructor_WhenExistingItem_SetsDisplayName()
    {
        var existing = new Item { DisplayName = "My Item" };
        var sut = CreateSut(existing: existing);

        Assert.That(sut.DisplayName, Is.EqualTo("My Item"));
    }

    [Test]
    public async Task PersistAsync_WhenNew_CallsCreateItemAsync()
    {
        var preset = new Preset();
        var sut = CreateSut(preset: preset);
        sut.DisplayName = "New Item";

        await sut.PersistAsync();

        A.CallTo(() => _itemUseCase.CreateItemAsync(
            A<Item>.That.Matches(i => i.PresetId == preset.Id)))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task PersistAsync_WhenExisting_CallsUpdateItemAsync()
    {
        var existing = new Item { DisplayName = "Existing" };
        var sut = CreateSut(existing: existing);

        await sut.PersistAsync();

        A.CallTo(() => _itemUseCase.UpdateItemAsync(existing)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _itemUseCase.CreateItemAsync(A<Item>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task PersistAsync_TrimsDisplayName()
    {
        var sut = CreateSut();
        sut.DisplayName = "  Trimmed  ";

        await sut.PersistAsync();

        A.CallTo(() => _itemUseCase.CreateItemAsync(
            A<Item>.That.Matches(i => i.DisplayName == "Trimmed")))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task PersistAsync_CollectsValuesFromFieldEditors()
    {
        var fakeDef = new TextFieldDefinition { Label = "X" };
        var fakeValue = new TextFieldValue { Value = "hello" };
        var fakeEditor = A.Fake<FieldEditorViewModelBase>();
        A.CallTo(() => fakeEditor.GetCurrentValue()).Returns(fakeValue);
        A.CallTo(() => _editorRegistry.Create(fakeDef, A<FieldValue?>._, A<ItemEditingContext>._)).Returns(fakeEditor);

        var sut = CreateSut(fields: [fakeDef]);

        Item? captured = null;
        A.CallTo(() => _itemUseCase.CreateItemAsync(A<Item>._))
            .Invokes(call => captured = call.GetArgument<Item>(0));

        await sut.PersistAsync();

        Assert.That(captured?.Values, Has.Count.EqualTo(1));
        Assert.That(captured!.Values[0], Is.SameAs(fakeValue));
    }

    [Test]
    public async Task BackCommand_WhenPersistSucceeds_InvokesOnSaved()
    {
        var invoked = false;
        var sut = CreateSut(onSaved: () => { invoked = true; });

        await sut.BackCommand.ExecuteAsync(null);

        Assert.That(invoked, Is.True);
    }

    [Test]
    public async Task BackCommand_WhenPersistFails_DoesNotInvokeOnSaved()
    {
        A.CallTo(() => _itemUseCase.CreateItemAsync(A<Item>._)).Throws<InvalidOperationException>();
        var invoked = false;
        var sut = CreateSut(onSaved: () => { invoked = true; });

        await sut.BackCommand.ExecuteAsync(null);

        Assert.That(invoked, Is.False);
    }

    [Test]
    public async Task HandleSystemBackAsync_PersistsItemNavigatesBackAndReturnsTrue()
    {
        var invoked = false;
        var sut = CreateSut(onSaved: () => { invoked = true; });
        sut.DisplayName = "Captured";

        var handled = await ((ISystemBackHandler)sut).HandleSystemBackAsync();

        Assert.Multiple(() =>
        {
            Assert.That(handled, Is.True);
            Assert.That(invoked, Is.True);
        });
        A.CallTo(() => _itemUseCase.CreateItemAsync(A<Item>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task SaveCommand_WhenPersistFails_SetsErrorMessage()
    {
        A.CallTo(() => _itemUseCase.CreateItemAsync(A<Item>._)).Throws<InvalidOperationException>();
        var sut = CreateSut();

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.That(sut.ErrorMessage, Is.Not.Null);
    }

    [Test]
    public async Task SaveCommand_WhenPersistSucceeds_ClearsErrorMessage()
    {
        var sut = CreateSut();
        sut.ErrorMessage = "old error";

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.That(sut.ErrorMessage, Is.Null);
    }

    [Test]
    public void Constructor_AddsDisplayNameEditorForDisplayNameField()
    {
        var sut = CreateSut(fields: [new DisplayNameFieldDefinition()], existing: new Item { DisplayName = "Seed" });

        var dnEditor = sut.FieldEditors.OfType<DisplayNameFieldEditorViewModel>().SingleOrDefault();
        Assert.That(dnEditor, Is.Not.Null);
        Assert.That(dnEditor!.Text, Is.EqualTo("Seed"));
    }

    [Test]
    public async Task PersistAsync_UsesDisplayNameEditorTextOverProperty()
    {
        var sut = CreateSut(fields: [new DisplayNameFieldDefinition()]);
        var dnEditor = sut.FieldEditors.OfType<DisplayNameFieldEditorViewModel>().Single();
        dnEditor.Text = "  From Editor  ";
        sut.DisplayName = "From Property";

        Item? captured = null;
        A.CallTo(() => _itemUseCase.CreateItemAsync(A<Item>._)).Invokes(c => captured = c.GetArgument<Item>(0));

        await sut.PersistAsync();

        Assert.That(captured!.DisplayName, Is.EqualTo("From Editor"));
    }

    [Test]
    public async Task PersistAsync_ExcludesDisplayNameEditorFromValues()
    {
        var sut = CreateSut(fields: [new DisplayNameFieldDefinition()]);

        Item? captured = null;
        A.CallTo(() => _itemUseCase.CreateItemAsync(A<Item>._)).Invokes(c => captured = c.GetArgument<Item>(0));

        await sut.PersistAsync();

        Assert.That(captured!.Values, Is.Empty);
    }

    [Test]
    public async Task PersistAsync_WhenNew_AdoptsCreatedItemSoSecondSaveUpdates()
    {
        var sut = CreateSut();

        await sut.PersistAsync();
        await sut.PersistAsync();

        A.CallTo(() => _itemUseCase.CreateItemAsync(A<Item>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _itemUseCase.UpdateItemAsync(A<Item>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public void IsNarrow_SetterForwardsToContext()
    {
        var ctx = MakeContext();
        var sut = CreateSut(context: ctx);

        sut.IsNarrow = true;

        Assert.That(ctx.IsNarrow, Is.True);
    }

    [Test]
    public void Constructor_NarrowContext_ForcesLabelAbove_EvenWhenBeside()
    {
        var def = new TextFieldDefinition { Label = "A" };
        var editor = FakeEditorFor(def);
        var ctx = MakeContext();
        ctx.GlobalFieldLabelLayout = FieldLabelLayout.Beside;
        ctx.IsNarrow = true;

        CreateSut(preset: new Preset { ColumnCount = 1 }, fields: [def], context: ctx);

        Assert.Multiple(() =>
        {
            Assert.That(ctx.LabelAbove, Is.True);
            Assert.That(editor.LabelAbove, Is.True);
        });
    }

    [Test]
    public void Constructor_NotNarrowContext_Beside_KeepsLabelBeside()
    {
        var def = new TextFieldDefinition { Label = "A" };
        var editor = FakeEditorFor(def);
        var ctx = MakeContext();
        ctx.GlobalFieldLabelLayout = FieldLabelLayout.Beside;
        ctx.IsNarrow = false;

        CreateSut(preset: new Preset { ColumnCount = 1 }, fields: [def], context: ctx);

        Assert.That(editor.LabelAbove, Is.False);
    }

    [Test]
    public void IsNarrow_SetTrue_FlipsBesideLabelsAbove()
    {
        var def = new TextFieldDefinition { Label = "A" };
        var editor = FakeEditorFor(def);
        var ctx = MakeContext();
        ctx.GlobalFieldLabelLayout = FieldLabelLayout.Beside;
        ctx.IsNarrow = false;
        var sut = CreateSut(preset: new Preset { ColumnCount = 1 }, fields: [def], context: ctx);
        Assume.That(editor.LabelAbove, Is.False);

        sut.IsNarrow = true;

        Assert.That(editor.LabelAbove, Is.True);
    }

    [Test]
    public void IsNarrow_SetBackToFalse_RestoresBesideLabels()
    {
        var def = new TextFieldDefinition { Label = "A" };
        var editor = FakeEditorFor(def);
        var ctx = MakeContext();
        ctx.GlobalFieldLabelLayout = FieldLabelLayout.Beside;
        ctx.IsNarrow = true;
        var sut = CreateSut(preset: new Preset { ColumnCount = 1 }, fields: [def], context: ctx);
        Assume.That(editor.LabelAbove, Is.True);

        sut.IsNarrow = false;

        Assert.That(editor.LabelAbove, Is.False);
    }

    [Test]
    public void FieldMinColumnWidth_Beside_IsWiderThanAbove_SoBesideColumnsCollapseSooner()
    {
        var besideCtx = MakeContext();
        besideCtx.GlobalFieldLabelLayout = FieldLabelLayout.Beside;
        besideCtx.IsNarrow = false;
        var beside = CreateSut(preset: new Preset { ColumnCount = 2 }, context: besideCtx);

        var aboveCtx = MakeContext();
        aboveCtx.GlobalFieldLabelLayout = FieldLabelLayout.Above;
        var above = CreateSut(preset: new Preset { ColumnCount = 2 }, context: aboveCtx);

        Assert.That(beside.FieldMinColumnWidth, Is.GreaterThan(above.FieldMinColumnWidth));
    }

    [Test]
    public void CancelCommand_InvokesOnCancelled()
    {
        var invoked = false;
        var sut = CreateSut(onCancelled: () => { invoked = true; });

        sut.CancelCommand.Execute(null);

        Assert.That(invoked, Is.True);
    }

    [Test]
    public void Constructor_BucketsGroupedAndUngroupedFields()
    {
        var group = new FieldGroup { Name = "Specs", DisplayMode = GroupDisplayMode.Card, DisplayOrder = 0 };
        var grouped = new TextFieldDefinition { Label = "G" };
        var ungrouped = new TextFieldDefinition { Label = "U" };
        FakeEditorFor(grouped);
        FakeEditorFor(ungrouped);

        var sut = CreateSut(effective: new EffectiveFields
        {
            Fields = [ungrouped, grouped],
            Groups = [group],
            GroupByFieldId = new Dictionary<Guid, Guid?> { [grouped.Id] = group.Id }
        });

        Assert.That(sut.UngroupedEditors, Has.Count.EqualTo(1));
        var card = sut.LayoutRegions.OfType<FieldGroupViewModel>().Single();
        Assert.That(card.Editors, Has.Count.EqualTo(1));
    }

    [Test]
    public void Constructor_TabGroupsMergeIntoSingleTabRegion()
    {
        var tab1 = new FieldGroup { Name = "T1", DisplayMode = GroupDisplayMode.Tab, DisplayOrder = 0 };
        var tab2 = new FieldGroup { Name = "T2", DisplayMode = GroupDisplayMode.Tab, DisplayOrder = 1 };
        var f1 = new TextFieldDefinition { Label = "F1" };
        var f2 = new TextFieldDefinition { Label = "F2" };
        FakeEditorFor(f1);
        FakeEditorFor(f2);

        var sut = CreateSut(effective: new EffectiveFields
        {
            Fields = [f1, f2],
            Groups = [tab1, tab2],
            GroupByFieldId = new Dictionary<Guid, Guid?> { [f1.Id] = tab1.Id, [f2.Id] = tab2.Id }
        });

        var tabRegion = sut.LayoutRegions.OfType<TabRegionViewModel>().Single();
        Assert.That(tabRegion.TabGroups, Has.Count.EqualTo(2));
    }

    [Test]
    public void Constructor_EmptyGroupProducesNoRegion()
    {
        var group = new FieldGroup { Name = "Empty", DisplayMode = GroupDisplayMode.Card, DisplayOrder = 0 };

        var sut = CreateSut(effective: new EffectiveFields
        {
            Fields = [],
            Groups = [group],
            GroupByFieldId = new Dictionary<Guid, Guid?>()
        });

        Assert.That(sut.LayoutRegions, Is.Empty);
    }

    [Test]
    public void Constructor_GroupIsExpandedFromDefaultCollapsed()
    {
        var group = new FieldGroup { Name = "G", DisplayMode = GroupDisplayMode.Card, DefaultCollapsed = true, DisplayOrder = 0 };
        var field = new TextFieldDefinition { Label = "F" };
        FakeEditorFor(field);

        var sut = CreateSut(effective: new EffectiveFields
        {
            Fields = [field],
            Groups = [group],
            GroupByFieldId = new Dictionary<Guid, Guid?> { [field.Id] = group.Id }
        });

        var card = sut.LayoutRegions.OfType<FieldGroupViewModel>().Single();
        Assert.That(card.IsExpanded, Is.False);
    }

    [Test]
    public void Constructor_CardGroupContainingTwoTabGroupsRendersTabRegionAsChildRegion()
    {
        var parent = new FieldGroup { Name = "Parent", DisplayMode = GroupDisplayMode.Card, DisplayOrder = 0 };
        var tabB = new FieldGroup { Name = "B", DisplayMode = GroupDisplayMode.Tab, DisplayOrder = 0, ParentGroupId = parent.Id };
        var tabC = new FieldGroup { Name = "C", DisplayMode = GroupDisplayMode.Tab, DisplayOrder = 1, ParentGroupId = parent.Id };
        var f1 = new TextFieldDefinition { Label = "F1" };
        var f2 = new TextFieldDefinition { Label = "F2" };
        FakeEditorFor(f1);
        FakeEditorFor(f2);

        var sut = CreateSut(effective: new EffectiveFields
        {
            Fields = [f1, f2],
            Groups = [parent, tabB, tabC],
            GroupByFieldId = new Dictionary<Guid, Guid?> { [f1.Id] = tabB.Id, [f2.Id] = tabC.Id }
        });

        var parentCard = sut.LayoutRegions.OfType<FieldGroupViewModel>().Single();
        Assert.That(parentCard.Name, Is.EqualTo("Parent"));
        Assert.That(parentCard.Editors, Is.Empty);
        var tabRegion = parentCard.ChildRegions.OfType<TabRegionViewModel>().Single();
        Assert.That(tabRegion.TabGroups.Select(g => g.Name), Is.EqualTo(new[] { "B", "C" }));
    }

    [Test]
    public void Constructor_NestedCardGroupRendersAsChildRegion()
    {
        var parent = new FieldGroup { Name = "Parent", DisplayMode = GroupDisplayMode.Card, DisplayOrder = 0 };
        var child = new FieldGroup { Name = "Child", DisplayMode = GroupDisplayMode.Card, DisplayOrder = 0, ParentGroupId = parent.Id };
        var field = new TextFieldDefinition { Label = "Deep" };
        FakeEditorFor(field);

        var sut = CreateSut(effective: new EffectiveFields
        {
            Fields = [field],
            Groups = [parent, child],
            GroupByFieldId = new Dictionary<Guid, Guid?> { [field.Id] = child.Id }
        });

        var parentCard = sut.LayoutRegions.OfType<FieldGroupViewModel>().Single();
        Assert.That(parentCard.Name, Is.EqualTo("Parent"));
        var childCard = parentCard.ChildRegions.OfType<FieldGroupViewModel>().Single();
        Assert.That(childCard.Name, Is.EqualTo("Child"));
        Assert.That(childCard.Editors, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task PersistAsync_StillCollectsAllEditorsRegardlessOfGrouping()
    {
        var group = new FieldGroup { Name = "G", DisplayMode = GroupDisplayMode.Card, DisplayOrder = 0 };
        var grouped = new TextFieldDefinition { Label = "G" };
        var ungrouped = new TextFieldDefinition { Label = "U" };
        var gEditor = FakeEditorFor(grouped);
        var uEditor = FakeEditorFor(ungrouped);
        A.CallTo(() => gEditor.GetCurrentValue()).Returns(new TextFieldValue { Value = "g" });
        A.CallTo(() => uEditor.GetCurrentValue()).Returns(new TextFieldValue { Value = "u" });

        var sut = CreateSut(effective: new EffectiveFields
        {
            Fields = [ungrouped, grouped],
            Groups = [group],
            GroupByFieldId = new Dictionary<Guid, Guid?> { [grouped.Id] = group.Id }
        });

        Item? captured = null;
        A.CallTo(() => _itemUseCase.CreateItemAsync(A<Item>._)).Invokes(call => captured = call.GetArgument<Item>(0));

        await sut.PersistAsync();

        Assert.That(captured?.Values, Has.Count.EqualTo(2));
    }
}

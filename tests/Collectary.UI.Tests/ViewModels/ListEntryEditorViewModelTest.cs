using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Presentation.DI;
using Collectary.Presentation.Localization;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class ListEntryEditorViewModelTest
{
    private IFieldEditorRegistry _registry = null!;

    [SetUp]
    public void SetUp()
    {
        _registry = A.Fake<IFieldEditorRegistry>();
        LocalizationService.Instance.Apply("en");
    }

    private ItemEditingContext MakeContext(Action? goBack = null, Func<Task>? save = null)
    {
        var ctx = new ItemEditingContext(
            editorRegistry: _registry,
            listCellBuilder: A.Fake<IListCellBuilder>(),
            goBack: goBack ?? (() => { }),
            pickAndStoreImageAsync: () => Task.FromResult<(string, string, Avalonia.Media.Imaging.Bitmap)?>(null),
            exportImageAsync: (_, _) => Task.CompletedTask,
            loadImageBitmap: _ => null,
            deleteImageAsync: _ => Task.CompletedTask);
        if (save is not null) ctx.SaveAsync = save;
        return ctx;
    }

    private FieldEditorViewModelBase FakeEditorFor(FieldDefinition def, FieldValue? value = null)
    {
        var editor = A.Fake<FieldEditorViewModelBase>();
        A.CallTo(() => editor.Definition).Returns(def);
        if (value is not null) A.CallTo(() => editor.GetCurrentValue()).Returns(value);
        A.CallTo(() => _registry.Create(def, A<FieldValue?>._, A<ItemEditingContext>._)).Returns(editor);
        return editor;
    }

    [Test]
    public void EntryId_ReflectsEntry()
    {
        var entry = new ListEntry();
        var sut = new ListEntryEditorViewModel(new ListFieldDefinition(), entry, 1, MakeContext());
        Assert.That(sut.EntryId, Is.EqualTo(entry.Id));
    }

    [Test]
    public void EntryLabel_CombinesLocalizedWordAndNumber()
    {
        var sut = new ListEntryEditorViewModel(new ListFieldDefinition(), new ListEntry(), 3, MakeContext());
        Assert.That(sut.EntryLabel, Is.EqualTo($"{LocalizationService.Instance["Entry"]} 3"));
    }

    [Test]
    public void EntryNumberChange_RaisesEntryLabelNotification()
    {
        var sut = new ListEntryEditorViewModel(new ListFieldDefinition(), new ListEntry(), 1, MakeContext());
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.EntryNumber = 2;

        Assert.That(raised, Does.Contain(nameof(sut.EntryLabel)));
    }

    [Test]
    public void Constructor_BuildsEditorsForSubFieldsInDisplayOrder()
    {
        var subA = new TextFieldDefinition { Label = "A", DisplayOrder = 0 };
        var subB = new TextFieldDefinition { Label = "B", DisplayOrder = 1 };
        var editorA = FakeEditorFor(subA);
        var editorB = FakeEditorFor(subB);
        var def = new ListFieldDefinition { SubFields = [subB, subA] };

        var sut = new ListEntryEditorViewModel(def, new ListEntry(), 1, MakeContext());

        Assert.That(sut.FieldEditors, Is.EqualTo(new[] { editorA, editorB }));
    }

    [Test]
    public void Constructor_SkipsNullEditors()
    {
        var sub = new TextFieldDefinition { Label = "A" };
        A.CallTo(() => _registry.Create(sub, A<FieldValue?>._, A<ItemEditingContext>._)).Returns(null);
        var def = new ListFieldDefinition { SubFields = [sub] };

        var sut = new ListEntryEditorViewModel(def, new ListEntry(), 1, MakeContext());

        Assert.That(sut.FieldEditors, Is.Empty);
    }

    [Test]
    public void CollectValues_ReturnsEachEditorsCurrentValue()
    {
        var sub = new TextFieldDefinition { Label = "A" };
        var value = new TextFieldValue { Value = "v" };
        FakeEditorFor(sub, value);
        var def = new ListFieldDefinition { SubFields = [sub] };

        var sut = new ListEntryEditorViewModel(def, new ListEntry(), 1, MakeContext());

        Assert.That(sut.CollectValues(), Is.EqualTo(new[] { value }));
    }

    [Test]
    public async Task SaveCommand_InvokesContextSave()
    {
        var saved = false;
        var sut = new ListEntryEditorViewModel(new ListFieldDefinition(), new ListEntry(), 1,
            MakeContext(save: () => { saved = true; return Task.CompletedTask; }));

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.That(saved, Is.True);
    }

    [Test]
    public void GoBackCommand_InvokesContextGoBack()
    {
        var back = false;
        var sut = new ListEntryEditorViewModel(new ListFieldDefinition(), new ListEntry(), 1,
            MakeContext(goBack: () => back = true));

        sut.GoBackCommand.Execute(null);

        Assert.That(back, Is.True);
    }

    [Test]
    public async Task SaveAndGoBackCommand_SavesThenGoesBack()
    {
        var order = new List<string>();
        var sut = new ListEntryEditorViewModel(new ListFieldDefinition(), new ListEntry(), 1,
            MakeContext(goBack: () => order.Add("back"), save: () => { order.Add("save"); return Task.CompletedTask; }));

        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        Assert.That(order, Is.EqualTo(new[] { "save", "back" }));
    }

    [Test]
    public async Task HandleSystemBackAsync_SavesThenGoesBackAndReturnsTrue()
    {
        var order = new List<string>();
        var sut = new ListEntryEditorViewModel(new ListFieldDefinition(), new ListEntry(), 1,
            MakeContext(goBack: () => order.Add("back"), save: () => { order.Add("save"); return Task.CompletedTask; }));

        var handled = await ((ISystemBackHandler)sut).HandleSystemBackAsync();

        Assert.Multiple(() =>
        {
            Assert.That(handled, Is.True);
            Assert.That(order, Is.EqualTo(new[] { "save", "back" }));
        });
    }
}

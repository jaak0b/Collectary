using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.UI.DI;
using Collectary.UI.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class ListFieldEditorViewModelTest
{
    private IFieldEditorRegistry _registry = null!;
    private IListCellBuilder _cellBuilder = null!;

    [SetUp]
    public void SetUp() => (_registry, _cellBuilder) = ListFieldEditorTestHarness.MakeFakes();

    private ItemEditingContext MakeContext(
        Action<ListFieldEditorViewModel>? openList = null,
        Action<ListEntryEditorViewModel, string>? openEntry = null,
        Func<Task>? save = null,
        Action? goBack = null) =>
        ListFieldEditorTestHarness.MakeContext(_registry, _cellBuilder, openList, openEntry, save, goBack);

    [Test]
    public void Constructor_BuildsEntriesFromValueOrderedByDisplayOrder()
    {
        var value = new ListFieldValue();
        value.Entries.Add(new ListEntry { DisplayOrder = 1 });
        value.Entries.Add(new ListEntry { DisplayOrder = 0 });

        var sut = new ListFieldEditorViewModel(ListFieldEditorTestHarness.DefinitionWith(), value, MakeContext());

        Assert.That(sut.Entries, Has.Count.EqualTo(2));
        Assert.That(sut.Entries[0].EntryNumber, Is.EqualTo(1));
        Assert.That(sut.Entries[1].EntryNumber, Is.EqualTo(2));
        Assert.That(sut.EntryCount, Is.EqualTo(2));
    }

    [Test]
    public void ColumnFields_IncludeOnlyDisplayableWithAvailableCell()
    {
        var shown = new TextFieldDefinition { Label = "Title", ShowInList = true };
        var hidden = new TextFieldDefinition { Label = "Notes", ShowInList = false };
        var noCell = new TextFieldDefinition { Label = "NoCell", ShowInList = true };

        A.CallTo(() => _cellBuilder.HasListCellViewModel(typeof(TextFieldDefinition))).Returns(true);
        A.CallTo(() => _cellBuilder.HasListCellViewModel(typeof(ImageFieldDefinition))).Returns(false);
        var noCellImage = new ImageFieldDefinition { Label = "Pic" };

        var sut = new ListFieldEditorViewModel(
            ListFieldEditorTestHarness.DefinitionWith(shown, hidden, noCell, noCellImage), new ListFieldValue(), MakeContext());

        Assert.That(sut.ColumnFields.Select(f => f.Label), Is.EqualTo(new[] { "Title", "NoCell" }));
    }

    [Test]
    public void AddEntry_AppendsRenumbersAndOpensEntry()
    {
        ListEntryEditorViewModel? opened = null;
        var ctx = MakeContext(openEntry: (e, _) => opened = e);
        var sut = new ListFieldEditorViewModel(ListFieldEditorTestHarness.DefinitionWith(), ListFieldEditorTestHarness.ValueWithEntries(1), ctx);

        sut.AddEntryCommand.Execute(null);

        Assert.That(sut.Entries, Has.Count.EqualTo(2));
        Assert.That(sut.Entries[1].EntryNumber, Is.EqualTo(2));
        Assert.That(opened, Is.SameAs(sut.Entries[1]));
        Assert.That(sut.EntryRows, Has.Count.EqualTo(2));
    }

    [Test]
    public void DeleteEntry_RemovesAndRenumbersRemaining()
    {
        var sut = new ListFieldEditorViewModel(ListFieldEditorTestHarness.DefinitionWith(), ListFieldEditorTestHarness.ValueWithEntries(3), MakeContext());
        var firstRow = sut.EntryRows[0];

        sut.DeleteEntryCommand.Execute(firstRow);

        Assert.That(sut.Entries, Has.Count.EqualTo(2));
        Assert.That(sut.Entries[0].EntryNumber, Is.EqualTo(1));
        Assert.That(sut.Entries[1].EntryNumber, Is.EqualTo(2));
    }

    [Test]
    public void Open_DelegatesToContextOpenList()
    {
        ListFieldEditorViewModel? opened = null;
        var ctx = MakeContext(openList: l => opened = l);
        var sut = new ListFieldEditorViewModel(ListFieldEditorTestHarness.DefinitionWith(), new ListFieldValue(), ctx);

        sut.OpenCommand.Execute(null);

        Assert.That(opened, Is.SameAs(sut));
    }

    [Test]
    public void EditEntry_DelegatesToContextOpenEntry()
    {
        ListEntryEditorViewModel? opened = null;
        var ctx = MakeContext(openEntry: (e, _) => opened = e);
        var sut = new ListFieldEditorViewModel(ListFieldEditorTestHarness.DefinitionWith(), ListFieldEditorTestHarness.ValueWithEntries(1), ctx);

        sut.EditEntryCommand.Execute(sut.EntryRows[0]);

        Assert.That(opened, Is.SameAs(sut.Entries[0]));
    }

    [Test]
    public async Task SaveAndGoBack_SavesThenGoesBack()
    {
        var order = new List<string>();
        var ctx = MakeContext(save: () => { order.Add("save"); return Task.CompletedTask; },
                              goBack: () => order.Add("back"));
        var sut = new ListFieldEditorViewModel(ListFieldEditorTestHarness.DefinitionWith(), new ListFieldValue(), ctx);

        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        Assert.That(order, Is.EqualTo(new[] { "save", "back" }));
    }

    [Test]
    public void GetCurrentValue_RebuildsEntriesWithSubValuesAndLinks()
    {
        var sub = new TextFieldDefinition { Label = "Title" };
        var value = ListFieldEditorTestHarness.ValueWithEntries(2);
        var sut = new ListFieldEditorViewModel(ListFieldEditorTestHarness.DefinitionWith(sub), value, MakeContext());

        var result = (ListFieldValue)sut.GetCurrentValue();

        Assert.That(result.Entries, Has.Count.EqualTo(2));
        for (var i = 0; i < result.Entries.Count; i++)
        {
            var entry = result.Entries[i];
            Assert.That(entry.DisplayOrder, Is.EqualTo(i));
            Assert.That(entry.ListFieldValueId, Is.EqualTo(value.Id));
            Assert.That(entry.SubValues, Has.Count.EqualTo(1));
            Assert.That(entry.SubValues[0].ListEntryId, Is.EqualTo(entry.Id));
        }
    }

    [Test]
    public void IsGridInline_ReflectsDefinitionInlineStyle()
    {
        var grid = new ListFieldEditorViewModel(
            new ListFieldDefinition { InlineStyle = ListInlineStyle.Grid }, new ListFieldValue(), MakeContext());
        var card = new ListFieldEditorViewModel(
            new ListFieldDefinition { InlineStyle = ListInlineStyle.Card }, new ListFieldValue(), MakeContext());

        Assert.That(grid.IsGridInline, Is.True);
        Assert.That(grid.IsCardInline, Is.False);
        Assert.That(card.IsCardInline, Is.True);
        Assert.That(card.IsGridInline, Is.False);
    }
}

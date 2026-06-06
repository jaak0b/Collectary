using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class ListDetailViewModelTest
{
    [Test]
    public void DelegatesToUnderlyingList()
    {
        var (registry, cellBuilder) = ListFieldEditorTestHarness.MakeFakes();
        var ctx = ListFieldEditorTestHarness.MakeContext(registry, cellBuilder);
        var list = new ListFieldEditorViewModel(
            ListFieldEditorTestHarness.DefinitionWith(), ListFieldEditorTestHarness.ValueWithEntries(1), ctx);

        var sut = new ListDetailViewModel(list, ctx);

        Assert.That(sut.List, Is.SameAs(list));
        Assert.That(sut.Label, Is.EqualTo(list.Label));
        Assert.That(sut.EntryRows, Is.SameAs(list.EntryRows));
        Assert.That(sut.ColumnFields, Is.SameAs(list.ColumnFields));
        Assert.That(sut.AddEntryCommand, Is.SameAs(list.AddEntryCommand));
        Assert.That(sut.SaveCommand, Is.SameAs(list.SaveCommand));
    }

    [Test]
    public async Task HandleSystemBackAsync_SavesThroughList_AndReturnsTrue()
    {
        var (registry, cellBuilder) = ListFieldEditorTestHarness.MakeFakes();
        var order = new List<string>();
        var ctx = ListFieldEditorTestHarness.MakeContext(registry, cellBuilder,
            save: () => { order.Add("save"); return Task.CompletedTask; },
            goBack: () => order.Add("back"));
        var list = new ListFieldEditorViewModel(
            ListFieldEditorTestHarness.DefinitionWith(), ListFieldEditorTestHarness.ValueWithEntries(1), ctx);
        var sut = new ListDetailViewModel(list, ctx);

        var handled = await ((ISystemBackHandler)sut).HandleSystemBackAsync();

        Assert.Multiple(() =>
        {
            Assert.That(handled, Is.True);
            Assert.That(order, Is.EqualTo(new[] { "save", "back" }));
        });
    }
}

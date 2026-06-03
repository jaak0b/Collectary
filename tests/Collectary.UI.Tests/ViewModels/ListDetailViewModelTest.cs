using Collectary.UI.ViewModels;

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
}

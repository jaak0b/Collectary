using FakeItEasy;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.DI;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class LinkedItemFieldEditorViewModelTest
{
    private static ItemEditingContext MakeContext(IReadOnlyList<LinkedItemOption>? candidates = null)
    {
        var ctx = new ItemEditingContext(
            editorRegistry: A.Fake<IFieldEditorRegistry>(),
            listCellBuilder: A.Fake<IListCellBuilder>(),
            goBack: () => { },
            pickAndStoreImageAsync: () => Task.FromResult<(string, string, Avalonia.Media.Imaging.Bitmap)?>(null),
            exportImageAsync: (_, _) => Task.CompletedTask,
            loadImageBitmap: _ => null,
            deleteImageAsync: _ => Task.CompletedTask);
        if (candidates is not null)
            ctx.LoadLinkableItemsAsync = () => Task.FromResult(candidates);
        return ctx;
    }

    [Test]
    public void ExistingLink_ShownBeforeLoad()
    {
        var id = Guid.NewGuid();
        var sut = new LinkedItemFieldEditorViewModel(new LinkedItemFieldDefinition(),
            new LinkedItemFieldValue { TargetItemId = id, TargetDisplay = "Falcon" }, MakeContext());

        Assert.That(sut.SelectedItem, Is.Not.Null);
        Assert.That(sut.SelectedItem!.Id, Is.EqualTo(id));
        Assert.That(sut.SelectedItem.Display, Is.EqualTo("Falcon"));
    }

    [Test]
    public async Task LoadCandidates_PopulatesAndPreservesSelection()
    {
        var id = Guid.NewGuid();
        var candidates = new[] { new LinkedItemOption(id, "Falcon"), new LinkedItemOption(Guid.NewGuid(), "X-Wing") };
        var sut = new LinkedItemFieldEditorViewModel(new LinkedItemFieldDefinition(),
            new LinkedItemFieldValue { TargetItemId = id, TargetDisplay = "Falcon (old label)" }, MakeContext(candidates));

        await sut.LoadCandidatesCommand.ExecuteAsync(null);

        Assert.That(sut.Candidates, Has.Count.EqualTo(2));
        Assert.That(sut.SelectedItem!.Id, Is.EqualTo(id));
        Assert.That(sut.SelectedItem.Display, Is.EqualTo("Falcon"));
    }

    [Test]
    public void GetCurrentValue_PersistsSelectedTarget()
    {
        var id = Guid.NewGuid();
        var sut = new LinkedItemFieldEditorViewModel(new LinkedItemFieldDefinition(),
            new LinkedItemFieldValue(), MakeContext())
        {
            SelectedItem = new LinkedItemOption(id, "Deck A")
        };

        var v = (LinkedItemFieldValue)sut.GetCurrentValue();
        Assert.That(v.TargetItemId, Is.EqualTo(id));
        Assert.That(v.TargetDisplay, Is.EqualTo("Deck A"));
    }

    [Test]
    public void ClearingSelection_PersistsNull()
    {
        var sut = new LinkedItemFieldEditorViewModel(new LinkedItemFieldDefinition(),
            new LinkedItemFieldValue { TargetItemId = Guid.NewGuid(), TargetDisplay = "x" }, MakeContext());

        sut.SelectedItem = null;

        var v = (LinkedItemFieldValue)sut.GetCurrentValue();
        Assert.That(v.TargetItemId, Is.Null);
        Assert.That(v.TargetDisplay, Is.Null);
    }
}

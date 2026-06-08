using FakeItEasy;
using Collectary.Presentation.DI;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class ItemEditingContextFieldMinColumnWidthTest
{
    private static ItemEditingContext Make() => new(
        editorRegistry: A.Fake<IFieldEditorRegistry>(),
        listCellBuilder: A.Fake<IListCellBuilder>(),
        goBack: () => { },
        pickAndStoreImageAsync: () => Task.FromResult<(string, string, Avalonia.Media.Imaging.Bitmap)?>(null),
        exportImageAsync: (_, _) => Task.CompletedTask,
        loadImageBitmap: _ => null,
        deleteImageAsync: _ => Task.CompletedTask);

    [Test]
    public void Above_UsesNarrowMinColumn()
    {
        var ctx = Make();
        ctx.LabelAbove = true;

        Assert.That(ctx.FieldMinColumnWidth, Is.EqualTo(200));
    }

    [Test]
    public void Beside_UsesWiderMinColumn_SoColumnsCollapseSoonerAndDoNotOverlap()
    {
        var ctx = Make();
        ctx.LabelAbove = false;

        Assert.That(ctx.FieldMinColumnWidth, Is.GreaterThan(200));
    }
}

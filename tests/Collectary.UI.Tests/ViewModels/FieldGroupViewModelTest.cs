using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.DI;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class FieldGroupViewModelTest
{
    private static ItemEditingContext MakeContext() => new(
        editorRegistry: A.Fake<IFieldEditorRegistry>(),
        listCellBuilder: A.Fake<IListCellBuilder>(),
        goBack: () => { },
        pickAndStoreImageAsync: () => Task.FromResult<(string, string, Avalonia.Media.Imaging.Bitmap)?>(null),
        exportImageAsync: (_, _) => Task.CompletedTask,
        loadImageBitmap: _ => null,
        deleteImageAsync: _ => Task.CompletedTask);

    [Test]
    public void Context_IsExposedSoTheGroupGridCanBindMinColumnWidth()
    {
        var ctx = MakeContext();
        var group = new FieldGroup { Name = "Specs", DisplayMode = GroupDisplayMode.Card, ColumnCount = 2 };

        var vm = new FieldGroupViewModel(group, ctx);

        Assert.That(vm.Context, Is.SameAs(ctx));
    }
}

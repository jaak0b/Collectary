using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.DI;
using Collectary.Presentation.ViewModels;
using Collectary.Presentation.ViewModels.ListCells;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class ItemRowViewModelTest
{
    private static IListCellBuilder BuilderReturning(IReadOnlyList<ListCellViewModelBase> cells)
    {
        var b = A.Fake<IListCellBuilder>();
        A.CallTo(() => b.Build(A<IReadOnlyList<FieldValue>>._, A<IReadOnlyList<FieldDefinition>>._)).Returns(cells);
        return b;
    }

    [Test]
    public void DisplayName_ReflectsItem()
    {
        var item = new Item { DisplayName = "Widget" };
        var sut = new ItemRowViewModel(item, [], BuilderReturning([]));

        Assert.That(sut.DisplayName, Is.EqualTo("Widget"));
        Assert.That(sut.Item, Is.SameAs(item));
    }

    [Test]
    public void HasListCells_FalseWhenNoCells()
    {
        var sut = new ItemRowViewModel(new Item(), [], BuilderReturning([]));
        Assert.That(sut.HasListCells, Is.False);
    }

    [Test]
    public void HasListCells_TrueWhenCellsPresent()
    {
        var cells = new List<ListCellViewModelBase>
        {
            new TextListCellViewModel(new TextFieldValue { Value = "x" }, new TextFieldDefinition())
        };
        var sut = new ItemRowViewModel(new Item(), [], BuilderReturning(cells));

        Assert.That(sut.HasListCells, Is.True);
        Assert.That(sut.ListCells.Count, Is.EqualTo(1));
    }

    [Test]
    public void Constructor_PassesItemValuesAndFieldsToBuilder()
    {
        var item = new Item { Values = { new TextFieldValue { Value = "v" } } };
        var fields = new List<FieldDefinition> { new TextFieldDefinition() };
        var builder = A.Fake<IListCellBuilder>();
        A.CallTo(() => builder.Build(A<IReadOnlyList<FieldValue>>._, A<IReadOnlyList<FieldDefinition>>._))
            .Returns(new List<ListCellViewModelBase>());

        _ = new ItemRowViewModel(item, fields, builder);

        A.CallTo(() => builder.Build(item.Values, fields)).MustHaveHappenedOnceExactly();
    }
}

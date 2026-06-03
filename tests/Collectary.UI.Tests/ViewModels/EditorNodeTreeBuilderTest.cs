using Collectary.Core.Domain.Fields;
using Collectary.UI.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class EditorNodeTreeBuilderTest
{
    private static FieldDefinitionRowViewModel Field(string label, Guid? group, int order) =>
        new(new TextFieldDefinition { Label = label }) { AssignedGroupId = group, DisplayOrder = order };

    [Test]
    public void Build_PlacesUngroupedFieldsAtRoot()
    {
        var f = Field("Loose", null, 0);
        var tree = new EditorNodeTreeBuilder().Build([], [f]);

        Assert.That(tree, Has.Count.EqualTo(1));
        Assert.That(tree[0], Is.SameAs(f));
    }

    [Test]
    public void Build_NestsFieldUnderItsGroup()
    {
        var group = new FieldGroupRowViewModel("G") { DisplayOrder = 0 };
        var f = Field("Inner", group.Id, 0);

        var tree = new EditorNodeTreeBuilder().Build([group], [f]);

        Assert.That(tree, Has.Count.EqualTo(1));
        Assert.That(tree[0], Is.SameAs(group));
        Assert.That(group.ChildNodes, Has.Count.EqualTo(1));
        Assert.That(group.ChildNodes[0], Is.SameAs(f));
    }

    [Test]
    public void Build_NestsChildGroupUnderParentGroup()
    {
        var parent = new FieldGroupRowViewModel("Parent") { DisplayOrder = 0 };
        var child = new FieldGroupRowViewModel("Child") { ParentGroupId = parent.Id, DisplayOrder = 0 };

        var tree = new EditorNodeTreeBuilder().Build([parent, child], []);

        Assert.That(tree, Has.Count.EqualTo(1));
        Assert.That(tree[0], Is.SameAs(parent));
        Assert.That(parent.ChildNodes, Does.Contain(child));
    }

    [Test]
    public void Build_OrdersRootNodesByDisplayOrder()
    {
        var first = Field("First", null, 0);
        var second = Field("Second", null, 1);

        var tree = new EditorNodeTreeBuilder().Build([], [second, first]);

        Assert.That(tree[0], Is.SameAs(first));
        Assert.That(tree[1], Is.SameAs(second));
    }

    [Test]
    public void Build_OrdersChildrenWithinGroupByDisplayOrder()
    {
        var group = new FieldGroupRowViewModel("G") { DisplayOrder = 0 };
        var a = Field("A", group.Id, 0);
        var b = Field("B", group.Id, 1);

        new EditorNodeTreeBuilder().Build([group], [b, a]);

        Assert.That(group.ChildNodes[0], Is.SameAs(a));
        Assert.That(group.ChildNodes[1], Is.SameAs(b));
    }

    [Test]
    public void Flatten_AssignsSequentialDisplayOrder()
    {
        var a = Field("A", null, 5);
        var b = Field("B", null, 9);

        var flat = new EditorNodeTreeBuilder().Flatten([a, b]);

        Assert.That(a.DisplayOrder, Is.EqualTo(0));
        Assert.That(b.DisplayOrder, Is.EqualTo(1));
        Assert.That(flat.Fields, Is.EqualTo(new[] { a, b }));
    }

    [Test]
    public void Flatten_RootFieldsGetNullScope()
    {
        var a = Field("A", Guid.NewGuid(), 0);

        new EditorNodeTreeBuilder().Flatten([a]);

        Assert.That(a.AssignedGroupId, Is.Null);
    }

    [Test]
    public void Flatten_NestedFieldGetsGroupScope_AndGroupGetsParentScope()
    {
        var group = new FieldGroupRowViewModel("G");
        var nested = Field("Inner", null, 0);
        group.ChildNodes.Add(nested);

        var flat = new EditorNodeTreeBuilder().Flatten([group]);

        Assert.That(group.ParentGroupId, Is.Null);
        Assert.That(nested.AssignedGroupId, Is.EqualTo(group.Id));
        Assert.That(flat.Groups, Does.Contain(group));
        Assert.That(flat.Fields, Does.Contain(nested));
    }

    [Test]
    public void Flatten_NestedChildGroupGetsParentGroupScope()
    {
        var parent = new FieldGroupRowViewModel("P");
        var child = new FieldGroupRowViewModel("C");
        parent.ChildNodes.Add(child);

        new EditorNodeTreeBuilder().Flatten([parent]);

        Assert.That(child.ParentGroupId, Is.EqualTo(parent.Id));
    }
}

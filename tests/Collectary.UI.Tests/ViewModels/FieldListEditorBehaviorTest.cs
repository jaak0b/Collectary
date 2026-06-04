using System.Collections.ObjectModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class FieldListEditorBehaviorTest
{
    private sealed class TestEditor : FieldListEditorViewModel
    {
        public TestEditor(bool supportsGroups = true)
        {
            InitRoot("Root", new ObservableCollection<IEditorNode>(), supportsGroups);
        }

        public Task AddFieldPublic(FieldDefinition def) => AddField(def);
    }

    private static FieldDefinitionRowViewModel AddField(TestEditor sut, string label = "F")
    {
        sut.AddFieldPublic(new TextFieldDefinition { Label = label }).GetAwaiter().GetResult();
        return (FieldDefinitionRowViewModel)sut.CurrentRows.Last(n => n is FieldDefinitionRowViewModel);
    }

    [Test]
    public void AddField_SetsDisplayOrderToPriorCount_AndSelectsNewRow()
    {
        var sut = new TestEditor();

        var first = AddField(sut, "A");
        var second = AddField(sut, "B");

        Assert.That(first.DisplayOrder, Is.EqualTo(0));
        Assert.That(second.DisplayOrder, Is.EqualTo(1));
        Assert.That(sut.SelectedNode, Is.SameAs(second));
    }

    [Test]
    public void AddGroup_AddsGroupAndSelectsIt_WhenGroupsSupported()
    {
        var sut = new TestEditor(supportsGroups: true);

        sut.AddGroupCommand.Execute(null);

        var group = sut.CurrentRows.OfType<FieldGroupRowViewModel>().Single();
        Assert.That(sut.SelectedNode, Is.SameAs(group));
    }

    [Test]
    public void AddGroup_IsNoOp_WhenGroupsUnsupported()
    {
        var sut = new TestEditor(supportsGroups: false);

        sut.AddGroupCommand.Execute(null);

        Assert.That(sut.CurrentRows.OfType<FieldGroupRowViewModel>(), Is.Empty);
    }

    [Test]
    public void MoveField_ReordersCurrentRows()
    {
        var sut = new TestEditor();
        var a = AddField(sut, "A");
        var b = AddField(sut, "B");

        sut.MoveField(0, 1);

        Assert.That(sut.CurrentRows[0], Is.SameAs(b));
        Assert.That(sut.CurrentRows[1], Is.SameAs(a));
    }

    [Test]
    public void DrillIntoGroup_PushesLevelAndShowsGroupChildren()
    {
        var sut = new TestEditor();
        sut.AddGroupCommand.Execute(null);
        var group = sut.CurrentRows.OfType<FieldGroupRowViewModel>().Single();

        sut.DrillIntoCommand.Execute(group);

        Assert.That(sut.IsNested, Is.True);
        Assert.That(sut.Levels.Count, Is.EqualTo(2));
    }

    [Test]
    public void NavigateToLevel_PopsBackToRoot()
    {
        var sut = new TestEditor();
        sut.AddGroupCommand.Execute(null);
        var group = sut.CurrentRows.OfType<FieldGroupRowViewModel>().Single();
        sut.DrillIntoCommand.Execute(group);

        sut.NavigateToLevelCommand.Execute(sut.Levels[0]);

        Assert.That(sut.Levels.Count, Is.EqualTo(1));
        Assert.That(sut.IsNested, Is.False);
        Assert.That(sut.CurrentRows, Does.Contain(group));
    }

    [Test]
    public void DrillInto_NonDrillableNode_DoesNothing()
    {
        var sut = new TestEditor();
        var field = AddField(sut, "A");

        sut.DrillIntoCommand.Execute(field);

        Assert.That(sut.Levels.Count, Is.EqualTo(1));
    }

    [Test]
    public void MovingFieldIntoGroup_SetsAssignedGroupIdAndRemovesFromCurrentRows()
    {
        var sut = new TestEditor();
        sut.AddGroupCommand.Execute(null);
        var group = sut.CurrentRows.OfType<FieldGroupRowViewModel>().Single();
        var field = AddField(sut, "A");

        field.SelectedGroup = group;

        Assert.That(field.AssignedGroupId, Is.EqualTo(group.Id));
        Assert.That(sut.CurrentRows, Does.Not.Contain(field));
        Assert.That(group.ChildNodes, Does.Contain(field));
    }

    [Test]
    public void MovingFieldIntoGatedOffGroup_SuppressesListColumn()
    {
        var sut = new TestEditor();
        sut.AddGroupCommand.Execute(null);
        var group = sut.CurrentRows.OfType<FieldGroupRowViewModel>().Single();
        group.ShowInList = false;
        var field = AddField(sut, "A");

        field.SelectedGroup = group;

        Assert.That(field.ListColumnSuppressed, Is.True);
    }

    [Test]
    public void RemoveGroup_RehomesNestedFieldsAsUngrouped()
    {
        var sut = new TestEditor();
        sut.AddGroupCommand.Execute(null);
        var group = sut.CurrentRows.OfType<FieldGroupRowViewModel>().Single();
        var field = AddField(sut, "A");
        field.SelectedGroup = group;

        sut.RemoveFieldCommand.ExecuteAsync(group).GetAwaiter().GetResult();

        Assert.That(field.AssignedGroupId, Is.Null);
        Assert.That(field.ListColumnSuppressed, Is.False);
        Assert.That(sut.CurrentRows, Does.Contain(field));
        Assert.That(sut.CurrentRows, Does.Not.Contain(group));
    }

    [Test]
    public void RemoveField_DisplayNameField_IsNotRemoved()
    {
        var sut = new TestEditor();
        var dn = new FieldDefinitionRowViewModel(new DisplayNameFieldDefinition());
        sut.CurrentRows.Add(dn);

        sut.RemoveFieldCommand.ExecuteAsync(dn).GetAwaiter().GetResult();

        Assert.That(sut.CurrentRows, Does.Contain(dn));
    }

    [Test]
    public void RemoveField_ClearsSelectionWhenRemovingSelectedNode()
    {
        var sut = new TestEditor();
        var field = AddField(sut, "A");
        sut.SelectedNode = field;

        sut.RemoveFieldCommand.ExecuteAsync(field).GetAwaiter().GetResult();

        Assert.That(sut.SelectedNode, Is.Null);
    }

    [Test]
    public void MoveField_SameIndex_IsNoOp()
    {
        var sut = new TestEditor();
        var a = AddField(sut, "A");
        var b = AddField(sut, "B");

        sut.MoveField(1, 1);

        Assert.That(sut.CurrentRows[0], Is.SameAs(a));
        Assert.That(sut.CurrentRows[1], Is.SameAs(b));
    }

    [Test]
    public void MoveField_NegativeIndex_IsNoOp()
    {
        var sut = new TestEditor();
        var a = AddField(sut, "A");

        Assert.DoesNotThrow(() => sut.MoveField(-1, 0));
        Assert.That(sut.CurrentRows[0], Is.SameAs(a));
    }

    [Test]
    public void DrillIntoListField_PushesLevelShowingItsSubFields()
    {
        var sut = new TestEditor();
        sut.AddFieldPublic(new ListFieldDefinition { Label = "List" }).GetAwaiter().GetResult();
        var list = (FieldDefinitionRowViewModel)sut.CurrentRows.Single(n => n is FieldDefinitionRowViewModel);

        sut.DrillIntoCommand.Execute(list);

        Assert.That(sut.IsNested, Is.True);
        Assert.That(sut.Levels.Count, Is.EqualTo(2));

        AddField(sut, "Sub");
        Assert.That(list.SubFieldRows.OfType<FieldDefinitionRowViewModel>().Any(), Is.True);
    }

    [Test]
    public void SelectedFieldRow_AndSelectedGroupRow_ReflectSelectedNodeType()
    {
        var sut = new TestEditor();
        var field = AddField(sut, "A");
        Assert.That(sut.SelectedFieldRow, Is.SameAs(field));
        Assert.That(sut.SelectedGroupRow, Is.Null);

        sut.AddGroupCommand.Execute(null);
        var group = sut.CurrentRows.OfType<FieldGroupRowViewModel>().Single();
        sut.SelectedNode = group;
        Assert.That(sut.SelectedGroupRow, Is.SameAs(group));
        Assert.That(sut.SelectedFieldRow, Is.Null);
    }
}

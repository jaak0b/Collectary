using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.UI.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class FieldGroupRowViewModelTest
{
    [Test]
    public void NameConstructor_DefaultsToCardAndShowInList()
    {
        var row = new FieldGroupRowViewModel("Specs");
        Assert.That(row.Name, Is.EqualTo("Specs"));
        Assert.That(row.DisplayMode, Is.EqualTo(GroupDisplayMode.Card));
        Assert.That(row.ShowInList, Is.True);
        Assert.That(row.IsGroupNode, Is.True);
        Assert.That(row.IsDrillable, Is.True);
        Assert.That(row.TypeIcon, Is.EqualTo("🗂"));
    }

    [Test]
    public void GroupConstructor_CopiesAllProperties()
    {
        var group = new FieldGroup
        {
            Name = "G",
            DisplayMode = GroupDisplayMode.Tab,
            DefaultCollapsed = true,
            ShowInList = false,
            PrefixColumnHeaders = true,
            ParentGroupId = Guid.NewGuid(),
            DisplayOrder = 3
        };
        var row = new FieldGroupRowViewModel(group);
        Assert.That(row.Id, Is.EqualTo(group.Id));
        Assert.That(row.DisplayMode, Is.EqualTo(GroupDisplayMode.Tab));
        Assert.That(row.DefaultCollapsed, Is.True);
        Assert.That(row.ParentGroupId, Is.EqualTo(group.ParentGroupId));
        Assert.That(row.DisplayOrder, Is.EqualTo(3));
    }

    [Test]
    public void Build_TrimsNameAndAppliesDisplayOrder()
    {
        var row = new FieldGroupRowViewModel("  Specs  ") { DisplayMode = GroupDisplayMode.Tab };
        var built = row.Build(7);
        Assert.That(built.Name, Is.EqualTo("Specs"));
        Assert.That(built.DisplayOrder, Is.EqualTo(7));
        Assert.That(built.DisplayMode, Is.EqualTo(GroupDisplayMode.Tab));
        Assert.That(built.Id, Is.EqualTo(row.Id));
    }

    [Test]
    public void EffectiveListAllowed_RequiresAncestorAndOwnFlag()
    {
        var row = new FieldGroupRowViewModel("G") { ShowInList = true };
        Assert.That(row.EffectiveListAllowed, Is.True);

        row.ApplyListGate(false);
        Assert.That(row.EffectiveListAllowed, Is.False);

        row.ApplyListGate(true);
        row.ShowInList = false;
        Assert.That(row.EffectiveListAllowed, Is.False);
    }

    [Test]
    public void ApplyListGate_SuppressesChildFieldColumns()
    {
        var parent = new FieldGroupRowViewModel("G") { ShowInList = false };
        var field = new FieldDefinitionRowViewModel(new TextFieldDefinition { Label = "F" });
        parent.ChildNodes.Add(field);

        parent.ApplyListGate(true);

        Assert.That(field.ListColumnSuppressed, Is.True);
    }

    [Test]
    public void ApplyListGate_CascadesToNestedGroups()
    {
        var parent = new FieldGroupRowViewModel("P") { ShowInList = false };
        var child = new FieldGroupRowViewModel("C") { ShowInList = true };
        var field = new FieldDefinitionRowViewModel(new TextFieldDefinition { Label = "F" });
        child.ChildNodes.Add(field);
        parent.ChildNodes.Add(child);

        parent.ApplyListGate(true);

        Assert.That(child.EffectiveListAllowed, Is.False);
        Assert.That(field.ListColumnSuppressed, Is.True);
    }
}

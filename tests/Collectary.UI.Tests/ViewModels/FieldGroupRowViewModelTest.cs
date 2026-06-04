using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

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

    [Test]
    public void GroupConstructor_LoadsColumnCount()
    {
        var group = new FieldGroup { Name = "G", ColumnCount = 5 };
        var row = new FieldGroupRowViewModel(group);
        Assert.That(row.ColumnCount, Is.EqualTo(5));
    }

    [Test]
    public void Build_PreservesColumnCount()
    {
        var row = new FieldGroupRowViewModel("G") { ColumnCount = 3 };
        Assert.That(row.Build(0).ColumnCount, Is.EqualTo(3));
    }

    [Test]
    public void OnColumnCountChanged_ClampsChildSpanAboveNewLimit()
    {
        var group = new FieldGroupRowViewModel("G") { ColumnCount = 4 };
        var field = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        field.SetParentColumnCount(4);
        field.ColumnSpan = 4;
        group.ChildNodes.Add(field);

        group.ColumnCount = 2;

        Assert.That(field.ColumnSpan, Is.EqualTo(2));
    }

    [Test]
    public void OnColumnCountChanged_DoesNotReduceSpanWithinNewLimit()
    {
        var group = new FieldGroupRowViewModel("G") { ColumnCount = 4 };
        var field = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        field.SetParentColumnCount(4);
        field.ColumnSpan = 2;
        group.ChildNodes.Add(field);

        group.ColumnCount = 4;

        Assert.That(field.ColumnSpan, Is.EqualTo(2));
    }

    [Test]
    public void OnColumnCountChanged_UpdatesChildColumnSpanOptions()
    {
        var group = new FieldGroupRowViewModel("G") { ColumnCount = 1 };
        var field = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        group.ChildNodes.Add(field);
        sut_SetGroupOnField(field, group);

        group.ColumnCount = 3;

        Assert.That(field.ColumnSpanOptions, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void RefreshChildColumnSpans_SetsCorrectOptionsOnGroupedField()
    {
        var group = new FieldGroupRowViewModel("G") { ColumnCount = 3 };
        var field = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        sut_SetGroupOnField(field, group);
        group.ChildNodes.Add(field);

        group.RefreshChildColumnSpans();

        Assert.That(field.ColumnSpanOptions, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void RefreshChildColumnSpans_ClampsSpanAboveColumnCount()
    {
        var group = new FieldGroupRowViewModel("G") { ColumnCount = 2 };
        var field = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        sut_SetGroupOnField(field, group);
        field.ColumnSpan = 4;
        group.ChildNodes.Add(field);

        group.RefreshChildColumnSpans();

        Assert.That(field.ColumnSpan, Is.EqualTo(2));
    }

    [Test]
    public void RefreshChildColumnSpans_RecursesIntoNestedGroups()
    {
        var parent = new FieldGroupRowViewModel("P") { ColumnCount = 4 };
        var child = new FieldGroupRowViewModel("C") { ColumnCount = 4 };
        var field = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        sut_SetGroupOnField(field, child);
        child.ChildNodes.Add(field);
        parent.ChildNodes.Add(child);

        parent.RefreshChildColumnSpans();

        Assert.That(field.ColumnSpanOptions, Is.EqualTo(new[] { 1, 2, 3, 4 }));
    }

    private static void sut_SetGroupOnField(FieldDefinitionRowViewModel field, FieldGroupRowViewModel group)
    {
        field.AvailableGroups.Add(group);
        field.AssignedGroupId = group.Id;
    }
}

using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;
using Collectary.Presentation.ViewModels.Mapping;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class FieldDefinitionRowViewModelExtraTest
{
    private readonly IFieldEditorMapper _mapper = new TestFieldEditorMapper().Create();

    [Test]
    public void DisplayLabel_NormalField_IsPlainLabel()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition { Label = "Foo" });
        Assert.That(sut.DisplayLabel, Is.EqualTo("Foo"));
    }

    [Test]
    public void DisplayLabel_SystemField_IsLockPrefixed()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition { Label = "Tag" }, isSystemField: true);
        Assert.That(sut.DisplayLabel, Is.EqualTo("🔒 Tag"));
    }

    [Test]
    public void DisplayLabel_DisplayNameField_UsesLocalizedTypeName()
    {
        var sut = new FieldDefinitionRowViewModel(new DisplayNameFieldDefinition());
        Assert.That(sut.DisplayLabel, Is.EqualTo(sut.TypeDisplayName));
    }

    [Test]
    public void Constructor_MapsGroupIdToAssignedGroupId()
    {
        var gid = Guid.NewGuid();
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition { GroupId = gid });
        Assert.That(sut.AssignedGroupId, Is.EqualTo(gid));
    }

    [Test]
    public void Constructor_MapsListInlineStyle()
    {
        var sut = new FieldDefinitionRowViewModel(new ListFieldDefinition { InlineStyle = ListInlineStyle.Grid });
        Assert.That(sut.InlineStyle, Is.EqualTo(ListInlineStyle.Grid));
    }

    [Test]
    public void Constructor_MapsImageDimensionsAndSizeMode()
    {
        var def = new ImageFieldDefinition { DisplayWidth = 321, DisplayHeight = 123, SizeMode = ImageSizeMode.Min };
        var sut = new FieldDefinitionRowViewModel(def);
        Assert.That(sut.DisplayWidth, Is.EqualTo(321));
        Assert.That(sut.DisplayHeight, Is.EqualTo(123));
        Assert.That(sut.SizeMode, Is.EqualTo(ImageSizeMode.Min));
    }

    [Test]
    public void Constructor_MapsCurrencySymbol()
    {
        var sut = new FieldDefinitionRowViewModel(new CurrencyFieldDefinition { CurrencySymbol = "£" });
        Assert.That(sut.CurrencySymbol, Is.EqualTo("£"));
    }

    [Test]
    public void IsGridInline_TrueOnlyForGridList()
    {
        Assert.That(new FieldDefinitionRowViewModel(new ListFieldDefinition { InlineStyle = ListInlineStyle.Grid }).IsGridInline, Is.True);
        Assert.That(new FieldDefinitionRowViewModel(new ListFieldDefinition { InlineStyle = ListInlineStyle.Card }).IsGridInline, Is.False);
        Assert.That(new FieldDefinitionRowViewModel(new TextFieldDefinition()).IsGridInline, Is.False);
    }

    [Test]
    public void BuildDefinition_NormalField_AssignsAssignedGroupId()
    {
        var gid = Guid.NewGuid();
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition()) { AssignedGroupId = gid };
        Assert.That(_mapper.ToDefinition(sut).GroupId, Is.EqualTo(gid));
    }

    [Test]
    public void BuildDefinition_DisplayNameField_ForcesNullGroupId()
    {
        var sut = new FieldDefinitionRowViewModel(new DisplayNameFieldDefinition()) { AssignedGroupId = Guid.NewGuid() };
        Assert.That(_mapper.ToDefinition(sut).GroupId, Is.Null);
    }

    [Test]
    public void BuildDefinition_DisplayNameField_DoesNotOverwriteLabel()
    {
        var def = new DisplayNameFieldDefinition();
        var original = def.Label;
        var sut = new FieldDefinitionRowViewModel(def) { Label = "changed" };

        var result = _mapper.ToDefinition(sut);

        Assert.That(result.Label, Is.EqualTo(original));
        Assert.That(result.Label, Is.Not.EqualTo("changed"));
    }

    [Test]
    public void BuildDefinition_ListField_StampsParentIdOnGroupsAndSubFields()
    {
        var group = new FieldGroup { Name = "G", DisplayOrder = 0 };
        var sub = new TextFieldDefinition { Label = "S", DisplayOrder = 0 };
        var def = new ListFieldDefinition { Groups = [group], SubFields = [sub] };
        var sut = new FieldDefinitionRowViewModel(def);

        var result = (ListFieldDefinition)_mapper.ToDefinition(sut);

        Assert.That(result.Groups[0].ParentListFieldDefinitionId, Is.EqualTo(result.Id));
        Assert.That(result.SubFields[0].ParentListFieldDefinitionId, Is.EqualTo(result.Id));
    }

    [Test]
    public void SelectedGroup_Getter_ResolvesByAssignedGroupId()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        var g1 = new FieldGroupRowViewModel("G1");
        var g2 = new FieldGroupRowViewModel("G2");
        sut.AvailableGroups.Add(g1);
        sut.AvailableGroups.Add(g2);
        sut.AssignedGroupId = g2.Id;

        Assert.That(sut.SelectedGroup, Is.SameAs(g2));
    }

    [Test]
    public void LabelChange_RaisesDisplayLabelNotification()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition { Label = "a" });
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.Label = "b";

        Assert.That(raised, Does.Contain(nameof(sut.Label)));
        Assert.That(raised, Does.Contain(nameof(sut.DisplayLabel)));
    }

    [Test]
    public void AssignedGroupIdChange_RaisesSelectedGroupNotification()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.AssignedGroupId = Guid.NewGuid();

        Assert.That(raised, Does.Contain(nameof(sut.SelectedGroup)));
    }

    [Test]
    public void ListColumnSuppressedChange_RaisesShowInListCheckboxVisibleNotification()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition { ShowInList = true });
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.ListColumnSuppressed = true;

        Assert.That(raised, Does.Contain(nameof(sut.ShowInListCheckboxVisible)));
    }

    [Test]
    public void InlineStyleChange_RaisesIsGridInlineNotification()
    {
        var sut = new FieldDefinitionRowViewModel(new ListFieldDefinition { InlineStyle = ListInlineStyle.Card });
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.InlineStyle = ListInlineStyle.Grid;

        Assert.That(raised, Does.Contain(nameof(sut.IsGridInline)));
    }

    [Test]
    public void SelectedGroup_NoMatchingGroup_ReturnsNull()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        sut.AvailableGroups.Add(new FieldGroupRowViewModel("G"));
        sut.AssignedGroupId = Guid.NewGuid();

        Assert.That(sut.SelectedGroup, Is.Null);
    }

    [Test]
    public void Constructor_NonCurrencyField_CurrencySymbolDefaultsToEuro()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        Assert.That(sut.CurrencySymbol, Is.EqualTo("€"));
    }

    [Test]
    public void Constructor_LoadsChoicesOrderedByDisplayOrder()
    {
        var def = new SingleChoiceFieldDefinition
        {
            Choices =
            [
                new ChoiceOption { Value = "second", DisplayOrder = 1 },
                new ChoiceOption { Value = "first", DisplayOrder = 0 }
            ]
        };

        var sut = new FieldDefinitionRowViewModel(def);

        Assert.That(sut.ChoiceItems.Select(c => c.Value), Is.EqualTo(new[] { "first", "second" }));
    }

    [Test]
    public void ClearGroup_WhenAlreadyUngrouped_DoesNotInvokeMove()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        var moveCalled = false;
        sut.GroupMoveRequested = (_, _) => moveCalled = true;
        sut.AssignedGroupId = null;

        sut.ClearGroupCommand.Execute(null);

        Assert.That(moveCalled, Is.False);
    }

    [Test]
    public void ClearGroup_WhenGrouped_InvokesMoveToNull()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition()) { AssignedGroupId = Guid.NewGuid() };
        FieldGroupRowViewModel? moveTarget = null;
        var moveCalled = false;
        sut.GroupMoveRequested = (_, target) => { moveCalled = true; moveTarget = target; };

        sut.ClearGroupCommand.Execute(null);

        Assert.That(moveCalled, Is.True);
        Assert.That(moveTarget, Is.Null);
    }

    [Test]
    public void BuildDefinition_PreservesChoiceDisplayOrderByPosition()
    {
        var def = new SingleChoiceFieldDefinition();
        var sut = new FieldDefinitionRowViewModel(def);
        sut.ChoiceItems.Add(new ChoiceOptionRowViewModel("first"));
        sut.ChoiceItems.Add(new ChoiceOptionRowViewModel("second"));

        var result = (SingleChoiceFieldDefinition)_mapper.ToDefinition(sut);

        Assert.That(result.Choices[0].DisplayOrder, Is.EqualTo(0));
        Assert.That(result.Choices[1].DisplayOrder, Is.EqualTo(1));
        Assert.That(result.Choices[1].Value, Is.EqualTo("second"));
    }

    [Test]
    public void ColumnSpanOptions_DefaultParentCount1_ReturnsOnly1()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        Assert.That(sut.ColumnSpanOptions, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void ColumnSpanOptions_AfterSetParentColumnCount3_Returns1To3()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        sut.SetParentColumnCount(3);
        Assert.That(sut.ColumnSpanOptions, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void SetParentColumnCount_ClampsExistingSpanAboveNewLimit()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition()) { ColumnSpan = 1 };
        sut.SetParentColumnCount(4);
        sut.ColumnSpan = 4;
        sut.SetParentColumnCount(2);
        Assert.That(sut.ColumnSpan, Is.EqualTo(2));
    }

    [Test]
    public void SetParentColumnCount_DoesNotReduceSpanWithinNewLimit()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        sut.SetParentColumnCount(3);
        sut.ColumnSpan = 2;
        sut.SetParentColumnCount(4);
        Assert.That(sut.ColumnSpan, Is.EqualTo(2));
    }

    [Test]
    public void IsInMultiColumnContext_FalseWhenParentCount1()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        Assert.That(sut.IsInMultiColumnContext, Is.False);
    }

    [Test]
    public void IsInMultiColumnContext_TrueAfterSetParentColumnCount2()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        sut.SetParentColumnCount(2);
        Assert.That(sut.IsInMultiColumnContext, Is.True);
    }

    [Test]
    public void IsInMultiColumnContext_TrueWhenAssignedGroupHasMultipleColumns()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        var group = new FieldGroupRowViewModel(new FieldGroup { ColumnCount = 3 });
        sut.AvailableGroups.Add(group);
        sut.AssignedGroupId = group.Id;
        Assert.That(sut.IsInMultiColumnContext, Is.True);
    }

    [Test]
    public void ColumnSpanOptions_GroupColumnCountTakesPriorityOverParent()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        sut.SetParentColumnCount(1);
        var group = new FieldGroupRowViewModel(new FieldGroup { ColumnCount = 4 });
        sut.AvailableGroups.Add(group);
        sut.AssignedGroupId = group.Id;
        Assert.That(sut.ColumnSpanOptions, Is.EqualTo(new[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void BuildDefinition_WritesListColumnCountToDefinition()
    {
        var def = new ListFieldDefinition();
        var sut = new FieldDefinitionRowViewModel(def) { ColumnCount = 3 };
        var result = (ListFieldDefinition)_mapper.ToDefinition(sut);
        Assert.That(result.ColumnCount, Is.EqualTo(3));
    }

    [Test]
    public void Constructor_LoadsListColumnCountFromDefinition()
    {
        var def = new ListFieldDefinition { ColumnCount = 4 };
        var sut = new FieldDefinitionRowViewModel(def);
        Assert.That(sut.ColumnCount, Is.EqualTo(4));
    }
}

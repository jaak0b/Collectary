using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.UI.Services;
using Collectary.UI.ViewModels;
using Collectary.UI.ViewModels.SystemFields;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class FieldListEditorViewModelTest
{
    private ISystemFieldUseCase _useCase = null!;
    private IDialogService _dialogService = null!;
    private SystemFieldLibraryViewModel _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _useCase = A.Fake<ISystemFieldUseCase>();
        _dialogService = A.Fake<IDialogService>();
        _sut = new SystemFieldLibraryViewModel(_useCase, _dialogService, onDone: () => { });
    }

    [Test]
    public async Task AddTextFieldCommand_AddsTextRowToCurrentRows()
    {
        await _sut.AddTextFieldCommand.ExecuteAsync(null);

        Assert.That(_sut.CurrentRows.Count, Is.EqualTo(1));
        Assert.That(((FieldDefinitionRowViewModel)_sut.CurrentRows[0]).IsList, Is.False);
    }

    [Test]
    public async Task AddBoolFieldCommand_AddsBoolRow()
    {
        await _sut.AddBoolFieldCommand.ExecuteAsync(null);

        Assert.That(_sut.CurrentRows.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task AddImageFieldCommand_AddsImageRow()
    {
        await _sut.AddImageFieldCommand.ExecuteAsync(null);

        Assert.That(_sut.CurrentRows.Count, Is.EqualTo(1));
        Assert.That(((FieldDefinitionRowViewModel)_sut.CurrentRows[0]).IsPicture, Is.True);
    }

    [Test]
    public async Task AddListFieldCommand_AddsListRow()
    {
        await _sut.AddListFieldCommand.ExecuteAsync(null);

        Assert.That(_sut.CurrentRows.Count, Is.EqualTo(1));
        Assert.That(((FieldDefinitionRowViewModel)_sut.CurrentRows[0]).IsList, Is.True);
    }

    [Test]
    public async Task MultipleAdds_EachIncreasesCount()
    {
        await _sut.AddTextFieldCommand.ExecuteAsync(null);
        await _sut.AddBoolFieldCommand.ExecuteAsync(null);
        await _sut.AddIntegerFieldCommand.ExecuteAsync(null);

        Assert.That(_sut.CurrentRows.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task AddTextField_SetsSelectedField()
    {
        await _sut.AddTextFieldCommand.ExecuteAsync(null);

        Assert.That(_sut.SelectedNode, Is.SameAs(_sut.CurrentRows[0]));
    }

    [Test]
    public async Task RemoveFieldCommand_RemovesSelectedRow()
    {
        await _sut.AddTextFieldCommand.ExecuteAsync(null);
        var row = _sut.CurrentRows[0];

        await _sut.RemoveFieldCommand.ExecuteAsync(row);

        Assert.That(_sut.CurrentRows, Does.Not.Contain(row));
    }

    [Test]
    public async Task RemoveFieldCommand_ClearsSelectedFieldWhenRemovingSelected()
    {
        await _sut.AddTextFieldCommand.ExecuteAsync(null);
        var row = _sut.CurrentRows[0];
        _sut.SelectedNode = row;

        await _sut.RemoveFieldCommand.ExecuteAsync(row);

        Assert.That(_sut.SelectedNode, Is.Null);
    }

    [Test]
    public async Task RemoveFieldCommand_DoesNotRemoveDisplayNameField()
    {
        var dnDef = new DisplayNameFieldDefinition { IsRequired = true };
        var dnRow = new FieldDefinitionRowViewModel(dnDef);
        _sut.CurrentRows.Add(dnRow);

        await _sut.RemoveFieldCommand.ExecuteAsync(dnRow);

        Assert.That(_sut.CurrentRows, Does.Contain(dnRow));
    }

    [Test]
    public async Task DrillIntoCommand_PushesNewLevel()
    {
        await _sut.AddListFieldCommand.ExecuteAsync(null);
        var listRow = _sut.CurrentRows[0];
        var levelsBefore = _sut.Levels.Count;

        _sut.DrillIntoCommand.Execute(listRow);

        Assert.That(_sut.Levels.Count, Is.EqualTo(levelsBefore + 1));
    }

    [Test]
    public async Task DrillIntoCommand_SwitchesCurrentRowsToSubFields()
    {
        await _sut.AddListFieldCommand.ExecuteAsync(null);
        var listRow = (FieldDefinitionRowViewModel)_sut.CurrentRows[0];

        _sut.DrillIntoCommand.Execute(listRow);
        await _sut.AddTextFieldCommand.ExecuteAsync(null);

        Assert.That(listRow.DrillChildren, Has.Count.EqualTo(1));
        Assert.That(_sut.CurrentRows, Is.EqualTo(listRow.DrillChildren));
    }

    [Test]
    public async Task DrillIntoCommand_SetsIsNested()
    {
        await _sut.AddListFieldCommand.ExecuteAsync(null);

        _sut.DrillIntoCommand.Execute(_sut.CurrentRows[0]);

        Assert.That(_sut.IsNested, Is.True);
    }

    [Test]
    public async Task NavigateToLevelCommand_PopsToTargetLevel()
    {
        await _sut.AddListFieldCommand.ExecuteAsync(null);
        var rootLevel = _sut.Levels[0];
        _sut.DrillIntoCommand.Execute(_sut.CurrentRows[0]);

        _sut.NavigateToLevelCommand.Execute(rootLevel);

        Assert.That(_sut.Levels.Count, Is.EqualTo(1));
        Assert.That(_sut.IsNested, Is.False);
    }

    [Test]
    public async Task NavigateToLevelCommand_RestoresCurrentRows()
    {
        await _sut.AddListFieldCommand.ExecuteAsync(null);
        var rootLevel = _sut.Levels[0];
        var listRow = _sut.CurrentRows[0];
        _sut.DrillIntoCommand.Execute(_sut.CurrentRows[0]);

        _sut.NavigateToLevelCommand.Execute(rootLevel);

        Assert.That(_sut.CurrentRows, Does.Contain(listRow));
    }

    [Test]
    public async Task DrillIntoCommand_IgnoresNonListRows()
    {
        await _sut.AddTextFieldCommand.ExecuteAsync(null);
        var textRow = _sut.CurrentRows[0];
        var levelsBefore = _sut.Levels.Count;

        _sut.DrillIntoCommand.Execute(textRow);

        Assert.That(_sut.Levels.Count, Is.EqualTo(levelsBefore));
    }

    [Test]
    public void MoveField_SwapsRowPositions()
    {
        var rowA = new FieldDefinitionRowViewModel(new TextFieldDefinition { Label = "A" });
        var rowB = new FieldDefinitionRowViewModel(new TextFieldDefinition { Label = "B" });
        _sut.CurrentRows.Add(rowA);
        _sut.CurrentRows.Add(rowB);

        _sut.MoveField(0, 1);

        Assert.That(((FieldDefinitionRowViewModel)_sut.CurrentRows[0]).Label, Is.EqualTo("B"));
        Assert.That(((FieldDefinitionRowViewModel)_sut.CurrentRows[1]).Label, Is.EqualTo("A"));
    }

    [Test]
    public void MoveField_WithOutOfRangeIndex_DoesNotThrow()
    {
        _sut.CurrentRows.Add(new FieldDefinitionRowViewModel(new TextFieldDefinition()));

        Assert.DoesNotThrow(() => _sut.MoveField(0, 5));
    }

    [Test]
    public async Task SelectedFieldRow_ReturnsFieldWhenSelectedNodeIsField()
    {
        await _sut.AddTextFieldCommand.ExecuteAsync(null);
        var field = _sut.CurrentRows[0];
        _sut.SelectedNode = field;

        Assert.That(_sut.SelectedFieldRow, Is.SameAs(field));
        Assert.That(_sut.SelectedGroupRow, Is.Null);
    }

    [Test]
    public void SelectedGroupRow_ReturnsGroupWhenSelectedNodeIsGroup()
    {
        var group = new FieldGroupRowViewModel("Test");
        _sut.CurrentRows.Add(group);
        _sut.SelectedNode = group;

        Assert.That(_sut.SelectedGroupRow, Is.SameAs(group));
        Assert.That(_sut.SelectedFieldRow, Is.Null);
    }

    [Test]
    public async Task RemoveFieldCommand_OnDrilledNonGroupField_RemovesFromCurrentRows()
    {
        await _sut.AddListFieldCommand.ExecuteAsync(null);
        _sut.DrillIntoCommand.Execute(_sut.CurrentRows[0]);
        await _sut.AddTextFieldCommand.ExecuteAsync(null);
        var field = _sut.CurrentRows[0];

        await _sut.RemoveFieldCommand.ExecuteAsync(field);

        Assert.That(_sut.CurrentRows, Does.Not.Contain(field));
    }

    [Test]
    public async Task CurrentRows_Replace_MirroredToBacking()
    {
        await _sut.AddListFieldCommand.ExecuteAsync(null);
        _sut.DrillIntoCommand.Execute(_sut.CurrentRows[0]);
        await _sut.AddTextFieldCommand.ExecuteAsync(null);

        var replacement = new FieldDefinitionRowViewModel(new TextFieldDefinition { Label = "R" });
        _sut.CurrentRows[0] = replacement;

        Assert.That(_sut.CurrentRows[0], Is.SameAs(replacement));
    }

    [Test]
    public async Task CurrentRows_Clear_MirroredToBacking()
    {
        await _sut.AddListFieldCommand.ExecuteAsync(null);
        _sut.DrillIntoCommand.Execute(_sut.CurrentRows[0]);
        await _sut.AddTextFieldCommand.ExecuteAsync(null);

        _sut.CurrentRows.Clear();

        Assert.That(_sut.CurrentRows, Is.Empty);
    }

    [Test]
    public async Task MovingSelectedFieldIntoGroup_ClearsSelectionAndRemovesFromCurrentRows()
    {
        await _sut.AddListFieldCommand.ExecuteAsync(null);
        _sut.DrillIntoCommand.Execute(_sut.CurrentRows[0]);
        _sut.AddGroupCommand.Execute(null);
        var group = (FieldGroupRowViewModel)_sut.CurrentRows[0];
        await _sut.AddTextFieldCommand.ExecuteAsync(null);
        var field = (FieldDefinitionRowViewModel)_sut.CurrentRows.Last(n => n is FieldDefinitionRowViewModel);
        _sut.SelectedNode = field;

        field.SelectedGroup = group;

        Assert.That(_sut.SelectedNode, Is.Null);
        Assert.That(_sut.CurrentRows, Does.Not.Contain(field));
        Assert.That(group.ChildNodes, Does.Contain(field));
    }

    [Test]
    public async Task SettingSelectedGroupToNull_DoesNotEjectFieldFromGroup()
    {
        await _sut.AddListFieldCommand.ExecuteAsync(null);
        _sut.DrillIntoCommand.Execute(_sut.CurrentRows[0]);
        _sut.AddGroupCommand.Execute(null);
        var group = (FieldGroupRowViewModel)_sut.CurrentRows[0];
        _sut.DrillIntoCommand.Execute(group);
        await _sut.AddTextFieldCommand.ExecuteAsync(null);
        var field = (FieldDefinitionRowViewModel)_sut.CurrentRows.Last(n => n is FieldDefinitionRowViewModel);

        field.SelectedGroup = null;

        Assert.That(field.AssignedGroupId, Is.EqualTo(group.Id));
        Assert.That(group.ChildNodes, Does.Contain(field));
    }

    [Test]
    public async Task ClearGroupCommand_EjectsFieldFromGroup()
    {
        await _sut.AddListFieldCommand.ExecuteAsync(null);
        _sut.DrillIntoCommand.Execute(_sut.CurrentRows[0]);
        _sut.AddGroupCommand.Execute(null);
        var group = (FieldGroupRowViewModel)_sut.CurrentRows[0];
        _sut.DrillIntoCommand.Execute(group);
        await _sut.AddTextFieldCommand.ExecuteAsync(null);
        var field = (FieldDefinitionRowViewModel)_sut.CurrentRows.Last(n => n is FieldDefinitionRowViewModel);

        field.ClearGroupCommand.Execute(null);

        Assert.That(field.AssignedGroupId, Is.Null);
        Assert.That(group.ChildNodes, Does.Not.Contain(field));
    }
}

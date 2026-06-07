using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Localization;
using Collectary.Presentation.ViewModels;
using Collectary.Presentation.ViewModels.Mapping;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class FieldDefinitionRowViewModelTest
{
    private readonly IFieldEditorMapper _mapper = new TestFieldEditorMapper().Create();

    [TearDown]
    public void ResetLanguage() => LocalizationService.Instance.Apply("en");

    [Test]
    public void IsDragging_TogglesAndRaisesPropertyChanged()
    {
        var row = new FieldDefinitionRowViewModel(new TextFieldDefinition { Label = "L" });
        Assert.That(((IDraggableRow)row).IsDragging, Is.False);
        var raised = false;
        row.PropertyChanged += (_, e) => raised |= e.PropertyName == nameof(FieldDefinitionRowViewModel.IsDragging);

        ((IDraggableRow)row).IsDragging = true;

        Assert.That(row.IsDragging, Is.True);
        Assert.That(raised, Is.True);
    }

    [Test]
    public void Constructor_LoadsLabelFromDefinition()
    {
        var def = new TextFieldDefinition { Label = "My Label" };
        var sut = new FieldDefinitionRowViewModel(def);

        Assert.That(sut.Label, Is.EqualTo("My Label"));
    }

    [Test]
    public void Constructor_LoadsIsRequiredFromDefinition()
    {
        var def = new TextFieldDefinition { IsRequired = true };
        var sut = new FieldDefinitionRowViewModel(def);

        Assert.That(sut.IsRequired, Is.True);
    }

    [Test]
    public void Constructor_LoadsShowInListFromDefinition()
    {
        var def = new TextFieldDefinition { ShowInList = true };
        var sut = new FieldDefinitionRowViewModel(def);

        Assert.That(sut.ShowInList, Is.True);
    }

    [Test]
    public void Constructor_LoadsChoicesFromSingleChoiceDefinition()
    {
        var def = new SingleChoiceFieldDefinition
        {
            Choices = [new ChoiceOption { Value = "Red" }, new ChoiceOption { Value = "Blue" }]
        };
        var sut = new FieldDefinitionRowViewModel(def);

        Assert.That(sut.ChoiceItems.Select(c => c.Value), Is.EqualTo(new[] { "Red", "Blue" }));
    }

    [Test]
    public void Constructor_LoadsSubFieldsFromListDefinition()
    {
        var sub = new TextFieldDefinition { Label = "Sub", DisplayOrder = 0 };
        var def = new ListFieldDefinition { SubFields = [sub] };
        var sut = new FieldDefinitionRowViewModel(def);

        Assert.That(sut.SubFieldRows.Count, Is.EqualTo(1));
        Assert.That(((FieldDefinitionRowViewModel)sut.SubFieldRows[0]).Label, Is.EqualTo("Sub"));
    }

    [Test]
    public void Constructor_LoadsColorFormatFromColorDefinition()
    {
        var def = new ColorFieldDefinition { Format = ColorFormat.Rgb };
        var sut = new FieldDefinitionRowViewModel(def);

        Assert.That(sut.Format, Is.EqualTo(ColorFormat.Rgb));
    }

    [Test]
    public void Constructor_LoadsImageSizeModeFromImageDefinition()
    {
        var def = new ImageFieldDefinition { SizeMode = ImageSizeMode.Min };
        var sut = new FieldDefinitionRowViewModel(def);

        Assert.That(sut.SizeMode, Is.EqualTo(ImageSizeMode.Min));
    }

    [Test]
    public void IsSharedField_FalseByDefault()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());

        Assert.That(sut.IsSharedField, Is.False);
    }

    [Test]
    public void IsSharedField_TrueWhenPassedTrue()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition(), isSharedField: true);

        Assert.That(sut.IsSharedField, Is.True);
    }

    [Test]
    public void AddChoiceCommand_AddsEmptyOption()
    {
        var sut = new FieldDefinitionRowViewModel(new SingleChoiceFieldDefinition());

        sut.AddChoiceCommand.Execute(null);

        Assert.That(sut.ChoiceItems.Count, Is.EqualTo(1));
    }

    [Test]
    public void RemoveChoiceCommand_RemovesOption()
    {
        var def = new SingleChoiceFieldDefinition { Choices = [new ChoiceOption { Value = "X" }] };
        var sut = new FieldDefinitionRowViewModel(def);
        var item = sut.ChoiceItems[0];

        sut.RemoveChoiceCommand.Execute(item);

        Assert.That(sut.ChoiceItems, Is.Empty);
    }

    [Test]
    public void BuildDefinition_Text_PreservesLabel()
    {
        var def = new TextFieldDefinition { Label = "Original" };
        var sut = new FieldDefinitionRowViewModel(def);
        sut.Label = "Updated";

        var result = _mapper.ToDefinition(sut);

        Assert.That(result.Label, Is.EqualTo("Updated"));
    }

    [Test]
    public void BuildDefinition_PreservesIsRequired()
    {
        var def = new TextFieldDefinition { IsRequired = false };
        var sut = new FieldDefinitionRowViewModel(def);
        sut.IsRequired = true;

        var result = _mapper.ToDefinition(sut);

        Assert.That(result.IsRequired, Is.True);
    }

    [Test]
    public void BuildDefinition_PreservesShowInList()
    {
        var def = new TextFieldDefinition { ShowInList = false };
        var sut = new FieldDefinitionRowViewModel(def);
        sut.ShowInList = true;

        var result = (TextFieldDefinition)_mapper.ToDefinition(sut);

        Assert.That(result.ShowInList, Is.True);
    }

    [Test]
    public void BuildDefinition_SingleChoice_PreservesOptions()
    {
        var def = new SingleChoiceFieldDefinition();
        var sut = new FieldDefinitionRowViewModel(def);
        sut.AddChoiceCommand.Execute(null);
        sut.ChoiceItems[0].Value = "Red";
        sut.AddChoiceCommand.Execute(null);
        sut.ChoiceItems[1].Value = "Blue";

        var result = (SingleChoiceFieldDefinition)_mapper.ToDefinition(sut);

        Assert.That(result.Choices.Select(c => c.Value), Is.EqualTo(new[] { "Red", "Blue" }));
    }

    [Test]
    public void BuildDefinition_SingleChoice_SetsDisplayOrderByPosition()
    {
        var def = new SingleChoiceFieldDefinition();
        var sut = new FieldDefinitionRowViewModel(def);
        sut.AddChoiceCommand.Execute(null);
        sut.AddChoiceCommand.Execute(null);

        var result = (SingleChoiceFieldDefinition)_mapper.ToDefinition(sut);

        Assert.That(result.Choices[0].DisplayOrder, Is.EqualTo(0));
        Assert.That(result.Choices[1].DisplayOrder, Is.EqualTo(1));
    }

    [Test]
    public void BuildDefinition_Image_PreservesDisplayDimensions()
    {
        var def = new ImageFieldDefinition();
        var sut = new FieldDefinitionRowViewModel(def);
        sut.DisplayWidth = 300;
        sut.DisplayHeight = 150;

        var result = (ImageFieldDefinition)_mapper.ToDefinition(sut);

        Assert.That(result.DisplayWidth, Is.EqualTo(300));
        Assert.That(result.DisplayHeight, Is.EqualTo(150));
    }

    [Test]
    public void BuildDefinition_Image_PreservesSizeMode()
    {
        var def = new ImageFieldDefinition { SizeMode = ImageSizeMode.Fixed };
        var sut = new FieldDefinitionRowViewModel(def);
        sut.SizeMode = ImageSizeMode.Min;

        var result = (ImageFieldDefinition)_mapper.ToDefinition(sut);

        Assert.That(result.SizeMode, Is.EqualTo(ImageSizeMode.Min));
    }

    [Test]
    public void BuildDefinition_List_PreservesInlineStyle()
    {
        var def = new ListFieldDefinition { InlineStyle = ListInlineStyle.Card };
        var sut = new FieldDefinitionRowViewModel(def);
        sut.InlineStyle = ListInlineStyle.Grid;

        var result = (ListFieldDefinition)_mapper.ToDefinition(sut);

        Assert.That(result.InlineStyle, Is.EqualTo(ListInlineStyle.Grid));
    }

    [Test]
    public void BuildDefinition_List_PreservesSubFieldsWithDisplayOrder()
    {
        var sub1 = new TextFieldDefinition { Label = "S1", DisplayOrder = 0 };
        var sub2 = new TextFieldDefinition { Label = "S2", DisplayOrder = 1 };
        var def = new ListFieldDefinition { SubFields = [sub1, sub2] };
        var sut = new FieldDefinitionRowViewModel(def);

        var result = (ListFieldDefinition)_mapper.ToDefinition(sut);

        Assert.That(result.SubFields.Count, Is.EqualTo(2));
        Assert.That(result.SubFields[0].DisplayOrder, Is.EqualTo(0));
        Assert.That(result.SubFields[1].DisplayOrder, Is.EqualTo(1));
    }

    [Test]
    public void BuildDefinition_WhenSharedField_ReturnsOriginalDefinitionUnchanged()
    {
        var def = new TextFieldDefinition { Label = "System" };
        var sut = new FieldDefinitionRowViewModel(def, isSharedField: true);
        sut.Label = "Changed";

        var result = _mapper.ToDefinition(sut);

        Assert.That(result.Label, Is.EqualTo("System"));
    }

    [Test]
    public void BuildDefinition_Color_PreservesColorFormat()
    {
        var def = new ColorFieldDefinition { Format = ColorFormat.Hex };
        var sut = new FieldDefinitionRowViewModel(def);
        sut.Format = ColorFormat.Rgb;

        var result = (ColorFieldDefinition)_mapper.ToDefinition(sut);

        Assert.That(result.Format, Is.EqualTo(ColorFormat.Rgb));
    }

    [Test]
    public void IsList_TrueForListDefinition()
    {
        var sut = new FieldDefinitionRowViewModel(new ListFieldDefinition());

        Assert.That(sut.IsList, Is.True);
    }

    [Test]
    public void IsList_FalseForTextDefinition()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());

        Assert.That(sut.IsList, Is.False);
    }

    [Test]
    public void CanDelete_FalseForDisplayNameField()
    {
        var sut = new FieldDefinitionRowViewModel(new DisplayNameFieldDefinition());

        Assert.That(sut.CanDelete, Is.False);
    }

    [Test]
    public void CanDelete_TrueForNonDisplayNameField()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());

        Assert.That(sut.CanDelete, Is.True);
    }

    [Test]
    public void IsEditable_TrueWhenNotSharedField()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition(), isSharedField: false);
        Assert.That(sut.IsEditable, Is.True);
    }

    [Test]
    public void IsEditable_FalseWhenSharedField()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition(), isSharedField: true);
        Assert.That(sut.IsEditable, Is.False);
    }

    [Test]
    public void ShowInListCheckboxVisible_TrueWhenCanShowInListAndNotSuppressed()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition { ShowInList = true });
        sut.ListColumnSuppressed = false;

        Assert.That(sut.ShowInListCheckboxVisible, Is.True);
    }

    [Test]
    public void ShowInListCheckboxVisible_FalseWhenSuppressed()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        sut.ListColumnSuppressed = true;

        Assert.That(sut.ShowInListCheckboxVisible, Is.False);
    }

    [Test]
    public void HasAvailableGroups_FalseWhenNoGroups()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        Assert.That(sut.HasAvailableGroups, Is.False);
    }

    [Test]
    public void HasAvailableGroups_TrueWhenGroupAdded()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        sut.AvailableGroups.Add(new FieldGroupRowViewModel("G"));

        Assert.That(sut.HasAvailableGroups, Is.True);
    }

    [Test]
    public void TypeDisplayName_ReturnsLocalizedString()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        Assert.That(sut.TypeDisplayName, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void TypeIcon_ReturnsNonEmptyString()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        Assert.That(sut.TypeIcon, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void SubFieldCount_ReflectsSubFieldRowsCount()
    {
        var def = new ListFieldDefinition { SubFields = [new TextFieldDefinition { Label = "S", DisplayOrder = 0 }] };
        var sut = new FieldDefinitionRowViewModel(def);

        Assert.That(sut.SubFieldCount, Is.EqualTo(1));
    }

    [Test]
    public void CanShowInList_TrueForTextDefinition()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        Assert.That(sut.CanShowInList, Is.True);
    }

    [Test]
    public void HasChoices_TrueForSingleChoiceDefinition()
    {
        var sut = new FieldDefinitionRowViewModel(new SingleChoiceFieldDefinition());
        Assert.That(sut.HasChoices, Is.True);
    }

    [Test]
    public void HasChoices_TrueForMultiChoiceDefinition()
    {
        var sut = new FieldDefinitionRowViewModel(new MultiChoiceFieldDefinition());
        Assert.That(sut.HasChoices, Is.True);
    }

    [Test]
    public void HasChoices_FalseForTextDefinition()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        Assert.That(sut.HasChoices, Is.False);
    }

    [Test]
    public void IsLabelEditable_FalseForDisplayNameField()
    {
        var sut = new FieldDefinitionRowViewModel(new DisplayNameFieldDefinition());
        Assert.That(sut.IsLabelEditable, Is.False);
    }

    [Test]
    public void IsLabelEditable_TrueForNonDisplayNameField()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        Assert.That(sut.IsLabelEditable, Is.True);
    }

    [Test]
    public void IsColor_TrueForColorDefinition()
    {
        var sut = new FieldDefinitionRowViewModel(new ColorFieldDefinition());
        Assert.That(sut.IsColor, Is.True);
    }

    [Test]
    public void IsColor_FalseForTextDefinition()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        Assert.That(sut.IsColor, Is.False);
    }

    [Test]
    public void IsPicture_TrueForImageDefinition()
    {
        var sut = new FieldDefinitionRowViewModel(new ImageFieldDefinition());
        Assert.That(sut.IsPicture, Is.True);
    }

    [Test]
    public void IsPicture_FalseForTextDefinition()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        Assert.That(sut.IsPicture, Is.False);
    }

    [Test]
    public void IsCurrency_TrueForCurrencyDefinition()
    {
        var sut = new FieldDefinitionRowViewModel(new CurrencyFieldDefinition());
        Assert.That(sut.IsCurrency, Is.True);
    }

    [Test]
    public void IsCurrency_FalseForTextDefinition()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        Assert.That(sut.IsCurrency, Is.False);
    }

    [Test]
    public void Constructor_LoadsChoicesFromMultiChoiceDefinition()
    {
        var def = new MultiChoiceFieldDefinition
        {
            Choices = [new ChoiceOption { Value = "A", DisplayOrder = 0 }, new ChoiceOption { Value = "B", DisplayOrder = 1 }]
        };
        var sut = new FieldDefinitionRowViewModel(def);

        Assert.That(sut.ChoiceItems.Select(c => c.Value), Is.EqualTo(new[] { "A", "B" }));
    }

    [Test]
    public void Constructor_ListDefinitionWithGroups_AppliesListGate()
    {
        var group = new FieldGroup { Name = "G" };
        var def = new ListFieldDefinition { Groups = [group] };
        var sut = new FieldDefinitionRowViewModel(def);

        Assert.That(sut.SubFieldRows.Count, Is.EqualTo(1));
    }

    [Test]
    public void SelectedGroup_Getter_ReturnsMatchingGroup()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        var group = new FieldGroupRowViewModel("G");
        sut.AvailableGroups.Add(group);
        sut.AssignedGroupId = group.Id;

        Assert.That(sut.SelectedGroup, Is.EqualTo(group));
    }

    [Test]
    public void SelectedGroup_Setter_WithNoGroupMoveRequested_SetsAssignedGroupId()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        var group = new FieldGroupRowViewModel("G");
        sut.AvailableGroups.Add(group);
        sut.GroupMoveRequested = null;

        sut.SelectedGroup = group;

        Assert.That(sut.AssignedGroupId, Is.EqualTo(group.Id));
    }

    [Test]
    public void SelectedGroup_Setter_ClampsSpanToNewGroupColumnCount()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        sut.SetParentColumnCount(4);
        sut.ColumnSpan = 4;
        var group = new FieldGroupRowViewModel("G") { ColumnCount = 2 };
        sut.AvailableGroups.Add(group);
        sut.GroupMoveRequested = null;

        sut.SelectedGroup = group;

        Assert.That(sut.ColumnSpan, Is.EqualTo(2),
            "Assigning a narrower group must clamp the field's span (the previously-divergent SelectedGroup path)");
    }

    [Test]
    public void ClearGroupCommand_WithNoGroupMoveRequested_SetsAssignedGroupIdToNull()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        var group = new FieldGroupRowViewModel("G");
        sut.AvailableGroups.Add(group);
        sut.AssignedGroupId = group.Id;
        sut.GroupMoveRequested = null;

        sut.ClearGroupCommand.Execute(null);

        Assert.That(sut.AssignedGroupId, Is.Null);
    }

    [Test]
    public void BuildDefinition_MultiChoice_PreservesOptions()
    {
        var def = new MultiChoiceFieldDefinition();
        var sut = new FieldDefinitionRowViewModel(def);
        sut.AddChoiceCommand.Execute(null);
        sut.ChoiceItems[0].Value = "X";

        var result = (MultiChoiceFieldDefinition)_mapper.ToDefinition(sut);

        Assert.That(result.Choices.Select(c => c.Value), Is.EqualTo(new[] { "X" }));
    }

    [Test]
    public void BuildDefinition_Currency_PreservesCurrencySymbol()
    {
        var def = new CurrencyFieldDefinition();
        var sut = new FieldDefinitionRowViewModel(def);
        sut.CurrencySymbol = "$";

        var result = (CurrencyFieldDefinition)_mapper.ToDefinition(sut);

        Assert.That(result.CurrencySymbol, Is.EqualTo("$"));
    }

    [Test]
    public void BuildDefinition_List_PreservesGroups()
    {
        var group = new FieldGroup { Name = "G", DisplayOrder = 0 };
        var sub = new TextFieldDefinition { Label = "F", DisplayOrder = 0 };
        var def = new ListFieldDefinition { Groups = [group], SubFields = [sub] };
        var sut = new FieldDefinitionRowViewModel(def);

        var result = (ListFieldDefinition)_mapper.ToDefinition(sut);

        Assert.That(result.Groups.Count, Is.EqualTo(1));
    }

    [Test]
    public void Constructor_LoadsMaxLengthFromTextDefinition()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition { MaxLength = 64 });
        Assert.That(sut.MaxLength, Is.EqualTo(64));
    }

    [Test]
    public void Constructor_LoadsMinMaxFromIntegerDefinition()
    {
        var sut = new FieldDefinitionRowViewModel(new IntegerFieldDefinition { Min = 2, Max = 9 });
        Assert.That(sut.Min, Is.EqualTo(2));
        Assert.That(sut.Max, Is.EqualTo(9));
    }

    [Test]
    public void Constructor_LoadsDecimalPlacesFromDecimalDefinition()
    {
        var sut = new FieldDefinitionRowViewModel(new DecimalFieldDefinition { DecimalPlaces = 4 });
        Assert.That(sut.DecimalPlaces, Is.EqualTo(4));
    }

    [Test]
    public void Constructor_LoadsThreeStateFromBoolDefinition()
    {
        var sut = new FieldDefinitionRowViewModel(new BoolFieldDefinition { ThreeState = true });
        Assert.That(sut.ThreeState, Is.True);
    }

    [Test]
    public void HasTypeSettings_TrueForText() =>
        Assert.That(new FieldDefinitionRowViewModel(new TextFieldDefinition()).HasTypeSettings, Is.True);

    [Test]
    public void HasTypeSettings_TrueForInteger() =>
        Assert.That(new FieldDefinitionRowViewModel(new IntegerFieldDefinition()).HasTypeSettings, Is.True);

    [Test]
    public void HasTypeSettings_TrueForDecimal() =>
        Assert.That(new FieldDefinitionRowViewModel(new DecimalFieldDefinition()).HasTypeSettings, Is.True);

    [Test]
    public void HasTypeSettings_TrueForBool() =>
        Assert.That(new FieldDefinitionRowViewModel(new BoolFieldDefinition()).HasTypeSettings, Is.True);

    [Test]
    public void HasTypeSettings_TrueForCurrency() =>
        Assert.That(new FieldDefinitionRowViewModel(new CurrencyFieldDefinition()).HasTypeSettings, Is.True);

    [Test]
    public void HasTypeSettings_TrueForColor() =>
        Assert.That(new FieldDefinitionRowViewModel(new ColorFieldDefinition()).HasTypeSettings, Is.True);

    [Test]
    public void HasTypeSettings_TrueForRating() =>
        Assert.That(new FieldDefinitionRowViewModel(new RatingFieldDefinition()).HasTypeSettings, Is.True);

    [Test]
    public void HasTypeSettings_TrueForPicture() =>
        Assert.That(new FieldDefinitionRowViewModel(new ImageFieldDefinition()).HasTypeSettings, Is.True);

    [Test]
    public void HasTypeSettings_TrueForChoices() =>
        Assert.That(new FieldDefinitionRowViewModel(new SingleChoiceFieldDefinition()).HasTypeSettings, Is.True);

    [Test]
    public void HasTypeSettings_TrueForList() =>
        Assert.That(new FieldDefinitionRowViewModel(new ListFieldDefinition()).HasTypeSettings, Is.True);

    [Test]
    public void HasTypeSettings_FalseForPlainType() =>
        Assert.That(new FieldDefinitionRowViewModel(new DateFieldDefinition()).HasTypeSettings, Is.False);

    [Test]
    public void BuildDefinition_Text_PreservesMaxLength()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition()) { MaxLength = 25 };
        var result = (TextFieldDefinition)_mapper.ToDefinition(sut);
        Assert.That(result.MaxLength, Is.EqualTo(25));
    }

    [Test]
    public void BuildDefinition_Bool_PreservesThreeState()
    {
        var sut = new FieldDefinitionRowViewModel(new BoolFieldDefinition()) { ThreeState = true };
        var result = (BoolFieldDefinition)_mapper.ToDefinition(sut);
        Assert.That(result.ThreeState, Is.True);
    }

    [Test]
    public void SelectedGroup_SetNull_RaisesSelectedGroupAndRefreshesColumns()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        sut.SetParentColumnCount(3);
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.SelectedGroup = null;

        Assert.That(raised, Does.Contain(nameof(sut.SelectedGroup)));
        Assert.That(raised, Does.Contain(nameof(sut.ColumnSpanOptions)));
    }

    [Test]
    public void SelectedGroup_SetViaGroupMove_RaisesSelectedGroup()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        var group = new FieldGroupRowViewModel("G");
        sut.AvailableGroups.Add(group);
        sut.GroupMoveRequested = (_, _) => { };
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.SelectedGroup = group;

        Assert.That(raised, Does.Contain(nameof(sut.SelectedGroup)));
    }

    [Test]
    public void AvailableGroups_Add_RaisesGroupNotifications()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.AvailableGroups.Add(new FieldGroupRowViewModel("G"));

        Assert.That(raised, Does.Contain(nameof(sut.HasAvailableGroups)));
        Assert.That(raised, Does.Contain(nameof(sut.SelectedGroup)));
    }

    [Test]
    public void SubFieldRows_Add_RaisesSubFieldCount()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.SubFieldRows.Add(new FieldDefinitionRowViewModel(new TextFieldDefinition()));

        Assert.That(raised, Does.Contain(nameof(sut.SubFieldCount)));
    }

    [Test]
    public void LanguageChanged_RaisesTypeDisplayNameAndDisplayLabel()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        LocalizationService.Instance.Apply("de");

        Assert.That(raised, Does.Contain(nameof(sut.TypeDisplayName)));
        Assert.That(raised, Does.Contain(nameof(sut.DisplayLabel)));
    }

    [Test]
    public void ClearGroup_ViaGroupMove_RaisesSelectedGroup()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        var group = new FieldGroupRowViewModel("G");
        sut.AvailableGroups.Add(group);
        sut.AssignedGroupId = group.Id;
        sut.GroupMoveRequested = (_, _) => { };
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.ClearGroupCommand.Execute(null);

        Assert.That(raised, Does.Contain(nameof(sut.SelectedGroup)));
    }

    [Test]
    public void AddChoice_AddsOptionWithEmptyValue()
    {
        var sut = new FieldDefinitionRowViewModel(new SingleChoiceFieldDefinition());

        sut.AddChoiceCommand.Execute(null);

        Assert.That(sut.ChoiceItems[0].Value, Is.Empty);
    }

    [Test]
    public void Constructor_UngroupedSubField_InheritsListColumnCount()
    {
        var sub = new TextFieldDefinition { Label = "F" };
        var def = new ListFieldDefinition { ColumnCount = 3, SubFields = [sub] };
        var sut = new FieldDefinitionRowViewModel(def);

        var child = sut.SubFieldRows.OfType<FieldDefinitionRowViewModel>().Single();

        Assert.That(child.IsInMultiColumnContext, Is.True);
    }

    [Test]
    public void Constructor_GroupedSubField_InheritsGroupColumnCount()
    {
        var group = new FieldGroup { Name = "G", ColumnCount = 3, ShowInList = true };
        var sub = new TextFieldDefinition { Label = "F", GroupId = group.Id };
        var def = new ListFieldDefinition { Groups = [group], SubFields = [sub] };
        var sut = new FieldDefinitionRowViewModel(def);

        var groupRow = sut.SubFieldRows.OfType<FieldGroupRowViewModel>().Single();
        var child = groupRow.ChildNodes.OfType<FieldDefinitionRowViewModel>().Single();

        Assert.That(child.IsInMultiColumnContext, Is.True);
    }

    [Test]
    public void Constructor_GroupShownInList_LeavesChildColumnVisible()
    {
        var group = new FieldGroup { Name = "G", ShowInList = true };
        var sub = new TextFieldDefinition { Label = "F", GroupId = group.Id };
        var def = new ListFieldDefinition { Groups = [group], SubFields = [sub] };
        var sut = new FieldDefinitionRowViewModel(def);

        var groupRow = sut.SubFieldRows.OfType<FieldGroupRowViewModel>().Single();
        var child = groupRow.ChildNodes.OfType<FieldDefinitionRowViewModel>().Single();

        Assert.That(child.ListColumnSuppressed, Is.False);
    }

    [Test]
    public void Constructor_NonListDisplayableType_DefaultsShowInListFalse()
    {
        var sut = new FieldDefinitionRowViewModel(new ImageFieldDefinition());
        Assert.That(sut.ShowInList, Is.False);
    }

    [Test]
    public void Constructor_NonBoolType_DefaultsThreeStateFalse()
    {
        var sut = new FieldDefinitionRowViewModel(new TextFieldDefinition());
        Assert.That(sut.ThreeState, Is.False);
    }

    [Test]
    public void Constructor_GroupHiddenFromList_SuppressesChildColumn()
    {
        var group = new FieldGroup { Name = "G", ShowInList = false };
        var sub = new TextFieldDefinition { Label = "F", GroupId = group.Id };
        var def = new ListFieldDefinition { Groups = [group], SubFields = [sub] };
        var sut = new FieldDefinitionRowViewModel(def);

        var groupRow = sut.SubFieldRows.OfType<FieldGroupRowViewModel>().Single();
        var child = groupRow.ChildNodes.OfType<FieldDefinitionRowViewModel>().Single();

        Assert.That(child.ListColumnSuppressed, Is.True);
    }
}

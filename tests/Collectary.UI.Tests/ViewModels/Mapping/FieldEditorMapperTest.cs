using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;
using Collectary.Presentation.ViewModels.Mapping;

namespace Collectary.UI.Tests.ViewModels.Mapping;

[TestFixture]
public class FieldEditorMapperTest
{
    private readonly IFieldEditorMapper _mapper = new TestFieldEditorMapper().Create();

    [Test]
    public void ToDefinition_PreservesExistingDefinitionId()
    {
        var def = new TextFieldDefinition();
        var originalId = def.Id;
        var row = new FieldDefinitionRowViewModel(def);

        var result = _mapper.ToDefinition(row);

        Assert.That(result.Id, Is.EqualTo(originalId), "Mapping onto the existing definition must preserve its Id");
    }

    [Test]
    public void ToDefinition_CopiesBaseScalars()
    {
        var row = new FieldDefinitionRowViewModel(new TextFieldDefinition())
        {
            Label = "Title",
            IsRequired = true,
            ColumnSpan = 2,
            ShowInList = true
        };

        var result = (TextFieldDefinition)_mapper.ToDefinition(row);

        Assert.That(result.Label, Is.EqualTo("Title"));
        Assert.That(result.IsRequired, Is.True);
        Assert.That(result.ColumnSpan, Is.EqualTo(2));
        Assert.That(result.ShowInList, Is.True);
    }

    [Test]
    public void ToDefinition_CopiesTypeSpecificScalars_ForEveryRenamedMismatch()
    {
        var color = (ColorFieldDefinition)_mapper.ToDefinition(
            new FieldDefinitionRowViewModel(new ColorFieldDefinition()) { Format = ColorFormat.Rgb });
        Assert.That(color.Format, Is.EqualTo(ColorFormat.Rgb));

        var image = (ImageFieldDefinition)_mapper.ToDefinition(
            new FieldDefinitionRowViewModel(new ImageFieldDefinition()) { SizeMode = ImageSizeMode.Min, DisplayWidth = 321, DisplayHeight = 123 });
        Assert.That(image.SizeMode, Is.EqualTo(ImageSizeMode.Min));
        Assert.That(image.DisplayWidth, Is.EqualTo(321));
        Assert.That(image.DisplayHeight, Is.EqualTo(123));

        var currency = (CurrencyFieldDefinition)_mapper.ToDefinition(
            new FieldDefinitionRowViewModel(new CurrencyFieldDefinition()) { CurrencySymbol = "$" });
        Assert.That(currency.CurrencySymbol, Is.EqualTo("$"));

        var rating = (RatingFieldDefinition)_mapper.ToDefinition(
            new FieldDefinitionRowViewModel(new RatingFieldDefinition()) { MaxStars = 9 });
        Assert.That(rating.MaxStars, Is.EqualTo(9));

        var list = (ListFieldDefinition)_mapper.ToDefinition(
            new FieldDefinitionRowViewModel(new ListFieldDefinition()) { ColumnCount = 4, InlineStyle = ListInlineStyle.Grid });
        Assert.That(list.ColumnCount, Is.EqualTo(4));
        Assert.That(list.InlineStyle, Is.EqualTo(ListInlineStyle.Grid));
    }

    [Test]
    public void ToDefinition_CopiesNumericAndBoolConfig()
    {
        var text = (TextFieldDefinition)_mapper.ToDefinition(
            new FieldDefinitionRowViewModel(new TextFieldDefinition()) { MaxLength = 80 });
        Assert.That(text.MaxLength, Is.EqualTo(80));

        var integer = (IntegerFieldDefinition)_mapper.ToDefinition(
            new FieldDefinitionRowViewModel(new IntegerFieldDefinition()) { Min = -5, Max = 50 });
        Assert.That(integer.Min, Is.EqualTo(-5));
        Assert.That(integer.Max, Is.EqualTo(50));

        var dec = (DecimalFieldDefinition)_mapper.ToDefinition(
            new FieldDefinitionRowViewModel(new DecimalFieldDefinition()) { DecimalPlaces = 4 });
        Assert.That(dec.DecimalPlaces, Is.EqualTo(4));

        var boolean = (BoolFieldDefinition)_mapper.ToDefinition(
            new FieldDefinitionRowViewModel(new BoolFieldDefinition()) { ThreeState = true });
        Assert.That(boolean.ThreeState, Is.True);
    }

    [Test]
    public void ToDefinition_List_StampsParentIdAndPreservesSubFieldOrder()
    {
        var sub1 = new TextFieldDefinition { Label = "S1", DisplayOrder = 0 };
        var sub2 = new TextFieldDefinition { Label = "S2", DisplayOrder = 1 };
        var def = new ListFieldDefinition { SubFields = [sub1, sub2] };
        var row = new FieldDefinitionRowViewModel(def);

        var result = (ListFieldDefinition)_mapper.ToDefinition(row);

        Assert.That(result.SubFields, Has.Count.EqualTo(2));
        Assert.That(result.SubFields.All(f => f.ParentListFieldDefinitionId == def.Id), Is.True);
        Assert.That(result.SubFields.Select(f => f.DisplayOrder), Is.EqualTo(new[] { 0, 1 }));
    }

    [Test]
    public void ToGroup_CopiesScalars_TrimsName_AndStampsOwner()
    {
        var presetId = Guid.NewGuid();
        var row = new FieldGroupRowViewModel("  Specs  ")
        {
            DisplayMode = GroupDisplayMode.Tab,
            ColumnCount = 3,
            DefaultCollapsed = true,
            ShowInList = false,
            PrefixColumnHeaders = true,
            DisplayOrder = 5
        };

        var group = _mapper.ToGroup(row, presetId, parentListFieldDefinitionId: null);

        Assert.That(group.Id, Is.EqualTo(row.Id));
        Assert.That(group.Name, Is.EqualTo("Specs"));
        Assert.That(group.DisplayMode, Is.EqualTo(GroupDisplayMode.Tab));
        Assert.That(group.ColumnCount, Is.EqualTo(3));
        Assert.That(group.DefaultCollapsed, Is.True);
        Assert.That(group.ShowInList, Is.False);
        Assert.That(group.PrefixColumnHeaders, Is.True);
        Assert.That(group.DisplayOrder, Is.EqualTo(5));
        Assert.That(group.PresetId, Is.EqualTo(presetId));
    }

    [Test]
    public void ToDefinition_SharedField_ReturnsDefinitionUnchanged()
    {
        var def = new TextFieldDefinition { Label = "System" };
        var row = new FieldDefinitionRowViewModel(def, isSharedField: true) { Label = "changed" };

        var result = _mapper.ToDefinition(row);

        Assert.That(result.Label, Is.EqualTo("System"));
    }

    [Test]
    public void ToDefinition_EveryConcreteFieldType_MapsWithoutStrictModeError()
    {
        var concreteTypes = typeof(FieldDefinition).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(FieldDefinition).IsAssignableFrom(t));

        foreach (var type in concreteTypes)
        {
            var def = (FieldDefinition)Activator.CreateInstance(type)!;
            var row = new FieldDefinitionRowViewModel(def);
            Assert.DoesNotThrow(() => _mapper.ToDefinition(row),
                $"Strict Mapster config rejected {type.Name} — a destination member has no source and no ignore");
        }
    }
}

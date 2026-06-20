using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Infrastructure.Sync;

namespace Collectary.Infrastructure.Tests.Sync;

[TestFixture]
public class SyncSerializerTest
{
    private SyncSerializer _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new SyncSerializer();

    [Test]
    public void Preset_RoundTrips_PolymorphicFieldsAndGroups()
    {
        var group = new FieldGroup { Name = "Main" };
        var list = new ListFieldDefinition { Label = "Tracks" };
        list.SubFields.Add(new IntegerFieldDefinition { Label = "Number" });
        var preset = new Preset
        {
            Name = "Model trains",
            OwnerId = Guid.NewGuid(),
            Revision = 4,
            Groups = { group },
            Fields =
            {
                new TextFieldDefinition { Label = "Title" },
                list,
            },
        };

        var clone = _sut.Deserialize<Preset>(_sut.Serialize(preset));

        Assert.Multiple(() =>
        {
            Assert.That(clone.Name, Is.EqualTo("Model trains"));
            Assert.That(clone.OwnerId, Is.EqualTo(preset.OwnerId));
            Assert.That(clone.Revision, Is.EqualTo(4));
            Assert.That(clone.Groups.Single().Name, Is.EqualTo("Main"));
            Assert.That(clone.Fields[0], Is.TypeOf<TextFieldDefinition>());
            Assert.That(clone.Fields[0].Label, Is.EqualTo("Title"));
            Assert.That(clone.Fields[1], Is.TypeOf<ListFieldDefinition>());
            Assert.That(((ListFieldDefinition)clone.Fields[1]).SubFields.Single(), Is.TypeOf<IntegerFieldDefinition>());
        });
    }

    [Test]
    public void AutoNumberField_RoundTrips_WithAllConfigAndValue()
    {
        var field = new AutoNumberFieldDefinition
        {
            Label = "Number",
            Editable = true,
            Strategy = AutoNumberStrategy.FillGaps,
            OnDuplicate = DuplicateHandling.Warn,
            ShowInList = false,
        };
        var preset = new Preset { Name = "Trains", OwnerId = Guid.NewGuid(), Fields = { field } };
        var item = new Item { PresetId = preset.Id, DisplayName = "BR 01" };
        item.Values.Add(new AutoNumberFieldValue { FieldDefinitionId = field.Id, Value = 7 });

        var clonedPreset = _sut.Deserialize<Preset>(_sut.Serialize(preset));
        var clonedItem = _sut.Deserialize<Item>(_sut.Serialize(item));

        Assert.Multiple(() =>
        {
            var def = (AutoNumberFieldDefinition)clonedPreset.Fields.Single();
            Assert.That(def.Editable, Is.True);
            Assert.That(def.Strategy, Is.EqualTo(AutoNumberStrategy.FillGaps));
            Assert.That(def.OnDuplicate, Is.EqualTo(DuplicateHandling.Warn));
            Assert.That(def.ShowInList, Is.False);
            Assert.That(((AutoNumberFieldValue)clonedItem.Values.Single()).Value, Is.EqualTo(7));
        });
    }

    [Test]
    public void MultiChoiceField_RoundTrips_WithDisplayMode()
    {
        var field = new MultiChoiceFieldDefinition
        {
            Label = "Colours",
            DisplayMode = MultiChoiceDisplayMode.Collapsed,
            Choices = { new ChoiceOption { Value = "Red" }, new ChoiceOption { Value = "Blue" } },
        };
        var preset = new Preset { Name = "Palette", OwnerId = Guid.NewGuid(), Fields = { field } };

        var clone = _sut.Deserialize<Preset>(_sut.Serialize(preset));

        var def = (MultiChoiceFieldDefinition)clone.Fields.Single();
        Assert.Multiple(() =>
        {
            Assert.That(def.DisplayMode, Is.EqualTo(MultiChoiceDisplayMode.Collapsed));
            Assert.That(def.Choices, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void Item_RoundTrips_PolymorphicValuesAndNestedListEntries()
    {
        var listValue = new ListFieldValue();
        var entry = new ListEntry();
        entry.SubValues.Add(new IntegerFieldValue { Value = 5 });
        listValue.Entries.Add(entry);

        var item = new Item
        {
            DisplayName = "Loco 42",
            PresetId = Guid.NewGuid(),
            Revision = 2,
            Values =
            {
                new TextFieldValue { Value = "hello" },
                listValue,
            },
        };

        var clone = _sut.Deserialize<Item>(_sut.Serialize(item));

        Assert.Multiple(() =>
        {
            Assert.That(clone.DisplayName, Is.EqualTo("Loco 42"));
            Assert.That(clone.PresetId, Is.EqualTo(item.PresetId));
            Assert.That(clone.Revision, Is.EqualTo(2));
            Assert.That(clone.Values[0], Is.TypeOf<TextFieldValue>());
            Assert.That(((TextFieldValue)clone.Values[0]).Value, Is.EqualTo("hello"));
            Assert.That(clone.Values[1], Is.TypeOf<ListFieldValue>());
            var clonedEntry = ((ListFieldValue)clone.Values[1]).Entries.Single();
            Assert.That(((IntegerFieldValue)clonedEntry.SubValues.Single()).Value, Is.EqualTo(5));
        });
    }

    [Test]
    public void Deserialize_PinsLegacyWireDiscriminators()
    {
        // A document previously written to a user's cloud. Renaming any FieldValue CLR type would change
        // its $type and silently break deserialization of already-synced data — this guards the wire format.
        var json = """
        {
          "DisplayName": "legacy",
          "Values": [
            { "$type": "TextFieldValue", "Value": "hi" },
            { "$type": "IntegerFieldValue", "Value": 7 }
          ]
        }
        """;

        var item = _sut.Deserialize<Item>(json);

        Assert.Multiple(() =>
        {
            Assert.That(item.Values[0], Is.TypeOf<TextFieldValue>());
            Assert.That(((TextFieldValue)item.Values[0]).Value, Is.EqualTo("hi"));
            Assert.That(item.Values[1], Is.TypeOf<IntegerFieldValue>());
        });
    }

    [Test]
    public void Item_RoundTrips_TagsAndMultiChoiceCollections()
    {
        var tags = new TagsFieldValue();
        tags.Tags.Add("a");
        tags.Tags.Add("b");
        var multi = new MultiChoiceFieldValue();
        multi.Selected.Add("x");
        var item = new Item { DisplayName = "T", Values = { tags, multi } };

        var clone = _sut.Deserialize<Item>(_sut.Serialize(item));

        Assert.Multiple(() =>
        {
            Assert.That(((TagsFieldValue)clone.Values[0]).Tags, Is.EqualTo(new[] { "a", "b" }));
            Assert.That(((MultiChoiceFieldValue)clone.Values[1]).Selected, Is.EqualTo(new[] { "x" }));
        });
    }
}

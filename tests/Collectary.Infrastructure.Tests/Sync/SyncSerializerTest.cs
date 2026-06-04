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

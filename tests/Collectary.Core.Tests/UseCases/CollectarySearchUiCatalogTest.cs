using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using Collectary.Search;

namespace Collectary.Core.Tests.UseCases;

[TestFixture]
public class CollectarySearchUiCatalogTest
{
    private static async Task<SearchUiSnapshot> Snapshot(SearchCatalogSnapshot source)
    {
        var inner = A.Fake<ISearchFieldCatalog>();
        A.CallTo(() => inner.GetSnapshotAsync()).Returns(source);
        return await new CollectarySearchUiCatalog(inner).GetSnapshotAsync();
    }

    private static SearchUiField Field(SearchUiSnapshot snapshot, string label) =>
        snapshot.Fields.Single(f => string.Equals(f.Label, label, StringComparison.Ordinal));

    [Test]
    public async Task Snapshot_AlwaysExposesTheFivePseudoFieldsInOrder()
    {
        var snapshot = await Snapshot(new SearchCatalogSnapshot());

        Assert.That(snapshot.Fields.Select(f => f.Label),
            Is.EqualTo(new[] { "name", "preset", "collection", "created", "updated" }));
    }

    [Test]
    public async Task Snapshot_PseudoFields_ComeStraightFromThePseudoFieldRegistry()
    {
        var snapshot = await Snapshot(new SearchCatalogSnapshot());
        var pseudo = new PseudoFieldCatalog();

        Assert.That(snapshot.Fields.Select(f => f.Label), Is.EqualTo(pseudo.Labels),
            "every pseudo field in the registry must appear in the UI snapshot");
        foreach (var label in pseudo.Labels)
            Assert.That(Field(snapshot, label).Aliases, Is.EqualTo(pseudo.AliasesFor(label)),
                $"aliases for the {label} field must come from the registry");
    }

    [Test]
    public async Task Snapshot_OnlyTheCollectionFieldAliasesPreset()
    {
        var snapshot = await Snapshot(new SearchCatalogSnapshot());

        Assert.That(Field(snapshot, "collection").Aliases, Is.EqualTo(new[] { "preset" }));
        Assert.That(Field(snapshot, "name").Aliases, Is.Empty);
        Assert.That(Field(snapshot, "preset").Aliases, Is.Empty);
        Assert.That(Field(snapshot, "created").Aliases, Is.Empty);
        Assert.That(Field(snapshot, "updated").Aliases, Is.Empty);
    }

    [Test]
    public async Task Snapshot_PresetAndCollectionSuggestThePresetNames_OthersSuggestNothing()
    {
        var snapshot = await Snapshot(new SearchCatalogSnapshot
        {
            Presets = [new SearchPresetEntry(Guid.NewGuid(), "Trains"), new SearchPresetEntry(Guid.NewGuid(), "Books")],
        });

        Assert.That(Field(snapshot, "preset").ValueSuggestions, Is.EqualTo(new[] { "Trains", "Books" }));
        Assert.That(Field(snapshot, "collection").ValueSuggestions, Is.EqualTo(new[] { "Trains", "Books" }));
        Assert.That(Field(snapshot, "name").ValueSuggestions, Is.Empty);
        Assert.That(Field(snapshot, "created").ValueSuggestions, Is.Empty);
        Assert.That(Field(snapshot, "updated").ValueSuggestions, Is.Empty);
    }

    [Test]
    public async Task Snapshot_PseudoOperatorsMatchTheirCatalog()
    {
        var snapshot = await Snapshot(new SearchCatalogSnapshot());
        var pseudo = new PseudoFieldCatalog();

        foreach (var label in new[] { "name", "preset", "collection", "created", "updated" })
            Assert.That(Field(snapshot, label).Operators, Is.EqualTo(pseudo.OperatorsFor(label)),
                $"operators for the {label} field must come from the pseudo-field catalog");
    }

    [Test]
    public async Task Snapshot_SearchableFields_CarryTheirValueSuggestionsAndOperators()
    {
        var status = new SingleChoiceFieldDefinition { Label = "Status" };
        status.Choices.Add(new ChoiceOption { Value = "open" });
        status.Choices.Add(new ChoiceOption { Value = "done" });
        var snapshot = await Snapshot(new SearchCatalogSnapshot
        {
            Fields = [new SearchFieldGroup("Status", [status])],
        });

        var field = Field(snapshot, "Status");
        Assert.That(field.ValueSuggestions, Is.EquivalentTo(new[] { "open", "done" }));
        Assert.That(field.Operators, Is.EquivalentTo(((ISearchableFieldDefinition)status).SupportedOperators));
        Assert.That(field.Aliases, Is.Empty, "real fields carry no label aliases");
    }

    [Test]
    public async Task Snapshot_NonSearchableFields_AreDropped()
    {
        var snapshot = await Snapshot(new SearchCatalogSnapshot
        {
            Fields = [new SearchFieldGroup("Photo", [new ImageFieldDefinition { Label = "Photo" }])],
        });

        Assert.That(snapshot.Fields.Any(f => f.Label == "Photo"), Is.False);
    }

    [Test]
    public async Task Snapshot_RealFieldsCollidingWithAPseudoLabel_AreNotDuplicated()
    {
        var snapshot = await Snapshot(new SearchCatalogSnapshot
        {
            Fields = [new SearchFieldGroup("collection", [new TextFieldDefinition { Label = "collection" }])],
        });

        Assert.That(snapshot.Fields.Count(f => string.Equals(f.Label, "collection", StringComparison.OrdinalIgnoreCase)),
            Is.EqualTo(1));
    }
}

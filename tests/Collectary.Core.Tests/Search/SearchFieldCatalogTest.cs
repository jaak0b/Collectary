using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using FakeItEasy;

namespace Collectary.Core.Tests.Search;

[TestFixture]
public class SearchFieldCatalogTest
{
    private IPresetUseCase _presets = null!;
    private SearchFieldCatalog _catalog = null!;

    [SetUp]
    public void SetUp()
    {
        _presets = A.Fake<IPresetUseCase>();
        _catalog = new SearchFieldCatalog(_presets);
    }

    private void SetUpPresets(params (Preset Preset, FieldDefinition[] Fields)[] entries)
    {
        A.CallTo(() => _presets.GetAllPresetsAsync())
            .Returns(entries.Select(e => e.Preset).ToList());
        foreach (var (preset, fields) in entries)
        {
            A.CallTo(() => _presets.GetEffectiveFieldsAsync(preset.Id))
                .Returns(new EffectiveFields { Fields = fields });
        }
    }

    [Test]
    public async Task GetSnapshotAsync_GroupsSameLabelCaseInsensitivelyAcrossPresets()
    {
        var bookField = new TextFieldDefinition { Label = "Price" };
        var gameField = new IntegerFieldDefinition { Label = "price" };
        SetUpPresets(
            (new Preset { Name = "Books" }, new FieldDefinition[] { bookField }),
            (new Preset { Name = "Games" }, new FieldDefinition[] { gameField }));

        var snapshot = await _catalog.GetSnapshotAsync();

        var group = snapshot.Fields.Single();
        Assert.That(group.Definitions, Is.EquivalentTo(new FieldDefinition[] { bookField, gameField }));
    }

    [Test]
    public async Task GetSnapshotAsync_ListsAllPresetNames()
    {
        SetUpPresets(
            (new Preset { Name = "Books" }, Array.Empty<FieldDefinition>()),
            (new Preset { Name = "Games" }, Array.Empty<FieldDefinition>()));

        var snapshot = await _catalog.GetSnapshotAsync();

        Assert.That(snapshot.Presets.Select(p => p.Name), Is.EquivalentTo(new[] { "Books", "Games" }));
    }

    [Test]
    public async Task FindField_MatchesLabelCaseInsensitively()
    {
        SetUpPresets((new Preset { Name = "Books" },
            new FieldDefinition[] { new TextFieldDefinition { Label = "Author" } }));

        var snapshot = await _catalog.GetSnapshotAsync();

        Assert.That(snapshot.FindField("author"), Is.Not.Null);
        Assert.That(snapshot.FindField("AUTHOR"), Is.Not.Null);
        Assert.That(snapshot.FindField("missing"), Is.Null);
    }

    [Test]
    public async Task GetSnapshotAsync_SkipsBlankLabels()
    {
        SetUpPresets((new Preset { Name = "Books" },
            new FieldDefinition[] { new TextFieldDefinition { Label = "" } }));

        var snapshot = await _catalog.GetSnapshotAsync();

        Assert.That(snapshot.Fields, Is.Empty);
    }
}

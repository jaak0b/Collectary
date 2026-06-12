using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Core.Search;
using Collectary.Core.UseCases;
using Collectary.Infrastructure.Persistence;

namespace Collectary.Infrastructure.Tests.Search;

file sealed class SeededCatalog : ISearchFieldCatalog
{
    private readonly SearchCatalogSnapshot _snapshot;

    public SeededCatalog(IEnumerable<Preset> presets)
    {
        var presetList = presets.ToList();
        _snapshot = new SearchCatalogSnapshot
        {
            Fields = presetList
                .SelectMany(p => p.Fields)
                .Where(f => !string.IsNullOrWhiteSpace(f.Label))
                .GroupBy(f => f.Label, StringComparer.OrdinalIgnoreCase)
                .Select(g => new SearchFieldGroup(g.Key, g.ToList()))
                .ToList(),
            Presets = presetList.Select(p => new SearchPresetEntry(p.Id, p.Name)).ToList(),
        };
    }

    public Task<SearchCatalogSnapshot> GetSnapshotAsync() => Task.FromResult(_snapshot);
}

[TestFixture]
public class ItemSearchServiceRoundTripTest : DbIntegrationTestBase
{
    private ItemRepository _repository = null!;
    private Preset _books = null!;
    private Preset _games = null!;
    private CurrencyFieldDefinition _bookPrice = null!;
    private IntegerFieldDefinition _gamePrice = null!;
    private TagsFieldDefinition _bookTags = null!;

    [SetUp]
    public new void BaseSetUp()
    {
        base.BaseSetUp();
        _repository = new ItemRepository(DbFactory);

        _books = new Preset { Name = "Books" };
        _bookPrice = new CurrencyFieldDefinition { Label = "Price", PresetId = _books.Id };
        _bookTags = new TagsFieldDefinition { Label = "Tags", PresetId = _books.Id };
        _books.Fields.Add(_bookPrice);
        _books.Fields.Add(_bookTags);

        _games = new Preset { Name = "Games" };
        _gamePrice = new IntegerFieldDefinition { Label = "Price", PresetId = _games.Id };
        _games.Fields.Add(_gamePrice);

        using var db = DbFactory();
        db.Presets.AddRange(_books, _games);
        db.SaveChanges();
    }

    private ItemSearchService CreateService() => new(
        _repository,
        new SeededCatalog([_books, _games]),
        new QueryParser(new QueryLexer()),
        new QueryBinder(new PseudoFieldCatalog(TimeZoneInfo.Utc, CultureInfo.InvariantCulture)),
        new ServerFilterBuilder(),
        new QueryEvaluator());

    private async Task SeedItemsAsync()
    {
        var cheapBook = new Item { PresetId = _books.Id, DisplayName = "Cheap Book" };
        cheapBook.Values.Add(new CurrencyFieldValue { FieldDefinitionId = _bookPrice.Id, Value = 5m });
        cheapBook.Values.Add(new TagsFieldValue { FieldDefinitionId = _bookTags.Id, Tags = ["rare"] });

        var deluxeBook = new Item { PresetId = _books.Id, DisplayName = "Deluxe Book" };
        deluxeBook.Values.Add(new CurrencyFieldValue { FieldDefinitionId = _bookPrice.Id, Value = 99m });
        deluxeBook.Values.Add(new TagsFieldValue { FieldDefinitionId = _bookTags.Id, Tags = ["mint"] });

        var bigGame = new Item { PresetId = _games.Id, DisplayName = "Big Game" };
        bigGame.Values.Add(new IntegerFieldValue { FieldDefinitionId = _gamePrice.Id, Value = 60 });

        var smallGame = new Item { PresetId = _games.Id, DisplayName = "Small Game" };
        smallGame.Values.Add(new IntegerFieldValue { FieldDefinitionId = _gamePrice.Id, Value = 10 });

        await _repository.AddAsync(cheapBook);
        await _repository.AddAsync(deluxeBook);
        await _repository.AddAsync(bigGame);
        await _repository.AddAsync(smallGame);
    }

    [Test]
    public async Task SearchAsync_SameLabelAcrossPresets_MatchesBothValueTypes()
    {
        await SeedItemsAsync();

        var result = await CreateService().SearchAsync("Price > 20 ORDER BY name");

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Items.Select(i => i.DisplayName),
            Is.EqualTo(new[] { "Big Game", "Deluxe Book" }));
    }

    [Test]
    public async Task SearchAsync_TagsCondition_FallsBackToMemoryAndStaysExact()
    {
        await SeedItemsAsync();

        var result = await CreateService().SearchAsync("Tags = rare");

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Items.Single().DisplayName, Is.EqualTo("Cheap Book"));
    }

    [Test]
    public async Task SearchAsync_NegatedUntranslatableCondition_KeepsPolarityCorrect()
    {
        await SeedItemsAsync();

        var result = await CreateService().SearchAsync("preset = Books AND NOT Tags = rare");

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Items.Single().DisplayName, Is.EqualTo("Deluxe Book"));
    }

    [Test]
    public async Task SearchAsync_PresetClause_ScopesToOneCollection()
    {
        await SeedItemsAsync();

        var result = await CreateService().SearchAsync("preset = \"Books\" AND Price > 3 ORDER BY Name DESC");

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Items.Select(i => i.DisplayName),
            Is.EqualTo(new[] { "Deluxe Book", "Cheap Book" }));
    }

    [Test]
    public async Task SearchAsync_EmptyQuery_ReturnsEverything()
    {
        await SeedItemsAsync();

        var result = await CreateService().SearchAsync("");

        Assert.That(result.Items, Has.Count.EqualTo(4));
    }

    [Test]
    public async Task SearchAsync_OrderByPrice_SortsAcrossValueTypes()
    {
        await SeedItemsAsync();

        var result = await CreateService().SearchAsync("ORDER BY Price");

        Assert.That(result.Items.Select(i => i.DisplayName),
            Is.EqualTo(new[] { "Cheap Book", "Small Game", "Big Game", "Deluxe Book" }));
    }
}

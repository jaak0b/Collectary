using System.Globalization;
using System.Linq.Expressions;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Search;
using Collectary.Core.UseCases;
using FakeItEasy;

namespace Collectary.Core.Tests.UseCases;

[TestFixture]
public class ItemSearchServiceTest
{
    private IItemRepository _repository = null!;
    private ISearchFieldCatalog _catalog = null!;
    private ItemSearchService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = A.Fake<IItemRepository>();
        _catalog = A.Fake<ISearchFieldCatalog>();
        A.CallTo(() => _catalog.GetSnapshotAsync()).Returns(new SearchCatalogSnapshot());
        _sut = new ItemSearchService(
            _repository,
            _catalog,
            new QueryParser(new QueryLexer()),
            new QueryBinder(new PseudoFieldCatalog(TimeZoneInfo.Utc, CultureInfo.InvariantCulture)),
            new ServerFilterBuilder(),
            new QueryEvaluator());
    }

    [Test]
    public async Task SearchAsync_ParseError_ReturnsErrorsWithoutTouchingRepository()
    {
        var result = await _sut.SearchAsync("name =");

        Assert.That(result.Errors, Is.Not.Empty);
        Assert.That(result.Items, Is.Empty);
        A.CallTo(() => _repository.SearchAsync(A<Expression<Func<Item, bool>>?>._))
            .MustNotHaveHappened();
    }

    [Test]
    public async Task SearchAsync_BindError_ReturnsErrorsWithoutTouchingRepository()
    {
        var result = await _sut.SearchAsync("Ghost = 1");

        Assert.That(result.Errors.Single().Code, Is.EqualTo(QueryErrorCode.UnknownField));
        A.CallTo(() => _repository.SearchAsync(A<Expression<Func<Item, bool>>?>._))
            .MustNotHaveHappened();
    }

    [Test]
    public async Task SearchAsync_FiltersCandidatesExactlyAndSorts()
    {
        var loco = new Item { DisplayName = "Loco 42" };
        var wagon = new Item { DisplayName = "Wagon" };
        var anotherLoco = new Item { DisplayName = "Loco 7" };
        A.CallTo(() => _repository.SearchAsync(A<Expression<Func<Item, bool>>?>._))
            .Returns(new[] { loco, wagon, anotherLoco });

        var result = await _sut.SearchAsync("name ~ loco ORDER BY name DESC");

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Items, Is.EqualTo(new[] { anotherLoco, loco }));
    }

    [Test]
    public async Task SearchAsync_EmptyQuery_ReturnsEverythingTheRepositoryYields()
    {
        var items = new[] { new Item { DisplayName = "a" }, new Item { DisplayName = "b" } };
        A.CallTo(() => _repository.SearchAsync(null)).Returns(items);

        var result = await _sut.SearchAsync("");

        Assert.That(result.Items, Is.EqualTo(items));
    }

    [Test]
    public async Task SearchAsync_PassesTheServerFilterToTheRepository()
    {
        Expression<Func<Item, bool>>? captured = null;
        A.CallTo(() => _repository.SearchAsync(A<Expression<Func<Item, bool>>?>._))
            .Invokes((Expression<Func<Item, bool>>? filter) => captured = filter)
            .Returns(Array.Empty<Item>());

        await _sut.SearchAsync("name = loco");

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Compile()(new Item { DisplayName = "loco" }), Is.True);
    }

    [Test]
    public async Task SearchAsync_NoticesFlowThrough()
    {
        A.CallTo(() => _repository.SearchAsync(A<Expression<Func<Item, bool>>?>._))
            .Returns(Array.Empty<Item>());

        var result = await _sut.SearchAsync("preset = Ghost");

        Assert.That(result.Notices.Single().Code, Is.EqualTo(QueryErrorCode.InvalidValue));
    }
}

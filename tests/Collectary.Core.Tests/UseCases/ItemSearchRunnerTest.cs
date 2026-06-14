using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using Collectary.Search;

namespace Collectary.Core.Tests.UseCases;

[TestFixture]
public class ItemSearchRunnerTest
{
    [Test]
    public async Task SearchAsync_PassesTheResultListThroughWithoutCopying()
    {
        var items = new List<Item> { new() { DisplayName = "a" }, new() { DisplayName = "b" } };
        var errors = new List<QueryError> { new(QueryErrorCode.UnknownField, 0, 0, "ghost") };
        var notices = new List<QueryNotice> { new(QueryErrorCode.InvalidValue, "x") };
        var service = A.Fake<IItemSearchService>();
        A.CallTo(() => service.SearchAsync("name ~ a"))
            .Returns(new ItemSearchResult(items, errors, notices));

        var outcome = await new ItemSearchRunner(service).SearchAsync("name ~ a");

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Items, Is.SameAs(items), "the item list must flow through covariantly, not be copied");
            Assert.That(outcome.Errors, Is.SameAs(errors));
            Assert.That(outcome.Notices, Is.SameAs(notices));
        });
    }
}

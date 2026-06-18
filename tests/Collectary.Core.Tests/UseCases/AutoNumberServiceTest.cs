using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using FakeItEasy;

namespace Collectary.Core.Tests.UseCases;

[TestFixture]
public class AutoNumberServiceTest
{
    [Test]
    public async Task UsedNumbersAsync_DelegatesToRepository_ForwardingFieldAndExcludedItem()
    {
        var repo = A.Fake<IItemRepository>();
        var fieldId = Guid.NewGuid();
        var excludeId = Guid.NewGuid();
        A.CallTo(() => repo.GetUsedAutoNumbersAsync(fieldId, excludeId)).Returns(new[] { 1, 2, 5 });

        var result = await new AutoNumberService(repo).UsedNumbersAsync(fieldId, excludeId);

        Assert.That(result, Is.EquivalentTo(new[] { 1, 2, 5 }));
    }
}

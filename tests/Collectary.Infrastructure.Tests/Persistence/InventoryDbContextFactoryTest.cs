using Collectary.Infrastructure.Persistence;

namespace Collectary.Infrastructure.Tests.Persistence;

[TestFixture]
public class InventoryDbContextFactoryTest
{
    [Test]
    public void CreateDbContext_ReturnsConfiguredContext()
    {
        var factory = new InventoryDbContextFactory();

        using var ctx = factory.CreateDbContext([]);

        Assert.That(ctx, Is.Not.Null);
        Assert.That(ctx.Database.ProviderName, Does.Contain("Sqlite"));
    }
}

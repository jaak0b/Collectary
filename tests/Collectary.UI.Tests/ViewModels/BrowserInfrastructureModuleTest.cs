using Autofac;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Infrastructure.Persistence;
using Collectary.UI.DI;
using Collectary.UI.Storage;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class BrowserInfrastructureModuleTest
{
    private static IContainer Build()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule(new CoreModule());
        builder.RegisterModule(new BrowserInfrastructureModule());
        return builder.Build();
    }

    [Test]
    public void ImageStore_IsInMemory()
    {
        using var container = Build();
        Assert.That(container.Resolve<IImageStore>(), Is.TypeOf<InMemoryImageStore>());
    }

    [Test]
    public void DbContext_UsesInMemoryProvider()
    {
        using var container = Build();
        using var scope = container.BeginLifetimeScope();

        var db = scope.Resolve<InventoryDbContext>();

        Assert.That(db.Database.ProviderName, Is.EqualTo("Microsoft.EntityFrameworkCore.InMemory"));
    }

    [Test]
    public async Task PresetRepository_RoundTripsAgainstInMemoryModel()
    {
        using var container = Build();
        using var scope = container.BeginLifetimeScope();
        scope.Resolve<InventoryDbContext>().Database.EnsureCreated();
        var repo = scope.Resolve<IPresetRepository>();

        await repo.AddAsync(new Preset { Id = Guid.NewGuid(), Name = "Books", CreatedAt = DateTime.UtcNow });

        var all = await repo.GetAllAsync();
        Assert.That(all.Select(p => p.Name), Does.Contain("Books"));
    }
}

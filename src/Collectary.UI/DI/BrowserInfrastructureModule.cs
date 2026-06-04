using Autofac;
using Collectary.Core.Logging;
using Collectary.Core.Ports;
using Collectary.Infrastructure.Persistence;
using Collectary.UI.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Collectary.UI.DI;

public class BrowserInfrastructureModule : Module
{
    private readonly InMemoryDatabaseRoot _databaseRoot = new();

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<NullAppLogger>().As<IAppLogger>().SingleInstance();

        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase("collectary", _databaseRoot)
            .Options;

        builder.Register(_ => new InventoryDbContext(options))
               .AsSelf()
               .InstancePerDependency();

        builder.RegisterType<FieldDefinitionMerger>().As<IFieldDefinitionMerger>().SingleInstance();
        builder.RegisterType<PresetRepository>().As<IPresetRepository>().SingleInstance();
        builder.RegisterType<ItemRepository>().As<IItemRepository>().SingleInstance();
        builder.RegisterType<SystemFieldRepository>().As<ISystemFieldRepository>().SingleInstance();

        builder.RegisterType<InMemoryImageStore>().As<IImageStore>().SingleInstance();
    }
}

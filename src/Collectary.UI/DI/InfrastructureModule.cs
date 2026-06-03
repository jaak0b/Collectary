using Autofac;
using Collectary.Core.Ports;
using Collectary.Infrastructure.Persistence;
using Collectary.Infrastructure.Storage;
using Collectary.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace Collectary.UI.DI;

public class InfrastructureModule : Module
{
    private readonly string _databasePath;
    private readonly string _imageStorePath;

    public InfrastructureModule(string databasePath, string imageStorePath)
    {
        _databasePath = databasePath;
        _imageStorePath = imageStorePath;
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SerilogAppLogger>().As<IAppLogger>().SingleInstance();

        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;

        builder.Register(_ => new InventoryDbContext(options))
               .AsSelf()
               .InstancePerDependency();

        builder.RegisterType<FieldDefinitionMerger>().As<IFieldDefinitionMerger>().SingleInstance();

        builder.RegisterType<PresetRepository>().As<IPresetRepository>().SingleInstance();
        builder.RegisterType<ItemRepository>().As<IItemRepository>().SingleInstance();
        builder.RegisterType<SystemFieldRepository>().As<ISystemFieldRepository>().SingleInstance();

        builder.Register(_ => new FileSystemImageStore(_imageStorePath))
               .As<IImageStore>()
               .SingleInstance();
    }
}

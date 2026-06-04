using Autofac;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using Collectary.Infrastructure.Persistence;
using Collectary.Infrastructure.Storage;
using Collectary.Infrastructure.Sync;
using Collectary.Presentation.Services;
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
        builder.RegisterType<UserRepository>().As<IUserRepository>().SingleInstance();
        builder.RegisterType<CredentialStore>().As<ICredentialStore>().SingleInstance();
        builder.RegisterType<ShareRepository>().As<IShareRepository>().SingleInstance();

        builder.Register(_ => new FileSystemImageStore(_imageStorePath))
               .As<IImageStore>()
               .SingleInstance();

        builder.RegisterType<Services.PreferencesSyncStatus>().As<ISyncStatus>().SingleInstance();
        builder.RegisterType<SyncSerializer>().As<ISyncSerializer>().SingleInstance();
        builder.RegisterType<EfSyncStore>().As<ISyncStore>().SingleInstance();
        builder.Register(_ => new FileSystemSyncBackend(() => AppPreferences.Load().SyncLocation))
               .As<ISyncBackend>()
               .SingleInstance();
        builder.RegisterType<SyncService>().As<ISyncService>().SingleInstance();
    }
}

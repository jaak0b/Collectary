using Autofac;
using Collectary.Core.Logging;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using Collectary.Infrastructure.Persistence;
using Collectary.Infrastructure.Sync;
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
        builder.RegisterType<SharedFieldRepository>().As<ISharedFieldRepository>().SingleInstance();
        builder.RegisterType<UserRepository>().As<IUserRepository>().SingleInstance();
        builder.RegisterType<CredentialStore>().As<ICredentialStore>().SingleInstance();
        builder.RegisterType<ShareRepository>().As<IShareRepository>().SingleInstance();

        builder.RegisterType<InMemoryImageStore>().As<IImageStore>().SingleInstance();

        builder.RegisterType<Services.PreferencesSyncStatus>().As<ISyncStatus>().SingleInstance();
        builder.RegisterType<SyncSerializer>().As<ISyncSerializer>().SingleInstance();
        builder.RegisterType<EfSyncStore>().As<ISyncStore>().SingleInstance();
        builder.Register(_ => new FileSystemSyncBackend(string.Empty)).As<ISyncBackend>().SingleInstance();
        builder.RegisterType<SyncService>().As<ISyncService>().SingleInstance();
    }
}

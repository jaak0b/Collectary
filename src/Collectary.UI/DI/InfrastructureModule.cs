using Autofac;
using Collectary.Core.Domain;
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
        builder.RegisterType<Infrastructure.Barcode.ZXingBarcodeImageDecoder>().As<IBarcodeImageDecoder>().SingleInstance();
        builder.RegisterType<Infrastructure.Barcode.ZXingBarcodeImageGenerator>().As<IBarcodeImageGenerator>().SingleInstance();

        builder.RegisterType<PresetRepository>().As<IPresetRepository>().SingleInstance();
        builder.RegisterType<ItemRepository>().As<IItemRepository>().SingleInstance();
        builder.RegisterType<SharedFieldRepository>().As<ISharedFieldRepository>().SingleInstance();
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
               .Keyed<ISyncBackend>(CloudProvider.Folder)
               .SingleInstance();

        // The single backend SyncService consumes: routes to the active provider's backend,
        // resolving cloud backends lazily (only when selected) so MSAL/Graph never spin up for
        // Folder-only users, and tolerating providers whose backend isn't registered on this platform.
        builder.Register(c =>
        {
            var context = c.Resolve<IComponentContext>();
            var backends = new Dictionary<CloudProvider, Func<ISyncBackend>>
            {
                [CloudProvider.Folder] = () => context.ResolveKeyed<ISyncBackend>(CloudProvider.Folder),
            };
            foreach (var provider in new[] { CloudProvider.OneDrive, CloudProvider.GoogleDrive })
                if (context.IsRegisteredWithKey<ISyncBackend>(provider))
                    backends[provider] = () => context.ResolveKeyed<ISyncBackend>(provider);

            return new RoutingSyncBackend(() => AppPreferences.Load().SyncProvider, backends);
        }).As<ISyncBackend>().SingleInstance();

        builder.RegisterType<SyncService>().As<ISyncService>().SingleInstance();
        builder.RegisterType<BackupService>().As<IBackupService>().SingleInstance();
    }
}

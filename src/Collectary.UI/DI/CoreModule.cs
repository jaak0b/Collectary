using Autofac;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;

namespace Collectary.UI.DI;

public class CoreModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<PresetUseCase>().As<IPresetUseCase>().SingleInstance();
        builder.RegisterType<ItemUseCase>().As<IItemUseCase>().SingleInstance();
        builder.RegisterType<SystemFieldUseCase>().As<ISystemFieldUseCase>().SingleInstance();
        builder.RegisterType<CollectionAuthorizationService>().As<ICollectionAuthorization>().SingleInstance();
        builder.RegisterType<ShareUseCase>().As<IShareUseCase>().SingleInstance();
        builder.RegisterType<AccountBootstrapper>().As<IAccountBootstrapper>().SingleInstance();
    }
}

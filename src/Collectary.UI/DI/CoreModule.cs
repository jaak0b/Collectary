using Autofac;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using Collectary.Core.UseCases.Import;

namespace Collectary.UI.DI;

public class CoreModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<PresetUseCase>().As<IPresetUseCase>().SingleInstance();
        builder.RegisterType<ItemUseCase>().As<IItemUseCase>().SingleInstance();
        builder.RegisterType<SharedFieldUseCase>().As<ISharedFieldUseCase>().SingleInstance();
        builder.RegisterType<CollectionAuthorizationService>().As<ICollectionAuthorization>().SingleInstance();
        builder.RegisterType<ShareUseCase>().As<IShareUseCase>().SingleInstance();
        builder.RegisterType<AccountBootstrapper>().As<IAccountBootstrapper>().SingleInstance();

        builder.RegisterType<GridShaper>().As<IGridShaper>().SingleInstance();
        builder.RegisterType<CultureDetector>().As<ICultureDetector>().SingleInstance();
        builder.RegisterType<FieldTypeInference>().As<IFieldTypeInference>().SingleInstance();
        builder.RegisterType<SpreadsheetImportService>().As<ISpreadsheetImportService>().SingleInstance();
    }
}

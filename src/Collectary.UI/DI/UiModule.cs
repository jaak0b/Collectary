using Autofac;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Localization;
using Collectary.Presentation.DI;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;
using Collectary.Presentation.ViewModels.ListCells;
using Collectary.Presentation.ViewModels.Mapping;
using Collectary.UI.Services;
using Collectary.UI.Views;
using MapsterMapper;

namespace Collectary.UI.DI;

public class UiModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        RegisterInfrastructure(builder);
        RegisterMapping(builder);
        RegisterFieldEditors(builder);
        RegisterListCells(builder);
        RegisterColorEditors(builder);
        RegisterTemplates(builder);
        RegisterNavigation(builder);
    }

    private void RegisterTemplates(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(typeof(Collectary.Presentation.Templates.IPresetTemplate).Assembly)
            .Where(t => typeof(Collectary.Presentation.Templates.IPresetTemplate).IsAssignableFrom(t) && !t.IsAbstract)
            .As<Collectary.Presentation.Templates.IPresetTemplate>()
            .SingleInstance();
        builder.RegisterType<Collectary.Presentation.Templates.PresetTemplateLibrary>()
            .As<Collectary.Presentation.Templates.IPresetTemplateLibrary>()
            .SingleInstance();
    }

    private void RegisterMapping(ContainerBuilder builder)
    {
        builder.RegisterInstance(new FieldEditorMappingConfig().Build()).SingleInstance();
        builder.RegisterType<Mapper>().As<IMapper>().SingleInstance();
        builder.RegisterType<FieldEditorMapper>().As<IFieldEditorMapper>().SingleInstance();
    }

    private void RegisterInfrastructure(ContainerBuilder builder)
    {
        builder.RegisterInstance(LocalizationService.Instance).SingleInstance();
        builder.RegisterInstance(ThemeService.Instance).SingleInstance();
        builder.RegisterType<OverlayDialogService>()
            .As<IDialogService>()
            .As<IDialogHost>()
            .SingleInstance();
        builder.RegisterType<ListCellBuilder>().As<IListCellBuilder>().AsSelf().SingleInstance();
        builder.RegisterType<Collectary.Presentation.Services.CountryCatalog>()
            .As<Collectary.Presentation.Services.ICountryCatalog>().SingleInstance();
        builder.RegisterType<FieldEditorRegistry>().As<IFieldEditorRegistry>().AsSelf().SingleInstance();
    }

    private void RegisterFieldEditors(ContainerBuilder builder)
    {
        builder.RegisterType<TextFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(TextFieldDefinition));
        builder.RegisterType<BoolFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(BoolFieldDefinition));
        builder.RegisterType<IntegerFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(IntegerFieldDefinition));
        builder.RegisterType<DecimalFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(DecimalFieldDefinition));
        builder.RegisterType<DateFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(DateFieldDefinition));
        builder.RegisterType<ColorFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(ColorFieldDefinition));
        builder.RegisterType<RatingFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(RatingFieldDefinition));
        builder.RegisterType<UrlFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(UrlFieldDefinition));
        builder.RegisterType<SingleChoiceFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(SingleChoiceFieldDefinition));
        builder.RegisterType<MultiChoiceFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(MultiChoiceFieldDefinition));
        builder.RegisterType<ListFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(ListFieldDefinition));
        builder.RegisterType<ImageFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(ImageFieldDefinition));
        builder.RegisterType<RichTextFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(RichTextFieldDefinition));
        builder.RegisterType<PhoneFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(PhoneFieldDefinition));
        builder.RegisterType<EmailFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(EmailFieldDefinition));
        builder.RegisterType<PercentageFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(PercentageFieldDefinition));
        builder.RegisterType<DurationFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(DurationFieldDefinition));
        builder.RegisterType<TimeFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(TimeFieldDefinition));
        builder.RegisterType<CurrencyFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(CurrencyFieldDefinition));
        builder.RegisterType<BarcodeFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(BarcodeFieldDefinition));
        builder.RegisterType<QrCodeFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(QrCodeFieldDefinition));
        builder.RegisterType<MultiImageFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(MultiImageFieldDefinition));
        builder.RegisterType<FileAttachmentFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(FileAttachmentFieldDefinition));
        builder.RegisterType<CountryFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(CountryFieldDefinition));
        builder.RegisterType<MeasurementFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(MeasurementFieldDefinition));
        builder.RegisterType<WeightFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(WeightFieldDefinition));
        builder.RegisterType<DateRangeFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(DateRangeFieldDefinition));
        builder.RegisterType<LinkedItemFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(LinkedItemFieldDefinition));
        builder.RegisterType<AudioFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(AudioFieldDefinition));
        builder.RegisterType<TagsFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(TagsFieldDefinition));
    }

    private void RegisterListCells(ContainerBuilder builder)
    {
        void TextCell(string key) =>
            builder.Register<Func<FieldValue, FieldDefinition, ListCellViewModelBase>>(
                    _ => (fv, fd) => new TextListCellViewModel(fv, fd))
                .Keyed<Func<FieldValue, FieldDefinition, ListCellViewModelBase>>(key);

        TextCell(nameof(TextFieldDefinition));
        TextCell(nameof(IntegerFieldDefinition));
        TextCell(nameof(DateFieldDefinition));
        TextCell(nameof(RatingFieldDefinition));
        TextCell(nameof(UrlFieldDefinition));
        TextCell(nameof(SingleChoiceFieldDefinition));
        TextCell(nameof(MultiChoiceFieldDefinition));

        builder.Register<Func<FieldValue, FieldDefinition, ListCellViewModelBase>>(
                _ => (fv, fd) => new ColorListCellViewModel(fv, fd))
            .Keyed<Func<FieldValue, FieldDefinition, ListCellViewModelBase>>(nameof(ColorFieldDefinition));

        builder.Register<Func<FieldValue, FieldDefinition, ListCellViewModelBase>>(
                _ => (fv, fd) => new RichTextListCellViewModel(fv, fd))
            .Keyed<Func<FieldValue, FieldDefinition, ListCellViewModelBase>>(nameof(RichTextFieldDefinition));

        TextCell(nameof(PhoneFieldDefinition));
        TextCell(nameof(EmailFieldDefinition));
        TextCell(nameof(BarcodeFieldDefinition));
        TextCell(nameof(QrCodeFieldDefinition));
        TextCell(nameof(CountryFieldDefinition));
        TextCell(nameof(MeasurementFieldDefinition));
        TextCell(nameof(WeightFieldDefinition));
        TextCell(nameof(DateRangeFieldDefinition));
        TextCell(nameof(LinkedItemFieldDefinition));

        builder.Register<Func<FieldValue, FieldDefinition, ListCellViewModelBase>>(
                _ => (fv, fd) => new PercentageListCellViewModel(fv, fd))
            .Keyed<Func<FieldValue, FieldDefinition, ListCellViewModelBase>>(nameof(PercentageFieldDefinition));

        builder.Register<Func<FieldValue, FieldDefinition, ListCellViewModelBase>>(
                _ => (fv, fd) => new DurationListCellViewModel(fv, fd))
            .Keyed<Func<FieldValue, FieldDefinition, ListCellViewModelBase>>(nameof(DurationFieldDefinition));

        builder.Register<Func<FieldValue, FieldDefinition, ListCellViewModelBase>>(
                _ => (fv, fd) => new TimeListCellViewModel(fv, fd))
            .Keyed<Func<FieldValue, FieldDefinition, ListCellViewModelBase>>(nameof(TimeFieldDefinition));

        builder.Register<Func<FieldValue, FieldDefinition, ListCellViewModelBase>>(
                _ => (fv, fd) => new CurrencyListCellViewModel(fv, fd))
            .Keyed<Func<FieldValue, FieldDefinition, ListCellViewModelBase>>(nameof(CurrencyFieldDefinition));

        builder.Register<Func<FieldValue, FieldDefinition, ListCellViewModelBase>>(
                _ => (fv, fd) => new TagsListCellViewModel(fv, fd))
            .Keyed<Func<FieldValue, FieldDefinition, ListCellViewModelBase>>(nameof(TagsFieldDefinition));

        builder.Register<Func<FieldValue, FieldDefinition, ListCellViewModelBase>>(
                _ => (fv, fd) => new DecimalListCellViewModel(fv, fd))
            .Keyed<Func<FieldValue, FieldDefinition, ListCellViewModelBase>>(nameof(DecimalFieldDefinition));

        builder.Register<Func<FieldValue, FieldDefinition, ListCellViewModelBase>>(
                _ => (fv, fd) => new BoolListCellViewModel(fv, fd))
            .Keyed<Func<FieldValue, FieldDefinition, ListCellViewModelBase>>(nameof(BoolFieldDefinition));
    }

    private void RegisterColorEditors(ContainerBuilder builder)
    {
        builder.RegisterType<HexColorFormatEditorViewModel>().Named<ColorFormatEditorViewModel>(nameof(ColorFormat.Hex));
        builder.RegisterType<RgbColorFormatEditorViewModel>().Named<ColorFormatEditorViewModel>(nameof(ColorFormat.Rgb));
        builder.RegisterType<ArgbColorFormatEditorViewModel>().Named<ColorFormatEditorViewModel>(nameof(ColorFormat.Argb));
        builder.RegisterType<CmykColorFormatEditorViewModel>().Named<ColorFormatEditorViewModel>(nameof(ColorFormat.Cmyk));
        builder.RegisterType<ColorFormatEditorFactory>().AsSelf().SingleInstance();
    }

    private void RegisterNavigation(ContainerBuilder builder)
    {
        builder.RegisterType<Services.AvaloniaUiDispatcher>()
            .As<Collectary.Presentation.Services.IUiDispatcher>()
            .SingleInstance();
        builder.RegisterType<Services.TaskBackgroundRunner>()
            .As<Collectary.Presentation.Services.IBackgroundRunner>()
            .SingleInstance();
        builder.RegisterType<Services.DispatcherSyncScheduler>()
            .As<Collectary.Presentation.Services.ISyncScheduler>();
        builder.RegisterType<MainWindowViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<MainWindow>().AsSelf().SingleInstance();
    }
}

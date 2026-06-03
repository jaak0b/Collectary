using Autofac;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.UI.Localization;
using Collectary.UI.Services;
using Collectary.UI.ViewModels;
using Collectary.UI.ViewModels.ListCells;
using Collectary.UI.Views;

namespace Collectary.UI.DI;

public class UiModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        RegisterInfrastructure(builder);
        RegisterFieldEditors(builder);
        RegisterListCells(builder);
        RegisterColorEditors(builder);
        RegisterNavigation(builder);
    }

    private void RegisterInfrastructure(ContainerBuilder builder)
    {
        builder.RegisterInstance(LocalizationService.Instance).SingleInstance();
        builder.RegisterInstance(ThemeService.Instance).SingleInstance();
        builder.RegisterInstance(AvaloniaDialogService.Instance).As<IDialogService>().SingleInstance();
        builder.RegisterType<ListCellBuilder>().As<IListCellBuilder>().AsSelf().SingleInstance();
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
        builder.RegisterType<TagsFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(TagsFieldDefinition));
    }

    private void RegisterListCells(ContainerBuilder builder)
    {
        void TextCell(string key) =>
            builder.Register<Func<FieldValue, FieldDefinition, ListCellViewModelBase>>(
                    _ => (fv, fd) => new TextListCellViewModel(fv, fd))
                .Keyed<Func<FieldValue, FieldDefinition, ListCellViewModelBase>>(key);

        TextCell(nameof(TextFieldDefinition));
        TextCell(nameof(BoolFieldDefinition));
        TextCell(nameof(IntegerFieldDefinition));
        TextCell(nameof(DecimalFieldDefinition));
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
        builder.RegisterType<MainWindowViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<MainWindow>().AsSelf().SingleInstance();
    }
}

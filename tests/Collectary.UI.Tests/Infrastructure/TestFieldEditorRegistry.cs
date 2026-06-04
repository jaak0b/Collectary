using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.DI;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.Infrastructure;

public class TestFieldEditorRegistry : IFieldEditorRegistry
{
    public FieldEditorViewModelBase? Create(FieldDefinition def, FieldValue? existing, ItemEditingContext context)
    {
        return def switch
        {
            TextFieldDefinition d =>
                new TextFieldEditorViewModel(d, d.GetOrCreateEmptyValue(existing)),
            BoolFieldDefinition d =>
                new BoolFieldEditorViewModel(d, d.GetOrCreateEmptyValue(existing)),
            IntegerFieldDefinition d =>
                new IntegerFieldEditorViewModel(d, d.GetOrCreateEmptyValue(existing)),
            DecimalFieldDefinition d =>
                new DecimalFieldEditorViewModel(d, d.GetOrCreateEmptyValue(existing)),
            DateFieldDefinition d =>
                new DateFieldEditorViewModel(d, d.GetOrCreateEmptyValue(existing)),
            TimeFieldDefinition d =>
                new TimeFieldEditorViewModel(d, d.GetOrCreateEmptyValue(existing)),
            DurationFieldDefinition d =>
                new DurationFieldEditorViewModel(d, d.GetOrCreateEmptyValue(existing)),
            CurrencyFieldDefinition d =>
                new CurrencyFieldEditorViewModel(d, d.GetOrCreateEmptyValue(existing)),
            PercentageFieldDefinition d =>
                new PercentageFieldEditorViewModel(d, d.GetOrCreateEmptyValue(existing)),
            RatingFieldDefinition d =>
                new RatingFieldEditorViewModel(d, d.GetOrCreateEmptyValue(existing)),
            UrlFieldDefinition d =>
                new UrlFieldEditorViewModel(d, d.GetOrCreateEmptyValue(existing)),
            EmailFieldDefinition d =>
                new EmailFieldEditorViewModel(d, d.GetOrCreateEmptyValue(existing)),
            PhoneFieldDefinition d =>
                new PhoneFieldEditorViewModel(d, d.GetOrCreateEmptyValue(existing)),
            RichTextFieldDefinition d =>
                new RichTextFieldEditorViewModel(d, d.GetOrCreateEmptyValue(existing)),
            TagsFieldDefinition d =>
                new TagsFieldEditorViewModel(d, d.GetOrCreateEmptyValue(existing)),
            SingleChoiceFieldDefinition d =>
                new SingleChoiceFieldEditorViewModel(d, d.GetOrCreateEmptyValue(existing)),
            MultiChoiceFieldDefinition d =>
                new MultiChoiceFieldEditorViewModel(d, d.GetOrCreateEmptyValue(existing)),
            ColorFieldDefinition d =>
                new ColorFieldEditorViewModel(d, d.GetOrCreateEmptyValue(existing),
                    TestColorFormatEditorFactoryFactory.Instance),
            ListFieldDefinition d =>
                new ListFieldEditorViewModel(d, d.GetOrCreateEmptyValue(existing), context),
            ImageFieldDefinition d =>
                new ImageFieldEditorViewModel(d, d.GetOrCreateEmptyValue(existing), context),
            _ => null
        };
    }
}

using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Localization;

namespace Collectary.Presentation.Templates;

public abstract class PresetTemplateBase
{
    protected string L(string key) => LocalizationService.Instance[key];

    protected DisplayNameFieldDefinition Title(string labelKey) =>
        new() { Label = L(labelKey), IsRequired = true, ShowInList = true };

    protected TextFieldDefinition Text(string labelKey, bool showInList = false) =>
        new() { Label = L(labelKey), ShowInList = showInList };

    protected RichTextFieldDefinition RichText(string labelKey) =>
        new() { Label = L(labelKey) };

    protected IntegerFieldDefinition Integer(string labelKey) =>
        new() { Label = L(labelKey) };

    protected DecimalFieldDefinition Decimal(string labelKey) =>
        new() { Label = L(labelKey) };

    protected CurrencyFieldDefinition Currency(string labelKey, string symbol = "€") =>
        new() { Label = L(labelKey), CurrencySymbol = symbol };

    protected PercentageFieldDefinition Percentage(string labelKey) =>
        new() { Label = L(labelKey) };

    protected DateFieldDefinition Date(string labelKey) =>
        new() { Label = L(labelKey) };

    protected DurationFieldDefinition Duration(string labelKey) =>
        new() { Label = L(labelKey) };

    protected RatingFieldDefinition Rating(string labelKey, int maxStars = 5) =>
        new() { Label = L(labelKey), MaxStars = maxStars };

    protected BoolFieldDefinition Bool(string labelKey) =>
        new() { Label = L(labelKey) };

    protected ColorFieldDefinition Color(string labelKey) =>
        new() { Label = L(labelKey) };

    protected ImageFieldDefinition Image(string labelKey) =>
        new() { Label = L(labelKey) };

    protected SingleChoiceFieldDefinition SingleChoice(string labelKey, params string[] optionKeys)
    {
        var def = new SingleChoiceFieldDefinition { Label = L(labelKey) };
        for (var i = 0; i < optionKeys.Length; i++)
            def.Choices.Add(new ChoiceOption { Value = L(optionKeys[i]), DisplayOrder = i });
        return def;
    }

    protected MultiChoiceFieldDefinition MultiChoice(string labelKey, params string[] optionKeys)
    {
        var def = new MultiChoiceFieldDefinition { Label = L(labelKey) };
        for (var i = 0; i < optionKeys.Length; i++)
            def.Choices.Add(new ChoiceOption { Value = L(optionKeys[i]), DisplayOrder = i });
        return def;
    }

    protected ListFieldDefinition List(string labelKey, ListInlineStyle inlineStyle, params FieldDefinition[] subFields)
    {
        var def = new ListFieldDefinition { Label = L(labelKey), InlineStyle = inlineStyle };
        for (var i = 0; i < subFields.Length; i++)
        {
            subFields[i].DisplayOrder = i;
            subFields[i].ParentListFieldDefinitionId = def.Id;
            def.SubFields.Add(subFields[i]);
        }
        return def;
    }

    protected FieldGroup Group(string nameKey, int columns, params FieldDefinition[] fields)
    {
        var group = new FieldGroup
        {
            Name = L(nameKey),
            ColumnCount = columns,
            DisplayMode = GroupDisplayMode.Card,
        };
        foreach (var field in fields)
            field.GroupId = group.Id;
        return group;
    }

    protected Preset Compose(string nameKey, int columns, IReadOnlyList<FieldDefinition> fields, IReadOnlyList<FieldGroup> groups)
    {
        var preset = Compose(nameKey, columns, fields);
        foreach (var group in groups)
            group.PresetId = preset.Id;
        preset.Groups = groups.ToList();
        return preset;
    }

    protected Preset Compose(string nameKey, int columns, IReadOnlyList<FieldDefinition> fields)
    {
        var ordered = fields.ToList();
        if (!ordered.Any(f => f is DisplayNameFieldDefinition))
            ordered.Insert(0, new DisplayNameFieldDefinition
            {
                Label = L("DisplayNameField"),
                IsRequired = true,
                ShowInList = true
            });

        for (var i = 0; i < ordered.Count; i++)
            ordered[i].DisplayOrder = i;

        return new Preset
        {
            Name = L(nameKey),
            ColumnCount = columns,
            Fields = ordered,
        };
    }
}

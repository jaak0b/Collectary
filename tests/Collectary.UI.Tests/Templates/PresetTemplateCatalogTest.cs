using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Templates;

namespace Collectary.UI.Tests.Templates;

[TestFixture]
public class PresetTemplateCatalogTest
{
    [TearDown]
    public void Reset() => LocalizationService.Instance.Apply("en");

    private static IEnumerable<IPresetTemplate> Templates => TemplateTestHelper.AllTemplates();

    [Test]
    public void Catalog_HasExpectedNumberOfTemplates()
    {
        Assert.That(TemplateTestHelper.AllTemplates(), Has.Count.EqualTo(22));
    }

    [Test]
    public void AllKeys_AreUnique()
    {
        var keys = TemplateTestHelper.AllTemplates().Select(t => t.Key).ToList();
        Assert.That(keys, Is.Unique);
    }

    [Test]
    public void EveryTemplate_BuildsPresetWithNameAndFields([ValueSource(nameof(Templates))] IPresetTemplate template)
    {
        var preset = template.Build();
        Assert.That(preset.Name, Is.Not.Null.And.Not.Empty);
        Assert.That(preset.Fields, Has.Count.GreaterThanOrEqualTo(3));
    }

    [Test]
    public void EveryTemplate_HasExactlyOneTitleField([ValueSource(nameof(Templates))] IPresetTemplate template)
    {
        var preset = template.Build();
        Assert.That(preset.Fields.Count(f => f is DisplayNameFieldDefinition), Is.EqualTo(1));
    }

    [Test]
    public void EveryTemplate_FieldsHaveContiguousDisplayOrder([ValueSource(nameof(Templates))] IPresetTemplate template)
    {
        var preset = template.Build();
        var orders = preset.Fields.Select(f => f.DisplayOrder).OrderBy(o => o).ToList();
        Assert.That(orders, Is.EqualTo(Enumerable.Range(0, preset.Fields.Count).ToList()));
    }

    [Test]
    public void EveryChoiceField_HasAtLeastTwoChoicesWithDistinctOrder([ValueSource(nameof(Templates))] IPresetTemplate template)
    {
        var preset = template.Build();
        foreach (var single in preset.Fields.OfType<SingleChoiceFieldDefinition>())
        {
            Assert.That(single.Choices, Has.Count.GreaterThanOrEqualTo(2), $"{template.Key}.{single.Label}");
            Assert.That(single.Choices.Select(c => c.DisplayOrder), Is.Unique, $"{template.Key}.{single.Label}");
            Assert.That(single.Choices.Select(c => c.Value), Is.All.Not.Empty);
        }
        foreach (var multi in preset.Fields.OfType<MultiChoiceFieldDefinition>())
        {
            Assert.That(multi.Choices, Has.Count.GreaterThanOrEqualTo(2), $"{template.Key}.{multi.Label}");
            Assert.That(multi.Choices.Select(c => c.DisplayOrder), Is.Unique, $"{template.Key}.{multi.Label}");
            Assert.That(multi.Choices.Select(c => c.Value), Is.All.Not.Empty);
        }
    }

    [Test]
    public void ListSubFields_HaveParentIdSet([ValueSource(nameof(Templates))] IPresetTemplate template)
    {
        var preset = template.Build();
        foreach (var list in preset.Fields.OfType<ListFieldDefinition>())
        {
            Assert.That(list.SubFields, Is.Not.Empty, $"{template.Key} list {list.Label} should have sub-fields");
            Assert.That(list.SubFields.All(s => s.ParentListFieldDefinitionId == list.Id), Is.True);
        }
    }

    [Test]
    public void RecipesTemplate_HasIngredientsList()
    {
        var recipes = TemplateTestHelper.AllTemplates().Single(t => t.Key == "recipes");
        var preset = recipes.Build();
        var list = preset.Fields.OfType<ListFieldDefinition>().Single();
        Assert.That(list.SubFields, Has.Count.EqualTo(2));
    }

    [Test]
    public void MakeupTemplate_HasColorField()
    {
        var makeup = TemplateTestHelper.AllTemplates().Single(t => t.Key == "makeup");
        var preset = makeup.Build();
        Assert.That(preset.Fields.OfType<ColorFieldDefinition>().Count(), Is.EqualTo(1));
    }

    [Test]
    public void ModelTrainsTemplate_HasScaleAndManufacturerChoices()
    {
        var trains = TemplateTestHelper.AllTemplates().Single(t => t.Key == "modeltrains");
        var preset = trains.Build();
        Assert.That(preset.Fields.OfType<SingleChoiceFieldDefinition>().Count(), Is.GreaterThanOrEqualTo(3));
        Assert.That(preset.Fields.OfType<IntegerFieldDefinition>().Any(), Is.True, "DCC address");
    }
}

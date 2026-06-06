using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Templates;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Tests.Infrastructure;
using Collectary.UI.Tests.Templates;

namespace Collectary.UI.Tests.PresetEditor;

[TestFixture]
public class PresetTemplateSeedFlowTest : FlowTestBase
{
    [TearDown]
    public void ResetLanguage() => LocalizationService.Instance.Apply("en");

    private IPresetTemplate Template(string key) =>
        TemplateTestHelper.AllTemplates().Single(t => t.Key == key);

    private async Task<Core.Domain.Preset> SeedSaveAndReload(string templateKey)
    {
        LocalizationService.Instance.Apply("en");
        var seed = Template(templateKey).Build();
        var editor = MakePresetEditorVm(seed: seed);
        await editor.LoadAsync();
        await editor.SaveAndGoBackCommand.ExecuteAsync(null);
        return (await PresetRepo.GetAllAsync()).Single();
    }

#if DEBUG
    [Test]
    public async Task ChooseDeveloperTemplate_PersistsPresetWithGroupedFields()
    {
        var saved = await SeedSaveAndReload("developer");

        Assert.That(saved.Groups, Is.Not.Empty, "the field group must survive seed → save → reload");
        var group = saved.Groups[0];
        Assert.That(saved.Fields.Count(f => f.GroupId == group.Id), Is.EqualTo(2));
        Assert.That(saved.Fields.OfType<ListFieldDefinition>().Single().SubFields, Is.Not.Empty);
    }
#endif

    [Test]
    public async Task ChooseBooksTemplate_PersistsPresetWithFields()
    {
        var saved = await SeedSaveAndReload("books");

        Assert.That(saved.Name, Is.EqualTo("Books"));
        Assert.That(saved.Fields.Any(f => f is DisplayNameFieldDefinition), Is.True);
        Assert.That(saved.Fields.Any(f => f.Label == "Author"), Is.True);
    }

    [Test]
    public async Task SeededEditor_IsCreateMode_ProducesExactlyOnePreset()
    {
        await SeedSaveAndReload("movies");
        var all = await PresetRepo.GetAllAsync();
        Assert.That(all, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ChooseBooksTemplate_SingleChoiceOptionsRoundTrip()
    {
        var saved = await SeedSaveAndReload("books");

        var format = saved.Fields.OfType<SingleChoiceFieldDefinition>().Single(f => f.Label == "Format");
        var values = format.Choices.OrderBy(c => c.DisplayOrder).Select(c => c.Value).ToList();
        Assert.That(values, Is.EqualTo(new[] { "Hardcover", "Paperback", "eBook", "Audiobook" }));
    }

    [Test]
    public async Task ChooseBooksTemplate_RatingMaxStarsRoundTrips()
    {
        var saved = await SeedSaveAndReload("books");
        var rating = saved.Fields.OfType<RatingFieldDefinition>().Single();
        Assert.That(rating.MaxStars, Is.EqualTo(5));
    }

    [Test]
    public async Task ChooseRecipesTemplate_ListSubFieldsRoundTrip()
    {
        var saved = await SeedSaveAndReload("recipes");

        var list = saved.Fields.OfType<ListFieldDefinition>().Single();
        Assert.That(list.SubFields, Has.Count.EqualTo(2));
        Assert.That(list.SubFields.All(s => s.ParentListFieldDefinitionId == list.Id), Is.True);
        Assert.That(list.InlineStyle, Is.EqualTo(ListInlineStyle.Grid));
    }

    [Test]
    public async Task ChooseMakeupTemplate_ColorFieldRoundTrips()
    {
        var saved = await SeedSaveAndReload("makeup");
        Assert.That(saved.Fields.OfType<ColorFieldDefinition>().Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task ChooseModelTrainsTemplate_PersistsScaleAndDccFields()
    {
        var saved = await SeedSaveAndReload("modeltrains");

        var scale = saved.Fields.OfType<SingleChoiceFieldDefinition>().Single(f => f.Label == "Scale / Gauge");
        Assert.That(scale.Choices.Select(c => c.Value), Does.Contain("HO"));
        Assert.That(saved.Fields.OfType<IntegerFieldDefinition>().Any(f => f.Label == "DCC address"), Is.True);
        Assert.That(saved.Fields.OfType<BoolFieldDefinition>().Any(f => f.Label == "DCC equipped"), Is.True);
    }

    [Test]
    public async Task ChooseTemplateInGerman_PersistsGermanLabels()
    {
        LocalizationService.Instance.Apply("de");
        var seed = Template("books").Build();
        var editor = MakePresetEditorVm(seed: seed);
        await editor.LoadAsync();
        await editor.SaveAndGoBackCommand.ExecuteAsync(null);

        var saved = (await PresetRepo.GetAllAsync()).Single();
        Assert.That(saved.Name, Is.EqualTo("Bücher"));
        Assert.That(saved.Fields.Any(f => f.Label == "Autor"), Is.True);
    }

    [Test]
    public async Task SeededPreset_CanCreateItemAfterSave()
    {
        var saved = await SeedSaveAndReload("books");
        var ef = await PresetUseCase.GetEffectiveFieldsAsync(saved.Id);

        var itemVm = MakeItemEditorVm(saved, ef);
        SetDisplayName(itemVm, "The Hobbit");
        await itemVm.PersistAsync();

        var items = await ItemUseCase.GetItemsForPresetAsync(saved.Id);
        Assert.That(items, Has.Count.EqualTo(1));
        Assert.That(items[0].DisplayName, Is.EqualTo("The Hobbit"));
    }

    [Test]
    public async Task ChooseCoinsTemplate_PersistsPresetWithFields()
    {
        var saved = await SeedSaveAndReload("coins");

        Assert.That(saved.Name, Is.EqualTo("Coins"));
        Assert.That(saved.Fields.OfType<SingleChoiceFieldDefinition>().Any(f => f.Label == "Grade"), Is.True);
        Assert.That(saved.Fields.OfType<CurrencyFieldDefinition>().Any(f => f.Label == "Value"), Is.True);
    }

    [Test]
    public async Task SaveCommand_ThenSaveAndGoBack_UpdatesNotCreates()
    {
        var onSavedCount = 0;
        var seed = Template("coins").Build();
        var editor = MakePresetEditorVm(seed: seed, onSaved: () => onSavedCount++);
        await editor.LoadAsync();

        await editor.SaveCommand.ExecuteAsync(null);
        editor.Name = "My Coins";
        await editor.SaveAndGoBackCommand.ExecuteAsync(null);

        var all = await PresetRepo.GetAllAsync();
        Assert.That(all, Has.Count.EqualTo(1), "SaveAndGoBack must not create a second preset");
        Assert.That(onSavedCount, Is.EqualTo(1), "SaveAndGoBack must have succeeded and fired onSaved");
        Assert.That(all[0].Name, Is.EqualTo("My Coins"), "The rename from the second save must have persisted");
    }

    [Test]
    public async Task SaveCommand_ThenSaveAndGoBack_FieldsIntact()
    {
        var seed = Template("coins").Build();
        var editor = MakePresetEditorVm(seed: seed);
        await editor.LoadAsync();

        await editor.SaveCommand.ExecuteAsync(null);
        await editor.SaveAndGoBackCommand.ExecuteAsync(null);

        var saved = (await PresetRepo.GetAllAsync()).Single();
        Assert.That(saved.Fields.OfType<SingleChoiceFieldDefinition>().Any(f => f.Label == "Grade"), Is.True);
    }
}

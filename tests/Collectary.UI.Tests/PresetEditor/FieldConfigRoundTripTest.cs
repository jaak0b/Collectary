using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Tests.Infrastructure;

namespace Collectary.UI.Tests.PresetEditor;

[TestFixture]
public class FieldConfigRoundTripTest : FlowTestBase
{
    private async Task<FieldDefinitionRowViewModel> SaveAndGetRow(
        Func<PresetEditorViewModel, Task> configure,
        Func<FieldDefinitionRowViewModel, bool> selector)
    {
        var sut = MakePresetEditorVm();
        await sut.LoadAsync();
        sut.Name = "P";
        await configure(sut);
        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        var saved = (await PresetRepo.GetAllAsync())[0];
        var reloaded = MakePresetEditorVm(existing: saved);
        return reloaded.CurrentRows
            .OfType<FieldDefinitionRowViewModel>()
            .Concat(reloaded.CurrentRows.OfType<FieldGroupRowViewModel>()
                .SelectMany(g => g.ChildNodes.OfType<FieldDefinitionRowViewModel>()))
            .First(selector);
    }

    [Test]
    public async Task TextField_BaseScalars_RoundTrip()
    {
        var row = await SaveAndGetRow(async sut =>
        {
            await sut.AddFieldAsync<TextFieldDefinition>();
            var r = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Last(f => !f.IsDisplayName);
            r.Label = "Author";
            r.IsRequired = true;
            r.ShowInList = true;
        }, r => r.Label == "Author");

        Assert.That(row.Label, Is.EqualTo("Author"));
        Assert.That(row.IsRequired, Is.True);
        Assert.That(row.ShowInList, Is.True);
    }

    [Test]
    public async Task ColorField_Format_Rgb_RoundTrips()
    {
        var row = await SaveAndGetRow(async sut =>
        {
            await sut.AddFieldAsync<ColorFieldDefinition>();
            var r = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Last(f => f.IsColor);
            r.Label = "Spine Color";
            r.Format = ColorFormat.Rgb;
        }, r => r.Label == "Spine Color");

        Assert.That(row.Format, Is.EqualTo(ColorFormat.Rgb));
    }

    [Test]
    public async Task ColorField_Format_Argb_RoundTrips()
    {
        var row = await SaveAndGetRow(async sut =>
        {
            await sut.AddFieldAsync<ColorFieldDefinition>();
            var r = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Last(f => f.IsColor);
            r.Format = ColorFormat.Argb;
            r.Label = "C1";
        }, r => r.Label == "C1");

        Assert.That(row.Format, Is.EqualTo(ColorFormat.Argb));
    }

    [Test]
    public async Task ColorField_Format_Cmyk_RoundTrips()
    {
        var row = await SaveAndGetRow(async sut =>
        {
            await sut.AddFieldAsync<ColorFieldDefinition>();
            var r = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Last(f => f.IsColor);
            r.Format = ColorFormat.Cmyk;
            r.Label = "C2";
        }, r => r.Label == "C2");

        Assert.That(row.Format, Is.EqualTo(ColorFormat.Cmyk));
    }

    [Test]
    public async Task CurrencyField_Symbol_RoundTrips()
    {
        var row = await SaveAndGetRow(async sut =>
        {
            await sut.AddFieldAsync<CurrencyFieldDefinition>();
            var r = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Last(f => f.IsCurrency);
            r.CurrencySymbol = "$";
            r.Label = "Price";
        }, r => r.Label == "Price");

        Assert.That(row.CurrencySymbol, Is.EqualTo("$"));
    }

    [Test]
    public async Task RatingField_MaxStars_RoundTrips()
    {
        var row = await SaveAndGetRow(async sut =>
        {
            await sut.AddFieldAsync<RatingFieldDefinition>();
            var r = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Last(f => f.IsRating);
            r.MaxStars = 10;
            r.Label = "Score";
        }, r => r.Label == "Score");

        Assert.That(row.MaxStars, Is.EqualTo(10));
    }

    [Test]
    public async Task ImageField_Sizing_RoundTrips()
    {
        var row = await SaveAndGetRow(async sut =>
        {
            await sut.AddFieldAsync<ImageFieldDefinition>();
            var r = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Last(f => f.IsPicture);
            r.DisplayWidth = 320;
            r.DisplayHeight = 240;
            r.SizeMode = ImageSizeMode.Max;
            r.Label = "Cover";
        }, r => r.Label == "Cover");

        Assert.That(row.DisplayWidth, Is.EqualTo(320));
        Assert.That(row.DisplayHeight, Is.EqualTo(240));
        Assert.That(row.SizeMode, Is.EqualTo(ImageSizeMode.Max));
    }

    [Test]
    public async Task ListField_ColumnCountAndInlineStyle_RoundTrip()
    {
        var row = await SaveAndGetRow(async sut =>
        {
            await sut.AddFieldAsync<ListFieldDefinition>();
            var r = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Last(f => f.IsList);
            r.ColumnCount = 3;
            r.InlineStyle = ListInlineStyle.Grid;
            r.Label = "Episodes";
        }, r => r.Label == "Episodes");

        Assert.That(row.ColumnCount, Is.EqualTo(3));
        Assert.That(row.InlineStyle, Is.EqualTo(ListInlineStyle.Grid));
    }

    [Test]
    public async Task ListField_SubField_RoundTrips()
    {
        var sut = MakePresetEditorVm();
        await sut.LoadAsync();
        sut.Name = "P";
        await sut.AddFieldAsync<ListFieldDefinition>();
        var listRow = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Last(f => f.IsList);
        listRow.Label = "Chapters";

        sut.DrillIntoCommand.Execute(listRow);
        await sut.AddFieldAsync<TextFieldDefinition>();
        var sub = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Last(f => !f.IsDisplayName);
        sub.Label = "Title";

        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        var saved = (await PresetRepo.GetAllAsync())[0];
        var listDef = saved.Fields.OfType<ListFieldDefinition>().First(f => f.Label == "Chapters");
        Assert.That(listDef.SubFields, Has.Count.EqualTo(1));
        Assert.That(listDef.SubFields[0].Label, Is.EqualTo("Title"));
    }

    [Test]
    public async Task SingleChoiceField_Choices_RoundTrip()
    {
        var sut = MakePresetEditorVm();
        await sut.LoadAsync();
        sut.Name = "P";
        await sut.AddFieldAsync<SingleChoiceFieldDefinition>();
        var r = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Last(f => f.HasChoices);
        r.Label = "Genre";
        r.AddChoiceCommand.Execute(null);
        r.ChoiceItems[0].Value = "Fiction";
        r.AddChoiceCommand.Execute(null);
        r.ChoiceItems[1].Value = "Non-Fiction";
        r.AddChoiceCommand.Execute(null);
        r.ChoiceItems[2].Value = "Science";

        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        var saved = (await PresetRepo.GetAllAsync())[0];
        var def = saved.Fields.OfType<SingleChoiceFieldDefinition>().First(f => f.Label == "Genre");
        Assert.That(def.Choices, Has.Count.EqualTo(3));
        var values = def.Choices.OrderBy(c => c.DisplayOrder).Select(c => c.Value).ToList();
        Assert.That(values, Does.Contain("Fiction"));
        Assert.That(values, Does.Contain("Non-Fiction"));
        Assert.That(values, Does.Contain("Science"));
    }

    [Test]
    public async Task MultiChoiceField_Choices_RoundTrip()
    {
        var sut = MakePresetEditorVm();
        await sut.LoadAsync();
        sut.Name = "P";
        await sut.AddFieldAsync<MultiChoiceFieldDefinition>();
        var r = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Last(f => f.HasChoices);
        r.Label = "Tags";
        r.AddChoiceCommand.Execute(null);
        r.ChoiceItems[0].Value = "Alpha";
        r.AddChoiceCommand.Execute(null);
        r.ChoiceItems[1].Value = "Beta";

        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        var saved = (await PresetRepo.GetAllAsync())[0];
        var def = saved.Fields.OfType<MultiChoiceFieldDefinition>().First(f => f.Label == "Tags");
        Assert.That(def.Choices, Has.Count.EqualTo(2));
        Assert.That(def.Choices.Select(c => c.Value), Does.Contain("Alpha"));
        Assert.That(def.Choices.Select(c => c.Value), Does.Contain("Beta"));
    }

    [Test]
    public async Task RemoveChoice_SurvivorStaysIntact()
    {
        var sut = MakePresetEditorVm();
        await sut.LoadAsync();
        sut.Name = "P";
        await sut.AddFieldAsync<SingleChoiceFieldDefinition>();
        var r = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Last(f => f.HasChoices);
        r.Label = "Status";
        r.AddChoiceCommand.Execute(null);
        r.ChoiceItems[0].Value = "Active";
        r.AddChoiceCommand.Execute(null);
        r.ChoiceItems[1].Value = "Inactive";
        r.AddChoiceCommand.Execute(null);
        r.ChoiceItems[2].Value = "Pending";

        r.RemoveChoiceCommand.Execute(r.ChoiceItems[1]);

        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        var saved = (await PresetRepo.GetAllAsync())[0];
        var def = saved.Fields.OfType<SingleChoiceFieldDefinition>().First(f => f.Label == "Status");
        Assert.That(def.Choices, Has.Count.EqualTo(2));
        Assert.That(def.Choices.Select(c => c.Value), Does.Contain("Active"));
        Assert.That(def.Choices.Select(c => c.Value), Does.Contain("Pending"));
        Assert.That(def.Choices.Select(c => c.Value), Does.Not.Contain("Inactive"));
    }

    [Test]
    public async Task GroupWithFieldsAndColumnCount_RoundTrips()
    {
        var sut = MakePresetEditorVm();
        await sut.LoadAsync();
        sut.Name = "P";
        sut.AddGroupCommand.Execute(null);
        var group = sut.CurrentRows.OfType<FieldGroupRowViewModel>().First();
        group.Name = "Specs";
        group.ColumnCount = 2;

        sut.DrillIntoCommand.Execute(group);
        await sut.AddFieldAsync<TextFieldDefinition>();
        var fieldRow = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Last(f => !f.IsDisplayName);
        fieldRow.Label = "Notes";
        fieldRow.ColumnSpan = 2;

        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        var saved = (await PresetRepo.GetAllAsync())[0];
        var savedGroup = saved.Groups.First(g => g.Name == "Specs");
        Assert.That(savedGroup.ColumnCount, Is.EqualTo(2));

        var savedField = saved.Fields.First(f => f.Label == "Notes");
        Assert.That(savedField.GroupId, Is.EqualTo(savedGroup.Id));
        Assert.That(savedField.ColumnSpan, Is.EqualTo(2));
    }
}

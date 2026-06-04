using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Tests.Infrastructure;

namespace Collectary.UI.Tests.ItemEditor;

[TestFixture]
public class FieldValueRoundTripTest : FlowTestBase
{
    private async Task<TEditor> RoundTrip<TEditor>(
        FieldDefinition fieldDef,
        Action<TEditor> setValue)
        where TEditor : FieldEditorViewModelBase
    {
        var preset = new Preset
        {
            Name = "P",
            Fields = [new DisplayNameFieldDefinition { IsRequired = false }, fieldDef]
        };
        await PresetUseCase.CreatePresetAsync(preset);
        var ef = await PresetUseCase.GetEffectiveFieldsAsync(preset.Id);

        var vm = MakeItemEditorVm(preset, ef);
        SetDisplayName(vm, "Test");
        var editor = vm.FieldEditors.OfType<TEditor>().Single();
        setValue(editor);
        await vm.PersistAsync();

        var saved = (await ItemUseCase.GetItemsForPresetAsync(preset.Id))[0];
        var vm2 = MakeItemEditorVm(preset, ef, existing: saved);
        return vm2.FieldEditors.OfType<TEditor>().Single();
    }

    [Test]
    public async Task TextField_Value_RoundTrips()
    {
        var def = new TextFieldDefinition { Label = "Note" };
        var reloaded = await RoundTrip<TextFieldEditorViewModel>(def, e => e.Text = "hello world");
        Assert.That(reloaded.Text, Is.EqualTo("hello world"));
    }

    [Test]
    public async Task TextField_Null_RoundTrips()
    {
        var def = new TextFieldDefinition { Label = "Note" };
        var reloaded = await RoundTrip<TextFieldEditorViewModel>(def, e => e.Text = null);
        Assert.That(reloaded.Text, Is.Null.Or.Empty);
    }

    [Test]
    public async Task BoolField_True_RoundTrips()
    {
        var def = new BoolFieldDefinition { Label = "Read" };
        var reloaded = await RoundTrip<BoolFieldEditorViewModel>(def, e => e.IsChecked = true);
        Assert.That(reloaded.IsChecked, Is.True);
    }

    [Test]
    public async Task BoolField_False_RoundTrips()
    {
        var def = new BoolFieldDefinition { Label = "Read" };
        var reloaded = await RoundTrip<BoolFieldEditorViewModel>(def, e => e.IsChecked = false);
        Assert.That(reloaded.IsChecked, Is.False);
    }

    [Test]
    public async Task IntegerField_Value_RoundTrips()
    {
        var def = new IntegerFieldDefinition { Label = "Pages" };
        var reloaded = await RoundTrip<IntegerFieldEditorViewModel>(def, e => e.Number = 342);
        Assert.That(reloaded.Number, Is.EqualTo(342));
    }

    [Test]
    public async Task IntegerField_Null_RoundTrips()
    {
        var def = new IntegerFieldDefinition { Label = "Pages" };
        var reloaded = await RoundTrip<IntegerFieldEditorViewModel>(def, e => e.Number = null);
        Assert.That(reloaded.Number, Is.Null);
    }

    [Test]
    public async Task DecimalField_Value_RoundTrips()
    {
        var def = new DecimalFieldDefinition { Label = "Weight" };
        var reloaded = await RoundTrip<DecimalFieldEditorViewModel>(def, e => e.Number = 3.14m);
        Assert.That(reloaded.Number, Is.EqualTo(3.14m));
    }

    [Test]
    public async Task DateField_Value_RoundTrips()
    {
        var expected = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var def = new DateFieldDefinition { Label = "Published" };
        var reloaded = await RoundTrip<DateFieldEditorViewModel>(def, e => e.Date = expected);
        Assert.That(reloaded.Date, Is.EqualTo(expected));
    }

    [Test]
    public async Task TimeField_Value_RoundTrips()
    {
        var def = new TimeFieldDefinition { Label = "Time" };
        var reloaded = await RoundTrip<TimeFieldEditorViewModel>(def, e =>
        {
            e.Hour = 14;
            e.Minute = 30;
        });
        Assert.That(reloaded.Hour, Is.EqualTo(14));
        Assert.That(reloaded.Minute, Is.EqualTo(30));
    }

    [Test]
    public async Task DurationField_Value_RoundTrips()
    {
        var def = new DurationFieldDefinition { Label = "Duration" };
        var reloaded = await RoundTrip<DurationFieldEditorViewModel>(def, e =>
        {
            e.Hours = 1;
            e.Minutes = 30;
        });
        Assert.That(reloaded.Hours, Is.EqualTo(1));
        Assert.That(reloaded.Minutes, Is.EqualTo(30));
    }

    [Test]
    public async Task CurrencyField_Value_RoundTrips()
    {
        var def = new CurrencyFieldDefinition { Label = "Price", CurrencySymbol = "€" };
        var reloaded = await RoundTrip<CurrencyFieldEditorViewModel>(def, e => e.Amount = 29.99m);
        Assert.That(reloaded.Amount, Is.EqualTo(29.99m));
    }

    [Test]
    public async Task PercentageField_Value_RoundTrips()
    {
        var def = new PercentageFieldDefinition { Label = "Progress" };
        var reloaded = await RoundTrip<PercentageFieldEditorViewModel>(def, e => e.Number = 0.75m);
        Assert.That(reloaded.Number, Is.EqualTo(0.75m));
    }

    [Test]
    public async Task RatingField_Stars_RoundTrip()
    {
        var def = new RatingFieldDefinition { Label = "Rating", MaxStars = 5 };
        var reloaded = await RoundTrip<RatingFieldEditorViewModel>(def, e => e.Stars = 4);
        Assert.That(reloaded.Stars, Is.EqualTo(4));
    }

    [Test]
    public async Task RatingField_ZeroStars_StoredAsNull()
    {
        var def = new RatingFieldDefinition { Label = "Rating" };
        var reloaded = await RoundTrip<RatingFieldEditorViewModel>(def, e => e.Stars = 0);
        Assert.That(reloaded.Stars, Is.EqualTo(0));
    }

    [Test]
    public async Task UrlField_Value_RoundTrips()
    {
        var def = new UrlFieldDefinition { Label = "Website" };
        var reloaded = await RoundTrip<UrlFieldEditorViewModel>(def, e => e.Url = "https://example.com");
        Assert.That(reloaded.Url, Is.EqualTo("https://example.com"));
    }

    [Test]
    public async Task EmailField_Value_RoundTrips()
    {
        var def = new EmailFieldDefinition { Label = "Email" };
        var reloaded = await RoundTrip<EmailFieldEditorViewModel>(def, e => e.Text = "test@example.com");
        Assert.That(reloaded.Text, Is.EqualTo("test@example.com"));
    }

    [Test]
    public async Task PhoneField_Value_RoundTrips()
    {
        var def = new PhoneFieldDefinition { Label = "Phone" };
        var reloaded = await RoundTrip<PhoneFieldEditorViewModel>(def, e => e.Text = "+491234567890");
        Assert.That(reloaded.Text, Is.EqualTo("+491234567890"));
    }

    [Test]
    public async Task RichTextField_Value_RoundTrips()
    {
        var def = new RichTextFieldDefinition { Label = "Bio" };
        var reloaded = await RoundTrip<RichTextFieldEditorViewModel>(def, e => e.Markdown = "**Bold** text");
        Assert.That(reloaded.Markdown, Is.EqualTo("**Bold** text"));
    }

    [Test]
    public async Task TagsField_Values_RoundTrip()
    {
        var def = new TagsFieldDefinition { Label = "Tags" };
        var reloaded = await RoundTrip<TagsFieldEditorViewModel>(def, e =>
        {
            e.Tags.Add("alpha");
            e.Tags.Add("beta");
        });
        Assert.That(reloaded.Tags, Does.Contain("alpha"));
        Assert.That(reloaded.Tags, Does.Contain("beta"));
        Assert.That(reloaded.Tags, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task SingleChoiceField_Selected_RoundTrips()
    {
        var def = new SingleChoiceFieldDefinition
        {
            Label = "Genre",
            Choices =
            [
                new ChoiceOption { Value = "Fiction", DisplayOrder = 0 },
                new ChoiceOption { Value = "Non-Fiction", DisplayOrder = 1 }
            ]
        };
        var reloaded = await RoundTrip<SingleChoiceFieldEditorViewModel>(def, e => e.Selected = "Fiction");
        Assert.That(reloaded.Selected, Is.EqualTo("Fiction"));
    }

    [Test]
    public async Task SingleChoiceField_Null_RoundTrips()
    {
        var def = new SingleChoiceFieldDefinition
        {
            Label = "Genre",
            Choices = [new ChoiceOption { Value = "Fiction", DisplayOrder = 0 }]
        };
        var reloaded = await RoundTrip<SingleChoiceFieldEditorViewModel>(def, e => e.Selected = null);
        Assert.That(reloaded.Selected, Is.Null.Or.Empty);
    }

    [Test]
    public async Task MultiChoiceField_MultipleSelected_RoundTrip()
    {
        var def = new MultiChoiceFieldDefinition
        {
            Label = "Skills",
            Choices =
            [
                new ChoiceOption { Value = "A", DisplayOrder = 0 },
                new ChoiceOption { Value = "B", DisplayOrder = 1 },
                new ChoiceOption { Value = "C", DisplayOrder = 2 }
            ]
        };
        var reloaded = await RoundTrip<MultiChoiceFieldEditorViewModel>(def, e =>
        {
            e.ChoiceItems[0].IsSelected = true;
            e.ChoiceItems[2].IsSelected = true;
        });
        var selected = reloaded.ChoiceItems.Where(c => c.IsSelected).Select(c => c.Label).ToList();
        Assert.That(selected, Does.Contain("A"));
        Assert.That(selected, Does.Contain("C"));
        Assert.That(selected, Does.Not.Contain("B"));
    }

    [Test]
    public async Task ColorField_Hex_RoundTrips()
    {
        var def = new ColorFieldDefinition { Label = "Color", Format = ColorFormat.Hex };
        var reloaded = await RoundTrip<ColorFieldEditorViewModel>(def, e =>
        {
            var hex = (HexColorFormatEditorViewModel)e.SubEditor;
            hex.Hex = "#FF5733";
        });
        var hexEditor = (HexColorFormatEditorViewModel)reloaded.SubEditor;
        Assert.That(hexEditor.Hex, Is.EqualTo("#FF5733"));
    }

    [Test]
    public async Task DisplayNameField_SetsItemDisplayName_NotInValues()
    {
        var preset = new Preset
        {
            Name = "P",
            Fields = [new DisplayNameFieldDefinition { IsRequired = false }]
        };
        await PresetUseCase.CreatePresetAsync(preset);
        var ef = await PresetUseCase.GetEffectiveFieldsAsync(preset.Id);

        var vm = MakeItemEditorVm(preset, ef);
        var dnEditor = vm.FieldEditors.OfType<DisplayNameFieldEditorViewModel>().Single();
        dnEditor.Text = "My Item";
        await vm.PersistAsync();

        var saved = (await ItemUseCase.GetItemsForPresetAsync(preset.Id))[0];
        Assert.That(saved.DisplayName, Is.EqualTo("My Item"));
        Assert.That(saved.Values, Is.Empty, "DisplayName must not produce a FieldValue entry");
    }
}

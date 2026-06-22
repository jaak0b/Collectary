using Avalonia.Media.Imaging;
using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.DI;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Tests.Infrastructure;

namespace Collectary.UI.Tests.ItemEditor;

[TestFixture]
public class FieldValueRoundTripTest : FlowTestBase
{
    private static ItemEditingContext BareContext() => new(
        editorRegistry: A.Fake<IFieldEditorRegistry>(),
        listCellBuilder: A.Fake<IListCellBuilder>(),
        goBack: () => { },
        pickAndStoreImageAsync: () => Task.FromResult<(string, string, Bitmap)?>(null),
        exportImageAsync: (_, _) => Task.CompletedTask,
        loadImageBitmap: _ => null,
        deleteImageAsync: _ => Task.CompletedTask);

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
        var expected = new DateTime(2024, 6, 1);
        var def = new DateFieldDefinition { Label = "Published" };
        var reloaded = await RoundTrip<DateFieldEditorViewModel>(def, e => e.Date = expected);
        Assert.That(reloaded.Date, Is.EqualTo(expected));
    }

    [Test]
    public async Task DateField_WithTime_RoundTripsDateAndTime()
    {
        var def = new DateFieldDefinition { Label = "Logged at", WithTime = true };
        var reloaded = await RoundTrip<DateFieldEditorViewModel>(def, e =>
        {
            e.Date = new DateTime(2024, 6, 1);
            e.Time = new TimeSpan(14, 30, 0);
        });
        Assert.That(reloaded.Date!.Value.Date, Is.EqualTo(new DateTime(2024, 6, 1)));
        Assert.That(reloaded.Time, Is.EqualTo(new TimeSpan(14, 30, 0)));
    }


    [Test]
    public async Task DurationField_Value_RoundTrips()
    {
        var def = new DurationFieldDefinition { Label = "Duration" };
        var reloaded = await RoundTrip<DurationFieldEditorViewModel>(def, e => e.Text = "1h 30m");
        Assert.That(reloaded.Text, Is.EqualTo("1h 30m"));
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
    public async Task BarcodeField_Value_RoundTrips()
    {
        var def = new BarcodeFieldDefinition { Label = "Barcode" };
        var reloaded = await RoundTrip<BarcodeFieldEditorViewModel>(def, e => e.Code = "5901234123457");
        Assert.That(reloaded.Code, Is.EqualTo("5901234123457"));
    }

    [Test]
    public async Task QrCodeField_Value_RoundTrips()
    {
        var def = new QrCodeFieldDefinition { Label = "Label" };
        var reloaded = await RoundTrip<QrCodeFieldEditorViewModel>(def, e => e.Content = "SHELF-A1");
        Assert.That(reloaded.Content, Is.EqualTo("SHELF-A1"));
    }

    [Test]
    public async Task MultiImageField_Keys_RoundTrip()
    {
        var def = new MultiImageFieldDefinition { Label = "Photos" };
        var reloaded = await RoundTrip<MultiImageFieldEditorViewModel>(def, e =>
        {
            e.Images.Add(new MultiImageEntryViewModel("img-1", "first.jpg", null, BareContext(), _ => Task.CompletedTask));
            e.Images.Add(new MultiImageEntryViewModel("img-2", "second.png", null, BareContext(), _ => Task.CompletedTask));
        });
        Assert.That(reloaded.Images.Select(i => i.Key), Is.EqualTo(new[] { "img-1", "img-2" }));
        Assert.That(reloaded.Images.Select(i => i.FileName), Is.EqualTo(new[] { "first.jpg", "second.png" }));
    }

    [Test]
    public async Task FileAttachmentField_Files_RoundTrip()
    {
        var def = new FileAttachmentFieldDefinition { Label = "Docs" };
        var reloaded = await RoundTrip<FileAttachmentFieldEditorViewModel>(def, e =>
        {
            e.Attachments.Add(new FileAttachmentEntryViewModel("k1", "manual.pdf", BareContext(), _ => Task.CompletedTask));
            e.Attachments.Add(new FileAttachmentEntryViewModel("k2", "warranty.pdf", BareContext(), _ => Task.CompletedTask));
        });
        Assert.That(reloaded.Attachments.Select(a => a.FileName), Is.EqualTo(new[] { "manual.pdf", "warranty.pdf" }));
        Assert.That(reloaded.Attachments.Select(a => a.Key), Is.EqualTo(new[] { "k1", "k2" }));
    }

    [Test]
    public async Task FileAttachmentField_EditedName_RoundTripsWithSameKey()
    {
        var def = new FileAttachmentFieldDefinition { Label = "Docs" };
        var reloaded = await RoundTrip<FileAttachmentFieldEditorViewModel>(def, e =>
        {
            var entry = new FileAttachmentEntryViewModel("k1", "manual.pdf", BareContext(), _ => Task.CompletedTask);
            entry.EditingName = "owner-handbook";
            e.Attachments.Add(entry);
        });
        Assert.That(reloaded.Attachments.Single().Key, Is.EqualTo("k1"));
        Assert.That(reloaded.Attachments.Single().FileName, Is.EqualTo("owner-handbook.pdf"));
    }

    [Test]
    public async Task CountryField_Value_RoundTrips()
    {
        var def = new CountryFieldDefinition { Label = "Origin" };
        var reloaded = await RoundTrip<CountryFieldEditorViewModel>(def,
            e => e.SelectedCountry = e.Countries.First(c => c.Code == "JP"));
        Assert.That(reloaded.SelectedCountry?.Code, Is.EqualTo("JP"));
    }

    [Test]
    public async Task MeasurementField_Value_RoundTrips()
    {
        var def = new MeasurementFieldDefinition { Label = "Diameter" };
        var reloaded = await RoundTrip<MeasurementFieldEditorViewModel>(def, e =>
        {
            e.Amount = 38m;
            e.SelectedUnit = "cm";
        });
        Assert.That(reloaded.Amount, Is.EqualTo(38m));
        Assert.That(reloaded.SelectedUnit, Is.EqualTo("cm"));
    }

    [Test]
    public async Task WeightField_Value_RoundTrips()
    {
        var def = new WeightFieldDefinition { Label = "Weight" };
        var reloaded = await RoundTrip<WeightFieldEditorViewModel>(def, e =>
        {
            e.Amount = 31.1m;
            e.SelectedUnit = "oz";
        });
        Assert.That(reloaded.Amount, Is.EqualTo(31.1m));
        Assert.That(reloaded.SelectedUnit, Is.EqualTo("oz"));
    }

    [Test]
    public async Task DateRangeField_Value_RoundTrips()
    {
        var from = new DateTime(2018, 5, 1);
        var to = new DateTime(2020, 6, 30);
        var def = new DateRangeFieldDefinition { Label = "Period" };
        var reloaded = await RoundTrip<DateRangeFieldEditorViewModel>(def, e =>
        {
            e.From = from;
            e.To = to;
        });
        Assert.That(reloaded.From, Is.EqualTo(from));
        Assert.That(reloaded.To, Is.EqualTo(to));
    }

    [Test]
    public async Task LinkedItemField_Value_RoundTrips()
    {
        var targetId = Guid.NewGuid();
        var def = new LinkedItemFieldDefinition { Label = "Belongs to" };
        var reloaded = await RoundTrip<LinkedItemFieldEditorViewModel>(def,
            e => e.SelectedItem = new LinkedItemOption(targetId, "Millennium Falcon"));
        Assert.That(reloaded.SelectedItem?.Id, Is.EqualTo(targetId));
        Assert.That(reloaded.SelectedItem?.Display, Is.EqualTo("Millennium Falcon"));
    }

    [Test]
    public async Task AudioField_Value_RoundTrips()
    {
        var def = new AudioFieldDefinition { Label = "Voice note" };
        var reloaded = await RoundTrip<AudioFieldEditorViewModel>(def, e =>
        {
            e.AudioKey = "audio-1";
            e.DurationSeconds = 8;
        });
        Assert.That(reloaded.AudioKey, Is.EqualTo("audio-1"));
        Assert.That(reloaded.DurationSeconds, Is.EqualTo(8));
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

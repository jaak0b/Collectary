using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Tests.Infrastructure;

namespace Collectary.UI.Tests.ItemEditor;

[TestFixture]
public class RandomizeFieldEditorTest : FlowTestBase
{
    private static SingleChoiceFieldDefinition SingleChoiceDef() =>
        new()
        {
            Label = "Choice",
            Choices =
            [
                new ChoiceOption { Value = "A", DisplayOrder = 0 },
                new ChoiceOption { Value = "B", DisplayOrder = 1 }
            ]
        };

    private static MultiChoiceFieldDefinition MultiChoiceDef() =>
        new()
        {
            Label = "Multi",
            Choices =
            [
                new ChoiceOption { Value = "A", DisplayOrder = 0 },
                new ChoiceOption { Value = "B", DisplayOrder = 1 }
            ]
        };

    private static IEnumerable<FieldDefinition> RandomizableDefs() =>
    [
        new TextFieldDefinition { Label = "Text" },
        new RichTextFieldDefinition { Label = "Rich" },
        new IntegerFieldDefinition { Label = "Int" },
        new DecimalFieldDefinition { Label = "Dec" },
        new PercentageFieldDefinition { Label = "Pct" },
        new CurrencyFieldDefinition { Label = "Cur" },
        new DateFieldDefinition { Label = "Date" },
        new TimeFieldDefinition { Label = "Time" },
        new DateRangeFieldDefinition { Label = "Range" },
        new DurationFieldDefinition { Label = "Dur" },
        new BoolFieldDefinition { Label = "Bool" },
        SingleChoiceDef(),
        MultiChoiceDef(),
        new TagsFieldDefinition { Label = "Tags" },
        new CountryFieldDefinition { Label = "Country" },
        new RatingFieldDefinition { Label = "Rating" },
        new EmailFieldDefinition { Label = "Email" },
        new UrlFieldDefinition { Label = "Url" },
        new PhoneFieldDefinition { Label = "Phone" },
        new BarcodeFieldDefinition { Label = "Barcode" },
        new QrCodeFieldDefinition { Label = "Qr" },
        new MeasurementFieldDefinition { Label = "Measure" },
        new WeightFieldDefinition { Label = "Weight" }
    ];

    private static IEnumerable<FieldDefinition> MediaAndLinkedDefs() =>
    [
        new ImageFieldDefinition { Label = "Image" },
        new MultiImageFieldDefinition { Label = "Gallery" },
        new AudioFieldDefinition { Label = "Audio" },
        new FileAttachmentFieldDefinition { Label = "File" },
        new LinkedItemFieldDefinition { Label = "Link" }
    ];

    [Test]
    public void Randomize_FillsEveryRandomizableEditor()
    {
        var data = new BogusSampleData(99);
        var ctx = MakeItemContext();

        Assert.Multiple(() =>
        {
            foreach (var def in RandomizableDefs())
            {
                var editor = EditorRegistry.Create(def, null, ctx)!;
                editor.Randomize(data);
                Assert.That(editor.GetCurrentValue().IsEmpty, Is.False, def.GetType().Name);
            }
        });
    }

    [Test]
    public void Randomize_LeavesMediaAndLinkedEditorsEmpty()
    {
        var data = new BogusSampleData(99);
        var ctx = MakeItemContext();

        Assert.Multiple(() =>
        {
            foreach (var def in MediaAndLinkedDefs())
            {
                var editor = EditorRegistry.Create(def, null, ctx)!;
                editor.Randomize(data);
                Assert.That(editor.GetCurrentValue().IsEmpty, Is.True, def.GetType().Name);
            }
        });
    }

    [Test]
    public void Randomize_Integer_RespectsMinAndMax()
    {
        var data = new BogusSampleData(3);
        var def = new IntegerFieldDefinition { Label = "n", Min = 10, Max = 12 };
        var editor = (IntegerFieldEditorViewModel)EditorRegistry.Create(def, null, MakeItemContext())!;

        for (var i = 0; i < 50; i++)
        {
            editor.Randomize(data);
            Assert.That(editor.Number, Is.InRange(10, 12));
        }
    }

    [Test]
    public void Randomize_SingleChoice_PicksAnOfferedChoice()
    {
        var data = new BogusSampleData(3);
        var def = SingleChoiceDef();
        var editor = (SingleChoiceFieldEditorViewModel)EditorRegistry.Create(def, null, MakeItemContext())!;

        editor.Randomize(data);

        Assert.That(new[] { "A", "B" }, Does.Contain(editor.Selected));
    }

    [Test]
    public void Randomize_List_AddsEntriesWithFilledSubValues()
    {
        var listDef = new ListFieldDefinition { Label = "L" };
        var sub1 = new TextFieldDefinition { Label = "s1", DisplayOrder = 0, ParentListFieldDefinitionId = listDef.Id };
        var sub2 = new IntegerFieldDefinition { Label = "s2", DisplayOrder = 1, ParentListFieldDefinitionId = listDef.Id };
        listDef.SubFields.Add(sub1);
        listDef.SubFields.Add(sub2);

        var editor = (ListFieldEditorViewModel)EditorRegistry.Create(listDef, null, MakeItemContext())!;
        editor.Randomize(new BogusSampleData(5));

        var value = (ListFieldValue)editor.GetCurrentValue();
        Assert.That(value.Entries, Is.Not.Empty);
        Assert.That(value.Entries[0].SubValues.Any(v => !v.IsEmpty), Is.True);
    }

    [Test]
    public void Randomize_Base_NoOp_DoesNotThrow_AndLeavesValueUnset()
    {
        var data = new BogusSampleData(1);
        var editor = EditorRegistry.Create(new ImageFieldDefinition { Label = "Img" }, null, MakeItemContext())!;

        Assert.DoesNotThrow(() => editor.Randomize(data));
    }
}

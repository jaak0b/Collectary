#if DEBUG
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.Templates.Catalog;

public sealed class DeveloperTemplate : PresetTemplateBase, IPresetTemplate
{
    public string Key => "developer";
    public PresetTemplateCategory Category => PresetTemplateCategory.Developer;
    public string Icon => IconGlyphs.Options;
    public string NameKey => "Tmpl_Developer_Name";
    public string DescriptionKey => "Tmpl_Developer_Desc";

    public Preset Build()
    {
        var measurement = new MeasurementFieldDefinition { Label = L("FieldType_Measurement") };
        var weight = new WeightFieldDefinition { Label = L("FieldType_Weight") };
        var specs = Group("Tmpl_Developer_Group", columns: 2, measurement, weight);

        var fields = new FieldDefinition[]
        {
            Title("FieldType_Text"),
            Text("FieldType_Text"),
            RichText("FieldType_RichText"),
            Integer("FieldType_Integer"),
            new AutoNumberFieldDefinition { Label = L("FieldType_AutoNumber") },
            Decimal("FieldType_Decimal"),
            Percentage("FieldType_Percentage"),
            Currency("FieldType_Currency"),
            Date("FieldType_Date"),
            new DateRangeFieldDefinition { Label = L("FieldType_DateRange") },
            Duration("FieldType_Duration"),
            Bool("FieldType_Bool"),
            SingleChoice("FieldType_SingleChoice",
                "Tmpl_Developer_Opt1", "Tmpl_Developer_Opt2", "Tmpl_Developer_Opt3"),
            MultiChoice("FieldType_MultiChoice",
                "Tmpl_Developer_Opt1", "Tmpl_Developer_Opt2", "Tmpl_Developer_Opt3"),
            new TagsFieldDefinition { Label = L("FieldType_Tags") },
            new CountryFieldDefinition { Label = L("FieldType_Country") },
            new LinkedItemFieldDefinition { Label = L("FieldType_LinkedItem") },
            Color("FieldType_Color"),
            Rating("FieldType_Rating"),
            Image("FieldType_Image"),
            new MultiImageFieldDefinition { Label = L("FieldType_MultiImage") },
            new AudioFieldDefinition { Label = L("FieldType_Audio") },
            new QrCodeFieldDefinition { Label = L("FieldType_QrCode") },
            new BarcodeFieldDefinition { Label = L("FieldType_Barcode") },
            new FileAttachmentFieldDefinition { Label = L("FieldType_FileAttachment") },
            new EmailFieldDefinition { Label = L("FieldType_Email") },
            new UrlFieldDefinition { Label = L("FieldType_Url") },
            new PhoneFieldDefinition { Label = L("FieldType_Phone") },
            measurement,
            weight,
            List("FieldType_List", ListInlineStyle.Card,
                Text("FieldType_Text"),
                Integer("FieldType_Integer"),
                SingleChoice("FieldType_SingleChoice",
                    "Tmpl_Developer_Opt1", "Tmpl_Developer_Opt2", "Tmpl_Developer_Opt3")),
        };

        return Compose(NameKey, columns: 2, fields, new[] { specs });
    }
}
#endif

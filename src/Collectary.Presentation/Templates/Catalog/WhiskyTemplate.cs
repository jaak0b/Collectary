using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.Templates.Catalog;

public sealed class WhiskyTemplate : PresetTemplateBase, IPresetTemplate
{
    public string Key => "whisky";
    public PresetTemplateCategory Category => PresetTemplateCategory.Lifestyle;
    public string Icon => "🥃";
    public string NameKey => "Tmpl_whisky_Name";
    public string DescriptionKey => "Tmpl_whisky_Desc";

    public Preset Build() => Compose(NameKey, columns: 2, fields: new FieldDefinition[]
    {
        Title("Tmpl_whisky_f_Name"),
        Text("Tmpl_whisky_f_Distillery", showInList: true),
        Integer("Tmpl_whisky_f_Age"),
        SingleChoice("Tmpl_whisky_f_Type",
            "Tmpl_whisky_c_SingleMalt", "Tmpl_whisky_c_Blend", "Tmpl_whisky_c_Bourbon",
            "Tmpl_whisky_c_Rye", "Tmpl_whisky_c_Other"),
        Percentage("Tmpl_whisky_f_ABV"),
        Currency("Tmpl_whisky_f_Price"),
        Rating("Tmpl_whisky_f_Rating"),
        RichText("Tmpl_whisky_f_TastingNotes"),
    });
}

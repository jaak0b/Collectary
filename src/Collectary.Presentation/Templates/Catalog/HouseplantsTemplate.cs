using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.Templates.Catalog;

public sealed class HouseplantsTemplate : PresetTemplateBase, IPresetTemplate
{
    public string Key => "houseplants";
    public PresetTemplateCategory Category => PresetTemplateCategory.Lifestyle;
    public string Icon => "🌿";
    public string NameKey => "Tmpl_houseplants_Name";
    public string DescriptionKey => "Tmpl_houseplants_Desc";

    public Preset Build() => Compose(NameKey, columns: 2, fields: new FieldDefinition[]
    {
        Title("Tmpl_houseplants_f_Name"),
        Text("Tmpl_houseplants_f_Species", showInList: true),
        Text("Tmpl_houseplants_f_Location"),
        SingleChoice("Tmpl_houseplants_f_Watering",
            "Tmpl_houseplants_c_Daily", "Tmpl_houseplants_c_Weekly",
            "Tmpl_houseplants_c_Biweekly", "Tmpl_houseplants_c_Monthly"),
        Date("Tmpl_houseplants_f_Acquired"),
        Image("Tmpl_houseplants_f_Photo"),
        RichText("Tmpl_houseplants_f_Notes"),
    });
}

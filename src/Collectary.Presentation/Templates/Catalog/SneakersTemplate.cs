using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.Templates.Catalog;

public sealed class SneakersTemplate : PresetTemplateBase, IPresetTemplate
{
    public string Key => "sneakers";
    public PresetTemplateCategory Category => PresetTemplateCategory.Lifestyle;
    public string Icon => IconGlyphs.PersonWalking;
    public string NameKey => "Tmpl_sneakers_Name";
    public string DescriptionKey => "Tmpl_sneakers_Desc";

    public Preset Build() => Compose(NameKey, columns: 2, fields: new FieldDefinition[]
    {
        Title("Tmpl_sneakers_f_Model"),
        Text("Tmpl_sneakers_f_Brand", showInList: true),
        Text("Tmpl_sneakers_f_Colorway"),
        Decimal("Tmpl_sneakers_f_Size"),
        Date("Tmpl_sneakers_f_ReleaseDate"),
        Currency("Tmpl_sneakers_f_RetailPrice"),
        SingleChoice("Tmpl_sneakers_f_Condition",
            "Tmpl_sneakers_c_Deadstock", "Tmpl_sneakers_c_Worn", "Tmpl_sneakers_c_Beaters"),
        Image("Tmpl_sneakers_f_Photo"),
    });
}

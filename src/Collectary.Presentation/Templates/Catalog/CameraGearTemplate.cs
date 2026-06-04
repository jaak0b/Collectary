using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.Templates.Catalog;

public sealed class CameraGearTemplate : PresetTemplateBase, IPresetTemplate
{
    public string Key => "cameragear";
    public PresetTemplateCategory Category => PresetTemplateCategory.Practical;
    public string Icon => "📷";
    public string NameKey => "Tmpl_cameragear_Name";
    public string DescriptionKey => "Tmpl_cameragear_Desc";

    public Preset Build() => Compose(NameKey, columns: 2, fields: new FieldDefinition[]
    {
        Title("Tmpl_cameragear_f_Model"),
        Text("Tmpl_cameragear_f_Brand", showInList: true),
        SingleChoice("Tmpl_cameragear_f_Type",
            "Tmpl_cameragear_c_Body", "Tmpl_cameragear_c_Lens", "Tmpl_cameragear_c_Accessory"),
        Text("Tmpl_cameragear_f_SerialNumber"),
        Date("Tmpl_cameragear_f_PurchaseDate"),
        Currency("Tmpl_cameragear_f_Price"),
        Image("Tmpl_cameragear_f_Photo"),
    });
}

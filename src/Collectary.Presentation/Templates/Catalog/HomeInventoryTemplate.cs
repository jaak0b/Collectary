using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.Templates.Catalog;

public sealed class HomeInventoryTemplate : PresetTemplateBase, IPresetTemplate
{
    public string Key => "homeinventory";
    public PresetTemplateCategory Category => PresetTemplateCategory.Practical;
    public string Icon => "🏠";
    public string NameKey => "Tmpl_homeinventory_Name";
    public string DescriptionKey => "Tmpl_homeinventory_Desc";

    public Preset Build() => Compose(NameKey, columns: 2, fields: new FieldDefinition[]
    {
        Title("Tmpl_homeinventory_f_Item"),
        SingleChoice("Tmpl_homeinventory_f_Category",
            "Tmpl_homeinventory_c_Electronics", "Tmpl_homeinventory_c_Furniture",
            "Tmpl_homeinventory_c_Appliance", "Tmpl_homeinventory_c_Jewelry", "Tmpl_homeinventory_c_Other"),
        Text("Tmpl_homeinventory_f_Room", showInList: true),
        Text("Tmpl_homeinventory_f_Brand"),
        Date("Tmpl_homeinventory_f_PurchaseDate"),
        Currency("Tmpl_homeinventory_f_Price"),
        Date("Tmpl_homeinventory_f_WarrantyUntil"),
        Image("Tmpl_homeinventory_f_Photo"),
        RichText("Tmpl_homeinventory_f_Notes"),
    });
}

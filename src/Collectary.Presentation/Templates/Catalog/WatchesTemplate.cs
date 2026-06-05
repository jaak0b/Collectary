using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.Templates.Catalog;

public sealed class WatchesTemplate : PresetTemplateBase, IPresetTemplate
{
    public string Key => "watches";
    public PresetTemplateCategory Category => PresetTemplateCategory.Lifestyle;
    public string Icon => IconGlyphs.Smartwatch;
    public string NameKey => "Tmpl_watches_Name";
    public string DescriptionKey => "Tmpl_watches_Desc";

    public Preset Build() => Compose(NameKey, columns: 2, fields: new FieldDefinition[]
    {
        Title("Tmpl_watches_f_Model"),
        Text("Tmpl_watches_f_Brand", showInList: true),
        SingleChoice("Tmpl_watches_f_Movement",
            "Tmpl_watches_c_Automatic", "Tmpl_watches_c_Manual", "Tmpl_watches_c_Quartz"),
        Date("Tmpl_watches_f_PurchaseDate"),
        Currency("Tmpl_watches_f_Price"),
        SingleChoice("Tmpl_watches_f_Condition",
            "Tmpl_watches_c_New", "Tmpl_watches_c_Excellent", "Tmpl_watches_c_Good", "Tmpl_watches_c_Worn"),
        Image("Tmpl_watches_f_Photo"),
    });
}

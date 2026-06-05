using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.Templates.Catalog;

public sealed class MakeupTemplate : PresetTemplateBase, IPresetTemplate
{
    public string Key => "makeup";
    public PresetTemplateCategory Category => PresetTemplateCategory.Lifestyle;
    public string Icon => IconGlyphs.PaintBrush;
    public string NameKey => "Tmpl_makeup_Name";
    public string DescriptionKey => "Tmpl_makeup_Desc";

    public Preset Build() => Compose(NameKey, columns: 2, fields: new FieldDefinition[]
    {
        Title("Tmpl_makeup_f_Name"),
        Text("Tmpl_makeup_f_Brand", showInList: true),
        SingleChoice("Tmpl_makeup_f_Category",
            "Tmpl_makeup_c_Foundation", "Tmpl_makeup_c_Lipstick", "Tmpl_makeup_c_Mascara",
            "Tmpl_makeup_c_Eyeshadow", "Tmpl_makeup_c_Blush", "Tmpl_makeup_c_Other"),
        Text("Tmpl_makeup_f_Shade"),
        Color("Tmpl_makeup_f_Color"),
        Date("Tmpl_makeup_f_PurchaseDate"),
        Date("Tmpl_makeup_f_Expiry"),
        Currency("Tmpl_makeup_f_Price"),
        Rating("Tmpl_makeup_f_Rating"),
        Bool("Tmpl_makeup_f_Finished"),
        Image("Tmpl_makeup_f_Photo"),
    });
}

using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.Templates.Catalog;

public sealed class ModelTrainsTemplate : PresetTemplateBase, IPresetTemplate
{
    public string Key => "modeltrains";
    public PresetTemplateCategory Category => PresetTemplateCategory.Collectibles;
    public string Icon => IconGlyphs.VehicleSubway;
    public string NameKey => "Tmpl_modeltrains_Name";
    public string DescriptionKey => "Tmpl_modeltrains_Desc";

    public Preset Build() => Compose(NameKey, columns: 2, fields: new FieldDefinition[]
    {
        Title("Tmpl_modeltrains_f_Name"),
        SingleChoice("Tmpl_modeltrains_f_Scale",
            "Tmpl_modeltrains_c_Z", "Tmpl_modeltrains_c_N", "Tmpl_modeltrains_c_TT",
            "Tmpl_modeltrains_c_HO", "Tmpl_modeltrains_c_OO", "Tmpl_modeltrains_c_O", "Tmpl_modeltrains_c_G"),
        Text("Tmpl_modeltrains_f_RoadName", showInList: true),
        Text("Tmpl_modeltrains_f_RoadNumber"),
        SingleChoice("Tmpl_modeltrains_f_Manufacturer",
            "Tmpl_modeltrains_c_Marklin", "Tmpl_modeltrains_c_Roco", "Tmpl_modeltrains_c_Fleischmann",
            "Tmpl_modeltrains_c_Trix", "Tmpl_modeltrains_c_Other"),
        Integer("Tmpl_modeltrains_f_DccAddress"),
        Bool("Tmpl_modeltrains_f_DccEquipped"),
        SingleChoice("Tmpl_modeltrains_f_Era",
            "Tmpl_modeltrains_c_I", "Tmpl_modeltrains_c_II", "Tmpl_modeltrains_c_III",
            "Tmpl_modeltrains_c_IV", "Tmpl_modeltrains_c_V", "Tmpl_modeltrains_c_VI"),
        Date("Tmpl_modeltrains_f_PurchaseDate"),
        Currency("Tmpl_modeltrains_f_Price"),
        Image("Tmpl_modeltrains_f_Photo"),
        RichText("Tmpl_modeltrains_f_Notes"),
    });
}

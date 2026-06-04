using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.Templates.Catalog;

public sealed class BuildingBrickSetsTemplate : PresetTemplateBase, IPresetTemplate
{
    public string Key => "bricks";
    public PresetTemplateCategory Category => PresetTemplateCategory.Collectibles;
    public string Icon => "🧱";
    public string NameKey => "Tmpl_bricks_Name";
    public string DescriptionKey => "Tmpl_bricks_Desc";

    public Preset Build() => Compose(NameKey, columns: 2, fields: new FieldDefinition[]
    {
        Title("Tmpl_bricks_f_SetName"),
        Text("Tmpl_bricks_f_SetNumber", showInList: true),
        SingleChoice("Tmpl_bricks_f_Theme",
            "Tmpl_bricks_c_Vehicles", "Tmpl_bricks_c_Buildings", "Tmpl_bricks_c_Space",
            "Tmpl_bricks_c_Mechanical", "Tmpl_bricks_c_Figures", "Tmpl_bricks_c_Other"),
        Integer("Tmpl_bricks_f_Pieces"),
        Integer("Tmpl_bricks_f_Year"),
        Currency("Tmpl_bricks_f_Price"),
        Bool("Tmpl_bricks_f_Built"),
        Image("Tmpl_bricks_f_Box"),
    });
}

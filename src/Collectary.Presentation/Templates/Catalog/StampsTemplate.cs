using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.Templates.Catalog;

public sealed class StampsTemplate : PresetTemplateBase, IPresetTemplate
{
    public string Key => "stamps";
    public PresetTemplateCategory Category => PresetTemplateCategory.Collectibles;
    public string Icon => "📮";
    public string NameKey => "Tmpl_stamps_Name";
    public string DescriptionKey => "Tmpl_stamps_Desc";

    public Preset Build() => Compose(NameKey, columns: 2, fields: new FieldDefinition[]
    {
        Title("Tmpl_stamps_f_Name"),
        Text("Tmpl_stamps_f_Country", showInList: true),
        Integer("Tmpl_stamps_f_Year"),
        Text("Tmpl_stamps_f_Denomination"),
        SingleChoice("Tmpl_stamps_f_Condition",
            "Tmpl_stamps_c_Mint", "Tmpl_stamps_c_Used", "Tmpl_stamps_c_Hinged", "Tmpl_stamps_c_Damaged"),
        Currency("Tmpl_stamps_f_Value"),
        Image("Tmpl_stamps_f_Image"),
    });
}

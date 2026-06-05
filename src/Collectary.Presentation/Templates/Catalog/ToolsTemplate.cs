using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.Templates.Catalog;

public sealed class ToolsTemplate : PresetTemplateBase, IPresetTemplate
{
    public string Key => "tools";
    public PresetTemplateCategory Category => PresetTemplateCategory.Practical;
    public string Icon => IconGlyphs.Wrench;
    public string NameKey => "Tmpl_tools_Name";
    public string DescriptionKey => "Tmpl_tools_Desc";

    public Preset Build() => Compose(NameKey, columns: 2, fields: new FieldDefinition[]
    {
        Title("Tmpl_tools_f_Name"),
        SingleChoice("Tmpl_tools_f_Category",
            "Tmpl_tools_c_Hand", "Tmpl_tools_c_Power", "Tmpl_tools_c_Measuring", "Tmpl_tools_c_Other"),
        Text("Tmpl_tools_f_Brand", showInList: true),
        Text("Tmpl_tools_f_Location"),
        Date("Tmpl_tools_f_PurchaseDate"),
        Currency("Tmpl_tools_f_Price"),
        Image("Tmpl_tools_f_Photo"),
    });
}

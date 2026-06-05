using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.Templates.Catalog;

public sealed class ComicsTemplate : PresetTemplateBase, IPresetTemplate
{
    public string Key => "comics";
    public PresetTemplateCategory Category => PresetTemplateCategory.MediaEntertainment;
    public string Icon => IconGlyphs.BookOpen;
    public string NameKey => "Tmpl_comics_Name";
    public string DescriptionKey => "Tmpl_comics_Desc";

    public Preset Build() => Compose(NameKey, columns: 2, fields: new FieldDefinition[]
    {
        Title("Tmpl_comics_f_Title"),
        Text("Tmpl_comics_f_Series", showInList: true),
        Integer("Tmpl_comics_f_IssueNumber"),
        Text("Tmpl_comics_f_Writer"),
        Text("Tmpl_comics_f_Artist"),
        Text("Tmpl_comics_f_Publisher"),
        SingleChoice("Tmpl_comics_f_Condition",
            "Tmpl_comics_c_Mint", "Tmpl_comics_c_NearMint", "Tmpl_comics_c_VeryGood",
            "Tmpl_comics_c_Good", "Tmpl_comics_c_Fair"),
        Currency("Tmpl_comics_f_Value"),
        Image("Tmpl_comics_f_Cover"),
    });
}

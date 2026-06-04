using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.Templates.Catalog;

public sealed class ActionFiguresTemplate : PresetTemplateBase, IPresetTemplate
{
    public string Key => "actionfigures";
    public PresetTemplateCategory Category => PresetTemplateCategory.Collectibles;
    public string Icon => "🎯";
    public string NameKey => "Tmpl_actionfigures_Name";
    public string DescriptionKey => "Tmpl_actionfigures_Desc";

    public Preset Build() => Compose(NameKey, columns: 2, fields: new FieldDefinition[]
    {
        Title("Tmpl_actionfigures_f_Name"),
        Text("Tmpl_actionfigures_f_Series", showInList: true),
        Text("Tmpl_actionfigures_f_Number"),
        Text("Tmpl_actionfigures_f_Variant"),
        Bool("Tmpl_actionfigures_f_Boxed"),
        Currency("Tmpl_actionfigures_f_Value"),
        Image("Tmpl_actionfigures_f_Photo"),
    });
}

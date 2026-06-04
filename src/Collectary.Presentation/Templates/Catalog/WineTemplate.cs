using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.Templates.Catalog;

public sealed class WineTemplate : PresetTemplateBase, IPresetTemplate
{
    public string Key => "wine";
    public PresetTemplateCategory Category => PresetTemplateCategory.Lifestyle;
    public string Icon => "🍷";
    public string NameKey => "Tmpl_wine_Name";
    public string DescriptionKey => "Tmpl_wine_Desc";

    public Preset Build() => Compose(NameKey, columns: 2, fields: new FieldDefinition[]
    {
        Title("Tmpl_wine_f_Name"),
        Integer("Tmpl_wine_f_Vintage"),
        Text("Tmpl_wine_f_Region", showInList: true),
        Text("Tmpl_wine_f_Varietal"),
        Rating("Tmpl_wine_f_Rating"),
        Integer("Tmpl_wine_f_Bottles"),
        Currency("Tmpl_wine_f_Price"),
        RichText("Tmpl_wine_f_TastingNotes"),
    });
}

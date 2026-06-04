using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.Templates.Catalog;

public sealed class CoinsTemplate : PresetTemplateBase, IPresetTemplate
{
    public string Key => "coins";
    public PresetTemplateCategory Category => PresetTemplateCategory.Collectibles;
    public string Icon => "🪙";
    public string NameKey => "Tmpl_coins_Name";
    public string DescriptionKey => "Tmpl_coins_Desc";

    public Preset Build() => Compose(NameKey, columns: 2, fields: new FieldDefinition[]
    {
        Title("Tmpl_coins_f_Name"),
        Text("Tmpl_coins_f_Country", showInList: true),
        Integer("Tmpl_coins_f_Year"),
        Text("Tmpl_coins_f_Denomination"),
        SingleChoice("Tmpl_coins_f_Grade",
            "Tmpl_coins_c_Poor", "Tmpl_coins_c_Fine", "Tmpl_coins_c_VeryFine",
            "Tmpl_coins_c_ExtremelyFine", "Tmpl_coins_c_Uncirculated"),
        Text("Tmpl_coins_f_MintMark"),
        Currency("Tmpl_coins_f_Value"),
        Image("Tmpl_coins_f_Image"),
    });
}

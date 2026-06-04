using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.Templates.Catalog;

public sealed class TradingCardsTemplate : PresetTemplateBase, IPresetTemplate
{
    public string Key => "tradingcards";
    public PresetTemplateCategory Category => PresetTemplateCategory.Collectibles;
    public string Icon => "🃏";
    public string NameKey => "Tmpl_tradingcards_Name";
    public string DescriptionKey => "Tmpl_tradingcards_Desc";

    public Preset Build() => Compose(NameKey, columns: 2, fields: new FieldDefinition[]
    {
        Title("Tmpl_tradingcards_f_Name"),
        Text("Tmpl_tradingcards_f_Set", showInList: true),
        Text("Tmpl_tradingcards_f_CardNumber"),
        SingleChoice("Tmpl_tradingcards_f_Rarity",
            "Tmpl_tradingcards_c_Common", "Tmpl_tradingcards_c_Uncommon",
            "Tmpl_tradingcards_c_Rare", "Tmpl_tradingcards_c_Mythic"),
        SingleChoice("Tmpl_tradingcards_f_Condition",
            "Tmpl_tradingcards_c_Mint", "Tmpl_tradingcards_c_NearMint",
            "Tmpl_tradingcards_c_Played", "Tmpl_tradingcards_c_Damaged"),
        Integer("Tmpl_tradingcards_f_Quantity"),
        Currency("Tmpl_tradingcards_f_Value"),
        Image("Tmpl_tradingcards_f_Image"),
    });
}

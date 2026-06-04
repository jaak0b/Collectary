using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.Templates.Catalog;

public sealed class BoardGamesTemplate : PresetTemplateBase, IPresetTemplate
{
    public string Key => "boardgames";
    public PresetTemplateCategory Category => PresetTemplateCategory.MediaEntertainment;
    public string Icon => "🎲";
    public string NameKey => "Tmpl_boardgames_Name";
    public string DescriptionKey => "Tmpl_boardgames_Desc";

    public Preset Build() => Compose(NameKey, columns: 2, fields: new FieldDefinition[]
    {
        Title("Tmpl_boardgames_f_Name"),
        Text("Tmpl_boardgames_f_Designer", showInList: true),
        Integer("Tmpl_boardgames_f_MinPlayers"),
        Integer("Tmpl_boardgames_f_MaxPlayers"),
        Duration("Tmpl_boardgames_f_PlayTime"),
        Rating("Tmpl_boardgames_f_Complexity"),
        Bool("Tmpl_boardgames_f_Owned"),
        Image("Tmpl_boardgames_f_Cover"),
    });
}

using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.Templates.Catalog;

public sealed class VideoGamesTemplate : PresetTemplateBase, IPresetTemplate
{
    public string Key => "videogames";
    public PresetTemplateCategory Category => PresetTemplateCategory.MediaEntertainment;
    public string Icon => "🎮";
    public string NameKey => "Tmpl_videogames_Name";
    public string DescriptionKey => "Tmpl_videogames_Desc";

    public Preset Build() => Compose(NameKey, columns: 2, fields: new FieldDefinition[]
    {
        Title("Tmpl_videogames_f_Title"),
        SingleChoice("Tmpl_videogames_f_Platform",
            "Tmpl_videogames_c_PC", "Tmpl_videogames_c_PlayStation", "Tmpl_videogames_c_Xbox",
            "Tmpl_videogames_c_Switch", "Tmpl_videogames_c_Mobile"),
        MultiChoice("Tmpl_videogames_f_Genre",
            "Tmpl_videogames_c_Action", "Tmpl_videogames_c_RPG", "Tmpl_videogames_c_Strategy",
            "Tmpl_videogames_c_Shooter", "Tmpl_videogames_c_Puzzle", "Tmpl_videogames_c_Sports"),
        Bool("Tmpl_videogames_f_Completed"),
        Rating("Tmpl_videogames_f_Rating"),
        Integer("Tmpl_videogames_f_HoursPlayed"),
        Date("Tmpl_videogames_f_ReleaseDate"),
        Image("Tmpl_videogames_f_Cover"),
    });
}

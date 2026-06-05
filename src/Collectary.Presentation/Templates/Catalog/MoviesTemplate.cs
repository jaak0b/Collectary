using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.Templates.Catalog;

public sealed class MoviesTemplate : PresetTemplateBase, IPresetTemplate
{
    public string Key => "movies";
    public PresetTemplateCategory Category => PresetTemplateCategory.MediaEntertainment;
    public string Icon => IconGlyphs.MoviesAndTv;
    public string NameKey => "Tmpl_movies_Name";
    public string DescriptionKey => "Tmpl_movies_Desc";

    public Preset Build() => Compose(NameKey, columns: 2, fields: new FieldDefinition[]
    {
        Title("Tmpl_movies_f_Title"),
        Text("Tmpl_movies_f_Director", showInList: true),
        Integer("Tmpl_movies_f_Year"),
        MultiChoice("Tmpl_movies_f_Genre",
            "Tmpl_movies_c_Action", "Tmpl_movies_c_Comedy", "Tmpl_movies_c_Drama",
            "Tmpl_movies_c_SciFi", "Tmpl_movies_c_Horror", "Tmpl_movies_c_Documentary"),
        Duration("Tmpl_movies_f_Runtime"),
        SingleChoice("Tmpl_movies_f_Format",
            "Tmpl_movies_c_DVD", "Tmpl_movies_c_BluRay", "Tmpl_movies_c_FourK", "Tmpl_movies_c_Digital"),
        Rating("Tmpl_movies_f_Rating"),
        Bool("Tmpl_movies_f_Watched"),
        Image("Tmpl_movies_f_Poster"),
        RichText("Tmpl_movies_f_Notes"),
    });
}

using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.Templates.Catalog;

public sealed class MusicTemplate : PresetTemplateBase, IPresetTemplate
{
    public string Key => "music";
    public PresetTemplateCategory Category => PresetTemplateCategory.MediaEntertainment;
    public string Icon => IconGlyphs.MusicNote;
    public string NameKey => "Tmpl_music_Name";
    public string DescriptionKey => "Tmpl_music_Desc";

    public Preset Build() => Compose(NameKey, columns: 2, fields: new FieldDefinition[]
    {
        Title("Tmpl_music_f_Album"),
        Text("Tmpl_music_f_Artist", showInList: true),
        Integer("Tmpl_music_f_Year"),
        MultiChoice("Tmpl_music_f_Genre",
            "Tmpl_music_c_Rock", "Tmpl_music_c_Pop", "Tmpl_music_c_Jazz",
            "Tmpl_music_c_Classical", "Tmpl_music_c_Electronic", "Tmpl_music_c_HipHop"),
        SingleChoice("Tmpl_music_f_Format",
            "Tmpl_music_c_Vinyl", "Tmpl_music_c_CD", "Tmpl_music_c_Digital", "Tmpl_music_c_Cassette"),
        Rating("Tmpl_music_f_Rating"),
        Image("Tmpl_music_f_Cover"),
    });
}

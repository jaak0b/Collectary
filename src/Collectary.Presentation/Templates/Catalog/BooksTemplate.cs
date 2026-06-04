using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.Templates.Catalog;

public sealed class BooksTemplate : PresetTemplateBase, IPresetTemplate
{
    public string Key => "books";
    public PresetTemplateCategory Category => PresetTemplateCategory.MediaEntertainment;
    public string Icon => "📚";
    public string NameKey => "Tmpl_books_Name";
    public string DescriptionKey => "Tmpl_books_Desc";

    public Preset Build() => Compose(NameKey, columns: 2, fields: new FieldDefinition[]
    {
        Title("Tmpl_books_f_Title"),
        Text("Tmpl_books_f_Author", showInList: true),
        Text("Tmpl_books_f_Series"),
        SingleChoice("Tmpl_books_f_Format",
            "Tmpl_books_c_Hardcover", "Tmpl_books_c_Paperback", "Tmpl_books_c_eBook", "Tmpl_books_c_Audiobook"),
        Integer("Tmpl_books_f_Pages"),
        Rating("Tmpl_books_f_Rating"),
        Bool("Tmpl_books_f_Read"),
        Date("Tmpl_books_f_DateRead"),
        Image("Tmpl_books_f_Cover"),
        Text("Tmpl_books_f_ISBN"),
        RichText("Tmpl_books_f_Notes"),
    });
}

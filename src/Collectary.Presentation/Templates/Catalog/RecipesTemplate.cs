using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.Templates.Catalog;

public sealed class RecipesTemplate : PresetTemplateBase, IPresetTemplate
{
    public string Key => "recipes";
    public PresetTemplateCategory Category => PresetTemplateCategory.Lifestyle;
    public string Icon => "🍳";
    public string NameKey => "Tmpl_recipes_Name";
    public string DescriptionKey => "Tmpl_recipes_Desc";

    public Preset Build() => Compose(NameKey, columns: 2, fields: new FieldDefinition[]
    {
        Title("Tmpl_recipes_f_Name"),
        SingleChoice("Tmpl_recipes_f_Cuisine",
            "Tmpl_recipes_c_Italian", "Tmpl_recipes_c_Asian", "Tmpl_recipes_c_Mexican",
            "Tmpl_recipes_c_American", "Tmpl_recipes_c_French", "Tmpl_recipes_c_Other"),
        Duration("Tmpl_recipes_f_PrepTime"),
        Duration("Tmpl_recipes_f_CookTime"),
        Integer("Tmpl_recipes_f_Servings"),
        List("Tmpl_recipes_f_Ingredients", ListInlineStyle.Grid,
            Text("Tmpl_recipes_f_IngredientName", showInList: true),
            Text("Tmpl_recipes_f_IngredientAmount", showInList: true)),
        RichText("Tmpl_recipes_f_Instructions"),
        Rating("Tmpl_recipes_f_Rating"),
        Image("Tmpl_recipes_f_Photo"),
    });
}

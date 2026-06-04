using Autofac;
using Collectary.Core.Domain;
using Collectary.Presentation.DI;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.DI;

public class FieldEditorRegistry(IComponentContext ctx) : IFieldEditorRegistry
{
    public FieldEditorViewModelBase? Create(FieldDefinition definition, FieldValue? existing, ItemEditingContext context)
    {
        var key = definition.GetType().Name;
        if (!ctx.IsRegisteredWithName<FieldEditorViewModelBase>(key)) return null;
        var value = definition.GetOrCreateEmptyValue(existing);
        return ctx.ResolveNamed<FieldEditorViewModelBase>(key,
            new NamedParameter("definition", definition),
            new NamedParameter("value", value),
            new NamedParameter("context", context));
    }
}

using Autofac;
using Collectary.Core.Domain.Fields;

namespace Collectary.UI.ViewModels;

public class ColorFormatEditorFactory(IComponentContext ctx)
{
    public ColorFormatEditorViewModel Create(ColorFormat format, string? raw) =>
        ctx.ResolveNamed<ColorFormatEditorViewModel>(
            format.ToString(),
            new NamedParameter("raw", raw));
}

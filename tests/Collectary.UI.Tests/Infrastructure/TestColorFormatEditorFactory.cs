using Autofac;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.Infrastructure;

internal static class TestColorFormatEditorFactoryFactory
{
    private static readonly ColorFormatEditorFactory _instance = Build();

    private static ColorFormatEditorFactory Build()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<HexColorFormatEditorViewModel>()
            .Named<ColorFormatEditorViewModel>(ColorFormat.Hex.ToString());
        builder.RegisterType<RgbColorFormatEditorViewModel>()
            .Named<ColorFormatEditorViewModel>(ColorFormat.Rgb.ToString());
        builder.RegisterType<ArgbColorFormatEditorViewModel>()
            .Named<ColorFormatEditorViewModel>(ColorFormat.Argb.ToString());
        builder.RegisterType<CmykColorFormatEditorViewModel>()
            .Named<ColorFormatEditorViewModel>(ColorFormat.Cmyk.ToString());
        var container = builder.Build();
        return new ColorFormatEditorFactory(container);
    }

    public static ColorFormatEditorFactory Instance => _instance;
}

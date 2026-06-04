using Autofac;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class ColorFieldEditorViewModelTest
{
    internal static ColorFormatEditorFactory BuildFactory()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<HexColorFormatEditorViewModel>().Named<ColorFormatEditorViewModel>("Hex");
        builder.RegisterType<RgbColorFormatEditorViewModel>().Named<ColorFormatEditorViewModel>("Rgb");
        builder.RegisterType<ArgbColorFormatEditorViewModel>().Named<ColorFormatEditorViewModel>("Argb");
        builder.RegisterType<CmykColorFormatEditorViewModel>().Named<ColorFormatEditorViewModel>("Cmyk");
        builder.Register(c => new ColorFormatEditorFactory(c.Resolve<IComponentContext>()));
        return builder.Build().Resolve<ColorFormatEditorFactory>();
    }

    [Test]
    public void CreatesSubEditorForFormat_AndPersistsEncoded()
    {
        var def = new ColorFieldDefinition { Format = ColorFormat.Hex };
        var sut = new ColorFieldEditorViewModel(def, new ColorFieldValue { Value = "#FF0000" }, BuildFactory());

        Assert.That(sut.SubEditor, Is.TypeOf<HexColorFormatEditorViewModel>());
        Assert.That(((ColorFieldValue)sut.GetCurrentValue()).Value, Is.EqualTo("#FF0000"));
    }
}

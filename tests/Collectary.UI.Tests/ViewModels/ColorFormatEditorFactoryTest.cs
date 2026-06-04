using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class ColorFormatEditorFactoryTest
{
    [Test]
    public void Create_ProducesEachFormat()
    {
        var factory = ColorFieldEditorViewModelTest.BuildFactory();
        Assert.That(factory.Create(ColorFormat.Hex, "#000000"), Is.TypeOf<HexColorFormatEditorViewModel>());
        Assert.That(factory.Create(ColorFormat.Rgb, "1,2,3"), Is.TypeOf<RgbColorFormatEditorViewModel>());
        Assert.That(factory.Create(ColorFormat.Argb, "1,2,3,4"), Is.TypeOf<ArgbColorFormatEditorViewModel>());
        Assert.That(factory.Create(ColorFormat.Cmyk, "1,2,3,4"), Is.TypeOf<CmykColorFormatEditorViewModel>());
    }
}

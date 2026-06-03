using Collectary.UI.ViewModels;

namespace Collectary.UI.Tests;

[TestFixture]
public class RgbColorFormatEditorViewModelTest
{
    [Test]
    public void Constructor_ParsesEachChannel()
    {
        var vm = new RgbColorFormatEditorViewModel("11,22,33");

        Assert.That(vm.R, Is.EqualTo(11));
        Assert.That(vm.G, Is.EqualTo(22));
        Assert.That(vm.B, Is.EqualTo(33));
    }

    [Test]
    public void ChannelSettersClampAndEncode()
    {
        var vm = new RgbColorFormatEditorViewModel("0,0,0") { R = 300, G = 128, B = -10 };

        Assert.That(vm.R, Is.EqualTo(255));
        Assert.That(vm.G, Is.EqualTo(128));
        Assert.That(vm.B, Is.EqualTo(0));
        Assert.That(vm.Encode(), Is.EqualTo("255,128,0"));
    }

    [Test]
    public void ChannelSetters_UpdateOnlyTargetChannel()
    {
        var vm = new RgbColorFormatEditorViewModel("1,2,3") { R = 10, G = 20, B = 30 };

        Assert.That(vm.R, Is.EqualTo(10));
        Assert.That(vm.G, Is.EqualTo(20));
        Assert.That(vm.B, Is.EqualTo(30));
    }

    [Test]
    public void ChannelSetter_NullClampsToZero()
    {
        var vm = new RgbColorFormatEditorViewModel("50,50,50") { G = null };
        Assert.That(vm.G, Is.EqualTo(0));
    }

    [Test]
    public void SettingChannel_RaisesChannelAndSwatchNotifications()
    {
        var vm = new RgbColorFormatEditorViewModel("0,0,0");
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.G = 100;

        Assert.That(raised, Does.Contain(nameof(vm.R)));
        Assert.That(raised, Does.Contain(nameof(vm.G)));
        Assert.That(raised, Does.Contain(nameof(vm.B)));
        Assert.That(raised, Does.Contain(nameof(vm.PickerColor)));
        Assert.That(raised, Does.Contain(nameof(vm.SwatchBrush)));
    }

    [Test]
    public void Encode_UsesRgbOrder()
    {
        var vm = new RgbColorFormatEditorViewModel("64,128,192");
        Assert.That(vm.Encode(), Is.EqualTo("64,128,192"));
    }

    [Test]
    public void SupportsPicker_IsTrue()
    {
        Assert.That(new RgbColorFormatEditorViewModel("0,0,0").SupportsPicker, Is.True);
    }
}

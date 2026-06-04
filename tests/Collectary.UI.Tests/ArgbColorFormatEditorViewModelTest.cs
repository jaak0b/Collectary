using System.ComponentModel;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests;

[TestFixture]
public class ArgbColorFormatEditorViewModelTest
{
    [Test]
    public void Constructor_ParsesEachChannel()
    {
        var vm = new ArgbColorFormatEditorViewModel("12,34,56,78");

        Assert.That(vm.A, Is.EqualTo(12));
        Assert.That(vm.R, Is.EqualTo(34));
        Assert.That(vm.G, Is.EqualTo(56));
        Assert.That(vm.B, Is.EqualTo(78));
    }

    [Test]
    public void Constructor_InvalidRaw_DefaultsToWhite()
    {
        var vm = new ArgbColorFormatEditorViewModel(null);

        Assert.That(vm.A, Is.EqualTo(255));
        Assert.That(vm.R, Is.EqualTo(255));
        Assert.That(vm.G, Is.EqualTo(255));
        Assert.That(vm.B, Is.EqualTo(255));
    }

    [Test]
    public void ChannelSetters_UpdateOnlyTargetChannel()
    {
        var vm = new ArgbColorFormatEditorViewModel("255,0,0,0") { A = 128, R = 10, G = 20, B = 30 };

        Assert.That(vm.A, Is.EqualTo(128));
        Assert.That(vm.R, Is.EqualTo(10));
        Assert.That(vm.G, Is.EqualTo(20));
        Assert.That(vm.B, Is.EqualTo(30));
        Assert.That(vm.Encode(), Is.EqualTo("128,10,20,30"));
    }

    [Test]
    public void ChannelSetters_ClampAboveAndBelowRange()
    {
        var vm = new ArgbColorFormatEditorViewModel("255,255,255,255") { A = 300, R = -10, G = 256, B = -1 };

        Assert.That(vm.A, Is.EqualTo(255));
        Assert.That(vm.R, Is.EqualTo(0));
        Assert.That(vm.G, Is.EqualTo(255));
        Assert.That(vm.B, Is.EqualTo(0));
    }

    [Test]
    public void ChannelSetter_NullClampsToZero()
    {
        var vm = new ArgbColorFormatEditorViewModel("255,40,40,40") { R = null };
        Assert.That(vm.R, Is.EqualTo(0));
    }

    [Test]
    public void SettingChannel_RaisesAllChannelAndSwatchNotifications()
    {
        var vm = new ArgbColorFormatEditorViewModel("255,0,0,0");
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.R = 200;

        Assert.That(raised, Does.Contain(nameof(vm.A)));
        Assert.That(raised, Does.Contain(nameof(vm.R)));
        Assert.That(raised, Does.Contain(nameof(vm.G)));
        Assert.That(raised, Does.Contain(nameof(vm.B)));
        Assert.That(raised, Does.Contain(nameof(vm.PickerColor)));
        Assert.That(raised, Does.Contain(nameof(vm.SwatchBrush)));
    }

    [Test]
    public void Encode_UsesArgbOrder()
    {
        var vm = new ArgbColorFormatEditorViewModel("10,20,30,40");
        Assert.That(vm.Encode(), Is.EqualTo("10,20,30,40"));
    }
}

using Avalonia.Media;
using Collectary.Presentation.Localization;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class CustomColorSlotTest
{
    [TearDown]
    public void TearDown() => LocalizationService.Instance.Apply("en");

    [Test]
    public void Color_Change_FlagsOverriddenAndNotifiesOwner()
    {
        CustomColorSlot? notified = null;
        var slot = new CustomColorSlot("Background", "Color_Background", true, Colors.White, false, s => notified = s);

        slot.Color = Colors.Magenta;

        Assert.Multiple(() =>
        {
            Assert.That(slot.IsOverridden, Is.True);
            Assert.That(notified, Is.SameAs(slot));
        });
    }

    [Test]
    public void Ctor_SeedingColor_DoesNotNotifyOwner()
    {
        var notified = false;
        _ = new CustomColorSlot("Background", "Color_Background", true, Colors.Magenta, false, _ => notified = true);

        Assert.That(notified, Is.False);
    }

    [Test]
    public void Revert_SetsColorWithoutFlaggingOrNotifying()
    {
        var notified = false;
        var slot = new CustomColorSlot("Background", "Color_Background", true, Colors.White, true, _ => notified = true);

        slot.Revert(Colors.Black);

        Assert.Multiple(() =>
        {
            Assert.That(slot.Color, Is.EqualTo(Colors.Black));
            Assert.That(slot.IsOverridden, Is.False);
            Assert.That(notified, Is.False);
        });
    }

    [Test]
    public void Label_LocalizesFromLabelKey()
    {
        var slot = new CustomColorSlot("Background", "Color_Background", true, Colors.White, false, _ => { });

        LocalizationService.Instance.Apply("de");

        Assert.That(slot.Label, Is.EqualTo("Fensterhintergrund"));
    }
}

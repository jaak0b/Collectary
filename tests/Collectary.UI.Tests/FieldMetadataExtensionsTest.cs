using Collectary.Core.Domain.Fields;
using Collectary.UI.Localization;
using Collectary.UI.ViewModels;

namespace Collectary.UI.Tests;

[TestFixture]
public class FieldMetadataExtensionsTest
{
    [Test]
    public void ToLocalizedString_ReadsLocalizedNameAttribute()
    {
        LocalizationService.Instance.Apply("en");
        Assert.That(typeof(TextFieldDefinition).ToLocalizedString(), Is.EqualTo("Text"));
    }

    [Test]
    public void GetFieldIcon_ReadsFieldIconAttribute() =>
        Assert.That(typeof(CurrencyFieldDefinition).GetFieldIcon(), Is.EqualTo("💰"));

    [Test]
    public void ToLocalizedString_Throws_WhenAttributeMissing() =>
        Assert.Throws<InvalidOperationException>(() => typeof(string).ToLocalizedString());

    [Test]
    public void GetFieldIcon_Throws_WhenAttributeMissing() =>
        Assert.Throws<InvalidOperationException>(() => typeof(string).GetFieldIcon());
}

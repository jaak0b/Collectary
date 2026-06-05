using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Presentation.Localization;
using Collectary.UI.Converters;

namespace Collectary.UI.Tests;

[TestFixture]
public class CloudProviderDisplayConverterTest
{
    private readonly CloudProviderDisplayConverter _sut = new();

    [Test]
    public void Convert_EachProvider_ReturnsLocalizedName()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Convert(CloudProvider.OneDrive),
                Is.EqualTo(LocalizationService.Instance["Settings_Provider_OneDrive"]));
            Assert.That(Convert(CloudProvider.GoogleDrive),
                Is.EqualTo(LocalizationService.Instance["Settings_Provider_GoogleDrive"]));
            Assert.That(Convert(CloudProvider.Folder),
                Is.EqualTo(LocalizationService.Instance["Settings_Provider_Folder"]));
        });
    }

    [Test]
    public void Convert_NonProviderValue_FallsBackToToString() =>
        Assert.That(_sut.Convert(42, typeof(string), null, CultureInfo.InvariantCulture), Is.EqualTo("42"));

    [Test]
    public void Convert_Null_ReturnsNull() =>
        Assert.That(_sut.Convert(null, typeof(string), null, CultureInfo.InvariantCulture), Is.Null);

    [Test]
    public void ConvertBack_Throws() =>
        Assert.Throws<NotSupportedException>(() =>
            _sut.ConvertBack(null, typeof(CloudProvider), null, CultureInfo.InvariantCulture));

    private object? Convert(CloudProvider provider) =>
        _sut.Convert(provider, typeof(string), null, CultureInfo.InvariantCulture);
}

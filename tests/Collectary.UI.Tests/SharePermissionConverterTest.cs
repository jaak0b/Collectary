using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Presentation.Localization;
using Collectary.UI.Converters;

namespace Collectary.UI.Tests;

[TestFixture]
public class SharePermissionConverterTest
{
    private readonly SharePermissionConverter _sut = new();

    [Test]
    public void Convert_EachPermission_ReturnsLocalizedLabel()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Convert(SharePermission.Edit),
                Is.EqualTo(LocalizationService.Instance["Permission_Edit"]));
            Assert.That(Convert(SharePermission.Read),
                Is.EqualTo(LocalizationService.Instance["Permission_Read"]));
        });
    }

    [Test]
    public void Convert_NonPermissionValue_FallsBackToToString() =>
        Assert.That(_sut.Convert(7, typeof(string), null, CultureInfo.InvariantCulture), Is.EqualTo("7"));

    [Test]
    public void Convert_Null_ReturnsNull() =>
        Assert.That(_sut.Convert(null, typeof(string), null, CultureInfo.InvariantCulture), Is.Null);

    [Test]
    public void ConvertBack_Throws() =>
        Assert.Throws<NotSupportedException>(() =>
            _sut.ConvertBack(null, typeof(SharePermission), null, CultureInfo.InvariantCulture));

    private object? Convert(SharePermission permission) =>
        _sut.Convert(permission, typeof(string), null, CultureInfo.InvariantCulture);
}

using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class DateRangeFieldEditorViewModelTest
{
    [Test]
    public void LoadsBothEnds()
    {
        var sut = new DateRangeFieldEditorViewModel(new DateRangeFieldDefinition(),
            new DateRangeFieldValue { From = new DateTime(2019, 1, 1), To = new DateTime(2019, 12, 31) });
        Assert.That(sut.From, Is.EqualTo(new DateTime(2019, 1, 1)));
        Assert.That(sut.To, Is.EqualTo(new DateTime(2019, 12, 31)));
    }

    [Test]
    public void NullEnds_StayNull()
    {
        var sut = new DateRangeFieldEditorViewModel(new DateRangeFieldDefinition(), new DateRangeFieldValue());
        Assert.That(sut.From, Is.Null);
        Assert.That(sut.To, Is.Null);
    }

    [Test]
    public void GetCurrentValue_PersistsBothEndsAsUtc()
    {
        var sut = new DateRangeFieldEditorViewModel(new DateRangeFieldDefinition(), new DateRangeFieldValue())
        {
            From = new DateTime(2018, 5, 1),
            To = new DateTime(2020, 6, 30)
        };

        var v = (DateRangeFieldValue)sut.GetCurrentValue();
        Assert.That(v.From, Is.EqualTo(new DateTime(2018, 5, 1)));
        Assert.That(v.From!.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
        Assert.That(v.To, Is.EqualTo(new DateTime(2020, 6, 30)));
        Assert.That(v.To!.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
    }
}

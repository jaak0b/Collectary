using Collectary.Presentation.Services;

namespace Collectary.UI.Tests.Services;

[TestFixture]
public class BogusSampleDataTest
{
    [Test]
    public void SameSeed_ProducesIdenticalSequences()
    {
        var a = new BogusSampleData(123);
        var b = new BogusSampleData(123);

        var seqA = Enumerable.Range(0, 5).Select(_ => a.Int(1, 1000)).ToList();
        var seqB = Enumerable.Range(0, 5).Select(_ => b.Int(1, 1000)).ToList();

        Assert.That(seqA, Is.EqualTo(seqB));
        Assert.That(a.Digits(13), Is.EqualTo(b.Digits(13)));
    }

    [Test]
    public void Int_StaysWithinInclusiveBounds()
    {
        var data = new BogusSampleData(7);

        for (var i = 0; i < 200; i++)
        {
            var n = data.Int(5, 9);
            Assert.That(n, Is.InRange(5, 9));
        }
    }

    [Test]
    public void Decimal_IsRoundedToRequestedPlacesAndWithinBounds()
    {
        var data = new BogusSampleData(7);

        for (var i = 0; i < 200; i++)
        {
            var d = data.Decimal(1m, 10m, 2);
            Assert.That(d, Is.InRange(1m, 10m));
            Assert.That(decimal.Round(d, 2), Is.EqualTo(d));
        }
    }

    [Test]
    public void Digits_HasRequestedLengthAndIsNumeric()
    {
        var data = new BogusSampleData(7);

        var code = data.Digits(13);

        Assert.That(code, Has.Length.EqualTo(13));
        Assert.That(code, Does.Match("^[0-9]+$"));
    }

    [Test]
    public void Words_ReturnsNonEmptyText()
    {
        var data = new BogusSampleData(7);

        Assert.That(data.Words(2), Is.Not.Empty);
        Assert.That(data.Sentence(), Is.Not.Empty);
    }

    [Test]
    public void PickOne_ReturnsAnElementFromTheList()
    {
        var data = new BogusSampleData(7);
        var items = new[] { "a", "b", "c" };

        for (var i = 0; i < 50; i++)
            Assert.That(items, Does.Contain(data.PickOne(items)));
    }

    [Test]
    public void PastDateUtc_IsUtcAndInThePast()
    {
        var data = new BogusSampleData(7);

        var d = data.PastDateUtc();

        Assert.That(d.Kind, Is.EqualTo(DateTimeKind.Utc));
        Assert.That(d, Is.LessThan(DateTime.UtcNow));
    }
}

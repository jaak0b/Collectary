using Collectary.Core.Domain;

namespace Collectary.Core.Tests.Domain;

[TestFixture]
public class LamportClockTest
{
    private readonly LamportClock _clock = new();

    [Test]
    public void Next_IsOneAboveTheHigherOfCurrentAndObserved()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_clock.Next(5, 3), Is.EqualTo(6));
            Assert.That(_clock.Next(3, 5), Is.EqualTo(6));
            Assert.That(_clock.Next(0, 0), Is.EqualTo(1));
        });
    }
}

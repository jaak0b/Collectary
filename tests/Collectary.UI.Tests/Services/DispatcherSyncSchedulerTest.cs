using Collectary.UI.Services;

namespace Collectary.UI.Tests.Services;

[TestFixture]
public class DispatcherSyncSchedulerTest
{
    [Test]
    public void StartStopDispose_AreSafeAndIdempotent()
    {
        var sut = new DispatcherSyncScheduler();

        Assert.DoesNotThrow(() =>
        {
            sut.Start(TimeSpan.FromMinutes(5), () => Task.CompletedTask);
            sut.Start(TimeSpan.FromMinutes(1), () => Task.CompletedTask);
            sut.Stop();
            sut.Stop();
            sut.Dispose();
            sut.Dispose();
        });
    }
}

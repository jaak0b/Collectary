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
            sut.Start(TimeSpan.FromMinutes(5), _ => Task.CompletedTask);
            sut.Start(TimeSpan.FromMinutes(1), _ => Task.CompletedTask);
            sut.Stop();
            sut.Stop();
            sut.Dispose();
            sut.Dispose();
        });
    }

    [Test]
    public void Tick_WhenItThrows_RaisesTickFailed()
    {
        using var sut = new DispatcherSyncScheduler();
        Exception? captured = null;
        using var signaled = new ManualResetEventSlim();
        sut.TickFailed += ex =>
        {
            captured = ex;
            signaled.Set();
        };

        sut.Start(TimeSpan.FromMilliseconds(20), _ => throw new InvalidOperationException("boom"));

        Assert.That(signaled.Wait(TimeSpan.FromSeconds(5)), Is.True, "a throwing tick must surface via TickFailed");
        sut.Stop();
        Assert.That(captured, Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void Start_WhenATickIsInFlight_SkipsOverlappingTicks()
    {
        using var sut = new DispatcherSyncScheduler();
        var release = new TaskCompletionSource();
        var concurrent = 0;
        var maxConcurrent = 0;
        using var entered = new ManualResetEventSlim();

        sut.Start(TimeSpan.FromMilliseconds(20), async _ =>
        {
            var now = Interlocked.Increment(ref concurrent);
            maxConcurrent = Math.Max(maxConcurrent, now);
            entered.Set();
            await release.Task;
            Interlocked.Decrement(ref concurrent);
        });

        Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True, "the first tick must run");
        Thread.Sleep(150);
        release.SetResult();
        sut.Stop();

        Assert.That(maxConcurrent, Is.EqualTo(1), "a tick already in flight must suppress overlapping ticks");
    }
}

using Collectary.Core.Ports;
using Collectary.Presentation.Services;
using FakeItEasy;

namespace Collectary.UI.Tests.Services;

[TestFixture]
public class UpdateCheckTest
{
    [Test]
    public async Task RunAsync_WhenAnUpdateIsAvailable_DownloadsIt()
    {
        var updater = A.Fake<IAppUpdater>();
        A.CallTo(() => updater.CheckForUpdateAsync()).Returns(true);

        await new UpdateCheck(updater, A.Fake<IAppLogger>()).RunAsync();

        A.CallTo(() => updater.DownloadUpdateAsync()).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task RunAsync_WhenNoUpdateIsAvailable_DoesNotDownload()
    {
        var updater = A.Fake<IAppUpdater>();
        A.CallTo(() => updater.CheckForUpdateAsync()).Returns(false);

        await new UpdateCheck(updater, A.Fake<IAppLogger>()).RunAsync();

        A.CallTo(() => updater.DownloadUpdateAsync()).MustNotHaveHappened();
    }

    [Test]
    public async Task RunAsync_WhenAnUpdateIsAvailable_StagesItForApplyOnExitAfterDownloading()
    {
        var updater = A.Fake<IAppUpdater>();
        A.CallTo(() => updater.CheckForUpdateAsync()).Returns(true);

        await new UpdateCheck(updater, A.Fake<IAppLogger>()).RunAsync();

        A.CallTo(() => updater.DownloadUpdateAsync()).MustHaveHappenedOnceExactly()
            .Then(A.CallTo(() => updater.ApplyUpdateOnExit()).MustHaveHappenedOnceExactly());
    }

    [Test]
    public async Task RunAsync_WhenNoUpdateIsAvailable_DoesNotStageApply()
    {
        var updater = A.Fake<IAppUpdater>();
        A.CallTo(() => updater.CheckForUpdateAsync()).Returns(false);

        await new UpdateCheck(updater, A.Fake<IAppLogger>()).RunAsync();

        A.CallTo(() => updater.ApplyUpdateOnExit()).MustNotHaveHappened();
    }

    [Test]
    public async Task RunAsync_WhenTheDownloadFails_DoesNotStageApply()
    {
        var updater = A.Fake<IAppUpdater>();
        A.CallTo(() => updater.CheckForUpdateAsync()).Returns(true);
        A.CallTo(() => updater.DownloadUpdateAsync()).Throws(new InvalidOperationException("network drop"));

        await new UpdateCheck(updater, A.Fake<IAppLogger>()).RunAsync();

        A.CallTo(() => updater.ApplyUpdateOnExit()).MustNotHaveHappened();
    }

    [Test]
    public void RunAsync_WhenTheCheckFails_SwallowsTheError()
    {
        var updater = A.Fake<IAppUpdater>();
        A.CallTo(() => updater.CheckForUpdateAsync()).Throws(new InvalidOperationException("offline"));

        Assert.That(async () => await new UpdateCheck(updater, A.Fake<IAppLogger>()).RunAsync(), Throws.Nothing);
    }

    [Test]
    public async Task RunAsync_WhenTheDownloadFails_SwallowsTheError()
    {
        var updater = A.Fake<IAppUpdater>();
        A.CallTo(() => updater.CheckForUpdateAsync()).Returns(true);
        A.CallTo(() => updater.DownloadUpdateAsync()).Throws(new InvalidOperationException("network drop"));

        await new UpdateCheck(updater, A.Fake<IAppLogger>()).RunAsync();

        A.CallTo(() => updater.DownloadUpdateAsync()).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task RunAsync_WhenItFails_LogsTheError()
    {
        var updater = A.Fake<IAppUpdater>();
        var logger = A.Fake<IAppLogger>();
        var failure = new InvalidOperationException("offline");
        A.CallTo(() => updater.CheckForUpdateAsync()).Throws(failure);

        await new UpdateCheck(updater, logger).RunAsync();

        A.CallTo(() => logger.Error(failure, "Background update check failed.", A<object?[]>._))
            .MustHaveHappenedOnceExactly();
    }
}

using Collectary.UI.Services;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class SerilogAppLoggerTest
{
    [Test]
    public void DelegatesWithoutThrowing()
    {
        var logger = new SerilogAppLogger();
        Assert.DoesNotThrow(() =>
        {
            logger.Verbose("v {X}", 1);
            logger.Debug("d {X}", 1);
            logger.Information("i {X}", 1);
            logger.Warning("w {X}", 1);
            logger.Error(new Exception("boom"), "e {X}", 1);
        });
    }
}

using Collectary.Core.Logging;

namespace Collectary.Core.Tests.Logging;

[TestFixture]
public class NullAppLoggerTest
{
    private NullAppLogger _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new NullAppLogger();

    [Test]
    public void Verbose_DoesNotThrow() =>
        Assert.DoesNotThrow(() => _sut.Verbose("msg {0}", "arg"));

    [Test]
    public void Debug_DoesNotThrow() =>
        Assert.DoesNotThrow(() => _sut.Debug("msg {0}", "arg"));

    [Test]
    public void Information_DoesNotThrow() =>
        Assert.DoesNotThrow(() => _sut.Information("msg {0}", "arg"));

    [Test]
    public void Warning_DoesNotThrow() =>
        Assert.DoesNotThrow(() => _sut.Warning("msg {0}", "arg"));

    [Test]
    public void Error_DoesNotThrow() =>
        Assert.DoesNotThrow(() => _sut.Error(new Exception("test"), "msg {0}", "arg"));
}

using Collectary.Core.Logging;
using Collectary.Core.Ports;

namespace Collectary.Infrastructure.Tests.Sync;

internal sealed class FixedDeviceIdentity : IDeviceIdentity
{
    public FixedDeviceIdentity(Guid id) => DeviceId = id;

    public Guid DeviceId { get; }
}

internal sealed class RecordingLogger : IAppLogger
{
    public int Errors { get; private set; }
    public int Warnings { get; private set; }
    public void Verbose(string messageTemplate, params object?[] propertyValues) { }
    public void Debug(string messageTemplate, params object?[] propertyValues) { }
    public void Information(string messageTemplate, params object?[] propertyValues) { }
    public void Warning(string messageTemplate, params object?[] propertyValues) => Warnings++;
    public void Error(Exception exception, string messageTemplate, params object?[] propertyValues) => Errors++;
}
